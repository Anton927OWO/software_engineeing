using System;
using System.Collections.Generic;

namespace TrackingNIU
{
    /// <summary>一局遊戲進行到哪個階段。</summary>
    public enum RunPhase
    {
        Prologue,  // 序章
        Indoor,    // 第一章：圖資館樓層地圖，館內關卡
        Outdoor,   // 第二章：無圍牆博物館地圖，戶外關卡
        Finale,    // 回到 2F 宜大記憶庫進行終章
        Finished,  // 結算
    }

    /// <summary>地圖上的關卡狀態。</summary>
    public enum LevelStatus
    {
        NotInRun,   // 這局沒有抽到
        Locked,     // 抽到了，但還沒到這個階段
        Available,  // 現在可以進入（地圖上的紅色驚嘆號）
        Completed,  // 已完成（地圖上的綠色勾）
    }

    /// <summary>兩張地圖：圖資館樓層地圖與無圍牆博物館（校園）地圖。</summary>
    public enum MapKind
    {
        Library,
        Campus,
    }

    /// <summary>完成一關後留下的紀錄，結算畫面依此顯示各關分數。</summary>
    public sealed class LevelRecord
    {
        public LevelResult Result { get; }
        public ScoreBreakdown Score { get; }
        public LevelId LevelId => Result.LevelId;

        public LevelRecord(LevelResult result, ScoreBreakdown score)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Score = score;
        }
    }

    /// <summary>
    /// 一局遊戲的狀態：抽到哪些關卡、目前階段、各關紀錄與收集到的文物。
    /// 不依賴 Scene，所以可以直接寫單元測試；Scene 切換由 GameManager 負責。
    /// </summary>
    public sealed class GameRun
    {
        public const int IndoorDrawCount = 2;
        public const int OutdoorDrawCount = 1;

        readonly List<LevelRecord> _records = new List<LevelRecord>();
        readonly List<ArtifactId> _artifacts = new List<ArtifactId>();

        public IReadOnlyList<LevelId> IndoorLevels { get; }
        public IReadOnlyList<LevelId> OutdoorLevels { get; }
        public RunPhase Phase { get; private set; } = RunPhase.Prologue;
        public IReadOnlyList<LevelRecord> Records => _records;
        public IReadOnlyList<ArtifactId> CollectedArtifacts => _artifacts;

        public int TotalScore
        {
            get
            {
                var scores = new List<ScoreBreakdown>(_records.Count);
                foreach (var record in _records)
                    scores.Add(record.Score);
                return ScoreCalculator.CalculateTotal(scores);
            }
        }

        public string Title => ScoreCalculator.GetTitle(TotalScore);

        /// <summary>目前階段的關卡在哪張地圖上；序章與結算時為 null。</summary>
        public MapKind? CurrentMap
        {
            get
            {
                switch (Phase)
                {
                    case RunPhase.Indoor:
                    case RunPhase.Finale:
                        return MapKind.Library;
                    case RunPhase.Outdoor:
                        return MapKind.Campus;
                    default:
                        return null;
                }
            }
        }

        /// <summary>關卡所在的地圖：戶外關卡在校園地圖，其他都在圖資館。</summary>
        public static MapKind GetMap(LevelId levelId) =>
            LevelCatalog.Get(levelId).Pool == LevelPool.Outdoor ? MapKind.Campus : MapKind.Library;

        /// <param name="includeFinalsLevels">期末加入黃春明關後設為 true，館內改為 5 抽 2。</param>
        public GameRun(bool includeFinalsLevels, Random random)
        {
            IndoorLevels = LevelDraw.Draw(LevelDraw.GetPool(LevelPool.Indoor, includeFinalsLevels), IndoorDrawCount, random);
            OutdoorLevels = LevelDraw.Draw(LevelDraw.GetPool(LevelPool.Outdoor, includeFinalsLevels), OutdoorDrawCount, random);
        }

        public LevelStatus GetStatus(LevelId levelId)
        {
            if (FindRecord(levelId) != null)
                return LevelStatus.Completed;

            switch (LevelCatalog.Get(levelId).Pool)
            {
                case LevelPool.Prologue:
                    return Phase == RunPhase.Prologue ? LevelStatus.Available : LevelStatus.Locked;
                case LevelPool.Indoor:
                    if (!Contains(IndoorLevels, levelId))
                        return LevelStatus.NotInRun;
                    return Phase == RunPhase.Indoor ? LevelStatus.Available : LevelStatus.Locked;
                case LevelPool.Outdoor:
                    if (!Contains(OutdoorLevels, levelId))
                        return LevelStatus.NotInRun;
                    return Phase == RunPhase.Outdoor ? LevelStatus.Available : LevelStatus.Locked;
                case LevelPool.Finale:
                    return Phase == RunPhase.Finale ? LevelStatus.Available : LevelStatus.Locked;
                default:
                    return LevelStatus.NotInRun;
            }
        }

        public bool CanEnter(LevelId levelId) => GetStatus(levelId) == LevelStatus.Available;

        /// <summary>目前可以進入的關卡，地圖用來決定哪些標記可以點。</summary>
        public List<LevelId> GetAvailableLevels()
        {
            var levels = new List<LevelId>();
            foreach (var level in LevelCatalog.All)
            {
                if (CanEnter(level.Id))
                    levels.Add(level.Id);
            }
            return levels;
        }

        public LevelRecord FindRecord(LevelId levelId)
        {
            foreach (var record in _records)
            {
                if (record.LevelId == levelId)
                    return record;
            }
            return null;
        }

        /// <summary>記錄一關的結果並推進階段。失敗後選擇繼續也算完成，仍然獲得文物。</summary>
        public void Complete(LevelResult result, ScoreBreakdown score)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!CanEnter(result.LevelId))
                throw new InvalidOperationException($"{result.LevelId} 目前的狀態是 {GetStatus(result.LevelId)}，不能記錄結果");

            _records.Add(new LevelRecord(result, score));
            if (result.Artifact != ArtifactId.None)
                _artifacts.Add(result.Artifact);

            AdvancePhase();
        }

        void AdvancePhase()
        {
            switch (Phase)
            {
                case RunPhase.Prologue:
                    Phase = RunPhase.Indoor;
                    break;
                case RunPhase.Indoor when AllCompleted(IndoorLevels):
                    Phase = RunPhase.Outdoor;
                    break;
                case RunPhase.Outdoor when AllCompleted(OutdoorLevels):
                    Phase = RunPhase.Finale;
                    break;
                case RunPhase.Finale:
                    Phase = RunPhase.Finished;
                    break;
            }
        }

        bool AllCompleted(IReadOnlyList<LevelId> levels)
        {
            foreach (var level in levels)
            {
                if (FindRecord(level) == null)
                    return false;
            }
            return true;
        }

        static bool Contains(IReadOnlyList<LevelId> levels, LevelId levelId)
        {
            foreach (var level in levels)
            {
                if (level == levelId)
                    return true;
            }
            return false;
        }
    }
}
