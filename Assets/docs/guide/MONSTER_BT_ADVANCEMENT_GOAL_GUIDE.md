# 몬스터 BT 행동 패턴 고도화 목표 가이드

> 작성일: 2026-05-17  
> 코드 반영: 2026-05-17  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 적용 범위: `Assets/02.Scripts/AI/BehaviorTree/`, `Assets/10.Datas/AI/BehaviorTree/SourceJson/`, 몬스터 전투 AI 데이터

---

## 개요

이 문서는 몬스터 행동 패턴을 Behavior Tree 기반으로 다시 고도화하기 위한 목표 가이드다.

현재 기준 데이터는 `Assets/10.Datas/AI/BehaviorTree/SourceJson/Test/EnemyBehavior_Test_SearchPatrol_ReactivePhase.json` 1종이다. 앞으로의 방향은 이 데이터를 최소 기준 샘플로 삼고, 몬스터별 BT를 무작정 늘리기보다 **행동 판단 축**, **블랙보드 키**, **조건 노드**, **에디터 표시명 정책**을 먼저 정리하는 것이다.

핵심 목표는 다음과 같다.

- 몬스터 행동을 `거리`, `공격성`, `반응성`, `쿨다운`, `파티/무리 상황`, `플레이어 상태` 기준으로 판단한다.
- 피격 반응을 단순 Hit 상태 진입이 아니라 데미지 강도, Poise, 최근 피격 이력, 페이즈, 몬스터 성격에 따라 다르게 만든다.
- JSON과 BT 에셋 내부 키는 영문 고정 식별자를 유지한다.
- BT 에디터에서는 기획자가 읽기 쉬운 한글 표시명을 제공한다.
- 기존 상태 머신과 `EnemyCombat`을 버리지 않고, BT는 "어떤 상태/공격을 선택할지"를 결정하는 상위 의사결정 계층으로 사용한다.

---

## 1. 재출발 기준

### 1.1 남길 기준 데이터

현재 남은 JSON 1종을 기준 샘플로 유지한다.

| 항목 | 경로 |
|---|---|
| 기준 JSON | `Assets/10.Datas/AI/BehaviorTree/SourceJson/Test/EnemyBehavior_Test_SearchPatrol_ReactivePhase.json` |
| actorKind | `Ground` |
| 구조 | `groups` 기반 |
| 주요 기능 | 탐색/순찰, 페이즈 분기, 플레이어 공격 반응, 공격 가중 선택, 거리 조절 |

### 1.2 기본 그룹 구조

앞으로 새 몬스터 BT는 가능하면 다음 그룹 순서를 사용한다.

| 그룹 | 우선순위 | 역할 |
|---|---:|---|
| `01 Interrupt And Target Search` | 1000 | 경직/사망/공격 중 등 중단 불가 상태 유지, 타겟 없음 처리 |
| `02 Emergency Reactions` | 950 | 체력 위기, 최근 피격, 가드 브레이크, 긴급 회피 |
| `03 Phase Or Role Branch` | 900 | 페이즈, 역할, 전투 스타일별 분기 |
| `04 Combat Pressure` | 800 | 공격 선택, 카운터, 가드 브레이크, 회복 딜캐 |
| `05 Positioning` | 600 | 추격, 후퇴, 원형 이동, 측면 이동, 선호 거리 유지 |
| `06 Fallback` | 100 | 마지막 안전 행동 |

복잡한 BT에서는 top-level `rules`를 비워두고 `groups`만 사용한다.

---

## 2. 블랙보드 고도화 목표

블랙보드는 몬스터의 성격과 전투 판단 값을 담는다. 코드와 JSON에서는 영문 키를 사용하고, 에디터에서는 한글 표시명을 함께 보여준다.

