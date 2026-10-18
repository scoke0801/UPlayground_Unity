using System;
using System.Collections.Generic;
using KINEMATION.MotionWarping.Runtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Components
{
    /// <summary>모델별 무기 부착축과 상체 보정 한계. 지정된 무기에만 적용한다.</summary>
    [Serializable]
    public sealed class WeaponAimSettings
    {
        public bool enabled;
        public WeaponType weaponType;
        public ParentConstraint weaponConstraint;
        public Vector3 localAimOrigin;
        public Vector3 localAimAxis = Vector3.up;
        [Range(0f, 1f)] public float weight = 1f;
        [Range(0f, 45f)] public float chestAngle = 10f;
        [Range(0f, 60f)] public float armAngle = 30f;
        [Range(0f, 1f)] public float chestShare = 0.3f;
        [Tooltip("이 각도부터 보정을 줄여 휘두르기 궤적을 보존한다.")]
        [Range(0f, 180f)] public float fullAimAngle = 35f;
        [Range(0f, 180f)] public float cutoffAngle = 75f;
        [Min(0f)] public float releaseSpeed = 10f;
    }

    /// <summary>타격 타임라인에 맞춰 무기 축을 조준하고 양손의 애니메이션 관계를 보존한다.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WeaponAimController : MonoBehaviour
    {
        private PlayerActorAnimator _motion;
        private PlayerActor _owner;
        private PlayerEquipment _equipment;
        private WeaponAimSettings _settings;
        private Transform _chest, _mainArm, _mainHand, _offArm, _offForearm, _offHand;
        private Transform _source;
        private int _sourceIndex;
        private Transform _target;
        private Collider _targetCollider;
        private IDamageable _targetDamageable;
        private Vector3 _lastTargetPoint;
        private float _weight;
        private bool _hasTargetPoint;

        /// <summary>モデル初期化時にボーンと武器の手側Constraintを解決する。</summary>
        public void Initialize(PlayerActorAnimator motion, PlayerActor owner, WeaponAimSettings settings)
        {
            _motion = motion;
            _owner = owner;
            _equipment = owner.GetPlayerEquipment();
            _settings = settings;
            _source = null;
            ResetAim();
            Animator animator = motion.GetAnimator;
            if (!animator.isHuman || settings.weaponConstraint == null)
                return;

            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            ParentConstraint constraint = settings.weaponConstraint;
            for (int i = 0; i < constraint.sourceCount; i++)
            {
                Transform source = constraint.GetSource(i).sourceTransform;
                bool right = source != null && rightHand != null && source.IsChildOf(rightHand);
                bool left = source != null && leftHand != null && source.IsChildOf(leftHand);
                if (!right && !left) continue;
                _source = source;
                _sourceIndex = i;
                _mainHand = right ? rightHand : leftHand;
                _mainArm = animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
                _offArm = animator.GetBoneTransform(right ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
                _offForearm = animator.GetBoneTransform(right ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                _offHand = right ? leftHand : rightHand;
                break;
            }
            _chest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (_chest == null) _chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        }

        /// <summary>공격 시작과 콤보 연결 시 선택한 타겟을 사용한다. IK 평가 중 탐색하지 않는다.</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _targetCollider = target != null ? target.GetComponentInParent<CapsuleCollider>() : null;
            _targetDamageable = target != null ? target.GetComponentInParent<GameActor>() as IDamageable : null;
        }

        /// <summary>투사체와 자세 보정에서 같은 목표점을 조회한다.</summary>
        public bool TryGetTargetPoint(out Vector3 position, out Transform target)
        {
            position = default;
            target = null;
            if (!CanAim()) return false;
            if (_owner.Abilities != null
                && _owner.Abilities.TryGetPrimaryTargetReservation(out var reservation)
                && reservation.IsConfirmed && reservation.Mode != AbilityTargetingMode.None)
            {
                if (reservation.Target != null && (!reservation.Target.gameObject.activeInHierarchy
                    || (reservation.Target is IDamageable damageable && !damageable.IsAlive()))) return false;
                position = reservation.Position;
                target = reservation.Target != null ? reservation.Target.transform : null;
                return true;
            }
            if (_target == null || !_target.gameObject.activeInHierarchy
                || (_targetDamageable != null && !_targetDamageable.IsAlive())) return false;
            position = _targetCollider != null && _targetCollider.enabled
                ? _targetCollider.bounds.center : _target.position;
            target = _target;
            return true;
        }

        private bool CanAim()
        {
            if (_settings == null || !_settings.enabled || _source == null || _mainArm == null
                || _owner == null || _motion == null || !_motion.isActiveAndEnabled
                || _equipment == null || !_equipment.IsMainWeaponEquipped
                || _equipment.GetMainWeaponType() != _settings.weaponType)
                return false;
            ParentConstraint constraint = _settings.weaponConstraint;
            return constraint != null && constraint.isActiveAndEnabled && constraint.constraintActive
                && constraint.gameObject.activeInHierarchy && constraint.weight >= 0.99f
                && _sourceIndex < constraint.sourceCount
                && constraint.GetSource(_sourceIndex).sourceTransform == _source
                && constraint.GetSource(_sourceIndex).weight >= 0.99f;
        }

        private void OnDisable() => ResetAim();

        private void ResetAim()
        {
            _weight = 0f;
            _target = null;
            _targetCollider = null;
            _targetDamageable = null;
            _hasTargetPoint = false;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!CanAim())
            {
                _weight = 0f;
                _hasTargetPoint = false;
                return;
            }
            MotionEventBase aimEvent = null;
            float window = _motion.IsPlayingMotionSet
                ? WeaponAimMath.EvaluateMotionWeight(_motion.CurrentMotionSet, _motion.CurrentEvaluatedMotionSetTime, out aimEvent)
                : 0f;
            bool hasTarget = TryGetTargetPoint(out Vector3 point, out _);
            if (hasTarget)
            {
                if (aimEvent is SpawnProjectileEvent projectile) point += projectile.targetOffset;
                _lastTargetPoint = point;
                _hasTargetPoint = true;
            }
            float desired = hasTarget ? window * _settings.weight : 0f;
            // 타격 전 페이드는 타임라인이 소유한다. 타겟 소실·중단 시에만 별도 감쇠한다.
            _weight = desired > 0f ? desired : Mathf.MoveTowards(_weight, 0f,
                _settings.releaseSpeed * Time.deltaTime * Mathf.Max(0f, _owner.LocalTimeScale));
            if (_weight <= 0f || !_hasTargetPoint) return;

            GetAimRay(out Vector3 origin, out Vector3 axis);
            float angleWeight = WeaponAimMath.EvaluateAngleWeight(axis, _lastTargetPoint - origin,
                _settings.fullAimAngle, _settings.cutoffAngle);
            float weight = _weight * angleWeight;
            if (weight <= 0f) return;

            RotateBone(_chest, _settings.chestAngle, weight * _settings.chestShare);
            // 원본 포즈의 양손 간격을 보존하므로 창의 미끄러지는 그립과 발사 후 손떼기도 유지한다.
            Vector3 offPosition = _offHand != null ? _mainHand.InverseTransformPoint(_offHand.position) : default;
            Quaternion offRotation = _offHand != null ? Quaternion.Inverse(_mainHand.rotation) * _offHand.rotation : Quaternion.identity;
            RotateBone(_mainArm, _settings.armAngle, weight);
            if (_offArm != null && _offForearm != null && _offHand != null)
                TwoBoneIk.SolveTwoBoneIK(_offArm, _offForearm, _offHand,
                    (_mainHand.TransformPoint(offPosition), _mainHand.rotation * offRotation),
                    hint: null, targetWeight: 1f, hintWeight: 0f);
        }

        private void GetAimRay(out Vector3 origin, out Vector3 axis)
        {
            ParentConstraint constraint = _settings.weaponConstraint;
            Quaternion rotation = _source.rotation * Quaternion.Euler(constraint.GetRotationOffset(_sourceIndex));
            // Constraint 결과 Transform은 IK 시점에 이전 프레임일 수 있어 손측 source에서 재구성한다.
            origin = _source.TransformPoint(constraint.GetTranslationOffset(_sourceIndex))
                + rotation * Vector3.Scale(_settings.localAimOrigin, constraint.transform.lossyScale);
            axis = rotation * _settings.localAimAxis.normalized;
        }

        private void RotateBone(Transform bone, float limit, float weight)
        {
            if (bone == null) return;
            GetAimRay(out Vector3 origin, out Vector3 axis);
            Quaternion correction = WeaponAimMath.GetPivotCorrection(bone.position, origin, axis, _lastTargetPoint, limit, weight);
            bone.rotation = correction * bone.rotation;
        }
    }

    /// <summary>무기 조준의 각도 제한과 MotionSet 타격 구간을 계산한다.</summary>
    public static class WeaponAimMath
    {
        /// <summary>팔 회전으로 무기 원점도 이동하는 것을 포함해 조준선의 교점을 맞춘다.</summary>
        public static Quaternion GetPivotCorrection(Vector3 pivot, Vector3 origin, Vector3 axis,
            Vector3 target, float limit, float weight)
        {
            if (axis.sqrMagnitude < 0.000001f) return Quaternion.identity;
            Vector3 offset = origin - pivot;
            Vector3 destination = target - pivot;
            axis.Normalize();
            float projection = Vector3.Dot(offset, axis);
            // 회전 전 조준선과 피벗 중심 구의 교점을 구하면, 회전 후 원점 이동 오차가 남지 않는다.
            float discriminant = projection * projection + destination.sqrMagnitude - offset.sqrMagnitude;
            if (discriminant < 0f) return Quaternion.identity;
            float distance = -projection + Mathf.Sqrt(discriminant);
            if (distance < 0f) return Quaternion.identity;
            return GetCorrection(offset + axis * distance, destination, limit, weight);
        }

        /// <summary>큰 휘두르기와 반대 방향 공격을 강제로 조준 자세로 바꾸지 않는다.</summary>
        public static float EvaluateAngleWeight(Vector3 axis, Vector3 direction, float fullAngle, float cutoffAngle)
        {
            if (axis.sqrMagnitude < 0.000001f || direction.sqrMagnitude < 0.000001f) return 0f;
            float angle = Vector3.Angle(axis, direction);
            if (angle >= cutoffAngle) return 0f;
            if (angle <= fullAngle) return 1f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fullAngle, cutoffAngle, angle));
        }

        /// <summary>무기 축의 오차만 제한된 각도로 보정하며 축 주위의 원본 회전은 유지한다.</summary>
        public static Quaternion GetCorrection(Vector3 axis, Vector3 direction, float limit, float weight)
        {
            if (axis.sqrMagnitude < 0.000001f || direction.sqrMagnitude < 0.000001f) return Quaternion.identity;
            Quaternion delta = Quaternion.FromToRotation(axis, direction);
            return Quaternion.Slerp(Quaternion.identity,
                Quaternion.RotateTowards(Quaternion.identity, delta, Mathf.Max(0f, limit)), Mathf.Clamp01(weight));
        }

        /// <summary>실제 타격·발사 이벤트에 연결된 블렌드 구간만 평가한다.</summary>
        public static float EvaluateMotionWeight(MotionSet set, float time)
            => EvaluateMotionWeight(set, time, out _);

        /// <summary>겹친 구간 중 가장 강한 보정 이벤트와 강도를 반환한다.</summary>
        public static float EvaluateMotionWeight(MotionSet set, float time, out MotionEventBase selected)
        {
            selected = null;
            if (set == null) return 0f;
            float weight = 0f;
            EvaluateEvents(set, set.globalEvents, time, ref weight, ref selected);
            if (set.motions != null)
                for (int i = 0; i < set.motions.Count; i++)
                    EvaluateEvents(set, set.motions[i]?.events, time, ref weight, ref selected);
            // 별도 레이어는 다른 타겟/상체 마스크를 가질 수 있어 기본 공격 트랙만 처리한다.
            return weight;
        }

        private static void EvaluateEvents(MotionSet set, List<MotionEventBase> events, float time,
            ref float weight, ref MotionEventBase selected)
        {
            if (events == null) return;
            for (int i = 0; i < events.Count; i++)
            {
                MotionEventBase evt = events[i];
                float lead, release;
                if (evt is BeginCollisionEvent collision && !collision.suppressWeaponAim
                    && collision.collisionSource == CollisionSourceType.AttachedHitboxGroup)
                {
                    lead = collision.weaponAimBlendIn;
                    release = collision.weaponAimBlendOut;
                }
                else if (evt is SpawnProjectileEvent projectile && !projectile.suppressWeaponAim
                    && !projectile.projectTargetToGround
                    && (projectile.targetMode == ProjectileTargetMode.Forward || projectile.targetMode == ProjectileTargetMode.LockOnTarget))
                {
                    lead = projectile.weaponAimBlendIn;
                    release = projectile.weaponAimBlendOut;
                }
                else continue;
                if (!MotionTimelineResolver.TryGetEventGlobalRange(set, evt, out float start, out float end)) continue;
                if (evt is SpawnProjectileEvent) end = start;
                float candidate = EvaluateWindow(time, start, end, lead, release);
                if (candidate <= weight) continue;
                weight = candidate;
                selected = evt;
            }
        }

        /// <summary>타격 직전에 강도를 올리고 판정 종료 후 원본 자세로 복귀한다.</summary>
        public static float EvaluateWindow(float time, float start, float end, float lead, float release)
        {
            lead = Mathf.Max(0f, lead);
            release = Mathf.Max(0f, release);
            if (time < start - lead || time > end + release) return 0f;
            if (time < start) return Mathf.SmoothStep(0f, 1f, (time - start + lead) / lead);
            if (time > end) return 1f - Mathf.SmoothStep(0f, 1f, (time - end) / release);
            return 1f;
        }
    }
}
