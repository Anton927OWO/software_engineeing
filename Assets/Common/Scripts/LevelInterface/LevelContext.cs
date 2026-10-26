using System;
using System.Collections.Generic;

namespace TrackingNIU
{
    /// <summary>關卡開始時收到的資料。在 Start() 裡用 LevelSession.Current 取得。</summary>
    public sealed class LevelContext
    {
        public LevelId LevelId { get; }
        /// <summary>關卡名稱，顯示在 HUD 左上角。</summary>
        public string DisplayName { get; }
        /// <summary>地點，顯示在關卡名稱下方。</summary>
        public string Location { get; }
        /// <summary>限時秒數；0 代表不限時（序章）。</summary>
        public float TimeLimit { get; }
        /// <summary>通關後獲得的文物編號。</summary>
        public ArtifactId Artifact { get; }
        /// <summary>每次失誤扣的分數，HUD 或提示文字需要時可以用。</summary>
        public int MistakePenalty { get; }
        public bool IsScored { get; }
        public bool IsFinale { get; }
        /// <summary>本局已取得的文物，終章開局翻開提示用；其他關卡用不到。</summary>
        public IReadOnlyList<ArtifactId> CollectedArtifacts { get; }
        /// <summary>true 代表由 LevelTestLauncher 單獨啟動，不是從 GameManager 進來。</summary>
        public bool IsTestRun { get; }

        public bool HasTimeLimit => TimeLimit > 0f;

        public LevelContext(LevelDefinition definition, IReadOnlyList<ArtifactId> collectedArtifacts = null,
            bool isTestRun = false, float timeLimitOverride = 0f)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            LevelId = definition.Id;
            DisplayName = definition.DisplayName;
            Location = definition.Location;
            TimeLimit = timeLimitOverride > 0f ? timeLimitOverride : definition.TimeLimit;
            Artifact = definition.Artifact;
            MistakePenalty = definition.MistakePenalty;
            IsScored = definition.IsScored;
            IsFinale = definition.IsFinale;
            CollectedArtifacts = collectedArtifacts ?? Array.Empty<ArtifactId>();
            IsTestRun = isTestRun;
        }

        public override string ToString()
        {
            var time = HasTimeLimit ? $"{TimeLimit} 秒" : "不限時";
            return $"{DisplayName}（{LevelId}）｜限時 {time}｜文物 {LevelCatalog.GetArtifactName(Artifact)}";
        }
    }
}
