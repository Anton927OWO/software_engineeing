using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;
using TeamSession = TrackingNIU.LevelSession;

namespace XunJiYiDa.Levels.Integration.Tests
{
    public sealed class TrackingLevelFlowTests
    {
        private Scene loaded;
        private Scene previous;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ClearTeamSession();
            previous = SceneManager.GetActiveScene();
            previousTimeScale = Time.timeScale;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ClearTeamSession();
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            Time.timeScale = previousTimeScale;
        }

        [UnityTest]
        public IEnumerator TimelineReportsTeamScoreAndLeavesOnlyAfterContinue()
        {
            int exits = 0;
            bool cleared = false;
            var context = new TrackingNIU.LevelContext(TrackingNIU.LevelCatalog.Get(TrackingNIU.LevelId.Timeline),
                timeLimitOverride: 12);
            TeamSession.Begin(context, (result, score) => { exits++; cleared = result.Cleared; });
            yield return Load("Timeline");
            var root = Root();
            var controller = root.GetComponent<SharedLevelController>();
            Assert.That(controller.Session.RemainingTime, Is.EqualTo(12), "The team context owns the time limit.");
            StartGame(root);
            controller.RecordMistake();
            controller.Succeed();
            Assert.That(TeamSession.HasReported, Is.True, "Completing the game must report to TrackingNIU.");
            Assert.That(TeamSession.Result.LevelId, Is.EqualTo(TrackingNIU.LevelId.Timeline));
            Assert.That(TeamSession.Result.Artifact, Is.EqualTo(TrackingNIU.ArtifactId.WoodenDisc));
            Assert.That(TeamSession.Result.Mistakes, Is.EqualTo(1));
            Assert.That(TeamSession.Score.MistakePenalty, Is.EqualTo(20));
            Assert.That(exits, Is.Zero, "The result card must remain until Continue.");
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("總分")), Is.True);
            var button = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "ContinueToMap");
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            button.onClick.Invoke();
            Assert.That(exits, Is.EqualTo(1));
            Assert.That(cleared, Is.True);
            Assert.That(TeamSession.IsActive, Is.False);
        }

        [UnityTest]
        public IEnumerator StandaloneRetryClearsReportedScoreAndReportsFreshAttempt()
        {
            yield return Load("Train");
            var root = Root();
            var controller = root.GetComponent<SharedLevelController>();
            Assert.That(TeamSession.Current, Is.Not.Null, "The saved scene must support direct Play.");
            Assert.That(TeamSession.Current.IsTestRun, Is.True);
            StartGame(root);
            controller.RecordMistake();
            controller.Succeed();
            Assert.That(TeamSession.HasReported, Is.True);
            controller.Retry();
            Assert.That(TeamSession.IsActive, Is.True);
            Assert.That(TeamSession.HasReported, Is.False);
            Assert.That(controller.Session.MistakeCount, Is.Zero);
            StartGame(root);
            controller.Fail();
            Assert.That(TeamSession.HasReported, Is.True);
            Assert.That(TeamSession.Result.Cleared, Is.False);
            Assert.That(TeamSession.Result.Mistakes, Is.Zero);
        }

        [UnityTest]
        public IEnumerator StandaloneCanRetryAfterContinue()
        {
            yield return Load("Train");
            var root = Root();
            var controller = root.GetComponent<SharedLevelController>();
            StartGame(root);
            controller.Succeed();
            root.GetComponentsInChildren<Button>(true).Single(b => b.name == "ContinueToMap").onClick.Invoke();
            Assert.That(TeamSession.IsActive, Is.False);
            controller.Retry();
            Assert.That(TeamSession.IsActive, Is.True, "Retry must start a new standalone attempt after Continue.");
            Assert.That(controller.Session.Result, Is.Null);
            StartGame(root);
            controller.Succeed();
            Assert.That(TeamSession.HasReported, Is.True);
        }

        [UnityTest]
        public IEnumerator FoundationDoesNotCreateOrReplaceTeamContext()
        {
            var context = new TrackingNIU.LevelContext(TrackingNIU.LevelCatalog.Get(TrackingNIU.LevelId.Train));
            TeamSession.Begin(context, null);
            yield return Load("Shared/Scenes/SharedFoundation", false);
            Assert.That(TeamSession.Current, Is.SameAs(context));
            Assert.That(Root().GetComponent<SharedLevelController>().Definition.LevelId, Is.EqualTo("SharedFoundation"));
        }

        private IEnumerator Load(string level, bool namedLevel = true)
        {
            string path = namedLevel ? $"Assets/Levels/{level}/L_{level}.unity" : $"Assets/Levels/{level}.unity";
            if (!System.IO.File.Exists(path)) Assert.Ignore("This branch does not include " + level + ".");
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(loaded);
            yield return null;
        }

        private GameObject Root() => loaded.GetRootGameObjects().Single(g => g.GetComponent<SharedLevelController>() != null);
        private static void StartGame(GameObject root)
            => root.GetComponentsInChildren<SharedContentPanel>(true).Single(p => p.name == "Introduction").PrimaryButton.onClick.Invoke();

        private static void ClearTeamSession()
        {
            if (!TeamSession.IsActive) return;
            if (!TeamSession.HasReported) TeamSession.Report(new TrackingNIU.LevelResult());
            TeamSession.Exit();
        }
    }
}
