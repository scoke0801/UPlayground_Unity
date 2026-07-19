# 몬스터 도감 시스템 스펙

> 문서 버전: 1.0
> 기준일: 2026-07-19
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP
> 상태: 코드 구현 완료 — 런타임/기본 UI/데이터·프리팹 빌더 완료,
> 빌더 실행으로 실제 에셋 생성 및 아이콘·밸런스·Play Mode 검증 대기
> 관련 문서: `PASSIVE_ABILITY_SYSTEM_SPEC.md`, `../guide/COMBAT_SYSTEM_GUIDE.md`, `../guide/STAT_SYSTEM_GUIDE.md`, `../cycle/`

---

## 1. 목적

플레이어가 처치한 몬스터의 정보를 종(種) 단위로 누적 기록하고, 종별 기록 진행도(%)에 따라
해당 몬스터를 상대할 때의 전투·성장 보정을 제공한다. 도감은 수집 동기와 반복 사냥 보상을
연결하는 메타 진행 시스템이며, 사이클형 보스 헌팅 구조(`../cycle/`)와 병행해 동작한다.

핵심 목표:

- 처치 기록의 단일 소스는 세이브 데이터이며, 도감 정의(정적 데이터)와 분리한다.
- 종별 기록 진행도(%)에 따라 **경험치 획득량 증가 / 가하는 피해 증가 / 입는 피해 감소** 보정을 제공한다.
- 몬스터 고유 속성(`CombatElement`)을 도감에 표시한다.
- 새 게임마다 속성이 재추첨되는 Humanoid형 몬스터(`CombatElementAssignmentMode.RandomPerNewGame`)는
  아직 조우/판별 전이면 속성 자리에 `?` 아이콘을 표시한다.
- 진입점은 `UI_MenuPanel`에 추가하고, 도감은 별도 고유 UI 클래스와 UI 빌더 코드로 구현한다(Scene 타입).
- 전투 보정 계산은 기존 데미지 해석 파이프라인(`DamageResolver`)과 경험치 지급 경로(`AwardBattleExp`)에
  최소 침습으로 삽입한다.

---

## 2. 설계 결정

| ID | 결정 |
|----|------|
| C-01 | 도감 항목의 정적 정의는 신규 `MonsterCodexEntrySO`가, 전체 목록/조회는 신규 `MonsterCodexDatabaseSO`가 소유한다. 대상 몬스터는 `ActorDatabase`의 `ActorType.Monster` 정의를 기준으로 매핑한다. |
| C-02 | 런타임 기록(처치 수, 발견 여부, 발견된 속성)은 `GameSaveData`에 저장하는 동적 상태이며, 도감 정의에는 저장하지 않는다. |
| C-03 | 기록 진행도(%)는 종별 누적 처치 수를 정의된 목표 처치 수(`fullRecordKillCount`)로 나눈 0~1 값으로 **선형** 산출한다. |
| C-04 | 처치 기록은 신규 매니저 `MonsterCodexManager`(`IMonsterCodexService`)가 소유하며, `MonsterActor.OnDeath`에서 한 번만 통지한다. 기존 `NotifyWorldStatekill`/`GrantPartyExp` 흐름은 변경하지 않는다. |
| C-05 | 전투 보정은 **상대 몬스터 종 기준**으로 적용한다. 가하는 피해 증가·입는 피해 감소는 그 종을 상대할 때만, 경험치 증가는 그 종을 처치할 때만 적용한다. (범위 A 확정) |
| C-06 | 가하는 피해/입는 피해 보정은 `DamageResolver` 최종 배율에 곱으로 합류시키며, `HitPhaseData.damage` 원본 데이터는 수정하지 않는다(패시브 시스템 P-07과 동일 원칙). |
| C-12 | 도감 기록(처치 수·발견 속성)은 **새 게임 단위** 상태다. 새 게임 시작 시 전체 도감 진행도를 리셋한다. `RandomPerNewGame` 속성 재추첨 주기와 정합한다. |
| C-13 | 보스 등급(`MonsterActorGrade.Boss`)도 도감에 포함한다. `includeInCodex` 기본값은 `true`이며, 예외적으로 특정 항목만 제외할 때 끈다. 사이클 보스 어시스트(`BossAssist`) 영입 경로와는 독립이다. |
| C-14 | 진행도→보정 매핑은 **선형**이며, 최대 보정 수치에 상한(cap)을 두지 않는다. 단, 입는 피해 배율은 최종 계산에서 음수 방지 안전 하한만 적용한다. |
| C-07 | 경험치 보정은 `GrantPartyExp` 지급 직전에 최종 배율로 적용한다. `expReward` 정의 값은 수정하지 않는다. |
| C-08 | 도감 UI는 `UI_MonsterCodex`(`UI_Base` 상속, `CanvasLayer.Scene`)로 구현하고, 프리팹은 신규 빌더 `UIMonsterCodexPrefabBuilder`로 생성한다(기존 Scene UI 빌더 패턴 준용). |
| C-09 | 속성 표시는 기존 `UICombatElementDisplay`를 재사용한다. `RandomPerNewGame`이고 미발견이면 속성 대신 `?` 상태를 표시한다. |
| C-10 | 미발견 몬스터(처치 0회)는 실루엣/잠금 상태로 목록에 표기하고 상세 수치는 가린다. |

