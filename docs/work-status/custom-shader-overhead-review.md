# 커스텀 셰이더 오버헤드 조사

- 조사일: 2026-11-13. 사용자 요청은 불필요한 오버헤드 조사이며 코드·재질·씬 수정은 하지 않았다.
- 진입점: [월드와 아트 위키](../wiki/world-art.md). 기존 [물·비 비가시 비용 개선](water-rain-visibility-cost.md)은 현재 소스와 대조했으며 이번 조사 성과로 합산하지 않는다.
- 범위: `Assets/06.Shaders`의 45개 소스 인벤토리(Shader 22, HLSL 12, Graph 10, Compute 1), 물·하늘·환경 반응·식생 접촉·환경 생물·화면 효과의 주요 경로. 모든 Graph의 생성 코드와 구매 셰이더 전체를 감사한 결과는 아니다.
- 현재 판정: 사용자 후속 요청으로 아래 5개 셰이더 계산 생략을 반영했다. Unity GPU 전후 비교 70건과 식생 최종 재비교 18건 통과. 아래 초기 조사 순위는 GPU 병목 순위나 절감량 실측이 아니다. 최종 변경·검증 범위는 다음 절을 따른다.

## Unity MCP 개선 후속

### 반영

| 파일 | 최종 변경 | 보존한 계약 |
|---|---|---|
| [LakeWater](../../Assets/06.Shaders/LakeWater.shader) | 관리 수면의 일반 패스에서 정점을 같은 위치로 퇴화시켜 파도·좌표 변환·래스터화 생략 | Feature 활성과 관리 상태를 동시에 검사. 하나라도 꺼지면 기존 Forward로 복귀. Surface Info는 별도 패스라 유지 |
| [DaySkyCloudVolume](../../Assets/06.Shaders/DaySkyCloudVolume.hlsl) | 최종 horizonFade가 0이면 광선 적분 생략. Formation activation 0이면 fine/micro 샘플 2개 생략 | 기존 층운 blanket·coverage 감쇠·빈 공간 거리 반환·비/야간 표현 유지. 표본 수와 SO 설정 변경 없음 |
| [UPG_FoliageInteraction](../../Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl) | 액터 없음·뿌리 마스크 0·눕힘 강도 0이면 거리 반복과 변위 계산 생략 | 유효 접촉의 반경·강도·높이 감쇠와 Graph 공용 함수 유지. 미초기화 경고를 피하도록 단일 반환 사용 |
| [EnvironmentSurface](../../Assets/06.Shaders/Environment/EnvironmentSurface.hlsl) | 반응 마스크 미사용이면 샘플 생략. 평상시/젖음만 있는 표면은 탄화·균열·발광 패턴 생략 | `_UseReactionMask`의 0/0.5/1 보간, 셀 마스크 상태, 잎 역할·소거·조명 유지 |
| [WaterSceneResolve](../../Assets/06.Shaders/Water/WaterSceneResolve.shader) | 수중에서 덮어쓸 환경 반사 샘플을 조건문 밖에서 미리 계산하지 않도록 변경 | 기존 수중 방사휘도·수면 반사·광학 구간·포말 계산 유지 |

관리 수면의 일반 패스 **드로우 제출 자체는 남는다.** 렌더러/재질의 전역 패스를 끄지 않아 카메라별 복귀를 보존했다. 물 전체 화면 광학 탐색·RT 형식·Opaque Snapshot과 고밀도 시각 메시도 유지했다. 이 구조를 줄이는 추가 변경은 실측 없이 확대하지 않았다.

### 근거와 검사

