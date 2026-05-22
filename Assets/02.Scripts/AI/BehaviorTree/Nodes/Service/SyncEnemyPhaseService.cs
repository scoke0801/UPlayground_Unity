using UPlayGround.Component;
using UPlayGround.Data.Enemy;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 몬스터 HP 기반 페이즈를 EnemyAIContext와 Blackboard에 동기화한다.
    /// </summary>
    public class SyncEnemyPhaseService : BTServiceNode
    {
        protected override void OnServiceTick()
        {
            var context = Context?.GetComponentCached<EnemyAIContext>();
            if (context == null || Context.Blackboard == null)
                return;

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
