using System;
using System.Collections.Generic;

namespace XunJiYiDa.Levels.Shared
{
    /// <summary>An immutable snapshot. Artifact IDs are copied before publication.</summary>
    [Serializable]
    public sealed class LevelResult
    {
        public string LevelId { get; }
        public bool Success { get; }
        public float RemainingTime { get; }
        public int MistakeCount { get; }
        public IReadOnlyList<string> ArtifactIds { get; }

        internal LevelResult(string levelId, bool success, float time, int mistakes, List<string> artifacts)
        {
            LevelId = levelId;
            Success = success;
            RemainingTime = time;
            MistakeCount = mistakes;
            ArtifactIds = Array.AsReadOnly(artifacts.ToArray());
        }
    }

    public interface IFinalePairProgress
    {
        int MatchedPairs { get; }
    }

    public interface ILevelResultReceiver
    {
        void ReceiveLevelResult(LevelResult result);
    }
}
