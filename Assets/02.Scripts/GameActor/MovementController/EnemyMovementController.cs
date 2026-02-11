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
        protected override void Start()
        {
            base.Start();
            
            TransitionToState(new EnemyIdleState(this));
        }
        
        /// <summary>
        /// 호출 시점: 모터가 주변의 물리적 장애물을 감지하고 충돌 계산을 시작하기 직전, 매 충돌 후보마다 호출됩니다.
        /// 역할: 특정 콜라이더와 충돌할지 말지를 결정하는 **'통행권 체크'**입니다.
        /// </summary>
        public override bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }
            return base.IsColliderValidForCollisions(coll);
        }
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