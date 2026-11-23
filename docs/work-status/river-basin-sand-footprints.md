# RiverBasin4km 동부 사막과 발자국 작업 상태

[설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_SAND_FOOTPRINTS_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md)

## 대시 발소리·전 캐릭터 확장

- `ActorFootstep`의 지면 이동 판정에 `Dash` 상태를 포함했다. 대시도 걸음과 같은 발 포즈 경로로 발자국·소리를 내며 종류는 Run이다. 모션 이벤트 경로의 억제 조건도 같은 판정을 쓴다. 대시 모션에는 베이크한 `FootstepEvent`가 없어 포즈 경로가 유일한 근거다.
- 라온 외 플레이어블 11명(Arin·Hwarin·Lian·Lili·Myomyo·MyoRyeong·Reine·SeolA·Sera·YeonHoa·Yura) 모델 프리팹에 `ActorFootstep`을 붙이고 `Assets/10.Datas/Actor/Footsteps/<이름>_FootstepSettings.asset`을 연결했다. 표면 프로필은 라온과 공유하고, 발목 높이·발바닥 크기·앞 오프셋은 모델의 발 높이와 발~발끝 길이에 비례해 보정한 추정값이다. H09는 미사용이라 제외했다. Lian은 발끝 본이 없어 액터 전방을 기준으로 한다.
- RiverBasin4km은 씬 전체에 `SandFootprintWorld`가 구독하므로 포즈 경로가 모든 표면에 적용된다. 다른 월드에서는 모션 이벤트 경로에 의존하며 신규 11명에게는 베이크한 이벤트가 없어 소리가 나지 않는다.
- 검증: 컴파일 오류 0, `FootstepEnvironmentTests` 14건 통과. Play 모드 주행·대시 접촉 횟수·파티 교체 시 활성 모델만 재생되는지·캐릭터별 발 치수 보정 값은 미검증이며 `사용자 테스트 대기`.

## 사막 권역 확대

- 동쪽 협곡 아래 지형 여섯 타일을 한 권역으로 확장했다. 모래 우세 면적은 약 0.405㎢이고 기존 0.070㎢의 약 5.8배다. 범위는 X 3241~4096 / Z 1471~2431이며 기존 분지와 연결된 면적이 99%다.
- WorldBuilder Sculpt 9번으로 새 사구를 기록했다. 높이 표본 255,302개, 최대 변화 2.14m. 저장본에서 여섯 타일 내부의 높이차 0m, 공유 모래 표본 차이 0, R8 마스크 불일치 0을 확인했다.
- [확장 저작·경계 검사](../../Tools/RiverBasinWorld/Rebuild/east-desert-expand.cs.txt). 사암 사면과 수로 완충부를 두고, 여섯 타일에 걸친 광역 R8 마스크를 하나의 발자국 도메인에서 조회한다.
- [확장 후 4km 지도](river-basin-sand-footprints-evidence/river-basin-desert-expanded-map.png). 노란 외곽은 모래 가중치 50% 이상이며, 원형 핀은 기존 분지의 발자국 검증 지점이다. 기존 발 디딤 및 발자국 수학 EditMode 21건이 통과했다.
- Editor Play에서 라온으로 X 3540 / Z 1608부터 X 3640 / Z 1625까지 주행하며 X 3584의 타일 경계를 건넜다. 경유지 세 곳에 도달했고, 경계 전부터 발생한 발자국 요청은 총 47개, 드롭 0개였으며 사막 발소리 프로필과 GPU D/R 기록을 확인했다. [실제 주행](river-basin-sand-footprints-evidence/expanded-play-on.png) · [실행 로그](river-basin-sand-footprints-evidence/expanded-play-on.txt)
- 짧은 Editor 주행에서 2.06초의 최장 프레임을 관측했다. 원인은 미분류이며 이번 실행은 성능 인수용 반복 계측으로 사용하지 않는다. 입구부터 북동 협곡까지의 전체 보행·미술 검수는 사용자 테스트 대기다. 확장 전 씬·데이터는 `output/RiverBasinWorld/EastDesert/expansion-backup/`에 보관했다.

