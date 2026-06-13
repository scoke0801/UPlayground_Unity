# GameTime / HitStop 시스템 가이드

## 개요

게임의 **시간 흐름 제어**를 담당하는 두 매니저입니다.

- **`GameTimeManager`** — `Time.timeScale` 의 단일 소유권자. 동시에 여러 시스템이 감속을 요청해도 **가장 강한(scale이 가장 낮은) 요청**이 적용되도록 큐를 관리하고, 게임 일시정지(Pause), 누적 플레이 시간을 추적.
- **`HitStopHandler`** — 타격 정지(HitStop) 연출 전담. `GameCombatManager` 산하 핸들러로 동작하며, `GameTimeManager`에 id 기반 timeScale 요청을 등록하고, Post-Process Volume 페이드와 액터별 `LocalTimeScale` 조작을 함께 수행. 외부 접근은 `GameCombatManager.Instance.HitStop`.

핵심 특징:

- **id 기반 다중 요청 큐** — `Request(scale) → id`, `Release(id)` 페어. 활성 요청 중 최저 scale이 자동 적용.
- **Pause는 최우선** — Pause 중에는 `Time.timeScale = 0`, 재개 시 활성 요청 scale로 복구.
- **Plays seconds는 unscaled** — `TotalPlaySeconds`는 Pause를 제외한 unscaled 시간 누적 (HitStop 중에도 정상 누적).
- **HitStop 강도 프리셋** — `Light/Medium/Heavy/Critical/PlayerDie/PlayerGuard` 6단계.
- **PlayerGuard는 actor-only** — timeScale을 건드리지 않고 플레이어 외 액터만 슬로우.
- **Post-Process Volume 페이드** — Addressables `SlowMoveVolume` 프리팹을 인스턴스화하여 HitStop 중 시각 효과 적용.

---

## 아키텍처

```
GameTimeManager (BaseManager<T>, IManager)
├── _requests : Dictionary<int, float>     id → 요청 scale
├── _activeScale : float                   현재 적용된 scale (Pause 해제 시 복구용)
├── IsPaused, TotalPlaySeconds, IsSlowed
│
├── Request(float scale) → int id          큐에 등록 + ApplyLowest()
├── Release(int id)                        큐에서 제거 + ApplyLowest()
├── ReleaseAll()                           강제 전체 초기화
│
├── SetPause(bool) / TogglePause()         Time.timeScale = pause ? 0 : _activeScale
├── SetTotalPlaySeconds(float)             세이브 로드용
└── FormatPlayTime() → "HH:MM:SS"


GameCombatManager (BaseManager<T>, IManager)
└── HitStopHandler (GameHandlerBase)
    ├── _globalCoroutines : Dictionary<int, Coroutine>    전역 HitStop 코루틴
    ├── _actorCoroutines  : Dictionary<GameActor, ActorTimeScaleRequest>  액터별 LocalTimeScale 슬로우
    ├── _volume : Volume                       Addressables: "SlowMoveVolume"
    │
    ├── Execute(HitStopIntensity)              프리셋 기반
    ├── Execute(duration, scale=0.1)           커스텀 + 강도 비교 후 약한 요청 정리
    ├── ExecuteActorOnly(actor, duration, animSpeed)
    ├── ExecuteLocalImpact(attacker, victim, duration, localTimeScale, includeAttacker)
    ├── Stop() / StopActor() / StopAllActors() / ResetActorTimeScale()
    └── IsHitStopping (= GameTimeManager.IsSlowed), IsActorHitStopping(actor)


협력:
   PlayerCombat / EnemyCombat / KillCam ──► GameCombatManager.Instance.HitStop.Execute(HitStopIntensity.X)
                                                 │
                                                 ▼
                              GameTimeManager.Request(scale)  ──► Time.timeScale 적용
                                                 │
                                                 │  duration 후
                                                 ▼
                              GameTimeManager.Release(id)    ──► 큐에서 제거
                                                 │
                              ApplyLowest() : 남은 요청 중 최저 scale or 1.0
```

### 파일 구조

```
Assets/02.Scripts/Manager/
├── GameTimeManager.cs              timeScale 큐 + Pause + TotalPlaySeconds
└── Combat/
    ├── GameCombatManager.cs        전투 핸들러 호스트
    └── HitStopHandler.cs           HitStop 프리셋 + Volume 페이드 + Actor LocalTimeScale 슬로우
```

---

## 핵심 클래스 / API

### GameTimeManager

#### Pause

