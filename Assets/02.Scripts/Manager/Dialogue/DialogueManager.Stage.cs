using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Diagnostics;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// Main 대화 세션 동안 참여 액터의 자율 행동을 멈추고 시선을 상대에게 고정하는 연출 홀드.
    /// 대화 시작 경로(상호작용 / FlowGraph / 스토리 트리거)에 따라 연출이 달라지지 않도록
    /// 홀드 소유자를 대화 계층 한곳으로 모았다.
    /// </summary>
    public partial class DialogueManager
    {
        private readonly struct StagedDialogueActor
        {
            public StagedDialogueActor(GameActor actor, IDialogueStageActor stage, IDisposable lease)
            {
                Actor = actor;
                Stage = stage;
                Lease = lease;
            }

            public GameActor Actor { get; }
            public IDialogueStageActor Stage { get; }
            public IDisposable Lease { get; }
        }

        private readonly List<StagedDialogueActor> _stagedActors = new();
        private readonly HashSet<string> _warnedOffscreenSpeakerIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _warnedMissingSilentParticipantIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> _warnedUnknownMotionIds = new(StringComparer.Ordinal);
        private IDisposable _storyProtagonistPresentationLease;

        private const string DialogueMotionCatalogResourcePath = "DialogueMotionCatalog";
        private DialogueMotionCatalogSO _motionCatalog;
        private bool _motionCatalogLoaded;

        /// <summary>그래프에 등장하는 참여자와 플레이어에게 대화 홀드를 건다.</summary>
        private void BeginDialogueStage(DialogueGraphSO graph, Transform playerTransform)
        {
            // 대역 스폰은 이미 세션 시작 단계에서 끝났으므로 여기서는 홀드만 초기화한다.
            ReleaseStagedHolds();
            if (graph == null)
                return;

            // 플레이어는 화자 스캔으로 잡히지 않지만(화자 해석이 플레이어를 제외한다) 항상 참여자다.
            // 상호작용 경로 밖에서 시작된 대화도 플레이어가 같은 대화 자세를 취하려면 여기서 홀드를 건다.
            TryStageActor(GameObjectManager.Instance?.Player, _dialoguePartner);

            TryStageActor(_dialoguePartnerOverrideActor, playerTransform);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DialogueNodeSO node = graph.nodes[i];
                if (node == null || node.channel != DialogueChannel.Main)
                    continue;
                if (node.nodeType != NodeType.Talk && node.nodeType != NodeType.Choice)
                    continue;

                TryStageActor(ResolveStageActor(node.speakerId), playerTransform);
                TryStageActor(ResolveStageActor(node.listenerSpeakerId), playerTransform);
            }

            StageSilentParticipants(graph, playerTransform);
        }

        /// <summary>
        /// 대사가 없어 노드 스캔으로는 잡히지 않는 동행 인물에게도 홀드를 건다.
        /// 이들이 없으면 대화 연출 뒤에서 동료가 계속 배회한다.
        /// </summary>
        private void StageSilentParticipants(DialogueGraphSO graph, Transform playerTransform)
        {
            List<string> silentSpeakerIds = graph.silentParticipantSpeakerIds;
            if (silentSpeakerIds == null)
                return;

            for (int i = 0; i < silentSpeakerIds.Count; i++)
            {
                string speakerId = silentSpeakerIds[i];
                if (string.IsNullOrWhiteSpace(speakerId))
                    continue;

                GameActor actor = ResolveStageActor(speakerId);
                if (actor == null)
                {
                    WarnMissingSilentParticipantOnce(speakerId);
                    continue;
                }

                TryStageActor(actor, playerTransform);
            }
        }

        /// <summary>
        /// 이번 라인의 화자·청자에 맞춰 홀드 중인 액터의 시선을 갱신한다.
        /// 화자와 청자는 서로를 보고, 대화에 참여하지만 이번 라인에 없는 인물은 플레이어를 본다.
        /// 주목 대상이 있는 라인에서는 화자와 청자가 함께 그쪽을 본다 — 카메라만 돌면 "가리키는" 연기가 비므로.
        /// </summary>
        private void UpdateDialogueStageFocus(
            Transform speaker,
            Transform listener,
            Transform playerTransform,
            Transform focusTarget = null)
        {
            for (int i = 0; i < _stagedActors.Count; i++)
            {
                StagedDialogueActor staged = _stagedActors[i];
                if (staged.Actor == null)
                    continue;

                Transform self = staged.Actor.transform;
                Transform lookTarget;
                if (focusTarget != null && (self == speaker || self == listener))
                    lookTarget = focusTarget;
                else if (self == speaker)
                    lookTarget = listener;
                else if (self == listener)
                    lookTarget = speaker;
                else
                    lookTarget = playerTransform;

                if (lookTarget != null && lookTarget != self)
                    staged.Stage.SetDialogueStageLookTarget(lookTarget);
            }
        }

        /// <summary>
        /// 대화 제스처 카탈로그. DialogueManager는 씬 배치 없이 런타임에 생성되므로
        /// 인스펙터 참조만으로는 항상 비어 있다 — GameplayTagRegistry와 같은 방식으로 Resources에서 읽는다.
        /// 씬에 매니저를 직접 배치해 다른 카탈로그를 쓰고 싶을 때만 인스펙터 참조가 우선한다.
        /// </summary>
        private DialogueMotionCatalogSO MotionCatalog
        {
            get
            {
                if (_motionCatalogOverride != null)
                    return _motionCatalogOverride;

                if (!_motionCatalogLoaded)
                {
                    _motionCatalogLoaded = true;
                    _motionCatalog = Resources.Load<DialogueMotionCatalogSO>(
                        DialogueMotionCatalogResourcePath);

                    if (_motionCatalog == null)
                    {
                        Debug.LogWarning(
                            $"[Dialogue] Resources/{DialogueMotionCatalogResourcePath}.asset을 찾지 못했습니다. "
                            + "대화 제스처 변형 없이 기본 대화 모션만 재생합니다.");
                    }
                }

                return _motionCatalog;
            }
        }

        /// <summary>
        /// 이번 라인의 화자·청자가 취할 제스처를 지정한다.
        /// 노드가 ID로 지정하면 그것을, 비워두면 노드의 정서 분류에서 랜덤으로 뽑는다.
        /// 화자와 청자를 같은 규칙으로 처리하므로 저작하지 않은 대화도 양쪽 모두 라인마다 움직인다.
        /// </summary>
        private void UpdateDialogueStageMotion(
            DialogueNodeSO node,
            Transform speaker,
            Transform listener)
        {
            if (MotionCatalog == null || node == null)
                return;

            ApplyDialogueMotion(speaker, node.speakerMotionId, node.speakerMotionCategory);
            ApplyDialogueMotion(listener, node.listenerMotionId, node.listenerMotionCategory);
        }

        private void ApplyDialogueMotion(
            Transform target,
            string motionId,
            DialogueMotionCategory category)
        {
            if (target == null)
                return;

            IDialogueMotionActor motionActor = FindStagedMotionActor(target);
            if (motionActor == null)
                return;

            DialogueMotionCatalogSO catalog = MotionCatalog;
            if (!catalog.TryGetMotionTag(motionId, out GameplayTag motionTag))
            {
                WarnUnknownDialogueMotionOnce(motionId);

                // 직전 제스처를 제외 후보로 넘겨 같은 동작이 연속으로 나오지 않게 한다.
                motionTag = catalog.PickRandom(category, motionActor.DialogueMotionTag);
            }

            motionActor.SetDialogueMotion(motionTag);
        }

        private IDialogueMotionActor FindStagedMotionActor(Transform target)
        {
            for (int i = 0; i < _stagedActors.Count; i++)
            {
                GameActor actor = _stagedActors[i].Actor;
                if (actor != null && actor.transform == target)
                    return actor as IDialogueMotionActor;
            }

            return null;
        }

        /// <summary>카탈로그에 없는 제스처 ID는 라인마다 반복 경고하지 않고 한 번만 알린다.</summary>
        private void WarnUnknownDialogueMotionOnce(string motionId)
        {
            if (string.IsNullOrWhiteSpace(motionId)
                || !_warnedUnknownMotionIds.Add(motionId))
                return;

            Debug.LogWarning(
                $"[Dialogue] 대화 제스처 카탈로그에 없는 ID '{motionId}'입니다. 랜덤 제스처로 대체합니다.");
        }

        /// <summary>홀드를 풀고 임시 대역을 내보낸다. immediate면 디졸브 없이 즉시 파괴한다.</summary>
        private void EndDialogueStage(bool immediate = false)
        {
            // 홀드를 먼저 풀어야 대역이 디졸브 중 시선 고정 상태로 남지 않는다.
            ReleaseStagedHolds();
            EndStoryProtagonistPresentation();
            DespawnStandIns(immediate);
        }

        /// <summary>선택한 서사 주인공을 Main 대화의 플레이어 외형으로 표시한다.</summary>
        private void BeginStoryProtagonistPresentation()
        {
            EndStoryProtagonistPresentation();
            _storyProtagonistPresentationLease =
                PartyManager.Instance?.BeginStoryProtagonistPresentation();
        }

        private void EndStoryProtagonistPresentation()
        {
            IDisposable lease = _storyProtagonistPresentationLease;
            _storyProtagonistPresentationLease = null;
            lease?.Dispose();
        }

        private void ReleaseStagedHolds()
        {
            for (int i = 0; i < _stagedActors.Count; i++)
                _stagedActors[i].Lease?.Dispose();

            _stagedActors.Clear();
            _warnedOffscreenSpeakerIds.Clear();
            _warnedMissingSilentParticipantIds.Clear();
        }

        private void TryStageActor(GameActor actor, Transform lookTarget)
        {
            if (actor is not IDialogueStageActor stage)
                return;

            for (int i = 0; i < _stagedActors.Count; i++)
            {
                if (ReferenceEquals(_stagedActors[i].Actor, actor))
                    return;
            }

            IDisposable lease = stage.BeginDialogueStage(lookTarget);
            if (lease != null)
                _stagedActors.Add(new StagedDialogueActor(actor, stage, lease));
        }

        /// <summary>홀드 대상 액터 해석. 플레이어 화자는 여기서 해석하지 않는다 — 플레이어 홀드는 세션 진입 시 한 번만 건다.</summary>
        private GameActor ResolveStageActor(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId)
                || DialogueSpeakerResolver.IsActivePlayerSpeaker(speakerId)
                || DialogueSpeakerResolver.IsProtagonistSpeaker(speakerId))
            {
                return null;
            }

            // 지정 상대는 인스턴스를 그대로 쓴다 — ID로 다시 찾으면 같은 actorId의 다른 개체를 홀드한다.
            if (_dialoguePartnerOverrideActor != null
                && string.Equals(
                    speakerId,
                    _dialoguePartnerOverrideSpeakerId,
                    StringComparison.Ordinal))
            {
                return _dialoguePartnerOverrideActor;
            }

            string actorId = ResolveActorId(speakerId);
            return string.IsNullOrEmpty(actorId) ? null : FindActorInstance(actorId);
        }

        /// <summary>
        /// 저작된 무언 참여자가 월드에 없음을 세션당 한 번만 알린다.
        /// 화자와 달리 대역을 세우지 않으므로, 조용히 넘기면 저작 오타가 드러나지 않는다.
        /// </summary>
        private void WarnMissingSilentParticipantOnce(string speakerId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_warnedMissingSilentParticipantIds.Add(speakerId))
                return;

            Debug.LogWarning(
                $"[Dialogue] 무언 참여자 '{speakerId}'의 월드 액터를 찾지 못해 홀드를 걸지 못했습니다."
                + " 화자 ID 표기 또는 해당 인물의 배치를 확인하세요.");
#endif
        }

        /// <summary>
        /// 월드에 없는 화자를 플레이어 대역으로 처리했음을 세션당 한 번만 남긴다.
        /// 해설자처럼 몸이 없는 화자에도 해당하는 정상 경로이므로 경고가 아니라 진단 로그로 남긴다.
        /// </summary>
        private void WarnOffscreenSpeakerOnce(string speakerId)
        {
            string key = speakerId ?? string.Empty;
            if (!_warnedOffscreenSpeakerIds.Add(key))
                return;

            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[Dialogue] 화자 '{key}'의 월드 액터가 없어 활성 플레이어를 카메라 대역으로 사용합니다.");
        }
    }
}
