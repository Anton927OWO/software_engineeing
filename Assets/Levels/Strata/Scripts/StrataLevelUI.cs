using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class StrataLevelUI : MonoBehaviour
{
    [System.Serializable]
    private sealed class KnowledgePage
    {
        public string title;
        public Sprite image;
        [TextArea(3, 8)] public string body;
        [TextArea(2, 5)] public string source;
    }
    [SerializeField] private StrataKnowledgeCatalog knowledgeCatalog;
    [SerializeField] private KnowledgePage[] knowledgePages = new KnowledgePage[0];
    private Image knowledgeImage;
    private TMP_Text knowledgeName, knowledgeBody, knowledgePageLabel;
    private Button knowledgePrevious, knowledgeNext, knowledgeFinish;
    private int knowledgeIndex;
    private bool knowledgeFinished;
    private RectTransform root;
    private GameObject hud, modal, intro, pause, success, failure, knowledge;
    private TMP_Text timer, failureTime, failureMistakes, successTime, successMistakes, successStageOne, successStageTwo;
    private GameObject layerHintPanel;
    private Button layerHintButton;
    private RectTransform pauseButton;
    private bool layerHintOpen;
    private TMP_FontAsset font;
    private StrataLevelFlow flow;
    private static readonly Color Cream = new Color32(250,244,233,255);
    private static readonly Color Forest = new Color32(32,61,50,255);
    private static readonly Color Wood = new Color32(120,85,56,255);
    public bool IsBuilt => root != null;
    public void Build(Canvas canvas, TMP_FontAsset fontAsset, StrataLevelFlow owner)
    {
        if (IsBuilt) return;
        font = fontAsset; flow = owner;
        EnsureKnowledgeData();
        root = Box("StrataFlowUI", canvas.transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        hud = Box("SharedTimerHUD", root, Vector2.zero, Vector2.zero).gameObject;
        var hr = (RectTransform)hud.transform;
        hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.offsetMin = hr.offsetMax = Vector2.zero;
        var timeCard = Box("TimerCard", hr, new Vector2(400,480), new Vector2(280,64));
        Paint(timeCard, Cream, false); StrataUIStyle.Round(timeCard.GetComponent<Image>());
        timer = Label("Time", timeCard, Vector2.zero, new Vector2(270,60), "", 30, Forest);
        pauseButton=Button(hr, "Pause", new Vector2(835,480), new Vector2(170,76), "暫停", flow.PauseGame).GetComponent<RectTransform>();
        layerHintButton=Button(hr,"LayerHintButton",new Vector2(645,480),new Vector2(170,76),"提示",ToggleLayerHint);
        layerHintPanel=Card("LayerHintPanel",hr,new Vector2(380,280));
        ((RectTransform)layerHintPanel.transform).anchoredPosition=new Vector2(754,125);
        var hintTitle=Label("Title",layerHintPanel.transform,new Vector2(0,94),new Vector2(360,60),"地層提示",32,Forest,true);
        hintTitle.alignment=TextAlignmentOptions.Left;
        hintTitle.margin=new Vector4(10,4,10,4);
        var hint=Label("Pairings",layerHintPanel.transform,new Vector2(0,-32),new Vector2(360,180),
            "磚瓦殘片 → 表土層\n玻璃珠 → 堆積土層\n印紋陶片 → 舊社文化層",32,new Color32(115,90,70,255));
        hint.alignment=TextAlignmentOptions.TopLeft; hint.lineSpacing=10;
        hint.margin=new Vector4(10,6,10,6);
        layerHintPanel.SetActive(false);
        layerHintButton.gameObject.SetActive(false);
        var mr = Box("FlowModal", root, Vector2.zero, Vector2.zero);
        mr.anchorMin=Vector2.zero; mr.anchorMax=Vector2.one; mr.offsetMin=mr.offsetMax=Vector2.zero;
        Paint(mr,new Color(0,0,0,.4f),true); modal=mr.gameObject;
        intro=Card("Introduction",mr,new Vector2(1480,760));
        Color introBody=new Color32(115,90,70,255);
        Label("Title",intro.transform,new Vector2(0,286),new Vector2(1360,76),"地層解碼",44,Forest,true);
        Label("Location",intro.transform,new Vector2(0,218),new Vector2(1360,58),
            "行政大樓 B1 萬斌廳・宜蘭農校遺址文化層",33,introBody);
        var introRule=Box("IntroAccentRule",intro.transform,new Vector2(0,170),new Vector2(170,2));
        Paint(introRule,new Color32(200,167,132,255),false);
        Label("Content",intro.transform,new Vector2(0,112),new Vector2(1320,70),
            "透過地層與陶器紋飾，認識宜蘭考古文化。",36,introBody);
        var stageOneIntro=Box("StageOneIntroduction",intro.transform,new Vector2(-350,-40),new Vector2(640,168));
        Paint(stageOneIntro,new Color32(241,231,213,255),false);
        StrataUIStyle.Round(stageOneIntro.GetComponent<Image>());
        var stageOneHeading=Label("Heading",stageOneIntro,new Vector2(0,42),new Vector2(584,60),"文物歸層",34,Forest,true);
        stageOneHeading.alignment=TextAlignmentOptions.Left;
        var stageOneDescription=Label("Description",stageOneIntro,new Vector2(0,-24),new Vector2(584,72),
            "將文物放入對應地層，完成觀察題。",35,introBody);
        stageOneDescription.alignment=TextAlignmentOptions.TopLeft;
        var stageTwoIntro=Box("StageTwoIntroduction",intro.transform,new Vector2(350,-40),new Vector2(640,168));
        Paint(stageTwoIntro,new Color32(241,231,213,255),false);
        StrataUIStyle.Round(stageTwoIntro.GetComponent<Image>());
        var stageTwoHeading=Label("Heading",stageTwoIntro,new Vector2(0,42),new Vector2(584,60),"紋飾配對",34,Forest,true);
        stageTwoHeading.alignment=TextAlignmentOptions.Left;
        var stageTwoDescription=Label("Description",stageTwoIntro,new Vector2(0,-24),new Vector2(584,72),
            "找出相同的陶器紋飾，完成配對。",35,introBody);
        stageTwoDescription.alignment=TextAlignmentOptions.TopLeft;
        Label("Reminder",intro.transform,new Vector2(0,-185),new Vector2(1320,56),
            "兩階段共用 60 秒，配對錯誤會記錄失誤。",30,introBody);
        Button(intro.transform,"Start",new Vector2(0,-296),new Vector2(360,88),"開始挑戰",flow.BeginChallenge);
        pause=Card("Pause",mr,new Vector2(930,470));
        Label("Title",pause.transform,new Vector2(0,138),new Vector2(830,80),"遊戲暫停",42,Forest,true);
        Label("Content",pause.transform,new Vector2(0,0),new Vector2(830,80),"準備好後，繼續挑戰吧！",30,Wood);
        Button(pause.transform,"Resume",new Vector2(0,-134),new Vector2(360,88),"繼續遊戲",flow.ResumeGame);
        success=Card("Success",mr,new Vector2(1320,860));
        Label("Title",success.transform,new Vector2(0,326),new Vector2(1200,80),"地層解碼完成",42,Forest,true);
        var resultRule=Box("ResultRule",success.transform,new Vector2(0,266),new Vector2(170,2));
        Paint(resultRule,new Color32(200,167,132,255),false);
        Label("SuccessMessage",success.transform,new Vector2(0,162),new Vector2(1200,64),"成功完成考古挑戰！",36,Forest,true);
        successTime=SuccessStat("RemainingTime",new Vector2(-270,12),"剩餘時間");
        successMistakes=SuccessStat("TotalMistakes",new Vector2(270,12),"總失誤次數");
        successStageOne=SuccessStat("LayerMistakes",new Vector2(-270,-148),"第一階段失誤");
        successStageTwo=SuccessStat("PatternMistakes",new Vector2(270,-148),"紋飾配錯次數");
        var resultKnowledge=Button(success.transform,"Retry",new Vector2(0,-324),new Vector2(360,88),"查看文物知識",flow.RequestKnowledge);
        StyleKnowledgeButton(resultKnowledge);
        failure=Card("Failure",mr,new Vector2(1100,660));
        Label("Title",failure.transform,new Vector2(0,240),new Vector2(980,80),"挑戰失敗",42,new Color32(145,67,55,255),true);
        var failureRule=Box("FailureRule",failure.transform,new Vector2(0,184),new Vector2(170,2));
        Paint(failureRule,new Color32(200,167,132,255),false);
        // The existing flow has only a time-expiry failure; do not infer a new outcome.
        Label("Content",failure.transform,new Vector2(0,120),new Vector2(980,72),"時間已到，未能完成考古挑戰。",32,new Color32(115,90,70,255));
        failureTime=FailureStat("RemainingTime",new Vector2(-220,-36),"剩餘時間");
        failureMistakes=FailureStat("TotalMistakes",new Vector2(220,-36),"總失誤次數");
        var failureRetry=Button(failure.transform,"Retry",new Vector2(0,-232),new Vector2(360,88),"重新挑戰",flow.RetryChallenge);
        StyleKnowledgeButton(failureRetry);
        knowledge=Card("Knowledge",mr,new Vector2(1520,840));
        Color knowledgeWood=new Color32(115,90,70,255);
        Label("Title",knowledge.transform,new Vector2(-180,340),new Vector2(1080,76),"考古知識卡",42,Forest,true);
        knowledgePageLabel=Label("KnowledgePage",knowledge.transform,new Vector2(650,340),new Vector2(160,64),"1 / 3",30,knowledgeWood);
        var knowledgeRule=Box("KnowledgeRule",knowledge.transform,new Vector2(0,286),new Vector2(170,2));
        Paint(knowledgeRule,new Color32(200,167,132,255),false);
        var mat=Box("ArtifactMat",knowledge.transform,new Vector2(-485,-8),new Vector2(430,460));
        Paint(mat,new Color32(238,227,206,255),false); StrataUIStyle.Round(mat.GetComponent<Image>());
        var picture=Box("ArtifactIllustration",knowledge.transform,new Vector2(-485,-8),new Vector2(390,410));
        Paint(picture,Color.white,false); knowledgeImage=picture.GetComponent<Image>(); knowledgeImage.preserveAspect=true;
        knowledgeName=Label("ArtifactName",knowledge.transform,new Vector2(200,162),new Vector2(900,70),"",38,Forest,true);
        knowledgeName.alignment=TextAlignmentOptions.Left;
        knowledgeBody=Label("ArtifactIntroduction",knowledge.transform,new Vector2(200,-62),new Vector2(900,330),"",36,knowledgeWood);
        knowledgeBody.alignment=TextAlignmentOptions.TopLeft;
        knowledgeBody.richText=true; knowledgeBody.lineSpacing=0; knowledgeBody.paragraphSpacing=10;
        knowledgeBody.margin=new Vector4(12,5,12,5);
        knowledgePrevious=Button(knowledge.transform,"PreviousArtifact",new Vector2(-400,-330),new Vector2(360,88),"上一件",()=>ShowKnowledgePage(knowledgeIndex-1));
        knowledgeNext=Button(knowledge.transform,"NextArtifact",new Vector2(0,-330),new Vector2(360,88),"下一件",()=>ShowKnowledgePage(knowledgeIndex+1));
        knowledgeFinish=Button(knowledge.transform,"Continue",new Vector2(400,-330),new Vector2(360,88),"完成關卡",FinishKnowledge);
        StyleKnowledgeButton(knowledgePrevious); StyleKnowledgeButton(knowledgeNext); StyleKnowledgeButton(knowledgeFinish);
    }
    private TMP_Text SuccessStat(string name, Vector2 position, string caption)
    {
        var card=Box(name,success.transform,position,new Vector2(500,140));
        Paint(card,new Color32(241,231,213,255),false); StrataUIStyle.Round(card.GetComponent<Image>());
        Label("Caption",card,new Vector2(0,37),new Vector2(468,46),caption,26,new Color32(115,90,70,255));
        return Label("Value",card,new Vector2(0,-23),new Vector2(468,64),"",36,Forest,true);
    }
    private TMP_Text FailureStat(string name, Vector2 position, string caption)
    {
        var card=Box(name,failure.transform,position,new Vector2(400,136));
        Paint(card,new Color32(241,231,213,255),false); StrataUIStyle.Round(card.GetComponent<Image>());
        Label("Caption",card,new Vector2(0,34),new Vector2(368,46),caption,26,new Color32(115,90,70,255));
        return Label("Value",card,new Vector2(0,-24),new Vector2(368,64),"",36,Forest,true);
    }
    private static void StyleKnowledgeButton(Button button)
    {
        button.targetGraphic.color=Color.white;
        var colors=button.colors;
        colors.normalColor=new Color32(49,93,70,255);
        colors.highlightedColor=new Color32(68,114,90,255);
        colors.pressedColor=new Color32(37,72,53,255);
        colors.selectedColor=colors.normalColor;
        colors.disabledColor=new Color32(166,154,136,255);
        button.colors=colors;
    }
    // Explicit initialization also supports a Scene already open before the catalog field was added.
    private void EnsureKnowledgeData()
    {
        if (knowledgeCatalog == null)
            knowledgeCatalog = Resources.Load<StrataKnowledgeCatalog>(StrataKnowledgeCatalog.ResourcePath);
        if (knowledgeCatalog == null || !knowledgeCatalog.HasCompletePages) return;
        knowledgePages = new KnowledgePage[knowledgeCatalog.entries.Length];
        for (int i = 0; i < knowledgePages.Length; i++)
        {
            var entry = knowledgeCatalog.entries[i];
            knowledgePages[i] = new KnowledgePage
            {
                title = entry.title, image = entry.image, body = entry.body, source = entry.source
            };
        }
    }
    private void ShowKnowledgePage(int index)
    {
        if (knowledgeFinished) return;
        int count=knowledgePages!=null ? knowledgePages.Length : 0;
        if (count==0)
        {
            knowledgeName.text="知識卡資料尚未設定"; knowledgeBody.text="";
            knowledgeImage.enabled=false; knowledgePageLabel.text="0 / 0";
            knowledgePrevious.interactable=knowledgeNext.interactable=knowledgeFinish.interactable=false;
            return;
        }
        knowledgeIndex=Mathf.Clamp(index,0,count-1);
        var entry=knowledgePages[knowledgeIndex];
        knowledgeName.text=entry!=null ? entry.title : "";
        knowledgeBody.text=entry!=null ? "<line-height=1.5em>"+(entry.body??"").Replace("\r\n","\n").Replace("\n\n","\n")+"</line-height>" : "";
        knowledgeImage.sprite=entry!=null ? entry.image : null;
        knowledgeImage.enabled=knowledgeImage.sprite!=null;
        knowledgePageLabel.text=$"{knowledgeIndex+1} / {count}";
        knowledgePrevious.interactable=knowledgeIndex>0;
        knowledgeNext.interactable=knowledgeIndex<count-1;
        knowledgeFinish.interactable=knowledgeIndex==count-1;
    }
    private void FinishKnowledge()
    {
        if (knowledgeFinished || knowledgePages==null || knowledgePages.Length==0 ||
            knowledgeIndex!=knowledgePages.Length-1 || flow==null || flow.CurrentScreen!=StrataLevelFlow.Screen.Knowledge) return;
        knowledgeFinished=true;
        knowledgeFinish.interactable=false;
        // Final knowledge page submits once; the successful result panel was already shown.
        flow.ContinueFromKnowledge();
    }
    private void ToggleLayerHint()
    {
        if(flow==null || !flow.CanShowLayerHint || !flow.CanUseInput())
        { SetLayerHintAvailability(false); return; }
        layerHintOpen=!layerHintOpen;
        layerHintPanel.SetActive(layerHintOpen);
    }
    public void SetLayerHintAvailability(bool available)
    {
        if(layerHintButton==null || layerHintPanel==null) return;
        layerHintButton.gameObject.SetActive(available);
        if(!available) layerHintOpen=false;
        layerHintPanel.SetActive(available && layerHintOpen);
    }
    public void ShowKnowledge() { EnsureKnowledgeData(); knowledgeFinished=false; ShowKnowledgePage(0); Show(knowledge); }
    public void ShowIntroduction() { knowledgeIndex=0; knowledgeFinished=false; Show(intro); }
    public void ShowPause() { Show(pause); }
    public void ShowRunning() { Show(null); }
    public void ShowResult(StrataLevelResult result)
    {
        successTime.text=$"{Mathf.CeilToInt(result.remainingSeconds)} 秒";
        successMistakes.text=$"{result.TotalMistakes} 次";
        successStageOne.text=$"{result.stageOneMistakes} 次";
        successStageTwo.text=$"{result.stageTwoMistakes} 次";
        failureTime.text=$"{Mathf.CeilToInt(result.remainingSeconds)} 秒";
        failureMistakes.text=$"{result.TotalMistakes} 次";
        Show(result.success ? success : failure);
    }
    public void SetTime(float remaining)
    {
        timer.text=$"剩餘時間：{Mathf.CeilToInt(remaining)} 秒";
        timer.color=remaining<=10 ? new Color32(171,55,45,255) : Forest;
    }
    public void BringToFront()
    {
        if (pauseButton != null && flow != null)
        {
            if (flow.CurrentScreen == StrataLevelFlow.Screen.StageTwo)
                pauseButton.anchoredPosition = new Vector2(817, pauseButton.anchoredPosition.y);
            else if (flow.CurrentScreen == StrataLevelFlow.Screen.StageOne || flow.CurrentScreen == StrataLevelFlow.Screen.Introduction)
                pauseButton.anchoredPosition = new Vector2(835, pauseButton.anchoredPosition.y);
        }
        if(root!=null && root.GetSiblingIndex()!=root.parent.childCount-1) root.SetAsLastSibling();
    }
    private void Show(GameObject panel)
    {
        // Every screen transition closes the optional aid; resume never reopens it.
        SetLayerHintAvailability(false);
        intro.SetActive(panel==intro); pause.SetActive(panel==pause);
        success.SetActive(panel==success); failure.SetActive(panel==failure);
        knowledge.SetActive(panel==knowledge);
        // Modal results cover gameplay; the translucent pause treatment stays unchanged.
        modal.GetComponent<Image>().color=(panel==intro || panel==knowledge || panel==success || panel==failure) ? new Color32(113,91,72,255) : new Color(0,0,0,.4f);
        modal.SetActive(panel!=null); hud.SetActive(panel==null); BringToFront();
    }
    private GameObject Card(string name, Transform parent, Vector2 size)
    {
        var border=Box(name,parent,Vector2.zero,size); Paint(border,new Color32(200,167,132,255),false);
        StrataUIStyle.Round(border.GetComponent<Image>());
        var inner=Box("Card",border,Vector2.zero,size-new Vector2(4,4)); Paint(inner,Cream,false);
        StrataUIStyle.Round(inner.GetComponent<Image>());
        return border.gameObject;
    }
    private static RectTransform Box(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
    }
    private static void Paint(RectTransform rect,Color color,bool raycast)
    { var image=rect.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=raycast; }
    private TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string text,float fontSize,Color color,bool bold=false)
    {
        var rect=Box(name,parent,pos,size); var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font; label.text=text; label.fontSize=fontSize; label.color=color;
        label.fontStyle=bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment=TextAlignmentOptions.Center; label.lineSpacing=4; label.raycastTarget=false;
        label.enableAutoSizing=false; label.textWrappingMode=TextWrappingModes.Normal;
        label.overflowMode=TextOverflowModes.Truncate; label.margin=new Vector4(8,4,8,4); return label;
    }
    private Button Button(Transform parent,string name,Vector2 pos,Vector2 size,string text,UnityEngine.Events.UnityAction action)
    {
        var rect=Box(name,parent,pos,size); Paint(rect,new Color32(49,93,70,255),true);
        StrataUIStyle.Round(rect.GetComponent<Image>());
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
        var colors=button.colors; colors.highlightedColor=new Color(1.18f,1.14f,1.04f,1);
        colors.pressedColor=new Color(.78f,.82f,.74f,1); colors.disabledColor=new Color(.52f,.58f,.53f,.8f); button.colors=colors;
        button.onClick.AddListener(()=> { action(); if(flow!=null) flow.PlayUIClick(); });
        Label("Label",rect,Vector2.zero,size-new Vector2(16,8),text,30,new Color32(255,248,232,255),true);
        return button;
    }
}
