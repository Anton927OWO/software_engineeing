using System;

namespace XunJiYiDa.Levels.Finale
{
    [Serializable]
    public sealed class FinaleArtifactCatalog
    {
        public FinaleArtifactFact[] items;
        public FinaleArtifactFact Find(string id) => Array.Find(items, item => item.id == id);
    }

    [Serializable]
    public sealed class FinaleArtifactFact
    {
        public string id;
        public string summary;
        public string knowledge;
        public string memoryCue;
        public string sourceUrl;
        public string verificationStatus;
        public string verifiedOn;
        public string context;
        public string contextSourceUrl;

        public bool IsVerified => verificationStatus == "VerifiedOfficialSource";
        public string Summary => IsVerified ? summary : "觀察輪廓與紋樣，記住這件文物的特色。";
        public string GalleryBody => IsVerified
            ? knowledge + "\n\n" + context
            : "仔細看看圖卡。\n辨識線索：" + memoryCue;
        public string KnowledgeBody => GalleryBody + (IsVerified
            ? "\n\n文物來源：" + sourceUrl + "\n文化來源：" + contextSourceUrl
            : "");
    }
}
