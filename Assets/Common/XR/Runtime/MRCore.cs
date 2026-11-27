using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrackingNIU.XR
{
    /// <summary>
    /// MR 原型的常駐物件：玩家（攝影機、手把）、passthrough、XR EventSystem、桌面錨點與選單鍵。
    /// 整個遊戲只有一份，跨場景保留。關卡場景不需要放它，XRLevelRoot 會自動建立。
    /// </summary>
    [DefaultExecutionOrder(-1500)]
    public sealed class MRCore : MonoBehaviour
    {
        /// <summary>Resources 底下的 prefab 路徑，由「3. 建立 MRCore 與 MR 場景」產生。</summary>
        public const string ResourcePath = "TrackingNIU/MRCore";

        [SerializeField] XROrigin xrOrigin;
        [SerializeField] TableAnchor tableAnchor;

        [SerializeField, Tooltip("沒有接頭盔時（編輯器）自動生成的 XR 模擬器 prefab")]
        GameObject simulatorPrefab;

        [SerializeField, Tooltip("開啟 passthrough：攝影機背景改為透明，看得到真實環境")]
        bool passthrough = true;

        [SerializeField, Tooltip("模擬器沒有 passthrough，改用這個背景色")]
        Color simulatorBackground = new Color(0.32f, 0.34f, 0.36f, 1f);

        [SerializeField, Min(0.2f), Tooltip("按住右手 B 鍵（鍵盤 R）幾秒後，把桌面重新擺到玩家面前")]
        float recenterHoldSeconds = 1f;

        InputAction _menuAction;
        InputAction _recenterAction;
        float _recenterHeld;
        bool _recenterFired;

        public static MRCore Instance { get; private set; }

        /// <summary>按下左手選單鍵（鍵盤 Esc）。暫停面板訂閱這個事件。</summary>
        public static event Action MenuPressed;

        public XROrigin Origin => xrOrigin;
        public Camera Camera => xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;
        public Transform Head => Camera != null ? Camera.transform : null;
        public TableAnchor Table => tableAnchor;

        /// <summary>目前是不是在編輯器裡用模擬器操作（沒有頭盔）。</summary>
        public bool IsSimulated { get; private set; }

        /// <summary>需要時從 Resources 建立 MRCore。已經有的話直接回傳。</summary>
        public static MRCore EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var prefab = Resources.Load<MRCore>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[MRCore] 找不到 MRCore prefab。請執行「尋跡宜大 → MR 原型 → 3. 建立 MRCore 與 MR 場景」。");
                return null;
            }

            var core = Instantiate(prefab);
            core.name = prefab.name;
            return core;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyCameraBackground(passthrough ? new Color(0f, 0f, 0f, 0f) : simulatorBackground);

            _menuAction = new InputAction("Menu", InputActionType.Button);
            _menuAction.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            _menuAction.AddBinding("<Keyboard>/escape");

            _recenterAction = new InputAction("Recenter", InputActionType.Button);
            _recenterAction.AddBinding("<XRController>{RightHand}/{SecondaryButton}");
            _recenterAction.AddBinding("<Keyboard>/r");
        }

        IEnumerator Start()
        {
            // 等 XR 初始化完成再判斷有沒有頭盔。
            yield return null;
            yield return null;

            if (Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive)
            {
                IsSimulated = true;
                ApplyCameraBackground(simulatorBackground);
                if (simulatorPrefab != null)
                {
                    var simulator = Instantiate(simulatorPrefab, transform);
                    simulator.name = simulatorPrefab.name;
                    Debug.Log("[MRCore] 沒有偵測到頭盔，啟用 XR 模擬器。");
                }
                else
                {
                    Debug.LogWarning("[MRCore] 沒有偵測到頭盔，也沒有設定模擬器 prefab。");
                }
            }
        }

        void OnEnable()
        {
            _menuAction?.Enable();
            _recenterAction?.Enable();
        }

        void OnDisable()
        {
            _menuAction?.Disable();
            _recenterAction?.Disable();
        }

        void OnDestroy()
        {
            _menuAction?.Dispose();
            _recenterAction?.Dispose();
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (_menuAction.WasPressedThisFrame())
                MenuPressed?.Invoke();

            if (_recenterAction.IsPressed())
            {
                _recenterHeld += Time.unscaledDeltaTime;
                if (!_recenterFired && _recenterHeld >= recenterHoldSeconds && tableAnchor != null)
                {
                    _recenterFired = true;
                    tableAnchor.Recenter(Head);
                }
            }
            else
            {
                _recenterHeld = 0f;
                _recenterFired = false;
            }
        }

        void ApplyCameraBackground(Color color)
        {
            var cam = Camera;
            if (cam == null)
                return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = color;
        }

        // 關閉 Domain Reload 時清除靜態事件。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            MenuPressed = null;
        }
    }
}
