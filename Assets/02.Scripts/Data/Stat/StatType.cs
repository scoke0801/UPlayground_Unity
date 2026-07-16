namespace UPlayGround.Data.Stat
{
    /// <summary>
    /// 모든 GameActor가 보유 가능한 스탯의 종류.
    /// ActorStatSO와 ActorStatContainer가 이 enum을 키로 사용한다.
    /// 추가 시 ActorStatSO._defaults / ActorStatSOEditor의 카테고리/슬라이더 범위도 함께 업데이트할 것.
    /// </summary>
    public enum StatType
    {
        // ── 생존 ──────────────────────────────
        MaxHealth,          // 최대 체력
        HealthRegenRate,    // 초당 자연 회복량 (0이면 미적용)

        // ── 전투 ──────────────────────────────
        AttackPower,        // 공격력 배율 (1.0 = 기본, HitPhaseData.damage에 곱해짐)
        Defense,            // 방어 계수 (0~1, 받는 피해 감소율)
        CritRate,           // 치명타 확률 (0.0~1.0)
        CritMultiplier,     // 치명타 데미지 배율 (기본 1.5)

        // ── 이동 ──────────────────────────────
        MoveSpeed,          // 이동속도 배율 (1.0 = 기본)
        DashDistance,       // 대시 거리 배율

        // ── 강인도 ────────────────────────────
        MaxPoise,           // 최대 Poise
        PoiseRecoveryRate,  // 초당 Poise 회복량
        PoiseRecoveryDelay, // Poise 회복 대기 시간

        // ── 스킬 ──────────────────────────────
        SkillGaugeRate,     // 스킬 게이지 충전 속도 배율
        InvincibleDuration, // 무적 시간 배율

        // ── 생활 ──────────────────────────────
        GatheringPower,     // 채집력 (채광/벌목/채집 1회 타격량)
        AttackSpeed,        // 공격 애니메이션 재생 속도 배율 (1.0 = 기본, 직렬화 호환을 위해 끝에 추가)
    }
}
