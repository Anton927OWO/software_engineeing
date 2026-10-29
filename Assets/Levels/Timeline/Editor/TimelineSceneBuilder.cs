using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Timeline.Editor
{
    /// <summary>Creates serialized objects with Unity Editor APIs; never writes another level.</summary>
    public static class TimelineSceneBuilder
    {
        public const string Root = "Assets/Levels/Timeline";
        public const string ScenePath = Root + "/L_Timeline.unity";
        private const string Shared = "Assets/Levels/Shared";
        private const string Source = "Assets/unity其中素材/Timeline/Sprites";
        private static readonly int[] Years = { 1926, 1946, 1988, 1998, 2003 };
        private static readonly string[] Names = { "農林學校", "農業職校", "農工專科", "技術學院", "宜蘭大學" };
        private static readonly string[] SchoolSprites = { "School_01_1926", "School_02_1946", "School_03_1988", "School_04_1998", "School_05_2003" };
        private static readonly Color Ink = new Color32(31, 58, 46, 255);

        [MenuItem("尋跡宜大/Timeline/建立缺少的百年軌跡場景")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) { Debug.Log("Timeline scene already exists; preserved: " + ScenePath); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Shared + "/Prefabs/SharedLevelRoot.prefab");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Shared + "/Fonts/NotoSansTC-Regular SDF.asset");
            if (prefab == null || font == null) throw new InvalidOperationException("Shared prefab and font are required.");
            var previous = SceneManager.GetActiveScene();
            bool replaceEmpty = SceneManager.sceneCount == 1 && previous.IsValid() &&
                string.IsNullOrEmpty(previous.path) && !previous.isDirty && previous.rootCount == 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path) && !replaceEmpty)
                    throw new InvalidOperationException("Save untitled scenes before generating Timeline. Open content is preserved.");

            EnsureFolder(Root + "/Data");
            EnsureFolder(Root + "/Sprites");
            foreach (string name in SchoolSprites) CopySprite(name);
            CopySprite("Background_1920x1080");
            CopySprite("Timeline_Track");
            CreateDefinition();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceEmpty ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                // NewScene unloads unused assets. Resolve these after that boundary so the
                // scene never serializes a destroyed ScriptableObject as a null override.
                var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(Root + "/Data/TimelineDefinition.asset");
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Shared + "/Prefabs/SharedLevelRoot.prefab");
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Shared + "/Fonts/NotoSansTC-Regular SDF.asset");
                if (definition == null || prefab == null || font == null)
                    throw new InvalidOperationException("Could not resolve Timeline scene dependencies.");
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 0, -10);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5.4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(243, 234, 219, 255);
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                var sharedRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var controller = sharedRoot.GetComponent<SharedLevelController>();
                Set(controller, "definition", definition);
                PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
                Populate(sharedRoot, font);
                TimelineQualityUpgrade.ApplyPresentation(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Failed to save " + ScenePath);
                Debug.Log("Timeline scene generated with five cards and five slots: " + ScenePath);
            }
            finally
            {
                if (replaceEmpty) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [MenuItem("尋跡宜大/Timeline/開啟百年軌跡場景")]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) BuildAll();
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static SharedLevelDefinition CreateDefinition()
        {
            const string path = Root + "/Data/TimelineDefinition.asset";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(path);
            if (!AssetDatabase.CopyAsset(Shared + "/Data/SharedFoundation.asset", path))
                throw new IOException("Cannot copy Shared level definition.");
            var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(path);
            var so = new SerializedObject(definition);
            so.FindProperty("levelId").stringValue = "Timeline";
            so.FindProperty("displayName").stringValue = "尋跡宜大｜百年軌跡";
            so.FindProperty("durationSeconds").floatValue = 45;
            so.FindProperty("mistakeLimit").intValue = 0;
            so.FindProperty("showIntroduction").boolValue = true;
            so.FindProperty("introductionTitle").stringValue = "百年軌跡｜校史時光廊";
            so.FindProperty("introductionBody").stringValue = TimelineQualityUpgrade.IntroductionBody;
            so.FindProperty("introductionImage").objectReferenceValue = Sprite(SchoolSprites[0]);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(definition);
            return definition;
        }

        private static void Populate(GameObject root, TMP_FontAsset font)
        {
            var canvas = root.GetComponentInChildren<Canvas>();
            var background = Image("TimelineBackground", canvas.transform, Sprite("Background_1920x1080"), Color.white, false);
            Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();
            var safe = canvas.transform.Find("SafeArea");
            var boardRect = Rect("TimelineBoard", safe);
            Stretch(boardRect);
            boardRect.SetAsFirstSibling();
            var board = boardRect.gameObject.AddComponent<TimelineBoard>();
            Set(board, "controller", root.GetComponent<SharedLevelController>());
            Set(board, "ui", root.GetComponentInChildren<SharedLevelUI>());
            Set(board, "audioPlayer", root.GetComponentInChildren<SharedAudioPlayer>());

            Text("Eyebrow", boardRect, font, "校史探索  /  1926—2003", 22, new Vector2(-530, 348), new Vector2(740, 40), TextAlignmentOptions.MidlineLeft);
            Text("Title", boardRect, font, "百年軌跡", 62, new Vector2(-530, 284), new Vector2(740, 90), TextAlignmentOptions.MidlineLeft);
            Text("Instructions", boardRect, font, "將五張學制卡，放回屬於它的年代。", 27, new Vector2(-420, 240), new Vector2(960, 44), TextAlignmentOptions.MidlineLeft);
            var intro = Button("SpaceIntroduction", boardRect, font, "空間介紹", new Vector2(425, 302), new Vector2(270, 74));
            var knowledge = Button("Knowledge", boardRect, font, "閱讀知識卡", new Vector2(745, 302), new Vector2(300, 74));
            Set(board, "spaceIntroduction", intro);
            Set(board, "knowledge", knowledge);

            var track = Image("TimelineTrack", boardRect, Sprite("Timeline_Track"), Color.white, false);
            Place(track.rectTransform, new Vector2(20, 130), new Vector2(1800, 220));
            var tray = Image("CardTray", boardRect, AssetDatabase.LoadAssetAtPath<Sprite>(Shared + "/Sprites/Panel_Cream.png"), new Color32(239, 229, 209, 255), false);
            tray.type = UnityEngine.UI.Image.Type.Sliced;
            Place(tray.rectTransform, new Vector2(0, -337), new Vector2(1840, 320));
            Text("TrayLabel", boardRect, font, "待定位的學制卡", 26, new Vector2(-640, -196), new Vector2(500, 50), TextAlignmentOptions.MidlineLeft);
            var progress = Text("Progress", boardRect, font, "已定位 0 / 5", 28, new Vector2(710, 240), new Vector2(360, 44));
            Set(board, "progress", progress);
            var feedback = Text("Feedback", boardRect, font, "拖曳學制卡到對應年份；閱讀知識卡可獲得提示。", 23,
                new Vector2(240, -196), new Vector2(1280, 44));
            Set(board, "feedback", feedback);
            Text("Footer", boardRect, font, "錯放可再試  ·  五張全對即可通關  ·  Esc 暫停", 20,
                new Vector2(0, -520), new Vector2(1750, 34));
            var cards = new UnityEngine.Object[5];
            var slots = new UnityEngine.Object[5];
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 360;
                Text("Year_" + Years[i], boardRect, font, Years[i].ToString(), 40, new Vector2(x, 182), new Vector2(230, 58));
                var surface = Image("Slot_" + Years[i], boardRect,
                    AssetDatabase.LoadAssetAtPath<Sprite>(Shared + "/Sprites/Panel_Cream.png"), new Color32(250, 246, 236, 230), true);
                surface.type = UnityEngine.UI.Image.Type.Sliced;
                Place(surface.rectTransform, new Vector2(x, -25), new Vector2(310, 266));
                var border = surface.gameObject.AddComponent<Outline>();
                border.effectColor = new Color32(170, 148, 116, 160);
                border.effectDistance = new Vector2(2, -2);
                var prompt = Text("DropPrompt", surface.transform, font, "拖到這裡\n放入學制卡", 26, Vector2.zero, new Vector2(260, 160));
                var slot = surface.gameObject.AddComponent<TimelineSlot>();
                Set(slot, "board", board);
                Set(slot, "surface", surface);
                Set(slot, "prompt", prompt);
                SetInt(slot, "index", i);
                slots[i] = slot;

                var cardSurface = Image("Card_" + Years[i], boardRect,
                    AssetDatabase.LoadAssetAtPath<Sprite>(Shared + "/Sprites/Panel_Cream.png"), Color.white, true);
                cardSurface.type = UnityEngine.UI.Image.Type.Sliced;
                Place(cardSurface.rectTransform, new Vector2((4 - i - 2) * 360, -340), new Vector2(300, 250));
                var shadow = cardSurface.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.1f, 0.15f, 0.11f, 0.22f);
                shadow.effectDistance = new Vector2(0, -5);
                cardSurface.gameObject.AddComponent<CanvasGroup>();
                // Clip the printed year from the supplied artwork, so the school identity is the puzzle.
                var viewport = Rect("SchoolArtwork", cardSurface.transform);
                Place(viewport, new Vector2(0, 35), new Vector2(280, 144));
                viewport.gameObject.AddComponent<RectMask2D>();
                var art = Image("Illustration", viewport, Sprite(SchoolSprites[i]), Color.white, false);
                Place(art.rectTransform, new Vector2(0, -30), new Vector2(280, 204));
                Text("SchoolName", cardSurface.transform, font, Names[i], 34, new Vector2(0, -67), new Vector2(270, 60));
                var gesture = Text("Gesture", cardSurface.transform, font, "拖曳至年份", 20, new Vector2(0, -108), new Vector2(260, 30));
                gesture.color = new Color32(124, 89, 58, 255);
                var card = cardSurface.gameObject.AddComponent<TimelineCard>();
                Set(card, "board", board);
                SetInt(card, "targetIndex", i);
                Set(card, "illustration", Sprite(SchoolSprites[i]));
                Set(card, "gestureLabel", gesture);
                cards[i] = card;
            }
            SetArray(board, "cards", cards);
            SetArray(board, "slots", slots);
        }

        private static void CopySprite(string name)
        {
            string path = Root + "/Sprites/" + name + ".png";
            if (File.Exists(path)) return;
            File.Copy(Source + "/" + name + ".png", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Sprites/" + name + ".png");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size,
            Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            Place(text.rectTransform, position, dimensions);
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = Ink;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Button Button(string name, Transform parent, TMP_FontAsset font, string label, Vector2 position, Vector2 size)
        {
            var image = Image(name, parent, AssetDatabase.LoadAssetAtPath<Sprite>(Shared + "/Sprites/Button_Green.png"), Color.white, true);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            Place(image.rectTransform, position, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", image.transform, font, label, 28, Vector2.zero, size - new Vector2(20, 8));
            text.color = new Color32(251, 248, 242, 255);
            return button;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
