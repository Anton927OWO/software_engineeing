using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace TrackingNIU.XR
{
    /// <summary>
    /// （P1）在頭盔上自動尋找真實桌面：開始後幾秒內，找一個高度像桌子的水平面，把 TableAnchor 移過去。
    /// 找不到、在模擬器裡、或玩家沒有做過空間設定時，維持虛擬檯面。
    /// 需要 Quest 的「空間設定」裡有掃描過房間（或手動標示桌子）。
    /// </summary>
    public sealed class TableDetector : MonoBehaviour
    {
        const string ScenePermission = "com.oculus.permission.USE_SCENE";

        [SerializeField] bool detectRealTable = true;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] TableAnchor table;
        [SerializeField, Min(1f)] float searchSeconds = 5f;
        [SerializeField] float minTableHeight = 0.55f;
        [SerializeField] float maxTableHeight = 1.2f;
        [SerializeField, Tooltip("桌面面積至少要多少平方公尺")]
        float minArea = 0.2f;

        IEnumerator Start()
        {
            if (!detectRealTable || planeManager == null || table == null)
                yield break;

            // 等 MRCore 判斷是不是模擬器。
            yield return null;
            yield return null;
            yield return null;
            if (MRCore.Instance != null && MRCore.Instance.IsSimulated)
                yield break;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission))
                UnityEngine.Android.Permission.RequestUserPermission(ScenePermission);
#endif

            var elapsed = 0f;
            while (elapsed < searchSeconds)
            {
                if (TryFindTable(out var plane))
                {
                    var head = MRCore.Instance != null ? MRCore.Instance.Head : null;
                    var rotation = head != null ? TableAnchor.FacingAwayFrom(head.position, plane.center) : plane.transform.rotation;
                    table.UseRealTable(plane.center, rotation, plane.size);
                    Debug.Log($"[TableDetector] 找到真實桌面，高度 {plane.center.y:0.00} 公尺。");
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            Debug.Log("[TableDetector] 沒有找到真實桌面，使用虛擬檯面。");
        }

        bool TryFindTable(out ARPlane best)
        {
            best = null;
            var head = MRCore.Instance != null ? MRCore.Instance.Head : null;
            var headPos = head != null ? head.position : Vector3.zero;
            var bestDistance = float.MaxValue;

            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                    continue;
                var height = plane.center.y;
                if (height < minTableHeight || height > maxTableHeight)
                    continue;
                if (plane.size.x * plane.size.y < minArea)
                    continue;

                var offset = plane.center - headPos;
                offset.y = 0f;
                if (offset.sqrMagnitude < bestDistance)
                {
                    bestDistance = offset.sqrMagnitude;
                    best = plane;
                }
            }
            return best != null;
        }
    }
}
