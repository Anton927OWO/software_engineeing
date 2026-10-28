using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using XunJiYiDa.Levels.Finale.Editor;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale.Tests
{
    public sealed class FinaleSceneTests
    {
        [Test]
        public void FinaleSceneIsASavedUnityScene()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Levels/Finale/L_Finale.unity"),
                Is.Not.Null, "The playable Finale scene must be generated and saved by the Editor tool.");
        }

        [Test]
        public void SavedSceneHasLinkedSharedPrefabAssignedReferencesAndFourByThreeGrid()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FinaleSceneBuilder.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var components = roots.SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Assert.That(components.OfType<Camera>().Count(), Is.EqualTo(1));
                Assert.That(components.OfType<AudioListener>().Count(), Is.EqualTo(1));
                Assert.That(components.OfType<EventSystem>().Count(), Is.EqualTo(1));
                Assert.That(components.OfType<InputSystemUIInputModule>().Count(), Is.EqualTo(1));
                var shared = components.OfType<SharedLevelController>().Single();
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(shared.gameObject),
                    Is.EqualTo("Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab"));
                Assert.That(shared.Definition.LevelId, Is.EqualTo("Finale"));
                Assert.That(shared.Definition.CreateSession().RemainingTime, Is.EqualTo(90));
                Assert.That(shared.Definition.MistakeLimit, Is.Zero);
                Assert.That(shared.Definition.ShowIntroduction, Is.True);
                Assert.That(components.OfType<SharedLevelUI>().Count(), Is.EqualTo(1));
                Assert.That(components.OfType<LevelResultRelay>().Count(), Is.EqualTo(1));
                var grid = components.OfType<GridLayoutGroup>().Single();
                Assert.That(grid.constraintCount, Is.EqualTo(4));
                Assert.That(grid.GetComponentsInChildren<FinaleCardView>(true).Length, Is.EqualTo(12));
                foreach (var behaviour in components.OfType<MonoBehaviour>())
                {
                    if (!(behaviour is FinaleLevelController || behaviour is FinaleCardView)) continue;
                    var property = new SerializedObject(behaviour).GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                            Assert.That(property.objectReferenceValue, Is.Not.Null, behaviour.name + ": " + property.propertyPath);
                }
                foreach (var text in components.OfType<TMP_Text>())
                    Assert.That(AssetDatabase.GetAssetPath(text.font), Is.EqualTo(FinaleSceneBuilder.FontPath));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void BuilderIsIdempotentAndLeavesSharedAndBuildSettingsUntouched()
        {
            string[] paths = {
                FinaleSceneBuilder.ScenePath, FinaleSceneBuilder.DefinitionPath,
                "Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab",
                "Assets/Levels/Shared/Fonts/NotoSansTC-Regular SDF.asset",
                "ProjectSettings/EditorBuildSettings.asset", "Packages/manifest.json"
            };
            var before = paths.Select(File.ReadAllBytes).ToArray();
            FinaleSceneBuilder.Build();
            for (int i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(before[i]), paths[i]);
        }

        [Test]
        public void SourceIdsArePrioritizedTrimmedDeduplicatedAndLimitedToThree()
        {
            Assert.That(FinaleArtifactSource.ResolveIds(
                new[] { " artifact-06 ", "artifact-06", null, "unknown", "artifact-04", "artifact-02", "artifact-01" },
                new[] { "artifact-01", "artifact-03", "artifact-05" }),
                Is.EqualTo(new[] { "artifact-06", "artifact-04", "artifact-02" }));
        }

        [Test]
        public void MissingOrMalformedSourceUsesValidConfigurableFallback()
        {
            Assert.That(FinaleArtifactSource.ResolveIds(null, new[] { "artifact-05", "artifact-02", "artifact-04" }),
                Is.EqualTo(new[] { "artifact-05", "artifact-02", "artifact-04" }));
            Assert.That(FinaleArtifactSource.ResolveIds(new[] { "artifact-00", "artifact-07", "ARTIFACT-01", "" },
                new[] { "artifact-03", "artifact-03", "unknown" }),
                Is.EqualTo(new[] { "artifact-03", "artifact-01", "artifact-02" }));
        }
    }
}
