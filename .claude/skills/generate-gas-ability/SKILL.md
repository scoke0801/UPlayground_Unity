---
name: generate-gas-ability
description: "UPlayGround 프로젝트의 Gameplay Ability System(GAS) 데이터를 생성·복제·구성·검증한다. 다음 상황에서 반드시 이 스킬을 사용한다: (1) Ability 데이터 제작 — 'Ability 만들어줘', '스킬 추가해줘', '궁극기 데이터 만들어', '몬스터 공격 Ability 추가', '보스 패턴 Ability' 처럼 GameplayAbilitySO/AbilitySetSO/GameplayEffectSO/Motion Payload 에셋을 새로 만들거나 고칠 때. (2) 연결 작업 — 'MotionKey 매핑해줘', 'HitPhase 맞춰줘', 'AbilitySet에 연결해줘', 'aiSelectable 켜줘', '플레이어 스킬 슬롯에 붙여줘'. (3) 공유/포크 — '이 Ability 복제해서 보스용으로', '공용 Set 상속해서 파생 만들어', 'Fork 해줘'. (4) 태그 트리거 — '피격 시 자동 발동', '태그 조건 붙여줘', 'Request 라우터', 'GameplayEvent 트리거'. (5) 진단 — 'Ability 데이터 검증', 'MotionKey 해석 안 됨', 'AbilityDataValidator 오류'. 반대로 Ability 런타임 C# 코드 자체를 수정하거나(ActorAbilitySystem, 태스크 노드 구현) GAS 아키텍처를 설명만 하는 요청에는 사용하지 않는다."
---

# UPlayGround GAS 데이터 제작

플레이어/몬스터 공격·스킬 데이터의 단일 소스는 `AbilitySetSO`다. 이 스킬은 그 아래 Ability/Variant/Payload/Effect/MotionKey를 **일관되게 저작하고 검증**하기 위한 절차다.

## ⭐ 가장 중요한 두 규칙

**1. `.asset` YAML을 손으로 쓰지 않는다.** GUID 복제, YAML 신규 작성, 텍스트 치환으로 에셋을 만들지 않는다. Unity의 Ability Editor / 양산화 Wizard / 제작 검증 대시보드의 Preview·Undo 경로, 또는 검증된 Editor API를 사용한다. Claude가 Unity를 못 띄우면 **에셋을 조작하지 말고**, 저작해야 할 내용을 데이터 계약으로 정리해 사용자에게 넘긴다.

**2. Motion / HitPhase 근거가 없으면 발명하지 않는다.** MotionKey를 abilityId 규칙만으로 추측하지 않는다. 실제 소유 액터의 `ActorAnimationMotionSet.abilityMotions`에서 해석되는지 확인한다. 근거가 없으면 임의 매핑 대신 **미해결로 보고**한다. (Dryad 공격 3개, Training Dummy 공격 1개는 이미 이렇게 기록된 미해결 항목이다 — 건드리지 않는다.)

## 매 작업 전에 현재 코드를 읽는다

데이터 스키마는 변한다. 문서·완료 가이드가 코드와 다르면 **코드가 이긴다.**

- `Assets/02.Scripts/Data/Ability/GameplayAbilitySO.cs`
- `Assets/02.Scripts/Data/Ability/AbilitySetSO.cs`
- `Assets/02.Scripts/Data/Ability/AbilityDefinitions.cs`
- `Assets/02.Scripts/Data/Ability/GameplayEffectSO.cs`
- `Assets/02.Scripts/Ability/UPlayGround/UPlayGroundMotionAbilityPayloadSO.cs`
- `Assets/02.Scripts/Data/Combat/CombatData.cs`
- `Assets/02.Scripts/Data/Actor/Animation/ActorAnimationMotionSet.cs`
- 검사기: `Assets/02.Scripts/Data/Editor/Ability/AbilityDataValidator.cs`

`git status --short`로 기존 사용자 변경을 먼저 기록하고, 그 변경을 덮어쓰지 않는다.

## 런타임 연결 사슬

