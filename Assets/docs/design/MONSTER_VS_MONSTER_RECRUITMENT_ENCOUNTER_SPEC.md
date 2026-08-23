# 몬스터 진영 전투 및 공동전투 영입 조우 구현 스펙

> 문서 버전: **v0.17-destination-led-opening**<br>
> 작성일: **2026-08-17**<br>
> 상태: **공용 런타임·저장·FlowGraph 구현 및 LakeOfLife 2단계 영입 콘텐츠 바인딩 완료 / 획득 후 대화 저장 경계 추가 / Play Mode 수직 슬라이스 재검증 대기**<br>
> 범위: 몬스터 대 몬스터 전투, 임시 아군, 지역 조우, 전투 완료 대화, 플레이어블 파티 영입, 획득 후 대화, 저장/로드<br>
> 비범위: 신규 전투 모션 제작, 최종 컷신 제작, BossAssist 영입, 동시 다인 파티 AI

## 0. 결론

요구 시나리오는 다음과 같이 구현한다.

```text
지역 진입
  → 영입 대상 액터와 적 그룹의 실전 전투 활성화
  → 플레이어와 영입 대상 액터를 같은 전투 진영으로 판정
  → 적 그룹 전멸
  → 전투 상태 정리 및 영입 대상 액터 대화 상태 전환
  → 필수 대화 정상 완료
  → 플레이어블 캐릭터 로스터에 멱등 영입
  → 조우 완료를 영구 저장
```

이 기능은 Trigger 여러 개의 연쇄로 만들지 않는다. **FlowGraph가 순서를 소유**하고, 지역 볼륨은 시작 신호만 보낸다. 전투 관계는 레이어나 `ActorType`이 아니라 **데이터 기반 전투 진영**으로 판정하며, 영입은 `MonsterActor._recruitableAs`의 사망 보상 경로를 사용하지 않는다.

핵심 구현 결정은 다음과 같다.

1. `CombatFactionSO`와 `ICombatRelationService`를 도입해 `Ally / Neutral / Hostile` 관계를 한 곳에서 판정한다.
2. 영입 대상은 `MonsterActor` 기반 AI 전투 런타임을 재사용하되 `RecruitmentEncounterParticipant`가 임시 진영, 생존 정책, 보상 귀속을 덮어쓴다.
3. `StoryManager`가 `IRecruitmentEncounterService`를 구현하고 순수 상태 저장소에 조우 단계를 보존한다. 신규 최상위 매니저는 만들지 않는다.
4. 씬의 `RecruitmentEncounterAnchor`가 안정적인 `encounterId`와 액터/적 그룹 참조를 연결한다.
5. 영입 대화 정상 완료와 파티 영입 성공 뒤 `RecruitmentCommitted`를 기록하고, 획득 후 대화까지 정상 완료된 뒤에만 `Completed`를 기록한다.
6. 저장 중인 FlowGraph 토큰은 복원하지 않는다. 대신 조우 단계와 처치한 참가자 ID를 복원하고 FlowGraph가 현재 단계부터 다시 진입한다.

## 1. 목표와 플레이어 경험

### 1.1 기능 목표

- 특정 지역에 처음 진입하면 특정 액터가 적 몬스터들과 싸우는 장면이 실제 전투로 시작된다.
- 플레이어와 해당 액터는 서로 공격하거나 타기팅하지 않고 동일 팀으로 적을 상대한다.
- 적은 플레이어와 아군 액터 중 전술적으로 적절한 대상을 선택한다.
- 전투가 끝나면 남은 공격, 투사체, 어그로를 정리한 뒤 해당 액터와 대화한다.
- 대화를 정상 완료하면 지정 `CharacterActorType`이 로스터에 합류한다.
- 저장/로드, 지역 이탈, 플레이어 사망, 대화 중단, 중복 진입에도 영입 누락이나 중복 지급이 없어야 한다.
- 같은 구조로 다른 캐릭터 영입 조우를 데이터만 바꿔 반복 제작할 수 있어야 한다.

### 1.2 품질 목표

- 아군과 적을 외형·타기팅·HP UI·히트 피드백에서 즉시 구분할 수 있어야 한다.
- 아군 AI가 적을 모두 처치하거나 플레이어가 잠시 이탈하더라도 진행 불능이 생기지 않아야 한다.
- AI 대 AI 피격이 플레이어 전투만큼 전역 히트스톱·카메라 셰이크·진동을 남발하지 않아야 한다.
- 영입 장면이 “몬스터 사망 보상으로 캐릭터 획득”처럼 보이지 않고, 함께 싸운 뒤 관계가 성립하는 흐름으로 읽혀야 한다.

## 2. 비목표와 경계

- 파티에 합류한 캐릭터가 필드에서 독립 AI 동료로 계속 따라다니는 시스템은 이번 범위가 아니다.
- 전투 종료 후 `PartyManager`의 Roster/BattleOrder에 합류하며, 기존 캐릭터 스왑 구조를 그대로 사용한다.
- `BossAssistManager`의 보스 어시스트 영입과 섞지 않는다.
- `MonsterActor._recruitableAs`는 몬스터 사망 시 즉시 캐릭터를 해금하는 별도 경로다. 본 조우의 아군 액터에는 반드시 `None`을 유지한다.
- 신규 공격 모션이 필요하지 않으면 기존 MotionSet과 MotionKey를 재사용한다.
- P0에서는 전투 중 자유 저장을 막지 않는다.
- P0에서는 플레이어가 지역에 접근하기 전부터 전투를 영구 시뮬레이션하지 않는다. 활성화 볼륨을 시야선보다 앞에 배치해, 플레이어가 장면을 보는 시점에는 전투가 이미 시작된 것처럼 연출한다.

## 3. 현재 코드 기준선과 결손

| 현재 구현 | 재사용 가능 지점 | 이번 기능에서의 결손 |
| --- | --- | --- |
| `FlowGraphTriggerVolume` + `OnTriggerVolumeEntryNode` | 플레이어 지역 진입으로 그래프 발화 | 실행 토큰 자체는 저장되지 않으므로 도메인 단계 복원이 필요 |
| `FlowGraphManager.ApplyMapFlowGraphs` | 지역별 FlowGraph 등록 | DDOL 러너는 씬 오브젝트를 직접 참조할 수 없어 안정 ID 기반 앵커가 필요 |
| `MonsterGroupController.OnGroupDefeated` | 전멸 신호 및 기존 그룹 전술 | 조우 참가자별 저장 ID와 중간 처치 복원이 없음 |
| `PlayDialogueNode` | 추적 대화 실행 후 종료까지 대기 | 서비스/그래프 누락 또는 시작 거부 시 `Out`으로 통과하므로 영입 필수 대화에 안전하지 않음 |
| `IPartyService.UnlockCharacter` | Roster 추가, 빈 BattleOrder 자동 편입 | 반환 `bool`은 영입 성공이 아니라 “BattleOrder 자동 편입 성공”이므로 완료 판정에 사용할 수 없음 |
| `ActorAbilitySystem.MatchesTargetRelation` | GAS의 Self/Ally/Enemy 조건 | Player/Monster `ActorType`만으로 같은 진영을 하드코딩함 |
| `EnemyDetection` | 탐지, 시야, 추격 해제, 외부 타깃 주입 | 레이어의 첫 후보를 선택하고 진영 관계를 확인하지 않음 |
| `PlayerTargetingController` | 공격 보정 후보 검색 | 레이어와 `IDamageable`만 확인하므로 Monster 레이어의 아군을 잡을 수 있음 |
| `CombatHitDetector` | NonAlloc 광역 충돌 수집 | 레이어는 광역 후보 필터일 뿐이며 아군 피해 최종 차단이 없음 |
| `CombatResolutionPipeline` | 모든 피격의 중앙 진입점 | 공격자-피격자 관계 최종 가드가 없음 |
| `MonsterActor.ApplyResolvedHit` | 피격 후 공격자를 타깃으로 획득 | 공격자가 아군인지 확인하지 않음 |
| `MonsterActor.OnDeath` | 그룹 해제, 월드 상태, 퀘스트, 도감, 드랍, EXP, 골드 | 실제 처치 귀속과 무관하게 모든 보상을 지급하며 조우 수명 정책을 분리할 수 없음 |

따라서 지역 이벤트만 추가하면 아군 오인 타기팅, 아군 피해, 잘못된 보상, 대화 실패 후 자동 영입이 동시에 발생할 수 있다. **전투 진영 기반을 먼저 완성한 뒤 영입 조우를 얹는 순서가 필수**다.

## 4. 시스템 책임 분리

### 4.1 전투 3계층

```text
BT / EnemyDetection / Threat
  → 누구를 적으로 보고 언제 타깃을 바꿀지 판단

GAS / CombatRelation
  → 대상 관계 조건, 피해 허용, 비용·쿨다운·수치 판정

MotionSet
  → 애니메이션, 히트박스, 투사체, VFX/SFX의 실제 타이밍 실행
```

- FlowGraph는 액터에게 특정 공격을 명령하지 않는다. 조우 활성화·완료 대기·대화·영입만 조율한다.
- BT는 피해량이나 모션 시간을 소유하지 않는다.
- GAS Payload는 MotionSet 참조를 소유하지 않고 기존 `motionKey`만 전달한다.
- 진영 관계는 레이어와 분리한다. 레이어는 물리 쿼리의 broad phase로만 사용한다.

### 4.2 콘텐츠 흐름

```text
지역 볼륨
  → FlowGraph 진입
    → RecruitmentEncounterService 현재 단계 확인/활성화
      → 실제 전투 시스템이 전투 수행
    → 전투 완료 이벤트 대기
    → 필수 대화 정상 완료 대기
    → 파티 영입 + 조우 완료 원자적 커밋
```

TriggerComposer나 UnityEvent가 `전멸 → 대화 → 영입`을 직접 연결하지 않는다. 예상 밖 발화 순서와 저장/로드 재개를 FlowGraph와 조우 상태 머신이 흡수한다.

### 4.3 asmdef 배치

| 모듈/위치 | 신규 책임 |
| --- | --- |
| `UPlayGround.Data` | 진영/관계 SO, 조우 정의 SO, 단계·저장 DTO, 결과 enum |
| `UPlayGround.Contracts` | `ICombatAffiliationView`, `ICombatRelationService`, `IRecruitmentEncounterService`, 파티 영입 결과 계약 |
| `UPlayGround.Actor` | GameActor의 소속 구현, 관계 기반 탐지·피해·보상 귀속, 조우 참가자 생존 정책 |
| `UPlayGround.FlowGraph` | 서비스 계약만 호출하는 Resume/Wait/Prepare/Dialogue/Commit 노드와 컨텍스트 teardown |
| `Manager/Handler/Combat` | `GameCombatManager` 산하 관계·기여·피드백 handler |
| `Manager/Story` | `StoryManager`가 소유하는 순수 조우 상태 저장소와 저장 연결 |
| `Gameplay/Encounter` 통합 계층 | 씬 Anchor, Actor/FlowGraph/Story 서비스 바인딩 |
| 각 모듈 `Editor` | ID·관계·그래프 경로·데이터 무결성 검증 |

Data와 Contracts는 `MonsterActor`, `FlowGraphRunner`, Manager 구현을 참조하지 않는다. FlowGraph 노드는 `StoryManager.Instance`나 `PartyManager.Instance`를 직접 호출하지 않고 `Svc` 계약만 사용한다.

## 5. 전투 진영 모델

### 5.1 데이터

```csharp
public enum CombatRelation
{
    Ally,
    Neutral,
    Hostile,
}

[CreateAssetMenu(menuName = "UPlayGround/Combat/Faction")]
public sealed class CombatFactionSO : ScriptableObject
{
    public string factionId;
}

[CreateAssetMenu(menuName = "UPlayGround/Combat/Faction Relation Table")]
public sealed class CombatFactionRelationTableSO : ScriptableObject
{
    public CombatFactionSO defaultPlayerFaction;
    public CombatFactionSO defaultMonsterFaction;
    public CombatFactionSO defaultNeutralFaction;
    public List<CombatFactionRelationEntry> relations;
}
```

