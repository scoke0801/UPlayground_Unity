# 미니맵(Minimap) 시스템 가이드

## 개요

UPlayground의 미니맵 시스템은 **아이콘 기반 HUD 미니맵**으로, 플레이어·적·퀘스트 목표를 UI Canvas 위의 아이콘으로 표시합니다.

### 핵심 특징

- **두 가지 표시 모드**: 플레이어 중심 아이콘 전용 모드(IconOnly)와 촬영된 배경 이미지 위에 아이콘을 표시하는 맵 이미지 모드(MapImage)
- **3-채널 표시 시스템**: 플레이어 방향 화살표 / 적 감지 상태 구분 아이콘 / 퀘스트 목표 마커를 독립적으로 관리
- **GameObjectManager 이벤트 연동**: `OnActorRegistered` / `OnActorUnregistered` 이벤트로 액터 스폰·사망을 자동 반영
- **QuestManager 이벤트 연동**: `QuestEvent.QuestAccepted` / `QuestCompleted` / `QuestFailed` 구독으로 퀘스트 상태 변경 시 마커 자동 갱신
- **MinimapMarkerRegistrar**: 씬 오브젝트에 부착해 퀘스트 목표 위치를 LocationId로 등록하는 경량 컴포넌트
- **에디터 캡처 도구**: `MinimapCaptureEditorWindow`로 씬을 탑다운 촬영 → PNG 저장 → `MinimapIconConfigSO` 자동 할당

---

## 아키텍처

```
┌─────────────────────────────────────────────────────────┐
│                    UI_Minimap (UI_Base)                  │
│  CanvasLayer.HUD                                         │
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │  플레이어     │  │   적 아이콘  │  │  퀘스트 마커  │  │
│  │  방향 화살표  │  │  _enemyIconMap│  │  _questIconMap│  │
│  │  _playerIcon  │  │              │  │               │  │
│  └──────────────┘  └──────┬───────┘  └──────┬────────┘  │
└─────────────────────────────────────────────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
   GameObjectManager    MonsterActor      MinimapMarkerRegistry
   .OnActorRegistered   .Detection          (정적 레지스트리)
   .OnActorUnregistered .HasTarget               │
                                          MinimapMarkerRegistrar
                                          (씬 오브젝트에 부착)
                              ▲
                    QuestManager (EventManager)
                    QuestEvent.QuestAccepted
                    QuestEvent.QuestCompleted
                    QuestEvent.QuestFailed
```

### 클래스 의존 관계

```
데이터 계층 (Assets/02.Scripts/Data/UI/)
└── MinimapIconConfigSO     — 아이콘 스프라이트·색상·표시 설정 통합 SO

UI 계층 (Assets/02.Scripts/UI/HUD/)
├── UI_Minimap              — 메인 HUD (UI_Base 상속)
├── MinimapEntityIcon       — 개별 아이콘 컴포넌트 (팩토리 패턴)
├── MinimapMarkerRegistrar  — 씬 배치 위치 등록 컴포넌트
└── MinimapMarkerRegistry   — 정적 레지스트리 (static class)

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
│   │       ├── UI_Minimap.cs               — 미니맵 HUD 메인 클래스
│   │       ├── MinimapEntityIcon.cs         — 개별 아이콘 컴포넌트
│   │       ├── MinimapMarkerRegistrar.cs    — 씬 마커 등록 컴포넌트
│   │       └── MinimapMarkerRegistry.cs     — 마커 정적 레지스트리
│   │
│   ├── Data/
│   │   └── UI/
│   │       └── MinimapIconConfigSO.cs       — 아이콘 설정 ScriptableObject
│   │
│   └── Tool/
│       └── Editor/
│           └── Minimap/
│               └── MinimapCaptureEditorWindow.cs  — 씬 캡처 에디터
│
└── 10.Datas/
    └── UI/
        └── Minimap/                         — 캡처된 배경 텍스처 저장 위치 (권장)
```

---

## 핵심 클래스

### `UI_Minimap`

`UI_Base`를 상속하는 HUD 클래스. `OnShow`에서 모든 이벤트를 구독하고 `OnHide`에서 해제합니다. `LateUpdate`에서 매 프레임 4개 섹션을 순차 갱신합니다.

| 직렬화 필드 | 타입 | 설명 |
|-------------|------|------|
| `_iconContainer` | `RectTransform` | 적·NPC·채집 아이콘의 부모 |
| `_questContainer` | `RectTransform` | 퀘스트 마커 부모 (`_iconContainer`와 같아도 무방) |
| `_playerIcon` | `RectTransform` | 플레이어 방향 화살표 이미지 |
| `_mapBackground` | `Image` | MapImage 모드 배경 이미지 |
| `_config` | `MinimapIconConfigSO` | 아이콘·스케일·모드 설정 |
| `_maskDisplaySize` | `float` | 마스크 원 픽셀 크기 (기본 200) |

