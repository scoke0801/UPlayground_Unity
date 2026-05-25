# Balance Designer Tool 가이드

## 개요

`Balance Designer Tool`은 `ActorDefinitionSO`를 기준으로 몬스터의 스탯, 공격 데이터, Behavior Tree 의사결정 흐름을 한 화면에서 확인하고, 지정한 플레이어 조건과 전투 시간 안에서 전투가 성립하는지 추정하기 위한 에디터 도구다.

레벨 차이와 몬스터 등급별 목표 전투 시간은 [LEVEL_GRADE_COMBAT_BALANCE_POLICY.md](LEVEL_GRADE_COMBAT_BALANCE_POLICY.md)를 기준으로 한다.

현재 프로젝트에는 다음 기반 기능이 이미 있다.

| 구분 | 기존 기반 |
|------|-----------|
| 액터 기준 데이터 | `ActorDefinitionSO`, `ActorDatabase`, `ActorDatabaseEditorWindow` |
| 스탯 | `ActorStatSO`, `ActorStatContainer`, `StatType` |
| 공격 데이터 | `EnemyAttackDataSO`, `PlayerAttackDataSO`, `HitPhaseData`, `AttackDataFromMotionSetWindow` |
| Motion 기반 생성 | `AttackDataFromMotionSetWindow`가 `ActorAnimationMotionSet`의 공격 `AnimKey`와 `BeginCollisionEvent`를 스캔해 `AttackDataSO`를 생성/동기화 |
| BT 디버그 | `BehaviorTreeEditorWindow`, `BehaviorTreeRunner`, `BehaviorTreeDebugTrace`, `IntentScoreTimelineView`, `EncounterReplay` |

이 문서는 위 기능을 묶어 밸런스 디자이너용 분석 툴로 확장하기 위한 기준 문서다. 현재 1차 구현은 `Balance Designer` 분석 창, 누락 데이터 자동 생성, MotionSet 기반 공격 데이터 생성 개선까지 포함한다.

---

## 목표

### 핵심 질문

밸런스 디자이너가 툴에서 확인해야 하는 첫 질문은 다음이다.

> 특정 레벨의 특정 몬스터 타입이 특정 플레이어와 전투할 때, N초 동안 전투가 가능한가?

여기서 "가능"은 단순히 죽지 않는다는 뜻이 아니라 아래 조건을 함께 본다.

| 지표 | 의미 |
|------|------|
| 플레이어 예상 생존 시간 | 몬스터의 평균/최대 압박을 받았을 때 플레이어 HP가 0이 되기까지의 추정 시간 |
| 몬스터 예상 생존 시간 | 플레이어 평균 공격 루프를 기준으로 몬스터 HP가 0이 되기까지의 추정 시간 |
| N초 유지 여부 | 양쪽 중 하나가 너무 빨리 사망하지 않고 기준 시간 이상 전투가 이어지는지 |
| 공격 기회 밀도 | 몬스터가 N초 동안 몇 번 공격 기회를 갖는지 |
| 위험도 | 특정 공격 또는 BT Intent가 전투 시간을 과하게 줄이는지 |
| 데이터 누락 | `ActorDefinitionSO.statData`, `attackData`, `behaviorData`, 공격 `hitPhases` 등이 비어 있는지 |

### 비목표

초기 버전에서는 실제 플레이어 회피/가드/패리 숙련도를 완전 시뮬레이션하지 않는다. 먼저 데이터 기반 정적 추정과 Play Mode 리플레이 비교가 가능한 도구를 만든다.

---

## 데이터 흐름

```
ActorDatabase
    └── ActorDefinitionSO
          ├── level
          ├── grade
          ├── statData              → ActorStatSO
          ├── attackData            → EnemyAttackDataSO
          ├── behaviorData          → EnemyBehaviorSO / BehaviorTreeAsset 연계 대상
          └── prefab                → GameActor / BehaviorTreeRunner / EnemyCombat

ActorAnimationMotionSet
    └── MotionSetAsset
          └── MotionSet
                └── BeginCollisionEvent(hitPhaseIndex)
                      ↓
AttackDataFromMotionSetWindow
                      ↓
EnemyAttackDataSO / PlayerAttackDataSO
                      ↓
Balance Designer Tool 분석 입력
```

