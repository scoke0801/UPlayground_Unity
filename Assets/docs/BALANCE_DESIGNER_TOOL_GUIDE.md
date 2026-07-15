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

### 외부 조사 기반 원칙

최근 게임 밸런싱 자료와 연구는 공통적으로 "수치 균등화"보다 **플레이어 체감 + 전투 로그 + 반복 가능한 시뮬레이션**의 결합을 강조한다.

| 원칙 | 적용 방식 |
|------|-----------|
| 수치상 균형만으로 충분하지 않음 | `Stable` 판정이어도 플레이테스트/리플레이 로그로 체감 난이도를 확인한다. 참고: `It might be balanced, but is it actually good?`(arXiv 2024) |
| 플레이어 의견과 전투 데이터 결합 | 정적 예측값과 실제 `CombatLogRecorder`/`EncounterReplay` 통계를 비교한다. 참고: `On Video Game Balancing: Joining Player- and Data-Driven Analytics`(arXiv 2023) |
| 자동 테스트는 보조 수단 | 수동 플레이테스트를 대체하지 않고, 반복 파라미터 검증과 회귀 탐지에 사용한다. 참고: `Assessing Video Game Balance using Autonomous Agents`(arXiv 2023), `Automatic Playtesting for Game Parameter Tuning`(arXiv 2019) |
| 변경 원인 추적 필요 | 자동 적용은 `Undo`와 변경 전/후 CSV를 남기고, 공유 SO 변경 위험을 명시한다 |

따라서 UPlayground의 밸런스 툴은 `ActorDefinitionSO`를 중심으로 한 정적 추정에서 출발하되, 최종 의사결정은 리플레이/전투 로그 비교까지 거친다.

### 명조 레퍼런스 기반 기본값

몬스터 기본 Scaling은 명조(Wuthering Waves)의 공개 전투 구조를 참고한 **액션 전투 페이싱 프리셋**이다. 명조의 정확한 적 HP/ATK 원시 테이블은 공식적으로 공개된 자료가 아니므로, 아래 값은 원본 수치 복제가 아니라 UPlayground의 `Weak/Normal/Elite/Boss` 등급 체계에 맞춘 상대 배율이다.

참고한 공개 구조:

| 레퍼런스 | 반영 기준 |
|----------|-----------|
| 명조 전투는 Basic/Heavy/Mid-air/Dodge Counter, Resonance Skill/Liberation, Intro/Outro, Echo를 조합한다. | 플레이어가 회피 카운터/스킬로 위험 구간을 끊을 수 있으므로 일반 몬스터 HP 벽을 낮춘다. |
| 적은 Common/Elite/Boss/Calamity 등으로 분류된다. | UPlayground의 `Weak/Normal/Elite/Boss`에 대응해 Elite부터 HP/Poise를 뚜렷하게 올린다. |
| Vibration Strength가 모두 깎이면 적이 Immobilize 되고, 카운터/회피 카운터/Intro Skill이 이를 크게 줄인다. | `MaxPoise` 배율을 HP보다 별도 축으로 운용한다. Elite/Boss는 경직 플레이를 요구하되, Boss Poise를 과도하게 높이지 않는다. |

참고 URL:

- https://wutheringwaves.fandom.com/wiki/Combat
- https://wutheringwaves.fandom.com/wiki/Enemy
- https://wutheringwaves.fandom.com/wiki/Vibration_Strength

적용 기본값:

| 등급 | HP | ATK | Poise | DEF+ | Move | 공격 1타 피해 |
|------|----|-----|-------|------|------|----------------|
| Weak | 0.45 | 0.65 | 0.35 | 0.00 | 1.02 | 0.65 |
| Normal | 1.00 | 1.00 | 1.00 | 0.00 | 1.00 | 1.00 |
| Elite | 2.60 | 1.20 | 2.10 | 0.04 | 1.05 | 1.25 |
| Boss | 9.50 | 1.45 | 4.20 | 0.08 | 1.00 | 1.60 |

레벨 성장 기본값:

