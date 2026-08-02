# Cycle Runtime 구현 스펙

## 1. 목표

사이클의 생성, 진행, 완료, 포기와 현재 상태 조회를 한 곳에서 관리한다. 월드 스폰·전투·UI·세이브가 서로 직접 참조하지 않고 사이클 상태를 통해 협력하게 한다.

P0에서 해결할 문제:

- 재현 가능한 시드 생성
- 사이클 1~3 난이도 배율
- 시작부터 탈출 정산까지 명시적인 상태 전이
- 중복 시작·중복 정산·보스 처치 직후 자동 종료 방지
- 각 하위 서비스가 사용할 단일 런 컨텍스트 제공

---

## 2. 기존 코드 접점

| 기존 타입 | 사용 방식 |
|---|---|
| `GameManager` | 신규 매니저 등록 및 생명주기 호출 |
| `IManager` | `Init`, `AfterInit`, `Dispose`, `OnSceneChanged` 계약 |
| `ISaveable` | 실행 중 사이클 상태 내보내기·복원·새 게임 초기화 |
| `SaveManager` | 사이클 매니저 등록과 저장 실행 |
| `SceneManager` | 현재 맵 ID와 사이클 플레이 씬 확인 |
| `PortalActor` | 중앙 보스 처치 후 탈출 포털 활성화 |
| `PartyManager` | 현재 파티와 전투력 조회. 사이클 상태를 소유하지 않음 |

`ActorSpawnManager`는 Actor ID 기반 생성만 담당한다. 시드 추첨과 배치 정책을 넣지 않는다.

---

## 3. 신규 책임

### `CycleRunManager`

`BaseManager<CycleRunManager>`, `IManager`, `ISaveable` 구현을 권장한다.

| 책임 | 설명 |
|---|---|
| 상태 전이 | `Inactive → Preparing → Active → BossDefeated → Settling → Completed` |
| 시드 | 새 시드 발급, 지정 시드 재현, RNG 스트림 제공 |
| 런 컨텍스트 | 사이클 번호, 맵 ID, 시작 시각, 배치 결과 참조 |
| 완료 조건 | 중앙 보스 처치와 탈출 포털 진입을 분리 |
| 서비스 조정 | 월드 생성, 정산, 저장 요청 순서 제어 |
| 이벤트 | HUD와 텔레메트리에 상태 변경 통지 |

### `CycleConfigSO`

P0 공통 튜닝값을 보관한다.

```csharp
// 신규 제안 데이터 형태. 구현 시 namespace와 직렬화 정책을 프로젝트 규칙에 맞춘다.
public sealed class CycleConfigSO : ScriptableObject
{
    public int prototypeTargetMinutes = 20;
    public int releaseMaxMinutes = 40;
    public List<CycleDifficultyEntry> difficultyByCycle;
    public float expLossRate = 0.30f;
    public bool dropUnsettledMaterials = true;
    public bool enableEquipmentFragmentLoss = false;
}
```

`difficultyByCycle`은 P0에서 세 항목만 허용한다.

| 사이클 | HP | 공격력 | 보상 등급 |
|---|---|---|---|
| 1 | 1.00 | 1.00 | 일반 중심 |
| 2 | 1.35 | 1.18 | 희귀 추가 |
| 3 | 1.75 | 1.38 | 영웅 추가 |

---

## 4. 런타임 상태 모델

```csharp
public enum CycleRunPhase
{
    Inactive,
    Preparing,
    Active,
    BossDefeated,
    Settling,
    Completed,
}

[Serializable]
public sealed class CycleRunState
{
    public int cycleIndex;
    public int seed;
    public string mapId;
    public CycleRunPhase phase;
    public float elapsedSeconds;
    public bool centralBossDefeated;
    public bool exitPortalActivated;
}
```

- `CycleRunState`는 저장 가능한 순수 데이터다.
- Unity 오브젝트 참조는 상태 DTO에 넣지 않는다.
- 현재 배치의 세부 정보는 `CycleLayoutState`가 소유하고 `CycleRunState`가 포함하거나 별도 필드로 저장한다.
- RNG는 `UnityEngine.Random` 전역 상태를 사용하지 않는다. 사이클 전용 결정적 RNG 인스턴스를 사용한다.
- 서로 다른 시스템이 추첨 순서에 영향을 주지 않도록 `Layout`, `BossPool`, `Reward` 스트림을 시드에서 파생하는 방식을 권장한다.

### 시드가 결정하지 않는 것

시드의 적용 범위를 명시적으로 제한한다. 아래 항목은 시드를 바꿔도 변하지 않는다.

