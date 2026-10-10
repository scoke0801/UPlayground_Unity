using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class LakeExploration
        {
            public WorldProp[] props = Array.Empty<WorldProp>();
            public float boundarySampleSpacing = 2f;
            public float shorelineClearance = 0.3f;
            public float entranceClearance = 7f;
            public Vector3[] entrances = Array.Empty<Vector3>();
        }

        /// <summary>호수의 섬·연결길·물가 충돌과 지도 데이터를 저장한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 호수 탐험 구역 배치")]
        public static void ApplyLakeExploration()
        {
            RequireEditMode();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("열린 씬을 저장한 뒤 호수 구역을 적용하세요.");
            Layout layout = ReadLayout();
            if (layout.lakeExploration == null || layout.lakeExploration.boundarySampleSpacing <= 0)
                throw new InvalidOperationException("호수 탐험 배치와 물가 표본 간격이 필요합니다.");
            foreach (WorldProp prop in layout.lakeExploration.props) RequireEnvironmentPrefab(prop.prefab);
            foreach (Placement placement in layout.placements)
                if (placement.applyWithLakeExploration) RequireEnvironmentPrefab(placement.prefab);
            Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
            Transform environment = FindQualityTransform(scene, "AuthoredEnvironment");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            string backup = BackupQualityAssets(layout);
            string[] additionalAssets = { "MapBackground.png" };
            foreach (string name in additionalAssets)
                File.Copy(layout.assetDirectory + "/" + name, backup + "/" + name);
            try
            {
                var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
                var routes = new List<Route>(layout.routes);
                routes.AddRange(world.routes);
                layout.routes = routes.ToArray();
                ReshapeDistrictTerrain(layout, terrain);
                foreach (Placement placement in layout.placements)
                {
                    if (!placement.applyWithLakeExploration) continue;
                    Transform existing = environment.Find(placement.name);
                    if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    GameObject placed = PlaceEnvironment(placement, terrain, environment);
                    ApplyExistingQualityMaterials(layout, placed);
                }
                Transform previous = environment.Find("LakeExploration");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var islands = new GameObject("LakeExploration").transform;
                islands.SetParent(environment, false);
                foreach (WorldProp prop in layout.lakeExploration.props) PlaceWorldProp(prop, terrain, islands);
                ApplyExistingQualityMaterials(layout, islands.gameObject);
                PaintQualityRoutes(layout, terrain);
                RestoreEnvironmentGrounding(layout, terrain, environment);
                RebuildExplorationWaterBoundary(layout, terrain, environment.Find("PlayBoundaries"));
                ClearExplorationEntrances(layout, environment);
                Physics.SyncTransforms();
                BakeNavigation(layout, environment.gameObject);
                Validate();
                SaveQualityScene(scene, layout);
                AssetDatabase.SaveAssets();
                CaptureMapBackground(layout, refresh: true);
                File.WriteAllText(ReportDirectory + "/LakeExploration.txt",
                    "호수 탐험 구역 저장·동선 검증 통과\n배치 소품: " + layout.lakeExploration.props.Length
                    + "\n백업: " + backup + "\n빌드·테스트·Play Mode 실행 안 함");
                Debug.Log("[ScenarioMap] 호수 탐험 구역·물가 경계·이동 데이터·지도 저장 완료");
            }
            catch
            {
                RestoreQualityBackup(layout, backup);
                foreach (string name in additionalAssets)
                {
                    string path = layout.assetDirectory + "/" + name;
                    File.Copy(backup + "/" + name, path, true);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                throw;
            }
        }

        private static void ClearExplorationEntrances(Layout layout, Transform environment)
        {
            Transform boundaries = environment.Find("PlayBoundaries");
            foreach (Transform fence in boundaries)
            {
                if (!fence.name.StartsWith("ShoreFence_", StringComparison.Ordinal)) continue;
                foreach (Vector3 entrance in layout.lakeExploration.entrances)
                {
                    Vector2 offset = new(fence.position.x - entrance.x, fence.position.z - entrance.z);
                    if (offset.magnitude > layout.lakeExploration.entranceClearance) continue;
                    fence.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(fence.gameObject);
                    break;
                }
            }
        }

        private static void RebuildExplorationWaterBoundary(Layout layout, Terrain terrain, Transform boundaries)
        {
            // 호수 전체를 덮는 convex 충돌은 섬도 막는다. 수면과 지형의 교선에만 양면 벽을 둔다.
            MeshCollider collider = boundaries.Find("DeepLake").GetComponent<MeshCollider>();
            Mesh existing = collider.sharedMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float step = layout.lakeExploration.boundarySampleSpacing;
            float level = layout.lake.center.y + layout.lakeExploration.shorelineClearance;
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            int columns = Mathf.CeilToInt(size.x / step);
            int rows = Mathf.CeilToInt(size.z / step);
            Vector3[] lower = SampleShorelineRow(layout, terrain, columns, step, 0, level);
            for (int row = 1; row <= rows; row++)
            {
                Vector3[] upper = SampleShorelineRow(layout, terrain, columns, step, Mathf.Min(row * step, size.z), level);
                for (int column = 0; column < columns; column++)
                {
                    AddShorelineTriangle(lower[column], upper[column], upper[column + 1], level,
                        layout, collider.transform, vertices, triangles);
                    AddShorelineTriangle(lower[column], upper[column + 1], lower[column + 1], level,
                        layout, collider.transform, vertices, triangles);
                }
                lower = upper;
            }
            var mesh = new Mesh { name = existing.name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            collider.sharedMesh = null;
            collider.convex = false;
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            collider.sharedMesh = existing;
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        private static Vector3[] SampleShorelineRow(Layout layout, Terrain terrain,
            int columns, float spacing, float z, float level)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            var points = new Vector3[columns + 1];
            for (int x = 0; x <= columns; x++)
            {
                Vector3 point = origin + new Vector3(Mathf.Min(x * spacing, size.x), 0, z);
                point.y = terrain.SampleHeight(point) + origin.y;
                // 실제 수면의 외곽도 교선에 포함해 호수 밖 저지대에 벽이 생기지 않게 한다.
                float surfaceEdge = level + (LakeDistance(point, layout.lake) - 1f)
                    * Mathf.Min(layout.lake.radius.x, layout.lake.radius.y);
                point.y = Mathf.Max(point.y, surfaceEdge);
                points[x] = point;
            }
            return points;
        }

        private static void AddShorelineTriangle(Vector3 a, Vector3 b, Vector3 c, float level,
            Layout layout, Transform boundary, List<Vector3> vertices, List<int> triangles)
        {
            if ((a.y < level) == (b.y < level) && (b.y < level) == (c.y < level)) return;
            Vector3 first = default;
            Vector3 second = default;
            int count = 0;
            AddShorelineCrossing(a, b, level, ref first, ref second, ref count);
            AddShorelineCrossing(b, c, level, ref first, ref second, ref count);
            AddShorelineCrossing(c, a, level, ref first, ref second, ref count);
            if (count != 2 || (first - second).sqrMagnitude < 0.000001f) return;
            int start = vertices.Count;
            float bottom = layout.terrainOrigin.y;
            float top = bottom + layout.safety.boundaryHeight;
            vertices.Add(boundary.InverseTransformPoint(new Vector3(first.x, bottom, first.z)));
            vertices.Add(boundary.InverseTransformPoint(new Vector3(second.x, bottom, second.z)));
            vertices.Add(boundary.InverseTransformPoint(new Vector3(first.x, top, first.z)));
            vertices.Add(boundary.InverseTransformPoint(new Vector3(second.x, top, second.z)));
            triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3,
                start + 1, start + 2, start, start + 3, start + 2, start + 1 });
        }

        private static void AddShorelineCrossing(Vector3 a, Vector3 b, float level,
            ref Vector3 first, ref Vector3 second, ref int count)
        {
            if ((a.y < level) == (b.y < level)) return;
            Vector3 crossing = Vector3.Lerp(a, b, (level - a.y) / (b.y - a.y));
            if (count == 0) first = crossing;
            else second = crossing;
            count++;
        }
    }
}
