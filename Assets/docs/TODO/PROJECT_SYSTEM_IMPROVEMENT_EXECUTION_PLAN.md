# 프로젝트 시스템 개선 실행 계획

> 작성일: 2026-06-18  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 상태: 구현 진행 중  
> 대상 범위: 매니저 초기화, 씬 전환, Addressables, 이벤트, 세이브, 런타임 코드 경계, 자동화 테스트

---

## 구현 진행 현황

| 작업 | 상태 | 반영 내용 |
|------|------|-----------|
| Phase 0 부팅 상태·진단 기반 | 구현 완료·PlayMode 기록 대기 | `GameBootState`, 실패 원인, 매니저별 비동기 초기화 소요 시간과 스모크 체크리스트 추가 |
| Phase 1 비동기 초기화 계약 | 완료 | `IAsyncInitializableManager`와 `CancellationToken` 기반 순차 초기화 경로 추가 |
| Phase 1 Addressables 초기화 전환 | 완료 | Settings, Sound, Asset, UI, Party, Item, ActorSpawn, Dialogue, Recipe, Quest 매니저의 준비 완료를 `GameManager`가 대기 |
| Phase 1 핸들 수명주기 보강 | 완료 | 신규 전환 대상의 장기 보유 핸들을 저장하고 `Dispose()`에서 Release |
| Phase 2 씬 전환 상태 머신 | 구현 완료·PlayMode 검증 대기 | `SceneLoadState`, `SceneLoadRequest`, 중복 요청 무시, 명시적 활성화·취소, 실제 대상 `SceneContext` 준비 후 완료 처리 |
| Phase 3 Addressables 공통 경로 | 기반 완료·인스턴스 정책 대기 | 전역/씬 수명 캐시, 소유자 추적, 씬 전환 전 범위 해제, 개발용 핸들 통계 추가. 런타임 직접 에셋 로드도 `AssetManager`로 제한 |
| Phase 4 이벤트 타입·범위 | 핵심 완료 | Payload 타입 포함 키, Scene/Global 구독 범위, `IDisposable` 구독 토큰, 구독자별 예외 격리와 통계 추가 |
| Phase 5 세이브 안정성 | 핵심 완료·PlayMode 검증 대기 | tmp 검증, sav/bak 교체, 백업 복구, 1.x→2.0 마이그레이션, 미래 버전 차단, 중복 저장 방지와 구조화된 작업 결과 추가 |
| Phase 6 책임 분리 | 1차 구현 | 파티 Roster/BattleOrder 규칙을 Unity 비의존 `PartyRosterService`로 추출하고 Facade에서 위임 |
| Phase 7 asmdef·자동화 테스트 | 1차 구현 | `UPlayGround.Core`와 EditMode 테스트 어셈블리 추가, 파티 편성 규칙 회귀 테스트 추가 |
| Phase 8 업데이트 계약 | 1차 구현 | Update/FixedUpdate/LateUpdate 능력 인터페이스와 구현체별 캐시 순회 추가 |

2026-06-19 기준 전체 솔루션 컴파일 오류 0개, Unity EditMode 테스트 4/4 통과를 확인했다. Unity PlayMode에서 Addressable 키 누락, 정상 부팅, 종료 중 취소, 로딩 씬 왕복, 세이브 쓰기 중단 시나리오는 `PROJECT_SYSTEM_SMOKE_TEST_CHECKLIST.md`에 따라 추가 검증이 필요하다.

### 2026-06-18 Phase 2 구현 메모

- `Loading` 씬의 `SceneContext`는 중간 상태 신호로만 처리하고 전체 로드를 완료하지 않는다.
- 요청한 실제 대상 씬 이름과 준비 신호를 보낸 씬 이름이 일치할 때만 `Completed`로 전환한다.
- `OnLoadComplete`는 대상 씬의 `SceneContext` 준비 이후에만 호출한다.
- 로딩 중 새 요청은 경고 후 무시한다.
- `StartPendingLoad()` 중복 호출을 차단한다.
- 대상 씬 정보가 없으면 현재 요청을 실패 처리한 뒤 Title 직접 로드로 복구한다.

### 2026-06-18 Phase 3 구현 메모

