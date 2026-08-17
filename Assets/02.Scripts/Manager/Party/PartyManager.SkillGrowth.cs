using System.Collections.Generic;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Manager
{
    public partial class PartyManager
    {
        private readonly CharacterSkillProgressionService _skillProgression = new();

        public event System.Action<CharacterActorType> OnSkillProgressChanged;

        public CharacterSkillTreeSO GetSkillTree(CharacterActorType type) =>
            _skillProgression.GetTree(type);

        public int GetAvailableSkillPoints(CharacterActorType type) =>
            _skillProgression.GetAvailablePoints(type);

        public int GetSkillNodeRank(CharacterActorType type, string nodeId) =>
            _skillProgression.GetNodeRank(type, nodeId);

        public bool CanTakeSkillNode(
            CharacterActorType type,
            string nodeId,
            out SkillNodeBlockReason reason) =>
            _skillProgression.CanTakeNode(type, nodeId, out reason);

        public bool TryTakeSkillNode(CharacterActorType type, string nodeId) =>
            _skillProgression.TryTakeNode(type, nodeId);

        public bool TryRespecSkillTree(CharacterActorType type) =>
            _skillProgression.TryRespec(type);

        public IReadOnlyList<SkillStatModifierEntry> GetSkillStatModifiers(
            CharacterActorType type) =>
            _skillProgression.GetStatModifiers(type);

        public float GetAbilityScalar(
            CharacterActorType type,
            string abilityId,
            AbilityScalarKind kind) =>
            _skillProgression.GetAbilityScalar(type, abilityId, kind);

        public bool IsAbilityUnlocked(CharacterActorType type, string abilityId) =>
            _skillProgression.IsAbilityUnlocked(type, abilityId);

        public float GetDodgeCooldownMultiplier(CharacterActorType type) =>
            _skillProgression.GetDodgeCooldownMultiplier(type);

        public IReadOnlyList<PassiveAbilitySO> GetGrantedPassives(
            CharacterActorType type) =>
            _skillProgression.GetGrantedPassives(type);

        private void ConfigureSkillProgression()
        {
            _skillProgression.Configure(
                _config?.characterSkillTrees,
                _config?.skillPointRule,
                GetLevel);
        }

        private void HandleSkillProgressChanged(CharacterActorType type)
        {
            _player?.RefreshSkillTreeStatsForCharacter(type);
            OnSkillProgressChanged?.Invoke(type);
            OnPartyProgressionChanged?.Invoke(type);
        }

        private List<PassiveAbilitySO> GetAllPassives(CharacterActorType type)
        {
            var result = new List<PassiveAbilitySO>();
            var seen = new HashSet<PassiveAbilitySO>();
            CharacterPassiveSetSO fixedSet = GetPassiveSet(type);
            if (fixedSet?.passives != null)
                for (int i = 0; i < fixedSet.passives.Count; i++)
                {
                    PassiveAbilitySO passive = fixedSet.passives[i];
                    if (passive != null && seen.Add(passive))
                        result.Add(passive);
                }
            IReadOnlyList<PassiveAbilitySO> granted = GetGrantedPassives(type);
            for (int i = 0; i < granted.Count; i++)
                if (granted[i] != null && seen.Add(granted[i]))
                    result.Add(granted[i]);
            return result;
        }
    }
}
