using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace TrackingNIU.XR.Editor
{
    /// <summary>
    /// MR 原型的第 2 步：匯入 XR Interaction Toolkit 範例、設定 OpenXR／Meta Quest、URP 與 Android。
    /// 只做「在 Quest 3 上跑起來」需要的設定，不處理正式版輸出。可以重複執行。
    /// </summary>
    public static class XRProjectConfigurator
    {
        const string MenuRoot = "尋跡宜大/MR 原型/";
        const string XriPackage = "com.unity.xr.interaction.toolkit";

        static readonly string[] s_samples = { "Starter Assets", "XR Device Simulator", "XR Interaction Simulator" };

        // 依類別名稱啟用，避免直接引用 Meta OpenXR 的組件。
        static readonly string[] s_openXRFeatures =
        {
            "MetaQuestFeature",                    // Meta Quest 支援
            "OculusTouchControllerProfile",        // Quest 手把
            "MetaQuestTouchPlusControllerProfile", // Quest 3 手把
            "ARSessionFeature",                    // Meta OpenXR：AR Session
            "ARCameraFeature",                     // Meta OpenXR：Passthrough
            "ARPlaneFeature",                      // Meta OpenXR：平面偵測（找真實桌面）
        };

        [MenuItem(MenuRoot + "2. 匯入範例並設定專案", priority = 2)]
        static void Run()
        {
            if (!EditorUtility.DisplayDialog("設定 MR 專案",
                    "將會：\n• 匯入 XR Interaction Toolkit 的 Starter Assets 與模擬器範例\n• 設定 OpenXR 與 Meta Quest 功能\n" +
                    "• 調整 URP（關閉 HDR、MSAA 4x）\n• 平台切到 Android（Vulkan、ARM64、IL2CPP）\n\n" +
                    "切換平台會重新匯入資源，需要一些時間。", "開始", "取消"))
                return;

            ImportSamples();
            ConfigureXRManagement();
            ConfigureOpenXRFeatures();
            ConfigureURP();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();

            Debug.Log("[MR 設定] 專案設定完成。請打開 Project Settings → XR Plug-in Management → Project Validation，" +
                      "按「Fix All」處理剩下的建議項目，然後執行「3. 建立 MRCore 與 MR 場景」。");
            SwitchToAndroid();
        }

        static void ImportSamples()
        {
            foreach (var sample in Sample.FindByPackage(XriPackage, null))
            {
                if (!s_samples.Contains(sample.displayName))
                    continue;
                if (sample.isImported)
                {
                    Debug.Log($"[MR 設定] 範例已匯入：{sample.displayName}");
                    continue;
                }
                if (sample.Import())
                    Debug.Log($"[MR 設定] 匯入範例：{sample.displayName}");
                else
                    Debug.LogWarning($"[MR 設定] 無法匯入範例：{sample.displayName}，請從 Package Manager 手動匯入。");
            }
        }

        static void ConfigureXRManagement()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                const string folder = "Assets/XR";
                if (!AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.CreateFolder("Assets", "XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, folder + "/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);

            var settings = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            settings.InitManagerOnStart = true;
            if (XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
                Debug.Log("[MR 設定] Android 已啟用 OpenXR。");
            else
                Debug.Log("[MR 設定] Android 的 OpenXR 已經啟用，或需要到 XR Plug-in Management 手動勾選 OpenXR。");

            // Windows 不啟用 XR，編輯器按 Play 時使用模擬器。要用 Quest Link 測試時，再到 XR Plug-in Management 的 Windows 分頁勾選 OpenXR。
            EditorUtility.SetDirty(perTarget);
        }

        static void ConfigureOpenXRFeatures()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null)
            {
                Debug.LogWarning("[MR 設定] 找不到 Android 的 OpenXR 設定，請到 XR Plug-in Management 勾選 OpenXR 後再執行一次。");
                return;
            }

            var found = new HashSet<string>();
            foreach (var feature in settings.GetFeatures())
            {
                var typeName = feature.GetType().Name;
                if (!s_openXRFeatures.Contains(typeName))
                    continue;
                found.Add(typeName);
                if (!feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                    Debug.Log($"[MR 設定] 啟用 OpenXR 功能：{feature.name}");
                }
            }
            foreach (var missing in s_openXRFeatures.Where(f => !found.Contains(f)))
                Debug.LogWarning($"[MR 設定] 找不到 OpenXR 功能 {missing}，請到 Project Settings → XR Plug-in Management → OpenXR（Android）確認。");
            EditorUtility.SetDirty(settings);
        }

        static void ConfigureURP()
        {
            var assets = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main)
                assets.Add(main);
            for (var i = 0; i < QualitySettings.count; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset quality)
                    assets.Add(quality);
            }

            foreach (var asset in assets)
            {
                // Passthrough 需要透明背景；Quest 上 HDR 與後製的成本也太高。
                asset.supportsHDR = false;
                asset.msaaSampleCount = 4;
                EditorUtility.SetDirty(asset);
                Debug.Log($"[MR 設定] URP：{asset.name} 關閉 HDR、MSAA 4x。");
            }
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            Debug.Log("[MR 設定] Android：IL2CPP、ARM64、最低 API 32、Vulkan。");
        }

        static void SwitchToAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                return;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[MR 設定] 這台電腦的 Unity 沒有安裝 Android Build Support。請在 Unity Hub → Installs → 6000.3.19f1 → Add modules 安裝（含 OpenJDK 與 Android SDK & NDK）。");
                return;
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }
    }
}