P0 기본 진영은 최소 다음 세 개다.

| factionId | 용도 |
| --- | --- |
| `PlayerParty` | 플레이어와 영입 조우의 임시 아군 |
| `WorldHostile` | 일반 적 몬스터 |
| `WorldNeutral` | 전투 비참가 액터 |

관계 표는 `(A, B)`와 `(B, A)`를 모두 검증한다. 비대칭 관계를 지원할 명확한 콘텐츠 요구가 생기기 전에는 대칭만 허용한다.

### 5.2 런타임 소속

`GameActor`는 다음 두 값을 분리해 갖는다.

- `BaseFaction`: ActorDefinition 또는 Profile이 지정하는 기본 진영
- `RuntimeFactionOverride`: 조우·매혹·강제 동맹처럼 제한된 수명의 덮어쓰기

런타임 덮어쓰기는 단순 setter가 아니라 소유 토큰을 반환하는 lease 방식으로 구현한다. 조우 종료·씬 이탈·컴포넌트 비활성화 시 해당 토큰만 해제해 다른 효과의 덮어쓰기를 망가뜨리지 않는다.

Contracts 모듈이 Actor 구현을 역참조하지 않도록 관계 서비스는 `GameActor`가 아니라 소비자 계약을 받는다.

```csharp
public interface ICombatAffiliationView
{
    int CombatantRuntimeId { get; }
    CombatFactionSO CombatFaction { get; }
    CombatCreditOwner CreditOwner { get; }
    bool IsCombatAvailable { get; }
}

public interface ICombatRelationService : IGameService
{
    CombatRelation GetRelation(ICombatAffiliationView source, ICombatAffiliationView target);
    bool CanTarget(ICombatAffiliationView source, ICombatAffiliationView target);
    bool CanDamage(
        ICombatAffiliationView source,
        ICombatAffiliationView target,
        CombatTargetPolicy policy);
    bool CanAssist(ICombatAffiliationView source, ICombatAffiliationView target);
    IDisposable OverrideFaction(
        ICombatAffiliationView actor,
        CombatFactionSO faction,
        object owner);
}
```

구현은 `GameCombatManager` 산하 `CombatRelationHandler`가 소유하고 `Svc` 계약으로 노출한다. Actor 모듈에서 새 Manager 싱글톤을 직접 참조하지 않는다.

### 5.3 마이그레이션 폴백

모든 기존 액터 데이터 이관이 끝나기 전까지만 중앙 폴백을 둔다.

- `ActorType.Player` → `PlayerParty`
- `ActorType.Monster` → `WorldHostile`
- 그 외 → `WorldNeutral`

폴백은 최초 1회 진단을 남기고, 액터 이름·프리팹 이름으로 분기하지 않는다. 데이터 이관 완료 후 폴백 0건을 검증하고 제거한다.

## 6. 타기팅, 피해, 그룹 전술

### 6.1 적 후보 탐색

`EnemyDetection`은 다음 순서로 후보를 선택한다.

1. LayerMask로 후보 Collider를 NonAlloc 수집한다.
2. `GameActor`를 해석하고 자기 자신·사망·비활성 대상을 제거한다.
3. `ICombatRelationService.CanTarget`이 false인 후보를 제거한다.
4. 시야, 추격 반경, 앵커 이탈 조건을 적용한다.
5. 위협 점수가 가장 높은 대상을 선택한다.

위협 점수는 `CombatTargetingProfileSO` 또는 기존 EnemyBehavior 데이터가 소유한다.

```text
위협 점수 = 받은 피해 기여 + 근접 압박 + 현재 타깃 유지 보너스 + Taunt - 거리 감쇠
```

- 플레이어라는 이유만으로 무조건 우선하지 않는다.
- 타깃 전환에는 최소 유지 시간과 전환 임계값을 둬 플레이어/아군 사이의 프레임 단위 핑퐁을 막는다.
- `AcquireTarget`, 그룹 경보 전파, 피격 반격 타깃 주입도 관계 검사를 반드시 거친다.

### 6.2 BT Blackboard 의미 이관

현재 플레이어 전용 의미를 가진 키는 타깃 의미로 일반화한다.

| 기존 의미 | 신규 의미 |
| --- | --- |
| `Memory.Player.*` | `Memory.Target.*` |
| 플레이어 거리 | 현재 적대 타깃 거리 |
| 플레이어 상태 | 현재 적대 타깃 상태 |

신규 Blackboard key는 Registry와 Generator를 통해 추가한다. Rules JSON에 등록되지 않은 문자열 키를 임의로 쓰지 않는다. 이관 기간에는 기존 키를 읽기 호환 alias로만 유지하고 신규 BT는 `Target` 키만 저작한다.

BT 원본은 `Assets/10.Datas/AI/BehaviorTree/SourceJson/`에서 수정하고 Generated `.asset`을 직접 편집하지 않는다.

### 6.3 공격 슬롯

`MonsterGroupController`의 근접/원거리/진형 슬롯은 한 개의 전역 타깃을 전제로 하지 않고 다음 키로 분리한다.

```text
(targetActorRuntimeId, attackType)
```

이렇게 해야 적 그룹이 플레이어와 임시 아군을 동시에 상대할 때 한 대상의 슬롯 점유가 다른 대상의 공격 템포를 잘못 막지 않는다. 전체 그룹 공격 상한이 필요하면 대상별 상한과 별도 총량 상한을 함께 둔다.

### 6.4 최종 피해 가드

관계 검사는 두 번 수행한다.

- `CombatHitDetector`: 후보 수집 단계의 조기 필터
- `CombatResolutionPipeline.Execute`: 실제 피해 적용 직전의 최종 권위 가드

투사체는 생성 당시 레이어만 믿지 않고 충돌 시점의 소유자와 대상 관계를 다시 판정한다. 반사된 투사체는 새 소유자의 진영을 사용한다.

Ability가 명시적으로 아군 지원·자해·중립 대상 효과를 허용할 때만 `CombatTargetPolicy`로 예외를 표현한다. 개별 액터 이름이나 “영입 조우 중” 조건을 피해 코드에 넣지 않는다.

### 6.5 플레이어 타기팅과 UI

- `PlayerTargetingController`, Finish Attack 후보, Ultimate 후보, Camera Lock-on은 `Hostile`만 선택한다.
- Monster 레이어에 있는 임시 아군은 공격 보정과 락온 후보에서 제외한다.
- 적 HP 바와 전투 노출 UI는 진영 관계를 확인한다. 임시 아군을 적 HP 바로 표시하지 않는다.
- P0에 신규 화면은 만들지 않는다. 기존 캐릭터 해금 알림과 대화 UI를 재사용한다.
- 별도 아군 표식이 필요하면 기존 `Assets/04.Images/` 또는 `Assets/ExternalAssets/UI/` 리소스를 사용한다. 트윈이 추가되면 모두 `SetUpdate(true)`를 적용한다.

## 7. 처치 귀속과 보상

### 7.1 관계와 보상 귀속 분리

같은 진영이라고 항상 플레이어 보상을 주는 것은 아니다. 다음 값을 별도로 둔다.

```csharp
public enum CombatCreditOwner
{
    None,
    PlayerParty,
    World,
}
```

- 일반 몬스터끼리 싸운 결과는 `World`이며 플레이어 EXP·골드·퀘스트·도감 처치를 주지 않는다.
- 활성화된 영입 조우의 임시 아군은 `PlayerParty` 귀속을 받아 해당 아군의 처치를 플레이어 팀 처치로 인정한다.
- 조우가 활성화되기 전 연출 전투가 추가될 경우 귀속은 `None`이고 실제 보상·사망 커밋을 막는다.

### 7.2 기여 기록

피격 대상별 `CombatContributionLedger`가 마지막 유효 공격자, 진영 귀속, 피해 기여, 시각을 기록한다. 사망 시 `CombatKillContext`를 한 번 생성해 다음 소비자가 같은 판정을 사용한다.

- Quest/Recipe 진행
- WorldState 또는 조우 수명 상태
- Monster Codex
- Drop/EXP/Gold
- 전투 텔레메트리

현재 `MonsterActor.OnDeath`의 무조건 호출을 위 컨텍스트 기반 정책으로 분리한다. 조우 아군의 기여는 `PlayerParty`로 인정하므로 함께 싸운 의미가 보상에서도 유지된다.

### 7.3 전투 피드백 예산

| 상황 | 전역 히트스톱 | 카메라/진동 | 로컬 VFX/SFX |
| --- | --- | --- | --- |
| 플레이어가 때리거나 맞음 | 기존 정책 | 기존 정책 | 사용 |
| 화면 안의 임시 아군이 적을 강하게 타격 | 축소 또는 없음 | 카메라 셰이크만 거리/가시성 제한 | 사용 |
| 화면 밖 AI 대 AI | 없음 | 없음 | 시뮬레이션 중요도에 따라 축소 |

AI 대 AI의 모든 타격이 전역 시간축을 멈추지 않도록 `CombatFeedbackContext`에서 플레이어 관여, 카메라 가시성, 거리를 판정한다.

## 8. 영입 조우 데이터

### 8.1 `RecruitmentEncounterDefinitionSO`

```csharp
public enum RecruitmentAllyFailurePolicy
{
    Incapacitate,
}

public enum RecruitmentEncounterResetScope
{
    PersistUntilNewGame,
    ResetOnCycle,
}

[CreateAssetMenu(menuName = "UPlayGround/Story/Recruitment Encounter")]
public sealed class RecruitmentEncounterDefinitionSO : ScriptableObject
{
    public string encounterId;
    public CharacterActorType recruitCharacter;
    public CombatFactionSO allyFaction;
    public RecruitmentAllyFailurePolicy allyFailurePolicy;
    public RecruitmentEncounterResetScope resetScope;
    public float postCombatSettleSeconds;
}
```

P0 규칙:

- `encounterId`는 저장 키이며 변경하지 않는다.
- `recruitCharacter`는 `None` 금지다.
- `allyFaction`은 `PlayerParty`와 동맹이어야 한다.
- `allyFailurePolicy`는 `Incapacitate`만 허용한다.
- 스토리 영입은 기본 `PersistUntilNewGame`이다. 사이클 정산으로 완료 상태나 관계를 지우지 않는다.
- 대화 그래프는 FlowGraph의 `PlayDialogueRequiredNode`가 소유한다. Data SO가 FlowGraph 런타임 에셋을 역참조하지 않는다.
- 공격 수치, 쿨다운, MotionKey는 이 SO에 중복 저장하지 않는다.

### 8.2 `RecruitmentEncounterAnchor`

씬 바인딩 컴포넌트다.

```csharp
public sealed class RecruitmentEncounterAnchor : MonoBehaviour
{
    [SerializeField] private RecruitmentEncounterDefinitionSO _definition;
    [SerializeField] private FlowGraphRunner _flowRunner;
    [SerializeField] private MonsterActor _allyActor;
    [SerializeField] private MonsterGroupController _hostileGroup;
    [SerializeField] private RecruitmentEncounterParticipant[] _participants;
    [SerializeField] private Transform _dialogueAnchor;
}
```

역할:

- 씬 활성화 시 `encounterId`로 서비스에 등록한다.
- 저장 단계에 따라 액터와 적 그룹을 `Dormant / Combat / Dialogue / Hidden` 상태로 복원한다.
- `CombatActive` 또는 `CombatResolved`를 로드했으면 수동 `Resume` 진입점을 발화한다.
- 비활성화 시 이벤트 구독, 진영 override lease, 전투 활성 lease를 정리한다.
- 적 전멸을 직접 대화나 영입으로 연결하지 않고 서비스 단계만 `CombatResolved`로 바꾼다.

### 8.3 참가자 안정 ID

```csharp
public enum RecruitmentEncounterRole
{
    RequiredAlly,
    Hostile,
}

public sealed class RecruitmentEncounterParticipant : MonoBehaviour
{
    [SerializeField] private string _participantId;
    [SerializeField] private RecruitmentEncounterRole _role;
}
```

