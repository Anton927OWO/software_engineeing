using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Train.Tests
{
    public sealed class TrainRoundTests
    {
        private static TrainQuestion[] Questions(int count = 12) => Enumerable.Range(0, count).Select(i =>
            new TrainQuestion { id = "q-" + i, prompt = "題目 " + i, options = new[] { "甲", "乙", "丙" }, correctIndex = i % 3 }).ToArray();
        private static LevelSession Session() { var s = new LevelSession("Train", 60); s.Begin(); return s; }

        [Test]
        public void DeckUsesEveryQuestionOnceAndFailsOnlyAfterLastReveal()
        {
            var s = Session(); var r = new TrainRound(s, Questions(), 17); var seen = new HashSet<string>();
            for (int i = 0; i < 12; i++)
            {
                Assert.That(seen.Add(r.Current.id), Is.True);
                Assert.That(r.Submit((r.Current.correctIndex + 1) % 3), Is.True);
                r.Advance(0.99f);
                Assert.That(s.Result, Is.Null);
                r.Advance(0.011f);
            }
            Assert.That(s.Result.Success, Is.False);
            Assert.That(s.Result.MistakeCount, Is.EqualTo(12));
            Assert.That(s.Result.ArtifactIds, Is.Empty);
        }

        [Test]
        public void FiveCarriagesMustCoupleThenLeaveBeforeOneSuccessResult()
        {
            var s = Session(); var r = new TrainRound(s, Questions(), 3); int results = 0;
            s.Completed += _ => results++;
            for (int i = 0; i < 5; i++)
            {
                Assert.That(r.Submit(r.Current.correctIndex), Is.True);
                Assert.That(r.Carriages, Is.EqualTo(i + 1));
                Assert.That(r.Submit(r.Current.correctIndex), Is.False, "Repeated input must not score.");
                Assert.That(s.Result, Is.Null);
                r.Advance(TrainRound.CouplingSeconds);
            }
            Assert.That(r.Phase, Is.EqualTo(TrainPhase.Departing));
            r.Advance(2.19f); Assert.That(s.Result, Is.Null);
            r.Advance(0.02f);
            Assert.That(s.Result.Success, Is.True);
            Assert.That(s.Result.LevelId, Is.EqualTo("Train"));
            Assert.That(s.Result.ArtifactIds, Is.EqualTo(Enumerable.Range(1, 5).Select(i => $"train-carriage-{i:00}")));
            r.Advance(50); Assert.That(results, Is.EqualTo(1));
        }

        [Test]
        public void WrongAnswerRecordsOneMistakeAndLocksForOneSecond()
        {
            var s = Session(); var r = new TrainRound(s, Questions(), 4); string previous = r.Current.id;
            r.Submit((r.Current.correctIndex + 1) % 3);
            Assert.That(r.Phase, Is.EqualTo(TrainPhase.Revealing));
            Assert.That(r.Submit(0), Is.False);
            r.Advance(0.999f); Assert.That(r.Current.id, Is.EqualTo(previous));
            r.Advance(0.002f); Assert.That(r.Current.id, Is.Not.EqualTo(previous));
            Assert.That(s.MistakeCount, Is.EqualTo(1)); Assert.That(r.Carriages, Is.Zero);
        }

        [TestCase(false)] [TestCase(true)]
        public void PausingFreezesBothRevealAndCoupling(bool correct)
        {
            var s = Session(); var r = new TrainRound(s, Questions(), 5);
            r.Submit((r.Current.correctIndex + (correct ? 0 : 1)) % 3);
            r.Advance(0.2f); float progress = r.PhaseProgress;
            s.SetPaused(PauseReason.Manual, true); s.Tick(30); r.Advance(30);
            Assert.That(r.PhaseProgress, Is.EqualTo(progress)); Assert.That(s.RemainingTime, Is.EqualTo(60));
            s.SetPaused(PauseReason.Modal, true); s.SetPaused(PauseReason.Manual, false);
            r.Advance(30); Assert.That(r.PhaseProgress, Is.EqualTo(progress));
            s.SetPaused(PauseReason.Modal, false); r.Advance(1);
            Assert.That(r.Phase, Is.EqualTo(TrainPhase.Asking));
        }

        [Test]
        public void TimeOutWinsEvenDuringDepartureAndDoesNotPublishTwice()
        {
            var s = Session(); var r = new TrainRound(s, Questions(), 2); int count = 0; s.Completed += _ => count++;
            for (int i = 0; i < 5; i++) { r.Submit(r.Current.correctIndex); r.Advance(0.7f); }
            s.Tick(60); r.Advance(9);
            Assert.That(s.Result.Success, Is.False); Assert.That(s.Result.RemainingTime, Is.Zero);
            Assert.That(r.Submit(0), Is.False); Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void ExhaustedBankWithFourCarriagesFails()
        {
            var s = Session(); var r = new TrainRound(s, Questions(4), 5);
            for (int i = 0; i < 4; i++) { r.Submit(r.Current.correctIndex); r.Advance(0.7f); }
            Assert.That(r.Carriages, Is.EqualTo(4)); Assert.That(s.Result.Success, Is.False);
        }

        [Test]
        public void FiveQuestionBankCanStillWinOnItsLastQuestion()
        {
            var s = Session(); var r = new TrainRound(s, Questions(5), 9);
            for (int i = 0; i < 5; i++) { r.Submit(r.Current.correctIndex); r.Advance(0.7f); }
            r.Advance(2.2f); Assert.That(s.Result.Success, Is.True);
        }

        [Test]
        public void InvalidAndDuplicateQuestionIdsAreSkippedWithoutRepeating()
        {
            var qs = Questions(2).ToList(); qs.Add(qs[0]); qs.Add(null);
            qs.Add(new TrainQuestion { id = "bad", prompt = "bad", options = new[] { "same", "same", "x" } });
            var r = new TrainRound(Session(), qs, 1); Assert.That(r.QuestionCount, Is.EqualTo(2));
        }

        [Test]
        public void EmptyBankFailsWhenRunningWithoutCrashing()
        {
            var s = Session(); var r = new TrainRound(s, null, 1); r.Advance(0);
            Assert.That(s.Result.Success, Is.False);
        }

        [Test]
        public void ReadyPausedInvalidAndTerminalInputsCannotScore()
        {
            var s = new LevelSession("Train", 60); var r = new TrainRound(s, Questions(), 1);
            Assert.That(r.Submit(0), Is.False); s.Begin();
            Assert.That(r.Submit(-1), Is.False); Assert.That(r.Submit(3), Is.False);
            s.SetPaused(PauseReason.Focus, true); Assert.That(r.Submit(0), Is.False);
            s.SetPaused(PauseReason.Focus, false); s.Fail(); Assert.That(r.Submit(0), Is.False);
            Assert.That(r.AnsweredCount, Is.Zero);
        }

        [Test]
        public void RetryBuildsFreshRoundAndClearsSessionArtifactsMistakesAndResult()
        {
            var s = Session(); var qs = Questions(); var r = new TrainRound(s, qs, 11);
            r.Submit(r.Current.correctIndex); r.Advance(0.7f);
            r.Submit((r.Current.correctIndex + 1) % 3); s.Fail(); s.Retry();
            var next = new TrainRound(s, qs, 12);
            Assert.That(next.Carriages, Is.Zero); Assert.That(next.AnsweredCount, Is.Zero);
            Assert.That(s.RemainingTime, Is.EqualTo(60)); Assert.That(s.MistakeCount, Is.Zero);
            Assert.That(s.Result, Is.Null); Assert.That(s.ArtifactIds, Is.Empty);
        }

        [Test]
        public void UnverifiedQuestionIsExplicitlyMarkedInKnowledgeCard()
        {
            var q = Questions(1)[0]; q.sourceUrl = "https://www.niu.edu.tw";
            Assert.That(q.VerificationLabel, Is.EqualTo("待驗證"));
            Assert.That(q.KnowledgeText, Does.Contain("待驗證"));
            q.verified = true; q.verifiedOn = "2026-10-26";
            Assert.That(q.VerificationLabel, Does.StartWith("已查核"));
        }

        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidDeltaIsRejected(float delta)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new TrainRound(Session(), Questions(), 1).Advance(delta));
    }
}