BT 쪽 데이터는 두 갈래로 사용한다.

| 사용 방식 | 설명 |
|-----------|------|
| 정적 추정 | `EnemyAttackInfo.selectionWeight`, `cooldown`, `requiredLevel`, `minRange`, `maxRange`, `attackCategory`를 사용해 공격 빈도와 기대 DPS를 계산 |
| 런타임 검증 | `BehaviorTreeRunner.DebugTrace`, `IntentScoreTimeline`, `EncounterReplay`를 로드해 실제 선택 Intent와 정적 추정 결과를 비교 |

---

## 아키텍처

### 제안 파일 구조

```
Assets/02.Scripts/Tool/Editor/Balance/
├── BalanceDesignerWindow.cs                 신규: 메인 에디터 창
├── BalanceScenarioAsset.cs                  신규: 분석 조건 ScriptableObject
├── BalanceScenarioResult.cs                 신규: 분석 결과 DTO
├── BalanceCombatEstimator.cs                신규: 정적 전투 추정 계산기
├── BalanceAttackAnalyzer.cs                 신규: AttackDataSO 요약/검증
├── BalanceActorDataValidator.cs             신규: ActorDefinitionSO 누락 검증
├── BalanceReplayComparator.cs               신규: EncounterReplay 비교
└── BalanceDesignerStyles.cs                 신규: UI Toolkit/IMGUI 스타일 분리
```

### 책임 분리

| 클래스 | 책임 |
|--------|------|
| `BalanceDesignerWindow` | Actor/Player/시간/거리/가정값 입력, 결과 테이블 표시, Motion 공격 데이터 생성기와 BT 에디터 열기 |
| `BalanceScenarioAsset` | 반복 테스트할 조건 저장. 예: 플레이어 레벨, 몬스터 레벨 범위, 기준 시간, 거리 가정 |
| `BalanceCombatEstimator` | HP, 방어, 공격 주기, 쿨다운, 가중치를 이용해 예상 생존 시간 계산 |
| `BalanceAttackAnalyzer` | `EnemyAttackDataSO`와 `PlayerAttackDataSO`에서 총 피해량, 평균 피해량, 히트 수, 쿨다운, 레벨 해금 정보를 요약 |
| `BalanceActorDataValidator` | `ActorDefinitionSO` 필수 참조와 공격 데이터 누락을 검사 |
| `BalanceReplayComparator` | `EncounterReplay`의 실제 Intent/거리/선택 빈도와 정적 추정치를 비교 |

---

## 툴 개선 방향

### Balance Designer 개선 방향

Balance Designer는 단순히 `Stable/TooEasy/TooLethal`만 보여주는 도구가 아니라, 왜 그런 판정이 나왔는지 디자이너가 바로 수정 방향을 알 수 있는 분석 도구로 확장한다.

| 개선 항목 | 설명 |
|-----------|------|
| 레벨 프리셋 | [LEVEL_GRADE_COMBAT_BALANCE_POLICY.md](LEVEL_GRADE_COMBAT_BALANCE_POLICY.md)의 저레벨 Normal, 동레벨 Normal, Elite, Boss 기준을 Scenario 프리셋으로 제공 |
| 강한 공격 확률 표시 | 사용 가능 공격 풀에서 Heavy, Skill, Heavy+Skill 합산 사용 확률을 표시 |
| 위험 기여도 표시 | 공격별 DPS 기여도와 함께 전투 전체 위험 기여도를 정렬 표시 |
| Danger Ring 누락 경고 | Heavy/Skill 공격인데 `useDangerRing == false`이면 Warning |
| 텔레그래프 누락 경고 | 넓은 범위, 긴 사거리, 큰 피해량 공격인데 `useTelegraph == false`이면 Warning |
| Motion 데이터 연결 상태 | 공격 `AnimKey`가 MotionSet에 존재하는지, Collision 이벤트가 있는지 표시 |
| 레벨 해금 분석 | 현재 몬스터 레벨에서 해금되는 공격 수와 잠긴 공격 수를 표시 |
| BT 공격 Intent 분석 | 공격 Intent가 실제로 선택될 가능성과 공격 카테고리 요청 흐름을 요약 |
| 추천 조정값 | 목표 시간 대비 HP, Defense, damage, cooldown, selectionWeight 보정 후보를 제안 |
| CSV 확장 | 카테고리별 확률, Heavy/Skill 합산 확률, Danger Ring 누락 여부를 내보내기 |

