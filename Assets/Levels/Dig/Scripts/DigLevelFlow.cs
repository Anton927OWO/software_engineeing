using System;
using NIUAdventure.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(DigLevelUI), typeof(DigLevelAudio))]
public sealed class DigLevelFlow : MonoBehaviour, ILevelHandoffSource
{
    public enum Screen { Introduction, Playing, Paused, Success, Failure, Knowledge, Completed, ResolvingSuccess }
    [SerializeField] private DigManager manager;
    [SerializeField] private SoilBrush soilBrush;
    [SerializeField] private BrushCursorController brushCursorController;
    [SerializeField] private RectTransform brushCursor;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private DigArtifactCatalog artifactCatalog;
    [SerializeField] private ArtifactSpawner artifactSpawner;
    [SerializeField] private string integrationLevelId="L_Dig";
    [SerializeField] private string rewardArtifactId=""; // Supplied by the integration owner, never invented here.
    [Header("整合入口：不自行換關、不計算全組分數")]
    [SerializeField] private UnityEvent onLevelCompleted = new UnityEvent();
    public event Action<DigLevelResult> LevelCompleted;
    public event Action<LevelHandoffResult> ResultReady;
    public LevelHandoffResult LastHandoffResult { get; private set; }
    public bool IsReady => isActiveAndEnabled && manager!=null && manager.IsInitialized &&
        soilBrush!=null && soilBrush.CanResetSoil;
    private bool resultHandled, integrationPublished;
    private string attemptId, attemptLevelId, attemptRewardId;
    public Screen CurrentScreen { get; private set; }
    public DigLevelResult LastResult { get; private set; }
    public DigArtifactCatalog ArtifactCatalog => artifactCatalog;
    private DigLevelUI view;
    private DigLevelAudio sound;
    private Image soilImage;
    private bool submitted;
    [Header("成功畫面過渡（僅 UI）")]
    [SerializeField, Min(0f)] private float successPanelDelay = 0.5f;
    private Coroutine successRoutine;
    private int successTransitionVersion;

