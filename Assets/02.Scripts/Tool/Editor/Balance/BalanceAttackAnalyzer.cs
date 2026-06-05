#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Balance
{
    public static class BalanceAttackAnalyzer
    {
        public static bool IsStrongEnemyAttack(EnemyAttackInfo skill)
        {
            if (skill == null)
                return false;

            if (skill.attackCategory is EnemyAttackCategory.Heavy or EnemyAttackCategory.Skill)
                return true;

            AnimKey key = skill.baseInfo != null ? skill.baseInfo.animKey : AnimKey.None;
            int value = (int)key;
            return key == AnimKey.Fly_Attack ||
                   (value >= (int)AnimKey.HeavyAttack_1 && value <= (int)AnimKey.HeavyAttack_10) ||
                   (value >= (int)AnimKey.Skill_1 && value <= (int)AnimKey.Skill_9) ||
                   (value >= (int)AnimKey.Counter_Attack_1 && value <= (int)AnimKey.Counter_Attack_2);
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

        public static List<EnemyAttackInfo> GetUsableEnemySkills(EnemyAttackDataSO data, float distance, int level)
        {
            var result = new List<EnemyAttackInfo>();
            if (data?.skills == null)
                return result;

            for (int i = 0; i < data.skills.Count; i++)
            {
                EnemyAttackInfo skill = data.skills[i];
                if (skill == null || skill.baseInfo == null)
                    continue;
                if (skill.skillType != SkillType.Attack)
                    continue;
                if (!skill.IsUnlockedForLevel(level))
                    continue;
                if (!skill.IsInRange(distance))
                    continue;
                if (SumDamage(skill.baseInfo) <= 0f)
                    continue;

                result.Add(skill);
            }

            return result;
        }

        public static float EstimatePlayerRawDps(PlayerAttackDataSO data, float attackInterval, float fallbackDps)
        {
            if (data == null)
                return fallbackDps;

            var attacks = new List<PlayerAttackInfo>();
            AddRange(attacks, data.liteComboAttackList);
            AddRange(attacks, data.heavyComboAttackList);
            AddRange(attacks, data.jumpAttackList);
            AddRange(attacks, data.dashAttackList);
            AddRange(attacks, data.skillAttackList);
            AddOne(attacks, data.counterAttack);
            AddOne(attacks, data.parryCounterAttack);
            AddOne(attacks, data.entryAttack);
            AddOne(attacks, data.swapEvadeCounterAttack);
            AddOne(attacks, data.swapSpecialAttack);

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

        public static float EstimatePlayerRawBreakDps(PlayerAttackDataSO data, float attackInterval)
        {
            if (data == null)
                return 0f;

            var attacks = new List<PlayerAttackInfo>();
            AddRange(attacks, data.liteComboAttackList);
            AddRange(attacks, data.heavyComboAttackList);
            AddRange(attacks, data.jumpAttackList);
            AddRange(attacks, data.dashAttackList);
            AddRange(attacks, data.skillAttackList);
            AddOne(attacks, data.counterAttack);
            AddOne(attacks, data.parryCounterAttack);
            AddOne(attacks, data.entryAttack);
            AddOne(attacks, data.swapEvadeCounterAttack);
            AddOne(attacks, data.swapSpecialAttack);

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

        private static void AddRange(List<PlayerAttackInfo> target, List<PlayerAttackInfo> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                AddOne(target, source[i]);
        }

        private static void AddOne(List<PlayerAttackInfo> target, PlayerAttackInfo value)
        {
            if (value?.baseInfo != null)
                target.Add(value);
        }
    }
}
#endif