| StatType | 성장률 |
|----------|--------|
| `MaxHealth` | 레벨당 3.5% |
| `AttackPower` | 레벨당 3.0% |
| `MaxPoise` | 레벨당 1.8% |

`MonsterScalingSO.ApplyActionCombatDefaults()`와 `Assets/10.Datas/Stat/Generated/MonsterScaling_Default.asset`은 위 표를 단일 기준으로 사용한다.

### 구현 현황 (2026-06 고도화)

1단계 정적 분석 위에 다음 고도화가 반영되었다.

| 항목 | 내용 |
|------|------|
| 경직 압박 분석 | 적 `poiseDamage`로 초당 경직 압박(poise/s)을 계산하고, 플레이어 `PoiseRecoveryRate` 대비 순 압박을 표시. 가드 브레이크는 가드 횟수 기반이라 '브레이크 시간'은 추정하지 않는다 |
| DPS 기여도 정렬 | 공격 기여도 테이블을 DPS 내림차순 정렬하고 DPS 비중(`DPS%`)을 표시. 단일 공격이 전체 적 DPS의 35%를 넘으면 경고 |
| Strong% 등급 밴드 | 강한 공격 합산 확률을 등급 권장 밴드와 대조해 상한 초과는 경고, 하한 미만은 정보로 안내 (순수 기본 공격 몬스터는 경고 제외) |
| 텔레그래프 검증 | 넓은 범위/긴 사거리 강공격인데 `useTelegraph`가 꺼져 있으면 경고 |
| 해금/잠김 표시 | 현재 레벨 기준 해금·잠김 공격 수와 사용 가능 공격 수, 최대 기여 공격을 요약에 표시 |
| Danger Ring 프리뷰 | 공격별 `dangerRingDuration`(0이면 런타임 자동) 및 텔레그래프 플래그를 기여도 테이블에 표시 |
| 품질 점수 | 목표 시간 대비 플레이어 생존비/몬스터 처치비, 공격 기회, 공격 과점, Strong%를 0~100 점수로 요약 |
| 권장 액션 | `TooEasy`, `TooLethal`, 공격 과점, 경직 압박 등 원인별 다음 조치 문구를 결과 테이블에 표시 |
| 명조형 기본값 | `MonsterScalingSO.ApplyActionCombatDefaults()`와 기본 Scaling 에셋에 명조형 Common/Elite/Boss 전투 구조를 반영 |
| CSV 확장 | 경직 압박, 순 압박, 최대 기여 공격/비중, Danger Ring 누락 수, 해금/잠김 수 컬럼 추가 |

