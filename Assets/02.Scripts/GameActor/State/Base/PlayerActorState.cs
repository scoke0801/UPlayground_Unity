using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 모든 Actor 이동 상태의 베이스 클래스
    /// </summary>
    public abstract class PlayerActorState : GameActorState
    {
        protected PlayerMovementController playerController;
        protected PlayerActor playerActor;

        /// <summary>
        /// 이 상태가 진입 시 반드시 재생해야 하는 모션 키.
        /// null 이면 모션 보유 여부와 무관하게 진입 가능.
        /// DashAttack / JumpAttack 처럼 전용 모션이 없으면 의미가 없는 상태에서 오버라이드한다.
        /// </summary>
        protected virtual UPlayGround.Gameplay.Tag.GameplayTag? RequiredMotionKey => null;

        /// <summary>
        /// 액터가 RequiredMotionKey 에 해당하는 모션을 보유하고 있는지 확인.
        /// 상태 전이 가드(<see cref="CanTransitionState"/>) 에서 호출.
        /// </summary>
        protected bool HasRequiredMotion()
        {
            if (RequiredMotionKey.HasValue == false) return true;

            var animator = gameActor?.Animator;
            if (animator == null) return false;

            return animator.HasMotion(RequiredMotionKey.Value, true);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            playerActor = gameActor as PlayerActor;
        }

        protected PlayerActorState(ActorMovementController controller) : base(controller)
        {
            playerController = controller as PlayerMovementController;
        }

        /// <summary>제스처가 라인마다 교체될 때의 페이드 시간. 진입 페이드보다 짧게 잡아 대사 호흡을 놓치지 않는다.</summary>
        protected const float DialogueGestureSwapFade = 0.18f;

        /// <summary>마지막으로 확인한 대화 계층의 지정값. 매 프레임 모션을 다시 해석하지 않기 위한 기준이다.</summary>
        private UPlayGround.Gameplay.Tag.GameplayTag _requestedDialogueMotion;

        /// <summary>실제로 재생 중인 제스처 슬롯. 폴백 때문에 지정값과 다를 수 있다.</summary>
        private UPlayGround.Gameplay.Tag.GameplayTag _playingDialogueMotion;

        private bool _hasResolvedDialogueMotion;

        /// <summary>
        /// 대화 계층이 지정한 제스처를 재생하고 실제로 재생된 슬롯을 돌려준다.
        /// 지정이 그대로면 해석 자체를 건너뛰므로 매 프레임 호출해도 된다 — 대화 자세를 소유하는 상태
        /// (<see cref="PlayerDialogueState"/> · NPC 상호작용 중의 <see cref="PlayerInteractionState"/>)가
        /// 진입 시 한 번, 이후 매 프레임 호출해 라인이 넘어갈 때 제스처를 이어받는다.
        /// 지정 제스처가 없거나 이 캐릭터가 갖고 있지 않으면 기본 대화 → 대기 순으로 폴백한다.
        /// 모션을 재생하지 않으면 직전 상태(달리기 등)의 클립이 그대로 남아 제자리 달리기로 대화하게 된다.
        /// </summary>
        protected UPlayGround.Gameplay.Tag.GameplayTag PlayDialogueMotion(float fadeDuration)
        {
            var animator = gameActor?.Animator;
            if (animator == null)
                return default;

            UPlayGround.Gameplay.Tag.GameplayTag requested =
                playerActor != null ? playerActor.DialogueMotionTag : default;
            if (_hasResolvedDialogueMotion && requested == _requestedDialogueMotion)
                return _playingDialogueMotion;

            _requestedDialogueMotion = requested;
            _hasResolvedDialogueMotion = true;

            UPlayGround.Gameplay.Tag.GameplayTag resolved =
                UPlayGround.Animation.DialogueMotionPlayback.Resolve(animator, requested);
            if (resolved == _playingDialogueMotion)
                return resolved;

            _playingDialogueMotion = resolved;
            animator.PlayMotion(resolved, fadeDuration);
            return resolved;
        }

        /// <summary>
        /// 지정한 대상을 향해 수평으로 부드럽게 회전시킨다. 대상이 없으면 현재 회전을 유지한다.
        /// 대화·상호작용처럼 이동 입력이 아니라 특정 대상이 시선을 결정하는 상태가 공유한다.
        /// </summary>
        protected void SmoothLookAt(Transform target, ref Quaternion currentRotation, float deltaTime)
        {
            if (target == null)
            {
                currentRotation = currentRotation.normalized;
                return;
            }

            Vector3 lookDirection = target.position - gameActor.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f && controller.OrientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(
                    motor.CharacterForward,
                    lookDirection.normalized,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime)).normalized;

                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }

        /// <summary>
        /// 이동 입력 방향을 캐릭터 기준 4방향(F/B/L/R)으로 분류해 해당 모션 키를 반환.
        /// 매칭되는 모션이 없거나 입력이 없으면 fallbackKey 반환.
        /// </summary>
        protected UPlayGround.Gameplay.Tag.GameplayTag ResolveDirectionalMotionKey(
            UPlayGround.Gameplay.Tag.GameplayTag forwardKey, UPlayGround.Gameplay.Tag.GameplayTag backKey, UPlayGround.Gameplay.Tag.GameplayTag leftKey, UPlayGround.Gameplay.Tag.GameplayTag rightKey, UPlayGround.Gameplay.Tag.GameplayTag fallbackKey)
        {
            var animator = gameActor?.Animator;
            if (animator == null)
                return fallbackKey;

            if (playerController == null || playerController.HasMoveInput() == false)
                return animator.HasMotion(forwardKey, true) ? forwardKey : fallbackKey;

            Vector3 input = playerController.MoveInputVector.normalized;
            float forwardDot = Vector3.Dot(input, motor.CharacterForward);
            float rightDot   = Vector3.Dot(input, motor.CharacterRight);

            UPlayGround.Gameplay.Tag.GameplayTag candidate = Mathf.Abs(forwardDot) >= Mathf.Abs(rightDot)
                ? (forwardDot >= 0f ? forwardKey : backKey)
                : (rightDot   >= 0f ? rightKey   : leftKey);

            return animator.HasMotion(candidate, true) ? candidate : fallbackKey;
        }
    }
}