| API | 시그니처 | 용도 |
|-----|----------|------|
| `IsPaused` | `bool (get)` | 현재 일시정지 여부 |
| `SetPause(bool)` | — | 일시정지 토글. `Time.timeScale = pause ? 0 : _activeScale` |
| `TogglePause()` | — | 위 단축 |
| `OnPauseChanged` | `static event Action<bool>` | Pause 상태 변화 알림 |

#### TimeScale 요청 큐

| API | 시그니처 | 용도 |
|-----|----------|------|
| `Request(float scale)` | `→ int id` | 새 요청 등록. scale은 [0.001, 1.0]로 클램프 |
| `Release(int id)` | — | id 매칭 요청 해제 |
| `ReleaseAll()` | — | 모든 요청 강제 해제 (씬 전환 등) |
| `IsSlowed` | `bool (get)` | `_activeScale < 1f` |

내부 로직 — `ApplyLowest()`:

```csharp
float lowest = 1f;
foreach (var v in _requests.Values)
    if (v < lowest) lowest = v;

_activeScale = lowest;
if (!IsPaused) Time.timeScale = _activeScale;
```

활성 요청 중 **가장 낮은 scale**이 시간 스케일로 적용됨. 빈 큐는 1.0으로 복구.

#### 누적 시간

| API | 용도 |
|-----|------|
| `TotalPlaySeconds` | 누적 플레이 시간(초). Pause 제외, unscaledDeltaTime 누적 |
| `SetTotalPlaySeconds(float)` | 세이브 로드용 |
| `FormatPlayTime()` | `"HH:MM:SS"` |

> **Note:** TotalPlaySeconds는 `unscaledDeltaTime` 기반이라 HitStop 중에도 정상 누적된다. 게임 외부 시계 기반의 진짜 플레이 시간.

### HitStopHandler

> 외부 접근: `GameCombatManager.Instance.HitStop`

#### HitStopIntensity 프리셋

| Intensity | duration | timeScale | 비고 |
|-----------|----------|-----------|------|
| `Light` | 0.05s | 0.15 | 약한 타격 |
| `Medium` | 0.08s | 0.10 | 일반 타격 |
| `Heavy` | 0.12s | 0.05 | 강타 |
| `Critical` | 0.15s | 0.02 | 치명타 |
| `PlayerDie` | 1.00s | 0.02 | 플레이어 사망 연출 |
| `PlayerGuard` | actor-only | (timeScale 변경 없음) | 가드 — 플레이어 외 액터만 슬로우 |

> `PlayerGuard`는 `Time.timeScale` 대신 `GameObjectManager.SetGlobalTimeScaleExceptPlayer(0.05f, 3f)` 호출 + Volume weight = 1로 시각 처리. 플레이어 입력은 정상 속도 유지.

#### Public API

| API | 시그니처 | 용도 |
|-----|----------|------|
| `Execute()` | — | 기본값(0.08s/0.1) HitStop |
| `Execute(HitStopIntensity)` | — | 프리셋 |
| `Execute(duration, timeScale=0.1f)` | — | 커스텀. 더 강한 요청이면 약한 활성 요청을 정리 후 등록 |
| `Stop()` | — | 모든 전역 HitStop 강제 종료 |
| `ExecuteActorOnly(actor, duration, animSpeed=0.1f)` | — | 액터의 `LocalTimeScale`만 변경. Animator/KCC/상태 업데이트에 함께 반영 |
| `ExecuteLocalImpact(attacker, victim, duration, localTimeScale=0.1f, includeAttacker=true)` | — | 공격자/피격자만 로컬 히트스톱. 적용 대상이 없으면 전역 HitStop 폴백 |
| `StopActor(actor)` / `StopAllActors()` | — | 액터 슬로우 해제 |
| `ResetActorTimeScale()` | — | Volume 즉시 페이드아웃 + 글로벌 타임스케일 리셋 |
| `IsHitStopping` | `bool (get)` | `GameTimeManager.IsSlowed` |
| `IsActorHitStopping(actor)` | `→ bool` | 액터 슬로우 여부 |

### LocalTimeScale 기반 HitStop 검토

명조(Wuthering Waves)식 타격감은 화면 전체를 오래 멈추는 방식보다, 적중 순간의 **공격자/피격자만 짧게 붙잡고 입력·카메라·UI는 계속 반응**하게 두는 쪽이 조작감에 유리하다. UPlayground의 현재 구조에서는 이 목적에 `GameActor.LocalTimeScale`을 사용할 수 있다.

검토 결과:

