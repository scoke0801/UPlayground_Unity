# 캐릭터 스왑 회피 / 카운터 시스템 설계 문서

> 작성일: 2026-06-03  
> Phase 1 구현 갱신: 2026-06-03  
> Phase 2 구현 갱신: 2026-06-03  
> Phase 3 구현 갱신: 2026-06-03  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 레퍼런스: 명조 `Dodge Counter`, `Intro / Outro Skill`, 퀵스왑 카운터

---

## 구현 현황

2026-06-03 기준 Phase 1~3을 구현했다.

| 항목 | 상태 | 구현 파일 |
|------|------|-----------|
| 스왑 회피 전역 설정 | 완료 | `PartyConfigSO` |
| 몬스터 공격 위협 스냅샷 | 완료 | `EnemyAttackThreat`, `EnemyCombat.TryGetSwapEvadeThreat()` |
| 텔레그래프/충돌 타이밍 기록 | 완료 | `EnemyCombat.BeginTelegraph()`, `EnemyCombat.SetEnableCollision()` |
| 교체 전 위협 평가 | 완료 | `PartyManager.TryEvaluateSwapEvade()` |
| 스왑 우선순위 | 완료 | `SwapEvade > PerfectDodge Assist > EntryAttack` |
| 스왑 회피 i-frame | 완료 | `PlayerActor.BeginSwapEvadeIFrame()`, `CanTakeDamage()` |
| 스왑 회피 카운터 큐 | 완료 | `PlayerActor.QueueSwapEvade()`, `ConsumeSwapEvadeQueue()` |
| 카운터 공격 데이터 | 완료 | `PlayerAttackDataSO.swapEvadeCounterAttack`, `PlayerCombat.ExecuteSwapEvadeCounterAttack()` |
| 전용 AnimKey | 완료 | `AnimKey.Player_SwapEvadeCounterAttack_1` |
| 공격 데이터 에디터 탭 | 완료 | `PlayerAttackDataSODrawer`의 `회피` 탭 |
| MotionSet 기반 생성기 | 완료 | `AttackDataFromMotionSetWindow`의 `SwapEvadeCounter` 카테고리 |
| 밸런스 분석 포함 | 완료 | `BalanceAttackAnalyzer`, `BalanceDataExtractor` |
| 성공 피드백 설정 | 완료 | `PartyConfigSO`의 `Swap Evade Feedback` |
| 성공 히트스톱/쉐이크/FX | 완료 | `PlayerActor.PlaySwapEvadeFeedback()` |
| Danger Ring 완료 처리 | 완료 | `PartyManager.RequestSwapTo()` |
| 바이탈 오브 보상 옵션 | 완료 | `PartyConfigSO.swapEvadeSpawnDodgeVitalOrb` |
| 빌드 검증 | 완료 | `dotnet build UPlayground.sln --no-restore` 성공 |

현재 정책:

- 일반 스왑에는 무적을 주지 않는다.
- 스왑 회피 성공 시에만 짧은 i-frame을 부여한다.
- 스왑 회피 카운터는 `swapEvadeCounterAttack` 데이터를 우선 사용한다.
- `swapEvadeCounterAttack`이 비어 있으면 `entryAttack`, 약 공격 첫 번째 순으로 폴백한다.
- 전용 상태 `PlayerSwapEvadeCounterState`는 아직 만들지 않았다. 현재는 `PlayerAttackState` 안에서 전용 공격 라우팅만 분리한다.
- 성공 피드백은 `Swap Evade Feedback` 설정으로 제어한다. 기본값은 짧은 히트스톱, `LiteHit` 쉐이크, Danger Ring 완료이며 FX와 바이탈 오브 보상은 기본 비활성이다.

---

## 개요

캐릭터 스왑을 단순한 모델 교체가 아니라, 몬스터 공격 타이밍을 읽고 회피/카운터로 전환할 수 있는 전투 입력으로 확장한다.

현재 프로젝트에는 이미 다음 기반이 있다.

