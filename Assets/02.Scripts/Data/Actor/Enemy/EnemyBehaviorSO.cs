using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 지상형 EnemyAIController의 행동 설정 전체를 담는 SO.
    /// 기본 전투 수치 + 페이즈 배열을 포함한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorData", menuName = "UPlayGround/Enemy/Behavior Data")]
    public class EnemyBehaviorSO : ScriptableObject
    {
        [Header("Behavior Tree")]
        [Tooltip("이 몬스터의 행동을 결정할 BT Asset. EnemyAIController가 런타임에 BehaviorTreeRunner로 주입한다.")]
        public BehaviorTreeAsset behaviorTree;

        [Header("AI 역할")]
        [Tooltip("Intent 점수 보정에 사용하는 역할. 기본값 Melee는 기존 동작과 최대한 유사한 중립 보정이다.")]
        public EnemyAIRole aiRole = EnemyAIRole.Melee;

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

        [Header("순찰")]
        public bool  enablePatrol  = true;
        public float patrolRadius  = 5f;
        public float patrolWaitTime = 2f;

        [Header("페이즈 (HP threshold 내림차순 정렬)")]
        public BehaviorPhase[] phases = System.Array.Empty<BehaviorPhase>();
    }
}
