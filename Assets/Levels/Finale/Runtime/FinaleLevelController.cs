using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Finale
{
    [Serializable]
    public sealed class FinaleArtifact
    {
        public string id;
        public string title;
        public Sprite face;
        public Sprite icon;
    }

    public sealed class FinaleLevelController : MonoBehaviour, IFinalePairProgress
    {
        public const float HintSeconds = 2f;
        public const float MismatchSeconds = 0.8f;
        public const float FlipSeconds = 0.18f;
        [SerializeField] private SharedLevelController controller;
        [SerializeField] private SharedLevelUI ui;
        [SerializeField] private SharedAudioPlayer audioPlayer;
        [SerializeField] private FinaleArtifactSource artifactSource;
        [SerializeField] private TextAsset artifactFacts;
        [SerializeField] private FinaleArtifact[] artifacts;
        private FinaleArtifactCatalog facts;
        [SerializeField] private Sprite cardBack;
        [SerializeField] private FinaleCardView[] cards;
        [SerializeField] private TMP_Text progress;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private Button introductionButton;
        [SerializeField] private Button knowledgeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private GameObject hintPanel;
        [SerializeField] private Image[] hintImages;
        [SerializeField] private TMP_Text[] hintLabels;
        private bool awaitingHint;
        private bool busy;
        private float hintElapsed;
        private int first = -1;
        private string[] acquiredIds = Array.Empty<string>();

        public FinaleMemoryBoard Board { get; private set; }
        public int MatchedPairs => Board != null ? Board.MatchedPairs : 0;
        public IReadOnlyList<FinaleCardView> Cards => cards;
        public IReadOnlyList<string> AcquiredArtifactIds => Array.AsReadOnly(acquiredIds);
        public bool IsShowingHint => hintPanel.activeSelf;
        public bool IsBusy => busy || IsShowingHint;
        public bool CanFlip => isActiveAndEnabled && !awaitingHint && !IsBusy && controller.CanInteract;

        private void Awake()
        {
            facts = JsonUtility.FromJson<FinaleArtifactCatalog>(artifactFacts.text);
            for (int i = 0; i < cards.Length; i++) cards[i].Bind(i, index => TryFlip(index));
            ResetAttempt();
        }

        private void OnEnable()
        {
            controller.Retried += ResetAttempt;
            controller.Completed += OnCompleted;
            controller.Session.StateChanged += OnState;
            introductionButton.onClick.AddListener(ShowSpaceIntroduction);
            knowledgeButton.onClick.AddListener(ShowKnowledge);
            restartButton.onClick.AddListener(controller.Retry);
            if (controller.Session.Result != null) OnCompleted(controller.Session.Result);
            else OnState(controller.Session.State);
        }

        private void OnDisable()
        {
            controller.Retried -= ResetAttempt;
            controller.Completed -= OnCompleted;
            controller.Session.StateChanged -= OnState;
            introductionButton.onClick.RemoveListener(ShowSpaceIntroduction);
            knowledgeButton.onClick.RemoveListener(ShowKnowledge);
            restartButton.onClick.RemoveListener(controller.Retry);
            // Deactivating a GameObject stops its coroutines permanently. Roll back only unfinished
            // selections; keep completed pairs, the hint clock, elapsed time and mistake count.
            StopAllCoroutines();
            if (Board == null) return;
            Board.CancelPendingSelection();
            first = -1; busy = false;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                cards[i].SetFace(Board.IsMatched(i));
                cards[i].SetFlipScale(1);
                cards[i].SetInteractable(false);
            }
        }

        private void OnState(LevelState state)
        {
            if (state != LevelState.Running || !awaitingHint) return;
            awaitingHint = false;
            acquiredIds = artifactSource.Capture();
            for (int i = 0; i < acquiredIds.Length; i++)
            {
                var artifact = Array.Find(artifacts, item => item.id == acquiredIds[i]);
                hintImages[i].sprite = artifact.icon;
                hintLabels[i].text = artifact.title;
                controller.CollectArtifact(acquiredIds[i]);
            }
            hintElapsed = 0;
            hintPanel.SetActive(true);
            controller.Session.SetPaused(PauseReason.Modal, true);
            RefreshInteractivity();
        }

        private void Update()
        {
            // The hint owns Modal while visible. Manual/focus pauses stop its two-second clock.
            if (IsShowingHint && controller.Session.Result == null &&
                controller.Session.PauseReasons == PauseReason.Modal)
            {
                hintElapsed += Time.unscaledDeltaTime;
                if (hintElapsed >= HintSeconds)
                {
                    hintPanel.SetActive(false);
                    controller.Session.SetPaused(PauseReason.Modal, false);
                }
            }
            RefreshInteractivity();
        }

        private void ResetAttempt()
        {
            StopAllCoroutines();
            busy = false; awaitingHint = true; hintElapsed = 0; first = -1;
            acquiredIds = Array.Empty<string>();
            hintPanel.SetActive(false);
            Board = new FinaleMemoryBoard(UnityEngine.Random.Range(0, int.MaxValue));
            for (int i = 0; i < cards.Length; i++) cards[i].ResetCard(artifacts[Board.ArtifactAt(i)].face, cardBack);
            progress.text = "記憶拼圖　0 / 6";
            feedback.text = "準備好後，開始找回文物記憶。";
            RefreshInteractivity();
        }

        public bool TryFlip(int index)
        {
            if (!CanFlip || !Board.TryReveal(index)) return false;
            busy = true; // Acquire the lock before starting any animation or allowing another click.
            audioPlayer?.PlayClick();
            bool second = Board.PairReady;
            int previous = first;
            if (!second) first = index;
            else if (!Board.PairMatches) controller.RecordMistake();
            StartCoroutine(RevealAndResolve(index, previous, second));
            RefreshInteractivity();
            return true;
        }

        private IEnumerator RevealAndResolve(int index, int previous, bool second)
        {
            yield return Flip(cards[index], null, true);
            if (!second)
            {
                feedback.text = "再翻一張，找出相同的文物。";
                busy = false;
                yield break;
            }
            bool correct = Board.PairMatches;
            if (correct)
            {
                Board.ResolvePair();
                cards[previous].SetMatched(true); cards[index].SetMatched(true);
                audioPlayer?.PlayCorrect();
                progress.text = $"記憶拼圖　{Board.MatchedPairs} / 6";
                feedback.text = "記憶連結成功：" + artifacts[Board.ArtifactAt(index)].title;
                first = -1; busy = false;
                if (Board.IsComplete) controller.Succeed();
            }
            else
            {
                feedback.text = "再看清楚，0.8 秒後翻回。";
                yield return WaitForRunningSeconds(MismatchSeconds);
                yield return Flip(cards[previous], cards[index], false);
                Board.ResolvePair();
                first = -1; busy = false;
                feedback.text = "記住剛才的位置，再試一次。";
            }
        }

        private IEnumerator Flip(FinaleCardView a, FinaleCardView b, bool up)
        {
            float elapsed = 0;
            bool switched = false;
            while (elapsed < FlipSeconds)
            {
                yield return null;
                if (!isActiveAndEnabled || !controller.CanInteract) continue;
                elapsed += Time.unscaledDeltaTime;
                float fraction = Mathf.Clamp01(elapsed / FlipSeconds);
                if (!switched && fraction >= 0.5f)
                {
                    a.SetFace(up); if (b != null) b.SetFace(up);
                    switched = true;
                }
                float scale = Mathf.Max(0.035f, Mathf.Abs(1 - fraction * 2));
                a.SetFlipScale(scale); if (b != null) b.SetFlipScale(scale);
            }
            a.SetFace(up); a.SetFlipScale(1);
            if (b != null) { b.SetFace(up); b.SetFlipScale(1); }
        }

        private IEnumerator WaitForRunningSeconds(float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                yield return null;
                if (isActiveAndEnabled && controller.CanInteract) elapsed += Time.unscaledDeltaTime;
            }
        }

        public void ShowSpaceIntroduction()
        {
            if (!controller.CanInteract || IsBusy) return;
            ui.ShowIntroduction(controller.Definition.IntroductionTitle,
                controller.Definition.IntroductionBody, controller.Definition.IntroductionImage);
        }

        public void ShowKnowledge()
        {
            if (!controller.CanInteract || IsBusy) return;
            int artifactIndex = first >= 0 ? Board.ArtifactAt(first) :
                Array.FindIndex(artifacts, item => acquiredIds.Length > 0 && item.id == acquiredIds[0]);
            var artifact = artifacts[Mathf.Max(0, artifactIndex)];
            ui.ShowKnowledgeCard(artifact.title, facts.Find(artifact.id).KnowledgeBody, artifact.icon);
        }

        private void OnCompleted(LevelResult result)
        {
            StopAllCoroutines();
            busy = false;
            hintPanel.SetActive(false);
            foreach (var card in cards) card.SetFlipScale(1);
            feedback.text = result.Success ? "六組文物的記憶，都已找回。" : "時間到了，下次再找回完整的記憶。";
            RefreshInteractivity();
        }

        private void RefreshInteractivity()
        {
            if (Board == null) return;
            for (int i = 0; i < cards.Length; i++) cards[i].SetInteractable(CanFlip && !Board.IsFaceUp(i));
            bool canOpen = controller.CanInteract && !IsBusy && !awaitingHint;
            introductionButton.interactable = knowledgeButton.interactable = canOpen;
            restartButton.interactable = controller.CanInteract || IsShowingHint;
        }
    }
}