- `PartyManager.RequestSwapTo()`에서 교체 성공 시 어시스트/등장 공격을 예약한다.
- `PlayerCombat.IsPerfectDodgeWindow` 중 스왑하면 `QueueSwapAssist()`가 발동한다.
- `PlayerActor.Update()`는 예약된 스왑 어시스트/등장 공격을 다음 프레임 공격 입력으로 주입한다.
- `SwapResidualAttackRunner`는 공격 중 교체한 outgoing 모델의 잔류 공격을 실행한다.
- `EnemyCombat`는 텔레그래프, Danger Ring, 실제 충돌 활성 시점을 알고 있다.

따라서 새 시스템은 완전 신규 전투 체계가 아니라, 기존 파티 스왑 / 등장 공격 / 몬스터 공격 예고 구조 위에 "스왑 회피 성공 판정"을 추가하는 방향으로 설계한다.

핵심 방향:

- 스왑 입력은 위험한 몬스터 공격 타이밍에 맞으면 회피 입력으로도 인정한다.
- 성공한 스왑 회피는 짧은 무적과 진입 카운터 공격을 제공한다.
- 일반 스왑은 무적을 보장하지 않는다.
- 스왑 회피는 기존 `PerfectDodgeWindow` 어시스트보다 독립적인 판정으로 둔다.
- 기존 잔류 공격 시스템과 충돌하지 않도록 우선순위를 명확히 둔다.

---

## 레퍼런스 조사 요약

### 명조에서 확인한 전투 감각

| 항목 | 레퍼런스 내용 | UPlayground 반영 |
|------|---------------|------------------|
| 정확 회피 | 적 공격을 정확히 피하면 Dodge Counter 기회가 생긴다. | 스왑 입력도 특정 위험 타이밍에서는 회피 성공으로 인정한다. |
| Intro / Outro | 교체 시 진입 캐릭터의 Intro, 퇴장 캐릭터의 Outro가 발동한다. | incoming 캐릭터의 진입 카운터 공격과 outgoing 잔류 공격이 동시에 전투 기여할 수 있다. |
| 퀵스왑 | 공격 도중 교체해도 이전 캐릭터 공격이 남는 플레이가 가능하다. | 기존 `SwapResidualAttackRunner` 유지. 스왑 회피는 이 위에 추가되는 방어형 진입 조건이다. |
| 스왑 카운터 | 플레이어 사례에서 교체를 패리/카운터 타이밍에 활용하는 운용이 언급된다. | 몬스터 공격 위협 판정을 통과한 스왑만 카운터 보상으로 처리한다. |
| 무적 여부 | 교체 자체가 항상 무적이라는 공식 규칙은 확인되지 않는다. | 일반 스왑 무적은 금지. 성공한 스왑 회피에만 짧은 i-frame을 부여한다. |

참고 자료:

- WutheringWaves.gg, Combat Guide: https://wutheringwaves.gg/combat-basics/
- WutheringWaves.gg, Intro & Outro Skill System Guide: https://wutheringwaves.gg/intro-outro-explained/
- Wuthering Waves Wiki, Dodge: https://wutheringwaves.fandom.com/wiki/Dodge
- Wuthering Waves Wiki, Combat: https://wutheringwaves.fandom.com/wiki/Combat
- Reddit 플레이어 사례, 교체로 카운터 타이밍 활용: https://www.reddit.com/r/WutheringWaves/comments/1d136gj/switch_counters_using_character_switching_to/

공개 자료만으로 명조 내부의 정확한 무적 프레임, 좌표 보정, 판정 알고리즘은 확정할 수 없다. 본 설계는 레퍼런스의 체감 전투 구조를 UPlayground의 기존 코드 구조에 맞춰 해석한 것이다.

---

## 현재 프로젝트 구조

### 파티 스왑 흐름

현재 교체 요청의 중심은 `PartyManager.RequestSwapTo(int targetIndex)`다.

현재 흐름:

1. `CanSwapTo(targetIndex)`로 교체 가능 여부 확인.
2. 현재 캐릭터 타입과 목표 캐릭터 타입을 계산.
3. `isAssist = _player.GetCombat()?.IsPerfectDodgeWindow == true` 판정.
4. `PlayerSwapBehaviour.SwapTo(targetType)`로 실제 모델 교체.
5. `isAssist`면 `_player.QueueSwapAssist()` 호출.
6. 아니면 `TryFindEntryAttackTarget(...)` 성공 시 `_player.QueueEntryAttack(entryTarget)` 호출.
7. 이전 캐릭터에 스왑 쿨타임 기록.
8. `OnSwapCompleted` 발행.

