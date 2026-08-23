using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>플레이어 캐릭터 정의의 Addressable 위치를 보관하는 경량 카탈로그.</summary>
    [CreateAssetMenu(
        fileName = "PlayerCharacterCatalog",
        menuName = "UPlayGround/파티/Player Character Catalog")]
    public sealed class PlayerCharacterCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public CharacterActorType characterType;
            [Tooltip("PlayerCharacterDefinitionSO의 Addressable 주소입니다.")]
            public string definitionAddress;
        }

        public List<Entry> entries = new();

        /// <summary>캐릭터 타입에 대응하는 정의 주소를 반환한다.</summary>
        public bool TryGetDefinitionAddress(
            CharacterActorType characterType,
            out string definitionAddress)
        {
            for (int i = 0; i < (entries?.Count ?? 0); i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.characterType != characterType)
                    continue;

                definitionAddress = entry.definitionAddress?.Trim();
                return !string.IsNullOrEmpty(definitionAddress);
            }

            definitionAddress = null;
            return false;
        }
    }

}
