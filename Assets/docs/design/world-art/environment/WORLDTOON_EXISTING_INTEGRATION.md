# WorldToon — 기존 시스템과 프로젝트 규칙 통합 계약

- 전달 원문: [WorldToon 설계서](../../../../../docs/전달용/WorldToon_Handoff/WorldToon_Design.md) · [구현 요청문](../../../../../docs/전달용/WorldToon_Handoff/IMPLEMENTATION_REQUEST.md) · [검증 템플릿](../../../../../docs/전달용/WorldToon_Handoff/ACCEPTANCE_CHECKLIST.md) · [전달 안내](../../../../../docs/전달용/WorldToon_Handoff/START_HERE.md)
- 구현·검증·미수행: [작업 상태](../../../../../docs/work-status/worldtoon.md)
- 분야 위키: [월드와 아트](../../../../../docs/wiki/world-art.md)

전달 원문은 이 저장소를 조사하지 않고 쓴 범용 설계(v1.0, 2026-11-16)다. 원문의 목표·단계(G0~G7)·테스트(T01~T28)·금지 사항은 그대로 정본으로 쓰고, 이 문서는 **이 프로젝트와 충돌하거나 겹치는 지점의 결정**만 기록한다. 원문 내용을 복제하지 않는다. 원문과 이 문서가 다르면 이 문서가 우선한다.

## 1. 이미 있는 것과의 관계