- 동일 열린 Editor `UPlayground@fddd0702`, LakeVillageWorld에서 수행했다. 첫 상태 조회는 Play true / Pause false였으며 Play 전환 중 표시도 있었다. 이전 조사 당시의 일시정지 상태를 현재 상태로 승계하지 않았다. 씬의 `isDirty=false` 조회를 확보했다. Play 종료·재시작·정규 씬 저장은 하지 않았다.
- 변경은 Shader/HLSL 5개뿐이다. C# 변경이 없어 메서드 본문 Hot Reload 대상이 아니며, Unity AssetDatabase의 셰이더 import와 실제 GPU 실행으로 검사했다.
- [직접 GPU 비교 스크립트](../../output/shader-overhead-20261113/gpu-self-check.csx)와 [1차 결과](../../output/shader-overhead-20261113/gpu-self-check-result.txt): 환경 24조건(역할 2 × 마스크 보간 3 × 상태 4), 일반 수면 4조건(관리/Feature 조합), 식생 18조건(액터 0/1/8 × 마스크 0/0.5/1 × 강도 0/0.4), 구름 24조건(Formation 2 × coverage 4 × 시선 높이 3), 합계 70건 PASS.
- 환경은 실제 Forward 출력을 변경 전 셰이더와 비교했다. 수면 검사는 실제 정점 함수에 단색 검사 Fragment를 연결해 관리 상태의 픽셀 억제와 나머지 세 조합의 복귀를 확인했다. 수면 광학 전체 렌더 비교는 아니다. 식생은 원본/수정 HLSL 함수의 변위를 실제 GPU에서 비교했다. 구름은 원본/수정 RenderCloudVolume 출력을 같은 재질·시계에서 비교했다.
- 1차 식생에서 새 `potentially uninitialized variable` 경고가 발생해 조기 반환을 단일 반환으로 수정했다. [최종 식생 재검사](../../output/shader-overhead-20261113/foliage-final-result.txt) 18건 PASS, 최대 절대차 약 `7.21e-9`. 70+18을 서로 다른 88개 검증 조건으로 표시하지 않는다. GPU 비교 허용치는 환경/수면 0.004, 식생/구름 0.002이며 최종 식생 이외의 1차 비교는 차이 0이었다.
- 이 검사는 `execute_code` 직접 검사이며 NUnit Test Runner 통과 건수가 아니다. 검사 셰이더·재질·RT·메시·텍스처는 메모리에서 만들고 finally로 해제했다. 기존 환경 검사 Render helper는 변경하지 않고 재사용했다.
- [최종 import·SetPass·표면 검사 결과](../../output/shader-overhead-20261113/final-validation.txt): 대상 Shader/Graph 8개 모두 지원/SetPass 성공/오류 0. 식생 미초기화 경고는 실제 Grass·Leaves·Plant·Idyllic 경로에서 제거됐다. Resolve 루프 경고 2개, Grass의 폐기 예정 keyword/include 경고 2개, Leaves의 음수 pow 경고 1개, Plant의 음수 pow 경고 2개는 남았다. 경고 전체 0이라고 보고하지 않는다. Resolve 경고의 변경 전후 동일 여부는 별도 비교 전까지 미확정이다.
- 같은 최종 검사에서 기존 `EveryPassCompilesWithOpaqueAndNormalMapVariants`, `GpuWetnessDarkensAndExtinguishingRemovesOnlyEmission`, `OnlyExplicitFoliageCoverageMayRemoveTheSilhouette`, `FoliageIgnoresWoodDamageAndBurningEdges` 4개 메서드를 직접 호출해 PASS했다. 최종 기록은 Play false / Pause false이며, 에이전트는 Play 종료 명령을 실행하지 않았다. 이후 Console 조회는 오류 0개를 반환했다.
- 이후 동일 4개 검사를 **정식 EditMode Test Runner로 실행해 4/4 PASS, 실패/건너뜀 0**을 확인했다. job `db5008aec0ba463996121a0e7bd57738`, 실행 8.57초. [MCP 결과 요약](../../output/shader-overhead-20261113/editmode-result.json)은 get_test_job의 실제 반환 요약과 지정 필터를 보존한 자료이며 NUnit XML 원본은 아니다. 앞의 직접 검사와 별도 조건으로 중복 합산하지 않는다.
- MCP는 `WinError 10053`, 연결 종료, 응답 시간 초과, 일시적인 인스턴스 미검출이 반복됐다. 긴 명령의 응답 실패 뒤에도 Editor에서 실행이 끝나 결과 파일이 생기는 경우가 있어, 파일의 실제 PASS/종료 내용을 확인한 결과만 집계했다. Roslyn 명시 호출은 패키지 미설치로 거절되어 추가 설치 없이 기본 CodeDom을 사용했다.
- Frame Debugger enable 응답의 event count는 0이었고 후속 이벤트 조회는 시간 초과였다. 최종 disable 성공(`enabled=false`)을 확인했다. 실제 드로우 이벤트 수와 셰이더별 GPU 시간은 확보하지 못했다.
- FrameTimingManager 1회 응답(CPU 약 40.35ms/GPU 약 0.34ms)은 대상 구도·워밍업·셰이더별 비용·전후 비교가 통제되지 않아 절감 근거에서 제외했다. **성능 개선량은 미측정**, SO01~04의 실제 장면·사용자 품질 확인 대기다.