### 스왑 공격 예약 흐름

`PlayerActor.Update()`는 다음 순서로 예약 행동을 입력 상태로 변환한다.

1. `_swapAssistQueued`가 있으면 `_attackInputCondition = InputCondition.Pressed`.
2. 아니면 `_entryAttackQueued`가 있으면 `ConsumeEntryAttackQueue()`.
3. 이후 `PlayerMovementController.SetInputs(...)`로 상태 머신에 입력 전달.

즉, 스왑 회피 카운터도 같은 예약 흐름에 얹을 수 있다.

### 몬스터 공격 위협 정보

`EnemyCombat`는 다음 정보를 이미 가지고 있다.

- `CurrentSkill`
- `IsPossibleCollide`
- `BeginTelegraph(int hitPhaseIndex, bool lockPositionOnStart)`
- `SetEnableCollision(bool isCollisionEnable)`
- `GetCurrentAttackPosition()`
- `GetCurrentAttackRadius()`
- `CurrentSkill.useDangerRing`
- 텔레그래프 위치 고정 여부와 히트 페이즈 인덱스

현재 부족한 것은 "지금 플레이어가 스왑하면 회피 성공인지"를 외부에서 물어볼 수 있는 정리된 위협 스냅샷 API다.

---

## 목표 플레이 경험

플레이어가 몬스터 공격이 들어오기 직전에 캐릭터 스왑을 누르면:

1. outgoing 캐릭터는 현재 공격 중이라면 기존 잔류 공격을 계속 실행한다.
2. incoming 캐릭터는 짧은 무적으로 진입한다.
3. 몬스터 공격은 피격으로 처리되지 않는다.
4. incoming 캐릭터가 스왑 회피 카운터 공격을 자동 발동한다.
5. 성공 피드백으로 히트스톱, 카메라 셰이크, Danger Ring 완료/해제, 이펙트를 준다.

실패한 일반 스왑은:

1. 기존처럼 캐릭터만 교체한다.
2. 범위 내 적이 있으면 기존 등장 공격을 발동할 수 있다.
3. 적 공격 타이밍과 겹치면 피격될 수 있다.

---

## 판정 정책

### 성공 조건

스왑 회피는 아래 조건을 모두 만족해야 성공한다.

1. `PartyConfigSO.enableSwapEvade == true`
2. 현재 플레이어가 `Death`, `Grabbed` 상태가 아님
3. 교체 대상 캐릭터가 생존하고 스왑 쿨타임이 아님
4. 주변 몬스터 중 하나 이상이 공격 위협 상태임
5. 플레이어 위치가 해당 공격의 실질 피해 범위 안이거나, 설정된 패딩 범위 안임
6. 공격 타이밍이 다음 중 하나임
   - 텔레그래프 종료 직전 `swapEvadeWindowBeforeHit` 안
   - 충돌 활성 시작 직후 `swapEvadeGraceAfterHitStart` 안

### 실패 조건

다음 경우는 일반 스왑으로 처리한다.

- 적 공격 위협이 없음
- 위협은 있지만 플레이어가 공격 범위 밖임
- 텔레그래프가 너무 이르거나 이미 늦음
- 타겟 캐릭터가 쿨타임/사망 상태임
- 설정에서 스왑 회피를 끔

### 우선순위

스왑 성공 후 예약 행동 우선순위:

1. `SwapEvadeCounter`
2. `PerfectDodge SwapAssist`
3. `EntryAttack`
4. 일반 스왑

이 순서를 유지해야 같은 입력에서 두 공격 예약이 중복되지 않는다.

---

## 데이터 설계

### PartyConfigSO 추가 필드

`PartyConfigSO`의 `Swap` 섹션 아래 또는 별도 `Swap Evade` 섹션을 추가한다.

