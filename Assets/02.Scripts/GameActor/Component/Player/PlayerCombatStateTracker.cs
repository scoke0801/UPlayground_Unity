using System;
using UnityEngine;

namespace UPlayGround.Components
{
    public class PlayerCombatStateTracker : PlayerActorComponent
    {
        private readonly Collider[] _threatOverlapBuffer = new Collider[128];

        private float _combatStateDuration = 30f;
        private float _threatDetectionRange = 20f;
        private float _threatCheckInterval = 0.5f;
        private LayerMask _threatLayerMask = -1;
        private float _lastCombatEventTime = -999f;
        private bool _cachedCombatState;
        private float _threatCheckTimer;

        public event Action<bool> OnChangeCombatState;

        public bool IsInCombat => Time.time - _lastCombatEventTime < _combatStateDuration;

        public void Configure(
            float combatStateDuration,
            float threatDetectionRange,
            float threatCheckInterval,
            LayerMask threatLayerMask)
        {
            _combatStateDuration = Mathf.Max(0f, combatStateDuration);
            _threatDetectionRange = Mathf.Max(0f, threatDetectionRange);
            _threatCheckInterval = Mathf.Max(0.01f, threatCheckInterval);
            _threatLayerMask = threatLayerMask;
        }

        public void Tick()
        {
            _threatCheckTimer += Time.deltaTime;
            if (_threatCheckTimer >= _threatCheckInterval)
            {
                _threatCheckTimer = 0f;
                if (HasThreatNearby())
                    NotifyCombatEvent();
            }

            bool current = IsInCombat;
            if (_cachedCombatState == current)
                return;

            _cachedCombatState = current;
            OnChangeCombatState?.Invoke(current);
        }

        public void NotifyCombatEvent()
        {
            _lastCombatEventTime = Time.time;
        }

        public void ForceExitCombat()
        {
            if (!IsInCombat)
                return;

            _lastCombatEventTime = -999f;
        }

        private bool HasThreatNearby()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _threatDetectionRange,
                _threatOverlapBuffer,
                _threatLayerMask);

            if (hitCount == _threatOverlapBuffer.Length)
            {
                Collider[] saturatedHits = Physics.OverlapSphere(
                    transform.position,
                    _threatDetectionRange,
                    _threatLayerMask);
                return ContainsAggroThreat(saturatedHits, saturatedHits.Length);
            }

            return ContainsAggroThreat(_threatOverlapBuffer, hitCount);
        }

        private static bool ContainsAggroThreat(Collider[] hits, int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;

                MonsterActor monster = hit.GetComponent<MonsterActor>()
                                      ?? hit.GetComponentInParent<MonsterActor>();
                if (monster?.AIController != null && monster.AIController.HasAggroTarget)
                    return true;
            }

            return false;
        }
    }
}
