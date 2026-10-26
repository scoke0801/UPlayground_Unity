# 생명의 호수 프레임 개선 및 사용자 검증

## 조사 결과

2026-10-25, Unity 6000.3.21f1 Editor Play Mode, PC 품질, Game 카메라 2560×1440에서 조사했다. Boot에서 기존 SceneManager로 LakeOfLifeScenario를 로드하고 시작 위치에서 입력 없이 측정했다. CPU Profiler를 켜고 Deep Profile 및 allocation callstack은 사용하지 않았다. 각 표본은 로딩 완료 후 최근 120프레임의 평균이다.

| 항목 | 수정 전 | 수정 후 |
|---|---:|---:|
| Main Thread 프레임 | 43.55ms (23.0FPS 환산) | 31.19ms / 27.34ms (32.1 / 36.6FPS 환산) |
| ScriptRunBehaviourLateUpdate | 9.44ms | 1.07ms / 1.06ms |
| ActorCameraProximityDither.LateUpdate | 7.33ms | 두 번째 표본 0.118ms |
| EnvironmentController.LateUpdate 자체 비용 | 0.993ms | 두 번째 표본 0.191ms |
| GetComponentNullErrorMessage | 5.01ms (자체 4.54ms) | 두 번째 표본에서 관측되지 않음 |

- 장면 전체 활성 오브젝트의 Renderer 수는 전후 20,206개였다. 이는 실제 카메라에 보이는 렌더러 수가 아니다. 수정 전 Draw Call 단일 표본은 1,819회, SetPass는 418회였다.
- 첫 프레임 타이밍 표본은 CPU 33.94ms / GPU 17.10ms였다. CPU 병목 조사에 사용했으며 GPU 개선량은 측정하지 않았다.
- 전체 프레임은 에디터·백그라운드 작업·날씨·NPC 상태에 영향을 받는다. 위 수치는 짧은 정지 표본이며 이동/전투 평균, 1% low 또는 제품 성능 보장이 아니다. 공유 작업 공간에서 다른 코드도 변경되고 있었으므로 전체 개선량을 두 수정만의 엄밀한 A/B 결과로 간주하지 않는다.
- 실제 병목 코드 두 곳의 비용 감소는 Profiler에서 확인했다. 최종 60FPS 달성은 미확인이다.

근거 파일: `Logs/LakePerformance/before-profile.json`, `after-profile-1.json`, `after-profile-2.json`. 원본 전체 프로파일 대신 작은 집계 JSON만 보관했다.

## 반영 내용

1. `ActorCameraProximityDither`: 디더 재질의 준비와 실제 적용 상태를 구분한다. 완전히 보이는 액터에 원본 재질 복구를 계속 수행하지 않고, 실제 적용한 재질을 복구한 뒤 종료한다. 복구 시 Presentation 조회를 렌더러마다 반복하지 않는다. 나중에 추가되는 Presentation은 계속 해석하며, 조회 실패 시 Editor 할당을 유발하는 GetComponent 대신 TryGetComponent를 사용한다. 근접 페이드·외부 숨김·재질 워밍업은 유지한다.
2. `EnvironmentEffects`: 보호 구역의 행렬·중심·크기를 입자마다 읽지 않고 구역마다 한 번 읽는다. 이동·회전·크기·활성 상태 변경은 호출마다 반영한다. 제거한 입자가 없으면 SetParticles를 생략한다.

씬, 프리팹, 그래픽 품질, 밸런스 데이터는 이 작업에서 수정하지 않았다. 기존 사용자 변경은 유지했다. 커밋 및 Player/Addressables 빌드는 수행하지 않았다.

## 자동 검사 결과

- Unity 컴파일: 오류 0.
- EditMode: 디더 회귀 3개 + 기존 CinematicStageCoreTests 4개 + 환경 효과 회귀 2개, 최종 9개 통과.
- 환경 테스트 첫 실행은 정지한 ParticleSystem에 입자가 준비되지 않아 2개 실패했다. Play/Pause로 입자 시스템을 준비하고 입자 수를 먼저 검증하도록 테스트를 수정한 뒤 2개 통과했다.
- 중간 재실행 1회는 Unity 연결/재컴파일 중 테스트 초기화 시간 초과로 실행되지 않았다. 성공한 재실행과 구분한다.
- 최종 성공 환경 테스트 job: `dda01ee0683f4c61b2cdc6477cf47686`. 디더 및 기존 숨김 검사 job: `0fed36f730d848dcad393f5b15916624`의 7개 통과.
- AI Play Mode 조사: 씬 진입 완료, 정지 상태 CPU 측정, 측정 후 Boot 정지 상태 및 Profiler 비활성으로 복귀. 실제 입력·화면 품질 검증을 대신하지 않는다.

## 사용자 테스트 대기

준비: 위 변경이 반영된 Unity Editor에서 기존 생명의 호수 진입 경로를 사용한다. 동일 저장 상태·시작 위치, PC 품질, Game 해상도 2560×1440, 동일 Scene/Game 창 배치를 유지한다. 게임패드를 기본으로 하고 사용한 입력 장치를 결과에 기록한다. 저장을 덮어쓸 필요는 없다.

| ID | 실행 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| PERF-01 | 로딩 완료 후 같은 카메라 방향에서 30초 정지. F10 HUD를 열고 마지막 수치를 기록. 필요하면 F12로 600프레임 JSON 저장. | 이전 약 20FPS 상황과 비교해 지속 비용이 감소해야 한다. 도구 호출·로딩 직후 표본은 제외. 20FPS 수준이 계속되면 위치·카메라 방향·Profiler 상위 항목 전달. |
| PERF-02 | 동일 동선으로 마을·물가 이동 후 적과 전투. 카메라를 회전한다. | 이동·전투 중 정지/스파이크와 체감 입력 지연을 확인. 시작점의 FPS 상승만으로 통과시키지 않는다. |
| VIS-01 | 벽 가까이에서 카메라를 플레이어 몸 쪽으로 접근시켰다가 멀리 뺀다. 이후 장비·캐릭터 교체 및 대화를 실행한다. | 몸 근접 페이드와 복귀가 정상이며 검은 재질·잔류 투명·인물 실종이 없어야 한다. 대화 클로즈업에 디더가 없어야 한다. |
| VIS-02 | 비가 오는 상태에서 처마 안팎을 이동한다. | 실내 입자는 제거되고 실외 입자는 유지된다. 실내 비 관통 또는 실외 입자의 과도한 소실은 실패. |
| ERR-01 | 위 절차 중 Console의 오류를 확인한다. | 신규 예외·반복 오류 0. 화면 품질과 별도로 기록. |

