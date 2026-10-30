using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class StrataStageOneManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private RectTransform artifactPanel;
    [SerializeField] private StrataArtifactDrag[] artifacts;
    [SerializeField] private StrataLayerDropZone[] layers;
    [Header("回饋與整合：不自行換 Scene、不計算全組分數")]
    [SerializeField, Min(.1f)] private float feedbackDuration = 1.5f;
    [SerializeField] private UnityEvent onStageOneCompleted = new UnityEvent();
    [SerializeField] private UnityEvent onSecondStageRequested = new UnityEvent();
    public event Action StageOneCompleted;
    public event Action<bool> OperationFeedback;
    public event Action SecondStageRequested;
    public bool IsComplete { get; private set; }
    public bool IsInitialized => initialized;
    public int MistakeCount { get; private set; }
    public System.Func<bool> InputGuard { get; set; }
    private bool inputEnabled = true, positionsPrepared;
    private float suspendedAt;
    private bool AllowsInput() => inputEnabled && (InputGuard == null || InputGuard());
    public bool HasRequestedSecondStage { get; private set; }
    public int MatchedCount => matched.Count;
    public RectTransform DragRoot { get; private set; }
    private readonly HashSet<StrataArtifactDrag> matched = new HashSet<StrataArtifactDrag>();
    private StrataArtifactDrag activeArtifact;
    private TMP_Text feedback, knowledge, task;
    private const string ObservationSuccessMessage = "回答正確！";
    private readonly TMP_Text[] trayNames=new TMP_Text[3], trayCompleted=new TMP_Text[3];
    private readonly Dictionary<StrataArtifactDrag,int> artifactSlots=new Dictionary<StrataArtifactDrag,int>();
    private RectTransform trayDecoration;
    private Image trayBackground;
    private RectTransform feedbackBackdrop;
    private Vector2 layoutCanvasSize;
    private float layoutCanvasScale = -1f;
    private float knowledgeUntil;
    private RectTransform stageOneHud;
    private Button secondStageButton;
    private RectTransform observationPanel;
    private Button observationCorrect, observationIncorrect;
    private bool observationPending;
    private float observationHighlightUntil;
    private float feedbackUntil;
    private bool initialized;
    private static readonly Color Ink = ColorOf("203D32"), Cream = ColorOf("F5EBD6"),
        Wood = ColorOf("9B7049"), Correct = ColorOf("BFE3C0"), Incorrect = ColorOf("F1B7B1");

    void Awake()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("Strata 第一階段引用不完整或重複，請檢查 Canvas、三件文物與四個地層。", this);
            enabled = false;
            return;
        }
        BuildFeedbackUI();
        BuildTray();
        DragRoot = MakeRect("StrataDragLayer", canvas.transform, Vector2.zero, Vector2.zero);
        DragRoot.anchorMin = Vector2.zero;
        DragRoot.anchorMax = Vector2.one;
        DragRoot.offsetMin = DragRoot.offsetMax = Vector2.zero;
        initialized = true;
        ShowProgress();
    }

    public void SetInputEnabled(bool value)
    {
        if (inputEnabled != value)
        {
            inputEnabled = value;
            if (!value)
            {
                ClearDragHighlights();
                suspendedAt = Time.unscaledTime;
                if (activeArtifact != null) activeArtifact.CancelDrag();
            }
            else
            {
                float delay=Mathf.Max(0, Time.unscaledTime-suspendedAt);
                feedbackUntil+=delay; knowledgeUntil+=delay; observationHighlightUntil+=delay;
            }
        }
        if (secondStageButton != null)
            secondStageButton.interactable = value && IsComplete && !HasRequestedSecondStage;
        if (observationCorrect != null)
        {
            observationCorrect.interactable = observationIncorrect.interactable = value && observationPending && !IsComplete;
            if (value && observationPending && Time.unscaledTime<observationHighlightUntil) layers[3].SetHighlight(true);
        }
    }

    public void ResetForNewChallenge()
    {
        SetInputEnabled(false);
        foreach (var artifact in artifacts) artifact.ResetForNewChallenge();
        matched.Clear();
        artifactSlots.Clear();
        foreach(var label in trayCompleted) label.gameObject.SetActive(false);
        trayDecoration.gameObject.SetActive(true);
        MistakeCount = 0;
        IsComplete = false;
        observationPending=false;
        observationHighlightUntil=0;
        observationPanel.gameObject.SetActive(false);
        observationCorrect.interactable=observationIncorrect.interactable=false;
        knowledge.color=Ink;
        HasRequestedSecondStage = false;
        positionsPrepared = false;
        feedbackUntil = 0;
        knowledgeUntil=0;
        knowledge.text="";
        task.gameObject.SetActive(true);
        ClearDragHighlights();
        secondStageButton.gameObject.SetActive(false);
        secondStageButton.interactable = false;
        ShowProgress();
        RefreshTrayPresentation();
        SetPresentationVisible(true);
    }

    public bool PrepareRandomizedLayout()
    {
        if (!initialized || positionsPrepared || inputEnabled || activeArtifact != null ||
            IsComplete || matched.Count != 0) return false;
        var slots = new[] { new Vector2(-300, -9), new Vector2(0, -9), new Vector2(300, -9) };
        StrataShuffle.FisherYates(slots, upper => UnityEngine.Random.Range(0, upper));
        for (int i = 0; i < artifacts.Length; i++)
            if (artifacts[i].IsPlaced) return false;
        for (int i = 0; i < artifacts.Length; i++)
        {
            if (!artifacts[i].SetInitialPosition(slots[i])) return false;
            int slot=Mathf.RoundToInt(slots[i].x/300)+1;
            artifactSlots[artifacts[i]]=slot;
            trayNames[slot].text=artifacts[i].TargetLayer==1 ? "磚瓦碎片" :
                artifacts[i].TargetLayer==2 ? "玻璃珠" : "印紋陶片";
        }
        positionsPrepared = true;
        return true;
    }

    private bool ValidateReferences()
    {
        if (canvas == null || font == null || artifactPanel == null || artifacts == null ||
            artifacts.Length != 3 || layers == null || layers.Length != 4) return false;
        var uniqueArtifacts = new HashSet<StrataArtifactDrag>();
        var targets = new HashSet<int>();
        var uniqueLayers = new HashSet<StrataLayerDropZone>();
        for (int i = 0; i < artifacts.Length; i++)
            if (artifacts[i] == null || artifacts[i].Owner != this ||
                !uniqueArtifacts.Add(artifacts[i]) || artifacts[i].TargetLayer < 1 ||
                artifacts[i].TargetLayer > 3 || !targets.Add(artifacts[i].TargetLayer)) return false;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] == null || layers[i].Owner != this || layers[i].LayerNumber != i + 1 ||
                !uniqueLayers.Add(layers[i])) return false;
        return true;
    }

    internal bool TryBeginDrag(StrataArtifactDrag artifact)
    {
        if (!initialized || !isActiveAndEnabled || !AllowsInput() || IsComplete || activeArtifact != null ||
            artifact == null || artifact.IsPlaced || Array.IndexOf(artifacts, artifact) < 0) return false;
        activeArtifact = artifact;
        return true;
    }

    internal void UpdateDragHover(StrataArtifactDrag artifact, Vector2 position, Camera camera)
    {
        if (artifact != activeArtifact || !inputEnabled) { ClearDragHighlights(); return; }
        foreach (var layer in layers)
            layer.SetHighlight(layer.isActiveAndEnabled &&
                RectTransformUtility.RectangleContainsScreenPoint(layer.Rect, position, camera));
    }
    private void ClearDragHighlights()
    {
        if (layers == null) return;
        foreach (var layer in layers) if (layer != null) layer.SetHighlight(false);
    }
    internal void ReleaseDrag(StrataArtifactDrag artifact)
    {
        ClearDragHighlights();
        if (activeArtifact == artifact) activeArtifact = null;
        if (observationPending && inputEnabled && Time.unscaledTime<observationHighlightUntil)
            layers[3].SetHighlight(true);
    }

    internal bool TryPlace(StrataArtifactDrag artifact, StrataLayerDropZone zone)
    {
        ClearDragHighlights();
        if (!initialized || !isActiveAndEnabled || !AllowsInput() || IsComplete || artifact == null || zone == null ||
            activeArtifact != artifact || artifact.IsPlaced || matched.Contains(artifact) ||
            Array.IndexOf(layers, zone) < 0) return false;
        artifact.MarkDropHandled();
        if (artifact.TargetLayer != zone.LayerNumber)
        {
            ReportIncorrectDrop();
            return false;
        }
        artifact.Place(zone.Rect, zone.PlacementPosition);
        matched.Add(artifact);
        if(artifactSlots.TryGetValue(artifact,out int slot)) trayCompleted[slot].gameObject.SetActive(true);
        OperationFeedback?.Invoke(true);
        string[] notes = {
            "磚瓦碎片已歸層。",
            "玻璃珠已歸層。",
            "印紋陶片已歸層。"
        };
        task.gameObject.SetActive(false);
        knowledge.text=notes[zone.LayerNumber-1];
        knowledgeUntil=Time.unscaledTime+2f;
        ShowFeedback("配對正確！", Correct);
        if (matched.Count == artifacts.Length)
        {
            trayDecoration.gameObject.SetActive(false);
            observationPending=true;
            observationHighlightUntil=Time.unscaledTime+1.5f;
            layers[3].SetHighlight(true);
            observationPanel.gameObject.SetActive(true);
            observationCorrect.interactable=observationIncorrect.interactable=inputEnabled;
            ShowProgress();
        }
        return true;
    }

    // Both UI buttons use the same input/deadline guard as dragging.
    public void AnswerObservation(bool correct)
    {
        if (!initialized || !isActiveAndEnabled || !observationPending || IsComplete ||
            matched.Count!=artifacts.Length || !AllowsInput()) return;
        task.gameObject.SetActive(false);
        knowledgeUntil=Time.unscaledTime+4f;
        if (!correct)
        {
            MistakeCount++;
            OperationFeedback?.Invoke(false);
            knowledge.color=ColorOf("A4372E");
            knowledge.text="答錯了，請再試一次。";
            return;
        }
        // Set terminal state before invoking listeners: repeated clicks cannot complete twice.
        observationPending=false;
        IsComplete=true;
        OperationFeedback?.Invoke(true);
        observationPanel.gameObject.SetActive(false);
        observationCorrect.interactable=observationIncorrect.interactable=false;
        ClearDragHighlights();
        knowledge.color=ColorOf("315D46");
        knowledge.text=ObservationSuccessMessage;
        feedback.text="文物 3 / 3｜生土層觀察完成";
        feedback.color=Correct;
        secondStageButton.interactable=inputEnabled;
        secondStageButton.gameObject.SetActive(true);
        RefreshTrayPresentation();
        StageOneCompleted?.Invoke();
        onStageOneCompleted?.Invoke();
    }

    public void ReportIncorrectDrop()
    {
        if (initialized && isActiveAndEnabled && !IsComplete && AllowsInput())
        {
            MistakeCount++;
            OperationFeedback?.Invoke(false);
            ShowFeedback("地層不正確，請再試一次", Incorrect);
        }
    }

    public void RequestSecondStage()
    {
        if (!isActiveAndEnabled || !AllowsInput() || !IsComplete || HasRequestedSecondStage) return;
        HasRequestedSecondStage = true;
        secondStageButton.interactable = false;
        // Integration hook only: the pottery-pattern stage/scene is intentionally not loaded here.
        SecondStageRequested?.Invoke();
        onSecondStageRequested?.Invoke();
    }

    void Update()
    {
        if (initialized && inputEnabled && observationPending && Time.unscaledTime>=observationHighlightUntil)
            layers[3].SetHighlight(false);
        if (initialized && inputEnabled && Time.unscaledTime>=knowledgeUntil)
        { knowledge.text=""; task.gameObject.SetActive(true); }
        if (initialized && inputEnabled && !IsComplete && Time.unscaledTime >= feedbackUntil) ShowProgress();
    }

    private void ShowFeedback(string message, Color color)
    {
        feedback.text = message;
        feedback.color = color;
        feedbackUntil = Time.unscaledTime + feedbackDuration;
    }
    private void ShowProgress()
    {
        feedback.text = observationPending ? "文物 3 / 3｜生土層觀察題" : "已配對 " + MatchedCount + " / 3";
        feedback.color = Cream;
    }
    void OnDisable() { ClearDragHighlights(); if (activeArtifact != null) activeArtifact.CancelDrag(); }

    // Presentation-only layout in effective Canvas units; no artifact/drop-zone local geometry changes.
    private void RefreshStageOneLayout()
    {
        if (canvas == null || artifactPanel == null || layers == null || layers.Length != 4) return;
        var canvasRect = canvas.transform as RectTransform;
        var ground = layers[0].Rect.parent as RectTransform;
        if (canvasRect == null || ground == null || ground.parent != canvasRect || artifactPanel.parent != canvasRect) return;
        for (int i = 1; i < layers.Length; i++)
            if (layers[i].Rect.parent != ground) return;
        Vector2 size = canvasRect.rect.size;
        if (size.x <= 0 || size.y <= 0) return; // Canvas may not have its final dimensions during Awake.
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        if (size == layoutCanvasSize && Mathf.Approximately(scale, layoutCanvasScale)) return;
        layoutCanvasSize = size;
        layoutCanvasScale = scale;
        // Reference-canvas spacing plus pixel floors keep small 16:9 windows visibly separated.
        float bottom = Mathf.Max(32f, 20f / scale);
        float gap = Mathf.Max(16f, 12f / scale);
        float trayHeight = artifactPanel.rect.height;
        float trayY = -size.y * .5f + bottom + trayHeight * .5f;
        float groundHeight = ground.rect.height * Mathf.Abs(ground.localScale.y);
        artifactPanel.anchorMin = artifactPanel.anchorMax = artifactPanel.pivot = new Vector2(.5f, .5f);
        ground.anchorMin = ground.anchorMax = ground.pivot = new Vector2(.5f, .5f);
        artifactPanel.anchoredPosition = new Vector2(0, trayY);
        ground.anchoredPosition = new Vector2(0, trayY + trayHeight * .5f + gap + groundHeight * .5f);
        // Follow the actual responsive ground top; leave all ground/tray positions unchanged.
        if (feedbackBackdrop != null && feedback != null)
        {
            float progressY = ground.anchoredPosition.y + groundHeight * .5f + feedbackBackdrop.rect.height * .5f + 16f;
            feedbackBackdrop.anchoredPosition = new Vector2(0, progressY);
            feedback.rectTransform.anchoredPosition = new Vector2(0, progressY);
        }
    }

    void LateUpdate()
    {
        if (!initialized) return;
        RefreshStageOneLayout();
        RefreshTrayPresentation();
        RefreshObservationNotice();
    }

    private void RefreshObservationNotice()
    {
        if (knowledge == null) return;
        bool hasMessage = !string.IsNullOrEmpty(knowledge.text);
        bool successMessage = hasMessage && knowledge.text == ObservationSuccessMessage;
        bool incorrectMessage = hasMessage && knowledge.text == "答錯了，請再試一次。";
        knowledge.gameObject.SetActive(hasMessage);
        knowledge.fontSize = successMessage || incorrectMessage ? 32 : 26;
        knowledge.fontStyle = successMessage ? FontStyles.Bold : FontStyles.Normal;
    }

    // UI state only: keep the tray parent alive so its original transition button still works.
    private void RefreshTrayPresentation()
    {
        if (trayBackground != null) trayBackground.enabled = !IsComplete;
        if (trayDecoration != null) trayDecoration.gameObject.SetActive(!IsComplete && !observationPending);
        if (secondStageButton == null || artifactPanel == null) return;
        if (!IsComplete)
        {
            secondStageButton.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }
        var ground = layers[0].Rect.parent as RectTransform;
        if (ground == null || ground.parent != artifactPanel.parent) return;
        var buttonRect = secondStageButton.GetComponent<RectTransform>();
        float groundBottom = ground.anchoredPosition.y - ground.rect.height * Mathf.Abs(ground.localScale.y) * .5f;
        float buttonY = groundBottom - 24f - buttonRect.rect.height * .5f;
        buttonRect.anchoredPosition = new Vector2(0, buttonY - artifactPanel.anchoredPosition.y);
    }

    private void BuildTray()
    {
        var panelImage=artifactPanel.GetComponent<Image>();
        trayBackground=panelImage;
        if(panelImage!=null) { panelImage.color=ColorOf("E9D7BE"); panelImage.raycastTarget=false; StrataUIStyle.Round(panelImage); }
        artifactPanel.sizeDelta=new Vector2(1240,208); // Presentation only; the three slots retain their horizontal spacing.
        trayDecoration=MakeRect("ArtifactTrayDecoration",artifactPanel,Vector2.zero,artifactPanel.sizeDelta);
        trayDecoration.SetAsFirstSibling();
        MakeLabel("TrayTitle",trayDecoration,"待辨識文物",new Vector2(0,77),new Vector2(500,42),28,ColorOf("61452F"),true);
        for(int i=0;i<3;i++)
        {
            var shadow=MakeBox("CardShadow"+i,trayDecoration,new Vector2((i-1)*300,-25),new Vector2(274,148),new Color(.28f,.18f,.10f,.12f));
            StrataUIStyle.Round(shadow.GetComponent<Image>());
            var border=MakeBox("ArtifactSlot"+i,trayDecoration,new Vector2((i-1)*300,-23),new Vector2(270,144),ColorOf("C8A784"));
            StrataUIStyle.Round(border.GetComponent<Image>());
            var card=MakeBox("Card",border,Vector2.zero,new Vector2(266,140),ColorOf("FAF4E9"));
            StrataUIStyle.Round(card.GetComponent<Image>());
            trayNames[i]=MakeLabel("ArtifactName",border,"",new Vector2(0,-48),new Vector2(258,36),24,ColorOf("61452F"));
            trayCompleted[i]=MakeLabel("Completed",border,"已完成",new Vector2(0,8),new Vector2(240,48),28,Ink,true);
            trayCompleted[i].gameObject.SetActive(false);
        }
        RefreshStageOneLayout();
    }

    private void BuildFeedbackUI()
    {
        RectTransform backdrop = MakeBox("StrataStageOneBackdrop", canvas.transform,
            Vector2.zero, new Vector2(1920, 1080), ColorOf("E8DECA"));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        backdrop.SetAsFirstSibling();
        RectTransform hud = MakeRect("StrataStageOneHUD", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
        stageOneHud = hud;
        MakeLabel("StrataStageOneTitle", hud, "地層解碼｜文物歸層", new Vector2(-420, 480), new Vector2(960, 64), 38, Ink, true);
        task=MakeLabel("StrataStageOneTask", hud, "拖曳文物，找出對應地層",
            new Vector2(0, 410), new Vector2(1740, 44), 28, Ink);
        feedbackBackdrop = MakeBox("StrataFeedbackBackdrop", hud, new Vector2(0, 345), new Vector2(1200, 52), Ink);
        feedback = MakeLabel("StrataPairingFeedback", hud, "", new Vector2(0, 345), new Vector2(1170, 48), 28, Cream);
        knowledge=MakeLabel("StrataKnowledgeHint", hud, "", new Vector2(0,410), new Vector2(1740,52), 26, Ink);
        knowledge.gameObject.SetActive(false);
        RectTransform buttonRect = MakeBox("StrataSecondStageButton", artifactPanel, Vector2.zero,
            new Vector2(360, 88), Ink);
        Image buttonImage = buttonRect.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        StrataUIStyle.Round(buttonImage);
        secondStageButton = buttonRect.gameObject.AddComponent<Button>();
        secondStageButton.targetGraphic = buttonImage;
        MakeLabel("StrataSecondStageButtonText", buttonRect, "下一階段",
            Vector2.zero, new Vector2(344, 80), 30, Cream, true);
        secondStageButton.onClick.AddListener(RequestSecondStage);
        secondStageButton.gameObject.SetActive(false);
        BuildObservationUI(hud);
    }
    private void BuildObservationUI(RectTransform parent)
    {
        observationPanel=MakeBox("L4ObservationPanel",parent,new Vector2(0,-408),new Vector2(1240,220),ColorOf("C8A784"));
        StrataUIStyle.Round(observationPanel.GetComponent<Image>());
        var card=MakeBox("Card",observationPanel,Vector2.zero,new Vector2(1236,216),ColorOf("FAF4E9"));
        StrataUIStyle.Round(card.GetComponent<Image>());
        MakeLabel("Question",observationPanel,"生土層未發現文物，可能代表什麼？",
            new Vector2(0,75),new Vector2(1160,50),30,Ink,true);
        observationCorrect=MakeObservationButton("AnswerA",10,
            "A. 缺乏明顯的人類活動痕跡",true);
        observationIncorrect=MakeObservationButton("AnswerB",-58,
            "B. 生土層絕對不可能有文物",false);
        observationPanel.gameObject.SetActive(false);
    }
    private Button MakeObservationButton(string name,float y,string text,bool correct)
    {
        var rect=MakeBox(name,observationPanel,new Vector2(0,y),new Vector2(1140,54),Ink);
        var image=rect.GetComponent<Image>(); image.raycastTarget=true;
        StrataUIStyle.Round(image);
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        MakeLabel("Label",rect,text,Vector2.zero,new Vector2(1100,50),28,Cream);
        button.onClick.AddListener(()=>AnswerObservation(correct));
        button.interactable=false;
        return button;
    }
    // Presentation only; matching rules, progress and public events are unchanged.
    public void SetPresentationVisible(bool visible)
    {
        ClearDragHighlights();
        if (activeArtifact != null) activeArtifact.CancelDrag();
        if (stageOneHud != null) stageOneHud.gameObject.SetActive(visible);
        if (DragRoot != null) DragRoot.gameObject.SetActive(visible);
    }

    private static Color ColorOf(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color result);
        return result;
    }
    private static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
    private static RectTransform MakeBox(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = MakeRect(name, parent, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
    private TMP_Text MakeLabel(string name, Transform parent, string text, Vector2 position,
        Vector2 size, float fontSize, Color color, bool bold = false)
    {
        var label = MakeRect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = color;
        label.raycastTarget = false;
        label.lineSpacing = 6;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }
}
