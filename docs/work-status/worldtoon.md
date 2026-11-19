# WorldToon — 구현·검증 상태

- 설계 입력(전달 원문): [WorldToon 설계서](../전달용/WorldToon_Handoff/WorldToon_Design.md) · [구현 요청문](../전달용/WorldToon_Handoff/IMPLEMENTATION_REQUEST.md) · [검증 템플릿](../전달용/WorldToon_Handoff/ACCEPTANCE_CHECKLIST.md) · [전달 안내](../전달용/WorldToon_Handoff/START_HERE.md)
- 프로젝트 통합 계약: [WorldToon 기존 시스템 통합](../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md)
- 연결: [월드와 아트 위키](../wiki/world-art.md), [전달 설계와 프로젝트 연결](../전달용/README.md)
- 관련 선행 작업: [Non Humanoid lilToon 전환](non-humanoid-toon-migration.md), [반응형 환경](reactive-environment.md), [식생 빌드 검정](vegetation-build-black.md), [커스텀 셰이더 오버헤드](custom-shader-overhead-review.md)
- 현재 판정: **설계 연결만 완료(2026-11-18). 구현 착수 전. 코드·셰이더·재질·씬 변경 없음. 아래 전 항목 미실행.**

이 문서의 상태가 위키·설계 문서의 요약보다 우선한다. 전달 원문의 "완료"·성능 문구를 구현 증거로 승계하지 않는다. 판정 용어는 원문의 `PASS / FAIL / NOT_RUN / NOT_APPLICABLE`와 프로젝트의 `사용자 테스트 대기`·`사용자 지시 대기`를 함께 쓴다.

## 이번 연결 작업에서 한 일

- 전달 패키지 5개 파일을 읽고 프로젝트 문서 체계(위키·상태·설계·전달용 README)와 대조했다.
- 선행 작업과의 충돌·중복을 확인했다: 비인간형 몬스터 lilToon 전환, 반응형 환경 재질, 기존 식생 셰이더 패스, WorldBuilder 지형, 기존 lilToon 변환기의 원본 직접 수정. 결정은 통합 계약 §1에 있다.
- Unity 실행·셰이더 작성·측정은 하지 않았다. 문서·링크 변경만 했다.

## 단계

| 단계 | 상태 | 산출물·로그 |
|---|---|---|
| G0 조사 | NOT_RUN | 위치: `docs/work-status/worldtoon/ProjectAudit.md`(미작성) |
| G1 Mesh 최소본 | NOT_RUN | 위치: `Assets/06.Shaders/WorldToon/`(미작성) |
| G2 최소 A/B | NOT_RUN | |
| G3 Foliage | NOT_RUN | |
| G4 NPC | NOT_RUN | 이미 전환된 lilToon 재질 되돌림 금지 |
| G5 Terrain4 | NOT_RUN | WorldBuilder 지형은 읽기 전용 |
| G6 도구·빌드 | NOT_RUN | Player·Addressables 빌드는 `사용자 지시 대기` |
| G7 도입 판정 | NOT_RUN | 미술 품질은 `사용자 테스트 대기` |

## 기능 검증 T01~T28

전부 `NOT_RUN`이다. 항목 정의는 [설계서 §16.2](../전달용/WorldToon_Handoff/WorldToon_Design.md)와 [검증 템플릿](../전달용/WorldToon_Handoff/ACCEPTANCE_CHECKLIST.md)을 따르며 결과를 실제로 실행한 뒤 이 문서 또는 `worldtoon/ValidationReport.md`에 기록한다. T27·T28은 빌드가 필요해 `사용자 지시 대기`로 표기한다.

## 도입 판정

| 계열 | 판정 | 이유 |
|---|---|---|
| Mesh·Props | 미정 | |
| NPC·Monster | 미정 | 기존 lilToon 전환 결과가 기준선. A/B 이득이 없으면 유지 |
| Foliage | 미정 | 기존 식생 셰이더·접촉 변위와 비교 |
| Terrain4 | 미정 | 5개 이상 레이어는 원본 유지 |

## 남은 확인과 다음 행동

1. 사용자가 구현 범위를 지시하면 **G0 조사 + G1 Mesh 최소본**부터 시작한다(통합 계약 §4). G0 결과로 이 문서의 환경 값을 채운다.
2. 시작 전 보호 목록(플레이어·주요 캐릭터·반응형 환경 재질 사용처)을 확정한다.
3. 비인간형 lilToon 전환의 사용자 확인 결과를 확인하고 G4 범위에 반영한다.