| 항목 | 결정 주체 | 근거 문서 |
|---|---|---|
| 플레이어 시작 지점 | `CycleWorldConfigSO.fixedPlayerSpawnId` (고정) | `02_WORLD_SPAWN_ENCOUNTER_SPEC.md` 6.1 |
| 보스 어시스트 영입 성공 여부 | 플레이 조건 달성 (확정) | `04_BOSS_ASSIST_RECRUITMENT_SPEC.md` 5 |
| 캐릭터 스킬 노드 구성·포인트 총량 | 캐릭터별 고정 저작 + 레벨의 함수 | `08_CHARACTER_SKILL_GROWTH_SPEC.md` |

P0에서 시드가 실제로 결정하는 것은 **외곽·중앙 보스의 위치와 종류, 부활 지점 활성화, 보상 롤**뿐이다. 새 랜덤 요소를 추가할 때는 이 표에 넣을지 먼저 판단하고, 넣지 않기로 하면 결정 주체를 함께 명시한다.

---

## 5. 상태 전이

### 시작

```text
StartCycle(request)
  -> 현재 phase가 Inactive/Completed인지 검사
  -> cycleIndex와 seed 확정
  -> Preparing
  -> CycleWorldSpawnService.BuildLayout()
  -> 플레이어 위치 적용
  -> 보스와 마커 생성
  -> Active
  -> 즉시 저장 요청
```

### 중앙 보스 처치

```text
NotifyCentralBossDefeated(bossInstanceId)
  -> 현재 중앙 보스와 일치하는지 검사
  -> 중복 알림 무시
  -> centralBossDefeated = true
  -> BossDefeated
  -> 탈출 PortalActor 활성화
  -> 저장 요청
```

보스 처치만으로 정산하지 않는다. 플레이어가 전리품과 유해를 정리할 선택을 보존한다.

### 탈출

```text
RequestExit()
  -> phase == BossDefeated 검사
  -> Settling
  -> CycleSettlementService.Settle()
  -> 런 한정 상태 정리
  -> Completed
  -> 저장 요청
```

### 포기

P0에서는 전용 포기 UI를 만들지 않는다. 개발 치트로만 `AbortCycle`을 제공하며 영구 보상 없이 런 상태를 초기화한다. P1에서 포기 페널티를 확정한 뒤 사용자 UI를 추가한다.

---

## 6. 공개 API와 이벤트

```csharp
public CycleRunState Current { get; }
public bool IsActive { get; }

public bool StartNewCycle(int? requestedSeed = null);
public bool NotifyCentralBossDefeated(string spawnId);
public bool RequestExit();

public event Action<CycleRunState> OnPhaseChanged;
public event Action<int> OnCycleStarted;
public event Action<int> OnCycleCompleted;
```

- 실패 가능한 명령은 `bool` 또는 명시적인 결과 enum을 반환한다.
- 이벤트 구독자는 상태를 변경하지 않는다.
- 하위 서비스는 `CycleRunManager`를 다시 호출하는 순환 의존을 만들지 않는다.

---

## 7. GameManager 등록 순서

권장 순서:

```text
SaveManager
InputManager
AssetManager
...
PartyManager
WorldStateManager
ActorSpawnManager
CycleRunManager
SceneManager
...
```

`CycleRunManager`가 `ActorSpawnManager`와 `PartyManager`의 준비를 요구하므로 두 매니저 뒤에 등록한다. 비동기 설정 로드가 필요하면 `IAsyncInitializableManager`를 추가하고 `CycleConfigSO`를 Addressables 전역 키로 로드한다.

---

## 8. 실패 처리

- 설정 또는 스폰 후보가 없으면 `Preparing`에서 `Active`로 넘어가지 않는다.
- 부분 생성된 보스와 마커를 정리한 뒤 오류를 기록한다.
- 중앙 보스 ID가 레이아웃과 다르면 처치 알림을 거부한다.
- 정산 도중 실패하면 `Settling` 상태를 저장하지 않는다. 정산은 메모리에서 검증 후 한 번에 커밋한다.
- 씬 변경 시 사이클 플레이 씬이 아니면 런 오브젝트 참조를 정리하되 저장 DTO는 유지한다.

---

## 9. 완료 조건

1. 동일 시드와 설정으로 동일 `CycleLayoutState`가 생성된다.
2. 사이클 시작을 두 번 호출해도 보스가 중복 생성되지 않는다.
3. 중앙 보스 처치 전 탈출 요청이 거부된다.
4. 중앙 보스 처치 후에도 포털 진입 전에는 정산되지 않는다.
5. 사이클 1~3 배율이 스폰된 몬스터 런타임 스탯과 보상에 적용된다.
6. 저장·로드 후 상태 전이가 이어진다.
7. 새 게임에서는 이전 시드, 배치, 완료 상태가 남지 않는다.
