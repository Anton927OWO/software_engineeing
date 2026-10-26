namespace TrackingNIU
{
    /// <summary>所有關卡。Scene 名稱一律是 L_ 加上這裡的名稱，例如 L_Dig。</summary>
    public enum LevelId
    {
        Prologue,   // 序章：知識之林尋書
        Dig,        // 考古小隊
        Timeline,   // 百年軌跡
        Train,      // 知識列車
        Bookshelf,  // 書海尋寶
        Collage,    // 黃春明撕畫工作室（期末擴充）
        Deer,       // 草澤擋鹿
        Strata,     // 地層解碼
        Finale,     // 終章：文物記憶翻牌
    }

    /// <summary>關卡屬於哪個關卡池，GameManager 依此抽關。</summary>
    public enum LevelPool
    {
        Prologue,
        Indoor,
        Outdoor,
        Finale,
    }

    /// <summary>六件出土文物，也是終章翻牌的六組牌。</summary>
    public enum ArtifactId
    {
        None,
        GoldenCarp,   // 金鯉魚
        GoldFoil,     // 金箔片
        PotteryJar,   // 幾何印紋陶罐
        WoodenSpade,  // 木鏟
        WoodenDisc,   // 木質圓形雕板
        AgateBead,    // 瑪瑙珠
    }
}
