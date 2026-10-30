using System;
using System.Collections.Generic;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Train
{
    public enum TrainPhase { Asking, Revealing, Coupling, Departing, Finished }

    /// <summary>Shared owns the clock/result; this state machine owns only the train and quiz.</summary>
    public sealed class TrainRound
    {
        public const int TargetCarriages = 5;
        public const float RevealSeconds = 1f;
        public const float CouplingSeconds = 0.7f;
        public const float DepartureSeconds = 2.2f;
        private readonly LevelSession session;
        private readonly List<TrainQuestion> deck = new List<TrainQuestion>();
        private int cursor;
        private float phaseElapsed;
        public TrainQuestion Current { get; private set; }
        public TrainPhase Phase { get; private set; }
        public int Carriages { get; private set; }
        public int AnsweredCount { get; private set; }
        public int QuestionCount => deck.Count;
        public bool LastWasCorrect { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public float PhaseProgress => Math.Min(1f, phaseElapsed / (Phase == TrainPhase.Departing ? DepartureSeconds :
            Phase == TrainPhase.Coupling ? CouplingSeconds : RevealSeconds));

        public TrainRound(LevelSession session, IEnumerable<TrainQuestion> questions, int seed)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (questions != null)
                foreach (var q in questions)
                    if (q != null && q.IsPlayable && ids.Add(q.id.Trim())) deck.Add(q);
            var random = new Random(seed);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = deck[i]; deck[i] = deck[j]; deck[j] = swap;
            }
            NextQuestion();
        }

        public bool Submit(int answerIndex)
        {
            if (!session.CanInteract || Phase != TrainPhase.Asking || Current == null || answerIndex < 0 || answerIndex > 2)
                return false;
            SelectedIndex = answerIndex;
            LastWasCorrect = answerIndex == Current.correctIndex;
            AnsweredCount++;
            phaseElapsed = 0;
            if (LastWasCorrect)
            {
                Carriages++;
                session.CollectArtifact($"train-carriage-{Carriages:00}");
                Phase = TrainPhase.Coupling;
            }
            else
            {
                Phase = TrainPhase.Revealing;
                session.RecordMistake();
            }
            return true;
        }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (session.Result != null) { Phase = TrainPhase.Finished; return; }
            if (!session.CanInteract) return;
            if (Current == null) { session.Fail(); Phase = TrainPhase.Finished; return; }
            if (Phase == TrainPhase.Asking || Phase == TrainPhase.Finished) return;
            phaseElapsed += seconds;
            float duration = Phase == TrainPhase.Revealing ? RevealSeconds :
                Phase == TrainPhase.Coupling ? CouplingSeconds : DepartureSeconds;
            if (phaseElapsed < duration) return;
            phaseElapsed = 0;
            if (Phase == TrainPhase.Departing)
            {
                Phase = TrainPhase.Finished;
                session.Succeed();
            }
            else if (Carriages >= TargetCarriages) Phase = TrainPhase.Departing;
            else if (cursor >= deck.Count) { Phase = TrainPhase.Finished; session.Fail(); }
            else NextQuestion();
        }

        private void NextQuestion()
        {
            Current = cursor < deck.Count ? deck[cursor++] : null;
            Phase = TrainPhase.Asking;
            phaseElapsed = 0;
            SelectedIndex = -1;
        }
    }
}
