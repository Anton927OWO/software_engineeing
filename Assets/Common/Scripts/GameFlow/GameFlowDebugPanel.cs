using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TrackingNIU
{
    /// <summary>
    /// 流程測試面板，和 GameManager 放在同一個物件上。按 F1 顯示或隱藏。
    /// 地圖還沒做好前用它開新局、進入關卡或模擬通關；正式版本取消勾選 Visible 即可。
    /// </summary>
    public sealed class GameFlowDebugPanel : MonoBehaviour
    {
        [SerializeField] bool visible = true;

        GUIStyle _label;
        GUIStyle _button;

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
#endif
        }

        void OnGUI()
        {
            var manager = GameManager.Instance;
            if (manager == null)
                return;

            _label ??= new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 24 };

            // 以 1080p 為基準縮放，任何解析度看起來大小都一樣。
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (!visible)
            {
                // 隱藏時在左下角留一個小按鈕，避免忘記怎麼打開。
                if (GUI.Button(new Rect(16, 1080 - 56, 220, 40), "測試面板（F1）", _button))
                    visible = true;
                return;
            }

            GUILayout.BeginArea(new Rect(24, 24, 640, 1032), GUI.skin.box);
            GUILayout.Label("流程測試面板（F1 隱藏）", _label);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("開始新的一局", _button, GUILayout.Height(48)))
                manager.StartNewGame();
            if (GUILayout.Button("跳過序章開始", _button, GUILayout.Height(48)))
                manager.StartNewGame(skipPrologue: true);
            if (GUILayout.Button("隱藏", _button, GUILayout.Height(48), GUILayout.Width(100)))
                visible = false;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"文物圖鑑 {ArtifactCollection.Count} / {ArtifactCollection.All.Length}", _label, GUILayout.Width(380));
            if (GUILayout.Button("清除圖鑑紀錄", _button, GUILayout.Height(40)))
                ArtifactCollection.Clear();
            GUILayout.EndHorizontal();

            var run = manager.Run;
            if (run != null)
                DrawRun(manager, run);

            GUILayout.EndArea();
        }

        void DrawRun(GameManager manager, GameRun run)
        {
            GUILayout.Space(12);
            GUILayout.Label($"階段：{run.Phase}｜時間 {manager.PlayTime:0} 秒", _label);
            GUILayout.Label($"總分 {run.TotalScore}｜{run.Title}", _label);
            GUILayout.Label($"文物：{ArtifactNames(run.CollectedArtifacts)}", _label);
            GUILayout.Space(12);

            foreach (var level in LevelCatalog.All)
            {
                var status = run.GetStatus(level.Id);
                if (status == LevelStatus.NotInRun)
                    continue;

                GUILayout.BeginHorizontal();
                var record = run.FindRecord(level.Id);
                var detail = record != null ? $"{record.Score.Total} 分" : StatusText(status);
                GUILayout.Label($"{level.DisplayName}　{detail}", _label, GUILayout.Width(380));

                GUI.enabled = status == LevelStatus.Available && !LevelSession.IsActive;
                if (GUILayout.Button("進入", _button, GUILayout.Height(40)))
                    manager.EnterLevel(level.Id);
                if (GUILayout.Button("模擬通關", _button, GUILayout.Height(40)))
                    manager.SimulateClear(level.Id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        static string StatusText(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Available: return "可進入";
                case LevelStatus.Completed: return "已完成";
                default: return "未開放";
            }
        }

        static string ArtifactNames(IReadOnlyList<ArtifactId> artifacts)
        {
            if (artifacts.Count == 0)
                return "尚未取得";
            var names = new List<string>(artifacts.Count);
            foreach (var artifact in artifacts)
                names.Add(LevelCatalog.GetArtifactName(artifact));
            return string.Join("、", names);
        }
    }
}
