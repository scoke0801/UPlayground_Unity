using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Story
{
    /// <summary>한 주인공과 그 주인공에게 대응하는 최종 보스 정의를 묶습니다.</summary>
    [Serializable]
    public sealed class AlternateSelfVariantDefinition
    {
        [SerializeField] private CharacterActorType _storyProtagonist;
        [SerializeField] private ActorDefinitionSO _bossActor;

        public CharacterActorType StoryProtagonist => _storyProtagonist;
        public ActorDefinitionSO BossActor => _bossActor;
    }

    /// <summary>새 게임 주인공을 제1장 최종 보스 액터 정의에 연결합니다.</summary>
    [CreateAssetMenu(
        fileName = "AlternateSelfVariantSet_",
        menuName = "UPlayGround/Story/Alternate Self Variant Set")]
    public sealed class AlternateSelfVariantSetSO : ScriptableObject
    {
        [SerializeField]
        private List<AlternateSelfVariantDefinition> _variants = new();

        public IReadOnlyList<AlternateSelfVariantDefinition> Variants =>
            _variants;

        /// <summary>주인공에 정확히 하나의 유효한 매핑이 있을 때만 보스 정의를 반환합니다.</summary>
        public bool TryGetVariant(
            CharacterActorType storyProtagonist,
            out ActorDefinitionSO bossActor)
        {
            bossActor = null;
            if (storyProtagonist == CharacterActorType.None
                || _variants == null)
            {
                return false;
            }

            int matchCount = 0;
            for (int i = 0; i < _variants.Count; i++)
            {
                AlternateSelfVariantDefinition variant = _variants[i];
                if (variant == null
                    || variant.StoryProtagonist != storyProtagonist)
                {
                    continue;
                }

                matchCount++;
                bossActor = variant.BossActor;
            }

            if (matchCount == 1 && bossActor != null)
                return true;

            bossActor = null;
            return false;
        }
    }
}
