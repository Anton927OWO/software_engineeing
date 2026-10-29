using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Timeline
{
    /// <summary>Owns this level's puzzle; time, pause and results belong to Shared.</summary>
    public sealed class TimelineBoard : MonoBehaviour
    {
        [SerializeField] private SharedLevelController controller;
        [SerializeField] private SharedLevelUI ui;
        [SerializeField] private SharedAudioPlayer audioPlayer;
        [SerializeField] private TimelineCard[] cards;
        [SerializeField] private TimelineSlot[] slots;
        [SerializeField] private TMP_Text progress;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private Button knowledge;
        [SerializeField] private Button spaceIntroduction;
        [SerializeField] private TimelineHistoryView historyView;
        private bool initialized;
        private int placed;
        private int[] previousOrder;

        public bool CanInteract => isActiveAndEnabled && controller != null && controller.CanInteract;

        private void OnEnable()
        {
            controller.Retried += ResetAttempt;
            controller.Session.StateChanged += HandleState;
            knowledge.onClick.AddListener(ShowKnowledge);
            spaceIntroduction.onClick.AddListener(ShowIntroduction);
            if (!initialized) { ResetAttempt(); initialized = true; }
            HandleState(controller.Session.State);
        }

        private void OnDisable()
        {
            controller.Retried -= ResetAttempt;
            controller.Session.StateChanged -= HandleState;
            knowledge.onClick.RemoveListener(ShowKnowledge);
            spaceIntroduction.onClick.RemoveListener(ShowIntroduction);
            foreach (var card in cards) card.CancelDrag();
        }

        public void TryPlace(TimelineCard card, TimelineSlot slot, int pointerId)
        {
            if (!CanInteract || card == null || slot == null || card.Board != this || slot.Board != this ||
                !card.IsDraggingWith(pointerId) || card.IsLocked) return;
            if (card.TargetIndex != slot.Index || slot.IsOccupied)
            {
                Reject(card);
                return;
            }
            slot.SetOccupied(true);
            card.LockAt(slot.transform.position);
            placed++;
            historyView.RememberPlacement(card.TargetIndex);
            audioPlayer?.PlayCorrect();
            progress.text = $"已定位 {placed} / 5";
            feedback.text = TimelineHistory.Stages[card.TargetIndex].Feedback;
            if (placed == cards.Length) controller.Succeed();
        }

        public void Reject(TimelineCard card)
        {
            if (!CanInteract || card == null || card.Board != this || card.IsLocked || !card.IsDragging) return;
            // End the gesture before counting: OnDrop followed by OnEndDrag must count only once.
            card.BounceHome();
            controller.RecordMistake();
            feedback.text = "線索｜" + TimelineHistory.Stages[card.TargetIndex].Clue;
        }

        private void ResetAttempt()
        {
            placed = 0;
            var order = new int[cards.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            // Rotate rejected orders. Of five distinct rotations at most two are rejected,
            // so this terminates without making another random draw.
            while (true)
            {
                bool same = previousOrder != null;
                bool chronological = true;
                for (int i = 0; i < order.Length; i++)
                {
                    same &= previousOrder != null && order[i] == previousOrder[i];
                    chronological &= order[i] == i;
                }
                if (!same && !chronological) break;
                int first = order[0];
                for (int i = 0; i < order.Length - 1; i++) order[i] = order[i + 1];
                order[order.Length - 1] = first;
            }
            previousOrder = order;
            for (int position = 0; position < order.Length; position++)
                cards[order[position]].ResetTo(new Vector2((position - 2) * 360, -340));
            foreach (var slot in slots) slot.SetOccupied(false);
            progress.text = "已定位 0 / 5";
            feedback.text = "拖曳至年份；「年代提示」可查校史與宜大記憶庫的文化連結。";
        }

        private void HandleState(LevelState state)
        {
            bool running = CanInteract;
            knowledge.interactable = running;
            spaceIntroduction.interactable = running ||
                (state == LevelState.Paused && controller.Session.PauseReasons == PauseReason.Modal);
            if (!running) foreach (var card in cards) card.CancelDrag();
        }

        private void ShowKnowledge()
        {
            if (!CanInteract) return;
            audioPlayer?.PlayClick();
            historyView.ShowKnowledge();
        }

        private void ShowIntroduction()
        {
            if (!CanInteract && !(controller.Session.State == LevelState.Paused &&
                controller.Session.PauseReasons == PauseReason.Modal)) return;
            audioPlayer?.PlayClick();
            var definition = controller.Definition;
            ui.ShowIntroduction(definition.IntroductionTitle, definition.IntroductionBody, definition.IntroductionImage);
        }
    }
}
