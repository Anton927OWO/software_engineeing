using System;
using UnityEngine;

namespace XunJiYiDa.Levels.Shared
{
    public enum SharedSound { Click, Correct, Wrong, Victory, Tick }

    [CreateAssetMenu(menuName = "尋跡宜大/Shared/Audio Library")]
    public sealed class SharedAudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SharedSound sound;
            public AudioClip clip;
            [Range(0, 1)] public float volume;
        }
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        public bool TryGet(SharedSound sound, out AudioClip clip, out float volume)
        {
            foreach (var entry in entries)
                if (entry.sound == sound)
                {
                    clip = entry.clip;
                    volume = Mathf.Clamp01(entry.volume);
                    return clip != null;
                }
            clip = null;
            volume = 0;
            return false;
        }
    }
}
