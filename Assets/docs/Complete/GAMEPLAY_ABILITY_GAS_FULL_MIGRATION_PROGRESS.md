# Gameplay Ability GAS 전체 마이그레이션 진행 기록

기준 문서: `GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_SPEC.md`  
최종 갱신: 2026-07-24

> [!IMPORTANT]
> GAS 코드·데이터 구조 마이그레이션 Phase 1~8은 완료했다.
> 다만 콘텐츠 근거가 없는 MotionReference 4건과 GAS 외부의 lilToon Player Build 오류 1건,
> 최종 수동 Play Mode 스모크가 남아 있으므로 전체 작업을 완료 처리하거나 문서를
> `Assets/docs/Complete/`로 이동하지 않았다.

## 현재 상태

| Phase | 상태 | 비고 |
|---|---|---|
| 0 기준선 | 부분 완료 | 자동 회귀 기준선 확보. Dryad 3건과 Training Dummy 1건의 MotionReference는 콘텐츠 결정 대기 |
| 1 Attribute Runtime | 완료 | 안정 `AttributeId`, Base/Current, Modifier, Clamp, Transaction/Event와 Profile 전환 완료 |
| 2 ASC/Debugger | 완료 | ASC 집합 루트, Snapshot, Recorder, Registry와 런타임 디버거 구현 완료 |
| 3 EffectSpec | 완료 | Modifier/Tag/자원/주기/스택 적용을 `GameplayEffectSpec`/Active Effect 권위로 전환 |
| 4 자원 수직 슬라이스 | 완료 | Health, Poise, UltimateEnergy, 슬롯 쿨다운을 ASC Attribute/Cooldown Store 권위로 전환 |
| 5 Damage/Healing | 완료 | Damage/Healing/Poise Execution 적용 및 공용 계산 경로 통합 |
| 6 Ability Task | 완료 | Task Runtime과 Motion Execution Graph로 플레이어/몬스터 Ability 연결 |
| 7 콘텐츠 시스템 | 완료 | 장비·성장·체급·회복·쿨다운을 Attribute/EffectSpec 구조로 전환 |
| 8 레거시 삭제 | 완료 | `ActorStatSO`, `StatType`, `StatModifier`와 구 저작/마이그레이션 도구·직렬화 폴백 삭제 |
| 9 안정화 | 진행 중 | 컴파일·테스트·Missing Script 통과. Player Build 산출은 성공했으나 lilToon 오류 1건과 수동 스모크가 남음 |

## 최종 구조

- `AbilitySystemComponent`가 Ability, Effect, Tag, Attribute, Cooldown의 액터 단일 집합 루트다.
- Player/Monster Health는 `Vital.Health`/`Vital.MaxHealth`, Poise는 `Vital.Poise`,
  UltimateEnergy는 `Resource.UltimateEnergy`가 단일 수치 권위를 가진다.
- Player/Monster 공격 데이터는 같은 `AbilitySetSO` → `GameplayAbilitySO` →
  `UPlayGroundMotionAbilityPayloadSO` → `MotionReferenceSO` 경로를 사용한다.
- 장비 Modifier, 파티 성장, 몬스터 스케일링, 밸런스 도구와 UI는 안정 `AttributeId`를 사용한다.
- Actor 초기값은 `AttributeProfileSO`를 사용하며 `ActorStatSO` 폴백은 없다.
- 플레이어 캐릭터별 Health/Gauge/Cooldown/Effect 저장은 세이브 포맷 3.0의
  `AbilitySystemSaveData` 단일 맵을 사용한다. ASC 스냅샷 스키마 버전은 2다.
- 487개 GameplayAbility가 공용 Motion Task Graph를 사용한다.

## 2026-07-24 완료한 Phase 8 작업

### 런타임과 계약

- `AbilitySystemComponent`의 StatType 순회·매핑을 제거하고
  `UPlayGroundAttributeDefaults.All/Get` 및 `AttributeId` API로 교체했다.
- `UPlayGroundAbilityOwnerPorts`, `DamageResolver`, `HitRequest`,
  Player/Monster 초기화와 공격 상태, 파티·사이클·치트 소비자를 `AttributeId`로 전환했다.
- `IPartyService.GetGrowthStats`를
  `IReadOnlyDictionary<AttributeId, float>` 계약으로 전환했다.
