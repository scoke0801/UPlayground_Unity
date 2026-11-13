# 물·비 비가시 비용 검토

- 요청: 보이지 않는 원경 파문·물·비의 불필요한 연산을 차단하고 이미 차단된 경로는 유지한다.
- 정본: [물 통합 계약](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md), [월드 위키](../wiki/world-art.md).
- 판정: 구현 반영, 열린 Unity의 직접 assertion 검사 5건 통과, 최종 셰이더 4종 오류 0. Test Runner 실행 및 실기 GPU 시간은 미수행이다.

## 조사와 반영 (2026-11-13)

| 대상 | 조사 결과와 수정 |
|---|---|
| 비 파문 | 기존 화면 미분 기반 감쇠가 있었으나 고리 9개를 계산한 뒤 결과에만 곱했다. 완전 감쇠 시 마스크 샘플·고리 계산 전에 반환하고, 노출 마스크가 0인 구역도 고리 계산을 생략한다. 비 강도 0의 기존 조기 반환은 유지한다. |
| 접촉·폭포 파문 | 스탬프가 없거나 강도가 0이면 반환한다. 기존 SO의 고리 폭과 한 화소의 월드 크기를 비교해 원경에서 부드럽게 감쇠하고, 완전 감쇠 시 스탬프 반복을 생략한다. SurfaceInfo 법선과 Resolve 포말은 같은 함수를 사용한다. |
| 물 패스 | 기존 검사는 layer·scene만 검사했다. 재사용 Plane 배열로 카메라 frustum과 수역 체적을 검사하고, 비활성·강제 숨김 렌더러도 제외한다. 대상이 없으면 기존 Feature가 업로드·수면 정보·전체 화면 합성을 등록하지 않는다. |
| 수중 보존 | 수면 bounds만 검사하면 아래를 보는 수중 카메라에서 광학이 꺼진다. 로컬 체적의 변환된 AABB와 파도 최대 변위를 포함하며, 무제한 수직 투영 수역은 카메라 far 범위까지 아래로 포함한다. |
| 비 입자 | 실제 열린 정규 씬의 Rain은 `AlwaysSimulate`였다. 초기화 시 전용 비·눈·먼지에 `Pause`를 적용해 화면 밖 시뮬레이션과 재진입 catch-up을 방지한다. 기존 실내/잠수 방출 차단·남은 입자 제거·날씨 종료 StopEmitting은 유지한다. |

코드: [스냅샷](../../Assets/02.Scripts/Gameplay/World/WaterRenderSnapshot.cs), [환경 입자](../../Assets/02.Scripts/Gameplay/World/LivingWorld/EnvironmentEffects.cs), [수면 정보](../../Assets/06.Shaders/Water/WaterSurfaceInfo.shader), [공통 파문](../../Assets/06.Shaders/Water/WaterCommon.hlsl), [회귀 검사](../../Assets/02.Scripts/Editor/WaterRenderingContractTests.cs).

기존 미커밋 WaterSurfaceInfo·WaterSceneResolve·WaterRenderingContractTests 등의 사용자 변경을 보존했다. SO·프리팹·씬·GUID는 수정하지 않았다. 수영/높이 조회와 날씨 생산자의 게임플레이 갱신은 시각적 비가시성과 무관하므로 유지한다. 벽 뒤이지만 frustum 안인 수역의 occlusion 판정, 공유 자산 메모리 해제, GPU 시간 절감 수치까지 해결했다고 주장하지 않는다.

## 검증

- 열린 Unity MCP에서 실제 Rain `AlwaysSimulate` 확인.
- 동일 Editor의 컴파일 후 `execute_code`에서 기존 NUnit fixture의 SetUp/TearDown과 assertion을 직접 실행: 제한 체적 유/무 가시성 2건, 잠수/재부상 날씨 1건 모두 PASS. 화면 밖 제외·카메라 아래 수중 유지·forceRenderingOff 제외·Pause 설정을 확인했다. **Test Runner 실행 결과나 렌더링 인수 결과가 아니다.**
- EditMode Test Runner job `ef4294152a12472d9e75b7dd66705368`는 Editor가 Play Mode라는 사유로 시작 실패(실행 0건). 사용자의 일시정지 플레이 상태를 유지했다.
- 중간에 MCP 연결 종료·WinError 10053이 있었으나 같은 Editor에 재연결해 최종 조회했다. ShaderUtil에서 WaterSurfaceInfo/WaterSceneResolve/WaterContractProbe/WeatherParticle 4종 오류 0. 런타임 수면/합성/날씨 셰이더는 경고 0이며 Probe에 기존 광학 루프 경고 2건만 남았다. 새 WaterStampGradient 초기화 경고는 단일 반환·명시 초기화로 제거했다.
- 최종 공통 HLSL에서 실제 GPU Waves(큰 월드 좌표 CPU/GPU 일치)·Medium(수중 거리 감쇠) assertion 2건 PASS. 앞의 가시성/날씨 3건과 합쳐 직접 검사 5건이며 NUnit Test Runner 통과 건수로 표시하지 않는다.
- Console에는 별개 RuntimePlacementLoader ActorDatabase 준비 시간 초과 3건이 조회되었다. 물/날씨 변경과의 인과는 확인하지 않았으며 요청 범위 밖이라 수정하지 않았다. Console 전체 오류 0으로 보고하지 않는다.
- `git diff --check` 대상 파일 통과. Player·Addressables 빌드 미수행.
- 준비표: `output/harness/20261113-145414-f2850087/`. 현재 검증 범위를 식별하는 자료로 보존한다. 초기 준비표 20261113-144852-06aedd56의 2개 파일은 최종본으로 대체하여 제거했다. 별도 임시 씬·캡처·빌드 파일 생성 없음, 직접 검사의 임시 GameObject·Mesh·Material·SO·RT는 TearDown/finally로 제거.

## 사용자 테스트 대기

대상은 기존 LakeVillageWorld, 키보드/마우스 TPS, 기존 세이브를 유지한 상태다.

| ID | 조작 | 기대 결과 / 실패 기준 |
|---|---|---|
| WV01 | 비 오는 호수에서 근접→원경→근접으로 이동 | 가까운 파문 유지, 멀리서 점멸 없이 감쇠. 가까운 파문 소실/갑작스러운 경계는 실패 |
| WV02 | 호수 반대 방향을 보다가 재진입하고 잠수 후 아래를 본다 | 화면 밖 물 패스 제외, 재진입 복구, 수면이 안 보여도 수중 색·감쇠 유지 |
| WV03 | 비/눈을 시야 밖에 두었다가 카메라를 돌리고 실내·잠수·재부상 | 밀린 시뮬레이션의 순간 부하 없이 복구. 멈춘 입자 덩어리가 두드러지거나 실내/수중 강수가 남으면 실패 |
| WV04 | Unity Profiler/Frame Debugger에서 동일 카메라 구도를 비교 | 수역 체적이 frustum 밖일 때 Water Domain Upload/Surface Info/Scene Resolve가 사라지는지 확인. 실기 GPU 절감량은 별도 측정 |

회신: `ID / 통과·실패·미수행 / 실제 결과 / 필요 시 화면·로그`.
