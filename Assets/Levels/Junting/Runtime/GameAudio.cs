using System;
using System.Collections.Generic;
using UnityEngine;
namespace XunJiYiDa.Levels.Junting {
 [Serializable] public struct NamedClip { public string key; public AudioClip clip; }
 public sealed class GameAudio:MonoBehaviour {
  AudioSource music,effects;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();NamedClip[] fallbacks=Array.Empty<NamedClip>();
  public bool MusicEnabled {get;private set;}=true;public bool EffectsEnabled {get;private set;}=true;
  void Awake(){music=gameObject.AddComponent<AudioSource>();effects=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.35f;effects.volume=.7f;MusicEnabled=PlayerPrefs.GetInt("niu.music",1)==1;EffectsEnabled=PlayerPrefs.GetInt("niu.sfx",1)==1;music.mute=!MusicEnabled;effects.mute=!EffectsEnabled;}
  public void SetFallbacks(NamedClip[] value){fallbacks=value??Array.Empty<NamedClip>();clips.Clear();}
  // 原始音效放在 Resources/Junting/Audio；缺檔時改用場景指定的替代音效。
  AudioClip Clip(string name){if(!clips.ContainsKey(name)){var clip=Resources.Load<AudioClip>("Junting/Audio/"+name);if(!clip)foreach(var f in fallbacks)if(f.key==name){clip=f.clip;break;}clips[name]=clip;}return clips[name];}
  public string CurrentBgmKey {get;private set;}
  public void StopMusic(){music.Stop();music.clip=null;CurrentBgmKey=null;}
  public void PlayBgm(string key){if(string.IsNullOrWhiteSpace(key)){StopMusic();Debug.LogError("Level has no BGM key");return;}var clip=Clip(key);if(!clip){StopMusic();Debug.LogWarning("Missing level BGM: "+key);return;}CurrentBgmKey=key;if(music.clip!=clip){music.Stop();music.clip=clip;music.Play();}else if(!music.isPlaying)music.Play();}
  public void Play(string name){var clip=Clip(name);if(clip&&EffectsEnabled)effects.PlayOneShot(clip);}
  public void SetMusic(bool value){MusicEnabled=value;music.mute=!value;PlayerPrefs.SetInt("niu.music",value?1:0);PlayerPrefs.Save();}
  public void SetEffects(bool value){EffectsEnabled=value;effects.mute=!value;PlayerPrefs.SetInt("niu.sfx",value?1:0);PlayerPrefs.Save();}
  public void Pause(bool paused){if(paused){music.Pause();effects.Stop();}else if(music.clip)music.UnPause();}
 }
}
