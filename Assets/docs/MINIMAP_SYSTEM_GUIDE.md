# 미니맵(Minimap) 시스템 가이드

## 개요

UPlayground의 미니맵 시스템은 촬영된 맵 이미지 위에 아이콘을 표시하는 **MapImage 전용** HUD입니다.  
**HUD 미니맵(`UI_Minimap`)** 과 **전체 맵 뷰(`UI_Map`)** 두 UI가 한 쌍으로 동작하며, 씬별 `SceneContext.MapID`를 기반으로 `MapConfigDatabaseSO`에서 설정을 자동으로 조회합니다.

### 핵심 특징

- **MapImage 전용**: IconOnly 모드 없음. 모든 씬은 반드시 맵 이미지 Config를 가집니다.
- **씬 자동 Config 조회**: `SceneContext.MapID` → `MapConfigDatabaseSO` → `MinimapIconConfigSO` 로 씬 전환마다 자동 로드됩니다.
- **HUD 미니맵 + 전체 맵 분리**: `UI_Minimap`은 항상 표시되는 소형 HUD, `UI_Map`은 M키로 토글하는 전체 맵 뷰입니다.
- **미니맵 오프셋 클램핑**: 플레이어가 맵 가장자리로 이동해도 마스크 영역이 항상 맵 이미지로 채워집니다.
- **확대 맵 전환**: `UI_Minimap.ToggleExpandedMap()`으로 미니맵을 확대 뷰로 부드럽게 전환합니다.
- **3-채널 아이콘 시스템**: 플레이어 방향 화살표 / 적 감지 상태 구분 / 퀘스트 마커를 독립 딕셔너리로 관리합니다.
- **GameObjectManager 이벤트 연동**: `OnActorRegistered` / `OnActorUnregistered`로 액터 스폰·사망 자동 반영
- **QuestManager 이벤트 연동**: `QuestAccepted` / `QuestCompleted` / `QuestFailed` 구독으로 퀘스트 마커 자동 갱신
- **에디터 캡처 도구**: `MinimapCaptureEditorWindow`로 씬 탑다운 촬영 → PNG 저장 → Config 자동 할당

---

## 아키텍처

```
┌─────────────────────────────────────────────────────────────────────┐
│  SceneContext.MapID ──► MapConfigDatabaseSO ──► MinimapIconConfigSO  │
│                                    ▲                                  │
│                         SceneManager.CurrentMapID                    │
└─────────────────────────────────────────────────────────────────────┘
          │                            │
          ▼                            ▼
  UI_Minimap (HUD)               UI_Map (전체 맵, M키 토글)
  ┌──────────────────────┐       ┌──────────────────────────┐
  │ _mapBackground       │       │ _mapBackground (전체 이미지) │
  │ _iconContainer       │       │ _iconContainer            │
  │ _questContainer      │       │ _questContainer           │
  │ _playerIcon          │       │ _playerIcon               │
  │  (항상 중심 고정)     │       │  (맵 상의 절대 위치)       │
  └──────────┬───────────┘       └──────────┬────────────────┘
             │                              │
             ▼                              ▼
      GameObjectManager              MapInputReceiver
      .OnActorRegistered             (드래그·스크롤 중계)
      .OnActorUnregistered
             │
      MinimapMarkerRegistry ◄── MinimapMarkerRegistrar (씬 배치)
             │
      QuestManager (EventManager)
```

### 클래스 의존 관계

```
데이터 계층 (Assets/02.Scripts/Data/UI/)
├── MapConfigDatabaseSO        — MapID → MinimapIconConfigSO 매핑 테이블 (씬당 1개)
└── MinimapIconConfigSO        — 아이콘·이미지·줌·확대 설정 SO

씬 컨텍스트 (Assets/02.Scripts/Manager/Scene/)
└── SceneContext                — MapID 필드 보유, SceneManager에 전달

매니저 (Assets/02.Scripts/Manager/)
└── SceneManager               — CurrentMapID 프로퍼티 노출

UI 계층 (Assets/02.Scripts/UI/HUD/)
├── UI_Minimap                 — HUD 미니맵 (UI_Base 상속)
├── UI_Map                     — 전체 맵 뷰 (UI_Base 상속, Popup 레이어)
├── MapInputReceiver           — UI_Map 드래그·스크롤 입력 중계
├── MinimapEntityIcon          — 개별 아이콘 컴포넌트 (팩토리 패턴)
├── MinimapMarkerRegistrar     — 씬 배치 위치 등록 컴포넌트
└── MinimapMarkerRegistry      — 정적 레지스트리 (static class)

에디터 도구 (Assets/02.Scripts/Tool/Editor/Minimap/)
└── MinimapCaptureEditorWindow — 탑다운 씬 캡처 & PNG 저장
```