회신 형식: `항목 ID / 통과·실패·미수행 / 실제 결과 / 첨부 또는 로그`. 실패 시 시작 위치·카메라 방향·입력 장치·저장 상태와 해당 Console 또는 Profiler 캡처만 추가한다. F12 JSON 위치는 `Application.persistentDataPath/PerformanceSnapshots/`이며 F11은 치트 패널이다.

## 남은 비용

수정 후에도 렌더링 CPU 약 9.7~11.1ms, Constraint 갱신 약 2.9~3.3ms, MagicaCloth 약 2.0~2.3ms가 남는다. 각 항목은 포함 관계가 있어 단순 합산하지 않는다. 이들은 화면·애니메이션·옷감 품질 검증을 동반한 후속 조사 대상이며 이번 작업에서 임의로 끄지 않았다. ActorSimulationManager는 GameManager에 정상 등록되어 있다. 로딩 중 Active 액터 수만으로 원거리 정지 실패라고 판정하지 않는다.

## 후속 최적화: 원거리 보조 모션과 피격색 렌더링

### 반영 내용

- `ActorSecondaryMotionSimulation`을 기존 `ActorSimulationManager` 등록 경로에 연결했다. 거리 판정·안전 조건·대화 활성 임대를 그대로 사용하며 별도 Update를 추가하지 않는다. Actor 모듈에 MagicaCloth 의존을 추가하지 않고 조립 계층에서 연결한다.
- 정지 진입 시 활성 Constraint만 기록해 끄고, 재개·활성 임대·등록 해제·컴포넌트 비활성화 시 복구한다. 원래 꺼져 있던 제약은 켜지 않는다. 다음 정지 시 하위 컴포넌트를 다시 수집하므로 모델 교체를 반영한다.
- 정지 액터의 옷감은 카메라 컬링을 `Keep`으로 임시 변경한다. 화면 안에서는 기존 시뮬레이션을 유지하고 화면 밖에서 자세를 보존하며 멈춘다. `enabled`를 끄지 않으므로 MeshCloth가 원본 메시로 돌아가지 않는다. 재개 시 기존 컬링 모드를 복구한다. Scene View 등 다른 카메라에 보이면 계속 계산될 수 있다.
- `ActorColorChanger`가 초기화 때 기본색 MPB를 붙이던 동작을 제거했다. 피격 중에만 색상을 덮어쓰며, 끝나거나 비활성화되면 피격 전 MPB를 복구한다. 원래 MPB가 없었으면 제거하여 SRP Batcher 사용을 막는 잔류 상태를 없앤다. 연속 피격은 최초 원본을 보존한다.
- 이번 후속 변경도 씬·프리팹·품질 설정·거리 임계값을 저장하거나 변경하지 않는다. 기존 공유 작업의 데이터 변경을 보존한다.

### 후속 측정과 자동 검사

- 수정 전 정지 표본과, 같은 Play 세션에서 원거리 Constraint 정지·옷감 Keep 컬링만 임시 적용한 표본을 비교했다. 각 120프레임, PC 품질, Game 카메라 2560×1440이다. 근거는 `Logs/LakePerformance/remaining-before.json`, `remaining-experiment.json`이다.

| 항목 | 후속 수정 전 | 보조 모션 임시 적용 |
|---|---:|---:|
| PlayerLoop CPU | 24.41ms | 20.59ms |
| MagicaManager | 2.10ms | 1.60ms |
| 렌더링 CPU (RenderPlayModeViewCameras) | 10.14ms | 10.02ms |
| Main Thread 전체 | 40.99ms | 37.91ms |
| EditorLoop | 15.76ms | 16.48ms |

- 이번 표본은 EditorLoop 비용이 앞선 조사보다 크게 높았다. Main Thread 전체를 이전 32~37FPS와 직접 비교하거나, PlayerLoop만 FPS로 환산하지 않는다. 다른 작업이 공유 에디터·코드를 변경하고 있었으므로 엄밀한 단독 A/B는 아니다. 임시 적용 표본은 피격색 MPB 수정을 포함하지 않으며 최종 코드의 FPS 보장이 아니다.
- Unity 컴파일: 오류 없음(`scriptCompilationFailed=false`), 새 런타임 및 테스트 타입 로드 확인.
- EditMode 최종: 보조 모션 6개 + 기존 거리 정책 7개 + 피격색 4개 = 17개 통과, 실패/건너뜀 0. job `b43d261872114f2e90685af936083ffc`.
- 첫 새 테스트 실행은 17개 중 7개가 실패했다. EditMode에서 호출되지 않는 일반 MonoBehaviour 생명주기를 준비하지 않은 점과 MagicaCloth 어셈블리명 오기를 고쳐 재실행했다. 이전 7개만 실행된 결과는 신규 검사 통과로 포함하지 않는다. Play Mode/재컴파일 경합과 연결 종료로 실행되지 않은 시도도 최종 통과와 구분한다.
- 실제 입력을 통한 이동·전투·대화·옷감 시각 검수는 사용자 테스트 대기다.
- 최초 최종 코드 재측정은 Test Runner 통과 후 Unity MCP 연결 종료로 실행하지 못했다. 이후 이어서 수행한 재측정 결과는 아래에 기록한다. 피격색 변경만의 개별 렌더링 절감량은 여전히 분리 측정하지 않았다.

### 연결 복구 후 최종 코드 재측정

사용자의 계속 지시에 따라 Unity 연결을 복구하고 Boot → 기존 SceneManager → LakeOfLifeScenario 경로로 진입했다. 로딩 완료, PC 품질, 2560×1440에서 최종 코드를 측정했다. 최근 120프레임(54664~54783)의 집계다.

