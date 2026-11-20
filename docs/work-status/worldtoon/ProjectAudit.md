# WorldToon G0 — 프로젝트 조사

- 상위: [WorldToon 작업 상태](../worldtoon.md) · [통합 계약](../../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md)
- 조사일: 2026-11-18. 방법: 파일 직접 읽기 + 열린 Editor에서 Unity MCP `execute_code` 읽기 전용 질의. 표기: **확인됨 / 해당 없음 / 미확인**.

## 1. 엔진·패키지

| 항목 | 값 | 상태 | 근거 |
|---|---|---|---|
| Unity | 6000.3.21f1 (c02631ffc030) | 확인됨 | `ProjectSettings/ProjectVersion.txt` |
| URP / Core / Shader Graph | 17.3.0 | 확인됨 | `Packages/manifest.json` |
| URP 설치 경로 | `Library/PackageCache/com.unity.render-pipelines.universal@8aa8d736cba6` | 확인됨 | 셰이더 include 기준. 패키지 업데이트 시 WorldToon 재검증 지점 |
| Addressables | 2.9.1 | 확인됨 | manifest |
| lilToon | git `#master`(버전 비고정), 잠금 커밋 `252fd8cfc46106d4967e95b3f2c788418502f227` | 확인됨 | `Packages/packages-lock.json` |

## 2. 렌더링 경로와 타깃

| 항목 | 값 | 상태 |
|---|---|---|
| 그래픽 API(Win64) | Direct3D11, Direct3D12 | 확인됨. 통합 계약의 "Direct3D11"만 적은 기존 기재는 불완전 |
| 색 공간 | Linear | 확인됨 |
| Quality 단계 | 1개(`PC`) → `Assets/Settings/PC_RPAsset.asset` | 확인됨 |
| 기본 RP | `PC_RPAsset` | 확인됨 |
| 런타임 RP 교체 | `SceneEnvironmentRendering`이 활성 월드 씬의 RP로 `QualitySettings.renderPipeline`을 바꿈(예: LakeVillage 계열은 SSAO/Decal off) | 확인됨. **WorldToon은 SSAO·DBuffer on/off 두 변형이 모두 필요**하다. 보존은 [식생 빌드 검정](../vegetation-build-black.md)의 Renderer 목록·`IncludeReferencedInScenes` 수정에 의존 |
| PC_Renderer 렌더링 모드 | `m_RenderingMode=2` = Forward+ (URP 17 enum: 0 Forward, 1 Deferred, 2 Forward+) | 확인됨(직렬화 값). 검증 캡처에서 이 Renderer로 그린 WorldToon Standard가 점광원에 반응함. 실행 중 활성 키워드는 Frame Debugger로 미확인 |
| Depth Priming | Auto(1) | 확인됨 → DepthOnly 패스 필수 |
| MSAA | 4x(PC) | 확인됨 |
| HDR / Depth·Opaque Texture | on / on / on | 확인됨 |
| 그림자 | 거리 30m, 캐스케이드 2, soft on, 추가 광원 그림자 on | 확인됨 |
| 추가 광원 | Per Pixel | 확인됨 |
| SRP Batcher | on | 확인됨 |
| 광원 프로브 | Legacy Light Probes(APV 아님) | 확인됨 → `SAMPLE_GI` SH 경로. APV 지원 주장 금지 |
| 기타 URP Asset | LakeVillage_RPAsset(그림자 80m·3캐스케이드), Mobile_RPAsset(Forward+, SSAO·Decal 없음), AmbientFauna 계열, RiverBasinPipeline, 구매 에셋 샘플 다수 | 확인됨(구매 샘플은 빌드 비대상으로 간주, 미검증) |

## 3. Renderer Feature(PC_Renderer)

| 기능 | 설정 | WorldToon 요구 |
|---|---|---|
| SSAO | Source=DepthNormals, AfterOpaque=false, Downsample, 강도 0.4, DirectLightingStrength 0.25 | `DepthNormals` 패스 + Forward에서 `_SCREEN_SPACE_OCCLUSION` 수신(직접·간접 AO 적용) |
| Decal | technique=DBuffer, decalLayers=on | Forward에서 `_DBUFFER_MRT1/2/3` + `ApplyDecalToSurfaceData`, DepthNormals에서 rendering layer 출력 |
| CameraOccluderFadeFeature | `UniversalForward`·`UniversalForwardOnly`·`SRPDefaultUnlit` 패스를 스텐실(상위 4비트) 비교 오버라이드로 재렌더 | WorldToon 컬러 패스는 LightMode `UniversalForward`, **자체 Stencil 블록 금지** |
| SpeedRadialBlur, Underwater | 후처리·수중 | 직접 의존 없음. 수면 합성 순서를 건드리지 않음 |
| AmbientFauna(별도 Renderer) | 환경 생물 | 해당 없음 |

모션 벡터 요구: **확인됨(2026-11-19, 파일 검색).** 인게임 카메라(`LakeOfLife`, `RiverBasinWorld`, `Title`, `Loading` 등)의 `m_Antialiasing: 2` = SMAA로 TAA/STP는 아니다. Motion Blur는 `Assets/Settings/Action_SlowMo_Volume Profile.asset`(강도 0.678, `GameHitStopHandler`가 사용)에서 켜지므로 **히트스톱 슬로모션 중 모션벡터가 실제로 쓰인다.** 기본 볼륨의 Motion Blur 강도는 0이다. WorldToon은 MotionVectors 패스를 유지하며 Foliage는 바람 이전 위치를 계산한다.

