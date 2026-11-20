# WorldToon — 구현·검증 상태

- 설계 입력(전달 원문): [WorldToon 설계서](../전달용/WorldToon_Handoff/WorldToon_Design.md) · [구현 요청문](../전달용/WorldToon_Handoff/IMPLEMENTATION_REQUEST.md) · [검증 템플릿](../전달용/WorldToon_Handoff/ACCEPTANCE_CHECKLIST.md) · [전달 안내](../전달용/WorldToon_Handoff/START_HERE.md)
- 프로젝트 통합 계약: [WorldToon 기존 시스템 통합](../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md)
- 산출물: [G0 조사](worldtoon/ProjectAudit.md) · [결정 기록](worldtoon/Decisions.md) · [검증 보고 T01~T28](worldtoon/ValidationReport.md) · [성능 보고](worldtoon/PerformanceReport.md) · [변환 기록 JSON](worldtoon/ConversionReport.json) · [출처 고지](worldtoon/ThirdPartyNotices.md) · [캡처](worldtoon/captures/)
- 연결: [월드와 아트 위키](../wiki/world-art.md), [전달 설계와 프로젝트 연결](../전달용/README.md)
- 관련 선행 작업: [Non Humanoid lilToon 전환](non-humanoid-toon-migration.md), [반응형 환경](reactive-environment.md), [식생 빌드 검정](vegetation-build-black.md), [커스텀 셰이더 오버헤드](custom-shader-overhead-review.md)
- 현재 판정(2026-11-19): **코드 작성·컴파일 확인 완료, 시각 검증은 자동 캡처까지(외형 `사용자 테스트 대기`), 성능 `NOT_MEASURED`, Player 빌드 `사용자 지시 대기`, 도입 승인 없음. 실제 게임 씬·프리팹·지형에 적용한 재질 없음.**

이 문서의 상태가 위키·설계 문서의 요약보다 우선한다. 전달 원문의 "완료"·성능 문구를 구현 증거로 승계하지 않는다.

## 완료 상태 구분(구현 요청문 기준)

| 구분 | 상태 |
|---|---|
| 코드 작성 | 완료(Mesh·Foliage·Terrain4·Basemap 셰이더, 분석·변환·적용·복구·검사·검증 씬 도구, EditMode 테스트) |
| 컴파일 확인 | 완료 — 셰이더 import 오류 0, 대표 변형 300개 D3D 컴파일 실패 0, C# 컴파일 오류 0(WorldToon 범위). 같은 시점 다른 작업의 `WorldBuilder/Editor/WorldCaveEvaluation.cs` 컴파일 오류가 콘솔에 있었으며 범위 밖이라 손대지 않았다 |
| 시각 검증 | 자동 캡처 PASS(렌더 깨짐 없음). 미술 품질 `사용자 테스트 대기` |
| 성능 검증 | `NOT_MEASURED`(정적 바이트코드 지표만) |
| 도입 승인 | 없음. 바위·소품만 후보 |

## 단계

| 단계 | 상태 | 결과 |
|---|---|---|
| G0 조사 | 완료 | [ProjectAudit](worldtoon/ProjectAudit.md). Forward+, D3D11/12, SSAO(DepthNormals), DBuffer 데칼·레이어, Legacy 프로브, 런타임 RP 교체, 카메라 SMAA, 히트스톱 슬로모션 Motion Blur(모션벡터 실사용), lilToon 커밋 확인 |
| G1 Mesh 최소본 | 완료(Editor) | `UPlayGround/WorldToon/Mesh`. 바위 3종·구·박스 렌더 정상, 그림자·데칼 캡처 확인. SSAO 수신·Fog·카메라 가림 페이드는 구조상 연결, 화면 확인 NOT_RUN |
| G2 최소 A/B | 부분 | 검증 씬 비교 캡처 + 정적 지표(Lite 프래그먼트 −40%). GPU A/B는 Player 필요 → `사용자 지시 대기` |
| G3 Foliage | 셰이더 완료, 적용 보류 | 절차 카드로 Cutout·양면·바람·그림자 확인. 기존 식생 메시 Read/Write 꺼짐(WT-FOLIAGE-001)으로 실제 교체 보류 |
| G4 NPC | 적용 후보 없음 | NPC·몬스터 슬롯 대부분 lilToon 보호(2,441슬롯). 비보호 스키닝 슬롯은 WT-MAT-002 |
| G5 Terrain4 | 셰이더 완료, 적용 보류 | 검증 지형 2/4레이어·구멍 정상, 5레이어 차단. 실제 지형: 주요 월드 7~27레이어(불가), 4레이어 이하 80개는 WorldBuilder 타일(읽기 전용) |
| G6 도구·빌드 | 도구 완료 / 빌드 `사용자 지시 대기` | Analyze·복제·적용·복구·충돌 차단·Manifest·Terrain/식생 검사. 보고 전용 스트리퍼는 빌드 실행 없이 검증할 수 없어 만들지 않음 |
| G7 도입 판정 | 대기 | 아래 표. 외형 `사용자 테스트 대기`, 성능 미측정 |

## 도입 판정

| 계열 | 판정 | 이유 |
|---|---|---|
| Mesh·Props(URP/Lit 바위·소품) | **후보** | 렌더 정상, 데칼·그림자·추가 광원 동작. 외형 확인과 Player A/B 후 결정 |
| NPC·Monster | 원본 유지 | lilToon 보호(통합 계약 §1). 적용 대상 없음 |
| Foliage | 보류(원본 유지) | 바람 마스크 확인 불가, 기존 접촉 변위·반응형 경로 |
| Terrain4 | 보류(원본 유지) | 주요 지형 4레이어 초과·레이어 노멀 사용, 나머지는 WorldBuilder 읽기 전용 |

## 실행 방법(MCP `execute_code`)

| 목적 | 호출 |
|---|---|
| 셰이더 대표 변형 컴파일 | `UPlayGround.Tool.Editor.WorldToon.WorldToonShaderCompileCheck.RunDeferred()` → `docs/work-status/worldtoon/logs/shader-compile-check.txt`(git 제외) |
| 검증 씬 재생성 | `WorldToonValidationSceneBuilder.Build()` → `Assets/01.Scenes/ArtReview/WorldToonValidation.unity`(git 제외 폴더) |
| 캡처 | `WorldToonValidationSceneBuilder.Capture("day", false)` / `("night", true)` |
| 테스트 | EditMode `UPlayGround.Editor.Tests.WorldToonConversionTests` |

## 남은 확인과 다음 행동

1. **사용자 외형 확인:** [캡처](worldtoon/captures/) 또는 재생성한 검증 씬에서 바위·소품의 툰 명암(Shade Tint·경계)이 의도에 맞는지. 스타일 수치는 [WorldToonStyle_Default](../../Assets/10.Datas/Environment/WorldToon/WorldToonStyle_Default.asset)에서 바꾸고 변환 도구를 다시 실행한다.
2. **Player 빌드 지시:** T26~T28과 G2 GPU A/B는 Player(필요 시 Addressables) 빌드가 필요하다. 지시가 있으면 [성능 보고](worldtoon/PerformanceReport.md)의 계획대로 측정한다.
3. 실제 게임 씬의 바위·소품 적용 범위(어느 씬·프리팹)를 정하면 복제 재질 적용을 진행한다. 원본 모델·프리팹은 수정하지 않는다.
4. NOT_RUN 항목(T04 캐스케이드, T08~T10, T13~T15, T18, T20~T21)은 적용 범위 확정 후 Frame Debugger와 함께 진행한다.