| 항목 | 최종 코드 |
|---|---:|
| Main Thread 전체 | 25.57ms (39.1FPS 환산) |
| PlayerLoop CPU | 21.25ms |
| EditorLoop | 3.40ms |
| ConstraintManagerUpdate | 0.338ms |
| MagicaManager | 1.735ms |
| 렌더링 CPU (RenderPlayModeViewCameras) | 10.398ms |

- 원거리 보조 모션 적용 전 기록의 Constraint 3.02ms와 비교하면 약 89% 감소했다. 전체 프레임은 표본 시점·에디터 부하가 달라 엄밀한 단독 A/B로 보지 않는다. 이전 32~37FPS보다 높은 정지 표본을 얻었지만 60FPS 달성은 아니다.
- 런타임 조회 당시 플레이어 위치는 `(1118.37, 51.79, 391.17)`였다. 시뮬레이션 대상/연결 컴포넌트 71개, 정지 액터 45개, 활성 Constraint 64개, **정지 액터에 남은 활성 Constraint 0개**를 확인했다. 활성 계층 옷감 115개 중 카메라 컬링 상태는 96개였다. 별도 조회에서 MPB가 있는 Renderer는 26개였다. 로딩·시간에 따라 대상 수는 변할 수 있다.
- 실제 Play Mode에서 정지 NPC 2개와 인간형 적 1개에 짧은 활성 임대를 적용했다. 제약 21/21/23개가 즉시 복구됐으며, 임대 해제 후 정지로 되돌렸을 때 각 제약의 활성 상태가 검사 전과 정확히 일치했다. 이는 API 상태 전이 검사이며 실제 대화·입력·옷감 시각 품질 검증은 아니다.
- Frame Debugger의 불투명 패스 표본(event 250)에서 `Hidden/lilToonOutline` → `Hidden/ltspass_opaque`의 `FORWARD`, DrawCall 1, 분리 원인 `SRP: Node use multi-pass shader`를 확인했다. MPB 제거 후에도 외곽선 다중 패스의 분리가 남는다. 이 한 표본으로 전체 렌더링 비용을 모두 설명하지는 않는다.
- 원거리 Animancer 그래프 평가 중단도 임시 시험했으나, 도중 플레이어 위치가 `(1075.65, 60.56, 439.36)`으로 바뀌어 성능 비교에서 제외했다. 시험한 45개 그래프의 정지를 모두 복구하고 자동 정지 그래프가 남지 않았음을 확인했다. **그래프 정지는 영구 코드에 반영하지 않았다.**
- 이번 이어서 작업은 측정·진단·문서 갱신이며 런타임 코드를 추가 변경하지 않았다. 이전 EditMode 17개 통과 결과를 유지하고, 종료 시 Unity 컴파일 오류 및 Console 오류 0을 확인했다. 새 변경이 없어 동일 검사를 반복 실행하지 않았다.
- Frame Debugger/Profiler/Play Mode를 종료하고 작업 시작 시 열려 있던 `Assets/01.Scenes/Test/CombatTestMap.unity`로 복귀했다. 씬·프리팹·SO를 저장하지 않았다.

근거: `Logs/LakePerformance/final-code-profile-1.json`, `final-runtime-audit.json`.

다음 최적화 우선순위는 화면에 보이는 NPC의 거리별 외곽선 처리와 메시 LOD다. 실루엣·대화 클로즈업·접근 시 전환 품질을 비교해야 하므로 전체 외곽선 비활성화나 품질 설정 하향은 적용하지 않았다. 실제 이동·전투와 아래 시각 검수는 계속 사용자 테스트 대기다.

### 사용자 테스트 대기 — 추가 항목

위 준비 조건과 PERF-01/02, VIS-01/02, ERR-01을 유지한다. 다음 항목도 각각 결과를 남긴다.

| ID | 실행 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| SIM-01 | NPC와 인간형 적을 관찰한 뒤 65m 밖으로 멀어졌다가 55m 안으로 돌아온다. 두 번 반복한다. | 복귀 시 본·장비 위치와 대기/이동 애니메이션이 정상이다. 원래 감춰진 장비가 나타나거나 무기가 몸에서 떨어지면 실패. |
| CLOTH-01 | 옷감·머리카락이 있는 원거리 NPC를 카메라 밖으로 돌렸다가 다시 본 뒤 접근한다. 대화 클로즈업도 연다. | 옷이 원본 메시로 순간 복귀하거나 늘어지지 않는다. 재접근·대화에서 옷감이 정상적으로 반응한다. 수치 검사와 별도로 육안 판단한다. |
| HIT-01 | 플레이어와 적이 한 번, 연속으로 피격되게 한다. 피격 연출 중 캐릭터/장비를 교체하고 다시 복귀한다. | 피격색이 정상적으로 나타나고 종료 후 원래 색상으로 돌아온다. 빨간색 잔류·다른 렌더링 효과 소실·재질 이상이 없어야 한다. |
| SIM-02 | NPC 대화를 시작/종료하고, 구역 이동 후 같은 NPC에게 돌아온다. | 대화 중 본 제약이 정지하지 않으며 재등록 후에도 애니메이션·옷감이 복구된다. 신규 예외 0. |

시작점의 짧은 정지 측정만으로 60FPS 달성이나 전투·이동 품질 통과를 판정하지 않는다. Player/Addressables 빌드는 수행하지 않는다.

## 후속 개선: NPC 외곽선 거리 LOD

### 구현과 적용 범위

