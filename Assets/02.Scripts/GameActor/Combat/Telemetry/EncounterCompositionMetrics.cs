using System;
using UPlayGround.Data.Combat;
using UnityEngine;

namespace UPlayGround.Combat
{
    [Serializable]
    public struct IncomingHitCause
    {
        public float time;
        public string abilityId;
        public float damage;
        public bool offscreen;
        public bool telegraphed;
    }

    /// <summary>교전별 지표 원시 합계. 분모가 0인 비율은 측정되지 않은 값이다.</summary>
    [Serializable]
    public sealed class EncounterCompositionRecord
    {
        public CombatEncounterKind kind;
        public int comboStarts;
        public int comboActions;
        public int voluntaryThreatStops;
        public int defensiveCancels;
        public int targetTransitions;
        public float targetTransitionSeconds;
        public int dodgeCounterAttempts;
        public float dodgeCounterDamage;
        public int exposedAttackAttempts;
        public float exposedAttackDamage;
        public float lostControlSeconds;
        public int deathHitCount;
        public int deathOffscreenHits;
        public int deathTelegraphedHits;
        public int incomingOverflow;
        public IncomingHitCause[] lastDeathHits = new IncomingHitCause[64];
        public int lastDeathHitCount;
        public float AverageComboLength => comboStarts > 0 ? (float)comboActions / comboStarts : 0f;
        public float AverageTargetTransition => targetTransitions > 0 ? targetTransitionSeconds / targetTransitions : 0f;
        public float CounterDamageRatio => dodgeCounterAttempts > 0 && exposedAttackAttempts > 0 && exposedAttackDamage > 0f
            ? (dodgeCounterDamage / dodgeCounterAttempts) / (exposedAttackDamage / exposedAttackAttempts) : -1f;
    }

    /// <summary>미리 할당한 세 교전 버킷과 피격 링 버퍼로 전투 중 할당 없이 지표를 모은다.</summary>
    public sealed class EncounterCompositionMetrics
    {
        public readonly EncounterCompositionRecord[] Records =
        {
            new EncounterCompositionRecord { kind = CombatEncounterKind.Explore },
            new EncounterCompositionRecord { kind = CombatEncounterKind.Sweep },
            new EncounterCompositionRecord { kind = CombatEncounterKind.Duel },
        };
        private readonly IncomingHitCause[] _incoming = new IncomingHitCause[64];
        private readonly CombatEncounterKind[] _incomingKinds = new CombatEncounterKind[64];
        private int _incomingCount;
        private int _incomingCursor;
        private bool _comboActive;
        private CombatEncounterKind _comboKind;
        private float _lastAction = float.NegativeInfinity;
        private float _killTime = -1f;
        private int _killedTarget;
        private CombatEncounterKind _killKind;
        private bool _isCounter;
        private bool _isExposed;

        public void RecordAttack(CombatEncounterKind kind, float now, float comboGap, bool counter, bool exposed)
        {
            var record = Records[(int)kind];
            if (!_comboActive || _comboKind != kind || now - _lastAction > comboGap) record.comboStarts++;
            record.comboActions++;
            _comboActive = true; _comboKind = kind; _lastAction = now;
            _isCounter = counter; _isExposed = exposed && !counter;
            if (_isCounter) record.dodgeCounterAttempts++;
            if (_isExposed) record.exposedAttackAttempts++;
        }

        public void EndCombo(bool defensiveCancel)
        {
            if (_comboActive && defensiveCancel) Records[(int)_comboKind].defensiveCancels++;
            _comboActive = false;
        }

        public void RecordOutgoingHit(CombatEncounterKind kind, float now, int target, float damage)
        {
            var record = Records[(int)_comboKind];
            if (_isCounter) record.dodgeCounterDamage += damage;
            if (_isExposed) record.exposedAttackDamage += damage;
            if (_killTime < 0f || target == _killedTarget) return;
            Records[(int)_killKind].targetTransitions++;
            Records[(int)_killKind].targetTransitionSeconds += Mathf.Max(0f, now - _killTime);
            _killTime = -1f;
        }

        public void RecordKill(CombatEncounterKind kind, float now, int target)
        { _killTime = now; _killKind = kind; _killedTarget = target; }

        public void RecordIncoming(CombatEncounterKind kind, IncomingHitCause cause)
        {
            if (_incomingCount == _incoming.Length && cause.time - _incoming[_incomingCursor].time <= 5f)
                Records[(int)kind].incomingOverflow++;
            _incoming[_incomingCursor] = cause;
            _incomingKinds[_incomingCursor] = kind;
            _incomingCursor = (_incomingCursor + 1) % _incoming.Length;
            _incomingCount = Mathf.Min(_incomingCount + 1, _incoming.Length);
        }

        public void RecordDeath(float now)
        {
            foreach (var record in Records) record.lastDeathHitCount = 0;
            for (int i = 0; i < _incomingCount; i++)
            {
                int index = (_incomingCursor - _incomingCount + i + _incoming.Length) % _incoming.Length;
                var hit = _incoming[index];
                if (now - hit.time > 5f || now < hit.time) continue;
                var record = Records[(int)_incomingKinds[index]];
                record.deathHitCount++;
                if (hit.offscreen) record.deathOffscreenHits++;
                if (hit.telegraphed) record.deathTelegraphedHits++;
                record.lastDeathHits[record.lastDeathHitCount++] = hit;
            }
            _incomingCount = 0; _incomingCursor = 0; _comboActive = false; _killTime = -1f;
        }
    }
}
