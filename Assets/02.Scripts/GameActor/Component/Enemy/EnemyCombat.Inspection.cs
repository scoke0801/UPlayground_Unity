#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Components
{
    public partial class EnemyCombat
    {
        public event System.Action<string, string, string> LayaAbilityStarted;
        public event System.Action<bool> LayaAbilityEnded;
        /// <summary>실제 선택 버퍼·난수·활성화 실패 계측에 영향을 주지 않는 전송용 공격 자격.</summary>
        public JArray CaptureAttackEligibility(float distance)
        {
            var entries = new JArray();
            if (_abilitySet == null || _abilitySystem == null) return entries;
            int aliveSpawned = 0;
            foreach (var unit in _spawnedUnits)
                if (unit != null && unit.GetComponent<UPlayGround.IDamageable>()?.IsAlive() != false) aliveSpawned++;
            var context = CreateContext(distance, aliveSpawned);
            var target = ResolveAbilityTarget();
            var seen = new HashSet<string>();
            foreach (var ability in _abilitySet.GetRuntimeAbilities())
            {
                if (ability?.variants == null || !seen.Add(ability.abilityId)) continue;
                foreach (var variant in ability.variants)
                {
                    if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out var info)
                        || !info.aiSelectable) continue;
                    var activation = _abilitySystem.InspectVariant(ability, variant, IsGrounded(), target, out var cooldown);
                    var reason = EnemyAbilitySelectionPolicy.Evaluate(info, in context, AbilityAttackCategory.None, false, false);
                    float score = info.selectionWeight;
                    reason |= EvaluateStrategy(ability, ResolveCombatStrategy(), ref score);
                    if (info.baseInfo.attackType == AttackType.Melee && !EnemyAttackRangePolicy.CoversDistance(
                        ability, info, distance, CurrentLevel, AbilityAttackCategory.None, false, false,
                        useMeleeApproachRange: true, personalSpaceDistance: _aiContext.PersonalSpaceDistance))
                        reason |= EnemyAbilityRejectReason.OutsideEffectiveMeleeRange;
                    bool hasMotion = ActorAbilityMotionResolver.TryResolve(_ownerActor, info, out _);
                    entries.Add(new JObject {
                        ["id"] = ability.abilityId, ["variant"] = variant.variantId,
                        ["category"] = info.attackCategory.ToString(), ["motion_key"] = info.motionKey.value,
                        ["eligible"] = activation == AbilityActivationResult.Success && reason == EnemyAbilityRejectReason.None && hasMotion,
                        ["can_activate"] = activation == AbilityActivationResult.Success,
                        ["cooldown_s"] = cooldown, ["activation"] = activation.ToString(),
                        ["rejection"] = reason.ToString(), ["has_motion"] = hasMotion
                    });
                }
            }
            return entries;
        }
    }
}
#endif
