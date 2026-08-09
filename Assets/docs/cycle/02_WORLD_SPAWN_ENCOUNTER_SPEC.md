# World Spawn & Encounter 구현 스펙

## 1. 목표

**고정된 플레이어 시작점**에서 출발해, 수작업 검증된 N개 외곽 후보에서 보스 위치를 결정하고 중앙 보스를 별도 아레나에 생성한다. 모든 보스 위치는 처음부터 `?`로 표시하되 정체는 실제 조우 전까지 숨긴다.

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

`CycleBossRuntimeHandle`은 `spawnId`, 중앙 여부, 발견/처치 이벤트만 연결하는 얇은 컴포넌트다. 보스 AI와 전투 로직을 소유하지 않는다.

월드 사이클 보스는 일반 `MonsterRespawnManager` 재스폰 대상에서 제외한다. `SceneEntityId` 영구 처치 기록과도 분리해 다음 사이클의 재배치를 막지 않게 한다.

---

## 8. 미발견 마커와 조우

### 표시 규칙

- 외곽·중앙 보스 모두 사이클 시작 즉시 실제 위치에 `?`를 표시한다.
- 마커는 위치만 제공한다.
- 이름, Actor ID, 실루엣, 등급 색상, 속성, 보상은 숨긴다.
- 조우하면 같은 마커 인스턴스의 아이콘과 라벨을 갱신한다.
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

### 자동 생성 검증 퀘스트

`CycleWorldConfigSO.autoGeneration.enabled`와 `generateValidationQuest`가 모두 켜져 있으면 저장되는 `CycleLayoutState.generatedContent`를 단일 소스로 런타임 퀘스트를 저작한다. QuestDatabase 에셋이나 `QuestIdType`은 변경하지 않는다.

- ID: `cycle:auto:{mapId}:{cycleIndex}:{seed}`
- 목표: 일반 조우 완료, 외곽·중앙 보스 처치, 자동 루팅 획득, 자동 상호작용 완료
- 목표 수량: 설정값을 다시 읽지 않고 실제 저장 레이아웃의 유효 항목 수와 루팅 수량 합계에서 계산
- 진행 상태: 조우/보스/루팅/상호작용의 저장 플래그에서 복원
- 저장 규칙: 런타임 QuestSO와 퀘스트 진행 카운트는 일반 퀘스트 세이브에서 제외하고, 같은 레이아웃으로 재저작한 뒤 완료 플래그를 재적용
- 완료 저장 복원: 모든 목표가 끝난 레이아웃이면 퀘스트를 다시 수락하지 않아 완료 이벤트·효과음의 중복 재생을 막음

P0 `LakeOfLife`는 `autoGeneration.enabled = true`로 일반 조우 12개, 루팅 6개, 상호작용 3개와 검증 퀘스트를 생성한다. 외곽 3개와 중앙 1개의 지역 경로마다 쉬움·보통·어려움 조우를 하나씩 배치한다. 기존 외부 데모용 Animal NavMesh는 사용하지 않는다. 퀘스트 자체에는 별도 보상을 넣지 않아 사이클 정산 보상과 중복되지 않는다.

### 일반 조우·루팅·상호작용 자동 생성

`CycleWorldAutoGenerationSettings.enabled`가 켜져 있으면 `UPlayGround.World.Generation`의 순수 계획기가 `CycleLayoutState.generatedContent`를 만든다. 계획기는 씬과 Manager를 참조하지 않으며 입력 후보를 안정 ID로 정렬한다.

