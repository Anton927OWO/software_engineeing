using System;
using UnityEngine;
[CreateAssetMenu(menuName = "NIU/Dig/Artifact catalog")]
public sealed class DigArtifactCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public string id;
        public string displayName;
        public Sprite illustration;
        [TextArea(3, 8)] public string introduction;
        [TextArea(2, 5)] public string source;
        public bool verified;
    }
    public Entry[] entries = new Entry[0];
}
