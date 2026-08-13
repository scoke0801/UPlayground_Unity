# World Spawn & Encounter 구현 스펙

## 1. 목표

**고정된 플레이어 시작점**에서 출발해, 수작업 검증된 N개 외곽 후보에서 보스 위치를 결정하고 중앙 보스를 별도 아레나에 준비한다. 외곽 위치는 작은 회색 신호로 안내하고, 중앙 위치는 외곽 보스 세 명 처치 후 큰 붉은 신호로 공개한다. 실제 보스의 정체는 조우 전까지 숨긴다.

> 2026-08-02 개정: 플레이어 시작 지점 추첨을 제거했다. 시작점은 설정이 지정한 단일 `spawnId`이며 시드의 영향을 받지 않는다. 상세는 6.1절.

---

## 2. 기존 코드 접점

| 기존 타입 | 활용 |
|---|---|
| `ActorSpawnManager.SpawnActor` | 결정된 Actor ID를 위치·회전에 생성 |
| `ActorDatabase` | 보스 Actor ID와 프리팹 검증 |
| `MonsterActor.ApplyRuntimeLevel` | 사이클 난이도에 맞춘 런타임 레벨·스탯 적용 |
| `MonsterActor.SetRuntimeRewards` | 사이클 보상 배율 적용 |
| `MinimapMarkerRegistry` | 씬 정적 마커 등록 구조 참고 |
| `MinimapMarkerRegistrar` | 위치 마커의 등록/해제 생명주기 참고 |
| `MinimapEntityIcon.SetEntry` | `?`에서 발견 아이콘으로 외관 변경 |
| `MinimapIconConfigSO` | `unknownBoss`, `discoveredBoss`, `remains` 아이콘 필드 확장 |
| `PortalActor.SetPortalActive` | 중앙 보스 처치 전 탈출 포털 비활성화 |

현재 `MinimapMarkerRegistrar`는 직렬화된 정적 마커이며 런타임 타입 변경 API가 없다. 보스 마커를 이 컴포넌트에 억지로 넣지 않고 사이클 전용 런타임 마커 모델을 추가한다.

---

## 3. 씬 저작 컴포넌트

### `CycleSpawnPoint`

```csharp
public sealed class CycleSpawnPoint : MonoBehaviour
{
    [SerializeField] private string _spawnId;
    [SerializeField] private CycleSpawnRole _allowedRoles;
    [SerializeField] private string _sectorId;
    [SerializeField] private float _safetyRadius;
    [SerializeField] private Transform _arrivalPoint;
}
```

| 필드 | 규칙 |
|---|---|
| `spawnId` | 맵 안에서 영구적으로 유일. 저장 키이므로 이름 변경 금지 |
| `allowedRoles` | `Player`, `OuterBoss`, `Respawn` 플래그 |
| `sectorId` | 동일 섹터 중복 보스 제한과 텔레메트리용 |
| `safetyRadius` | 플레이어 시작점 주변 보스 배치 금지 반경 |
| `arrivalPoint` | KCC 배치 위치·회전. 없으면 컴포넌트 Transform 사용 |

### `CentralBossSpawnPoint`

중앙 아레나 전용 단일 컴포넌트다. 씬에 0개 또는 2개 이상이면 검증 오류로 처리한다.

### 에디터 검증

- `spawnId` 공백·중복
- 지면에서 과도하게 뜨거나 파묻힌 위치
- `fixedPlayerSpawnId` 미지정, 또는 해당 후보가 씬에 없거나 `Player` 역할이 아님
- 외곽 보스 역할 후보 수 부족
- 중앙 아레나 후보 누락
- `ActorDatabase`에 없는 보스 ID
- 미니맵 캡처 범위를 벗어난 후보

P0에서는 별도 에디터 창보다 `OnDrawGizmos`와 메뉴 검증 명령을 우선한다.

---

## 4. 데이터와 결과 모델

### `CycleWorldConfigSO`

```csharp
public sealed class CycleWorldConfigSO : ScriptableObject
{
    public string mapId;

    // 고정 플레이어 시작점. 값이 있으면 추첨하지 않고 이 spawnId를 그대로 사용한다.
    public string fixedPlayerSpawnId;

    public List<string> outerBossActorIds;
    public List<string> centralBossActorIds;
    public int outerBossCount = 3;
    public int maxSameSectorBossCount = 1;
}
```

`fixedPlayerSpawnId`는 P0에서 **필수**다. 비어 있으면 레이아웃 생성을 실패시키고, 조용히 추첨으로 폴백하지 않는다. 씬에 `Player` 역할 후보가 여러 개 저작되어 있어도 지정되지 않은 후보는 **미집행 데이터**로 남을 뿐 런타임 결과에 영향을 주지 않는다. 시작점 랜덤화를 다시 도입할 경우를 대비해 후보 저작 구조 자체는 유지한다.

