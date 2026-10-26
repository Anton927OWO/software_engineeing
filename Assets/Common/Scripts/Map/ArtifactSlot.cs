using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>右側面板「文物收集」的一格。</summary>
    public sealed class ArtifactSlot : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text placeholder;

        public void Show(ArtifactId artifact)
        {
            bool collected = artifact != ArtifactId.None;
            fill.color = collected ? Color.white : UiPalette.Background;
            nameText.text = collected ? LevelCatalog.GetArtifactName(artifact) : "";
            placeholder.gameObject.SetActive(!collected);
        }
    }
}
