# WorldToon — 검증 보고(T01~T28)

- 상위: [WorldToon 작업 상태](../worldtoon.md) · 정의: [설계서 §16.2](../../전달용/WorldToon_Handoff/WorldToon_Design.md)
- 실행일: 2026-11-19. 환경: 열린 Unity 6000.3.21f1 Editor(D3D11), PC_RPAsset(Forward+), Unity MCP. **Player 빌드 없음.**
- 판정: `PASS / FAIL / NOT_RUN / NOT_APPLICABLE` + `사용자 테스트 대기`(외형) + `사용자 지시 대기`(빌드). 자동 캡처 PASS는 "렌더가 깨지지 않음"까지이며 미술 승인이 아니다.

## 실행한 검사

| 검사 | 명령·도구 | 결과 |
|---|---|---|
| 셰이더 import | `ShaderUtil.ShaderHasError` | Mesh·Foliage·Terrain4·TerrainBaseMap 오류 0 |
| 대표 변형 컴파일(D3D) | `WorldToonShaderCompileCheck.RunDeferred()` → `logs/shader-compile-check.txt`(git 제외) | 아래 「컴파일」 |
| EditMode | `UPlayGround.Editor.Tests.WorldToonConversionTests` 9개 | 아래 「EditMode」 |
| 검증 씬 | `WorldToonValidationSceneBuilder.Build()` → `Assets/01.Scenes/ArtReview/WorldToonValidation.unity`(git 제외 폴더, 도구로 재생성) | 생성·재생성 성공, 재실행 시 복제 재질 전부 재사용 |
| 고정 카메라 캡처 | `WorldToonValidationSceneBuilder.Capture("day"/"night")` → [captures/](captures/) | 5대 × 낮·밤 |
| 적용·복구 | 검증 씬에서 `ApplyToSlot`/`Rollback`(씬 미저장) | 아래 T24 |

### 컴파일

대표 키워드 조합 6종 × 패스 × 정점/프래그먼트를 D3D로 실제 컴파일했다. 1차 실행에서 TerrainBaseMap의 DepthNormals 패스 12건이 실패(`invalid subscript 'uvSplat01'`, base pass 정의 오적용)해 수정했다. 재실행 결과는 이 문서 하단 「재컴파일」에 적는다. 검사한 조합 밖의 변형은 보증하지 않는다.

## 기능 테스트

