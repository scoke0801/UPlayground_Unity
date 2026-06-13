using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.Manager.Handler;
using UPlayGround.MovementController;
using UPlayGround.UI;

namespace UPlayGround.Component
{
    /// <summary>
    /// 캐릭터 스왑 후 필드에 남는 모델 전용 히트 판정 실행체.
    /// PlayerCombat 상태와 보상 이벤트를 공유하지 않는다.
    /// </summary>
    public sealed class ResidualPlayerCombat : MonoBehaviour,
        IMotionEventCombatTarget,
        IFinishAttackMotionEventTarget,
        ISpecialBreakAttackMotionEventTarget
    {
        private readonly HashSet<IDamageable> _hitTargets = new();
        private readonly Collider[] _hitOverlapBuffer = new Collider[128];
        private readonly List<CombatHit> _detectedHits = new(32);
        private static readonly Func<Transform, bool> WarpDamageableFilter = static t =>
        {
            var d = t.GetComponent<IDamageable>() ?? t.GetComponentInParent<IDamageable>();
            return d != null && d.CanTakeDamage();
        };

        private PlayerActor _ownerPlayer;
        private AttackData _attackData;
        private AttackInfoBase _attackInfoBase;
        private IReadOnlyList<HitPhaseData> _hitPhases;
        private LayerMask _targetLayerMask = -1;
        private bool _isCollisionEnabled;
        private bool _allowHitStop = true;
        private CharacterActorType _ownerType;
        private float _feedbackMinInterval = 0.08f;
        private float _hitStopDuration = 0.04f;
        private float _hitStopTimeScale = 0.2f;
        private float _lastFeedbackTime = -999f;
        private bool _showCharacterOnDamageFloater;
        private MonsterActor _finishTarget;
        private MonsterActor _specialBreakTarget;
        private float _specialBreakDamageByMaxHpRate;
        private float _specialBreakFixedDamage;
        private float _specialBreakMinReferenceHealth;

        public event Action<AttackData> OnAttackHit;

        public void Initialize(
            PlayerResidualAttackSnapshot snapshot,
            bool allowHitStop,
            float feedbackMinInterval = 0.08f,
            float hitStopDuration = 0.04f,
            float hitStopTimeScale = 0.2f,
            bool showCharacterOnDamageFloater = false)
        {
            _ownerPlayer = snapshot.OwnerPlayer;
            _ownerType = snapshot.CharacterType;
            _attackData = CopyAttackData(snapshot.CurrentAttackData);
            _attackInfoBase = snapshot.CurrentAttackInfoBase;
            _hitPhases = snapshot.HitPhases;
            _targetLayerMask = snapshot.TargetLayerMask;
            _allowHitStop = allowHitStop;
            _feedbackMinInterval = Mathf.Max(0f, feedbackMinInterval);
            _hitStopDuration = Mathf.Max(0f, hitStopDuration);
            _hitStopTimeScale = Mathf.Clamp(hitStopTimeScale, 0.01f, 1f);
            _showCharacterOnDamageFloater = showCharacterOnDamageFloater;
            _finishTarget = snapshot.FinishTarget;
            _specialBreakTarget = snapshot.SpecialBreakTarget;
            _specialBreakDamageByMaxHpRate = snapshot.SpecialBreakDamageByMaxHpRate;
            _specialBreakFixedDamage = snapshot.SpecialBreakFixedDamage;
            _specialBreakMinReferenceHealth = snapshot.SpecialBreakMinReferenceHealth;
            _isCollisionEnabled = false;
            _hitTargets.Clear();

            Debug.Log($"[ResidualAttack] Combat initialized. owner={_ownerPlayer?.name}, character={_ownerType}, animKey={_attackData?.animKey}, kind={_attackData?.attackKind}, range={_attackData?.hitRange}, angle={_attackData?.hitAngle}, targetLayer={_targetLayerMask.value}, hitPhaseCount={_hitPhases?.Count ?? 0}, hasInfoBase={_attackInfoBase != null}, allowHitStop={_allowHitStop}");
        }

        private void Update()
        {
            if (_isCollisionEnabled)
                PerformHitDetection();
        }

        public void ClearHitTargets()
        {
            _hitTargets.Clear();
            Debug.Log("[ResidualAttack] Combat hit targets cleared.");
        }

        public void SetEnableCollision(bool enabled)
        {
            _isCollisionEnabled = enabled;
            Debug.Log($"[ResidualAttack] Combat collision {(enabled ? "ON" : "OFF")}. animKey={_attackData?.animKey}, range={_attackData?.hitRange}, angle={_attackData?.hitAngle}, targetLayer={_targetLayerMask.value}");
        }

        public void SetTargetLayerMask(LayerMask targetLayerMask) => _targetLayerMask = targetLayerMask;

        public WarpResolverContext BuildWarpResolverContext()
        {
            if (_attackData == null) return default;

            return new WarpResolverContext
            {
                origin = transform,
                hitRange = _attackData.hitRange,
                hitAngle = _attackData.hitAngle,
                targetLayer = _targetLayerMask,
                targetFilter = WarpDamageableFilter,
            };
        }

        public void SetHitPhaseIndex(int hitPhaseIndex)
        {
            if (_attackData == null)
            {
                Debug.LogWarning($"[ResidualAttack] Hit phase skipped: attack data is null. phase={hitPhaseIndex}");
                return;
            }

            var phase = _attackInfoBase != null
                ? _attackInfoBase.GetHitPhase(hitPhaseIndex)
                : GetHitPhase(_hitPhases, hitPhaseIndex);
            if (phase == null)
            {
                Debug.LogWarning($"[ResidualAttack] Hit phase skipped: phase missing. phase={hitPhaseIndex}, hasInfoBase={_attackInfoBase != null}, hitPhaseCount={_hitPhases?.Count ?? 0}");
                return;
            }

            _attackData.hitPhaseIndex = hitPhaseIndex;
            _attackData.damage = UPlayGround.Util.ApplyRandomValue(phase.damage, -0.2f, 0.2f);
            _attackData.poiseDamage = phase.poiseDamage;
            _attackData.breakDamage = phase.breakDamage;
            _attackData.reactionDuration = phase.reactionDuration;
            _attackData.forceReaction = phase.forceReaction;
            _attackData.forceBreakExpose = phase.forceBreakExpose;
            _attackData.reactionType = phase.reactionType;
            _attackData.hitRange = phase.attackRadius;
            _attackData.hitHeightOffset = phase.attackOffset.y;
            _attackData.hitHeightRange = phase.hitHeightRange;
            _attackData.hitParticleName = phase.hitParticleName;
            _attackData.pullForce = phase.pullForce;
            _attackData.airborneForce = phase.airborneForce;
            _attackData.knockbackForce = phase.knockBackForce;
            _attackData.knockbackDrag = phase.knockBackDrag;
            _attackData.victimForcedAnimKey = phase.victimForcedAnimKey;
            _attackData.guaranteedReaction = phase.guaranteedReaction;
            _attackData.reactionData = phase.reactionProfile?.Resolve();

            Debug.Log($"[ResidualAttack] Hit phase applied. phase={hitPhaseIndex}, damage={_attackData.damage}, range={_attackData.hitRange}, angle={_attackData.hitAngle}, heightOffset={_attackData.hitHeightOffset}, heightRange={_attackData.hitHeightRange}");
        }

        public void ApplyFinishAttackFromMotionEvent()
        {
            var ownerCombat = _ownerPlayer != null ? _ownerPlayer.GetCombat() : null;
            if (_finishTarget == null ||
                ownerCombat == null ||
                !ownerCombat.IsFinishableTarget(_finishTarget.transform, requirePositionCheck: false))
            {
                Debug.LogWarning($"[ResidualAttack] Finish event skipped. target={_finishTarget != null}, ownerCombat={ownerCombat != null}");
                return;
            }

            Debug.Log($"[ResidualAttack] Finish event applied. target={_finishTarget.name}");
            _finishTarget.OnTakeFinishAttack(transform.forward);
        }

        public void ApplySpecialBreakAttackFromMotionEvent()
        {
            if (_specialBreakTarget == null || !_specialBreakTarget.IsAlive())
            {
                Debug.LogWarning($"[ResidualAttack] SpecialBreak event skipped. target={_specialBreakTarget != null}, alive={_specialBreakTarget != null && _specialBreakTarget.IsAlive()}");
                return;
            }

            Debug.Log($"[ResidualAttack] SpecialBreak event applied. target={_specialBreakTarget.name}, rate={_specialBreakDamageByMaxHpRate}, fixed={_specialBreakFixedDamage}");
            _specialBreakTarget.OnTakeSpecialBreakAttack(
                _ownerPlayer,
                _specialBreakDamageByMaxHpRate,
                _specialBreakFixedDamage,
                _specialBreakMinReferenceHealth);
        }

        private void PerformHitDetection()
        {
            if (_attackData == null)
            {
                Debug.LogWarning("[ResidualAttack] Hit detection skipped: attack data is null.");
                return;
            }

            Vector3 origin = transform.position + Vector3.up * _attackData.hitHeightOffset;
            var hitShape = new MeleeHitShape(
                transform,
                origin,
                transform.forward,
                _attackData.hitRange,
                _attackData.hitAngle,
                _attackData.hitHeightRange,
                _targetLayerMask);
            CombatHitDetector.DetectMeleeHits(hitShape, _hitOverlapBuffer, _hitTargets, _detectedHits);

            bool hitOccurred = false;
            Vector3 firstHitPoint = Vector3.zero;
            Vector3 firstHitDir = Vector3.zero;
            GameObject firstHitTarget = null;

            _attackData.attacker = _ownerPlayer;

            foreach (CombatHit hit in _detectedHits)
            {
                _attackData.hitTarget = hit.HitObject;
                _attackData.hitPoint = hit.HitPoint;
                _attackData.attackDirection = hit.AttackDirection;

                _hitTargets.Add(hit.Damageable);
                hit.Damageable.TakeDamage(_attackData);
                ShowDamageFloater(_attackData);
                GameObjectManager.Instance?.ShowFX(GetHitFxKey(_attackData), hit.HitPoint);
                OnAttackHit?.Invoke(_attackData);
                _ownerPlayer?.GetCombat()?.NotifyAttackHit(_attackData);
                Debug.Log($"[ResidualAttack] Hit applied. target={hit.HitObject?.name}, damage={_attackData.damage}, phase={_attackData.hitPhaseIndex}, point={hit.HitPoint}");

                if (!hitOccurred)
                {
                    hitOccurred = true;
                    firstHitPoint = hit.HitPoint;
                    firstHitDir = hit.AttackDirection;
                    firstHitTarget = hit.HitObject;
                }
            }

            if (hitOccurred)
            {
                _attackData.hitTarget = firstHitTarget;
                _attackData.hitPoint = firstHitPoint;
                _attackData.attackDirection = firstHitDir;
                ApplyHitFeedback();
            }
        }

        private static AttackData CopyAttackData(AttackData source)
        {
            if (source == null) return null;

            return new AttackData
            {
                animKey = source.animKey,
                damage = source.damage,
                poiseDamage = source.poiseDamage,
                breakDamage = source.breakDamage,
                reactionDuration = source.reactionDuration,
                forceReaction = source.forceReaction,
                forceBreakExpose = source.forceBreakExpose,
                interruptActions = source.interruptActions,
                attackKind = source.attackKind,
                reactionType = source.reactionType,
                attacker = source.attacker,
                hitRange = source.hitRange,
                hitAngle = source.hitAngle,
                hitHeightOffset = source.hitHeightOffset,
                hitHeightRange = source.hitHeightRange,
                hitPoint = source.hitPoint,
                hitTarget = source.hitTarget,
                criticalMultiplier = source.criticalMultiplier,
                isCounterAttack = source.isCounterAttack,
                useCounterHitFeedback = source.useCounterHitFeedback,
                attackDirection = source.attackDirection,
                hitParticleName = source.hitParticleName,
                defenseType = source.defenseType,
                pullForce = source.pullForce,
                airborneForce = source.airborneForce,
                knockbackForce = source.knockbackForce,
                knockbackDrag = source.knockbackDrag,
                grabDuration = source.grabDuration,
                victimForcedAnimKey = source.victimForcedAnimKey,
                guaranteedReaction = source.guaranteedReaction,
                hitPhaseIndex = source.hitPhaseIndex,
                reactionData = source.reactionData,
            };
        }

        private static HitPhaseData GetHitPhase(IReadOnlyList<HitPhaseData> phases, int index)
        {
            if (phases == null || phases.Count == 0) return null;
            return phases[Mathf.Clamp(index, 0, phases.Count - 1)];
        }

        private void ShowDamageFloater(AttackData attackData)
        {
            var style = attackData.attackKind is AttackKind.HeavyAttack
                                              or AttackKind.SkillAttack
                                              or AttackKind.FinishAttack
                                              or AttackKind.ChargeAttack
                ? FloatStyle.Critical
                : FloatStyle.Normal;

            if (_showCharacterOnDamageFloater && _ownerType != CharacterActorType.None)
            {
                string label = $"{_ownerType} {Mathf.RoundToInt(attackData.damage)}";
                UIManager.Instance?.ShowDamageFloaterLabel(attackData.hitPoint, label, style);
            }
            else
            {
                UIManager.Instance?.ShowDamageFloater(attackData.hitPoint, attackData.damage, style);
            }
        }

        private void ApplyHitFeedback()
        {
            if (!_allowHitStop || GameCombatManager.Instance == null) return;
            if (_feedbackMinInterval > 0f && Time.unscaledTime - _lastFeedbackTime < _feedbackMinInterval)
                return;

            _lastFeedbackTime = Time.unscaledTime;
            if (_hitStopDuration > 0f)
                GameCombatManager.Instance.GameHitStop.Execute(_hitStopDuration, _hitStopTimeScale);
        }

        private static string GetHitFxKey(AttackData attackData)
        {
            return !string.IsNullOrWhiteSpace(attackData?.hitParticleName)
                ? attackData.hitParticleName
                : FXKeyType.DefaultCombatHit.ToKey();
        }
    }
}
