# 런타임 로그 및 성능 검증 시스템 가이드

> 작성일: 2026-06-25  
> 대상 버전: Unity 6 (6000.0.60f1)  
> 적용 범위: Editor, Development Build, Release Build

---

## 개요

런타임 로그 및 성능 검증 시스템은 개발 중 필요한 상세 진단 정보는 유지하면서 출시 빌드의 로그 비용을 제한하고, 동일한 전투 시나리오의 성능 기준선을 반복 측정하기 위한 시스템이다.

핵심 기능은 다음과 같다.

- Release Build에서 일반 `Debug.Log` 출력을 전역 차단한다.
- Warning, Error, Exception은 출시 빌드에서도 유지한다.
- 개발용 상세 로그를 기능별 카테고리로 제어한다.
- 반복되는 로그를 시간 간격 기준으로 제한한다.
- 최근 600프레임의 프레임 시간과 GC 할당량을 고정 버퍼에 기록한다.
- 성능 HUD를 표시하고 측정 결과를 JSON 파일로 저장한다.

---

## 아키텍처

```text
게임 코드
├── 전투 / 입력 / AI / 카메라
│       └── RuntimeLog.Trace / TraceThrottled
│
├── Editor 또는 Development Build
│       ├── 카테고리 필터 확인
│       └── Unity Console 출력
│
└── Release Build
        ├── Trace 호출 및 인자 평가 컴파일 제거
        └── 기존 Debug.Log 전역 출력 차단

RuntimePerformanceMonitor
├── Time.unscaledDeltaTime
├── ProfilerRecorder
│   ├── GC Allocated In Frame
│   ├── Draw Calls Count
│   └── SetPass Calls Count
├── 최근 600프레임 고정 배열
├── F10 성능 HUD
└── F11 JSON 기준선 저장
```

### 관련 파일

```text
Assets/02.Scripts/
├── Util/
│   └── Util.cs
│       ├── RuntimeLogCategory
│       └── RuntimeLog
├── Tool/
│   └── PlayerControlFeelDebugHUD.cs
│       └── RuntimePerformanceMonitor
├── GameActor/
│   ├── Object/Monster/MonsterActor.cs
│   └── State/Player/ComboRouteRunner.cs
└── AI/BehaviorTree/Nodes/Action/LogNode.cs
```

---

## 런타임 로그 통제

### 빌드별 동작

| 환경 | 일반 `Debug.Log` | `RuntimeLog.Trace` | Warning/Error |
|------|------------------|--------------------|---------------|
| Unity Editor | 출력 | 출력 가능 | 출력 |
| Development Build | 출력 | 출력 가능 | 출력 |
| Release Build | 전역 차단 | 호출과 문자열 인자 평가 제거 | 출력 |

초기화 시 다음 정책을 적용한다.

```csharp
Debug.unityLogger.filterLogType =
    Debug.isDebugBuild ? LogType.Log : LogType.Warning;
```

따라서 기존 코드에 남아 있는 `Debug.Log`도 Release Build에서는 표시되지 않는다. 다만 `Debug.Log` 호출문의 문자열 생성 자체는 실행될 수 있으므로, 프레임 경로나 반복 호출 지점은 `RuntimeLog.Trace`로 전환해야 한다.

### 로그 카테고리

`RuntimeLogCategory`는 `[Flags]` enum이다.

| 카테고리 | 용도 |
|----------|------|
| `Boot` | 게임 및 매니저 초기화 |
| `Combat` | 공격, 피해, 리액션, 몬스터 전투 상태 |
| `Input` | 입력 버퍼, 콤보 입력, 입력 라우팅 |
| `AI` | Behavior Tree, 탐지, 의사결정 |
| `Camera` | 카메라 모드, 락온, 연출 |
| `UI` | UI 표시 및 입력 레이어 |
| `Asset` | Addressables와 데이터 로딩 |
| `Performance` | 성능 측정 및 기준선 저장 |
| `Player` | 플레이어 상태 및 플레이어 전용 진단 |
| `Monster` | 몬스터 Behavior Tree 및 몬스터 전용 진단 |
| `Default` | 별도 기능 분류가 없는 일반 진단 |
| `System` | 매니저와 시스템 생명주기 진단 |
| `All` | 모든 카테고리 |

