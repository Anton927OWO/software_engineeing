using UnityEngine;

namespace TrackingNIU.XR.Samples
{
    /// <summary>示範用的可刷表面：被刷到就慢慢變薄，太用力時震動警告。考古小隊的土堆可以參考這個寫法。</summary>
    public sealed class DemoBrushSurface : MonoBehaviour, IBrushSurface
    {
        [SerializeField, Min(0.001f), Tooltip("每公尺刷動距離削去的厚度")]
        float wearPerMeter = 0.02f;

        [SerializeField, Tooltip("壓得比這個深就算太用力")]
        float tooDeep = 0.015f;

        [SerializeField, Min(0.001f)] float minThickness = 0.005f;

        float _nextWarning;

        public void OnBrush(in BrushContact contact)
        {
            if (contact.Depth > tooDeep)
            {
                if (Time.time >= _nextWarning)
                {
                    _nextWarning = Time.time + 0.5f;
                    contact.Tool.Pulse(HapticPreset.Warning);
                }
                return;
            }

            var scale = transform.localScale;
            scale.y = Mathf.Max(minThickness, scale.y - contact.Speed * Time.fixedDeltaTime * wearPerMeter);
            transform.localScale = scale;
        }
    }
}
