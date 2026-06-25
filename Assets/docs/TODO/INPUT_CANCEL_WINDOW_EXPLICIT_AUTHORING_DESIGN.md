# 명시적 캔슬 윈도우 저작 — 입력/캔슬 구간 방식 고도화 설계

> 작성일: 2026-06-25
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: **설계서(미구현 계획)**. 본 문서의 코드 스니펫은 모두 **의사코드 스케치**이며 실제 구현이 아니다.
> 레퍼런스: 격투/액션 장르의 cancel window·hit-confirm 관례(DMC/베요네타의 액션 캔슬, 격투게임의 gatling/special-cancel), 본 프로젝트 기존 전투 에디터 오버레이.
> 선행/연관: `project_combat_editor_overlay`(전투 트랙·프레임 테이블), `project_interrupt_cancel_system`(PlayerInterruptAction 마스크 + 콜리전 캔슬 윈도우).

---

## 0. 개요

본 문서는 플레이어 공격의 **캔슬 허용 "구간(When)"** 을 현재의 *콜리전 파생(암묵)* 방식에서 **per-attack 명시적 저작 데이터**로 승격하는 설계다.

핵심 한 줄: **"무엇을 캔슬할지(interruptActions 마스크)"는 이미 데이터지만, "언제 캔슬할지"는 히트박스 콜리전의 여집합으로 자동 결정되어 저작·튜닝이 불가능하다. 이 "언제"를 프레임 단위로 저작 가능한 1급 데이터로 만든다.**

이 변경은 게임플레이 규칙을 즉시 바꾸지 않는다(§5 하위호환). 저작 윈도우가 없는 기존 공격은 현행 콜리전-off 동작을 그대로 폴백으로 유지하며, 윈도우를 명시한 공격만 새 규칙을 따른다.

---

## 1. 현황 진단

### 1.1 책임 분리 (이미 잘 되어 있음 — 재발명 금지)

| 축 | 메커니즘 | 위치 |
|---|---|---|
| **무엇을(What) 캔슬** | `PlayerInterruptAction` 마스크(`interruptActions`) — per-attack 데이터 | `Data/Enum/PlayerInterruptAction.cs`, `AttackInfoBase` |
| **언제(When) 캔슬** | `IsCancelWindowOpen = !IsPossibleCollide` — **콜리전 활성의 여집합(암묵)** | `PlayerCombat.cs:213,219` |
| **입력 보존** | 액티브 히트 중 버퍼 만료정지 + 재개 시 타임스탬프 시프트 | `InputBuffer.SetExpiryPaused`, `PlayerAttackState.cs:378` |
| **우선순위 소비** | Dodge→Jump→Dash→Guard→공격타입(첫 매칭 종료) | `PlayerInterruptResolver.TryInterrupt` |
| **Move(걷기) 캔슬** | `moveCancelDelayAfterLastHit` 별도 후딜 게이트(윈드업 페인트 방지) | `PlayerAttackState.cs:621` |
| **가드 캔슬 게이트** | `_hasActiveHitFired`(액티브 1회 후 리커버리/멀티히트 간격만) | `PlayerAttackState.cs:388` |

### 1.2 핵심 한계

`PlayerCombat.cs:219`:

```csharp
public bool IsCancelWindowOpen => !IsPossibleCollide;   // 콜리전 활성의 여집합
```

이 한 줄이 모든 캔슬 타이밍을 결정한다. 결과적으로:

1. **윈드업 전체 + 리커버리 전체 + 멀티히트 간격 전체**가 무조건 캔슬 가능 구간이 된다. "선딜 초반은 캔슬 불가, 후반만 허용" 같은 **부분 윈도우 저작이 불가능**하다.
2. **캔슬 타이밍이 히트박스 타이밍에 종속**된다. 히트박스를 옮기면 캔슬 구간도 따라 바뀐다 — 독립 튜닝 불가.
3. **무브 커밋이 약하다.** 윈드업을 통째로 캔슬할 수 있어 "지른 공격에 대한 책임"이 옅다.
4. 에디터 오버레이의 "✂ 캔슬" 트랙도 이 암묵 규칙을 그대로 반영한다 — `ComputeComplementSpans(collisions, total)`로 **콜리전의 여집합을 그려줄 뿐**, 저작된 구간이 아니다(`MotionSetWindow.CombatOverlay.cs:313`).

