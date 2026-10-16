using System;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Components
{
    /// <summary>모델별 얼굴 본·눈꺼풀 연결. 관리 틱에서 애니메이션 평가 전 복원하고 평가 후 적용한다.</summary>
    [Serializable]
    public sealed class DialogueFaceBinding
    {
        public DialogueFaceProfileSO profile;
        public Transform head;
        public Transform leftEye;
        public Transform rightEye;
        public Transform eyeCenter;
        public Vector3 headTargetOffset;
        public SkinnedMeshRenderer eyelids;
        public string blinkShape = "Eye_Blink";
        public string[] expressionShapes = Array.Empty<string>();
        private Transform _root;
        private Transform[] _bones;
        private Quaternion[] _before;
        private Quaternion[] _written;
        private Vector3[] _forward;
        private int[] _expressionIndices;
        private int _blinkIndex = -1;
        private bool _hasPose;
        private bool _hasBlink;
        private float _beforeBlink;
        private float _writtenBlink;
        private float _blinkTime;
        private float _nextBlink;
        private float _lookWeight;
        private Vector2 _headAngles;
        private Vector2 _eyeAngles;

        /// <summary>모델 교체 시 본과 눈꺼풀 채널을 캐시한다.</summary>
        public void Initialize(Transform root)
        {
            Clear();
            _root = root;
            if (profile == null) return;
            _bones = new[] { head, leftEye, rightEye };
            _before = new Quaternion[3];
            _written = new Quaternion[3];
            _forward = new Vector3[3];
            CacheForwardAxes(root);
            _blinkIndex = eyelids != null && eyelids.sharedMesh != null
                ? eyelids.sharedMesh.GetBlendShapeIndex(blinkShape) : -1;
            _expressionIndices = new int[expressionShapes.Length];
            for (int i = 0; i < expressionShapes.Length; i++)
                _expressionIndices[i] = eyelids != null && eyelids.sharedMesh != null
                    ? eyelids.sharedMesh.GetBlendShapeIndex(expressionShapes[i]) : -1;
            ScheduleBlink();
        }

        private void CacheForwardAxes(Transform root)
        {
            Transform[] meshBones = eyelids != null ? eyelids.bones : Array.Empty<Transform>();
            Matrix4x4[] bindPoses = eyelids != null && eyelids.sharedMesh != null
                ? eyelids.sharedMesh.bindposes : Array.Empty<Matrix4x4>();
            // 머리는 기존 모델 정면을 유지한다. 메시 좌표계는 임포트 축과 스케일을 포함한다.
            _forward[0] = head != null ? head.InverseTransformDirection(root.forward) : Vector3.forward;
            int headIndex = Array.IndexOf(meshBones, head);
            for (int i = 1; i < _bones.Length; i++)
            {
                if (_bones[i] == null) continue;
                int eyeIndex = Array.IndexOf(meshBones, _bones[i]);
                // 공통 메시 좌표계를 상쇄하여 머리 기준 정면을 눈의 바인드 공간으로 옮긴다.
                _forward[i] = headIndex >= 0 && headIndex < bindPoses.Length
                    && eyeIndex >= 0 && eyeIndex < bindPoses.Length
                    ? (bindPoses[eyeIndex] * bindPoses[headIndex].inverse).MultiplyVector(_forward[0]).normalized
                    : _bones[i].InverseTransformDirection(root.forward);
            }
        }

        /// <summary>다른 표정이 바꾼 값은 보존하고 자신이 쓴 보정만 복원한다.</summary>
        public void Restore()
        {
            if (_hasPose)
                for (int i = 2; i >= 0; i--)
                    if (_bones[i] != null && Quaternion.Angle(_bones[i].localRotation, _written[i]) < 0.001f)
                        _bones[i].localRotation = _before[i];
            if (_hasBlink && eyelids != null && Mathf.Abs(eyelids.GetBlendShapeWeight(_blinkIndex) - _writtenBlink) < 0.001f)
                eyelids.SetBlendShapeWeight(_blinkIndex, _beforeBlink);
            _hasPose = false;
            _hasBlink = false;
        }

        /// <summary>중단·종료·모델 교체 시 얼굴 보정을 해제한다.</summary>
        public void Clear()
        {
            Restore();
            _lookWeight = 0;
            _headAngles = Vector2.zero;
            _eyeAngles = Vector2.zero;
            _blinkTime = 0;
        }

        /// <summary>애니메이션 평가 후 양 눈에 같은 방향을 적용해 근거리 사시를 방지한다.</summary>
        public void Apply(Transform target, bool paused, bool suppressLook, bool suppressBlink, float deltaTime)
        {
            if (profile == null || _root == null || head == null || _bones == null) return;
            float elapsed = paused ? 0 : deltaTime;
            if (!suppressLook && target != null && !target.IsChildOf(_root)) ApplyLook(target.position, elapsed);
            if (!suppressBlink) ApplyBlink(elapsed);
        }

        private void ApplyLook(Vector3 target, float elapsed)
        {
            Vector3 origin = eyeCenter != null ? eyeCenter.position : head.TransformPoint(headTargetOffset);
            Vector3 direction = _root.InverseTransformDirection(target - origin);
            float distance = direction.magnitude;
            if (distance < profile.minimumDistance) return;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            float fade = 1 - Mathf.Clamp01((Mathf.Abs(yaw) - profile.headYaw) / Mathf.Max(0.01f, profile.outsideFadeDegrees));
            _lookWeight = Mathf.Lerp(_lookWeight, fade, 1 - Mathf.Exp(-profile.headSpeed * elapsed));
            Vector2 headGoal = new(Mathf.Clamp(pitch, -profile.headUp, profile.headDown), Mathf.Clamp(yaw, -profile.headYaw, profile.headYaw));
            _headAngles = Vector2.Lerp(_headAngles, headGoal * fade, 1 - Mathf.Exp(-profile.headSpeed * elapsed));
            Vector2 eyesGoal = new(Mathf.Clamp(pitch - _headAngles.x, -profile.eyePitch, profile.eyePitch), Mathf.Clamp(yaw - _headAngles.y, -profile.eyeYaw, profile.eyeYaw));
            _eyeAngles = Vector2.Lerp(_eyeAngles, eyesGoal * fade, 1 - Mathf.Exp(-profile.eyeSpeed * elapsed));
            ApplyBone(0, _headAngles);
            ApplyBone(1, _headAngles + _eyeAngles);
            ApplyBone(2, _headAngles + _eyeAngles);
            _hasPose = true;
        }

        private void ApplyBone(int index, Vector2 angles)
        {
            Transform bone = _bones[index];
            if (bone == null) return;
            _before[index] = bone.localRotation;
            Vector3 forward = bone.TransformDirection(_forward[index]);
            Vector3 goal = _root.TransformDirection(Quaternion.Euler(angles.x, angles.y, 0) * Vector3.forward);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(forward, goal), _lookWeight) * bone.rotation;
            _written[index] = bone.localRotation;
        }

        private void ApplyBlink(float elapsed)
        {
            if (_blinkIndex < 0 || eyelids == null) return;
            _beforeBlink = eyelids.GetBlendShapeWeight(_blinkIndex);
            bool ownedByExpression = _beforeBlink > 0.001f;
            for (int i = 0; i < _expressionIndices.Length; i++)
                if (_expressionIndices[i] >= 0 && eyelids.GetBlendShapeWeight(_expressionIndices[i]) > 0.001f)
                    ownedByExpression = true;
            if (ownedByExpression) { _blinkTime = 0; ScheduleBlink(); return; }
            if (_nextBlink > 0) { _nextBlink -= elapsed; return; }
            _blinkTime += elapsed;
            float progress = _blinkTime / Mathf.Max(0.01f, profile.blinkDuration);
            if (progress >= 1) { _blinkTime = 0; ScheduleBlink(); return; }
            _writtenBlink = Mathf.Sin(progress * Mathf.PI) * profile.blinkWeight;
            eyelids.SetBlendShapeWeight(_blinkIndex, _writtenBlink);
            _hasBlink = true;
        }

        private void ScheduleBlink()
        {
            if (profile != null) _nextBlink = UnityEngine.Random.Range(Mathf.Max(0.01f, profile.blinkInterval.x), Mathf.Max(profile.blinkInterval.x, profile.blinkInterval.y));
        }
    }
}
