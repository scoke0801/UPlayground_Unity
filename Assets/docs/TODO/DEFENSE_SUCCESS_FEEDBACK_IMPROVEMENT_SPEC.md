# 방어 성공 피드백 개선 스펙

> 작성일: 2026-06-05  
> 분류: 전투 피드백 개선 / TODO  
> 기준 레퍼런스: 명조(Wuthering Waves) 패리, 퍼펙트 회피, 카운터 피드백  
> 관련 문서: `Assets/docs/Complete/TIME_HITSTOP_GUIDE.md`, `Assets/docs/Complete/COMBAT_CAMERA_SYSTEM_IMPROVEMENT_PLAN.md`, `Assets/docs/Complete/COMBAT_SYSTEM_NEXT_IMPROVEMENT_PROPOSAL.md`

---

## 1. 목적

현재 패리, 퍼펙트 가드, 퍼펙트 회피 성공 피드백은 `GameHitStopHandler.HitStopIntensity.PlayerGuard`에 묶여 있다.

이 프리셋은 `Time.timeScale`을 직접 낮추지 않고, 플레이어를 제외한 액터의 `LocalTimeScale`을 0.05로 낮춘 뒤 `SlowMoveVolume` 포스트프로세스를 3초 동안 페이드아웃한다.

문제는 다음과 같다.

- 플레이어는 정상 속도로 움직이므로 성공 직후 화면 초점에서 멀어질 수 있다.
- 성공 피드백이 시작되자마자 반격 공격이 성공하면 기존 HitStop/Volume이 정리되어 효과가 거의 보이지 않을 수 있다.
- 패리, 퍼펙트 가드, 퍼펙트 회피가 같은 프리셋을 사용해 성공 종류별 차이가 약하다.
- 포스트프로세스는 켜지지만 성공 순간의 위치 고정, 입력 억제, 카메라 초점 고정이 없어 "성공했다"는 판독성이 낮다.

이 문서는 방어 성공 피드백을 "성공 순간 고정 + 짧은 bullet time + 명확한 카운터 유도" 구조로 개선하기 위한 스펙이다.

---

## 2. 레퍼런스 요약

명조의 내부 구현은 공개되어 있지 않으므로, 공개 가이드와 플레이 관찰에서 드러나는 전투 감각만 설계 목표로 환산한다.

참고 자료:

- WutheringWaves.gg Combat Guide: https://wutheringwaves.gg/combat-basics/
- Game8 How to Perfect Dodge: https://game8.co/games/Wuthering-Waves/archives/456639
- Game8 How to Parry and Counterattack: https://game8.co/games/Wuthering-Waves/archives/455880
- Wuthering.gg Counterattacks: https://wuthering.gg/guide/fighting/counterattacks

### 핵심 관찰

| 항목 | 레퍼런스 동작 | UPlayground 적용 방향 |
|------|---------------|-----------------------|
| 퍼펙트 회피 | 공격이 맞기 직전에 회피하면 bullet time, 짧은 무적, 카운터 창이 열린다 | 성공 직후 짧은 전체 정지/슬로우와 `DodgeCounter` 창을 명확히 부여 |
| 패리 | 적 공격의 Weakness Halo 타이밍에 공격을 맞추면 공격을 무효화하고 카운터가 성립한다 | 패리 성공 시 공격자 모션/히트박스를 확실히 끊고 카운터 가능 상태를 시각화 |
| 가드/카운터 가독성 | 성공 전후의 링, 경고, 타격 플래시가 판정 근거를 알려준다 | 성공 순간의 FX, 카메라, 포스트프로세스를 같은 위치에 묶어 판독성 확보 |
| 보상 | Vibration Strength 감소, 에너지 회복, 추가 피해/특수 카운터로 보상한다 | VitalOrb, Break/Poise 보상, 카운터 공격을 성공 피드백과 직접 연결 |

---

## 3. 현재 코드 기준 문제 지점

### 공통 성공 피드백 호출

