# 몬스터 브레이크 / 행동 불능 / 특수공격 시스템 기획 문서

> 작성일: 2026-05-18  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 레퍼런스: 명조 `Vibration Strength` / `Off-Tune` 계열 브레이크 기믹

---

## 개요

플레이어와 몬스터의 피격 반응, 행동 불능 상태, 몬스터 브레이크 게이지, 브레이크 특수공격을 하나의 전투 리액션 체계로 정리한 기획 문서.

현재 프로젝트에는 이미 다음 기반이 있다.

- `AttackReactionType`: `Light`, `Hit`, `Heavy`, `KnockBack`, `Stun`, `Pull`, `Airborne`, `Knockdown`, `Grab`
- `PoiseStat`: 몬스터가 피격 반응을 할지 버틸지 결정하는 강인도 컴포넌트
- `PlayerHitState`, `PlayerGrabbedState`, `PlayerGuardBreakState`
- `EnemyHitState`, `EnemyAirborneState`, `EnemyGrabbedState`
- `MonsterActor.OnTakeFinishAttack()`, `PlayerCombat.FindFinishableTarget()`

따라서 새 시스템은 완전 신규 체계가 아니라 기존 피격/강인도/피니시 공격 구조를 확장한다.

핵심 방향은 다음과 같다.

- `Poise`는 경직/행동 불능 진입 판정 축으로 유지한다.
- `Break Gauge`는 몬스터에게 특수공격 기회를 만들기 위한 별도 축으로 추가한다.
- 플레이어 행동 불능은 짧고 명확하게 제한한다.
- 몬스터 행동 불능은 플레이어 공격 보상과 공략 리듬을 만들도록 적극 활용한다.
- 기존 HP 조건 피니시 공격과 브레이크 특수공격은 입력/조건/피해 목적을 분리한다.

---

## 설계 목표

- 기존 `PoiseStat`과 충돌하지 않는 브레이크 게이지를 설계한다.
- 행동 불능 상태를 플레이어/몬스터 공통 데이터로 표현하되, 실제 State 클래스는 액터별로 분리한다.
- 몬스터 브레이크는 자동 처형이 아니라 플레이어가 입력해서 발동하는 전투 중 특수공격 기회로 만든다.
- 몬스터 등급에 따라 경직, 스턴, 다운, 공중 띄우기, 브레이크 허용 범위를 다르게 둔다.
- 플레이어는 조작권 박탈을 최소화하고, 몬스터는 피격 피드백과 공략 보상을 강화한다.
- UI, AI, 상태 머신, MotionSet 이벤트가 명확한 책임을 갖도록 한다.

---

## 핵심 용어

| 용어 | 설명 |
|------|------|
| `Reaction` | 공격 적중 후 대상에게 적용되는 피격 반응 |
| `Control Lock` | 이동/공격/회피/가드/교체 등 조작 제한 범위 |
| `Poise` | 피격 반응을 버틸 수 있는 강인도 |
| `Poise Break` | `PoiseStat`이 0 이하가 되어 경직/행동 불능 상태 진입이 허용된 상태 |
| `Break Gauge` | 몬스터가 피격될 때 누적되는 특수공격 준비 게이지 |
| `Break Damage` | 공격이 브레이크 게이지에 주는 누적량 |
| `BreakExposed` | 게이지가 가득 차 특수공격 입력을 받을 수 있는 취약 상태 |
| `Special Break Attack` | 브레이크 노출 중 플레이어가 발동하는 특수공격 |
| `Groggy` | 몬스터가 큰 공격/처형/특수공격을 받을 수 있는 장시간 취약 상태 |
| `Break Lock` | 특수공격 연출 중 플레이어/몬스터/AI/카메라를 잠그는 상태 |

---

## 현재 구조

