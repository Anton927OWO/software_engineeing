using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 產生無圍牆博物館地圖 Map_Campus 場景，沿用圖資館樓層地圖的 MapScreen 與右側面板，
    /// 左側改為校園簡圖：圖資館、噶瑪蘭文化廊道、行政大樓 B1 萬斌廳，以及從圖資館出發的路線。
    /// 產生後可以直接在場景中調整位置與外觀；重新產生會覆蓋場景。
    /// </summary>
    public static class MapCampusSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Map_Campus.unity";

        const float CardWidth = 420f;
        const float CardHeight = 260f;

        sealed class PlaceSpec
        {
            public string Name;
            public string Detail;
            public Vector2 Position;   // 地圖面板內的左上角
            public Color Zone;
            public LevelId Level;
        }

        static readonly PlaceSpec s_library = new PlaceSpec
        {
            Name = "圖書資訊館", Detail = "終章｜2F 宜大記憶庫", Position = new Vector2(80, 306),
            Zone = UiPalette.Text, Level = LevelId.Finale,
        };

        static readonly PlaceSpec[] s_outdoor =
        {
            new PlaceSpec
            {
                Name = "噶瑪蘭文化廊道", Detail = "戶外展示｜無圍牆博物館", Position = new Vector2(680, 130),
                Zone = UiPalette.GetZoneColor(LevelId.Deer), Level = LevelId.Deer,
            },
            new PlaceSpec
            {
                Name = "萬斌廳", Detail = "行政大樓 B1", Position = new Vector2(680, 500),
                Zone = UiPalette.GetZoneColor(LevelId.Strata), Level = LevelId.Strata,
            },
        };

        [MenuItem("Tracking NIU/產生 Map_Campus 場景", false, 21)]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("產生 Map_Campus 場景",
                    "Map_Campus 已經存在，重新產生會覆蓋場景中的所有修改。要繼續嗎？", "覆蓋", "取消"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build();
        }

        /// <summary>batch mode 用：Unity.exe -batchmode -quit -executeMethod TrackingNIU.EditorTools.MapCampusSceneBuilder.Build</summary>
        public static void Build()
        {
            var parts = new MapSceneParts(MapKind.Campus, "無圍牆博物館地圖");
            var campus = parts.BuildMapPanel("Campus", "國立宜蘭大學校園（示意圖）");

            // 路線先建立，才會畫在地點卡片後面。
            var routeLayer = UiBuilder.Node("Routes", campus, 0, 0, 1160, 872);
            var routes = new List<MapRoute>();
            var start = new Vector2(s_library.Position.x + CardWidth, s_library.Position.y + CardHeight / 2);
            foreach (var place in s_outdoor)
                routes.Add(BuildRoute(routeLayer, place.Level, start, new Vector2(place.Position.x, place.Position.y + CardHeight / 2)));

            var locations = new List<MapLocation> { BuildPlace(parts, campus, s_library) };
            foreach (var place in s_outdoor)
                locations.Add(BuildPlace(parts, campus, place));

            UiBuilder.AddText(campus, "Legend", 32, 800, 1096, 42, "路線：紅色是下一站，綠色是已完成，可以沿路線返回圖資館",
                parts.Ui.Regular, 28, UiPalette.MutedText, TextAlignmentOptions.MidlineLeft, minAutoSize: 22);

            parts.Finish(ScenePath, locations, routes);
        }

        static MapLocation BuildPlace(MapSceneParts parts, RectTransform campus, PlaceSpec spec)
        {
            var ui = parts.Ui;
            var fill = ui.OutlinedPanel($"Place {spec.Level}", campus, spec.Position.x, spec.Position.y, CardWidth, CardHeight, UiPalette.Disabled, out var card);
            var nameLabel = UiBuilder.AddText(card, "Name", 24, 16, 372, 56, spec.Name, ui.Bold, 36, Color.white, TextAlignmentOptions.MidlineLeft, minAutoSize: 28);
            var detailLabel = UiBuilder.AddText(card, "Detail", 24, 70, 372, 42, spec.Detail, ui.Regular, 28, Color.white, TextAlignmentOptions.MidlineLeft, minAutoSize: 22);
            var marker = parts.BuildMarker(card, spec.Level, (CardWidth - MapSceneParts.MarkerWidth) / 2, 112);
            return parts.MakeLocation(card, fill, spec.Zone, new TMP_Text[] { nameLabel, detailLabel }, new[] { marker });
        }

        /// <summary>從 from 到 to 的 12 px 直線（座標同 Node，以左上角為原點、y 向下）。</summary>
        static MapRoute BuildRoute(RectTransform parent, LevelId destination, Vector2 from, Vector2 to)
        {
            var delta = to - from;
            var rect = UiBuilder.Node($"Route {destination}", parent, from.x, from.y, delta.magnitude, 12);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(from.x, -from.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var line = UiBuilder.AddImage(rect, null, UiPalette.Disabled);
            line.raycastTarget = false;
            var route = rect.gameObject.AddComponent<MapRoute>();
            UiBuilder.Wire(route, ("destination", destination), ("line", line));
            return route;
        }
    }
}
