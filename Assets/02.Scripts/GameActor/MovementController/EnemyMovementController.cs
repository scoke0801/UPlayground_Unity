using System.Collections.Generic;
using JetBrains.Annotations;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.State;
using UPlayGround.Input;
using UPlayGround.Diagnostics;

namespace UPlayGround.MovementController
{
    // BeforeCharacterUpdate -> UpdateRotation / UpdateVelocity -> KCC Motor -> AfterCharacterUpdate
    public partial class EnemyMovementController : ActorMovementController
    {
        protected override void RegisterDefaultStates()
        {
            StateMachine.Register(new EnemyIdleState(this));
            StateMachine.Register(new EnemyAirborneState(this));
            StateMachine.Register(new EnemyChaseState(this));
            StateMachine.Register(new EnemyCircleState(this));
            StateMachine.Register(new EnemyStageApproachState(this));
            StateMachine.Register(new EnemyIncapacitatedState(this));
            StateMachine.Register(new EnemyFlyingChaseState(this));
        }

        protected override void Start()
        {
            base.Start();
            
            TransitionToState(ActorStateId.Idle);
        }
        
        // IgnoredColliders 필터는 ActorMovementController.IsColliderValidForCollisions로 통합됨.
    }
    
    public partial class EnemyMovementController : ActorMovementController
    {
        private void OnLanded()
        {
            RuntimeLog.Trace(
                RuntimeLogCategory.Combat | RuntimeLogCategory.Monster,
                "[MonsterMovement] 착지",
                Actor != null ? Actor : this);
        }

        private void OnLeaveStableGround()
        {
            RuntimeLog.Trace(
                RuntimeLogCategory.Combat | RuntimeLogCategory.Monster,
                "[MonsterMovement] 지면 이탈",
                Actor != null ? Actor : this);
        }
    }
}
