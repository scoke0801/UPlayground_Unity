using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class EnvironmentGroundingSample
        {
            public string path;
            public string prefab;
            public Vector3 pivot;
            public Vector3 bottom;
            public float pivotGap;
            public float geometryGap;
            public Vector3 support;
            public float supportGap;
            public float expectedOffset;
            public float previousSupportGap;
        }

        [Serializable] private sealed class EnvironmentGroundingAudit
        {
            public List<EnvironmentGroundingSample> samples = new();
        }

        /// <summary>환경 프리팹의 피벗과 메시 바닥 높이를 씬 수정 없이 실측한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 환경 접지 조사")]
        public static void InspectEnvironmentGrounding()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 환경 접지를 조사하세요.");
            Layout layout = ReadLayout();
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            Terrain terrain = FindQualityTransform(scene, "AuthoredEnvironment").GetComponentInChildren<Terrain>();
            var audit = new EnvironmentGroundingAudit();
            var vertices = new Dictionary<Mesh, Vector3[]>();
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>())
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(node.gameObject)) continue;
                string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject);
                if (!prefab.StartsWith("Assets/ExternalAssets/Environment/", StringComparison.Ordinal)) continue;
                if (node.GetComponentInChildren<MeshFilter>() == null) continue;
                if (!Array.Exists(node.GetComponentsInChildren<MeshRenderer>(), renderer => renderer.enabled)) continue;
                Bounds bounds = GetPropGeometryBounds(node.gameObject);
                Vector3 bottom = new(bounds.center.x, bounds.min.y, bounds.center.z);
                if (!TryGetEnvironmentSupport(node, vertices, out Vector3 support)) continue;
                audit.samples.Add(new EnvironmentGroundingSample
                {
                    path = GetTransformPath(node), prefab = prefab, pivot = node.position, bottom = bottom,
                    pivotGap = node.position.y - SampleEnvironmentGroundHeight(node.position, terrain),
                    geometryGap = bottom.y - SampleEnvironmentGroundHeight(bottom, terrain),
                    support = support, supportGap = support.y - SampleEnvironmentGroundHeight(support, terrain)
                });
            }
            audit.samples.Sort((first, second) => second.supportGap.CompareTo(first.supportGap));
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/EnvironmentGroundingAudit.json", JsonUtility.ToJson(audit, true));
        }

        private static bool TryGetEnvironmentSupport(Transform node, Dictionary<Mesh, Vector3[]> cache, out Vector3 support)
        {
            support = new Vector3(0, float.PositiveInfinity, 0);
            foreach (MeshFilter filter in node.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer) || !renderer.enabled) continue;
                LODGroup group = filter.GetComponentInParent<LODGroup>();
                if (group != null && !Array.Exists(group.GetLODs()[0].renderers, item => item == renderer)) continue;
                Vector3[] vertices = ReadEnvironmentVertices(filter.sharedMesh, cache);
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 point = filter.transform.TransformPoint(vertex);
                    if (point.y < support.y) support = point;
                }
            }
            return !float.IsPositiveInfinity(support.y);
        }

        private static Vector3[] ReadEnvironmentVertices(Mesh mesh, Dictionary<Mesh, Vector3[]> cache)
        {
            if (cache != null && cache.TryGetValue(mesh, out Vector3[] cached)) return cached;
            // 바람 연출용으로 확장된 bounds를 접지면으로 쓰지 않으며 임포터의 Read/Write 설정도 변경하지 않는다.
            using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
            using var buffer = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp);
            data[0].GetVertices(buffer);
            Vector3[] vertices = buffer.ToArray();
            cache?.Add(mesh, vertices);
            return vertices;
        }

        [Serializable] private sealed class DistrictResident
        {
            public string scenePath;
            public Vector3 position;
            public float yaw;
            public float patrolRadius;
            public float patrolWaitTime;
            public bool enableWander = true;
        }

        /// <summary>마을 구역의 건물 접지와 주민 배치를 갱신하고 이동용 지형 데이터를 저장한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 마을 배치")]
        public static void ApplySettlement()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 마을 배치를 적용하세요.");
            Layout layout = ReadLayout();
            foreach (WorldProp prop in layout.district.props) RequireEnvironmentPrefab(prop.prefab);
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            Transform environment = FindQualityTransform(scene, "AuthoredEnvironment");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            foreach (DistrictResident resident in layout.district.residents)
                RequireDistrictResident(scene, resident);
            string backup = BackupQualityAssets(layout);
            string mapImage = layout.assetDirectory + "/MapBackground.png";
            File.Copy(mapImage, backup + "/MapBackground.png");
            try
            {
                foreach (string name in layout.district.removedPlacements)
                {
                    Transform obsolete = environment.Find(name);
                    if (obsolete != null) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
                }
                foreach (Placement placement in layout.placements)
                {
                    if (!placement.alignBaseToTerrain) continue;
                    Transform existing = environment.Find(placement.name);
                    if (existing != null) AlignEnvironmentBase(placement, terrain, existing.gameObject);
                }
                RebuildDistrictProps(layout.district, terrain, environment);
                ClearSettlementVegetation(layout, environment);
                ApplyDistrictResidents(layout.district, terrain, scene);
                RestoreEnvironmentGrounding(layout, terrain, environment);
                Physics.SyncTransforms();
                BakeNavigation(layout, environment.gameObject);
                SaveQualityScene(scene, layout);
                AssetDatabase.SaveAssets();
                CapturePreviewImages(false);
                CaptureMapBackground(layout, refresh: true);
                File.WriteAllText(ReportDirectory + "/Settlement.txt",
                    "마을 건물 접지·주민 배치 저장\n주민: " + layout.district.residents.Length + "\n백업: " + backup);
            }
            catch
            {
                RestoreQualityBackup(layout, backup);
                File.Copy(backup + "/MapBackground.png", mapImage, true);
                AssetDatabase.ImportAsset(mapImage, ImportAssetOptions.ForceUpdate);
                throw;
            }
        }

        /// <summary>기존 환경의 건물 기단과 식생·소품 접지를 복구하고 이동 데이터를 갱신한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 환경 접지 복구")]
        public static void RepairEnvironmentGrounding()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 환경 접지를 복구하세요.");
            Layout layout = ReadLayout();
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            Transform environment = FindQualityTransform(scene, "AuthoredEnvironment");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            string backup = BackupQualityAssets(layout);
            string mapPath = layout.assetDirectory + "/MapBackground.png";
            File.Copy(mapPath, backup + "/MapBackground.png");
            try
            {
                RestoreEnvironmentGrounding(layout, terrain, environment);
                if (layout.lakeExploration != null)
                    RebuildExplorationWaterBoundary(layout, terrain, environment.Find("PlayBoundaries"));
                Physics.SyncTransforms();
                BakeNavigation(layout, environment.gameObject);
                Validate();
                SaveQualityScene(scene, layout);
                AssetDatabase.SaveAssets();
                CaptureMapBackground(layout, refresh: true);
                CapturePreviewImages(false);
                File.WriteAllText(ReportDirectory + "/Grounding.txt", "환경 접지·동선 검증 및 저장 완료\n백업: " + backup);
            }
            catch
            {
                RestoreQualityBackup(layout, backup);
                File.Copy(backup + "/MapBackground.png", mapPath, true);
                AssetDatabase.ImportAsset(mapPath, ImportAssetOptions.ForceUpdate);
                throw;
            }
        }

        private static void RestoreEnvironmentGrounding(Layout layout, Terrain terrain, Transform environment)
        {
            var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
            var vertices = new Dictionary<Mesh, Vector3[]>();
            var audit = new EnvironmentGroundingAudit();
            // 지형 편집이 모두 끝난 뒤 기단을 먼저 복원해야 주변 식생이 이전 높이에 남지 않는다.
            RestorePropFoundations(layout.district?.props, environment.Find("LandscapeDistrict"), terrain);
            RestorePropFoundations(world.props, environment.Find("WorldExpansionGeometry"), terrain);
            RestorePropFoundations(layout.lakeExploration?.props, environment.Find("LakeExploration"), terrain);
            foreach (Placement placement in layout.placements)
            {
                Transform node = environment.Find(placement.name);
                if (node == null || !placement.alignBaseToTerrain) continue;
                Bounds bounds = GetPropGeometryBounds(node.gameObject);
                Vector3 ground = new(bounds.center.x, bounds.min.y - placement.groundOffset, bounds.center.z);
                LevelPropFootprint(terrain, ground, bounds.size, 0);
            }
            GroundExistingProps(layout.district?.props, environment.Find("LandscapeDistrict"), terrain, vertices, audit);
            GroundExistingProps(world.props, environment.Find("WorldExpansionGeometry"), terrain, vertices, audit);
            GroundExistingProps(layout.lakeExploration?.props, environment.Find("LakeExploration"), terrain, vertices, audit);
            foreach (Placement placement in layout.placements)
            {
                Transform node = environment.Find(placement.name);
                if (node == null || !placement.snapToTerrain || placement.alignBaseToTerrain) continue;
                if (placement.preserveGroundPivot) GroundEnvironmentPivot(node, terrain, placement.groundOffset);
                else GroundEnvironmentGeometry(node, terrain, placement.groundOffset, vertices, audit);
            }
            foreach (Scatter scatter in layout.scatters)
            {
                Transform group = environment.Find(scatter.name);
                if (group == null) continue;
                foreach (Transform node in group) GroundEnvironmentGeometry(node, terrain, 0, vertices, audit);
            }
            Transform boundaries = environment.Find("PlayBoundaries");
            if (boundaries != null)
                foreach (Transform node in boundaries)
                    if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject) == layout.safety.shoreFencePrefab)
                        GroundEnvironmentGeometry(node, terrain, 0, vertices, audit);
            GroundResourceVisuals(environment.gameObject.scene, terrain, vertices, audit);
            ValidatePropFoundations(layout.district?.props, environment.Find("LandscapeDistrict"), terrain);
            ValidatePropFoundations(world.props, environment.Find("WorldExpansionGeometry"), terrain);
            ValidatePropFoundations(layout.lakeExploration?.props, environment.Find("LakeExploration"), terrain);
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/EnvironmentGroundingRepair.json", JsonUtility.ToJson(audit, true));
        }

        private static void GroundResourceVisuals(Scene scene, Terrain terrain,
            Dictionary<Mesh, Vector3[]> vertices, EnvironmentGroundingAudit audit)
        {
            var quality = JsonUtility.FromJson<QualityLayout>(File.ReadAllText(QualityPath));
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "QualityInteractions") continue;
                foreach (ResourcePlacement resource in quality.resources)
                {
                    Transform node = root.transform.Find(resource.name);
                    if (node == null || node.Find("ResourceVisual") == null) continue;
                    // 외형만 내리면 상호작용 트리거가 공중에 남으므로 소유 오브젝트를 함께 옮긴다.
                    GroundEnvironmentGeometry(node, terrain, 0, vertices, audit);
                }
            }
        }

        private static void RestorePropFoundations(WorldProp[] props, Transform parent, Terrain terrain)
        {
            if (props == null || parent == null) return;
            foreach (WorldProp prop in props)
            {
                Transform node = parent.Find(prop.name);
                if (node == null || !prop.levelFootprint) continue;
                Bounds bounds = GetPropGeometryBounds(node.gameObject);
                // 현재 기단 높이를 유지하므로 반복 실행해도 offset이나 inset이 누적되지 않는다.
                Vector3 ground = new(bounds.center.x, bounds.min.y + prop.foundationInset, bounds.center.z);
                LevelPropFootprint(terrain, ground, bounds.size, prop.foundationBlend);
            }
        }

        private static void GroundExistingProps(WorldProp[] props, Transform parent, Terrain terrain,
            Dictionary<Mesh, Vector3[]> vertices, EnvironmentGroundingAudit audit)
        {
            if (props == null || parent == null) return;
            foreach (WorldProp prop in props)
            {
                Transform node = parent.Find(prop.name);
                if (node == null || prop.levelFootprint) continue;
                Vector3 offset = GroundEnvironmentGeometry(node, terrain, prop.position.y - prop.foundationInset, vertices, audit);
                // BoxCollider가 별도 형제로 생성되는 소품도 시각 메시와 함께 옮긴다.
                Transform collision = parent.Find(prop.name + "_Collision");
                if (prop.collision && !prop.meshCollision && collision != null) collision.position += offset;
            }
        }

        private static Vector3 GroundEnvironmentGeometry(Transform node, Terrain terrain, float offset,
            Dictionary<Mesh, Vector3[]> vertices, EnvironmentGroundingAudit audit = null)
        {
            if (!node.gameObject.activeInHierarchy) return Vector3.zero;
            if (!TryGetEnvironmentSupport(node, vertices, out Vector3 support))
                throw new InvalidOperationException("접지할 실제 메시가 없습니다: " + GetTransformPath(node));
            // 뿌리나 바위가 이미 묻혀 있으면 끌어올리지 않는다. 낮은 꼭짓점 하나를 올리면 경사면 반대쪽이 뜰 수 있다.
            float heightAdjustment = Mathf.Min(0, SampleEnvironmentGroundHeight(support, terrain) + offset - support.y);
            Vector3 adjustment = Vector3.up * heightAdjustment;
            float previousGap = support.y - SampleEnvironmentGroundHeight(support, terrain);
            node.position += adjustment;
            PrefabUtility.RecordPrefabInstancePropertyModifications(node);
            if (audit != null)
            {
                TryGetEnvironmentSupport(node, vertices, out Vector3 result);
                float gap = result.y - SampleEnvironmentGroundHeight(result, terrain);
                float tolerance = terrain.terrainData.size.y / ushort.MaxValue * 2f;
                if (gap - offset > tolerance)
                    throw new InvalidOperationException("실제 메시 접지 검증 실패: " + GetTransformPath(node));
                audit.samples.Add(new EnvironmentGroundingSample
                {
                    path = GetTransformPath(node), support = result, supportGap = gap,
                    expectedOffset = offset, previousSupportGap = previousGap
                });
            }
            return adjustment;
        }

        private static void GroundEnvironmentPivot(Transform node, Terrain terrain, float offset)
        {
            // 절벽처럼 지형에 묻어 쓰는 배치는 메시 끝점이 아닌 저작 기준점을 유지한다.
            Vector3 position = node.position;
            position.y = SampleEnvironmentGroundHeight(position, terrain) + offset;
            node.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(node);
        }

        private static void ValidatePropFoundations(WorldProp[] props, Transform parent, Terrain terrain)
        {
            if (props == null || parent == null) return;
            // Terrain의 정규화 높이 저장 오차까지만 허용한다.
            float tolerance = terrain.terrainData.size.y / ushort.MaxValue * 2f;
            foreach (WorldProp prop in props)
            {
                Transform node = parent.Find(prop.name);
                if (node == null || !prop.levelFootprint) continue;
                Bounds bounds = GetPropGeometryBounds(node.gameObject);
                float expected = bounds.min.y + prop.foundationInset;
                for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                {
                    Vector3 point = new(Mathf.Lerp(bounds.min.x, bounds.max.x, x * 0.5f), 0,
                        Mathf.Lerp(bounds.min.z, bounds.max.z, z * 0.5f));
                    if (Mathf.Abs(SampleEnvironmentGroundHeight(point, terrain) - expected) > tolerance)
                        throw new InvalidOperationException($"건물 기단 접지 실패: {prop.name}, 표본 {point}, 기단 {expected}, 지형 {SampleEnvironmentGroundHeight(point, terrain)}, 경계 {bounds}");
                }
            }
        }

        private static float SampleEnvironmentGroundHeight(Vector3 point, Terrain terrain)
        {
            // GroundPoint에는 액터 배치 여유 높이가 포함되어 있으므로 환경 접지에는 사용하지 않는다.
            return terrain.SampleHeight(point) + terrain.transform.position.y;
        }

        private static void RebuildDistrictProps(LandscapeDistrict district, Terrain terrain, Transform environment)
        {
            Transform previous = environment.Find("LandscapeDistrict");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var parent = new GameObject("LandscapeDistrict").transform;
            parent.SetParent(environment, false);
            foreach (WorldProp prop in district.props) PlaceWorldProp(prop, terrain, parent);
        }

        private static void ClearSettlementVegetation(Layout layout, Transform environment)
        {
            if (layout.district == null) return;
            Transform district = environment.Find("LandscapeDistrict");
            if (district == null) return;
            var buildings = new System.Collections.Generic.List<Bounds>();
            foreach (WorldProp prop in layout.district.props)
            {
                if (!prop.levelFootprint) continue;
                Transform building = district.Find(prop.name);
                if (building == null) continue;
                Bounds bounds = GetPropGeometryBounds(building.gameObject);
                bounds.Expand(new Vector3(prop.foundationBlend, 0, prop.foundationBlend));
                buildings.Add(bounds);
            }
            // 저작된 진행 오브젝트는 유지하고 자동 산포 군집만 정리한다.
            foreach (Scatter scatter in layout.scatters)
            {
                Transform group = environment.Find(scatter.name);
                if (group == null) continue;
                foreach (Transform child in group)
                {
                    if (!child.gameObject.activeSelf) continue;
                    Bounds bounds = GetRendererBounds(child.GetComponentsInChildren<Renderer>());
                    bool overlaps = false;
                    foreach (Bounds building in buildings)
                        if (building.Intersects(bounds)) { overlaps = true; break; }
                    foreach (Clearing clearing in layout.district.vegetationClearings)
                    {
                        Vector2 nearest = new(Mathf.Clamp(clearing.center.x, bounds.min.x, bounds.max.x),
                            Mathf.Clamp(clearing.center.z, bounds.min.z, bounds.max.z));
                        if (Vector2.Distance(nearest, new Vector2(clearing.center.x, clearing.center.z)) < clearing.radius)
                            overlaps = true;
                    }
                    if (!overlaps) continue;
                    child.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                }
            }
        }

        private static NpcBrain RequireDistrictResident(Scene scene, DistrictResident resident)
        {
            Transform node = FindQualityTransform(scene, resident.scenePath);
            NpcBrain brain = node != null ? node.GetComponent<NpcBrain>() : null;
            if (brain == null) throw new InvalidOperationException("마을 주민 배회 컴포넌트 누락: " + resident.scenePath);
            return brain;
        }

        private static void ApplyDistrictResidents(LandscapeDistrict district, Terrain terrain, Scene scene)
        {
            foreach (DistrictResident resident in district.residents)
            {
                NpcBrain brain = RequireDistrictResident(scene, resident);
                brain.transform.SetPositionAndRotation(GroundPoint(resident.position, terrain), Quaternion.Euler(0, resident.yaw, 0));
                var settings = new SerializedObject(brain);
                settings.FindProperty("_enableWander").boolValue = resident.enableWander;
                settings.FindProperty("_patrolRadius").floatValue = resident.patrolRadius;
                settings.FindProperty("_patrolWaitTime").floatValue = resident.patrolWaitTime;
                settings.ApplyModifiedPropertiesWithoutUndo();
                brain.gameObject.SetActive(true);
                EditorUtility.SetDirty(brain);
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(brain.gameObject);
            }
        }

        private static void AlignEnvironmentBase(Placement placement, Terrain terrain, GameObject instance)
        {
            Bounds bounds = GetPropGeometryBounds(instance);
            Vector3 ground = bounds.center;
            ground.y = SampleEnvironmentGroundHeight(ground, terrain);
            LevelPropFootprint(terrain, ground, bounds.size, 0);
            instance.transform.position += Vector3.up * (ground.y + placement.groundOffset - bounds.min.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }

        private static Bounds GetPropGeometryBounds(GameObject node)
        {
            Bounds bounds = default;
            bool hasBounds = false;
            // 생성 직후 Renderer.bounds의 갱신 시점에 의존하면 이전 위치의 경계로 접지 높이를 계산하게 된다.
            foreach (MeshFilter filter in node.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !filter.TryGetComponent(out MeshRenderer renderer) || !renderer.enabled) continue;
                Bounds local = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = filter.transform.TransformPoint(point);
                    if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!hasBounds) throw new InvalidOperationException("배치할 메시가 없습니다: " + node.name);
            return bounds;
        }

        private static void LevelPropFootprint(Terrain terrain, Vector3 ground, Vector3 size, float blend)
        {
            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            int resolution = data.heightmapResolution;
            Vector2 spacing = new(data.size.x / (resolution - 1), data.size.z / (resolution - 1));
            Vector3 half = size * 0.5f;
            // 한 높이맵 셀의 여유가 있어야 보간된 건물 가장자리에도 틈이 생기지 않는다.
            half.x += spacing.x;
            half.z += spacing.y;
            RectInt area = GetDistrictHeightBounds(ground - half, ground + half, blend, origin, spacing, resolution);
            float[,] heights = data.GetHeights(area.x, area.y, area.width, area.height);
            float target = Mathf.Clamp01((ground.y - origin.y) / data.size.y);
            for (int z = 0; z < area.height; z++)
            for (int x = 0; x < area.width; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(origin.x + (area.x + x) * spacing.x - ground.x) - half.x);
                float dz = Mathf.Max(0, Mathf.Abs(origin.z + (area.y + z) * spacing.y - ground.z) - half.z);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float weight = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance / Mathf.Max(blend, 0.001f)));
                heights[z, x] = Mathf.Lerp(heights[z, x], target, weight);
            }
            data.SetHeights(area.x, area.y, heights);
            EditorUtility.SetDirty(data);
            terrain.Flush();
        }
    }
}
