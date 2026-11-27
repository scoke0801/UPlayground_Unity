# 전투와 실행 데이터

- 2026-11-17 [KINEMATION MotionWarping 조사](../work-status/kinemation-motion-warping-investigation.md): 구매 패키지의 다구간 루트 이동 보정·파쿠르 예제·Editor 구조를 조사했다. 현재 전투 실행은 자체 MotionWarpController이며 구매 TwoBoneIk는 무기 파지·조준·글라이더에서 사용 중이다. 구매 실행 컴포넌트의 Rigidbody 위치 인자·빈 구간·상태 복구 위험을 정적으로 확인했다. 구현 변경·Unity 실행 검증은 하지 않았다.
- 같은 조사 문서의 **KCC 연동 검토**: 목표 자세 평가와 상태의 UpdateVelocity/UpdateRotation 적용 분리로 연동 가능하다. MoveCharacter는 현재 Simulator의 시간 배율 미적용 Phase 1에서 처리되므로 단순 API 교체는 비권장이다. 전투 실행은 유지하고 정적 낮은 턱부터 기존 MotionSet/KCC 경로로 연결하는 안을 제시했다. 구현·실행 검증은 미수행이다.

- 2026-11-15 [라온 런타임 운동 데이터 연결 수정](../work-status/motion-impact-build-fix.md): 전용 무기 모션 세트에 빠진 MotionImpact 연결을 복구하고 기존 4개 공격을 재베이크했다. 전체 MotionImpact·MotionWarp 빌드 전 검사 통과, 미매핑 경고 12건 잔존. 재발 방지 절차는 [전투 저작 지침 6.2](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#62-런타임-운동-데이터-연결재베이크)에 연결. Player 빌드·실전 타격 감각은 사용자 테스트 대기.

- 2026-11-15 [MotionWarp 빌드 차단 수정](../work-status/motionwarp-build-fix.md): MotionSet 4개의 실행 설정 이관, Katana 리그별 v3 프로필 13개 베이크, 아린·라온 제자리 구간 이동 가중치 정리. 전체 빌드 전 매핑 검증 오류 0·미매핑 경고 12. 재발 방지 절차는 [전투 저작 지침 6.1](../../Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md#61-motionwarp-설정프로필-준비)에 연결했으며 신규·복제·클립/리그 변경 시 같은 작업에서 검증한다. Player 빌드·실전 감각은 사용자 테스트 대기.

- 2026-11-25 [궁극기 모션 캐릭터별 재검증·재제작](../work-status/weapon-signature/PER_CHARACTER_REBUILD.md): 12명 클립이 구매 클립 동작을 지우고 골반 값·smoothstep 키·IK로 합성된 것이 어색함의 공통 원인임을 확인했다(라온 현재본 칼날 본 최고 속도 5.8m/s, 모캡 30~50m/s. 본 원점 기준이며 이벤트 보정 미적용). 사용자 승인으로 구매 모캡 기반 편집으로 전환했다. 라온은 Frank `S3_Skill01` 발도 모캡(+1.5m 전진, 잔상 강화)을 사용자 승인 후 `Raon_Ultimate.asset`에 연결했다. `AfterimageEvent`에 이벤트별 단색 실루엣 모드(깊이 선기록, 기본값은 기존 방식)를 추가해 라온에 적용했고, stale였던 라온 Katana MotionImpact를 재베이크했다(지문만 갱신). EditMode 50/50·MotionWarp·MotionImpact 전체 검증 통과. **실전 R01~R06 사용자 확인 대기**. 2026-11-26 레이네는 창 `Skill02` 웅크림·도약 하체와 RPG `Item_Throw_1` 골반 선행 투척 상체를 Humanoid 근육 공간에서 합성한 `Reine_LeapThrow`(2.95초, 손목 보정으로 창끝 정렬)를 연결하고, 체공을 1.43→0.95초로 줄였다. EditMode 29/29·MotionWarp·MotionImpact 통과. 이어서 "멀리 던지는 것처럼 보임" 피드백에 따라 `SpawnProjectileEvent.aimPoseAtResolvedTarget`(옵트인)을 추가했다. 지면 투영 투척도 `WeaponAimController`가 실제 착탄점으로 가슴·팔을 조준하고, 해제 뒤에는 팔 방향으로 팔로스루를 조준한다. `WeaponAimSettings.offHandGripDistance`(기본 0=기존 동작)로 손을 뗀 반대손은 끌지 않으며, 회전 전용 MotionWarp도 추가했다. EditMode 63/63·검증 통과, **실전 N01~N07 사용자 확인 대기**.
- 2026-11-15 [궁극기 손·무기 결합과 FX 재작업](../work-status/weapon-signature/GRIP_FX_REVISION.md): 12명 파지 수정 클립·구간 보정/복구·FX 파생 13종을 적용했다. 관련 EditMode 29/29 통과, 유라 나선 바람 메시·재질과 충격광 추가 수정 및 활성 FX 참조 확인을 수행했다. 범위 밖 손가락 커브, 양손 도달 거리, 클립 보정값 잔류를 수정하고 화린 참격 반복·지팡이 빔·착탄/회수 효과를 조정했다. 이전 버전의 품질 반려는 유효하며 이번 수정본의 실전·미술 인수는 아직 받지 않았다. 자동 검사와 정지 프리뷰는 미술 합격 근거가 아니다.

- 2026-11-15 [궁극기 전신 모션 재제작](../work-status/weapon-signature/MOTION_PRODUCTION.md): **12명 모션 품질 사용자 반려·재작업 필요. 전체 완료 판정 철회.** 사용자 피드백은 전원 모션 품질 실패이며 단순 검수 대기가 아니다. 데이터 적용 이력과 EditMode20/20·이전 PlayMode7/7 기록은 보존하지만 미술 합격 근거가 아니다. 537프레임은 12Hz 정지 샘플이며 실제 전투 영상이 아니다. 기존 독립 QA로 품질 합격을 주장하지 않는다. 최종 축 수정 후 추가13건은 미수행. [검수 결과·남은 항목](../work-status/weapon-signature/USER_TEST_REDESIGN.md).

- 2026-11-15 **궁극기 동작 품질 재반려·재설계:** 사용자 지시로 Sol 웹 조사 후 리드가 [12명 모션 제작 기준](../../Assets/docs/design/combat/player/ULTIMATE_MOTION_REDESIGN.md)을 재구성했다. [출처·조사 제한·상태](../work-status/weapon-signature/REFERENCE_REDESIGN.md). 조사 당시 실제 영상 재생 미확보. 후속 신규 모션 제작 상태는 위 항목 참조. 이전 카메라 조율·자동검사는 궁극기 미술 인수 근거가 아니다.

- 2026-11-15 [궁극기 품질 재작업](../work-status/weapon-signature/QUALITY_REVISION.md): 컷인10모션 연장, 12모델 포즈 샘플·9무기 카메라31SO 개선, 화린 반복 감속/되감기·다단 히트스톱 제거, 레이네 오른손 발사점 연결 및 생성기 반영. 최종 EditMode17/17, 회전301샘플 정지·역행0. 실제 전투·창 투척 연기·FX 합성·미술 인수는 사용자 테스트 대기다.

- 2026-11-15 [잔상 텍스처·Fauna 렌더 오류 수정](../work-status/afterimage-fauna-render-errors.md): 텍스처 속성이 없는 VFX 머티리얼의 무조건 `mainTexture` 조회를 제거했다. Unity 컴파일·관련 EditMode22/22·PlayMode2/2 통과, 실제 잔상 RF01 사용자 테스트 대기.

2026-11-14 사용자 정상 컴파일 뒤 MCP에서 충돌 필터 회귀 포함 관련 43/43, E01 60초 구르기 0회/최대 17.904m/복귀 재교전을 확인했다. 실제 두 궁수에 공격이 명중했으나 구르기 후 공격 기회는 미검증이다. E03 급경사 복귀는 NavMesh `PathPartial`로 실패했다. 추가 국소 우회 실험은 효과가 불충분해 원복했다. [실측·전체 AI 검사 151/152·남은 검증](../work-status/monster-bt-improvement/MCP_VALIDATION.md)을 확인한다.

## 정본

- [유라 컷인 추가](../work-status/weapon-ultimate-presentation/YURA_CUTIN.md) (2026-11-14): 기존 모델 전신 초상화와 SkillCutIn 스타일을 참조한 PNG 제작. 1536×1024 RGBA, Unity Sprite 임포트·로드 확인. 컷인 이미지 총12종이며 유라 실행 연결은 미변경이다. 미술·실제 패널은 사용자 테스트 대기다.

- [무기 9종 시그니처 설계](../../Assets/docs/design/combat/player/WEAPON_SIGNATURE_SKILL_CLIP_DESIGN.md) (2026-11-14): **9종 일반 공격형 궁극기 슬롯 연결 반영.** 신규 모션5종·라온 무기본 이식판, MotionSet11개, FX24개·투사체4개·카메라 SO·입력 이동/슬롯 숨김/목표 폴백/얼굴 이벤트를 구현했다. 시퀀스 원본과 공유 Default 세트는 보존하고 Spear는 파생했다. 기존 Spear 피해 페이즈0개는 Astra와 대검 단일타141을 초기 기준으로 정했다. 관련 EditMode **29/29**, PlayMode **12/13**(보스 리안 공격 진입1건 실패), 실제 모델 정지 프리뷰6명 확인. 컷인12명·추출 루트/창 높이 보정·추가 FX 반영 완료. 실제 그립·도약·연속동작·미술은 사용자 테스트 대기다. [현재 단계·검증·남은 항목](../work-status/weapon-signature/STATUS.md) · [SG01~16 직접 테스트](../work-status/weapon-signature/USER_TESTS.md).

- 궁극기 클립 조사 후속: [설계 §6의 실제 채택 에셋·조합·Blender 전환](../../Assets/docs/design/combat/player/PLAYABLE_ULTIMATE_PRESENTATION_CONCEPT_DESIGN.md#6-클립-조사-후-확정한-제작-방향과-에셋) · [조회·포즈·Astra 판정](../work-status/weapon-ultimate-presentation/MOTION_ASSET_DECISIONS.md). Unity 등록 모션111개·실제 모델10명의 실행 연결 조사. 7명 현행 재사용 채택, 세라/릴리는 별도 대검 입력과 메모리 조합 확인(적용 조건부), 레이네는 직선 후보와 실제 판정 경로 확인 대기. 세라 타격창0.357422초 차이를 숫자 보존으로 통과시키지 않는다. 부족 구간만 Blender MCP 제작하는 방향 확정이며 신규 실행 에셋·클립 저장 없음. 말미 Editor 연결 종료로 일부 재촬영·최종 상태 조회 미수행, 실제 플레이·미술은 사용자 테스트 대기.

- [전용 궁극기 연출 미보유 플레이어블 10명 컨셉](../../Assets/docs/design/combat/player/PLAYABLE_ULTIMATE_PRESENTATION_CONCEPT_DESIGN.md) (2026-11-14): Astra와 무기·인물 정본을 대조해 사선/방패와 검/창의 직선/채찍 곡선/활 조준/대검 세로·가로/지팡이 단일 도형·다점 수렴/쌍검 교차로 구분했다. **설계 작성, 신규 구현·모션·이미지 제작 없음.** 공용 궁극기 Ability 부재를 뜻하지 않으며 공유 실행과 다른 동작은 후속안이다. H09는 미사용으로 제외, Whip T3 사용자 결정 대기 유지. [조사·논의·검증과 남은 확인](../work-status/weapon-ultimate-presentation/MISSING_ULTIMATE_CONCEPTS.md).

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

- [Player Katana 공격 FX 보완](../work-status/player-katana-fx.md) (2026-11-14): 실제 `Set_Katana` 실행 경로의 공중·돌진·교대·라온 고유 공격 등 MotionSet 21개에 FX 24개 반영. 현재 모델 120Hz 궤적과 격리 프리뷰로 베기 23개 방향·크기 및 발차기 1개 연결. 기존 찌르기/스킬 FX 보존. 새 FX 포즈/참조 검사 93회 오류 0, 기존 구조 오류 1·경고 21은 변경 전과 동일. 실제 플레이 인수는 대기다.

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

최신 얼굴 아트: **아린 사용자 통과**. 묘령만 통과 아린을 화풍 기준으로 다시 그려 방패로 공격을 밀어내며 기합과 함께 궁극기 반격을 시작하는 장면으로 교체했다. [현재 적용본·출처·프롬프트·M01~02](../work-status/weapon-ultimate-presentation/ARIN_APPROVED_MYORYEONG_BATTLE.md). PNG/알파·다른 컷인 해시 보존 확인, 묘령 미술/실제 패널은 사용자 테스트 대기. 이전 차분한 버전과 다른 두 장 작화 기록은 현재 적용본이 아니다.

아린·묘령 두 장 신규 작화: 부분 덧칠 대신 원본 초상화만 참조해 재생성, 아린의 한 목표를 보는 양눈과 묘령의 상체를 세운 검방패 반격 자세 반영. [파일·프롬프트·F01~03](../work-status/weapon-ultimate-presentation/FRESH_ARIN_MYORYEONG.md). PNG/알파 확인, 기존 GUID/다른 PNG 보존. 기존 Default importer의 Sprite 정리는 Editor 연결 종료로 미수행, 실제 미술/패널은 사용자 테스트 대기다. H09 제외.

얼굴 컷인 재제작: 부드러운 정면 버전이 궁극기에 어울리지 않는다는 피드백을 반영해 Raon/Hwarin PNG를 공격 직전 집중/기합·대각선 자세·머리카락/옷의 동세로 다시 교체했다. 원본 외형 참조, GUID 보존, Unity Sprite 로드와 기존 연결2/2 확인. [현재 적용본·생성 출처·이전 이미지·CR01~02](../work-status/weapon-ultimate-presentation/IMAGE_PROMPTS.md). 실제 패널·미술 인수는 사용자 테스트 대기다.

[설계 정본](../../Assets/docs/design/combat/player/WEAPON_ULTIMATE_PRESENTATION_DESIGN.md) · [진행 상태](../work-status/weapon-ultimate-presentation/STATUS.md) · [얼굴 이미지·프롬프트](../work-status/weapon-ultimate-presentation/IMAGE_PROMPTS.md). P0 얼굴2종 제작·Sprite로드2/2, P1 얼굴 패널/이벤트/전용 프리팹/Raon 연결·EditMode11/11·PlayMode2/2 통과. 스타일·실제 전투 인수는 사용자 테스트 대기다. P2 Sword/GreatSword 실물2종·생성기 EditMode7/7 통과. P3 프로필10종·기존2종 궁극기 연결·재검사11/11 통과. P4 카메라 후보10종 보존(미적용). 사용자 구도 회귀로 Raon/Hwarin 자동 Snapshot 연결 해제·관련 EditMode6/6 통과. 기존 모션/Stage 카메라로 복구했으며 실제 구도 재확인은 대기다. [복구 기록](../work-status/weapon-ultimate-presentation/CAMERA_FRAMING_FIX.md). 기존클립5후보의 시각 적합성·Blender 신규 필요는 사용자 확인 대기. Astra 검토에 따라 Actor→Contracts 표시 계약→UI로 연결하고 전투 타이밍·기존 이벤트를 보존한다. 무기 정의는 Raon=Katana, Hwarin=DoubleAxe다.
