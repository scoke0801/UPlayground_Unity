# 수중·강수 프레임 저하 개선

- 요청: 물에 들어간 상태에서 비가 올 때 심한 프레임 저하 조사·개선.
- 정본: [물 통합 계약](../../Assets/docs/design/world-art/environment/TOON_WATER_EXISTING_INTEGRATION.md), [월드 위키](../wiki/world-art.md).
- 현재 판정: 구현 반영·최종 EditMode 32/32 통과·정규 호수 MSAA4 렌더 6조건 통과·대상 셰이더 4종 오류 0. 실제 게임 프레임 개선량과 화면 인수는 사용자 테스트 대기.

## 조사·구현 (2026-11-13)

열린 Unity 6000.3.21f1, LakeVillageWorld, Edit Mode에서 조사했다. 호수는 시각 메시 104,025정점과 광학용 프리즘 94셀을 사용한다. MainCamera 출력은 2560×1440이다. 비 입자는 최대 1,800개이며 Weather Particle 셰이더를 사용한다. 기존 화면 밖 컬링·잠수 시 방출 차단·수면 뒤 강수 clip이 이미 적용되어 있다. 이번 조사는 사용자 플레이의 Profiler 캡처를 확보한 것은 아니다.

확인한 고비용 경로는 수중 전체 화면 합성과 물 인식 투명 입자가 공유하는 `WaterCollectIntervals`다. 광선과 셀을 자른 뒤 파도가 있는 모든 구간에 8단계 파면 탐색을 적용했다. 깊은 물에도 파도 높이 평가·구간 병합이 반복된다. 근평면 보정도 모든 셀에서 파도 함수를 먼저 평가했다.

- `WaterCommon.hlsl`: 두 파도의 절대 진폭 합을 이용해 항상 물인 구간을 먼저 추가한다. 파면 탐색은 기준 평면 ±최대 진폭 구간으로 한정한다. 체적·건조 공간 차집합·중복 수역 우선순위는 기존 공통 처리를 유지한다. 새 품질 수치나 강수량 축소는 없다.
- `WaterSceneResolve.shader`: 근평면 선분이 파도 최대 범위에 닿을 수 없으면 파도 함수 평가 전에 보정을 건너뛴다. 수면 근처에서는 기존 보정을 유지한다.
- 이어진 물 구간은 8개 조각으로 반복 병합하지 않고 공기 전환점 또는 마지막 단계에서 한 번만 병합한다. 배열 함수 중복 전개를 피하면서 수면 근처의 병합 비용도 줄인다.
- `WaterRenderingContractTests.cs`: 평면/경사 수면, 깊은 물/공기/수면 근접, 상승/하강/수평 광선 12조건을 독립 CPU 수치 적분과 GPU 광학 길이·투과율로 비교한다.

## 측정·검증

256×256 ARGBFloat RT, 94셀, 2파도, 200m 광선의 GPU Probe를 사용했다. `Graphics.Blit` 후 ReadPixels 동기화까지 Stopwatch로 측정하므로 순수 GPU 타임스탬프나 실제 게임 프레임 시간이 아니다. 최종 비교는 시계 0 고정·조건별 6회 예열 뒤 10회 중앙값이다. 원본은 사용자 변경이 포함된 작업 전 HLSL을 메모리 셰이더로 컴파일하며 프로젝트 파일을 원복하지 않았다.

| 조건 | 수정 전 중앙값(ms) | 최종 수정 후 중앙값(ms) |
|---|---|---|
| 수중 y=-2, 완만한 상승 | 18.7589 | 2.85245 |
| 수면 y=0 | 2.96955 | 1.15010 |
| 공기 y=2 | 0.51200 | 0.50085 |

수중 합성 Probe 비용은 약 84% 감소했다. 원본 [측정](../../output/WaterRainPerformance/baseline-timing.json)과 [최종 소스 측정](../../output/WaterRainPerformance/final-source-timing.json), [재현 코드](../../output/WaterRainPerformance/controlled-benchmark.csx)를 보관한다(`BASELINE`을 true/false로 치환해 execute_code 실행). 수중 광학 길이는 수정 전 102.089m, 수정 후 102.090m로 교차점 근사 차이가 있다. 예열 전 초기 셰이더 준비 비용은 제외했으며 초기 실험에서 약 99ms 표본도 관측했다. 검사 조건에는 실제 강수 파문 렌더가 포함되지 않는다. 게임 전체의 성능 개선율로 환산하지 않는다.