- Rest Growth, Party Detail, Inventory의 표시·비교 경로를 같은 Attribute ID 기반으로 통합했다.

### 저작·밸런스 도구

- 밸런스 계산기, Monte Carlo, 타깃 솔버, 데이터 추출기, 몬스터 Stat Bake,
  파티 성장 편집기와 검증기를 `AttributeProfileSO`/`AttributeId` 기반으로 전환했다.
- 구 `StatDomainPanel`을 `AttributeProfileDomainPanel`로 교체했다.
- `BalanceSnapshot.StatValue.statType`을 `attributeId`로 변경했다.
- BalanceScenario 11개의 `playerAttributeProfile`을 해당 Party Growth의
  `baseProfile`과 다시 대조해 유효 GUID·단일 대상·참조 일치를 확인했다.

### 레거시 제거

- 다음 타입/도구와 `.meta`를 제거했다.
  - `ActorStatSO`, `StatTemplateSO`
  - `ActorStatSOEditor`, `StatDomainPanel`, `StatDatabaseEditorWindow`
  - `ActorStatAttributeProfilePreviewWindow`
  - `EquipmentGrowthAttributeIdMigration`
  - `GameplayEffectAttributeIdMigration`
  - `UPlayGroundAttributeMapping`
- `StatType.cs`는 GUID를 보존해 `UPlayGroundAttributeDefaults.cs`로,
  `StatModifier.cs`는 `ModifierType.cs`로 이름을 정리했다.
- `Assets/10.Datas/Stat` 아래 레거시 ActorStat/Template 에셋 74개와
  해당 `.meta` 74개를 참조 검사 후 제거했다.
- `MonsterScaling_Default.asset`은 보존했다.
- 장비·GameplayEffect 등의 직렬화 `statType:` 75줄을 제거했다.
- 장비 40개 에셋의 구 `attackPower`, `critChance`, `critDamage`,
  `attackSpeed` 직렬화 160줄과 런타임 폴백을 제거했다.
- `GameplayEffectSO.resourceOperations`와
  `GameplayResourceOperation`/`LegacyResourceOperationExecution`을 제거하고,
  자원 변경을 EffectSpec/Execution 경로로 단일화했다.
- `AbilitySystemAuthorityMode`의 사용되지 않는 이중 권위 모드와
  `CombatActionDefinition.LegacyAttackData` 잔여를 제거했다.
- 캐릭터별 `CharacterHpEntry`/`AbilityRuntimeSaveData` 이중 저장을 제거하고
  `CharacterAbilitySystemSaveEntry`의 `AbilitySystemSaveData` 한 종류만 저장한다.
- 스킬 슬롯 쿨다운 그룹을 `Ability.SkillSlot.{slot}`으로 통일했으며,
  `Legacy.SkillSlot` 키는 더 이상 읽거나 쓰지 않는다.
- `GameplayAbilitySO`의 Task Graph를 필수 실행 데이터로 승격했다.
  Graph/Root 누락은 검증과 활성화에서 즉시 실패하며 레거시 실행 폴백은 없다.
- 테스트와 런타임 식별자에 남아 있던 `Migration`, `LegacyResourcePort`,
  `MatchesLegacyFormula` 명칭도 현재 역할에 맞게 정리했다.
- 세이브 3.0 이전의 분리형 GAS 필드는 호환 변환하지 않는다.
  이전 세이브를 열면 해당 캐릭터의 ASC 상태는 현재 기본값으로 시작한다.
- 제거 대상 스크립트 GUID가 `.asset`, `.prefab`, `.unity`에서 더 이상
  참조되지 않음을 확인했다.

현재 정확 검색 결과:

- C#의 `StatType`, `ActorStatSO`, `StatModifier`, `ActorStatContainer`,
  `PlayerSkillGauge`, `LegacyActorStatMigrationFacade`: 0건
- `.asset`/`.prefab`/`.unity`의 `statType:`, `statData:`: 0건
- 장비의 `attackPower:`, `critChance:`, `critDamage:`, `attackSpeed:`: 0건
- C#·Ability 데이터의 `AbilityRuntimeSaveData`, `CharacterHpEntry`,
  `GameplayResourceOperation`, `resourceOperations`,
  `AbilitySystemAuthorityMode`, `CombatActionDefinition`,
  `Legacy.SkillSlot`: 0건
