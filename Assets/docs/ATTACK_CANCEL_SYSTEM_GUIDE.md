# 공격 캔슬(인터럽트) 시스템 가이드

## 개요

플레이어 공격 모션의 특정 구간에서 다른 동작으로 **끊고 들어가는(캔슬/인터럽트)** 시스템입니다.
가장 최근 추가된 **이동 후딜 캔슬**(공격 끝부분을 걷기로 끊는 기능)을 포함합니다.

핵심 원칙:

- **"무엇으로 캔슬할 수 있는가"** = 데이터(`PlayerInterruptAction` 마스크). 공격별로 SO에서 지정.
- **"언제 캔슬할 수 있는가"** = 캔슬 윈도우(히트박스 콜리전이 꺼져 있는 구간). 별도 이벤트 없이 콜리전 상태에서 자동 도출.
- **액티브 히트(타격 판정이 켜진 순간)에는 절대 캔슬 불가** — 공격이 헛돌지 않게 보장.
- **이동 캔슬만 예외 처리** — 다른 캔슬은 버튼(버퍼 입력)이지만 이동은 '누르고 있는 축'이라 별도 게이트가 붙는다.

> 한 줄 요약: 평소엔 "콜리전 꺼진 구간 + 허용된 입력"이면 캔슬, 단 **이동 캔슬은 "마지막 타격이 끝난 진짜 후딜"에서만** 발동.

---

## 캔슬 타임라인 (가장 중요)

하나의 공격 모션은 시간순으로 아래 구간으로 나뉩니다. 캔슬 가능 여부가 구간마다 다릅니다.

```
공격 모션 시작 ─────────────────────────────────────────────► 모션 완료
│           │              │           │              │        │
│  윈드업    │  액티브 히트  │  (간격)    │  액티브 히트  │  후딜   │
│ (준비동작) │  콜리전 ON   │  콜리전 OFF │  콜리전 ON   │(리커버리)│
│           │  [1타]       │           │  [2타]       │        │
└───────────┴──────────────┴───────────┴──────────────┴────────┘
 캔슬윈도우 O   캔슬윈도우 X   캔슬윈도우 O   캔슬윈도우 X   캔슬윈도우 O

 도지/대시/점프/가드/공격 캔슬:  콜리전 OFF인 모든 구간(윈드업 포함)에서 허용
 이동(걷기) 후딜 캔슬:          ★ 마지막 타격 이후 "후딜" 구간에서만 허용
```

- **캔슬 윈도우** = 콜리전이 꺼진 구간 = `PlayerCombat.IsCancelWindowOpen`(`= !IsPossibleCollide`).
- 도지·대시 등 일반 캔슬은 윈드업/간격/후딜 어디서든(콜리전만 꺼져 있으면) 가능합니다.
- **이동 캔슬은 윈드업·간격에서는 막혀 있고, 마지막 타격이 끝난 후딜에서만** 동작합니다. (이유는 아래 "이동 후딜 캔슬" 참고)

---

## 아키텍처

```
PlayerAttackState (공격 상태)
│  UpdateState() 매 프레임:
│    1. 액티브 히트 발생 기록 (_hasActiveHitFired)
│    2. [캔슬 윈도우 열림] → PlayerInterruptResolver.TryInterrupt(마스크)   ← 도지/대시/점프/가드/공격
│    3. 콤보 입력 검사 (CanCombo + 공격 버튼)
│    4. ★ 이동 후딜 캔슬 게이트 검사 → PlayerGroundMoveState 전환         ← 이동(걷기)
│
├── PlayerCombat (전투 컴포넌트)
│     IsPossibleCollide      : 지금 타격 판정이 켜져 있는가
│     IsCancelWindowOpen     : = !IsPossibleCollide (캔슬 가능 구간)
│     CurrentHitPhaseIndex   : 현재 진행된 히트 페이즈 번호
│     LastHitPhaseIndex      : 이 공격의 마지막 히트 페이즈 번호
│     CanCombo               : 콤보 윈도우가 열려 있는가
│
└── PlayerInterruptResolver (정적 헬퍼)
      TryInterrupt(마스크) : 우선순위대로 버퍼 입력 소비 후 해당 상태로 전환
```