| 원문의 대상 | 이 프로젝트의 현재 상태(기존 문서 기재, G0에서 재확인) | 결정 |
|---|---|---|
| 플레이어·주요 캐릭터 lilToon 유지 | 플레이어·ScenarioResidents NPC가 lilToon 계열 | 원문 그대로 보호 목록. G0에서 실제 목록을 확정해 상태 문서에 기록 |
| 일반 NPC·몬스터 → Mesh | 비인간형 몬스터 28+2개를 이미 lilToon으로 전환([전환 상태](../../../../../docs/work-status/non-humanoid-toon-migration.md)). 사용자 플레이·미술 확인 대기 | **이미 전환된 lilToon 재질을 되돌리거나 덮어쓰지 않는다.** WorldToon Mesh의 NPC·몬스터 적용은 G4에서 A/B로 이득이 확인된 슬롯만 후보로 올린다. 전환 작업의 사용자 확인 결과와 충돌하면 사용자 확인을 우선한다 |
| Foliage | `Assets/06.Shaders/Foliage/`(UPG_Grass·Grass/Plant/Leaves/IdyllicVegetation Graph, `UPG_FoliageInteraction.hlsl`), `Environment/Vegetation/`의 나무 셰이더와 Depth·Shadow·DepthNormals·MotionVectors 패스 | 기존 식생 셰이더가 정답 선례다. G3은 이들의 패스 정합·접촉 변위 계약을 읽고 따라야 하며, 접촉 변위(`FoliageInteraction`)와 경쟁하는 바람 입력을 새로 만들지 않는다. 대체가 아니라 **비교 후 후보** |
| Terrain4 | WorldBuilder가 만든 B·C 지형, 전용 TerrainLayer(매끄러움0 수정 이력), `CreekGround.shader` | G5는 WorldBuilder 지형 데이터를 **읽기 전용**으로 쓴다. 지형·TerrainData·레이어를 수정하는 적용은 사용자 승인 후 복제본에만 한다 |
| 물·폭포·안개·파티클 제외 | `Water/`·`LakeWater`·`ToonWaterfall`·`DaySky` 등 | 원문대로 범위 밖. 수면 합성 패스 순서를 건드리지 않는다 |
| 젖음·탄화 등 반응 표현 | `Environment/ToonReactive.shader`, `EnvironmentSurface.hlsl`, `VegetationReaction.hlsl`([반응형 환경](../../../../../docs/work-status/reactive-environment.md)) | **반응형 환경 재질을 쓰는 대상은 WorldToon 교체 대상에서 제외한다.** WorldToon Mesh에 젖음·탄화·소거를 넣지 않는다(원문의 "기본 제외"와 일치). Fog·젖음의 작성자는 한 곳만 둔다 — [전달 설계 사이의 계약](../../../../../docs/전달용/README.md#세-설계-사이의-계약) |
| 기존 변환 도구 | `LilToonHierarchyConverter`는 sharedMaterials 원본을 직접 수정하고, `P09ToonLookProfileSO`는 같은 셰이더만 허용 | G6 변환 도구의 기반으로 쓰지 않는다. 복제 재질을 프로젝트 소유 폴더(전례: `Assets/11.Material/NonHumanoidToon`)에 만드는 방식을 따른다 |

## 2. 경로·산출물 위치

원문의 `Docs/WorldToon/…` 경로는 이 프로젝트에 없다. 아래로 대체한다.

| 원문 산출물 | 이 프로젝트의 위치 |
|---|---|
| `ProjectAudit.md`·`Decisions.md`·`ValidationReport.md`·`PerformanceReport.md` | `docs/work-status/worldtoon/` 하위. 요약·판정만 [작업 상태](../../../../../docs/work-status/worldtoon.md)와 위키에 둔다 |
| `ConversionReport.json`·Variant Manifest | 같은 폴더의 기계용 파일. 큰 로그·캡처는 위키에 복사하지 않는다 |
| 셰이더·include | `Assets/06.Shaders/WorldToon/` (기존 `06.Shaders` 하위 도메인 구조와 동일) |
| 프리셋·품질 SO | `Assets/10.Datas/`의 SO. 밸런스·연출 수치는 데이터 SO가 단일 소스이며 셰이더에 이름·액터 분기를 넣지 않는다 |
| 복제 재질 | `Assets/11.Material/WorldToon/` (원본 재질·구매 원본은 수정하지 않음) |
| Editor 도구 C# | 기존 asmdef·네임스페이스 규칙([아키텍처](../../../../../.claude/rules/architecture.md), [코드 컨벤션](../../../../../.claude/rules/code-conventions.md)). 반복 사용하는 사람용 도구만 툴 런처에 등록 |
| `output/` | 임시 작업 폴더다. 보존할 결론은 `docs/work-status/worldtoon/`로 옮긴다 |

## 3. 원문 지침과 프로젝트 규칙의 충돌 해소

| 원문 | 프로젝트 규칙 | 결정 |
|---|---|---|
| G0~G7을 한 번에 순서대로 진행 | 단계마다 부산물 정리·위키 갱신·링크 확인 후 다음 단계 | 단계 종료 때마다 [작업 상태](../../../../../docs/work-status/worldtoon.md)와 위키를 갱신한다. 한 요청에서 G7까지 밀어붙이지 않고, 사용자 지시 범위(기본은 G0~G1)에서 멈춘다 |
| T27(Addressables·원거리)·T28(Release·Development)·G6 "재현 가능한 Player 빌드" | 별도 사용자 지시 없이 Player·Addressables 빌드 금지 | 해당 항목은 `NOT_RUN`이 아니라 **`사용자 지시 대기`로 표기**하고 G6·G7의 도입 승인을 막는다. 앞선 [식생 빌드 검정 사례](../../../../../docs/work-status/vegetation-build-black.md)처럼 빌드 변형 누락(SSAO·DBuffer 등)은 Editor 화면만으로 확인할 수 없다 |
| `PASS/FAIL/NOT_RUN/NOT_APPLICABLE` | `사용자 테스트 대기`, 자동 검사와 사람 확인의 구분 | 두 체계를 병기한다. 미술 품질(외형 승인)은 자동 PASS로 대체하지 않고 사용자 확인 칸을 따로 둔다 |
| 구현 에이전트 1 + 검토 에이전트 | [역할 분담 하네스](../../../../../Tools/Harness/ROLES.md) | 메인 세션(리드)이 구현을 소유한다. 독립 검증은 QA 검토자, 캡처·모델 작업은 아티스트에 위임하되 **같은 셰이더 include·Manifest는 동시에 수정하지 않는다** |
| 임의 GPU/그래픽 테스트 기록 | 열린 Editor는 하나, MCP·Test Runner로만 조작 | Unity MCP로 측정한다. 두 번째 Editor 금지. 측정 불가 항목은 `NOT_MEASURED`로 둔다 |
| C# 변경 | [Hot Reload 운영](../../../guide/HOT_RELOAD_USAGE_GUIDE.md) | Editor 도구 C#은 지원 변경이면 Hot Reload 우선. **`.shader`·`.hlsl`은 Hot Reload 대상이 아니며** AssetDatabase import와 GPU 실행으로 검사한다([전례](../../../../../docs/work-status/custom-shader-overhead-review.md)) |
| 커밋·승인 | 요청 없으면 커밋하지 않음, 새 승인 단계 추가 금지 | 원문에 없지만 도입 판정(G7)의 "승인"은 사용자 확인이며 별도 자동 승인 절차를 만들지 않는다 |
| 사용자 데이터 | 기존 미커밋 변경·구매 원본·`.meta` 보존 | 작업 전후 diff 확인, `Assets/10.Datas/`·`Assets/03.Prefabs/` 자동 재직렬화는 원복 |

## 4. 시작 조건과 첫 범위

1. G0에서 파일로 확정한 값(2026-11-19): Unity 6000.3.21f1, URP 17.3.0, Direct3D11·Direct3D12, Linear, PC_RPAsset(Forward+·Depth Priming Auto·SSAO DepthNormals·DBuffer 데칼 레이어·Legacy 프로브). 상세와 미확인 항목은 [ProjectAudit](../../../../../docs/work-status/worldtoon/ProjectAudit.md).
2. 첫 범위는 원문대로 **G0 조사 + G1 Mesh 최소본**이다. 대상은 지형 장식 바위 같은 단순 정적 메시 한 종류로 시작하며 NPC·식생·Terrain 교체는 하지 않는다.
3. G0에서 반드시 대조할 프로젝트 고유 항목: ① 위 §1의 기존 셰이더·재질 목록과 보호 목록 ② SSAO·Decal(DBuffer)·카메라 가림 디더(`CameraOccluderDitherMask`) 사용 여부 ③ 반응형 환경 재질 사용처 ④ 빌드 변형 스트리핑 설정(식생 검정 사례의 렌더 설정 지문).
4. G2 비교 기준은 **현재 실제 사용 재질(최적화된 lilToon·URP/Lit·기존 커스텀)**이다. 개선이 없으면 교체하지 않는 원문의 판정 기준을 유지한다.

## 5. 제외·보류 사유 기록

적용에서 제외하거나 보류한 대상은 이유와 함께 상태 문서에 남기고 같은 후보를 다음 작업에서 반복 제안하지 않는다. 원문 §19의 v1 이후 확장은 이 문서가 승인하지 않는다. 현재 보류 목록은 [결정 기록](../../../../../docs/work-status/worldtoon/Decisions.md)의 「적용 보류·원본 유지」다.

## 6. 구현 결과로 확정된 계약(2026-11-19)

- **전역 스타일 없음:** `WorldToonStyleProfileSO`(`Assets/10.Datas/Environment/WorldToon/`) 값은 변환 도구가 생성 재질 상수로 기록한다. 원문 `WorldToonGlobals`는 만들지 않는다. 프로파일 변경 후에는 변환 도구를 다시 실행한다.
- **반응형 식생(2026-11-20):** Foliage는 기존 식생 파생 셰이더와 같은 반응 계약(`_EnvironmentReactionSupported`·`_EnvState0/1`·`_FuelCoverage`·`_ENVIRONMENT_REACTION_BUFFER`·지형 셀 마스크)을 `VegetationReaction.hlsl` 그대로 include해 지원한다. 접촉 변위는 `UPG_FoliageInteraction.hlsl`을 그대로 쓴다(새 입력 없음). 이로써 §1의 "Foliage는 비교 후 후보" 중 반응형 식생도 후보가 됐다. Reactive Toon(외곽선·균열·역할)은 대체하지 않는다.
- **바람 입력:** Foliage는 `EnvironmentShaderState`의 `_LW_Wind/_LW_PreviousWind`만 읽는다.
- **저장 범위:** WorldToon 도구는 자기 에셋만 `SaveAssetIfDirty`로 저장한다. 전체 `SaveAssets`는 자동 업그레이드된 RP 에셋 같은 무관한 에셋까지 저장할 수 있어 쓰지 않는다.
- **보호 강제 범위:** 도구는 lilToon 계열 셰이더(WT-PROTECT-001), 반응형 환경 재질·반응형 오브젝트 하위 렌더러(WT-PROTECT-002, 판정은 런타임 `ReactiveEnvironmentSurface.SupportsReaction`과 `ReactiveEnvironmentObject` 소유 여부), 스키닝·애니메이터 슬롯(WT-MAT-002)을 막는다. WorldToon에는 반응 상태(`_EnvState0/_EnvState1/_FuelCoverage`) 계약이 없어, 반응형 오브젝트에 쓰면 런타임 검증이 표면을 거부하고 젖음·그을림·연소 표현이 사라지기 때문이다. 전역 비 젖음 `_LW_Wetness`는 지면 셰이더(CreekGround·GroundOverlay)만 읽으므로 바위·소품(URP/Lit 원본)은 교체 전후 모두 젖음 표현이 없다. 보호 캐릭터 프리팹의 비lilToon 정적 슬롯은 경로로 막지 않으므로 적용 범위를 고를 때 [ProjectAudit §6](../../../../../docs/work-status/worldtoon/ProjectAudit.md)의 목록을 제외한다.
- 그 밖의 설계 변경은 [결정 기록](../../../../../docs/work-status/worldtoon/Decisions.md)을 따른다.