| 항목 | 판단 | 근거 |
|------|------|------|
| Animator 정지 | 가능 | `GameActor.LocalTimeScale` setter가 `ActorAnimator.Speed`에 반영 |
| 상태 머신 감속 | 가능 | `ActorMovementController.Update()`가 `Actor.DeltaTime` 사용 |
| KCC 이동 감속 | 가능 | `KCCSimulator`가 `LocalTimeScale`별로 Motor를 그룹핑하고 `baseDt * scale`로 시뮬레이션 |
| 입력 유지 | 가능 | `Time.timeScale`을 낮추지 않으므로 `InputManager`, UI, 카메라 입력은 전역 감속 영향을 받지 않음 |
| 카메라 쉐이크 유지 | 가능 | 전역 timeScale을 낮추지 않아 쉐이크/카메라 모드가 정상 틱 |
| 전역 슬로모션 | 별도 필요 | 킬캠, 사망, 컷신성 연출은 여전히 `GameTimeManager.Request()` 기반 전역 감속이 적합 |

따라서 일반 공격 적중은 `ExecuteLocalImpact()`를 기본 경로로 사용하고, 킬캠/사망/궁극기/연출형 슬로모션은 기존 `Execute(duration, timeScale)` 전역 경로를 유지한다.

권장 분류:

| 상황 | 권장 경로 | 이유 |
|------|-----------|------|
| 평타/스킬 적중 | `ExecuteLocalImpact(attacker, victim, ...)` | 조작·카메라 반응성 유지 |
| 패링/저스트 가드 | `PlayerGuard` 또는 별도 로컬 슬로우 | 플레이어 반격 창 유지 |
| 보스 브레이크/마무리 | 로컬 HitStop + 별도 카메라/FX, 필요 시 짧은 전역 보강 | 성공 보상은 크게, 입력 지연은 짧게 |
| 플레이어 사망/킬캠 | 전역 HitStop | 연출 우선 |
| 메뉴 Pause | `GameTimeManager.SetPause()` | 모든 시뮬레이션 정지 |

명조 레퍼런스 기준 1차 권장값:

| 이벤트 | duration | scale | 적용 대상 |
|--------|----------|-------|-----------|
| 약한 평타 적중 | 0.025-0.04s | 0.15-0.25 | attacker + victim |
| 일반 스킬/중타 | 0.045-0.07s | 0.08-0.15 | attacker + victim |
| 강공격/차지 | 0.075-0.11s | 0.03-0.08 | attacker + victim |
| 패링/카운터 | 0.09-0.14s | 0.01-0.05 | victim 중심, 필요 시 attacker 포함 |
| 보스 브레이크/마무리 | 0.12-0.18s | 0.01-0.04 | victim 중심 + 카메라/FOV/FX |

현재 적용:

- `CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback()`는 플레이어 공격 적중 시 `ExecuteLocalImpact()`를 기본으로 호출한다.
- 로컬 정지만으로 체감이 약한 문제를 보완하기 위해, 적중 순간에는 `0.025~0.04s / timeScale 0.2`의 짧은 전역 펄스를 함께 건다. 긴 정지는 로컬에 맡기고 전역 펄스는 입력 지연이 커지지 않도록 짧게 제한한다.
- `AttackData.reactionData.hitStopDuration`이 있으면 해당 값을 우선 사용한다.
- 자동 반응 데이터가 없을 때의 로컬 폴백은 약타 `0.06s/0.03`, 강타 `0.10s/0.015`, 스킬·차지 `0.13s/0.01`.
- 적용 대상이 없으면 기존 전역 `Execute(duration, scale)`로 폴백한다.

적용 범위:

| 공격 | 적용 여부 | 경로 |
|------|-----------|------|
| 약/강 일반 공격 | 적용 | `PlayerCombat.ApplyHitFeedback()` → `ApplyPlayerAttackHitFeedback()` |
| 대시/점프/점프대시 공격 | 적용 | `AttackKind.DashAttack` / `JumpAttack` 강타 폴백 |
| 스킬/차지 공격 | 적용 | `AttackKind.SkillAttack` / `ChargeAttack` 스킬 폴백 |
| 퍼펙트 가드/패리/회피/스왑 회피 카운터 | 적용 | `AttackData.isCounterAttack` 우선 분기 |
| 브레이크 특수공격 | 적용 | `PlayerSpecialBreakAttackState` → `ApplyPlayerSpecialBreakHitStop()` |
| 킬 적중 | 적용 | 일반 타격 카메라/HitStop을 먼저 보장한 뒤 `TryPlayKill()` 추가 시도 |

#### 강도 비교 로직 (`Execute(duration, scale)`)

