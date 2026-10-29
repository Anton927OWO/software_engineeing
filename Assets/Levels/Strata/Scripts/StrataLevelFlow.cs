using System;
using NIUAdventure.Integration;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class StrataLevelResult
{
    public bool success;
    public float remainingSeconds;
    public int stageOneMistakes, stageTwoMistakes, stageOneMatched, stageTwoMatched;
    public int TotalMistakes => stageOneMistakes + stageTwoMistakes;
}

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(StrataLevelUI))]
public sealed class StrataLevelFlow : MonoBehaviour, ILevelHandoffSource
{
    public enum Screen { Introduction, StageOne, StageTwo, Paused, Success, Failure, Knowledge, Completed }
    [SerializeField] private StrataStageOneManager firstStage;
    [SerializeField] private StrataStageTwoManager secondStage;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMPro.TMP_FontAsset font;
    [SerializeField] private StrataLevelUI view;
    [SerializeField] private StrataLevelAudio levelAudio;
    [SerializeField, Min(1)] private float gameDuration = 60;
    [SerializeField] private string integrationLevelId="L_Strata";
    [SerializeField] private string rewardArtifactId="";
    [Header("關卡內事件：音效／知識卡／後續整合，不計算全組分數")]
    [SerializeField] private UnityEvent onChallengeStarted = new UnityEvent();
    [SerializeField] private UnityEvent onPaused = new UnityEvent();
    [SerializeField] private UnityEvent onResumed = new UnityEvent();
    [SerializeField] private UnityEvent onChallengeSucceeded = new UnityEvent();
    [SerializeField] private UnityEvent onChallengeFailed = new UnityEvent();
    [SerializeField] private UnityEvent onKnowledgeRequested = new UnityEvent();
    public event Action<StrataLevelResult> ChallengeEnded;
    public event Action<StrataLevelResult> LevelCompleted;
    public event Action<LevelHandoffResult> ResultReady;
    public LevelHandoffResult LastHandoffResult { get; private set; }
    public bool IsReady => initialized && isActiveAndEnabled;
    private string attemptId, attemptLevelId, attemptRewardId;
    public Screen CurrentScreen { get; private set; } = Screen.Introduction;
    public StrataLevelResult LastResult { get; private set; }
    public bool CanShowLayerHint => initialized && isActiveAndEnabled &&
        CurrentScreen==Screen.StageOne && firstStage!=null && firstStage.MatchedCount<3 &&
        clock.Remaining(Now)>0;
    public float RemainingSeconds => (float)clock.Remaining(Now);
    public int MistakeCount => (firstStage != null ? firstStage.MistakeCount : 0) +
        (secondStage != null ? secondStage.MistakeCount : 0);
    private readonly StrataChallengeClock clock = new StrataChallengeClock();
    private Screen resumeScreen;
    private bool initialized, resultPublished;
    private static double Now => Time.unscaledTimeAsDouble;
    private bool IsPlaying => CurrentScreen == Screen.StageOne || CurrentScreen == Screen.StageTwo;

