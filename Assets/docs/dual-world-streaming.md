# 듀얼 월드 씬 스트리밍 설계 (간이 오픈월드)

> 두 개의 거대 씬을 인접 영역으로 묶어 "간이 오픈월드"처럼 운용하기 위한 씬 아키텍처 설계 문서.
> 기존 단일 활성 씬 모델(`SceneManager.LoadScene` → Loading 씬 경유 풀 교체)을 깨지 않으면서, 두 거대 씬 간에는 로딩 화면 없이 심리스로 전환되도록 한다.

## 1. 배경 & 요구사항

### 1.1 현재 구조

| 요소 | 현재 동작 |
|------|----------|
| `SceneManager.LoadScene` | Loading 씬 경유 → `LoadSceneMode.Single` 풀 교체 |
| `SceneManager.LoadSceneDirect` | Loading 없이 풀 교체 (Boot → Title 전용) |
| `BaseManager<T>` | 싱글톤. 사실상 DontDestroyOnLoad와 동일하게 씬 전환에도 살아남음 |
| `SceneContext.Start()` | 각 씬에서 `GameManager` 보장 + `SceneManager.OnSceneContextReady`로 `SceneType`/`MapID` 통지 |
| `PortalActor` | `SceneTransition`(풀 교체) / `InMapTeleport`(동일 씬 워프) 2종 |
| `ActorSpawnManager.OnSceneChanged` | 스폰 기록 초기화 (씬이 풀 교체된다는 가정) |

### 1.2 새 요구사항

- **두 개의 거대 씬**(`World_A`, `World_B`)을 인접 영역으로 두고 자연스럽게 오갈 수 있어야 한다.
- 두 거대 씬 간 전환은 **로딩 화면 없이 심리스**여야 한다 (간이 오픈월드 체감).
- 지금까지의 던전/타이틀/테스트 씬으로의 전환은 **기존 LoadScene 흐름을 그대로** 사용한다.
- 1인 개발이므로 **DOTS Subscene이나 SECTR 같은 외부 솔루션은 도입하지 않는다.** 빌트인 `SceneManager` + Addressables 만으로 구성한다.

### 1.3 비목표 (Non-goals)

- 그리드 기반 무한 청크 스트리밍 (Genshin / SoulMask 류). 본 설계는 "큰 씬 2개를 짝지어 운용"에 한정.
- 멀티플레이/네트워크 동기화.
- 진정한 LOD0/LOD1 분리 스트리밍. (필요 시 후속 작업으로 분리)

---

## 2. 웹 리서치 요약

### 2.1 핵심 패턴

