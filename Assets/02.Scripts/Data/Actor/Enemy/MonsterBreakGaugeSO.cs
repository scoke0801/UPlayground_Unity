using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Enemy
{
    [System.Serializable]
    public class MonsterBreakGradePolicy
    {
        [Min(0.01f)] public float weakGaugeMultiplier = 0.5f;
        [Min(0.01f)] public float normalGaugeMultiplier = 1f;
        [Min(0.01f)] public float eliteGaugeMultiplier = 1.5f;
        [Min(0.01f)] public float bossGaugeMultiplier = 2.5f;

        public float GetGaugeMultiplier(MonsterActorGrade grade)
        {
            return grade switch
            {
                MonsterActorGrade.Weak => weakGaugeMultiplier,
                MonsterActorGrade.Elite => eliteGaugeMultiplier,
                MonsterActorGrade.Boss => bossGaugeMultiplier,
                _ => normalGaugeMultiplier,
            };
        }
    }

    [CreateAssetMenu(fileName = "MonsterBreakGauge", menuName = "UPlayGround/적/Break Gauge")]
    public class MonsterBreakGaugeSO : ScriptableObject
    {
        [Header("Usage")]
        public bool useBreakGauge = true;
        public bool allowRepeatBreak = true;

        [Header("Gauge")]
        [Min(1f)] public float maxGauge = 100f;
        [Range(0f, 1f)] public float breakResist = 0f;
        [Tooltip("특수 브레이크 공격이 적중한 뒤 다시 브레이크 게이지를 누적할 수 있을 때까지의 공통 쿨타임. 0이면 런타임 기본값을 사용합니다.")]
        [Min(0f)] public float repeatBreakCooldown = 5f;

        [Header("Exposed")]
        [Min(0.1f)] public float exposedDuration = 4f;
        [Tooltip("Break 노출 중 받는 피해 배율. 통합 취약 배율 채널의 한 입력으로, 리액션 상태(Stun/Knockdown 등) 배율과 동시 성립 시 더 큰 쪽 하나만 적용된다(max-wins).")]
        [Min(0f)] public float damageTakenMultiplierWhileExposed = 1.15f;
        [Tooltip("노출 시간이 끝났을 때 이미 깎인 비율. 0.25면 잔량 75%에서 재시작")]
        [Range(0f, 1f)] public float resetGaugeRatioOnExpire = 0.25f;
        [Tooltip("특수 브레이크 공격으로 소비했을 때 이미 깎인 비율. 0이면 잔량 100%로 재시작")]
        [Range(0f, 1f)] public float resetGaugeRatioOnSpecialAttack = 0f;

        [Header("Grade")]
        public MonsterBreakGradePolicy gradePolicy = new MonsterBreakGradePolicy();
    }
}
