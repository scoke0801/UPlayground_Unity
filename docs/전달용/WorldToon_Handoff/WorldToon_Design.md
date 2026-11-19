# WorldToon — 경량 월드 툰 셰이더 설계안

> **Claude · Codex 구현 전달용 / v1.0 / 2026-11-16**  
> 적용 대상: 일반 NPC·몬스터, 지형, 건물·바위·소품, 잔디·덤불·나뭇잎  
> 유지 대상: 플레이어 및 명시적으로 보호한 주요 캐릭터의 기존 lilToon

## 00. 결정 요약

**캐릭터용 lilToon은 유지한다. 주변 월드는 동일한 툰 명암 규칙을 공유하는 경량 셰이더군으로 분리한다.** 하나의 거대한 셰이더에 모든 기능을 넣지 않는다. NPC와 일반 메시 오브젝트는 같은 Mesh 계열을 사용하고, 식생과 Unity Terrain은 각각의 렌더링 요구를 처리한다.

이 문서는 **구현 명세**다. 셰이더 소스 구현, 프로젝트 컴파일, 실제 GPU 성능 검증까지 완료되었다는 의미는 아니다. 성능 수치와 초기 파라미터는 별도 표기가 없으면 설계 목표이며 측정 결과가 아니다.

| 항목 | 확정할 방향 |
|---|---|
| 기준 환경 | Unity 6 + URP. 정확한 에디터·패키지 버전 및 렌더링 경로는 G0에서 프로젝트 파일로 확정 |
| 구현 방식 | ShaderLab + HLSL. 공통 라이팅·알파·바람 로직을 include로 재사용 |
| 기본 외형 | Base Color + 2단 툰 명암 + 주 광원 그림자 + 환경광 + Fog |
| 기본 제외 | Outline, MatCap, Rim, PBR 금속 반사, Parallax, Fur, Refraction, 범용 Transparent |
| Mesh | NPC·몬스터·건물·바위·일반 소품. Opaque/Cutout, Lite/Standard의 제한된 프리셋 |
| Foliage | Cutout + 양면 선택 + 정점 바람 + 검증된 인스턴싱 경로 |
| Terrain | Unity Terrain 전용. v1은 타일당 Terrain Layer 최대 4개, 원거리 Basemap·Holes·Terrain Instancing 유지 |
| 교체 정책 | 분석 → 복제 → 미리보기 → 검증 → 명시적 적용. 원본 일괄 덮어쓰기 금지 |
| 도입 순서 | G0 조사 → Mesh 최소본 → 식생 → NPC → Terrain → 실제 빌드 검증 |
| 성공 판정 | 외형·기능 검증과 측정이 모두 통과한 범위만 적용. 개선이 없으면 기존 재질 유지 |

**먼저 고칠 기존 설명의 오해:** lilToon을 쓴다는 사실만으로 무겁다고 단정하지 않는다. lilToon은 자체 최적화와 Lite 변환을 제공한다. 비교 기준은 최적화된 실제 Player 빌드이며, 코드 줄 수나 Inspector 옵션 수로 성능을 판정하지 않는다.[S01][S02]

## 01. 범위와 보호 규칙

### 1.1 대상 분류

| 분류 | 기본 처리 | 예외 |
|---|---|---|
| 플레이어·주요 연출 캐릭터 | 기존 lilToon 유지 | 사용자가 별도로 승인한 경우만 변경 |
| 일반 NPC·일반 몬스터 | Mesh Standard 또는 Mesh Lite | 특수 얼굴·눈·머리카락·투명 재질은 슬롯별 원본 유지 가능 |
| 건물·바위·절벽·동굴 메시 | Mesh Lite, 조명 요구가 있으면 Standard | 물·유리·특수 반사·특수 VFX는 제외 |
| Unity Terrain | Terrain4 | 5개 이상 레이어/미지원 기능이 있으면 기존 Terrain Lit 유지 |
| 잔디·나뭇잎·덤불 | Foliage | SpeedTree 전용 변형·빌보드는 별도 호환 검증 전 원본 유지 |
| 나무 줄기 | Mesh Lite/Standard | 줄기까지 바람 변형이 필요하면 별도 승인된 Foliage 프리셋 |
| 수면·폭포·안개·파티클 | 기존 셰이더 유지 | 이번 작업 범위 밖 |

NPC도 **모델 전체가 아닌 머티리얼 슬롯 단위**로 이관할 수 있어야 한다. 옷·피부는 WorldToon, 눈·투명 머리카락은 원본으로 남기는 부분 적용을 허용한다. 혼합 적용이 무조건 빠른 것은 아니므로 실제 패스 수와 외형을 함께 확인한다.

### 1.2 비목표

이번 작업으로 lilToon 전체 기능, 자체 렌더 파이프라인, GPU 군중 스키닝, VAT, Compute 기반 식생 생성기, 새 월드 스트리밍 시스템을 만들지 않는다. 기존 Animancer·Animator·AI·NavMesh·게임플레이·월드 생성 구조는 변경하지 않는다. 신규 외부 에셋 구매와 엔진/URP 업그레이드를 필수 조건으로 만들지 않는다.

**금지:** `Library/PackageCache` 직접 수정, URP 패키지의 공용 라이팅 코드 전역 패치, 원본 lilToon 소스 수정, 원본 재질·텍스처·TerrainData 덮어쓰기, 프로파일링 없이 전체 월드 강제 교체.

## 02. G0 — 프로젝트 조사와 기준선

이전 대화의 버전 번호를 현재 저장소의 확정값으로 사용하지 않는다. 아래 항목을 읽어 `Docs/WorldToon/ProjectAudit.md`에 기록한다. 버전이 다르다는 이유만으로 프로젝트를 업그레이드하지 않는다.

| 조사 항목 | 확인할 파일·데이터 | 결정에 미치는 영향 |
|---|---|---|
| 에디터·URP·Shader Graph 버전 | `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `Packages/packages-lock.json` | include 경로, 매크로, 패스 구현 기준 |
| 렌더링 경로 | Graphics/Quality의 모든 URP Asset 및 Renderer Data | Forward, Forward+, Deferred 여부 |
| 실제 타깃 | Build Profile/Player 설정, 지원 그래픽스 API | 컴파일·성능 측정 대상 |
| 카메라 기능 | SSAO, TAA/STP, Motion Blur, MSAA, Depth Texture, Depth Priming, 카메라 스택 | DepthNormals·MotionVectors·알파 처리 |
| 조명 기능 | Main/Additional Lights, 그림자, Mixed Lighting, Lightmap, Light Probes, APV, Rendering Layers | 필수 라이팅 기능 및 배리언트 |
| 특수 렌더링 | Decal Renderer Feature, DBuffer/Screen-space, 커스텀 안개, 물 Depth 사용 | 별도 호환 요구 |
| lilToon | 설치 버전, 최적화 설정, 실제 사용 재질과 키워드 | 원본 보존·변환 가능성·비교 기준 |
| 월드 지형 | TerrainData, 타일별 레이어 수, Holes, Basemap 거리, Instancing | Terrain4 적용 대상 |
| 식생 | Terrain Detail/Tree, MeshRenderer, 기존 인스턴서, LODGroup, SpeedTree | 셰이더가 연결될 실제 렌더링 경로 |
| NPC | SkinnedMeshRenderer 수, 슬롯 수, 기존 LOD, BlendShape, 재질 애니메이션 | 스키닝 유지 및 변환 위험 |
| 빌드 자산 | Addressables 그룹, 품질별 재질, 스트리핑 설정 | Player에서 누락되는 변형 방지 |

기록 형식은 `확인됨 / 해당 없음 / 미확인`을 구분한다. 자동 조사로 확정 가능한 값은 사용자에게 다시 묻지 않는다. 에디터·플레이어를 실행할 수 없으면 정적 조사와 테스트 코드를 먼저 작성하고, 실행 검증은 `NOT_RUN`으로 남긴다.

**렌더링 경로 결정:** v1의 우선 지원은 실제 프로젝트의 Forward 또는 Forward+다. Deferred가 활성화되어 있으면 `UniversalForwardOnly` 경로 및 필요한 DepthNormals를 검증하는 별도 분기를 사용한다. 셰이더 때문에 프로젝트 전체를 Forward로 자동 변경하지 않으며, 검증 전 Deferred 지원 완료라고 쓰지 않는다.[S09]

## 03. 구조 — 표현은 공유하고 렌더링은 분리

```text
기존 캐릭터                         WorldToon
lilToon (수정하지 않음)             WorldToonStyleProfile
        │                          색조 / 명암 경계 / 환경광 강도
        │ 외형 기준                         │
        └──────────────┬────────────────────┘
                       │   ToonLighting.hlsl
         ┌─────────────┼───────────────┐
         │             │               │
      Mesh 계열      Foliage 계열     Terrain 계열
   NPC / 건물 / 바위  풀 / 잎 / 덤불  Terrain4 + Basemap
   기존 Renderer     기존 배치 시스템  Terrain 네이티브 경로
         │             │               │
         └────── 검증·변환·품질 프리셋 도구 ──┘