또한 데이터셋 전반을 한눈에 비교하기 위한 **Balance Data Extractor**(`UPlayGround/Gameplay/Balance/Balance Data Extractor`)를 추가했다. 플레이어 공격 데이터, 몬스터 공격 데이터, 플레이어 스탯, 몬스터 스탯을 프로젝트 전체에서 스캔·요약하고 탭별 CSV로 내보낸다. 스탯의 플레이어/몬스터 분류는 `ActorDefinitionSO.actorType`과 `PartyMemberGrowthSO.baseStat` 참조를 기준으로 한다. 기존 `StatDatabaseEditorWindow`/`StatDataGeneratorWindow`가 스탯 단일 편집/생성에 집중하는 것과 달리, 이 창은 4개 데이터 카테고리를 통합 비교하는 읽기 전용 뷰다.

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
| 품질 점수 | 목표 시간 주변에 얼마나 가까운지, 공격 기회/과점/강공격 비중이 적정한지 |
| 권장 액션 | 다음에 조정할 후보. 예: HP 상향, 특정 공격 피해 하향, selectionWeight 조정 |

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
├── BalanceDesignerWindow.cs                 ✅ 메인 에디터 창
├── BalanceScenarioAsset.cs                  ✅ 분석 조건 ScriptableObject
├── BalanceScenarioResult.cs                 ✅ 분석 결과 DTO
├── BalanceCombatEstimator.cs                ✅ 정적 전투 추정 계산기 (경직 압박/정렬/과점/품질 점수 포함)
├── BalanceAttackAnalyzer.cs                 ✅ AttackDataSO 요약/검증 (damage/poise 합산)
├── BalanceActorDataValidator.cs             ✅ 누락 검증 + 텔레그래프/Strong밴드/과점 사후 검증
├── BalanceDataAutoGenerator.cs              ✅ MotionSet 기반 누락 데이터 자동 생성
├── BalanceScenarioGenerator.cs              ✅ 현재 Player 데이터 기반 시나리오 에셋 자동 생성/갱신
├── BalanceDataExtractor.cs                  ✅ 4개 데이터 카테고리 스캔/요약 서비스
├── BalanceDataExtractorWindow.cs            ✅ 데이터 추출 탭 창 + CSV
├── MonsterStatBakeService.cs                ✅ 몬스터 statData 발급/갱신 단일 서비스
├── MonsterStatGeneratorWindow.cs            ✅ 커브 기반 몬스터 스탯 배치 생성/재레벨링
├── BalanceTargetSolver.cs                   ✅ 목표 전투시간 역산 (권장 HP/피해 배율 + 적용)
├── BalanceReplayComparator.cs               ⏳ 미구현: EncounterReplay 비교 (인프라 존재)
└── BalanceDesignerStyles.cs                 ⏳ 미구현: 스타일 분리 (현재 인라인)
```

### 책임 분리

| 클래스 | 책임 |
|--------|------|
| `BalanceDesignerWindow` | Actor/Player/시간/거리/가정값 입력, 결과 테이블 표시, Motion 공격 데이터 생성기와 BT 에디터 열기 |
| `BalanceScenarioAsset` | 반복 테스트할 조건 저장. 예: 플레이어 레벨, 몬스터 레벨 범위, 기준 시간, 거리 가정 |
| `BalanceCombatEstimator` | HP, 방어, 공격 주기, 쿨다운, 가중치를 이용해 예상 생존 시간 계산 |
| `BalanceAttackAnalyzer` | `EnemyAttackDataSO`와 `PlayerAttackDataSO`에서 총 피해량, 평균 피해량, 히트 수, 쿨다운, 레벨 해금 정보를 요약 |
| `BalanceActorDataValidator` | `ActorDefinitionSO` 필수 참조와 공격 데이터 누락을 검사 |
| `BalanceScenarioGenerator` | `PartyConfigSO`(성장 데이터) + 캐릭터 모델 공격 데이터를 읽어 `BalanceScenarioAsset`을 자동 생성/갱신 |
| `BalanceReplayComparator` | `EncounterReplay`의 실제 Intent/거리/선택 빈도와 정적 추정치를 비교 |

---

### 몬스터 스탯 발급 통합

몬스터 `statData` 발급 경로는 `MonsterStatBakeService`로 통합한다.

| 호출자 | 역할 |
|--------|------|
| `MonsterStatGeneratorWindow` | 수동 일괄 생성/재레벨링 |
| `BalanceDataAutoGenerator` | Balance Designer에서 누락 데이터 자동 생성 |
| `StatDataGeneratorWindow` | 마이그레이션/전체 보정 중 몬스터 스탯 계산 위임 |
| `P09Builder.SyncActorDatabaseStep` | 프리팹 빌드 후 `ActorDefinitionSO.statData` 생성/갱신 |

계산 자체는 여전히 `MonsterStatCalculator`가 담당한다. `MonsterStatBakeService`는 에셋 생성, 기존 에셋 제자리 갱신, `MonsterScalingSO` 연결, 생성된 `PoiseSO` 동기화, `BreakGaugeSO` 보정 같은 에디터 정책만 담당한다.

---

## 현재 Player 데이터 기반 시나리오 자동 생성

`BalanceScenarioAsset`은 분석할 때마다 플레이어 캐릭터·스탯·공격 데이터·레벨을 손으로 채워야 했다. 이를 **현재 Player 데이터에서 자동 생성**하도록 `BalanceScenarioGenerator`를 추가했다. `Balance Designer` 창 툴바의 다음 두 버튼으로 사용한다.

| 버튼 | 동작 |
|------|------|
| `Scenario ← Player` | `PartyConfigSO.growthData`의 첫 유효 캐릭터(없으면 Bokusei) 1명에 대한 시나리오를 생성/갱신하고 창의 분석 대상으로 자동 연결 |
| `Scenario ← Party` | `PartyConfig.growthData`에 등록된 모든 파티 캐릭터에 대해 시나리오를 일괄 생성/갱신 |

### 채워지는 값과 보존되는 값

생성기는 **플레이어 파생 4개 필드만** 기록한다.

| 필드 | 출처 |
|------|------|
| `playerCharacter` | 해석된 캐릭터 타입 |
| `playerStatData` | `PartyMemberGrowthSO.baseStat` (캐릭터 타입 일치) |
| `playerLevel` | `PartyMemberGrowthSO.initialLevel` (없으면 1) |
| `playerAttackData` | ① Model 프리팹 `CharacterModelData.attackData` → ② 이름에 캐릭터명이 포함된 `PlayerAttackDataSO` → ③ 프로젝트에 `PlayerAttackDataSO`가 하나뿐이면 공용 기본값 → ④ 실패 시 `null`(추정기가 `manualPlayerDps`로 폴백) |

인카운터/방어/임계 가정값(`targetDuration`, `assumedDistance`, `hitReceiveRate`, `guardMitigationRate` 등)은 **건드리지 않는다.** 신규 생성 시에는 `BalanceScenarioAsset`의 필드 기본값을 그대로 쓰고, 기존 에셋을 재생성할 때는 사용자가 손으로 튜닝한 가정값을 보존한 채 플레이어 4개 필드만 새로고침한다(Undo 지원).

### 저장 위치와 갱신 규칙

`Assets/10.Datas/Balance/Scenarios/BalanceScenario_{Character}.asset` 경로에 캐릭터별로 1개씩 결정적 경로로 저장한다. 같은 캐릭터를 다시 생성하면 새 에셋을 만들지 않고 기존 에셋을 제자리 갱신하므로 중복이 쌓이지 않는다.

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
actorId | level | grade | score | 플레이어 생존 | 몬스터 처치 | 플레이어 DPS | 적 DPS | Basic% | Heavy% | Skill% | Strong% | 권장 액션 | 상태 | 요약
```

