# 전투와 실행 데이터

## 정본

- [전투 저작](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [무기 모션 이식](../../Assets/docs/guide/WEAPON_MOTION_RETARGETING_GUIDE.md)
- [GAS 명세](../../Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md)
- [밸런스 도구](../../Assets/docs/guide/BALANCE_DESIGNER_TOOL_GUIDE.md), [레벨·등급 정책](../../Assets/docs/LEVEL_GRADE_COMBAT_BALANCE_POLICY.md)
- [스탯 계산 가이드](../balanceguide/STAT_AND_LEVEL_BALANCE_GUIDE.md): 계산식은 실제 `ActorStatContainer`와 소비 코드를 대조한다. 이 문서의 과거 공격 필드를 새 데이터 계약으로 복구하지 않는다.
- [사운드 셋업](../SOUND_SYSTEM_SETUP_GUIDE.md): 셋업 참고 자료이며 현재 Mixer·Addressable·서비스 계약과 대조 후 사용한다.

판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다. 공격 실행 경로는 AbilitySet → GameplayAbility/Payload → MotionKey → 액터 MotionSet 매핑이다. `EnemyAttackDataSO`와 제거된 레거시 Resolver를 재도입하지 않는다.

## 확인된 작업 기록

| 항목 | 근거 | 상태와 후속 작업 |
|---|---|---|
| 무기 등록 클립 조사 | [활성 조사 포인터](../../output/WeaponMotionRetargeting/active-inventory.txt), [수집 상태](../../output/WeaponMotionRetargeting/20261025_074248_inventory/progress.json) | 파일 조사 11,445/11,445, 오류 0 기록. 접촉·실제 그립·전체 조합 품질 승인과 구분. 활성 폴더와 후속 보정 자료 보존 |
| 무기 IK | [설계·프로토타입 범위](../WEAPON_IK_SYSTEM_DESIGN.md), [실제 구현](../../Assets/02.Scripts/GameActor/Component/Player/WeaponIKController.cs) | 코드에 Phase 0 보조손 그립 프로토타입 존재. 설계의 모든 단계가 구현된 것으로 보지 않는다 |
| 무기 보정 후속 작업 | [Frank 보정 자료](../../output/FrankWeaponRetarget/), [워프 작업](../../output/BokuseiWarpAssist/) | 적용·측정·복구 자료 보존. 필요한 조합의 최종 결과를 열고 실제 MotionSet과 대조 |

구 전투 밸런스 툴 문서는 제거된 공격 데이터와 개별 메뉴를 안내하여 정본에서 제외했다. [과거 문서](../_archive/combat/COMBAT_BALANCE_TOOLS_GUIDE.md)는 역사 비교에만 사용한다.

무기 조사 중간 진행 카운터는 [부산물 정리 기록](cleanup.md)에 따라 정리했다. 인계가 참조하는 004236과 최종 011445 체크포인트, 실제 수집 데이터와 후속 보정 증거는 유지한다. 조사 수집 종료를 전체 무기 이식 완료로 해석하지 않는다.

[위키 시작](README.md)
