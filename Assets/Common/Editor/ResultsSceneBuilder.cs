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
    /// 產生結算畫面 Results 場景，版面依《關卡畫面設計說明》圖 4：
    /// ① 總分（思源宋體）② 稱號 ③ 各關分數 ④ 6 格文物圖鑑與進度，最下方是「結束」「重新遊玩」。
    /// 產生後可以直接在場景中調整位置與外觀；重新產生會覆蓋場景。
    /// </summary>
    public static class ResultsSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Results.unity";

        const string ArtifactSpriteFolder = "Assets/unity其中素材/Finale/Sprites/";
        const string SoundFolder = "Assets/Common/audio/kenney_interface-sounds/Audio/";
        const float CornerRadius = 32f;
        const int ScoreRowCount = 4;
        const float CardSize = 140f;
        const float CardGap = 26f;

        [MenuItem("Tracking NIU/產生 Results 結算場景", false, 22)]
        public static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("產生 Results 結算場景",
                    "Results 已經存在，重新產生會覆蓋場景中的所有修改。要繼續嗎？", "覆蓋", "取消"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Build();
        }

        /// <summary>batch mode 用：Unity.exe -batchmode -quit -executeMethod TrackingNIU.EditorTools.ResultsSceneBuilder.Build</summary>
        public static void Build()
        {
            var ui = new UiBuilder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var frame = ui.CreateScreen("ResultsCanvas", out var canvas, UiPalette.ResultsBackground);
            var screen = canvas.gameObject.AddComponent<ResultsScreen>();

            UiBuilder.AddText(frame, "Heading", 0, 36, 1920, 88, "探索結算", ui.Bold, 60, UiPalette.Panel, TextAlignmentOptions.Center);
            var playTime = UiBuilder.AddText(frame, "PlayTime", 0, 122, 1920, 42, "用時 0 秒", ui.Regular, 28, UiPalette.ResultsMutedText, TextAlignmentOptions.Center);

            // ① 總分
            var total = UiBuilder.AddText(frame, "Total", 0, 168, 1920, 196, "0", ui.Serif, 140, UiPalette.ResultsGold, TextAlignmentOptions.Center);

            // ② 稱號：膠囊形紅底，總分跑完後才彈出。
            var badge = UiBuilder.Node("TitleBadge", frame, 620, 368, 680, 96);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition += new Vector2(340f, -48f);
            Pill(ui, badge, UiPalette.Primary, 96f);
            var title = UiBuilder.AddText(badge, "Title", 0, 0, 680, 96, "", ui.Serif, 64, UiPalette.Panel, TextAlignmentOptions.Center, minAutoSize: 44);

            // ③ 各關分數
            var rows = new ResultScoreRow[ScoreRowCount];
            for (int i = 0; i < ScoreRowCount; i++)
            {
                var row = UiBuilder.Node($"Score {i + 1}", frame, 620, 496 + i * 62, 680, 56);
                var name = UiBuilder.AddText(row, "Name", 0, 0, 500, 56, "", ui.Regular, 36, UiPalette.ResultsMutedText, TextAlignmentOptions.MidlineLeft, minAutoSize: 26);
                var score = UiBuilder.AddText(row, "Score", 500, 0, 180, 56, "0", ui.Bold, 40, UiPalette.Panel, TextAlignmentOptions.MidlineRight);
                rows[i] = row.gameObject.AddComponent<ResultScoreRow>();
                UiBuilder.Wire(rows[i], ("nameText", name), ("scoreText", score));
            }

            // ④ 文物圖鑑
            var artifacts = ArtifactCollection.All;
            float cardsLeft = (1920f - (artifacts.Length * CardSize + (artifacts.Length - 1) * CardGap)) / 2f;
            var cards = new ResultArtifactCard[artifacts.Length];
            for (int i = 0; i < artifacts.Length; i++)
                cards[i] = BuildCard(ui, frame, artifacts[i], cardsLeft + i * (CardSize + CardGap), 768);
            var progress = UiBuilder.AddText(frame, "Progress", 0, 920, 1920, 42, "文物圖鑑 0 / 6", ui.Regular, 28, UiPalette.ResultsMutedText, TextAlignmentOptions.Center);

            var quit = BuildButton(ui, frame, "QuitButton", "結束", 540, 976, UiPalette.MutedText);
            var replay = BuildButton(ui, frame, "ReplayButton", "重新遊玩", 1020, 976, UiPalette.Primary);

            var music = canvas.gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.volume = 0.5f;
            var sfx = canvas.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            UiBuilder.Wire(screen,
                ("playTimeText", playTime),
                ("totalText", total),
                ("titleBadge", badge),
                ("titleText", title),
                ("scoreRows", rows.Cast<Object>().ToArray()),
                ("artifactCards", cards.Cast<Object>().ToArray()),
                ("progressText", progress),
                ("quitButton", quit),
                ("replayButton", replay),
                ("musicSource", music),
                ("sfxSource", sfx));

            var clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "click_001.ogg");
            var finishSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundFolder + "confirmation_002.ogg");
            if (clickSound != null)
                UiBuilder.Wire(screen, ("clickSound", clickSound));
            if (finishSound != null)
                UiBuilder.Wire(screen, ("finishSound", finishSound));

            int missing = UiBuilder.ValidateWiring("music", "clickSound", "finishSound");
            ui.SaveScene(scene, ScenePath);
            if (missing == 0)
                Debug.Log($"[場景產生器] 已產生 {ScenePath}。");
        }

        static ResultArtifactCard BuildCard(UiBuilder ui, RectTransform parent, ArtifactId artifact, float x, float y)
        {
            var root = UiBuilder.Node($"Artifact {artifact}", parent, x, y, CardSize, CardSize);
            var frameImage = UiBuilder.AddImage(root, ui.Sprites.RoundedRect, UiPalette.MutedText, CornerRadius / 20f);
            float innerScale = CornerRadius / (20f - UiBuilder.OutlineWidth);
            UiBuilder.AddImage(UiBuilder.Stretch("Fill", root, UiBuilder.OutlineWidth), ui.Sprites.RoundedRect, UiPalette.ResultsSlot, innerScale);

            var icon = UiBuilder.AddImage(UiBuilder.Stretch("Icon", root, 16f), LoadArtifactSprite(artifact), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 「本局」標籤壓在格子上緣。
            var tag = UiBuilder.Node("NewTag", root, 30, -18, 80, 36);
            Pill(ui, tag, UiPalette.ResultsGold, 36f);
            UiBuilder.AddText(tag, "Label", 0, 0, 80, 36, "本局", ui.Bold, 22, UiPalette.ResultsBackground, TextAlignmentOptions.Center);

            var card = root.gameObject.AddComponent<ResultArtifactCard>();
            UiBuilder.Wire(card, ("artifact", artifact), ("frame", frameImage), ("icon", icon), ("newTag", tag.gameObject));
            return card;
        }

        static Button BuildButton(UiBuilder ui, RectTransform parent, string name, string label, float x, float y, Color fill)
        {
            var root = UiBuilder.Node(name, parent, x, y, 360, 84);
            var image = Pill(ui, root, fill, 84f);
            UiBuilder.AddText(root, "Label", 0, 0, 360, 84, label, ui.Bold, 40, Color.white, TextAlignmentOptions.Center);
            return UiBuilder.AddButton(root.gameObject, image);
        }

        /// <summary>圓角半徑等於高度一半的膠囊形底圖。</summary>
        static Image Pill(UiBuilder ui, RectTransform rect, Color color, float height) =>
            UiBuilder.AddImage(rect, ui.Sprites.RoundedRect, color, CornerRadius / (height / 2f));

        static Sprite LoadArtifactSprite(ArtifactId artifact)
        {
            // Artifact_01～06 的順序與 ArtifactId 一致（金鯉魚、金箔片、幾何印紋陶罐、木鏟、木質圓形雕板、瑪瑙珠）。
            var path = $"{ArtifactSpriteFolder}Artifact_{(int)artifact:00}.png";
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
                throw new FileNotFoundException($"找不到文物圖 {path}");
            return sprite;
        }
    }
}
