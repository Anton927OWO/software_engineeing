using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 放置插槽。物件放進來時判斷對錯：
    /// 放對觸發 Accepted（可選擇鎖住物件）；放錯觸發 Rejected，物件彈回原位。
    /// 判斷規則：有設定 Validator 就用 Validator，否則看 acceptedIds（留空代表全部接受）。
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class XRSnapSocket : MonoBehaviour
    {
        [SerializeField, Tooltip("接受的物件 ID；留空代表全部接受")]
        string[] acceptedIds = Array.Empty<string>();

        [SerializeField, Tooltip("放對時把物件鎖在插槽裡，不能再拿出來")]
        bool lockOnAccept = true;

        [SerializeField, Tooltip("放對一次後就關閉插槽")]
        bool singleUse = true;

        [SerializeField, Tooltip("鎖住時物件要停的位置；留空使用插槽的 Attach Transform")]
        Transform lockPoint;

        [SerializeField, Min(0f), Tooltip("放錯後插槽暫停多久，避免物件馬上又被吸回來")]
        float rejectCooldown = 0.6f;

        XRSocketInteractor _socket;

        /// <summary>自訂判斷規則，例如序章要依照順序。</summary>
        public Func<XRGrabbable, bool> Validator { get; set; }

        public XRGrabbable Occupant { get; private set; }
        public XRSocketInteractor Socket => _socket;

        public event Action<XRSnapSocket, XRGrabbable> Accepted;
        public event Action<XRSnapSocket, XRGrabbable> Rejected;

        void Awake()
        {
            _socket = GetComponent<XRSocketInteractor>();
        }

        void OnEnable()
        {
            _socket.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            _socket.selectEntered.RemoveListener(OnSelectEntered);
        }

        /// <summary>清空插槽並重新開啟（重試時使用）。</summary>
        public void ResetSocket()
        {
            StopAllCoroutines();
            Occupant = null;
            _socket.socketActive = true;
        }

        public bool IsAccepted(XRGrabbable item)
        {
            if (item == null)
                return false;
            if (Validator != null)
                return Validator(item);
            if (acceptedIds == null || acceptedIds.Length == 0)
                return true;
            return Array.IndexOf(acceptedIds, item.ItemId) >= 0;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            var item = args.interactableObject.transform.GetComponent<XRGrabbable>();
            if (item == null)
                return;

            if (IsAccepted(item))
            {
                Occupant = item;
                Accepted?.Invoke(this, item);
                if (lockOnAccept)
                    item.LockAt(lockPoint != null ? lockPoint : _socket.attachTransform);
                if (singleUse)
                    StartCoroutine(CloseNextFrame());
            }
            else
            {
                Rejected?.Invoke(this, item);
                StartCoroutine(Eject(item));
            }
        }

        IEnumerator CloseNextFrame()
        {
            yield return null;
            _socket.socketActive = false;
        }

        IEnumerator Eject(XRGrabbable item)
        {
            yield return null;
            if (_socket.hasSelection && _socket.interactionManager != null)
                _socket.interactionManager.SelectExit((IXRSelectInteractor)_socket, (IXRSelectInteractable)item.Interactable);
            _socket.socketActive = false;
            item.ReturnHome();
            yield return new WaitForSeconds(rejectCooldown);
            _socket.socketActive = true;
        }
    }
}
