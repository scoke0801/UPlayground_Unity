using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>시나리오 실행선과 환경 부품을 분리해 새 플레이 맵을 저작한다.</summary>
    public static partial class ScenarioMapAuthoring
    {
        private const string SourceScenePath = "Assets/01.Scenes/Ingame/LakeOfLife.unity";
        private const string LayoutPath = "Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario.layout.json";
        private const string ReportDirectory = "output/ScenarioMap";

        [Serializable] private sealed class Inspection
        {
            public List<SceneEntry> entries = new();
            public List<AssetEntry> assets = new();
        }

        [Serializable] private sealed class AssetRequest { public string[] paths; }

        [Serializable] private sealed class SceneEntry
        {
            public string path;
            public bool active;
            public Vector3 position;
            public float groundHeight;
            public string prefab;
            public List<string> components = new();
            public List<string> bindings = new();
            public List<Bounds> volumes = new();
        }

        [Serializable] private sealed class AssetEntry
        {
            public string path;
            public Vector3 center;
            public Vector3 size;
            public int colliders;
            public int renderers;
        }

        /// <summary>원본 씬을 저장하지 않고 진행 지점과 환경 부품 크기를 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 원본 조사")]
        public static void InspectSources()
        {
            RequireEditMode();
            Directory.CreateDirectory(ReportDirectory);
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = SceneManager.GetSceneByPath(SourceScenePath);
            bool ownsScene = !scene.IsValid() || !scene.isLoaded;
            if (ownsScene) scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                Physics.SyncTransforms();
                var inspection = new Inspection();
                foreach (GameObject root in scene.GetRootGameObjects())
                    InspectTransform(root.transform, root.name, 0, inspection);
                if (File.Exists(ReportDirectory + "/AssetRequest.json"))
                {
                    var request = JsonUtility.FromJson<AssetRequest>(File.ReadAllText(ReportDirectory + "/AssetRequest.json"));
                    foreach (string path in request.paths)
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null)
                            throw new InvalidOperationException("프리팹을 찾을 수 없습니다: " + path);
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
                        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                        Bounds bounds = GetRendererBounds(renderers);
                        inspection.assets.Add(new AssetEntry
                        {
                            path = path, center = bounds.center, size = bounds.size,
                            colliders = instance.GetComponentsInChildren<Collider>().Length,
                            renderers = renderers.Length
                        });
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                }
                File.WriteAllText(ReportDirectory + "/Inspection.json", JsonUtility.ToJson(inspection, true));
                Debug.Log("[ScenarioMap] 원본 조사 완료: " + inspection.entries.Count + "개 진행 지점, " + inspection.assets.Count + "개 환경 부품");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                if (ownsScene) EditorSceneManager.CloseScene(scene, true);
                if (ownsScene && !Application.isBatchMode)
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void InspectTransform(Transform transform, string path, int depth, Inspection inspection)
        {
            if (depth > 0 && transform.root.name == "Environment")
                return;
            Component[] components = transform.GetComponents<Component>();
            bool isPoint = depth < 2;
            foreach (Component component in components)
                if (component != null && (component.GetType().Name.Contains("Trigger")
                    || component.GetType().Name.Contains("Anchor") || component is GameActor
                    || component.GetType().Name == "CameraLookAtPoint"))
                    isPoint = true;
            if (isPoint)
            {
                var entry = new SceneEntry
                {
                    path = path, active = transform.gameObject.activeInHierarchy, position = transform.position,
                    prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject),
                    groundHeight = SampleOriginalGround(transform.position, transform.gameObject.scene)
                };
                foreach (Component component in components)
                {
                    if (component == null) { entry.components.Add("Missing Script"); continue; }
                    entry.components.Add(component.GetType().FullName);
                    if (component is Collider collider) entry.volumes.Add(collider.bounds);
                    if (component is not MonoBehaviour) continue;
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(property.stringValue))
                            entry.bindings.Add(property.propertyPath + "=" + property.stringValue);
                        else if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                        {
                            UnityEngine.Object reference = property.objectReferenceValue;
                            string target = reference is Component other ? GetTransformPath(other.transform) : AssetDatabase.GetAssetPath(reference);
                            entry.bindings.Add(property.propertyPath + "=" + target);
                        }
                    }
                }
                inspection.entries.Add(entry);
            }
            foreach (Transform child in transform)
                InspectTransform(child, path + "/" + child.name, depth + 1, inspection);
        }

        private static float SampleOriginalGround(Vector3 point, Scene scene)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                if (terrain.gameObject.scene != scene) continue;
                Vector3 local = point - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z)
                    return terrain.SampleHeight(point) + terrain.transform.position.y;
            }
            return point.y;
        }

        private static Bounds GetRendererBounds(Renderer[] renderers)
        {
            Bounds bounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static string GetTransformPath(Transform transform) =>
            transform.parent == null ? transform.name : GetTransformPath(transform.parent) + "/" + transform.name;

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 맵을 저작하세요.");
        }
    }
}
