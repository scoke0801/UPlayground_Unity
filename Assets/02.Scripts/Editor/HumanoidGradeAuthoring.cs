using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Editor
{
    /// <summary>명시적 제작표로 공유 원본을 보존하면서 등급 Set과 레벨 해금을 저작한다.</summary>
    public static partial class HumanoidGradeAuthoring
    {
        public const string RecipePath = "Assets/10.Datas/Ability/Actor/HumanoidGradeVariation.json";
        public const string OutputRoot = "Assets/10.Datas/Ability/Actor/GradeVariation";
        private const string LogRoot = "Logs/HumanoidGrade";

        [Serializable] public sealed class Recipe { public Weapon[] weapons; }
        [Serializable] public sealed class Weapon
        {
            public string key;
            public string sourceSet;
            public string iconPath;
            public Attack[] attacks;
            public string[] definitions;
        }
        [Serializable] public sealed class Attack
        {
            public string source;
            public int grade;
            public int level = 1;
            public string roles;
            public string category;
            public float[] damage;
            public bool replace;
            public bool latePhase;
            public float maxHealth = 0.5f;
            public float minimumStartup;
            public float minimumRecovery;
            public float cooldown;
            public string cooldownGroup;
            public bool followup;
        }

        /// <summary>파생 Set의 적용 예정 내역을 파일로 확인한다. 적용은 명시적 무기별 API로 수행한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/전투/Humanoid 등급 Preview")]
        public static void Preview()
        {
            foreach (var weapon in JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath)).weapons)
                Apply(weapon.key);
        }

        /// <summary>대상 무기를 Preview 또는 단일 Undo·백업 경로로 적용한다.</summary>
        public static string Apply(string weaponKey, bool preview = true, int failurePoint = 0)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode가 필요합니다.");
            Weapon weapon = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath)).weapons.Single(w => w.key == weaponKey);
            var source = Require<AbilitySetSO>(weapon.sourceSet);
            var definitions = weapon.definitions.Select(Require<ActorDefinitionSO>).ToArray();
            var staged = new Dictionary<string, ScriptableObject>();
            var sets = new AbilitySetSO[3];
            var report = new List<string>();
            string root = OutputRoot + "/" + weapon.key;
            var previous = new Dictionary<string, GameplayAbilitySO>();
            for (int grade = 0; grade < sets.Length; grade++)
            {
                var set = ScriptableObject.CreateInstance<AbilitySetSO>();
                set.name = $"AbilitySet_{weapon.key}_{(MonsterActorGrade)grade}";
                set.baseSet = grade == 0 ? source : sets[grade - 1];
                sets[grade] = set;
                staged.Add(root + "/" + set.name + ".asset", set);
                if (grade == 0)
                {
                    var keep = weapon.attacks.Where(a => a.grade == 0 && !NeedsFork(a)).Select(a => Require<GameplayAbilitySO>(a.source)).ToHashSet();
                    foreach (var ability in source.EnumerateAll())
                    {
                        if (keep.Contains(ability) || !IsAttack(ability)) continue;
                        set.abilityOverrides.Add(new AbilitySetSO.AbilityOverrideEntry { sourceAbility = ability, operation = AbilitySetOverrideOperation.Remove });
                    }
                }
                foreach (Attack entry in weapon.attacks.Where(a => a.grade == grade))
                {
                    var original = Require<GameplayAbilitySO>(entry.source);
                    GameplayAbilitySO ability = original;
                    if (NeedsFork(entry))
                    {
                        if (original.variants.Count != 1 || original.variants[0].executionPayload is not UPlayGroundMotionAbilityPayloadSO payload)
                            throw new InvalidOperationException($"단일 Payload 원본 필요: {original.name}");
                        ability = UnityEngine.Object.Instantiate(original);
                        var fork = UnityEngine.Object.Instantiate(payload);
                        ability.name = $"GA_{weapon.key}_{(MonsterActorGrade)grade}_{original.name}";
                        fork.name = "Payload_" + ability.name;
                        ability.abilityId = $"Monster.Grade.{weapon.key}.{(MonsterActorGrade)grade}.{original.abilityId}";
                        ability.variants[0].executionPayload = fork;
                        ability.editorMemo = $"{original.abilityId}에서 안전 Fork. MotionKey 유지. 등급/레벨 제작표: {RecipePath}";
                        if (!string.IsNullOrEmpty(weapon.iconPath)) ability.presentation.icon = Require<Sprite>(weapon.iconPath);
                        var info = fork.attackInfo;
                        info.requiredLevel = entry.level;
                        if (!string.IsNullOrEmpty(entry.roles)) info.aiRoles = Enum.Parse<AbilityAIRole>(entry.roles);
                        if (!string.IsNullOrEmpty(entry.category)) info.attackCategory = Enum.Parse<AbilityAttackCategory>(entry.category);
                        if (entry.damage?.Length > 0)
                        {
                            if (entry.damage.Length != info.baseInfo.hitPhases.Count) throw new InvalidOperationException("Phase 수 변경은 별도 모션 검수 필요");
                            for (int i = 0; i < entry.damage.Length; i++) info.baseInfo.hitPhases[i].damage = entry.damage[i];
                        }
                        if (entry.minimumStartup > 0f) info.useDangerRing = true;
                        if ((info.aiRoles & (AbilityAIRole.Signature | AbilityAIRole.Punish | AbilityAIRole.Finisher)) != 0) info.useDangerRing = true;
                        if (entry.cooldown > 0f)
                        {
                            ability.cooldown.durationSeconds = entry.cooldown;
                            ability.cooldown.cooldownGroupId = ability.abilityId;
                        }
                        if (!string.IsNullOrEmpty(entry.cooldownGroup)) ability.cooldown.cooldownGroupId = entry.cooldownGroup;
                        if (entry.latePhase)
                        {
                            info.conditionGroup.conditionOperator = ConditionOperator.And;
                            info.conditionGroup.conditions.Add(new SkillCondition { type = ConditionType.SelfHealthBased, maxHealthPercent = entry.maxHealth });
                        }
                        ability.balance.expectedDamage = info.baseInfo.hitPhases.Sum(h => h.damage);
                        staged.Add(root + "/" + ability.name + ".asset", ability);
                        staged.Add(root + "/" + fork.name + ".asset", fork);
                    }
                    if (entry.replace)
                    {
                        if (!previous.TryGetValue(entry.source, out var inherited) || !set.baseSet.EnumerateAll().Contains(inherited))
                            throw new InvalidOperationException($"Replace 원본이 유효 Set에 없음: {entry.source}");
                        if (Info(ability).requiredLevel != 1) throw new InvalidOperationException("Replace는 Lv1이어야 합니다.");
                        set.abilityOverrides.Add(new AbilitySetSO.AbilityOverrideEntry { sourceAbility = inherited, replacementAbility = ability });
                    }
                    else if (grade > 0 || NeedsFork(entry)) set.additionalAbilities.Add(ability);
                    previous[entry.source] = ability;
                    foreach (var definition in definitions)
                    {
                        ValidateMotion(definition, ability, entry);
                    }
                    report.Add($"{weapon.key}/{(MonsterActorGrade)grade}: {ability.name}, Lv{Info(ability).requiredLevel}, 피해 {string.Join("+", Info(ability).baseInfo.hitPhases.Select(h => h.damage))}");
                }
                ValidateSet(set, grade > 0);
            }
            Directory.CreateDirectory(LogRoot);
            File.WriteAllLines(LogRoot + "/" + weapon.key + ".preview.txt", report);
            if (preview)
            {
                foreach (var asset in staged.Values) UnityEngine.Object.DestroyImmediate(asset);
                return string.Join("\n", report);
            }
            foreach (string path in staged.Keys)
                if (File.Exists(path)) throw new InvalidOperationException($"이미 적용됐거나 경로 충돌: {path}");
            var targets = definitions.SelectMany(d => new UnityEngine.Object[] { d, d.monsterProfile }).Distinct().ToArray();
            if (targets.Any(t => t == null)) throw new InvalidOperationException("Profile 누락");
            var backups = targets.Select(AssetDatabase.GetAssetPath).SelectMany(p => new[] { p, p + ".meta" }).Distinct().ToDictionary(p => p, File.ReadAllBytes);
            string backupRoot = LogRoot + "/Backup/" + weapon.key + "/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            foreach (var pair in backups)
            {
                string destination = backupRoot + "/" + pair.Key;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, pair.Value);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Humanoid 등급 공격 " + weapon.key);
            var created = new List<string>();
            try
            {
                EnsureFolder(root);
                // 참조 대상인 Payload→Ability→Set 순으로 영속화해야 임시 instance ID가 fileID 0으로 저장되지 않는다.
                foreach (var pair in staged.OrderBy(p => p.Value is UPlayGroundMotionAbilityPayloadSO ? 0 : p.Value is GameplayAbilitySO ? 1 : 2))
                {
                    created.Add(pair.Key);
                    AssetDatabase.CreateAsset(pair.Value, pair.Key);
                }
                foreach (var asset in staged.Values) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                foreach (var set in sets) set.RebuildRuntimeIndex();
                if (failurePoint == 1) throw new InvalidOperationException("실패 주입: 신규 저장 후");
                Undo.RegisterCompleteObjectUndo(targets, "등급별 공격 연결");
                foreach (var definition in definitions)
                {
                    int grade = definition.EffectiveGrade == MonsterActorGrade.Boss ? 2 : definition.EffectiveGrade == MonsterActorGrade.Elite ? 1 : 0;
                    definition.abilitySet = sets[grade];
                    definition.monsterProfile.abilitySet = sets[grade];
                    EditorUtility.SetDirty(definition);
                    EditorUtility.SetDirty(definition.monsterProfile);
                }
                if (failurePoint == 2) throw new InvalidOperationException("실패 주입: 참조 변경 후");
                foreach (var definition in definitions)
                {
                    if (definition.EffectiveAbilitySet != definition.monsterProfile.abilitySet)
                        throw new InvalidOperationException("Profile 유효 Set 불일치");
                    foreach (var ability in definition.EffectiveAbilitySet.EnumerateAll().Where(IsAttack)) ValidateMotion(definition, ability, null);
                }
                foreach (var target in targets) AssetDatabase.SaveAssetIfDirty(target);
                Undo.CollapseUndoOperations(group);
                File.WriteAllLines(LogRoot + "/" + weapon.key + ".applied.txt", report);
                return $"{weapon.key}: {definitions.Length}개 연결, 신규 {created.Count}개";
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (string path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                foreach (var pair in backups) File.WriteAllBytes(pair.Key, pair.Value);
                foreach (string path in backups.Keys.Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                foreach (var pair in backups)
                    if (!File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)) throw new InvalidOperationException($"복구 바이트 불일치: {pair.Key}");
                File.WriteAllText(LogRoot + $"/{weapon.key}.rollback{failurePoint}.txt", "원본 파일/meta 바이트 일치. 생성 에셋 제거. Undo 그룹 전체 복구.");
                throw;
            }
        }

        private static bool NeedsFork(Attack attack) => attack.level > 1 || attack.replace || attack.latePhase || !string.IsNullOrEmpty(attack.roles) || !string.IsNullOrEmpty(attack.category) || attack.damage?.Length > 0;
        private static bool IsAttack(GameplayAbilitySO ability) => ability.variants.Any(v => v.executionPayload is UPlayGroundMotionAbilityPayloadSO p && p.attackInfo.aiSelectable);
        private static AbilityAttackInfo Info(GameplayAbilitySO ability) => ((UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload).attackInfo;

        /// <summary>실제 선택 레벨마다 필수 역할의 공백과 상위 전용기의 저레벨 Replace를 검사한다.</summary>
        public static void ValidateSet(AbilitySetSO set, bool signatureRequired)
        {
            var roles = new[] { AbilityAIRole.Opener, AbilityAIRole.Punish, AbilityAIRole.Counter, AbilityAIRole.GapCloser, AbilityAIRole.Finisher };
            foreach (int level in new[] { 1, 9, 10, 19, 20, 29, 30, 40, 50 })
            foreach (var role in signatureRequired ? roles.Append(AbilityAIRole.Signature) : roles)
            {
                if (!set.EnumerateAll().Where(IsAttack).Select(Info).Any(i => i.IsUnlockedForLevel(level) && (i.aiRoles & role) != 0 && !i.isMotionContentPending))
                    throw new InvalidOperationException($"역할 공백: {set.name}/Lv{level}/{role}");
            }
        }

        private static void ValidateMotion(ActorDefinitionSO definition, GameplayAbilitySO ability, Attack entry)
        {
            var animator = definition.prefab == null ? null : definition.prefab.GetComponentInChildren<ActorAnimator>(true);
            var info = Info(ability);
            var motion = animator?.MotionSet?.GetAbilityMotionAsset(info.motionKey);
            if (motion == null || SerializationUtility.HasManagedReferencesWithMissingTypes(motion))
                throw new InvalidOperationException($"소유 모션 누락: {definition.actorId}/{ability.name}");
            var events = motion.motionSet.globalEvents.Concat(motion.motionSet.motions.SelectMany(m => m.events)).ToArray();
            if (events.Any(e => e == null)) throw new InvalidOperationException("MotionEvent 누락");
            var collisions = events.OfType<BeginCollisionEvent>().ToArray();
            var projectiles = events.OfType<SpawnProjectileEvent>().ToArray();
            if (collisions.Length + projectiles.Length == 0) throw new InvalidOperationException($"판정 이벤트 없음: {motion.name}");
            foreach (int index in collisions.Select(e => e.hitPhaseIndex).Concat(projectiles.Select(e => e.hitPhaseIndex)))
                if (index < 0 || index >= info.baseInfo.hitPhases.Count) throw new InvalidOperationException($"Phase 범위 오류: {motion.name}/{index}");
            var groups = definition.prefab.GetComponentsInChildren<CombatHitbox>(true).Select(h => h.GroupId).ToHashSet();
            foreach (var collision in collisions.Where(c => c.collisionSource == CollisionSourceType.AttachedHitboxGroup))
            {
                string group = string.IsNullOrEmpty(collision.hitboxGroupId) ? info.baseInfo.GetHitPhase(collision.hitPhaseIndex).hitboxGroupId : collision.hitboxGroupId;
                if (!groups.Contains(string.IsNullOrEmpty(group) ? CombatHitbox.DefaultGroupId : group)) throw new InvalidOperationException($"HitBox 그룹 누락: {definition.actorId}/{group}");
            }
            if (entry == null || entry.minimumStartup <= 0f) return;
            var timed = new List<(float start, float end)>();
            void Collect(IEnumerable<MotionEventBase> sequence, float offset)
            {
                foreach (var e in sequence.Where(e => e is BeginCollisionEvent || e is SpawnProjectileEvent)) timed.Add((offset + e.startTime, offset + e.endTime));
            }
            Collect(motion.motionSet.globalEvents, 0f);
            float time = 0f;
            foreach (var part in motion.motionSet.motions) { Collect(part.events, time); time += part.Duration; }
            float startup = timed.Min(t => t.start);
            float recovery = motion.motionSet.TotalDuration - timed.Max(t => t.end);
            if (startup < entry.minimumStartup || recovery < entry.minimumRecovery)
                throw new InvalidOperationException($"전조/후딜 부족: {definition.actorId}/{ability.name}: {startup:F3}/{recovery:F3}");
        }

        private static T Require<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("에셋 누락: " + path);
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
