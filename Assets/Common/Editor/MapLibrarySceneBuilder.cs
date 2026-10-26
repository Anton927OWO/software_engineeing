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
            var ui = new UiBuilder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var frame = ui.CreateScreen("MapCanvas", out var canvas);
            ui.CreateHud(frame, "圖資館樓層地圖", UiPalette.Text, out var subtitle);
            var scoreText = UiBuilder.AddText(frame, "Score", 1356, 24, 500, 72, "總分 0", ui.Bold, 44, UiPalette.Text, TextAlignmentOptions.MidlineRight);

            var locations = BuildBuilding(ui, frame);
            var screen = canvas.gameObject.AddComponent<MapScreen>();
            BuildSidePanel(ui, frame, screen);

            var sfx = canvas.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            UiBuilder.Wire(screen,
                ("subtitle", subtitle),
                ("scoreText", scoreText),
                ("locations", locations.ToArray()),
                ("sfxSource", sfx));

            int missing = UiBuilder.ValidateWiring("clickSound");
            ui.SaveScene(scene, ScenePath);
            if (missing == 0)
                Debug.Log($"[場景產生器] 已產生 {ScenePath}。");
        }

        static List<MapLocation> BuildBuilding(UiBuilder ui, RectTransform frame)
        {
            ui.OutlinedPanel("Building", frame, 64, 160, 1160, 872, UiPalette.Panel, out var building);
            UiBuilder.AddText(building, "Header", 32, 16, 1000, 56, "國立宜蘭大學圖書資訊館", ui.Bold, 36, UiPalette.Text, TextAlignmentOptions.MidlineLeft);

            const float floorHeight = 140f;
            const float floorGap = 12f;
            var locations = new List<MapLocation>();
            for (int i = 0; i < s_floors.Length; i++)
            {
                var spec = s_floors[i];
                var fill = ui.OutlinedPanel($"Floor {spec.Floor}", building, 32, 88 + i * (floorHeight + floorGap), 1096, floorHeight, UiPalette.Disabled, out var floor);
                var floorLabel = UiBuilder.AddText(floor, "FloorNumber", 28, 0, 120, floorHeight, spec.Floor, ui.Bold, 60, Color.white, TextAlignmentOptions.MidlineLeft);
                var areaLabel = UiBuilder.AddText(floor, "Area", 150, 0, 360, floorHeight, spec.Area, ui.Bold, 36, Color.white, TextAlignmentOptions.MidlineLeft);

                var markers = new List<MapMarker>();
                for (int m = 0; m < spec.Levels.Length; m++)
                {
                    float x = 1096 - 24 - 200 * (spec.Levels.Length - m);
                    markers.Add(BuildMarker(ui, floor, spec.Levels[m], x));
                }

                var button = UiBuilder.AddButton(floor.gameObject, fill);
                var location = floor.gameObject.AddComponent<MapLocation>();
                var zoneColor = spec.Levels.Length > 0 ? UiPalette.GetZoneColor(spec.Levels[0]) : UiPalette.Disabled;
                UiBuilder.Wire(location,
                    ("button", button),
                    ("fill", fill),
                    ("labels", new Object[] { floorLabel, areaLabel }),
                    ("zoneColor", zoneColor),
                    ("markers", markers.ToArray()));
                locations.Add(location);
            }
            return locations;
        }

        static MapMarker BuildMarker(UiBuilder ui, Transform floor, LevelId levelId, float x)
        {
            var root = UiBuilder.Node($"Marker {levelId}", floor, x, 0, 200, 140);
            var ring = UiBuilder.AddImage(UiBuilder.Node("SelectionRing", root, 50, 0, 100, 100), ui.Sprites.Circle, UiPalette.Warning);
            ring.raycastTarget = false;
            ring.gameObject.SetActive(false);

            var icon = ui.AddStatusIcon(root, "Icon", 58, 8, 84);
            var label = UiBuilder.AddText(root, "Label", 0, 96, 200, 44, LevelCatalog.Get(levelId).DisplayName,
                ui.Bold, 28, Color.white, TextAlignmentOptions.Center, minAutoSize: 20);

            var button = UiBuilder.AddButton(root.gameObject, icon.GetComponent<UnityEngine.UI.Image>());
            var marker = root.gameObject.AddComponent<MapMarker>();
            UiBuilder.Wire(marker,
                ("levelId", levelId),
                ("button", button),
                ("icon", icon),
                ("label", label),
                ("selectionRing", ring.gameObject));
            return marker;
        }

        static void BuildSidePanel(UiBuilder ui, RectTransform frame, MapScreen screen)
        {
            ui.OutlinedPanel("SidePanel", frame, 1272, 160, 584, 872, UiPalette.Panel, out var panel);

            UiBuilder.AddText(panel, "TasksHeader", 32, 16, 520, 56, "本局任務", ui.Bold, 36, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            var rows = new List<TaskRow>();
            for (int i = 0; i < 4; i++)
            {
                var row = UiBuilder.Node($"Task {i + 1}", panel, 32, 80 + i * 90, 520, 86);
                var icon = ui.AddStatusIcon(row, "Icon", 0, 15, 56);
                var title = UiBuilder.AddText(row, "Title", 72, 0, 448, 44, "", ui.Bold, 30, UiPalette.Text, TextAlignmentOptions.MidlineLeft, minAutoSize: 24);
                var detail = UiBuilder.AddText(row, "Detail", 72, 44, 448, 42, "", ui.Regular, 28, UiPalette.MutedText, TextAlignmentOptions.MidlineLeft, minAutoSize: 22);
                var taskRow = row.gameObject.AddComponent<TaskRow>();
                UiBuilder.Wire(taskRow, ("icon", icon), ("title", title), ("detail", detail));
                rows.Add(taskRow);
            }

            UiBuilder.AddText(panel, "ArtifactsHeader", 32, 440, 520, 56, "文物收集", ui.Bold, 36, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            var slots = new List<ArtifactSlot>();
            for (int i = 0; i < 3; i++)
            {
                var fill = ui.OutlinedPanel($"Artifact {i + 1}", panel, 32 + i * 180, 500, 160, 120, UiPalette.Background, out var slotRoot);
                var nameText = UiBuilder.AddText(slotRoot, "Name", 8, 0, 144, 120, "", ui.Bold, 28, UiPalette.Text, TextAlignmentOptions.Center, minAutoSize: 20);
                nameText.textWrappingMode = TextWrappingModes.Normal;
                var placeholder = UiBuilder.AddText(slotRoot, "Placeholder", 0, 0, 160, 120, "?", ui.Bold, 64, UiPalette.Disabled, TextAlignmentOptions.Center);
                var slot = slotRoot.gameObject.AddComponent<ArtifactSlot>();
                UiBuilder.Wire(slot, ("fill", fill), ("nameText", nameText), ("placeholder", placeholder));
                slots.Add(slot);
            }

            var selectedName = UiBuilder.AddText(panel, "SelectedName", 32, 630, 520, 54, "", ui.Bold, 36, UiPalette.Text, TextAlignmentOptions.MidlineLeft, minAutoSize: 28);
            var selectedInfo = UiBuilder.AddText(panel, "SelectedInfo", 32, 684, 520, 42, "", ui.Regular, 28, UiPalette.MutedText, TextAlignmentOptions.MidlineLeft, minAutoSize: 22);

            var goFill = ui.OutlinedPanel("GoButton", panel, 32, 732, 520, 120, UiPalette.Primary, out var goRoot);
            var goLabel = UiBuilder.AddText(goRoot, "Label", 0, 0, 520, 120, "前往", ui.Bold, 44, Color.white, TextAlignmentOptions.Center);
            var goButton = UiBuilder.AddButton(goRoot.gameObject, goFill);

            UiBuilder.Wire(screen,
                ("taskRows", rows.ToArray()),
                ("artifactSlots", slots.ToArray()),
                ("selectedName", selectedName),
                ("selectedInfo", selectedInfo),
                ("goButton", goButton),
                ("goButtonFill", goFill),
                ("goLabel", goLabel));
        }
    }
}
