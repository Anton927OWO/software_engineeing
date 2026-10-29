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

        public Button Button => button;
        public bool FaceVisible { get; private set; }
        public bool GoldFrameVisible => goldFrame.activeSelf;

        public void Bind(int index, Action<int> reveal)
        {
            if (click != null) button.onClick.RemoveListener(click);
            click = () => reveal(index);
            button.onClick.AddListener(click);
        }

        public void ResetCard(Sprite face, Sprite cardBack)
        {
            front = face; back = cardBack;
            SetFace(false); SetMatched(false); SetFlipScale(1);
        }

        public void SetFace(bool up)
        {
            FaceVisible = up;
            artwork.sprite = up ? front : back;
        }

        public void SetMatched(bool value)
        {
            goldFrame.SetActive(value);
            matchedLabel.gameObject.SetActive(value);
            if (matchedBadge != null) matchedBadge.SetActive(value);
        }

        public void SetFlipScale(float x) => visual.localScale = new Vector3(x, 1, 1);
        public void SetInteractable(bool value) => button.interactable = value;
    }
}
