using System;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Dialogue;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Story;

namespace UPlayGround
{
    /// <summary>
    /// IInteractable을 구현한 NPC.
    /// 플레이어가 상호작용하면 DialogueManager에 대화를 시작시킵니다.
    /// StoryManager를 통한 트리거가 아니라 직접 대화하는 경우에 사용합니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(NpcMovementController))]
    public class NpcActor : GameActor, IInteractable, IDialogueStageActor
    {
        [SerializeField] private NpcActorSO _data;

        private bool _isInteracting;
        private IDisposable _simulationLease;

        private int _dialogueStageHolds;
        private IDisposable _dialogueStageSimulationLease;
        private Transform _dialogueStageLookTarget;

        /// <summary>대화 연출 홀드 중인지. 배회·Idle 상태가 Talk 상태로 넘어가는 조건이다.</summary>
        public bool IsDialogueStaged => _dialogueStageHolds > 0;

        /// <summary>Talk 상태가 바라볼 대상. 지정되지 않으면 플레이어를 본다.</summary>
        public Transform DialogueStageLookTarget => _dialogueStageLookTarget;

        // ── IInteractable ────────────────────────────────────────────

        public bool CanInteract()
            => !_isInteracting
               && !IsDialogueStaged
               && (_data?.dialogueGraph != null || FindEligibleStory() != null);

        public bool IsInteracting() => _isInteracting;

        public Transform GetInteractionTransform() => transform;

        public GameActor GetActor() => this;

        public InteractableActorSO GetData() => _data;

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;

            // 대화가 실제로 시작된 뒤에만 상호작용 상태로 들어간다.
            // 먼저 상태를 잡으면 스토리 트리거가 거절됐을 때 NPC가 잠긴 채로 남는다.
            StoryEntrySO story = FindEligibleStory();

            _isInteracting = true;
            _simulationLease = ActorSvc.Simulation?.AcquireActiveLease(this, this, "Dialogue");
            Svc.Dialogue.OnDialogueEnd += OnDialogueEnd;

            bool started = story != null && (Svc.StoryFlow?.TryTriggerStory(story) ?? false);
            if (!started && _data?.dialogueGraph != null)
            {
                Svc.Dialogue.StartDialogue(_data.dialogueGraph);
                started = true;
            }

            if (!started)
            {
                Svc.Dialogue.OnDialogueEnd -= OnDialogueEnd;
                _isInteracting = false;
                ReleaseSimulationLease();
            }
        }

        public void StopInteract()
        {
            if (!_isInteracting) return;

            // 강제 종료 시 이벤트 정리
            Svc.Dialogue.OnDialogueEnd -= OnDialogueEnd;
            _isInteracting = false;
            ReleaseSimulationLease();
        }

        // InteractionAnimEvent는 현재 NPC 대화에서 사용하지 않으므로 빈 구현
        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData { }

        // ── IDialogueStageActor ──────────────────────────────────────

        public IDisposable BeginDialogueStage(Transform lookTarget)
        {
            _dialogueStageLookTarget = lookTarget;
            _dialogueStageHolds++;

            // 대화 상대가 시뮬레이션 컬링으로 멈추면 회전·모션이 끊기므로 홀드 동안 활성 상태를 보장한다.
            _dialogueStageSimulationLease ??=
                ActorSvc.Simulation?.AcquireActiveLease(this, this, "DialogueStage");

            // 실제 Talk 전환은 Idle/Wander 상태가 IsDialogueStaged를 폴링해 처리한다 —
            // 상호작용 경로(_isInteracting)와 같은 흐름을 쓰기 위해 여기서 상태를 직접 밀지 않는다.
            return new ActorRuntimeLease(ReleaseDialogueStage);
        }

        public void SetDialogueStageLookTarget(Transform lookTarget)
        {
            if (IsDialogueStaged)
                _dialogueStageLookTarget = lookTarget;
        }

        private void ReleaseDialogueStage()
        {
            _dialogueStageHolds = Mathf.Max(0, _dialogueStageHolds - 1);
            if (IsDialogueStaged)
                return;

            _dialogueStageLookTarget = null;
            _dialogueStageSimulationLease?.Dispose();
            _dialogueStageSimulationLease = null;
        }

        // ── 내부 ────────────────────────────────────────────────────

        /// <summary>
        /// 담당 스토리 중 조건이 맞는 첫 항목. 없으면 null (기본 대화로 폴백).
        /// </summary>
        private StoryEntrySO FindEligibleStory()
        {
            StoryEntrySO[] entries = _data?.storyEntries;
            if (entries == null) return null;

            IStoryFlowService storyFlow = Svc.StoryFlow;
            if (storyFlow == null) return null;

            for (int i = 0; i < entries.Length; i++)
            {
                if (storyFlow.IsStoryEligible(entries[i]))
                    return entries[i];
            }

            return null;
        }

        private void OnDialogueEnd()
        {
            Svc.Dialogue.OnDialogueEnd -= OnDialogueEnd;
            _isInteracting = false;
            ReleaseSimulationLease();
        }

        private void ReleaseSimulationLease()
        {
            _simulationLease?.Dispose();
            _simulationLease = null;
        }

        protected override void OnDestroy()
        {
            ReleaseSimulationLease();
            _dialogueStageHolds = 0;
            _dialogueStageSimulationLease?.Dispose();
            _dialogueStageSimulationLease = null;
            base.OnDestroy();
        }

        /// <summary>
        /// ActorDefinitionSO에 연결된 NPC 데이터를 적용한다.
        /// 씬 배치 NPC와 ActorSpawnManager 스폰 NPC가 동일한 데이터 흐름을 사용한다.
        /// </summary>
        public override void SetDefinition(ActorDefinitionSO definition)
        {
            base.SetDefinition(definition);

            if (definition?.npcData != null)
                _data = definition.npcData;
        }

        /// <summary>
        /// 씬에 배치된 NPC를 지역 전용 대화 데이터로 재사용한다.
        /// 씬 파일을 직접 수정하지 못하는 런타임 지역 부트스트랩에서만 사용한다.
        /// </summary>
        public void SetNpcData(NpcActorSO data)
        {
            if (data != null)
                _data = data;
        }

        private void OnValidate()
        {
            // 일반 NPC 기본값. 전투형이라면 인스펙터에서 Combat 플래그를 추가하세요.
            _actorType = ActorType.NPC | ActorType.Talkable;
        }
    }
}