기본값은 `All`이며 설정값은 다음 PlayerPrefs 키에 저장된다.

```text
UPlayGround.RuntimeLog.CategoryMask
```

### 일반 상세 로그

```csharp
using UPlayGround.Diagnostics;

RuntimeLog.Trace(
    RuntimeLogCategory.Combat,
    $"[Combat] 공격 시작: {attackData.animKey}",
    this);
```

`Trace`에는 `UNITY_EDITOR`, `DEVELOPMENT_BUILD` 조건이 적용되어 있다. Release Build에서는 메서드 호출뿐 아니라 보간 문자열 생성도 제거된다.

### 다중 카테고리

```csharp
RuntimeLog.Trace(
    RuntimeLogCategory.Combat | RuntimeLogCategory.Input,
    $"[ComboRoute] 입력 라우트 매칭: {route.routeName}");
```

현재 활성 카테고리와 하나라도 겹치면 출력된다.

### 반복 로그 제한

```csharp
RuntimeLog.TraceThrottled(
    RuntimeLogCategory.Combat,
    GetInstanceID(),
    1f,
    $"[MonsterActor] {name}는 현재 데미지를 받을 수 없습니다.",
    this);
```

같은 `key`의 로그를 `intervalSeconds` 동안 한 번만 출력한다.

키 사용 기준:

- 객체별 제한: `GetInstanceID()`
- 호출 지점별 제한: 충돌하지 않는 상수
- 객체와 사건을 구분해야 할 때: 별도 안정적인 키 조합

### 카테고리 변경

```csharp
RuntimeLog.SetEnabledCategories(
    RuntimeLogCategory.Combat |
    RuntimeLogCategory.Input);
```

기본적으로 PlayerPrefs에 저장된다. 현재 세션에만 적용하려면 두 번째 인자를 `false`로 전달한다.

```csharp
RuntimeLog.SetEnabledCategories(
    RuntimeLogCategory.Performance,
    persist: false);
```

### 에디터 필터 창

`UPlayGround > 툴 런처`를 연 뒤 `디버그 > 런타임 로그 필터`에서 카테고리를 체크하여 변경할 수 있다.

- 체크 변경은 Edit Mode와 Play Mode의 현재 필터에 즉시 반영된다.
- 기본적으로 `PlayerPrefs`에 저장되어 다음 Play Mode에도 유지된다.
- `모두 켜기`, `모두 끄기`, `저장값 다시 읽기`를 지원한다.
- 여러 카테고리가 지정된 로그는 활성 카테고리와 하나라도 겹치면 출력된다.

### 적용된 고빈도 경로

| 코드 | 적용 내용 |
|------|-----------|
| `ComboRouteRunner` | 상세 콤보 로그 기본 비활성화, Combat/Input 카테고리 적용 |
| `MonsterActor` | 무적 대상 반복 로그 스로틀, 크리티컬·회복·사망·패리 로그 분류 |
| `BehaviorTreeRunner` / `LogNode` | BT 시작·정지·일시정지·루트 결과와 명시적 BT 로그에 Combat/Monster 카테고리 적용 |
| `ActorMovementController` | 성공한 플레이어 상태 전이에 Combat/Player 카테고리 적용 |
| `UI_Base` | UI 열기·숨김·제거 생명주기에 UI 카테고리 적용 |
| `InputManager` | 최종 콜백 디스패치에 Input 카테고리 적용. 연속 값의 반복 `performed`는 제외 |

`ComboRouteRunner.DebugLog` 기본값은 `false`다. 콤보 라우트 조사 시에만 임시로 활성화해야 한다.

---

## 성능 검증 시스템

### 동작 환경

