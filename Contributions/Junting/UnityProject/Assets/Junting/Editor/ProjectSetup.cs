using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
namespace NIU.Junting.Editor {
 public static class ProjectSetup {
  [MenuItem("Junting/1 Setup Project")]
  public static void Setup(){
   string essential="Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
   if(!AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset")){AssetDatabase.importPackageCompleted+=Imported;AssetDatabase.ImportPackage(essential,false);return;}
   foreach(string name in new[]{"rounded","deer","pot","paddle"}){string p="Assets/Junting/Resources/Junting/"+name+".png";var ti=AssetImporter.GetAtPath(p) as TextureImporter;if(ti){ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;if(name=="rounded")ti.spriteBorder=new Vector4(16,16,16,16);ti.SaveAndReimport();}}
   string fp="Assets/Junting/Resources/Junting/ChineseFont.asset";
   if(!AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fp)){
    var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Common/Fonts/NotoSansTC-Regular.otf");
    if(!source)throw new Exception("Noto Sans TC font missing");
    var font=TMP_FontAsset.CreateFontAsset(source,80,9,GlyphRenderMode.SDFAA,4096,4096,AtlasPopulationMode.Dynamic,true);font.name="NIU Noto Sans TC";font.atlasTexture.name="NIU Chinese Atlas";font.material.name="NIU Chinese Material";
    string chars=File.ReadAllText("Assets/Junting/Resources/Junting/levels.json")+File.ReadAllText("Assets/Junting/Scripts/JuntingGame.cs")+"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz─╱→×−秒";
    string missing;font.TryAddCharacters(new string(chars.Distinct().ToArray()),out missing);Debug.Log("Font unavailable glyphs (code punctuation may be harmless): "+missing);
    AssetDatabase.CreateAsset(font,fp);AssetDatabase.AddObjectToAsset(font.material,font);foreach(var texture in font.atlasTextures)if(texture)AssetDatabase.AddObjectToAsset(texture,font);EditorUtility.SetDirty(font);
   }
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Camera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.08f,.06f);camera.gameObject.AddComponent<AudioListener>();new GameObject("JuntingGame",typeof(JuntingGame));Directory.CreateDirectory("Assets/Junting/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Junting/Scenes/JuntingMain.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Junting/Scenes/JuntingMain.unity",true)};
   PlayerSettings.companyName="NIU First Group";PlayerSettings.productName="尋跡宜大 朱峻廷關卡";PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
   PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.template="PROJECT:NIU";PlayerSettings.WebGL.memorySize=256;PlayerSettings.WebGL.dataCaching=true;
   AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validation.Run();Debug.Log("NIU_SETUP_OK");
  }
  static void Imported(string name){AssetDatabase.importPackageCompleted-=Imported;EditorApplication.delayCall+=Setup;}
  [MenuItem("Junting/2 Validate Game Rules")]
  public static void Validate(){Validation.Run();}
  [MenuItem("Junting/3 Build WebGL")]
  public static void BuildWeb(){if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new Exception("Install matching WebGL Build Support in Unity Hub first.");Build(BuildTarget.WebGL,Path.GetFullPath(Path.Combine(Application.dataPath,"../../NIU_Junting_WebGL")));}
  [MenuItem("Junting/Build Windows Preview")]
  public static void BuildWindows(){Build(BuildTarget.StandaloneWindows64,Path.GetFullPath(Path.Combine(Application.dataPath,"../../NIU_Junting_Windows/NIU.exe")));}
  static void Build(BuildTarget target,string path){Validation.Run();var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Junting/Scenes/JuntingMain.unity"},locationPathName=path,target=target,options=BuildOptions.Development});if(r.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+r.summary.result);if(target==BuildTarget.WebGL&&(!Directory.Exists(Path.Combine(path,"Build"))||Directory.GetFiles(Path.Combine(path,"Build"),"*.wasm").Length==0))throw new Exception("WebGL postprocess produced no WASM. Restart Unity after installing the module.");Debug.Log("NIU_BUILD_OK "+r.summary.totalSize);}
 }
 public static class Validation {
  static int count;static void Check(bool value,string label){if(!value)throw new Exception("NIU_TEST_FAIL "+label);count++;}
  public static void Run(){count=0;var s=new ShelfRound();Check(!s.Place(5,1),"Wrong shelf rejected");Check(s.Placed==0,"Wrong placement does not change progress");Check(s.Place(1,1)&&s.Place(3,3)&&s.Place(5,5),"All three books accepted");Check(s.Placed==3&&!s.Place(1,1),"No duplicate book reward");
   Check(Book.Compare(new Book{classification="25.4",author="1000",year=2024},new Book{classification="50",author="1000",year=2020})<0,"Numeric classification");Check(Book.Compare(new Book{classification="25.4",author="1000",year=2020},new Book{classification="25.4",author="2000",year=2010})<0,"Secondary author ordering");
   var b=new DeerBoard();for(int x=1;x<=5;x++)Check(b.Toggle(x,0),"First five barriers allowed");Check(!b.Toggle(6,0)&&b.barriers.Count==5,"Sixth barrier rejected");Check(b.Toggle(1,0)&&b.barriers.Count==4,"Barrier removal allowed before release");Check(!b.Toggle(0,1)&&!b.Toggle(8,4),"Spawn and target blocked");
   b=new DeerBoard();b.Toggle(9,1);b.released=true;Check(!b.Toggle(4,1),"No edits after release");for(int i=0;i<40;i++)b.Step();Check(b.arrived==3&&!b.escaped,"Puzzle has a winning route for all deer");b.Step();Check(b.arrived==3,"No duplicate arrivals");
   b=new DeerBoard{released=true};for(int i=0;i<20;i++)b.Step();Check(b.escaped&&b.arrived==0,"Unprotected route escapes");
   var r=new RoundState{stage=RoundState.Stage.Playing};r.Tick(1);r.paused=true;r.Tick(9);Check(r.remaining==59,"Pause freezes clock");r.paused=false;r.stage=RoundState.Stage.Quiz;r.Tick(20);Check(r.remaining==59,"Quiz freezes clock");r.stage=RoundState.Stage.Playing;r.Tick(100);Check(r.remaining==0&&r.stage==RoundState.Stage.Failed,"Timeout ends round at zero");
   var score=new DemoScoreProvider().Evaluate("test","books",10.8f,2,1);Check(score.baseScore==300&&score.timeBonus==20&&score.mistakePenalty==40&&score.totalScore==280,"Score breakdown consistent");
   var c=JsonUtility.FromJson<LevelCatalog>(File.ReadAllText("Assets/Junting/Resources/Junting/levels.json"));Check(c.levels.Length==2,"Both assigned levels configured");foreach(var l in c.levels){Check(l.intro.Length>=3&&l.intro.Length<=5&&l.intro.All(x=>x.Length<=14),"Intro line contract "+l.id);Check(l.questions.Length>=2&&l.questions.All(q=>q.answer>=0&&q.answer<q.options.Length),"Quiz answers in range "+l.id);Check(l.knowledgeUrl.StartsWith("https://lic.niu.edu.tw/"),"Knowledge source official "+l.id);}
   Check(c.levels.Select(l=>l.bgmKey).Distinct().Count()==c.levels.Length,"Each level has a different BGM key");
   Check(c.levels.Select(l=>l.artifactId).Distinct().Count()==c.levels.Length,"Each level has its own single artifact");
   foreach(var l in c.levels){Check(!string.IsNullOrWhiteSpace(l.bgmKey)&&Resources.Load<AudioClip>("Audio/"+l.bgmKey)!=null,"BGM exists "+l.id);Check(!string.IsNullOrWhiteSpace(l.artifactId)&&!string.IsNullOrWhiteSpace(l.artifactName),"Single artifact configured "+l.id);}
   Debug.Log("NIU_TESTS_PASSED "+count);
  }
 }
}