1. **Persistent + Gameplay Scene 분리** ([Unity Manual: Multi-Scene editing](https://docs.unity3d.com/Manual/MultiSceneEditing.html), [Unity Learn](https://learn.unity.com/tutorial/managing-projects-with-multi-scene-editing))
   - 매니저 / UI / 카메라처럼 모든 레벨에서 살아있어야 하는 객체는 별도 "Persistent" 씬에 두고, 각 월드는 그 위에 `LoadSceneMode.Additive`로 얹는다.
   - 본 프로젝트는 이미 `BaseManager<T>` 싱글톤이 그 역할을 대신하고 있으므로, **Persistent 씬 도입은 선택 사항**(권장도 아님 - 5절 참고).

2. **추가(Additive) 비동기 로딩 + 활성 씬 전환** ([80.lv: Smooth Scene Streaming](https://80.lv/articles/smooth-scene-streaming-with-unity3d), [outscal: Multiple Scenes Guide](https://outscal.com/blog/unity-scene-management-guide))
   - 인접 영역에 들어갈 무렵 다음 씬을 `LoadSceneAsync(..., Additive)` 로 백그라운드 로드.
   - 로드가 끝났을 때 시야가 가려지는 트리거(문/계단/터널)에서 `SetActiveScene` 으로 라이팅·스카이박스 권한을 넘기고, 이전 씬은 거리/시야 기준으로 `UnloadSceneAsync`.
   - 큐잉(Queue)이 핵심: **다음 작업은 이전 작업 완료 후에만 시작**한다. 아니면 두 씬이 동시에 로드 중인 동안 프레임이 무너진다.

3. **포탈 (Doorway) 두 단계 트리거** ([outscal](https://outscal.com/blog/unity-scene-management-complete-guide), [Unity Discussions: Seamless Transition](https://discussions.unity.com/t/seamless-scene-transition/800018))
   - **Preload Trigger** (멀리 있는 큰 박스): 진입 시 인접 씬 비동기 로드 시작.
   - **Activate Trigger** (실제 경계, 좁은 게이트): 진입 시 `SetActiveScene` + 카메라/플레이어 위치 갱신, 반대편 씬 unload 큐잉.

4. **Addressables LoadSceneAsync 옵션 주의점** ([Unity Addressables 2.0 - Load a scene](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/LoadingScenes.html))
   - `activateOnLoad: false` 로 프리로드를 만들면 **Addressables 큐 전체가 막힌다.** 본 설계에서는 어드레서블 씬을 쓰더라도 `activateOnLoad: true` + Additive 만 사용.
   - 활성 씬 전환은 `SceneInstance.Scene`을 받아 `UnityEngine.SceneManagement.SceneManager.SetActiveScene` 으로 처리.

### 2.2 본 프로젝트에 적용/배제 결정

| 패턴 | 적용? | 이유 |
|------|-------|------|
| Multi-Scene Editing 워크플로 | ✅ | 작업자(나) 한 명이 두 거대 씬을 동시에 편집할 수 있어야 함 |
| Additive 비동기 로딩 + Active Scene 전환 | ✅ | 핵심. 심리스 전환의 골자 |
| 두 단계 트리거 (Preload / Activate) | ✅ | 기존 PortalActor 확장으로 흡수 |
| Persistent 매니저 씬 분리 | ❌ | `BaseManager<T>` 싱글톤이 동일 역할 수행 중. 중복 도입은 복잡도만 증가 |
| 그리드 청크 스트리밍 | ❌ | "큰 씬 2개" 요구를 초과. YAGNI |
| Addressables 씬화 | ⏸ 보류 | 빌트인 빌드 씬으로 충분. 원격 패치가 필요해지는 시점에 재검토 |

---

## 3. 아키텍처 개요

```
┌────────────────────────────────────────────────────────────┐
│  Persistent Singletons (BaseManager<T> 그룹)                │
│   - GameManager / SceneManager / CameraManager / UIManager  │
│   - GameObjectManager(Player 추적) / ActorSpawnManager ...  │
└────────────────────────────────────────────────────────────┘
                  ▲ 씬 전환과 무관하게 살아있음
                  │
┌─────────────────┴──────────────────────────────────────────┐
│ 활성(Active) 씬 (UnityEngine.SceneManagement.SceneManager) │
│                                                             │
│   ┌──────────────────┐         ┌──────────────────┐        │
│   │   World_A.unity   │ ◄────► │   World_B.unity  │        │
│   │  SceneContext     │  포탈   │  SceneContext    │        │
│   │  (MapID="WorldA") │ 두단계  │  (MapID="WorldB")│        │
│   └──────────────────┘  트리거  └──────────────────┘        │
│           ▲                                                 │
│  심리스 전환: Additive 로드 → SetActiveScene → 반대편 Unload│
└─────────────────────────────────────────────────────────────┘
```

### 3.1 씬 종류

| 분류 | 씬 | 비고 |
|------|----|----|
| 부트 / 메뉴 | `Boot`, `Title`, `Loading` | 변경 없음 |
| 거대 월드 (듀얼) | `World_Field`, `World_Town` *(이름 예시)* | 새 듀얼 월드. 서로 인접하며 심리스 전환 |
| 기타 단일 씬 | `InGame_Dungeon`, 테스트 씬들 | 기존 LoadScene 흐름 유지 |

### 3.2 전환 매트릭스

| From → To | 흐름 |
|-----------|------|
| Title → World_Field | `LoadScene` (기존, Loading 경유) |
| World_Field → World_Town | **신규: 심리스 듀얼 월드 전환** (3.3 참고) |
| World_Town → World_Field | 동일하게 신규 흐름 |
| World_* → InGame_Dungeon | `LoadScene` (기존, Loading 경유). 듀얼 월드를 모두 unload |
| InGame_Dungeon → World_* | `LoadScene` (기존). 진입 시 어느 월드로 들어갔는지 SaveData 기반 결정 |

### 3.3 심리스 듀얼 월드 전환 시퀀스

```
플레이어가 World_A에 위치
   │
   ▼
[1] PreloadTrigger 진입
    └─ DualWorldStreamer.RequestPreload(World_B)
         └─ SceneManager.LoadSceneAsync(World_B, Additive)
            (allowSceneActivation = true, but 활성 씬 전환은 아직 안 함)
            ※ 큐 보장: 이미 로드 중이거나 로드돼 있으면 무시
   │
   ▼ (플레이어가 계속 이동)
[2] ActivateTrigger 진입
    └─ DualWorldStreamer.SwitchActive(World_B)
         ├─ Wait UntilDone(이전 LoadAsync)
         ├─ SetActiveScene(World_B)        // 라이팅/스카이박스 권한 이동
         ├─ World_B SceneContext가 신규 활성 통지
         │  → SceneManager.OnSceneContextReady → MapID 갱신 → HUD 갱신
         ├─ Player를 World_B 루트로 MoveGameObjectToScene (선택)
         └─ Schedule Unload(World_A)       // 큐 (단, 즉시 unload 금지)
   │
   ▼
[3] World_A 측 UnloadGuard 트리거에서 멀어짐
    └─ DualWorldStreamer.UnloadIfFar(World_A)
         └─ SceneManager.UnloadSceneAsync(World_A)
```

핵심 원칙:
- **양방향 대칭**: 같은 트리거 컴포넌트를 양쪽 씬 경계에 대칭으로 배치한다.
- **연산 큐**: 한 시점에 진행 중인 Load/Unload는 최대 1개. 동시 다발 호출은 큐잉.
- **Unload 즉시 금지**: 활성 전환 직후에 unload 하면 LOD/그래픽스 셔터링이 발생. 안전 거리(예: 80m) 이상 멀어지면 unload.

---

## 4. 신규 / 수정 컴포넌트

### 4.1 `SceneManager.DualWorld.cs` (신규 partial 파일)

기존 `SceneManager` partial에 듀얼 월드 전환 API를 추가한다. (현재 `SceneManager.Load.cs` 와 같은 디렉토리: `Assets/02.Scripts/Manager/Scene/SceneManager.DualWorld.cs`)

핵심 API:
```csharp
public partial class SceneManager
{
    private string _activeWorldScene;          // 현재 SetActive 된 World 씬 이름
    private string _adjacentWorldScene;        // Preload 되어 있는 인접 World 씬 이름
    private bool   _isWorldOpInFlight;         // Load/Unload 진행 중 플래그 (큐 가드)
    private readonly Queue<Func<UniTask>> _worldOpQueue = new();

    public string  ActiveWorldScene   => _activeWorldScene;
    public string  AdjacentWorldScene => _adjacentWorldScene;

    /// <summary>인접 World 씬을 Additive로 비동기 로드. 이미 로드 중/완료면 무시.</summary>
    public void RequestPreloadAdjacentWorld(string sceneName);

    /// <summary>활성 World를 sceneName으로 전환. 이전 활성 World는 unload 예약.</summary>
    public void SwitchActiveWorld(string sceneName);

    /// <summary>플레이어가 충분히 멀어지면 호출. 이전 World 씬을 안전하게 unload.</summary>
    public void RequestUnloadFarWorld(string sceneName);

    /// <summary>Title→World 진입 시 사용. 시작 World를 Single 모드로 로드.</summary>
    public void EnterDualWorld(string startWorldScene);

    /// <summary>듀얼 월드를 떠날 때 (예: 던전 진입). 모든 World 씬 unload + 일반 LoadScene.</summary>
    public void LeaveDualWorld(string nextSceneName);
}
```

큐잉 규칙:
- `_worldOpQueue`에 들어간 작업은 직렬로 await 처리한다.
- Preload 요청과 SwitchActive 요청이 같은 씬에 대해 누적될 수 있으므로, **SwitchActive는 Preload 완료 후에만 실행되도록 큐 순서를 보장**한다.
- Unload는 가장 마지막에 실행되며, 활성 씬 전환 직후 최소 1프레임은 기다린 뒤 실행 (`UniTask.NextFrame()`).

상태 다이어그램:
```
        RequestPreload          SwitchActive         RequestUnload
A only ─────────────────► A+B ─────────────────► A+B(active=B) ──────────► B only
   ▲                                                                         │
   └──────────────────── RequestPreload(A) + SwitchActive(A) ◄────────────────┘
```

### 4.2 `DualWorldPortal` (신규 컴포넌트)

기존 `PortalActor`의 `PortalType`에 두 케이스를 추가하기보다, **별도 컴포넌트로 분리**한다 (관심사 분리: PortalActor는 풀 교체/맵 내 워프 전용 그대로 유지).

위치: `Assets/02.Scripts/Scene/DualWorldPortal.cs`

```csharp
public enum DualWorldPortalRole
{
    Preload,    // 인접 씬을 백그라운드 로드만 함. 플레이어 위치는 안 바꿈
    Activate,   // 활성 씬을 전환. 플레이어가 이미 새 영역의 지오메트리 위에 있어야 함
    UnloadGuard // 충분히 멀어졌음을 의미. 이전 씬 unload 트리거
}

[RequireComponent(typeof(Collider))]
public class DualWorldPortal : MonoBehaviour
{
    [SerializeField] private DualWorldPortalRole _role;
    [SerializeField] private string _targetSceneName;   // 대상 씬 (Preload/Activate)
    [SerializeField] private string _sourceSceneName;   // 본인이 속한 씬 (UnloadGuard)
    [SerializeField] private bool _onlyOnce = false;    // 한 번만 작동시키고 비활성

    private bool _consumed;

    private void OnTriggerEnter(Collider other) { ... 플레이어 검사 → 역할별 SceneManager API 호출 ... }
}
```

배치 규칙:
- 두 World 씬의 경계 영역에 **다리/터널/계단처럼 시야가 좁아지는 구간**을 만들고:
  - Preload Trigger: 경계 N미터 앞 (충분히 빨리 로드 시작)
  - Activate Trigger: 경계 정중앙 (이 시점에 활성 씬 전환)
  - UnloadGuard Trigger: Activate 지점에서 안전 거리(예: 80m) 떨어진 반대쪽
- 두 씬 모두 자기 쪽에서 상대 씬으로 갈 때의 트리거 3종을 보유 (**대칭**).

### 4.3 `SceneContext` 확장

각 World 씬의 `SceneContext`에 듀얼 월드 식별자를 추가:
```csharp
public class SceneContext : MonoBehaviour
{
    public string SceneType;          // 기존
    public string MapID;              // 기존
    [Header("듀얼 월드 (선택)")]
    public bool   IsDualWorldRegion;  // true면 SceneManager가 듀얼 월드 모드로 인식
}
```

`SceneContext.Start()`에서 추가 호출:
```csharp
if (IsDualWorldRegion)
    SceneManager.Instance.OnDualWorldSceneLoaded(this);
```

### 4.4 `ActorSpawnManager.OnSceneChanged` 보완

현재 코드:
```csharp
public void OnSceneChanged(string sceneType)
{
    _spawnedActors.Clear();   // 풀 교체 가정
}
```

듀얼 월드에서 활성 씬이 World_A → World_B로 바뀔 때 `World_A`의 적/오브젝트는 아직 살아있다. **무조건 클리어하면 미니맵·웨이브 매니저가 망가진다.**

수정:
```csharp
public void OnSceneChanged(string sceneType)
{
    // 듀얼 월드 활성 전환은 액터 기록을 유지한다.
    // 풀 교체(Single 로드)일 때만 Unity가 객체를 정리하므로 그때만 클리어.
    if (SceneManager.Instance.IsDualWorldActiveSwitch) return;
    _spawnedActors.Clear();
}
```
→ `SceneManager`가 직전 전환이 듀얼 월드 활성 전환인지 플래그로 노출.

언로드되는 씬에 속한 액터만 정리하려면, `ActorSpawnManager`에 씬별 인덱스를 추가하거나, `SceneManager.UnloadSceneAsync` 콜백에서 `actor.gameObject.scene == unloaded` 인 항목을 솎아낸다. 후자가 단순하므로 권장.

### 4.5 `Scene.cs` (Enum) 추가

```csharp
public static class SceneName
{
    // 기존...
    public const string World_Field = "World_Field";
    public const string World_Town  = "World_Town";
}
```

### 4.6 `LeaveDualWorld` 시 카메라/플레이어 처리

현재 `CameraManager`/`GameObjectManager._player`는 씬 풀 교체 시 `FindWithTag("Player")`로 재수집된다. 듀얼 월드 활성 전환에서는 **플레이어 GameObject가 동일 객체 그대로** 유지되어야 한다 (재스폰 금지). 따라서:
- `World_*` 씬 양쪽에 Player 태그 객체를 두지 않는다. **Player는 첫 진입 시에만 활성 씬에 스폰**되어 그대로 유지.
- 활성 씬 전환 직후 `SceneManager.MoveGameObjectToScene(player, newActiveScene)` 호출 (선택, unload 시 동반 파괴 방지).

---

## 5. Persistent 씬을 도입하지 않는 이유 (의도된 결정)

웹 자료 다수가 "Persistent 매니저 씬"을 권장하지만, 본 프로젝트에는 도입하지 않는다.

근거:
1. `BaseManager<T>` 싱글톤이 Awake 시 자기 자신을 보장하고 씬 전환 동안 살아남는다 — 이미 동일 효과 달성.
2. `SceneContext.EnsureGameManagerInitialized()`가 단독 씬 실행에서도 매니저 풀을 자동 부팅한다 — Persistent 씬을 분리하면 이 단독 실행성을 깨뜨려 테스트 씬 워크플로가 망가진다.
3. Persistent 씬을 추가하면 Build Settings, BootLoader 흐름, 모든 테스트 씬의 멀티씬 셋업을 손봐야 한다. 이득 대비 비용 과다.

→ "Persistent 씬을 굳이 만들지 않은 채로도 같은 효과를 내고 있다"는 점은 본 프로젝트 아키텍처의 강점이며, 듀얼 월드 도입 후에도 유지한다.

---

## 6. 멀티 씬 에디팅 워크플로

### 6.1 에디터 셋업

`Assets/01.Scenes/GameLogic/_DualWorld_EditorSetup.unity` 같은 진입용 더미 씬을 만들 필요 없이, **`EditorSceneManager.GetSceneManagerSetup` 기반 SceneSetup ScriptableObject**를 한 개 만들어 두면 충분하다.

선택지:
- **A안 (간단):** Hierarchy에서 `World_Field` + `World_Town`을 동시에 열어두는 워크플로를 컨벤션화. 별도 도구 없음.
- **B안 (편의):** 에디터 메뉴 `UPlayGround/Open Dual World` 추가 — 두 씬을 동시에 열고 카메라를 경계 위치로 이동.

1인 개발이고, 먼저 A안으로 가다가 빈도가 잦아지면 B안으로 승격.

### 6.2 라이팅 / 스카이박스

- 두 World 씬은 각자의 라이팅 세팅을 가지며, **활성 씬의 라이팅 세팅이 적용**된다 (`SetActiveScene` 효과).
- 라이트 베이크는 씬별로 따로 굽는다. 멀티씬 라이팅(혼합 베이크)은 권장하지 않는다 — 1인 개발에서 빌드 시간 폭주의 원인.

### 6.3 Build Settings

빌드 씬 인덱스에 `World_Field`, `World_Town`을 추가. Boot/Title/Loading 다음 순서.

---

## 7. 단계별 구현 로드맵

> 각 단계는 별도 PR/커밋으로 분리. 한 단계 완료 후 게임이 정상 부팅되어야 함.

### Step 1. SceneManager 듀얼 월드 API 골격 (코드만)
- `SceneManager.DualWorld.cs` 생성 + 큐 인프라(UniTask 기반).
- 유닛 동작 검증: 빈 씬 두 개를 만들어 콘솔에서 LoadAdditive → SwitchActive → Unload 시퀀스만 통과시킴.
- `ActorSpawnManager.OnSceneChanged` 가드 추가.
- 영향: 기존 흐름 변경 없음.

### Step 2. SceneContext / SceneName 확장
- `SceneContext.IsDualWorldRegion` 플래그 추가.
- `SceneName.World_Field`, `SceneName.World_Town` 상수 추가.
- 영향: 기존 씬 직렬화 호환 (필드 추가만).

### Step 3. World_Field / World_Town 더미 씬 작성
- 1×1km 평면 + 경계 다리 + 양쪽 SceneContext.
- 빌드 씬에 등록.

### Step 4. DualWorldPortal 구현 + 두 씬 경계에 배치
- 양쪽 씬 경계에 Preload/Activate/UnloadGuard 트리거 3종 대칭 배치.
- Title 메뉴에서 "Enter World" → `EnterDualWorld(World_Field)` 진입 가능하게.

### Step 5. 수동 플레이 검증
- Field ↔ Town 왕복, Activate 시 라이팅 변화, 경계에서 멈춰서 왕복했을 때 큐가 안 깨지는지, FPS 스파이크.

### Step 6. 액터/UI 통합
- 미니맵(`UI_Minimap`) — 활성 씬 전환 시 `MapID`가 갱신되므로 자동 동작 여부 확인.
- 활성 씬 전환 직후 카메라가 튀지 않도록 `CameraManager.SnapToTarget` 호출 (포탈의 InMapTeleport와 동일한 처리).

### Step 7. 던전 진입 / 복귀 흐름
- `LeaveDualWorld(InGame_Dungeon)` → 듀얼 월드 모두 unload 후 일반 LoadScene.
- 던전 종료 후 복귀 시 SaveData에 마지막 활성 World 저장 → 그쪽으로 EnterDualWorld.

---

## 8. 리스크 / 엣지 케이스

| 리스크 | 영향 | 완화 |
|--------|------|------|
| 경계에서 빠르게 왕복 (대시) | Load/Unload가 큐에 쌓여 메모리 진동 | (a) UnloadGuard에 디바운스(2초). (b) Unload가 큐에 들어가 있는데 SwitchActive로 그 씬으로 다시 돌아오면 Unload 취소 |
| Activate 시점에 아직 Preload 미완 | 한 프레임 검은 화면 / 라이팅 깨짐 | Preload Trigger를 충분히 앞에 배치 + Activate 큐가 Preload 완료를 await |
| 두 씬 모두 NavMesh 사용 | 활성 씬 전환 시 NavMesh 재계산 비용 | NavMesh를 Bake가 아닌 NavMeshSurface(컴포넌트) 단위로 분리. 씬 unload 시 자동 해제 |
| 라이트 프로브 / 리플렉션 프로브 경계 | 활성 씬 전환 순간 경계에서 색감 점프 | 경계 구간 자체를 터널/실내로 만들어 시각적으로 가림 (5절 웹 자료 권장 패턴) |
| Player가 World_A 씬 소속이라 unload 시 같이 파괴됨 | 게임 즉사 | Player 진입 시 `SceneManager.MoveGameObjectToScene(player, World_B)` 또는 처음부터 활성 씬 외부에 배치 후 활성 씬으로 이동 |
| Addressables 큐 블로킹 | 다른 어드레서블 로드가 같이 멈춤 | `activateOnLoad: false` 사용 금지. 본 설계는 빌트인 `SceneManager.LoadSceneAsync`만 사용 |
| `BaseManager<T>.OnSceneChanged` 가 Single 풀교체 가정 | Field→Town 전환 시 매니저 상태가 잘못 리셋 | `SceneManager.IsDualWorldActiveSwitch` 플래그를 모든 매니저가 분기 처리. ActorSpawnManager는 4.4에서 처리, 다른 매니저는 케이스별로 점검 |

---

## 9. 변경 영향 표

| 파일/모듈 | 변경 |
|-----------|------|
| `Manager/Scene/SceneManager.DualWorld.cs` | **신규** |
| `Manager/Scene/SceneContext.cs` | `IsDualWorldRegion` 필드 추가 |
| `Manager/SceneManager.cs` | `OnDualWorldSceneLoaded` 진입점 + `IsDualWorldActiveSwitch` 노출 |
| `Manager/Actor/ActorSpawnManager.cs` | `OnSceneChanged` 가드. unload 콜백에서 해당 씬 액터만 솎아냄 |
| `Scene/DualWorldPortal.cs` | **신규** |
| `Scene/PortalActor.cs` | 변경 없음 (관심사 분리) |
| `Data/Enum/Scene.cs` | `World_Field`, `World_Town` 추가 |
| `01.Scenes/GameLogic/World_Field.unity` | **신규** |
| `01.Scenes/GameLogic/World_Town.unity` | **신규** |
| Build Settings | 두 씬 등록 |
| 다른 매니저(`CameraManager`, `UIManager`, `PartyManager` 등) | `OnSceneChanged`에서 듀얼 월드 활성 전환을 어떻게 다룰지 케이스별 점검 (별도 체크리스트 필요) |

---

## 10. 후속 작업 (이 설계의 범위 밖)

- **3개 이상의 거대 씬으로 확장**: 큐 + 인접 그래프(Adjacency Graph) 도입 필요. 현 설계는 인접 1개만 가정.
- **Addressables 원격 패치**: World 씬을 어드레서블화하면 콘텐츠 패치가 가능. 다만 `activateOnLoad: false` 큐 블로킹 이슈를 별도로 다뤄야 함.
- **LOD 분리 스트리밍**: 한 World 씬을 멀리/가까이로 더 쪼개는 작업. 그리드 청크로 가는 첫걸음이지만 1인 개발에서 비용 큼.
- **컷씬/스토리 트리거와의 결합**: `StoryManager`가 듀얼 월드 활성 전환을 트리거할 수 있도록 API 노출.

---

## 참고 자료

- [Unity Manual: Multi-Scene editing](https://docs.unity3d.com/Manual/MultiSceneEditing.html)
- [Unity Manual: Occlusion culling and Scene loading](https://docs.unity3d.com/Manual/occlusion-culling-scene-loading.html)
- [Unity Addressables 2.0: Load a scene](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/LoadingScenes.html)
- [80.lv — Smooth Scene Streaming with Unity3D](https://80.lv/articles/smooth-scene-streaming-with-unity3d)
- [outscal — Managing Multiple Scenes in Unity (Seamless Open World)](https://outscal.com/blog/unity-scene-management-guide)
- [outscal — A Complete Guide to Unity's Scene Management](https://outscal.com/blog/unity-scene-management-complete-guide)
- [Unity Discussions — Seamless scene transition](https://discussions.unity.com/t/seamless-scene-transition/800018)
- [Unity Blog — Achieve better Scene workflow with ScriptableObjects](https://blog.unity.com/engine-platform/achieve-better-scene-workflow-with-scriptableobjects)
- [Catlike Coding — Multiple Scenes](https://catlikecoding.com/unity/tutorials/object-management/multiple-scenes/)
- [GitHub — tiredamage42/OpenWorldFramework](https://github.com/tiredamage42/OpenWorldFramework)
