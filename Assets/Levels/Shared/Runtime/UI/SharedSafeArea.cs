using UnityEngine;

namespace XunJiYiDa.Levels.Shared
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SharedSafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect previous;
        private Vector2Int previousSize;
        private void Awake() => rect = (RectTransform)transform;
        private void OnEnable() => Apply();
        private void Update()
        {
            if (previous != Screen.safeArea || previousSize.x != Screen.width || previousSize.y != Screen.height)
                Apply();
        }
        private void Apply()
        {
            if (rect == null) rect = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            previous = Screen.safeArea;
            previousSize = new Vector2Int(Screen.width, Screen.height);
            rect.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
            rect.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