```csharp
[Header("Swap Evade")]
public bool enableSwapEvade = true;

[Tooltip("실제 피격 시점 이전에 스왑 회피 성공으로 인정할 시간.")]
[Min(0f)] public float swapEvadeWindowBeforeHit = 0.25f;

[Tooltip("충돌 활성 직후에도 입력 지연을 보정해 성공으로 인정할 시간.")]
[Min(0f)] public float swapEvadeGraceAfterHitStart = 0.08f;

[Tooltip("스왑 회피 성공 직후 플레이어 피격을 막는 시간.")]
[Min(0f)] public float swapEvadeIFrameDuration = 0.35f;

[Tooltip("스왑 회피 카운터 공격 입력을 유지할 시간.")]
[Min(0f)] public float swapEvadeCounterInputWindow = 0.45f;

[Tooltip("위협 탐색 범위. 0 이하이면 entryAttack 기본 범위를 사용.")]
[Min(0f)] public float swapEvadeThreatSearchRange = 6f;

[Tooltip("공격 반경에 더해 회피 성공으로 인정할 여유 거리.")]
[Min(0f)] public float swapEvadeThreatRadiusPadding = 0.5f;

public LayerMask swapEvadeThreatLayer = ~0;
```

### CharacterModelData 선택 필드

캐릭터별 차등이 필요하면 2차에서 추가한다.

```csharp
[Header("Swap Evade")]
public float swapEvadeCounterRange = 0f;
public bool preferSwapEvadeCounter = true;
```

1차 구현에서는 전역 설정만 사용한다.

---

## 런타임 타입 설계

### EnemyAttackThreat

몬스터 공격 위협 정보를 값 타입으로 묶는다.

```csharp
public readonly struct EnemyAttackThreat
{
    public readonly MonsterActor Source;
    public readonly EnemyCombat Combat;
    public readonly Vector3 Position;
    public readonly float Radius;
    public readonly float TimeToHit;
    public readonly bool IsCollisionActive;
    public readonly int HitPhaseIndex;
}
```

`TimeToHit`은 정확한 애니메이션 잔여 시간이 아니라, `EnemyCombat`가 추적 가능한 텔레그래프/충돌 기준 상대 시간으로 계산한다.

### EnemyCombat API

`EnemyCombat`에 다음 API를 추가한다.

```csharp
public bool TryGetSwapEvadeThreat(
    Vector3 playerPosition,
    float beforeHitWindow,
    float afterHitGrace,
    float radiusPadding,
    out EnemyAttackThreat threat)
```

내부에 필요한 상태:

- `_lastTelegraphStartTime`
- `_lastTelegraphHitPhaseIndex`
- `_lastCollisionStartTime`
- `_currentHitPhaseIndex`

위 값은 `BeginTelegraph(...)`와 `SetEnableCollision(true)`에서 갱신한다.

### PartyManager API

`PartyManager.RequestSwapTo()` 안에서 교체 전 위협을 평가한다.

```csharp
bool isSwapEvade = TryEvaluateSwapEvade(out EnemyAttackThreat threat);
bool isAssist = !isSwapEvade && _player.GetCombat()?.IsPerfectDodgeWindow == true;
```

교체 성공 후:

```csharp
if (isSwapEvade)
{
    _player.BeginSwapEvadeIFrame(SwapEvadeIFrameDuration);
    _player.QueueSwapEvade(threat.Source);
}
else if (isAssist)
{
    _player.QueueSwapAssist();
}
else if (TryFindEntryAttackTarget(...))
{
    _player.QueueEntryAttack(entryTarget);
}
```

중요: i-frame은 모델 교체 이후가 아니라 교체 성공 직후 즉시 켜야 한다. 다음 프레임 예약 공격보다 몬스터 히트 판정이 먼저 들어오는 경우를 막기 위해서다.

### PlayerActor 확장

추가 필드:

```csharp
private bool _swapEvadeQueued;
private MonsterActor _swapEvadeTarget;
private float _swapEvadeInvincibleEndTime = -999f;
private float _swapEvadeCounterInputEndTime = -999f;
```

추가 프로퍼티:

```csharp
public bool IsSwapEvadeInvincible => Time.time <= _swapEvadeInvincibleEndTime;
public bool IsSwapEvadeCounterAvailable => Time.time <= _swapEvadeCounterInputEndTime;
```

추가 메서드:

```csharp
public void BeginSwapEvadeIFrame(float duration)
{
    _swapEvadeInvincibleEndTime = Time.time + Mathf.Max(0f, duration);
}

public void QueueSwapEvade(MonsterActor target, float counterWindow)
{
    _swapEvadeQueued = true;
    _swapEvadeTarget = target;
    _swapEvadeCounterInputEndTime = Time.time + Mathf.Max(0f, counterWindow);
}
```

`CanTakeDamage()` 또는 데미지 진입점에는 `IsSwapEvadeInvincible` 체크를 추가한다.

---

## 공격 실행 정책

### 1차 구현

전용 상태를 만들지 않고 기존 공격 입력 주입을 재사용한다.

- `_swapEvadeQueued`가 있으면 `_attackInputCondition = InputCondition.Pressed`
- `PlayerCombat`는 `IsSwapEvadeCounterAvailable`일 때 공격 데이터 선택 우선순위를 조정한다.
- 전용 데이터가 없으면 기존 `entryAttack` 또는 일반 1타로 폴백한다.

장점:

- 변경 범위가 작다.
- 기존 공격 상태/모션/히트 판정을 재사용한다.
- 먼저 판정 체감 검증이 가능하다.

단점:

- 전용 카운터 모션/피해/연출을 구분하기 어렵다.

### 2차 구현

전용 상태 `PlayerSwapEvadeCounterState`를 추가한다.

역할:

- 진입 시 타겟 방향으로 회전.
- 짧은 슬라이드/워프 보정.
- 전용 `AnimKey.Player_SwapCounterAttack_1` 또는 `Player_SwapAttack_*` 실행.
- MotionEvent 기반 충돌은 기존 `PlayerCombat` 경로 사용.
- 종료 후 이동 입력이 있으면 `GroundMove`, 없으면 `Idle`.

전용 `PlayerAttackDataSO` 필드:

```csharp
[Tooltip("스왑 회피 성공 시 발동하는 카운터 공격 데이터.")]
public PlayerAttackInfo swapEvadeCounterAttack;
```

---

## 구현 단계

### Phase 1: 판정 / 무적 / 기존 공격 재사용

목표: 스왑 타이밍으로 몬스터 공격을 피하고 기존 공격으로 반격하는 최소 기능.

작업:

1. `PartyConfigSO`에 `Swap Evade` 설정 추가.
2. `PartyManager`에 읽기 전용 프로퍼티 추가.
3. `EnemyCombat`에 텔레그래프/충돌 시작 시각 기록 추가.
4. `EnemyCombat.TryGetSwapEvadeThreat(...)` 추가.
5. `PartyManager.TryEvaluateSwapEvade(...)` 추가.
6. `RequestSwapTo()` 우선순위를 `SwapEvade > PerfectDodge Assist > EntryAttack`으로 수정.
7. `PlayerActor.BeginSwapEvadeIFrame(...)`, `QueueSwapEvade(...)` 추가.
8. 플레이어 피격 가능 판정에 `IsSwapEvadeInvincible` 반영.
9. 스왑 회피 성공 시 기존 공격 입력으로 카운터 발동.
10. `dotnet build UPlayground.sln --no-restore` 검증.

완료 기준:

- 적 Danger Ring/텔레그래프 말기에 스왑하면 피격되지 않는다.
- 같은 타이밍에 일반 스왑 공격 또는 어시스트 공격이 중복 예약되지 않는다.
- 스왑 회피 실패 시 일반 스왑 동작은 기존과 같다.

### Phase 2: 전용 카운터 공격

목표: 스왑 회피 성공 시 전용 모션/피해/연출 사용.

작업:

1. `PlayerAttackDataSO.swapEvadeCounterAttack` 추가.
2. `PlayerAttackDataSODrawer`에 에디터 탭 추가.
3. `PlayerCombat`에 스왑 회피 카운터 공격 데이터 선택 경로 추가.
4. 필요 시 `PlayerSwapEvadeCounterState` 추가.
5. `AnimKey.Player_SwapCounterAttack_1` 추가 또는 기존 `Player_SwapAttack_*` 재사용 정책 확정.
6. MotionSet/HitPhase 데이터 생성.

완료 기준:

