using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 처형 공격 상태 (FinishAttack)
    /// - HeavyAttack 입력 시 HP 임계값 이하 대상이 범위 내 존재할 때 진입
    /// - 진입 시 주변 모든 적의 AI를 정지, 종료 시 재개
    /// - 이동·회전은 원본 루트모션과 저작된 워프 구간을 사용한다.
    /// </summary>
    public class PlayerFinishAttackState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.FinishAttack;
        protected override ActorStateTag StateTagsCore => ActorStateTag.Combat;
        public override bool GrantsInvincibility => true;

        [Header("Freeze Settings")]
        private const float FREEZE_RADIUS = 15f;

        private readonly Transform _finishTarget;
        private PlayerCombat _combat;
        private List<IEnemyAIController> _frozenEnemyControllers = new List<IEnemyAIController>();

        private MotionWarpController _motionWarp;

        /// <summary>FinishSideViewEvent 등 모션 이벤트에서 처형 타겟 참조용</summary>
        public Transform FinishTarget => _finishTarget;
        public bool IsTransitionLocked { get; private set; }

        public override bool BlocksExitTo(GameActorState newState)
            => IsTransitionLocked && newState?.StateId != ActorStateId.Ultimate;

        public PlayerFinishAttackState(ActorMovementController controller, Transform finishTarget)
            : base(controller)
        {
            _finishTarget = finishTarget;
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _combat = playerActor.GetCombat();
            IsTransitionLocked = true;
            if (_combat == null || !_combat.IsFinishableTarget(_finishTarget))
            {
                Debug.LogWarning($"[FinishAttack] 피니시 불가 대상입니다. target={_finishTarget?.name}");
                IsTransitionLocked = false;
                TransitionToIdleOrMove();
                return;
            }

            playerActor.GetPlayerEquipment()?.SetMainWeaponDrawn(true);
            _motionWarp = controller.MotionWarp;
            _combat.SetupFinishAttackData(_finishTarget);

            _motionWarp.EndMotionWarpForStateExit();
            _motionWarp.BeginTargetLock();
            _motionWarp.SetTarget(_finishTarget, useSnapshot: true);

            // FinishAttack 애니메이션 재생
            var animState = gameActor.Animator.PlayMotion(
                UPlayGround.Data.Actor.Animation.MotionTags.FinishAttack, 0.15f);
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
            _combat.FillEnemyAIControllersInRadius(FREEZE_RADIUS, _frozenEnemyControllers);
            foreach (var brain in _frozenEnemyControllers)
            {
                if (IsValidAIController(brain))
                    brain.Freeze();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            IsTransitionLocked = false;
            _motionWarp?.EndMotionWarpForStateExit();
            _motionWarp?.EndTargetLock();
            _motionWarp?.ClearTarget();

            // 모든 적 Unfreeze
            foreach (var brain in _frozenEnemyControllers)
            {
                if (IsValidAIController(brain))
                    brain.Unfreeze();
            }
            _frozenEnemyControllers.Clear();

            base.OnExit(toState);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);
            Vector3 vertical = Vector3.Project(currentVelocity, motor.CharacterUp);
            Vector3 raw = gameActor.Animator.GetRootMotionStepVelocity(deltaTime);
            Vector3 velocity = _motionWarp.EvaluateVelocity(raw, motor.TransientPosition,
                _motionWarp.IsMotionWarping, _motionWarp.WarpRemainingTime, _motionWarp.WarpDuration,
                0f, _combat.AttackAssistSearchRange, 0f, deltaTime);
            velocity = _motionWarp.ClampApproachVelocity(velocity, motor.TransientPosition, deltaTime);
            currentVelocity = Vector3.ProjectOnPlane(velocity, motor.CharacterUp) + vertical;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.RootMotionStepDeltaRotation;
            if (_motionWarp.TryEvaluateRotation(currentRotation, motor.TransientPosition,
                    _motionWarp.IsMotionWarping, _motionWarp.WarpRemainingTime, _motionWarp.WarpDuration,
                    0f, _combat.AttackAssistSearchRange, 0f, out Quaternion rotation))
                currentRotation = rotation;
            currentRotation = currentRotation.normalized;
        }

        private void OnFinishAttackEnd()
        {
            // 다른 모션으로 블렌드 아웃된 AnimancerState의 OnEnd가 뒤늦게 발화해도
            // 새 상태(특히 Ultimate)의 모션을 Idle로 덮어쓰지 않는다.
            if (controller.CurrentState != this)
                return;

            IsTransitionLocked = false;
            TransitionToIdleOrMove();
        }

        private void TransitionToIdleOrMove()
        {
            if (playerController.HasMoveInput())
                controller.TransitionToState(ActorStateId.GroundMove);
            else
                controller.TransitionToState(ActorStateId.Idle);
        }

        private static bool IsValidAIController(IEnemyAIController controller)
        {
            return controller is UnityEngine.Object unityObject && unityObject != null;
        }
    }
}
