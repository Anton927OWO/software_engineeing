using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>地圖上的關卡標記。這局沒抽到的關卡會隱藏。</summary>
    public sealed class MapMarker : MonoBehaviour
    {
        [SerializeField] LevelId levelId;
        [SerializeField] Button button;
        [SerializeField] StatusIcon icon;
        [SerializeField] TMP_Text label;
        [SerializeField] GameObject selectionRing;

        public LevelId LevelId => levelId;
        public Button Button => button;

        public void Refresh(LevelStatus status, bool selected)
        {
            gameObject.SetActive(status != LevelStatus.NotInRun);
            icon.SetStatus(status);
            label.text = LevelCatalog.Get(levelId).DisplayName;
            selectionRing.SetActive(selected);
        }
    }
}