- 캐릭터별 스왑 회피 카운터 모션과 피해를 독립 튜닝할 수 있다.
- 기존 등장 공격과 스왑 특수 공격 데이터가 오염되지 않는다.

### Phase 3: 피드백 / 밸런스

목표: 성공 감각과 밸런스 안정화.

작업:

1. 성공 이펙트 FXKey 추가. 완료.
2. 성공 카메라 셰이크 키 추가. 완료.
3. 짧은 히트스톱/슬로우 연출 추가. 완료.
4. Danger Ring 즉시 완료/해제 연동. 완료.
5. HUD에 성공 텍스트 또는 플래시 표시 검토. 보류.
6. 쿨타임/게이지 보상 여부 결정. 바이탈 오브 보상 옵션만 추가하고 기본 비활성.

구현된 피드백 설정:

| 설정 | 기본값 | 설명 |
|------|--------|------|
| `swapEvadeEnableHitStop` | true | 성공 시 글로벌 히트스톱 재생 |
| `swapEvadeHitStopDuration` | 0.06초 | 성공 히트스톱 지속 시간 |
| `swapEvadeHitStopTimeScale` | 0.08 | 성공 히트스톱 타임스케일 |
| `swapEvadeCameraShakeKey` | `LiteHit` | 성공 카메라 쉐이크 |
| `swapEvadeFxKey` | 빈 문자열 | 비워두면 FX 미재생 |
| `swapEvadeFxSocket` | `Center` | FX 위치 기준 소켓 |
| `swapEvadeFxOffset` | `Vector3.zero` | FX 위치 오프셋 |
| `swapEvadeCompleteDangerRing` | true | 성공 위협의 Danger Ring 완료 처리 |
| `swapEvadeSpawnDodgeVitalOrb` | false | Dodge 바이탈 오브 보상 |

### Unity Editor 설정 체크리스트

코드는 연결되어 있지만, 실제 플레이 체감을 내려면 에디터에서 아래 항목을 확인해야 한다.

1. `Assets/10.Datas/Party/PartyConfig.asset`의 `Swap Evade` 값을 확인한다.
   - `enableSwapEvade`가 켜져 있어야 한다.
   - `swapEvadeThreatLayer`는 몬스터 콜라이더가 포함된 레이어만 지정하는 것을 권장한다.
   - `swapEvadeThreatSearchRange`, `swapEvadeThreatRadiusPadding`은 실제 전투 거리와 몬스터 공격 반경에 맞춰 조정한다.

2. `PartyConfig.asset`의 `Swap Evade Feedback` 값을 확인한다.
   - `swapEvadeCameraShakeKey`는 등록된 `CameraShakeDatabase` 키여야 한다.
   - `swapEvadeFxKey`를 사용할 경우 `GameObjectManager` FX 풀/Addressables에 해당 키가 등록되어 있어야 한다.
   - `swapEvadeSpawnDodgeVitalOrb`는 보상 밸런스가 달라지므로 전투 테스트 후 켠다.

3. 각 플레이어 `PlayerAttackDataSO`의 `회피` 탭을 확인한다.
   - 전용 모션을 쓸 경우 `swapEvadeCounterAttack.baseInfo.animKey`를 `Player_SwapEvadeCounterAttack_1`로 설정한다.
   - 비워두면 `entryAttack`, 약 공격 첫 번째 순으로 폴백한다.
   - 폴백 대상도 MotionSet이 없으면 카운터 공격 상태 진입이 실패할 수 있으므로 최소 하나는 유효한 MotionSet을 가진다.

4. MotionSet 자동 생성기를 사용할 경우 `Player_SwapEvadeCounterAttack_1` MotionSet을 만든 뒤 생성기를 실행한다.
   - `UPlayGround` 공격 데이터 생성 창에서 해당 MotionSet이 `회피` 카테고리로 분류되는지 확인한다.
   - 생성 후 `PlayerAttackDataSO`의 `회피` 탭에 데이터가 들어갔는지 확인한다.