| 성공 종류 | 현재 호출 위치 | 현재 피드백 |
|-----------|----------------|-------------|
| 패리 | `PlayerActor.OnParrySuccess()` | `GameHitStop.Execute(PlayerGuard)`, `PlayPerfectGuard`, `ParryFX`, VitalOrb, `monster.OnParried()` |
| 퍼펙트 회피 | `PlayerActor.TryPerfectDodge()` | `ClosePerfectDodgeWindow`, VitalOrb, `GameHitStop.Execute(PlayerGuard)`, `PlayPerfectDodge` |
| 퍼펙트 가드 | `PlayerGuardState.OnAttackBlocked()` | VitalOrb, `GameHitStop.Execute(PlayerGuard)`, `PlayPerfectGuard`, 선택적 `monster.OnParried()`, counter window |

### `PlayerGuard` 프리셋 문제

현재 구현:

```csharp
case HitStopIntensity.PlayerGuard:
    _targetWeight  = 0f;
    _currentWeight = 1f;
    _transitionTime = 3f;
    GameObjectManager.Instance?.SetGlobalTimeScaleExceptPlayer(0.05f, 3f);
    break;
```

문제:

- `currentWeight=1`, `targetWeight=0`이므로 포스트프로세스는 즉시 켜진 뒤 바로 페이드아웃한다.
- 플레이어는 제외되므로 성공 후 이동/공격으로 화면 구도가 즉시 바뀐다.
- `SetGlobalTimeScaleExceptPlayer(0.05f, 3f)`는 모든 비플레이어 액터를 3초 동안 느리게 만든다. 성공 피드백 치고 지속 시간이 길어 후속 전투 리듬에 간섭한다.
- `PlayerAttackState`나 `PlayerGuardState`의 반격 진입 경로에서 `GameHitStop.Stop()`이 호출되면 최소 표시 시간이 보장되지 않는다.

---

## 4. 개선 목표

### 목표 감각

방어 성공 순간에는 플레이어가 다음을 즉시 인식해야 한다.

1. 방어 성공이 발생했다.
2. 어떤 종류의 성공인지 구분된다.
3. 공격자가 끊겼거나 위험이 무효화되었다.
4. 지금 카운터 입력/공격을 하면 된다는 것을 알 수 있다.

### 설계 원칙

| 원칙 | 설명 |
|------|------|
| 성공 순간 고정 | 성공 직후 0.08~0.12초는 플레이어와 공격자를 함께 고정해 성공 지점을 보존한다 |
| 짧은 tail | 고정 이후 0.20~0.35초만 bullet time/포스트프로세스 tail을 남긴다 |
| 최소 표시 시간 보장 | 반격이 즉시 시작되어도 성공 플래시와 핵심 FX는 최소 시간 동안 유지한다 |
| 성공 종류 분리 | 패리, 퍼펙트 가드, 퍼펙트 회피는 다른 프로필을 사용한다 |
| 카운터 유도 | 성공 피드백은 단순 정지가 아니라 카운터 창과 연결된다 |
| 데이터 튜닝 | 시간, timeScale, Volume weight, 카메라 효과, 입력 억제 시간을 코드 상수보다 데이터로 조정 가능하게 한다 |

---

## 5. 신규 피드백 모델

### DefenseSuccessType

```csharp
public enum DefenseSuccessType
{
    Parry,
    PerfectGuard,
    PerfectDodge
}
```

### DefenseSuccessFeedbackProfile

ScriptableObject 또는 직렬화 가능한 데이터 구조로 관리한다.

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `successType` | - | 패리/퍼펙트 가드/퍼펙트 회피 |
| `freezeDuration` | 0.08~0.12 | 성공 직후 완전 정지 또는 극저속 구간 |
| `freezeTimeScale` | 0.001~0.03 | 전역 timeScale 또는 액터 로컬 스케일 |
| `tailDuration` | 0.20~0.35 | bullet time 잔여 구간 |
| `tailTimeScale` | 0.12~0.25 | tail 구간의 전역/로컬 슬로우 |
| `playerLockDuration` | 0.08~0.12 | 플레이어 이동/루트모션 입력 억제 시간 |
| `minPostProcessVisibleDuration` | 0.12 | 반격으로 중단되어도 보장할 최소 표시 시간 |
| `postProcessPeakWeight` | 1.0 | 성공 순간 Volume weight |
| `postProcessHoldDuration` | 0.06~0.10 | peak 유지 시간 |
| `postProcessFadeOutDuration` | 0.18~0.30 | fade out 시간 |
| `attackerLockDuration` | 0.12~0.25 | 공격자 모션/AI 재개 지연 |
| `counterWindowDuration` | 기존 값 사용 | 카운터 입력 가능 시간 |
| `cameraIntentType` | - | `PerfectGuard`, `PerfectDodge`, `DodgeCounter` 등 |
| `shakeKey` | - | 상황별 흔들림 |
| `fxKey` | - | 성공 FX |
| `spawnVitalOrbTrigger` | - | VitalOrb 보상 |

