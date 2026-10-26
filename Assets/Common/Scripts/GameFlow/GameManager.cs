using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackingNIU
{
    /// <summary>
    /// 管理一局遊戲的流程：抽關、進入關卡、記錄結果與文物、切換地圖與結算。
    /// 放在 Boot 場景，跨場景保留。地圖與結算畫面透過 GameManager.Instance.Run 讀取狀態。
    /// 流程：序章 → 圖資館樓層地圖（館內 2 關）→ 無圍牆博物館地圖（戶外 1 關）→ 圖資館樓層地圖（終章）→ 結算。
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] string libraryMapScene = "Map_Library";
        [SerializeField] string campusMapScene = "Map_Campus";
        [SerializeField] string resultsScene = "Results";

        [SerializeField, Tooltip("期末加入黃春明關後勾選，館內改為 5 抽 2")]
        bool includeFinalsLevels;

        [SerializeField, Tooltip("固定亂數種子，方便重現同一組抽關；0 代表每局不同")]
        int randomSeed;

        string _bootScene;
        float _startTime;
        float? _finishTime;
        System.Random _random;

        public static GameManager Instance { get; private set; }

        /// <summary>目前這一局；還沒開始時為 null。</summary>
        public GameRun Run { get; private set; }

        /// <summary>本局遊玩秒數。用 Time.time 計算，暫停（timeScale = 0）時不計。</summary>
        public float PlayTime => Run == null ? 0f : (_finishTime ?? Time.time) - _startTime;

        /// <summary>抽關、完成關卡或開新局時觸發，地圖可以藉此更新標記。</summary>
        public event Action RunChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _bootScene = SceneManager.GetActiveScene().name;
            _random = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>重新抽關並從序章開始。結算畫面的「重新遊玩」也呼叫這個。</summary>
        /// <param name="skipPrologue">測試用：直接把序章記為完成，進入圖資館樓層地圖。</param>
        public void StartNewGame(bool skipPrologue = false)
        {
            if (LevelSession.IsActive)
                LevelSession.Abort();

            Run = new GameRun(includeFinalsLevels, _random);
            _startTime = Time.time;
            _finishTime = null;
            Debug.Log($"[GameManager] 新的一局｜館內：{Names(Run.IndoorLevels)}｜戶外：{Names(Run.OutdoorLevels)}");
            RunChanged?.Invoke();

            if (skipPrologue)
                SimulateClear(LevelId.Prologue);
            else
                EnterLevel(LevelId.Prologue);
        }

        /// <summary>地圖上的「前往」按鈕呼叫這個。關卡目前不能進入時回傳 false。</summary>
        public bool EnterLevel(LevelId levelId)
        {
            if (!CanStartLevel(levelId))
                return false;

            var definition = LevelCatalog.Get(levelId);
            LevelSession.Begin(new LevelContext(definition, Run.CollectedArtifacts), OnLevelExit);
            if (SceneLoader.Load(definition.SceneName))
                return true;

            LevelSession.Abort();
            return false;
        }

        /// <summary>
        /// 測試用：不載入關卡 Scene，直接以「通關、剩一半時間、零失誤」記錄結果。
        /// 其他人的關卡還沒做好時，可以用它走完整個流程。
        /// </summary>
        public void SimulateClear(LevelId levelId)
        {
            if (!CanStartLevel(levelId))
                return;

            var definition = LevelCatalog.Get(levelId);
            LevelSession.Begin(new LevelContext(definition, Run.CollectedArtifacts), OnLevelExit);
            LevelSession.Report(new LevelResult
            {
                Cleared = true,
                RemainingTime = definition.TimeLimit / 2f,
                MatchedPairs = ScoreCalculator.FinalePairs,
            });
            LevelSession.Exit();
        }

        /// <summary>回到目前階段該顯示的地圖或結算畫面。</summary>
        public void ReturnToMap() => LoadScene(GetSceneForPhase());

        /// <summary>結束這一局，回到 Boot 場景。</summary>
        public void ReturnToTitle()
        {
            if (LevelSession.IsActive)
                LevelSession.Abort();
            Run = null;
            RunChanged?.Invoke();
            LoadScene(_bootScene);
        }

        bool CanStartLevel(LevelId levelId)
        {
            if (Run == null)
            {
                Debug.LogWarning("[GameManager] 還沒開始遊戲，請先呼叫 StartNewGame()。");
                return false;
            }
            if (LevelSession.IsActive)
            {
                Debug.LogWarning($"[GameManager] {LevelSession.Current.LevelId} 還在進行中，不能進入 {levelId}。");
                return false;
            }
            if (!Run.CanEnter(levelId))
            {
                Debug.LogWarning($"[GameManager] {levelId} 目前的狀態是 {Run.GetStatus(levelId)}，不能進入。");
                return false;
            }
            return true;
        }

        void OnLevelExit(LevelResult result, ScoreBreakdown score)
        {
            Run.Complete(result, score);
            if (Run.Phase == RunPhase.Finished)
                _finishTime = Time.time;

            Debug.Log($"[GameManager] 完成 {result}｜{score}｜目前總分 {Run.TotalScore}｜階段 {Run.Phase}");
            RunChanged?.Invoke();
            ReturnToMap();
        }

        string GetSceneForPhase()
        {
            switch (Run?.Phase)
            {
                case RunPhase.Indoor:
                case RunPhase.Finale:
                    return libraryMapScene;
                case RunPhase.Outdoor:
                    return campusMapScene;
                case RunPhase.Finished:
                    return resultsScene;
                default:
                    return _bootScene;
            }
        }

        void LoadScene(string sceneName)
        {
            // 地圖與結算畫面還沒做好之前，先回到 Boot 場景，用流程測試面板操作。
            if (!SceneLoader.Exists(sceneName))
            {
                Debug.LogWarning($"[GameManager] 還沒有 {sceneName} 場景，先回到 {_bootScene}。");
                sceneName = _bootScene;
            }

            if (SceneManager.GetActiveScene().name != sceneName)
                SceneLoader.Load(sceneName);
        }

        static string Names(IReadOnlyList<LevelId> levels)
        {
            var names = new List<string>(levels.Count);
            foreach (var level in levels)
                names.Add(LevelCatalog.Get(level).DisplayName);
            return string.Join("、", names);
        }
    }
}