5. 몬스터 공격 데이터와 MotionEvent를 확인한다.
   - 스왑 회피 판정은 `EnemyCombat.BeginTelegraph()`와 `SetEnableCollision(true)` 타이밍을 기준으로 한다.
   - `BeginTelegraph`가 없더라도 현재 MotionSet에 `BeginCollisionEvent` 또는 `SpawnProjectileEvent`가 남아 있으면 사전 판정 창이 열린다.
   - 다만 바닥 텔레그래프 위치 고정, Danger Ring, 명확한 예고 연출이 필요한 공격은 `BeginTelegraph` 이벤트를 넣는 것을 권장한다.
   - 실제 히트 이벤트 직후 보정은 `SetEnableCollision(true)` 이후 `swapEvadeGraceAfterHitStart` 동안만 동작한다.

6. 씬 플레이 테스트에서 아래를 확인한다.
   - Danger Ring 종료 직전 스왑 시 HP가 감소하지 않는지.
   - 일반 스왑에는 무적이 생기지 않는지.
   - 성공 시 카운터 공격, 히트스톱, 카메라 쉐이크, FX가 중복 없이 한 번만 발생하는지.
   - 이전 캐릭터 스왑 쿨타임이 정상 적용되는지.

권장 기본값:

| 설정 | 기본값 | 이유 |
|------|--------|------|
| `swapEvadeWindowBeforeHit` | 0.25초 | 퍼펙트 도지보다 약간 여유 있지만 스왑 쿨타임이 있어 남발 제한 |
| `swapEvadeGraceAfterHitStart` | 0.08초 | 입력/프레임 지연 보정 |
| `swapEvadeIFrameDuration` | 0.35초 | 교체 직후 다단 히트 방지 |
| `swapEvadeCounterInputWindow` | 0.45초 | 다음 프레임 상태 전환 안정화 |
| `swapEvadeThreatRadiusPadding` | 0.5m | KCC/캡슐/텔레그래프 시각 오차 보정 |

---

## 밸런스 정책

### 강점

- 위험한 적 공격을 공격 기회로 전환한다.
- 잔류 공격과 결합해 교체 전투의 숙련도를 높인다.
- 캐릭터 교체 쿨타임이 있으므로 무한 회피보다 리듬형 방어가 된다.

### 제한

- 일반 스왑에는 무적을 주지 않는다.
- 성공 판정은 실제 위협 상태의 몬스터 공격에만 열린다.
- 스왑 회피 성공 후 이전 캐릭터 쿨타임은 기존처럼 적용한다.
- 다단 공격 전체를 무효화하지 않도록 i-frame은 짧게 유지한다.
- 보스 대형 패턴은 필요 시 `canSwapEvade` 플래그로 예외 처리할 수 있게 한다.

### 실패 리스크

| 리스크 | 대응 |
--------|------|
| 스왑만 반복해 모든 공격을 무시 | 스왑 쿨타임 유지, 일반 스왑 무적 금지 |
| 몬스터 공격 판정과 회피 판정이 어긋남 | `EnemyCombat`의 실제 공격 위치/반경 API를 사용 |
| 피격과 스왑 회피가 같은 프레임에 충돌 | 교체 성공 직후 즉시 i-frame 적용 |
| 등장 공격/어시스트/카운터 중복 | 예약 우선순위 단일화 |
| 잔류 공격과 보상 중복 | 잔류 공격은 기존처럼 파티 게이지 충전 제외 유지 |

---

## 코드 변경 후보 파일