```text
CharacterModelData / MonsterActorProfileSO / ActorDefinitionSO
→ AbilitySetSO 유효 구성
→ GameplayAbilitySO            (정책 · 비용 · 쿨다운 · Variant 선택)
→ AbilityVariantDefinition     (조건 + priority)
→ UPlayGroundMotionAbilityPayloadSO  (프로젝트 실행 데이터 · 공격 수치)
→ AbilityAttackInfo.motionKey + hitPhases
→ 소유 액터의 ActorAnimationMotionSet.abilityMotions
→ MotionSetAsset / MotionEvent
```

Ability는 **정책**을, Payload는 **실행 데이터**를, Actor MotionSet은 **실제 Motion 에셋**을 소유한다. 이 경계를 넘어 Motion 참조를 중복 저장하지 않는다.

제거된 `PlayerAttackDataSO`, Variant V1 직접 실행 필드, 레거시 Resolver/폴백, `MotionReferenceSO`를 다시 도입하지 않는다.

## 작업 절차

### 1. 요청을 데이터 계약으로 정리

변경 전에 짧게 적는다 — 소유자(플레이어/몬스터/보스), 목적(공격/방어/이동/지원/궁극기/패시브), 활성화 경로(입력 슬롯 / 전투 시퀀스 / BT 선택 / 명시 호출 / 태그 트리거), 대상·거리·지상 조건, 비용·쿨다운·동시 실행 정책, Variant 분기와 우선순위, Motion 근거와 Collision 이벤트 수, 필요한 HitPhase, owner/target/commit/end Effect, 연결할 AbilitySet과 Base/파생 여부, 저장 루트와 안정 ID.

의도를 바꾸는 핵심 값만 사용자에게 확인하고, 나머지는 **같은 액터·같은 역할의 기존 데이터**를 기준으로 제안한다.

### 2. 기존 데이터와 역참조 조사

1. 대상 `AbilitySetSO`와 실제 소유 액터의 `ActorAnimationMotionSet`을 찾는다.
2. 같은 소유자·역할·실행 경로의 Ability를 최소 하나 비교 기준으로 잡는다.
3. `abilityId`, `effectId`, 저장 경로, MotionKey 중복을 검사한다.
4. 공유 Ability/Payload/Effect/TaskGraph/MotionSet을 고치기 전에 대시보드 역참조 Preview로 영향 범위를 본다.
5. 공유 데이터의 **한 대상만** 바꿔야 하면 안전 Fork(Ability+Payload 복제, TaskGraph/Effect 공유) 후 파생 Set의 Replace/Add로 연결한다.

상세 소유권·연결 규칙은 [references/data-contracts.md](references/data-contracts.md).

### 3. 표준 도구 사용 (Unity)

`UPlayGround > 툴 런처 > 게임플레이 / 전투`:

- **Ability 에디터** — 검색·편집·탭 복사·복제
- **Ability 양산화 Wizard** — 레시피 기반 신규 생성, 공용/파생 Set 합성
- **Ability 제작 검증 대시보드** — Motion/HitPhase 분석, 안전 Fork, 역참조, 전체 검증
- **Ability Runtime Sandbox** — Prepare → Commit → End 수직 슬라이스

가장 가까운 레시피를 고르고 Preview 오류를 전부 없앤 뒤 적용한다. 레시피가 의도를 다 표현 못 하면 결과를 **초안**으로 쓰고 Ability Editor에서 명시적으로 보완한다.

### 4. 연결 순서를 지킨다

1. `GameplayEffectSO`를 만들거나 공유 Effect를 고른다.
2. `GameplayAbilitySO`의 안정 ID, 표시 정보, Ability 태그, 활성화·비용·쿨다운·동시 실행 정책.
3. 일반 실행 Ability에 지원되는 TaskGraph 연결. **모든 Trigger가 `Request`인 순수 라우터만** TaskGraph/Payload 부재가 허용된다.
4. Variant ID·조건·priority 결정 후 Payload 연결.
5. Motion 실행이면 `attackInfo`에 MotionKey와 공격 정보.
6. 실제 `MotionSetAsset`은 소유 액터 `abilityMotions`에 **같은 MotionKey**로 매핑.
7. Motion의 `BeginCollisionEvent.hitPhaseIndex`와 Payload `hitPhases`의 수·인덱스·HitBox 그룹을 맞춘다.
8. AbilitySet의 정확한 실행 표면에 연결.
9. Actor/Profile/CharacterModelData가 그 AbilitySet을 실제로 참조하는지 확인.

