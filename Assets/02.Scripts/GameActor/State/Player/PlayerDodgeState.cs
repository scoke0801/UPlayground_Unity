using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 구르기 상태
    /// </summary>
    public class PlayerDodgeState : PlayerActorState
    {
        public override string StateName => "Dodge";
        
        private readonly List<Collider> _ignoredOnDodge = new();
        private readonly List<EnemyMovementController> _enemyControllers = new();

        public PlayerDodgeState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit")
                return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 도지 시작 즉시 퍼펙트 도지 판정 창 열기
            playerActor.GetCombat()?.OpenPerfectDodgeWindow();

            IgnoreMonsterColliders();

            gameActor.Animator.ApplyRootMotion(true);

            var animState = gameActor.Animator.PlayMotion(AnimKey.Dodge, 0.25f);
            if (animState != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
        }

        public override void OnExit(GameActorState toState)
        {
            RestoreAndResolvePenetration();
            
            gameActor.Animator.ApplyRootMotion(false);
            
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;

            base.OnExit(toState);
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // // Drag
            // currentVelocity *= (1f / (1f + (controller.LandDrag * deltaTime)));
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }
        private void ChangeToNextState()
        {
            // 이동 입력이 있으면 GroundMove, 없으면 Idle
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
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

                // 1. 플레이어 컨트롤러에서 몬스터 콜라이더 무시
                controller.AddIgnoreCollider(col);

                // 2. 몬스터 컨트롤러에서 플레이어 캡슐 무시 (양방향)
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
    }
}