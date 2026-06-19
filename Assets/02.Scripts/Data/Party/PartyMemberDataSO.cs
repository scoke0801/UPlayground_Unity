using System;
using System.Collections.Generic;
using UnityEngine;
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
    }
}