using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class StrataArtifactDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private StrataStageOneManager manager;
    [SerializeField, Range(1, 4)] private int targetLayer = 1;
    [SerializeField] private string displayName;
    [SerializeField] private Vector2 placedSize = new Vector2(110, 110);
    private RectTransform rect, homeParent;
    private Image image, soilCover;
    private Vector2 homePosition, homeSize, homeAnchorMin, homeAnchorMax, homePivot;
    private Vector3 homeScale, grabOffset;
    private Quaternion homeRotation;
    private int homeSibling, pointerId;
    private bool dragging, dropHandled, homeRaycast;
    private Coroutine placementAnimation;
    public StrataStageOneManager Owner => manager;
    public int TargetLayer => targetLayer;
    public string DisplayName => displayName;
    public bool IsPlaced { get; private set; }

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        homeParent = rect.parent as RectTransform;
        homePosition = rect.anchoredPosition;
        homeSize = rect.sizeDelta;
        homeScale = rect.localScale;
        homeRotation = rect.localRotation;
        homeAnchorMin = rect.anchorMin;
        homeAnchorMax = rect.anchorMax;
        homePivot = rect.pivot;
        homeSibling = rect.GetSiblingIndex();
        homeRaycast = image.raycastTarget;
    }

    // Called once while input is locked, before the new challenge starts.
    public bool SetInitialPosition(Vector2 position)
    {
        if (rect == null || dragging || IsPlaced || rect.parent != homeParent) return false;
        rect.anchoredPosition = position;
        homePosition = position;
        return true;
    }

    public void ResetForNewChallenge()
    {
        CancelDrag();
        if (placementAnimation != null) StopCoroutine(placementAnimation);
        placementAnimation = null;
        IsPlaced = false;
        if (soilCover != null) soilCover.gameObject.SetActive(false);
        dropHandled = false;
        ReturnHome();
    }

    public bool AcceptsPointer(int id) => dragging && pointerId == id;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || dragging || IsPlaced ||
            manager == null || !manager.TryBeginDrag(this)) return;
        Vector3 pointer;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(manager.DragRoot,
                eventData.position, eventData.pressEventCamera, out pointer))
        {
            manager.ReleaseDrag(this);
            return;
        }
        pointerId = eventData.pointerId;
        dragging = true;
        dropHandled = false;
        grabOffset = rect.position - pointer;
        rect.SetParent(manager.DragRoot, true);
        rect.SetAsLastSibling();
        // Only the layer underneath can receive the drop while this sprite is moving.
        image.raycastTarget = false;
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (AcceptsPointer(eventData.pointerId) && !IsPlaced) MoveToPointer(eventData);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        Vector3 pointer;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(manager.DragRoot,
                eventData.position, eventData.pressEventCamera, out pointer))
            rect.position = pointer + grabOffset;
        manager.UpdateDragHover(this, eventData.position, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!AcceptsPointer(eventData.pointerId)) return;
        dragging = false;
        if (!IsPlaced)
        {
            ReturnHome();
            if (!dropHandled && manager != null) manager.ReportIncorrectDrop();
        }
        else image.raycastTarget = false;
        if (manager != null) manager.ReleaseDrag(this);
    }

    internal void MarkDropHandled() => dropHandled = true;

    internal void Place(RectTransform layer, Vector2 position)
    {
        if (IsPlaced) return;
        IsPlaced = true;
        dropHandled = true;
        rect.SetParent(layer, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = placedSize;
        rect.localRotation = Quaternion.identity;
        image.raycastTarget = false;
        ShowSoilCover();
        placementAnimation = StartCoroutine(AnimatePlacement());
    }

    private void ShowSoilCover()
    {
        if (soilCover == null)
        {
            var cover = new GameObject("BuriedLowerEdge", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cover.SetParent(rect, false); cover.anchorMin=Vector2.zero; cover.anchorMax=Vector2.one;
            cover.offsetMin=cover.offsetMax=Vector2.zero;
            soilCover=cover.GetComponent<Image>();
            soilCover.raycastTarget=false;
            soilCover.type=Image.Type.Filled; soilCover.fillMethod=Image.FillMethod.Vertical;
            soilCover.fillOrigin=(int)Image.OriginVertical.Bottom; soilCover.fillAmount=.23f;
            soilCover.color=new Color(.48f,.35f,.22f,.65f);
        }
        // Reuse the original sprite alpha so no rectangle covers transparent PNG margins.
        soilCover.sprite=image.sprite; soilCover.preserveAspect=image.preserveAspect;
        soilCover.gameObject.SetActive(true);
    }

    private IEnumerator AnimatePlacement()
    {
        const float duration = .18f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            rect.localScale = Vector3.one * Mathf.Lerp(.9f, 1, t);
            yield return null;
        }
        rect.localScale = Vector3.one;
        placementAnimation = null;
    }

    private void ReturnHome()
    {
        if (homeParent == null) return;
        rect.SetParent(homeParent, false);
        rect.anchorMin = homeAnchorMin;
        rect.anchorMax = homeAnchorMax;
        rect.pivot = homePivot;
        rect.anchoredPosition = homePosition;
        rect.sizeDelta = homeSize;
        rect.localScale = homeScale;
        rect.localRotation = homeRotation;
        rect.SetSiblingIndex(Mathf.Min(homeSibling, homeParent.childCount - 1));
        image.raycastTarget = homeRaycast;
    }

    public void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        if (!IsPlaced) ReturnHome();
        if (manager != null) manager.ReleaseDrag(this);
    }

    void OnDisable()
    {
        CancelDrag();
        if (placementAnimation != null)
        {
            StopCoroutine(placementAnimation);
            placementAnimation = null;
            if (rect != null && IsPlaced) rect.localScale = Vector3.one;
        }
    }
    void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
}
