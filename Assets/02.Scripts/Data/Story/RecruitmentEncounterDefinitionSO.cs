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
        [SerializeField] private CharacterActorType _recruitCharacter;
        [SerializeField] private CombatFactionSO _allyFaction;
        [SerializeField] private RecruitmentAllyFailurePolicy _allyFailurePolicy =
            RecruitmentAllyFailurePolicy.Incapacitate;
        [SerializeField] private RecruitmentEncounterResetScope _resetScope =
            RecruitmentEncounterResetScope.PersistUntilNewGame;
        [Min(0f)] [SerializeField] private float _postCombatSettleSeconds = 0.5f;

        public string EncounterId => _encounterId;
        public CharacterActorType RecruitCharacter => _recruitCharacter;
        public CombatFactionSO AllyFaction => _allyFaction;
        public RecruitmentAllyFailurePolicy AllyFailurePolicy => _allyFailurePolicy;
        public RecruitmentEncounterResetScope ResetScope => _resetScope;
        public float PostCombatSettleSeconds => Mathf.Max(0f, _postCombatSettleSeconds);

        private void OnValidate()
        {
            _encounterId = _encounterId?.Trim();
            _postCombatSettleSeconds = Mathf.Max(0f, _postCombatSettleSeconds);
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
