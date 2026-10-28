using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XunJiYiDa.Levels.Shared;
using TeamSession = TrackingNIU.LevelSession;

namespace XunJiYiDa.Levels.Finale.Tests
{
    public sealed class TrackingFinaleFlowTests
    {
        private Scene loaded;
        private Scene previous;

        [UnityTest]
        public IEnumerator FinaleUsesCollectedArtifactsFromTeamContext()
        {
            yield return LoadFinale();
            var root = loaded.GetRootGameObjects().Single(g => g.GetComponent<SharedLevelController>() != null);
            root.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction").PrimaryButton.onClick.Invoke();
            var finale = root.GetComponentInChildren<FinaleLevelController>(true);
            Assert.That(finale.AcquiredArtifactIds, Is.EqualTo(new[] { "artifact-05", "artifact-06", "artifact-01" }),
                "The hint must reflect this run, rather than standalone sample artifacts.");
        }

        [UnityTest]
        public IEnumerator FinaleTimeoutReportsActualPartialPairs()
        {
            yield return LoadFinale();
            var root = loaded.GetRootGameObjects().Single(g => g.GetComponent<SharedLevelController>() != null);
            root.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction").PrimaryButton.onClick.Invoke();
            var finale = root.GetComponentInChildren<FinaleLevelController>(true);
            var controller = root.GetComponent<SharedLevelController>();
            for (int artifact = 0; artifact < 2; artifact++)
            {
                var indices = Enumerable.Range(0, 12).Where(i => finale.Board.ArtifactAt(i) == artifact).ToArray();
                Assert.That(finale.Board.TryReveal(indices[0]), Is.True);
                Assert.That(finale.Board.TryReveal(indices[1]), Is.True);
                finale.Board.ResolvePair();
            }
            controller.Session.SetPaused(PauseReason.Modal, false);
            controller.Session.Tick(100);
            Assert.That(TeamSession.HasReported, Is.True);
            Assert.That(TeamSession.Result.Cleared, Is.False);
            Assert.That(TeamSession.Result.MatchedPairs, Is.EqualTo(2));
            Assert.That(TeamSession.Score.MatchBonus, Is.EqualTo(200));
            Assert.That(TeamSession.Score.TimeBonus, Is.Zero);
        }

        private IEnumerator LoadFinale()
        {
            ClearSession();
            previous = SceneManager.GetActiveScene();
            var context = new TrackingNIU.LevelContext(TrackingNIU.LevelCatalog.Get(TrackingNIU.LevelId.Finale),
                new[] { TrackingNIU.ArtifactId.WoodenDisc, TrackingNIU.ArtifactId.AgateBead, TrackingNIU.ArtifactId.GoldenCarp });
            TeamSession.Begin(context, null);
            const string path = "Assets/Levels/Finale/L_Finale.unity";
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(loaded);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ClearSession();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            Time.timeScale = 1;
        }

        private static void ClearSession()
        {
            if (!TeamSession.IsActive) return;
            if (!TeamSession.HasReported) TeamSession.Report(new TrackingNIU.LevelResult());
            TeamSession.Exit();
        }
    }
}
