using System.Linq;
using NUnit.Framework;

namespace XunJiYiDa.Levels.Finale.Tests
{
    public sealed class FinaleMemoryBoardTests
    {
        [TestCase(1)] [TestCase(27)] [TestCase(802)]
        public void ShuffledDeckContainsTwoOfEachOfSixArtifacts(int seed)
        {
            var board = new FinaleMemoryBoard(seed);
            var values = Enumerable.Range(0, 12).Select(board.ArtifactAt).ToArray();
            Assert.That(values.OrderBy(x => x), Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }));
            Assert.That(Enumerable.Range(0, 12).All(i => !board.IsFaceUp(i) && !board.IsMatched(i)), Is.True);
        }

        [Test]
        public void DifferentSeedsDoNotAlwaysReuseTheSameLayout()
        {
            var layouts = Enumerable.Range(0, 8).Select(seed =>
            {
                var board = new FinaleMemoryBoard(seed);
                return string.Join(",", Enumerable.Range(0, 12).Select(board.ArtifactAt));
            });
            Assert.That(layouts.Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void DuplicateInvalidAndThirdSelectionsCannotChangeAPendingPair()
        {
            var board = new FinaleMemoryBoard(123);
            Assert.That(board.TryReveal(-1), Is.False);
            Assert.That(board.TryReveal(12), Is.False);
            Assert.That(board.TryReveal(0), Is.True);
            Assert.That(board.TryReveal(0), Is.False);
            Assert.That(board.TryReveal(1), Is.True);
            Assert.That(board.TryReveal(2), Is.False);
            Assert.That(board.IsFaceUp(2), Is.False);
            Assert.That(board.PairReady, Is.True);
        }

        [Test]
        public void WrongPairTurnsBackAndCanBeSelectedAgain()
        {
            var board = new FinaleMemoryBoard(10);
            int other = Enumerable.Range(1, 11).First(i => board.ArtifactAt(i) != board.ArtifactAt(0));
            board.TryReveal(0); board.TryReveal(other);
            Assert.That(board.PairMatches, Is.False);
            Assert.That(board.ResolvePair(), Is.False);
            Assert.That(board.IsFaceUp(0), Is.False);
            Assert.That(board.IsFaceUp(other), Is.False);
            Assert.That(board.MatchedPairs, Is.Zero);
            Assert.That(board.TryReveal(0), Is.True);
        }

        [Test]
        public void SixCorrectPairsRemainFaceUpAndFinishExactlyOnce()
        {
            var board = new FinaleMemoryBoard(42);
            for (int artifact = 0; artifact < 6; artifact++)
            {
                var pair = Enumerable.Range(0, 12).Where(i => board.ArtifactAt(i) == artifact).ToArray();
                Assert.That(pair.Length, Is.EqualTo(2));
                Assert.That(board.TryReveal(pair[0]), Is.True);
                Assert.That(board.TryReveal(pair[1]), Is.True);
                Assert.That(board.PairMatches, Is.True);
                Assert.That(board.ResolvePair(), Is.True);
                Assert.That(pair.All(i => board.IsFaceUp(i) && board.IsMatched(i)), Is.True);
                Assert.That(board.TryReveal(pair[0]), Is.False);
                Assert.That(board.IsComplete, Is.EqualTo(artifact == 5));
            }
            Assert.That(board.MatchedPairs, Is.EqualTo(6));
            Assert.That(board.ResolvePair(), Is.False);
        }

        [Test]
        public void ResolvingASingleCardDoesNotDiscardIt()
        {
            var board = new FinaleMemoryBoard(5);
            Assert.That(board.TryReveal(0), Is.True);
            Assert.That(board.ResolvePair(), Is.False);
            Assert.That(board.IsFaceUp(0), Is.True);
            Assert.That(board.PairReady, Is.False);
        }
    }
}
