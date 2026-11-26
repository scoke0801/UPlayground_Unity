# RiverBasinWorld — 제작·검증 상태

- 설계 정본: [정규 월드 제작 설계](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) (좌표·치수·예산은 블록아웃에서 조정할 시작값이며 구현 완료 수치가 아니다)
- 저작 지침: [오픈월드 맵](../../Assets/docs/guide/OPENWORLD_MAP_AUTHORING_GUIDE.md), [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)
- 저작 원본·기록 스크립트: [Tools/RiverBasinWorld/README.html](../../Tools/RiverBasinWorld/README.html)
- 분야 위키: [월드와 아트](../wiki/world-art.md) (표의 「강 유역 월드」 행)
- 조사 기준: 2026-11-14 문서·파일 조사. 이번에 게임·Unity를 실행하지 않았다.
- 현재 판정: **독립 씬 1차 제작본 저장됨 / 미술 NO-SHIP·사용자 인수 대기 / P5 정규 등록 미제작**.

## WorldBuilder 신규 제작

### B1 석교와 성채 조망 아트 패스

**2026-11-25 프리뷰 씬 반영.** B3에서 오는 길의 목서가 석교 진입축을 덜 가리고, 양안의 판석 끝이 지면과 또렷이 맞닿는다. 다리 건너편에서는 능선 위 성채 지붕과 회색 오르막 방향 표지가 함께 보인다. 마커 없는 초견 길 찾기, 해질녘·악천후 시인성, 미술 인수는 사용자 테스트 대기다. 문루 진입은 다음 구간의 검증 범위다. [권역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md)

| 지점 | 월드 X / Y / Z (m) | 변경과 확인 결과 | 같은 구도 |
|---|---|---|---|
| B3 쪽 첫 조망 | 고정 카메라 **1689.000 / 54.300 / 1811.700**, 목서 **1695.200 / 51.568 / 1828.000** | 길 가장자리에 걸리던 목서를 왼쪽 수목군 쪽으로 약 4.8m 옮기고 기존 매입 깊이를 유지했다. 수관이 왼쪽 화면을 감싸고, 길축과 석교 입구가 함께 보인다. | [전](river-basin-b1-art-evidence/before-from-b3.jpg) · [후](river-basin-b1-art-evidence/after-from-b3.jpg) |
| B1 남서·북동 접속 | 남서 판석 끝 **1721.601 / 52.270 / 1844.668**, 북동 판석 끝 **1758.399 / 52.270 / 1875.332** | 지형에 물리던 판석 20장을 끝으로 갈수록 최대 2.4cm 올렸다. LOD 3종의 렌더 메시를 파생 에셋으로 저장했다. 기존 BoxCollider 3개와 MeshCollider 1개를 사용한다. | [남서 전](river-basin-b1-art-evidence/before-south-joint.jpg) · [후](river-basin-b1-art-evidence/after-south-joint.jpg) · [북동 전](river-basin-b1-art-evidence/before-north-joint.jpg) · [후](river-basin-b1-art-evidence/after-north-joint.jpg) |
| 성채 조망 갈림길 | 기준 카메라 **1896.000 / 62.422 / 1992.000**, 새 화살표 **1904.741 / 61.993 / 1995.826** | 기둥과 낮은 돌을 분기 사이 길 어깨로 옮겼다. 기존 사각 판자 대신 기존 목제 화살표 프리팹을 붙여 회색 성채 오르막을 향하도록 돌렸다. 성채 지붕은 두 길 너머 능선 위로 보인다. | [갈림길 전](river-basin-b1-art-evidence/before-fork.jpg) · [후](river-basin-b1-art-evidence/after-fork.jpg) · [성채 조망](river-basin-b1-art-evidence/after-vista.jpg) |

**Editor 정적 검사.** LOD0의 끝 판석을 150곳 비교했다. 수정 전 146곳에서 지형이 판석을 덮었고 수정 후에는 0곳이다. 끝 판석의 최종 상면은 지형보다 1.19~2.71cm 높다. 양안과 교량의 5열 지지면 레이 1,045곳에서 누락 0, 인접 지지면 최대 높이차 8.1mm를 기록했다. 석교를 가로지르는 저장 NavMesh는 `PathComplete`다. 성채 조망점은 검색 반경 3m에서 저장 NavMesh 종점을 찾지 못해 해당 구간은 실제 플레이어 입력으로 확인했다. 누락 스크립트는 0개다. [검사 로그](river-basin-b1-art-evidence/editor-checks.txt) · [배치 실측](river-basin-b1-art-evidence/changes.txt) · [고정 카메라 좌표](river-basin-b1-art-evidence/after-cameras.txt) · [파생 메시](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinB1LandingPass.asset)

**낮 Editor Play.** 처음에만 B3 쪽 접근로 X 1682 / Z 1803으로 위치를 설정하고 가상 Gamepad의 양쪽 스틱과 기본 TPS를 사용했다. (1692.383 / 51.640 / 1815.271)에서 목서 사이로 석교가 보인다. 양안 접속부를 지나 (1894.877 / 59.955 / 1990.963)에서 표지와 성채 지붕을 확인하고, 갈림길 너머 (1924.767 / 62.203 / 2011.559)에서 방향을 돌렸다. 석교를 다시 건너 남서 접근로 (1707.429 / 51.755 / 1832.878)까지 연속 이동했다. 총 604.13m, 중간 위치 재설정 0회다. 전체 비접지 표본 81개가 있었고, 시작 위치 설정 직후에도 비접지 표본이 기록됐다. [첫 접근 TPS](river-basin-b1-art-evidence/after-input-1.jpg) · [남서 접속 TPS](river-basin-b1-art-evidence/after-input-4.jpg) · [갈림길 TPS](river-basin-b1-art-evidence/after-input-13.jpg) · [왕복 입력 로그](river-basin-b1-art-evidence/after-input.txt)

같은 세션에서 위치 재설정 없이 석교 양안 접속부를 133.78m 추가 왕복했다. 비접지 표본은 0개였다. [북동 접속 TPS](river-basin-b1-art-evidence/after-bridge-contact-6.jpg) · [남서 접속 재통과](river-basin-b1-art-evidence/after-bridge-contact-9.jpg) · [접속부 재검증 로그](river-basin-b1-art-evidence/after-bridge-contact.txt)

**RB-B1-ART-01 · 사용자 테스트 대기.** 대상 씬의 낮 기본 TPS에서 B3 쪽으로부터 석교에 접근한다. 걷거나 달릴 때 목서 사이에서 길과 교량이 함께 보이는지, 양안 접속부에서 발이나 카메라가 걸리는지 확인한다. 이어서 지도·마커 없이 성채 방향을 고른다. 해질녘·안개에서도 표지와 회색 오르막이 구분되는지 확인한다. `RB-B1-ART-01 / 시간·구간 / 통과·실패·미수행 / 실제 결과 / 화면`으로 기록한다.

**백업과 Editor 상태.** 작업 전 [디스크 씬](../../output/river-basin-b1-art-20261125/disk-before.unity)과 [열린 씬](../../output/river-basin-b1-art-20261125/open-before.unity)을 각각 백업했다. Editor Play를 종료하고 저장 데이터를 대조했다. 구매 원본은 기존 참조를 사용했고 변경한 판석은 파생 에셋에 저장했다. Player·Addressables 빌드와 커밋은 미수행이다. [종료 상태와 해시 대조](river-basin-b1-art-evidence/editor-session.txt)

### B3 현수교와 E2 갈림길 아트 패스

**2026-11-25 프리뷰 씬 반영.** 마을 쪽에서 굽이를 넘으면 현수교가 보이고, 그 앞 갈림길에서는 오른쪽 흙길이 교대로, 왼쪽 회색 자갈길이 돌 표식과 E2 하강로로 이어진다. 맑은 낮 화면과 가상 입력으로 확인했다. 지도 없이 처음 길을 고르는 경험과 야간·악천후 시인성은 사용자 테스트 대기다. [협곡 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md) · [권역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md)

| 지점 | 월드 X / Y / Z (m) | 변경과 확인 결과 | 같은 구도 |
|---|---|---|---|
| 마을 쪽 굽이와 동쪽 교대 | 고정 조망 **1648.000 / 50.700 / 1580.000**, 바깥 발판 **1638.901 / 48.035 / 1599.719** | 굽이와 수목 너머에서 다리 탑이 먼저 드러나는 구도를 유지했다. 길이 흐려지던 23.67m에 기존 황갈색 흙 재질을 이어 발판으로 유도했다. 양안에서 지형에 묻혔던 목재 발판을 올리고 현수교 상판 재질로 판재를 나눴다. | [첫 조망 전](river-basin-b3-art-evidence/before-first-bridge.jpg) · [후](river-basin-b3-art-evidence/after-first-bridge.jpg) · [동쪽 접합 전](river-basin-b3-art-evidence/before-east-deck.jpg) · [후](river-basin-b3-art-evidence/after-east-deck.jpg) |
| 서쪽 교대 | 바깥 발판 **1605.098 / 48.035 / 1640.280**, 자갈 접속 시작 **1604.200 / 48.259 / 1655.300** | B1 쪽 기존 자갈길에서 발판까지 17.98m를 이었다. 발판에 가까워지며 폭을 줄여 교대 입구를 읽을 수 있다. | [전](river-basin-b3-art-evidence/before-west-deck.jpg) · [후](river-basin-b3-art-evidence/after-west-deck.jpg) |
| E2 갈림길 | 이정표 **1651.900 / 48.604 / 1556.900**, 첫 돌 표식 **1639.480 / 48.770 / 1543.710** | 이정표를 두 길 사이로 옮기고 관목 두 개를 어깨 쪽으로 옮겼다. 회색 자갈길 중심과 돌무더기 실루엣이 입구와 복귀 방향에서 보인다. | [갈림길 전](river-basin-b3-art-evidence/before-junction.jpg) · [후](river-basin-b3-art-evidence/after-junction.jpg) · [하강 방향 전](river-basin-b3-art-evidence/before-e2-mouth.jpg) · [후](river-basin-b3-art-evidence/after-e2-mouth.jpg) |
| E2 마지막 내리막 | 고정 카메라 **1495.000 / 37.200 / 1447.800**, 옮긴 관목 **1498.200 / 37.267 / 1443.400** | 렌즈 가까이 걸리던 관목을 경사 어깨로 옮겨 하단의 물과 자갈길이 보이게 했다. | [전](river-basin-b3-art-evidence/before-e2-top.jpg) · [후](river-basin-b3-art-evidence/after-e2-top.jpg) |

[배치·접합 좌표](river-basin-b3-art-evidence/changes.txt) · [고정 카메라 좌표](river-basin-b3-art-evidence/after-cameras.txt) · [현수교 파생 메시](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinB3LandingPass.asset)

**정적 검사.** 양안 교대에서 중심과 좌우 0.85m 세 열을 0.125m 간격으로 조사했다. 지지면 레이 258개가 모두 표면에 닿았다. 발판 구간 162곳에서는 목재 발판이 최상면이며, 표본 사이 최대 높이차는 2.5cm였다. 저장된 NavMesh에서 교량 양쪽, 갈림길에서 E2 상단, E2 상단에서 하단까지 세 경로 모두 `PathComplete`였다. 누락 스크립트는 0개다. [검사 로그](river-basin-b3-art-evidence/editor-checks.txt) · [수정 전 접합 높이](river-basin-b3-art-evidence/before-contact-deck-rays.txt)

**낮 Editor Play.** 가상 Gamepad 양쪽 스틱과 기본 TPS로 검증했다. 처음에만 마을 쪽 접근로 X 1672 / Z 1535로 위치를 설정했다. 갈림길과 굽이를 지나 현수교를 건너 (1603.453 / 48.066 / 1653.158)에 도달하고 갈림길로 돌아왔다. 왕복 261.12m이며 비접지 표본은 73개다. [다리 첫 노출](river-basin-b3-art-evidence/after-main-play-2.jpg) · [발판과 상판 접합](river-basin-b3-art-evidence/after-main-play-5.jpg) · [상판에서 내려다본 폭포](river-basin-b3-art-evidence/after-main-play-6.jpg) · [교량 왕복 입력](river-basin-b3-art-evidence/after-main-play.txt)

이어서 위치 재설정 없이 E2 상단 (1492.414 / 34.549 / 1450.745)과 하단 (1464.794 / 29.557 / 1470.438)을 거쳐 갈림길 (1655.478 / 48.734 / 1554.737)로 복귀했다. 왕복 460.04m의 비접지 표본은 0개다. [첫 돌 표식](river-basin-b3-art-evidence/after-e2-play-2.jpg) · [E2 경사](river-basin-b3-art-evidence/after-e2-play-9.jpg) · [하단에서 복귀 방향](river-basin-b3-art-evidence/after-e2-play-10.jpg) · [하강·복귀 입력](river-basin-b3-art-evidence/after-e2-play.txt)

동쪽 흙길의 렌더 메시를 연장한 뒤 최종 저장본을 다시 열었다. 카메라 초기화를 기다리고 굽이에서 한 번 위치를 설정했다. (1648.600 / 45.990 / 1578.400)에서 흙길과 발판을 밟아 다리 중앙 (1622.299 / 47.361 / 1619.563)까지 50.05m를 이동했다. 비접지 표본은 0개다. [최종 굽이 TPS](river-basin-b3-art-evidence/saved-east-ready-0.jpg) · [최종 발판 TPS](river-basin-b3-art-evidence/saved-east-ready-3.jpg) · [최종 입력 로그](river-basin-b3-art-evidence/saved-east-ready.txt)

**RB-B3-ART-01 · 사용자 테스트 대기.** 대상 씬의 기본 TPS로 마을에서 B3 방향을 따라 간다. 지도와 마커를 끄고 갈림길에서 다리와 하강로를 각각 찾는다. 다리 입구와 발판 사이에서 지표가 끊겨 보이거나 조작이 걸리는지 확인한다. E2로 내려갔다가 같은 갈림길로 돌아오며 방향을 놓친 지점을 남긴다. 야간과 안개에서 이정표 및 하강로 가독성은 별도 확인한다. `RB-B3-ART-01 / 시간·구간 / 통과·실패·미수행 / 실제 결과 / 화면`으로 기록한다.

**백업과 Editor 상태.** 작업 전 [디스크 씬](../../output/river-basin-b3-art-20261125/disk-before.unity)과 [열린 씬](../../output/river-basin-b3-art-20261125/open-before.unity)을 각각 백업했다. 새 판재와 길 메시를 권역용 에셋에 저장했다. 기존 씬 인스턴스에만 발판, 이정표, 관목 보정을 적용했다. 작업 중 시작한 Play를 종료했고 종료 전후 Console 오류는 0건이다. Player·Addressables 빌드는 미수행이다. [Editor 종료 상태와 저장 데이터 해시 대조](river-basin-b3-art-evidence/editor-session.txt)

### 호수 마을 첫 방문 아트 패스