```

WorldToon의 공통 프로파일은 **WorldToon 셰이더군**을 제어한다. lilToon까지 같은 수식으로 바뀌는 것이 아니다. 원본 캐릭터와 나란히 놓고 색감·대비·그림자 밝기를 맞춘다. 모든 재질의 물리적 수치가 같거나 픽셀 단위로 동일해야 한다는 목표는 두지 않는다.

### 3.1 권장 파일 구조

실제 프로젝트의 네임스페이스·asmdef·폴더 규칙이 있으면 그것을 우선한다. 아래는 기본안이다.

```text
Assets/Project/Rendering/WorldToon/
  Runtime/
    WorldToonStyleProfile.cs
    WorldToonQualityProfile.cs
    WorldToonGlobals.cs
    WorldToonMaterialRegistry.cs
    WorldToonRendering.Runtime.asmdef
  Shaders/
    WorldToonMesh.shader
    WorldToonFoliage.shader
    WorldToonTerrain4.shader
    Hidden/WorldToonTerrainBaseMap.shader
    Hidden/WorldToonTerrainBaseMapGen.shader
    Includes/
      WorldToonInput.hlsl
      WorldToonLighting.hlsl
      WorldToonURPAdapter.hlsl
      WorldToonAlpha.hlsl
      WorldToonWind.hlsl
      WorldToonMeshPasses.hlsl
      WorldToonFoliagePasses.hlsl
      WorldToonTerrainPasses.hlsl
  Editor/
    WorldToonShaderGUI.cs
    WorldToonAuditWindow.cs
    WorldToonMaterialConverter.cs
    WorldToonTerrainValidator.cs
    WorldToonBuildValidator.cs
    WorldToonVariantManifest.cs
    WorldToonRendering.Editor.asmdef
  Profiles/
  Generated/Materials/
  Generated/Textures/
  Tests/EditMode/
  Tests/PlayMode/
  Samples/WorldToonValidation.unity
Docs/WorldToon/
  ProjectAudit.md
  Decisions.md
  ConversionReport.json
  ValidationReport.md
  PerformanceReport.md
  ThirdPartyNotices.md
```

**각 단계에서 실제로 사용하는 파일만 만든다.** 빈 구현 파일을 한꺼번에 만드는 방식은 피한다. 특히 Renderer Feature는 기본적으로 필요하지 않다. 선택적 Outline·특수 효과를 위해 미리 RenderGraph 패스를 추가하지 않는다.

## 04. 공통 툰 라이팅 계약

### 4.1 최소 수식

아래는 **의도 설명용 의사 코드**이며 복사 즉시 컴파일되는 URP 코드가 아니다. 실제 조명/프로브 함수와 구조체는 설치된 URP 패키지 기준으로 작성한다.

```hlsl
// 입력 색은 선형 공간. normalWS와 lightDirWS는 정규화.
half ndl = saturate(dot(normalWS, mainLight.direction));
half edge = max(style.bandSoftness, material.minimumEdgeWidth);
// AA 보정은 옵션. 필요한 프리셋에서만 화면 미분을 사용한다.
edge = max(edge, optionalDerivativeAA(ndl));
half band = smoothstep(style.bandThreshold - edge,
                       style.bandThreshold + edge, ndl);
half visibility = saturate(mainLight.shadowAttenuation)
                * mainLight.distanceAttenuation;

half3 directTint = lerp(style.shadeTint, style.litTint, band);
half3 direct = mainLight.color * directTint * visibility;
half3 indirect = SampleSelectedGI(normalWS) * style.indirectStrength;

half3 extra = EvaluateEnabledAdditionalIrradiance();
half3 color = baseColor * (direct + indirect * occlusion + extra);
color += materialEmission; // v1 기본값 0. 미지원 재질은 변환 제외.
return ApplyProjectFog(color);
```

**규칙:** 툰 명암 단계와 그림자맵 가시성을 구분한다. shadow attenuation을 명암 입력에 섞은 뒤 또 곱해 그림자를 이중으로 어둡게 만들지 않는다. 위 식은 하나의 아트 모델이며 물리 기반 BRDF는 아니다. Shade Tint는 어두운 쪽의 직접광 표현이고, 밤·실내의 최소 밝기는 별도 환경광/프로브 정책으로 결정한다.

주 광원이 꺼졌는데 재질이 자체 발광하듯 밝게 남는 하드코딩을 금지한다. `GetMainLight()`만 호출하고 그림자를 지원했다고 판정하지 않는다. 설치된 URP의 shadow 좌표·cascade·shadow mask 경로를 사용한다.[S08]

### 4.2 표현 정책

| 요소 | v1 결정 |
|---|---|
| 명암 | 2단, 경계 Softness 조절. 3단 이상과 Ramp Texture는 기본 제외 |
| Base Color | `_BaseMap × _BaseColor`. 텍스처 없는 단색은 White 텍스처로 호환 |
| 주 광원 | URP가 제공하는 Main Light 방향·색·세기 사용 |
| 환경광 | Mesh는 필요한 SH/프로브/라이트맵 경로. Terrain Detail은 별도 정책 |
| 그림자 | Near/Mid 기본 수신. Far는 명시적 무그림자 프리셋 허용 |
| 안개 | 프로젝트의 실제 URP Fog 경로와 통합. 임의의 별도 거리 안개 중복 적용 금지 |
| Normal Map | Standard의 승인된 재질만. Lite/Foliage/Terrain4 기본 제외 |
| 반사·하이라이트 | 기본 제외. 중요한 금속·보석은 기존 셰이더 유지 |
| Emission | v1 기본 제외. 필수 에셋은 보존하거나 별도 승인된 확장으로 구현 |
| SSAO | 프로젝트에서 요구할 때 표면 기여도 처리까지 검증. DepthNormals 출력만으로 수신 완료라고 하지 않음 |
| 색 공간 | 프로젝트 설정 유지. 신규 프로파일·베이크 산출물의 sRGB/Linear 정책을 명시 |

### 4.3 추가 광원

`MainOnly`와 `MainPlusAdditional` 두 모드를 둔다. **단순함을 이유로 횃불·실내 조명이 필요한 NPC를 MainOnly로 강제하지 않는다.** 밤의 마을·실내 NPC와 가까운 건축물은 Standard를 기본 후보로 둔다.

추가 광원은 기본적으로 단순 확산광이며, 광원마다 완전한 다단 툰 램프를 반복하지 않는다. 추가 광원 그림자는 v1 기본 제외하되 실제 게임의 필수 표현이면 G0에서 의존성으로 승격한다. 기능을 제외했을 때 외형이 허용되지 않는 재질은 원본 유지 대상으로 분류한다.

Forward와 Forward+의 광원 순회 방식은 다르다. 설치 버전의 공식 루프를 사용하며, Forward+에서 `GetAdditionalLightsCount()`만 이용한 단순 `for` 루프를 만들지 않는다. 비주광 방향광과 clustered punctual light 경로를 확인한다. 최신 웹 예제의 `_FORWARD_PLUS` 또는 이름이 다른 매크로를 현재 패키지에 존재하는지 확인 없이 복사하지 않는다.[S07]

**v1은 임의의 ‘첫 두 개 광원’ 제한을 만들지 않는다.** 어떤 광원이 먼저 나오는지에 의존하는 결과와 팝핑을 피하기 위해, 현재 렌더러의 광원 목록을 따르거나 프리셋 전체에서 추가 광원을 끈다. 더 강한 광원 예산은 별도 검증 과제로 남긴다.

## 05. 프리셋·데이터 모델

### 5.1 셰이더와 품질 프리셋 구분

품질 등급마다 셰이더 코드를 복제하지 않는다. 한 계열의 공통 코드와 소수의 승인된 정적 조합을 사용한다. 색·명암 경계·바람 세기는 일반 상수이고, 큰 비용 차이를 만드는 기능만 정적 키워드로 분리한다.

| 프리셋 ID | 대상 | 주광 그림자 수신 | 추가 광원 | Normal Map | Outline |
|---|---|---|---|---|---|
| `MeshLite` | 야외 바위·단순 소품·일반 NPC | 켬 | 끔 | 끔 | 없음 |
| `MeshStandard` | 가까운 NPC·실내·횃불 주변 | 켬 | 켬 | 기본 끔, 승인 재질만 켬 | 없음 |
| `MeshFar` | 작은 화면 점유의 원거리 LOD | 끔 | 끔 | 끔 | 없음 |
| `FoliageNear` | 가까운 풀·잎 | 켬 | 기본 끔 | 끔 | 없음 |
| `FoliageFar` | 원거리 식생/승인된 LOD | 끔 | 끔 | 끔 | 없음 |
| `Terrain4` | 주 지형 | 켬 | 기본 끔, 요구 시 승인 조합 | 끔 | 없음 |

`MeshFar`도 기본 주광 방향과 환경광은 유지한다. ‘Far = Unlit’로 자동 전환하여 낮밤이 깨지게 하지 않는다. 정확한 프리셋-키워드 조합은 `WorldToonVariantManifest`가 단일 기준이다.

### 5.2 ScriptableObject 계약

| 타입 | 책임 | 포함하지 않을 것 |
|---|---|---|
| `WorldToonStyleProfile` | Lit/Shade Tint, 명암 Threshold/Softness, 간접광 강도, 식생 환경광 색 | 머티리얼마다 매 프레임 반복되는 로직 |
| `WorldToonQualityProfile` | 프리셋 선택, 선택적 그림자 정책, 기존 LOD/식생 시스템에 넘길 거리·밀도 가이드 | 새로운 월드 스트리밍/AI/스키닝 시스템 |
| `WorldToonVariantManifest` | 사용 셰이더·프리셋·키워드·패스·플랫폼 목록 | 임의의 전역 키워드 활성화 |
| `WorldToonMaterialRegistry` | 변환 재질 및 품질별 재질 참조, 원본 매핑 | 런타임 재질 무한 생성 |

구조 예시:

```csharp
// 설계 계약 예시. 실제 파일·직렬화·네임스페이스는 프로젝트 규칙을 따른다.
public enum WorldToonPreset
{
    MeshLite, MeshStandard, MeshFar,
    FoliageNear, FoliageFar, Terrain4
}

