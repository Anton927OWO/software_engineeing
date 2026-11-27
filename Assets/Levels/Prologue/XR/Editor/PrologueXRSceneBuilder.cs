using TrackingNIU;
using TrackingNIU.XR;
using UnityEditor;
using UnityEngine;
using static TrackingNIU.XR.Editor.XRSceneBuildUtility;

namespace XunJiYiDa.Levels.Prologue.XR.Editor
{
    /// <summary>產生序章 MR 場景 L_Prologue_XR（灰盒）。可以重複執行，會覆蓋場景。</summary>
    public static class PrologueXRSceneBuilder
    {
        const string Folder = "Assets/Levels/Prologue/XR";
        const string ScenePath = Folder + "/L_Prologue_XR.unity";
        const string MaterialFolder = Folder + "/Materials";

        static readonly Vector3 BookSize = new Vector3(0.035f, 0.2f, 0.15f);

        [MenuItem("尋跡宜大/MR 原型/關卡/序章（L_Prologue_XR）", priority = 20)]
        static void Build()
        {
            if (!TryNewScene(out var scene))
                return;

            var root = CreateLevelRoot(new Vector2(0.9f, 0.5f));
            AddTestLauncher(root.transform, LevelId.Prologue);

            var red = GetOrCreateMaterial(MaterialFolder + "/Book_Red.mat", Hex("#C0473A"));
            var blue = GetOrCreateMaterial(MaterialFolder + "/Book_Blue.mat", Hex("#3D6A8C"));
            var yellow = GetOrCreateMaterial(MaterialFolder + "/Book_Yellow.mat", Hex("#E0B040"));
            var boxMat = GetOrCreateMaterial(MaterialFolder + "/BookBox.mat", Hex("#9C7146"));
            var mossMat = GetOrCreateMaterial(MaterialFolder + "/Moss.mat", Hex("#6E8B5A"));

            // 左邊散落的書（擺放順序故意和收集順序不同）。
            var books = new GameObject("Books").transform;
            books.SetParent(root.transform, false);
            CreateBox("Ground", books, new Vector3(-0.22f, 0.002f, 0f), new Vector3(0.36f, 0.004f, 0.3f), mossMat, keepCollider: false);
            var redBook = CreateBook("Book_Red", "red", books, new Vector3(-0.14f, 0f, 0.04f), 12f, red);
            var yellowBook = CreateBook("Book_Yellow", "yellow", books, new Vector3(-0.32f, 0f, 0.02f), -8f, yellow);
            var blueBook = CreateBook("Book_Blue", "blue", books, new Vector3(-0.23f, 0f, -0.07f), 25f, blue);

            // 右邊的書箱：底板＋四面牆，上方是插槽。
            var box = new GameObject("BookBox").transform;
            box.SetParent(root.transform, false);
            box.localPosition = new Vector3(0.24f, 0f, 0f);
            const float w = 0.26f, d = 0.2f, h = 0.12f, t = 0.012f;
            CreateBox("Bottom", box, new Vector3(0f, t / 2f, 0f), new Vector3(w, t, d), boxMat);
            CreateBox("Wall_Front", box, new Vector3(0f, h / 2f, -d / 2f), new Vector3(w, h, t), boxMat);
            CreateBox("Wall_Back", box, new Vector3(0f, h / 2f, d / 2f), new Vector3(w, h, t), boxMat);
            CreateBox("Wall_Left", box, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(t, h, d), boxMat);
            CreateBox("Wall_Right", box, new Vector3(w / 2f, h / 2f, 0f), new Vector3(t, h, d), boxMat);

            var slots = new Transform[3];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = new GameObject($"Slot_{i + 1}").transform;
                slots[i].SetParent(box, false);
                slots[i].localPosition = new Vector3(-0.07f + 0.07f * i, t + BookSize.y / 2f, 0f);
            }

            var socketGo = new GameObject("BoxSocket");
            socketGo.transform.SetParent(box, false);
            socketGo.transform.localPosition = new Vector3(0f, h + 0.04f, 0f);
            // 順序由 PrologueXRController 判斷，插槽本身全部接受、不鎖、可重複使用。
            var socket = MakeSocket(socketGo, 0.13f, null, lockOnAccept: false, singleUse: false);

            // 控制器與音效。
            var controllerGo = new GameObject("PrologueXRController");
            controllerGo.transform.SetParent(root.transform, false);
            var sfx = controllerGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0.6f;
            var controller = controllerGo.AddComponent<PrologueXRController>();

            var so = new SerializedObject(controller);
            var list = so.FindProperty("books");
            list.arraySize = 3;
            SetBook(list.GetArrayElementAtIndex(0), redBook, "紅色", "#D9483B");
            SetBook(list.GetArrayElementAtIndex(1), blueBook, "藍色", "#3D7CC9");
            SetBook(list.GetArrayElementAtIndex(2), yellowBook, "黃色", "#C99A1E");
            so.FindProperty("bookBox").objectReferenceValue = socket;
            var slotList = so.FindProperty("boxSlots");
            slotList.arraySize = slots.Length;
            for (var i = 0; i < slots.Length; i++)
                slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.FindProperty("sfx").objectReferenceValue = sfx;
            so.FindProperty("clickClip").objectReferenceValue = LoadClip("Assets/Levels/Prologue/Audio/select_001.ogg");
            so.FindProperty("correctClip").objectReferenceValue = LoadClip("Assets/Levels/Prologue/Audio/drop_001.ogg");
            so.FindProperty("completeClip").objectReferenceValue = LoadClip("Assets/Levels/Prologue/Audio/confirmation_001.ogg");
            so.FindProperty("wrongClip").objectReferenceValue = LoadClip("Assets/Common/audio/kenney_interface-sounds/Audio/error_004.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, ScenePath);
        }

        static XRGrabbable CreateBook(string name, string id, Transform parent, Vector3 position, float yaw, Material material)
        {
            var book = CreateBox(name, parent, position + new Vector3(0f, BookSize.y / 2f, 0f), BookSize, material);
            book.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return MakeGrabbable(book, id);
        }

        static void SetBook(SerializedProperty element, XRGrabbable book, string displayName, string colorHex)
        {
            element.FindPropertyRelative("book").objectReferenceValue = book;
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("colorHex").stringValue = colorHex;
        }

        static AudioClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"[序章 MR] 找不到音效 {path}");
            return clip;
        }
    }
}
