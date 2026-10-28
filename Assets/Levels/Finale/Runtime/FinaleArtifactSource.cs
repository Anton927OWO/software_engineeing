using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using XunJiYiDa.Levels.Integration;

namespace XunJiYiDa.Levels.Finale
{
    /// <summary>Implement on GameManager or a scene adapter to supply this run's collected artifacts.</summary>
    public interface IFinaleArtifactSource
    {
        IEnumerable<string> GetFinaleArtifactIds();
    }

    public sealed class FinaleArtifactSource : MonoBehaviour
    {
        [Tooltip("Optional GameManager/adapter implementing IFinaleArtifactSource. Empty: discover a source in loaded scenes.")]
        [SerializeField] private MonoBehaviour source;
        [Tooltip("Standalone test IDs. Valid IDs are artifact-01 through artifact-06; three distinct IDs are used.")]
        [SerializeField] private string[] testArtifactIds = { "artifact-01", "artifact-03", "artifact-06" };

        public string[] Capture()
        {
            if (TrackingArtifactMap.HasFinaleContext && TrackingArtifactMap.IsOfficialRun)
                return ResolveIds(TrackingArtifactMap.CurrentFinaleArtifacts(), testArtifactIds);
            MonoBehaviour candidate = source;
            if (candidate == null)
            {
                var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var behaviour in behaviours)
                    if (behaviour is IFinaleArtifactSource) { candidate = behaviour; break; }
                if (candidate == null)
                    foreach (var behaviour in behaviours)
                        if (behaviour.GetType().Name == "GameManager") { candidate = behaviour; break; }
            }
            IEnumerable<string> ids = null;
            if (candidate != null)
            {
                try
                {
                    if (candidate is IFinaleArtifactSource provider) ids = provider.GetFinaleArtifactIds();
                    else ids = ReadGameManager(candidate);
                    // Enumerate inside the try: a lazy provider can also throw.
                    return ResolveIds(ids, testArtifactIds);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Finale could not read artifact IDs; using configured test IDs. " + exception.Message, this);
                }
            }
            return ResolveIds(TrackingArtifactMap.HasFinaleContext ? TrackingArtifactMap.CurrentFinaleArtifacts() : null, testArtifactIds);
        }

        private static IEnumerable<string> ReadGameManager(MonoBehaviour manager)
        {
            var type = manager.GetType();
            foreach (string name in new[] { "CollectedArtifactIds", "ArtifactIds", "collectedArtifactIds" })
            {
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
                    if (property.GetValue(manager) is IEnumerable<string> values) return values;
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
                if (field?.GetValue(manager) is IEnumerable<string> fieldValues) return fieldValues;
            }
            var method = type.GetMethod("GetCollectedArtifactIds", Type.EmptyTypes);
            if (method?.Invoke(manager, null) is IEnumerable<string> result) return result;
            Debug.LogWarning("Finale: GameManager has no supported artifact source. Implement IFinaleArtifactSource or assign an adapter.", manager);
            return null;
        }

        public static string[] ResolveIds(IEnumerable<string> collected, IEnumerable<string> fallback)
        {
            var result = new List<string>(3);
            Append(collected, result);
            Append(fallback, result);
            // Bad or partial Inspector data must still produce a playable three-item hint.
            for (int i = 1; result.Count < 3; i++) Append(new[] { $"artifact-{i:00}" }, result);
            return result.ToArray();
        }

        private static void Append(IEnumerable<string> input, List<string> output)
        {
            if (input == null || output.Count == 3) return;
            foreach (var value in input)
            {
                string id = value?.Trim();
                if (id == null || id.Length != 11 || !id.StartsWith("artifact-0", StringComparison.Ordinal) ||
                    id[10] < '1' || id[10] > '6' || output.Contains(id)) continue;
                output.Add(id);
                if (output.Count == 3) break;
            }
        }
    }
}
