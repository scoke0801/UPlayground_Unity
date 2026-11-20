# WorldToon — 출처 고지

| 프로젝트 파일 | 원본 | 라이선스 | 변경 |
|---|---|---|---|
| `Assets/06.Shaders/WorldToon/Includes/WorldToonTerrainInput.hlsl` | `com.unity.render-pipelines.universal` 17.3.0 `Shaders/Terrain/TerrainLitInput.hlsl` | Unity Companion License(패키지의 `LICENSE.md`) | `UnityPerMaterial` CBUFFER에 WorldToon 스타일 상수 6개 추가. 나머지 동일 |

## include로 재사용(복제하지 않음)

URP 17.3.0의 `Shaders/ShadowCasterPass.hlsl`, `DepthOnlyPass.hlsl`, `DepthNormalsPass.hlsl`, `ShaderLibrary/ObjectMotionVectors.hlsl`, `UniversalMetaPass.hlsl`, `Shaders/Terrain/TerrainLitPasses.hlsl`, `TerrainLitDepthNormalsPass.hlsl`, `TerrainLitMetaPass.hlsl`. Mesh 컬러 패스의 정점·GI 구성은 `SimpleLitForwardPass.hlsl`의 구조를 따랐다.

## URP 업데이트 시 재검증

1. `TerrainLitInput.hlsl` 원본과 위 복제본을 diff 비교해 CBUFFER·매크로 변경을 반영한다.
2. `Lighting.hlsl`의 추가 광원 루프(`USE_CLUSTER_LIGHT_LOOP`, `LIGHT_LOOP_BEGIN`)와 `GetMainLight`/`CreateAmbientOcclusionFactor` 시그니처를 확인한다.
3. `WorldToonShaderCompileCheck.RunDeferred()`로 대표 변형을 다시 컴파일한다.