> 즉 현재 "캔슬 구간"은 *디자이너가 정한 것이 아니라 히트박스 배치의 부산물*이다. 본 설계는 이를 **디자이너가 정하는 1급 데이터**로 바꾼다.

---

## 2. 설계 목표와 비목표

**목표**
- per-attack 으로 캔슬 가능 구간을 **정규화 시간축(0~1) 다중 스팬**으로 저작.
- 저작 없으면 **현행 콜리전-off 동작을 폴백**으로 유지(무회귀).
- 전투 에디터 오버레이에 **저작 캔슬 트랙을 시각화**(폴백과 구분 표기).
- 향후 규칙 층(on-hit 조건·최소 커밋)을 얹을 **인터페이스 훅**만 마련.

**비목표(이번 범위 밖)**
- on-hit/on-whiff 조건부 캔슬의 *실제 구현* (별도 축).
- 버퍼 우선순위/leniency 데이터화 (별도 축).
- 적(Enemy) 캔슬 — 본 설계는 플레이어 한정. Enemy는 BT/상태머신이 별도 관할.

---

## 3. 데이터 모델

### 3.1 거처

캔슬 윈도우는 **공격 단위** 속성이므로 `hitPhases`와 형제로 `AttackInfoBase`에 둔다(`CombatData.cs:227,333`).

```csharp
// (스케치) CombatData.cs — AttackInfoBase 내부
[Serializable]
public struct CancelWindowSpan
{
    [Range(0f, 1f)] public float start;   // 정규화 시간(0=모션 시작, 1=종료)
    [Range(0f, 1f)] public float end;
    // 비우면(None) 공격의 interruptActions 마스크를 그대로 사용.
    // 지정하면 이 스팬에서만 허용되는 액션을 마스크로 좁힌다(부분 구간별 차등 캔슬).
    public PlayerInterruptAction maskOverride;
}

[Header("캔슬 윈도우 (비우면 콜리전-off 폴백)")]
[Tooltip("이 공격에서 캔슬이 허용되는 정규화 구간. 비어 있으면 기존처럼 '콜리전 비활성 = 캔슬 가능'으로 동작한다.")]
public List<CancelWindowSpan> cancelWindows = new();
```

### 3.2 시간축 — 정규화(0~1) 채택

**정규화 시간축**을 쓴다. 근거:

- 기존 오버레이/타임라인이 이미 정규화 스팬으로 동작한다(`CollectCollisionSpans`, `ComputeComplementSpans`, `OverlaySpan.start/end`가 0~1). 동일 좌표계라 시각화·검증 로직을 재사용한다.
- 모션 클립 길이가 바뀌어도 비율이 유지된다(프레임 절대값은 클립 교체 시 깨진다).
- 트레이드오프: 프레임 단위 정밀 튜닝은 정규화×길이로 환산해야 한다. 에디터가 현재 프레임도 병기하므로(§6) 실무 영향은 작다.

> **미해결 결정 D1:** 일부 디자이너는 "프레임 18~30" 식 절대 프레임을 선호할 수 있다. 1차는 정규화로 가고, 필요 시 에디터에 "프레임 입력→정규화 자동 환산" 보조 필드를 추가한다.

### 3.3 마스크와의 직교성 유지

`interruptActions`(무엇)와 `cancelWindows`(언제)는 **직교**를 유지한다.

- 전역 허용 액션 = `interruptActions`(공격 단위).
- 스팬별 `maskOverride`가 있으면 그 구간에서는 **교집합**으로 좁힌다(예: 후딜 후반은 Dodge만, 멀티히트 간격은 Light만).
- `maskOverride == None` 이면 전역 마스크를 그대로 쓴다.

이로써 "선딜 후반엔 회피만, 리커버리엔 모든 캔슬" 같은 **구간별 차등 캔슬**이 가능해진다 — 현행 모델이 표현 못 하던 것.

---

## 4. 런타임 평가

### 4.1 단일 진입점 유지

현행 게이트는 `PlayerAttackState.cs:386`의 `_combat.IsCancelWindowOpen` 단일 분기다. 이 진입점을 보존하고 내부 평가만 확장한다.

