using System;
[Serializable] public sealed class DigArtifactResult
{
    public string sceneObject;
    public bool firstSeen;
    public bool found;
}
[Serializable] public sealed class DigLevelResult
{
    public bool success;
    public float remainingSeconds;
    public int damageCount;
    public int foundCount;
    public string failureReason;
    public DigArtifactResult[] artifacts;
}