### 보존과 후속

이번 작업 전 사용자 변경이 포함된 원본 7개는 `output/shader-overhead-20261113/baseline/`에 보존했다. GPU 전후 비교의 입력이자 미커밋 사용자 데이터 보존 자료이므로 임의 삭제하지 않는다. 준비표 `output/harness/20261113-150109-c97e85b8/`와 소형 결과 텍스트·검사 스크립트도 재현 근거로 유지한다. 별도 빌드·임시 Unity 씬·구매 아트·재질·SO 수정 없음. Player/Addressables 빌드는 실행하지 않았다.

이하 항목은 **개선 전 최초 조사**이며, 현재 구현 여부는 위 표를 기준으로 한다.

## 우선 개선 후보

### 1. 관리 수면의 일반 패스가 정점 처리 후 모든 픽셀을 버림

근거: [LakeWater.shader](../../Assets/06.Shaders/LakeWater.shader) 96·110행, [UnderwaterRendererFeature](../../Assets/02.Scripts/Gameplay/World/UnderwaterRendererFeature.cs)의 Surface Info DrawMesh.

관리 수면은 Surface Info와 Resolve가 화면을 구성한다. 그러나 일반 Forward의 정점 함수도 파도 변위를 계산하며, Fragment에 도달한 뒤 `_WaterSceneActive && _WaterManaged` 조건으로 `clip(-1)`한다. 일반 패스가 제출되면 화면 기여 없이 드로우 제출·정점 처리·삼각형 준비가 남는다. Fragment 초반 clip은 이후 광학 계산을 막지만 정점 비용까지 없애지는 않는다.

개선 방향: 관리 상태에서 일반 Forward 제출 자체를 생략할 수 있는 경로를 검토한다. 카메라별 Feature 미지원·비활성·SceneView에서 기존 수면으로 복귀하는 계약이 있으므로 전역 재질 패스를 무조건 끄면 안 된다. 실제 중복 드로우 발생과 메시 정점 수는 Frame Debugger에서 확인해야 한다. 현재 로드된 LakeWater 슬롯은 5개였지만 모두 관리 수면이라는 조회 결과는 확보하지 못했다.

### 2. 하늘의 출력 기여 0 구간·비활성 형상에서 세부 밀도 계산

근거: [DaySkyCloudVolume.hlsl](../../Assets/06.Shaders/DaySkyCloudVolume.hlsl) 54~77·121·136·170·198행.