| 영문 키 | 에디터 한글 표시명 | 타입 | 권장 범위 | 의미 |
|---|---|---|---:|---|
| `aggression` | 공격성 | Float | 0.0 ~ 1.0 | 공격 행동을 우선할 확률과 압박 유지 성향 |
| `reactionChance` | 반응 확률 | Float | 0.0 ~ 1.0 | 플레이어 행동에 반응 행동을 선택할 기본 확률 |
| `counterChance` | 반격 확률 | Float | 0.0 ~ 1.0 | 회피/가드 대신 카운터를 선택할 확률 |
| `dodgeChance` | 회피 확률 | Float | 0.0 ~ 1.0 | 근접 공격 반응에서 회피를 선택할 확률 |
| `punishRecoveryChance` | 후딜 응징 확률 | Float | 0.0 ~ 1.0 | 플레이어 회복/후딜 상태를 공격으로 응징할 확률 |
| `antiGuardChance` | 가드 대응 확률 | Float | 0.0 ~ 1.0 | 플레이어 가드 중 가드 브레이크/강공격을 선택할 확률 |
| `minRetreatCooldown` | 최소 후퇴 쿨다운 | Float | 0.0 ~ 10.0 | 후퇴 행동을 너무 자주 반복하지 않기 위한 최소 간격 |
| `maxComboPressureCount` | 최대 연속 압박 횟수 | Int | 0 ~ 10 | 연속 공격/압박 행동의 상한 |
| `preferredRange` | 선호 교전 거리 | Float | 0.0 ~ 20.0 | 몬스터가 유지하려는 기본 거리 |

### 2.1 기존 거리 키와의 관계

기존 키는 유지하되 의미를 다음처럼 분리한다.

| 기존 키 | 유지 의미 |
|---|---|
| `personalSpaceDistance` | 너무 가까워서 회피/후퇴가 필요한 거리 |
| `minCombatDistance` | 근접 전투가 시작되는 최소 거리 |
| `optimalCombatDistance` | 현재 공격 데이터 기준으로 공격 가능한 대표 거리 |
| `preferredRange` | 몬스터 성격상 유지하고 싶은 거리 |

`optimalCombatDistance`는 공격 가능 여부 판단에, `preferredRange`는 위치 선정 판단에 우선 사용한다.

---

## 3. 조건 노드 고도화 목표

아래 조건 노드는 새로 추가하거나 기존 노드의 별칭/표시명으로 정리할 대상이다.

| 조건 노드 | 에디터 한글 표시명 | 반환 기준 |
|---|---|---|
| `IsPlayerLowHealth` | 플레이어 체력 낮음 | 플레이어 현재 체력이 지정 비율 이하 |
| `IsSelfLowHealth` | 자신 체력 낮음 | 몬스터 현재 체력이 지정 비율 이하 |
| `HasLineOfSight` | 시야 확보 | 타겟까지 시야가 막히지 않음 |
| `IsTargetBehind` | 타겟이 후방에 있음 | 타겟이 몬스터 후방 각도 안에 있음 |
| `IsTargetCastingOrCharging` | 타겟이 시전/차지 중 | 플레이어가 차지, 시전, 긴 선딜 상태 |
| `RecentlyHitByPlayer` | 최근 피격됨 | 일정 시간 안에 플레이어에게 피격됨 |
| `RecentlyGuardBroken` | 최근 가드 깨짐 | 일정 시간 안에 가드 브레이크를 당함 |
| `AllyNearby` | 근처 아군 있음 | 지정 반경 안에 같은 그룹 몬스터가 있음 |
| `AllyCountNearby` | 근처 아군 수 조건 | 지정 반경 안의 아군 수가 비교 조건을 만족 |
| `HasAttackSlot` | 공격 슬롯 확보 | 그룹/타겟 기준 공격 슬롯을 점유 가능 |
| `CooldownReady` | 쿨다운 준비됨 | 지정 키 또는 행동 쿨다운이 완료됨 |

### 3.1 노드 설계 원칙

- 조건 노드는 최대한 부작용 없이 `true/false`만 반환한다.
- 타이머, 최근 피격, 최근 가드 브레이크 같은 값은 BT 노드가 직접 계산하지 않고 `EnemyTacticalMemory` 또는 블랙보드 동기화 서비스가 갱신한다.
- `HasAttackSlot`은 공격 실행 직전의 최종 게이트로 사용한다.
- `CooldownReady`는 후퇴, 카운터, 강공격처럼 반복되면 어색한 행동에 우선 적용한다.

---

## 4. 에디터 한글 표시명 정책

### 4.1 원칙

BT 런타임 식별자는 영문을 유지한다.

```json
{ "condition": "RecentlyHitByPlayer" }
```

에디터 표시만 한글로 변환한다.

```text
최근 피격됨
RecentlyHitByPlayer
```

이렇게 분리하는 이유는 다음과 같다.

