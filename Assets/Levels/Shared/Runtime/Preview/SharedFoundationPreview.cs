using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XunJiYiDa.Levels.Shared
{
    /// <summary>Only used by SharedFoundation.unity to exercise infrastructure without level mechanics.</summary>
    public sealed class SharedFoundationPreview : MonoBehaviour
    {
        [SerializeField] private SharedLevelController controller;
        [SerializeField] private SharedLevelUI ui;
        [SerializeField] private Button introduction;
        [SerializeField] private Button minigame;
        [SerializeField] private Button knowledge;
        [SerializeField] private Button mistake;
        [SerializeField] private Button success;
        [SerializeField] private Button failure;

        private void OnEnable()
        {
            introduction.onClick.AddListener(ShowIntroduction);
            minigame.onClick.AddListener(ShowMinigame);
            knowledge.onClick.AddListener(ShowKnowledge);
            mistake.onClick.AddListener(controller.RecordMistake);
            success.onClick.AddListener(controller.Succeed);
            failure.onClick.AddListener(controller.Fail);
        }
        private void OnDisable()
        {
            introduction.onClick.RemoveListener(ShowIntroduction);
            minigame.onClick.RemoveListener(ShowMinigame);
            knowledge.onClick.RemoveListener(ShowKnowledge);
            mistake.onClick.RemoveListener(controller.RecordMistake);
            success.onClick.RemoveListener(controller.Succeed);
            failure.onClick.RemoveListener(controller.Fail);
        }
        private void Update()
        {
            bool enabled = controller.CanInteract;
            introduction.interactable = minigame.interactable = knowledge.interactable = enabled;
            mistake.interactable = success.interactable = failure.interactable = enabled;
        }
        private void ShowIntroduction() => ui.ShowIntroduction("空間介紹", "這個面板可顯示空間的文字介紹與圖片。\n此處由各關卡替換內容。");
        private void ShowMinigame() => ui.ShowMinigame("小遊戲", "這是共用的小遊戲容器。\n此場景不包含關卡玩法，倒數仍會繼續。");
        private void ShowKnowledge() => ui.ShowKnowledgeCard("知識卡", "知識卡可呈現文字與圖片。\n閱讀期間暫停倒數，關閉後恢復探索。");
    }
}