데이터(마스크)가 흐르는 경로:

```
PlayerAttackInfo.interruptActions  (SO, 공격별 정의)
        │  ConvertToAttackData()
        ▼
AttackData.interruptActions        (런타임)
        │  PlayerAttackState가 읽음
        ▼
캔슬 가능 여부 판정
```

### 파일 구조

```
Assets/02.Scripts/
├── Data/Enum/
│   └── PlayerInterruptAction.cs          캔슬 허용 액션 [Flags] 마스크
├── Data/Combat/
│   └── CombatData.cs                      AttackInfoBase / AttackData / interruptActions 필드
├── GameActor/State/Player/
│   ├── PlayerAttackState.cs               캔슬 윈도우 게이트 + 이동 후딜 캔슬 판정
│   └── PlayerInterruptResolver.cs         마스크 → 상태 전환 라우팅
├── GameActor/Component/Player/
│   └── PlayerCombat.cs                    IsCancelWindowOpen / 페이즈 인덱스 노출
└── Data/Combat/Editor/
    ├── AttackDataFromMotionSetWindow.cs   신규 공격 생성 시 기본 마스크 부여
    └── PlayerAttackDataInterruptMigration.cs  기존 에셋 일괄 마이그레이션 메뉴
```

---

## 핵심 개념

### PlayerInterruptAction (캔슬 허용 마스크)

공격마다 "어떤 입력으로 끊을 수 있는가"를 비트 플래그로 지정합니다.

| 플래그 | 값 | 의미 |
|--------|----|------|
| `None` | 0 | 캔슬 불가 |
| `Dodge` | 1 | 회피로 캔슬 |
| `Jump` | 2 | 점프(공중)로 캔슬 |
| `Dash` | 4 | 대시로 캔슬 (조건부 — 실패 시 콤보로 폴백) |
| `Guard` | 8 | 가드로 캔슬 |
| `LightAttack` | 16 | 약공 입력으로 캔슬(다른 공격으로 전환) |
| `HeavyAttack` | 32 | 강공 입력으로 캔슬 |
| `Skill` | 64 | 스킬 입력으로 캔슬(게이지 충분 시) |
| `Move` | 128 | **이동(걷기)으로 후딜 캔슬** (※ 다른 플래그와 동작 방식이 다름) |

여러 개를 OR로 조합합니다. 예: `Dodge | Dash | Move` = 회피·대시·이동으로 캔슬 가능.

### 캔슬 윈도우 = 콜리전 비활성 구간

별도의 "캔슬 가능" 이벤트를 만들지 않습니다. 타격 판정(히트박스 콜리전)이 **꺼져 있으면** 캔슬 윈도우가 자동으로 열립니다.

```csharp
// PlayerCombat.cs
public bool IsPossibleCollide  => 지금 타격 판정 ON 여부;
public bool IsCancelWindowOpen => !IsPossibleCollide;   // 타격 OFF면 캔슬 가능
```

→ 타격이 나가는 순간(액티브 히트)에는 무엇으로도 캔슬할 수 없습니다.

### 콤보 윈도우와의 관계

- **같은 타입 연계(약→약→약)** = 콤보 윈도우(`CanCombo`)로 처리. 공격 버튼을 눌러 다음 타로 이어감.
- **다른 타입 전환(약→강/스킬)** = 캔슬(`HeavyAttack`/`Skill` 마스크)로 처리.
- 둘 다 성립하면 **캔슬이 우선**합니다.

---

## ★ 이동 후딜 캔슬 (이번 추가 기능)

### 무엇을 하는가

공격 모션의 끝(후딜/리커버리)을 **걷기 입력으로 즉시 끊고** 이동 상태로 빠져나옵니다.
원래는 후딜 동안 가만히 모션이 끝나길 기다려야 했지만, 이제 마지막 타격이 끝나면 바로 움직일 수 있어 전투가 경쾌해집니다.

### 왜 다른 캔슬과 다르게 처리하는가

