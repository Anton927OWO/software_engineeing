using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DefaultExecutionOrder(-50)]
public sealed class StrataStageTwoManager : MonoBehaviour
{
    [SerializeField] private StrataStageOneManager firstStage;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private GameObject[] firstStageAreas;
    [SerializeField] private StrataPatternQuestionSet questionSet;
    [SerializeField, Min(.1f)] private float correctFeedbackDuration = .9f;
    [SerializeField, Min(.1f)] private float mistakeFeedbackDuration = 1.5f;
    [SerializeField] private UnityEvent onStageTwoCompleted = new UnityEvent();
    public event Action StageTwoCompleted;
    public event Action<bool> OperationFeedback;
    public Func<bool> InputGuard { get; set; }
    public bool IsInitialized => root != null;
    public int CurrentQuestionIndex { get; private set; }
    public int CorrectCount { get; private set; }
    public int MistakeCount { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsActive => started && questionReady && !IsComplete && inputEnabled &&
        isActiveAndEnabled && root != null && root.gameObject.activeInHierarchy;
    public bool IsPreviewContent => questionSet != null && questionSet.previewOnly;
    public bool HasMissingContent => started && !questionReady;
    public RectTransform DragRoot { get; private set; }
    public bool CanInteract => started && inputEnabled && questionReady && !resolving &&
        !IsComplete && isActiveAndEnabled && root != null && root.gameObject.activeInHierarchy &&
        (InputGuard == null || InputGuard());
    private RectTransform root, matchTarget, focusFrame, feedbackGroup;
    private Image artifactImage;
    private RawImage enlargedPattern;
    private TMP_Text prompt, progress, feedback, notice, dropCaption;
    private readonly StrataPatternOptionDrag[] choices = new StrataPatternOptionDrag[3];
    private readonly RawImage[] choiceImages = new RawImage[3];
    private readonly TMP_Text[] choiceLabels = new TMP_Text[3];
    private StrataPatternOptionDrag activeChoice;
    private int[] questionOrder;
    private int DataQuestionIndex => questionOrder != null && CurrentQuestionIndex < questionOrder.Length
        ? questionOrder[CurrentQuestionIndex] : CurrentQuestionIndex;
    private bool started, questionReady, resolving, inputEnabled = true, resumeAfterEnable;
    private float nextQuestionAt, feedbackUntil, suspendedAt;
    private static readonly Color Ink = Hex("203D32"), Cream = Hex("F5EBD6"),
        Wood = Hex("705438"), Correct = Hex("BFE3C0"), Incorrect = Hex("F1B7B1");

    void Awake()
    {
        if (firstStage == null || canvas == null || font == null || firstStageAreas == null ||
            firstStageAreas.Length != 2 || firstStageAreas[0] == null || firstStageAreas[1] == null)
        {
            Debug.LogError("Strata 第二階段缺少第一階段、Canvas、字型或兩個第一階段顯示區引用。", this);
            enabled = false;
            return;
        }
        BuildUI();
        root.gameObject.SetActive(false);
    }
    void OnEnable()
    {
        if (root == null || firstStage == null) return;
        firstStage.SecondStageRequested += BeginStageTwo;
        if (resumeAfterEnable) SetInputEnabled(true);
        if (started) { root.gameObject.SetActive(true); RefreshInput(); }
        else if (firstStage.HasRequestedSecondStage) BeginStageTwo();
    }
    void OnDisable()
    {
        if (firstStage != null) firstStage.SecondStageRequested -= BeginStageTwo;
        resumeAfterEnable = inputEnabled;
        SetInputEnabled(false);
        if (root != null) root.gameObject.SetActive(false);
    }

    public void BeginStageTwo()
    {
        if (!inputEnabled || (InputGuard != null && !InputGuard()) ||
            started || root == null || firstStage == null || !firstStage.IsComplete ||
            !firstStage.HasRequestedSecondStage) return;
        firstStage.SetPresentationVisible(false);
        foreach (GameObject area in firstStageAreas) area.SetActive(false);
        if (questionOrder == null) PrepareQuestionOrder();
        started = true;
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        ShowQuestion();
    }

    public void ResetForNewChallenge()
    {
        SetInputEnabled(false);
        if (activeChoice != null) activeChoice.CancelDrag();
        started = questionReady = resolving = IsComplete = false;
        CorrectCount = MistakeCount = CurrentQuestionIndex = 0;
        PrepareQuestionOrder();
        feedbackUntil = nextQuestionAt = 0;
        HideFeedback();
        suspendedAt = Time.unscaledTime;
        resumeAfterEnable = false;
        foreach (var option in choices) if (option != null) option.Bind("");
        dropCaption.gameObject.SetActive(true);
        root.gameObject.SetActive(false);
        foreach (var area in firstStageAreas) area.SetActive(true);
        firstStage.SetPresentationVisible(true);
        RefreshInput();
    }

    private void PrepareQuestionOrder()
    {
        questionOrder=new[] { 0, 1, 2 };
        StrataShuffle.FisherYates(questionOrder, upper => UnityEngine.Random.Range(0,upper));
    }

    // Future shared timer/pause controllers can suspend input and feedback without changing timeScale.
    public void SetInputEnabled(bool value)
    {
        if (inputEnabled == value) return;
        inputEnabled = value;
        if (!value)
        {
            suspendedAt = Time.unscaledTime;
            if (activeChoice != null) activeChoice.CancelDrag();
        }
        else
        {
            float delay = Mathf.Max(0, Time.unscaledTime - suspendedAt);
            feedbackUntil += delay;
            if (resolving) nextQuestionAt += delay;
        }
        RefreshInput();
    }

    internal bool TryBeginDrag(StrataPatternOptionDrag option)
    {
        if (!CanInteract || activeChoice != null || option == null || option.IsPlaced ||
            Array.IndexOf(choices, option) < 0) return false;
        activeChoice = option;
        return true;
    }
    internal void ReleaseDrag(StrataPatternOptionDrag option)
    {
        if (activeChoice == option) activeChoice = null;
    }
    internal bool TryMatch(StrataPatternOptionDrag option)
    {
        if (!CanInteract || activeChoice != option || option == null || option.IsPlaced) return false;
        option.MarkHandled();
        if (option.OptionId != questionSet.questions[DataQuestionIndex].correctOptionId)
        {
            ReportMistake();
            return false;
        }
        option.Place(matchTarget);
        dropCaption.gameObject.SetActive(false);
        CorrectCount++;
        if(CorrectCount<3) OperationFeedback?.Invoke(true);
        resolving = true;
        nextQuestionAt = Time.unscaledTime + correctFeedbackDuration;
        ShowFeedback("回答正確！", Correct, correctFeedbackDuration);
        RefreshProgress();
        RefreshInput();
        if (CorrectCount == 3)
        {
            IsComplete = true;
            feedback.text = "回答正確！";
            StageTwoCompleted?.Invoke();
            onStageTwoCompleted?.Invoke();
        }
        return true;
    }
    public void ReportMistake()
    {
        if (!CanInteract) return;
        MistakeCount++;
        OperationFeedback?.Invoke(false);
        RefreshProgress();
        ShowFeedback("答錯了，請再試一次。", Incorrect, mistakeFeedbackDuration);
    }

    void Update()
    {
        if (!started || !inputEnabled || root == null || !root.gameObject.activeInHierarchy) return;
        if (resolving && !IsComplete && activeChoice == null && Time.unscaledTime >= nextQuestionAt)
        {
            CurrentQuestionIndex++;
            resolving = false;
            ShowQuestion();
        }
        else if (feedbackGroup.gameObject.activeSelf && Time.unscaledTime >= feedbackUntil)
        {
            HideFeedback();
        }
    }

    private void ShowQuestion()
    {
        HideFeedback();
        questionReady = false;
        dropCaption.gameObject.SetActive(true);
        StrataPatternQuestionSet.Question q = questionSet != null &&
            DataQuestionIndex < questionSet.Count ? questionSet.questions[DataQuestionIndex] : null;
        artifactImage.sprite = q != null ? q.artifactImage : null;
        artifactImage.enabled = false; // Retain the original Sprite source; the cropped target below is the sole visible preview.
        prompt.text = "觀察紋樣，拖曳相同圖案完成配對";
        notice.text = questionSet == null ? "題庫尚未設定，無法開始配對。" : "預覽題組";
        notice.gameObject.SetActive(questionSet == null || questionSet.previewOnly);
        int[] optionOrder={0,1,2};
        StrataShuffle.FisherYates(optionOrder, upper => UnityEngine.Random.Range(0,upper));
        for (int i = 0; i < choices.Length; i++)
        {
            int source=optionOrder[i];
            var option = q != null && q.options != null && source < q.options.Length ? q.options[source] : null;
            choices[i].Bind(option != null ? option.id : "");
            bool hasImage = option != null && ApplyVisual(choiceImages[i], option.sprite,
                option.region, new Vector2(420, 144));
            choiceImages[i].enabled = hasImage;
            choiceLabels[i].text = hasImage ? "" : "素材待補";
        }
        string reason = "請在 Inspector 指定三題題庫。";
        questionReady = questionSet != null && questionSet.IsQuestionReady(DataQuestionIndex, out reason);
        focusFrame.gameObject.SetActive(false); // The single target preview needs no duplicate full-image focus frame.
        enlargedPattern.enabled = questionReady;
        if (questionReady)
        {
            SetFocus(q.focusRegion);
            ApplyVisual(enlargedPattern, q.artifactImage, q.focusRegion, new Vector2(620, 220));
        }
        else ShowFeedback("本題尚未就緒：" + reason, Incorrect, 1.5f);
        RefreshProgress();
        RefreshInput();
    }
    private void RefreshProgress()
    {
        if (progress != null) progress.text = $"題目 {CurrentQuestionIndex + 1} / 3　配對 {CorrectCount} / 3　失誤 {MistakeCount}";
    }
    private void RefreshInput()
    {
        foreach (var option in choices) if (option != null) option.SetInteractable(CanInteract);
    }
    private void ShowFeedback(string message, Color color, float duration)
    {
        if (string.IsNullOrEmpty(message)) { HideFeedback(); return; }
        feedbackGroup.gameObject.SetActive(true);
        feedback.text = message;
        bool incorrect = color == Incorrect, correct = color == Correct;
        feedback.color = incorrect ? Hex("A4372E") : correct ? Hex("315D46") : Hex("735A46");
        feedback.fontStyle = correct ? FontStyles.Bold : FontStyles.Normal;
        feedbackUntil = Time.unscaledTime + duration;
    }
    private void HideFeedback()
    {
        if (feedback != null) feedback.text = "";
        if (feedbackGroup != null) feedbackGroup.gameObject.SetActive(false);
    }

    private static bool ApplyVisual(RawImage view, Sprite sprite, Rect region, Vector2 maximumSize)
    {
        Texture texture; Rect uv; float aspect;
        if (!StrataPatternQuestionSet.TryGetVisual(sprite, region, out texture, out uv, out aspect))
        { view.texture = null; view.enabled = false; return false; }
        view.texture = texture;
        view.uvRect = uv;
        float width = Mathf.Min(maximumSize.x, maximumSize.y * aspect);
        view.rectTransform.sizeDelta = new Vector2(width, width / aspect);
        view.enabled = true;
        return true;
    }
    private void SetFocus(Rect region)
    {
        Rect drawn = artifactImage.GetPixelAdjustedRect();
        float aspect = artifactImage.sprite.rect.width / artifactImage.sprite.rect.height;
        if (drawn.width / drawn.height > aspect)
        {
            float width = drawn.height * aspect;
            drawn.x += (drawn.width - width) * .5f; drawn.width = width;
        }
        else
        {
            float height = drawn.width / aspect;
            drawn.y += (drawn.height - height) * .5f; drawn.height = height;
        }
        focusFrame.anchoredPosition = new Vector2(drawn.xMin + region.center.x * drawn.width,
            drawn.yMin + region.center.y * drawn.height);
        focusFrame.sizeDelta = new Vector2(region.width * drawn.width, region.height * drawn.height);
    }

    private void BuildUI()
    {
        root = Box("StrataStageTwoRoot", canvas.transform, Vector2.zero, new Vector2(1920, 1080), Hex("E8DECA"));
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        // The shared timer and pause button remain above this local, non-interactive header.
        Surface("StageTwoHeaderCard", root, new Vector2(0, 438), new Vector2(1840, 188), Hex("FAF4E9"));
        Label("StageTwoTitle", root, "地層解碼｜紋飾配對", new Vector2(-420, 480), new Vector2(960, 64), 40, Ink, true);
        prompt = Label("StageTwoPrompt", root, "", new Vector2(-430, 414), new Vector2(940, 48), 28, Hex("735A46"));
        progress = Label("StageTwoProgress", root, "", new Vector2(470, 414), new Vector2(820, 56), 30, Ink, true);
        // Text-only answer feedback: no Image, border, accent or shadow is created here.
        feedbackGroup = Rect("StageTwoFeedbackGroup", root, new Vector2(0, 294), new Vector2(860, 68));
        feedback = Label("StageTwoFeedback", feedbackGroup, "", Vector2.zero, new Vector2(832, 56), 32, Hex("315D46"), true);
        feedback.margin = new Vector4(12, 4, 12, 4);
        HideFeedback();

        Surface("PotteryReferenceCard", root, new Vector2(-390, 50), new Vector2(700, 360), Hex("FAF4E9"));
        Surface("TargetPatternMatCard", root, new Vector2(-390, 10), new Vector2(644, 240), Hex("F4ECDD"));
        Label("PotteryReferenceCaption", root, "目標紋飾", new Vector2(-390, 182), new Vector2(620, 52), 32, Ink, true);
        // Keep the full Sprite source and focus references for compatibility; only the cropped target is drawn.
        artifactImage = Box("PotteryReference", root, new Vector2(-390, 10), new Vector2(620, 220), Color.white).GetComponent<Image>();
        artifactImage.preserveAspect = true;
        artifactImage.enabled = false;
        focusFrame = Rect("PatternFocusFrame", artifactImage.transform, Vector2.zero, Vector2.zero);
        Border(focusFrame, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 4));
        Border(focusFrame, "Bottom", Vector2.zero, new Vector2(1, 0), new Vector2(0, 4));
        Border(focusFrame, "Left", Vector2.zero, new Vector2(0, 1), new Vector2(4, 0));
        Border(focusFrame, "Right", new Vector2(1, 0), Vector2.one, new Vector2(4, 0));
        focusFrame.gameObject.SetActive(false);
        enlargedPattern = Rect("PatternEnlargement", root, new Vector2(-390, 10), new Vector2(620, 220)).gameObject.AddComponent<RawImage>();
        enlargedPattern.raycastTarget = false;

        Surface("PatternDropBorder", root, new Vector2(390, 50), new Vector2(700, 360), Hex("FAF4E9"));
        Surface("PatternDropWellCard", root, new Vector2(390, 10), new Vector2(644, 240), Hex("F4ECDD"));
        matchTarget = Box("PatternDropTarget", root, new Vector2(390, 10), new Vector2(640, 236), Hex("F4ECDD"));
        StrataUIStyle.Round(matchTarget.GetComponent<Image>());
        matchTarget.GetComponent<Image>().raycastTarget = true;
        matchTarget.gameObject.AddComponent<StrataPatternDropTarget>().Initialize(this);
        dropCaption = Label("PatternDropCaption", matchTarget, "配對區", new Vector2(0, 172), new Vector2(620, 52), 32, Ink, true);

        Label("OptionsHeading", root, "選擇相同紋飾", new Vector2(0, -180), new Vector2(1560, 48), 30, Hex("735A46"), true);
        Surface("PatternOptionsTrayCard", root, new Vector2(0, -330), new Vector2(1640, 230), Hex("FAF4E9"));
        RectTransform tray = Rect("StrataPatternOptions", root, Vector2.zero, new Vector2(1920, 1080));
        for (int i = 0; i < choices.Length; i++)
        {
            Vector2 slot = new Vector2((i - 1) * 500, -330);
            RectTransform shadow = Box("OptionShadowCard" + i, tray, slot + new Vector2(0, -4), new Vector2(466, 184), new Color(.28f, .18f, .10f, .10f));
            StrataUIStyle.Round(shadow.GetComponent<Image>());
            RectTransform card = Box("PatternOption" + (i + 1), tray, slot, new Vector2(460, 180), Hex("C8A784"));
            card.GetComponent<Image>().raycastTarget = true;
            var face = Box("OptionFaceCard", card, Vector2.zero, new Vector2(456, 176), Hex("FAF4E9"));
            StrataUIStyle.Round(face.GetComponent<Image>());
            var imageMat = Box("OptionImageMatCard", card, Vector2.zero, new Vector2(424, 148), Hex("F4ECDD"));
            var imagePaper = Box("ImageSurface", imageMat, Vector2.zero, new Vector2(420, 144), Hex("F4ECDD"));
            StrataUIStyle.Round(imagePaper.GetComponent<Image>());
            choiceImages[i] = Rect("PatternOptionImage", card, Vector2.zero, new Vector2(420, 144)).gameObject.AddComponent<RawImage>();
            choiceImages[i].raycastTarget = false;
            choiceLabels[i] = Label("PatternOptionLabel", card, "", Vector2.zero, new Vector2(420, 80), 26, Hex("735A46"));
            choices[i] = card.gameObject.AddComponent<StrataPatternOptionDrag>();
            choices[i].Initialize(this);
        }
        notice = Label("StageTwoContentNotice", root, "", new Vector2(0, -482), new Vector2(1760, 44), 24, Hex("735A46"));
        DragRoot = Rect("StrataPatternDragLayer", root, Vector2.zero, Vector2.zero);
        DragRoot.anchorMin = Vector2.zero; DragRoot.anchorMax = Vector2.one;
        DragRoot.offsetMin = DragRoot.offsetMax = Vector2.zero;
    }