## P0 통합 조사

- 대상은 WorldBuilder 월드 `6effd90b68ea4b3d8f8caa5197fcfe34`의 `Preview/RiverBasin4km.unity`다. 시작 시 씬에 미저장 변경 0. 동부는 512m Terrain, 높이 513², 페인트 128²로 구성된다. 실제 재질은 URP Terrain/Lit이며 기존 레이어 8장을 쓴다.
- Unity 6000.3.21f1, URP·Core 17.3.0, Animancer 8.3.1. Editor 계측 환경은 RTX 3060 Ti / Direct3D11 / PC_RPAsset이다.
- `ActorFootstep`, `FootstepEvent` 및 라온의 지면별 효과 데이터가 있다. 이벤트는 `PostAnimationEvaluation`을 사용하고 Foot IK 목표는 `OnAnimatorIK`에서 공급한다. 모래 알베도는 기존 `SandLayer`가 참조한다.
## 사막 지형 저작

[확장 전 사막 지도](river-basin-sand-footprints-evidence/river-basin-desert-map.png)

- `Terrain_7_3`의 높이 표본 54,685개를 바꿨다. 최대 변화량 3.05m. WorldBuilder Sculpt 명령 8번으로 캡처했다. 타일 외곽 높이는 원본을 보존했다.
- `FineDesertSand` 레이어는 기존 Idyllic Fantasy Nature의 모래 알베도와 노멀을 사용한다. 가중치를 조정한 7,505개 표본 중 모래가 0.5 이상인 표본은 4,321개다. 기존 8개 레이어의 슬롯을 보존했다.
- [재현 도구](../../Tools/RiverBasinWorld/Rebuild/east-desert-terrain.cs.txt). 작업 전 Terrain, 월드 정의, 씬 사본은 `output/RiverBasinWorld/EastDesert/backup/`에 보관한다.
- 기존 Polyart Dreamscape EastLands 원본으로 아치·암반 14개를 배치했다. 콜라이더 13개, LOD 그룹 14개. 재질 복제본 7개에서 이끼를 끄고 황토 계열을 적용했다. [조감](river-basin-sand-footprints-evidence/desert-overview.png) · [서쪽 접근](river-basin-sand-footprints-evidence/west-approach.png)

## 실접촉과 렌더링

- [연결·마스크 베이크 도구](../../Tools/RiverBasinWorld/Rebuild/east-desert-bind.cs.txt)로 4km 씬을 저장했다. 적용 대상은 `Terrain_7_3`, `EastDesertMask`(R8 128²), 별도 Terrain 재질, `PC_Renderer`의 발자국 렌더 패스다.
- 라온에 발 크기·신발 밑창 보정과 Sand 표면 프로필을 등록했다. 기존 모래 발소리 리소스 및 먼지 프리팹을 사용한다. 기존 MotionSet의 평가 후 발 디딤 이벤트에 연결했다.
- URP Terrain/Lit 17.3.0 포크는 Forward·GBuffer·AddPass·BasePass·DepthNormals에 공통 샘플링을 삽입했다. 저장 경로는 분리된 읽기 캐시/작업 버퍼를 사용하고, 타일마다 마지막 저장 epoch에서 렌더 시간까지 감쇠를 적용한다.
- `SandFootprintMathTests`와 기존 `FootstepEnvironmentTests` 21건 통과(실패·건너뜀 0). 음수 좌표, 0/30/50° 투영, 비대칭 마스크 샘플링, epoch 감쇠 및 기존 지면 응답을 검사했다.
- [저장 씬 검사](river-basin-sand-footprints-evidence/scene-check.txt)에서 모래 페인트와 R8 마스크 표본 불일치 0, 인접 세 타일의 최대 높이차 0m, Terrain·AddPass·BasePass 셰이더 지원을 확인했다. 실제 PC Renderer는 Forward+다.

## Editor Play 검증

