using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Animancer;
using UPlayGround.Animation;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>단일 프레임 미리보기에서 스킨과 소켓을 같은 샘플 자세로 표시한다.</summary>
    internal sealed class P09PosePreview : IDisposable
    {
        private readonly GameObject _root;
        private readonly AnimancerComponent _animancer;
        private readonly MotionSetPlaybackController _playback;
        private MotionSetAsset _asset;
        private readonly ParentConstraint[] _constraints;
        private readonly List<SkinnedMeshRenderer> _skins = new();
        private readonly List<Mesh> _meshes = new();
        private readonly List<GameObject> _views = new();

        public P09PosePreview(GameObject root)
        {
            _root = root;
            var animator = root.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animancer = animator.GetComponent<AnimancerComponent>();
            if (_animancer == null) _animancer = animator.gameObject.AddComponent<AnimancerComponent>();
            _animancer.Animator = animator;
            _animancer.Graph.UpdateMode = DirectorUpdateMode.Manual;
            _playback = new MotionSetPlaybackController(_animancer);
            _constraints = root.GetComponentsInChildren<ParentConstraint>(true);
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.enabled) continue;
                var mesh = new Mesh { name = "P09 Pose Preview", hideFlags = HideFlags.HideAndDontSave };
                var view = new GameObject("P09 Pose Preview") { hideFlags = HideFlags.HideAndDontSave };
                view.transform.SetParent(skin.transform, false);
                view.AddComponent<MeshFilter>().sharedMesh = mesh;
                view.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                _skins.Add(skin);
                _meshes.Add(mesh);
                _views.Add(view);
                skin.enabled = false;
            }
        }

        /// <summary>게임 이벤트를 실행하지 않고 클립·제약·스킨을 샘플링한다.</summary>
        public void Sample(MotionSetAsset asset, float normalizedTime, ParentConstraint staff)
        {
            if (asset == null) return;
            if (_asset != asset)
            {
                if (!_playback.Play(asset)) throw new InvalidOperationException("미리보기 모션 재생 실패: " + asset.name);
                _asset = asset;
            }
            var position = _root.transform.position;
            var rotation = _root.transform.rotation;
            _playback.SeekNormalized(normalizedTime);
            _animancer.Evaluate(0f);
            _root.transform.SetPositionAndRotation(position, rotation);
            P09StaffAttachment.PosePreview(staff);
            foreach (var constraint in _constraints)
                EvaluateConstraint(constraint);
            // 일반 PlayerLoop 없이 연속 촬영하면 GPU 스킨이 이전 자세를 재사용한다.
            // 에디터 미리보기 전용 메시로 베이크하여 손과 몸의 평가 시점을 맞춘다.
            for (int i = 0; i < _skins.Count; i++)
                _skins[i].BakeMesh(_meshes[i]);
        }

        private static void EvaluateConstraint(ParentConstraint constraint)
        {
            if (!constraint.enabled || !constraint.constraintActive || constraint.weight <= 0f ||
                !constraint.gameObject.activeInHierarchy) return;
            float total = 0f;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            for (int i = 0; i < constraint.sourceCount; i++)
            {
                var source = constraint.GetSource(i);
                if (source.sourceTransform == null || source.weight <= 0f) continue;
                var sourceRotation = source.sourceTransform.rotation * Quaternion.Euler(constraint.GetRotationOffset(i));
                position += source.sourceTransform.TransformPoint(constraint.GetTranslationOffset(i)) * source.weight;
                total += source.weight;
                rotation = Quaternion.Slerp(rotation, sourceRotation, source.weight / total);
            }
            if (total <= 0f) return;
            var target = constraint.transform;
            target.SetPositionAndRotation(Vector3.Lerp(target.position, position / total, constraint.weight),
                Quaternion.Slerp(target.rotation, rotation, constraint.weight));
        }

        /// <summary>임시 메시와 뷰를 제거하고 원본 렌더러를 복원한다.</summary>
        public void Dispose()
        {
            _playback.Dispose();
            for (int i = 0; i < _skins.Count; i++)
            {
                if (_skins[i] != null) _skins[i].enabled = true;
                UnityEngine.Object.DestroyImmediate(_views[i]);
                UnityEngine.Object.DestroyImmediate(_meshes[i]);
            }
            _skins.Clear();
            _views.Clear();
            _meshes.Clear();
        }
    }
}
