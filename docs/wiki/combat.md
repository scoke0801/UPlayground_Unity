# 전투와 실행 데이터

2026-11-14 사용자 정상 컴파일 뒤 MCP에서 충돌 필터 회귀 포함 관련 43/43, E01 60초 구르기 0회/최대 17.904m/복귀 재교전을 확인했다. 실제 두 궁수에 공격이 명중했으나 구르기 후 공격 기회는 미검증이다. E03 급경사 복귀는 NavMesh `PathPartial`로 실패했다. 추가 국소 우회 실험은 효과가 불충분해 원복했다. [실측·전체 AI 검사 151/152·남은 검증](../work-status/monster-bt-improvement/MCP_VALIDATION.md)을 확인한다.

## 정본

- [투사체 패리 구현 요청서](../../Assets/docs/requests/PROJECTILE_PARRY_IMPLEMENTATION_PROMPTS.md): 확정 설계 기반 전체 요청과 P0~P4 단계별 프롬프트. 요청서 작성 완료이며 런타임 구현은 미착수다.

- [무기별 궁극기 연출 + 사이드 얼굴 패널 설계](../../Assets/docs/design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md) (2026-11-14): 사용자 레퍼런스(화면 좌측 얼굴 패널, 전투는 계속 보임)에 맞춰 기존 `UltimateSequenceAsset`·연출 스테이지 위에 얼굴 패널 이벤트와 무기 10종 연출 템플릿을 얹는 설계다. 얼굴 이미지는 Codex CLI ImageGen, 적용은 궁극기만. **설계만 반영, 구현·이미지·Blender 작업 미착수.** 패널 속도·길이는 영상 근거가 없는 제안값이고 "스칼렛 스트링스"는 웹에서 확인하지 못했다.

- [투사체 패리 설계](../../Assets/docs/design/combat/PROJECTILE_PARRY_SYSTEM_DESIGN.md) · [구현 요청](../../Assets/docs/requests/PROJECTILE_PARRY_IMPLEMENTATION_PROMPTS.md) · [상태·조사 근거](../work-status/projectile-parry.md): P0에서 실제 적 Bow Attack1/DefaultArrow와 P09 Staff Bolt 발사 연결 및 보상·귀속 소비자를 식별했다. P0~P2 코드 반영 및 관련 EditMode 39건/PlayMode 5건 통과. 실제 Bow Attack1/P09Staff Bolt와 형태·음향 SO 연결 반영. 확장 EditMode 109/110(범위 밖 WoodlandWolf 정책 누락1). 관련 최종 EditMode103/103 통과. Editor 일시 차단 해소 뒤 최신 PlayMode7/7도 통과했다. P0~P4 구현·선택 검사 인계 완료이며 전체 Ability 검사 범위 밖 오류7건은 별도 기록한다. [최종 대조·원본](../work-status/projectile-parry/P4.md)과 [PP01~PP10 사용자 명세](../work-status/projectile-parry/USER_TEST.md)를 따른다. 사용자 플레이 인수는 대기다.

- [궁수 화살 높이 보정](../work-status/archer-projectile/STATUS.md): MotionSet 1,421개에서 몬스터 화살 모션 11개·17개 발사 이벤트 전수조사. 낮은 발사점 6개와 직선 화살 16개의 몸통 조준을 수정했다. 보조 C# 컴파일 오류 0·Unity 데이터 검사 오류 0. Play 유지로 Unity 정상 컴파일·회귀 실행·실제 경사 교전은 검증 대기다.

