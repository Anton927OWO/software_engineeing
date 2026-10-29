using System;

namespace NIUAdventure.Integration
{
    // Local handoff DTO only. No inventory, score, persistence or next-scene policy.
    public sealed class LevelHandoffResult
    {
        public string LevelId { get; }
        public string AttemptId { get; }
        public bool Success { get; }
        public float? RemainingSeconds { get; }
        public int? MistakeCount { get; }
        public string RewardArtifactId { get; }
        public bool IsRewardEligible { get; }
        public bool RequiresRewardConfiguration => IsRewardEligible && RewardArtifactId == null;

        public LevelHandoffResult(string levelId, string attemptId, bool success,
            float? remainingSeconds, int? mistakeCount, string rewardArtifactId, bool awardsArtifact)
        {
            LevelId = levelId;
            AttemptId = attemptId; // Temporary per-attempt token, not an artifact or save ID.
            Success = success;
            RemainingSeconds = remainingSeconds;
            MistakeCount = mistakeCount;
            IsRewardEligible = success && awardsArtifact;
            RewardArtifactId = IsRewardEligible && !string.IsNullOrWhiteSpace(rewardArtifactId)
                ? rewardArtifactId.Trim() : null;
        }
    }

    public interface ILevelHandoffSource
    {
        event Action<LevelHandoffResult> ResultReady;
        LevelHandoffResult LastHandoffResult { get; }
        bool IsReady { get; }
        bool ConfigureIntegration(string levelId, string rewardArtifactId);
        void BeginChallenge();
        void PauseGame();
        void ResumeGame();
        bool ResetForNewChallenge(); // Return to introduction, no automatic start or scene load.
    }
}
