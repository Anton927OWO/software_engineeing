using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    /// <summary>刷子（或其他手持工具）碰到表面時的資料。</summary>
    public readonly struct BrushContact
    {
        /// <summary>工具尖端最接近表面的點（世界座標）。</summary>
        public readonly Vector3 Point;
        /// <summary>工具尖端的移動速度（公尺／秒）。</summary>
        public readonly Vector3 Velocity;
        /// <summary>工具尖端壓進表面的深度（公尺），0 代表只是輕輕碰到。</summary>
        public readonly float Depth;
        public readonly Collider Collider;
        public readonly BrushTool Tool;

        public float Speed => Velocity.magnitude;

        public BrushContact(Vector3 point, Vector3 velocity, float depth, Collider collider, BrushTool tool)
        {
            Point = point;
            Velocity = velocity;
            Depth = depth;
            Collider = collider;
            Tool = tool;
        }
    }

    /// <summary>可以被刷的表面，例如考古小隊的土堆。實作在碰撞體所在的物件或它的父物件上。</summary>
    public interface IBrushSurface
    {
        void OnBrush(in BrushContact contact);
    }

    /// <summary>
    /// 手持工具：被拿著的時候，每個物理幀檢查尖端碰到哪些 IBrushSurface，並回報接觸點、速度與按壓深度。
    /// 判斷刷得好不好（覆蓋率、太用力算損傷）由各關自己決定。
    /// </summary>
    public sealed class BrushTool : MonoBehaviour
    {
        [SerializeField, Tooltip("工具尖端（刷毛的位置）")]
        Transform tip;

        [SerializeField, Min(0.001f)] float radius = 0.02f;
        [SerializeField] LayerMask surfaceLayers = ~0;

        [SerializeField, Tooltip("只有被手拿著時才回報")]
        bool onlyWhenHeld = true;

        readonly Collider[] _hits = new Collider[16];
        readonly HashSet<IBrushSurface> _touched = new HashSet<IBrushSurface>();
        XRGrabInteractable _grab;
        Vector3 _lastTipPosition;

        public Transform Tip => tip != null ? tip : transform;
        public float Radius => radius;
        public Vector3 TipVelocity { get; private set; }

        public bool IsHeld => _grab == null || (_grab.isSelected && !(_grab.firstInteractorSelecting is XRSocketInteractor));

        /// <summary>目前拿著工具的手；沒有拿著時為 null。</summary>
        public IXRSelectInteractor Holder => _grab != null && IsHeld ? _grab.firstInteractorSelecting : null;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _lastTipPosition = Tip.position;
        }

        void FixedUpdate()
        {
            var position = Tip.position;
            TipVelocity = (position - _lastTipPosition) / Time.fixedDeltaTime;
            _lastTipPosition = position;

            if (onlyWhenHeld && !IsHeld)
                return;

            _touched.Clear();
            var count = Physics.OverlapSphereNonAlloc(position, radius, _hits, surfaceLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.transform.IsChildOf(transform))
                    continue;
                var surface = hit.GetComponentInParent<IBrushSurface>();
                if (surface == null || !_touched.Add(surface))
                    continue;

                var point = hit.ClosestPoint(position);
                // 尖端球體的底部壓到表面頂部以下多少。
                var depth = Mathf.Max(0f, hit.bounds.max.y - (position.y - radius));
                surface.OnBrush(new BrushContact(point, TipVelocity, depth, hit, this));
            }
        }

        /// <summary>讓拿著工具的手震動。</summary>
        public void Pulse(HapticPreset preset) => Haptics.Play(Holder, preset);

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Tip.position, radius);
        }
#endif
    }
}
