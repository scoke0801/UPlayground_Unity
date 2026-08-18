using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Diagnostics;

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

        /// <summary>그래프에 등장하는 비플레이어 참여자에게 대화 홀드를 건다.</summary>
        private void BeginDialogueStage(DialogueGraphSO graph, Transform playerTransform)
        {
            // 대역 스폰은 이미 세션 시작 단계에서 끝났으므로 여기서는 홀드만 초기화한다.
            ReleaseStagedHolds();
            if (graph == null)
                return;

            if (!string.IsNullOrEmpty(_dialoguePartnerOverrideActorId))
                TryStageActor(FindActorInstance(_dialoguePartnerOverrideActorId), playerTransform);

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
        }

        /// <summary>
        /// 이번 라인의 화자·청자에 맞춰 홀드 중인 액터의 시선을 갱신한다.
        /// 화자와 청자는 서로를 보고, 대화에 참여하지만 이번 라인에 없는 인물은 플레이어를 본다.
        /// </summary>
        private void UpdateDialogueStageFocus(
            Transform speaker,
            Transform listener,
            Transform playerTransform)
        {
            for (int i = 0; i < _stagedActors.Count; i++)
            {
                StagedDialogueActor staged = _stagedActors[i];
                if (staged.Actor == null)
                    continue;

                Transform self = staged.Actor.transform;
                Transform lookTarget;
                if (self == speaker)
                    lookTarget = listener;
                else if (self == listener)
                    lookTarget = speaker;
                else
                    lookTarget = playerTransform;

                if (lookTarget != null && lookTarget != self)
                    staged.Stage.SetDialogueStageLookTarget(lookTarget);
            }
        }

        /// <summary>홀드를 풀고 임시 대역을 내보낸다. immediate면 디졸브 없이 즉시 파괴한다.</summary>
        private void EndDialogueStage(bool immediate = false)
        {
            // 홀드를 먼저 풀어야 대역이 디졸브 중 시선 고정 상태로 남지 않는다.
            ReleaseStagedHolds();
            DespawnStandIns(immediate);
        }

        private void ReleaseStagedHolds()
        {
            for (int i = 0; i < _stagedActors.Count; i++)
                _stagedActors[i].Lease?.Dispose();

            _stagedActors.Clear();
            _warnedOffscreenSpeakerIds.Clear();
        }

        private void TryStageActor(GameActor actor, Transform playerTransform)
        {
            if (actor is not IDialogueStageActor stage || actor is PlayerActor)
                return;

            for (int i = 0; i < _stagedActors.Count; i++)
            {
                if (ReferenceEquals(_stagedActors[i].Actor, actor))
                    return;
            }

            IDisposable lease = stage.BeginDialogueStage(playerTransform);
            if (lease != null)
                _stagedActors.Add(new StagedDialogueActor(actor, stage, lease));
        }

        /// <summary>홀드 대상 액터 해석. 플레이어 화자는 홀드 대상이 아니다.</summary>
        private GameActor ResolveStageActor(string speakerId)
        {
            if (string.IsNullOrEmpty(speakerId)
                || DialogueSpeakerResolver.IsActivePlayerSpeaker(speakerId)
                || DialogueSpeakerResolver.IsProtagonistSpeaker(speakerId))
            {
                return null;
            }

            string actorId = ResolveActorId(speakerId);
            return string.IsNullOrEmpty(actorId) ? null : FindActorInstance(actorId);
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