```csharp
private bool ShouldReplaceExisting(float newScale)
{
    float current = GameTimeManager.IsSlowed ? Time.timeScale : 1f;
    return newScale < current;   // 더 강한 효과
}

private void StopWeakerThan(float newScale)
{
    // 활성 코루틴 전부 중단 + GameTimeManager.Release
}
```

> 새 요청이 **더 강한** 효과(scale 더 낮음)이면 기존 요청을 모두 정리하고 새로 등록. **더 약하거나 같으면** 큐에만 추가되고 현재 scale은 유지된다.

#### Volume 페이드

`SlowMoveVolume` 프리팹(Addressables)을 매니저 GameObject 자식으로 인스턴스화하여 보유. `_currentWeight`를 SmoothDamp(unscaledDeltaTime)로 보간해 `Volume.weight`에 적용.

| 상황 | _targetWeight | _transitionTime |
|------|---------------|-----------------|
| HitStop 진입 (큐에 첫 요청) | 1f (코루틴 측에서 처리 보강 가능) | 짧음 |
| 모든 HitStop 종료 | 0f | 0.05s 페이드아웃 |
| `PlayerGuard` 진입 | currentWeight=1, target=0 | 3s |
| `ResetActorTimeScale()` | 0f | 0s (즉시) |

---

## 사용 예시

### 1. 단순 일시정지

```csharp
GameTimeManager.Instance.TogglePause();

// 구독자 측
GameTimeManager.OnPauseChanged += isPaused =>
{
    if (isPaused) ShowPauseMenu();
    else          HidePauseMenu();
};
```

### 2. 타격 시 HitStop (가장 흔한 케이스)

```csharp
// PlayerCombat.cs : 일반 히트
GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.Medium);

// 강타 / 치명타
GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.Heavy);
GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.Critical);

// 플레이어 사망 연출
GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.PlayerDie);
```

### 3. 가드 성공 (PlayerGuard)

```csharp
// PlayerGuardState — 퍼펙트 가드 성공 시
GameCombatManager.Instance.HitStop.Execute(HitStopHandler.HitStopIntensity.PlayerGuard);
```

플레이어는 정상 속도, 적은 0.05배속 3초간 → 반격 창 형성.

### 4. 액터 단독 슬로우 (적 잡기 / 그랩 연출)

```csharp
// 적의 Animator/상태/KCC 이동을 함께 잠시 정지
GameCombatManager.Instance.HitStop.ExecuteActorOnly(grabbedEnemy, duration: 1.5f, animSpeed: 0f);

// 정리
GameCombatManager.Instance.HitStop.StopActor(grabbedEnemy);
```

### 5. 커스텀 timeScale 요청 (HitStop 외 용도)

```csharp
// 버프 효과: 0.5배속 5초간
int id = GameTimeManager.Instance.Request(0.5f);
StartCoroutine(ReleaseAfter(id, 5f));

IEnumerator ReleaseAfter(int id, float t)
{
    yield return new WaitForSecondsRealtime(t);
    GameTimeManager.Instance.Release(id);
}
```

> 이 패턴이면 **동시에 더 강한 HitStop이 발화해도 그것의 scale이 적용**되고, HitStop 종료 후 자동으로 0.5배속으로 복귀한다. 큐 기반이라 충돌이 일어나지 않음.

### 6. 누적 플레이 시간 표시 (UI)

```csharp
playTimeLabel.text = GameTimeManager.Instance.FormatPlayTime();
```

세이브 로드 시:

```csharp
GameTimeManager.Instance.SetTotalPlaySeconds(saveData.playSeconds);
```

---

## 셋업 방법

1. **GameManager 등록 순서 확인**
   - `GameCombatManager`(산하 `HitStopHandler`)는 `GameTimeManager` 이후에 호출되어야 한다. 현재 GameManager 등록 순서에서 `GameTimeManager`가 더 늦게 등록되지만 `Request/Release`는 `Init` 단계가 아닌 런타임 호출이라 문제없음. 둘 다 IManager에 등록되어 있어야 함.
2. **SlowMoveVolume 프리팹 등록**
   - URP Post-Process `Volume` 컴포넌트가 부착된 GameObject 프리팹 작성
   - Profile에 Vignette / Color Adjustment 등 슬로우 시각 효과 셋업
   - Addressables 키 `SlowMoveVolume` 으로 등록
3. **인스펙터 기본값 (선택)**
   - `_defaultHitStopDuration`(0.08s) / `_defaultTimeScale`(0.1) 인스펙터에서 조정 가능
4. **타격 측 호출 추가**
   - `PlayerCombat`/`EnemyCombat`의 데미지 적용 지점에서 강도 매핑하여 `Execute(intensity)` 호출

