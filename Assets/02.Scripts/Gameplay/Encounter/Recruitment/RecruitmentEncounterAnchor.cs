using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data.Cinematic;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Diagnostics;
using UPlayGround.FlowGraph;
using UPlayGround.Group;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Encounter
{
    /// <summary>씬의 영입 대상, 적 참가자, FlowGraph를 저장 가능한 조우 서비스에 연결한다.</summary>
    public sealed class RecruitmentEncounterAnchor :
        MonoBehaviour, IRecruitmentEncounterRuntimePort, IManagedTick, IAggroLockSource
    {
        [SerializeField] private RecruitmentEncounterDefinitionSO _definition;
        [SerializeField] private FlowGraphRunner _flowRunner;
        [SerializeField] private FlowGraphTriggerVolume _entryVolume;
        [SerializeField] private string _resumeEntryId = "Resume";
        [SerializeField] private MonsterActor _allyActor;
        [SerializeField] private MonsterGroupController _hostileGroup;
        [SerializeField] private RecruitmentEncounterParticipant[] _participants;
        [SerializeField] private Transform _dialogueAnchor;

        [Tooltip("진입 가능해진 참가자를 미리 보여주고 전투에서 제외한 대치 장면으로 세웁니다. 화면 안에서 갑자기 나타나는 현상을 막습니다.")]
        [SerializeField] private bool _stageParticipantsBeforeEntry = true;

        [Tooltip("고정 카메라 연출이 반드시 필요한 조우에서만 켭니다. 켜면 위치가 순간 변경될 수 있으므로 기본값은 끔입니다.")]
        [SerializeField] private bool _placeAllyAtDialogueAnchor;

        [Tooltip("등장 시 참가자를 상대 쪽으로 돌립니다(적→플레이어, 아군→적). 등을 보인 채 시작하는 어색함을 막습니다.")]
        [SerializeField] private bool _alignFacingOnActivate = true;

        [Tooltip("등장 시 참가자가 상대를 즉시 교전 대상으로 잡습니다. 시야 밖이라 감지가 늦어 멈춰 서 있는 것을 막습니다.")]
        [SerializeField] private bool _engageOnActivate = true;

        [Tooltip("플레이어와 이보다 가까운 참가자는 이 거리까지 밀어 배치합니다. 0이면 배치 위치를 그대로 씁니다.")]
        [Min(0f)] [SerializeField] private float _minPlayerSpawnDistance = 5f;

        [Tooltip("진입 조건(목격·개입 거리·진입 볼륨)을 다시 확인하는 주기(초).")]
        [SerializeField] private float _entryPollInterval = 0.25f;

        [Tooltip("진입 볼륨을 두지 않은 조우에서 전투를 시작할 때 발화할 수동 진입점 ID입니다.")]
        [SerializeField] private string _entryEntryId = "Entry";

        // 저작 위치와 높이가 이보다 벌어지면 지붕·절벽을 찍은 것으로 보고 배치를 포기한다.
        private const float MaxSpawnHeightDelta = 2f;

        // 목격·시선 판정에 쓰는 눈높이. 발밑 기준으로 재면 지면 경사에 시야가 막힌다.
        private const float ObserverEyeHeight = 1f;

        // 등장이 화면에 걸리는지 판단할 때 참가자 하나를 감싸는 대략적인 크기다.
        private static readonly Vector3 ParticipantVisibilityBoundsSize = new(2f, 2f, 2f);

        private readonly List<string> _hostileParticipantIds = new();
        private IDisposable _runtimeLease;
        private Coroutine _runtimeRegistrationRoutine;
        private bool _participantsBound;
        private bool _participantsStagedBeforeCombat;
        private bool _dialogueTransitionStarted;
        private bool _isDialogueTransitionReady;
        private bool _isEntryPollActive;
        private float _entryPollTimer;
        private bool _isEntryRevealPending;
        private bool _hasNoticedEncounter;
        private float _entryStandoffRemaining;
        private readonly Plane[] _frustumPlanes = new Plane[6];
        private FlowVolumeRouteFailure _lastEntryFailure = FlowVolumeRouteFailure.None;

        public string EncounterId => _definition != null ? _definition.EncounterId : null;
        public RecruitmentEncounterDefinitionSO Definition => _definition;
        public string DialoguePartnerActorId => _allyActor != null ? _allyActor.ActorId : null;
        public IReadOnlyList<string> HostileParticipantIds => _hostileParticipantIds;
        public bool IsDialogueTransitionReady => _isDialogueTransitionReady;

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
            _entryEntryId = _entryEntryId?.Trim();
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
                    _entryVolume?.SetRoutingEnabled(false);
                    SetAllParticipantsHidden();
                    ResetDialogueTransition();
                    return true;
                case RecruitmentEncounterPhase.CombatActive:
                    return TryActivateCombat();
                case RecruitmentEncounterPhase.CombatResolved:
                case RecruitmentEncounterPhase.RecruitmentCommitted:
                    return TryPrepareDialogue();
                case RecruitmentEncounterPhase.Completed:
                    EndEntryPolling();
                    _entryVolume?.SetRoutingEnabled(false);
                    SetAllParticipantsHidden();
                    ResetDialogueTransition();
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

                if (!participant.ActivateCombat(_definition.AllyFaction, this))
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
                bool wasStagedBeforeCombat = _participantsStagedBeforeCombat;
                _participantsStagedBeforeCombat = false;
                ResetDialogueTransition();
                StageActivatedParticipants(wasStagedBeforeCombat);
                EndEntryPolling();
                _entryVolume?.SetRoutingEnabled(false);
                RuntimeLog.Trace(
                    RuntimeLogCategory.System,
                    $"[RecruitmentEncounter] '{EncounterId}' 전투를 시작했습니다.",
                    this);
            }
            return activated;
        }

        /// <summary>
        /// 등장한 참가자를 플레이어 기준으로 정렬한다.
        /// 조우는 플레이어가 진입 볼륨을 밟는 위치에 따라 상대 배치가 달라지므로,
        /// 씬에 저작된 포즈만으로는 "등을 보인 채 등장" 과 "코앞 등장" 을 막을 수 없다.
        /// </summary>
        private void StageActivatedParticipants(bool wasStagedBeforeCombat)
        {
            if (!_alignFacingOnActivate
                && !_engageOnActivate
                && _minPlayerSpawnDistance <= 0f)
            {
                return;
            }

            if (!Services.TryGet<IActorQueryService>(out var actors)
                || actors.PlayerTransform == null)
            {
                return;
            }

            Transform player = actors.PlayerTransform;
            MonsterActor firstHostile = null;
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                MonsterActor actor = participant != null ? participant.Actor : null;
                if (actor == null || !actor.gameObject.activeInHierarchy)
                    continue;

                // 이미 화면에 보여준 대치 참가자를 전투 시작 순간 다시 배치하면 순간이동처럼 보인다.
                if (!wasStagedBeforeCombat && _minPlayerSpawnDistance > 0f)
                    PushOutsideMinimumPlayerDistance(actor, player.position);

                if (participant.Role != RecruitmentEncounterRole.Hostile)
                    continue;

                firstHostile ??= actor;
                if (_alignFacingOnActivate)
                    actor.FaceTargetHorizontally(player.position);

                // 시야 밖에서 등장한 적은 감지를 기다리는 동안 멈춰 서 있는다 → 등장 즉시 교전으로 붙인다.
                if (_engageOnActivate)
                    actor.Detection?.AcquireTarget(player);
            }

            if (_allyActor == null || !_allyActor.gameObject.activeInHierarchy)
                return;

            // 아군은 플레이어가 아니라 적을 상대하는 쪽이 자연스럽다. 적이 없으면 플레이어를 본다.
            Transform allyFocus = firstHostile != null ? firstHostile.transform : player;
            if (_alignFacingOnActivate)
                _allyActor.FaceTargetHorizontally(allyFocus.position);

            // 아군도 같은 이유로 교전에 붙인다 — 이미 싸우던 상황으로 보여야 조우 도입이 성립한다.
            if (_engageOnActivate && firstHostile != null)
                _allyActor.Detection?.AcquireTarget(firstHostile.transform);
        }

        /// <summary>
        /// 플레이어와 너무 가까운 참가자를 같은 방향의 최소 거리 지점으로 밀어낸다.
        /// 지면이나 통과 가능 공간을 찾지 못하면 저작 위치를 그대로 유지한다 — 벽 안쪽 배치가 더 나쁜 결과다.
        /// </summary>
        private void PushOutsideMinimumPlayerDistance(MonsterActor actor, Vector3 playerPosition)
        {
            Vector3 fromPlayer = actor.transform.position - playerPosition;
            fromPlayer.y = 0f;
            float distance = fromPlayer.magnitude;
            if (distance >= _minPlayerSpawnDistance)
                return;

            Vector3 direction = distance > 0.01f
                ? fromPlayer / distance
                : -PlayerHorizontalForward(playerPosition, actor.transform.position);
            Vector3 candidate = playerPosition + direction * _minPlayerSpawnDistance;
            candidate.y = actor.transform.position.y;

            if (!ActorStagePlacement.TryResolveGroundedPosition(
                    actor,
                    candidate,
                    actor.transform.position.y,
                    MaxSpawnHeightDelta,
                    out Vector3 grounded))
            {
                return;
            }

            actor.PlaceAtPose(grounded, actor.transform.rotation);
        }

        private Vector3 PlayerHorizontalForward(Vector3 playerPosition, Vector3 fallbackTarget)
        {
            Vector3 forward = fallbackTarget - playerPosition;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        public bool TryPrepareDialogue()
        {
            if (_allyActor == null)
                return false;

            if (_dialogueTransitionStarted)
                return true;

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

            _dialogueTransitionStarted = true;
            if (_placeAllyAtDialogueAnchor && _dialogueAnchor != null)
            {
                _allyActor.PlaceAtEncounterAnchor(_dialogueAnchor);
                _isDialogueTransitionReady = true;
                return true;
            }

            if (!Services.TryGet<IActorQueryService>(out var actors)
                || actors.PlayerTransform == null
                || _definition.DialogueApproachDistance <= 0f)
            {
                FaceAllyTowardPlayer(actors?.PlayerTransform);
                _isDialogueTransitionReady = true;
                return true;
            }

            Transform player = actors.PlayerTransform;
            if (!_allyActor.TryBeginStageApproach(
                    player,
                    _definition.DialogueApproachDistance,
                    _definition.DialogueApproachSpeedMultiplier,
                    _definition.DialogueApproachTimeoutSeconds,
                    _ => CompleteDialogueTransition(player)))
            {
                CompleteDialogueTransition(player);
            }
            return true;
        }

        /// <summary>진입 조건이 열린 참가자를 전투 대상이 아닌 대치 장면으로 미리 노출한다.</summary>
        private void StageDormantParticipants()
        {
            // 등장 전환 콜백은 비활성화된 뒤에도 도착할 수 있다. 꺼진 조우가 참가자를 되살리지 않게 막는다.
            if (!isActiveAndEnabled
                || !_stageParticipantsBeforeEntry
                || _participantsStagedBeforeCombat)
            {
                return;
            }

            MonsterActor firstHostile = null;
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null)
                    continue;

                participant.PrepareDormantPresentation();
                if (firstHostile == null
                    && participant.Role == RecruitmentEncounterRole.Hostile)
                {
                    firstHostile = participant.Actor;
                }
            }

            if (firstHostile != null && _allyActor != null)
            {
                _allyActor.FaceTargetHorizontally(firstHostile.transform.position);
                for (int i = 0; i < _participants.Length; i++)
                {
                    RecruitmentEncounterParticipant participant = _participants[i];
                    if (participant != null
                        && participant.Role == RecruitmentEncounterRole.Hostile)
                    {
                        participant.Actor.FaceTargetHorizontally(_allyActor.transform.position);
                    }
                }
            }

            _participantsStagedBeforeCombat = true;
        }

        private void CompleteDialogueTransition(Transform player)
        {
            FaceAllyTowardPlayer(player);
            _isDialogueTransitionReady = true;
        }

        private void FaceAllyTowardPlayer(Transform player)
        {
            _allyActor.Detection?.ForceResetTarget();
            _allyActor.StopStageApproach(player);
        }

        private void ResetDialogueTransition()
        {
            _dialogueTransitionStarted = false;
            _isDialogueTransitionReady = false;
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
                BeginEntryPolling();
                yield break;
            }

            if (phase is RecruitmentEncounterPhase.CombatActive
                or RecruitmentEncounterPhase.CombatResolved
                or RecruitmentEncounterPhase.RecruitmentCommitted)
            {
                _flowRunner?.FireManualEntries(_resumeEntryId);
            }
        }

        /// <summary>
        /// 조우 진입을 주기적으로 판정한다.
        /// 판정 기준은 "플레이어가 볼륨을 밟았는가"가 아니라 "조우를 목격하고 다가왔는가"다 —
        /// 위치 통과만 보면 우회할 때 눈앞의 대치를 놓치고, 스쳐 지나갈 때 등 뒤에서 전투가 열린다.
        /// </summary>
        public void ManagedTick(float deltaTime)
        {
            if (!_isEntryPollActive)
                return;

            if (_entryStandoffRemaining > 0f)
                _entryStandoffRemaining = Mathf.Max(0f, _entryStandoffRemaining - deltaTime);

            _entryPollTimer -= deltaTime;
            if (_entryPollTimer > 0f)
                return;

            _entryPollTimer = _entryPollInterval;

            IRecruitmentEncounterService service = Svc.RecruitmentEncounters;
            if (service == null || !service.IsEntryReady(EncounterId))
                return;

            // 1단(목격): 대치 장면을 먼저 세우고 이번 주기는 여기서 끝낸다.
            // 등장과 전투 시작이 같은 주기에 겹치면 다가서는 순간 몬스터가 솟아나는 그림이 된다.
            // 판정 시점에 참가자는 아직 숨겨져 있으므로 목격은 "지금 보인다"가 아니라
            // "그 자리에 있었다면 보였을 위치·시선인가"를 뜻한다. 등장 자체가 목격의 결과다.
            if (_stageParticipantsBeforeEntry && !_participantsStagedBeforeCombat)
            {
                if (!ShouldRevealParticipants())
                    return;
                if (TryBeginCoveredReveal())
                    return;

                StageDormantParticipants();
                return;
            }

            // 등장 전환과 그 직후 대치 시간 동안에는 진입을 열지 않는다.
            if (_isEntryRevealPending || _entryStandoffRemaining > 0f)
                return;

            // 2단(개입): 목격한 조우에 플레이어가 실제로 다가왔을 때 전투를 연다.
            if (!IsCommitConditionMet())
                return;

            CommitEntry();
        }

        /// <summary>대치 장면을 세울 시점인지. 목격했거나, 목격 없이 이미 개입 거리까지 들어온 경우다.</summary>
        private bool ShouldRevealParticipants()
            => IsEncounterNoticed() || IsCommitConditionMet();

        /// <summary>
        /// 플레이어가 조우를 목격했는지. 반경 안의 참가자가 화면에 잡히고 시선이 열려 있으면 성립한다.
        /// 한 번 성립하면 유지된다 — 목격한 뒤 시선을 돌렸다고 조우가 없던 일이 되면 더 어색하다.
        /// </summary>
        private bool IsEncounterNoticed()
        {
            if (_hasNoticedEncounter)
                return true;
            if (_definition == null || _definition.NoticeRadius <= 0f)
                return false;
            if (!HasParticipantInView(_definition.NoticeRadius, _definition.RequireLineOfSight))
                return false;

            _hasNoticedEncounter = true;
            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[RecruitmentEncounter] '{EncounterId}' 플레이어가 조우를 목격했습니다.",
                this);
            return true;
        }

        /// <summary>
        /// 전투를 열 조건. 진입 볼륨 안은 저작자가 지정한 확정 지점이므로 목격 여부와 무관하게 연다.
        /// 그 밖에는 목격한 조우에 개입 거리까지 다가왔을 때만 연다.
        /// </summary>
        private bool IsCommitConditionMet()
        {
            if (!TryGetPlayer(out IWorldActor player, out Vector3 playerPosition))
                return false;

            if (_entryVolume != null && _entryVolume.ContainsActor(player))
                return true;

            float commitRadius = _definition != null ? _definition.CommitRadius : 0f;
            if (commitRadius <= 0f)
                return false;
            if ((_definition == null || _definition.RequireNoticeBeforeCommit)
                && !IsEncounterNoticed())
            {
                return false;
            }

            return TryGetNearestParticipantSqrDistance(playerPosition, out float sqrDistance)
                   && sqrDistance <= commitRadius * commitRadius;
        }

        /// <summary>진입점을 발화한다. 볼륨이 있으면 볼륨 진입점을, 없으면 수동 진입점을 쓴다.</summary>
        private void CommitEntry()
        {
            if (!TryGetPlayer(out IWorldActor player, out _))
                return;

            if (_entryVolume != null)
            {
                // 볼륨 안에서 대기 중이었다면 라우팅을 여는 것만으로 발화된다.
                if (!_entryVolume.SetRoutingEnabled(true)
                    && !_entryVolume.TryRouteActor(player, out FlowVolumeRouteFailure failure))
                {
                    LogEntryFailureOnce(failure);
                    return;
                }
            }
            else if (_flowRunner == null || !_flowRunner.FireManualEntries(_entryEntryId))
            {
                LogEntryFailureOnce(FlowVolumeRouteFailure.EntryNotFired);
                return;
            }

            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[RecruitmentEncounter] '{EncounterId}' 진입을 시작했습니다.",
                this);
            EndEntryPolling();
        }

        private void LogEntryFailureOnce(FlowVolumeRouteFailure failure)
        {
            // 같은 사유가 매 주기 반복되므로 사유가 바뀔 때만 남긴다.
            if (failure == _lastEntryFailure)
                return;

            _lastEntryFailure = failure;
            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[RecruitmentEncounter] '{EncounterId}' 진입 대기 — 사유: {failure}",
                this);
        }

        private bool TryGetPlayer(out IWorldActor player, out Vector3 playerPosition)
        {
            player = null;
            playerPosition = default;
            if (!Services.TryGet<IActorQueryService>(out var actors) || actors.Player == null)
                return false;

            Transform playerTransform = actors.PlayerTransform;
            if (playerTransform == null)
                return false;

            player = actors.Player;
            playerPosition = playerTransform.position;
            return true;
        }

        private bool TryGetNearestParticipantSqrDistance(Vector3 origin, out float sqrDistance)
        {
            sqrDistance = float.MaxValue;
            if (_participants == null)
                return false;

            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null || participant.Actor == null)
                    continue;

                float candidate = (participant.Actor.transform.position - origin).sqrMagnitude;
                if (candidate < sqrDistance)
                    sqrDistance = candidate;
            }

            return sqrDistance < float.MaxValue;
        }

        /// <summary>
        /// 화면 안에서 일어나는 등장을 화면 전환으로 가린다.
        /// 참가자는 씬에 미리 배치돼 있어도 진입 조건이 열릴 때 비로소 보이므로,
        /// 플레이어 시야 안에서 그 순간이 그대로 노출되면 스폰 버그로 읽힌다.
        /// </summary>
        private bool TryBeginCoveredReveal()
        {
            if (_definition == null
                || _definition.EntryRevealTransition == CinematicStageTransitionType.None
                || !IsRevealVisibleToPlayer()
                || !Services.TryGet<ICinematicStageService>(out var cinematic))
            {
                return false;
            }

            var request = new ScreenCoverRequest(
                _definition.EntryRevealTransition,
                _definition.EntryRevealCoverSeconds,
                _definition.EntryRevealHoldSeconds,
                _definition.EntryRevealSeconds,
                StageDormantParticipants,
                HandleEntryRevealCompleted);

            if (!cinematic.TryPlayScreenCover(request))
                return false;

            _isEntryRevealPending = true;
            return true;
        }

        private void HandleEntryRevealCompleted()
        {
            _isEntryRevealPending = false;
            _entryStandoffRemaining = _definition != null ? _definition.EntryStandoffSeconds : 0f;
        }

        /// <summary>참가자 중 하나라도 현재 카메라 절두체 안에 있는지. 화면 밖 등장은 가릴 필요가 없다.</summary>
        private bool IsRevealVisibleToPlayer()
            => HasParticipantInView(maxDistance: 0f, requireLineOfSight: false);

        /// <summary>
        /// 참가자 중 하나라도 화면에 잡히는지 판정한다.
        /// <paramref name="maxDistance"/>가 0보다 크면 플레이어와의 거리로,
        /// <paramref name="requireLineOfSight"/>가 켜져 있으면 시선 차단으로 추가로 거른다.
        /// </summary>
        private bool HasParticipantInView(float maxDistance, bool requireLineOfSight)
        {
            UnityEngine.Camera camera = Svc.Camera?.GetMainCamera();
            if (camera == null || _participants == null)
                return false;

            bool hasDistanceLimit = maxDistance > 0f;
            Vector3 eyePosition = default;
            if (hasDistanceLimit || requireLineOfSight)
            {
                if (!TryGetPlayer(out _, out Vector3 playerPosition))
                    return false;

                eyePosition = playerPosition + Vector3.up * ObserverEyeHeight;
            }

            float maxSqrDistance = maxDistance * maxDistance;
            GeometryUtility.CalculateFrustumPlanes(camera, _frustumPlanes);
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                if (participant == null || participant.Actor == null)
                    continue;

                Vector3 chestPosition =
                    participant.Actor.transform.position + Vector3.up * ObserverEyeHeight;
                if (hasDistanceLimit
                    && (chestPosition - eyePosition).sqrMagnitude > maxSqrDistance)
                {
                    continue;
                }

                var bounds = new Bounds(
                    participant.Actor.transform.position + Vector3.up,
                    ParticipantVisibilityBoundsSize);
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                    continue;

                if (requireLineOfSight && IsSightBlocked(eyePosition, chestPosition))
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 두 지점 사이가 시야 차단 레이어로 막혀 있는지.
        /// 차단 레이어를 지정하지 않은 조우는 막힘 없음으로 본다 — 저작 미설정으로 조우가 열리지 않는 쪽이 더 나쁘다.
        /// </summary>
        private bool IsSightBlocked(Vector3 origin, Vector3 target)
        {
            LayerMask obstacles = _definition != null ? _definition.NoticeObstacleLayer : 0;
            if (obstacles.value == 0)
                return false;

            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            return distance > Mathf.Epsilon
                   && Physics.Raycast(
                       origin,
                       direction / distance,
                       distance,
                       obstacles,
                       QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 조우 전투 중 어그로가 비었을 때 채울 상대를 돌려준다.
        /// 아군은 살아 있는 적 참가자를, 적은 아군과 플레이어 중 가까운 쪽을 다시 문다.
        /// </summary>
        public Transform ResolveAggroTarget(GameActor owner)
        {
            if (owner == null || _participants == null)
                return null;

            Vector3 origin = owner.transform.position;
            if (owner == _allyActor)
                return FindNearestLivingHostile(origin);

            Transform ally = _allyActor != null && _allyActor.IsCombatAvailable
                ? _allyActor.transform
                : null;
            Transform player =
                Services.TryGet<IActorQueryService>(out var actors)
                && actors.Player != null
                && actors.Player.IsAlive
                    ? actors.PlayerTransform
                    : null;
            return PickNearest(origin, ally, player);
        }

        private Transform FindNearestLivingHostile(Vector3 origin)
        {
            Transform nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < _participants.Length; i++)
            {
                RecruitmentEncounterParticipant participant = _participants[i];
                MonsterActor actor = participant != null ? participant.Actor : null;
                if (actor == null
                    || participant.Role != RecruitmentEncounterRole.Hostile
                    || !actor.gameObject.activeInHierarchy
                    || !actor.IsCombatAvailable)
                {
                    continue;
                }

                float sqr = (actor.transform.position - origin).sqrMagnitude;
                if (sqr >= nearestSqr)
                    continue;

                nearestSqr = sqr;
                nearest = actor.transform;
            }

            return nearest;
        }

        private static Transform PickNearest(Vector3 origin, Transform first, Transform second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;

            return (first.position - origin).sqrMagnitude
                   <= (second.position - origin).sqrMagnitude
                ? first
                : second;
        }

        private void BeginEntryPolling()
        {
            // 진입 발화 경로는 볼륨 또는 수동 진입점 중 하나면 된다.
            if (_isEntryPollActive || (_entryVolume == null && _flowRunner == null))
                return;

            _isEntryPollActive = true;
            _entryPollTimer = 0f;
            _lastEntryFailure = FlowVolumeRouteFailure.None;
            _hasNoticedEncounter = false;
            AgentTickManager.Instance?.Register(null, this);
        }

        private void EndEntryPolling()
        {
            if (!_isEntryPollActive)
                return;

            _isEntryPollActive = false;
            _isEntryRevealPending = false;
            _entryStandoffRemaining = 0f;
            AgentTickManager.Instance?.Unregister(this);
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
            _participantsStagedBeforeCombat = false;
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
