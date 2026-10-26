using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>關卡狀態圖示：紅色驚嘆號為未完成、綠色勾為已完成、灰色鎖為尚未開放。</summary>
    public sealed class StatusIcon : MonoBehaviour
    {
        [SerializeField] Image circle;
        [SerializeField] Image symbol;
        [SerializeField] Sprite availableSprite;
        [SerializeField] Sprite completedSprite;
        [SerializeField] Sprite lockedSprite;

        public void SetStatus(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Available:
                    circle.color = UiPalette.Error;
                    symbol.sprite = availableSprite;
                    break;
                case LevelStatus.Completed:
                    circle.color = UiPalette.Correct;
                    symbol.sprite = completedSprite;
                    break;
                default:
                    circle.color = UiPalette.Disabled;
                    symbol.sprite = lockedSprite;
                    break;
            }
        }
    }
}