상세 패널은 아래 순서로 구성한다.

1. 전투 시간 요약
2. 품질 점수와 목표비
3. 데이터 누락/검증 메시지
4. 공격 카테고리 확률 요약
5. 공격별 DPS/확률/쿨다운/HitPhase 분석
6. MotionSet/Collision/Danger Ring/Telegraph 검증
7. BT/Intent 요약
8. 권장 보정 후보

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

### 품질 점수

`BalanceScenarioResult.BalanceScore`는 0~100 점수다. 자동 판정의 보조 지표이며, 점수가 높다고 플레이테스트를 생략하지 않는다.

| 요소 | 가중 |
|------|------|
| 플레이어 생존 시간이 목표 시간에 가까운가 | 34% |
| 몬스터 처치 시간이 목표 시간에 가까운가 | 34% |
| 기준 시간 동안 공격 기회가 충분한가 | 18% |
| 특정 공격 하나가 DPS를 과점하지 않는가 | 8% |
| 등급별 Strong% 상한을 크게 넘지 않는가 | 6% |

목표비는 다음처럼 해석한다.

```text
플레이어 생존 목표비 = PlayerTimeToDeath / TargetDuration
몬스터 처치 목표비 = MonsterTimeToDeathWithBreak 또는 MonsterTimeToDeath / TargetDuration
```

대략적인 운영 기준은 아래와 같다.

| 점수 | 해석 |
|------|------|
| 80~100 | 정적 추정상 양호. 리플레이/플레이테스트 검증으로 이동 |
| 60~79 | 큰 결함은 없지만 특정 축 조정 권장 |
| 40~59 | 목표 시간/공격 구성/피해량 중 하나 이상 재조정 필요 |
| 0~39 | 데이터 누락 또는 전투 성립성 문제 우선 해결 |

