---
paths:
  - "Assets/02.Scripts/Ability/**"
  - "Assets/02.Scripts/Data/Ability/**"
  - "Assets/02.Scripts/Data/Editor/Ability/**"
  - "Assets/10.Datas/Ability/**"
  - "Assets/Tests/**/Ability/**"
---

# Gameplay Ability 시스템 상세 계약

Ability 데이터·코드 작업의 상세 규칙이다. AGENTS.md에는 단일 소스 원칙만 있고 상세는 여기에 있다. Claude Code는 위 경로 파일을 읽거나 고칠 때 자동으로 읽고, 다른 Agent와 MCP 중심 작업은 AGENTS.md 안내를 따라 직접 연다. 시스템 정본은 `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`다.

플레이어 공격·스킬 데이터의 단일 소스는 `AbilitySetSO`다.

```text
CharacterModelData.abilitySet
→ ActorAbilitySystem / PlayerCombatAbilityDataView
→ GameplayAbilitySO.Variant
→ UPlayGroundMotionAbilityPayloadSO
→ AbilityAttackInfo.motionKey
→ ActorAnimator.TryResolveAbilityMotion
→ PlayerActorAnimationMotionSet(WeaponType) / ActorAnimationMotionSet.abilityMotions
→ MotionSetAsset
```

- `GameplayAbilitySO`는 활성화 조건, 비용, 쿨다운, Variant 선택 정책을 소유한다.
- `UPlayGroundMotionAbilityPayloadSO`는 공용 `AbilityAttackInfo`를 소유하고, 실행 Motion의 단일 소스는 `AbilityAttackInfo.motionKey`다. Motion 참조와 공격 수치는 형제 필드로 분리되어 있다 — `motionKey`는 모션을, `baseInfo`는 히트 페이즈 수치만 소유하며 `baseInfo`는 모션 참조를 갖지 않는다. 모션 해석은 `baseInfo` 유무와 무관하므로 히트 페이즈가 없는 모션 전용 Ability도 실행된다. Ability Editor도 "실행 · 모션"과 "공격 · 히트 페이즈" 그룹으로 나누어 저작한다. `MotionKey`는 Ability/Variant 식별자를 포함하지 않는 **독립 문자열 키**이며 GAS는 이 값만 전달하고 `MotionSetAsset` 참조를 소유하지 않는다. 저작 규약은 `abilityId`에서 최상위 분류 접두사(`Actor.`/`Player.`/`Monster.`/`Boss.`)를 떨어낸 형태다 (`Actor.Ent.Attack.1.01` → `Ent.Attack.1.01`, `Boss.Bokusei.Counter.04` → `Bokusei.Counter.04`). 신규 생성은 `AbilityAssetFactory.BuildMotionKey`가 이 규약을 적용한다.
- 시퀀스형 Ultimate는 `UPlayGroundUltimateAbilityPayloadSO`가 `UltimateSequenceAsset`을 소유한다. `PlayerCombat`은 GAS Prepare/Commit/End와 시퀀스 재생을 조정할 뿐 캐릭터별 시퀀스 목록을 직렬화하지 않는다. 인게임 Motion은 Payload의 `motionKey`로 해석하고 `UltimateSequenceAsset.motionSet`은 에디터 미리보기에만 사용한다. 비용·쿨다운·Variant 선택의 단일 소스는 `GameplayAbilitySO`다.
- 실제 Motion 매핑은 `ActorAnimationMotionSet.abilityMotions`가 소유한다. 플레이어는 현재 `WeaponType`의 `PlayerActorAnimationMotionSet`에서 먼저 해석하고 `NoWeapon` 세트로 폴백한다.
- Payload 바깥에 중복 Motion 참조나 `AnimKey`/레거시 Ref 폴백을 다시 두지 않는다.
- `PlayerCombat`과 밸런스·검증 도구는 `PlayerCombatAbilityDataView`를 통해 같은 Set을 소비한다.
- `PlayerSkillSlot`은 입력 슬롯 바인딩이며 공격 수치의 원본이 아니다.
- 제거된 `PlayerAttackDataSO`, Variant V1 직접 실행 필드, 레거시 Resolver/폴백, 일회성 마이그레이션 도구를 다시 도입하지 않는다.
- 플레이어와 몬스터는 같은 Ability 구조를 사용하며, 몬스터 BT는 AbilitySet의 `aiSelectable` 공격 Ability를 선택·활성화한다.
- 플레이어 Ability 에셋은 `Assets/10.Datas/Ability/Migrated/`에 있고, UI Toolkit 기반 Ability Editor에서 편집·검증한다.
- Core asmdef는 프로젝트 비의존이지만 Ability/Effect/Set 정의와 Effect 수명주기 일부가 아직 Data/Actor에 있으므로 전체 시스템은 아직 외부 재사용 가능한 독립 패키지가 아니다.

## 데이터 스냅샷

2026-07-18 기준 플레이어/몬스터 통합 데이터는 AbilitySet 34개, GameplayAbility 에셋 482개, Variant/Payload 493개였다. 2026-08-08 스냅샷은 AbilitySet 39개, GameplayAbility 559개, Motion Payload 547개다. 현재 수치는 에셋을 직접 세어 확인한다.

## 모션 콘텐츠 보류

2026-07-31 Motion Key 이관 기준: Motion Payload 498개의 Key를 생성하고 Actor MotionSet 34개에 기존 기본/무기별 Motion 해석 결과를 이관했다. 레거시 `MotionReferenceSO` 타입과 490개 Ref 에셋은 제거했으며 직렬화 잔존 참조는 0건이다. Elemental Imbue 5종은 플레이어 무기 세트에 공통 Motion으로 연결했다.

- Dryad 공격 3개와 Training Dummy 공격 1개는 대응 Motion의 근거가 없어 미해결이다. 이 네 건은 임의 매핑하지 말고 콘텐츠 Motion을 확정한 뒤 연결한다.
- 미해결 상태는 Payload의 `AbilityAttackInfo.isMotionContentPending`(모션 콘텐츠 보류)으로 데이터에 명시되어 있다. 보류 공격은 몬스터 AI 선택 후보에서 제외되고(`EnemyAbilityRejectReason.MotionContentPending`), `AbilityDataValidator`의 전수 검증은 "모션 콘텐츠 보류 상태" Warning으로 보고한다. 콘텐츠 확정 전까지 예상된 Warning이므로 Error로 승격하지 않는다.
- `MonsterAbilitySetIntegrationTests`는 `aiSelectable` Ability의 Motion 해석 실패를 건너뛰지 않고 Payload·Motion Key 매핑·HitPhase 누락을 모아서 보고해야 하며, 보류 표시된 공격만 실패 대신 보류 목록으로 출력한다. 보류 공격의 Key가 해석되기 시작하면 이 테스트가 실패하므로 Motion을 연결할 때 보류 표시도 함께 끈다.
- Motion 확정 전에 만드는 몬스터 공격에도 같은 보류 표시를 쓴다.
