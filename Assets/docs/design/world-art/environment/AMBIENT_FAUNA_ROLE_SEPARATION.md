# CPU 야생 생물과 GPU 환경 군집의 역할 분리

- 상위 정본: [GPU 환경 생물 정규 설계](../../../../../docs/전달용/ambient_fauna_design/01_환경생물_정규설계.md)
- 구현·검증: [환경 생물 작업 상태](../../../../../docs/work-status/ambient-fauna.md)
- 저작: [환경 생물 저작 지침](../../../guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md)

## 목적과 책임

큰 동물의 반응과 사냥 가능성을 유지하면서 작은 장식 생물의 밀도를 개체별 CPU 비용 없이 확장한다. 종의 이름이나 비행 여부로 실행 경로를 자동 선택하지 않는다.

| 구분 | CPU 야생 생물 | GPU 환경 군집 |
|---|---|---|
| 관리 단위 | WildlifeCreature 개체, WildlifeHabitat 일괄 갱신 | Habitat × Species × Material의 AmbientFaunaGroup |
| 개체 상태 | 위치·행동·체력·착지·피격·사망 | GPU StructuredBuffer |
| 게임 권위 | 기존 피해·락온·드롭 계약 유지 | 피해·락온·포획·드롭·퀘스트 권위 없음 |
| 월드 입력 | 기존 플레이어 서비스 | 같은 플레이어·시간 서비스, EnvironmentController |
| CPU 작업 | 개체 판단·이동·안전 검사 | 그룹 입력·시간/날씨 밀도·예산·자극 최대 8개 |
| 금지 | GPU 장식 개체를 CPU 배열에 생성 | 개체별 GameObject/Animator/Physics, 정상 경로의 위치 readback |

CPU 서식지는 가장 가까운 CPU 서식지의 생물만 Awake에서 수집한다. 중첩 서식지에서 부모가 자식 생물의 Tick과 이웃 배열을 덮어쓰지 않는다. 실행 중 CPU 계층 재배치는 지원하지 않는다.

## ADR-AF-01 — 프로젝트 실행 백엔드

프로젝트 구현은 **Compute + Indirect 한 경로**를 사용한다. 상위 설계 ADR-01의 VFX Graph 기본값에 대한 명시적 예외다. 설치된 VFX Graph 17.3의 VFXGraph, CustomHLSL 모델은 비공개 Editor API이며, 날개 정점 변형까지 저작하려면 ShaderGraph와 내부 직렬화 의존도 함께 생긴다. 패키지 내부 YAML을 생성하는 대신 ComputeShader/GraphicsBuffer와 직접 관리하는 URP HLSL을 선택한다. 출력은 AmbientFaunaRendererFeature의 RenderGraph 패스에서 CommandBuffer.DrawMeshInstancedIndirect로 제출한다.

이전 입력 브리지는 실제 GPU 수명을 소유하는 AmbientFaunaGroup으로 확장한다. VisualEffect 컴포넌트와 병렬 VFX 시뮬레이션은 두지 않는다. 기존 브리지에는 연결된 정규 VFX 에셋/씬이 없으므로 원본 VFX 데이터 이관은 없다. 직접 Graphics 제출은 선택한 검증 경로에서 색상 차이 0으로 실패했으며, 명시적 RenderGraph 연결을 채택한다. 이 결과만으로 특정 엔진 이슈와 원인이 같다고 단정하지 않는다.

렌더 연결은 원본 URP/Renderer를 복제한 씬 전용 자산에만 추가한다. 기존 SceneEnvironmentRendering이 이 프로필을 적용·복원한다. 원본 전역 Renderer·렌더 설정·패키지는 변경하지 않는다. 이미 연결된 복제 프로필을 재베이크해도 중복 복제하지 않는다.

## 데이터와 좌표 계약

AmbientFaunaProfileSO는 종 ID, 정적 메시, 머티리얼, ComputeShader, 크기·속도·날갯짓, 출현 시간 곡선, 비/눈 밀도, 거리·fade 설정만 소유한다. 개체 상태와 GPU 버퍼는 SO에 저장하지 않는다. 프로필 Revision과 그룹 Inspector 변경 시 버퍼를 반환하고 다시 검증·바인딩한다.

