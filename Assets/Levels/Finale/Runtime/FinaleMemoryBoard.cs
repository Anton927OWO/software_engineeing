using System;

namespace XunJiYiDa.Levels.Finale
{
    /// <summary>Pair-selection contract; presentation and time belong to the scene controller.</summary>
    public sealed class FinaleMemoryBoard
    {
        public const int CardCount = 12;
        public const int PairCount = 6;
        private readonly int[] artifacts = new int[CardCount];
        private readonly bool[] faceUp = new bool[CardCount];
        private readonly bool[] matched = new bool[CardCount];
        private int first = -1;
        private int second = -1;

        public FinaleMemoryBoard(int seed)
        {
            for (int i = 0; i < CardCount; i++) artifacts[i] = i / 2;
            var random = new Random(seed);
            for (int i = CardCount - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int value = artifacts[i]; artifacts[i] = artifacts[j]; artifacts[j] = value;
            }
        }

        public int ArtifactAt(int index) => artifacts[index];
        public bool IsFaceUp(int index) => faceUp[index];
        public bool IsMatched(int index) => matched[index];
        public bool PairReady => second >= 0;
        public bool PairMatches => PairReady && artifacts[first] == artifacts[second];
        public int MatchedPairs { get; private set; }
        public bool IsComplete => MatchedPairs == PairCount;

        public bool TryReveal(int index)
        {
            if (index < 0 || index >= CardCount || PairReady || faceUp[index] || IsComplete) return false;
            faceUp[index] = true;
            if (first < 0) first = index;
            else second = index;
            return true;
        }

        public bool ResolvePair()
        {
            if (!PairReady) return false;
            bool correct = PairMatches;
            if (correct)
            {
                matched[first] = matched[second] = true;
                MatchedPairs++;
            }
            else faceUp[first] = faceUp[second] = false;
            first = second = -1;
            return correct;
        }

        public void CancelPendingSelection()
        {
            for (int i = 0; i < CardCount; i++) faceUp[i] = matched[i];
            first = second = -1;
        }
    }
}
