using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR
{
    public enum HapticPreset
    {
        Tick,     // 輕觸：碰到物件、翻頁
        Correct,  // 放對
        Wrong,    // 放錯
        Warning,  // 警告：刷太用力、時間快到
    }

    public enum Hand
    {
        Left,
        Right,
    }

    /// <summary>手把震動。重要回饋（放對、放錯、損傷）請同時搭配震動。</summary>
    public static class Haptics
    {
        public static void GetPreset(HapticPreset preset, out float amplitude, out float duration)
        {
            switch (preset)
            {
                case HapticPreset.Correct: amplitude = 0.4f; duration = 0.08f; break;
                case HapticPreset.Wrong: amplitude = 0.7f; duration = 0.2f; break;
                case HapticPreset.Warning: amplitude = 0.9f; duration = 0.3f; break;
                default: amplitude = 0.2f; duration = 0.03f; break;
            }
        }

        /// <summary>讓操作這個 interactor 的那隻手震動。</summary>
        public static void Play(IXRInteractor interactor, HapticPreset preset)
        {
            if (interactor == null || interactor.transform == null)
                return;

            GetPreset(preset, out var amplitude, out var duration);
            var player = interactor.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (player != null)
            {
                player.SendHapticImpulse(amplitude, duration);
                return;
            }

            if (TryGetHand(interactor.transform, out var hand))
                Play(hand, preset);
        }

        public static void Play(Hand hand, HapticPreset preset)
        {
            GetPreset(preset, out var amplitude, out var duration);
            var device = hand == Hand.Left ? XRController.leftHand : XRController.rightHand;
            if (device is XRControllerWithRumble rumble)
                rumble.SendImpulse(amplitude, duration);
        }

        public static void PlayBoth(HapticPreset preset)
        {
            Play(Hand.Left, preset);
            Play(Hand.Right, preset);
        }

        static bool TryGetHand(Transform t, out Hand hand)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.Contains("Left"))
                {
                    hand = Hand.Left;
                    return true;
                }
                if (t.name.Contains("Right"))
                {
                    hand = Hand.Right;
                    return true;
                }
            }
            hand = Hand.Right;
            return false;
        }
    }
}