- GPU 개체 stride 48 bytes: positionSize / velocityPhase / headingSeed 각 float4.
- 앵커 stride 16 bytes: 그룹 로컬 중심 xyz와 안전 구 반경 w.
- UV0: 기존 BaseColor/Alpha, UV1: wingSide(-1/0/+1)와 flapWeight, UV2: hinge local xz.
- 몸통은 side/weight 0으로 유지하고 좌우 날개만 hinge 기준 Z축 회전한다. 노멀도 같은 각도로 변형한다.
- 고정 그룹의 world matrix와 단위 스케일을 베이크 시 저장한다. 좌표 변경·0 앵커·NaN·잘못된 반경은 실패로 처리한다.
- 구 안에서만 움직이므로 앵커 간 장애물 관통 경로를 만들지 않는다. 저작 도구는 정적 충돌체/물 아래 공간을 제외한 구를 선택한다. 지형·구조물 변경 후에는 재베이크가 필요하다. 완전한 동적 장애물 회피나 SDF 경로 탐색은 범위 밖이다.

원본 Idyllic Fantasy Nature 나비는 몸통/날개 submesh로 분리돼 있다. 정적 메시를 파생하고 날개 폭 1m로 정규화해 종 크기를 적용한다. 원본 FBX·리그·텍스처·meta는 보존한다. 벌레/반딧불은 Blender MCP 전용 원본에서 파생한 독립 메시를 사용한다. 부유 입자는 기술용 Quad/방사형 마스크를 사용한다. 외형 생산 예외와 출처는 ADR-AF-02를 따른다.

## 월드 입력과 수명

서식지는 IActorQueryService의 관측점, IGameTimeService의 시각, 명시적으로 연결된 EnvironmentController의 강수·바람을 읽는다. 서비스가 없는 독립 검증 씬에는 preview 입력을 쓴다. 실제 플레이어의 기존 PlayerCombat.OnAttackStarted를 관측하며 공격 판정이나 GAS/MotionSet을 바꾸지 않는다. 활성 플레이어 교체·서식지 종료 시 구독을 해제한다.

개체별 위치·회피·날갯짓 위상은 GPU가 갱신한다. Time.deltaTime을 제한된 substep으로 적분하고, deltaTime 0에서는 이동·날갯짓·fade·자극 TTL을 멈춘다. 관측점 순간이동은 속도 자극으로 확대하지 않는다. 공격 자극은 최대 8개·TTL·활성 sequence 중복 제거·overflow 계수를 갖는다.

등록 → Dormant → Active → Retiring → Dormant/Disabled 순서다. 그룹은 가까워질 때 실제 Capacity를 예약한 뒤 버퍼를 만든다. 퇴장 중에도 예약을 유지하며 fade 종료·서식지 중지·셀 비활성화·Play 종료에 버퍼와 예약을 반환한다. 먼 거리/밀도 0에서 실제 버퍼를 반환하는 것과, 표시 밀도만 줄여 같은 Capacity를 계속 시뮬레이션하는 것을 구분한다.

여러 서식지의 할당/예약은 공동 예산에 합산한다. 활성 서식지의 한도가 다르면 낮은 한도를 적용하며, 한도 축소 시 후순위 할당을 반환한다. 품질 SO의 단계별 Capacity/그룹/예상 화면 점유 상한을 적용하고 종 프로필 Priority와 카메라 거리로 5Hz에서 선택한다. Boids는 도입하지 않는다.

동기 직접 에셋 참조를 사용하므로 늦은 비동기 에셋 로드 콜백과 GPU 풀은 도입하지 않는다. generation은 진단 readback이 이전 할당의 완료를 적용하지 않도록 식별한다. 정상 경로에는 GetData/WaitForCompletion을 넣지 않는다.

시뮬레이션은 Update에서 그룹당 프레임에 한 번 수행한다. 카메라 콜백에서 적분하지 않아 보조 카메라 때문에 이중 이동하지 않는다. 렌더 패스는 카메라별 frustum과 그룹 Bounds로 거르고, 색상 및 깊이/노멀을 같은 GPU 상태에서 그린다. Bounds는 모든 앵커와 날개 크기를 포함한다. URP 셰이더는 양면 노멀·알파 클립·그룹 단위 앰비언트 프로브/주광·안개·독립 발광 맥동·카메라별 근접 dither를 처리한다. PC/Mobile Renderer의 실제 Deferred 설정에 맞춘 색상/깊이/노멀 패스에 같은 변형과 clipping을 쓴다. 개체별 실시간 그림자와 Motion Vector는 출력하지 않으므로 TAA 조건은 사용자 품질 검수에서 확인한다.

