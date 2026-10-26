using System;
using System.Collections.Generic;

namespace TrackingNIU
{
    /// <summary>一個關卡的固定設定。數值來自《期中專案規畫書》的計時與成功／失敗條件。</summary>
    public sealed class LevelDefinition
    {
        public LevelId Id { get; }
        public string DisplayName { get; }
        public string Location { get; }
        public LevelPool Pool { get; }
        /// <summary>限時秒數；0 代表不限時。</summary>
        public float TimeLimit { get; }
        /// <summary>每次失誤扣的分數。</summary>
        public int MistakePenalty { get; }
        /// <summary>通關後獲得的文物。</summary>
        public ArtifactId Artifact { get; }
        /// <summary>期中版本是否放進抽關。</summary>
        public bool AvailableInMidterm { get; }

        public string SceneName => "L_" + Id;
        public bool IsScored => Pool != LevelPool.Prologue;
        public bool IsFinale => Pool == LevelPool.Finale;

        public LevelDefinition(LevelId id, string displayName, string location, LevelPool pool,
            float timeLimit, int mistakePenalty, ArtifactId artifact, bool availableInMidterm = true)
        {
            Id = id;
            DisplayName = displayName;
            Location = location;
            Pool = pool;
            TimeLimit = timeLimit;
            MistakePenalty = mistakePenalty;
            Artifact = artifact;
            AvailableInMidterm = availableInMidterm;
        }
    }

    /// <summary>全部關卡的設定表。只有游士霆修改這個檔案。</summary>
    public static class LevelCatalog
    {
        // 文物分配為開工會議提案，確認後在這裡修改即可，各關程式不用動。
        static readonly LevelDefinition[] s_levels =
        {
            new LevelDefinition(LevelId.Prologue,  "序章：知識之林尋書", "圖資館前廣場「知識之林」",   LevelPool.Prologue, 0f,  0,  ArtifactId.None),
            new LevelDefinition(LevelId.Dig,       "考古小隊",           "2F 宜大記憶庫・展區二",       LevelPool.Indoor,   60f, 50, ArtifactId.GoldFoil),
            new LevelDefinition(LevelId.Timeline,  "百年軌跡",           "2F 宜大記憶庫・展區四",       LevelPool.Indoor,   45f, 20, ArtifactId.WoodenDisc),
            new LevelDefinition(LevelId.Train,     "知識列車",           "3F 學習共享空間・車廂區",     LevelPool.Indoor,   60f, 30, ArtifactId.AgateBead),
            new LevelDefinition(LevelId.Bookshelf, "書海尋寶",           "5F 中文圖書區",               LevelPool.Indoor,   60f, 20, ArtifactId.WoodenSpade),
            // 期末擴充：文物待實地確認後決定。
            new LevelDefinition(LevelId.Collage,   "黃春明撕畫工作室",   "3F 黃春明體驗行動館",         LevelPool.Indoor,   60f, 20, ArtifactId.None, availableInMidterm: false),
            // 規畫書沒有寫擋鹿的失誤扣分，提案：每隻鹿離開地圖算一次失誤。
            new LevelDefinition(LevelId.Deer,      "草澤擋鹿",           "噶瑪蘭文化廊道",              LevelPool.Outdoor,  60f, 20, ArtifactId.GoldenCarp),
            new LevelDefinition(LevelId.Strata,    "地層解碼",           "行政大樓 B1 萬斌廳",          LevelPool.Outdoor,  60f, 20, ArtifactId.PotteryJar),
            new LevelDefinition(LevelId.Finale,    "終章：文物記憶翻牌", "2F 宜大記憶庫・設校紀念碑",   LevelPool.Finale,   90f, 20, ArtifactId.None),
        };

        public static IReadOnlyList<LevelDefinition> All => s_levels;

        public static LevelDefinition Get(LevelId id)
        {
            foreach (var level in s_levels)
            {
                if (level.Id == id)
                    return level;
            }
            throw new ArgumentOutOfRangeException(nameof(id), id, "LevelCatalog 中沒有這個關卡");
        }

        public static string GetArtifactName(ArtifactId artifact)
        {
            switch (artifact)
            {
                case ArtifactId.GoldenCarp: return "金鯉魚";
                case ArtifactId.GoldFoil: return "金箔片";
                case ArtifactId.PotteryJar: return "幾何印紋陶罐";
                case ArtifactId.WoodenSpade: return "木鏟";
                case ArtifactId.WoodenDisc: return "木質圓形雕板";
                case ArtifactId.AgateBead: return "瑪瑙珠";
                default: return "";
            }
        }
    }
}
