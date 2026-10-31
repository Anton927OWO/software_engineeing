using NUnit.Framework;
using TrackingNIU;
using UnityEngine;

namespace XunJiYiDa.Levels.Junting.Tests
{
    public sealed class JuntingRulesTests
    {
        [Test]
        public void Shelf_RejectsWrongAndInvalidPlacements_AndCompletesOnce()
        {
            var shelf = new ShelfRound();
            Assert.IsFalse(shelf.Place(5, 1));
            Assert.AreEqual(0, shelf.Placed);
            Assert.IsFalse(shelf.Place(-1, 1) || shelf.Place(1, 6));
            Assert.IsTrue(shelf.Place(1, 1) && shelf.Place(3, 3) && shelf.Place(5, 5));
            Assert.AreEqual(3, shelf.Placed);
            Assert.IsFalse(shelf.Place(1, 1));
        }

        [Test]
        public void Shelf_RandomRoundsAreSortedWithThreeGaps_AndCanBeCompleted()
        {
            for (int seed = 0; seed < 30; seed++)
            for (int stage = 0; stage < 2; stage++)
            {
                var shelf = new ShelfRound(seed, stage);
                for (int i = 0; i + 1 < ShelfRound.Size; i++)
                    Assert.Less(Book.Compare(shelf.books[i], shelf.books[i + 1]), 0, "seed " + seed + " stage " + stage);
                var empty = System.Linq.Enumerable.ToArray(shelf.EmptySlots);
                Assert.AreEqual(ShelfRound.Empty, empty.Length);
                if (stage == 1)
                    Assert.IsTrue(System.Array.TrueForAll(shelf.books, b => b.classification == shelf.books[0].classification));
                foreach (int slot in empty) Assert.IsTrue(shelf.Place(slot, slot));
                Assert.AreEqual(ShelfRound.Empty, shelf.Placed);
            }
        }

        [Test]
        public void DeerChallenges_NeedTheirBarriers_AndRocksStayFixed()
        {
            int[][][] solutions =
            {
                new[] { new[] { 5, 2 }, new[] { 9, 3 } },
                new[] { new[] { 8, 1 }, new[] { 6, 2 }, new[] { 7, 3 } },
                new[] { new[] { 6, 2 }, new[] { 4, 3 }, new[] { 5, 4 } },
            };
            Assert.AreEqual(solutions.Length, DeerMap.Challenges.Length);
            for (int m = 0; m < solutions.Length; m++)
            {
                var map = DeerMap.Challenges[m];
                var empty = new DeerBoard(map) { released = true };
                for (int i = 0; i < 80; i++) empty.Step();
                Assert.AreEqual(0, empty.arrived, "map " + m + " must not be won without barriers");

                var board = new DeerBoard(map);
                int rock = map.rocks[0];
                Assert.IsFalse(board.Toggle(rock % DeerBoard.Width, rock / DeerBoard.Width), "rocks cannot be replaced");
                foreach (var cell in solutions[m]) Assert.IsTrue(board.Toggle(cell[0], cell[1]));
                board.released = true;
                for (int i = 0; i < 80; i++) board.Step();
                Assert.AreEqual(3, board.arrived, "map " + m);
                Assert.IsFalse(board.escaped);
            }
        }

        [Test]
        public void Book_ComparesClassificationThenAuthorThenYear()
        {
            Assert.Less(Book.Compare(new Book { classification = "25.4", author = "1234", year = 2024 }, new Book { classification = "50", author = "0001", year = 2010 }), 0);
            Assert.Less(Book.Compare(new Book { classification = "25.4", author = "1000", year = 2024 }, new Book { classification = "25.4", author = "2000", year = 2000 }), 0);
            Assert.Less(Book.Compare(new Book { classification = "25.4", author = "1000", year = 2020 }, new Book { classification = "25.4", author = "1000", year = 2024 }), 0);
        }

        [Test]
        public void DeerBoard_LimitsBarriers_AndProtectsSpawnAndTarget()
        {
            var board = new DeerBoard();
            for (int x = 1; x <= 5; x++) Assert.IsTrue(board.Toggle(x, 0));
            Assert.IsFalse(board.Toggle(6, 0));
            Assert.AreEqual(5, board.barriers.Count);
            Assert.IsTrue(board.Toggle(1, 0));
            Assert.AreEqual(4, board.barriers.Count);
            Assert.IsFalse(board.Toggle(0, 1) || board.Toggle(8, 4));
            Assert.IsFalse(board.Toggle(-1, 0) || board.Toggle(12, 0));
        }

        [Test]
        public void DeerBoard_KnownRouteWins_EmptyRouteLoses()
        {
            var board = new DeerBoard();
            board.Toggle(9, 1);
            board.Step();
            Assert.AreEqual(0, board.ticks, "Deer cannot move before release");
            board.released = true;
            Assert.IsFalse(board.Toggle(4, 1), "Placement locked after release");
            for (int i = 0; i < 40; i++) board.Step();
            Assert.AreEqual(3, board.arrived);
            Assert.IsFalse(board.escaped);

            board = new DeerBoard { released = true };
            for (int i = 0; i < 20; i++) board.Step();
            Assert.IsTrue(board.escaped);
            Assert.AreEqual(0, board.arrived);
        }

        [Test]
        public void RoundState_OnlyCountsDownWhilePlaying()
        {
            var round = new RoundState { stage = RoundState.Stage.Playing };
            round.Tick(1);
            round.paused = true; round.Tick(9);
            Assert.AreEqual(59, round.remaining);
            round.paused = false; round.stage = RoundState.Stage.Quiz; round.Tick(20);
            Assert.AreEqual(59, round.remaining);
            round.stage = RoundState.Stage.Playing; round.Tick(100);
            Assert.AreEqual(0, round.remaining);
            Assert.AreEqual(RoundState.Stage.Failed, round.stage);
        }

        [Test]
        public void TeamLevelIds_MapToLevelData()
        {
            var catalog = JsonUtility.FromJson<JuntingLevelCatalog>(Resources.Load<TextAsset>("Junting/levels").text);
            foreach (var id in new[] { LevelId.Bookshelf, LevelId.Deer })
            {
                var local = JuntingGame.ToLocalLevelId(id);
                var data = System.Array.Find(catalog.levels, l => l.id == local);
                Assert.IsNotNull(data, id.ToString());
                Assert.IsNotEmpty(data.questions);
            }
        }
    }
}
