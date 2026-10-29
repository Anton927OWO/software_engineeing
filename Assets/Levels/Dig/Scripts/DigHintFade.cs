using TMPro;
using UnityEngine;
[RequireComponent(typeof(TMP_Text))]
public sealed class DigHintFade : MonoBehaviour
{
    private TMP_Text label;
    private bool wanted;
    private float alpha;
    void Awake()
    {
        label = GetComponent<TMP_Text>();
        label.raycastTarget = false;
    }
    public static void SetVisible(TMP_Text text, bool visible)
    {
        if (text == null) return;
        DigHintFade fade = text.GetComponent<DigHintFade>();
        if (fade == null) fade = text.gameObject.AddComponent<DigHintFade>();
        fade.wanted = visible;
        if (visible && !text.gameObject.activeSelf) text.gameObject.SetActive(true);
    }
    void LateUpdate()
    {
        alpha = Mathf.MoveTowards(alpha, wanted ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);
        Color color = label.color;
        color.a = alpha;
        label.color = color;
        if (!wanted && alpha <= 0f) gameObject.SetActive(false);
    }
}
