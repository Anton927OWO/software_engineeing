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
        public static void UpgradeGallery() => EditScene(ConfigureGallery);

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

        public static void ConfigureGallery(GameObject root)
        {
            var safe = root.GetComponentInChildren<Canvas>(true).transform.Find("SafeArea");
            var card = safe.Find("ResultOverlay/ResultCard");
            if (card.Find("ArtifactGallery") != null) return;
            var template = safe.Find("FinaleView/GuideCard/AcquiredSummary").GetComponent<TMP_Text>();
            var galleryRoot = new GameObject("ArtifactGallery", typeof(RectTransform));
            galleryRoot.layer = 5;
            galleryRoot.transform.SetParent(card, false);
            Place((RectTransform)galleryRoot.transform, 0, -50, 1000, 640);
            var gallery = galleryRoot.AddComponent<FinaleArtifactGallery>();
            var heading = GalleryText("Heading", galleryRoot.transform, template, "文物圖鑑｜點選六件文物，查看知識與來源", 26);
            Place(heading.rectTransform, 0, 240, 1000, 44);
            var buttons = new Button[6]; var images = new Image[6]; var labels = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var background = CreateImage("Artifact" + i, galleryRoot.transform, new Color32(247, 241, 226, 255));
                background.raycastTarget = true;
                Place(background.rectTransform, (i - 2.5f) * 164, 120, 148, 170);
                buttons[i] = background.gameObject.AddComponent<Button>(); buttons[i].targetGraphic = background;
                images[i] = CreateImage("Icon", background.transform, Color.white);
                images[i].preserveAspect = true;
                Place(images[i].rectTransform, 0, 32, 100, 88);
                labels[i] = GalleryText("Label", background.transform, template, "文物", 21);
                Place(labels[i].rectTransform, 0, -45, 146, 64);
            }
            var selectedIcon = CreateImage("SelectedIcon", galleryRoot.transform, Color.white);
            selectedIcon.preserveAspect = true;
            Place(selectedIcon.rectTransform, -370, -125, 210, 210);
            var title = GalleryText("SelectedTitle", galleryRoot.transform, template, "文物", 26);
            title.alignment = TextAlignmentOptions.Left;
            Place(title.rectTransform, 120, 10, 700, 48);
            var body = GalleryText("Description", galleryRoot.transform, template, "", 22);
            body.alignment = TextAlignmentOptions.TopLeft;
            Place(body.rectTransform, 120, -140, 700, 236);
            var artifactSource = GallerySource("ArtifactSource", galleryRoot.transform, template, "開啟文物來源（官方）", -240);
            var contextSource = GallerySource("ContextSource", galleryRoot.transform, template, "開啟校史與文化來源（官方）", 240);
            Bind(gallery, "selectedIcon", selectedIcon); Bind(gallery, "selectedTitle", title); Bind(gallery, "description", body);
            Bind(gallery, "artifactSourceButton", artifactSource); Bind(gallery, "contextSourceButton", contextSource);
            Bind(gallery, "resultCard", card); Bind(gallery, "resultTitle", card.Find("Title").GetComponent<TMP_Text>());
            Bind(gallery, "resultBody", card.Find("Body").GetComponent<TMP_Text>());
            Bind(gallery, "retry", card.Find("Retry")); Bind(gallery, "continueButton", card.Find("ContinueToMap"));
            BindArray(gallery, "entries", buttons); BindArray(gallery, "icons", images); BindArray(gallery, "labels", labels);
            Bind(root.GetComponentInChildren<FinaleLevelController>(true), "gallery", gallery);
            galleryRoot.SetActive(false);
        }

        private static TMP_Text GalleryText(string name, Transform parent, TMP_Text template, string value, float size)
        {
            var text = UnityEngine.Object.Instantiate(template, parent);
            text.name = name; text.text = value; text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        private static Button GallerySource(string name, Transform parent, TMP_Text template, string value, float x)
        {
            var background = CreateImage(name, parent, new Color32(31, 58, 46, 255));
            background.raycastTarget = true;
            Place(background.rectTransform, x, -295, 440, 44);
            var button = background.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var label = GalleryText("Label", background.transform, template, value, 22);
            label.color = new Color32(251, 248, 242, 255);
            Place(label.rectTransform, 0, 0, 432, 44);
            return button;
        }

        private static void BindArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var data = new SerializedObject(target); var array = data.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
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
