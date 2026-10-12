using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        /// <summary>승인된 노년 모델을 보존한 채 연목의 NPC 기능과 생활 배치를 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/연목 배치")]
        public static void PlaceElderResident()
        {
            RequireLifeEditor();
            var resident = JsonUtility.FromJson<Resident>(File.ReadAllText(DataRoot + "/Yeonmok.json"));
            var database = Require<ActorDatabase>(DatabasePath);
            string databaseBefore = EditorJsonUtility.ToJson(database);
            BuildContext context = null;
            try
            {
                if (!File.Exists(resident.PrefabPath))
                {
                    if (database.Contains(resident.id) || File.Exists(resident.DefinitionPath) || File.Exists(resident.NpcPath))
                        throw new InvalidOperationException("연목 신규 생성 경로 충돌");
                    var config = new CharacterBuildConfig
                    {
                        ActorKind = BuilderActorKind.Npc, Sex = BuilderSex.Male, UseMagicaCloth = false,
                        UseManualName = true, ManualName = resident.id,
                        Stats = new StatsAssignment { createNewNpcData = true, npcDisplayName = resident.displayName,
                            npcDescription = resident.description, npcEnableWander = false, npcWanderWaitTime = resident.waitSeconds }
                    };
                    context = new BuildContext(config) { PrefabName = resident.id,
                        PrefabFolder = Path.GetDirectoryName(resident.PrefabPath).Replace('\\', '/') };
                    // 외형 적용 단계를 실행하지 않는다. 검수된 메시·소재·리그를 그대로 인스턴스화한다.
                    context.RootInstance = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(
                        "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Male_Elder_Final.prefab"));
                    context.RootInstance.name = resident.id;
                    new AttachActorComponentsStep().Execute(context);
                    new GenerateActorDescStep().Execute(context);
                    new AssignStatsStep().Execute(context);
                    var animator = context.RootInstance.GetComponentInChildren<ActorAnimator>(true);
                    if (animator == null) animator = context.RootInstance.AddComponent<ActorAnimator>();
                    var serialized = new SerializedObject(animator);
                    serialized.FindProperty("_motionSet").objectReferenceValue = Require<ActorAnimationMotionSet>(resident.motionSet);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PathConfig.EnsureFolderExists(context.PrefabFolder);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(context.RootInstance, resident.PrefabPath);
                    if (prefab == null) throw new InvalidOperationException("연목 프리팹 저장 실패");
                    context.GeneratedAssetPaths.Add(resident.PrefabPath);
                    context.Bag["finalPrefabAsset"] = prefab;
                    new SyncActorDatabaseStep().Execute(context);
                    foreach (var asset in context.GeneratedDescs) AssetDatabase.SaveAssetIfDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(Require<ActorDefinitionSO>(resident.DefinitionPath));
                    AssetDatabase.SaveAssetIfDirty(database);
                    AssetDatabase.SaveAssetIfDirty(prefab);
                }
                var definition = Require<ActorDefinitionSO>(resident.DefinitionPath);
                definition.npcData.dialogueGraph = CreateDialogue(resident);
                EditorUtility.SetDirty(definition.npcData);
                AssetDatabase.SaveAssetIfDirty(definition.npcData);
                if (database.All.Count(x => x != null && x.actorId == resident.id) != 1)
                    throw new InvalidOperationException("연목 등록 중복");
                var approved = Require<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Male_Elder_Final.prefab");
                var saved = Require<GameObject>(resident.PrefabPath);
                foreach (var original in approved.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string path = AnimationUtility.CalculateTransformPath(original.transform, approved.transform);
                    var renderer = saved.transform.Find(path)?.GetComponent<SkinnedMeshRenderer>();
                    if (renderer == null || renderer.sharedMesh != original.sharedMesh
                        || !renderer.sharedMaterials.SequenceEqual(original.sharedMaterials))
                        throw new InvalidOperationException("승인된 노년 외형 참조 변경: " + path);
                }
                Directory.CreateDirectory(LifeOutput + "/Models");
                var review = UnityEngine.Object.Instantiate(saved);
                try { Capture(review, LifeOutput + "/Models/Yeonmok.png", false, 0); }
                finally { UnityEngine.Object.DestroyImmediate(review); }
                File.WriteAllText(LifeOutput + "/ElderValidation.txt", "승인 노년 모델의 SkinnedMeshRenderer 메시·소재 참조 일치, 등록 ID 1개 검사 통과.");
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(databaseBefore, database);
                database.InvalidateLookup();
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
                if (context != null)
                    foreach (string path in context.GeneratedAssetPaths.Distinct().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally
            {
                if (context?.RootInstance != null) UnityEngine.Object.DestroyImmediate(context.RootInstance);
                context?.DiscardStagedAssetBackups();
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            ApplyResidentLife();
        }
    }
}
