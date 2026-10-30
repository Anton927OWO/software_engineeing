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
        public string Summary => IsVerified ? summary : "史實待查證；先觀察示意圖的形狀。";
        public string VerificationLabel => IsVerified ? "已核對官方來源｜" + verifiedOn : "待查證";
        public string KnowledgeBody => IsVerified
            ? knowledge + "\n\n" + context + "\n\n" + VerificationLabel +
                "\n文物來源：" + sourceUrl + "\n文化來源：" + contextSourceUrl + "\n圖像為示意，非考古測繪。"
            : "史實待查證。\n圖像觀察：" + memoryCue + "\n圖像為示意，非考古測繪。";
    }
}
