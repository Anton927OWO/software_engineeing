using UnityEngine;
using UnityEngine.EventSystems;

public class BookDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("書箱")]
    [SerializeField] private RectTransform bookBox;
    [SerializeField] private PrologueManager prologueManager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startPosition;
    private Vector3 startScale;
    private bool initialized, collected, dragging;
    private int pointerId;

    void Awake() { Initialize(); }
    private void Initialize()
    {
        if (initialized) return;
        rectTransform=GetComponent<RectTransform>();
        canvas=GetComponentInParent<Canvas>();
        startPosition=rectTransform.anchoredPosition;
        startScale=rectTransform.localScale;
        initialized=true;
    }
    public void ResetForNewChallenge()
    {
        Initialize();
        dragging=false; collected=false;
        ReturnToStart();
        gameObject.SetActive(true);
    }
    public void CancelDrag()
    {
        if (!initialized || !dragging) return;
        dragging=false;
        if (!collected) ReturnToStart();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        Initialize();
        if (dragging || eventData.button!=PointerEventData.InputButton.Left || !CanDrag()) return;
        dragging=true; pointerId=eventData.pointerId;
        rectTransform.localScale=startScale*1.1f;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || pointerId!=eventData.pointerId) return;
        if (!CanDrag()) { CancelDrag(); return; }
        rectTransform.anchoredPosition+=eventData.delta/Mathf.Max(.01f,canvas.scaleFactor);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || pointerId!=eventData.pointerId) return;
        dragging=false;
        rectTransform.localScale=startScale;
        if (!CanDrag()) { ReturnToStart(); return; }
        if (bookBox==null) { Debug.LogWarning("尚未指定 BookBox！",this); ReturnToStart(); return; }
        Vector2 screenPosition=RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera,rectTransform.position);
        bool isInside=RectTransformUtility.RectangleContainsScreenPoint(bookBox,screenPosition,eventData.pressEventCamera);
        if (isInside && prologueManager.OnBookPlaced(gameObject.name))
        {
            collected=true;
            gameObject.SetActive(false);
        }
        else ReturnToStart();
    }
    private bool CanDrag()
    {
        return !collected && canvas!=null && prologueManager!=null &&
            prologueManager.IsGameplayInputEnabled && prologueManager.CanDragBook(gameObject.name);
    }
    private void ReturnToStart()
    {
        rectTransform.anchoredPosition=startPosition;
        rectTransform.localScale=startScale;
    }
    void OnDisable() { CancelDrag(); }
    void OnApplicationFocus(bool focused) { if(!focused) CancelDrag(); }
    void OnApplicationPause(bool paused) { if(paused) CancelDrag(); }
}