`RecommendedAction`은 점수와 별개로 가장 먼저 확인할 조정 후보를 표시한다. 예를 들어 플레이어 생존 목표비가 낮고 특정 공격 DPS 비중이 35%를 넘으면 `최대 기여 공격 피해/가중치 하향`을 우선 제안한다.

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
│ actorId | score | 플레이어 생존 | 몬스터 처치 | status | action │
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

## 사용 방법

### 1. 읽기 전용 분석

가장 안전한 기본 흐름이다. SO 값을 수정하지 않고 현재 데이터의 상태만 확인한다.

1. Unity 에디터에서 `UPlayGround/게임플레이/밸런스/밸런스 디자이너`를 연다.
2. `Actor Database`가 자동으로 잡히지 않으면 `Assets/10.Datas/Actor/DataBase/ActorDatabase.asset`을 지정한다.
3. 플레이어 기준값을 정한다.
   - 빠른 확인: `Scenario`를 비우고 임시 분석 조건을 입력한다.
   - 반복 검증: `Scenario ← Player` 또는 `Scenario ← Party`로 `BalanceScenarioAsset`을 생성한 뒤 사용한다.
4. 좌측 목록에서 몬스터를 선택하고 `Analyze Selected`를 누른다.
5. 전체 몬스터를 비교하려면 `Analyze Database`를 누른다.
6. 결과 테이블에서 `Score`, `Status`, `권장 액션`을 먼저 본다.
7. 상세 패널에서 전투 시간, 목표비, 검증 메시지, 공격 기여도를 확인한다.
8. 결과를 보존하려면 `Export CSV`로 내보낸다.

### 2. 결과 해석

| 항목 | 해석 |
|------|------|
| `Score` | 0~100 정적 품질 점수. 80 이상이면 정적 추정상 양호, 60 미만이면 조정 후보 |
| `Status` | `Stable`, `TooEasy`, `TooLethal`, `Stalled`, `InvalidData` 중 하나 |
| `플레이어 생존` | 현재 가정에서 플레이어 HP가 0이 되기까지의 예상 시간 |
| `몬스터 처치` | 현재 가정에서 몬스터 HP가 0이 되기까지의 예상 시간 |
| `Strong%` | 사용 가능 공격 풀 안에서 Heavy+Skill 계열 선택 확률 |
| `권장 액션` | 가장 먼저 확인할 조정 후보. 자동 적용 결과가 아니라 디자이너 검토 출발점 |

상세 패널의 공격 기여도에서 한 공격의 `DPS%`가 35%를 넘으면 그 공격이 전투 위험도를 과점하고 있을 가능성이 높다. `TooLethal`과 함께 나타나면 해당 공격의 `damage`, `selectionWeight`, `cooldown`, `Danger Ring/Telegraph`를 우선 확인한다.

### 3. 누락 데이터 생성

누락 데이터 생성은 에셋을 만든다. 실행 전 git 상태를 확인하고, 생성 결과를 리뷰한다.

1. `Balance Designer`에서 대상 몬스터를 선택한다.
2. 상단 요약의 `Generate Missing` 버튼이 활성화되어 있으면 누락 데이터가 있다는 뜻이다.
3. `Generate Missing`을 누르면 다음 데이터가 필요에 따라 생성/연결된다.
   - `ActorStatSO`
   - `EnemyAttackDataSO`
   - `EnemyBehaviorSO`
   - `BehaviorTreeAsset`
   - 누락된 `MonsterScalingSO` 연결
4. 전체 누락 보정이 필요하면 `Generate Missing All`을 사용한다.
5. 생성 후 자동 재분석 결과를 확인하고, 생성된 경로를 다이얼로그에서 확인한다.

### 4. 몬스터 스탯 재레벨링

몬스터 스탯만 배치 생성/갱신하려면 `UPlayGround/게임플레이/밸런스/몬스터 스탯 생성기`를 사용한다.

