using System.Globalization;
using TMPro;
using UnityEngine;

namespace TrackingNIU
{
    /// <summary>結算畫面「各關分數」的一列：左邊關卡名稱，右邊分數。</summary>
    public sealed class ResultScoreRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text scoreText;

        public void Show(string levelName, int score, bool cleared)
        {
            gameObject.SetActive(true);
            nameText.text = cleared
                ? levelName
                : $"{levelName}<size=75%><color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Error)}>（未通關）</color></size>";
            scoreText.text = score.ToString("N0", CultureInfo.InvariantCulture);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