### 권장 기본값

| 성공 종류 | Freeze | Tail | 플레이어 고정 | 공격자 고정 | PP 표시 | 카운터 |
|-----------|--------|------|---------------|-------------|---------|--------|
| 패리 | 0.10s / 0.01 | 0.22s / 0.18 | 0.10s | 0.25s | 0.08s hold + 0.22s fade | 패리 카운터 |
| 퍼펙트 가드 | 0.08s / 0.02 | 0.20s / 0.20 | 0.08s | 0.18s | 0.06s hold + 0.20s fade | Parryable만 카운터 |
| 퍼펙트 회피 | 0.06s / 0.03 | 0.30s / 0.15 | 0.06s | 0.12s | 0.08s hold + 0.28s fade | 회피 카운터 |

---

## 6. 피드백 실행 시퀀스

### 공통 시퀀스

```
방어 성공 판정
    │
    ├── 공격 판정/히트박스 중단
    ├── 성공 지점 계산(hitPoint, attacker, player)
    ├── DefenseSuccessFeedbackProfile 선택
    │
    ▼
DefenseSuccessFeedbackHandler.Play(profile, context)
    │
    ├── 1. 성공 순간 freeze 시작
    ├── 2. 플레이어 입력/루트모션 짧게 억제
    ├── 3. 공격자 LocalTimeScale/AI 재개 지연
    ├── 4. PostProcess peak 보장
    ├── 5. 카메라 intent 실행
    ├── 6. 성공 FX/SFX/VitalOrb 스폰
    ├── 7. counter window 열기
    │
    ▼
freeze 종료
    │
    ├── tail slow-motion 유지
    ├── counter 입력 허용
    └── post-process fade out
```

### 패리

필수 동작:

- 현재 플레이어 공격 충돌을 즉시 닫는다.
- 공격자 히트박스와 공격 모션을 확실히 취소하거나 경직 상태로 전환한다.
- 플레이어를 Idle로 즉시 보내더라도 0.10초 동안 이동/루트모션을 억제한다.
- 패리 카운터 창을 먼저 열고, 성공 피드백이 최소 표시 시간 전에 강제 종료되지 않게 한다.

### 퍼펙트 가드

필수 동작:

- 일반 가드 FX와 퍼펙트 가드 FX를 분리한다.
- `AttackDefenseType.Parryable`이면 공격자 경직과 카운터 창을 연다.
- `GuardableOnly`이면 카운터는 열지 않지만 성공 플래시와 보상은 유지한다.

### 퍼펙트 회피

필수 동작:

- `ClosePerfectDodgeWindow()`로 중복 발동을 막는다.
- 회피 방향 잔상/플래시를 플레이어 위치에 고정한다.
- `DodgeCounterAvailable` 상태를 별도 플래그로 열고, 기본 공격 입력 시 회피 카운터로 라우팅한다.
- 플레이어가 멀리 도망가도 카운터 입력 시 최근 공격자 또는 위협 대상을 향해 짧은 soft target assist를 적용한다.

---

## 7. 코드 변경 제안

### 7.1 `GameHitStopHandler` 역할 분리

현재 `PlayerGuard` 프리셋은 이름과 동작이 맞지 않는다. 다음 중 하나로 정리한다.

권장안:

- 기존 `HitStopIntensity.PlayerGuard` 사용 중단.
- 신규 `DefenseSuccessFeedbackHandler`를 `GameCombatManager` 산하 핸들러로 추가.
- `GameHitStopHandler`는 전역 `Time.timeScale` 요청과 일반 타격 HitStop만 담당.
- 포스트프로세스 Volume 제어는 별도 `CombatPostProcessFeedback` 또는 신규 핸들러가 담당.

대안:

- `PlayerGuard`를 `DefenseSuccessSlowMo`로 이름 변경.
- 내부 동작을 profile 기반으로 확장.
- 최소 표시 시간과 freeze/tail 구간을 추가.

### 7.2 신규 핸들러

