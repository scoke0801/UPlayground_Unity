using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(
        fileName = "PassiveSet_",
        menuName = "UPlayGround/Ability/Character Passive Set")]
    public sealed class CharacterPassiveSetSO : ScriptableObject
    {
        public const int MaxCharacterSelectRepresentatives = 2;

        public CharacterActorType characterType;

        [Tooltip("캐릭터가 실제로 보유하는 전체 패시브. 개수 제한 없음.")]
        public List<PassiveAbilitySO> passives = new();

        [Tooltip("UI_CharacterSelect에 표시할 대표 패시브. 전체 목록에 포함된 항목만 최대 2개.")]
        public List<PassiveAbilitySO> characterSelectRepresentatives = new();

        public IEnumerable<PassiveAbilitySO> EnumerateCharacterSelectRepresentatives()
        {
            if (characterSelectRepresentatives == null)
                yield break;

            var seen = new HashSet<PassiveAbilitySO>();
            int emitted = 0;
            for (int i = 0;
                 i < characterSelectRepresentatives.Count
                 && emitted < MaxCharacterSelectRepresentatives;
                 i++)
            {
                PassiveAbilitySO passive = characterSelectRepresentatives[i];
                if (passive == null
                    || passives == null
                    || !passives.Contains(passive)
                    || !seen.Add(passive))
                {
                    continue;
                }

                emitted++;
                yield return passive;
            }
        }
    }
}
