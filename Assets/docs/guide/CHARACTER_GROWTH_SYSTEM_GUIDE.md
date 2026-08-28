# 캐릭터 성장 시스템 가이드

> **현재 성장 시스템의 단일 권위 문서다.**
> 레벨·EXP·스킬 포인트·스킬 트리·Attribute·Ability·Passive·저장 규칙은 이 문서를 우선한다.
> 전투 판정과 Motion 타이밍은 `COMBAT_SYSTEM_AUTHORING_GUIDE.md`, 저장 프레임워크는 `../Complete/SAVE_SYSTEM_GUIDE.md`를 따른다.

## 1. 확정 규칙

- 레벨은 스킬 포인트 지급과 단계 입구 노드의 레벨 조건에만 관여한다.
- 레벨 상승만으로 Attribute가 자동 증가하지 않는다.
- 캐릭터 기본 Attribute는 `PartyMemberGrowthSO.baseProfile`이 소유한다.
- 선택 성장은 `CharacterSkillTreeSO`가 소유하며 Attribute, Ability 해금·배율, Passive, 회피 쿨다운 규칙을 낸다.
- 일반 장비는 Attribute Modifier만 부여한다. 장비가 Passive를 부여하는 런타임 경로는 현재 없다.
- 출전 캐릭터는 전투 EXP 100%, 보유 중인 비출전 캐릭터는 50%를 받는다. 두 비율은 `PartyConfigSO`에서 조정한다.

## 2. 권위와 데이터 흐름

```text
몬스터 보상
→ RewardManager
→ PartyManager.GrantBattleExpToRoster
   ├─ Level / EXP
   └─ CharacterSkillProgressionService
      → CharacterGrowthResolver
      → ResolvedCharacterGrowth
         ├─ AttributeModifiers
         ├─ Ability Unlock / Scalar
         ├─ GrantedPassives
         └─ DodgeCooldownMultiplier
```

| 책임 | 단일 권위 |
|---|---|
| 캐릭터별 레벨·EXP | `PartyManager` |
| 필요 EXP·레벨 상한·기본 Attribute | `PartyMemberGrowthSO` |
| 레벨별 스킬 포인트 규칙 | `SkillPointRule` |
| 노드 정의·효과·스키마 버전 | `CharacterSkillTreeSO` |
| 취득 노드와 출처별 보너스 포인트 | `CharacterSkillProgressionService` |
| 성장 효과의 최종 읽기 모델 | `ResolvedCharacterGrowth` |
| 런타임 Attribute 저장·재계산 | `AttributeSetRuntime` |

`CharacterSkillProgressState.totalPoints`, `spentPoints`, `grantedUpToLevel`은 런타임 호환·표시용 계산 결과다. 새 세이브의 권위가 아니다.

## 3. EXP와 레벨

`GrantBattleExpToRoster(amount)`는 보상을 나누어 소진하지 않는다. 같은 기준 EXP에 멤버별 비율과 `ExperienceGain` 패시브 배율을 곱해 각 캐릭터에 지급한다.

```text
출전 BattleOrder: amount × battleMemberExpRate(기본 1.0)
비출전 Roster:    amount × reserveMemberExpRate(기본 0.5)
```

벤치 따라잡기 보정은 현재 사용하지 않는다. 벤치 EXP와 따라잡기를 동시에 강하게 적용해 캐릭터 선택의 성장 의미를 지우지 않기 위해서다.

레벨업 피드백은 활성 캐릭터에 `레벨 N`과 해당 레벨에서 새로 생긴 스킬 포인트를 표시한다. 전투 중 메뉴를 강제로 열지 않는다. 성장 메뉴 바로가기, 캐릭터 탭 배지, 새로 열린 노드 요약은 UI 후속 작업이다.

## 4. 스킬 트리 저작 규칙

캐릭터의 정상 조작에 필요한 기본 기술은 처음부터 사용할 수 있어야 한다. `grantedByDefault` 또는 기본 콤보 타수로 제공하고, 포인트는 플레이 방식의 전문화에 사용한다.

- 캐릭터당 세 분기를 기본으로 한다.
- 단계 입구 노드만 `requiredLevel`을 사용한다.
- 일반 분기는 `requiredNodeIds`와 비용으로 통제한다.
- `AbilityUnlockEffect`와 `PassiveGrantEffect`가 있는 노드는 `maxRank = 1`이다.
- 반복 랭크는 `StatDeltaEffect`, `AbilityScalarEffect`, `DodgeCooldownEffect`처럼 누적 의미가 명확한 효과에만 사용한다.
- 순수 수치 노드보다 수치와 플레이 규칙을 함께 바꾸는 노드를 우선한다.
- 노드 잠금 사유는 UI에서 레벨, 선행 노드, 부족 포인트를 문장으로 구분한다.

`CharacterSkillTreeValidator`는 플레이어블 트리 연결, 세 분기, 말단 플레이 스타일 효과, Ability ID, 단일 랭크 효과, 버전 마이그레이션을 검사한다.

## 5. 성장 결과 통합

`CharacterGrowthResolver`는 취득 노드를 한 번 순회해 `ResolvedCharacterGrowth`를 만든다. 전투와 UI는 개별 효과를 다시 순회하지 않고 이 결과를 읽는다.

```text
노드 취득 / 리스펙 / 로드 / 트리 재구성
→ 해당 캐릭터 캐시 무효화
→ 다음 조회에서 새 ResolvedCharacterGrowth 생성
→ Version 증가
```

