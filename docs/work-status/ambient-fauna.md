# GPU 환경 생물 — 구현과 작업 상태

- 설계 입력: [정규 설계 v1.0](../전달용/ambient_fauna_design/01_환경생물_정규설계.md), [패키지 안내](../전달용/ambient_fauna_design/README.md)
- 프로젝트 정본: [CPU/GPU 역할·백엔드 ADR](../../Assets/docs/design/AMBIENT_FAUNA_ROLE_SEPARATION.md)
- 저작: [환경 생물 저작 지침](../../Assets/docs/guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md)
- 연결: [월드 위키](../wiki/world-art.md), [3종 공통 계약](../전달용/README.md)
- 현재 판정: **기능·셰이더·네 종 자산·정규 월드 연결 반영 / 기존 구조 EditMode 15/15·밀도 수정 후 PlayMode 2/2 통과 / 실제 TPS 품질·장시간 성능 사용자 테스트 대기**. G5 최종 인수와 Player 검증을 완료한 것은 아니다.

## 반영 범위 — 2026-11-12

| 항목 | 실제 반영과 정본 |
|---|---|
| CPU 역할 보존 | [WildlifeHabitat](../../Assets/02.Scripts/Gameplay/World/LivingWorld/WildlifeHabitat.cs)는 중첩 서식지의 개체/이웃 배열을 격리한다. 사냥·개별 생활·락온·드롭은 기존 CPU 계층에 유지한다 |
| GPU 이동·회피 | [AmbientFaunaGroup](../../Assets/02.Scripts/Gameplay/World/LivingWorld/AmbientFaunaGroup.cs), [Compute](../../Assets/06.Shaders/AmbientFauna/AmbientFauna.compute). GPU에서 개체 이동·접근/공격 회피·독립 위상을 처리한다 |
| 셰이더·출력 | [URP Shader](../../Assets/06.Shaders/AmbientFauna/AmbientFauna.shader), [공통 HLSL](../../Assets/06.Shaders/AmbientFauna/AmbientFaunaRender.hlsl), [RendererFeature](../../Assets/02.Scripts/Gameplay/World/LivingWorld/AmbientFaunaRendererFeature.cs). 양면 날개·몸통 분리, 노멀 회전, 알파·깊이/노멀, 근접 dither, 발광, 부유 입자 billboard |
| 월드 입력·예산 | [AmbientFaunaHabitat](../../Assets/02.Scripts/Gameplay/World/LivingWorld/AmbientFaunaHabitat.cs). 기존 플레이어/시간/날씨와 공격 시작 이벤트를 관측한다. 최대 8개 TTL 자극, 다중 서식지 공동 Capacity/그룹 예산에 퇴장·예약을 포함한다 |
| 수명·fallback | 거리/시간·날씨에 따른 fade와 실제 버퍼 반환, 일시정지·재할당·해제/재활성화. 실제 렌더 기능 확인 전에는 CPU 원본을 숨기지 않는다. 실패/중지 시 원래 활성 상태를 복구하고 중복 소유를 거절한다 |
| 저작 | [AmbientFaunaAuthoring](../../Assets/02.Scripts/Tool/Editor/Map/AmbientFaunaAuthoring.cs). 사람용 베이크만 툴 런처의 월드/맵에 노출한다. 일회성 생성·촬영·검증은 Catalog/MCP 전용이다 |

VFX Graph 기본값 대신 Compute + Indirect 단일 백엔드를 선택한 이유와 RenderGraph 출력 연결은 ADR-AF-01에 기록했다. 초기 직접 Graphics 제출의 렌더 비교 0픽셀은 실패로 처리하고 교체했다. 특정 Unity 이슈와 원인이 같다고 확정하지 않았다. 전역 원본 RP/Renderer를 수정하지 않고 씬 전용 복제 자산과 기존 SceneEnvironmentRendering을 사용한다.

비동기 에셋 로더·GPU 풀·대규모 Boids·포획/보상 권위는 추가하지 않았다. 직접 에셋 참조이며 정상 플레이 경로에는 GPU 위치 readback이 없다.

## 실제 자산과 월드 연결

