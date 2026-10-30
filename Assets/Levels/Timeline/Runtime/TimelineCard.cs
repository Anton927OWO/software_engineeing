using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XunJiYiDa.Levels.Timeline
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class TimelineCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TimelineBoard board;
        [SerializeField] private int targetIndex;
        [SerializeField] private Sprite illustration;
        [SerializeField] private TMP_Text gestureLabel;
        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 home;
        private Vector2 grabOffset;
        private int activePointer;
        private bool dragging;
        private bool locked;
        private Coroutine returning;

        public TimelineBoard Board => board;
        public int TargetIndex => targetIndex;
        public Sprite Illustration => illustration;
        public bool IsLocked => locked;
        public bool IsDragging => dragging;
        public bool IsDraggingWith(int pointerId) => dragging && activePointer == pointerId;
        private RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;
        private CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !board.CanInteract || locked || dragging) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent,
                eventData.pressPosition, eventData.pressEventCamera, out var point)) return;
            StopReturning();
            activePointer = eventData.pointerId;
            dragging = true;
            grabOffset = Rect.anchoredPosition - point;
            Rect.SetAsLastSibling();
            Group.blocksRaycasts = false;
            Group.alpha = 0.88f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsDraggingWith(eventData.pointerId)) return;
            if (!board.CanInteract) { CancelDrag(); return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent,
                eventData.position, eventData.pressEventCamera, out var point))
                Rect.anchoredPosition = point + grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsDraggingWith(eventData.pointerId)) return;
            if (board.CanInteract) board.Reject(this);
            else CancelDrag();
        }

        public void ResetTo(Vector2 position)
        {
            StopReturning();
            home = position;
            dragging = locked = false;
            Rect.anchoredPosition = home;
            Group.blocksRaycasts = true;
            Group.alpha = 1;
            gestureLabel.text = "拖曳至年份";
        }

        public void LockAt(Vector3 position)
        {
            StopReturning();
            dragging = false;
            locked = true;
            Rect.position = position;
            Group.blocksRaycasts = false;
            Group.alpha = 1;
            gestureLabel.text = "已定位";
        }

        public void BounceHome()
        {
            dragging = false;
            Group.blocksRaycasts = true;
            Group.alpha = 1;
            StopReturning();
            returning = StartCoroutine(ReturnAnimation());
        }

        public void CancelDrag()
        {
            if (locked) return;
            StopReturning();
            dragging = false;
            Rect.anchoredPosition = home;
            Group.blocksRaycasts = true;
            Group.alpha = 1;
        }

        private IEnumerator ReturnAnimation()
        {
            var from = Rect.anchoredPosition;
            float elapsed = 0;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.22f);
                Rect.anchoredPosition = Vector2.LerpUnclamped(from, home,
                    1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2));
                yield return null;
            }
            Rect.anchoredPosition = home;
            returning = null;
        }

        private void StopReturning()
        {
            if (returning == null) return;
            StopCoroutine(returning);
            returning = null;
        }

        private void OnDisable() => CancelDrag();
    }
}
