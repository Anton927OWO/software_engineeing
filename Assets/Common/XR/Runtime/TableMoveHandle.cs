using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 桌面前緣的把手：用握把鍵抓住後移動手，就能調整桌面的位置與高度。
    /// 放開後桌面自動轉向，讓 +Z 朝向遠離玩家的方向。
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class TableMoveHandle : MonoBehaviour
    {
        [SerializeField] TableAnchor table;

        XRSimpleInteractable _interactable;
        Transform _attach;
        Vector3 _handStart;
        Vector3 _tableStart;

        void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            if (table == null)
                table = GetComponentInParent<TableAnchor>();
        }

        void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            _attach = null;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (table == null || args.interactorObject is XRSocketInteractor)
                return;
            _attach = args.interactorObject.GetAttachTransform(_interactable);
            _handStart = _attach.position;
            _tableStart = table.transform.position;
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (_attach == null)
                return;
            _attach = null;
            var head = MRCore.Instance != null ? MRCore.Instance.Head : null;
            if (head != null)
                table.SetPose(table.transform.position, TableAnchor.FacingAwayFrom(head.position, table.transform.position));
        }

        void Update()
        {
            if (_attach == null)
                return;
            var position = _tableStart + (_attach.position - _handStart);
            table.SetPose(position, table.transform.rotation);
        }
    }
}
