using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 用程式畫出共用 UI 圖形（圓角矩形、圓形、狀態圖示），放在 Assets/Common/UI/Sprites。
    /// 全部是白色，使用時再由 Image 的顏色上色。已經存在的檔案不會覆蓋。
    /// </summary>
    public static class UiSpriteGenerator
    {
        public const string Folder = "Assets/Common/UI/Sprites";
        const int Size = 128;
        const float CornerRadius = 32f;
        static readonly Vector2 Center = new Vector2(Size / 2f, Size / 2f);

        public sealed class UiSprites
        {
            public Sprite RoundedRect;
            public Sprite Circle;
            public Sprite Exclaim;
            public Sprite Check;
            public Sprite Lock;
        }

        public static UiSprites EnsureSprites()
        {
            Directory.CreateDirectory(Folder);
            return new UiSprites
            {
                // 9-slice 邊框 40 px，比圓角半徑大，拉伸時圓角不會變形。
                RoundedRect = Ensure("RoundedRect", p => RoundedBox(p, Center, new Vector2(63f, 63f), CornerRadius), new Vector4(40, 40, 40, 40)),
                Circle = Ensure("Circle", p => (p - Center).magnitude - 63f, Vector4.zero),
                Exclaim = Ensure("IconExclaim", p => Mathf.Min(
                    Segment(p, new Vector2(64, 56), new Vector2(64, 100), 11f),
                    (p - new Vector2(64, 30)).magnitude - 11f), Vector4.zero),
                Check = Ensure("IconCheck", p => Mathf.Min(
                    Segment(p, new Vector2(34, 66), new Vector2(56, 44), 11f),
                    Segment(p, new Vector2(56, 44), new Vector2(96, 86), 11f)), Vector4.zero),
                Lock = Ensure("IconLock", LockShape, Vector4.zero),
            };
        }

        static float LockShape(Vector2 p)
        {
            float body = RoundedBox(p, new Vector2(64, 46), new Vector2(30, 24), 6f);
            float keyhole = (p - new Vector2(64, 50)).magnitude - 7f;
            float bodyWithHole = Mathf.Max(body, -keyhole);

            var shackleCenter = new Vector2(64, 72);
            float shackle = p.y >= shackleCenter.y
                ? Mathf.Abs((p - shackleCenter).magnitude - 20f) - 6f
                : Mathf.Min(Segment(p, new Vector2(44, 62), new Vector2(44, 72), 6f),
                            Segment(p, new Vector2(84, 62), new Vector2(84, 72), 6f));
            return Mathf.Min(bodyWithHole, shackle);
        }

        static Sprite Ensure(string name, Func<Vector2, float> distance, Vector4 border)
        {
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
                Write(path, distance, border);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Write(string path, Func<Vector2, float> distance, Vector4 border)
        {
            // 依到形狀邊緣的距離計算透明度，邊緣有 1 px 的反鋸齒。
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float alpha = Mathf.Clamp01(0.5f - distance(new Vector2(x + 0.5f, y + 0.5f)));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        static float RoundedBox(Vector2 p, Vector2 center, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half + new Vector2(radius, radius);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude - radius;
        }
    }
}
