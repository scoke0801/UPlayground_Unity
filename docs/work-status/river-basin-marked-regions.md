# RiverBasin4km 첨부 지도 범위 제작

[월드와 아트 위키](../wiki/world-art.md) · [4km 공간 설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_WORLD_REBUILD_DESIGN.md)

## 지형 반영

첨부 지도에서 정북과 4096m 좌표를 맞췄다. 원으로 표시한 네 곳에 섬을 추가했다. WorldBuilder Sculpt `e42069872cd14e7a94bbd4f77a8af898`과 Terrain 7개에 높이를 저장했다.

| 섬 | 중심 X/Z | 육지 면적 |
|---|---|---:|
| 숲 연못 | 631 / 2173 | 11,740m² |
| 서남 호수 서쪽 | 824 / 790 | 25,672m² |
| 서남 호수 동쪽 | 1420 / 853 | 15,400m² |
| 동쪽 호수의 긴 섬 | 3054 / 1080 | 13,720m² |

[조형 원본](../../Tools/RiverBasinWorld/Rebuild/marked-islands.cs.txt) · [면적과 수로 검사](river-basin-marked-regions-evidence/island-sculpt.txt)

## 지면과 배치

- 사막은 기존 모래분지에서 채석장 남쪽과 동쪽 호수 동안 쪽으로 확장했다. 고운 모래 가중치 50% 이상인 면적은 0.40 → 1.20㎢, 모래와 노출 사암이 이어지는 사막 핵심 권역은 1.26㎢다.
- 북쪽 산줄기 전역에 고도와 경사에 따라 적설을 확장했다. 눈 가중치 50% 이상인 면적은 0.25 → 1.60㎢다. Terrain 33개의 지면을 저작하고 수신 타일 28개의 발소리 지도와 1017×890 R8 발자국 마스크를 연결했다. [지면 저작](../../Tools/RiverBasinWorld/Rebuild/marked-biomes.cs.txt) · [적용 수치](river-basin-marked-regions-evidence/biomes.txt)
- 구매 및 프로젝트 프리팹 17종을 재사용해 섬 네 곳에 수목·바위·석등 등 장식 37개와 발견 보상 4곳을 배치했다. 숲 연못에는 기존 수면 메시의 수영 조회를 연결했다. [섬 배치](../../Tools/RiverBasinWorld/Rebuild/marked-island-dressing.cs.txt)

## 검증

- 저장 지형에서 섬마다 48방향에 수심 1.2m 이상의 물길을 확인했다. 물가의 최대 종경사는 37.3°이며, 네 섬의 수면과 지상 높이를 각각 다시 조회했다. [수면·배치 검사](river-basin-marked-regions-evidence/island-dressing.txt)
- 수신 타일 22개의 공통 경계 표본 2,098개를 맞췄다. 다시 계산한 발자국 마스크와 Terrain의 수치가 내부·공유 경계 모두 일치하며 발소리 지도 불일치도 0이다. 자동 재직렬화만 일어난 Terrain 4개는 작업 전 파일로 복구한 뒤 WorldBuilder 조형 해시를 대조했다. [경계 저작](../../Tools/RiverBasinWorld/Rebuild/marked-region-edges.cs.txt) · [저장본 검사](river-basin-marked-regions-evidence/biomes-verify.txt)
- Unity에서 4096×4096 정북 지도를 다시 촬영했고 기존 스프라이트 GUID, 좌표계와 BC7 임포트를 확인했다. 오브젝트 크기를 조정한 최종 씬의 지역 구도를 확인했다. [지도 전후](river-basin-marked-regions-evidence/map-before-after.jpg) · [현재 지도](../../Assets/10.Datas/Map/RiverBasin4km/Minimap.png) · [숲 연못](river-basin-marked-regions-evidence/forest-island.jpg) · [서남 호수](river-basin-marked-regions-evidence/west-islands.jpg) · [동쪽 호수](river-basin-marked-regions-evidence/east-island.jpg) · [사막](river-basin-marked-regions-evidence/desert-extent.jpg) · [설산](river-basin-marked-regions-evidence/snow-extent.jpg)
- Editor Play에서 현재 지도 참조, 발자국 수신 타일 28개 초기화, 숲 연못 수면 조회, 발견 보상 등록과 근접 획득 가능 상태 4/4를 확인했다. [Play 확인](river-basin-marked-regions-evidence/play-smoke.txt)

## 사용자 테스트 대기

| ID | 절차와 기대 결과 |
|---|---|
| RB-MARK-01 | `RiverBasin4km` Play에서 지도 `M`을 열고 표시한 사막·설산·섬의 모양을 비교한다. 동쪽 호수 동안·남동부와 북쪽 능선의 눈선이 현장에서 읽히는지 확인한다. |
| RB-MARK-02 | 네 섬에 호안에서 접근해 수영으로 입수·상륙·귀환한다. 숲 연못에서도 수영 상태로 이어지고 나무·바위가 상륙을 가로막지 않아야 한다. |
| RB-MARK-03 | 새 방문 상태에서 보상을 한 곳 획득하고 같은 저장으로 재방문한다. 별도로 확장한 모래와 눈 경계에서 걸으며 발소리·발자국 전환을 확인한다. |

회신은 `항목 ID / 통과·실패·미수행 / 실제 결과 / 화면 또는 로그`로 기록한다. 양방향 주행과 수영, 획득 입력·재방문, Player·Addressables 빌드는 미수행이다.

작업 전 지형·지도 사본 68.73MiB, 지면 변경 전 48.81MiB, 경계 보정 전 24.04MiB, 배치 전 씬 4.13MiB와 조형 세션을 `output/RiverBasinWorld/MarkedRegions/` 및 WorldBuilder 저작 경로에 보관했다. 동일한 Terrain 임시 사본과 검토용 합성 이미지는 정리했다.
