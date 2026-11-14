# 전투와 실행 데이터

## 정본

- [전투 저작](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [무기 모션 이식](../../Assets/docs/guide/WEAPON_MOTION_RETARGETING_GUIDE.md)
- [GAS 명세](../../Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md)
- [밸런스 도구](../../Assets/docs/guide/BALANCE_DESIGNER_TOOL_GUIDE.md), [레벨·등급 정책](../../Assets/docs/LEVEL_GRADE_COMBAT_BALANCE_POLICY.md)
- [스탯 계산 가이드](../balanceguide/STAT_AND_LEVEL_BALANCE_GUIDE.md): 계산식은 실제 `ActorStatContainer`와 소비 코드를 대조한다. 이 문서의 과거 공격 필드를 새 데이터 계약으로 복구하지 않는다.
- [사운드 셋업](../SOUND_SYSTEM_SETUP_GUIDE.md): 셋업 참고 자료이며 현재 Mixer·Addressable·서비스 계약과 대조 후 사용한다.

판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다. 공격 실행 경로는 AbilitySet → GameplayAbility/Payload → MotionKey → 액터 MotionSet 매핑이다. `EnemyAttackDataSO`와 제거된 레거시 Resolver를 재도입하지 않는다.

## 확인된 작업 기록

아이템 장비 수치 후속 보정은 [합격 보고](../work-status/item-economy-acceptance.md)에 기록했다. 기존 MonsterScalingSO의 HP 곡선/등급 배율만 보정하고 146액터 체력을 재베이크했다. 기준 라온 TTK 36/36·기여도 108/108·관련 EditMode 30/30 통과이며 실전 플레이 품질은 사용자 테스트 대기다.

| 항목 | 근거 | 상태와 후속 작업 |
|---|---|---|
| 무기 등록 클립 조사 | 조사 원자료는 2026-11-13 output 정리로 삭제됨 | 당시 파일 조사 11,445/11,445, 오류 0 기록. 접촉·실제 그립·전체 조합 품질 승인과 구분. 현행 에셋과 대조하려면 재조사 필요 |
| 무기 IK | [설계·프로토타입 범위](../WEAPON_IK_SYSTEM_DESIGN.md), [실제 구현](../../Assets/02.Scripts/GameActor/Component/Player/WeaponIKController.cs) | 코드에 Phase 0 보조손 그립 프로토타입 존재. 설계의 모든 단계가 구현된 것으로 보지 않는다 |
| 무기 보정 후속 작업 | Frank 보정·Bokusei 워프 작업 자료(2026-11-13 output 정리로 삭제) | 적용 결과는 현재 MotionSet·에셋이 정본이다. 필요한 조합은 실제 MotionSet과 대조해 재측정 |

구 전투 밸런스 툴 문서는 제거된 공격 데이터와 개별 메뉴를 안내하여 정본에서 제외했다. [과거 문서](../_archive/combat/COMBAT_BALANCE_TOOLS_GUIDE.md)는 역사 비교에만 사용한다.

무기 조사 자료는 [정리 기록](cleanup.md)에 따라 2026-11-13 output 정리에서 삭제했다. 과거 조사 수집 종료를 전체 무기 이식 완료로 해석하지 않는다.

분야별 설계·상태·검증 문서 목록은 [설계 문서 분류](design-folder-map.md)에서 `전투` 행을 본다. 가이드는 제외한다.

[위키 시작](README.md)

## 불속성 부여의 환경 점화 확장 설계

[환경 통합 계약](../../Assets/docs/design/world-art/environment/REACTIVE_ENVIRONMENT_INTEGRATION.md#f-불속성-부여로-불-지르기)에 기존 `GE_ElementalImbue_Fire`의 유효 부여 출처/우선순위를 공통 공격 snapshot의 환경 열로 연결하는 후속 설계를 추가했다. 일반 공격에 영구 열을 채우지 않으며 기존 AbilitySet·MotionKey·Effect 수명/스왑 정책과 다중탄 분배를 유지한다. 설계만 반영했으며 새 필드/런타임 연결/실제 부여 입력 검증은 미구현이다. [상태](../work-status/reactive-environment.md#정규-월드-연결-확장-설계--2026-11-13)·[RE-F 인수](../work-status/reactive-environment-tests.md#정규-월드-연결-확장-인수).

## 번개·천둥 SFX 리소스 (2026-11-13)

[조사·파일별 용도·라이선스·검증](../work-status/lightning-sfx-resources.md): CC0 천둥 5종을 [Lightning](../../Assets/06.Sounds/Environment/Lightning)에 확보했다. BigSoundBank WAV 4종(5.03~17.20초)과 rubberduck OGG 1종(5.26초), 원본과 배포·라이선스 근거는 [원본 목록](../../ArtSource/Audio/Lightning/README.md)에 보존한다. Unity AudioClip 임포트·로드 5/5 확인. 게임 데이터 연결·직접 청취·최종 청감은 미수행/사용자 테스트 대기다.
