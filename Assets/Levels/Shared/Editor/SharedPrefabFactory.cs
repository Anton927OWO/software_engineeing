using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Shared.Editor
{
    /// <summary>Authoring factory for later level generators; creates only shared infrastructure.</summary>
    public static class SharedPrefabFactory
    {
        private static readonly Color Ink = new Color32(31, 58, 46, 255);
        private static readonly Color Paper = new Color32(251, 248, 242, 255);
        private static readonly Color Green = new Color32(47, 143, 91, 255);

        public static GameObject Create(TMP_FontAsset font, SharedLevelDefinition definition, SharedAudioLibrary library)
        {
            var root = new GameObject("SharedLevelRoot");
            root.SetActive(false);
            var controller = root.AddComponent<SharedLevelController>();
            var relay = root.AddComponent<LevelResultRelay>();
            var audio = new GameObject("Audio");
            audio.transform.SetParent(root.transform, false);
            var uiSource = audio.AddComponent<AudioSource>();
            var gameSource = audio.AddComponent<AudioSource>();
            uiSource.playOnAwake = gameSource.playOnAwake = false;
            uiSource.spatialBlend = gameSource.spatialBlend = 0;
            uiSource.ignoreListenerPause = true;
            var player = audio.AddComponent<SharedAudioPlayer>();
            Set(player, "library", library); Set(player, "uiSource", uiSource); Set(player, "gameplaySource", gameSource);

            var gameplay = new GameObject("GameplayRoot");
            gameplay.transform.SetParent(root.transform, false);
            var canvasRect = Rect("SharedCanvas", root.transform);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            var ui = canvasRect.gameObject.AddComponent<SharedLevelUI>();
            var safe = Rect("SafeArea", canvasRect);
            Stretch(safe);
            safe.gameObject.AddComponent<SharedSafeArea>();

            var hud = Image("HUD", safe, Sprite("HUD_Cream"), Paper, false);
            Top(hud.rectTransform, new Vector2(0, -16), new Vector2(1888, 120));
            var name = Text("LevelName", hud.transform, font, "尋跡宜大｜共用基礎", 34, TextAlignmentOptions.MidlineLeft);
            Place(name.rectTransform, new Vector2(-600, 10), new Vector2(560, 56));
            var state = Text("Status", hud.transform, font, "準備探索", 20, TextAlignmentOptions.MidlineLeft);
            Place(state.rectTransform, new Vector2(-600, -30), new Vector2(560, 32));
            var time = Text("Countdown", hud.transform, font, "02:00", 44);
            Place(time.rectTransform, new Vector2(100, 0), new Vector2(240, 70));
            var mistakes = Text("Mistakes", hud.transform, font, "失誤　0", 28);
            Place(mistakes.rectTransform, new Vector2(490, 0), new Vector2(240, 64));
            var pause = Button("Pause", hud.transform, font, "暫停", new Vector2(780, 0), new Vector2(224, 70));

            var intro = ContentPanel("Introduction", safe, font, "空間介紹", false);
            var minigame = ContentPanel("Minigame", safe, font, "小遊戲", true);
            var knowledge = ContentPanel("KnowledgeCard", safe, font, "知識卡", false);
            var pauseOverlay = Overlay("PauseOverlay", safe);
            var pauseCard = Card("PauseCard", pauseOverlay.transform, new Vector2(900, 530));
            var pauseTitle = Text("Title", pauseCard.transform, font, "探索暫停中", 52);
            Place(pauseTitle.rectTransform, new Vector2(0, 135), new Vector2(720, 90));
            var pauseNote = Text("Body", pauseCard.transform, font, "休息一下，準備好後繼續探索。", 28);
            Place(pauseNote.rectTransform, new Vector2(0, 20), new Vector2(760, 95));
            var resume = Button("Resume", pauseCard.transform, font, "繼續探索", new Vector2(-190, -140), new Vector2(320, 82));
            var pauseRetry = Button("Retry", pauseCard.transform, font, "重新開始", new Vector2(190, -140), new Vector2(320, 82), true);
            var resultOverlay = Overlay("ResultOverlay", safe);
            var resultCard = Card("ResultCard", resultOverlay.transform, new Vector2(1000, 680));
            var resultTitle = Text("Title", resultCard.transform, font, "探索成功", 56);
            Place(resultTitle.rectTransform, new Vector2(0, 200), new Vector2(800, 100));
            var resultBody = Text("Body", resultCard.transform, font, "剩餘時間　02:00\n失誤次數　0\n收集文物　0", 34);
            Place(resultBody.rectTransform, new Vector2(0, 10), new Vector2(800, 225));
            var resultRetry = Button("Retry", resultCard.transform, font, "再試一次", new Vector2(0, -215), new Vector2(380, 90));
            var resultContinue = Button("ContinueToMap", resultCard.transform, font, "繼續探索", new Vector2(210, -215), new Vector2(380, 90));
            resultContinue.gameObject.SetActive(false);
            pauseOverlay.SetActive(false);
            resultOverlay.SetActive(false);

            Set(controller, "definition", definition); Set(controller, "ui", ui); Set(controller, "audioPlayer", player);
            Set(relay, "controller", controller);
            Set(ui, "controller", controller); Set(ui, "audioPlayer", player);
            Set(ui, "levelName", name); Set(ui, "timer", time); Set(ui, "mistakes", mistakes); Set(ui, "status", state);
            Set(ui, "pauseButton", pause); Set(ui, "pausePanel", pauseOverlay); Set(ui, "resumeButton", resume);
            Set(ui, "pauseRetryButton", pauseRetry); Set(ui, "resultPanel", resultOverlay);
            Set(ui, "resultTitle", resultTitle); Set(ui, "resultBody", resultBody); Set(ui, "resultRetryButton", resultRetry); Set(ui, "resultContinueButton", resultContinue);
            Set(ui, "introduction", intro); Set(ui, "minigame", minigame); Set(ui, "knowledgeCard", knowledge);
            root.SetActive(true);
            return root;
        }

        private static SharedContentPanel ContentPanel(string name, Transform parent, TMP_FontAsset font, string title, bool hasMinigame)
        {
            var overlay = Overlay(name, parent);
            var panel = overlay.AddComponent<SharedContentPanel>();
            var card = Card("Card", overlay.transform, new Vector2(1360, 820));
            var heading = Text("Title", card.transform, font, title, 48);
            Place(heading.rectTransform, new Vector2(0, 280), new Vector2(1200, 95));
            var body = Text("Body", card.transform, font, "", 30, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, new Vector2(-210, hasMinigame ? 160 : 15), new Vector2(770, hasMinigame ? 140 : 390));
            var illustration = Image("Illustration", card.transform, null, Color.white, false);
            illustration.preserveAspect = true;
            Place(illustration.rectTransform, new Vector2(430, 30), new Vector2(300, 350));
            illustration.gameObject.SetActive(false);
            var content = Rect("ContentRoot", card.transform);
            Place(content, new Vector2(0, -30), new Vector2(1180, 270));
            var button = Button("Continue", card.transform, font, "繼續探索", new Vector2(0, -295), new Vector2(420, 90));
            Set(panel, "title", heading); Set(panel, "body", body); Set(panel, "illustration", illustration);
            Set(panel, "primaryButton", button); Set(panel, "primaryLabel", button.GetComponentInChildren<TMP_Text>());
            Set(panel, "contentRoot", content);
            overlay.SetActive(false);
            return panel;
        }

        private static GameObject Overlay(string name, Transform parent)
        {
            var image = Image(name, parent, null, new Color(0.04f, 0.09f, 0.07f, 0.72f), true);
            Stretch(image.rectTransform);
            return image.gameObject;
        }
        private static Image Card(string name, Transform parent, Vector2 size)
        {
            var card = Image(name, parent, Sprite("Panel_Cream"), Color.white, true);
            Place(card.rectTransform, Vector2.zero, size);
            return card;
        }
        internal static Button Button(string name, Transform parent, TMP_FontAsset font, string label,
            Vector2 position, Vector2 size, bool red = false)
        {
            var image = Image(name, parent, Sprite(red ? "Button_Red" : "Button_Green"), Color.white, true);
            Place(image.rectTransform, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.65f);
            button.colors = colors;
            var text = Text("Label", image.transform, font, label, 30);
            text.color = Paper;
            Stretch(text.rectTransform);
            text.margin = new Vector4(20, 6, 20, 6);
            return button;
        }
        internal static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        private static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }
        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        internal static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
        }
        private static void Top(RectTransform rect, Vector2 position, Vector2 size)
        {
            Place(rect, position, size);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SharedAssetPaths.Root + "/Sprites/" + name + ".png");
        internal static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new System.InvalidOperationException(target.name + ": missing field " + field);
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