| 파일 | 변경 내용 |
------|-----------|
| `Assets/02.Scripts/Data/Party/PartyConfigSO.cs` | 스왑 회피 설정 필드 추가 |
| `Assets/02.Scripts/Manager/Party/PartyManager.cs` | 스왑 회피 판정, 우선순위, 설정 프로퍼티 추가 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 위협 스냅샷 API와 타이밍 기록 추가 |
| `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | 스왑 회피 큐, i-frame, 피격 차단 추가 |
| `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.cs` | 전용 공격 데이터 선택 경로 추가 완료 |
| `Assets/02.Scripts/Data/Combat/PlayerAttackDataSO.cs` | `swapEvadeCounterAttack` 추가 완료 |
| `Assets/02.Scripts/Data/Enum/AnimKey.cs` | `Player_SwapEvadeCounterAttack_1` 추가 완료 |
| `Assets/02.Scripts/Data/Combat/Editor/PlayerAttackDataSODrawer.cs` | 에디터 `회피` 탭 추가 완료 |

---

## 구현 전 확인 질문

1. 스왑 회피 성공 시 무조건 카운터 공격을 자동 발동할 것인가, 아니면 다음 공격 입력이 들어왔을 때만 발동할 것인가?
2. 스왑 회피 카운터는 `swapEvadeCounterAttack` 데이터를 우선 사용하고, 비어 있으면 기존 `entryAttack`으로 대체한다.
3. 보스의 대형 전멸기/잡기/가드불가 공격도 스왑 회피 가능하게 둘 것인가?
4. 스왑 회피 성공 시 파티 스킬 게이지를 보상으로 줄 것인가?

1차 구현 추천 답:

- 자동 발동.
- 1차는 기존 `entryAttack` 또는 일반 공격 재사용.
- 잡기/특수 대형 패턴은 기본 불가.
- 게이지 보상은 보류.

---

## 1차 구현 의사코드

```csharp
public bool RequestSwapTo(int targetIndex)
{
    if (!CanSwapTo(targetIndex)) return false;

    bool isSwapEvade = TryEvaluateSwapEvade(out EnemyAttackThreat threat);
    bool isAssist = !isSwapEvade && _player.GetCombat()?.IsPerfectDodgeWindow == true;

    _isSwapping = true;
    OnSwapStarted?.Invoke(_player, _player);

    if (!swap.SwapTo(targetType))
    {
        _isSwapping = false;
        return false;
    }

    if (isSwapEvade)
    {
        _player.BeginSwapEvadeIFrame(SwapEvadeIFrameDuration);
        _player.QueueSwapEvade(threat.Source, SwapEvadeCounterInputWindow);
    }
    else if (isAssist)
    {
        _player.QueueSwapAssist();
    }
    else if (TryFindEntryAttackTarget(swap.GetModelData(targetType), out var entryTarget))
    {
        _player.QueueEntryAttack(entryTarget);
    }

    RecordSwapCooldown(previousType);
    NotifyActivePlayerChanged();
    OnSwapCompleted?.Invoke(_player);
    return true;
}
```

---

## 검증 시나리오

### 성공 케이스

1. 몬스터가 Danger Ring 공격을 시작한다.
2. 링이 거의 닫힐 때 `PlayerSwap` 입력.
3. incoming 캐릭터로 교체된다.
4. 플레이어 HP가 감소하지 않는다.
5. incoming 캐릭터가 카운터 공격을 시작한다.
6. 이전 캐릭터에는 스왑 쿨타임이 적용된다.

### 실패 케이스

1. 몬스터가 공격하지 않을 때 `PlayerSwap` 입력.
2. 일반 스왑 또는 기존 등장 공격만 발동한다.
3. 무적 플래그가 켜지지 않는다.

### 경계 케이스

1. 몬스터 다단 히트 중 첫 히트 직후 스왑.
2. `swapEvadeGraceAfterHitStart` 안이면 성공, 이후면 실패.
3. 성공 시 i-frame 동안 후속 다단 히트는 무시된다.
4. i-frame 종료 후 남은 공격에는 정상 피격된다.

### 잔류 공격 결합 케이스

1. 플레이어가 공격 중이다.
2. 몬스터 공격 타이밍에 스왑한다.
3. outgoing 잔류 공격이 유지된다.
4. incoming 스왑 회피 카운터가 발동한다.
5. 잔류 공격과 카운터 공격이 서로 입력/피격 상태를 공유하지 않는다.

---

## 결론

스왑 회피 시스템은 기존 `PartyManager`, `PlayerActor` 공격 예약, `EnemyCombat` 텔레그래프 구조를 활용하면 작은 단계로 도입할 수 있다.

가장 중요한 설계 원칙은 다음이다.

1. 일반 스왑은 무적이 아니다.
2. 실제 몬스터 공격 위협이 있을 때만 스왑 회피가 성공한다.
3. 성공 즉시 짧은 i-frame을 켜서 같은 프레임 피격을 막는다.
4. 카운터 공격은 등장 공격/어시스트보다 높은 우선순위를 가진다.
5. 잔류 공격 시스템은 유지하고, 스왑 회피는 incoming 캐릭터의 진입 보상으로 분리한다.
