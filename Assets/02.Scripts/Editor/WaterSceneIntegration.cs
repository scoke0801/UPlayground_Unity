using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.World;
using UPlayGround.Gameplay.World;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.EditorTools
{
    /// <summary>기존 수역의 GUID·원본 메시·프로필을 보존하며 파도와 강수 저작 데이터를 연결한다.</summary>
    public static class WaterSceneIntegration
    {
        private struct Triangle { public int A, B, C; public Triangle(int a, int b, int c) { A = a; B = b; C = c; } }

        [UPlaygroundTool("Water/IntegrateLoadedScene")]
        public static void IntegrateLoadedScene()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Edit Mode에서 실행해야 합니다.");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("저장되지 않은 기존 씬은 자동 통합하지 않습니다. 저장 상태를 정리한 뒤 실행해야 합니다.");
            var defaultDynamics = AssetDatabase.LoadAssetAtPath<WaterDynamicsProfileSO>(WaterLabAuthoring.DynamicsPath);
            if (defaultDynamics == null) throw new InvalidOperationException("공통 WaterDynamics 에셋을 먼저 생성해야 합니다.");
            EnvironmentController weather = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                weather = root.GetComponentInChildren<EnvironmentController>(true);
                if (weather != null) break;
            }
            int changed = 0;
            foreach (var region in UnderwaterRegion.ActiveRegions)
            {
                if (region == null || region.gameObject.scene != scene || region.Surface == null) continue;
                region.RefreshBinding();
                var surface = region.Surface;
                var filter = surface.surfaceMesh;
                if (filter == null || filter.sharedMesh == null || region.SurfaceRenderer == null || region.SurfaceRenderer.sharedMaterial == null
                    || region.SurfaceRenderer.sharedMaterial.FindPass("WaterSurfaceInfo") < 0) continue;
                Undo.RecordObjects(new UnityEngine.Object[] { surface, filter, region }, "기존 수역 렌더링 통합");
                surface.queryMesh = surface.queryMesh != null ? surface.queryMesh : filter.sharedMesh;
                string meshId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(surface.queryMesh));
                if (string.IsNullOrEmpty(meshId)) throw new InvalidOperationException("저장되지 않은 원본 수면 메시는 통합하지 않습니다.");
                var dynamics = surface.dynamics != null ? surface.dynamics : defaultDynamics;
                Vector4 first = dynamics.firstWave.GetParameters(), second = dynamics.secondWave.GetParameters();
                float curvature = Mathf.Abs(first.z) * (first.x * first.x + first.y * first.y)
                    + Mathf.Abs(second.z) * (second.x * second.x + second.y * second.y);
                float maximumEdge = curvature > 0f ? Mathf.Sqrt(0.02f * 6f / curvature) * 0.95f : float.PositiveInfinity;
                string geometryId = Hash128.Compute(first.ToString("R") + second.ToString("R") + filter.transform.lossyScale.ToString("R")).ToString();
                string meshPath = WaterLabAuthoring.Folder + "/Visual_" + meshId + "_" + geometryId + ".asset";
                Mesh visual = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (visual == null)
                {
                    visual = Subdivide(surface.queryMesh, filter.transform.localToWorldMatrix, maximumEdge);
                    AssetDatabase.CreateAsset(visual, meshPath);
                }
                filter.sharedMesh = visual;
                surface.dynamics = dynamics;
                surface.RefreshMesh();
                region.BindWeather(weather);
                string exposurePath = WaterLabAuthoring.Folder + "/Rain_" + meshId + ".asset";
                if (region.rainExposure == null)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(exposurePath);
                    if (existing != null) region.rainExposure = existing;
                    else WaterLabAuthoring.BakeExposure(region, exposurePath, 128);
                }
                EditorUtility.SetDirty(surface); EditorUtility.SetDirty(filter); EditorUtility.SetDirty(region);
                changed++;
            }
            if (changed == 0) throw new InvalidOperationException("현재 씬에 통합할 관리 수면이 없습니다.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Mesh Subdivide(Mesh source, Matrix4x4 matrix, float maximumEdge)
        {
            var vertices = new List<Vector3>(source.vertices);
            var uv = new List<Vector2>(source.uv);
            while (uv.Count < vertices.Count) uv.Add(Vector2.zero);
            var pending = new Stack<Triangle>();
            var triangles = new List<int>();
            var midpoints = new Dictionary<ulong, int>();
            int[] sourceTriangles = source.triangles;
            for (int i = 0; i < sourceTriangles.Length; i += 3)
                pending.Push(new Triangle(sourceTriangles[i], sourceTriangles[i + 1], sourceTriangles[i + 2]));
            float maximumSquared = maximumEdge * maximumEdge;
            while (pending.Count > 0)
            {
                Triangle triangle = pending.Pop();
                float ab = matrix.MultiplyVector(vertices[triangle.B] - vertices[triangle.A]).sqrMagnitude;
                float bc = matrix.MultiplyVector(vertices[triangle.C] - vertices[triangle.B]).sqrMagnitude;
                float ca = matrix.MultiplyVector(vertices[triangle.A] - vertices[triangle.C]).sqrMagnitude;
                if (Mathf.Max(ab, Mathf.Max(bc, ca)) <= maximumSquared)
                {
                    triangles.Add(triangle.A); triangles.Add(triangle.B); triangles.Add(triangle.C);
                    continue;
                }
                int a = triangle.A, b = triangle.B, opposite = triangle.C;
                if (bc >= ab && bc >= ca) { a = triangle.B; b = triangle.C; opposite = triangle.A; }
                else if (ca >= ab && ca >= bc) { a = triangle.C; b = triangle.A; opposite = triangle.B; }
                ulong key = ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                if (!midpoints.TryGetValue(key, out int middle))
                {
                    middle = vertices.Count;
                    vertices.Add((vertices[a] + vertices[b]) * 0.5f);
                    uv.Add((uv[a] + uv[b]) * 0.5f);
                    midpoints.Add(key, middle);
                }
                pending.Push(new Triangle(a, middle, opposite));
                pending.Push(new Triangle(middle, b, opposite));
                if (vertices.Count > 500000) throw new InvalidOperationException("시각 수면 세분화 예산 50만 정점 초과. 작은 수역으로 나누어야 합니다.");
            }
            var result = new Mesh { name = source.name + " Water Visual", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            result.SetVertices(vertices); result.SetUVs(0, uv); result.SetTriangles(triangles, 0);
            result.RecalculateNormals(); result.RecalculateBounds();
            return result;
        }
    }
}
