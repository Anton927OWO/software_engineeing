using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 產生 Boot 場景：GameManager（含流程測試面板）與標題畫面。
    /// 標題畫面有遊戲名稱、「開始遊戲」「離開遊戲」、跨局文物圖鑑，以及開局時的抽關翻牌視窗。
    /// 產生後可以直接在場景中調整位置與外觀；重新產生會覆蓋場景。
    /// </summary>
    public static class BootSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Boot.unity";

        const string SoundFolder = "Assets/Common/audio/kenney_interface-sounds/Audio/";
        const string MusicPath = "Assets/Common/audio/Kistol - Snowfall (Looped ver.).ogg";
        const float CornerRadius = 32f;
        const float CollectionCardSize = 96f;
        const float CollectionCardGap = 24f;

        // 翻牌視窗：起點、三張抽到的關卡、終點排成一列。
        const float PanelWidth = 1500f;
        const float RouteTop = 200f;
        const float RouteHeight = 320f;
        const float FixedCardWidth = 200f;
        const float DrawCardWidth = 260f;
        const float ArrowWidth = 50f;

        [MenuItem("Tracking NIU/產生 Boot 標題場景", false, 19)]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("產生 Boot 標題場景",
                    "Boot 已經存在，重新產生會覆蓋場景中的所有修改。要繼續嗎？", "覆蓋", "取消"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build();
        }

        /// <summary>batch mode 用：Unity.exe -batchmode -quit -executeMethod TrackingNIU.EditorTools.BootSceneBuilder.Build</summary>
        public static void Build()
        {
            var ui = new UiBuilder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var manager = new GameObject("GameManager");
            manager.AddComponent<GameManager>();
            manager.AddComponent<GameFlowDebugPanel>();

            var frame = ui.CreateScreen("TitleCanvas", out var canvas);
            var screen = canvas.gameObject.AddComponent<TitleScreen>();

            UiBuilder.AddImage(UiBuilder.Node("TopStrip", frame, 0, 0, 1920, 12), null, UiPalette.Primary);
            UiBuilder.AddText(frame, "Eyebrow", 0, 150, 1920, 48, "國立宜蘭大學 圖資館 × 無圍牆博物館", ui.Regular, 32, UiPalette.MutedText, TextAlignmentOptions.Center);
            UiBuilder.AddText(frame, "Title", 0, 200, 1920, 290, "尋跡宜大", ui.Serif, 200, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuilder.AddImage(UiBuilder.Node("Rule", frame, 880, 500, 160, 8), null, UiPalette.Primary);
            UiBuilder.AddText(frame, "Subtitle", 0, 530, 1920, 54, "走進宜大的百年記憶，找回六件出土文物", ui.Regular, 36, UiPalette.Text, TextAlignmentOptions.Center);

            var start = BuildButton(ui, frame, "StartButton", "開始遊戲", 740, 630, 440, 100, UiPalette.Primary);
            var quit = BuildButton(ui, frame, "QuitButton", "離開遊戲", 780, 752, 360, 80, UiPalette.MutedText);

            // 跨局文物圖鑑
            var progress = UiBuilder.AddText(frame, "Progress", 0, 864, 1920, 42, "文物圖鑑 0 / 6", ui.Regular, 28, UiPalette.MutedText, TextAlignmentOptions.Center);
            var artifacts = ArtifactCollection.All;
            float cardsLeft = (1920f - (artifacts.Length * CollectionCardSize + (artifacts.Length - 1) * CollectionCardGap)) / 2f;
            var cards = new ResultArtifactCard[artifacts.Length];
            for (int i = 0; i < artifacts.Length; i++)
                cards[i] = ResultsSceneBuilder.BuildCard(ui, frame, artifacts[i], cardsLeft + i * (CollectionCardSize + CollectionCardGap), 916, CollectionCardSize);

            UiBuilder.AddText(frame, "Credit", 1420, 1032, 460, 36, "第一組・互動遊戲程式設計 期中專案", ui.Regular, 22, UiPalette.MutedText, TextAlignmentOptions.MidlineRight);

            var overlay = BuildDrawOverlay(ui, canvas.transform, out var drawCards, out var depart);

            var music = canvas.gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.volume = 0.3f;
            var sfx = canvas.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            UiBuilder.Wire(screen,
                ("startButton", start),
                ("quitButton", quit),
                ("artifactCards", cards.Cast<Object>().ToArray()),
                ("progressText", progress),
                ("drawOverlay", overlay),
                ("drawCards", drawCards.Cast<Object>().ToArray()),
                ("departButton", depart),
                ("musicSource", music),
                ("sfxSource", sfx));

            var musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            var clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "click_001.ogg");
            var flipSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "switch_002.ogg");
            if (musicClip != null)
                UiBuilder.Wire(screen, ("music", musicClip));
            if (clickSound != null)
                UiBuilder.Wire(screen, ("clickSound", clickSound));
            if (flipSound != null)
                UiBuilder.Wire(screen, ("flipSound", flipSound));

            int missing = UiBuilder.ValidateWiring("music", "clickSound", "flipSound");
            ui.SaveScene(scene, ScenePath);
            MoveToFirstInBuild();
            if (missing == 0)
                Debug.Log($"[場景產生器] 已產生 {ScenePath}。");
        }

        /// <summary>半透明遮罩與翻牌視窗。蓋住整個 Canvas，非 16:9 時兩側也會變暗。</summary>
        static CanvasGroup BuildDrawOverlay(UiBuilder ui, Transform canvas, out DrawCard[] drawCards, out Button depart)
        {
            var root = UiBuilder.Stretch("DrawOverlay", canvas);
            UiBuilder.AddImage(root, null, new Color(0f, 0f, 0f, 0.65f));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(root, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1920f, 1080f);

            ui.OutlinedPanel("Panel", content, (1920f - PanelWidth) / 2f, 200, PanelWidth, 680, UiPalette.Panel, out var panel);
            UiBuilder.AddText(panel, "Heading", 0, 36, PanelWidth, 84, "本次探索路線", ui.Bold, 56, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuilder.AddText(panel, "Hint", 0, 120, PanelWidth, 42, "館內關卡抽 2 關、戶外關卡抽 1 關，最後回到記憶庫挑戰終章", ui.Regular, 28, UiPalette.MutedText, TextAlignmentOptions.Center);

            float x = (PanelWidth - (FixedCardWidth * 2 + DrawCardWidth * 3 + ArrowWidth * 4)) / 2f;
            BuildFace(ui, panel, "Start", x, FixedCardWidth, "起點", UiPalette.GetZoneColor(LevelId.Prologue), "序章", "知識之林尋書", "", out _, out _, out _);
            x += FixedCardWidth;

            drawCards = new DrawCard[3];
            string[] pools = { "館內抽籤", "館內抽籤", "戶外抽籤" };
            for (int i = 0; i < drawCards.Length; i++)
            {
                AddArrow(ui, panel, x);
                x += ArrowWidth;
                drawCards[i] = BuildDrawCard(ui, panel, i, x, pools[i]);
                x += DrawCardWidth;
            }

            AddArrow(ui, panel, x);
            x += ArrowWidth;
            BuildFace(ui, panel, "Finish", x, FixedCardWidth, "終點", UiPalette.GetZoneColor(LevelId.Finale), "終章", "文物記憶翻牌", "", out _, out _, out _);

            depart = BuildButton(ui, panel, "DepartButton", "出發！", (PanelWidth - 400f) / 2f, 556, 400, 92, UiPalette.Primary);
            // 平常隱藏，場景視窗才看得到標題畫面；要調整翻牌視窗時在 Hierarchy 打開即可。
            root.gameObject.SetActive(false);
            return group;
        }

        static DrawCard BuildDrawCard(UiBuilder ui, RectTransform parent, int index, float x, string pool)
        {
            var root = UiBuilder.Node($"DrawCard {index + 1}", parent, x, RouteTop, DrawCardWidth, RouteHeight);
            // 翻牌時以中心縮放。
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition += new Vector2(DrawCardWidth / 2f, -RouteHeight / 2f);

            ui.OutlinedPanel("Back", root, 0, 0, DrawCardWidth, RouteHeight, UiPalette.Primary, out var back);
            UiBuilder.AddText(back, "Mark", 0, 40, DrawCardWidth, 200, "?", ui.Serif, 160, UiPalette.Panel, TextAlignmentOptions.Center);
            UiBuilder.AddText(back, "Pool", 0, 244, DrawCardWidth, 44, pool, ui.Bold, 30, UiPalette.Panel, TextAlignmentOptions.Center);

            var front = BuildFace(ui, root, "Front", 0, DrawCardWidth, $"第 {index + 1} 站", UiPalette.Text, "", "", pool,
                out var strip, out var name, out var location);
            // BuildFace 以 RouteTop 為上緣，放進卡片裡要歸零。
            front.anchoredPosition = Vector2.zero;

            var card = root.gameObject.AddComponent<DrawCard>();
            UiBuilder.Wire(card, ("back", back.gameObject), ("front", front.gameObject), ("zoneStrip", strip), ("nameText", name), ("locationText", location));
            return card;
        }

        /// <summary>卡片正面：站別、區域色條、關卡名稱、地點（可換行）與抽籤池。</summary>
        static RectTransform BuildFace(UiBuilder ui, RectTransform parent, string objectName, float x, float width,
            string order, Color zone, string levelName, string location, string pool,
            out Image strip, out TMP_Text nameText, out TMP_Text locationText)
        {
            ui.OutlinedPanel(objectName, parent, x, RouteTop, width, RouteHeight, UiPalette.Panel, out var root);
            UiBuilder.AddText(root, "Order", 0, 28, width, 40, order, ui.Regular, 26, UiPalette.MutedText, TextAlignmentOptions.Center);
            var stripRect = UiBuilder.Node("ZoneStrip", root, 40, 78, width - 80, 10);
            strip = UiBuilder.AddImage(stripRect, ui.Sprites.RoundedRect, zone, CornerRadius / 5f);
            nameText = UiBuilder.AddText(root, "Name", 16, 100, width - 32, 66, levelName, ui.Bold, 44, UiPalette.Text, TextAlignmentOptions.Center, minAutoSize: 30);
            var locationLabel = UiBuilder.AddText(root, "Location", 20, 176, width - 40, 76, location, ui.Regular, 24, UiPalette.Text, TextAlignmentOptions.Top);
            locationLabel.textWrappingMode = TextWrappingModes.Normal;
            locationText = locationLabel;
            if (pool.Length > 0)
                UiBuilder.AddText(root, "Pool", 0, 264, width, 36, pool, ui.Regular, 22, UiPalette.MutedText, TextAlignmentOptions.Center);
            return root;
        }

        static void AddArrow(UiBuilder ui, RectTransform parent, float x) =>
            UiBuilder.AddText(parent, "Arrow", x, RouteTop + RouteHeight / 2f - 40f, ArrowWidth, 80, "→", ui.Bold, 40, UiPalette.MutedText, TextAlignmentOptions.Center);

        static Button BuildButton(UiBuilder ui, RectTransform parent, string name, string label, float x, float y, float width, float height, Color fill)
        {
            var root = UiBuilder.Node(name, parent, x, y, width, height);
            var image = UiBuilder.AddImage(root, ui.Sprites.RoundedRect, fill, CornerRadius / (height / 2f));
            UiBuilder.AddText(root, "Label", 0, 0, width, height, label, ui.Bold, height * 0.44f, Color.white, TextAlignmentOptions.Center);
            return UiBuilder.AddButton(root.gameObject, image);
        }

        /// <summary>遊戲從 Build Settings 的第一個場景開始，所以 Boot 一定要排第一。</summary>
        static void MoveToFirstInBuild()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index <= 0)
                return;
            var boot = scenes[index];
            scenes.RemoveAt(index);
            scenes.Insert(0, boot);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
