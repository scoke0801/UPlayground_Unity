# Cycle Save & Settlement 구현 스펙

## 1. 목표

사이클 실행 상태와 영구 진행을 명확히 분리해 저장하고, 중앙 보스 처치 후 탈출 포털 진입 시 한 번만 정산한다. 로드 순서와 중복 정산으로 아이템·경험치·로스터가 복제되지 않게 한다.

---

## 2. 기존 저장 구조

`GameSaveData`에는 현재 다음 루트가 존재한다.

- `inventory`
- `story`
- `flags`
- `recipe`
- `quest`
- `party`
- `world`
- `time`

`PartySaveData`는 플레이어블 로스터, BattleOrder, 레벨·경험치, HP·게이지, 위치를 저장한다. 보스 어시스트와 사이클 상태를 이 DTO에 넣지 않는다.

기존 `ISaveable` 계약:

```csharp
void ExportSaveData(GameSaveData saveData);
void ImportSaveData(GameSaveData saveData);
void ResetForNewGame();
```

신규 `CycleRunManager` 또는 전용 저장 소유자가 이 계약을 구현한다.

---

## 3. 저장 데이터 확장

```csharp
public class GameSaveData
{
    // 기존 필드 유지
    public CycleSaveData cycle = new CycleSaveData();
}

[Serializable]
public sealed class CycleSaveData
{
    public int dataVersion = 1;
    public CycleRunState run;
    public CycleLayoutState layout;
    public List<CycleItemStack> unsettledMaterials;
    public RemainsState remains;
    public AssistProgressSaveData assists;
    public CycleHistorySaveData history;
}
```

### 실행 중 데이터

| 데이터 | 저장 시점 |
|---|---|
| phase, cycleIndex, seed, elapsed | 시작·상태 변경·일반 저장 |
| layout와 발견/처치 상태 | 생성·조우·처치 |
| 미정산 재료 | 획득·사망·회수·정산 |
| 유해 | 생성·회수·재사망 |
| 어시스트 남은 쿨다운 | 일반 저장·전멸 |

### 영구 데이터

| 데이터 | 소유 |
|---|---|
| 플레이어블 로스터·레벨·경험치 | 기존 `PartySaveData` |
| 캐릭터별 스킬 포인트·취득 노드 | `PartySaveData.skillProgress`의 `CharacterSkillProgressState` |
| 영구 인벤토리·골드·장비 | 기존 `InventorySaveData` |
| 어시스트 로스터·장착 ID | `AssistProgressSaveData` |
| 보스별 누적 처치 횟수 | `AssistProgressSaveData` |
| 완료 사이클 수 | `CycleHistorySaveData` |

스킬 진행도는 **영구 데이터**다. 사이클 정산·전멸·포기 어느 경로에서도 변경하지 않는다. 상세 스키마는 `08_CHARACTER_SKILL_GROWTH_SPEC.md` 5.3절을 따른다.

`직전 시작점`은 시작점 고정으로 의미가 없어져 `CycleHistorySaveData`에서 제거한다.

---

## 4. 어시스트 저장

```csharp
[Serializable]
public sealed class AssistProgressSaveData
{
    public List<string> roster;
    public string equippedAssistId;
    public List<AssistDefeatCountEntry> defeatCounts;   // 보스별 누적 처치 횟수
    public List<AssistCooldownEntry> cooldowns;
    public string pendingRecruitAssistId;
}
```

- 로스터와 누적 처치 횟수는 영구 유지한다. 처치 횟수는 영입 성공 후에도 초기화하지 않는다.
- 쿨다운은 실행 중 사이클에만 의미가 있다. 사이클이 `Inactive/Completed`면 로드 시 0으로 정리한다.
- 쿨다운은 `Time.time` 종료시각이 아니라 남은 초로 저장한다.
- 존재하지 않는 `assistId`는 로드 경고 후 제거한다.
- 장착 ID가 로스터에 없으면 첫 로스터 항목 또는 빈 값으로 보정한다.

---

## 5. 로드 순서

```text
SaveManager가 GameSaveData 역직렬화
  -> PartyManager가 기존 파티 pending load 보관
  -> CycleRunManager가 CycleSaveData pending load 보관
  -> AssetManager/ActorSpawnManager DB 준비
  -> PartyManager 파티·플레이어 복원
  -> CycleRunManager 설정·씬 확인
  -> CycleLayoutState로 보스 생성
  -> 발견/처치 상태와 마커 복원
  -> CycleLootLedger 복원
  -> RemainsActor 복원
  -> BossAssistManager 쿨다운 복원
```

중요 규칙:

- 레이아웃을 로드할 때 RNG를 다시 굴리지 않는다.
- 저장된 보스 Actor가 이미 씬에 있으면 중복 생성하지 않는다.
- `PartyManager`가 플레이어 위치를 복원한 뒤 사이클 시작점으로 덮어쓰지 않는다.
- 저장된 `mapId`와 현재 사이클 씬이 다르면 씬 준비가 끝날 때까지 pending 상태를 유지한다.

