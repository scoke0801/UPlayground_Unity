using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>제작표의 장비·등급 조합을 기존 P09 전투 프리팹에서 파생하고 검증 후 등록한다.</summary>
    public static partial class P09ProgressionBuilder
    {
        private const string ToolId = "UPlayGround/캐릭터/P09/상위 장비 몬스터 제작";
        private const string DataRoot = "Assets/10.Datas/Actor/P09Progression";
        private const string RecipePath = DataRoot + "/P09Progression.json";
        private const string DatabasePath = "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset";
        private const string CodexPath = "Assets/10.Datas/Codex/Monster/MonsterCodexDatabase.asset";
        private const string ReportPath = "Logs/P09-progression-validation.txt";

        [Serializable] private sealed class Recipe
        {
            public Tier[] tiers;
            public Role[] roles;
            public float experienceRatio;
            public float eliteExperienceMultiplier;
        }

        [Serializable] private sealed class Tier
        {
            public int armor;
            public int level;
            public MonsterActorGrade grade;
            public string label;
            public int weapon;
        }

        [Serializable] private sealed class Role
        {
            public string key;
            public string source;
            public BuilderSex sex;
            public string label;
            public string[] weaponSlots;
        }

        private sealed class Entry
        {
            public Tier Tier;
            public Role Role;
            public ActorDefinitionSO Source;
            public CharacterBuildConfig Config;
            public string Id => $"Enemy_P09_Armor{Tier.armor:00}_{Role.key}";
            public string Folder => $"{DataRoot}/Generated/{Id}";
            public string DefinitionPath => $"{Folder}/{Id}_ActorDef.asset";
            public string PrefabPath => $"Assets/03.Prefabs/Characters/Enemy/Armor_{Tier.armor:00}/{Id}/{Id}.prefab";
        }

        /// <summary>새 조합만 제작한다. 기존 결과는 검증만 하여 수동 튜닝과 GUID를 보존한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId, priority = 1101)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode에서는 제작할 수 없습니다.");
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var database = Require<ActorDatabase>(DatabasePath);
            var codex = Require<MonsterCodexDatabaseSO>(CodexPath);
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            List<Entry> entries = Prepare(recipe, catalog, database, codex);
            var created = new List<string>();
            var definitions = new List<ActorDefinitionSO>();
            var codexEntries = new List<MonsterCodexEntrySO>();
            string databaseBackup = EditorJsonUtility.ToJson(database);
            string codexBackup = EditorJsonUtility.ToJson(codex);
            try
            {
                foreach (Entry entry in entries)
                {
                    var existing = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(entry.DefinitionPath);
                    if (existing != null)
                    {
                        ValidateDefinition(existing, entry, catalog);
                        continue;
                    }
                    ActorDefinitionSO definition = Create(entry, recipe, catalog, created);
                    ValidateDefinition(definition, entry, catalog);
                    definitions.Add(definition);
                    var sourceCodex = codex.GetEntry(entry.Source.actorId);
                    var newCodex = Clone(sourceCodex, entry.Folder + "/Codex.asset", created);
                    newCodex.actorId = entry.Id;
                    newCodex.displayNameOverride = string.Empty;
                    newCodex.descriptionOverride = string.Empty;
                    Save(newCodex);
                    codexEntries.Add(newCodex);
                }

                // 모든 프리팹이 완성된 뒤에만 런타임 데이터베이스에 공개한다.
                foreach (ActorDefinitionSO definition in definitions)
                    database.AddDefinition(definition);
                foreach (MonsterCodexEntrySO entry in codexEntries)
                    codex.AddEntry(entry);
                Save(database);
                Save(codex);
                Validate();
                Debug.Log($"[P09Progression] 신규 {definitions.Count}개 제작·등록 완료");
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(databaseBackup, database);
                EditorJsonUtility.FromJsonOverwrite(codexBackup, codex);
                database.InvalidateLookup();
                codex.InvalidateLookup();
                Save(database);
                Save(codex);
                for (int i = created.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
        }

        private static List<Entry> Prepare(Recipe recipe, P09AssetCatalog catalog,
            ActorDatabase database, MonsterCodexDatabaseSO codex)
        {
            if (recipe?.tiers == null || recipe.roles == null || recipe.tiers.Length == 0
                || recipe.roles.Length == 0 || recipe.experienceRatio <= 0f
                || recipe.eliteExperienceMultiplier <= 0f)
                throw new InvalidOperationException("제작표의 조합 또는 보상 설정이 비어 있습니다.");
            var result = new List<Entry>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (Tier tier in recipe.tiers)
            foreach (Role role in recipe.roles)
            {
                var source = Require<ActorDefinitionSO>($"Assets/10.Datas/Actor/DataBase/{role.source}_ActorDef.asset");
                if (source.prefab == null || source.monsterProfile == null || source.poiseData == null
                    || source.EffectiveBreakGaugeData == null || source.EffectiveMonsterScaling == null
                    || source.EffectiveBehaviorData?.behaviorTree == null || source.EffectiveDropTable == null)
                    throw new InvalidOperationException($"원본의 전투 연결 누락: {source.name}");
                if (tier.level <= source.EffectiveLevel || tier.level > source.EffectiveMonsterScaling.levelCap
                    || !Enum.IsDefined(typeof(MonsterActorGrade), tier.grade))
                    throw new InvalidOperationException($"제작표의 등급/레벨이 잘못되었습니다: {tier.armor}");
                if (codex.GetEntry(source.actorId) == null)
                    throw new InvalidOperationException($"원본 도감 누락: {source.actorId}");
                var config = new CharacterBuildConfig { Sex = role.sex };
                ArmorIndexPreset armor = ArmorIndexPresetUtility.Build(catalog).SingleOrDefault(x => x.Index == tier.armor);
                if (armor == null || armor.Chest == null)
                    throw new InvalidOperationException($"갑옷 카탈로그 누락: {tier.armor}");
                ArmorIndexPresetUtility.Apply(config.ArmorSelections, armor);
                foreach (string slot in role.weaponSlots)
                {
                    List<ScriptableObject> weapons = GetWeapons(catalog, slot);
                    ScriptableObject part = weapons.SingleOrDefault(x => x is IEditPartData d && d.ContentId == tier.weapon);
                    if (part == null)
                        throw new InvalidOperationException($"무기 카탈로그 누락: {slot}/{tier.weapon}");
                    typeof(CharacterBuildConfig).GetField(slot + "So").SetValue(config, part);
                }
                var entry = new Entry { Tier = tier, Role = role, Source = source, Config = config };
                if (!ids.Add(entry.Id))
                    throw new InvalidOperationException($"제작표 ID 중복: {entry.Id}");
                ActorDefinitionSO existing = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(entry.DefinitionPath);
                if (existing == null && (database.Contains(entry.Id) || codex.GetEntry(entry.Id) != null
                    || (Directory.Exists(entry.Folder) && Directory.GetFiles(entry.Folder).Length > 0)
                    || File.Exists(entry.PrefabPath)))
                    throw new InvalidOperationException($"불완전하거나 충돌하는 결과가 있습니다: {entry.Id}");
                ValidateCombat(source);
                result.Add(entry);
            }
            return result;
        }

        private static ActorDefinitionSO Create(Entry entry, Recipe recipe,
            P09AssetCatalog catalog, List<string> created)
        {
            PathConfig.EnsureFolderExists(entry.Folder);
            PathConfig.EnsureFolderExists(Path.GetDirectoryName(entry.PrefabPath).Replace('\\', '/'));
            var definition = Clone(entry.Source, entry.DefinitionPath, created);
            definition.actorId = entry.Id;
            definition.displayName = entry.Tier.label + " " + entry.Role.label;
            definition.grade = entry.Tier.grade;
            definition.level = entry.Tier.level;
            definition.attributeProfile = null;
            definition.prefab = null;
            definition.monsterProfile = Clone(entry.Source.monsterProfile, entry.Folder + "/Profile.asset", created);
            definition.poiseData = Clone(entry.Source.poiseData, entry.Folder + "/Poise.asset", created);
            definition.breakGaugeData = Clone(entry.Source.EffectiveBreakGaugeData, entry.Folder + "/BreakGauge.asset", created);
            definition.monsterScaling = entry.Source.EffectiveMonsterScaling;
            definition.abilitySet = entry.Source.EffectiveAbilitySet;
            definition.behaviorData = entry.Source.EffectiveBehaviorData;
            definition.dropTable = entry.Source.EffectiveDropTable;
            definition.recruitableAs = CharacterActorType.None;
            definition.expReward = MonsterExperienceRewardService.Calculate(definition,
                new MonsterExperienceRewardService.Options
                {
                    LevelCurve = Require<LevelCurveSO>("Assets/10.Datas/Party/Growth/LevelCurve_Default.asset"),
                    PlayerLevel = entry.Tier.level,
                    SameLevelNormalRewardRatio = recipe.experienceRatio,
                    EliteMultiplier = recipe.eliteExperienceMultiplier,
                    RecordUndo = false,
                }).Reward;
            var profile = definition.monsterProfile;
            profile.grade = definition.grade;
            profile.level = definition.level;
            profile.breakGaugeData = definition.breakGaugeData;
            profile.monsterScaling = definition.monsterScaling;
            profile.expReward = definition.expReward;
            profile.recruitableAs = CharacterActorType.None;
            // 게이지 최대치에 이미 등급 배율을 bake하므로 런타임에서 두 번 곱하지 않는다.
            var policy = definition.breakGaugeData.gradePolicy;
            policy.weakGaugeMultiplier = policy.normalGaugeMultiplier = 1f;
            policy.eliteGaugeMultiplier = policy.bossGaugeMultiplier = 1f;
            string statPath = entry.Folder + "/AttributeProfile_" + entry.Id + ".asset";
            created.Add(statPath);
            MonsterStatBakeService.Bake(definition, new MonsterStatBakeService.Options
            {
                StatSavePath = entry.Folder, UseStableStatAssetPath = true,
                ForceRegenerate = true, RecordUndo = false,
                SyncGeneratedPoise = true, SyncExistingBreakGauge = true,
            });
            Save(definition.attributeProfile);
            Save(definition.poiseData);
            Save(definition.breakGaugeData);
            Save(profile);
            Save(definition);

            created.Add(entry.PrefabPath);
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(entry.Source.prefab), entry.PrefabPath))
                throw new InvalidOperationException($"프리팹 복사 실패: {entry.Id}");
            GameObject root = PrefabUtility.LoadPrefabContents(entry.PrefabPath);
            try
            {
                EnsureNoMissingScripts(root);
                root.name = entry.Id;
                AppearanceApplier.Apply(root, entry.Config, catalog);
                // 활은 카탈로그에 없으므로 원본의 활·화살·전용 바인딩을 그대로 보존한다.
                if (entry.Role.weaponSlots.Length > 0)
                    ApplyWeaponStep.Apply(root, entry.Config, catalog);
                foreach (ParentConstraint constraint in root.GetComponentsInChildren<ParentConstraint>(true))
                {
                    if (!constraint.gameObject.activeInHierarchy || constraint.sourceCount < 2) continue;
                    var hand = constraint.GetSource(0);
                    hand.weight = 1f;
                    constraint.SetSource(0, hand);
                    var sheath = constraint.GetSource(1);
                    sheath.weight = 0f;
                    constraint.SetSource(1, sheath);
                }
                MonsterActor actor = root.GetComponent<MonsterActor>();
                var serialized = new SerializedObject(actor);
                serialized.FindProperty("_actorId").stringValue = entry.Id;
                serialized.FindProperty("_definition").objectReferenceValue = definition;
                serialized.FindProperty("_grade").intValue = (int)definition.grade;
                serialized.FindProperty("_level").intValue = definition.level;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ReflectionUtil.SetField(root.GetComponent<PoiseStat>(), "_data", definition.poiseData);
                definition.prefab = PrefabUtility.SaveAsPrefabAsset(root, entry.PrefabPath, out bool success);
                if (!success || definition.prefab == null)
                    throw new InvalidOperationException($"프리팹 저장 실패: {entry.Id}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Save(definition);
            return definition;
        }

        /// <summary>제작된 데이터·프리팹·도감·공격 연결을 읽기 전용으로 전수 확인한다.</summary>
        public static void Validate()
        {
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            var database = Require<ActorDatabase>(DatabasePath);
            var codex = Require<MonsterCodexDatabaseSO>(CodexPath);
            var report = new List<string>();
            foreach (Entry entry in Prepare(recipe, catalog, database, codex))
            {
                ActorDefinitionSO definition = Require<ActorDefinitionSO>(entry.DefinitionPath);
                ValidateDefinition(definition, entry, catalog);
                if (database.GetDefinition(entry.Id) != definition || codex.GetEntry(entry.Id) == null)
                    throw new InvalidOperationException($"DB 등록 누락: {entry.Id}");
                report.Add($"{entry.Id}: Lv{definition.EffectiveLevel} {definition.EffectiveGrade}, EXP {definition.EffectiveExpReward}, 공격 {ValidateCombat(definition)}개");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllLines(ReportPath, report);
            Debug.Log($"[P09Progression] {report.Count}개 연결 검증 통과: {ReportPath}");
        }

        /// <summary>공유 GAS의 기존 경고와 신규 경고를 비교할 전수 보고서를 저장한다.</summary>
        public static void ValidateAbilities()
        {
            var issues = AbilityDataValidator.ValidateAll();
            File.WriteAllLines("Logs/P09-ability-validation.txt", issues.Select(x =>
                $"{x.Severity}: {AssetDatabase.GetAssetPath(x.Context)}: {x.Message}"));
            Debug.Log($"[P09Progression] 전체 GAS 오류 {issues.Count(x => x.Severity == AbilityValidationSeverity.Error)}, 경고 {issues.Count(x => x.Severity == AbilityValidationSeverity.Warning)}");
        }

        private static void ValidateDefinition(ActorDefinitionSO definition, Entry entry, P09AssetCatalog catalog)
        {
            if (definition.actorId != entry.Id || definition.prefab == null || definition.attributeProfile == null
                || definition.monsterProfile == null || definition.EffectiveLevel != entry.Tier.level
                || definition.EffectiveGrade != entry.Tier.grade || definition.EffectiveExpReward <= 0
                || definition.EffectiveAbilitySet != entry.Source.EffectiveAbilitySet
                || definition.EffectiveBehaviorData != entry.Source.EffectiveBehaviorData
                || definition.EffectiveDropTable == null || definition.poiseData == entry.Source.poiseData
                || definition.EffectiveBreakGaugeData == entry.Source.EffectiveBreakGaugeData)
                throw new InvalidOperationException($"정의 연결 또는 공유 데이터 격리 오류: {entry.Id}");
            MonsterActor actor = definition.prefab.GetComponent<MonsterActor>();
            var serialized = new SerializedObject(actor);
            if (serialized.FindProperty("_definition").objectReferenceValue != definition
                || serialized.FindProperty("_actorId").stringValue != entry.Id)
                throw new InvalidOperationException($"프리팹 역참조 누락: {entry.Id}");
            foreach (var value in MonsterStatBakeService.CalculatePlanned(definition))
            {
                if (!definition.attributeProfile.TryGetBaseValue(value.Key, out float actual)
                    || !Mathf.Approximately(actual, value.Value))
                    throw new InvalidOperationException($"스탯 bake 불일치: {entry.Id}/{value.Key}");
            }
            var poise = new SerializedObject(definition.prefab.GetComponent<PoiseStat>());
            if (poise.FindProperty("_data").objectReferenceValue != definition.poiseData)
                throw new InvalidOperationException($"강인도 프리팹 연결 오류: {entry.Id}");
            EnsureNoMissingScripts(definition.prefab);
            ValidateCombat(definition);
            var transforms = definition.prefab.GetComponentsInChildren<Transform>(true);
            foreach (string slot in entry.Role.weaponSlots)
            {
                var part = (IEditPartData)typeof(CharacterBuildConfig).GetField(slot + "So").GetValue(entry.Config);
                if (!transforms.Any(t => t.name == part.MeshName && IsActiveBelowRoot(t, definition.prefab.transform)))
                    throw new InvalidOperationException($"무기 메시 비활성/누락: {entry.Id}/{part.MeshName}");
            }
            foreach (BuilderArmorSlot slot in BuilderArmorSlotExtensions.All)
            {
                if (entry.Config.ArmorSelections.Get(slot) is not IEditPartData part) continue;
                string male = string.Format(part.MeshName, "Male");
                string female = string.Format(part.MeshName, "Female");
                string fem = string.Format(part.MeshName, "Fem");
                bool found = transforms.Any(t => (t.name == male || t.name == female || t.name == fem)
                    && IsActiveBelowRoot(t, definition.prefab.transform));
                if (!found) throw new InvalidOperationException($"갑옷 메시 비활성/누락: {entry.Id}/{slot}/{part.MeshName}");
            }
        }

        private static bool IsActiveBelowRoot(Transform current, Transform root)
        {
            while (current != null && current != root)
            {
                if (!current.gameObject.activeSelf) return false;
                current = current.parent;
            }
            return true;
        }

        private static int ValidateCombat(ActorDefinitionSO definition)
        {
            EnsureNoMissingScripts(definition.prefab);
            var animator = definition.prefab.GetComponentInChildren<ActorAnimator>(true);
            var controller = definition.prefab.GetComponent<EnemyAIController>();
            if (animator?.MotionSet == null || controller == null || !controller.enabled
                || definition.EffectiveAbilitySet == null)
                throw new InvalidOperationException($"AI/애니메이터/AbilitySet 누락: {definition.name}");
            int attacks = 0;
            foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll())
            foreach (var variant in ability.variants)
            {
                if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out var attack)
                    || !attack.aiSelectable) continue;
                var motion = animator.MotionSet.GetAbilityMotionAsset(attack.motionKey);
                if (variant.executionPayload is not UPlayGroundMotionAbilityPayloadSO
                    || !attack.motionKey.IsValid || motion == null
                    || attack.baseInfo?.hitPhases == null || attack.baseInfo.hitPhases.Count == 0)
                    throw new InvalidOperationException($"Motion/HitPhase 누락: {definition.name}/{ability.name}");
                if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(motion))
                    throw new InvalidOperationException($"Motion 타입 누락: {motion.name}");
                attacks++;
            }
            if (attacks == 0) throw new InvalidOperationException($"AI 공격 없음: {definition.name}");
            return attacks;
        }

        private static void EnsureNoMissingScripts(GameObject root)
        {
            if (root == null) throw new InvalidOperationException("프리팹이 없습니다.");
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                    throw new InvalidOperationException($"Missing Script: {root.name}/{child.name}");
        }

        private static List<ScriptableObject> GetWeapons(P09AssetCatalog catalog, string slot) => slot switch
        {
            "Sword" => catalog.Swords, "SubSword" => catalog.SubSwords,
            "GreatSword" => catalog.GreatSwords, "Shield" => catalog.Shields,
            "DualAxe" => catalog.DualAxes,
            _ => throw new InvalidOperationException($"지원하지 않는 카탈로그 슬롯: {slot}"),
        };

        private static T Require<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"에셋 누락: {path}");

        private static T Clone<T>(T source, string path, List<string> created) where T : ScriptableObject
        {
            if (source == null || File.Exists(path))
                throw new InvalidOperationException($"복제 원본 누락 또는 경로 충돌: {path}");
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = Path.GetFileNameWithoutExtension(path);
            created.Add(path);
            AssetDatabase.CreateAsset(clone, path);
            return clone;
        }

        private static void Save(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
