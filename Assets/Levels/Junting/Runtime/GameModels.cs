using System;
using System.Collections.Generic;
using System.Globalization;
namespace XunJiYiDa.Levels.Junting {
 // artifact 為 true 時，題目旁顯示本關文物圖，讓玩家看圖作答。
 [Serializable] public class Question { public string prompt; public string[] options; public int answer; public string explanation; public bool artifact; }
 // 文物與計分由主系統 LevelCatalog 決定，這裡只保留關卡自己的介紹、題目與素材鍵值。
 [Serializable] public class LevelData { public string id,title,subtitle,place,color,photo,photoCaption,sourceUrl,bgmKey; public string[] intro,rules; public Question[] questions; }
 [Serializable] public class JuntingLevelCatalog { public LevelData[] levels; }
 [Serializable] public class ArtifactFact { public string id,summary,knowledge,sourceUrl; }
 [Serializable] public class ArtifactFactList { public ArtifactFact[] items; }
 public class RoundState {
  public enum Stage { Intro,Playing,Quiz,Reward,Failed }
  public Stage stage=Stage.Intro; public bool paused; public float remaining=60; public int mistakes,quizIndex,quizMistakes; public bool answered;
  public bool Active {get{return stage==Stage.Playing&&!paused;}}
  public void Tick(float dt){if(Active){remaining=Math.Max(0,remaining-dt);if(remaining<=0)stage=Stage.Failed;}}
 }
 public class Book {
  public string classification,author; public int year,volume;
  public string Label {get{return classification+"\n"+author+"\n"+year+(volume>0?"\nv."+volume:"");}}
  public static int Compare(Book a,Book b) {
   int c=decimal.Parse(a.classification,CultureInfo.InvariantCulture).CompareTo(decimal.Parse(b.classification,CultureInfo.InvariantCulture));
   if(c!=0)return c; c=int.Parse(a.author,CultureInfo.InvariantCulture).CompareTo(int.Parse(b.author,CultureInfo.InvariantCulture));
   if(c!=0)return c;c=a.year.CompareTo(b.year);return c!=0?c:a.volume.CompareTo(b.volume);
  }
 }
 public class ShelfRound {
  public const int Size=6,Empty=3;
  // Practice numbers only; no claim of real holdings. Pairs such as 025.12/025.4 practise reading decimals digit by digit.
  static readonly string[] Classes={"020","025.12","025.4","050","110","128.5","130","538.7","733","733.1","863.57","863.6"};
  // books are sorted, so book i belongs in slot i; stage 1 shares one classification so authors and years decide.
  public readonly Book[] books; public readonly bool[] filled; readonly bool[] empty=new bool[Size]; public readonly int stage;
  public ShelfRound(){books=new[]{new Book{classification="020",author="1234",year=2020},new Book{classification="025.4",author="2345",year=2021},new Book{classification="050",author="3456",year=2022},new Book{classification="110",author="4567",year=2023},new Book{classification="120",author="5678",year=2021},new Book{classification="130",author="6789",year=2024}};filled=new[]{true,false,true,false,true,false};for(int i=0;i<Size;i++)empty[i]=!filled[i];}
  public ShelfRound(int seed,int stage){this.stage=stage;var random=new Random(seed);var list=new List<Book>();
   if(stage==0){var pool=new List<string>(Classes);for(int i=0;i<Size;i++){int k=random.Next(pool.Count);list.Add(new Book{classification=pool[k],author=random.Next(1000,10000).ToString(CultureInfo.InvariantCulture),year=random.Next(2000,2026)});pool.RemoveAt(k);}}
   else{var authors=new HashSet<int>();while(authors.Count<4)authors.Add(random.Next(1000,10000));var ids=new List<int>(authors);
    // Two authors have two books each, so the year has to break the tie.
    for(int i=0;i<Size;i++){int author=ids[i%4];int year=random.Next(2000,2013)+(i>=4?13:0);list.Add(new Book{classification="733.9",author=author.ToString(CultureInfo.InvariantCulture),year=year});}}
   list.Sort(Book.Compare);books=list.ToArray();filled=new bool[Size];for(int i=0;i<Size;i++)filled[i]=true;
   var slots=new List<int>{0,1,2,3,4,5};for(int i=0;i<Empty;i++){int k=random.Next(slots.Count);filled[slots[k]]=false;empty[slots[k]]=true;slots.RemoveAt(k);}
  }
  public IEnumerable<int> EmptySlots{get{for(int i=0;i<Size;i++)if(empty[i])yield return i;}}
  public int Placed{get{int n=0;for(int i=0;i<Size;i++)if(empty[i]&&filled[i])n++;return n;}}
  public bool Place(int book,int slot){if(book<0||book>=Size||slot<0||slot>=Size||filled[slot]||filled[book]||book!=slot)return false;filled[slot]=true;return true;}
 }
 /// <summary>One deer board: entry row, two-cell marsh and fixed rocks. Rocks turn deer like barriers but cannot be moved.</summary>
 public class DeerMap {
  public int startRow,targetRow,targetMin,targetMax;public int[] rocks=new int[0];public string hint;
  public static readonly DeerMap Original=new DeerMap{startRow=1,targetRow=4,targetMin=8,targetMax=10,hint="在第 10 欄第 2 列放擋板，鹿會在第 9 欄向下轉。"};
  // Designed with a simulator of DeerBoard.Step: none can be solved with fewer barriers than the hint says.
  public static readonly DeerMap[] Challenges={
   new DeerMap{startRow=1,targetRow=4,targetMin=8,targetMax=9,rocks=new[]{3*12+4,4*12+5,4*12+6,1*12+7,0*12+10,4*12+11},hint="草澤在下方：先讓鹿往下走，再在草澤上方把牠轉進去。最少兩個擋板。"},
   new DeerMap{startRow=1,targetRow=0,targetMin=7,targetMax=8,rocks=new[]{0*12+4,3*12+4,2*12+9,3*12+9},hint="草澤在最上排：鹿要轉三次彎（右→下→左→上）才能往上走。最少三個擋板。"},
   new DeerMap{startRow=2,targetRow=0,targetMin=5,targetMax=6,rocks=new[]{0*12+1,1*12+6,3*12+9,3*12+10},hint="石頭擋住了往上的捷徑：讓鹿往下繞一圈再往上。最少三個擋板。"},
  };
 }
 public class DeerBoard {
  public const int Width=12,Height=5,Limit=5;public readonly HashSet<int> barriers=new HashSet<int>();public readonly DeerMap map;readonly HashSet<int> rocks;
  public class Deer {public int x=-1,y=1,dir=0;public bool arrived,escaped,spawned;}
  public readonly Deer[] deer;public bool released;public int ticks,arrived;public bool escaped;
  static readonly int[] dx={1,0,-1,0},dy={0,1,0,-1};
  public DeerBoard():this(DeerMap.Original){}
  public DeerBoard(DeerMap map){this.map=map;rocks=new HashSet<int>(map.rocks);deer=new[]{new Deer{y=map.startRow},new Deer{y=map.startRow},new Deer{y=map.startRow}};}
  public bool Target(int x,int y){return y==map.targetRow&&x>=map.targetMin&&x<=map.targetMax;}
  public bool Rock(int x,int y){return rocks.Contains(y*Width+x);}
  public bool Toggle(int x,int y){if(released||x<1||x>=Width||y<0||y>=Height||Target(x,y)||Rock(x,y))return false;int k=y*Width+x;if(barriers.Remove(k))return true;if(barriers.Count>=Limit)return false;barriers.Add(k);return true;}
  public void Step(){if(!released||arrived==3||escaped)return;ticks++;for(int i=0;i<3;i++){Deer d=deer[i];if(d.arrived||d.escaped||ticks<=i*5)continue;d.spawned=true;int x=d.x+dx[d.dir],y=d.y+dy[d.dir];if(x>=0&&x<Width&&y>=0&&y<Height&&(barriers.Contains(y*Width+x)||rocks.Contains(y*Width+x))){d.dir=(d.dir+1)%4;continue;}d.x=x;d.y=y;if(x<0||x>=Width||y<0||y>=Height){d.escaped=true;escaped=true;continue;}if(Target(x,y)){d.arrived=true;arrived++;}}}
 }
}