---

## 파일 구조

```
Assets/
├── 02.Scripts/
│   ├── UI/
│   │   └── HUD/
│   │       ├── UI_Minimap.cs               — HUD 미니맵 메인 클래스
│   │       ├── UI_Map.cs                   — 전체 맵 뷰 클래스
│   │       ├── MapInputReceiver.cs         — 전체 맵 드래그·스크롤 입력 중계
│   │       ├── MinimapEntityIcon.cs        — 개별 아이콘 컴포넌트
│   │       ├── MinimapMarkerRegistrar.cs   — 씬 마커 등록 컴포넌트
│   │       └── MinimapMarkerRegistry.cs    — 마커 정적 레지스트리
│   │
│   ├── Data/
│   │   ├── UI/
│   │   │   ├── MapConfigDatabaseSO.cs      — MapID→Config 매핑 테이블 SO
│   │   │   └── MinimapIconConfigSO.cs      — 아이콘·이미지 설정 SO
│   │   └── Path/
│   │       └── UIKeyType.cs                — Minimap=17, Map=18 등록됨
│   │
│   ├── Manager/
│   │   ├── SceneManager.cs                 — CurrentMapID 프로퍼티
│   │   └── Scene/
│   │       └── SceneContext.cs             — MapID 필드
│   │
│   └── Tool/
│       └── Editor/
│           └── Minimap/
│               └── MinimapCaptureEditorWindow.cs
│
└── 10.Datas/
    ├── Minimap/                            — MinimapIconConfigSO 에셋 저장
    │   ├── InGame_MinimapConfig.asset
    │   └── CombatScene_MinimapConfig.asset
    └── UI/                                 — MapConfigDatabase.asset 저장 권장
```

---

## 핵심 클래스

### `MapConfigDatabaseSO`

씬별 MapID와 `MinimapIconConfigSO`를 연결하는 매핑 테이블입니다.  
프로젝트 전체에서 **하나만** 생성하고 `UI_Minimap`, `UI_Map` 인스펙터에 모두 할당합니다.

| 인스펙터 | 설명 |
|----------|------|
| `_entries` (List) | `mapId` (string) + `config` (MinimapIconConfigSO) 쌍의 목록 |

```csharp
// mapId에 해당하는 Config 반환, 없으면 null + LogWarning
MinimapIconConfigSO GetConfig(string mapId)
```

> **Create Asset**: `UPlayGround/UI/MapConfigDatabase`

---

### `SceneContext`

씬마다 배치하는 컴포넌트. `MapID` 필드를 Inspector에서 설정합니다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `SceneType` | `string` | 씬 타입 식별자 (기존) |
| `MapID` | `string` | `MapConfigDatabaseSO`의 `mapId`와 일치해야 함 |

---

### `SceneManager`

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `CurrentMapID` | `string` | SceneContext에서 받아 저장한 현재 씬의 MapID |

`OnSceneContextReady(SceneContext)` 호출 시 `CurrentMapID`가 먼저 저장되고, 이후 UI Show 체인이 실행됩니다. UI의 `OnShow()`에서 `SceneManager.Instance.CurrentMapID`를 참조하면 항상 최신 값을 가져올 수 있습니다.

---

### `UI_Minimap`

`UI_Base` 상속. `OnShow()`에서 `MapConfigDatabaseSO`로 Config를 조회하고, `LateUpdate`에서 매 프레임 갱신합니다.

**직렬화 필드**

| 필드 | 타입 | 설명 |
|------|------|------|
| `_iconContainer` | `RectTransform` | 적·NPC·채집 아이콘 부모 |
| `_questContainer` | `RectTransform` | 퀘스트 마커 부모 (`null`이면 `_iconContainer` 대체) |
| `_playerIcon` | `RectTransform` | 플레이어 방향 화살표 (항상 마스크 중심에 고정) |
| `_mapBackground` | `Image` | 맵 배경 이미지 |
| `_minimapMask` | `RectTransform` | 확대 전환 시 크기 변경 대상 |
| `_mapConfigDB` | `MapConfigDatabaseSO` | MapID→Config 조회 테이블 |
| `_maskDisplaySize` | `float` | 기본 마스크 픽셀 크기 (MinimapMask sizeDelta.x와 일치) |