1. `Monster Scaling`과 `Actor Database`를 지정한다.
2. `Generate Missing (All)`은 `statData`가 없는 몬스터만 생성한다. 기존 수동 조정값은 건드리지 않는다.
3. 기존 `statData`까지 커브 기준으로 다시 계산하려면 대상 행을 체크한 뒤 `Apply Selected (덮어쓰기)`를 누른다.
4. 덮어쓰기는 기존 `ActorStatSO` 에셋을 제자리 갱신하므로 같은 에셋을 공유하는 몬스터도 함께 영향을 받을 수 있다.
5. 보스나 손튜닝 몬스터는 체크 해제로 보호한다.

### 5. 권장 보정 적용

상세 패널의 `권장 보정` 섹션은 목표 전투시간 기준으로 HP와 피해 배율을 역산한다.

| 버튼 | 변경 대상 |
|------|-----------|
| `Apply HP` | `ActorDefinitionSO.statData.MaxHealth` |
| `Apply Damage` | `ActorDefinitionSO.attackData.skills[*].baseInfo.hitPhases[*].damage` |

적용 전 확인 다이얼로그가 뜨며, `Undo`가 가능하다. 단, 공유 SO를 수정하면 같은 SO를 참조하는 다른 몬스터도 함께 바뀐다. 적용 후에는 자동 재분석되므로 `Score`, `Status`, `권장 액션`이 의도대로 바뀌었는지 확인한다.

### 6. 데이터셋 추출

전체 데이터 분포를 보고 싶으면 `UPlayGround/게임플레이/밸런스/밸런스 데이터 추출기`를 사용한다.

1. 창을 열면 플레이어 공격, 몬스터 공격, 플레이어 스탯, 몬스터 스탯을 스캔한다.
2. 탭별로 이상치와 평균 범위를 확인한다.
3. `Export CSV`로 외부 스프레드시트에서 비교한다.

---

## 테스트 방법

### 1. 컴파일 검증

코드 수정 후 최소 검증은 아래 명령으로 한다.

```powershell
dotnet build UPlayground.sln --no-restore
```

성공 기준:

| 항목 | 기준 |
|------|------|
| 오류 | 0개 |
| 경고 | 기존 Unity/패키지 경고는 허용. 새 밸런스 툴 관련 경고가 생기면 수정 |

현재 프로젝트는 Unity 패키지 참조 충돌 경고가 있을 수 있다. 빌드 실패 여부는 `오류 0개`를 기준으로 판단한다.

### 2. 읽기 전용 기능 테스트

SO를 변경하지 않는 기능부터 확인한다.

1. Unity에서 `밸런스 디자이너`를 연다.
2. `ActorDatabase`가 로드되는지 확인한다.
3. `Analyze Database`를 실행한다.
4. 결과 테이블에 몬스터별 `Score`, `Status`, `권장 액션`이 표시되는지 확인한다.
5. 행을 클릭했을 때 상세 패널이 갱신되는지 확인한다.
6. `Export CSV`를 실행해 CSV가 생성되고, 새 컬럼이 포함되는지 확인한다.
   - `balanceScore`
   - `playerSurvivalRatio`
   - `monsterKillRatio`
   - `recommendedAction`

실패 시 확인할 것:

| 증상 | 확인 |
|------|------|
| `InvalidData`가 많음 | `ActorDefinitionSO.statData`, `attackData`, `EnemyAttackDataSO.skills` 누락 |
| 모든 `Enemy DPS`가 0 | 기준 거리에서 사용 가능한 공격이 없는지, `selectionWeight/cooldown/damage` 확인 |
| `Score`가 0에 가까움 | 목표 시간 대비 생존/처치 시간이 극단값인지 확인 |

### 3. 시나리오 생성 테스트

1. `Scenario ← Player`를 실행한다.
2. `Assets/10.Datas/Balance/Scenarios/BalanceScenario_{Character}.asset`이 생성/갱신되는지 확인한다.
3. 생성된 시나리오가 현재 창의 `Scenario` 필드에 자동 연결되는지 확인한다.
4. `Analyze Database`를 실행해 시나리오 기준 분석값이 바뀌는지 확인한다.
5. `Scenario ← Party`를 실행해 파티 캐릭터별 시나리오가 생성/갱신되는지 확인한다.