- `Encounter`, `Loot`, `Interaction`은 서로 독립된 `CycleRandomStream`을 사용한다. 한 종류의 개수를 바꿔도 다른 종류의 결과를 흔들지 않는다.
- `requireEveryRoutePerDifficultyZone`가 켜진 맵은 활성 난이도 구역의 조우 수가 지역 경로 수 이상이어야 한다. 각 구역은 경로를 순환 배정하고, 구역 진행률 범위를 조우 수만큼 겹치지 않는 층으로 나눈 뒤 각 층 안에서 시드 난수를 사용한다. 따라서 모든 지역 경로가 난이도별로 최소 한 번씩 사용되고 같은 구역의 조우가 한 지점에 몰리지 않는다.
- 경로 진행률 범위는 `easyRouteMin/MaxProgress` → `normalRouteMin/MaxProgress` → `hardRouteMin/MaxProgress` 순서로 겹치지 않아야 한다. 플레이어에서 멀어질수록 위협 예산과 몬스터 후보 티어가 함께 상승한다. 루팅·상호작용도 `auxiliaryRouteMin/MaxProgress` 전체를 층화해 지역 경로에 균등 순환 배정한다.
- 조우의 `threatBudget`은 정상 선택에서 남은 예산 이하 후보만 뽑는다. 후보가 하나도 들어가지 않는 경우에는 빈 조우를 막기 위해 해당 구역에서 허용되는 최저 비용 후보를 한 번 배치하며, 허용 구역 후보도 없으면 전체 최저 비용 후보를 사용한다. 따라서 현재 값은 절대 상한이 아니라 목표 예산이다.
- 순수 계획기는 임시 좌표만 저장하지 않고 `routeId`, 누적 길이 기준 `routeProgress`, 횡방향 오프셋과 멤버 로컬 오프셋도 함께 저장한다. 런타임은 플레이어 스폰에서 선택된 각 보스 스폰까지 먼저 직선 지면 경로를 검사한다. 직선이 막히면 `routeDetourStep` 간격과 `routeDetourMaxOffset` 범위의 측면 격자 DAG에서 최소 비용 경로를 결정론적으로 찾고, 저장된 진행률을 최종 경로의 누적 길이에 투영해 검증된 `anchorPosition`/`position`을 저장한다.
- `CycleSpawnPoint`/중앙 보스 마커는 액터 스폰 피벗이면서 경로의 XZ 목표다. 경로 시작·도착 앵커는 `groundProbeUpDistance`/`groundProbeDownDistance` 범위에서 허용 Terrain 지면으로 변환하며, 자동 생성 콘텐츠 배치 후보용 `maxGroundProjectionDistance`를 적용하지 않는다. 경로 중간 표본은 두 마커의 Y를 선형 보간하지 않고 직전 Terrain 표본의 실제 Y를 다음 수직 탐색 기준으로 사용한다. 우회 격자도 도달한 노드의 Terrain 표본을 캐시해 후속 간선이 같은 지면 높이에서 이어지도록 한다.
- 지면 표본은 설정된 표면 레이어를 위에서 아래로 모두 검사한다. 가장 위의 비트리거 충돌체가 명시된 Ground 레이어의 `TerrainCollider`일 때만 승인한다. 급경사, 설정된 단차보다 큰 높이 불연속, 낮은 천장·바위 같은 캡슐 장애물은 실패로 처리한다.
- Collider가 없는 물 메시도 허용 지면으로 오인하지 않도록 `excludedSurfaceMaterials`에 물 Material 에셋을 명시한다. 런타임은 해당 Material을 쓰는 현재 활성 `MeshRenderer`에 숨김 `MeshCollider` 프록시를 검증 중에만 만든다. 목록의 에셋 참조가 유실되면 실패하지만 현재 씬에서 비활성·미사용인 Material은 복원을 막지 않는다. 오브젝트 이름이나 Material 이름 추론은 사용하지 않는다.
- 각 몬스터의 `KinematicCharacterMotor` 프리팹에서 반지름·높이·Y 오프셋·최대 안정 경사·최대 단차를 읽어 실제 캡슐로 지면과 이동 구간을 검사한다. Terrain 나무는 Ground `TerrainCollider`와 같은 레이어로 보고되므로 tree instance와 prototype Collider의 보수적 월드 bounds를 별도 공간 캐시에 넣어 정지 캡슐과 이동 구간 모두 검사한다. 멤버끼리의 가상 캡슐 중첩과 캡슐 반지름을 포함한 모든 보스의 `bossExclusionRadius` 침범도 거부한다.
- 자동 조우는 조우별 `MonsterGroupController` 루트 아래에 몬스터를 생성한다. 생성이 끝나면 부모 계층, 그룹 레지스트리, `IEnemyAIController.Group` 참조와 생존 멤버 수를 교차 검증한 뒤 그룹을 활성화한다. 하나라도 연결되지 않으면 개별 AI로 조용히 폴백하지 않고 사이클 월드 생성을 실패시킨다.
- 최초 생성에서 기준점이나 개별 멤버 위치가 부적합하면 generation ID와 placement ID의 안정 해시로 정렬된 고정 ring 후보만 제한 반경 안에서 탐색한다. 멤버별 최종 위치와 갱신된 로컬 오프셋을 저장하며, Unity 전역 난수와 `string.GetHashCode`는 사용하지 않는다.
- 우회 격자의 모든 간선은 직선 경로와 동일한 Ground Terrain, 물 제외, 경사·단차, KCC 캡슐 장애물 검사를 통과해야 한다. 레이어나 장식물 Collider를 무시하는 폴백은 허용하지 않는다. 탐색은 전진 또는 한 단계 측면 전진만 허용하는 제한된 배치 경로이며 AI 길찾기는 아니다. 제한 반경 안에 경로가 없으면 월드 생성은 실패하고, 복잡한 우회가 필요한 맵은 수작업 route/area 또는 일반 지면 그래프를 별도 저작한다.

### 복원 실패와 정리 계약

`TryRestore`는 보스, 일반 조우, 루팅, 상호작용, 런타임 퀘스트를 순서대로 복원한다. 중간 단계가 실패하면 해당 복원 시도에서 생성한 `_spawnedObjects`와 `cycle:auto:` 런타임 퀘스트를 모두 정리한 뒤 실패를 반환한다. 부분 성공 오브젝트를 다음 재시도에 남겨서는 안 된다. 다시 스폰할 미완료 자동 생성 위치와 그 항목이 참조하는 지면 경로만 현재 Ground/KCC/물 제외/보스 반경 계약을 저장 좌표 그대로 통과해야 한다. 복원에서는 ring 재탐색이나 좌표 보정을 하지 않으며, 검증은 복제본에 원자적으로 수행한다. 이미 완료되어 스폰하지 않는 항목의 과거 좌표나 그 전용 경로 변화는 복원을 막지 않는다. 결정론적 우회 경로가 도입된 현재 `placementValidationVersion`은 2이며, 버전이 다른 구형 자동 배치 레이아웃은 현재 규칙으로 암묵 재해석하지 않는다.

씬에는 활성 `CycleWorldContext`를 하나만 둔다. 같은 Context의 중복 등록은 no-op이고, 다른 Context가 같은 씬에서 추가 등록되면 기존 런타임 오브젝트의 소유권을 잃지 않도록 오류로 거부한다.

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
