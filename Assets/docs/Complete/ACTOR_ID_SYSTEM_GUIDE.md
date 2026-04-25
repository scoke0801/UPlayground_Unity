# Actor ID 시스템 가이드

## 개요

Actor ID 시스템은 씬에 존재하는 모든 GameActor에 **고유 문자열 ID**를 부여하고, 이를 기반으로 스탯 데이터 관리, 런타임 스폰, 그룹 지정, 상태 모니터링을 통합적으로 수행하는 데이터 중심 시스템입니다.

### 핵심 특징

- **ScriptableObject 기반**: `ActorDefinitionSO`에 ID·프리팹·스탯을 묶어 코드 변경 없이 밸런싱 가능
- **런타임 스폰**: `ActorSpawnManager.SpawnActor(actorId, ...)` 한 줄로 스폰 + 스탯 자동 적용
- **그룹 연동**: 스폰 시 `MonsterGroupController` 지정으로 즉시 그룹 AI에 편입
- **커스텀 에디터 2종**: 데이터 관리 창 + 런타임 상태 모니터 창

---

## 아키텍처

```
┌─────────────────────────────────────────────────────┐
│                   GameManager (싱글톤)                │
└───────────────────────┬─────────────────────────────┘
                        │ 등록
               ┌────────▼────────┐
               │ ActorSpawnManager│  ← ActorDatabase SO 참조
               └────────┬────────┘
                        │ SpawnActor(actorId, ...)
            ┌───────────▼────────────┐
            │    ActorDefinitionSO   │  (ID + 프리팹 + 스탯)
            └───────────┬────────────┘
                        │ Instantiate → SetDefinition()
            ┌───────────▼────────────┐
            │       GameActor        │  ← ActorId 프로퍼티
            │  (MonsterActor 등)      │  ← Definition 프로퍼티
            └────────────────────────┘
```

### 파일 구조

```
Assets/02.Scripts/
├── Data/Actor/
│   ├── ActorDefinitionSO.cs        # Actor 한 종류의 정의 SO
│   └── ActorDatabase.cs            # 전체 정의를 관리하는 Database SO
│
├── Manager/Actor/
│   └── ActorSpawnManager.cs        # 런타임 스폰 & 추적 매니저
│
└── Tool/Editor/Actor/
    ├── ActorDatabaseEditorWindow.cs # 데이터 관리 에디터 창
    └── ActorRuntimeMonitorWindow.cs # 런타임 상태 모니터 에디터 창

Assets/10.Datas/Actor/              # ScriptableObject 에셋 저장 위치
├── ActorDatabase.asset
├── ActorDef_EnemySword.asset
└── ActorDef_EnemyArcher.asset
```

---

## 핵심 클래스

### ActorDefinitionSO

Actor 한 종류를 정의하는 ScriptableObject.

| 필드 | 타입 | 설명 |
|------|------|------|
| `actorId` | `string` | 런타임 고유 ID (중복 불가) |
| `displayName` | `string` | 에디터/UI 표시용 이름 |
| `description` | `string` | 설명 (TextArea) |
| `actorType` | `ActorType` | Flags enum (Monster, Combat 등) |
| `characterType` | `CharacterActorType` | 캐릭터 세부 타입 |
| `prefab` | `GameObject` | 스폰에 사용할 프리팹 (GameActor 필수) |
| `stats` | `EnemyStatsSO` | 스탯 SO (null이면 프리팹 기본값 사용) |
| `poiseData` | `PoiseSO` | Poise SO (null이면 프리팹 기본값 사용) |

> `actorId`를 비워두면 에셋 파일명으로 자동 설정됩니다 (`OnValidate`).

### ActorDatabase

모든 `ActorDefinitionSO`를 `actorId` 키로 관리하는 Database ScriptableObject.

```csharp
// 조회
ActorDefinitionSO def = database.GetDefinition("enemy_sword");

// 포함 여부
bool exists = database.Contains("enemy_archer");

// 전체 목록 (읽기 전용)
IReadOnlyList<ActorDefinitionSO> all = database.All;
```

### ActorSpawnManager

`BaseManager<ActorSpawnManager>`를 상속하는 싱글톤 매니저.

