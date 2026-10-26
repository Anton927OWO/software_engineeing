using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 建立全組共用的中文字型 Font Asset，放在 Assets/Common/Fonts。
    /// 先執行「Tracking NIU → 匯入 TMP Essential Resources」，再執行「Tracking NIU → 建立中文字型 Font Asset」。
    /// 已經存在的 Font Asset 不會重建，可以重複執行。
    /// </summary>
    public static class FontAssetBuilder
    {
        const string FontFolder = "Assets/Common/Fonts";
        const string EssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // 中文字數多，用 Dynamic 模式在執行時才產生用到的字，並允許一張圖集滿了自動加第二張。
        const int SamplingPointSize = 90;
        const int AtlasPadding = 9;
        const int AtlasSize = 2048;
        const int BoldWeightIndex = 7; // fontWeightTable 中 700（Bold）的位置

        [MenuItem("Tracking NIU/匯入 TMP Essential Resources", false, 1)]
        public static void ImportEssentials()
        {
            if (HasTmpSettings())
            {
                Debug.Log("[字型] TMP Essential Resources 已經匯入過了。");
                ExitIfBatchMode(0);
                return;
            }

            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.ImportPackage(Path.GetFullPath(EssentialsPackage), false);
        }

        // 匯入是非同步的，batch mode 要等匯入完成才結束 Unity。
        static void OnImportCompleted(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            Debug.Log("[字型] 已匯入 TMP Essential Resources。");
            ExitIfBatchMode(0);
        }

        static void OnImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            Debug.LogError($"[字型] 匯入 TMP Essential Resources 失敗：{error}");
            ExitIfBatchMode(1);
        }

        static void ExitIfBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }

        [MenuItem("Tracking NIU/建立中文字型 Font Asset", false, 2)]
        public static void BuildFontAssets()
        {
            if (!HasTmpSettings())
            {
                Debug.LogError("[字型] 請先執行 Tracking NIU → 匯入 TMP Essential Resources。");
                return;
            }

            var regular = GetOrCreate("NotoSansTC-Regular");
            var bold = GetOrCreate("NotoSansTC-Bold");
            var serif = GetOrCreate("NotoSerifTC-Black");
            if (regular == null || bold == null || serif == null)
                return;

            // 文字設成粗體時改用真正的 Bold 字重，不用程式模擬的假粗體。
            regular.fontWeightTable[BoldWeightIndex].regularTypeface = bold;
            EditorUtility.SetDirty(regular);

            // 新增的 TextMeshPro 元件預設使用思源黑體 Regular。
            TMP_Settings.defaultFontAsset = regular;
            EditorUtility.SetDirty(TMP_Settings.instance);

            AssetDatabase.SaveAssets();
            Debug.Log("[字型] 中文字型 Font Asset 建立完成：思源黑體 Regular／Bold、思源宋體 Black。");
        }

        static TMP_FontAsset GetOrCreate(string fontName)
        {
            string assetPath = $"{FontFolder}/{fontName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            string fontPath = $"{FontFolder}/{fontName}.otf";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[字型] 找不到 {fontPath}。");
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding,
                GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (fontAsset == null)
            {
                Debug.LogError($"[字型] 無法從 {fontPath} 建立 Font Asset。");
                return null;
            }

            fontAsset.name = fontName + " SDF";
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // 圖集與材質要存成 Font Asset 的子物件，否則重開專案後會遺失。
            fontAsset.atlasTextures[0].name = fontName + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            fontAsset.material.name = fontName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            // Font Asset Creator 視窗讀取這些設定，之後要重建圖集時才會沿用相同參數。
            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileGUID = AssetDatabase.AssetPathToGUID(fontPath),
                pointSize = SamplingPointSize,
                padding = AtlasPadding,
                atlasWidth = AtlasSize,
                atlasHeight = AtlasSize,
                characterSetSelectionMode = 7, // Custom Characters
                characterSequence = string.Empty,
                renderMode = (int)GlyphRenderMode.SDFAA,
            };

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        static bool HasTmpSettings() => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;
    }
}
