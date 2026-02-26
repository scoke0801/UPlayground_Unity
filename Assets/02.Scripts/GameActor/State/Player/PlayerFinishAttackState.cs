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
    /// - 실제 데미지는 애니메이션 이벤트로 처리
    /// </summary>
    public class PlayerFinishAttackState : PlayerActorState
    {
        public override string StateName => "FinishAttack";

        [Header("Freeze Settings")]
        private const float FREEZE_RADIUS = 15f;

        private readonly Transform _finishTarget;
        private PlayerCombat _combat;
        private List<EnemyBrain> _frozenBrains = new List<EnemyBrain>();

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
            // 처형 애님 진행 중에는 다른 입력 차단
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 처형 모션 중 제자리 고정
            currentVelocity = Vector3.zero;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 처형 타겟 방향 고정
            if (_finishTarget == null)
                return;

            Vector3 dir = (_finishTarget.position - gameActor.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                currentRotation = Quaternion.LookRotation(dir.normalized);

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
