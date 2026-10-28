using System;
using System.IO;
using System.Linq;
using TMPro;
using TrackingNIU;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Integration.Editor
{
    public static class TrackingIntegrationSetup
    {
        private const string PrefabPath = "Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab";

        [MenuItem("尋跡宜大/整合/準備已存在的關卡與 Build Settings")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            bool disposable = SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(SceneManager.GetSceneAt(0).path) &&
                !SceneManager.GetSceneAt(0).isDirty && SceneManager.GetSceneAt(0).rootCount == 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isDirty || (string.IsNullOrEmpty(open.path) && !disposable))
                    throw new InvalidOperationException("Save all open scenes before preparing integration. No scene or prefab was changed.");
            }
            PreparePrefab();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var id in new[] { LevelId.Train, LevelId.Timeline, LevelId.Finale })
                {
                    string path = $"Assets/Levels/{id}/L_{id}.unity";
                    if (!File.Exists(path)) continue;
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var root = scene.GetRootGameObjects().Single(g => g.GetComponent<SharedLevelController>() != null);
                    var launcher = root.GetComponent<LevelTestLauncher>() ?? root.AddComponent<LevelTestLauncher>();
                    var so = new SerializedObject(launcher);
                    var level = so.FindProperty("levelId");
                    level.enumValueIndex = Array.IndexOf(level.enumNames, id.ToString());
                    var artifacts = so.FindProperty("collectedArtifacts");
                    artifacts.arraySize = 3;
                    var ids = new[] { ArtifactId.GoldenCarp, ArtifactId.PotteryJar, ArtifactId.AgateBead };
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var entry = artifacts.GetArrayElementAtIndex(i);
                        entry.enumValueIndex = Array.IndexOf(entry.enumNames, ids[i].ToString());
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    // New prefab UI must use each scene's existing level-specific font.
                    var ui = root.GetComponentInChildren<SharedLevelUI>(true);
                    var font = root.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "LevelName").font;
                    var button = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "ContinueToMap");
                    foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        text.font = font;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    }
                    EditorSceneManager.SaveScene(scene);
                    var scenes = EditorBuildSettings.scenes.ToList();
                    if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
            }
            finally
            {
                if (setup.Any(s => s.isLoaded && !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void PreparePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<TrackingLevelBridge>() == null) root.AddComponent<TrackingLevelBridge>();
                var ui = root.GetComponentInChildren<SharedLevelUI>(true);
                var so = new SerializedObject(ui);
                var retry = (Button)so.FindProperty("resultRetryButton").objectReferenceValue;
                var parent = (RectTransform)retry.transform.parent;
                parent.sizeDelta = new Vector2(1000, 760);
                ((RectTransform)retry.transform).anchoredPosition = new Vector2(-210, -245);
                var title = (TMP_Text)so.FindProperty("resultTitle").objectReferenceValue;
                title.rectTransform.anchoredPosition = new Vector2(0, 245);
                var body = (TMP_Text)so.FindProperty("resultBody").objectReferenceValue;
                body.rectTransform.sizeDelta = new Vector2(840, 310);
                body.fontSize = 30;
                var next = parent.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == "ContinueToMap");
                if (next == null)
                {
                    next = UnityEngine.Object.Instantiate(retry, parent);
                    next.name = "ContinueToMap";
                    next.onClick = new Button.ButtonClickedEvent();
                }
                ((RectTransform)next.transform).anchoredPosition = new Vector2(210, -245);
                next.GetComponentInChildren<TMP_Text>(true).text = "繼續探索";
                next.gameObject.SetActive(false);
                so.FindProperty("resultContinueButton").objectReferenceValue = next;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
