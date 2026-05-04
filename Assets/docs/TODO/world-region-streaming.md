# 월드 리전 스트리밍 설계 (간이 오픈월드)

> N개의 거대 씬을 인접 그래프로 묶어 "간이 오픈월드"처럼 운용하기 위한 씬 아키텍처 설계 문서.
> 기존 단일 활성 씬 모델(`SceneManager.LoadScene` → Loading 씬 경유 풀 교체)을 깨지 않으면서, 인접 리전 간에는 로딩 화면 없이 심리스로 전환되도록 한다.
> 파일명은 초기 "듀얼 월드" 시안을 따르고 있으나, **본 설계는 N=2 부터 임의 개수까지 일반화**되어 있다. 2개는 가장 단순한 특수 케이스다.

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

- **N개의 거대 씬**(Region)을 인접 영역으로 묶고 자연스럽게 오갈 수 있어야 한다. 초기 시안은 2개지만, 추후 3개 일렬, 허브-스포크, 4-인접 격자 같은 토폴로지로 확장될 수 있다.
- 인접 리전 간 전환은 **로딩 화면 없이 심리스**여야 한다.
- 비인접 리전(맵 워프, 던전 등)으로의 전환은 **기존 LoadScene 흐름**(Loading 씬 경유)을 그대로 사용한다.
- 1인 개발이므로 **DOTS Subscene이나 SECTR 같은 외부 솔루션은 도입하지 않는다.** 빌트인 `SceneManager` + Addressables 만으로 구성한다.
- **메모리 예산**을 명시적으로 둔다. 무한정 인접 리전을 동시 보유하지 않는다.

### 1.3 비목표 (Non-goals)

- 그리드 기반 무한 청크 스트리밍 (Genshin / 진짜 오픈월드 류). 본 설계는 "수~수십 개 리전을 명시적 그래프로 운용"에 한정.
- 멀티플레이/네트워크 동기화.
- 리전 내부의 LOD0/LOD1 분리 스트리밍. (필요 시 후속 작업으로 분리 — 10절)

---

## 2. 웹 리서치 요약

### 2.1 핵심 패턴