카메라 가림 페이드: `OccluderGroup`가 원본 `sharedMaterials`와 PropertyBlock을 프록시에 그대로 복사하고 `CameraOccluderFadeFeature`가 `UniversalForward` 패스를 스텐실로 다시 그린다. 셰이더 종류와 무관한 구조라 WorldToon(Stencil 블록 없음)과 충돌하지 않는다. 화면 확인은 NOT_RUN.

환경 런타임의 재질 속성 쓰기(WT-MAT-002 범위): `Gameplay/World`에서 재질 속성을 쓰는 곳은 반응형 환경(제외 대상), 식생 바람 `EnvironmentVegetationWind`(구매 식생 셰이더 속성), 물·하늘·생물 전용 경로뿐이다. 바위·소품 후보의 재질을 런타임에 바꾸는 코드는 찾지 못했다.

## 4. 그래픽스 설정·스트리핑

| 항목 | 값 | 상태 |
|---|---|---|
| Always Included | Legacy Diffuse, Cube*, Sprites/Default, UI/Default(ETC1), Hidden/lilToonCutout(Outline) | 확인됨. **WorldToon을 추가하지 않는다** |
| Preloaded SVC | `Assets/Settings/NewShaderVariants.shadervariants` | 확인됨 |
| Instancing/Lightmap/Fog 스트리핑 | Instancing=0(Strip unused), Lightmap=0(Auto), Fog=0(Auto), Linear/Exp/Exp2 keep | 확인됨 |
| 사례 지문 | 식생 검정 사례: Quality에 RP가 하나라 SSAO off 변형이 사전 필터로 제거됨 → PC_RPAsset Renderer 목록에 호수 Renderer 추가로 보존 | 확인됨(문서). WorldToon도 같은 경로에 의존 |

## 5. 재질 사용 현황(Assets 전체 `.mat`+모델 내장, 상위)

URP/Lit 915, Hidden/lilToonOutline 365, HS_Blend_CG 218, CFX 파티클 163, lilToon 121, Polyart Props 96, Standard 94, … `UPlayGround/Foliage/LeavesGraph` 51, `IdyllicVegetationGraph` 46, `UPlayGround/Foliage/Grass` 26, `PlantGraph` 20, Polyart M_Rocks 27, Idyllic Surface 26. 오류 셰이더(`Hidden/InternalErrorShader`) 67개가 있으나 WorldToon 범위 밖이며 수정하지 않는다(보고만).

## 6. 보호·제외 목록(초안)

| 분류 | 대상 | 근거 | 상태 |
|---|---|---|---|
| 보호(WT-PROTECT-001, 도구가 강제) | lilToon 계열 셰이더(`lilToon`, `Hidden/lilToon*`, `_lil/*`)를 쓰는 모든 재질 | 원문 보호 규칙 + [비인간형 전환](../non-humanoid-toon-migration.md) 사용자 확인 대기 | 셰이더 이름 기준 자동 차단 |
| 보호(문서 기준, 도구 미강제) | 플레이어 프리팹, `Assets/03.Prefabs/Characters/ScenarioResidents/`, `Assets/03.Prefabs/Actor/NPC/`, `Assets/03.Prefabs/Actor/Monster/`의 **모든 슬롯**(비lilToon 포함). ScenarioResidents에는 URP/Lit 50슬롯이 있다 | 통합 계약 §1 | 도구는 셰이더 기준 보호와 스키닝·애니메이터 슬롯의 WT-MAT-002 차단만 한다. 이 경로의 비lilToon 정적 슬롯을 경로로 막는 기능은 없으므로 적용 범위를 고를 때 사람이 제외한다 |
| 제외(반응형 환경) | `Assets/11.Material/EnvironmentReactive/` 6개(ArenaTree_0, CargoCrate_0, FarmGrass_0, FarmTree_0~2) 및 `UPlayGround/Environment/ToonReactive` 셰이더 | 통합 계약 §1 | 확인됨 |
| 제외(물·하늘·파티클) | `06.Shaders/Water/*`, LakeWater, ToonWaterfall, DaySky, 파티클 셰이더 | 원문 범위 밖 | 확인됨 |
| 제외(투명) | Transparent 큐/블렌드 재질 | WT-MAT-001 | 규칙 확정 |

## 7. 기존 전역값 작성자

| 전역값 | 유일 작성자 | WorldToon 결정 |
|---|---|---|
| `_LW_Wind`, `_LW_PreviousWind`, `_LW_Wetness`, `SICSGlobalWind*` | `Gameplay/World/LivingWorld/EnvironmentShaderState` | Foliage 바람은 이 값을 **읽기만** 한다. 새 바람 작성자 금지 |
| 접촉 변위(`_UPG_*` 배우 위치) | `Manager/World/FoliageInteractionManager` | 경쟁 입력 금지. G3에서 재사용 여부 판단 |
| 반응형 표면 버퍼 | `ReactiveEnvironment/EnvironmentSurfaceStateBuffer` | 해당 재질 제외 |

## 8. 후속 조사 결과와 남은 미확인

- 카메라 AA·Motion Blur, lilToon 커밋: 위 §1·§3에서 확인됨.
- 지형(G5): 프로젝트 TerrainData 137개 중 4레이어 이하·레이어 노멀/마스크 없음은 80개이며 전부 WorldBuilder 생성 타일. 주요 월드 지형은 7~27레이어. (로그 `logs/g5-terrain-audit.txt`, git 제외)
- 식생(G3): 확인한 식생 프리팹 메시는 Read/Write가 꺼져 정점 색 바람 마스크를 읽을 수 없음. Terrain Detail/Tree 배치 경로별 분류는 미확인.
- NPC·몬스터(G4): 슬롯 분포는 [작업 상태](../worldtoon.md) G4 행. 재질 애니메이션 클립 조사는 적용 대상이 없어 하지 않음.
