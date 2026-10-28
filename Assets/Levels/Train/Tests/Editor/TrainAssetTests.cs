using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using TMPro;
using XunJiYiDa.Levels.Train.Editor;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Train.Tests
{
    public sealed class TrainAssetTests
    {
        public const string ScenePath = "Assets/Levels/Train/L_Train.unity";

        [Test]
        public void SceneExistsAsNativeUnityScene()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null,
                "Train Scene must be generated through Unity Editor.");
        }

        [Test]
        public void EditableBankContainsAtLeastTenSourcedThreeChoiceQuestions()
        {
            var bank = AssetDatabase.LoadAssetAtPath<Object>("Assets/Levels/Train/Data/TrainQuestions.asset");
            Assert.That(bank, Is.Not.Null, "Editable Train question bank is missing.");
            var questions = new SerializedObject(bank).FindProperty("questions");
            Assert.That(questions.arraySize, Is.GreaterThanOrEqualTo(10));
            var ids = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < questions.arraySize; i++)
            {
                var q = questions.GetArrayElementAtIndex(i);
                Assert.That(ids.Add(q.FindPropertyRelative("id").stringValue), Is.True);
                Assert.That(q.FindPropertyRelative("options").arraySize, Is.EqualTo(3));
                Assert.That(q.FindPropertyRelative("sourceUrl").stringValue, Does.StartWith("https://"));
                Assert.That(q.FindPropertyRelative("correctIndex").intValue, Is.InRange(0, 2));
                Assert.That(q.FindPropertyRelative("verified").boolValue, Is.True);
                Assert.That(q.FindPropertyRelative("verifiedOn").stringValue, Is.EqualTo("2026-10-26"));
                Assert.That(q.FindPropertyRelative("sourceUrl").stringValue, Does.Contain(".niu.edu.tw/"));
            }
        }

        [Test]
        public void SceneReusesSharedPrefabAndSixtySecondDefinition()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var controllers = roots.SelectMany(r => r.GetComponentsInChildren<SharedLevelController>(true)).ToArray();
                Assert.That(controllers.Length, Is.EqualTo(1));
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(controllers[0]),
                    Is.EqualTo("Assets/Levels/Shared/Prefabs/SharedLevelRoot.prefab"));
                Assert.That(controllers[0].Definition.LevelId, Is.EqualTo("Train"));
                Assert.That(controllers[0].Definition.DurationSeconds, Is.EqualTo(60));
                Assert.That(controllers[0].Definition.MistakeLimit, Is.Zero);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<InputSystemUIInputModule>(true)).Count(), Is.EqualTo(1));
                foreach (var mb in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
                    Assert.That(mb, Is.Not.Null, "Scene must not contain missing scripts.");
                var train = roots.SelectMany(r => r.GetComponentsInChildren<TrainLevelController>(true)).Single();
                var fields = new SerializedObject(train).GetIterator();
                while (fields.NextVisible(true))
                    if (fields.propertyType == SerializedPropertyType.ObjectReference)
                        Assert.That(fields.objectReferenceValue, Is.Not.Null, fields.propertyPath);
                foreach (var text in roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
                    Assert.That(AssetDatabase.GetAssetPath(text.font), Does.StartWith("Assets/Levels/Train/Fonts/"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void GeneratorPreservesExistingSceneBankAndBuildSettings()
        {
            string scene = System.IO.File.ReadAllText(ScenePath);
            string bank = System.IO.File.ReadAllText("Assets/Levels/Train/Data/TrainQuestions.asset");
            string settings = System.IO.File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            TrainSceneBuilder.BuildAll();
            Assert.That(System.IO.File.ReadAllText(ScenePath), Is.EqualTo(scene));
            Assert.That(System.IO.File.ReadAllText("Assets/Levels/Train/Data/TrainQuestions.asset"), Is.EqualTo(bank));
            Assert.That(System.IO.File.ReadAllText("ProjectSettings/EditorBuildSettings.asset"), Is.EqualTo(settings));
            Assert.That(EditorSceneManager.GetSceneManagerSetup().Select(s => s.path), Is.EqualTo(setup.Select(s => s.path)));
        }

        [Test]
        public void GeneratorDoesNotSaveUnrelatedDirtyAssets()
        {
            const string path = "Assets/Levels/Train/Tests/UnsavedTestDefinition.asset";
            var asset = ScriptableObject.CreateInstance<SharedLevelDefinition>();
            try
            {
                AssetDatabase.CreateAsset(asset, path);
                string before = System.IO.File.ReadAllText(path);
                var so = new SerializedObject(asset);
                so.FindProperty("displayName").stringValue = "Unsaved sentinel";
                so.ApplyModifiedPropertiesWithoutUndo();
                TrainSceneBuilder.BuildAll();
                Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo(before));
                Assert.That(EditorUtility.IsDirty(asset), Is.True);
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
    }
}