    void Awake()
    {
        // Lock both stages before their Awake builds graphics; do not deactivate artifact objects.
        if (firstStage != null) { firstStage.InputGuard = CanUseInput; firstStage.SetInputEnabled(false); }
        if (secondStage != null) { secondStage.InputGuard = CanUseInput; secondStage.SetInputEnabled(false); }
        if (firstStage == null || secondStage == null || canvas == null || font == null || view == null)
        {
            Debug.LogError("StrataLevelFlow 缺少兩階段、Canvas、字型或UI引用。", this);
            enabled = false;
        }
    }
    void OnEnable()
    {
        if (firstStage != null) firstStage.SecondStageRequested += HandleSecondStageRequested;
        if (secondStage != null) secondStage.StageTwoCompleted += HandleStageTwoCompleted;
        if (firstStage != null) firstStage.OperationFeedback += HandleOperationFeedback;
        if (secondStage != null) secondStage.OperationFeedback += HandleOperationFeedback;
    }
    void Start()
    {
        if (!firstStage.IsInitialized || !secondStage.IsInitialized)
        {
            Debug.LogError("第三關兩階段尚未正確初始化，保持輸入鎖定。", this);
            enabled = false;
            return;
        }
        view.Build(canvas, font, this);
        initialized = true;
        PrepareIntroduction();
    }
    private void PrepareIntroduction()
    {
        CurrentScreen = Screen.Introduction;
        LastResult = null;
        LastHandoffResult=null;
        resultPublished=false;
        if(levelAudio!=null) levelAudio.RestartMusic();
        SetStageInput(false, false);
        clock.Reset(gameDuration);
        secondStage.ResetForNewChallenge();
        firstStage.ResetForNewChallenge();
        if (!firstStage.PrepareRandomizedLayout())
        {
            Debug.LogError("第三關文物初始排列失敗，保持輸入鎖定。", this);
            enabled = false;
            return;
        }
        view.ShowIntroduction();
        view.SetTime(RemainingSeconds);
    }
    public bool ConfigureIntegration(string levelId,string artifactRewardId)
    {
        if(CurrentScreen!=Screen.Introduction || string.IsNullOrWhiteSpace(levelId)) return false;
        integrationLevelId=levelId.Trim();
        rewardArtifactId=string.IsNullOrWhiteSpace(artifactRewardId) ? "" : artifactRewardId.Trim();
        return true;
    }
    public bool ResetForNewChallenge()
    {
        if(!IsReady) return false;
        PrepareIntroduction();
        return enabled && CurrentScreen==Screen.Introduction;
    }
    public void BeginChallenge()
    {
        if (!initialized || !isActiveAndEnabled || CurrentScreen != Screen.Introduction) return;
        attemptId=Guid.NewGuid().ToString("N");
        attemptLevelId=integrationLevelId; attemptRewardId=rewardArtifactId;
        CurrentScreen = Screen.StageOne;
        clock.Start(Now);
        SetStageInput(true, false);
        view.ShowRunning();
        onChallengeStarted?.Invoke();
    }
    // Used by both drag managers at the point of mutation, not just once per Update.
    public bool CanUseInput()
    {
        if (!initialized || !isActiveAndEnabled || !IsPlaying) return false;
        if (clock.Remaining(Now) <= 0) { Finish(false); return false; }
        return true;
    }
    void Update()
    {
        if (!initialized) return;
        if (IsPlaying && clock.Remaining(Now) <= 0) Finish(false);
        view.SetTime(RemainingSeconds);
    }
    void LateUpdate()
    {
        // StageTwo builds its own root; keep shared HUD and modal blockers above it.
        if (initialized && view != null)
        {
            // Recheck after EventSystem.Update: the third drop can open L4 this frame.
            view.SetLayerHintAvailability(CanShowLayerHint);
            view.BringToFront();
        }
    }
    private void HandleSecondStageRequested()
    {
        if (!CanUseInput() || !firstStage.IsComplete) return;
        CurrentScreen = Screen.StageTwo;
        // No clock reset: waiting at the transition button and second-stage feedback use the same deadline.
        SetStageInput(false, true);
        if(levelAudio!=null) levelAudio.PlayStageSwitch();
        secondStage.BeginStageTwo();
    }
    private void HandleStageTwoCompleted()
    {
        if (!CanUseInput() || !firstStage.IsComplete || firstStage.MatchedCount != 3 ||
            !secondStage.IsComplete || secondStage.CorrectCount != 3) return;
        Finish(true);
    }
    public void PauseGame()
    {
        if (!CanUseInput()) return;
        resumeScreen = CurrentScreen;
        clock.Pause(Now);
        CurrentScreen = Screen.Paused;
        SetStageInput(false, false);
        view.ShowPause();
        if(levelAudio!=null) levelAudio.PauseMusic();
        onPaused?.Invoke();
    }
    public void ResumeGame()
    {
        if (!initialized || !isActiveAndEnabled || CurrentScreen != Screen.Paused) return;
        if (clock.Remaining(Now) <= 0) { Finish(false); return; }
        CurrentScreen = resumeScreen;
        clock.Start(Now);
        SetStageInput(CurrentScreen == Screen.StageOne, CurrentScreen == Screen.StageTwo);
        view.ShowRunning();
        if(levelAudio!=null) levelAudio.ResumeMusic();
        onResumed?.Invoke();
    }
    private void Finish(bool success)
    {
        if (!initialized || (CurrentScreen != Screen.Paused && !IsPlaying)) return;
        clock.Pause(Now);
        success = success && clock.Remaining(Now) > 0 && firstStage.IsComplete && secondStage.IsComplete;
        CurrentScreen = success ? Screen.Success : Screen.Failure;
        SetStageInput(false, false);
        LastResult = new StrataLevelResult
        {
            success = success, remainingSeconds = RemainingSeconds,
            stageOneMistakes = firstStage.MistakeCount, stageTwoMistakes = secondStage.MistakeCount,
            stageOneMatched = firstStage.MatchedCount, stageTwoMatched = secondStage.CorrectCount
        };
        if(levelAudio!=null) levelAudio.PlayResult(success);
        if(success)
        {
            view.ShowResult(LastResult); // Show the frozen statistics before opening knowledge cards.
        }
        else { view.ShowResult(LastResult); PublishResult(); }
    }
    public void ContinueFromKnowledge()
    {
        if(!initialized || !isActiveAndEnabled || CurrentScreen!=Screen.Knowledge ||
            LastResult==null || !LastResult.success || resultPublished) return;
        CurrentScreen=Screen.Completed; // Keep the final knowledge page visible until the host continues.
        PublishResult();
    }
    private void PublishResult()
    {
        if(resultPublished || LastResult==null) return;
        resultPublished=true;
        var nativeResult=LastResult;
        var handoff=new LevelHandoffResult(attemptLevelId,attemptId,nativeResult.success,
            nativeResult.remainingSeconds,nativeResult.TotalMistakes,attemptRewardId,true);
        LastHandoffResult=handoff;
        ResultReady?.Invoke(handoff);
        ChallengeEnded?.Invoke(nativeResult);
        if(nativeResult.success) { LevelCompleted?.Invoke(nativeResult); onChallengeSucceeded?.Invoke(); }
        else onChallengeFailed?.Invoke();
    }
    public void PlayUIClick() { if(levelAudio!=null) levelAudio.PlayButton(); }
    private void HandleOperationFeedback(bool correct)
    { if(CanUseInput() && levelAudio!=null) levelAudio.PlayFeedback(correct); }