public enum WorldToonConversionStatus
{
    Ready, NeedsReview, Unsupported, Protected, Applied, RolledBack
}

public interface IWorldToonMaterialAnalyzer
{
    MaterialAnalysis Analyze(Material source, RendererContext context);
}

public interface IWorldToonMaterialConverter
{
    ConversionResult CreateCopy(MaterialAnalysis analysis,
                                WorldToonPreset preset,
                                string destinationFolder);
}
```

### 5.3 전역 상태 계약

`WorldToonGlobals` 하나가 `_WT_*` 전역 스타일 및 바람 시간/이전 시간을 제공한다. 재질별 값은 `UnityPerMaterial`, 전역값은 별도 전역 uniform으로 구분한다. **같은 이름을 Material Property와 Global 양쪽에 넣어 우선순위를 혼동하지 않는다.** 글로벌 스타일을 각 머티리얼에 매 프레임 복사하지 않는다.

색조 프로파일은 시작·변경 시 갱신하고, 바람 시간만 필요하면 프레임마다 갱신한다. 낮밤 시스템이 있으면 기존 시스템의 이벤트/시간값을 소비하며 두 번째 태양 제어기를 만들지 않는다. Additive Scene 로드 시 중복 컨트롤러를 경고하고 소유자를 하나로 고정한다. Disable/도메인 리로드/Play Mode 종료 시 기본값 복구와 테스트 간 상태 격리를 제공한다.

전역 스타일은 모든 카메라에서 공유한다. 다른 스타일을 요구하는 프리뷰 카메라는 별도 미리보기 환경 또는 상태 저장·복구로 격리하며, 게임 카메라의 값을 덮어쓰지 않는다. 미래의 바이옴별 값은 머티리얼/데이터 확장으로 처리하고, v1에서 청크마다 전역 색을 경쟁적으로 설정하지 않는다.

## 06. Mesh — 일반 NPC와 오브젝트

NPC와 소품을 서로 다른 라이팅 코드로 만들지 않는다. `WorldToonMesh.shader`에서 같은 표현을 사용하되 프리셋과 원본 재질의 알파 요구를 구분한다.

### 6.1 표면 계약

| 항목 | 규칙 |
|---|---|
| Opaque | 기본. `ZWrite On`, 불투명 블렌드, 기본 Back-face culling |
| Cutout | 실제 구멍이 필요한 슬롯만 사용. Base alpha·Cutoff를 모든 관련 패스에서 동일하게 적용 |
| Transparent | v1 미지원. 불투명으로 조용히 변환하지 않고 `Unsupported` 또는 원본 유지 |
| 양면 | 실제로 필요한 재질만. 임의로 전체 NPC와 건물을 `Cull Off`로 설정하지 않음 |
| Normal Map | Standard의 승인 조합만. Tangent·스케일·방향 및 DepthNormals 일관성 검사 |
| 텍스처 | BaseMap 필수 슬롯. Packed Map·추가 샘플은 필요성을 입증한 뒤 확장 |
| 그림자 | 수신 여부와 생성 여부를 구분. 생성은 Renderer/LOD 정책도 함께 다룸 |
| 베이크 조명 | 기존 Lightmap을 사용하는 건물은 UV·샘플링·Meta·Mixed Lighting 의존성 유지 |
| 이동 NPC의 GI | 현재 프로젝트의 Light Probes 또는 APV 요구에 맞춤. 지원하지 않으면 배포 차단 |

샘플 수 목표는 **BaseMap 1회, Standard에서 승인된 Normal Map 1회 추가**다. 이는 표면 재질 샘플만 센 값이며 shadow map, lightmap, APV, SSAO 등 전체 샘플 수가 1~2회라는 뜻이 아니다.

### 6.2 NPC 연결 규칙

SkinnedMeshRenderer, Bone, BlendShape, Animancer 동작은 그대로 둔다. 새로운 버텍스 셰이더에서 CPU/GPU 스키닝을 다시 구현하지 않는다. 일반 MeshRenderer용 GPU Instancing 옵션을 켰다고 SkinnedMeshRenderer NPC가 자동으로 인스턴싱되는 것은 아니다.[S05]

변환 전에 재질 속성을 애니메이션/게임 코드가 수정하는지 조사한다. `_MainTex`, `_Color`, 디졸브, 피격색, 투명도 등의 문자열 프로퍼티 참조도 검색한다. 이름이 바뀌면 어댑터 또는 명시적 호출부 수정이 필요하다. 기존 속성을 무시한 채 ‘보이는 것만’ 통과시키지 않는다.

NPC 얼굴의 조명 품질이 부족하면 얼굴 SDF·머리카락 스텐실·전용 하이라이트를 v1에 모두 추가하지 않는다. 우선 그 슬롯 또는 해당 NPC를 원본 유지 대상으로 돌린다. 대화 카메라로 확대되는 NPC는 일반 군중과 별도로 검증한다.

**효과의 범위:** 셰이더 단순화는 라이팅/샘플링/일부 패스 비용을 줄일 후보이다. NPC의 AI, 애니메이션 평가, 스키닝, 머티리얼 슬롯 수, 삼각형 수는 별개다. 이 비용이 병목이면 해당 시스템의 별도 최적화 과제로 기록한다.

## 07. Foliage — 식생 전용

식생은 단순한 색 계산뿐 아니라 알파 픽셀, 겹침, 양면, 그림자, 바람, 배치 효율을 함께 관리한다. 기본 정책은 **Cutout + 한 개의 BaseMap + 단순 툰 명암 + 정점 바람**이다. Transparent 블렌딩은 기본 경로로 제공하지 않는다. Cutout도 비용이 없다는 뜻은 아니며 타깃 장치에서 측정한다.

### 7.1 지원 경로

| 경로 | v1 방침 |
|---|---|
| 기존 Terrain의 Instanced Detail Mesh | 우선 검증. 프리팹 재질·셰이더와 인스턴싱 변형 확인 |
| 기존 MeshRenderer + LODGroup | 지원. 실제 SRP Batcher/Instancing 경로를 측정 |
| 이미 있는 명시적 인스턴서 | 기존 데이터 계약에 연결. 셰이더만 별도로 교체 |
| 신규 Compute/Indirect 식생 시스템 | v1에서 만들지 않음 |
| Terrain의 레거시 Grass/Vertex Lit | 커스텀 재질의 모든 속성이 적용된다고 가정하지 않음 |
| Terrain Tree/SpeedTree | Detail과 동일하다고 취급하지 않음. 바람·빌보드·LOD 호환성을 별도 검사 |

Unity의 Instanced Detail Mesh는 지정한 프리팹의 재질/셰이더를 사용하지만, 해당 경로의 인스턴스별 Light Probe/Lightmap 조명에는 제약이 있다. 일반 MeshRenderer의 프로브 경로를 그대로 사용할 수 있다고 가정하지 않는다.[S06]

**Terrain Detail의 기본 GI는 전역 하늘/땅 색 기반의 저비용 환경광**으로 정한다. 따라서 동굴·실내의 위치별 간접광은 자동으로 해결되지 않는다. 그런 공간에는 Terrain Detail을 배치하지 않거나 검증된 프로브 대응 렌더링 경로를 사용한다. APV의 월드 위치 샘플링은 향후 선택지지만, Terrain Detail 기본 지원으로 표기하지 않는다.

### 7.2 정점 바람 데이터

| 데이터 | v1 의미 | 기본값/검증 |
|---|---|---|
| Vertex Color R | 바람 가중치. 뿌리 0, 끝 1 | 색이 없으면 자동으로 1로 간주하지 말고 경고 |
| Vertex Color G | 예약, v1 미사용 | 기존 AO/색 데이터 덮어쓰기 금지 |
| Vertex Color B | 선택적 위상/개체 변주 보조 | 미사용 시 고정값 |
| Vertex Color A | 예약, v1 미사용 | BaseMap alpha와 혼동하지 않음 |
| Material Wind Amplitude | 최대 횡방향 이동량의 기준 | 월드 단위 정의 및 bounds 확장에 사용 |
| Global Wind Direction | 월드 수평 방향 | 0 벡터 입력에 안전한 기본값 |
| Current/Previous Wind Time | 게임 시간 기준 | 일시정지 시 정지, 카메라 컷/재시작 시 history reset |

기본 바람은 월드 공간 위치/개체 위상으로 계산하는 저주파 1~2개의 사인파와 정점 가중치다. 추가 노이즈 텍스처를 필수로 하지 않는다. 바람에 의해 위치가 달라진 정점을 ShadowCaster·DepthOnly·DepthNormals·MotionVectors에도 일관되게 적용한다. ShadowCaster의 bias는 변형된 위치·노멀 처리와 맞춰 검증한다.

기존 모델에 Vertex Color가 없거나 다른 용도로 사용 중이면 원본 메시를 수정하지 않는다. 복제 메시의 높이 기반 가중치 생성은 **단순 풀 모델에만 명시적으로 실행**할 수 있다. 여러 줄기·수관이 섞인 나무에는 자동 생성 성공을 보장하지 않는다.

월드 피벗/오브젝트 원점을 위상값으로 사용하는 경로는 Static Batching과의 호환을 확인한다. 필요하면 바람 대상의 Static Batching만 제외한다. 프로젝트 전체의 배칭을 끄지 않는다.

### 7.3 양면 노멀·경계·그림자

양면 카드는 앞/뒤 면 노멀 처리 규칙을 명시한다. 기본은 뒷면 노멀 반전 후 라이팅하고, 스타일용 상향 노멀 혼합은 선택적 재질값으로 둔다. 나무 줄기나 모든 NPC에 이 보정을 적용하지 않는다.

알파 Cutoff, UV, LOD 디더, 정점 변형을 패스별로 복붙하지 말고 공통 함수로 유지한다. 작은 잎의 mip에서 형상이 갑자기 사라지는지 검사한다. 알파 커버리지/텍스처 임포트 조정은 원본 대신 복제 자산에 적용하고, 과도한 카드 겹침을 셰이더만으로 해결하려 하지 않는다.

잔디의 그림자 **생성**은 기본 Off 후보이고, 근거리 중요 덤불·수목은 On 후보다. 모두 실제 검증 후 채택한다. Shader의 Receive Shadows Off만으로 ShadowCaster 비용이 사라진다고 설명하지 않는다. Renderer, Terrain Detail 경로 및 실제 shadow draw를 확인한다.

바람의 최대 변위만큼 culling bounds가 안전해야 한다. Terrain Detail처럼 bounds를 직접 제어하기 어려운 경로는 그 렌더러의 지원 방식을 확인하고 카메라 가장자리 테스트를 통과시키기 전 적용하지 않는다. `updateWhenOffscreen=true` 같은 광범위한 우회 설정을 기본 해결책으로 쓰지 않는다.

## 08. Terrain — Terrain4 전용 경로

**Unity Terrain에 일반 Mesh 셰이더를 그대로 넣는 방식은 사용하지 않는다.** Terrain의 control/splat blending, 타일 좌표, height-derived normal, instancing, holes, 원거리 basemap 등 네이티브 계약을 유지한 상태에서 최종 라이팅을 WorldToon으로 교체한다.

### 8.1 구현 원칙

설치된 URP 패키지의 `Shaders/Terrain/` 구현을 읽고 정확한 버전·관련 파일을 기록한다. 필요한 파일/로직만 프로젝트 소유 영역으로 분리하며, 출처와 고지사항을 유지한다. GitHub `master`는 구조를 이해하는 자료일 뿐 설치 버전의 대체물이 아니다. Terrain Lit의 BaseMap/생성 셰이더 의존 구조는 공식 소스에서도 확인할 수 있다.[S11][S12]

| 구성 | 책임 |
|---|---|
| `WorldToonTerrain4.shader` | 최대 4개 TerrainLayer의 표면 혼합 + 툰 라이팅 |
| `WorldToonTerrainBaseMap.shader` | 원거리 합성 색을 같은 명암·그림자·안개 규칙으로 렌더링 |
| `WorldToonTerrainBaseMapGen.shader` | 원거리용 표면 데이터 생성. 시간대 라이팅을 텍스처에 굽지 않음 |
| `WorldToonTerrainValidator` | 레이어 수, 의존 셰이더, holes, basemap, 타일 경계 검사 |

**v1에서는 Terrain 한 타일의 등록 Layer 수를 4개 이하로 제한한다.** 월드 전체에 4개 재질만 쓰라는 뜻이 아니다. 숲·가을숲·메마른 지역·평야·고산·강가·호숫가 등 바이옴마다 다른 4개 이하의 타일 팔레트를 사용할 수 있다. 지역/바이옴 종류와 한 타일의 Terrain Layer 개수는 별개다.

URP의 레이어 패스 비용을 고려한 보수적 제한이며, 4개를 초과하는 기존 타일을 자동으로 잘라내거나 페인트 데이터를 재배치하지 않는다. 해당 타일은 원본 Terrain Lit 유지와 `WT-TERRAIN-001` 경고 대상으로 둔다.[S10]

### 8.2 근거리 처리 순서

```text
Terrain native vertex/instance setup
  → UV / control map / layer scale·offset
  → layer weight blend (최대 4)
  → Base Color 혼합
  → 지형 노멀과 선택된 GI 준비
  → WorldToon 라이팅 한 번
  → Main Shadow / 필요한 Additional Light / Fog
