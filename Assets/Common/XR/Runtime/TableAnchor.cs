using System;
using System.Collections;
using UnityEngine;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 桌面錨點。所有關卡內容都放在這個桌面上。
    /// 座標約定：原點是桌面中心、y = 0 是桌面表面；玩家站在 -Z 那一側，面向 +Z。
    /// 預設在玩家面前生成虛擬檯面；TableDetector 找到真實桌面時改用真實桌面。
    /// </summary>
    [DefaultExecutionOrder(-1450)]
    public sealed class TableAnchor : MonoBehaviour
    {
        [SerializeField, Tooltip("可用的桌面範圍（公尺），X 為寬、Y 為深")]
        Vector2 size = new Vector2(1.0f, 0.6f);

        [SerializeField, Tooltip("虛擬檯面的外觀（使用真實桌面時隱藏，但保留碰撞體）")]
        Renderer[] virtualTableRenderers = Array.Empty<Renderer>();

        [SerializeField, Tooltip("移動桌面用的把手")]
        GameObject moveHandle;

        [SerializeField, Min(0.2f), Tooltip("桌面中心離玩家頭部的水平距離")]
        float distanceFromHead = 0.55f;

        [SerializeField, Tooltip("桌面比頭部低多少")]
        float belowHead = 0.6f;

        [SerializeField] float minHeight = 0.7f;
        [SerializeField] float maxHeight = 1.0f;

        bool _placed;

        public static TableAnchor Current { get; private set; }

        /// <summary>桌面移動後觸發，XRLevelRoot 依此跟著移動。</summary>
        public event Action Moved;

        public Vector2 Size => size;
        public bool IsRealTable { get; private set; }
        public Pose Pose => new Pose(transform.position, transform.rotation);

        void Awake()
        {
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        IEnumerator Start()
        {
            // 等頭部追蹤有資料再擺放。
            yield return null;
            yield return null;
            if (!_placed)
                Recenter(MRCore.Instance != null ? MRCore.Instance.Head : null);
        }

        /// <summary>把虛擬檯面重新擺到玩家面前。使用真實桌面時改回虛擬檯面。</summary>
        public void Recenter(Transform head)
        {
            var pose = head != null
                ? ComputeVirtualPose(head.position, head.forward, distanceFromHead, belowHead, minHeight, maxHeight)
                : new Pose(new Vector3(0f, 0.75f, distanceFromHead), Quaternion.identity);
            UseVirtualTable();
            SetPose(pose.position, pose.rotation);
        }

        /// <summary>移到指定位置。rotation 只取水平方向。</summary>
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _placed = true;
            transform.SetPositionAndRotation(position, YawOnly(rotation));
            Moved?.Invoke();
        }

        /// <summary>改用真實桌面：隱藏虛擬檯面外觀，保留碰撞體。</summary>
        public void UseRealTable(Vector3 center, Quaternion rotation, Vector2 realSize)
        {
            IsRealTable = true;
            SetRenderersVisible(false);
            if (realSize.x < size.x || realSize.y < size.y)
                Debug.Log($"[TableAnchor] 真實桌面 {realSize.x:0.00}×{realSize.y:0.00} 公尺，比建議的 {size.x:0.00}×{size.y:0.00} 小，內容可能超出桌面。");
            SetPose(center, rotation);
        }

        public void UseVirtualTable()
        {
            IsRealTable = false;
            SetRenderersVisible(true);
        }

        public void SetHandleVisible(bool visible)
        {
            if (moveHandle != null)
                moveHandle.SetActive(visible);
        }

        void SetRenderersVisible(bool visible)
        {
            foreach (var r in virtualTableRenderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }

        /// <summary>
        /// 依頭部位置計算虛擬檯面的位置：在頭部正前方（只看水平方向），高度依頭部高度推算並限制範圍。
        /// 假設 XR Origin 使用地板模式，世界座標 y = 0 是地板。
        /// </summary>
        public static Pose ComputeVirtualPose(Vector3 headPosition, Vector3 headForward, float distance,
            float belowHead, float minHeight, float maxHeight)
        {
            var flat = new Vector3(headForward.x, 0f, headForward.z);
            if (flat.sqrMagnitude < 1e-4f)
                flat = Vector3.forward;
            flat.Normalize();

            var height = Mathf.Clamp(headPosition.y - belowHead, minHeight, maxHeight);
            var position = new Vector3(headPosition.x, 0f, headPosition.z) + flat * distance;
            position.y = height;
            return new Pose(position, Quaternion.LookRotation(flat, Vector3.up));
        }

        /// <summary>讓桌面的 +Z 指向遠離玩家的方向。</summary>
        public static Quaternion FacingAwayFrom(Vector3 head, Vector3 tableCenter)
        {
            var dir = tableCenter - head;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;
            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        static Quaternion YawOnly(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
                return Quaternion.identity;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
