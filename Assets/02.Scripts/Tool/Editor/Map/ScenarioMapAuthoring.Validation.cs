using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string PreviewDirectory = "ArtSource/ScenarioMaps/LakeOfLife";

        [Serializable] private sealed class ValidationReport
        {
            public string scene;
            public string unityVersion;
            public int environmentPrefabs;
            public int renderers;
            public int missingScripts;
            public int routeSamples;
            public int connectedRoutes;
            public List<string> errors = new();
        }

        /// <summary>새 맵의 동선 연결·보행 충돌·누락 스크립트·환경 출처를 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 검증")]
        public static void Validate()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != layout.scenePath) throw new InvalidOperationException("시나리오 맵을 먼저 여세요.");
            var report = new ValidationReport { scene = scene.path, unityVersion = Application.unityVersion };
            Terrain terrain = null;
            var identities = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    report.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                    if (!identity.HasGuid || !identities.Add(identity.Guid)) report.errors.Add("저장 식별자 누락/중복: " + GetTransformPath(identity.transform));
                if (root.name != "AuthoredEnvironment") continue;
                terrain = root.GetComponentInChildren<Terrain>();
                foreach (Transform child in root.GetComponentsInChildren<Transform>())
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    {
                        string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                        if (!source.StartsWith("Assets/ExternalAssets/Environment/", StringComparison.Ordinal))
                            report.errors.Add("환경 출처가 잘못되었습니다: " + source);
                        report.environmentPrefabs++;
                    }
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                {
                    report.renderers++;
                    foreach (Material material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                            report.errors.Add("렌더링 참조 누락: " + GetTransformPath(renderer.transform));
                }
            }
            if (report.missingScripts > 0) report.errors.Add("Missing Script " + report.missingScripts + "개");
            if (terrain == null) throw new InvalidOperationException("새로 저작한 Terrain이 없습니다.");
            Physics.SyncTransforms();
            foreach (Route route in layout.routes) ValidateRoute(route, terrain, report);
            foreach (Clearing clearing in layout.clearings)
                if (!NavMesh.SamplePosition(GroundPoint(clearing.center, terrain), out _, 2f, NavMesh.AllAreas)) report.errors.Add("전투/탐색 공간 NavMesh 누락: " + clearing.name);
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/Validation.json", JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new InvalidOperationException("시나리오 맵 검증 실패: " + string.Join("\n", report.errors));
            Debug.Log($"[ScenarioMap] 검증 통과: 환경 인스턴스 {report.environmentPrefabs}, 동선 표본 {report.routeSamples}, 연결 {report.connectedRoutes}, Missing Script 0");
        }

        private static void ValidateRoute(Route route, Terrain terrain, ValidationReport report)
        {
            var path = new NavMeshPath();
            for (int i = 1; i < route.points.Length; i++)
            {
                Vector3 first = GroundPoint(route.points[i - 1], terrain);
                Vector3 second = GroundPoint(route.points[i], terrain);
                if (!NavMesh.CalculatePath(first, second, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    report.errors.Add("동선 단절: " + route.name + "/" + (i - 1) + "→" + i);
                else report.connectedRoutes++;
                int count = Mathf.CeilToInt(Vector3.Distance(first, second) / 2f);
                for (int sample = 0; sample <= count; sample++)
                {
                    Vector3 point = GroundPoint(Vector3.Lerp(first, second, (float)sample / Mathf.Max(count, 1)), terrain);
                    report.routeSamples++;
                    if (!NavMesh.SamplePosition(point, out _, 1.5f, NavMesh.AllAreas))
                        report.errors.Add("보행 NavMesh 누락: " + route.name + " " + point);
                    Collider[] obstacles = Physics.OverlapCapsule(point + Vector3.up * 0.4f, point + Vector3.up * 1.5f, 0.35f,
                        LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore);
                    foreach (Collider obstacle in obstacles)
                        if (obstacle is not TerrainCollider)
                            report.errors.Add("주 동선의 보행 충돌: " + route.name + " " + point + " / " + GetTransformPath(obstacle.transform));
                }
            }
        }

        private static Vector3 GroundPoint(Vector3 point, Terrain terrain)
        {
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y + 0.08f;
            return point;
        }

        /// <summary>맵 전체와 주요 플레이 시점의 실제 Unity 렌더를 저장한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 맵 미리보기 저장")]
        public static void CapturePreviews()
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (SceneManager.GetActiveScene().path != layout.scenePath)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty)
                        throw new InvalidOperationException("열린 씬을 저장한 뒤 미리보기를 촬영하세요.");
                EditorSceneManager.OpenScene(layout.scenePath);
            }
            CapturePreviewImages(true);
        }

        private static void CapturePreviewImages(bool shouldSaveScene, View[] additionalViews = null)
        {
            RequireEditMode();
            Layout layout = ReadLayout();
            if (SceneManager.GetActiveScene().path != layout.scenePath) throw new InvalidOperationException("시나리오 맵을 먼저 여세요.");
            Directory.CreateDirectory(PreviewDirectory);
            var views = new List<View>(layout.views);
            if (additionalViews != null) views.AddRange(additionalViews);
            foreach (View view in views)
            {
                var cameraObject = new GameObject("AuthoringPreviewCamera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = view.fieldOfView;
                camera.nearClipPlane = 0.2f;
                camera.farClipPlane = 1800f;
                camera.transform.position = view.position;
                camera.transform.LookAt(view.target);
                camera.clearFlags = CameraClearFlags.Skybox;
                RenderTexture target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
                RenderTexture previous = RenderTexture.active;
                var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(PreviewDirectory + "/" + view.name + ".png", texture.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(target);
                    UnityEngine.Object.DestroyImmediate(texture);
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }
            // 촬영 카메라는 결과 씬에 남기지 않는다.
            if (shouldSaveScene) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}
