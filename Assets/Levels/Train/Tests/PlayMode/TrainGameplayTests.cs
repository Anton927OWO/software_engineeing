using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using Object = UnityEngine.Object;

namespace XunJiYiDa.Levels.Train.Tests
{
    public sealed class TrainGameplayTests
    {
        private const string ScenePath = "Assets/Levels/Train/L_Train.unity";
        private TrainLevelController train;
        private SharedLevelController shared;
        private Scene scene;
        private float previousScale;

        [UnitySetUp]
        public IEnumerator LoadNativeScene()
        {
            previousScale = Time.timeScale;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync("L_Train", LoadSceneMode.Additive);
#endif
            scene = SceneManager.GetSceneByPath(ScenePath);
            train = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TrainLevelController>(true)).Single();
            shared = train.Shared;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadNativeScene()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousScale;
        }

        private T Find<T>(string name) where T : Component => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).Single(c => c.name == name);
        private SharedContentPanel Panel(string name) => Find<SharedContentPanel>(name);
        private static void Click(Button button)
        {
            Assert.That(button.gameObject.activeInHierarchy && button.interactable, Is.True, button.name);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        private void Begin() => Click(Panel("Introduction").PrimaryButton);

        [UnityTest]
        public IEnumerator IntroductionKnowledgePauseAndRetryUseSharedUIAndFreezeTime()
        {
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            float initial = shared.Session.RemainingTime;
            Capture("introduction");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(initial));
            Begin(); yield return null;
            Assert.That(shared.CanInteract, Is.True);
            Capture("question");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(shared.Session.RemainingTime, Is.LessThan(initial));
            Click(Find<Button>("Knowledge")); yield return null;
            float paused = shared.Session.RemainingTime;
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Paused));
            Capture("knowledge");
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(paused));
            shared.Pause();
            Panel("KnowledgeCard").PrimaryButton.onClick.Invoke();
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Paused));
            shared.Resume(); yield return null;
            Click(Find<Button>("SpaceIntroduction")); yield return null;
            Click(Panel("Introduction").PrimaryButton); yield return null;
            Click(Find<Button>("Choice" + train.Round.Current.correctIndex));
            Assert.That(train.Round.Carriages, Is.EqualTo(1));
            shared.Pause(); yield return null;
            Capture("pause");
            var pause = Find<Transform>("PauseOverlay");
            Click(pause.GetComponentsInChildren<Button>().Single(b => b.name == "Retry"));
            yield return null;
            Assert.That(train.Round.Carriages, Is.Zero);
            Assert.That(train.Round.AnsweredCount, Is.Zero);
            Assert.That(shared.Session.ArtifactIds, Is.Empty);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(60));
            Assert.That(Panel("Introduction").gameObject.activeSelf, Is.True);
            for (int i = 1; i <= 5; i++) Assert.That(Find<RectTransform>("Carriage" + i).gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FiveCorrectButtonAnswersAnimateOutThenRelayOneSuccess()
        {
            Begin(); yield return null;
            int completed = 0; shared.Completed += _ => completed++;
            var root = Find<RectTransform>("Train");
            for (int i = 0; i < 5; i++)
            {
                Click(Find<Button>("Choice" + train.Round.Current.correctIndex));
                Assert.That(shared.Session.Result, Is.Null);
                Assert.That(train.Round.Carriages, Is.EqualTo(i + 1));
                if (i == 0)
                {
                    Capture("coupling");
                    shared.Pause(); float progress = train.Round.PhaseProgress;
                    yield return new WaitForSecondsRealtime(0.15f);
                    Assert.That(train.Round.PhaseProgress, Is.EqualTo(progress));
                    shared.Resume();
                }
                float guard = Time.realtimeSinceStartup + 3;
                while (train.Round.Phase == TrainPhase.Coupling && Time.realtimeSinceStartup < guard) yield return null;
                Assert.That(train.Round.Phase, Is.EqualTo(i == 4 ? TrainPhase.Departing : TrainPhase.Asking));
            }
            Capture("five-carriages");
            Assert.That(shared.Session.Result, Is.Null);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(root.anchoredPosition.x, Is.LessThan(-50));
            shared.Pause(); float position = root.anchoredPosition.x;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(root.anchoredPosition.x, Is.EqualTo(position)); shared.Resume();
            float deadline = Time.realtimeSinceStartup + 4;
            while (shared.Session.Result == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(shared.Session.Result, Is.Not.Null);
            Assert.That(shared.Session.Result.Success, Is.True);
            Assert.That(root.anchoredPosition.x, Is.LessThan(-2200));
            Assert.That(completed, Is.EqualTo(1));
            var relay = shared.GetComponent<LevelResultRelay>();
            Assert.That(relay.LastResultJson, Does.Contain("\"levelId\":\"Train\""));
            Assert.That(relay.LastResultJson, Does.Contain("\"success\":true"));
            Assert.That(relay.LastResult.ArtifactIds.Count, Is.EqualTo(5));
            Capture("success");
        }

        [UnityTest]
        public IEnumerator WrongAnswerShowsCorrectAnswerForASecondAndKnowledgeIncludesSource()
        {
            Begin(); yield return null;
            var q = train.Round.Current;
            Click(Find<Button>("Choice" + ((q.correctIndex + 1) % 3)));
            Capture("wrong-answer");
            Assert.That(Find<TMP_Text>("Feedback").text, Does.Contain(q.options[q.correctIndex]));
            Assert.That(shared.Session.MistakeCount, Is.EqualTo(1));
            train.SubmitAnswer(q.correctIndex);
            Assert.That(train.Round.Carriages, Is.Zero);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(train.Round.Current.id, Is.EqualTo(q.id));
            Click(Find<Button>("Knowledge")); yield return null;
            var body = Panel("KnowledgeCard").transform.Find("Card/Body").GetComponent<TMP_Text>().text;
            Assert.That(body, Does.Contain(q.sourceUrl)); Assert.That(body, Does.Contain(q.options[q.correctIndex]));
            Capture("knowledge-answer");
            float phase = train.Round.PhaseProgress;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(train.Round.PhaseProgress, Is.EqualTo(phase));
            Click(Panel("KnowledgeCard").PrimaryButton);
            float guard = Time.realtimeSinceStartup + 2;
            while (train.Round.Phase != TrainPhase.Asking && Time.realtimeSinceStartup < guard) yield return null;
            Assert.That(train.Round.Current.id, Is.Not.EqualTo(q.id));
        }

        [UnityTest]
        public IEnumerator TimeoutRelaysFailureAndResultRetryResetsEverything()
        {
            Begin(); yield return null;
            var q = train.Round.Current;
            Click(Find<Button>("Choice" + ((q.correctIndex + 1) % 3)));
            shared.Session.Tick(60); yield return null;
            var relay = shared.GetComponent<LevelResultRelay>();
            Assert.That(relay.LastResult.Success, Is.False);
            Assert.That(relay.LastResult.RemainingTime, Is.Zero);
            Assert.That(relay.LastResult.MistakeCount, Is.EqualTo(1));
            Capture("failure");
            var result = Find<Transform>("ResultOverlay");
            Click(result.GetComponentsInChildren<Button>().Single(b => b.name == "Retry")); yield return null;
            Assert.That(relay.LastResultJson, Is.Empty);
            Assert.That(train.Round.AnsweredCount, Is.Zero);
            Assert.That(shared.Session.MistakeCount, Is.Zero);
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
        }

        [UnityTest]
        public IEnumerator ExhaustingEveryQuestionThroughButtonsReportsFailure()
        {
            Begin(); yield return null;
            var ids = new System.Collections.Generic.HashSet<string>();
            int count = train.Round.QuestionCount;
            for (int i = 0; i < count; i++)
            {
                var q = train.Round.Current;
                Assert.That(ids.Add(q.id), Is.True);
                Click(Find<Button>("Choice" + ((q.correctIndex + 1) % 3)));
                // Boundary logic is covered with exact deltas in EditMode; this exercises scene wiring.
                train.Round.Advance(1);
                yield return null;
            }
            var result = shared.GetComponent<LevelResultRelay>().LastResult;
            Assert.That(result.Success, Is.False);
            Assert.That(result.MistakeCount, Is.EqualTo(count));
            Assert.That(result.RemainingTime, Is.GreaterThan(0));
            Assert.That(result.ArtifactIds, Is.Empty);
        }

        private void Capture(string name)
        {
            string directory = Environment.GetEnvironmentVariable("NIU_TRAIN_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Directory.CreateDirectory(directory);
            var camera = Find<Camera>("Main Camera");
            var canvases = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).ToArray();
            var modes = canvases.Select(c => c.renderMode).ToArray();
            var cameras = canvases.Select(c => c.worldCamera).ToArray();
            var distances = canvases.Select(c => c.planeDistance).ToArray();
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in canvases)
                { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                camera.targetTexture = target; Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive; camera.targetTexture = oldTarget;
                for (int i = 0; i < canvases.Length; i++)
                { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
        }
    }
}