**런타임 상태**

| 필드 | 설명 |
|------|------|
| `_config` | `OnShow()`에서 MapID로 조회한 현재 Config |
| `_currentMaskSize` | 현재 마스크 크기 (확대 전환 중 보간값) |
| `_currentMapZoom` | 현재 맵 줌 배율 (확대 전환 중 보간값) |

**주요 메서드**

```csharp
// M키 또는 외부 호출로 미니맵 ↔ 확대 뷰 전환
public void ToggleExpandedMap()
```

**오프셋 클램핑 (마스크 항상 채우기)**

```
maxOffset = maskSize × (zoom − 1) / 2
offset.x, offset.y ∈ [−maxOffset, +maxOffset]
```

이미지 실제 크기(`maskSize × zoom`)가 마스크(`maskSize`)보다 클 때만 이동이 허용됩니다.  
`zoom ≤ 1`이면 `maxOffset = 0`으로 고정됩니다.

---

### `UI_Map`

`UI_Base` 상속. M키로 열리는 전체 맵 팝업입니다. ESC 또는 닫기 버튼으로 닫힙니다.

**직렬화 필드**

| 필드 | 타입 | 설명 |
|------|------|------|
| `_mapViewport` | `RectTransform` | 입력 수신 영역 (MapInputReceiver 부착) |
| `_mapContainer` | `RectTransform` | 줌·패닝 대상 컨테이너 |
| `_mapBackground` | `Image` | 전체 맵 이미지 |
| `_iconContainer` | `RectTransform` | 적·NPC 마커 부모 |
| `_questContainer` | `RectTransform` | 퀘스트 마커 부모 |
| `_playerIcon` | `RectTransform` | 플레이어 위치 마커 (맵 상 절대 위치) |
| `_mapConfigDB` | `MapConfigDatabaseSO` | MapID→Config 조회 테이블 |
| `_mapDisplaySize` | `float` | 맵 이미지 픽셀 크기 전체 (기본 1000) |
| `_minZoom` / `_maxZoom` | `float` | 줌 범위 (기본 0.5~4) |
| `_zoomStep` | `float` | ±버튼 줌 단위 (기본 0.5) |
| `_scrollZoomSpeed` | `float` | 스크롤 줌 속도 (기본 0.1) |
| `_showAllEnemiesOnMap` | `bool` | Config의 `showEnemies`와 무관하게 전체 적 표시 |

**주요 동작**

- 열릴 때 현재 플레이어 위치를 중심으로 초기화 (`CenterOnPlayer()`)
- 마우스 드래그 패닝, 스크롤 줌 (마우스 포인터 기준), ±버튼 줌 (뷰 중심 기준)
- "나 찾기" 버튼(`_findMeButton`)으로 언제든 플레이어 위치로 재이동
- `ClampPan`으로 맵 이미지 경계 밖으로 패닝 불가

---

### `MapInputReceiver`

`UI_Map`의 `MapViewport`에 부착하는 경량 입력 중계 컴포넌트.  
`IBeginDragHandler`, `IDragHandler`, `IScrollHandler`를 구현하여 이벤트를 `UI_Map`에 전달합니다.

```csharp
// 이벤트 (UI_Map에서 구독)
event Action<PointerEventData> OnBeginDragEvent
event Action<PointerEventData> OnDragEvent
event Action<PointerEventData> OnScrollEvent
```

> `[RequireComponent(typeof(Graphic))]` — `MapViewport`에 `Image` 컴포넌트(alpha=0, raycastTarget=true)가 있어야 합니다.

---

### `MinimapIconConfigSO`

`Assets/10.Datas/Minimap/` 아래 씬별로 생성합니다. (`UPlayGround/UI/MinimapIconConfig`)

**아이콘 항목 (`IconEntry` struct)**

| 필드 | 타입 | 설명 |
|------|------|------|
| `sprite` | `Sprite` | 아이콘 스프라이트 |
| `color` | `Color` | 아이콘 색상 |
| `size` | `float` | 아이콘 픽셀 크기 (8~40) |

