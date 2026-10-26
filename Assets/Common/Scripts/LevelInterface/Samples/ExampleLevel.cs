using UnityEngine;

namespace TrackingNIU.Samples
{
    /// <summary>
    /// 關卡腳本範本。複製到自己的關卡資料夾（例如 Assets/Levels/Dig），
    /// 把 class 名稱改成自己的（例如 DigLevel），namespace 改成 TrackingNIU.Levels，再填入玩法。
    /// 測試方式：Scene 中放 LevelTestLauncher 和這個腳本，按 Play 後在 Inspector 右上角選單
    /// 執行「測試：失誤一次」「測試：通關」，再執行「測試：知識卡按繼續」，Console 會印出結果。
    /// </summary>
    public sealed class ExampleLevel : MonoBehaviour
    {
        LevelContext _context;
        float _remainingTime;
        int _mistakes;
        bool _playing;

        void Start()
        {
            _context = LevelSession.Current;
            if (_context == null)
            {
                Debug.LogError("找不到 LevelSession.Current，請在 Scene 中加上 LevelTestLauncher。");
                enabled = false;
                return;
            }

            // TODO：在這裡顯示空間介紹（共用版型 10/29 交出前先用文字畫面代替），
            // 玩家按「開始挑戰」後呼叫 StartMiniGame()。
            StartMiniGame();
        }

        public void StartMiniGame()
        {
            _remainingTime = _context.TimeLimit;
            _mistakes = 0;
            _playing = true;
        }

        void Update()
        {
            if (!_playing || !_context.HasTimeLimit)
                return;

            // 暫停選單把 Time.timeScale 設為 0 時，計時會自動停止。
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                EndMiniGame(cleared: false);
            }
        }

        /// <summary>放錯、答錯、損壞時呼叫。扣分由 ScoreCalculator 計算，關卡只要計次。</summary>
        public void AddMistake()
        {
            if (_playing)
                _mistakes++;
        }

        void EndMiniGame(bool cleared)
        {
            if (!_playing)
                return;
            _playing = false;

            var score = LevelSession.Report(new LevelResult
            {
                Cleared = cleared,
                RemainingTime = _remainingTime,
                Mistakes = _mistakes,
                // 終章另外填 MatchedPairs。
            });

            // TODO：顯示知識卡，得分明細使用 score.ClearBonus、score.TimeBonus、score.MistakePenalty、score.Total。
            // 失敗時可以多放一個「再試一次」按鈕呼叫 LevelSession.Retry()。
            Debug.Log($"顯示知識卡：{score}");
        }

        /// <summary>知識卡的「繼續」按鈕呼叫這個方法。</summary>
        public void OnKnowledgeCardContinue() => LevelSession.Exit();

        [ContextMenu("測試：失誤一次")]
        void TestMistake() => AddMistake();

        [ContextMenu("測試：通關")]
        void TestClear() => EndMiniGame(cleared: true);

        [ContextMenu("測試：失敗")]
        void TestFail() => EndMiniGame(cleared: false);

        [ContextMenu("測試：知識卡按繼續")]
        void TestContinue() => OnKnowledgeCardContinue();
    }
}