| ID | 판정 | 근거 |
|---|---|---|
| T01 보호 대상 | lilToon 셰이더 기준 PASS / 보호 캐릭터의 비lilToon 정적 슬롯 판정은 미구현 | 테스트 `LilToonMaterial_IsProtected_AndNotCopied`(원본 내용 해시 불변·생성 0), 검증 씬 lilToon 슬롯 `ApplyToSlot` → `WT-PROTECT-001` 차단, 테스트 `SkinnedSlot_IsBlockedUntilReviewed`(스키닝 슬롯 WT-MAT-002 차단). 경로 기준 보호 목록은 [ProjectAudit §6](ProjectAudit.md)에서 사람이 제외 |
| T02 Opaque/Cutout | PASS(자동) · 외형 `사용자 테스트 대기` | 테스트 `CutoutSource_UsesAlphaTestQueueAndKeyword`(AlphaTest 큐·키워드·Cutoff), 식생 카드 실루엣 캡처 |
| T03 주광·낮밤 | PASS(자동 캡처) · `사용자 테스트 대기` | 밤 캡처에서 WorldToon 재질은 주광을 끄면 어두워짐(자체 발광 없음). 대조로 원본 URP/Lit은 밝게 남았다(환경 반사로 추정, 원인 미확인) |
| T04 주광 그림자·캐스케이드 | 부분 PASS / 캐스케이드 경계·거리 페이드 NOT_RUN | 그림자 수신·생성 캡처 확인. 2캐스케이드 경계 장면 미구성 |
| T05 Point/Spot | PASS(자동 캡처) | 밤 B: Lite 바위는 횃불 무반응, Standard 바위만 반응 |
| T06 Forward+ | 부분 PASS / 비주광 방향광 NOT_RUN | Forward+ Renderer에서 점광원 반응 확인. 추가 방향광 장면 없음 |
| T07 실제 GI | 부분 PASS / Lightmap·Shadowmask NOT_RUN | Legacy 프로브 SH(Trilight 환경광) 반영 확인. 베이크 장면 없음 |
| T08 Fog·Depth | NOT_RUN | 선형 안개 장면은 있으나 원본 대비 판정과 물 깊이 합성 미검증 |
| T09 SSAO/DepthNormals | NOT_RUN | 패스 컴파일만. AO 수신 화면 비교 미실시 |
| T10 TAA/Motion Blur | NOT_RUN | 카메라는 SMAA(TAA 아님). 히트스톱 슬로모션 볼륨의 Motion Blur(0.678)가 모션벡터를 실제로 쓰므로 그 상황에서 확인해야 한다. MotionVectors 패스 컴파일만 |
| T11 NPC 애니메이션 | NOT_APPLICABLE | G4 적용 대상 없음(보호 lilToon) |
| T12 양면 식생 | PASS(자동 캡처) · `사용자 테스트 대기` | 카드 앞뒤 모두 검은 면 없음 |
| T13 바람 패스 정합 | NOT_RUN | 패스가 같은 함수를 쓰도록 구현했으나 Frame Debugger 비교 미실시 |
| T14 바람 bounds | NOT_RUN | Validator가 진폭>메시 반경을 경고하도록 구현, 화면 가장자리 시험 미실시 |
| T15 인스턴싱·배칭 | NOT_RUN | Frame Debugger 캡처 없음. SRP Batcher 호환 여부 미확인 |
| T16 Terrain 1/2/4 | 2·4 PASS(캡처) / 1레이어 NOT_RUN | 띠 혼합·레이어 경계 정상 |
| T17 Terrain 5레이어 | PASS | 테스트 + 검증 씬 `WT-TERRAIN-001` 차단, 원본 Terrain Lit 유지 |
| T18 Basemap | NOT_RUN | basemapDistance 40m 설정, 근·원 전환 비교 미실시. **위험:** Terrain이 근거리 재질의 스타일 상수를 원거리(Basemap) 재질로 복사하는지 미확인. 복사되지 않으면 프로파일을 바꿔도 원거리 지형만 기본값으로 남는다. 확인법: 검증 지형 재질 `_ShadeTint`를 기본과 다른 색으로 바꾸고 basemapDistance를 1로 낮춰 캡처 |
| T19 Holes·경계 | 컬러 PASS / 그림자·깊이 NOT_RUN | 4레이어 지형 구멍 캡처 |
| T20 Terrain Instancing | NOT_RUN | |
| T21 LOD/CrossFade·Far | NOT_RUN | |
| T22 Decal | PASS(자동 캡처) | DBuffer 데칼이 WorldToon 바위·URP 표면 모두에 합성 |
| T23 Analyze·반복 변환 | PASS | 테스트 `RepeatedConversion_*`, `HandEditedGeneratedMaterial_*`, 검증 씬 재생성 시 전부 재사용 |
| T24 Apply/Rollback·충돌 | PASS | 적용→복구 원본 일치, 적용 후 다른 재질로 바뀐 슬롯 `WT-CONFLICT-001` 차단, 보호 슬롯 차단 |
| T25 Domain Reload·Additive | NOT_APPLICABLE | 전역 상태 없음([결정](Decisions.md)) |
| (추가) 카메라 가림 페이드 | NOT_RUN | 구조상 호환(프록시가 원본 재질 복사, Stencil 블록 없음 — [ProjectAudit §3](ProjectAudit.md)). 바위·소품 후보라 실제 가림 디더 화면 확인이 필요하다 |
| T26 Player 품질 전환 | `사용자 지시 대기` | Player 빌드 필요 |
| T27 Addressables·원거리 | `사용자 지시 대기` | Player·Addressables 빌드 필요 |
| T28 Release/Development | `사용자 지시 대기` | Player 빌드 필요 |

### EditMode

2026-11-19 — **9/9 PASS**: 보호(lilToon)·Transparent 차단·반복 변환 재사용과 원본 불변·프리셋 키워드·Cutout·수정된 생성본 보호·스키닝 슬롯 차단·Terrain 4/5레이어·Manifest.

### 재컴파일

2026-11-19 22:09 — **PASS, 300개 컴파일 실패 0**(Mesh·Foliage·Terrain4·TerrainBaseMap 전 패스).

## 사용자가 볼 화면

[captures/](captures/)의 `Cam_A_Compare`(원본|WorldToon), `Cam_A_ShadeSide`(어두운 면·명암 경계), `Cam_B_Lights`(Lite|Standard, 밤), `Cam_C_Foliage`, `Cam_D_Terrain`(2|4+구멍|5레이어 차단). 검증 씬을 직접 열려면 `WorldToonValidationSceneBuilder.Build()`로 재생성한 뒤 `Assets/01.Scenes/ArtReview/WorldToonValidation.unity`를 연다(월드 원점에서 20km 떨어진 곳에 배치).
