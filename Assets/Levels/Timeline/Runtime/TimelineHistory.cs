using System;
using System.Collections.Generic;

namespace XunJiYiDa.Levels.Timeline
{
    /// <summary>Selected milestones, not a complete list of school names. Sources checked 2026-10-29.</summary>
    public static class TimelineHistory
    {
        public const string SchoolHistoryUrl = "https://www.niu.edu.tw/p/412-1000-1049.php";
        public const string MemoryUrl = "https://lic.niu.edu.tw/p/404-1010-63587.php";
        public const string CampusUrl = "https://www.niu.edu.tw/p/406-1000-56719%2Cr43.php";
        public const string ForumUrl = "https://lic.niu.edu.tw/p/412-1010-3425.php";
        public const string ScopeNote = "五個主要節點；期間另有 1967、1969 年職校更名，並非完整校名史。";

        public static IReadOnlyList<TimelineEra> Stages { get; } = Array.AsReadOnly(new[]
        {
            new TimelineEra(1926, "農林學校", "臺北州立宜蘭農林學校",
                "創立臺北州立宜蘭農林學校，開啟農林教育。",
                "從農業與林業教育的起點想想。",
                "農林學校的設立回應了地方發展需求。", CampusUrl),
            new TimelineEra(1946, "農業職校", "臺灣省立宜蘭農業職業學校",
                "更名為臺灣省立宜蘭農業職業學校。",
                "這時仍是職業教育，還不是專科。",
                "宜大記憶庫保存校園地景、生活物件與師生故事。", MemoryUrl),
            new TimelineEra(1988, "農工專科", "國立宜蘭農工專科學校",
                "改制為國立宜蘭農工專科學校，進入專科階段。",
                "留意農工並列，以及專科的學制。",
                "從職校、專科到大學的歷程，也見證臺灣教育制度的演變。", CampusUrl),
            new TimelineEra(1998, "技術學院", "國立宜蘭技術學院",
                "升格為國立宜蘭技術學院，從專科走向學院。",
                "學院承接專科，再往大學階段前進。",
                "官方百年校史專題將校史連結到農業、產業與教育的發展。", ForumUrl),
            new TimelineEra(2003, "宜蘭大學", "國立宜蘭大學",
                "更名為國立宜蘭大學，邁入大學階段。",
                "找找從技術學院走向大學的階段。",
                "宜大記憶庫結合噶瑪蘭文化廊道、萬斌廳考古展示，連結在地記憶。", CampusUrl)
        });
    }

    public sealed class TimelineEra
    {
        public int Year { get; }
        public string ShortName { get; }
        public string SchoolName { get; }
        public string Change { get; }
        public string Clue { get; }
        public string Connection { get; }
        public string SourceUrl => TimelineHistory.SchoolHistoryUrl;
        public string ConnectionSourceUrl { get; }
        public string Feedback => $"{Year}｜{Change}";
        public string Summary => $"{SchoolName}\n{Change}\n\n文化連結（非當年事件）\n{Connection}";

        public TimelineEra(int year, string shortName, string schoolName, string change,
            string clue, string connection, string connectionSourceUrl)
        {
            Year = year;
            ShortName = shortName;
            SchoolName = schoolName;
            Change = change;
            Clue = clue;
            Connection = connection;
            ConnectionSourceUrl = connectionSourceUrl;
        }
    }
}
