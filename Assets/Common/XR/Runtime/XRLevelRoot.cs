using UnityEngine;

namespace TrackingNIU.XR
{
    /// <summary>
    /// 放在每個 MR 關卡場景的根物件上，所有桌上內容都放在它底下。
    /// 載入時自動建立 MRCore（單獨打開場景測試也可以），並把自己對齊到桌面錨點；桌面移動時跟著移動。
    /// 關卡場景裡不要放 Camera、AudioListener、EventSystem。
    /// </summary>
    [DefaultExecutionOrder(-1400)]
    public sealed class XRLevelRoot : MonoBehaviour
    {
        [SerializeField, Tooltip("這一關需要的桌面範圍（公尺），X 為寬、Y 為深")]
        Vector2 requiredSize = new Vector2(1.0f, 0.6f);

        [SerializeField, Tooltip("桌面移動時跟著移動")]
        bool followTable = true;

        TableAnchor _table;

        public static XRLevelRoot Current { get; private set; }
        public Vector2 RequiredSize => requiredSize;

        void Awake()
        {
            if (transform.parent != null)
                Debug.LogWarning("[XRLevelRoot] 應該放在場景的最上層物件。", this);

            Current = this;
            MRCore.EnsureExists();
        }

        void OnEnable()
        {
            _table = TableAnchor.Current;
            if (_table == null)
                return;
            if (followTable)
                _table.Moved += Place;
            Place();

            if (requiredSize.x > _table.Size.x + 0.01f || requiredSize.y > _table.Size.y + 0.01f)
                Debug.LogWarning($"[XRLevelRoot] 這一關需要 {requiredSize}，超過桌面範圍 {_table.Size}。", this);
        }

        void OnDisable()
        {
            if (_table != null)
                _table.Moved -= Place;
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>對齊到桌面錨點。</summary>
        public void Place()
        {
            if (_table == null)
                return;
            transform.SetPositionAndRotation(_table.transform.position, _table.transform.rotation);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(requiredSize.x, 0.001f, requiredSize.y));
            Gizmos.DrawLine(new Vector3(0f, 0f, -requiredSize.y / 2f), new Vector3(0f, 0f, -requiredSize.y / 2f - 0.15f));
        }
#endif
    }
}