```

v1은 레이어별 Normal Map·Height Blend·고급 Mask Map·Tri-planar를 기본 제외한다. **기존 Terrain이 이에 의존하면 일반 변환 대상이 아니다.** 실제 사용 의존성을 검사해 `NeedsReview`로 남기고 유지한다. TerrainLayer의 레이아웃, 타일링, 색상 remap, 알파 의미도 정확히 지원하는 범위를 문서화한다.

4개 레이어를 하나의 패스에서 혼합한 뒤 툰 라이팅을 수행한다. 향후 다중 AddPass를 추가할 때 각 패스의 비선형 툰 계산·환경광·안개가 누적되어 색이 변할 수 있으므로 단순한 패스 복제를 금지한다. v1은 AddPass 확장을 하지 않는다.

### 8.3 필수 검증

타일 경계의 높이·색·노멀, 가까운 지형과 원거리 Basemap의 색감, Basemap 재생성, Terrain Holes의 컬러·깊이·그림자, Terrain Instancing On/Off, 캐스케이드 경계, 낮밤 전환을 검증한다. Holes나 basemap이 깨지면 ‘최적화 때문에 생긴 제한’으로 묵인하지 않는다.

원거리용 텍스처에는 낮 시간의 명암을 굽지 않는다. Basemap 거리 변경은 별도 품질 최적화이며 동일 조건 A/B에서는 고정한다. `_Control`, `_Splat*`, Terrain 전용 바인딩을 월드 전역으로 직접 덮어쓰지 않는다.

## 09. 렌더 패스와 기능 의존성

‘Forward 한 패스’는 **컬러 라이팅 패스가 한 개**라는 뜻이다. 그림자·깊이·모션벡터까지 포함하여 draw가 무조건 한 번이라는 뜻이 아니다. 필요 없는 패스를 실행하지 않는 것과, 필요한 패스를 제거해 화면을 깨뜨리는 것을 구분한다.

| 패스/기능 | 적용 조건 | 반드시 일치할 것 |
|---|---|---|
| `UniversalForward` 또는 `UniversalForwardOnly` | 실제 렌더링 경로에 따라 선정 | 공통 라이팅, Base alpha, 인스턴스 데이터 |
| `ShadowCaster` | 그림자를 생성하는 재질/렌더러 | Cutout·바람·LOD 상태, 올바른 bias |
| `DepthOnly` | Depth prepass/Depth Priming 등 필요 시 | 실제 표면 실루엣·정점 변형 |
| `DepthNormals` 계열 | SSAO/노멀 요구 기능이 있을 때 | URP가 기대하는 노멀 인코딩·알파·변형 |
| `MotionVectors` | 현재 TAA/STP/Motion Blur 등 요구 시 | 스키닝, 이전 위치/시간, 알파, 디더 |
| `Meta` | GI 베이크 대상 정적 재질 | 색/발광 입력. 툰 그림자 자체를 베이크하지 않음 |
| Scene selection/picking | 에디터 선택/페인팅에 필요한 경우 | Terrain/식생 선택과 도구 사용 정상 |
| Decal 지원 | 프로젝트의 필수 decal이 있을 때 | 실제 DBuffer/Screen-space 경로를 별도 검증 |

패스의 **이름(Name)** 과 **LightMode 태그**를 혼동하지 않는다. `DepthNormals`/`DepthNormalsOnly`는 실제 설치 버전과 렌더러 요구를 따르며, 이름만 둘 다 복제해 중복 렌더링을 만들지 않는다.[S09]

URP의 Motion Vector fallback은 알파 클리핑·LOD CrossFade·커스텀 정점 변형을 자동으로 정확하게 처리하는 만능 해결책이 아니다. 바람 식생에는 현재/이전 프레임의 변형을 계산하는 전용 처리가 필요하다. 단순 skinned/transform motion에 fallback을 재사용하더라도 해당 조건을 충족하는지 확인한다.[S13]

GI는 SH, Lightmap, APV를 한꺼번에 더하지 않는다. 프로젝트가 실제로 사용하는 경로를 선택하고 Mixed Lighting의 직접광 중복도 방지한다. APV가 필요한 프로젝트에서 단순 `SampleSH` 호출만으로 ‘프로브 지원 완료’라 하지 않는다.[S14]

**Decal 유의점:** URP Lit에 보이던 스킬 바닥 표시가 커스텀 셰이더에도 자동으로 보인다고 가정하지 않는다. 기존 스킬 인디케이터/DBuffer 데칼이 필수이면 통합 테스트 항목으로 승격한다. 이를 위해 전체 PBR 라이팅을 다시 가져오기보다 필요한 입력 처리만 연결하며, 미지원 상태에서는 해당 표면 이관을 보류한다.

## 10. 배칭·인스턴싱·런타임 비용

| 대상/경로 | 기본 방침 | 금지할 가정 |
|---|---|---|
| 다양한 MeshRenderer 소품 | SRP Batcher 호환 유지 | SRP Batcher가 모든 draw call을 합친다는 설명 |
| SkinnedMeshRenderer NPC | SRP Batcher 호환 및 재질/패스 최소화 | Enable Instancing만으로 NPC 군중 인스턴싱 완성 |
| 반복 식생 | 기존 Terrain Instanced Detail 또는 검증된 인스턴서 | Material checkbox만 확인하고 성공 판정 |
| 선택적 개체 색 변화 | 제한된 공유 재질 팔레트/기존 instance data | 모든 Renderer에 MPB를 무조건 배포 |
| 바람·시간·월드 스타일 | 소수의 전역값 | 잔디/NPC마다 Update에서 SetFloat/SetColor |

SRP Batcher는 렌더 상태 설정의 CPU 비용을 줄이는 경로다. 커스텀 셰이더의 재질 상수는 단일 `UnityPerMaterial` CBUFFER에 모으고, 같은 셰이더의 모든 패스에서 레이아웃을 일치시킨다. 일반 Renderer에 MaterialPropertyBlock을 적용하면 SRP Batcher 호환성에 영향을 주므로 사용을 제한한다.[S03][S04]

**단, MPB를 전면 금지하지 않는다.** 명시적 인스턴싱 경로에서 필요한 인스턴스/프로브 데이터 전달 등은 별도 계약이다. SRP Batcher와 명시적 인스턴싱을 같은 것으로 취급하지 않는다. 프로젝트 전체의 SRP Batcher를 끄는 변경은 v1에서 금지한다.[S05]

`.material` 접근으로 런타임 재질 복제를 만들지 않는다. 변환/품질용 재질은 에디터에서 생성·참조하고, 런타임은 등록된 참조를 적용한다. 특수한 개체별 피격색·디졸브는 현재 게임 시스템의 요구를 확인하고 별도 승인한다.

런타임 정상 상태 목표는 이 모듈이 만드는 **프레임당 관리 힙 할당 0 B**, 오브젝트 수에 비례하는 재질 갱신 루프 없음, 불필요한 자체 Tick 컴포넌트 없음이다. 실제 Profiler 측정으로 검증한다. 셰이더 include의 `half` 사용이 모든 GPU에서 항상 더 빠르다고 가정하지 않으며 월드 위치·시간·행렬은 필요한 정밀도를 유지한다.

## 11. LOD·품질 단계와 가시성

기존 `LODGroup`과 식생 시스템의 거리/밀도 기능을 먼저 사용한다. 셰이더에서 픽셀을 버리는 것만으로 오브젝트의 제출·정점 처리가 사라졌다고 보지 않는다. 거리에 따른 비표시는 실제 culling/LOD 시스템이 담당한다.

| 단계 | 재질 정책 | 기하·배치 정책 |
|---|---|---|
| Near | 필요한 그림자·추가 조명·알파·Normal 유지 | 원본 또는 가까운 LOD |
| Mid | Normal 제거 후보, 단순 라이팅, 중요 그림자 유지 | 기존 저해상도 LOD 및 밀도 축소 |
| Far | 주광 + 환경광, 그림자 수신/생성 축소 후보 | 더 단순한 LOD·기존 빌보드·컬링 |

고정 거리 20/50/100m를 모든 대상에 강제하지 않는다. NPC의 화면 크기, 나무 높이, 카메라 FOV와 기존 LOD 기준을 따른다. 자동 인스턴싱 Detail 경로가 개체별 프리셋 교체를 지원하지 않으면 그 경로에 없는 기능을 구현했다고 쓰지 않는다. v1에서 FoliageFar는 **그 기능을 제공하는 렌더링 경로에만** 사용한다.

CrossFade를 사용할 때는 컬러·깊이·노멀·그림자·모션벡터에서 해당 URP 버전의 규칙을 적용한다. 화면 공간과 shadow map 공간이 다르다는 점도 검증한다. LOD 전환 시 두 LOD가 동시에 렌더되는 구간의 비용을 별도로 측정한다.

품질 설정을 낮춘다는 이유로 멀리 있는 건물 내부가 항상 밝아지거나 NPC가 밤에 발광하면 실패다. 낮은 품질도 아트 방향의 최소 기준을 유지해야 한다.

## 12. 머티리얼 변환 도구 — 복제와 복구 우선

### 12.1 사용자 흐름

```text
선택 자산/프리팹/씬 범위 지정
  → Analyze (읽기 전용)
  → 보호 대상·미지원 기능·코드 의존성 보고
  → 프리셋 선택 + 복제 생성
  → 원본/변환본 A/B 미리보기
  → Validate
  → 선택 항목만 Apply
  → Manifest 저장
  → 필요하면 Rollback