- NPC에만 외곽선 거리 LOD를 적용했다. 기존 `ActorCameraProximityDither`가 보유한 원본 재질·카메라·평가 주기를 사용하며 액터별 Update나 별도 매니저를 추가하지 않는다.
- `Assets/10.Datas/Resources/Config/NpcOutlineLodSettings.asset`에서 활성화, 복구 거리 30m, 축소 거리 35m, 기준 수직 FOV 60도를 조정한다. 실제 카메라 FOV로 거리를 보정하여 줌인 시 원본을 더 오래 유지한다. 30~35m 구간은 이전 상태를 유지한다. 직교 카메라는 원본을 유지한다.
- `Hidden/lilToonOutline` → `lilToon`, `Hidden/lilToonCutoutOutline` → `Hidden/lilToonCutout`으로 런타임 사본만 교체한다. 외곽선 폭을 0으로 만드는 방식과 달리 해당 셰이더에는 외곽선 패스가 없다. 표면 속성·알파 클립·렌더 큐·그림자·깊이 패스는 유지한다. 원본 재질과 lilToon 패키지는 수정하지 않았다. 기존 대응 변형만으로 패스를 줄일 수 있어 이번에는 커스텀 셰이더가 필요하지 않았다. 설치된 `lts.shader`/`lts_cutout.shader`의 UsePass 구성을 확인했다([lilToon 원본](https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Shader/lts.shader)).
- 투명·TwoPassTransparent·Multi·Fur 등 다른 패스 의미를 가진 표면은 변환하지 않는다. 대화 카메라/외부 디더 억제, NPC 대화 배치·상호작용, 근접 디더, 비활성화에서 원본을 복구한다. 모델 갱신·파괴 때 임시 재질을 해제한다. 다른 시스템이 교체한 재질 슬롯과 Renderer 숨김 상태는 보존한다.
- 조사한 WorldResidents/r01 NPC 프리팹 4개에는 LODGroup이 없었다. 이번 변경은 **외곽선 패스 LOD**이며 메시 단순화 LOD가 아니다. 메시 LOD에는 스키닝·블렌드셰이프·옷감과 모듈형 장비 경계를 보존하는 저해상도 메시 저작 및 근거리 비교가 추가로 필요하다. 검증되지 않은 자동 메시 축소나 NPC 전체 숨김은 적용하지 않았다.

### 측정과 자동 검사

Unity 6000.3.21f1 Editor Play Mode에서 Boot → SceneManager → LakeOfLifeScenario로 진입했다. PC 품질, Game 카메라 2560×1440, 플레이어 `(1118.37, 51.79, 391.17)`, 카메라 `(1118.37, 54.11, 387.10)`, 회전 `(14, 0, 0)`이다. 같은 Play 세션에서 설정의 **메모리 사본**으로 LOD만 껐다가 켰다. 각 CPU 표본은 도구 실행 직전 끝 5프레임을 제외한 120프레임 평균이다.

| 항목 | LOD 끔 | LOD 켬 |
|---|---:|---:|
| 렌더링 CPU (RenderPlayModeViewCameras) | 9.660ms | 8.167ms |
| PlayerLoop CPU | 20.201ms | 18.798ms |
| Main Thread 전체 | 36.315ms | 24.508ms |
| EditorLoop | 15.185ms | 4.739ms |
| Draw Call (조회 순간) | 1,836 | 1,790 |
| SetPass (조회 순간) | 374 | 299 |
| 삼각형 (조회 순간) | 2,933,231 | 2,719,229 |

- 렌더링 CPU 약 **15.5% 감소**, SetPass 약 **20.1% 감소**를 관측했다. 삼각형 감소는 메시 축소가 아니라 외곽선 패스 제거를 포함한 화면 전체 드로우 표본이다. NPC·날씨는 정지시키지 않았으며 카운터는 평균이 아니다.
- EditorLoop가 크게 달라 전체 프레임 차이를 LOD 효과로 환산하지 않는다. LOD 켬의 전체 프레임은 약 40.8FPS 환산이지만 60FPS 달성·이동/전투 평균·제품 성능을 뜻하지 않는다. 이전 25.57ms 기록과도 엄밀한 A/B가 아니다.
- 처음에는 런타임 Profiler만 켜지고 Editor Profiler 수집이 꺼져 이전 세션 프레임을 반환했다. 이 표본은 제외하고 `ProfilerDriver.enabled`를 켠 뒤 위 유효 표본을 수집했다. 추가 반복 측정 중 두 번 Play가 종료되어 실패한 호출도 비교에서 제외했다. 시작 화면 캡처는 유효한 전후 비교를 얻지 못해 보관하지 않았다.
- 시작점 NPC 29개에 LOD 적용을 확인했다. 실제 NPC `RedTree_Guest`, `Npc_Daon`, `FarmWorker_0`의 대화 배치 임대를 API로 열고 평가해 원본 복구를 확인했으며, 임대 종료 후 셋 모두 이전 LOD 상태로 복귀했다. 실제 대화 입력·화면 검수를 대신하지 않는다.
- Unity 컴파일 오류 0, 최종 Console 오류 0. 새 파일 임포트 도중 잠시 발생한 타입 미해결은 전체 Refresh 후 해소됐다.
- EditMode: 새 LOD 11개 + 기존 디더 3개 + 피격색 4개 = **18개 통과, 실패/건너뜀 0**. job `e8f3af7c7e104a06af5abb78ae553d02`. 외곽선 패스 감소, 속성/그림자 유지, 히스테리시스·줌, 특수 재질 제외, 외부 슬롯 보존, 해제·대화 억제·근접 디더 복구를 검사했다.
- 종료 시 측정용 메모리 설정 사본을 해제하고 Profiler를 껐으며 `CombatTestMap.unity`로 복귀했다. 새 LOD 설정 에셋 외에 씬·프리팹·기존 SO를 저장하지 않았다. 기존 작업의 에셋 변경을 보존했다. Player/Addressables 빌드와 커밋은 수행하지 않았다. 셰이더 참조의 Player 포함·스트리핑 결과는 미검증이다.

근거: `Logs/LakePerformance/outline-lod-comparison.json`.

### 사용자 테스트 대기 — 외곽선

기존 준비 조건과 PERF-01/02, VIS-01/02, ERR-01을 유지한다. 설정은 위 LOD 에셋을 사용하며 테스트 후 기본값으로 복구한다. 저장 데이터를 덮어쓸 필요는 없다. 아래 항목은 모두 사용자 테스트 대기다.

