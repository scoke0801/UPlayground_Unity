#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround.Data.Enemy.EditorTools
{
    /// <summary>
    /// 기본 Intent Weights SO 자산을 코드로 생성한다.
    /// EnemyCombatDecisionEvaluator 레거시 매직 넘버와 동일한 결과를 내는 IW_Default_Melee를 기준으로,
    /// 성격이 다른 3개 프로파일을 함께 만든다.
    /// </summary>
    public static class IntentWeightsAssetFactory
    {
        private const string AssetFolder = "Assets/10.Datas/AI/IntentWeights";

        [MenuItem("UPlayGround/Enemy/Intent Weights/Generate All Default Profiles")]
        public static void GenerateAll()
        {
            EnsureFolder();
            CreateOrUpdate("IW_Default_Melee",     BuildDefaultMelee);
            CreateOrUpdate("IW_AggressiveMelee",   BuildAggressiveMelee);
            CreateOrUpdate("IW_DefensiveShield",   BuildDefensiveShield);
            CreateOrUpdate("IW_RangedCaster",      BuildRangedCaster);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[IntentWeights] 4개 기본 프로파일 생성/갱신 완료.");
        }

        [MenuItem("UPlayGround/Enemy/Intent Weights/Regenerate Default Melee (Legacy Equivalent)")]
        public static void RegenerateDefaultMelee()
        {
            EnsureFolder();
            CreateOrUpdate("IW_Default_Melee", BuildDefaultMelee);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[IntentWeights] IW_Default_Melee 재생성 완료. EnemyCombatDecisionEvaluator의 레거시 매직 넘버와 동등.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "../" + AssetFolder));
                AssetDatabase.Refresh();
            }
        }

        private static void CreateOrUpdate(string fileName, System.Action<EnemyIntentWeightsSO> build)
        {
            var path = $"{AssetFolder}/{fileName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<EnemyIntentWeightsSO>(path);
            var isNew = so == null;
            if (isNew) so = ScriptableObject.CreateInstance<EnemyIntentWeightsSO>();

            // 모든 entry 초기화 (재생성 시 기존 데이터 폐기)
            so.attack       = new IntentWeightEntry();
            so.punish       = new IntentWeightEntry();
            so.counter      = new IntentWeightEntry();
            so.pressure     = new IntentWeightEntry();
            so.chase        = new IntentWeightEntry();
            so.retreat      = new IntentWeightEntry();
            so.keepDistance = new IntentWeightEntry();
            so.defend       = new IntentWeightEntry();
            so.recover      = new IntentWeightEntry();

            build(so);

            if (isNew) AssetDatabase.CreateAsset(so, path);
            else EditorUtility.SetDirty(so);
        }

        // ─────────────────────────────────────────────────────────────
        // IW_Default_Melee : 레거시 EnemyCombatDecisionEvaluator와 동등
        // ─────────────────────────────────────────────────────────────
        private static void BuildDefaultMelee(EnemyIntentWeightsSO so)
        {
            // Attack
            so.attack.baseScore = 0.10f;
            so.attack.baseContinuous = Single(new ContinuousContribution(ContinuousValueId.Aggression, 0.42f));
            so.attack.bonuses = new List<ConditionBonus>
            {
                Bonus("InAttackRange&Delay",       0.45f, T(IntentConditionId.InAttackRange), T(IntentConditionId.ActionDelayElapsed)),
                Bonus("CanUseSkill",               0.08f, T(IntentConditionId.CanUseSkill)),
                Bonus("PlayerStaggered",           0.18f, T(IntentConditionId.IsPlayerStaggered)),
                Bonus("PlayerRecoverFrequent",     0.10f, T(IntentConditionId.IsPlayerRecoveringFrequently))
            };
            so.attack.multipliers = new List<ConditionMultiplier>
            {
                Mul("NoAttack|NotReady", 0.55f, ConditionMode.Any,
                    T(IntentConditionId.HasAvailableAttack, true),
                    T(IntentConditionId.ActionDelayElapsed, true))
            };

            // Punish
            so.punish.baseScore = 0.03f;
            so.punish.baseContinuous = Single(new ContinuousContribution(ContinuousValueId.PunishChance, 0.45f));
            so.punish.bonuses = new List<ConditionBonus>
            {
                Bonus("PlayerRecover|Stagger",     0.45f, ConditionMode.Any,
                    T(IntentConditionId.IsPlayerRecovering),
                    T(IntentConditionId.IsPlayerStaggered)),
                Bonus("PlayerDodgeFrequent",       0.18f, T(IntentConditionId.IsPlayerDodgingFrequently)),
                Bonus("PlayerRecoverFrequent",     0.18f, T(IntentConditionId.IsPlayerRecoveringFrequently)),
                Bonus("InAttackRange&Delay",       0.22f, T(IntentConditionId.InAttackRange), T(IntentConditionId.ActionDelayElapsed))
            };
            so.punish.multipliers = new List<ConditionMultiplier>
            {
                Mul("NoAttack|NotReady", 0.45f, ConditionMode.Any,
                    T(IntentConditionId.HasAvailableAttack, true),
                    T(IntentConditionId.ActionDelayElapsed, true))
            };

            // Counter
            so.counter.baseScore = 0.04f;
            so.counter.baseContinuous = Single(new ContinuousContribution(ContinuousValueId.CounterChance, 0.50f));
            so.counter.bonuses = new List<ConditionBonus>
            {
                BonusWithContinuous("PlayerAttack&WithinOptimal", 0.16f,
                    new[] { T(IntentConditionId.IsPlayerAttacking), T(IntentConditionId.IsDistanceWithinOptimal) },
                    new[] { new ContinuousContribution(ContinuousValueId.ReactionChance, 0.45f) }),
                Bonus("TooClose&PlayerAttack",     0.16f, T(IntentConditionId.TooClose), T(IntentConditionId.IsPlayerAttacking)),
                Bonus("PlayerAttackFrequent",      0.20f, T(IntentConditionId.IsPlayerAttackingFrequently))
            };
            so.counter.multipliers = new List<ConditionMultiplier>
            {
                Mul("NotReady", 0.65f, ConditionMode.All, T(IntentConditionId.ActionDelayElapsed, true))
            };

            // Pressure
            so.pressure.baseScore = 0.12f;
            so.pressure.baseContinuous = new List<ContinuousContribution>
            {
                new(ContinuousValueId.Aggression, 0.24f),
                new(ContinuousValueId.CircleWeight, 0.22f)
            };
            so.pressure.bonuses = new List<ConditionBonus>
            {
                Bonus("!PlayerAttack&WithinPreferred", 0.16f,
                    T(IntentConditionId.IsPlayerAttacking, true),
                    T(IntentConditionId.IsDistanceWithinPreferredPlusBuffer)),
                Bonus("PlayerGuard|DodgeFrequent",     0.12f, ConditionMode.Any,
                    T(IntentConditionId.IsPlayerGuarding),
                    T(IntentConditionId.IsPlayerDodgingFrequently)),
                Bonus("PlayerGuardFrequent",           0.14f, T(IntentConditionId.IsPlayerGuardingFrequently)),
                Bonus("NotReady",                      0.10f, T(IntentConditionId.ActionDelayElapsed, true))
            };

            // Chase
            so.chase.baseScore = 0.05f;
            so.chase.bonuses = new List<ConditionBonus>
            {
                BonusWithContinuous("OverPreferred", 0.50f,
                    new[] { T(IntentConditionId.OverPreferredRange) },
                    new[] { new ContinuousContribution(ContinuousValueId.Aggression, 0.25f) }),
                Bonus("FarFromOptimal",              0.18f, T(IntentConditionId.IsDistanceFarFromOptimal))
            };

            // Retreat
            so.retreat.baseScore = 0.02f;
            so.retreat.baseContinuous = Single(new ContinuousContribution(ContinuousValueId.RetreatChance, 0.42f));
            so.retreat.bonuses = new List<ConditionBonus>
            {
                Bonus("TooClose",              0.35f, T(IntentConditionId.TooClose)),
                Bonus("WasHitRecently",        0.18f, T(IntentConditionId.WasHitRecently)),
                Bonus("PoiseBroken",           0.30f, T(IntentConditionId.IsPoiseBroken)),
                Bonus("LowHealth",             0.18f, T(IntentConditionId.LowHealth)),
                Bonus("PlayerAttackFreq&TooClose", 0.12f,
                    T(IntentConditionId.IsPlayerAttackingFrequently),
                    T(IntentConditionId.TooClose))
            };
            so.retreat.multipliers = new List<ConditionMultiplier>
            {
                Mul("RetreatCooldown", 0.35f, ConditionMode.All, T(IntentConditionId.TimeSinceRetreatBelowMinCooldown))
            };

            // KeepDistance
            so.keepDistance.baseScore = 0.05f;
            so.keepDistance.bonuses = new List<ConditionBonus>
            {
                Bonus("UnderPreferred",                  0.34f, T(IntentConditionId.UnderPreferredRange)),
                Bonus("PlayerAttack&WithinMin",          0.20f,
                    T(IntentConditionId.IsPlayerAttacking),
                    T(IntentConditionId.IsDistanceWithinMinDistance)),
                Bonus("PlayerAttackFreq&WithinOptimal",  0.12f,
                    T(IntentConditionId.IsPlayerAttackingFrequently),
                    T(IntentConditionId.IsDistanceWithinOptimal))
            };
            so.keepDistance.multipliers = new List<ConditionMultiplier>
            {
                Mul("OverPreferred", 0.45f, ConditionMode.All, T(IntentConditionId.OverPreferredRange))
            };

            // Defend
            so.defend.baseScore = 0.04f;
            so.defend.baseContinuous = Single(new ContinuousContribution(ContinuousValueId.GuardChance, 0.38f));
            so.defend.bonuses = new List<ConditionBonus>
            {
                Bonus("GuardMotion&PlayerAttack&WithinOptimal", 0.30f,
                    T(IntentConditionId.HasGuardMotion),
                    T(IntentConditionId.IsPlayerAttacking),
                    T(IntentConditionId.IsDistanceWithinOptimal)),
                Bonus("Hit&NotPoiseBroken", 0.12f,
                    T(IntentConditionId.WasHitRecently),
                    T(IntentConditionId.IsPoiseBroken, true)),
                Bonus("PlayerAttackFrequent", 0.16f, T(IntentConditionId.IsPlayerAttackingFrequently))
            };

            // Recover
            so.recover.baseScore = 0.02f;
            so.recover.bonuses = new List<ConditionBonus>
            {
                Bonus("LowHealth",            0.20f, T(IntentConditionId.LowHealth)),
                Bonus("Hit&LowHealth",        0.18f, T(IntentConditionId.WasHitRecently), T(IntentConditionId.LowHealth))
            };
            so.recover.multipliers = new List<ConditionMultiplier>
            {
                Mul("TooClose|PlayerAttack", 0.55f, ConditionMode.Any,
                    T(IntentConditionId.TooClose),
                    T(IntentConditionId.IsPlayerAttacking))
            };
        }

        // ─────────────────────────────────────────────────────────────
        // IW_AggressiveMelee : 공격적 검사. Attack/Punish/Pressure ↑, Retreat ↓
        // ─────────────────────────────────────────────────────────────
        private static void BuildAggressiveMelee(EnemyIntentWeightsSO so)
        {
            BuildDefaultMelee(so);

            so.attack.baseScore = 0.18f;
            so.attack.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.Aggression, 0.55f);
            so.attack.bonuses[0].amount = 0.55f;

            so.punish.bonuses[0].amount = 0.55f;
            so.punish.bonuses[3].amount = 0.30f;

            so.pressure.baseScore = 0.18f;
            so.pressure.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.Aggression, 0.32f);

            so.retreat.baseScore = 0.0f;
            so.retreat.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.RetreatChance, 0.25f);
            so.retreat.multipliers[0].factor = 0.20f;

            so.defend.baseScore = 0.02f;
            so.defend.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.GuardChance, 0.25f);
        }

        // ─────────────────────────────────────────────────────────────
        // IW_DefensiveShield : 방패병. Defend/KeepDistance ↑↑, Counter 강화
        // ─────────────────────────────────────────────────────────────
        private static void BuildDefensiveShield(EnemyIntentWeightsSO so)
        {
            BuildDefaultMelee(so);

            so.defend.baseScore = 0.18f;
            so.defend.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.GuardChance, 0.55f);
            so.defend.bonuses[0].amount = 0.45f;

            so.keepDistance.baseScore = 0.12f;
            so.keepDistance.bonuses[0].amount = 0.45f;

            so.counter.baseScore = 0.10f;
            so.counter.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.CounterChance, 0.65f);
            so.counter.bonuses[0].amount = 0.28f;

            so.attack.baseScore = 0.06f;
            so.attack.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.Aggression, 0.30f);
            so.attack.bonuses[0].amount = 0.32f;
        }

        // ─────────────────────────────────────────────────────────────
        // IW_RangedCaster : 원거리 캐스터. KeepDistance/Retreat ↑, Attack 거리 조건 강화
        // ─────────────────────────────────────────────────────────────
        private static void BuildRangedCaster(EnemyIntentWeightsSO so)
        {
            BuildDefaultMelee(so);

            so.keepDistance.baseScore = 0.20f;
            so.keepDistance.bonuses[0].amount = 0.50f;

            so.retreat.baseScore = 0.10f;
            so.retreat.baseContinuous[0] = new ContinuousContribution(ContinuousValueId.RetreatChance, 0.55f);
            so.retreat.bonuses[0].amount = 0.45f; // TooClose 강화

            so.attack.baseScore = 0.08f;
            so.attack.bonuses[0].amount = 0.50f; // InAttackRange 조건 강화 (충족 시 큰 점수)
            so.attack.multipliers[0].factor = 0.35f; // 조건 미충족 시 크게 감쇠

            so.pressure.baseScore = 0.06f;
            so.pressure.baseContinuous = new List<ContinuousContribution>
            {
                new(ContinuousValueId.Aggression, 0.15f),
                new(ContinuousValueId.CircleWeight, 0.28f)
            };

            so.chase.baseScore = 0.03f;
            so.chase.bonuses[0].amount = 0.30f; // 원거리는 추격을 덜 함
        }

        // ─────────────────────────────────────────────────────────────
        // 헬퍼들
        // ─────────────────────────────────────────────────────────────
        private static List<ContinuousContribution> Single(ContinuousContribution c)
            => new() { c };

        private static ConditionTerm T(IntentConditionId id, bool negate = false)
            => new(id, negate);

        private static ConditionBonus Bonus(string label, float amount, params ConditionTerm[] terms)
            => Bonus(label, amount, ConditionMode.All, terms);

        private static ConditionBonus Bonus(string label, float amount, ConditionMode mode, params ConditionTerm[] terms)
            => new()
            {
                label = label,
                mode = mode,
                terms = new List<ConditionTerm>(terms),
                amount = amount,
                continuous = new List<ContinuousContribution>()
            };

        private static ConditionBonus BonusWithContinuous(
            string label, float amount, ConditionTerm[] terms, ContinuousContribution[] continuous)
            => new()
            {
                label = label,
                mode = ConditionMode.All,
                terms = new List<ConditionTerm>(terms),
                amount = amount,
                continuous = new List<ContinuousContribution>(continuous)
            };

        private static ConditionMultiplier Mul(string label, float factor, ConditionMode mode, params ConditionTerm[] terms)
            => new()
            {
                label = label,
                mode = mode,
                factor = factor,
                terms = new List<ConditionTerm>(terms)
            };
    }
}
#endif
