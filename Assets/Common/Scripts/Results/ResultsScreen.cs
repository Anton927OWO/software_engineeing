using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackingNIU
{
    /// <summary>
    /// 結算畫面（《關卡畫面設計說明》圖 4）：① 總分由 0 跑到最終分數 ② 稱號 ③ 各關分數
    /// ④ 文物圖鑑（跨局保存）。另外顯示本局用時，並提供「結束」與「重新遊玩」。
    /// 直接打開 Results 場景按 Play 時，會建立預覽用的 GameManager 並把所有關卡模擬通關。
    /// </summary>
    public sealed class ResultsScreen : MonoBehaviour
    {
        const float CountDuration = 1.6f;
        const float BadgePopDuration = 0.25f;

        [SerializeField] TMP_Text playTimeText;
        [SerializeField] TMP_Text totalText;
        [SerializeField] RectTransform titleBadge;
        [SerializeField] TMP_Text titleText;
        [SerializeField] ResultScoreRow[] scoreRows;
        [SerializeField] ResultArtifactCard[] artifactCards;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button quitButton;
        [SerializeField] Button replayButton;

        [Header("音效（結算 BGM 到位後再指定）")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip music;
        [SerializeField] AudioClip clickSound;
        [Tooltip("總分跑完時播放")]
        [SerializeField] AudioClip finishSound;

        void Start()
        {
            EnsureFinishedRun();

            quitButton.onClick.AddListener(OnQuitClicked);
            replayButton.onClick.AddListener(OnReplayClicked);

            var run = GameManager.Instance.Run;
            playTimeText.text = $"用時 {FormatTime(GameManager.Instance.PlayTime)}";
            titleText.text = run.Title;
            ShowScores(run);
            ShowCollection(run);

            if (musicSource != null && music != null)
            {
                musicSource.clip = music;
                musicSource.loop = true;
                musicSource.Play();
            }
            StartCoroutine(CountUp(run.TotalScore));
        }

        void EnsureFinishedRun()
        {
            if (GameManager.Instance != null && GameManager.Instance.Run?.Phase == RunPhase.Finished)
                return;

            if (GameManager.Instance == null)
            {
                Debug.Log("[結算] 沒有 GameManager，建立預覽用的 GameManager 並模擬完成一局。正式遊玩請從 Boot 場景開始。");
                var manager = new GameObject("GameManager（預覽）");
                manager.AddComponent<GameManager>();
                manager.AddComponent<GameFlowDebugPanel>().Visible = false;
            }
            GameManager.Instance.StartPreviewResults();
        }

        /// <summary>依遊玩順序列出計分的關卡，序章不計分所以不列。</summary>
        void ShowScores(GameRun run)
        {
            var scored = new List<LevelRecord>();
            foreach (var record in run.Records)
            {
                if (record.Score.IsScored)
                    scored.Add(record);
            }

            for (int i = 0; i < scoreRows.Length; i++)
            {
                if (i < scored.Count)
                    scoreRows[i].Show(LevelCatalog.Get(scored[i].LevelId).DisplayName, scored[i].Score.Total, scored[i].Result.Cleared);
                else
                    scoreRows[i].Hide();
            }
        }

        void ShowCollection(GameRun run)
        {
            int collectedCount = 0;
            foreach (var card in artifactCards)
            {
                bool thisRun = Contains(run.CollectedArtifacts, card.Artifact);
                // 預覽局不寫進圖鑑，所以本局取得的也要算成已收集。
                bool collected = thisRun || ArtifactCollection.Has(card.Artifact);
                card.Show(collected, thisRun);
                if (collected)
                    collectedCount++;
            }
            progressText.text = $"文物圖鑑 {collectedCount} / {artifactCards.Length}";
        }

        IEnumerator CountUp(int total)
        {
            titleBadge.localScale = Vector3.zero;
            totalText.text = "0";

            for (float t = 0f; t < CountDuration; t += Time.unscaledDeltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - t / CountDuration, 3f);
                totalText.text = FormatScore(Mathf.RoundToInt(total * eased));
                yield return null;
            }
            totalText.text = FormatScore(total);
            if (sfxSource != null && finishSound != null)
                sfxSource.PlayOneShot(finishSound);

            // 稱號從小彈出，略微超過再回到原尺寸。
            for (float t = 0f; t < BadgePopDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / BadgePopDuration;
                titleBadge.localScale = Vector3.one * (p < 0.7f ? Mathf.Lerp(0f, 1.08f, p / 0.7f) : Mathf.Lerp(1.08f, 1f, (p - 0.7f) / 0.3f));
                yield return null;
            }
            titleBadge.localScale = Vector3.one;
        }

        void OnQuitClicked() => StartCoroutine(ClickThen(() => GameManager.Instance.ReturnToTitle()));

        void OnReplayClicked() => StartCoroutine(ClickThen(() => GameManager.Instance.StartNewGame()));

        /// <summary>播完點擊音效再切換場景，否則音效會被切掉。等待期間停用按鈕，避免連點。</summary>
        IEnumerator ClickThen(System.Action action)
        {
            quitButton.interactable = false;
            replayButton.interactable = false;
            if (sfxSource != null && clickSound != null)
            {
                sfxSource.PlayOneShot(clickSound);
                yield return new WaitForSecondsRealtime(clickSound.length);
            }
            action();
        }

        static string FormatScore(int score) => score.ToString("N0", CultureInfo.InvariantCulture);

        static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return total >= 60 ? $"{total / 60} 分 {total % 60:00} 秒" : $"{total} 秒";
        }

        static bool Contains(IReadOnlyList<ArtifactId> artifacts, ArtifactId artifact)
        {
            foreach (var item in artifacts)
            {
                if (item == artifact)
                    return true;
            }
            return false;
        }
    }
}