- 최종 Unity 컴파일·리로드 완료 후 **EditMode 32/32, 실패/건너뜀 0**, job `447999174adf4cfe965c9b8cb791b840`. [최종 Test Runner 반환](../../output/WaterRainPerformance/final-source-editmode.json)은 MCP 반환 원본이며 NUnit XML은 아니다.
- 첫 새 검사 7건 실패는 CPU 0초/GPU 전역 시계 불일치였다. Probe 재질 시계를 0으로 고정한 뒤 직접 12조건과 정식 32조건이 통과했다. [실패 원본](../../output/WaterRainPerformance/initial-test-result.json)을 보존한다.
- 전체 Resolve 검사에서 임시 레지스터 4096 한도 초과를 발견했다. 구간 배열 병합 함수를 중복 전개하지 않게 공통 호출로 모으고 연속 물 구간은 한 번만 병합해 수정했다. 이 최종 소스로 EditMode·렌더·성능을 모두 재검증했다. 교차점 이분 탐색의 기존 8회 unroll은 유지한다.
- 정상 import/SetPass가 120초 MCP 응답 제한을 넘긴 사례가 있었다. 반복 Refresh나 Editor 재시작 대신 동일 Editor 후속 조회로 컴파일 종료를 확인했다. 최종 [ShaderUtil 진단](../../output/WaterRainPerformance/final-source-diagnostics.json): SurfaceInfo·SceneResolve·ContractProbe·WeatherParticle 오류 0. Resolve/Probe의 기존 loop 경고 각 2건은 남아 있다.
- 정규 LakeVillageWorld에서 임시 카메라로 320×180, MSAA4, 높이 +2/-2/0.0001m × rainIntensity 0/1의 **6조건 실제 렌더**를 실행했다. 모두 Body1/Cell94/색 샘플4, 분홍 오류 화면 없음. [절차](../../output/WaterRainPerformance/render-smoke.csx)·[반환](../../output/WaterRainPerformance/render-result.json)·[비 수면](../../output/WaterRainPerformance/render-2-1.png)·[비 수중](../../output/WaterRainPerformance/render--2-1.png)을 보관한다. 이는 Edit Mode의 강수 파문 주입이며 실제 Play의 비 입자·사용자 입력·날씨 전환 시험이 아니다.
- 최종 씬 dirty=false·임시 카메라0. 마지막 Console 오류 조회0, `git diff --check` 통과. Hot Reload는 Edit Mode·미등록 테스트 메서드 변경이므로 적용 대상 밖이며 정상 컴파일했다. 새 Editor·Player/Addressables 빌드·커밋 미수행.

## 사용자 테스트 대기

기존 LakeVillageWorld와 저장 데이터, 같은 해상도·품질·카메라 구도, 키보드/마우스로 확인한다. Player·Addressables 빌드는 요청하지 않았으므로 실행하지 않는다.

| ID | 절차 | 기대 결과·실패 기준 |
|---|---|---|
| WR01 | 같은 물가에서 맑음/비 각각 육지→얕은 물→수영→잠수 | CPU/GPU 프레임 시간을 각각 기록. 비가 올 때 수중에서만 심해지는 끊김이 남으면 실패 |
| WR02 | 비가 오는 상태에서 수면을 올려다보고 아래를 본 뒤 수면 경계 왕복 | 수중 색·투과·반사 유지. 수면 경계 깜빡임/마른 틈/강수 관통은 실패 |
| WR03 | 같은 수중 구도로 Profiler의 Water Scene Resolve 시간 비교 | 동일 해상도·강수·시계 조건에서 비교. 합성 Probe 결과를 게임 FPS로 대신 쓰지 않음 |

회신: `ID / 통과·실패·미수행 / 실제 결과 / 해상도·품질·프레임 시간 / 필요 시 로그`.

## 보존·정리

기존 미커밋 변경은 보존한다. 작업 전 사용자 변경을 포함한 파일 3개·실패/중간/최종 측정·재현 코드·렌더 6장은 `output/WaterRainPerformance/`에 보관한다(1MB 미만). 초기 준비표 `output/harness/20261113-155209-2c3fba2e/`, 최종 준비표 `output/harness/20261113-160723-dac4d61c/`를 보존한다. 중간 준비표 160030은 최종본으로 대체해 제거했다. 씬·SO·프리팹은 저장하지 않았다. 임시 GPU 자원과 카메라는 finally/TearDown에서 해제했다. 위키·상태·설계의 로컬 링크 존재를 확인했다.