**2026-11-25 프리뷰 씬 반영.** 북서 출구 접속부, 마을 안쪽, 서안 물가까지 작업했다. 첫 방문자가 마을 지붕과 우물에서 방향을 잡고, 둔덕을 넘으면 수면과 버드나무 섬을 발견하는 구성을 확인했다. 마커 없는 야간 초견 길 찾기와 미술 인수는 **사용자 테스트 대기**다. 섬을 정면으로 보는 고정 구도에서 수면의 각진 명암 경계가 보인다. 장마·안개, 지면 전체 마감, 월드 Bake와 정규 월드 등록 후의 시작 동선은 이번 검증 범위 밖이다. [씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [권역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [직전 북서 출구 검증](river-basin-local-defects-evidence/final-sections-play.txt) · [월드와 아트 위키](../wiki/world-art.md)

| 지점 | 월드 X / Y / Z (m) | 변경과 확인 결과 | 같은 구도 |
|---|---|---|---|
| 첫 조망 | 이전 Player (1706.967 / 51.925 / 1832.472), 새 시작 접지 (2311.000 / 35.958 / 953.000), 기본 카메라 (2311.000 / 38.578 / 948.149) | 기존 직접 Play는 B1 접근부에서 시작했다. 프리뷰 Player와 초기 카메라 배치를 마을 남쪽으로 옮겼다. 입력 전 기본 TPS에서 목서와 지붕, 우물, 배경 설산이 보인다. 약 7m 이동해 우물 쪽으로 시선을 돌리면 가게와 지붕 사이의 물가 방향 흙길이 드러난다. 수면은 둔덕을 지난 뒤 열린다. | [고정 전](river-basin-lake-village-evidence/before-fixed-arrival.jpg) · [고정 후](river-basin-lake-village-evidence/final-fixed-arrival.jpg) · [기본 TPS](river-basin-lake-village-evidence/final-nw-play-0.jpg) · [몇 걸음 뒤](river-basin-lake-village-evidence/final-nw-play-1.jpg) |
| 어부집 출입면 | 계단 기준 (2318.959 / 36.268 / 951.068), 기존 배 (2319.190 / 35.805 / 952.355) | 문을 가로막던 목선 메시를 분리했다. 가게의 기존 석재 계단으로 단을 낮춰 문턱과 지면을 이었다. 가상 패드로 아래 계단에 올랐다가 우물 쪽으로 돌아왔다. | [전](river-basin-lake-village-evidence/before-fixed-fisher.jpg) · [후](river-basin-lake-village-evidence/final-fixed-fisher.jpg) · [계단 TPS](river-basin-lake-village-evidence/final-fisher-play-3.jpg) |
| 가게 출입면 | 차양 기둥 하단 (2339.128 / 35.952 / 963.561), (2334.974 / 35.962 / 964.910), 상자 (2339.950 / 35.991 / 963.180), 통 (2340.580 / 35.982 / 962.370) | 떠 있던 차양 기둥 두 개의 밑단을 약 0.4m 늘려 지면에 묻었다. 상자와 통을 차양 옆으로 모았고 골목에서 가게 문턱까지 흙길을 덧댔다. 낮 Play에서 문 앞마당을 거쳐 동북 골목으로 나갔다. | [전](river-basin-lake-village-evidence/before-fixed-shop.jpg) · [후](river-basin-lake-village-evidence/final-fixed-shop.jpg) |
| 북서 출구 | 울타리 기둥 (2309.250 / 35.962 / 976.250), (2307.430 / 35.969 / 979.280), B3 접속부 보행 (2277.282 / 37.372 / 1021.162) | 끊어져 보이던 울타리 두 모듈의 방향과 위치를 맞췄다. 손수레와 자루를 울타리 안쪽 집 방향에 모았다. 입구 등잔 옆에서 기존 흙길이 이어지고 배치 뒤에도 B3 방향으로 걸어 나가 다시 돌아올 수 있었다. | [마을 쪽 전](river-basin-lake-village-evidence/before-fixed-nw-village.jpg) · [마을 쪽 후](river-basin-lake-village-evidence/final-fixed-nw-village.jpg) · [B3 연결 Play](river-basin-lake-village-evidence/final-nw-play-7.jpg) |
| 우물에서 서안 | 흙길 시작 (2319.500 / 36.040 / 971.500) → 끝 (2474.000 / 22.758 / 1068.000) | 기존 흙길 재질의 가장자리를 퍼뜨려 우물 갈림길, 가게 골목, 서안 내리막을 연결했다. 서안 길 길이는 207.0m다. | [골목 전](river-basin-lake-village-evidence/before-fixed-lane.jpg) · [골목 후](river-basin-lake-village-evidence/final-fixed-lane.jpg) · [내리막 전](river-basin-lake-village-evidence/before-fixed-bank-approach.jpg) · [내리막 후](river-basin-lake-village-evidence/final-fixed-bank-approach.jpg) |
| 섬 조망과 물가 | 고정 카메라 (2454.000 / 29.450 / 1079.700), 새 등잔 접지 (2450.600 / 28.778 / 1078.500), 수선 자리 목선 (2468.200 / 23.993 / 1064.350) | 시야를 막던 목서 두 그루와 관목 세 개를 옮겼다. 목서 한 그루는 마을 가장자리, 나머지 식생은 서안 양옆에 두었다. 표지와 시각 연동 등잔을 길 어깨에 두고, 물가까지 내려가며 수면과 섬이 함께 보이는 것을 확인했다. | [전](river-basin-lake-village-evidence/before-fixed-island.jpg) · [후](river-basin-lake-village-evidence/final-fixed-island.jpg) |

[인스턴스와 메시 좌표 기록](river-basin-lake-village-evidence/changes.txt) · [고정 카메라 좌표](river-basin-lake-village-evidence/final-fixed-cameras.txt) · [파생 메시 에셋](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinLakeVillagePass.asset)

**Editor 정적 검사.** 흙길 세 메시의 정점 2,744개가 지형보다 4.3cm 높고 길 자체의 충돌체는 0개다. 지름 0.70m 캡슐로 서안 중심 228곳과 북서 출구 중심 76곳을 검사해 지형 외 겹침 0곳을 확인했다. 가게 차양, 어부집 벽, 석재 계단, 배의 렌더와 충돌 메시가 일치한다. 계단의 두 수직 레이가 각각 Y 36.236m와 36.045m의 돌 디딤면에 닿았다. Terrain 64개와 누락 스크립트 0개를 확인했다. [검사 로그](river-basin-lake-village-evidence/editor-checks.txt)

**낮 Editor Play 연속 입력.** 저장본에서 시작해 게임 시각만 12:00으로 맞췄다. 실제 Input System의 가상 Gamepad 왼쪽·오른쪽 스틱을 사용했다. 북서 출구 왕복, 어부집, 물가 세 구간은 같은 Play의 연속 이동이며 위치 재설정은 0회다. 시작 사진은 좌우 스틱을 넣기 전에 촬영했다. [기존 Play 시작 화면](river-basin-lake-village-evidence/initial-start.jpg) · [기존 시작 좌표](river-basin-lake-village-evidence/initial-start.txt) · [새 시작 좌표와 초기 카메라](river-basin-lake-village-evidence/saved-start.txt)

| 구간 | 결과 | 실제 화면과 입력 기록 |
|---|---|---|
| 시작 → 북서 출구 → 우물 곁 | (2311.000 / 35.958 / 953.000)에서 시작했다. 울타리와 등잔 곁을 지나 B3 흙길의 (2277.282 / 37.372 / 1021.162)까지 걷고 마을로 되돌아왔다. 왕복 153.60m. | [울타리와 출구](river-basin-lake-village-evidence/final-nw-play-3.jpg) · [접속](river-basin-lake-village-evidence/final-nw-play-7.jpg) · [입력 로그](river-basin-lake-village-evidence/final-nw-play.txt) |
| 우물 곁 → 어부집 → 우물 곁 | (2318.541 / 36.149 / 951.963)의 계단을 밟고 마을 중심으로 돌아왔다. 왕복 36.84m. | [계단](river-basin-lake-village-evidence/final-fisher-play-3.jpg) · [입력 로그](river-basin-lake-village-evidence/final-fisher-play.txt) |
| 우물 곁 → 가게 앞마당 → 물가 | (2434.314 / 35.801 / 1053.264)에서 수면과 굽는 내리막이 보인다. (2456.278 / 27.260 / 1079.385)에서 버드나무 섬과 얕은 물가로 이어지는 길이 한 화면에 들어온다. (2472.135 / 23.096 / 1068.895)의 얕은 물까지 215.63m를 이동했고 비접지 표본은 0이다. | [가게 앞](river-basin-lake-village-evidence/final-bank-play-4.jpg) · [내리막](river-basin-lake-village-evidence/final-bank-play-11.jpg) · [섬](river-basin-lake-village-evidence/final-bank-play-14.jpg) · [얕은 물 도달](river-basin-lake-village-evidence/final-bank-play-17.jpg) · [입력 로그](river-basin-lake-village-evidence/final-bank-play.txt) |

**해질녘 고정 화면.** 18:33에 어부집과 가게 등잔이 문 주변 벽과 출입면을 밝힌다. 북서 출구와 서안 점광을 켜고 끈 비교에서 등잔 발치와 흙길 가까운 쪽이 옅게 밝아진다. 먼 구간에 닿는 빛과 기둥 등잔의 유리 밝기는 약하다. [어부집](river-basin-lake-village-evidence/final-dusk-fisher.jpg) · [가게](river-basin-lake-village-evidence/final-dusk-shop.jpg) · [서안 점광 끔](river-basin-lake-village-evidence/dusk-point-off-bank-approach.jpg) · [서안 점광 켬](river-basin-lake-village-evidence/dusk-point-on-bank-approach.jpg) · [북서 점광 끔](river-basin-lake-village-evidence/dusk-point-off-nw-village.jpg) · [북서 점광 켬](river-basin-lake-village-evidence/dusk-point-on-nw-village.jpg) · [조명 실측](river-basin-lake-village-evidence/dusk-lights.txt)

**RB-LAKE-ART-01 · 사용자 테스트 대기.** 지정 프리뷰를 직접 열어 기본 TPS로 Play한다. 12:00에 우물 곁에서 두 출구를 각각 찾고, 18:33에 어부집과 가게의 입구가 구별되는지 본다. 22:00에는 지도와 마커 없이 새로 시작해 먼저 물가를 찾고 마을로 돌아와 B3 쪽 흙길에 접속한다. 목적지를 놓치거나 지도를 열게 된 첫 위치를 화면과 함께 남긴다. 결과는 `RB-LAKE-ART-01 / 시간대·구간 / 통과·실패·미수행 / 실제 결과 / 화면`으로 기록한다.

**백업과 Editor 상태.** 작업 전 디스크와 열린 씬을 각각 [디스크 백업](../../output/river-basin-lake-village-20261125/RiverBasin4km.disk-before.unity), [열린 씬 백업](../../output/river-basin-lake-village-20261125/RiverBasin4km.open-before.unity)에 저장했다. 시작 시 열린 씬은 대상 한 개, Edit Mode, 미저장 변경 0건이었으며 두 백업의 SHA-256이 같다. 새 메시만 권역용 에셋에 넣어 구매 원본을 보존했다. 작업 중 시작한 Play를 종료했고 마지막 씬 사본의 해시가 디스크와 일치한다. 종료 직전과 직후 Console 오류는 각각 0건이다. [작업 종료 상태와 저장 슬롯 해시 비교](river-basin-lake-village-evidence/editor-session.txt)

### 4km 지역 콘셉트 재검토

**2026-11-23 부지 확정.** 열린 `RiverBasin4km` 저장 씬에서 Terrain 64장, 기존 배치, R1·R4·R5·`coast-dock`·설산길 중심선을 대조했다. 부지 24곳의 중심 높이와 2m 격자 경사를 비교하고, 기존 수면 메시와 접근 시점을 확인했다. [지역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [부지 지도](river-basin-region-sites-evidence/site-map.jpg) · [Unity 고정 카메라 15컷](river-basin-region-sites-evidence/site-contact-sheet.jpg) · [기존 동선 좌표](../../Tools/RiverBasinWorld/Rebuild/offroute-blockout-4km.csv)

| 조사 | 관찰과 결정 |
|---|---|
| 호수 마을·B3·성채 | 마을 중심에서 섬의 가설 수관 상단까지 지형이 시선을 약 0.7m 가린다. 서안 물가 X 2457 / Z 1080에서는 지형 시선이 열리며, 현장 화면 전경에 목서가 걸려 블록아웃에서 조망 틈을 잡아야 한다. B3 마을 쪽 교대 약 36m 전에서 현수교, 다리 위에서 하류 협곡을 고정 촬영했다. B1 이후 X 1896 / Z 1992에서 성채 문루의 가설 14m 상단까지 지형 시선이 열린다. |
| 숲 마을 | 반경 60m Terrain 수목은 강가 앵커 2그루, R1 X 1248 / Z 2292에서 165그루, 더 북쪽 X 1224 / Z 2352에서 176그루였다. 확정한 부지는 수관이 이어지고 표본 경사 중앙값·최댓값이 4.4°·11.0°다. 북쪽 대안은 5.0°·14.8°다. 강가 앵커를 서부 연결점으로 쓰고 수관 아래에 마을을 둔다. |
| 채석장·던전 | 작업 단구는 요새보다 북서쪽이고 중심 고도 차는 약 49m다. 동쪽 절개 암석 바로 앞 X 3210 / Z 2202의 12 × 12m 대기면은 Y 93.69~93.98, 최대 경사 2.7°다. 북쪽 대기면 X 3201 / Z 2238은 최대 27.7°다. 확정한 입구 방향에는 기존 절개 암석이 있고, 발견물은 Z 2215에 있어 입구 공사에서 따로 보존한다. |
| 사막 마을 | R4 시작점 Y 61.85를 갈림길로 비우고 서쪽 사면의 X 3632 / Z 1512를 택했다. 동쪽 대안 Y 59.39보다 약 5.1m 높고 서쪽 대안보다 부지 최대 경사가 낮다(14.4° 대 18.6°). 수면 메시가 중심점과 겹치지 않았다. 국지 배수와 골목 입구의 가림은 블록아웃에서 잡는다. |
| 항구마을 | 기존 선착장 앵커 Y 9.99에서 남쪽 해안까지 육상 연결이 필요하다. X 2826에서 Z 160은 지면 Y 1.33, Z 145는 지면 Y −1.55이고 수면은 Y 0.04였다. 시장은 기존 길과 이어지는 X 2790 / Z 270에 두고, 해안의 얕은 굽이를 방파제로 감싸는 배치를 택했다. |
| 설산 밑 폐성 | 기존 유적 남쪽 20m의 바위턱에서 40 × 32m 표본의 높이 범위는 Y 195.21~198.21, 최대 경사는 15.0°다. 기존 유적 중심 표본의 최대 경사는 25.8°였다. 폐성 중심을 바위턱에 두면 기존 오르막 중심선과 약 22m 떨어지고, 남쪽 접근에서 가설 8m 문루까지 지형 시선이 열린다. |

**부지 확정 단계의 검증 범위.** 높이·경사·수면은 저장 씬의 Editor 실측이다. 지형 시선은 2m 간격으로 Terrain 높이와 비교했다. 고정 화면은 비교 시트로 합쳤다. 첫 회색 매스 반영은 [중앙 본선 블록아웃](#호수-마을에서-성채까지-첫-블록아웃)에 기록한다. 갱도 연결, 사막 배수, 항구 정박 수심은 미수행이다. [월드와 아트 위키](../wiki/world-art.md)

### 호수 마을에서 성채까지 첫 블록아웃

**2026-11-24 구현 반영.** [RiverBasin4km 프리뷰 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 `RiverBasin · 호수 마을–B3–성채 블록아웃` 아래에 Unity 기본 큐브로 만든 36개 매스를 저장했다. 호수 마을은 주택 다섯 채와 북서 육상 출구·동북 물가 방향을 잡았다. B3 앞 갈림길에 표식과 E2 하강 방향 돌을 놓았다. 성채에는 기존 본채를 살려 남서 문루와 성벽, 시장 매스 세 동을 배치했다. 처음 잡은 문루 축은 기존 본채의 충돌체에 막혀 20m 남쪽의 기존 도로로 옮겼다. 씬에서 이 루트를 접어 두면 배치 전체를 찾아 편집할 수 있다. [지역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md)

| 검사 | 결과 |
|---|---|
| Unity 고정 카메라 | [마을 출구](river-basin-mainline-blockout-evidence/01-village.jpg) · [B3 굽이 전](river-basin-mainline-blockout-evidence/02-b3-before.jpg) · [교대 약 36m 전](river-basin-mainline-blockout-evidence/03-b3-first.jpg) · [E2 갈림길](river-basin-mainline-blockout-evidence/04-b3-fork.jpg) · [다리 위에서 본 폭포](river-basin-mainline-blockout-evidence/05-b3-overlook.jpg) · [B1 뒤 성채 원경](river-basin-mainline-blockout-evidence/06-keep-after-b1.jpg) · [문루 통과 구도](river-basin-mainline-blockout-evidence/07-keep-gate.jpg) · [마을 귀환 구도](river-basin-mainline-blockout-evidence/08-village-return.jpg) |
| Editor 경로·충돌 | 기존 NavMesh에서 마을→B3 955m, B3 횡단 72m, B3 갈림길→E2 상단 196m, E2 경사로 35m `PathComplete`. 성채 문루는 현재 NavMesh에서 끝점을 찾지 못했다. 대신 기존 도로를 따라 X 2238~2320의 문루 통과 구간을 플레이어 치수에 가까운 캡슐로 87곳 검사했고 장애물에 걸린 표본은 0곳이었다. [경로 검사 로그](river-basin-mainline-blockout-evidence/editor-checks.txt) |
| Editor Play 가상 패드 | 마을 북서 출구, B3 현수교, B1 석교, 성채 새 문루를 각각 시작점에서 통과했다. **4/4 통과**, 수영 전환 0회. 전체 경로의 연속 조작과 카메라 체감은 사용자 테스트 대기다. [가상 입력 로그](river-basin-mainline-blockout-evidence/play-checks.txt) |

#### 사용자 테스트 대기

저장된 4km 프리뷰 씬을 단독 Play로 열고 기본 TPS 카메라를 사용한다. 지도 마커 없이 접근 순서와 귀환 방향을 본다.

| 항목 | 재현과 기대 결과 |
|---|---|
| RB-MAIN-01 | 호수 마을 X 2320 / Z 970에서 북서 출구로 나와 B3까지 간다. 멀리서부터 계속 보이는 대신 굽이 뒤에 다리가 드러나는지, 귀로에서 마을 지붕이 방향을 잡아 주는지 확인한다. |
| RB-MAIN-02 | B3 직전 X 1656 / Z 1555에서 직진과 왼쪽 하강을 비교한다. 하강 표식을 따라 E2 상단 X 1492 / Z 1450을 찾고 협곡을 본 뒤 같은 경사로로 돌아온다. 교대의 절벽을 곧장 내려갈 길로 오인하거나 복귀 방향을 놓치면 실패다. |
| RB-MAIN-03 | B1을 건너 X 1896 / Z 1992의 갈림길에서 성채 지붕을 보고 문루 X 2270 / Z 2088까지 간다. 문루 중앙을 통과해 돌아오며 충돌이나 TPS 카메라 급당김이 있는지 확인한다. |

### 여덟 지역 회색 매스 배치

**2026-11-24 구현 반영.** 중앙 본선에 이어 [RiverBasin4km 프리뷰 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 `RiverBasin · 권역 여섯 곳 블록아웃` 아래에 Unity 기본 메시 매스 103개를 저장했다. 지역별 하위 루트에서 각각 편집할 수 있다. [지역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [확정 부지 지도](river-basin-region-sites-evidence/site-map.jpg)

| 지역 | 저장한 배치 |
|---|---|
| 깊은 숲 마을 | R1 양옆에서 기존 수목 사이를 골라 주택 세 채를 배치했다. 실개천 윤곽, 목교, 연못·사당 방향 표식을 놓았다. |
| 채석장 | 하단 작업 단구에 인양틀을 놓고 상부 요새 접근로 양쪽에 통제 시설 매스를 세웠다. |
| 던전 | 기존 절개 암석 앞 X 3215.4 / Z 2204에 서향 갱도 입면과 끊긴 운반 흔적을 배치했다. 상부 운반길 옆에 귀환 문턱 위치를 잡았다. |
| 사막 마을 | 평지붕 건물 다섯 채, 굽은 입구 양쪽 사암 매스, 골목 그늘막과 R4 하강 표식을 놓았다. |
| 항구마을 | 시장과 창고를 해안 접근로에 맞춰 배치했다. 해안 경사 끝에 부두를 잇고 방파제 두 줄, 정박선과 돛대, 등대를 놓았다. |
| 설산 밑 버려진 성채 | 고갯길 서편에 낮은 무너진 문루와 끊긴 성벽을 놓고 안뜰과 고원 오르막 분기를 표시했다. |

이번 저장 범위는 프리뷰 씬의 공간 배치다. 실개천은 윤곽 매스, 던전은 입구와 귀환 위치까지 반영했다. 내부 연결, 수계 조형, 최종 아트는 후속 작업이다. 사용자 지시에 따라 새 배치의 조망·보행·카메라·충돌·수면 검증과 캡처는 미수행이다. 작업 중 생성한 임시 파일은 없다. [월드와 아트 위키](../wiki/world-art.md)

### 여덟 권역 실물 배치와 연결

**2026-11-24 아트 배치 반영.** [RiverBasin4km 프리뷰 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)에 기존 환경 프리팹 35종을 연결했다. 호수·숲·사막 주거, 성채 문루와 시장, 채석 인양틀, 항구 부두·정박선·등대, 설산 폐성과 B3 방향 표식을 `RiverBasin · 지역 아트` 아래에 저장했다. 이전 회색 매스를 비활성 상태로 보관했다.

**채석장 던전 연결 저장.** 하단 입구를 절개 암석 앞 X 3214.65 / Z 2204에 배치했다. 프리뷰 씬 지하에 4m 모듈 바닥 18칸, 낮은 양쪽 회랑, 높은 중앙 묘실과 등잔을 배치했다. 하단 왕복과 상부 운반길 왕복에 기존 `PortalActor`를 쓴다. 상부 지름길은 던전 안에서 상호작용해 해금하며 이후 운반길에서도 들어올 수 있게 기존 포탈 활성화 플래그에 연결했다. 기존 시나리오와 퀘스트는 그대로 유지한다.

**R1 숲 물길 연결 저장.** X 1224 / Z 2308의 샘에서 시작해 X 1243 / Z 2305의 목교 아래를 지나 X 1258 / Z 2305의 자갈 여울까지 약 35m의 실개천을 조형했다. 기존 얕은 물 프로필을 수면에 연결했고, WorldBuilder 조형 명령 `074e9019b07e4cd58956c3b026459ede`에 강바닥 높이를 기록했다. [수면 메시](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/R1_ForestCreekSurface.asset) · [지역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md)

**배치 전 백업.** 씬·Terrain·월드 정의와 조형 직전 씬 사본은 `output/RiverBasinWorld/RegionArt/before`에 약 25.2MiB로 보존했다. 조형 세션 스냅샷은 WorldBuilder 저작 기록에 남겼다.

**2026-11-24 실물 배치 독립 검증.** 하단 귀환, 상부 포탈 해금·왕복과 격리 저장 재진입, 숲 목교 양방향·얕은 물 발소리 반응을 Editor Play 입력으로 확인했다. 하단에서 묘실로 향하는 회랑이 소품에 막혀 상부 포탈은 서쪽 회랑에서 별도로 시작했다. 호수 마을부터 B3·B1을 건너는 연속 입력은 성채 문루 문턱에서 막혔다. 숲 개울 두 나무의 ID 표 불일치가 수목 승격 초기화를 중단했다. 재현 좌표, 고정 화면, 입출력 로그와 사용자 테스트 대기 항목은 [독립 검증 보고](river-basin-region-verification.md)에 기록했다. [월드와 아트 위키](../wiki/world-art.md)

**2026-11-24 결함 개선·재검증.** 던전 소품을 묘실 남쪽으로 옮기고 바닥·천장 이음부와 도착 카메라를 보완했다. 하단 입장부터 묘실, 상부 해금·왕복, 격리 저장 재진입까지 걸어서 확인했다. 문루 문턱, E2 표식과 숲 표지를 보정해 기존 차단 구간을 통과했다. 수목 ID 표 두 높이를 동기화했고 목교 상판을 덮던 R1 메시와 항구 원경 돛대를 보완했다. [항목별 재검증과 비교 화면](river-basin-region-verification.md#개선-및-재검증) · [월드와 아트 위키](../wiki/world-art.md)

### 여덟 권역 1차 제작 아트 패스

**2026-11-24 프리뷰 씬 반영.** 기존 접근로 11개의 표시 메시를 지형에 맞춰 다시 입히고 권역 연결길 6개를 저작했다. 호수의 황갈색 흙, 숲의 낙엽 흙, 성채·채석장의 자갈, 사막의 단단한 모래, 해안 자갈, 설산의 눈 섞인 길에 서로 다른 바닥 재질을 적용했다. 길 가장자리에서 지형과 섞이도록 하고, 조망에서 보인 그림자 경계선을 셰이더에서 보정했다. 목교와 부두에서 길 표면이 교량 상판과 겹치지 않도록 경계를 유지했다. [지면 셰이더](../../Assets/06.Shaders/RiverBasinTrail.shader) · [메시와 재질](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinTrailSurfaces.asset)

**접근 조망과 생활 흔적.** 호수 출구의 수레·울타리, B1 너머의 낮은 노두, B3 갈림길 어깨, 성채 장터, 숲 개울가의 장작, 채석장의 운반 자리, 던전 묘실의 석관·촛대, 사막 샘터의 그늘과 항아리, 항구 하역장과 폐성의 붕괴 흔적을 기존 프리팹 38개로 배치했다. 중심 보행선은 열어 두고 전경과 가장자리에서 목적지가 보이게 했다. [프리팹·좌표·원본 경로](river-basin-production-art-evidence/asset-placement.txt) · [같은 구도 전후](river-basin-production-art-evidence/before-after.jpg) · [13개 고정 화면](river-basin-production-art-evidence/contact-sheet-1.jpg)

**Editor 정적 검증 통과.** 길 렌더러 17개와 재질 6종, 길 충돌체 0개, 기존 길 메시의 삼각형 연결 11/11 일치를 확인했다. 아트 배치 계층의 Missing Script는 0개였고, 9개 동선의 중심선과 좌우에 보행 캡슐 8,064개를 대조해 새 소품 충돌 0개를 확인했다. [정적 검사](river-basin-production-art-evidence/editor-checks.txt) · [카메라 좌표](river-basin-production-art-evidence/fixed-camera.txt)

**Editor Play 통과.** 호수 출구, B1 이후, 성채 문루, 숲 목교, 채석 단구, 사막 진입, 항구 하강, 폐성 옆 오르막, E2 하강 접근 9/9를 가상 패드로 통과했다. 던전 입장·묘실·하단 귀환 3/3과 Console 오류 0건을 확인했다. 이후 항구 상자 위 밧줄만 조정해 같은 정적 충돌 검사를 다시 통과했다. Play 종료 후 목교 상판 위 길 겹침 표본 0/335, 수목 ID 표 3,264/3,264와 기존 저장 파일 해시 일치를 확인했다. [접근로 입력](river-basin-production-art-evidence/approaches-play.txt) · [던전 입력](river-basin-production-art-evidence/quarry-play.txt) · [Console](river-basin-production-art-evidence/console.json) · [종료 검사](river-basin-production-art-evidence/final-session.txt)

작업 직전 프리뷰 씬은 `output/RiverBasinWorld/RegionArt/before-production-art-pass`에 보존했다. 미술 인수는 플레이어 시점의 초견 흐름과 시간대별 화면을 직접 확인하는 단계다. [지역별 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md)

### 건축 근접 디테일과 시간대별 조명

**2026-11-24 프리뷰 씬 반영.** 건축 렌더러 125개에 근접 재질을 연결해 목재, 지붕, 회벽, 기단과 던전 석재의 표면 차이를 살렸다. 숲 오두막·성채 상가·항구 창고에 목제 덧문 4곳을 배치했다. 재질은 씬용 복제본을 만들고 프로젝트에 있는 노멀 맵을 참조했다. [건축 전후 비교](river-basin-architecture-lighting-evidence/architecture-before-after.jpg) · [재질 출처와 배치 좌표](river-basin-architecture-lighting-evidence/authoring.txt)

호수 마을 북서 출구, 성채 문루와 장터, 숲 오두막, 사막 샘터, 갱도 입구, 항구 창고와 부두 경사에 등잔 13개를 배치했다. 등잔과 창 발광면 22개를 기존 게임 시계에 연결했다. 폐성은 기존 월드 조명에 연결한 달 방향광으로 윤곽을 드러냈다. 부두 경사와 문루의 길 표면에는 등잔 빛이 닿도록 기존 길 셰이더에 URP 추가 광원 처리를 넣었다. [시계 연동 컴포넌트](../../Assets/02.Scripts/Gameplay/World/WorldTimeFixtureGroup.cs) · [씬용 근접 아트 에셋](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinArchitectureDetails.asset)

| 판정 | 확인 방식 | 재현과 결과 |
|---|---|---|
| 통과 | Editor 정적 | 저장한 프리뷰 씬의 `RiverBasin · 처마와 생활 조명`을 확인했다. 씬용 재질 19개, 등잔 13개, 발광면 22개를 읽었고 새 충돌체·Missing Script·길 셰이더 오류는 각각 0개였다. [정적 검사](river-basin-architecture-lighting-evidence/editor-checks.txt) |
| 통과 | Editor Play | 맑은 날씨로 고정한 뒤 기존 게임 시계를 12:00 → 18:30 → 22:00 → 06:00 → 12:00으로 설정했다. 정오 소등, 해질녘 부분 점등, 야간 완전 점등과 달 활성, 새벽 감광, 정오 재소등을 5/5 확인했다. 런타임 Console 오류는 0건이다. [시간 전환](river-basin-architecture-lighting-evidence/time-play.txt) · [Console](river-basin-architecture-lighting-evidence/console.json) |
| 통과 | 고정 화면 | 9개 시점을 같은 위치로 비교했다. 성채 문루 양옆과 부두 하강 시작·끝을 밤에도 구분할 수 있고, 항구 창고 벽에 등잔 빛이 닿는다. 폐성은 달빛에서 석벽 윤곽을 확인했다. [시간대 비교](river-basin-architecture-lighting-evidence/lighting-comparison.jpg) · [폐성 야간](river-basin-architecture-lighting-evidence/night-08-alpine.jpg) |
| 실패 | 고정 화면 | X 2259.000 / Y 87.883 / Z 2090.500에서 성채 문루 안쪽을 보면 붉은 가로 부재 끝이 기둥과 떨어져 보인다. 작업 전 화면에도 나타난다. [작업 전](river-basin-architecture-lighting-evidence/before-03-keep.jpg) · [야간](river-basin-architecture-lighting-evidence/night-03-keep.jpg) |
| 미수행 | Editor Play | 이번 근접 아트 작업에서는 실제 입력 동선 재검증, 강우·안개에서의 시인성, 야간 조명 성능 실측은 수행하지 않았다. |
| 사용자 테스트 대기 | Editor Play | 마커 없는 야간 초견 동선과 플레이어 카메라의 근접 가림, 권역별 아트 인수. |

Play를 종료하고 대상 씬 한 개, 미저장 변경 0건, 기존 저장 파일 3개의 해시 일치를 확인했다. 작업 전 씬은 `output/RiverBasinWorld/RegionArt/before-architecture-lighting-pass`에 보존했다. [종료 검사](river-basin-architecture-lighting-evidence/final-session.txt) · [월드와 아트 위키](../wiki/world-art.md)

### 접근로 접지와 성채 문루 보정

**2026-11-24 프리뷰 씬 반영.** 성채 문루의 동쪽 붉은 부재는 `StonegateEast/Castle_Gate.prefab` 모델에 포함된 문패다. 동쪽 아치의 안쪽 면이 비어 있어 서쪽에서 문패 뒷면이 하늘 위로 노출됐다. 이 인스턴스에 파생 메시를 연결해 문패 양끝을 석재까지 연장하고 안쪽 아치 면을 채웠다. 측면 TPS 시점에서 문루가 페이드될 때 따로 남던 등잔 두 개를 기존 문루 가림 그룹에 포함했다. [문루 파생 메시](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinGateLintel.asset) · [등잔 연결 근거](river-basin-approach-art-evidence/fixture-links.txt)

호수 북서 출구부터 B3·B1과 성채 문루까지 길 옆에 흙 마모·낮은 돌·풀을 붙였다. 항구는 창고 하역 자리의 상자와 밧줄이 부두 입구의 적재 자리로 이어지게 배치하고, 경사 입구 표지에 등잔을 추가했다. 기존 프리팹 인스턴스 39개와 기존 길 재질을 참조하는 마모 메시 15개를 적용했다. 사막 마을에서는 흙집 석재 기단 밑면을 경사에 맞춰 최대 0.872m 연장했다. 반지하 가옥은 0.57m 올리고 외벽에서 분리돼 있던 장식 차양을 비활성화했다. [소품 배치와 좌표](river-basin-approach-art-evidence/placements.txt) · [돌·밧줄 최종 배치](river-basin-approach-art-evidence/refined-props.txt) · [지면 마모](river-basin-approach-art-evidence/wear-surfaces.txt) · [사막 접지](river-basin-approach-art-evidence/desert-contact.txt)

| 주요 지점 | 월드 X / Y / Z | 확인 방법과 관찰 결과 | 근거 |
|---|---|---|---|
| 문루 서쪽 접근과 동쪽 문패 | 촬영 2259.000 / 87.883 / 2090.500, 문패 2274.086 / 91.980 / 2087.206 | 서쪽에서 붉은 뒷면 대신 석재 아치가 보인다. 동쪽은 문패가 석재에 붙어 읽힌다. 서쪽에서 올려 보면 아치 모서리에 작은 틈이 남는다. | [서쪽 전](river-basin-approach-art-evidence/before-gate.jpg) · [서쪽 후](river-basin-approach-art-evidence/after-gate.jpg) · [동쪽 전](river-basin-approach-art-evidence/before-gate-east.jpg) · [동쪽 후](river-basin-approach-art-evidence/after-gate-east.jpg) |
| 호수 북서 출구 | 돌 2312.900 / 36.039 / 976.500 | 길이 울타리와 흙 마모 사이에서 이어지는 것을 고정 시점으로 확인했다. 가상 패드로 마을에서 출구까지 걸었다. | [전](river-basin-approach-art-evidence/before-lake.jpg) · [후](river-basin-approach-art-evidence/after-lake.jpg) |
| B3 갈림길과 현수교 | 갈림길 돌 1654.100 / 48.870 / 1549.400, 첫 조망 1648.000 / 50.700 / 1580.000 | 갈림길 표지와 낮은 길 어깨가 함께 보인다. 첫 조망에서 현수교 입구와 상판이 보인다. E2 접근과 현수교 횡단을 개별 입력 경로로 확인했다. | [갈림길 전](river-basin-approach-art-evidence/before-fork.jpg) · [갈림길 후](river-basin-approach-art-evidence/after-fork.jpg) · [현수교 전](river-basin-approach-art-evidence/before-b3-first.jpg) · [현수교 후](river-basin-approach-art-evidence/after-b3-first.jpg) |
| B1 너머 성채 방향 | 돌 1900.800 / 60.290 / 1991.700, 촬영 1896.000 / 62.422 / 1992.000 | 길 옆 접지 디테일 위로 성채 지붕이 보인다. B1 이후 접근 일부를 가상 패드로 걸었다. | [전](river-basin-approach-art-evidence/before-b1.jpg) · [후](river-basin-approach-art-evidence/after-b1.jpg) |
| 항구 창고·시장 진입 | 통 2811.800 / 9.980 / 249.350, 등잔 2820.250 / 11.915 / 245.200 | 하역 소품이 창고 옆의 일하는 자리를 만든다. 어둠에서 실루엣만 잡히던 시장 표지에 월드 시계 연동 등잔을 배치했다. | [시장 전](river-basin-approach-art-evidence/before-harbor-top.jpg) · [시장 후](river-basin-approach-art-evidence/after-harbor-top.jpg) |
| 항구 경사와 부두 입구 | 경사 돌 2829.900 / 8.739 / 197.600, 부두 상자 2821.200 / 1.721 / 162.000 | 경사 끝에 인접한 하역 소품과 입구 등잔을 고정 화면에서 확인했다. 가상 패드로 상판까지 내려갔다. 상판에 보이는 작은 얼룩은 작업 전후에 남는다. | [경사 전](river-basin-approach-art-evidence/before-harbor-mid.jpg) · [경사 후](river-basin-approach-art-evidence/after-harbor-mid.jpg) · [입구 전](river-basin-approach-art-evidence/before-harbor-entry.jpg) · [입구 후](river-basin-approach-art-evidence/after-harbor-entry.jpg) |
| 사막 흙집 | 기단 3620.000 / 65.442 / 1516.000, 반지하 가옥 3631.000 / 63.204 / 1525.000 | 고정 시점에서 흙집 기단 아래의 틈이 닫혔다. 반지하 가옥의 사면 교차와 이웃 원경 소품에는 추가 확인이 필요하다. | [기단 전](river-basin-approach-art-evidence/before-desert-footing.jpg) · [기단 후](river-basin-approach-art-evidence/after-desert-footing.jpg) · [반지하 가옥 전](river-basin-approach-art-evidence/before-desert-awning.jpg) · [반지하 가옥 후](river-basin-approach-art-evidence/after-desert-awning.jpg) |

깊은 숲 남쪽 접근, 채석장 작업 단구, 던전 묘실, 설산 폐성 오르막은 대표 고정 화면으로 검토했다. 전수 배치 검사와 수관 아래 전 구간의 근접 확인은 남아 있다. 주 동선과 조망 앵커는 기존 콘셉트의 위치를 사용했다. [숲](river-basin-approach-art-evidence/before-forest.jpg) · [채석장](river-basin-approach-art-evidence/before-quarry.jpg) · [묘실](river-basin-approach-art-evidence/before-dungeon.jpg) · [폐성](river-basin-approach-art-evidence/before-alpine.jpg) · [권역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md)

**Editor 정적 검사.** 변경한 충돌체를 기준으로 접근로 여섯 곳에서 보행 캡슐 3,336개를 대조해 교차 0건을 확인했다. 지형 높이 기준 검사의 부두 상판 표본 78개는 제외하고 입력 테스트에서 확인했다. 마모 메시 충돌체·누락 스크립트·조명 참조 누락은 각각 0개다. 문루와 흙집 기단의 렌더·충돌 메시가 일치하고, 문루 중심 위쪽 레이가 지붕 충돌체에 닿았다. [정적 검사](river-basin-approach-art-evidence/editor-checks.txt)

**Editor Play 1차.** 시작 위치를 옮긴 뒤 가상 패드 왼쪽 스틱을 실제 Input System에 넣었다. 호수 북서 출구, E2 갈림길, B3 횡단, B1 이후, 문루 양방향, 항구 해질녘·야간 진입, 사막 마을 접근 9/9를 통과했다. 문루 양방향 입력에서 문루의 페이드 동작과 등잔 분리를 관찰했다. [1차 입력 로그](river-basin-approach-art-evidence/approaches-play.txt)

**Editor Play 최종.** 문루 양방향, 해질녘·야간 항구, 사막 진입 5/5를 재통과했다. 문루의 가림이 걸린 프레임에서 캐릭터가 보였고 등잔도 문루와 함께 페이드됐다. 두 방향에서 카메라와 캐릭터 머리 사이의 레이가 문루에 닿은 프레임은 19개와 20개였다. 기존 페이드의 희미한 등잔 윤곽은 남는다. [최종 입력 로그](river-basin-approach-art-evidence/final-approaches-play.txt) · [가림 수정 전](river-basin-approach-art-evidence/tps-gate-west-east-2.jpg) · [서→동 가림 수정 후](river-basin-approach-art-evidence/tps-final-gate-west-east-2.jpg) · [동→서 가림 수정 후](river-basin-approach-art-evidence/tps-final-gate-east-west-2.jpg)

**항구 시간대 비교.** 맑은 날씨로 게임 시계를 18:30과 22:00에 맞췄다. 시장 등잔이 표지판과 길 가장자리를 비추며 경사 표지, 부두 입구 등잔과 상판이 이어서 보인다. 시장에서 상판까지 입력으로 내려간 위치는 각각 (2825.995, 0.974, 153.663), (2825.994, 0.975, 153.670)이다. 1차와 최종 Unity Console에서 오류는 각각 0건이었다. [야간 표지 전](river-basin-approach-art-evidence/pre-sign-night-harbor-sign.jpg) · [야간 표지 후](river-basin-approach-art-evidence/final-night-harbor-sign.jpg) · [해질녘 표지](river-basin-approach-art-evidence/final-dusk-harbor-sign.jpg) · [해질녘 부두 입구](river-basin-approach-art-evidence/final-dusk-harbor-entry.jpg) · [야간 부두 입구](river-basin-approach-art-evidence/final-night-harbor-entry.jpg) · [시간대·시점 로그](river-basin-approach-art-evidence/final-harbor-time.txt) · [Console](river-basin-approach-art-evidence/console.json)

**미수행·사용자 테스트 대기.** 강우·안개 시인성, 조명 성능 실측과 중앙 본선 전체 연속 주행은 미수행이다. 마커 없는 야간 초견 길 찾기와 권역별 미술 인수는 사용자 테스트 대기다.

사용자 테스트 `RB-NIGHT-01`은 RiverBasin4km Editor Play의 맑은 22시와 기본 TPS 카메라에서 지도 마커를 끄고 시작한다. 호수 북서 출구에서 B3 본선·E2 분기를 읽고 현수교와 B1을 거쳐 성채를 찾는다. 항구에서는 시장에서 배가 있는 부두를 찾아 내려간다. 되돌아가거나 지도를 열게 된 첫 지점, 문루 가까이에서 캐릭터가 가려진 지점이 있으면 입력 장치와 화면을 함께 남긴다.

작업 시작 시 열린 Editor에서 대상 씬 한 개와 Play 종료 상태를 확인했다. 디스크와 열린 씬 사본을 대조해 사용자가 조정 중이던 바위 인스턴스 네 곳의 Y 차이를 식별했다. 이번 아트 변경만 디스크에 저장하고 네 곳의 열린 값은 Editor에 다시 적용했다. 두 사본은 `output/RiverBasinWorld/RegionArt/before-approach-contact-pass/`에 보관했다. 최종 대조에서 이 네 곳의 높이만 저장본과 열린 씬 사이에 차이가 났다. Editor Play를 종료했고 게임 저장 파일 5개의 해시가 일치했다. [종료 검사](river-basin-approach-art-evidence/final-session.txt) · [주간 전후 모음](river-basin-approach-art-evidence/day-before-after.jpg) · [촬영 좌표](river-basin-approach-art-evidence/fixed-camera.txt) · [월드와 아트 위키](../wiki/world-art.md)

### 문루·부두·반지하 가옥과 중앙 본선 연결

**2026-11-25 프리뷰 씬 반영.** 11월 24일 비교 화면의 문루 모서리 틈, 부두 상판의 검은 자국, 사막 가옥 주변의 들뜸을 재현했다. 한 번의 중앙 본선 주행에서 호수 북서 출구의 흙길이 잔디 앞에서 끝나고 B3 서쪽에서 다음 길이 흐려지는 것을 확인했다. 설계의 첫 조망과 접근 방향을 따라 본선 지표를 잇고 B3 서쪽의 길 위 식생을 양옆으로 옮겼다. [권역 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_REGION_CONCEPT_DESIGN.md) · [메시와 배치 기록](river-basin-local-defects-evidence/mesh-adjustments.txt)

| 주요 지점 | 월드 X / Y / Z (m) | 정적 확인과 반영 결과 | 화면 |
|---|---|---|---|
| 성채 문루 서·동쪽 | 서쪽 카메라 2259.000 / 87.883 / 2090.500, 동쪽 카메라 2283.000 / 90.350 / 2085.700 | 서쪽에서 비치던 하늘색 틈 두 곳을 재현했다. 씬 전용 아치의 양쪽 가장자리 정점 네 개를 옆 석재 안으로 연장했다. 동쪽에서 문패와 아치 통로를 비교했다. | [서쪽 전](river-basin-local-defects-evidence/before-gate-west.jpg) · [서쪽 후](river-basin-local-defects-evidence/after-gate-west.jpg) · [동쪽 전](river-basin-local-defects-evidence/before-gate-east.jpg) · [동쪽 후](river-basin-local-defects-evidence/after-gate-east.jpg) |
| 항구 안벽과 목재 상판 | 입구 카메라 2826.100 / 5.150 / 174.000, 상판 근접 카메라 2826.100 / 2.450 / 153.500, 안벽 중심 2826.000 / 1.460 / 158.400 | 그림자를 꺼도 검은 자국이 남았다. 근접 시 자국이 판재와 석판 사이의 열린 줄눈으로 이어졌다. 목재 모듈 다섯 개와 경사진 안벽 한 곳의 상면이 맞닿도록 메시를 보정했다. 입구와 근접 구도에서 끊긴 검은 줄이 사라졌다. | [그림자 끔](river-basin-local-defects-evidence/no-shadows-harbor-close.jpg) · [입구 전](river-basin-local-defects-evidence/before-harbor-entry.jpg) · [입구 후](river-basin-local-defects-evidence/after-harbor-entry.jpg) · [목재 근접 전](river-basin-local-defects-evidence/before-harbor-close.jpg) · [목재 근접 후](river-basin-local-defects-evidence/after-harbor-close.jpg) · [석재 근접 전](river-basin-local-defects-evidence/before-quay-close.jpg) · [석재 근접 후](river-basin-local-defects-evidence/after-quay-close.jpg) |
| 반지하 가옥과 이웃 흙집 | 가옥 3631.000 / 63.204 / 1525.000, 이웃 짚단 3620.271 / 64.673 / 1522.514 및 3621.324 / 64.572 / 1522.011 | 남쪽 오르막에서 흙더미가 사면에 잠기는 부분은 낮은 반지하 실루엣을 만든다. 북쪽 출입면과 동쪽 판벽의 하단 틈은 별도 결함이었다. 판벽과 지주 하단을 지면에 맞추고 돌 여섯 개, 장작 더미, 항아리와 뚜껑, 이웃 짚단 두 개를 내렸다. 출입구 발 아래의 열린 공간과 지붕 높이는 고정 구도로 확인했다. | [남동쪽 전](river-basin-local-defects-evidence/before-desert-awning.jpg) · [남동쪽 후](river-basin-local-defects-evidence/after-desert-awning.jpg) · [북쪽 전](river-basin-local-defects-evidence/before-desert-entry.jpg) · [북쪽 후](river-basin-local-defects-evidence/after-desert-entry.jpg) · [이웃 기단 전](river-basin-local-defects-evidence/before-desert-footing.jpg) · [이웃 기단 후](river-basin-local-defects-evidence/after-desert-footing.jpg) |
| 호수 북서 출구와 B3/E2 분기 | 출구 끝 2282.000 / 37.207 / 1011.000, 갈림길 주행 1656.671 / 48.768 / 1554.355, 현수교 첫 조망 1648.000 / 50.700 / 1580.000 | 북서 출구의 흙길을 동쪽 교대까지 928.2m 이었다. 황갈색 본선이 교대로 굽고 회색 E2 하강길은 표지 옆으로 갈린다. 첫 조망에서 현수교 입구와 상판을 확인했다. | [출구 전](river-basin-local-defects-evidence/before-lake-continuity.jpg) · [출구 후](river-basin-local-defects-evidence/after-lake-continuity.jpg) · [분기 전](river-basin-local-defects-evidence/before-fork-entry.jpg) · [분기 후](river-basin-local-defects-evidence/after-fork-entry.jpg) · [첫 조망 전](river-basin-local-defects-evidence/before-b3-first.jpg) · [첫 조망 후](river-basin-local-defects-evidence/after-b3-first.jpg) |
| B3 서쪽 교대와 B1 이후 | 목서 이동 후 1600.800 / 47.982 / 1657.800, B1 상판 주행 1739.651 / 52.260 / 1859.719, 성채 조망 카메라 1896.000 / 62.422 / 1992.000 | B3 서쪽에서 B1까지 244.0m의 회색 길을 이었다. 본선 중심을 가리던 목서 한 그루와 관목 두 개를 길 어깨로 옮겼다. B1 이후 흙길 분기와 능선 위 성채 지붕이 함께 보인다. | [서쪽 교대 전](river-basin-local-defects-evidence/before-b3-west.jpg) · [서쪽 교대 후](river-basin-local-defects-evidence/after-b3-west.jpg) · [B1 접근 후](river-basin-local-defects-evidence/after-b1-approach.jpg) · [성채 조망](river-basin-local-defects-evidence/after-b1-view.jpg) |

**Editor 정적 검사.** 문루와 반지하 가옥의 렌더·충돌 메시가 일치했다. 서쪽에서 틈이 보이던 두 픽셀의 레이가 보정한 아치에 닿았고, 지붕 수직 레이도 충돌했다. 부두 6개 인스턴스의 BoxCollider 참조와 지면 높이를 확인했다. 본선 지표 두 메시의 정점 13,706개는 지형 표본보다 4.1cm 높고, 지표 충돌체는 0개다. B3 동쪽 교대와 B1 진입 끝단을 제외한 본선 중심 캡슐 485개에서 지형 외 충돌 겹침이 없었다. 누락 스크립트와 시작·종료 Console 오류는 각각 0건이다. [정적 검사](river-basin-local-defects-evidence/editor-checks.txt) · [고정 카메라 좌표](river-basin-local-defects-evidence/camera-views.txt) · [Console](river-basin-local-defects-evidence/console.txt)

**중앙 본선 연속 Editor Play.** 세 건의 국소 보정을 적용한 뒤 길 표면을 추가하기 전에 주행했다. 호수 마을 (2320.000, 36.137, 970.000)에서 한 번 위치를 설정하고 가상 Gamepad로 북서 출구, B3/E2 갈림길, 현수교, B1 석교, 성채 문루를 이어서 통과했다. 도착점은 (2305.440, 89.083, 2085.072)이며 이동 1,940.8m, 312.6초, 수영 0프레임이다. 이동 중 TPS 카메라가 한동안 아래를 향해 가상 패드 오른쪽 스틱 입력으로 시점을 되돌렸다. 도중 위치 재설정은 0회였다. [연속 입력 로그](river-basin-local-defects-evidence/main-route-play.txt) · [현수교 첫 노출](river-basin-local-defects-evidence/route-b3-first.jpg) · [B1 이후](river-basin-local-defects-evidence/route-b1-vista.jpg) · [성채 도착](river-basin-local-defects-evidence/route-keep-in.jpg)

**저장본 구간별 Editor Play.** 가상 Gamepad와 기본 TPS를 사용했다. 각 구간의 시작점만 설정했다.

- 문루는 양방향으로 통과했다. 측면 시점에서 카메라와 캐릭터 머리 사이의 레이가 문루에 닿은 프레임은 서→동 43개, 동→서 10개다. 해당 화면에서 캐릭터가 보였고 등잔은 문루와 함께 페이드됐다. 희미한 등잔 윤곽은 남는다. [서→동](river-basin-local-defects-evidence/gate-west-east-occlusion.jpg) · [동→서](river-basin-local-defects-evidence/gate-east-west-occlusion.jpg)
- 항구는 맑은 18시 33분과 22시 23분 고정 화면에서 시장 표지 등잔, 경사, 부두 입구의 연결을 확인했다. 두 시간대에 (2816.000, 10.135, 248.000)에서 경사진 석재 안벽과 목재 모듈 이음부를 건너 상판 안쪽 (2826.000, 0.850, 약 139.74)까지 걸었다. 수영은 두 번 모두 0프레임이다. [해질녘 표지](river-basin-local-defects-evidence/harbor-dusk-sign.jpg) · [경사](river-basin-local-defects-evidence/harbor-dusk-slope.jpg) · [해질녘 접합부](river-basin-local-defects-evidence/harbor-dusk-5.jpg) · [야간 표지](river-basin-local-defects-evidence/harbor-night-sign.jpg) · [야간 입구](river-basin-local-defects-evidence/harbor-night-entry.jpg) · [야간 접합부](river-basin-local-defects-evidence/harbor-night-5.jpg)
- 북서 출구에서 (2221.524, 38.791, 1097.442)까지 새 흙길을 따라갔다. 별도 갈림길 재주행에서는 (1661.031, 48.948, 1548.039)에서 바위 충돌체에 걸렸다. 바위를 (1660.600, 48.833, 1548.900)에서 (1664.100, 48.853, 1551.300)으로 옮긴 뒤 같은 시작점부터 갈림길, B3, B1, 성채 지붕 조망점 (1895.435, 59.993, 1991.505)까지 이어 달렸다. E2는 본선에서 갈리는 방향을 확인했다. [호수 출구 재검증](river-basin-local-defects-evidence/lake-recheck-1.jpg) · [바위 전](river-basin-local-defects-evidence/before-fork-boulder.jpg) · [바위 후](river-basin-local-defects-evidence/after-fork-boulder.jpg) · [현수교 첫 노출](river-basin-local-defects-evidence/b3-b1-recheck-2.jpg) · [B1 이후 지붕](river-basin-local-defects-evidence/b3-b1-recheck-17.jpg)
- 사막에서는 가옥 북쪽 출입면을 따라 (3640.000, 61.378, 1531.800)에서 (3628.737, 62.478, 1529.891)까지 걸어 판벽, 돌과 항아리 접지를 확인했다. [TPS 화면](river-basin-local-defects-evidence/desert-front-3.jpg)

[문루·항구·호수 구간 및 최초 B3 실패 로그](river-basin-local-defects-evidence/final-sections-play.txt) · [바위 보정 후 B3~B1 및 사막 통과 로그](river-basin-local-defects-evidence/fork-and-desert-play.txt)

**미검증 및 사용자 테스트 대기.** 저장본 전체를 한 번에 다시 주행하는 검증, E2 하강로 내부 주행, 강우·안개 시인성과 야간 조명 성능 계측은 미수행이다. 마커 없는 야간 초견 길 찾기와 아트 인수는 `RB-NIGHT-01` 사용자 테스트 대기다.

**Editor 상태와 백업.** 단일 Unity Editor에서 대상 씬 한 개와 Edit Mode를 확인했다. 디스크와 열린 씬을 각각 `output/RiverBasinWorld/RegionArt/before-local-defects-20261125-211104/`의 `disk.unity`, `open.unity`로 백업했다. 두 SHA-256 해시가 일치했고, 직전 작업에서 열려 있던 바위 네 곳의 Y 차이는 이번 시작 상태에 없었다. Play 종료 후 열린 씬 사본과 저장본의 해시도 일치했다. 대상 씬을 Edit Mode로 다시 열었으며 저장 슬롯 파일 5개의 해시는 작업 전과 같다. [월드와 아트 위키](../wiki/world-art.md)

### 4km 저지대 전역 기복

**2026-11-23 구현 반영.** “산지가 아닌 이상 대체로 너무 평탄해”, “언덕도 없고 경사 높은 지역 없고 ‘탐험’할 맛이 없어”라는 제보를 반영했다. 남부 해안, 서부 숲, 중앙 분지, 동부 사막과 산기슭에 비대칭 능선과 안부 스물네 곳을 조형했다. Terrain 64장 중 51장에 영향을 주며, 기본 조형에서 2m 이상 높이 변화 구역은 약 2.48㎢, 최대 상승은 약 32m다. WorldBuilder AddDelta 패치 16개를 저장하고, 새 급사면에는 기존 권역 재질을 섞었다. 동부 바위 보호 경계가 둥글게 드러난 곳은 사면으로 연결하고 소품 세 개를 재접지했다. 지면 높이에 따라 수목 7,229개의 높이와 승격 에셋 10개를 동기화했다. [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#저지대-전역의-능선과-골짜기) · [좌표](../../Tools/RiverBasinWorld/Rebuild/world-relief-4km.csv) · [WorldDefinition](../../Assets/10.Datas/WorldBuilder/6effd90b68ea4b3d8f8caa5197fcfe34_Authoring.asset)

| 권역 | 조형 영향 범위에서 경사 24° 이상인 4m 격자 표본 | 적용 후 목표 경험 |
|---|---:|---|
| 남부 해안 | 707 → 6,110 | 만에서 능선 너머 다음 해안 조망 |
| 서부 숲 | 1,876 → 8,162 | 숲 안부와 연못 전망 사이의 시야 변화 |
| 중앙 분지 | 4,491 → 9,473 | 낮은 본선과 능선 조망길 사이 선택 |
| 동부 사막·산기슭 | 3,311 → 6,673 | 사암 사면을 돌아 좁은 골짜기로 내려가기 |

**Editor 검사 통과.** 저장본 높이 표본 16,641개와 조형값을 대조해 최대 편차 2.33cm를 확인했다. 수계 보호 표본 2,568개와 동선·거점 보호 표본 603개의 높이 편차는 0.04mm 미만이었다. Terrain 경계 112곳의 높이 차는 0m다. 길과 B3 현수교를 포함한 NavMesh 네 경로가 모두 `PathComplete`였다. 수목 데이터 18,666개에서 위치·프로토타입 불일치가 0건이며 배치 ID 순서도 보존했다. 저장 자산 해시가 달라진 조형 영역 밖 타일 다섯 장은 높이 편차 0.04mm 미만, Terrain Hole 변경 0건을 확인했다. [사전 계산](river-basin-world-relief-evidence/preflight.txt) · [저장본 검사](river-basin-world-relief-evidence/checks.json)

**Unity 화면과 지도.** 권역별 같은 조감 위치에서 촬영했고, 전역 미니맵을 4096²로 다시 만들었다. 기존 지도 스프라이트 GUID와 중심 좌표를 확인했다. [권역별 전후 비교](river-basin-world-relief-evidence/before-after.jpg) · [중앙 보행 높이 적용 전](river-basin-world-relief-evidence/central-ground-before.jpg) · [중앙 보행 높이 적용 후](river-basin-world-relief-evidence/central-ground-after.jpg) · [갱신한 전역 지도](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)

**RB-RELIEF01 · 사용자 테스트 대기.** `RiverBasin4km` Editor Play에서 새 저장을 만들 필요 없이 기존 플레이어와 평소 입력 장치로 확인한다. 중앙 분지 X/Z `(2508, 1550)` → `(2562, 1626)`, 남부 해안 `(1398, 364)` → `(1557, 274)`에서 낮은 쪽으로 우회한 뒤 사면을 오른다. 서부 `(1069, 2214)`와 동부 `(3641, 1794)`에서도 양쪽 경사를 비교한다. 올라갈 방향을 알아볼 수 있고, 능선 위에 도착했을 때 이전에 가려졌던 풍경이 보여야 한다. 길이 막히거나 경사가 읽히지 않으면 `RB-RELIEF01 / 좌표 / 진행 방향 / 증상`으로 기록한다. 실제 조작·카메라 품질과 전역 성능, Player·Addressables 빌드는 미수행이다.

#### 능선 발치의 인공 홈 보정

사용자 제보 “이 인위적인 파임은 뭐임”의 형태와 같은 홈을 서부 숲 남쪽에서 확인했다. 전역 기복 패스가 능선 한쪽에 적용한 절토가 떠 있는 물길처럼 길고 좁게 드러났다. 비교 위치 X/Z `(832, 1616)`은 전역 조형 전 56.87m에서 52.87m로 내려갔다. 첨부 이미지 각각의 좌표는 확인하지 못했다.

전역 기복 패치의 음수 높이값만 취소하는 AddDelta 패치 15개를 저장했다. Terrain 45장의 1m 표본 753,678개를 복원했고, 비교 지점은 56.87m로 돌아왔다. 조형 규격의 절토 입력을 0m로 갱신하고 수목 높이와 미니맵을 다시 맞췄다. Editor 검사에서 복원 격자 최대 편차 2.29cm, 보정 영역 밖 검사 표본의 높이 변화 0m, 타일 경계 112곳의 오차 0m를 확인했다. NavMesh 경로 4개와 수목 배치 18,666개가 통과했다. 사용자 제보 구도의 재확인과 실제 조작은 대기다. [검사 수치](river-basin-world-relief-evidence/groove-checks.json) · [갱신한 미니맵](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)

#### Terrain_3_2 선택 화면의 동쪽 홈 보정

첨부 구도를 Unity에서 재현했다. 선택 윤곽은 `Terrain_3_2`이지만, 원으로 표시된 홈은 동쪽 이웃 `Terrain_4_2`의 X 2094.79 / Y 40.20 / Z 1304.83 부근에 있다. 홈 양쪽 사면의 높이와 경사를 연결하는 WorldBuilder AddDelta 패치를 적용했다. 주변 바위 네 개는 당시 지면 대비 높이차를 유지하도록 Y를 맞췄다. [조감·근접 전후](river-basin-tile-3-2-evidence/before-after.jpg) · [WorldBuilder 패치](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/Patches/Terrain_4_2_ShallowGroove_2095_1305.bytes)

**Editor 정적·고정 화면 통과.** 1m 높이 표본 544개를 보정했다. 대표 횡단면의 1m당 최대 높이차는 0.656m에서 0.072m로 낮아졌다. 보정 마스크 밖 높이 변화와 인접 타일 네 변의 경계 오차는 0m다. 지형 충돌체 수직 Raycast 15/15와 `Terrain_3_2` 에셋 해시 일치를 확인했다. 씬 파일은 백업 대비 바위 네 개의 Y만 달라졌다. Editor Play 보행은 미수행이다. [보정 수치](river-basin-tile-3-2-evidence/checks.txt) · [경계와 충돌체 검사](river-basin-tile-3-2-evidence/final-editor-check.txt) · [월드와 아트 위키](../wiki/world-art.md)

### 4km 남부 호수 섬

**FEEL-RB-LAKES-01 · 경관과 이동 방식.** 사용자 피드백을 두 호수 섬과 R4 남안에 반영했다.

> 1. 다리만 덜렁 있는게 이뻐 보이지 않는다.
> 2. 섬만 덜렁 있는게 이뻐보이지 않는다.
> 3. 다리만 너무 길게 배치되어 예쁘지 않다.
> 이동 수단은 다리가 아니라 헤엄도 있고 공중 날기도 있으니 다리는 굳이 없어도 될 것 같다

**구현 반영.** 호수 보행교 10개와 52m B5 보행교를 철거했다. 서남쪽 호수는 버드나무와 자갈, 호수 마을 쪽은 넓은 수관과 갈대를 중심으로 배치했다. 섬 여덟 곳과 호안·유출 하천 양안에 구매 수목·바위·갈대 프리팹 502개를 군락으로 두고 상륙 방향을 비워 뒀다. 기존 수면 메시 10개에 수영 조회와 얕은 물 발소리 프로필을 연결했다. B1 석교와 기존 Sculpt 지형을 보존했다. [저작 스크립트](../../Tools/RiverBasinWorld/Rebuild/lake-islands-swim-art.cs.txt) · [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#남부-호수의-섬과-수영-동선)

**Editor 검사 통과.** 저장 씬을 다시 열어 횡단 11곳에서 중심과 좌우 1.8m의 단면을 확인했다. 양쪽 마른 땅과 수심 1.2m 이상 구간이 이어진 표본은 33/33이고, 경로 표본 위 충돌물은 0건이다. B5가 있던 구간은 깊은 물 약 22m, 양안 최대 종경사 23.7°다. 씬 레코드 감사에서 삭제는 교량 계층 두 곳에 한정됐다. 재촬영한 미니맵은 기존 GUID와 4096×4096 BC7 규격을 유지한다. [수치 검사](river-basin-lakes-evidence/swim-shore-editor-audit.txt) · [미니맵](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)

| Unity 고정 구도 | 변경 전 | 변경 후 |
|---|---|---|
| 서남쪽 호수 | [교량과 작은 섬](river-basin-lakes-evidence/west-before.png) | [버드나무와 상륙면](river-basin-lakes-evidence/west-after.png) |
| 호수 마을 | [교량과 초지 섬](river-basin-lakes-evidence/east-before.png) | [갈대와 중앙섬 수관](river-basin-lakes-evidence/east-after.png) |
| 유출 하천 | [B5 보행교](river-basin-lakes-evidence/outlet-before.png) | [양안의 수영 진입부](river-basin-lakes-evidence/outlet-after.png) |

**RB-ISLAND01 · 사용자 테스트 대기.** `RiverBasin4km`에서 서남쪽 호수 북안 (949, 1150) → 섬 → 남안 (1100, 550), 호수 마을 서안 (2452, 1065) → 섬 → 남안 (2900, 693)을 키보드 또는 게임패드로 수영한다. 역방향도 확인하고, 비행 가능한 상태라면 중앙섬의 열린 구간에 착지한다. 입수·출수가 자연스럽고 낮은 자갈면에서 카메라 가림이나 걸림 없이 움직이는지 본다. `RB-ISLAND01 / 호수와 방향 / 통과·실패·미수행 / 실제 증상 / 화면 또는 로그`로 기록한다. 실제 Play Mode 이동 계측과 성능 교차 검사는 미수행이다.

**보존과 정리.** 원래 섬 모양은 [초기 Sculpt 패치](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/Patches/1e41f2f3dbdb49c49aec4918b6beb653.bytes)에 있다. 적용 전 씬과 미니맵 사본은 로컬 `output/RiverBasinLakes/`의 `before-20261122-100305-173.unity`와 `minimap-original.png`에 보존했고, 중간본 10개(59.07MB)를 정리했다.

### 4km 미니맵

- **구현 반영.** [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 `SceneContext.WorldMapConfig`에 [전용 설정](../../Assets/10.Datas/Map/RiverBasin4km/MinimapConfig.asset)을 연결하고 탐험 HUD를 켰다. 기존 호수 월드의 아이콘과 표시 범위를 기준으로 미니맵은 한 변 약 90m를 보여준다. 탐험 HUD 설정은 월드 시계와 NPC 주변 대사에도 적용된다.
- **지도 원본.** 현재 Unity 씬의 64개 Terrain을 X/Z 0~4096m, 중심 (2048, 2048)에서 정북 기준으로 촬영했다. 지도에서 수계를 읽기 쉽도록 촬영 중에만 물 표면 42개를 파란색으로 표시하고 원래 재질을 복원했다. [배경 PNG](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)는 4096×4096, BC7, 밉맵 13단계로 임포트됐다. 수목 디테일은 촬영 이미지에서 희미하다.
- **Editor 검증 통과.** 저장된 씬의 지도 GUID를 확인했다. 씬 단독 Play Mode에서 HUD가 자동으로 표시됐고, 게임 HUD의 지도 토글을 호출해 전체 지도도 같은 스프라이트와 월드 좌표 설정을 사용함을 확인했다. 월드 북동 모서리 (4096, 4096)는 지도 중심 기준 (2048, 2048)로 변환됐다.
- **RB-MAP01 · 사용자 테스트 대기.** `RiverBasin4km` 씬을 열고 별도 저장 불러오기 없이 Play한다. B1 진입로에서 북쪽으로 이동하고 회전하며 미니맵의 플레이어 표시와 강·석교 위치를 비교한다. 키보드 `M`으로 전체 지도를 열어 현재 위치와 북쪽 상류·동부 사막의 방향이 맞는지 보고 다시 닫는다. 검은 배경, 위치 어긋남, 지도 열기·닫기 실패가 있으면 `RB-MAP01 / 결과 / 발생 위치와 증상 / 화면 또는 로그`로 기록한다.

### 4km 미니맵 재촬영과 Ctrl 이동

- **2026-11-23 전역 재촬영.** B3 사면 반영 후 저장된 `RiverBasin4km`의 Terrain 64개를 정북 기준으로 다시 촬영하고, 탐험로와 B3 본선 점선을 원본 경로로 다시 그렸다. 수면과 낙수 렌더러 56개는 촬영 중에만 파랑으로 표시했다. Unity에서 기존 지도 GUID·4096×4096·BC7·밉맵 13단계와 중심 X/Z (2048, 2048)를 확인했다. PNG `.meta` 해시는 동일하고 촬영 후 씬은 저장 상태를 유지했다. 새 배경의 실제 HUD 확인은 `RB-MAP02` 사용자 테스트 대기다. [지도 PNG](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png) · [직전 사면 작업](river-basin-gorge-relief.md#b3에서-호수-마을로-이어지는-사면--2026-11-23)
- **구현 반영.** 2026-11-22에 열린 `RiverBasin4km` 씬의 활성 Terrain 64개를 정북 기준으로 다시 촬영했다. 수면은 촬영 중에만 밝은 파랑으로 표시했다. [지도 PNG](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)는 4096×4096, BC7, 밉맵 13단계로 임포트됐다. 기존 스프라이트 참조, 중심 X/Z (2048, 2048), 범위 4096×4096m를 유지했다.
- **Editor Play Mode 통과.** HUD의 지도 토글을 호출해 전체 지도를 열고 새 배경의 에셋 경로를 확인했다. 가상 좌측 Ctrl과 EventSystem 클릭으로 X/Z (1780.97, 1887.47)에 이동했다. Game View 포커스를 설정한 뒤 가상 우측 Ctrl과 마우스 클릭을 UI 입력 모듈로 처리해 X/Z (1687.97, 1863.47)의 지면에 도착했다. 두 시도 모두 X/Z 오차 0m, Console 오류 0건이다. 기존 치트 연결을 사용했다.
- **RB-MAP02 · 사용자 테스트 대기.** Editor에서 이 씬을 Play하고 키보드 `M`으로 전체 지도를 연다. 강과 남쪽 호수 위치를 확인하고, 마커가 없는 땅을 Ctrl+좌클릭한 뒤 지도를 닫는다. 선택 지점에 도착하면 통과다. 실제 입력이나 지도 가독성 문제가 있으면 `RB-MAP02 / 결과 / 클릭한 위치와 증상 / 화면 또는 로그`로 기록한다. 현재 HUD 설정에서 소형 미니맵은 기본 비표시다.

### 4km 수중 색과 수면 경계 조사

- **FEEL-RIVER-BASIN-01 · 수중 연출.** 사용자 제보 “Riber Brains 4Km 맵 수중 쉐이더 왜ㅑ 이렇까?” 첨부 화면에서 가까운 암반은 또렷하고 화면 위쪽은 남색 단색에 가깝다. B1 Play 구간의 기존 [수중 캡처](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Underwater.png)에서도 같은 양상을 확인했다. 제보 화면의 정확한 위치·시야각과 재현 빈도는 모름.
- 열린 Editor의 활성 씬은 `RiverBasin4km`였다. B1의 `UnderwaterRegion`은 [UpperOptics](../../Assets/10.Datas/World/ToonWater/UpperOptics.asset)에 연결돼 있고, 마지막 수중 합성 기록은 수역 1개·체적 셀 11개·거절 0개였다. Play Mode는 미수행.
- [수중 합성](../../Assets/06.Shaders/Water/WaterSceneResolve.shader)은 수면 아래에서 비스듬히 올려다볼 때 전반사를 적용하며, 반사색을 수중 산란색으로 근사한다. [수중 광학](../../Assets/06.Shaders/Water/WaterCommon.hlsl)은 물속 경로가 길수록 같은 산란색에 가까워진다. 파도 프로필도 비어 있어 수면 각도 변화가 적다. 첨부 화면에서 두 경로가 차지하는 픽셀의 구분은 미확인.

### 4km B1 수중 합성 개선

- **구현 반영.** 수면 아래의 Fresnel 반사율을 물과 공기의 굴절률로 계산해 임계각에서 전반사로 연속적으로 이어지게 했다. 반사 방향에 따라 먼 물의 산란색에서 얕은 물의 색으로 이어진다. 이 계산은 관리 수역의 공통 셰이더에 적용된다. B1이 사용하는 [UpperOptics](../../Assets/10.Datas/World/ToonWater/UpperOptics.asset)는 남색 위주의 산란과 흡수를 청록 쪽으로 보정하고 기존 집광 무늬(Caustics) 세기를 0.12에서 0.22로 올렸다. 이 프로필과 메타는 기존 저장소의 월드 데이터 제외 규칙에 속한다.
- **Editor 고정 카메라 확인.** B1 좌표 X 1655 / Z 1961의 수평 잠수, 올려다보기, 수면 위 구도에서 수정 전후 각 3장(640×360, MSAA 1배)을 촬영했다. 최종 3장 모두 카메라당 합성 1회, 수역 1개·셀 11개, 거절·자홍색 픽셀 0이었다. 위를 바라볼 때 반사 영역의 밝기 변화와 하늘 경계의 전이를 확인했다. 카메라는 같으며 시간과 조명 상태는 고정하지 않았다. [수평 전](../../output/RiverBasinWorld/UnderwaterReview/under-level-before.png) · [수평 후](../../output/RiverBasinWorld/UnderwaterReview/under-level-final.png) · [위쪽 전](../../output/RiverBasinWorld/UnderwaterReview/under-up-before.png) · [위쪽 후](../../output/RiverBasinWorld/UnderwaterReview/under-up-final.png) · [수면 위 전](../../output/RiverBasinWorld/UnderwaterReview/above-before.png) · [수면 위 후](../../output/RiverBasinWorld/UnderwaterReview/above-final.png)
- **자동 검사.** 수치 검사에서 반사율의 0~1 범위, 단조성, 임계각 연속성과 수직 입사 약 2.04%를 확인했다. Unity `WaterRenderingContractTests`는 Play Mode 전환 중 실행 요청이 거절되어 미수행이다.
- **사용자 테스트 대기.** `FEEL-RIVER-BASIN-01`의 B1 수영로에서 잠수하고 시선을 수평에서 위로 올린 뒤 재부상한다. 수면 아래 반사의 톤 변화, 바닥과 물빛 구분, 재부상 시 급격한 색 점프 유무를 확인한다. 실제 입력·밤·비·GPU 시간은 미수행. 수정 전후 비교에 쓴 사진 6장은 보존했고 중간본 3장은 정리했다.

### 4km 북부 2단 폭포

2026-11-22 **제작 씬 반영·저장 · 가상 패드 Play 3경로 통과 / 미술·청감 사용자 테스트 대기.** 사용자 요청("RiverBasin4km에 폭포 연결해줘. 게임적으로 예쁘게 어울리게")으로 설계 순서(본선 → B2~B4 → 폭포)보다 앞당겨 제작했다.

- **위치 결정.** 설계의 X 2800 / Z 3300 부근은 폭 150m가 넘는 평탄한 초원이라 낙차를 만들면 보(洑)처럼 읽힌다. 반면 본류 상류 끝(계획 첫 제어점 X 2850 / Z 3360)은 초원 한가운데서 사각 수면으로 뚝 끊겨 있었다([작업 전](../../output/RiverBasinWorld/NorthFalls/before-downstream.png)). 그래서 그 바로 북쪽 산체 계곡 입구(립 X 2844 / Z 3418)에 폭포를 두었다. 이곳은 양옆 사면이 폭포를 감싸고, 폭포가 강이 시작되는 지점이 된다. 하류 방향은 (−0.08, 0, −1)이다.
- **구성.** 바위 틈 샘(수위 156.6) → 상단 폭포 약 9.4m(폭 6m) → 얕은 립 웅덩이(147.2, 수심 0.6m로 걸어서 건넌다) → 본 폭포 11.2m(폭 13~14m, 던짐 4m) → 낙수 웅덩이(136.04, 폭 약 26m, 중심 수심 4.0~4.2m) → 기존 본류 머리(136.04에서 하류로 0.09m/m 하강)로 이어진다. 웅덩이 서쪽 s 12~32는 17° 자갈 출구로 비워 두었고, 하류는 10° 이하로 얕아져 본류로 걸어 나간다.
- **지형.** Terrain_5_6 한 장의 X 2817~2865 / Z 3361~3452(3,249표본, 최대 절토 12.67m·성토 3.51m)만 바꿨다. 네이티브 높이를 고친 뒤 `WorldTerrainAuthoringSession.CaptureSculpt`로 **지형 명령 스택 7번째(AddDelta, 소유자 없음)**로 기록했다. 그래서 WorldBuilder 지형 재생성에도 유지된다. 적용 후 `EnsureReadyForBake`에서 미캡처 변경이 없음을 확인했다. alphamap은 4m 해상도(128²)라 급경사 NorthCliff·Rock, 립 하상 WeatheredRock, 물가 0.9m Shore만 칠했다.
- **수면·낙수.** 수면은 기존 본류와 같은 `Surface.mat`(Lake Water)의 1m 격자 3장(샘·립 웅덩이·낙수 웅덩이)이다. 모두 `FootstepWaterSurface`에 등록해 웅덩이는 수영, 립 웅덩이·샘은 얕은 물 걷기가 된다. 낙수는 호수 폭포 R3의 양면 중력 곡선 방식을 따른다. 재질은 `Lake Wide Fall` 복제본에 강 합성 값을 `Surface.mat`에서 복사했고, 몸통 색을 강의 탁한 초록으로 맞췄다(첫 청록은 렌더 비교에서 어긋났다). 상·하 폭포에는 `WaterfallLink`를 연결했다. 본류 머리 `Water_5_6`은 원본 WaterJoined 자산을 두고, 세 꼭짓점이 모두 웅덩이 안인 삼각형 32개만 뺀 파생 메시로 교체했다. 웅덩이 수면은 본류가 아직 136.04로 평평한 s 48까지 덮는다. 하류 하상은 본류의 실제 수면(s 46부터 하강)에서 깊이를 빼서 만들었고, 서쪽 굴착 폭은 본류 서안(q≈+3)에 맞췄다. 첫 시도에서는 평평한 웅덩이 수위 기준으로 대칭 굴착해, 수면 없는 갈색 오목지와 수심 0의 투명 띠가 생겼다([진단: 웅덩이만 끔](../../output/RiverBasinWorld/NorthFalls/diag-junction-poolOff.png)).
- **P05 대신 수작업 수면을 쓴 이유.** (1) 잇는 본류가 P05가 아닌 기존 Lake Water 수면이라, 폭포의 립 합성이 같은 셰이더 계열이어야 경계가 남지 않는다. (2) P05 수계를 재생성하면 B1 계층과 수영 어댑터를 지우고 다시 만들어 B1 재검증이 필요하다. (3) P05 폭포 curtain은 립→착지를 잇는 평평한 직선 띠다. 지형만 WorldBuilder 명령 스택에 기록했다.
- **암반·식생·연출.** 월드가 쓰는 Polyart EastLands 바위·절벽 17개는 낙구 양끝의 사각 윤곽과 지형 모서리를 덮는 기둥 바위, 웅덩이를 감싸는 절벽, 샘 바위, 물가 바위, 웅덩이 출구 여울의 반쯤 잠긴 강돌 3개다(총 20개). 목서·관목·갈대 12개도 함께 배치했다. 물보라·미스트 14개는 기존 RiverBasinWorld 입자를 복제했다. 소리는 B1 강바닥 루프와 구분되는 저역 굉음 12초 루프를 결정적 합성해 본 폭포(10~110m)와 상단 폭포(5~45m)에 붙였다.
- **Play 검증(가상 패드, 1920×1080).** [웅덩이 횡단 왕복](../../Tools/RiverBasinWorld/Rebuild/NorthFalls_PoolCross_PlayMode.txt): 동쪽 둑 → 수영 → 서쪽 자갈 출구 → 역방향, 수영 진입·이탈 각 2회, 막힘 0. [하류 출구](../../Tools/RiverBasinWorld/Rebuild/NorthFalls_Downstream_PlayMode.txt): 웅덩이 수영 → 본류 쪽 걸음 전환, 막힘 0. [립 낙하](../../Tools/RiverBasinWorld/Rebuild/NorthFalls_LipJump_PlayMode.txt): 동쪽 사면 → 립 웅덩이 걷기 → 낙구 낙하 약 14m → 수영 → 서쪽 상륙, 막힘 0. 이 프로젝트에는 낙하 피해가 없어 립 낙하는 안전한 다이빙 동선이다. Play 중 Console 오류는 0이다. 실제 입력·카메라 체감은 대신하지 않는다.
- **화면.** [하류 TPS](../../output/RiverBasinWorld/NorthFalls/play-tps-downstream-final.png) · [서쪽 출구 TPS](../../output/RiverBasinWorld/NorthFalls/play-tps-west-beach.png) · [정면](../../output/RiverBasinWorld/NorthFalls/v2-front.png) · [측면](../../output/RiverBasinWorld/NorthFalls/v2-side.png) · [조망](../../output/RiverBasinWorld/NorthFalls/v2-vista.png) · [수중](../../output/RiverBasinWorld/NorthFalls/v2-underwater.png) · [하류 접합부 TPS 구도](../../output/RiverBasinWorld/NorthFalls/junction-final-tps.png) · [하류 접합부 서쪽](../../output/RiverBasinWorld/NorthFalls/junction-final-west.png)
- **남은 확인·주의.** (1) 하류에서는 본류 수면이 출구 여울(s≈49)에서 웅덩이 쪽으로 완만하게 오르는 경사 꺾임이 보인다. 여울 강돌로 자연 지형처럼 보이게 했으나, 미술 인수는 사용자 확인이 필요하다. (2) 기존 본류 `Water_5_6`에는 수면 등록이 없어 웅덩이를 벗어나면 수영이 아니라 걷기다(작업 전부터 같은 상태). (3) 물소리는 합성 임시 음원이며 청감 인수 대기다. (4) 성능·밤·비 미계측. (5) 작업 후 검사에서 B1 P05 수계 계층 지문이 P05 수면 재질 값 차이로 불일치했다. 작업 전 상태는 확인하지 않았다. 이번 작업에서 해당 재질을 바꾸지 않았고 git 기준 변경도 없다. P05 재생성 전에 확인해야 한다.
- **재생성 시 유지 범위.** 지형은 명령 스택 7번째라 WorldBuilder 지형 재생성에도 남는다. P05 수계 재생성은 이 폭포를 건드리지 않는다. 다음 경우에는 표시된 단계를 다시 실행해야 한다. 본류 수면(WaterJoined)을 다시 생성하면 잘라낸 `Water_5_6`이 원본으로 돌아가 웅덩이 위에 사각 머리가 다시 겹치므로 `north-falls-trim`을 다시 실행한다. P06 생태 등으로 alphamap을 다시 쓰면 `north-falls-paint`를 다시 실행한다(paint는 작업 전 백업 기준이라 그 백업도 갱신해야 한다). 수작업 메시·바위·입자는 지형 높이가 바뀌면 water 이후 단계를 다시 실행한다.
- **저작 기록·되돌리기.** [Tools/RiverBasinWorld/Rebuild/north-falls-*.cs.txt](../../Tools/RiverBasinWorld/Rebuild/north-falls-run.cs.txt)를 `north-falls-run.cs.txt`가 공용 형상 정의와 함께 terrain → paint → water → trim → dress → fx → capture 순으로 실행한다. `north-falls-reset`은 이번 Sculpt 명령만 지형 스택에서 빼고 「북부 폭포」 계층·생성 메시·재질·본류 메시·alphamap을 작업 전으로 되돌린다. 작업 전 사본은 `output/RiverBasinWorld/NorthFalls/backup/`(Terrain_5_6·씬·월드 정의·alphamap)에 있다. 제작 반복 중 생긴 미참조 Sculpt 패치·세션 4벌은 지웠다(그 반복들에 대한 Editor Undo는 쓸 수 없다). 최종 패치 `86ff23fc…`와 세션 스냅샷 `c1920703…`(84MB, 미추적)만 남겼다. 기존 작업들도 세션 스냅샷을 보존해 왔다. 다만 100MB 미만이라 커밋할 때 함께 포함될 수 있다.

사용자 테스트: RiverBasin4km을 단독 Play하고 Hierarchy의 `북부 폭포`로 이동하거나 하류 본류(X 2836 / Z 3362)에서 북쪽을 본다. 결과는 `NF-조망/접근/수영/상륙/립낙하/소리 | 통과·실패·미수행 | 증상·위치`로 남긴다.

### 4km 거리 안개 조사

2026-11-21 열린 Unity Editor의 활성 [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)에서 `RenderSettings.fog` 활성, `ExponentialSquared`, 밀도 `0.00062`를 읽고 저장값과 대조했다. `PC_RPAsset`의 기본 Renderer에 [수중 렌더러 기능](../../Assets/02.Scripts/Gameplay/World/UnderwaterRendererFeature.cs)이 활성화되어 있다. 이 기능이 수역을 합성할 때 [WaterCommon](../../Assets/06.Shaders/Water/WaterCommon.hlsl)에서 공기 구간의 거리 안개를 계산한다. Play Mode 화면과 Player 확인은 미수행이다. [월드 위키](../wiki/world-art.md)

### 4km 서부 가을 숲 전역 식생

2026-11-21 **제작 씬 반영 · 자동 검사와 Editor Play 왕복 통과 / 성능 목표 미달 / 미술 인수 사용자 테스트 대기.** 설계의 [서부 가을 숲](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#서부-가을-숲과-수변-식생) 범위 X 440~1450 / Z 1600~2900과 사당 접근 능선에 126m 파일럿의 수종·풀 규칙을 넓혔다. 설계 순서상 본선(호수 마을 → B3 → B1 → 성채)이 먼저지만 사용자 요청에 따라 숲을 앞당겼다. 본선 도로가 확정되면 숲 가장자리를 아래 스크립트로 재평가한다.

- **방식.** 이 월드에는 P06 `WorldEcology`가 없고 P06 적용은 alphamap을 다시 쓰므로 쓰지 않았다. 전역 좌표 기반 결정적 스크립트 [forest-west-populate.cs.txt](../../Tools/RiverBasinWorld/Rebuild/forest-west-populate.cs.txt)(인자 [forest-west-run.txt](../../Tools/RiverBasinWorld/Rebuild/forest-west-run.txt))로 TerrainTree·Terrain Detail·표시 거리만 썼다. 높이·지표 재질은 그대로다. 파일럿의 나무 219그루(식재 범위 X 845~1023 / Z 2174~2384), 풀(상자 X 826~1024 / Z 2172~2412), 관목 오브젝트 26개는 유지했다. 상자 안에서 파일럿 나무가 없던 서쪽·북쪽 띠는 새 수목으로 채웠다. 저장본은 기록 파일 내용을 그대로 붙여 실행한 결과이며, 같은 인자로 dry 실행해 생성 수와 피복률이 같게 나오는 것을 확인했다. 인자 파일은 `mode=dry`로 두었다.
- **배치.** `Terrain_0~2_3~6` 12장에 파일럿과 같은 순서의 수목 프로토타입 12종 + 목서 관목 3종, 풀 3종(0.5m 셀·InstanceCountMode)을 맞췄다. 새 인스턴스는 18,447개로 대형·어린 활엽수 10,780, 묘목 5,432, 관목 2,235다. 구역별 수종은 연못·개울 둑 자작나무 위주, 숲 안 목서·너도밤나무, Z 2720 이북과 사당 능선길 구리·황금 목서, 길·가장자리 어린 나무·어린 활엽수다. 낮은 주파수 잡음으로 수관이 비는 공터를 만들고 풀은 수관 아래에서 성기게, 공터·길가에서 촘촘하게 했다.
- **수치(정적).** 핵심 숲 수관 피복 55.4%(숲 안 59.6, 능선 55.6, 물가 28.9, 가장자리 29.2). 대형 수목 최근접 간격 p10/p50/p90 7.2/8.1/10.8m. 파일럿 포함 전 수목 18,666그루 중 순환로·사당길 중심 4m 안 0, 길 중심에서 수관 반경+2m 안의 어린 활엽수 0, 실제 수면 메시 아래 0, 강가 마을·사당 부지 0, 지면 이탈 0. 첫 감사에서 길 4m 안 묘목 11그루, 길로 가지가 넘어오는 어린 활엽수 8그루를 찾아 스크립트 최종 필터로 막았다. 숲 타일 10장의 표시 설정은 풀 90m·나무 340m·빌보드 95m로 같다.
- **파일럿 경계.** 바깥 대형 수목 밀도가 파일럿의 약 두 배라 상자 바깥 50m에 걸쳐 대형 수목(0.5배→1배)과 풀(0.75배→1배)을 서서히 늘렸다. 대형 수목 밀도는 상자 안 47그루/ha, 바깥 60m 띠 72그루/ha다. [북쪽 경계](../../Tools/RiverBasinWorld/Rebuild/ForestWest_PilotSeam_North.png) · [서쪽 연못 둑](../../Tools/RiverBasinWorld/Rebuild/ForestWest_PilotSeam_West.png). 서쪽 연못 둑 공터에 같은 크기의 자작나무 묘목이 고르게 흩어져 있어 군락처럼 보이지 않는 점은 미술 확인 항목이다.
- **나무 충돌.** 숲 타일 10장 모두 TerrainCollider 나무 충돌이 켜져 있다. Play에서 대형 수종 9종(목서 6, 자작나무, 너도밤나무 2)은 네 방향 수평 레이가 모두 줄기에 맞았다. 어린 활엽수·너도밤 묘목·관목은 충돌이 없다(아래 결함).
- **화면.** [내부 수관과 하층 풀](../../Tools/RiverBasinWorld/Rebuild/ForestWest_S1_Interior.png) · [순환로에서 강가 마을 방향](../../Tools/RiverBasinWorld/Rebuild/ForestWest_LoopToVillage.png) · [사당 능선길](../../Tools/RiverBasinWorld/Rebuild/ForestWest_ShrineRidge.png) · [TPS 순환로](../../Tools/RiverBasinWorld/Rebuild/ForestWest_LoopEdge_TPS.png) · [TPS 사당 능선](../../Tools/RiverBasinWorld/Rebuild/ForestWest_ShrineRidge_TPS.png). 이 다섯 장은 파일럿 경계 보정으로 재생성하기 전 배치다. 수종·규칙은 같지만 개별 나무 위치는 다르다. 상공 캡처는 나무 표시 거리 밖이 섞여 밀도 판단에 쓰지 않는다([참고](../../Tools/RiverBasinWorld/Rebuild/ForestWest_S1_Aerial.png)).
- **Editor Play 왕복(1920×1080, 가상 패드, [프로브](../../Tools/RiverBasinWorld/Rebuild/forest-west-probe.cs.txt)).** 순환로 가장자리(Z 2560 경계), 숲 안 자유 보행(X 1024 경계), 사당 능선길, 개울 동안(Z 2048 경계) 최종 배치로 다시 왕복한 네 경로 모두 막힘 0, 안정 지지 100%(5,557 / 4,524 / 5,355 / 3,149 표본)다. 이전 배치 첫 실행의 막힘 1건은 3m 안 장애물이 없는 지점의 2.65초 프레임 정지에 따른 오판이었다. 그래서 판정에 프레임 수 기준을 함께 쓰도록 고쳤다. Console 오류는 기존 Localization 1건뿐이다. [순환로](../../Tools/RiverBasinWorld/Rebuild/ForestWest_LoopEdge_PlayMode.txt) · [숲 안](../../Tools/RiverBasinWorld/Rebuild/ForestWest_Interior_PlayMode.txt) · [사당 능선](../../Tools/RiverBasinWorld/Rebuild/ForestWest_ShrineRidge_PlayMode.txt) · [개울](../../Tools/RiverBasinWorld/Rebuild/ForestWest_CreekBank_PlayMode.txt)

**성능 목표 미달.** 최종 배치 이동 CPU p95는 23~42ms, GPU p95는 15~33ms로 16.7ms를 넘는다(사당 능선 CPU 23~25 / GPU 15~17ms, 순환로 33~40 / 25~28ms, 숲 안 30~32 / 25ms, 개울 36~42 / 31~33ms). 파일럿 기준은 CPU 39~42 / GPU 20~21ms였다. 이동 중 1.8~8.9초의 프레임 정지가 경로마다 0~1회 나타났다(개울 복귀 8.9초, 사당 복귀 1.8초, 이전 배치 순환로·숲 안 2.6~2.8초). 같은 지점에 서서 다시 계측하면 재현되지 않아 원인은 미분류다. 플레이 체감에 직접 닿는 문제라 Profiler로 먼저 분류해야 한다.

| 같은 지점 정지 계측 (X 1000 / Z 2450, 서향) | CPU p50 1회 / 2회 | GPU p50 1회 / 2회 |
|---|---:|---:|
| 전체 | 24.3 / 25.4ms | 19.4 / 21.5ms |
| 풀 끔 | 21.8 / 35.7ms | 19.4 / 16.5ms |
| 나무 끔 | 17.6 / 20.7ms | 17.1 / 14.9ms |
| 식생 모두 끔 | 16.9 / 18.3ms | 15.1 / 11.0ms |

정지 계측은 재생성 전 배치에서 했다. 나무가 가장 큰 항목이고(표시 340m 안 4,431그루), 식생을 모두 꺼도 CPU가 16.7ms 근처다. 나무 표시 340/260/200/140m 비교는 같은 340m 설정이 30.9 → 27.4ms로 흔들려 Editor에서 효과를 판정하지 못했다. 저장값 340m를 유지한다. [식생 항목 비교](../../Tools/RiverBasinWorld/Rebuild/ForestWest_Bench_PlayMode.txt)(2회차 기록) · [나무 거리 비교](../../Tools/RiverBasinWorld/Rebuild/ForestWest_BenchTreeDist_PlayMode.txt). 목표 판정은 Profiler를 붙인 Player 계측이 필요하며 별도 지시 전에는 실행하지 않는다.

**남은 항목.**
- 사용자 테스트 대기: 실제 입력으로 숲길·공터·연못 조망 확인, 카메라 회전 가림, 수종·색 배합과 길 양옆이 가로수처럼 곧게 늘어서 보이는지에 대한 미술 판단, 밤 장면.
- 알려진 결함(기존): 파일럿부터 쓰는 `PF_Farm_GenTree_B_Young_01`(새로 445그루)과 `TGV_Beech_Sapling_01A`는 MeshCollider라 TerrainTree 충돌이 생기지 않는다(`TerrainCollider: MeshCollider is not supported` 경고). 어린 활엽수 줄기를 통과할 수 있다. 외부 에셋 원본 대신 캡슐 콜라이더를 둔 프로젝트 복제 프리팹으로 바꾸는 것을 권한다.
- 미수행: 청크 로드·재진입, Player 계측, 숲 바닥 지표 재질 보정(현재 Meadow·Forest 혼합 유지), 갈대·수생 식물 호안 띠.

#### 지면 식생 자연화 (사용자 제보 · 2026-11-21)

**제작 씬 반영 · 같은 구도 비교 캡처 완료 / Play 계측·미술 인수 사용자 테스트 대기.** 사용자 제보는 "잔디 배치가 인위적이고 어디나 같은 풀"이다. 원인은 이전 스크립트 세 곳이었다. 수관 원 경계에서 풀 밀도가 0/1로 끊겼고, 길 가장자리 감쇠 폭이 모든 길에서 같았다. 또 모든 칸이 같은 3종을 70/20/1.5% 비율로 뽑아 균일한 점무늬가 됐다.

- **방식.** 풀은 나무 배치와 분리한 결정적 스크립트 [forest-west-grass.cs.txt](../../Tools/RiverBasinWorld/Rebuild/forest-west-grass.cs.txt)(인자 [forest-west-grass-run.txt](../../Tools/RiverBasinWorld/Rebuild/forest-west-grass-run.txt), `scale=1.4`)가 소유한다. 그늘은 실제 배치된 TreeInstance(17,691그루)와 이후의 반응형 수목 GameObject에서 계산한다. 그래서 나무를 다시 배치하면 이 스크립트를 이어서 실행한다. [forest-west-populate.cs.txt](../../Tools/RiverBasinWorld/Rebuild/forest-west-populate.cs.txt)는 이제 나무만 쓴다. 파일럿 상자의 풀도 같은 규칙으로 다시 만들었다. 옛 균일 풀과 새 풀 사이에 이음새가 생기기 때문이다. 본문은 [run-script.cs.txt](../../Tools/RiverBasinWorld/Rebuild/run-script.cs.txt)로 파일을 그대로 컴파일해 실행한다.
- **규칙.** 수관 그늘은 연속값으로 바꾸고 4m 블러와 잡음을 넣었다. 길 거리와 감쇠 폭(1.2~4.7m)은 잡음으로 흔들었다. 도메인 왜곡한 저주파 잡음으로 군락 유형을 고른다. 유형은 물가(짙은 녹색), 숲 바닥(포기로 뭉치고 맨땅·낙엽 더미 얼룩), 무성한 풀밭(키 큰 황갈색 풀 포함, 칸당 최대 2), 건조 능선·양지 사면(마른 황토색), 보통 틈이다. 약 4m 주기 잡음으로 포기를 만들고, 그늘이 옅은 곳에는 억새·들꽃 군락을 둔다.
- **에셋.** [환경 에셋 카탈로그](../asset-catalog/plant.html)에서 Farmlands 풀 3종 메시, Farmlands 키 큰 풀, EastLands 억새, Toon Golden Valley 들꽃 군락 2종과 낙엽 더미를 골랐다. 구매 원본 대신 프로젝트 소유 재질 복제본(`Assets/10.Datas/World/RiverBasinWorld/ForestWest/` 8개, 인스턴싱 켬)과 Detail 프리팹(`Assets/03.Prefabs/Environment/RiverBasinWorld/ForestWest/` 11개)을 만들었다. 가을 풀은 B1 재질보다 밑동을 밝게 해 지면과 이었다. 어두운 포기가 밝은 지면에서 구멍처럼 보였기 때문이다. B1 재질은 그대로다. Idyllic FlowerMeadow는 위에서 삼각형 판으로 보여 폐기했다.
- **수량.** 숲 경계 안 풀 인스턴스 3,240,697 → 2,771,383(85%). 프로토타입은 3종에서 11종으로 늘었다. 인스턴스 총량은 줄었지만 패치·프로토타입별 그리기 호출은 늘 수 있다. 성능 판정은 Play 계측이 필요하다.
- **비교.** 같은 위치에서 적용 전후를 찍었다. [숲 안 전](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/Before_Interior.png) · [숲 안 후](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/After_Interior_1.png) · [능선 전](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/Before_Ridge.png) · [능선 후](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/After_Ridge2.png) · [공터 들꽃 군락](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/After_Overview2.png). Editor Console의 풀 관련 오류는 0건이다.
- **사용자 테스트 대기.** 실제 이동 중 풀 밀도의 변화와 색 배합, 길 가장자리, 들꽃·억새 군락의 빈도, 숲 바닥 낙엽 더미를 확인한다. 이동 계측(CPU·GPU p95)은 미수행이다. 지표 재질이 밝은 단색 잔디라 풀 사이 바닥이 인공적으로 보이는 문제는 남아 있다. 숲 바닥 지표 재질 보정은 별도 작업이다.
- **저장 에셋 재확인.** Unity Editor에서 지형 12장을 읽어 숲 경계 안 인스턴스 2,771,383개, 각 타일의 프로토타입 11종, 누락된 풀 프리팹 참조 0건을 확인했다. 연결 재질은 9종이며 새로 생성된 재질 수는 확인하지 않았다. 풀 프리팹과 재질 경로는 현재 Git 제외 대상이다. 나무 배치 스크립트는 현재 풀 쓰기를 건너뛰며, 건너뛴 지점 아래에 옛 풀 배치 코드가 남아 있다.

#### 서부 숲 지면 식생 레퍼런스 비교

2026-11-23 **Phase 0 지표 요인 비교와 Phase 1 색 보정 후보 구현.** 제작 씬을 미리보기로 열어 지형 데이터·레이어·풀 재질을 메모리에서 복제했다. 같은 네 구도를 1280×720, FOV 54로 렌더했다. 재현 절차는 제작 씬을 편집 모드로 연 뒤 [측정 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/measure-ground-vegetation.cs.txt) 본문을 Unity MCP에서 실행하는 것이다. [설계](../../Assets/docs/design/world-art/environment/RIVER_BASIN_GROUND_VEGETATION_REFERENCE_DESIGN.md)

- **지표.** X 440–1450 / Z 1600–2900과 교차한 타일 9장 모두 레이어 8개를 사용한다. 7장은 URP Terrain/Lit, `Terrain_0_5`와 `Terrain_1_5`는 Sand Lit이다. Meadow의 원본 알베도는 평균 선형 휘도 약 0.353, Forest는 0.203이다. 각 구도 전방 12m 표본에서 Meadow 비중은 숲 안 48.6%, 능선 76.9%, 공터 71.0%, 낮은 공터 72.2%였다. 가까운 타일의 풀과 나무를 숨긴 비교에서는 알베도 감쇠와 Meadow를 Forest로 치환하는 조작 모두 노출된 지표를 어둡게 했다. Meadow 전량 치환은 흙색 얼룩이 강하게 드러나는 진단용 조작이다. [지표 비교와 보정 후보](river-basin-grass-reference-evidence/ground-comparison.jpg) · [타일·재질 원시 측정](river-basin-grass-reference-evidence/measurements.txt)
- **풀과 성능.** 같은 사각형 안에 저장된 풀 수는 2,304,680개다. 앞서 보고한 2,771,383개는 사당 능선까지 포함한 배치 범위다. InstanceCountMode의 API 상한은 프로토타입별 셀당 16, 관측 최대는 2였다. 제작 타일은 90m, 검증 씬의 직렬화 값은 80m 8장과 90m 1장이다. 풀 11종 중 7종이 `UPG_Grass` 원본을 쓴다. 80m·디더링·노이즈 배수는 정지 이미지로 기여도를 확정하기 어렵다. 같은 기준값으로 연달아 렌더해도 Editor 바람 시간에 따라 픽셀이 움직였다. 후처리를 끄면 이 구도에서는 화면 평균이 소폭 밝아졌다. [요인 비교](river-basin-grass-reference-evidence/factors.jpg) · [이미지 변화량](river-basin-grass-reference-evidence/image-deltas.txt)
- **색 보정 후보.** 기존 스플랫·풀 수·풀 종·거리·셰이더를 유지한 복제본에서 Forest와 Meadow 알베도 remap을 완만하게 낮추고, `UPG_Grass` 재질 6개의 밑동 녹색 성분을 함께 낮췄다. 숲 안 지표와 풀 밑동의 색 차이가 줄었고 공터와 능선의 변화는 작았다. [비교 이미지의 마지막 두 열](river-basin-grass-reference-evidence/ground-comparison.jpg)
- **검증.** 편집 모드에서 게임 카메라 렌더 52장과 캡처 중 오류·경고 0건을 기록했다. MCP 응답은 전송 중 끊겼으나 디스크의 52장과 마지막 로그까지 확인했다. 실제 Game 뷰의 날씨·움직임, draw call, CPU/GPU, 플레이 품질은 미수행이다. 과거 캡처의 뷰 종류는 확인할 수 없었다. 사용자 테스트는 같은 네 구도에서 그림자 속 밑동이 구멍처럼 보이는지, 걸을 때 풀밭 밀도와 길 가장자리가 읽히는지 확인하고 구도명·통과/실패·화면으로 회신한다. Player 성능 계측은 사용자 지시 대기다. Phase 2 바람과 Phase 3 열린 풀밭은 후속 작업이다.
- **근거 보관.** 비교판 2장과 측정값(382,102바이트)은 이 문서 옆에, 최종 전체 해상도 PNG 52장(97,159,557바이트)은 `output/RiverBasinWorld/GrassReferencePhase0/`에 보관했다. 초기 캡처와 연결 확인 이미지(64,522,409바이트)를 정리했고, 임시 미리보기 씬과 복제본은 Unity에서 닫고 해제했다.

##### Phase 1 제작 반영

- **지표 색 저장 완료.** 타일 9장의 미사용 Sandstone·WeatheredRock 슬롯에 보정 Forest·Meadow 레이어를 넣었다. 숲 사각형 X 440–1450 / Z 1600–2900 안에서만 해당 가중치를 옮기고, 안쪽 40m에 걸쳐 전환했다. 복제본과 실제 저장 작업에서 모두 8개 슬롯과 알파맵 전체 채널의 재읽기 검사를 통과했다. 제작 씬 파일, 공유 원본 레이어, 풀·나무 배치, 지형 높이는 같은 데이터를 사용한다. [적용 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/apply-ground-tone.cs.txt) · [보정 레이어와 meta](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/ForestWestGround/) · [적용 검사](river-basin-grass-reference-evidence/production-apply.txt)
- **풀 밑동 색 저장 완료.** 초기 자동 승인 검토 이후 사용자가 `Terrain_0_6`·`Terrain_1_6`·`Terrain_2_6`을 포함한 재질 적용 범위를 승인했다. 제작 타일 12장의 Detail 참조를 확인하고 `FW_MI_Grass` 재질 6개의 `_ColorBottom`에 후보와 같은 RGB 배율 0.94/0.89/0.95를 적용했다. 백업과 비교해 재질 6개에서 해당 속성만 변경됐고 meta의 SHA-256은 같았다. 이 재질은 기존 Git 제외 경로에 있다. [적용 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/apply-grass-bottom-tone.cs.txt) · [재질 검사](river-basin-grass-reference-evidence/grass-bottom-apply.txt)
- **저장본 확인.** 보정 가중치가 생긴 알파맵 표본 80,519개, 범위 밖 표본 0개, 누락된 풀 프리팹·재질 참조 0개를 확인했다. 타일 9장의 meta는 백업과 SHA-256이 같았다. 경계 안쪽·타일 경계·범위 밖을 포함한 6구도 전후 12장을 렌더했고 캡처 중 오류는 0건이었다. 숲 안 지표에서 색 변화가 가장 뚜렷했고, 타일 경계 비교에서 새 색 경계가 눈에 띄지 않았다. 실제 Game 뷰에서 이동 중 거리별 경계·디더링과 드로우 콜/CPU/GPU는 사용자 테스트 대기다. 같은 여섯 구도와 경계에서 지면 색 띠 또는 밝기 튐이 보이면 위치와 화면을 남긴다. Phase 2 바람과 Phase 3 열린 풀밭은 후속이다. [제작 전후 비교](river-basin-grass-reference-evidence/production-comparison.jpg) · [저장본 촬영 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/capture-production-ground.cs.txt)
- **복구 근거.** 작업 직전 타일 9장과 meta(43,868,785바이트)는 `output/RiverBasinWorld/GrassReferencePhase1/backup-20261123-215356-731/`에 있다. 전후 원본 PNG 12장(23,774,897바이트)은 같은 Phase 1 폴더에 보관했다. 임시 렌더 리소스는 해제했다.
- **풀 밑동 저장본 확인과 복구.** 사당 능선·서북 사면을 포함한 8구도의 전후 16장을 렌더했고 캡처 오류는 0건이었다. 서부 숲 안쪽 풀 밑동의 색 변화는 작았다. 실제 Game 뷰에서 눌러붙는 어두운 포기나 이동 중 색 튐이 생기는지 사용자 확인이 필요하다. `output/RiverBasinWorld/GrassReferencePhase1/grass-bottom/`에 PNG 16장(30,310,761바이트)과 `backup-20261123-215750-096/`의 재질·meta 12개(34,069바이트)를 보관했다. [풀 밑동 전후 비교](river-basin-grass-reference-evidence/grass-bottom-comparison.jpg)
- **피드백 후 지표 톤 보강.** 사용자 비교에서 차이가 작아 노출 지표가 많은 숲 안과 타일 경계를 중심으로 기존값·중간값·강한 값을 복제본에서 비교했다. 숲 안에서 풀 사이가 갈색 흙으로 읽힌 강한 값을 채택했다. 전용 레이어 2개의 `DiffuseRemapMax`는 Forest (0.231, 0.30, 0.52, 1), Meadow (0.260, 0.286, 0.64, 1)이며 각각 해당 속성 한 줄만 바뀌었다. 제작 저장본 8구도에서 오류 0건이다. 숲 안 확대 영역의 평균 sRGB G−R은 최초 10.44 → 기존 보정 8.27 → 최종 0.08이었다. 풀의 화면 점유율이 높은 능선과 공터의 변화는 작았고, 실제 이동과 성능은 검증 대기다. 비교 원본 PNG 26장(50,735,254바이트)과 이전 레이어·meta(2,079바이트)는 `output/RiverBasinWorld/GrassReferencePhase1/tone-tuning/`에 보관했고 임시 미리보기 씬은 해제했다. [최초·이전·최종 확대 비교](river-basin-grass-reference-evidence/ground-tone-final.jpg)

##### Phase 2 풀 형태와 방향성 바람

- **제작 반영.** 타일 9장에서 키 큰 가을 풀의 메시 높이 환산 범위를 0.50–0.90m, 억새를 0.56–0.90m로 조정하고 폭에 편차를 줬다. 경계 타일 8장은 두 프로토타입의 숲 안·밖 밀도 배열을 나눠 숲 밖 63,178포기의 기존 배율을 보존했다. 서부 숲 인스턴스 2,304,680개와 기존 셀별 총량을 유지했다. 나무 배열과 프리팹 순서는 적용 전후 일치했다. [복제 비교](river-basin-grass-reference-evidence/phase2-shape-comparison.jpg) · [경계와 범위 밖](river-basin-grass-reference-evidence/phase2-boundary-comparison.jpg) · [셀 검사](river-basin-grass-reference-evidence/phase2-preview-checks.txt)
- **셰이더.** `UPG_Grass`의 본체·그림자·깊이·노멀·모션 벡터에 동일한 변위 함수를 연결했다. 모션 벡터에는 이전 풍향과 이전 프레임 시간도 입력한다. 밟기 입력에 프레임 이력이 없어 벡터의 전후 위치에는 같은 접촉 오프셋을 적용한다. 반응형 파생은 `VegetationReactionShaderAuthoring.Generate`로 재생성했다. 원본·파생은 각 10패스, 가져오기 오류 0건이다. 기존 URP GBuffer 폐기 예정 경고 1건씩 남아 있다.
- **사용처.** 공용 셰이더의 새 바람 배율 기본값은 0이다. 제작 씬에서 원본 셰이더를 직접 참조한 7종의 전용 재질 6개에만 0.23m 또는 0.32m를 저장했다. 이 재질은 서부 숲과 사당 능선 타일 12장에서 사용한다. 별도 B1 가을 풀과 `TD_Grass_Fresh`·`TD_Grass_Lime`는 배율 기본값을 사용한다. 억새는 기존 PlantGraph로 바람을 처리한다. 제작 타일에는 `TerrainDetailReaction` 컴포넌트가 없음을 확인했다.
- **Editor Play.** 2560×1440, 품질 0, VSync 1, 같은 TPS 좌표·FOV·날씨에서 정지 계측 8초. CPU p50/p95 25.76/40.00 → 23.66/32.02ms, GPU 22.16/28.98 → 21.81/27.29ms, 드로우 콜 p50/p95 10,183/10,185 → 10,185/10,188. 측정 구간 뒤 패드 입력에서 플레이어 밟기 입력 최고값은 0.833 → 0.850, 수집 구간 Console 오류는 0건이다. 단일 Editor 구도 참고값이다. 영상은 실제 Game 뷰의 약 3fps 표본을 재생한다. [전 로그](river-basin-grass-reference-evidence/phase2-before-play.txt) · [후 로그](river-basin-grass-reference-evidence/phase2-after-play.txt) · [전 영상](river-basin-grass-reference-evidence/phase2-game-before.mp4) · [후 영상](river-basin-grass-reference-evidence/phase2-game-after.mp4)
- **회귀 검사.** 환경 반응·환경 바람 상태·식생 저작·반응 통합 EditMode 4개 그룹에서 83/83 실행을 마쳤고 작업은 성공 상태다. 방향성 바람을 켠 같은 파생 풀을 GPU로 렌더해 젖음 7,844픽셀의 명도 감소, 연소 5,572픽셀의 적색 발광 증가, 완전 소진 뒤 남은 픽셀 0을 확인했다. 모션 벡터 패스가 이전 풍향을 읽고 파생 셰이더의 연료 소거가 해당 패스에도 적용되는지 소스를 대조했다. [젖음·연소·소진 검사](river-basin-grass-reference-evidence/phase2-reaction-render.txt)
- **재현과 복구.** [복제 촬영 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/capture-vegetation-shape.cs.txt) · [제작 반영 스크립트](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/apply-vegetation-shape.cs.txt) · [Play 계측·촬영](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/probe-vegetation-play.cs.txt) · [제작 적용 기록](river-basin-grass-reference-evidence/phase2-production-checks.txt). 작업 직전 타일·전용 재질·meta와 셰이더는 `output/RiverBasinWorld/GrassReferencePhase2/`에 백업했다. 타일·전용 재질 meta 15개는 백업과 SHA-256이 일치했다.

##### Phase 3 서부 숲의 열린 풀밭

- **제작 반영.** 기존 수관의 중심과 LOD0 메시 반경을 표본화해 피복 추정치 24.1%인 X 1170 / Z 2270을 골랐다. 볕이 드는 X 반경 약 18m, Z 반경 약 24m의 기존 식생 셀에 키 큰 가을 풀 1,311포기를 보탰다. 가장자리는 수관 그늘과 군락에 따라 성기게 했고, 인접 띠에서 같은 풀 1,311포기를 회수했다. `Terrain_2_4`의 해당 레이어 패치 총량은 6,163개, 서부 숲 전체는 2,304,680개다. [낮은 두 시점과 가장자리](river-basin-grass-reference-evidence/phase3-meadow-comparison.jpg)
- **복제 후 저장.** 복제 씬의 세 구도 전후 6장과 풀 셀 재읽기를 확인했다. 제작에 적용한 패치의 SHA-256이 복제 검증본과 일치한다. 추가 셀과 기존 길 중심 사이 최소 146.17m, 수면·보호 부지 침범 0이다. 나무를 고르거나 이동시키지 않고 기존 `TreeInstance` 배열의 모든 값과 프로토타입 순서가 일치하는지 확인했다. 같은 나무 인덱스와 벌목·반응 배치표를 사용한다. [복제 지형 검사](river-basin-grass-reference-evidence/phase3-preview-checks.txt) · [제작 적용 기록](river-basin-grass-reference-evidence/phase3-production-checks.txt)
- **Game 뷰.** 남쪽 X 1170 / Z 2250에서 Phase 2와 같은 해상도·품질·풍향으로 서서 8초씩 계측한 뒤 가상 패드로 풀밭 쪽을 걸었다. 최초 전후 계측 카메라 위치는 (1170.00, 66.98, 2245.15), 피치 14°, FOV 55°다. 수집 구간의 Unity 오류는 전후 0, 최대 플레이어 밟기 입력은 0.841 → 0.834였다. 영상은 실제 Game 뷰의 약 3fps 표본을 재생한다. [전 영상](river-basin-grass-reference-evidence/phase3-game-before.mp4) · [후 영상](river-basin-grass-reference-evidence/phase3-game-after.mp4)
- **재현.** [수관 표본화](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/find-meadow.cs.txt)로 후보를 찾고 [풀밭 패치](../../Tools/RiverBasinWorld/Rebuild/GrassCompare/shape-open-meadow.cs.txt)의 `preview`에서 촬영했다. `apply`는 같은 패치 해시를 확인하고 한 TerrainData의 풀 레이어만 저장한다. 적용 직전 TerrainData와 meta는 `output/RiverBasinWorld/GrassReferencePhase3/`에 보관했다.

| Editor 정지 계측 | CPU p50 / p95 (ms) | GPU p50 / p95 (ms) | 드로우 콜 p50 / p95 |
|---|---:|---:|---:|
| [Phase 3 전](river-basin-grass-reference-evidence/phase3-before-play.txt) | 23.13 / 249.10 | 21.24 / 25.25 | 11,039 / 11,039 |
| [Phase 3 전 재측정](river-basin-grass-reference-evidence/phase3-before-repeat-play.txt) | 24.11 / 34.88 | 21.39 / 29.17 | 11,034 / 11,034 |
| [Phase 3 후](river-basin-grass-reference-evidence/phase3-after-play.txt) | 24.25 / 32.34 | 21.62 / 28.09 | 11,040 / 11,648 |

첫 기준 측정에 순간 지연이 있었고, 적용 후 드로우 콜 상위 표본이 늘었다. 표의 값에는 Editor 전체 프레임의 변동이 포함된다. 이후 별도 월드 기복 저작으로 제작 지형이 바뀌어 같은 조건의 추가 계측은 보류했다.

- **최신 저장본 확인.** 월드 기복 작업 후에도 풀밭 패치 해시, 서부 숲 저장 풀 2,304,680개, 누락 참조 0을 확인했다. 남쪽 지면 높이는 Y 64.32m에서 73.16m로 올라갔다. 같은 X/Z와 지면 기준 카메라 높이로 세 구도를 다시 찍었으며, 최신 지형에서는 낮은 시점으로 사면을 따라 이어지는 풀과 성근 나무가 보인다. 촬영 오류 0건이다. [최신 지형의 풀밭](river-basin-grass-reference-evidence/phase3-current-terrain.jpg) · [최신 에셋 재검사](river-basin-grass-reference-evidence/phase3-final-recheck.txt)

###### 사용자 테스트 대기

같은 `RiverBasin4km`를 Editor Play하고 평소 쓰는 입력 장치로 이동한다. X/Z 좌표를 기준으로 아래 항목을 확인한다.

| ID | 이동과 관찰 지점 | 통과 조건 |
|---|---|---|
| FG-01 풀 형태와 시야 | 숲 안 (1000, 2450), 능선 (1360, 2850), 공터 (1130, 2330)를 기본 TPS 시점으로 걷는다. | 가을 풀과 억새 높이가 구별되고 캐릭터와 길이 걷는 동안 읽힌다. |
| FG-02 바람과 밟기 | 풀밭 남쪽 (1170, 2250)에서 북쪽을 보고 잠깐 서 있다가 (1170, 2270)까지 걷는다. 돌아서 경계를 다시 지난다. | 멀리 있는 풀 결은 풍향을 따르고, 가까운 풀은 발 주변에서만 눕는다. 수관과 풀 밀도 전환에 눈에 띄는 띠가 없다. |
| FG-03 사당 능선 | (1310, 3235)에서 기존 사당길을 따라 걷는다. | 공유 재질의 바람으로 길과 진행 표식이 가리지 않는다. |

결과는 `항목 ID / 통과·실패·미수행 / 좌표·방향 / 화면 또는 로그`로 남긴다. 실제 퀘스트 진행과 저장·재로드, Terrain Detail 모션 벡터 버퍼의 픽셀 판정은 미수행이다. Player·Addressables 빌드와 Player 성능 계측은 사용자 지시 대기다.

#### 반응형 수목 승격 설계와 Play 실측

**구현 및 제작 씬 반영.** 4km 씬의 지형 10장에 목서 6종 **6,118그루**를 연결했다. 씬은 RiverBasin 날씨 시간표를 사용한다. 사당 능선 금색·구리색 목서를 위해 기존 씬의 색과 임포스터 텍스처를 반응형 재질 복제본에 옮겼다. 자작나무·너도밤나무·어린 활엽수와 관목은 TerrainTree로 유지한다. [지형 나무 승격 설계](../../Assets/docs/design/world-art/environment/TERRAIN_TREE_PROMOTION_DESIGN.md) · [씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)

- **채택 방향.** 평소에는 TerrainTree로 두고 상호작용이 일어난 나무만 반응형 GameObject로 승격한다. 원장에는 승격된 실물만 등록하고 바뀐 나무만 저장한다. 선례는 지형 풀을 셀 단위 C# 객체로 반응시키는 `TerrainDetailReaction`이다. [지형 나무 승격 설계](../../Assets/docs/design/world-art/environment/TERRAIN_TREE_PROMOTION_DESIGN.md)
- **Play 실측(Terrain_1_4, 1,820그루).** 런타임 사본 생성 3.7ms·4.3MB, 한 그루 숨김 직후 정지 없음(3회), 숨긴 나무는 화면에서 사라짐. 줄기 충돌은 콜라이더 데이터 재지정(3~7ms)으로만 제거됐고 대조 나무는 유지됐다. 플레이어가 그 타일에 서 있는 상태에서 재지정해도 KCC 안정 지지가 끊기지 않았다. 사본 직후 한 나무의 높이가 0으로 읽힌 결과는 원인 미확인이며 설계 문서에 기록했다. 한 그루의 프로토타입 교체는 Unity가 거부해 밑동 교체는 전체 배열 재설정(0.08~0.38ms)으로 한다. Play 종료 뒤 원본 에셋과 씬은 변경 없음. 세부는 설계 문서의 실측 표.
- **상한과 저장.** 32개를 넘기지 않으며 타격으로 풀이 찼을 때 반응이 끝난 실물을 먼저 반환한다. 전부 반응 중이면 새 타격은 받지 못하며 지속 열은 다시 조회한다. 베이기 전의 부분 피해는 현재 실행에서만 이어지고, 저장은 벌목·연소 변화만 기록한다. 낙하·연소 중인 목서는 로드 후 기존 원장에서 이어진다.
- **측정 중 정지.** 2.0초(숨김 전)와 1.3초(숨김 15프레임 뒤) 정지가 있었다. 반복 3회에서는 숨김 직후 정지가 없어 숨김 비용으로 판정하지 않았다. 앞서 기록된 원인 미상 정지와 함께 Profiler로 분류할 항목이다.
- **재배치.** 원본 TerrainData를 다시 저작한 뒤 [4km 승격 연결 도구](../../Assets/02.Scripts/Editor/RiverBasinTerrainTreeAuthoring.cs)의 `Apply()`를 실행한다. 같은 위치·수종의 ID를 이어받는다. 이 단계에서는 지형이나 풀 배치 스크립트를 재실행하지 않았다.
- **구현본 Editor Play 확인.** 날씨 준비, 지형 사본 10장 순서 검증, 초기 활성 실물 0개를 확인했다. `Terrain_1_4`의 금색 목서(X 925 / Z 2332)에 줄기 프록시를 통해 피해를 전달하니 내구도 100→99, 등록 1개, 저장 0개였다. 다음 프레임에 지형 원형 숨김, 지형 줄기 레이 0/4, 플레이어 안정 지지를 확인했다. 벌목 타격은 낙하 레코드를 만들고 보상 확정 뒤 같은 인덱스를 밑동으로 바꿨다. 메모리에서 반응 원장을 내보내고 다시 불러와 밑동 유지와 원본 프로토타입 보존을 확인했다. 근처 목서에 지속 열원을 두자 별도로 승격되고 그을림이 저장됐다.
- **자동 검증.** Unity 정상 컴파일과 반응형 환경 통합 EditMode 56개가 통과했다. 부분 피해는 실제 저장에서 제외되며 같은 실행에서 재승격하면 이어지고, 벌목은 저장·로드 뒤 한 레코드로 남는 회귀 검사를 포함한다. 에셋 감사에서 ID 6,118개 중복 0건, 표시·승격 프리팹의 LOD 렌더러 24개 메시·재질 일치와 인스턴싱 활성화를 확인했다. 작업 전과 Play 종료 후 원본 TerrainData 12장 SHA-256이 모두 일치했다.
- **사용자 테스트 대기.** 연못 동쪽에서 목서 타격·낙하·밑동과 자작나무의 이동 충돌을 확인하고, 사당 능선에서 금색·구리색 목서의 피격 전후 수관 색과 LOD 전환을 확인한다. 연쇄 전파, 비에 의한 소화, 실제 저장 슬롯 재진입, 여러 타일 동시 승격 시 프레임 시간도 남아 있다. 원거리 직격 투사체는 플레이어 주변 프록시가 있는 범위에서 미승격 나무를 맞힐 수 있다. 불이 꺼져 살아남은 나무의 그을림은 다음 승격 때 다시 표시된다.

### 4km 숲 연못 동쪽 식생 적용

2026-11-21 **제작 씬 반영 및 정적 검사 통과.** 약 126m 숲길의 시작은 X 988 / Z 2320, 연못 조망은 X 880 / Z 2257이다. [환경 에셋 카탈로그](../asset-catalog/tree.html)에서 확인한 Dreamscape EastLands 목서, Toon Golden Valley 자작나무·너도밤나무·어린 나무와 Farmlands 어린 활엽수의 검증 배치를 [RiverBasin4km 제작 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)에 옮겼다. 연못 측 자작나무 21그루, 숲 내부 목서와 너도밤나무, 밝은 틈의 어린 나무 44그루를 포함해 TerrainTree 219그루와 프리팹 관목 26개를 배치했다.

- `Terrain_1_4`에 0.5m 셀, 풀 65,277개, 풀 표시 90m, 나무 표시 340m 및 빌보드 기준 95m를 적용했다. 길 중심이 읽히도록 검증본 도색을 경로 주변 409개 표본에 섞었다. 타일 가장자리의 재질 가중치는 기존 제작 데이터를 유지했다.
- 검증 씬에서 1080p 가상 패드 왕복 시 지지 표본 1,482개 모두 안정이었다. 연못 앞 회전 캡처에서 캐릭터와 호안이 보였다. [검증 씬 왕복 로그](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Review_PlayMode.txt) · [연못 방향](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Review_Orbit2.png)
- 제작 씬을 다시 열어 Terrain 및 TerrainCollider 64쌍, Missing Script 0건, 대상 타일 전체 높이 일치, 동쪽 경계 높이차 0m를 확인했다. [제작 씬 에셋 검사](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Assets.txt)
- **제작 씬 Editor Play 통과.** B1 기본 시작점에서 로드한 뒤 검증 입력으로만 숲길 시작점에 이동했다. 가상 패드로 126m 구간을 왕복해 안정 지지 888/888을 확인했다. 두 방향에서 낮은 풀 사이의 길이 읽혔고 연못 앞과 숲 내부에서 카메라를 회전한 캡처에 캐릭터가 보였다. [숲길 진입](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Production_Start.png) · [역방향](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Production_MidBack.png) · [연못이 열리는 장면](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Production_Orbit3.png) · [수관 아래 회전](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Canopy_Orbit3.png) · [왕복 로그](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Production_PlayMode.txt)
- **상주 타일 경계 Editor Play 통과.** X 1008 / Z 2329에서 X 1040 / Z 2344까지 왕복해 X 1024를 양방향으로 넘었다. 안정 지지 197/197. 서쪽을 바라본 두 경계 캡처에서 가까운 풀과 수관의 갑작스러운 누락이 관찰되지 않았다. 이 경로는 `Terrain_1_4`와 `Terrain_2_4`가 모두 상주한 제작 씬이다. [동쪽으로 통과](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Boundary_CrossEast.png) · [서쪽으로 복귀](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Boundary_CrossWest.png) · [경계 로그](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_Boundary_PlayMode.txt)

**1080p Editor 성능 목표 미달.** RTX 3060 Ti, D3D11, PC 품질·렌더 스케일 1, VSync 1에서 워밍업 후 같은 카메라와 가상 패드 경로로 한 번씩 왕복했다. 캡처 없이 `FrameTimingManager`의 유효 CPU/GPU 표본을 집계했다. CPU는 프레임 전체 시간이다.

| 설정 | CPU p95 진행 / 복귀 | GPU p95 진행 / 복귀 |
|---|---:|---:|
| 저장값 · 풀 90m, 표시 밀도 100%, 나무 340m | 39.27 / 41.88ms | 21.33 / 20.30ms |
| 비교값 · 풀 70m, 표시 밀도 75%, 나무 280m | 39.84 / 44.84ms | 22.29 / 21.34ms |
| 진단 · 대상 타일 풀·수목과 관목 숨김 | 35.10 / 41.99ms | 19.39 / 22.23ms |

나무 빌보드 기준은 두 표시 설정에서 95m였다. 거리·밀도 축소는 CPU·GPU p95를 줄이지 못했다. 식생 숨김도 16.7ms를 넘어 이 구간 식생만 줄여서 목표를 달성할 근거는 부족하다. 서부 숲의 다음 길 주변 배치는 풀 0.5m 셀과 현재 표시 밀도·거리, 나무 표시 340m·빌보드 95m를 기준으로 시작한다. 범위를 넓힐 때는 16.7ms 목표와 청크 경계 재검증을 근거로 밀도·거리를 확정한다. 모든 Play 확인 뒤 Console 오류 0건이었고, 진단값을 복원하고 편집 모드로 돌아왔다. [저장값 계측](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_BenchA_PlayMode.txt) · [거리·밀도 축소 계측](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_BenchB_PlayMode.txt) · [식생 숨김 비교](../../Tools/RiverBasinWorld/Rebuild/ForestPilot_BenchNoVegetation_PlayMode.txt)

실제 입력으로 길과 캐릭터 실루엣을 확인하는 미술 인수는 사용자 테스트 대기다. 이 월드의 청크 로드·해제 및 Player 계측은 미수행이다. 다음 본선 제작 순서는 호수 마을과 B3 → B1 → 성채 연결이다. [본선 제작 방침](#4km-다음-제작-방침)

### 4km 서부 숲 식생 검증

2026-11-21 **서부 숲 식생 검증 씬 저장.** [RiverBasin4km_ForestReview](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/ForestReview/RiverBasin4km_ForestReview.unity)을 단독으로 열고 Play한다. 숲길 입구에서 출발해 수관 아래를 지나 연못이 보이는 지점까지 약 126m를 왕복한다. 제작 월드의 Terrain 9장을 배경으로 사용하고 식생과 검증용 지면 도색은 `Terrain_1_4` 복제본에 넣었다. 제작 씬과 원본 `Terrain_1_4` 파일의 SHA-256을 대조했다. [숲 검증 경로 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#서부-가을-숲과-수변-식생)

- 기존 Dreamscape 수목 6개 프리팹으로 TerrainTree 175그루를 배치했다. 길 가장자리와 수목 아래에 관목 26개, 길과 물가를 피해 풀 Terrain Detail 2종 13,175개를 저장했다. 풀 표시 거리는 90m, 나무 빌보드 거리 설정은 95m다. 길 132개 표본의 최대 경사는 12.69°다.
- 첫 카메라 검사에서 나무와 관목은 보였고 풀은 화면에 나타나지 않았다. 기존 Terrain Detail 전용 풀 프리팹과 `InstanceCountMode` 조합으로 바꾼 뒤 TPS 근경에서 풀 렌더링을 확인했다. [식생 적용 TPS](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Plants.png) · [같은 위치에서 식생 숨김](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_WithoutPlants.png) · [숲 내부](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Canopy.png) · [연못 앞](../../Tools/RiverBasinWorld/Rebuild/ForestReview_TPS_Pond.png)
- **Editor Play 검증 통과.** GameManager `Ready`와 TPS `InGame`에서 가상 패드로 같은 길을 왕복했다. KCC가 이동 표본 2,537개에서 안정 지지를 유지했고 Console 오류는 0건이었다. 2560×1440 Editor의 이동 중 p95는 23.47ms다. 같은 정지 구도에서 식생을 켬 → 끔 → 켬으로 바꾼 세 구간의 p50은 16.13 → 15.88 → 16.11ms, p95는 23.05 → 24.81 → 24.56ms였다. 이 반복의 변동 폭으로 개별 식생 비용을 판정할 수 없다. [왕복·프레임 기록](../../Tools/RiverBasinWorld/Rebuild/ForestReview_PlayMode.txt)
- 사용자 확인 항목은 양방향 길 가독성, 수관 그늘에서의 풀 색과 밀도, 나무 근경과 원경 전환, 연못이 처음 드러나는 구도다. 서부 전역 밀도 확정, P06 재생성, 청크 발행과 1080p CPU/GPU 계측은 후속 범위다.

#### 사용자 제보에 따른 잔디 밀도 보정

숲 바닥에서 큰 풀 포기 사이가 넓게 비어 보인다는 제보를 반영했다. Terrain Detail 격자를 1m에서 0.5m로 줄이고, 배치 수를 13,175개에서 65,277개로 늘렸다. 낮은 풀 50,117개를 바탕으로 중간 풀 14,108개와 큰 포기 1,052개를 섞었다. 숲길 중심에서 2.25m를 비우고 군락과 물가에서 밀도를 완만하게 조절했다. 보고 시점의 Scene View 카메라 자세와 화각을 같은 해상도로 재현해 빈 지면이 줄었는지 확인했다. [보정 전](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Grass_Before.png) · [보정 후](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Grass_After.png)

#### 수형 다양화와 어린 나무

처음 사용한 Dreamscape 수목 6종은 목서 계열의 색상과 수형 변형이었다. Toon Golden Valley 자작나무·너도밤나무와 Farmlands 활엽수·어린 나무 후보 26개를 Unity에서 로드해 높이, 수관 폭, LOD, 셰이더 지원과 충돌 참조를 비교했다. 검증 구간의 시야와 수관 간격에 맞는 중형 후보를 선택했다.

| 배치 | 적용한 프리팹과 수량 |
|---|---|
| 연못 쪽 밝은 사면 | `TGV_Birch_Tree_04B` 21그루. 가는 밝은 줄기가 기존 목서 사이에서 읽힌다. |
| 숲 안쪽 수관 | `TGV_Beech_Tree_03B` 11그루, `TGV_Beech_Tree_05A` 8그루, `PF_Farm_GenTree_B_Young_01` 4그루. 넓고 낮은 수관을 다른 높이로 섞었다. |
| 수관 사이 작은 군락 | `TGV_Birch_Sapling_01B` 12그루, `TGV_Beech_Sapling_01A` 32그루. 약 2.3~4m 높이의 어린 나무를 17개 군락으로 배치했다. |

기존 큰 나무 중 44그루를 교체하고 어린 나무 44그루를 추가했다. 저장본에는 수목 프리팹 12종과 나무 219그루가 있으며, 가장 가까운 어린 나무 중심은 숲길 중심과 약 6.59m 떨어져 있다. 물가 아래와 연못 조망을 여는 경로 끝은 군락 배치에서 비웠다. 동일한 제보 시점에서 줄기와 수관이 구분되는 화면과 어린 나무 근접 화면을 확인했다. [혼합 수관과 잔디](../../Tools/RiverBasinWorld/Rebuild/ForestReview_MixedTrees.png) · [어린 자작나무 근접](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Saplings.png)

TPS 입구 캡처에서 Farmlands 어린 활엽수의 낮은 가지가 플레이어를 가리는 것을 확인해 해당 나무를 숲 안쪽으로 옮겼다. 같은 시작 카메라에서 플레이어와 길이 보이는 것을 확인했다. 최종 가상 패드 왕복에서 이동 표본 2,589개가 모두 안정 지지였고 Console 오류는 0건이었다. 2560×1440 Editor 이동 p95는 23.66ms다. 실제 카메라 조작과 식생 외형은 사용자 테스트 대기이며, 1080p CPU·GPU 계측은 미수행이다. [보정 후 TPS 입구](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Mixed_TPS_Plants.png) · [최종 왕복 기록](../../Tools/RiverBasinWorld/Rebuild/ForestReview_Mixed_PlayMode.txt)

### 4km 다음 제작 방침

2026-11-21 **설계 반영.** 다음 납품 구간은 호수 마을 출구에서 B3와 B1을 지나 중앙 성채 정문까지 이어지는 약 1.9km 본선이다. B1 성능과 Editor 청크 로딩의 기준을 먼저 잡고, B3 단면과 도로 소켓을 실측해 본선의 건조 왕복을 제작한다. 기존 61.96ms는 캡처가 섞인 Editor p95 기록이므로 병목과 목표 품질 검증은 다음 계측에서 확인한다. 이번 변경은 설계·문서 연결에 반영했으며 신규 씬 제작과 계측은 미수행이다. [B1 이후 제작 방침](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#b1-이후-제작-방침)

### 4km 미연결 구역 동선 설계

2026-11-22 **G2 블록아웃 구현 반영 · Editor 형상 검사 통과 · 사용자 테스트 대기.** 열린 Unity의 Terrain 64개와 실제 수면 메시 35개를 기준으로 경로를 조정했다. [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 `미연결 구역 동선 블록아웃`에 길 메시 11개와 갈림길·전망 표지를 저장했다. R4 남안 B5는 후속 피드백으로 수영 구간으로 바꿨다. [초기 블록아웃 지도](river-basin-offroute-evidence/river-basin-offroute-blockout-map.png) · [설계와 구현 결정](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_OFFROUTE_REGIONS_DESIGN.md#8-구현-결정과-저작-원본)

- **구현 결정.** R1은 첫 방문부터 양방향으로 열었다. R2는 기존 경로 ID를 쓰고 성채 동쪽으로 접속한다. R3는 요새 아래에서 R5로 합류한다. R5는 능선과 사막 남쪽을 잇는다. R4는 동안과 남안에서 유출 하천을 수영하거나 비행으로 건너 호수 마을에 닿고, 해안 선착장으로 가지길을 낸다.
- **Editor 검사 통과.** 초기 육상 11구간의 수면 침범 표본 0과 중심선 위 기존 충돌체 레이 0을 확인했다. 교량 철거 전 수치와 변경 후 입수·상륙 경사를 각각 보존했다. [초기 수치 근거](river-basin-offroute-evidence/blockout-editor-audit.txt) · [남안 수영 검사](#4km-남부-호수-섬)
- **Unity 고정 구도.** [남안 수영 진입부](river-basin-lakes-evidence/outlet-after.png) · [R4 호수 동안](river-basin-offroute-evidence/R4-shore-blockout.png) · [R1b 연못 가지길](river-basin-offroute-evidence/R1b-pond-blockout.png)
- **남은 제작.** R4a는 1m 표본에서 p90 26.9%, 최대 38.2%다. 사용자 주행 결과를 바탕으로 지그재그와 사면 폭을 보정한다. 호수 마을에서 B1로 돌아오는 기존 `lake-loop` 남서 접속부와 B3는 본선 제작이 남아 있어 동쪽 대순환 전체 인수는 대기다. 씬의 활성 NavMesh가 없어 경로 연결 자동 검사는 미수행이다. P04/P05 도로·교량 마감, P06 식생·지면, 실제 조우·휴식지점, Player·Addressables 검증은 미수행이다.
- **저작 원본과 보존.** [중심선 CSV](../../Tools/RiverBasinWorld/Rebuild/offroute-blockout-4km.csv) · [저작 스크립트](../../Tools/RiverBasinWorld/Rebuild/offroute-blockout.cs.txt). 현재 지형과 `terrain-plan-4km.json`, 초기 후보 지도, 저장 씬의 검사 자료를 보존했다. 적용 직전 사용자 변경을 포함한 씬 사본은 로컬 `output/RiverBasinOffroute/before-20261122-094328-082.unity`에 있다. 임시 격자 표본·탐색·비교 파일 4개(8.42MB)를 정리했다.

**사용자 테스트 대기.** `RiverBasin4km`를 Editor에서 열고 별도 저장 불러오기 없이 Play한다. 블록아웃 지도의 X/Z 위치와 갈림길 표지를 참고해 키보드나 게임패드로 확인한다.

| ID | 시작과 확인 지점 | 기대 결과 |
|---|---|---|
| RB4-R01 | 강가 마을 (1312, 2060) → 연못 분기 (1152, 2640) → 사당 (1280, 3256). 연못 가지길과 역방향도 확인 | 표지와 노면이 이어지고 숲 굽이에서 카메라가 이동 방향을 가리지 않는다. |
| RB4-R02 | B1 동쪽 (1896, 1992) → 요새 아래 (3376, 2016). 성채 동쪽 (2324, 2116) → 요새 (3336, 2132)도 확인 | 저지대 길과 성채 접근로가 모두 R5로 연결되고 요새 쪽 분기가 읽힌다. |
| RB4-R03 | 사막 남쪽 (3664, 1472) → 호수 동안 (3392, 1000) | 걷기·달리기·회피로 내려올 수 있고 급경사에서 미끄러짐·낙하·카메라 가림이 없다. |
| RB4-R04 | 동안 → 남안 수영 횡단 (3114, 564) → 호수 마을 (2320, 972), 역방향 포함 | 수영으로 걸림 없이 입수·상륙하고, 비행이 가능할 때 반대편 착지 여유가 있다. |
| RB4-R05 | 성채 → 요새 → 사막 → 동안 → 남안 횡단 → 호수 마을 | 길을 놓친 지점과 도착 시간을 기록한다. 호수 마을에서 성채로 돌아오는 본선은 후속 확인한다. |
| RB4-R06 | R1 숲 굽이·R4 하강·남안 횡단에서 밤과 전투 직후 카메라 확인 | 밝기와 카메라가 달라져도 다음 진행 방향과 발밑이 읽힌다. |

결과는 `ID / 통과·실패·미수행 / 입력 장치와 위치 / 실제 증상 / 화면 또는 로그`로 기록한다.

### 4km B1 석교와 상류 수영로

2026-11-21 **B1 플레이 구간 구현 반영.** 당시 [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity)의 단독 Play는 B1 남서쪽 접근로에서 시작했다. 테스트는 활성 저장 슬롯 없이 진행했다. 현재 시작점은 [호수 마을 남쪽 입구](#호수-마을-첫-방문-아트-패스)다.

- **교량.** 기존 석교 원본의 석재 재질, LOD 3단, 상판·난간 충돌을 재사용했다. 양안 착지 실측에 맞춰 길이 48m, 상판 높이 Y 52.25m로 배치하고 아치 하부와 충돌 메시를 하상까지 내렸다. `Castle_Moat_Bridge`의 기존 Solid 카메라 규칙을 사용한다. 양안 세 경로의 지지면 레이 201개에서 누락 0, 침수 0, 최대 표본 경사 6.38°를 확인했다. [P04 계획](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/B1_Plan.asset) · [단면·지지면 기록](../../Tools/RiverBasinWorld/Rebuild/B1_SectionAndDeck.json)
- **수계.** X 1655 / Z 1961.6의 횡단에서 수면 폭 약 28m, 수심 1.8m 이상 연속 폭 약 17m, 최대 3.19m를 확인했다. 양쪽 수중 경사 약 21.5~21.7°, 물살 0.35m/s다. 수영 어댑터는 수면과 같은 P05 수역을 조회한다. 교량과 상류를 하나의 수역 ID로 연결하고 겹치는 레거시 수면만 잘라냈다. 기존 높이 패치 3건은 적용 순서를 보존한 채 P04·P05보다 먼저 평가하도록 옮겼고, 이 전환만 비교했을 때 기존 높이 16,785,409개는 모두 같았다. [P05 수계](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/B1_WaterSystem.asset)
- **경관·음향.** 양안의 자갈길을 교대 접근로에 연결하고 하상은 자갈과 암반으로 도색했다. 수영 진출입부는 바위와 갈대 사이로 열어 두고, 기존 수목·암석·성채 프리팹을 배치했다. 교량과 수영로에 12초 합성 물소리의 3D 루프를 놓았다. P05 수면 셰이더를 기존 수중 합성에 연결했다. 카메라를 수면 아래에 둔 Play 캡처에서 광학 프로필 `UpperOptics`, 수역 1개, 체적 셀 11개와 합성 실행을 확인했다. P05 수계를 다시 생성하면 씬의 `UnderwaterRegion`에서 새 수영 어댑터를 다시 연결하는 확인이 필요하다. [교량 진입](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Play_Bridge.png) · [가상 패드 수영](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Play_Swim.png) · [성채 원경](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_B1_Keep.png)
- **출수 재현과 수정.** 첫 가상 패드 실행에서 수영 부력이 플레이어를 양쪽의 완만한 하상에 붙잡아 걸어서 나오지 못했다. 수면에서 진행 방향의 걸을 수 있는 경사를 감지하면 발 높이가 하상을 따라가도록 수정했다.
- **검증.** Unity 재개방에서 Terrain 64장, 콜라이더 참조, 공유 경계 112곳 높이 차 0m, Missing Script 0을 확인했다. 양안 자갈길 중심을 따라 캡슐 겹침 464곳을 검사했고 장애물과 겹치는 표본은 0이었다. 가상 패드로 석교 양방향, 수영 양방향, 각각 입수한 둑으로 복귀 총 6개가 통과했다. 두 물소리 소스의 재생을 확인했다. 도구 입력으로 시작 좌표를 옮긴 뒤 실제 게임 입력과 KCC로 이동했다. 관련 EditMode 110/110 통과, 실패·건너뜀 0이다. 단독 시작에서 서비스 미등록과 캐릭터 무게 프로필 폴백 경고가 있었다. [Play Mode 기록](../../Tools/RiverBasinWorld/Rebuild/B1_PlayMode.txt)
- **남은 인수.** 실제 입력·회피·카메라 회전, 물소리의 거리감·반복감 및 미술 품질은 사용자 테스트 대기다. 수중 합성 연결 후 가상 패드 실행에서 Editor 프레임 시간 95백분위는 61.96ms였다. 캡처가 포함된 Editor 값이며 1080p 목표 품질의 CPU/GPU 계측은 미수행이다. 월드 Bake·스트리밍과 호수 마을에서 성채까지 이어지는 완성 동선은 미수행이다. B2~B4, 폭포, 사막과 숲은 후속 제작 범위다.

### 4km 게임월드 제작 인계

2026-11-21 **제작 착수 전 기준 기록.** B1 상류 양안 수영로와 석교를 첫 인수 구간으로 잡고, B2~B4, 북부 폭포, 사암 사막, 서부 가을 숲으로 이어가는 설계를 확정했다. 최신 반영 범위는 [B1 작업 상태](#4km-b1-석교와-상류-수영로)에서 확인한다. [4km 게임월드 제작 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#4km-게임월드-제작) · [월드와 아트 위키](../wiki/world-art.md)

- 씬 직렬화에서 Terrain 64장과 수면 메시 35개를 대조했다. 4km Authoring의 `_planning`, `_ecology`, `_waterSystem`은 미연결 상태다. 제작용 Profile 참조는 높이 범위 −32~768m인 `Production/Profile.asset`을 가리킨다. [4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km Authoring](../../Assets/10.Datas/WorldBuilder/6effd90b68ea4b3d8f8caa5197fcfe34_Authoring.asset)
- 최종 Joined RAW에서 계획 수위 기준으로 교량마다 1m 간격 하상 횡단 표본을 뽑았다. 중앙 깊이는 B1~B4 순으로 2.71/2.90/2.64/2.63m, 연속 젖은 폭은 약 22/22/26/28m다. 지류 교량의 계획 gap 22m는 현장 경간 확정에 쓰기 전에 다시 재야 한다. B1 상류 X 1670 / Z 1960에서는 기존 계획 직선상 수심이 약 0.01m이고 횡단면 가장 깊은 표본은 약 2.86m여서, 저작 시 현재 물길에 맞춰 수영 중심선을 옮겨야 한다. RAW 근사 단면이며 실제 수면 메시·물리 체적의 측정은 후속 제작에서 수행한다. [4km 계획 JSON](../../Tools/RiverBasinWorld/Rebuild/terrain-plan-4km.json) · [측정 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Joined_R16LE_SouthFirst.raw)
- 저장 캡처에서 동부 사면은 녹색 지표가 우세했다. 채석 요새 동쪽에서 사암 단구와 모래 분지를 한 구역으로 설계하고, 숲은 서부 연못과 사당으로 이어지는 수관·하층 식생·수변 군락을 정했다. [동부 비교 기준 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_EastSlopeFinal.png)
- 다음 작업은 보정 지형 재평가와 B1의 실제 양안·수영 단면 확정이다. 현장 좌표·치수, Unity 적용, 본편 입력·수영·카메라·음향·프레임 성능 검증은 미수행이다. [제작 순서와 인수](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#제작-순서와-인수)

### 타일 이음새·합류부·설산 보정

2026-11-21 사용자 시점의 직선 재질 이음새, 도로 사면의 급한 꺾임, 하천 교차부의 돌출과 적설이 읽히지 않는 문제를 보정했다.

- 지형 64장의 레이어 순서를 통일하고 공유 경계 양쪽 20m에서 가중치를 섞었다. 저장된 경계 112곳에서 재질 가중치 최대차는 0.00392로, 8비트 한 단계다. 적용 직전 부동소수 기준 최대차는 약 0.941이었다.
- 가까운 물길의 폭·수위를 합류부에서 섞고, 도로의 중앙 높이를 유지하면서 물가와 연결되는 사면의 영향량을 줄였다. 교량 예정지 둘레의 원형 높이 마스크는 하천 횡단면 기준으로 바꾸고, 지류 끝이 본류 바깥 둑을 뚫고 나온 부분을 잘랐다. B1 기존 암반 띠의 가중치는 수면 고도에 따라 자갈과 풀로 옮겼다. [최종 HeightMap](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Joined_R16LE_SouthFirst.raw)은 불변 Base 위의 절대 높이 패치 3건으로 적용했다. 수면 메시 35개를 맞춰 저장했고 물길 중심선 표본 1,194개에서 단절이 검출되지 않았다. [경사·수계 검사](../../Tools/RiverBasinWorld/Rebuild/joined-height-validation.json)
- 설산 레이어에 내장 ImageGen으로 생성한 [알파인 적설 알베도](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/Layers/AlpineSnow_Albedo.png)를 적용했다. 높은 완사면과 눈이 쌓이기 쉬운 오목한 면의 눈 비중을 올리고 가파른 절벽은 암반이 드러나게 했다. 북부 470m 이상 표본에서 평균 눈 가중치는 46.7%다.
- 최종 Unity 확인에서 공유 경계 높이 차 0m, RAW 대비 최대 오차 0.01223m, 레이어와 TerrainCollider 참조를 확인했다. 합류부 바깥 둑의 물 돌출은 사라졌고, B1 양안에 옅은 띠가 남아 있다. [설산 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_SnowRange.png) · [합류부](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_RiverJunction.png) · [사면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Joined_SlopeTransition.png). 실제 플레이·수영·프레임 성능 검사는 미수행이다.

ImageGen 프롬프트

> Generate a production game terrain seamless repeatable square PBR base-color texture tile, top-down planar orthographic, raw alpine snow material. Neutral off-white with a subtle cool blue cast inside very small shadows, soft compact windblown fine snow and softly packed fine grain, slightly varied micro-scale soft shallow waves, seamless tile periodic in both X and Y. Flat diffuse albedo only with uniform exposure, palette predominantly matte bright white. Do not include rocks, dirt, any ground objects, trees, footprints, large cracks, border, drop shadows, words, lighting gradient, perspective, horizon, scenery, mountains or mockup. Very low visual contrast and controlled fine grain for Unity Terrain tiling across hundreds of meters. Fill every pixel with the texture.

### 동부 사면 재질 반복 보정

2026-11-21 사용자 Scene View 구도에서 동일한 암석 패턴이 전경 절개면을 덮고, 뒤쪽 사면에도 풀과 암반 띠가 유사한 대비로 이어지는 것을 확인했다. 화면 중심과 좌우를 쏜 TerrainCollider 레이로 동부·중동부 사면을 식별했다.

- 동부 TerrainData 16장에 원래 15.3m 암석과 다른 알베도·노멀을 쓰는 40×29m 풍화암 레이어를 섞었다. 경사·고도·볼록한 면과 낮은 면을 바탕으로 암반과 퇴적토·건조 풀의 분포를 조정했다. 구릉 실루엣과 높이 데이터는 유지했다.
- 같은 카메라 위치에서 반복이 많이 보였던 전경 암석을 비교했다. 적용 타일 16장의 텍스처 참조, 레이어 8개 이하와 양자화된 가중치 합 0.9922~1.0118을 확인했다. HeightMap 대비 최대 오차는 이전과 같은 0.01223m였다. 플레이 성능은 미측정이다. [적용 후 Unity 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_EastSlopeFinal.png)

### 북부 산맥 원경 재질 보정

2026-11-21 사용자 원경 캡처를 보고 북부 산맥을 고정 카메라에서 확인했다. 대표 타일의 바위 텍스처와 가중치가 연결돼 있었으며, Unity Terrain 원경 전환 거리를 일시적으로 1,000m에서 3,500m로 바꾼 비교에서도 산맥이 균일한 회색으로 보이는 현상은 남았다. 이전 북부 대표 타일 두 곳의 회색 바위 가중치는 평균 85~88%였다.

- 북부 TerrainData 21장에 프로젝트의 기존 Cliff 알베도·노멀을 사용하는 전용 절벽 레이어를 연결했다. 고도, 경사, 사면 방향에 따라 절벽과 눈 노출을 나눴다. 대표 `Terrain_4_7`의 최종 평균은 바위 63.0%, 절벽 13.9%, 눈 15.3%다.
- Unity에서 적용 후 바위·눈·절벽 레이어를 읽고 고정 시점의 원경을 촬영했다. 적용 타일의 RAW 대비 높이 오차는 이전과 같은 최대 약 0.01223m였다. 원경 전환 거리는 기존 1,000m다. 실제 플레이 중 프레임 성능과 미술 인수는 미수행이다. [보정 후 Unity 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_NorthCliffFinal.png)

### 4km 제작용 HeightMap 적용

기존 `RiverBasin4km` 씬과 월드 ID를 유지하고 4km 월드 좌표에서 생성한 제작 지형으로 교체했다. 북부는 봉우리·안부·침식 골짜기로 구분하고 동부에는 층상 협곡 사면을 저작했다. 원래 있던 두 호수·숲 연못·7개 물길과 거점 연결 관계를 이어갔다.

- [같은 RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km 원시 HeightMap](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Production_R16LE_SouthFirst.raw) · [저작 좌표·산체 계획](../../Tools/RiverBasinWorld/Rebuild/terrain-plan-4km.json) · [지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_Production_Relief.png)
- 4,096m 정사각, 4,097² 표본, 1m 간격이다. 프로필 수직 범위 −32~768m와 실제 고도 −30.58~688.39m를 구분한다. 제작 Base는 `Production/Height.bytes`이며 이전 `Source/BaseHeight.bytes`와 미리보기 TerrainData를 보존했다. [4km 원본 검증](../../Tools/RiverBasinWorld/Rebuild/production-height-validation.json)
- 512m TerrainData·콜라이더 64장을 씬에 연결하고, 실제 지형 텍스처 6종의 가중치를 경사·고도·호안 위치로 저장했다. 수면은 타일 단위 메시 35개와 기존 Lake Water 계열의 전용 복제 재질을 사용한다. 넓고 평평한 수면 내부는 큰 삼각형으로, 호안과 강은 작은 삼각형으로 구성했다. [산맥 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_NorthRange.png) · [전체 조감](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_Overview.png) · [상류 근접](../../Tools/RiverBasinWorld/Rebuild/RiverBasin4km_Final_UpperRiver.png)
- 저장 씬 재개방에서 타일 경계 112곳의 높이 차 0m, 새 RAW 대비 최대 오차 0.01223m, 콜라이더·수면 참조를 확인했다. 물길 중심선 1,194개 표본의 단절과 교량 예정 구간 외 동선 1,245개 표본의 침수는 0이다. 일반 순환로 중심선 최대 경사는 8.86°다. 고산 접근로는 9.63°다.
- 조감에서 좁은 물길 일부가 점선처럼 보이는 표현은 남아 있다. 실제 도로·교량 배치, 수영 판정, TPS 이동 및 미술 인수, Bake·스트리밍은 미수행이다. 첫 4km 비교판은 아래 이력에 남긴다.

### 강둑 보정과 4km 미리보기

사용자 근접 캡처의 사각 수면 가장자리와 일정한 강둑 단면을 보정했다. 강폭·하상 깊이·둑의 사면을 구간별로 바꾸고, 수면 삼각형은 지형과 수위가 만나는 위치에서 잘랐다. 기존 2km 씬의 지형에는 절대 높이 보정 패치 1건을 더했고 수면 메시 자산을 갱신했다. 2km Base RAW와 서쪽 수계 수정 전후 기록은 보존했다.

- [RiverBasin4km 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [4km 조감](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_Overview.png) · [숲 연못 합류부](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_ForestShore.png) · [남서 호수](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_4km_SouthwestInlet.png)
- 첫 4km 판은 2km 계획의 수평 좌표를 두 배로 옮기고 같은 고도를 사용했다. 중간 표본은 보간해 4,096m 정사각형에 4,097² 높이 표본과 512m Terrain 64장을 저장했다. 재개방 검사에서 콜라이더 참조와 경계 112곳이 일치했으며 Base RAW 대비 최대 높이 오차는 약 5.87mm다. [4km 원본 메타데이터](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Source/BaseHeight.json)
- 수면을 잘라 낸 뒤 물길 제어점 59곳이 수면 삼각형 안에 있는지 검사했다. [보정한 2km 수치 검사](../../Tools/RiverBasinWorld/Rebuild/shore-validation.json)는 중심선 표본 615개 수면 연결과 교량 예정 구간 외 동선 표본 609개 침수 0을 기록한다. 이 검사는 발행본의 플레이 검증을 대신하지 않는다.
- 첫 4km 판의 색과 물 재질은 검토용이다. 4km 기준에서 거점 사이 이동 시간, 넓어진 교량 횡단부와 세부 호안 아트, 실제 수영과 TPS 이동, Bake·스트리밍은 미수행이다.

### 남서 호수와 서쪽 숲 수계

사용자 표시를 따라 본류 중류에서 남서 호수로 이어지는 지류와 두 하구를 추가했다. 서쪽 숲에는 상류 개울과 연못을 두고, 연못 물길은 남서 호수로 연결했다. 기존 순환로 일부를 호안 북쪽으로 옮겼고, 새 지류 횡단 B3·B4의 상판 높이를 계획에 반영했다.

- [갱신한 수계 지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_WestWater_Relief.png) · [Unity 적용 화면](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_WestWater_Unity.png) · [변경본 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_2049_WestWater_R16LE_SouthFirst.raw)
- [같은 WorldBuilder 미리보기 씬](../../Assets/WorldBuilder/Worlds/f150b4516d3140f58fae94b43f64432c/Preview/WorldPreview.unity)에 RAW의 높이 변경 831,765개를 절대 높이 패치 1건으로 저장했다. Base RAW와 월드 ID를 보존했다. 4m 격자로 만든 검토용 수면 메시를 함께 저장했다. Editor에서 재개방해 지형 16장과 수면 자산을 읽었고 지형 경계 24곳의 높이 차 0m, 변경 RAW 대비 최대 오차 약 5.87mm를 확인했다.
- [수치 검사](../../Tools/RiverBasinWorld/Rebuild/west-water-validation.json)에서 남서 호수 중심 수심 9m·숲 연못 중심 수심 6m, 7개 물길 중심선 표본 615개의 수면 연결, 교량 예정 구간을 제외한 동선 표본 609개의 침수 0을 확인했다. 순환로 중심선 최대 기울기는 16.47°다.
- 정식 물리 수역과 수영, 교량 제작과 실제 주행은 남아 있다. 검토용 수면은 좁은 하천 가장자리에서 4m 격자가 보인다. 아래 기록은 서쪽 수계 변경 전 기준 지형의 제작·검증 이력이다.

2026-11-20 **독립 신작 HeightMap과 Unity 지형 블록아웃 생성 · Editor 재개방 및 수치 검사 통과.** v1.1 전달 HTML의 2km·1m 표본·512m 타일 계약과 지형/수면 분리 원칙을 반영했다. C 아틀라스의 5개 바이옴과 3개 거점이 맡는 공간 역할을 검토해 재구성했다. 새 지형의 수직 범위는 −32~352m로 정했다. C의 이미지와 RAW는 픽셀 정합된 지도가 아니며 700m 설정은 C의 원본 자산에 해당한다. 신규 원본은 이전 RiverBasin Terrain 및 C RAW를 입력으로 사용하지 않는다.

| 산출물 | 위치 |
|---|---|
| 신규 공간 설계 | [강 유역 월드 재설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md) |
| 저작 원본 | [지형·수계·동선·조망 계획](../../Tools/RiverBasinWorld/Rebuild/terrain-plan.json) · [높이맵 생성기](../../Tools/RiverBasinWorld/Rebuild/generate-heightmap.mjs) |
| 생성물 | [새 R16 RAW](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_2049_R16LE_SouthFirst.raw) · [수계·동선 지형도](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_Relief.png) · [수치 검사 결과](../../Tools/RiverBasinWorld/Rebuild/terrain-validation.json) |
| Unity 월드 | [지형 미리보기 씬](../../Assets/WorldBuilder/Worlds/f150b4516d3140f58fae94b43f64432c/Preview/WorldPreview.unity) · [Unity 조감 캡처](../../Tools/RiverBasinWorld/Rebuild/RiverBasin_Unity_Overview.png) · [WorldDefinition](../../Assets/10.Datas/WorldBuilder/f150b4516d3140f58fae94b43f64432c_Authoring.asset) |

- 2,048m 정사각형에 512m Terrain 16장을 배치했다. R16 little-endian 남쪽 첫 행, 2049² 표본, 실측 높이 −16.71~294.84m다. Node에서 같은 입력으로 다시 생성했을 때 RAW SHA-256 `bdb14bc21b18839ede1bfcf3dcc572f4cf48337efb379e3941421bcebb12f0f6`이 일치했다.
- Unity에서 저장 씬의 타일 16개·콜라이더와 도색 레이어를 확인했다. 경계 24곳의 높이 차 0m, RAW 대비 Terrain 높이 최대 오차 약 5.87mm다. 저작 정의의 P02 입력 검사도 통과했다. 물과 도로는 수계·동선 지형도에 표시하며 Unity 조감은 지형과 임시 바이옴 색을 렌더링한다.
- 동선 5개에서 교량 중심 반경 32m의 연결 예정 구간을 빼고 628개 중심선 구간을 검사했다. 계획 수위 기준 침수 0, 최대 기울기는 호수 순환 14.70°, 고산 접근 17.28°다. 교량 위치 2곳에서 하상과 예정 상판의 수위 여유를 확인했다. 상판과 양안 접근부는 아직 제작 전이다.
- 관측자 지면 높이 +2m에서 가상 성채 높이와 교량을 잇는 지형 차폐 검사 기준으로 마을→성채, 굽이→첫 교량, 교량 이후→성채 3개 후보는 열린다. 고산 유적→호수는 능선에 가린다. X 1520/Z 1770 후보는 호수 수면 중심으로 시선이 열리지만 접근로·TPS 검증은 남아 있다.
- 호수 마을에서 첫 교량을 지나 중앙 성채로 오르는 구간의 실제 주행, 교량 접속 경사와 조망, 급한 절개면의 아트 보정은 후속 블록아웃에서 확인한다. 물 메시·물리 수역, 도로·교량, 건물·식생, 콘텐츠, Bake·스트리밍은 미수행이다.

## WorldBuilder HeightMap · 2026-11-20

**기준 지형과 저작 정의 생성 완료 · Editor 재개방 검증 통과.** 독립 씬의 마지막 저장 지형을 이관했다. 초기 `Generated/*.f32`와 1cm 넘게 다른 타일 표본이 37,104개였고 최대 차이는 약 11.08m였다. 원본 씬과 TerrainData 9장의 전후 SHA-256이 모두 일치했다.

| 산출물 | 경로 |
|---|---|
| 설계 | [독립 정규 월드 제작 설계안](../../Assets/docs/design/world-art/open-world/REGULAR_WORLD_PRODUCTION_DESIGN.html) · [실제 제작 경로 입력](../../Tools/RiverBasinWorld/production-plan.json) |
| 교환용 HeightMap | [SouthFirst R16 RAW](../../Tools/RiverBasinWorld/WorldBuilder/RiverBasin_1537_R16LE_SouthFirst.raw) |
| WorldBuilder 원본 | [BaseHeight.bytes](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Source/BaseHeight.bytes) · [원본 규격과 SHA-256](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Source/BaseHeight.json) |
| 저작 에셋 | [WorldProfile](../../Assets/10.Datas/WorldBuilder/5f12bb1019204196a2324aaf28a2f9dc.asset) · [WorldDefinition](../../Assets/10.Datas/WorldBuilder/5f12bb1019204196a2324aaf28a2f9dc_Authoring.asset) |
| Unity 미리보기 | [WorldPreview.unity](../../Assets/WorldBuilder/Worlds/5f12bb1019204196a2324aaf28a2f9dc/Preview/WorldPreview.unity) · [Editor 조감 캡처](../../Tools/RiverBasinWorld/WorldBuilder/RiverBasin_Terrain_Overview.png) |

- 규격은 1,536m × 1,536m, 1m 간격, 1,537² 전역 표본, 512m Terrain 3×3, 타일당 513² 표본이다. RAW는 4,724,738바이트의 헤더 없는 unsigned 16-bit little-endian이며 첫 행은 남쪽이다. 원점 고도 −32m, 수직 범위 288m, 실제 표면 높이 약 −14.68~207.58m를 보존했다.
- 후속 배치에는 `X(새 월드) = X(기존) + 768`, `Z(새 월드) = Z(기존) + 768`, `Y(새 월드) = Y(기존)`를 사용한다. 예를 들어 기존 제작 계획의 숲 다리 B1 중심은 새 월드에서 `(1075.20, 77.00, 752.64)`다. 좌표 변환 후 실제 접근로와 착지를 다시 확인한다.
- Unity의 기존 RAW 코덱과 Terrain 미리보기 생성기를 사용했다. 방향 표본 검사, RAW SHA 확인과 재가져오기, 저작 정의 연결을 통과했다. 저장 씬을 재개방해 TerrainData 해상도·위치·콜라이더 참조, 공유 경계 12곳, 원본 대비 2,368,521개 타일 표본을 대조했다. 저장된 Terrain 높이와 경계 차이 0m, RAW 인코딩 최대 오차 약 2.20mm다. RAW와 BaseHeight의 SHA-256은 `01a533e4d6572ee3be42f4188ecff375e6241caec53f5111d4cad2274afd4d49`다.
- 조감에서는 기존 하상과 산체가 읽힌다. 직선적인 수로 굽이와 일부 급한 둑·절개면도 남아 있다. 다음 블록아웃은 호수 마을 입구에서 호안과 숲 다리를 지나 초원으로 돌아오는 대표 구간에서 하상·호안 단면, 양안 접근 경사, TPS에서 다리가 처음 보이는 위치를 검증한다. 높이 보정은 저작 정의의 명령과 패치에 남긴다.
- 수면, 도로, 교량, 거점, 식생과 콘텐츠는 후속 저작 범위다. 실제 TPS 이동·조망·미술 인수와 Bake·스트리밍은 이 단계에서 미수행이다.

## 반영된 에셋

| 구분 | 위치 |
|---|---|
| 씬 | `Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity` (MapID `RiverBasinWorld`) |
| 전용 데이터 | `Assets/10.Datas/World/RiverBasinWorld/` (VisualR1: `RiverBasinPipeline.asset`, `RiverBasinSky.mat`) |
| 하늘·날씨 | `Assets/10.Datas/World/Sky/Sky_RiverBasin.asset`, `Assets/10.Datas/World/LivingWorld/Environment/RiverBasin_Weather_v1.json` |
| 납품 자산 폴더 | `Assets/03.Prefabs/Environment/RiverBasinWorld/`, `Assets/08.Models/World/RiverBasinWorld/`, `Assets/04.Images/World/RiverBasinWorld/` |
| 저작 입력 | `Tools/RiverBasinWorld/` (`world-plan.json`, `production-plan.json`, `asset-sources.json`, `Generated/*.f32`, `Source/`, `VisualR1/`, `VisualR3/`) |

최종 입력 기준 경로 길이는 A=2.315km, B=1.598km, C=2.472km, 대표 구간 평면 354.8m다. 길이는 XZ 평면 합계이며 이동 시간으로 환산하지 않는다.

## 단계 기록

| 단계 | 내용 | 상태 |
|---|---|---|
| 설계 | 지역·경로·VP·POI·검증 계획 | 문서 완료. 실제 조망·초견 검증은 미수행 |
| 1차 제작 | 전용 Terrain 9장, 교량·사당·수목 군락, 지도 계약·재질·수면 굽이, 검병 1명·발견 상자 1개 | 씬 저장됨. 기록 스크립트는 재실행 금지 |
| VisualR1 | 파이프라인·하늘 재질 | 데이터 존재. 개별 판정 기록 없음 |
| VisualR3 (B1, 2026-11-09) | 지형·물 비주얼 개선, 폭포 립 연결 | 교훈은 [지형·물 교훈](../../Assets/docs/guide/TERRAIN_WATER_ART_LESSONS.md)에 정리. 사용자 지적 후 재수정 |
| P5 | 정규 등록, 지도 배경, 정상 맵 전환 | **미제작** |

## 검증 결과

| 확인 | 결과 | 근거 |
|---|---|---|
| 정적 지지면·통행 중심선·양방향 NavMesh | 통과 | Tools README (원본 보고서는 삭제됨) |
| 카메라 가림 이름 규칙 | 8/8 | 위와 동일 |
| 단독 Play 초기화·실제 카메라 렌더 | Ready | 위와 동일. 입력 기반 검증 아님 |
| 경고 | 미등록 서비스 2건, 무게 프로필 폴백 1건, Localization 오류 | 위키 기록 |
| 미술 | NO-SHIP | 위키 기록. 사용자 인수 대기 |

## 사용자 테스트 대기·미수행

실제 입력 이동, 전투, 카메라, 물가 복귀, 밤, 저장/재진입, 성능, 미술 품질. 위 표의 한 항목만으로 전체 통과를 판단하지 않는다.

## 차단·주의

- **근거 소실:** `output/RiverBasinWorld/`(`RESULT.html`, `TEST_SPEC.html`, `audit.json`, 캡처)는 2026-11-13 output 정리로 삭제됐다. Tools README의 해당 링크는 끊겨 있다. 위 검증 수치는 그 보고서를 현재 다시 확인한 값이 아니다.
- **재실행 금지:** `build-world.csx`, `revise-stage.csx`, `finish-*.csx`, `place-encounter.csx`는 기록용이다. 수동 최종 배치가 반영돼 있어 재실행해도 같은 씬이 나온다고 보장하지 않는다. 재생성이 필요하면 현재 씬·전용 데이터·meta·ManualGameplay ID부터 보호한다.
- **git ignore:** 환경 에셋은 git ignore 대상이라 저장소 diff만으로 변경·복구 여부를 판단하지 않는다.
- 기존 OpenWorld 생성기와 과거 일회성 스크립트는 실행하지 않는다.

## 다음 행동

1. 사용자가 미술 방향을 확정하고 사용자 테스트 명세를 새로 작성한다(삭제된 `TEST_SPEC.html` 대체).
2. `audit-world.csx`·`capture-world.csx`로 현재 씬을 다시 검사해 이 문서의 검증 표를 갱신한다.
3. 위 경고 3종과 Localization 오류를 원인 분류한다.
4. P5 정규 등록은 사용자 지시 후 착수한다.