- `AssetLifetime.Global`, `AssetLifetime.Scene`을 추가하고 타입+키+수명 범위별 핸들을 캐시한다.
- 모든 공통 로드 요청에 소유자 이름을 기록하고 개발 빌드 종료 시 활성 핸들 통계를 출력한다.
- 씬 범위 에셋은 새 씬의 `Awake/Start`에서 로드한 자산을 오해제하지 않도록 `SceneContext` 준비 시점이 아니라 씬 전환 요청 직전에 해제한다.
- Camera 설정/흔들림/대화/KillCam/전투 프로필/FOV 데이터와 FX DB, ItemActor, HitStop Volume, VitalOrb 데이터를 공통 전역 로드 경로로 이전했다.
- `CameraManager`, `GameObjectManager`를 비동기 초기화 계약에 포함해 필수 데이터 준비를 `GameManager`가 대기한다.
- 매니저 폴더의 직접 `Addressables.Load/Release` 호출은 `AssetManager` 내부만 남겼다.
- 남은 직접 호출은 개별 런타임 컴포넌트의 인스턴스/연출 수명 경로이며, 다음 이전 단위에서 `InstantiateAsync`와 반환 정책을 별도로 정리한다.

---

## 1. 목적

현재 프로젝트는 `GameManager`가 모든 매니저를 순차 등록하고, 각 매니저가 싱글톤과 다른 매니저의 `Instance`에 직접 접근하는 구조다. 기능은 동작하지만 시스템 수가 늘어나면서 다음 위험이 커지고 있다.

- 동기 `Init()` 안에서 시작한 비동기 데이터 로드가 완료되기 전에 `AfterInit()`이 실행된다.
- 씬 로딩 완료, 씬 활성화, `SceneContext` 준비 시점이 명확하게 분리되지 않는다.
- Addressables 핸들의 소유자와 해제 시점이 호출부마다 다르다.
- 이벤트 키에 Payload 타입이 포함되지 않아 잘못된 구독 조합을 컴파일 시점에 막지 못한다.
- 세이브 파일을 최종 경로에 직접 기록해 쓰기 중단 시 복구 수단이 부족하다.
- 대형 런타임 클래스와 단일 `Assembly-CSharp` 경계 때문에 변경 영향 범위와 테스트 비용이 크다.

이 계획은 전체 시스템을 한 번에 교체하지 않는다. 먼저 부팅·씬·저장처럼 실패 영향이 큰 기반 계층을 안정화하고, 이후 책임 분리와 테스트 경계를 점진적으로 도입한다.

---

## 2. 기본 원칙

| 원칙 | 적용 기준 |
|------|-----------|
| 점진적 전환 | 기존 public API를 즉시 제거하지 않고 신규 경로와 호환 계층을 병행한다. |
| 실패 조기 노출 | 필수 데이터나 매니저가 없으면 빈 인스턴스로 계속 진행하지 않고 명확한 오류 상태로 전환한다. |
| 준비 상태 명시 | `bool IsInitialized` 하나가 아니라 초기화·로딩 상태와 실패 원인을 표현한다. |
| 소유권 명시 | Addressables 핸들, 이벤트 구독, CancellationToken의 생성자와 해제자를 일치시킨다. |
| 검증 가능한 단위 | Unity API 호출과 순수 규칙을 분리해 EditMode 테스트가 가능한 코드를 늘린다. |
| 동작 보존 우선 | 구조 변경 단계에서는 게임 규칙과 밸런스 수치를 변경하지 않는다. |

---

## 3. 현재 기준선

### 3.1 확인된 구조

| 항목 | 현재 상태 |
|------|-----------|
| 매니저 생명주기 | `Init → AfterInit → Update 계열 → Dispose → OnSceneChanged` |
| 비동기 진입점 | `async void`, `UniTaskVoid` 기반 호출 다수 |
| Addressables | 개별 매니저와 컴포넌트가 직접 로드하고 일부만 Release |
| 씬 완료 이벤트 | `allowSceneActivation = true` 설정 직후 `OnLoadComplete` 발송 |
| 이벤트 키 | `(Enum Type, int Value)` |
| 세이브 쓰기 | 암호화 후 최종 `.sav` 경로에 직접 `File.WriteAllBytes` |
| 프로젝트 asmdef | 자체 런타임/에디터/테스트 asmdef 없음 |
| 자동화 테스트 | 프로젝트 핵심 시스템 대상 테스트 어셈블리 없음 |