주의: 기존 시나리오를 갱신할 때 인카운터/방어 가정값은 보존되고, 플레이어 파생 필드만 갱신되어야 한다.

### 4. 누락 데이터 생성 테스트

테스트 전 git 변경 사항을 확인한다.

```powershell
git status --short
```

절차:

1. 누락 데이터가 있는 테스트용 `ActorDefinitionSO`를 선택한다.
2. `Generate Missing`을 실행한다.
3. 생성 다이얼로그의 경로를 확인한다.
4. Inspector에서 `statData`, `attackData`, `behaviorData`, `behaviorTree`, `monsterScaling` 연결 상태를 확인한다.
5. 자동 재분석 후 `InvalidData`가 해소되는지 확인한다.
6. `git diff --stat` 또는 Unity Project 창에서 생성 에셋 범위를 확인한다.

성공 기준:

| 항목 | 기준 |
|------|------|
| `statData` | `ActorDefinitionSO.statData`에 연결 |
| 몬스터 스탯 | `MonsterStatBakeService` 경로로 생성/갱신 |
| 공격 데이터 | MotionSet이 있으면 Motion 기반 스킬 생성, 없으면 기본 공격 생성 |
| 분석 상태 | 필수 데이터 누락 Error 감소 |

### 5. 몬스터 스탯 생성기 테스트

1. `몬스터 스탯 생성기`를 연다.
2. `Monster Scaling`과 `Actor Database`가 지정되어 있는지 확인한다.
3. `Generate Missing (All)`을 실행한다.
4. 기존 `statData`가 있는 몬스터는 변경되지 않았는지 확인한다.
5. 테스트 대상 1개만 체크하고 `Apply Selected (덮어쓰기)`를 실행한다.
6. 해당 `ActorStatSO`의 HP/ATK/DEF/Poise가 `MonsterScalingSO` 기준으로 갱신되는지 확인한다.
7. `Undo`로 되돌릴 수 있는지 확인한다.

### 6. 권장 보정 적용 테스트

1. 분석 결과에서 `TooEasy` 또는 `TooLethal` 몬스터를 선택한다.
2. 상세 패널의 권장 보정 섹션을 확인한다.
3. `Apply HP` 또는 `Apply Damage` 중 하나만 적용한다.
4. 자동 재분석 후 목표비와 `Score`가 개선되는지 확인한다.
5. Unity `Edit/Undo`로 되돌리고 원래 값으로 복구되는지 확인한다.

공유 SO 위험 테스트:

1. 수정 대상 `ActorStatSO` 또는 `EnemyAttackDataSO`를 참조하는 다른 `ActorDefinitionSO`가 있는지 확인한다.
2. 공유 중이면 적용 전 별도 에셋으로 분리하거나, 영향 범위를 문서화한다.

### 7. 회귀 테스트 체크리스트

밸런스 툴 수정 후 다음 항목을 최소 확인한다.

| 체크 | 방법 |
|------|------|
| 컴파일 | `dotnet build UPlayground.sln --no-restore` |
| 창 열림 | `밸런스 디자이너`, `몬스터 스탯 생성기`, `밸런스 데이터 추출기` 메뉴 실행 |
| 전체 분석 | `Analyze Database` 실행 |
| CSV | `Export CSV` 실행 후 컬럼 확인 |
| 누락 생성 | 테스트 Actor 1개로 `Generate Missing` 실행 |
| Undo | `Apply HP`, `Apply Damage`, `Apply Selected` 후 Undo |
| 공유 데이터 | 적용 전/후 참조 에셋 영향 범위 확인 |

---

## 구현 단계

### 1단계: 읽기 전용 분석 툴

1. `BalanceScenarioAsset` 생성
2. `BalanceCombatEstimator`로 플레이어 생존 시간, 몬스터 처치 예상 시간, 품질 점수, 권장 액션 계산
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

