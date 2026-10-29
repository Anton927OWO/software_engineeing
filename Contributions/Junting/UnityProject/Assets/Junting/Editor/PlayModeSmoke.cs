using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace NIU.Junting.Editor {
 [InitializeOnLoad] public static class PlayModeSmoke {
  static JuntingGame game;static int phase,checks,completed,failed;static double next;static float pausedTime;static bool running;static string first,firstMusic;static bool oneArtifact=true;static System.Collections.Generic.HashSet<string> rewards;
  static PlayModeSmoke(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=OnPlay;}
  [MenuItem("Junting/Run Full Play Mode Smoke Test")]
  public static void Start(){if(EditorSceneManager.GetActiveScene().isDirty)throw new Exception("Save the active scene before testing");EditorSceneManager.OpenScene("Assets/Junting/Scenes/JuntingMain.unity");File.WriteAllText(".smoke-request","random-v2");EditorApplication.isPlaying=true;}
  static void OnPlay(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&File.Exists(".smoke-request")){File.Delete(".smoke-request");running=true;phase=0;checks=completed=failed=0;oneArtifact=true;rewards=new System.Collections.Generic.HashSet<string>();next=EditorApplication.timeSinceStartup+2;}}
  static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
  static void CompleteGame(){if(game.CurrentLevelId=="books"){Check(!game.TryPlaceBook(5,1),"Wrong book rejected");game.TryPlaceBook(1,1);game.TryPlaceBook(3,3);game.TryPlaceBook(5,5);}else{game.ToggleBarrier(9,1);game.ReleaseDeer();next+=12;}}
  static void AnswerAll(){Check(game.CurrentRound.stage==RoundState.Stage.Quiz,"Challenge reaches quiz");game.Answer(1);game.NextQuestion();game.Answer(0);game.NextQuestion();}
  static void Tick(){if(!running||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;try{switch(phase++){
   case 0:game=UnityEngine.Object.FindFirstObjectByType<JuntingGame>();Check(game!=null&&game.CurrentScreen=="home","Home loaded");game.PauseWhenUnfocused=false;Application.runInBackground=true;game.LevelCompleted+=r=>{completed++;oneArtifact&=r.artifactCount==1;rewards.Add(r.artifactId);};game.LevelFailed+=r=>failed++;game.BeginRandomJourney();break;
   case 1:first=game.CurrentLevelId;firstMusic=game.CurrentBgmKey;Check(first=="books"||first=="deer","Random assignment is a valid owned level");Check(firstMusic=="bgm_"+first,"Assigned level plays its dedicated BGM");game.Home();game.BeginRandomJourney();Check(game.CurrentLevelId==first,"Returning home does not reroll unfinished assignment");game.BeginGame();game.PauseGame(true);pausedTime=game.CurrentRound.remaining;next+=1;break;
   case 2:Check(game.CurrentRound.remaining==pausedTime,"Pause freezes clock");game.PauseGame(false);game.CurrentRound.remaining=.01f;break;
   case 3:Check(game.CurrentScreen=="failed"&&completed==0&&failed==1,"Failure gives no artifact");game.Home();game.BeginRandomJourney();Check(game.CurrentLevelId==first,"Failure retains assigned level");game.BeginGame();CompleteGame();break;
   case 4:AnswerAll();break;
   case 5:Check(game.CurrentScreen=="reward"&&completed==1&&oneArtifact,"First assigned level gives exactly one artifact");game.NextQuestion();Check(completed==1,"Reward cannot repeat");game.ContinueJourney();Check(game.CurrentLevelId!=first,"Second assignment differs from first");Check(game.CurrentBgmKey!=firstMusic&&game.CurrentBgmKey=="bgm_"+game.CurrentLevelId,"Second level uses different BGM");game.BeginGame();CompleteGame();break;
   case 6:AnswerAll();break;
   case 7:Check(game.CurrentScreen=="reward"&&completed==2&&oneArtifact&&rewards.Count==2,"Two levels give two distinct artifacts");game.ContinueJourney();Check(game.CurrentScreen=="complete"&&completed==2,"Journey ends after two levels");game.Home();game.BeginRandomJourney();Check(game.CurrentScreen=="intro","New journey can start");game.SetExternalProgression(true);int requests=0;game.NextLevelRequested+=()=>requests++;game.ContinueJourney();game.ContinueJourney();Check(requests==1,"Host next-level request is sent once");game.SetExternalProgression(false);game.Home();break;
   case 8:File.WriteAllText(".smoke-result","PASS "+checks+" random-route Play Mode checks; completed="+completed+" failed="+failed+" distinctArtifacts="+rewards.Count);running=false;EditorApplication.isPlaying=false;break;
  }}catch(Exception e){running=false;File.WriteAllText(".smoke-result","FAIL phase="+(phase-1)+" "+e);Debug.LogException(e);EditorApplication.isPlaying=false;}}
 }
}
