# WorldToon — 설계와 달라진 결정

- 상위: [WorldToon 작업 상태](../worldtoon.md) · [통합 계약](../../../Assets/docs/design/world-art/environment/WORLDTOON_EXISTING_INTEGRATION.md) · [전달 설계서](../../전달용/WorldToon_Handoff/WorldToon_Design.md)

전달 설계서와 다르게 구현한 지점과 근거만 적는다. 같은 후보를 다시 제안하지 않도록 보류 사유도 여기에 둔다.

## 구조

| 설계서 | 결정 | 근거 |
|---|---|---|
| `WorldToonGlobals`가 `_WT_*` 전역 스타일을 매 변경 시 설정 | **전역 상태를 두지 않는다.** `WorldToonStyleProfileSO` 값을 변환 도구가 생성 재질의 `UnityPerMaterial` 상수로 기록한다 | 런타임 컴포넌트·소유자 경쟁·Additive 씬 중복·도메인 리로드 복구 문제가 사라진다(T25 해당 없음). SRP Batcher 호환을 유지하고 Player에 별도 초기화 경로가 필요 없다. 대가: 프로파일을 바꾸면 변환 도구를 다시 실행해 재질에 재기록해야 한다(반복 실행은 재사용·충돌 보호가 보장됨) |
| `WorldToonQualityProfile` SO | 만들지 않음 | 거리·밀도는 기존 LODGroup·식생 시스템이 담당한다(설계 §11). 품질 차이는 프리셋 재질(MeshFar·FoliageFar)로만 표현한다 |
| `WorldToonVariantManifest` SO | 코드 상수 `WorldToonVariantManifest.cs` | 프리셋→키워드 표는 사람이 조정할 밸런스 값이 아니라 셰이더 계약이다. 셰이더와 같은 변경 단위로 리뷰되게 코드에 둔다 |
| `WorldToonMaterialRegistry` | `docs/work-status/worldtoon/ConversionReport.json`(원본 GlobalObjectId↔생성 GUID↔적용 슬롯) | 런타임 참조 교체 기능이 없으므로 레지스트리 에셋이 필요 없다 |
| `Assets/Project/Rendering/WorldToon/` 별도 asmdef | 셰이더 `Assets/06.Shaders/WorldToon/`, 데이터 `UPlayGround.Data`(`Data/Rendering`), 도구 `Tool/Editor/WorldToon`(Assembly-CSharp-Editor) | 통합 계약 §2. 런타임 C#이 없어 런타임 asmdef가 필요 없다 |
| `WorldToonShaderGUI` 커스텀 인스펙터 | 만들지 않음. `[Toggle(KEYWORD)]`·`[ToggleOff]` 속성 드로어 사용 | 키워드는 드로어가, Cutout 렌더 큐는 변환 도구가 설정한다 |
| `WorldToonTerrainBaseMapGen.shader` | URP `Terrain/Lit (Basemap Gen)`을 Dependency로 재사용 | URP 생성기는 레이어 알베도만 합성하고 라이팅을 굽지 않아 설계 요구(§8.1)를 그대로 만족한다. 중복 소스를 만들지 않는다 |
| Terrain 입력 | URP `TerrainLitInput.hlsl`을 `WorldToonTerrainInput.hlsl`로 복제해 `UnityPerMaterial` 끝에 스타일 상수만 추가 | Terrain 셰이더도 단일 `UnityPerMaterial`을 유지해야 SRP Batcher 호환이다. 정점·스플랫·Holes·인스턴싱·보조 패스는 URP `TerrainLitPasses.hlsl`을 include로 그대로 쓴다. [출처 고지](ThirdPartyNotices.md) |

## 셰이더 동작

