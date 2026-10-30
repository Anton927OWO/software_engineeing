using UnityEngine;

namespace XunJiYiDa.Levels.Shared
{
    [CreateAssetMenu(menuName = "尋跡宜大/Shared/Level Definition")]
    public sealed class SharedLevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "SharedFoundation";
        [SerializeField] private string displayName = "尋跡宜大｜共用基礎";
        [SerializeField, Min(1)] private float durationSeconds = 120;
        [Tooltip("0 means unlimited mistakes.")]
        [SerializeField, Min(0)] private int mistakeLimit;
        [SerializeField] private string introductionTitle = "歡迎來到尋跡宜大";
        [SerializeField, TextArea(3, 10)] private string introductionBody = "這裡提供關卡共用的時間、暫停、介紹與知識卡介面。\n準備好後，按下開始探索。";
        [SerializeField] private Sprite introductionImage;
        [SerializeField] private bool showIntroduction = true;

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public float DurationSeconds => durationSeconds;
        public int MistakeLimit => mistakeLimit;
        public string IntroductionTitle => introductionTitle;
        public string IntroductionBody => introductionBody;
        public Sprite IntroductionImage => introductionImage;
        public bool ShowIntroduction => showIntroduction;

        public LevelSession CreateSession() => new LevelSession(levelId, durationSeconds, mistakeLimit);
    }
}