---

## 6. 저장 호환성

- 기존 세이브에는 `cycle` 필드가 없을 수 있다. null이면 `Inactive` 기본 상태로 생성한다.
- `GameSaveData.saveVersion` 변경 여부는 기존 SaveManager 마이그레이션 정책에 맞춘다.
- `CycleSaveData.dataVersion`을 별도로 둬 사이클 DTO만 마이그레이션할 수 있게 한다.
- enum은 가능한 한 문자열 또는 안정 ID로 저장한다.
- 스폰 후보와 어시스트 ID 변경은 명시적인 마이그레이션 테이블 없이는 금지한다.

---

## 7. 정산 서비스

### `CycleSettlementService`

정산은 다음 계획을 먼저 만들고 검증한 뒤 적용한다.

```csharp
public sealed class CycleSettlementPlan
{
    public string settlementId;
    public List<CycleItemStack> materialRewards;
    public int completedCycleIndex;
    public bool discardRemains;
}
```

### 정산 순서

```text
RequestExit
  -> phase == BossDefeated 확인
  -> Settling 진입
  -> 현재 ledger와 보상으로 SettlementPlan 생성
  -> 아이템 ID·수량·로스터 보류 상태 검증
  -> InventoryManager.AddItem으로 재료 커밋
  -> 완료 사이클 히스토리 갱신
  -> 미회수 유해 폐기
  -> ledger/layout/run 한정 상태 정리
  -> phase = Completed
  -> 저장 1회
```

- 중앙 보스 처치 시점에는 정산하지 않는다.
- 포털 진입 콜백이 여러 번 들어와도 `Settling/Completed`에서는 거부한다.
- P0에서는 영혼 결정, 유물, 랜덤 접사 정산을 넣지 않는다.
- 5마리째 어시스트 보류가 있으면 정산 UI에서 방출 또는 신규 포기 결정을 완료한 뒤 커밋한다.

### 중복 방지

- `settlementId = seed + cycleIndex + completionSequence` 형태의 안정 ID를 저장한다.
- 마지막 완료 ID와 같은 정산 요청은 무시한다.
- 적용 도중 예외가 발생하면 `Completed`로 바꾸지 않는다.
- 기존 저장 시스템이 트랜잭션 파일 쓰기를 지원하지 않으면 정산 적용과 저장 사이 구간을 최소화하고, 개발 빌드에서 중복 정산 치트를 제공해 검증한다.

---

## 8. 새 게임과 사이클 재시작

### `ResetForNewGame`

다음을 모두 초기화한다.

- 런 상태와 레이아웃
- 미정산 재료
- 유해
- 어시스트 로스터·장착·누적 처치 횟수
- 캐릭터별 스킬 포인트·취득 노드
- 사이클 히스토리
- pending load와 pending recruit

기존 파티·인벤토리 초기화는 각 매니저가 계속 소유한다.

### 다음 사이클

- 어시스트 로스터와 누적 처치 횟수 유지
- 캐릭터 스킬 포인트·취득 노드 유지 (사이클 전환은 스킬 진행도를 건드리지 않는다)
- 어시스트 쿨다운 0
- 이전 레이아웃·발견·유해·미정산 재료 제거
- `cycleIndex + 1`과 새 시드로 `Preparing` 시작
- P0는 3사이클 이후 추가 시작을 개발 경고와 함께 막거나 3사이클 데이터를 반복하지 않는다.

---

## 9. 저장 시점

필수 자동 저장 트리거:

1. 사이클 시작 성공
2. 보스 발견
3. 외곽·중앙 보스 처치
4. 어시스트 영입 결과
5. 파티 전멸과 새 유해 생성
6. 유해 회수
7. 탈출 정산 완료

재료 한 개 획득마다 디스크 저장하지 않는다. 원장 dirty 플래그를 두고 기존 저장 정책 또는 휴식 지점 상호작용 때 함께 저장한다.

---

## 10. 완료 조건

1. 구버전 세이브를 로드하면 사이클이 비활성 기본 상태로 열린다.
2. 실행 중 저장·로드 후 동일 보스 배치와 발견 상태가 복원된다.
3. 미정산 재료와 유해가 중복·유실 없이 복원된다.
4. 어시스트 로스터·장착·누적 처치 횟수가 영구 유지되고 쿨다운은 실행 중에만 유지된다.
4-1. 사이클 시작·완료·전멸 어느 경로에서도 스킬 포인트와 취득 노드가 변하지 않는다.
5. 중앙 보스 처치만으로 재료가 인벤토리에 들어오지 않는다.
6. 포털 진입 정산이 정확히 한 번 적용된다.
7. 새 게임에 이전 사이클 데이터가 누수되지 않는다.
8. 다음 사이클은 이전 유해·마커·레이아웃 없이 시작한다.
