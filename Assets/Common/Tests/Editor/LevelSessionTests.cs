using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TrackingNIU.Tests
{
    public class LevelSessionTests
    {
        LevelResult _exitResult;
        ScoreBreakdown _exitScore;
        int _exitCount;

        [SetUp]
        public void SetUp()
        {
            _exitResult = null;
            _exitScore = default;
            _exitCount = 0;
            LevelSession.Begin(new LevelContext(LevelCatalog.Get(LevelId.Dig)), OnExit);
        }

        [TearDown]
        public void TearDown()
        {
            if (!LevelSession.IsActive)
                return;
            if (!LevelSession.HasReported)
                LevelSession.Report(new LevelResult());
            LevelSession.Exit();
        }

        void OnExit(LevelResult result, ScoreBreakdown score)
        {
            _exitResult = result;
            _exitScore = score;
            _exitCount++;
        }

        [Test]
        public void Report_FillsLevelAndArtifact_AndReturnsScore()
        {
            var score = LevelSession.Report(new LevelResult { Cleared = true, RemainingTime = 60f });

            Assert.AreEqual(1500, score.Total);
            Assert.AreEqual(LevelId.Dig, LevelSession.Result.LevelId);
            Assert.AreEqual(ArtifactId.GoldFoil, LevelSession.Result.Artifact);
        }

        [Test]
        public void Report_ClampsInvalidValues()
        {
            LevelSession.Report(new LevelResult { Cleared = true, RemainingTime = 999f, Mistakes = -3 });

            Assert.AreEqual(60f, LevelSession.Result.RemainingTime);
            Assert.AreEqual(0, LevelSession.Result.Mistakes);
        }

        [Test]
        public void Report_Twice_KeepsFirstResult()
        {
            LevelSession.Report(new LevelResult { Cleared = true, RemainingTime = 60f });
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("已經回報過結果"));
            var second = LevelSession.Report(new LevelResult { Cleared = false });

            Assert.AreEqual(1500, second.Total);
            Assert.IsTrue(LevelSession.Result.Cleared);
        }

        [Test]
        public void Exit_InvokesCallbackOnce_AndEndsSession()
        {
            LevelSession.Report(new LevelResult { Cleared = true, RemainingTime = 30f, Mistakes = 1 });
            LevelSession.Exit();

            Assert.AreEqual(1, _exitCount);
            Assert.AreEqual(ArtifactId.GoldFoil, _exitResult.Artifact);
            Assert.AreEqual(1000 + 250 - 50, _exitScore.Total);
            Assert.IsFalse(LevelSession.IsActive);
        }

        [Test]
        public void Exit_BeforeReport_IsRejected()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("要先呼叫 Report"));
            LevelSession.Exit();

            Assert.AreEqual(0, _exitCount);
            Assert.IsTrue(LevelSession.IsActive);
        }
    }
}
