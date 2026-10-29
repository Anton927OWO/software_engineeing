using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Timeline
{
    /// <summary>Browses sourced milestones inside the existing knowledge and result panels.</summary>
    public sealed class TimelineHistoryView : MonoBehaviour
    {
        [SerializeField] private SharedLevelController controller;
        [SerializeField] private SharedLevelUI ui;
        [SerializeField] private SharedContentPanel knowledgePanel;
        [SerializeField] private TimelineCard[] cards;
        [SerializeField] private GameObject resultBrowser;
        [SerializeField] private TMP_Text resultHeading;
        [SerializeField] private TMP_Text resultSummary;
        [SerializeField] private Button[] previous;
        [SerializeField] private Button[] next;
        [SerializeField] private Button[] historySources;
        [SerializeField] private Button[] cultureSources;
        private int page;
        private int lastPlaced;

        private TimelineEra Current => TimelineHistory.Stages[page];
        private bool CanBrowse => isActiveAndEnabled && (controller.Session.Result != null ||
            (knowledgePanel.gameObject.activeInHierarchy && controller.Session.State == LevelState.Paused &&
             controller.Session.PauseReasons == PauseReason.Modal));

        private void OnEnable()
        {
            controller.Session.StateChanged += HandleState;
            controller.Retried += ResetAttempt;
            foreach (var button in previous) button.onClick.AddListener(Previous);
            foreach (var button in next) button.onClick.AddListener(Next);
            foreach (var button in historySources) button.onClick.AddListener(OpenHistorySource);
            foreach (var button in cultureSources) button.onClick.AddListener(OpenCultureSource);
            HandleState(controller.Session.State);
        }

        private void OnDisable()
        {
            controller.Session.StateChanged -= HandleState;
            controller.Retried -= ResetAttempt;
            foreach (var button in previous) button.onClick.RemoveListener(Previous);
            foreach (var button in next) button.onClick.RemoveListener(Next);
            foreach (var button in historySources) button.onClick.RemoveListener(OpenHistorySource);
            foreach (var button in cultureSources) button.onClick.RemoveListener(OpenCultureSource);
        }

        public void RememberPlacement(int index) => lastPlaced = Mathf.Clamp(index, 0, TimelineHistory.Stages.Count - 1);

        public void ShowKnowledge()
        {
            if (!controller.CanInteract) return;
            page = lastPlaced;
            ui.ShowKnowledgeCard(Heading, Current.Summary, cards[page].Illustration);
            Refresh();
        }

        private string Heading => $"{page + 1} / 5｜{Current.Year} {Current.ShortName}";
        private void Previous() { if (CanBrowse && page > 0) { page--; Refresh(); } }
        private void Next() { if (CanBrowse && page < TimelineHistory.Stages.Count - 1) { page++; Refresh(); } }
        private void OpenHistorySource() { if (CanBrowse) Application.OpenURL(Current.SourceUrl); }
        private void OpenCultureSource() { if (CanBrowse) Application.OpenURL(Current.ConnectionSourceUrl); }

        private void HandleState(LevelState state)
        {
            bool finished = state == LevelState.Succeeded || state == LevelState.Failed;
            if (finished && !resultBrowser.activeSelf) page = lastPlaced;
            resultBrowser.SetActive(finished);
            Refresh();
        }

        private void ResetAttempt()
        {
            page = lastPlaced = 0;
            resultBrowser.SetActive(false);
            Refresh();
        }

        private void Refresh()
        {
            if (knowledgePanel.gameObject.activeInHierarchy)
                knowledgePanel.Present(Heading, Current.Summary, cards[page].Illustration, "繼續探索");
            resultHeading.text = Heading;
            resultSummary.text = Current.Summary;
            foreach (var button in previous) button.interactable = CanBrowse && page > 0;
            foreach (var button in next) button.interactable = CanBrowse && page < TimelineHistory.Stages.Count - 1;
            foreach (var button in historySources) button.interactable = CanBrowse;
            foreach (var button in cultureSources) button.interactable = CanBrowse;
        }
    }
}
