using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XunJiYiDa.Levels.Shared;

namespace XunJiYiDa.Levels.Train
{
    [DisallowMultipleComponent]
    public sealed class TrainLevelController : MonoBehaviour
    {
        [SerializeField] private SharedLevelController shared;
        [SerializeField] private SharedLevelUI sharedUI;
        [SerializeField] private SharedAudioPlayer audioPlayer;
        [SerializeField] private TrainQuestionBank questionBank;
        [SerializeField] private TMP_Text questionNumber;
        [SerializeField] private TMP_Text prompt;
        [SerializeField] private TMP_Text source;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private TMP_Text progress;
        [SerializeField] private Button[] choices;
        [SerializeField] private Button introductionButton;
        [SerializeField] private Button knowledgeButton;
        [SerializeField] private RectTransform trainRoot;
        [SerializeField] private RectTransform[] carriageViews;
        private UnityAction[] answerActions;
        private TrainQuestion lastAnswered;
        private int attempt;
        private string renderedQuestion;
        public TrainRound Round { get; private set; }
        public SharedLevelController Shared => shared;
        public TrainQuestionBank QuestionBank => questionBank;

        private void OnEnable()
        {
            shared.Retried += ResetAttempt;
            answerActions = new UnityAction[choices.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                answerActions[i] = () => SubmitAnswer(index);
                choices[i].onClick.AddListener(answerActions[i]);
            }
            introductionButton.onClick.AddListener(ShowIntroduction);
            knowledgeButton.onClick.AddListener(ShowKnowledge);
            if (Round == null) ResetAttempt();
        }

        private void OnDisable()
        {
            shared.Retried -= ResetAttempt;
            for (int i = 0; i < choices.Length; i++) choices[i].onClick.RemoveListener(answerActions[i]);
            introductionButton.onClick.RemoveListener(ShowIntroduction);
            knowledgeButton.onClick.RemoveListener(ShowKnowledge);
        }

        private void ResetAttempt()
        {
            Round = new TrainRound(shared.Session, questionBank != null ? questionBank.questions : null,
                unchecked(Environment.TickCount + ++attempt * 7919));
            lastAnswered = null;
            renderedQuestion = null;
            trainRoot.anchoredPosition = new Vector2(0, -330);
            Render();
        }

        private void Update()
        {
            Round.Advance(Time.unscaledDeltaTime);
            Render();
        }

        public void SubmitAnswer(int index)
        {
            if (!shared.CanInteract || !Round.Submit(index)) return;
            lastAnswered = Round.Current;
            if (Round.LastWasCorrect) audioPlayer.PlayCorrect();
            // Shared.RecordMistake's sound is not used by the pure round, so play once here.
            else audioPlayer.PlayWrong();
            Render();
        }

        public void ShowIntroduction()
        {
            if (!shared.CanInteract) return;
            sharedUI.ShowIntroduction(shared.Definition.IntroductionTitle, shared.Definition.IntroductionBody,
                shared.Definition.IntroductionImage);
        }

        public void ShowKnowledge()
        {
            if (!shared.CanInteract) return;
            if (lastAnswered != null)
                sharedUI.ShowKnowledgeCard("知識車票｜" + lastAnswered.id, lastAnswered.KnowledgeText);
            else if (Round.Current != null)
                sharedUI.ShowKnowledgeCard("知識車票｜資料來源", "每答完一題，可在此閱讀正解與解說。\n\n本題來源：" +
                    Round.Current.sourceTitle + "\n" + Round.Current.sourceUrl + "\n" + Round.Current.VerificationLabel);
        }

        private void Render()
        {
            var q = Round.Current;
            if (q != null && renderedQuestion != q.id)
            {
                renderedQuestion = q.id;
                prompt.text = q.prompt;
                source.text = "資料來源：" + q.sourceTitle + "　｜　" + q.VerificationLabel;
                for (int i = 0; i < choices.Length; i++)
                    choices[i].GetComponentInChildren<TMP_Text>().text = $"{(char)('A' + i)}　{q.options[i]}";
            }
            if (q == null) { prompt.text = "題庫沒有可用題目，請編輯 TrainQuestions。"; source.text = ""; }
            questionNumber.text = $"知識車站　{Mathf.Min(Round.AnsweredCount + (Round.Phase == TrainPhase.Asking ? 1 : 0), Round.QuestionCount):00} / {Round.QuestionCount:00}";
            progress.text = $"已連接　{Round.Carriages} / {TrainRound.TargetCarriages} 節車廂";
            bool answering = shared.CanInteract && Round.Phase == TrainPhase.Asking && q != null;
            introductionButton.interactable = knowledgeButton.interactable = shared.CanInteract;
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i].interactable = answering;
                var image = choices[i].GetComponent<Image>();
                image.color = Round.Phase == TrainPhase.Revealing && q != null ?
                    i == q.correctIndex ? new Color32(40, 135, 84, 255) :
                    i == Round.SelectedIndex ? new Color32(157, 53, 43, 255) : new Color32(91, 108, 99, 255)
                    : new Color32(40, 91, 70, 255);
            }
            feedback.color = Round.Phase == TrainPhase.Revealing ? new Color32(157, 53, 43, 255) : new Color32(31, 58, 46, 255);
            feedback.text = Round.Phase == TrainPhase.Revealing && q != null ? "答錯了！正確答案：" + q.options[q.correctIndex] :
                Round.Phase == TrainPhase.Coupling ? "答對了！車廂連接中……" :
                Round.Phase == TrainPhase.Departing ? "五節集滿！知識列車出發……" :
                shared.Session.Result != null ? shared.Session.Result.Success ? "知識列車已出站！" :
                    shared.Session.RemainingTime <= 0 ? "時間已到，下次再挑戰！" : "題目已用完，下次再挑戰！" :
                "選出正確答案，讓知識成為前進的動力。";

            float departure = Round.Phase == TrainPhase.Departing ? Round.PhaseProgress :
                shared.Session.Result != null && shared.Session.Result.Success ? 1 : 0;
            trainRoot.anchoredPosition = new Vector2(-2400 * departure * departure, -330);
            for (int i = 0; i < carriageViews.Length; i++)
            {
                carriageViews[i].gameObject.SetActive(i < Round.Carriages);
                float t = i == Round.Carriages - 1 && Round.Phase == TrainPhase.Coupling ? Round.PhaseProgress : 1;
                float ease = 1 - Mathf.Pow(1 - t, 3);
                carriageViews[i].anchoredPosition = new Vector2(-380 + i * 235 + (1 - ease) * 650,
                    -10 + Mathf.Sin(t * Mathf.PI * 4) * (1 - t) * 10);
            }
        }
    }
}
