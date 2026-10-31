using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// 校園地圖上的一個地點：點擊後打開知識卡，關閉知識卡時在旁邊放出對應的書。
public class PrologueLandmark : MonoBehaviour, IPointerClickHandler
{
    [Header("地點資料")]
    [SerializeField] private string displayName = "地點";
    [SerializeField] private string cardTitle = "知識卡";
    [SerializeField, TextArea(3, 8)] private string cardBody = "";

    [Header("藏在這裡的書（BookDrag 物件名稱）")]
    [SerializeField] private string bookName = "Book1";

    [Header("外觀")]
    [SerializeField] private Image pin;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private GameObject foundMark;
    [SerializeField] private Color hiddenColor = new Color(0.82f, 0.32f, 0.22f);
    [SerializeField] private Color foundColor = new Color(0.30f, 0.55f, 0.36f);

    [SerializeField] private PrologueManager prologueManager;

    public string DisplayName => displayName;
    public string CardTitle => cardTitle;
    public string CardBody => cardBody;
    public string BookName => bookName;
    public bool Discovered { get; private set; }

    private Vector3 baseScale = Vector3.one;
    private bool initialized;

    void Awake() { Initialize(); }
    private void Initialize()
    {
        if (initialized) return;
        if (pin != null) baseScale = pin.rectTransform.localScale;
        initialized = true;
    }

    public void ResetForNewChallenge()
    {
        Initialize();
        Discovered = false;
        Refresh();
    }

    public void MarkDiscovered()
    {
        Discovered = true;
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || prologueManager == null) return;
        prologueManager.OnLandmarkClicked(this);
    }

    // 未探索的地點會輕微脈動，提示可以點擊；暫停時 timeScale 為 0，脈動也跟著停。
    void Update()
    {
        if (pin == null || Discovered) return;
        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
        pin.rectTransform.localScale = baseScale * pulse;
    }

    private void Refresh()
    {
        if (pin != null)
        {
            pin.color = Discovered ? foundColor : hiddenColor;
            pin.rectTransform.localScale = baseScale;
        }
        if (nameLabel != null) nameLabel.text = displayName;
        if (foundMark != null) foundMark.SetActive(Discovered);
    }
}
