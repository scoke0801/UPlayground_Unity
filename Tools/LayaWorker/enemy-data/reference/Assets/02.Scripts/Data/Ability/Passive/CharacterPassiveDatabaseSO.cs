using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Ability
{
    [CreateAssetMenu(
        fileName = "CharacterPassiveDatabase",
        menuName = "UPlayGround/Ability/Character Passive Database")]
    public sealed class CharacterPassiveDatabaseSO : ScriptableObject
    {
        public List<CharacterPassiveSetSO> entries = new();

        public CharacterPassiveSetSO Get(CharacterActorType type)
        {
            if (type == CharacterActorType.None || entries == null)
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                CharacterPassiveSetSO entry = entries[i];
                if (entry != null && entry.characterType == type)
                    return entry;
            }
            return null;
        }
    }
}
