using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace UPlayGround.EditorTools
{
    /// <summary>열린 씬의 스토리 배치를 변경하지 않고 지면·콜라이더·참조 정보를 기록한다.</summary>
    public static class StoryPlacementAudit
    {
        [Serializable]
        private sealed class Report
        {
            public string scene;
            public bool hasUnsavedChanges;
            public List<Entry> entries = new();
        }

        [Serializable]
        private sealed class ProbeRequest
        {
            public Vector3[] positions;
        }

        [Serializable]
        private sealed class Entry
        {
            public string path;
            public bool active;
            public Vector3 position;
            public Vector3 scale;
            public List<string> components = new();
            public List<string> bindings = new();
            public List<Volume> volumes = new();
            public List<Surface> surfaces = new();
            public List<string> standingObstacles = new();
            public bool hasTerrain;
            public float terrainHeight;
            public float heightAboveTerrain;
            public bool hasNavMesh;
            public Vector3 navMeshPosition;
        }

        [Serializable]
        private sealed class Volume
        {
            public string type;
            public bool enabled;
            public bool trigger;
            public Vector3 center;
            public Vector3 size;
            public List<Vector3> terrainSamples = new();
        }

        [Serializable]
        private sealed class Surface
        {
            public string path;
            public string layer;
            public Vector3 point;
            public Vector3 normal;
        }

        /// <summary>스토리 루트와 주시점의 배치 증거를 Temp/StoryPlacementAudit.json에 저장한다.</summary>
        [UPlaygroundTool("UPlayGround/검증/스토리 배치 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("스토리 배치 검증은 편집 모드에서 실행하세요.");

            Scene scene = SceneManager.GetActiveScene();
            var report = new Report { scene = scene.path, hasUnsavedChanges = scene.isDirty };
            var selected = new HashSet<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    bool isStoryRoot = transform.name.StartsWith("Story_", StringComparison.OrdinalIgnoreCase)
                        || transform.name.StartsWith("[STORY]", StringComparison.OrdinalIgnoreCase);
                    bool isPoint = HasComponent(transform, "CameraLookAtPoint")
                        || HasComponent(transform, "RecruitmentEncounterAnchor")
                        || HasComponent(transform, "FlowGraphTriggerVolume");
                    if (!isStoryRoot && !isPoint) continue;
                    foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
                        selected.Add(child);
                }
            }

            Physics.SyncTransforms();
            foreach (Transform transform in selected)
                report.entries.Add(Capture(transform));
            var graphs = new HashSet<UnityEngine.Object>();
            foreach (Transform transform in selected)
            foreach (MonoBehaviour component in transform.GetComponents<MonoBehaviour>())
            {
                if (component == null || component.GetType().Name != "FlowGraphRunner") continue;
                SerializedProperty graph = new SerializedObject(component).FindProperty("_graph");
                if (graph?.objectReferenceValue != null) graphs.Add(graph.objectReferenceValue);
            }
            foreach (UnityEngine.Object graph in graphs)
                CaptureSpawnPositions(graph, report.entries);
            if (File.Exists("Temp/StoryPlacementProbes.json"))
            {
                var probes = JsonUtility.FromJson<ProbeRequest>(File.ReadAllText("Temp/StoryPlacementProbes.json"));
                foreach (Vector3 position in probes.positions)
                {
                    var entry = new Entry { path = "[Probe] " + position, position = position };
                    CaptureGround(entry);
                    report.entries.Add(entry);
                }
            }
            report.entries.Sort((left, right) => string.CompareOrdinal(left.path, right.path));
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/StoryPlacementAudit.json", JsonUtility.ToJson(report, true));
            Debug.Log($"[StoryPlacementAudit] {scene.name}: {report.entries.Count}개 위치 기록. Temp/StoryPlacementAudit.json");
        }

        private static Entry Capture(Transform transform)
        {
            var entry = new Entry
            {
                path = GetPath(transform), active = transform.gameObject.activeInHierarchy,
                position = transform.position, scale = transform.lossyScale
            };
            foreach (Component component in transform.GetComponents<Component>())
            {
                if (component == null) { entry.components.Add("Missing Script"); continue; }
                entry.components.Add(component.GetType().Name);
                if (component is MonoBehaviour)
                    CaptureBindings(component, entry.bindings);
                if (component is Collider collider)
                    entry.volumes.Add(CaptureVolume(collider));
            }
            CaptureGround(entry);
            return entry;
        }

        private static void CaptureSpawnPositions(UnityEngine.Object graph, List<Entry> entries)
        {
            var serialized = new SerializedObject(graph);
            SerializedProperty property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.Vector3 || property.name != "position") continue;
                string parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'));
                SerializedProperty node = serialized.FindProperty(parentPath);
                string id = node?.FindPropertyRelative("id")?.stringValue;
                var entry = new Entry
                {
                    path = "[Flow] " + AssetDatabase.GetAssetPath(graph) + "/" + id,
                    position = property.vector3Value, active = true
                };
                entry.components.Add("FlowSpawnPosition");
                CaptureGround(entry);
                entries.Add(entry);
            }
        }

        private static void CaptureGround(Entry entry)
        {
            entry.hasTerrain = TrySampleTerrain(entry.position, out entry.terrainHeight);
            entry.heightAboveTerrain = entry.position.y - entry.terrainHeight;
            entry.hasNavMesh = NavMesh.SamplePosition(entry.position, out NavMeshHit nav, 3f, NavMesh.AllAreas);
            entry.navMeshPosition = nav.position;
            if (entry.path.StartsWith("[Probe]", StringComparison.Ordinal)
                || entry.path.StartsWith("[Flow]", StringComparison.Ordinal))
            {
                // 표본의 입력 Y는 광선 시작 높이일 수 있으므로 발밑 Terrain 기준으로 검사한다.
                Vector3 feet = new Vector3(entry.position.x, entry.terrainHeight + 0.05f, entry.position.z);
                foreach (Collider obstacle in Physics.OverlapCapsule(feet + Vector3.up * 0.4f,
                    feet + Vector3.up * 1.5f, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                    entry.standingObstacles.Add(GetPath(obstacle.transform));
            }
            // 상부 수목·다리와 지면을 구분할 수 있도록 하나의 높이를 정답으로 고르지 않는다.
            RaycastHit[] hits = Physics.RaycastAll(entry.position + Vector3.up * 20f,
                Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                entry.surfaces.Add(new Surface
                {
                    path = GetPath(hit.collider.transform), layer = LayerMask.LayerToName(hit.collider.gameObject.layer),
                    point = hit.point, normal = hit.normal
                });
            }
        }

        private static Volume CaptureVolume(Collider collider)
        {
            Bounds bounds = collider.bounds;
            var volume = new Volume
            {
                type = collider.GetType().Name, enabled = collider.enabled, trigger = collider.isTrigger,
                center = bounds.center, size = bounds.size
            };
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, 0f, z));
                if (TrySampleTerrain(point, out float height))
                    volume.terrainSamples.Add(new Vector3(point.x, height, point.z));
            }
            return volume;
        }

        private static void CaptureBindings(Component component, List<string> bindings)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(property.stringValue))
                    bindings.Add(property.propertyPath + "=" + property.stringValue);
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                    continue;
                UnityEngine.Object reference = property.objectReferenceValue;
                string path = reference is Component target ? GetPath(target.transform) : AssetDatabase.GetAssetPath(reference);
                bindings.Add(property.propertyPath + "=" + path);
            }
        }

        private static bool TrySampleTerrain(Vector3 point, out float height)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 local = point - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z) continue;
                height = terrain.SampleHeight(point) + terrain.transform.position.y;
                return true;
            }
            height = 0f;
            return false;
        }

        private static bool HasComponent(Transform transform, string name)
        {
            foreach (Component component in transform.GetComponents<Component>())
                if (component != null && component.GetType().Name == name) return true;
            return false;
        }

        private static string GetPath(Transform transform) =>
            transform.parent == null ? transform.name : GetPath(transform.parent) + "/" + transform.name;
    }
}
