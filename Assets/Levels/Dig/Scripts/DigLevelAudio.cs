using UnityEngine;
public sealed class DigLevelAudio : MonoBehaviour
{
    [Header("請指定已確認授權的音檔；空白時保持靜音")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip brushLoop;
    [SerializeField] private AudioClip artifactDiscovery;
    [SerializeField] private AudioClip artifactDamage;
    [SerializeField] private AudioClip challengeSuccess;
    [SerializeField] private AudioClip challengeFailure;
    [SerializeField] private AudioClip levelBgm;
    [Header("提示音音量（總音量乘上個別音量）")]
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float discoveryVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float damageVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float successVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float failureVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float brushVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.15f;
    private AudioSource effects, brush, music;
    private float lastBrushMotion = -999f;
    private bool paused;
    void Awake()
    {
        effects = MakeSource(false, effectsVolume);
        brush = MakeSource(true, brushVolume);
        music = MakeSource(true, bgmVolume);
    }
    private AudioSource MakeSource(bool loop, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }
    public void StartMusic()
    {
        if (levelBgm == null || music == null || music.isPlaying) return;
        music.clip = levelBgm;
        music.Play();
    }
    public void StopMusic() { if(music!=null) music.Stop(); }
    public void RestartMusic()
    {
        StopBrush(); paused=false;
        if(effects!=null) effects.Stop();
        if(music!=null) music.Stop();
        StartMusic();
    }
    public void Click() => Play(buttonClick, buttonClickVolume);
    public void Discovery() => Play(artifactDiscovery, discoveryVolume);
    public void Damage() => Play(artifactDamage, damageVolume);
    public void Result(bool success) => Play(success ? challengeSuccess : challengeFailure,
        success ? successVolume : failureVolume);
    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && effects != null) effects.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
    public void BrushMotion()
    {
        lastBrushMotion = Time.unscaledTime;
        if (paused || brushLoop == null || brush == null || brush.isPlaying) return;
        brush.clip = brushLoop;
        brush.Play();
    }
    public void StopBrush()
    {
        lastBrushMotion = -999f;
        if (brush != null && brush.isPlaying) brush.Stop();
    }
    public void SetPaused(bool value)
    {
        paused = value;
        StopBrush();
        if (music == null) return;
        if (value) music.Pause(); else music.UnPause();
    }
    void Update()
    {
        if (Time.unscaledTime - lastBrushMotion > 0.12f) StopBrush();
    }
}
