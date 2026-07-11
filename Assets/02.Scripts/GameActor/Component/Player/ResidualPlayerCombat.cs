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

namespace UPlayGround.Components
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
        private CombatHitboxSet _hitboxSet;
        private string _requestedHitboxGroupId;
        private IReadOnlyList<string> _requestedHitboxGroupIds;
        private float _homingReachRange;
        private float _homingReachAngle;
        private float _warpSearchRange;

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
            _homingReachRange = snapshot.HomingReachRange;
            _homingReachAngle = snapshot.HomingReachAngle;
            _warpSearchRange = snapshot.WarpSearchRange;
            _isCollisionEnabled = false;
            _hitTargets.Clear();
            _hitboxSet = gameObject.GetOrAddComponent<CombatHitboxSet>();
            _hitboxSet.Refresh();

            Debug.Log($"[ResidualAttack] Combat initialized. owner={_ownerPlayer?.name}, character={_ownerType}, animKey={_attackData?.animKey}, kind={_attackData?.attackKind}, homingReach={_homingReachRange}/{_homingReachAngle}, targetLayer={_targetLayerMask.value}, hitPhaseCount={_hitPhases?.Count ?? 0}, hasInfoBase={_attackInfoBase != null}, allowHitStop={_allowHitStop}");
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
            if (enabled)
                BeginHitboxWindow();
            else
            {
                _hitboxSet?.EndGroup();
                // 윈도우 종료 시 그룹 요청을 비워 다음 윈도우에 직전 공격의 그룹이 잔존하지 않게 한다.
                _requestedHitboxGroupId = null;
                _requestedHitboxGroupIds = null;
            }
            _isCollisionEnabled = enabled;
            Debug.Log($"[ResidualAttack] Combat collision {(enabled ? "ON" : "OFF")}. animKey={_attackData?.animKey}, group={_hitboxSet?.ActiveGroupId ?? "-"}, targetLayer={_targetLayerMask.value}");
        }

        public void SetTargetLayerMask(LayerMask targetLayerMask) => _targetLayerMask = targetLayerMask;

        public void SetHitboxGroup(string hitboxGroupId)
        {
            _requestedHitboxGroupId = string.IsNullOrWhiteSpace(hitboxGroupId)
                ? null
                : hitboxGroupId.Trim();
            _requestedHitboxGroupIds = null;
        }

        public void SetHitboxGroups(IReadOnlyList<string> hitboxGroupIds)
        {
            _requestedHitboxGroupIds = hitboxGroupIds != null && hitboxGroupIds.Count > 0
                ? hitboxGroupIds
                : null;
        }

        public WarpResolverContext BuildWarpResolverContext()
        {
            if (_attackData == null) return default;

            return new WarpResolverContext
            {
                origin = transform,
                targetingRange = _homingReachRange,
                // searchRange를 명시하지 않으면 resolver가 targetingRange를 OverlapSphere 반경으로 폴백한다.
                // reach(작은 값)를 검색 반경으로 쓰면 잔상 워프가 대상을 못 찾으므로 캡처한 검색 반경을 명시한다.
                searchRange = _warpSearchRange,
                targetingAngle = _homingReachAngle,
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
            _attackData.hitParticleName = phase.hitParticleName;
            _attackData.pullForce = phase.pullForce;
            _attackData.airborneForce = phase.airborneForce;
            _attackData.knockbackForce = phase.knockBackForce;
            _attackData.knockbackDrag = phase.knockBackDrag;
            _attackData.victimForcedAnimKey = phase.victimForcedAnimKey;
            _attackData.guaranteedReaction = phase.guaranteedReaction;
            _attackData.reactionData = phase.reactionProfile?.Resolve();

            Debug.Log($"[ResidualAttack] Hit phase applied. phase={hitPhaseIndex}, damage={_attackData.damage}, group={phase.hitboxGroupId}");
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

            if (_hitboxSet == null || !_hitboxSet.IsActive)
                return;

            _hitboxSet.DetectActiveGroup(
                transform,
                _targetLayerMask,
                _hitTargets,
                _detectedHits,
                includeInvincibleTargets: false);

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
                CombatResult result = hit.Damageable.ReceiveHit(HitRequest.FromAttackData(_attackData));
                ShowDamageFloater(result);
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

        private void BeginHitboxWindow()
        {
            HitPhaseData phase = _attackInfoBase != null
                ? _attackInfoBase.GetHitPhase(_attackData?.hitPhaseIndex ?? 0)
                : GetHitPhase(_hitPhases, _attackData?.hitPhaseIndex ?? 0);
            string groupId = !string.IsNullOrWhiteSpace(_requestedHitboxGroupId)
                ? _requestedHitboxGroupId
                : phase?.hitboxGroupId;
            List<string> groupIds = HitboxGroupIds.Normalize(groupId, _requestedHitboxGroupIds);
            bool activated;
            if (_hitboxSet == null)
                activated = false;
            else if (groupIds != null && groupIds.Count > 0)
                activated = _hitboxSet.BeginGroups(groupIds);
            else
                activated = _hitboxSet.BeginGroup(groupId);

            if (!activated)
            {
                Debug.LogError(
                    $"[ResidualAttack] 필수 HitBox 그룹 '{HitboxGroupIds.Describe(groupId, groupIds)}'을 찾지 못해 공격 판정을 중단합니다.",
                    this);
            }
        }

        private void ShowDamageFloater(in CombatResult result)
        {
            if (!result.DamageApplied || result.FinalDamage <= 0f)
                return;

            if (_showCharacterOnDamageFloater && _ownerType != CharacterActorType.None)
            {
                string label = $"{_ownerType} {Mathf.RoundToInt(result.FinalDamage)}";
                UIManager.Instance?.ShowDamageFloaterLabel(result.Hit.HitPoint, label, result.FloaterStyle);
            }
            else
            {
                UIManager.Instance?.ShowDamageFloater(
                    result.Hit.HitPoint,
                    result.FinalDamage,
                    result.FloaterStyle);
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
