using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>
    /// 結算畫面「文物圖鑑」的一格：收集過的文物亮起來，本局取得的加上金框與「本局」標籤，
    /// 沒收集過的只顯示深色剪影。
    /// </summary>
    public sealed class ResultArtifactCard : MonoBehaviour
    {
        [SerializeField] ArtifactId artifact;
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] GameObject newTag;

        public ArtifactId Artifact => artifact;

        public void Show(bool collected, bool thisRun)
        {
            frame.color = thisRun ? UiPalette.ResultsGold
                : collected ? UiPalette.ResultsMutedText
                : UiPalette.MutedText;
            icon.color = collected ? Color.white : UiPalette.ResultsBackground;
            newTag.SetActive(thisRun);
        }
    }
}
