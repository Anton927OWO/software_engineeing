using System;
using UnityEngine;

[CreateAssetMenu(menuName = "NIU/Strata/Pattern questions")]
public sealed class StrataPatternQuestionSet : ScriptableObject
{
    [Serializable] public sealed class Option
    {
        public string id;
        public Sprite sprite;
        [Tooltip("相對 Sprite 的0～1範圍，原點在左下；完整圖片用(0,0,1,1)。")]
        public Rect region = new Rect(0, 0, 1, 1);
    }
    [Serializable] public sealed class Question
    {
        public string id;
        [TextArea(1, 3)] public string prompt;
        public Sprite artifactImage;
        public Rect focusRegion = new Rect(0, 0, 1, 1);
        public Option[] options = new Option[3];
        public string correctOptionId;
    }
    public bool previewOnly = true;
    [TextArea(2, 4)] public string contentNotice;
    [TextArea(2, 5)] public string source;
    public Question[] questions = new Question[3];
    public int Count => questions != null ? questions.Length : 0;

    public bool IsQuestionReady(int index, out string reason)
    {
        reason = "";
        if (Count != 3 || index < 0 || index >= Count || questions[index] == null)
        { reason = "請設定三題題目資料。"; return false; }
        Question q = questions[index];
        Texture texture; Rect uv; float aspect;
        if (!TryGetVisual(q.artifactImage, q.focusRegion, out texture, out uv, out aspect))
        { reason = "題目陶片圖片或觀察範圍尚未設定。"; return false; }
        if (q.options == null || q.options.Length != 3 || string.IsNullOrEmpty(q.correctOptionId))
        { reason = "請設定三個圖片選項及正確答案ID。"; return false; }
        var ids = new System.Collections.Generic.HashSet<string>();
        bool hasCorrect = false;
        foreach (Option option in q.options)
        {
            if (option == null || string.IsNullOrEmpty(option.id) || !ids.Add(option.id) ||
                !TryGetVisual(option.sprite, option.region, out texture, out uv, out aspect))
            { reason = "選項圖片缺少、裁切範圍無效，或選項ID重複。"; return false; }
            if (option.id == q.correctOptionId) hasCorrect = true;
        }
        if (!hasCorrect) { reason = "正確答案ID不在本題選項中。"; return false; }
        return true;
    }

    public static bool TryGetVisual(Sprite sprite, Rect region, out Texture texture,
        out Rect uv, out float aspect)
    {
        texture = null; uv = new Rect(); aspect = 1;
        if (sprite == null || sprite.texture == null || sprite.packed ||
            float.IsNaN(region.x + region.y + region.width + region.height) ||
            region.x < 0 || region.y < 0 || region.width <= 0 || region.height <= 0 ||
            region.xMax > 1.00001f || region.yMax > 1.00001f) return false;
        Rect sr = sprite.rect;
        if (sr.width <= 0 || sr.height <= 0) return false;
        texture = sprite.texture;
        uv = new Rect((sr.x + region.x * sr.width) / texture.width,
            (sr.y + region.y * sr.height) / texture.height,
            region.width * sr.width / texture.width, region.height * sr.height / texture.height);
        aspect = region.width * sr.width / (region.height * sr.height);
        return true;
    }
}
