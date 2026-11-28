# RiverBasin4km 사암골과 옛 취수장

2026-11-27 기준 | 초도 배치는 사용자 조감에서 블록아웃 수준으로 판정됐다. 남동쪽 사구와 취수장 주변의 사암 단애 재작업을 프리뷰에 저장했다. 최종 미술과 실제 TPS 조작은 사용자 테스트 대기다.

## 조감 지적과 수정 방향

사용자 조감에서 비슷한 길이의 볼록한 모래 둔덕과 그 위에 얹힌 암반이 반복됐다. 취수장도 절벽·골짜기에서 떨어져 보였다. 이전 단계의 보행·참조 검사는 초도 배치의 기술 근거로 보존하고 미술 판정은 실패로 바꾼다. 동쪽에는 사구 뒤에서 취수장 배후까지 한 덩어리로 이어지는 사암 단애를 만들고, 시장 쪽에는 낮은 암벽을 둬 높이 차이를 만든다. [초도 조감](river-basin-desert-waterworks-evidence/escarpment-region-before.jpg)

## 사암 단애 재작업

- 높이가 비슷한 모래 둔덕 여섯 곳을 걷어내고, 원래 사구를 되살렸다. 동쪽 해안의 사암 단애는 최대 33m까지 올라가고 취수장 남쪽으로 굽어 들어간다. 시장 옆 낮은 암벽은 출발부를 가린다. 두 암벽의 표면 이음과 절벽 뒤 모래 접합을 조정했다. [같은 구도의 재작업 조감](river-basin-desert-waterworks-evidence/escarpment-region-after.jpg) · [암벽 에셋](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinDesertEscarpment.asset)
- 기존 취수장 옆에서 절벽 기슭으로 이어지는 붕괴한 수문 제방을 저작했다. 기존 암반 프리팹을 기슭의 큰 낙석과 작은 파편으로 묶어 배치했다. [취수장과 제방](river-basin-desert-waterworks-evidence/escarpment-site-after.jpg) · [취수장 접근](river-basin-desert-waterworks-evidence/escarpment-approach-after.jpg) · [절벽 발치](river-basin-desert-waterworks-evidence/escarpment-cliff-foot-after.jpg)
- 최종 Editor 검사에서 두 길의 캡슐 1,233개에 장애물 겹침 0건, 임시 NavMesh 8/8, R4/R5 876개 정점의 지반 높이 변화 0m 및 타일 높이·도색 경계차 0을 확인했다. 제방은 기존 카메라 Solid 규칙과 MeshCollider에 연결했다. 실제 플레이와 광역 사막의 최종 미술 인수는 사용자 테스트 대기다. [재작업 Editor 검사](river-basin-desert-waterworks-evidence/escarpment-editor-check.txt)

## 초도 배치 기록

## 설계와 조사