- `participantId`는 encounter 내부에서 유일하고 저장 후 변경하지 않는다.
- 가능하면 `SceneEntityId.Guid`를 사용하고, 동적 생성 참가자는 조우 정의가 결정적으로 부여한다.
- 적 사망 즉시 해당 ID를 저장 상태의 `defeatedHostileIds`에 기록한다.
- 로드 시 이미 처치한 참가자는 그룹 활성화 전에 사망/비활성 복원해 보상 중복과 재등록을 막는다.
- 조우 참가자는 일반 `MonsterRespawnManager`의 재스폰 대상으로 등록하지 않는다.

## 9. 조우 상태 머신

### 9.1 영구 상태

```csharp
public enum RecruitmentEncounterPhase
{
    Dormant,
    CombatActive,
    CombatResolved,
    Completed = 3,
    RecruitmentCommitted = 4,
}

[Serializable]
public sealed class RecruitmentEncounterSaveEntry
{
    public string encounterId;
    public RecruitmentEncounterPhase phase;
    public List<string> defeatedHostileIds;
}
```

`DialogueRunning`은 저장 단계가 아니다. 영입 대화 중 저장/씬 전환/취소가 일어나면 `CombatResolved`, 획득 후 대화 중이면 `RecruitmentCommitted`로 복원해 해당 대화를 처음부터 안전하게 다시 시작한다. `Completed = 3`은 기존 저장 호환을 위해 유지한다.

```text
Dormant
  └─ 지역 진입 + 조우 활성화 성공 → CombatActive

CombatActive
  ├─ 적 참가자 사망 → defeatedHostileIds 누적
  └─ 모든 적 참가자 처치 → CombatResolved

CombatResolved
  ├─ 대화 시작/취소/실패 → CombatResolved 유지
  └─ 영입 대화 정상 완료 + 파티 영입 성공/이미 보유 → RecruitmentCommitted

RecruitmentCommitted
  ├─ 획득 후 대화 시작/취소/실패 → RecruitmentCommitted 유지
  └─ 획득 후 대화 정상 완료 → Completed

Completed
  └─ 영구 유지, 조우 액터와 적 그룹 미생성
```

### 9.2 저장 소유권

`StorySaveData`에 `recruitmentEncounters`를 추가하고 `StoryManager`가 내보내기/가져오기를 담당한다. `StoryManager`는 `IRecruitmentEncounterService`를 구현하되 상태 전이 규칙은 Unity 오브젝트를 모르는 `RecruitmentEncounterStateStore`에 위임한다.

FlowGraph의 `FlowProgressState`는 진단과 반복 정책에만 사용한다. 조우의 권위 상태로 사용하지 않는다. FlowGraph는 실행 중 토큰 위치를 저장하지 않기 때문이다.

### 9.3 저장/로드 복원 표

| 저장 단계 | 로드 결과 |
| --- | --- |
| `Dormant` | 적/아군 잠복, 지역 진입 대기 |
| `CombatActive`, 처치 0 | 아군과 전체 적을 전투 가능 상태로 복원 |
| `CombatActive`, 일부 처치 | 처치 ID는 숨기고 남은 참가자만 복원. 생존 참가자 HP/Poise/쿨다운은 P0에서 초기화 |
| `CombatResolved` | 적 미생성, 아군을 대화 위치·안전 상태로 복원, 대화 재개 |
| `RecruitmentCommitted` | 파티 해금 유지, 월드 아군을 대화 위치·안전 상태로 복원, 획득 후 대화 재개 |
| `Completed` | 적과 월드 아군 미생성, 로스터 상태 검증 |

생존 참가자의 전투 세부 스냅샷을 저장하지 않는 것은 명시적 P0 경계다. 이미 처치한 적과 지급된 보상은 되돌리지 않으며, 살아 있던 참가자만 새 전투 상태로 초기화한다.

### 9.4 사이클 경계

| 시점 | `PersistUntilNewGame` | `ResetOnCycle` |
| --- | --- | --- |
| 저장/로드 | 모든 단계와 처치 ID 유지 | 모든 단계와 처치 ID 유지 |
| 사이클 정산 | 유지 | `Completed`만 유지하고 미완료는 `Dormant`로 초기화 |
| 새 게임 | 전체 초기화 | 전체 초기화 |

스토리 플레이어블 영입 조우는 `PersistUntilNewGame`을 사용한다. 사이클 정산 때문에 함께 싸운 전투나 끝난 대화를 반복시키지 않는다.

## 10. 런타임 서비스 계약

### 10.1 `IRecruitmentEncounterService`

```csharp
public interface IRecruitmentEncounterRuntimePort
{
    string EncounterId { get; }
    RecruitmentEncounterDefinitionSO Definition { get; }
    string DialoguePartnerActorId { get; }
    bool TryApplyPhase(RecruitmentEncounterPhase phase);
    bool TryActivateCombat();
    bool TryPrepareDialogue();
}

public interface IRecruitmentDialogueAttempt : IDisposable
{
    string EncounterId { get; }
}

public interface IRecruitmentEncounterService : IGameService
{
    IDisposable RegisterRuntime(IRecruitmentEncounterRuntimePort runtime);
    RecruitmentEncounterPhase GetPhase(string encounterId);
    bool TryAcquireExecution(string encounterId, out IDisposable lease);
    RecruitmentEncounterStartResult TryStartOrResume(string encounterId);
    IDisposable ObservePhase(string encounterId, Action<RecruitmentEncounterPhase> observer);
    void RecordHostileDefeated(string encounterId, string participantId);
    bool TryBeginDialogueAttempt(
        string encounterId,
        out IRecruitmentDialogueAttempt attempt);
    void ConfirmDialogueCompleted(IRecruitmentDialogueAttempt attempt);
    RecruitmentCommitResult TryCommitRecruitment(
        string encounterId,
        IRecruitmentDialogueAttempt completedAttempt);
}
```

`RecruitmentEncounterAnchor`가 `IRecruitmentEncounterRuntimePort`를 구현한다. 서비스는 구체 Anchor, `MonsterActor`, `FlowGraphRunner`를 알지 않고 포트만 보며, 동일 `encounterId` 런타임이 두 개 등록되면 두 번째 등록을 거부한다. 런타임 미등록 상태의 Start/Resume도 실패한다.

필수 불변식:

- 알 수 없는 `encounterId`는 조용히 생성하지 않고 실패한다.
- 같은 참가자 사망 기록은 멱등이다.
- `CombatResolved` 이전에는 영입할 수 없다.
- 대화 정상 완료 증명 없이 영입을 커밋할 수 없다. 서비스가 발급한 대화 attempt를 `PlayDialogueRequiredNode`가 정상 종료 콜백에서만 완료 처리하고 Commit 노드에 전달한다.
- 파티 영입에 실패하면 `Completed`를 기록하지 않는다.
- 이미 로스터에 있는 캐릭터는 성공으로 취급하되 중복 해금 알림은 보내지 않는다.
- 실행 lease는 encounter당 하나만 허용하고 FlowContext 완료·취소 때 반드시 해제한다.
- 취소되거나 Dispose된 대화 attempt는 Commit에 사용할 수 없고 재사용할 수 없다.

### 10.2 파티 영입 결과 계약

현재 `UnlockCharacter`의 `bool` 의미를 바꾸지 않는다. 호환 API는 유지하고 다음 명시적 계약을 추가한다.

```csharp
public enum CharacterUnlockResult
{
    AddedToBattle,
    AddedToRoster,
    AlreadyOwned,
    InvalidCharacter,
    ServiceNotReady,
    MissingModel,
}

public interface IPartyService
{
    bool IsCharacterUnlocked(CharacterActorType type);
    CharacterUnlockResult EnsureCharacterUnlocked(CharacterActorType type);
}
```

- `AddedToBattle`, `AddedToRoster`, `AlreadyOwned`만 조우 완료 성공이다.
- BattleOrder가 가득 찬 경우 `AddedToRoster`이며 정상 영입이다.
- Player/Swap 서비스 초기화 전이면 `ServiceNotReady`로 재시도하고 콘텐츠 오류로 오인하지 않는다.
- `MissingModel`은 콘텐츠 오류다. 아군 액터를 숨기거나 완료 플래그를 쓰지 않고 재시도 가능한 `CombatResolved`를 유지한다.
- 기존 `UnlockCharacter`는 내부적으로 새 API를 호출한 뒤 “이번 호출에서 BattleOrder에 추가됨”만 반환해 기존 호출자를 보존한다.
- Commit은 파티 영입을 먼저 적용하고 성공 결과를 받은 뒤 조우를 `Completed`로 쓴다. 두 쓰기 사이에 중단되더라도 다음 재개에서 `AlreadyOwned`를 성공으로 받아 `Completed`를 복구하므로 영입 누락과 중복 알림이 없다.

## 11. FlowGraph 계약

### 11.1 신규 노드

| 노드 | 출력 | 책임 |
| --- | --- | --- |
| `ResumeRecruitmentEncounterNode` | `Combat`, `Dialogue`, `PostDialogue`, `Completed`, `Failed` | 현재 단계를 조회하고 Dormant면 전투 활성화 |
| `WaitRecruitmentCombatResolvedNode` | `Resolved`, `Failed` | 현재 단계 선확인 후 이벤트 구독, 전멸 race 방지 |
| `PrepareRecruitmentDialogueNode` | `Ready`, `Failed` | 타깃/Ability/슬롯/잔존 투사체 정리, 아군 대화 상태 전환 |
| `PlayDialogueRequiredNode` | `Completed`, `Rejected` | 서비스가 발급한 대화 attempt를 정상 종료 콜백에서만 완료하고 FlowContext에 저장 |
| `CommitRecruitmentEncounterNode` | `Completed`, `Failed` | FlowContext의 완료 attempt를 소비해 파티를 해금하고 `RecruitmentCommitted` 기록 |
| `PlayRecruitmentPostDialogueNode` | `Completed`, `Rejected` | 파티 해금 뒤 월드 영입 대상과 후속 대화 재생 |
| `FinalizeRecruitmentEncounterNode` | `Completed`, `Failed` | 후속 대화가 끝난 조우를 최종 완료 처리 |

`PlayDialogueRequiredNode`는 기존 `PlayDialogueNode`의 호환 동작을 바꾸지 않고 별도 추가한다. 대화 서비스 미등록, 그래프 누락, 시작 거부를 성공으로 통과시키지 않는다. 대화 서비스가 다른 대화를 재생 중이면 즉시 영입하지 않고 재시도 가능한 `Rejected`로 종료한다. 대화 attempt는 FlowContext teardown에도 등록해 러너 취소 시 무효화한다.

### 11.2 권장 그래프

```text
OnTriggerVolume(Enter, Always) 또는 Manual(Resume)
  → ResumeRecruitmentEncounter(encounterId)
      Combat
        → WaitRecruitmentCombatResolved(encounterId)
        → PrepareRecruitmentDialogue(encounterId)
      Dialogue
        → PrepareRecruitmentDialogue(encounterId)
      Completed
        → End

Prepare Ready
  → PlayDialogueRequired(dialogue, partnerActorId)
  → CommitRecruitmentEncounter(encounterId)
  → End
```

- 지역 진입 Entry는 `Always`로 두고 서비스의 실행 lease가 중복 컨텍스트를 막는다.
- 같은 씬에서 실행 중이면 두 번째 진입은 `Failed/AlreadyRunning`으로 종료한다.
- 실행이 취소되면 lease가 해제되어 다음 지역 진입이나 Anchor의 Resume 발화가 재시도할 수 있다.
- `Completed` 여부는 FlowGraph Entry 반복 횟수가 아니라 조우 상태로 판정한다.

현재 `FlowContext`에는 컨텍스트 수명 teardown 등록 계약이 없다. `ResumeRecruitmentEncounterNode`가 얻은 실행 lease를 후속 노드까지 유지하려면 다음 범용 기능을 FlowGraph 런타임에 추가한다.

```csharp
public void RegisterTeardown(IDisposable teardown);
```

`FlowGraphRunner`는 컨텍스트의 마지막 토큰이 끝났을 때와 `CancelAll`로 취소할 때 teardown을 정확히 한 번 Dispose한다. 조우 노드가 static 실행 플래그를 갖거나 서비스 lease를 영구 점유하는 방식은 금지한다.