도지·대시 같은 캔슬은 **버튼(버퍼 입력)** 이라 "눌렀다 = 의도"가 명확합니다.
하지만 이동은 **스틱을 계속 쥐고 있는 축**입니다. 단순히 "콜리전 OFF + 이동 중"으로 처리하면:

- **윈드업 페인트 버그**: 공격을 시작하자마자(타격 전) 걸어서 빠지는 게 가능해져 공격이 헛돕니다.
- **멀티히트 끊김 버그**: 2타짜리 공격에서 스틱을 쥐고 있으면 1타 후 간격에서 바로 이동으로 빠져 2타가 안 나갑니다.

그래서 이동 캔슬에는 **"진짜 후딜인지"를 가리는 게이트**가 붙습니다.

### 게이트 조건 (5개 모두 만족해야 발동)

`PlayerAttackState.UpdateState()`에서 검사합니다.

| 조건 | 코드 | 거르는 대상 |
|------|------|------------|
| Move 플래그 보유 | `interruptActions & Move != 0` | 이동 캔슬 비허용 공격 |
| 콤보 윈도우 닫힘 | `!_combat.CanCombo` | 콤보 잇는 중엔 억제 (반응형 콤보 보존) |
| 타격 1회 이상 발생 | `_hasActiveHitFired` | **윈드업**(아직 한 대도 안 때림) |
| 콜리전 비활성 | `!_combat.IsPossibleCollide` | 타격 판정 켜진 순간 |
| 마지막 페이즈 통과 | `CurrentHitPhaseIndex >= LastHitPhaseIndex` | **멀티히트 중간 간격** |
| (그리고) 이동 입력 | `playerController.HasMoveInput()` | 이동 안 할 땐 발동 안 함 |

```csharp
// PlayerAttackState.cs — UpdateState() 발췌
if ((_currentAttack.interruptActions & PlayerInterruptAction.Move) != 0
    && !_combat.CanCombo
    && _hasActiveHitFired
    && !_combat.IsPossibleCollide
    && _combat.CurrentHitPhaseIndex >= _combat.LastHitPhaseIndex
    && playerController.HasMoveInput())
{
    _combat.ResetCombo();
    controller.TransitionToState(new PlayerGroundMoveState(controller));
}
```

### 멀티히트에서 어떻게 동작하나 (예: 2타 공격)

```
1타 윈드업      : _hasActiveHitFired=false      → 게이트 닫힘 (윈드업 제외)
1타 타격 중      : IsPossibleCollide=true        → 게이트 닫힘 (타격 중 제외)
1타~2타 간격     : phase(0) < last(1)            → 게이트 닫힘 (멀티히트 보존) ★
2타 타격 중      : IsPossibleCollide=true        → 게이트 닫힘
2타 후 후딜      : 모든 조건 충족                 → 게이트 열림 → 이동 캔슬 ✅
```

→ **마지막 타격이 끝난 진짜 후딜에서만** 이동 캔슬이 됩니다.

### 콤보 윈도우 중 억제 (설계 결정)

콤보 윈도우(`CanCombo`)가 열려 있는 동안엔 이동 캔슬을 막습니다.
스틱을 쥔 채 화면을 보다가 공격 버튼을 눌러 **반응형으로 콤보를 잇는** 조작감을 보존하기 위함입니다.
콤보 윈도우가 닫힌 **리커버리 꼬리 구간**에서만 이동 캔슬이 동작합니다.

---

## 셋업 방법

### 1. 코드는 이미 적용됨

위 파일들은 수정이 끝나 있습니다. **Unity 에디터에서 컴파일 에러가 없는지 콘솔을 확인**하세요.

### 2. 기존 공격 에셋에 Move 플래그 부여 (필수)

기존 `PlayerAttackDataSO` 에셋에는 아직 `Move` 비트가 없으므로 이동 캔슬이 동작하지 않습니다.
아래 메뉴를 실행해 **비파괴적으로**(기존 손튜닝 값 보존, OR로 추가) Move 플래그를 넣습니다.

```
메뉴: UPlayGround → 게임플레이 → 전투 → PlayerAttackData 이동 후딜 캔슬(Move) 플래그 추가
```

