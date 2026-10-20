using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Combat;
using UPlayGround.Data.Ability;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround
{
    public partial class MonsterActor
    {
        private void OnEliteBreakTrigger(AbilityTriggerRequest request)
        {
            if (request.TriggerTag != GameplayTags.Trigger_Monster_EliteBreak || Grade != MonsterActorGrade.Elite
                || request.Variant?.executionPayload is not EliteBreakShockwavePayloadSO payload) return;
            var prepared = Abilities.TryPrepareAbility(request.Ability, true, null, out var handle, out _, request.TriggerEvent);
            if (prepared != AbilityActivationResult.Success) { Abilities.ReportTriggerRejected(request.Ability, prepared); return; }
            var committed = Abilities.Commit(handle);
            if (committed != AbilityActivationResult.Success)
            { Abilities.Abort(handle); Abilities.ReportTriggerRejected(request.Ability, committed); return; }
            Abilities.BindActiveExecutionToTrigger(handle, request);
            try
            {
                var source = request.TriggerEvent?.Payload as GameActor;
                ApplyEliteBreakShockwave(payload, source);
                Abilities.EndAbility(handle, true);
            }
            catch { Abilities.EndAbility(handle, false); throw; }
        }

        private void ApplyEliteBreakShockwave(EliteBreakShockwavePayloadSO payload, GameActor source)
        {
            if (source == null || !Services.TryGet<IActorQueryService>(out var query)
                || query.AllActors is not IReadOnlyList<IWorldActor> actors) return;
            var attack = new AttackData
            {
                attacker = source, damage = 0f, poiseDamage = 0f, breakDamage = 0f,
                reactionType = payload.reaction, reactionDuration = payload.reactionSeconds,
                forceReaction = true, guaranteedReaction = true, abilityId = "Monster.EliteBreak.Shockwave",
            };
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] is not MonsterActor victim || victim == this || !victim.gameObject.activeInHierarchy || !victim.IsAlive()
                    || (victim.Grade != MonsterActorGrade.Weak && victim.Grade != MonsterActorGrade.Normal)) continue;
                if ((victim.transform.position - transform.position).sqrMagnitude > payload.radius * payload.radius) continue;
                if (!CombatRelationUtility.CanDamage(source, victim)) continue;
                attack.hitTarget = victim.gameObject;
                attack.hitPoint = victim.transform.position;
                attack.attackDirection = victim.transform.position - transform.position;
                if (!victim.CanTakeDamage()) continue;
                // 피해 없는 군중 제어이므로 가드·피해 피드백을 거치지 않고 기존 리액션 Ability에 전달한다.
                var hit = HitContext.Create(HitRequest.FromAttackData(attack), victim);
                var reaction = ReactionResolver.ResolveMonsterReaction(new MonsterReactionQuery(false,
                    victim.CanPlayHitReaction(hit), victim.ShouldEnterAirborneState(hit), victim.CanEnterKnockdownState(hit),
                    victim.Grade, victim.Definition?.EffectiveCombatReactionPolicy), hit);
                if (!reaction.ShouldEnterState) continue;
                victim.Abilities?.IssueTriggerEvent(ResolveMonsterHitTrigger(hit, reaction), source, victim,
                    new HitReactionTriggerPayload(hit, reaction.TargetState));
            }
            if (payload.presentation == null) return;
            if (!string.IsNullOrEmpty(payload.presentation.visualEffectKey))
                ActorSvc.Objects?.ShowFX(payload.presentation.visualEffectKey, transform.position);
            if (payload.presentation.cameraEffect != null)
                CameraManager.Instance?.PlayEffect(payload.presentation.cameraEffect);
        }
    }
}
