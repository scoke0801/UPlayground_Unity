using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Dialogue;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Interaction;
using UPlayGround.MovementController;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string LifeRecipePath = DataRoot + "/ResidentLife.json";
        private const string LifeOutput = Output + "/ResidentLife";
        private const string SourceVillage = "Assets/01.Scenes/Ingame/LakeOfLife.unity";

        [Serializable] private sealed class LifeRecipe
        {
            public LifeResident[] residents;
            public LifeChange[] changes;
        }

        [Serializable] private sealed class LifeResident
        {
            public string sourcePath, targetPath, actorId, npcData, sceneEntityId, prefabPath;
            public Vector3 position;
            public float yaw, radius, waitSeconds = 5;
        }

        [Serializable] private sealed class LifeChange
        {
            public string name, actorPath, spawnActorId, sceneEntityId, flag;
            public Vector3 position;
            public float yaw;
            public string[] beforeProps = Array.Empty<string>();
            public string[] afterProps = Array.Empty<string>();
        }

        /// <summary>원본 주민의 식별자·대화 연결을 보존하고 생활 배치와 사건별 복원을 적용한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/주민 생활 배치 적용")]
        public static void ApplyResidentLife()
        {
            RequireLifeEditor();
            var recipe = JsonUtility.FromJson<LifeRecipe>(File.ReadAllText(LifeRecipePath));
            string backup = LifeOutput + "/Runs/" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(backup);
            File.Copy(ScenePath, backup + "/Before.unity");
            File.Copy(PortraitTablePath, backup + "/SpeakerPortraitTable.asset");
            string portraitBefore = EditorJsonUtility.ToJson(Require<SpeakerPortraitTableSO>(PortraitTablePath));
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var source = EditorSceneManager.OpenScene(SourceVillage, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var report = new List<string>();
            try
            {
                Terrain terrain = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Terrain>()).Single();
                foreach (var resident in recipe.residents)
                {
                    var target = FindLifeTransform(scene, resident.targetPath);
                    GameObject prefab = string.IsNullOrEmpty(resident.prefabPath) ? null : Require<GameObject>(resident.prefabPath);
                    if (prefab != null && target != null && PrefabUtility.GetCorrespondingObjectFromSource(target.gameObject) != prefab)
                    {
                        if (target.GetComponent<SceneEntityId>()?.Guid != resident.sceneEntityId)
                            throw new InvalidOperationException("외형 교체 대상 ID 불일치");
                        UnityEngine.Object.DestroyImmediate(target.gameObject);
                        target = null;
                    }
                    if (target == null)
                    {
                        var original = prefab != null ? prefab : (FindLifeTransform(source, resident.sourcePath)?.gameObject
                            ?? throw new InvalidOperationException("원본 주민 누락: " + resident.sourcePath));
                        var copy = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene) : UnityEngine.Object.Instantiate(original);
                        copy.name = resident.targetPath.Split('/').Last();
                        copy.transform.SetParent(RequireLifePath(scene, resident.targetPath[..resident.targetPath.LastIndexOf('/')]), false);
                        target = copy.transform;
                        // 구 안내인에 직렬화된 중복 컨트롤러를 새 배치로 복제하지 않는다.
                        var controllers = copy.GetComponents<NpcMovementController>();
                        for (int i = 1; i < controllers.Length; i++) UnityEngine.Object.DestroyImmediate(controllers[i]);
                    }
                    var actor = target.GetComponent<NpcActor>() ?? throw new InvalidOperationException("NPC 누락");
                    var data = Require<NpcActorSO>(resident.npcData);
                    var serialized = new SerializedObject(actor);
                    serialized.FindProperty("_actorId").stringValue = resident.actorId;
                    serialized.FindProperty("_data").objectReferenceValue = data;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
                    var identity = target.GetComponent<SceneEntityId>();
                    if (identity == null) identity = target.gameObject.AddComponent<SceneEntityId>();
                    else if (identity.HasGuid && identity.Guid != resident.sceneEntityId)
                        throw new InvalidOperationException("기존 주민 ID 불일치: " + resident.targetPath);
                    identity.EditorSetGuid(resident.sceneEntityId);
                    Vector3 point = GroundLifePoint(terrain, resident.position);
                    target.SetPositionAndRotation(point, Quaternion.Euler(0, resident.yaw, 0));
                    target.gameObject.SetActive(true);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    ConfigureLifeBrain(target.GetComponent<NpcBrain>(), resident.radius, resident.waitSeconds);
                    report.Add(resident.actorId + " | " + resident.sceneEntityId + " | " + point + " | " + resident.npcData);
                }
                foreach (var change in recipe.changes) ApplyLifeChange(scene, terrain, change);
                ValidateLife(scene, recipe);
                EditorSceneManager.CloseScene(source, true);
                ConnectLifePortraits();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scene = EditorSceneManager.OpenScene(ScenePath);
                ValidateLife(scene, recipe);
                File.WriteAllLines(backup + "/Applied.txt", report);
                File.WriteAllLines(LifeOutput + "/Applied.txt", report);
                Debug.Log("[ResidentLife] 주민 " + recipe.residents.Length + "명, 사건 표현 " + recipe.changes.Length + "개 저장·재로드 통과.");
            }
            catch
            {
                if (source.IsValid() && source.isLoaded) EditorSceneManager.CloseScene(source, true);
                // 실패한 메모리 씬을 버리고 이번 호출 직전의 디스크 씬을 보존한다.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var portraits = Require<SpeakerPortraitTableSO>(PortraitTablePath);
                EditorJsonUtility.FromJsonOverwrite(portraitBefore, portraits);
                EditorUtility.SetDirty(portraits);
                AssetDatabase.SaveAssetIfDirty(portraits);
                File.Copy(backup + "/Before.unity", ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath);
                EditorSceneManager.OpenScene(ScenePath);
                throw;
            }
        }

        private static void RequireLifeEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 편집 모드가 필요합니다.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("미저장 씬이 있습니다.");
        }

        private static void ConnectLifePortraits()
        {
            var table = Require<SpeakerPortraitTableSO>(PortraitTablePath);
            foreach (var item in new[] { ("Shop", "상인"), ("Jun", "준"), ("Yeonmok", "연목") })
            {
                string path = "Assets/04.Images/UI/Portrait/Npc/Npc_" + item.Item1 + "_Dialogue_Portrait.png";
                if (!File.Exists(path)) continue;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                ConnectPortrait(table, item.Item2, Require<Sprite>(path));
            }
            AssetDatabase.SaveAssetIfDirty(table);
        }

        private static void ConfigureLifeBrain(NpcBrain brain, float radius, float wait)
        {
            if (brain == null) throw new InvalidOperationException("주민 배회 설정 누락");
            var serialized = new SerializedObject(brain);
            serialized.FindProperty("_enableWander").boolValue = radius > 0;
            serialized.FindProperty("_patrolRadius").floatValue = radius;
            serialized.FindProperty("_patrolWaitTime").floatValue = wait;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyLifeChange(Scene scene, Terrain terrain, LifeChange change)
        {
            Transform owner = RequireLifePath(scene, "ResidentLife/" + change.name);
            var presentation = owner.GetComponent<NpcLifePresentation>();
            if (presentation == null) presentation = owner.gameObject.AddComponent<NpcLifePresentation>();
            Transform actor = string.IsNullOrEmpty(change.actorPath) ? null : FindLifeTransform(scene, change.actorPath)
                ?? throw new InvalidOperationException("사건 주민 누락: " + change.actorPath);
            Transform before = actor == null ? null : RequireLifePath(scene, "ResidentLife/" + change.name + "/Before");
            if (before != null) before.SetPositionAndRotation(actor.position, actor.rotation);
            Transform after = RequireLifePath(scene, "ResidentLife/" + change.name + "/After");
            after.SetPositionAndRotation(GroundLifePoint(terrain, change.position), Quaternion.Euler(0, change.yaw, 0));
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("_completedFlag").stringValue = change.flag;
            serialized.FindProperty("_actor").objectReferenceValue = actor == null ? null : actor.GetComponent<NpcActor>();
            serialized.FindProperty("_spawnActorId").stringValue = change.spawnActorId;
            serialized.FindProperty("_sceneEntityId").stringValue = change.sceneEntityId;
            serialized.FindProperty("_beforeAnchor").objectReferenceValue = before;
            serialized.FindProperty("_afterAnchor").objectReferenceValue = after;
            SetLifeProps(serialized.FindProperty("_beforeVisuals"), scene, change.beforeProps, true);
            SetLifeProps(serialized.FindProperty("_afterVisuals"), scene, change.afterProps, false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLifeProps(SerializedProperty property, Scene scene, string[] paths, bool visible)
        {
            property.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                Transform target = FindLifeTransform(scene, paths[i]) ?? throw new InvalidOperationException("생활 소품 누락: " + paths[i]);
                property.GetArrayElementAtIndex(i).objectReferenceValue = target.gameObject;
                target.gameObject.SetActive(visible);
            }
        }

        private static Vector3 GroundLifePoint(Terrain terrain, Vector3 position)
        {
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            if (!NavMesh.SamplePosition(position, out var hit, 1.5f, NavMesh.AllAreas))
                throw new InvalidOperationException("생활 지점 NavMesh 누락: " + position);
            return hit.position;
        }

        private static Transform FindLifeTransform(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            var roots = scene.GetRootGameObjects().Where(x => x.name == parts[0]).ToArray();
            if (roots.Length > 1) throw new InvalidOperationException("중복 루트: " + path);
            Transform target = roots.SingleOrDefault()?.transform;
            for (int i = 1; i < parts.Length && target != null; i++) target = target.Find(parts[i]);
            return target;
        }

        private static Transform RequireLifePath(Scene scene, string path)
        {
            Transform existing = FindLifeTransform(scene, path);
            if (existing != null) return existing;
            int separator = path.LastIndexOf('/');
            Transform parent = separator < 0 ? null : RequireLifePath(scene, path[..separator]);
            var created = new GameObject(separator < 0 ? path : path[(separator + 1)..]);
            SceneManager.MoveGameObjectToScene(created, scene);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static void ValidateLife(Scene scene, LifeRecipe recipe)
        {
            var identities = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SceneEntityId>(true)).ToArray();
            foreach (var resident in recipe.residents)
            {
                Transform target = FindLifeTransform(scene, resident.targetPath);
                if (target == null || identities.Count(x => x.Guid == resident.sceneEntityId) != 1)
                    throw new InvalidOperationException("주민 배치·ID 오류: " + resident.actorId);
                foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        throw new InvalidOperationException("주민 Missing Script: " + resident.actorId);
                var actor = target.GetComponent<NpcActor>();
                var serialized = new SerializedObject(actor);
                if (serialized.FindProperty("_data").objectReferenceValue != Require<NpcActorSO>(resident.npcData)
                    || serialized.FindProperty("_actorId").stringValue != resident.actorId)
                    throw new InvalidOperationException("주민 데이터 연결 오류: " + resident.actorId);
            }
        }
    }
}
