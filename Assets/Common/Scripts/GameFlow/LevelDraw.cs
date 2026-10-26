using System;
using System.Collections.Generic;

namespace TrackingNIU
{
    /// <summary>從關卡池中隨機抽出不重複的關卡。</summary>
    public static class LevelDraw
    {
        /// <summary>回傳 GameManager 這一局要用的關卡池。</summary>
        public static List<LevelId> GetPool(LevelPool pool, bool includeFinalsLevels)
        {
            var levels = new List<LevelId>();
            foreach (var level in LevelCatalog.All)
            {
                if (level.Pool == pool && (level.AvailableInMidterm || includeFinalsLevels))
                    levels.Add(level.Id);
            }
            return levels;
        }

        /// <summary>用 Fisher–Yates 洗牌抽出 count 個關卡，順序也是隨機的。</summary>
        public static List<LevelId> Draw(IReadOnlyList<LevelId> pool, int count, Random random)
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (count < 0 || count > pool.Count)
                throw new ArgumentOutOfRangeException(nameof(count), count, $"關卡池只有 {pool.Count} 關，無法抽出 {count} 關");

            var shuffled = new List<LevelId>(pool);
            for (int i = 0; i < count; i++)
            {
                int j = random.Next(i, shuffled.Count);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled.GetRange(0, count);
        }
    }
}