**내부 아이콘 맵 (런타임)**

| 딕셔너리 | 키 | 값 | 용도 |
|----------|----|----|------|
| `_enemyIconMap` | `MonsterActor` | `MinimapEntityIcon` | 적 아이콘 |
| `_actorIconMap` | `GameActor` | `MinimapEntityIcon` | NPC·채집 아이콘 |
| `_questIconMap` | `string` (locationId) | `MinimapEntityIcon` | 퀘스트 마커 |

---

### `MinimapIconConfigSO`

`Assets/10.Datas/UI/` 아래에 에셋으로 생성합니다. (`UPlayGround/UI/MinimapIconConfig`)

**아이콘 항목 (`IconEntry` struct)**

| 필드 | 타입 | 설명 |
|------|------|------|
| `sprite` | `Sprite` | 아이콘 스프라이트 |
| `color` | `Color` | 아이콘 색상 |
| `size` | `float` | 아이콘 픽셀 크기 (8~40) |

**아이콘 항목 목록**

| 항목 | 표시 대상 |
|------|-----------|
| `player` | 플레이어 |
| `enemy` | 적 (비전투 상태) |
| `enemyDetected` | 적 (플레이어를 인식한 전투 상태) |
| `npc` | NPC |
| `gathering` | 채집 오브젝트 |
| `questTarget` | ReachLocation 목표 마커 |
| `questNpc` | ItemDeliver NPC 마커 |
| `customMarker` | MinimapMarkerType.Custom 마커 |

**표시 옵션**

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `displayMode` | `IconOnly` | IconOnly / MapImage 모드 전환 |
| `showQuestMarkers` | `true` | 퀘스트 마커 표시 여부 |
| `showEnemies` | `true` | 적 아이콘 표시 여부 |
| `showOnlyDetectedEnemies` | `false` | true = 플레이어 감지한 적만 표시 |
| `showNpcs` | `true` | NPC 아이콘 표시 여부 |
| `showGathering` | `true` | 채집 오브젝트 아이콘 표시 여부 |
| `rotateWithPlayer` | `true` | IconOnly 모드에서 플레이어 전방이 항상 위 |
| `worldToMinimapScale` | `0.05` | 월드 1유닛 = N픽셀 (IconOnly 모드) |
| `minimapRadius` | `100` | 아이콘 클리핑 반지름 px (IconOnly 모드) |

**MapImage 모드 전용 필드**

| 필드 | 설명 |
|------|------|
| `backgroundSprite` | 캡처된 배경 스프라이트 (MinimapCaptureEditor가 자동 할당) |
| `captureCenter` | 캡처 당시 월드 XZ 중심 (`Vector2`) |
| `captureWorldSize` | 캡처 범위 (월드 유닛, 한 변 길이) |

---

### `MinimapEntityIcon`

개별 아이콘 GameObject. `UI_Minimap`이 팩토리 메서드로 생성하며 직접 `AddComponent`하지 않습니다.

```csharp
// 액터 추적 아이콘 생성
MinimapEntityIcon.Create(Transform parent, GameActor actor, MinimapIconConfigSO.IconEntry entry)

// 정적 마커 생성 (퀘스트 목표, 위치 등)
MinimapEntityIcon.CreateStatic(Transform parent, string label, MinimapIconConfigSO.IconEntry entry)

// 위치·가시성 갱신
void UpdateIcon(Vector2 minimapPos, bool isVisible)

// 런타임 색상 변경 (적 감지 상태 전환 등)
void SetColor(Color color)

// 스프라이트·색상·크기 일괄 변경
void SetEntry(MinimapIconConfigSO.IconEntry entry)
```

---

### `MinimapMarkerRegistrar`

씬 오브젝트에 부착해 `MinimapMarkerRegistry`에 위치를 등록합니다. `Awake`/`OnDestroy`에서 자동 등록/해제합니다.

| 직렬화 필드 | 설명 |
|-------------|------|
| `_locationId` | 퀘스트 목표와 매핑되는 문자열 ID |
| `_markerType` | `QuestTarget` (노란 "!") 또는 `Custom` |

**프로퍼티**

```csharp
string           LocationId    // Inspector에서 설정한 locationId
MinimapMarkerType MarkerType   // 마커 타입
Vector3          WorldPosition // transform.position 래퍼
```

---

### `MinimapMarkerRegistry`

씬의 모든 `MinimapMarkerRegistrar`를 `locationId`로 관리하는 정적 클래스입니다.

```csharp
// 이벤트
static event Action<MinimapMarkerRegistrar> OnMarkerAdded
static event Action<MinimapMarkerRegistrar> OnMarkerRemoved

// 조회
static bool TryGet(string locationId, out MinimapMarkerRegistrar registrar)
static IEnumerable<MinimapMarkerRegistrar> GetAll()
```

