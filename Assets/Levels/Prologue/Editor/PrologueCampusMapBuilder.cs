using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 把 L_Prologue 改成「校園地圖探索尋書」：加入校園地圖、三個地點與知識卡，
/// 書本與書箱移到地圖上。可以重複執行，會先移除上一次產生的地圖與知識卡。
/// 地點座標以 CampusMap.png（1300×820，依神農校區總區地圖重繪、北方朝上）左上角為原點。
/// </summary>
public static class PrologueCampusMapBuilder
{
    const string ScenePath = "Assets/Levels/Prologue/L_Prologue.unity";
    const string MapSpritePath = "Assets/Levels/Prologue/Art/CampusMap.png";
    const float MapWidth = 1300f, MapHeight = 820f;
    static readonly Vector2 MapCenter = new Vector2(0f, -120f);

    static readonly Color Ink = new Color32(36, 61, 50, 255);
    static readonly Color Brown = new Color32(115, 90, 70, 255);
    static readonly Color Paper = new Color32(250, 244, 233, 255);
    static readonly Color Border = new Color32(200, 167, 132, 255);
    static readonly Color Pill = new Color32(250, 244, 233, 235);

    sealed class Spot
    {
        public string Name, Book, Title, Body;
        public Vector2 Pin, BookSpot;
    }

    // 知識卡內容為初稿，請以宜大記憶庫與文化廊道的展板文字校對後再定稿。
    static readonly Spot[] s_places =
    {
        new Spot
        {
            Name = "校門口", Book = "Book1", Pin = new Vector2(1112, 489), BookSpot = new Vector2(1112, 375),
            Title = "百年農林，宜大的起點",
            Body = "宜大的前身是日治時期設立的農林學校，從培育農業人才開始，一路發展成今天的綜合大學。" +
                   "2026 年，宜大迎來創校一百週年。\n走進校門，抬頭就能望見環繞蘭陽平原的群山。",
        },
        new Spot
        {
            Name = "噶瑪蘭文化廊道", Book = "Book2", Pin = new Vector2(818, 690), BookSpot = new Vector2(695, 690),
            Title = "蘭陽平原的主人：噶瑪蘭族",
            Body = "早在漢人大量移入以前，噶瑪蘭族就生活在蘭陽平原，沿著河流與海岸建立許多聚落（社）。" +
                   "\n他們善於捕魚與種稻，也以香蕉絲織布聞名。沿著廊道走一趟，認識這段在地的歷史。",
        },
        new Spot
        {
            Name = "萬斌廳", Book = "Book3", Pin = new Vector2(924, 491), BookSpot = new Vector2(924, 375),
            Title = "從土裡讀歷史：淇武蘭遺址",
            Body = "淇武蘭遺址位在礁溪，是噶瑪蘭族的舊社遺址，出土了陶罐、瑪瑙珠與金屬飾品等大量文物。" +
                   "\n考古學家一層一層挖開土壤，越深的地層通常年代越久遠。接下來的旅程，你也要親手試試看！",
        },
    };

    [MenuItem("Tracking NIU/序章/建立校園地圖版", false, 40)]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先離開 Play Mode。");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var mapSprite = EnsureSprite(MapSpritePath);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string name) => all.FirstOrDefault(t => t != null && t.name == name)
            ?? throw new InvalidOperationException($"L_Prologue 找不到 {name}。");

        var canvas = (RectTransform)Find("Canvas");
        var manager = Find("PrologueManager").GetComponent<PrologueManager>();
        var font = Find("TutorialText").GetComponent<TMP_Text>().font;
        var books = new[] { "Book1", "Book2", "Book3" }.Select(n => (RectTransform)Find(n)).ToArray();
        var bookBox = (RectTransform)Find("BookBox");

        // 移除上一次產生的內容；書本與書箱先移回 Canvas，避免跟著被刪除。
        foreach (var rect in books.Append(bookBox)) rect.SetParent(canvas, false);
        foreach (var old in all.Where(t => t != null && (t.name == "CampusMap" || t.name == "KnowledgeCard")).ToArray())
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

        // 地圖放在背景之上、HUD 之下。
        var map = Node("CampusMap", canvas, MapCenter, new Vector2(MapWidth, MapHeight));
        map.SetSiblingIndex(Find("Background").GetSiblingIndex() + 1);
        var mapImage = map.gameObject.AddComponent<Image>();
        mapImage.sprite = mapSprite;
        mapImage.raycastTarget = false;
        Label(map, "MapTitle", "國立宜蘭大學神農校區", font, 24, Ink, MapPoint(170, 38), new Vector2(280, 44), true);
        Label(map, "ForestLabel", "知識之林・書箱", font, 22, Ink, MapPoint(766, 222), new Vector2(190, 38), true);

