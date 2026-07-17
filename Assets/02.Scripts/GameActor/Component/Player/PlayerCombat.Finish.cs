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

        public Transform FindAttackSnapTarget(bool isLockedOn)
        {
            return FindAttackSnapTargetInternal(
                _homingReachRange,
                _homingReachAngle,
                GetSnapSearchRange(isLockedOn),
                GetSnapSearchAngle(isLockedOn),
                skipIfAlreadyCovered: true);
        }

        public Transform FindMotionWarpTarget(bool isLockedOn, float warpMaxDistance)
        {
            float searchRange = Mathf.Max(GetSnapSearchRange(isLockedOn), warpMaxDistance);
            float searchAngle = GetSnapSearchAngle(isLockedOn);
            if (!isLockedOn)
                searchAngle = Mathf.Max(searchAngle, _freeAttackFacingSearchAngle);

            return FindAttackSnapTargetInternal(
                _homingReachRange,
                _homingReachAngle,
                searchRange,
                searchAngle,
                skipIfAlreadyCovered: true);
        }

        public Transform FindFreeAttackFacingTarget()
        {
            float searchAngle = Mathf.Max(_freeSnapSearchAngle, _freeAttackFacingSearchAngle);
            return FindAttackSnapTargetInternal(
                _homingReachRange,
                _homingReachAngle,
                _freeSnapSearchRange,
                searchAngle,
                skipIfAlreadyCovered: false);
        }

        private Transform FindAttackSnapTargetInternal(
            float targetingRange,
            float targetingAngle,
            float searchRange,
            float searchAngle,
            bool skipIfAlreadyCovered)
            => _targetingController?.FindAttackTarget(
                targetingRange,
                targetingAngle,
                searchRange,
                searchAngle,
                _targetLayerMask,
                skipIfAlreadyCovered);

        #endregion
    }
}