---

## 3. 현재 기반과 필요한 확장

### 3.1 재사용할 현재 구조

| 영역 | 현재 타입 | 재사용 방식 |
|------|-----------|-------------|
| 몬스터 정의/열거 | `ActorDatabase.All`, `ActorDefinitionSO` (`actorId`, `displayName`, `description`, `actorType`, `EffectiveGrade`) | 도감 항목 매핑과 표시 메타 |
| 몬스터 속성 | `CombatElement`, `CombatElementAssignmentMode`, `ActorDefinitionSO.ResolveCombatElement(seed)` | 속성 표시/발견 판정 |
| 처치 시점 | `MonsterActor.OnDeath` (`Assets/02.Scripts/GameActor/Object/Monster/MonsterActor.cs:510`) | 도감 기록 통지 삽입 지점 |
| 경험치 지급 | `MonsterActor.GrantPartyExp` → `Svc.Party.AwardBattleExp(long)` | 경험치 보정 적용 지점 |
| 데미지 해석 | `DamageResolver` / `CombatPolicyResolver` / `DamageResult.DamageTakenMultiplier` | 가하는·입는 피해 배율 합류 |
| 세이브 | `GameSaveData` (`killedMonsters` 등 Dictionary 패턴), `SaveManager` | 도감 진행 상태 저장/복원 |
| 매니저 계약 | `BaseManager<T>` + `IGameService`, `GameManager.RegisterManager`, `Svc`/`ActorSvc` | `MonsterCodexManager` 등록/조회 |
| 진입 메뉴 | `UI_MenuPanel` (`_partyButton` 등 버튼 → `Toggle(UIKeyType)`) | 도감 진입 버튼 추가 |
| UI 키 | `UIKeyType` enum + `UIKeyTypeExtensions.ToKey`, `UIPrefabDatabase` | 도감 UI 키 등록 |
| Scene UI 패턴 | `UI_Base`(`BlocksLowerInput`, `RegisterInputEvents`), `UISvc.UI.ShowUI/HideUI` | 도감 UI 생명주기 |
| Scene UI 빌더 패턴 | `Assets/02.Scripts/UI/Editor/UIQuestMenuPrefabBuilder.cs` 등 `*PrefabBuilder` | 도감 프리팹 자동 생성 |
| 속성 아이콘 표시 | `UICombatElementDisplay` (`Assets/02.Scripts/UI/Common/`) | 도감 항목 속성 표시 |
| 보정 리더 패턴 | `IPassiveModifierReader.GetActiveMultiplier(...)` | `IMonsterCodexReader` 설계 참고 |

### 3.2 신규로 필요한 구조