- 초기 제외는 `view.y < -0.02`지만 최종 `horizonFade`는 `view.y <= -0.01`에서 0이다. 따라서 `[-0.02, -0.01]`의 광선이 구름층과 교차하면 결과를 배경으로 되돌리면서도 ray march에 진입할 수 있다. 실제 교차 여부는 카메라 고도·거리 제한에 따른다.
- Formation 경로는 `activation`을 구하기 전에 fine/micro 3D 텍스처를 두 번 읽는다. `activation == 0`이면 mass는 0이고 반환은 blanket만 필요하다. activation을 먼저 판단하면 이 경로의 두 세부 샘플을 생략할 여지가 있다. 기존 blanket과 surfaceDistance 반환 계약을 보존해야 한다.
- 현재 하늘 재질은 표본 수 96, coverage 0.98, density 2.4, Formation 활성 1이었다. 밀도 있는 표본마다 직접광 밀도 조회 4회와 하늘 차폐 조회 2회가 추가된다. 최대 반복 수까지 모두 실행했다는 뜻은 아니며 불투명도·거리·빈 공간 조기 종료가 이미 있다.

먼저 기여 0 경로를 줄이고, 해상도 축소·표본 수 감소는 별도 화질 비교로 판단한다. 표본 수를 일괄 낮추면 기존 층무늬·깜박임 문제가 재발할 수 있다. 현재 높은 coverage에서는 activation 0 최적화의 실제 효과가 작을 수 있다.

### 3. 식생 접촉 반복을 결과가 0인 정점에서도 수행

근거: [UPG_FoliageInteraction.hlsl](../../Assets/06.Shaders/Foliage/UPG_FoliageInteraction.hlsl) 15~35행.

최대 8개 액터의 거리·정규화·낙하량을 계산한 뒤 `rootMask * _UPG_FoliageBend.x`를 곱한다. 뿌리 고정 정점이나 전체 눕힘 강도 0에서는 이 반복의 출력이 0이다. 마스크·강도·액터 수가 0일 때 반환하는 후보가 있다. 현재 액터 수의 실제 조회는 연결 소실로 확보하지 못했다. 액터 수 0이면 반복을 돌지 않는 기존 동작은 유지한다.

거리 밖 모든 정점의 반복까지 줄이려면 오브젝트/청크별 영향 액터 선별 또는 거리별 상호작용 품질을 검토한다. 이는 단순 조기 반환보다 범위가 크고 경계에서 풀 움직임이 끊기지 않는지 확인해야 한다. 뿌리 마스크는 정점마다 달라 분기 효율도 GPU 측정이 필요하다.

현재 조회의 식생 재질 슬롯은 PlantGraph 6,402 / Grass 19,021 / LeavesGraph 20,734 / Idyllic 651이었다. **슬롯 참조 수이며 드로우콜·실제 가시 LOD·렌더링 인스턴스 수가 아니다.** `Renderer.isVisible`은 여러 카메라와 그림자의 영향을 받으므로 이 수로 GPU 시간을 추정하지 않는다. 적용 범위가 넓다는 근거로만 사용한다.

### 4. 반응이 없는 환경 표면에서도 반응 마스크·3D 노이즈 계산

근거: [EnvironmentSurface.hlsl](../../Assets/06.Shaders/Environment/EnvironmentSurface.hlsl) 140~205행.

Forward는 `_ReactionMask` 샘플과 EnvNoise를 무조건 작성 경로에 둔다. EnvNoise는 해시 8개를 보간하며 각 해시는 sin을 사용한다. 탄화·균열·발광이 모두 0이면 이 패턴은 최종 표면색에 필요하지 않고, 젖음만 있을 때도 3D 패턴은 필요 없다. `_UseReactionMask == 0`에서도 마스크를 먼저 읽고 lerp로 무시한다.

개선 방향: 셀 마스크를 포함한 실제 상태를 읽은 뒤 젖음/탄화/균열/발광에 필요한 계산만 수행한다. 반응 마스크 샘플은 사용 여부로, 균열은 표면 역할과 상태로 제한한다. 현재 씬 Renderer 조회에서는 ToonReactive 슬롯이 나오지 않아 이 항목을 현재 씬의 지배적 비용으로 보지 않는다. Compiler가 분기 이동·샘플 제거를 수행할 수 있으므로 생성 코드 또는 GPU A/B 확인이 필요하다.