결과 테이블은 최종적으로 아래 정보를 한 줄에서 확인할 수 있어야 한다.

```text
actorId | level | grade | 플레이어 생존 | 몬스터 처치 | 플레이어 DPS | 적 DPS | Basic% | Heavy% | Skill% | Strong% | 공격기회 | 상태 | 요약
```

상세 패널은 아래 순서로 구성한다.

1. 전투 시간 요약
2. 데이터 누락/검증 메시지
3. 공격 카테고리 확률 요약
4. 공격별 DPS/확률/쿨다운/HitPhase 분석
5. MotionSet/Collision/Danger Ring/Telegraph 검증
6. BT/Intent 요약
7. 권장 보정 후보

### Motion 기반 공격 데이터 생성기 개선 방향

`AttackDataFromMotionSetWindow`와 Balance Designer의 자동 생성 기능은 같은 규칙을 공유해야 한다. 수동 창에서 만든 공격 데이터와 누락 자동 생성으로 만든 공격 데이터가 서로 다른 기본값을 가지면 밸런스 분석이 흔들린다.

| 개선 항목 | 설명 |
|-----------|------|
| 공통 생성 규칙 분리 | MotionSet 스캔, 공격 카테고리 판정, HitPhase 생성, 대미지 배분을 공용 서비스로 분리 |
| AnimKey 기반 카테고리 자동 지정 | `Attack_* = Basic`, `HeavyAttack_* = Heavy`, `Skill_* = Skill`, `Fly_Attack = Skill` |
| 강한 공격 Danger Ring 자동화 | Heavy/Skill 생성 시 `useDangerRing = true`, `dangerRingDuration = 0` 기본 설정 |
| 강한 공격 확률 기본값 | 카테고리별 기본 `selectionWeight`를 등급 기준으로 다르게 제안 |
| Collision 기반 Phase 동기화 | `BeginCollisionEvent.hitPhaseIndex` 최대값과 이벤트 개수를 모두 반영해 `HitPhaseData` 개수 생성 |
| Collision 없는 공격 처리 | 투사체/소환형 Skill은 제외 대신 별도 경고와 수동 duration 입력 경로 제공 |
| TelegraphEvent 검증 | `useMotionEventTelegraph == true`인데 MotionSet에 `TelegraphEvent`가 없으면 경고 |
| Danger Ring 미리보기 | 첫 Collision까지의 수축 시간을 미리 계산해 생성 전 표시 |
| 기존 데이터 동기화 정책 | Skip, SyncPhaseCount, Replace 외에 `SyncWarningFlags`를 추가해 UI 경고 필드만 갱신 |
| 생성 후 즉시 재분석 | Balance Designer에서 생성/동기화 후 선택 Actor를 자동 재분석 |

#### 현재 구현된 생성 규칙

Motion 기반 공격 데이터 생성기는 `ActorDefinitionSO`를 직접 입력으로 받을 수 있다. Actor를 지정하면 프리팹의 `ActorAnimator.MotionSet`, `attackData`, `statData`, `level`, `grade`를 함께 읽어 생성 기준으로 사용한다.

| 입력 | 반영 방식 |
|------|-----------|
| `ActorDefinitionSO.prefab` | `ActorAnimator.MotionSet` 자동 탐색 |
| `ActorDefinitionSO.attackData` | 몬스터 공격 데이터 생성/동기화 대상 자동 지정 |
| `ActorDefinitionSO.statData.AttackPower` | 런타임 공격력 곱셈을 고려해 저장 피해량을 역보정 |
| `ActorDefinitionSO.level` | 레벨당 피해 성장률로 Motion 추출 피해량에 반영 |
| `ActorDefinitionSO.grade` | 기본 피해량과 Heavy/Skill `selectionWeight` 기본값에 반영 |

