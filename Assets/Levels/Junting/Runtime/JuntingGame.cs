using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TrackingNIU;
using XunJiYiDa.Levels.Integration;
using TeamSession = TrackingNIU.LevelSession;
namespace XunJiYiDa.Levels.Junting {
 [Serializable] public struct ArtifactArt { public ArtifactId artifact; public Texture2D image; }
 /// <summary>書海尋寶、草澤擋鹿共用的關卡程式。一個 Scene 跑一關，依 LevelSession.Current 決定關卡、限時與文物。</summary>
 public sealed class JuntingGame:MonoBehaviour {
  [SerializeField] TMP_FontAsset font;
  [SerializeField,Tooltip("Resources/Junting/rounded 缺檔時使用")] Sprite panelSprite;
  [SerializeField,Tooltip("與終章共用的文物知識（Finale/Data/ArtifactFacts.json）")] TextAsset artifactFacts;
  [SerializeField] ArtifactArt[] artifactArt=Array.Empty<ArtifactArt>();
  [SerializeField,Tooltip("Resources/Junting/Audio 缺檔時使用的替代音效")] NamedClip[] fallbackClips=Array.Empty<NamedClip>();
  public bool PauseWhenUnfocused {get;set;}=true;
  public bool CanPlay {get{return round!=null&&round.Active;}}
  public RoundState CurrentRound {get{return round;}}
  public ShelfRound Shelf {get{return shelf;}}
  public DeerBoard Board {get{return board;}}
  public string CurrentLevelId {get{return level==null?"":level.id;}}
  public string CurrentScreen {get;private set;}
  readonly Color ink=Hex("#1F3A2E"),paper=Hex("#FBF8F2"),red=Hex("#B3322A"),green=Hex("#2F8F5B"),yellow=Hex("#E0A100");
  Canvas canvas;RectTransform root,screen,overlay;Sprite rounded;GameAudio audioBus;JuntingLevelCatalog catalog;ArtifactFactList facts;LevelData level;LevelContext context;
  RoundState round;ShelfRound shelf;DeerBoard board;ScoreBreakdown score;bool cleared;
  TextMeshProUGUI timerText,progressText,scoreText,hintText;Image timeBar;RectTransform deerLayer; Image[] boardCells; TextMeshProUGUI[] boardMarks; RectTransform[] deerTokens; GameObject boardControls; float targetFlash;readonly RectTransform[] slots=new RectTransform[6];readonly Image[] slotImages=new Image[6];readonly RectTransform[] trayBooks=new RectTransform[6];
  int selected=-1,lastSecond;float stepClock;bool resultSent;float toastUntil;
  float TimeLimit {get{return context!=null&&context.HasTimeLimit?context.TimeLimit:60f;}}
  int Penalty {get{return context!=null?context.MistakePenalty:20;}}
  public static Color Hex(string s){Color c;ColorUtility.TryParseHtmlString(s,out c);return c;}
  public static string ToLocalLevelId(LevelId id){switch(id){case LevelId.Bookshelf:return "books";case LevelId.Deer:return "deer";default:return null;}}
  void Awake(){gameObject.name="JuntingGame";rounded=Resources.Load<Sprite>("Junting/rounded");if(!rounded)rounded=panelSprite;if(!font){Debug.LogError("JuntingGame 沒有指定字型。",this);enabled=false;return;}
   catalog=JsonUtility.FromJson<JuntingLevelCatalog>(Resources.Load<TextAsset>("Junting/levels").text);
   facts=artifactFacts?JsonUtility.FromJson<ArtifactFactList>(artifactFacts.text):new ArtifactFactList{items=Array.Empty<ArtifactFact>()};
   audioBus=gameObject.AddComponent<GameAudio>();audioBus.SetFallbacks(fallbackClips);BuildCanvas();
  }
  void Start(){if(!enabled)return;context=TeamSession.Current;string id=context==null?null:ToLocalLevelId(context.LevelId);var found=id==null?null:Array.Find(catalog.levels,l=>l.id==id);
   if(found==null){Debug.LogError("沒有對應的關卡資料。請在 Scene 加上 LevelTestLauncher 並選 Bookshelf 或 Deer。",this);enabled=false;return;}
   level=found;round=new RoundState{remaining=TimeLimit};ShowIntro();
  }
  void BuildCanvas(){var go=new GameObject("Junting Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
   var bg=go.AddComponent<Image>();bg.color=Hex("#162A22");bg.raycastTarget=false;root=Rect(go.transform,"16 by 9",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;Panel(root,0,0,1920,1080,paper,false);
   if(EventSystem.current==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));Fit();
  }
  void Fit(){if(!canvas)return;var size=((RectTransform)canvas.transform).rect.size;float scale=Mathf.Min(size.x/1920f,size.y/1080f);root.localScale=Vector3.one*scale;}
  RectTransform Rect(Transform parent,string name,float x,float y,float w,float h){var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;}
  Image Panel(Transform parent,float x,float y,float w,float h,Color color,bool roundCorners=true){var rt=Rect(parent,"Panel",x,y,w,h);var im=rt.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;if(roundCorners&&rounded){im.sprite=rounded;im.type=Image.Type.Sliced;}return im;}
  TextMeshProUGUI Text(Transform parent,string text,float x,float y,float w,float h,int size=38,Color? color=null,bool bold=false){var rt=Rect(parent,text.Length>20?text.Substring(0,20):text,x,y,w,h);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=text;t.fontSize=size;t.color=color??ink;t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;t.raycastTarget=false;return t;}
  Button Button(Transform parent,string label,float x,float y,float w,float h,UnityAction action,Color? color=null,int size=38){var p=Panel(parent,x,y,w,h,color??red);p.raycastTarget=true;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;var cb=b.colors;cb.highlightedColor=new Color(.94f,.94f,.88f);cb.pressedColor=new Color(.8f,.82f,.75f);b.colors=cb;b.onClick.AddListener(()=>{audioBus.Play("click");action();});var t=Text(p.transform,label,12,6,w-24,h-12,size,Color.white,true);t.alignment=TextAlignmentOptions.Center;return b;}
  void Picture(Transform parent,Texture2D texture,float x,float y,float w,float h){float ratio=(float)texture.width/texture.height;float pw=Mathf.Min(w,h*ratio),ph=pw/ratio;var rt=Rect(parent,texture.name,x+(w-pw)/2,y+(h-ph)/2,pw,ph);var im=rt.gameObject.AddComponent<RawImage>();im.texture=texture;im.raycastTarget=false;}
  void Photo(Transform parent,string key,float x,float y,float w,float h){var texture=Resources.Load<Texture2D>("Junting/"+key);if(texture){Picture(parent,texture,x,y,w,h);return;}var t=Text(parent,"實景照片待補",x,y+h/2-40,w,80,40,Hex("#738073"));t.alignment=TextAlignmentOptions.Center;}
  Texture2D ArtifactImage(ArtifactId artifact){foreach(var a in artifactArt)if(a.artifact==artifact)return a.image;return null;}
  ArtifactFact Fact(ArtifactId artifact){string id=TrackingArtifactMap.ToLocalId(artifact);return Array.Find(facts.items,f=>f.id==id);}
  void Clear(){if(screen){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}if(overlay){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);overlay=null;}screen=Rect(root,"Screen",0,0,1920,1080);hintText=null;}
  public void ShowIntro(){round.stage=RoundState.Stage.Intro;Clear();CurrentScreen="intro";audioBus.PlayBgm(level.bgmKey);Text(screen,level.place,64,52,1300,60,36,Hex(level.color));Text(screen,level.title,64,140,1500,90,64,null,true);
   Panel(screen,64,270,830,570,Hex("#E6E3D5"));Photo(screen,level.photo,80,286,798,520);Text(screen,level.photoCaption,64,854,850,85,28);Button(screen,"資料來源",64,896,350,120,()=>Application.OpenURL(level.sourceUrl),ink,34);
   Text(screen,string.Join("\n",level.intro),970,270,870,250,38);Text(screen,"挑戰說明",970,550,850,60,44,null,true);Text(screen,string.Join("\n",level.rules),970,630,880,240,34);
   Button(screen,"開始挑戰",1390,896,460,120,BeginGame,Hex(level.color));
  }
  public void BeginGame(){if(level==null)return;round=new RoundState{stage=RoundState.Stage.Playing,remaining=TimeLimit};shelf=new ShelfRound();board=new DeerBoard();resultSent=false;selected=-1;stepClock=0;lastSecond=Mathf.CeilToInt(TimeLimit);Clear();CurrentScreen="game";Hud();if(level.id=="books")DrawShelf();else DrawBoard();UpdateHud();}
  void Hud(){Panel(screen,0,0,1920,120,paper,false);Text(screen,level.title,64,10,510,64,44,null,true);Text(screen,level.place,64,73,520,42,28);Text(screen,"剩餘時間",620,10,270,45,28);timerText=Text(screen,"",960,2,310,58,44,null,true);Panel(screen,620,72,660,20,Hex("#DBDED2"));timeBar=Panel(screen,620,72,660,20,green);progressText=Text(screen,"",640,142,1200,50,34);scoreText=Text(screen,"",1340,16,290,55,36,null,true);Button(screen,"暫停",1650,0,206,120,ShowPause,ink);Panel(screen,0,120,1920,8,Hex(level.color),false);hintText=Text(screen,"",120,940,1680,74,34);hintText.alignment=TextAlignmentOptions.Center;Panel(screen,112,930,1696,90,Color.white).transform.SetSiblingIndex(1);}
  void Hint(string msg,bool error=false){toastUntil=Time.unscaledTime+3;if(hintText){hintText.text=msg;hintText.color=error?Hex("#D64545"):ink;}}
  string DefaultHint(){return level.id=="books"?"拖曳下方書本至黃色空位；也可先點書，再點空位。":"先點格子放擋板，再按放行；鹿遇擋板會順時針轉向。";}
  void UpdateHud(){if(timerText==null||round==null)return;timerText.text=Mathf.CeilToInt(round.remaining).ToString("00")+" 秒";timeBar.rectTransform.sizeDelta=new Vector2(660*round.remaining/TimeLimit,20);timeBar.color=round.remaining<TimeLimit*.3f?yellow:green;scoreText.text="失誤 −"+(round.mistakes*Penalty);progressText.text=level.id=="books"?"歸位 "+shelf.Placed+" / 3":"擋板 "+board.barriers.Count+" / 5     進入 "+board.arrived+" / 3";if(Time.unscaledTime>toastUntil&&hintText){hintText.text=DefaultHint();hintText.color=ink;}}
  void BookVisual(Transform parent,int id,float x,float y,bool draggable){Color[] colors={Hex("#7C9774"),Hex("#3D6A8C"),Hex("#A3634D"),Hex("#87759C"),Hex("#A78B45"),Hex("#5D8482")};var p=Panel(parent,x,y,190,270,colors[id]);var outline=p.gameObject.AddComponent<Outline>();outline.effectColor=ink;outline.effectDistance=new Vector2(4,-4);Panel(p.transform,12,12,8,246,new Color(1,1,1,.15f));var label=Text(p.transform,shelf.books[id].Label,25,46,145,186,32,Color.white,true);label.alignment=TextAlignmentOptions.Center;
   if(draggable){trayBooks[id]=p.rectTransform;p.raycastTarget=true;var drag=p.gameObject.AddComponent<BookDrag>();drag.game=this;drag.book=id;}
  }
  void DrawShelf(){Text(screen,"依索書號由小到大排列",64,220,1700,70,44,null,true);Text(screen,"本關為排架練習題，書號不代表實際館藏。",64,298,1750,50,28);Panel(screen,214,350,1492,310,Hex("#BD9B70"));Panel(screen,226,362,1468,278,Hex("#E9DDC3"));
   for(int i=0;i<6;i++){float x=260+i*238;if(shelf.filled[i])BookVisual(screen,i,x,370,false);else{int slot=i;var b=Button(screen,"空位",x,370,190,270,()=>PlaceSelected(slot),Hex("#D2A73B"),32);slots[i]=(RectTransform)b.transform;slotImages[i]=b.GetComponent<Image>();var hover=b.gameObject.AddComponent<HoverSlot>();hover.game=this;hover.slot=i;}}
   Text(screen,"待歸位書本",64,720,500,60,36,null,true);int[] order={5,1,3};for(int j=0;j<3;j++)BookVisual(screen,order[j],620+j*300,660,true);
  }
  public void SelectBook(int id){if(!CanPlay||level.id!="books"||id<0||id>=6||shelf.filled[id])return;selected=id;for(int i=0;i<6;i++)if(trayBooks[i]&&!shelf.filled[i])trayBooks[i].localScale=Vector3.one*(i==id?1.1f:1f);Hint("已選取 "+shelf.books[id].classification+"，請放入適合的空位。");}
  public void HighlightSlot(int slot,bool on){if(CanPlay&&slot>=0&&slot<6&&slotImages[slot]&&!shelf.filled[slot])slotImages[slot].color=on?Hex("#F4D875"):Hex("#D2A73B");}
  public void DropBook(int id,Vector2 position,Camera camera){if(!CanPlay)return;for(int i=0;i<6;i++)if(slots[i]&&!shelf.filled[i]&&RectTransformUtility.RectangleContainsScreenPoint(slots[i],position,camera)){selected=id;PlaceSelected(i);return;}Hint("放到黃色空位中，就能讓書本歸位。");}
  public bool TryPlaceBook(int id,int slot){selected=id;return PlaceSelected(slot);}
  bool PlaceSelected(int slot){if(!CanPlay||level.id!="books")return false;if(selected<0){Hint("請先選一本待歸位的書。");return false;}if(slot<0||slot>=6||shelf.filled[slot])return false;int id=selected;if(!shelf.Place(id,slot)){round.mistakes++;audioBus.Play("wrong");Hint("順序不對，請比較左右書本的分類號。",true);if(trayBooks[id])StartCoroutine(Shake(trayBooks[id]));UpdateHud();return false;}
   audioBus.Play("snap");audioBus.Play("insert");if(slots[slot]){slots[slot].gameObject.SetActive(false);}if(trayBooks[id])trayBooks[id].gameObject.SetActive(false);BookVisual(screen,id,260+slot*238,370,false);selected=-1;Hint("歸位成功！");UpdateHud();if(shelf.Placed==3)FinishGame();return true;
  }
  IEnumerator Shake(RectTransform rt){Vector2 origin=rt.anchoredPosition;for(int i=0;i<8;i++){while(round!=null&&round.paused)yield return null;if(!rt)yield break;rt.anchoredPosition=origin+new Vector2(i%2==0?8:-8,0);yield return new WaitForSecondsRealtime(.035f);}if(rt)rt.anchoredPosition=origin;}
  void DrawBoard(){Text(screen,"安排路徑，讓三隻鹿走進草澤",420,205,850,65,36,null,true);var p=Panel(screen,232,296,1456,616,ink);deerLayer=Rect(p.transform,"Deer Board",8,8,1440,600);boardCells=new Image[60];boardMarks=new TextMeshProUGUI[60];deerTokens=new RectTransform[3];
   for(int y=0;y<5;y++)for(int x=0;x<12;x++){int cx=x,cy=y,k=y*12+x;var button=Button(deerLayer,"",x*120,y*120,120,120,()=>ToggleBarrier(cx,cy),paper);button.gameObject.name="Cell_"+x+"_"+y;boardCells[k]=button.GetComponent<Image>();boardMarks[k]=Text(button.transform,"",3,28,114,64,36,ink,true);boardMarks[k].alignment=TextAlignmentOptions.Center;}
   var deerSprite=Resources.Load<Sprite>("Junting/deer");
   for(int i=0;i<3;i++){var token=Rect(deerLayer,"Deer_"+i,2,125,116,110);if(deerSprite){var im=Panel(token,0,0,116,110,Color.white,false);im.sprite=deerSprite;im.preserveAspect=true;}else{Panel(token,8,5,100,100,Hex("#A9481B"));var t=Text(token,"鹿",8,22,100,64,48,Color.white,true);t.alignment=TextAlignmentOptions.Center;}deerTokens[i]=token;}
   boardControls=Rect(screen,"Board Controls",0,0,1920,1080).gameObject;Button(boardControls.transform,"放行",64,182,310,120,ReleaseDeer,red,34);Button(boardControls.transform,"路徑提示",1360,182,320,120,()=>Hint("在第 10 欄第 2 列放擋板，鹿會在第 9 欄向下轉。"),ink,32);RepaintBoard();
  }
  void RepaintBoard(){for(int y=0;y<5;y++)for(int x=0;x<12;x++){int k=y*12+x;bool barrier=board.barriers.Contains(k);boardCells[k].color=board.Target(x,y)?(targetFlash>0?Hex("#CCEFA1"):Hex("#8EB879")):barrier?Hex("#A9481B"):Hex((x+y)%2==0?"#D5DFC1":"#DCE4CC");boardMarks[k].text=barrier?"╱":board.Target(x,y)?"草澤":y==1?"→":"";boardMarks[k].color=barrier?Color.white:ink;}
   boardControls.SetActive(!board.released);
   for(int i=0;i<3;i++){var d=board.deer[i];bool visible=board.released?d.spawned&&!d.arrived&&!d.escaped&&d.x>=0:i==0;bool appeared=visible&&!deerTokens[i].gameObject.activeSelf;deerTokens[i].gameObject.SetActive(visible);if(appeared||!board.released)deerTokens[i].anchoredPosition=new Vector2(Math.Max(0,d.x)*120+2,-(d.y*120+5));}
  }
  void AnimateDeer(){if(deerTokens==null)return;for(int i=0;i<3;i++){var d=board.deer[i];if(deerTokens[i].gameObject.activeSelf&&board.released)deerTokens[i].anchoredPosition=Vector2.Lerp(deerTokens[i].anchoredPosition,new Vector2(d.x*120+2,-(d.y*120+5)),1-Mathf.Exp(-14*Time.unscaledDeltaTime));}if(targetFlash>0){targetFlash-=Time.unscaledDeltaTime;if(targetFlash<=0)RepaintBoard();}}
  IEnumerator Land(RectTransform rect){for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){while(round!=null&&round.paused)yield return null;if(!rect)yield break;rect.localScale=Vector3.one*(1.08f-.08f*t/.18f);yield return null;}if(rect)rect.localScale=Vector3.one;}
  public bool ToggleBarrier(int x,int y){if(!CanPlay||level.id!="deer")return false;if(!board.Toggle(x,y)){Hint(board.released?"放行後不能更動擋板。":"最多五個擋板，起點與草澤不能放置。",true);return false;}audioBus.Play("barrier");RepaintBoard();if(board.barriers.Contains(y*12+x))StartCoroutine(Land(boardCells[y*12+x].rectTransform));UpdateHud();return true;}
  public void ReleaseDeer(){if(!CanPlay||board.released)return;board.released=true;stepClock=0;RepaintBoard();Hint("鹿群出發了，觀察牠們遇到擋板的轉向。");}
  void FinishGame(){if(!CanPlay)return;round.stage=RoundState.Stage.Quiz;round.quizIndex=0;round.answered=false;audioBus.Play("win");ShowQuiz();}
  void ShowQuiz(){Clear();CurrentScreen="quiz";Question q=level.questions[round.quizIndex];Text(screen,level.title+"  /  結尾問答 "+(round.quizIndex+1)+" / "+level.questions.Length,120,64,1700,70,36,Hex(level.color));Text(screen,"把發現變成知識",120,170,1680,100,64,null,true);Text(screen,"倒數已停止。答錯可重試，完成後領取文物知識卡。",120,290,1680,65,34);Text(screen,q.prompt,120,390,1680,100,44,null,true);
   for(int i=0;i<q.options.Length;i++){int choice=i;Button(screen,q.options[i],120,510+i*132,1680,120,()=>Answer(choice),ink,38);} }
  public bool Answer(int choice){if(round==null||round.stage!=RoundState.Stage.Quiz||round.answered)return false;var q=level.questions[round.quizIndex];if(choice<0||choice>=q.options.Length)return false;if(choice!=q.answer){round.quizMistakes++;audioBus.Play("wrong");ShowQuizFeedback(false,q.explanation);return false;}round.answered=true;audioBus.Play("correct");ShowQuizFeedback(true,q.explanation);return true;}
  void ShowQuizFeedback(bool correct,string explanation){if(overlay){Destroy(overlay.gameObject);}overlay=Rect(root,"Answer explanation",0,0,1920,1080);Panel(overlay,0,0,1920,1080,new Color(.05f,.1f,.08f,.76f),false).raycastTarget=true;Panel(overlay,240,230,1440,620,paper);Text(overlay,correct?"答對了":"再想一想",304,290,1300,80,52,correct?green:red,true);Text(overlay,explanation,304,420,1300,170,38);Button(overlay,correct?"繼續":"再試一次",1120,654,450,120,()=>{Destroy(overlay.gameObject);overlay=null;if(correct)NextQuestion();},correct?green:ink);}
  public void NextQuestion(){if(round==null||round.stage!=RoundState.Stage.Quiz||!round.answered)return;round.quizIndex++;round.answered=false;if(round.quizIndex<level.questions.Length)ShowQuiz();else Reward();}
  // 問答答錯可重試，不計入失誤；計分交給主系統 ScoreCalculator。
  void Reward(){if(resultSent||round.stage!=RoundState.Stage.Quiz)return;round.stage=RoundState.Stage.Reward;SendResult(true);ShowKnowledge();audioBus.Play("collect");}
  void SendResult(bool success){if(resultSent)return;resultSent=true;cleared=success;score=TeamSession.IsActive?TeamSession.Report(new TrackingNIU.LevelResult{Cleared=success,RemainingTime=success?round.remaining:0f,Mistakes=round.mistakes}):ScoreBreakdown.NotScored;}
  void ShowKnowledge(){Clear();CurrentScreen="reward";var artifact=context.Artifact;var fact=Fact(artifact);
   Text(screen,cleared?"探索完成  /  獲得文物":"挑戰未完成  /  仍帶走文物線索",64,48,1700,60,36,Hex(level.color));Text(screen,"帶走一段校園記憶",64,138,1700,100,64,null,true);Panel(screen,64,285,550,480,Hex("#E9DFC9"));var image=ArtifactImage(artifact);if(image)Picture(screen,image,100,305,478,430);
   Text(screen,LevelCatalog.GetArtifactName(artifact),690,286,1150,80,52,null,true);Text(screen,fact!=null?fact.knowledge:"",690,390,1150,220,38);if(fact!=null&&!string.IsNullOrEmpty(fact.sourceUrl))Button(screen,"知識來源",690,640,350,120,()=>Application.OpenURL(fact.sourceUrl),ink,34);Text(screen,"插畫依官方文物外形簡化；獎勵不表示於該場域出土。",690,770,1150,50,28);
   Text(screen,"基礎分 "+score.ClearBonus+"    時間獎勵 +"+score.TimeBonus+"    失誤 −"+score.MistakePenalty+"    總分 "+score.Total,64,834,1790,55,38,null,true);Button(screen,"繼續",1370,896,480,120,ContinueToMap,Hex(level.color));}
  void ContinueToMap(){if(!TeamSession.IsActive||!TeamSession.HasReported)return;bool testRun=context.IsTestRun;TeamSession.Exit();
   // 單獨測試時沒有 GameManager 接手，重新載入本關方便再玩一次。
   if(testRun)SceneManager.LoadScene(SceneManager.GetActiveScene().name);}
  void Retry(){if(TeamSession.IsActive)TeamSession.Retry();}
  public void Fail(string reason){if(round==null||round.stage==RoundState.Stage.Failed&&resultSent)return;round.stage=RoundState.Stage.Failed;Clear();CurrentScreen="failed";audioBus.Play("fail");Text(screen,"本次挑戰結束",120,180,1660,110,64,null,true);Text(screen,reason,120,365,1650,190,44,red);Text(screen,"先觀察線索，再安排下一次挑戰。",120,585,1650,80,38);Button(screen,"再試一次",120,770,650,120,Retry);Button(screen,"繼續旅程",1070,770,650,120,()=>{SendResult(false);ShowKnowledge();},ink);}
  public void ShowPause(){if(!CanPlay)return;PauseGame(true);ShowSettings();}
  public void PauseGame(bool value){if(round==null||round.stage!=RoundState.Stage.Playing)return;round.paused=value;audioBus.Pause(value);}
  void ShowSettings(){overlay=Rect(root,"Pause",0,0,1920,1080);Panel(overlay,0,0,1920,1080,new Color(.05f,.1f,.08f,.78f),false).raycastTarget=true;Panel(overlay,460,200,1000,640,paper);Text(overlay,"休息一下",530,265,860,90,58,null,true);Text(overlay,"倒數與遊戲操作已暫停。",530,380,860,80,36);Button(overlay,"音樂 "+(audioBus.MusicEnabled?"開":"關"),530,495,390,120,()=>{audioBus.SetMusic(!audioBus.MusicEnabled);ReplaceSettings();},ink);Button(overlay,"音效 "+(audioBus.EffectsEnabled?"開":"關"),980,495,390,120,()=>{audioBus.SetEffects(!audioBus.EffectsEnabled);ReplaceSettings();},ink);Button(overlay,"繼續",530,680,840,120,()=>{Destroy(overlay.gameObject);overlay=null;PauseGame(false);});}
  void ReplaceSettings(){Destroy(overlay.gameObject);overlay=null;ShowSettings();}
  void Update(){Fit();if(round==null||!CanPlay)return;round.Tick(Time.unscaledDeltaTime);if(round.stage==RoundState.Stage.Failed){Fail("時間到了，尚未完成本關挑戰。");return;}int sec=Mathf.CeilToInt(round.remaining);if(sec!=lastSecond){if(sec<=10&&sec>0)audioBus.Play("countdown");lastSecond=sec;}if(level.id=="deer"&&board.released){stepClock+=Time.unscaledDeltaTime;while(stepClock>=.45f&&CanPlay){stepClock-=.45f;int before=board.arrived;board.Step();audioBus.Play("hoof");if(board.arrived>before){targetFlash=.55f;audioBus.Play("arrive");Hint("鹿已進入草澤！");}RepaintBoard();if(board.escaped){Fail("鹿群離開地圖了。調整擋板位置後再挑戰。");break;}if(board.arrived==3){FinishGame();break;}}}if(CanPlay){if(level.id=="deer")AnimateDeer();UpdateHud();}}
  void OnApplicationFocus(bool focus){if(PauseWhenUnfocused&&!focus&&CanPlay)ShowPause();}
  void OnApplicationPause(bool paused){if(PauseWhenUnfocused&&paused&&CanPlay)ShowPause();}
  void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
 }
}