### 3.2 주요 관련 파일

```text
Assets/02.Scripts/Manager/
├── GameManager.cs
├── Base/
│   ├── BaseManager.cs
│   └── IManager.cs
├── AssetManager.cs
├── Event/EventManager.cs
├── SceneManager.cs
├── Scene/SceneManager.Load.cs
├── Save/SaveManager.cs
└── Party/PartyManager.cs
```

---

## 4. 목표 구조

```text
GameBootstrapper
    │
    ├── Phase 1: Core
    │     Save / Input / Settings
    │
    ├── Phase 2: Data
    │     Asset / Item / Recipe / Actor Database
    │
    ├── Phase 3: Gameplay
    │     Party / Combat / Quest / Story / World State
    │
    └── Phase 4: Presentation
          UI / Camera / Sound

각 Phase
    InitializeAsync(token)
        ├── 성공 → 다음 Phase
        ├── 선택 기능 실패 → 경고 + 폴백
        └── 필수 기능 실패 → BootFailed 상태
```

씬 전환은 다음 상태를 명시적으로 가진다.

```text
Idle
  → LoadingTransitionScene
  → LoadingTargetScene
  → AwaitingActivation
  → Activating
  → WaitingForSceneContext
  → Completed
```

---

## 5. 단계별 실행 계획

### Phase 0. 기준선과 관측성 확보

목표: 구조 변경 전 현재 동작을 재현하고 실패 위치를 확인할 수 있게 한다.

### 작업 항목

- 부팅 단계별 시작/완료/소요 시간 로그 추가
- 매니저별 초기화 상태와 실패 원인을 표시하는 개발용 진단 정보 추가
- 씬 로딩 상태, 대상 씬, 진행률, `SceneContext` 준비 여부 기록
- Addressables 로드 키, 소유자, 핸들 상태를 확인할 수 있는 개발 로그 정의
- 현재 주요 플레이 흐름 수동 스모크 테스트 체크리스트 작성

### 필수 스모크 시나리오

1. Boot에서 Title 진입
2. 새 게임으로 GamePlay 씬 진입
3. GamePlay에서 다른 맵으로 전환
4. 파티 캐릭터 교체
5. 저장 후 타이틀로 이동
6. 저장 슬롯 로드 후 원래 맵과 위치 복원
7. 종료 후 재실행하여 저장 데이터 재로드

### 완료 기준

- 각 시나리오의 기대 결과와 실제 결과를 반복 확인할 수 있다.
- 초기화 실패가 단순 NullReference가 아니라 어느 단계에서 발생했는지 로그로 식별된다.

---

### Phase 1. 비동기 매니저 초기화 정비

목표: 필수 Addressables와 설정 데이터가 준비되기 전에 다음 시스템이 사용되는 문제를 제거한다.

### 대상

- `GameManager`
- `IManager`
- `SettingsManager`
- `AssetManager`
- `UIManager`
- `SoundManager`
- `PartyManager`
- Item/Recipe/Quest 등 비동기 DB 로드 매니저

### 작업 항목

1. 비동기 초기화 계약 추가

```csharp
public interface IAsyncInitializableManager
{
    UniTask InitializeAsync(CancellationToken cancellationToken);
}
```

2. `GameManager`에 부팅 상태 추가

```csharp
public enum GameBootState
{
    None,
    Initializing,
    Ready,
    Failed,
    Disposing,
}
```

3. 매니저를 의존 순서별 Phase로 등록
4. `async void` 초기화 메서드를 `UniTask` 반환형으로 변경
5. 필수 로드와 선택 로드를 구분
6. 부팅 취소용 `CancellationTokenSource`를 `GameManager`가 소유
7. `AfterInit()`은 모든 필수 초기화 성공 후에만 실행
8. 기존 `Init()`은 전환 기간에 동기 등록 전용으로 제한

### 호환 전략

- 1차 단계에서는 기존 `IManager.Init()`을 유지한다.
- 비동기 작업이 없는 매니저는 기존 경로를 사용한다.
- 비동기 매니저만 `IAsyncInitializableManager`를 추가 구현한다.
- 모든 매니저 전환이 끝난 뒤 `Init/AfterInit` 통합 여부를 결정한다.