- 기존 원본: Idyllic Fantasy Nature의 Butterfly_01 prefab/Butterfly FBX/날개 텍스처. 원본 66정점·68삼각형, 몸통/날개 submesh 2개와 UV/노멀을 조사하고 정적 메시로 파생했다.
- [제작 출처·GUID·localId·정규화 기록](../../output/AmbientFauna/GpuImplementation/asset-inventory.json). 정규화 계수 4.731775, 파생 결과 66정점·68삼각형. 원본 FBX·리그·텍스처·meta를 덮어쓰지 않았다. 구매 증빙/전체 라이선스 전수 감사는 미수행이며 원본의 외부 AI 업로드는 하지 않았다.
- [파생 메시 2개](../../Assets/08.Models/AmbientFauna/), [프로필·머티리얼 4종](../../Assets/10.Datas/World/AmbientFauna/), [프리팹 4종](../../Assets/03.Prefabs/Environment/AmbientFauna/), [독립 검증 씬](../../Assets/01.Scenes/Test/AmbientFaunaTest.unity). Gnat/Firefly는 작은 기존 나비 실루엣을 재사용하고 AirMote는 기술용 Quad를 사용한다. 신규 Blender/ImageGen 외형 제작은 없다.
- [씬 전용 렌더 프로필](../../Assets/10.Datas/World/AmbientFauna/Rendering/)은 PC_RPAsset 및 LakeVillage_RPAsset 원본의 복제본이다. 원본 설정·기존 렌더 기능을 보존하고 환경 생물 패스만 추가했다.
- [LakeVillageWorld](../../Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity)의 기존 장식 나비 17곳을 **GPU 그룹 17개 × Capacity 2 = 총 저작 용량 34**으로 연결했다. CPU 나비 원본은 삭제하지 않았다. 실제 동시 할당/표시 수는 거리·시간·날씨에 따라 달라진다.
- [연결 결과](../../output/AmbientFauna/GpuImplementation/world-integration.json), [Unity 재로드 검사](../../output/AmbientFauna/GpuImplementation/world-validation.json): CPU WildlifeCreature 104개 보존, fallback 대상 17개, GPU 그룹 17/17 준비 유효, 신규 루트 Missing Script 0.
- 변경 전 씬과 meta는 [백업](../../output/AmbientFauna/GpuImplementation/LakeVillageWorld.before-gpu-fauna.unity)으로 보존한다. 씬 본문 150,836,308 bytes(약 143.85 MiB). 백업 대비 저장본은 1,417줄 추가·1줄 제거이며 신규 GPU 루트/그룹과 씬 렌더 프로필 참조 변경을 확인했다.

본편에는 나비 전환을 연결했다. 다른 세 종은 프로필·프리팹·검증 씬을 제공하며 전 지역에 임의로 추가 배치하지 않았다. 환경 프리팹/씬 재생성 후에는 기존 저작 지침에 따라 GPU 배치를 다시 베이크·연결한다.

## 검증 근거 — 최초 구현과 후속 조정 구분

실행 환경은 Unity 6000.3.21f1 / URP 17.3 / Direct3D11 / RTX 3060 Ti이며 Compute/Instancing 지원을 실조회했다. 아래는 해당 Editor에서 확인한 결과다.