        bookBox.SetParent(map, false);
        Place(bookBox, MapPoint(766, 295), new Vector2(150, 112));

        var landmarks = new PrologueLandmark[s_places.Length];
        for (int i = 0; i < s_places.Length; i++)
        {
            var place = s_places[i];
            landmarks[i] = BuildLandmark(map, place, font, manager);
            var book = books.First(b => b.name == place.Book);
            book.SetParent(map, false);
            Place(book, MapPoint(place.BookSpot.x, place.BookSpot.y), new Vector2(124, 93));
        }
        // 書本畫在地點上方，拖曳時不會被地點遮住。
        foreach (var book in books) book.SetAsLastSibling();

        var card = BuildKnowledgeCard(canvas, font, manager, Find("StartButton").gameObject, out var cardSurfaces);
        card.transform.SetSiblingIndex(Find("CollectionText").GetSiblingIndex() + 1);
        Find("PausePanel").SetSiblingIndex(card.transform.GetSiblingIndex() + 1);

        var managerData = new SerializedObject(manager);
        managerData.FindProperty("campusMap").objectReferenceValue = map.gameObject;
        managerData.FindProperty("knowledgeCard").objectReferenceValue = card;
        var landmarkArray = managerData.FindProperty("landmarks");
        landmarkArray.arraySize = landmarks.Length;
        for (int i = 0; i < landmarks.Length; i++)
            landmarkArray.GetArrayElementAtIndex(i).objectReferenceValue = landmarks[i];
        managerData.ApplyModifiedPropertiesWithoutUndo();

        var style = all.Select(t => t != null ? t.GetComponent<PrologueUIStyle>() : null).FirstOrDefault(s => s != null);
        if (style != null)
        {
            var styleData = new SerializedObject(style);
            var surfaces = styleData.FindProperty("surfaces");
            foreach (var image in cardSurfaces)
            {
                surfaces.arraySize++;
                surfaces.GetArrayElementAtIndex(surfaces.arraySize - 1).objectReferenceValue = image;
            }
            styleData.ApplyModifiedPropertiesWithoutUndo();
        }

