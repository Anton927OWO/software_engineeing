using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class StrataLayerDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private StrataStageOneManager manager;
    [SerializeField, Range(1, 4)] private int layerNumber = 1;
    [SerializeField] private Vector2 placementPosition = new Vector2(100, 0);
    public StrataStageOneManager Owner => manager;
    public int LayerNumber => layerNumber;
    public Vector2 PlacementPosition => placementPosition;
    public RectTransform Rect => (RectTransform)transform;

    private GameObject highlight;
    internal void SetHighlight(bool visible)
    {
        if (visible && highlight == null)
        {
            var frame = new GameObject("DragHoverBorder", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(transform, false);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            highlight = frame.gameObject;
            Edge(frame, "Top", new Vector2(0,1), Vector2.one, new Vector2(0,4));
            Edge(frame, "Bottom", Vector2.zero, new Vector2(1,0), new Vector2(0,4));
            Edge(frame, "Left", Vector2.zero, new Vector2(0,1), new Vector2(4,0));
            Edge(frame, "Right", new Vector2(1,0), Vector2.one, new Vector2(4,0));
        }
        if (highlight != null) highlight.SetActive(visible);
    }
    private static void Edge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin=min; rect.anchorMax=max;
        rect.anchoredPosition=Vector2.zero; rect.sizeDelta=size;
        var image=rect.GetComponent<Image>();
        image.color=new Color(1f,.87f,.48f,.6f); image.raycastTarget=false;
    }
    void OnDisable() { SetHighlight(false); }

    public void OnDrop(PointerEventData eventData)
    {
        SetHighlight(false);
        if (manager == null || eventData.pointerDrag == null) return;
        StrataArtifactDrag artifact = eventData.pointerDrag.GetComponent<StrataArtifactDrag>();
        if (artifact == null || artifact.Owner != manager ||
            !artifact.AcceptsPointer(eventData.pointerId)) return;
        manager.TryPlace(artifact, this);
    }
}
