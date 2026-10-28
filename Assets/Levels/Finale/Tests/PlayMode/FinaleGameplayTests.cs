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
            Control("FinaleView/GuideCard/SpaceIntroduction").onClick.Invoke();
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

        private void AssertVisibleTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False,
                    text.transform.parent.name + "/" + text.name + " must display the complete text: " + text.text);
            }
        }
#endif
    }

    public sealed class FinaleArtifactProviderFixture : MonoBehaviour, IFinaleArtifactSource
    {
        public IEnumerable<string> GetFinaleArtifactIds() => new[] { "artifact-06", "artifact-04", "artifact-02" };
    }
}
