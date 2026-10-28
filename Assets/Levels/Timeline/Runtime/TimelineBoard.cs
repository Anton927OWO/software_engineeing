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
            audioPlayer?.PlayCorrect();
            progress.text = $"已定位 {placed} / 5";
            feedback.text = "定位正確！繼續拼回宜大的百年軌跡。";
            if (placed == cards.Length) controller.Succeed();
        }

        public void Reject(TimelineCard card)
        {
            if (!CanInteract || card == null || card.Board != this || card.IsLocked || !card.IsDragging) return;
            // End the gesture before counting: OnDrop followed by OnEndDrag must count only once.
            card.BounceHome();
            controller.RecordMistake();
            feedback.text = "尚未對上這個年份，卡片已返回；再試一次！";
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
            feedback.text = "拖曳學制卡到對應年份；閱讀知識卡可獲得提示。";
        }

        private void HandleState(LevelState state)
        {
            bool running = CanInteract;
            knowledge.interactable = spaceIntroduction.interactable = running;
            if (!running) foreach (var card in cards) card.CancelDrag();
        }

        private void ShowKnowledge()
        {
            if (!CanInteract) return;
            audioPlayer?.PlayClick();
            ui.ShowKnowledgeCard("百年學制的五個節點",
                "1926｜臺北州立宜蘭農林學校，開啟農林教育。\n" +
                "1946｜臺灣省立宜蘭農業職業學校。\n" +
                "1988｜改制國立宜蘭農工專科學校。\n" +
                "1998｜升格國立宜蘭技術學院。\n" +
                "2003｜更名國立宜蘭大學。\n\n" +
                "期間另有 1967、1969 年職校改名。\n這五張卡呈現主要節點，並非全部校名。",
                cards[4].Illustration);
        }

        private void ShowIntroduction()
        {
            if (!CanInteract) return;
            audioPlayer?.PlayClick();
            var definition = controller.Definition;
            ui.ShowIntroduction(definition.IntroductionTitle, definition.IntroductionBody, definition.IntroductionImage);
        }
    }
}
