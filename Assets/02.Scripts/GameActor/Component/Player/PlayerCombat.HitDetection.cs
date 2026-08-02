using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Manager;
using UPlayGround.UI;
using UPlayGround.Input;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using UPlayGround.Debugging;

namespace UPlayGround.Components
{
    public partial class PlayerCombat : PlayerActorComponent,
        UPlayGround.Combat.ICombatCollisionExecutor,
        UPlayGround.Combat.ICollisionAnchorProvider,
        IDebugGizmoProvider
    {
        #region Hit Detection

        public void ClearHitTargets() => _hitTargets.Clear();

        public void PerformHitDetection()
        {
            if (_currentAttackData == null)
            {
                Debug.LogWarning("[PlayerCombat] 현재 공격 정보가 없습니다.");
                return;
            }

            if (_actionRunner != null
                && _actionRunner.IsCollisionActive
                && _currentAttackData.hitPhaseIndex != _actionRunner.CurrentPhaseIndex)
            {
                SetHitPhaseIndex(_actionRunner.CurrentPhaseIndex);
            }

            // 판정 소스는 배타적이다 — 명시적 세션이 열려 있으면 부착형 그룹을 질의하지 않는다.
            bool explicitActive = _collisionSession.ShouldDetect();
            if (!explicitActive && (_hitboxSet == null || !_hitboxSet.IsActive))
                return;

            // 프레임당 1회만 검출한다. LateUpdate 폴링과 애니메이션 이벤트(OnAnimationEvent_HitCheck)가
            // 같은 프레임에 함께 들어오면 스윕 기준 형상이 이중 커밋되어 스윕 구간을 잃기 때문이다.
            if (_lastHitDetectionFrame == Time.frameCount)
                return;
            _lastHitDetectionFrame = Time.frameCount;

            if (explicitActive)
            {
                _collisionSession.Detect(
                    transform,
                    _targetLayerMask,
                    _hitTargets,
                    _detectedHits,
                    includeInvincibleTargets: false);

                // OnceOnBegin은 시작 시점 1회로 끝난다. 이후 프레임에서 다시 질의하지 않는다.
                if (_collisionSession.Evaluation == CollisionEvaluationType.OnceOnBegin)
                    _collisionSession.MarkConsumed();
            }
            else
            {
                _hitboxSet.DetectActiveGroup(
                    transform,
                    _targetLayerMask,
                    _hitTargets,
                    _detectedHits,
                    includeInvincibleTargets: false);
            }

            // 첫 번째 히트 정보만 피드백(킬캠 등)에 사용
            bool    hitOccurred   = false;
            Vector3 firstHitPoint = Vector3.zero;
            Vector3 firstHitDir   = Vector3.zero;
            GameObject firstHitTarget = null;

            _currentAttackData.attacker = _playerActor;

            foreach (CombatHit hit in _detectedHits)
            {
                // 공유 AttackData에 퍼-타겟 정보 기록 (TakeDamage 및 이벤트 수신자 참조용)
                _currentAttackData.hitTarget       = hit.HitObject;
                _currentAttackData.hitPoint        = hit.HitPoint;
                _currentAttackData.attackDirection = hit.AttackDirection;

                _hitTargets.Add(hit.Damageable);
                CombatResult result = hit.Damageable.ReceiveHit(HitRequest.FromAttackData(_currentAttackData));
                ShowAttackHitFeedback(result);
                OnAttackHit?.Invoke(_currentAttackData);

                if (!hitOccurred)
                {
                    hitOccurred      = true;
                    firstHitPoint    = hit.HitPoint;
                    firstHitDir      = hit.AttackDirection;
                    firstHitTarget   = hit.HitObject;
                }
            }

            if (hitOccurred)
            {
                // 피드백(킬캠, 히트스톱)은 첫 번째 히트 기준으로 적용
                _currentAttackData.hitTarget       = firstHitTarget;
                _currentAttackData.hitPoint        = firstHitPoint;
                _currentAttackData.attackDirection = firstHitDir;
                ApplyHitFeedback();
            }
        }

        private void ShowAttackHitFeedback(in CombatResult result)
            => _presenter?.ShowHit(result);

        private void ApplyHitFeedback()
        {
            _presenter?.ApplyImpact(_currentAttackData, IsParryCounterAvailable);
        }

        // ── P4: 외부 소스(투사체/AOE) attacker-side 피드백 통일 ──────────────
        // 근접과 동일한 연출 정책(CombatFeedbackDispatcher)을 외부 공격이 재사용한다.
        // _currentAttackData가 아니라 전달된 attackData로 동작해 투사체/AOE의 실제 공격 정보를 반영한다.

        /// <summary>외부 소스의 단일 히트 연출 — 데미지 숫자 + 히트 VFX. 대상마다 호출한다.</summary>
        public void ShowExternalHitFeedback(in CombatResult result)
        {
            ShowAttackHitFeedback(result);
        }

        /// <summary>외부 소스의 임팩트 연출 — 히트스톱/카메라/바이탈오브/킬캠. 공격 1회당 호출(AOE는 1회로 제한).</summary>
        public void ApplyExternalAttackImpact(AttackData attackData)
        {
            if (attackData == null) return;
            _presenter?.ApplyImpact(attackData, IsParryCounterAvailable);
        }

        private PlayerAttackHitFeedbackProfile CreatePlayerAttackHitFeedbackProfile()
        {
            return new PlayerAttackHitFeedbackProfile(
                _punchStrengthLight,
                _punchStrengthHeavy,
                _punchStrengthSkill,
                _punchDurationLight,
                _punchDurationHeavy,
                _punchDurationSkill,
                _shakeKeyLight,
                _shakeKeyHeavy);
        }

        #endregion
    }
}
