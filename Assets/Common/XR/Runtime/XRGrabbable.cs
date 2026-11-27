using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 可以抓起來的物件。帶有物件 ID，給 XRSnapSocket 判斷放對放錯。
    /// 放開時如果沒有放進插槽，會回到原位（可關閉）。
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class XRGrabbable : MonoBehaviour
    {
        [SerializeField, Tooltip("物件 ID，例如 red、blue、artifact-01")]
        string itemId;

        [SerializeField, Tooltip("放開時沒有放進插槽就回到原位")]
        bool returnHomeOnRelease = true;

        [SerializeField, Min(0f)] float returnDelay = 0.15f;
        [SerializeField, Min(0.01f)] float moveDuration = 0.25f;

        XRGrabInteractable _grab;
        Transform _homeParent;
        Vector3 _homeLocalPosition;
        Quaternion _homeLocalRotation;
        Coroutine _moving;

        public string ItemId
        {
            get => itemId;
            set => itemId = value;
        }

        public XRGrabInteractable Interactable => _grab;

        /// <summary>被插槽鎖住後就不能再抓。</summary>
        public bool IsLocked { get; private set; }

        /// <summary>是否正被手拿著（被插槽選取不算）。</summary>
        public bool IsHeld => _grab.isSelected && !(_grab.firstInteractorSelecting is XRSocketInteractor);

        /// <summary>最後一隻抓過它的手，用來決定哪隻手要震動。</summary>
        public IXRSelectInteractor LastHolder { get; private set; }

        public event Action<XRGrabbable> Grabbed;
        public event Action<XRGrabbable> Released;
        public event Action<XRGrabbable> ReturnedHome;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            RecordHome();
        }

        void OnEnable()
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        /// <summary>把目前位置記為原位。</summary>
        public void RecordHome()
        {
            _homeParent = transform.parent;
            _homeLocalPosition = transform.localPosition;
            _homeLocalRotation = transform.localRotation;
        }

        /// <summary>開放或禁止抓取（例如介紹面板顯示時禁止）。</summary>
        public void SetGrabEnabled(bool value)
        {
            if (!IsLocked)
                _grab.enabled = value;
        }

        /// <summary>平滑回到原位。</summary>
        public void ReturnHome()
        {
            if (IsLocked)
                return;
            StartMove(HomePose(), () => ReturnedHome?.Invoke(this));
        }

        /// <summary>立刻回到原位並解除鎖定（重試時使用）。</summary>
        public void ResetToHome()
        {
            StopMove();
            IsLocked = false;
            _grab.enabled = true;
            transform.SetParent(_homeParent, true);
            transform.localPosition = _homeLocalPosition;
            transform.localRotation = _homeLocalRotation;
        }

        /// <summary>鎖在指定位置，之後不能再抓。target 為 null 時鎖在目前位置。</summary>
        public void LockAt(Transform target)
        {
            if (IsLocked)
                return;
            IsLocked = true;
            StartCoroutine(LockRoutine(target));
        }

        IEnumerator LockRoutine(Transform target)
        {
            // 在插槽的事件裡直接取消選取可能造成錯誤，下一幀再處理。
            yield return null;
            if (_grab.isSelected && _grab.interactionManager != null)
                _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
            _grab.enabled = false;
            if (target != null)
                StartMove(new Pose(target.position, target.rotation), null);
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;
            StopMove();
            LastHolder = args.interactorObject;
            Grabbed?.Invoke(this);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (IsLocked || args.interactorObject is XRSocketInteractor)
                return;
            Released?.Invoke(this);
            if (returnHomeOnRelease && isActiveAndEnabled)
                _moving = StartCoroutine(ReturnAfterDelay());
        }

        IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(returnDelay);
            // 已經被插槽接住，或又被抓起來，就不回去。
            if (IsLocked || _grab.isSelected)
                yield break;
            _moving = null;
            ReturnHome();
        }

        Pose HomePose()
        {
            if (_homeParent == null)
                return new Pose(_homeLocalPosition, _homeLocalRotation);
            return new Pose(_homeParent.TransformPoint(_homeLocalPosition), _homeParent.rotation * _homeLocalRotation);
        }

        void StartMove(Pose target, Action done)
        {
            StopMove();
            _moving = StartCoroutine(MoveRoutine(target, done));
        }

        void StopMove()
        {
            if (_moving != null)
                StopCoroutine(_moving);
            _moving = null;
        }

        IEnumerator MoveRoutine(Pose target, Action done)
        {
            var startPos = transform.position;
            var startRot = transform.rotation;
            for (var t = 0f; t < 1f; t += Time.deltaTime / moveDuration)
            {
                var k = Mathf.SmoothStep(0f, 1f, t);
                transform.SetPositionAndRotation(Vector3.Lerp(startPos, target.position, k),
                    Quaternion.Slerp(startRot, target.rotation, k));
                yield return null;
            }
            transform.SetPositionAndRotation(target.position, target.rotation);
            _moving = null;
            done?.Invoke();
        }
    }
}
