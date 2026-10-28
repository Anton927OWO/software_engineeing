using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace XunJiYiDa.Levels.Shared.Tests
{
    public sealed class SharedLifecycleTests
    {
#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator FoundationSceneSupportsKnowledgePauseAndRetry()
        {
            float previousTime = Time.timeScale;
            var previousScene = SceneManager.GetActiveScene();
            const string path = "Assets/Levels/Shared/Scenes/SharedFoundation.unity";
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = SceneManager.GetSceneByPath(path);
            try
            {
                SceneManager.SetActiveScene(scene);
                yield return null;
                var root = scene.GetRootGameObjects().Single(g => g.GetComponent<SharedLevelController>() != null);
                var controller = root.GetComponent<SharedLevelController>();
                var ui = root.GetComponentInChildren<SharedLevelUI>();
                var intro = root.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction");
                Assert.That(intro.gameObject.activeInHierarchy, Is.True);
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("introduction");
                yield return null;
                yield return null;
                intro.PrimaryButton.onClick.Invoke();
                Assert.That(controller.CanInteract, Is.True);
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("hud");
                yield return null;
                yield return null;
                ui.ShowKnowledgeCard("繁體中文知識卡", "宜蘭大學的探索故事。\n閱讀期間暫停倒數。");
                float remaining = controller.Session.RemainingTime;
                yield return null;
                Assert.That(controller.Session.RemainingTime, Is.EqualTo(remaining));
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("knowledge");
                yield return null;
                yield return null;
                controller.Pause();
                ui.CloseContent();
                Assert.That(controller.Session.State, Is.EqualTo(LevelState.Paused));
                Assert.That(root.GetComponentsInChildren<Button>(true).Single(b => b.name == "Pause").navigation.mode,
                    Is.EqualTo(Navigation.Mode.None));
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("pause");
                yield return null;
                yield return null;
                controller.Resume();
                controller.RecordMistake();
                controller.Fail();
                Assert.That(root.GetComponent<LevelResultRelay>().LastResult.Success, Is.False);
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("failure");
                yield return null;
                yield return null;
                controller.Retry();
                Assert.That(controller.Session.MistakeCount, Is.Zero);
                Assert.That(intro.gameObject.activeInHierarchy, Is.True);
                intro.PrimaryButton.onClick.Invoke();
                ui.ShowMinigame("小遊戲容器", "此處掛載關卡自己的互動內容。\n關閉面板後繼續探索。");
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("minigame");
                yield return null;
                yield return null;
                ui.CloseContent();
                controller.CollectArtifact("validation-artifact");
                controller.Succeed();
                Assert.That(root.GetComponent<LevelResultRelay>().LastResult.Success, Is.True);
                yield return new WaitForSecondsRealtime(0.15f);
                Capture("success");
                yield return null;
                yield return null;
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
                if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                Time.timeScale = previousTime;
            }
            yield return null;
        }

        private static void Capture(string name)
        {
            string directory = System.Environment.GetEnvironmentVariable("NIU_SHARED_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            System.IO.Directory.CreateDirectory(directory);
            var camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Single(c => c.gameObject.scene.path == "Assets/Levels/Shared/Scenes/SharedFoundation.unity");
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Single(c => c.gameObject.scene == camera.gameObject.scene);
            var mode = canvas.renderMode;
            var worldCamera = canvas.worldCamera;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target };
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                canvas.renderMode = mode;
                canvas.worldCamera = worldCamera;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        [UnityTest]
        public IEnumerator ReenablingRootKeepsIntroductionAndItsStartAction()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab");
            var root = Object.Instantiate(prefab);
            float previous = 1;
            try
            {
                var controller = root.GetComponent<SharedLevelController>();
                yield return null;
                var intro = root.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction");
                Assert.That(intro.gameObject.activeSelf, Is.True);
                root.SetActive(false);
                previous = Time.timeScale;
                root.SetActive(true);
                Assert.That(intro.gameObject.activeInHierarchy, Is.True);
                intro.PrimaryButton.onClick.Invoke();
                Assert.That(controller.Session.State, Is.EqualTo(LevelState.Running));
                Assert.That(Time.timeScale, Is.EqualTo(previous));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Time.timeScale = previous;
            }
        }

        [UnityTest]
        public IEnumerator MinigameTrapsNavigationWhileItsClockContinuesAndRetryClearsResult()
        {
            float previous = Time.timeScale;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab");
            var root = Object.Instantiate(prefab);
            GameObject background = null;
            try
            {
                yield return null;
                var controller = root.GetComponent<SharedLevelController>();
                var ui = root.GetComponentInChildren<SharedLevelUI>();
                var panels = root.GetComponentsInChildren<SharedContentPanel>(true);
                panels.Single(p => p.name == "Introduction").PrimaryButton.onClick.Invoke();
                background = new GameObject("Underlying action", typeof(RectTransform), typeof(Image), typeof(Button));
                background.transform.SetParent(root.GetComponentInChildren<Canvas>().transform, false);
                var underlying = background.GetComponent<Button>();
                var originalNavigation = underlying.navigation.mode;
                ui.ShowMinigame("小遊戲", "共用容器");
                Assert.That(underlying.navigation.mode, Is.EqualTo(Navigation.Mode.None));
                Assert.That(controller.Session.State, Is.EqualTo(LevelState.Running));
                float before = controller.Session.RemainingTime;
                yield return null;
                Assert.That(controller.Session.RemainingTime, Is.LessThan(before));
                panels.Single(p => p.name == "Minigame").PrimaryButton.onClick.Invoke();
                Assert.That(underlying.navigation.mode, Is.EqualTo(originalNavigation));
                controller.CollectArtifact("preview-artifact");
                controller.RecordMistake();
                controller.Succeed();
                var relay = root.GetComponent<LevelResultRelay>();
                Assert.That(relay.LastResultJson, Does.Contain("\"success\":true"));
                Assert.That(relay.LastResultJson, Does.Contain("\"artifactIds\":[\"preview-artifact\"]"));
                Assert.That(relay.LastResult.MistakeCount, Is.EqualTo(1));
                controller.Retry();
                Assert.That(relay.LastResult, Is.Null);
                Assert.That(relay.LastResultJson, Is.Empty);
                Assert.That(controller.Session.MistakeCount, Is.Zero);
                Assert.That(controller.Session.ArtifactIds, Is.Empty);
                Assert.That(panels.Single(p => p.name == "Introduction").gameObject.activeSelf, Is.True);
            }
            finally
            {
                if (background != null) Object.DestroyImmediate(background);
                Object.DestroyImmediate(root);
                Time.timeScale = previous;
            }
        }
#endif

        [UnityTest]
        public IEnumerator DisablingControllerRestoresPreviousWorldTimeScale()
        {
            float original = Time.timeScale;
            GameObject go = null;
            try
            {
                Time.timeScale = 0.5f;
                go = new GameObject("Lifecycle test");
                var controller = go.AddComponent<SharedLevelController>();
                Assert.That(Time.timeScale, Is.Zero);
                controller.Begin();
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                controller.Pause();
                Assert.That(Time.timeScale, Is.Zero);
                controller.enabled = false;
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                yield return null;
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                Time.timeScale = original;
            }
        }
    }
}
