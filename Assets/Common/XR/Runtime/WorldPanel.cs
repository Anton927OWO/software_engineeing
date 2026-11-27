using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 空間中的 UGUI 面板：標題、內文、按鈕，可以用手把射線點擊（扳機鍵）。
    /// 1 像素 = 1 公釐，寬 86 公分，高度依內容自動調整；底邊是錨點，方便立在桌面後緣。
    /// 一張面板的內文盡量不超過 4 行。
    /// </summary>
    public sealed class WorldPanel : MonoBehaviour
    {
        /// <summary>Resources 底下的 prefab 路徑，由「3. 建立 MRCore 與 MR 場景」產生。</summary>
        public const string ResourcePath = "TrackingNIU/WorldPanel";

        [SerializeField] Canvas canvas;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] RectTransform buttonRow;
        [SerializeField] Button buttonTemplate;

        [SerializeField, Tooltip("每幀轉向玩家（只轉水平方向）")]
        bool faceHead;

        readonly List<Button> _buttons = new List<Button>();

        /// <summary>在 parent 底下建立面板。localPosition 是面板底邊中心的位置。</summary>
        public static WorldPanel Create(Transform parent, Vector3 localPosition, string titleText = "", string bodyText = "")
        {
            var prefab = Resources.Load<WorldPanel>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[WorldPanel] 找不到 WorldPanel prefab。請執行「尋跡宜大 → MR 原型 → 3. 建立 MRCore 與 MR 場景」。");
                return null;
            }

            var panel = Instantiate(prefab, parent);
            panel.name = prefab.name;
            panel.transform.localPosition = localPosition;
            panel.transform.localRotation = Quaternion.identity;
            panel.SetText(titleText, bodyText);
            return panel;
        }

        void Awake()
        {
            if (buttonTemplate != null)
                buttonTemplate.gameObject.SetActive(false);
        }

        void Start()
        {
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = MRCore.Instance != null ? MRCore.Instance.Camera : Camera.main;
        }

        void LateUpdate()
        {
            if (!faceHead || MRCore.Instance == null || MRCore.Instance.Head == null)
                return;
            var dir = transform.position - MRCore.Instance.Head.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        public bool FaceHead
        {
            get => faceHead;
            set => faceHead = value;
        }

        public void SetText(string titleText, string bodyText)
        {
            SetTitle(titleText);
            SetBody(bodyText);
        }

        public void SetTitle(string text)
        {
            if (title == null)
                return;
            title.text = text ?? "";
            title.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetBody(string text)
        {
            if (body == null)
                return;
            body.text = text ?? "";
            body.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        /// <summary>加一個按鈕，按鈕排成兩欄。</summary>
        public Button AddButton(string label, Action onClick)
        {
            var button = Instantiate(buttonTemplate, buttonRow);
            button.gameObject.SetActive(true);
            button.name = "Button_" + label;
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            _buttons.Add(button);
            buttonRow.gameObject.SetActive(true);
            return button;
        }

        public void ClearButtons()
        {
            foreach (var button in _buttons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            _buttons.Clear();
            buttonRow.gameObject.SetActive(false);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