### 위험 요소

- 초기화 순서 변경으로 기존 암묵적 의존성이 드러날 수 있다.
- 씬에 미리 배치된 매니저와 자동 생성된 매니저의 `Awake` 순서가 다를 수 있다.
- 부팅 중 UI가 필요한 오류 표시 경로는 최소 Bootstrap UI로 분리해야 한다.

### 완료 기준

- `PartyManager.AfterInit()` 실행 시 `PartyConfigSO` 로드가 완료되어 있다.
- `AssetManager.IsLoaded` 폴링 없이 준비 완료를 await할 수 있다.
- 필수 Addressable 키가 없으면 게임이 Ready 상태로 진입하지 않는다.
- PlayMode 종료 또는 GameManager 파괴 시 진행 중 초기화가 취소된다.

---

### Phase 2. 씬 전환 상태 머신 정비

목표: 로딩 완료와 실제 씬 준비 완료를 구분하고 중복 요청·실패·취소를 일관되게 처리한다.

### 작업 항목

- `SceneLoadState` enum 추가
- 로딩 요청을 `SceneLoadRequest` 데이터로 표현
- `_activateCallback` 대신 명시적 활성화 메서드와 현재 작업 핸들 보관
- `allowSceneActivation = true` 이후 실제 비동기 작업 완료까지 await
- `SceneContext.OnSceneContextReady`를 최종 준비 완료 신호로 사용
- 다음 이벤트를 분리

```text
OnLoadStarted
OnReadyToActivate
OnSceneActivated
OnSceneContextReady
OnLoadFailed
```

- `_isLoading` 해제는 `SceneContext` 준비 또는 명시적 실패 처리 후 수행
- 로딩 중 새 요청 정책 정의: 무시, 큐잉, 현재 작업 취소 중 하나를 API별로 명시
- `UniTaskVoid` 내부 예외가 유실되지 않도록 반환형과 호출부 정리

### 완료 기준

- `OnLoadComplete` 성격의 이벤트는 새 씬의 `SceneContext` 준비 이후에만 발생한다.
- 로딩 중 대상 씬이 없거나 로드 실패해도 `_isLoading`이 영구 고정되지 않는다.
- 같은 프레임의 중복 로딩 요청이 하나의 정책으로 처리된다.
- 로딩 화면 최소 표시 시간과 실제 로드 진행률 계산이 분리되어 있다.

---

### Phase 3. Addressables 소유권 통합

목표: 로드 중복, 핸들 누수, 해제 후 참조 사용을 방지한다.

### 작업 항목

- `AssetManager`에 공통 로드 API 추가
- 에셋 타입과 키를 조합한 핸들 캐시 도입
- 전역 에셋과 씬 범위 에셋 구분
- 인스턴스 생성과 단순 에셋 로드 API 분리
- 실패 결과에 키, 타입, 예외, 상태 포함
- 모든 로드 API에 CancellationToken 전달
- 개발 빌드에서 미해제 핸들 통계 출력

### 제안 API

```csharp
public UniTask<AssetLease<T>> AcquireAsync<T>(
    string key,
    AssetLifetime lifetime,
    CancellationToken cancellationToken);
```

`AssetLease<T>`가 Dispose될 때 참조를 반납하도록 구성한다. 단, 초기 구현 비용이 크면 매니저가 핸들을 직접 보관하고 `ReleaseScope()`를 제공하는 방식부터 시작한다.

### 마이그레이션 순서

1. `AssetManager`, `SettingsManager`
2. `UIManager`, `SoundManager`
3. Item/Recipe/Actor DB
4. Camera profile과 Dialogue 데이터
5. FX, Item, Projectile 인스턴스 생성 경로

### 완료 기준

- Addressables 직접 호출 위치가 승인된 기반 계층으로 제한된다.
- 같은 키·타입의 전역 에셋은 중복 로드하지 않는다.
- 씬 범위 에셋은 씬 전환 후 해제된다.
- 개발 로그에서 미해제 핸들의 키와 소유자를 확인할 수 있다.

---