    public void RetryChallenge()
    {
        if (!initialized || !isActiveAndEnabled ||
            (CurrentScreen != Screen.Success && CurrentScreen != Screen.Failure)) return;
        // Reset in place: no Build Profile dependency, no duplicate UI, no scene reload.
        ResetForNewChallenge();
    }
    public void RequestKnowledge()
    {
        if (!initialized || !isActiveAndEnabled || CurrentScreen!=Screen.Success ||
            LastResult==null || !LastResult.success || resultPublished) return;
        CurrentScreen=Screen.Knowledge;
        view.ShowKnowledge();
        onKnowledgeRequested?.Invoke();
    }
    private void SetStageInput(bool stageOne, bool stageTwo)
    {
        if (firstStage != null) firstStage.SetInputEnabled(stageOne);
        if (secondStage != null) secondStage.SetInputEnabled(stageTwo);
    }
    void OnApplicationPause(bool paused) { if (paused && IsPlaying) PauseGame(); }
    void OnApplicationFocus(bool focused) { if (!focused && IsPlaying) PauseGame(); }
    void OnDisable()
    {
        if (firstStage != null) firstStage.SecondStageRequested -= HandleSecondStageRequested;
        if (secondStage != null) secondStage.StageTwoCompleted -= HandleStageTwoCompleted;
        if (firstStage != null) firstStage.OperationFeedback -= HandleOperationFeedback;
        if (secondStage != null) secondStage.OperationFeedback -= HandleOperationFeedback;
        if (initialized && IsPlaying)
        {
            resumeScreen = CurrentScreen;
            clock.Pause(Now);
            CurrentScreen = Screen.Paused;
            if(levelAudio!=null) levelAudio.PauseMusic();
            if (view != null && view.IsBuilt) view.ShowPause();
        }
        SetStageInput(false, false);
    }
}
