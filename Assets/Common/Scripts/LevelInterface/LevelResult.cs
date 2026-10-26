namespace TrackingNIU
{
    /// <summary>
    /// 小遊戲結束時回傳的結果。關卡只填前四個欄位，分數由 ScoreCalculator 計算，
    /// 關卡編號與文物編號由 LevelSession 依 LevelContext 自動補上。
    /// </summary>
    public sealed class LevelResult
    {
        /// <summary>是否通關。</summary>
        public bool Cleared;
        /// <summary>剩餘秒數；不限時的關卡填 0。</summary>
        public float RemainingTime;
        /// <summary>失誤次數（放錯、答錯、損壞、配錯）。</summary>
        public int Mistakes;
        /// <summary>只有終章使用：配對成功的組數（0～6）。</summary>
        public int MatchedPairs;

        public LevelId LevelId { get; internal set; }
        /// <summary>獲得的文物。失敗時也會給，確保能湊齊三件進入終章。</summary>
        public ArtifactId Artifact { get; internal set; }

        public override string ToString()
        {
            var text = $"{LevelId}｜{(Cleared ? "通關" : "失敗")}｜剩餘 {RemainingTime:0.0} 秒｜失誤 {Mistakes} 次" +
                       $"｜文物 {LevelCatalog.GetArtifactName(Artifact)}";
            return LevelId == LevelId.Finale ? text + $"｜配對 {MatchedPairs} 組" : text;
        }
    }
}