주의할 점은 런타임 피해 공식이다. 현재 전투 런타임은 `HitPhaseData.damage`에 공격자의 `AttackPower`를 다시 곱한다.

```csharp
최종피해 = HitPhaseData.damage * Attacker.AttackPower * 방어보정
```

따라서 생성기가 `AttackPower`를 그대로 저장 피해량에 곱하면 실제 전투에서는 공격력이 중복 적용된다. 기본 옵션인 `AttackPower 런타임 곱셈 역보정`은 아래 방식으로 저장 피해량을 만든다.

```text
저장 피해량 = 목표 최종 피해량 * 레벨 보정 / AttackPower
예상 최종 피해량 = 저장 피해량 * AttackPower
```

이렇게 하면 Stat 데이터와 레벨을 기준으로 목표 피해량을 정하면서도, 런타임의 기존 피해 공식을 바꾸지 않는다. 생성기 미리보기의 `DMG`는 저장될 `HitPhaseData.damage` 합계이고, `Final`은 런타임에서 공격력이 곱해진 뒤의 예상 피해량이다.

공격 데이터 생성 시 기본값은 아래 정책을 따른다.

| 카테고리 | selectionWeight 기본값 | cooldown 기본값 | useDangerRing | useTelegraph |
|----------|------------------------|----------------|---------------|--------------|
| Basic | 10 | 2.0초 | false | false |
| Heavy | 4 ~ 6 | 3.0초 | true | 필요 시 true |
| Skill | 3 ~ 5 | 4.0초 | true | 범위/장판형이면 true |
| Counter | 3 | 3.0초 | true | false |
| Fly_Attack | 4 | 4.0초 | true | 패턴에 따라 true |

등급별 추천 가중치는 아래처럼 보정한다.

| 등급 | Basic | Heavy | Skill |
|------|-------|-------|-------|
| Normal | 10 | 3 | 1 |
| Elite | 10 | 5 | 4 |
| Boss | 10 | 7 | 7 |

이 값은 개별 공격 1개의 기본값이다. 같은 카테고리에 공격이 여러 개 있으면 카테고리 합산 확률이 커지므로 Balance Designer가 합산 확률을 별도로 경고해야 한다.

---

## 입력 데이터

### ActorDefinitionSO

`ActorDefinitionSO`는 이 툴의 중심 입력이다.

| 필드 | 사용 |
|------|------|
| `actorId` | 결과 테이블 키, 리포트 식별자 |
| `displayName` | UI 표시명 |
| `actorType` | 몬스터/플레이어/NPC 필터 |
| `characterType` | 플레이어블 캐릭터 비교 기준 |
| `level` | `EnemyAttackInfo.requiredLevel` 필터와 레벨 스케일링 기준 |
| `grade` | Normal/Elite/Boss 가중치 또는 기준 시간 프리셋 |
| `statData` | `MaxHealth`, `AttackPower`, `Defense`, `MaxPoise` 등 계산 입력 |
| `attackData` | 몬스터 공격 풀 분석 |
| `behaviorData` | BT/AI 프로필 연결 지점 |
| `prefab` | Play Mode 검증 시 `BehaviorTreeRunner`, `EnemyCombat` 등 런타임 컴포넌트 확인 |

### AttackDataSO

`EnemyAttackDataSO.skills`는 몬스터의 공격 가능성과 위험도를 계산하는 핵심 데이터다.

| 필드 | 계산 방식 |
|------|-----------|
| `baseInfo.hitPhases[].damage` | 공격 1회 총 피해량 |
| `baseInfo.hitPhases[].poiseDamage` | 플레이어 경직/방어 압박 지표 |
| `baseInfo.hitPhases[].breakDamage` | 몬스터 피격 측 분석에서는 브레이크 누적 지표 |
| `selectionWeight` | 기대 선택 확률 |
| `cooldown` | 공격 반복 주기 하한 |
| `minRange` / `maxRange` | 기준 거리에서 사용 가능한 공격 필터 |
| `requiredLevel` | 몬스터 레벨에 따른 해금 필터 |
| `attackCategory` | BT가 특정 카테고리를 요청할 때의 분포 추정 |

