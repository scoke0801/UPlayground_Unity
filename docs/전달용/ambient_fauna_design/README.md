# GPU 환경 생물 설계 패키지

프로젝트 연결: [분석·작업 상태](../../work-status/ambient-fauna.md) · [설계 3종 연결](../README.md). 현재 진행·검증 결과는 상태 문서에서 관리한다.

버전 1.0 · 2026-11-12

## 열람과 전달

팀 공유에는 `환경생물_정규설계_및_AI구현지침.html`을 여세요. 4개 문서와 도식 5개가 한 파일에 들어 있어 별도 이미지 없이 오프라인 열람할 수 있습니다.

Codex 또는 Claude에는 이 패키지 전체와 실제 프로젝트 저장소를 함께 제공하고, `02_Codex_Claude_구현지침.md`의 **바로 전달할 시작 요청문**을 사용하세요. 문서를 하나만 첨부할 때는 `00_통합_설계_구현지침.md`를 사용하세요. 이 통합본의 도식까지 보려면 같은 폴더의 `diagrams/`도 함께 유지해야 합니다.

## 구성

| 파일 | 역할 |
|---|---|
| 환경생물_정규설계_및_AI구현지침.html | 전체 내용을 합친 팀 열람본, 도식 내장 |
| 00_통합_설계_구현지침.md | 한 번에 전달할 전체 Markdown |
| 01_환경생물_정규설계.md | 요구사항·아키텍처·데이터·GPU·월드·최적화 |
| 02_Codex_Claude_구현지침.md | 실행 요청문·G0~G5·인계·검토 프롬프트 |
| 03_BlenderMCP_ImageGen_아트제작.md | 자산 재사용·신규 제작·UV·이미지 프롬프트 |
| 04_검증_인수기준.md | 기능 35 + 아트 14 + 성능 8개 검증 시나리오 |
| diagrams/ | 구조·수명·에셋 흐름·Compute·아트 계약 SVG 5개 |
| templates/project_preflight.template.json | 실제 버전·서비스·도구·자산 조사 기록 |
| templates/asset_manifest.template.json | 에셋 결정·출처·제작·검수 기록 |
| templates/asset_manifest.schema.json | 에셋 manifest 구조와 일부 완료 조건 검증 |
| templates/atlas_contract.template.json | 원본·mask·UV 좌표·아틀라스 가공 규약 |
| templates/performance_report.template.json | 측정 환경·실측 결과·증거 기록 |
| DOCUMENT_VALIDATION.md | 이 문서 패키지 자체의 검증 기록 |

## 핵심 방침

기존 프로젝트의 모델·텍스처가 적합하면 재사용합니다. GPU용 파생 변환으로 해결할 수 있으면 원본을 보존하고 파생본을 만듭니다. 적합한 모델이 없을 때 Blender MCP로 메시·UV를 만들고, 적합한 외형 텍스처가 없을 때 ImageGen으로 원본을 제작한 후 UV·알파·padding을 가공합니다.

기본 구현은 VFX Graph, 대안/확장은 Compute + Indirect입니다. 개체별 GameObject나 Animator, 정상 경로의 GPU→CPU 위치 readback은 사용하지 않습니다. 게임플레이 권위를 가진 포획·보상 개체는 별도 시스템입니다.

## 상태와 주의

본 패키지는 설계·실행 지침입니다. Unity 프로젝트 구현, .blend/FBX/PNG 제작, 실제 모델 조사, 플레이 테스트, 성능 측정을 수행한 결과물은 아닙니다. JSON의 null·UNRESOLVED·NOT_RUN은 의도적인 미확인 상태입니다.

프로젝트 문서에서 확인한 Unity 6000.3.21f1·URP 17.3.0 기록은 참고 정보입니다. 구현자는 실제 저장소 설정과 해석된 패키지 버전을 먼저 확인해야 합니다. 과거 대화의 6000.0.60f1 값을 고정하거나 패키지를 임의로 업그레이드하지 않습니다.

JSON Schema는 파일 존재, 이미지 alpha, Unity 빌드, GPU 결과까지 검증하지 못합니다. 실제 근거 파일과 실행 검증이 별도로 필요합니다. 이 패키지는 Unity 설치용 패키지나 완성 게임 에셋이 아닙니다.
