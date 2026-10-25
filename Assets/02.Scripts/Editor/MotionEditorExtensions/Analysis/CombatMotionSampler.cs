using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Combat;

namespace UPlayGround.Animation.Editor
{
    /// <summary>기존 재생 커널을 격리된 프리팹 사본에서 평가한다. MotionEventExecutor는 연결하지 않는다.</summary>
    public sealed class CombatMotionSampler : IDisposable
    {
        private readonly CombatMotionCatalog _catalog;
        private readonly AnimancerComponent _animancer;
        private readonly MotionSetPlaybackController _playback;
        private readonly Transform _weapon, _base, _tip, _mount;
        private readonly Vector3 _mountOffset;
        private readonly Quaternion _mountRotation;
        private readonly CombatHitbox[] _hitboxes;
        private readonly AvatarMask _upperMask;
        private readonly Transform[] _transforms;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private MotionSet _currentSet;
        private static readonly HumanBodyBones[] Bones = {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot };

        public GameObject Root { get; private set; }
        public Animator Animator { get; }

        /// <summary>측정 지점 누락은 대체 지점을 추측하지 않고 실패시킨다.</summary>
        public CombatMotionSampler(CombatMotionCatalog catalog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode 전용 측정입니다.");
            _catalog = catalog;
            try
            {
                Root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(catalog.modelPrefab));
                Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Animator = Root.GetComponent<Animator>();
                if (Animator == null || Animator.avatar == null || !Animator.avatar.isValid || !Animator.avatar.isHuman)
                    throw new InvalidOperationException("유효한 Humanoid Avatar가 있는 모델 루트가 필요합니다.");
                _upperMask = Root.GetComponent<PlayerActorAnimator>()?.UpperBodyMask;
                _weapon = RequireTransform(catalog.weaponPath);
                _base = RequireTransform(catalog.bladeBasePath);
                _tip = RequireTransform(catalog.bladeTipPath);
                var constraint = _weapon.GetComponent<ParentConstraint>();
                if (constraint == null || catalog.drawnConstraintSource < 0 || catalog.drawnConstraintSource >= constraint.sourceCount)
                    throw new InvalidOperationException("발도 상태의 ParentConstraint source가 없습니다.");
                _mount = constraint.GetSource(catalog.drawnConstraintSource).sourceTransform;
                if (_mount == null) throw new InvalidOperationException("무기 장착 본이 없습니다.");
                _mountOffset = constraint.GetTranslationOffset(catalog.drawnConstraintSource);
                _mountRotation = Quaternion.Euler(constraint.GetRotationOffset(catalog.drawnConstraintSource));
                _hitboxes = Root.GetComponentsInChildren<CombatHitbox>(true);
                // 일반 컴포넌트는 비활성화해 RequireComponent 관계를 보존한다.
                // Animator 콜백을 받는 ActorAnimator만 제거하고 Clip 이벤트도 아래에서 차단한다.
                foreach (var behaviour in Root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    behaviour.enabled = false;
                    if (behaviour is ActorAnimator) UnityEngine.Object.DestroyImmediate(behaviour);
                }
                foreach (var behaviour in Root.GetComponentsInChildren<Behaviour>(true))
                    if (behaviour is not UnityEngine.Animator) behaviour.enabled = false;
                foreach (var particle in Root.GetComponentsInChildren<ParticleSystem>(true))
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Root.SetActive(true);
                for (var node = _weapon; node != null; node = node.parent) node.gameObject.SetActive(true);
                Animator.enabled = true;
                Animator.runtimeAnimatorController = null;
                Animator.fireEvents = false;
                Animator.applyRootMotion = true;
                Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animancer = Root.GetComponent<AnimancerComponent>() ?? Root.AddComponent<AnimancerComponent>();
                _animancer.enabled = true;
                _animancer.Animator = Animator;
                _playback = new MotionSetPlaybackController(_animancer);
                _transforms = Root.GetComponentsInChildren<Transform>(true);
                _restPositions = _transforms.Select(t => t.localPosition).ToArray();
                _restRotations = _transforms.Select(t => t.localRotation).ToArray();
            }
            catch { Dispose(); throw; }
        }

        private Transform RequireTransform(string path)
        {
            var result = string.IsNullOrEmpty(path) ? null : Root.transform.Find(path);
            if (result == null) throw new InvalidOperationException($"무기 측정 지점 누락: {path}");
            return result;
        }

