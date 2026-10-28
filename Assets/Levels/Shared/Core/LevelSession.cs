using System;
using System.Collections.Generic;

namespace XunJiYiDa.Levels.Shared
{
    public sealed class LevelSession
    {
        private readonly string levelId;
        private readonly float duration;
        private readonly int mistakeLimit;
        private readonly List<string> artifacts = new List<string>();
        private readonly IReadOnlyList<string> artifactView;
        private bool publishingResult;
        private bool retryRequested;
        public event Action<LevelState> StateChanged;
        public event Action<LevelResult> Completed;
        public event Action Retried;
        public LevelState State { get; private set; }
        public PauseReason PauseReasons { get; private set; }
        public float RemainingTime { get; private set; }
        public int MistakeCount { get; private set; }
        public IReadOnlyList<string> ArtifactIds => artifactView;
        public LevelResult Result { get; private set; }
        public bool CanInteract => State == LevelState.Running;

        public LevelSession(string levelId, float duration, int mistakeLimit = 0)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level ID is required.", nameof(levelId));
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            if (mistakeLimit < 0) throw new ArgumentOutOfRangeException(nameof(mistakeLimit));
            this.levelId = levelId;
            this.duration = duration;
            this.mistakeLimit = mistakeLimit;
            artifactView = artifacts.AsReadOnly();
            RemainingTime = duration;
            State = LevelState.Ready;
        }

        public bool Begin()
        {
            if (State != LevelState.Ready) return false;
            ChangeState(PauseReasons == PauseReason.None ? LevelState.Running : LevelState.Paused);
            return true;
        }

        /// <summary>Pass unscaled seconds. Paused/ready/terminal sessions do not consume time.</summary>
        public void Tick(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            if (!CanInteract) return;
            RemainingTime = Math.Max(0, RemainingTime - delta);
            if (RemainingTime <= 0) Finish(false);
        }

        public void SetPaused(PauseReason reason, bool paused)
        {
            const PauseReason valid = PauseReason.Manual | PauseReason.Modal | PauseReason.Focus;
            if (reason == PauseReason.None || (reason & ~valid) != 0)
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (State == LevelState.Succeeded || State == LevelState.Failed) return;
            var next = paused ? PauseReasons | reason : PauseReasons & ~reason;
            if (next == PauseReasons) return;
            PauseReasons = next;
            // Publish even if still paused: the UI must distinguish manual and modal pauses.
            if (State == LevelState.Ready) StateChanged?.Invoke(State);
            else ChangeState(next == PauseReason.None ? LevelState.Running : LevelState.Paused);
        }

        public bool RecordMistake()
        {
            if (!CanInteract) return false;
            MistakeCount++;
            if (mistakeLimit > 0 && MistakeCount >= mistakeLimit) Finish(false);
            return true;
        }

        public bool CollectArtifact(string id)
        {
            if (!CanInteract || string.IsNullOrWhiteSpace(id)) return false;
            id = id.Trim();
            if (artifacts.Contains(id)) return false;
            artifacts.Add(id);
            return true;
        }

        public bool Succeed() => CanInteract && Finish(true);
        public bool Fail() => CanInteract && Finish(false);

        /// <summary>Reset to Ready. Level-specific objects must reset through the controller's Retried event.</summary>
        public void Retry()
        {
            if (publishingResult) { retryRequested = true; return; }
            RemainingTime = duration;
            MistakeCount = 0;
            artifacts.Clear();
            Result = null;
            PauseReasons = PauseReason.None;
            ChangeState(LevelState.Ready);
            Retried?.Invoke();
        }

        private bool Finish(bool success)
        {
            if (!CanInteract) return false;
            var result = new LevelResult(levelId, success, RemainingTime, MistakeCount, artifacts);
            Result = result;
            PauseReasons = PauseReason.None;
            publishingResult = true;
            try
            {
                ChangeState(success ? LevelState.Succeeded : LevelState.Failed);
                Completed?.Invoke(result);
            }
            finally
            {
                publishingResult = false;
                if (retryRequested) { retryRequested = false; Retry(); }
            }
            return true;
        }

        private void ChangeState(LevelState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