    void Awake()
    {
        view = GetComponent<DigLevelUI>();
        sound = GetComponent<DigLevelAudio>();
        if (manager == null || soilBrush == null || canvas == null || font == null || artifactCatalog == null)
        {
            Debug.LogError("DigLevelFlow 缺少關卡引用，請檢查 L_Dig 的 DigLevelFlow 物件。", this);
            enabled = false;
            return;
        }
        manager.SetGameplayActive(false);
        soilImage = soilBrush.GetComponent<Image>();
        soilImage.raycastTarget = false;
        SetCursor(false);
        view.Build(canvas, font, manager, artifactCatalog, sound, this);
        manager.SetWarningLabel(view.WarningLabel);
        manager.ChallengeEnded += HandleResult;
        manager.OnArtifactFirstSeen += HandleDiscovery;
        manager.ArtifactDamaged += HandleDamage;
        manager.ArtifactExcavated += HandleExcavated;
        soilBrush.OnBrushMotion += HandleBrushMotion;
        soilBrush.OnBrushReleased += StopBrushAudio;
        CurrentScreen = Screen.Introduction;
    }
    void Start()
    {
        if (enabled) sound.StartMusic();
    }
    public bool ConfigureIntegration(string levelId,string artifactRewardId)
    {
        if(CurrentScreen!=Screen.Introduction || string.IsNullOrWhiteSpace(levelId)) return false;
        integrationLevelId=levelId.Trim();
        rewardArtifactId=string.IsNullOrWhiteSpace(artifactRewardId) ? "" : artifactRewardId.Trim();
        return true;
    }
    private LevelHandoffResult CaptureHandoff(DigLevelResult result)
    {
        if(integrationPublished) return null;
        integrationPublished=true;
        LastHandoffResult=new LevelHandoffResult(attemptLevelId,attemptId,result.success,
            result.remainingSeconds,result.damageCount,attemptRewardId,true);
        return LastHandoffResult;
    }
    public void BeginChallenge()
    {
        if (CurrentScreen != Screen.Introduction || !manager.IsInitialized) return;
        attemptId=Guid.NewGuid().ToString("N");
        attemptLevelId=integrationLevelId; attemptRewardId=rewardArtifactId;
        LastHandoffResult=null; integrationPublished=false;
        CurrentScreen = Screen.Playing;
        view.ShowPlaying();
        manager.SetGameplayActive(true);
        soilBrush.enabled = true;
        soilImage.raycastTarget = true;
        SetCursor(true);
    }
    public void PauseGame()
    {
        if (CurrentScreen == Screen.ResolvingSuccess)
        {
            CancelSuccessTransition();
            sound.SetPaused(true);
            CurrentScreen = Screen.Paused;
            view.ShowPause();
            return;
        }
        if (CurrentScreen != Screen.Playing || manager.HasEnded) return;
        manager.SetGameplayActive(false);
        soilBrush.enabled = false;
        soilImage.raycastTarget = false;
        SetCursor(false);
        sound.SetPaused(true);
        CurrentScreen = Screen.Paused;
        view.ShowPause();
    }
    public void ResumeGame()
    {
        if (CurrentScreen != Screen.Paused) return;
        if (resultHandled && LastResult != null && LastResult.success)
        {
            sound.SetPaused(false);
            CurrentScreen = Screen.ResolvingSuccess;
            view.ShowPlaying();
            view.StopGameplayNotices();
            BeginSuccessTransition(LastResult);
            return; // Never re-enable brushing after success.
        }
        manager.SetGameplayActive(true);
        soilBrush.enabled = true;
        soilImage.raycastTarget = true;
        SetCursor(true);
        sound.SetPaused(false);
        CurrentScreen = Screen.Playing;
        view.ShowPlaying();
    }
    private void SetCursor(bool visible)
    {
        if (brushCursorController != null) brushCursorController.enabled = visible;
        if (!visible && brushCursor != null) brushCursor.gameObject.SetActive(false);
        if (!visible) Cursor.visible = true;
    }
    private void HandleResult(DigLevelResult result)
    {
        if(resultHandled || result==null) return;
        resultHandled=true;
        LastResult = result;
        // DigManager has already committed the outcome and stopped its timer/damage updates.
        manager.SetGameplayActive(false);
        soilBrush.enabled = false;
        soilImage.raycastTarget = false;
        view.StopGameplayNotices();
        SetCursor(false);
        sound.StopBrush();
        sound.StopMusic();
        if (result.success)
        {
            CurrentScreen = Screen.ResolvingSuccess;
            sound.Result(true); // Play once immediately; the UI delay never waits for this clip.
            BeginSuccessTransition(result);
        }
        else
        {
            CurrentScreen = Screen.Failure;
            sound.Result(false);
            view.ShowResult(result);
            var handoff=CaptureHandoff(result);
            if(handoff!=null) ResultReady?.Invoke(handoff);
        }
    }
    private void BeginSuccessTransition(DigLevelResult result)
    {
        CancelSuccessTransition();
        successRoutine = StartCoroutine(ShowSuccessAfterDelay(result, successTransitionVersion));
    }
    private System.Collections.IEnumerator ShowSuccessAfterDelay(DigLevelResult result, int version)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, successPanelDelay));
        if (version != successTransitionVersion || !isActiveAndEnabled ||
            CurrentScreen != Screen.ResolvingSuccess || LastResult != result) yield break;
        successRoutine = null;
        CurrentScreen = Screen.Success;
        view.ShowResult(result);
    }
    private void CancelSuccessTransition()
    {
        successTransitionVersion++;
        if (successRoutine != null) StopCoroutine(successRoutine);
        successRoutine = null;
    }
    void OnDisable()
    {
        CancelSuccessTransition();
        if (view != null) view.StopGameplayNotices();
    }
    void OnEnable()
    {
        if (view != null && CurrentScreen == Screen.ResolvingSuccess && LastResult != null && LastResult.success)
            BeginSuccessTransition(LastResult);
    }
    private void HandleExcavated(int index)
    {
        Sprite sprite = manager.GetArtifactSprite(index);
        string artifactName = "文物";
        if (sprite != null && artifactCatalog != null && artifactCatalog.entries != null)
        {
            foreach (DigArtifactCatalog.Entry entry in artifactCatalog.entries)
            {
                if (entry != null && entry.illustration == sprite)
                {
                    artifactName = entry.displayName;
                    break;
                }
            }
        }
        view.QueueExcavationHint("成功發掘" + artifactName + "！");
    }
    private void HandleDiscovery(int index)
    {
        sound.Discovery();
        if (CurrentScreen == Screen.Playing) view.ShowDiscoveryNotice();
    }
    private void HandleDamage(int index)
    {
        sound.Damage();
        if (CurrentScreen == Screen.Playing) view.ShowDamageNotice(manager.GetArtifactSprite(index));
    }
    private void StopBrushAudio() => sound.StopBrush();
    private void HandleBrushMotion(Vector2 from, Vector2 to, Camera camera, float average, float instant)
    {
        if (CurrentScreen == Screen.Playing && instant > 0f) sound.BrushMotion();
    }
    public void OpenKnowledge()
    {
        if (CurrentScreen != Screen.Success || LastResult == null || !LastResult.success) return;
        CurrentScreen = Screen.Knowledge;
        view.ShowKnowledge(0);
    }
    public void FinishLevel()
    {
        if (CurrentScreen != Screen.Knowledge || submitted || LastResult == null ||
            artifactCatalog.entries == null || view.KnowledgePage != artifactCatalog.entries.Length - 1) return;
        submitted = true;
        CurrentScreen = Screen.Completed;
        view.ShowIntegrationComplete();
        var nativeResult=LastResult;
        var handoff=CaptureHandoff(nativeResult); // Capture before outward listeners can reset this level.
        if(handoff!=null) ResultReady?.Invoke(handoff);
        LevelCompleted?.Invoke(nativeResult);
        onLevelCompleted?.Invoke();
    }
    public bool ResetForNewChallenge()
    {
        if(!IsReady || artifactSpawner==null) return false;
        CancelSuccessTransition();
        view.StopGameplayNotices();
        StopAllCoroutines();
        manager.SetGameplayActive(false);
        soilBrush.enabled=false; soilImage.raycastTarget=false;
        SetCursor(false); sound.StopBrush();
        if(!manager.ResetForNewChallenge()) return false;
        artifactSpawner.RandomizeArtifacts();
        if(!soilBrush.ResetSoil()) return false;
        submitted=false; resultHandled=false; integrationPublished=false;
        LastResult=null; LastHandoffResult=null;
        CurrentScreen=Screen.Introduction;
        view.ResetForNewChallenge();
        sound.RestartMusic();
        return true;
    }
    public void RetryChallenge()
    {
        if(CurrentScreen!=Screen.Failure) return;
        if(!ResetForNewChallenge()) Debug.LogError("Dig 重試缺少已初始化的泥土、管理器或生成器引用。",this);
    }
    void OnApplicationPause(bool paused)
    {
        if (paused && (CurrentScreen == Screen.Playing || CurrentScreen == Screen.ResolvingSuccess)) PauseGame();
    }
    void OnApplicationFocus(bool focused)
    {
        if (!focused && (CurrentScreen == Screen.Playing || CurrentScreen == Screen.ResolvingSuccess)) PauseGame();
    }
    void OnDestroy()
    {
        CancelSuccessTransition();
        if (manager != null)
        {
            manager.ChallengeEnded -= HandleResult;
            manager.OnArtifactFirstSeen -= HandleDiscovery;
            manager.ArtifactDamaged -= HandleDamage;
            manager.ArtifactExcavated -= HandleExcavated;
        }
        if (soilBrush != null)
        {
            soilBrush.OnBrushMotion -= HandleBrushMotion;
            soilBrush.OnBrushReleased -= StopBrushAudio;
        }
        Cursor.visible = true;
    }
}
