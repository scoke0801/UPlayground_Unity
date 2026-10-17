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
    public partial class PlayerActor : IDialogueStageActor, IDialogueMotionActor, IDialogueFaceActor
    {
        private UPlayGround.Components.DialogueFaceBinding _dialogueFace;
        private Transform _dialogueFaceTarget;
        private GameActor _dialogueFaceTargetActor;
        private bool _suppressFaceLook;
        private bool _suppressFaceBlink;

        /// <summary>애니메이션 평가 전 Idle·대화 얼굴 보정을 복원한다.</summary>
        public void RestoreDialogueFace() => _dialogueFace?.Restore();

        private void LateUpdate()
        {
            // 대화 중에는 DialogueManager가 일시정지와 명시 제스처를 포함해 갱신한다.
            if (IsDialogueStaged) return;

            if (MovementController?.CurrentState?.StateId != ActorStateId.Idle)
            {
                _dialogueFace?.Clear();
                return;
            }

            _dialogueFace?.Apply(null, paused: false, suppressLook: true,
                suppressBlink: false, deltaTime: DeltaTime);
        }

        /// <summary>명시 제스처가 자동 얼굴 연출을 억제하도록 라인 의도를 전달한다.</summary>
        public void SetDialogueFaceIntent(bool suppressLook, bool suppressBlink)
        {
            _suppressFaceLook = suppressLook;
            _suppressFaceBlink = suppressBlink;
        }

        /// <summary>대화와 상호작용 상태에만 애니메이션 후 얼굴 보정을 허용한다.</summary>
        public void ApplyDialogueFace(bool paused)
        {
            var state = MovementController?.CurrentState?.StateId;
            if (!isActiveAndEnabled || !IsDialogueStaged
                || (state != ActorStateId.Dialogue && state != ActorStateId.Interaction))
            { _dialogueFace?.Clear(); return; }
            if (_dialogueFaceTargetActor != null)
            {
                _dialogueFaceTarget = null;
                if (_dialogueFaceTargetActor.isActiveAndEnabled)
                {
                    if (_dialogueFaceTargetActor is PlayerActor targetPlayer && targetPlayer._dialogueFace?.eyeCenter != null)
                        _dialogueFaceTarget = targetPlayer._dialogueFace.eyeCenter;
                    else if (_dialogueFaceTargetActor.TryGetSocket(ActorSocketType.Head, out Transform targetHead))
                        _dialogueFaceTarget = targetHead;
                }
            }
            _dialogueFace?.Apply(_dialogueFaceTarget, paused, _suppressFaceLook, _suppressFaceBlink, Time.unscaledDeltaTime);
        }

        private int _dialogueStageHolds;
        private Transform _dialogueStageLookTarget;
        private UPlayGround.Gameplay.Tag.GameplayTag _dialogueMotionTag;

        /// <summary>
        /// 이 홀드가 대화 자세(상태 전환·무기 수납)를 직접 세웠는지.
        /// 상호작용 상태가 이미 같은 자세를 소유 중일 때 무기 복원을 두 주체가 하지 않도록 구분한다.
        /// </summary>
        private bool _dialogueStageOwnsPresentation;

        public Transform DialogueStepTarget { get; private set; }
        public float DialogueStepSpeedMultiplier { get; private set; }
        public float DialogueStepStopDistance { get; private set; }
        public bool IsDialogueStepFacingMovement { get; private set; }

        /// <summary>대화 상태의 KCC 이동으로 짧은 행동을 시작한다. 상호작용 상태는 빼앗지 않는다.</summary>
        public bool TryBeginDialogueStep(Transform target, float speedMultiplier, float stopDistance, bool faceMovement = true)
        {
            if (!IsDialogueStaged || target == null
                || MovementController?.CurrentState?.StateId != ActorStateId.Dialogue)
                return false;

            DialogueStepTarget = target;
            IsDialogueStepFacingMovement = faceMovement;
            DialogueStepSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            DialogueStepStopDistance = Mathf.Max(0.1f, stopDistance);
            return true;
        }

        /// <summary>중단과 정상 종료 모두에서 연출 이동을 해제한다.</summary>
        public void StopDialogueStep() => DialogueStepTarget = null;

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
            _dialogueStageLookTarget = IsDialogueStaged && lookTarget != transform ? lookTarget : null;
            _dialogueFaceTarget = null;
            _dialogueFaceTargetActor = null;
            if (_dialogueStageLookTarget == null) return;
            var actor = lookTarget.GetComponent<GameActor>();
            _dialogueFaceTargetActor = actor;
            if (actor != null && actor.TryGetSocket(ActorSocketType.Head, out Transform head))
                _dialogueFaceTarget = head;
            // 발 위치는 시선 폴백으로 쓰지 않는다. 얼굴 소켓이 없으면 자동 시선을 생략한다.
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
            _dialogueFaceTarget = null;
            _dialogueFaceTargetActor = null;
            _dialogueFace?.Clear();
            _suppressFaceLook = false;
            _suppressFaceBlink = false;
            _dialogueMotionTag = default;
            StopDialogueStep();

            if (!_dialogueStageOwnsPresentation)
                return;

            _dialogueStageOwnsPresentation = false;
            GetPlayerEquipment()?.EndInteractionEquipment();
            // 상태 복귀는 PlayerDialogueState가 홀드 해제를 감지해 직접 처리한다.
        }
    }
}