`RuntimePerformanceMonitor`는 다음 전처리 조건 안에서만 컴파일된다.

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
```

씬에 직접 배치할 필요가 없다. 씬 로드 후 인스턴스가 없으면 자동 생성되며 `DontDestroyOnLoad`로 유지된다.

### 단축키

| 키 | 기능 |
|----|------|
| `F10` | 성능 HUD 표시/숨김 |
| `F11` | 최근 측정 윈도우를 JSON으로 저장 |

### 측정 항목

| 항목 | 산출 방식 |
|------|-----------|
| 평균 프레임 시간 | 최근 600프레임 `unscaledDeltaTime` 평균 |
| 최대 프레임 시간 | 최근 600프레임 최댓값 |
| 평균 FPS | `1000 / 평균 프레임 시간(ms)` |
| Slow Frame 비율 | 목표 FPS의 프레임 예산을 넘은 프레임 비율 |
| 평균·최대 GC Alloc | `GC Allocated In Frame` ProfilerRecorder |
| Draw Calls | `Draw Calls Count` ProfilerRecorder 최신값 |
| SetPass Calls | `SetPass Calls Count` ProfilerRecorder 최신값 |
| Mono Used | `Profiler.GetMonoUsedSizeLong()` |
| Total Allocated | `Profiler.GetTotalAllocatedMemoryLong()` |

프레임과 GC 표본은 길이 600의 고정 배열에 저장한다. 측정 중 리스트 확장이나 매 프레임 파일 쓰기는 수행하지 않는다.

### 기본 판정 기준

| 설정 | 기본값 | 의미 |
|------|--------|------|
| `_targetFps` | 60 | 목표 프레임률 |
| `_slowFrameWarningRatio` | 0.1 | 느린 프레임이 10%를 넘으면 경고 |
| `_gcWarningBytesPerFrame` | 1024 | 평균 GC가 프레임당 1 KB를 넘으면 경고 |
| 표본 크기 | 600프레임 | 60 FPS 기준 약 10초 |

이 값은 초기 경고 기준이지 최종 성능 예산이 아니다. 실제 타깃 플랫폼에서 기준선을 수집한 뒤 조정해야 한다.

### JSON 저장

F11 입력 시 다음 경로에 저장한다.

```text
Application.persistentDataPath/PerformanceSnapshots/
yyyyMMdd_HHmmss_<SceneName>.json
```

주요 필드:

```json
{
  "capturedAtUtc": "2026-06-25T00:00:00.0000000Z",
  "scene": "GameScene",
  "unityVersion": "6000.0.60f1",
  "platform": "WindowsPlayer",
  "sampleCount": 600,
  "targetFps": 60.0,
  "averageFrameMilliseconds": 15.4,
  "maximumFrameMilliseconds": 25.1,
  "slowFrameRatio": 0.08,
  "averageGcAllocatedBytes": 0,
  "maximumGcAllocatedBytes": 512,
  "drawCalls": 850,
  "setPassCalls": 120
}
```

값은 예시이며 실제 측정 결과가 아니다.

---

## 권장 측정 절차

성능 변경 전후에 동일한 조건을 재현해야 비교가 의미 있다.

1. Development Build와 Autoconnect Profiler를 사용한다.
2. 같은 씬, 같은 그래픽 옵션, 같은 해상도로 실행한다.
3. 워밍업을 위해 Addressables 로딩과 첫 전투를 한 번 완료한다.
4. 동일한 적 수와 동일한 전투 행동을 재현한다.
5. 최소 600프레임을 유지한다.
6. F11로 기준선을 저장한다.
7. 변경 전후 JSON의 평균, 최대, Slow Frame 비율, GC를 비교한다.
8. 이상 구간은 Unity Profiler Timeline과 Profile Analyzer로 원인을 추적한다.

### 권장 시나리오

| 시나리오 | 검증 목적 |
|----------|-----------|
| 플레이어 단독 이동 | 이동·카메라 기본 비용 |
| 일반 몬스터 10마리 | AI와 탐지 기본 확장성 |
| 일반 몬스터 20~30마리 | AI/KCC 병목과 프레임 스파이크 |
| 다단 히트 전투 | 히트박스, 데미지, VFX, 로그 비용 |
| 파티 캐릭터 교체 | 모델·애니메이션·IK·Addressables 순간 비용 |
| Break/특수공격 연출 | 카메라, HitStop, VFX 동시 부하 |

---

## 측정 결과 해석

### 평균값만으로 판단하지 않는다

평균 60 FPS여도 최대 프레임 시간이 크거나 Slow Frame 비율이 높으면 조작감이 불안정하다.

우선순위:

1. 프레임 스파이크
2. Slow Frame 비율
3. 지속적인 GC Alloc
4. 평균 프레임 시간
5. Draw Call과 SetPass

### GC Alloc

전투 중 정상 프레임은 가능하면 `0 B/frame`을 목표로 한다. 다음은 흔한 원인이다.

- 프레임 경로의 문자열 보간과 `Debug.Log`
- LINQ `ToList()`, `ToArray()`
- 반복적인 컬렉션 생성
- 클로저와 람다 캡처
- 매 프레임 `GetComponents` 결과 배열 생성

JSON 직렬화와 파일 저장은 F11 입력 시에만 수행되므로 기준선 저장 순간의 스파이크는 측정 대상에서 분리해 해석한다.

### Draw Call과 SetPass

두 값은 장면과 렌더링 설정에 크게 의존한다. 절대값 하나보다 동일 장면의 변경 전후 차이를 비교한다.

---

## 주의 사항

### Editor 측정은 최종 기준선이 아니다

Editor에는 Inspector, Scene View, 에디터 확장, 도메인 상태 등의 비용이 섞인다. 빠른 회귀 확인에는 사용할 수 있지만 최종 판정은 Development Build에서 수행한다.

### Warning/Error를 반복 출력하지 않는다

Release Build에서도 Warning/Error는 유지된다. 프레임마다 발생할 수 있는 정상 분기는 Warning/Error로 기록하면 안 된다.

### 로그 게이트 전에 문자열을 만들지 않는다

다음 코드는 카테고리가 꺼져도 문자열이 먼저 생성된다.

```csharp
string message = $"Target={target.name}";
RuntimeLog.Trace(RuntimeLogCategory.Combat, message);
```

호출문 안에서 직접 보간해야 Release Build에서 인자 평가까지 제거된다.

```csharp
RuntimeLog.Trace(
    RuntimeLogCategory.Combat,
    $"Target={target.name}");
