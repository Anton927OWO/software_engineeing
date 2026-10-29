using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale.Editor
{
    // Explicit, local scene upgrades. Never apply modifications to the source Shared prefab.
    public static class FinaleScenePolish
    {
        public static void UpgradeInventory() => EditScene(ConfigureInventory);

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
            Place(instructions.rectTransform, 0, 90, 390, 140);
            var summaryTransform = side.Find("AcquiredSummary");
            var summary = summaryTransform != null ? summaryTransform.GetComponent<TMP_Text>() :
                UnityEngine.Object.Instantiate(instructions, side);
            summary.name = "AcquiredSummary";
            summary.text = "本局文物\n開始後顯示本局的三件文物";
            summary.fontSize = 22;
            summary.alignment = TextAlignmentOptions.TopLeft;
            Place(summary.rectTransform, 0, -45, 390, 132);
            var finale = root.GetComponentInChildren<FinaleLevelController>(true);
            Bind(finale, "acquiredSummary", summary);
            Bind(finale, "hintTitle", safe.Find("FinaleView/AcquiredArtifactHint/HintCard/Title").GetComponent<TMP_Text>());
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
