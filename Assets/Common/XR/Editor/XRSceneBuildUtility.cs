using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace TrackingNIU.XR.Editor
{
    /// <summary>
    /// 建置 MR 場景用的共用工具。各關的 Editor 建置腳本可以直接使用：
    /// 建立關卡根物件、灰盒物件、可抓取物件、插槽、材質，並存檔加入 Build Settings。
    /// </summary>
    public static class XRSceneBuildUtility
    {
        public const string FontPath = "Assets/Common/Fonts/NotoSansTC-Regular SDF.asset";

        /// <summary>詢問是否儲存目前場景，然後開一個空場景。使用者取消時回傳 false。</summary>
        public static bool TryNewScene(out Scene scene)
        {
            scene = default;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            return true;
        }

        /// <summary>建立掛有 XRLevelRoot 的場景根物件。</summary>
        public static GameObject CreateLevelRoot(Vector2 requiredSize)
        {
            var root = new GameObject("LevelRoot");
            var levelRoot = root.AddComponent<XRLevelRoot>();
            SetVector2(levelRoot, "requiredSize", requiredSize);
            return root;
        }

        /// <summary>加上 LevelTestLauncher，單獨打開場景按 Play 時自動開始測試 Session。</summary>
        public static LevelTestLauncher AddTestLauncher(Transform parent, LevelId levelId)
        {
            var go = new GameObject("LevelTestLauncher");
            go.transform.SetParent(parent, false);
            var launcher = go.AddComponent<LevelTestLauncher>();
            var so = new SerializedObject(launcher);
            so.FindProperty("levelId").enumValueIndex = (int)levelId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return launcher;
        }

        public static Material GetOrCreateMaterial(string path, Color color)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        /// <summary>建立灰盒方塊。size 是實際尺寸（公尺）。</summary>
        public static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 size,
            Material material, bool keepCollider = true)
            => CreatePrimitive(PrimitiveType.Cube, name, parent, localPosition, size, material, keepCollider);

        public static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition,
            Vector3 scale, Material material, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>讓物件可以被抓起來（運動學剛體、不會被丟出去、抓哪裡就拿哪裡）。</summary>
        public static XRGrabbable MakeGrabbable(GameObject go, string itemId, bool returnHome = true)
        {
            var body = GetOrAdd<Rigidbody>(go);
            body.isKinematic = true;
            body.useGravity = false;

            var grab = GetOrAdd<XRGrabInteractable>(go);
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;

            var grabbable = GetOrAdd<XRGrabbable>(go);
            SetString(grabbable, "itemId", itemId);
            SetBool(grabbable, "returnHomeOnRelease", returnHome);
            return grabbable;
        }

        /// <summary>讓物件成為插槽。accepted 為 null 或空陣列代表全部接受。</summary>
        public static XRSnapSocket MakeSocket(GameObject go, float radius, string[] accepted, bool lockOnAccept,
            bool singleUse, Transform lockPoint = null)
        {
            var trigger = GetOrAdd<SphereCollider>(go);
            trigger.isTrigger = true;
            trigger.radius = radius;

            GetOrAdd<XRSocketInteractor>(go);

            var socket = GetOrAdd<XRSnapSocket>(go);
            var so = new SerializedObject(socket);
            var ids = so.FindProperty("acceptedIds");
            ids.arraySize = accepted?.Length ?? 0;
            for (var i = 0; i < ids.arraySize; i++)
                ids.GetArrayElementAtIndex(i).stringValue = accepted[i];
            so.FindProperty("lockOnAccept").boolValue = lockOnAccept;
            so.FindProperty("singleUse").boolValue = singleUse;
            so.FindProperty("lockPoint").objectReferenceValue = lockPoint;
            so.ApplyModifiedPropertiesWithoutUndo();
            return socket;
        }

        public static XRPokeTarget MakePokeTarget(GameObject go)
        {
            if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();
            return GetOrAdd<XRPokeTarget>(go);
        }

        /// <summary>取得元件，沒有就加上。（編輯器中 GetComponent 找不到時不是真正的 null，不能用 ??。）</summary>
        public static T GetOrAdd<T>(GameObject go) where T : Component
            => go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();

        public static TMP_FontAsset LoadFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                Debug.LogWarning($"[MR 建置] 找不到字型 {FontPath}，中文可能無法顯示。");
            return font;
        }

        public static void SaveScene(Scene scene, string path, bool addToBuild = true)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            if (addToBuild)
                AddSceneToBuild(path);
            Debug.Log($"[MR 建置] 已儲存 {path}");
        }

        public static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.FindIndex(s => s.path == path);
            if (existing >= 0)
            {
                if (!scenes[existing].enabled)
                {
                    scenes[existing] = new EditorBuildSettingsScene(path, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        public static string FindPrefab(params string[] names)
        {
            foreach (var name in names)
            {
                foreach (var guid in AssetDatabase.FindAssets($"\"{name}\" t:Prefab"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) == name)
                        return path;
                }
            }
            return null;
        }

        public static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefs(Object target, string property, IList<Object> values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(property);
            array.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetString(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector2(Object target, string property, Vector2 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetVector3(Object target, string property, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
