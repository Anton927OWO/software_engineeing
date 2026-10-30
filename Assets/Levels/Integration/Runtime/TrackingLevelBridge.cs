using System;
using System.Linq;
using UnityEngine;
using XunJiYiDa.Levels.Shared;
using TeamSession = TrackingNIU.LevelSession;

namespace XunJiYiDa.Levels.Integration
{
    /// <summary>Adapts the existing level UI and mechanics to the team's unchanged flow API.</summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SharedLevelController))]
    public sealed class TrackingLevelBridge : MonoBehaviour
    {
        private SharedLevelController controller;
        private SharedLevelUI ui;
        private TrackingNIU.LevelContext context;
        private Action retry;

        private bool OwnsContext => context != null && ReferenceEquals(TeamSession.Current, context);

        private void Awake()
        {
            controller = GetComponent<SharedLevelController>();
            string id = controller.Definition != null ? controller.Definition.LevelId : "";
            if (id != "Train" && id != "Timeline" && id != "Finale") { enabled = false; return; }
            if (!Enum.TryParse(id, out TrackingNIU.LevelId levelId)) { enabled = false; return; }
            context = TeamSession.Current;
            if (context == null || context.LevelId != levelId)
            {
                Debug.LogError("Level context does not match " + id + ". Add and configure LevelTestLauncher in this scene.", this);
                controller.enabled = false;
                enabled = false;
                return;
            }
            ui = GetComponentInChildren<SharedLevelUI>(true);
            controller.InitializeSession(new Shared.LevelSession(id, context.TimeLimit, controller.Definition.MistakeLimit));
        }

        private void OnEnable()
        {
            controller.Completed += Report;
            retry = Retry;
            controller.RetryOverride = retry;
        }

        private void OnDisable()
        {
            if (controller == null) return;
            controller.Completed -= Report;
            if (controller.RetryOverride == retry) controller.RetryOverride = null;
        }

        private void Report(Shared.LevelResult local)
        {
            if (!OwnsContext || TeamSession.HasReported) return;
            var pairs = controller.GetComponentsInChildren<MonoBehaviour>(true).OfType<IFinalePairProgress>().FirstOrDefault();
            var score = TeamSession.Report(new TrackingNIU.LevelResult
            {
                Cleared = local.Success,
                RemainingTime = local.RemainingTime,
                Mistakes = local.MistakeCount,
                MatchedPairs = context.IsFinale && pairs != null ? pairs.MatchedPairs : 0
            });
            string knowledge = context.IsFinale
                ? "探索中收集的文物，連結了宜大的文化記憶。"
                : "本關獲得文物：" + TrackingNIU.LevelCatalog.GetArtifactName(context.Artifact);
            ui.ShowResultDetails(knowledge + $"\n基礎分　{score.ClearBonus + score.MatchBonus}　時間獎勵　{score.TimeBonus}" +
                $"\n失誤扣分　−{score.MistakePenalty}\n總分　{score.Total}", Continue);
        }

        private void Continue()
        {
            if (OwnsContext && TeamSession.HasReported) TeamSession.Exit();
        }

        private void Retry()
        {
            if (!OwnsContext)
            {
                // Standalone Continue only logs and leaves this scene open. Permit replay,
                // but never replace a different active level that started in the meantime.
                if (context == null || !context.IsTestRun || TeamSession.IsActive) return;
                TeamSession.Begin(context, LogStandaloneExit);
                controller.RetryLocally();
                return;
            }
            if (!context.IsTestRun)
            {
                TeamSession.Retry();
                return;
            }
            // A standalone attempt has no GameManager callback to preserve. Keep local
            // replay instantaneous while clearing the team's one-report-per-attempt state.
            if (!TeamSession.HasReported) TeamSession.Report(new TrackingNIU.LevelResult());
            TeamSession.Exit();
            TeamSession.Begin(context, LogStandaloneExit);
            controller.RetryLocally();
        }

        private static void LogStandaloneExit(TrackingNIU.LevelResult result, TrackingNIU.ScoreBreakdown score)
            => Debug.Log($"[測試] 關卡結束：{result}\n[測試] 得分：{score}");

        private void OnDestroy()
        {
            if (!OwnsContext || !context.IsTestRun) return;
            if (!TeamSession.HasReported) TeamSession.Report(new TrackingNIU.LevelResult());
            TeamSession.Exit();
        }
    }
}
