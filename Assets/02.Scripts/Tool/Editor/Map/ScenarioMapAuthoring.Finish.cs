using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>외곽과 깊은 물의 이탈을 막고 해안 난간·지도·NavMesh를 마감한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 경계 마감")]
        public static void FinishScenarioMap()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(layout.scenePath);
            var environment = GameObject.Find("AuthoredEnvironment");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            if (environment.transform.Find("PlayBoundaries") == null)
            {
                var boundaries = new GameObject("PlayBoundaries").transform;
                boundaries.SetParent(environment.transform);
                float height = layout.safety.boundaryHeight;
                float depth = layout.safety.boundaryDepth;
                Vector3 origin = layout.terrainOrigin;
                Vector3 size = layout.terrainSize;
                AddBoundaryBox("WestRidge", boundaries, origin + new Vector3(-depth / 2, height / 2, size.z / 2),
                    new Vector3(depth, height, size.z + depth * 2));
                AddBoundaryBox("EastRidge", boundaries, origin + new Vector3(size.x + depth / 2, height / 2, size.z / 2),
                    new Vector3(depth, height, size.z + depth * 2));
                AddBoundaryBox("SouthRidge", boundaries, origin + new Vector3(size.x / 2, height / 2, -depth / 2),
                    new Vector3(size.x, height, depth));
                AddBoundaryBox("NorthRidge", boundaries, origin + new Vector3(size.x / 2, height / 2, size.z + depth / 2),
                    new Vector3(size.x, height, depth));
                AddLakeBoundary(layout, boundaries);
                AddShoreFences(layout, terrain, boundaries);
                BakeNavigation(layout, environment);
            }
            Physics.SyncTransforms();
            ValidateBoundaries(layout, terrain, environment.transform.Find("PlayBoundaries"));
            if (!EditorSceneManager.SaveScene(terrain.gameObject.scene)) throw new IOException("경계 마감 씬 저장에 실패했습니다.");
            CaptureMapBackground(layout, true);
            CapturePreviews();
            Validate();
            Debug.Log("[ScenarioMap] 외곽·호수 경계 마감 및 지도 촬영 완료");
        }

        private static void AddBoundaryBox(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var boundary = new GameObject(name, typeof(BoxCollider));
            boundary.transform.SetParent(parent);
            boundary.transform.position = center;
            boundary.layer = LayerMask.NameToLayer("Ground");
            boundary.GetComponent<BoxCollider>().size = size;
            var modifier = boundary.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");
        }

        private static void AddLakeBoundary(Layout layout, Transform parent)
        {
            const int segments = 32;
            var vertices = new Vector3[segments * 2];
            var triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                Vector3 point = new(Mathf.Cos(angle) * layout.lake.radius.x * layout.safety.lakeRadiusScale,
                    0, Mathf.Sin(angle) * layout.lake.radius.y * layout.safety.lakeRadiusScale);
                vertices[i] = point;
                vertices[i + segments] = point + Vector3.up * layout.safety.boundaryHeight;
                int next = (i + 1) % segments;
                triangles.AddRange(new[] { i, i + segments, next, next, i + segments, next + segments });
                if (i > 0 && i < segments - 1)
                {
                    triangles.AddRange(new[] { 0, i, i + 1, segments, i + 1 + segments, i + segments });
                }
            }
            string path = layout.assetDirectory + "/LakeBoundary.asset";
            if (File.Exists(path)) throw new InvalidOperationException("호수 경계 에셋이 이미 있습니다. 기존 경계 씬을 복구하세요.");
            var mesh = new Mesh { name = "LakeBoundary", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            var boundary = new GameObject("DeepLake", typeof(MeshCollider));
            boundary.transform.SetParent(parent);
            boundary.transform.position = new Vector3(layout.lake.center.x, layout.terrainOrigin.y, layout.lake.center.z);
            boundary.layer = LayerMask.NameToLayer("Ground");
            var collider = boundary.GetComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            var modifier = boundary.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = NavMesh.GetAreaFromName("Not Walkable");
        }

        private static void AddShoreFences(Layout layout, Terrain terrain, Transform parent)
        {
            SafetySettings safety = layout.safety;
            float span = safety.shoreFenceAngles.y - safety.shoreFenceAngles.x;
            float length = span * Mathf.Deg2Rad * Mathf.Max(layout.lake.radius.x, layout.lake.radius.y) * safety.shoreFenceRadiusScale;
            int count = Mathf.CeilToInt(length / safety.shoreFenceSpacing);
            for (int i = 0; i <= count; i++)
            {
                float angle = Mathf.Lerp(safety.shoreFenceAngles.x, safety.shoreFenceAngles.y, (float)i / count) * Mathf.Deg2Rad;
                Vector3 point = layout.lake.center + new Vector3(Mathf.Cos(angle) * layout.lake.radius.x * safety.shoreFenceRadiusScale,
                    0, Mathf.Sin(angle) * layout.lake.radius.y * safety.shoreFenceRadiusScale);
                Vector3 tangent = new(-Mathf.Sin(angle) * layout.lake.radius.x, 0, Mathf.Cos(angle) * layout.lake.radius.y);
                // 난간 프리팹의 긴 축은 X이므로 접선에서 90도 회전한다.
                float rotation = Quaternion.LookRotation(tangent).eulerAngles.y + 90;
                PlaceEnvironment(new Placement { name = "ShoreFence_" + i.ToString("D3"), prefab = safety.shoreFencePrefab,
                    position = point, rotation = new Vector3(0, rotation, 0), scale = safety.shoreFenceScale,
                    snapToTerrain = true, hasCollision = true }, terrain, parent);
            }
        }

        private static void ValidateBoundaries(Layout layout, Terrain terrain, Transform boundaries)
        {
            if (boundaries == null) throw new InvalidOperationException("맵 경계가 없습니다.");
            Directory.CreateDirectory(ReportDirectory);
            var errors = new List<string>();
            Collider water = boundaries.Find("DeepLake").GetComponent<Collider>();
            const int samples = 96;
            for (int i = 0; i < samples; i++)
            {
                float angle = (float)i / samples * Mathf.PI * 2;
                Vector3 outside = layout.lake.center + new Vector3(Mathf.Cos(angle) * layout.lake.radius.x * 1.2f,
                    0, Mathf.Sin(angle) * layout.lake.radius.y * 1.2f);
                outside.y = terrain.SampleHeight(outside) + terrain.transform.position.y + 1;
                Vector3 inside = new(layout.lake.center.x, outside.y, layout.lake.center.z);
                Vector3 direction = inside - outside;
                if (!water.Raycast(new Ray(outside, direction.normalized), out _, direction.magnitude)) errors.Add("호수 이탈 경계 누락: " + i);
            }
            foreach (string name in new[] { "WestRidge", "EastRidge", "SouthRidge", "NorthRidge" })
                if (boundaries.Find(name)?.GetComponent<BoxCollider>()?.enabled != true) errors.Add("외곽 경계 누락: " + name);
            File.WriteAllText(ReportDirectory + "/Boundaries.txt", $"호수 경계 표본: {samples}\n외곽 경계: 4\n오류: {errors.Count}\n" + string.Join("\n", errors));
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }
    }
}
