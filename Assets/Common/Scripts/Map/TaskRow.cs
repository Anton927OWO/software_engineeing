using TMPro;
using UnityEngine;

namespace TrackingNIU
{
    /// <summary>右側面板「本局任務」的一列。</summary>
    public sealed class TaskRow : MonoBehaviour
    {
        [SerializeField] StatusIcon icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text detail;

        public void Show(string titleText, string detailText, LevelStatus status)
        {
            gameObject.SetActive(true);
            icon.SetStatus(status);
            title.text = titleText;
            detail.text = detailText;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