| ID | 실행 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| LOD-01 | 기본 FOV에서 NPC의 카메라 거리를 약 40m → 25m → 40m로 바꾸며 두 번 왕복한다. 30~35m 부근에서 천천히 왕복한다. | 먼 거리에서 외곽선만 줄어들고 가까이서 원복된다. 경계에서 외곽선이 반복 깜빡이거나 얼굴·의상·머리카락이 사라지면 실패. 전환이 눈에 거슬리면 거리와 영상을 전달한다. |
| LOD-02 | 멀리 있는 NPC를 줌으로 관찰한 뒤 접근하여 대화를 시작/종료한다. 옷감·머리카락·눈·얼굴 클로즈업을 확인한다. | 줌과 대화에서 근거리 외곽선·표면 품질이 유지된다. 검은 재질·분홍색·투명 표면 변화·대화 종료 후 재질 잔류가 없어야 한다. |
| LOD-03 | NPC에 카메라를 밀착해 근접 페이드를 확인하고 다시 멀리 이동한다. 구역 이동 후 같은 NPC에게 돌아온다. | 디더와 외곽선 LOD가 서로 재질을 덮어쓰지 않는다. 근접에서 원본 복구 실패, 숨긴 장비 등장, NPC 실종이 없어야 한다. |
| LOD-04 | 동일 위치·카메라 방향에서 LOD 설정 활성화/비활성화별 정지 30초 및 동일 마을 이동 동선을 비교한다. | F10/F12 프레임 기록과 화면 품질을 별도로 전달한다. 정지 표본의 절감이 이동 중 끊김으로 상쇄되면 실패. 전환 순간 스파이크도 기록한다. |

회신 형식: `항목 ID / 통과·실패·미수행 / 실제 결과 / 첨부 또는 로그`. 실루엣·접근 전환·실제 대화 품질과 60FPS는 아직 확인되지 않았다.

## 렌더링 외 병목 조사

### 측정 범위

2026-10-25 후속 조사. 런타임 코드·설정 변경 없이 Boot → 기존 SceneManager → LakeOfLifeScenario로 진입했다. 로딩 완료 후 PC 품질, 2560×1440, 플레이어 `(1118.37, 51.79, 391.17)`, 카메라 `(1118.37, 54.11, 387.10)`, 회전 `(14, 0, 0)`에서 측정했다. NPC 외곽선 LOD는 기존 설정을 유지했다. 새 통합 월드 `LakeVillageWorld`의 성능 측정은 아니다.

- 기준 표본 2회, 원거리 그래프 임시 중단 1회, 복구 후 1회를 각각 120프레임 수집했다. 도구 실행 직전 마지막 5프레임을 제외했다. Deep Profile은 껐다.
- CPU 측정과 별도로 allocation callstack을 켜고 10프레임의 할당 호출 경로를 수집했다. 이후 명시적으로 껐으며 해당 구간은 CPU 비교에 사용하지 않았다.
- Main Thread의 `GC.Alloc` 중 PlayerLoop 하위만 집계했다. EditorLoop 및 다른 스레드의 할당까지 포함한 프로세스 전체 메모리 수치가 아니다. 표의 ms는 하위 호출을 포함하므로 부모·자식 마커를 더하지 않는다.
- 실제 이동·전투·대화 입력, 장시간 GC 추이, 디스크 로딩·새 월드 스트리밍은 측정하지 않았다. 정지 표본을 이동 성능이나 제품 FPS로 해석하지 않는다.

### 확인된 CPU 비용

| 항목 | 기준 1 | 기준 2 | 해석 |
|---|---:|---:|---|
| PlayerLoop | 17.825ms | 17.849ms | 렌더링을 포함한 게임 루프 |
| 렌더링 CPU | 7.736ms | 7.705ms | 비교용. 이번 조사 대상 밖 |
| MagicaManager | 1.628ms | 1.603ms | 옷감 처리와 동기화 비용. 주요 비렌더링 후보 |
| KCCSimulator.FixedUpdate | 1.041ms | 1.004ms | 활성 모터의 이동·접지·충돌 처리 포함 |
| DirectorUpdateAnimationBegin | 0.861ms | 0.845ms | 원거리 정지 그래프 평가도 남음 |
| DirectorUpdateAnimationEnd | 0.305ms | 0.307ms | 애니메이션 후처리 |
| GameManager.Update | 0.678ms | 0.669ms | AI·게임 서비스 전체 집계. AI만의 비용은 분리되지 않음 |
| ConstraintManagerUpdate | 0.316ms | 0.311ms | 기존 거리 정지 이후 남은 비용 |
| PhysicsFixedUpdate | 0.213ms | 0.210ms | KCC 쿼리와 구분되는 물리 루프 |

기준 1의 `Physics.OverlapCapsuleNonAlloc`은 0.256ms/프레임, 약 75회/프레임이며 `Physics.CapsuleCastNonAlloc`은 0.163ms, 약 43회다. KCC와 포함 관계가 있다. FixedUpdate는 약 1.19회/렌더 프레임이므로 KCC 1.04ms를 물리 스텝 1회의 비용으로 읽지 않는다.

런타임에서 거리 관리 대상 71개 중 45개가 정지 상태였고, 등록 KCC 모터 72개 중 실제 시뮬레이션 대상은 27개였다. 따라서 원거리 AI·물리가 모두 계속 실행되는 상황은 아니다. 다만 KCC의 `PreSimulationInterpolationUpdate`, `PostSimulationInterpolationUpdate`, `CustomInterpolationUpdate`는 전체 등록 모터를 순회한다. 이 잔여 순회의 **개별 비용은 미분리**이며, 충돌 정확도나 보간을 희생하면서 먼저 줄일 근거는 없다.

`WaitForJobGroupID` 약 1.93ms는 여러 시스템에 포함되는 대기·작업 실행 마커이므로 별도 병목 1.93ms로 합산하지 않는다. 기준 1의 `Profiler.FlushMemoryCounters` 약 0.88ms와 EditorLoop 4.91ms 역시 게임 코드 최적화 목표와 구분한다.

### 반복 GC 할당의 실제 원인

