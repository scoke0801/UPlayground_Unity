using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Combat;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 전방 직선 대시 상태
    /// - 대시 중 Enemy 레이어를 CollidableLayers에서 제거해 충돌 무시
    /// - 대시 종료 시 ComputePenetration으로 겹침 해소 후 레이어 복구
    /// </summary>
    public class PlayerDashState : PlayerActorState
    {
        public override string StateName => "Dash";
        public override bool GrantsInvincibility => true;

        private Vector3 _dashDirection;
        
        private readonly List<Collider> _ignoredOnDodge = new();
        private readonly List<EnemyMovementController> _enemyControllers = new();
        
        public PlayerDashState(ActorMovementController controller) : base(controller) { }

        // 상태 전환 제한
        public override bool CanTransitionState(string stateName)
        {
            if (playerController != null && playerController.IsDashReady == false)
                return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTagId.State_Dash);
            playerActor?.ComboInputTracker.Push(ComboInputToken.Dash);

            _dashDirection = playerController.HasMoveInput()
                ? playerController.MoveInputVector.normalized
                : motor.CharacterForward;

            IgnoreMonsterColliders();

            AnimKey dashKey = ResolveDirectionalMotionKey(
                AnimKey.Dash_F, AnimKey.Dash_B, AnimKey.Dash_L, AnimKey.Dash_R, AnimKey.Dash);
            var animState = gameActor.Animator.PlayMotion(dashKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAnimationEnd;
            }
            else
                FinishDash();
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Dash);
            RestoreAndResolvePenetration();

            gameActor.Animator.OnMotionSetCompleted -= OnAnimationEnd;
            playerController.StartDashCooldown();

            // Dash하면 Sprint
            gameActor.MoveAnimType = BaseMoveAnimType.Sprint;
            base.OnExit(toState);
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = _dashDirection * controller.DashSpeed;
            currentVelocity.y = 0f;
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround &&
                InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                playerController.TryTransitionToState(new PlayerJumpDashAttackState(playerController));
            }
        }

        private void IgnoreMonsterColliders()
        {
            _ignoredOnDodge.Clear();
            _enemyControllers.Clear();

            int monsterLayer = LayerMask.GetMask("Enemy");

            Vector3 capsuleBottom = motor.TransientPosition + motor.CharacterUp * motor.Capsule.radius;
            Vector3 capsuleTop    = motor.TransientPosition + motor.CharacterUp * (motor.Capsule.height - motor.Capsule.radius);

            var hits = Physics.OverlapCapsule(capsuleBottom, capsuleTop, 5f, monsterLayer);

            foreach (var col in hits)
            {
                _ignoredOnDodge.Add(col);

                // 플레이어 컨트롤러에서 몬스터 콜라이더 무시
                controller.AddIgnoreCollider(col);

                // 몬스터 컨트롤러에서 플레이어 캡슐 무시 (양방향)
                var enemyController = col.GetComponentInParent<EnemyMovementController>();
                if (enemyController != null)
                {
                    enemyController.AddIgnoreCollider(motor.Capsule);
                    _enemyControllers.Add(enemyController);
                }
            }
        }

        private void RestoreAndResolvePenetration()
        {
            Vector3 resolvedPosition = motor.TransientPosition;

            // ComputePenetration으로 겹침 해소
            foreach (var col in _ignoredOnDodge)
            {
                if (col == null) continue;

                bool overlapping = Physics.ComputePenetration(
                    motor.Capsule,      resolvedPosition,        motor.TransientRotation,
                    col,                col.transform.position,  col.transform.rotation,
                    out Vector3 dir,    out float dist
                );

                if (overlapping)
                    resolvedPosition += dir * (dist + 0.01f);
            }

            if (resolvedPosition != motor.TransientPosition)
                motor.SetPosition(resolvedPosition);

            // 플레이어 쪽 무시 목록 해제
            foreach (var col in _ignoredOnDodge)
            {
                if (col != null)
                    controller.RemoveIgnoreCollider(col);
            }

            // 몬스터 쪽 무시 목록 해제 (양방향)
            foreach (var enemyController in _enemyControllers)
            {
                if (enemyController != null)
                    enemyController.RemoveIgnoreCollider(motor.Capsule);
            }

            _ignoredOnDodge.Clear();
            _enemyControllers.Clear();
        }
        
        private void FinishDash()
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }

        private void OnAnimationEnd()
        {
            if (controller.CurrentState == this)
                FinishDash();
        }
    }
}
