# 지형 디테일 풀 미렌더 원인 규명·수정 작업 요청서

> 버전 1.3 / 작성일 2026-11-07 / 상태: 풀 미렌더 수정·필수 검증 완료, 씬 재로드 정리 완료 (14장)
> 대상: 별도 에이전트. 이 문서만 읽고 작업할 수 있도록 맥락·확인된 사실·미확인 항목·절차·완료 조건을 모두 담았다.
> 상위 문서: [호수 마을 월드 비주얼 품질 개선 설계](LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md) (6.1.1, 8.2가 이 문제의 근거)
> 이 문서의 작성은 원인 규명·수정·검증의 완료를 뜻하지 않는다.

## 1. 한 줄 요약

`LakeVillageWorld` 씬의 지형(`SculptedTerrain`)에는 풀 디테일 데이터가 있는데, **지형 디테일 인스턴싱 드로우가 렌더 프레임에 아예 제출되지 않는다.** 왜 제출되지 않는지 찾아 고친다. 풀의 밀도·색·모양 개선은 이 작업의 범위가 아니다.

## 2. 왜 중요한가

호수 마을이 레퍼런스 게임보다 밋밋해 보이는 가장 큰 원인이 "지면에 풀이 없음"이다. 지금은 풀을 더 심어도 그려지지 않을 가능성이 높아서, 이 문제를 풀지 않으면 비주얼 개선의 1단계(지면)를 시작할 수 없다.

## 3. 확인된 사실 (재검증 불필요, 근거 포함)

아래는 2026-11-07에 `LakeVillageWorld`를 연 에디터(Edit Mode와 Play Mode)에서 읽기 전용으로 확인한 값이다.

### 3.1 지형과 디테일 데이터

| 항목 | 값 |
|---|---|
| 지형 오브젝트 | `AuthoredEnvironment/SculptedTerrain`, 위치 `(0, 0, -700)`, 크기 `2400 × 400 × 2800` |
| 지형 머티리얼 | `Universal Render Pipeline/Terrain/Lit` |
| 지형 데이터 에셋 | 씬 파일 guid `78ef78390269c52469b8f93fc02abbd2` (바이너리라 파일로는 못 읽음. 에디터 코드로 `Terrain.activeTerrains[0].terrainData` 접근) |
| 디테일 설정 | `detailObjectDistance 80`, `detailObjectDensity 1`, `detailResolution 1024`, `drawInstanced true`, `drawTreesAndFoliage true` |
| 지형 나무 | 0그루 (나무는 별도 GameObject) |
| 디테일 프로토타입 | **1종.** `SM_Farm_SmallGrass_03_Detail`(메시 모드, `usePrototypeMesh true`, `renderMode VertexLit`, `useInstancing true`, 높이/폭 0.7~1.2, `noiseSpread 0.2`, `holeEdgePadding 0`, `density 1`) |
| 프로토타입 에셋 | `Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Grass/Grass_02/SM_Farm_SmallGrass_03_Detail.prefab` |
| 프로토타입 메시 | `SM_Farm_SmallGrass_03_LOD0`, 정점 101, 크기 약 `1.08 × 0.54 × 1.08` |
| 프로토타입 재질 | `MI_FarmGrass_Medium_01`, 셰이더 `UPlayGround/Foliage/Grass`(`Assets/06.Shaders/Foliage/UPG_Grass.shader`, Amplify), `enableInstancing True`, `renderQueue 2450`, 키워드 `_USEDITHERING_ON`, `_USEVERTEXCOLOR_ON`, `_MaskClip 0.7` |
| 셰이더 인스턴싱 | `UPG_Grass.shader`에 `#pragma multi_compile_instancing`이 있음(여러 패스). `procedural:` 옵션은 없음 |
| 디테일 레이어 데이터 | 레이어 0 합계 342,156. 1024×1024 셀 중 10.9%만 값이 있고 셀당 최대 3개. 셀 크기 약 2.34 × 2.73m |
| 최고 밀도 지점 | 월드 `(1134, 63.5, 273)`. 이 주변 16×16 셀에서 233/256 셀에 값이 있고 합계 699 |
| 스폰 `(1091, 57, 418)` | 주변 20×20 셀에 디테일 0개 (이 지점은 풀 데이터 자체가 없음. 별개 문제) |

### 3.2 렌더 설정

- 렌더 파이프라인 에셋: `Assets/Settings/PC_RPAsset.asset`, 렌더러 `PC_Renderer`(피처: SSAO, Decal). 품질 레벨 0 `PC`.
- Unity `6000.3.21f1`, URP, Render Graph 사용(이벤트 이름에 `ExecuteRenderGraph`).
- Frame Debugger 이벤트 이름에 `QuadTreeBatchNode.Render`가 보인다. GPU Resident Drawer 사용 여부는 **미확인**(후보 1).

### 3.3 관측 결과

