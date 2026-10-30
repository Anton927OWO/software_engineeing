using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale.Tests
{
    public sealed class FinaleGameplayTests
    {
#if UNITY_EDITOR
        private Scene scene;
        private Scene previousScene;
        private float previousScale;
        private GameObject root;
        private SharedLevelController shared;
        private FinaleLevelController finale;
        private LevelResultRelay relay;
        private int completed;

        [UnitySetUp]
        public IEnumerator LoadRealFinaleScene()
        {
            previousScene = SceneManager.GetActiveScene(); previousScale = Time.timeScale;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Levels/Finale/L_Finale.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath("Assets/Levels/Finale/L_Finale.unity");
            SceneManager.SetActiveScene(scene);
            yield return null;
            root = scene.GetRootGameObjects().Single(go => go.GetComponent<SharedLevelController>() != null);
            shared = root.GetComponent<SharedLevelController>();
            finale = root.GetComponentInChildren<FinaleLevelController>();
            relay = root.GetComponent<LevelResultRelay>(); completed = 0;
            shared.Completed += CountResult;
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
        }

        [UnityTearDown]
        public IEnumerator UnloadOnlyTestScene()
        {
            if (shared != null) shared.Completed -= CountResult;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = previousScale;
        }

        private void CountResult(LevelResult result) => completed++;
        private Transform Safe => root.GetComponentInChildren<Canvas>().transform.Find("SafeArea");
        private SharedContentPanel Panel(string name) => Safe.Find(name).GetComponent<SharedContentPanel>();
        private Button Control(string path) => Safe.Find(path).GetComponent<Button>();

        private void ConfirmIntroduction() => Panel("Introduction").PrimaryButton.onClick.Invoke();

        private IEnumerator WaitUntilIdle()
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (finale.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(finale.IsBusy, Is.False, "Hint/animation must finish without a stuck input lock.");
        }

        private IEnumerator Begin()
        {
            ConfirmIntroduction();
            Assert.That(finale.IsShowingHint, Is.True);
            yield return WaitUntilIdle();
            Assert.That(shared.CanInteract, Is.True);
        }

        private IEnumerator Flip(int index)
        {
            Assert.That(finale.TryFlip(index), Is.True);
            yield return WaitUntilIdle();
        }

        private int DifferentFrom(int index) => Enumerable.Range(0, 12)
            .First(i => finale.Board.ArtifactAt(i) != finale.Board.ArtifactAt(index));

        [UnityTest]
        public IEnumerator StandaloneHintAndInventoryClearlyMarkTestData()
        {
            ConfirmIntroduction();
            Assert.That(Safe.Find("FinaleView/AcquiredArtifactHint/HintCard/Title").GetComponent<TMP_Text>().text,
                Does.Contain("測試資料"));
            for (int i = 0; i < 3; i++)
                Assert.That(Safe.Find("FinaleView/AcquiredArtifactHint/HintCard/ArtifactName" + i).GetComponent<TMP_Text>().text,
                    Does.Contain("測試"));
            yield return WaitUntilIdle();
            var summary = Safe.Find("FinaleView/GuideCard/AcquiredSummary");
            Assert.That(summary, Is.Not.Null, "The three collected artifacts must remain readable after the short hint.");
            string text = summary.GetComponent<TMP_Text>().text;
            Assert.That(text, Does.Contain("測試資料").And.Contain("金鯉魚").And.Contain("幾何印紋陶罐").And.Contain("瑪瑙珠"));
            AssertVisibleTextFits();
        }

        [UnityTest]
        public IEnumerator MatchingTeachesWithoutPausingOrBlockingTheNextFlip()
        {
            yield return Begin();
            var pair = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == 0).ToArray();
            yield return Flip(pair[0]); yield return Flip(pair[1]);
            var note = Safe.Find("FinaleView/GuideCard/LearningNote");
            Assert.That(note, Is.Not.Null, "Matched artifacts need a nonmodal knowledge sentence.");
            var text = note.GetComponent<TMP_Text>();
            Assert.That(text.text, Does.Contain("銅線").And.Contain("已核對").And.Contain("宜大記憶庫"));
            Assert.That(shared.CanInteract, Is.True);
            Assert.That(Panel("KnowledgeCard").gameObject.activeSelf, Is.False);
            string learned = text.text;
            int next = Enumerable.Range(0, 12).First(i => !finale.Board.IsFaceUp(i));
            Assert.That(finale.TryFlip(next), Is.True);
            Assert.That(text.text, Is.EqualTo(learned), "The next input must not erase the fact before it can be read.");
            yield return WaitUntilIdle();
            AssertVisibleTextFits();
        }

        [UnityTest]
        public IEnumerator MismatchGivesSpecificComparisonCuesWithoutChangingTheWaitOrRules()
        {
            yield return Begin();
            int first = Enumerable.Range(0, 12).First(i => finale.Board.ArtifactAt(i) == 0);
            int other = Enumerable.Range(0, 12).First(i => finale.Board.ArtifactAt(i) == 2);
            yield return Flip(first); yield return Flip(other);
            string text = Safe.Find("FinaleView/Feedback").GetComponent<TMP_Text>().text;
            Assert.That(text, Does.Contain("魚形輪廓").And.Contain("罐身"));
            Assert.That(finale.Board.IsFaceUp(first) || finale.Board.IsFaceUp(other), Is.False);
            Assert.That(shared.Session.MistakeCount, Is.EqualTo(1));
            AssertVisibleTextFits();
        }

        [UnityTest]
        public IEnumerator SuccessUnlocksSixClickableCatalogEntriesAndRetryHidesThem()
        {
            Capture("introduction");
            ConfirmIntroduction();
            Capture("inventory");
            yield return WaitUntilIdle();
            Capture("board");
            for (int artifact = 0; artifact < 6; artifact++)
            {
                var pair = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == artifact).ToArray();
                yield return Flip(pair[0]); yield return Flip(pair[1]);
                if (artifact == 0) { yield return null; Capture("matched"); }
            }
            var gallery = Safe.Find("ResultOverlay/ResultCard/ArtifactGallery");
            Assert.That(gallery, Is.Not.Null, "Successful results must expose the six artifact entries.");
            Assert.That(gallery.gameObject.activeInHierarchy, Is.True);
            yield return null; // Let newly enabled UI graphics enter the canvas rebuild and raycast registry.
            string[] evidence = { "銅線", "閉眼人臉", "蘭陽平原", "推測", "同心圓", "六角管形" };
            for (int i = 0; i < 6; i++)
            {
                var button = gallery.Find("Artifact" + i).GetComponent<Button>();
                Assert.That(button.interactable, Is.True);
                ClickByRaycast(button);
                var detail = gallery.Find("Description").GetComponent<TMP_Text>();
                Assert.That(detail.text, Does.Contain(evidence[i]).And.Contain("已核對").And.Contain("2026-10-29"));
                Assert.That(gallery.Find("ArtifactSource").GetComponent<Button>().interactable, Is.True);
                Assert.That(gallery.Find("ContextSource").GetComponent<Button>().interactable, Is.True);
                AssertVisibleTextFits();
                Capture("catalog-" + i);
                yield return null;
            }
            AssertRaycastTarget(gallery.Find("ArtifactSource").GetComponent<Button>());
            AssertRaycastTarget(gallery.Find("ContextSource").GetComponent<Button>());
            root.SetActive(false); yield return null; root.SetActive(true); yield return null;
            Assert.That(gallery.gameObject.activeInHierarchy, Is.True);
            ClickByRaycast(gallery.Find("Artifact0").GetComponent<Button>());
            Assert.That(gallery.Find("Description").GetComponent<TMP_Text>().text, Does.Contain("銅線"));
            Assert.That(completed, Is.EqualTo(1));
            Control("ResultOverlay/ResultCard/Retry").onClick.Invoke();
            Assert.That(gallery.gameObject.activeSelf, Is.False);
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(((RectTransform)Safe.Find("ResultOverlay/ResultCard")).sizeDelta, Is.EqualTo(new Vector2(1000, 760)));
        }

        [UnityTest]
        public IEnumerator FailureKeepsTheOriginalResultLayoutWithoutUnlockingCatalog()
        {
            yield return Begin();
            shared.Session.Tick(90);
            var gallery = Safe.Find("ResultOverlay/ResultCard/ArtifactGallery");
            Assert.That(gallery, Is.Not.Null);
            Assert.That(gallery.gameObject.activeSelf, Is.False);
            Assert.That(((RectTransform)Safe.Find("ResultOverlay/ResultCard")).sizeDelta, Is.EqualTo(new Vector2(1000, 760)));
            Assert.That(relay.LastResult.Success, Is.False);
            AssertVisibleTextFits();
            Capture("failure");
        }

        [UnityTest]
        public IEnumerator HintLastsTwoSecondsWithoutSpendingCountdownAndHonorsPause()
        {
            Assert.That(finale.TryFlip(0), Is.False);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(90));
            ConfirmIntroduction();
            Assert.That(finale.AcquiredArtifactIds, Is.EqualTo(new[] { "artifact-01", "artifact-03", "artifact-06" }));
            Assert.That(finale.TryFlip(0), Is.False);
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(finale.IsShowingHint, Is.True);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(90));
            Control("HUD/Pause").onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.That(finale.IsShowingHint, Is.True);
            Control("PauseOverlay/PauseCard/Resume").onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(finale.IsShowingHint, Is.True);
            yield return WaitUntilIdle();
            Assert.That(shared.Session.RemainingTime, Is.GreaterThan(89.7f));
        }

        [UnityTest]
        public IEnumerator AnimationsRejectRapidClicksAndWrongPairWaitsPointEightActiveSeconds()
        {
            yield return Begin();
            int other = DifferentFrom(0);
            Assert.That(finale.TryFlip(0), Is.True);
            Assert.That(finale.TryFlip(other), Is.False, "First flip animation must lock every card.");
            yield return WaitUntilIdle();
            Assert.That(finale.TryFlip(other), Is.True);
            int third = Enumerable.Range(0, 12).First(i => i != 0 && i != other);
            Assert.That(finale.TryFlip(third), Is.False);
            Assert.That(shared.Session.MistakeCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.72f);
            Assert.That(finale.Cards[0].FaceVisible && finale.Cards[other].FaceVisible, Is.True);
            Assert.That(finale.TryFlip(third), Is.False);
            shared.Pause(); float time = shared.Session.RemainingTime;
            yield return new WaitForSecondsRealtime(1);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(time));
            Assert.That(finale.Cards[0].FaceVisible && finale.Cards[other].FaceVisible, Is.True);
            shared.Resume(); yield return WaitUntilIdle();
            Assert.That(finale.Cards[0].FaceVisible || finale.Cards[other].FaceVisible, Is.False);
            Assert.That(finale.Board.IsFaceUp(third), Is.False);
            Assert.That(shared.Session.MistakeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SixPairsKeepGoldFramesAndPublishOneImmutableSuccessResult()
        {
            yield return Begin();
            for (int artifact = 0; artifact < 6; artifact++)
            {
                var pair = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == artifact).ToArray();
                yield return Flip(pair[0]); yield return Flip(pair[1]);
                Assert.That(pair.All(i => finale.Cards[i].FaceVisible && finale.Cards[i].GoldFrameVisible), Is.True);
            }
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Succeeded));
            Assert.That(finale.Board.MatchedPairs, Is.EqualTo(6));
            Assert.That(completed, Is.EqualTo(1));
            var result = relay.LastResult;
            Assert.That(result.LevelId, Is.EqualTo("Finale"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.RemainingTime, Is.GreaterThan(0).And.LessThan(90));
            Assert.That(result.MistakeCount, Is.Zero);
            Assert.That(result.ArtifactIds, Is.EqualTo(new[] { "artifact-01", "artifact-03", "artifact-06" }));
            Assert.That(relay.LastResultJson, Does.Contain("\"levelId\":\"Finale\"").And.Contain("\"success\":true"));
            Assert.That(finale.TryFlip(0), Is.False);
            shared.Succeed(); shared.Fail(); shared.Session.Tick(500);
            Assert.That(completed, Is.EqualTo(1));
            Control("ResultOverlay/ResultCard/Retry").onClick.Invoke();
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(shared.Session.ArtifactIds, Is.Empty);
            Assert.That(relay.LastResult, Is.Null);
            Assert.That(relay.LastResultJson, Is.Empty);
            Assert.That(result.Success, Is.True);
            Assert.That(result.ArtifactIds.Count, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator TimeoutDuringAnimationPublishesFailureOnceAndCancelsPendingWork()
        {
            yield return Begin();
            int other = DifferentFrom(0);
            yield return Flip(0);
            Assert.That(finale.TryFlip(other), Is.True);
            shared.Session.Tick(90);
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Failed));
            Assert.That(relay.LastResult.Success, Is.False);
            Assert.That(relay.LastResult.RemainingTime, Is.Zero);
            Assert.That(relay.LastResult.MistakeCount, Is.EqualTo(1));
            Assert.That(relay.LastResult.ArtifactIds.Count, Is.EqualTo(3));
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(finale.TryFlip(2), Is.False);
            Assert.That(finale.Board.MatchedPairs, Is.Zero);
            Assert.That(Safe.Find("ResultOverlay").gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator PauseRetryClearsPendingMismatchAndStartsAFreshHint()
        {
            yield return Begin();
            yield return Flip(0); Assert.That(finale.TryFlip(DifferentFrom(0)), Is.True);
            Control("HUD/Pause").onClick.Invoke();
            Control("PauseOverlay/PauseCard/Retry").onClick.Invoke();
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(90));
            Assert.That(shared.Session.MistakeCount, Is.Zero);
            Assert.That(finale.Board.MatchedPairs, Is.Zero);
            Assert.That(finale.Cards.All(card => !card.FaceVisible && !card.GoldFrameVisible), Is.True);
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            Assert.That(completed, Is.Zero);
            yield return Begin();
            yield return Flip(0);
        }

        [UnityTest]
        public IEnumerator KnowledgeAndIntroductionPauseCountdownWithoutClearingManualPause()
        {
            yield return Begin(); yield return Flip(0);
            Control("FinaleView/GuideCard/Knowledge").onClick.Invoke();
            float time = shared.Session.RemainingTime;
            Assert.That(Panel("KnowledgeCard").gameObject.activeSelf, Is.True);
            Assert.That(finale.TryFlip(DifferentFrom(0)), Is.False);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(time));
            shared.Pause(); Panel("KnowledgeCard").PrimaryButton.onClick.Invoke();
            Assert.That(shared.CanInteract, Is.False);
            shared.Resume();
            finale.ShowSpaceIntroduction();
            Assert.That(Panel("Introduction").gameObject.activeSelf, Is.True);
            Assert.That(shared.CanInteract, Is.False);
            ConfirmIntroduction();
            Assert.That(finale.IsShowingHint, Is.False, "Reopening the introduction must not replay the initial hint.");
            Assert.That(finale.Board.IsFaceUp(0), Is.True);
            Assert.That(shared.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator PausingMidFlipFreezesVisualAndRejectsPointerClickBehindOverlay()
        {
            yield return Begin();
            Assert.That(finale.TryFlip(0), Is.True);
            shared.Pause();
            var visual = finale.Cards[0].transform.Find("Visual");
            Vector3 scale = visual.localScale;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(visual.localScale, Is.EqualTo(scale));
            ExecuteEvents.Execute(finale.Cards[1].gameObject,
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
            Assert.That(finale.Board.IsFaceUp(1), Is.False);
            shared.Resume(); yield return WaitUntilIdle();
            Assert.That(finale.Cards[0].FaceVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator AssignedCollectedArtifactsDriveTheHintAndReportedResult()
        {
            var provider = new GameObject("GameManager artifact adapter").AddComponent<FinaleArtifactProviderFixture>();
            SceneManager.MoveGameObjectToScene(provider.gameObject, scene);
            yield return Begin();
            Assert.That(finale.AcquiredArtifactIds, Is.EqualTo(new[] { "artifact-06", "artifact-04", "artifact-02" }));
            shared.Session.Tick(90);
            Assert.That(relay.LastResult.ArtifactIds, Is.EqualTo(new[] { "artifact-06", "artifact-04", "artifact-02" }));
        }

        [UnityTest]
        public IEnumerator RestartDuringHintDiscardsHintAndDoesNotStartOldCountdown()
        {
            ConfirmIntroduction();
            Assert.That(finale.IsShowingHint, Is.True);
            Control("FinaleView/Restart").onClick.Invoke();
            Assert.That(finale.IsShowingHint, Is.False);
            Assert.That(shared.Session.State, Is.EqualTo(LevelState.Ready));
            yield return new WaitForSecondsRealtime(2.1f);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(90));
            Assert.That(finale.AcquiredArtifactIds, Is.Empty);
            yield return Begin();
        }

        [UnityTest]
        public IEnumerator RootReactivationCancelsUnfinishedFlipsButPreservesMatchedPairs()
        {
            yield return Begin();
            var pair = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == 0).ToArray();
            yield return Flip(pair[0]); yield return Flip(pair[1]);
            int next = Enumerable.Range(0, 12).First(i => !finale.Board.IsMatched(i));
            Assert.That(finale.TryFlip(next), Is.True);
            root.SetActive(false); yield return null; root.SetActive(true); yield return null;
            Assert.That(finale.IsBusy, Is.False, "Stopped coroutines must not leave a permanent input lock.");
            Assert.That(finale.Cards[next].FaceVisible, Is.False);
            Assert.That(finale.Board.MatchedPairs, Is.EqualTo(1));
            Assert.That(pair.All(i => finale.Cards[i].GoldFrameVisible), Is.True);
            yield return Flip(next);
            int wrong = Enumerable.Range(0, 12).First(i => !finale.Board.IsMatched(i) && finale.Board.ArtifactAt(i) != finale.Board.ArtifactAt(next));
            Assert.That(finale.TryFlip(wrong), Is.True);
            root.SetActive(false); yield return null; root.SetActive(true); yield return null;
            Assert.That(finale.IsBusy, Is.False);
            Assert.That(finale.Cards[next].FaceVisible || finale.Cards[wrong].FaceVisible, Is.False);
            Assert.That(shared.Session.MistakeCount, Is.EqualTo(1));
            yield return Flip(next);
        }

        [UnityTest]
        public IEnumerator FocusLossFreezesHintAndMismatchWait()
        {
            ConfirmIntroduction();
            root.SendMessage("OnApplicationFocus", false);
            yield return new WaitForSecondsRealtime(2.1f);
            Assert.That(finale.IsShowingHint, Is.True);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(90));
            root.SendMessage("OnApplicationFocus", true);
            yield return WaitUntilIdle();
            yield return Flip(0); int other = DifferentFrom(0);
            Assert.That(finale.TryFlip(other), Is.True);
            yield return new WaitForSecondsRealtime(0.3f);
            root.SendMessage("OnApplicationFocus", false);
            float remaining = shared.Session.RemainingTime;
            yield return new WaitForSecondsRealtime(1);
            Assert.That(shared.Session.RemainingTime, Is.EqualTo(remaining));
            Assert.That(finale.Cards[0].FaceVisible && finale.Cards[other].FaceVisible, Is.True);
            root.SendMessage("OnApplicationFocus", true);
            yield return WaitUntilIdle();
            Assert.That(finale.Cards[0].FaceVisible || finale.Cards[other].FaceVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator IntroductionAllKnowledgeCardsAndResultsFitTheirTextAreas()
        {
            AssertVisibleTextFits();
            yield return Begin(); AssertVisibleTextFits();
            for (int artifact = 0; artifact < 6; artifact++)
            {
                var pair = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == artifact).ToArray();
                yield return Flip(pair[0]);
                Control("FinaleView/GuideCard/Knowledge").onClick.Invoke();
                Assert.That(Panel("KnowledgeCard").gameObject.activeSelf, Is.True);
                AssertVisibleTextFits();
                Panel("KnowledgeCard").PrimaryButton.onClick.Invoke();
                yield return Flip(pair[1]);
            }
            AssertVisibleTextFits();
        }

        private void ClickByRaycast(Button button)
        {
            var pointer = AssertRaycastTarget(button);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private PointerEventData AssertRaycastTarget(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " must receive pointer input at " + pointer.position +
                " in " + Screen.width + "x" + Screen.height + "; canvas=" + root.GetComponentInChildren<Canvas>().renderMode +
                "; graphic depth=" + button.GetComponent<Image>().depth);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(button.gameObject),
                button.name + " must not be blocked by another UI element.");
            return pointer;
        }

        private void Capture(string name)
        {
            string directory = System.Environment.GetEnvironmentVariable("FINALE_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            System.IO.Directory.CreateDirectory(directory);
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            var canvas = root.GetComponentInChildren<Canvas>();
            var mode = canvas.renderMode;
            var worldCamera = canvas.worldCamera;
            float previousPlaneDistance = canvas.planeDistance;
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
                canvas.planeDistance = previousPlaneDistance;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        private void AssertVisibleTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False,
                    text.transform.parent.name + "/" + text.name + " preferred height " + text.preferredHeight +
                    " in " + text.rectTransform.rect.height + " must display the complete text: " + text.text);
            }
        }
#endif
    }

    public sealed class FinaleArtifactProviderFixture : MonoBehaviour, IFinaleArtifactSource
    {
        public IEnumerable<string> GetFinaleArtifactIds() => new[] { "artifact-06", "artifact-04", "artifact-02" };
    }
}
