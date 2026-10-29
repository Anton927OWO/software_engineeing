using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class StrataPatternOptionDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private StrataStageTwoManager owner;
    private RectTransform rect, homeParent;
    private Image graphic;
    private Vector2 homePosition, homeSize;
    private int homeSibling, pointerId;
    private Vector3 grabOffset;
    private bool dragging, handled;
    public string OptionId { get; private set; }
    public bool IsPlaced { get; private set; }
    public StrataStageTwoManager Owner => owner;

    public void Initialize(StrataStageTwoManager manager)
    {
        owner = manager;
        rect = GetComponent<RectTransform>();
        graphic = GetComponent<Image>();
        homeParent = rect.parent as RectTransform;
        homePosition = rect.anchoredPosition;
        homeSize = rect.sizeDelta;
        homeSibling = rect.GetSiblingIndex();
    }
    public void Bind(string optionId)
    {
        CancelDrag();
        OptionId = optionId;
        IsPlaced = false;
        RestoreHome();
        handled = false;
    }
    public void SetInteractable(bool value)
    {
        if (graphic != null) graphic.raycastTarget = value && !IsPlaced;
    }
    public bool AcceptsPointer(int id) => dragging && pointerId == id;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || owner == null ||
            IsPlaced || dragging || !owner.TryBeginDrag(this)) return;
        Vector3 point;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(owner.DragRoot,
                eventData.position, eventData.pressEventCamera, out point))
        { owner.ReleaseDrag(this); return; }
        pointerId = eventData.pointerId;
        dragging = true;
        handled = false;
        grabOffset = rect.position - point;
        rect.SetParent(owner.DragRoot, true);
        rect.SetAsLastSibling();
        graphic.raycastTarget = false;
        Move(eventData);
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (AcceptsPointer(eventData.pointerId) && !IsPlaced) Move(eventData);
    }
    private void Move(PointerEventData eventData)
    {
        Vector3 point;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(owner.DragRoot,
                eventData.position, eventData.pressEventCamera, out point))
            rect.position = point + grabOffset;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!AcceptsPointer(eventData.pointerId)) return;
        dragging = false;
        if (!IsPlaced)
        {
            RestoreHome();
            if (!handled && owner != null) owner.ReportMistake();
        }
        if (owner != null) owner.ReleaseDrag(this);
    }
    internal void MarkHandled() => handled = true;
    internal void Place(RectTransform target)
    {
        IsPlaced = true;
        handled = true;
        rect.SetParent(target, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = homeSize;
        rect.localScale = Vector3.one;
        graphic.raycastTarget = false;
    }
    private void RestoreHome()
    {
        if (rect == null || homeParent == null) return;
        rect.SetParent(homeParent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = homePosition;
        rect.sizeDelta = homeSize;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetSiblingIndex(Mathf.Min(homeSibling, homeParent.childCount - 1));
        graphic.raycastTarget = owner != null && owner.CanInteract && !IsPlaced;
    }
    public void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        if (!IsPlaced) RestoreHome();
        if (owner != null) owner.ReleaseDrag(this);
    }
    void OnDisable() => CancelDrag();
    void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
}