    // Decoration only. The original match target and option roots own all input events.
    private RectTransform Surface(string name, Transform parent, Vector2 position, Vector2 size, Color fill)
    {
        var shadow = Box(name + "Shadow", parent, position + new Vector2(0, -4), size + new Vector2(4, 4), new Color(.28f, .18f, .10f, .07f));
        StrataUIStyle.Round(shadow.GetComponent<Image>());
        var border = Box(name, parent, position, size, Hex("C8A784"));
        StrataUIStyle.Round(border.GetComponent<Image>());
        var face = Box("Surface", border, Vector2.zero, size - new Vector2(4, 4), fill);
        StrataUIStyle.Round(face.GetComponent<Image>());
        return border;
    }

    private void Border(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 size)
    {
        var rect = Box(name, parent, Vector2.zero, size, Hex("EDD596"));
        rect.anchorMin = min; rect.anchorMax = max;
    }
    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    private static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        // Keep pottery and the drop surface rectangular; round decorative cards only.
        if (name.EndsWith("Card") || name.StartsWith("PatternOption") || name == "PatternDropBorder") StrataUIStyle.Round(image);
        return rect;
    }
    private TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize, Color color, bool bold = false)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = fontSize;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = color; label.raycastTarget = false; label.lineSpacing=4;
        return label;
    }
}