        private void Begin(MotionSet set)
        {
            _playback.Stop();
            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i].SetLocalPositionAndRotation(_restPositions[i], _restRotations[i]);
            Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Animator.Rebind();
            if (set.baseLayerIndex > 0) _animancer.Layers[set.baseLayerIndex].Mask = _upperMask;
            if (!_playback.Play(set)) throw new InvalidOperationException("기존 모션 재생 커널이 타임라인을 거부했습니다.");
            _currentSet = set;
            _animancer.Graph.PauseGraph();
            _playback.SeekTime(0);
            _animancer.Evaluate(0);
            Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ApplyWeaponMount();
        }

        /// <summary>고정 간격과 모든 타격/캔슬 경계를 포함한다. 델타는 원본 importer의 루트 추출 결과다.</summary>
        public void Sample(MotionSet set, List<MotionPoseSample> samples, List<MotionBoundaryPose> poses, List<MotionTimingWindow> windows)
        {
            Begin(set);
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hitbox in _hitboxes.Where(h => h.transform.IsChildOf(_weapon))) groups.Add(hitbox.GroupId);
            foreach (var window in windows ?? new List<MotionTimingWindow>())
                foreach (string group in window.groups) groups.Add(group);
            var times = new SortedSet<float> { 0, set.TotalDuration };
            for (int i = 1; i < Mathf.CeilToInt(set.TotalDuration * _catalog.sampleRate); i++) times.Add(i / (float)_catalog.sampleRate);
            float offset = 0;
            foreach (var motion in set.motions) { offset += motion?.Duration ?? 0; times.Add(offset); }
            foreach (var window in windows ?? new List<MotionTimingWindow>())
            {
                times.Add(Mathf.Clamp(window.start, 0, set.TotalDuration));
                times.Add(Mathf.Clamp(window.end, 0, set.TotalDuration));
            }
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            float previousTime = 0, yaw = 0;
            List<MotionJointPose> previousJoints = null;
            foreach (float time in times)
            {
                float delta = time - previousTime;
                if (delta > 0.000001f)
                {
                    // Seek는 제어 이벤트·시간 보정을 진행하지 않는다. 경계 블렌드는 명시적으로 제외한다.
                    float seek = previousTime;
                    if (set.GetMotionAtTime(seek + 0.000001f, out int next, out _) &&
                        set.GetMotionAtTime(seek, out int prior, out _) && next != prior) seek += 0.000001f;
                    _playback.SeekTime(seek);
                    _animancer.Evaluate(0);
                    Root.transform.SetPositionAndRotation(position, rotation);
                    _animancer.Evaluate(Mathf.Max(0, time - seek));
                    position += Animator.deltaPosition;
                    Quaternion deltaRotation = Animator.deltaRotation;
                    yaw += Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y);
                    rotation = (rotation * deltaRotation).normalized;
                }
                Root.transform.SetPositionAndRotation(position, rotation);
                ApplyWeaponMount();
                var sample = CaptureSample(set, time, yaw, groups);
                if (samples.Count > 0 && delta > 0.000001f)
                {
                    var previous = samples[samples.Count - 1];
                    sample.baseVelocity = (sample.bladeBase - previous.bladeBase) / delta;
                    sample.middleVelocity = (sample.bladeMiddle - previous.bladeMiddle) / delta;
                    sample.tipVelocity = (sample.bladeTip - previous.bladeTip) / delta;
                    sample.tipSpeed = sample.tipVelocity.magnitude;
                }
                samples.Add(sample);
                var joints = CaptureJoints(previousJoints, delta);
                if (poses != null && IsBoundary(time, set.TotalDuration, windows, out string label))
                    poses.Add(new MotionBoundaryPose { time = time, label = label, joints = joints });
                previousJoints = joints;
                previousTime = time;
            }
        }

        private void ApplyWeaponMount()
        {
            _weapon.SetPositionAndRotation(_mount.position + _mount.rotation * _mountOffset, _mount.rotation * _mountRotation);
        }

        private MotionPoseSample CaptureSample(MotionSet set, float time, float yaw, HashSet<string> groups)
        {
            set.GetMotionAtTime(time, out int index, out float local);
            var motion = index >= 0 ? set.motions[index] : null;
            var sample = new MotionPoseSample { time = time, motionIndex = index,
                clipTime = motion == null ? -1 : motion.ClipStartTime + local * (motion.playbackSpeed > 0 ? motion.playbackSpeed : 1),
                rootPosition = Root.transform.position, rootRotation = Root.transform.rotation, rootYaw = yaw,
                bladeBase = _base.position, bladeMiddle = (_base.position + _tip.position) * 0.5f, bladeTip = _tip.position };
            foreach (var hitbox in _hitboxes)
            {
                if (!groups.Contains(hitbox.GroupId)) continue;
                if (!hitbox.TryGetWorldShape(out var shape)) continue;
                sample.hitboxes.Add(new MotionHitboxSample { path = AnimationUtility.CalculateTransformPath(hitbox.transform, Root.transform),
                    group = hitbox.GroupId, shape = shape.Type.ToString(), center = shape.Center, rotation = shape.Rotation,
                    halfExtents = shape.HalfExtents, point0 = shape.Point0, point1 = shape.Point1, radius = shape.Radius });
            }
            return sample;
        }

        private List<MotionJointPose> CaptureJoints(List<MotionJointPose> previous, float delta)
        {
            var result = new List<MotionJointPose>();
            foreach (var bone in Bones)
            {
                var transform = Animator.GetBoneTransform(bone);
                if (transform == null) continue;
                var pose = new MotionJointPose { bone = bone.ToString(), position = Root.transform.InverseTransformPoint(transform.position),
                    rotation = Quaternion.Inverse(Root.transform.rotation) * transform.rotation };
                var old = previous?.FirstOrDefault(p => p.bone == pose.bone);
                if (old != null && delta > 0.000001f) pose.velocity = (pose.position - old.position) / delta;
                result.Add(pose);
            }
            return result;
        }

        private static bool IsBoundary(float time, float duration, List<MotionTimingWindow> windows, out string label)
        {
            label = time == 0 ? "Start" : time == duration ? "End" : "";
            foreach (var window in windows ?? new List<MotionTimingWindow>())
            {
                if (window.kind != "Hit" && window.kind != "Cancel" && window.kind != "AuthoredCancel" && window.kind != "Combo") continue;
                if (Mathf.Abs(time - window.start) < 0.000001f) label += $" {window.kind}:{window.label}:Start";
                if (Mathf.Abs(time - window.end) < 0.000001f) label += $" {window.kind}:{window.label}:End";
            }
            return label.Length > 0;
        }

        /// <summary>저장된 표본 시각의 포즈를 같은 커널로 재현한다.</summary>
        public void ShowPose(MotionSet set, MotionPoseSample sample)
        {
            if (_currentSet != set) Begin(set);
            Root.transform.SetPositionAndRotation(sample.rootPosition, sample.rootRotation);
            _playback.SeekTime(sample.time);
            _animancer.Evaluate(0);
            Root.transform.SetPositionAndRotation(sample.rootPosition, sample.rootRotation);
            ApplyWeaponMount();
        }

        /// <summary>현재 프리뷰 포즈와 저장된 칼끝 좌표의 오차를 비교한다.</summary>
        public float GetTipError(MotionPoseSample sample) => Vector3.Distance(_tip.position, sample.bladeTip);

        /// <summary>표본 형상을 감싸는 시작 좌표계 AABB를 계산한다.</summary>
        public static Bounds GetBounds(MotionHitboxSample shape)
        {
            if (shape.shape != CombatHitboxShapeType.Box.ToString())
            {
                var bounds = new Bounds(shape.point0, Vector3.one * shape.radius * 2);
                bounds.Encapsulate(new Bounds(shape.point1, Vector3.one * shape.radius * 2));
                return bounds;
            }
            var box = new Bounds(shape.center, Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        box.Encapsulate(shape.center + shape.rotation * Vector3.Scale(shape.halfExtents, new Vector3(x, y, z)));
            return box;
        }

        /// <summary>사용자 씬에 저장하지 않고 격리 프리팹과 재생 그래프를 해제한다.</summary>
        public void Dispose()
        {
            _playback?.Dispose();
            if (Root != null) PrefabUtility.UnloadPrefabContents(Root);
            Root = null;
        }
    }
}