#### 공격 선택 확률

`selectionWeight`는 단독 확률값이 아니라, 현재 레벨/거리/조건을 통과한 사용 가능 공격 풀 안에서의 상대 가중치다.

```text
공격 사용 확률 = 해당 공격 selectionWeight / 사용 가능 공격 selectionWeight 합
```

예를 들어 기준 거리에서 사용 가능한 공격이 아래와 같다면 실제 선택 확률은 다음처럼 계산한다.

| 공격 | attackCategory | selectionWeight | 실제 선택 확률 |
|------|----------------|-----------------|----------------|
| Attack_1 | Basic | 10 | 50% |
| HeavyAttack_1 | Heavy | 6 | 30% |
| Skill_1 | Skill | 4 | 20% |

Balance Designer는 개별 공격 확률뿐 아니라 카테고리별 합산 확률도 표시해야 한다.

| 카테고리 | 계산 |
|----------|------|
| Basic 사용 확률 | Basic 가중치 합 / 전체 사용 가능 가중치 합 |
| Heavy 사용 확률 | Heavy 가중치 합 / 전체 사용 가능 가중치 합 |
| Skill 사용 확률 | Skill 가중치 합 / 전체 사용 가능 가중치 합 |
| 강한 공격 사용 확률 | Heavy + Skill 가중치 합 / 전체 사용 가능 가중치 합 |

초기 권장 범위는 아래를 기준으로 한다.

| 몬스터 등급 | Heavy 사용 확률 | Skill 사용 확률 | 강한 공격 합산 |
|-------------|----------------|----------------|----------------|
| Normal | 10 ~ 20% | 0 ~ 10% | 10 ~ 25% |
| Elite | 15 ~ 30% | 10 ~ 20% | 25 ~ 45% |
| Boss | 20 ~ 35% | 20 ~ 35% | 40 ~ 65% |

이 값은 BT가 공격 Intent를 선택한 이후, 실제 공격 풀에서 어떤 공격이 선택되는지에 대한 기준이다. BT가 방어/후퇴/추격 Intent를 자주 선택하면 전체 전투에서의 체감 사용 빈도는 더 낮아진다.

#### 강한 공격 UI 경고 정책

강한 공격은 플레이어가 대응해야 하는 공격이므로 `UIDangerRing` 노출을 기본 정책으로 둔다.

| 조건 | 기본 동작 |
|------|-----------|
| `attackCategory == EnemyAttackCategory.Heavy` | `useDangerRing = true` 자동 설정 |
| `attackCategory == EnemyAttackCategory.Skill` | `useDangerRing = true` 자동 설정 |
| `AnimKey.HeavyAttack_*` | 공격 데이터 생성 시 `attackCategory = Heavy`, `useDangerRing = true` |
| `AnimKey.Skill_*`, `AnimKey.Fly_Attack`, 카운터형 특수 공격 | 공격 데이터 생성 시 `attackCategory = Skill`, `useDangerRing = true` |
| Basic 공격 | 기본 `useDangerRing = false`, 단 예외 패턴은 수동 설정 |

`UIDangerRing`은 바닥 범위 예고인 `useTelegraph`와 별개다. `useTelegraph`는 공격 범위와 위치를 알려주는 연출이고, `UIDangerRing`은 적 몸통/락온 지점에 붙어 타격 타이밍을 알려주는 UI다. 따라서 Heavy/Skill 공격은 바닥 텔레그래프가 없어도 Danger Ring을 노출할 수 있어야 한다.

Danger Ring 수축 시간은 기본적으로 현재 MotionSet의 다음 `BeginCollisionEvent`까지의 시간을 사용한다. Collision 이벤트가 없는 투사체/소환형 공격은 `dangerRingDuration`을 수동 지정하거나, 공격 데이터 생성기가 안전한 기본값을 넣어야 한다.

### BT / Intent

BT 분석은 초기 버전에서 완전한 노드 시뮬레이션보다 "의사결정 보정값"으로 사용한다.

