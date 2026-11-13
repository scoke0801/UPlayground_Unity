# GPU 환경 생물 — 구현과 작업 상태

- 설계 입력: [정규 설계 v1.0](../전달용/ambient_fauna_design/01_환경생물_정규설계.md), [패키지 안내](../전달용/ambient_fauna_design/README.md)
- 프로젝트 정본: [CPU/GPU 역할·백엔드 ADR](../../Assets/docs/design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md)
- 저작: [환경 생물 저작 지침](../../Assets/docs/guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md)
- 연결: [월드 위키](../wiki/world-art.md), [3종 공통 계약](../전달용/README.md)
- 현재 판정: **종별 전용 외형·행동·본편23그룹/Capacity56·셀 수명·베이크/제외/stale·품질/화면 예산·지형 변경 억제 구현 반영. 비가시 자원 수정 후 현재 EditMode22/22·PlayMode5/5 통과. 실제 TPS/야간 미술·Addressables 지역 왕복·실기 장시간 성능은 사용자 테스트 대기.** 최신 근거는 [비가시 자원 검증](#비가시-자원-검증--2026-11-13)을 따른다.

## 근거리 소멸 사용자 피드백 — 2026-11-13

- 사용자 실패: 멀리서는 작아서 잘 보이지 않는데 가까이 가면 빠르게 사라져 버그처럼 보인다. 실제 사용자 관찰이며 이전 자동 검사 통과로 덮지 않는다.
- 확인한 원인: `AmbientFaunaHabitat.Budget.EstimateOccupancy/SelectBudget`는 거리가 줄수록 추정 점유율을 키운 뒤 상한을 넘은 군집 전체를 탈락시킨다. 기존 Medium0.1%에서 FOV60/16:9/나비2마리/반경0.7m 한 군집의 카메라 중심거리3m 추정은0.269%라 탈락한다. FOV52에서는5m에서도 한 군집이0.1035%로 탈락한다. 정상 렌더러가 있으므로 CPU fallback도 다시 켜지지 않아 실제 소멸처럼 보일 수 있다. 이 증상과 카메라 바로 앞의 근접 fade는 구분한다.
- 반영: `Assets/10.Datas/World/AmbientFauna/Quality.asset`의 ScreenFraction만 Low0.04/Medium0.06/High0.08로 조정했다. 최대 크기·경계 상자의 보수적 추정 한도이며 실제 화면 픽셀 비율을 뜻하지 않는다. Capacity256/512/1024, 그룹12/24/32, 종별 크기/개체수, 접근 회피, 거리/시야 밖 반환, 카메라 근접 fade는 유지했다. C#·셰이더·씬·프리팹 변경은 없다. 신규 Quality SO 생성 기본값은 별도이며 본편은 기존 저작 SO를 사용한다.
- 보조 검증: 열린 Unity에서 저장된 SO를 실제 로드하고 기존 비공개 `EstimateOccupancy`를 호출했다. 격리된 Preview Scene의 나비2마리·반경0.7m 군집을 세 개 합산한 보수적 조건으로 FOV52/55/60 × 중심거리15/10/7/5/3/2/1.2/0.5m × 품질3단계 **72조건 통과**. 최악3.805%로 Low4% 안에 들어온다. 실제 프레임/픽셀 렌더 또는 플레이 입력 검증은 아니다. 이전 BudgetCamera는 finally로 복구하고 Preview Scene을 닫았다. [수치 원본](../../output/AmbientFauna/ApproachBudget/verification.json).
- 첫 검사 코드는 internal setter 접근으로 메모리 컴파일에 실패했고 실행되지 않았다. reflection setter로 교정한 최종72조건만 결과로 센다. EditMode Test Runner 실행 요청은 다른 진행 중 검사의 `tests_running/busy` 응답으로 시작되지 않았다. 이 작업의 EditMode/PlayMode 재실행 통과를 주장하지 않는다. C# 변경이 없어 소스 재컴파일은 요청하지 않았다. Player/Addressables 빌드·커밋도 하지 않았다.
- **AF-C1 / 사용자 테스트 대기:** 본편 낮·맑음·비전투 상태에서 기존에 사라지던 나비 군집으로 걸어가고 뒤로 물러난다. 10m→5m→3m→근접 관찰 중 군집 전체가 예산 때문에 꺼지지 않아야 한다. Low/Medium/High에서 반복하고 접근 회피와 구분한다. 카메라 중심0.3~1.2m의 기존 점진 fade는 유지된다. 실제 화면에서 여전히 갑자기 사라지면 당시 날씨·전투 여부·품질과 발생 위치를 전달한다. 회신 형식: `AF-C1 / 통과·실패·미수행 / 실제 결과 / 필요 시 영상`.
- 남은 범위: 원거리 작은 실루엣 자체의 가독성, 좁은 화각/세로 화면/더 많은 군집이 겹치는 조건, 실제 화면 미술·성능은 미확인이다. 원거리 문제를 임의 크기 확대로 덮지 않았다. 검증 JSON·하네스 준비표는 근거로 보존하고 임시 씬/오브젝트는 해제했다.

## 현재 배치 조사 — 2026-11-13

사용자 요청으로 열린 `Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity`의 컴포넌트와 저장된 SO·배치/Compute 코드를 읽어 대조했다. Unity는 편집 모드이고 조사 전후 씬 dirty=false다. Play·베이크·자동 검사·씬 저장은 실행하지 않았다. 이전 테스트 결과를 이번 조사 결과로 승계하지 않는다.

- 루트 `AmbientFauna` 아래 Butterfly 17그룹/Capacity34, Gnat 2/8, Firefly 2/6, AirMote 2/8, 합계23/56이다. CPU WildlifeCreature104개 중 나비17개는 각 GPU 그룹의 fallback으로 보존되어 있으므로 동시 생물 수에 중복 합산하지 않는다. 환경 컨트롤러 참조는 연결되어 있다.
- 나비는 그룹당 원점 앵커1개·안전 구 반경0.7m·Capacity2다. 월드 XZ 기준 (1078,437), (1042,463), (1034,698), (1058,710) 부근에 각각3그룹, (1089.53,470.35), (1042,478.62), (1291.99,1436.33), (1695,820), (1312.53,67.35)에 각각1그룹이다.
- Gnat_WetReeds_A/B의 XZ는 (1642,771)/(1630,797), Firefly_LakeMeadow/WetReeds는 (1039,699)/(1636,787), AirMote_Shade/Reeds는 (1045,465)/(1628,812)다. 여섯 그룹은 모두 Box5m·ManualCount·UseSurface=true이며 명시 Exclusions는0개다. 정적 충돌체 기반 안전 공간 검사는 별도로 사용한다.
- 해당 여섯 그룹은 지표 위0.9m(곤충)/1.6m(부유 입자)에서 안전 구 반경0.3m/0.45m로 베이크되어 있다. 앵커는9~11개지만 Capacity는3~4다. Compute의 `id % _AF_AnchorCount`로 각 개체의 앵커를 고정하므로 현재 할당에서는 앞쪽3~4개 앵커만 사용하며 앵커 사이를 순회하지 않는다.
- 현재 종 SO는 활성35m/반환45m, Fade0.8초, 최대 비에서 밀도0.1배·최대 눈에서0배다. 나비는08~17시 밀도1,20~05시0; 반딧불은20~05시1,07~17시0이다. 전투 시 서식지 밀도는0.25배다. Capacity56은 동시 화면 표시 수가 아니다.
- 관찰 판단: 특정 지점의 작은 국소 서식지 위주이며 월드 전역 생태 분포/이동 경로를 생성하는 방식은 아니다. 실제 플레이 시 가시성·자연스러움·장소별 밀도는 이번 조사에서 확인하지 않았다. 코드·게임 에셋 변경과 임시 산출물은 없다.

## 반영 범위 — 2026-11-12 (이전 단계 기록)

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
- 제작 출처·GUID·localId·정규화 기록. 정규화 계수 4.731775, 파생 결과 66정점·68삼각형. 원본 FBX·리그·텍스처·meta를 덮어쓰지 않았다. 구매 증빙/전체 라이선스 전수 감사는 미수행이며 원본의 외부 AI 업로드는 하지 않았다.
- [파생 메시 2개](../../Assets/08.Models/AmbientFauna/), [프로필·머티리얼 4종](../../Assets/10.Datas/World/AmbientFauna/), [프리팹 4종](../../Assets/03.Prefabs/Environment/AmbientFauna/), [독립 검증 씬](../../Assets/01.Scenes/Test/AmbientFaunaTest.unity). Gnat/Firefly는 작은 기존 나비 실루엣을 재사용하고 AirMote는 기술용 Quad를 사용한다. 신규 Blender/ImageGen 외형 제작은 없다.
- [씬 전용 렌더 프로필](../../Assets/10.Datas/World/AmbientFauna/Rendering/)은 PC_RPAsset 및 LakeVillage_RPAsset 원본의 복제본이다. 원본 설정·기존 렌더 기능을 보존하고 환경 생물 패스만 추가했다.
- [LakeVillageWorld](../../Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity)의 기존 장식 나비 17곳을 **GPU 그룹 17개 × Capacity 2 = 총 저작 용량 34**으로 연결했다. CPU 나비 원본은 삭제하지 않았다. 실제 동시 할당/표시 수는 거리·시간·날씨에 따라 달라진다.
- 연결 결과, Unity 재로드 검사: CPU WildlifeCreature 104개 보존, fallback 대상 17개, GPU 그룹 17/17 준비 유효, 신규 루트 Missing Script 0.
- 변경 전 씬과 meta는 백업으로 보존한다. 씬 본문 150,836,308 bytes(약 143.85 MiB). 백업 대비 저장본은 1,417줄 추가·1줄 제거이며 신규 GPU 루트/그룹과 씬 렌더 프로필 참조 변경을 확인했다.

본편에는 나비 전환을 연결했다. 다른 세 종은 프로필·프리팹·검증 씬을 제공하며 전 지역에 임의로 추가 배치하지 않았다. 환경 프리팹/씬 재생성 후에는 기존 저작 지침에 따라 GPU 배치를 다시 베이크·연결한다.

## 검증 근거 — 최초 구현과 후속 조정 구분

실행 환경은 Unity 6000.3.21f1 / URP 17.3 / Direct3D11 / RTX 3060 Ti이며 Compute/Instancing 지원을 실조회했다. 아래는 해당 Editor에서 확인한 결과다.

| 검사 | 실제 결과와 근거 |
|---|---|
| 최초 구조·EditMode | 밀도 조정 전 **15/15 통과, 실패 0·스킵 0**. 원본 JSON, job a7ad895b6bbc439aa9e4fcd0c7819472 |
| 검사 범위 | [역할 경계](../../Assets/Tests/EditMode/LivingWorld/FaunaRoleSeparationTests.cs), [데이터·예산·fallback 검사](../../Assets/Tests/EditMode/AmbientFauna/AmbientFaunaDataTests.cs). 네 프로필/셰이더·메시 채널·48 byte stride·자정/NaN·0 앵커/stale bake·예약·다중 서식지 예산·자극 중복/상한·실제 렌더 전 CPU 유지·사냥 개체 전환 거절 |
| Editor PlayMode | 밀도 조정 후 **2/2 통과**, 실패·스킵 0. 상세 원본, job 990be84a7ba449f2ad5e6313c9d39eb0. 이전 도메인 복원 후 result:null이던 job과 구분한다 |
| 실제 GPU 상태 | [PlayMode 검사 코드](../../Assets/Tests/PlayMode/AmbientFauna/AmbientFaunaGpuTests.cs), 실행 결과. 32→128 재할당, 유한/안전 구 상태, 중심 중첩 회피, 일시정지 상태 불변, 개체 수에 따른 CPU GameObject/Animator 증가 없음, 호스트/그룹 해제·재활성화 |
| 실제 색상 출력 | PlayMode 나비 Off/On **1,066픽셀 차이**. 측정, On, Off |
| 다른 외형 출력 | Editor RenderGraph 요청에서 반딧불 **2,198픽셀**, 부유 입자 **1,982픽셀** 차이. 반딧불/측정, 부유 입자/측정. AI가 실제 이미지를 확인했다 |
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
- 준비표: 초기 범위, 역할 분리 단계. 실제 산출물과 검증 근거는 이 문서와 분야 위키를 기준으로 한다.


## 사용자 화면 피드백 후 밀도 조정 — 2026-11-12

- 사용자 제공 본편 화면에서 큰 주황색 나비가 좁은 범위에 몰리는 문제를 확인했다. 이전 자동 출력 검사는 이 미술 문제를 잡지 못했다.
- 나비 프로필 날개 폭을 0.14~0.21m에서 0.06~0.10m로 줄이고, 본편 17개 그룹의 Capacity를 각각 8→2(총136→34)로 낮췄다. CPU fallback17개와 CPU 원본104개, 안전 영역·배치 좌표는 유지한다.
- 새 나비 프리팹 기본값도 2개, 검증 씬은 비교용6개로 맞춘다. 생성 코드도 같은 설정으로 갱신한다. 실제 에셋·화면 재검증을 마쳤다.
- 작은 벌레/Gnat·반딧불/Firefly·부유 입자/AirMote도 구현되어 있으나 현재 본편 배치는 나비다. Gnat/Firefly는 작은 기존 나비 메시를 재사용하며 독립 종 외형 제작 완료를 뜻하지 않는다.
- 이전 15/15·2/2와 world-validation의136은 밀도 수정 이전 기록이다. 현재 변경본의 통과 근거로 자동 승계하지 않는다.- 수정 후 PlayMode 2/2 통과, Console 오류 0. 수정 후 월드 재검사: 17그룹·Capacity34·CPU104·fallback17·잘못된 그룹0·Missing Script0.
- 동일 카메라·시뮬레이션 시간의 Editor 비교 변경 전/변경 후: 표시용 테스트 그룹32→6, 폭14~21cm→6~10cm. Off/On 차이 픽셀11,861→1,083(약91% 감소). 실제 본편 같은 시점의 전후 화면 비교나 FPS 개선 측정은 아니다. 본편 그룹은 비교용6개보다 적은2개다.
- [인계·산출물 기록](../../output/AmbientFauna/VisualTuning/review.md). 기존 안전 구를 넓히거나 군집 이동 알고리즘을 바꾸지 않고 저작 밀도와 실루엣 크기를 조정했다. 본편 전투 가독성·장시간 성능·최종 미술 인수는 사용자 테스트 대기다.
## 후속 구현 — 2026-11-13 (단계 기록)

- 조사 단계 완료: 지정 정규 설계·검증표·역할 ADR·저작 지침을 실제 Compute/그룹/서식지/저작기와 대조했다. 종별 행동·캡슐 위협·표면/볼륨 베이크·제외·기하 stale·품질/우선순위/화면 예산·명시 지역 준비 연결이 남아 있었다.
- 보유 자산 조사: 기존 나비 원본은 보존. PolyartStudio의 `T_Insect.png`와 `M_Insect.mat`는 실제 사각 마스크로 곤충 실루엣에 부적합. `firefl` 검색은 프로젝트 임시 Firefly와 Fireball flipbook만 반환했다. 이를 최종 종 외형으로 승격하지 않는다.
- Blender 직접 MCP에서 5.2.2 LTS 연결 확인. 전용 `AF_Work_Insects` 씬/컬렉션에 날벌레 58삼각형·반딧불 82삼각형을 제작했다. [원본·제작 코드](../../ArtSource/AmbientFauna/)와 FBX는 보존. Unity 축·채널 검증은 다음 단계다. 처음 export가 기본 Cube를 선택한 문제를 임포트 검사로 발견하여 전용 씬 컨텍스트로 재수출했다. 잘못된 중간 FBX를 최종 결과로 사용하지 않는다.
- 작업 준비표: [review](../../output/harness/20261113-113950-003d1334/review.md). 기존 CPU104·fallback17·나비17그룹/34개·폭6~10cm를 보존 기준으로 고정한다. 다른 작업의 반응형 환경/Hot Reload 변경이 동시에 있으므로 그 파일을 되돌리지 않는다.
- 구현 진행 중: 종별 Compute와 캡슐 자극, `AmbientFaunaPlacement`, 품질 SO/예산 선택, 기존 `StreamedRegionContent` 준비·공개·퇴장 연결. 현재 변경본 테스트는 아직 미수행이다. 이전 통과를 승계하지 않는다.
### 구현·자산·본편 배치 단계 반영

- 전용 Unity 메시 임포트 확인: Gnat 146정점/58삼각형, Firefly 206정점/82삼각형, 단일 submesh·폭1m·UV1/UV2·색상 보존. Firefly 발광 정점은 복부60/206개로 한정된다. 실제 폭은 Gnat1.2~2cm, Firefly2.5~3.5cm다. [ADR-AF-02/03](../../Assets/docs/design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md)에서 정점 색 저작과 지역/베이크/예산 대체 결정을 기록했다.
- 본편 추가 배치 저장: 나비17/34 유지, 날벌레2/8·반딧불2/6·부유 입자2/8을 젖은 갈대·호수 초지·그늘에 추가했다. [현재 Unity 검사](../../output/AmbientFauna/Completion/world-validation.json): 그룹23, Capacity56, CPU104, fallback17, Missing Script0, 오류0. 신규 안전 앵커는 표면 Raycast/경사/여유 공간 검사를 통과했다.
- [본편 날벌레 화면](../../output/AmbientFauna/Completion/world-Gnat_WetReeds_A-on.png), [본편 반딧불 서식지 화면](../../output/AmbientFauna/Completion/world-Firefly_LakeMeadow-on.png)은 실제 본편 지형에서 fauna 시각/바람을 주입한 Editor RenderGraph 출력이다. 씬 조명은 그대로이므로 후자를 실제 야간 플레이 인수로 해석하지 않는다. 캡처 JSON에 입력·좌표·용량을 남겼다.
- [독립 Firefly 출력](../../output/AmbientFauna/Completion/firefly-on.png), [Gnat 출력](../../output/AmbientFauna/Completion/gnat-on.png), [AirMote 출력](../../output/AmbientFauna/Completion/airmote-on.png)과 시간차 t1~t3 프레임을 생성했다. 각각4개 기준 실제 색상 차이는144/21/91픽셀이었다. 작은 곤충은 나비용 픽셀 문턱30으로 출력 없음 판정을 받았으나 실제 출력21픽셀을 확인했으므로 0픽셀 검출로 수정했다. 크기·수를 늘려 통과시키지 않았다. 확대 FOV10의 macro 검수는 정상 TPS 화면과 구분한다.
- 확장 EditMode19/19의 [초기 결과](../../output/AmbientFauna/Completion/editmode-initial.json)를 보존했다. 이후 프로파일링/진단/수명 보완이 있어 최종 재실행 예정이다. PlayMode는 100회 억제/재활성화·32→128·일시정지·출력 검사를 실행했으나 MCP 종료 상세 result:null 문제로 최종 XML 보존 후 다시 판정한다. 진행0에서 막힌 중간 실행은 취소했고 통과 근거가 아니다.
- Editor 독립 씬 CPU marker120프레임은 평균0.142ms/p95 0.190ms/p99 0.236ms였다. [원본 CSV·맥락](../../output/AmbientFauna/Completion/editor-cpu-samples.csv), [요약](../../output/AmbientFauna/Completion/editor-cpu-summary.json). 단기 Editor 자체 marker이며 본편 전체 비용·GC0·GPU 시간·장시간/실기 성능 합격은 아니다.
### 수명·저작 검증 보완

- 지역 비활성화는 새 예약/자극을 거절하고 TTL·관측 속도·임시 전투 억제를 지운다. 다시 켤 때 과거 공격이 재생되지 않는다. 생산자별 sourceId+sequence 중복 제거와 서로 다른 source의 같은 sequence 허용을 검사했다.
- [Undo/Redo·누락 프로필 실제 Editor 검사](../../output/AmbientFauna/Completion/editor-undo-validation.json)는 모두 true다. 임시 additive 씬을 닫고 사용자 Undo 전체를 지우지 않았다. [누락 프로필 오류 원본](../../output/AmbientFauna/Completion/negative-profile-validation.json)을 정상 본편 검사와 분리했다.
- 본편 복원 뒤 AirMote_Shade의 stale 경고를 확인했다. Editor 베이크/해시 조회 전에 Physics.SyncTransforms를 호출하도록 보완하고, 6개 신규 배치를 재베이크한 뒤 실제 저장→씬 재로드→Unity 재검사를 실행했다. [재로드 검사](../../output/AmbientFauna/Completion/scene-reload-validation.json)는23그룹/56·CPU104/fallback17·Missing Script0·stale0이다. 새 지형을 변경하지 않았다.
- [보존 대조](../../output/AmbientFauna/Completion/cpu-preservation.json): 직접 직렬화 CPU17개와 해당 루트/컴포넌트 문서 변경0, 기존 prefab instance19,792개 변경0. 다른 CPU는 prefab 인스턴스를 통해 로드되므로 텍스트 수17을 CPU 전체104로 오인하지 않는다. Unity 실제 로드 수104를 별도로 대조했다.
- [아트 인수 manifest](../../output/AmbientFauna/Completion/art-manifest.json)65파일·meta 누락0. 보유 후보 검색·실제 GUID는 [asset-search](../../output/AmbientFauna/Completion/asset-search.json), [Blender 원형 화면](../../ArtSource/AmbientFauna/blender-review.png)과 [Unity Gnat 확대](../../output/AmbientFauna/Completion/gnat-macro-on.png)/[Firefly 확대](../../output/AmbientFauna/Completion/firefly-macro-on.png)를 연결한다. 확대 화면은 FOV10·1개·표시밀도1의 외형 검사이며 TPS 밀도 합격 근거가 아니다.
## 요구사항별 충족표 — 후속 구현 판정

`구현 반영`은 코드·에셋 연결을, `자동 확인`은 아래 범위만 실제 실행했다는 뜻이다. 최종 플레이·미술·대상 기기 성능 인수는 별개다.

| 요구사항 | 구현 반영·실제 근거 | 남은 확인·판정 |
|---|---|---|
| FR-01 보유 에셋 조사 | asset-search의 실경로/GUID, T_Insect 실제 픽셀 확인, 나비 원본 유지, 전용 곤충 제작 결정 | 후보 검색/파생 결정 확인. 라이선스 전수 감사는 미수행이며 외부 AI 업로드 없음 |
| FR-02 데이터 기반 종/변형 | 종별 Behavior·속도·호버·회복·시간·날씨·외형 SO, Butterfly_AmberSoft 데이터 변형 추가·프로필 검사 | 자동 확인. 변형은 본편 미배치 |
| FR-03 범위/높이/제외 | Box/Sphere·표면 Raycast·경사·Clearance·간격·제외 bounds, 안전 구 내 GPU 제한, 실제 본편6곳 베이크/재로드 검사 | 자동 기초 확인. 동굴·문·높은 경사·최악 시야의 장시간 관찰은 사용자 대기 |
| FR-04 접근/대시/공격 | 몸체/전방 캡슐 최근접점, 속도 반응, source+sequence·TTL·우선순위/overflow, 교전·락온 밀도 억제 | GPU 중심 중첩/퇴화 캡슐·유한 상태 자동 확인. 실제 입력 감각·경고/락온 가독성은 사용자 대기 |
| FR-05 월드 수명 | StreamedRegionContent 준비 억제→완료 공개, ContinuousWorldRegion 언로드 선반환, CPU fallback 복구, 지역 종료 입력/예약 거절 | GPU API100회 억제/재활성화·버퍼0·구독0 확인. 실제 Addressables 지역 왕복은 미수행 |
| FR-06 시간/날씨/품질 | 기존 시간/날씨, Settings qualityIndex 연동, 점진 fade·품질 예산, Firefly 낮/밤/비 뒤 버퍼 반환 검사 | 자동 확인. 본편 실제 야간/강수 조명과 설정 UI 조작은 사용자 대기 |
| FR-07 작업자 도구 | Placement/서식지 Inspector·Gizmo·베이크·검사·예산/버퍼/점유 추정, 설정/기하 stale, Undo/Redo·누락 프로필 오류 | 실제 Editor 확인. 점유 수치는 보수적 추정이며 정확한 GPU visible 수 아님 |
| FR-08 외형 생산 | Blender 원본·FBX·채널 JSON·Unity 파생 메시·머티리얼·meta·확대/원래 크기 출력, ADR-AF-02 정점 색 경로 | Unity 출력 확인. 새 ImageGen 비트맵은 필요 없다고 판단해 미사용. 최종 미술 인수 대기 |
| NFR-01 개체 CPU 객체/readback 금지 | 32→128에서도 객체/Animator 증가0, 정상 루프는 GPU 상태만 유지, readback은 검사 코드에만 존재 | 코드·GPU 자동 확인 |
| NFR-02 GC/성능 | Ambient Fauna Tick·Editor120프레임 CSV/요약 제공 | GC0B, GPU 시간, 본편 전투 Off/On·60초×3·p95/p99 실기 예산은 미수행. 성능 합격 아님 |
| NFR-03 실제 예산 상한 | 공동 예약·퇴장 합산, 품질 Tier/그룹/화면 추정 상한, Priority/거리 선택, 한도 축소 실제 반환 | 예약/품질·다중 호스트 자동 확인. 최악 카메라/전투 스트레스는 사용자 대기 |
| NFR-04 원본/GUID/렌더 보존 | CPU104/fallback17, 직접 CPU17 직렬화·원래 prefab19,792개 변경0, 구매 원본·meta 유지, 씬 전용 RP 복제 | Unity 검사+텍스트 보존 비교. 다른 작업 변경은 원복하지 않음 |
| NFR-05 사실 기반 보고 | NUnit XML/JSON·캡처·CPU raw CSV, NOT_RUN/사용자 대기 분리 | 아래 미수행 표와 테스트 명세 참조 |

### 검증표 ID별 한계

| 검증 ID | 현재 증거 수준 |
|---|---|
| T01~02 | 실제 버전/디바이스·후보 조사 기록 |
| T03~05, T07~10, T14 | 밀도 단위/시간/베이크 재현/예약·캡슐/중복·stride/잘못된 데이터 자동 검사 |
| T06 | 거리 히스테리시스 구현. 실제 경계 왕복은 AF-W1 사용자 대기 |
| T11~12, T15 | Unity 저장/재로드·stale, 실제 Undo/Redo·미리보기/임시 씬 정리 확인 |
| T13 | 본편6곳 표면 베이크·제외 기초 검사. 실내/동굴/모든 경사 조합은 미수행 |
| T16, T19, T24 | 실제 GPU32→128·낮/밤/비·일시정지 확인. 0.25/2배 정량 비교는 미수행 |
| T17~18 | 접근·캡슐·자극 API/유한 상태/TTL·생산자 중복 검사. 실제 대시/공격/폭발 영상·전투 감각은 사용자 대기 |
| T20~23 | 독립 버퍼/셀 수명/텔레포트 방어 구현. 동일 SO 두 지역 동시 GPU 대조·실제 Addressables 전환·텔레포트·180도 관찰은 미수행 |
| T25~26 | 기본 Editor 진입·해제와100회 API 수명 확인. Domain Reload On/Off 쌍·실제 보조 카메라 수량 대조는 미수행 |
| T27~28 | 정상 코드 readback 없음, 밀도와 실제 Capacity 구분, 0밀도 버퍼 반환 확인 |
| T29 | 직접 Compute Capacity 재할당/그룹 반환 채택(ADR-AF-03). 자동 풀 용량 클래스/교차 fade는 도입하지 않음 |
| T30~32 | 안전 구/Bounds·음수 scale/누락 검사. 벽 앞 장시간·모든 frustum 모서리·Compute 미지원 장치 실제 실행은 미수행 |
| T33~34 | 코드 없는 색상 변형·16/32/128 GPU 상태와 개체 인덱스 상한·0밀도 해제 확인 |
| T35 | Boids 미도입. v1 범위 밖이며 PASS로 표시하지 않음 |
| A01~13 | Blender/Unity 형상·채널·실제 출력 일부 확인. 모든 거리/후면/AA·최종 TPS 스타일은 사용자 대기 |
| A14, P01~08 | Player/Addressables 빌드 미수행(정책). 전체 성능 시나리오는 미수행. 단기 Editor CPU marker만 별도 측정 |

## 남은 구현과 사용자 테스트 대기

- 현재 경로는 ADR-AF-01/02/03의 Compute·정점 색·보수적 안전 구·직접 용량/그룹 반환을 구현했다. VFX Graph, GPU 풀/비동기 에셋 로더, 개체별 compact/cull 버퍼, Boids, 동적 GPU 제외/SDF/height field는 중복 도입하지 않았다. 이 미채택 항목을 구현 완료로 세지 않는다.
- 정밀한 동적 회피·자동 재베이크는 없다. 대신 기존 정적 Collider 변경 감시와 Terrain 높이맵 콜백으로 해당 군집을 자동 무효화해 재베이크 전까지 억제한다. 새 Collider 삽입/같은 Mesh 내부 변형 생산자는 NotifyGeometryChanged(worldBounds)를 호출해야 한다. 아직 존재하지 않는 신규 생산자의 연동까지 완료했다고 세지 않는다.
- 실제 Boot→본편 진입, 지역 스트리밍 왕복, 저장/재로드 동선, CPU 사냥/보상, 실제 입력·카메라·야간 미술·장시간/실기 성능은 아래 확장 명세에 따라 사용자 테스트 대기다. G5 전체 인수 완료를 선언하지 않는다.
## 확장 사용자 테스트 명세

준비: 현재 변경본 Unity6000.3.21f1/URP에서 평소 Boot 경로로 LakeVillageWorld 진입. 기존 저장본은 유지하며 저장 초기화·Player 빌드는 하지 않는다. 키보드/마우스 또는 실제 게임패드 사용. 아래 좌표는 탐색 위치이며 카메라를 강제로 고정하지 않는다. 필요한 경우 Play 중 Hierarchy에서 AmbientFauna 아래 명시 그룹을 선택해 위치/프로필/상태를 확인한다. Play 중 바꾼 검사 값은 저장하지 않는다.

| ID | 재현 절차 | 기대 결과·실패 기준 | 상태 |
|---|---|---|---|
| AF-W3 | Gnat_WetReeds_A(1642,77 부근,771), B(1630,80 부근,797)에서 멈춤→걷기→대시→공격→이탈 | 1.2~2cm의 약한 점/좁은 날개가 짧게 호버하다 흩어지고 복귀. 큰 검정 무리·나비 축소 실루엣·영구 소실이면 실패 | 사용자 대기 |
| AF-W4 | Firefly_LakeMeadow(1039,82 부근,699), Firefly_WetReeds(1636,79 부근,787)를 낮/밤/비에 비교 | 낮엔 없고 밤에 소수 출현. 복부만 독립 점멸하며 실시간 Light/큰 halo 없음. 비에 표시가 감소. 낮에도 밝은 떼·동시 점멸·날갯짓과 같은 빠른 발광이면 실패 | 사용자 대기 |
| AF-W5 | AirMote_Shade(1045,64 부근,465), AirMote_Reeds(1628,84 부근,812)에서 바람/근접/일시정지 비교 | 약한 바람 드리프트와 완만한 재배치·fade, 가까운 입자는 억제. 불투명 큰 사각형·정지 중 날개/발광만 진행하면 실패 | 사용자 대기 |
| AF-W6 | 같은 영역에서 실제 적 락온→공격 경고/스킬→락온 해제 | 교전/락온 중 밀도가 줄고 적 실루엣·경고·조준을 가리지 않음. 경고보다 더 강하게 빛나거나 화면을 장시간 가리면 실패 | 사용자 대기 |
| AF-W7 | 설정 UI 품질 최저↔최고, Habitat Inspector의 할당/그룹/점유 추정 확인 | 최저256/12, 중간512/24, 높음1024/32 상한 이내. 소규모 배치이므로 품질을 바꿔도 개체 수 차이가 반드시 보이는 것은 아님 | 사용자 대기 |
| AF-W8 | 기존 LakeVillageWorld↔DroughtVillageRegion 연결부를 왕복하고 월드 종료/재진입 | 거리에 따라 자원 반환/복귀, 지역 준비 중 조기 공개 없음. disposed buffer·중복 구독·원점 스폰·CPU 중복이면 실패. 자동100회 소유 API 검사와 실제 Addressables 왕복은 별개 | 사용자 대기 |
| AF-G1 | 독립 검증 씬의 복사본에서 베이크 후 기존 정적 Collider 이동/비활성화, 또는 Terrain 높이 일부 수정 | 영향 그룹이 Faulted/IsGeometryInvalidated로 중단되고 버퍼 반환. 껐다 켜는 것으로 재개하지 않음. Play 종료 뒤 재베이크하면 복구. 원본 본편 Terrain을 편집해 검사하지 않음 | 사용자 대기 |
| AF-A2 | 원래 크기의 3종·입자를 TPS/락온/대시/그늘/후면/밤에서 관찰 | 나비6~10cm·기존34 유지, 날벌레/반딧불 전용 몸통과 막날개, 미술 스타일/노멀/AA/안개/잔상/과밀 허용 여부를 사용자 판정 | 사용자 대기 |
| AF-L1 | Domain Reload On/Off 각2회, 보조 Game 카메라 추가 후 위 경로 재현 | 같은 프레임 이중 이동 없음, 지형 구독은 활성1/비활성0, 공격 구독0~1, 종료 자원0 | 사용자 대기 |
| AF-PF1 | 지정 기기의 실제 전투·스킬/식생·180도 회전·텔레포트·셀 경계에서 동일 경로 Off/On, warm-up 뒤60초 이상3회 | fauna CPU/GPU·GC/메모리·p95/p99/최댓값을 측정한 뒤 합의 예산으로 판정. 단기 Editor marker/픽셀 차이로 통과시키지 않음 | 미수행·대상 기기 인수 대기 |

회신: `항목 ID / 통과·실패·미수행 / 실제 결과 / 입력 장치 / 화면·로그`. AF-C1/C2의 기존 CPU 사냥·보상과 fallback 검사는 앞 명세도 함께 사용한다.
### 실행 중 지형 유효성 보완

- [그룹 Geometry 소유권](../../Assets/02.Scripts/Gameplay/World/LivingWorld/AmbientFaunaGroup.Geometry.cs)을 추가했다. 베이크할 때 같은 씬의 정적 Collider 참조·Matrix·Bounds·Mesh/Terrain 참조를 저장한다. 5Hz 예산 검사에서 변화를 발견하면 GPU와 fallback 소유권을 반환하고, 편집 모드 재베이크 전까지 Faulted/IsGeometryInvalidated를 유지한다.
- 기존 TerrainCallbacks.heightmapChanged의 수정 영역으로 군집을 즉시 억제한다. 지역 비활성/컴포넌트 종료에는 지형 구독도0으로 돌리고 활성 때1개만 연결한다. 새 충돌체 삽입이나 참조/Bounds가 같은 Mesh 변형은 NotifyGeometryChanged API를 제공한다. 정상 프레임에 Physics 탐색/GetComponent/LINQ를 추가하지 않았다.
- 지역 공개 직전 TryPrepare를 다시 실행해 기존 배치 로더 준비 이후의 실제 안전 공간도 검사한다. 자동 검사100회는 단순 Habitat API뿐 아니라 실제 StreamedRegionContent 소유 경로를 통해 수행하도록 보완했다.
- 정적 구조물 변경·복구/재베이크 검사 추가 후 EditMode22/22를 확인했다. 실제 TerrainData.SetHeights 호출과 이벤트 해제/재활성화가 포함된 PlayMode 결과를 최종 표에 기록한다. 테스트 DLL이 갱신되지 않았던 중간 실행은 새 테스트의 근거로 쓰지 않고, 명시 임포트 후 live reflection에서 신규 테스트 메서드를 확인했다.
### 실제 본편 실행의 읽기 전용 관측

실행 중이던 LakeVillageWorld에서 플레이어/시간/날씨를 바꾸지 않고 API로23그룹 Ready·기하 무효0, 지형 구독1·실제 플레이어 공격 구독1, 할당0을 관측했다. 이후 GameView 캡처 시도 전 Play가 종료되어 화면 파일은 생성되지 않았으며 런타임 화면 근거로 쓰지 않는다. 편집 모드 복귀 뒤에는 관련 자동 검증을 계속한다. 실제 화면 근거는 상태 주입한 Editor RenderGraph 캡처임을 유지한다.
## 최종 인계 — 2026-11-13

| 항목 | 실제 결과·근거 |
|---|---|
| 코드/셰이더 | 현재 Unity6000.3.21f1/URP17.3·D3D11 컴파일/출력, 마지막 Console Error 조회0. Compute+Indirect/RenderGraph ADR 유지 |
| EditMode | **22/22**, 실패0·스킵0. [전체 NUnit XML](../../output/AmbientFauna/Completion/editmode-final.xml), [MCP 상세 JSON](../../output/AmbientFauna/Completion/editmode-final.json). 정적 구조물 변화/재베이크·양산자 source+sequence·퇴장 TTL 초기화·밀도 단위·품질·CPU 역할/fallback 포함 |
| Editor PlayMode | **3/3**, 실패0·스킵0. [NUnit XML](../../output/AmbientFauna/Completion/playmode-final.xml), [상세 JSON](../../output/AmbientFauna/Completion/playmode-final.json). GPU16/32/128, 실제 지역 소유 경로100회·공개 직전 재검사, pause, 객체/Animator 수, callback 수명, Firefly 낮/밤·강수, 실제 TerrainData.SetHeights 후 억제/버퍼 반환 |
| 날씨 검사의 범위 | 버퍼0까지 확인하는 강수 검사는 시험 복제 프로필 RainDensity0 조건이다. 본편 프로필은 기본 RainDensity0.1로 **표시 감소와 같은 Capacity 유지**가 가능하다. 둘을 혼동하지 않는다 |
| 본편 저장/재로드 | [최종 재로드 검사](../../output/AmbientFauna/Completion/scene-reload-validation.json):23그룹·Capacity56·CPU104·fallback17·Missing Script0·stale/오류0. 나비17그룹/34·폭6~10cm 유지. 새 그룹은 Gnat2/8·Firefly2/6·AirMote2/8 |
| 보존 | [최종 보존 대조](../../output/AmbientFauna/Completion/cpu-preservation.json): 직접 CPU17 및 루트/컴포넌트 변경0, 기존 prefab19,792개 변경0. CPU 전체104는 Unity 로드 수로 별도 확인. 구매 원본·GUID/meta·CPU 사냥/보상 코드는 보존 |
| 아트 | Gnat58/Firefly82삼각형 전용 Blender 메시, Unity146/206정점·단일 submesh·폭1m·날개/복부 마스크 검사. [manifest](../../output/AmbientFauna/Completion/art-manifest.json)·[원형](../../ArtSource/AmbientFauna/blender-review.png)·[Gnat 확대](../../output/AmbientFauna/Completion/gnat-macro-on.png)·[Firefly 확대](../../output/AmbientFauna/Completion/firefly-macro-on.png) |
| Unity 출력 | 본편 [Gnat](../../output/AmbientFauna/Completion/world-Gnat_WetReeds_A-on.png)·[Firefly 서식지](../../output/AmbientFauna/Completion/world-Firefly_LakeMeadow-on.png)·[AirMote](../../output/AmbientFauna/Completion/world-AirMote_Shade-on.png). 이 세 장은 fauna 시간/바람을 주입하고 씬 조명을 유지한 Editor 출력이다. 독립 야간 [Firefly](../../output/AmbientFauna/Completion/firefly-on.png)와 시간차 t1~t3는 별도 근거다 |
| 픽셀 수의 범위 | 마지막 PlayMode 나비 Off/On747픽셀. 독립4개 기준 Gnat8·Firefly144·AirMote91픽셀. 출력 유무 지표이며 미술·성능 합격 아님. Gnat 수/크기를 늘려 문턱을 통과시키지 않음 |
| Editor CPU | fauna Tick marker120프레임 평균 **0.138ms**, p95 **0.194ms**, p99 **0.290ms**, 최대 **0.363ms**. [원본](../../output/AmbientFauna/Completion/editor-cpu-samples.csv)·[요약](../../output/AmbientFauna/Completion/editor-cpu-summary.json). 독립 검증 씬의 모든 fauna Tick 합계이며 본편/GPU/GC·실기 성능 인수 아님 |
| 미수행/사용자 대기 | 앞 FR/NFR 및 T/A/P 표와 AF-W3~AF-PF1 명세. 실제 입력·야간 최종 미술·락온/경고·Domain Reload On/Off·보조 카메라·실제 지역 스트리밍·CPU 사냥/보상·60초×3 성능/GC. Player/Addressables 빌드·커밋은 지시가 없어 실행하지 않음 |

새 매니저·VFX 병렬 경로·개체별 CPU 객체·정상 위치 readback은 추가하지 않았다. 정밀 동적 충돌/자동 재베이크·Boids·GPU 풀/개체 compact는 ADR의 미채택 항목이다. 새 Collider/같은 Mesh 내부 변형 생산자 도입 때는 NotifyGeometryChanged 호출을 연결해야 한다. 현재 설계 요구사항의 **구현 반영**, 자동 검사 통과, 전체 G5 인수는 서로 구분한다.

최종 준비표: [명시 산출물 review](../../output/harness/20261113-130031-12d33e7a/review.md). 임시 NUnit 씬은 종료 후 제거됐고 기존 InitTestScene07609c79… 원본은 보존했다. 새 의존성/빌드/대용량 캐시는 만들지 않았다. 사용자 미커밋 원본이 포함된 before-species 씬+meta 약143.9MiB, Blender 제작 원본/FBX/파생/meta와 XML/JSON/최종 캡처를 복구·인수 근거로 보존한다. 잘못된 캡처 시도의 live-world.json은 삭제했고 없는 GameView PNG를 근거에 연결하지 않았다.

최종 문서 로컬 링크136건의 대상 존재를 확인했다([결과](../../output/AmbientFauna/Completion/document-links.json)). 소스21파일의 GUID/meta는 [source-manifest](../../output/AmbientFauna/Completion/source-manifest.json)에 기록했고 누락 meta는0이다. 이 manifest와 CheckPlan은 실행 검증 자체를 대신하지 않는다.

## 궤도 무늬 사용자 피드백 후속 — 2026-11-13

사용자 검증 씬 화면에서 생물 군집이 무한대 기호처럼 보이는 실패를 접수했다. 정적 픽셀 출력 통과는 이 미술 실패를 검출하지 못했다. 원인은 공통 사인/코사인 목표 궤도와 같은 반경의 초기 고리 분포였고, 검증 씬의 종당64개가 무늬를 강조했다.

- 웹 조사: [실험실 날벌레의 3D 궤적 연구](https://pmc.ncbi.nlm.nih.gov/articles/PMC3545223/), [실측 궤적 데이터](https://www.nature.com/articles/sdata201936). 군집 내 낮은 방향 정렬과 개별 불규칙 운동을 참고했다. 생물 물리 모델을 그대로 재현했다는 주장은 하지 않는다.
- 구현: 개체별 hash 기반 3축 연속 목표 보간, 서로 다른 갱신 속도/시점/호버 신호, 3D 초기 부피 분포로 변경했다. 기존 종별 속도·회복·위협/안전 구·GPU stride48·날갯짓/점멸은 유지한다.
- 저작: 검증 씬 기본 Capacity를 나비2/날벌레4/반딧불3/입자4로 변경했다. 본편23그룹/56·나비 폭6~10cm·CPU104/fallback17은 변경하지 않았다.
- 검증 진행: GPU64개 초기 반경 분산 및3/6/9초 방향 정렬 회귀 검사를 추가했다. 이전22/3 통과를 이번 변경본 결과로 승계하지 않는다. 현재 실행 및 화면 확인 결과는 이 절 아래에 기록한다.

최종 검증 및 인계:

- 현재 shader 최종 로직으로 PlayMode **4/4 통과·실패0·스킵0**. job `fda78743f43243cca9ac03b1e336590a`, [NUnit XML](../../output/AmbientFauna/MotionRevision/playmode-final.xml). 기존 수명/지역100회/정지/재할당/출력·4종 행동 검사와 새64개 회귀 검사다. 종료 후 EOF 공백 정리만 했다. EditMode22개는 이번에 재실행하지 않았으며 이전 기록으로 구분한다.
- [초기 부피 분산](../../output/AmbientFauna/MotionRevision/initial-volume.json): 반경 표준편차0.1437m. [방향 정렬 CSV](../../output/AmbientFauna/MotionRevision/independent-motion.csv): 나비/날벌레/반딧불64개 각각3·6·9초의 정규화 평균 속도 방향 크기0.019~0.125(0=상쇄,1=모두 같은 방향). 이것은 글로벌 정렬의 회귀 지표이며 형태/미술 합격 수치가 아니다.
- Unity Editor 실제 RenderGraph 출력: [기본 낮](../../output/AmbientFauna/MotionRevision/preview-day.png), [fauna 시간22시](../../output/AmbientFauna/MotionRevision/preview-fauna-night.png), [날벌레64개](../../output/AmbientFauna/MotionRevision/gnat-stress64.png)와2초 간격 [t1](../../output/AmbientFauna/MotionRevision/gnat-stress64-t1.png)·[t2](../../output/AmbientFauna/MotionRevision/gnat-stress64-t2.png)·[t3](../../output/AmbientFauna/MotionRevision/gnat-stress64-t3.png). 다른 열린 씬은 카메라 layer로 제외하고 최종 촬영했다. 시간22시는 fauna 출현만 바꿨고 조명은 검증 씬 그대로이므로 야간 미술 근거가 아니다. AI가64개 출력에서8자/공통 선이 사라지고 공간에 흩어진 것을 확인했다. 최종 실제 움직임의 자연스러움은 사용자 판단 대기다.
- 본편 LakeVillageWorld 파일 SHA256 `773179985e5d4c9fee926676697dfaa39b51336d012ba8ee83709663aac6d5fe`가 이전 최종 인계와 동일하다. 본편/CPU/원본 에셋 변경 없음. [변경 범위 해시/meta](../../output/AmbientFauna/MotionRevision/source-manifest.json). Console 오류0 확인, diff 공백 검사 통과. 촬영의 임시 Capacity64·카메라·레이어·GPU 버퍼·등록은 복구/해제했다.
- 중간 MCP 재연결/Play 전환으로 시작하지 못한2회는 검증 결과에 포함하지 않았다. 처음 수동 촬영의 미연결 habitat/URP 설정을 교정한 뒤 최종 이미지로 교체했다. 이전3/3 XML은 원래 위치에 보존하고 이번4/4는 별도 폴더에 저장했다. 사용자 원본 화면은 [before](../../output/AmbientFauna/MotionRevision/user-before.png)에 보존했다. 이 근거는 정확히 같은 카메라/시간의 전후 비교가 아니다.
- 재현 **AF-V1**: AmbientFaunaTest를 Play하고10~20초 관찰한다. T/N/R로 낮/밤/비 전환, WASD 접근 및 Space 회피 후 복귀를 확인한다. 개체가8자/고리/공통 곡선에 정렬되면 실패, 급격한 순간 이동·동기화된 방향 전환도 실패다. 이번 수정의 사용자 테스트 대기이며 Player/Addressables 빌드·커밋은 미수행이다.

## 비가시 자원 검증 — 2026-11-13

사용자 요청으로 offscreen 자원 경로를 조사했다. 기존 `AmbientFaunaRendererFeature.AddRenderPasses`는 카메라 layer만 맞으면 모든 그룹에 렌더러 생존 신호를 줬고, frustum 검사는 실제 draw 단계에만 있었다. 따라서 가까운 화면 밖 그룹은 그리기0이어도 Compute 이동/버퍼가 유지될 수 있었다.

- 구현: 렌더러 연결과 실제 frustum+카메라 거리 가시성 신호를 분리했다. 첫 가시성 신호 전에는 할당하지 않고, 마지막 가시성 후2프레임 유예를 넘기면 Compute/버퍼/예약을 반환한다. 비가시 상태는 같은 빈 자원을 반복 해제하지 않는다. 예산 후보에서도 제외한다.
- CPU fallback: 정상 렌더러가 있는 단순 시야 이탈에서는 CPU 원본을 다시 켜지 않는다. 기능 해제/구독 해제/렌더러 부재에서는 기존 CPU 복구 계약을 유지한다. Play 중 SceneView가 군집을 깨우는 경로도 제외한다.
- 기존 CPU 야생 생물은 WildlifeCreature.Tick에서 SimulationDistance 밖이면 Animator/Animancer와 이동/NavMesh 추종을 멈추고 짧은 거리 판정만 남긴다. 사냥/피격·보상·화면 밖 근거리 행동은 gameplay 계약이므로 임의로 정지시키지 않았다.
- 범위: frustum/layer/거리/시간/셀/예산에 의한 억제다. frustum 안에서 벽에 가려진 그룹의 정확한 occlusion 판정은 추가하지 않았다. 보이지 않는 모든 픽셀에 비용0이라는 뜻이 아니며, 새 Physics raycast·GPU readback 프레임워크도 만들지 않았다.
- 검증 진행: 실제 카메라를 반대로 돌린 채 시작/재진입/30프레임 정지/일시정지 중 이탈/100m 거리 이탈/CPU 원본 보존을 검사하고 Editor 전용 Dispatch 카운터로0증분을 확인한다. 이전4/4 결과는 승계하지 않는다.

최종 결과:

| 검사 | 현재 변경본 근거 |
|---|---|
| 실제 카메라 비가시 수명 | [visibility.json](../../output/AmbientFauna/Visibility/visibility.json):32개 그룹, 최초 화면 밖 할당0B, 화면 안1572B, 화면 밖0B, 이후30프레임 Dispatch 증분0·재할당 반복0. 시야 재진입·일시정지 이탈·100m 이탈·카메라 layer 제외/복구·CPU fallback 유지/비활성 복구 통과 |
| PlayMode | **5/5·실패0·스킵0**, [최종 XML](../../output/AmbientFauna/Visibility/playmode-final.xml), job `a91b852e1c134ffe86a6b1f36bfc5d01`. 기존100회 지역/해제/재진입·종별 GPU·pause·출력·궤도 회귀도 재실행했다. 레이어 제외 때 CPU 원본이 켜지는 경로를 보완해 마지막으로 다시 실행했다 |
| EditMode | **22/22·실패0·스킵0**, [XML](../../output/AmbientFauna/Visibility/editmode-final.xml), job `dbb8f84206ec4648b4bd493f9f836a29`. CPU/GPU 역할·사냥 경계·fallback 복구·데이터/예산/기하 계약 재확인. 이후 renderer의 layer 신호 순서만 보완했고 해당 동작은 최종 PlayMode에서 검사했다 |
| 출력/컴파일 | 기존 실제 URP 색상 출력 검사 통과. 카메라 방향 전환 검사는 실제 Game 카메라 렌더 heartbeat로 수행했으며 테스트가 MarkVisible을 직접 호출해 통과시키지 않는다. 코드 컴파일/Console 오류0 확인, diff 공백 검사 통과 |
| 보존/범위 | [소스 해시·meta](../../output/AmbientFauna/Visibility/source-manifest.json). 런타임 Group/RendererFeature·Editor 캡처 bootstrap·PlayMode 회귀만 수정. 구매 원본/프로필/프리팹/본편 씬을 저장하거나 변경하지 않았다. 현재 본편 파일은 이전 턴 스냅샷과 해시가 달라 이전 보존 대조 결과를 이번 턴의 파일 동일성 근거로 승계하지 않는다. 다른 작업 변경은 보존한다 |

**AF-R1 사용자 확인:** 본편/검증 씬 Play에서 GPU 그룹 Inspector를 열고 카메라를 반대로 돌린다.2프레임 유예 후 AllocatedCapacity/BufferBytes0, 다시 보면 예산 주기+fade 뒤 복구되어야 한다. SceneView만 그룹을 보고 있는 경우에도 GPU는 깨어나지 않아야 한다. 시야 경계에서 카메라를 빠르게 흔들었을 때 팝인/재생성 품질은 사용자 테스트 대기다. 실제 보조 Game 카메라가 그룹을 볼 때의 활성 유지는 정상이다.

남는 비용은 호스트/등록 목록의 가시성·거리·예산 검사와5Hz의 정적 기하 검증 등 CPU 관리다. shared mesh/material/texture가 메모리에서 모두 제거된다는 뜻이 아니며, 측정0B는 그룹 전용 GPU buffer다. 벽 뒤 frustum 내부의 정밀 가림, 타깃 기기 GPU ms/FPS/장시간 성능은 미측정이며 이 기능의 근거로 합격 처리하지 않는다. Player/Addressables 빌드·커밋은 실행하지 않았다.

부산물: 원래 Completion의 과거 XML은 복원했고 현재 XML/측정/PNG는 Visibility에 별도 보존했다. 처음 소스 사본4개는 미커밋 선행 작업이 포함된 복구 자료라 보존한다. 테스트 생성 임시 씬의 종료 정리와 기존 사용자 InitTestScene 보존을 확인한다.
