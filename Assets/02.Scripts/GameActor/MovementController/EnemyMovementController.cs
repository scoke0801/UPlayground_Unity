using System.Collections.Generic;
using JetBrains.Annotations;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.State;
using UPlayGround.Input;

namespace UPlayGround.MovementController
{
    // BeforeCharacterUpdate -> UpdateRotation / UpdateVelocity -> KCC Motor -> AfterCharacterUpdate
    public partial class EnemyMovementController : ActorMovementController
    {
        protected override void RegisterDefaultStates()
        {
            StateMachine.Register(new EnemyIdleState(this));
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
            Debug.Log("Landed");
        }

        private void OnLeaveStableGround()
        {
            Debug.Log("Left ground");
        }
    }
}
