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

    public enum RecruitmentEncounterCombatMode
    {
        CooperativeBattle = 0,
        HostileRecruitTarget = 1,
    }

    public enum RecruitmentIncapacitationRule
    {
        AnyFatalDamage = 0,
        FinishAttack = 1,
    }

    /// <summary>영입 대상의 치명 피해가 실제 제압 조건을 만족했는지 판정한다.</summary>
    public static class RecruitmentIncapacitationRuleEvaluator
    {
        public static bool IsSatisfied(
            RecruitmentIncapacitationRule rule,
            AttackKind attackKind,
            bool isSpecialBreak)
        {
            return rule switch
            {
                RecruitmentIncapacitationRule.AnyFatalDamage => true,
                RecruitmentIncapacitationRule.FinishAttack =>
                    attackKind == AttackKind.FinishAttack && !isSpecialBreak,
                _ => false,
            };
        }
    }

    public enum RecruitmentEncounterPhase
    {
        Dormant = 0,
        CombatActive = 1,
        CombatResolved = 2,
        Completed = 3,
        RecruitmentCommitted = 4,
        IntroductionPending = 5,
    }

    public enum RecruitmentEncounterRole
    {
        RequiredAlly = 0,
        Hostile = 1,
        RecruitTarget = 2,
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
        [SerializeField] private RecruitmentEncounterCombatMode _combatMode;
        [Tooltip("적대 영입 대상의 제압 조건입니다. 기존 공동 전투 데이터는 AnyFatalDamage 값을 유지합니다.")]
        [SerializeField] private RecruitmentIncapacitationRule _incapacitationRule =
            RecruitmentIncapacitationRule.AnyFatalDamage;
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

        [Tooltip("등장을 가리기 위해 화면을 덮는 시간입니다. 이 전환은 연출용 암전이 아니라 등장을 가리는 마스킹이므로, 덮기는 짧게 하고 걷기를 더 길게 잡습니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealCoverSeconds = 0.18f;

        [Tooltip("완전히 덮인 상태를 유지하는 시간입니다. 참가자 배치가 자리를 잡을 여유를 줍니다. 참가자 활성화 프레임에 스파이크가 나면 걷기가 하드컷으로 튀므로 너무 짧게 두지 않습니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealHoldSeconds = 0.12f;

        [Tooltip("덮은 화면을 다시 걷어내는 시간입니다. 플레이어가 바뀐 화면을 다시 읽어야 하므로 덮기보다 길게 잡습니다.")]
        [Min(0f)] [SerializeField] private float _entryRevealSeconds = 0.3f;

        [Tooltip("등장 후 진입 볼륨이 열리기까지 두는 최소 대치 시간입니다. 등장과 전투 시작이 같은 프레임에 겹치는 것을 막고, 조작이 돌아온 플레이어가 대치 장면을 읽을 시간을 줍니다.")]
        [Min(0f)] [SerializeField] private float _entryStandoffSeconds = 0.5f;

        [Tooltip("마지막 전투 처리와 사망 반응이 끝난 뒤 동료가 플레이어에게 다가오기 시작하는 시간입니다. 사망 디졸브가 끝날 때까지 기다리지는 않습니다 - 시체가 녹는 동안 동료가 걸어오는 겹침이 페이싱상 낫습니다. 이 값만 게임 시간 기준이라 히트스톱만큼 늘어납니다.")]
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
        public RecruitmentEncounterCombatMode CombatMode => _combatMode;
        public RecruitmentIncapacitationRule IncapacitationRule => _incapacitationRule;
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