- 약공/강공/스킬 공격 리스트에 `Move`를 추가합니다.
- 차지·대시·점프 공격은 커밋감 유지를 위해 **제외**(자동 부여 안 함). 필요하면 인스펙터에서 개별 체크.

### 3. 개별 조정 (인스펙터)

특정 공격만 이동 캔슬을 켜거나 끄려면 `PlayerAttackDataSO`의 해당 공격 `interruptActions`에서 `Move` 체크박스를 직접 토글합니다.

---

## 사용 예시

### 새 공격을 코드로 만들 때

신규 공격을 `AttackDataFromMotionSetWindow`로 생성하면 Light/Heavy/Skill 카테고리는 기본으로 `Move`가 포함됩니다. 별도 작업 불필요.

### 데이터에서 캔슬 가능 액션 지정

```
약공 1타 (PlayerAttackInfo.interruptActions):
  Dodge | Jump | Dash | Guard | HeavyAttack | Skill | Move

강공 마무리 (커밋감 주고 싶다면):
  Dodge | Skill           ← Move 빼서 후딜 이동 캔슬 막음
```

---

## 에디터 도구

| 메뉴 경로 | 기능 |
|-----------|------|
| `UPlayGround/게임플레이/전투/PlayerAttackData 이동 후딜 캔슬(Move) 플래그 추가` | **비파괴**. 약/강/스킬에 `Move`만 OR 추가(손튜닝 보존) |
| `UPlayGround/게임플레이/전투/PlayerAttackData 캔슬 기본값 마이그레이션` | **덮어쓰기 주의**. 캔슬 마스크를 카테고리별 기본값으로 초기화(손튜닝 날아감) |

> ⚠️ 이동 캔슬만 추가하고 싶다면 반드시 **위쪽(비파괴)** 메뉴를 쓰세요. 아래 "기본값 마이그레이션"은 기존에 손으로 조정한 캔슬 설정을 전부 덮어씁니다.

---

## 주의 사항

- **마이그레이션 메뉴를 안 돌리면** 기존 공격에는 `Move` 비트가 없어 이동 캔슬이 전혀 안 됩니다("기능이 적용 안 된다"의 가장 흔한 원인).
- **페이즈 개수 불일치**: 이동 캔슬 판정은 `LastHitPhaseIndex = hitPhases 개수 - 1`을 기준으로 합니다. 손으로 작성한 데이터에서 Collision 이벤트의 `hitPhaseIndex`와 `hitPhases` 리스트 개수가 어긋나면, 이동 캔슬이 안 뜨고 모션 완료까지 기다립니다(보수적 실패 — 게임이 깨지진 않음).
- **타격이 전혀 없는 모션**(콜리전 이벤트 없음)은 `_hasActiveHitFired`가 끝까지 false라 이동 캔슬이 발동하지 않습니다.
- **콤보 윈도우 중엔 이동 캔슬이 억제**됩니다. 후딜 캔슬이 늦게 걸린다고 느껴지면 콤보 윈도우(`ComboWindowEvent`)가 너무 늦게 닫히는지 확인하세요.
- 이동 캔슬은 `PlayerInterruptResolver`를 **거치지 않습니다**(버튼이 아니라 축이라서). `Move` 비트를 리졸버에 넘겨도 그쪽엔 분기가 없어 무시되므로 무해합니다. 실제 판정은 전부 `PlayerAttackState`에 있습니다.

---

## 확장 포인트

- **새 캔슬 액션 추가**(버튼류): `PlayerInterruptAction`에 플래그 1개 + `PlayerInterruptResolver.TryInterrupt`에 분기 1줄.
- **캔슬 구간 정밀화**: 지금은 "콜리전 OFF"로 캔슬 윈도우를 도출합니다. 더 정밀하게 하려면 전용 MotionEvent로 윈도우를 따로 그어 `IsCancelWindowOpen` 규칙을 교체할 수 있습니다.
- **이동 캔슬 타이밍 조정**: 후딜 진입 시점을 당기거나 늦추려면 마지막 Collision 이벤트의 종료 시점, 콤보 윈도우 종료 시점을 MotionSet 타임라인에서 조정합니다.
