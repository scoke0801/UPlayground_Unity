using System;

namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// 플레이어 동작(공격/차지 등)을 캔슬할 수 있는 입력 액션 마스크.
    /// 공격 데이터(PlayerAttackInfo / ChargeStageData)에서 "이 동작을 어떤 입력으로 캔슬할 수 있는가"를
    /// 데이터로 지정한다. 새 캔슬 액션 추가 = 여기 플래그 1개 + PlayerInterruptResolver 매핑 1줄.
    ///
    /// 기존 단일 bool(canBeInterrupted = 1, 캔슬 허용)의 동작은 Dodge|Jump|Dash(=7)와 동일하다.
    ///
    /// 캔슬 허용 "구간"은 마스크가 아니라 캔슬 윈도우(PlayerCombat.IsCancelWindowOpen,
    /// 현재 규칙: 히트박스 콜리전 비활성 구간)가 결정한다 — 액티브 히트 중엔 캔슬 불가.
    ///
    /// 공격타입 캔슬 사용 관례: 서로 "다른 타입"으로의 전환(약공→강공/스킬)에 사용한다.
    /// 같은 타입 연계(약공→약공)는 기존 ComboWindow(ComboWindowEvent)를 쓴다.
    /// 둘 다 성립하면 캔슬이 우선하며, TryEnter 경로는 콤보 인덱스를 이어가지 않고
    /// 새 공격으로 진입할 수 있다(체감은 플레이로 확인 권장).
    /// </summary>
    [Flags]
    public enum PlayerInterruptAction
    {
        None        = 0,
        Dodge       = 1 << 0, // 1 — 회피로 캔슬
        Jump        = 1 << 1, // 2 — 점프(Airborne)로 캔슬
        Dash        = 1 << 2, // 4 — 대시로 캔슬 (조건부 전환)
        Guard       = 1 << 3, // 8 — 가드로 캔슬
        LightAttack = 1 << 4, // 16 — 약공 입력으로 캔슬(다른 공격으로 전환)
        HeavyAttack = 1 << 5, // 32 — 강공 입력으로 캔슬
        Skill       = 1 << 6, // 64 — 스킬 입력으로 캔슬(게이지 충분 시)
    }
}