```
AttackData / HitPhaseData
    ├── damage
    ├── poiseDamage
    ├── reactionType
    ├── pullForce / airborneForce / knockbackForce / knockbackDrag
    └── grabDuration / victimForcedAnimKey

MonsterActor.TakeDamage()
    ├── HP 데미지 처리
    ├── PoiseStat.TakePoiseDamage()
    ├── EnemyTacticalMemory.NotifyTookDamage()
    ├── AttackReactionType별 물리 힘 적용
    └── EnemyHitState / EnemyAirborneState / EnemyGrabbedState 전환

PlayerActor.OnDamaged()
    ├── 슈퍼아머 / 피격 반응 억제 확인
    ├── AttackReactionType별 물리 힘 적용
    ├── PlayerHitState / PlayerAirborneState / PlayerGrabbedState 전환
    └── 카메라 쉐이크 / FX / 피격 플래시
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Enum/AttackInfo.cs` | `AttackReactionType`, `AttackKind` 정의 |
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `HitPhaseData`, `AttackData`, 공격별 반응 데이터 |
| `Assets/02.Scripts/GameActor/Component/Common/PoiseStat.cs` | 강인도 런타임 컴포넌트 |
| `Assets/02.Scripts/Data/Actor/Enemy/PoiseSO.cs` | 강인도 데이터 |
| `Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs` | 몬스터 데미지/피격/사망 처리 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | 플레이어 데미지/피격/사망 처리 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerHitState.cs` | 플레이어 기본 피격 경직, 캔슬 윈도우 처리 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerGrabbedState.cs` | 플레이어 잡힘 행동 불능 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerGuardBreakState.cs` | 플레이어 가드 브레이크 행동 불능 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyHitState.cs` | 몬스터 기본 피격 경직 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyAirborneState.cs` | 몬스터 공중 행동 불능 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyGrabbedState.cs` | 몬스터 잡힘 행동 불능 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyTacticalMemory.cs` | 최근 피격/강한 피격/Poise Break 기억 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 피니시 타겟 탐색, 공격 데이터 생성 후보 |
| `Assets/02.Scripts/Manager/Handler/Combat/GameHitStopHandler.cs` | 특수공격 적중 연출 강화 후보 |
| `Assets/docs/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md` | 카메라/모션/입력 잠금형 연출 설계 재사용 후보 |

---

## 반응 계층

행동 불능은 `AttackReactionType`을 기준으로 분류한다.

| 반응 | 설명 | 플레이어 | 몬스터 |
|------|------|----------|--------|
| `None` | 반응 없음. Poise로 버티거나 특수 상태에서 무시 | O | O |
| `Light` | 아주 짧은 피격 플래시/경직 | O | O |
| `Hit` | 일반 피격 경직 | O | O |
| `Heavy` | 긴 경직. 플레이어는 회피 캔슬만 허용 | O | O |
| `KnockBack` | 공격 방향으로 밀림 | O | O |
| `Stun` | 일정 시간 행동 불능 | 제한 | O |
| `Pull` | 공격자 쪽으로 끌림 | O | O |
| `Airborne` | 공중으로 띄워짐 | 제한 | O |
| `Knockdown` | 지면 다운/넘어짐 | 제한 | O |
| `Grab` | 잡힘/구속. 공격자 릴리즈 또는 시간 만료로 해제 | O | O |
| `Groggy` | 몬스터 전용 장시간 취약 상태. 신규 개념 | X | O |
| `BreakExposed` | 브레이크 특수공격 입력 가능 상태. 신규 개념 | X | O |

권장 우선순위:

```text
Death
> Grabbed / SpecialBreakVictim
> Knockdown
> Airborne
> Stun / GuardBreak
> BreakExposed / Groggy
> KnockBack
> Heavy
> Hit
> Light
> None
```

상태 전환을 실제 코드에 넣을 때는 문자열 우선순위보다 `CanTransitionState()`와 현재 State의 `GrantsInvincibility`, `SuppressesHitReaction`, `BlocksBehaviorTree` 정책을 우선한다.

---

## 플레이어 행동 불능 정책

플레이어는 조작권을 잃는 시간이 길면 전투 감각이 나빠진다. 따라서 플레이어 행동 불능은 짧게, 명확하게, 회복 가능하게 설계한다.

| 상태 | 권장 시간 | 정책 |
|------|----------|------|
| `Light` | 0.05~0.15초 | 피격 플래시 중심. 즉시 캔슬 가능 |
| `Hit` | 0.20~0.45초 | 짧은 경직. 일정 시간 후 공격/회피 캔슬 가능 |
| `Heavy` | 0.50~0.80초 | 긴 경직. 회피 캔슬만 허용 |
| `KnockBack` | 0.40~0.90초 | 물리 밀림. 캔슬 불가 또는 착지/종료 후 회피 |
| `Stun` | 1.00~2.50초 | 매우 제한적으로 사용. 보스 패턴/가드 실패 보상 |
| `Airborne` | 0.80~1.50초 | 특수 공격에만 사용. 남발 금지 |
| `Knockdown` | 1.20~2.00초 | 보스 강공격/패턴 실패 시 사용 |
| `Grab` | 패턴별 | 잡기 전용 연출. 탈출/해제 조건 필요 |
| `GuardBreak` | 1.20초 전후 | 현재 `PlayerGuardBreakState`처럼 전 행동 불가 |

### 현재 구현과 개선 방향

`PlayerHitState`는 이미 다음 정책을 갖고 있다.

- `Light`: 즉시 캔슬
- `Hit`: 0.2초 후 공격/회피 캔슬
- `Heavy`: 0.5초 후 회피 캔슬만 허용
- `KnockBack`, `Pull`, `Airborne`, `Knockdown`, `Stun`, `Grab`: 사실상 캔슬 불가

개선 방향:

- `Stun`과 `Knockdown`은 `PlayerHitState` 안에서 긴 Hit로 처리하기보다 전용 State를 추가한다.
- `PlayerKnockdownState`는 다운, 기상, 기상 무적, 기상 회피 버퍼를 분리한다.
- `PlayerStunState`는 가드 브레이크와 구분해 상태 이상/패턴 실패용으로 쓴다.
- `PlayerAirborneState`는 현재 점프/낙하 상태와 피격 띄우기 상태를 구분할 필요가 있다.
- 플레이어 행동 불능 중에도 `InputBuffer`는 유지하고, 회복 가능 타이밍부터 소비한다.

### 플레이어 조작 제한

| 기능 | `Hit` | `Heavy` | `Stun` | `Knockdown` | `Airborne` | `Grab` |
|------|------|---------|--------|-------------|------------|--------|
| 이동 | 제한 | 제한 | X | X | X | X |
| 회전 | 제한 | 제한 | X | X | 제한 | X |
| 공격 | 캔슬 후 O | X | X | X | X | X |
| 회피 | 캔슬 후 O | 캔슬 후 O | 해제용만 | 기상 타이밍 | X | 특수 |
| 가드 | X | X | X | X | X | X |
| 캐릭터 교체 | X | X | X | 제한 | X | X |
| 피격 | O | O | O | 정책 선택 | O | 패턴별 |

권장:

- 다운 직후 0.3~0.5초 피격 무적을 준다.
- 기상 중에는 짧은 무적을 준다.
- 스턴은 보스/엘리트 패턴에만 제한적으로 사용한다.
- 일반 몬스터가 플레이어를 자주 다운시키지 않게 한다.

---

## 몬스터 행동 불능 정책

몬스터 행동 불능은 플레이어 공격의 피드백과 보상 구조다. 등급별로 허용 범위를 다르게 둔다.

| 상태 | 용도 |
|------|------|
| `Light` | 약한 피격 피드백. Poise가 남은 대상은 무시 가능 |
| `Hit` | 기본 피격 경직 |
| `Heavy` | 강공격/차지공격 성공 보상 |
| `KnockBack` | 폭발, 강타, 공간 제어 |
| `Stun` | 패리, 약점 공격, 속성 누적 보상 |
| `Pull` | 플레이어 스킬/흡입/연계 공격 |
| `Airborne` | 소형/중형 대상 공중 콤보 |
| `Knockdown` | 다운 공격, 큰 딜 타이밍 |
| `Grab` | 플레이어 스킬/특수 연출 |
| `Groggy` | 엘리트/보스 공략 보상 |
| `BreakExposed` | 브레이크 특수공격 입력 대기 |

### 등급별 허용 범위

| 등급 | `Hit` | `Stun` | `Knockdown` | `Airborne` | `BreakExposed` |
|------|------|--------|-------------|------------|----------------|
| Weak | O | O | O | O | 선택 |
| Normal | O | O | O | O | O |
| Elite | O | 누적/조건부 | 제한 | 제한 | O |
| Boss | 제한 | `Groggy`로 대체 | 특수 다운만 | X | 페이즈/쿨타임 제한 |

보스는 일반 `Stun`을 그대로 받으면 전투 리듬이 무너진다. 보스는 `Poise Break` 또는 `Break Gauge` 완료 시 일반 스턴 대신 `Groggy` 또는 `BreakExposed`로 진입시키는 편이 좋다.

---

## Poise와 Break Gauge 분리

`PoiseStat`은 몬스터가 피격 반응을 할지 버틸지를 정한다. 새 `Break Gauge`는 플레이어에게 특수공격 기회를 줄지를 정한다.

| 구분 | Poise | Break Gauge |
|------|-------|-------------|
| 목적 | 경직/행동 불능 상태 진입 제어 | 특수공격 기회 생성 |
| 방향 | 데미지를 받으면 감소하고 0에서 Break | 데미지를 받으면 증가하고 최대치에서 노출 |
| 기본 UI | 기존 HP바 보조 게이지 | HP바 하단/외곽 별도 게이지 |
| 회복 | 지연 후 최대치 회복 | 노출 종료/특수공격 후 정책대로 리셋 |
| 데이터 | `PoiseSO` | 신규 `MonsterBreakGaugeSO` |
| 상태 연결 | `EnemyHitState`, `EnemyAirborneState`, `EnemyGrabbedState` | 신규 `EnemyBreakExposedState`, `EnemySpecialBreakVictimState` |

중요한 룰:

```text
Poise가 남아 있음:
- HP 데미지와 Break Gauge 누적은 적용한다.
- 몬스터는 Light/Hit 반응을 무시하거나 아주 짧게만 표현할 수 있다.