---

## 주의 사항

- **timeScale 직접 수정 금지.** 다른 코드에서 `Time.timeScale = X`를 직접 쓰면 큐 모델이 깨진다. 항상 `GameTimeManager.Request/Release`를 거칠 것.
- **Request/Release 페어링 필수.** id를 받았으면 반드시 Release. 누락하면 timeScale이 영구 슬로우 상태로 남는다. try/finally 또는 코루틴 종료 보장 패턴 권장.
- **WaitForSeconds vs WaitForSecondsRealtime.** HitStop 중에도 만료되어야 하므로 매니저 내부는 모두 `WaitForSecondsRealtime` 사용. 외부에서 직접 timeScale 요청 시 마찬가지.
- **ReleaseAll은 위험.** 다른 시스템(버프, 카메라 이펙트의 TimeScale)도 큐에 들어 있을 수 있다. `ReleaseAll`은 씬 전환 등 명확한 정리 시점에만 사용.
- **Pause 중에는 활성 scale이 보존된다.** Pause 해제 시 `_activeScale`로 복구되므로, Pause 중에 Request된 요청은 정상적으로 큐에 누적됨. 다만 코루틴 기반 자동 Release는 Pause 중 시간이 흐르지 않으므로(`WaitForSecondsRealtime` 제외) 실제 Release 타이밍에 주의.
- **PlayerGuard는 timeScale을 건드리지 않는다.** GameTimeManager에 등록되지 않으므로 `IsSlowed`는 false 반환. UI/시스템이 "HitStop 중인지" 판단할 때 Guard도 포함하려면 별도 플래그 필요.
- **로컬 HitStop은 `IsHitStopping`에 잡히지 않는다.** `IsHitStopping`은 전역 `GameTimeManager.IsSlowed` 기준이다. 액터별 정지 여부는 `IsActorHitStopping(actor)`로 확인한다.
- **액터 슬로우의 복원값.** 매니저는 ActorOnly 시작 시점의 `LocalTimeScale`을 저장하고 종료 시 해당 값으로 복원한다. 도중에 다른 시스템이 같은 액터의 `LocalTimeScale`을 직접 변경하면 복원값이 의도와 달라질 수 있으므로, 액터 단위 시간 조작은 매니저 경로로 일원화한다.
- **`ResetActorTimeScale`의 의도.** 함수명은 "Actor"지만 실제로는 Volume 페이드아웃 + GlobalTimeScale(액터 LocalTimeScale) 리셋. 큐에 등록된 timeScale 요청은 건드리지 않는다. 큐 정리는 Release/Stop 사용.
- **Volume 인스턴스는 매니저 자식.** 씬 전환 시 매니저가 살아 있다면 Volume도 살아남는다. `OnSceneChanged`에서 Stop/StopAllActors는 호출되지만 Volume 자체는 유지.

---

## 확장 포인트

### 신규 HitStop 강도 추가

`HitStopIntensity` enum에 멤버 추가 → `Execute(HitStopIntensity)` switch에 분기 추가. duration/scale 한 줄.

### timeScale 외 효과 묶기

현재 `Execute`는 timeScale + Volume 두 가지만 변경. 추가로 카메라 이펙트나 사운드 로우패스 같은 효과를 묶고 싶다면 `Execute` 내부에서 `CameraManager.PlayEffect(...)` 등을 함께 호출하거나, **HitStopProfile** SO를 만들어 (timeScale, duration, volumeWeight, cameraEffect, sound) 일괄 데이터로 외부화.

### Pause 시 무시할 시스템 화이트리스트

현재 Pause는 전역. 일부 시스템(설정 메뉴 애니메이션 등)을 Pause 영향에서 제외하려면 `unscaledDeltaTime` 기반으로 직접 갱신하거나, Manager의 IsPaused 플래그를 보고 자체 갱신 분기를 둔다. 또는 별도 LocalTimeScale 풀 도입.

### timeScale 요청자 디버깅

`_requests` 딕셔너리에 owner 이름을 함께 보관(예: `Request(scale, name)`)하도록 확장하면 디버그 모니터에서 누가 슬로우를 걸고 있는지 표시 가능. 누수(미해제 요청) 추적에 유용.

### TotalPlaySeconds 라이프 보강

세이브 직전마다 `ExportSaveData`에서 `TotalPlaySeconds`를 같이 직렬화하면 영속화. 현재 `GameTimeManager`는 `ISaveable`을 구현하지 않으므로, 필요하면 추가하여 SaveManager에 등록.
