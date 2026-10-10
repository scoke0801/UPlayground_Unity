using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class LandscapeDistrict
        {
            public WorldProp[] props = Array.Empty<WorldProp>();
            public DistrictResident[] residents = Array.Empty<DistrictResident>();
            public Clearing[] terrainPads = Array.Empty<Clearing>();
            public Clearing[] vegetationClearings = Array.Empty<Clearing>();
            public string[] removedPlacements = Array.Empty<string>();
            public Vector3[] destinations = Array.Empty<Vector3>();
            public string[] npcPaths = Array.Empty<string>();
            public float npcApproachRadius = 2f;
            public Route[] walkChecks = Array.Empty<Route>();
        }

        [Serializable] private sealed class DistrictReport
        {
            public int props;
            public int connectedDestinations;
            public int accessibleNpcs;
            public int continuedPathQueries;
            public List<string> errors = new();
        }

        private static void ApplyLandscapeDistrict(Layout layout, Terrain terrain, Transform environment)
        {
            LandscapeDistrict district = layout.district;
            if (district == null) return;
            foreach (WorldProp prop in district.props) RequireEnvironmentPrefab(prop.prefab);
            ReshapeDistrictTerrain(layout, terrain);
            Transform previous = environment.Find("LandscapeDistrict");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = new GameObject("LandscapeDistrict").transform;
            root.SetParent(environment);
            foreach (string name in district.removedPlacements)
            {
                Transform placement = environment.Find(name);
                if (placement != null) UnityEngine.Object.DestroyImmediate(placement.gameObject);
            }
            foreach (WorldProp prop in district.props) PlaceWorldProp(prop, terrain, root);
            ApplyDistrictResidents(district, terrain, environment.gameObject.scene);
        }

        private static void ReshapeDistrictTerrain(Layout layout, Terrain terrain)
        {
            TerrainData data = terrain.terrainData;
            int resolution = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            var weights = new float[resolution, resolution];
            var targets = new float[resolution, resolution];
            var distances = new float[resolution, resolution];
            Vector3 origin = terrain.transform.position;
            Vector2 spacing = new(data.size.x / (resolution - 1), data.size.z / (resolution - 1));
            foreach (Route route in layout.routes)
            {
                if (!route.reshapeTerrain) continue;
                for (int segment = 1; segment < route.points.Length; segment++)
                {
                    Vector3 first = route.points[segment - 1];
                    Vector3 second = route.points[segment];
                    Vector2 delta = new(second.x - first.x, second.z - first.z);
                    RectInt bounds = GetDistrictHeightBounds(Vector3.Min(first, second), Vector3.Max(first, second),
                        route.width + route.terrainBlend, origin, spacing, resolution);
                    for (int z = bounds.yMin; z < bounds.yMax; z++)
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        Vector2 point = new(origin.x + x * spacing.x, origin.z + z * spacing.y);
                        float fraction = Mathf.Clamp01(Vector2.Dot(point - new Vector2(first.x, first.z), delta)
                            / Mathf.Max(delta.sqrMagnitude, 0.001f));
                        Vector3 closest = Vector3.Lerp(first, second, fraction);
                        float distance = Vector2.Distance(point, new Vector2(closest.x, closest.z));
                        float weight = 1f - SmoothRange(route.width + 1f, route.width + route.terrainBlend, distance);
                        // 겹치는 구간의 평탄부에서는 가까운 중심선을 택해야 구간 끝에 계단이 생기지 않는다.
                        if (weight <= 0 || weight < weights[z, x]
                            || (weight == weights[z, x] && distance >= distances[z, x])) continue;
                        weights[z, x] = weight;
                        targets[z, x] = closest.y;
                        distances[z, x] = distance;
                    }
                }
            }
            foreach (Clearing pad in layout.district.terrainPads)
            {
                RectInt bounds = GetDistrictHeightBounds(pad.center, pad.center, pad.radius + pad.blend, origin, spacing, resolution);
                for (int z = bounds.yMin; z < bounds.yMax; z++)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    float distance = Vector2.Distance(new Vector2(origin.x + x * spacing.x, origin.z + z * spacing.y),
                        new Vector2(pad.center.x, pad.center.z));
                    float weight = 1f - SmoothRange(pad.radius, pad.radius + pad.blend, distance);
                    if (weight <= weights[z, x]) continue;
                    weights[z, x] = weight;
                    targets[z, x] = pad.center.y;
                }
            }
            ProtectDistrictActorGround(layout, terrain.gameObject.scene, weights, origin, spacing, resolution);
            ProtectDistrictExistingRoutes(layout, weights, origin, spacing, resolution);
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
                heights[z, x] = Mathf.Lerp(heights[z, x], Mathf.Clamp01((targets[z, x] - origin.y) / data.size.y), weights[z, x]);
            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);
            terrain.Flush();
        }

        private static void ProtectDistrictActorGround(Layout layout, Scene scene, float[,] weights,
            Vector3 origin, Vector2 spacing, int resolution)
        {
            var positions = new HashSet<Vector3>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true)) positions.Add(identity.transform.position);
                foreach (GameActor actor in root.GetComponentsInChildren<GameActor>(true)) positions.Add(actor.transform.position);
            }
            // 진행 오브젝트를 이동시키지 않으므로 그 발밑 지형도 함께 보존한다.
            foreach (Vector3 position in positions)
            {
                RectInt bounds = GetDistrictHeightBounds(position, position, layout.shape.floorBlend, origin, spacing, resolution);
                for (int z = bounds.yMin; z < bounds.yMax; z++)
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    float distance = Vector2.Distance(new Vector2(origin.x + x * spacing.x, origin.z + z * spacing.y),
                        new Vector2(position.x, position.z));
                    weights[z, x] = Mathf.Min(weights[z, x], SmoothRange(layout.shape.floorRadius, layout.shape.floorBlend, distance));
                }
            }
        }

        private static void ProtectDistrictExistingRoutes(Layout layout, float[,] weights,
            Vector3 origin, Vector2 spacing, int resolution)
        {
            foreach (Route route in layout.routes)
            {
                if (route.reshapeTerrain) continue;
                for (int segment = 1; segment < route.points.Length; segment++)
                {
                    Vector3 first = route.points[segment - 1];
                    Vector3 second = route.points[segment];
                    Vector2 delta = new(second.x - first.x, second.z - first.z);
                    float outer = route.width + Mathf.Max(2f, route.terrainBlend);
                    RectInt bounds = GetDistrictHeightBounds(Vector3.Min(first, second), Vector3.Max(first, second),
                        outer, origin, spacing, resolution);
                    for (int z = bounds.yMin; z < bounds.yMax; z++)
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        Vector2 offset = new(origin.x + x * spacing.x - first.x, origin.z + z * spacing.y - first.z);
                        float fraction = Mathf.Clamp01(Vector2.Dot(offset, delta) / Mathf.Max(delta.sqrMagnitude, 0.001f));
                        float distance = (offset - delta * fraction).magnitude;
                        weights[z, x] = Mathf.Min(weights[z, x], SmoothRange(route.width + 1f, outer, distance));
                    }
                }
            }
        }

        private static RectInt GetDistrictHeightBounds(Vector3 minimum, Vector3 maximum, float margin,
            Vector3 origin, Vector2 spacing, int resolution)
        {
            int left = Mathf.Clamp(Mathf.FloorToInt((minimum.x - margin - origin.x) / spacing.x), 0, resolution);
            int bottom = Mathf.Clamp(Mathf.FloorToInt((minimum.z - margin - origin.z) / spacing.y), 0, resolution);
            int right = Mathf.Clamp(Mathf.CeilToInt((maximum.x + margin - origin.x) / spacing.x) + 1, 0, resolution);
            int top = Mathf.Clamp(Mathf.CeilToInt((maximum.z + margin - origin.z) / spacing.y) + 1, 0, resolution);
            return new RectInt(left, bottom, right - left, top - bottom);
        }

        private static void ValidateLandscapeDistrict(Layout layout, Terrain terrain, Scene scene)
        {
            LandscapeDistrict district = layout.district;
            if (district == null) return;
            var report = new DistrictReport { props = district.props.Length };
            var path = new NavMeshPath();
            Vector3 start = GroundPoint(layout.routes[0].points[0], terrain);
            if (NavMesh.SamplePosition(start, out NavMeshHit startHit, 1.5f, NavMesh.AllAreas)) start = startHit.position;
            foreach (Vector3 destination in district.destinations)
            {
                Vector3 end = GroundPoint(destination, terrain);
                if (NavMesh.SamplePosition(end, out NavMeshHit endHit, 1.5f, NavMesh.AllAreas)) end = endHit.position;
                if (!CanTraverseDistrict(start, end, path, out int continuations))
                    report.errors.Add("시작점에서 구역 접근 불가: " + destination + " / " + path.status
                        + " / 목표 " + end + " / 마지막 " + (path.corners.Length > 0 ? path.corners[path.corners.Length - 1].ToString() : "없음"));
                else report.connectedDestinations++;
                report.continuedPathQueries += continuations;
            }
            foreach (string npcPath in district.npcPaths)
            {
                Transform npc = FindQualityTransform(scene, npcPath);
                if (!npc.gameObject.activeInHierarchy)
                {
                    report.errors.Add("마을 NPC 비활성: " + npcPath);
                    continue;
                }
                if (!NavMesh.SamplePosition(npc.position, out NavMeshHit hit, district.npcApproachRadius, NavMesh.AllAreas)
                    || !NavMesh.CalculatePath(start, hit.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                    report.errors.Add("마을 NPC 접근 불가: " + npcPath);
                else report.accessibleNpcs++;
            }
            File.WriteAllText(ReportDirectory + "/District.json", JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new InvalidOperationException(string.Join("\n", report.errors));
        }

        private static bool CanTraverseDistrict(Vector3 start, Vector3 end, NavMeshPath path, out int continuations)
        {
            continuations = 0;
            // 넓고 조밀한 NavMesh의 장거리 쿼리는 부분 경로로 끝날 수 있어 마지막 도달점부터 연결을 확인한다.
            // 실제 단절이면 같은 끝점에서 더 전진하지 못하므로 성공으로 취급하지 않는다.
            var visited = new List<Vector3> { start };
            for (int query = 0; query < 16; query++)
            {
                if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path)) return false;
                if (path.status == NavMeshPathStatus.PathComplete) return true;
                Vector3[] corners = path.corners;
                if (path.status != NavMeshPathStatus.PathPartial || corners.Length < 2) return false;
                Vector3 next = corners[corners.Length - 1];
                foreach (Vector3 previous in visited)
                    if (Vector3.Distance(previous, next) < 1f) return false;
                visited.Add(next);
                start = next;
                continuations++;
            }
            return false;
        }
    }
}
