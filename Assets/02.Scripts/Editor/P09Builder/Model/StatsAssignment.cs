using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace Game.Editor.P09Builder
{
    [Serializable]
    public class StatsAssignment
    {
        // ---------- Enemy ----------
        public bool createNewStats = true;
        public ScriptableObject existingStatsSo;
        public float defaultHp = 100f;
        public float defaultWalkSpeed = 2f;
        public float defaultRunSpeed = 4f;
        public float defaultDetectionRadius = 10f;
        public bool createNewPoise = true;
        public ScriptableObject existingPoiseSo;
        public float defaultMaxPoise = 100f;
        public float defaultPoiseRecoveryDelay = 2f;
        public float defaultPoiseRecoveryRate = 40f;
        public bool defaultHasHyperArmor = false;
        public MonsterActorGrade grade = MonsterActorGrade.Normal;
        public int level = 1;
        public bool applyLevelScaling = true;
        public float healthPerLevel = 0.08f;
        public float attackPerLevel = 0.04f;
        public bool applyArmorStatBonus = true;
        public float armorHealthPerTier = 0.025f;
        public float armorMoveSpeedPerTier = 0.005f;
        public bool applyWeaponAttackBonus = true;
        public float defaultAttackDamage = 10f;
        public float weaponAttackPerTier = 0.04f;
        public bool randomizeStatsOnBuild = false;
        public float randomStatMin = 0.9f;
        public float randomStatMax = 1.1f;

        public bool createNewBehavior = true;
        public ScriptableObject existingBehaviorSo;
        public float optimalCombatDistance = 2.5f;

        public ScriptableObject attackDataSo;
        public EnemyCombatStyle combatStyle = EnemyCombatStyle.Melee;

        public bool recruitableOnDefeat = false;
        public CharacterActorType recruitableAs = CharacterActorType.None;

        // ---------- Player ----------
        public ScriptableObject playerAttackDataSo;
        public bool addToStartingParty = false;
        public int partyOrder = 0;

        // ---------- NPC ----------
        public ScriptableObject dialogueSo;
        public float wanderRadius = 5f;
    }

    internal readonly struct EnemyStatTuning
    {
        public readonly float HealthMultiplier;
        public readonly float MoveSpeedMultiplier;
        public readonly float AttackMultiplier;

        public EnemyStatTuning(float healthMultiplier, float moveSpeedMultiplier, float attackMultiplier)
        {
            HealthMultiplier = healthMultiplier;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            AttackMultiplier = attackMultiplier;
        }
    }

    internal static class EnemyStatTuningUtility
    {
        public static EnemyStatTuning Calculate(CharacterBuildConfig config)
        {
            var stats = config?.Stats;
            if (stats == null)
                return new EnemyStatTuning(1f, 1f, 1f);

            float health = GetGradeHealthMultiplier(stats.grade);
            float move = 1f;
            float attack = GetGradeAttackMultiplier(stats.grade);

            if (stats.applyLevelScaling)
            {
                int levelDelta = Mathf.Max(0, stats.level - 1);
                health *= 1f + stats.healthPerLevel * levelDelta;
                attack *= 1f + stats.attackPerLevel * levelDelta;
            }

            if (stats.applyArmorStatBonus)
            {
                int armorTier = GetArmorTier(config);
                health *= 1f + stats.armorHealthPerTier * armorTier;
                move *= 1f + stats.armorMoveSpeedPerTier * armorTier;
            }

            if (stats.applyWeaponAttackBonus)
            {
                int weaponTier = GetWeaponTier(config);
                attack *= 1f + stats.weaponAttackPerTier * weaponTier;
            }

            if (stats.randomizeStatsOnBuild)
            {
                float min = Mathf.Min(stats.randomStatMin, stats.randomStatMax);
                float max = Mathf.Max(stats.randomStatMin, stats.randomStatMax);
                health *= UnityEngine.Random.Range(min, max);
                move *= UnityEngine.Random.Range(min, max);
                attack *= UnityEngine.Random.Range(min, max);
            }

            return new EnemyStatTuning(
                Mathf.Max(0.01f, health),
                Mathf.Max(0.01f, move),
                Mathf.Max(0.01f, attack));
        }

        public static int GetArmorTier(CharacterBuildConfig config)
        {
            if (config?.ArmorSelections == null) return 0;
            int presetIndex = ArmorIndexPresetUtility.GetCurrentPresetIndex(config.ArmorSelections);
            if (presetIndex < 0)
                presetIndex = config.ArmorSelections.TryGetArmorIndex(BuilderArmorSlot.Chest);
            return Mathf.Clamp(presetIndex, 0, 30);
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

        private static float GetGradeHealthMultiplier(MonsterActorGrade grade)
        {
            return grade switch
            {
                MonsterActorGrade.Weak => 0.75f,
                MonsterActorGrade.Elite => 1.6f,
                MonsterActorGrade.Boss => 5.0f,
                _ => 1f,
            };
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
