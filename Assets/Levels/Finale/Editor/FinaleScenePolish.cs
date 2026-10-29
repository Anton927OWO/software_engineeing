using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale.Editor
{
    // Explicit, local scene upgrades. Never apply modifications to the source Shared prefab.
    public static class FinaleScenePolish
    {
        public static void UpgradeInventory() => EditScene(ConfigureInventory);
        public static void UpgradeLearning() => EditScene(ConfigureLearning);

        private static void EditScene(Action<GameObject> edit)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var scene = EditorSceneManager.OpenScene(FinaleSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponent<SharedLevelController>() != null) edit(root);
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        public static void ConfigureInventory(GameObject root)
        {
            var safe = root.GetComponentInChildren<Canvas>(true).transform.Find("SafeArea");
            var side = safe.Find("FinaleView/GuideCard");
            var instructions = side.Find("Instructions").GetComponent<TMP_Text>();
            instructions.text = "每次翻兩張，找出相同文物。\n金框表示配對成功。\n失配後記住兩張的位置。";
            instructions.fontSize = 24;
            instructions.alignment = TextAlignmentOptions.TopLeft;
            Place(instructions.rectTransform, 0, 100, 390, 140);
            var summaryTransform = side.Find("AcquiredSummary");
            var summary = summaryTransform != null ? summaryTransform.GetComponent<TMP_Text>() :
                UnityEngine.Object.Instantiate(instructions, side);
            summary.name = "AcquiredSummary";
            summary.text = "本局文物\n開始後顯示本局的三件文物";
            summary.fontSize = 22;
            summary.alignment = TextAlignmentOptions.TopLeft;
            Place(summary.rectTransform, 0, -40, 390, 132);
            var finale = root.GetComponentInChildren<FinaleLevelController>(true);
            Bind(finale, "acquiredSummary", summary);
            Bind(finale, "hintTitle", safe.Find("FinaleView/AcquiredArtifactHint/HintCard/Title").GetComponent<TMP_Text>());
        }

        public static void ConfigureLearning(GameObject root)
        {
            var safe = root.GetComponentInChildren<Canvas>(true).transform.Find("SafeArea");
            var side = safe.Find("FinaleView/GuideCard");
            var title = side.Find("Title").GetComponent<TMP_Text>();
            title.text = "90 秒，找回\n六組文物記憶";
            title.alignment = TextAlignmentOptions.Left;
            side.Find("SpaceIntroduction").gameObject.SetActive(false);
            var progress = side.Find("Progress").GetComponent<TMP_Text>();
            progress.fontSize = 28;
            Place(progress.rectTransform, 0, -135, 390, 50);
            Place((RectTransform)side.Find("AcquiredSummary"), 0, -40, 390, 132);
            Place(side.Find("Instructions").GetComponent<RectTransform>(), 0, 100, 390, 140);
            var noteTransform = side.Find("LearningNote");
            var note = noteTransform != null ? noteTransform.GetComponent<TMP_Text>() :
                UnityEngine.Object.Instantiate(side.Find("AcquiredSummary").GetComponent<TMP_Text>(), side);
            note.name = "LearningNote";
            note.text = "邊配對，邊認識文物\n配對後在這裡留下新發現。\n圖像為示意圖。";
            note.fontSize = 22;
            Place(note.rectTransform, 0, -255, 390, 174);
            Bind(root.GetComponentInChildren<FinaleLevelController>(true), "learningNote", note);
            Place((RectTransform)side.Find("Knowledge"), 0, -372, 370, 44);
            side.Find("Knowledge/Label").GetComponent<TMP_Text>().fontSize = 24;
            side.Find("Knowledge/Label").GetComponent<TMP_Text>().text = "查看文物與來源";
            var feedback = safe.Find("FinaleView/Feedback").GetComponent<TMP_Text>();
            feedback.fontSize = 24;
            feedback.alignment = TextAlignmentOptions.Left;
            Place(feedback.rectTransform, 250, -496, 1040, 78);
            foreach (var card in root.GetComponentsInChildren<FinaleCardView>(true))
            {
                var visual = card.transform.Find("Visual");
                var frame = visual.Find("MatchedGoldFrame").GetComponent<Image>();
                frame.color = new Color32(226, 177, 69, 255);
                frame.rectTransform.offsetMin = new Vector2(-8, -8);
                frame.rectTransform.offsetMax = new Vector2(8, 8);
                var badgeTransform = visual.Find("MatchedBadge");
                var badge = badgeTransform != null ? badgeTransform.GetComponent<Image>() :
                    CreateImage("MatchedBadge", visual, frame.color);
                Place(badge.rectTransform, 0, 103, 184, 36);
                var label = visual.Find("Matched").GetComponent<TMP_Text>();
                badge.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
                label.fontStyle = FontStyles.Bold;
                label.color = new Color32(31, 58, 46, 255);
                Place(label.rectTransform, 0, 103, 184, 36);
                Bind(card, "matchedBadge", badge.gameObject);
                badge.gameObject.SetActive(false);
            }
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }

        private static void Bind(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