1. **Persistent + Gameplay Scene 분리** ([Unity Manual: Multi-Scene editing](https://docs.unity3d.com/Manual/MultiSceneEditing.html), [Unity Learn](https://learn.unity.com/tutorial/managing-projects-with-multi-scene-editing))
   - 매니저 / UI / 카메라처럼 모든 레벨에서 살아있어야 하는 객체는 별도 "Persistent" 씬에 두고, 각 월드를 그 위에 `LoadSceneMode.Additive`로 얹는다.
   - 본 프로젝트는 이미 `BaseManager<T>` 싱글톤이 그 역할을 대신하고 있으므로, **Persistent 씬 도입은 선택 사항**(권장도 아님 — 5절).

2. **추가(Additive) 비동기 로딩 + 활성 씬 전환** ([80.lv: Smooth Scene Streaming](https://80.lv/articles/smooth-scene-streaming-with-unity3d), [outscal: Multiple Scenes Guide](https://outscal.com/blog/unity-scene-management-guide))
   - 인접 영역에 들어갈 무렵 다음 씬을 `LoadSceneAsync(..., Additive)` 로 백그라운드 로드.
   - 시야가 가려지는 트리거(문/계단/터널)에서 `SetActiveScene` 으로 라이팅·스카이박스 권한을 넘기고, 멀어진 씬은 `UnloadSceneAsync`.
   - 큐잉(Queue)이 핵심: **다음 작업은 이전 작업 완료 후에만 시작**.

3. **포탈 (Doorway) 두 단계 트리거** ([outscal](https://outscal.com/blog/unity-scene-management-complete-guide), [Unity Discussions: Seamless Transition](https://discussions.unity.com/t/seamless-scene-transition/800018))
   - **Preload Trigger** (큰 박스): 진입 시 인접 씬 비동기 로드.
   - **Activate Trigger** (좁은 게이트): 진입 시 `SetActiveScene` + 카메라/플레이어 후속 처리.
   - **UnloadGuard Trigger** (안전 거리): 통과 시 이전 씬 unload.

4. **Adjacency Graph 패턴** ([GitHub - tiredamage42/OpenWorldFramework](https://github.com/tiredamage42/OpenWorldFramework), [Unity Discussions: Open World Workflow](https://discussions.unity.com/t/open-world-map-creating-chunking-streaming-workflow/848971))
   - 리전 = 노드, 인접 관계 = 에지로 명시. 활성 리전을 중심으로 **N-hop 이웃**을 자동 preload, **그 외**는 LRU 기반 unload.
   - 본 설계는 1-hop preload + 메모리 예산 LRU를 채택.

5. **Addressables `LoadSceneAsync` 옵션 주의점** ([Unity Addressables 2.0](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/LoadingScenes.html))
   - `activateOnLoad: false` 로 프리로드를 만들면 **Addressables 큐 전체가 막힌다.** 본 설계에서는 어드레서블 씬을 쓰더라도 `activateOnLoad: true` + Additive 만 사용.

### 2.2 본 프로젝트에 적용/배제 결정

| 패턴 | 적용? | 이유 |
|------|-------|------|
| Multi-Scene Editing 워크플로 | ✅ | 인접 리전 동시 편집이 필수 |
| Additive 비동기 로딩 + Active Scene 전환 | ✅ | 핵심. 심리스 전환의 골자 |
| 두 단계 트리거 (Preload / Activate / UnloadGuard) | ✅ | 신규 컴포넌트로 흡수 |
| **Adjacency Graph (N개 일반화)** | ✅ | "단순 2개가 아닐 수 있다"는 요구의 직접 응답 |
| **메모리 예산 + LRU eviction** | ✅ | N개로 늘어났을 때 동시 로드 폭주 방지 |
| Persistent 매니저 씬 분리 | ❌ | `BaseManager<T>` 싱글톤이 동일 역할 수행 중 |
| 그리드 청크 스트리밍 | ❌ | 본 설계 범위 밖. YAGNI |
| Addressables 씬화 | ⏸ 보류 | 빌트인 빌드 씬으로 충분. 원격 패치 시점에 재검토 |

---

## 3. 아키텍처 개요

### 3.1 토폴로지 예시 (N으로 일반화)

```
N=2 (단순):              N=3 일렬:                 N=4 허브-스포크:           N=5 격자:
  A ─ B                   A ─ B ─ C                       A                   A ─ B
                                                          │                   │   │
                                                      D ─ HUB ─ B             C ─ D ─ E
                                                          │
                                                          C
```

활성 리전은 항상 1개. **활성 + 활성의 1-hop 이웃들**이 동시에 로드되어 있는 것이 정상 상태이며, 메모리 예산을 넘으면 LRU로 일부를 unload한다.

### 3.2 전체 그림

```
┌────────────────────────────────────────────────────────────┐
│  Persistent Singletons (BaseManager<T> 그룹)               │
│   - GameManager / SceneManager / CameraManager / UIManager │
│   - GameObjectManager(Player 추적) / ActorSpawnManager ... │
└────────────────────────────────────────────────────────────┘
                  ▲ 씬 전환과 무관하게 살아있음
                  │
┌─────────────────┴───────────────────────────────────────────┐
│ Active Scene + Loaded Adjacent Regions                      │
│                                                              │
│   ┌──────────┐   ┌──────────┐   ┌──────────┐                │
│   │ Region A │ ─ │ Region B │ ─ │ Region C │  (B가 활성)    │
│   │  loaded  │   │ ★active  │   │  loaded  │                │
│   └──────────┘   └──────────┘   └──────────┘                │
│      ▲                ▲              ▲                       │
│      └────────────────┴──────────────┘                       │
│        WorldRegionGraphSO 가 인접 관계를 정의                │
│        WorldRegionStreamer 가 활성 변경 시 자동 Load/Unload  │
└──────────────────────────────────────────────────────────────┘
```

### 3.3 씬 종류

| 분류 | 씬 | 비고 |
|------|----|----|
| 부트 / 메뉴 | `Boot`, `Title`, `Loading` | 변경 없음 |
| 리전 (월드) | `World_Field`, `World_Town`, ... (N개) | 그래프에 등록된 거대 씬들 |
| 기타 단일 씬 | `InGame_Dungeon`, 테스트 씬들 | 기존 LoadScene 흐름 유지 |

### 3.4 전환 매트릭스

| 시나리오 | 흐름 |
|---------|------|
| Title → 첫 리전 | `EnterRegion(startRegionId)` (Loading 경유 후 활성 리전 단일 로드) |
| 인접 리전 ↔ 인접 리전 | **신규: 심리스 스트리밍** (3.5) |
| 비인접 리전 워프 (맵 클릭) | `WarpToRegion(regionId)` (Loading 경유 + 모든 리전 unload + 새 리전 single 로드) |
| 리전 → 던전 (`InGame_Dungeon`) | `LeaveAllRegions(InGame_Dungeon)` → 기존 LoadScene |
| 던전 → 리전 | `EnterRegion(savedRegionId)` |

### 3.5 인접 리전 심리스 전환 시퀀스

```
플레이어가 Region B (활성) 에 위치, 이웃 = {A, C}
   상태: A loaded · ★B active · C loaded
   │
   ▼
[1] B→C 경계 PreloadTrigger 진입 (이미 C가 로드돼 있으면 no-op)
   │
   ▼
[2] B→C 경계 ActivateTrigger 진입
    └─ WorldRegionStreamer.SwitchActive(C)
         ├─ Wait UntilDone(이전 LoadAsync)
         ├─ SetActiveScene(C)
         ├─ NotifySceneContext(C) → MapID/HUD 갱신
         ├─ MoveGameObjectToScene(player, C)
         ├─ NewNeighbors = C의 이웃 = {B, D, ...}
         ├─ ToPreload  = NewNeighbors \ Loaded   → Preload 큐
         ├─ ToUnload   = Loaded \ (NewNeighbors ∪ {C})  → Unload 후보
         └─ Apply Memory Budget (LRU)
   │
   ▼
[3] UnloadGuard 통과 또는 디바운스 만료
    └─ Unload 후보 중 안전한 항목 실제 UnloadSceneAsync
```

핵심 원칙:
- **활성 변경 트리거가 곧 그래프 재계산**: 이웃이 자동으로 preload되고, 더 이상 이웃이 아닌 리전은 unload 후보로.
- **연산 큐**: 동시에 진행 중인 Load/Unload는 1개. 직렬화.
- **Unload 즉시 금지**: 활성 전환 직후 일정 시간(예: 2초)은 unload 보류 — 빠른 왕복(대시) 시 토글 비용 회피.
- **메모리 예산**: 동시 로드 슬롯 수 상한(`MaxConcurrentRegions`, 기본 3~4). 초과 시 LRU(가장 오래 안 들른 리전)부터 unload.

---

## 4. 데이터 모델 (Adjacency Graph)

### 4.1 `WorldRegionSO` (신규 ScriptableObject)

위치: `Assets/10.Datas/World/WorldRegions/`

```csharp
[CreateAssetMenu(fileName = "Region_", menuName = "UPlayGround/World/Region")]
public class WorldRegionSO : ScriptableObject
{
    [Tooltip("SceneContext.MapID 와 동일")]
    public string regionId;

    [Tooltip("Build Settings에 등록된 씬 이름")]
    public string sceneName;

    [Tooltip("이 리전의 직접 이웃들 (양방향이라면 양쪽에 서로 등록)")]
    public List<WorldRegionSO> neighbors;

    [Tooltip("메모리 예산 가중치. 큰 씬일수록 큰 값 → 압박 시 먼저 unload")]
    [Range(1, 10)] public int memoryWeight = 1;

    [Tooltip("재진입 디바운스 시간(초). 빠른 왕복 시 unload 지연")]
    public float unloadDebounceSeconds = 2f;
}
```

### 4.2 `WorldRegionGraphSO` (신규 ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "WorldRegionGraph", menuName = "UPlayGround/World/RegionGraph")]
public class WorldRegionGraphSO : ScriptableObject
{
    public List<WorldRegionSO> regions;

    [Header("스트리밍 정책")]
    [Tooltip("활성 리전 + 동시 로드된 이웃의 최대 수")]
    public int maxConcurrentRegions = 4;

    [Tooltip("Preload할 hop 수. 1 = 직접 이웃까지만")]
    [Range(1, 2)] public int preloadHops = 1;

    public WorldRegionSO FindByRegionId(string regionId);
    public WorldRegionSO FindBySceneName(string sceneName);

    /// <summary>그래프 무결성 검사 (에디터 전용)</summary>
    public bool ValidateGraph(out List<string> errors);
    //  - regionId 중복
    //  - sceneName 미등록 (Build Settings)
    //  - 이웃 관계 비대칭 (A → B 있는데 B → A 없음)
    //  - 그래프 비연결 (도달 불가능 컴포넌트)
}
```

그래프는 단 하나의 `Master` 인스턴스를 만들고, `SceneManager`가 `Resources.Load` 또는 Addressables `LoadAssetAsync`로 부팅 시 가져온다.

### 4.3 그래프 사용 흐름

```
GameManager.Init
  → SceneManager.Init
       → WorldRegionStreamer.Init
            → load WorldRegionGraphSO (Addressable: "WorldRegionGraph")
            → ValidateGraph() (Editor/Development build only)
```

---

## 5. 신규 / 수정 컴포넌트

### 5.1 `SceneManager.WorldStreaming.cs` (신규 partial)

위치: `Assets/02.Scripts/Manager/Scene/SceneManager.WorldStreaming.cs`

```csharp
public partial class SceneManager
{
    private WorldRegionStreamer _streamer;

    public string ActiveRegionId => _streamer?.ActiveRegionId;
    public IReadOnlyCollection<string> LoadedRegionIds => _streamer?.LoadedRegionIds;
    public bool IsRegionActiveSwitch => _streamer?.IsActiveSwitchInProgress ?? false;

    /// <summary>Title→첫 리전 진입. Loading 경유 후 streamer가 인계 받음.</summary>
    public void EnterRegion(string regionId);

    /// <summary>인접 리전으로 활성 전환. RegionBoundaryPortal에서 호출.</summary>
    public void SwitchActiveRegion(string regionId);

    /// <summary>비인접 리전으로의 워프(맵 클릭 등). Loading 경유.</summary>
    public void WarpToRegion(string regionId);

    /// <summary>리전에서 일반 씬으로 (던전, 타이틀 등). 모든 리전 unload + LoadScene.</summary>
    public void LeaveAllRegions(string nextSceneName);

    /// <summary>RegionBoundaryPortal.Preload에서 호출.</summary>
    public void HintPreloadRegion(string regionId);

    /// <summary>RegionBoundaryPortal.UnloadGuard에서 호출.</summary>
    public void HintUnloadFarRegion(string regionId);
}
```

### 5.2 `WorldRegionStreamer` (신규 코어 로직)

위치: `Assets/02.Scripts/Manager/Scene/WorldRegionStreamer.cs`

`SceneManager`의 내부 컴포넌트. 그래프 정책을 모두 여기에 모은다.

핵심 책임:
- **상태 관리**: `ActiveRegionId`, `Loaded`(Set), `LastVisited`(Dictionary<regionId, timestamp> for LRU).
- **연산 큐**: `Queue<Func<UniTask>>` 직렬화.
- **그래프 재계산**: 활성 변경 시 ToPreload/ToUnload 자동 산출.
- **메모리 예산**: 슬롯 초과 시 `LastVisited` 오래된 순 + `memoryWeight` 큰 순으로 unload 선정.
- **디바운스**: unload는 `unloadDebounceSeconds` 만큼 지연. 그 사이 다시 이웃이 되면 취소.
- **씬별 액터 인덱스**: 4.4 참고.

```csharp
internal class WorldRegionStreamer
{
    public string ActiveRegionId { get; private set; }
    public IReadOnlyCollection<string> LoadedRegionIds => _loaded;
    public bool IsActiveSwitchInProgress { get; private set; }
    public event Action<string> OnActiveRegionChanged;

    private readonly HashSet<string> _loaded = new();
    private readonly Dictionary<string, float> _lastVisited = new();
    private readonly Dictionary<string, CancellationTokenSource> _pendingUnloads = new();
    private readonly Queue<Func<UniTask>> _opQueue = new();
    private bool _isProcessingQueue;

    public void Init(WorldRegionGraphSO graph);
    public UniTask EnterRegionAsync(string regionId);    // single-load 새 시작
    public UniTask SwitchActiveAsync(string regionId);   // 인접 전환
    public UniTask WarpAsync(string regionId);           // 비인접 (전체 unload 후 single-load)
    public UniTask LeaveAllAsync();                      // 모든 리전 unload

    public void HintPreload(string regionId);            // 큐에 추가, 이미 로드/큐잉 중이면 무시
    public void HintUnloadFar(string regionId);          // 즉시 unload 큐잉 (이웃이면 무시)

    private UniTask LoadAdditiveAsync(WorldRegionSO r);
    private UniTask UnloadAsync(string regionId, bool immediate = false);
    private void   RecomputeNeighborhood();              // active 변경 직후 호출
    private void   ApplyMemoryBudget();                  // LRU 평가
}
```

### 5.3 `RegionBoundaryPortal` (신규 컴포넌트)

위치: `Assets/02.Scripts/Scene/RegionBoundaryPortal.cs`

기존 `PortalActor`(풀 교체 + 동일 씬 워프)와 **분리**한다 — 관심사 분리.

```csharp
public enum RegionBoundaryRole
{
    Preload,      // 인접 씬을 백그라운드 로드만 함
    Activate,     // 활성 씬 전환 (플레이어가 이미 새 영역의 지오메트리 위에 있어야 함)
    UnloadGuard   // 충분히 멀어졌음 → 떠나는 리전 unload 힌트
}

[RequireComponent(typeof(Collider))]
public class RegionBoundaryPortal : MonoBehaviour
{
    [SerializeField] private RegionBoundaryRole _role;

    [Tooltip("Preload/Activate일 때: 들어가는 리전. UnloadGuard일 때: 떠나는 리전.")]
    [SerializeField] private string _regionId;

    [SerializeField] private bool _onlyOnce = false;

    private bool _consumed;
    // OnTriggerEnter → Player 검사 → 역할별 SceneManager.Hint*/SwitchActiveRegion
}
```

### 5.4 `SceneContext` 확장

```csharp
public class SceneContext : MonoBehaviour
{
    public string SceneType;
    public string MapID;

    [Header("월드 리전 (선택)")]
    [Tooltip("이 씬이 WorldRegionGraph에 등록된 리전이라면 true")]
    public bool   IsWorldRegion;
    [Tooltip("MapID와 같은 값. 일관성을 위해 분리 보관 (편집 편의)")]
    public string RegionId;
}
```

`SceneContext.Start()` 마지막에:
```csharp
if (IsWorldRegion)
    SceneManager.Instance.OnRegionSceneLoaded(this);
```

### 5.5 `ActorSpawnManager` 보완 (씬별 인덱스화)

현재 `OnSceneChanged`는 풀 교체 가정으로 전체 클리어. N개 리전에서는 **언로드되는 리전의 액터만 솎아내야** 한다.

수정:
```csharp
private readonly Dictionary<int, SpawnedActorInfo> _spawnedActors = new();

public void OnSceneChanged(string sceneType)
{
    // 활성 리전 전환은 객체 보존 → 클리어 금지
    if (SceneManager.Instance.IsRegionActiveSwitch) return;
    _spawnedActors.Clear();
}

/// <summary>SceneManager가 특정 씬 unload 직전에 호출.</summary>
public void OnRegionUnloading(Scene unloaded)
{
    _cleanupBuffer.Clear();
    foreach (var kv in _spawnedActors)
    {
        var actor = kv.Value.Actor;
        if (actor != null && actor.gameObject.scene == unloaded)
            _cleanupBuffer.Add(kv.Key);
    }
    foreach (var id in _cleanupBuffer) _spawnedActors.Remove(id);
}
```

`GameObjectManager`도 같은 패턴으로 등록된 `_allActors`에서 unload 씬 소속만 제거.

### 5.6 활성 씬 동행 객체 (Player & Camera)

활성 리전 전환 시 **이전 리전이 unload되면 그 씬에 속한 객체는 함께 파괴**된다. 그래서:
- Player는 첫 진입(EnterRegion) 시 활성 씬에 스폰되고, **활성 전환 직후마다 새 활성 씬으로 `MoveGameObjectToScene`** 으로 옮긴다.
- 카메라 리그도 동일하게 동행시키거나, 처음부터 활성 씬 외부(매니저 GameObject 하위)에 둔다. 본 프로젝트는 후자(`CameraManager`가 매니저 산하).

`WorldRegionStreamer.SwitchActiveAsync` 의 마지막 단계:
```csharp
var player = GameObjectManager.Instance.Player;
if (player != null)
    UnitySceneMgr.MoveGameObjectToScene(player.gameObject, newActiveScene);
foreach (var go in _companionRoots)  // SO로 외부화 가능
    UnitySceneMgr.MoveGameObjectToScene(go, newActiveScene);
```

### 5.7 `Scene.cs` (Enum) 확장

```csharp
public static class SceneName
{
    // 기존...
    // 리전 씬은 enum 상수보다 WorldRegionSO.sceneName 으로 다루지만,
    // 코드에서 직접 LoadScene 호출하는 곳이 있으면 상수로 등록.
}
```

리전 씬 이름은 가능하면 코드 상수가 아닌 `WorldRegionSO`에서만 다룬다 — 그래프 추가/제거가 SO 편집만으로 가능하도록.

---

## 6. 운영 정책 (수치 가이드)

| 정책 | 기본값 | 사유 |
|-----|--------|------|
| `maxConcurrentRegions` | 4 | 활성 1 + 이웃 최대 3까지. 허브 리전(이웃 4+)에서는 LRU로 일부 비활성화 |
| `preloadHops` | 1 | 1-hop이면 충분. 2-hop은 메모리 비용 과다 |
| `unloadDebounceSeconds` | 2.0 | 대시/달리기 왕복(평균 1.5초) 흡수 |
| Preload Trigger 거리 | 30~80m | Activate 도달 전 LoadAsync 완료 가능한 거리. 씬 크기에 따라 조정 |
| UnloadGuard 거리 | Activate에서 60~120m | 시야가 가려진 후. 카메라 시야 밖이 보장되는 지점 |
| 큐 직렬화 | 항상 1개 | Load/Unload 동시 실행 금지. 프레임 안정성 |

---

## 7. Persistent 씬을 도입하지 않는 이유 (의도된 결정)

웹 자료 다수가 "Persistent 매니저 씬"을 권장하지만 본 프로젝트에는 도입하지 않는다.

근거:
1. `BaseManager<T>` 싱글톤이 Awake 시 자기 자신을 보장하고 씬 전환 동안 살아남는다 — 이미 동일 효과 달성.
2. `SceneContext.EnsureGameManagerInitialized()` 가 단독 씬 실행에서도 매니저 풀을 자동 부팅한다 — Persistent 씬을 분리하면 단독 실행성을 깨뜨려 테스트 씬 워크플로가 망가진다.
3. Persistent 씬 추가 = Build Settings, BootLoader 흐름, 모든 테스트 씬의 멀티씬 셋업 손봐야 함. 이득 대비 비용 과다.

→ "Persistent 씬을 굳이 만들지 않은 채로도 같은 효과를 내고 있다"는 점은 본 프로젝트 아키텍처의 강점이며, N-리전 도입 후에도 유지한다.

---

## 8. 멀티 씬 에디팅 워크플로

### 8.1 에디터 도구 (`UPlayGround/World` 메뉴)

| 메뉴 | 동작 |
|------|------|
| `Open Region (and Neighbors)` | 선택된 `WorldRegionSO`의 씬 + 이웃 씬을 멀티씬 에디팅으로 동시 오픈 |
| `Validate Region Graph` | `WorldRegionGraphSO.ValidateGraph` 호출. 비대칭/누락/미등록 씬 콘솔 보고 |
| `Visualize Graph` | EditorWindow에서 노드/에지 그래프 표시 (GraphView 또는 GUI 단순 렌더) |

1인 개발이므로 우선 `Open Region (and Neighbors)` 만 구현, 나머지는 그래프가 5개 이상으로 늘어나면 추가.

### 8.2 라이팅 / 스카이박스

- 각 리전은 자기 라이팅 세팅 보유. 활성 씬 라이팅이 적용됨 (`SetActiveScene` 효과).
- 라이트 베이크는 리전별로 따로. 멀티씬 혼합 베이크 비권장.

### 8.3 Build Settings

- 모든 `WorldRegionSO.sceneName` 이 빌드 씬에 등록되어 있어야 한다.
- `ValidateGraph` 가 빌드 씬 등록 여부도 검사.

---

## 9. 단계별 구현 로드맵

> 각 단계는 별도 PR/커밋. 한 단계 완료 후 게임이 정상 부팅되어야 함.

### Step 1. 데이터 모델 (`WorldRegionSO`, `WorldRegionGraphSO`)
- ScriptableObject 2종 + `ValidateGraph`.
- 빈 그래프 자산 1개 생성 (`Assets/10.Datas/World/Master_RegionGraph.asset`).
- 영향: 코드만 추가, 런타임 영향 없음.

### Step 2. `WorldRegionStreamer` 골격
- 큐/상태/그래프 재계산만 구현. 실제 LoadAsync 대신 로그.
- 콘솔 명령으로 EnterRegion / SwitchActive / WarpTo / LeaveAll 시퀀스 검증.

### Step 3. SceneManager 통합
- `SceneManager.WorldStreaming.cs` partial로 streamer 호스팅.
- `ActorSpawnManager.OnSceneChanged` 가드 + `OnRegionUnloading` 추가.
- `SceneContext.IsWorldRegion` / `RegionId` 필드 추가.

### Step 4. 첫 두 리전 (`World_Field`, `World_Town`) + RegionBoundaryPortal
- 1×1km 평면 + 경계 다리 + Preload/Activate/UnloadGuard 3종 대칭 배치.
- 그래프에 두 리전 등록 + 이웃 관계 양방향.
- Title에서 `EnterRegion(World_Field)` 진입 가능.
- **수동 플레이 검증**: Field ↔ Town 왕복, 활성 전환 시 라이팅 변화, 빠른 왕복 시 큐가 안 깨지는지, FPS 스파이크 모니터.

### Step 5. 세 번째 리전 추가로 N-일반화 검증
- `World_Cave` 추가, Town의 이웃으로 등록 (A-B-C 일렬).
- A에 있을 때 C가 unload, B에서 양쪽 preload, C에서 A unload 동작 확인.
- 메모리 예산 초과 케이스 (강제로 maxConcurrentRegions=2로) 검증.

### Step 6. 비인접 워프 (`WarpToRegion`) + 던전 ↔ 리전 왕복
- 맵 UI에서 다른 리전 클릭 → Loading 경유 워프.
- 던전 진입 시 `LeaveAllRegions(InGame_Dungeon)`.
- 던전 종료 시 SaveData의 `lastRegionId`로 `EnterRegion`.

### Step 7. 에디터 편의 도구
- `Open Region (and Neighbors)` 메뉴.
- 잦아지면 그래프 시각화 윈도우 추가.

---

## 10. 리스크 / 엣지 케이스

| 리스크 | 영향 | 완화 |
|--------|------|------|
| 빠른 왕복 (대시) | Load/Unload 큐 적체, 메모리 진동 | UnloadGuard 디바운스(2초) + 디바운스 중 다시 이웃이 되면 취소 (CTS cancel) |
| Activate 시 Preload 미완 | 한 프레임 검은 화면 / 라이팅 깨짐 | Preload Trigger를 충분히 앞에 배치 + Activate 큐가 Preload 완료 await |
| 두 리전 모두 NavMesh 사용 | 활성 전환 시 NavMesh 재계산 비용 | `NavMeshSurface` 컴포넌트로 씬 단위 분리. unload 시 자동 해제 |
| 라이트/리플렉션 프로브 경계 | 활성 전환 순간 색감 점프 | 경계를 터널/실내로 만들어 시각적으로 가림 |
| Player가 unload되는 씬에 소속 | 게임 즉사 | SwitchActive 마지막 단계에서 `MoveGameObjectToScene`로 동행 |
| Addressables 큐 블로킹 | 다른 어드레서블 로드 동반 정지 | `activateOnLoad: false` 사용 금지. 빌트인 `SceneManager.LoadSceneAsync`만 사용 |
| `BaseManager<T>.OnSceneChanged` 가 풀 교체 가정 | 활성 전환 시 매니저 상태 잘못 리셋 | `SceneManager.IsRegionActiveSwitch` 플래그 모든 매니저가 분기 처리 |
| 그래프 비대칭 (A→B만 등록) | 한쪽 방향만 preload되어 반대쪽은 끊김 | `ValidateGraph` 가 비대칭 검출. 빌드 시 실패시킴 |
| 허브 리전(이웃 5+)에서 메모리 폭주 | OOM / GC 폭주 | `maxConcurrentRegions` LRU eviction. 허브 리전 자체를 의도적으로 작게 만듦 |
| 동시 원거리 워프 + 인접 전환 충돌 | 큐 꼬임 | `IsActiveSwitchInProgress` 동안 `WarpToRegion`/`LeaveAllRegions` 거부 또는 큐 후미 |
| 에디터 멀티씬 편집에서 잘못된 활성 씬 | 라이팅이 다른 씬 기준으로 베이크됨 | 에디터 메뉴가 항상 메인 리전을 활성으로 설정 |

---

## 11. 변경 영향 표

| 파일/모듈 | 변경 |
|-----------|------|
| `Manager/Scene/WorldRegionStreamer.cs` | **신규** |
| `Manager/Scene/SceneManager.WorldStreaming.cs` | **신규** (partial) |
| `Manager/Scene/SceneContext.cs` | `IsWorldRegion`, `RegionId` 필드 |
| `Manager/SceneManager.cs` | `OnRegionSceneLoaded` 진입점 + `IsRegionActiveSwitch` 노출 |
| `Manager/Actor/ActorSpawnManager.cs` | `OnSceneChanged` 가드 + `OnRegionUnloading(Scene)` 추가 |
| `Manager/Object/GameObjectManager.cs` | `OnRegionUnloading(Scene)` (씬별 액터 솎기) |
| `Scene/RegionBoundaryPortal.cs` | **신규** |
| `Scene/PortalActor.cs` | 변경 없음 (관심사 분리 유지) |
| `Data/World/WorldRegionSO.cs` | **신규** |
| `Data/World/WorldRegionGraphSO.cs` | **신규** |
| `10.Datas/World/Master_RegionGraph.asset` | **신규 자산** |
| `01.Scenes/GameLogic/World_*.unity` | 신규 N개 |
| `Tool/Editor/World/RegionGraphMenu.cs` | **신규 (에디터)** Open Region + Neighbors |
| Build Settings | 모든 리전 씬 등록 |
| 다른 매니저(`CameraManager`, `UIManager`, `PartyManager`, `StoryManager` 등) | `OnSceneChanged`에서 `IsRegionActiveSwitch` 분기 점검 (별도 체크리스트) |

---

## 12. 후속 작업 (이 설계의 범위 밖)

- **2-hop preload 옵션**: 더 큰 그래프에서 체감 끊김이 있을 때 활성화. 메모리 비용 큼.
- **Addressables 원격 패치**: 리전 씬을 어드레서블화하면 콘텐츠 패치 가능. `activateOnLoad: false` 큐 블로킹 회피책 필요.
- **리전 내부 LOD/Cell 분할**: 한 리전이 너무 커지면 그 안을 다시 청크로 쪼개는 작업. 본 설계의 자연스러운 후속.
- **컷씬/스토리 트리거와 결합**: `StoryManager`가 `SwitchActiveRegion` 또는 `WarpToRegion` 트리거.
- **세이브 데이터**: `currentRegionId` + `currentTransform` + `loadedAuxRegions`(선택). 이어하기 시 `EnterRegion(currentRegionId)` 후 위치 복원.
- **그래프 자동 검증 CI**: `ValidateGraph` 를 Editor 빌드 후크에 연결.

---

## 참고 자료

- [Unity Manual: Multi-Scene editing](https://docs.unity3d.com/Manual/MultiSceneEditing.html)
- [Unity Manual: Occlusion culling and Scene loading](https://docs.unity3d.com/Manual/occlusion-culling-scene-loading.html)
- [Unity Addressables 2.0: Load a scene](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/LoadingScenes.html)
- [80.lv — Smooth Scene Streaming with Unity3D](https://80.lv/articles/smooth-scene-streaming-with-unity3d)
- [outscal — Managing Multiple Scenes in Unity (Seamless Open World)](https://outscal.com/blog/unity-scene-management-guide)
- [outscal — A Complete Guide to Unity's Scene Management](https://outscal.com/blog/unity-scene-management-complete-guide)
- [Unity Discussions — Seamless scene transition](https://discussions.unity.com/t/seamless-scene-transition/800018)
- [Unity Discussions — Open World Map Workflow](https://discussions.unity.com/t/open-world-map-creating-chunking-streaming-workflow/848971)
- [Unity Blog — Achieve better Scene workflow with ScriptableObjects](https://blog.unity.com/engine-platform/achieve-better-scene-workflow-with-scriptableobjects)
- [Catlike Coding — Multiple Scenes](https://catlikecoding.com/unity/tutorials/object-management/multiple-scenes/)
- [GitHub — tiredamage42/OpenWorldFramework](https://github.com/tiredamage42/OpenWorldFramework)
