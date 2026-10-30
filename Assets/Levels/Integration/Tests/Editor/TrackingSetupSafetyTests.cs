using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XunJiYiDa.Levels.Integration.Editor;

namespace XunJiYiDa.Levels.Integration.Tests
{
    public sealed class TrackingSetupSafetyTests
    {
        [Test]
        public void PrepareRejectsUnsavedSceneBeforeReplacingItsContents()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var marker = new GameObject("Unsaved teammate edit");
            EditorSceneManager.MarkSceneDirty(scene);
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() => TrackingIntegrationSetup.Prepare());
                Assert.That(error.Message, Does.Contain("Save"));
                Assert.That(marker != null, Is.True, "Unsaved scene objects must survive rejected setup.");
                Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
