using System;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Story
{
    /// <summary>Unity 오브젝트와 무관하게 조우 단계와 부분 처치 상태의 불변식을 관리한다.</summary>
    public sealed class RecruitmentEncounterStateStore
    {
        private readonly Dictionary<string, RecruitmentEncounterSaveEntry> _states = new();
        private readonly Dictionary<string, RecruitmentEncounterResetScope> _resetScopes = new();

        public bool TryRegisterDefinition(
            string encounterId,
            CharacterActorType recruitCharacter,
            RecruitmentEncounterResetScope resetScope)
        {
            encounterId = NormalizeId(encounterId);
            if (encounterId == null || recruitCharacter == CharacterActorType.None)
                return false;

            if (_states.TryGetValue(encounterId, out RecruitmentEncounterSaveEntry existing))
            {
                if (existing.recruitCharacter != CharacterActorType.None
                    && existing.recruitCharacter != recruitCharacter)
                {
                    return false;
                }

                existing.recruitCharacter = recruitCharacter;
                existing.defeatedHostileIds ??= new List<string>();
            }
            else
            {
                _states.Add(encounterId, new RecruitmentEncounterSaveEntry
                {
                    encounterId = encounterId,
                    recruitCharacter = recruitCharacter,
                    phase = RecruitmentEncounterPhase.Dormant,
                });
            }

            _resetScopes[encounterId] = resetScope;
            return true;
        }

        public bool Contains(string encounterId) =>
            _states.ContainsKey(NormalizeId(encounterId) ?? string.Empty);

        public RecruitmentEncounterPhase GetPhase(string encounterId)
        {
            return TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                ? entry.phase
                : RecruitmentEncounterPhase.Dormant;
        }

        public bool IsCompleted(string encounterId) =>
            GetPhase(encounterId) == RecruitmentEncounterPhase.Completed;

        public CharacterActorType GetRecruitCharacter(string encounterId)
        {
            return TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                ? entry.recruitCharacter
                : CharacterActorType.None;
        }

        public IReadOnlyList<string> GetDefeatedHostileIds(string encounterId)
        {
            return TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                ? entry.defeatedHostileIds
                : Array.Empty<string>();
        }

        public bool TryStartCombat(string encounterId)
        {
            if (!TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                || entry.phase != RecruitmentEncounterPhase.Dormant)
                return false;

            entry.phase = RecruitmentEncounterPhase.CombatActive;
            return true;
        }

        public bool RecordHostileDefeated(string encounterId, string participantId)
        {
            participantId = NormalizeId(participantId);
            if (participantId == null
                || !TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                || entry.phase != RecruitmentEncounterPhase.CombatActive)
                return false;

            entry.defeatedHostileIds ??= new List<string>();
            if (!entry.defeatedHostileIds.Contains(participantId))
                entry.defeatedHostileIds.Add(participantId);
            return true;
        }

        public bool TryResolveCombat(string encounterId)
        {
            if (!TryGet(encounterId, out RecruitmentEncounterSaveEntry entry)
                || entry.phase != RecruitmentEncounterPhase.CombatActive)
                return false;

            entry.phase = RecruitmentEncounterPhase.CombatResolved;
            return true;
        }

        public bool TryCommitRecruitment(string encounterId)
        {
            if (!TryGet(encounterId, out RecruitmentEncounterSaveEntry entry))
                return false;
            if (entry.phase == RecruitmentEncounterPhase.RecruitmentCommitted
                || entry.phase == RecruitmentEncounterPhase.Completed)
                return true;
            if (entry.phase != RecruitmentEncounterPhase.CombatResolved)
                return false;

            entry.phase = RecruitmentEncounterPhase.RecruitmentCommitted;
            return true;
        }

        public bool TryComplete(string encounterId)
        {
            if (!TryGet(encounterId, out RecruitmentEncounterSaveEntry entry))
                return false;
            if (entry.phase == RecruitmentEncounterPhase.Completed)
                return true;
            if (entry.phase != RecruitmentEncounterPhase.RecruitmentCommitted)
                return false;

            entry.phase = RecruitmentEncounterPhase.Completed;
            return true;
        }

        public List<RecruitmentEncounterSaveEntry> Export()
        {
            var result = new List<RecruitmentEncounterSaveEntry>(_states.Count);
            foreach (RecruitmentEncounterSaveEntry entry in _states.Values)
                result.Add(entry.Clone());
            result.Sort((left, right) => string.CompareOrdinal(left.encounterId, right.encounterId));
            return result;
        }

        public void Import(IEnumerable<RecruitmentEncounterSaveEntry> entries)
        {
            _states.Clear();
            if (entries == null)
                return;

            foreach (RecruitmentEncounterSaveEntry source in entries)
            {
                string encounterId = NormalizeId(source?.encounterId);
                if (encounterId == null || _states.ContainsKey(encounterId))
                    continue;

                RecruitmentEncounterSaveEntry copy = source.Clone();
                copy.encounterId = encounterId;
                if (!Enum.IsDefined(typeof(CharacterActorType), copy.recruitCharacter))
                    copy.recruitCharacter = CharacterActorType.None;
                if (!Enum.IsDefined(typeof(RecruitmentEncounterPhase), copy.phase))
                    copy.phase = RecruitmentEncounterPhase.Dormant;
                copy.defeatedHostileIds = NormalizeParticipantIds(copy.defeatedHostileIds);
                _states.Add(encounterId, copy);
            }
        }

        public void ResetForNewGame()
        {
            _states.Clear();
            _resetScopes.Clear();
        }

        public List<string> ResetForCycle()
        {
            var resetEncounterIds = new List<string>();
            foreach (KeyValuePair<string, RecruitmentEncounterResetScope> pair in _resetScopes)
            {
                if (pair.Value != RecruitmentEncounterResetScope.ResetOnCycle
                    || !_states.TryGetValue(pair.Key, out RecruitmentEncounterSaveEntry entry)
                    || entry.phase == RecruitmentEncounterPhase.Completed)
                {
                    continue;
                }

                entry.phase = RecruitmentEncounterPhase.Dormant;
                entry.defeatedHostileIds?.Clear();
                resetEncounterIds.Add(pair.Key);
            }
            return resetEncounterIds;
        }

        private bool TryGet(string encounterId, out RecruitmentEncounterSaveEntry entry)
        {
            string normalized = NormalizeId(encounterId);
            if (normalized != null)
                return _states.TryGetValue(normalized, out entry);
            entry = null;
            return false;
        }

        private static string NormalizeId(string value)
        {
            string normalized = value?.Trim();
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }

        private static List<string> NormalizeParticipantIds(IEnumerable<string> values)
        {
            var result = new List<string>();
            if (values == null)
                return result;

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string value in values)
            {
                string normalized = NormalizeId(value);
                if (normalized != null && unique.Add(normalized))
                    result.Add(normalized);
            }
            return result;
        }
    }
}
