using System.Collections.Generic;
using TrackingNIU;

namespace XunJiYiDa.Levels.Integration
{
    public static class TrackingArtifactMap
    {
        public static string ToLocalId(ArtifactId artifact)
        {
            switch (artifact)
            {
                case ArtifactId.GoldenCarp: return "artifact-01";
                case ArtifactId.GoldFoil: return "artifact-02";
                case ArtifactId.PotteryJar: return "artifact-03";
                case ArtifactId.WoodenSpade: return "artifact-04";
                case ArtifactId.WoodenDisc: return "artifact-05";
                case ArtifactId.AgateBead: return "artifact-06";
                default: return "";
            }
        }

        public static IEnumerable<string> CurrentFinaleArtifacts()
        {
            var context = LevelSession.Current;
            if (context == null || !context.IsFinale) yield break;
            foreach (var artifact in context.CollectedArtifacts) yield return ToLocalId(artifact);
        }

        public static bool HasFinaleContext => LevelSession.Current != null && LevelSession.Current.IsFinale;
        public static bool IsOfficialRun => LevelSession.Current != null && !LevelSession.Current.IsTestRun;
    }
}
