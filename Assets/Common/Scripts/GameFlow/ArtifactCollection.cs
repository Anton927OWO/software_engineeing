using System.Collections.Generic;
using UnityEngine;

namespace TrackingNIU
{
    /// <summary>
    /// 結算畫面的文物圖鑑：記錄玩家曾經收集過哪些文物，跨局保存在 PlayerPrefs。
    /// 每件文物占一個位元，ArtifactId.GoldenCarp 是第 1 位，依此類推。
    /// </summary>
    public static class ArtifactCollection
    {
        public const string PrefsKey = "TrackingNIU.ArtifactCollection";

        /// <summary>圖鑑上的全部文物，依 ArtifactId 的順序排列。</summary>
        public static readonly ArtifactId[] All =
        {
            ArtifactId.GoldenCarp,
            ArtifactId.GoldFoil,
            ArtifactId.PotteryJar,
            ArtifactId.WoodenSpade,
            ArtifactId.WoodenDisc,
            ArtifactId.AgateBead,
        };

        public static bool Has(ArtifactId artifact) => artifact != ArtifactId.None && (Load() & Bit(artifact)) != 0;

        public static int Count
        {
            get
            {
                int count = 0;
                foreach (var artifact in All)
                {
                    if (Has(artifact))
                        count++;
                }
                return count;
            }
        }

        public static void Add(IEnumerable<ArtifactId> artifacts)
        {
            int mask = Load();
            foreach (var artifact in artifacts)
            {
                if (artifact != ArtifactId.None)
                    mask |= Bit(artifact);
            }
            PlayerPrefs.SetInt(PrefsKey, mask);
            PlayerPrefs.Save();
        }

        /// <summary>清除圖鑑紀錄，流程測試面板使用。</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        static int Load() => PlayerPrefs.GetInt(PrefsKey, 0);

        static int Bit(ArtifactId artifact) => 1 << (int)artifact;
    }
}
