using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackingNIU
{
    /// <summary>
    /// 關卡與 GameManager 之間唯一的連接點。每關的固定流程：
    /// 1. Start() 讀取 LevelSession.Current，開始空間介紹與小遊戲。
    /// 2. 小遊戲結束時呼叫 Report(result)，拿到得分明細，顯示在知識卡上。
    /// 3. 玩家在知識卡按「繼續」時呼叫 Exit()，回到地圖（測試時在 Console 印出結果）。
    /// 失敗時可以呼叫 Retry() 重新載入本關，或照常 Report → Exit 繼續遊戲。
    /// </summary>
    public static class LevelSession
    {
        /// <summary>參數依序為關卡結果與得分明細。</summary>
        public delegate void ExitHandler(LevelResult result, ScoreBreakdown score);

        static ExitHandler s_onExit;

        /// <summary>目前關卡的開始資料；沒有關卡在進行時為 null。</summary>
        public static LevelContext Current { get; private set; }
        public static bool IsActive => Current != null;
        public static LevelResult Result { get; private set; }
        public static ScoreBreakdown Score { get; private set; }
        public static bool HasReported => Result != null;

        /// <summary>由 GameManager（或 LevelTestLauncher）在載入關卡 Scene 前呼叫。</summary>
        public static void Begin(LevelContext context, ExitHandler onExit)
        {
            if (IsActive)
                Debug.LogWarning($"[LevelSession] {Current.LevelId} 尚未 Exit 就開始 {context?.LevelId}，前一關的結果會被丟棄。");

            Current = context ?? throw new ArgumentNullException(nameof(context));
            s_onExit = onExit;
            Result = null;
            Score = default;
        }

        /// <summary>小遊戲結束時呼叫，回傳得分明細給知識卡顯示。每次遊玩只能呼叫一次。</summary>
        public static ScoreBreakdown Report(LevelResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!IsActive)
            {
                Debug.LogError("[LevelSession] 沒有進行中的關卡。請確認 Scene 裡有 LevelTestLauncher。");
                return ScoreBreakdown.NotScored;
            }
            if (HasReported)
            {
                Debug.LogWarning($"[LevelSession] {Current.LevelId} 已經回報過結果，這次回報會被忽略。");
                return Score;
            }

            result.LevelId = Current.LevelId;
            result.Artifact = Current.Artifact;
            result.Mistakes = Math.Max(0, result.Mistakes);
            result.RemainingTime = Current.HasTimeLimit ? Mathf.Clamp(result.RemainingTime, 0f, Current.TimeLimit) : 0f;

            Result = result;
            Score = ScoreCalculator.Calculate(Current, result);
            return Score;
        }

        /// <summary>知識卡按「繼續」時呼叫，離開關卡。</summary>
        public static void Exit()
        {
            if (!IsActive)
            {
                Debug.LogError("[LevelSession] 沒有進行中的關卡，無法 Exit。");
                return;
            }
            if (!HasReported)
            {
                Debug.LogError($"[LevelSession] {Current.LevelId} 要先呼叫 Report(result) 才能 Exit。");
                return;
            }

            var onExit = s_onExit;
            var result = Result;
            var score = Score;
            Current = null;
            s_onExit = null;
            Result = null;
            Score = default;

            onExit?.Invoke(result, score);
        }

        /// <summary>重新載入目前的關卡 Scene，開始資料保持不變，之前的結果清除。</summary>
        public static void Retry()
        {
            if (!IsActive)
            {
                Debug.LogError("[LevelSession] 沒有進行中的關卡，無法 Retry。");
                return;
            }

            Result = null;
            Score = default;
            SceneLoader.Load(SceneManager.GetActiveScene().name);
        }

        /// <summary>GameManager 載入關卡 Scene 失敗時，取消已經開始的關卡，不呼叫結束回呼。</summary>
        internal static void Abort()
        {
            Current = null;
            s_onExit = null;
            Result = null;
            Score = default;
        }

        // 關閉 Domain Reload 時，靜態欄位會保留上一次 Play 的值，這裡手動清除。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Abort();
    }
}