**아이콘 항목 목록**

| 항목 | 표시 대상 |
|------|-----------|
| `player` | 플레이어 방향 화살표 |
| `enemy` | 적 (비전투 상태) |
| `enemyDetected` | 적 (플레이어 감지·전투 상태) |
| `npc` | NPC |
| `gathering` | 채집 오브젝트 |
| `questTarget` | ReachLocation 목표 마커 |
| `questNpc` | ItemDeliver NPC 마커 |
| `customMarker` | MinimapMarkerType.Custom 마커 |

**표시 옵션**

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `showQuestMarkers` | `true` | 퀘스트 마커 표시 여부 |
| `showEnemies` | `true` | 적 아이콘 표시 여부 |
| `showOnlyDetectedEnemies` | `false` | `true` = 플레이어를 감지한 적만 표시 |
| `showNpcs` | `true` | NPC 아이콘 표시 여부 |
| `showGathering` | `true` | 채집 오브젝트 아이콘 표시 여부 |

**맵 이미지 설정**

| 필드 | 설명 |
|------|------|
| `backgroundSprite` | 캡처된 배경 스프라이트 (MinimapCaptureEditor가 자동 할당) |
| `captureCenter` | 캡처 당시 월드 XZ 중심 (`Vector2`) |
| `captureWorldSize` | 캡처 범위 (월드 유닛, 한 변 길이) |
| `mapZoom` | HUD 미니맵 줌 배율 (0.5~100, 기본 1) |

**확대 맵 설정**

| 필드 | 설명 |
|------|------|
| `expandedMapSize` | 확대 시 마스크 픽셀 크기 (100~800, 기본 500) |
| `expandedMapZoom` | 확대 시 맵 줌 배율 (0.5~100, 기본 3) |
| `expandTransitionDuration` | 전환 애니메이션 시간 초 (0~0.5, 기본 0.2) |

**좌표 변환 메서드**

```csharp
// 월드 XZ 좌표 → 미니맵 UI 픽셀 좌표
// nx = (worldPos.x - captureCenter.x) / captureWorldSize  → -0.5~0.5
// ny = (worldPos.z - captureCenter.y) / captureWorldSize
// return = new Vector2(nx * displaySize, ny * displaySize)
Vector2 WorldToMapImagePos(Vector3 worldPos, float minimapDisplaySize)
```

---

### `MinimapEntityIcon`

개별 아이콘 GameObject. `UI_Minimap` / `UI_Map`이 팩토리 메서드로 생성합니다.

```csharp
// 액터 추적 아이콘 생성 (actor.transform을 매 프레임 추적)
MinimapEntityIcon.Create(Transform parent, GameActor actor, MinimapIconConfigSO.IconEntry entry)

// 정적 마커 생성 (퀘스트 목표 등 위치 고정)
MinimapEntityIcon.CreateStatic(Transform parent, string label, MinimapIconConfigSO.IconEntry entry)

// 위치·가시성 갱신
void UpdateIcon(Vector2 minimapPos, bool isVisible)

// 런타임 색상 변경 (적 감지 상태 전환 등)
void SetColor(Color color)

// 스프라이트·색상·크기 일괄 변경
void SetEntry(MinimapIconConfigSO.IconEntry entry)
```

---

### `MinimapMarkerRegistrar` / `MinimapMarkerRegistry`

씬 오브젝트에 `MinimapMarkerRegistrar`를 부착해 퀘스트 목표 위치를 등록합니다.

| 직렬화 필드 | 설명 |
|-------------|------|
| `_locationId` | 퀘스트 목표와 매핑되는 문자열 ID |
| `_markerType` | `QuestTarget` (노란 "!") 또는 `Custom` |

`MinimapMarkerRegistry`는 정적 클래스로 씬 내 모든 등록된 마커를 관리합니다.

```csharp
static event Action<MinimapMarkerRegistrar> OnMarkerAdded
static event Action<MinimapMarkerRegistrar> OnMarkerRemoved
static bool TryGet(string locationId, out MinimapMarkerRegistrar registrar)
```

---

## 셋업 방법

### 1단계: MinimapIconConfigSO 생성 (씬별)

