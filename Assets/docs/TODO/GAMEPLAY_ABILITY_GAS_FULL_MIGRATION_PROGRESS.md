# Gameplay Ability GAS 전체 마이그레이션 진행 기록

기준 문서: `GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_SPEC.md`  
최종 갱신: 2026-07-22

## 현재 단계

| Phase | 상태 | 비고 |
|---|---|---|
| 0 기준선 | 부분 완료 | 기존 Ability 회귀 테스트 유지. 미확정 MotionReference 4건은 콘텐츠 결정 대기 |
| 1 Attribute Runtime | 완료 | Core 구현, 모든 StatType 매핑, 전체 ActorStatSO Shadow 일치 자동 검증, 읽기 전용 Preview 제공 |
| 2 ASC/Debugger | 완료 | ASC 집합 루트·Snapshot·Recorder·Registry·7개 탭 구현. Stat Monitor도 GAS Attribute/Effect 조회로 전환 |
| 3 EffectSpec | 완료 | 기존 GameplayEffectSO 적용이 모두 Spec Bridge를 통과하며 Modifier/Tag/자원/주기/스택 권위는 Active Container가 소유 |
| 4 자원 수직 슬라이스 | 완료 | Health/Poise/UltimateEnergy와 슬롯 쿨다운을 Attribute/Cooldown Store 권위로 전환 |
| 5 Damage/Healing | 완료 | Damage/Healing/Poise Execution 적용. DamageResolver 일반 공식은 Core DamageExecution.Calculate 단일 공식 사용 |
| 6 Ability Task | 완료 | 부모 취소/구독 정리 Task Runtime과 Legacy Motion Task 구현. GameplayAbility 487개에 유효 Task Graph 연결 |
| 7 콘텐츠 시스템 | 완료 | 장비·체급 Modifier를 Infinite Effect, 성장값을 Base Transaction, 소모품/오브/스킬 회복을 EffectSpec으로 전환 |
| 8 레거시 삭제 | 부분 완료 | ActorStatContainer와 PlayerSkillGauge 타입 제거. 씬 GUID 보존용 무권위 셸과 ASC 소비 View 유지. StatType/StatModifier 데이터 입력 및 Ability/Effect/Tag 전환 컴포넌트는 잔존 |
| 9 안정화 | 부분 완료 | CLI/Unity 테스트 완료. Player Build와 전체 프리팹/씬 스모크는 열린 Editor 잠금으로 미실행 |

## 이번 구현 범위

- 프로젝트 비의존 Attribute 식별자, Base/Current, Modifier 집계, Clamp, 최대값 변경 정책, Transaction/Event
- 계층형 Tag 집계, Gameplay Event Router, Debug Recorder/Registry/Snapshot
- `GameplayEffectSpec`, Context, Capture, SetByCaller, Magnitude/Execution, Active Effect 수명주기
- Damage/Healing/Poise Execution
- Ability Task 수명주기와 Delay/Event/ApplyEffect/Sequence/Parallel Task
- 버전형 Attribute/Cooldown/Active Effect 저장 DTO
- `AbilitySystemComponent`와 Player/Monster Health, Poise, UltimateEnergy 연결
- GAS Runtime Debugger와 ActorStat→Attribute 읽기 전용 Preview
- 487개 GameplayAbilitySO에 공용 Legacy Motion Task Graph 연결
- `ActorStatContainer` → GUID 보존용 `LegacyActorStatMigrationFacade`, `PlayerSkillGauge` → ASC 소비 전용 `PlayerAbilityResourceView` 전환
- 기존 GameplayEffectSO의 Modifier/Tag/자원/주기/스택을 Spec/Active Effect Container 권위로 전환
- 장비·체급·성장·피해·회복·Poise·쿨다운의 직접 Stat/자원 권위 제거

## 자동 검증

- `UPlayGround.Ability.Core`, `UPlayGround.Ability.UPlayGround`, `UPlayGround.Actor`, `UPlayGround.UI`, `Assembly-CSharp`, Data/GameActor Editor, Ability Test 프로젝트: CLI 컴파일 오류 0
- Ability EditMode: 63개 중 62개 통과
  - 유일한 실패는 사전 미확정 데이터 4건: Dryad 공격 3개, Training Dummy 공격 1개 MotionReference 누락
- Ability PlayMode 수직 슬라이스: 2개 중 2개 통과
- GameplayAbility 487개 모두 Task Graph/Root/Legacy Motion Task 타입 참조 검증 통과
- `Assets/10.Datas/Ability`: 각 GameplayAbility의 `taskGraph` 1줄 외 자동 재직렬화 변경 0
- `Assets/03.Prefabs/`: 변경 0

## 다음 작업 순서

1. `GameplayEffectController`, `GameplayTagContainer`, `ActorAbilitySystem` 전환 컴포넌트를 ASC 내부 프로젝트 Runtime으로 물리 통합
2. `StatType`/`StatModifier`/`ActorStatSO` 데이터 입력을 안정 문자열 Attribute Profile/Effect Definition 에셋으로 변환
3. 캐릭터별 Health/Gauge/슬롯 Cooldown/AbilityRuntime 맵을 `AbilitySystemSaveData` 하나로 통합하고 구 세이브 마이그레이션 검증
4. Legacy Motion Task를 MotionSet/KCC 전용 순수 Task 조합으로 교체
5. 로컬 씬의 구 Stat 컴포넌트 GUID 참조를 제거한 뒤 `LegacyActorStatMigrationFacade` 삭제
6. Missing Script, managed reference/VFX, Save/Load, 캐릭터 교체, Player Build를 재검증하고 기준 문서를 Complete로 이동

## 차단 사항

- Dryad 3개와 Training Dummy 1개의 공격 Motion은 대응 근거가 없어 임의 매핑하지 않는다.
- 현재 프로젝트가 별도 Unity Editor에서 열려 있어 독립 BatchMode Player Build는 프로젝트 잠금으로 실행할 수 없다. 열린 Editor의 자동 테스트는 사용 가능하다.
- 작업공간의 로컬 씬 3개가 과거 Stat MonoScript GUID를 직렬화하고 있어, 씬 검증 없이 호환 셸을 삭제하면 Missing Script가 발생한다.