## CPU 원본 인계

GPU 전환 대상은 명시한 기존 장식 프로필 참조와 안전한 앵커가 있는 배치다. MaxHealth가 양수이거나 DropTable이 있는 CPU 생물은 대체하지 않는다. 실제 카메라의 출력 패스가 확인된 뒤 CPU 원본을 숨긴다. 렌더 기능 부재가 이어지면 GPU 버퍼를 반환하고 CPU 원본을 유지한다. 지원 불가·준비 실패·GPU 예외·그룹 중지 시에도 원래 active 상태를 복구한다. 거리/시간으로 GPU 개체가 잠시 사라졌다고 CPU 원본을 다시 표시하지 않는다.

CPU 원본과 GUID는 삭제하지 않는다. 한 원본의 중복 GPU 소유를 거절한다. 런타임 Inspector에서 fallback 참조가 바뀌어도 기존에 인계받은 원본을 정확히 복구하도록 준비 시 참조 배열을 별도로 보존한다.

이 인계는 기능 연결이며 미술 인수를 뜻하지 않는다. 실제 TPS 시야·락온 가독성·낮/밤 외형과 장시간 성능은 사용자 확인 및 지정 기기 측정으로 별도 판정한다.


## ADR-AF-02 — 작은 곤충의 전용 형상과 정점 색

Gnat/Firefly를 나비 축소본으로 쓰던 임시 외형을 폐기한다. 보유 PolyartStudio `T_Insect`는 사각 마스크이며 대응 생물 모델이 없어 최종 실루엣에 부적합했다. 직접 Blender MCP에서 별도 씬/컬렉션으로 몸통·좁은 막날개·다리를 제작하고 반딧불만 복부 색상 alpha를 발광 마스크로 쓴다. [편집 원본·제작 코드·채널 메타데이터](../../../../../ArtSource/AmbientFauna)를 보존한다.

이 작은 곤충은 실제 폭 1.2~3.5cm로 원거리 텍스처 무늬가 읽히지 않는다. 신규 컬러 비트맵 대신 메시 정점의 무광 갈색/회색과 복부 황록색을 저작했다. 그러므로 ImageGen 텍스처는 만들지 않았으며 생성 원본이 있다고 주장하지 않는다. 신규 텍스처가 필요한 근접 콘텐츠로 확장될 때만 원래 Blender→UV→ImageGen 절차를 적용한다. 나비 구매 텍스처·모델은 보존한다.

FBX의 노드 스케일·축을 파생 정적 Mesh에 적용하고 폭1m로 정규화한다. Unity에서 UV1/UV2·색상·복부 마스크와 삼각형 수를 검사한다. 임포트된 vertex 수는 UV/노멀 분리 때문에 Blender vertex 수보다 크다.

## ADR-AF-03 — 기존 지역 수명과 안전 구 베이크 확장

독립 셀 매니저·풀·비동기 에셋 로더를 새로 만들지 않는다. `StreamedRegionContent`가 ContentRoot의 서식지를 준비 동안 억제하고, 저장/배치 복원 완료 뒤 활성화한다. `ContinuousWorldRegion.UnloadAsync`에서 먼저 억제하여 공격 구독·GPU 버퍼를 반환한 뒤 기존 비활성화/Addressables 언로드를 진행한다. 직접 에셋 참조이므로 늦은 에셋 완료 콜백은 없다. 실제 Addressables 지역 왕복은 별도 사용자 테스트이며 단순 호스트 토글 검사와 구분한다.

기존 수동 안전 구는 그대로 쓴다. 선택적으로 `AmbientFaunaPlacement`를 붙여 Box/Sphere 볼륨, Raycast 표면, 경사, 비행 높이, 간격, seed, 제외 Collider를 저작한다. 제외 경계와 겹치는 안전 구 전체를 버리고, 런타임 개체는 그 구 안에서만 움직인다. 동적 제외 도형별 GPU 충돌이나 height field 대신 보수적인 공간 베이크를 선택했다. 제외의 GPU 재평가는 없으며 동적 문/파괴 지형은 명시 억제 후 재베이크가 필요하다.

