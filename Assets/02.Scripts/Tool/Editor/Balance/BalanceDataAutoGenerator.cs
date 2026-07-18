#if UNITY_EDITOR
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 밸런스 창의 누락 데이터 안내기.
    /// 공격 Ability는 실행 Payload와 Variant를 함께 저작해야 하므로 Ability Editor에서 생성한다.
    /// </summary>
    public static class BalanceDataAutoGenerator
    {
        public static bool HasMissingData(ActorDefinitionSO actor)
        {
            if (actor == null)
                return false;

            return actor.statData == null
                   || actor.EffectiveAbilitySet == null
                   || actor.EffectiveBehaviorData == null;
        }

        public static GenerationSummary GenerateMissing(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario = null,
            BalanceScenarioInput fallbackInput = default)
        {
            if (actor != null && actor.EffectiveAbilitySet == null)
            {
                Debug.LogWarning(
                    $"[BalanceDataAutoGenerator] {actor.name}: 공격 AbilitySet은 "
                    + "Ability Editor에서 생성·연결해야 합니다.",
                    actor);
            }

            return default;
        }

        public struct GenerationSummary
        {
            public int CreatedCount;
            public string StatDataPath;
            public string AttackDataPath;
            public string BehaviorDataPath;
            public string BehaviorTreePath;
            public string MotionSetSource;
            public int GeneratedAttackSkillCount;

            public bool CreatedAny => CreatedCount > 0;
        }
    }
}
#endif
