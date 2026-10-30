using UnityEngine;

namespace XunJiYiDa.Levels.Train
{
    [CreateAssetMenu(menuName = "尋跡宜大/Train/Question Bank")]
    public sealed class TrainQuestionBank : ScriptableObject
    {
        [Tooltip("每題必須有唯一 ID、三個不同選項與 0～2 的正解索引。新題請先保持 verified 關閉。")]
        public TrainQuestion[] questions = System.Array.Empty<TrainQuestion>();
    }
}
