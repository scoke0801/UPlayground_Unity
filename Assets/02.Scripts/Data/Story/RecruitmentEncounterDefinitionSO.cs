using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Cinematic;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Story
{
    public enum RecruitmentAllyFailurePolicy
    {
        Incapacitate,
    }

    public enum RecruitmentEncounterResetScope
    {
        PersistUntilNewGame,
        ResetOnCycle,
    }

    public enum RecruitmentEncounterPhase
    {
        Dormant,
        CombatActive,
        CombatResolved,
        Completed,
        RecruitmentCommitted,
    }

    public enum RecruitmentEncounterRole
    {
        RequiredAlly,
        Hostile,
    }

    [CreateAssetMenu(
        fileName = "RecruitmentEncounter",
        menuName = "UPlayGround/Story/Recruitment Encounter")]
    public sealed class RecruitmentEncounterDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _encounterId;
        [SerializeField] private string _prerequisiteEncounterId;
        [SerializeField] private string _requiredFlagKey;

        [Tooltip("이 조우 지점을 가리키는 퀘스트 마커 위치 ID. 퀘스트 목표의 markerLocationId와 같은 값을 쓴다. 비우면 마커가 생기지 않는다.")]
        [SerializeField] private string _questMarkerLocationId;
        [SerializeField] private CharacterActorType _recruitCharacter;
        [SerializeField] private CombatFactionSO _allyFaction;
        [SerializeField] private RecruitmentAllyFailurePolicy _allyFailurePolicy =
            RecruitmentAllyFailurePolicy.Incapacitate;
        [SerializeField] private RecruitmentEncounterResetScope _resetScope =
            RecruitmentEncounterResetScope.PersistUntilNewGame;
        [Header("진입 인지")]
        [Tooltip("이 거리 안에서 참가자가 화면에 잡히면 '목격'으로 보고 대치 장면을 노출합니다. 0이면 목격 판정을 쓰지 않고 진입 볼륨만 사용합니다.")]
        [Min(0f)] [SerializeField] private float _noticeRadius = 22f;

        [Tooltip("목격 판정에 시선 차단 검사를 요구합니다. 끄면 벽 너머로도 화면에 잡히기만 하면 목격으로 봅니다.")]
        [SerializeField] private bool _requireLineOfSight = true;

        [Tooltip("목격 판정과 시선 검사에서 시야를 가로막는 것으로 볼 레이어입니다.")]
        [SerializeField] private LayerMask _noticeObstacleLayer;

        [Tooltip("목격한 뒤 플레이어가 이 거리까지 다가오면 전투를 시작합니다. 0이면 거리로는 시작하지 않고 진입 볼륨만 사용합니다.")]
        [Min(0f)] [SerializeField] private float _commitRadius = 12f;

        [Tooltip("끄면 목격 없이도 개입 거리만으로 전투를 시작합니다. 반드시 발생해야 하는 스토리 조우에 사용합니다.")]
        [SerializeField] private bool _requireNoticeBeforeCommit = true;

        [Header("등장 연출")]
        [Tooltip("참가자가 화면 안에서 등장할 때 그 순간을 가릴 전환입니다. None이면 가리지 않고 그대로 나타납니다.")]
        [SerializeField] private CinematicStageTransitionType _entryRevealTransition =
            CinematicStageTransitionType.Fade;

        [Tooltip("등장을 가리기 위해 화면을 덮는 시간입니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealCoverSeconds = 0.25f;

        [Tooltip("완전히 덮인 상태를 유지하는 시간입니다. 참가자 배치가 자리를 잡을 여유를 줍니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealHoldSeconds = 0.1f;

        [Tooltip("덮은 화면을 다시 걷어내는 시간입니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealSeconds = 0.45f;

        [Tooltip("등장 후 진입 볼륨이 열리기까지 두는 최소 대치 시간입니다. 등장과 전투 시작이 같은 프레임에 겹치는 것을 막습니다.")]
        [Min(0f)] [SerializeField] private float _entryStandoffSeconds = 0.35f;

        [Tooltip("마지막 적의 사망 연출과 전투 여운을 보존한 뒤 동료가 플레이어에게 다가오기 시작하는 시간입니다.")]
        [Min(0f)] [SerializeField] private float _postCombatSettleSeconds = 1.25f;

        [Tooltip("0보다 크면 전투 종료 후 동료가 플레이어의 이 거리까지 직접 다가온 뒤 대화를 시작합니다. 0이면 현재 위치를 유지합니다.")]
        [Min(0f)] [SerializeField] private float _dialogueApproachDistance = 2.8f;

        [Tooltip("대화 접근 이동에 적용할 해당 액터 달리기 속도의 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float _dialogueApproachSpeedMultiplier = 0.65f;

        [Tooltip("길 막힘 등으로 접근을 끝내지 못해도 대화 흐름이 멈추지 않도록 기다리는 최대 시간입니다.")]
        [Min(0.1f)] [SerializeField] private float _dialogueApproachTimeoutSeconds = 6f;

        public float NoticeRadius => Mathf.Max(0f, _noticeRadius);
        public bool RequireLineOfSight => _requireLineOfSight;
        public LayerMask NoticeObstacleLayer => _noticeObstacleLayer;
        public float CommitRadius => Mathf.Max(0f, _commitRadius);
        public bool RequireNoticeBeforeCommit => _requireNoticeBeforeCommit;
        public CinematicStageTransitionType EntryRevealTransition => _entryRevealTransition;
        public float EntryRevealCoverSeconds => Mathf.Max(0f, _entryRevealCoverSeconds);
        public float EntryRevealHoldSeconds => Mathf.Max(0f, _entryRevealHoldSeconds);
        public float EntryRevealSeconds => Mathf.Max(0f, _entryRevealSeconds);
        public float EntryStandoffSeconds => Mathf.Max(0f, _entryStandoffSeconds);

        public string EncounterId => _encounterId;
        public string PrerequisiteEncounterId => _prerequisiteEncounterId;
        public string RequiredFlagKey => _requiredFlagKey;
        public string QuestMarkerLocationId => _questMarkerLocationId;
        public CharacterActorType RecruitCharacter => _recruitCharacter;
        public CombatFactionSO AllyFaction => _allyFaction;
        public RecruitmentAllyFailurePolicy AllyFailurePolicy => _allyFailurePolicy;
        public RecruitmentEncounterResetScope ResetScope => _resetScope;
        public float PostCombatSettleSeconds => Mathf.Max(0f, _postCombatSettleSeconds);
        public float DialogueApproachDistance => Mathf.Max(0f, _dialogueApproachDistance);
        public float DialogueApproachSpeedMultiplier =>
            Mathf.Max(0.1f, _dialogueApproachSpeedMultiplier);
        public float DialogueApproachTimeoutSeconds =>
            Mathf.Max(0.1f, _dialogueApproachTimeoutSeconds);

        private void OnValidate()
        {
            _encounterId = _encounterId?.Trim();
            _prerequisiteEncounterId = _prerequisiteEncounterId?.Trim();
            _requiredFlagKey = _requiredFlagKey?.Trim();
            _noticeRadius = Mathf.Max(0f, _noticeRadius);
            _commitRadius = Mathf.Max(0f, _commitRadius);
            _entryRevealCoverSeconds = Mathf.Max(0f, _entryRevealCoverSeconds);
            _entryRevealHoldSeconds = Mathf.Max(0f, _entryRevealHoldSeconds);
            _entryRevealSeconds = Mathf.Max(0f, _entryRevealSeconds);
            _entryStandoffSeconds = Mathf.Max(0f, _entryStandoffSeconds);
            _postCombatSettleSeconds = Mathf.Max(0f, _postCombatSettleSeconds);
            _dialogueApproachDistance = Mathf.Max(0f, _dialogueApproachDistance);
            _dialogueApproachSpeedMultiplier = Mathf.Max(
                0.1f,
                _dialogueApproachSpeedMultiplier);
            _dialogueApproachTimeoutSeconds = Mathf.Max(
                0.1f,
                _dialogueApproachTimeoutSeconds);
        }
    }

    [Serializable]
    public sealed class RecruitmentEncounterSaveEntry
    {
        public string encounterId;
        public CharacterActorType recruitCharacter;
        public RecruitmentEncounterPhase phase;
        public List<string> defeatedHostileIds = new();

        public RecruitmentEncounterSaveEntry Clone()
        {
            return new RecruitmentEncounterSaveEntry
            {
                encounterId = encounterId,
                recruitCharacter = recruitCharacter,
                phase = phase,
                defeatedHostileIds = defeatedHostileIds != null
                    ? new List<string>(defeatedHostileIds)
                    : new List<string>(),
            };
        }
    }
}
