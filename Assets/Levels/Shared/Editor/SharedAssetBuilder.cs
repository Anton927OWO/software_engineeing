using System;
using System.Linq;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace XunJiYiDa.Levels.Shared.Editor
{
    public static class SharedAssetBuilder
    {
        [MenuItem("尋跡宜大/Shared/建立缺少的共用資產與驗證場景")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before generating shared assets.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureSprites();
            var font = EnsureFont();
            var definition = EnsureDefinition();
            var library = EnsureAudio();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SharedAssetPaths.Prefab) == null)
            {
                SharedAssetPaths.EnsureFolder(SharedAssetPaths.Root + "/Prefabs");
                var root = SharedPrefabFactory.Create(font, definition, library);
                try
                {
                    if (PrefabUtility.SaveAsPrefabAsset(root, SharedAssetPaths.Prefab) == null)
                        throw new InvalidOperationException("Could not save the shared prefab.");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            SharedSceneBuilder.CreateFoundationIfMissing();
            Debug.Log("尋跡宜大：共用資產已備妥。Scene: " + SharedAssetPaths.Scene);
        }

        [MenuItem("尋跡宜大/Shared/開啟共用基礎驗證場景")]
        public static void OpenFoundation()
        {
            BuildAll();
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SharedAssetPaths.Scene);
        }

        private static void EnsureSprites()
        {
            SharedAssetPaths.EnsureFolder(SharedAssetPaths.Root + "/Sprites");
            foreach (var name in new[] { "Button_Red", "Panel_Cream", "Button_Green", "Hint_White", "HUD_Cream", "Button_Disabled" })
            {
                string target = SharedAssetPaths.Root + "/Sprites/" + name + ".png";
                if (!File.Exists(target) &&
                    !AssetDatabase.CopyAsset(SharedAssetPaths.Source + "/UI/" + name + ".png", target))
                    throw new InvalidOperationException("Missing UI source: " + name);
                var importer = AssetImporter.GetAtPath(target) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Cannot import " + target);
                var border = new Vector4(32, 32, 32, 32);
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    importer.spriteBorder == border && !importer.mipmapEnabled && importer.alphaIsTransparency) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static TMP_FontAsset EnsureFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedAssetPaths.Font);
            if (existing != null) return existing;
            if (TMP_Settings.LoadDefaultSettings() == null)
                throw new InvalidOperationException("Shared/TextMeshPro essential resources are missing.");
            var source = AssetDatabase.LoadAssetAtPath<Font>(SharedAssetPaths.Source + "/Noto_Sans_TC/static/NotoSansTC-Regular.ttf");
            if (source == null) throw new InvalidOperationException("The static Noto Sans TC Regular source font is missing.");
            SharedAssetPaths.EnsureFolder(SharedAssetPaths.Root + "/Fonts");
            var font = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (font == null) throw new InvalidOperationException("TextMeshPro could not create the Noto font asset.");
            font.name = "NotoSansTC-Regular SDF";
            font.material.name = font.name + " Material";
            string ascii = new string(Enumerable.Range(32, 95).Select(n => (char)n).ToArray());
            string chinese = "尋跡宜大國立宜蘭大學繁體中文臺灣共用基礎驗證歡迎來到這裡提供關卡的時間暫停介紹與知識介面準備好後按下開始探索進行中已結束未完成成功剩餘失誤次數收集文物繼續重試關閉認識空間開啟小遊戲閱讀記錄結果本場景僅不含玩法請將內容掛載於容器可顯示文字及圖片此處替換說明回畫面後恢復操作歷史文化金鯉魚箔片幾何印紋陶罐木鏟質圓形雕板瑪瑙珠｜，。：；！？（）「」『』、　";
            if (!font.TryAddCharacters(ascii + chinese, out string missing))
                Debug.LogWarning("Noto font is missing seed characters: " + missing, font);
            AssetDatabase.CreateAsset(font, SharedAssetPaths.Font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            var fontSettings = new SerializedObject(font);
            fontSettings.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            fontSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            // Only adjust the settings delivered by this foundation, never another team's settings asset.
            string settingsPath = SharedAssetPaths.Root + "/TextMeshPro/Resources/TMP Settings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
            if (settings != null && AssetDatabase.GetAssetPath(TMP_Settings.instance) == settingsPath)
            {
                var so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
                so.FindProperty("m_defaultFontAssetPath").stringValue = "";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            return font;
        }

        private static SharedLevelDefinition EnsureDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<SharedLevelDefinition>(SharedAssetPaths.Definition);
            if (definition != null) return definition;
            SharedAssetPaths.EnsureFolder(SharedAssetPaths.Root + "/Data");
            definition = ScriptableObject.CreateInstance<SharedLevelDefinition>();
            AssetDatabase.CreateAsset(definition, SharedAssetPaths.Definition);
            return definition;
        }

        private static SharedAudioLibrary EnsureAudio()
        {
            var library = AssetDatabase.LoadAssetAtPath<SharedAudioLibrary>(SharedAssetPaths.Audio);
            if (library != null) return library;
            SharedAssetPaths.EnsureFolder(SharedAssetPaths.Root + "/Data");
            library = ScriptableObject.CreateInstance<SharedAudioLibrary>();
            AssetDatabase.CreateAsset(library, SharedAssetPaths.Audio);
            var so = new SerializedObject(library);
            var entries = so.FindProperty("entries");
            var sounds = (SharedSound[])Enum.GetValues(typeof(SharedSound));
            entries.arraySize = sounds.Length;
            for (int i = 0; i < sounds.Length; i++)
            {
                var item = entries.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("sound").enumValueIndex = (int)sounds[i];
                item.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    SharedAssetPaths.Source + "/Audio/UI/" + sounds[i] + ".wav");
                item.FindPropertyRelative("volume").floatValue = sounds[i] == SharedSound.Tick ? 0.35f : 0.8f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
            return library;
        }
    }
}