### 11.3 전투 종료 정리

대화를 즉시 시작하지 않고 다음 순서를 지킨다.

1. `CombatResolved`를 먼저 저장 가능한 상태로 기록한다.
2. 적 그룹 공격 슬롯과 경보를 해제한다.
3. 아군의 현재 Ability를 취소하고 Detection 타깃을 비운다.
4. 조우 적이 소유한 잔존 투사체가 피해를 주지 못하게 무효화한다.
5. 쓰러진 아군이면 대화 가능한 최소 HP로 회복하고 안전 상태로 전환한다.
6. `postCombatSettleSeconds` 동안 마지막 사망·VFX를 정리한다.
7. 대화 파트너를 안정 ID로 다시 해석하고 대화를 시작한다.

연출 수치인 settle 시간은 `RecruitmentEncounterDefinitionSO`가 소유한다.

## 12. 임시 아군 액터 정책

### 12.1 런타임 형태

P0 영입 대상은 `MonsterActor` + `EnemyAIController` + 기존 상태 머신을 재사용한다. `PlayerActor`는 입력·스왑·파티 상태를 전제로 하므로 월드 AI 전투 액터로 직접 사용하지 않는다.

`RecruitmentEncounterParticipant`가 활성화 동안 다음 lease를 소유한다.

- `PlayerParty` 진영 override
- `PlayerParty` 처치 귀속 override
- 조우 전용 수명 정책
- 필수 아군 사망 정책
- 전투 활성/일시정지 lease

조우가 끝난 뒤 대화 완료까지 월드 아군을 유지하고, 파티 영입 커밋 성공 후에만 숨기거나 디졸브 퇴장한다. 플레이어 모델과 월드 아군이 동시에 남지 않게 한다.

### 12.2 사망과 진행 불능 방지

필수 아군은 영구 사망하지 않는다.

- HP가 0이 되는 피해를 받으면 `Incapacitated`로 전환한다.
- 공격·이동·피격 대상으로서 비활성화하고 적의 위협 테이블에서 제거한다.
- 적 전멸 후 대화 가능한 상태로 복귀한다.
- 플레이어가 전투에서 패배하면 체크포인트 복원 시 아군도 정상 전투 상태로 복귀한다.
- 아군 쓰러짐을 조우 실패나 영입 실패로 사용하지 않는다.

기존 `EnemyDeathState`와 `MonsterActor.OnDeath`를 실행한 뒤 되살리는 방식은 보상·월드 상태·그룹 전멸을 오염시키므로 금지한다. 최종 피해 적용 전에 조우 생존 정책이 사망을 무력화하고 전용 쓰러짐 상태로 전환해야 한다.

### 12.3 지역 이탈

활성 조우에서 플레이어가 전투 지역을 크게 벗어나면 다음을 적용한다.

- 아군과 남은 적의 시뮬레이션을 일시정지한다.
- 서로의 HP를 오프스크린에서 계속 깎지 않는다.
- 처치된 참가자 ID는 유지한다.
- 플레이어가 돌아오면 남은 전투를 재개한다.

이 정책은 아군이 화면 밖에서 조우를 끝내 대화가 유실되는 문제를 막는다. 거리 기준은 씬 Anchor의 전투 경계 또는 별도 Collider로 저작한다.

## 13. BT·GAS·Motion 데이터 저작

### 13.1 BT

- 기존 지상/비행 BT 구조를 재사용하되 현재 타깃이 Player라는 전제를 제거한다.
- 판단은 `Target` Blackboard 키와 관계 서비스가 제공한 적대 후보만 사용한다.
- 영입 대상 전용 행동 차이가 필요하면 공용 Rules JSON을 직접 훼손하지 않고 파생 Rules JSON을 만든다.
- `groups`, 명시적 `AbortMode`, 반응형 행동 우선순위를 유지한다.
- 생성 후 SourceJson 정적 검증과 Unity importer를 모두 통과해야 한다.

### 13.2 GAS

- 기존 플레이어용 공유 Ability 에셋을 `aiSelectable`로 직접 바꾸지 않는다.
- 영입 대상 AI용 파생 `AbilitySetSO`를 만들고, AI 선택 조건이 필요한 Ability만 안전 Fork한다.
- Fork된 Payload는 기존 HitPhase와 MotionKey를 유지한다.
- 실행 모션은 `ActorAnimationMotionSet.abilityMotions`가 해석한다.
- MotionKey 해석이 불가능한 기술은 임의 모션으로 폴백하지 않고 콘텐츠 오류로 차단한다.
- Ability/Variant/Payload/MotionSet 참조는 Ability Editor와 검증 도구로 생성·검증하며 YAML을 손으로 작성하지 않는다.

### 13.3 MotionSet

- 기존 Collision/VFX/SFX 타이밍을 재사용한다.
- 아군 전용 피해 축소를 MotionEvent에 넣지 않는다. 피해 수치는 GAS 파생 데이터에서 조정한다.
- 몬스터 대 몬스터에서도 텔레그래프, 히트 타이밍, 캔슬 창은 기존 저작 계약을 유지한다.

### 13.4 대화

- 실제 대사 원고를 작성하거나 수정할 때는 `STORY_PLOT_AUTHORING_GUIDE.md`를 먼저 적용한다.
- 플레이어 노출 문장에 FlowGraph, 파티 해금, 조우 단계 같은 개발 용어를 쓰지 않는다.
- 마지막 대사 노드가 정상 종료 콜백을 낸 뒤에만 대화 attempt를 완료한다.
- 선택지나 조건 분기는 Dialogue Graph가 소유하고, 대화 전후 전투·영입 순서는 FlowGraph가 소유한다.

## 14. 실패 처리

| 실패/예외 | 요구 동작 |
| --- | --- |
| 중복 지역 진입 | 기존 실행 lease 유지, 두 번째 흐름은 부수효과 없이 종료 |
| 등록되지 않은 `encounterId` | 오류 진단, 전투/대화/영입 모두 실행하지 않음 |
| 필수 아군 또는 적 그룹 누락 | 검증 오류, 조우 시작 거부 |
| 플레이어 사망 | 처치 ID 유지, 체크포인트에서 남은 전투 복원 |
| 아군 HP 0 | 사망 대신 쓰러짐, 진행 지속 |
| 지역 이탈 | 전투 일시정지, 복귀 시 재개 |
| 대화 서비스 사용 중 | 영입하지 않고 `CombatResolved` 유지, 재시도 |
| 대화 취소/씬 전환 | `CombatResolved` 유지, 다음 Resume에서 대화 재시작 |
| 대화 그래프 누락 | 영입 금지, 명시적 콘텐츠 오류 |
| 파티 BattleOrder 가득 참 | Roster에는 합류, `Completed` 성공 |
| 캐릭터 이미 보유 | 중복 알림 없이 `Completed` 성공 |
| 파티 서비스 초기화 전 | 영입 보류, 월드 아군 유지, 서비스 준비 뒤 재시도 |
| PlayerActor에 모델 누락 | 영입 실패, 월드 아군 유지, `Completed` 기록 금지 |
| 저장 데이터에 알 수 없는 참가자 ID | 경고 후 보존, 조용히 다른 적에 매핑하지 않음 |
| 조우 정의의 캐릭터 변경 | 기존 저장과 불일치 오류. 에셋 이름이나 현재 필드로 자동 치환하지 않음 |

## 15. 에디터 검증

기존 콘텐츠/FlowGraph 검증기에 다음 규칙을 추가한다.

- `encounterId` 공백·중복
- 정의와 Anchor의 `encounterId` 불일치
- 참가자 ID 공백·중복
- 필수 아군 정확히 1명
- 적 참가자 1명 이상
- 필수 아군의 `_recruitableAs`가 `None`이 아님
- 영입 캐릭터의 `PlayerSwapBehaviour` 모델 데이터 누락
- 아군/적 진영 관계가 Hostile이 아님
- 플레이어/아군 진영 관계가 Ally가 아님
- FlowGraph에 `PlayDialogueRequiredNode` 또는 `CommitRecruitmentEncounterNode` 누락
- `CommitRecruitmentEncounterNode`가 필수 대화 완료 경로 바깥에서 도달 가능
- Completed 이후 적/아군을 다시 활성화하는 경로
- 참가자에게 일반 필드 재스폰 정책이 동시에 설정됨
- AI 선택 Ability의 Payload/MotionKey/HitPhase 해석 실패
- BT 신규 Blackboard key 미등록

`RecruitmentEncounterAuthoringWindow`는 조우 정의, 표준 FlowGraph, 씬 Anchor·진입 볼륨·참가자 구성을 한 창에서 생성·연결한다. 기존 에셋의 `encounterId`와 GUID는 보존하고, 신규 에셋만 생성하며 씬 변경은 단일 Undo 그룹으로 적용한다. 전용 창과 통합 툴 런처는 같은 도구 ID `UPlayGround/게임플레이/흐름/영입 조우 저작`을 사용한다.

창과 Anchor 인스펙터는 같은 검증기를 사용하며 다음을 추가로 확인한다.

- 정의 `encounterId` 프로젝트 중복
- 진입 볼륨 Collider·Runner·Volume ID와 Graph Entry 일치
- PlayerParty–임시 아군 Ally, 임시 아군–적 Hostile 관계
- 모든 영입 조우 노드의 `encounterId` 일치
- 필수 대화 완료 뒤 Commit 도달 경로 및 대화 우회 Commit 경로 부재
- 참가자 비활성 저작과 일반 필드 재스폰 식별자 중복 가능성

## 16. 구현 순서

### Phase 1 — 전투 관계 기반 `[구현 완료]`

1. `CombatFactionSO`, 관계 표, 런타임 소속과 override lease 추가
2. `ICombatRelationService`와 `GameCombatManager` handler 연결
3. GAS TargetRelation, EnemyDetection, PlayerTargeting, Lock-on을 관계 기반으로 전환
4. `CombatResolutionPipeline` 최종 피해 가드 추가
5. 기존 Player/Monster 데이터 진영 이관과 폴백 진단

완료 조건: 플레이어와 임시 아군은 서로 타기팅·피해·락온하지 않고 양쪽 모두 적을 공격할 수 있다.

### Phase 2 — 몬스터 대 몬스터 전투 완성 `[구현 완료]`

1. 위협 기반 타깃 선택과 관계 검증된 외부 AcquireTarget
2. 타깃별 공격/진형 슬롯
3. 처치 기여·귀속과 보상 정책 분리
4. AI 대 AI 피드백 예산 적용
5. 지상/원거리/투사체/비행 공격의 관계 검증

완료 조건: 플레이어가 없는 샌드박스에서도 서로 적대인 두 몬스터 그룹이 정상 전투하고 플레이어 보상을 만들지 않는다.

### Phase 3 — 영입 조우 상태와 저장 `[구현 완료]`

1. `RecruitmentEncounterDefinitionSO`, Save DTO, 순수 상태 저장소 추가
2. `StoryManager`의 서비스/저장 계약 연결
3. `RecruitmentEncounterAnchor/Participant` 구현
4. 부분 처치 ID, 지역 이탈, 플레이어 사망, 로드 복원 구현
5. 필수 아군 쓰러짐 정책 구현

완료 조건: 전투 중 모든 저장 지점에서 진행 불능이나 보상 중복 없이 복원된다.

### Phase 4 — FlowGraph와 영입 커밋 `[구현 완료]`

1. 조우 Resume/Wait/Prepare/Commit 노드 추가
2. `PlayDialogueRequiredNode` 추가
3. `IPartyService` 명시적 영입 결과 계약 추가
4. 지역 진입 FlowGraph 저작
5. 대화 완료 후 월드 아군 퇴장과 기존 해금 알림 연결

완료 조건: 대화 정상 완료 전에는 어떤 경로에서도 파티가 해금되지 않는다.

### Phase 5 — 대표 콘텐츠 수직 슬라이스 `[테스트 데이터 저작 완료 / Play Mode 검증 대기]`

