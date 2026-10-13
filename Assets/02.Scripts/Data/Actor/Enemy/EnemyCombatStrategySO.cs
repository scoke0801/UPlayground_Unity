using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Enemy
{
    /// <summary>적의 행동 의도와 Ability 선택 정책을 보관한다.</summary>
    [CreateAssetMenu(
        fileName = "EnemyCombatStrategy",
        menuName = "UPlayGround/적/Combat Strategy")]
    public sealed class EnemyCombatStrategySO : ScriptableObject
    {
        [Header("Intent")]
        [Tooltip("이 전략의 Intent 점수 프로파일. null이면 EnemyBehaviorSO.intentWeights를 사용합니다.")]
        public EnemyIntentWeightsSO intentWeights;

        [Header("Ability Tactical Tags")]
        [Tooltip("후보 점수에 가산할 GameplayAbilitySO.abilityTagIds")]
        public List<GameplayTag> preferredAbilityTags = new();
        [Tooltip("후보에서 제외할 GameplayAbilitySO.abilityTagIds")]
        public List<GameplayTag> blockedAbilityTags = new();
        [Min(0f)] public float preferredTagScoreBonus = 2f;

        [Header("반복·Commitment")]
        [Range(0f, 1f)] public float repeatedAbilityScoreMultiplier = 0.45f;
        [Min(0)] public int maxConsecutiveSameAbility = 2;
        [Min(0f)] public float minimumCommitmentSeconds = 0.15f;

        [Header("그룹")]
        [Range(0f, 2f)] public float groupPressureMultiplier = 1f;
        [Range(0f, 2f)] public float groupBreatherMultiplier = 1f;
    }

}
