using System;
using System.Collections.Generic;
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
        /// <summary>기존 상인 데이터로 고정 외형을 제작하고 초상화 제작용 실제 모델을 촬영한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/상인 외형 분리")]
        public static void BuildLifeMerchant()
        {
            RequireLifeEditor();
            BuildLifeAppearance("MerchantAppearance.json", "");
        }

        /// <summary>기존 준 정의의 외형 참조만 고정 작업복 모델로 교체한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/준 외형 연결")]
        public static void BuildLifeJun()
        {
            RequireLifeEditor();
            BuildLifeAppearance("JunAppearance.json", "Assets/10.Datas/Actor/DataBase/Story/Story_NPC_Jun.asset");
        }

        private static void BuildLifeAppearance(string recipeName, string existingDefinitionPath)
        {
            var resident = JsonUtility.FromJson<Resident>(File.ReadAllText(DataRoot + "/" + recipeName));
            var database = Require<ActorDatabase>(DatabasePath);
            var originalDefinition = string.IsNullOrEmpty(existingDefinitionPath) ? null : Require<ActorDefinitionSO>(existingDefinitionPath);
            string originalDefinitionJson = originalDefinition == null ? null : EditorJsonUtility.ToJson(originalDefinition);
            var created = new List<string>();
            string before = EditorJsonUtility.ToJson(database);
            Directory.CreateDirectory(LifeOutput + "/Models");
            try
            {
                if (!File.Exists(resident.PrefabPath))
                {
                    if (originalDefinition == null && database.Contains(resident.id) || File.Exists(resident.DefinitionPath) || File.Exists(resident.PresetPath))
                        throw new InvalidOperationException("상인 생성 경로 충돌");
                    var catalog = new P09AssetCatalog();
                    catalog.Refresh();
                    var config = CreateConfig(resident, catalog);
                    config.Stats.createNewNpcData = false;
                    config.Stats.existingNpcData = Require<NpcActorSO>("Assets/10.Datas/Actor/Npc/" + resident.id + ".asset");
                    config.Stats.npcEnableWander = false;
                    var result = new PrefabBuildPipeline().Build(config);
                    if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
                    created.AddRange(result.GeneratedAssetPaths);
                    if (originalDefinition != null)
                    {
                        // 공용 정의의 수치·ID·기타 참조는 유지하고 요청된 외형만 바꾼다.
                        EditorJsonUtility.FromJsonOverwrite(originalDefinitionJson, originalDefinition);
                        originalDefinition.prefab = Require<GameObject>(result.PrefabPath);
                        EditorUtility.SetDirty(originalDefinition);
                        AssetDatabase.SaveAssetIfDirty(originalDefinition);
                    }
                    var root = PrefabUtility.LoadPrefabContents(result.PrefabPath);
                    try
                    {
                        var animator = new SerializedObject(root.GetComponentInChildren<ActorAnimator>(true));
                        animator.FindProperty("_motionSet").objectReferenceValue = Require<ActorAnimationMotionSet>(resident.motionSet);
                        animator.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(root, result.PrefabPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    var preset = ScriptableObject.CreateInstance<BuildPreset>();
                    preset.config = config;
                    preset.description = resident.description;
                    AssetDatabase.CreateAsset(preset, resident.PresetPath);
                    created.Add(resident.PresetPath);
                    AssetDatabase.SaveAssetIfDirty(database);
                }
                foreach (int yaw in new[] { 0, 45, 90 })
                {
                    var root = UnityEngine.Object.Instantiate(Require<GameObject>(resident.PrefabPath));
                    try { Capture(root, LifeOutput + "/Models/" + resident.id + "_" + yaw + ".png", false, yaw); }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                }
                File.WriteAllText(LifeOutput + "/" + resident.id + ".txt", "기존 NPC 데이터 보존, 고정 외형과 프리셋·ActorDefinition·DB 연결. 실제 대화·초상화 검수는 별도.");
            }
            catch
            {
                if (originalDefinition != null)
                {
                    EditorJsonUtility.FromJsonOverwrite(originalDefinitionJson, originalDefinition);
                    EditorUtility.SetDirty(originalDefinition);
                    AssetDatabase.SaveAssetIfDirty(originalDefinition);
                }
                EditorJsonUtility.FromJsonOverwrite(before, database);
                database.InvalidateLookup();
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
                foreach (string path in created.Distinct().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
        }
    }
}