| 관측 | 결과 | 근거 |
|---|---|---|
| 풀 디테일이 화면에 보이는가 | **안 보임.** 최고 밀도 지점에서 지상 1.7m·5m 시점, Scene View, Play Mode 모두 | `Assets/Screenshots/before_02_densest_grass.png`, `before_04_grass_closeup.png`, `before_06_grass_sceneview.png`, `play_01_grass_closeup.png` |
| 같은 프리팹을 일반 오브젝트로 놓으면 | **정상적으로 그려짐.** 재질·셰이더 문제가 아님 | `play_02_grass_prefab_test.png` (Play Mode에서 임시 배치, 종료 시 소멸) |
| Frame Debugger (카메라를 최고 밀도 지점으로 옮긴 프레임, 473이벤트) | 지형 패치 27개는 인스턴싱으로 정상 제출(이벤트당 인스턴스 6~88). `UPlayGround/Foliage/Grass` 드로우는 4개뿐이고 모두 **인스턴스 수 0**(SRP Batcher 일반 오브젝트). **지형 디테일 인스턴싱 드로우 없음** | 이벤트 228~402 전수 조사 |
| 콘솔 | 지형·풀 관련 오류/경고 없음 (무관한 `[ToolCatalog]` 경고 3건만 있었음) | `read_console` |

**결론:** 데이터는 있고, 프리팹은 그려지는데, 지형 디테일 경로에서만 제출되지 않는다.

## 4. 아직 모르는 것

**왜 제출되지 않는가.** 아래는 가설이며 어느 것도 검증하지 않았다. 검증 비용이 낮은 순서로 배열했다.

| # | 가설 | 확인 방법 |
|---|---|---|
| H1 | **GPU Resident Drawer·Render Graph와의 상호작용.** 렌더러/RP 에셋의 GPU Resident Drawer 설정이 지형 디테일 인스턴싱을 막거나 우회시킨다 | `PC_RPAsset.asset`의 `m_GPUResidentDrawerMode`, `m_GPUResidentDrawerEnableOcclusionCullingInCameras` 등을 읽고, 런타임에 해당 모드를 끄고 비교 |
| H2 | **프로토타입 설정 문제.** `useInstancing`/`renderMode`/프리팹의 LOD 구조(LOD0 메시, `LODGroup`)/렌더러 레이어 때문에 디테일이 걸러진다 | 런타임 복제 TerrainData에서 프로토타입 값을 바꿔 비교(5.2) |
| H3 | **셰이더·재질의 지형 디테일 비호환.** `UPlayGround/Foliage/Grass`가 지형 디테일 인스턴싱 경로에서 요구하는 조건을 만족하지 않는다 | 프로토타입 프리팹의 재질을 `Universal Render Pipeline/Lit`로 바꿔 렌더되는지 비교 |
| H4 | **디테일 레이어 데이터 해석 문제.** 레이어 인덱스·셀 방향이 프로토타입과 어긋나 실제로는 해당 셀에 심겨 있지 않다(데이터 합계는 크지만 렌더 쪽 패치 구성이 비어 있음) | 런타임에 `SetDetailLayer`로 가까운 셀에 값을 써서(복제본) 렌더 확인 |
| H5 | **엔진/패키지 버그 또는 제약.** 6000.3.21f1 + URP 조합의 알려진 문제 | Unity 이슈 트래커·릴리스 노트 조사 (웹 검색 사용 가능) |
| H6 | **다른 시스템이 런타임에 비활성화.** 스크립트가 `drawTreesAndFoliage`·밀도·거리를 조정한다 | `Terrain` 관련 설정을 쓰는 코드 검색(`detailObjectDistance`, `detailObjectDensity`, `drawTreesAndFoliage` 등). 초기 확인에서 Play Mode 값은 `True`였으나 다른 값은 쓰기 지점을 아직 추적하지 않았다 |

## 5. 작업 절차

### 5.1 원칙 (반드시 지킨다)

1. **원본 `TerrainData` 에셋과 씬을 수정하지 않는다.** 실험은 Play Mode에서 `terrain.terrainData = UnityEngine.Object.Instantiate(terrain.terrainData)`로 **런타임 복제본**을 만든 뒤 그 복제본에만 적용한다. Play Mode를 종료하면 사라진다.
2. 작업 시작 전과 종료 후 `manage_scene get_active`의 `isDirty`가 `false`인지 확인한다. 임시 오브젝트는 Play Mode 안에서만 만든다.
3. 실험은 **변수 하나씩** 바꾼다. 여러 개를 동시에 바꿔서 원인을 흐리지 않는다.
4. 모든 판정은 **Frame Debugger**로 한다. 화면에서 풀이 보이는지는 보조 증거다(풀이 작고 지면 색과 비슷해서 눈으로는 판단이 불확실하다).

### 5.2 단계

**단계 0 — 재현 준비 (공통)**

1. `LakeVillageWorld`가 로드된 상태에서 Play Mode 진입(`manage_editor play`). 진입 직후 `execute_code`가 한두 번 실패할 수 있으니 재시도한다.
2. 카메라를 최고 밀도 지점으로 옮긴다: 위치 `(1134, 65.2, 268)`, 주시점 `(1134, 63.5, 276)`. (`Camera.main`은 씬의 정적 카메라이므로 Play Mode에서 위치를 바꿔도 된다. 종료하면 복원된다.)
3. `manage_editor pause` → `manage_profiler frame_debugger_enable`. Frame Debugger는 **일시정지 상태에서만** 켜진다.
4. 기준 측정: 6장의 "측정 방법"으로 풀 디테일 인스턴싱 드로우 유무를 기록한다(현재는 없음).

**단계 1 — 설정·코드 조사 (비용 최저, 먼저)**

