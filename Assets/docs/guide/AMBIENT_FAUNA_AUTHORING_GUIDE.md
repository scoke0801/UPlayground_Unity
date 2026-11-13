# GPU 환경 생물 저작 지침

- 설계: [CPU/GPU 역할과 렌더 계약](../design/world-art/environment/AMBIENT_FAUNA_ROLE_SEPARATION.md)
- 구현·검증·사용자 테스트: [환경 생물 작업 상태](../../../docs/work-status/ambient-fauna.md)

## 배치와 베이크

1. `Assets/03.Prefabs/Environment/AmbientFauna/`의 Butterfly, Gnat, Firefly, AirMote 중 하나를 씬에 배치한다. 프리팹의 서식지 루트 아래 그룹이 GPU 입자 한 묶음이다.
2. 그룹의 Profile과 Capacity를 확인한다. 부모를 포함한 월드 스케일은 1로 유지하고, 실행 중 이동하는 오브젝트에 붙이지 않는다.
3. 그룹의 Anchors에서 xyz는 그룹 로컬 중심, w는 비어 있어야 하는 안전 구 반경이다. 반경은 해당 종의 최대 크기보다 커야 한다. 기존 프리팹에는 로컬 원점 앵커가 있다.
4. 배치한 루트 또는 그룹을 선택하고 `UPlayGround → 툴 런처 → 월드 / 맵 → GPU 환경 생물 베이크`를 실행한다. 좌표와 정적 장애물을 검사하고 이 씬 전용 URP/Renderer 복제 프로필을 연결한다. 원본 렌더러 에셋은 변경하지 않는다.
5. 오류가 없으면 씬을 저장하고 Play로 확인한다. 위치·회전·부모·앵커·주변 정적 지형/구조물을 바꾸면 다시 베이크한다. 실패한 일괄 베이크는 Undo 그룹을 되돌린다. 생성된 복제 렌더 자산은 재사용을 위해 남는다.

안전 구는 지면·벽·수면 아래 공간과 겹치지 않게 둔다. GPU 개체는 이 구 안에서만 움직인다. 여러 앵커를 연결해 벽을 넘어가는 경로 탐색을 하지 않는다. 준비 시 정적 충돌체가 새로 들어온 앵커를 거절한다. 동적으로 움직이는 문·가구 등의 경로는 별도 배치 검수가 필요하다.

## 데이터 조정

프로필은 `Assets/10.Datas/World/AmbientFauna/`에 있다. 위치·속도·개체별 위상은 공유 SO에 저장하지 않는다.

| 항목 | 의미 |
|---|---|
| SizeRange | 정규화된 정적 메시의 실제 폭 범위(m). 나비 기본 0.06~0.10 |
| SpeedRange / Acceleration | 평상시 이동과 목표 속도 추종 |
| FleeSpeed / AvoidRadius | 접근·공격 자극에 반응하는 속도와 반경 |
| FlapFrequency / FlapAngle | 개체별로 위상을 달리한 날갯짓 |
| HourDensity | 0~24시 표시 목표 곡선. 자정 전후 값을 맞추고 NaN/Infinity를 넣지 않음 |
| RainDensity / SnowDensity | 비/눈이 최대일 때의 밀도 배율 |
| ActivationDistance / ReleaseDistance | 활성화와 반환 사이 히스테리시스 |
| FadeSeconds / NearFade | 퇴장·진입 시간과 카메라 근접 가림 방지 거리 |
| Capacity | 그룹 인스턴스의 실제 GPU 할당 개체 수. 표시 밀도 감소와 별개 |

프로필/그룹 Inspector 변경 시 기존 버퍼를 반환하고 다시 검증한다. 지원되지 않는 셰이더·빈 앵커·잘못된 수치·베이크 좌표 불일치는 준비 실패다. 여러 활성 서식지가 있으면 Capacity/그룹 한도를 함께 합산하며, 서로 다른 한도 중 낮은 값을 적용한다. 퇴장 중 그룹도 포함한다.