```csharp
// (스케치) PlayerCombat — 현행 단일 bool을 윈도우 인지로 확장
public bool IsCancelWindowOpen => ResolveCancelMask(out _) != PlayerInterruptAction.None;

// 현재 정규화 시점에서 허용되는 캔슬 마스크를 산출. 호출부(PlayerAttackState)는
// 이 마스크를 TryInterrupt에 넘겨 '무엇을' 거른다(기존 _currentAttack.interruptActions 대체).
public PlayerInterruptAction ResolveCancelMask(out bool fromAuthored)
{
    fromAuthored = false;
    var baseInfo = _currentAttackInfoBase;
    var global = _currentAttack != null ? _currentAttack.interruptActions : PlayerInterruptAction.None;

    // 폴백: 저작 윈도우가 없으면 현행 규칙 그대로(콜리전 OFF에서 전역 마스크 허용).
    if (baseInfo == null || baseInfo.cancelWindows == null || baseInfo.cancelWindows.Count == 0)
        return IsPossibleCollide ? PlayerInterruptAction.None : global;

    fromAuthored = true;
    float t = CurrentNormalizedTime();   // 액션 러너/모션 진행도(0~1)
    PlayerInterruptAction allowed = PlayerInterruptAction.None;
    foreach (var span in baseInfo.cancelWindows)
    {
        if (t < span.start || t > span.end) continue;
        var spanMask = span.maskOverride == PlayerInterruptAction.None ? global : (global & span.maskOverride);
        allowed |= spanMask;
    }
    return allowed;
}
```

`PlayerAttackState`는 `TryInterrupt(controller, _currentAttack.interruptActions, …)`를 `TryInterrupt(controller, _combat.ResolveCancelMask(out _), …)`로 바꾼다. 우선순위·버퍼 소비 로직(`PlayerInterruptResolver`)은 **무변경**.

### 4.2 정규화 진행도 소스

`CurrentNormalizedTime()`은 액티브 히트 페이즈를 구동하는 동일 타임라인에서 얻는다(`CombatActionRunner`/`CombatActionInstance`의 진행도 또는 Animancer state.NormalizedTime). 히트 검출과 **같은 시계**를 써야 캔슬·히트 타이밍이 프레임 정합한다.

### 4.3 가드/무브 캔슬과의 관계

- **가드 캔슬:** `_hasActiveHitFired` 게이트(`PlayerAttackState.cs:388`)는 유지. 저작 윈도우에 Guard가 포함돼도 "액티브 1회 후"라는 안전장치는 남긴다(윈드업 가드 튕김 방지).
- **무브 캔슬:** `moveCancelDelayAfterLastHit`(`PlayerAttackState.cs:621`)는 별도 축(눌림 상태)이라 본 윈도우와 독립 유지. 단, 장기적으로 무브 캔슬도 `cancelWindows`의 한 스팬으로 흡수 가능(§7 통합안).

---

## 5. 하위호환·마이그레이션

**무회귀가 1순위 원칙.**

- `cancelWindows`가 빈 리스트인 모든 기존 공격 = §4.1 폴백 경로 = **현행 콜리전-off 동작과 비트 동일**.
- 신규 직렬화 필드라 기존 에셋은 빈 리스트로 역직렬화 → 자동 폴백. 데이터 마이그레이션 불필요.
- 점진 도입: 핵심 무기/대표 공격부터 윈도우를 저작해 체감을 검증하고, 나머지는 폴백으로 둔다.

> **검증 포인트:** 폴백 경로가 기존과 동일함을 회귀로 확인 — 동일 공격에서 캔슬 가능 프레임 집합이 변경 전후 일치해야 한다(§9).

---

## 6. 에디터 시각화 (전투 오버레이)

현재 `MotionSetWindow.CombatOverlay.cs`는 캔슬 트랙을 **콜리전 여집합**으로 그린다(`ComputeComplementSpans`, 313행). 이를 저작 인지로 승격한다:

- **저작 윈도우 있음:** `cancelWindows`를 그대로 트랙 스팬으로 렌더(실선). 스팬별 `maskOverride`를 라벨로 표기(예: `✂ 캔슬 [Dodge]`).
- **저작 윈도우 없음(폴백):** 기존 complement 트랙을 **점선/흐림**으로 렌더해 "자동 추론(미저작)"임을 시각 구분.
- 프레임 병기: `ComputeFrameMetrics`(선딜/후딜)와 동일 축이므로, 캔슬 스팬에도 `start*length`/`end*length` 프레임을 병기.
- 편집: 스팬 드래그로 start/end 조정(기존 OverlaySpan 렌더 재사용). `RefreshCombatOverlayTracks`의 상시 실행 분리 함정(`project_combat_editor_overlay`)을 그대로 준수.

