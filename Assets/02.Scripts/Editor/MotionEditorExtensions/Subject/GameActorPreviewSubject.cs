using Animancer;
using KinematicCharacterController;
using UPlayGround.Manager;
using UPlayGround.Components;
using UPlayGround.MovementController;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed class GameActorPreviewBinder : IMotionPreviewSubjectBinder
    {
        public int Priority => 50;

        public IMotionPreviewSubject TryBind(GameObject root)
        {
            UPlayGround.Animation.ActorAnimator animator =
                root != null
                    ? root.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true)
                    : null;
            return animator != null
                ? new GameActorPreviewSubject(root, animator)
                : null;
        }
    }

    public class GameActorPreviewSubject :
        IMotionPreviewSubject,
        IMotionPreviewRootMotion,
        IMotionPreviewPlaybackOwnership,
        IMotionPreviewSubjectSession
    {
        private readonly GameObject _root;
        protected UPlayGround.Animation.ActorAnimator ActorAnimator;
        protected ActorMovementController Movement;
        private ActorAnimationMotionSetCatalog _catalog;
        private bool _simulationStateCaptured;
        private bool _motorWasEnabled;
        private bool _previewOwnershipActive;
        private UPlayGround.Animation.ActorAnimator _previewOwnedAnimator;
        private bool _spawnSessionActive;
        private bool _spawnMotorWasEnabled;
        private EnemyAIController[] _enemyBrains;
        private bool[] _enemyBrainStates;
        private EnemyFlyingAIController[] _flyingBrains;
        private bool[] _flyingBrainStates;
        private Transform _cameraTargetBeforePreview;
        private bool _cameraTargetCaptured;

        public GameActorPreviewSubject(
            GameObject root,
            UPlayGround.Animation.ActorAnimator animator)
        {
            _root = root;
            ActorAnimator = animator;
            RefreshCore();
        }

        public GameObject Root => _root;
        public virtual AnimancerComponent Animancer =>
            ActorAnimator != null
                ? ActorAnimator.GetAnimancerComponent()
                  ?? ActorAnimator.GetComponent<AnimancerComponent>()
                : null;
        public virtual IMotionSetCatalog Catalog => _catalog;
        public Vector3 DeltaPosition =>
            ActorAnimator != null ? ActorAnimator.DeltaPosition : Vector3.zero;
        public Quaternion DeltaRotation =>
            ActorAnimator != null ? ActorAnimator.DeltaRotation : Quaternion.identity;

        public AvatarMask GetLayerMask(int layerIndex) =>
            layerIndex > 0 && ActorAnimator != null
                ? ActorAnimator.UpperBodyMask
                : null;

        public virtual void Refresh()
        {
            RefreshCore();
        }

        private void RefreshCore()
        {
            if (_root == null)
                return;

            ActorAnimator =
                _root.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true);
            Movement = _root.GetComponentInChildren<ActorMovementController>(true);
            _catalog = ActorAnimator != null && ActorAnimator.MotionSet != null
                ? new ActorAnimationMotionSetCatalog(ActorAnimator.MotionSet)
                : null;
            RefreshPreviewOwnership();
        }

        public void SetSimulationSuspended(bool suspended)
        {
            KinematicCharacterMotor motor = Movement != null ? Movement.Motor : null;
            if (motor == null)
                return;

            if (suspended)
            {
                if (!_simulationStateCaptured)
                {
                    _motorWasEnabled = motor.enabled;
                    _simulationStateCaptured = true;
                }
                motor.enabled = false;
            }
            else if (_simulationStateCaptured)
            {
                motor.enabled = _motorWasEnabled;
                _simulationStateCaptured = false;
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            KinematicCharacterMotor motor = Movement != null ? Movement.Motor : null;
            if (motor != null)
                motor.SetPositionAndRotation(position, rotation);
            else if (_root != null)
                _root.transform.SetPositionAndRotation(position, rotation);
        }

        public virtual void OnPreviewLoaded(bool spawned)
        {
            if (!spawned || _root == null || _spawnSessionActive)
                return;

            _spawnSessionActive = true;
            KinematicCharacterMotor motor = Movement != null ? Movement.Motor : null;
            if (motor != null)
            {
                _spawnMotorWasEnabled = motor.enabled;
                motor.enabled = false;
            }

            _enemyBrains = _root.GetComponentsInChildren<EnemyAIController>(true);
            _enemyBrainStates = CaptureAndDisable(_enemyBrains);
            _flyingBrains =
                _root.GetComponentsInChildren<EnemyFlyingAIController>(true);
            _flyingBrainStates = CaptureAndDisable(_flyingBrains);
            FocusGameCamera();
        }

        /// <summary>게임 카메라 추적 대상을 스폰한 프리뷰 액터로 옮긴다.</summary>
        /// <remarks>
        /// 프리뷰 전용 카메라를 따로 세우면 화면에는 잡히지만 프로젝트의 회전·줌·
        /// 락온 조작이 전부 무력화된다. 기존 카메라를 그대로 쓰고 추적 대상만 바꾸면
        /// 인게임과 동일한 조작으로 프리뷰를 볼 수 있다.
        /// </remarks>
        private void FocusGameCamera()
        {
            CameraManager camera = CameraManager.Instance;
            if (camera == null || _root == null || _cameraTargetCaptured)
                return;

            _cameraTargetBeforePreview = camera.GetTarget();
            _cameraTargetCaptured = true;
            camera.SetTarget(_root.transform);
        }

        private void RestoreGameCamera()
        {
            if (!_cameraTargetCaptured)
                return;

            _cameraTargetCaptured = false;
            CameraManager camera = CameraManager.Instance;
            if (camera != null && _cameraTargetBeforePreview != null)
                camera.SetTarget(_cameraTargetBeforePreview);
            _cameraTargetBeforePreview = null;
        }

        public void OnPreviewReleased()
        {
            ReleasePreviewOwnership();
            RestoreGameCamera();

            if (!_spawnSessionActive)
                return;

            KinematicCharacterMotor motor = Movement != null ? Movement.Motor : null;
            if (motor != null)
                motor.enabled = _spawnMotorWasEnabled;
            Restore(_enemyBrains, _enemyBrainStates);
            Restore(_flyingBrains, _flyingBrainStates);
            _enemyBrains = null;
            _enemyBrainStates = null;
            _flyingBrains = null;
            _flyingBrainStates = null;
            _spawnSessionActive = false;
        }

        public void AcquirePreviewOwnership()
        {
            if (_previewOwnershipActive)
                return;

            _previewOwnershipActive = true;
            RefreshPreviewOwnership();
        }

        public void ReleasePreviewOwnership()
        {
            if (!_previewOwnershipActive)
                return;

            if (_previewOwnedAnimator != null)
                _previewOwnedAnimator.EndExternalPreview();
            _previewOwnedAnimator = null;
            _previewOwnershipActive = false;
        }

        protected void RefreshPreviewOwnership()
        {
            if (!_previewOwnershipActive ||
                _previewOwnedAnimator == ActorAnimator)
                return;

            if (_previewOwnedAnimator != null)
                _previewOwnedAnimator.EndExternalPreview();
            _previewOwnedAnimator = ActorAnimator;
            if (_previewOwnedAnimator != null)
                _previewOwnedAnimator.BeginExternalPreview();
        }

        private static bool[] CaptureAndDisable<T>(T[] behaviours)
            where T : Behaviour
        {
            bool[] states = new bool[behaviours?.Length ?? 0];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = behaviours[i] != null && behaviours[i].enabled;
                if (behaviours[i] != null)
                    behaviours[i].enabled = false;
            }
            return states;
        }

        private static void Restore<T>(T[] behaviours, bool[] states)
            where T : Behaviour
        {
            if (behaviours == null || states == null)
                return;
            int count = Mathf.Min(behaviours.Length, states.Length);
            for (int i = 0; i < count; i++)
            {
                if (behaviours[i] != null)
                    behaviours[i].enabled = states[i];
            }
        }
    }
}