```

기본 동작은 Analyze다. 창을 열거나 스캔했다는 이유만으로 원본이 변경되지 않는다. 출력은 `Generated/Materials`, `Generated/Textures` 아래로 제한한다. 프리팹은 가능하면 Variant/복제본으로 검증하고, 실제 원본 참조 교체는 사용자가 선택한 범위와 승인된 목록에만 수행한다.

### 12.2 변환 가능성

| 원본 기능 | 기본 처리 | 위험 |
|---|---|---|
| Base Texture·Color·UV transform | 설치 버전의 속성을 확인해 복사 | 색을 굽고 다시 Tint를 곱하는 이중 적용 |
| Opaque/Cutout·Cutoff·Cull | 실질 렌더 모드까지 확인해 대응 | Render Queue만 보고 Transparent를 오판 |
| 단순 Normal Map | Standard만 복사 후보 | Tangent·강도·패킹 차이 |
| lilToon 색 보정·2nd/3rd layer | 지원되는 정적 베이크 후 검토 | 애니메이션/UV/특수 Blend 재현 불가 |
| Shadow/Rim/MatCap/Specular | 근사치 또는 제외를 명시 | 원본 외형 보존을 보장할 수 없음 |
| Transparent·굴절·털·스텐실·특수 얼굴 | 자동 변환 제외 | 불투명 처리 시 파손 |
| 재질 애니메이션/게임 코드 프로퍼티 | 사용처와 새 속성 매핑 검증 | 피격·소멸·상호작용 연출 파손 |
| Terrain | 전용 Validator만 사용 | 일반 Material Converter로 처리 금지 |

lilToon 공식 도구의 베이크 기능은 일부 정적 색/레이어 통합에 활용할 수 있다. 해당 버전에 호출 가능한 안정적 API가 확인되지 않으면 reflection으로 내부 메서드를 무리하게 호출하지 말고 수동 베이크 단계로 표시한다. **베이크는 시점·조명 의존 효과나 애니메이션 기능까지 보존하는 만능 변환이 아니다.**[S01]

### 12.3 보고·복구 데이터

각 항목은 `sourceMaterialGuid`, `sourceDependencyHash`, `targetMaterialGuid`, `presetId`, `renderer/prefab path`, `materialSlot`, `sourceShaderName`, `sourceKeywords`, `warnings`, `status`, `toolVersion`을 기록한다.

같은 원본+설정으로 다시 실행하면 기존 생성본을 재사용하거나 충돌을 보고한다. `material (1)`, `(2)`가 누적되는 변환기를 만들지 않는다. 이미 사람이 수정한 생성본은 hash 차이를 경고하고 자동 덮어쓰지 않는다.

적용은 Editor Undo와 영구 매핑 기록을 함께 제공한다. Rollback은 **기록된 대상 슬롯**만 되돌리며 작업 이후 다른 사람이 변경한 참조를 덮어쓰지 않도록 현재 참조 일치 여부를 검사한다. 중간 실패 시 작업 단위로 원복하고 성공/실패를 분리해서 남긴다. TerrainData와 원본 텍스처는 어떤 경우에도 자동 삭제하지 않는다.

## 13. Shader Variant·빌드·Addressables

색, 명암 경계, 바람 세기처럼 값만 다른 항목을 모두 키워드로 만들지 않는다. 반대로 Lite에서 필요 없는 Normal 샘플링/추가 광원 루프가 매 픽셀 런타임 분기 뒤에 남지 않도록 큰 기능은 승인된 정적 조합으로 구분한다.

### 13.1 키워드 정책

| 종류 | 정책 |
|---|---|
| WorldToon 자체 기능 | 충돌 방지 접두사와 local keyword 사용을 우선. 용도에 맞는 shader stage 확인 |
| URP 플랫폼·조명 키워드 | 설치된 URP의 계약을 따름. 임의로 이름을 바꾸거나 제거하지 않음 |
| Alpha Clip | 필요한 재질/패스에만. 모든 관련 패스의 동일 실루엣 유지 |
| Additional Lights/Normal | 프리셋 Manifest에서 허용한 조합만 |
| Debug 기능 | Development 전용. Release 변형과 분리 |
| 런타임 품질 변경 | 미리 생성·참조된 재질 프리셋으로 전환. 보존되지 않은 키워드 조합 생성 금지 |

초기 목표는 **Mesh 계열의 프로젝트 자체 승인 조합 8개 안팎, Foliage 4개 안팎, Terrain4 2개 안팎**이다. 이는 URP 조명·플랫폼·패스·GI 키워드까지 포함한 총 compiled variant 수가 아니다. 기능 요구 때문에 늘어나면 이유와 실제 증가량을 `Decisions.md`에 기록한다.

URP의 스트리핑은 빌드에 포함된 여러 URP Asset/품질 설정에 영향을 받는다. ‘현재 씬에서 안 쓴다’만으로 기능 변형을 삭제하지 않는다.[S15] Addressables로 나중에 로드되는 재질, 낮은 품질의 Far 재질, Terrain BaseMap 의존 셰이더, 인스턴싱·Holes·모션벡터 변형을 별도로 확인한다.

**Always Included Shaders 전체 등록으로 핑크 문제를 덮지 않는다.** 필요한 재질/프리셋 참조와 ShaderVariantCollection 등 보존 경로를 실제 빌드 구조에 맞춰 제한적으로 사용한다. SVC 하나가 모든 Addressables/커스텀 스트리퍼 문제를 자동 해결한다고 가정하지 않는다. 필요한 조합은 실제 Player에서 렌더링한다.[S16][S19]

v1의 커스텀 스트리퍼는 먼저 **보고 전용**으로 구현한다. URP 기본 스트리핑이 정상 동작하고 테스트 범위가 확보된 뒤, 승인된 WorldToon 조합에만 제거 규칙을 적용한다. 다른 에셋/lilToon/URP 셰이더의 키워드를 전역으로 삭제하지 않는다.

## 14. 에디터 사용성과 오류 코드

`Tools/WorldToon/Audit & Convert` 창에서 스코프, 프로파일, 상태별 개수, 개별 경고, 생성 경로를 보여준다. 적어도 Analyze / Create Copies / Preview / Validate / Apply / Rollback / Export Report를 제공한다. 고급 기능을 한꺼번에 만들기 어렵다면 G1~G3에서는 단순 메뉴 도구로 시작하고 파이프라인 계약부터 고정한다.

| 코드 | 의미 | 처리 |
|---|---|---|
| `WT-PROTECT-001` | 보호된 플레이어/주요 캐릭터 | 변환·적용 차단 |
| `WT-MAT-001` | Transparent/특수 기능 미지원 | 원본 유지 |
| `WT-MAT-002` | 재질 프로퍼티 애니메이션/코드 의존 | 어댑터 검증 전 적용 차단 |
| `WT-TERRAIN-001` | Terrain Layer > 4 | Terrain4 적용 차단 |
| `WT-TERRAIN-002` | Basemap/Holes/Layer 의미 불일치 | 적용 차단 |
| `WT-FOLIAGE-001` | 바람 마스크/노멀/bounds 부적합 | 해당 프리셋 적용 차단 또는 바람 없는 원본 유지 |
| `WT-GI-001` | 실제 GI 경로 미지원 | 대상 이관 차단 |
| `WT-BATCH-001` | 기대한 배칭 경로 불일치 | 측정 경고, 관련 최적화 주장 금지 |
| `WT-BUILD-001` | 필요한 variant/hidden shader 누락 | 릴리스 적용 차단 |
| `WT-PASS-001` | 필수 패스 실루엣/데이터 불일치 | 릴리스 적용 차단 |
| `WT-CONFLICT-001` | 원본/생성본/대상 참조가 변경됨 | 자동 덮어쓰기·Rollback 차단 |
| `WT-ENV-001` | 실행/컴파일/GPU 측정 환경 없음 | `NOT_RUN`, 성공으로 표시 금지 |

Build Validator는 **WorldToon 적용 범위와 활성화된 기능**에 한정해서 검사한다. 이번 작업과 무관한 프로젝트 문제를 갑자기 전부 빌드 차단 대상으로 만들지 않는다. 승인 예외에는 대상·이유·기능 영향·검증 상태를 함께 기록한다.

## 15. 구현 단계와 종료 조건

각 단계의 산출물을 확인한 후 다음 단계로 진행한다. ‘설계만 하고 멈추기’가 아니라 구현 에이전트가 이 순서대로 실행하는 계획이다. 다만 실제 성능/외형이 실패한 대상의 강제 적용은 금지한다.

| 단계 | 구현/작업 | 완료 조건 | 실패 시 |
|---|---|---|---|
| **G0 조사** | 프로젝트·원본 재질·렌더러 조사, 기준 씬 확보 | 버전/경로/필수 기능/보호 목록 기록 | 미확인 부분을 명시하고 안전한 최소 작업만 |
| **G1 Mesh 최소본** | BaseMap·툰 명암·주광 그림자·GI·Fog, 필요한 Depth 패스 | 바위/단순 소품의 실제 프로젝트 렌더링 정상 | 라이팅·패스부터 수정, 부가 기능 추가 금지 |
| **G2 최소 A/B** | 원본/최적화 원본/WorldToon 비교 | 측정 가능하거나 측정 불가 사유와 재현 절차 기록 | 개선 없으면 범위 축소·원본 유지 후보 |
| **G3 Foliage** | Cutout·바람·양면·배치 연결·필요 MotionVectors | 패스 정합·bounds·인스턴싱 확인 | 기존 식생 유지, 신규 렌더러 재작성으로 확장 금지 |
| **G4 NPC** | 스키닝 재질 연결·슬롯별 변환·Standard 추가 광원 | 애니메이션·대화 근접·낮밤·게임 연출 회귀 통과 | 중요 슬롯/해당 NPC 원본 유지 |
| **G5 Terrain4** | Terrain/베이스맵 경로, layer validator | 1~4 레이어·타일 경계·원거리·holes 정상 | 원본 Terrain Lit 유지 |
| **G6 도구·빌드** | 변환/복구·Variant Manifest·Addressables 검증 | 재현 가능한 Player 빌드·품질/LOD/원거리 통과 | 적용 차단 및 구체적 오류 보고 |
| **G7 도입 판정** | 성능·외형 보고서, 적용 대상 목록 | 통과 대상만 승인, 나머지는 보류/원복 | 전체 일괄 교체하지 않음 |

G0~G2가 가장 먼저다. BaseMap/변환 UI를 전부 만든 뒤 처음으로 성능을 재는 순서는 피한다. Terrain은 Mesh/Foliage보다 네이티브 통합의 검증 범위가 넓으므로 뒤에서 진행한다. 사용자가 지형 외형을 우선하면 G5 순서를 당길 수 있지만 테스트를 생략하지 않는다.

### 15.1 Claude와 Codex의 협업 규칙

한 번에 한 에이전트만 작업 트리의 구현을 소유한다. 다른 에이전트는 diff와 테스트 로그를 검토한다. 모델 이름에 따라 영구적인 역할을 고정할 필요는 없다.

각 단계에서 `구현 변경 / 실행한 명령 / 실제 결과 / 미실행 항목 / 다음 단계 / 변경한 설계 결정`을 남긴다. 동일 셰이더 include와 Manifest를 두 에이전트가 동시에 수정하지 않는다. 관련 없는 파일·기존 사용자 수정은 되돌리지 않는다.

## 16. 검증 씬과 테스트 명세

테스트는 `PASS / FAIL / NOT_RUN / NOT_APPLICABLE` 네 상태를 사용한다. `NOT_APPLICABLE`에는 G0의 근거가 필요하다. 셰이더가 에디터에서 보인다는 사실만으로 Player/GPU/Addressables 검증까지 PASS 처리하지 않는다.

### 16.1 테스트 씬

별도 `WorldToonValidation` 씬에 고정 시드와 고정 카메라 지점을 둔다. 게임 씬을 대량 수정해 검증하지 않는다. 원본과 변환본을 같은 위치에 동시에 그려 성능을 비교하지 않고, 동일한 객체 집합의 활성 재질 세트만 전환한다.

| 구역 | 내용 | 주로 확인할 것 |
|---|---|---|
| A. 재질 비교 | 구·박스·바위·기존 NPC 한 명 | 기본 명암, 색, 그림자, 법선 |
| B. 마을 | 낮/밤, 실내, 횃불 Point/Spot Light | NPC·건물 추가 광원 및 GI |
| C. 식생 | 풀/덤불/나무, 카메라 낮은 각도 | alpha 겹침, 바람, 그림자, bounds |
| D. Terrain | 1/2/4 레이어 및 인접 타일, 동굴 hole | splat·basemap·holes·seam |
| E. 실패 입력 | 5 레이어, Transparent, 보호 캐릭터 | 자동 차단·보고·원본 유지 |
| F. 스트리밍 | Addressables/Additive 로드·언로드 | 재질/variant 누락, 전역 소유권 |

### 16.2 기능 테스트

| ID | 시험 | 통과 기준 |
|---|---|---|
| T01 | 보호 대상 스캔·변환 | 플레이어/보호 재질의 파일·참조 hash 불변 |
| T02 | Opaque/Cutout | 올바른 queue·ZWrite·culling 및 alpha silhouette |
| T03 | 주광 On/Off·낮밤 | 의도하지 않은 자체 발광 없음, 주광 방향 반영 |
| T04 | Main Shadow·Cascade | 그림자 위치·경계·distance fade 정상 |
| T05 | Point/Spot 및 실제 Forward 경로 | Standard에서 광원 반응 정상, Lite의 제외는 명시 |
| T06 | 실제 Forward+ 경로 | clustered light·비주광 방향광 누락 없음 |
| T07 | 실제 GI | 기존 프로브/APV/Lightmap 요구 충족, 중복 조명 없음 |
| T08 | Fog·물 깊이/카메라 Depth | 지형·NPC·식생이 정상적으로 깊이/안개에 반영 |
| T09 | SSAO/DepthNormals | 바람·알파 형상과 노멀 일치, 요구 시 AO 수신 정상 |
| T10 | TAA/STP/Motion Blur | skinned·wind history 정상, 잔상·엉뚱한 모션 없음 |
| T11 | NPC 애니메이션 | Animancer/Animator, BlendShape, 피격/소멸 재질 제어 회귀 없음 |
| T12 | 양면 식생 | 앞뒤 조명·알파 정상, 뒤집힌 검은 면/스펙 오류 없음 |
| T13 | 바람 패스 정합 | 컬러·깊이·그림자·모션 위치 일치 |
| T14 | 바람 bounds | 카메라/그림자 frustum 경계에서 조기 사라짐 없음 |
| T15 | 인스턴싱/배칭 | 실제 Frame Debugger 캡처로 의도 경로 확인 |
| T16 | Terrain 1/2/4 layer | UV/가중치/기본색 정상 |
| T17 | Terrain 5 layer | 자동 파괴 없이 적용 차단 |
| T18 | Terrain BaseMap | 원거리 밝기/색 급변 없음, 갱신 정상 |
| T19 | Terrain Holes·타일 경계 | 모든 필요한 패스에서 구멍/경계 정상 |
| T20 | Terrain Instancing | 활성/비활성 경로의 형상·노멀·Holes 정상 |
| T21 | LOD/CrossFade·Far | 허용된 프리셋, 깜빡임/그림자 중복/핑크 없음 |
| T22 | Decal/스킬 인디케이터 | 실제 필수 표시가 대상 표면에서 보임 |
| T23 | Analyze와 반복 변환 | 원본 불변, 반복 실행 시 중복 자산 생성 없음 |
| T24 | Apply/Rollback·충돌 | 슬롯별 정확한 복구, 사후 변경 덮어쓰기 차단 |
| T25 | Domain Reload·Additive Scene | 전역 상태 누수/복수 소유자 문제 없음 |
| T26 | Player 품질 전환 | 모든 사용 재질·패스·Hidden Shader가 정상 |
| T27 | Addressables·원거리 로드 | 에디터에는 없던 재질까지 Player에서 정상 |
| T28 | Release/Development | 필요한 기능은 유지, debug 전용 기능은 의도대로 분리 |

EditMode는 프로파일 범위, 키워드 매핑, 보호 규칙, 변환 idempotence, 4-layer 제한, GUID 매핑, 참조 손상 검사를 담당한다. PlayMode/그래픽 테스트는 렌더링·시간·카메라·스트리밍을 담당한다. headless `-nographics`에서 C# 테스트가 성공해도 시각적 렌더링 검증 성공으로 간주하지 않는다.

## 17. 성능 측정과 도입/중단 기준

### 17.1 비교군

| 대상 | 기준선 | 후보 |
|---|---|---|
| NPC·현재 lilToon 소품 | 현재 빌드 / 최적화된 lilToon / 외형 허용 시 lilToonLite | Mesh Lite/Standard |
| 기존 URP 소품 | 현재 사용 셰이더 | Mesh Lite/Standard |
| 식생 | 현재 실제 식생 재질·렌더링 경로 | 같은 경로의 Foliage |
| Terrain | 현재 Terrain Lit 또는 실제 기존 Terrain 셰이더 | 같은 타일·레이어·거리의 Terrain4 |

Terrain에 lilToon을 억지로 적용한 비정상 장면을 기준선으로 만들지 않는다. 원본이 Outline/MatCap을 사용하면 ‘기능을 뺀 원본’과도 비교해 **기능 제거 효과와 셰이더 구현 효과를 구분**한다.

### 17.2 비교 조건

1차 A/B는 동일 해상도·Render Scale·AA·카메라·조명·그림자·메시·밀도·애니메이션·LOD 상태를 고정한다. 기준 해상도는 프로젝트의 실제 목표를 사용하며 별도 확인이 없으면 QHD 2560×1440을 테스트 시작안으로 둔다. 목표 하드웨어를 확정하기 전 ‘몇 FPS 보장’은 하지 않는다.

측정 제안은 준비 300프레임, 측정 1,000프레임, 각 조건 3회 이상이다. 고정 시드와 재생 가능한 카메라 경로를 사용한다. VSync/프레임 제한/동적 해상도는 측정 목적에 맞춰 동일하게 고정하거나 비활성화하고 설정을 기록한다. shader warmup과 cold-load hitch는 steady-state 측정과 분리한다.

기능 축소, 그림자 생성 감소, LOD/밀도 변경을 포함하는 실제 저품질 프리셋 비교는 **2차 실사용 비교**로 별도 기록한다. 1차 A/B에서만 밀도를 낮추고 그 결과를 셰이더 성능 개선으로 보고하지 않는다.

### 17.3 지표와 도구

| 지표 | 용도 |
|---|---|
| GPU Frame ms, 대상 패스 ms | GPU 병목과 실제 개선 확인 |
| Main/Render Thread ms | 배칭·제출 비용의 변화 |
| Draw/Batches/SetPass, triangle/vertex | 패스/배치 구조 변화 확인 |
| Shadow/Depth/MotionVectors 비용 | 컬러 패스 외 비용 확인 |
| 메모리·로드/컴파일 시간·variant 수 | 유지보수/빌드 측면 비교 |
| median·p95, 반복 측정 편차 | 평균 FPS만으로 숨겨지는 지연과 노이즈 |
| 이 모듈의 GC Alloc | 정상 상태의 관리 메모리 할당 검증 |

Frame Debugger는 실제 패스·재질·인스턴싱·배칭을 확인하는 도구로 사용한다. 정밀한 GPU 타이밍은 지원되는 Unity GPU Profiler 또는 해당 타깃의 GPU 프로파일러를 이용한다. 지원하지 않는 API/장치에서 0ms나 누락값을 성공으로 처리하지 않는다.[S17][S18]

NPC는 1/10/30/100명 같은 단계, 식생은 고정 구역의 밀도 단계, Terrain은 동일 카메라 경로에서 비교한다. 이 수치는 벤치마크 입력 예시이지 실제 플레이 권장 수량이나 처리 성능 보장이 아니다. CPU-bound와 GPU-bound 결과를 분리한다.

### 17.4 도입 판정

**필수 조건:** T01~T28 중 적용되는 항목이 통과하고, 보호 자산 불변, 빌드 정상, 필수 외형/게임 기능 손실 없음.

**성능 목적의 도입:** 대상 GPU/CPU 병목이 반복 측정 오차보다 확실히 줄고, 전체 프레임의 p95나 메모리가 의미 있게 악화되지 않아야 한다. 초기 참고 목표는 대상 렌더링 비용 10% 이상 감소로 둘 수 있으나, 이는 관측 결과도 보편적인 합격 기준도 아니다. 최소 개선 ms·허용 회귀·노이즈 판정법은 기준선 하드웨어에서 G2 때 고정하고 결과를 본 뒤 유리하게 바꾸지 않는다.

**외형/유지보수 목적의 도입:** 비용 개선이 작더라도 스타일 일관성이나 작업 편의 때문에 도입할 수 있다. 이 경우 ‘경량화 성공’이 아니라 목적을 따로 기록하고 선택 적용한다.

다음 상황에서는 해당 대상의 교체를 멈춘다: 최적화된 원본과 차이가 측정 오차 이내, 원본의 얼굴/조명/데칼 기능을 재현하는 데 범위가 급증, Terrain native 기능 손상, 식생의 주요 병목이 기하/오버드로인데 셰이더 변화가 효과 없음, Player에서 필요한 변형 누락. 하나의 계열이 실패해도 통과한 다른 계열까지 되돌릴 필요는 없다.

## 18. 에이전트 산출물과 완료 보고

구현 완료 보고에는 ‘구현했습니다’ 한 문장 대신 아래 자료를 포함한다.

| 산출물 | 내용 |
|---|---|
| 코드/셰이더 | 실제 사용 중인 계열 및 공통 include, Editor 도구, 테스트 |
| 조사 기록 | 실제 Unity/URP 버전, 렌더 경로, 타깃, 필수 기능, 보호 목록 |
| 변환 기록 | 원본→생성본→적용 슬롯, 미지원·보류·복구 정보 |
| 검증 결과 | T01~T28의 상태, 실행 명령·로그·스크린샷, 미실행 사유 |
| 성능 결과 | 기준선·후보·설정·장치·median/p95·패스 및 프레임 비용 |
| 빌드 결과 | 실제 성공/실패/미실행, Addressables·품질·LOD·Basemap 확인 |
| 도입 목록 | 적용 / 보류 / 원본 유지와 각각의 이유 |
| 유지보수 | 설치 URP 의존 파일·버전, 출처 고지, 업데이트 시 재검증 지점 |

성능 보고서의 빈 값은 `NOT_MEASURED`로 둔다. 가상의 30% 개선, 임의의 60FPS, 실행하지 않은 빌드 성공을 기록하지 않는다. 문서의 의사 코드를 실제 실행 코드인 것처럼 전달하지 않는다.

## 19. v1 이후 확장 조건

기본 셰이더가 검증된 뒤 필요성이 확인된 기능만 추가한다. 후보는 제한된 Emission/피격색, 특수 장비용 Normal/하이라이트, 월드 반응 마스크, APV 기반 식생 GI, 더 세밀한 원거리 정책이다.

새 기능을 추가할 때마다 **사용 대상, 최소 품질에서 제외 가능 여부, 추가 텍스처/패스/배리언트, 기존 재질 영향, 성능 A/B, 빌드 보존**을 함께 명시한다. 모든 기능을 NPC·Terrain·Foliage 전부에 자동 전파하지 않는다. 파생 셰이더가 다시 lilToon 수준의 범용 기능 집합이 되지 않게 한다.

**최종 방향:** 주변 NPC·오브젝트는 같은 경량 Mesh Toon을 쓰고, 지형·식생은 전용 경로로 분리한다. 성능에 유리한 대상을 먼저 이관하며, 원본을 유지하는 결정도 정상적인 완료 결과로 인정한다.

## 20. 확인한 공식 자료와 적용 범위

확인일: **2026-11-16**. 공식 자료의 기술 사실과 이 문서의 프로젝트 설계 결정은 구분한다. 아래 URL의 Unity 문서는 리다이렉트될 수 있으며 구현의 최종 기준은 프로젝트에 설치된 패키지다. 공개 소스를 참조/복제하는 경우 포함된 라이선스·고지문을 보존하고 실제 사용 범위를 `ThirdPartyNotices.md`에 기록한다.

| ID | 공식 자료 | 이 문서에서 사용하는 근거 |
|---|---|---|
| S01 | [lilToon 최적화](https://lilxyzw.github.io/lilToon/ja_JP/other/optimization.html) | 베이크, Lite 변환, 원본 최적화 비교 |
| S02 | [lilToon Shader Settings](https://lilxyzw.github.io/lilToon/ja_JP/other/settings.html) | 빌드 최적화와 Editor 비교 주의 |
| S03 | [URP SRP Batcher 호환성](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher-Materials.html) | UnityPerMaterial·MPB 제약 |
| S04 | [URP SRP Batcher](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/srpbatcher/srpbatcher) | CPU 렌더 상태 설정 최적화의 의미 |
| S05 | [GPU Instancing 활성화](https://docs.unity3d.com/kr/6000.0/Manual/gpu-instancing-enable.html) | SkinnedMeshRenderer 제약, 명시적 instancing API와 구분 |
| S06 | [Terrain Grass and other details](https://docs.unity3d.com/6000.0/Documentation/Manual/terrain-Grass.html) | Detail Mesh 재질 사용, 프로브/라이트맵 제약 |
| S07 | [Forward/Forward+ 추가 광원](https://docs.unity3d.com/cn/6000.0/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html) | 광원 루프·InputData·방향광 경로 |
| S08 | [Custom URP Shader의 Shadows](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html) | 주광 그림자 처리 |
| S09 | [URP ShaderLab Pass Tags](https://docs.unity3d.com/kr/6000.0/Manual/urp/urp-shaders/urp-shaderlab-pass-tags.html) | ForwardOnly·DepthNormals·Meta 등 |
| S10 | [Terrain Layers](https://docs.unity.com/en-us/engine/6000.0/manual/creating-environments/script-terrain/class-terrain-layer) | 레이어와 패스 비용의 관계 |
| S11 | [Unity Graphics — TerrainLit.shader](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLit.shader) | Terrain 셰이더 의존 구조. master를 버전 고정 소스로 쓰지 않음 |
| S12 | [Unity Graphics — TerrainLitPasses.hlsl](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl) | Terrain 처리 구조. 실제 구현은 설치 버전 기준 |
| S13 | [Custom Shader Motion Vectors](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/motion-vectors-custom-shader.html) | custom deformation·alpha·LOD 및 fallback 제한 |
| S14 | [Custom URP Shader의 간접광](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/use-built-in-shader-methods-indirect-lighting.html) | APV 등 실제 GI 경로 확인 |
| S15 | [URP Shader Stripping](https://docs.unity3d.com/kr/6000.0/Manual/urp/shader-stripping.html) | 여러 URP Asset과 기능별 variant 관리 |
| S16 | [Graphics Settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-GraphicsSettings.html) | Always Included·Instancing variant·preload 설정 |
| S17 | [GPU Usage Profiler](https://docs.unity3d.com/kr/6000.0/Manual/ProfilerGPU.html) | GPU 측정과 플랫폼/API 지원 제약 |
| S18 | [Frame Debugger](https://docs.unity3d.com/6000.0/Documentation/Manual/FrameDebugger.html) | 실제 draw event·패스 검증 |
| S19 | [Shader Variant Stripping](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variant-stripping.html) | 필요한 variant 보존 및 stripping 주의 |

---

**전달 순서:** `START_HERE.md` → 이 설계서 → `IMPLEMENTATION_REQUEST.md`. 첫 구현 목표는 **G0 조사와 G1의 작은 Mesh Toon**, 전체 월드 교체가 아니다.