| 구분 | 타입 | 역할 |
|------|------|------|
| 정의 SO | `MonsterCodexEntrySO` | 항목별 표시 메타, 목표 처치 수, 진행도별 보정 곡선, 도감 포함 여부 |
| 정의 DB | `MonsterCodexDatabaseSO` | `actorId → MonsterCodexEntrySO` 매핑, 전체 목록 |
| 저장 DTO | `MonsterCodexEntrySave` | 종별 처치 수, 발견 여부, 발견된 속성 |
| 매니저 | `MonsterCodexManager : BaseManager<MonsterCodexManager>, IMonsterCodexService` | 기록 누적/저장/보정 조회 |
| 계약 | `IMonsterCodexService`, `IMonsterCodexReader` | 소비자용 조회 계약(`Svc`/`ActorSvc`) |
| UI | `UI_MonsterCodex` | 도감 화면(목록 + 상세) |
| UI 빌더 | `UIMonsterCodexPrefabBuilder` | 도감 프리팹 자동 생성 |

---

## 4. 데이터 모델

### 4.1 `MonsterCodexEntrySO` (신규, `Assets/02.Scripts/Data/Codex/`)

```
[CreateAssetMenu(menuName = "UPlayGround/도감/Monster Codex Entry")]
- string actorId              // ActorDatabase의 몬스터 actorId와 1:1
- bool includeInCodex = true  // 보스 등 항목별 노출 제어 (C-11)
- Sprite portrait             // 도감 표시용 초상화 (미발견 시 실루엣 처리)
- int fullRecordKillCount     // 100% 기록 도달에 필요한 처치 수 (C-03)
- MonsterCodexBonusCurve bonus // 진행도(0~1) → 보정치
```

`MonsterCodexBonus` (직렬화 구조체):

```
- float maxExpBonus          // 100% 기록 시 경험치 획득 배율 가산분 (예: 0.20 = +20%)
- float maxDamageDealtBonus  // 100% 기록 시 가하는 피해 배율 가산분
- float maxDamageTakenReduce // 100% 기록 시 입는 피해 감소분 (예: 0.15 = -15%)
```

- 진행도→보정은 **선형**이다(C-14). 별도 곡선 에셋을 두지 않는다.
- 최대 보정 수치에 상한(cap)을 두지 않는다. 입는 피해 감소는 최종 배율이 음수가 되지 않도록 안전 하한만 클램프한다.

> 표시명/설명은 `ActorDefinitionSO.displayName`/`description`을 우선 사용하고, 도감 전용 문구가 필요할 때만
> `MonsterCodexEntrySO`에 override 필드를 둔다(중복 저작 최소화).

### 4.2 `MonsterCodexDatabaseSO` (신규)

- `List<MonsterCodexEntrySO> _entries`, `Dictionary<string, MonsterCodexEntrySO> _lookup`
- `ActorDatabase`와 동일한 지연 초기화/중복 경고 패턴(`Assets/02.Scripts/Data/Actor/ActorDatabase.cs`)을 따른다.
- 에디터 검증 툴에서 `ActorType.Monster` 정의와 도감 항목 누락/불일치를 리포트한다(§8).

### 4.3 저장 DTO (`GameSaveData` 확장)

`GameSaveData`에 추가(기존 `killedMonsters` Dictionary 패턴과 동일한 직렬화 정책):

```
public List<MonsterCodexEntrySave> monsterCodex = new();

[Serializable]
public class MonsterCodexEntrySave
{
    public string actorId;
    public long killCount;            // 누적 처치 수
    public bool discovered;           // 최초 조우/처치 여부
    public int discoveredElement;     // (CombatElement) RandomPerNewGame 발견 시 확정 속성, 미발견은 None(0)
}
```

> `killedMonsters`(월드 상태·재스폰 판정용)와 도감 `monsterCodex`(누적 통계·진행도용)는 목적이 다르므로
> 분리한다. 전자는 씬별 개체 GUID 집합, 후자는 종별 카운트다.
>
> **새 게임 단위 상태(C-12):** `monsterCodex`는 현재 새 게임(플레이스루) 범위의 진행도이며 세이브 파일에
> 저장·복원된다. 새 게임 시작 시 전체 항목을 초기화(`killCount=0`, `discovered=false`,
> `discoveredElement=None`)한다. `RandomPerNewGame` 속성 재추첨과 리셋 주기가 일치한다.

---