```

### ProfilerRecorder 이름

플랫폼이나 Unity 버전에 따라 일부 Recorder 통계가 유효하지 않을 수 있다. Recorder 시작 실패는 Performance 카테고리 로그로 기록되며 해당 HUD 값은 0으로 표시된다.

---

## 확장 지침

### 로그 전환 우선순위

기존 `Debug.Log` 전체를 기계적으로 치환하지 않는다. 다음 순서로 전환한다.

1. `Update`, `Tick`, `UpdateState` 내부 로그
2. 전투 적중, AI 탐지처럼 자주 호출되는 로그
3. 문자열이 길거나 객체 상태를 많이 조합하는 로그
4. 초기화 완료처럼 한 번만 발생하는 정보 로그

오류 진단에 필요한 `Debug.LogWarning`, `Debug.LogError`는 의미를 검토한 후 유지하거나 별도 정책을 적용한다.

### 향후 성능 기준선 자동 비교

JSON 스냅샷을 기반으로 다음 기능을 추가할 수 있다.

- 기준 JSON과 신규 JSON의 자동 차이 계산
- 시나리오 이름과 적 수 메타데이터
- 플랫폼별 성능 예산 ScriptableObject
- 허용 범위 초과 시 Editor 경고
- CI Development Build 성능 스모크 테스트

---

## 부록: 전투 Core 분리와 성능 검증의 관계

현재 성능 모니터는 실제 플레이 전체의 비용을 측정한다. 반면 전투 Core 분리는 데미지·리액션·콤보 판정을 Unity 객체에서 분리하여 순수 입력과 출력으로 만드는 별도 구조 개선이다.

```text
현재
MonsterActor / PlayerCombat
    └── Unity 상태를 직접 읽으며 전투 계산

Core 분리 후
Unity 계층
    ├── MonsterActor 상태를 값 구조체로 변환
    └── CombatRequest 생성
            ↓
순수 Combat Core
    ├── DamageCalculator
    ├── ReactionCalculator
    └── ComboRouteMatcher
            ↓
CombatResult
            ↓
Unity 계층이 HP, 상태, 애니메이션, VFX 적용
```

분리의 효과:

- Unity Editor 없이 전투 수치 테스트 가능
- 경계값과 정책 조합을 빠르게 반복 검증
- 실제 플레이 성능 측정 전에 계산 로직 자체의 비용을 별도 확인
- MonoBehaviour 생명주기와 전투 규칙의 결합 감소

이 가이드의 로그·성능 시스템은 전투 Core 분리의 전제 조건이 아니다. 현재 구조에서도 독립적으로 사용할 수 있으며, Core 분리 후에는 실제 플레이 성능과 순수 계산 성능을 각각 검증할 수 있다.
