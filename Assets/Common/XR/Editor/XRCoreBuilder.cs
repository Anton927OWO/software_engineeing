using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TrackingNIU.XR.Samples;
using static TrackingNIU.XR.Editor.XRSceneBuildUtility;

namespace TrackingNIU.XR.Editor
{
    /// <summary>
    /// MR 原型的第 3 步：產生 MRCore、WorldPanel prefab，以及範本場景、共用元件示範場景與 MR 地圖場景，
    /// 並在 Boot 場景的 GameManager 勾選 MR 模式。可以重複執行，會覆蓋這些檔案。
    /// </summary>
    public static class XRCoreBuilder
    {
        const string MenuRoot = "尋跡宜大/MR 原型/";
        const string XRRoot = "Assets/Common/XR";
        const string ResourcesFolder = XRRoot + "/Resources/TrackingNIU";
        const string MaterialsFolder = XRRoot + "/Materials";
        public const string MRCorePrefabPath = ResourcesFolder + "/MRCore.prefab";
        public const string WorldPanelPrefabPath = ResourcesFolder + "/WorldPanel.prefab";
        public const string TemplateScenePath = XRRoot + "/Templates/L_Template_XR.unity";
        public const string DemoScenePath = XRRoot + "/Samples/XR_Components_Demo.unity";
        public const string MapScenePath = "Assets/Scenes/Map_XR.unity";
        const string BootScenePath = "Assets/Scenes/Boot.unity";

        static readonly Vector2 TableSize = new Vector2(1.0f, 0.6f);