### Phase 4. 이벤트 시스템 타입 및 범위 안전성 강화

목표: 잘못된 Payload 타입과 씬 전환 구독 손실을 방지한다.

### 작업 항목

- 이벤트 키에 Payload 타입 포함

```csharp
(Type enumType, int enumValue, Type payloadType)
```

- 데이터 없는 이벤트는 `Unit` 또는 전용 무데이터 키로 구분
- 같은 Enum 값에 다른 Payload 타입 등록 시 명확한 예외 또는 오류 로그 출력
- `Global`과 `Scene` 구독 범위 분리
- `IDisposable` 구독 토큰 반환 API 검토
- 이벤트 발송 중 구독 해제·추가가 발생하는 경우의 순회 정책 정의
- 한 구독자의 예외가 나머지 구독자 실행을 중단할지 정책 정의

### 완료 기준

- 잘못된 Payload 타입 조합이 첫 등록 시점에 탐지된다.
- 씬 전환 시 Scene 구독만 제거되고 Global 구독은 유지된다.
- 중복 구독과 남은 구독자를 개발 도구에서 확인할 수 있다.

---

### Phase 5. 세이브 안정성과 버전 마이그레이션

목표: 저장 중단·파일 손상·버전 변경 상황에서 복구 가능한 저장 시스템을 만든다.

### 작업 항목

1. 원자적 쓰기

```text
serialize
  → encrypt
  → save_slot_n.tmp 쓰기
  → tmp 복호화/역직렬화 검증
  → 기존 sav를 bak으로 이동
  → tmp를 sav로 교체
```

2. 슬롯별 `.bak` 유지 및 복구 API 추가
3. `saveVersion` 검사와 버전별 Migrator 도입
4. 지원하지 않는 미래 버전 로드 차단
5. 암호문 또는 Payload 체크섬 검토
6. 저장 중복 실행 방지
7. 파일 IO와 직렬화의 프레임 정지 측정 후 필요 시 비동기화
8. `ISaveable`별 Export/Import 실패 결과를 구조화

### 제안 구조

```text
SaveManager
├── SaveSerializer
├── SaveFileStore
├── SaveMigrationPipeline
└── SaveValidationResult
```

### 완료 기준

- 저장 도중 강제 종료를 모사해도 기존 정상 세이브가 보존된다.
- 지원하는 이전 버전 데이터가 현재 버전으로 변환된다.
- 손상된 본 파일이 있을 때 백업 파일 복구 여부를 판단할 수 있다.
- 부분 Import 실패가 로그 문자열뿐 아니라 결과 데이터로 호출자에게 반환된다.

---

### Phase 6. 대형 런타임 클래스 책임 분리

목표: 기능 변경 시 수정 범위와 회귀 위험을 줄인다.

### 우선 대상

| 클래스 | 분리 방향 |
|--------|-----------|
| `PartyManager` | Roster, 성장, 스왑, 스왑 회피, 저장 어댑터 |
| `PlayerCombat` | 입력/행동 요청, 콤보, 판정 연동, 전투 상태, 피드백 연결 |
| `ActorMovementController` | 상태 호스팅, KCC 콜백 위임, RootMotion, 물리 보조 |
| `CameraManager` | 모드, 효과, 흔들림, 타겟/락온, 씬 참조 재수집 |

### 분리 원칙

- 먼저 순수 계산이나 데이터 변환 책임을 일반 C# 클래스로 추출한다.
- MonoBehaviour는 Unity 참조 수집과 생명주기 연결에 집중한다.
- public API를 한 번에 변경하지 않고 기존 Facade가 신규 서비스로 위임한다.
- partial 분리는 탐색성을 개선하지만 책임을 줄이지 않으므로 최종 해결책으로 간주하지 않는다.

### `PartyManager` 1차 목표

```text
PartyManager (Facade)
├── PartyRosterService
├── PartyProgressionService
├── CharacterSwapController
├── SwapEvadeEvaluator
└── PartySaveAdapter
```

### 완료 기준

- 각 서비스의 상태 소유권이 중복되지 않는다.
- 성장 계산과 Roster 변경은 EditMode 테스트에서 MonoBehaviour 없이 검증할 수 있다.
- 기존 UI와 저장 시스템은 `PartyManager` Facade를 통해 동작을 유지한다.