**MotionKey 저작 규약**: `abilityId`에서 최상위 분류 접두사(`Actor.`/`Player.`/`Monster.`)를 뗀 형태 (`Actor.Ent.Attack.1.01` → `Ent.Attack.1.01`). 신규는 `AbilityAssetFactory.BuildMotionKey`가 적용한다.

### 5. AbilitySet 실행 표면을 섞지 않는다

| 표면 | 용도 |
| --- | --- |
| `playerSlots` | 플레이어 입력 스킬 / Ultimate / ElementalImbue |
| `combatBindings` | 플레이어 기본 전투 시퀀스 (Light/Heavy/Jump/Dash) |
| `additionalAbilities` | 몬스터·보스 AI 후보, 명시 실행, 태그 트리거 라우터 |
| `charge` / `comboRoutes` / `comboLinkWindow` | 플레이어 전투 구성 |
| `baseSet` + `abilityOverrides` | 공용 Set 상속 파생 구성 |

- 동일 타입 몬스터는 `MonsterActorProfileSO.abilitySet` 공용 Set을 쓴다. 특수 개체는 공용 Ability를 직접 변형하지 말고 `baseSet` + Replace/Remove/Add.
- Request 전용 라우터를 입력 슬롯·전투 시퀀스에 연결하거나 `aiSelectable`로 만들지 않는다.
- 몬스터 공격의 `aiSelectable`은 **실제 HitPhase**와 **자기 액터 MotionSet에서 해석되는 MotionKey**가 둘 다 있을 때만 켠다.

태그 조건·자동 트리거를 만들 때는 [references/tag-triggers.md](references/tag-triggers.md)를 반드시 읽는다.

## 검증

작은 검사 → 전체 검사 순서로 올린다. 체크리스트 전문은 [references/validation.md](references/validation.md).

1. Ability Editor 선택 에셋 검증
2. 대시보드 Motion/HitPhase 교차 검증 + 역참조
3. `AbilityDataValidator.ValidateAll()` 전체 검증 — **자동 Fix는 쓰지 않는다**
4. EditMode Ability 테스트 (`AbilityDataIntegrityTests`, `AbilitySetCompositionTests`, `AbilityProductionPlannerTests`, `PlayerCombatAbilityDataViewTests`, `MonsterAbilitySetIntegrationTests`). 런타임 경로를 바꿨으면 PlayMode 수직 슬라이스도.
5. 보조 컴파일: `dotnet build UPlayGround.Data.csproj --no-restore` 등 (생성 `.csproj`가 최신일 때만 근거로 인정)
6. Play Mode 스모크: 입력/BT 선택 → Motion 재생 → Collision/HitPhase → Effect → 쿨다운 → 종료 정리
7. `git diff -- Assets/10.Datas/Ability Assets/03.Prefabs` 로 의도한 에셋만 바뀌었는지 확인

**MotionSet/Ultimate 타입 오류나 managed reference/VFX 누락이 보이면 저장이나 일괄 재직렬화를 즉시 중단한다.**

`AbilityDataValidator` 전수 검증은 미해결 Motion 4건을 Warning으로 보고한다 — 예상된 Warning이므로 Error로 승격하지 않는다.

## 결과 보고 형식

```
**생성·수정:**
- Ability / Payload / Effect / Set / Motion 매핑 (경로)

**연결:**
- 안정 ID, 실행 표면(슬롯 / combatBinding / additionalAbilities), 공유 vs Fork 결정

**검증:**
- 실행한 것: (검증기 / 테스트 / 컴파일) 결과
- 미검증: Unity Play Mode, 콘텐츠 Motion 확인 등

**미해결:**
- 근거 없어 연결하지 않은 항목
```

Unity를 실행할 수 없어 필수 검증이 남으면 **통과했다고 추정하지 말고** 미검증 항목을 정확히 적는다. 사용자 요청이 없으면 커밋하지 않는다.
