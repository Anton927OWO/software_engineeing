using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Timeline
{
    public sealed class TimelineSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField] private TimelineBoard board;
        [SerializeField] private int index;
        [SerializeField] private Image surface;
        [SerializeField] private TMP_Text prompt;
        public TimelineBoard Board => board;
        public int Index => index;
        public bool IsOccupied { get; private set; }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null || eventData.button != PointerEventData.InputButton.Left) return;
            board.TryPlace(eventData.pointerDrag.GetComponent<TimelineCard>(), this, eventData.pointerId);
        }

        public void SetOccupied(bool occupied)
        {
            IsOccupied = occupied;
            surface.color = occupied ? new Color32(222, 237, 218, 255) : new Color32(250, 246, 236, 230);
            prompt.gameObject.SetActive(!occupied);
        }
    }
}