### 5. 물 Resolve의 전체 화면 탐색과 추가 복사: 구조 개선 후보

근거: [WaterSceneResolve.shader](../../Assets/06.Shaders/Water/WaterSceneResolve.shader) 148~181행, [WaterCommon.hlsl](../../Assets/06.Shaders/Water/WaterCommon.hlsl)의 WaterCollectIntervals·WaterEvaluateMedium, [UnderwaterRendererFeature](../../Assets/02.Scripts/Gameplay/World/UnderwaterRendererFeature.cs) 225·298~310행.

수역이 카메라 frustum에 하나라도 남으면 전체 화면 Resolve가 등록된다. 각 픽셀은 Near Clip 보정을 위해 수역/셀을 순회하고, 수면 정보가 없는 픽셀도 매질 평가를 수행한다. 수면 픽셀은 앞/뒤 구간을 별도로 평가한다. 셀 수와 화면 해상도가 함께 증가하면 비용이 커질 수 있다. 투명 입자 역시 `WaterApplyTransparent`를 통해 매질을 다시 평가하므로 투명층 중첩이 비용을 확대할 수 있다.

이 경로는 수면이 안 보이는 수중·반잠수·안개 소유권을 보존하기 위한 것이므로 **정보가 없는 픽셀을 바로 원본색으로 반환하는 수정은 부적절**하다. 보수적인 수역/광선 broad phase와 Near Clip 위험 수역 선별을 먼저 검토한다. 수역 밖에는 공기 안개를 동일하게 적용해야 한다.

Surface Info는 full-resolution RGBA32F와 별도 Depth32를 사용한다. 예를 들어 1920×1080에서 원시 저장량은 약 31.6MiB + 7.9MiB다(실제 카메라 크기·할당량·대역폭 실측이 아님). MSAA 색 Resolve 대상과 Opaque Snapshot은 별도다. OpaqueTexture가 있으면 다시 전체 화면 Snapshot을 복사한다. 기존 URP 복사와의 중복 여부는 프레임에서 확인해야 한다. 정밀도를 낮추거나 복사를 제거하면 깊이/ID 인코딩·반잠수·투명 굴절·MSAA 회귀 위험이 있다.

### 6. 조건부 비용 후보는 컴파일 결과 확인 후 판단

WaterSceneResolve 198~200행은 반사 환경 샘플을 구한 뒤 수중이면 다른 색으로 덮는다. 207행은 포말 계수가 0이어도 WaterStampGradient 호출을 작성 경로에 둔다. 환경 Outline도 폭 0을 Fragment에서 clip한다.

수중 반사 샘플은 컴파일러가 조건부로 옮길 수 있어 **실행 오버헤드 확정 항목으로 세지 않는다**. 포말 0 분기를 추가할 경우 WaterStampGradient 안의 ddx/ddy를 비균일 분기에 가두지 않도록 해야 한다. Outline은 실제 제출·폭 0 재질 여부를 확인한 뒤 패스 제외를 검토한다. 선언된 패스 수만으로 모든 패스가 매 프레임 실행된다고 판단하지 않는다.

## 이미 제외되어 있는 비용

- 물·비: 원경/차폐 강수 파문, 접촉 스탬프 없음/강도 0, frustum 밖 수역, 강제 숨김 Renderer 제외가 현재 소스에 있다. 수면이 안 보이는 수중 유지는 의도된 동작이다.
- 환경 생물: 그룹 frustum·거리·준비 상태 선별과 빈 가시 목록의 패스 미등록이 있다. Compute 존재만으로 항상 실행한다고 판단하지 않는다.
- 고속 블러: 강도 0이면 Feature에서 패스를 등록하지 않는다. 화면 중앙과 0.5픽셀 미만 변위는 단일 샘플로 반환하며 반복은 4~16회다.
- 숨 경고 UI: `_danger <= 0.001`이면 Image를 비활성화한다. 평상시 보이지 않는 전체 화면 효과가 계속 그려진다는 지적은 현재 코드에 해당하지 않는다. Danger가 켜진 동안 `_UnderwaterBlend=0`에 곱해 없어지는 기포 계산은 추가 확인 후보다.
- 고밀도 수면 메시: 이전 물가 일렁임 정확도를 위해 채택한 근거가 있다. 버텍스 수만 보고 불필요하다고 판정하지 않는다.
- 셰이더 변형: multi_compile 선언은 있지만 실제 스트리핑 후 변형 수·빌드 크기는 이번에 측정하지 않았다. 선언 조합의 단순 곱을 런타임 오버헤드로 제시하지 않는다.