1. `Assets/10.Datas/Minimap/` 에서 우클릭 → `UPlayGround/UI/MinimapIconConfig`
2. 씬 이름을 반영한 이름으로 저장 (예: `InGame_MinimapConfig`, `CombatScene_MinimapConfig`)
3. Inspector에서 각 아이콘 항목별 **Sprite / Color / Size** 설정

### 2단계: 맵 이미지 캡처

1. `UPlayGround → Minimap → Minimap Capture Editor` 창 열기
2. 씬 뷰 Gizmo로 캡처 범위 확인 후 MinimapIconConfigSO를 연결
3. **캡처 & 저장** 클릭 → `backgroundSprite` / `captureCenter` / `captureWorldSize` 자동 할당

### 3단계: MapConfigDatabaseSO 생성 (프로젝트 전체에서 1개)

1. `Assets/10.Datas/UI/` 에서 우클릭 → `UPlayGround/UI/MapConfigDatabase`
2. `MapConfigDatabase.asset` 이름으로 저장
3. `Entries` 목록에 씬별 항목 추가:

| mapId | config |
|-------|--------|
| `"InGame"` | `InGame_MinimapConfig` |
| `"CombatTest"` | `CombatScene_MinimapConfig` |

> `mapId` 문자열은 다음 단계의 `SceneContext.MapID`와 정확히 일치해야 합니다.

### 4단계: SceneContext에 MapID 설정

각 씬의 `SceneContext` 컴포넌트 Inspector에서 `MapID` 필드를 입력합니다.

| 씬 | MapID |
|----|-------|
| InGame | `"InGame"` |
| CombatTest | `"CombatTest"` |

### 5단계: UI 프리팹 제작

#### UI_Minimap 프리팹 구조

```
UI_Minimap (GameObject)
  ├─ Canvas 컴포넌트 (CanvasLayer = HUD)
  ├─ UI_Minimap 컴포넌트
  │    ├─ _mapConfigDB  ← MapConfigDatabase.asset 할당
  │    └─ _maskDisplaySize ← MinimapMask sizeDelta.x 와 동일한 값
  └─ MinimapMask (Image — 원형 스프라이트 + Mask 컴포넌트)  ← _minimapMask
       ├─ MapBackground (Image)          ← _mapBackground
       ├─ QuestContainer (RectTransform) ← _questContainer
       ├─ IconContainer  (RectTransform) ← _iconContainer
       └─ PlayerIcon     (Image — 화살표 스프라이트) ← _playerIcon
```

> **MinimapMask 설정**: `Image Type = Simple`, 원형 Sprite 할당, `Mask` 컴포넌트 추가.

#### UI_Map 프리팹 구조

```
UI_Map (GameObject)
  ├─ Canvas 컴포넌트 (CanvasLayer = Popup)
  ├─ UI_Map 컴포넌트
  │    └─ _mapConfigDB  ← MapConfigDatabase.asset 할당
  ├─ MapViewport (RectTransform + Image(alpha=0, raycastTarget=true) + MapInputReceiver) ← _mapViewport
  │    └─ MapContainer (RectTransform)    ← _mapContainer
  │         ├─ MapBackground (Image)      ← _mapBackground
  │         ├─ QuestContainer (RectTransform) ← _questContainer
  │         ├─ IconContainer  (RectTransform) ← _iconContainer
  │         └─ PlayerIcon     (Image)     ← _playerIcon
  ├─ CloseButton   (Button)               ← _closeButton
  ├─ ZoomInButton  (Button)               ← _zoomInButton
  ├─ ZoomOutButton (Button)               ← _zoomOutButton
  └─ FindMeButton  (Button)               ← _findMeButton
```

### 6단계: UIPrefabDatabase 등록

기존 등록 방식과 동일하게 `Minimap` (키=17), `Map` (키=18) 키로 각 프리팹을 추가합니다.

### 7단계: 퀘스트 목표 마커 연결 (퀘스트가 있는 경우)

씬의 목표 오브젝트(트리거 존, NPC 등)에 `MinimapMarkerRegistrar` 컴포넌트를 추가합니다.

| 퀘스트 목표 타입 | LocationId 설정 방법 |
|-----------------|---------------------|
| `ReachLocation` | `QuestObjectiveData.targetStringId`와 동일하게 설정 |
| `ItemDeliver` (NPC 전달) | `"npc_{npcId}"` 형식 (예: npcId=101 → `"npc_101"`) |

---

