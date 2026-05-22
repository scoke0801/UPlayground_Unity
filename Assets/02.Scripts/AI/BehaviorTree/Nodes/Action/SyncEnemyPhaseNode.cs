using UPlayGround.Component;
using UPlayGround.Data.Enemy;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// SyncEnemyPhaseService의 1회 Tick 버전 Action. JSON으로 만든 BT가 Service 부착 없이도
    /// HP 기반 페이즈 정보(Self.HpPercent / Self.PhaseName / Self.PhaseIndex / AllowCharge / AllowFlank /
    /// MaxConsecutiveAttacks / ContinueAttackChance / GuardChance / RetreatChance)를
    /// Blackboard에 채울 수 있도록 한다. 페이즈 분기를 사용하는 Sequence의 맨 앞에 둔다.
    /// </summary>
    public class SyncEnemyPhaseNode : BTActionNode
    {
        protected override BTStatus OnUpdate()
        {
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null || Context.Blackboard == null)
                return BTStatus.Failure;

            var hpPercent = context.HealthPercent;
            context.UpdatePhase(hpPercent);

            var phase = context.CurrentPhase;
            var phaseIndex = GetPhaseIndex(context.BehaviorData, phase);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.SelfHpPercent, hpPercent);
            Context.Blackboard.SetString(EnemyBlackboardKeys.SelfPhaseName, phase?.phaseName ?? "");
            Context.Blackboard.SetInt(EnemyBlackboardKeys.SelfPhaseIndex, phaseIndex);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.AllowCharge, phase?.allowCharge ?? false);
            Context.Blackboard.SetBool(EnemyBlackboardKeys.AllowFlank, phase?.allowFlank ?? false);
            Context.Blackboard.SetInt(EnemyBlackboardKeys.MaxConsecutiveAttacks, phase?.maxConsecutiveAttacks ?? 3);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.ContinueAttackChance, phase?.continueAttackChance ?? context.BehaviorData?.continueAttackChance ?? 0.3f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.GuardChance, phase?.guardChance ?? context.BehaviorData?.guardChance ?? 0.25f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.RetreatChance, phase?.retreatChance ?? context.BehaviorData?.retreatChance ?? 0.2f);
            Context.Blackboard.SetString(EnemyBlackboardKeys.EnemyAIRole, context.BehaviorData?.aiRole.ToString() ?? "Melee");
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightAttack, phase?.attackWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightPunish, phase?.punishWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightCounter, phase?.counterWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightPressure, phase?.pressureWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightChase, phase?.chaseWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightRetreat, phase?.retreatWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightKeepDistance, phase?.keepDistanceWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightDefend, phase?.defendWeight ?? 1f);
            Context.Blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightRecover, phase?.recoverWeight ?? 1f);
            return BTStatus.Success;
        }

        private static int GetPhaseIndex(EnemyBehaviorSO behavior, BehaviorPhase currentPhase)
        {
            if (behavior?.phases == null || currentPhase == null)
                return -1;

            for (var i = 0; i < behavior.phases.Length; i++)
            {
                if (ReferenceEquals(behavior.phases[i], currentPhase))
                    return i;
            }

            return -1;
        }
    }
}