- JSON, 코드, 저장 데이터의 안정성을 유지한다.
- 한글 표시명 변경이 런타임 호환성을 깨지 않는다.
- 검색과 디버깅에서는 영문 키를 계속 사용할 수 있다.
- 에디터에서는 기획 의도를 빠르게 읽을 수 있다.

### 4.2 표시 형식

노드 카드에서는 한글을 1순위로 보여주고, 영문 식별자는 보조 텍스트로 보여준다.

```text
최근 피격됨
Condition · RecentlyHitByPlayer
```

블랙보드에서는 한글 표시명과 영문 키를 같이 보여준다.

```text
공격성 (aggression)
반응 확률 (reactionChance)
선호 교전 거리 (preferredRange)
```

인스펙터 필드와 드롭다운에서도 같은 형식을 사용한다.

```text
후딜 응징 확률 (punishRecoveryChance)
쿨다운 준비됨 (CooldownReady)
```

### 4.3 구현 방향

에디터 전용 표시명 레지스트리를 추가한다.

권장 파일:

```text
Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeDisplayNameRegistry.cs
```

역할:

- 블랙보드 키 표시명 변환
- 조건 노드명 표시명 변환
- 액션 노드명 표시명 변환
- 상태명/공격 카테고리 표시명 확장 여지 제공

예상 API:

```csharp
public static class BehaviorTreeDisplayNameRegistry
{
    public static string GetBlackboardLabel(string key);
    public static string GetConditionLabel(string conditionName);
    public static string GetActionLabel(string actionName);
    public static string FormatWithRawName(string displayName, string rawName);
}
```

적용 후보:

| 파일 | 적용 지점 |
|---|---|
| `BehaviorTreeNodeView.cs` | 노드 카드 제목과 카테고리 보조 텍스트 |
| `BehaviorTreeNodeSearchWindow.cs` | 노드 생성 검색 목록 |
| `BehaviorTreeInspectorView.cs` | 선택 노드 인스펙터 |
| `BehaviorTreeBlackboardView.cs` | 블랙보드 키 목록 |
| `BlackboardKeySelectorDrawer.cs` | 블랙보드 키 드롭다운 |
| `MonsterBehaviorTreeJsonImporter.cs` | JSON rule/action 이름을 생성 노드 DisplayName에 반영할 때 |

### 4.4 표시명 매핑 초안

#### 블랙보드

| 영문 키 | 한글 표시명 |
|---|---|
| `aggression` | 공격성 |
| `reactionChance` | 반응 확률 |
| `counterChance` | 반격 확률 |
| `dodgeChance` | 회피 확률 |
| `punishRecoveryChance` | 후딜 응징 확률 |
| `antiGuardChance` | 가드 대응 확률 |
| `minRetreatCooldown` | 최소 후퇴 쿨다운 |
| `maxComboPressureCount` | 최대 연속 압박 횟수 |
| `preferredRange` | 선호 교전 거리 |

#### 조건 노드

| 영문 노드 | 한글 표시명 |
|---|---|
| `IsPlayerLowHealth` | 플레이어 체력 낮음 |
| `IsSelfLowHealth` | 자신 체력 낮음 |
| `HasLineOfSight` | 시야 확보 |
| `IsTargetBehind` | 타겟이 후방에 있음 |
| `IsTargetCastingOrCharging` | 타겟이 시전/차지 중 |
| `RecentlyHitByPlayer` | 최근 피격됨 |
| `RecentlyGuardBroken` | 최근 가드 깨짐 |
| `AllyNearby` | 근처 아군 있음 |
| `AllyCountNearby` | 근처 아군 수 조건 |
| `HasAttackSlot` | 공격 슬롯 확보 |
| `CooldownReady` | 쿨다운 준비됨 |

---

## 5. 행동 패턴 설계 목표

### 5.1 근접 반응형 몬스터

목표: 플레이어 공격에 반응하면서 압박을 유지하는 기본형.

주요 키:

| 키 | 권장값 |
|---|---:|
| `aggression` | 0.55 |
| `reactionChance` | 0.45 |
| `counterChance` | 0.20 |
| `dodgeChance` | 0.30 |
| `preferredRange` | 2.5 |

주요 룰:

- 플레이어가 공격 중이고 근거리이면 가드/회피/후퇴를 가중 선택한다.
- 플레이어가 후딜이면 `punishRecoveryChance`에 따라 공격한다.
- 공격 슬롯이 없으면 공격하지 않고 `Circle` 또는 `Flank`로 이동한다.

### 5.2 방어형 몬스터

목표: 무리 앞라인에서 공격 슬롯을 점유하고 플레이어 접근을 막는다.

주요 키:

| 키 | 권장값 |
|---|---:|
| `aggression` | 0.40 |
| `reactionChance` | 0.60 |
| `counterChance` | 0.35 |
| `dodgeChance` | 0.10 |
| `antiGuardChance` | 0.50 |

주요 룰:

- 플레이어 공격에는 회피보다 `Guard`와 `Counter`를 우선한다.
- 플레이어가 가드 중이면 강공격 또는 가드 브레이크 공격을 선택한다.
- 자신 체력이 낮으면 후퇴보다 방어 유지 또는 아군 근처 이동을 우선한다.

### 5.3 원거리 견제형 몬스터

목표: 선호 거리를 유지하면서 플레이어의 접근과 회피 습관을 압박한다.

주요 키:

| 키 | 권장값 |
|---|---:|
| `aggression` | 0.35 |
| `reactionChance` | 0.50 |
| `dodgeChance` | 0.45 |
| `minRetreatCooldown` | 2.0 |
| `preferredRange` | 7.0 |

주요 룰:

- `preferredRange`보다 가까우면 후퇴 또는 측면 이동한다.
- 시야가 확보될 때만 원거리 공격을 실행한다.
- 시야가 끊기면 추격보다 재위치 선정 행동을 우선한다.

### 5.4 보스형 몬스터

목표: 페이즈마다 행동 철학이 바뀌는 전투를 만든다.

예시:

| 페이즈 | 행동 철학 |
|---|---|
| Phase 0 | 탐색, 거리 유지, 기본 공격 중심 |
| Phase 1 | 플레이어 공격 반응과 카운터 증가 |
| Phase 2 | 후딜 응징, 가드 대응, 연속 압박 증가 |
| Phase 3 | 후퇴 감소, 공격성 증가, 위험 패턴 빈도 증가 |

보스는 단순히 공격 확률만 올리지 않고, `aggression`, `reactionChance`, `counterChance`, `maxComboPressureCount` 값을 페이즈별로 바꾸는 것을 목표로 한다.

---

## 6. Hit Reaction 고도화 목표

Hit Reaction은 플레이어의 공격이 몬스터에게 닿은 뒤 어떤 반응을 보일지 결정하는 계층이다. 목표는 모든 공격을 같은 Hit 상태로 처리하지 않고, 몬스터의 내구도와 성격, 공격의 위력, 최근 피격 상황에 따라 다른 결과를 만드는 것이다.

### 6.1 핵심 방향

현재 프로젝트에는 `AttackReactionType`과 `PoiseStat` 계열이 있으므로, Hit Reaction은 다음 순서로 판단하는 것이 좋다.

```text
공격 데이터의 AttackReactionType
→ 몬스터 Poise / 슈퍼아머 / 현재 상태
→ 최근 피격 이력과 콤보 누적
→ 체력/페이즈/성격 블랙보드
→ 최종 리액션 상태 또는 BT 메모리 갱신
```

BT는 "피격 순간의 물리 반응"을 직접 실행하기보다, 피격 이후의 의사결정에 필요한 기억을 남기는 역할을 맡는다.

예시:

- 약한 공격을 맞음: 경직 없이 `RecentlyHitByPlayer`만 갱신
- Poise가 깨짐: `Hit`, `HeavyHit`, `KnockBack`, `Knockdown` 중 하나로 전환
- 보스 슈퍼아머 중 피격: 상태는 유지하지만 `counterChance` 또는 보복 룰 가중치 증가
- 최근 연속 피격: 회피/후퇴/가드/분노 페이즈 전환 후보 증가

### 6.2 리액션 등급

`AttackReactionType`은 전투 데이터의 의도이고, 실제 몬스터 반응은 Poise와 상태를 합쳐 최종 결정한다.

