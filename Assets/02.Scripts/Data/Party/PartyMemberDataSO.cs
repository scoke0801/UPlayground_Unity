using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 파티 멤버 개별 인원의 정보를 관리하는 SO
    /// PartyConfigSO 에 배치해서 쓰자
    /// </summary>
    [CreateAssetMenu(fileName = "PartyMemberData", menuName = "UPlayGround/파티/Member Data")]
    public class PartyMemberDataSO : ScriptableObject
    {
        [Serializable]
        public struct PartyMemberSpriteData
        {
            public CharacterActorType type;
            public Sprite fullBodySprite;
            public Sprite headSprite;

            public Sprite weaponIcon;
            public string name;

            [Tooltip("무기 이름 (예: 카타나)")]
            public string weaponName;
            [Tooltip("플레이어블 캐릭터의 고유 전투 속성")]
            public CombatElement combatElement;
            [Tooltip("등급(별 개수). 1~5")]
            [Range(1, 5)] public int rarity;
            [Tooltip("역할/특성 태그")]
            public PartyRole role;

            // [TODO] 쓸 일 있으면 쓰자
            public Sprite angrySprite;
            public Sprite happySprite;
            public Sprite blueSprite;

        }
        
        public List<PartyMemberSpriteData> sprites = new();

        public Sprite GetFullBodySprite(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var data = sprites[index];
                if (data.type != type) continue;

                return data.fullBodySprite;
            }

            return null;
        }
        
        public Sprite GetHeadSprite(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var data = sprites[index];
                if (data.type != type) continue;

                return data.headSprite;
            }

            return null;
        }

        public Sprite GetWeaponIcon(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var data = sprites[index];
                if (data.type != type) continue;

                return data.weaponIcon;
            }

            return null;
        }

        public string GetName(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                var data = sprites[index];
                if (data.type != type) continue;

                return data.name;
            }

            return string.Empty;
        }

        public string GetWeaponName(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                if (sprites[index].type != type) continue;
                return sprites[index].weaponName;
            }
            return string.Empty;
        }

        public CombatElement GetCombatElement(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                if (sprites[index].type != type) continue;
                return sprites[index].combatElement;
            }
            return CombatElement.None;
        }

        /// <summary> 등급(별 개수, 1~5). 미설정이면 1. </summary>
        public int GetRarity(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                if (sprites[index].type != type) continue;
                return Mathf.Clamp(sprites[index].rarity, 1, 5);
            }
            return 1;
        }

        public PartyRole GetRole(CharacterActorType type)
        {
            for (var index = 0; index < sprites.Count; index++)
            {
                if (sprites[index].type != type) continue;
                return sprites[index].role;
            }
            return PartyRole.Balanced;
        }
    }
}