외형은 머티리얼의 Tint, Body Color, Cutoff, Emission, Glow Frequency와 Appearance로 조정한다. 날개 위상과 발광 맥동은 서로 독립적이며 일시정지 중에는 둘 다 멈춘다. 부유 입자는 카메라를 향한다. 신규 나비 메시를 만들 때는 UV0=컬러/알파, UV1=날개 방향/가중치, UV2=hinge xz 계약을 유지한다. 몸통은 날개 방향/가중치가 0이어야 한다.

## CPU 생물과 함께 쓸 때

GPU 장식 그룹에는 WildlifeCreature, Animator, NavMeshAgent 또는 보상/포획 권위를 개체별로 생성하지 않는다. 사냥·착지·개별 생활 행동이 필요한 생물은 기존 CPU 경로를 유지한다.

Cpu Fallbacks에는 같은 배치 슬롯의 기존 CPU 장식 생물을 참조할 수 있다. 원본은 GPU 그룹의 자식으로 옮기지 않는다. MaxHealth가 양수이거나 DropTable이 있으면 전환을 거절한다. 같은 원본을 여러 GPU 그룹에 중복 연결하지 않는다. 실제 카메라의 렌더 기능이 확인된 뒤 원본을 숨기고, 준비 실패·렌더 기능 부재·그룹/서식지 중지 시 원래 활성 상태로 복구한다. 원본 GameObject와 GUID는 삭제하지 않는다.

정규 월드의 시간·플레이어·공격 시작은 기존 서비스를 사용한다. 날씨는 서식지의 Environment 참조를 연결한다. 외부 폭발 등의 자극은 `EmitStimulus`를 호출할 수 있으며 sequence는 해당 서식지 내에서 고유하게 발급한다. 동시 자극은 8개까지이며 TTL은 scaled 시간이다.

## 확인 순서

먼저 `Assets/01.Scenes/Test/AmbientFaunaTest.unity`를 열어 Play한다.

| 조작 | 키보드 | 게임패드 |
|---|---|---|
| 관측점 이동 | WASD | 왼쪽 스틱 |
| 공격 자극 | Space | 남쪽 버튼 |
| 낮 / 밤 | T / N | D-pad 위 / 아래 |
| 비 켜기/끄기 | R | D-pad 오른쪽 |

이후 본편에서 접근·대시·공격·일시정지·거리 왕복·밤/비 전환·씬 재진입을 확인한다. 원본 CPU 동물의 사냥/생활 행동과 카메라 락온 가독성도 확인한다. 자세한 기대 결과와 회신 형식은 작업 상태 문서의 AF 항목을 따른다.

출력 캡처와 작은 자동 테스트는 최종 미술·성능 인수를 대신하지 않는다. 개체별 그림자와 Motion Vector는 출력하지 않는다. TAA 잔상, 과밀, 전투 경고 가림과 장시간 GPU 비용은 실제 프로젝트 카메라·대상 기기에서 확인한다. Player/Addressables 빌드는 별도 지시 없이 실행하지 않는다.

나비는 작은 안전 구 한 곳당 기본 Capacity 2로 시작한다. 검증 씬의 6마리는 비교용이며 스트레스 테스트의 32/128마리를 본편 기본 밀도로 복사하지 않는다. 더 많은 수가 필요하면 한 구에 몰아넣기 전에 주변 안전 공간과 전투 가독성을 확인한다.

## 표면·볼륨 저작과 진단

수동 앵커는 기존대로 사용할 수 있다. 자동 후보가 필요할 때 그룹에 `AmbientFaunaPlacement`를 추가한다.