- 라온을 X 3796 / Z 1773의 모래 구역에 놓고 가상 게임패드로 세 지점을 주행했다. [ON](river-basin-sand-footprints-evidence/play-on.txt) · [OFF](river-basin-sand-footprints-evidence/play-off.txt) · [TPS 화면](river-basin-sand-footprints-evidence/play-on.png)
- 단일 스탬프를 타일 네 장의 교차점에 넣어 네 슬라이스의 D/R 값을 읽었다. 박스 콜라이더를 위에 놓았을 때 새 요청 0, 차폐를 제거한 뒤 새 요청 1이었다. 보조 Base 카메라의 같은 구도에서 발자국 노멀·색상 켜기/끄기 차이를 확인했다. 일시정지 1초 동안 감쇠 clock 변화 0, Terrain과 앵커 평행 이동에 반대 방향 논리 오프셋이 적용됐다. [검증 기록](river-basin-sand-footprints-evidence/edge-check.txt) · [근접 화면](river-basin-sand-footprints-evidence/edge-on.png)
- 실제 일반 점프에서 정지 4초와 체공의 새 요청 0, 안정 착지 뒤 2개, 표면 프로필 `Surface_Sand`를 확인했다. 초기 시험에서는 착지 블렌드 중 발이 높아 0개였다. 모터의 착지 시점에 힌트를 주고 포즈가 실제 모래에 닿을 때 확정하도록 수정했다. [착지 기록](river-basin-sand-footprints-evidence/jump-check.txt)
- Editor 1920×1080 / RTX 3060 Ti / D3D11에서 10초 예열 후 같은 분지 경로를 60초씩 측정했다. ON에서 3,363/3,363 프레임 접지, 경유지 15개, 모래 요청 223개, 초과 드롭 0이었고, OFF에서도 15개를 지나며 모래 발소리 분류가 유지됐다. ON 마지막 GPU 배열은 눌림 픽셀 3,994개, 테두리 픽셀 3,213개를 반환했다. [ON 원본](river-basin-sand-footprints-evidence/timing-on.txt) · [OFF 원본](river-basin-sand-footprints-evidence/timing-off.txt)

| 1080p Editor p95 | ON | OFF |
|---|---:|---:|
| 전체 CPU 프레임 | 24.030ms | 24.201ms |
| 전체 GPU 프레임 | 15.397ms | 15.261ms |
| Editor 전체 GC 할당 | 30,807B | 23,647B |
| 발자국 GPU 텍스처 본체 | 40.31MiB | 0 |

전체 CPU p95는 두 실행에서 모두 16.7ms를 넘었다. GPU 전체 p95의 ON/OFF 차이는 +0.136ms였다. 전체 프레임과 Editor GC 수치이며, 발자국 업데이트·셰이딩·할당의 단독 비용은 분리 계측 대상이다. 6회 이상 디스패치한 ON 프레임 71개의 전체 프레임 시간 p95는 24.173ms였다. 제작 Terrain R8 마스크는 CPU·GPU 각 16KiB다.

## 저작과 재검증