| 리액션 등급 | 후보 타입 | 목표 반응 |
|---|---|---|
| 없음/흡수 | `Light` | 상태 유지, 피격 플래시, 최근 피격 메모리만 갱신 |
| 약경직 | `Hit` | 짧은 Hit 상태, 즉시 복귀 가능 |
| 강경직 | `Heavy`, `Stun` | 긴 Hit 상태, 플레이어의 후속 공격 기회 제공 |
| 밀림 | `KnockBack`, `Pull` | 위치 변화 포함, 거리 재계산 필요 |
| 공중/다운 | `Airborne`, `Knockdown` | 긴 무력화, 후속 행동 제한 |
| 잡기 | `Grab` | 별도 잡기 대응 흐름, 보스/대형 몬스터는 면역 가능 |

### 6.3 몬스터 타입별 차별화

| 몬스터 타입 | Hit Reaction 방향 |
|---|---|
| 소형 일반몹 | Poise 낮음, 경직과 넉백이 잘 발생 |
| 중형 전투몹 | 약공격은 버티고 강공격/스킬에만 큰 반응 |
| 방어형 몬스터 | 정면 피격은 버티고 측면/후방 피격에 취약 |
| 암살형 몬스터 | 피격 후 후퇴/회피 확률 높음 |
| 원거리 몬스터 | 피격 후 `preferredRange` 회복을 우선 |
| 보스 | 페이즈별 슈퍼아머 구간, Poise 파괴 시 짧은 보상 창 제공 |

### 6.4 추가 블랙보드 후보

Hit Reaction 고도화에 필요한 키는 기존 목표 키와 별도로 다음을 고려한다.

| 영문 키 | 에디터 한글 표시명 | 타입 | 의미 |
|---|---|---|---|
| `recentHitCount` | 최근 피격 횟수 | Int | 짧은 시간 안에 누적된 피격 횟수 |
| `lastHitReactionType` | 마지막 피격 반응 타입 | String/Enum | 마지막으로 받은 `AttackReactionType` |
| `poiseRatio` | 강인도 비율 | Float | 현재 Poise / 최대 Poise |
| `isPoiseBroken` | 강인도 붕괴됨 | Bool | 이번 피격 또는 최근 누적으로 Poise가 깨졌는지 |
| `hitReactionLockTime` | 피격 반응 잠금 시간 | Float | 리액션 중 다른 행동으로 덮어쓰지 않을 시간 |
| `revengeChance` | 보복 확률 | Float | 피격 후 카운터/돌진으로 응수할 확률 |

현재 구현 상태:

| 키 | 상태 |
|---|---|
| `recentHitCount` | `EnemyTacticalMemory`와 `SyncEnemyMemoryService`로 런타임 동기화 |
| `lastHitReactionType` | 마지막 `AttackReactionType` 문자열로 동기화 |
| `poiseRatio` | `PoiseStat.PoisePercent` 기준 동기화 |
| `isPoiseBroken` | `PoiseStat.IsPoiseBroken` 기준 동기화 |
| `revengeChance` | JSON/Blackboard 튜닝값으로 추가 |
| `hitReactionLockTime` | Blackboard 키만 추가, 실제 잠금 정책은 후속 구현 |

### 6.5 추가 조건 노드 후보

| 조건 노드 | 에디터 한글 표시명 | 반환 기준 |
|---|---|---|
| `WasLastHitHeavy` | 마지막 피격이 강함 | 마지막 피격이 `Heavy`, `KnockBack`, `Stun` 계열 |
| `IsPoiseBroken` | 강인도 붕괴됨 | 현재 Poise가 붕괴 상태 |
| `RecentHitCountGreaterOrEqual` | 최근 피격 횟수 이상 | 최근 피격 횟수가 지정 값 이상 |
| `CanIgnoreLightHit` | 약경직 무시 가능 | 현재 상태/Poise/슈퍼아머로 약한 경직 무시 가능 |
| `CanRevengeAfterHit` | 피격 후 보복 가능 | 피격 후 카운터 쿨다운과 상태 조건이 만족 |
| `IsHitFromBehind` | 후방 피격됨 | 마지막 피격 방향이 후방 판정 |

현재 구현된 조건:

| 조건 노드 | 상태 |
|---|---|
| `WasLastHitHeavy` | 구현 완료 |
| `IsPoiseBroken` | 구현 완료 |
| `RecentHitCountGreaterOrEqual` | 구현 완료 |
| `CanIgnoreLightHit` | 구현 완료 |
| `CanRevengeAfterHit` | 구현 완료 |
| `IsHitFromBehind` | 후보 유지 |