| 데이터 | 사용 |
|--------|------|
| `BehaviorTreeRunner.DebugTrace` | Play Mode에서 실제 Tick 경로 확인 |
| `IntentScoreTimeline` | 선택된 Intent와 점수 흐름 확인 |
| `EncounterReplay.frames` | 거리, HP 비율, 선택 Intent, 공격 슬롯 여부 비교 |
| `EnemyBlackboardKeys.DecisionSelectedIntent` | 공격/후퇴/방어/추격 등 실제 선택 경향 |

---

## 생존 가능성 계산

### 1차 정적 계산

초기 구현은 아래처럼 단순하고 설명 가능한 모델을 사용한다.

```csharp
몬스터_공격1회피해 = Sum(hitPhase.damage) * 몬스터_AttackPower * (1 - 플레이어_Defense)
몬스터_기대DPS = Sum(공격1회피해 * 선택확률 / Max(cooldown, globalCooldown))
플레이어_예상생존시간 = 플레이어_MaxHealth / 몬스터_기대DPS

플레이어_기대DPS = 플레이어_공격루프_기준DPS * 플레이어_AttackPower * (1 - 몬스터_Defense)
몬스터_처치예상시간 = 몬스터_MaxHealth / 플레이어_기대DPS
```

공격 선택 확률은 기준 거리와 레벨 조건을 통과한 공격만 대상으로 한다.

```csharp
선택확률 = skill.selectionWeight / 사용가능공격 selectionWeight 합
```

### N초 전투 가능 판정

| 결과 | 조건 |
|------|------|
| `TooEasy` | 몬스터 예상 생존 시간이 기준 시간보다 너무 짧음 |
| `TooLethal` | 플레이어 예상 생존 시간이 기준 시간보다 너무 짧음 |
| `Stable` | 양쪽 예상 생존 시간이 기준 시간 이상이고 공격 기회가 충분함 |
| `Stalled` | 양쪽 생존은 가능하지만 몬스터 공격 기회 또는 유효 DPS가 너무 낮음 |
| `InvalidData` | 필수 데이터 누락으로 계산 불가 |

기준 시간은 `BalanceScenarioAsset`에 저장한다. 예시는 다음과 같다.

| 등급 | 기본 기준 시간 |
|------|----------------|
| Normal | 20초 |
| Elite | 45초 |
| Boss | 90초 |

### 방어/회피 가정값

플레이어 숙련도는 결과를 크게 바꾸므로 수동 가정값으로 둔다.

| 값 | 설명 |
|----|------|
| 피격 허용률 | 몬스터 공격 중 실제로 맞는 비율. 기본 0.45 |
| 가드 경감률 | 방어 가능한 공격의 평균 경감률 |
| 회피 성공률 | 텔레그래프/위험 링 공격 회피율 |
| 패리 성공률 | `AttackDefenseType.Parryable` 공격에 대한 평균 패리 성공률 |

초기 버전에서는 이 값을 곱셈 보정으로 처리하고, 이후 `EncounterReplay`와 비교해 프리셋을 조정한다.

---

## 에디터 UX

### 메뉴

신규 메뉴 경로는 다음으로 둔다.

```text
UPlayGround/Gameplay/Balance/Balance Designer
```

기존 공격 데이터 생성기 메뉴는 유지한다.

```text
UPlayGround/Gameplay/Combat/MotionSet 기반 공격 데이터 생성기
```

### 화면 구성

```
┌────────────────────────────────────────────────────────────┐
│ Toolbar                                                    │
│ Scenario | ActorDatabase | Analyze | Export CSV | Open BT  │
├───────────────┬────────────────────────────────────────────┤
│ Actor List    │ Selected Actor Summary                     │
│ - filter      │ - ActorDefinitionSO fields                 │
│ - grade       │ - Stat summary                             │
│ - level range │ - Attack summary                           │
├───────────────┴────────────────────────────────────────────┤
│ Result Table                                               │
│ actorId | level | 플레이어 생존 | 몬스터 처치 | status | notes │
├────────────────────────────────────────────────────────────┤
│ Detail                                                     │
│ Attack breakdown | BT/Intent breakdown | Replay compare    │
└────────────────────────────────────────────────────────────┘
```