---

## 셋업 방법

### 1단계: MinimapIconConfigSO 생성

1. `Project` 창에서 우클릭 → `UPlayGround/UI/MinimapIconConfig`
2. `Assets/10.Datas/UI/` 아래 저장
3. Inspector에서 아이콘 항목별 **Sprite / Color / Size** 설정

### 2단계: UI_Minimap 프리팹 제작

```
UI_Minimap (GameObject)
  ├─ Canvas 컴포넌트 (CanvasLayer = HUD 인스펙터 설정)
  ├─ UI_Minimap 컴포넌트
  └─ MinimapMask (Image — 원형 스프라이트 + Mask 컴포넌트)
       ├─ MapBackground (Image)          ← _mapBackground 슬롯
       ├─ QuestContainer (RectTransform) ← _questContainer 슬롯
       ├─ IconContainer  (RectTransform) ← _iconContainer 슬롯
       └─ PlayerIcon     (Image — 화살표 스프라이트) ← _playerIcon 슬롯
```

> **MinimapMask 설정**: `Image Type = Simple`, `Preserve Aspect = false`, 원형 Sprite 할당, `Mask` 컴포넌트 추가.  
> `_maskDisplaySize`는 MinimapMask의 `RectTransform.sizeDelta.x`와 동일한 값으로 설정.

### 3단계: UIPrefabDatabase에 등록

기존 `HudPlayerInfo` 등록 방식과 동일하게 `Minimap` 키로 프리팹 추가.  
`UIKeyType` enum에 `Minimap = 17` 항목이 이미 추가되어 있습니다.

### 4단계: 퀘스트 목표 마커 연결 (퀘스트 목표가 있는 경우)

씬의 목표 오브젝트(트리거 존, NPC 등)에 `MinimapMarkerRegistrar` 컴포넌트를 추가합니다.

| 퀘스트 목표 타입 | LocationId 설정 방법 |
|-----------------|---------------------|
| `ReachLocation` | `QuestObjectiveData.targetStringId`와 동일하게 설정 |
| `ItemDeliver` (NPC 전달) | `"npc_{npcId}"` 형식 (예: npcId=101 → `"npc_101"`) |

### 5단계: MapImage 모드 설정 (선택)

아이콘 전용 모드로 충분한 경우 이 단계를 건너뜁니다.

1. `UPlayGround → Minimap → Minimap Capture Editor` 창 열기
2. 씬 뷰 Gizmo로 캡처 범위 확인 후 **캡처 & 저장** 클릭
3. MinimapIconConfigSO를 연결하면 `backgroundSprite` / `captureCenter` / `captureWorldSize` 자동 할당
4. Config의 `displayMode`가 `MapImage`로 자동 전환됨

---

## 사용 예시

### 기본: 인게임 미니맵 표시 (UI_GamePlay에서)

```csharp
// UI_GamePlay.cs — OnShow()
protected override void OnShow()
{
    _hudPlayerInfo = UIManager.Instance.ShowUI(UIKeyType.HudPlayerInfo)
                              ?.GetComponent<UI_HudPlayerInfo>();
    // 미니맵 표시
    UIManager.Instance.ShowUI(UIKeyType.Minimap);
    // ...
}

protected override void OnHide()
{
    UIManager.Instance.HideUI(UIKeyType.Minimap);
    // ...
}
```

### 퀘스트 목표 마커 씬 배치

```csharp
// 씬의 목표 지점 GameObject에 부착
// Inspector: LocationId = "dungeon_entrance"

// QuestSO의 objectives 중 ReachLocation 목표:
// targetStringId = "dungeon_entrance"
// → 퀘스트 수락 시 미니맵에 자동으로 "!" 마커 표시
```

### ItemDeliver 목표 NPC 마커

```csharp
// NPC GameObject에 MinimapMarkerRegistrar 부착
// Inspector: LocationId = "npc_101"  (npcId = 101)

// QuestObjectiveData:
//   type = ItemDeliver
//   npcId = 101
// → 퀘스트 수락 시 해당 NPC 위치에 NPC 마커 자동 표시
```

### MinimapIconConfigSO 런타임 설정 확인 예시

```csharp
// 적 감지 상태 아이콘 색상 구분 동작 원리 (UI_Minimap 내부)
bool isDetected = monster.Detection != null && monster.Detection.HasTarget;
icon.SetColor(isDetected ? _config.enemyDetected.color : _config.enemy.color);
```

### 월드 → 미니맵 좌표 변환

