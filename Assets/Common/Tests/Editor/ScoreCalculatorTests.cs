using NUnit.Framework;

namespace TrackingNIU.Tests
{
    public class ScoreCalculatorTests
    {
        static LevelContext Context(LevelId id) => new LevelContext(LevelCatalog.Get(id));

        static ScoreBreakdown Score(LevelId id, bool cleared, float remaining, int mistakes, int pairs = 0) =>
            ScoreCalculator.Calculate(Context(id), new LevelResult
            {
                Cleared = cleared,
                RemainingTime = remaining,
                Mistakes = mistakes,
                MatchedPairs = pairs,
            });

        [Test]
        public void MaxTotal_MatchesPlan()
        {
            Assert.AreEqual(5600, ScoreCalculator.MaxTotal);
        }

        [Test]
        public void PerfectLevel_Scores1500()
        {
            var score = Score(LevelId.Bookshelf, cleared: true, remaining: 60f, mistakes: 0);
            Assert.AreEqual(1000, score.ClearBonus);
            Assert.AreEqual(500, score.TimeBonus);
            Assert.AreEqual(1500, score.Total);
        }

        [Test]
        public void TimeBonus_UsesRemainingRatio()
        {
            // 百年軌跡限時 45 秒，剩一半時間就是 250 分。
            Assert.AreEqual(250, Score(LevelId.Timeline, true, 22.5f, 0).TimeBonus);
            Assert.AreEqual(250, Score(LevelId.Dig, true, 30f, 0).TimeBonus);
        }

        [Test]
        public void MistakePenalty_UsesLevelValue()
        {
            Assert.AreEqual(100, Score(LevelId.Dig, true, 0f, 2).MistakePenalty);
            Assert.AreEqual(60, Score(LevelId.Train, true, 0f, 2).MistakePenalty);
            Assert.AreEqual(40, Score(LevelId.Strata, true, 0f, 2).MistakePenalty);
        }

        [Test]
        public void MistakePenalty_CappedAt500()
        {
            var score = Score(LevelId.Train, true, 0f, 20);
            Assert.AreEqual(500, score.MistakePenalty);
            Assert.AreEqual(500, score.Total);
        }

        [Test]
        public void FailedLevel_GetsNoClearOrTimeBonus_AndNeverNegative()
        {
            var score = Score(LevelId.Dig, cleared: false, remaining: 30f, mistakes: 3);
            Assert.AreEqual(0, score.ClearBonus);
            Assert.AreEqual(0, score.TimeBonus);
            Assert.AreEqual(150, score.MistakePenalty);
            Assert.AreEqual(0, score.Total);
        }

        [Test]
        public void Finale_ScoresPairsAndTime()
        {
            var score = Score(LevelId.Finale, cleared: true, remaining: 45f, mistakes: 2, pairs: 6);
            Assert.AreEqual(0, score.ClearBonus);
            Assert.AreEqual(600, score.MatchBonus);
            Assert.AreEqual(250, score.TimeBonus);
            Assert.AreEqual(40, score.MistakePenalty);
            Assert.AreEqual(810, score.Total);
        }

        [Test]
        public void Finale_Perfect_Scores1100()
        {
            Assert.AreEqual(1100, Score(LevelId.Finale, true, 90f, 0, pairs: 6).Total);
        }

        [Test]
        public void Finale_TimeOut_KeepsMatchedPairs()
        {
            var score = Score(LevelId.Finale, cleared: false, remaining: 0f, mistakes: 1, pairs: 4);
            Assert.AreEqual(400, score.MatchBonus);
            Assert.AreEqual(380, score.Total);
        }

        [Test]
        public void Prologue_IsNotScored()
        {
            var score = Score(LevelId.Prologue, true, 0f, 0);
            Assert.IsFalse(score.IsScored);
            Assert.AreEqual(0, score.Total);
        }

        [TestCase(5600, "宜大文化守護者")]
        [TestCase(4800, "宜大文化守護者")]
        [TestCase(4799, "噶瑪蘭文化探索達人")]
        [TestCase(4000, "噶瑪蘭文化探索達人")]
        [TestCase(3999, "蘭陽探索者")]
        [TestCase(3200, "蘭陽探索者")]
        [TestCase(3199, "宜大文化新旅人")]
        [TestCase(0, "宜大文化新旅人")]
        public void Title_MatchesScoreRange(int total, string expected)
        {
            Assert.AreEqual(expected, ScoreCalculator.GetTitle(total));
        }

        [Test]
        public void CalculateTotal_SumsLevels()
        {
            var scores = new[]
            {
                Score(LevelId.Dig, true, 60f, 0),
                Score(LevelId.Train, true, 60f, 0),
                Score(LevelId.Deer, true, 60f, 0),
                Score(LevelId.Finale, true, 90f, 0, pairs: 6),
            };
            Assert.AreEqual(ScoreCalculator.MaxTotal, ScoreCalculator.CalculateTotal(scores));
        }
    }
}
