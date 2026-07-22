#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Combat
{
    public enum CombatValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct CombatValidationIssue
    {
        public readonly CombatValidationSeverity Severity;
        public readonly string AssetPath;
        public readonly string Context;
        public readonly string Message;

        public CombatValidationIssue(
            CombatValidationSeverity severity,
            string assetPath,
            string context,
            string message)
        {
            Severity = severity;
            AssetPath = assetPath;
            Context = context;
            Message = message;
        }
    }

    public static class CombatDataValidator
    {
        public static List<CombatValidationIssue> ValidateAll()
        {
            var issues = new List<CombatValidationIssue>();
            ValidateCombatBindings(issues);
            ValidateAiSelectableAbilities(issues);
            ValidateMotionSetMatching(issues);
            ValidateCombatPolicyData(issues);
            return issues;
        }

        private static void ValidateCombatBindings(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AbilitySetSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(path);
                PlayerCombatAbilityDataView view = PlayerCombatAbilityDataView.Build(asset);
                if (view == null)
                    continue;

                ValidatePlayerList(issues, path, "liteComboAttackList", view.liteComboAttackList);
                ValidatePlayerList(issues, path, "heavyComboAttackList", view.heavyComboAttackList);
                ValidatePlayerList(issues, path, "jumpAttackList", view.jumpAttackList);
                ValidatePlayerList(issues, path, "dashAttackList", view.dashAttackList);
                ValidatePlayerList(issues, path, "skillAttackList", view.skillAttackList);
                ValidatePlayerAttack(issues, path, "counterAttack", view.counterAttack);
                ValidatePlayerAttack(issues, path, "entryAttack", view.entryAttack);
                ValidatePlayerAttack(issues, path, "swapSpecialAttack", view.swapSpecialAttack);
                ValidatePlayerAttack(issues, path, "swapEvadeCounterAttack", view.swapEvadeCounterAttack);
                ValidatePlayerAttack(issues, path, "parryCounterAttack", view.parryCounterAttack);
            }
        }

        private static void ValidateAiSelectableAbilities(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AbilitySetSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(path);
                if (asset == null)
                    continue;

                var entries = AbilityAttackEditorUtility.Collect(asset, true);
                if (entries.Count == 0)
                    continue;

                for (int i = 0; i < entries.Count; i++)
                    ValidateAiSelectableAttack(
                        issues,
                        path,
                        entries[i].Ability.name,
                        entries[i].AttackInfo);
            }
        }

        private static void ValidatePlayerList(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            List<AbilityAttackInfo> attacks)
        {
            if (attacks == null)
                return;

            for (int i = 0; i < attacks.Count; i++)
                ValidatePlayerAttack(issues, path, $"{context}[{i}]", attacks[i]);
        }

        private static void ValidatePlayerAttack(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AbilityAttackInfo attack)
        {
            if (attack == null)
                return;

            ValidateAttackInfoBase(issues, path, context, attack.baseInfo, requireMeleeHitPhase: true);
        }

        private static void ValidateAiSelectableAttack(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AbilityAttackInfo attack)
        {
            if (attack == null)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "AbilityAttackInfo가 null입니다.");
                return;
            }

            ValidateAttackInfoBase(
                issues,
                path,
                context,
                attack.baseInfo,
                attack.skillType == SkillType.Attack);

            if (attack.selectionWeight <= 0f)
                AddIssue(issues, CombatValidationSeverity.Warning, path, context, "selectionWeight가 0 이하라 선택되지 않을 수 있습니다.");
        }

        private static void ValidateAttackInfoBase(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AttackInfoBase baseInfo,
            bool requireMeleeHitPhase)
        {
            if (baseInfo == null)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "baseInfo가 null입니다.");
                return;
            }

            if (baseInfo.motionRef == null || !baseInfo.motionRef.HasAnyMotion)
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "실행 가능한 MotionReference가 없습니다.");

            if (baseInfo.hitPhases == null || baseInfo.hitPhases.Count == 0)
            {
                if (requireMeleeHitPhase)
                    AddIssue(issues, CombatValidationSeverity.Error, path, context, "hitPhases가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < baseInfo.hitPhases.Count; i++)
            {
                HitPhaseData phase = baseInfo.hitPhases[i];
                if (phase == null)
                {
                    AddIssue(issues, CombatValidationSeverity.Error, path, $"{context}.hitPhases[{i}]", "HitPhaseData가 null입니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(phase.hitboxGroupId))
                {
                    AddIssue(
                        issues,
                        CombatValidationSeverity.Error,
                        path,
                        $"{context}.hitPhases[{i}]",
                        "필수 hitboxGroupId가 비어 있습니다.");
                }
                if (phase.damage < 0f)
                    AddIssue(issues, CombatValidationSeverity.Error, path, $"{context}.hitPhases[{i}]", "damage가 음수입니다.");
            }
        }

        // ---------------------------------------------------------------------
        // MotionSet 매칭 검증 (P0)
        //  - Enemy: ActorDefinitionSO → prefab의 MotionSet + attackData 바인딩이 결정적이라 전체 룰셋 적용.
        //  - Player: CharacterModelData를 가진 프리팹에서 attackData + MotionSet을 추출 (additive, 부분 룰셋).
        // ---------------------------------------------------------------------
        private static void ValidateMotionSetMatching(List<CombatValidationIssue> issues)
        {
            ValidateEnemyMotionSetMatching(issues);
            ValidatePlayerMotionSetMatching(issues);
        }

        private static void ValidateEnemyMotionSetMatching(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (actor == null || actor.EffectiveAbilitySet == null)
                    continue;

                ActorAnimationMotionSet motionSet = ResolveMotionSet(actor.prefab);
                if (motionSet == null)
                {
                    AddIssue(issues, CombatValidationSeverity.Warning, path, "prefab",
                        "AbilitySet이 있으나 prefab에서 ActorAnimationMotionSet을 찾을 수 없어 MotionSet 매칭 검증을 건너뜁니다.");
                    continue;
                }

                var entries = AbilityAttackEditorUtility.Collect(
                    actor.EffectiveAbilitySet,
                    true);
                for (int i = 0; i < entries.Count; i++)
                {
                    AbilityAttackInfo skill = entries[i].AttackInfo;
                    if (skill?.baseInfo == null)
                        continue;
                    ValidateAttackMotionSet(issues, path, $"skills[{i}]", skill.baseInfo, motionSet, skill);
                }
            }
        }

        private static void ValidatePlayerMotionSetMatching(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var modelData = prefab.GetComponentInChildren<CharacterModelData>(true);
                if (modelData == null || modelData.abilitySet == null)
                    continue;

                ActorAnimationMotionSet motionSet = ResolveMotionSet(prefab);
                if (motionSet == null)
                    continue;

                PlayerCombatAbilityDataView data =
                    PlayerCombatAbilityDataView.Build(modelData.abilitySet);
                ValidatePlayerListMotionSet(issues, path, "liteComboAttackList", data.liteComboAttackList, motionSet);
                ValidatePlayerListMotionSet(issues, path, "heavyComboAttackList", data.heavyComboAttackList, motionSet);
                ValidatePlayerListMotionSet(issues, path, "jumpAttackList", data.jumpAttackList, motionSet);
                ValidatePlayerListMotionSet(issues, path, "dashAttackList", data.dashAttackList, motionSet);
                ValidatePlayerListMotionSet(issues, path, "skillAttackList", data.skillAttackList, motionSet);
                ValidatePlayerAttackMotionSet(issues, path, "counterAttack", data.counterAttack, motionSet);
                ValidatePlayerAttackMotionSet(issues, path, "entryAttack", data.entryAttack, motionSet);
                ValidatePlayerAttackMotionSet(issues, path, "swapSpecialAttack", data.swapSpecialAttack, motionSet);
                ValidatePlayerAttackMotionSet(issues, path, "swapEvadeCounterAttack", data.swapEvadeCounterAttack, motionSet);
                ValidatePlayerAttackMotionSet(issues, path, "parryCounterAttack", data.parryCounterAttack, motionSet);
            }
        }

        private static void ValidatePlayerListMotionSet(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            List<AbilityAttackInfo> attacks,
            ActorAnimationMotionSet motionSet)
        {
            if (attacks == null)
                return;

            for (int i = 0; i < attacks.Count; i++)
                ValidatePlayerAttackMotionSet(issues, path, $"{context}[{i}]", attacks[i], motionSet);
        }

        private static void ValidatePlayerAttackMotionSet(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AbilityAttackInfo attack,
            ActorAnimationMotionSet motionSet)
        {
            if (attack?.baseInfo == null)
                return;
            ValidateAttackMotionSet(issues, path, context, attack.baseInfo, motionSet, null);
        }

        private static ActorAnimationMotionSet ResolveMotionSet(GameObject prefab)
        {
            if (prefab == null)
                return null;
            var animator = prefab.GetComponentInChildren<ActorAnimator>(true);
            return animator != null ? animator.MotionSet : null;
        }

        /// <summary>
        /// 단일 공격의 baseInfo와 MotionSet 타임라인 이벤트의 정합성을 검증한다.
        /// <paramref name="enemyInfo"/>가 null이 아니면 텔레그래프/방어 정책 등 적 전용 룰셋까지 적용한다.
        /// </summary>
        private static void ValidateAttackMotionSet(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AttackInfoBase baseInfo,
            ActorAnimationMotionSet motionSetRoot,
            AbilityAttackInfo enemyInfo)
        {
            // 모션 참조 누락은 SO 기본 검증에서 이미 보고하므로 여기서는 건너뛴다.
            if (baseInfo?.motionRef == null || !baseInfo.motionRef.HasAnyMotion)
                return;

            MotionSetAsset motionAsset = baseInfo.motionRef.defaultMotion;
            if (motionAsset == null && baseInfo.motionRef.weaponOverrides != null)
            {
                foreach (var weaponOverride in baseInfo.motionRef.weaponOverrides)
                {
                    if (weaponOverride.motion == null) continue;
                    motionAsset = weaponOverride.motion;
                    break;
                }
            }

            MotionSet motionSet = motionAsset?.motionSet;
            if (motionSet == null)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context,
                    $"MotionReference '{baseInfo.motionRef.name}'에서 유효한 MotionSet을 찾을 수 없습니다.");
                return;
            }

            MotionSetCombatEvents events = MotionSetCombatEvents.Collect(motionSet);
            int phaseCount = baseInfo.hitPhases?.Count ?? 0;

            if (baseInfo.attackType == AttackType.Melee && !events.HasCollision)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context,
                    $"근접(Melee) 공격 '{motionAsset.name}'의 MotionSet에 BeginCollisionEvent가 없습니다.");
            }

            foreach (BeginCollisionEvent collision in events.Collisions)
            {
                int index = collision.hitPhaseIndex;
                if (index < 0 || index >= phaseCount)
                {
                    AddIssue(issues, CombatValidationSeverity.Error, path, context,
                        $"BeginCollisionEvent.hitPhaseIndex={index}가 hitPhases 범위(0~{phaseCount - 1})를 벗어납니다. (motion '{motionAsset.name}')");
                }
            }

            if (phaseCount > 1 && events.HasCollision)
            {
                HashSet<int> usedPhases = events.CollisionPhaseIndices();
                if (usedPhases.Count == 1 && usedPhases.Contains(0))
                {
                    AddIssue(issues, CombatValidationSeverity.Warning, path, context,
                        $"hitPhases가 {phaseCount}개인데 Collision 이벤트가 phase 0만 사용합니다. 멀티 히트 구간이 발동하지 않을 수 있습니다.");
                }
            }

            if (enemyInfo == null)
                return;

            // --- 적 전용 룰셋 ---
            if (enemyInfo.useMotionEventTelegraph && !events.HasTelegraph)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context,
                    "useMotionEventTelegraph가 true인데 MotionSet에 TelegraphEvent가 없습니다.");
            }

            if (enemyInfo.useTelegraphPositionForHit && !events.HasPositionLockedTelegraph)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context,
                    "useTelegraphPositionForHit가 true인데 위치를 고정하는 TelegraphEvent(lockPositionOnStart)가 없습니다.");
            }

            if (enemyInfo.defenseType == AttackDefenseType.Unblockable
                && !enemyInfo.useDangerRing
                && !enemyInfo.useTelegraph
                && !events.HasTelegraph)
            {
                AddIssue(issues, CombatValidationSeverity.Warning, path, context,
                    "Unblockable(회피 필수) 공격인데 Danger Ring/Telegraph 표현이 전혀 없습니다. 회피 유도 단서가 부족합니다.");
            }
        }

        private static void ValidateCombatPolicyData(List<CombatValidationIssue> issues)
        {
            ValidateReactionPolicyAssets(issues);
            ValidateDefensePolicyAssets(issues);
            ValidateActorPolicyCoverage(issues);
        }

        private static void ValidateReactionPolicyAssets(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CombatReactionPolicySO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var policy = AssetDatabase.LoadAssetAtPath<CombatReactionPolicySO>(path);
                if (policy == null || policy.monsterGradeRules == null)
                    continue;

                var grades = new HashSet<MonsterActorGrade>();
                for (int i = 0; i < policy.monsterGradeRules.Count; i++)
                {
                    var rule = policy.monsterGradeRules[i];
                    if (rule == null)
                    {
                        AddIssue(issues, CombatValidationSeverity.Warning, path, $"monsterGradeRules[{i}]", "비어 있는 등급 규칙입니다.");
                        continue;
                    }

                    if (!grades.Add(rule.grade))
                    {
                        AddIssue(issues, CombatValidationSeverity.Warning, path, $"monsterGradeRules[{i}]",
                            $"등급 '{rule.grade}' 규칙이 중복됩니다. 런타임은 먼저 발견된 항목만 사용합니다.");
                    }

                    bool allowsAnyReaction = rule.allowHit
                                             || rule.allowStun
                                             || rule.allowKnockdown
                                             || rule.allowAirborne
                                             || rule.allowGrab;
                    if (!allowsAnyReaction)
                    {
                        AddIssue(issues, CombatValidationSeverity.Warning, path, $"monsterGradeRules[{i}]",
                            "모든 리액션 상태가 비활성입니다. forceReaction/PoiseBreak가 발생해도 상태 전환이 전부 차단됩니다.");
                    }
                }
            }
        }

        private static void ValidateDefensePolicyAssets(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CombatDefensePolicySO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var policy = AssetDatabase.LoadAssetAtPath<CombatDefensePolicySO>(path);
                if (policy == null)
                    continue;

                if (policy.allowGuardAgainstUnblockable)
                {
                    AddIssue(issues, CombatValidationSeverity.Warning, path, "allowGuardAgainstUnblockable",
                        "Unblockable을 가드 가능하게 설정했습니다. Danger Ring/Telegraph의 회피 필수 표현과 의도가 맞는지 확인하세요.");
                }
            }
        }

        private static void ValidateActorPolicyCoverage(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (actor == null || actor.EffectiveAbilitySet == null)
                    continue;

                if (actor.grade is MonsterActorGrade.Elite or MonsterActorGrade.Boss
                    && actor.combatReactionPolicy == null)
                {
                    AddIssue(issues, CombatValidationSeverity.Warning, path, "combatReactionPolicy",
                        $"{actor.grade} 몬스터에 리액션 정책이 없습니다. 기본 정책(기존 동작)을 사용합니다.");
                }
            }
        }

        private static void AddIssue(
            List<CombatValidationIssue> issues,
            CombatValidationSeverity severity,
            string path,
            string context,
            string message)
        {
            issues.Add(new CombatValidationIssue(severity, path, context, message));
        }
    }
}
#endif