기존 프리뷰에는 우물 교역소, 호숫가 정착지, 해안 전망대, 동쪽 하역 마당과 사구가 저장돼 있었다. 하역 마당의 남쪽에서 사구 사이 약 400m 구간에 탐색 목적지가 드물었다. HoYoverse의 수메르 자료에서 모래 평원과 암반, 오래된 시설을 번갈아 보여 주는 구성을 참고했다. [설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_DESERT_SETTLEMENT_EXPANSION_DESIGN.md#사구-너머의-옛-취수장) · [기존 조감](river-basin-desert-waterworks-evidence/aerial-before.jpg) · [HoYoverse 수메르 소개](https://genshin.hoyoverse.com/en/map)

[환경 에셋 카탈로그](../asset-catalog/index.html)와 열린 프리뷰를 대조했다. EastLands 절벽·바위와 사막 재질 변형, 기존 사막 야자, 사암·모래 TerrainLayer를 재사용 대상으로 골랐다. 범용 던전 아치와 폐허 프리팹은 있었고, 통과 가능한 중앙 개구부와 비대칭 바람탑을 한 덩어리로 읽을 수 있는 취수장 모델은 조사 범위에서 찾지 못했다. 이 모델을 Astra와 Blender MCP로 저작하고 ImageGen 층리 알베도를 연결한다.

## 씬 반영

- 하역 마당 동쪽에서 사암층과 돌 표식을 지나 취수장에 도착하는 길을 배치했다. 사구 발치로 돌아오는 옆길은 취수장 앞에서 본길과 합류한다. 사막 타일의 높이와 도색은 새 TerrainData에 저장했다. [프리뷰 씬](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Preview/RiverBasin4km.unity) · [TerrainData](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/Terrain/Terrain_7_2_DesertWaterworks.asset)
- 옛 취수장 앞에 물받이와 끊어진 수로, 야자 여섯 그루를 배치했다. 중앙 개구부와 앞뒤 돌 포장을 통해 시설을 가로지를 수 있다. 새 사암층에는 기존 절벽·바위 46개 인스턴스와 복제한 사암 재질을 사용했다. [재질과 메시](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Production/RegionArt/RiverBasinDesertWaterworks.asset)
- Astra로 저작한 원본에는 ImageGen 알베도가 내장돼 있다. Unity에서는 같은 알베도를 주 석재와 풍화 석재 슬롯에 연결했다. 폭 28.90m, 깊이 14.06m, 지상 높이 17.02m, 중앙 통과 폭 6.30m, 삼각형 6,812개다. [Blender 원본](../../Tools/RiverBasinWorld/DesertWaterworks/DesertWindWaterworks.blend) · [Blender 렌더](../../Tools/RiverBasinWorld/DesertWaterworks/DesertWindWaterworks.png) · [FBX](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/DesertWaterworks/SM_DesertWindWaterworks.fbx) · [ImageGen 텍스처](../../Assets/WorldBuilder/Worlds/6effd90b68ea4b3d8f8caa5197fcfe34/Authoring/DesertWaterworks/Waterworks_Sandstone.png)

## 화면과 검증

- [조감 전](river-basin-desert-waterworks-evidence/aerial-before.jpg) · [조감 후](river-basin-desert-waterworks-evidence/aerial-after.jpg) · [하역 마당에서 본 사구](river-basin-desert-waterworks-evidence/route-after.jpg) · [시장 출발 전](river-basin-desert-waterworks-evidence/market-exit-before.jpg) · [시장 출발 후](river-basin-desert-waterworks-evidence/market-exit-after.jpg) · [사구 옆길](river-basin-desert-waterworks-evidence/dune-path-after.jpg)
- [취수장 터 이전](river-basin-desert-waterworks-evidence/waterworks-before.jpg) · [취수장 접근](river-basin-desert-waterworks-evidence/waterworks-after.jpg) · [물받이](river-basin-desert-waterworks-evidence/catchment-after.jpg) · [중앙 통로](river-basin-desert-waterworks-evidence/passage-after.jpg)
- 저장한 프리뷰를 다시 열고 검사했다. 본길 464.2m, 사구 옆길 259.3m의 최대 표본 경사는 각각 29.0°, 23.5°다. 양쪽 폭을 포함한 캡슐 겹침 0/1,233, 중앙 통로 양방향을 포함한 임시 NavMesh 8/8을 확인했다. 출구의 건물 기단과 길 표식이 보행 표본에 걸려 동선을 조정한 뒤 재검사했다. [Editor 검사](river-basin-desert-waterworks-evidence/editor-check.txt)
- 타일 네 면의 높이·도색 추가 경계차 0, 기존 R4/R5의 876개 정점 지반 높이차 0m. 모래 셰이더와 기존 발소리 연결을 확인했다. 지붕 레이, 카메라 실내 이름 규칙, 누락 재질·스크립트 검사 통과. 기존 카메라 이름 규칙 EditMode 8/8 통과.
- 실제 TPS 조작과 최종 미술 인수는 사용자 테스트 대기. Player·Addressables 빌드는 미수행.

| 항목 | 확인 절차와 기대 결과 |
|---|---|
| DW-01 길 찾기 | RiverBasin4km 편집 프리뷰를 기준으로, 평소 사용하는 라온과 입력 장치로 하역 마당 X3701.5/Z1523에서 시작한다. 마커 없이 남쪽 길을 따라가고 갈림 지점 X3716/Z1373에서 사구 발치 길로 들어간다. 돌 표식과 바람탑을 이용해 취수장에 도착할 수 있다. 두 길을 왕복하는 동안 충돌로 막히거나 발이 턱에 걸리는 구간이 없어야 한다. |
| DW-02 통로와 카메라 | 취수장 X3830/Z1124의 중앙 아치를 양방향으로 통과한다. 기둥과 천장 쪽으로 카메라를 밀어 본다. 화면이 석재 안으로 들어가거나 시야를 잃는 구간이 없어야 한다. |
| DW-03 재질과 발자국 | 모래길에서 걸음과 달리기를 확인하고 물받이 옆에서 수면과 석재 접합을 본다. 모래의 발자국과 발소리가 동작하고 물받이 주변에 뜬 석재나 끊긴 수면이 보이지 않아야 한다. |

실패를 재현한 경우 항목 ID, 실제 결과, 입력 장치, 위치와 화면을 기록한다.

## 제작 도구 기록

Blender 5.2.2에서 MCP로 분리한 씬을 저장하던 중 내부 오류로 열린 Blender가 종료됐다. 기존 파일에 덮어쓰기는 없었다. 직전 자동 저장본을 22,958,807바이트의 [복구용 사본](../../output/harness/20261127-192322-db561390/Blender-36256-autosave.blend)으로 보존했다. 이 파일의 복구는 미검증이다. 동일 모델의 최종 원본과 FBX는 독립 백그라운드 Blender로 저장했다.