- 먼저 [4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)을 열면 현재 적용본을 사용한다. 시작 위치는 4km 씬의 B1 접근로다. 사막만 반복 확인할 때는 Play에서 [분지 실행 도구](../../Tools/RiverBasinWorld/Rebuild/east-desert-probe.cs.txt)를 MCP `execute_code`로 실행한다.
- 메시와 Terrain의 수동 확인은 [메시 검수 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/EastDesert/Samples/SandFootprintMeshLab.unity) 또는 [Terrain 검수 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/EastDesert/Samples/SandFootprintTerrainLab.unity)을 열고 Play한다. 수신면의 `SandFootprintWorld` 컨텍스트 메뉴에서 **플레이어 앞에 양발 테스트 자국**과 **발자국 지우기**를 사용할 수 있다. 검수 씬에서는 배치된 앵커가 플레이어 위치를 대신한다.
- [Play 샘플 순회](river-basin-sand-footprints-evidence/lab-check.txt)에서 메시 Low, Terrain Standard, Terrain Extended 모두 양발 요청 2개, GPU 값 기록, 텍스처 규격을 확인했다. 읽은 텍스처 본체 크기는 각각 10.16 / 40.31 / 104.81MiB다. 순회 후 Console 오류 0건.
- 라온 설정은 `Assets/10.Datas/Actor/Footsteps/Raon_FootstepSettings.asset`이다. 13×29cm 발 모양, 발목과 밑창 위치, 모래 보행·달리기·착지 소리 각 4개 및 기존 먼지를 연결했다. 발 들기 이벤트 18개는 원래 클립을 공유하는 라온의 지면 효과용 MotionSet 복제본에 기록했다. 현행 Git 제외 경로에 있는 프로필·모션·사운드와 meta도 함께 보존한다.
- 현재 제작 씬의 수신 좌표와 공유 모래 표본은 [확장 저작 도구](../../Tools/RiverBasinWorld/Rebuild/east-desert-expand.cs.txt)의 `verify` 모드로 대조한다. 새 캐릭터의 신발 크기와 위치는 `FootstepSettingsSO`에 설정하고 `ActorFootstep`의 밑창 Gizmo와 움직임을 확인한다. NPC는 같은 컴포넌트와 평가 후 이벤트를 사용하며, 기본 허용 범위는 앵커 9m / 프레임당 4개다.
- 시각 효과를 끄려면 해당 씬 수신면의 `SandFootprintWorld`를 비활성화한다. 지형 조형의 원복 단위는 WorldBuilder Sculpt 명령 `e45db77e5b1f40a2a00dffea654de3f1` 및 사막 전용 페인트다. 작업 전 Terrain·월드 정의·씬 사본은 이후 변경을 대조하는 자료로 보존한다.

## 인수 범위

### 사막·설산의 좌우 발 접촉 누락 수정

- 사용자가 두 지역에서 발자국이 간헐적으로 한 발씩 찍힌다고 보고했다. 기존 코드는 애니메이션 디딤 이벤트가 실행된 프레임에만 지면 접촉을 확인했다.
- `ActorFootstep`은 좌우 발마다 별도로 디딤을 대기시키고, 이벤트 프레임에 발이 공중에 있으면 최대 0.18초 동안 평가가 끝난 발 포즈를 다시 조회한다. 발을 떼는 이벤트와 지면 이탈로 다음 디딤을 준비하고 같은 프레임에는 발마다 한 번만 찍는다.
- 첫 재주행에서는 사막 왼발 9회·오른발 10회가 교대로 찍혔다. 설산은 왼발 9회·오른발 15회로 차이가 남았다. 경사에서 발끝이나 뒤꿈치가 먼저 닿을 때를 처리하도록 발바닥 폭·길이와 맞닿은 지면 기울기를 접촉 및 이탈 계산에 반영했다. [경사 보정 전 주행](river-basin-north-alpine-evidence/shared-foot-contact-before-slope.txt)
- Unity 정상 컴파일 및 발 디딤·발자국 EditMode 25/25 통과. 늦게 닿는 발과 반대 발의 독립 접촉, 접지 중 중복 방지, 대기 종료, 평지·경사 접촉 거리를 검사했다. 라온의 일반·카타나 보행·달리기·질주 에셋에서 좌우 디딤 이벤트를 확인했다.
- 최종 Editor Play 입력 주행에서 사막 왼발 9회·오른발 9회, 설산 왼발 16회·오른발 16회를 기록했다. 두 구간 모두 같은 발의 연속 기록과 발자국 큐 드롭이 0회였다. [수정 후 좌우 발 기록](river-basin-north-alpine-evidence/shared-foot-contact.txt)
- **RB-FOOT-01 · 사용자 테스트 대기.** `RiverBasin4km`에서 사막 X 3796 / Z 1773과 설산 X 3310 / Z 3584를 각각 걷고 달린다. 뒤돌아봤을 때 접지한 좌우 발에 맞춰 자국이 이어지고, 멈춘 상태에서는 자국이 추가되지 않아야 한다. 한쪽이 계속 비면 위치, 이동 속도, 장착 무기와 함께 기록한다.

