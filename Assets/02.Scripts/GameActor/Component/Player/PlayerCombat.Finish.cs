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
using UPlayGround.Data.Event;

namespace UPlayGround.Components
{
    public partial class PlayerCombat : PlayerActorComponent, UPlayGround.Combat.ICombatCollisionExecutor, IDebugGizmoProvider
    {
        #region Finish Attack

        public bool IsFinishableTarget(Transform target, bool requirePositionCheck = true)
        {
            MonsterActor monsterActor = ResolveFinishTarget(target);
            if (monsterActor == null || !monsterActor.CanTakeDamage()) return false;
            if (monsterActor.Grade == MonsterActorGrade.Weak) return false;
            if (monsterActor.Grade == MonsterActorGrade.Boss && !_finishAttackAllowBoss) return false;
            if (monsterActor.GetCurrentHealth() > GetFinishAttackHealthThreshold(monsterActor)) return false;
            if (RequiresFinishAttackBreakState(monsterActor) && !IsInFinishAttackBreakState(monsterActor)) return false;

            if (!requirePositionCheck)
                return true;

            if (GetAttackWarpTarget(monsterActor.transform, _finishAttackSearchRange) == null)
                return false;

            Vector3 dir = monsterActor.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > _finishAttackSearchRange * _finishAttackSearchRange) return false;
            if (dir.sqrMagnitude <= 0.001f) return true;

            return Vector3.Angle(transform.forward, dir) <= _finishAttackSearchAngle;
        }

        public Transform FindFinishableTarget()
        {
            Vector3    origin  = transform.position;
            Collider[] hits    = Physics.OverlapSphere(origin, _finishAttackSearchRange, _targetLayerMask);

            Transform bestTarget   = null;
            float     bestDistSq   = float.MaxValue;
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                MonsterActor monsterActor = ResolveFinishTarget(hit.transform);
                if (monsterActor == null || !IsFinishableTarget(monsterActor.transform)) continue;

                Vector3 dir = monsterActor.transform.position - origin;
                dir.y = 0f;

                if (lockOnTarget != null &&
                    (monsterActor.transform == lockOnTarget || lockOnTarget.IsChildOf(monsterActor.transform)))
                    return monsterActor.transform;

                float distSq = dir.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = monsterActor.transform;
                }
            }
            return bestTarget;
        }

        private static MonsterActor ResolveFinishTarget(Transform target)
        {
            if (target == null) return null;
            return target.GetComponent<MonsterActor>()
                   ?? target.GetComponentInParent<MonsterActor>();
        }

        private float GetFinishAttackHealthThreshold(MonsterActor monsterActor)
        {
            float maxHealth = Mathf.Max(1f, monsterActor.MaxHealth);
            float healthRate = monsterActor.Grade switch
            {
                MonsterActorGrade.Elite => _finishAttackEliteHealthRate,
                MonsterActorGrade.Boss  => _finishAttackBossHealthRate,
                _                       => _finishAttackNormalHealthRate,
            };

            float minThreshold = Mathf.Max(0f, _finishAttackDamageThreshold);
            float maxThreshold = Mathf.Max(minThreshold, _finishAttackMaxHealthThreshold);
            return Mathf.Clamp(maxHealth * Mathf.Clamp01(healthRate), minThreshold, maxThreshold);
        }

        private bool RequiresFinishAttackBreakState(MonsterActor monsterActor)
        {
            return monsterActor.Grade switch
            {
                MonsterActorGrade.Normal => _finishAttackRequireBreakForNormal,
                MonsterActorGrade.Elite => _finishAttackRequireBreakForElite,
                MonsterActorGrade.Boss  => _finishAttackRequireBreakForBoss,
                _                       => _finishAttackRequireBreakForNormal,
            };
        }

        private static bool IsInFinishAttackBreakState(MonsterActor monsterActor)
        {
            if (monsterActor.BreakGauge != null && monsterActor.BreakGauge.IsExposed)
                return true;

            PoiseStat poise = monsterActor.GetComponent<PoiseStat>();
            return poise != null && poise.IsPoiseBroken;
        }

        public Transform FindSpecialBreakAttackTarget()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            float searchRange = _specialBreakAttackData != null
                ? _specialBreakAttackData.searchRange
                : _specialBreakAttackSearchRange;
            float searchAngle = _specialBreakAttackData != null
                ? _specialBreakAttackData.searchAngle
                : _specialBreakAttackSearchAngle;
            Collider[] hits = Physics.OverlapSphere(origin, searchRange, _targetLayerMask);

            Transform bestTarget = null;
            float bestDistSq = float.MaxValue;
            Transform lockOnTarget = CameraManager.Instance.GetLockOnTarget();

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                MonsterActor monsterActor = hit.GetComponent<MonsterActor>()
                                            ?? hit.GetComponentInParent<MonsterActor>();
                if (monsterActor == null
                    || !monsterActor.CanTakeDamage()
                    || monsterActor.BreakGauge == null
                    || !monsterActor.BreakGauge.IsExposed)
                    continue;

                Vector3 dir = monsterActor.transform.position - origin;
                dir.y = 0f;
                if (dir.sqrMagnitude <= 0.001f) continue;
                if (Vector3.Angle(forward, dir) > searchAngle) continue;

                if (lockOnTarget != null &&
                    (monsterActor.transform == lockOnTarget || lockOnTarget.IsChildOf(monsterActor.transform)))
                    return monsterActor.transform;

                float distSq = dir.sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestTarget = monsterActor.transform;
                }
            }

            return bestTarget;
        }

        public List<IEnemyAIController> GetEnemyAIControllersInRadius(float radius)
        {
            var        result = new List<IEnemyAIController>();
            FillEnemyAIControllersInRadius(radius, result);
            return result;
        }

        public void FillEnemyAIControllersInRadius(float radius, List<IEnemyAIController> result)
        {
            _targetingController?.FillEnemyControllers(radius, _targetLayerMask, result);
        }

        #endregion

        #region Homing Target Search

        /// <summary>이동 가능 여부와 독립적으로 일반 공격의 회전 대상을 유지한다.</summary>
        public Transform FindNormalAttackFacingTarget()
            => FindAttackFacingTarget(_attackAssistSearchRange, _attackAssistSearchAngle);

        /// <summary>접근 공격의 입력 방향에 맞는 대상을 선택한다.</summary>
        public Transform FindDashAttackFacingTarget()
            => FindAttackFacingTarget(_dashAttackAssistSearchRange, _attackAssistSearchAngle);

        /// <summary>명시적 조준 대상도 해당 공격의 접근 반경 밖이면 이동 워프에서 제외한다.</summary>
        public Transform GetAttackWarpTarget(Transform target, float searchRange)
        {
            if (target == null) return null;
            return _targetingController != null && _targetingController.CanSelectTarget(target,
                searchRange, _attackAssistMaxHeightDifference, _attackAssistObstacleLayers) ? target : null;
        }

        private Transform FindAttackFacingTarget(float searchRange, float searchAngle)
        {
            Vector3 direction = _playerActor?.PlayerController != null
                ? _playerActor.PlayerController.MoveInputVector
                : Vector3.zero;
            // 가까운 적의 존재는 이동 생략 조건이지 회전 대상 취소 조건이 아니다.
            return _targetingController?.FindAttackTarget(searchRange,
                Mathf.Min(searchAngle, _freeAttackFacingSearchAngle), direction,
                _targetLayerMask, _attackAssistObstacleLayers, _attackAssistMaxHeightDifference);
        }
        #endregion
    }
}
