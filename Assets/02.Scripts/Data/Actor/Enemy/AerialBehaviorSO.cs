using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 공중 행동 전용 설정 파라미터.
    /// BehaviorPhase.overrideAerial = true 시 해당 페이즈 값으로 덮어쓴다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/Enemy/Aerial Behavior", fileName = "AerialBehavior")]
    public class AerialBehaviorSO : ScriptableObject
    {
        [Header("체공 고도")]
        [Tooltip("체공 최소 고도 (지면 기준)")]
        public float minHoverHeight = 3f;
        [Tooltip("체공 최대 고도. 유저 원거리 사정거리 이내로 설정 필수")]
        public float maxHoverHeight = 7f;

        [Header("이동")]
        public float hoverMoveSpeed  = 4.5f;
        [Tooltip("공중 Idle 시 플레이어 주변 선회 반경")]
        public float hoverIdleRadius = 5f;
        [Tooltip("고도 유지 상승 속도")]
        public float hoverAscentSpeed  = 3f;
        [Tooltip("고도 유지 하강 속도")]
        public float hoverDescentSpeed = 2f;
        [Tooltip("고도 수렴 스프링 계수")]
        public float springK  = 6f;
        [Tooltip("고도 수렴 댐핑 계수")]
        public float damping  = 2f;

        [Header("이륙 조건")]
        [Tooltip("이륙 확률 (0~1). 페이즈 오버라이드 가능")]
        [Range(0f, 1f)] public float takeOffChance = 0.4f;
        [Tooltip("착지 후 재이륙까지 최소 쿨다운 (초)")]
        public float takeOffCooldown = 8f;
        [Tooltip("이 HP 비율 이하일 때만 이륙 가능 (1 = 항상)")]
        [Range(0f, 1f)] public float aerialHpThreshold = 1f;

        [Header("체공 한계")]
        [Tooltip("최대 체공 시간. 초과 시 강제 착지")]
        public float aerialDuration = 12f;
        [Tooltip("체공 중 사용 가능한 공중 스킬 최대 횟수")]
        public int   maxAerialAttackCount = 3;

        [Header("착지 충격")]
        [Tooltip("착지 충격 판정 반경")]
        public float landingImpactRadius = 2.5f;
        [Tooltip("착지 충격 하강 속도")]
        public float landDescentSpeed    = 8f;
    }
}
