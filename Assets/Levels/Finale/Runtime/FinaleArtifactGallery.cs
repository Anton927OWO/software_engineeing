using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Finale
{
    /// <summary>Extends the existing successful result; never publishes or changes a level result.</summary>
    public sealed class FinaleArtifactGallery : MonoBehaviour
    {
        [SerializeField] private Button[] entries;
        [SerializeField] private Image[] icons;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private TMP_Text selectedTitle;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Button artifactSourceButton;
        [SerializeField] private Button contextSourceButton;
        [SerializeField] private RectTransform resultCard;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultBody;
        [SerializeField] private RectTransform retry;
        [SerializeField] private RectTransform continueButton;
        private FinaleArtifact[] artifacts;
        private FinaleArtifactCatalog catalog;
        private FinaleArtifactFact selected;
        private Vector2 originalCardSize, originalTitlePosition, originalTitleSize, originalBodyPosition, originalBodySize;
        private Vector2 originalRetryPosition, originalRetrySize, originalContinuePosition, originalContinueSize;
        private float originalBodyFontSize;
        private bool initialized;

        public void Initialize(FinaleArtifact[] artifactViews, FinaleArtifactCatalog facts)
        {
            if (initialized) return;
            initialized = true;
            artifacts = artifactViews; catalog = facts;
            originalCardSize = resultCard.sizeDelta;
            originalTitlePosition = resultTitle.rectTransform.anchoredPosition; originalTitleSize = resultTitle.rectTransform.sizeDelta;
            originalBodyPosition = resultBody.rectTransform.anchoredPosition; originalBodySize = resultBody.rectTransform.sizeDelta;
            originalBodyFontSize = resultBody.fontSize;
            originalRetryPosition = retry.anchoredPosition; originalRetrySize = retry.sizeDelta;
            originalContinuePosition = continueButton.anchoredPosition; originalContinueSize = continueButton.sizeDelta;
            for (int i = 0; i < entries.Length; i++)
            {
                int index = i;
                icons[i].sprite = artifacts[i].icon;
                labels[i].text = artifacts[i].title;
                entries[i].onClick.AddListener(() => Select(index));
            }
            artifactSourceButton.onClick.AddListener(() => OpenSource(selected?.sourceUrl));
            contextSourceButton.onClick.AddListener(() => OpenSource(selected?.contextSourceUrl));
        }

        public void Show(int artifactIndex)
        {
            gameObject.SetActive(true);
            resultCard.sizeDelta = new Vector2(1120, 980);
            Position(resultTitle.rectTransform, new Vector2(0, 435), new Vector2(1000, 90));
            Position(resultBody.rectTransform, new Vector2(0, 300), new Vector2(1000, 170));
            resultBody.fontSize = 26;
            Position(retry, new Vector2(-210, -425), new Vector2(380, 72));
            Position(continueButton, new Vector2(210, -425), new Vector2(380, 72));
            Select(Mathf.Clamp(artifactIndex, 0, artifacts.Length - 1));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (!initialized) return;
            selected = null;
            resultCard.sizeDelta = originalCardSize;
            Position(resultTitle.rectTransform, originalTitlePosition, originalTitleSize);
            Position(resultBody.rectTransform, originalBodyPosition, originalBodySize);
            resultBody.fontSize = originalBodyFontSize;
            Position(retry, originalRetryPosition, originalRetrySize);
            Position(continueButton, originalContinuePosition, originalContinueSize);
        }

        private void Select(int index)
        {
            if (!gameObject.activeInHierarchy) return;
            selected = catalog.Find(artifacts[index].id);
            selectedIcon.sprite = artifacts[index].icon;
            selectedTitle.text = artifacts[index].title;
            description.text = selected.IsVerified
                ? selected.knowledge + "\n" + selected.context + "\n" + selected.VerificationLabel + "\n圖像為示意，非考古測繪。"
                : selected.KnowledgeBody;
            artifactSourceButton.interactable = selected.IsVerified && IsWebUrl(selected.sourceUrl);
            contextSourceButton.interactable = selected.IsVerified && IsWebUrl(selected.contextSourceUrl);
            for (int i = 0; i < entries.Length; i++)
                entries[i].GetComponent<Image>().color = i == index
                    ? new Color32(226, 177, 69, 255) : new Color32(247, 241, 226, 255);
        }

        private void OpenSource(string url)
        {
            if (gameObject.activeInHierarchy && selected != null && selected.IsVerified && IsWebUrl(url))
                Application.OpenURL(url);
        }

        private static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
        private static void Position(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
}
