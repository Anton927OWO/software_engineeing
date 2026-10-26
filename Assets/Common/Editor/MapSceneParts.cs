using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 兩張地圖（Map_Library、Map_Campus）共用的版面：HUD 與總分、左側地圖面板、關卡標記、
    /// 右側任務清單與文物欄、前往按鈕。左側面板的內容由各自的場景產生器決定。
    /// </summary>
    public sealed class MapSceneParts
    {
        public const float MarkerWidth = 200f;
        public const float MarkerHeight = 140f;

        public UiBuilder Ui { get; }
        public Scene Scene { get; }
        public RectTransform Frame { get; }
        public MapScreen Screen { get; }

        readonly MapKind _mapKind;
        readonly TMP_Text _subtitle;
        readonly TMP_Text _scoreText;

        public MapSceneParts(MapKind mapKind, string title)
        {
            _mapKind = mapKind;
            Ui = new UiBuilder();
            Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Frame = Ui.CreateScreen("MapCanvas", out var canvas);
            Ui.CreateHud(Frame, title, UiPalette.Text, out _subtitle);
            _scoreText = UiBuilder.AddText(Frame, "Score", 1356, 24, 500, 72, "總分 0", Ui.Bold, 44, UiPalette.Text, TextAlignmentOptions.MidlineRight);
            Screen = canvas.gameObject.AddComponent<MapScreen>();
        }

        /// <summary>左側 1160×872 的地圖面板與標題，回傳面板，地點都放在裡面。</summary>
        public RectTransform BuildMapPanel(string name, string header)
        {
            Ui.OutlinedPanel(name, Frame, 64, 160, 1160, 872, UiPalette.Panel, out var panel);
            UiBuilder.AddText(panel, "Header", 32, 16, 1000, 56, header, Ui.Bold, 36, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            return panel;
        }

        /// <summary>200×140 的關卡標記：狀態圖示、選取框與關卡名稱。</summary>
        public MapMarker BuildMarker(Transform parent, LevelId levelId, float x, float y)
        {
            var ui = Ui;
            var root = UiBuilder.Node($"Marker {levelId}", parent, x, y, MarkerWidth, MarkerHeight);
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

        /// <summary>把地點設成可以點選的 MapLocation：抽到其中的關卡時顯示區域色。</summary>
        public MapLocation MakeLocation(RectTransform root, UnityEngine.UI.Image fill, Color zoneColor, TMP_Text[] labels, MapMarker[] markers)
        {
            var button = UiBuilder.AddButton(root.gameObject, fill);
            var location = root.gameObject.AddComponent<MapLocation>();
            UiBuilder.Wire(location,
                ("button", button),
                ("fill", fill),
                ("labels", labels),
                ("zoneColor", zoneColor),
                ("markers", markers));
            return location;
        }

        /// <summary>建立右側面板、接好 MapScreen 的欄位並存檔。</summary>
        public void Finish(string scenePath, IList<MapLocation> locations, IList<MapRoute> routes = null)
        {
            BuildSidePanel();

            var sfx = Screen.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            UiBuilder.Wire(Screen,
                ("mapKind", _mapKind),
                ("subtitle", _subtitle),
                ("scoreText", _scoreText),
                ("locations", ToArray(locations)),
                ("routes", ToArray(routes ?? new MapRoute[0])),
                ("sfxSource", sfx));

            int missing = UiBuilder.ValidateWiring("clickSound");
            Ui.SaveScene(Scene, scenePath);
            if (missing == 0)
                Debug.Log($"[場景產生器] 已產生 {scenePath}。");
        }

        static Object[] ToArray<T>(IList<T> items) where T : Object
        {
            var array = new Object[items.Count];
            for (int i = 0; i < items.Count; i++)
                array[i] = items[i];
            return array;
        }

        void BuildSidePanel()
        {
            var ui = Ui;
            ui.OutlinedPanel("SidePanel", Frame, 1272, 160, 584, 872, UiPalette.Panel, out var panel);

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

            UiBuilder.Wire(Screen,
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