## 사용 예시

### 인게임 미니맵 + 전체 맵 열기/닫기

```csharp
// UI_GamePlay.cs — OnShow / OnHide
protected override void OnShow()
{
    UIManager.Instance.ShowUI(UIKeyType.Minimap);
    // ...
}

protected override void OnHide()
{
    UIManager.Instance.HideUI(UIKeyType.Minimap);
    // ...
}

// M키 전체 맵 토글 (UI_GamePlay.Update에 구현됨)
private void ToggleMap()
{
    var map = UIManager.Instance.GetActiveUI(UIKeyType.Map.ToKey())?.GetComponent<UI_Map>();
    if (map != null && map.IsVisible)
        UIManager.Instance.HideUI(UIKeyType.Map);
    else
        UIManager.Instance.ShowUI(UIKeyType.Map);
}
```

### 미니맵 확대 전환 외부 호출

```csharp
var minimap = UIManager.Instance.GetActiveUI(UIKeyType.Minimap.ToKey())?.GetComponent<UI_Minimap>();
minimap?.ToggleExpandedMap();
```

### 퀘스트 목표 마커 씬 배치

```csharp
// 씬의 목표 지점 GameObject에 MinimapMarkerRegistrar 부착
// Inspector: LocationId = "dungeon_entrance"

// QuestSO의 objectives 중 ReachLocation 목표:
//   targetStringId = "dungeon_entrance"
// → 퀘스트 수락 시 미니맵·전체 맵에 자동으로 "!" 마커 표시
```

### 월드 → 미니맵 좌표 변환

```csharp
// WorldToMapImagePos 내부 동작
float nx = (worldPos.x - captureCenter.x) / captureWorldSize;  // -0.5 ~ 0.5
float ny = (worldPos.z - captureCenter.y) / captureWorldSize;
Vector2 pixelPos = new Vector2(nx * maskSize, ny * maskSize);

// UI_Minimap: 컨테이너 내 아이콘 최종 위치 (줌 적용)
Vector2 iconPos = pixelPos * currentMapZoom;

// 컨테이너 offset (플레이어가 항상 중심)
Vector2 offset = -playerPixelPos * currentMapZoom;
// → 오프셋 클램핑으로 마스크 경계 밖으로 나가지 않음
```

### 새로운 씬에 미니맵 추가 요약

```
1. MinimapCaptureEditor로 씬 이미지 캡처 → MinimapIconConfigSO 생성
2. MapConfigDatabase.asset의 Entries에 mapId + config 추가
3. 씬의 SceneContext.MapID = 추가한 mapId
4. 끝. UI_Minimap / UI_Map은 씬 전환 시 자동으로 Config를 조회
```

---

## 에디터 도구 — Minimap Capture Editor

메뉴: `UPlayGround → Minimap → Minimap Capture Editor`

### 탭 구성

| 탭 | 내용 |
|----|------|
| **캡처** | 캡처 영역·카메라·해상도·저장 경로·자동 할당 설정 및 미리보기 |
| **설정** | 캡처 카메라 오브젝트 확인 및 기본값 초기화 |
| **도움말** | 사용 순서 및 주의사항 안내 |

### 캡처 탭 기능 상세

| 섹션 | 주요 기능 |
|------|-----------|
| **캡처 영역** | 월드 중심(Vector3), 캡처 크기(월드 유닛), 씬 뷰 중심 자동 설정 버튼 |
| **씬 뷰 Gizmo** | 초록 사각형으로 캡처 범위 시각화, 핸들 드래그로 중심 이동 |
| **카메라** | 높이(Y), 배경색/투명 배경, 레이어 마스크, Near/Far Clip |
| **해상도** | 256 / 512 / 1024 / 2048 / 4096 프리셋 버튼 |
| **저장 경로** | 폴더 선택 다이얼로그, 파일명 설정 |
| **자동 할당** | 저장 후 MinimapIconConfigSO에 Sprite·캡처 범위 자동 입력 |
| **미리보기** | 저장 전 결과 확인 (투명 배경 시 체크보드 패턴 표시) |

### 저장 시 자동 처리

1. PNG / JPG 텍스처를 **Sprite** 타입으로 임포트 설정 (`mipmapEnabled = false`, `SpriteMeshType.FullRect`)
2. `MinimapIconConfigSO.backgroundSprite` 할당
3. `captureCenter`, `captureWorldSize` 기록

