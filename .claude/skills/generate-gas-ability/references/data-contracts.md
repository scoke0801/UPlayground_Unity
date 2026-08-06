# GAS 데이터 계약

## 목차

- [기준 파일](#기준-파일)
- [런타임 연결](#런타임-연결)
- [GameplayAbilitySO](#gameplayabilityso)
- [Variant와 Payload](#variant와-payload)
- [Motion과 HitPhase](#motion과-hitphase)
- [GameplayEffectSO](#gameplayeffectso)
- [AbilitySetSO](#abilitysetso)
- [Production Wizard 레시피](#production-wizard-레시피)
- [경로와 복제 정책](#경로와-복제-정책)

## 기준 파일

작업 직전에 다음 파일을 읽고 필드와 검증 규칙이 이 문서보다 최신인지 확인한다.

- 데이터: `Assets/02.Scripts/Data/Ability/`
- 프로젝트 Payload: `Assets/02.Scripts/Ability/UPlayGround/`
- 공격 데이터: `Assets/02.Scripts/Data/Combat/CombatData.cs`
- Motion 매핑: `Assets/02.Scripts/Data/Actor/Animation/ActorAnimationMotionSet.cs`
- 저작 도구: `Assets/02.Scripts/Data/Editor/Ability/`
- 검사기: `Assets/02.Scripts/Data/Editor/Ability/AbilityDataValidator.cs`
- 제작 가이드: `Assets/docs/guide/GAMEPLAY_ABILITY_PRODUCTION_GUIDE.md`
- 구조 스펙: `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`

가이드에 제거된 `MotionReferenceSO`나 레거시 필드가 남아 있으면 사용하지 않는다. 현재 타입과 자동 테스트를 따른다.

## 런타임 연결

```text
CharacterModelData / MonsterActorProfileSO / ActorDefinitionSO
→ AbilitySetSO의 유효 구성
→ GameplayAbilitySO
→ 우선순위와 조건으로 선택한 AbilityVariantDefinition
→ UPlayGroundMotionAbilityPayloadSO
→ AbilityAttackInfo.motionKey + AttackInfoBase.hitPhases
→ 실제 소유 ActorAnimationMotionSet.abilityMotions
→ MotionSetAsset / MotionEvent
```

Ability는 정책과 Variant 선택을 소유하고, Payload는 프로젝트 실행 데이터와 공격 수치를 소유하며, Actor MotionSet은 실제 Motion 에셋을 소유한다.

## GameplayAbilitySO

최소 계약을 확인한다.

- `abilityId`: 비어 있지 않은 프로젝트 전역 안정 ID로 설정한다. 파일명과 구분하며 숫자 suffix로 충돌을 숨기지 않는다.
- `presentation`: 표시명, 설명, 카테고리, 아이콘과 현지화 키를 설정한다. 아이콘과 현지화 키 부재는 현재 검사기에서 경고/정보가 될 수 있다.
- `abilityTagIds`: Ability 자체 분류·취소·차단에 쓰는 태그만 둔다.
- `activation`: Owner/Source/Target 태그, 지상 상태, 대상 정책/관계, 거리 조건을 둔다.
- `cost`, `cooldown`: 음수를 금지하고 비용 정책과 자원 종류를 일치시킨다.
- `concurrency`: 주 실행은 보통 `RejectNew` 또는 `CancelExisting`, 독립 지속 실행은 `Background`를 검토한다.
- `taskGraph`: 일반 실행 Ability에는 Root가 있는 지원 TaskGraph를 연결한다.
- `commitEffects`, `endEffects`: Commit 직후와 종료 시점의 Effect를 구분한다.
- `persistence`: 캐릭터 교체 정책과 Background 최대 시간을 의도적으로 설정한다.

거리 조건이 있으면 TargetPolicy가 `None`이 아닌지 확인한다. Self 대상은 최소 거리를 0으로 둔다.

## Variant와 Payload

- `variantId`를 Ability 안에서 의미 있고 안정적으로 유지한다.
- 조건을 통과한 후보 중 `priority`가 높은 Variant가 선택되므로 같은 조건의 더 낮은 우선순위 Variant가 영구 가려지지 않게 한다.
- 태그 조건은 새 `ownerTagRequirement`를 우선 사용하고 레거시 required/blocked 리스트와 혼용하지 않는다.
- Motion 실행은 `UPlayGroundMotionAbilityPayloadSO`를 사용하고 `attackInfo`를 null로 두지 않는다.
- 공격 수치와 AI 선택 정보는 `attackInfo.baseInfo` 및 형제 필드에 둔다.
- Variant의 owner/target Effect와 Ability의 commit/end Effect는 적용 시점과 대상이 다르므로 의도에 맞게 구분한다.
- 모든 Trigger가 `Request`인 순수 라우터 Ability는 TaskGraph와 Payload가 없어도 실행 가능으로 취급된다. 이 예외를 일반 Ability에 확대하지 않는다.

## Motion과 HitPhase

- MotionKey 문자열을 Ability ID 규칙만으로 추측하지 않는다. 같은 액터의 기존 Payload/매핑과 현재 `MotionKey` 타입을 확인한다.
- Payload `attackInfo.motionKey`와 소유 액터 `ActorAnimationMotionSet.abilityMotions` 키를 정확히 일치시킨다.
- 플레이어는 현재 무기 세트와 `NoWeapon` 폴백의 실제 해석 순서를 확인한다.
- 몬스터는 프로젝트 어딘가의 매핑이 아니라 해당 ActorDefinition 프리팹의 `ActorAnimator.MotionSet`에서 해석되는지 확인한다.
- Motion의 Collision 구간마다 참조되는 `hitPhaseIndex`가 유효해야 한다.
- 공격/Ultimate는 실제 피해가 필요하면 HitPhase를 하나 이상 둔다. 순수 모션·라우터 Ability는 근거 없이 더미 HitPhase를 추가하지 않는다.
- `hitboxGroupId`를 사용하는 경우 MotionEvent와 HitPhase와 부착 HitBox의 그룹 ID를 모두 맞춘다.
- Dashboard의 `부족한 HitPhase만 추가`는 기존 Phase를 수정·삭제하지 않는 보조 기능이다. 추가된 기본값은 콘텐츠 의도에 맞게 다시 검토한다.
- Dryad 공격 3개와 Training Dummy 공격 1개처럼 대응 Motion 근거가 없다고 기록된 항목은 콘텐츠 Motion 확정 전 임의 매핑하지 않는다.

## GameplayEffectSO

- `effectId`를 고유하고 안정적으로 둔다.
- `Instant`, `Duration`, `Infinite` 중 수명주기를 먼저 결정한다.
- Duration은 양수 지속 시간이 필요하고, 주기 Effect는 양수 `periodSeconds`가 필요하다.
- 중첩 정책이 `RejectNew`가 아니면 명시적 `stackingKey`를 둔다.
- `maxStackCount`는 1 이상으로 둔다.
- Modifier는 Registry에 존재하는 Attribute ID와 의도한 Flat/Percent 정책을 사용한다.
- `grantedTagIds`, 적용 required/blocked/immunity/dispel 태그와 `grantedAbilities`의 순환 가능성을 검토한다.
- 공유 Effect 변경 전 역참조를 확인한다. 한 Ability만 달라야 하면 Effect도 Fork할지 명시적으로 결정한다.

## AbilitySetSO

실행 표면을 섞지 않는다.

- `playerSlots`: Ability/Ultimate/ElementalImbue 입력 슬롯
- `combatBindings`: Light/Heavy/Jump/Dash 등 플레이어 전투 시퀀스
- `additionalAbilities`: 몬스터/보스 AI 후보, 명시 실행, 태그 트리거 라우터
- `charge`, `comboRoutes`, `comboLinkWindow`: 플레이어 전투 구성
- `baseSet` + `abilityOverrides`: 공용 Set을 상속한 파생 구성

Base 순환, 중복 슬롯, 중복 전투 바인딩, 같은 원본에 대한 Override 중복을 금지한다. Replace 원본은 Base Set의 유효 Ability여야 한다. Remove에는 replacement를 두지 않는다.

몬스터는 `MonsterActorProfileSO.abilitySet`을 공용 권위로 사용한다. `ActorDefinitionSO.EffectiveAbilitySet`이 프로필과 일치하는지 검사한다.

## Production Wizard 레시피

현재 `AbilityRecipeCatalog.cs`의 목록을 최종 기준으로 삼는다.

| 레시피 | 기본 연결 | 핵심 확인 |
| --- | --- | --- |
| `Player.Basic.Melee` | Player Combat Sequence | 선택 Motion, Collision, HitPhase |
| `Player.Skill.Projectile` | Player Skill Slot | 투사체 MotionEvent와 대상 정책 |
| `Monster.Basic.Melee` | Additional | `aiSelectable`, 자기 MotionSet 매핑 |
| `Monster.Heavy.Telegraph` | Additional | Telegraph와 실제 Collision |
| `Combat.AreaAttack` | Additional | AOE/범위 판정 근거 |
| `Support.HealOrBuff` | Player Skill Slot | Commit/End Effect와 대상 관계 |

레시피는 MotionEvent를 새로 만들지 않는다. Preview가 성공해도 선택 Motion과 레시피 의도가 맞는지 Dashboard에서 확인한다.

`Monster.Heavy.Telegraph` 레시피는 현재 공격 카테고리 등 기본값만 잡고 `useTelegraph`, Shape, FX 키, MotionEvent 방식을 자동 설정하지 않는다. 이 필드들을 명시적으로 저작한다. 현재 `EnemyCombat` 런타임은 Circle 전조만 지원하므로 다른 Shape를 선택하지 않는다. `useMotionEventTelegraph`를 켜면 실제 Motion에 `TelegraphEvent`가 있는지 `전투 데이터 검증기`로 확인하고, FX 키가 등록되어 있는지도 검사한다.

## 경로와 복제 정책

- 플레이어 기존 데이터: `Assets/10.Datas/Ability/Migrated/`
- 몬스터/보스 데이터: `Assets/10.Datas/Ability/Actor/`
- 공용 TaskGraph 등은 기존 실제 경로를 검색해 재사용한다.
- 저장 루트의 같은 액터 폴더와 명명 패턴을 먼저 따른다.
- 경로/ID 충돌은 자동 suffix로 우회하지 말고 Reuse, Replace, 새 안정 ID 중 하나를 명시적으로 선택한다.
- 안전 Fork 기본값은 Ability + Payload 독립 복제, TaskGraph/Effect 공유다. Motion은 새 키·새 매핑이 필요한지 의도에 따라 결정한다.