## 5. 기록 로직

### 5.1 통지 지점

`MonsterActor.OnDeath`(`MonsterActor.cs:510`)의 기존 통지 블록에 한 줄을 추가한다.
`NotifyWorldStateKill`/`GrantPartyExp`/`TryRecruitToParty` 흐름은 그대로 두고 병렬로 추가한다.

```
NotifyQuestMonsterKill();
NotifyRecipeMonsterKill();
NotifyWorldStateKill();
NotifyCodexKill();     // 신규
...
```

```
private void NotifyCodexKill()
{
    ActorSvc.MonsterCodex?.RecordKill(ActorId, ResolveCurrentElement());
}
```

- `ActorId`는 종 식별자이므로 재스폰/동적 스폰과 무관하게 동일 종으로 누적된다.
- `ResolveCurrentElement()`는 런타임에 확정된 `CombatElement`를 반환한다.
  `RandomPerNewGame` 몬스터는 이 값으로 도감의 `discoveredElement`를 채워 `?` → 실제 속성으로 전환한다.

### 5.2 진행도 산출

```
recordRatio = clamp01(killCount / max(1, fullRecordKillCount));   // 선형
```

- `expMultiplier    = 1 + maxExpBonus        * recordRatio`
- `damageDealtMul   = 1 + maxDamageDealtBonus  * recordRatio`
- `damageTakenMul   = max(minFloor, 1 - maxDamageTakenReduce * recordRatio)`  (음수 방지 하한만 클램프, 상한 없음)

### 5.3 보정 조회 계약 (`IMonsterCodexReader`)

```
public interface IMonsterCodexService : IGameService
{
    void RecordKill(string actorId, CombatElement element);
    float GetRecordRatio(string actorId);              // 0~1
    bool IsDiscovered(string actorId);
    CombatElement GetDiscoveredElement(string actorId); // 미발견은 None
    IReadOnlyList<MonsterCodexEntryView> GetAllEntries(); // UI용
}

public interface IMonsterCodexReader   // 전투/성장 소비자용 (얇은 조회)
{
    float GetExpMultiplier(string actorId);        // 경험치 배율 (기본 1)
    float GetDamageDealtMultiplier(string actorId);   // 가하는 피해 배율 (기본 1)
    float GetDamageTakenMultiplier(string actorId);   // 입는 피해 배율 (기본 1)
}
```

`MonsterCodexManager`가 두 계약을 함께 구현하고 `GameManager.RegisterManager`로 자동 등록한다.
소비자는 `ActorSvc.MonsterCodex` / 전용 리더 정적 프로퍼티로 접근한다(`Svc`/`ActorSvc` 계약 규약 준수).

### 5.4 전투/경험치 적용 지점

- **가하는 피해**: 플레이어→몬스터 피해 계산 시 `DamageResolver`(또는 `CombatPolicyResolver`)에서
  대상 몬스터 `ActorId` 기준 `GetDamageDealtMultiplier`를 최종 배율에 곱한다.
- **입는 피해**: 몬스터→플레이어 피해 계산 시 공격자 몬스터 `ActorId` 기준
  `GetDamageTakenMultiplier`를 최종 배율에 곱한다. 기존 `DamageResult.DamageTakenMultiplier`와 같은 합류 규칙을 쓴다.
- **경험치**: `MonsterActor.GrantPartyExp`에서 `AwardBattleExp` 호출 직전
  `exp = round(exp * GetExpMultiplier(ActorId))`로 보정한다.

> 적용 시점 원칙은 패시브 시스템(P-07, P-12)과 동일: 원본 데이터 불변, 런타임 최종 배율에서 1회 적용.

---

## 6. 속성 표시 규칙

| 몬스터 조건 | 도감 표시 |
|-------------|-----------|
| `elementAssignmentMode == Fixed`, `combatElement != None` | 고정 속성 아이콘 (`UICombatElementDisplay`) |
| `Fixed`, `combatElement == None` | 무속성(표시 생략 또는 무속성 아이콘) |
| `RandomPerNewGame`, 미발견(`discoveredElement == None`) | **`?` 아이콘** (Humanoid 랜덤 속성 미확인) |
| `RandomPerNewGame`, 발견됨 | 발견된 속성 아이콘 |