### 필수 버튼

| 버튼 | 동작 |
|------|------|
| `Analyze Selected` | 선택한 `ActorDefinitionSO`만 계산 |
| `Analyze Database` | `ActorDatabase.All` 전체 계산 |
| `Open Attack Generator` | `AttackDataFromMotionSetWindow.Open(...)` 호출 |
| `Open Behavior Tree` | 연결 가능한 `BehaviorTreeAsset` 또는 BT 에디터 열기 |
| `Load Replay` | `EncounterReplayLoader`로 리플레이 JSON 로드 |
| `Export CSV` | 결과 테이블을 밸런스 비교용 CSV로 저장 |

---

## 구현 단계

### 1단계: 읽기 전용 분석 툴

1. `BalanceScenarioAsset` 생성
2. `BalanceCombatEstimator`로 플레이어 생존 시간과 몬스터 처치 예상 시간 계산
3. `ActorDatabase` 전체를 읽어 결과 테이블 표시
4. 데이터 누락 경고 표시
5. CSV 내보내기

이 단계에서는 기존 SO를 수정하지 않는다.

### 2단계: Motion 공격 데이터 편집 연동

1. 선택한 `ActorDefinitionSO.attackData`를 공격 데이터 생성기에 전달
2. `ActorAnimationMotionSet` 참조를 수동 입력하거나 프리팹에서 탐색
3. `BeginCollisionEvent.hitPhaseIndex`와 `HitPhaseData` 개수 불일치 경고 표시
4. Heavy/Skill 공격은 `useDangerRing`을 자동으로 켜고, 첫 Collision까지의 수축 시간을 자동 산출
5. 생성/동기화 후 즉시 재분석

자동 누락 데이터 생성은 `ActorDefinitionSO.prefab`의 `ActorAnimator.MotionSet`을 먼저 읽고, 없으면 `actorId`/에셋 이름/displayName으로 `ActorAnimationMotionSet` 에셋을 검색한다. MotionSet에서 공격 `AnimKey`와 `BeginCollisionEvent`를 찾으면 해당 Collision 개수와 `hitPhaseIndex`를 기준으로 `EnemyAttackDataSO.skills`와 `HitPhaseData`를 생성한다. 유효한 공격 Motion을 찾지 못한 경우에만 기본 `Attack_1` 플레이스홀더를 생성한다.

### 3단계: BT 정적 요약

1. `EnemyAttackCategory`별 공격 풀 요약
2. Basic/Heavy/Skill/강한 공격 합산 사용 확률 표시
3. Intent별 기대 공격 빈도 보정값 표시
4. `behaviorData` 또는 연결된 `BehaviorTreeAsset` 누락 경고
5. 공격 Intent가 없는 BT, 공격 슬롯을 요청하지 않는 BT, 레벨 해금 공격이 없는 몬스터 탐지

### 4단계: Play Mode 리플레이 비교

1. `EncounterReplay` 로드
2. 실제 선택 Intent 비율 계산
3. 실제 거리 분포 기반으로 사용 가능 공격 재계산
4. 정적 예상 전투 시간과 리플레이 기반 압박 지표 비교

### 5단계: 권장 보정 제안

자동 수정은 마지막 단계에서만 제공한다.

| 제안 | 조건 |
|------|------|
| 공격 피해량 낮추기 | `TooLethal`이고 특정 공격 피해 기여도가 과도함 |
| 쿨다운 늘리기 | DPS보다 공격 빈도가 문제일 때 |
| selectionWeight 낮추기 | 특정 공격이 지나치게 자주 선택될 때 |
| 몬스터 HP/방어 조정 | `TooEasy`일 때 |
| 공격 해금 레벨 조정 | 낮은 레벨에서 고위험 공격이 열려 있을 때 |

자동 적용 버튼은 `Undo.RecordObject`와 변경 전/후 프리뷰를 반드시 포함한다.

---

## 검증 규칙

### ActorDefinitionSO 검증

