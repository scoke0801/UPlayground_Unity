# Ability 태그 조건과 트리거

## 목차

- [현재 기준](#현재-기준)
- [태그 조건](#태그-조건)
- [트리거 선택](#트리거-선택)
- [필수 불변식](#필수-불변식)
- [Request 라우터](#request-라우터)
- [기존 마이그레이션 데이터](#기존-마이그레이션-데이터)
- [검증 대상](#검증-대상)

## 현재 기준

작업 전에 다음 파일을 읽는다.

- 설계 및 진행 상태: `Assets/docs/TODO/ABILITY_TAG_TRIGGER_SYSTEM_SPEC.md`
- 데이터 타입: `Assets/02.Scripts/Data/Ability/AbilityDefinitions.cs`
- Ability 필드: `Assets/02.Scripts/Data/Ability/GameplayAbilitySO.cs`
- 런타임 인덱스/발화: `Assets/02.Scripts/GameActor/Gameplay/Ability/ActorAbilitySystem.Triggers.cs`
- 전투 구독: `PlayerCombat.cs`, `EnemyCombat.cs`
- 검증: `Assets/02.Scripts/Data/Editor/Ability/AbilityDataValidator.cs`
- 데이터 회귀: `Assets/Tests/EditMode/Ability/AbilityDataIntegrityTests.cs`

TODO 문서의 단계표는 작성 시점 상태일 수 있다. 실제 코드와 테스트로 구현 완료 여부를 확인한다.

## 태그 조건

`AbilityTagRequirement`를 다음 의미로 사용한다.

- `requireAll`: 유효 항목 전부 보유해야 한다.
- `requireAny`: 유효 항목 중 하나 이상 보유해야 한다. 비어 있으면 검사하지 않는다.
- `blockAny`: 유효 항목 중 하나라도 보유하면 차단한다.
- `matchMode = Hierarchy`: 하위 계층 태그도 일치시킨다.
- `matchMode = Exact`: 문자열이 정확히 같은 태그만 일치시킨다.
- `expression`: 위 평면 조건으로 표현할 수 없는 중첩 조건을 담는다. 평면 조건과 **AND**로 결합된다.

### 중첩 태그 조건 (`expression`)

평면 조건 세 개로 표현 가능한 조건은 평면 조건을 쓴다. `(A AND NOT B) OR C`처럼 OR 안에 AND/NOT이 들어가는 경우에만 `expression`을 쓴다.

| 노드 | 의미 |
| --- | --- |
| `AbilityTagLeafExpression` | 태그 묶음을 `mode`(All/Any/None)로 판정. `matchMode`를 노드마다 지정한다 |
| `AbilityTagAllExpression` | 자식 전부 참 (AND) |
| `AbilityTagAnyExpression` | 자식 하나라도 참 (OR) |
| `AbilityTagNotExpression` | 자식의 부정 (NOT) |

- 최대 깊이는 `AbilityTagExpression.MaxDepth`(8)다. 초과하면 런타임에서 항상 실패하고 검증이 Error로 보고한다.
- 자식이나 유효 태그가 없는 노드는 참이다(조건을 걸지 않은 것으로 본다). 빈 노드를 "항상 거짓"으로 쓰지 않는다.
- 중첩 조건 실패는 항상 `MissingRequired`로 보고된다. 차단 의미를 실패 결과로 구분해야 하면 `blockAny`를 쓴다.

Owner, Source, Target의 태그 컨테이너가 다르므로 조건을 올바른 대상에 둔다. 새 조건은 `ownerTagRequirement`, `sourceTagRequirement`, `targetTagRequirement`를 사용한다. 같은 위치에서 레거시 `requiredTagIds`/`blockedTagIds`와 새 쿼리를 함께 채우지 않는다.

## 트리거 선택

먼저 사건의 성격과 활성화 주체를 구분한다.

| 필드 | 선택 기준 |
| --- | --- |
| `GameplayEvent` | 피격, 공격 요청, 입력처럼 순간 사건과 Payload가 필요한 사건 |
| `OwnedTagAdded` | 소유 태그가 새로 붙는 순간 한 번 시도할 상태 변화 |
| `OwnedTagPresent` | 태그가 존재하는 동안 유지하고 제거 시 취소할 오라·지속 상태 |
| `Request` | PlayerCombat/EnemyCombat의 상태 전환과 외부 검증을 거쳐야 하는 주 실행 |
| `Immediate` | 전투 상태 전환 없이 ASC가 직접 실행할 독립 Background Ability |

순간 사건을 태그 Add/Remove 펄스로 흉내 내지 말고 `GameplayEvent`를 사용한다. 트리거는 활성화 조건을 우회하지 않으며 Prepare → 외부 검증 → Commit 계약을 보존한다.

## 필수 불변식

- Trigger tag는 유효하고 GameplayTag Registry에 존재해야 한다.
- `Immediate`는 `concurrency == Background`에서만 사용한다.
- Immediate Background에는 `persistence.backgroundMaxDurationSeconds > 0`을 둔다.
- `OwnedTagPresent`는 Background Ability에서만 사용한다.
- Trigger tag를 같은 Ability의 `executionGrantedTagIds`에 포함해 자기 재발동 루프를 만들지 않는다.
- `cancelAbilitiesWithTag`가 자기 `abilityTagIds`를 계층 포함하지 않게 한다.
- 같은 프레임의 여러 Trigger는 `priority`로 의도를 명확히 한다.
- `retriggerIntervalSeconds`는 쿨다운과 별도이며 OwnedTagPresent에는 적용되지 않는다.
- `allowPreemption`은 기존 주 실행이 있어도 Request를 발행할 수 있게 할 뿐이다. 실제로 기존 주 실행을 취소해야 하는 반응은 `concurrency = CancelExisting`도 함께 사용한다. `RejectNew`이면 구독자의 Prepare 단계에서 충돌할 수 있다.
- `allowPreemption`과 `CancelExisting`은 기존 주 실행을 선점해야 하는 명시적 반응에만 사용한다. 일반 공격 요청에는 기본적으로 적용하지 않는다.
- Source/Tag가 같은 Trigger를 한 AbilitySet 안에 중복 노출하지 않는다.

## Request 라우터

모든 Trigger가 Request인 순수 라우터 Ability는 다음 계약을 따른다.

- TaskGraph가 없어도 된다.
- Variant는 필요하지만 실행 Payload가 없어도 된다.
- 대상 AbilitySet의 `additionalAbilities`에 정확히 한 번 연결한다.
- `playerSlots`와 `combatBindings`에 연결하지 않는다.
- Payload를 넣었다면 `aiSelectable`을 끈다. BT가 직접 선택해 트리거 경로를 우회하지 않게 한다.
- 요청 종류에 맞는 구독자가 실제 상태/Ability로 라우팅하는지 확인한다. 현재 플레이어·몬스터 피격 리액션은 각각 `PlayerActor.Combat.cs`와 `MonsterActor.cs`, 공격 요청은 `PlayerCombat.cs`와 `EnemyCombat.cs`가 처리한다.
- 피격 반응처럼 선점이 필요한 이벤트와 일반 공격 개시를 같은 `allowPreemption` 정책으로 묶지 않는다.

## 기존 마이그레이션 데이터

- 몬스터 Trigger: `Assets/10.Datas/Ability/Actor/TagTriggers/`
- 플레이어 Trigger: `Assets/10.Datas/Ability/Migrated/TagTriggers/`
- 연결 버전: `AbilitySetSO.tagTriggerMigrationVersion`

기존 폴더와 AbilitySet을 편집하기 전에 `AbilityDataIntegrityTests`가 기대하는 Trigger 수, 접두사, 연결 횟수, migration version을 확인한다. 새 Trigger가 의도적으로 전 세트에 포함되어야 한다면 데이터와 회귀 테스트의 계약을 함께 검토하되, 테스트 숫자만 통과시키기 위해 임의 수정하지 않는다.

## 검증 대상

태그 작업 뒤 최소한 다음을 확인한다.

- Registry 유효성 및 Exact/Hierarchy 선택
- Owner/Source/Target 조건의 실제 태그 소유자
- 자기 발동·자기 취소·Effect granted tag 연쇄의 순환
- Request 구독자와 선점 정책
- 선점형 Request의 `allowPreemption` + `CancelExisting` 조합과 비선점 요청의 `RejectNew` 동작
- 캐릭터 교체 시 Trigger 인덱스 재구성과 구독 해제
- 동일 프레임 중복 발화와 retrigger 간격
- OwnedTagPresent 제거 시 실행 취소
- AbilitySet에 정확히 한 번 연결
- `AbilityDataValidator` 오류 0
- 관련 EditMode 및 PlayMode 테스트 통과
