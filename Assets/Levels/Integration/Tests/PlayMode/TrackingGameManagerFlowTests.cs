using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;
using TeamSession = TrackingNIU.LevelSession;

namespace XunJiYiDa.Levels.Integration.Tests
{
    public sealed class TrackingGameManagerFlowTests
    {
        private TrackingNIU.GameManager manager;

        [UnityTest]
        public IEnumerator OfficialRetryPreservesManagerCallbackAndReturnsToLibraryMap()
        {
            AbortSession();
            var go = new GameObject("Integration GameManager");
            manager = go.AddComponent<TrackingNIU.GameManager>();
            TrackingNIU.LevelId? selected = null;
            for (int attempt = 0; attempt < 32 && !selected.HasValue; attempt++)
            {
                manager.StartPreviewRun(TrackingNIU.MapKind.Library);
                foreach (var id in manager.Run.IndoorLevels)
                    if ((id == TrackingNIU.LevelId.Train || id == TrackingNIU.LevelId.Timeline) &&
                        System.IO.File.Exists($"Assets/Levels/{id}/L_{id}.unity")) selected = id;
            }
            if (!selected.HasValue) Assert.Ignore("This branch has no playable indoor level.");
            Assert.That(manager.EnterLevel(selected.Value), Is.True);
            yield return null;
            yield return null;
            var context = TeamSession.Current;
            var controller = Object.FindFirstObjectByType<SharedLevelController>();
            StartGame(controller);
            controller.Succeed();
            Assert.That(TeamSession.HasReported, Is.True);
            controller.Retry();
            yield return null;
            yield return null;
            Assert.That(TeamSession.Current, Is.SameAs(context));
            Assert.That(TeamSession.HasReported, Is.False);
            controller = Object.FindFirstObjectByType<SharedLevelController>();
            StartGame(controller);
            controller.Succeed();
            controller.GetComponentsInChildren<Button>(true).Single(b => b.name == "ContinueToMap").onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map_Library"));
            Assert.That(manager.Run.TotalScore, Is.GreaterThan(0));
            Assert.That(manager.Run.CollectedArtifacts, Does.Contain(context.Artifact));
            Assert.That(TeamSession.IsActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AbortSession();
            if (manager != null) Object.DestroyImmediate(manager.gameObject);
            var current = SceneManager.GetActiveScene();
            var clean = SceneManager.CreateScene("IntegrationTestCleanup");
            SceneManager.SetActiveScene(clean);
            if (current.IsValid() && current.isLoaded) yield return SceneManager.UnloadSceneAsync(current);
            Time.timeScale = 1;
        }

        private static void StartGame(SharedLevelController controller)
            => controller.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction").PrimaryButton.onClick.Invoke();

        // Fixture cleanup must not invoke a live manager callback and trigger another scene load.
        private static void AbortSession()
            => typeof(TeamSession).GetMethod("Abort", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