- H1: `PC_RPAsset.asset`과 `PC_Renderer.asset`에서 GPU Resident Drawer 관련 값을 Grep으로 읽는다.
- H6: 코드에서 `drawTreesAndFoliage`, `detailObjectDistance`, `detailObjectDensity`, `SetDetailLayer`, `detailPrototypes`를 검색한다. 쓰는 곳이 있으면 호출 시점과 값을 정리한다.
- H5: Unity 6000.3 + URP의 "terrain detail mesh instancing not rendering / GPU Resident Drawer terrain details" 등으로 웹 검색해 알려진 문제를 정리한다. 출처 URL을 보고서에 남긴다.

**단계 2 — 런타임 복제 실험 (변수 하나씩)**

각 실험 뒤 6장의 방법으로 측정하고, 결과를 표로 남긴다. 실험마다 Play Mode를 다시 시작해 상태를 초기화한다.

| 실험 | 변경 | 가설 |
|---|---|---|
| E1 | GPU Resident Drawer를 런타임에 끈다(가능한 API·설정이 있을 때). 불가능하면 이유를 기록 | H1 |
| E2 | 복제 TerrainData의 프로토타입 `useInstancing`을 `false`로 | H2 |
| E3 | 프로토타입 `renderMode`와 `usePrototypeMesh` 조합을 바꿔 본다(예: 텍스처 모드로 기본 디테일 텍스처 1종 추가) | H2 |
| E4 | 프로토타입 프리팹을 단순 큐브(`Universal Render Pipeline/Lit`)로 교체한 프로토타입 1종을 **추가** | H2/H3 구분 |
| E5 | 기존 프로토타입의 재질만 `Universal Render Pipeline/Lit`로 바꾼 복제 재질 사용 | H3 |
| E6 | 최고 밀도 지점 근처 셀에 `SetDetailLayer`로 값을 크게 쓰고 렌더 확인 | H4 |

해석 기준:
- E4(단순 큐브)가 그려지면 → 지형 디테일 경로는 살아 있고 문제는 **기존 프로토타입 메시/프리팹/재질/셰이더 쪽**이다(E5와 E2·E3으로 좁힌다).
- E4도 안 그려지면 → 지형 디테일 경로 자체가 막혀 있다(H1, H4, H5, H6).
- E6에서만 그려지면 → 기존 데이터 해석 문제(H4)다.

**단계 3 — 원인 보고 (수정 전)**

수정하기 전에 7장 형식으로 **원인 보고**를 먼저 남긴다. 아래에 해당하면 수정을 적용하지 말고 사용자에게 선택지를 제시한다.

- 수정이 `TerrainData`, `PC_RPAsset`, 공용 셰이더·재질(다른 씬이 쓰는 것) 중 하나를 바꿔야 한다.
- 엔진 버그라서 우회책이 필요하다.

**단계 4 — 수정 적용 (승인 후 또는 범위가 작을 때)**

적용 전에 다음을 한다.
1. 바꿀 에셋의 `git status`로 미커밋 변경이 없는지 확인한다. 있으면 사용자에게 알리고 멈춘다.
2. 최소 변경만 한다(원인과 직접 관련된 필드·설정 하나). 밀도·색·모양 개선은 하지 않는다.
3. 변경 전·후 값을 보고서에 남긴다.
4. 커밋은 하지 않는다.

## 6. 측정 방법 (Frame Debugger)

`manage_profiler`의 `frame_debugger_get_events`는 이벤트 **이름**만 주고 셰이더·인스턴스 수는 주지 않는다. 이름만으로는 지형 디테일을 구분할 수 없으므로 `execute_code`의 리플렉션으로 이벤트 데이터를 읽는다. 이전 조사에서 확인한 사실은 다음과 같다.

- 타입: `UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility`, `FrameDebuggerEventData` (어셈블리 `UnityEditor.CoreModule`). 6000.3에서는 `UnityEditorInternal.FrameDebuggerUtility`가 아니다.
- `GetFrameEventInfoName(int)`, `GetFrameEventObject(int)`는 데이터 없이 바로 동작한다.
- `GetFrameEventData(int index, FrameDebuggerEventData data)`는 **`limit`을 `index + 1`로 설정하고 에디터를 다시 그린 다음 프레임에서만** `true`를 돌려준다. 한 번의 호출로 전체 이벤트를 읽을 수 없다.
- 필드: `m_OriginalShaderName`, `m_PassName`, `m_InstanceCount`, `m_MeshInstanceID`, `m_DrawCallCount`, `m_FrameEventIndex`.
- 전수 스윕 방법: `EditorApplication.update`에 콜백을 등록해 매 틱 `limit`을 1씩 올리며 데이터를 읽고, 결과를 `SessionState.SetString`에 저장한 뒤 별도 `execute_code` 호출로 읽는다. 불투명 구간(`DrawOpaqueObjects`, 이전 프레임 기준 이벤트 228~402)만 훑어도 충분했다. 175개에 `execute_code` 폴링 약 10회가 걸렸다.
- 판정: `m_OriginalShaderName`이 `UPlayGround/Foliage/Grass`이면서 `m_InstanceCount > 0`인 이벤트가 있으면 **지형 디테일이 제출된 것**이다. 현재 기준은 그런 이벤트가 0개다.
- 디테일 프로토타입을 단순 큐브(E4)로 바꾼 경우에는 해당 큐브 재질의 셰이더 이름과 `m_InstanceCount > 0`로 판정한다.
- 측정이 끝나면 `frame_debugger_disable`을 호출한다.