### 6.6 BT와 상태 머신의 역할 분리

Hit Reaction 자체는 상태 머신과 전투 컴포넌트가 처리한다.

| 계층 | 책임 |
|---|---|
| `EnemyCombat` | 데미지, 공격 타입, HitPhase, Poise 감소 계산 |
| `PoiseStat` | 강인도 누적, 붕괴 여부, 회복 타이밍 |
| `MonsterActor` / `EnemyMovementController` | 최종 Hit/KnockBack/Death 상태 전환 |
| `EnemyTacticalMemory` | 최근 피격 횟수, 마지막 피격 타입, 피격 방향 저장 |
| `BehaviorTree` | 피격 이후 후퇴/보복/방어/페이즈 분기 선택 |

BT가 직접 매 피격을 판정하면 책임이 섞인다. BT는 동기화된 블랙보드와 메모리를 보고 "다음 행동"을 고르는 쪽이 안정적이다.

### 6.7 Hit Reaction 룰 예시

```json
{
    "name": "RetreatWhenPoiseBroken",
    "priority": 940,
    "when": [
        { "condition": "HasTarget" },
        { "condition": "IsPoiseBroken" },
        { "condition": "CooldownReady", "value": "PoiseBreakReaction" }
    ],
    "select": "WeightedRandom",
    "choices": [
        { "weightKey": "dodgeChance", "action": "Transition", "state": "Dodge" },
        { "weight": 0.35, "action": "Transition", "state": "Retreat" },
        { "weightKey": "counterChance", "action": "Transition", "state": "Counter" }
    ]
}
```

```json
{
    "name": "BossRevengeAfterLightHit",
    "priority": 880,
    "when": [
        { "condition": "HasTarget" },
        { "condition": "RecentlyHitByPlayer" },
        { "condition": "CanIgnoreLightHit" },
        { "condition": "CanRevengeAfterHit" }
    ],
    "select": "WeightedRandom",
    "choices": [
        { "weightKey": "revengeChance", "action": "Transition", "state": "Counter" },
        { "weight": 0.40, "action": "Transition", "state": "Guard" }
    ]
}
```

### 6.8 구현 우선순위

| 순서 | 항목 | 이유 |
|---:|---|---|
| 1 | `EnemyTacticalMemory`에 최근 피격 타입/횟수/방향 저장 | 구현 완료 |
| 2 | `IsPoiseBroken`, `RecentHitCountGreaterOrEqual` 조건 추가 | 구현 완료 |
| 3 | `CanIgnoreLightHit` 조건 추가 | 구현 완료 |
| 4 | `CanRevengeAfterHit` 조건 추가 | 구현 완료 |
| 5 | 보스 페이즈별 슈퍼아머/Poise 보정 | 보스 전투 보상 창 설계 |

---

## 7. JSON 작성 예시

아래 예시는 새 키와 조건 노드를 사용하는 목표 형태다. 실제 적용 전에는 해당 조건/액션 노드 구현이 필요하다.

```json
{
    "name": "PunishPlayerRecovery",
    "priority": 820,
    "when": [
        { "condition": "HasTarget" },
        { "condition": "HasLineOfSight" },
        { "condition": "IsTargetCastingOrCharging" },
        { "condition": "DistanceLessOrEqual", "value": "optimalCombatDistance" },
        { "condition": "HasAttackSlot" },
        { "condition": "CooldownReady", "value": "PunishRecovery" }
    ],
    "select": "WeightedRandom",
    "choices": [
        { "weightKey": "punishRecoveryChance", "action": "ExecuteAttack", "attackCategory": "Heavy" },
        { "weight": 0.30, "action": "Transition", "state": "Flank" }
    ]
}
```

```json
{
    "name": "ReactAfterRecentlyHit",
    "priority": 900,
    "when": [
        { "condition": "HasTarget" },
        { "condition": "RecentlyHitByPlayer" },
        { "condition": "DistanceLessOrEqual", "value": "minCombatDistance" },
        { "condition": "CooldownReady", "value": "DefensiveReaction" }
    ],
    "select": "WeightedRandom",
    "choices": [
        { "weightKey": "dodgeChance", "action": "Transition", "state": "Dodge" },
        { "weightKey": "counterChance", "action": "Transition", "state": "Counter" },
        { "weight": 0.20, "action": "Transition", "state": "Retreat" }
    ]
}
```