---

## 주의 사항

### MapID 불일치
`SceneContext.MapID`, `MapConfigDatabaseSO`의 `mapId`는 **대소문자까지 정확히 일치**해야 합니다.  
불일치 시 `OnShow()`에서 오류 로그가 출력되고 미니맵이 표시되지 않습니다.

### _maskDisplaySize와 MinimapMask sizeDelta 동기화
`UI_Minimap._maskDisplaySize`는 `MinimapMask RectTransform.sizeDelta.x`와 반드시 동일해야 합니다.  
불일치 시 아이콘이 마스크 경계 밖으로 나갑니다.

### mapZoom ≤ 1일 때 오프셋 클램핑
`mapZoom ≤ 1`이면 `maxOffset = 0`으로 계산되어 맵 이미지가 항상 중앙에 고정됩니다.  
플레이어 위치 추적이 동작하지 않으므로 `mapZoom`은 **1 이상**을 권장합니다.

### 아이콘 스프라이트 미설정
`IconEntry.sprite`가 `null`인 타입은 아이콘이 **생성되지 않습니다**. 모든 항목에 스프라이트를 할당하세요.

### 퀘스트 마커가 표시되지 않는 경우
- `MinimapMarkerRegistrar.LocationId`와 `QuestObjectiveData.targetStringId` (또는 `"npc_{npcId}"`)가 정확히 일치해야 합니다.
- `QuestManager.IsDBLoaded`가 `false`인 상태(로딩 중)에서는 `RefreshAllQuestMarkers()`가 빈 목록을 반환합니다. DB 로드 완료 후 퀘스트 수락 시에는 정상 동작합니다.

### 적 감지 상태 아이콘
- `MonsterActor.Detection`이 없는 몬스터는 항상 `enemy`(비전투) 색상으로 표시됩니다.
- `showOnlyDetectedEnemies = true` 설정 시, 플레이어를 감지하지 않은 몬스터는 미니맵에서 숨겨집니다.

### 맵 이미지 캡처 재촬영 기준
- 레벨 지형이 크게 변경된 경우 재촬영이 필요합니다.
- `captureWorldSize`가 실제 맵보다 작으면 범위 밖 아이콘 좌표가 마스크를 벗어날 수 있습니다.

### UI_Map MapInputReceiver
`MapViewport`에 **반드시 Image 컴포넌트**(alpha=0, raycastTarget=true)와 `MapInputReceiver` 컴포넌트가 모두 있어야 드래그·스크롤 입력이 동작합니다.

---

## 확장 포인트

### 새로운 아이콘 타입 추가

`MinimapIconConfigSO`에 `IconEntry` 필드를 추가하고 `GetActorIconEntry()`에 분기를 추가합니다.

```csharp
// MinimapIconConfigSO.cs
public IconEntry boss;

public IconEntry GetActorIconEntry(ActorType actorType)
{
    if ((actorType & ActorType.Player)  != 0) return player;
    if ((actorType & ActorType.Monster) != 0) return enemy; // 보스 구분 시 MonsterActor 직접 참조
    if ((actorType & ActorType.NPC)     != 0) return npc;
    return gathering;
}
```

### 새로운 퀘스트 목표 타입 마커 지원

`UI_Minimap.ResolveQuestLocationId()` (및 `UI_Map`의 동명 메서드)에 타입 분기를 추가합니다.

```csharp
private static string ResolveQuestLocationId(QuestObjectiveData obj) => obj.type switch
{
    QuestObjectiveType.ReachLocation => obj.targetStringId,
    QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
    QuestObjectiveType.MonsterKill   => $"monster_{obj.targetId}", // 확장 예시
    _                               => null,
};
```

### 커스텀 마커 프로그래밍 등록

`MinimapMarkerRegistrar`를 컴포넌트로 부착하지 않고 코드로 직접 등록하려면, 별도 `RuntimeMinimapMarker` 래퍼 클래스를 만들어 `MinimapMarkerRegistry.Register()`를 호출합니다.

### 아이콘 애니메이션 효과

`MinimapEntityIcon`에 코루틴 기반 펄스 메서드를 추가하거나, `UpdateIcon()` 호출 전에 `SetColor()`로 매 프레임 색상을 변경해 깜빡임 효과를 구현할 수 있습니다.
