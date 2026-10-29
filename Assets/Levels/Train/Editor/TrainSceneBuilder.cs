using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Train.Editor
{
    public static class TrainSceneBuilder
    {
        public const string Root = "Assets/Levels/Train";
        public const string ScenePath = Root + "/L_Train.unity";
        private const string SharedRoot = "Assets/Levels/Shared";
        private const string Art = "Assets/unity其中素材/Train/Sprites/";
        private static readonly Color Ink = new Color32(31, 58, 46, 255);
        private static readonly Color Paper = new Color32(255, 250, 238, 255);

        [Serializable] private sealed class Seed { public TrainQuestion[] questions; }

        [MenuItem("尋跡宜大/Train/建立缺少的知識列車資產與場景")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先離開 Play Mode。");
            Directory.CreateDirectory(Root + "/Data");
            Directory.CreateDirectory(Root + "/Fonts");
            AssetDatabase.Refresh();
            var bank = AssetDatabase.LoadAssetAtPath<TrainQuestionBank>(Root + "/Data/TrainQuestions.asset");
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<TrainQuestionBank>();
                bank.questions = JsonUtility.FromJson<Seed>(File.ReadAllText(Root + "/Data/TrainQuestions.seed.json")).questions;
                AssetDatabase.CreateAsset(bank, Root + "/Data/TrainQuestions.asset");
            }
            var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(Root + "/Data/TrainLevel.asset");
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SharedLevelDefinition>();
                var so = new SerializedObject(definition);
                so.FindProperty("levelId").stringValue = "Train";
                so.FindProperty("displayName").stringValue = "尋跡宜大｜知識列車";
                so.FindProperty("durationSeconds").floatValue = 60;
                so.FindProperty("mistakeLimit").intValue = 0;
                so.FindProperty("introductionTitle").stringValue = "知識列車｜校史車站";
                so.FindProperty("introductionBody").stringValue = "60 秒內答對五題，接滿五節車廂並出站。\n答錯不扣車廂；正解與原因會留在回饋區，可開「知識卡／來源」暫停細讀。\n\n時間到或題庫用完即失敗；接車與出站仍計時。\n介紹、知識卡與暫停不扣秒；暫停面板可重新開始。";
                so.FindProperty("showIntroduction").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, Root + "/Data/TrainLevel.asset");
            }
            string fontPath = Root + "/Fonts/Train NotoSansTC SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null &&
                !AssetDatabase.CopyAsset(SharedRoot + "/Fonts/NotoSansTC-Regular SDF.asset", fontPath))
                throw new IOException("無法複製 Train 專用字型。");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            string characters = definition.IntroductionTitle + definition.IntroductionBody +
                "百年起點校園記憶考古發現噶瑪蘭文化知識終點上一題再想想正解玩法答完可開知識卡細讀重新開始再探索知識車站已連接節車廂資料來源已查核待驗證知識車票正確答案題庫沒有可用題目請編輯答對了答錯了連接中五節集滿出發時間已到下次再挑戰題目用完知識列車已出站選出正確答案讓知識成為前進的動力空間介紹知識卡來源每答完一題可在此閱讀正解與解說本題" +
                string.Concat(bank.questions.Select(q => q.station + q.prompt + string.Concat(q.options) + q.explanation + q.sourceTitle + q.sourceUrl + q.verifiedOn));
            font.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Train 字型未收錄：" + missing);
            AssetDatabase.SaveAssetIfDirty(font);
            if (File.Exists(ScenePath)) { Debug.Log("保留既有 Train Scene 與可編輯資料。"); return; }
            CreateScene(bank, definition, font);
            Debug.Log("Train Scene generated: " + ScenePath);
        }

        [MenuItem("尋跡宜大/Train/開啟知識列車場景")]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static void CreateScene(TrainQuestionBank bank, SharedLevelDefinition definition, TMP_FontAsset font)
        {
            var previous = SceneManager.GetActiveScene();
            bool emptyUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path) &&
                previous.rootCount == 0 && !previous.isDirty;
            if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Any(s => string.IsNullOrEmpty(s.path) && !(s == previous && emptyUntitled)))
                throw new InvalidOperationException("請先儲存未命名的 Scene，再建立 Train 場景。");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                emptyUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                // Replacing an untitled scene can unload unreferenced ScriptableObjects.
                // Load persistent authoring assets only after the new scene is active.
                Populate(AssetDatabase.LoadAssetAtPath<TrainQuestionBank>(Root + "/Data/TrainQuestions.asset"),
                    AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(Root + "/Data/TrainLevel.asset"),
                    AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/Train NotoSansTC SDF.asset"));
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Train Scene 儲存失敗。");
            }
            finally
            {
                if (emptyUntitled) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static void Populate(TrainQuestionBank bank, SharedLevelDefinition definition, TMP_FontAsset font)
        {
            if (bank == null || definition == null || font == null)
                throw new InvalidOperationException("Train 場景建立需要有效的題庫、關卡設定與字型資產。");
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Paper;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule))
                .GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedRoot + "/Prefabs/SharedLevelRoot.prefab");
            if (prefab == null) throw new InvalidOperationException("SharedLevelRoot.prefab 不存在。");
            var sharedRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var shared = sharedRoot.GetComponent<SharedLevelController>();
            var sharedUI = sharedRoot.GetComponentInChildren<SharedLevelUI>(true);
            Set(shared, "definition", definition);
            foreach (var text in sharedRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            // Widen the shared card's body through instance overrides when no illustration is used.
            foreach (var panel in sharedRoot.GetComponentsInChildren<SharedContentPanel>(true))
            {
                var body = panel.transform.Find("Card/Body").GetComponent<TMP_Text>();
                body.rectTransform.anchoredPosition = new Vector2(0, 15);
                body.rectTransform.sizeDelta = new Vector2(1180, 410);
                body.fontSize = 28;
                PrefabUtility.RecordPrefabInstancePropertyModifications(body.rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
            }

            var worldCanvas = Rect("TrainScenery", null);
            ConfigureCanvas(worldCanvas, 0, false);
            var backdrop = Picture("Background", worldCanvas, Sprite("Background_1920x1080"), Vector2.zero, new Vector2(1920, 1080));
            Stretch(backdrop.rectTransform);
            Picture("Rail", worldCanvas, Sprite("Rail_1920"), new Vector2(0, -415), new Vector2(1920, 125));

            var canvasRect = Rect("TrainCanvas", sharedRoot.transform.Find("GameplayRoot"));
            ConfigureCanvas(canvasRect, 10, true);
            var safe = Rect("SafeArea", canvasRect);
            Stretch(safe);
            safe.gameObject.AddComponent<SharedSafeArea>();
            var card = Picture("QuestionCard", safe, SharedSprite("Panel_Cream"), new Vector2(0, 115), new Vector2(1610, 540));
            card.type = Image.Type.Sliced;
            var number = Text("QuestionNumber", card.transform, font, "知識車站", 25, new Vector2(-410, 205), new Vector2(650, 46), TextAlignmentOptions.MidlineLeft);
            var instruction = Text("Goal", card.transform, font, "60 秒・答對五題・接滿車廂", 23, new Vector2(425, 205), new Vector2(650, 46));
            instruction.color = new Color32(125, 92, 48, 255);
            var question = Text("Prompt", card.transform, font, "準備出發", 38, new Vector2(0, 130), new Vector2(1450, 100));
            var choices = new Button[3];
            for (int i = 0; i < 3; i++) choices[i] = Button("Choice" + i, card.transform, font,
                ((char)('A' + i)).ToString(), new Vector2(0, 28 - i * 89), new Vector2(1420, 76), 29);
            var source = Text("Source", card.transform, font, "資料來源", 22, new Vector2(0, -220), new Vector2(1450, 45));
            var feedback = Text("Feedback", safe, font, "", 26, new Vector2(190, -210), new Vector2(1260, 100));
            var progress = Text("Progress", safe, font, "已連接　0 / 5 節車廂", 25, new Vector2(-610, -195), new Vector2(450, 65));
            var trainRoot = Rect("Train", safe);
            Place(trainRoot, new Vector2(0, -330), new Vector2(1750, 200));
            Picture("Locomotive", trainRoot, Sprite("Locomotive"), new Vector2(-650, 0), new Vector2(270, 145));
            var carriages = new RectTransform[5];
            var stations = new TMP_Text[5];
            for (int i = 0; i < carriages.Length; i++)
            {
                carriages[i] = Picture("Carriage" + (i + 1), trainRoot, Sprite("Carriage"),
                    new Vector2(-380 + i * 235, -10), new Vector2(224, 125)).rectTransform;
                stations[i] = Text("StationLabel", carriages[i], font, "", 22,
                    new Vector2(0, 5), new Vector2(215, 65));
                carriages[i].gameObject.SetActive(false);
            }
            var intro = Button("SpaceIntroduction", safe, font, "玩法", new Vector2(-630, -490), new Vector2(310, 72), 27);
            var knowledge = Button("Knowledge", safe, font, "知識卡／來源", new Vector2(-245, -490), new Vector2(390, 72), 27);
            Text("OperationHint", safe, font, "滑鼠點選 A／B／C　｜　Escape 暫停　｜　暫停面板可重新開始", 22,
                new Vector2(490, -490), new Vector2(950, 65));
            var logic = new GameObject("TrainLevelController").AddComponent<TrainLevelController>();
            logic.transform.SetParent(sharedRoot.transform.Find("GameplayRoot"), false);
            Set(logic, "shared", shared); Set(logic, "sharedUI", sharedUI);
            Set(logic, "audioPlayer", sharedRoot.GetComponentInChildren<SharedAudioPlayer>());
            Set(logic, "questionBank", bank); Set(logic, "questionNumber", number);
            Set(logic, "prompt", question); Set(logic, "source", source);
            Set(logic, "feedback", feedback); Set(logic, "progress", progress);
            Set(logic, "introductionButton", intro); Set(logic, "knowledgeButton", knowledge);
            Set(logic, "trainRoot", trainRoot); SetArray(logic, "choices", choices); SetArray(logic, "carriageViews", carriages); SetArray(logic, "carriageStations", stations);
        }

        private static void ConfigureCanvas(RectTransform rect, int order, bool raycasts)
        {
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            if (raycasts) rect.gameObject.AddComponent<GraphicRaycaster>();
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Art + name + ".png").OfType<Sprite>().First();
        private static Sprite SharedSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SharedRoot + "/Sprites/" + name + ".png");
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static Image Picture(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = true;
            Place(image.rectTransform, pos, size); return image;
        }
        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size,
            Vector2 pos, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Ink; text.alignment = alignment;
            text.richText = false; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            Place(text.rectTransform, pos, rectSize); return text;
        }
        private static Button Button(string name, Transform parent, TMP_FontAsset font, string label, Vector2 pos, Vector2 size, float textSize)
        {
            var image = Picture(name, parent, SharedSprite("Button_Green"), pos, size);
            image.type = Image.Type.Sliced; image.preserveAspect = false; image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 1); button.colors = colors;
            var text = Text("Label", image.transform, font, label, textSize, Vector2.zero, size - new Vector2(40, 12));
            text.color = Paper; return button;
        }
        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        private static void SetArray<T>(UnityEngine.Object target, string name, T[] values) where T : UnityEngine.Object
        {
            var so = new SerializedObject(target); var property = so.FindProperty(name); property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
