using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround.Combat
{
    /// <summary>기존 전투·상태 이벤트를 교전 지표에 연결한다. 의도 판정은 관찰자의 명시적 기록으로 남긴다.</summary>
    public static class EncounterCompositionTelemetry
    {
        public static EncounterCompositionMetrics Metrics { get; private set; } = new EncounterCompositionMetrics();
        private static float _lastDodge = float.NegativeInfinity;
        private static float _controlLostAt = -1f;
        private static CombatEncounterKind _controlKind;
        private static bool _pendingDeath;
        public static CombatEncounterKind CurrentKind => Services.TryGet<ICombatEncounterService>(out var service)
            ? service.CurrentEncounter : CombatEncounterKind.Explore;

        public static void Reset()
        { Metrics = new EncounterCompositionMetrics(); _lastDodge = float.NegativeInfinity; _controlLostAt = -1f; _pendingDeath = false; }

        public static void MarkPlayerDeath() => _pendingDeath = true;

        public static void FinishObservation()
        {
            if (_controlLostAt >= 0f)
            { Metrics.Records[(int)_controlKind].lostControlSeconds += Time.time - _controlLostAt; _controlLostAt = Time.time; }
            if (_pendingDeath) { Metrics.RecordDeath(Time.time); _pendingDeath = false; }
        }

        public static void RecordAttack(PlayerActor player)
        {
            if (!CombatTelemetrySession.Enabled || player == null) return;
            float gap = Services.TryGet<ICombatEncounterService>(out var service) && service.EncounterProfile != null
                ? service.EncounterProfile.continuousHitGap : 1.5f;
            bool exposed = false;
            if (Services.TryGet<IActorQueryService>(out var query) && query.AllActors is IReadOnlyList<IWorldActor> actors)
                for (int i = 0; i < actors.Count; i++)
                    if (actors[i] is MonsterActor monster && monster.IsAlive() && monster.Detection?.CurrentTarget == player.transform
                        && monster.ActorController?.CurrentState?.StateId == ActorStateId.Attack) { exposed = true; break; }
            Metrics.RecordAttack(CurrentKind, Time.time, gap, Time.time - _lastDodge <= gap, exposed);
            _lastDodge = float.NegativeInfinity;
        }

        public static void RecordState(ActorStateId state)
        {
            if (!CombatTelemetrySession.Enabled) return;
            bool lost = state is ActorStateId.Hit or ActorStateId.Stun or ActorStateId.Knockdown or ActorStateId.Grabbed or ActorStateId.GuardBreak;
            if (lost && _controlLostAt < 0f) { _controlLostAt = Time.time; _controlKind = CurrentKind; }
            if (!lost && _controlLostAt >= 0f)
            { Metrics.Records[(int)_controlKind].lostControlSeconds += Time.time - _controlLostAt; _controlLostAt = -1f; }
            if (state != ActorStateId.Attack && state != ActorStateId.Charge && state != ActorStateId.DashAttack
                && state != ActorStateId.JumpAttack && state != ActorStateId.Ultimate)
                Metrics.EndCombo(state is ActorStateId.Dodge or ActorStateId.Guard or ActorStateId.Dash);
        }

        public static void RecordVoluntaryThreatStop()
        {
            if (CombatTelemetrySession.Enabled) Metrics.Records[(int)CurrentKind].voluntaryThreatStops++;
        }

        public static void Observe(CombatResult result)
        {
            if (!CombatTelemetrySession.Enabled) return;
            if (result.Victim is PlayerActor && result.DefenseOutcome == DefenseOutcome.PerfectDodge) _lastDodge = Time.time;
            if (!result.DamageApplied || result.FinalDamage <= 0f) return;
            if (result.Attacker is PlayerActor && result.Victim is MonsterActor)
                Metrics.RecordOutgoingHit(CurrentKind, Time.time, result.Victim.GetInstanceID(), result.FinalDamage);
            if (result.Victim is not PlayerActor) return;
            bool offscreen = false;
            if (result.Attacker != null && Services.TryGet<ICameraViewService>(out var cameraService))
            {
                var camera = cameraService.GetMainCamera();
                if (camera != null)
                {
                    var point = camera.WorldToViewportPoint(result.Attacker.transform.position);
                    offscreen = point.z <= 0f || point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f;
                }
            }
            Metrics.RecordIncoming(CurrentKind, new IncomingHitCause
            {
                time = Time.time, abilityId = result.Hit.AbilityId, damage = result.FinalDamage, offscreen = offscreen,
                telegraphed = result.Attacker is MonsterActor monster && monster.Combat?.HasTelegraphedCurrentAttack == true,
            });
            // 사망 훅은 최종 CombatResult 발행보다 먼저 오므로 마지막 치명타까지 기록한 뒤 닫는다.
            if (_pendingDeath) { Metrics.RecordDeath(Time.time); _pendingDeath = false; }
        }
    }
}
