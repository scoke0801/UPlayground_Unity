# 월드와 아트

LakeVillageWorld 대기 프레임 저하(2026-11-14~15): [조사·최종 근거](../work-status/lake-village-idle-performance.md#debug-최종-판정--acceptance-3). **Editor Debug의 초기화 후 대기 기준 통과**: 맑음/비 각600프레임, 평균40.01/36.73FPS·임의500ms 최저33.81/31.46FPS.2560×1440/MSAA4와18,472대상을 유지하며 그림자25m/1cascade·SSAO off·빈 Decal 패스off를 전용 에셋에 저장했다. 최신 회귀100/100, 강수 위치/차폐 차이0, GPU 유효 표면45개 통과/8개 미관측이다. 단일 프레임 최대88.25ms와 기존 ActorDatabase 지역 배치 시간 초과2건은 남는다. 전체 NPC 정상 배치·이동/전투·미술 인수는 사용자 확인 대상이며 대기 수치를 전체 월드 품질 통과로 승계하지 않는다.

## LakeVillageWorld 식생 전수 전환 — 2026-11-14

[최종 결과·검증 근거](../work-status/reactive-environment.md#식생-전수-전환-결과--2026-11-14): 식생15,396/15,396개와 Terrain10층을 실제 본편에 연결·저장했다. 재로드 후 전수 구성 유효, 누락/중복 ID·배치/활성/ID 변경0, 원본/메타332개 해시 변경0이다. 퀘스트/수중/채집 시각 식생829개를 비가연·비파괴 정책으로 보호한다. 외형·LOD·바람·알파와 기존 채집 소유권을 보존한다.

[표현 최적화·최종 자동 검증](../work-status/reactive-environment.md#공유-표현-버퍼와-전환-최종-자동-검증--2026-11-14): 활성 식생15,284개+Terrain 셀3,188개(총18,472대상), 강수 판정 전수 차이0. Native 강수 묶음과 공유 GPU 표현으로 강수 중앙 tick은 Editor에서220~230ms→67~74ms, 표현은약60ms→10ms로 줄었다. CPU 화학·전파·저장과 카메라 밖 대상 처리는 유지한다. GPU 일반/인스턴싱 출력 대조와 열/물·JSON·ID 재등록·Terrain 원본 복구를 확인했다.

**전수 전환·자동 검증과 최종 플레이 인수를 구분한다.** 일반 공격 입력·실제 저장/지역 왕복·KCC/Nav·모든 LOD/임포스터·목표 기기 성능·미술/청감은 [LV-V01~06 사용자 테스트 대기](../work-status/reactive-environment-tests.md#lakevillageworld-식생-전수-전환-인수-전환-완료사용자-검수-대기-2026-11-14)다. 강수 tick 잔여67~74ms는 목표 기기 FPS 통과 근거가 아니며 성능 인수는 보류다. 기존 재생성 원본 부재와 비식생을 포함한 W 전체 인수는 이번 식생 전환으로 해소되지 않는다.

## Unity FX 제작 스킬

2026-11-14: [uplayground-vfx 정본](../../.claude/skills/uplayground-vfx/SKILL.md)을 작성하고 Codex 사용자 스킬 폴더에 설치했다. 기존 Unity MCP·Shuriken·MotionEvent·무기 궤적·풀을 활용하며 VFX Graph 노드 편집 제한을 명시했다. 정본/설치본 구조 검사와 3개 파일 해시 대조 통과. 실제 FX 제작·Unity 실행·미술 검증은 미수행이다. [설치·검증·사용법](../work-status/uplayground-vfx-skill.md).