### 걷기·달리기의 평가된 발 포즈 추적

- 사용자가 걷기와 달리기에서 발자국이 나오지 않는다고 다시 제보했다. 앞선 가상 입력 검증에는 실제 이동 모드와 재생 클립 기록이 없었다.
- Unity의 그래프 평가·블렌딩·본 기록 순서를 확인했다. Animancer는 이벤트 방식과 커브 방식 예제를 제공하며, Johansen의 지면 보행 연구는 발끝과 뒤꿈치가 지면에 닿는 구간을 따로 다룬다. [Unity 실행 순서](https://docs.unity.cn/Components/ExecutionOrder.html) · [Animancer 발 디딤 예제](https://kybernetik.com.au/animancer/docs/samples/events/footsteps/) · [보행 연구 원문](https://runevision.com/thesis/rune_skovbo_johansen_thesis.pdf)
- 플레이어 지면 이동에서 매 프레임 평가된 좌우 발의 이동 방향 상대 위치를 읽는다. 뒤쪽에서 일정 거리 앞으로 이동한 발이 느려질 때 지면을 조회하고, 접촉이 확인되면 자국과 표면 효과를 한 번 재생한다. 정지·체공·이동 상태 이탈·순간이동에서 보폭 추적을 초기화한다. 점프 착지와 일반 모션 이벤트는 기존 경로를 사용한다.
- Unity 컴파일 및 EditMode 26/26 통과. 발을 내미는 단계와 접지 지연, 같은 보폭 중 중복 처리 방지를 검사했다.
- Editor Play에서 걷기 전환 핸들러와 가상 패드 이동으로 네 경우를 실행했다. 사막 걷기 좌/우 6/7회, 사막 달리기 5/5회, 설산 걷기 8/8회, 설산 달리기 6/6회였다. 실제 재생 모션은 `Raon_Common_Walk`와 `Raon_Common_Run`으로 구분했다. 네 경우 모두 좌우가 번갈아 찍혔고, 정지 후 추가 기록과 큐 드롭은 0회였다. GPU 눌림 텍셀도 네 경우 모두 확인했다. [모드·모션·GPU 로그](river-basin-north-alpine-evidence/walk-run-footprints.txt)
- 기본 입력의 걷기 액션에는 바인딩이 없어 검증에서 기존 전환 핸들러를 직접 호출했다. 입력 장치별 조작과 다양한 경로에서 발바닥·자국의 시각 정렬은 사용자 테스트 대기다.

### 출발·정지·공격의 새 접지

- 기존 추적은 지면 이동 상태를 벗어날 때 보폭을 초기화했다. 플레이어는 모션 전환 뒤에도 좌우 발의 마지막 접지 위치를 유지한다. 발의 재배치 거리, 들린 높이, 멎는 속도와 지면을 확인해 한 번만 찍는다. 걷기와 달리기의 보폭 후보와 같은 접지 위치를 공유한다.
- 문헌의 위치·속도 기반 접촉 검출과 블렌드 후 포즈를 사용했다. 발별 이력과 이동 중 보폭 판정을 조합해 단일 속도 기준으로 모든 모션을 처리할 때의 누락을 줄였다. [EPFL 발 접촉 검출 연구](https://infoscience.epfl.ch/server/api/core/bitstreams/e67f08d0-326e-4d41-9532-40cfad36fb3b/content) · [Animancer 블렌딩 자료](https://kybernetik.com.au/animancer/docs/introduction/features)
- 전환 검증본에서 EditMode 27/27이 통과했다. Editor Play에서 사막과 설산의 출발·입력 해제·기본 공격 중 새 디딤과 안정된 대기 상태의 추가 기록 0회를 확인했다. [전환 입력 로그](river-basin-north-alpine-evidence/replant-transitions.txt)
- 걷기·달리기 재검사 중 설산 걷기에서 같은 발의 연속 기록이 2회 있었다. 이동 보폭을 추적하는 동안에는 일반 재배치 정착 판정이 새 디딤을 생성하지 않도록 보정했다. [중간 회귀 로그](river-basin-north-alpine-evidence/replant-gait-regression.txt)
- 최신 코드가 Unity에 로드된 상태에서 EditMode 27/27 통과. 최종 Editor Play에서 사막은 출발 3회·정지 전환 3회·기본 공격 1회, 설산은 4회·2회·2회였다. 두 지형 모두 이후 안정된 정지 상태에서 추가 기록은 0회였다. [최종 전환 로그](river-basin-north-alpine-evidence/replant-transitions-final.txt)
- 최종 걷기·달리기 회귀에서 사막 걷기 좌/우 7/7, 달리기 5/5, 설산 걷기 8/8, 달리기 6/6회였다. 네 경우 모두 같은 발 연속 기록, 안정된 정지 중 추가 기록과 큐 드롭이 0회였다. GPU 눌림 값도 확인했다. [최종 보행 회귀 로그](river-basin-north-alpine-evidence/replant-gait-final.txt)
- 사용자 테스트 대기 범위는 장착 무기별 다른 공격 모션과 시각 정렬이다.

| 항목 | 확인한 범위 | 남은 확인 |
|---|---|---|
| T01–T02, V01–V03 | 기존 평가 후 발 이벤트, 양발 착지 2개, 체공·정지 0개, 분지 주행 | 낮은 보행 20주기 정량 검사, 제자리·옆·뒤걸음과 공유 믹서 중첩 재현 |
| T03–T07, V04–V07 | 음수 타일·30/50° 투영·픽셀 중심·epoch 수학 단위 검사. 제작 씬 GPU 교차점 네 슬라이스와 차폐. 보조 카메라에서 자국 렌더링 확인 | 실제 30/50° 인물 접촉, Terrain holes·별도 레이어 순서 변형·SSAO 단독 비교 |
| T08–T10, V08–V12 | 주행 중 타일 교체, 샘플 이동에 오프셋 보상, 1초 일시정지, 보조 Base 카메라 | 100회 경계 왕복, 음수 월드 실주행, 장시간 큐 과부하, 전 카메라 스택, 시간배율·프레임 제한 조합 |
| V13–V16 | 메시·Terrain 검수 씬과 품질 프리셋 3종 GPU 기록, D3D11 Forward+ Editor 경로 | 실제 NPC 혼잡, 품질 20회 재할당, GPU 패스 단독 비용·Player·기타 API |

한 번에 활성화하는 수신 도메인은 하나다. 제작 씬은 한 Terrain의 모래층을 쓴다. 샘플의 메시와 Terrain은 독립 씬으로 확인한다. 현재 하드웨어에서 RG16F를 사용했고, 미지원 장치에서는 발자국을 끄는 경로를 사용한다. 타일·채널 진단은 수동 샘플과 GPU 읽기 도구로 제공한다. 정규 WorldBuilder 청크 Bake와 스트리밍 재진입은 미수행이다.

### 사용자 테스트 대기

- **RB-SAND-01** 4km 씬에서 분지 X 3796 / Z 1773 부근을 걷고 달리다 멈춘 뒤 카메라를 낮추고 돌아본다. 좌우가 바닥 방향과 맞고 사구의 색을 가리지 않으면서 자국을 알아볼 수 있어야 한다. 모래에서 바위로 나갈 때 바위에 자국이 번지거나 음향과 먼지가 끊기는지 함께 확인한다.
- **RB-SAND-02** 분지의 낮은 사면에 양발 착지하고, 지형 솔기를 지나 달린 뒤 다시 돌아온다. 지속 시간과 테두리 강도, 프레임 끊김, TPS에서 카메라가 암반을 통과하는지 확인한다. 먼 동선인 채석 요새 접근부터 아치까지는 미술과 진행 방향을 함께 검수한다.
- 회신 형식은 `항목 ID / 통과·실패·미수행 / 위치·증상 / 화면 또는 로그`다.
