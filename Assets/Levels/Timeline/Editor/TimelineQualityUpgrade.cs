using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Timeline.Editor
{
    /// <summary>Edits the existing Timeline objects; does not rebuild the scene or Shared prefab.</summary>
    public static class TimelineQualityUpgrade
    {
        public const string IntroductionBody = "找出宜大學制變化的五個節點。\n\n倒數內放對五張卡即可通關；錯放可再試，並記錄失誤。\n開啟「年代提示」可查閱校史；閱讀與暫停期間不扣時間。";
        private static readonly Color32[] Accents =
        {
            new Color32(117, 73, 38, 255), new Color32(139, 63, 49, 255),
            new Color32(38, 102, 101, 255), new Color32(66, 78, 126, 255), new Color32(35, 105, 61, 255)
        };
        private static readonly Color32[] Paper =
        {
            new Color32(255, 237, 201, 255), new Color32(255, 226, 217, 255),
            new Color32(219, 245, 239, 255), new Color32(227, 234, 255, 255), new Color32(232, 249, 218, 255)
        };

        [MenuItem("尋跡宜大/Timeline/套用百年軌跡品質改善（保留場景）")]
        public static void UpgradeSavedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save modified scenes before applying the Timeline upgrade.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(TimelineSceneBuilder.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(TimelineSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                ApplyPresentation(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save Timeline scene.");
                var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(TimelineSceneBuilder.Root + "/Data/TimelineDefinition.asset");
                var so = new SerializedObject(definition);
                so.FindProperty("introductionBody").stringValue = IntroductionBody;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(definition);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ApplyPresentation(Scene scene)
        {
            var board = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TimelineBoard>(true)).Single();
            var root = board.transform;
            var feedback = root.Find("Feedback").GetComponent<TMP_Text>();
            Place(feedback.rectTransform, new Vector2(180, -194), new Vector2(1420, 68));
            feedback.fontSize = 28;
            feedback.alignment = TextAlignmentOptions.MidlineLeft;
            feedback.color = new Color32(31, 58, 46, 255);
            var trayLabel = root.Find("TrayLabel").GetComponent<TMP_Text>();
            Place(trayLabel.rectTransform, new Vector2(-730, -196), new Vector2(340, 50));
            trayLabel.fontSize = 24;
            root.Find("Knowledge/Label").GetComponent<TMP_Text>().text = "年代提示";
            var guide = (Button)new SerializedObject(board).FindProperty("spaceIntroduction").objectReferenceValue;
            guide.GetComponentInChildren<TMP_Text>(true).text = "操作說明";
            foreach (var card in board.GetComponentsInChildren<TimelineCard>(true))
            {
                int i = card.TargetIndex;
                card.GetComponent<Image>().color = Paper[i];
                var outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
                outline.effectColor = Accents[i];
                outline.effectDistance = new Vector2(2, -2);
                var band = Decoration("EraBand", card.transform, Accents[i]);
                Place(band.rectTransform, new Vector2(0, 117), new Vector2(270, 6));
                var mark = card.transform.Find("EraMark") as RectTransform;
                if (mark == null)
                {
                    mark = Rect("EraMark", card.transform);
                    Place(mark, new Vector2(-112, 84), new Vector2(44, 32));
                    // Graphic mnemonics, not reconstructions of period artefacts.
                    if (i == 0)
                        for (int n = 0; n < 3; n++) Stroke(mark, n, new Vector2(0, 10 - n * 10), new Vector2(40 - n * 8, 3), Accents[i]);
                    else if (i == 1)
                        for (int n = 0; n < 4; n++) Stroke(mark, n, new Vector2(-15 + n * 10, 0), new Vector2(3, 30), Accents[i]);
                    else if (i == 2)
                        for (int n = 0; n < 4; n++) Stroke(mark, n, new Vector2((n % 2) * 20 - 10, (n / 2) * 18 - 9), new Vector2(13, 12), Accents[i]);
                    else if (i == 3)
                        for (int n = 0; n < 3; n++) Stroke(mark, n, new Vector2(-12 + n * 12, -10 + n * 10), new Vector2(13, 5), Accents[i]);
                    else
                    {
                        Stroke(mark, 0, new Vector2(-12, 0), new Vector2(4, 30), Accents[i]);
                        Stroke(mark, 1, new Vector2(12, 0), new Vector2(4, 30), Accents[i]);
                        Stroke(mark, 2, new Vector2(0, 15), new Vector2(28, 4), Accents[i]);
                    }
                }
                var name = card.transform.Find("SchoolName").GetComponent<TMP_Text>();
                name.fontSize = 36;
                Place(name.rectTransform, new Vector2(0, -62), new Vector2(280, 56));
                var gesture = card.transform.Find("Gesture").GetComponent<TMP_Text>();
                gesture.fontSize = 22;
                Place(gesture.rectTransform, new Vector2(0, -104), new Vector2(260, 40));
                gesture.color = Accents[i];
            }
            ApplyLearning(scene, board);
        }

        private static void ApplyLearning(Scene scene, TimelineBoard board)
        {
            var controller = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SharedLevelController>(true)).Single();
            var ui = controller.GetComponentInChildren<SharedLevelUI>(true);
            var panels = ui.GetComponentsInChildren<SharedContentPanel>(true);
            var knowledgePanel = panels.Single(p => p.name == "KnowledgeCard");
            var knowledgeCard = knowledgePanel.transform.Find("Card");
            var font = board.transform.Find("Progress").GetComponent<TMP_Text>().font;
            TextAt("CultureConnection", board.transform, font, TimelineHistory.Stages[4].Connection, 24,
                new Vector2(220, 302), new Vector2(630, 88));
            var body = knowledgeCard.Find("Body").GetComponent<TMP_Text>();
            Place(body.rectTransform, new Vector2(-210, 45), new Vector2(770, 300));
            body.fontSize = 30;
            Place((RectTransform)knowledgeCard.Find("Illustration"), new Vector2(430, 90), new Vector2(300, 250));
            var boardSo = new SerializedObject(board);
            var guide = (Button)boardSo.FindProperty("spaceIntroduction").objectReferenceValue;
            guide.transform.SetParent(knowledgeCard, false);
            Place((RectTransform)guide.transform, new Vector2(430, -90), new Vector2(260, 60));
            Place(guide.GetComponentInChildren<TMP_Text>().rectTransform, Vector2.zero, new Vector2(240, 52));
            TextAt("KnowledgeScope", knowledgeCard, font, TimelineHistory.ScopeNote, 20,
                new Vector2(0, -150), new Vector2(1180, 40));
            var kp = ButtonAt("KnowledgePrevious", knowledgeCard, font, "上一階段", new Vector2(-450, -215), 260);
            var kn = ButtonAt("KnowledgeNext", knowledgeCard, font, "下一階段", new Vector2(-150, -215), 260);
            var kh = ButtonAt("KnowledgeHistorySource", knowledgeCard, font, "校史出處", new Vector2(150, -215), 260);
            var kc = ButtonAt("KnowledgeCultureSource", knowledgeCard, font, "文化出處", new Vector2(450, -215), 260);

            var resultCard = ui.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ResultCard");
            ((RectTransform)resultCard).sizeDelta = new Vector2(1000, 760);
            var resultTitle = resultCard.Find("Title").GetComponent<TMP_Text>();
            Place(resultTitle.rectTransform, new Vector2(0, 330), new Vector2(800, 70));
            resultTitle.fontSize = 44;
            PrefabUtility.RecordPrefabInstancePropertyModifications(resultTitle);
            var resultBody = resultCard.Find("Body").GetComponent<TMP_Text>();
            Place(resultBody.rectTransform, new Vector2(0, 220), new Vector2(880, 150));
            resultBody.fontSize = 24;
            PrefabUtility.RecordPrefabInstancePropertyModifications(resultBody);
            Place((RectTransform)resultCard.Find("Retry"), new Vector2(-210, -295), new Vector2(380, 90));
            Place((RectTransform)resultCard.Find("ContinueToMap"), new Vector2(210, -295), new Vector2(380, 90));
            var browser = resultCard.Find("ResultHistory") as RectTransform;
            if (browser == null) browser = Rect("ResultHistory", resultCard);
            Place(browser, Vector2.zero, new Vector2(1000, 550));
            var heading = TextAt("ResultHistoryTitle", browser, font, "五階段校史摘要", 28,
                new Vector2(0, 115), new Vector2(880, 50));
            var summary = TextAt("ResultHistoryBody", browser, font, "", 25,
                new Vector2(0, -5), new Vector2(880, 200));
            summary.alignment = TextAlignmentOptions.TopLeft;
            TextAt("ResultHistoryScope", browser, font, TimelineHistory.ScopeNote, 16,
                new Vector2(0, -139), new Vector2(880, 34));
            var rp = ButtonAt("ResultPrevious", browser, font, "上一階段", new Vector2(-330, -195), 200);
            var rn = ButtonAt("ResultNext", browser, font, "下一階段", new Vector2(-110, -195), 200);
            var rh = ButtonAt("ResultHistorySource", browser, font, "校史出處", new Vector2(110, -195), 200);
            var rc = ButtonAt("ResultCultureSource", browser, font, "文化出處", new Vector2(330, -195), 200);
            browser.gameObject.SetActive(false);

            var view = board.GetComponent<TimelineHistoryView>() ?? board.gameObject.AddComponent<TimelineHistoryView>();
            Set(view, "controller", controller);
            Set(view, "ui", ui);
            Set(view, "knowledgePanel", knowledgePanel);
            Set(view, "resultBrowser", browser.gameObject);
            Set(view, "resultHeading", heading);
            Set(view, "resultSummary", summary);
            SetArray(view, "cards", board.GetComponentsInChildren<TimelineCard>(true).OrderBy(c => c.TargetIndex).ToArray());
            SetArray(view, "previous", new[] { kp, rp });
            SetArray(view, "next", new[] { kn, rn });
            SetArray(view, "historySources", new[] { kh, rh });
            SetArray(view, "cultureSources", new[] { kc, rc });
            Set(board, "historyView", view);
        }

        private static TMP_Text TextAt(string name, Transform parent, TMP_FontAsset font, string value,
            float size, Vector2 position, Vector2 dimensions)
        {
            var existing = parent.Find(name);
            var text = existing != null ? existing.GetComponent<TMP_Text>() : Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            Place(text.rectTransform, position, dimensions);
            text.font = font;
            text.fontSize = size;
            text.color = new Color32(31, 58, 46, 255);
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Button ButtonAt(string name, Transform parent, TMP_FontAsset font, string label, Vector2 position, float width)
        {
            var image = Decoration(name, parent, Color.white);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Levels/Shared/Sprites/Button_Green.png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            Place(image.rectTransform, position, new Vector2(width, 54));
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = TextAt("Label", image.transform, font, label, 22, Vector2.zero, new Vector2(width - 16, 48));
            text.color = new Color32(251, 248, 242, 255);
            return button;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stroke(Transform parent, int index, Vector2 position, Vector2 size, Color color)
        {
            var stroke = Decoration("Stroke_" + index, parent, color);
            Place(stroke.rectTransform, position, size);
        }

        private static Image Decoration(string name, Transform parent, Color color)
        {
            var existing = parent.Find(name);
            var image = existing != null ? existing.GetComponent<Image>() : Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
