# WorldToon 검증 상태 템플릿

> 이 파일은 미실행 템플릿이다. 초기 상태는 전부 `NOT_RUN`이다.
> 실제 실행한 항목만 PASS/FAIL로 변경한다. 해당 없는 항목은 근거와 함께 NOT_APPLICABLE로 표기한다.

## 환경

| 항목 | 기록 |
|---|---|
| 코드 리비전 | 미확인 |
| Unity / URP | 미확인 |
| 렌더링 경로 / URP Asset | 미확인 |
| OS / GPU / Driver / Graphics API | 미확인 |
| Build Profile / 품질 / 해상도 | 미확인 |
| 테스트 씬 / 카메라 / 시드 | 미확인 |
| 테스트 실행자 / 일시 | 미확인 |

## 단계

| 단계 | 상태 | 산출물·로그 |
|---|---|---|
| G0 조사 | NOT_RUN | |
| G1 Mesh 최소본 | NOT_RUN | |
| G2 최소 A/B | NOT_RUN | |
| G3 Foliage | NOT_RUN | |
| G4 NPC | NOT_RUN | |
| G5 Terrain4 | NOT_RUN | |
| G6 도구·빌드 | NOT_RUN | |
| G7 도입 판정 | NOT_RUN | |

## 기능 검증

| ID | 항목 | 상태 | 근거·로그·캡처 |
|---|---|---|---|
| T01 | 보호 자산 불변 | NOT_RUN | |
| T02 | Opaque/Cutout | NOT_RUN | |
| T03 | 주광·낮밤 | NOT_RUN | |
| T04 | 주광 그림자·Cascade | NOT_RUN | |
| T05 | 추가 광원·Forward | NOT_RUN | |
| T06 | Forward+ 광원 | NOT_RUN | |
| T07 | GI·프로브·APV·Lightmap | NOT_RUN | |
| T08 | Fog·Depth | NOT_RUN | |
| T09 | SSAO·DepthNormals | NOT_RUN | |
| T10 | MotionVectors·TAA/STP | NOT_RUN | |
| T11 | NPC 애니메이션·재질 제어 | NOT_RUN | |
| T12 | 양면 식생 | NOT_RUN | |
| T13 | 바람 패스 일치 | NOT_RUN | |
| T14 | 바람 bounds | NOT_RUN | |
| T15 | 실제 배칭·인스턴싱 | NOT_RUN | |
| T16 | Terrain 1/2/4 layer | NOT_RUN | |
| T17 | Terrain 5 layer 차단 | NOT_RUN | |
| T18 | Terrain BaseMap | NOT_RUN | |
| T19 | Terrain Holes·타일 경계 | NOT_RUN | |
| T20 | Terrain Instancing | NOT_RUN | |
| T21 | LOD·CrossFade·Far | NOT_RUN | |
| T22 | Decal·스킬 표시 | NOT_RUN | |
| T23 | 분석·변환 반복 안전성 | NOT_RUN | |
| T24 | Apply·Rollback·충돌 | NOT_RUN | |
| T25 | Reload·Additive Scene | NOT_RUN | |
| T26 | Player 품질 변경 | NOT_RUN | |
| T27 | Addressables·원거리 로드 | NOT_RUN | |
| T28 | Release·Development | NOT_RUN | |

## 성능 기록

| 항목 | 기준선 | WorldToon | 차이·측정 근거 |
|---|---|---|---|
| GPU frame median ms | NOT_MEASURED | NOT_MEASURED | |
| GPU frame p95 ms | NOT_MEASURED | NOT_MEASURED | |
| 대상 패스 GPU ms | NOT_MEASURED | NOT_MEASURED | |
| Main thread ms | NOT_MEASURED | NOT_MEASURED | |
| Render thread ms | NOT_MEASURED | NOT_MEASURED | |
| Draw / SetPass | NOT_MEASURED | NOT_MEASURED | |
| Shadow / Depth / MotionVectors | NOT_MEASURED | NOT_MEASURED | |
| 모듈 GC Alloc | NOT_MEASURED | NOT_MEASURED | |
| 메모리 / variant / build time | NOT_MEASURED | NOT_MEASURED | |

## 도입 판정

| 계열 | 적용/보류/원본 유지 | 이유 | 승인 근거 |
|---|---|---|---|
| Mesh·Props | 미정 | | |
| NPC·Monster | 미정 | | |
| Foliage | 미정 | | |
| Terrain4 | 미정 | | |

필수 기능이 미실행/실패인 대상은 최종 도입 승인으로 표시하지 않는다.
