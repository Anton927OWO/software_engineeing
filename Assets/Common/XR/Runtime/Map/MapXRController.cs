using System.Text;
using UnityEngine;

namespace TrackingNIU.XR
{
    /// <summary>
    /// MR 地圖場景（Map_XR）：同一個場景依遊戲階段顯示標題、地圖或結算。
    /// 原型先用空間面板列出關卡，點按鈕進關；立體校園模型之後再加。
    /// 還沒有 MR 版的關卡會顯示「模擬通關」，讓流程可以先走完。
    /// </summary>
    public sealed class MapXRController : MonoBehaviour
    {
        [SerializeField, Tooltip("面板底邊在桌面上的位置（桌面座標）")]
        Vector3 panelPosition = new Vector3(0f, 0.02f, 0.2f);

        WorldPanel _panel;
        GameManager _manager;

        void Start()
        {
            _manager = GameManager.EnsureMixedReality();
            if (!_manager.IsMixedReality)
                Debug.LogWarning("[MR 地圖] GameManager 不是 MR 模式，進關時會載入 2D 版。請在 Boot 的 GameManager 勾選 Mixed Reality。");

            _panel = WorldPanel.Create(transform, panelPosition);
            _manager.RunChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_manager != null)
                _manager.RunChanged -= Refresh;
        }

        void Refresh()
        {
            if (_panel == null)
                return;

            _panel.ClearButtons();
            var run = _manager.Run;
            if (run == null)
                ShowTitle();
            else if (run.Phase == RunPhase.Finished)
                ShowResults(run);
            else
                ShowMap(run);
        }

        void ShowTitle()
        {
            _panel.SetText("尋跡宜大",
                "MR 原型\n用扳機鍵點按鈕，用握把鍵抓東西。\n按住右手 B 鍵可以把桌面重新擺到面前。");
            _panel.AddButton("開始遊戲", () => _manager.StartNewGame());
            _panel.AddButton("跳過序章", () => _manager.StartNewGame(skipPrologue: true));
        }

        void ShowMap(GameRun run)
        {
            var mapName = run.CurrentMap == MapKind.Campus ? "無圍牆博物館" : "圖資館";
            var text = new StringBuilder();
            text.Append("目前總分 ").Append(run.TotalScore).Append("｜文物 ").Append(run.CollectedArtifacts.Count).Append(" 件\n");

            var available = run.GetAvailableLevels();
            if (available.Count == 0)
                text.Append("這一站沒有可以進入的關卡。");
            else
                text.Append("選擇下一站：");
            _panel.SetText(mapName, text.ToString());

            foreach (var id in available)
            {
                var level = id;
                var definition = LevelCatalog.Get(level);
                if (_manager.HasLevelScene(level))
                    _panel.AddButton(definition.DisplayName, () => _manager.EnterLevel(level));
                else
                    _panel.AddButton(definition.DisplayName + "（模擬通關）", () => _manager.SimulateClear(level));
            }
            _panel.AddButton("回到標題", _manager.ReturnToTitle);
        }

        void ShowResults(GameRun run)
        {
            var text = new StringBuilder();
            foreach (var record in run.Records)
            {
                if (!record.Score.IsScored)
                    continue;
                text.Append(LevelCatalog.Get(record.LevelId).DisplayName).Append("　").Append(record.Score.Total).Append('\n');
            }
            text.Append("總分 ").Append(run.TotalScore).Append("｜").Append(run.Title);
            text.Append("\n遊玩時間 ").Append(Mathf.FloorToInt(_manager.PlayTime / 60f)).Append(" 分 ")
                .Append(Mathf.FloorToInt(_manager.PlayTime % 60f)).Append(" 秒");

            _panel.SetText("旅程完成", text.ToString());
            _panel.AddButton("再玩一次", () => _manager.StartNewGame());
            _panel.AddButton("回到標題", _manager.ReturnToTitle);
        }
    }
}