1. Shape를 Box/Sphere로 정하고 Size/Radius를 실제 서식 공간에 맞춘다. 월드 scale1을 유지한다. 나비 한 구에 개체를 모으기 전에 공간을 나눈다.
2. ManualCount는 기존 Capacity를 유지한다. Surface는 UseSurface를 켜고 m²당 밀도를, Volume은 UseSurface를 끄고 m³당 밀도를 입력한다. 두 단위를 섞으면 베이크가 실패한다. MaximumCount는 베이크로 계산하는 Capacity의 상한이다.
3. SurfaceMask·MaximumSlope·FlightHeight·Clearance·MinimumSpacing·Seed·Samples를 조정한다. 정적 표면만 사용하며 NPC/생물/동적 Rigidbody 위에는 앵커를 만들지 않는다. ValidMeasure는 후보의 유효 비율로 추정한 면적/체적이며 정밀 측량값이 아니다.
4. Exclusions에 실내/길/전투 가독성 보호에 필요한 기존 Collider를 지정한다. Collider의 bounds와 닿는 안전 구 전체를 버리는 보수적 방식이다. 런타임 GPU 개체별 동적 충돌이 아니다. 베이크 시 주변 정적 Collider의 Transform·bounds·메시/지형 참조도 저장한다. 기존 구조물 변경은 예산 갱신 주기에 자동 억제하며, Terrain 높이 변경은 기존 TerrainCallbacks로 영향 영역을 즉시 억제한다.
5. 루트나 그룹 선택 후 툴 런처의 `GPU 환경 생물 베이크`를 실행한다. Inspector에 앵커 수·유효 측정값·stale 상태가 보인다. 설정/제외 Transform/주변 Collider·Mesh·Terrain 의존성이 바뀌면 다시 베이크한다. 실패한 선택 일괄 작업은 Undo 그룹을 되돌린다.
6. `GPU 환경 생물 검사` 또는 Habitat Inspector의 검사 버튼으로 현재 씬을 확인한다. 결과는 `output/AmbientFauna/Completion/world-validation.json`에 저장한다. 다른 씬 검사로 같은 파일이 교체될 수 있으므로 인수 근거로 쓸 때 씬 이름·원본을 함께 보존한다.

Placement Inspector는 실제 할당/상한·버퍼 bytes·표시 밀도·예산 선택을, Habitat Inspector는 예약 Capacity/그룹·지역 활성·공격 구독·자극 누락을 보여 준다. 화면 점유율은 최대 크기/거리/Capacity로 계산한 보수적 추정이며 정확한 GPU visible 픽셀 수가 아니다. `Ambient Fauna Tick` CPU marker는 Profiler에서 따로 확인한다.

## 종별 외형·품질·월드 수명

- Gnat/Firefly는 `Assets/08.Models/AmbientFauna/*Gpu.asset`의 전용 메시다. 나비 축소본으로 되돌리지 않는다. 제작 원본은 `ArtSource/AmbientFauna/`에 있다. UV1/UV2와 정점 색 alpha를 보존한다. Firefly의 alpha는 복부 발광 마스크이며 투명도 의미가 아니다.
- Gnat는 작은 고주파 배회와 짧은 호버, Firefly는 느린 수평 배회와 개체별 점멸을 쓴다. AirMote는 바람 드리프트와 수명별 재배치·출입 fade를 쓴다. Behavior/WanderFrequency/WanderRadius/HoverFraction/RecoverySeconds/ParticleLifetime은 종 SO에서 조정한다.
- 기본 프리팹 Capacity는 Butterfly2/Gnat4/Firefly3/AirMote4다. 본편 나비 폭6~10cm, Gnat1.2~2cm, Firefly2.5~3.5cm다. 작은 벌레가 멀리서 뚜렷하게 안 읽힌다는 이유만으로 수나 크기를 키우지 않는다.
- `Quality.asset`은 Low/Medium/High의 Capacity/그룹/ScreenFraction을 소유한다. 기본적으로 ISettingsService의 qualityIndex0/1/2~3을 세 단계에 대응시킨다. Follow Settings Quality를 끄면 저작 단계가 적용된다. 외부 검증에서 `SetQualityTier`를 호출해도 자동 추종을 끈다.
- 본편 `Quality.asset`의 화면 점유 추정 한도는 Low4%/Medium6%/High8%다. 이는 최대 크기와 경계 상자 기반의 보수적 추정이며 실제 픽셀 점유가 아니다. 종전0.05%/0.1%/0.2%에서는 나비에 접근할수록 전체 군집이 탈락했다. 수치를 줄일 때는 근접 fade가 시작되기 전 관찰 거리에서도 세 군집/나비6마리가 유지되는지 확인한다. 새 Quality SO의 코드 기본값은 본편 튜닝값과 다르므로 신규 프로필에는 기존 저작 SO를 기준으로 명시 설정한다.
- Priority가 높은 종·카메라 가까운 그룹을 먼저 선택한다. 한도 축소에는 실제 버퍼를 반환하고, 일반 선택 탈락에는 퇴장 fade가 끝날 때까지 예약을 유지한다. 배경 밀도 감소를 실제 할당 감소라고 표시하지 않는다.
- 기존 교전 서비스/명시 락온/플레이어 공격 뒤에는 CombatDensity를 적용해 전투 가독성을 우선한다. 접근은 몸체 높이의 캡슐, 공격은 전방 캡슐이다. 외부 자극은 `EmitCapsuleStimulus(start,end,radius,strength,seconds,sequence)` 또는 구형 `EmitStimulus`를 사용한다. 상한8개를 넘으면 가까운 강한 장수명 자극을 우선하고 DroppedStimuli를 올린다.
- 스트리밍 지역은 GPU 루트를 기존 `StreamedRegionContent.ContentRoot` 하위에 둔다. 준비 동안 억제, 준비 완료 뒤 공개, 언로드 시작에 구독/버퍼 반환을 거친다. 별도 fauna 셀 로더를 만들지 않는다. 정규 상주 씬은 기존 루트 그대로 사용한다.

