using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Enemy
{
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

    /// <summary>
    /// 지상형 EnemyAIController의 행동 설정 전체를 담는 SO.
    /// 기본 전투 수치 + 페이즈 배열을 포함한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorData", menuName = "UPlayGround/적/Behavior")]
    public class EnemyBehaviorSO : ScriptableObject
    {
        [Header("Behavior Tree")]
        [Tooltip("이 몬스터의 행동을 결정할 BT Asset. EnemyAIController가 런타임에 BehaviorTreeRunner로 주입한다.")]
        public ScriptableObject behaviorTree;

        [Header("AI 역할")]
        [Tooltip("Intent 점수 보정에 사용하는 역할. 기본값 Melee는 기존 동작과 최대한 유사한 중립 보정이다.")]
        public EnemyAIRole aiRole = EnemyAIRole.Melee;

        [Header("Intent Weights")]
        [Tooltip("Intent 점수 계산에 사용할 가중치 SO. null이면 레거시 하드코딩 경로로 폴백 (기존 동작 유지)")]
        public EnemyIntentWeightsSO intentWeights;

        [Header("Combat Strategy")]
        [Tooltip("Intent 성향과 Ability 전술 태그·반복 정책. null이면 기존 설정으로 동작합니다.")]
        public EnemyCombatStrategySO combatStrategy;

        [Header("Ability Tag Trigger")]
        [Tooltip("피격 상태 전환을 태그 트리거 Ability에 맡깁니다. 검증 전에는 false로 유지합니다.")]
        public bool useTagTriggeredHitReaction;

        [Header("전투 거리")]
        public float optimalCombatDistance  = 2.5f;
        public float minCombatDistance      = 1.5f;
        public bool  maintainDistance       = true;

        [Header("Chase 정지 거리")]
        [Tooltip("이 거리 이하가 되면 Chase 이동을 멈추고 BT의 행동 결정을 기다린다.")]
        public float chaseStopDistance     = 2.0f;
        [Tooltip("이 거리 이하로 겹치면 강제 Retreat. Attack Active 중에는 Brain에서 무시.")]
        public float personalSpaceDistance = 0.8f;

        [Header("공격 후 기본 행동 확률 (Phase 미적용 시)")]
        [Range(0f, 1f)] public float continueAttackChance = 0.3f;
        [Range(0f, 1f)] public float guardChance          = 0.25f;
        [Range(0f, 1f)] public float retreatChance        = 0.2f;

        [Header("이동")]
        public float chaseSpeedMultiplier = 1.2f;
        public float circleDuration       = 2.5f;
        public float guardDuration        = 1.5f;
        public float retreatDistance      = 3.0f;

        [Header("그룹 템포")]
        [Tooltip("이 몬스터가 공격을 끝낸 뒤 적용할 그룹 breather 시간. 음수면 그룹 기본값을 사용")]
        public float breatherDurationOverride = -1f;

        [Header("순찰")]
        public bool  enablePatrol  = true;
        public float patrolRadius  = 5f;
        public float patrolWaitTime = 2f;

        [Header("페이즈 (HP threshold 내림차순 정렬)")]
        public BehaviorPhase[] phases = System.Array.Empty<BehaviorPhase>();
    }
}