같은 캐릭터를 변경 없이 다시 조회하면 같은 결과 인스턴스를 반환한다. Attribute 적용과 Passive 갱신이 서로 다른 노드 해석 결과를 보지 않도록 하기 위한 계약이다.

## 6. `PlayerActor.ApplyCharacterStats`의 실제 책임

캐릭터 교체 시 적용 순서는 다음과 같다.

```text
이전 장비·스킬트리 Attribute Effect 제거
→ 캐릭터 타입 교체
→ ApplyCharacterStats
   ├─ PartyManager.GetBaseStats의 baseProfile 값으로 기본 Attribute 초기화
   └─ 없으면 ActorDefinition.attributeProfile로 폴백
→ ResolvedCharacterGrowth.AttributeModifiers를 SkillTree Source Effect로 적용
→ 장착 장비 Attribute를 Equipment Source Effect로 적용
→ 캐릭터별 AbilitySystem 상태 복원
→ 기본 Passive + ResolvedCharacterGrowth.GrantedPassives 재구성
```

`ApplyCharacterStats`는 스킬트리·장비·Passive를 한꺼번에 적용하지 않는다. 기본 Attribute를 초기화하는 단계이며, 각 출처는 별도 핸들과 Source ID로 적용·제거된다. 살아 있는 활성 캐릭터가 노드를 취득하면 `ApplySkillTreeStatsForActiveCharacter`와 Passive 갱신만 수행해 현재 자원과 다른 Effect를 보존한다.

## 7. 저장과 복원

세이브 버전 3.4부터 캐릭터별 성장 데이터는 `PartyMemberSaveEntry`에 함께 저장한다.

```text
PartyMemberSaveEntry
├─ type
├─ level
├─ exp
└─ skillTree
   ├─ skillTreeVersion
   ├─ bonusPointGrants[]
   │  ├─ sourceId
   │  └─ amount
   └─ takenNodes[]
      ├─ nodeId
      └─ rank
```

로드 시 다음 값을 다시 계산한다.

```text
levelPoints     = SkillPointRule.TotalPointsAtLevel(level)
bonusPoints     = bonusPointGrants의 유효 amount 합
spentPoints     = 현재 트리의 node cost × 저장 rank 합
availablePoints = max(0, levelPoints + bonusPoints - spentPoints)
```

`totalPoints`, `spentPoints` 같은 결과값은 새 포맷에 저장하지 않는다. 3.3 이하 세이브의 `skillProgress`는 로드 시 한 번 읽고, 레벨 포인트를 제외한 차액을 `Legacy.SaveTotal` 출처로 변환한다.

### 스킬 트리 버전 변경

- 노드 ID와 의미는 가능한 한 영구적으로 유지한다.
- 노드 ID를 바꾸거나 제거해야 하면 `skillTreeVersion`을 1 증가시킨다.
- 이름 변경은 `nodeIdMigrations`에 `fromVersion / oldNodeId / newNodeId`를 기록한다.
- 여러 버전을 건너뛴 세이브는 버전 단계별 규칙을 순서대로 적용한다.
- 삭제 노드는 `newNodeId`를 비워 명시한다. 로드 시 해당 랭크를 제거하고 비용을 자동 환불한다.
- 비용이 바뀌면 저장된 랭크는 유지하고 현재 비용으로 `spentPoints`를 재계산한다.
- 현재 포인트보다 취득 비용이 많아진 세이브는 노드를 임의 삭제하지 않는다. 사용 가능 포인트를 0으로 고정하고 리스펙 선택권을 보존한다.

## 8. 장비와 Passive 경계

```text
캐릭터 데이터: 기본 Passive
스킬 트리:      추가 Passive 해금과 성장 규칙
일반 장비:      Attribute Modifier
```

장비 Passive가 필요해지면 `EquipmentGrantedPassive`처럼 명시적인 출처 계약과 저장·중복 정책을 먼저 만든다. 장비 랜덤 Attribute 롤에 숨은 Passive 부여를 섞지 않는다. 동일 Passive는 `PassiveAbilityController`의 스택 정책을 따르고 디버그 정보에서 각 Source를 구분할 수 있어야 한다.

## 9. 검증 완료 조건

자동 검증은 최소 다음 수직 슬라이스를 보장해야 한다.

```text
레벨업
→ 노드 3개 취득
→ 캐릭터 교체
→ 저장
→ 새 런타임으로 로드
→ Level / EXP / 출처별 보너스 / 노드 랭크 복원
→ total / spent / available 재계산
→ Attribute / Ability / Passive / 회피 규칙 재적용
```

현재 자동 테스트는 원인 기반 포인트 재계산, 구형 세이브 변환, 트리 버전 노드 ID 이관, 결과 캐시 무효화를 포함한다. 최종 완료 판정에는 Unity EditMode 테스트와 실제 저장 파일을 사용하는 Play Mode 스왑·재시작 검증이 모두 필요하다.

## 10. 후속 우선순위

1. 실제 저장 슬롯을 사용하는 성장 저장·교체·재시작 Play Mode 테스트
2. 스킬트리 UI의 전후 수치, 잠금 사유, 리스펙 결과 미리보기
3. `UI_Scene_SkillTree`의 화면·그래프·탐색·상세 Presenter 분리
4. Attribute Source별 최종값 디버거
5. Ability→Payload→MotionSet 전수 연결 검사기

Poise/Break, 방어 결과, HyperArmor, HitStop/TimeScale 통합은 성장 저장의 다음 전투 구조 단계로 분리해 진행하고 각각 독립적으로 검증한다.