## 7. 보고 형식 (필수)

작업 결과는 이 문서 하단에 "9. 작업 기록"을 추가하거나, 상위 설계 문서의 8.2 항목 1번을 갱신하는 방식으로 남긴다. 아래를 포함한다.

1. **원인:** 한 문장. 확정/추정을 구분한다.
2. **증거:** 실험 번호별 측정 결과 표(바꾼 것 / 지형 디테일 인스턴싱 드로우 유무 / 인스턴스 수).
3. **수정 내용:** 변경한 에셋·필드·전후 값. 수정하지 않았다면 이유와 선택지.
4. **검증 구분:** 수행한 것, 수행하지 못한 것을 나눠서 적는다. 아래를 각각 명시한다.
   - Frame Debugger 측정 수행 여부
   - 스크린샷 비교 수행 여부(수정 후)
   - Play Mode 수행 여부
   - Windows Player 빌드는 이 작업에서 필요한지와 수행 여부(셰이더·렌더 에셋을 바꿨다면 `TEST_VALIDATION_GUIDE.md` 기준 확인)
5. **남은 위험:** 수정이 다른 씬에 영향을 줄 가능성, 성능 영향.

"완료"는 6장 판정 기준을 만족한 경우에만 쓴다. 미검증이면 미검증이라고 적는다.

## 8. 완료 조건

| 조건 | 판정 |
|---|---|
| 수정 후 같은 지점·같은 시점에서 지형 디테일 풀 인스턴싱 드로우가 Frame Debugger에 존재(`m_InstanceCount > 0`) | 필수 |
| 수정 후 스크린샷에서 풀이 눈에 보임 (`before_04_grass_closeup.png`과 같은 시점) | 필수 |
| 원본 씬이 수정되지 않음(`isDirty false`), 임시 오브젝트·임시 에셋이 남지 않음 | 필수 |
| 수정이 에셋을 바꿨다면 변경 목록(`git status`)과 전후 값이 보고서에 있음 | 필수 |
| 프레임 시간 영향 측정 | 권장 (측정 환경 명시) |

## 9. 범위 밖 (하지 않는다)

- 풀 밀도 확대, 프로토타입 종류 추가, 꽃, 색 변주, 스폰 주변 풀 배치. 이 작업이 끝난 뒤 [상위 설계 문서](LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md) 5.1에서 다룬다.
- 라이팅·색·포스트 프로세싱·에셋 스타일 통일.
- `LakeVillageWorldPolishTool`을 포함한 에디터 도구 변경. 이 작업의 수정은 필요한 최소 에셋 설정에 한정한다.
- 툴 런처(`UPlaygroundToolsLauncher`) 등록. 조사용 코드는 `execute_code`로만 실행한다.

## 10. 도구 사용 메모 (이전 조사에서 겪은 문제)