---

## 8. 구현 단계

### 8.1 1단계: 표시명 시스템

- `BehaviorTreeDisplayNameRegistry` 추가
- 블랙보드 키 한글 표시명 적용
- 조건/액션 노드 카드에 한글 표시명 적용
- 검색창과 인스펙터에서 `한글명 (영문명)` 형식 적용

구현 파일:

| 파일 | 상태 |
|---|---|
| `Assets/02.Scripts/AI/BehaviorTree/Editor/BehaviorTreeDisplayNameRegistry.cs` | 추가 완료 |
| `BehaviorTreeNodeView.cs` | 노드 카드 한글 제목 적용 |
| `BehaviorTreeNodeSearchWindow.cs` | 검색 목록 한글명 적용 |
| `BehaviorTreeInspectorView.cs` | 인스펙터 헤더 한글 제목 적용 |
| `BehaviorTreeBlackboardView.cs` | 블랙보드 키 한글 라벨 적용 |
| `BlackboardKeySelectorDrawer.cs` | 키 드롭다운 한글 라벨 적용 |

완료 기준:

- `aggression`이 에디터에서 `공격성 (aggression)`으로 보인다.
- `RecentlyHitByPlayer`가 노드 카드에서 `최근 피격됨`으로 보인다.
- 저장 데이터의 영문 키는 변경되지 않는다.

### 8.2 2단계: 블랙보드 키 추가

- 기준 JSON blackboard에 새 키 추가
- `SyncEnemyBlackboardService` 또는 관련 동기화 경로에서 런타임 값 갱신 여부 검토
- 값이 정적 튜닝값인지, 런타임 관측값인지 분리

정적 튜닝값:

- `aggression`
- `reactionChance`
- `counterChance`
- `dodgeChance`
- `punishRecoveryChance`
- `antiGuardChance`
- `minRetreatCooldown`
- `maxComboPressureCount`
- `preferredRange`

### 8.3 3단계: 조건 노드 추가

우선순위는 다음 순서가 적절하다.

| 순서 | 노드 | 이유 |
|---:|---|---|
| 1 | `CooldownReady` | 모든 반복 행동 제어에 필요 |
| 2 | `RecentlyHitByPlayer` | 반응형 몬스터 체감이 즉시 좋아짐 |
| 3 | `IsSelfLowHealth` | 위기 반응/페이즈와 연결 쉬움 |
| 4 | `HasAttackSlot` | 무리 전투에서 공격 난사 방지 |
| 5 | `HasLineOfSight` | 원거리/돌진 패턴 품질 개선 |
| 6 | `IsTargetCastingOrCharging` | 후딜/선딜 응징 패턴의 핵심 |
| 7 | `IsPlayerLowHealth` | 마무리 압박 패턴 |
| 8 | `RecentlyGuardBroken` | 방어형/보스 패턴 차별화 |
| 9 | `AllyNearby`, `AllyCountNearby` | 그룹 전투 고도화 |
| 10 | `IsTargetBehind` | 암살형/후방 대응형 패턴 |
| 11 | `IsPoiseBroken` | Hit Reaction과 BT 후속 행동 연결 |
| 12 | `CanRevengeAfterHit` | 보스/중형 몬스터의 피격 후 보복 패턴 |

### 8.4 4단계: Hit Reaction 메모리 추가

- `EnemyTacticalMemory`에 최근 피격 타입, 피격 횟수, 피격 방향, 마지막 피격 시간을 저장한다.
- `PoiseStat`의 붕괴 여부를 BT 블랙보드 또는 조건 노드에서 읽을 수 있게 한다.
- 약경직 무시, 강경직, 넉백, 다운의 최종 판정 책임은 상태 머신에 둔다.
- BT는 피격 이후 후퇴/가드/보복/페이즈 분기를 선택한다.

현재 구현:

| 파일 | 구현 내용 |
|---|---|
| `EnemyTacticalMemory.cs` | 최근 피격 횟수, 마지막 피격 반응 타입, Poise Break 여부 저장 |
| `MonsterActor.cs` | 데미지 처리 후 Hit Reaction 메모리 통지 |
| `SyncEnemyMemoryService.cs` | 최근 피격/Poise 값을 Blackboard에 동기화 |
| `IsPoiseBrokenNode.cs`, `RecentHitCountGreaterOrEqualNode.cs`, `CanIgnoreLightHitNode.cs`, `CanRevengeAfterHitNode.cs`, `WasLastHitHeavyNode.cs` | Hit Reaction 조건 노드 추가 |
| `IsSelfLowHealthNode.cs`, `HasAttackSlotNode.cs`, `CooldownReadyNode.cs` | 유틸리티 조건 노드 추가 |
| `MonsterBehaviorTreeJsonImporter.cs` | 새 blackboard 키와 조건 이름 import 지원 |

### 8.5 5단계: 기준 JSON 확장

기준 JSON은 한 번에 보스급으로 키우지 않는다.

권장 확장 순서:

1. blackboard에 새 정적 튜닝값 추가
2. `02 Emergency Reactions` 그룹 추가
3. 최근 피격 반응 룰 추가
4. 후딜 응징 룰 추가
5. 공격 슬롯 게이트 추가
6. 선호 거리 기반 포지셔닝 룰 추가
7. Poise 붕괴 후 후퇴/보복 룰 추가

### 8.6 6단계: 몬스터 템플릿 분리

기준 JSON이 안정되면 다음 템플릿을 추가한다.

| 템플릿 | 목적 |
|---|---|
| `Ground_Melee_Reactive` | 기본 근접 반응형 |
| `Ground_Shield_Tank` | 방어/카운터 중심 |
| `Ground_Ranged_Kiter` | 거리 유지/원거리 견제 |
| `Ground_Assassin_Flanker` | 후방/측면 이동 중심 |
| `Boss_PhaseDuel` | 페이즈별 행동 철학 변경 |

### 8.7 현재 기준 JSON 반영

`Assets/10.Datas/AI/BehaviorTree/SourceJson/Test/EnemyBehavior_Test_SearchPatrol_ReactivePhase.json`에 다음 내용이 반영되어 있다.

- 새 성격/반응 블랙보드 키 추가
- `02 Emergency Reactions` 그룹 추가
- `PoiseBreakRetreatOrCounter` 룰 추가
- `RevengeAfterLightHit` 룰 추가
- `RepeatedHitDisengage` 룰 추가

---

## 9. 주의사항

- 한글 표시명은 저장 식별자가 아니다. JSON에는 영문 키와 영문 노드명을 저장한다.
- 표시명 변경은 런타임 동작에 영향을 주면 안 된다.
- 새 조건 노드를 추가할 때는 JSON importer, 에디터 검색창, Validator, 런타임 노드 평가까지 함께 확인한다.
- 반응형 룰은 너무 높은 우선순위로 남발하면 몬스터가 계속 방어/회피만 하게 된다.
- `HasAttackSlot` 없이 공격 룰을 늘리면 다수 몬스터 전투에서 공격이 동시에 몰릴 수 있다.
- `CooldownReady` 없이 후퇴/회피/카운터를 추가하면 같은 행동이 반복되어 부자연스럽다.
- Hit Reaction은 BT가 직접 데미지 판정을 대신하지 않는다. BT는 피격 결과를 읽고 다음 행동을 고른다.
- 보스에게 모든 피격을 무시시키면 플레이어 보상 창이 사라진다. Poise 붕괴나 특정 패턴 후에는 짧은 확정 리액션을 남긴다.

---

## 10. 완료 목표

1차 완료 상태는 다음과 같다.

- 에디터에서 주요 블랙보드 키와 조건 노드가 한글로 읽힌다.
- 기준 JSON 1종이 새 blackboard 키를 포함한다.
- 최근 피격, 쿨다운, 공격 슬롯, 시야 조건을 사용한 룰이 동작한다.
- Poise 붕괴와 최근 피격 횟수를 기준으로 후퇴/회피/보복 룰이 분기된다.
- 근접 반응형 몬스터 1종이 기존보다 명확하게 방어/회피/카운터/공격 판단을 나눈다.

2차 완료 상태는 다음과 같다.

- 방어형, 원거리형, 보스형 템플릿이 추가된다.
- 페이즈별로 `aggression`, `reactionChance`, `counterChance`, `maxComboPressureCount`가 달라진다.
- 그룹 전투에서 `HasAttackSlot`, `AllyNearby`, `AllyCountNearby`를 이용해 공격 밀도를 제어한다.
