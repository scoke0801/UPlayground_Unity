using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class P09ProgressionBuilder
    {
        private const string StaffRecipePath = DataRoot + "/P09StaffProgression.json";
        private const string StaffRoot = "Assets/10.Datas/Ability/Actor/P09Staff";
        private const string StaffSetPath = StaffRoot + "/AbilitySet_P09Staff.asset";
        private const string StaffMappingPath = StaffRoot + "/P09StaffAnimationSet.asset";
        private const string StaffLogRoot = "Logs/P09StaffMonsters";

        [Serializable] private sealed class StaffRecipe
        {
            public int[] weapons;
            public Role[] roles;
            public StaffAttack[] attacks;
            public string behaviorSource;
        }

        [Serializable] private sealed class StaffAttack
        {
            public string key;
            public string label;
            public string balanceSource;
            public string staffSource;
            public float minDistance;
            public float maxDistance;
            public int requiredLevel;
        }

        /// <summary>기존 성장표와 스태프 제작표로 남녀 몬스터를 신규 생성하고 검증 후 등록한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/P09/스태프 등급별 몬스터 제작", priority = 1105)]
        public static void BuildStaffMonsters() => BuildStaffMonstersCore(false);

        private static void BuildStaffMonstersCore(bool injectFailure)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 편집 모드에서 실행하세요.");
            Directory.CreateDirectory(StaffLogRoot);
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var staffRecipe = JsonUtility.FromJson<StaffRecipe>(File.ReadAllText(StaffRecipePath));
            if (staffRecipe.weapons.Length != recipe.tiers.Length || staffRecipe.roles.Length == 0)
                throw new InvalidOperationException("스태프 제작표와 성장 단계 수가 다릅니다.");
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            var database = Require<ActorDatabase>(DatabasePath);
            var codex = Require<MonsterCodexDatabaseSO>(CodexPath);
            var behaviorSource = Require<ActorDefinitionSO>(staffRecipe.behaviorSource);
            ValidateCombat(behaviorSource);
            var baseline = GetStaffAbilityIssues();
            File.WriteAllLines(StaffLogRoot + "/ability-before.txt", baseline);
            var created = new List<string>();
            var temporary = new List<UnityEngine.Object>();
            byte[] databaseBackup = File.ReadAllBytes(DatabasePath);
            byte[] codexBackup = File.ReadAllBytes(CodexPath);
            bool published = false;
            try
            {
                ActorAnimationMotionSet mapping = CreateStaffCombat(staffRecipe, catalog, created);
                ValidateStaffAbilityDelta(baseline);
                var entries = new List<Entry>();
                foreach (Role role in staffRecipe.roles)
                {
                    var original = Require<ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/" + role.source + "_ActorDef.asset");
                    ValidateCombat(original);
                    var source = UnityEngine.Object.Instantiate(original);
                    temporary.Add(source);
                    source.monsterProfile = UnityEngine.Object.Instantiate(original.monsterProfile);
                    temporary.Add(source.monsterProfile);
                    source.abilitySet = source.monsterProfile.abilitySet = mapping.attackAbilitySet;
                    source.behaviorData = source.monsterProfile.behaviorData = behaviorSource.EffectiveBehaviorData;
                    for (int i = 0; i < recipe.tiers.Length; i++)
                        entries.Add(CreateStaffEntry(recipe.tiers[i], staffRecipe.weapons[i], role, source, catalog));
                }
                foreach (Entry entry in entries)
                {
                    var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(entry.DefinitionPath);
                    if (definition == null)
                    {
                        if (database.Contains(entry.Id) || codex.GetEntry(entry.Id) != null
                            || File.Exists(entry.PrefabPath) || Directory.Exists(entry.Folder))
                            throw new InvalidOperationException("기존 데이터와 충돌: " + entry.Id);
                        definition = Create(entry, recipe, catalog, created);
                        WireStaffPrefab(definition, mapping);
                        var codexEntry = Clone(codex.GetEntry(behaviorSource.actorId), entry.Folder + "/Codex.asset", created);
                        codexEntry.actorId = entry.Id;
                        codexEntry.displayNameOverride = string.Empty;
                        codexEntry.descriptionOverride = string.Empty;
                        codexEntry.portrait = null;
                        Save(codexEntry);
                    }
                    ValidateDefinition(definition, entry, catalog);
                    ValidateStaffPrefab(definition, mapping);
                }
                ValidateStaffAbilityDelta(baseline);

                // 검증을 마친 신규 에셋만 공개한다. 기존 프리팹·공용 모션은 저장하지 않는다.
                published = true;
                foreach (Entry entry in entries)
                {
                    var definition = Require<ActorDefinitionSO>(entry.DefinitionPath);
                    if (!database.Contains(entry.Id)) database.AddDefinition(definition);
                    if (codex.GetEntry(entry.Id) == null)
                        codex.AddEntry(Require<MonsterCodexEntrySO>(entry.Folder + "/Codex.asset"));
                }
                Save(database);
                Save(codex);
                if (injectFailure) throw new InvalidOperationException("P09_STAFF_ROLLBACK_CHECK");
                var report = entries.Select(entry =>
                {
                    var definition = Require<ActorDefinitionSO>(entry.DefinitionPath);
                    if (database.GetDefinition(entry.Id) != definition || codex.GetEntry(entry.Id) == null)
                        throw new InvalidOperationException("DB 역참조 누락: " + entry.Id);
                    int unlocked = definition.EffectiveAbilitySet.EnumerateAll().Count(a =>
                        UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(a.variants[0], out var attack)
                        && attack.aiSelectable && attack.IsUnlockedForLevel(definition.EffectiveLevel));
                    return $"PASS {entry.Id}: Lv{definition.EffectiveLevel}, {definition.EffectiveGrade}, 스태프 {((IEditPartData)entry.Config.StaffSo).ContentId}, 공격 {unlocked}, EXP {definition.EffectiveExpReward}";
                }).ToArray();
                File.WriteAllLines(StaffLogRoot + "/connections.txt", report);
                File.WriteAllLines(StaffLogRoot + "/ability-after.txt", GetStaffAbilityIssues());
                Debug.Log($"[P09StaffMonsters] {report.Length}종 연결 검증 완료, 신규 에셋 {created.Count}개");
            }
            catch
            {
                if (published)
                {
                    File.WriteAllBytes(DatabasePath, databaseBackup);
                    File.WriteAllBytes(CodexPath, codexBackup);
                    AssetDatabase.ImportAsset(DatabasePath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.ImportAsset(CodexPath, ImportAssetOptions.ForceUpdate);
                    database.InvalidateLookup();
                    codex.InvalidateLookup();
                }
                for (int i = created.Count - 1; i >= 0; i--) AssetDatabase.DeleteAsset(created[i]);
                // 실패 뒤 빈 생성 폴더는 재시도 충돌로 취급하지 않도록 정리한다.
                foreach (string folder in created.Select(Path.GetDirectoryName).Distinct().OrderByDescending(p => p.Length))
                    if (Directory.Exists(folder) && Directory.GetFileSystemEntries(folder).Length == 0)
                        AssetDatabase.DeleteAsset(folder.Replace('\\', '/'));
                throw;
            }
            finally
            {
                foreach (var item in temporary) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static Entry CreateStaffEntry(Tier tier, int weapon, Role role, ActorDefinitionSO source, P09AssetCatalog catalog)
        {
            if (tier.level <= 0 || tier.level > source.EffectiveMonsterScaling.levelCap)
                throw new InvalidOperationException("성장 범위를 벗어난 스태프 단계입니다.");
            var config = new CharacterBuildConfig { Sex = role.sex };
            ArmorIndexPreset armor = ArmorIndexPresetUtility.Build(catalog).Single(a => a.Index == tier.armor);
            ArmorIndexPresetUtility.Apply(config.ArmorSelections, armor);
            config.StaffSo = catalog.Staves.Single(s => ((IEditPartData)s).ContentId == weapon);
            return new Entry { Tier = tier, Role = role, Source = source, Config = config };
        }

        private static ActorAnimationMotionSet CreateStaffCombat(StaffRecipe recipe, P09AssetCatalog catalog, List<string> created)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(StaffMappingPath);
            if (existing != null)
            {
                if (existing.attackAbilitySet != Require<AbilitySetSO>(StaffSetPath))
                    throw new InvalidOperationException("기존 스태프 전투 세트의 연결이 다릅니다.");
                return existing;
            }
            if (Directory.Exists(StaffRoot) && Directory.GetFiles(StaffRoot, "*.asset", SearchOption.AllDirectories).Length > 0)
                throw new InvalidOperationException("스태프 전투 폴더에 미완성 결과가 있습니다.");
            PathConfig.EnsureFolderExists(StaffRoot);
            var mapping = Clone(Require<ActorAnimationMotionSet>(P09StaffAttachment.MotionSetPath), StaffMappingPath, created);
            mapping.abilityMotions.Clear();
            Save(mapping);
            var set = ScriptableObject.CreateInstance<AbilitySetSO>();
            created.Add(StaffSetPath);
            AssetDatabase.CreateAsset(set, StaffSetPath);
            set.editorMemo = "P09 스태프 몬스터 공용. 레벨·등급 스탯은 ActorDefinition, 공격 선택은 기존 원거리 BT가 소유한다.";
            mapping.attackAbilitySet = set;
            Save(set);
            Save(mapping);
            foreach (StaffAttack entry in recipe.attacks)
            {
                var source = Require<GameplayAbilitySO>(entry.balanceSource);
                var staff = Require<GameplayAbilitySO>(entry.staffSource);
                var staffPayload = (UPlayGroundMotionAbilityPayloadSO)staff.variants[0].executionPayload;
                var sourceMotion = Require<ActorAnimationMotionSet>(P09StaffAttachment.MotionSetPath).GetAbilityMotionAsset(staffPayload.attackInfo.motionKey);
                if (sourceMotion == null || SerializationUtility.HasManagedReferencesWithMissingTypes(sourceMotion))
                    throw new InvalidOperationException("스태프 원본 모션/이벤트 누락: " + entry.key);
                var ability = Clone(source, StaffRoot + "/GA_P09Staff_" + entry.key + ".asset", created);
                ability.abilityId = "Monster.P09Staff." + entry.key;
                ability.presentation.displayName = entry.label;
                ability.presentation.description = string.Empty;
                ability.presentation.icon = ((WeaponEditPartData)catalog.Staves.Single(s =>
                    ((IEditPartData)s).ContentId == recipe.weapons[0])).Icon;
                ability.editorMemo = "수치 기준: " + source.abilityId + "; 모션 기준: " + staff.abilityId;
                ability.activation.minDistance = entry.minDistance;
                ability.activation.maxDistance = entry.maxDistance;
                ability.activation.groundCondition = AbilityGroundCondition.Grounded;
                // 후속 에셋 생성의 동기 Import 전에 변경을 확정해 원본 식별자가 다시 로드되지 않게 한다.
                Save(ability);
                var payload = Clone((UPlayGroundMotionAbilityPayloadSO)source.variants[0].executionPayload,
                    StaffRoot + "/Payload_P09Staff_" + entry.key + ".asset", created);
                ability.variants[0].executionPayload = payload;
                var attack = payload.attackInfo;
                attack.motionKey = new MotionKey("P09Staff." + entry.key);
                attack.aiSelectable = true;
                attack.requiredLevel = entry.requiredLevel;
                var phase = attack.baseInfo.hitPhases[0];
                attack.baseInfo.hitPhases = new() { phase };
                phase.projectileDefinition = staffPayload.attackInfo.baseInfo.hitPhases[0].projectileDefinition;
                attack.baseInfo.attackType = phase.projectileDefinition != null ? AttackType.Ranged : AttackType.Melee;
                phase.reactionProfile = JsonUtility.FromJson<UPlayGround.Data.AttackReactionProfile>(
                    JsonUtility.ToJson(staffPayload.attackInfo.baseInfo.hitPhases[0].reactionProfile));
                Save(payload);
                Save(ability);
                var motion = Clone(sourceMotion, StaffRoot + "/Motion_P09Staff_" + entry.key + ".asset", created);
                foreach (var spawn in motion.motionSet.motions.SelectMany(m => m.events)
                    .Concat(motion.motionSet.globalEvents).OfType<SpawnProjectileEvent>())
                    spawn.targetMode = ProjectileTargetMode.EnemySkillTarget;
                mapping.SetAbilityMotionAsset(attack.motionKey, motion);
                set.additionalAbilities.Add(ability);
                ability.balance.expectedDamage = phase.damage;
                ability.balance.expectedDuration = motion.motionSet.TotalDuration;
                Save(payload);
                Save(ability);
                Save(motion);
                Save(mapping);
                Save(set);
            }
            var sourceSet = Require<ActorDefinitionSO>(recipe.behaviorSource).EffectiveAbilitySet;
            set.additionalAbilities.AddRange(sourceSet.EnumerateAll().Where(a => a.triggers.Count > 0
                && a.triggers.All(t => t.mode == AbilityTriggerActivationMode.Request)));
            set.tagTriggerMigrationVersion = sourceSet.tagTriggerMigrationVersion;
            set.RebuildRuntimeIndex();
            Save(set);
            Save(mapping);
            return mapping;
        }

        private static void WireStaffPrefab(ActorDefinitionSO definition, ActorAnimationMotionSet mapping)
        {
            string path = AssetDatabase.GetAssetPath(definition.prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ReflectionUtil.SetField(root.GetComponentInChildren<ActorAnimator>(true), "_motionSet", mapping);
                ReflectionUtil.SetField(root.GetComponent<EnemyCombat>(), "_abilitySet", mapping.attackAbilitySet);
                ReflectionUtil.SetField(root.GetComponent<EnemyAIController>(), "_behaviorData", definition.EffectiveBehaviorData);
                var hand = root.GetComponentInChildren<Animator>(true).GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null) throw new InvalidOperationException("오른손 본 누락: " + definition.actorId);
                if (hand.Find("StaffMagicMuzzle") == null)
                    new GameObject("StaffMagicMuzzle").transform.SetParent(hand, false);
                definition.prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) throw new InvalidOperationException("스태프 프리팹 저장 실패: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Save(definition);
        }

        private static void ValidateStaffPrefab(ActorDefinitionSO definition, ActorAnimationMotionSet mapping)
        {
            var root = definition.prefab;
            if (root.GetComponentInChildren<ActorAnimator>(true).MotionSet != mapping
                || definition.monsterProfile.abilitySet != definition.EffectiveAbilitySet)
                throw new InvalidOperationException("스태프 MotionSet/프로필 불일치: " + definition.actorId);
            var staff = root.GetComponentsInChildren<WeaponSocketBinding>(true).Single(b => b.weaponType == WeaponType.Staff);
            var hand = root.GetComponentInChildren<Animator>(true).GetBoneTransform(HumanBodyBones.RightHand);
            if (hand.Find("StaffMagicMuzzle") == null || staff.equipPosition != EquipPosition.RightHand
                || !staff.Constraint.GetSource(0).sourceTransform.IsChildOf(hand)
                || staff.Constraint.GetSource(0).weight != 1f || staff.Constraint.GetSource(1).weight != 0f)
                throw new InvalidOperationException("스태프 손/발사 소켓 오류: " + definition.actorId);
            foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll())
            {
                if (ability.variants[0].executionPayload is not UPlayGroundMotionAbilityPayloadSO payload
                    || !payload.attackInfo.aiSelectable) continue;
                if (!UPlayGround.AI.BehaviorTree.EnemyAbilityTriggerTags.TryResolveAttackTrigger(
                    definition.EffectiveAbilitySet, payload.attackInfo.attackCategory, out _, out _, out _))
                    throw new InvalidOperationException("BT 공격 트리거 라우터 누락: " + ability.abilityId);
                var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                if (!motion.motionSet.layers.Any(l => l.enabled && l.channelId == P09StaffGripAuthoring.Channel))
                    throw new InvalidOperationException("손가락 레이어 누락: " + motion.name);
                var events = motion.motionSet.motions.SelectMany(m => m.events).Concat(motion.motionSet.globalEvents).ToArray();
                foreach (var evt in events)
                {
                    if (evt is SpawnProjectileEvent projectile && projectile.targetMode != ProjectileTargetMode.EnemySkillTarget)
                        throw new InvalidOperationException("몬스터 투사체 조준 대상 오류: " + ability.abilityId);
                    int phase = evt is SpawnProjectileEvent spawn ? spawn.hitPhaseIndex
                        : evt is BeginCollisionEvent collision ? collision.hitPhaseIndex : -1;
                    if (phase >= payload.attackInfo.baseInfo.hitPhases.Count)
                        throw new InvalidOperationException("HitPhase 범위 오류: " + ability.abilityId);
                }
                if (!events.Any(e => e is SpawnProjectileEvent || e is BeginCollisionEvent))
                    throw new InvalidOperationException("공격 판정 이벤트 누락: " + ability.abilityId);
            }
        }

        private static string[] GetStaffAbilityIssues() => AbilityDataValidator.ValidateAll()
            .Where(i => i.Severity != AbilityValidationSeverity.Info)
            .Select(i => $"{i.Severity}: {AssetDatabase.GetAssetPath(i.Context)}: {i.Message}")
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();

        private static void ValidateStaffAbilityDelta(string[] baseline)
        {
            var current = GetStaffAbilityIssues();
            var added = current.Except(baseline).Union(current.Where(s => s.Contains(StaffRoot + "/"))).ToArray();
            File.WriteAllLines(StaffLogRoot + "/ability-new.txt", added);
            if (added.Length > 0)
                throw new InvalidOperationException("신규 GAS 경고/오류: " + string.Join("\n", added));
        }

        /// <summary>DB 공개 직후 실패를 주입해 기존 DB와 신규 에셋의 롤백을 검사한다.</summary>
        public static void VerifyStaffRollback()
        {
            if (File.Exists(StaffSetPath))
                throw new InvalidOperationException("이미 제작된 스태프 데이터가 있으므로 실패 주입 검사를 실행하지 않습니다.");
            byte[] before = File.ReadAllBytes(DatabasePath);
            byte[] codexBefore = File.ReadAllBytes(CodexPath);
            try { BuildStaffMonstersCore(true); }
            catch (InvalidOperationException ex) when (ex.Message == "P09_STAFF_ROLLBACK_CHECK")
            {
                if (!before.SequenceEqual(File.ReadAllBytes(DatabasePath)) || !codexBefore.SequenceEqual(File.ReadAllBytes(CodexPath))
                    || File.Exists(StaffSetPath))
                    throw new InvalidOperationException("스태프 생성 롤백에 실패했습니다.");
                File.WriteAllText(StaffLogRoot + "/rollback.txt", "PASS: DB 공개 후 실패 주입, DB 원본 바이트 복원 및 신규 전투 에셋 제거");
                return;
            }
            throw new InvalidOperationException("실패 주입이 실행되지 않았습니다.");
        }

        /// <summary>저장된 결과의 재실행 보존을 검사한다. 실패 주입 뒤에는 별도 Editor 프로세스에서 실행한다.</summary>
        public static void VerifyStaffIdempotence()
        {
            BuildStaffMonsters();
            var paths = AssetDatabase.FindAssets("", new[] { StaffRoot, DataRoot + "/Generated" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).ToArray();
            var before = paths.ToDictionary(p => p, File.ReadAllBytes);
            BuildStaffMonsters();
            foreach (string path in paths)
                if (!before[path].SequenceEqual(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("재실행 시 기존 결과 변경: " + path);
            File.WriteAllText(StaffLogRoot + "/idempotence.txt", "PASS: 재실행 후 기존 생성 데이터 바이트 보존");
        }
    }
}
