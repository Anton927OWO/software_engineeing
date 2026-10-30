using System.Linq;
using NIUAdventure.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Integration;
using TeamSession = TrackingNIU.LevelSession;

/// <summary>
/// 把序章、考古小隊、地層解碼的 ILevelHandoffSource 接到團隊的 LevelSession。
/// 成功：回報後顯示得分與「繼續」；失敗：保留關卡原本的重試，另外提供「繼續旅程」；
/// 序章不計分，等關卡自己的繼續按鈕（ContinueRequested）才離開。
/// </summary>
[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public sealed class TrackingHandoffBridge : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;

    private ILevelHandoffSource source;
    private PrologueManager prologue;
    private TrackingNIU.LevelContext context;
    private LevelHandoffResult pendingFailure;
    private GameObject failureBar, scorePanel;
    private TextMeshProUGUI scoreText;

    private bool OwnsContext => context != null && ReferenceEquals(TeamSession.Current, context);

    private void Start()
    {
        context = TeamSession.Current;
        source = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<ILevelHandoffSource>().FirstOrDefault();
        if (context == null || source == null)
        {
            Debug.LogError("TrackingHandoffBridge 找不到 LevelSession 或關卡流程。請確認 Scene 裡有 LevelTestLauncher。", this);
            enabled = false;
            return;
        }
        prologue = source as PrologueManager;
        // 序章不發文物；其他關卡用終章同一套文物編號。
        string reward = context.IsScored ? TrackingArtifactMap.ToLocalId(context.Artifact) : null;
        if (!source.ConfigureIntegration(context.LevelId.ToString(), reward))
            Debug.LogWarning($"[TrackingHandoffBridge] {context.LevelId} 拒絕整合設定，結果仍會回報。", this);
        source.ResultReady += OnResult;
        if (prologue != null) prologue.ContinueRequested += Continue;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (source != null) source.ResultReady -= OnResult;
        if (prologue != null) prologue.ContinueRequested -= Continue;
        // 單獨測試中途停止時結束本次 Session，避免殘留到下一次 Play 或測試。
        if (!OwnsContext || !context.IsTestRun) return;
        if (!TeamSession.HasReported) TeamSession.Report(new TrackingNIU.LevelResult());
        TeamSession.Exit();
    }

    private void Update()
    {
        // 關卡原地重試會清掉 LastHandoffResult，此時收回「繼續旅程」。
        if (pendingFailure != null && source.LastHandoffResult == null)
        {
            pendingFailure = null;
            failureBar.SetActive(false);
        }
    }

    private void OnResult(LevelHandoffResult result)
    {
        if (!OwnsContext || TeamSession.HasReported || result == null) return;
        if (!result.Success)
        {
            pendingFailure = result;
            failureBar.SetActive(true);
            return;
        }
        var score = Report(result);
        if (prologue != null) return;
        scoreText.text = $"基礎分　{score.ClearBonus}　　時間獎勵　{score.TimeBonus}　　失誤扣分　−{score.MistakePenalty}\n" +
            $"總分　{score.Total}　　獲得文物：{TrackingNIU.LevelCatalog.GetArtifactName(context.Artifact)}";
        scorePanel.SetActive(true);
    }

    private TrackingNIU.ScoreBreakdown Report(LevelHandoffResult result) => TeamSession.Report(new TrackingNIU.LevelResult
    {
        Cleared = result.Success,
        RemainingTime = result.RemainingSeconds ?? 0f,
        Mistakes = result.MistakeCount ?? 0
    });

    private void ContinueAfterFailure()
    {
        if (pendingFailure == null || !OwnsContext || TeamSession.HasReported) return;
        Report(pendingFailure);
        Continue();
    }

    private void Continue()
    {
        if (!OwnsContext || !TeamSession.HasReported) return;
        bool testRun = context.IsTestRun;
        Time.timeScale = 1f; // 序章暫停選單使用 timeScale，離開前還原。
        TeamSession.Exit();
        // 單獨測試時沒有 GameManager 接手，重新載入本關方便再玩一次。
        if (testRun) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("Tracking Handoff Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        failureBar = Panel(canvasObject.transform, "FailureBar", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(560, 110), new Color(0, 0, 0, 0));
        Button(failureBar.transform, "繼續旅程（不重試）", new Vector2(560, 110), ContinueAfterFailure);
        failureBar.SetActive(false);

        scorePanel = Panel(canvasObject.transform, "ScorePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080), new Color(0.05f, 0.1f, 0.08f, 0.82f));
        var card = Panel(scorePanel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 520), new Color(0.98f, 0.97f, 0.95f));
        Label(card.transform, "關卡完成", 56, new Vector2(0, 170), new Vector2(1100, 90));
        scoreText = Label(card.transform, "", 36, new Vector2(0, 30), new Vector2(1100, 140));
        var button = Button(card.transform, "繼續", new Vector2(420, 110), Continue);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0, -160);
        scorePanel.SetActive(false);
    }

    private static GameObject Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI Label(Transform parent, string text, int size, Vector2 position, Vector2 box)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = new Color(0.12f, 0.23f, 0.18f);
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    private Button Button(Transform parent, string text, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var go = Panel(parent, "Button", new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(0.7f, 0.2f, 0.16f));
        var button = go.AddComponent<Button>();
        button.onClick.AddListener(action);
        var label = Label(go.transform, text, 38, Vector2.zero, size);
        label.color = Color.white;
        return button;
    }
}
