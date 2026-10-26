using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>
    /// 地圖上可以點選的地點：樓層地圖的一層樓，或校園地圖的一個建築。
    /// 這局有抽到其中的關卡時顯示區域色並可以點選，否則為灰色。
    /// </summary>
    public sealed class MapLocation : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image fill;
        [SerializeField] TMP_Text[] labels;
        [SerializeField] Color zoneColor = Color.white;
        [Tooltip("這個地點上的關卡標記；沒有關卡的樓層留空，永遠是灰色")]
        [SerializeField] MapMarker[] markers;

        public Button Button => button;
        public MapMarker[] Markers => markers;

        public void Refresh(GameRun run)
        {
            bool active = HasLevelInRun(run);
            fill.color = active ? zoneColor : UiPalette.Disabled;
            foreach (var label in labels)
                label.color = active ? Color.white : UiPalette.MutedText;
            button.interactable = active;
        }

        /// <summary>點選地點時要選取的關卡：優先選可以進入的，其次選第一個有抽到的。</summary>
        public bool TryPickLevel(GameRun run, out LevelId levelId)
        {
            foreach (var marker in markers)
            {
                if (run.CanEnter(marker.LevelId))
                {
                    levelId = marker.LevelId;
                    return true;
                }
            }
            foreach (var marker in markers)
            {
                if (run.GetStatus(marker.LevelId) != LevelStatus.NotInRun)
                {
                    levelId = marker.LevelId;
                    return true;
                }
            }
            levelId = default;
            return false;
        }

        bool HasLevelInRun(GameRun run)
        {
            foreach (var marker in markers)
            {
                if (run.GetStatus(marker.LevelId) != LevelStatus.NotInRun)
                    return true;
            }
            return false;
        }
    }
}
