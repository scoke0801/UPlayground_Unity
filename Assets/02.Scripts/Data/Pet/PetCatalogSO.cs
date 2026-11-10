using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Pet
{
    /// <summary>신규 게임의 해금 정책과 동행 가능한 콘텐츠 목록.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Catalog")]
    public sealed class PetCatalogSO : ScriptableObject
    {
        public List<PetDefinitionSO> definitions = new();
        public List<string> initiallyUnlocked = new();
        public string defaultPetId;

        /// <summary>표시 이름과 독립된 영속 ID로 콘텐츠를 찾는다.</summary>
        public PetDefinitionSO Find(string id)
        {
            for (int i = 0; i < definitions.Count; i++)
                if (definitions[i] != null && definitions[i].petId == id) return definitions[i];
            return null;
        }
    }
}