`Butterfly_AmberSoft.asset/.mat`은 코드 없이 만든 색상 변형 예이며 본편에는 추가 배치하지 않았다. 새 변형은 고유 SpeciesId와 기존 메시/행동을 공유하되 머티리얼을 분리한다.
여러 생산자가 자극을 발행할 때는 `EmitSourcedCapsuleStimulus(..., sourceId, sequence)`를 쓴다. 동일 sourceId+sequence만 중복 제거하며 서로 다른 생산자의 같은 sequence는 허용한다. 기존 EmitStimulus/EmitCapsuleStimulus는 sourceId0의 간편 경로이므로 그 경로끼리는 서식지 내 고유 sequence를 사용한다. 비활성 지역은 자극 입력과 새 예산 예약을 거절하고, 퇴장 때 남은 TTL과 관측점 속도를 지워 재진입 시 과거 공격이 재생되지 않게 한다.
실행 중 형상 유효성이 깨지면 그룹은 Faulted/IsGeometryInvalidated가 되고 버퍼·fallback 소유권을 반환한다. 비활성화→재활성화만으로 이 상태를 우회하지 못하며, 편집 모드에서 다시 베이크해야 한다. 새 충돌체 삽입이나 같은 Mesh 인스턴스의 정점 변형처럼 기존 참조/Bounds 검사로 알 수 없는 생산자는 `habitat.NotifyGeometryChanged(worldBounds)`를 호출한다. 움직이는 문을 항상 통과해 다니는 연출에는 사전에 제외 영역을 두며, 이 감시는 정밀 GPU 동적 충돌을 대신하지 않는다. TerrainSubscriptionCount는 지역 활성 때1, 비활성/종료 때0이어야 한다.
검증 씬의 기본 수량은 Butterfly2/Gnat4/Firefly3/AirMote4다.64개 스트레스 표본은 자동 검사에서만 사용한다. Play 후 N/T/R로 밤/낮/비를 전환하고10초 이상 관찰해 개체들이 고리·8자·공통 선으로 모이지 않는지 본다. 기존 GPU 출력 픽셀 성공만으로 이 항목을 통과시키지 않는다.

비가시 확인: Game 카메라를 군집 반대 방향으로 돌리면2프레임 유예 이후 AllocatedCapacity/BufferBytes가0이어야 한다. 다시 보면 예산 평가(기본0.2초)와 fade를 거쳐 복구된다. Play 중 SceneView에서만 군집을 보고 있어도 Game 시야 밖 군집은 깨어나지 않는다. 카메라 레이어 제외도 GPU/CPU 장식 대체 실행을 깨우지 않는다. 실제 다른 Game 카메라가 같은 그룹을 보고 있으면 활성 유지가 정상이다. 벽 뒤이지만 카메라 frustum 안인 그룹은 정밀 occlusion 대상이 아니므로 계속 시뮬레이션할 수 있다. 자원0은 GPU 그룹의 버퍼/Dispatch 기준이며 호스트의 가시성·거리·예산·정적 형상 검사는 남는다.
