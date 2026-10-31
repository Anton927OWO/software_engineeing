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

    [Header("校園地圖")]
    [SerializeField] private GameObject campusMap;
    [SerializeField] private PrologueLandmark[] landmarks;
    [SerializeField] private PrologueKnowledgeCard knowledgeCard;

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
        !completionPublished && Time.timeScale>0 && openLandmark==null &&
        (introPanel==null || !introPanel.activeSelf) && (completePanel==null || !completePanel.activeSelf);
    private bool prepared, challengeActive, paused, completionPublished, continuePublished;
    private string attemptId, attemptLevelId;

    // 三本書分別藏在地圖的三個地點，順序不限：
    // 點擊地點 → 閱讀知識卡 → 書本出現在地點旁 → 拖進知識之林的書箱。
    // CurrentStep：0～3 為已收集的書本數，5 為序章完成（保留既有對外步驟值）。
    private int currentStep = 0;

    private int collectedBooks = 0;
    private PrologueLandmark openLandmark;

    public int CurrentStep
    {
        get { return currentStep; }
    }
    public int CollectedBooks => collectedBooks;
    public bool IsKnowledgeCardOpen => openLandmark!=null;

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
        if(landmarks==null || landmarks.Length!=books.Length || Array.Exists(landmarks,landmark=>landmark==null) ||
            knowledgeCard==null)
        { Debug.LogError("序章需指定三個地圖地點及知識卡，請執行「Tracking NIU/序章/建立校園地圖版」。",this); return false; }
        challengeActive=false; paused=false;
        completionPublished=false; continuePublished=false;
        currentStep=0; collectedBooks=0; LastHandoffResult=null; openLandmark=null;
        attemptId=Guid.NewGuid().ToString("N");
        pauseMenu.ResetForNewChallenge();
        foreach(var book in books) { book.ResetForNewChallenge(); book.gameObject.SetActive(false); }
        foreach(var landmark in landmarks) landmark.ResetForNewChallenge();
        knowledgeCard.Hide();
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

    // 點擊地圖上的地點：打開知識卡
    public void OnLandmarkClicked(PrologueLandmark landmark)
    {
        if (!IsGameplayInputEnabled || landmark == null || landmark.Discovered ||
            Array.IndexOf(landmarks, landmark) < 0)
            return;

        foreach (var book in books) book.CancelDrag();
        openLandmark = landmark;
        var hiddenBook = FindBook(landmark.BookName);
        var bookImage = hiddenBook != null ? hiddenBook.GetComponent<Image>() : null;
        knowledgeCard.Show(landmark, bookImage != null ? bookImage.sprite : null);

        // 播放點擊音效
        if (prologueAudio != null)
            prologueAudio.PlayClickSound();

        UpdateUI(); SetTutorialVisible(false);
    }

    // 知識卡的「收下書本」：書本出現在地點旁，可以拖曳
    public void OnKnowledgeCardClosed()
    {
        if (openLandmark == null || paused || completionPublished) return;
        var landmark = openLandmark;
        openLandmark = null;
        knowledgeCard.Hide();
        landmark.MarkDiscovered();
        var book = FindBook(landmark.BookName);
        if (book != null) book.gameObject.SetActive(true);

        if (prologueAudio != null)
            prologueAudio.PlayClickSound();

        UpdateUI(); SetTutorialVisible(true);
    }

    // 已在地圖上找到、還沒放進書箱的書都可以拖曳
    public bool CanDragBook(string bookName)
    {
        if(!IsGameplayInputEnabled) return false;
        var landmark = FindLandmark(bookName);
        return landmark != null && landmark.Discovered;
    }

    private PrologueLandmark FindLandmark(string bookName)
    {
        foreach (var landmark in landmarks)
            if (landmark != null && landmark.BookName == bookName) return landmark;
        return null;
    }

    private BookDrag FindBook(string bookName)
    {
        foreach (var book in books)
            if (book != null && book.name == bookName) return book;
        return null;
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

        currentStep = collectedBooks;

        UpdateUI();
        if (collectedBooks == 3) CompleteChallenge();

        return true;
    }

    // Compatibility entry point: pausing is no longer a completion requirement.
    public void OnPauseCompleted() { CompleteChallenge(); }

    private void CompleteChallenge()
    {
        if (!challengeActive || paused || collectedBooks!=3 || completionPublished || currentStep != 3)
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

        if (campusMap != null)
            campusMap.SetActive(visible);

        if (!visible && knowledgeCard != null)
            knowledgeCard.Hide();
    }

    public void SetTutorialVisible(bool visible)
    {
        visible = visible && currentStep != 5 && openLandmark == null &&
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

        if (currentStep == 5)
        {
            tutorialText.text = "序章完成";
            return;
        }
        if (collectedBooks == 3)
        {
            tutorialText.text = "三本書已收齊！";
            return;
        }

        // 書本只能在地點被探索後收集，所以「已探索 − 已收集」就是正等著被拖進書箱的書。
        int discovered = 0;
        foreach (var landmark in landmarks)
            if (landmark != null && landmark.Discovered) discovered++;
        int waiting = discovered - collectedBooks;

        if (waiting > 0)
            tutorialText.text = "把書本拖進「知識之林」的書箱\n<size=26>書箱在生資大樓旁的知識之林</size>";
        else if (discovered == 0)
            tutorialText.text = "點擊地圖上閃爍的地點\n<size=26>三本書散落在校園各處，順序不限</size>";
        else
            tutorialText.text = "還有 " + (3 - collectedBooks) + " 本書，繼續探索其他地點\n<size=26>點擊閃爍的地點</size>";
    }
}