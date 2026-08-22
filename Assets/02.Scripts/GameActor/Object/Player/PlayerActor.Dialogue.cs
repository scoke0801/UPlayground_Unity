using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.State;

namespace UPlayGround
{
    /// <summary>
    /// 플레이어의 대화 연출 홀드.
    /// 상호작용 / FlowGraph / 스토리 어느 경로로 대화가 시작돼도 플레이어가 같은 자세
    /// (이동 정지 · 무기 수납 · 대화 모션 · 상대 주시)를 취하도록 대화 계층이 거는 홀드를 받는다.
    /// </summary>
    public partial class PlayerActor : IDialogueStageActor, IDialogueMotionActor
    {
        private int _dialogueStageHolds;
        private Transform _dialogueStageLookTarget;
        private UPlayGround.Gameplay.Tag.GameplayTag _dialogueMotionTag;

        /// <summary>
        /// 이 홀드가 대화 자세(상태 전환·무기 수납)를 직접 세웠는지.
        /// 상호작용 상태가 이미 같은 자세를 소유 중일 때 무기 복원을 두 주체가 하지 않도록 구분한다.
        /// </summary>
        private bool _dialogueStageOwnsPresentation;

        /// <summary>대화 연출 홀드 중인지.</summary>
        public bool IsDialogueStaged => _dialogueStageHolds > 0;

        /// <summary>대화 중 바라볼 대상. 홀드가 없으면 null.</summary>
        public Transform DialogueStageLookTarget => _dialogueStageLookTarget;

        public IDisposable BeginDialogueStage(Transform lookTarget)
        {
            _dialogueStageHolds++;
            SetDialogueStageLookTarget(lookTarget);

            if (_dialogueStageHolds == 1)
                BeginDialoguePresentation();

            return new ActorRuntimeLease(ReleaseDialogueStage);
        }

        public void SetDialogueStageLookTarget(Transform lookTarget)
        {
            if (IsDialogueStaged && lookTarget != null && lookTarget != transform)
                _dialogueStageLookTarget = lookTarget;
        }

        /// <summary>이번 라인의 대화 제스처. 무효면 상태가 기본 대화 모션으로 폴백한다.</summary>
        public UPlayGround.Gameplay.Tag.GameplayTag DialogueMotionTag => _dialogueMotionTag;

        public void SetDialogueMotion(UPlayGround.Gameplay.Tag.GameplayTag motionTag)
        {
            if (IsDialogueStaged)
                _dialogueMotionTag = motionTag;
        }

        /// <summary>
        /// 대화 자세로 전환한다.
        /// 상호작용으로 시작된 대화는 이미 <see cref="PlayerInteractionState"/>가 같은 자세를 유지하며
        /// 상호작용 리스도 그 상태가 소유하므로, 자세를 뺏지 않고 시선 갱신만 맡긴다.
        /// </summary>
        private void BeginDialoguePresentation()
        {
            if (MovementController == null)
                return;

            if (MovementController.CurrentState?.StateId == ActorStateId.Interaction)
                return;

            if (!MovementController.TransitionToState(new PlayerDialogueState(MovementController)))
                return;

            // 대화 모션은 맨손 기준이므로 인터렉션과 같은 경로로 무기를 수납한다.
            GetPlayerEquipment()?.BeginInteractionEquipment(InteractionObjectType.NPC);
            _dialogueStageOwnsPresentation = true;
        }

        private void ReleaseDialogueStage()
        {
            _dialogueStageHolds = Mathf.Max(0, _dialogueStageHolds - 1);
            if (IsDialogueStaged)
                return;

            _dialogueStageLookTarget = null;
            _dialogueMotionTag = default;

            if (!_dialogueStageOwnsPresentation)
                return;

            _dialogueStageOwnsPresentation = false;
            GetPlayerEquipment()?.EndInteractionEquipment();
            // 상태 복귀는 PlayerDialogueState가 홀드 해제를 감지해 직접 처리한다.
        }
    }
}