1. 화린 1명, 스켈레톤 적 2명, 지역 Anchor 1개를 `LakeOfLife`의 플레이어 시작 구역에 저작했다.
2. 세 액터 모두 검증된 기존 BT/AbilitySet/MotionSet을 재사용한다. 테스트만을 위한 파생 전투 데이터는 만들지 않았다.
3. Target 의미는 공용 관계 기반 탐지와 기존 BT가 사용하도록 연결했다.
4. 지역 진입, 공동 전투, 필수 대화, 파티 영입 경로를 단일 FlowGraph로 연결했다.
5. Unity Import·Play Mode 수직 슬라이스와 프로파일링은 남아 있다.

완료 조건: 실제 월드에서 지역 진입부터 파티 메뉴 확인까지 한 번에 플레이 가능하다.

## 17. 자동 테스트와 Play Mode 검증

### 17.1 EditMode

- 진영 관계 표 대칭성, 미지정 관계 폴백
- Runtime override lease 중첩/해제
- 아군 피해 차단과 적대 피해 허용
- 위협 점수 및 타깃 전환 히스테리시스
- 처치 귀속 `PlayerParty / World / None`
- 참가자 ID 중복과 단계 전이 거부
- 처치 ID 멱등 기록
- `Dormant → CombatActive → CombatResolved → Completed` 정상 전이
- 대화 증명 없는 Commit 거부
- `EnsureCharacterUnlocked`의 Roster/BattleOrder/AlreadyOwned/ServiceNotReady/MissingModel 결과
- Save DTO 구버전 기본값 복원

### 17.2 PlayMode 수직 슬라이스

1. 지역 진입 시 아군과 적이 서로 교전한다.
2. 플레이어 공격 보정과 Lock-on이 아군을 선택하지 않는다.
3. 적은 플레이어와 아군을 위협도에 따라 전환한다.
4. 아군 공격은 적에게 피해를 주고 플레이어에게는 피해를 주지 않는다.
5. 적 광역/투사체는 아군과 플레이어에게 피해를 줄 수 있다.
6. 아군이 마지막 적을 처치해도 전투 완료가 된다.
7. 일반 월드 몬스터끼리의 처치에는 플레이어 보상이 없다.
8. 전투 중 일부 처치 후 저장/로드하면 처치한 적이 부활하거나 보상을 다시 주지 않는다.
9. 아군이 쓰러져도 조우가 막히지 않고 전투 후 대화가 가능하다.
10. 전투 완료 직후 저장/로드하면 전투를 반복하지 않고 대화부터 재개한다.
11. 대화 취소/서비스 거부/씬 전환에서는 영입되지 않는다.
12. 대화 정상 완료 시 Roster에 1회만 추가된다.
13. BattleOrder가 가득 차도 Roster 영입 후 완료된다.
14. 이미 보유한 캐릭터로 로드해도 중복 없이 조우가 완료된다.
15. Completed 저장을 로드하면 적과 월드 아군이 다시 나타나지 않는다.

기존 FlowGraph EditMode 3개와 PlayMode 수직 슬라이스 3개도 함께 실행한다. Ability 데이터가 변경되면 EditMode 14개와 몬스터 통합 검증을, BT가 변경되면 SourceJson 정적 검증과 Unity Import를 수행한다.

## 18. 성능 예산

- 관계 판정은 faction ID 또는 런타임 인덱스 기반 O(1) 조회로 캐시한다.
- 탐지·위협 갱신은 기존 `AgentTickManager`를 사용하고 액터별 `Update`를 추가하지 않는다.
- Overlap은 NonAlloc 버퍼를 우선하고 포화 시에만 진단 후 임시 할당한다.
- 위협 테이블은 액터당 활성 후보 상한과 만료 시간을 둔다.
- AI 대 AI 오프스크린 전투는 지역 이탈 정책과 시뮬레이션 lease로 중단한다.
- 핫패스 진단은 `RuntimeLog.Trace/TraceThrottled`를 사용한다.

## 19. 완료 판정

다음 항목을 모두 충족해야 구현 완료다.

- Unity 컴파일 오류 0
- 관계 기반 타기팅·피해·GAS 조건 단위 테스트 통과
- 몬스터 대 몬스터 샌드박스 PlayMode 통과
- 지역 진입 → 공동전투 → 전멸 → 대화 → 파티 합류 수직 슬라이스 통과
- 전투 전/중/후/대화 중/완료 후 저장·로드 검증 통과
- 중복 EXP·골드·드랍·퀘스트·도감 처치 0
- 아군 오인 Lock-on·공격 보정·HP 바 0
- `_recruitableAs`와 BossAssist 영입 혼입 0
- Missing Script 0, managed reference/VFX 누락 0
- Player Build 오류 0
- Play Mode 서비스 경고·예외 0

## 20. P0 대표 저작 체크리스트

- [ ] 영입 대상 `CharacterActorType`과 Player 모델 존재
- [ ] 영입 대상 월드 액터 `_recruitableAs=None`
- [ ] `RecruitmentEncounterDefinitionSO`와 안정 `encounterId`
- [ ] Anchor의 필수 아군 1명, 적 참가자 1명 이상
- [ ] 모든 참가자의 안정 `participantId`
- [ ] 아군 `PlayerParty`, 적 `WorldHostile` 관계 검증
- [ ] 지역 활성화 볼륨이 플레이어 시야선보다 앞에 배치됨
- [ ] 영입 대상 AI AbilitySet과 MotionKey 해석 검증
- [ ] Target 의미 BT와 Blackboard Registry 검증
- [ ] 필수 대화 그래프와 partner actor ID 연결
- [ ] FlowGraph Resume/Combat/Dialogue/Commit 경로 연결
- [ ] 전투 후 아군 대화 위치와 잔존 투사체 정리 확인
- [ ] BattleOrder 만석·이미 보유·모델 누락 테스트
- [ ] 각 조우 단계 저장/로드 테스트

## 21. 2026-08-16 구현 및 검증 기록

### 21.1 구현된 공용 기반

- `CombatFactionSO`, `CombatFactionRelationTableSO`, `ICombatRelationService`와 런타임 override lease를 추가했다.
- 기본 `PlayerParty / WorldHostile / WorldNeutral` 진영 에셋과 `Resources/CombatFactionRelations` 관계 표를 제공한다.
- `EnemyDetection`, 플레이어 공격 보정, Lock-on, GAS `TargetRelation`, 최종 피해 파이프라인을 같은 관계 판정으로 통합했다.
- 적 위협도와 타깃 전환 히스테리시스, 타깃별 그룹 공격/진형 슬롯, 처치 귀속과 플레이어 보상 차단을 구현했다.
- `RecruitmentEncounterDefinitionSO`, 순수 상태 저장소, Save DTO, `StoryManager` 서비스를 추가했다.
- 참가자 부분 처치 복원, 필수 아군 행동불능, 적 그룹 잠복 복원, 대화 상태 전환, 완료 후 월드 액터 제거를 구현했다.
- FlowGraph에 Resume/Wait/Prepare/Play Dialogue Required/Commit 노드를 추가하고, 실행 lease와 대화 완료 증명을 컨텍스트 수명에 묶었다.
- `IPartyService.EnsureCharacterUnlocked`의 명시적 결과로 Roster 합류, 출전 편입, 이미 보유, 모델 누락을 구분한다.
- Anchor 인스펙터가 참가자 ID/역할/`recruitableAs`, 필수 참조, Manual Resume Entry, 필수 노드, 대화 우회 Commit 경로를 검증한다.

BT 데이터의 `Player` 명칭 조건과 Blackboard key는 기존 JSON/GUID 호환을 위해 유지했다. 런타임 관찰 대상은 전역 플레이어가 아니라 `EnemyDetection.CurrentTarget`이며, 현재 대상에 `PlayerBehaviorPredictor`가 없으면 예측 값은 `None/0`으로 폴백한다. 따라서 몬스터 타깃에서도 기본 거리·공격·리액션 분기는 동작하고 플레이어 전용 예측 분기만 비활성화된다.

GAS와 MotionSet은 기존 몬스터의 `AbilitySetSO → GameplayAbilitySO → Motion Payload → motionKey → ActorAnimationMotionSet` 연결을 그대로 재사용한다. 대표 콘텐츠도 화린와 스켈레톤 원본 프리팹의 검증된 연결을 사용하며, 테스트 전용 Ability/Motion을 임의 파생하거나 매핑하지 않았다.

### 21.2 검증 결과

| 검증 | 결과 |
| --- | --- |
| Unity 전체 스크립트 컴파일 | 전체 3회 오류 0. 마지막 Unity 재실행은 Licensing Client 연결 실패. 이후 Unity Bee Roslyn 응답 파일로 Data/Contracts/FlowGraph/Actor/Camera/Assembly-CSharp/Editor를 재컴파일해 오류 0 확인 |
| 관련 EditMode | `Content + Combat + FlowGraph` 57/57 통과. 이후 저장 정규화 테스트 1개를 추가했고 Combat/Content/FlowGraph 테스트 어셈블리 재컴파일 오류 0, 실행 재검증은 라이선스 문제로 대기 |
| BT SourceJson 정적 검증 | 22개 중 12개 통과. 기존 `abilityRole`을 검증 스크립트가 지원하지 않아 보스/휴머노이드 10개에서 기존 오류 149건 |
| Ability Unity EditMode | Licensing Client 재연결 단계에서 중단. Ability 테스트 어셈블리 Roslyn 재컴파일 오류 0, 이번 작업의 Ability/GAS 에셋 변경 없음 |
| `dotnet build` 보조 검증 | 생성 csproj의 외부 Unity/VFX/Animancer 참조 누락으로 비권위 실패. Unity 컴파일 결과를 기준으로 삼음 |
| diff 정합성 | 임시 테스트 로그/결과 파일 제거, 신규 GUID 충돌 검사 완료 |

### 21.3 LakeOfLife 대표 콘텐츠

플레이어 시작 위치에서 전방으로 짧게 이동해 전체 사이클을 확인할 수 있는 테스트 조우를 저작했다.

| 구분 | 적용 내용 |
| --- | --- |
| 씬 | `Assets/01.Scenes/Ingame/LakeOfLife.unity` |
| 재사용 프리팹 | `Assets/03.Prefabs/Test/RecruitmentEncounter_Test_HwarinRescue.prefab` |
| 조우 정의 | `Assets/10.Datas/Story/Test/RecruitmentEncounter_Test_HwarinRescue.asset` |
| FlowGraph | `Assets/10.Datas/Flow/Test/FLOW_Test_HwarinRescue.asset` |
| 필수 대화 | `Assets/10.Datas/Dialogue/Test/DLG_Test_HwarinRescue.asset` |
| 획득 후 대화 | `Assets/10.Datas/Dialogue/Test/DLG_Test_HwarinJoined.asset` |
| 영입 대상 | `Honoka`, 참가자 ID `honoka_ally`, Actor ID `TestRecruit_Honoka` |
| 적 그룹 | Skeleton Sword `skeleton_sword_a`, Skeleton Bow `skeleton_bow_b` |
| 저장 정책 | `PersistUntilNewGame` |

씬 루트는 `PlacementData_LakeOfLife`의 `Player_Main` 런타임 배치 위치 `(1118.3672, 51.818047, 391.17047)` 및 회전 identity에 놓았다. 지역 진입 볼륨은 로컬 전방 `z=3.5`, 화린는 `z=8`, 근접 적은 `z=10.5`, 원거리 적은 `z=12.5`에 배치했다. 플레이어가 약 2.25m 전진하면 진입 볼륨에 닿고, 적 그룹은 지역 진입 전까지 잠복한다. 참가자 세 명의 `_recruitableAs`는 모두 `None`이며, 영입은 대화 완료 뒤 `RecruitmentEncounter` 커밋만 담당한다. `CombatTest`의 기존 인스턴스는 제거해 테스트 진입점을 하나로 유지한다.

화린와 두 스켈레톤은 각 원본 프리팹의 `EnemyAIController`, `EnemyDetection`, `BehaviorTreeRunner`, `AbilitySetSO`, MotionSet 연결을 그대로 사용한다. 신규 BT/GAS/MotionSet 에셋은 만들거나 변경하지 않았다.