기준 표본의 PlayerLoop 하위 할당은 **16,749 / 16,619B/프레임**이었다. 기준 표본 240프레임에서는 수거 마커를 관측하지 않았다. 이는 장시간 GC 멈춤이 없다는 뜻이 아니다. 그래프 임시 중단 표본에서는 수거 마커가 1회 있었으며, 해당 프레임 PlayerLoop는 20.327ms였다. 이번 자료만으로 GC가 현재 프레임 지연의 주원인이라고 판정하지 않는다.

| 경로 | 할당/프레임 | 코드에서 확인한 원인과 수정 방향 |
|---|---:|---|
| ActorAnimator.LateUpdate → MotionEventExecutor | **8,640B, 108회** | `CollectEventsInRange`의 캡처 정렬 콜백 4,104B, `ProcessEvents`의 비교 델리게이트 3,456B, `MotionSetEventDebugOverlay.Publish`의 인터페이스 열거 경로 1,080B. 비교자 재사용·필요할 때만 정렬·진단 소비자가 있을 때만 상태 수집을 우선 검토 |
| PlayerAbilityResourceView.Update | **3,172B, 115회** | 남은 쿨다운만 읽어도 `TryGetPlayerSlotState`가 Ability 해석·활성화 평가·패시브 배율 계산까지 수행. `AbilitySetSO.GetPlayerAbility`의 새 HashSet, `PartyManager.GetAllPassives`의 새 List/HashSet, 자원 ID 변환의 `Enum.ToString()`이 호출 스택에 확인됨 |
| CameraManager.SyncCameraContext | **1,024B, 8회** | Update와 LateUpdate에서 인스턴스 메서드 4개를 델리게이트로 매번 대입. 콜백을 한 번 연결하거나 재사용 가능 |
| GameManager.Update 내 기타 경로 | 위와 별도 소규모·가변 | BT 상태 객체 생성과 디버그 문자열 등. 위 카메라 Update 할당도 GameManager 집계에 포함되므로 중복 합산 금지 |

MotionSet 8,640B는 27개 실행기의 반복 할당과 맞아떨어진다. 정지 기준 표본에서 `ActorAnimator.LateUpdate` 자체 CPU는 약 0.105ms이므로, 할당 제거만으로 수 ms 또는 60FPS를 확보한다고 예상하지 않는다. 다만 화면·옷감 품질을 낮추지 않고 제거할 수 있는 매 프레임 비용이며 액터 수 증가 시 누적된다.

추가 확인 사항:

- `EnemyActionResolver.TryTransition`은 `CreateState` 이후에 같은 상태인지 확인한다. 유지 중인 상태에 대한 반복 요청도 객체를 만든 뒤 버릴 수 있다. 실제 생성 경로는 할당 스택에 있으나, 이번 표본의 모든 생성이 불필요한 요청이었다고 단정하지 않는다.
- `SyncEnemyMemoryService`의 디버그 문자열 할당도 관측됐다. `BehaviorTreeContext.DebugTrace`는 DebugMode가 켜진 경우에만 반환되므로 전체 BT에 무조건 발생하는 비용이 아니다. 측정 조건에서 일부 디버그 추적이 활성화된 영향이다.
- 쿨다운 조회를 가볍게 할 때도 AbilitySet 단일 소스, 파티 변경·패시브 변경에 따른 갱신, 스킬 준비 상태의 즉시 반영을 유지해야 한다. 전투 판정 전체를 낮은 주기로 갱신하는 방식은 권장하지 않는다.

### 원거리 애니메이션 그래프 분리 실험

정지 액터 45개의 Animancer 그래프는 `Speed == 0`이지만 `IsGraphPlaying == true`였다. `ActorAnimator.SetSimulationPaused`가 시간을 멈추고 그래프 평가는 유지하는 코드와 일치한다. 같은 위치·카메라에서 이 45개에만 `PauseGraph()`를 임시 적용하고 측정한 뒤 모두 `UnpauseGraph()`로 복구했다.

| 항목 | 기준 2 | 임시 그래프 중단 | 복구 후 |
|---|---:|---:|---:|
| AnimationBegin + AnimationEnd | 1.152ms | **0.710ms** | 1.206ms |
| PlayerLoop | 17.849ms | 17.786ms | 18.317ms |
| 렌더링 CPU | 7.705ms | 7.866ms | 7.954ms |
| EditorLoop | 4.758ms | 4.867ms | 16.489ms |

애니메이션 구간에서 약 0.44ms 감소를 관측했고, 복구 후 원래 수준으로 돌아왔다. 반면 전체 PlayerLoop 이득은 0.063ms에 그쳤다. NPC·날씨·에디터 부하가 변하므로 전체 성능 개선량을 확정하지 않는다. 복구 표본은 EditorLoop와 스파이크가 커 전체 프레임 A/B에서 제외한다. 최종 조회에서 **45/45 복구, 실험으로 정지한 그래프 잔류 0**을 확인했다.

영구 반영은 하지 않았다. 그래프 정지를 적용하려면 대화 활성 임대, 모델 교체, 재등록, MotionSet 이벤트·루트모션 재개와 옷감 자세를 검증해야 한다. 화면 안의 원거리 인물이 부자연스럽게 굳는 문제도 별도 시각 검수 대상이다. 옷감 115개 전체를 끄거나 품질 설정을 낮추는 실험은 수행하지 않았다.

### 후속 우선순위와 미검증 항목

1. **반복 할당 제거:** MotionSet 정렬·진단, 카메라 콜백, 쿨다운 조회 및 패시브/자원 ID 경로 순으로 수정한다. 동작을 유지한 상태에서 GC/프레임을 전후 비교한다.
2. **원거리 그래프 평가 중단:** 약 0.44ms 구간 절감 근거가 있으나 복구·시각 품질 검증 후 반영한다.
3. **옷감 LOD 조사:** 약 1.6ms가 남는다. 현재 Keep 컬링과 실제 보이는 옷감의 비용을 분리하고 대화·접근 시 복구를 보장한다.
4. **이동·전투 표본 확보 후 KCC/AI 판단:** 정지 표본만으로 물리 주기·AI 반응 주기를 낮추지 않는다. 새 통합 월드의 스트리밍 비용도 별도 측정한다.

