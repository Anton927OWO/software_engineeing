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
        public const string IntroductionBody = "找出宜大學制變化的五個節點。\n\n45 秒內放對五張卡即可通關；錯放可再試，並記錄失誤。\n開啟「年代提示」可查閱校史；閱讀與暫停期間不扣時間。";
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
            root.Find("SpaceIntroduction/Label").GetComponent<TMP_Text>().text = "操作說明";
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
