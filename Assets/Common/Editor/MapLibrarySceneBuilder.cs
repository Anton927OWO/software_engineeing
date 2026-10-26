using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 產生圖資館樓層地圖 Map_Library 場景，版面依《關卡畫面設計說明》圖 1：
    /// ① 樓層剖面（只有 2F、3F、5F 可點選）② 關卡標記 ③ 右側任務清單與 3 格文物欄 ④ 前往按鈕。
    /// 產生後可以直接在場景中調整位置與外觀；重新產生會覆蓋場景。
    /// </summary>
    public static class MapLibrarySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Map_Library.unity";

        sealed class FloorSpec
        {
            public string Floor;
            public string Area;
            public LevelId[] Levels;
        }

        // 由上往下排列。沒有關卡的樓層只顯示樓層號碼。
        static readonly FloorSpec[] s_floors =
        {
            new FloorSpec { Floor = "5F", Area = "中文圖書區", Levels = new[] { LevelId.Bookshelf } },
            new FloorSpec { Floor = "4F", Area = "", Levels = new LevelId[0] },
            new FloorSpec { Floor = "3F", Area = "學習共享空間", Levels = new[] { LevelId.Train, LevelId.Collage } },
            new FloorSpec { Floor = "2F", Area = "宜大記憶庫", Levels = new[] { LevelId.Dig, LevelId.Timeline, LevelId.Finale } },
            new FloorSpec { Floor = "1F", Area = "", Levels = new LevelId[0] },
        };

        [MenuItem("Tracking NIU/產生 Map_Library 場景", false, 20)]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("產生 Map_Library 場景",
                    "Map_Library 已經存在，重新產生會覆蓋場景中的所有修改。要繼續嗎？", "覆蓋", "取消"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build();
        }

        /// <summary>batch mode 用：Unity.exe -batchmode -quit -executeMethod TrackingNIU.EditorTools.MapLibrarySceneBuilder.Build</summary>
        public static void Build()
        {
            var parts = new MapSceneParts(MapKind.Library, "圖資館樓層地圖");
            var building = parts.BuildMapPanel("Building", "國立宜蘭大學圖書資訊館");

            const float floorHeight = 140f;
            const float floorGap = 12f;
            var locations = new List<MapLocation>();
            for (int i = 0; i < s_floors.Length; i++)
            {
                var spec = s_floors[i];
                var fill = parts.Ui.OutlinedPanel($"Floor {spec.Floor}", building, 32, 88 + i * (floorHeight + floorGap), 1096, floorHeight, UiPalette.Disabled, out var floor);
                var floorLabel = UiBuilder.AddText(floor, "FloorNumber", 28, 0, 120, floorHeight, spec.Floor, parts.Ui.Bold, 60, Color.white, TextAlignmentOptions.MidlineLeft);
                var areaLabel = UiBuilder.AddText(floor, "Area", 150, 0, 360, floorHeight, spec.Area, parts.Ui.Bold, 36, Color.white, TextAlignmentOptions.MidlineLeft);

                var markers = new List<MapMarker>();
                for (int m = 0; m < spec.Levels.Length; m++)
                {
                    float x = 1096 - 24 - MapSceneParts.MarkerWidth * (spec.Levels.Length - m);
                    markers.Add(parts.BuildMarker(floor, spec.Levels[m], x, 0));
                }

                var zoneColor = spec.Levels.Length > 0 ? UiPalette.GetZoneColor(spec.Levels[0]) : UiPalette.Disabled;
                locations.Add(parts.MakeLocation(floor, fill, zoneColor, new TMP_Text[] { floorLabel, areaLabel }, markers.ToArray()));
            }

            parts.Finish(ScenePath, locations);
        }
    }
}
