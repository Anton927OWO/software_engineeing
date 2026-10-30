
using UnityEngine;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("暫停選單")]
    [SerializeField] private GameObject pausePanel;

    [Header("教學管理器")]
    [SerializeField] private PrologueManager prologueManager;

    [Header("教學提示文字")]
    [SerializeField] private TMP_Text tutorialText;

    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;

        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // 暫停遊戲
    public void PauseGame()
    {
        if (isPaused || prologueManager==null || !prologueManager.IsChallengeActive || prologueManager.HasCompleted)
            return;

        isPaused = true;
        prologueManager.SetPaused(true);

        // 隱藏原本的教學提示
        if (prologueManager != null)
        {
            prologueManager.SetTutorialVisible(false);
        }
        else if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(false);
        }

        // 顯示暫停選單
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;

        Debug.Log("遊戲已暫停");
    }

    // 恢復遊戲
    public void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;

        Time.timeScale = 1f;
        if(prologueManager!=null) prologueManager.SetPaused(false);

        // 關閉暫停選單
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 恢復原本的教學提示
        if (prologueManager != null)
        {
            prologueManager.SetTutorialVisible(true);
        }
        else if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(true);
        }

        Debug.Log("遊戲已恢復");

    }

    public void ResetForNewChallenge()
    {
        isPaused=false;
        Time.timeScale=1f;
        if(pausePanel!=null) pausePanel.SetActive(false);
    }
    void OnApplicationFocus(bool focused) { if(!focused) PauseGame(); }
    void OnApplicationPause(bool value) { if(value) PauseGame(); }

    void OnDisable()
    {
        ResetForNewChallenge();
        if(prologueManager!=null) prologueManager.SetPaused(false);
        Time.timeScale = 1f;

        if (prologueManager != null)
        {
            prologueManager.SetTutorialVisible(true);
        }
        else if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(true);
        }
    }
}
