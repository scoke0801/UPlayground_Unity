using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>
    /// 충돌 반응 동안만 휴머노이드 척추·머리 본에 기울기와 비틀림을 겹쳐 적용한다.
    /// 반응이 없을 때는 비활성이라 LateUpdate 비용이 없다.
    /// </summary>
    public sealed class NpcBumpPose : MonoBehaviour
    {
        private const float HeadShare = 0.35f;
        private const float HeadLag = -0.5f;

        private Transform[] _spineBones;
        private Transform _head;
        private NpcBumpProfileSO _profile;
        private Vector3 _pushDirection;
        private float _leanDegrees;
        private float _twistDegrees;
        private float _startTime;

        /// <summary>휴머노이드 Animator에서 본을 찾는다. 휴머노이드가 아니면 false.</summary>
        public bool TryBind(Animator animator)
        {
            if (animator == null || !animator.isHuman) return false;
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            _spineBones = CollectSpineBones(animator);
            enabled = false;
            return _spineBones.Length > 0;
        }

        /// <summary>반응을 시작한다. 진행 중이면 새 값으로 덮어쓴다.</summary>
        public void Play(NpcBumpProfileSO profile, Vector3 pushDirection, float leanDegrees, float twistDegrees)
        {
            if (_spineBones == null || pushDirection.sqrMagnitude < 0.0001f) return;
            _profile = profile;
            _pushDirection = pushDirection.normalized;
            _leanDegrees = leanDegrees;
            _twistDegrees = twistDegrees;
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
        }

        private void ApplyToBone(Transform bone, Vector3 leanAxis, float weight)
        {
            Quaternion lean = Quaternion.AngleAxis(_leanDegrees * weight, leanAxis);
            Quaternion twist = Quaternion.AngleAxis(_twistDegrees * weight, Vector3.up);
            bone.rotation = twist * lean * bone.rotation;
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
