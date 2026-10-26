using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>
    /// 地圖畫面：顯示地點與關卡標記、本局任務、文物收集，並用「前往」按鈕進入選取的關卡。
    /// 圖資館樓層地圖與無圍牆博物館地圖共用這個元件，只是地點不同。
    /// 直接打開地圖 Scene 按 Play 時，會建立預覽用的 GameManager 並跳過序章。
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] MapLocation[] locations;
        [SerializeField] TaskRow[] taskRows;
        [SerializeField] ArtifactSlot[] artifactSlots;
        [SerializeField] TMP_Text selectedName;
        [SerializeField] TMP_Text selectedInfo;
        [SerializeField] Button goButton;
        [SerializeField] Image goButtonFill;
        [SerializeField] TMP_Text goLabel;

        [Header("音效（共用音效到位後再指定）")]
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip clickSound;

        readonly List<MapMarker> _markers = new List<MapMarker>();
        LevelId? _selected;

        GameRun Run => GameManager.Instance.Run;

        void Start()
        {
            EnsureGameManager();

            foreach (var location in locations)
            {
                var current = location;
                current.Button.onClick.AddListener(() => OnLocationClicked(current));
                foreach (var marker in current.Markers)
                {
                    var levelId = marker.LevelId;
                    marker.Button.onClick.AddListener(() => Select(levelId));
                    _markers.Add(marker);
                }
            }
            goButton.onClick.AddListener(OnGoClicked);

            GameManager.Instance.RunChanged += OnRunChanged;
            SelectDefault();
            Refresh();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RunChanged -= OnRunChanged;
        }

        static void EnsureGameManager()
        {
            if (GameManager.Instance != null && GameManager.Instance.Run != null)
                return;

            if (GameManager.Instance == null)
            {
                Debug.Log("[地圖] 沒有 GameManager，建立預覽用的 GameManager 並跳過序章。正式遊玩請從 Boot 場景開始。");
                var manager = new GameObject("GameManager（預覽）");
                manager.AddComponent<GameManager>();
                manager.AddComponent<GameFlowDebugPanel>().Visible = false;
            }
            GameManager.Instance.StartNewGame(skipPrologue: true);
        }

        void OnRunChanged()
        {
            if (Run == null)
                return;
            if (!_selected.HasValue || !Run.CanEnter(_selected.Value))
                SelectDefault();
            Refresh();
        }

        void OnLocationClicked(MapLocation location)
        {
            if (location.TryPickLevel(Run, out var levelId))
                Select(levelId);
        }

        void Select(LevelId levelId)
        {
            PlayClick();
            _selected = levelId;
            Refresh();
        }

        /// <summary>預設選取任務清單中第一個可以進入、且在這張地圖上的關卡。</summary>
        void SelectDefault()
        {
            _selected = null;
            foreach (var levelId in GetTaskOrder(Run))
            {
                if (Run.CanEnter(levelId) && _markers.Exists(marker => marker.LevelId == levelId))
                {
                    _selected = levelId;
                    return;
                }
            }
        }

        /// <summary>任務清單的順序：館內兩關、戶外一關、終章。</summary>
        static List<LevelId> GetTaskOrder(GameRun run)
        {
            var tasks = new List<LevelId>(run.IndoorLevels);
            tasks.AddRange(run.OutdoorLevels);
            tasks.Add(LevelId.Finale);
            return tasks;
        }

        void OnGoClicked()
        {
            PlayClick();
            if (Run.Phase == RunPhase.Outdoor || Run.Phase == RunPhase.Finished)
                GameManager.Instance.ReturnToMap();
            else if (_selected.HasValue)
                GameManager.Instance.EnterLevel(_selected.Value);
        }

        void Refresh()
        {
            var run = Run;
            subtitle.text = GetSubtitle(run);
            scoreText.text = $"總分 {run.TotalScore}";

            foreach (var location in locations)
                location.Refresh(run);
            foreach (var marker in _markers)
                marker.Refresh(run.GetStatus(marker.LevelId), marker.LevelId == _selected);

            RefreshTasks(run);
            for (int i = 0; i < artifactSlots.Length; i++)
                artifactSlots[i].Show(i < run.CollectedArtifacts.Count ? run.CollectedArtifacts[i] : ArtifactId.None);

            RefreshSelection(run);
        }

        void RefreshTasks(GameRun run)
        {
            var tasks = GetTaskOrder(run);
            for (int i = 0; i < taskRows.Length; i++)
            {
                if (i >= tasks.Count)
                {
                    taskRows[i].Hide();
                    continue;
                }

                var definition = LevelCatalog.Get(tasks[i]);
                var status = run.GetStatus(definition.Id);
                var record = run.FindRecord(definition.Id);
                string state = record != null ? $"{record.Score.Total} 分"
                    : status == LevelStatus.Available ? "可以前往"
                    : "尚未開放";
                taskRows[i].Show(definition.DisplayName, $"{state}｜{definition.Location}", status);
            }
        }

        void RefreshSelection(GameRun run)
        {
            if (run.Phase == RunPhase.Outdoor)
            {
                ShowSelection("館內任務完成", "下一站是校園中的無圍牆博物館", "前往校園地圖", true);
                return;
            }
            if (run.Phase == RunPhase.Finished)
            {
                ShowSelection("本局完成", $"總分 {run.TotalScore}｜{run.Title}", "查看結算", true);
                return;
            }
            if (!_selected.HasValue)
            {
                ShowSelection("選擇樓層上的任務", "", "前往", false);
                return;
            }

            var definition = LevelCatalog.Get(_selected.Value);
            var status = run.GetStatus(definition.Id);
            string info = definition.TimeLimit > 0f
                ? $"{definition.Location}｜限時 {definition.TimeLimit:0} 秒"
                : definition.Location;
            string label = status == LevelStatus.Completed ? "已完成"
                : status == LevelStatus.Available ? "前往"
                : "尚未開放";
            ShowSelection(definition.DisplayName, info, label, status == LevelStatus.Available);
        }

        void ShowSelection(string title, string info, string buttonLabel, bool canGo)
        {
            selectedName.text = title;
            selectedInfo.text = info;
            goLabel.text = buttonLabel;
            goButton.interactable = canGo;
            goButtonFill.color = canGo ? UiPalette.Primary : UiPalette.Disabled;
        }

        static string GetSubtitle(GameRun run)
        {
            switch (run.Phase)
            {
                case RunPhase.Indoor:
                    int done = 0;
                    foreach (var levelId in run.IndoorLevels)
                    {
                        if (run.FindRecord(levelId) != null)
                            done++;
                    }
                    return $"第一章　在圖資館找回文物（{done} / {run.IndoorLevels.Count}）";
                case RunPhase.Outdoor:
                    return "第二章　前往校園中的無圍牆博物館";
                case RunPhase.Finale:
                    return "終章　帶著三件文物回到 2F 宜大記憶庫";
                case RunPhase.Finished:
                    return "探索完成";
                default:
                    return "序章　知識之林";
            }
        }

        void PlayClick()
        {
            if (sfxSource != null && clickSound != null)
                sfxSource.PlayOneShot(clickSound);
        }
    }
}
