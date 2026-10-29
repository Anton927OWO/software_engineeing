using UnityEngine;

public class BrushCursorController : MonoBehaviour
{
    [Header("UI 物件")]
    [SerializeField] private RectTransform brushCursor;
    [SerializeField] private RectTransform digArea;
    [SerializeField] private Canvas canvas;

    [Header("毛刷位置調整")]
    [SerializeField] private Vector2 cursorOffset =
        new Vector2(65f, 35f);

    [Header("游標設定")]
    [SerializeField] private bool hideSystemCursor = false;

    private RectTransform canvasRect;

    void Start()
    {
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (brushCursor != null)
            brushCursor.gameObject.SetActive(false);
    }

    void Update()
    {
        if (brushCursor == null ||
            digArea == null ||
            canvasRect == null)
        {
            return;
        }

        Vector2 pointerPosition;

        // 手機觸控
        if (Input.touchCount > 0)
        {
            pointerPosition = Input.GetTouch(0).position;
        }
        // 電腦滑鼠
        else if (!Application.isMobilePlatform)
        {
            pointerPosition = Input.mousePosition;
        }
        else
        {
            ShowBrush(false);
            return;
        }

        // 判斷滑鼠或手指是否在考古區域內
        bool inside =
            RectTransformUtility.RectangleContainsScreenPoint(
                digArea,
                pointerPosition,
                null
            );

        ShowBrush(inside);

        if (!inside)
            return;

        Vector2 localPosition;

        bool success =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                pointerPosition,
                null,
                out localPosition
            );

        if (!success)
            return;

        // 將毛刷移到滑鼠或手指附近
        brushCursor.anchoredPosition =
            localPosition + cursorOffset;
    }

    private void ShowBrush(bool visible)
    {
        if (brushCursor != null &&
            brushCursor.gameObject.activeSelf != visible)
        {
            brushCursor.gameObject.SetActive(visible);
        }

        // 可選擇隱藏電腦原本的滑鼠游標
        if (hideSystemCursor && !Application.isMobilePlatform)
        {
            Cursor.visible = !visible;
        }
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }
}