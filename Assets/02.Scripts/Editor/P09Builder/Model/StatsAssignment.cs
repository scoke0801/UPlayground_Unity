using System;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor.P09Builder
{
    [Serializable]
    public class StatsAssignment
    {
        // ---------- Enemy ----------
        // 체력/방어 등 런타임 스탯(ActorStatSO)은 P09 빌드 말미에
        // MonsterScalingSO/등급/레벨/무기 프로필 기준으로 발급·갱신한다.
        public bool createNewPoise = true;
        public ScriptableObject existingPoiseSo;
        public float defaultMaxPoise = 100f;
        public float defaultPoiseRecoveryDelay = 2f;
        public float defaultPoiseRecoveryRate = 40f;
        public bool defaultHasHyperArmor = false;
        public MonsterActorGrade grade = MonsterActorGrade.Normal;
        public int level = 1;
        public bool applyLevelScaling = true;
        public float attackPerLevel = 0.04f;
        public bool applyWeaponAttackBonus = true;
        public float defaultAttackDamage = 10f;
        public float weaponAttackPerTier = 0.04f;
        public bool randomizeStatsOnBuild = false;
        public float randomStatMin = 0.9f;
        public float randomStatMax = 1.1f;

        public bool createNewBehavior = true;
        public ScriptableObject existingBehaviorSo;
        public float optimalCombatDistance = 2.5f;

        public AbilitySetSO abilitySet;
        public EnemyCombatStyle combatStyle = EnemyCombatStyle.Melee;

        public bool recruitableOnDefeat = false;
        public CharacterActorType recruitableAs = CharacterActorType.None;

        // ---------- Player ----------
        public AbilitySetSO playerAbilitySet;

        // ---------- NPC ----------
        public ScriptableObject dialogueSo;
        public float wanderRadius = 5f;
    }

    /// <summary>
    /// 공격 Ability 생성 시 적용할 공격 배율을 계산한다.
    /// 체력/이동 등 런타임 스탯 튜닝은 MonsterScalingSO 기반 생성 경로가 담당하므로 여기서는 공격 배율만 다룬다.
    /// </summary>
    internal static class EnemyStatTuningUtility
    {
        public static float CalculateAttackMultiplier(CharacterBuildConfig config)
        {
            var stats = config?.Stats;
            if (stats == null)
                return 1f;

            float attack = GetGradeAttackMultiplier(stats.grade);

            if (stats.applyLevelScaling)
                attack *= 1f + stats.attackPerLevel * Mathf.Max(0, stats.level - 1);

            if (stats.applyWeaponAttackBonus)
                attack *= 1f + stats.weaponAttackPerTier * GetWeaponTier(config);

            if (stats.randomizeStatsOnBuild)
            {
                float min = Mathf.Min(stats.randomStatMin, stats.randomStatMax);
                float max = Mathf.Max(stats.randomStatMin, stats.randomStatMax);
                attack *= UnityEngine.Random.Range(min, max);
            }

            return Mathf.Max(0.01f, attack);
        }

        public static int GetWeaponTier(CharacterBuildConfig config)
        {
            if (config == null) return 0;

            int tier = 0;
            tier = Mathf.Max(tier, GetContentTier(config.SwordSo));
            tier = Mathf.Max(tier, GetContentTier(config.SubSwordSo));
            tier = Mathf.Max(tier, GetContentTier(config.GreatSwordSo));
            tier = Mathf.Max(tier, GetContentTier(config.ShieldSo));
            tier = Mathf.Max(tier, GetContentTier(config.BowSo));
            tier = Mathf.Max(tier, GetContentTier(config.StaffSo));
            tier = Mathf.Max(tier, GetContentTier(config.SpearSo));
            tier = Mathf.Max(tier, GetContentTier(config.DualAxeSo));
            tier = Mathf.Max(tier, GetContentTier(config.WhipSo));
            tier = Mathf.Max(tier, GetContentTier(config.WeaponGroupSo));
            return Mathf.Clamp(tier, 0, 30);
        }

        private static int GetContentTier(ScriptableObject so)
        {
            if (so == null) return 0;
            return ArmorIndexPresetUtility.TryGetIndex(so, out int index)
                ? index
                : 0;
        }

        private static float GetGradeAttackMultiplier(MonsterActorGrade grade)
        {
            return grade switch
            {
                MonsterActorGrade.Weak => 0.8f,
                MonsterActorGrade.Elite => 1.35f,
                MonsterActorGrade.Boss => 1.8f,
                _ => 1f,
            };
        }
    }
}
