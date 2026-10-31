using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 點擊地點後跳出的知識卡；按「收下書本」關閉，並通知 PrologueManager 放出書本。
public class PrologueKnowledgeCard : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image bookIcon;
    [SerializeField] private PrologueManager prologueManager;

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show(PrologueLandmark landmark, Sprite bookSprite)
    {
        if (panel == null || landmark == null) return;
        if (bookIcon != null)
        {
            bookIcon.sprite = bookSprite;
            bookIcon.enabled = bookSprite != null;
        }
        if (locationText != null) locationText.text = landmark.DisplayName;
        if (titleText != null) titleText.text = landmark.CardTitle;
        if (bodyText != null) bodyText.text = landmark.CardBody;
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // 「收下書本」按鈕
    public void OnConfirm()
    {
        if (prologueManager != null) prologueManager.OnKnowledgeCardClosed();
    }
}
