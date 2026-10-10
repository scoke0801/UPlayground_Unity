using System;
using System.Collections.Generic;
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
        private const string RelocationDirectory = "output/ScenarioGameplay/P01";
        private const string RelocationRequestPath = RelocationDirectory + "/RelocationRequest.json";

        [Serializable] private sealed class RelocationRequest
        {
            public string scenePath;
            public RelocationMove[] moves = Array.Empty<RelocationMove>();
            public RelocationVector[] vectors = Array.Empty<RelocationVector>();
            public RelocationSource[] sources = Array.Empty<RelocationSource>();
        }

        [Serializable] private sealed class RelocationMove
        {
            public string sceneEntityId;
            public string globalObjectId;
            public string transformPath;
            public string assetGuid;
            public string actorId;
            public string sourcePath;
            public string reason;
            public Vector3 beforePosition;
            public Vector3 beforeRotation;
            public Vector3 position;
            public Vector3 rotation;
        }

        [Serializable] private sealed class RelocationVector
        {
            public string assetGuid;
            public string assetPath;
            public string propertyPath;
            public string reason;
            public Vector3 before;
            public Vector3 after;
        }

        [Serializable] private sealed class RelocationSource
        {
            public string path;
            public string jsonPath;
            public string beforeJson;
            public string afterJson;
            public string reason;
        }

        [Serializable] private sealed class RelocationDiff
        {
            public string scenePath;
            public string requestPath;
            public List<BaselineFile> files = new();
            public List<RelocationMoveDiff> moves = new();
            public RelocationVector[] vectors;
            public RelocationSource[] sources;
            public List<string> references = new();
        }

        [Serializable] private sealed class RelocationMoveDiff
        {
            public string sceneEntityId;
            public string globalObjectId;
            public string assetGuid;
            public string actorId;
            public string path;
            public string parent;
            public string sourcePath;
            public string reason;
            public Vector3 beforePosition;
            public Vector3 beforeRotation;
            public Vector3 position;
            public Vector3 rotation;
        }

        private sealed class ResolvedRelocation
        {
            public RelocationDiff Diff;
            public readonly List<Transform> Transforms = new();
            public readonly List<SerializedObject> Assets = new();
            public readonly Dictionary<string, string> Sources = new();
        }

        /// <summary>정확한 식별자와 현재값을 대조하고 저장 없이 배치 차이를 출력한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 배치 이동 사전 검사")]
        public static void InspectScenarioRelocation() => RunScenarioRelocation(false);

        /// <summary>사전 검사한 배치·고정 좌표·저작 소스를 함께 적용하고 실패 시 복원한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 배치 이동 적용")]
        public static void ApplyScenarioRelocation() => RunScenarioRelocation(true);

        private static void RunScenarioRelocation(bool apply)
        {
            RequireRelocationEditor();
            string requestPath = RelocationRequestPath;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-scenarioRelocation") requestPath = args[i + 1];
            var request = JsonUtility.FromJson<RelocationRequest>(File.ReadAllText(requestPath));
            if (request == null || request.scenePath != ReadLayout().scenePath)
                throw new InvalidOperationException("시나리오 전용 요청이 아닙니다.");
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(request.scenePath);
                ResolvedRelocation resolved = ResolveRelocation(scene, request, requestPath);
                string report = RelocationDirectory + "/Runs/" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(report);
                File.WriteAllText(report + "/Diff.json", JsonUtility.ToJson(resolved.Diff, true));
                if (apply) ApplyRelocation(scene, request, resolved, report);
                Debug.Log("[ScenarioRelocation] " + (apply ? "적용·재로드 확인: " : "사전 검사: ") + report);
            }
            finally
            {
                if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void RequireRelocationEditor()
        {
            RequireEditMode();
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일 완료·오류 복구 후 실행하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 배치 작업을 중단합니다.");
        }

        private static void ApplyRelocation(Scene scene, RelocationRequest request, ResolvedRelocation resolved,
            string report, Action<int> checkpoint = null)
        {
            VerifyBaselineHashes(resolved.Diff.files.ToArray());
            string backup = report + "/backup/";
            foreach (BaselineFile file in resolved.Diff.files)
            {
                string destination = backup + file.path;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file.path, destination);
            }
            var identities = CaptureRelocationIdentities(scene);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("시나리오 배치 이동");
            try
            {
                for (int i = 0; i < request.moves.Length; i++)
                {
                    Transform target = resolved.Transforms[i];
                    Undo.RecordObject(target, "시나리오 배치 이동");
                    target.SetPositionAndRotation(request.moves[i].position, Quaternion.Euler(request.moves[i].rotation));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
                checkpoint?.Invoke(1);
                for (int i = 0; i < request.vectors.Length; i++)
                {
                    SerializedObject serialized = resolved.Assets[i];
                    serialized.Update();
                    serialized.FindProperty(request.vectors[i].propertyPath).vector3Value = request.vectors[i].after;
                    serialized.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(serialized.targetObject);
                }
                foreach (var source in resolved.Sources)
                {
                    File.WriteAllText(source.Key, source.Value);
                    AssetDatabase.ImportAsset(source.Key, ImportAssetOptions.ForceUpdate);
                }
                checkpoint?.Invoke(2);
                AssertBaselineReferences(scene);
                SaveQualityScene(scene, new Layout { scenePath = scene.path });
                checkpoint?.Invoke(3);
                scene = EditorSceneManager.OpenScene(request.scenePath);
                AssertRelocationApplied(scene, request, identities);
                Undo.CollapseUndoOperations(undoGroup);
                File.WriteAllText(report + "/Result.json", "{\"savedAndReloaded\":true,\"idsPreserved\":true}");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (BaselineFile file in resolved.Diff.files) File.Copy(backup + file.path, file.path, true);
                foreach (BaselineFile file in resolved.Diff.files)
                    if (file.path.StartsWith("Assets/", StringComparison.Ordinal) && !file.path.EndsWith(".meta", StringComparison.Ordinal))
                        AssetDatabase.ImportAsset(file.path, ImportAssetOptions.ForceUpdate);
                scene = EditorSceneManager.OpenScene(request.scenePath);
                VerifyBaselineHashes(resolved.Diff.files.ToArray());
                AssertRelocationIdentities(scene, identities);
                File.WriteAllText(report + "/Rollback.json", "{\"restoredFilesAndMetadata\":true,\"idsPreserved\":true}");
                throw;
            }
        }

        private static Dictionary<string, string> CaptureRelocationIdentities(Scene scene)
        {
            var result = new Dictionary<string, string>();
            var ids = new HashSet<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                {
                    if (!identity.HasGuid || !ids.Add(identity.Guid)) throw new InvalidOperationException("씬 ID 누락/중복");
                    result.Add(GlobalObjectId.GetGlobalObjectIdSlow(identity).ToString(), identity.Guid);
                }
            return result;
        }

        private static void AssertRelocationIdentities(Scene scene, Dictionary<string, string> expected)
        {
            var current = CaptureRelocationIdentities(scene);
            if (current.Count != expected.Count) throw new InvalidOperationException("배치 ID 개수가 바뀌었습니다.");
            foreach (var pair in expected)
                if (!current.TryGetValue(pair.Key, out string id) || id != pair.Value)
                    throw new InvalidOperationException("배치 ID가 바뀌었습니다: " + pair.Key);
        }

        private static void AssertRelocationApplied(Scene scene, RelocationRequest request, Dictionary<string, string> identities)
        {
            AssertRelocationIdentities(scene, identities);
            AssertBaselineReferences(scene);
            foreach (RelocationMove move in request.moves)
            {
                Transform target = ResolveRelocationTransform(scene, move);
                if (!IsRelocationPosition(target.position, move.position)
                    || Quaternion.Angle(target.rotation, Quaternion.Euler(move.rotation)) > 0.01f)
                    throw new InvalidOperationException("저장된 이동 결과 불일치: " + move.transformPath);
            }
            foreach (RelocationVector vector in request.vectors)
                if (!IsRelocationPosition(ResolveRelocationVector(vector).FindProperty(vector.propertyPath).vector3Value, vector.after))
                    throw new InvalidOperationException("저장된 고정 좌표 불일치: " + vector.propertyPath);
            foreach (RelocationSource source in request.sources)
            {
                var json = Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(source.path));
                if (!Newtonsoft.Json.Linq.JToken.DeepEquals(json.SelectToken(source.jsonPath, true), Newtonsoft.Json.Linq.JToken.Parse(source.afterJson)))
                    throw new InvalidOperationException("저장된 소스 좌표 불일치: " + source.jsonPath);
            }
        }

        private static bool IsRelocationPosition(Vector3 first, Vector3 second) => (first - second).sqrMagnitude < 0.000001f;
    }
}