## 검증과 한계

- AI 정적 검사: 주요 소스·호출자·기존 물 비가시 개선·현재 UI/블러 차단 경로 대조.
- Unity 읽기 조회 2회 성공: `LakeVillageWorld`, Play Mode true / Pause true 유지. 실제 skybox와 수치, 활성 계층 Renderer의 커스텀 셰이더 슬롯 분포 확인. `FindObjectsByType` 기본 조회이므로 비활성 계층·Terrain 전용 렌더·간접 드로우·Canvas 전체 사용량을 포함하지 않는다.
- 후속 조회는 `No Unity Editor instances found`로 실패. 물 실행 카운터·반응 재질 수치·접촉 액터 수는 확보하지 못했다. 재시작·재접속 복구·씬 저장은 수행하지 않았다.
- GPU 시간·Frame Debugger·컴파일된 GPU 명령은 미측정. 정적 비용 후보를 실측 병목이나 성능 개선 완료로 보고하지 않는다.
- 코드 수정이 없어 컴파일·테스트 실행 없음. Player/Addressables 빌드 미수행.
- 기존 다수 미커밋 변경 보존. 산출물은 이 보고서와 위키 연결, 준비표 `output/harness/20261113-145635-ceee1479/`뿐이다. 준비표는 조사 범위 식별용으로 보존하며 검증 통과 증거가 아니다. 임시 씬·빌드·캡처 생성 및 삭제 없음.

## 사용자 측정 대기

대상: 기존 LakeVillageWorld, 기존 세이브, 동일 해상도·MSAA·날씨·카메라에서 비교한다. 현재 코드는 변경되지 않았으므로 우선 기준선을 수집한다. 일시정지 상태의 시간은 실제 플레이 성능 기준으로 쓰지 않는다.

| ID | 절차 | 확인점 |
|---|---|---|
| SO01 | 물가/호수 반대편/잠수 아래 보기 각 구도에서 GPU Profiler와 Frame Debugger 확인 | Surface Info·Resolve·Opaque Snapshot 및 일반 LakeWater Forward의 실제 실행, draw 정점 수·GPU 시간 기록. 수역 frustum 제외 때 물 패스 소실 여부 확인 |
| SO02 | 같은 날씨로 하늘이 넓은 구도와 지면 구도에서 Day Sky GPU 시간 기록 | 구름 화면 점유율에 따른 비용 확인. 후속 수정 비교는 동일 시점에서 수행하고 구름 경계·층무늬·깜박임 확인 |
| SO03 | 식생 밀집 구역에서 플레이어 정지/이동, 가까운 풀과 원경 수관 관찰 | 관련 패스 GPU 시간과 실제 LOD/instancing 확인. 후속 최적화 시 뿌리 고정·접촉 반응·그림자 일치 유지 |
| SO04 | 실제 반응형 나무/풀에서 평상시·젖음·연소·파손을 각각 관찰 | 상태별 GPU 시간 기준선과 연출 유지 확인. 현재 씬 조회에서 ToonReactive 미검출이므로 실제 적용 대상부터 명시 |

회신 형식: `ID / 수행·미수행 / 해상도·MSAA·GPU / 패스명과 GPU ms / 화면 이상·캡처`.