아래는 사용자 테스트 대기다. 기존 PC 품질·해상도·저장 상태와 게임패드 조건을 유지하고 Profiler에서 Deep Profile을 끈다. CPU 수치는 allocation callstack을 끈 구간으로 비교한다.

| ID | 실행 절차 | 확인 기준 / 전달 정보 |
|---|---|---|
| CPU-01 | 기존 PERF-02의 마을→물가→전투 동선을 같은 방향으로 2회 반복하고 F12 기록 및 CPU Timeline을 수집한다. | 느린 프레임의 KCC/Physics·GameManager·애니메이션·MagicaManager·GC 구간을 구분. 시작 위치, 적 수, 입력 장치, 해당 프레임 첨부. 이 조사만으로 이동 성능 통과 처리하지 않음 |
| GC-01 | 로딩 완료 후 동일 위치에서 3분 대기하고 다시 이동한다. 할당 호출 경로가 필요할 때만 callstack을 별도 구간에서 켠다. | GC 수거 프레임과 체감 멈춤이 겹치는지 기록. 호출 스택 수집 오버헤드·도구 호출 프레임과 게임 자체 스파이크를 구분 |
| WORLD-CPU-01 | 새 통합 월드에서 지역 경계를 왕복하며 첫 방문과 재방문의 CPU Timeline을 각각 기록한다. | 로딩·오브젝트 활성화·메모리 할당·AI/물리 비용을 구분. 기존 단일 LakeOfLifeScenario 결과로 통합 월드 성능을 판정하지 않음 |

이번 작업은 조사·문서 갱신이다. 새 테스트나 런타임 수정이 없어 EditMode를 반복 실행하지 않았다. Play Mode 조사 중 Unity 컴파일 실패 없음 및 Console 오류 0을 확인했다. 종료 시 Profiler·allocation callstack·Play Mode를 끄고 시작 시 열려 있던 `LakeVillageWorld.unity`로 복귀했으며 dirty=false를 확인했다. 종료 직후 연결 끊김 1회는 재조회로 복구 상태를 확인했다. 씬·프리팹·SO 저장, Player/Addressables 빌드, 커밋은 수행하지 않았다. 기존 사용자 에셋 변경은 보존했다.

근거: `Logs/LakePerformance/nonrender-baseline-1.json`, `nonrender-baseline-2.json`, `nonrender-allocation-stacks.json`, `nonrender-graphs-paused.json`, `nonrender-restored.json`, `nonrender-runtime-audit.json`. CPU JSON의 `frames`는 프레임별 값, `markers`는 상위 200개 마커 집계, `allocationPaths`는 주요 할당 부모 경로다. 전체 raw 프로파일이나 모든 스레드 기록을 대체하지 않는다.

## 후속 반영: 반복 할당 제거와 원거리 그래프 정지

### 구현

사용자의 개선 요청에 따라 위 조사에서 확인한 반복 할당과 원거리 그래프 평가를 수정했다.

- **MotionSet:** 실행 순서 비교 델리게이트와 실행기별 시작 시각 비교자를 재사용한다. 0~1개 이벤트는 시작 시각 정렬을 생략한다. 단발성 에디터 호출의 기본 정렬과 실행 순서는 유지하며, 진단 상태 게시에는 구체 HashSet 열거를 사용해 열거자 박싱을 제거했다. 이벤트·히트·캔슬 타이밍 데이터는 바꾸지 않았다.
- **카메라:** 매 Update/LateUpdate에 생성하던 컨텍스트 콜백 4개를 재사용한다. 콜백은 기존 카메라 인스턴스를 계속 참조하며, 실제 전투 상태와 입력은 호출 시 읽는다.
- **쿨다운:** `ActorAbilitySystem.GetPlayerCooldownRemaining`으로 남은 시간 조회를 분리했다. `PlayerAbilityResourceView`의 매 프레임 조회는 활성화 조건·타깃·Variant·패시브 배율을 재평가하지 않는다. 실제 발동 가능 여부와 전체 슬롯 상태 조회는 기존 경로를 유지한다. PlayerActor 참조도 Awake에서 보관한다.
- **공용 조회:** AbilitySet 슬롯 탐색의 방문 집합을 Unity HashSetPool에서 빌리고 반환한다. 상속·Override·순환 방지 의미는 유지한다. 파티 패시브 합성 버퍼는 재사용하되 매 호출 현재 데이터로 다시 채워 변경을 즉시 반영한다. 자원 ID 대조의 Enum.ToString은 nameof 상수로 바꿨다.
- **거리 정지:** `ActorAnimator`가 속도뿐 아니라 Animancer 그래프 평가도 멈춘다. 자신이 정지시킨 그래프와 원래 재생 여부를 기록하고, 활성 임대·접근·등록 해제에서 기존 경로로 복구한다. 원래 중단된 그래프는 임의 재생하지 않는다. 보조 Animator에 전파하고, 정지 중 모델 교체·재활성화에는 새 그래프에도 적용한다. 비활성화·파괴에서 소유한 그래프 상태를 반환하며, 기존 루트모션 버퍼 정리와 MotionSet Seek 복구를 유지한다.

새 매니저·액터 Update·설정 SO는 추가하지 않았다. 테스트 어셈블리에만 Animancer 참조를 추가했으며 런타임 asmdef 경계는 바꾸지 않았다. 옷감 시뮬레이션 품질·거리 임계값·AI 및 물리 갱신 주기는 유지했다.

### 자동 검사와 실제 씬 상태 검사

