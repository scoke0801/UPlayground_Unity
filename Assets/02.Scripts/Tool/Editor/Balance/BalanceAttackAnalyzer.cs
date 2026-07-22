#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Tool.Editor.Balance
{
    public static class BalanceAttackAnalyzer
    {
        public static bool IsStrongEnemyAttack(AbilityAttackInfo skill)
        {
            if (skill == null)
                return false;

            if (skill.attackCategory is AbilityAttackCategory.Heavy or AbilityAttackCategory.Skill)
                return true;

            return false;
        }

        public static float SumDamage(AttackInfoBase baseInfo)
        {
            if (baseInfo?.hitPhases == null || baseInfo.hitPhases.Count == 0)
                return 0f;

            float damage = 0f;
            for (int i = 0; i < baseInfo.hitPhases.Count; i++)
                damage += baseInfo.hitPhases[i]?.damage ?? 0f;
            return damage;
        }

        public static float SumPoiseDamage(AttackInfoBase baseInfo)
        {
            if (baseInfo?.hitPhases == null || baseInfo.hitPhases.Count == 0)
                return 0f;

            float poise = 0f;
            for (int i = 0; i < baseInfo.hitPhases.Count; i++)
                poise += baseInfo.hitPhases[i]?.poiseDamage ?? 0f;
            return poise;
        }

        public static float SumBreakDamage(AttackInfoBase baseInfo)
        {
            if (baseInfo?.hitPhases == null || baseInfo.hitPhases.Count == 0)
                return 0f;

            float value = 0f;
            for (int i = 0; i < baseInfo.hitPhases.Count; i++)
                value += baseInfo.hitPhases[i]?.breakDamage ?? 0f;
            return value;
        }

        public static int CountHitPhases(AttackInfoBase baseInfo)
            => baseInfo?.hitPhases?.Count ?? 0;

        public static List<AbilityAttackInfo> GetUsableEnemySkills(
            AbilitySetSO data,
            float distance,
            int level)
        {
            var result = new List<AbilityAttackInfo>();
            List<AbilityAttackEditorUtility.Entry> entries =
                AbilityAttackEditorUtility.Collect(data, true);
            for (int i = 0; i < entries.Count; i++)
            {
                AbilityAttackInfo skill = entries[i].AttackInfo;
                if (skill == null || skill.baseInfo == null)
                    continue;
                if (skill.skillType != SkillType.Attack)
                    continue;
                if (!skill.IsUnlockedForLevel(level))
                    continue;
                if (!AbilityAttackEditorUtility.IsInRange(
                        entries[i].Ability,
                        distance))
                    continue;
                if (SumDamage(skill.baseInfo) <= 0f)
                    continue;

                result.Add(skill);
            }

            return result;
        }

        public static GameplayAbilitySO FindAbility(
            AbilitySetSO set,
            AbilityAttackInfo attackInfo)
        {
            List<AbilityAttackEditorUtility.Entry> entries =
                AbilityAttackEditorUtility.Collect(set);
            for (int i = 0; i < entries.Count; i++)
                if (ReferenceEquals(entries[i].AttackInfo, attackInfo))
                    return entries[i].Ability;
            return null;
        }

        public static float EstimatePlayerRawDps(AbilitySetSO data, float attackInterval, float fallbackDps)
        {
            if (data == null)
                return fallbackDps;

            List<AbilityAttackInfo> attacks = CollectAttacks(data);

            float totalDamage = 0f;
            int count = 0;
            for (int i = 0; i < attacks.Count; i++)
            {
                float damage = SumDamage(attacks[i]?.baseInfo);
                if (damage <= 0f)
                    continue;

                totalDamage += damage;
                count++;
            }

            if (count == 0)
                return fallbackDps;

            float averageDamage = totalDamage / count;
            return averageDamage / UnityEngine.Mathf.Max(0.05f, attackInterval);
        }

        public static float EstimatePlayerRawBreakDps(AbilitySetSO data, float attackInterval)
        {
            if (data == null)
                return 0f;

            List<AbilityAttackInfo> attacks = CollectAttacks(data);

            float totalBreak = 0f;
            int count = 0;
            for (int i = 0; i < attacks.Count; i++)
            {
                float breakDamage = SumBreakDamage(attacks[i]?.baseInfo);
                if (breakDamage <= 0f)
                    continue;

                totalBreak += breakDamage;
                count++;
            }

            if (count == 0)
                return 0f;

            float averageBreak = totalBreak / count;
            return averageBreak / UnityEngine.Mathf.Max(0.05f, attackInterval);
        }

        private static void AddRange(List<AbilityAttackInfo> target, List<AbilityAttackInfo> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                AddOne(target, source[i]);
        }

        private static void AddOne(List<AbilityAttackInfo> target, AbilityAttackInfo value)
        {
            if (value?.baseInfo != null)
                target.Add(value);
        }

        public static List<AbilityAttackInfo> CollectAttacks(AbilitySetSO data)
        {
            var attacks = new List<AbilityAttackInfo>();
            PlayerCombatAbilityDataView view =
                PlayerCombatAbilityDataView.Build(data);
            if (view == null)
                return attacks;

            AddRange(attacks, view.liteComboAttackList);
            AddRange(attacks, view.heavyComboAttackList);
            AddRange(attacks, view.jumpAttackList);
            AddRange(attacks, view.dashAttackList);
            AddRange(attacks, view.skillAttackList);
            AddOne(attacks, view.counterAttack);
            AddOne(attacks, view.parryCounterAttack);
            AddOne(attacks, view.entryAttack);
            AddOne(attacks, view.entryAttackVsGroggy);
            AddOne(attacks, view.entryAttackVsAirborne);
            AddOne(attacks, view.swapEvadeCounterAttack);
            AddOne(attacks, view.swapSpecialAttack);
            for (int i = 0; i < view.comboRoutes.Count; i++)
            {
                AddOne(attacks, view.comboRoutes[i]?.attackInfo);
                AddOne(attacks, view.comboRoutes[i]?.enhancedAttackInfo);
            }
            return attacks;
        }
    }
}
#endif
