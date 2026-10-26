using System;
using System.Collections.Generic;

namespace TrackingNIU
{
    /// <summary>一關的得分明細，知識卡的「得分明細」直接顯示這些數字。</summary>
    public readonly struct ScoreBreakdown
    {
        /// <summary>false 代表本關不計分（序章），知識卡不要顯示得分明細。</summary>
        public readonly bool IsScored;
        /// <summary>通關基礎分：一般關卡 1000，終章與失敗為 0。</summary>
        public readonly int ClearBonus;
        /// <summary>終章配對分：每組 100。</summary>
        public readonly int MatchBonus;
        /// <summary>時間獎勵：剩餘時間 ÷ 限時 × 500，只在通關時給。</summary>
        public readonly int TimeBonus;
        /// <summary>失誤扣分，為正數，顯示時加上負號。</summary>
        public readonly int MistakePenalty;
        /// <summary>本關總分，不會低於 0。</summary>
        public readonly int Total;

        public ScoreBreakdown(bool isScored, int clearBonus, int matchBonus, int timeBonus, int mistakePenalty)
        {
            IsScored = isScored;
            ClearBonus = clearBonus;
            MatchBonus = matchBonus;
            TimeBonus = timeBonus;
            MistakePenalty = mistakePenalty;
            Total = Math.Max(0, clearBonus + matchBonus + timeBonus - mistakePenalty);
        }

        public static ScoreBreakdown NotScored => new ScoreBreakdown(false, 0, 0, 0, 0);

        public override string ToString()
        {
            if (!IsScored)
                return "不計分";
            return $"基礎 {ClearBonus + MatchBonus}＋時間 {TimeBonus}－失誤 {MistakePenalty}＝{Total}";
        }
    }

    /// <summary>依《期中專案規畫書》計分規則計算分數與稱號。關卡內不要自己算分。</summary>
    public static class ScoreCalculator
    {
        public const int ClearBonus = 1000;
        public const int MaxTimeBonus = 500;
        public const int MaxMistakePenalty = 500;
        public const int PairBonus = 100;
        public const int FinalePairs = 6;
        /// <summary>單局滿分：3 × 1500 + 1100。</summary>
        public const int MaxTotal = 3 * (ClearBonus + MaxTimeBonus) + FinalePairs * PairBonus + MaxTimeBonus;

        static readonly (int minScore, string title)[] s_titles =
        {
            (4800, "宜大文化守護者"),
            (4000, "噶瑪蘭文化探索達人"),
            (3200, "蘭陽探索者"),
            (0, "宜大文化新旅人"),
        };

        public static ScoreBreakdown Calculate(LevelContext context, LevelResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!context.IsScored)
                return ScoreBreakdown.NotScored;

            int clearBonus = !context.IsFinale && result.Cleared ? ClearBonus : 0;
            int matchBonus = context.IsFinale ? Clamp(result.MatchedPairs, 0, FinalePairs) * PairBonus : 0;
            int timeBonus = result.Cleared ? CalculateTimeBonus(result.RemainingTime, context.TimeLimit) : 0;
            int penalty = Math.Min(Math.Max(0, result.Mistakes) * context.MistakePenalty, MaxMistakePenalty);

            return new ScoreBreakdown(true, clearBonus, matchBonus, timeBonus, penalty);
        }

        public static int CalculateTimeBonus(float remainingTime, float timeLimit)
        {
            if (timeLimit <= 0f)
                return 0;
            double ratio = Math.Min(Math.Max(remainingTime, 0f), timeLimit) / timeLimit;
            return (int)Math.Round(ratio * MaxTimeBonus, MidpointRounding.AwayFromZero);
        }

        public static int CalculateTotal(IEnumerable<ScoreBreakdown> levelScores)
        {
            int total = 0;
            foreach (var score in levelScores)
                total += score.Total;
            return total;
        }

        public static string GetTitle(int totalScore)
        {
            foreach (var (minScore, title) in s_titles)
            {
                if (totalScore >= minScore)
                    return title;
            }
            return s_titles[s_titles.Length - 1].title;
        }

        static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
    }
}