- `RandomPerNewGame`은 새 게임마다 `actorId + newGameSeed`로 재추첨되므로(`CombatElementRules.ResolveRandomElement`),
  `discoveredElement`는 **현재 새 게임 범위** 상태다. 새 게임 시작 시 미발견으로 리셋한다.
- 처치 누적 카운트(`killCount`)도 새 게임 단위다(C-12). 속성 재추첨/리셋 주기와 동일하게 관리한다.

---

## 7. UI 설계

### 7.1 진입점 — `UI_MenuPanel`

- `UI_MenuPanel.cs`에 `_codexButton`(Button) 직렬화 필드와 `OnClickedCodexButton` → `Toggle(UIKeyType.MonsterCodex)`를 추가한다.
- 프리팹 `Assets/03.Prefabs/UI/HUD/UI_MenuPanel.prefab`에 버튼 오브젝트를 추가하고 필드에 바인딩한다.
- 등록/해제 리스너를 `Awake`/`OnDispose`에 기존 버튼과 동일하게 추가한다.

### 7.2 도감 화면 — `UI_MonsterCodex` (Scene 타입, `UI_Base` 상속)

- 위치: `Assets/02.Scripts/UI/Scene/Codex/UI_MonsterCodex.cs`, 네임스페이스 `UPlayGround.UI`.
- `CanvasLayer.Scene`, `BlocksLowerInput = true`, `PerformBackFunction`으로 닫기.
- 구성:
  - **필터**: **등급(`MonsterActorGrade`) · 속성(`CombatElement`)** 필터만 제공한다(지역/이름 정렬 등은 후속). 보스도 목록에 포함된다(C-13).
  - **좌측 목록**: `MonsterCodexDatabaseSO` 순서로 항목 나열. 각 항목에 초상화(미발견 실루엣), 이름, 진행도 게이지.
  - **우측 상세**: 선택 항목의 초상화, 이름/설명, 속성(§6), 진행도 %, 진행도별 보정 요약(경험치/가하는 피해/입는 피해).
  - 미발견 항목은 이름/설명/속성/수치를 가리고 `???`로 표기(C-10).
- 데이터 바인딩은 `ActorSvc.MonsterCodex.GetAllEntries()`(뷰 DTO) 1회 조회로 채운다. 런타임 갱신은 화면 열릴 때 재조회.
- 항목 UI 요소는 `UI_ActorHpBar`류의 필 게이지 패턴과 `UICombatElementDisplay`를 재사용한다.

### 7.3 UI 키/프리팹 등록

- `UIKeyType`에 `MonsterCodex` 추가(자동 생성 enum이므로 **`UPlayGround/ID Enum Generator`로 재생성**),
  `UIKeyTypeExtensions.ToKey`에 매핑 포함.
- `UIPrefabDatabase.asset`에 `MonsterCodex` 키 + 프리팹 + `DefaultLayer = Scene` 등록.

### 7.4 UI 빌더 — `UIMonsterCodexPrefabBuilder` (Editor)

- 위치: `Assets/02.Scripts/UI/Editor/UIMonsterCodexPrefabBuilder.cs`.
- 기존 `UIQuestMenuPrefabBuilder` / `UIInventoryPrefabBuilder` 패턴을 따라 메뉴 항목에서 실행 시
  Canvas + `UI_MonsterCodex` + 목록/상세 레이아웃 + 항목 템플릿을 생성하고 프리팹으로 저장한다.
- 생성 후 `UI_MonsterCodex`의 직렬화 참조(목록 컨테이너, 항목 템플릿, 상세 필드)를 자동 바인딩한다.

---

## 8. 검증 · 툴

- `ActorType.Monster` 정의 중 `MonsterCodexDatabaseSO`에 누락된 항목 리포트(에디터 검증기 규칙 추가,
  `Assets/02.Scripts/Tool/Editor/Validation/ActorDataValidator.cs` 패턴 준용).
