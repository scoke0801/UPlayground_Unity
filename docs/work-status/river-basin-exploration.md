# RiverBasin4km 호수·동부·설산 탐험

[월드와 아트 위키](../wiki/world-art.md) · [공간 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md#호수동부설산-탐험)

## 반영

- 서남 호수와 호수 마을 앞 중앙섬에 수변 자갈턱과 완만한 내부 능선을 만들었다. 두 중앙섬과 인접섬 네 곳을 넓혔다. 버드나무 군락, 석등, 돌등을 수영 중에도 방향을 찾는 기준으로 쓴다.
- 채석장에는 접근로에서 갈라지는 303m 작업 단구 순환로를 냈다. 채석면의 회색 암반과 운반 자재, 요새 뒤 돌산의 바위 능선, 사막의 모래골 142m를 각각 다른 실루엣으로 읽게 했다.
- 설산 고원에 기존 순환로에서 갈라져 돌아오는 141m 눈벽 허리길과 반대편 고개의 계곡 조망 가지길을 냈다. 산 아래 갈림길, 눈선, 고원의 분기와 귀환부에 돌무더기 표지를 뒀다.
- 기존 구매 및 프로젝트 프리팹 19종으로 환경 요소 58개와 일회 획득 아이템 다섯 개를 배치했다. 각 아이템은 기존 획득 데이터와 별도의 씬 GUID를 쓴다. 높이가 바뀐 수변의 기존 배치 271개는 원래 매입 깊이를 기준으로 재정렬했다. [장식·보상·지면 저작](../../Tools/RiverBasinWorld/Rebuild/exploration-dressing.cs.txt) · [계곡 전망 표지](../../Tools/RiverBasinWorld/Rebuild/exploration-alpine-viewpoint.cs.txt)
- 지형 7개 타일에 높이 표본 189,302개를 WorldBuilder Sculpt `c7875cc0079048aba82e42aba4ceb2c7`로 기록했다. 작업 전 씬, 월드 정의, 타일, 지도 원본은 `output/RiverBasinWorld/Exploration/before/`에 보관했다. [조형 원본](../../Tools/RiverBasinWorld/Rebuild/exploration-terrain.cs.txt) · [적용 기록](river-basin-exploration-evidence/terrain-apply.txt)
- 확대 뒤 붙거나 좁아진 해협 3곳을 다시 열고 가파른 상륙 단면을 조정했다. Sculpt `caf1d25ad7144752ad9ff41f0e2ff272`로 지형 6개 타일에 기록했다. 별도 적용 전 사본을 만든 뒤 수변 장식 38개를 지면에 맞춰 정렬했다. [수로 저작](../../Tools/RiverBasinWorld/Rebuild/exploration-lake-crossings.cs.txt) · [적용 기록](river-basin-exploration-evidence/lake-crossings-apply.txt)
- 동부와 설산의 암반 렌더러 64개에 지역 재질을 적용했다. 모래골은 기존 사암 재질을 공유하고 채석장·돌산·설산은 복제 재질 7종을 사용한다. 돌산 암편 지면에서는 눈·고운 모래 가중치를 보존했다. [암반 렌더러](../../Tools/RiverBasinWorld/Rebuild/exploration-rock-finish.cs.txt) · [돌산 지면](../../Tools/RiverBasinWorld/Rebuild/exploration-ridge-surface.cs.txt)
- 정북 지도 4096×4096을 다시 촬영했다. 기존 스프라이트 GUID와 월드 좌표 기준을 유지하고 채석 단구, 모래골, 설산의 실제 산길을 점선으로 표시했다. [전후 지도 비교](river-basin-exploration-evidence/map-before-after.jpg) · [지도 PNG](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png) · [중심선 CSV](../../Tools/RiverBasinWorld/Rebuild/exploration-map-trails.csv)

## 검증

- **지도 재촬영 · 2026-11-22 22:01.** 저장된 씬의 정북 지도를 다시 렌더링하고 게임이 참조하는 PNG를 교체했다. Unity 재임포트에서 4096×4096, BC7, 밉맵 13단계와 기존 스프라이트 GUID를 확인했다. 교체 전 PNG는 `output/RiverBasinWorld/Exploration/recapture-before-20261122-220117/`에 보관했다. [현재 지도](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png)

- **저장 지형.** 두 중앙섬의 수면보다 0.35m 높은 면을 2m 격자로 쟀다. 서남 호수는 11,672 → 37,252m², 동쪽 호수는 11,968 → 32,588m²였다. 채석장·설산·사막의 동선 표본 930개 중 최대 경사는 각각 41.2°, 37.7°, 9.3°다. 112개 Terrain 경계의 높이 차는 0m다. [저장 지형 결과](river-basin-exploration-evidence/terrain-verify.txt)
- **수영 단면.** 양쪽 호수와 남안 유출 하천의 11개 횡단 경로를 좌·중·우 33개 단면으로 검사했다. 전부 물과 마른 양안까지 연결됐다. 물가 최대 종경사는 36.6°이며, 표본 위 충돌물과 남안 육상 접근로 중심의 충돌물은 0개다. [수면·충돌 조회](river-basin-exploration-evidence/lake-crossings-verify.txt)
- **배치.** 발견 보상 다섯 곳에 아이템·획득 프로필·콜라이더·서로 다른 GUID가 연결됐다. 여섯 그룹 내 누락 스크립트는 0개였다. [배치 검사](river-basin-exploration-evidence/dressing-verify.txt)
- **조망.** 설산 동쪽 순환로에서 전망까지 이어지는 35m의 최대 경사는 25.6°다. 중심을 캡슐로 검사해 장애물 0개를 확인했고, 도착점에서 계곡 방향 시선이 열렸다. [시선과 경사](river-basin-exploration-evidence/alpine-viewpoint.txt) · [허리길](river-basin-exploration-evidence/alpine.jpg) · [계곡 전망](river-basin-exploration-evidence/alpine-outlook.jpg)
- **Editor Play.** 씬이 준비된 뒤 보상 다섯 곳이 바닥 아이템 서비스에 등록됐고 라온을 가까이 배치했을 때 모두 획득 가능 상태였다. 획득 입력과 재방문은 사용자 테스트 대기다. [등록과 근접 조회](river-basin-exploration-evidence/play-smoke.txt)
- **Unity 고정 카메라.** 서남 호수의 중앙섬과 여울섬이 수로로 분리된 것을 확인했다. 동부는 채석 단구의 회색 작업면, 열려 있는 모래골, 돌산 지면과 암편을 확인했다. [서남 호수](river-basin-exploration-evidence/west-lake.jpg) · [동쪽 호수](river-basin-exploration-evidence/east-lake.jpg) · [채석장](river-basin-exploration-evidence/quarry.jpg) · [돌산](river-basin-exploration-evidence/stone-ridge.jpg) · [모래골](river-basin-exploration-evidence/dunes.jpg)
- **표면 회귀.** 기존 사막과 설산의 공용 발자국 마스크를 대조했다. 사막 원본 마스크와 설산 지형 표본의 불일치는 각각 0개였고, 눈 지면 음향 지도 연결도 확인했다. [기존 설산 표면 검사](river-basin-exploration-evidence/snow-surface-verify.txt)
- **보존.** 높이·구멍·재질 저작 대상 밖에서 작업 전 변경이 없던 Terrain 21개는 자동 재직렬화분을 복구했다. 복구 후 실시간 Terrain과 WorldBuilder 저장 해시가 일치했다. 검토용 씬 복제본과 중복 이미지, 자동 재직렬화 임시 사본을 정리했다. 작업 전 사본 31.65MiB, 수로 보정 전 9.80MiB, 돌산 지면 변경 전 4.53MiB와 WorldBuilder 조형·세션 파일을 보관했다. [되돌린 에셋](river-basin-exploration-evidence/serialization-reverted.txt) · [실측 해시](river-basin-exploration-evidence/physical-hash.txt)

## 사용자 테스트 대기

`RiverBasin4km`을 Editor에서 Play한다. 최초 방문을 확인할 수 있는 분리된 저장 또는 새 게임 상태에서 시작한다. 지도 `M`과 Ctrl+좌클릭으로 각 입구 근처에 이동한 뒤, 평소 사용하는 키보드 또는 게임패드로 아래 구간을 탐색한다.

| ID | 확인할 경험 |
|---|---|
| RB-EXP-01 | 서남 호수 여울섬 X 1070 / Z 956에서 중앙섬으로 수영한다. 상륙 뒤 버드나무 숲과 능선의 보상을 찾고 남쪽 작은 섬으로 건넌다. 입수·상륙 끊김, 수중 장애물, 섬 안쪽에 이동할 이유가 보이지 않는 지점을 기록한다. |
| RB-EXP-02 | 요새 접근로 X 3140 / Z 2102에서 채석 단구를 돌아온다. 요새 뒤 돌산과 X 3724 / Z 1680 모래골에도 들러 어느 지점에서 각 장소의 성격과 갈림길이 읽혔는지 확인한다. 낙하로 길이 끊기거나 보상이 암반에 가리면 실패다. |
| RB-EXP-03 | 설산 X 3304 / Z 3624의 갈림길에서 눈벽 허리길을 지나 순환로에 복귀한다. X 3385 / Z 3670의 표지에서 서쪽 계곡 전망으로 간다. 산길 찾기, 접지, 카메라 시야와 눈 표면이 자연스러워야 한다. |
| RB-EXP-04 | 발견 보상을 하나 획득한 뒤 같은 저장으로 씬에 재진입한다. 획득 결과가 인벤토리에 남고 같은 보상이 중복 획득되지 않아야 한다. |

회신 형식은 `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면 또는 로그`다. 장거리 양방향 주행, 발견의 재미와 시각 인수, Player·Addressables 빌드와 정규 청크 재진입은 미수행이다.