- Unity 6000.3.21f1 컴파일 최종 오류 0, Play Mode 조사 및 종료 시 Console 오류 0.
- EditMode **98개 통과, 실패/건너뜀 0**. 신규 `NonRenderingPerformanceTests` 9개와 MotionTimelineResolver, AbilitySetComposition, GameplayAbilitySystem, PassiveModifierCalculator, ActorSimulationPolicy, ActorSecondaryMotionSimulation 기존 검사를 포함한다. job: `1b5e9482d24c4312a2eed0f93e808243`.
- 신규 검사는 준비 후 모션 유지 평가(활성 이벤트 없음/있음), 슬롯 조회, 쿨다운 조회의 **100회 반복 할당 0B**, 모션 교체 후 이벤트 순서, 상속 순환 후 재조회, 본체/보조 그래프 복구, 원래 중단된 그래프 보존, 모델 교체, 비활성화 복구를 확인했다.
- 새 테스트 준비 중 참조·네임스페이스·추상 메서드 구현 누락을 수정했다. 초기 테스트 요청은 재컴파일 연결 종료로 시작하지 못했으며 최종 98개 통과 결과에 포함하지 않는다. 최종 실행된 검사에서 실패한 항목은 없다.
- 실제 LakeOfLifeScenario에서 대상 71개 중 정지 45개, **정지 그래프 45개, 정지 대상에 남은 재생 그래프 0개**를 확인했다.
- `RedTree_Guest`, `Npc_Daon`, `FarmWorker_0`의 `BeginDialogueStage` API를 열었을 때 셋 모두 그래프가 즉시 재개됐다. 해제 후 강제 상태 주입 없이 기존 거리 정책으로 셋 모두 정지에 복귀했고 활성 임대 잔류도 없었다. 실제 대사 UI·입력·화면 품질 검수를 대신하지 않는다.

### 재측정

위 조사와 같은 PC 품질·해상도·플레이어 위치·카메라 위치/회전에서 로딩 완료 후 120프레임씩 두 번 측정했다. Deep Profile과 allocation callstack은 끄고 끝 5프레임을 제외했다. 수정 전 기록은 이전 Play 세션이므로 전체 CPU의 엄밀한 단독 A/B는 아니다.

| 항목 | 수정 전 1 / 2 | 수정 후 1 / 2 |
|---|---:|---:|
| PlayerLoop 하위 GC 할당 | 16,749 / 16,619B/프레임 | **3,501 / 3,614B/프레임** |
| AnimationBegin + AnimationEnd | 1.166 / 1.152ms | **0.708 / 0.774ms** |
| PlayerLoop 전체 | 17.825 / 17.849ms | 17.068 / 18.867ms |
| 렌더링 CPU | 7.736 / 7.705ms | 7.801 / 8.366ms |
| Main Thread 전체 | 23.885 / 23.529ms | 21.345 / 23.449ms |
| EditorLoop | 4.905 / 4.758ms | 3.310 / 3.575ms |

- 두 표본 평균 기준 GC 할당은 **약 78.7% 감소**, 애니메이션 구간은 **약 36.1% 감소**했다. 새 표본에서 ActorAnimator.LateUpdate·PlayerAbilityResourceView.Update·GameManager.LateUpdate 하위 GC.Alloc은 관측되지 않았다.
- 수정 후에도 GameManager.Update 하위 약 2.5KB와 IMGUI 약 736B 등 할당이 남는다. 전체 게임 루프의 무할당을 의미하지 않는다. 수정 후 240프레임에 수거 마커는 없었지만 장시간 GC 스파이크 미발생을 보장하지 않는다.
- PlayerLoop 전체는 두 번째 표본에서 더 높았다. 렌더링·NPC·에디터 부하 변동을 포함하므로 전체 CPU/FPS 개선율을 확정하지 않는다. 옷감 약 1.6ms와 KCC 약 0.94~1.11ms도 남는다. **60FPS·이동·전투 성능 통과는 아니다.**

근거: `Logs/LakePerformance/nonrender-improvement-comparison.json`, `nonrender-improved-1.json`, `nonrender-improved-2.json`, `nonrender-improved-dialogue.json`, `nonrender-improved-runtime-audit.json`, `nonrender-implementation-tests.json`.

### 사용자 테스트 대기 — 이번 변경

기존 PERF-02, SIM-01/02, CLOTH-01, CPU-01, GC-01 조건을 유지한다. Editor에서 기존 호수 진입 경로와 같은 저장 상태를 사용하고 게임패드로 확인한다. 저장을 덮어쓸 필요는 없다.

| ID | 실행 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| GRAPH-01 | NPC·인간형 적에게 접근했다가 65m 밖으로 나간 뒤 55m 안으로 돌아온다. 두 번 반복하고 NPC 대화를 시작/종료한다. | 재접근·대화에서 몸·무기·옷감·루트모션이 복구되고 이벤트가 몰아서 발화하지 않는다. 굳은 자세 잔류·순간 이동·장비 분리·대화 중 정지는 실패 |
| GAS-PERF-01 | 스킬 3종을 사용하고 쿨다운 종료·자원 부족·캐릭터 교체 후 복귀를 확인한다. 가능한 경우 패시브 또는 스킬 성장 설정을 바꾼다. | 남은 시간·준비 표시·실제 사용 가능 상태가 일치하고 변경된 배율을 즉시 반영한다. 쿨다운 조기 해제·지연·표시 고착은 실패 |
| MOTION-PERF-01 | 일반 공격·연계·가드/회피 캔슬·피격 중단을 실행한다. | 히트박스·VFX·SFX·캔슬 창의 순서와 반응이 유지된다. 누락·중복 발화·복귀 후 선입력 불응은 실패 |
| CAMERA-PERF-01 | 카메라 수동 회전, 락온 전환, 대화 시작/종료를 반복한다. | 모드 복귀·정렬·수동 입력 반영이 기존대로 동작하고 카메라 고착이 없다 |

회신 형식은 `항목 ID / 통과·실패·미수행 / 실제 결과 / 첨부 또는 로그`다. 실패 시 NPC/캐릭터, 무기, 위치, 입력 장치와 해당 Console 오류 또는 짧은 영상을 함께 남긴다. 자동 검사는 위 시각·조작 확인을 대체하지 않는다.

종료 시 Profiler·Play Mode를 끄고 시작 씬으로 복귀했다. 씬·프리팹·SO는 저장하지 않았고 기존 변경 에셋 13개의 해시 일치를 확인했다. Player/Addressables 빌드와 커밋은 수행하지 않았다.
