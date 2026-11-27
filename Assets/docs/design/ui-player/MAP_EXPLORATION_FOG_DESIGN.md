# 지도 탐험 안개 설계

상태·검증·사용자 테스트: [map-exploration-fog.md](../../../../docs/work-status/map-exploration-fog.md)

## 플레이 경험

- 플레이어가 지나간 주변만 지도가 드러나고, 나머지는 구름 안개로 가린다. 빈 곳이 "아직 안 가 본 곳"으로 읽혀 탐험 동기가 된다.
- 안개가 길찾기를 막지 않는다. **퀘스트 마커와 사용자 마커는 안개 위에도 보인다.**
- 가 봐야 알 수 있는 것은 숨긴다. 마을·포탈·고정 NPC·커스텀·NPC·채집·적 아이콘은 탐험한 곳에서만 보인다. **안개 속 포탈은 지도에서 활성 상태가 될 수 없다**(보이지 않고 클릭 이동도 안 된다). 월드의 활성화 플래그는 바꾸지 않는다.
- 순간 이동은 도착 지점 주변만 밝힌다. 지나가지 않은 경로는 드러내지 않는다.
- 대상은 RiverBasin4km 오픈월드의 전체 지도 한 장이다. HUD 미니맵과 지역 목록 미리보기는 레거시라 설계 대상이 아니다.

## 구조

| 역할 | 위치 |
|---|---|
| 탐험 격자(기록·저장 변환) | `Data/UI/MapFog/ExplorationGrid` |
| 격자 → 마스크 텍스처 | `Data/UI/MapFog/MapFogLayer` |
| 공통 설정 | `Data/UI/MapFog/MapFogSettingsSO` (`Assets/10.Datas/Map/MapFogSettings.asset`) |
| 기록·저장·마스크 공급 | `Manager/World/MapExplorationManager` |
| UI 계약 | `IUIMapExplorationService` / `UISvc.MapExploration` |
| 그리기 | `UI/Common/UIMapFogOverlay`, `06.Shaders/UIMapFog.shader`, `10.Datas/Map/MapFog.mat` |

- **기록은 매니저가 맡는다.** 전체 지도는 잠깐 열리므로 UI가 기록하면 빠진다.
- **아이콘 판정**은 `UIMapFogOverlay.IsRevealed`가 그리는 안개와 같은 격자·좌표로 한다. 안개를 그리지 않는 지도에서는 모두 표시한다.
- **격자는 월드 XZ 원점에 고정한다.** 지도 이미지 좌표에 저장하지 않으므로 미니맵을 다시 촬영해도 기록이 어긋나지 않는다.
- **저장 키**는 지역 지도면 `mapId`(원본 지역 좌표), 연속 월드 공통 지도면 `world:{씬 MapID}`(월드 좌표)다. 도보로 논리 지역이 바뀌어도 월드 지도의 기록은 나뉘지 않는다.
- **그리기**는 지도 배경의 자식 `RawImage` 하나다. 줌·패닝을 상속하고 아이콘 컨테이너 아래에 놓인다. 런타임에 붙이므로 지도 프리팹은 바꾸지 않는다.
- **세이브 3.7** `mapExploration`: 지도별 격자 칸 크기와 32×32 청크 비트마스크(Base64). 저장된 격자는 만들 때의 칸 크기를 유지한다.

## 저작 값

| 값 | 위치 | 기본 |
|---|---|---|
| 지도별 안개 사용 | `MinimapIconConfigSO.useExplorationFog` | 켬 |
| 지도별 밝힘 반경 | `MinimapIconConfigSO.fogRevealRadius` | 0(자동) |
| 자동 반경 | `MapFogSettings` | 지도 긴 변의 3%, 18~90m |
| 격자 칸 | `MapFogSettings` | 긴 변을 최대 384칸, 최소 2m |
| 안개 색·불투명도·구름·경계 | `MapFog.mat` | 청회색, 0.97 |

- `SceneContext.ShowExplorationHud`가 꺼진 씬(훈련장 등)은 기록하지도 가리지도 않는다.
- `MapConfigDatabase`의 안개 설정 참조를 비우면 기능 전체가 꺼진다.
- Editor·Development 빌드의 치트 패널(지역 이동 탭)에서 현재 지도의 촬영 범위를 한 번에 밝힐 수 있다. 실제 탐험 기록을 채우므로 저장에 남는다.
- 안개를 빌드에 포함하는 경로는 `MapConfigDatabase → MapFogSettings → MapFog.mat → 셰이더` 참조다.

## 후속 후보 (미반영)

- 전망 지점·지역 해금으로 넓은 구역을 한 번에 밝히기(`ExplorationGrid.RevealCircle` 재사용).
- 새로 밝혀질 때의 안개 걷힘 연출, 지역별 탐험률 표시.
