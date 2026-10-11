using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private static ResolvedRelocation ResolveRelocation(Scene scene, RelocationRequest request, string requestPath,
            string verificationSource = null)
        {
            if (request.moves == null || request.vectors == null || request.sources == null)
                throw new InvalidOperationException("이동·좌표·소스 배열이 필요합니다.");
            AssertBaselineReferences(scene);
            CaptureRelocationIdentities(scene);
            var result = new ResolvedRelocation { Diff = new RelocationDiff
            {
                scenePath = scene.path, requestPath = requestPath, vectors = request.vectors, sources = request.sources
            } };
            AddRelocationFile(result.Diff, scene.path);
            foreach (RelocationMove move in request.moves)
            {
                RequireRelocationReason(move.reason);
                RequireFiniteRelocation(move.position);
                RequireFiniteRelocation(move.rotation);
                Transform target = ResolveRelocationTransform(scene, move);
                foreach (Transform other in result.Transforms)
                    if (target == other || target.IsChildOf(other) || other.IsChildOf(target))
                        throw new InvalidOperationException("부모·자식 또는 중복 이동을 한 요청에 섞을 수 없습니다.");
                bool isBefore = IsRelocationPosition(target.position, move.beforePosition)
                    && Quaternion.Angle(target.rotation, Quaternion.Euler(move.beforeRotation)) < 0.01f;
                bool isAfter = IsRelocationPosition(target.position, move.position)
                    && Quaternion.Angle(target.rotation, Quaternion.Euler(move.rotation)) < 0.01f;
                if (!isBefore && !isAfter) throw new InvalidOperationException("이동 전 좌표가 달라졌습니다: " + move.transformPath);
                if (string.IsNullOrEmpty(move.sourcePath) || !File.Exists(move.sourcePath))
                    throw new InvalidOperationException("배치 권위 소스를 명시하세요: " + move.transformPath);
                result.Transforms.Add(target);
                result.Diff.moves.Add(new RelocationMoveDiff
                {
                    sceneEntityId = target.GetComponent<SceneEntityId>()?.Guid,
                    globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target.gameObject).ToString(),
                    assetGuid = AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target.gameObject)),
                    actorId = target.GetComponent<GameActor>()?.ActorId,
                    path = GetTransformPath(target), parent = target.parent == null ? "" : GetTransformPath(target.parent),
                    beforePosition = target.position, beforeRotation = target.eulerAngles,
                    position = move.position, rotation = move.rotation, reason = move.reason, sourcePath = move.sourcePath
                });
            }
            var properties = new HashSet<string>();
            foreach (RelocationVector vector in request.vectors)
            {
                RequireRelocationReason(vector.reason);
                RequireFiniteRelocation(vector.after);
                if (!properties.Add(vector.assetGuid + "/" + vector.propertyPath))
                    throw new InvalidOperationException("고정 좌표가 중복 지정됐습니다.");
                SerializedObject serialized = ResolveRelocationVector(vector);
                if (EditorUtility.IsDirty(serialized.targetObject)) throw new InvalidOperationException("미저장 에셋: " + vector.assetPath);
                Vector3 current = serialized.FindProperty(vector.propertyPath).vector3Value;
                if (!IsRelocationPosition(current, vector.before) && !IsRelocationPosition(current, vector.after))
                    throw new InvalidOperationException("고정 좌표가 변경됐습니다: " + vector.propertyPath);
                result.Assets.Add(serialized);
                AddRelocationFile(result.Diff, vector.assetPath);
            }
            ResolveRelocationSources(request, result, verificationSource);
            ValidateRelocationOwnership(request, result);
            CollectRelocationReferences(scene, result);
            return result;
        }

        private static Transform ResolveRelocationTransform(Scene scene, RelocationMove move)
        {
            Transform result = null;
            if (!string.IsNullOrEmpty(move.sceneEntityId))
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                        if (identity.Guid == move.sceneEntityId)
                        {
                            if (result != null) throw new InvalidOperationException("중복 SceneEntity ID: " + move.sceneEntityId);
                            result = identity.transform;
                        }
                if (result == null) throw new InvalidOperationException("SceneEntity ID를 찾지 못했습니다: " + move.sceneEntityId);
            }
            if (!string.IsNullOrEmpty(move.globalObjectId))
            {
                if (!GlobalObjectId.TryParse(move.globalObjectId, out GlobalObjectId id))
                    throw new InvalidOperationException("GlobalObjectId 형식 오류");
                var target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject;
                if (target == null || target.scene != scene || (result != null && result != target.transform))
                    throw new InvalidOperationException("GlobalObjectId 대상 불일치");
                result = target.transform;
            }
            // ActorId는 타입이고 path는 조회 보조다. 둘 다 인스턴스 식별자를 대신하지 않는다.
            if (result == null || string.IsNullOrEmpty(move.transformPath) || GetTransformPath(result) != move.transformPath)
                throw new InvalidOperationException("배치 ID 또는 정확한 Transform path가 필요합니다.");
            if (!string.IsNullOrEmpty(move.assetGuid)
                && AssetDatabase.AssetPathToGUID(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(result.gameObject)) != move.assetGuid)
                throw new InvalidOperationException("프리팹 Asset GUID가 다릅니다.");
            if (!string.IsNullOrEmpty(move.actorId) && result.GetComponent<GameActor>()?.ActorId != move.actorId)
                throw new InvalidOperationException("Actor ID가 다릅니다.");
            return result;
        }

        private static SerializedObject ResolveRelocationVector(RelocationVector vector)
        {
            string root = ReadLayout().assetDirectory + "/";
            if (string.IsNullOrEmpty(vector.assetGuid) || string.IsNullOrEmpty(vector.assetPath)
                || vector.assetPath.Contains("..") || vector.assetPath.Contains('\\')
                || !vector.assetPath.StartsWith(root, StringComparison.Ordinal)
                || AssetDatabase.GUIDToAssetPath(vector.assetGuid) != vector.assetPath)
                throw new InvalidOperationException("시나리오 소유 Asset GUID/path가 아닙니다.");
            foreach (string dependency in AssetDatabase.GetDependencies(SourceScenePath, true))
                if (dependency == vector.assetPath) throw new InvalidOperationException("원본 맵 공유 에셋은 이동할 수 없습니다.");
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(vector.assetPath);
            if (asset == null || UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(asset))
                throw new InvalidOperationException("고정 좌표 에셋 또는 직렬화 타입 누락");
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(vector.propertyPath);
            if (property == null || property.propertyType != SerializedPropertyType.Vector3)
                throw new InvalidOperationException("정확한 Vector3 propertyPath가 필요합니다: " + vector.propertyPath);
            return serialized;
        }

        private static void ResolveRelocationSources(RelocationRequest request, ResolvedRelocation result, string verificationSource)
        {
            var paths = new HashSet<string>();
            foreach (RelocationSource source in request.sources)
            {
                RequireRelocationReason(source.reason);
                if (source.path != LayoutPath && source.path != QualityPath && source.path != WorldPath && source.path != verificationSource)
                    throw new InvalidOperationException("layout/quality/world 외의 소스 변경은 허용하지 않습니다.");
                if (!paths.Add(source.path + "/" + source.jsonPath)) throw new InvalidOperationException("소스 프로퍼티 중복");
                string text = result.Sources.TryGetValue(source.path, out string staged) ? staged : File.ReadAllText(source.path);
                JToken json = JToken.Parse(text);
                var matches = new List<JToken>(json.SelectTokens(source.jsonPath, true));
                if (matches.Count != 1) throw new InvalidOperationException("소스 프로퍼티를 하나만 지정하세요.");
                JToken before = JToken.Parse(source.beforeJson);
                JToken after = JToken.Parse(source.afterJson);
                RequireRelocationCoordinate(before);
                RequireRelocationCoordinate(after);
                if ((before is JObject) != (after is JObject))
                    throw new InvalidOperationException("좌표 형식을 숫자와 Vector3 사이에서 바꿀 수 없습니다.");
                JToken current = matches[0];
                if (!JToken.DeepEquals(current, before) && !JToken.DeepEquals(current, after))
                    throw new InvalidOperationException("소스 좌표가 변경됐습니다: " + source.jsonPath);
                if (!JToken.DeepEquals(current, after))
                {
                    current.Replace(after);
                    result.Sources[source.path] = json.ToString(Newtonsoft.Json.Formatting.Indented) + "\n";
                }
                AddRelocationFile(result.Diff, source.path);
            }
            foreach (RelocationMove move in request.moves)
                if (move.sourcePath != request.scenePath && !result.Diff.files.Exists(file => file.path == move.sourcePath))
                    throw new InvalidOperationException("씬 밖 권위 소스의 좌표 변경을 함께 지정하세요: " + move.sourcePath);
        }

        private static void RequireRelocationCoordinate(JToken value)
        {
            if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
            {
                double number = value.Value<double>();
                if (!double.IsNaN(number) && !double.IsInfinity(number)) return;
            }
            if (value is JObject vector && vector.Count == 3 && vector["x"] != null && vector["y"] != null && vector["z"] != null)
            {
                RequireFiniteRelocation(vector.ToObject<Vector3>());
                return;
            }
            throw new InvalidOperationException("소스 변경은 유한한 좌표/회전 수치만 허용합니다.");
        }

        private static void ValidateRelocationOwnership(RelocationRequest request, ResolvedRelocation result)
        {
            foreach (string path in new[] { LayoutPath, QualityPath, WorldPath })
            {
                var original = JObject.Parse(File.ReadAllText(path));
                var staged = result.Sources.TryGetValue(path, out string text) ? JObject.Parse(text) : original;
                foreach (RelocationMove move in request.moves)
                    foreach (JToken candidate in original.SelectTokens("$..position"))
                    {
                        if (candidate.Parent?.Parent is not JObject entry) continue;
                        string guid = (string)entry["guid"];
                        string scenePath = (string)entry["scenePath"];
                        string name = (string)entry["name"];
                        bool isOwner = !string.IsNullOrEmpty(move.sceneEntityId) && guid == move.sceneEntityId;
                        isOwner |= !string.IsNullOrEmpty(scenePath) && scenePath == move.transformPath;
                        isOwner |= path == LayoutPath && entry.Path.StartsWith("placements[", StringComparison.Ordinal)
                            && move.transformPath == "AuthoredEnvironment/" + name;
                        if (!isOwner) continue;
                        if (move.sourcePath == request.scenePath)
                            throw new InvalidOperationException("생성 소유 배치를 씬 전용으로 선언할 수 없습니다: " + candidate.Path);
                        var position = staged.SelectToken(candidate.Path, true).ToObject<Vector3>();
                        if (Mathf.Abs(position.x - move.position.x) > 0.001f || Mathf.Abs(position.z - move.position.z) > 0.001f)
                            throw new InvalidOperationException("재생성 소스의 좌표가 이동 요청과 다릅니다: " + path + ":" + candidate.Path);
                        JToken stagedEntry = staged.SelectToken(entry.Path, true);
                        if (entry["yaw"] != null && Mathf.Abs(Mathf.DeltaAngle(stagedEntry["yaw"].Value<float>(), move.rotation.y)) > 0.01f)
                            throw new InvalidOperationException("재생성 소스의 회전이 이동 요청과 다릅니다: " + entry.Path);
                        if (entry["rotation"] != null && Quaternion.Angle(Quaternion.Euler(stagedEntry["rotation"].ToObject<Vector3>()), Quaternion.Euler(move.rotation)) > 0.01f)
                            throw new InvalidOperationException("재생성 소스의 회전이 이동 요청과 다릅니다: " + entry.Path);
                        result.Diff.references.Add("배치 소유권: " + path + ":" + entry.Path);
                    }
            }
        }

        private static void AddRelocationFile(RelocationDiff diff, string path)
        {
            if (diff.files.Exists(file => file.path == path)) return;
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(path);
            diff.files.Add(new BaselineFile { path = path,
                sha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() });
            if (File.Exists(path + ".meta")) AddRelocationFile(diff, path + ".meta");
        }

        private static void RequireFiniteRelocation(Vector3 value)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.y)
                || float.IsNaN(value.z) || float.IsInfinity(value.z)) throw new InvalidOperationException("좌표는 유한해야 합니다.");
        }

        private static void RequireRelocationReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("변경 이유가 필요합니다.");
        }

        private static void CollectRelocationReferences(Scene scene, ResolvedRelocation result)
        {
            var targets = new HashSet<UnityEngine.Object>();
            var ids = new HashSet<string>();
            foreach (Transform moved in result.Transforms)
                foreach (Transform child in moved.GetComponentsInChildren<Transform>(true))
                {
                    targets.Add(child.gameObject);
                    foreach (Component component in child.GetComponents<Component>()) targets.Add(component);
                    var identity = child.GetComponent<SceneEntityId>();
                    if (identity != null) ids.Add(identity.Guid);
                    result.Diff.references.Add("이동 계층: " + GetTransformPath(child));
                }
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null) CollectRelocationObjectReferences(component, GetTransformPath(component.transform), targets, ids, result.Diff);
            foreach (string path in AssetDatabase.GetDependencies(scene.path, true))
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".asset", StringComparison.Ordinal)) continue;
                Type type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type)) continue;
                // 에셋은 씬 인스턴스를 직접 참조할 수 없다. ID 후보만 로드해 공용 DB의 전체 프리팹 로드를 피한다.
                string content = File.ReadAllText(path);
                bool hasId = false;
                foreach (string id in ids)
                    if (content.IndexOf(id, StringComparison.Ordinal) >= 0) { hasId = true; break; }
                if (!hasId) continue;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null) CollectRelocationObjectReferences(asset, path, targets, ids, result.Diff);
            }
        }

        private static void CollectRelocationObjectReferences(UnityEngine.Object owner, string path,
            HashSet<UnityEngine.Object> targets, HashSet<string> ids, RelocationDiff diff)
        {
            using var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.GetIterator();
            while (property.Next(true))
            {
                bool isReference = property.propertyType == SerializedPropertyType.ObjectReference && targets.Contains(property.objectReferenceValue);
                bool isId = property.propertyType == SerializedPropertyType.String && ids.Contains(property.stringValue);
                if (isReference || isId) diff.references.Add(path + ":" + property.propertyPath);
            }
        }
    }
}
