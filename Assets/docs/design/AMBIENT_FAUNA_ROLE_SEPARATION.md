# CPU 야생 생물과 GPU 환경 군집의 역할 분리

- 상위 정본: [GPU 환경 생물 정규 설계](../../../docs/전달용/ambient_fauna_design/01_환경생물_정규설계.md)
- 구현·검증: [환경 생물 작업 상태](../../../docs/work-status/ambient-fauna.md)
- 저작: [환경 생물 저작 지침](../guide/AMBIENT_FAUNA_AUTHORING_GUIDE.md)

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

원본 Idyllic Fantasy Nature 나비는 몸통/날개 submesh로 분리돼 있다. 정적 메시를 파생하고 날개 폭 1m로 정규화해 종 크기를 적용한다. 원본 FBX·리그·텍스처·meta는 보존한다. 벌레/반딧불은 작은 동일 실루엣을 재사용하며, 부유 입자는 기술용 Quad/방사형 마스크를 사용한다. 신규 외형 원본을 만들지 않으므로 Blender/ImageGen 호출은 필요하지 않다.

## 월드 입력과 수명

서식지는 IActorQueryService의 관측점, IGameTimeService의 시각, 명시적으로 연결된 EnvironmentController의 강수·바람을 읽는다. 서비스가 없는 독립 검증 씬에는 preview 입력을 쓴다. 실제 플레이어의 기존 PlayerCombat.OnAttackStarted를 관측하며 공격 판정이나 GAS/MotionSet을 바꾸지 않는다. 활성 플레이어 교체·서식지 종료 시 구독을 해제한다.

개체별 위치·회피·날갯짓 위상은 GPU가 갱신한다. Time.deltaTime을 제한된 substep으로 적분하고, deltaTime 0에서는 이동·날갯짓·fade·자극 TTL을 멈춘다. 관측점 순간이동은 속도 자극으로 확대하지 않는다. 공격 자극은 최대 8개·TTL·활성 sequence 중복 제거·overflow 계수를 갖는다.

등록 → Dormant → Active → Retiring → Dormant/Disabled 순서다. 그룹은 가까워질 때 실제 Capacity를 예약한 뒤 버퍼를 만든다. 퇴장 중에도 예약을 유지하며 fade 종료·서식지 중지·셀 비활성화·Play 종료에 버퍼와 예약을 반환한다. 먼 거리/밀도 0에서 실제 버퍼를 반환하는 것과, 표시 밀도만 줄여 같은 Capacity를 계속 시뮬레이션하는 것을 구분한다.

여러 서식지의 할당/예약은 공동 예산에 합산한다. 활성 서식지의 한도가 다르면 낮은 한도를 적용하며, 한도 축소 시 후순위 할당을 반환한다. 등록 순서를 기본 우선순위로 사용한다. 임의 종별 우선순위 정책이나 Boids는 도입하지 않는다.

동기 직접 에셋 참조를 사용하므로 늦은 비동기 에셋 로드 콜백과 GPU 풀은 도입하지 않는다. generation은 진단 readback이 이전 할당의 완료를 적용하지 않도록 식별한다. 정상 경로에는 GetData/WaitForCompletion을 넣지 않는다.

시뮬레이션은 Update에서 그룹당 프레임에 한 번 수행한다. 카메라 콜백에서 적분하지 않아 보조 카메라 때문에 이중 이동하지 않는다. 렌더 패스는 카메라별 frustum과 그룹 Bounds로 거르고, 색상 및 깊이/노멀을 같은 GPU 상태에서 그린다. Bounds는 모든 앵커와 날개 크기를 포함한다. URP 셰이더는 양면 노멀·알파 클립·그룹 단위 앰비언트 프로브/주광·안개·독립 발광 맥동·카메라별 근접 dither를 처리한다. PC/Mobile Renderer의 실제 Deferred 설정에 맞춘 색상/깊이/노멀 패스에 같은 변형과 clipping을 쓴다. 개체별 실시간 그림자와 Motion Vector는 출력하지 않으므로 TAA 조건은 사용자 품질 검수에서 확인한다.

## CPU 원본 인계

GPU 전환 대상은 명시한 기존 장식 프로필 참조와 안전한 앵커가 있는 배치다. MaxHealth가 양수이거나 DropTable이 있는 CPU 생물은 대체하지 않는다. 실제 카메라의 출력 패스가 확인된 뒤 CPU 원본을 숨긴다. 렌더 기능 부재가 이어지면 GPU 버퍼를 반환하고 CPU 원본을 유지한다. 지원 불가·준비 실패·GPU 예외·그룹 중지 시에도 원래 active 상태를 복구한다. 거리/시간으로 GPU 개체가 잠시 사라졌다고 CPU 원본을 다시 표시하지 않는다.

CPU 원본과 GUID는 삭제하지 않는다. 한 원본의 중복 GPU 소유를 거절한다. 런타임 Inspector에서 fallback 참조가 바뀌어도 기존에 인계받은 원본을 정확히 복구하도록 준비 시 참조 배열을 별도로 보존한다.

이 인계는 기능 연결이며 미술 인수를 뜻하지 않는다. 실제 TPS 시야·락온 가독성·낮/밤 외형과 장시간 성능은 사용자 확인 및 지정 기기 측정으로 별도 판정한다.