- **MCP 연결이 간헐적으로 끊긴다.** `Connection closed before reading expected bytes`, `No Unity Editor instances found`가 나오면 1~2회 재시도하고, 계속 실패하면 상황을 보고하고 멈춘다. 같은 호출을 반복하지 않는다. 씬이 바뀌어 있으면 `manage_scene get_active`로 확인하고 `LakeVillageWorld`가 아니면 사용자에게 알린다.
- `execute_code`의 컴파일러는 기본적으로 `codedom`(C# 6)이다. 실수 리터럴은 `f` 접미사가 필요하다(`new Vector3(1134f, 65.2f, 268f)`). `Object`는 `object`와 모호하므로 `UnityEngine.Object`로 적는다(프로젝트에 `UPlayGround.Object` 네임스페이스가 있어 일반 코드에서도 `Object.Destroy` 등이 `CS0234`가 난다).
- `execute_code`는 Play Mode 진입 직후 `t=0`이거나 타임아웃이 날 수 있다. 몇 초 뒤 재시도한다.
- 위치 지정 스크린샷: `manage_camera action=screenshot view_position=[x,y,z] view_target=[x,y,z] include_image=true`. 파일은 `Assets/Screenshots/`에 저장된다. 수정 후 비교용 이름은 `after_*.png`로 한다.
- 프로젝트 규칙(`CLAUDE.md`): 한국어로 문서·보고. 파일 읽기·검색은 Read/Grep/Glob 도구를 쓰고 셸 `cat`/`grep`을 쓰지 않는다. 테스트·검증 보고는 수행/실패/미수행을 구분한다.

## 11. 관련 파일

| 파일 | 용도 |
|---|---|
| `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity` | 대상 씬 |
| `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/PC_Renderer.asset` | 렌더 설정 (H1) |
| `Assets/06.Shaders/Foliage/UPG_Grass.shader` | 풀 셰이더 (H3) |
| `Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Grass/Grass_02/SM_Farm_SmallGrass_03_Detail.prefab` | 디테일 프로토타입 (H2) |
| `Assets/Screenshots/before_*.png`, `play_01_grass_closeup.png`, `play_02_grass_prefab_test.png` | 개선 전 기준 캡처 |
| `Assets/docs/design/LAKE_VILLAGE_VISUAL_QUALITY_DESIGN.md` | 상위 설계(6.1.1, 8.1, 8.2) |

## 12. 작업 기록 — 2026-11-07

### 12.1 원인과 판정

**확정: 셀당 개수로 작성한 0~3의 디테일 맵이 `CoverageMode`로 해석되어 풀 인스턴스가 생성되지 않는다.** 원본은 `Assets/10.Datas/World/OpenWorld/Terrain.asset`이다. 같은 맵을 보존한 런타임 복제본에서 `InstanceCountMode`로 바꾸면 기존 풀 메시·재질·셰이더로 인스턴싱 드로우가 제출된다. 원인 규명과 런타임 수정안 검증을 마쳤으며, 영구 수정은 아직 적용하지 않았다.

Unity 공식 문서의 [DetailScatterMode](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/detailscattermode)는 `InstanceCountMode`를 셀당 개수, `CoverageMode`를 면적의 피복량으로 구분한다. [ComputeDetailCoverage](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/terraindata/computedetailcoverage)는 후자의 제곱미터당 인스턴스 수를 설명한다. 이 프로젝트 프로토타입의 계산 피복량은 약 0.9760124였다.

생성 경로의 근거는 `OpenWorldSliceAuthoring.Surface.cs`의 `PopulateExpansionDetails`다. `groundCoverDensity`를 셀 값에 직접 쓰고, `Tools/OpenWorld/city-layout.json`의 해당 값은 3이다. `CopyNorthDetails`·`CopyGeneratedGround`도 맵을 복사하지만 산포 모드를 명시적으로 전달하지 않는다. 에셋이 이 상태가 된 정확한 과거 실행 순서는 추정이며, 현재 산포 모드와 맵의 해석 불일치는 실험으로 확인했다. 제작 도구 수정은 본 요청의 범위 밖이므로 적용하지 않았다.

### 12.2 측정과 실험

환경은 Unity 6000.3.21f1, Editor Play Mode, 기존 PC 렌더 설정이다. GPU Resident Drawer와 카메라 GPU 오클루전 컬링은 둘 다 켜져 있으며 실험 내내 유지했다. 런타임 디테일 설정을 비활성화하는 쓰기 코드는 `Assets/02.Scripts` 검색에서 발견하지 못했다.

`MainCamera`는 직접 이동해도 원래 위치로 복귀했다. 일시정지 후 직접 이동한 경우에도 Frame Debugger 측정 중 복귀가 관측됐다. 따라서 기존 카메라를 런타임 복제한 `GrassProbeCamera`로 관측하고 원래 카메라의 렌더만 껐다. 위치 `(1134,65.2,268)`, 주시점 `(1134,63.5,276)`, 회전 약 `(12,0,0)`을 확인했다. 마지막 전후 캡처는 **같은 Play Mode·카메라·FOV 60**에서 TerrainData만 교체해 촬영했다. 카메라 복귀의 내부 원인은 이번 범위에서 확정하지 않았다.

| 측정/실험 | 변경 | CPU 생성 인스턴스: 패치 (30,22) | Frame Debugger 풀 인스턴싱 |
|---|---|---:|---|
| 기준 B0 | 원본 `CoverageMode`, 맵 0~3 | 0 | 없음. 1,285개 이벤트 전수 스윕, 읽기 실패 0. 풀 Forward 이벤트 3건은 모두 인스턴스 0 |
| E6 보조 검사 | 씬에 연결하지 않은 런타임 복제본의 해당 16×16셀을 255로 설정 | 1,531 | 미측정. CPU 검사만 수행했으므로 렌더 통과로 판정하지 않음 |
| E7 보조 검사 | 복제본을 `InstanceCountMode`로 변경하고 기존 맵 복원 | 546 | 이 단계는 CPU 검사만 수행. 기존 패치의 셀 합계 546과 일치 |
| E7 렌더 검증 | 새 Play Mode에서 같은 변경을 별도 복제 TerrainData에 적용 | 위 보조 검사 참조 | **Forward 4건: 454, 92, 378, 420개, 합계 1,344개. DepthNormals 4건: 511, 35, 378, 420개, 합계 1,344개** |

E7의 풀 이벤트는 셰이더 `UPlayGround/Foliage/Grass`, 메시 ID `337734`로 기존 `SM_Farm_SmallGrass_03_LOD0`와 일치했다. Forward 이벤트는 1177~1180, DepthNormals는 241~244다. 패스별 합계는 같은 인스턴스의 중복 렌더이므로 합쳐서 개체 수로 세지 않는다. E7은 `GetFrameEvents()`의 `m_Type == InstancedMesh`인 75개 이벤트를 전부 스윕했다. E1~E5는 미수행이다. 기존 프로토타입·셰이더·GPU Resident Drawer를 유지한 E7에서 복구돼, 추가 변경 실험 없이 H4로 좁혔다. 엔진 버그로 확정할 근거는 없으며 엔진 교체·우회책을 적용하지 않는다.

모든 1,024×1,024셀의 변경 전후 값을 비교해 **전체 일치**, 합계 **342,156 유지**를 확인했다. 프로토타입 수치, 밀도 맵, 색, 모양은 E7에서 바꾸지 않았다. E6의 255 쓰기는 폐기한 별도 복제본에만 수행했다.

증거 경로는 프로젝트 루트 기준이다.

- `Logs/TerrainGrass/baseline-controlled-camera.txt`: B0 전체 스윕.
- `Logs/TerrainGrass/count-mode-instanced-events.txt`: E7 전체 인스턴싱 이벤트.
- `Logs/TerrainGrass/before-same-camera.png`, `after-same-camera.png`: 동일 카메라 전후 캡처. 직접 열어 풀의 출현을 확인했다.
- `Logs/TerrainGrass/comparison-camera.txt`: 최종 비교 카메라 위치·회전·FOV.
- `Logs/TerrainGrass/source-hashes-before.json`, `source-hash-verification.json`: 씬·TerrainData·PC_RPAsset·풀 셰이더 파일의 SHA-256 불변 확인.
- `Logs/TerrainGrass/baseline-uncontrolled-camera.txt`, `baseline-paused-camera-reset.txt`: 카메라 위치 복귀로 수정안 비교 기준에서 제외한 초기 측정.

### 12.3 영구 수정 제안 — 승인 필요

**권장안:** `Assets/10.Datas/World/OpenWorld/Terrain.asset`의 `detailScatterMode`를 `CoverageMode → InstanceCountMode`로 변경한다. 원본 맵·프로토타입·GUID는 보존한다. 씬·RP 에셋·공용 셰이더·재질 변경은 필요 없다. 대상 TerrainData의 작업 전 `git status`는 깨끗했고, 이번 조사에서도 파일은 바뀌지 않았다. 씬·프리팹·1MB 미만 텍스트 데이터 에셋의 GUID 검색에서는 `LakeVillageWorld`의 Terrain/TerrainCollider 참조만 발견했다. 동적 로딩까지 포함한 전체 사용처 보증은 아니다.

**주의:** 실제 실험에서 `SetDetailScatterMode` 호출은 기존 디테일 맵을 비웠다. API 한 번만 호출해서 저장하면 안 된다. 승인 후에는 모든 레이어 맵을 먼저 보관하고, 모드 변경 뒤 정확히 복원·전 셀 비교하고, 실패 시 원본을 복구하는 절차로 적용해야 한다. 런타임 E7에서도 이 순서로 복원했다.

대안은 `CoverageMode`를 유지하며 피복량에 맞게 맵을 다시 작성하는 것이다. 기존 셀 값을 바꾸고 밀도 재설계를 요구하므로 이번 범위에는 권장하지 않는다. 생성 도구의 산포 모드 보존이 빠져 있어 재생성 시 같은 문제가 재발할 위험은 별도로 남는다.

본 요청서 5.2 단계 3의 **“TerrainData … 중 하나를 바꿔야 한다”면 적용하지 말고 선택지를 제시**하는 조건에 해당하므로 원본 에셋 변경은 사용자 승인 대기다.

### 12.4 검증 상태와 남은 항목

| 항목 | 상태 |
|---|---|
| Frame Debugger | B0/E7 수행. 런타임 수정안에서 필수 인스턴싱 조건 통과 |
| 스크린샷 | 동일 카메라 전후 비교 수행. 런타임 수정안에서 풀 노출 확인 |
| Editor Play Mode | 기준·새 세션 수정안 수행 후 종료 |
| 원본 파일 보존 | 씬·TerrainData·PC_RPAsset·풀 셰이더 SHA-256 동일. 원본 재질·프리팹 수정 없음 |
| 임시 오브젝트 | 종료 후 `GrassProbeCamera` 없음, Terrain은 원본 에셋·`CoverageMode`로 복원. 임시 에셋 파일 생성 없음 |
| 씬 dirty | 시작 `false`, 종료 **`true`**. 디스크 씬 파일은 불변. 저장하지 않았고 dirty 원인은 미확정 |
| 영구 적용·재진입 | 승인 대기. 아직 미수행 |
| 성능·다른 시점 | 미측정. 복구 후 증가하는 풀 렌더 비용은 별도 확인 필요 |
| Windows Player | 별도 빌드 요청이 없고 검증 지침의 검증용 빌드 금지에 따라 미수행. Player 결과를 보증하지 않음 |
| 컴파일·자동 테스트 | 영구 코드 변경 없음. EditMode 테스트·별도 전체 컴파일 미수행 |

종료 후 dirty 상태를 정리하려고 디스크의 씬을 다시 여는 작업을 요청했으나 **자동 승인 검토가 거절**했다. 이유는 저장되지 않은 씬 변경을 폐기할 권한이 없어 기존 작업을 잃을 수 있다는 것이다. 우회하지 않았으며, 현재 에디터의 dirty 씬은 그대로 보존했다. 씬 재로드도 사용자 확인이 필요하다.

이번 Play Mode 콘솔에는 미등록 서비스(`IActorObjectService`, `IAssetService`, `IQuestFlowService`, `IGlobalFlagService`), DissolveMaterial 로드 실패, FlowGraph 진입점 무장 실패가 관측됐다. 풀 수정과의 인과관계는 확인하지 않았고 범위 밖이므로 수정하지 않았다. 콘솔 무오류로 보고하지 않는다.

영구 적용 후 사용자 확인 항목: `G1` 같은 위치에서 풀이 보이고 예외가 새로 발생하지 않는지, `G2` 80m 안팎 이동·카메라 회전에서 풀 컬링과 프레임 저하가 수용 가능한지, `G3` 씬 재진입 후에도 풀이 유지되는지. 결과는 `ID / 통과·실패·미수행 / 실제 결과 / 캡처·로그`로 남긴다. 스폰 주변은 원래 디테일 0개이므로 G1의 실패 기준으로 사용하지 않는다.

## 15. 독립 검증 — 2026-11-07 (요청자 측 에이전트)

13~14장의 영구 적용 이후, 작업 에이전트의 로그를 신뢰하지 않고 에디터에서 직접 확인한 결과다.

| 항목 | 결과 |
|---|---|
| `Terrain.asset` 현재 모드 | **`InstanceCountMode`** (에디터에서 직접 읽음, GUID `78ef7839...` 동일) |
| 디테일 맵 보존 | 레이어 0 합계 342,156, 값이 있는 셀 114,052. 수정 전 값과 동일 |
| 같은 지점 렌더(새 Play Mode) | **풀이 보인다.** 수정 전 `play_01_grass_closeup.png`는 풀 0개, 수정 후 `Assets/Screenshots/verify_after_fix_01_closeup_1.png`는 지면 곳곳에 풀 뭉치가 보임. 같은 위치·시점·FOV |
| 씬 | `isDirty false`, Play Mode 종료 후 루트 12개, 임시 프로브 카메라 없음, `MainCamera` 활성 (14장의 재로드 결과와 일치) |
| 씬·RP 에셋·풀 셰이더 | `git status`에 변경 없음 |

**Frame Debugger 재측정은 완료하지 못했다.** 첫 측정은 `MainCamera`가 플레이어 쪽 `(1118, 54, 386)`으로 복귀한 프레임이라 풀 지점을 보지 않았고(풀 인스턴싱 0, **무효**), 프로브 카메라를 쓴 두 번째 측정은 범위가 867개 이벤트로 넓어 중단했으며 중간에 에디터 연결이 한 번 끊겼다. 따라서 이 검증은 **렌더 단계 인스턴스 수를 직접 재현하지 못했다.** 13.2의 합계 1,344개는 작업 에이전트의 측정값이고, 요청자 측은 같은 지점의 스크린샷과 에셋 상태로만 교차 확인했다. 무효 측정을 "수정이 실패했다"는 증거로 쓰지 않는다.

**교훈(측정법):** Play Mode에서는 게임 카메라 시스템이 `MainCamera` 위치를 되돌린다. Frame Debugger 측정은 반드시 복제 카메라로 하고, 스윕 전에 이벤트 이름에 복제 카메라가 있는지(`...: VerifyProbeCamera`) 확인한다. 불투명 패스 전체가 아니라 `m_Type == InstancedMesh` 이벤트만 스윕하면 시간이 크게 줄어든다(13.2 방식).

**확인 사항 (13장과 일치):** `Assets/10.Datas/World` 전체가 `.gitignore` 814행에 걸려 `Terrain.asset`은 git이 추적하지 않는다(`git check-ignore`, `git ls-files`로 재확인). 되돌리려면 13.1의 `output/TerrainGrassFix/Terrain.before.bin` 사본뿐이다.

**미검증:** G2(80m 안팎 이동·회전 시 컬링과 프레임 영향), 다른 시점·성능, 씬 재진입 후 Play Mode 렌더, 레퍼런스 수준의 풀 밀도(풀은 보이지만 작고 지면과 색이 비슷하다).

**전체 완료 아님:** 원인 확인·런타임 수정안 검증은 끝났지만 영구 적용, 씬 dirty 정리, 영구 적용 후 재검증이 남았다. 커밋하지 않았다.

## 13. 영구 적용 기록 — 사용자 후속 지시 반영

12장은 영구 적용 전 조사 이력이다. 사용자의 후속 지시 “해결해”에 따라 검증된 TerrainData 변경을 적용했다.

### 13.1 변경과 데이터 보존

- 대상: `Assets/10.Datas/World/OpenWorld/Terrain.asset`.
- 변경: `detailScatterMode`, **`CoverageMode → InstanceCountMode`**.
- 기존 레이어 맵을 먼저 보관하고, 모드 변경 뒤 복원했다. 전체 1,024×1,024셀을 대조해 전부 일치했고 합계 **342,156**을 유지했다. 레이어 1개, 기존 프로토타입·재질·셰이더·거리·색·크기 유지.
- GUID `78ef78390269c52469b8f93fc02abbd2` 유지. `SaveAssetIfDirty`로 대상만 저장했다. 실패 시 기존 모드·맵을 복원하는 처리도 포함했으며 롤백은 실행되지 않았다.
- 저장 후 강제 재임포트하고 전체 셀을 다시 대조했다. 모드와 맵 저장을 확인했고 에셋 `isDirty=false`였다.
- **Git 관리 주의:** `.gitignore` 814행의 `/Assets/10.Datas/World` 규칙으로 대상 에셋은 추적되지 않는다. 작업 전 `git status`가 깨끗했던 것은 Git 추적 파일이라는 뜻이 아니었다. `git ls-files`와 `git check-ignore -v`로 확인했다. 따라서 Git diff에는 에셋 변경이 표시되지 않으며, 다른 환경에 전달하려면 이 에셋도 별도 전달해야 한다. ignore 규칙은 변경하지 않았다.
- Git으로 원본을 복원할 수 없어 수정 전 바이너리를 `output/TerrainGrassFix/Terrain.before.bin`에 보존했다. 크기 92,288,444바이트(약 88MiB). 수정 전 원본 복구를 위한 사본이며 Unity에 임포트되는 에셋이 아니다.

원본 SHA-256은 `2A85397D7AA53282BC7612A62AA04EF2723D7358642CDF8FD7264F0C15D89DAD`, 수정 후는 `4B5AF593BC1B3B5ECDE7DCFB91482800689244AA7E707CD2F48C909DBAADE832`다.

### 13.2 영구 적용 후 검증

새 Editor Play Mode에서 **원본 에셋을 직접 참조**하는 Terrain으로 확인했다. `AssetDatabase.GetAssetPath`는 위 원본 경로였고 `EditorUtility.IsPersistent`는 `true`였다. 이번 검증에는 TerrainData 복제본을 연결하지 않았다. 관측용 카메라만 런타임에 복제했다.

| 항목 | 결과 |
|---|---|
| 저장·재임포트 | 산포 모드 유지, 전 셀 일치, 합계 342,156 유지 |
| CPU 패치 (30,22) | 546개 생성 |
| Frame Debugger | 인스턴싱 이벤트 75개 전수 스윕. 풀 Forward 이벤트 1176~1179의 인스턴스 **454, 92, 378, 420**, 합계 **1,344** |
| DepthNormals | 이벤트 240~243, 인스턴스 **511, 35, 378, 420**, 합계 **1,344** |
| 셰이더·메시 | 기존 `UPlayGround/Foliage/Grass`, `SM_Farm_SmallGrass_03_LOD0`(메시 ID 337734) |
| 수정 후 화면 | `(1134,65.2,268)`, 회전 약 `(12,0,0)`, FOV 60. 캡처를 직접 열어 풀 노출 확인 |
| 종료 상태 | Play Mode·Frame Debugger 종료. 임시 `GrassProbeCamera` 없음. Terrain은 원본 에셋과 `InstanceCountMode` 유지 |
| 디스크 씬 | 수정 전후 SHA-256 동일: `88477CB7F468A65F785F51D4C38F2BC285A167989AF2B6284C77DA4D3BCF4144`. 저장하지 않음 |
| 에디터 씬 dirty | **`true` 유지.** 이 후속 작업 시작 전부터 남아 있던 상태이며, 저장하거나 변경을 폐기하지 않음 |

증거: `Logs/TerrainGrass/permanent-apply.txt`, `permanent-reimport.txt`, `permanent-instanced-events.txt`, `permanent-file-hashes.json`, `after-permanent-fix.png`.

**풀 미렌더의 영구 수정과 Editor 렌더 검증은 통과했다.** 요청서의 전체 완료 조건 중 씬 `isDirty=false` 정리는 남아 있다. 자동 승인 검토가 씬 재로드를 다시 거절했으며, 사유는 “문제 해결 요청은 저장되지 않은 씬 변경의 손실까지 승인하지 않는다”였다. 변경 폐기를 우회하지 않고 씬 상태를 보존했다. 미저장 씬 변경을 버리는 명시적 승인이 있어야 재로드할 수 있다.

Windows Player 빌드·성능 측정·실제 이동 중 컬링 품질·씬 재로드 후 검증은 미수행이다. 에셋 재임포트와 새 Play Mode 검증을 씬 재로드 검증으로 대체해 보고하지 않는다. 12.4의 기존 콘솔 경고·예외와 제작 도구 재생성 시 산포 모드 유실 위험도 별도로 남는다. 제작 도구와 공용 렌더 설정은 이번에 수정하지 않았다. 커밋하지 않았다.

## 14. 씬 재로드·최종 정리

미저장 씬 변경을 폐기하고 다시 열어도 되는지 묻는 질문에 사용자가 “ㄱㄱ”로 승인했다. 이에 `LakeVillageWorld`를 저장하지 않고 디스크에서 다시 열었다.

- 재로드 직후와 별도 `get_active` 조회 모두 **`isDirty=false`**.
- 원본 TerrainData 참조와 **`InstanceCountMode` 유지**, 디테일 맵 합계 **342,156**, 패치 (30,22) 생성 인스턴스 **546** 확인.
- 임시 관측 카메라 없음. Play Mode 종료 상태 유지.
- 씬과 수정된 TerrainData 파일은 13장의 저장 후 SHA-256과 동일. 씬 재로드 과정의 파일 변경 없음.
- 증거: `Logs/TerrainGrass/scene-reload-verification.txt`, `scene-reload-file-hashes.json`.

**풀 미렌더 수정과 요청서의 필수 완료 조건을 충족했다.** 영구 적용 후 Frame Debugger·스크린샷·Play Mode 검증은 13.2, 최종 씬 정리는 본 장의 결과다. 재로드 뒤에는 데이터 연결·생성 결과만 확인했으며 Play Mode·Frame Debugger를 다시 실행하지 않았다. 성능 측정·실제 이동 중 컬링 품질·Windows Player 빌드는 미수행이다. Git 제외 에셋의 별도 전달 필요, 제작 도구 재생성 시 산포 모드 유실 위험, 범위 밖 기존 콘솔 문제는 유지된다. 커밋하지 않았다.