| 검사 | 실제 결과와 근거 |
|---|---|
| 최초 구조·EditMode | 밀도 조정 전 **15/15 통과, 실패 0·스킵 0**. [원본 JSON](../../output/AmbientFauna/GpuImplementation/editmode-final.json), job a7ad895b6bbc439aa9e4fcd0c7819472 |
| 검사 범위 | [역할 경계](../../Assets/Tests/EditMode/LivingWorld/FaunaRoleSeparationTests.cs), [데이터·예산·fallback 검사](../../Assets/Tests/EditMode/AmbientFauna/AmbientFaunaDataTests.cs). 네 프로필/셰이더·메시 채널·48 byte stride·자정/NaN·0 앵커/stale bake·예약·다중 서식지 예산·자극 중복/상한·실제 렌더 전 CPU 유지·사냥 개체 전환 거절 |
| Editor PlayMode | 밀도 조정 후 **2/2 통과**, 실패·스킵 0. [상세 원본](../../output/AmbientFauna/VisualTuning/playmode.json), job 990be84a7ba449f2ad5e6313c9d39eb0. 이전 도메인 복원 후 result:null이던 job과 구분한다 |
| 실제 GPU 상태 | [PlayMode 검사 코드](../../Assets/Tests/PlayMode/AmbientFauna/AmbientFaunaGpuTests.cs), [실행 결과](../../output/AmbientFauna/GpuImplementation/gpu-lifecycle.json). 32→128 재할당, 유한/안전 구 상태, 중심 중첩 회피, 일시정지 상태 불변, 개체 수에 따른 CPU GameObject/Animator 증가 없음, 호스트/그룹 해제·재활성화 |
| 실제 색상 출력 | PlayMode 나비 Off/On **1,066픽셀 차이**. [측정](../../output/AmbientFauna/GpuImplementation/render-check.json), [On](../../output/AmbientFauna/GpuImplementation/butterfly-on.png), [Off](../../output/AmbientFauna/GpuImplementation/butterfly-off.png) |
| 다른 외형 출력 | Editor RenderGraph 요청에서 반딧불 **2,198픽셀**, 부유 입자 **1,982픽셀** 차이. [반딧불](../../output/AmbientFauna/GpuImplementation/firefly-on.png)/[측정](../../output/AmbientFauna/GpuImplementation/firefly-render-check.json), [부유 입자](../../output/AmbientFauna/GpuImplementation/airmote-on.png)/[측정](../../output/AmbientFauna/GpuImplementation/airmote-render-check.json). AI가 실제 이미지를 확인했다 |
| 정규 월드 데이터 | Unity에서 저장본 재로드·17개 그룹 준비/참조와 신규 루트 Missing Script 검사 통과. 실제 Boot→월드 이동·전투 입력 통과를 대신하지 않는다 |
| 미수행 | Player/Addressables 빌드, 대상 기기 장시간 CPU/GPU p95/p99 측정, 실제 TPS 미술·락온/경고 가독성 인수. 별도 사용자 빌드 지시가 없어 빌드하지 않았다 |

GPU readback은 위 PlayMode 검사에만 있다. 캡처는 독립 검증 씬의 조명/카메라 조건이며 실제 월드의 최종 미술/성능 합격이 아니다. 중간 Editor-only PlayMode 설정의 0건 실행과 MCP 초기화 timeout/연결 실패는 통과 근거에서 제외했다. 초기 입력 브리지 4/4 및 중간 13건 결과를 최종 변경본의 증거로 승계하지 않았다.

## 사용자 직접 테스트 명세

공통 준비: 변경본 Unity Editor에서 컴파일이 끝난 상태로 실행한다. 저장 초기화는 필요 없다. 평소 사용하는 키보드/마우스 또는 게임패드를 사용한다. 실패한 항목 ID·실제 증상·입력 장치·Console 로그/필요한 화면만 전달한다.

| ID | 대상과 조작 | 기대 결과·실패 기준 | 판정 |
|---|---|---|---|
| AF-P1 | AmbientFaunaTest를 열고 Play. WASD/왼쪽 스틱으로 관측점을 군집 안팎으로 이동 | 나비·벌레가 접근을 피해 각자 다른 방향으로 움직이고 안전 구로 복귀. 동시에 같은 방향으로만 튀거나 원점에 뭉치면 실패 | 사용자 테스트 대기 |
| AF-P2 | Space/남쪽 버튼, T/N 또는 D-pad 위/아래, R/D-pad 오른쪽 | 공격 자극, 낮/밤 출현, 비의 밀도 감소가 동작. 발광이 날갯짓 속도로 번쩍이거나 부유 입자가 옆에서 사라지면 실패 | 사용자 테스트 대기 |
| AF-P3 | 일시정지·재개, 그룹/서식지 비활성화·재활성화 | 이동·날갯짓·발광 맥동이 정지/재개. 해제 뒤 disposed buffer·중복 등록·예외가 없어야 함 | 사용자 테스트 대기 |
| AF-W1 | 기존 Boot 진입 방식으로 LakeVillageWorld에 들어가 기존 나비 영역 왕복 | 낮에는 GPU 나비가 보이고 CPU 원본과 중복되지 않음. 밤/비에는 규칙대로 fade. 먼 거리 복귀·씬 재진입에 영구 소실/원점 스폰 없음 | 사용자 테스트 대기 |
| AF-W2 | 같은 영역에서 이동·대시·공격·락온 전투 | 적 실루엣·공격 경고와 조준을 장시간 가리지 않고 카메라 근접 시 자연스럽게 사라짐. TAA 잔상·과밀·큰 사각형/핑크 출력은 실패 | 사용자 테스트 대기 |
| AF-C1 | 기존 사슴·조류 등 CPU 생물에 접근 후 이탈하고 기존 사냥 경로 사용 | 경계·도주·귀환/착지·사냥/보상이 유지됨. 이중 Tick 같은 가속/떨림이나 새 서비스 예외 없음 | 사용자 테스트 대기 |
| AF-C2 | 저장본을 보호한 임시 실행에서 GPU 서식지/그룹을 끄고 다시 켬 | 기존 활성 CPU 나비가 복구되고 GPU 재개 뒤 중복 원본이 남지 않음. 사냥 개체가 GPU로 전환되지 않음 | 사용자 테스트 대기 |
| AF-A1 | 나비 근접/원거리·그늘, 야간 반딧불, 부유 입자 여러 각도 | 날개 root 이탈·몸통 찌그러짐·후면 검정·알파 halo·과노출이 없어야 함. 구매 환경과 외형이 어울리는지 사용자 판단 | 사용자 테스트 대기 |

