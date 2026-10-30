using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Shared
{
    public sealed class SharedContentPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Image illustration;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private RectTransform contentRoot;
        public Button PrimaryButton => primaryButton;
        /// <summary>Mount a level's own UI here. This foundation supplies no minigame mechanics.</summary>
        public RectTransform ContentRoot => contentRoot;

        public void Present(string heading, string text, Sprite image, string actionLabel)
        {
            title.text = heading ?? "";
            body.text = text ?? "";
            illustration.sprite = image;
            illustration.gameObject.SetActive(image != null);
            primaryLabel.text = actionLabel;
            gameObject.SetActive(true);
        }
        public void Hide() => gameObject.SetActive(false);
    }
}