Poise가 0 이하:
- AttackReactionType에 따라 실제 행동 불능 상태로 전환한다.
- Heavy, KnockBack, Stun, Airborne, Knockdown, Grab이 의미를 가진다.

Break Gauge가 최대치:
- Poise 상태와 별개로 BreakExposed 진입을 요청한다.
- 단, Death / Grabbed / SpecialBreakVictim 같은 상위 상태가 있으면 대기하거나 무시한다.
```

---

## 브레이크 특수공격 플레이 흐름

```
PlayerCombat 공격 적중
        │
        ▼
MonsterActor.TakeDamage(AttackData)
        │
        ├── HP 데미지 처리
        ├── PoiseStat.TakePoiseDamage()
        ├── Reaction 상태 전환 여부 결정
        └── MonsterBreakGauge.TakeBreakDamage()
                │
                ├── 게이지 미충전: UI 갱신
                └── 게이지 최대치: BreakExposed 진입 요청
                        │
                        ├── AI/공격/이동 일시 정지
                        ├── 특수공격 프롬프트 표시
                        ├── 취약 시간 타이머 시작
                        └── 플레이어 입력 대기
                                │
                                ├── 입력 성공: SpecialBreakAttack 실행
                                └── 시간 만료: 게이지 리셋 후 AI 복귀