회신: 항목 ID / 통과·실패·미수행 / 실제 결과 / 입력 장치·근거. 전체 품질 인수는 이 결과와 대상 기기 성능 측정 후 판정한다.

## 보존과 단계 인계

- 구매 원본·파생 메시/머티리얼/프로필/프리팹·scene·meta와 검증 JSON/최종 캡처를 보존한다. Git 추적 밖 산출물도 인수 범위다.
- 변경 전 월드 백업은 사용자 미커밋/추적 밖 씬 복구를 위해 보존한다. 다른 작업의 에셋·기존 InitTestScene07609c79…와 기존 사용자 변경은 정리하지 않는다.
- 테스트 런너의 추가 임시 씬은 종료 후 남지 않았음을 확인했다. 중간 수동 draw 캡처·진단 텍스트와 최신 PlayMode 캡처에 대응하지 않는 중간 픽셀 측정은 정리했다.
- 준비표: [초기 범위](../../output/harness/20261112-204814-8910c192/review.md), [역할 분리 단계](../../output/harness/20261112-203357-5ef0431d/review.md). 실제 산출물과 검증 근거는 이 문서와 분야 위키를 기준으로 한다.


## 사용자 화면 피드백 후 밀도 조정 — 2026-11-12

- 사용자 제공 본편 화면에서 큰 주황색 나비가 좁은 범위에 몰리는 문제를 확인했다. 이전 자동 출력 검사는 이 미술 문제를 잡지 못했다.
- 나비 프로필 날개 폭을 0.14~0.21m에서 0.06~0.10m로 줄이고, 본편 17개 그룹의 Capacity를 각각 8→2(총136→34)로 낮췄다. CPU fallback17개와 CPU 원본104개, 안전 영역·배치 좌표는 유지한다.
- 새 나비 프리팹 기본값도 2개, 검증 씬은 비교용6개로 맞춘다. 생성 코드도 같은 설정으로 갱신한다. 실제 에셋·화면 재검증을 마쳤다.
- 작은 벌레/Gnat·반딧불/Firefly·부유 입자/AirMote도 구현되어 있으나 현재 본편 배치는 나비다. Gnat/Firefly는 작은 기존 나비 메시를 재사용하며 독립 종 외형 제작 완료를 뜻하지 않는다.
- 이전 15/15·2/2와 world-validation의136은 밀도 수정 이전 기록이다. 현재 변경본의 통과 근거로 자동 승계하지 않는다.- 수정 후 [PlayMode 2/2](../../output/AmbientFauna/VisualTuning/playmode.json) 통과, Console 오류 0. 수정 후 [월드 재검사](../../output/AmbientFauna/VisualTuning/world-validation.json): 17그룹·Capacity34·CPU104·fallback17·잘못된 그룹0·Missing Script0.
- 동일 카메라·시뮬레이션 시간의 Editor 비교 [변경 전](../../output/AmbientFauna/VisualTuning/before.png)/[변경 후](../../output/AmbientFauna/VisualTuning/after.png): 표시용 테스트 그룹32→6, 폭14~21cm→6~10cm. Off/On 차이 픽셀11,861→1,083(약91% 감소). 실제 본편 같은 시점의 전후 화면 비교나 FPS 개선 측정은 아니다. 본편 그룹은 비교용6개보다 적은2개다.
- [인계·산출물 기록](../../output/AmbientFauna/VisualTuning/review.md). 기존 안전 구를 넓히거나 군집 이동 알고리즘을 바꾸지 않고 저작 밀도와 실루엣 크기를 조정했다. 본편 전투 가독성·장시간 성능·최종 미술 인수는 사용자 테스트 대기다.