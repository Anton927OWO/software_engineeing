using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TrackingNIU.Tests
{
    public class LevelDrawTests
    {
        [Test]
        public void MidtermPools_AreIndoor4_Outdoor2()
        {
            CollectionAssert.AreEquivalent(
                new[] { LevelId.Dig, LevelId.Timeline, LevelId.Train, LevelId.Bookshelf },
                LevelDraw.GetPool(LevelPool.Indoor, includeFinalsLevels: false));
            CollectionAssert.AreEquivalent(
                new[] { LevelId.Deer, LevelId.Strata },
                LevelDraw.GetPool(LevelPool.Outdoor, includeFinalsLevels: false));
        }

        [Test]
        public void FinalsIndoorPool_AddsCollage()
        {
            var pool = LevelDraw.GetPool(LevelPool.Indoor, includeFinalsLevels: true);
            Assert.AreEqual(5, pool.Count);
            CollectionAssert.Contains(pool, LevelId.Collage);
        }

        [Test]
        public void Draw_ReturnsDistinctLevelsFromPool()
        {
            var pool = LevelDraw.GetPool(LevelPool.Indoor, false);
            var random = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                var drawn = LevelDraw.Draw(pool, 2, random);
                Assert.AreEqual(2, drawn.Count);
                Assert.AreNotEqual(drawn[0], drawn[1]);
                CollectionAssert.IsSubsetOf(drawn, pool);
            }
        }

        [Test]
        public void Draw_CoversEveryPair()
        {
            // 4 抽 2 共 6 種組合，抽很多次後每一種都應該出現。
            var pool = LevelDraw.GetPool(LevelPool.Indoor, false);
            var random = new Random(7);
            var combinations = new HashSet<string>();
            for (int i = 0; i < 500; i++)
                combinations.Add(string.Join(",", LevelDraw.Draw(pool, 2, random).OrderBy(id => id)));

            Assert.AreEqual(6, combinations.Count);
        }

        [Test]
        public void Draw_MoreThanPool_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelDraw.Draw(new[] { LevelId.Deer, LevelId.Strata }, 3, new Random(1)));
        }
    }

    public class GameRunTests
    {
        static void Play(GameRun run, LevelId id, bool cleared = true)
        {
            var context = new LevelContext(LevelCatalog.Get(id), run.CollectedArtifacts);
            LevelSession.Begin(context, null);
            var score = LevelSession.Report(new LevelResult
            {
                Cleared = cleared,
                RemainingTime = context.TimeLimit,
                MatchedPairs = ScoreCalculator.FinalePairs,
            });
            var result = LevelSession.Result;
            LevelSession.Exit();
            run.Complete(result, score);
        }

        [Test]
        public void NewRun_Draws2Indoor_1Outdoor_StartsAtPrologue()
        {
            var run = new GameRun(false, new Random(3));

            Assert.AreEqual(2, run.IndoorLevels.Count);
            Assert.AreEqual(1, run.OutdoorLevels.Count);
            Assert.AreEqual(RunPhase.Prologue, run.Phase);
            CollectionAssert.AreEqual(new[] { LevelId.Prologue }, run.GetAvailableLevels());
        }

        [Test]
        public void SameSeed_SameDraw_DifferentRunsVary()
        {
            var a = new GameRun(false, new Random(42));
            var b = new GameRun(false, new Random(42));
            CollectionAssert.AreEqual(a.IndoorLevels, b.IndoorLevels);
            CollectionAssert.AreEqual(a.OutdoorLevels, b.OutdoorLevels);

            // 同一個亂數來源連續開局（重新遊玩），抽關結果不會每次都一樣。
            var random = new Random(5);
            var draws = new HashSet<string>();
            for (int i = 0; i < 20; i++)
            {
                var run = new GameRun(false, random);
                draws.Add(string.Join(",", run.IndoorLevels.Concat(run.OutdoorLevels)));
            }
            Assert.Greater(draws.Count, 1);
        }

        [Test]
        public void IndoorLevels_AvailableAfterPrologue_InAnyOrder()
        {
            var run = new GameRun(false, new Random(3));
            Play(run, LevelId.Prologue);

            Assert.AreEqual(RunPhase.Indoor, run.Phase);
            CollectionAssert.AreEquivalent(run.IndoorLevels, run.GetAvailableLevels());
            Assert.AreEqual(LevelStatus.Locked, run.GetStatus(run.OutdoorLevels[0]));
            Assert.AreEqual(LevelStatus.Completed, run.GetStatus(LevelId.Prologue));
        }

        [Test]
        public void UndrawnLevel_IsNotInRun_AndCannotBeCompleted()
        {
            var run = new GameRun(false, new Random(3));
            Play(run, LevelId.Prologue);
            var undrawn = LevelDraw.GetPool(LevelPool.Indoor, false).First(id => !run.IndoorLevels.Contains(id));

            Assert.AreEqual(LevelStatus.NotInRun, run.GetStatus(undrawn));
            Assert.AreEqual(LevelStatus.NotInRun, run.GetStatus(LevelId.Collage));
            Assert.Throws<InvalidOperationException>(() => Play(run, undrawn));
        }

        [Test]
        public void FullRun_ReachesFinished_With3Artifacts_AndMaxScore()
        {
            var run = new GameRun(false, new Random(3));
            Play(run, LevelId.Prologue);
            Play(run, run.IndoorLevels[1]);
            Assert.AreEqual(RunPhase.Indoor, run.Phase);
            Play(run, run.IndoorLevels[0]);
            Assert.AreEqual(RunPhase.Outdoor, run.Phase);
            CollectionAssert.AreEqual(run.OutdoorLevels, run.GetAvailableLevels());
            Play(run, run.OutdoorLevels[0]);
            Assert.AreEqual(RunPhase.Finale, run.Phase);
            CollectionAssert.AreEqual(new[] { LevelId.Finale }, run.GetAvailableLevels());
            Play(run, LevelId.Finale);

            Assert.AreEqual(RunPhase.Finished, run.Phase);
            Assert.IsEmpty(run.GetAvailableLevels());
            Assert.AreEqual(3, run.CollectedArtifacts.Count);
            Assert.AreEqual(3, run.CollectedArtifacts.Distinct().Count());
            Assert.AreEqual(5, run.Records.Count);
            Assert.AreEqual(ScoreCalculator.MaxTotal, run.TotalScore);
            Assert.AreEqual("宜大文化守護者", run.Title);
        }

        [Test]
        public void FailedLevel_StillAdvances_AndGivesArtifact()
        {
            var run = new GameRun(false, new Random(3));
            Play(run, LevelId.Prologue);
            var level = run.IndoorLevels[0];
            Play(run, level, cleared: false);

            Assert.AreEqual(LevelStatus.Completed, run.GetStatus(level));
            CollectionAssert.Contains(run.CollectedArtifacts, LevelCatalog.Get(level).Artifact);
            Assert.AreEqual(0, run.FindRecord(level).Score.Total);
        }

        [Test]
        public void CompletedLevel_CannotBePlayedAgain()
        {
            var run = new GameRun(false, new Random(3));
            Play(run, LevelId.Prologue);

            Assert.IsFalse(run.CanEnter(LevelId.Prologue));
            Assert.Throws<InvalidOperationException>(() => Play(run, LevelId.Prologue));
        }

        [Test]
        public void CurrentMap_FollowsPhase_LibraryCampusLibrary()
        {
            var run = new GameRun(false, new Random(3));
            Assert.IsNull(run.CurrentMap);

            Play(run, LevelId.Prologue);
            Assert.AreEqual(MapKind.Library, run.CurrentMap);
            foreach (var level in run.IndoorLevels)
                Play(run, level);
            Assert.AreEqual(MapKind.Campus, run.CurrentMap);
            Play(run, run.OutdoorLevels[0]);
            Assert.AreEqual(MapKind.Library, run.CurrentMap);
            Play(run, LevelId.Finale);
            Assert.IsNull(run.CurrentMap);
        }

        [Test]
        public void GetMap_OutdoorLevelsOnCampus()
        {
            foreach (var level in LevelCatalog.All)
            {
                var expected = level.Pool == LevelPool.Outdoor ? MapKind.Campus : MapKind.Library;
                Assert.AreEqual(expected, GameRun.GetMap(level.Id), level.Id.ToString());
            }
        }
    }
}