---

### Phase 7. Assembly Definition과 자동화 테스트 도입

목표: 컴파일 경계를 줄이고 핵심 시스템의 회귀를 자동 검증한다.

### asmdef 도입 순서

1. `UPlayGround.Runtime`
2. `UPlayGround.Editor`
3. `UPlayGround.Tests.EditMode`
4. `UPlayGround.Tests.PlayMode`
5. 필요성이 확인된 이후 AI/UI 등 하위 모듈 추가 분리

처음부터 지나치게 많은 asmdef를 만들지 않는다. 순환 참조를 먼저 정리하고 큰 Runtime/Editor 경계를 확보한 뒤 세부 분리를 판단한다.

### 1차 테스트 대상

| 구분 | 테스트 |
|------|--------|
| EditMode | 세이브 마이그레이션과 검증 |
| EditMode | 파티 경험치·레벨·Roster 규칙 |
| EditMode | 이벤트 키와 Payload 타입 검증 |
| EditMode | InputBuffer 만료·소비 규칙 |
| EditMode | BT 노드와 전투 Intent 점수 계산 |
| PlayMode | 매니저 초기화 순서와 실패 처리 |
| PlayMode | 씬 활성화부터 SceneContext 준비까지의 상태 전이 |
| PlayMode | Addressables 로드·해제 기본 흐름 |
| PlayMode | Save → Load 라운드트립 |

### 관련 문서

- `Assets/docs/TODO/AUTOMATED_TEST_DESIGN.md`

### 완료 기준

- 런타임 코드와 Editor 코드가 별도 어셈블리로 컴파일된다.
- 테스트 코드가 플레이어 빌드에 포함되지 않는다.
- Phase 1~5에서 추가한 핵심 규칙에 최소 한 개 이상의 회귀 테스트가 있다.

---

### Phase 8. 싱글톤과 업데이트 계약 정리

목표: 자동 생성으로 설정 오류가 숨겨지는 문제와 빈 Update 호출 계약을 줄인다.

### 작업 항목

- 필수 매니저와 자동 생성 가능한 매니저 구분
- 필수 매니저 누락 시 개발 빌드에서 fail-fast
- `FindFirstObjectByType` 및 런타임 GameObject 자동 생성 경로 최소화
- 매니저 Prefab 또는 Bootstrap 씬 구성 검토
- `IManager`를 능력별 인터페이스로 분리

```csharp
public interface IUpdatableManager
{
    void OnUpdate();
}

public interface IFixedUpdatableManager
{
    void OnFixedUpdate();
}
```

- `GameManager`는 구현된 능력 목록만 별도 캐시해 순회
- `OnSceneChanged(string)`의 문자열을 `SceneContext` 또는 강타입 Scene 정보로 교체

### 완료 기준

- 필수 직렬화 참조가 필요한 매니저가 빈 GameObject로 자동 생성되지 않는다.
- 빈 Update 메서드만 가진 매니저는 프레임 순회 대상에서 제외된다.
- 씬 변경 콜백이 문자열 비교에 의존하지 않는다.

---

## 6. 우선순위와 의존 관계

| 순서 | Phase | 우선순위 | 선행 조건 | 예상 위험 |
|------|-------|----------|-----------|-----------|
| 1 | Phase 0 관측성 | 최우선 | 없음 | 낮음 |
| 2 | Phase 1 비동기 초기화 | 최우선 | Phase 0 | 높음 |
| 3 | Phase 2 씬 전환 | 최우선 | Phase 1 일부 | 높음 |
| 4 | Phase 3 Addressables | 높음 | Phase 1 | 중간~높음 |
| 5 | Phase 5 세이브 | 높음 | Phase 0 | 중간 |
| 6 | Phase 4 이벤트 | 중간 | 테스트 기반 권장 | 중간 |
| 7 | Phase 7 asmdef/테스트 | 중간 | 순환 의존 조사 | 중간~높음 |
| 8 | Phase 6 클래스 분리 | 중간 | Phase 7 권장 | 중간 |
| 9 | Phase 8 싱글톤/업데이트 | 낮음 | Phase 1, 7 | 중간 |

