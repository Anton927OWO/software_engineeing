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
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale.Editor
{
    public static class FinaleSceneBuilder
    {
        public const string Root = "Assets/Levels/Finale";
        public const string ScenePath = Root + "/L_Finale.unity";
        public const string DefinitionPath = Root + "/Data/Finale.asset";
        public const string FontPath = Root + "/Fonts/Finale NotoSansTC SDF.asset";
        private const string SharedRoot = "Assets/Levels/Shared";
        private const string Sprites = "Assets/unity其中素材/Finale/Sprites/";
        private static readonly Color Ink = new Color32(31, 58, 46, 255);
        private static readonly Color Paper = new Color32(251, 248, 242, 255);
        private static readonly Color Gold = new Color32(215, 174, 89, 255);
        private static readonly string[] Titles = { "金鯉魚", "金箔片", "幾何印紋陶罐", "木鏟", "木質圓形雕板", "瑪瑙珠" };
        [MenuItem("尋跡宜大/Finale/建立缺少的終章場景")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            EnsureFolder(Root + "/Data"); EnsureFolder(Root + "/Fonts");
            EnsureFont();
            EnsureDefinition();
            if (File.Exists(ScenePath)) return; // Safe to run repeatedly; keep hand edits.
            var previous = SceneManager.GetActiveScene();
            bool disposable = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(previous.path) &&
                !previous.isDirty && previous.rootCount == 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path) && !disposable)
                    throw new InvalidOperationException("Save untitled scenes before generating Finale. Open scenes were preserved.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                disposable ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                Populate(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save Finale scene.");
                Debug.Log("Finale scene created with linked SharedLevelRoot prefab: " + ScenePath);
            }
            finally
            {
                if (disposable) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [MenuItem("尋跡宜大/Finale/開啟終章場景")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        private static SharedLevelDefinition EnsureDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(DefinitionPath);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<SharedLevelDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("levelId").stringValue = "Finale";
            so.FindProperty("displayName").stringValue = "終章｜文物記憶翻牌";
            so.FindProperty("durationSeconds").floatValue = 90;
            so.FindProperty("mistakeLimit").intValue = 0;
            so.FindProperty("showIntroduction").boolValue = true;
            so.FindProperty("introductionTitle").stringValue = "終章空間｜找回文物記憶";
            so.FindProperty("introductionBody").stringValue =
                "先看本局的三件文物；2 秒提示不扣時間。\n\n90 秒內完成六組配對。相同文物保留金框；不同則在 0.8 秒後翻回並記錄一次失誤。\n\n配對時認識文物，通關後可點圖鑑查看來源。閱讀與暫停不扣時間。圖像為示意。";
            so.FindProperty("introductionImage").objectReferenceValue = Sprite("Artifact_01");
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            AssetDatabase.SaveAssetIfDirty(definition);
            return definition;
        }

        private static TMP_FontAsset EnsureFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) return font;
            // Own the dynamic atlas locally so adding Finale glyphs cannot dirty Shared font assets.
            var shared = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedRoot + "/Fonts/NotoSansTC-Regular SDF.asset");
            font = TMP_FontAsset.CreateFontAsset(shared.sourceFontFile, 42, 6, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Finale NotoSansTC SDF";
            font.material.name = font.name + " Material";
            string seed = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i)) +
                string.Join("", Titles) + AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/ArtifactFacts.json").text +
                "終章文物記憶翻牌空間介紹知識卡重新開始暫停繼續探索成功未完成本次已結束進行中準備剩餘時間失誤次數收集再試一次金框配對找回本局取得的三件兩秒後開始記住它們拼圖連結正確再看清楚翻回剛才位置休息一下好後完整一張相同六組都到了下次閱讀確認挑戰材料形狀紋樣通關倒數提示展示與秒後才開始共有種各兩張不同則等待並記錄一次即可可或閱讀不扣為示意圖。、，。｜：；／！？（）「」　×";
            font.TryAddCharacters(seed, out _);
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            var so = new SerializedObject(font);
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            return font;
        }

        private static void Populate(Scene scene)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 0, -10);
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(92, 28, 24, 255);
            var input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedRoot + "/Prefabs/SharedLevelRoot.prefab");
            if (prefab == null) throw new InvalidOperationException("SharedLevelRoot prefab is missing.");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            // NewScene unloads unused native assets, even when a local C# variable still refers to them.
            // Reload authoring assets after scene creation, immediately before binding the instance.
            var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(DefinitionPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var controller = root.GetComponent<SharedLevelController>();
            var ui = root.GetComponentInChildren<SharedLevelUI>(true);
            Set(controller, "definition", definition);
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            }
            var canvas = root.GetComponentInChildren<Canvas>(true);
            var background = Image("FinaleBackground", canvas.transform, Sprite("Background_1920x1080"), Color.white, false);
            Stretch(background.rectTransform); background.transform.SetAsFirstSibling();
            var safe = canvas.transform.Find("SafeArea");
            var view = Rect("FinaleView", safe); Stretch(view); view.SetAsFirstSibling();
            var gameplay = new GameObject("FinaleGameplay");
            gameplay.transform.SetParent(root.transform.Find("GameplayRoot"), false);
            var source = gameplay.AddComponent<FinaleArtifactSource>();
            var finale = gameplay.AddComponent<FinaleLevelController>();
            var heading = Text("BoardTitle", view, font, "找回文物記憶", 40, Paper);
            Place(heading.rectTransform, new Vector2(250, 359), new Vector2(960, 66));
            var side = Image("GuideCard", view, AssetDatabase.LoadAssetAtPath<Sprite>(SharedRoot + "/Sprites/Panel_Cream.png"), Color.white, false);
            Place(side.rectTransform, new Vector2(-650, -60), new Vector2(460, 800));
            var eyebrow = Text("Eyebrow", side.transform, font, "FINALE  /  終章", 23, new Color32(116, 83, 58, 255));
            Place(eyebrow.rectTransform, new Vector2(0, 323), new Vector2(390, 45));
            var sideTitle = Text("Title", side.transform, font, "文物的樣子，\n你還記得嗎？", 36, Ink);
            Place(sideTitle.rectTransform, new Vector2(0, 230), new Vector2(390, 110));
            var note = Text("Instructions", side.transform, font,
                "90 秒內完成六組配對。\n\n每次翻兩張，找出相同文物。\n配對成功會亮起金框。\n\n先翻一張，再閱讀知識卡，\n認識這件文物的圖像。", 23, Ink);
            Place(note.rectTransform, new Vector2(0, 44), new Vector2(390, 250));
            var progress = Text("Progress", side.transform, font, "記憶拼圖　0 / 6", 32, Ink);
            Place(progress.rectTransform, new Vector2(0, -130), new Vector2(390, 65));
            var intro = Button("SpaceIntroduction", side.transform, font, "空間介紹", new Vector2(0, -211));
            var knowledge = Button("Knowledge", side.transform, font, "閱讀知識卡", new Vector2(0, -285));
            var restart = Button("Restart", view, font, "重新開始", new Vector2(-650, -498), new Vector2(370, 57), true);
            var feedback = Text("Feedback", view, font, "準備好後，開始找回文物記憶。", 25, Paper);
            Place(feedback.rectTransform, new Vector2(250, -499), new Vector2(1040, 50));
            var grid = Rect("CardGrid", view);
            Place(grid, new Vector2(250, -63), new Vector2(832, 764));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(190, 244); layout.spacing = new Vector2(24, 16);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var views = new FinaleCardView[12];
            for (int i = 0; i < views.Length; i++) views[i] = CreateCard(grid, font, i);
            var hint = Image("AcquiredArtifactHint", view, null, new Color(0.05f, 0.05f, 0.04f, 0.94f), true);
            Stretch(hint.rectTransform);
            var hintCard = Image("HintCard", hint.transform, null, Paper, true);
            Place(hintCard.rectTransform, new Vector2(0, -20), new Vector2(1140, 520));
            var hintTitle = Text("Title", hintCard.transform, font, "本局取得的三件文物", 40, Ink);
            Place(hintTitle.rectTransform, new Vector2(0, 185), new Vector2(1040, 70));
            var hintNote = Text("Note", hintCard.transform, font, "記住它們，2 秒後開始挑戰", 26, Ink);
            Place(hintNote.rectTransform, new Vector2(0, 117), new Vector2(1040, 45));
            var hintImages = new Image[3]; var hintLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                hintImages[i] = Image("Artifact" + i, hintCard.transform, Sprite($"Artifact_{i + 1:00}"), Color.white, false);
                hintImages[i].preserveAspect = true;
                Place(hintImages[i].rectTransform, new Vector2((i - 1) * 340, -32), new Vector2(230, 210));
                hintLabels[i] = Text("ArtifactName" + i, hintCard.transform, font, "文物", 29, Ink);
                Place(hintLabels[i].rectTransform, new Vector2((i - 1) * 340, -185), new Vector2(330, 60));
            }
            hint.gameObject.SetActive(false);
            Set(finale, "artifactFacts", AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/ArtifactFacts.json"));
            Set(finale, "controller", controller); Set(finale, "ui", ui);
            Set(finale, "audioPlayer", root.GetComponentInChildren<SharedAudioPlayer>(true));
            Set(finale, "artifactSource", source); Set(finale, "cardBack", Sprite("CardBack"));
            Set(finale, "progress", progress); Set(finale, "feedback", feedback);
            Set(finale, "introductionButton", intro); Set(finale, "knowledgeButton", knowledge);
            Set(finale, "restartButton", restart); Set(finale, "hintPanel", hint.gameObject);
            SetArray(finale, "cards", views); SetArray(finale, "hintImages", hintImages); SetArray(finale, "hintLabels", hintLabels);
            var data = new SerializedObject(finale);
            var artifacts = data.FindProperty("artifacts"); artifacts.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var entry = artifacts.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = $"artifact-{i + 1:00}";
                entry.FindPropertyRelative("title").stringValue = Titles[i];
                entry.FindPropertyRelative("face").objectReferenceValue = Sprite($"CardFace_{i + 1:00}");
                entry.FindPropertyRelative("icon").objectReferenceValue = Sprite($"Artifact_{i + 1:00}");
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            // Fit the longer Finale introduction inside the existing Shared content panel.
            var introBody = safe.Find("Introduction/Card/Body").GetComponent<TMP_Text>();
            introBody.fontSize = 26;
            introBody.enableAutoSizing = true;
            introBody.fontSizeMin = 22; introBody.fontSizeMax = 26;
            Place(introBody.rectTransform, new Vector2(-210, -5), new Vector2(770, 450));
            PrefabUtility.RecordPrefabInstancePropertyModifications(introBody);
            PrefabUtility.RecordPrefabInstancePropertyModifications(introBody.rectTransform);
            var knowledgeBody = safe.Find("KnowledgeCard/Card/Body").GetComponent<TMP_Text>();
            knowledgeBody.fontSize = 24;
            knowledgeBody.enableAutoSizing = true;
            knowledgeBody.fontSizeMin = 22; knowledgeBody.fontSizeMax = 24;
            Place(knowledgeBody.rectTransform, new Vector2(-210, -5), new Vector2(770, 450));
            PrefabUtility.RecordPrefabInstancePropertyModifications(knowledgeBody);
            PrefabUtility.RecordPrefabInstancePropertyModifications(knowledgeBody.rectTransform);
            FinaleScenePolish.ConfigureInventory(root);
            FinaleScenePolish.ConfigureLearning(root);
        }

        private static FinaleCardView CreateCard(Transform parent, TMP_FontAsset font, int index)
        {
            var rect = Rect($"Card_{index + 1:00}", parent);
            var target = rect.gameObject.AddComponent<Image>(); target.color = Color.clear;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = target;
            var visual = Rect("Visual", rect); Stretch(visual);
            var frame = Image("MatchedGoldFrame", visual, null, Gold, false);
            Stretch(frame.rectTransform); frame.rectTransform.offsetMin = new Vector2(-6, -6); frame.rectTransform.offsetMax = new Vector2(6, 6);
            var image = Image("Artwork", visual, Sprite("CardBack"), Color.white, false);
            Stretch(image.rectTransform); image.preserveAspect = true;
            var matched = Text("Matched", visual, font, "已配對", 22, Ink);
            Place(matched.rectTransform, new Vector2(0, 105), new Vector2(148, 32));
            frame.gameObject.SetActive(false); matched.gameObject.SetActive(false);
            var view = rect.gameObject.AddComponent<FinaleCardView>();
            Set(view, "button", button); Set(view, "visual", visual); Set(view, "artwork", image);
            Set(view, "goldFrame", frame.gameObject); Set(view, "matchedLabel", matched);
            return view;
        }

        private static Button Button(string name, Transform parent, TMP_FontAsset font, string label,
            Vector2 position, Vector2? size = null, bool red = false)
        {
            var image = Image(name, parent, AssetDatabase.LoadAssetAtPath<Sprite>(SharedRoot + "/Sprites/Button_" + (red ? "Red" : "Green") + ".png"), Color.white, true);
            Place(image.rectTransform, position, size ?? new Vector2(370, 62));
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = Text("Label", image.transform, font, label, 26, Paper); Stretch(text.rectTransform);
            return button;
        }

        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = raycast;
            image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            return image;
        }

        private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Sprites + name + ".png").OfType<Sprite>().FirstOrDefault()
            ?? throw new InvalidOperationException("Missing Finale sprite: " + name);

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException(target.name + ": missing reference " + field);
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target); var array = so.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
