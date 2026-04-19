using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 처형 공격 상태 (FinishAttack)
    /// - HeavyAttack 입력 시 HP 임계값 이하 대상이 범위 내 존재할 때 진입
    /// - 진입 시 주변 모든 적의 AI를 정지, 종료 시 재개
    /// - 애니메이션 초반에 발차기 사거리에 맞춰 타겟 인접 위치로 부드럽게 슬라이딩(접근)
    /// </summary>
    public class PlayerFinishAttackState : PlayerActorState
    {
        public override string StateName => "FinishAttack";
        public override bool GrantsInvincibility => true;

        [Header("Freeze Settings")]
        private const float FREEZE_RADIUS = 15f;

        // --- Finish Move Settings ---
        // 발차기가 자연스럽게 닿는 최적의 거리 (캐릭터 모델 비율에 맞춰 조절하세요)
        private const float IDEAL_DISTANCE = 0.8f; 
        private const float MAX_SLIDE_SPEED = 20f; // 슬라이딩 최대 속도 (텔레포트 방지)

        private readonly Transform _finishTarget;
        private PlayerCombat _combat;
        private List<EnemyBrain> _frozenBrains = new List<EnemyBrain>();

        private Vector3 _targetPosition;
        private bool _isSliding;
        private float _stateTimer;

        /// <summary>FinishSideViewEvent 등 모션 이벤트에서 처형 타겟 참조용</summary>
        public Transform FinishTarget => _finishTarget;

        public PlayerFinishAttackState(ActorMovementController controller, Transform finishTarget)
            : base(controller)
        {
            _finishTarget = finishTarget;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _combat = playerActor.GetCombat();
            _stateTimer = 0f;
            _combat.SetupFinishAttackData();

            // 슬라이딩 목표 위치 계산
            if (_finishTarget != null)
            {
                _isSliding = true;
                Vector3 dirFromTargetToMe = (gameActor.transform.position - _finishTarget.position);
                dirFromTargetToMe.y = 0f;

                // 타겟의 중심에서 IDEAL_DISTANCE 만큼 내 쪽으로 떨어진 위치를 목표로 설정
                _targetPosition = _finishTarget.position + (dirFromTargetToMe.normalized * IDEAL_DISTANCE);
            }

            // FinishAttack 애니메이션 재생
            var animState = gameActor.Animator.PlayMotion(AnimKey.FinishAttack, 0.15f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = OnFinishAttackEnd;
            }
            else
            {
                OnFinishAttackEnd();
                return;
            }

            // 주변 모든 적 Freeze
            _frozenBrains = _combat.GetEnemyBrainsInRadius(FREEZE_RADIUS);
            foreach (var brain in _frozenBrains)
                brain.Freeze();
        }

        public override void OnExit(GameActorState toState)
        {
            // 모든 적 Unfreeze
            foreach (var brain in _frozenBrains)
            {
                if (brain != null)
                    brain.Unfreeze();
            }
            _frozenBrains.Clear();

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _stateTimer += deltaTime;
            // 처형 애님 진행 중에는 다른 입력 차단
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_isSliding && _finishTarget != null)
            {
                Vector3 toTarget = _targetPosition - gameActor.transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                // 목표 위치에 거의 도달했거나 일정 시간(예: 0.3초)이 지나면 슬라이딩 종료
                if (distance > 0.05f && _stateTimer < 0.3f)
                {
                    // 거리에 비례하여 속도를 결정 (멀면 빠르고, 가까우면 부드럽게 감속)
                    float speed = Mathf.Clamp(distance * 15f, 0f, MAX_SLIDE_SPEED);
                    currentVelocity = toTarget.normalized * speed;
                    return;
                }
                else
                {
                    _isSliding = false; // 슬라이딩 종료
                }
            }

            // 슬라이딩이 끝났거나 타겟이 없으면 제자리 고정
            currentVelocity = Vector3.zero;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_finishTarget == null)
                return;

            Vector3 dir = (_finishTarget.position - gameActor.transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                // 즉시 홱 도는 것보다 아주 약간의 보간을 주면 슬라이딩과 함께 훨씬 자연스럽습니다.
                currentRotation = Quaternion.Slerp(currentRotation, targetRot, deltaTime * 20f);
            }

            currentRotation = currentRotation.normalized;
        }

        private void OnFinishAttackEnd()
        {
            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}