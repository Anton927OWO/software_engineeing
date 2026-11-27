using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 碰一下就觸發的物件：用手指／手把戳，或用射線瞄準按握把鍵都可以。
    /// 例如終章翻牌、地圖上的地點標記、桌上的按鈕。
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class XRPokeTarget : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("觸發後多久內不再觸發")]
        float cooldown = 0.3f;

        [SerializeField] UnityEvent onPoked = new UnityEvent();

        XRSimpleInteractable _interactable;
        float _lastPoke = float.NegativeInfinity;

        public XRSimpleInteractable Interactable => _interactable;
        public event Action<XRPokeTarget, IXRSelectInteractor> Poked;

        void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
        }

        void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        public void SetInteractable(bool value) => _interactable.enabled = value;

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
                return;
            if (Time.unscaledTime - _lastPoke < cooldown)
                return;
            _lastPoke = Time.unscaledTime;
            Haptics.Play(args.interactorObject, HapticPreset.Tick);
            Poked?.Invoke(this, args.interactorObject);
            onPoked.Invoke();
        }
    }
}