```csharp
public sealed class DefenseSuccessFeedbackHandler : GameHandlerBase
{
    public void Play(DefenseSuccessFeedbackProfile profile, in DefenseSuccessFeedbackContext context);
    public void Stop(DefenseSuccessStopMode mode);
}
```

`DefenseSuccessStopMode`:

| 모드 | 설명 |
|------|------|
| `RespectMinimumDuration` | 최소 표시 시간 전이면 중단하지 않는다 |
| `FadeOut` | 남은 tail만 빠르게 fade out |
| `Immediate` | 씬 전환/사망 등 강제 종료 |

### 7.3 호출부 변경

| 파일 | 변경 방향 |
|------|----------|
| `PlayerActor.OnParrySuccess()` | `GameHitStop.Execute(PlayerGuard)` 대신 `DefenseSuccessFeedback.Play(Parry, context)` 호출 |
| `PlayerActor.TryPerfectDodge()` | `DefenseSuccessFeedback.Play(PerfectDodge, context)` 호출, dodge counter window 추가 |
| `PlayerGuardState.OnAttackBlocked()` | 일반 가드/퍼펙트 가드 피드백 분리 |
| `PlayerAttackState` | 카운터 진입 시 `GameHitStop.Stop()` 직접 호출 제거 또는 최소 표시 시간 존중 |
| `CombatCameraDirector` | `DefenseSuccessType`별 profile 또는 intent 연결 |
| `GameObjectManager` | 전체 비플레이어 3초 슬로우 대신 대상 액터 중심 로컬 슬로우 API 추가 검토 |

---

## 8. 카메라 / 포스트프로세스 정책

### 카메라

| 성공 종류 | 카메라 정책 |
|-----------|-------------|
| 패리 | 공격자와 플레이어 사이 지점으로 짧은 punch, 강한 shake, FOV pulse |
| 퍼펙트 가드 | GuardPosition 소켓 기준 짧은 punch, 낮은 shake, FOV pulse |
| 퍼펙트 회피 | 플레이어 회피 방향 반대쪽으로 약한 punch, 회피 후 공격자 soft target assist |

주의:

- 카메라를 긴 시간 강제 고정하지 않는다.
- 최근 수동 카메라 입력이 있으면 자동 보정 강도를 낮춘다.
- 락온 중이면 현재 락온 대상을 우선한다.

### 포스트프로세스

현재 `SmoothDamp` 3초 fade는 성공 피드백으로 길다. 다음 형태를 권장한다.

```
peak:  weight = 1.0 즉시
hold:  0.06~0.10초 유지
fade:  0.18~0.30초 동안 0으로 감소
```

반격이 즉시 시작되어도 `minPostProcessVisibleDuration` 이전에는 weight를 0으로 강제하지 않는다.

---

## 9. 입력 / 이동 억제 정책

성공 순간 초점이 흩어지는 문제를 막기 위해 매우 짧은 입력 억제가 필요하다.

| 항목 | 정책 |
|------|------|
| 이동 입력 | freeze 구간 동안 적용 보류 |
| 공격 입력 | counter 입력은 버퍼에 유지 |
| 카메라 입력 | 억제하지 않음. 단, 자동 보정은 수동 입력을 존중 |
| 루트모션 | freeze 구간 동안 delta position 적용 보류 또는 0에 가깝게 스케일 |
| KCC 이동 | 플레이어도 0.06~0.10초는 LocalTimeScale 또는 별도 lock으로 묶는다 |

중요: 입력 자체를 삭제하지 않는다. `InputBuffer`에 남겨 freeze 종료 직후 카운터로 소비할 수 있어야 한다.

---

## 10. 구현 단계

### P0 - 현재 동작 안정화

- `PlayerGuard` 프리셋 이름/용도를 문서와 코드에서 명확히 한다.
- `PlayerAttackState`/`PlayerGuardState`의 `GameHitStop.Stop()` 호출이 방어 성공 피드백을 즉시 지우는지 확인한다.
- 방어 성공 직후 최소 표시 시간을 임시 상수로라도 보장한다.

### P1 - 프로필 기반 방어 성공 피드백 도입

- `DefenseSuccessType` 추가.
- `DefenseSuccessFeedbackProfile` 추가.
- `DefenseSuccessFeedbackContext` 추가.
- `GameCombatManager`에 `DefenseSuccessFeedbackHandler` 추가.
- 패리/퍼펙트 가드/퍼펙트 회피 호출부를 신규 핸들러로 연결.