### `CycleLayoutState`

```csharp
[Serializable]
public sealed class CycleLayoutState
{
    public string playerSpawnId;
    public List<CycleBossPlacement> outerBosses;
    public CycleBossPlacement centralBoss;
    public List<string> activeRespawnPointIds;
}

[Serializable]
public sealed class CycleBossPlacement
{
    public string spawnId;
    public string actorId;
    public bool isCentral;
    public bool discovered;
    public bool defeated;
}
```

위치 좌표 대신 안정적인 `spawnId`를 저장한다. 씬 저작 변경으로 ID를 찾지 못하면 로드를 중단하고 새 레이아웃을 조용히 생성하지 않는다.

---

## 5. `CycleWorldSpawnService`

사이클 시드와 월드 설정으로 시작 지점, 외곽 보스, 중앙 보스, 부활 지점을 확정하는 런타임 서비스다. 씬 오브젝트 참조가 아닌 재현 가능한 ID 기반 `CycleLayoutState`를 반환한다.

```csharp
public sealed class CycleWorldSpawnService
{
    public CycleLayoutState BuildLayout(
        CycleWorldConfigSO config,
        int cycleIndex,
        int cycleSeed);

    public void SpawnLayout(CycleLayoutState layout);
    public void RestoreLayout(CycleLayoutState layout);
}
```

- `BuildLayout`: 결정적 난수 스트림으로 배치만 계산한다.
- `SpawnLayout`: 신규 사이클의 배치를 `ActorSpawnManager`로 생성한다.
- `RestoreLayout`: 저장된 생존·처치·발견 상태를 적용해 중단 지점을 복구한다.
- `CycleRunManager`만 이 서비스를 호출하며 UI는 결과 상태를 읽기만 한다.

---

## 6. 배치 알고리즘

### 6.1 플레이어 시작점 — 고정

```text
1. config.fixedPlayerSpawnId를 읽는다.
2. 값이 비었으면 즉시 레이아웃 생성 실패.
3. 해당 spawnId를 가진 CycleSpawnPoint를 찾는다. 없으면 실패.
4. 그 후보의 allowedRoles에 Player가 없으면 실패.
5. layout.playerSpawnId에 확정한다.
```

- 이 단계는 **RNG를 전혀 소비하지 않는다**. Layout RNG 스트림은 보스 위치 선택에서 처음 사용된다.
- 시드를 바꿔도 시작점은 동일하다. 시작 위치는 시드의 함수가 아니라 설정의 함수다.
- 씬에 남아 있는 다른 `Player` 역할 후보는 검증 경고 대상이 아니다. 미집행 데이터로 허용한다.
- 시작점이 고정되므로 시작 지점 주변 지형·조우 밀도·초반 동선은 **레벨 디자인으로 확정 저작**할 수 있다. 이 전제를 활용하는 것이 고정의 목적이다.

### 6.2 보스와 부활 지점 — 시드 기반 유지

```text
1. 확정된 플레이어 시작점의 safetyRadius 안 후보와 같은 spawnId를 보스 후보에서 제외한다.
2. 남은 OuterBoss 후보를 spawnId로 정렬한다.
3. 섹터 중복 제한을 적용해 outerBossCount개 위치를 선택한다.
4. BossPool RNG로 외곽 보스 Actor ID를 선택한다.
5. 중앙 풀에서 중앙 보스 Actor ID 1개를 선택한다.
6. Respawn 역할 후보에서 활성 지점을 선택한다.
7. 결과를 CycleLayoutState에 저장한 뒤 실제 Actor를 생성한다.
```

- 입력 리스트 정렬 없이 RNG 인덱스를 사용하면 씬 탐색 순서에 따라 결과가 바뀌므로 반드시 안정 정렬한다.
- 무한 재추첨을 금지한다. 조건을 만족하는 후보 목록을 먼저 만든 뒤 한 번 선택한다.
- 후보 부족 시 폴백 배치가 아니라 명시적인 생성 실패를 반환한다. 잘못된 씬 저작을 숨기지 않는다.
- 보스 미조우 천장은 영구 히스토리가 필요한 P1 항목이다. P0 결정성 검증 후 추가한다.
- 시작점 연속 제한 규칙은 시작점 고정으로 **불필요해졌으므로 제거**한다.

---

## 7. 보스 생성

```text
placement
  -> ActorSpawnManager.SpawnActor(actorId, position, rotation)
  -> MonsterActor 캐스팅 검사
  -> ApplyRuntimeLevel(cycleLevel, difficultyMultiplier)
  -> SetRuntimeRewards(exp, gold)
  -> CycleBossRuntimeHandle 연결
  -> 미발견 마커 생성
```

