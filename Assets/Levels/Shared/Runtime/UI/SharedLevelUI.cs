using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Shared
{
    public sealed class SharedLevelUI : MonoBehaviour
    {
        [SerializeField] private SharedLevelController controller;
        [SerializeField] private SharedAudioPlayer audioPlayer;
        [SerializeField] private TMP_Text levelName;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text mistakes;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRetryButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultBody;
        [SerializeField] private Button resultRetryButton;
        [SerializeField] private Button resultContinueButton;
        private string resultDetails;
        private Action resultContinueAction;
        [SerializeField] private SharedContentPanel introduction;
        [SerializeField] private SharedContentPanel minigame;
        [SerializeField] private SharedContentPanel knowledgeCard;
        private SharedContentPanel activeContent;
        private Action onContentConfirmed;
        private int displayedSecond = -1;
        private int displayedMistakes = -1;
        private readonly Dictionary<Selectable, Navigation> backgroundNavigation = new Dictionary<Selectable, Navigation>();
        public RectTransform MinigameContentRoot => minigame.ContentRoot;

        private void OnEnable()
        {
            pauseButton.onClick.AddListener(Pause);
            resumeButton.onClick.AddListener(Resume);
            pauseRetryButton.onClick.AddListener(Retry);
            resultRetryButton.onClick.AddListener(Retry);
            if (resultContinueButton != null) resultContinueButton.onClick.AddListener(ContinueResult);
            introduction.PrimaryButton.onClick.AddListener(ConfirmContent);
            minigame.PrimaryButton.onClick.AddListener(ConfirmContent);
            knowledgeCard.PrimaryButton.onClick.AddListener(ConfirmContent);
            controller.Session.StateChanged += RefreshState;
            levelName.text = controller.Definition != null ? controller.Definition.DisplayName : "尋跡宜大";
            RefreshState(controller.Session.State);
            RefreshCounters();
            if (pausePanel.activeSelf) RestrictNavigation(pausePanel.transform);
            else if (activeContent != null) RestrictNavigation(activeContent.transform);
        }

        private void OnDisable()
        {
            pauseButton.onClick.RemoveListener(Pause);
            resumeButton.onClick.RemoveListener(Resume);
            pauseRetryButton.onClick.RemoveListener(Retry);
            resultRetryButton.onClick.RemoveListener(Retry);
            if (resultContinueButton != null) resultContinueButton.onClick.RemoveListener(ContinueResult);
            introduction.PrimaryButton.onClick.RemoveListener(ConfirmContent);
            minigame.PrimaryButton.onClick.RemoveListener(ConfirmContent);
            knowledgeCard.PrimaryButton.onClick.RemoveListener(ConfirmContent);
            if (controller != null) controller.Session.StateChanged -= RefreshState;
            // Preserve the current content and its callback if the root is disabled and re-enabled.
            RestoreNavigation();
        }

        private void Update() => RefreshCounters();
        private void Pause() { audioPlayer?.PlayClick(); controller.Pause(); }
        private void Resume() { audioPlayer?.PlayClick(); controller.Resume(); }
        private void Retry() { audioPlayer?.PlayClick(); controller.Retry(); }

        public void ShowResultDetails(string details, Action onContinue)
        {
            resultDetails = details;
            resultContinueAction = onContinue;
            RefreshState(controller.Session.State);
        }

        private void ContinueResult()
        {
            audioPlayer?.PlayClick();
            var callback = resultContinueAction;
            resultContinueAction = null;
            if (resultContinueButton != null) resultContinueButton.interactable = false;
            callback?.Invoke();
        }

        public void ShowIntroduction(string title, string body, Sprite image = null, Action onContinue = null)
            => ShowContent(introduction, title, body, image, "開始探索", true, onContinue);

        public void ShowMinigame(string title, string instructions, bool pauseCountdown = false, Action onClose = null)
        {
            if (controller.Session.Result == null)
                ShowContent(minigame, title, instructions, null, "關閉", pauseCountdown, onClose);
        }

        public void ShowKnowledgeCard(string title, string body, Sprite image = null, Action onClose = null)
        {
            if (controller.Session.Result == null)
                ShowContent(knowledgeCard, title, body, image, "繼續探索", true, onClose);
        }

        private void ShowContent(SharedContentPanel panel, string title, string body, Sprite image,
            string actionLabel, bool pauses, Action confirmed)
        {
            CloseContent();
            activeContent = panel;
            onContentConfirmed = confirmed;
            controller.Session.SetPaused(PauseReason.Modal, pauses);
            panel.Present(title, body, image, actionLabel);
            RestrictNavigation(panel.transform);
            Select(panel.PrimaryButton);
        }

        /// <summary>Hide without accepting the panel or invoking its completion callback.</summary>
        public void CloseContent()
        {
            RestoreNavigation();
            if (activeContent != null) activeContent.Hide();
            activeContent = null;
            onContentConfirmed = null;
            if (controller != null) controller.Session.SetPaused(PauseReason.Modal, false);
            if (pausePanel.activeInHierarchy) RestrictNavigation(pausePanel.transform);
        }

        private void ConfirmContent()
        {
            audioPlayer?.PlayClick();
            var callback = onContentConfirmed;
            CloseContent();
            callback?.Invoke();
        }

        private void RefreshState(LevelState state)
        {
            var session = controller.Session;
            bool showPause = state == LevelState.Paused &&
                (session.PauseReasons & (PauseReason.Manual | PauseReason.Focus)) != 0;
            bool pauseWasVisible = pausePanel.activeSelf;
            pausePanel.SetActive(showPause);
            pauseButton.interactable = state == LevelState.Running || state == LevelState.Paused;
            bool finished = state == LevelState.Succeeded || state == LevelState.Failed;
            resultPanel.SetActive(finished);
            if (!finished) { resultDetails = null; resultContinueAction = null; }
            if (resultContinueButton != null)
            {
                resultContinueButton.gameObject.SetActive(finished && resultContinueAction != null);
                resultContinueButton.interactable = resultContinueAction != null;
            }
            if (finished)
            {
                CloseContent();
                RestrictNavigation(resultPanel.transform);
                resultTitle.text = state == LevelState.Succeeded ? "探索成功" : "探索未完成";
                resultBody.text = $"剩餘時間　{FormatTime(session.RemainingTime)}\n失誤次數　{session.MistakeCount}\n收集文物　{session.ArtifactIds.Count}";
                if (!string.IsNullOrEmpty(resultDetails)) resultBody.text = resultDetails;
                Select(resultContinueAction != null && resultContinueButton != null ? resultContinueButton : resultRetryButton);
            }
            else if (showPause && !pauseWasVisible)
            {
                RestoreNavigation();
                RestrictNavigation(pausePanel.transform);
                Select(resumeButton);
            }
            else if (!showPause && pauseWasVisible)
            {
                RestoreNavigation();
                if (activeContent != null)
                {
                    RestrictNavigation(activeContent.transform);
                    Select(activeContent.PrimaryButton);
                }
            }
            status.text = state == LevelState.Ready ? "準備探索" :
                state == LevelState.Paused ? "探索暫停中" :
                state == LevelState.Running ? "探索進行中" : "本次探索已結束";
            RefreshCounters();
        }

        private void RefreshCounters()
        {
            var session = controller.Session;
            int seconds = Mathf.CeilToInt(session.RemainingTime);
            if (seconds != displayedSecond)
            {
                displayedSecond = seconds;
                timer.text = FormatTime(session.RemainingTime);
                timer.color = seconds <= 10 ? new Color32(179, 50, 42, 255) : new Color32(31, 58, 46, 255);
            }
            if (session.MistakeCount != displayedMistakes)
            {
                displayedMistakes = session.MistakeCount;
                mistakes.text = $"失誤　{session.MistakeCount}";
            }
        }

        public static string FormatTime(float seconds)
        {
            int value = Mathf.CeilToInt(Mathf.Max(0, seconds));
            return $"{value / 60:00}:{value % 60:00}";
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current != null && selectable != null && selectable.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        private void RestrictNavigation(Transform scope)
        {
            // Selectable's automatic search excludes Navigation.None candidates. Raycasts are
            // already blocked by the overlay; this also blocks keyboard/gamepad escape behind it.
            foreach (var selectable in UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                if (selectable.transform.IsChildOf(scope) || backgroundNavigation.ContainsKey(selectable)) continue;
                backgroundNavigation.Add(selectable, selectable.navigation);
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.None;
                selectable.navigation = navigation;
            }
        }

        private void RestoreNavigation()
        {
            foreach (var entry in backgroundNavigation)
                if (entry.Key != null) entry.Key.navigation = entry.Value;
            backgroundNavigation.Clear();
        }
    }
}
