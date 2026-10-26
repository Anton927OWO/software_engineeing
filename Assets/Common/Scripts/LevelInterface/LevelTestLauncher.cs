using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackingNIU
{
    /// <summary>
    /// 測試用啟動腳本。放在每個關卡 Scene 的任一物件上，並選好 Level Id。
    /// 直接按 Play 時（沒有 GameManager）用預設設定開始，結束時在 Console 印出結果；
    /// 從 GameManager 進入時什麼都不做，所以整合後不用拿掉。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LevelTestLauncher : MonoBehaviour
    {
        [SerializeField, Tooltip("這個 Scene 是哪一關")]
        LevelId levelId;

        [SerializeField, Min(0f), Tooltip("測試時改用這個限時（秒），0 代表用關卡預設值")]
        float timeLimitOverride;

        [SerializeField, Tooltip("只有終章使用：假設本局已取得的文物")]
        ArtifactId[] collectedArtifacts = { ArtifactId.GoldFoil, ArtifactId.PotteryJar, ArtifactId.GoldenCarp };

        void Awake()
        {
            if (LevelSession.IsActive)
                return;

            var definition = LevelCatalog.Get(levelId);
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != definition.SceneName)
                Debug.LogWarning($"[測試] Scene 名稱是 {sceneName}，但 Level Id 對應的是 {definition.SceneName}，請確認 Level Id 是否選對。");

            var context = new LevelContext(definition, collectedArtifacts, isTestRun: true, timeLimitOverride);
            LevelSession.Begin(context, OnExit);
            Debug.Log($"[測試] 以預設設定開始：{context}");
        }

        static void OnExit(LevelResult result, ScoreBreakdown score)
        {
            Debug.Log($"[測試] 關卡結束：{result}\n[測試] 得分：{score}");
        }
    }
}
