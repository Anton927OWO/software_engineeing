using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using XunJiYiDa.Levels.Shared;
using XunJiYiDa.Levels.Timeline.Editor;

namespace XunJiYiDa.Levels.Timeline.Tests
{
    public sealed class TimelineSceneTests
    {
        public const string ScenePath = "Assets/Levels/Timeline/L_Timeline.unity";

        [Test]
        public void RepeatedGenerationPreservesSavedSceneOpenSceneAndBuildSettings()
        {
            var bytes = File.ReadAllBytes(ScenePath);
            var settings = File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");
            var definition = File.ReadAllBytes("Assets/Levels/Timeline/Data/TimelineDefinition.asset");
            var active = SceneManager.GetActiveScene();
            int count = SceneManager.sceneCount;
            TimelineSceneBuilder.BuildAll();
            Assert.That(File.ReadAllBytes(ScenePath), Is.EqualTo(bytes));
            Assert.That(File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset"), Is.EqualTo(settings));
            Assert.That(File.ReadAllBytes("Assets/Levels/Timeline/Data/TimelineDefinition.asset"), Is.EqualTo(definition));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
        }

        [Test]
        public void CardsHaveDistinctEraTreatmentsAndRetainArtworkAndTimeline()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                var treatments = new System.Collections.Generic.HashSet<Color>();
                foreach (int year in new[] { 1926, 1946, 1988, 1998, 2003 })
                {
                    var card = objects.Single(t => t.name == "Card_" + year);
                    Assert.That(card.Find("EraBand"), Is.Not.Null, "Each era needs a visible accent.");
                    Assert.That(card.Find("EraMark"), Is.Not.Null, "Use shape as well as colour.");
                    treatments.Add(card.GetComponent<UnityEngine.UI.Image>().color);
                    Assert.That(card.Find("SchoolArtwork/Illustration").GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);
                    Assert.That(objects.Single(t => t.name == "Year_" + year).GetComponent<TMP_Text>().text, Is.EqualTo(year.ToString()));
                }
                Assert.That(treatments.Count, Is.EqualTo(5));
                Assert.That(objects.Single(t => t.name == "TimelineTrack").gameObject.activeSelf, Is.True);
                Assert.That(objects.Single(t => t.name == "Feedback").GetComponent<TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(26));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void EveryHistoryFactAndCulturalConnectionHasAnOfficialSource()
        {
            var stages = TimelineHistory.Stages;
            Assert.That(stages.Select(e => e.Year), Is.EqualTo(new[] { 1926, 1946, 1988, 1998, 2003 }));
            foreach (var stage in stages)
            {
                foreach (var url in new[] { stage.SourceUrl, stage.ConnectionSourceUrl })
                {
                    Assert.That(System.Uri.TryCreate(url, System.UriKind.Absolute, out var uri), Is.True);
                    Assert.That(uri.Scheme, Is.EqualTo("https"));
                    Assert.That(uri.Host == "niu.edu.tw" || uri.Host.EndsWith(".niu.edu.tw"), Is.True);
                }
                Assert.That(stage.Change, Is.Not.Empty);
                Assert.That(stage.Connection, Is.Not.Empty);
                Assert.That(stage.Summary, Does.Contain("非當年事件"), "Do not backdate present-day cultural connections.");
            }
            Assert.That(TimelineHistory.ScopeNote, Does.Contain("1967").And.Contain("1969"));
        }

        [Test]
        public void RepeatedQualityUpgradePreservesSceneContextAndBuildSettings()
        {
            var active = SceneManager.GetActiveScene();
            int count = SceneManager.sceneCount;
            byte[] settings = File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");
            TimelineQualityUpgrade.UpgradeSavedScene();
            byte[] first = File.ReadAllBytes(ScenePath);
            TimelineQualityUpgrade.UpgradeSavedScene();
            Assert.That(File.ReadAllBytes(ScenePath), Is.EqualTo(first));
            Assert.That(File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset"), Is.EqualTo(settings));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
        }

        [Test]
        public void QualityUpgradeRefusesDirtyScenesWithoutWritingTheSavedScene()
        {
            byte[] saved = File.ReadAllBytes(ScenePath);
            var active = SceneManager.GetActiveScene();
            var probe = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.MarkSceneDirty(probe);
                Assert.Throws<System.InvalidOperationException>(() => TimelineQualityUpgrade.UpgradeSavedScene());
                Assert.That(File.ReadAllBytes(ScenePath), Is.EqualTo(saved));
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(probe, true);
            }
        }

        [Test]
        public void DeliveredSceneHasSharedPrefabFiveCardsFiveSlotsAndChineseFont()
        {
            Assert.That(File.Exists(ScenePath), Is.True, "Editor generator must deliver a saved Unity scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controllers = roots.SelectMany(g => g.GetComponentsInChildren<SharedLevelController>(true)).ToArray();
                Assert.That(controllers, Has.Length.EqualTo(1));
                var controller = controllers[0];
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(controller.gameObject),
                    Is.EqualTo("Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab"));
                Assert.That(controller.Definition, Is.Not.Null, "Scene creation must retain its Timeline definition.");
                Assert.That(controller.Definition.LevelId, Is.EqualTo("Timeline"));
                Assert.That(controller.Definition.DurationSeconds, Is.EqualTo(45));
                Assert.That(controller.Definition.MistakeLimit, Is.Zero);
                Assert.That(controller.Definition.ShowIntroduction, Is.True);
                Assert.That(controller.GetComponent<LevelResultRelay>(), Is.Not.Null);
                var objects = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach (int year in new[] { 1926, 1946, 1988, 1998, 2003 })
                {
                    var card = objects.Single(t => t.name == "Card_" + year);
                    Assert.That(card.GetComponents<MonoBehaviour>().Any(c => c is IBeginDragHandler && c is IEndDragHandler), Is.True);
                    Assert.That(objects.Single(t => t.name == "Slot_" + year).GetComponents<MonoBehaviour>().Any(c => c is IDropHandler), Is.True);
                }
                Assert.That(objects.Count(t => t.GetComponent<Camera>() != null), Is.EqualTo(1));
                Assert.That(objects.Count(t => t.GetComponent<AudioListener>() != null), Is.EqualTo(1));
                Assert.That(objects.Count(t => t.GetComponent<EventSystem>() != null), Is.EqualTo(1));
                Assert.That(objects.Single(t => t.GetComponent<EventSystem>() != null).GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Levels/Shared/Fonts/NotoSansTC-Regular SDF.asset");
                foreach (var text in objects.Select(t => t.GetComponent<TMP_Text>()).Where(t => t != null))
                    Assert.That(text.font, Is.SameAs(font), text.name);
                foreach (var behaviour in objects.SelectMany(t => t.GetComponents<MonoBehaviour>()))
                    Assert.That(behaviour, Is.Not.Null, "Scene contains a missing script.");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
