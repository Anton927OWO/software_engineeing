using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Finale
{
    public sealed class FinaleCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform visual;
        [SerializeField] private Image artwork;
        [SerializeField] private GameObject goldFrame;
        [SerializeField] private TMP_Text matchedLabel;
        [SerializeField] private GameObject matchedBadge;
        private Sprite front;
        private Sprite back;
        private UnityAction click;
        private string clue;
        private TMP_Text clueText;
        private string defaultMatchedLabel;

        public Button Button => button;
        public bool FaceVisible { get; private set; }
        public bool GoldFrameVisible => goldFrame.activeSelf;
        public bool IsClue => !string.IsNullOrEmpty(clue);

        public void Bind(int index, Action<int> reveal)
        {
            if (click != null) button.onClick.RemoveListener(click);
            click = () => reveal(index);
            button.onClick.AddListener(click);
        }

        /// <summary>A non-empty clue turns the card into a text card drawn over the blank face.</summary>
        public void ResetCard(Sprite face, Sprite cardBack, string clueBody = null)
        {
            front = face; back = cardBack; clue = clueBody;
            if (IsClue) EnsureClueText().text = "<size=70%><color=#8B6127>線索</color></size>\n" + clue;
            SetFace(false); SetMatched(false); SetFlipScale(1);
        }

        public void SetFace(bool up)
        {
            FaceVisible = up;
            artwork.sprite = up ? front : back;
            if (clueText != null) clueText.gameObject.SetActive(up && IsClue);
        }

        public void SetMatched(bool value, string label = null)
        {
            defaultMatchedLabel ??= matchedLabel.text;
            matchedLabel.text = label ?? defaultMatchedLabel;
            goldFrame.SetActive(value);
            matchedLabel.gameObject.SetActive(value);
            if (matchedBadge != null) matchedBadge.SetActive(value);
        }

        // Created at runtime so the scene's twelve cards need no rebuild; it scales with the flip.
        private TMP_Text EnsureClueText()
        {
            if (clueText != null) return clueText;
            var rect = new GameObject("ClueText", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(artwork.transform, false);
            rect.anchorMin = new Vector2(0.12f, 0.14f);
            rect.anchorMax = new Vector2(0.88f, 0.86f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            clueText = rect.gameObject.AddComponent<TextMeshProUGUI>();
            clueText.font = matchedLabel.font;
            clueText.color = new Color32(0x24, 0x3D, 0x32, 0xFF);
            clueText.alignment = TextAlignmentOptions.Center;
            clueText.enableAutoSizing = true;
            clueText.fontSizeMin = 14;
            clueText.fontSizeMax = 30;
            clueText.lineSpacing = 8;
            clueText.raycastTarget = false;
            return clueText;
        }

        public void SetFlipScale(float x) => visual.localScale = new Vector3(x, 1, 1);
        public void SetInteractable(bool value) => button.interactable = value;
    }
}
