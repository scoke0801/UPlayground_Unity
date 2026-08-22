using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// NPC 대화 중 상태.
    /// - 플레이어 방향으로 부드럽게 회전
    /// - 대화 종료 시 자동으로 IdleState 복귀
    /// </summary>
    public class NpcTalkState : NpcActorState
    {
        public override ActorStateId StateId => ActorStateId.Talk;

        /// <summary>대화 진입 시 직전 모션에서 넘어오는 페이드 시간.</summary>
        private const float EnterFadeDuration = 0.25f;

        /// <summary>제스처가 라인마다 교체될 때의 페이드 시간. 진입보다 짧게 잡아 대사 호흡을 놓치지 않는다.</summary>
        private const float GestureSwapFadeDuration = 0.18f;

        /// <summary>마지막으로 확인한 대화 계층의 지정값. 매 프레임 모션을 다시 해석하지 않기 위한 기준이다.</summary>
        private UPlayGround.Gameplay.Tag.GameplayTag _requestedMotionTag;

        /// <summary>실제로 재생 중인 제스처 슬롯. 폴백 때문에 지정값과 다를 수 있다.</summary>
        private UPlayGround.Gameplay.Tag.GameplayTag _playingMotionTag;

        private bool _hasResolvedMotion;

        public NpcTalkState(NpcMovementController controller) : base(controller) { }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            PlayDialogueMotion(EnterFadeDuration);
        }

        public override void UpdateState(float deltaTime)
        {
            // 상호작용 대화와 연출 홀드(FlowGraph·스토리 대화)가 모두 풀리면 Idle로 복귀
            if (!npcActor.IsInteracting() && !npcActor.IsDialogueStaged)
            {
                npcController.TransitionToState(new NpcIdleState(npcController));
                return;
            }

            PlayDialogueMotion(GestureSwapFadeDuration);
        }

        /// <summary>
        /// 대화 계층이 지정한 제스처를 재생한다. 지정이 그대로면 해석 자체를 건너뛴다.
        /// 지정을 이벤트로 밀지 않고 여기서 확인하는 이유는, 홀드가 Talk 상태 진입보다 먼저 걸릴 수 있어
        /// 밀어넣기 방식이면 진입 직전에 지정된 제스처를 놓치기 때문이다.
        /// </summary>
        private void PlayDialogueMotion(float fadeDuration)
        {
            var animator = gameActor?.Animator;
            if (animator == null)
                return;

            UPlayGround.Gameplay.Tag.GameplayTag requested = npcActor.DialogueMotionTag;
            if (_hasResolvedMotion && requested == _requestedMotionTag)
                return;

            _requestedMotionTag = requested;
            _hasResolvedMotion = true;

            UPlayGround.Gameplay.Tag.GameplayTag resolved =
                UPlayGround.Animation.DialogueMotionPlayback.Resolve(animator, requested);
            if (resolved == _playingMotionTag)
                return;

            _playingMotionTag = resolved;
            animator.PlayMotion(resolved, fadeDuration);
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 대화 상대를 향해 부드럽게 회전. 3인 이상 대화에서는 홀드가 지정한 상대가 플레이어가 아니다.
            Transform lookTarget = ResolveLookTarget();
            if (lookTarget == null) return;

            Vector3 lookDir = lookTarget.position - npcActor.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.001f) return;

            Vector3 smoothed = Vector3.Slerp(
                motor.CharacterForward,
                lookDir.normalized,
                1 - Mathf.Exp(-npcController.OrientationSharpness * deltaTime));

            currentRotation = Quaternion.LookRotation(smoothed, motor.CharacterUp);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity = Vector3.zero;
        }

        /// <summary>홀드가 지정한 상대를 우선하고, 없으면 플레이어를 본다.</summary>
        private Transform ResolveLookTarget()
        {
            Transform staged = npcActor.DialogueStageLookTarget;
            if (staged != null)
                return staged;

            var player = UPlayGround.Manager.ActorSvc.Objects.Player;
            return player != null ? player.transform : null;
        }
    }
}
