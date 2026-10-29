using UnityEngine;
using UnityEngine.InputSystem;

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

        Vector2 pointerPosition = default;
        bool hasTouch = false;
        var touchscreen = Touchscreen.current;

        // 觸控優先，每幀只選一個接觸點；此處只更新游標，不另行觸發刷土。
        if (touchscreen != null && touchscreen.enabled)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                pointerPosition = touch.position.ReadValue();
                hasTouch = true;
                break;
            }
        }

        if (!hasTouch)
        {
            var mouse = Mouse.current;

            // 保留桌面滑鼠懸停顯示；行動裝置無觸控時隱藏游標。
            if (!Application.isMobilePlatform && mouse != null && mouse.enabled)
            {
                pointerPosition = mouse.position.ReadValue();
            }
            else
            {
                ShowBrush(false);
                return;
            }
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