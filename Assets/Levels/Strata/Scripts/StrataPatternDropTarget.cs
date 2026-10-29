using UnityEngine;
using UnityEngine.EventSystems;

public sealed class StrataPatternDropTarget : MonoBehaviour, IDropHandler
{
    private StrataStageTwoManager owner;
    public void Initialize(StrataStageTwoManager manager) => owner = manager;
    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || eventData.pointerDrag == null) return;
        var option = eventData.pointerDrag.GetComponent<StrataPatternOptionDrag>();
        if (option != null && option.Owner == owner && option.AcceptsPointer(eventData.pointerId))
            owner.TryMatch(option);
    }
}
