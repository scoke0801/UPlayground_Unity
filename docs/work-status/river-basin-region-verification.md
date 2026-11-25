# River Basin 4KM 실물 배치 독립 검증

2026-11-24 · Unity 6000.3.21f1 · [지역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [제작 상태](river-basin-world.md#여덟-권역-실물-배치와-연결) · [월드와 아트](../wiki/world-art.md)

최신 판정은 [개선 및 재검증](#개선-및-재검증)에 있다. 아래 결함 표는 개선 전 재현 근거다.

## 재현된 결함

| 우선순위 | 판정·방식 | 월드 X / Y / Z와 재현 단계 | 실제 결과와 근거 |
|---|---|---|---|
| 진행 불가 · RB-Q01 | 실패 · Editor Play, 정적 캡슐 검사 | 하단 포탈로 진입해 (3225.000 / −71.994 / 2204.000)에서 중앙 묘실 (3208 / −72 / 2204)로 서진한다. Z 2204 중심과 Z 2203.10 남쪽으로 각각 걷는다. | 중심은 (3217.746 / −71.990 / 2204.582), 남쪽은 (3218.860 / −71.950 / 2203.335)에서 정지했다. `Dng_Brazier`와 `Dng_Crates_Barrels` 사이에 플레이어 캡슐(지름 0.70m)이 지나갈 연속 보행 공간이 없었다(6cm 격자, 발 Y −71.98). 상부 포탈까지의 정상 도보 접근도 이 구간에서 차단된다. [남쪽 재현](river-basin-region-qa-evidence/dungeon-play.txt) · [소품 충돌체](river-basin-region-qa-evidence/dungeon-colliders.txt) · [캡슐 단면](river-basin-region-qa-evidence/dungeon-capsule-grid.txt) |
| 이동·충돌 · RB-K01 | 실패 · Editor Play | 성채 남서쪽 (2238.000 / 83.656 / 2096.000)에서 문루 중앙 (2270.000 / 86.719 / 2088.000)을 향해 걷는다. 문루 축에 정렬해 다시 접근한다. | 독립 접근 두 번과 연속 본선에서 문턱 앞 (2265.67 / 86.41 / 2088.84~2088.85)에 정지했다. 전진 캡슐이 `Castle_Gate`에 맞은 지점은 (2266.024 / 86.736 / 2088.771)이다. 아래 지형은 계속 완만하게 올라간다. 점프와 성 안쪽에서 바깥 방향으로 걷기는 통과했다. [정렬 재현](river-basin-region-qa-evidence/approaches-retest-play.txt) · [연속 주행·점프](river-basin-region-qa-evidence/mainline-continuous-play.txt) · [지형 단면](river-basin-region-qa-evidence/gate-grade.txt) |
| 이동·충돌 · RB-B301 | 실패 · Editor Play | B3 갈림길 (1656.000 / 48.718 / 1555.000)에서 첫 E2 돌 표식 X 1638 / Z 1546을 향해 걷는다. | (1638.518 / 48.863 / 1547.016)에서 `Deco_Stone_Cairn`이 직진을 막는다. 표식 옆으로 돌아가는 접근은 별도 행에 기록했다. [입력·충돌 로그](river-basin-region-qa-evidence/approaches-play.txt) |
| 이동·충돌 · RB-F01 | 실패 · Editor Play | R1 남쪽 (1260.000 / 58.045 / 2232.000)에서 첫 표지 (1257.000 / 58.150 / 2252.000) 방향으로 걷는다. | (1257.353 / 58.222 / 2251.749)에서 `TGV_Wood_Sign_01A`와 기둥 충돌체에 걸렸다. 옆으로 돌아가는 접근은 별도 행에 기록했다. [입력·충돌 로그](river-basin-region-qa-evidence/approaches-play.txt) |
| 환경 상호작용 · RB-F02 | 실패 · Editor Play, 정적 데이터 대조 | 프리뷰를 단독 Play한다. `Terrain_2_4`의 나무 1074 (1247.970 / 57.976 / 2303.589), 2294 (1240.792 / 58.690 / 2306.187)를 ID 표와 비교한다. | 수목 승격 초기화가 1074에서 예외로 중단된다. 두 나무의 X/Z와 수종은 일치하고 Y는 ID 표보다 각각 0.436m, 0.342m 낮았다. 두 지점 모두 이번 개울 조형 명령 `074e9019b07e4cd58956c3b026459ede` 영역 안에 있어 개울 조형에 따른 높이와 ID 표 불일치로 판단한다. [실측](river-basin-region-qa-evidence/scene-probes.txt) · [Console](river-basin-region-qa-evidence/console.json) · [예외 발생 코드](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/TerrainTreePromotion.cs#L157) |
| 조망·카메라 · RB-Q02 | 실패 · Editor Play 화면 | 하단 포탈로 들어간 뒤 (3225.000 / −71.994 / 2204.000)에서 카메라 입력 없이 대기한다. | 캐릭터는 회랑 축을 따라 선다. 화면 정면은 옆 벽으로 가득 차서 진행 방향이 보이지 않는다. 관찰 끝 카메라 (3225.000 / −70.258 / 2202.666). 카메라 스냅이 직전 yaw를 유지하는 동작과 일치한다. 상부 내부에서도 같은 벽 방향을 촬영했다. [하단 도착](river-basin-region-qa-evidence/game-ENTER-after.jpg) · [상부 내부](river-basin-region-qa-evidence/portal-UPPER_UNLOCK-before.jpg) · [카메라 스냅](../../Assets/02.Scripts/Camera/CameraManager.cs#L932) |
| 조망·미술 · RB-F03 | 실패 · Editor 정적, 고정 화면, Editor Play 화면 | R1 목교 남단에서 북쪽을 보며 걷는다. 교량 위 (1241.500 / 58.883 / 2305.000)에서 기존 길 메시 높이를 비교한다. | `WB_P04_Road_R1` 회색 면이 나무 상판 일부를 덮는다. 같은 X/Z의 길 메시 Y 59.037, 상판 Y 58.883. 측정한 최대 겹침 높이 차는 0.272m다. 기존 길 메시에 충돌체가 없어 양방향 이동은 통과했다. [Play 화면](river-basin-region-qa-evidence/play-forest-bridge-center.jpg) · [위에서 본 접합](river-basin-region-qa-evidence/fixed-29-bridge-section.jpg) · [높이 대조](river-basin-region-qa-evidence/editor-session.txt) |
| 조망·미술 · RB-Q03 | 실패 · Editor 정적, 고정 화면 | 하단 회랑의 (3224.000 / −70.450 / 2204.000)에서 서쪽을 보고 X 3222.60~3225.10의 중심선을 위아래로 검사한다. | 바닥과 천장 중앙으로 푸른 배경이 새어 보인다. 세 X 표본에서 Z 2203.98~2204.02의 위아래 레이가 바닥과 천장을 모두 놓쳤고, Z 2203.97과 2204.03에서는 바닥과 천장이 잡혔다. 해당 구간에서 플레이어 추락은 없었다. [회랑 화면](river-basin-region-qa-evidence/fixed-18-dungeon-east.jpg) · [수직 레이](river-basin-region-qa-evidence/dungeon-seam.txt) |
| 조망·미술 · RB-H01 | 실패 · 고정 화면 | 내륙 접근 (2760.000 / 14.397 / 276.000)에서 시장 너머 정박선 방향을 본 뒤 남쪽 경사를 내려간다. | 설계상 내륙에서 처음 보여야 할 돛대 실루엣이 지정 시점에서 구분되지 않는다. 시장 끝 (2818.000 / 12.395 / 246.000)에서 등대가 먼저 보이고, 하강로 (2826.000 / 13.054 / 220.000)에서 돛과 방파제·수평선이 열린다. [내륙 접근](river-basin-region-qa-evidence/fixed-21-harbor-first.jpg) · [시장 끝](river-basin-region-qa-evidence/fixed-35-harbor-market-edge.jpg) · [하강로](river-basin-region-qa-evidence/fixed-36-harbor-220.jpg) |

마커 없는 초견 길 찾기, 발소리 청감과 카메라 체감은 [사용자 테스트 대기](#카메라-회전과-남은-사용자-검증)다.

## 범위와 방법

- Unity MCP 인스턴스 `UPlayground@fddd0702` 한 개에서 지정된 [프리뷰 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)을 검사했다. 시작 시 Edit Mode, 로드된 씬 한 개, `isDirty=False`였다. 작업 트리에 이미 있던 씬·Terrain 변경을 포함한 실제 실물 배치를 검증했다.
- Editor Play는 각 시작점으로만 플레이어를 옮긴 뒤 프로젝트 입력 바인딩에 가상 게임패드 왼쪽 스틱과 RT를 넣었다. 보행은 KCC, 상호작용은 게임의 상호작용 입력을 사용했다. 하단에서 상부로 이어지는 정상 보행이 막혀 상부 포탈 검사는 회랑 서쪽에서 독립 시작했다. 기존 회색 매스 4/4 결과는 이번 판정에 포함하지 않았다.
- 아래 좌표는 월드 X / Y / Z(m)다. 시작점에서 실제 입력을 넣어 끝점에 도착한 경우에만 이동 통과로 기록했다. 고정 화면은 그 좌표에서 프리팹이나 지형이 어떻게 보이는지 확인하는 자료다.

## 채석장 던전

| 항목 | 판정·방식 | 재현 단계와 월드 X / Y / Z | 실제 결과와 근거 |
|---|---|---|---|
| 포탈 네 곳 연결 | 통과 · Editor 정적 | 하단 바깥 (3213.300 / 93.669 / 2204.000), 하단 안 (3228.700 / −72 / 2204), 상부 안 (3183.450 / −72 / 2204), 상부 바깥 (3170.500 / 103.573 / 2171.350)의 PortalActor 확인 | 네 포탈의 목적지와 RT 이동 설정을 확인했다. 상부 포탈 해금 ID와 바깥 역진입의 요구 플래그가 일치한다. [포탈 설정과 초기 상태](river-basin-region-qa-evidence/editor-session.txt) |
| 하단 입장·프롬프트 | 통과 · Editor Play | (3210.000 / 94.050 / 2202.000)에서 입구 쪽으로 접근해 (3211.957 / 93.830 / 2203.739) 부근에서 RT | 입구에 RT 프롬프트 표시. (3225.000 / −71.994 / 2204.000)에서 안정 접지했고 이어서 회랑 안을 걸었다. 입력 해제 후 2.7초 이상 관찰에서 포탈 재발동이나 추락이 없었다. [입장 화면](river-basin-region-qa-evidence/entry-prompt.jpg) · [Play 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) |
| 회랑 → 묘실 | 실패 · Editor Play | 하단 입장 직후 X가 감소하는 방향으로 (3208 / −72 / 2204)까지 걷는다. | RB-Q01. 중앙 묘실까지 도착하지 못했다. [재현 로그](river-basin-region-qa-evidence/dungeon-play.txt) |
| 하단 귀환 | 통과 · Editor Play | 하단 포탈 도착점 (3225.000 / −71.994 / 2204.000)에서 동쪽으로 되돌아가 RT | 하단 대기면 (3210.000 / 93.897 / 2202.000)으로 돌아왔다. 안정 접지, 입력 해제 후 재발동 0회. [입력 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) |
| 초기 상부 역진입 잠금 | 통과 · Editor Play | 새 세션의 (3169.500 / 103.250 / 2174.600)에서 바깥 포탈 쪽으로 걸어가 RT | 플래그는 false, 상호작용 불가, (3170.544 / 103.225 / 2172.513)에 남았다. [입력 로그](river-basin-region-qa-evidence/dungeon-play.txt) |
| 내부 해금 → 상부 출구 → 역진입 | 통과 · Editor Play | 회랑 서쪽 (3187.500 / −71.880 / 2204.000)에서 시작해 (3184.511 / −71.994 / 2204.000)에서 RT, 잠시 기다렸다가 다시 RT. 상부 운반길에서 같은 문으로 돌아가 RT | 첫 입력으로 플래그가 true가 됐고 두 번째 입력으로 (3169.500 / 103.044 / 2174.600)에 도착했다. 역진입은 (3187.500 / −71.994 / 2204.000)으로 이어졌다. 각 도착에서 접지를 유지했고 재발동은 없었다. [격리 시작·포탈 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) |
| 저장·불러오기 후 상부 역진입 | 통과 · Editor Play | 같은 Play에서 새 폴더에 슬롯 0 저장, 메모리의 테스트 플래그를 false로 전환, 슬롯 0 로드 뒤 상부 운반길에서 RT | 저장 및 로드 성공, 플래그 true 복원, (3187.500 / −71.994 / 2204.000)에 재진입했다. 런타임 SaveManager의 저장 경로를 테스트 폴더로 지정했고 로드 직후 경로와 활성 슬롯을 복원했다. [격리 경로와 입출력 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) |
| 포탈 직후 카메라 추적 | 통과 · Editor Play 계측 | 하단·상부 두 도착 (3225.000 / −71.994 / 2204.000), (3187.500 / −71.994 / 2204.000) 직후 | 표본에서 카메라가 도착 플레이어를 따라 붙은 뒤 유지됐다. 지하 도착 뒤 카메라 거리는 약 2.2m였다. 진행 방향이 벽에 가리는 현상은 RB-Q02에 기록했다. [카메라 좌표 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) |

## Editor 정적 검사

| 항목 | 판정·방식 | 좌표 및 절차 | 실제 결과와 근거 |
|---|---|---|---|
| 지역 아트 계층 | 통과 · Editor 정적 | `RiverBasin · 지역 아트` 하위 계층 조사 | 여덟 지역과 B3 분기 오브젝트가 활성 계층에 존재한다. 활성 Renderer 337개, Collider 252개, Missing Script 0개. [계층 로그](river-basin-region-qa-evidence/editor-session.txt) · [컴포넌트 집계](river-basin-region-qa-evidence/gate-grade.txt) |
| 본선·E2 NavMesh | 통과 · Editor 정적 | 호수 마을 (2320.000 / 35.997 / 970.000)→B3 교대, B3 북단 (1599.000 / 47.814 / 1648.000)→B1, E2 갈림길 (1656.000 / 48.718 / 1555.000)→경사 상단 | 세 연결에서 `PathComplete`. [NavMesh 로그](river-basin-region-qa-evidence/mainline-nav.txt) |
| 성채 접근 NavMesh | 실패 · Editor 정적 | 문루 서쪽 (2238.000 / 83.656 / 2096.000)에서 5m 안의 NavMesh 검색 | 목적지 표본 없음. 현재 씬의 NavMesh 범위 관찰이다. 이번 문루 배치의 도보 충돌은 RB-K01에서 입력으로 별도 확인했다. [NavMesh 로그](river-basin-region-qa-evidence/mainline-nav.txt) |
| 개울·목교 접속 표면 | 통과 · Editor 정적 | (1224.000 / 60.486 / 2308.000) 샘 → (1243.200 / 58.045 / 2305.000) 목교 아래 → (1258.000 / 57.727 / 2305.000) 여울에서 지면·수면 조회. 양쪽 교대와 상판 수직 레이 검사 | 수심은 순서대로 0.424m, 0.272m, 0.204m였다. 상판 중심 Y 58.883, 남쪽 지반 Y 58.650, 북쪽 지반 Y 58.992. [정확한 수심](river-basin-region-qa-evidence/editor-session.txt) · [수면 단면](river-basin-region-qa-evidence/scene-probes.txt) · [수직 레이](river-basin-region-qa-evidence/structure-probes.txt) |
| 항구 접합부 | 통과 · Editor 정적 | X 2826에서 Z 165→147으로 지면, 석축, 목재 상판을 수직 레이로 표본 조사 | Z 154에서 석축 상면 Y 0.998와 데크 Y 0.840이 함께 잡힌다. Z 151에서 데크 Y 0.840까지 보행 상면이 이어진다. [수직 레이](river-basin-region-qa-evidence/structure-probes.txt) |
| 대표 구조물 접지와 수목 축 | 통과 · Editor 정적 | 호수·숲·사막 주거, 문루, 항구 창고/등대, 폐성 벽 총 31개 충돌체 외곽의 X/Z와 Terrain 수목 좌표를 비교한다. 인양틀 (3194.200 / 95.199 / 2194.000) 및 등대 (2862.000 / 3.162 / 174.000)의 받침 높이도 비교한다. | 건물 충돌체 외곽 X/Z 안의 Terrain 수목 축은 0개. 인양틀과 등대의 렌더 경계 하단은 중심 지면보다 각각 0.202m, 0.070m 낮았다. 수관의 화면 가림은 숲 고정 화면에 기록했다. [수목 표본](river-basin-region-qa-evidence/building-tree-overlap.txt) · [접지 표본](river-basin-region-qa-evidence/editor-session.txt) |

## 깊은 숲 마을 입력 검사

| 항목 | 판정·방식 | 재현 단계와 월드 X / Y / Z | 실제 결과와 근거 |
|---|---|---|---|
| 목교 양방향 | 통과 · Editor Play | 남안 (1245.550 / 58.654 / 2299.200) → 상판 (1243.200 / 58.883 / 2305.000) → 북안 (1240.800 / 59.060 / 2311.000), 역방향도 같은 중심선 | 남→북 102/102, 북→남 118/118 접지. 경계에서 발 빠짐·수영·이동 정지 없었다. 이동 시 카메라 최소 거리 양방향 5.35m 이상. 상판 화면에서 캐릭터가 보인다. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) · [상판 화면](river-basin-region-qa-evidence/play-forest-bridge-center.jpg) |
| 서쪽 얕은 물 | 통과 · Editor Play | (1228.800 / 60.310 / 2305.000)에서 물로 들어가 목교 방향 (1237.881 / 58.864 / 2304.428)까지 걷는다. | 물 표본 77프레임, 수영 0프레임. 발소리 처리에서 `Surface_ShallowWater` 반응 5건. 자갈을 지나는 순간 일시 이탈 뒤 재접지했다. [발소리·입력 로그](river-basin-region-qa-evidence/approaches-retest-play.txt) |
| 동쪽 자갈 여울 | 통과 · Editor Play | (1249.600 / 58.286 / 2301.700)에서 물로 들어가 (1257.677 / 58.413 / 2304.894)까지 걷는다. | 물 표본 52프레임, 수영 0프레임. 얕은 물 발소리 상태 3건. 자갈을 지나는 순간 일시 이탈 뒤 재접지했다. [발소리·입력 로그](river-basin-region-qa-evidence/approaches-retest-play.txt) |
| 남쪽 접근과 표지 우회 | 통과 · Editor Play | R1 (1260.000 / 58.045 / 2232.000)에서 표지 동쪽 X 1259로 우회하고 (1248 / 약 59 / 2292)와 목교를 통과한다. | 북안 (1241.070 / 59.035 / 2310.318) 도착, 접지 670/670. 경로 중 (1253.101 / 58.237 / 2268.655)에서 카메라 거리가 1.87m까지 짧아졌다. [우회·카메라 로그](river-basin-region-qa-evidence/approaches-retest-play.txt) |
| 사당·연못 분기 통행 | 통과 · Editor Play | R1 교차점 (1152.000 / 78.358 / 2640.000)에서 북쪽 사당, 남서쪽 R1b 연못 방향으로 각각 입력 | 사당 방향 (1152.944 / 79.249 / 2649.317), 연못 방향 (1139.943 / 78.197 / 2592.707) 도달. R1b (1140.919 / 77.903 / 2595.902)에서 카메라 거리 1.08m를 기록했다. [사당 입력](river-basin-region-qa-evidence/approaches-play.txt) · [연못 입력](river-basin-region-qa-evidence/approaches-retest-play.txt) |

## 여덟 지역 접근로 입력 검사

| 지역·항목 | 판정·방식 | 재현 단계와 월드 X / Y / Z | 실제 결과와 근거 |
|---|---|---|---|
| 호수 마을 북서 출구 | 통과 · Editor Play | (2320.000 / 35.997 / 970.000)에서 북서 출구 (2298.495 / 36.455 / 997.398)로 걷는다. | 접지 424/424, 수영 0. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) |
| B3 본선과 현수교 | 통과 · Editor Play | 갈림길 (1656.000 / 48.718 / 1555.000)에서 교대로 접근한 뒤 (1645.050 / 47.391 / 1592.350)에서 북서로 횡단 | 교대 진입 및 반대편 (1599.478 / 47.833 / 1647.125) 도달. 한 프레임 접지 이탈 후 복귀했으며 수영 0. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) · [다리 중앙](river-basin-region-qa-evidence/play-B3-center.jpg) |
| B3 E2 하강과 복귀 | 통과 · Editor Play | (1656.000 / 48.718 / 1555.000)에서 돌무더기 북동쪽으로 비켜 경사 상단에 도착한다. (1492.000 / 34.532 / 1450.000)에서 하단 X 1464 / Z 1471까지 내려갔다 상단으로 돌아온다. | 우회로 최소 Y 34.52, 2230/2230 접지. E2 경사 왕복 중 최소 Y 29.50, 572/572 접지, 수영 0. [입력 로그](river-basin-region-qa-evidence/approaches-retest-play.txt) |
| B1 횡단과 성채 접근 | 통과 · Editor Play | (1706.967 / 51.745 / 1832.472)→B1→(1763.956 / 52.459 / 1879.930), 별도 구간 (1856.000 / 57.922 / 1956.000)→(1895.474 / 59.995 / 1991.526) | 석교 횡단 및 성채 원경 위치를 지나가는 보행 모두 표본 전체 접지. 문루 통과는 RB-K01 실패. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) |
| 중앙 본선 연속 주행 | 실패 · Editor Play | 호수 마을 (2320.000 / 35.997 / 970.000)에서 시작해 B3, B1, 성채 오르막을 거쳐 문루로 이동한다. | 중간 좌표 이동 없이 문루 앞 (2265.673 / 86.413 / 2088.847)까지 도달했다. 접지 24,185/24,186, 수영 0. 문루의 동일한 문턱에서 정지했다. [연속 주행](river-basin-region-qa-evidence/mainline-continuous-play.txt) |
| 문루 점프와 역방향 | 통과 · Editor Play | 서쪽 문턱 직전 (2265.222 / 86.357 / 2088.929)에서 점프해 안쪽으로 진행한다. 역방향은 (2284.000 / 88.002 / 2086.000)에서 서쪽으로 걷는다. | 점프 후 안쪽 (2283.431 / 87.973 / 2086.047), 역방향 보행은 서쪽 (2252.735 / 84.988 / 2091.356)에 도착했다. [추가 재현](river-basin-region-qa-evidence/mainline-continuous-play.txt) |
| 깊은 숲 R1 | 통과 · Editor Play | (1260.000 / 58.045 / 2232.000)부터 표지를 우회해 목교 쪽으로 간다. | 목교 및 갈림길의 독립 입력 결과는 위 표 참조. [입력 로그](river-basin-region-qa-evidence/approaches-retest-play.txt) |
| 채석장 인양틀·단구 | 통과 · Editor Play | 상단 (3146.000 / 117.985 / 2140.000)→(3167.749 / 104.308 / 2169.277), 단구 (3192.000 / 94.399 / 2204.000)→(3209.253 / 93.893 / 2202.148) | 상단 접근 641/641, 단구 356/356 접지. 단구에서 입구 앞을 걸을 수 있었다. [상단 행로](river-basin-region-qa-evidence/approaches-retest-play.txt) · [단구 행로](river-basin-region-qa-evidence/approaches-play.txt) |
| 채석장 던전 | 실패 · Editor Play | 하단 대기면 (3210.000 / 93.897 / 2202.000)에서 회랑으로 들어가 중앙 묘실 방향으로 걷는다. | 포탈 연결·귀환은 통과했다. 정상 보행은 RB-Q01에서 막혔다. [던전 로그](river-basin-region-qa-evidence/dungeon-play.txt) |
| 사막 마을 입구·R4 | 통과 · Editor Play | R5 (3676.000 / 61.194 / 1500.000)에서 서쪽 골목 (3634.687 / 64.178 / 1512.069), 남쪽 R4 (3662.128 / 61.228 / 1461.682)로 각각 걷는다. | 골목 801/801, R4 784/784 접지. 낮은 지붕 방향 접근과 갈림길 통과를 확인했다. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) |
| 항구 시장·부두 | 통과 · Editor Play | (2760.000 / 11.997 / 276.000)→시장 (2790.000 / 11.118 / 270.000)→(2816 / 약 10 / 248)→부두 (2826.001 / 0.850 / 128.752) | 시장 접근·경사 하강·석축과 목재 접합부를 통과했다. 부두까지 접지 2020/2040, 수영 0. 경사 하단 재시험도 통과했다. [시장·접합부](river-basin-region-qa-evidence/approaches-retest-play.txt) · [부두 입력](river-basin-region-qa-evidence/approaches-play.txt) · [부두 화면](river-basin-region-qa-evidence/play-harbor-dock.jpg) |
| 설산 폐성 옆 고원길 | 통과 · Editor Play | (3223.000 / 183.53 / 3240.000)에서 폐성 동쪽 X 3220~3222를 지나 (3224.851 / 209.933 / 3337.316)까지 북상 | 접지 2261/2261. 폐성 벽 옆의 오르막에서 정지·수영 없었다. [입력 로그](river-basin-region-qa-evidence/approaches-play.txt) |

## 설계 접근 방향 고정 화면

좌표는 카메라의 월드 X / Y / Z다. 캡처는 58° 시야각의 고정 카메라 42컷이다. 전체 구도의 조준점과 높이를 로그에 기록했다. [카메라 로그](river-basin-region-qa-evidence/fixed-camera.txt) · [추가 조망점](river-basin-region-qa-evidence/fixed-followups.txt) · [비교 1](river-basin-region-qa-evidence/contact-sheet-1.jpg) · [비교 2](river-basin-region-qa-evidence/contact-sheet-2.jpg) · [비교 3](river-basin-region-qa-evidence/contact-sheet-3.jpg)

| 항목 | 판정·방식 | 카메라 위치와 재현 | 실제 결과와 근거 |
|---|---|---|---|
| 호수 마을 북서 출구 | 통과 · 고정 화면 | (2320.000 / 38.397 / 970.000)에서 북서 출구 방향으로 본다. | 왼쪽 민가와 출구 표지가 보이고 그 너머가 열린다. (2457.000 / 29.510 / 1080.000) 물가 시점에서 수면이 열리고 전경의 나무가 일부 가린다. [출구](river-basin-region-qa-evidence/fixed-01-lake-exit.jpg) · [물가](river-basin-region-qa-evidence/fixed-02-lake-shore.jpg) |
| B3 첫 노출 및 E2 분기 | 통과 · 고정 화면 | 마을 쪽 (1700.000 / 53.022 / 1525.000), 갈림길 (1656.000 / 51.118 / 1555.000), 교대 접근 (1664.000 / 49.091 / 1578.000)을 비교한다. | 첫 두 시점에서 현수교가 굽이·수목 뒤에 가려지고 교대 접근에서 현수교 탑이 드러난다. 갈림길의 표지와 E2 하강 지형을 확인했다. [굽이 전](river-basin-region-qa-evidence/fixed-03-B3-before.jpg) · [현수교 노출](river-basin-region-qa-evidence/fixed-04-B3-first.jpg) · [분기 표지](river-basin-region-qa-evidence/fixed-05-B3-fork.jpg) · [E2 하강](river-basin-region-qa-evidence/fixed-06-B3-E2.jpg) |
| B1 이후 성채 지붕 | 통과 · 고정 화면 | (1896.000 / 62.422 / 1992.000)에서 본채를 바라보고 오르막 (2060.000 / 73.922 / 2088.000)과 문루 앞 (2252.000 / 87.351 / 2094.000)을 비교한다. | 오르막 사이로 본채 상층 지붕이 드러난다. 올라가면서 문루와 성벽이 함께 드러난다. [B1 뒤 원경](river-basin-region-qa-evidence/fixed-07-B1-keep.jpg) · [오르막](river-basin-region-qa-evidence/fixed-34-keep-2060.jpg) · [문루](river-basin-region-qa-evidence/fixed-08-keep-gate.jpg) |
| 숲 지붕·목교 첫 조망 | 통과 · 고정 화면 | R1 남쪽 (1260.000 / 60.445 / 2232.000)부터 표지 옆 (1257.000 / 60.715 / 2252.000), 마을 (1248.000 / 61.326 / 2292.000)을 비교한다. | 촬영 표본 중 표지 옆부터 수관 사이의 지붕 일부를 구분할 수 있다. 마을 안 풀길에서 목교 상판이 드러난다. [R1 남쪽](river-basin-region-qa-evidence/fixed-09-forest-R1-south.jpg) · [지붕](river-basin-region-qa-evidence/fixed-38-forest-2252.jpg) · [목교](river-basin-region-qa-evidence/fixed-39-forest-2292.jpg) |
| 숲 샘·교량 아래·동쪽 여울 | 통과 · 고정 화면 | 샘 (1228.000 / 62.608 / 2304.800), 개울 중류 (1233.500 / 61.095 / 2306.500), 동쪽 (1252.000 / 60.235 / 2307.500)을 촬영한다. | 샘 바위에서 나오는 물과 교량 아래 흐름, 동쪽 여울의 수면·지면을 확인했다. 상판과 기존 길의 겹침은 RB-F03에 기록했다. [샘](river-basin-region-qa-evidence/fixed-28-creek-spring.jpg) · [중류](river-basin-region-qa-evidence/fixed-12-forest-creek.jpg) · [여울](river-basin-region-qa-evidence/fixed-13-forest-creek-east.jpg) |
| 숲 R1·R1b 방향 | 사용자 테스트 대기 · 고정 화면, Editor Play | 마을 북쪽 (1240.800 / 61.560 / 2311.000)을 지나 (1159.200 / 77.467 / 2621.000)에서 분기 접근. (1152.000 / 80.858 / 2640.000)에서 북쪽과 남서쪽을 비교한다. | 접근 시 갈림길이 보인다. 분기 중앙의 고정 시점에서 연못 방향은 가까운 수풀에 가리고, 남서쪽 (1148.000 / 79.979 / 2626.000)까지 움직이면 R1b 길이 다시 보인다. 첫 방문의 방향 구분은 사용자 테스트 대기다. [분기 접근](river-basin-region-qa-evidence/fixed-31-forest-fork-approach.jpg) · [사당 방향](river-basin-region-qa-evidence/fixed-14-forest-fork.jpg) · [연못 방향](river-basin-region-qa-evidence/fixed-15-forest-pond.jpg) · [R1b 노출](river-basin-region-qa-evidence/fixed-41-forest-pond-turn.jpg) |
| 채석장 인양틀·던전 입구 | 통과 · 고정 화면 | 굽이 (3146.000 / 120.385 / 2140.000)에서 절개면을 내려다보고 하단 대기면 (3210.000 / 96.087 / 2202.000)에서 입구 방향을 본다. | 회색 단구의 인양틀과 소품, 하단의 아치와 등잔을 구분할 수 있다. [인양틀](river-basin-region-qa-evidence/fixed-16-quarry-ridge.jpg) · [갱도 입구](river-basin-region-qa-evidence/fixed-17-quarry-entrance.jpg) |
| 사막 진입·R4 | 통과 · 고정 화면 | R5 (3676.000 / 63.594 / 1500.000)에서 서쪽 지붕, R4 (3664.000 / 64.253 / 1472.000)에서 남쪽 길을 본다. | 사암 사이 낮은 지붕이 보이며 남쪽 갈림길 전방이 열려 있다. [마을](river-basin-region-qa-evidence/fixed-19-desert-first.jpg) · [R4](river-basin-region-qa-evidence/fixed-20-desert-R4.jpg) |
| 항구 하강·부두 | 통과 · 고정 화면 | 시장 아래 (2826.000 / 10.468 / 194.000), 석축 직전 (2826.000 / 3.935 / 165.000)에서 남쪽을 본다. | 하강하면 부두, 배, 방파제와 수평선이 함께 열린다. 내륙 시점의 돛대 누락은 RB-H01에 기록했다. [하강](river-basin-region-qa-evidence/fixed-22-harbor-descent.jpg) · [부두 접속](river-basin-region-qa-evidence/fixed-23-harbor-seam.jpg) |
| 폐성·고원 오르막 | 통과 · 고정 화면 | (3223.000 / 186.065 / 3240.000)에서 설산 쪽, 폐성 동쪽 (3222.000 / 197.223 / 3290.000)에서 높은 쪽을 본다. | 설산 아래 끊어진 성벽 윤곽과 그 옆의 밝은 오르막이 구분된다. [폐성 첫 조망](river-basin-region-qa-evidence/fixed-24-alpine-first.jpg) · [고원 접근](river-basin-region-qa-evidence/fixed-25-alpine-ascent.jpg) |

## 카메라 회전과 남은 사용자 검증

| 항목 | 판정·방식 | 재현 단계와 월드 X / Y / Z | 실제 결과와 근거 |
|---|---|---|---|
| 구조물 인접 카메라 회전 | 통과 · Editor Play 계측 | 던전 도착 (3225.000 / −71.994 / 2204.000), R1 목교 (1243.200 / 58.893 / 2305.000), 문루 앞 (2265.650 / 86.410 / 2088.850), 채석 입구 (3210.000 / 93.897 / 2202.000), 항구 시장 출구 (2816.000 / 10.005 / 248.000), 폐성 동쪽 (3220.140 / 195.845 / 3299.270)에서 오른쪽 스틱을 한 바퀴 이상 돌린다. | 카메라에서 캐릭터 상체까지의 물리 레이에 걸린 구조물은 0건. 목교 위 최소 거리 4.57m, 던전 안 2.12m. 조망을 잃는 포탈 초기 프레이밍은 RB-Q02에 기록했다. [회전 각도·레이 로그](river-basin-region-qa-evidence/camera-play.txt) |
| 마커 없는 초견과 복귀 | 사용자 테스트 대기 · Editor Play | 호수 마을 (2320 / 36 / 970), B3 갈림길 (1656 / 48.72 / 1555), 숲 분기 (1152 / 78.36 / 2640), 각 접근로의 고정 화면 좌표에서 평소 입력으로 지도 마커 없이 출발한다. | 이동과 고정 시점 관찰을 완료했다. 첫 방문자가 본선·하강로 및 사당·연못 방향을 스스로 구분하는지는 직접 플레이로 확인한다. [카메라 좌표](river-basin-region-qa-evidence/fixed-camera.txt) |
| 실제 청감과 카메라 체감 | 사용자 테스트 대기 · Editor Play | 숲 얕은 물 (1229.64 / 60.17 / 2306.83), 목교 (1243.20 / 58.89 / 2305), 던전 도착·회랑, 성채 문루와 항구 경계에서 일반 입력으로 걷고 카메라를 돌린다. | 발소리 반응 이벤트와 카메라 계측은 수행했다. 출력 음량·질감, 전환 속도 체감, 자유 이동 중 시야는 사용자의 판단이 남았다. [얕은 물 입력·반응](river-basin-region-qa-evidence/approaches-retest-play.txt) · [카메라 로그](river-basin-region-qa-evidence/camera-play.txt) |

## Console

- 시작 전 오류 0건, Play 중 오류 2건을 수집했다. 배치 관련 오류는 `Terrain_2_4` 수목 승격 예외 1건이다. 실제 소스의 나무 두 개와 ID 표를 대조했고 예외 처리에서 수목 승격 컴포넌트를 비활성화하는 것을 코드로 확인했다. [Console 원문](river-basin-region-qa-evidence/console.json) · [예외와 중단 처리](../../Assets/02.Scripts/Gameplay/World/ReactiveEnvironment/TerrainTreePromotion.cs#L99)
- `CalculatePolygonPath: invalid input position(s)` 1건은 NavMesh 목적지 샘플에 실패한 뒤 검증 프로브가 Infinity를 넘겨 발생했다. 콜스택의 `MCPDynamicCode.Execute`와 실패 지점이 일치한다. 접근로 판정에는 목적지 표본이 없다는 사실을 사용했다. [Console 원문](river-basin-region-qa-evidence/console.json) · [샘플 결과](river-basin-region-qa-evidence/mainline-nav.txt)
- B3 LOD 중복 등록 및 TerrainCollider 경고도 수집했다. LOD 경고는 B3 교량의 `WB_P04_Bridge_B3_Suspension`과 `현수교 렌더링` 두 그룹을 가리킨다. 새 지역 아트 그룹의 직접 오류와 구분한다. TerrainCollider 경고는 대상 오브젝트나 스택이 없어 이번 배치와의 관련 여부를 확인하지 못했다. [Console 원문](river-basin-region-qa-evidence/console.json)

## 종료 상태

작업 중 시작한 Play는 종료했다. 같은 프리뷰 씬 한 개가 Edit Mode에 열려 있고, 고정 촬영 뒤에도 `isDirty=False`였다. 원본 저장 폴더의 기존 파일 3개는 이름과 SHA-256이 시작 전과 일치한다. 테스트 슬롯은 프로젝트 `output/RiverBasinWorld/RegionArt/qa-save-…`에 격리했다. Player와 Addressables 빌드는 검증 범위 밖이라 미수행이다. [Editor 상태](river-basin-region-qa-evidence/editor-session.txt) · [저장 검증 로그](river-basin-region-qa-evidence/dungeon-portals-play.txt) · [촬영 종료 상태](river-basin-region-qa-evidence/fixed-followups.txt)

## 개선 및 재검증

2026-11-24 · 기존 프리뷰 씬에 개선을 저장했다. 적용 직전 씬과 R1 길 메시 에셋, 수목 ID 표는 `output/RiverBasinWorld/RegionArt/before-qa-fixes`에 백업했다. [적용 좌표와 변경 로그](river-basin-region-fix-evidence/asset-fixes.txt)

| 결함 | 개선 | 재검증 |
|---|---|---|
| RB-Q01 · 묘실 보행 차단 | 회랑 상자·통을 묘실 남쪽 (3208.000 / −72.000 / 2199.600)으로 옮겨 중앙 통로를 열었다. | **통과 · Editor Play.** 하단 입구에서 묘실 (3207.854 / −71.994 / 2204.000)까지 걷고 하단 대기면으로 돌아왔다. 다시 입장해 같은 묘실을 지나 상부 해금과 왕복까지 도달했다. 중간 위치 이동은 없었다. 격리 저장을 불러온 뒤 상부 재진입도 통과했다. [입력·저장 로그](river-basin-region-fix-evidence/quarry-play.txt) · [묘실 도착](river-basin-region-fix-evidence/portal-CHAMBER-after.jpg) |
| RB-K01 · 문루 문턱 | 기존 문루를 0.45m 낮춰 서쪽 진입부의 충돌체 밑단을 지면에 맞췄다. 새 문루 위치 (2270.000 / 86.269 / 2088.000). | **통과 · Editor Play.** (2238.000 / 83.656 / 2096.000)→(2319.256 / 89.429 / 2087.960)로 점프 없이 통과했다. 반대 방향도 통과했다. 접지 1143/1143, 968/968. [보행 로그](river-basin-region-fix-evidence/approaches-play.txt) · [문루 화면](river-basin-region-fix-evidence/fixed-keep-gate.jpg) |
| RB-B301 · E2 돌무더기 충돌 | 돌 표식 여섯 개를 하강로 옆으로 2.73m 옮겨 지면 높이를 다시 맞췄다. | **통과 · Editor Play.** 기존 충돌 지점 X 1638 / Z 1546을 지나 E2 상단 (1492.161 / 34.534 / 1450.547)으로 내려갔다. [직진·경사 로그](river-basin-region-fix-evidence/approaches-play.txt) |
| RB-F01 · 숲 표지판 충돌 | 남쪽 표지를 길 오른쪽 (1260.250 / 58.219 / 2252.000)으로 옮겼다. | **통과 · Editor Play.** 기존 충돌 경로로 접근해 우회 입력 없이 (1245.065 / 58.677 / 2299.691)에 도착했다. [보행 로그](river-basin-region-fix-evidence/approaches-play.txt) |
| RB-F02 · 수목 초기화 예외 | `Terrain_2_4`의 현행 수목 높이와 ID 표의 1074, 2294 행을 동기화했다. | **통과 · Editor 정적, Editor Play.** ID 표의 높이 두 값만 변경했다. 실행 중 수목 승격 컴포넌트가 켜졌고 Console 오류가 없었다. [Editor 검사](river-basin-region-fix-evidence/editor-checks.txt) · [Console](river-basin-region-fix-evidence/console.json) |
| RB-Q02 · 도착 카메라 방향 | 도착 방향 정렬 옵션을 기존 카메라 회전과 스냅 경로에 연결하고 채석장 포탈 네 곳에서 켰다. | **통과 · Editor Play.** 하단 카메라 yaw 270°, 상부 역진입 90°. 두 방향 모두 회랑의 진행 방향이 화면 중심에 들어왔다. [카메라 좌표 로그](river-basin-region-fix-evidence/quarry-play.txt) · [하단](river-basin-region-fix-evidence/portal-LOWER_ENTRY-after.jpg) · [상부](river-basin-region-fix-evidence/portal-UPPER_REENTRY-after.jpg) |
| RB-F03 · 목교를 덮는 길 메시 | 목교가 가로지르는 구간의 기존 R1 메시를 절개하고 양안 접속면을 다듬었다. | **통과 · Editor 정적, 고정 화면, Editor Play.** 상판이 드러나고 목교 양방향을 통과했다. 양쪽 얕은 물에서 `Surface_ShallowWater` 반응을 다시 확인했다. 수영 전환은 없었다. [상판 사진](river-basin-region-fix-evidence/fixed-forest-bridge-section.jpg) · [Play 화면](river-basin-region-fix-evidence/play-forest-bridge-center.jpg) · [보행·발소리 로그](river-basin-region-fix-evidence/approaches-play.txt) |
| RB-Q03 · 회랑 바닥·천장 틈 | 던전용 복제 메시의 이음부 뒤에 돌 표면을 보강했다. 기존 바닥·천장 모듈과 충돌체에 같은 메시를 연결했다. | **통과 · Editor 정적, 고정 화면.** 이전 레이가 빠졌던 (3223.850 / −70.400 / 2204.000)에서 바닥 Y −72.026, 천장 Y −68.374를 확인했다. 같은 구도에서 푸른 배경이 사라졌다. [적용 검사](river-basin-region-fix-evidence/asset-fixes.txt) · [회랑 화면](river-basin-region-fix-evidence/fixed-quarry-corridor.jpg) |
| RB-H01 · 항구 첫 돛대 조망 | 선체와 접수 높이를 유지하면서 항구 배 전용 메시의 돛대와 돛을 높였다. 돛대 최고점 월드 Y 13.39. | **통과 · 고정 화면.** 내륙 시점 (2760.000 / 14.397 / 276.000)에서 돛대와 돛 윗부분을 구분할 수 있다. 부두에서도 같은 배를 확인했다. [내륙](river-basin-region-fix-evidence/fixed-harbor-inland.jpg) · [부두](river-basin-region-fix-evidence/fixed-harbor-waterfront.jpg) |

Editor 정적 검사에서 모듈의 바닥·천장 이음부 18/18, 수목 높이 및 수종 인덱스 3,264/3,264, 목교 상판 위 R1 메시 겹침 0/335 표본을 확인했다. 영역 아트 계층의 Missing Script는 0개다. [검사 로그](river-basin-region-fix-evidence/editor-checks.txt)

Editor Play 가상 입력은 던전 10/10, 접근로 10/10 통과했다. 접근로에 문루 양방향, 숲 진입로·목교·얕은 물, E2 직진·경사 왕복, 항구 시장에서 부두까지를 포함했다. 수정 후 Play의 Console 오류는 0건이었다. [던전 입력](river-basin-region-fix-evidence/quarry-play.txt) · [접근로 입력](river-basin-region-fix-evidence/approaches-play.txt) · [Console](river-basin-region-fix-evidence/console.json)

기존 `PortalProgressGateTests` PlayMode 3/3을 통과했다. 테스트 픽스처에서 입력·대화 서비스 미등록 경고 두 줄이 수집됐다. 프리뷰 실동선의 플래그와 격리 저장 검증은 위 가상 입력 로그에 있다. [포탈 회귀 테스트](river-basin-region-fix-evidence/portal-regression.json)

프리뷰를 저장하고 Play를 종료했다. 기존 저장 파일 3개의 이름과 SHA-256이 수정 전과 일치했고, 회귀 저장은 별도 폴더에서 실행했다. [Editor 상태](river-basin-region-fix-evidence/editor-checks.txt) · [격리 저장 로그](river-basin-region-fix-evidence/quarry-play.txt)

마커 없는 초견 길 찾기, 청감과 수동 카메라 전환 체감은 사용자 테스트 대기다.