이로써 디자이너가 **히트박스·콤보 윈도우·캔슬 윈도우를 한 타임라인에서 정렬**해 보고 튜닝한다.

---

## 7. 규칙 층 확장 훅 (향후, 인터페이스만)

본 설계는 다음 축을 **막지 않도록** 구조만 연다(구현은 별도 문서):

- **on-hit 조건부 캔슬:** `CancelWindowSpan`에 `requiresHit` 플래그를 추가하고, `ResolveCancelMask`가 "이 공격이 무언가를 맞췄는가"(`_hitTargets.Count > 0` 등)를 함께 검사. 히트 컨펌 숙련층 + 위프 캔슬 제한. 연계/장르 ①층과 정합.
- **최소 커밋 가드:** 공격타입 캔슬에도 `_hasActiveHitFired` 유사 게이트를 옵션화 — 윈드업 무한 캔슬 연쇄 차단.
- **무브 캔슬 통합:** `moveCancelDelayAfterLastHit`를 Move 스팬으로 흡수해 단일 모델로 수렴.

이 훅들은 `CancelWindowSpan`/`ResolveCancelMask`에 필드·분기를 더하는 형태라 **데이터·진입점 재설계 없이** 증분 가능하다.

---

## 8. 단계별 구현 계획

| Phase | 내용 | 산출물 |
|---|---|---|
| **P1 데이터** | `CancelWindowSpan` 구조 + `AttackInfoBase.cancelWindows` 추가(빈 리스트 기본) | 컴파일·역직렬화 무회귀 |
| **P2 런타임** | `ResolveCancelMask`/`CurrentNormalizedTime` 구현, `PlayerAttackState` 게이트 교체, 폴백 보존 | 저작 없는 공격 = 기존과 동일 |
| **P3 에디터** | 오버레이 캔슬 트랙을 저작 인지로 승격(실선/점선 구분, 프레임 병기, 드래그 편집) | 디자이너 저작 가능 |
| **P4 저작·튜닝** | 대표 무기 공격에 윈도우 저작, 체감 튜닝 | 밸런스/조작감 |
| **P5(선택) 규칙층** | on-hit/최소커밋 훅 활성화(별도 결정) | 깊이 |

P1~P3까지가 "방식 고도화"의 본체이며, P4는 콘텐츠, P5는 차기 축이다.

---

## 9. 리스크·검증 절차

**리스크**
- **시계 불일치:** `CurrentNormalizedTime`이 히트 검출과 다른 시계를 쓰면 캔슬·히트가 1프레임 어긋난다 → 반드시 동일 타임라인 소스 사용(§4.2).
- **폴백 회귀:** 폴백 경로가 기존과 미세하게 달라지면 모든 공격에 영향 → P2에서 비트 동일 검증 필수.
- **버퍼 만료정지 정합:** 캔슬창이 좁아지면(저작) `SetExpiryPaused` 구간과 실제 캔슬 가능 구간이 어긋날 수 있다. 만료정지는 "콜리전 활성"이 아니라 "캔슬 마스크가 None인 구간" 기준으로 재정렬할지 검토(미해결 결정 D2).

**검증(Unity PlayMode)**
1. **무회귀:** 윈도우 미저작 공격에서 캔슬 가능 프레임이 변경 전후 동일.
2. **부분 윈도우:** 선딜 후반만 Dodge 허용으로 저작 → 선딜 초반 회피 입력 무시, 후반 회피 발동.
3. **구간별 차등:** 멀티히트 간격엔 Light만, 리커버리엔 전체 → maskOverride 동작 확인.
4. **시계 정합:** 캔슬 가능 첫 프레임과 오버레이 표기 프레임 일치.
5. **버퍼 보존:** 좁은 윈도우에서도 선입력이 만료정지로 보존돼 창 열림 순간 소비.

---

## 10. 미해결 결정 요약

- **D1 시간축:** 정규화 1차 채택. 절대 프레임 입력 보조는 에디터 환산 필드로 후속.
- **D2 만료정지 기준:** `IsPossibleCollide`(현행) vs `ResolveCancelMask==None`(저작 정합). P2에서 결정.
- **D3 무브 캔슬 통합:** 별도 유지(1차) vs Move 스팬 흡수(통합). P5 이후.
- **D4 규칙층 우선순위:** on-hit 조건과 최소 커밋 중 어느 것을 먼저 — 별도 축 검토.
