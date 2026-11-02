using UPlayGround.Components;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public partial class PlayerMovementController
    {
        public PlayerGliderBinding GliderBinding { get; private set; }
        public bool HasUsedSuperJump { get; private set; }
        public bool IsTraversing => CurrentState is PlayerSuperJumpGlidingState { IsActive: true };

        /// <summary>모델 교체 전에 이전 활공을 정리하고 새 모델의 검수된 바인딩을 받는다.</summary>
        public void SetGliderBinding(PlayerGliderBinding binding)
        {
            CancelGliding();
            GliderBinding = binding;
        }

        /// <summary>생존·입력·접지·모션 조건이 모두 맞는 지상 상태에서만 시작한다.</summary>
        public bool TryStartSuperJump()
        {
            if (Actor is not PlayerActor { IsInputSuppressed: false } player || !player.IsAlive()
                || Motor == null || !Motor.GroundingStatus.IsStableOnGround
                || CurrentState?.StateId is not (ActorStateId.Idle or ActorStateId.GroundMove
                    or ActorStateId.Stop or ActorStateId.TurnInPlace)
                || GliderBinding == null || !GliderBinding.CanUse)
                return false;

            // 이전 도약의 권한은 실제 접지와 조작 가능한 지상 상태가 함께 확인될 때만 회복한다.
            HasUsedSuperJump = false;
            return TryTransitionToState(ActorStateId.SuperJumpGliding);
        }

        /// <summary>놓기 입력은 새 performed 콜백에서만 전달한다. 기존 점프 버퍼는 사용하지 않는다.</summary>
        public void ReleaseGlider()
        {
            if (CurrentState is PlayerSuperJumpGlidingState state) state.RequestRelease();
        }

        /// <summary>모델·액터 비활성화와 강제 교체에서 중복 호출 가능한 종료를 수행한다.</summary>
        public void CancelGliding()
        {
            if (CurrentState is PlayerSuperJumpGlidingState state) state.Cleanup();
        }

        internal void SpendSuperJump() => HasUsedSuperJump = true;

        public override void PostGroundingUpdate(float deltaTime)
        {
            base.PostGroundingUpdate(deltaTime);
            if (!IsTraversing && Motor.GroundingStatus.IsStableOnGround)
                HasUsedSuperJump = false;
        }

        protected override void OnDisable()
        {
            CancelGliding();
            base.OnDisable();
        }
    }
}