```

---

## 데이터 모델

### HitPhaseData 확장

`CombatData.cs`의 `HitPhaseData`에 브레이크 누적량과 행동 불능 지속시간 보정 필드를 추가하는 방향을 권장한다.

```csharp
[Serializable]
public class HitPhaseData
{
    [Header("Damage")]
    public float damage = 10f;
    public float poiseDamage = 30f;
    public float breakDamage = 10f;
    public AttackReactionType reactionType = AttackReactionType.Hit;

    [Header("Reaction")]
    public float reactionDuration = 0f;
    public bool forceReaction = false;

    [Header("Reaction Forces")]
    public float pullForce = 10f;
    public float airborneForce = 8f;
    public float knockBackForce = 10f;
    public float knockBackDrag = 20f;
}
```

`reactionDuration == 0`이면 각 State의 기본 지속시간 또는 애니메이션 길이를 사용한다.

### AttackData 확장

```csharp
public class AttackData
{
    public float damage;
    public float poiseDamage = 30f;
    public float breakDamage = 10f;
    public float reactionDuration = 0f;
    public bool forceReaction = false;
    public bool forceBreakExpose = false;
    public AttackKind attackKind = AttackKind.NormalAttack;
    public AttackReactionType reactionType = AttackReactionType.Hit;
}
```

권장 브레이크 누적 배율:

| 공격 종류 | `breakDamage` 배율 |
|-----------|--------------------|
| 일반 공격 | 1.0 |
| 일반 콤보 마지막 | 1.3~1.5 |
| 강 공격 | 1.5 |
| 차지 공격 | 2.0~3.0 |
| 퍼펙트 도지 반격 | 2.5 |
| 패리 반격 | 3.0 |
| 교체 등장/퇴장 특수공격 | 2.0 |
| 궁극기 | 2.0~4.0, 캐릭터별 조정 |

### MonsterReactionProfileSO

행동 불능 정책을 몬스터 등급/개체별로 데이터화할 때 추가한다.

```csharp
[CreateAssetMenu(fileName = "MonsterReactionProfile", menuName = "UPlayGround/Enemy/Reaction Profile")]
public class MonsterReactionProfileSO : ScriptableObject
{
    public bool allowStun = true;
    public bool allowAirborne = true;
    public bool allowKnockdown = true;
    public bool convertStunToGroggy = false;
    public bool convertKnockdownToHeavyHit = false;
    public float hitDurationScale = 1f;
    public float receivedKnockbackScale = 1f;
}
```

### PlayerReactionProfileSO

플레이어 피격 체감 조정을 데이터화할 때 추가한다.

```csharp
[CreateAssetMenu(fileName = "PlayerReactionProfile", menuName = "UPlayGround/Player/Reaction Profile")]
public class PlayerReactionProfileSO : ScriptableObject
{
    public float lightCancelWindow = 0f;
    public float hitCancelWindow = 0.2f;
    public float heavyDodgeCancelWindow = 0.5f;
    public float knockdownInvincibleTime = 0.4f;
    public float wakeupInvincibleTime = 0.3f;
    public bool allowWakeupDodge = true;
}
```

### MonsterBreakGaugeSO

```csharp
namespace UPlayGround.Data.Enemy
{
    [CreateAssetMenu(fileName = "MonsterBreakGauge", menuName = "UPlayGround/Enemy/Break Gauge")]
    public class MonsterBreakGaugeSO : ScriptableObject
    {
        public bool useBreakGauge = true;
        public float maxGauge = 100f;
        public float breakResist = 0f;
        public float exposedDuration = 4f;
        public float damageTakenMultiplierWhileExposed = 1.15f;
        public float resetGaugeRatioOnExpire = 0.25f;
        public float resetGaugeRatioOnSpecialAttack = 0f;
        public float repeatBreakCooldown = 0f;
        public bool allowRepeatBreak = true;
        public MonsterBreakGradePolicy gradePolicy;
    }
}
```

| 필드 | 설명 |
|------|------|
| `useBreakGauge` | 해당 몬스터가 브레이크 시스템을 사용할지 여부 |
| `maxGauge` | 최대 게이지 |
| `breakResist` | 0~1. 0.3이면 브레이크 누적량 30% 감소 |
| `exposedDuration` | 특수공격 입력 가능 시간 |
| `damageTakenMultiplierWhileExposed` | 노출 중 일반 공격 피해 보너스 |
| `resetGaugeRatioOnExpire` | 입력하지 않고 끝났을 때 남길 게이지 비율 |
| `resetGaugeRatioOnSpecialAttack` | 특수공격 후 남길 게이지 비율 |
| `repeatBreakCooldown` | 반복 브레이크 최소 간격 |
| `allowRepeatBreak` | 한 전투에서 반복 브레이크 허용 여부 |
| `gradePolicy` | 일반/엘리트/보스별 보정 |

---

## 런타임 컴포넌트

### MonsterBreakGauge

`MonsterActor`에 붙는 신규 컴포넌트. 이름은 `PoiseStat`과 역할을 분리하기 위해 `BreakStat`보다 명확한 `MonsterBreakGauge`를 권장한다.

```csharp
public class MonsterBreakGauge : MonoBehaviour
{
    public bool IsExposed { get; }
    public float GaugePercent { get; }

