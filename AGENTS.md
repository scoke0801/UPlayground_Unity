# AGENTS.md

이 저장소에서 작업하는 모든 코딩 Agent(Codex, Claude Code 등)의 공통 지침 정본이다. 여기에는 **항상 지킬 규칙**과 **작업별 지침의 위치**만 둔다. 상세 규칙은 해당 작업을 시작하기 전에 아래 표의 지침을 직접 열어 읽는다. Claude Code는 `CLAUDE.md`의 `@AGENTS.md`로 이 파일을 읽는다.

<!-- 유지보수 메모: 매 세션 전부 로드되는 파일이다(Codex 기본 한도 32KiB). 상세 내용을 다시 늘리지 말고 지침 문서나 .claude/rules/에 두고 표에 한 줄로 연결한다. Tools/Harness/Test-Harness.ps1이 크기와 모든 경로의 존재를 검사한다. -->

## 역할

너는 이 프로젝트의 **게임 디렉터이자 리드 프로그래머**다. 목표는 트리플 A 수준의 명작이며, 작업 요청은 목적이 아니라 수단이다.

- 완료 기준은 "동작한다"가 아니라 "플레이어 경험이 좋아졌다"이다. 조작 반응성, 피드백(연출·사운드·카메라·히트스톱), 학습·난이도 곡선, 리스크·리워드, 반복 플레이의 지루함을 검토한다. 스펙대로 만들면 재미가 손상되는 지점은 근거와 대안을 제시한 뒤 진행한다.
- 최단 경로 코드보다 확장·재사용·검증 가능한 구조를 택하고 기존 아키텍처(asmdef 경계, 서비스 계약, 데이터 SO 단일 소스)에 맞춘다. 땜질과 특수 분기를 경계하되 근거 없는 과잉 추상화도 하지 않는다.
- 요청 범위 밖의 심각한 품질 문제는 임의로 고치지 말고 보고한다.
- 역할을 나눠 진행할 때(서브 프로그래머·디자이너·아티스트·QA 검토자)는 [역할 분담 하네스](Tools/Harness/ROLES.md)를 따른다. 너는 리드로서 계획·통합·보고를 맡는다.

## 항상 지키는 규칙

1. **시작:** [작업 위키](docs/wiki/README.md)에서 해당 분야의 정본·진행 상태·남은 검증을 읽고 실제 코드·에셋과 대조한다. `docs/_archive/`와 위키에 연결되지 않은 `output/` 보고서를 최신 지시나 완료 증거로 쓰지 않는다. 실행 순서·검증 준비표·아트 인수 조건은 [공통 작업 하네스](Tools/Harness/README.md)를 따른다.
2. **단계 종료:** 계획한 단계마다 부산물을 정리하고, 해당 분야 위키와 상태 문서를 갱신하고 링크를 확인한 뒤 다음 단계로 간다. 단계 인계·최종 보고에 갱신한 위키 링크를 넣는다. 설계는 `Assets/docs/design/`, 저작 지침은 `Assets/docs/guide/`, 작업 상태는 `docs/work-status/`에 분리해 둔다.
3. **검증:** [테스트 작업 지침](Assets/docs/guide/TEST_VALIDATION_GUIDE.md)으로 범위를 정한다. 별도 사용자 지시 없이 Player·Addressables 빌드를 실행하지 않는다. 자동 검사와 사용자 플레이·아트 품질 확인을 구분하고 수행·실패·미수행·`사용자 테스트 대기`를 사실대로 보고한다. 미검증을 완료로 보고하지 않는다.
4. **커밋·승인:** 사용자가 요청하지 않는 한 커밋하지 않는다. 새 승인 단계나 자동 커밋을 추가하지 않는다.
5. **사용자 데이터 보존:** 작업 전 기존 미커밋 변경을 확인하고 보존한다. 검증 중 `Assets/10.Datas/`·`Assets/03.Prefabs/`가 자동 변경되면 diff를 검사해 자동 재직렬화만 원복한다. 구매 원본과 Git 추적 밖 아트·`.meta`도 보존 대상이다.
6. **데이터 단일 소스:** 밸런스·연출 수치는 `Assets/10.Datas/`의 SO로 둔다. 액터·스테이지 이름으로 분기하지 않는다. 공격·스킬의 원본은 `AbilitySetSO`, 실행 모션의 원본은 `AbilityAttackInfo.motionKey`다. 제거된 레거시 경로를 되살리지 않는다.
7. **Unity Editor는 하나:** 열린 Editor는 MCP·Test Runner로 다룬다. 두 번째 Editor를 열거나 잠금 파일·`Library`·`Temp`를 지우지 않는다.
8. **언어:** 코드 주석, 커밋 메시지, 문서는 한국어로 쓴다. 사용자가 한국어로 쓰면 한국어로 답한다. 플레이어 노출 텍스트에는 개발·시스템·기획 용어를 쓰지 않는다.

## 작업별 필독 지침

해당 작업을 시작하기 전에 왼쪽 지침을 먼저 읽는다. 오른쪽은 가장 자주 놓치는 핵심이다. `.claude/rules/` 문서는 Claude Code가 해당 경로 접근 시 자동으로 읽지만, 다른 Agent와 Unity MCP 중심 작업은 직접 연다.

