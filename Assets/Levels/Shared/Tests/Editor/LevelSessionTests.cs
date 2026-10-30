using System;
using NUnit.Framework;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Shared.Tests
{
    public sealed class LevelSessionTests
    {
        [Test]
        public void ReadyAndPauseDoNotConsumeTime()
        {
            var session = new LevelSession("shared", 120f);
            session.Tick(10f);
            Assert.That(session.RemainingTime, Is.EqualTo(120f));
            session.Begin();
            session.Tick(1.25f);
            session.SetPaused(PauseReason.Manual, true);
            session.Tick(50f);
            Assert.That(session.RemainingTime, Is.EqualTo(118.75f));
        }

        [Test]
        public void ClosingModalDoesNotCancelManualPause()
        {
            var session = new LevelSession("shared", 30f);
            session.Begin();
            session.SetPaused(PauseReason.Modal, true);
            session.SetPaused(PauseReason.Manual, true);
            session.SetPaused(PauseReason.Modal, false);
            Assert.That(session.State, Is.EqualTo(LevelState.Paused));
            session.SetPaused(PauseReason.Manual, false);
            session.Tick(2f);
            Assert.That(session.RemainingTime, Is.EqualTo(28f));
        }

        [Test]
        public void TimeoutClampsAtZeroAndCompletesExactlyOnce()
        {
            var session = new LevelSession("shared", 2f);
            int completed = 0;
            session.Completed += _ => completed++;
            session.Begin();
            session.Tick(100f);
            session.Tick(100f);
            session.Succeed();
            Assert.That(session.State, Is.EqualTo(LevelState.Failed));
            Assert.That(session.Result.RemainingTime, Is.Zero);
            Assert.That(session.Result.Success, Is.False);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void ResultKeepsItsOwnDeduplicatedArtifactsAcrossRetry()
        {
            var session = new LevelSession("shared", 60f);
            session.Begin();
            session.RecordMistake();
            session.CollectArtifact("artifact-01");
            session.CollectArtifact("artifact-01");
            session.Tick(4f);
            session.Succeed();
            var result = session.Result;
            session.Retry();
            Assert.That(result.LevelId, Is.EqualTo("shared"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.RemainingTime, Is.EqualTo(56f));
            Assert.That(result.MistakeCount, Is.EqualTo(1));
            Assert.That(result.ArtifactIds, Is.EqualTo(new[] { "artifact-01" }));
            Assert.That(session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(session.RemainingTime, Is.EqualTo(60f));
            Assert.That(session.MistakeCount, Is.Zero);
            Assert.That(session.ArtifactIds, Is.Empty);
            Assert.That(session.Result, Is.Null);
        }

        [Test]
        public void PausedAndFinishedSessionsRejectGameplayWrites()
        {
            var session = new LevelSession("shared", 10f);
            session.Begin();
            session.SetPaused(PauseReason.Focus, true);
            Assert.That(session.RecordMistake(), Is.False);
            Assert.That(session.CollectArtifact("a"), Is.False);
            Assert.That(session.Succeed(), Is.False);
            session.SetPaused(PauseReason.Focus, false);
            session.Succeed();
            Assert.That(session.RecordMistake(), Is.False);
            Assert.That(session.CollectArtifact("b"), Is.False);
        }

        [Test]
        public void MistakeLimitFailsOnTheConfiguredAttempt()
        {
            var session = new LevelSession("shared", 10f, 2);
            session.Begin();
            session.RecordMistake();
            Assert.That(session.State, Is.EqualTo(LevelState.Running));
            session.RecordMistake();
            Assert.That(session.State, Is.EqualTo(LevelState.Failed));
            Assert.That(session.Result.MistakeCount, Is.EqualTo(2));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void InvalidTickCannotCorruptTheClock(float delta)
        {
            var session = new LevelSession("shared", 10f);
            session.Begin();
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(delta));
            Assert.That(session.RemainingTime, Is.EqualTo(10f));
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new LevelSession(" ", 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSession("x", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSession("x", float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelSession("x", 10, -1));
        }

        [Test]
        public void RetryFromTerminalStateWaitsUntilResultHasBeenPublished()
        {
            var session = new LevelSession("shared", 10);
            int completions = 0;
            session.StateChanged += state => { if (state == LevelState.Succeeded) session.Retry(); };
            session.Completed += result =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(session.State, Is.EqualTo(LevelState.Succeeded));
                completions++;
            };
            session.Begin();
            session.Succeed();
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(session.Result, Is.Null);
        }
    }
}
