using System;
using System.IO;
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
            Vector3 ground = GroundPoint(bounds.center, terrain);
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
