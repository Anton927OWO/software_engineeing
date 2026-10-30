using System;
using System.IO;
using UnityEditor;

namespace XunJiYiDa.Levels.Shared.Editor
{
    public static class SharedAssetPaths
    {
        public const string Root = "Assets/Levels/Shared";
        public const string Source = "Assets/unity其中素材";
        public const string Font = Root + "/Fonts/NotoSansTC-Regular SDF.asset";
        public const string Definition = Root + "/Data/SharedFoundation.asset";
        public const string Audio = Root + "/Data/SharedAudioLibrary.asset";
        public const string Prefab = Root + "/Prefabs/SharedLevelRoot.prefab";
        public const string Scene = Root + "/Scenes/SharedFoundation.unity";

        public static string RequireSharedPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Contains("\\"))
                throw new ArgumentException("Use a Unity project-relative path.", nameof(path));
            string full = Path.GetFullPath(path);
            string root = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.Ordinal))
                throw new ArgumentException("The shared generator may only write inside " + Root, nameof(path));
            return path;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException("Invalid asset folder.", nameof(path));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
