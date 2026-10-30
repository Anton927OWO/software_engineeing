using System;
using NIUAdventure.Integration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class PrologueManager : MonoBehaviour, ILevelHandoffSource
{
    [Header("教學介面")]
    [SerializeField] private TMP_Text tutorialText;

    [SerializeField] private TMP_Text collectionText;

    [SerializeField] private GameObject tutorialBackdrop;
    [SerializeField] private GameObject collectionBackdrop;

    [Header("開場畫面")]
    [SerializeField] private GameObject introPanel;

    [Header("完成畫面")]
    [SerializeField] private GameObject completePanel;

    [Header("遊戲中的書本與書箱")]
    [SerializeField] private Image[] gameplayObjectImages;

    [Header("序章音效")]
    [SerializeField] private PrologueAudio prologueAudio;

    [Header("完整重置與主流程接口")]
    [SerializeField] private BookDrag[] books;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private string integrationLevelId = "L_Prologue";
    [SerializeField] private UnityEvent onPrologueCompleted = new UnityEvent();
    [SerializeField] private UnityEvent onContinueRequested = new UnityEvent();
    public event Action<LevelHandoffResult> PrologueCompleted;
    public event Action<LevelHandoffResult> ResultReady;
    public event Action ContinueRequested;
    public LevelHandoffResult LastHandoffResult { get; private set; }
    public bool IsReady => prepared && isActiveAndEnabled;
    public bool HasCompleted => completionPublished;
    public bool IsChallengeActive => challengeActive;
    public bool IsPaused => paused;
    public bool IsGameplayInputEnabled => isActiveAndEnabled && challengeActive && !paused &&
        !completionPublished && Time.timeScale>0 &&
        (introPanel==null || !introPanel.activeSelf) && (completePanel==null || !completePanel.activeSelf);
    private bool prepared, challengeActive, paused, completionPublished, continuePublished;
    private string attemptId, attemptLevelId;

    // 0：點擊紅色書本
    // 1：拖曳紅色書本
    // 2：拖曳藍色書本
    // 3：拖曳黃色書本
    // 4：三書已收齊，立即提交完成
    // 5：序章完成（保留既有對外步驟值）
    private int currentStep = 0;

    private int collectedBooks = 0;

    public int CurrentStep
    {
        get { return currentStep; }
    }

    void Start() { if(!prepared) ResetForNewChallenge(); }

    public bool ConfigureIntegration(string levelId, string rewardArtifactId)
    {
        if(challengeActive || completionPublished || string.IsNullOrWhiteSpace(levelId) ||
            !string.IsNullOrWhiteSpace(rewardArtifactId)) return false; // Tutorial never grants an artifact.
        integrationLevelId=levelId.Trim();
        return true;
    }
    public bool ResetForNewChallenge()
    {
        if(books==null || books.Length!=3 || Array.Exists(books,book=>book==null) || pauseMenu==null)
        { Debug.LogError("序章完整重置需指定三本書及 PauseMenu。",this); return false; }
        challengeActive=false; paused=false;
        completionPublished=false; continuePublished=false;
        currentStep=0; collectedBooks=0; LastHandoffResult=null;
        attemptId=Guid.NewGuid().ToString("N");
        pauseMenu.ResetForNewChallenge();
        foreach(var book in books) book.ResetForNewChallenge();
        if(introPanel!=null) introPanel.SetActive(true);
        if(completePanel!=null) completePanel.SetActive(false);
        UpdateUI(); SetGameplayUIVisible(false);
        if(prologueAudio!=null) prologueAudio.RestartMusic();
        prepared=true;
        return true;
    }
    public void BeginChallenge() { StartChallenge(); }
    public void StartChallenge()
    {
        if(challengeActive && !completionPublished) return;
        if(!ResetForNewChallenge()) return;
        if(introPanel!=null) introPanel.SetActive(false);
        attemptLevelId=integrationLevelId;
        challengeActive=true;
        UpdateUI(); SetGameplayUIVisible(true);
    }
    public void PauseGame() { if(pauseMenu!=null) pauseMenu.PauseGame(); }
    public void ResumeGame() { if(pauseMenu!=null) pauseMenu.ResumeGame(); }
    public void SetPaused(bool value)
    {
        if(!challengeActive || completionPublished) return;
        paused=value;
        if(value)
        {
            foreach(var book in books) book.CancelDrag();
        }
        if(prologueAudio!=null) prologueAudio.SetPaused(value);
    }

    // 點擊紅色書本
    public void OnBookClicked()
    {
        if (!IsGameplayInputEnabled || currentStep != 0)
            return;

        currentStep = 1;

        // 播放點擊音效
        if (prologueAudio != null)
            prologueAudio.PlayClickSound();

        UpdateUI();

        Debug.Log("完成點擊教學！");
    }

    // 判斷目前哪本書可以拖曳
    public bool CanDragBook(string bookName)
    {
        if(!IsGameplayInputEnabled) return false;
        switch (currentStep)
        {
            case 1:
                return bookName == "Book1";

            case 2:
                return bookName == "Book2";

            case 3:
                return bookName == "Book3";

            default:
                return false;
        }
    }

    // 書本成功放入書箱
    public bool OnBookPlaced(string bookName)
    {
        if (!CanDragBook(bookName))
            return false;

        collectedBooks++;

        // 播放收集音效
        if (prologueAudio != null)
            prologueAudio.PlayCollectSound();

        Debug.Log(
            bookName + " 收集成功：" +
            collectedBooks + " / 3"
        );

        // 前往下一階段
        currentStep++;

        UpdateUI();
        if (collectedBooks == 3) CompleteChallenge();

        return true;
    }

    // Compatibility entry point: pausing is no longer a completion requirement.
    public void OnPauseCompleted() { CompleteChallenge(); }

    private void CompleteChallenge()
    {
        if (!challengeActive || paused || collectedBooks!=3 || completionPublished || currentStep != 4)
            return;

        currentStep = 5;
        completionPublished=true;
        challengeActive=false;
        var result=new LevelHandoffResult(attemptLevelId,attemptId,true,null,null,null,false);
        LastHandoffResult=result;
        UpdateUI();

        if (completePanel != null)
            completePanel.SetActive(true);

        SetGameplayUIVisible(false);

        // 播放完成音效
        if (prologueAudio != null)
            prologueAudio.PlayCompleteSound();

        if(prologueAudio!=null) prologueAudio.StopMusic();
        PrologueCompleted?.Invoke(result);
        onPrologueCompleted?.Invoke();
        ResultReady?.Invoke(result);
        Debug.Log("序章教學完成！");
    }

    public void RequestContinue()
    {
        if(!completionPublished || continuePublished) return;
        continuePublished=true;
        if(prologueAudio!=null) prologueAudio.PlayClickSound();
        ContinueRequested?.Invoke();
        onContinueRequested?.Invoke();
    }

    // 控制遊戲中的 UI 顯示
    private void SetGameplayUIVisible(bool visible)
    {
        // Hide only graphics: BookDrag.Start still records the original position,
        // and collected books keep their existing inactive GameObject state.
        if (gameplayObjectImages != null)
        {
            foreach (Image image in gameplayObjectImages)
                if (image != null) image.enabled = visible;
        }

        SetTutorialVisible(visible);

        if (collectionText != null)
            collectionText.gameObject.SetActive(visible);

        if (collectionBackdrop != null)
            collectionBackdrop.SetActive(visible);
    }

    public void SetTutorialVisible(bool visible)
    {
        visible = visible && currentStep != 5 &&
            (introPanel == null || !introPanel.activeSelf) &&
            (completePanel == null || !completePanel.activeSelf);

        if (tutorialText != null)
            tutorialText.gameObject.SetActive(visible);

        if (tutorialBackdrop != null)
            tutorialBackdrop.SetActive(visible);
    }

    // 更新所有教學文字
    private void UpdateUI()
    {
        if (collectionText != null)
        {
            collectionText.text =
                "書本 " + collectedBooks + " / 3";
        }

        if (tutorialText == null)
            return;

        switch (currentStep)
        {
            case 0:
                tutorialText.text = "先點擊紅色書本\n<size=26>收集順序：紅 → 藍 → 黃</size>";
                break;

            case 1:
                tutorialText.text = "將紅色書本拖入書箱\n<size=26>收集順序：紅 → 藍 → 黃</size>";
                break;

            case 2:
                tutorialText.text = "將藍色書本拖入書箱\n<size=26>收集順序：紅 → 藍 → 黃</size>";
                break;

            case 3:
                tutorialText.text = "將黃色書本拖入書箱\n<size=26>收集順序：紅 → 藍 → 黃</size>";
                break;

            case 4:
                tutorialText.text = "三本書已收齊！";
                break;

            case 5:
                tutorialText.text = "序章完成";
                break;
        }
    }
}