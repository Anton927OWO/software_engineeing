using System;
using UnityEngine;

[CreateAssetMenu(menuName = "NIU/Strata/Knowledge catalog")]
public sealed class StrataKnowledgeCatalog : ScriptableObject
{
    public const string ResourcePath = "StrataKnowledgeCatalog";
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string title;
        public Sprite image;
        [TextArea(3, 8)] public string body;
        [TextArea(2, 5)] public string source;
    }
    public Entry[] entries = new Entry[0];
    public bool HasCompletePages
    {
        get
        {
            if (entries == null || entries.Length != 3) return false;
            for (int i = 0; i < entries.Length; i++)
            {
                var page = entries[i];
                if (page == null || string.IsNullOrWhiteSpace(page.id) || string.IsNullOrWhiteSpace(page.title) ||
                    page.image == null || string.IsNullOrWhiteSpace(page.body) || string.IsNullOrWhiteSpace(page.source)) return false;
                for (int j = 0; j < i; j++) if (entries[j].id == page.id) return false;
            }
            return true;
        }
    }
}
