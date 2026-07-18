#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// 에디터 도구가 AbilitySet의 공격 Payload를 같은 방식으로 읽도록 하는 공용 조회기.
    /// </summary>
    public static class AbilityAttackEditorUtility
    {
        public readonly struct Entry
        {
            public readonly GameplayAbilitySO Ability;
            public readonly UPlayGroundMotionAbilityPayloadSO Payload;
            public readonly AbilityAttackInfo AttackInfo;

            public Entry(
                GameplayAbilitySO ability,
                UPlayGroundMotionAbilityPayloadSO payload,
                AbilityAttackInfo attackInfo)
            {
                Ability = ability;
                Payload = payload;
                AttackInfo = attackInfo;
            }
        }

        public static List<Entry> Collect(
            AbilitySetSO set,
            bool aiOnly = false)
        {
            var result = new List<Entry>();
            if (set == null)
                return result;

            var visited = new HashSet<GameplayAbilitySO>();
            foreach (GameplayAbilitySO ability in set.EnumerateAll())
            {
                if (ability == null
                    || !visited.Add(ability)
                    || ability.variants == null)
                    continue;

                for (int i = 0; i < ability.variants.Count; i++)
                {
                    if (ability.variants[i]?.executionPayload
                            is not UPlayGroundMotionAbilityPayloadSO payload
                        || !UPlayGroundAbilityPayloadResolver.TryResolve(
                            ability.variants[i],
                            out _,
                            out AbilityAttackInfo attackInfo)
                        || attackInfo?.baseInfo == null
                        || (aiOnly && !attackInfo.aiSelectable))
                        continue;

                    result.Add(new Entry(ability, payload, attackInfo));
                    break;
                }
            }

            return result;
        }

        public static bool IsInRange(
            GameplayAbilitySO ability,
            float distance)
        {
            if (ability?.activation == null)
                return false;

            return distance >= ability.activation.minDistance
                   && distance <= ability.activation.maxDistance;
        }
    }
}
#endif
