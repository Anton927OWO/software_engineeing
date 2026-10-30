using UnityEngine;

// Two local sources allow short feedback and looping music to play independently.
public sealed class StrataLevelAudio : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClick, correct, incorrect, failure, stageSwitch, levelBgm;
    [SerializeField, Range(0,1)] private float buttonVolume=.5f, correctVolume=.6f,
        incorrectVolume=.6f, failureVolume=.6f, switchVolume=.5f, bgmVolume=.12f;
    private AudioSource effects, music;
    private bool suspended;
    void Awake() { EnsureSources(); }
    private void EnsureSources()
    {
        if (effects==null) effects=CreateSource("StrataFeedbackAudio",false);
        if (music==null) music=CreateSource("StrataMusicAudio",true);
    }
    private AudioSource CreateSource(string name,bool loop)
    {
        var child=new GameObject(name); child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>(); source.playOnAwake=false;
        source.loop=loop; source.spatialBlend=0; source.dopplerLevel=0;
        return source;
    }
    private void OneShot(AudioClip clip,float volume)
    { EnsureSources(); if(clip!=null) effects.PlayOneShot(clip,volume); }
    public void PlayButton() { OneShot(buttonClick,buttonVolume); }
    public void PlayFeedback(bool success)
    { if(!suspended) OneShot(success?correct:incorrect,success?correctVolume:incorrectVolume); }
    public void PlayStageSwitch() { if(!suspended) OneShot(stageSwitch,switchVolume); }
    public void RestartMusic()
    {
        EnsureSources(); suspended=false; effects.Stop(); music.Stop();
        music.clip=levelBgm; music.volume=bgmVolume;
        if(levelBgm!=null) music.Play();
    }
    public void PauseMusic()
    { EnsureSources(); suspended=true; music.Pause(); effects.Stop(); }
    public void ResumeMusic()
    { EnsureSources(); suspended=false; if(music.clip!=null) music.UnPause(); }
    public void PlayResult(bool success)
    {
        EnsureSources(); suspended=true; music.Stop(); effects.Stop();
        OneShot(success?correct:failure,success?correctVolume:failureVolume);
    }
    void OnDisable()
    { if(music!=null) music.Stop(); if(effects!=null) effects.Stop(); }
}