    public event Action<MonsterBreakGauge> OnBreakExposed;
    public event Action<MonsterBreakGauge> OnBreakRecovered;
    public event Action<float, float> OnGaugeChanged;

    public void Init(MonsterBreakGaugeSO data);
    public void TakeBreakDamage(AttackData attackData);
    public void ConsumeBySpecialAttack();
    public void ForceExpose();
    public void RecoverFromExpose(bool consumed);
}
```

책임:

- 브레이크 누적량 계산
- 등급별 보정 적용
- 노출 시간 타이머 관리
- UI 이벤트 발화
- 특수공격 소비/만료 리셋 처리

### ReactionResolver

초기 구현에서는 `MonsterActor.OnDamaged()`와 `PlayerActor.OnDamaged()`에 직접 분기해도 된다. 다만 상태가 늘어나면 공통 판정 전용 클래스를 두는 편이 안전하다.

```csharp
public readonly struct ReactionDecision
{
    public AttackReactionType ReactionType { get; }
    public bool ShouldEnterState { get; }
    public bool ShouldApplyForce { get; }
    public float Duration { get; }
}
```

책임:

- 대상 액터가 플레이어인지 몬스터인지 구분
- Poise Break 여부 반영
- 몬스터 등급별 면역/변환 적용
- 현재 State의 피격 억제 정책 반영
- `AttackReactionType`을 실제 State 전환으로 매핑

### SpecialBreakAttackController

플레이어 측 실행 컨트롤러. 기존 `PlayerCombat`이 너무 커지지 않도록 별도 컴포넌트로 분리한다.

책임:

- 현재 특수공격 가능 타겟 탐색
- 입력 프롬프트 조건 검증
- 캐릭터별 특수공격 MotionSet 선택
- 시전자/타겟 위치 보정
- 카메라/입력/AI/피격 잠금
- 타격 이벤트에서 데미지 적용
- 성공/실패/중단 시 복구

---

## 상태 설계

플레이어와 몬스터는 같은 `AttackReactionType`을 사용하더라도 상태 처리가 다르다. 따라서 데이터는 공통화하고 State 클래스는 분리한다.

### 플레이어 신규 후보

| State | 용도 |
|-------|------|
| `PlayerStunState` | 상태 이상/보스 패턴 실패용 장시간 행동 불능 |
| `PlayerKnockdownState` | 다운, 기상, 기상 무적, 기상 회피 버퍼 |
| `PlayerLaunchHitState` | 피격으로 뜬 상태. 일반 점프/낙하와 분리 |
| `PlayerSpecialBreakAttackState` | 브레이크 특수공격 실행 |

### 몬스터 신규 후보

| State | 용도 |
|-------|------|
| `EnemyStunState` | 일반/엘리트 스턴 |
| `EnemyKnockdownState` | 지상 다운/기상 |
| `EnemyGroggyState` | 엘리트/보스 장시간 취약 |
| `EnemyBreakExposedState` | 특수공격 입력 대기 |
| `EnemySpecialBreakVictimState` | 특수공격 연출 중 피해자 고정 |

### 몬스터 상태 전이

```
Any Combat State
    ├── Poise Break + Reaction
    │       ├── Hit / Heavy / KnockBack → EnemyHitState
    │       ├── Airborne → EnemyAirborneState
    │       ├── Grab → EnemyGrabbedState
    │       ├── Stun → EnemyStunState
    │       └── Knockdown → EnemyKnockdownState
    │
    └── Break Gauge Full
            └── EnemyBreakExposedState
                    ├── SpecialBreakAttack 입력
                    │       └── EnemySpecialBreakVictimState
                    │               ├── HP 0: EnemyDeathState
                    │               └── 생존: EnemyHitState / EnemyIdleState / EnemyChaseState
                    └── 시간 만료
                            └── EnemyIdleState / EnemyChaseState
