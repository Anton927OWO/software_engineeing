using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>抽關翻牌的一張卡：背面是「？」，翻開後顯示關卡名稱、地點與區域色條。</summary>
    public sealed class DrawCard : MonoBehaviour
    {
        const float FlipDuration = 0.35f;

        [SerializeField] GameObject back;
        [SerializeField] GameObject front;
        [SerializeField] Image zoneStrip;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text locationText;

        public void ShowBack()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
            back.SetActive(true);
            front.SetActive(false);
        }

        /// <summary>水平壓扁到 0、換成正面，再展開回來。</summary>
        public IEnumerator Flip(LevelDefinition level)
        {
            yield return ScaleX(1f, 0f);
            zoneStrip.color = UiPalette.GetZoneColor(level.Id);
            nameText.text = level.DisplayName;
            // 卡片窄，在「・」處換行，避免「展區二」之類的字被拆開。
            locationText.text = level.Location.Replace("・", "\n");
            back.SetActive(false);
            front.SetActive(true);
            yield return ScaleX(0f, 1f);
        }

        IEnumerator ScaleX(float from, float to)
        {
            float half = FlipDuration / 2f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                transform.localScale = new Vector3(Mathf.Lerp(from, to, t / half), 1f, 1f);
                yield return null;
            }
            transform.localScale = new Vector3(to, 1f, 1f);
        }
    }
}
