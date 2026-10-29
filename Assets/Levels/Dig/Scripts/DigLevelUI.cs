using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A replaceable, level-local view. No brushing, scoring or historical data lives here.
public sealed class DigLevelUI : MonoBehaviour
{
    private static readonly Color Ink = Hex("243D32"), Cream = Hex("FAF4E9"),
        Forest = Hex("315D46"), Wood = Hex("735A46"), Dark = Hex("715B48"),
        Border = Hex("C8A784"), Gold = Hex("61452F"), Red = Hex("A34A3B"), White = Hex("FFF8E8");
    private TMP_FontAsset font;
    private DigLevelAudio sound;
    private Sprite rounded;
    private Texture2D roundedTexture;
    private RectTransform hud, overlay;
    private GameObject intro, pause, success, failure, knowledge;
    private TMP_Text successFound, successDamage, successTime, failureReason, cardName, cardBody, page;
    private Image cardImage;
    // Read the manager-owned labels; only presentation is split into HUD columns.
    private TMP_Text timerSource, progressSource, discoveryLabel, foundLabel, damageLabel;
    private RectTransform noticePlate;
    private Image noticePlateImage;
    private string lastProgressValue, lastTimerValue;
    private bool hasDisplayedSeconds, lowTime;
    private Button previous, next, finish;
    private DigArtifactCatalog catalog;
    private int currentPage;
    private const string BrushInstruction = "輕輕刷去泥土，快速刷動可能損傷文物。";
    private TMP_Text brushHelp;
    [Header("提示顯示時間（僅 UI，不影響損傷判定）")]
    [SerializeField, Min(2.5f)] private float damageNoticeSeconds = 2.5f;
    [SerializeField, Min(2f)] private float discoveryNoticeSeconds = 2f;
    private readonly DigUiNoticeState notices = new DigUiNoticeState();
    private DigLevelFlow owner;
    private TMP_Text statusLabel;
    private bool noticesEnabled;
    public bool HasPendingExcavationHints => notices.HasExcavation;
    public TMP_Text WarningLabel { get; private set; }
    public int KnowledgePage => currentPage;

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }

    public void Build(Canvas canvas, TMP_FontAsset existingFont, DigManager manager,
        DigArtifactCatalog data, DigLevelAudio audio, DigLevelFlow flow)
    {
        font = existingFont;
        catalog = data;
        sound = audio;
        owner = flow;
        MakeRoundedSprite();
        RectTransform background = Box("DigBackdrop", canvas.transform, 0, 0, 1920, 1080, Dark);
        Stretch(background);
        background.SetAsFirstSibling();
        // Decorations follow the translated DigArea; interaction dimensions remain 1300x750.
        RectTransform shadow = Box("DigAreaShadow", canvas.transform, 0, -30, 1332, 778, new Color(0, 0, 0, .08f));
        shadow.SetSiblingIndex(1);
        RectTransform frame = Box("DigAreaFrame", canvas.transform, 0, -23, 1316, 766, Border);
        frame.SetSiblingIndex(2);
        RectTransform rim = Box("DigAreaInnerFrame", canvas.transform, 0, -23, 1308, 758, Cream);
        rim.SetSiblingIndex(3);

        hud = Rect("DigHUD", canvas.transform, 0, 0, 0, 0);
        Stretch(hud);
        RectTransform header = Box("HUDHeader", hud, 0, -84, 1840, 160, Cream);
        Anchor(header, new Vector2(.5f, 1f));
        TMP_Text heading = Label("LevelTitle", hud, "考古小隊", -460, -42, 820, 68, 38, Ink, true);
        Anchor(heading.rectTransform, new Vector2(.5f, 1f));
        timerSource = manager.TimerLabel;
        progressSource = manager.ProgressLabel;
        discoveryLabel = manager.DiscoveryLabel;
        StyleExisting(timerSource, hud, new Vector2(1, 1), -690, -42, 520, 68, 34, Ink);
        if (timerSource != null) timerSource.fontStyle = FontStyles.Bold;
        // Keep the original progress reference alive for DigManager.UpdateUI().
        StyleExisting(progressSource, hud, new Vector2(1, 1), -660, -111, 640, 60, 32, Ink);
        if (progressSource != null) progressSource.enabled = false;
        foundLabel = Label("FoundProgress", hud, "已發現 0 / 3", 140, -111, 300, 60, 32, Ink, true);
        damageLabel = Label("DamageProgress", hud, "損傷 0 / 3", 470, -111, 300, 60, 32, Red, true);
        Anchor(foundLabel.rectTransform, new Vector2(.5f, 1));
        Anchor(damageLabel.rectTransform, new Vector2(.5f, 1));
        RectTransform pauseButton = Button("PauseButton", hud, "暫停", 0, 0, 170, 88, flow.PauseGame).GetComponent<RectTransform>();
        Anchor(pauseButton, new Vector2(1, 1));
        pauseButton.anchoredPosition = new Vector2(-145, -82);
        brushHelp = Label("BrushHelp", hud, BrushInstruction, -460, -113, 820, 60, 30, Wood);
        Anchor(brushHelp.rectTransform, new Vector2(.5f, 1));
        DigHintFade.SetVisible(brushHelp, true);
        noticePlate = Box("HUDNoticePlate", hud, 0, 64, 1440, 100, Cream);
        Anchor(noticePlate, new Vector2(.5f, 0));
        noticePlateImage = noticePlate.GetComponent<Image>();
        noticePlate.gameObject.SetActive(false);
        // Preserve manager-owned source references; render only the priority-selected message.
        StyleExisting(discoveryLabel, hud, new Vector2(.5f, 0), 0, 64, 1320, 68, 32, Wood);
        if (discoveryLabel != null) discoveryLabel.enabled = false;
        WarningLabel = Label("WarningText", hud, "請放慢刷動速度！", 0, 64, 1320, 68, 32, Red, true);
        Anchor(WarningLabel.rectTransform, new Vector2(.5f, 0));
        WarningLabel.enabled = false;
        WarningLabel.gameObject.SetActive(false);
        statusLabel = Label("PriorityNotice", hud, "", 0, 64, 1360, 68, 32, Wood, true);
        Anchor(statusLabel.rectTransform, new Vector2(.5f, 0));
        statusLabel.gameObject.SetActive(false);

        overlay = Rect("DigModalLayer", canvas.transform, 0, 0, 0, 0);
        Stretch(overlay);
        Image dim = overlay.gameObject.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, .40f);
        dim.raycastTarget = true;
        // The introduction is created here once by DigLevelFlow.Awake, not from a prefab.
        // Split the reading area into two columns so its width is actually used.
        Color introInk = Hex("243D32"), introBody = Hex("735A46");
        intro = Panel("IntroPanel", 1580, 752);
        Label("IntroTitle", intro.transform, "考古小隊｜刷出文化層", 0, 302, 1440, 80, 44, introInk, true);
        Label("IntroLocation", intro.transform, "2F 宜大記憶庫・展區二", 0, 230, 1440, 58, 30, Hex("614B3B"));
        Rule(intro.transform, 186);
        TMP_Text description = Label("IntroDescription", intro.transform,
            "化身考古小隊成員，尋找埋藏在泥土中的珍貴文物。", 0, 121, 1440, 80, 39, introBody);
        description.lineSpacing = 8;
        TMP_Text goalHeading = Label("IntroGoalHeading", intro.transform, "遊戲目標", -360, 2, 680, 68, 38, introInk, true);
        goalHeading.alignment = TextAlignmentOptions.Left;
        TMP_Text goals = Label("IntroTask", intro.transform,
            "• <indent=1em>60 秒內找出 3 件文物。</indent>\n• <indent=1em>清除泥土，露出約 70% 即完成發掘。</indent>", -360, -130, 680, 180, 34, introBody);
        goals.alignment = TextAlignmentOptions.TopLeft;
        goals.richText = true; // Preserve hanging indent on naturally wrapped lines.
        goals.lineSpacing = 10;
        goals.paragraphSpacing = 0;
        TMP_Text operationHeading = Label("IntroOperationHeading", intro.transform, "操作提醒", 360, 2, 680, 68, 38, introInk, true);
        operationHeading.alignment = TextAlignmentOptions.Left;
        // Both columns use the same text flow, dimensions and spacing.
        TMP_Text operation = Label("IntroOperation", intro.transform,
            "• <indent=1em>輕輕刷去泥土，逐步發掘文物。</indent>\n• <indent=1em>刷土過快會造成損傷，累積 3 次即失敗。</indent>", 360, -130, 680, 180, 34, introBody);
        operation.alignment = TextAlignmentOptions.TopLeft;
        operation.richText = true;
        operation.lineSpacing = 10;
        operation.paragraphSpacing = 0;
        Button("StartButton", intro.transform, "開始考古", 0, -298, 360, 88, flow.BeginChallenge);

        pause = Panel("PausePanel", 930, 470);
        Label("PauseTitle", pause.transform, "暫停遊戲", 0, 140, 800, 78, 42, Ink, true);
        Rule(pause.transform, 82);
        Label("PauseDescription", pause.transform, "準備好後，繼續挑戰吧！", 0, 0, 800, 80, 34, Wood);
        Button("ResumeButton", pause.transform, "繼續挑戰", 0, -135, 360, 88, flow.ResumeGame);

        success = Panel("SuccessPanel", 1320, 760);
        Label("SuccessTitle", success.transform, "考古完成", 0, 275, 1200, 80, 42, Ink, true);
        Rule(success.transform, 216);
        Label("SuccessDescription", success.transform, "成功發掘三件文物！", 0, 164, 1200, 68, 36, Ink, true);
        TMP_Text successStory = Label("SuccessStory", success.transform,
            "接下來，一起探索文物背後的文化故事。", 0, 52, 1200, 72, 32, Wood);
        successStory.lineSpacing = 6;
        successFound = Stat(success.transform, "FoundStat", "發現文物", -400, Ink);
        successDamage = Stat(success.transform, "DamageStat", "損傷次數", 0, Red);
        successTime = Stat(success.transform, "TimeStat", "剩餘時間", 400, Ink);
        Button("KnowledgeButton", success.transform, "查看文物知識", 0, -280, 360, 88, flow.OpenKnowledge);

        failure = Panel("FailurePanel", 1100, 490);
        Label("FailureTitle", failure.transform, "挑戰失敗", 0, 150, 980, 76, 42, Red, true);
        Rule(failure.transform, 92);
        failureReason = Label("FailureReason", failure.transform, "", 0, 0, 980, 100, 34, Wood);
        Button("RetryButton", failure.transform, "重新挑戰", 0, -144, 360, 88, flow.RetryChallenge);

        knowledge = Panel("KnowledgePanel", 1520, 840);
        Label("KnowledgeTitle", knowledge.transform, "文物知識", -180, 340, 1080, 76, 42, Ink, true);
        page = Label("KnowledgePage", knowledge.transform, "1 / 3", 650, 340, 160, 64, 30, Wood);
        Rule(knowledge.transform, 286);
        Box("ArtifactMat", knowledge.transform, -485, -8, 430, 460, Hex("EEE3CE"));
        RectTransform illustration = Rect("ArtifactIllustration", knowledge.transform, -485, -8, 390, 410);
        cardImage = illustration.gameObject.AddComponent<Image>();
        cardImage.preserveAspect = true;
        cardImage.raycastTarget = false;
        cardName = Label("ArtifactName", knowledge.transform, "", 200, 162, 900, 70, 38, Ink, true);
        cardName.alignment = TextAlignmentOptions.Left;
        cardBody = Label("ArtifactIntroduction", knowledge.transform, "", 200, -80, 900, 366, 36, Wood);
        cardBody.alignment = TextAlignmentOptions.TopLeft;
        cardBody.richText = true;
        cardBody.lineSpacing = 0; // Display formatting sets 1.5em baseline spacing.
        cardBody.paragraphSpacing = 6; // Small sentence gaps; no blank line before the illustration notice.
        cardBody.characterSpacing = 0;
        previous = Button("PreviousArtifact", knowledge.transform, "上一件", -400, -330, 360, 88, () => ShowKnowledge(currentPage - 1));
        next = Button("NextArtifact", knowledge.transform, "下一件", 0, -330, 360, 88, () => ShowKnowledge(currentPage + 1));
        finish = Button("FinishLevel", knowledge.transform, "完成關卡", 400, -330, 360, 88, flow.FinishLevel);
        ShowIntro();
    }

    private void LateUpdate()
    {
        if (hud == null || !hud.gameObject.activeInHierarchy) return;
        RefreshHudPresentation();
    }

    private void RefreshHudPresentation()
    {
        string raw = progressSource != null ? progressSource.text ?? "" : "";
        if (raw != lastProgressValue)
        {
            lastProgressValue = raw;
            int divider = raw.IndexOf('　');
            foundLabel.text = (divider >= 0 ? raw.Substring(0, divider) : raw).Replace("找到", "已發現");
            damageLabel.text = divider >= 0 ? raw.Substring(divider + 1).Trim().Replace("損壞", "損傷") : "";
        }
        if (timerSource != null)
        {
            string displayed = timerSource.text ?? "";
            if (displayed != lastTimerValue)
            {
                lastTimerValue = displayed;
                int colon = displayed.IndexOf('：');
                int unit = displayed.LastIndexOf(" 秒", StringComparison.Ordinal);
                int seconds = 0;
                hasDisplayedSeconds = colon >= 0 && unit > colon &&
                    int.TryParse(displayed.Substring(colon + 1, unit - colon - 1), out seconds);
                lowTime = hasDisplayedSeconds && seconds <= 10;
            }
            if (hasDisplayedSeconds) timerSource.color = lowTime ? Red : Ink;
        }
        bool fast = WarningLabel != null && WarningLabel.gameObject.activeSelf && WarningLabel.color.a > .01f;
        bool running = noticesEnabled && owner != null && owner.CurrentScreen == DigLevelFlow.Screen.Playing;
        notices.Tick(Time.unscaledDeltaTime, running, fast);
        RenderNotice();
    }

    private void RenderNotice()
    {
        bool visible = noticesEnabled && notices.Kind != DigUiNoticeKind.None;
        if (visible)
        {
            statusLabel.text = notices.Message;
            Color hue = notices.Kind == DigUiNoticeKind.Damage ? Hex("A34626") :
                notices.Kind == DigUiNoticeKind.FastBrush ? Red : Wood;
            hue.a = statusLabel.color.a;
            statusLabel.color = hue;
        }
        DigHintFade.SetVisible(statusLabel, visible);
        Color plate = notices.Kind == DigUiNoticeKind.Damage ? Hex("F5E2D0") : Cream;
        plate.a = statusLabel.gameObject.activeSelf ? statusLabel.color.a : 0f;
        noticePlateImage.color = plate;
        noticePlate.gameObject.SetActive(plate.a > .01f);
    }

    public void ShowDiscoveryNotice()
    {
        if (!noticesEnabled) return;
        notices.ShowDiscovery("發現文物！請小心清理", discoveryNoticeSeconds);
    }

    public void ShowDamageNotice(Sprite artifact)
    {
        if (!noticesEnabled) return;
        string name = "文物";
        if (artifact != null && catalog != null && catalog.entries != null)
            foreach (var entry in catalog.entries)
                if (entry != null && entry.illustration == artifact) { name = entry.displayName; break; }
        notices.ShowDamage(name + "受損！請放慢刷動速度。", damageNoticeSeconds);
    }

    public void StopGameplayNotices()
    {
        noticesEnabled = false;
        notices.Reset();
        if (statusLabel != null)
        {
            statusLabel.text = "";
            Color color = statusLabel.color; color.a = 0; statusLabel.color = color;
            DigHintFade.SetVisible(statusLabel, false);
            statusLabel.gameObject.SetActive(false);
        }
        if (noticePlate != null) noticePlate.gameObject.SetActive(false);
        if (WarningLabel != null) WarningLabel.gameObject.SetActive(false);
        if (discoveryLabel != null) discoveryLabel.gameObject.SetActive(false);
    }

    private static string FailureCaption(string reason)
    {
        // Translate the existing result reason for display; never infer a new outcome.
        if (!string.IsNullOrEmpty(reason) && reason.Contains("時間"))
            return "時間已到，未能完成文物發掘。";
        if (!string.IsNullOrEmpty(reason) && reason.Contains("損傷"))
            return "文物損傷累積三次，挑戰失敗。";
        return string.IsNullOrEmpty(reason) ? "本次未能完成挑戰。" : reason;
    }

    public void QueueExcavationHint(string message)
    {
        if (noticesEnabled && !string.IsNullOrEmpty(message)) notices.QueueExcavation(message);
    }

    private void StyleExisting(TMP_Text text, Transform parent, Vector2 anchor,
        float x, float y, float width, float height, float size, Color color)
    {
        if (text == null) return;
        text.rectTransform.SetParent(parent, false);
        Anchor(text.rectTransform, anchor);
        text.rectTransform.anchoredPosition = new Vector2(x, y);
        text.rectTransform.sizeDelta = new Vector2(width, height);
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
    }

    private GameObject Panel(string name, float width, float height)
    {
        RectTransform border = Box(name, overlay, 0, 0, width + 4, height + 4, Border);
        Box(name + "Card", border, 0, 0, width, height, Cream);
        return border.gameObject;
    }

    private void Rule(Transform parent, float y) => Box("AccentRule", parent, 0, y, 170, 2, Border);

    private void ShowPanel(GameObject selected)
    {
        foreach (GameObject panel in new[] { intro, pause, success, failure, knowledge })
            if (panel != null) panel.SetActive(panel == selected);
        // Non-playing cards use the full-screen environment color, rather than exposing
        // a cropped strip of the excavation through the old 40% black dimmer.
        // Keep the existing translucent pause treatment. No excavation state is changed.
        overlay.GetComponent<Image>().color = selected == pause ? new Color(0, 0, 0, .40f) : Dark;
        overlay.gameObject.SetActive(selected != null);
        hud.gameObject.SetActive(selected == null);
    }
    public void ResetForNewChallenge()
    {
        StopGameplayNotices();
        currentPage=0;
        ShowIntro();
    }
    public void ShowIntro() { StopGameplayNotices(); ShowPanel(intro); }
    public void ShowPlaying() { noticesEnabled = true; ShowPanel(null); }
    public void ShowPause() => ShowPanel(pause);
    public void ShowResult(DigLevelResult result)
    {
        StopGameplayNotices();
        if (result.success)
        {
            successFound.text = $"{result.foundCount} / 3";
            successDamage.text = $"{result.damageCount} / 3";
            successTime.text = $"{Mathf.CeilToInt(result.remainingSeconds)} 秒";
            ShowPanel(success);
        }
        else
        {
            failureReason.text = FailureCaption(result.failureReason);
            ShowPanel(failure);
        }
    }
    public void ShowKnowledge(int index)
    {
        int count = catalog != null && catalog.entries != null ? catalog.entries.Length : 0;
        if (count == 0) return;
        currentPage = Mathf.Clamp(index, 0, count - 1);
        DigArtifactCatalog.Entry entry = catalog.entries[currentPage];
        cardName.text = entry.displayName;
        bool pottery = string.Equals(entry.id, "pottery", StringComparison.Ordinal);
        cardBody.paragraphSpacing = pottery ? 12 : 6;
        cardBody.text = pottery ? FormatPotteryKnowledgeText() : FormatKnowledgeText(entry.introduction);
        cardImage.sprite = entry.illustration;
        cardImage.enabled = entry.illustration != null;
        page.text = $"{currentPage + 1} / {count}";
        previous.interactable = currentPage > 0;
        next.interactable = currentPage < count - 1;
        finish.interactable = currentPage == count - 1;
        ShowPanel(knowledge);
    }
    public void ShowIntegrationComplete()
    {
        finish.interactable = false;
    }

    private static string FormatPotteryKnowledgeText()
    {
        // Display-only concise copy; the full introduction and official sources remain in the catalog.
        return "<line-height=1.5em>舊社時期的代表性陶器之一。先民利用黏土製作陶器，並在表面刻畫幾何紋飾，展現當時的製陶工藝<nobr>與生活文化。</nobr></line-height>";
    }

    private static string FormatKnowledgeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Only presentation changes: catalog wording, punctuation and source data stay intact.
        string[] paragraphs = text.Replace("\r\n", "\n").Split(new[] { "\n\n" }, StringSplitOptions.None);
        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i].Replace("\n", "");
            string[] sentences = paragraph.Split(new[] { '。' }, StringSplitOptions.None);
            var lines = new List<string>();
            for (int j = 0; j < sentences.Length; j++)
            {
                string sentence = sentences[j] + (j < sentences.Length - 1 ? "。" : "");
                if (sentence.Length == 0) continue;
                // Keep a short phrase together, so a final character/punctuation cannot stand alone.
                int tail = Mathf.Max(0, sentence.Length - 6);
                lines.Add(sentence.Substring(0, tail) + "<nobr>" + sentence.Substring(tail) + "</nobr>");
            }
            string content = string.Join("\n", lines);
            bool illustrationNotice = paragraph.StartsWith("圖像為遊戲", StringComparison.Ordinal);
            paragraphs[i] = illustrationNotice
                ? "<size=90%><line-height=1.5em>" + content + "</line-height></size>"
                : "<line-height=1.5em>" + content + "</line-height>";
        }
        // One paragraph break separates the notice, rather than an extra empty text line.
        return string.Join("\n", paragraphs);
    }

    private TMP_Text Stat(Transform parent, string name, string caption, float x, Color valueColor)
    {
        RectTransform card = Box(name, parent, x, -100, 360, 148, Hex("F1E7D5"));
        Label(name + "Caption", card, caption, 0, 36, 336, 48, 26, Wood);
        return Label(name + "Value", card, "", 0, -26, 336, 68, 38, valueColor, true);
    }

    private RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Anchor(RectTransform rect, Vector2 value) => rect.anchorMin = rect.anchorMax = value;
    private RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        RectTransform rect = Rect(name, parent, x, y, w, h);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
    private TMP_Text Label(string name, Transform parent, string text, float x, float y,
        float width, float height, float size, Color color, bool bold = false)
    {
        RectTransform rect = Rect(name, parent, x, y, width, height);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.color = color;
        label.raycastTarget = false;
        label.margin = new Vector4(12, 5, 12, 5);
        return label;
    }
    private Button Button(string name, Transform parent, string caption, float x, float y, float w, float h, Action action)
    {
        RectTransform rect = Box(name, parent, x, y, w, h, Forest);
        Image graphic = rect.GetComponent<Image>();
        graphic.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        ColorBlock colors = button.colors;
        graphic.color = Color.white;
        colors.normalColor = Forest;
        colors.highlightedColor = Hex("44725A");
        colors.pressedColor = Hex("254835");
        colors.selectedColor = Forest;
        colors.disabledColor = Hex("A69A88");
        button.colors = colors;
        Label(name + "Label", rect, caption, 0, 0, w - 16, h - 8, 30, White, true);
        button.onClick.AddListener(() => { if (sound != null) sound.Click(); action?.Invoke(); });
        return button;
    }
    private void MakeRoundedSprite()
    {
        const int size = 64;
        roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        roundedTexture.name = "DigRuntimeRoundedCard";
        roundedTexture.wrapMode = TextureWrapMode.Clamp;
        roundedTexture.filterMode = FilterMode.Bilinear;
        Color32[] colors = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, 13f - Mathf.Min(x, size - 1 - x));
                float dy = Mathf.Max(0, 13f - Mathf.Min(y, size - 1 - y));
                byte alpha = (byte)(Mathf.Clamp01(13.5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                colors[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        roundedTexture.SetPixels32(colors);
        roundedTexture.Apply(false, true);
        rounded = Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
    }
    void OnDestroy()
    {
        if (rounded != null) Destroy(rounded);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}

// Display-only state. Timers consume time only for the visible priority while playing.
internal enum DigUiNoticeKind { None, Excavation, FastBrush, Discovery, Damage }
internal sealed class DigUiNoticeState
{
    private readonly Queue<string> pending = new Queue<string>();
    private string damage = "", discovery = "", excavation = "";
    private float damageLeft, discoveryLeft, excavationLeft;
    private bool damageFresh, discoveryFresh, excavationFresh, fast;
    public bool HasExcavation => excavationLeft > 0 || pending.Count > 0;
    public DigUiNoticeKind Kind => damageLeft > 0 ? DigUiNoticeKind.Damage :
        fast ? DigUiNoticeKind.FastBrush : discoveryLeft > 0 ? DigUiNoticeKind.Discovery :
        excavationLeft > 0 ? DigUiNoticeKind.Excavation : DigUiNoticeKind.None;
    public string Message => Kind == DigUiNoticeKind.Damage ? damage :
        Kind == DigUiNoticeKind.Discovery ? discovery : Kind == DigUiNoticeKind.FastBrush ? "請放慢刷動速度！" :
        Kind == DigUiNoticeKind.Excavation ? excavation : "";
    public void ShowDamage(string message, float seconds)
    { damage = message; damageLeft = Math.Max(2.5f, seconds); damageFresh = true; }
    public void ShowDiscovery(string message, float seconds)
    { discovery = message; discoveryLeft = Math.Max(2f, seconds); discoveryFresh = true; }
    public void QueueExcavation(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        pending.Enqueue(message);
        if (excavationLeft <= 0) NextExcavation();
    }
    private void NextExcavation()
    {
        excavation = pending.Count > 0 ? pending.Dequeue() : "";
        excavationLeft = excavation.Length > 0 ? 1.5f : 0f;
        excavationFresh = true;
    }
    public void Tick(float delta, bool running, bool fastWarning)
    {
        fast = running && fastWarning;
        if (!running) return;
        delta = Math.Max(0, delta);
        if (damageLeft > 0)
        {
            if (damageFresh) { damageFresh = false; return; }
            damageLeft = Math.Max(0, damageLeft - delta); return;
        }
        if (fast) return;
        if (discoveryLeft > 0)
        {
            if (discoveryFresh) { discoveryFresh = false; return; }
            discoveryLeft = Math.Max(0, discoveryLeft - delta); return;
        }
        if (excavationLeft > 0)
        {
            if (excavationFresh) { excavationFresh = false; return; }
            excavationLeft = Math.Max(0, excavationLeft - delta);
            if (excavationLeft <= 0) NextExcavation();
        }
    }
    public void Reset()
    {
        pending.Clear(); damage = discovery = excavation = "";
        damageLeft = discoveryLeft = excavationLeft = 0;
        damageFresh = discoveryFresh = excavationFresh = fast = false;
    }
}
