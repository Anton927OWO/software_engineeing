using System;
using UnityEngine;
using UnityEngine.Events;

namespace XunJiYiDa.Levels.Shared
{
    [Serializable] public sealed class LevelResultEvent : UnityEvent<LevelResult> { }

    /// <summary>Scene-local integration point; no dependency on another team's Common or save format.</summary>
    public sealed class LevelResultRelay : MonoBehaviour
    {
        [SerializeField] private SharedLevelController controller;
        [Tooltip("Optional components implementing ILevelResultReceiver.")]
        [SerializeField] private MonoBehaviour[] receivers = Array.Empty<MonoBehaviour>();
        [SerializeField] private LevelResultEvent onCompleted = new LevelResultEvent();
        public LevelResult LastResult { get; private set; }
        public string LastResultJson { get; private set; } = "";

        private void OnEnable()
        {
            if (controller == null) return;
            controller.Completed += Publish;
            controller.Retried += Clear;
        }
        private void OnDisable()
        {
            if (controller == null) return;
            controller.Completed -= Publish;
            controller.Retried -= Clear;
        }
        private void Clear() { LastResult = null; LastResultJson = ""; }

        private void Publish(LevelResult result)
        {
            LastResult = result;
            LastResultJson = JsonUtility.ToJson(new ResultData(result));
            foreach (var receiver in receivers)
            {
                if (receiver == null) continue;
                if (!(receiver is ILevelResultReceiver target))
                {
                    Debug.LogWarning($"{receiver.name} does not implement ILevelResultReceiver.", receiver);
                    continue;
                }
                try { target.ReceiveLevelResult(result); }
                catch (Exception exception) { Debug.LogException(exception, receiver); }
            }
            onCompleted.Invoke(result);
        }

        [Serializable]
        private sealed class ResultData
        {
            public string levelId;
            public bool success;
            public float remainingTime;
            public int mistakeCount;
            public string[] artifactIds;
            public ResultData(LevelResult result)
            {
                levelId = result.LevelId;
                success = result.Success;
                remainingTime = result.RemainingTime;
                mistakeCount = result.MistakeCount;
                artifactIds = new string[result.ArtifactIds.Count];
                for (int i = 0; i < artifactIds.Length; i++) artifactIds[i] = result.ArtifactIds[i];
            }
        }
    }
}
