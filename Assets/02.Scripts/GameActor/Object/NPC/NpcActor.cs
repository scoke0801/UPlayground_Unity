using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Dialogue;
using UPlayGround.MovementController;

namespace UPlayGround
{
    /// <summary>
    /// IInteractable을 구현한 NPC.
    /// 플레이어가 상호작용하면 DialogueManager에 대화를 시작시킵니다.
    /// StoryManager를 통한 트리거가 아니라 직접 대화하는 경우에 사용합니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(NpcMovementController))]
    public class NpcActor : GameActor, IInteractable
    {
        [SerializeField] private NpcActorSO _data;

        private bool _isInteracting;

        // ── IInteractable ────────────────────────────────────────────

        public bool CanInteract() => !_isInteracting && _data?.dialogueGraph != null;

        public bool IsInteracting() => _isInteracting;

        public GameActor GetActor() => this;

        public InteractableActorSO GetData() => _data;

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;

            _isInteracting = true;

            DialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
            DialogueManager.Instance.StartDialogue(_data.dialogueGraph);
        }

        public void StopInteract()
        {
            if (!_isInteracting) return;

            // 강제 종료 시 이벤트 정리
            DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
            _isInteracting = false;
        }

        // InteractionAnimEvent는 현재 NPC 대화에서 사용하지 않으므로 빈 구현
        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData { }

        // ── 내부 ────────────────────────────────────────────────────

        private void OnDialogueEnd()
        {
            DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
            _isInteracting = false;
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

        private void OnValidate()
        {
            // 일반 NPC 기본값. 전투형이라면 인스펙터에서 Combat 플래그를 추가하세요.
            _actorType = ActorType.NPC | ActorType.Talkable;
        }
    }
}
