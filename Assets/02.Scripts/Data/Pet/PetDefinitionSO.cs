using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Data.Pet
{
    /// <summary>파티 동행 펫의 이동·외형 원본. 액터와 모션은 기존 정의에서 참조한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Definition")]
    public sealed class PetDefinitionSO : ScriptableObject
    {
        public string petId;
        public string modelId = "Monkey";
        public GameObject previewPrefab;
        public AnimationClip previewIdle;
        public UPlayGround.Animation.MotionSetAsset seatedMotion;
        [Header("등장 연출")]
        public string arrivalFxKey;
        [Min(.01f)] public float arrivalFxScale = .16f;
        [Min(.01f)] public float arrivalFxDuration = 2f;
        public Vector3 arrivalFxOffset = new(0f, .12f, 0f);
        public ActorDefinitionSO actor;
        public PetFollowProfileSO followProfile;
        public PetLootProfileSO lootProfile;
        public List<PetAppearance> appearances = new();

        /// <summary>누락 외형은 같은 종의 기본 외형으로 표시한다.</summary>
        public PetAppearance FindAppearance(string id)
        {
            for (int i = 0; i < appearances.Count; i++)
                if (appearances[i] != null && appearances[i].appearanceId == id) return appearances[i];
            return appearances.Count > 0 ? appearances[0] : null;
        }
    }

    [Serializable]
    public sealed class PetAppearance
    {
        public string appearanceId;
        public Sprite portrait;
        public Material body;
        public Material face;
    }
}
