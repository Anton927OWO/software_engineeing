using UnityEngine;

public class PrologueAudio : MonoBehaviour
{
    [Header("音效來源")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioSource musicSource;
    private bool musicPaused;
    public void SetPaused(bool paused)
    {
        if(musicSource==null) return;
        if(paused && !musicPaused && musicSource.isPlaying) { musicSource.Pause(); musicPaused=true; }
        else if(!paused && musicPaused) { musicSource.UnPause(); musicPaused=false; }
    }
    public void RestartMusic()
    {
        musicPaused=false;
        if(musicSource==null || musicSource.clip==null) return;
        musicSource.Stop(); musicSource.Play();
    }
    public void StopMusic()
    { musicPaused=false; if(musicSource!=null) musicSource.Stop(); }

    [Header("序章音效")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip completeSound;

    // 點擊紅色書本
    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    // 書本成功放入書箱
    public void PlayCollectSound()
    {
        PlaySound(collectSound);
    }

    // 完成新手教學
    public void PlayCompleteSound()
    {
        PlaySound(completeSound);
    }

    // 統一播放音效
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
