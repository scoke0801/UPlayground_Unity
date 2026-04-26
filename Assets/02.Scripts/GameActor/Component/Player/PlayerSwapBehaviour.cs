using KinematicCharacterController;
using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Component
{
    /// <summary>
    /// 파티 교체 시 캐릭터의 활성/대기 전환을 담당하는 단일 책임 컴포넌트.
    ///
    /// 4개 레이어를 일괄 제어한다:
    ///   1. Input       — PlayerActor.enabled (OnEnable/OnDisable에서 자동 등록·해제)
    ///   2. StateMachine— PlayerMovementController.enabled (Update 중단 → InputBuffer 비소비)
    ///   3. Physics     — KinematicCharacterMotor.enabled + CapsuleCollider.enabled
    ///   4. Visual      — Renderer.enabled
    /// </summary>
    public class PlayerSwapBehaviour : MonoBehaviour
    {
        private PlayerActor              _playerActor;
        private PlayerMovementController _movementController;
        private KinematicCharacterMotor  _motor;
        private GameObject               _modelRoot;

        private bool _isActive = true;
        public bool IsActive => _isActive;

        private void Awake()
        {
            _playerActor        = GetComponent<PlayerActor>();
            _movementController = GetComponent<PlayerMovementController>();
            _motor              = GetComponent<KinematicCharacterMotor>();

            var modelTransform = transform.Find("Model");
            if (modelTransform != null)
                _modelRoot = modelTransform.gameObject;
            else
                Debug.LogWarning($"[PlayerSwapBehaviour] {name}: 'Model' 자식을 찾을 수 없습니다.");
        }

        /// <summary>
        /// 이 캐릭터를 조작 가능 상태로 전환.
        /// outgoing 캐릭터의 위치·회전을 이어받아 등장한다.
        /// </summary>
        public void EnterActive(Vector3 position, Quaternion rotation)
        {
            if (_isActive) return;
            _isActive = true;

            // 1. Physics: 콜라이더 먼저 복원 후 모터 활성화 → 위치 설정
            if (_motor.Capsule != null) _motor.Capsule.enabled = true;
            _motor.enabled = true;
            _motor.SetPositionAndRotation(position, rotation);

            // 2. Visual
            _modelRoot?.SetActive(true);

            // 3. StateMachine + Input
            //    enabled = true → OnEnable() 자동 호출 → 입력 등록
            _movementController.enabled = true;
            _playerActor.enabled        = true;

            // 4. 클린 Idle 상태 진입 (공격·차지 중이었어도 안전하게 초기화)
            _movementController.TransitionToState(new PlayerIdleState(_movementController));
        }

        /// <summary>
        /// 이 캐릭터를 대기 상태로 전환.
        /// 입력·물리·렌더러·상태머신을 모두 비활성화한다.
        /// </summary>
        public void EnterStandby()
        {
            if (!_isActive) return;
            _isActive = false;

            // 1. Input + StateMachine
            //    enabled = false → OnDisable() 자동 호출 → 입력 해제 + 상태 초기화
            _playerActor.enabled        = false;
            _movementController.enabled = false;

            // 2. Visual
            _modelRoot?.SetActive(false);

            // 3. Physics: 모터 비활성화 후 콜라이더 비활성화
            //    (EnemyDetection.OverlapSphere가 대기 캐릭터를 감지하지 않도록)
            _motor.enabled = false;
            if (_motor.Capsule != null) _motor.Capsule.enabled = false;
        }

    }
}
