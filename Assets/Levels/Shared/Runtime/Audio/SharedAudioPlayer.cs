using UnityEngine;

namespace XunJiYiDa.Levels.Shared
{
    [DisallowMultipleComponent]
    public sealed class SharedAudioPlayer : MonoBehaviour
    {
        [SerializeField] private SharedAudioLibrary library;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource gameplaySource;
        [SerializeField, Range(0, 1)] private float volume = 1f;
        [SerializeField] private bool muted;
        private bool gameplayPaused;
        private AudioSource musicSource;
        private float musicGain;

        private void Awake()
        {
            if (uiSource != null) { uiSource.ignoreListenerPause = true; uiSource.spatialBlend = 0; }
            if (gameplaySource != null) gameplaySource.spatialBlend = 0;
        }

        public void Play(SharedSound sound)
        {
            if (library != null && library.TryGet(sound, out var clip, out var gain))
                PlayClip(clip, true, gain);
        }
        public void PlayClick() => Play(SharedSound.Click);
        public void PlayCorrect() => Play(SharedSound.Correct);
        public void PlayWrong() => Play(SharedSound.Wrong);

        /// <summary>Null clips are intentionally allowed for replaceable placeholder sounds.</summary>
        public void PlayClip(AudioClip clip, bool isUI = false, float gain = 1)
        {
            if (muted || clip == null || (!isUI && gameplayPaused)) return;
            var source = isUI ? uiSource : gameplaySource;
            if (source != null) source.PlayOneShot(clip, Mathf.Clamp01(gain) * volume);
        }

        /// <summary>Starts looping music from the beginning; it pauses and resumes with gameplay.</summary>
        public void PlayMusic(AudioClip clip, float gain)
        {
            StopMusic();
            if (clip == null) return;
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0;
            }
            musicGain = Mathf.Clamp01(gain);
            musicSource.clip = clip;
            musicSource.volume = musicGain * volume;
            musicSource.mute = muted;
            musicSource.Play();
            if (gameplayPaused) musicSource.Pause();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            if (uiSource != null) uiSource.mute = value;
            if (gameplaySource != null) gameplaySource.mute = value;
            if (musicSource != null) musicSource.mute = value;
        }
        public void SetVolume(float value)
        {
            volume = float.IsNaN(value) ? 0 : Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicGain * volume;
        }
        public void SetGameplayPaused(bool paused)
        {
            if (gameplayPaused == paused) return;
            gameplayPaused = paused;
            if (musicSource != null)
            {
                if (paused) musicSource.Pause();
                else musicSource.UnPause();
            }
            if (gameplaySource == null) return;
            if (paused) gameplaySource.Pause();
            else gameplaySource.UnPause();
        }
    }
}
