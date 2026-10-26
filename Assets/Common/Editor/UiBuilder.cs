using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TrackingNIU.EditorTools
{
    /// <summary>
    /// 用程式建立 UI 的小工具，地圖與結算畫面的場景產生器共用。
    /// 座標一律以 1920×1080 畫面的左上角為原點，和《關卡畫面設計說明》的示意圖一致。
    /// </summary>
    public sealed class UiBuilder
    {
        public const float OutlineWidth = 4f;

        public UiSpriteGenerator.UiSprites Sprites { get; }
        public TMP_FontAsset Regular { get; }
        public TMP_FontAsset Bold { get; }
        public TMP_FontAsset Serif { get; }

        public UiBuilder()
        {
            Sprites = UiSpriteGenerator.EnsureSprites();
            Regular = LoadFont("NotoSansTC-Regular SDF");
            Bold = LoadFont("NotoSansTC-Bold SDF");
            Serif = LoadFont("NotoSerifTC-Black SDF");
        }

        static TMP_FontAsset LoadFont(string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"Assets/Common/Fonts/{name}.asset");
            if (font == null)
                throw new FileNotFoundException($"找不到字型 {name}，請先執行 Tracking NIU → 建立中文字型 Font Asset。");
            return font;
        }

        /// <summary>建立相機、EventSystem 與 1920×1080 的 Canvas，回傳置中的 1920×1080 畫面區域。</summary>
        public RectTransform CreateScreen(string canvasName, out Canvas canvas)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiPalette.Background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasObject = new GameObject(canvasName, typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var background = Stretch("Background", canvasObject.transform);
            AddImage(background, null, UiPalette.Background);

            // 非 16:9 的螢幕上，內容維持 1920×1080 置中，多出來的部分顯示背景色。
            var frame = (RectTransform)new GameObject("Frame", typeof(RectTransform)).transform;
            frame.SetParent(canvasObject.transform, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(1920f, 1080f);
            return frame;
        }

        /// <summary>上方 120 px 的 HUD：底色、左側標題，下緣 8 px 區域色條。</summary>
        public RectTransform CreateHud(RectTransform frame, string title, Color stripColor, out TMP_Text subtitle)
        {
            var hud = Node("HUD", frame, 0, 0, 1920, 120);
            AddImage(hud, null, UiPalette.Panel);
            AddImage(Node("ZoneStrip", hud, 0, 112, 1920, 8), null, stripColor);
            AddText(hud, "Title", 64, 2, 1000, 70, title, Bold, 48, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            subtitle = AddText(hud, "Subtitle", 64, 70, 1100, 42, "", Regular, 28, UiPalette.Text, TextAlignmentOptions.MidlineLeft);
            return hud;
        }

        public static RectTransform Node(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color, float pixelsPerUnitMultiplier = 1f)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }
            return image;
        }

        /// <summary>4 px 深色外框的圓角面板。回傳內層的填色 Image。</summary>
        public Image OutlinedPanel(string name, Transform parent, float x, float y, float width, float height, Color fill, out RectTransform root)
        {
            root = Node(name, parent, x, y, width, height);
            AddImage(root, Sprites.RoundedRect, UiPalette.Text);
            // 內層圓角半徑少 4 px，外框寬度才會一致。
            float innerScale = CornerRadius / (CornerRadius - OutlineWidth);
            return AddImage(Stretch("Fill", root, OutlineWidth), Sprites.RoundedRect, fill, innerScale);
        }

        const float CornerRadius = 32f;

        /// <summary>
        /// 思源黑體的行高約是字級的 1.45 倍，height 要留足，否則自動縮放會把字縮小。
        /// 有設定 minAutoSize 的文字太長時會先縮小、再以「…」截斷；沒有設定的文字不會被裁掉。
        /// </summary>
        public static TextMeshProUGUI AddText(Transform parent, string name, float x, float y, float width, float height,
            string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment, float minAutoSize = 0f)
        {
            var label = Node(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            // Ellipsis 在第一行超出高度時會整段不顯示，所以只用在會自動縮放的文字。
            label.overflowMode = minAutoSize > 0f ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            label.raycastTarget = false;
            if (minAutoSize > 0f)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = minAutoSize;
                label.fontSizeMax = size;
            }
            return label;
        }

        /// <summary>按鈕外觀由程式控制（例如停用時改成灰色），所以 Button 本身只做按下時的明暗變化。</summary>
        public static Button AddButton(GameObject target, Graphic graphic)
        {
            var button = target.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        public StatusIcon AddStatusIcon(Transform parent, string name, float x, float y, float size)
        {
            var root = Node(name, parent, x, y, size, size);
            var circle = AddImage(root, Sprites.Circle, UiPalette.Disabled);
            var symbol = AddImage(Stretch("Symbol", root), Sprites.Lock, Color.white);
            symbol.raycastTarget = false;

            var icon = root.gameObject.AddComponent<StatusIcon>();
            Wire(icon, ("circle", circle), ("symbol", symbol),
                ("availableSprite", Sprites.Exclaim), ("completedSprite", Sprites.Check), ("lockedSprite", Sprites.Lock));
            return icon;
        }

        /// <summary>設定元件的 [SerializeField] 欄位。值可以是 Object、Object 陣列、Color、bool 或 enum。</summary>
        public static void Wire(Object target, params (string field, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                    throw new KeyNotFoundException($"{target.GetType().Name} 沒有欄位 {field}");

                switch (value)
                {
                    case Object reference:
                        property.objectReferenceValue = reference;
                        break;
                    case Object[] references:
                        property.arraySize = references.Length;
                        for (int i = 0; i < references.Length; i++)
                            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
                        break;
                    case Color color:
                        property.colorValue = color;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                    case System.Enum enumValue:
                        property.intValue = System.Convert.ToInt32(enumValue);
                        break;
                    default:
                        throw new System.ArgumentException($"不支援的欄位型別：{value?.GetType().Name}");
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>檢查場景中本專案元件的物件欄位都有設定，回傳缺少的數量。</summary>
        public static int ValidateWiring(params string[] optionalFields)
        {
            int missing = 0;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour.GetType().Namespace != "TrackingNIU")
                    continue;

                var property = new SerializedObject(behaviour).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null)
                        continue;
                    if (System.Array.IndexOf(optionalFields, property.name) >= 0)
                        continue;
                    Debug.LogError($"[場景產生器] {behaviour.name}（{behaviour.GetType().Name}）的 {property.propertyPath} 沒有設定");
                    missing++;
                }
            }
            return missing;
        }

        /// <summary>儲存場景並加進 Build Settings（已在其中則不重複加入）。</summary>
        public void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            DiscardFontChanges();
        }

        /// <summary>
        /// 建立文字時 TextMeshPro 會把用到的字加進 Dynamic Font Asset。這裡清回空白狀態再存檔：
        /// batch mode 下字形表會存進去、圖集卻是空的，執行時那些字就會變成透明。
        /// </summary>
        void DiscardFontChanges()
        {
            foreach (var font in new[] { Regular, Bold, Serif })
            {
                font.ClearFontAssetData();
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssetIfDirty(font);
            }
        }
    }
}