### 레벨 스케일링 (✅ 구현: MonsterScalingSO)

`ActorDefinitionSO.level`은 몬스터 기준 레벨이고, `EnemyAttackInfo.requiredLevel` 필터에 바로 사용된다. 레벨·등급·난이도 → 몬스터 스탯/목표 피해 산출은 `MonsterScalingSO`로 구현했다. 플레이어의 `PartyMemberGrowthSO`/`PartyPowerCalculator`를 미러하되, 플레이어에 없는 등급 배율·난이도 배율 두 축을 추가로 얹는다. 성장 규칙은 플레이어와 동일한 `StatGrowthRule`/`GrowthFormula`를 재사용한다.

| 구성 | 위치 | 역할 |
|------|------|------|
| `MonsterScalingSO` | `Data/Actor/Enemy/` | L1 기준값 + 성장규칙 + 등급 배율 + 난이도 + 공격 base 피해 단일 소스 |
| `MonsterStatCalculator` | `Data/Actor/Enemy/` | `(scaling, grade, level, difficulty) → 스탯` 순수 계산 (런타임 재사용 가능) |
| `MonsterStatGeneratorWindow` | `Tool/Editor/Balance/` | `UPlayGround/Gameplay/Balance/Monster Stat Generator`. ActorDatabase 순회, 각 def의 `level`+`grade`로 평가, **누락 statData만 생성 + 프리뷰**, Undo |

| 스케일 축 | 적용 |
|-----------|------|
| HP 스케일 | 성장규칙(레벨) × 등급 `healthMultiplier` × 난이도 |
| 공격 스케일 | `AttackPower` 성장 × 등급 `attackMultiplier` × 난이도, 공격 데이터 `HitPhaseData.damage`는 `GetBaseAttackDamage`로 연동 |
| 방어 스케일 | 등급 `defenseAdd` 가산 (0~1 클램프) |
| Poise 스케일 | 성장 × 등급 `poiseMultiplier` |

몬스터는 런타임 레벨 스케일링을 하지 않고(MonsterActor가 `statData`를 직접 사용) 에디터 생성기가 bake하므로, 커브를 한 번 고치고 재생성하면 전체 몬스터 스탯이 일괄 반영된다. 플레이어 레벨별 스탯은 기존 `PartyMemberGrowthSO` + `PartyGrowthEditorWindow`로 이미 완성돼 있어 그대로 사용한다.

#### Phase 2 — 목표 전투시간 역산 (✅ 구현: BalanceTargetSolver)

`BalanceCombatEstimator`가 HP·피해에 선형인 점을 이용해 역산한다. Balance Designer 상세 패널의 "권장 보정" 섹션에서 확인·적용한다.

| 역산 | 공식 |
|------|------|
| 권장 HP | `플레이어 예상 DPS × 목표 처치시간` → MonsterTimeToDeath가 목표 시간에 수렴 |
| 권장 피해 배율 | `(플레이어 HP / 목표 생존시간) / 현재 적 DPS` → 모든 HitPhase.damage에 곱해 PlayerTimeToDeath를 목표 시간에 수렴 |

목표 시간은 `result.TargetDuration`(시나리오/등급 기준)을 사용한다. 적/플레이어 DPS는 이미 방어·회피 가정을 반영한 값이라, 같은 결과 기준의 배율은 가정이 상쇄되어 일관된다. 적용은 `Undo.RecordObject` + 확인 다이얼로그를 거치며, 적용 후 자동 재분석한다. (`BalanceTargetSolver.ApplyHealth` / `ApplyDamageScale`)

#### 몬스터 스탯 배치 재레벨링

`MonsterStatGeneratorWindow`는 누락 생성 외에 **선택 행 덮어쓰기**를 지원한다. 체크한 몬스터(기존 statData 포함)를 커브로 재생성하며, 기존 에셋은 제자리 덮어쓰기(참조 링크 유지) + Undo + 확인 다이얼로그를 거친다. 보스 등 손튜닝 에셋은 체크 해제로 보호한다.

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
