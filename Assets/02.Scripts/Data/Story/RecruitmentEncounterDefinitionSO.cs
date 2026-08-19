using System;
using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] private CharacterActorType _recruitCharacter;
        [SerializeField] private CombatFactionSO _allyFaction;
        [SerializeField] private RecruitmentAllyFailurePolicy _allyFailurePolicy =
            RecruitmentAllyFailurePolicy.Incapacitate;
        [SerializeField] private RecruitmentEncounterResetScope _resetScope =
            RecruitmentEncounterResetScope.PersistUntilNewGame;
        [Tooltip("마지막 적의 사망 연출과 전투 여운을 보존한 뒤 동료가 플레이어에게 다가오기 시작하는 시간입니다.")]
        [Min(0f)] [SerializeField] private float _postCombatSettleSeconds = 1.25f;

        [Tooltip("0보다 크면 전투 종료 후 동료가 플레이어의 이 거리까지 직접 다가온 뒤 대화를 시작합니다. 0이면 현재 위치를 유지합니다.")]
        [Min(0f)] [SerializeField] private float _dialogueApproachDistance = 2.8f;

        [Tooltip("대화 접근 이동에 적용할 해당 액터 달리기 속도의 배율입니다.")]
        [Min(0.1f)] [SerializeField] private float _dialogueApproachSpeedMultiplier = 0.65f;

        [Tooltip("길 막힘 등으로 접근을 끝내지 못해도 대화 흐름이 멈추지 않도록 기다리는 최대 시간입니다.")]
        [Min(0.1f)] [SerializeField] private float _dialogueApproachTimeoutSeconds = 6f;

        public string EncounterId => _encounterId;
        public string PrerequisiteEncounterId => _prerequisiteEncounterId;
        public string RequiredFlagKey => _requiredFlagKey;
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
