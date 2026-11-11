using KINEMATION.MotionWarping.Runtime.Core;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>모델별 손바닥 접촉점과 탈것을 연결하고 애니메이션 평가 뒤 양팔을 보정한다.</summary>
    public sealed class PlayerGliderBinding : MonoBehaviour
    {
        [SerializeField] private PlayerGlidingSettingsSO _settings;
        [SerializeField] private PlayerGliderView _prefab;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _palmLeft;
        [SerializeField] private Transform _palmRight;
        [SerializeField] private Transform _elbowHintLeft;
        [SerializeField] private Transform _elbowHintRight;

        private Transform _upperLeft, _lowerLeft, _handLeft;
        private Transform _upperRight, _lowerRight, _handRight;
        private Vector3 _palmLeftPosition, _palmRightPosition;
        private Quaternion _palmLeftRotation, _palmRightRotation;
        private PlayerGliderView _view;
        private WeaponIKController _weaponIK;
        private WeaponAimController _weaponAim;
        private bool _wasWeaponIKDisabled;
        private bool _wasWeaponIKEnabled;
        private bool _wasWeaponAimEnabled;
        private bool _hasBoundBones;
        private bool _hasContact;
        private bool _hasTraversal;

        public PlayerGlidingSettingsSO Settings => _settings;
        public PlayerGliderView Prefab => _prefab;
        public PlayerGliderView ActiveView => _hasContact ? _view : null;
        public Transform PalmLeft => _palmLeft;
        public Transform PalmRight => _palmRight;
        public bool CanUse => isActiveAndEnabled && _settings != null && _settings.HasValidValues
            && _prefab != null && _prefab.HasValidBinding && _animator != null
            && _animator.isHuman && _hasBoundBones;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            _weaponIK = GetComponent<WeaponIKController>();
            _weaponAim = GetComponent<WeaponAimController>();
            if (_animator == null || !_animator.isHuman || _palmLeft == null || _palmRight == null) return;
            _upperLeft = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _lowerLeft = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _handLeft = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _upperRight = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lowerRight = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _handRight = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            _hasBoundBones = _upperLeft != null && _lowerLeft != null && _handLeft != null
                && _upperRight != null && _lowerRight != null && _handRight != null
                && _palmLeft.IsChildOf(_handLeft) && _palmRight.IsChildOf(_handRight);
            if (!_hasBoundBones) return;
            _palmLeftPosition = _handLeft.InverseTransformPoint(_palmLeft.position);
            _palmRightPosition = _handRight.InverseTransformPoint(_palmRight.position);
            _palmLeftRotation = Quaternion.Inverse(_handLeft.rotation) * _palmLeft.rotation;
            _palmRightRotation = Quaternion.Inverse(_handRight.rotation) * _palmRight.rotation;
        }

        /// <summary>준비부터 종료까지 장비용 손 IK보다 활공 자세를 우선한다.</summary>
        public void BeginTraversal()
        {
            EndTraversal();
            // 무기 조준 컴포넌트는 모델 Awake 뒤 ActorAnimator.Init에서 추가될 수 있다.
            if (_weaponAim == null) _weaponAim = GetComponent<WeaponAimController>();
            if (_weaponIK != null)
            {
                _wasWeaponIKDisabled = _weaponIK.ForceDisabled;
                _wasWeaponIKEnabled = _weaponIK.enabled;
                _weaponIK.ForceDisabled = true;
                _weaponIK.enabled = false;
            }
            if (_weaponAim != null)
            {
                _wasWeaponAimEnabled = _weaponAim.enabled;
                _weaponAim.enabled = false;
            }
            _hasTraversal = true;
        }

        /// <summary>실제 접촉 판정에 성공한 탈것만 IK 목표로 연결한다.</summary>
        public void Attach(PlayerGliderView view)
        {
            _view = view;
            _hasContact = view != null;
        }

        /// <summary>모델 비활성화와 중복 종료에도 이전 장비 IK 상태를 복구한다.</summary>
        public void EndTraversal()
        {
            _hasContact = false;
            _view = null;
            if (_hasTraversal && _weaponIK != null)
            {
                _weaponIK.ForceDisabled = _wasWeaponIKDisabled;
                _weaponIK.enabled = _wasWeaponIKEnabled;
            }
            if (_hasTraversal && _weaponAim != null) _weaponAim.enabled = _wasWeaponAimEnabled;
            _hasTraversal = false;
        }

        private void OnDisable() => EndTraversal();

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_hasContact || _view == null || !_hasBoundBones) return;
            SolveHand(_upperLeft, _lowerLeft, _handLeft, _view.GripLeft,
                _palmLeftPosition, _palmLeftRotation, _elbowHintLeft);
            SolveHand(_upperRight, _lowerRight, _handRight, _view.GripRight,
                _palmRightPosition, _palmRightRotation, _elbowHintRight);
        }

        private static void SolveHand(Transform upper, Transform lower, Transform hand,
            Transform grip, Vector3 palmPosition, Quaternion palmRotation, Transform hint)
        {
            Quaternion rotation = grip.rotation * Quaternion.Inverse(palmRotation);
            Vector3 position = grip.position - rotation * palmPosition;
            TwoBoneIk.SolveTwoBoneIK(upper, lower, hand, (position, rotation),
                hint: hint, targetWeight: 1f, hintWeight: hint != null ? 1f : 0f);
        }
    }
}
