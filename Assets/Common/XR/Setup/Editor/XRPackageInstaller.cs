using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace TrackingNIU.XR.Setup
{
    /// <summary>
    /// MR 原型的第 1 步：安裝 XR 套件。
    /// 這個組件不依賴任何 XR 套件，所以在還沒安裝套件的專案也能編譯執行。
    /// 不指定版本，由 Package Manager 安裝這個 Unity 版本建議的版本。
    /// </summary>
    public static class XRPackageInstaller
    {
        public const string MenuRoot = "尋跡宜大/MR 原型/";

        static readonly string[] s_packages =
        {
            "com.unity.xr.management",
            "com.unity.xr.openxr",
            "com.unity.xr.arfoundation",
            "com.unity.xr.meta-openxr",
            "com.unity.xr.interaction.toolkit",
        };

        static AddAndRemoveRequest s_request;

        [MenuItem(MenuRoot + "1. 安裝 XR 套件", priority = 1)]
        static void Install()
        {
            if (s_request != null && !s_request.IsCompleted)
            {
                Debug.LogWarning("[MR 設定] 套件還在安裝中，請稍候。");
                return;
            }

            if (!EditorUtility.DisplayDialog("安裝 XR 套件",
                    "將安裝以下套件（版本由 Package Manager 決定）：\n\n" + string.Join("\n", s_packages) +
                    "\n\n安裝後 Unity 會重新編譯，完成後請執行「2. 匯入範例並設定專案」。", "安裝", "取消"))
                return;

            s_request = Client.AddAndRemove(s_packages, null);
            EditorApplication.update += Poll;
            Debug.Log("[MR 設定] 開始安裝 XR 套件……");
        }

        static void Poll()
        {
            if (s_request == null || !s_request.IsCompleted)
                return;

            EditorApplication.update -= Poll;
            if (s_request.Status == StatusCode.Success)
                Debug.Log("[MR 設定] XR 套件安裝完成。等待編譯結束後，執行「尋跡宜大 → MR 原型 → 2. 匯入範例並設定專案」。");
            else
                Debug.LogError($"[MR 設定] 安裝失敗：{s_request.Error?.message}");
            s_request = null;
        }
    }
}