```

### 플레이어 상태 전이

```
PlayerIdle/GroundMove/Attack 후딜/Guard/Dodge 후딜
    └── SpecialBreakAttack 입력
            └── PlayerSpecialBreakAttackState
                    ├── 완료: PlayerIdleState / PlayerGroundMoveState
                    └── 중단: 안전 복구

Any Player Combat State
    └── 피격
            ├── Light / Hit / Heavy / KnockBack / Pull → PlayerHitState
            ├── Airborne → PlayerLaunchHitState 또는 PlayerAirborneState
            ├── Knockdown → PlayerKnockdownState
            ├── Stun → PlayerStunState
            └── Grab → PlayerGrabbedState
```

---

## 브레이크 특수공격 입력 정책

입력 허용 상태는 처음에는 보수적으로 제한한다.

허용:

- `PlayerIdleState`
- `PlayerGroundMoveState`
- `PlayerDodgeState` 종료 직후
- `PlayerAttackState` 후딜 캔슬 가능 구간

불허:

- `PlayerHitState`
- `PlayerDeathState`
- `PlayerInteractionState`
- `PlayerGrabbedState`
- `PlayerGuardBreakState`
- 궁극기/교체/카메라 시퀀스 잠금 중

같은 키를 쓸 경우 우선순위:

1. 스토리/대화/상호작용 필수 이벤트
2. HP 조건 피니시 공격
3. 브레이크 특수공격
4. 일반 상호작용

전투 감각을 분리하려면 브레이크 특수공격은 별도 키 또는 `LockOn + Attack` 조합이 더 명확하다.

---

## UI 설계

### 몬스터 HP바

기존 `UI_ActorHpBar`에 브레이크 게이지 표시를 추가한다.

권장 표시:

- HP바 아래 얇은 청록/백색 게이지
- Poise 게이지와 시각적으로 구분되는 색상/위치 사용
- 누적 중에는 부드럽게 증가
- 최대치 도달 시 게이지가 깜빡이고 타겟 위 프롬프트 표시
- 노출 중에는 게이지가 시간 제한 타이머처럼 감소
- 보스는 화면 상단 보스 HP바에 더 큰 게이지 사용 가능

### 입력 프롬프트

조건:

- 타겟이 `BreakExposed`
- 플레이어가 타겟과 일정 거리 이내
- 플레이어가 사망/피격/궁극기/교체 연출 중이 아님
- 락온 타겟이 있으면 락온 타겟 우선

표시:

- 키보드: `F` 또는 전용 특수공격 키
- 패드: 대응 버튼
- 위치: 타겟 머리 위 또는 락온 UI 주변

---

## 밸런스 초안

### 플레이어 피격 기준

| 반응 | 일반 몬스터 | 엘리트 | 보스 |
|------|-------------|--------|------|
| `Hit` | 자주 사용 | 자주 사용 | 자주 사용 |
| `Heavy` | 강공격만 | 강공격/스킬 | 주요 패턴 |
| `KnockBack` | 제한 | O | O |
| `Stun` | 거의 금지 | 제한 | 패턴 실패 보상 |
| `Airborne` | 거의 금지 | 제한 | 특정 패턴 |
| `Knockdown` | 거의 금지 | 제한 | 강한 패턴 |
| `Grab` | 전용 몬스터 | 전용 패턴 | 전용 패턴 |

### 몬스터 등급별 브레이크 기본값

| 등급 | maxGauge | exposedDuration | 특수공격 피해 | 반복 |
|------|----------|-----------------|---------------|------|
| Weak | 사용 안 함 또는 40 | 3.0초 | 최대 HP 50~100% | 선택 |
| Normal | 80 | 3.5초 | 최대 HP 25~40% | 허용 |
| Elite | 140 | 4.0초 | 최대 HP 15~25% | 허용 |
| Boss | 250 | 5.0초 | 현재 HP 또는 최대 HP 8~12% | 페이즈당 제한 권장 |

### 누적량 예시

| 행동 | 누적량 |
|------|--------|
| 일반 1타 | 8 |
| 일반 콤보 마지막 | 14 |
| 강 공격 | 18 |
| 차지 1단 | 25 |
| 차지 풀 | 45 |
| 퍼펙트 도지 반격 | 40 |
| 패리 반격 | 55 |
| 교체 특수공격 | 35 |
| 궁극기 주요 타격 | 60 |

---

## 특수공격 연출 정책

### 1차 구현

1차 구현은 과도한 시네마틱보다 안정적인 전투 기능을 우선한다.

- 플레이어가 타겟 앞으로 짧게 보정 이동
- 타겟은 `EnemySpecialBreakVictimState`로 고정
- 플레이어는 캐릭터 공통 특수공격 모션 재생
- MotionSet `CollisionEvent` 타이밍에 데미지 적용
- 짧은 히트스톱, 카메라 쉐이크, 슬로모션 적용
- 종료 후 플레이어는 `Idle` 또는 `GroundMove`, 몬스터는 생존 시 `Hit` 또는 `Idle/Chase` 복귀

### 2차 구현

- 캐릭터별 `SpecialBreakAttackAsset`
- `CameraSnapshotProfile` 연동
- 몬스터 크기별 타겟 위치 보정
- 보스 전용 컷신형 특수공격
- 파티 멤버 연계 특수공격

### SpecialBreakAttackAsset

```csharp
namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "SpecialBreakAttack", menuName = "UPlayGround/SO/Combat/Special Break Attack")]
    public class SpecialBreakAttackAsset : ScriptableObject
    {
        public CharacterActorType ownerType;
        public MotionSetAsset motionSet;
        public CameraSnapshotProfile cameraProfile;
        public float searchRange = 4f;
        public float startDistance = 1.5f;
        public float damageByMaxHpRate = 0.2f;
        public float fixedDamage = 0f;
        public float hitStopDuration = 0.08f;
        public string startVfxKey;
        public string hitVfxKey;
        public string finishVfxKey;
    }
}
```

---

## 구현 단계

### Phase 1: 행동 불능 정리

- `AttackReactionType`별 플레이어/몬스터 정책 확정
- `PlayerHitState`의 기존 캔슬 정책 유지
- `PlayerStunState`, `PlayerKnockdownState` 필요 여부 결정
- `EnemyStunState`, `EnemyKnockdownState`, `EnemyGroggyState` 추가 여부 결정
- 몬스터 등급별 반응 허용 정책 정의

완료 기준:

- 같은 공격 데이터가 플레이어/몬스터에게 어떤 상태를 만드는지 문서와 코드가 일치한다.
- 플레이어는 일반 피격에서 불필요하게 긴 조작 불능에 빠지지 않는다.
- 몬스터는 소형/중형/엘리트/보스별 경직 허용 범위가 다르다.

### Phase 2: 데이터와 런타임 게이지

- `MonsterBreakGaugeSO` 추가
- `MonsterBreakGauge` 컴포넌트 추가
- `ActorDefinitionSO` 또는 몬스터 프리팹에 브레이크 데이터 연결
- `HitPhaseData` / `AttackData`에 `breakDamage` 추가
- `MonsterActor.TakeDamage()`에서 브레이크 누적 호출
- `UI_ActorHpBar`에 브레이크 게이지 표시 추가

완료 기준:

- 공격 적중 시 몬스터 HP바에 브레이크 게이지가 증가한다.
- 게이지 최대치에서 `IsExposed == true`가 된다.
- 일정 시간 후 게이지가 정책대로 리셋된다.

### Phase 3: 노출 상태와 입력 프롬프트

- `EnemyBreakExposedState` 추가
- `MonsterBreakGauge.OnBreakExposed`에서 상태 전환 요청
- `SpecialBreakAttackController`에서 타겟 탐색
- UI 프롬프트 표시/숨김
- 입력 레이어와 연동

완료 기준:

- 게이지 완료 시 몬스터가 공격을 멈춘다.
- 플레이어가 가까이 가면 특수공격 입력 프롬프트가 뜬다.
- 시간 만료 시 프롬프트가 사라지고 몬스터가 복귀한다.

### Phase 4: 특수공격 실행

- `PlayerSpecialBreakAttackState` 추가
- `EnemySpecialBreakVictimState` 추가
- `SpecialBreakAttackAsset` 추가
- MotionSet 이벤트로 데미지/VFX/히트스톱 실행
- 특수공격 후 게이지 리셋

완료 기준:

- 입력 시 플레이어와 몬스터가 위치 보정 후 전용 모션을 재생한다.
- 타격 타이밍에 데미지와 연출이 발생한다.
- 종료 후 양쪽 상태가 정상 복구된다.

### Phase 5: 캐릭터/몬스터별 확장

- `CharacterActorType`별 특수공격 데이터 분리
- 보스 등급별 게이지/피해/반복 제한 적용
- 카메라 스냅샷 연출 연결
- 파티 교체 특수공격과 연계 가능성 검토

---

## 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| `PoiseStat`과 `Break Gauge` 역할이 겹침 | Poise는 경직, Break는 특수공격 기회로 문서/인스펙터 명칭 분리 |
| 플레이어가 너무 자주 행동 불능이 됨 | 플레이어 `Stun`, `Knockdown`, `Airborne`은 보스/엘리트 패턴 중심으로 제한 |
| 몬스터가 너무 자주 멈춰 난도가 낮아짐 | 등급별 최대 게이지 증가, 보스 페이즈당 횟수 제한 |
| 보스가 일반 경직으로 무력화됨 | 보스는 `Stun`을 `Groggy` 또는 `BreakExposed`로 변환 |
| 특수공격 연출 중 상태 복구 누락 | 실행 컨트롤러에서 입력/AI/카메라/타임스케일 복구를 한 곳에서 처리 |
| 다수 몬스터가 동시에 노출되어 UI가 복잡해짐 | 락온 타겟 우선, 화면 중앙/근거리 1개만 프롬프트 표시 |
| 피니시 공격과 조작이 겹침 | HP 기반 `FinishAttack`과 브레이크 기반 `SpecialBreakAttack` 입력 우선순위 정의 |
| 대형 콜라이더에서 위치 보정 실패 | 몬스터별 `SpecialAttackAnchor` 소켓 또는 반지름 데이터 추가 |

---

## 오픈 질문

- `PlayerKnockdownState`를 1차부터 만들 것인가, `PlayerHitState`에서 임시 처리할 것인가?
- 보스의 `Poise Break`와 `Break Gauge Full`을 같은 `Groggy`로 합칠 것인가, 별도 상태로 둘 것인가?
- 특수공격은 몬스터를 즉사시키는가, 큰 피해만 주는가?
- 보스에게도 동일하게 컷신형 특수공격을 허용할 것인가?
- 특수공격 입력 키를 기존 상호작용 키와 공유할 것인가?
- `Poise` 게이지 UI를 유지하면서 `Break Gauge`를 추가할 것인가, 기존 Poise UI를 브레이크 UI로 재해석할 것인가?
- 캐릭터별 특수공격을 1차부터 만들 것인가, 공통 모션 1개로 먼저 검증할 것인가?

---

## 권장 1차 결정안

- `PoiseStat`은 유지하고, 신규 `MonsterBreakGauge`를 추가한다.
- 플레이어 행동 불능은 기존 `PlayerHitState`의 짧은 캔슬 정책을 유지한다.
- 플레이어 `Stun`, `Knockdown`, `Airborne`은 보스/엘리트 패턴 중심으로 제한한다.
- 몬스터는 `Poise Break` 시 `AttackReactionType`에 따라 더 적극적으로 행동 불능 상태에 들어간다.
- 보스는 일반 스턴/다운 대신 `Groggy` 또는 `BreakExposed`로 변환한다.
- 브레이크 특수공격은 즉사가 아니라 최대 HP 비례 큰 피해로 시작한다.
- Normal/Elite는 반복 가능, Boss는 페이즈당 1회 또는 내부 쿨타임 20초 이상으로 제한한다.
- UI는 HP바 아래 별도 브레이크 게이지로 표시한다.
- 1차 특수공격은 Bokusei 공통 모션 1개로 만든다.
- 기존 `FinishAttack`은 HP 조건 마무리 공격으로 유지한다.