Phase 7의 전체 asmdef 전환은 후순위지만, Phase 1~5에서 추출되는 순수 클래스용 테스트 어셈블리는 가능한 한 조기에 추가하는 것이 좋다.

---

## 7. 권장 작업 단위

한 브랜치 또는 한 변경 묶음에서 여러 기반 시스템을 동시에 바꾸지 않는다.

| 작업 단위 | 포함 범위 |
|-----------|-----------|
| A | 부팅 상태와 진단 로그만 추가 |
| B | `AssetManager` 비동기 초기화 전환 |
| C | `PartyManager` Config 로드 대기 보장 |
| D | 씬 로딩 상태 enum과 이벤트 분리 |
| E | Addressables 공통 로드 API 및 첫 호출부 이전 |
| F | EventManager Payload 타입 키 추가 |
| G | 세이브 임시 파일·백업 교체 |
| H | 세이브 Migrator와 버전 테스트 |
| I | Runtime/Editor asmdef 경계 |
| J | `PartyManager` 첫 서비스 추출 |

각 작업 단위는 컴파일 성공, 해당 스모크 시나리오 통과, 신규 경고 없음까지 확인한 뒤 다음 단계로 진행한다.

---

## 8. 검증 전략

### 정적 검증

- `dotnet build UPlayground.sln --no-restore`
- `async void` 신규 추가 여부 검색
- Addressables 직접 호출 위치 검색
- `Addressables.Release` 대응 여부 점검
- 런타임 어셈블리에서 `UnityEditor` 참조 여부 확인

### Unity 검증

- Console Error 0개
- Domain Reload 활성/비활성 환경 모두 부팅 확인
- Enter Play Mode Options 조합별 싱글톤 정적 상태 확인
- Addressables Event Viewer 또는 Profiler로 핸들 잔존 확인
- 씬 왕복 전환 10회 후 이벤트 중복·메모리 증가 확인
- 세이브 슬롯 저장/덮어쓰기/손상/백업 복구 확인

### 회귀 기준

- 조작, 전투, 파티 교체, UI 표시 결과가 구조 변경 전과 동일하다.
- 부팅 시간과 씬 전환 시간이 기준선 대비 유의미하게 악화되지 않는다.
- 필수 데이터 실패 시 무한 대기하지 않고 실패 상태와 원인을 표시한다.

---

## 9. 롤백 기준

다음 조건 중 하나가 발생하면 해당 작업 단위를 이전 호환 경로로 되돌린다.

- 씬 전환 후 입력 또는 UI 구독이 중복된다.
- 저장 데이터가 이전 버전에서 복원되지 않는다.
- Addressables 해제 후 사용 예외가 반복 발생한다.
- 부팅 실패가 사용자에게 복구 불가능한 검은 화면으로 남는다.
- 변경 범위와 무관한 전투/AI 동작이 달라진다.

호환 API 제거는 최소 두 개의 안정화 작업 단위가 완료되고, 기존 호출부가 남아 있지 않음을 검색으로 확인한 뒤 진행한다.

---

## 10. 제외 범위

이번 계획에는 다음을 포함하지 않는다.

- 전투 밸런스 수치 변경
- Behavior Tree 행동 설계 변경
- UI 비주얼 리디자인
- 전체 DOTS/ECS 전환
- 모든 싱글톤의 즉시 DI 컨테이너 전환
- 모든 코드의 100% 테스트 커버리지

---

## 11. 최종 완료 조건

- 게임 부팅과 씬 전환의 상태 및 실패 원인이 명시적으로 표현된다.
- 필수 비동기 데이터 준비 전에 Gameplay 시스템이 실행되지 않는다.
- Addressables 핸들의 소유자와 해제 시점을 추적할 수 있다.
- 이벤트 Payload와 구독 범위가 타입 및 수명주기 측면에서 안전하다.
- 세이브 쓰기 중단과 버전 변경에 대한 복구 경로가 있다.
- 핵심 런타임 규칙이 자동화 테스트로 보호된다.
- 대형 클래스가 Facade와 검증 가능한 서비스 단위로 점진 분리된다.

이 조건을 충족한 뒤 기존 `Init/AfterInit`, 직접 Addressables 호출, 레거시 씬 완료 이벤트 등 호환 경로를 제거한다.
