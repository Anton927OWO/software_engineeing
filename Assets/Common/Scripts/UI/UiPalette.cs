using UnityEngine;

namespace TrackingNIU
{
    /// <summary>《關卡畫面設計說明》的共用色票。程式裡要用顏色時從這裡取，不要另外寫色碼。</summary>
    public static class UiPalette
    {
        public static readonly Color Text = Hex(0x1F3A2E);          // 文字、深色 UI
        public static readonly Color Panel = Hex(0xFBF8F2);         // HUD、對話框、知識卡底色
        public static readonly Color Primary = Hex(0xB3322A);       // 主按鈕
        public static readonly Color Correct = Hex(0x2F8F5B);       // 正確、已完成
        public static readonly Color Error = Hex(0xD64545);         // 錯誤、未完成
        public static readonly Color Warning = Hex(0xE0A100);       // 倒數警告、選取框

        // 色票以外的輔助色：背景、停用與次要文字。
        public static readonly Color Background = Hex(0xEDE6D8);
        public static readonly Color Disabled = Hex(0xB5B0A6);
        public static readonly Color MutedText = Hex(0x7A776F);

        // 結算畫面（圖 4）的深色底與金色總分。
        public static readonly Color ResultsBackground = Hex(0x271E1C);
        public static readonly Color ResultsSlot = Hex(0x433835);
        public static readonly Color ResultsMutedText = Hex(0xBFB6A8);
        public static readonly Color ResultsGold = Hex(0xF5C518);

        public static Color GetZoneColor(LevelId levelId)
        {
            switch (levelId)
            {
                case LevelId.Prologue: return Hex(0x4E8B4A);                                    // 知識之林
                case LevelId.Dig:
                case LevelId.Timeline:
                case LevelId.Finale: return Hex(0xB3322A);                                      // 2F 宜大記憶庫
                case LevelId.Train:
                case LevelId.Collage: return Hex(0xC99A5B);                                     // 3F 學習共享空間
                case LevelId.Bookshelf: return Hex(0x3D6A8C);                                   // 5F 中文圖書區
                case LevelId.Deer: return Hex(0xA9481B);                                        // 噶瑪蘭文化廊道
                case LevelId.Strata: return Hex(0x6B4A2E);                                      // 萬斌廳
                default: return Text;
            }
        }

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF);
    }
}