| 조건 | 심각도 |
|------|--------|
| `actorId` 비어 있음 | Error |
| `prefab` 없음 | Warning |
| `statData` 없음 | Error |
| 몬스터인데 `attackData` 없음 | Error |
| 몬스터인데 `behaviorData` 없음 | Warning |
| `level < 1` | Error |
| `targetLayerMask`가 0이고 기본 ActorType도 불명확 | Warning |

### AttackData 검증

| 조건 | 심각도 |
|------|--------|
| `EnemyAttackDataSO.skills` 비어 있음 | Error |
| `baseInfo` 또는 `hitPhases` 없음 | Error |
| `selectionWeight <= 0` | Warning |
| `cooldown <= 0` | Warning |
| `requiredLevel > ActorDefinitionSO.level`인 공격만 존재 | Error |
| 기준 거리에서 사용 가능한 공격 없음 | Warning |
| `damage <= 0` | Warning |
| Heavy/Skill 공격인데 `useDangerRing == false` | Warning |
| `useDangerRing == true`인데 MotionSet에 Collision 이벤트가 없고 `dangerRingDuration <= 0` | Warning |
| Heavy/Skill 합산 사용 확률이 등급 권장 범위를 크게 벗어남 | Warning |
| Boss 공격 1개의 DPS 기여도가 전체 몬스터 DPS의 35%를 초과 | Warning |

### BT 검증

| 조건 | 심각도 |
|------|--------|
| `BehaviorTreeRunner.SourceTree` 없음 | Warning |
| 공격 Intent 또는 공격 상태 전환 경로 확인 불가 | Warning |
| 리플레이에서 `hasAttackSlot == false`가 과도하게 많음 | Warning |
| `resolverFailureReason` 반복 | Warning |

---

## 확장 포인트

### 레벨 스케일링

현재 `ActorDefinitionSO.level`은 몬스터 기준 레벨이고, `EnemyAttackInfo.requiredLevel` 필터에 바로 사용할 수 있다. 이후 성장 곡선을 추가할 경우 신규 `BalanceLevelCurveSO`를 두고 다음 값을 분리한다.

| 값 | 설명 |
|----|------|
| HP 스케일 | 레벨별 `MaxHealth` 보정 |
| 공격 스케일 | `AttackPower` 또는 `HitPhaseData.damage` 보정 |
| 방어 스케일 | `Defense` 보정 |
| Poise 스케일 | `MaxPoise` / `poiseDamage` 보정 |

### 파티/캐릭터별 비교

플레이어는 `CharacterActorType` 단위로 비교한다. 초기에는 수동으로 `PlayerAttackDataSO`와 `ActorStatSO`를 입력하고, 이후 `PartyConfigSO` 또는 캐릭터별 데이터베이스와 연결한다.

### BT 리플레이 기반 프리셋

`EncounterReplay`가 쌓이면 몬스터 등급/타입별 기본 가정값을 보정할 수 있다.

| 리플레이 지표 | 보정 대상 |
|---------------|-----------|
| 실제 피격 빈도 | 피격 허용률 |
| 실제 거리 분포 | 기준 거리 프리셋 |
| 실제 선택 Intent 비율 | BT Intent 보정값 |
| 공격 슬롯 실패율 | 공격 기회 밀도 |

---

## 주의 사항

- 초기 툴은 실제 액션 게임 전투를 완전 재현하는 시뮬레이터가 아니다. 설명 가능한 정적 추정값과 리플레이 비교를 우선한다.
- `AttackDataFromMotionSetWindow`는 `BeginCollisionEvent`가 없는 공격을 제외할 수 있다. 투사체나 소환형 공격은 별도 규칙이 필요하다.
- `ActorStatSO.Defense`는 0~1 감소율로 해석해야 한다. 1 이상 값이 들어가면 피해량 계산이 0 이하로 떨어질 수 있으므로 검증해야 한다.
- BT는 노드 전체를 에디터에서 정적으로 실행하지 않는다. 정적 단계에서는 공격 풀과 Intent/카테고리 분포를 추정하고, 실제 검증은 `BehaviorTreeRunner`와 `EncounterReplay`를 사용한다.
- 자동 밸런스 수정은 최종 단계에서만 도입한다. 처음부터 SO 값을 직접 바꾸면 원인 추적이 어려워진다.
