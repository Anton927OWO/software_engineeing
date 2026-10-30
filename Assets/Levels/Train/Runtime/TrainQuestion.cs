using System;
using System.Linq;

namespace XunJiYiDa.Levels.Train
{
    [Serializable]
    public sealed class TrainQuestion
    {
        public string id;
        public string station;
        public string StationName => string.IsNullOrWhiteSpace(station) ? "知識車站" : station;
        public string prompt;
        public string[] options = new string[3];
        public int correctIndex;
        public string explanation;
        public string sourceTitle;
        public string sourceUrl;
        public bool verified;
        public string verifiedOn;

        public bool IsPlayable => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(prompt) &&
            options != null && options.Length == 3 && options.All(o => !string.IsNullOrWhiteSpace(o)) &&
            options.Distinct(StringComparer.Ordinal).Count() == 3 && correctIndex >= 0 && correctIndex < 3;

        public string VerificationLabel => verified && Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
            uri.Scheme == "https" && !string.IsNullOrWhiteSpace(verifiedOn) ? "已查核 " + verifiedOn : "待驗證";

        public string KnowledgeText => $"主題：{StationName}\n正確答案：{options[correctIndex]}\n{explanation}\n\n來源：{sourceTitle}\n{sourceUrl}\n{VerificationLabel}";
    }
}
