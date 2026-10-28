using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared.Editor;

namespace XunJiYiDa.Levels.Shared.Tests
{
    public sealed class SharedAssetTests
    {
        [TestCase("Assets/Common/unsafe.unity")]
        [TestCase("Assets/Levels/Shared/../Timeline/unsafe.unity")]
        [TestCase("Assets/Levels/SharedElse/unsafe.unity")]
        public void GeneratorRejectsPathsOutsideShared(string path)
            => Assert.Throws<ArgumentException>(() => SharedAssetPaths.RequireSharedPath(path));

        [Test]
        public void SharedPrefabHasAssignedRuntimeReferencesAndChineseFont()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(SharedAssetPaths.Prefab);
            Assert.That(root, Is.Not.Null);
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Assert.That(behaviour, Is.Not.Null, "Missing script in shared prefab");
                if (!(behaviour is SharedLevelController || behaviour is SharedLevelUI || behaviour is SharedContentPanel ||
                    behaviour is LevelResultRelay || behaviour is SharedAudioPlayer)) continue;
                var iterator = new SerializedObject(behaviour).GetIterator();
                while (iterator.NextVisible(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.name != "m_Script")
                        Assert.That(iterator.objectReferenceValue, Is.Not.Null, behaviour.GetType().Name + "." + iterator.propertyPath);
            }
            var scaler = root.GetComponentInChildren<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                Assert.That(AssetDatabase.GetAssetPath(text.font), Is.EqualTo(SharedAssetPaths.Font));
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedAssetPaths.Font);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
            Assert.That(font.isMultiAtlasTexturesEnabled, Is.True);
            Assert.That(font.sourceFontFile, Is.Not.Null);
            Assert.That(font.HasCharacters("繁體中文臺灣宜蘭大學"), Is.True);
        }

        [Test]
        public void BuildAllKeepsExistingAssetsAndBuildSettings()
        {
            string prefabGuid = AssetDatabase.AssetPathToGUID(SharedAssetPaths.Prefab);
            string sceneBefore = File.ReadAllText(SharedAssetPaths.Scene);
            string settingsBefore = File.ReadAllText("ProjectSettings/EditorBuildSettings.asset");
            SharedAssetBuilder.BuildAll();
            Assert.That(AssetDatabase.AssetPathToGUID(SharedAssetPaths.Prefab), Is.EqualTo(prefabGuid));
            Assert.That(File.ReadAllText(SharedAssetPaths.Scene), Is.EqualTo(sceneBefore));
            Assert.That(File.ReadAllText("ProjectSettings/EditorBuildSettings.asset"), Is.EqualTo(settingsBefore));
        }

        [Test]
        public void BuildAllDoesNotSaveUnrelatedDirtyAssets()
        {
            string path = SharedAssetPaths.Root + "/Tests/Unsaved-" + Guid.NewGuid().ToString("N") + ".asset";
            var asset = ScriptableObject.CreateInstance<SharedLevelDefinition>();
            try
            {
                AssetDatabase.CreateAsset(asset, path);
                string before = File.ReadAllText(path);
                var so = new SerializedObject(asset);
                so.FindProperty("displayName").stringValue = "Unsaved inspector edit";
                so.ApplyModifiedPropertiesWithoutUndo();
                SharedAssetBuilder.BuildAll();
                Assert.That(File.ReadAllText(path), Is.EqualTo(before));
                Assert.That(EditorUtility.IsDirty(asset), Is.True);
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
    }
}
