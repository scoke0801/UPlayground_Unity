using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;
using UPlayGround.FlowGraph;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>실제 시나리오 사본에서 재적용·식별 실패·저장 중 실패 복구와 파일 보존을 검증한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P01 배치 안전 검증")]
        public static void VerifyScenarioRelocation()
        {
            RequireRelocationEditor();
            Layout layout = ReadLayout();
            const string staging = "Assets/01.Scenes/Scenario/P01VerificationStaging.unity";
            string asset = layout.assetDirectory + "/P01VerificationFlow.asset";
            string source = layout.assetDirectory + "/P01VerificationSource.json";
            foreach (string path in new[] { staging, asset, source })
                if (File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("검사 임시 경로가 사용 중입니다: " + path);
            var protectedFiles = new RelocationDiff();
            foreach (string path in new[] { layout.scenePath, SourceScenePath, LayoutPath, QualityPath, WorldPath }) AddRelocationFile(protectedFiles, path);
            foreach (string path in AssetDatabase.GetDependencies(layout.scenePath, true))
                if (File.Exists(path)) AddRelocationFile(protectedFiles, path);
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            string report = RelocationDirectory + "/Verification/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(report);
            var checks = new List<string>();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(layout.scenePath);
                var originalIds = CaptureIdentities(scene);
                if (!EditorSceneManager.SaveScene(scene, staging, true)) throw new IOException("검사 씬 복사 실패");
                scene = EditorSceneManager.OpenScene(staging);
                RestoreIdentities(scene, originalIds);
                var fixture = new GameObject("P01_VerificationAnchor");
                fixture.transform.position = new Vector3(1044, 55, 408);
                var volume = new GameObject("Volume", typeof(BoxCollider));
                volume.transform.SetParent(fixture.transform, false);
                volume.GetComponent<BoxCollider>().isTrigger = true;
                fixture.AddComponent<SceneEntityId>();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("검사 씬 저장 실패");
                scene = EditorSceneManager.OpenScene(staging);
                fixture = FindQualityTransform(scene, "P01_VerificationAnchor").gameObject;
                var graph = ScriptableObject.CreateInstance<FlowGraphSO>();
                graph.graphId = "P01_VerificationOnly";
                graph.nodes.Add(new SpawnStoryActorNode { id = "P01_Node", position = fixture.transform.position });
                AssetDatabase.CreateAsset(graph, asset);
                AssetDatabase.SaveAssetIfDirty(graph);
                File.WriteAllText(source, "{\"position\":{\"x\":1044.0,\"y\":55.0,\"z\":408.0},\"keep\":\"사용자 값 보존\"}");
                AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport);
                var request = new RelocationRequest
                {
                    scenePath = staging,
                    moves = new[] { new RelocationMove
                    {
                        sceneEntityId = fixture.GetComponent<SceneEntityId>().Guid,
                        globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(fixture).ToString(),
                        transformPath = fixture.name, sourcePath = source, reason = "P01 사본의 앵커·볼륨 동반 이동 검증",
                        beforePosition = fixture.transform.position, position = fixture.transform.position + Vector3.right * 3,
                        rotation = new Vector3(0, 35, 0)
                    } },
                    vectors = new[] { new RelocationVector
                    {
                        assetGuid = AssetDatabase.AssetPathToGUID(asset), assetPath = asset,
                        propertyPath = "nodes.Array.data[0].position", before = fixture.transform.position,
                        after = fixture.transform.position + Vector3.right * 3, reason = "생성 노드 고정 좌표 동반 이동 검증"
                    } },
                    sources = new[] { new RelocationSource
                    {
                        path = source, jsonPath = "position", beforeJson = "{\"x\":1044.0,\"y\":55.0,\"z\":408.0}",
                        afterJson = "{\"x\":1047.0,\"y\":55.0,\"z\":408.0}", reason = "배치 권위 소스 동반 이동 검증"
                    } }
                };
                File.WriteAllText(report + "/Request.json", JsonUtility.ToJson(request, true));
                var baselineIds = CaptureRelocationIdentities(scene);
                int count = CountRelocationObjects(scene);
                CaptureRelocationView(report + "/Before.png");
                for (int failure = 1; failure <= 3; failure++)
                {
                    var resolved = ResolveRelocation(scene, request, report + "/Request.json", source);
                    string run = report + "/Failure" + failure;
                    Directory.CreateDirectory(run);
                    File.WriteAllText(run + "/Diff.json", JsonUtility.ToJson(resolved.Diff, true));
                    bool rejected = false;
                    try { ApplyRelocation(scene, request, resolved, run, checkpoint =>
                        { if (checkpoint == failure) throw new IOException("P01 의도한 중간 실패"); }); }
                    catch (IOException error) when (error.Message == "P01 의도한 중간 실패") { rejected = true; }
                    if (!rejected) throw new InvalidOperationException("중간 실패가 발생하지 않았습니다.");
                    VerifyBaselineHashes(resolved.Diff.files.ToArray());
                    scene = EditorSceneManager.OpenScene(staging);
                    AssertRelocationIdentities(scene, baselineIds);
                    checks.Add("rollback checkpoint " + failure + ": files/meta/IDs preserved");
                }
                for (int pass = 1; pass <= 2; pass++)
                {
                    var resolved = ResolveRelocation(scene, request, report + "/Request.json", source);
                    string run = report + "/Pass" + pass;
                    Directory.CreateDirectory(run);
                    File.WriteAllText(run + "/Diff.json", JsonUtility.ToJson(resolved.Diff, true));
                    ApplyRelocation(scene, request, resolved, run);
                    scene = EditorSceneManager.OpenScene(staging);
                    AssertRelocationApplied(scene, request, baselineIds);
                    if (CountRelocationObjects(scene) != count) throw new InvalidOperationException("중복 개체 생성");
                    Transform child = FindQualityTransform(scene, "P01_VerificationAnchor/Volume");
                    if (!IsRelocationPosition(child.position, request.moves[0].position)) throw new InvalidOperationException("볼륨 이동 실패");
                    checks.Add("apply pass " + pass + ": saved/reloaded, object count and IDs stable");
                }
                CaptureRelocationView(report + "/After.png");
                string expectedId = request.moves[0].sceneEntityId;
                request.moves[0].sceneEntityId = "invalid-exact-id";
                ExpectRelocationRejection(() => ResolveRelocation(scene, request, "test", source), checks, "missing ID does not fall back to valid GlobalObjectId/path");
                request.moves[0].sceneEntityId = expectedId;
                string expectedGuid = request.vectors[0].assetGuid;
                request.vectors[0].assetGuid = "00000000000000000000000000000000";
                ExpectRelocationRejection(() => ResolveRelocation(scene, request, "test", source), checks, "wrong asset GUID rejected");
                request.vectors[0].assetGuid = expectedGuid;
                request.moves[0].position += Vector3.one * 9;
                ExpectRelocationRejection(() => ResolveRelocation(scene, request, "test", source), checks, "stale coordinates rejected");
                RequireEnvironmentPrefab(layout.assetDirectory + "/LakeSouthBank.prefab", true);
                ExpectRelocationRejection(() => RequireEnvironmentPrefab("Assets/03.Prefabs/Characters/Test.prefab"), checks, "non-environment path rejected");
                ExpectRelocationRejection(() => RequireEnvironmentPrefab("Assets/03.Prefabs/Environment/Scenario/../Other.prefab"), checks, "path traversal rejected");
                checks.Add("existing custom LakeSouthBank prefab: renderer/collision valid");
                if (Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(source))["keep"].Value<string>() != "사용자 값 보존")
                    throw new InvalidOperationException("소스의 무관한 필드가 바뀌었습니다.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (string path in new[] { staging, asset, source })
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
                if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
                VerifyBaselineHashes(protectedFiles.files.ToArray());
                File.WriteAllText(report + "/ProtectedFiles.json", JsonUtility.ToJson(protectedFiles, true));
            }
            checks.Add("scenario/original/dependencies/source files and metadata: byte hashes preserved");
            File.WriteAllLines(report + "/Checks.txt", checks);
            File.WriteAllText(report + "/Result.json", "{\"passed\":true,\"applyPasses\":2,\"rollbackCheckpoints\":3}");
            Debug.Log("[ScenarioRelocation] P01 검증 통과: " + report);
        }

        private static void ExpectRelocationRejection(Action action, List<string> checks, string label)
        {
            try { action(); }
            catch (InvalidOperationException) { checks.Add(label); return; }
            throw new Exception("거부 검증 실패: " + label);
        }

        private static int CountRelocationObjects(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects()) count += root.GetComponentsInChildren<Transform>(true).Length;
            return count;
        }

        private static void CaptureRelocationView(string path)
        {
            var node = new GameObject("P01_VerificationCamera", typeof(Camera));
            var camera = node.GetComponent<Camera>();
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(new Vector3(1050, 175, 380), Quaternion.Euler(55, -20, 0));
            camera.farClipPlane = 1500;
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