외곽 보스는 즉시 활성화한다. 중앙 보스 오브젝트와 마커도 같은 레이아웃 단계에서 생성하되, 중앙 보스 오브젝트는 외곽 보스 세 명이 모두 처치될 때까지 비활성 상태로 유지한다. 저장 복원 시 외곽 처치 상태를 기준으로 중앙 활성 여부를 재구성한다.

`CycleBossRuntimeHandle`은 `spawnId`, 중앙 여부, 발견/처치 이벤트만 연결하는 얇은 컴포넌트다. 보스 AI와 전투 로직을 소유하지 않는다.

월드 사이클 보스는 일반 `MonsterRespawnManager` 재스폰 대상에서 제외한다. `SceneEntityId` 영구 처치 기록과도 분리해 다음 사이클의 재배치를 막지 않게 한다.

---

## 8. 미발견 마커와 조우

> `OuterBoss`/`CentralBoss`는 이 문서의 배치 역할이다. 마커·배너·HUD에는 해당 역할명을 노출하지 않고 [CYCLE_STORY_PLOT.md](CYCLE_STORY_PLOT.md)의 플레이어 언어를 따른다.

### 표시 규칙

- 외곽 보스는 작은 회색 신호로 사이클 시작 즉시 실제 위치를 표시한다.
- 중앙 보스는 시작 시 보스와 마커를 모두 숨기고, 외곽 보스 세 명 처치 후 활성화하면서 큰 붉은 보스 마커를 공개한다.
- 회색 표식은 외곽 보스 조우 시 주황색으로 바뀌고, 처치 시 제거된다.
- 미발견 마커 라벨은 역할명 대신 `미확인 상대`로 통일하고 위치만 제공한다.
- 이름, Actor ID, 실루엣, 등급 색상, 속성, 보상은 숨긴다.
- 조우하면 같은 마커 인스턴스의 아이콘과 라벨을 실제 캐릭터 표시명으로 갱신한다. 표시명은 배치된 MonsterActor/ActorDefinition의 런타임 참조를 사용한다.
- 처치하면 마커를 제거하거나 처치 상태 아이콘으로 바꾼다. P0 기본은 제거다.

### `CycleBossMarkerRegistry`

기존 `MinimapMarkerRegistry`는 정적 씬 마커용으로 유지한다. 신규 레지스트리는 런타임 DTO를 받는다.

```csharp
public readonly struct CycleBossMarkerData
{
    public readonly string spawnId;
    public readonly Vector3 worldPosition;
    public readonly bool discovered;
    public readonly bool isCentral;
}
```

나침반 UI가 현재 프로젝트에 완성되어 있지 않다면 레지스트리를 공통 소스로 먼저 만들고, P0에서는 미니맵을 완료한 뒤 같은 데이터를 나침반에 연결한다. 두 UI가 각자 발견 상태를 소유하면 안 된다.

### 조우 판정

다음 중 먼저 발생한 시점에 발견 처리한다.

1. 플레이어가 보스 전용 조우 Trigger에 진입
2. 보스가 플레이어를 감지하고 전투 상태 진입
3. 플레이어가 해당 보스에게 피해를 가함

```text
Discover(spawnId)
  -> 이미 discovered면 무시
  -> CycleLayoutState.discovered = true
  -> ? 아이콘을 보스 아이콘으로 변경
  -> 이름 배너와 BGM 이벤트 발행
  -> 저장 dirty 표시
  -> 텔레메트리 기록
```


---

## 9. 완료 조건

1. 플레이어는 시드·사이클 번호와 무관하게 항상 `fixedPlayerSpawnId` 위치에서 시작하고, 같은 위치에 보스가 생성되지 않는다.
2. 안전 반경 내 보스가 생성되지 않는다.
3. 같은 시드는 같은 보스 `spawnId/actorId` 조합을 만든다.
3-1. 시드만 바꾼 두 런의 `playerSpawnId`가 동일하다.
3-2. `fixedPlayerSpawnId`가 비었거나 해석되지 않으면 레이아웃 생성이 실패하고 추첨으로 폴백하지 않는다.
4. 외곽 3마리와 중앙 1마리가 정확히 한 번 생성된다.
5. 모든 보스는 처음부터 `?`로 보이고 정체 정보는 노출되지 않는다.
6. 조우 시 `?`가 정식 아이콘으로 한 번만 전환된다.
7. 저장·로드 후 발견/처치 상태와 마커가 복원된다.
8. 사이클 보스가 일반 몬스터 재스폰 또는 영구 처치 기록에 섞이지 않는다.
