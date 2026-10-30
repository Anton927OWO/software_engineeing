using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace XunJiYiDa.Levels.Finale.Editor
{
    // Runs the installed Unity Test Framework in the currently open Editor.
    [InitializeOnLoad]
    public static class FinaleTestRunner
    {
        private const string ReportRoot = "Assets/Levels/Finale/Verification";
        private static TestRunnerApi api;

        static FinaleTestRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
        }

        [MenuItem("尋跡宜大/Finale/驗證 Finale EditMode")]
        public static void RunFinaleEditMode() => Run(TestMode.EditMode, false);

        [MenuItem("尋跡宜大/Finale/驗證全部 EditMode（含 Shared）")]
        public static void RunAllEditMode() => Run(TestMode.EditMode, true);

        [MenuItem("尋跡宜大/Finale/驗證全部 PlayMode（含 Shared）")]
        public static void RunAllPlayMode() => Run(TestMode.PlayMode, true);

        private static void Run(TestMode mode, bool all)
        {
            Directory.CreateDirectory(ReportRoot);
            SessionState.SetString("Finale.TestReport", ReportRoot + "/" +
                (all ? "All" : "Finale") + mode + ".xml");
            var filter = new Filter { testMode = mode };
            string suffix = mode == TestMode.EditMode ? "Editor.Tests" : "PlayMode.Tests";
            filter.assemblyNames = all ? new[] { "XunJiYiDa.Levels.Finale." + suffix, "XunJiYiDa.Levels.Shared." + suffix }
                : new[] { "XunJiYiDa.Levels.Finale." + suffix };
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string path = SessionState.GetString("Finale.TestReport", "");
                if (string.IsNullOrEmpty(path)) return;
                TestRunnerApi.SaveResultToFile(result, path);
                File.WriteAllText(path + ".summary.txt",
                    $"Unity {Application.unityVersion}\nPassed: {result.PassCount}\nFailed: {result.FailCount}\nSkipped: {result.SkipCount}\n{result.ResultState}\n");
                SessionState.EraseString("Finale.TestReport");
                Debug.Log($"Finale verification: {result.PassCount} passed, {result.FailCount} failed. {path}");
            }
        }
    }
}