- GAS 관련 런타임·테스트 범위의 `legacy`/`레거시`/`migration` 표기: 0건
- Ability 에셋의 `resourceOperations:` 직렬화: 0건
- Ability 에셋의 null `taskGraph`: 0건

## 최종 자동 검증

### 컴파일

- Unity 스크립트 컴파일 오류 0
- 다음 CLI 프로젝트 오류 0:
  - `UPlayGround.Ability.Core.csproj`
  - `UPlayGround.Data.csproj`
  - `UPlayGround.Actor.csproj`
  - `UPlayGround.UI.csproj`
  - `UPlayGround.Data.Editor.csproj`
  - `Assembly-CSharp-Editor.csproj`
  - `UPlayGround.Ability.Tests.csproj`
- CLI 경고는 기존 SDK 참조 충돌과 외부 패키지 경고다.

### 테스트

- 레거시 최종 정리 후 Ability EditMode: 68개 중 67개 통과, 1개 실패
  - 실패 테스트:
    `MonsterAbilitySetIntegrationTests.몬스터_Ability_Payload는_실행_가능한_공격_정보를_가진다`
  - 보고된 항목은 기존 미확정 MotionReference 4건뿐이다.
    - `GA_dryad_01_Attack_1`
    - `GA_dryad_02_Attack_2`
    - `GA_dryad_03_Attack_3`
    - `GA_Training_Dummy_01_Attack_1`
  - 결과: `Temp/GasLegacyCleanupEditModeResults.xml`
- Ability PlayMode 수직 슬라이스: 2개 중 2개 통과
  - 결과: `Temp/GasLegacyCleanupPlayModeResults.xml`

### 에셋과 빌드

- 프로젝트 프리팹·씬 Missing Script: 0개
- 검증 후 `Assets/03.Prefabs`와 `Assets/01.Scenes` 추적 변경: 0개
- `git diff --check`: 공백 오류 0
- StandaloneWindows64 Development Player 생성 성공
  - 결과: `BuildResult.Succeeded`
  - 출력: `Temp/GasMigrationBuild/UPlayground.exe`
  - 빌드 시간: 487.074초
  - BuildReport: 오류 1, 경고 444
  - 오류는 GAS 코드가 아니라 기존 lilToon의
    `State comes from an incompatible keyword space`
    (`Hidden/lilToonTransparent` ↔ `Hidden/ltspass_transparent`)다.
  - 따라서 실행 파일은 생성됐지만 엄격한 Player Build 오류 0 게이트는 미통과로 기록한다.

## 남은 작업 목표

1. 콘텐츠 담당 결정 후 Dryad 공격 3개와 Training Dummy 공격 1개의 실제 Motion을
   확정하고 `MotionReferenceSO`를 연결한다.
   - 대응 근거가 생기기 전에는 임의 매핑하지 않는다.
2. 현재 작업 트리의 렌더링/lilToon 변경과 셰이더 캐시를 별도 점검해
   StandaloneWindows64 Development BuildReport 오류를 0으로 만든다.
   - GAS 작업과 무관한 사용자 렌더링 변경은 원복하지 않는다.
3. MotionSet/Ultimate 전체 managed reference 1,638개와 VFX 참조 168개의
   전용 누락 검증을 최종 상태에서 다시 실행한다.
4. Unity Play Mode에서 다음 수동 스모크를 수행한다.
   - Player/Monster Damage, Healing, Poise Break/Recovery
   - UltimateEnergy 획득·소비와 슬롯 Cooldown
   - 장비 교체와 파티 성장 반영
   - 캐릭터 교체, Save/Load 복원
   - 플레이어/몬스터 Ability 실행과 취소
5. 위 게이트와 알려진 Motion 4건을 모두 해결한 뒤
   `GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_SPEC.md`와 이 문서를
   `Assets/docs/Complete/`로 이동한다.

## 작업 트리 주의 사항

- 사용자가 별도로 진행한 Input/UI/Dialogue/FlowGraph/렌더링 변경이 함께 있다.
- GAS와 무관한 수정·삭제·신규 파일을 원복하지 않는다.
- `Assets/10.Datas`의 사용자 데이터와 Unity 자동 재직렬화 변경을 구분한다.
- 사용자가 요청하지 않았으므로 커밋하지 않았다.
- 이번에 제거한 레거시 소스·에셋은 커밋 전 작업 트리이므로 Git에서 복구할 수 있다.