```csharp
// IconOnly 모드 (플레이어 기준 상대 좌표)
Vector3 offset = worldPos - player.transform.position;
Vector2 minimapPos = new Vector2(offset.x, offset.z) * config.worldToMinimapScale;

// MapImage 모드 (캡처 범위 기준 절대 좌표)
// config.WorldToMapImagePos(worldPos, maskDisplaySize) 내부 동작:
float nx = (worldPos.x - captureCenter.x) / captureWorldSize;  // -0.5 ~ 0.5
float ny = (worldPos.z - captureCenter.y) / captureWorldSize;
Vector2 minimapPos = new Vector2(nx * maskDisplaySize, ny * maskDisplaySize);
```

---

## 에디터 도구 — Minimap Capture Editor

메뉴: `UPlayGround → Minimap → Minimap Capture Editor`

씬을 탑다운 직교 카메라로 촬영해 미니맵 배경 이미지를 생성합니다.

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
4. `displayMode = MapImage` 자동 전환

---

## 주의 사항

### 프리팹 구조
- `_maskDisplaySize`는 MinimapMask RectTransform의 `sizeDelta.x`와 반드시 동일해야 합니다. 불일치 시 아이콘이 마스크 경계 밖으로 나갑니다.
- `_questContainer`가 `null`이면 `_iconContainer`를 대신 사용합니다. 퀘스트 마커를 별도 레이어로 관리하려면 반드시 별도 오브젝트에 연결하세요.

### 아이콘 스프라이트 미설정
- `MinimapIconConfigSO`의 각 `IconEntry.sprite`가 `null`이면 해당 타입의 액터 아이콘이 생성되지 않습니다. 모든 항목에 스프라이트를 할당해야 합니다.

### 퀘스트 마커가 표시되지 않는 경우
- `MinimapMarkerRegistrar.LocationId`와 `QuestObjectiveData.targetStringId`(또는 `"npc_{npcId}"`)가 정확히 일치해야 합니다.
- `QuestManager.IsDBLoaded`가 `false`인 상태(DB 로딩 중)에서는 `RefreshAllQuestMarkers()`가 빈 목록을 반환합니다. DB 로드 완료 후 퀘스트 수락 시에는 정상 동작합니다.

### 적 감지 상태 아이콘
- `MonsterActor.Detection`이 null인 몬스터(Detection 컴포넌트 없음)는 항상 `enemy` (비전투) 색상으로 표시됩니다.
- `showOnlyDetectedEnemies = true` 설정 시, 전투 진입 전 몬스터는 미니맵에서 숨겨집니다.

### MapImage 모드 캡처 범위
- 캡처 이후 레벨 지형이 크게 변경된 경우 재캡처가 필요합니다.
- `captureWorldSize`가 실제 맵보다 작으면 범위 밖의 아이콘 좌표가 마스크 영역을 벗어납니다.

### UIKeyType 자동 생성
- `UIKeyType.cs`는 자동 생성 파일입니다. `UPlayGround/ID Enum Generator` 창 재실행 시 `Minimap = 17` 항목을 데이터 소스에 추가해야 합니다.

---

## 확장 포인트

### 새로운 아이콘 타입 추가

`MinimapIconConfigSO`에 `IconEntry` 필드를 추가하고, `GetActorIconEntry()` 메서드에 새 `ActorType` 분기를 추가합니다.

```csharp
// MinimapIconConfigSO.cs
public IconEntry boss; // 보스 아이콘

public IconEntry GetActorIconEntry(ActorType actorType)
{
    if ((actorType & ActorType.Player)  != 0) return player;
    if ((actorType & ActorType.Monster) != 0)
    {
        // 보스 등급 구분이 필요하면 MonsterActor를 직접 참조
        return enemy;
    }
    // ...
}
```

### 새로운 퀘스트 목표 타입 마커 지원

`UI_Minimap.ResolveQuestLocationId()` 메서드에 새 타입 분기를 추가합니다.

```csharp
private static string ResolveQuestLocationId(QuestObjectiveData obj)
{
    return obj.type switch
    {
        QuestObjectiveType.ReachLocation => obj.targetStringId,
        QuestObjectiveType.ItemDeliver   => $"npc_{obj.npcId}",
        QuestObjectiveType.MonsterKill   => $"monster_{obj.targetId}", // 확장 예시
        _                               => null,
    };
}
```

### 아이콘 깜빡임 등 애니메이션 효과

`MinimapEntityIcon.SetColor()` 대신 `SetEntry()`로 매 프레임 `IconEntry`를 전환하거나, `MinimapEntityIcon`에 `Coroutine` 기반 펄스 애니메이션 메서드를 추가할 수 있습니다.

### 커스텀 마커 프로그래밍 등록

`MinimapMarkerRegistrar`를 컴포넌트로 부착하지 않고 코드로 직접 등록하고 싶다면, 별도의 `RuntimeMinimapMarker` 래퍼 클래스를 만들어 `MinimapMarkerRegistry.Register()`를 호출합니다.