### 21.4 테스트 절차와 남은 검증

1. `LakeOfLife` 씬을 정상 게임 흐름 또는 직접 열고 Play Mode를 시작한다.
2. 플레이어 스폰 지점을 중심으로 한 진입 볼륨에서 조우가 자동 시작되는지 확인한다.
3. 화린와 한 팀으로 스켈레톤 둘을 처치한다.
4. 전투 정리 후 영입 대화 3개 노드가 끝까지 진행되는지 확인한다.
5. 화린가 실제 파티에 해금된 뒤 획득 후 대화 3개 노드가 이어지는지 확인한다.
6. 후속 대화 뒤 조우가 `Completed`가 되고 화린 영입 결과가 한 번만 반영되는지 확인한다.

실제 `AddedToRoster` 결과를 확인하려면 화린를 보유하지 않은 정상 새 게임/저장을 사용한다. 이미 화린를 보유한 저장에서는 의도대로 `AlreadyOwned` 멱등 경로와 조우 완료를 검증한다. 조우 완료 상태는 새 게임까지 유지되므로 같은 저장에서 다시 시험하려면 신규 저장으로 시작한다.

신규 에셋의 GUID, 프리팹 내부 fileID, 참가자 ID, FlowGraph 연결, 필수 대화에서 Commit까지의 단일 경로는 정적 검증을 통과했다. Unity batch 재실행은 Licensing Client 초기화 단계에서 정지해 Import와 Play Mode 검증은 수행하지 못했다. 최종 승인은 17.2절 수직 슬라이스와 Player Build 확인 뒤 내린다.

### 21.5 초기 진영 적용 순서 회귀 수정

`CombatTest` 직접 실행에서 화린가 플레이어를 공격하는 문제가 확인됐다. 씬 오브젝트의 `Start`가 비동기 `GameManager` 초기화보다 먼저 끝나면 Anchor가 `IRecruitmentEncounterService` 등록에 한 번 실패한 뒤 재시도하지 않았고, 활성 상태로 저작된 화린 BT가 기본 `WorldHostile` 진영으로 먼저 플레이어를 획득하는 것이 원인이었다.

Anchor는 플레이 시작 즉시 모든 참가자를 숨기고, 영입 조우 서비스가 등록될 때까지 제한된 초기화 코루틴으로 대기한 뒤 런타임을 등록한다. 필수 아군 활성화는 `PlayerParty` 진영 lease 발급과 기존 타깃 초기화를 AI 컴포넌트 활성화보다 먼저 수행한다. 참가자 프리팹은 비활성 상태로 저작하며 Anchor 인스펙터도 이를 검증한다. `Assembly-CSharp`/`Assembly-CSharp-Editor` 보조 컴파일은 오류 0을 확인했다. Unity batch 임포트도 종료 코드 0이며, 손으로 작성한 근사 회전값 때문에 발생했던 KCC 비단위 lossy scale 오류는 참가자 루트 회전을 identity로 정규화해 제거했다. 실제 공동 전투 Play Mode 재검증은 남아 있다.

### 21.6 LakeOfLife 테스트 위치 이관

대표 조우 인스턴스를 `CombatTest`에서 제거하고, 최초에는 `LakeOfLife`의 `player_spawn_pos` 기준 전방에 배치했다. 씬 YAML 정적 검증에서 조우 GUID는 `CombatTest` 0건, `LakeOfLife` 단일 인스턴스 13개 참조이며, 22,914개 Unity 문서 fileID의 중복은 0건이다. 프리팹 override 대상과 SceneRoots 연결도 모두 유효하다. Unity 6 배치 임포트는 기존 Unity 프로세스가 프로젝트의 `Temp/UnityLockfile`을 점유한 상태여서 종료 코드 1로 중단됐다. 점유 프로세스를 임의 종료하지 않았으며, 이후 열려 있던 에디터의 로그에서 씬 임포트 완료를 확인했다.

### 21.7 LakeOfLife 런타임 스폰 기준 수정

실제 Play Mode 로그에서 `Player_Main`은 정적 `CycleSpawnPoint`인 `player_spawn_pos` `(1091.08, 56.8, 418.08)`가 아니라 `PlacementData_LakeOfLife`의 Bake 레코드 `(1118.3672, 51.818047, 391.17047)`에서 먼저 생성됐다. 첫 생활 퀘스트 FlowGraph 준비 전에는 사이클 시작이 보류되어 정적 마커로 이동하지 않으므로, 기존 조우는 플레이 시작점에서 약 38m 떨어져 있었다. 조우 루트를 실제 런타임 스폰 레코드와 같은 위치·회전으로 옮겼다. 플레이어 콜라이더, `ActorType.Player`, Default–Player 물리 레이어 충돌은 정상이며, 해당 실행 로그에 영입 조우·FlowGraph 관련 예외는 없었다.

### 21.8 진입 트리거 초기화 레이스 수정

런타임 스폰 기준 수정 후 Play Mode 로그에서도 `Player_Main`은 지정 위치에 정상 생성됐지만, 영입 조우 등록·FlowGraph 발화 기록은 전혀 없었다. 기존 구조는 진입 볼륨이 씬 로드 즉시 활성화되는 반면 `IRecruitmentEncounterService`와 `ICombatRelationService`는 비동기 GameManager 초기화 뒤 준비된다. 플레이어가 준비 전에 볼륨을 밟으면 `ResumeRecruitmentEncounterNode` 실패가 연결되지 않은 `Failed` 포트로 소진되고, 이미 볼륨 안에 있는 플레이어에게 `OnTriggerEnter`가 다시 오지 않는 초기화 레이스가 있었다.

`FlowGraphTriggerVolume`은 라우팅이 닫힌 동안 겹친 콜라이더를 보관하고, 라우팅이 열리면 현재 겹친 대상의 Enter를 재생하도록 변경했다. `RecruitmentEncounterAnchor`는 두 필수 서비스와 런타임 등록이 끝날 때까지 진입 라우팅을 닫고, 저장 단계가 `Dormant`일 때만 연다. 조우 시작·대화 준비·완료 단계에서는 다시 닫아 중복 실행을 막는다. 등록·참가자 구성·진영 관계·노드 발화 실패는 원인을 포함한 경고 또는 오류로 남긴다.

테스트 볼륨은 런타임 스폰 위치 중심의 `10 × 3 × 10m`로 확대했다. 따라서 접근 방향이나 첫 이동 여부와 무관하게 준비 완료 직후 조우가 시작되어야 한다. `UPlayGround.FlowGraph`, `Assembly-CSharp`, `UPlayGround.FlowGraph.Tests` 보조 컴파일은 오류 0을 확인했다. 최신 수정본의 Unity Import 및 실제 공동 전투 Play Mode 재검증은 남아 있다.

### 21.9 KCC 진입 콜백 누락 보정

최신 Play Mode 로그는 `Player_Main` 스폰과 조우의 `Dormant` 런타임 등록까지 정상 완료됐지만, `FlowGraphTriggerVolume.OnTriggerEnter`와 전투 시작 기록은 남지 않았다. 계층 창에서 화린와 적 참가자가 비활성인 것은 이 시점의 정상 상태다. 진영 lease보다 먼저 AI를 켜면 화린가 플레이어를 적으로 인식하는 회귀가 재발하므로, 참가자는 조우 진입이 확정될 때까지 숨긴다.

KCC 액터가 볼륨 안에서 생성되거나 초기화되는 경우 Unity 물리 Trigger 콜백만으로 진입을 보장할 수 없으므로, Anchor 등록 완료 시 `IActorQueryService.Player`의 위치를 같은 `FlowGraphTriggerVolume`에 전달하는 단발 보정 경로를 추가했다. 볼륨은 소유 Collider의 월드 bounds와 `ActorType.Player` 필터를 검증한 뒤 기존 `OnTriggerVolumeEntryNode`를 발화한다. 별도 FlowGraph 실행, 액터 이름 분기, 프레임 폴링은 추가하지 않았으며 물리 콜백이 이미 처리된 경우에는 중복 발화하지 않는다.

보정 이후 기대 순서는 `런타임 준비 완료 → 플레이어 위치 진입 보정 → 전투 시작`이며, 전투 시작 직전에 화린에는 `PlayerParty`, 스켈레톤에는 `WorldHostile` 진영 lease가 적용되고 나서 참가자 GameObject가 활성화된다. `UPlayGround.FlowGraph`와 `Assembly-CSharp` 보조 컴파일은 오류 0을 확인했다. Unity 에디터가 기존 Play Mode를 종료한 상태여서 최신 수정본의 실제 공동 전투 확인은 재실행이 필요하다.

### 21.10 진입 판정의 물리 의존 제거

21.9 보정을 적용한 Play Mode 로그는 다음과 같았다.

```text
[RecruitmentEncounter] 'test.combat.honoka_rescue' 런타임 준비 완료 — 단계: Dormant
[FlowGraph] 볼륨 'test.honoka_rescue.entry' 위치 진입 검사 — 라우팅=True, 액터일치=True,
            액터위치=(1118.37, 51.83, 391.17), 볼륨중심=(0.00, 0.00, 0.00), 볼륨크기=(0.00, 0.00, 0.00)
```

라우팅과 액터 필터는 통과했고 플레이어 위치도 정확했지만 볼륨 bounds가 크기 0이었다. `Collider.bounds`는 콜라이더가 비활성이거나 물리 씬에 등록되기 전이면 크기 0을 반환하므로, 위치 폴백의 포함 판정이 성립할 수 없었다. 이는 21.9에서 `OnTriggerEnter`가 남지 않은 것과 같은 원인이며 KCC의 이동 방식과는 무관하다. 진입 실패를 두 개의 결함으로 나누어 본 앞선 진단은 틀렸다.

수정 내용은 다음과 같다.

- `FlowGraphTriggerVolume.ContainsWorldPoint`가 `Collider.bounds` 대신 콜라이더의 로컬 형상(`BoxCollider`/`SphereCollider`)과 트랜스폼으로 포함 여부를 판정한다. 물리 씬 등록 상태와 콜라이더 활성 여부에 좌우되지 않는다. 그 외 형상은 기존 bounds 근사로 폴백하며, 진입 볼륨은 Box/Sphere로 저작한다.
- `_volumeCollider`를 `ResolveVolumeCollider()`로 지연 확보한다. GameObject가 비활성이면 `Awake`가 실행되지 않아 직렬화가 비어 있는 참조를 메울 수 없었다.
- `TryRouteActorIfInside`가 `FlowVolumeRouteFailure`로 실패 사유를 반환한다. 볼륨은 로그를 직접 남기지 않고 통합 계층이 진단을 소유한다.
- 1회성 보정을 `RecruitmentEncounterAnchor`의 `IManagedTick` 주기 판정(기본 0.25초)으로 교체했다. 접근 방향, 스폰 위치, 진입 시점과 무관하게 `Dormant` 동안 진입을 계속 확인하고, 조우 시작·대화 준비·완료·비활성화 시점에 등록을 해제한다. 사유가 바뀔 때만 `RuntimeLog.Trace`를 남겨 주기 로그가 쌓이지 않게 한다.

`UPlayGround.FlowGraph`, `UPlayGround.FlowGraph.Editor`, `UPlayGround.FlowGraph.Tests`, `UPlayGround.FlowGraph.PlayModeTests`, `Assembly-CSharp` 보조 컴파일은 모두 오류 0이다. 실제 Play Mode 진입·공동 전투 검증은 아직 수행하지 않았다.

`FlowGraphTriggerVolume`은 현재 LakeOfLife의 두 영입 조우 프리팹에서 사용한다. 두 조우의 진입·재개 Play Mode 검증을 끝내기 전에는 다른 콘텐츠로 확대하지 않는다.

### 21.11 LakeOfLife 단일 스토리 2단계 영입

