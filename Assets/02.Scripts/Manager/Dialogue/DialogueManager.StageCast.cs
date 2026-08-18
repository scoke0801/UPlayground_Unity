using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 월드에 없는 화자를 대화 동안만 세우는 임시 출연진(대역) 관리.
    /// 파티에 합류해 월드에서 사라진 인물이나 스트리밍으로 빠진 인물이 말할 때,
    /// 카메라가 빈 자리를 잡지 않도록 실제 액터를 등장시키고 대화가 끝나면 디졸브로 정리한다.
    /// </summary>
    public partial class DialogueManager
    {
        private readonly struct DialogueStandIn
        {
            public DialogueStandIn(GameActor actor, IDisposable combatExclusion)
            {
                Actor = actor;
                CombatExclusion = combatExclusion;
            }

            public GameActor Actor { get; }
            public IDisposable CombatExclusion { get; }
        }

        private readonly List<DialogueStandIn> _standIns = new();
        private readonly List<string> _standInSpeakerIds = new();
        private readonly HashSet<string> _warnedStandInSpeakerIds = new(StringComparer.Ordinal);
        private DialogueStageSettingsSO _fallbackStageSettings;

        /// <summary>인스펙터에 에셋이 없으면 코드 기본값 인스턴스를 쓴다 — 연출은 데이터 없이도 동작해야 한다.</summary>
        private DialogueStageSettingsSO EffectiveStageSettings
        {
            get
            {
                if (_stageSettings != null)
                    return _stageSettings;

                return _fallbackStageSettings ??=
                    ScriptableObject.CreateInstance<DialogueStageSettingsSO>();
            }
        }

        /// <summary>
        /// 그래프의 화자 중 월드에 없는 인물을 플레이어 앞에 세운다.
        /// 대화 상대 해석보다 먼저 호출해야 스폰된 대역이 그대로 상대로 잡힌다.
        /// </summary>
        private void SpawnMissingSpeakers(DialogueGraphSO graph, Transform playerTransform)
        {
            DialogueStageSettingsSO settings = EffectiveStageSettings;
            if (!settings.SpawnMissingSpeakers || graph == null || playerTransform == null)
                return;

            var spawner = ActorSpawnManager.Instance;
            if (spawner == null || !spawner.IsDBLoaded || spawner.Database == null)
                return;

            CollectMissingSpeakerIds(graph, settings.MaxStandInCount);
            for (int i = 0; i < _standInSpeakerIds.Count; i++)
            {
                // 자리 번호는 실제로 세운 인원 수로 센다. 스폰 실패 인물의 자리를 비워두면
                // 정면 자리가 빈 채로 좌우에만 인물이 서는 구도가 된다.
                TrySpawnStandIn(_standInSpeakerIds[i], _standIns.Count, playerTransform, settings);
            }
        }

        /// <summary>대화가 끝난 대역을 디졸브로 내보낸다.</summary>
        private void DespawnStandIns(bool immediate)
        {
            float dissolveDuration = EffectiveStageSettings.DissolveDuration;
            for (int i = 0; i < _standIns.Count; i++)
            {
                DialogueStandIn standIn = _standIns[i];
                standIn.CombatExclusion?.Dispose();
                if (standIn.Actor == null)
                    continue;

                if (immediate)
                    Destroy(standIn.Actor.gameObject);
                else
                    standIn.Actor.PlayDissolveAndDestroy(dissolveDuration);
            }

            _standIns.Clear();
            _standInSpeakerIds.Clear();
            _warnedStandInSpeakerIds.Clear();
        }

        /// <summary>
        /// 그래프 등장 순서대로, 지금 월드에서 찾을 수 없는 비플레이어 화자 ID를 모은다.
        /// 등장 순서를 유지해야 첫 화자가 플레이어 정면 자리를 차지한다.
        /// </summary>
        private void CollectMissingSpeakerIds(DialogueGraphSO graph, int maxCount)
        {
            _standInSpeakerIds.Clear();
            for (int i = 0; i < graph.nodes.Count && _standInSpeakerIds.Count < maxCount; i++)
            {
                DialogueNodeSO node = graph.nodes[i];
                if (node == null || node.channel != DialogueChannel.Main)
                    continue;
                if (node.nodeType != NodeType.Talk && node.nodeType != NodeType.Choice)
                    continue;

                AddMissingSpeakerId(node.speakerId, maxCount);
                AddMissingSpeakerId(node.listenerSpeakerId, maxCount);
            }
        }

        private void AddMissingSpeakerId(string speakerId, int maxCount)
        {
            if (_standInSpeakerIds.Count >= maxCount
                || string.IsNullOrEmpty(speakerId)
                || DialogueSpeakerResolver.IsActivePlayerSpeaker(speakerId)
                || DialogueSpeakerResolver.IsProtagonistSpeaker(speakerId)
                || _standInSpeakerIds.Contains(speakerId))
            {
                return;
            }

            if (ResolveSpeakerTransform(speakerId) == null)
                _standInSpeakerIds.Add(speakerId);
        }

        private void TrySpawnStandIn(
            string speakerId,
            int slotIndex,
            Transform playerTransform,
            DialogueStageSettingsSO settings)
        {
            if (!TryResolveStandInActorId(speakerId, out string actorId))
                return;
            if (!TryResolveStandInPose(
                    slotIndex,
                    playerTransform,
                    settings,
                    out Vector3 position,
                    out Quaternion rotation))
            {
                return;
            }

            GameActor actor = ActorSpawnManager.Instance.SpawnActor(actorId, position, rotation);
            if (actor == null)
                return;

            // 등장 순서: 전투에서 빼고 → 전투 컴포넌트를 끄고 → 보이게 한다.
            // 배제를 먼저 걸어야 첫 프레임에 락온 후보나 적 어그로 대상으로 잡히지 않는다.
            IDisposable combatExclusion = actor.ExcludeFromCombat();
            PacifyStandIn(actor);
            actor.PlayReveal(settings.RevealDuration);
            _standIns.Add(new DialogueStandIn(actor, combatExclusion));
        }

        /// <summary>
        /// 스폰 가능한 대역 ActorId 해석. 정의가 없으면 세우지 않고 카메라 대역으로 폴백한다.
        /// 바인딩 테이블에 등록된 화자만 "몸이 있어야 하는 인물"로 보고 경고한다 —
        /// 해설자처럼 액터가 없는 화자까지 경고하면 정상 대화에서 로그가 넘친다.
        /// </summary>
        private bool TryResolveStandInActorId(string speakerId, out string actorId)
        {
            actorId = null;
            bool isBoundSpeaker = SpeakerActorBindings != null
                                  && SpeakerActorBindings.TryGetStandInActorId(speakerId, out actorId);
            if (!isBoundSpeaker)
                actorId = ResolveActorId(speakerId);

            if (!string.IsNullOrEmpty(actorId)
                && ActorSpawnManager.Instance.Database.TryGetDefinition(actorId, out _))
            {
                return true;
            }

            if (isBoundSpeaker)
                WarnMissingStandInDefinition(speakerId, actorId);
            return false;
        }

        /// <summary>
        /// 플레이어 정면을 중심으로 0, +1, -1 … 순서로 좌우 교대 배치한다.
        /// 정면 한 자리만 쓰면 여러 명이 겹치고, 한쪽으로만 늘리면 구도가 기울어진다.
        /// </summary>
        private static bool TryResolveStandInPose(
            int slotIndex,
            Transform playerTransform,
            DialogueStageSettingsSO settings,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 forward = playerTransform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            int step = (slotIndex + 1) / 2;
            float side = slotIndex % 2 == 0 ? step : -step;
            Vector3 playerPosition = playerTransform.position;
            Vector3 candidate = playerPosition
                                + forward * settings.SpawnDistance
                                + right * (side * settings.LateralSpacing);

            rotation = Quaternion.LookRotation(-forward, Vector3.up);
            return ActorStagePlacement.TryProbeGround(
                candidate,
                playerPosition.y,
                settings.MaxHeightDelta,
                out position);
        }

        /// <summary>
        /// 대역을 "보이지만 싸우지 않는" 상태로 만든다.
        /// 몬스터 정의를 그대로 스폰하므로 감지·전투·AI를 끄고 무적으로 두지 않으면
        /// 대화 중에 플레이어를 공격하거나 처치되어 영입 경로가 잘못 발화한다.
        /// </summary>
        private static void PacifyStandIn(GameActor actor)
        {
            if (actor is not MonsterActor monster)
                return;

            monster.SuppressRuntimePartyRecruitment();
            monster.SetCombatComponentsEnabled(false);
            monster.SetInvincible(true);
            monster.Detection?.ForceResetTarget();
            monster.Abilities?.CancelAllAbilities();
        }

        private void WarnMissingStandInDefinition(string speakerId, string actorId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string key = speakerId ?? string.Empty;
            if (!_warnedStandInSpeakerIds.Add(key))
                return;

            Debug.LogWarning(
                $"[Dialogue] 화자 '{key}'의 대역 정의를 찾지 못해 세우지 못했습니다"
                + $" (ActorId: {actorId ?? "<없음>"}). ActorDatabase 등록 또는"
                + " Speaker Binding Table의 standInActorId를 확인하세요.");
#endif
        }
    }
}
