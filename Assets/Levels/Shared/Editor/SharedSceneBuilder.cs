using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace XunJiYiDa.Levels.Shared.Editor
{
    /// <summary>Safe additive authoring framework. Does not edit Build Settings or an open scene.</summary>
    public static class SharedSceneBuilder
    {
        public static void CreateFoundationIfMissing()
        {
            if (File.Exists(SharedAssetPaths.Scene)) return;
            CreateScene(SharedAssetPaths.Scene, scene =>
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedAssetPaths.Prefab);
                if (prefab == null) throw new InvalidOperationException("Generate the shared prefab first.");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                AddPreview(root);
            });
        }

        /// <summary>Future level generators supply a population callback. Existing files are never overwritten.</summary>
        public static void CreateScene(string path, Action<Scene> populate)
        {
            SharedAssetPaths.RequireSharedPath(path);
            if (!path.EndsWith(".unity", StringComparison.Ordinal)) throw new ArgumentException("Expected a .unity path.", nameof(path));
            if (File.Exists(path)) throw new IOException("Refusing to overwrite an existing scene: " + path);
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            SharedAssetPaths.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var previous = SceneManager.GetActiveScene();
            // Unity rejects additive creation while an untitled scene is loaded. Batch mode starts
            // with a pristine empty scene; replace only that disposable scene and restore it below.
            bool replaceEmptyScene = SceneManager.sceneCount == 1 && previous.IsValid() &&
                string.IsNullOrEmpty(previous.path) && !previous.isDirty && previous.rootCount == 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path) && !replaceEmptyScene)
                    throw new InvalidOperationException("Save untitled scenes before generating. Your open scenes have been preserved.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceEmptyScene ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                CreateCamera(scene);
                CreateEventSystem(scene);
                populate?.Invoke(scene);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save scene: " + path);
            }
            finally
            {
                if (replaceEmptyScene) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                {
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void CreateCamera(Scene scene)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(251, 248, 242, 255);
        }

        private static void CreateEventSystem(Scene scene)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void AddPreview(GameObject root)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedAssetPaths.Font);
            var canvas = root.GetComponentInChildren<Canvas>();
            var previewRect = SharedPrefabFactory.Rect("FoundationPreview", canvas.transform);
            SharedPrefabFactory.Stretch(previewRect);
            previewRect.SetAsFirstSibling();
            var title = SharedPrefabFactory.Text("Title", previewRect, font, "共用基礎驗證", 64);
            SharedPrefabFactory.Place(title.rectTransform, new Vector2(0, 205), new Vector2(1500, 110));
            var body = SharedPrefabFactory.Text("Description", previewRect, font,
                "此場景僅驗證共用系統，不包含三個關卡的玩法。\n使用下方按鈕檢查介紹、內容面板與探索結果。", 28);
            SharedPrefabFactory.Place(body.rectTransform, new Vector2(0, 85), new Vector2(1540, 130));
            var component = previewRect.gameObject.AddComponent<SharedFoundationPreview>();
            SharedPrefabFactory.Set(component, "controller", root.GetComponent<SharedLevelController>());
            SharedPrefabFactory.Set(component, "ui", root.GetComponentInChildren<SharedLevelUI>());
            var fields = new[] { "introduction", "minigame", "knowledge", "mistake", "success", "failure" };
            var labels = new[] { "認識空間", "開啟小遊戲", "閱讀知識卡", "記錄失誤", "成功結算", "失敗結算" };
            for (int i = 0; i < fields.Length; i++)
            {
                var button = SharedPrefabFactory.Button(fields[i], previewRect, font, labels[i],
                    new Vector2((i % 3 - 1) * 430, -80 - (i / 3) * 150), new Vector2(365, 96), i == 3 || i == 5);
                SharedPrefabFactory.Set(component, fields[i], button);
            }
        }
    }
}
