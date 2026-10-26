using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>
    /// 地圖上從圖資館到某個地點的路線。只顯示本局抽到的地點：
    /// 還沒去時是紅色（下一站），完成後變成綠色（返回圖資館的路線）。
    /// </summary>
    public sealed class MapRoute : MonoBehaviour
    {
        [SerializeField] LevelId destination;
        [SerializeField] Image line;

        public void Refresh(GameRun run)
        {
            var status = run.GetStatus(destination);
            gameObject.SetActive(status != LevelStatus.NotInRun);
            line.color = status == LevelStatus.Completed ? UiPalette.Correct
                : status == LevelStatus.Available ? UiPalette.Primary
                : UiPalette.Disabled;
        }
    }
}