밀도는 ManualCount/Surface(m²)/Volume(m³) 중 하나만 적용한다. 베이크 ValidMeasure는 유효 후보 비율로 추정한 값으로 정밀 면적이 아니다. 설정/제외 Transform 해시와 Editor 지형/Collider/의존 에셋 해시를 검증하고 stale을 경고한다. 런타임 준비 시에는 안전 구의 실제 정적 충돌도 다시 검사한다.

품질/화면 상한은 정확한 GPU visible 수가 아닌 최대 크기·거리·Capacity 기반 보수적 투영 추정이다. 낮은 우선순위 그룹도 퇴장 중 실제 Capacity 예약을 유지하므로 새로운 그룹은 반환 전 대기한다. 즉각적인 품질 상한 축소에는 버퍼를 즉시 반환한다. 카메라별 개체 compact/cull 버퍼 대신 그룹 frustum 컬링과 bounded Indirect instance count를 유지한다. 현재 소규모 본편에서 별도 가시 인덱스·Boids 비용을 추가하지 않으며 비용 확대 시 실측 후 재검토한다.
교전·명시 락온은 기존 `ICombatEncounterService`/`ILockOnTargetService`로 읽고 낮은 CombatDensity를 적용한다. 품질은 기존 `ISettingsService`를 추종한다. 여러 공격 생산자는 sourceId+sequence 쌍으로 중복 제거하며, 지역 퇴장 때 TTL·관측 속도·임시 전투 억제를 지운다. 개체별 위치 저장이나 신규 사냥/보상 권위는 도입하지 않는다.
베이크된 정적 Collider의 Transform/bounds·Mesh/Terrain 참조를 그룹 단위로 보존하고 5Hz 예산 갱신에서 변화를 확인한다. 기존 `TerrainCallbacks.heightmapChanged`는 수정된 높이맵 영역의 XZ와 Terrain 높이 범위에 걸친 그룹을 즉시 무효화한다. 해당 그룹은 GPU와 CPU fallback 소유권을 반환하며 재베이크 전까지 Faulted 상태를 유지한다. 콜백 구독은 지역/컴포넌트 수명과 함께 등록·해제하고 중복 등록하지 않는다. 새 Collider 삽입·같은 Mesh 내부 변형은 `NotifyGeometryChanged` 호출 계약을 제공한다. 동적 정밀 충돌이나 자동 재베이크는 도입하지 않는다.
### 개체 이동의 시각적 계약

군집 목표를 공통 sin/cos 궤도로 생성하거나 개체의 초기 위치를 같은 반경의 고리에 정렬하지 않는다. 개체 ID/그룹 seed로 독립적인3축 목표를 만들고 서로 다른 간격·시점에서 부드럽게 갱신한다. 기존 속도/가속/회복/호버 수치를 재사용하며 안전 구와 캡슐 회피를 유지한다. 이는 제한된 GPU 비용으로 반복 곡선을 없애는 시각 근사이며 실측 날벌레의 상호작용 모델을 재현하는 것은 아니다. [사용자 실패·검증 상태](../../../../../docs/work-status/ambient-fauna.md#궤도-무늬-사용자-피드백-후속--2026-11-13).

### 비가시 자원 계약

렌더러 연결 heartbeat와 군집의 frustum/layer/거리 가시성 heartbeat를 분리한다. 렌더러 연결만으로 GPU를 할당하거나 계속 시뮬레이션하지 않는다. 실제 Game 카메라 중 하나에서 준비된 그룹의 안전 경계가 시야/ReleaseDistance 안에 들어온 뒤 활성화를 허용하며, 마지막 가시성 후2프레임 유예를 넘기면 Compute·GPU 버퍼·예산 예약을 반환한다. 화면 밖 그룹은 예산 후보에서도 제외한다. Play의 SceneView/Preview는 이를 깨우지 않는다. 벽에 가려진 frustum 내부의 정밀 occlusion까지 판정하는 방식은 아니다.

정상 렌더러가 있으면서 화면 밖/레이어 제외인 동안에는 CPU 장식 fallback을 비활성으로 유지해 비용이 CPU로 옮겨가지 않게 한다. 그룹/서식지 종료 및 렌더러 부재의 CPU 복구는 유지한다. 사냥 가능한 CPU 생물은 시야 대신 기존 SimulationDistance와 게임플레이 수명을 따른다. [검증·한계](../../../../../docs/work-status/ambient-fauna.md#비가시-자원-검증--2026-11-13).
