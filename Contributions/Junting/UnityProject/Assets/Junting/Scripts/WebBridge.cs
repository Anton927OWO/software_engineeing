using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace NIU.Junting {
 // Incoming browser calls target JuntingGame.StartLevel("books" or "deer").
 public static class WebBridge {
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")] static extern void NIU_Notify(string json);
#endif
  public static void Notify(string type,LevelResult result){string json=JsonUtility.ToJson(new Envelope{type=type,result=result});
#if UNITY_WEBGL && !UNITY_EDITOR
   NIU_Notify(json);
#else
   Debug.Log("NIU_EVENT "+json);
#endif
  }
  [Serializable] class Envelope {public string type;public LevelResult result;}
 }
}