```csharp
// 기본 스폰
GameActor actor = ActorSpawnManager.Instance.SpawnActor(
    "enemy_sword",
    transform.position,
    Quaternion.identity
);

// 그룹 지정 스폰
GameActor actor = ActorSpawnManager.Instance.SpawnActor(
    "enemy_sword",
    spawnPoint.position,
    Quaternion.identity,
    group: myGroupController
);

// 부모 Transform 지정 스폰
GameActor actor = ActorSpawnManager.Instance.SpawnActor(
    "enemy_archer",
    Vector3.zero,
    Quaternion.identity,
    group: null,
    parent: dungeonRoot
);
```

### GameActor (확장)

모든 GameActor에 다음이 추가되었습니다.

```csharp
// 런타임 고유 ID 조회
string id = actor.ActorId;

// 정의 SO 조회
ActorDefinitionSO def = actor.Definition;

// 정의 주입 (ActorSpawnManager가 스폰 직후 자동 호출)
actor.SetDefinition(definition);
```

씬에 미리 배치된 Actor는 Inspector의 `Actor Identity > Actor Id` 필드에서 직접 ID를 설정합니다.

---

## 셋업 방법

### 1단계: ActorDatabase 에셋 생성

1. 메뉴 `UPlayGround/Actor/Actor Database Editor` 열기
2. 툴바 `새 Database 생성` 클릭
3. `Assets/10.Datas/Actor/ActorDatabase.asset`으로 저장

### 2단계: ActorDefinitionSO 등록

같은 창에서:

1. `새 Actor 추가` 클릭 → 저장 경로 지정
2. 우측 패널에서 `actorId`, `displayName`, `prefab`, `stats` 입력
3. 저장은 자동 (SerializedObject 기반)

> actorId는 중복 시 경고가 출력되며 런타임 조회에서 제외됩니다.

### 3단계: ActorSpawnManager에 Database 연결

씬의 `ActorSpawnManager (Singleton)` GameObject를 Inspector에서 열고 `_database` 필드에 생성한 `ActorDatabase.asset`을 드래그합니다.

> DontDestroyOnLoad 싱글톤이므로 게임 시작 씬에 한 번만 배치합니다.

### 4단계: 씬 배치 Actor에 ID 부여 (선택)

스폰 없이 씬에 미리 배치된 Actor는 Inspector에서 `Actor Identity > Actor Id` 필드에 actorId를 직접 입력합니다. 런타임 모니터에서 필터링 및 식별에 활용됩니다.

---

## 런타임 스폰 예시

### 기본 스폰

```csharp
using UPlayGround.Manager;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private string _actorId = "enemy_sword";

    private void Start()
    {
        ActorSpawnManager.Instance.SpawnActor(
            _actorId,
            transform.position,
            transform.rotation
        );
    }
}
```

### 그룹 지정 스폰

```csharp
using UPlayGround.Manager;
using UPlayGround.Group;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private MonsterGroupController _group;
    [SerializeField] private string[] _actorIds;
    [SerializeField] private Transform[] _spawnPoints;

    public void SpawnWave()
    {
        for (int i = 0; i < _actorIds.Length; i++)
        {
            ActorSpawnManager.Instance.SpawnActor(
                _actorIds[i],
                _spawnPoints[i].position,
                _spawnPoints[i].rotation,
                group: _group
            );
        }
    }
}
```

### 스폰된 Actor 조회

```csharp
// 특정 actorId로 스폰된 Actor 목록
List<GameActor> swords = ActorSpawnManager.Instance.GetSpawnedActors("enemy_sword");

// 전체 스폰 Actor 목록
List<GameActor> all = ActorSpawnManager.Instance.GetAllSpawnedActors();

// 스폰 정보 맵 (시간, 위치, 그룹 포함)
IReadOnlyDictionary<int, ActorSpawnManager.SpawnedActorInfo> map
    = ActorSpawnManager.Instance.SpawnedActors;
```

---

## 스탯 적용 우선순위

| 상황 | 사용되는 스탯 |
|------|--------------|
| 씬에 배치된 Actor (스폰 없음) | 프리팹 Inspector 설정값 |
| `SpawnActor` 호출, `definition.stats == null` | 프리팹 Inspector 설정값 |
| `SpawnActor` 호출, `definition.stats != null` | **ActorDefinitionSO의 stats** |

`MonsterActor.SetDefinition()`이 호출되면 `_stats`, `_maxHealth`, `_currentHealth`가 덮어씌워지며 `OnHealthChanged` 이벤트가 발화됩니다.

