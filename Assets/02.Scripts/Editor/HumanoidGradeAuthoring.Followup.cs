using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    public static partial class HumanoidGradeAuthoring
    {
        /// <summary>기존 등급 Set과 GUID를 보존하고 제작표의 후속 공격만 단계 저장·복구한다.</summary>
        public static string ApplyAdditions(string weaponKey, bool preview = true, int failurePoint = 0)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode가 필요합니다.");
            var weapon = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath)).weapons.Single(w => w.key == weaponKey);
            string root = OutputRoot + "/" + weaponKey;
            var sets = Enumerable.Range(0, 3).Select(g => Require<AbilitySetSO>($"{root}/AbilitySet_{weaponKey}_{(MonsterActorGrade)g}.asset")).ToArray();
            var staged = new Dictionary<string, ScriptableObject>();
            var additions = new List<(int grade, GameplayAbilitySO ability)>();
            foreach (var entry in weapon.attacks.Where(a => a.followup))
            {
                var source = Require<GameplayAbilitySO>(entry.source);
                string name = $"GA_{weaponKey}_{(MonsterActorGrade)entry.grade}_{source.name}";
                string path = root + "/" + name + ".asset";
                if (File.Exists(path)) throw new InvalidOperationException("이미 적용된 후속 공격: " + path);
                if (entry.replace || entry.level != 30 || !NeedsFork(entry)) throw new InvalidOperationException("후속 공격은 Lv30 Add만 지원합니다.");
                var ability = UnityEngine.Object.Instantiate(source);
                var payload = UnityEngine.Object.Instantiate((UPlayGroundMotionAbilityPayloadSO)source.variants.Single().executionPayload);
                ability.name = name;
                payload.name = "Payload_" + name;
                ability.abilityId = $"Monster.Grade.{weaponKey}.{(MonsterActorGrade)entry.grade}.{source.abilityId}";
                ability.variants[0].executionPayload = payload;
                ability.editorMemo = source.abilityId + "에서 안전 Fork. 후속 제작표: " + RecipePath;
                if (!string.IsNullOrEmpty(weapon.iconPath)) ability.presentation.icon = Require<Sprite>(weapon.iconPath);
                var info = payload.attackInfo;
                info.requiredLevel = entry.level;
                info.aiRoles = Enum.Parse<AbilityAIRole>(entry.roles);
                info.attackCategory = Enum.Parse<AbilityAttackCategory>(entry.category);
                info.useDangerRing = true;
                if (entry.damage?.Length > 0)
                {
                    if (entry.damage.Length != info.baseInfo.hitPhases.Count) throw new InvalidOperationException("Phase 수 불일치");
                    for (int i = 0; i < entry.damage.Length; i++) info.baseInfo.hitPhases[i].damage = entry.damage[i];
                }
                if (entry.latePhase)
                {
                    info.conditionGroup.conditionOperator = ConditionOperator.And;
                    info.conditionGroup.conditions.Add(new SkillCondition { type = ConditionType.SelfHealthBased, maxHealthPercent = entry.maxHealth });
                }
                ability.cooldown.durationSeconds = entry.cooldown;
                ability.cooldown.cooldownGroupId = string.IsNullOrEmpty(entry.cooldownGroup) ? ability.abilityId : entry.cooldownGroup;
                ability.balance.expectedDamage = info.baseInfo.hitPhases.Sum(h => h.damage);
                foreach (string definition in weapon.definitions) ValidateMotion(Require<Data.Actor.ActorDefinitionSO>(definition), ability, entry);
                staged.Add(root + "/" + payload.name + ".asset", payload);
                staged.Add(path, ability);
                additions.Add((entry.grade, ability));
            }
            if (preview)
            {
                string result = string.Join("\n", additions.Select(a => a.ability.name));
                foreach (var asset in staged.Values) UnityEngine.Object.DestroyImmediate(asset);
                return result;
            }
            // 원래 응징도 같은 그룹을 써야 추가 공격이 기존 쿨다운을 우회하지 않는다.
            var cooldownTargets = weapon.attacks.Where(a => !a.followup && !string.IsNullOrEmpty(a.cooldownGroup))
                .Select(a => new { entry = a, ability = sets[a.grade].EnumerateAll().Single(x => x.name == $"GA_{weaponKey}_{(MonsterActorGrade)a.grade}_{Require<GameplayAbilitySO>(a.source).name}") }).ToArray();
            var targets = sets.Cast<UnityEngine.Object>().Concat(cooldownTargets.Select(a => a.ability)).Distinct().ToArray();
            var backups = targets.Select(AssetDatabase.GetAssetPath).SelectMany(p => new[] { p, p + ".meta" }).ToDictionary(p => p, File.ReadAllBytes);
            string backupRoot = LogRoot + "/Remaining/Backup/" + weaponKey + "/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            foreach (var pair in backups)
            {
                string path = backupRoot + "/" + pair.Key;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, pair.Value);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(targets, "Humanoid 후속 공격");
            var created = new List<string>();
            try
            {
                foreach (var pair in staged)
                {
                    if (File.Exists(pair.Key)) throw new InvalidOperationException("저장 경로 충돌: " + pair.Key);
                    AssetDatabase.CreateAsset(pair.Value, pair.Key);
                    created.Add(pair.Key);
                    AssetDatabase.SaveAssetIfDirty(pair.Value);
                }
                if (failurePoint == 1) throw new InvalidOperationException("실패 주입: 신규 저장 후");
                foreach (var item in additions) { sets[item.grade].additionalAbilities.Add(item.ability); EditorUtility.SetDirty(sets[item.grade]); }
                foreach (var item in cooldownTargets)
                {
                    item.ability.cooldown.cooldownGroupId = item.entry.cooldownGroup;
                    item.ability.cooldown.durationSeconds = item.entry.cooldown;
                    EditorUtility.SetDirty(item.ability);
                }
                foreach (var set in sets) { set.RebuildRuntimeIndex(); ValidateSet(set, set != sets[0]); }
                if (failurePoint == 2) throw new InvalidOperationException("실패 주입: Set 추가 후");
                foreach (var target in targets) AssetDatabase.SaveAssetIfDirty(target);
                Undo.CollapseUndoOperations(group);
                return $"{weaponKey}: 후속 공격 {additions.Count}개 저장";
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (string path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                foreach (var pair in backups) File.WriteAllBytes(pair.Key, pair.Value);
                foreach (string path in backups.Keys.Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                foreach (var set in sets) set.RebuildRuntimeIndex();
                if (backups.Any(p => !File.ReadAllBytes(p.Key).SequenceEqual(p.Value))) throw new InvalidOperationException("후속 공격 복구 바이트 불일치");
                File.WriteAllText(LogRoot + $"/Remaining/{weaponKey}.rollback{failurePoint}.txt", "에셋/meta 바이트 일치. 신규 에셋 제거. Set 인덱스 복구.");
                throw;
            }
        }
    }
}
