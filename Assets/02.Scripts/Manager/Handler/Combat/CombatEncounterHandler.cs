using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.State;

namespace UPlayGround.Manager.Handler
{
    /// <summary>기존 전투 매니저 수명 안에서 교전의 유일한 판정을 수행한다.</summary>
    public sealed class CombatEncounterHandler
    {
        private readonly CombatEncounterState _state = new CombatEncounterState();
        private readonly HashSet<string> _arenas = new HashSet<string>();
        private float _elapsed;
        public CombatEncounterProfileSO Profile { get; }
        public CombatEncounterKind Current => _state.Current;
        public bool IsBossAttacking { get; private set; }
        public event Action<CombatEncounterKind> Changed;

        public CombatEncounterHandler(CombatEncounterProfileSO profile) { Profile = profile; }

        public void SetArenaActive(string arenaId, bool active)
        {
            if (string.IsNullOrWhiteSpace(arenaId)) return;
            if (active) _arenas.Add(arenaId); else _arenas.Remove(arenaId);
        }

        public void Tick(float deltaTime)
        {
            if (Profile == null || deltaTime <= 0f) return;
            _elapsed += deltaTime;
            if (_elapsed < Profile.detectionInterval) return;
            float elapsed = _elapsed;
            _elapsed = 0f;
            if (!Services.TryGet<IActorQueryService>(out var query)) return;
            var player = query.Player as PlayerActor;
            bool bossInRange = false;
            IsBossAttacking = false;
            if (player != null && query.AllActors is IReadOnlyList<IWorldActor> actors)
            {
                for (int i = 0; i < actors.Count; i++)
                {
                    if (actors[i] is not MonsterActor boss || !boss.gameObject.activeInHierarchy
                        || boss.Grade != MonsterActorGrade.Boss || !boss.IsAlive()) continue;
                    if (boss.Detection == null || boss.Detection.CurrentTarget != player.transform) continue;
                    bool inRange = (boss.transform.position - player.transform.position).sqrMagnitude <= Profile.bossRadius * Profile.bossRadius;
                    bossInRange |= inRange;
                    var state = boss.ActorController?.CurrentState?.StateId ?? ActorStateId.None;
                    if (inRange || Current == CombatEncounterKind.Duel)
                        IsBossAttacking |= state is ActorStateId.Attack or ActorStateId.Flying_Dive or ActorStateId.Flying_GroundAttack;
                }
            }
            if (_state.Evaluate(player != null && player.IsInCombat, _arenas.Count > 0,
                bossInRange, IsBossAttacking, Profile.releaseHoldSeconds, elapsed)) Changed?.Invoke(Current);
        }

        public void Reset()
        {
            bool changed = Current != CombatEncounterKind.Explore;
            _arenas.Clear(); _state.Reset(); _elapsed = 0f; IsBossAttacking = false;
            if (changed) Changed?.Invoke(Current);
        }
    }
}