        SetText(Find("IntroDescription"), "<line-height=130%>圖資館的館員發現，三本記錄宜蘭故事的書被風吹散到校園各處。\n" +
            "打開宜大校園地圖，把書找回來，也認識這座校園與噶瑪蘭的故事！</line-height>");
        SetText(Find("IntroControlsBody"), "<line-height=130%>1. 點擊地圖上閃爍的地點，閱讀知識卡。\n" +
            "2. 把出現的書本拖進「知識之林」的書箱。\n3. 三本書都歸位就完成序章，順序不限。</line-height>");
        SetText(Find("CompleteDescription"), "<line-height=140%><size=36><b><color=#243D32>成功收集三本書！</color></b></size>\n\n" +
            "<voffset=36><color=#735A46>你認識了宜大的百年起點、噶瑪蘭族，\n以及淇武蘭遺址的考古故事。\n" +
            "接下來，循著線索找回散落的文物吧！</color></voffset></line-height>");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("序章已改為校園地圖版：" + ScenePath);
    }

    static PrologueLandmark BuildLandmark(RectTransform map, Spot place, TMP_FontAsset font, PrologueManager manager)
    {
        // 根物件是透明的點擊範圍，涵蓋圖釘與名稱。
        var root = Node("Landmark " + place.Name, map, MapPoint(place.Pin.x, place.Pin.y + 20), new Vector2(150, 130));
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);

        var pin = Node("Pin", root, new Vector2(0, 25), new Vector2(68, 68));
        var pinImage = pin.gameObject.AddComponent<Image>();
        pinImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        pinImage.raycastTarget = false;
        var outline = pin.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3, -3);
        Label(pin, "Mark", "？", font, 36, Color.white, Vector2.zero, new Vector2(68, 68), false);

        var name = Label(root, "Name", place.Name, font, 24, Ink, new Vector2(0, -35), new Vector2(28 * place.Name.Length + 36, 42), true);
        var found = Node("Found", root, new Vector2(70, 55), new Vector2(84, 32));
        var foundBackground = found.gameObject.AddComponent<Image>();
        foundBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        foundBackground.type = Image.Type.Sliced;
        foundBackground.color = new Color32(77, 140, 92, 255);
        foundBackground.raycastTarget = false;
        Label(found, "Text", "已找到", font, 20, Color.white, Vector2.zero, new Vector2(84, 32), false);
        found.gameObject.SetActive(false);

        var landmark = root.gameObject.AddComponent<PrologueLandmark>();
        var data = new SerializedObject(landmark);
        data.FindProperty("displayName").stringValue = place.Name;
        data.FindProperty("cardTitle").stringValue = place.Title;
        data.FindProperty("cardBody").stringValue = place.Body;
        data.FindProperty("bookName").stringValue = place.Book;
        data.FindProperty("pin").objectReferenceValue = pinImage;
        data.FindProperty("nameLabel").objectReferenceValue = name;
        data.FindProperty("foundMark").objectReferenceValue = found.gameObject;
        data.FindProperty("prologueManager").objectReferenceValue = manager;
        data.ApplyModifiedPropertiesWithoutUndo();
        pinImage.color = data.FindProperty("hiddenColor").colorValue;
        return landmark;
    }

    static PrologueKnowledgeCard BuildKnowledgeCard(RectTransform canvas, TMP_FontAsset font, PrologueManager manager,
        GameObject buttonTemplate, out Image[] surfaces)
    {
        var root = Stretch("KnowledgeCard", canvas);
        var card = root.gameObject.AddComponent<PrologueKnowledgeCard>();

        var panel = Stretch("KnowledgeCardPanel", root);
        panel.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        var border = Node("CardBorder", panel, new Vector2(0, -20), new Vector2(1132, 612));
        var borderImage = border.gameObject.AddComponent<Image>();
        borderImage.color = Border;
        var body = Node("Card", panel, new Vector2(0, -20), new Vector2(1120, 600));
        var bodyImage = body.gameObject.AddComponent<Image>();
        bodyImage.color = Paper;
        surfaces = new[] { borderImage, bodyImage };

        var location = Label(body, "Location", "地點", font, 26, Brown, new Vector2(-150, 240), new Vector2(740, 40), false);
        location.alignment = TextAlignmentOptions.MidlineLeft;
        var title = Label(body, "Title", "知識卡", font, 42, Ink, new Vector2(-150, 185), new Vector2(740, 64), false);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.fontStyle = FontStyles.Bold;
        var rule = Node("AccentRule", body, new Vector2(-150, 145), new Vector2(740, 4));
        rule.gameObject.AddComponent<Image>().color = Border;
        var text = Label(body, "Body", "", font, 28, Brown, new Vector2(-150, -5), new Vector2(740, 270), false);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.lineSpacing = 20;

        var icon = Node("BookIcon", body, new Vector2(375, 60), new Vector2(260, 196));
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        Label(body, "BookHint", "你找到了一本書！", font, 24, Ink, new Vector2(375, -65), new Vector2(300, 40), false);

        var button = UnityEngine.Object.Instantiate(buttonTemplate, body);
        button.name = "ConfirmButton";
        button.SetActive(true);
        var buttonRect = (RectTransform)button.transform;
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0, -225);
        var onClick = button.GetComponent<Button>().onClick;
        for (int i = onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(onClick, i);
        UnityEventTools.AddPersistentListener(onClick, new UnityAction(card.OnConfirm));
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.text = "收下書本";

        var data = new SerializedObject(card);
        data.FindProperty("panel").objectReferenceValue = panel.gameObject;
        data.FindProperty("locationText").objectReferenceValue = location;
        data.FindProperty("titleText").objectReferenceValue = title;
        data.FindProperty("bodyText").objectReferenceValue = text;
        data.FindProperty("bookIcon").objectReferenceValue = iconImage;
        data.FindProperty("prologueManager").objectReferenceValue = manager;
        data.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return card;
    }

    static Sprite EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("找不到地圖圖片 " + path);
        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 地圖圖片座標（左上角原點、y 向下）轉成地圖物件內的 anchoredPosition（中心原點、y 向上）。
    static Vector2 MapPoint(float x, float y) => new Vector2(x - MapWidth / 2f, MapHeight / 2f - y);

    static RectTransform Node(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        Place(rect, position, size);
        return rect;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    static RectTransform Stretch(string name, Transform parent)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
        Vector2 position, Vector2 box, bool withPill)
    {
        var rect = Node(name, parent, position, box);
        if (withPill)
        {
            var pill = rect.gameObject.AddComponent<Image>();
            pill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            pill.type = Image.Type.Sliced;
            pill.color = Pill;
            pill.raycastTarget = false;
            rect = Node("Text", rect, Vector2.zero, box);
        }
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    static void SetText(Transform target, string text)
    {
        var label = target.GetComponent<TMP_Text>();
        label.text = text;
        EditorUtility.SetDirty(label);
    }
}
