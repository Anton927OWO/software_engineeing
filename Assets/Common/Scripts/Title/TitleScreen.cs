using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrackingNIU
{
    /// <summary>
    /// 標題畫面（Boot 場景）：遊戲名稱、「開始遊戲」「離開遊戲」與跨局文物圖鑑。
    /// 按「開始遊戲」後抽關，用翻牌顯示這一局的路線（館內 2 關、戶外 1 關），玩家按「出發」才進入序章。
    /// 結算畫面的「重新遊玩」回到這裡時會直接開始抽關。
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        const float OverlayFadeDuration = 0.2f;
        const float FirstFlipDelay = 0.5f;
        const float FlipGap = 0.25f;

        [SerializeField] Button startButton;
        [SerializeField] Button quitButton;
        [SerializeField] ResultArtifactCard[] artifactCards;
        [SerializeField] TMP_Text progressText;

        [Header("抽關翻牌")]
        [SerializeField] CanvasGroup drawOverlay;
        [Tooltip("依序是館內兩關、戶外一關")]
        [SerializeField] DrawCard[] drawCards;
        [SerializeField] Button departButton;

        [Header("音效")]
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioClip music;
        [SerializeField] AudioClip clickSound;
        [Tooltip("每翻開一張卡播放")]
        [SerializeField] AudioClip flipSound;

        void Start()
        {
            startButton.onClick.AddListener(OnStartClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
            departButton.onClick.AddListener(OnDepartClicked);
            // WebGL 沒有「離開」可言，只在電腦版顯示。
            quitButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);

            ShowCollection();
            HideOverlay();

            if (musicSource != null && music != null)
            {
                musicSource.clip = music;
                musicSource.loop = true;
                musicSource.Play();
            }

            if (GameManager.Instance != null && GameManager.Instance.ConsumeRedrawRequest())
                StartCoroutine(RevealDraw());
        }

        void ShowCollection()
        {
            foreach (var card in artifactCards)
                card.Show(ArtifactCollection.Has(card.Artifact), false);
            progressText.text = $"文物圖鑑 {ArtifactCollection.Count} / {artifactCards.Length}";
        }

        void HideOverlay()
        {
            drawOverlay.alpha = 0f;
            drawOverlay.blocksRaycasts = false;
            drawOverlay.gameObject.SetActive(false);
        }

        void OnStartClicked() => StartCoroutine(ClickThen(() => StartCoroutine(RevealDraw())));

        void OnQuitClicked() => StartCoroutine(ClickThen(Quit));

        void OnDepartClicked() => StartCoroutine(ClickThen(() => GameManager.Instance.BeginRun()));

        IEnumerator RevealDraw()
        {
            var manager = GameManager.Instance;
            manager.DrawNewRun();
            var run = manager.Run;

            foreach (var card in drawCards)
                card.ShowBack();
            departButton.gameObject.SetActive(false);
            drawOverlay.gameObject.SetActive(true);
            drawOverlay.blocksRaycasts = true;
            for (float t = 0f; t < OverlayFadeDuration; t += Time.unscaledDeltaTime)
            {
                drawOverlay.alpha = t / OverlayFadeDuration;
                yield return null;
            }
            drawOverlay.alpha = 1f;

            yield return new WaitForSecondsRealtime(FirstFlipDelay);
            int index = 0;
            foreach (var level in run.IndoorLevels)
                yield return FlipCard(index++, level);
            foreach (var level in run.OutdoorLevels)
                yield return FlipCard(index++, level);

            departButton.interactable = true;
            departButton.gameObject.SetActive(true);
        }

        IEnumerator FlipCard(int index, LevelId level)
        {
            if (index >= drawCards.Length)
            {
                Debug.LogWarning($"[標題] 翻牌卡只有 {drawCards.Length} 張，{level} 沒有顯示。");
                yield break;
            }
            if (sfxSource != null && flipSound != null)
                sfxSource.PlayOneShot(flipSound);
            yield return drawCards[index].Flip(LevelCatalog.Get(level));
            yield return new WaitForSecondsRealtime(FlipGap);
        }

        /// <summary>播完點擊音效再執行，否則切換場景會把音效切掉。等待期間停用按鈕，避免連點。</summary>
        IEnumerator ClickThen(System.Action action)
        {
            startButton.interactable = false;
            quitButton.interactable = false;
            departButton.interactable = false;
            if (sfxSource != null && clickSound != null)
            {
                sfxSource.PlayOneShot(clickSound);
                yield return new WaitForSecondsRealtime(clickSound.length);
            }
            startButton.interactable = true;
            quitButton.interactable = true;
            action();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
