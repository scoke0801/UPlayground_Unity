using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>
    /// 충돌 반응 동안만 휴머노이드 척추·머리·골반·팔 본에 기울기, 비틀림, 튕김을 겹쳐 적용한다.
    /// 전용 모션이 없어도 몸 전체가 밀리는 느낌을 내기 위한 절차적 연출이며, 반응이 없을 때는 비활성이라 비용이 없다.
    /// </summary>
    public sealed class NpcBumpPose : MonoBehaviour
    {
        private const float HeadShare = 0.35f;
        private const float HeadLag = -0.5f;
        private const float LeanTierHipScale = 0.5f;

        private ParentConstraint[] _followConstraints;
        private Transform[] _spineBones;
        private Transform _head;
        private Transform _hips;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;
        private Transform _leftLowerArm;
        private Transform _rightLowerArm;
        private NpcBumpProfileSO _profile;
        private Vector3 _pushDirection;
        private float _leanDegrees;
        private float _twistDegrees;
        private float _startTime;
        private float _hipShift;
        private float _armSpread;
        private float _leftArmSign;
        private float _rightArmSign;

        /// <summary>휴머노이드 Animator에서 본을 찾는다. 휴머노이드가 아니면 false.</summary>
        public bool TryBind(Animator animator)
        {
            if (animator == null || !animator.isHuman) return false;
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _spineBones = CollectSpineBones(animator);
            _followConstraints = animator.GetComponentsInChildren<ParentConstraint>(true);
            enabled = false;
            return _spineBones.Length > 0;
        }

        /// <summary>반응을 시작한다. 진행 중이면 새 값으로 덮어쓴다.</summary>
        public void Play(NpcBumpProfileSO profile, NpcBumpTier tier, Vector3 pushDirection, float leanDegrees, float twistDegrees, float intensityScale = 1f)
        {
            if (_spineBones == null || pushDirection.sqrMagnitude < 0.0001f) return;
            _profile = profile;
            _pushDirection = pushDirection.normalized;
            _leanDegrees = leanDegrees;
            _twistDegrees = twistDegrees;
            _hipShift = profile.hipShiftMeters * (tier >= NpcBumpTier.Stagger ? 1f : LeanTierHipScale) * intensityScale;
            _armSpread = tier >= NpcBumpTier.Stagger ? profile.armSpreadDegrees * intensityScale : 0f;
            _leftArmSign = ResolveOutwardSign(_leftUpperArm, _leftLowerArm);
            _rightArmSign = ResolveOutwardSign(_rightUpperArm, _rightLowerArm);
            _startTime = Time.time;
            enabled = true;
        }

        private void LateUpdate()
        {
            float weight = NpcBumpResolver.EvaluateEnvelope(_profile, Time.time - _startTime);
            if (weight <= 0f)
            {
                enabled = false;
                return;
            }

            Vector3 leanAxis = Vector3.Cross(Vector3.up, _pushDirection);
            float share = 1f / _spineBones.Length;
            for (int i = 0; i < _spineBones.Length; i++)
                ApplyToBone(_spineBones[i], leanAxis, weight * share);
            if (_head != null)
                ApplyToBone(_head, leanAxis, weight * HeadShare * HeadLag);
            if (_hips != null)
                _hips.position += _pushDirection * (_hipShift * weight);
            SpreadArm(_leftUpperArm, _leftArmSign, weight);
            SpreadArm(_rightUpperArm, _rightArmSign, weight);
            SyncFollowConstraints();
        }

        /// <summary>
        /// 머리카락 등 ParentConstraint로 본에 붙은 오브젝트를 겹쳐 쓴 포즈에 맞춰 다시 붙인다.
        /// 제약은 LateUpdate보다 먼저 계산되므로 여기서 본을 바꾸면 머리카락만 원래 자세에 남는다.
        /// </summary>
        private void SyncFollowConstraints()
        {
            for (int i = 0; i < _followConstraints.Length; i++)
            {
                ParentConstraint constraint = _followConstraints[i];
                if (!CanSync(constraint)) continue;

                Transform source = constraint.GetSource(0).sourceTransform;
                constraint.transform.SetPositionAndRotation(
                    source.position + source.rotation * constraint.GetTranslationOffset(0),
                    source.rotation * Quaternion.Euler(constraint.GetRotationOffset(0)));
            }
        }

        /// <summary>단일 소스에 완전히 붙는 제약만 직접 계산한다. 축 고정이나 블렌딩이 있으면 식이 달라서 건드리지 않는다.</summary>
        private static bool CanSync(ParentConstraint constraint)
            => constraint != null
               && constraint.constraintActive
               && constraint.sourceCount == 1
               && constraint.GetSource(0).sourceTransform != null
               && Mathf.Approximately(constraint.weight, 1f)
               && Mathf.Approximately(constraint.GetSource(0).weight, 1f)
               && constraint.translationAxis == (Axis.X | Axis.Y | Axis.Z)
               && constraint.rotationAxis == (Axis.X | Axis.Y | Axis.Z);

        private void ApplyToBone(Transform bone, Vector3 leanAxis, float weight)
        {
            Quaternion lean = Quaternion.AngleAxis(_leanDegrees * weight, leanAxis);
            Quaternion twist = Quaternion.AngleAxis(_twistDegrees * weight, Vector3.up);
            bone.rotation = twist * lean * bone.rotation;
        }

        private void SpreadArm(Transform upperArm, float sign, float weight)
        {
            if (upperArm == null || _armSpread <= 0f) return;
            upperArm.rotation = Quaternion.AngleAxis(_armSpread * weight * sign, transform.forward) * upperArm.rotation;
        }

        /// <summary>팔을 몸 앞쪽 축으로 +방향 회전시켰을 때 팔꿈치가 몸에서 멀어지면 +1, 가까워지면 -1. 리그마다 좌우 축 부호가 달라서 현재 자세로 판정한다.</summary>
        private float ResolveOutwardSign(Transform upperArm, Transform lowerArm)
        {
            if (upperArm == null || lowerArm == null) return 0f;
            Vector3 arm = lowerArm.position - upperArm.position;
            Vector3 outward = Vector3.ProjectOnPlane(upperArm.position - transform.position, Vector3.up);
            Vector3 rotated = Quaternion.AngleAxis(10f, transform.forward) * arm;
            return Vector3.Dot(rotated - arm, outward) >= 0f ? 1f : -1f;
        }

        private static Transform[] CollectSpineBones(Animator animator)
        {
            var bones = new System.Collections.Generic.List<Transform>(3);
            AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Spine));
            AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.Chest));
            AddIfPresent(bones, animator.GetBoneTransform(HumanBodyBones.UpperChest));
            return bones.ToArray();
        }

        private static void AddIfPresent(System.Collections.Generic.List<Transform> bones, Transform bone)
        {
            if (bone != null) bones.Add(bone);
        }
    }
}
