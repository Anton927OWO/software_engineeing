using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackingNIU
{
    /// <summary>
    /// 依名稱載入 Scene。在編輯器中，還沒加進 Build Settings 的 Scene 也能載入，
    /// 方便各關做好 Scene 後不用等整合就能從 GameManager 進入。
    /// </summary>
    public static class SceneLoader
    {
        public static bool Exists(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return true;
#if UNITY_EDITOR
            return FindScenePathInEditor(sceneName) != null;
#else
            return false;
#endif
        }

        /// <summary>載入成功開始時回傳 true；找不到 Scene 時回傳 false 並印出錯誤。</summary>
        public static bool Load(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return true;
            }

#if UNITY_EDITOR
            var path = FindScenePathInEditor(sceneName);
            if (path != null)
            {
                Debug.LogWarning($"[SceneLoader] {sceneName} 還沒加進 Build Settings，先用編輯器方式載入；輸出版本前要加進去。");
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    path, new LoadSceneParameters(LoadSceneMode.Single));
                return true;
            }
#endif

            Debug.LogError($"[SceneLoader] 找不到 Scene：{sceneName}");
            return false;
        }

#if UNITY_EDITOR
        static string FindScenePathInEditor(string sceneName)
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets($"t:Scene {sceneName}", new[] { "Assets" }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return path;
            }
            return null;
        }
#endif
    }
}