- [전투 저작](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [무기 모션 이식](../../Assets/docs/guide/WEAPON_MOTION_RETARGETING_GUIDE.md)
- [GAS 명세](../../Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md)
- [밸런스 도구](../../Assets/docs/guide/BALANCE_DESIGNER_TOOL_GUIDE.md), [레벨·등급 정책](../../Assets/docs/LEVEL_GRADE_COMBAT_BALANCE_POLICY.md)
- [스탯 계산 가이드](../balanceguide/STAT_AND_LEVEL_BALANCE_GUIDE.md): 계산식은 실제 `ActorStatContainer`와 소비 코드를 대조한다. 이 문서의 과거 공격 필드를 새 데이터 계약으로 복구하지 않는다.
- [사운드 셋업](../SOUND_SYSTEM_SETUP_GUIDE.md): 셋업 참고 자료이며 현재 Mixer·Addressable·서비스 계약과 대조 후 사용한다.

판단은 BT, 수치는 GAS, 타이밍은 MotionSet이 소유한다. 공격 실행 경로는 AbilitySet → GameplayAbility/Payload → MotionKey → 액터 MotionSet 매핑이다. `EnemyAttackDataSO`와 제거된 레거시 Resolver를 재도입하지 않는다.

## 확인된 작업 기록

- [Player Katana 공격 FX 보완](../work-status/player-katana-fx.md) (2026-11-14): 실제 `Set_Katana`의 라온 Flow/FrankWeapon 실행 경로와 공중·돌진·교대 공격 FX 누락 조사 완료. 기존 베기/찌르기 구매 FX를 기준으로 저작 진행 중이며, 궤적 재측정과 적용 검증은 대기다.

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

## 몬스터 BT 행동 개선 (2026-11-14)

Staff 원거리 추가 확장과 레벨 해금은 [별도 상태·직접 테스트 R01~R05](../work-status/monster-bt-improvement/STAFF_RANGED_EXPANSION.md)에 기록한다. 최종 투사체 4·Beam 4·근접 2종의 데이터·BT를 연결했다. Burst20/Homing30/DoubleBeam40/FanBeam50 잠금과 실제 남녀 14종의 GAS 해금 목록(3/5/6/7/8/9/10종)을 확인했다. BT 87노드 오류/경고 0, Ability 신규 오류 0. 정식 NUnit은 테스트 경합·MCP 통신 단절로 결과 미확보이며 신규 4종 인게임 시연·미술 품질도 사용자 테스트 대기다.

Staff 공격 구성을 투사체 2·Beam 2·근접 2종으로 확장한 [후속 작업·검증·직접 테스트 A01~A05](../work-status/monster-bt-improvement/STAFF_ABILITY_EXPANSION.md): 서로 다른 6개 클립·전용 투사체 4개·Ability/모션/BT 연결을 반영했다. 관련 EditMode 15/15, BT 81노드 오류/경고 0, 전체 Ability 신규 오류 0/기존 7 Error 유지. 이후 다른 변경의 펫 테스트 오류, 최종적으로 ProjectileRuntime.cs의 IsAlive·인수 타입 컴파일 오류로 실제 Play 진입이 막혔다. 신규 6종의 인게임 실행·미술 품질은 차단 해소 후 사용자 테스트 대기다.

Staff 전용 원거리 마법사 행동은 [설계](../../Assets/docs/design/combat/monster/STAFF_RANGED_MAGE_BT_DESIGN.md)와 [상태·사용자 테스트](../work-status/monster-bt-improvement/STAFF_RANGED_MAGE.md)에 분리했다. 궁수 BT의 8m 제한·근접 접근과 공유 전략의 Bolt 반복 차단을 확인해 전용 BT·BehaviorData·전략·14종 연결·제작표를 반영했다. importer/그래프 오류·경고 0, Unity 연결 14/14, Staff 액터 검사 이슈 0. Play에서 저레벨 11.1~11.8m 사격 유지, 4m 압박 후 후퇴·재공격, 레벨 20 Bolt/Beam 시전·타깃 상실 후 Patrol을 확인했다. 벽·급경사·실제 피격/공략 체감은 사용자 테스트 대기이며 기존 P3~P5 전체 완료를 뜻하지 않는다.

[설계](../../Assets/docs/design/combat/monster/MONSTER_BT_BEHAVIOR_IMPROVEMENT_DESIGN.md) · [작업 요청서](../../Assets/docs/requests/MONSTER_BT_IMPROVEMENT_TASK_PROMPTS.md) · [작업 상태](../work-status/monster-bt-improvement/STATUS.md).

- P1 검증기·읽기 전용 감사 구현. SelectedIntent 오타, 무효 공격 쿨다운, 등록되지 않는 쿨다운과 확정 도달 불가 규칙을 검사한다. [인계](../work-status/monster-bt-improvement/P1_VALIDATOR.md).
- P0 테스트 BT 승격은 BT/JSON GUID와 BehaviorData 13개 연결을 보존했다. 미확정 몬스터 재연결은 사용자 결정 대기다. Humanoid의 Organic 참조는 프로젝트 루트에서 정상 해석된다. [인계·GUID 증거](../work-status/monster-bt-improvement/P0_ASSIGNMENT.md).
- P2 플레이어 읽기·부작용 없는 슬롯 조회·궁수 차지 견제 구현. 가드 파괴는 실제 결과만 읽으며 현재 적 가드 소모 시나리오는 보류다. [인계](../work-status/monster-bt-improvement/P2_PLAYER_READ.md).
- P2a 귀속 구역·복귀·공통 회피 제한·무적 구간 분리·회피 후 공략 구간 구현. 설정 SO 54개와 슬롯 조건 BT 8개를 연결했다. 실제 맵 경로와 체감은 사용자 테스트 대기이며 비행의 복잡한 3D 장애물 우회는 미지원이다. [인계](../work-status/monster-bt-improvement/P2A_TERRITORY_EVASION.md).
- 최신 자동 검사: 관련 EditMode **63/63**, JSON **27종 오류/경고 0**, 설정 **54개 오류 0**, 컴파일 실패 false. [원본](../work-status/monster-bt-improvement/FINAL_TEST_RESULTS.json)·[데이터 검사](../work-status/monster-bt-improvement/FINAL_VALIDATION.txt)·[최종 감사](../work-status/monster-bt-improvement/FINAL_AUDIT.md).
- P3 원형 분화·P4 의도 안정화·P5 그룹 역할은 미구현이다. 요청서 4단계의 P2a 사용자 검수 선행 조건을 충족해야 한다. [직접 테스트 명세](../work-status/monster-bt-improvement/USER_TESTS.md). 페이크 공격은 계속 보류다.

## 번개·천둥 SFX 리소스 (2026-11-13)

[조사·파일별 용도·라이선스·검증](../work-status/lightning-sfx-resources.md): CC0 천둥 5종을 [Lightning](../../Assets/06.Sounds/Environment/Lightning)에 확보했다. BigSoundBank WAV 4종(5.03~17.20초)과 rubberduck OGG 1종(5.26초), 원본과 배포·라이선스 근거는 [원본 목록](../../ArtSource/Audio/Lightning/README.md)에 보존한다. Unity AudioClip 임포트·로드 5/5 확인. 게임 데이터 연결·직접 청취·최종 청감은 미수행/사용자 테스트 대기다.

### 추가 행동 패턴 설계

[추가 조사·설계 상태](../work-status/monster-bt-improvement/ADDITIONAL_PATTERN_DESIGN.md): 기존 행동 개선 설계 §6에 원형별 패턴, 지연/이동 뒤 공격 재검사, 실패·중단·초기화, 인수 시나리오를 추가했다. 기존 차지 견제·회피 제한은 재제안하지 않는다. 문서만 반영했으며 P3~P5 구현과 P2a 남은 검수 상태는 그대로다.

### 지연 행동 실행 전제 개선

[구현·검증 기록](../work-status/monster-bt-improvement/DELAY_RECHECK.md): Wait 이후 그룹/규칙 조건 재검사, 지상 공격 슬롯 예약 전 사선 확인을 반영하고 BT 10개를 GUID 유지 재생성했다. 신규 회귀 포함 importer fixture 14/14 통과. 실제 전투 체감과 P3~P5는 미완료다.
- 최종 관련 EditMode는 74/74 통과했다. 원본은 위 구현 기록에서 연결한다.

### 지연 개선 실제 전투 관찰

[MCP 전투 관찰](../work-status/monster-bt-improvement/DELAY_RECHECK_MCP.md): 궁수/대검의 정상 교전과 엄폐 중 공격 정지를 확인했다. 일반 등급 궁수의 Signature 제외로 지연 사격 중단은 미재현이며 Console dt>0 Assert도 미해결이다. 전체 체감 합격으로 승격하지 않는다. 테스트 객체·가상 입력·무적을 정리하고 씬 dirty=false 확인.


## 무기별 궁극기 연출과 얼굴 패널 (2026-11-14)

얼굴 컷인 재제작: 날카로운 인상 지적을 반영해 원본 모델 기반 초상화의 둥근 눈·부드러운 윤곽/명암으로 Raon/Hwarin PNG를 교체했다. GUID 보존, Unity Sprite 로드와 기존 연결2/2 확인. [현재 적용본·생성 출처·이전 이미지·SR01~02](../work-status/weapon-ultimate-presentation/IMAGE_PROMPTS.md). 실제 패널·미술 인수는 사용자 테스트 대기다.

[설계 정본](../../Assets/docs/design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md) · [진행 상태](../work-status/weapon-ultimate-presentation/STATUS.md) · [얼굴 이미지·프롬프트](../work-status/weapon-ultimate-presentation/IMAGE_PROMPTS.md). P0 얼굴2종 제작·Sprite로드2/2, P1 얼굴 패널/이벤트/전용 프리팹/Raon 연결·EditMode11/11·PlayMode2/2 통과. 스타일·실제 전투 인수는 사용자 테스트 대기다. P2 Sword/GreatSword 실물2종·생성기 EditMode7/7 통과. P3 프로필10종·기존2종 궁극기 연결·재검사11/11 통과. P4 카메라 후보10종 보존(미적용). 사용자 구도 회귀로 Raon/Hwarin 자동 Snapshot 연결 해제·관련 EditMode6/6 통과. 기존 모션/Stage 카메라로 복구했으며 실제 구도 재확인은 대기다. [복구 기록](../work-status/weapon-ultimate-presentation/CAMERA_FRAMING_FIX.md). 기존클립5후보의 시각 적합성·Blender 신규 필요는 사용자 확인 대기. Astra 검토에 따라 Actor→Contracts 표시 계약→UI로 연결하고 전투 타이밍·기존 이벤트를 보존한다. 무기 정의는 Raon=Katana, Hwarin=DoubleAxe다.
