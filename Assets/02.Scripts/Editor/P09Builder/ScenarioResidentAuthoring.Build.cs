using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string DataRoot = "Assets/10.Datas/Actor/ScenarioResidents";
        private const string DatabasePath = "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset";
        [Serializable] private sealed class Recipe { public Resident[] residents; }
        [Serializable] private sealed class Resident
        {
            public string id, displayName, description, sceneEntityId, motionSet;
            public string[] dialogueLines;
            public BuilderSex sex;
            public int armor, face, hair, hairColor, skin, eye, legArmor;
            public float wanderRadius, waitSeconds, yaw;
            public Vector3 position;
            public string PrefabPath => "Assets/03.Prefabs/Characters/ScenarioResidents/Npc/" + id + "/" + id + ".prefab";
            public string DefinitionPath => PathConfig.ActorDefinitionRoot + "/" + id + "_ActorDef.asset";
            public string NpcPath => PathConfig.NpcDataRoot + "/" + id + "_NpcData.asset";
            public string PresetPath => DataRoot + "/" + id + "_BuildPreset.asset";
        }

        /// <summary>확정 제작표의 신규 주민만 만들고 기존 결과와 사용자 편집은 보존한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/시나리오 주민 제작")]
        public static void BuildResidents()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 실행하세요.");
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(DataRoot + "/ScenarioResidents.json"));
            var database = Require<ActorDatabase>(DatabasePath);
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            var created = new List<string>();
            string before = EditorJsonUtility.ToJson(database);
            Directory.CreateDirectory(Output + "/Models");
            try
            {
                foreach (var resident in recipe.residents)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(resident.DefinitionPath);
                    if (existing != null)
                    {
                        ValidateResident(existing, resident, database);
                        continue;
                    }
                    if (database.Contains(resident.id) || File.Exists(resident.PrefabPath)
                        || File.Exists(resident.NpcPath) || File.Exists(resident.PresetPath))
                        throw new InvalidOperationException("기존 출력과 충돌합니다: " + resident.id);
                    var config = CreateConfig(resident, catalog);
                    var result = new PrefabBuildPipeline().Build(config);
                    if (!result.Success) throw new InvalidOperationException(result.ErrorMessage);
                    created.AddRange(result.GeneratedAssetPaths);
                    var prefab = PrefabUtility.LoadPrefabContents(result.PrefabPath);
                    try
                    {
                        var animator = prefab.GetComponentInChildren<ActorAnimator>(true);
                        if (animator == null) throw new InvalidOperationException("ActorAnimator 누락");
                        var serialized = new SerializedObject(animator);
                        serialized.FindProperty("_motionSet").objectReferenceValue = Require<ActorAnimationMotionSet>(resident.motionSet);
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(prefab, result.PrefabPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(prefab); }
                    var preset = ScriptableObject.CreateInstance<BuildPreset>();
                    preset.config = config;
                    preset.description = resident.description + " 카탈로그 실물 촬영 후 고정. 무기·천 물리 없음.";
                    AssetDatabase.CreateAsset(preset, resident.PresetPath);
                    created.Add(resident.PresetPath);
                    AssetDatabase.SaveAssets();
                    ValidateResident(Require<ActorDefinitionSO>(resident.DefinitionPath), resident, database);
                    if (Environment.GetCommandLineArgs().Contains("-residentFailureProbe"))
                        throw new InvalidOperationException("P03 생성 후 복구 검사");
                }
                foreach (var resident in recipe.residents) CaptureResident(resident);
                foreach (string path in new[] { "Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Lake_Intro_Guide.asset", "Assets/10.Datas/Dialogue/Story/Dialogue/DLG_Npc_Morgan.asset" })
                    if (!Require<UPlayGround.Dialogue.DialogueGraphSO>(path).TryValidatePlayback(out var error))
                        throw new InvalidOperationException(path + ": " + error);
                File.WriteAllText(Output + "/Models/validation.txt", "제작표 " + recipe.residents.Length + "명: 고정 외형·프리셋·NPC·ActorDefinition·DB·생활 모션 연결 검사 통과.");
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(before, database);
                database.InvalidateLookup();
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
                foreach (string path in created.Distinct().Reverse()) AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
                File.WriteAllText(Output + "/Models/rollback.txt", "이번 호출의 신규 에셋과 DB 추가분 복구. 삭제 목록:\n" + string.Join("\n", created));
                throw;
            }
        }

        private static CharacterBuildConfig CreateConfig(Resident resident, P09AssetCatalog catalog)
        {
            var config = new CharacterBuildConfig
            {
                ActorKind = BuilderActorKind.Npc, PlayerCharacterType = CharacterActorType.None,
                Sex = resident.sex, UseMagicaCloth = false, IsRandomAppearance = false,
                UseManualName = true, ManualName = resident.id,
                SaveBaseFolder = "Assets/03.Prefabs/Characters/ScenarioResidents",
                FaceTypeSo = SelectPart(catalog.FaceTypes, resident.face),
                HairStyleSo = SelectPart(catalog.HairStyles, resident.hair),
                HairColorSo = SelectPart(catalog.HairColors, resident.hairColor),
                SkinColorSo = Require<ScriptableObject>(PathConfig.CatalogRoot + "/SkinColor/SkinColorEditPartData_" + (resident.sex == BuilderSex.Male ? "Male" : "Famale") + "_" + resident.skin.ToString("00") + ".asset"),
                EyeColorSo = SelectPart(catalog.EyeColors, resident.eye),
                Stats = new StatsAssignment
                {
                    createNewNpcData = true, npcDisplayName = resident.displayName,
                    npcDescription = resident.description, npcEnableWander = true,
                    wanderRadius = resident.wanderRadius, npcWanderWaitTime = resident.waitSeconds
                }
            };
            var armor = ArmorIndexPresetUtility.Build(catalog).Single(x => x.Index == resident.armor);
            ArmorIndexPresetUtility.Apply(config.ArmorSelections, armor);
            config.ArmorSelections.Set(BuilderArmorSlot.Head, null);
            if (resident.legArmor > 0)
                config.ArmorSelections.Set(BuilderArmorSlot.Leg, SelectPart(catalog.Legs, resident.legArmor));
            return config;
        }

        private static ScriptableObject SelectPart(List<ScriptableObject> parts, int id)
            => parts.Single(x => x is IEditPartData part && part.ContentId == id);

        private static T Require<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("에셋 누락: " + path);

        private static void ValidateResident(ActorDefinitionSO definition, Resident resident, ActorDatabase database)
        {
            if (definition.actorId != resident.id || definition.prefab == null || definition.npcData == null
                || definition.characterType != CharacterActorType.None || database.All.Count(x => x != null && x.actorId == resident.id) != 1)
                throw new InvalidOperationException("주민 등록 오류: " + resident.id);
            var preset = Require<BuildPreset>(resident.PresetPath);
            if (preset.config.IsRandomAppearance || preset.config.UseMagicaCloth || preset.config.ActorKind != BuilderActorKind.Npc)
                throw new InvalidOperationException("고정 NPC 제작 설정 오류");
            foreach (var transform in definition.prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing Script: " + transform.name);
            var animator = definition.prefab.GetComponentInChildren<ActorAnimator>(true);
            var serialized = new SerializedObject(animator);
            if (serialized.FindProperty("_motionSet").objectReferenceValue != Require<ActorAnimationMotionSet>(resident.motionSet))
                throw new InvalidOperationException("생활 모션 연결 오류");
        }

        private static void CaptureResident(Resident resident)
        {
            foreach (int yaw in new[] { 0, 45, 90, 180 })
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(resident.PrefabPath));
                try
                {
                    Capture(root, Output + "/Models/" + resident.id + "_" + yaw + ".png", false, yaw);
                }
                finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
            }
        }
    }
}