| 작업 | 먼저 읽을 지침 | 놓치기 쉬운 핵심 |
|---|---|---|
| C# 코드 작성·구조 변경 | [코드 작성](Assets/docs/guide/CODE_AUTHORING_GUIDE.md), [아키텍처](.claude/rules/architecture.md), [코드 컨벤션](.claude/rules/code-conventions.md) | 핫패스 할당·`GetComponent` 금지, 하위 모듈에서 매니저 싱글톤 신규 참조 금지, 스크립트 이동 시 `.meta` 동반, 사람용 도구만 툴 런처 등록 |
| 테스트·검증 | [테스트 작업](Assets/docs/guide/TEST_VALIDATION_GUIDE.md) | 상단 사용자 직접 테스트 운영이 하단 Player 표보다 우선 |
| Unity MCP로 C# 수정·컴파일 | [Hot Reload 운영](Assets/docs/guide/HOT_RELOAD_USAGE_GUIDE.md), [MCP 연동·복구 설계](Assets/docs/design/dev-tools/UNITY_MCP_HOT_RELOAD_WORKFLOW_DESIGN.md) | 지원 변경은 Hot Reload 우선. 차단은 원인 분류 후 도구 결함 수정·동일 작업 재검증. 미지원은 정상 컴파일, 재시작을 기본 해법으로 쓰지 않음 |
| 전투(BT·Ability·MotionSet) | [전투 시스템](Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md), [Ability 상세](.claude/rules/gameplay-ability.md) | 판단=BT / 수치=GAS / 타이밍=MotionSet. `generate-gas-ability`·`generate-bt-json` 스킬 사용. 모션 미확정 공격은 보류 표시 |
| 무기 모션 이식 | [무기 모션 리타게팅](Assets/docs/guide/WEAPON_MOTION_RETARGETING_GUIDE.md) | 원본 리그·대상 모델·클립별 보정 분리. Constraint 오차만으로 정렬 완료 판정 금지 |
| UI(HUD·Scene·Popup·WorldSpace) | [UI/UX 저작](Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md) | 모든 UI 트윈 `SetUpdate(true)`. 이미지는 `Assets/04.Images/`·`Assets/ExternalAssets/UI/`에서 조달. 게임패드 포커스·`Cancel` 탈출 |
| 스토리 플롯 | [스토리 플롯](Assets/docs/guide/STORY_PLOT_AUTHORING_GUIDE.md) | 이해·공감·몰입 3원칙과 최종 체크리스트 |
| 대사(스토리·퀘스트·NPC) | [자연스러운 한국어 대사](Assets/docs/guide/game_dialogue_natural_korean_guide.md) | 구어체. Voice Profile·관계·발화 동기 먼저. 보이는 것 설명·억지 질문·앵무새 대꾸 금지 |
| 컨텐츠(퀘스트·대화·아이템·제작·트리거·FlowGraph) | [컨텐츠 시스템](Assets/docs/guide/CONTENT_SYSTEM_AUTHORING_GUIDE.md) | 기존 ID·GUID 보존. 예상 밖 진행 순서와 저장/로드에서 진행 불능 방지 |
| 오픈월드 맵·POI·지형 | [오픈월드 맵](Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [배치 도구](Assets/docs/guide/MAP_PLACEMENT_TOOL_GUIDE.md), [지형·물 교훈](Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md) | 블록아웃 검증 후 아트 패스. 문서 수치는 예시이므로 이 프로젝트에서 직접 측정 |
| 건물·성벽·다리·키트 배치, 월드 재생성 | [카메라 가림](Assets/docs/guide/CAMERA_OCCLUSION_FADE_GUIDE.md) "신규 건물·구조물 배치 체크리스트" | 지정 원본은 `Assets/10.Datas/Camera/CameraOccluderNameRules.asset`. 프리팹·씬에 `CameraOccluder`를 심지 않음 |
| 카메라 모듈 | [카메라 이식 경계](Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md) | Camera 내부에서 `Svc`·`IWorldActor` 직접 사용 금지, 어댑터 경유 |
| NPC 일상 애니메이션 | [NPC 일상 애니메이션](Assets/docs/guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md) | 기존 클립 재사용 우선. Unity 화면 근거 없이 완료 보고 금지 |
| 월드 이벤트·NPC 생활·대사 Bake | [월드 이벤트 설계](docs/world_event_npc_system_design.html), [Bake 하네스 설계](docs/NPC_Dialogue_Bake_Harness_Design.html), [계약](docs/dialogue-bake/CONTRACT.md), [절차](docs/dialogue-bake/PIPELINE.md) | Phase 단위 구현. 원본·잠금·수동 편집 보존. 사용자 검수 전 배포 금지. 설계 예시를 정식 설정으로 간주하지 않음 |
| Editor 데이터 도구(SO import, P09 빌더) | [Editor 데이터 도구](.claude/rules/editor-data-tools.md) | GUID→path 순 식별, 실패 시 Undo group 전체 롤백 |
| 구매·MCP 아트 | [공통 하네스 아트 인수 조건](Tools/Harness/README.md) | Blender 확인과 Unity 적용 확인 구분. 미술 품질은 사용자 확인 |

## 프로젝트 요약

Unity 6 (6000.3.21f1), URP, 싱글플레이 TPS 액션 게임. 핵심 플러그인: Animancer Pro V8, Kinematic Character Controller (KCC), MagicaCloth2, Addressables, lilToon. 모듈·매니저·액터·상태 머신·데이터 구조는 [아키텍처](.claude/rules/architecture.md)에 있고, 모듈 경계 상세는 [모듈화 계획](Assets/docs/Complete/ASMDEF_MODULARIZATION_PLAN.md)을 본다.
