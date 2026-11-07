using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    public enum CombatRelation
    {
        Ally,
        Neutral,
        Hostile,
    }

    public enum CombatCreditOwner
    {
        None,
        PlayerParty,
        World,
    }

    public enum CombatTargetPolicy
    {
        Hostile,
        Ally,
        Self,
        Any,
    }

    /// <summary>전투 관계와 처치 귀속에 사용하는 안정적인 진영 식별자.</summary>
    [CreateAssetMenu(fileName = "CombatFaction_", menuName = "UPlayGround/Combat/Faction")]
    public sealed class CombatFactionSO : ScriptableObject
    {
        [SerializeField] private string _factionId;
        [SerializeField] private CombatCreditOwner _defaultCreditOwner;

        public string FactionId => _factionId;
        public CombatCreditOwner DefaultCreditOwner => _defaultCreditOwner;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _factionId = _factionId?.Trim();
        }
#endif
    }

    /// <summary>기존 액터 데이터가 진영 에셋을 아직 지정하지 않은 동안 사용하는 호환 규칙.</summary>
    public static class CombatFactionRules
    {
        public const string PlayerPartyId = "PlayerParty";
        public const string WorldHostileId = "WorldHostile";
        public const string WorldNeutralId = "WorldNeutral";

        public static string ResolveDefaultFactionId(ActorType actorType)
        {
            if ((actorType & ActorType.Player) != 0)
                return PlayerPartyId;
            if ((actorType & ActorType.Monster) != 0)
                return WorldHostileId;
            return WorldNeutralId;
        }

        public static CombatCreditOwner ResolveDefaultCreditOwner(ActorType actorType)
        {
            if ((actorType & ActorType.Player) != 0)
                return CombatCreditOwner.PlayerParty;
            if ((actorType & ActorType.Monster) != 0)
                return CombatCreditOwner.World;
            return CombatCreditOwner.None;
        }

        public static CombatRelation ResolveDefaultRelation(
            string firstFactionId,
            string secondFactionId)
        {
            if (string.IsNullOrWhiteSpace(firstFactionId)
                || string.IsNullOrWhiteSpace(secondFactionId))
            {
                return CombatRelation.Neutral;
            }

            if (string.Equals(firstFactionId, secondFactionId, StringComparison.Ordinal))
            {
                return string.Equals(
                    firstFactionId,
                    WorldNeutralId,
                    StringComparison.Ordinal)
                    ? CombatRelation.Neutral
                    : CombatRelation.Ally;
            }

            if (string.Equals(firstFactionId, WorldNeutralId, StringComparison.Ordinal)
                || string.Equals(secondFactionId, WorldNeutralId, StringComparison.Ordinal))
            {
                return CombatRelation.Neutral;
            }

            return CombatRelation.Hostile;
        }

        public static bool MatchesPolicy(
            CombatRelation relation,
            bool isSelf,
            CombatTargetPolicy policy)
        {
            return policy switch
            {
                CombatTargetPolicy.Hostile => !isSelf && relation == CombatRelation.Hostile,
                CombatTargetPolicy.Ally => !isSelf && relation == CombatRelation.Ally,
                CombatTargetPolicy.Self => isSelf,
                CombatTargetPolicy.Any => true,
                _ => false,
            };
        }
    }
}
