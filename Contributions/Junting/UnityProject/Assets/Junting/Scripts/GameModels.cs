using System;
using System.Collections.Generic;
using System.Globalization;
namespace NIU.Junting {
 [Serializable] public class Question { public string prompt; public string[] options; public int answer; public string explanation; }
 [Serializable] public class LevelData { public string id,title,subtitle,place,color,photo,photoCaption,sourceUrl,bgmKey; public string[] intro,rules; public string artifactId,artifactName,artifactImage,knowledge,knowledgeUrl; public Question[] questions; }
 [Serializable] public class LevelCatalog { public LevelData[] levels; }
 [Serializable] public class LevelResult { public string runId,levelId,artifactId; public bool completed; public int baseScore,timeBonus,mistakePenalty,totalScore,quizMistakes,artifactCount; public float secondsRemaining; }
 public interface IScoreProvider { LevelResult Evaluate(string runId,string levelId,float seconds,int mistakes,int quizMistakes); }
 // Demo policy only. Replace via SetScoreProvider with the team's scoring system.
 public class DemoScoreProvider:IScoreProvider {
  public LevelResult Evaluate(string runId,string levelId,float seconds,int mistakes,int quizMistakes) {
   int bonus=(int)Math.Floor(seconds)*2, penalty=mistakes*20;
   return new LevelResult{runId=runId,levelId=levelId,completed=true,baseScore=300,timeBonus=bonus,mistakePenalty=penalty,totalScore=Math.Max(0,300+bonus-penalty),quizMistakes=quizMistakes,secondsRemaining=seconds};
  }
 }
 // Fisher-Yates order. Drawing consumes an assignment, not a reward. The UI keeps
 // an unfinished assignment so returning to the lobby cannot reroll a failed level.
 public sealed class RandomLevelOrder {
  readonly List<string> remaining;public int TotalCount {get;private set;}public int Remaining {get{return remaining.Count;}}
  public RandomLevelOrder(IEnumerable<string> ids,Random random){if(ids==null||random==null)throw new ArgumentNullException();remaining=new List<string>();var seen=new HashSet<string>();foreach(var id in ids){if(string.IsNullOrWhiteSpace(id)||!seen.Add(id))throw new ArgumentException("Level IDs must be nonempty and unique");remaining.Add(id);}if(remaining.Count==0)throw new ArgumentException("Pool must contain a level");TotalCount=remaining.Count;for(int i=remaining.Count-1;i>0;i--){int j=random.Next(i+1);string tmp=remaining[i];remaining[i]=remaining[j];remaining[j]=tmp;}}
  public string DrawNext(){if(remaining.Count==0)return null;string id=remaining[0];remaining.RemoveAt(0);return id;}
  public void Exclude(string id){remaining.Remove(id);}
 }
 public class RoundState {
  public enum Stage { Intro,Playing,Quiz,Reward,Failed }
  public Stage stage=Stage.Intro; public bool paused; public float remaining=60; public int mistakes,quizIndex,quizMistakes; public bool answered; public readonly string runId=Guid.NewGuid().ToString("N");
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
  // Introductory exercise: three different classification numbers; no claim of real holdings.
  public readonly Book[] books={new Book{classification="020",author="1234",year=2020},new Book{classification="025.4",author="2345",year=2021},new Book{classification="050",author="3456",year=2022},new Book{classification="110",author="4567",year=2023},new Book{classification="120",author="5678",year=2021},new Book{classification="130",author="6789",year=2024}};
  public readonly bool[] filled={true,false,true,false,true,false}; public int Placed{get{int n=0;for(int i=1;i<6;i+=2)if(filled[i])n++;return n;}}
  public bool Place(int book,int slot){if(book<0||book>=6||slot<0||slot>=6||filled[slot]||filled[book]||book!=slot)return false;filled[slot]=true;return true;}
 }
 public class DeerBoard {
  public const int Width=12,Height=5,Limit=5;public readonly HashSet<int> barriers=new HashSet<int>();
  public class Deer {public int x=-1,y=1,dir=0;public bool arrived,escaped,spawned;}
  public readonly Deer[] deer={new Deer(),new Deer(),new Deer()};public bool released;public int ticks,arrived;public bool escaped;
  static readonly int[] dx={1,0,-1,0},dy={0,1,0,-1};
  public bool Target(int x,int y){return y==4&&x>=8&&x<=10;}
  public bool Toggle(int x,int y){if(released||x<1||x>=Width||y<0||y>=Height||Target(x,y))return false;int k=y*Width+x;if(barriers.Remove(k))return true;if(barriers.Count>=Limit)return false;barriers.Add(k);return true;}
  public void Step(){if(!released||arrived==3||escaped)return;ticks++;for(int i=0;i<3;i++){Deer d=deer[i];if(d.arrived||d.escaped||ticks<=i*5)continue;d.spawned=true;int x=d.x+dx[d.dir],y=d.y+dy[d.dir];if(x>=0&&x<Width&&y>=0&&y<Height&&barriers.Contains(y*Width+x)){d.dir=(d.dir+1)%4;continue;}d.x=x;d.y=y;if(x<0||x>=Width||y<0||y>=Height){d.escaped=true;escaped=true;continue;}if(Target(x,y)){d.arrived=true;arrived++;}}}
 }
}