LakeOfLife의 30~40분 단일 스토리 수직 슬라이스를 위해 영입 조우를 `화린 → 리안리안` 순서로 연결했다. 두 정의는 모두 `PersistUntilNewGame`을 사용하며 사이클 정산에 의존하지 않는다. 리안리안 조우의 선행 ID는 `test.combat.honoka_rescue`이므로 화린의 획득 후 대화까지 완료되기 전에는 진입 라우팅이 열리지 않는다.

| 순서 | 조우 | 구조 대화 | 획득 후 대화 | 이후 현장 대화 |
| --- | --- | --- | --- | --- |
| 1 | `test.combat.honoka_rescue` | `DLG_Test_HonokaRescue` | `DLG_Test_HonokaPostRescue` | 붉은 표식 조사 시 `DLG_Test_HonokaJoined` |
| 2 | `test.combat.lianlian_rescue` | `DLG_Test_LianLianRescue` | `DLG_Test_LianLianPostRescue` | 남색 천 조사 시 `DLG_Test_LianLianJoined` |

저장 단계는 `Dormant → CombatActive → CombatResolved → RecruitmentCommitted → Completed`다. `RecruitmentCommitted` 진입 전에 실제 파티 해금을 적용한다. 이 단계에서 저장·씬 전환·대화 취소가 발생하면 월드 영입 대상을 대화 상태로 복원하고 획득 후 대화부터 재개한다. 후속 대화를 건너뛰고 완료 처리하지 않으므로, 대화 연출의 실행 여부와 영입 멱등성을 동시에 보장한다. 기존 저장의 `Completed = 3` 직렬화 값은 보존하기 위해 신규 enum 값은 뒤에 추가했다.

리안리안 대표 프리팹은 기존 검증 구조와 같은 스켈레톤 근접·원거리 조합을 재사용하고, 캐릭터 원본 프리팹의 BT/GAS/MotionSet 연결을 유지한다. 참가자 `_recruitableAs`는 모두 `None`이며 파티 해금은 영입 서비스만 수행한다. 씬 배치는 첫 조우에서 약 32m 진행한 위치 `(1118.3672, 55, 423.17047)`다.

신규 두 FlowGraph는 9개 노드와 10개 연결로 구성되며 `Commit → Play Post Dialogue → Finalize` 및 저장 복원용 `Resume.PostDialogue → Play Post Dialogue` 경로를 갖는다. Data/Contracts/FlowGraph/Assembly-CSharp/FlowGraph.Editor/Content.Tests 보조 컴파일은 오류 0, Unity 6000.3.21f1 배치 임포트와 전체 스크립트 컴파일은 종료 코드 0, 영입 상태 EditMode 테스트는 6/6 통과했다. 씬·프리팹·대화 에셋 Unity 문서 fileID 중복은 0건이며 핵심 에셋 10개의 임포트 전후 해시는 동일하다. 두 공동 전투, 파티 스왑, 두 대화 카메라, 저장·로드 재개는 Play Mode에서 확인해야 한다.

### 21.12 마을 대화·구조 조우의 퀘스트화

동료 획득 흐름을 플래그만으로 진행하던 구조에서 **네 개의 연속 퀘스트**로 바꿨다. 플레이어가 NPC의 목적지 설명을 따라가는 대신 현장에서 단서를 발견하고 다음 방향을 갱신하도록 구조와 추적을 분리했다.

| 순서 | questId | 이름 | 목표 |
| --- | --- | --- | --- |
| 1 | `quest_sub_lake_missing_villagers` | 돌아오지 않은 사람들 | 안내인 → 미아 → 조안 대화 3단계(`revealAfterObjectiveIds`로 순차 공개) |
| 2 | `quest_sub_lake_rescue_honoka` | 붉은 천을 따라 | 붉은 천 조사 → 화린 구조 |
| 3 | `quest_sub_lake_rescue_lianlian` | 호숫가로 이어진 흔적 | 리안리안 표식 조사 → 리안리안 구조 |
| 4 | `quest_sub_lake_follow_tracks` | 남겨진 흔적 | 남색 천 조사 → 끌린 자국 조사 → 신전의 세 사람 확인 |

- 1번은 `FLOW_LakeSearchQuestLine`의 `MapReady` 진입점이 연다. `autoAcceptOnNewGame`은 쓰지 않는다 — 그 경로는 타이틀에서 새 게임을 눌렀을 때(`QuestManager.ResetForNewGame`)만 돌아서, 씬을 직접 실행하거나 저장을 이어받으면 퀘스트가 없는 채로 대화만 끝나 진행이 멈춘다. 2~4번은 `autoComplete` + `autoAcceptNextQuestIds`로 이어진다.
- 세 대화의 완료 플래그(`lake.story.guide_briefed` / `mia_spoken` / `joan_request_accepted`)를 `FLOW_LakeSearchQuestLine`이 받아 `NotifyStoryEvent`로 옮긴다. 대화 데이터는 그대로 두고 흐름 그래프만 퀘스트를 소유한다.
- `MapReady`는 열기·복구·추적을 한 경로로 처리한다. 퀘스트가 아직 `Available`이면 열고, 이미 참인 세 플래그를 `CheckFlag → NotifyStoryEvent`로 다시 목표에 반영한 뒤, 현재 활성 퀘스트를 추적한다. 플래그 변화 진입점은 저장 복원에서 다시 울리지 않으므로 이 재반영이 없으면 이미 대화를 끝낸 세이브에서 진행 불능이 된다. 플래그 진입점도 같은 사슬로 들어가 경로를 하나로 유지한다.
- 조안 대화에 있던 `Action_StartSearchQuest`(레거시 `quest_sub_hunter_skeleton_patrol` 수락)는 제거했다. 해당 퀘스트 에셋은 ID·GUID를 보존한 채 `isContentEnabled: 0`으로 내렸다.
- 진행 단계에 맞는 퀘스트 하나만 추적하도록 `FLOW_LakeSearchQuestLine`이 `CheckQuestStatus → TrackQuest` 사슬을 갖는다. 이 사슬은 지역 진입·저장 복원 시 `MapReady` 진입점으로도 실행된다.

#### 퀘스트 마커

`StoryEvent` 목표는 위치를 스스로 알 수 없으므로 `QuestObjectiveData.markerLocationId`를 추가하고, 미니맵·전체 지도·월드 마커가 공유하는 `QuestObjectiveMarker.ResolveLocationId`로 해석 규칙을 한곳에 모았다. 마커 지점은 씬이 아니라 **데이터가 소유**한다.

| 지점 | 위치 ID | 소유 데이터 |
| --- | --- | --- |
| 안내인 | `npc_guide` | `NpcActorSO.questMarkerLocationId` |
| 미아 | `npc_mia` | 같음 (`NPC_Mia`, `NPC_CycleAnchor_Mia`) |
| 조안 | `npc_joan` | 같음 |
| 붉은 천 | `clue_red_cloth` | 화린 조우 프리팹의 `MinimapMarkerRegistrar` |
| 화린 구조 | `encounter_honoka_rescue` | `RecruitmentEncounterDefinitionSO.QuestMarkerLocationId` |
| 리안리안 표식 | `clue_lianlian_marker` | 리안리안 조우 프리팹의 `MinimapMarkerRegistrar` |
| 리안리안 구조 | `encounter_lianlian_rescue` | 같음 |
| 남색 천 | `clue_navy_cloth` | 묘령 조우 프리팹의 `MinimapMarkerRegistrar` |
| 끌린 자국 | `clue_drag_tracks` | 같음 |
| 묘령 대치 | `encounter_siuha_duel` | `RecruitmentEncounterDefinitionSO.QuestMarkerLocationId` |

NPC 쪽은 `NpcQuestMarkerInstaller`가 씬 준비 시 `MinimapMarkerRegistrar.Install`로 설치하고, 조우 쪽은 `RecruitmentEncounterAnchor`가 직접 설치한다. 지역 씬 파일이 저장소에 없으므로 씬 직렬화에 마커를 의존시키지 않는다.

미니맵의 퀘스트 마커 아이콘 색상이 네 지역 모두 알파 0이라 스프라이트가 있어도 보이지 않던 상태였다. 월드 마커와 같은 금색으로 맞췄다. 또한 미니맵이 비공개 목표까지 마커로 그리던 경로를 `GetVisibleObjectives`로 바꿔 지도·월드 마커와 안내 순서를 일치시켰다.

Play Mode 검증은 아직 수행하지 않았다. 확인할 것은 세 마을 대화와 네 현장 단서의 순차 마커 노출, 조안 대화 직후 다음 퀘스트 자동 수락과 추적 이동, 세 조우 완료 시 목표 반영, 각 단계 저장·로드 후 추적 복원이다.

#### 지역 FlowGraph가 적용되지 않던 배선 결함

퀘스트가 발급되지 않던 실제 원인은 퀘스트 데이터가 아니라 `SceneContext._mapConfigDB`가 세 인게임 씬 모두에서 비어 있던 것이었다. 지역 정보를 못 찾으면 `ApplyMapFlowGraphs(MapID, null)`이 조용히 통과해 **그 지역의 FlowGraph가 하나도 무장되지 않는다.** `FLOW_CycleStoryAnchor`도 같은 이유로 등록되지 않아 `CycleRunManager`가 `첫 생활 퀘스트 FlowGraph가 준비될 때까지 사이클 시작을 보류합니다` 경고만 반복하며 사이클이 시작되지 않는 상태였다.

씬 파일이 저장소에 없어 인스펙터 배선은 언제든 다시 비어질 수 있으므로 코드에서 해결했다.

- `SceneContext`가 `_mapConfigDB`와 `_regionInfoOverride`가 모두 비어 있으면 Addressable 키 `MapConfigDatabase`로 전역 데이터베이스를 보충한다.
- `MapID`가 있는데 지역 정보를 해석하지 못하면 `Debug.LogError`로 드러낸다. 침묵 통과를 남기지 않는다.

이 수정으로 그동안 잠들어 있던 지역 흐름(`FLOW_IngameBase`, `FLOW_CycleQuestLine`)이 함께 살아난다. 같이 살아났을 파란 리본 반복 앵커는 완료 불가 상태로 사이클 시작을 영구히 막고 있었으므로 레거시로 제거했다. 근거와 제거 범위는 [12_LOOP_ANCHOR_QUEST_SPEC.md](../cycle/12_LOOP_ANCHOR_QUEST_SPEC.md) 머리말을 따른다. 마을 대화 수색선이 이 지역의 단일 오프닝이다.

### 21.13 적대 영입 대상과의 대결형 합류 흐름

영입 대상이 아군으로 공동 전투에 참가하는 흐름만으로는 “처음에는 적이었지만 직접 부딪힌 뒤 관계가 바뀐다”는 인물 서사를 만들 수 없다. 기존 공동 전투형은 보존하고 `RecruitmentEncounterCombatMode`로 두 흐름을 데이터에서 선택한다.

| 모드 | 영입 대상 역할 | 필수 흐름 |
| --- | --- | --- |
| `CooperativeBattle` | `RequiredAlly` | 공동 전투 → 결과 대화 → 해금 |
| `HostileRecruitTarget` | `RecruitTarget` | 조우 대화 → 영입 대상과 전투 → 승리 → 해금 |

적대 결투형의 표준 FlowGraph는 다음 순서를 소유한다.

```text
지역 진입
  → Resume
  → Prepare Dialogue
  → Play Dialogue Required (CombatIntroduction)
  → Start Combat
  → Wait Combat Resolved
  → Prepare Dialogue
  → Commit After Victory
  → 획득 후 대화(선택)
  → Finalize
```