        [MenuItem(MenuRoot + "3. 建立 MRCore 與 MR 場景", priority = 3)]
        static void BuildAll()
        {
            var rigPath = FindPrefab("XR Origin (XR Rig)");
            if (rigPath == null)
            {
                EditorUtility.DisplayDialog("找不到 XR Origin", "找不到 XR Interaction Toolkit 的「XR Origin (XR Rig)」prefab。\n請先執行「2. 匯入範例並設定專案」。", "好");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildWorldPanelPrefab();
            BuildMRCorePrefab(rigPath);
            BuildTemplateScene();
            BuildDemoScene();
            BuildMapScene();
            EnableMixedRealityInBoot();
            AssetDatabase.SaveAssets();

            Debug.Log("[MR 建置] 完成。可以打開 Common/XR/Samples/XR_Components_Demo 或 Scenes/Map_XR 按 Play 測試；" +
                      "接著執行「尋跡宜大 → MR 原型 → 關卡 → 序章」建立序章場景。");
        }

        // ───────────── WorldPanel ─────────────

        static void BuildWorldPanelPrefab()
        {
            EnsureFolder(ResourcesFolder);
            var font = LoadFont();

            var root = new GameObject("WorldPanel", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(860f, 400f);
            rect.pivot = new Vector2(0.5f, 0f);
            root.transform.localScale = Vector3.one * 0.001f;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            root.AddComponent<Image>().color = new Color(0.96f, 0.92f, 0.85f, 0.96f);

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 32, 32);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = CreateText("Title", root.transform, font, 46f, FontStyles.Bold, new Color(0.23f, 0.16f, 0.1f));
            title.text = "標題";
            var body = CreateText("Body", root.transform, font, 34f, FontStyles.Normal, new Color(0.25f, 0.22f, 0.2f));
            body.text = "內文";

            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);
            var grid = row.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(380f, 76f);
            grid.spacing = new Vector2(16f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            var template = new GameObject("ButtonTemplate", typeof(RectTransform));
            template.transform.SetParent(row.transform, false);
            var image = template.AddComponent<Image>();
            image.color = new Color(0.31f, 0.54f, 0.36f);
            var button = template.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.85f, 1f, 0.85f);
            colors.pressedColor = new Color(0.7f, 0.85f, 0.7f);
            button.colors = colors;
            var label = CreateText("Label", template.transform, font, 32f, FontStyles.Bold, Color.white);
            label.text = "按鈕";
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            template.SetActive(false);

            var panel = root.AddComponent<WorldPanel>();
            SetRef(panel, "canvas", canvas);
            SetRef(panel, "title", title);
            SetRef(panel, "body", body);
            SetRef(panel, "buttonRow", row.transform);
            SetRef(panel, "buttonTemplate", button);

            PrefabUtility.SaveAsPrefabAsset(root, WorldPanelPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[MR 建置] 已建立 {WorldPanelPrefabPath}");
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // ───────────── MRCore ─────────────

        static void BuildMRCorePrefab(string rigPath)
        {
            EnsureFolder(ResourcesFolder);

            var root = new GameObject("MRCore");
            var core = root.AddComponent<MRCore>();

            // 玩家物件：沿用 XR Interaction Toolkit 的 Starter Assets，關掉移動與傳送（MR 原型不需要走動）。
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, root.transform);
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
            {
                if (t != rig.transform && (t.name.Contains("Locomotion") || t.name.Contains("Teleport")))
                    t.gameObject.SetActive(false);
            }

            var origin = rig.GetComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            var cam = origin.Camera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.nearClipPlane = 0.05f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            GetOrAdd<ARCameraManager>(cam.gameObject);  // Meta OpenXR 以此開啟 passthrough

            var planes = GetOrAdd<ARPlaneManager>(rig);
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;

            var session = new GameObject("AR Session");
            session.transform.SetParent(root.transform, false);
            session.AddComponent<ARSession>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(root.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;

            var table = BuildTable(root.transform);

            var detector = root.AddComponent<TableDetector>();
            SetRef(detector, "planeManager", planes);
            SetRef(detector, "table", table);

            SetRef(core, "xrOrigin", origin);
            SetRef(core, "tableAnchor", table);
            var simulatorPath = FindPrefab("XR Interaction Simulator", "XR Device Simulator");
            if (simulatorPath != null)
                SetRef(core, "simulatorPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(simulatorPath));
            else
                Debug.LogWarning("[MR 建置] 找不到 XR 模擬器 prefab，編輯器裡沒有頭盔時將無法操作。請從 Package Manager 匯入 XR Interaction Toolkit 的模擬器範例。");

            PrefabUtility.SaveAsPrefabAsset(root, MRCorePrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[MR 建置] 已建立 {MRCorePrefabPath}");
        }

        static TableAnchor BuildTable(Transform parent)
        {
            var wood = GetOrCreateMaterial(MaterialsFolder + "/VirtualTable.mat", Hex("#C9A27A"));
            var accent = GetOrCreateMaterial(MaterialsFolder + "/TableHandle.mat", Hex("#3D6A8C"));

            var tableGo = new GameObject("Table");
            tableGo.transform.SetParent(parent, false);
            var anchor = tableGo.AddComponent<TableAnchor>();

            // 桌面比可用範圍大一點；碰撞體留著，使用真實桌面時只隱藏外觀。
            var top = CreateBox("TableTop", tableGo.transform, new Vector3(0f, -0.015f, 0f),
                new Vector3(TableSize.x + 0.1f, 0.03f, TableSize.y + 0.1f), wood);
            var renderers = new System.Collections.Generic.List<Object> { top.GetComponent<Renderer>() };
            const float legLength = 0.72f;
            var hx = TableSize.x / 2f;
            var hz = TableSize.y / 2f;
            foreach (var p in new[] { new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(-hx, hz), new Vector2(hx, hz) })
            {
                var leg = CreateBox("Leg", tableGo.transform, new Vector3(p.x, -0.03f - legLength / 2f, p.y),
                    new Vector3(0.04f, legLength, 0.04f), wood, keepCollider: false);
                renderers.Add(leg.GetComponent<Renderer>());
            }

            var handle = CreatePrimitive(PrimitiveType.Sphere, "MoveHandle", tableGo.transform,
                new Vector3(0f, 0.02f, -hz - 0.08f), Vector3.one * 0.05f, accent);
            handle.AddComponent<XRSimpleInteractable>();
            var mover = handle.AddComponent<TableMoveHandle>();
            SetRef(mover, "table", anchor);

            SetVector2(anchor, "size", TableSize);
            SetRefs(anchor, "virtualTableRenderers", renderers);
            SetRef(anchor, "moveHandle", handle);
            return anchor;
        }

        // ───────────── 場景 ─────────────

        static void BuildTemplateScene()
        {
            if (!TryNewScene(out var scene))
                return;
            var root = CreateLevelRoot(TableSize);
            AddTestLauncher(root.transform, LevelId.Prologue);
            new GameObject("Content").transform.SetParent(root.transform, false);

            var blue = GetOrCreateMaterial(MaterialsFolder + "/Demo_Blue.mat", Hex("#3D6A8C"));
            var green = GetOrCreateMaterial(MaterialsFolder + "/Demo_Green.mat", Hex("#4F8A5B"));
            var cube = CreateBox("Example_Grab", root.transform, new Vector3(-0.2f, 0.03f, -0.1f), Vector3.one * 0.06f, blue);
            MakeGrabbable(cube, "example");
            CreatePrimitive(PrimitiveType.Cylinder, "Example_SocketPad", root.transform, new Vector3(0.2f, 0.003f, 0f),
                new Vector3(0.12f, 0.003f, 0.12f), green, keepCollider: false);
            var socket = new GameObject("Example_Socket");
            socket.transform.SetParent(root.transform, false);
            socket.transform.localPosition = new Vector3(0.2f, 0.04f, 0f);
            MakeSocket(socket, 0.07f, new[] { "example" }, lockOnAccept: true, singleUse: true);

            SaveScene(scene, TemplateScenePath, addToBuild: false);
        }

        static void BuildDemoScene()
        {
            if (!TryNewScene(out var scene))
                return;
            var root = CreateLevelRoot(TableSize);
            var blue = GetOrCreateMaterial(MaterialsFolder + "/Demo_Blue.mat", Hex("#3D6A8C"));
            var red = GetOrCreateMaterial(MaterialsFolder + "/Demo_Red.mat", Hex("#C0473A"));
            var green = GetOrCreateMaterial(MaterialsFolder + "/Demo_Green.mat", Hex("#4F8A5B"));
            var yellow = GetOrCreateMaterial(MaterialsFolder + "/Demo_Yellow.mat", Hex("#E0B040"));
            var soil = GetOrCreateMaterial(MaterialsFolder + "/Demo_Soil.mat", Hex("#7A5A3A"));
            var wood = GetOrCreateMaterial(MaterialsFolder + "/Demo_Wood.mat", Hex("#B08050"));

            var blueCube = MakeGrabbable(CreateBox("Cube_Blue", root.transform, new Vector3(-0.32f, 0.03f, -0.08f), Vector3.one * 0.06f, blue), "blue");
            var redCube = MakeGrabbable(CreateBox("Cube_Red", root.transform, new Vector3(-0.2f, 0.03f, -0.08f), Vector3.one * 0.06f, red), "red");

            CreatePrimitive(PrimitiveType.Cylinder, "SocketPad", root.transform, new Vector3(-0.05f, 0.003f, 0.05f),
                new Vector3(0.12f, 0.003f, 0.12f), green, keepCollider: false);
            var socketGo = new GameObject("Socket_AcceptsBlue");
            socketGo.transform.SetParent(root.transform, false);
            socketGo.transform.localPosition = new Vector3(-0.05f, 0.04f, 0.05f);
            var socket = MakeSocket(socketGo, 0.07f, new[] { "blue" }, lockOnAccept: true, singleUse: true);

            var button = CreateBox("ResetButton", root.transform, new Vector3(0.08f, 0.015f, -0.15f), new Vector3(0.06f, 0.03f, 0.06f), yellow);
            var poke = MakePokeTarget(button);

            // 刷子：握柄＋刷毛，尖端在刷毛下方。
            var brush = new GameObject("Brush");
            brush.transform.SetParent(root.transform, false);
            brush.transform.localPosition = new Vector3(0.3f, 0.02f, -0.15f);
            CreateBox("Handle", brush.transform, Vector3.zero, new Vector3(0.02f, 0.02f, 0.14f), wood);
            CreateBox("Bristles", brush.transform, new Vector3(0f, -0.005f, 0.085f), new Vector3(0.045f, 0.02f, 0.03f), soil);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(brush.transform, false);
            tip.localPosition = new Vector3(0f, -0.018f, 0.085f);
            MakeGrabbable(brush, "brush");
            var tool = brush.AddComponent<BrushTool>();
            SetRef(tool, "tip", tip);

            var soilBlock = CreateBox("SoilBlock", root.transform, new Vector3(0.3f, 0.02f, 0.1f), new Vector3(0.16f, 0.04f, 0.16f), soil);
            soilBlock.AddComponent<DemoBrushSurface>();

            var demo = new GameObject("Demo").AddComponent<XRComponentsDemo>();
            demo.transform.SetParent(root.transform, false);
            SetRef(demo, "socket", socket);
            SetRef(demo, "resetButton", poke);
            SetRefs(demo, "grabbables", new Object[] { blueCube, redCube });

            SaveScene(scene, DemoScenePath, addToBuild: false);
        }

        static void BuildMapScene()
        {
            if (!TryNewScene(out var scene))
                return;
            var root = CreateLevelRoot(TableSize);
            var controller = new GameObject("MapController").AddComponent<MapXRController>();
            controller.transform.SetParent(root.transform, false);
            SaveScene(scene, MapScenePath);
        }

        static void EnableMixedRealityInBoot()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) == null)
            {
                Debug.LogWarning($"[MR 建置] 找不到 {BootScenePath}，請自行在 GameManager 勾選 Mixed Reality。");
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager == null)
            {
                Debug.LogWarning("[MR 建置] Boot 場景裡沒有 GameManager。");
                return;
            }
            SetBool(manager, "mixedReality", true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MR 建置] Boot 的 GameManager 已切換為 MR 模式（從 Boot 按 Play 會進入 Map_XR）。");
        }
    }
}
