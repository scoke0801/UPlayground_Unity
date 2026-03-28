using UnityEngine;

namespace UPlayGround.Data.Enemy
{
    /// <summary>
    /// 비행 몬스터 State들의 튜닝 값을 모아둔 SO.
    /// EnemyFlyingBrain이 참조하고, 각 State가 Brain을 통해 접근한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/Enemy/Flying Settings", fileName = "EnemyFlyingSettings")]
    public class EnemyFlyingSettingsSO : ScriptableObject
    {
        [Header("── TakeOff ──")]
        [Tooltip("이륙 모션 허용 시간 (초과 시 강제 전환)")]
        public float takeOffDuration = 0.7f;
        [Tooltip("상승 스프링 계수 (높을수록 빠르게 목표 고도 도달)")]
        public float ascentSpringK = 5f;
        [Tooltip("상승 속도 하한")]
        public float ascentSpeedMin = 2f;
        [Tooltip("상승 속도 상한")]
        public float ascentSpeedMax = 16f;

        [Header("── AirCircle ──")]
        [Tooltip("선회 진입 후 첫 발사까지 대기")]
        public float firstShotDelay = 0.5f;
        [Tooltip("발사 간 대기")]
        public float shotInterval = 0.8f;
        [Tooltip("공격 모션 타임아웃 (이벤트 미발화 대비)")]
        public float attackMotionTimeout = 3.0f;
        [Tooltip("공중 최대 체류 시간 — 랜덤 범위 하한")]
        public float maxAirStayMin = 4.0f;
        [Tooltip("공중 최대 체류 시간 — 랜덤 범위 상한")]
        public float maxAirStayMax = 8.0f;
        [Tooltip("선회 방향 전환 최소 간격")]
        public float dirChangeTimeMin = 1.5f;
        [Tooltip("선회 방향 전환 최대 간격")]
        public float dirChangeTimeMax = 3.5f;
        [Tooltip("고도 랜덤 편차 (Brain.AirHoverHeight ± 이 값)")]
        public float hoverHeightVariance = 1.5f;

        [Header("── Dive ──")]
        [Tooltip("텔레그래핑(날개 접기) 시간")]
        public float diveTelegraphDuration = 0.7f;
        [Tooltip("착지 후딜 — 플레이어 반격 창")]
        public float diveRecoveryDuration = 1.0f;
        [Tooltip("Approach 목표: 타겟 전방 이 거리에서 내려찍기 (0=머리 위)")]
        public float diveApproachOffset = 3.0f;
        [Tooltip("Approach 도달 판정 수평 거리")]
        public float diveApproachArrivalDist = 2.5f;
        [Tooltip("Approach 타임아웃")]
        public float diveApproachTimeout = 3.0f;

        [Header("── Land (일반 착지) ──")]
        [Tooltip("착지 모션 대기 시간")]
        public float landMotionDuration = 0.8f;
        [Tooltip("부드러운 하강 속도")]
        public float landDescentSpeed = 6f;
        [Tooltip("하강 중 타겟 방향 수평 접근 속도")]
        public float landApproachSpeed = 2f;
        [Tooltip("하강 타임아웃 (안전장치)")]
        public float landMaxDescentTime = 5f;

        [Header("── GroundAttack ──")]
        [Tooltip("공격 모션 타임아웃")]
        public float groundAttackMotionTimeout = 3.0f;
    }
}