- `IntroductionPending` 단계를 저장한다. 전투 전 대화 도중 저장·씬 이탈·취소가 발생하면 해금이나 전투 시작으로 건너뛰지 않고 같은 대화부터 재개한다.
- `Start Combat`은 정상 종료된 조우 대화 증명을 소비해야만 `CombatActive`로 전환한다. 일반 `PlayDialogueNode`의 실패 통과 경로는 사용하지 않는다.
- `RecruitTarget`은 전투 중 플레이어에게 적대 진영으로 남는다. 임시 아군 진영을 덮어쓰지 않으며 락온·피해·AI 타기팅도 기존 진영 관계를 그대로 따른다.
- 영입 대상의 치명 피해는 사망 대신 체력 1에서 보호한다. `RecruitmentIncapacitationRule`이 `FinishAttack`이면 브레이크 노출을 열고 실제 피니시 공격이 적중한 순간에만 제압과 참가자 패배 ID를 기록한다. `AnyFatalDamage`는 기존 공동 전투·호환 데이터의 즉시 전투불능 규칙으로 유지한다.
- 제압된 대상은 사망 상태가 아니라 지속 `Incapacitated` 상태로 전환해 쓰러짐 모션을 유지한다. 처치 보상·월드 사망·`MonsterActor._recruitableAs` 경로는 실행하지 않는다.
- `Commit After Victory`는 적대 결투형에서만 허용한다. 공동 전투형은 기존처럼 결과 대화 증명 없이는 해금할 수 없다.
- 저장 복원 시 이미 패배한 영입 대상은 다시 싸우지 않고 결과 전환으로 이어진다. 미완료 추가 적이 있으면 그 적만 복원한다.
- 영입 조우 저작 창의 신규 기본값은 적대 결투형과 `FinishAttack` 제압이다. 공동 전투형을 고르면 기존 표준 그래프와 참가자 검증을 그대로 생성한다.

이 절은 런타임과 저작 구조만 확정한다. 특정 인물이 왜 주인공과 싸우고, 패배 뒤 왜 합류하는지는 인물 플롯에서 먼저 확정해야 하며, 이유 없이 “실력을 시험한다”는 대사는 만들지 않는다.

### 21.14 묘령 적대 영입 저작 샘플

기존 화린·리안리안 공동 전투형은 수정하지 않고, 적대 결투형의 실제 저작 샘플로 묘령 조우를 추가했다. 이 조우는 LakeOfLife 구조선의 별도 확장 샘플이며 메인 플롯의 확정 사건을 바꾸지 않는다.

| 항목 | 값 |
| --- | --- |
| 조우 ID | `test.combat.siuha_duel` |
| 선행 조우 | `test.combat.lianlian_rescue` 완료 |
| 영입 대상 | `CharacterActorType.Siuha` |
| 전투 모드 / 역할 | `HostileRecruitTarget` / `RecruitTarget` |
| 제압 조건 | 체력을 모두 소진하는 치명 피해 (`AnyFatalDamage`) |
| 전투 전 대화 | `DLG_Test_SiuhaConfrontation` |
| 합류 후 대화 | `DLG_Test_SiuhaJoined` |
| FlowGraph | `FLOW_Test_SiuhaDuel` |
| 배치용 프리팹 | `RecruitmentEncounter_Test_SiuhaDuel` |
| 저장 경계 | `PersistUntilNewGame` |

묘령는 실종된 세 사람을 문 안쪽에 숨겨 보호하고 있다. 앞서 세 사람을 끌고 온 자들이 아직 돌아올 수 있어 낯선 접근자를 모두 위협으로 간주하고 길을 막는다. 주인공은 묘령의 반응으로 세 사람이 안에 있음을 판단하고, 경고를 이해한 뒤에도 구조를 우선해 전진한다. 묘령는 체력이 소진되어 쓰러진 뒤에도 주인공이 공격을 멈춘 것을 보고 살의가 없었음을 확인한다. 정체 확인 퀴즈나 옷차림 암호로 오해가 풀리는 경로는 사용하지 않는다.

FlowGraph는 `IntroductionPending`부터 저장 복원되며, 전투 전 대화가 정상 종료되지 않으면 전투가 시작되지 않는다. 묘령에게 치명 피해가 들어오면 마지막 공격 종류와 관계없이 사망 대신 지속 쓰러짐 상태로 전환하고 즉시 승리 판정을 기록한다. 승리 커밋 뒤 파티 해금과 후속 대화를 거쳐 완료되며, 추가 적 없이 묘령 한 명만 전투 목표로 등록해 일대일 대결의 초점을 보존했다.

### 21.15 현장 발견 중심 수색과 주인공 대사 기능

2026-08-22 수색선의 정보 전달 방식을 `NPC 설명 → 목적지 이동`에서 `단서 제시 → 이동·조사 → 현장 판단 → 목적지 갱신`으로 바꿨다. 안내인은 미아만 연결하고, 미아는 조안에게 마지막 동선 확인을 맡긴다. 조안도 화린의 위치를 확정하지 않고 동쪽 풀숲의 붉은 천만 알려준다.

```text
안내인 → 미아 → 조안
  → 붉은 천 발견 → 화린 구조
  → 리안리안 표식 발견 → 리안리안 구조
  → 남색 천 발견 → 끌린 자국 발견
  → 신전의 묘령 대치 → 실종자 생존 확인
```

- 붉은 천·리안리안 표식·남색 천·끌린 자국은 각각 3D 월드 비주얼, 조사 콜라이더, `FlowGraphInteractable`, `FlowGraphTriggerVolume`, `MinimapMarkerRegistrar`를 가진다. 붉은 표식은 짧은 말뚝에 묶은 천 조각, 남색 천은 바닥에 접혀 떨어진 조각으로 표현한다. 대화 힌트용 UI 이미지를 월드 SpriteRenderer로 재사용하지 않는다.
- 조사 콜라이더는 `InteractableObject` 레이어에 두고, 물리 진입 자동 발화는 억제한다. 플레이어가 상호작용 버튼을 눌렀을 때만 `FlowGraphInteractable`이 같은 볼륨 진입점을 명시적으로 발화한다.
- 네 단서는 `FlowGraphInteractable`의 트리거 후 비활성화 옵션을 사용한다. 진입점 발화가 실패하면 월드에 남아 재시도할 수 있고, 성공한 경우에만 해당 단서 GameObject를 비활성화해 중복 조사와 남은 상호작용 프롬프트를 제거한다.
- 상호작용 프롬프트는 해당 퀘스트가 `Active`이고 조사 플래그가 아직 거짓일 때만 표시한다. 끌린 자국은 남색 천 조사 플래그도 요구하므로, 첫 단서를 보기 전 두 번째 단서와 상호작용하거나 선행 퀘스트 전에 현장을 지나가는 순서 이탈은 진행을 앞당기지 않는다.
- 단서 대화가 끝나면 `NotifyQuestStoryEvent → SetFlag` 순서로 기록한다. 저장이 두 노드 사이에 끊겨도 퀘스트 목표가 먼저 보존되며, 반대 순서에서 생길 수 있는 “플래그는 참인데 목표는 미완료라 재조사도 불가능한” 진행 불능을 막는다.
- 단서와 영입 조우의 플래그·조우 상태는 새 게임까지 유지한다. 사이클 정산으로 초기화하지 않으며, 비반복 퀘스트의 저장 경계와 일치시킨다.
- `FLOW_LakeSearchQuestLine.MapReady`는 네 번째 `quest_sub_lake_follow_tracks`까지 활성 상태를 검사해 저장·로드 뒤 현재 퀘스트 추적을 복구한다.

주인공 대사는 질문 수가 아니라 **문장 기능**을 기준으로 다듬었다. 마을에서는 마지막 동선을 정리하고 조사 순서를 결정하며, 구조 장면에서는 부상을 관찰하고 동행 위험을 판단한다. 현장에서는 단서 수·발자국 깊이·방향을 조합하고, 묘령 대치에서는 상대의 반응으로 실종자의 위치를 추론한 뒤 구조 우선 결정을 내린다. 장면마다 주인공이 `관찰 → 판단 → 행동 결정` 중 최소 두 단계를 담당하게 하고, NPC 설명을 꺼내기 위한 짧은 질문의 연쇄는 피한다.

화린·리안리안·묘령는 모두 주인공과 초면이라는 관계 전제를 대사에 반영한다. 화린 구조 직후에는 서로의 목적만 확인한 임시 동행으로 시작하고, 리안리안의 이름은 화린에게 처음 듣는다. 리안리안 구조 장면에서는 화린가 주인공에게 도움받았다고 말해 신뢰를 보증하며, 이후 공동 조사를 통해 지시가 아닌 역할 분담으로 관계를 진전시킨다. 묘령는 실종자를 보호하는 입장에서 낯선 무장 일행을 믿지 못하고, 주인공도 정체불명의 방해자를 그대로 믿지 못한다. 전투 뒤에야 살의가 없었음을 확인하고 이름과 목적을 공유한다. 관계 단계는 `낯선 구조자 → 목적이 같은 임시 동행 → 제3자의 신뢰 보증 → 공동 추적 → 상호 불신의 충돌 → 목적 일치` 순서를 따른다.

대화 13개는 시작·종료·다음 노드 참조와 node/file ID 중복 0, FlowGraph 4개는 노드 목록·managed reference·연결 대상 누락 0, 프리팹 3개는 로컬 fileID 중복·누락 0을 정적으로 확인했다. `FlowGraphInteractable`과 수동 라우팅 변경은 CLI 컴파일 오류 0을 확인했다. Unity Import, 실제 상호작용 아이콘·입력, 월드 단서 가시성, 단계별 저장·로드는 Play Mode 재검증이 남아 있다.

### 21.16 목적지 중심 오프닝과 동행 리듬

LakeOfLife 오프닝의 주인공 위치를 `의뢰서를 보고 온 수색자`에서 **호숫가 신전으로 향하는 외지인**으로 바꿨다. 신전에 가는 구체적인 이유는 아직 밝히지 않지만, 첫 조작부터 `호숫가의 신전` 퀘스트와 길 질문으로 목적지를 보여 준다. 미아·조안의 부탁과 화린·리안리안 구조는 모두 주인공의 원래 이동 방향에서 만나는 사건이다.

```text
호숫가 신전으로 향한다
  → 안내인에게 길과 숲의 위험을 듣는다
  → 같은 길에서 미아의 오빠와 조안이 찾는 두 사람을 알게 된다
  → 붉은 천을 확인하고 길을 막은 몬스터와 싸운다
  → 화린와 목적지가 겹쳐 임시 동행한다
  → 남색 매듭 표식을 직접 조사한다
  → 화린가 앞의 위험을 판단하고 주인공이 전투를 결정한다
  → 리안리안을 구조한다
  → 남은 흔적이 원래 목적지인 신전으로 이어진다
```

- 네 연속 퀘스트의 ID·GUID·플래그는 보존하고 표시 이름과 플레이어 문구만 현재 흐름에 맞췄다. 저장 복원과 `FLOW_LakeSearchQuestLine`의 단계 동기화 계약은 변하지 않는다.
- 화린 구조 후 대사는 감사·실종 확인·흔적 수색만 남긴다. 주인공은 별도의 구조 약속을 하지 않고 호수로 가는 길이 같다는 사실만 밝힌다.
- 리안리안의 표식은 기존 붉은 단서와 시각적으로 구분되는 남색 재질을 사용한다. 긴 매듭 끝은 프리팹 로컬 `+Z`, 즉 리안리안 조우 방향을 실제로 가리킨다. 조사 삽화도 기존 남색 천 이미지를 별도 액션으로 재사용한다.
- 리안리안 조우 진입 시 `lake.story.lianlian_danger_spotted`를 확인해 짧은 전투 전 대화를 한 번만 재생한다. 화린가 위험과 경로를 판단하고, 주인공이 우회 여부와 전투 결정을 맡은 뒤 적 그룹을 활성화한다.
- 리안리안 구조 뒤에는 화린가 주인공의 도움을 짧게 보증하고, 주인공은 부상과 이동 가능 여부만 확인한다. 재치 있는 초면 농담이나 자기 성격을 과시하는 대사는 두지 않는다.
- 끌린 자국 대화는 `발견 → 세 사람의 흔적 확인 → 신전 방향 확인 → 서두른다` 다섯 줄로 줄였다. 처음 제시한 신전이 후반 수색의 목적지로 다시 연결된다.

신규 런타임 코드는 추가하지 않았다. 기존 `PlayDialogueNode`, `CheckFlagNode`, `SetFlagNode`, 조사 상호작용, 퀘스트 단계 공개만으로 저작했으며, 실제 길이·전투 전 가시 거리·매듭 방향·대화 카메라는 Play Mode에서 확인해야 한다.