| 항목 | 결정 |
|---|---|
| 명암 | `band = smoothstep(N·L)`, 주광 기여 = `light.color × distanceAttenuation × lerp(Shade, Lit, band × shadowAttenuation)`. 그림자 가시성은 한 번만 결합(이중 감쇠 없음). 주광이 꺼지면 직접광 0 |
| GI | 설치 URP의 `SAMPLE_GI` 단일 경로(현재 프로젝트는 Legacy Light Probes·Lightmap). `MixRealtimeAndBakedGI`로 Subtractive 중복 방지 |
| SSAO | `CreateAmbientOcclusionFactor` + `GetMainLight(inputData, shadowMask, aoFactor)`로 직접·간접 AO 수신 |
| 추가 광원 | `_WT_ADDITIONAL_LIGHTS`(Standard)에서만. URP 17.3 공식 순서(Forward+ 방향광 선행 루프 → `LIGHT_LOOP_BEGIN`)를 따른 단순 확산광 |
| 데칼 | DBuffer `ApplyDecalToSurfaceData`를 라이팅 전에 적용 |
| 패스 | Mesh: ForwardLit·ShadowCaster·DepthOnly·DepthNormals·Meta·MotionVectors. 보조 패스는 URP 소스 재사용(같은 `_BaseMap/_BaseColor/_Cutoff` 계약). Stencil 블록 없음(카메라 가림 페이드 호환) |
| Normal Map·Emission | v1 미구현. 분석기가 Emission·금속 재질을 NeedsReview로 표시 |
| Foliage 바람 | `EnvironmentShaderState`의 `_LW_Wind/_LW_PreviousWind`를 읽기만 한다. 전역 바람이 0인 씬을 위해 재질 `_WindIdle`(기본 0.15) 흔들림을 둔다. 모션벡터는 이전 바람·이전 시간(`_Time.y - unity_DeltaTime.x`)으로 계산 |
| Foliage 접촉 변위 | 넣지 않음. 접촉(`FoliageInteraction`)이 필요한 식생은 기존 셰이더 유지 |
| Terrain | 4레이어 한 패스 혼합 후 툰 라이팅 1회. `AddPassShader` Dependency를 선언하지 않는다(5레이어 이상은 Validator가 차단) |
| Terrain Basemap | `UsePass`로 Terrain4 패스를 빌리면 import 순서에 따라 패스가 빠지는 현상을 확인해 패스를 직접 선언했다 |

## 도구

| 항목 | 결정 |
|---|---|
| 재질 키워드 기록 | 저장된 재질에 속성을 쓰면 키워드가 다시 동기화되어 지워지는 현상을 테스트로 확인했다. 모든 속성을 쓴 뒤 `material.enabledKeywords`(LocalKeyword)로 마지막에 확정한다 |
| 생성본 이름 | `{원본이름}__{원본 GlobalObjectId 해시 8자}.mat`. FBX 내장 재질은 모델마다 별도 원본이므로 같은 이름 `Stone_Field`도 3개로 분리된다 |
| 충돌 판정 | 생성본 내용 해시(셰이더·키워드·큐·직렬화 속성, 참조는 GUID+localId)가 기록과 다르면 덮어쓰지 않는다 |
| 저장 범위 | `AssetDatabase.SaveAssets()` 전체 저장 금지, 자기 에셋만 `SaveAssetIfDirty`(예방 규칙). 작업 기간 중 `Mobile_RPAsset`이 URP 자동 업그레이드(에셋 버전 12→13)로 재직렬화되어 원복했다. 수정 시각(11-18 22:23)이 도구의 전체 저장보다 앞서 저장 주체는 미확정이다 |
| 적용 차단 | `ApplyToSlot`은 렌더러 문맥까지 분석해 Ready만 적용한다. NeedsReview(스키닝·애니메이터 하위 WT-MAT-002 등)는 `allowNeedsReview`를 명시해야 적용된다 |
| 툴 런처 | 등록하지 않음(AI 검증·검증 씬 생성 도구). `UPlaygroundToolCatalog` ID 또는 MCP로 실행 |

## 적용 보류·원본 유지

| 계열 | 결정 | 이유 | 다시 볼 조건 |
|---|---|---|---|
| NPC·몬스터 | 적용 후보 없음 | 슬롯 대부분이 lilToon(보호) — Actor/NPC 254, Monster 1432, ScenarioResidents 755. ScenarioResidents의 URP/Lit 50슬롯도 보호 캐릭터 소속. 스키닝 슬롯은 피격·디졸브 재질 어댑터 검증 전 NeedsReview(WT-MAT-002) | 사용자가 비보호 NPC를 지정하고 재질 속성 어댑터를 승인할 때 |
| 실제 식생 | 적용 보류 | 기존 식생 메시가 Read/Write 꺼짐 → 바람 마스크 확인 불가(WT-FOLIAGE-001). 기존 Graph 셰이더는 접촉 변위·반응형 경로를 가짐 | 복제 메시에 마스크를 만들거나 Read/Write 허용을 사용자가 승인할 때 |
| 주요 월드 지형 | 적용 불가 | 7~27 레이어, 레이어 Normal/Mask 사용(WT-TERRAIN-001/002) | v1 범위 밖 |
| WorldBuilder 생성 타일(4레이어 이하 80개) | 적용 보류 | 통합 계약상 읽기 전용이며 다른 작업이 현재 편집 중 | 사용자가 복제본 적용을 승인할 때 |
| 바위·소품(URP/Lit) | **후보** | 검증 씬에서 Lite/Standard 렌더 정상, 정적 지표 개선 | 사용자 외형 확인 + Player A/B 측정 후 |