- `fullRecordKillCount <= 0`, 보정 곡선 음수/과대치 경고.
- `includeInCodex == false`인데 목록 UI에서 참조되는 경우 경고.
- EditMode 테스트: 진행도 산출(`recordRatio`/`bonusRatio`), 보정 배율 클램프, `RandomPerNewGame` 발견 전/후
  속성 상태 전이의 순수 계산 검증.

---

## 9. 확정 사항 (2026-07-19)

| 항목 | 결정 | 참조 |
|------|------|------|
| 보정 적용 범위 | **상대 종 한정(A)**. 그 종을 상대할 때/처치할 때만 적용 | C-05 |
| 처치 카운트 지속성 | **새 게임 단위**. 새 게임 시작 시 도감 진행도 전체 리셋 | C-12 |
| 보스 도감 포함 | **포함**. `includeInCodex` 기본 `true` | C-13 |
| 진행도→보정 곡선 | **선형** | C-03, C-14 |
| 최대 보정 수치 상한 | **상한 없음**(입는 피해만 음수 방지 안전 하한) | C-14 |
| 도감 필터 | **등급 · 속성 필터만** | §7.2 |

### 남은 밸런스 작업(구현 후)

- 종별 `fullRecordKillCount`, `maxExpBonus`/`maxDamageDealtBonus`/`maxDamageTakenReduce` 초안값 설정.
- 밸런스 툴로 종별 보정 누적 영향 검증(상한이 없으므로 고반복 사냥 시 과증폭 여부 확인).

---

## 10. 구현 순서(권장)

1. 데이터: `MonsterCodexEntrySO`, `MonsterCodexDatabaseSO`, `GameSaveData.monsterCodex` DTO.
2. 매니저: `MonsterCodexManager` + `IMonsterCodexService`/`IMonsterCodexReader`, `GameManager` 등록, 세이브 연동.
3. 기록 훅: `MonsterActor.NotifyCodexKill` 삽입.
4. 보정 훅: `DamageResolver`(가하는/입는 피해), `GrantPartyExp`(경험치).
5. UI: `UIKeyType`/`UIPrefabDatabase` 등록 → `UI_MonsterCodex` → `UIMonsterCodexPrefabBuilder` → `UI_MenuPanel` 진입 버튼.
6. 검증기 + EditMode 테스트 + 초안 데이터 구성.

---

## 11. 구현 체크포인트 (2026-07-19)

완료:

- `MonsterCodexEntrySO`, `MonsterCodexDatabaseSO`, 선형 보정 계산기와 UI 뷰 DTO
- `GameSaveData.monsterCodex` 저장 DTO 및 구버전 세이브 null 보정
- `MonsterCodexManager` 서비스 등록, 저장/복원/새 게임 리셋
- `MonsterActor.OnDeath` 처치 기록과 경험치 배율 적용
- `DamageResolver` 플레이어→몬스터/몬스터→플레이어 및 특수 브레이크 피해 배율 적용
- `UIKeyType.MonsterCodex`, `UI_MenuPanel` 진입 코드, `UI_MonsterCodex`/목록 슬롯 코드
- `UIMonsterCodexPrefabBuilder`(도감/슬롯 프리팹, 메뉴 버튼, UIPrefabDatabase 등록)
- UI Toolkit 기반 `MonsterCodexEditorWindow`(검색, Actor 연결 확인, 항목 편집, 생성/검증/저장)
- 전체 화면 Scene UI 앵커, 고정 헤더/필터, 제한 폭 목록/유연 상세 패널과 슬롯 Button 클릭 경로
- ActorDatabase 기반 도감 데이터 생성·Addressable 등록 및 누락/수치 검증 메뉴
- 진행도/보정/입는 피해 안전 하한 EditMode 테스트
- Data/Contracts/Actor/Assembly-CSharp/UI/Data.Editor/Test 프로젝트 CLI 컴파일 오류 0

남음:

- 에디터 메뉴 `UPlayGround/도감/몬스터 도감 데이터 생성 또는 갱신` 실행 후 종별 초안값 조정
- Unity EditMode 테스트 실행, Play Mode에서 최종 UI 레이아웃/클릭, 처치/세이브/로드/새 게임 리셋/양방향 피해 보정 스모크
