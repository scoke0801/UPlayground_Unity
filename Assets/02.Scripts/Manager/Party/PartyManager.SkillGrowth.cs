using System.Collections.Generic;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Manager
{
    public partial class PartyManager
    {
        private readonly CharacterSkillProgressionService _skillProgression = new();
        private bool _skillTreeAccessAllowed;
        // CharacterSkillProgressionService의 변경 이벤트는 상태 반영 후 동기 발화된다.
        // 벤치 HP 비율을 보존하려면 변경 전 실효 최대 체력을 여기서 임시 보관해야 한다.
        private CharacterActorType _skillProgressHealthSnapshotType;
        private float _skillProgressPreviousHealth;
        private float _skillProgressPreviousMaxHealth;
        private bool _hasSkillProgressHealthSnapshot;

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

        public bool CanPreviewSkillNode(
            CharacterActorType type,
            string nodeId,
            out SkillNodeBlockReason reason) =>
            _skillProgression.CanTakeNode(
                type,
                nodeId,
                out reason,
                requireSafeZone: false);

        public bool TryTakeSkillNode(CharacterActorType type, string nodeId)
        {
            CaptureSkillProgressHealth(type);
            try
            {
                return _skillProgression.TryTakeNode(type, nodeId);
            }
            finally
            {
                ClearSkillProgressHealthSnapshot();
            }
        }

        public bool TryRespecSkillTree(CharacterActorType type)
        {
            CaptureSkillProgressHealth(type);
            try
            {
                return _skillProgression.TryRespec(type);
            }
            finally
            {
                ClearSkillProgressHealthSnapshot();
            }
        }

        public void SetSkillTreeAccessAllowed(bool allowed) =>
            _skillTreeAccessAllowed = allowed;

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

        public IReadOnlyList<PassiveAbilitySO> GetGrantedPassives(
            CharacterActorType type) =>
            _skillProgression.GetGrantedPassives(type);

        private void ConfigureSkillProgression()
        {
            _skillProgression.Configure(
                _config?.characterSkillTrees,
                _config?.skillPointRule,
                GetLevel,
                () => _skillTreeAccessAllowed);
        }

        private void HandleSkillProgressChanged(CharacterActorType type)
        {
            if (_player != null
                && _hasSkillProgressHealthSnapshot
                && _skillProgressHealthSnapshotType == type)
            {
                _player.RefreshSkillTreeStatsForCharacter(
                    type,
                    _skillProgressPreviousHealth,
                    _skillProgressPreviousMaxHealth);
            }
            else
            {
                _player?.RefreshSkillTreeStatsForCharacter(type);
            }
            OnSkillProgressChanged?.Invoke(type);
            OnPartyProgressionChanged?.Invoke(type);
        }

        private void CaptureSkillProgressHealth(CharacterActorType type)
        {
            ClearSkillProgressHealthSnapshot();
            if (_player == null
                || type == CharacterActorType.None
                || type == _player.CharacterType
                || !_player.HasHealthRecordForCharacter(type))
            {
                return;
            }

            _skillProgressHealthSnapshotType = type;
            _skillProgressPreviousHealth = _player.GetHealthForCharacter(type);
            _skillProgressPreviousMaxHealth = _player.GetMaxHealthForCharacter(type);
            _hasSkillProgressHealthSnapshot = true;
        }

        private void ClearSkillProgressHealthSnapshot()
        {
            _skillProgressHealthSnapshotType = CharacterActorType.None;
            _skillProgressPreviousHealth = 0f;
            _skillProgressPreviousMaxHealth = 0f;
            _hasSkillProgressHealthSnapshot = false;
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