---

## 커스텀 에디터

### Actor Database Editor

메뉴: `UPlayGround/Actor/Actor Database Editor`

| 영역 | 기능 |
|------|------|
| 툴바 | Database SO 연결/생성, Actor 추가 |
| 좌측 목록 | actorId·이름 검색, 선택, 삭제 |
| 우측 상세 | SerializedObject 기반 인스펙터 (전체 필드 편집) |
| Inspector 열기 버튼 | 선택된 SO를 Project 창에서 선택 |

![Actor Database Editor](../_placeholder_screenshot_db_editor)

**검색:** 이름 또는 actorId 부분 일치 (대소문자 무시)

**삭제 동작:** Database 목록에서 제거만 하며, 에셋 파일은 삭제되지 않습니다.

---

### Actor Runtime Monitor

메뉴: `UPlayGround/Actor/Actor Runtime Monitor`

Play 모드에서만 의미 있는 데이터를 표시합니다.

| 컬럼 | 내용 |
|------|------|
| ActorID | actorId (없으면 "(없음)") |
| 이름 | GameObject 이름 (클릭 시 씬에서 선택·포커스) |
| 타입 | ActorType 플래그 문자열 |
| HP | 체력 바 + 수치 (MonsterActor만, 그 외 N/A) |
| 현재 상태 | 상태머신 CurrentState.StateName |
| 그룹 | 소속 MonsterGroupController 이름 |
| 스폰 경과 | 스폰 후 경과 시간(초). 배치된 Actor는 "-" |

**필터**

| 필터 | 동작 |
|------|------|
| ActorID 검색 | 부분 일치 (대소문자 무시) |
| 타입 필터 | Flags 조합 가능 (Monster + Combat 등) |
| 스폰된 것만 | ActorSpawnManager를 통해 스폰된 Actor만 표시 |

**자동 갱신:** 0.25초 인터벌로 자동 새로 고침 (토글 가능)

---

## 주의 사항

### actorId 중복 방지

`actorId`는 Database 전체에서 유일해야 합니다. 중복 시 두 번째 항목은 런타임 조회에서 무시되며 경고가 출력됩니다.

```
[ActorDatabase] 중복된 actorId: 'enemy_sword' (ActorDef_EnemySword2)
```

### 프리팹 필수 조건

`definition.prefab`에는 반드시 `GameActor`(또는 하위 클래스) 컴포넌트가 있어야 합니다. 없으면 스폰을 중단하고 오브젝트를 즉시 파괴합니다.

### MonsterGroupController.Start() 타이밍

`MonsterGroupController.Start()`는 자식 오브젝트의 MonsterActor를 자동 수집합니다. 런타임 스폰으로 추가된 Monster는 `SpawnActor`의 `group` 파라미터로 명시 지정해야 그룹에 편입됩니다.

### 씬 전환 시 스폰 기록 초기화

`ActorSpawnManager.OnSceneChanged()`가 호출되면 `_spawnedActors` 딕셔너리가 초기화됩니다. 실제 오브젝트는 Unity가 씬 언로드 시 정리합니다.

---

## 확장 포인트

### 다른 Actor 타입에 스탯 적용

`SetDefinition`을 오버라이드하면 됩니다. `MonsterActor` 예시:

```csharp
public override void SetDefinition(ActorDefinitionSO definition)
{
    base.SetDefinition(definition); // ActorId 설정

    if (definition?.stats != null)
    {
        _stats = definition.stats;
        _maxHealth = _stats.maxHealth;
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    if (definition?.poiseData != null && _poiseStat != null)
        _poiseStat.Init(definition.poiseData);
}
```

### ActorDefinitionSO 필드 추가

SO를 상속하거나 필드를 추가해 프로젝트 요구에 맞게 확장할 수 있습니다.

```csharp
// 예: 드롭 테이블, 대화 데이터 등 추가
[Header("드롭")]
public ItemDropList dropList;
```

### 스폰 후 콜백

`SpawnActor`는 `GameActor`를 반환하므로 즉시 추가 처리가 가능합니다.

```csharp
var actor = ActorSpawnManager.Instance.SpawnActor("enemy_boss", pos, rot);
if (actor is MonsterActor boss)
{
    boss.SetInvincible(true);
    StartCoroutine(BossIntroSequence(boss));
}
```
