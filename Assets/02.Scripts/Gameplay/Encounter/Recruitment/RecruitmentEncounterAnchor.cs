using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Diagnostics;
using UPlayGround.FlowGraph;
using UPlayGround.Group;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Encounter
{
    /// <summary>씬의 영입 대상, 적 참가자, FlowGraph를 저장 가능한 조우 서비스에 연결한다.</summary>
    public sealed class RecruitmentEncounterAnchor : MonoBehaviour, IRecruitmentEncounterRuntimePort, IManagedTick
    {
        [SerializeField] private RecruitmentEncounterDefinitionSO _definition;
        [SerializeField] private FlowGraphRunner _flowRunner;
        [SerializeField] private FlowGraphTriggerVolume _entryVolume;
        [SerializeField] private string _resumeEntryId = "Resume";
        [SerializeField] private MonsterActor _allyActor;
        [SerializeField] private MonsterGroupController _hostileGroup;
        [SerializeField] private RecruitmentEncounterParticipant[] _participants;
        [SerializeField] private Transform _dialogueAnchor;

        [Tooltip("진입 볼륨 안에 플레이어가 있는지 다시 확인하는 주기(초).")]
        [SerializeField] private float _entryPollInterval = 0.25f;

        private readonly List<string> _hostileParticipantIds = new();
        private IDisposable _runtimeLease;
        private Coroutine _runtimeRegistrationRoutine;
        private bool _participantsBound;
        private bool _isEntryPollActive;
        private float _entryPollTimer;
        private FlowVolumeRouteFailure _lastEntryFailure = FlowVolumeRouteFailure.None;

        public string EncounterId => _definition != null ? _definition.EncounterId : null;
        public RecruitmentEncounterDefinitionSO Definition => _definition;
        public string DialoguePartnerActorId => _allyActor != null ? _allyActor.ActorId : null;
        public IReadOnlyList<string> HostileParticipantIds => _hostileParticipantIds;

        private void Awake()
        {
            RebuildHostileParticipantIds();
            if (Application.isPlaying)
            {
                _entryVolume?.SetRoutingEnabled(false);
                SetAllParticipantsHidden();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying && _runtimeRegistrationRoutine == null)
                _runtimeRegistrationRoutine = StartCoroutine(RegisterRuntimeWhenAvailable());
        }

        private void OnDisable()
        {
            if (_runtimeRegistrationRoutine != null)
                StopCoroutine(_runtimeRegistrationRoutine);
            _runtimeRegistrationRoutine = null;
            EndEntryPolling();
            _entryVolume?.SetRoutingEnabled(false);
            SetAllParticipantsHidden();
            _runtimeLease?.Dispose();
            _runtimeLease = null;
            UnbindParticipants();
        }

        private void OnValidate()
        {
            _resumeEntryId = _resumeEntryId?.Trim();
            _entryVolume ??= GetComponentInChildren<FlowGraphTriggerVolume>(true);
            if (_participants == null || _participants.Length == 0)
                _participants = GetComponentsInChildren<RecruitmentEncounterParticipant>(true);
            RebuildHostileParticipantIds();
        }

        public bool TryApplyPhase(RecruitmentEncounterPhase phase)
        {
            switch (phase)
            {
                case RecruitmentEncounterPhase.Dormant:
                    SetAllParticipantsHidden();
                    return true;
                case RecruitmentEncounterPhase.CombatActive:
                    return TryActivateCombat();
                case RecruitmentEncounterPhase.CombatResolved:
                    return TryPrepareDialogue();
                case RecruitmentEncounterPhase.Completed:
                    EndEntryPolling();
                    _entryVolume?.SetRoutingEnabled(false);
                    SetAllParticipantsHidden();
                    return true;
                default:
                    return false;
            }
        }

        public bool TryActivateCombat()
        {
            if (!TryValidateParticipantLayout(out string invalidReason))
            {
                Debug.LogError(
                    $"[RecruitmentEncounter] '{EncounterId}' 참가자 구성이 유효하지 않습니다: {invalidReason}",
                    this);
                return false;
            }

            if (!Services.TryGet<ICombatRelationService>(out _))
            {
                Debug.LogError(
                    $"[RecruitmentEncounter] '{EncounterId}' 전투 진영 서비스가 준비되지 않았습니다.",
                    this);
                return false;
            }

            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            IReadOnlyList<string> defeated = service?.GetDefeatedHostileIds(EncounterId);
            bool allyActivated = false;
            int activeHostiles = 0;

            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null)
                    continue;

                if (participant.Role == RecruitmentEncounterRole.Hostile
                    && ContainsOrdinal(defeated, participant.ParticipantId))
                {
                    _hostileGroup.UnregisterMember(participant.Actor);
                    participant.SetDormantOrHidden();
                    continue;
                }

                if (!participant.ActivateCombat(_definition.AllyFaction))
                {
                    SetAllParticipantsHidden();
                    Debug.LogError(
                        $"[RecruitmentEncounter] '{EncounterId}' 참가자 '{participant.ParticipantId}' 활성화에 실패했습니다.",
                        participant);
                    return false;
                }

                if (participant.Role == RecruitmentEncounterRole.RequiredAlly)
                    allyActivated = true;
                else
                {
                    activeHostiles++;
                    _hostileGroup?.EnsureMemberRegistered(participant.Actor);
                }
            }

            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant.Role != RecruitmentEncounterRole.Hostile
                    || CombatRelationUtility.CanTarget(participant.Actor, _allyActor))
                {
                    continue;
                }

                SetAllParticipantsHidden();
                Debug.LogError(
                    $"[RecruitmentEncounter] '{EncounterId}' 적과 필수 아군의 진영 관계가 적대가 아닙니다.",
                    this);
                return false;
            }

            _hostileGroup?.Activate();
            bool activated = allyActivated && (activeHostiles > 0 || AllHostilesWereDefeated(defeated));
            if (activated)
            {
                EndEntryPolling();
                _entryVolume?.SetRoutingEnabled(false);
                RuntimeLog.Trace(
                    RuntimeLogCategory.System,
                    $"[RecruitmentEncounter] '{EncounterId}' 전투를 시작했습니다.",
                    this);
            }
            return activated;
        }

        public bool TryPrepareDialogue()
        {
            if (_allyActor == null)
                return false;

            EndEntryPolling();
            _entryVolume?.SetRoutingEnabled(false);

            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null)
                    continue;
                if (participant.Role == RecruitmentEncounterRole.RequiredAlly)
                    participant.PrepareDialogue();
                else
                    participant.SetDormantOrHidden();
            }

            _allyActor.Detection?.ForceResetTarget();
            _allyActor.Abilities?.CancelAllAbilities();
            if (_dialogueAnchor != null)
                _allyActor.PlaceAtEncounterAnchor(_dialogueAnchor);
            return true;
        }

        private bool TryRegisterRuntime()
        {
            if (_runtimeLease != null)
                return true;
            if (!TryValidateParticipantLayout(out string invalidReason))
            {
                Debug.LogError(
                    $"[RecruitmentEncounter] 런타임 등록에 실패했습니다: {invalidReason}",
                    this);
                return false;
            }
            if (!Services.TryGet<IRecruitmentEncounterService>(out var service))
                return false;
            if (!BindParticipants(service))
                return false;

            _runtimeLease = service.RegisterRuntime(this);
            if (_runtimeLease != null)
                return true;

            UnbindParticipants();
            Debug.LogError(
                $"[RecruitmentEncounter] '{EncounterId}' 런타임 등록이 거부되었습니다. 중복 ID와 정의 데이터를 확인하세요.",
                this);
            return false;
        }

        private IEnumerator RegisterRuntimeWhenAvailable()
        {
            while (isActiveAndEnabled
                   && (!Services.TryGet<IRecruitmentEncounterService>(out _)
                       || !Services.TryGet<ICombatRelationService>(out _)
                       || !Services.TryGet<IActorQueryService>(out var registeredActors)
                       || registeredActors.Player == null))
            {
                yield return null;
            }

            if (!isActiveAndEnabled || !TryRegisterRuntime())
            {
                _runtimeRegistrationRoutine = null;
                yield break;
            }

            // FlowGraphRunner.OnEnable과 서비스 초기화가 모두 끝난 뒤 저장 단계를 재개한다.
            yield return null;
            _runtimeRegistrationRoutine = null;

            if (!Services.TryGet<IRecruitmentEncounterService>(out var service))
                yield break;

            RecruitmentEncounterPhase phase = service.GetPhase(EncounterId);
            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[RecruitmentEncounter] '{EncounterId}' 런타임 준비 완료 — 단계: {phase}",
                this);

            if (phase == RecruitmentEncounterPhase.Dormant)
            {
                bool overlapEntryFired = _entryVolume != null
                    && _entryVolume.SetRoutingEnabled(true);
                if (!overlapEntryFired)
                    BeginEntryPolling();
                yield break;
            }

            if (phase is RecruitmentEncounterPhase.CombatActive
                or RecruitmentEncounterPhase.CombatResolved)
            {
                _flowRunner?.FireManualEntries(_resumeEntryId);
            }
        }

        /// <summary>
        /// 플레이어가 진입 볼륨 안에 있는지 주기적으로 확인한다.
        /// KCC 액터는 물리 Trigger 콜백이 보장되지 않으므로 조우 시작을 콜백 한 번에 의존하지 않는다.
        /// </summary>
        public void ManagedTick(float deltaTime)
        {
            if (!_isEntryPollActive)
                return;

            _entryPollTimer -= deltaTime;
            if (_entryPollTimer > 0f)
                return;

            _entryPollTimer = _entryPollInterval;
            TryRouteEntryByPlayerPosition();
        }

        private void BeginEntryPolling()
        {
            if (_isEntryPollActive || _entryVolume == null)
                return;

            _isEntryPollActive = true;
            _entryPollTimer = 0f;
            _lastEntryFailure = FlowVolumeRouteFailure.None;
            AgentTickManager.Instance?.Register(null, this);
        }

        private void EndEntryPolling()
        {
            if (!_isEntryPollActive)
                return;

            _isEntryPollActive = false;
            AgentTickManager.Instance?.Unregister(this);
        }

        private void TryRouteEntryByPlayerPosition()
        {
            if (!Services.TryGet<IActorQueryService>(out var actors))
                return;

            if (_entryVolume.TryRouteActorIfInside(actors.Player, out FlowVolumeRouteFailure failure))
            {
                RuntimeLog.Trace(
                    RuntimeLogCategory.System,
                    $"[RecruitmentEncounter] '{EncounterId}' 플레이어 위치로 진입을 보정했습니다.",
                    this);
                EndEntryPolling();
                return;
            }

            // 같은 사유가 매 주기 반복되므로 사유가 바뀔 때만 남긴다.
            if (failure == _lastEntryFailure)
                return;

            _lastEntryFailure = failure;
            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[RecruitmentEncounter] '{EncounterId}' 진입 대기 — 사유: {failure}",
                this);
        }

        private bool BindParticipants(IRecruitmentEncounterService service)
        {
            if (_participantsBound)
                return true;
            if (!TryValidateParticipantLayout(out _))
                return false;

            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (!participant.Bind(service, EncounterId))
                {
                    UnbindParticipants();
                    return false;
                }
            }

            _participantsBound = true;
            return true;
        }

        private bool TryValidateParticipantLayout(out string invalidReason)
        {
            invalidReason = null;
            if (_definition == null) return FailValidation("정의가 없습니다.", out invalidReason);
            if (string.IsNullOrWhiteSpace(EncounterId)) return FailValidation("조우 ID가 비어 있습니다.", out invalidReason);
            if (_definition.RecruitCharacter == CharacterActorType.None) return FailValidation("영입 캐릭터가 지정되지 않았습니다.", out invalidReason);
            if (_definition.AllyFaction == null) return FailValidation("아군 진영이 지정되지 않았습니다.", out invalidReason);
            if (_flowRunner == null || _flowRunner.Graph == null) return FailValidation("FlowGraph Runner 또는 Graph가 없습니다.", out invalidReason);
            if (_entryVolume == null) return FailValidation("진입 볼륨이 지정되지 않았습니다.", out invalidReason);
            if (string.IsNullOrWhiteSpace(_resumeEntryId)) return FailValidation("재개 진입점 ID가 비어 있습니다.", out invalidReason);
            if (_allyActor == null) return FailValidation("필수 아군 액터가 없습니다.", out invalidReason);
            if (_hostileGroup == null) return FailValidation("적 그룹이 없습니다.", out invalidReason);
            if (_participants == null || _participants.Length == 0) return FailValidation("참가자가 없습니다.", out invalidReason);

            var ids = new HashSet<string>(StringComparer.Ordinal);
            int allyCount = 0;
            int hostileCount = 0;
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null
                    || participant.Actor == null)
                    return FailValidation("참가자 또는 참가자 액터 참조가 누락되었습니다.", out invalidReason);
                if (string.IsNullOrWhiteSpace(participant.ParticipantId))
                    return FailValidation("참가자 ID가 비어 있습니다.", out invalidReason);
                if (!ids.Add(participant.ParticipantId))
                    return FailValidation($"참가자 ID '{participant.ParticipantId}'가 중복됩니다.", out invalidReason);

                if (participant.Role == RecruitmentEncounterRole.RequiredAlly)
                {
                    allyCount++;
                    if (participant.Actor != _allyActor)
                        return FailValidation("필수 아군 참가자와 아군 액터 참조가 일치하지 않습니다.", out invalidReason);
                }
                else
                {
                    hostileCount++;
                }
            }

            if (allyCount != 1)
                return FailValidation("필수 아군은 정확히 한 명이어야 합니다.", out invalidReason);
            if (hostileCount == 0)
                return FailValidation("적 참가자가 한 명 이상 필요합니다.", out invalidReason);
            return true;
        }

        private static bool FailValidation(string reason, out string invalidReason)
        {
            invalidReason = reason;
            return false;
        }

        private void UnbindParticipants()
        {
            if (_participants != null)
            {
                for (int i = 0; i < _participants.Length; i++)
                    _participants[i]?.Unbind();
            }
            _participantsBound = false;
        }

        private void SetAllParticipantsHidden()
        {
            if (_participants == null)
                return;
            for (int i = 0; i < _participants.Length; i++)
                _participants[i]?.SetDormantOrHidden();
        }

        private void RebuildHostileParticipantIds()
        {
            _hostileParticipantIds.Clear();
            if (_participants == null)
                return;
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant != null
                    && participant.Role == RecruitmentEncounterRole.Hostile
                    && !string.IsNullOrWhiteSpace(participant.ParticipantId))
                {
                    _hostileParticipantIds.Add(participant.ParticipantId);
                }
            }
        }

        private bool AllHostilesWereDefeated(IReadOnlyList<string> defeated)
        {
            if (_hostileParticipantIds.Count == 0)
                return false;
            for (int i = 0; i < _hostileParticipantIds.Count; i++)
            {
                if (!ContainsOrdinal(defeated, _hostileParticipantIds[i]))
                    return false;
            }
            return true;
        }

        private static bool ContainsOrdinal(IReadOnlyList<string> values, string expected)
        {
            if (values == null)
                return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
