using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Combat
{
    [Serializable]
    public sealed class CombatFactionRelationEntry
    {
        public CombatFactionSO first;
        public CombatFactionSO second;
        public CombatRelation relation = CombatRelation.Hostile;
    }

    /// <summary>진영 쌍 관계의 데이터 단일 소스. 미지정 쌍은 공용 기본 규칙을 따른다.</summary>
    [CreateAssetMenu(
        fileName = "CombatFactionRelations",
        menuName = "UPlayGround/Combat/Faction Relations")]
    public sealed class CombatFactionRelationTableSO : ScriptableObject
    {
        [SerializeField] private List<CombatFactionRelationEntry> _relations = new();

        public CombatRelation Resolve(string firstFactionId, string secondFactionId)
        {
            for (int i = 0; i < _relations.Count; i++)
            {
                CombatFactionRelationEntry entry = _relations[i];
                if (entry?.first == null || entry.second == null)
                    continue;

                bool direct = string.Equals(
                                  entry.first.FactionId,
                                  firstFactionId,
                                  StringComparison.Ordinal)
                              && string.Equals(
                                  entry.second.FactionId,
                                  secondFactionId,
                                  StringComparison.Ordinal);
                bool reverse = string.Equals(
                                   entry.first.FactionId,
                                   secondFactionId,
                                   StringComparison.Ordinal)
                               && string.Equals(
                                   entry.second.FactionId,
                                   firstFactionId,
                                   StringComparison.Ordinal);
                if (direct || reverse)
                    return entry.relation;
            }

            return CombatFactionRules.ResolveDefaultRelation(
                firstFactionId,
                secondFactionId);
        }
    }
}