### P2 - 카운터 창과 카메라 연동 강화

- 퍼펙트 회피 전용 `DodgeCounterAvailable` 플래그 추가.
- 회피 카운터 진입 시 `CombatCameraDirector.PlayDodgeCounter()` 연결.
- 패리/퍼펙트 가드/퍼펙트 회피 카메라 profile을 `CombatCameraProfileSO`로 분리.

### P3 - 적 텔레그래프 / Weakness Halo 연동

- `AttackDefenseType.Parryable` 공격에 Weakness Halo 또는 Danger Ring 정책을 연결한다.
- `UI_DangerRing`과 방어 성공 피드백 색/타이밍을 맞춘다.
- 강한 공격 전 warning prompt를 데이터화한다.

---

## 11. 검수 기준

### 기능 검수

- 패리 성공 직후 최소 0.10초 동안 성공 FX와 포스트프로세스가 보인다.
- 퍼펙트 회피 성공 직후 플레이어가 멀리 이동해도 성공 위치/공격자가 화면에서 읽힌다.
- 반격 공격을 즉시 입력해도 성공 피드백이 완전히 사라지지 않는다.
- 패리, 퍼펙트 가드, 퍼펙트 회피의 카메라/FX/사운드가 구분된다.
- 일반 가드와 퍼펙트 가드가 명확히 다르게 보인다.
- `GuardableOnly` 퍼펙트 가드는 성공 피드백은 나오지만 패리 카운터 창은 열리지 않는다.

### 리듬 검수

- 방어 성공 피드백이 연속 공격 패턴을 읽는 데 방해되지 않는다.
- 3초 동안 모든 적이 느려지는 체감이 사라지고, 성공 순간 중심의 짧은 연출로 바뀐다.
- 여러 적이 동시에 공격해도 가장 최근 성공 피드백이 이전 피드백을 무조건 파괴하지 않는다.

### 옵션 / 접근성 검수

- 카메라 shake off 상태에서는 shake가 적용되지 않는다.
- 전투 카메라 sequence intensity가 0이면 snapshot/FOV 계열 강한 연출이 꺼진다.
- 추후 "퍼펙트 회피 슬로모션 약화/끄기" 옵션을 추가할 수 있는 구조여야 한다.

---

## 12. 리스크

| 리스크 | 대응 |
|--------|------|
| 전역 `Time.timeScale` 사용 시 Animancer/KCC/MotionEvent 타이밍이 꼬일 수 있음 | freeze는 `WaitForSecondsRealtime` 기반으로 관리하고, 가능하면 짧게 유지 |
| 플레이어 입력 억제가 조작감 저하로 느껴질 수 있음 | 0.06~0.10초 이하로 제한하고 공격 입력은 버퍼 유지 |
| 연속 성공 시 Volume weight가 튀거나 늦게 꺼질 수 있음 | profile id 기반 요청 큐와 최소 표시 시간 정책 사용 |
| 모든 비플레이어 3초 슬로우가 전투 난이도를 낮출 수 있음 | 대상 공격자 중심 로컬 슬로우로 전환 |
| 카메라 자동 보정이 불쾌할 수 있음 | 최근 수동 입력 기준 감쇠와 설정 옵션 연동 |

---

## 13. 최종 권장 방향

현재 문제는 효과 시간이 짧아서가 아니라, 성공 순간의 초점이 고정되지 않고 후속 공격에 의해 피드백이 쉽게 지워지는 것이다.

따라서 다음 방향을 우선한다.

1. `PlayerGuard` 3초 actor-only slow를 방어 성공 공통 피드백에서 제거한다.
2. 패리/퍼펙트 가드/퍼펙트 회피를 profile 기반으로 분리한다.
3. 성공 직후 0.06~0.12초의 플레이어 포함 freeze를 넣는다.
4. 포스트프로세스는 최소 표시 시간 + 짧은 fade out으로 바꾼다.
5. 카운터 창과 카메라/FX/SFX를 같은 피드백 context에서 실행한다.

이 방식이 명조식 방어 성공 피드백의 핵심인 "짧고 선명한 성공 판독성"과 "바로 이어지는 카운터 보상"에 가장 가깝다.
