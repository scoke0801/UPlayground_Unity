# PlayerActor 접근·캐싱 코드 전수 조사

> 조사 범위: `Assets/02.Scripts` (ExternalAssets 제외)  
> 조사 목적: 파티 캐릭터 전환 시 구버전 PlayerActor 참조가 남는 위험 지점 파악

---

## 1. 전체 목록

| # | 파일 | 접근·캐싱 방식 | 갱신 시점 | 전환 안전 |
|---|------|--------------|---------|---------|
| 1 | `Manager/Object/GameObjectManager.cs` | 필드 `_player` ← `FindWithTag("Player")` | `Init()`, `OnSceneChanged()`, `SetActivePartyPlayer()` 호출 시 | ⚠️ 수동 갱신 의존 |
| 2 | `Manager/Party/PartyManager.cs` | 리스트 `_partyMembers` ← `FindObjectsByType<PlayerActor>()` | `BuildPartyFromScene()` (`AfterInit`, `OnSceneChanged`) | ✅ |
| 3 | `Component/Player/PlayerSwapBehaviour.cs` | 필드 `_playerActor` ← `GetComponent<>()` (동일 GO) | `Awake()` | ✅ |
| 4 | `UI/HUD/UI_HUD_GamePlay.cs` | 필드 `_playerActor` ← `GameObjectManager.Instance.Player` | `OnShow()` | ⚠️ |
| 5 | `UI/HUD/UI_HUD_Minimap.cs` | 필드 `_player` ← `GameObjectManager.Instance?.Player` | `OnShow()` | ⚠️ |
| 6 | `UI/HUD/UI_Scene_Map.cs` | 필드 `_player` ← `GameObjectManager.Instance?.Player` | `OnShow()` | ⚠️ |
| 7 | `UI/HUD/UI_HUD_PlayerInfo.cs` | 필드 `_playerActor` ← `GameObjectManager.Instance.Player` | `OnShow()` | ⚠️ |
| 8 | `UI/Inventory/UI_Scene_Inventory.cs` | 로컬 변수 ← `GameObjectManager.Instance?.Player` | 각 메서드 호출 시 (매번 획득) | ✅ |
| 9 | `Object/Player/PlayerPreviewActor.cs` | 필드 `_cachedPlayerEquipment` ← `GameObjectManager.Instance.Player?.GetPlayerEquipment()` | `Awake()` (단 1회) | 🔴 |
| 10 | `Manager/Handler/GameInteractionHandler.cs` | 필드 `_player` ← lazy-init (`GameObjectManager.Instance.Player`) | `Update()` 중 null일 때만 재획득 | ⚠️ |
| 11 | `Object/Prop/GatheringActor.cs` | 로컬 변수 ← `GameObjectManager.Instance.Player` | `OnHitEvent()` 마다 (매번 획득) | ✅ |
| 12 | `Object/Prop/ItemActor.cs` | 필드 `_player` ← `GameObjectManager.Instance.Player.transform` | `Start()` | 🔴 |
| 13 | `Object/VitalOrb/VitalOrbActor.cs` | 필드 `_playerTransform` ← `GameObjectManager.Instance.Player?.GetSocket()` | `Initialize()` (생성 직후 1회) | ⚠️ |
| 14 | `State/NPC/NpcTalkState.cs` | 로컬 변수 ← `GameObjectManager.Instance.Player` | `UpdateRotation()` 마다 (매번 획득) | ✅ |
| 15 | `Data/Event/Animation/MotionEvent_*.cs` (6종) | 메서드 파라미터 `target as/GetComponent<PlayerActor>()` | `Execute()`, `OnCompleteEvent()` 호출 시 | ✅ |
| 16 | `Object/Projectile/BaseProjectile.cs` | 메서드 파라미터 `ownerObject as PlayerActor` | `Initialize()` 호출 시 | ✅ |

---

## 2. 위험 등급별 분류

### 🔴 즉시 수정 필요 (2개)

#### `ItemActor.cs`
- **문제**: `Start()`에서 `GameObjectManager.Instance.Player.transform`을 `_player` 필드에 캐싱한 뒤 갱신 없음
- **증상**: 파티 전환 후 드롭된 아이템이 구캐릭터 위치로 계속 이동
- **해결**: `Update()`마다 `GameObjectManager.Instance.Player.transform`을 직접 참조하거나, `PartyManager.OnSwapCompleted`에서 갱신

#### `PlayerPreviewActor.cs`
- **문제**: `Awake()`에서 `_cachedPlayerEquipment`를 단 1회 캐싱
- **증상**: 인벤토리 프리뷰가 파티 전환 이후에도 초기 캐릭터 장비만 표시
- **해결**: `PartyManager.OnSwapCompleted` 구독 후 `_cachedPlayerEquipment` 갱신

---

### ⚠️ 파티 전환 시 주의 (5개)

#### `UI_HUD_GamePlay.cs` / `UI_HUD_Minimap.cs` / `UI_Scene_Map.cs` / `UI_HUD_PlayerInfo.cs`
- **문제**: 모두 `OnShow()`에서만 `GameObjectManager.Instance.Player`를 가져옴
- **증상**: UI가 열린 상태로 파티 전환 시 HP·스킬 게이지가 구캐릭터 이벤트에 계속 반응
- **해결 패턴** (공통):
  ```csharp
  // 초기화 및 전환 이벤트 동시 처리
  private void Start()
  {
      PartyManager.Instance.OnSwapCompleted += OnActivePlayerChanged;
  }

  private void OnActivePlayerChanged(PlayerActor newPlayer)
  {
      // 기존 이벤트 해제 후 새 플레이어 구독
      UnsubscribeFromPlayer(_playerActor);
      _playerActor = newPlayer;
      SubscribeToPlayer(_playerActor);
  }
  ```

#### `GameInteractionHandler.cs`
- **문제**: `_player` 필드가 null일 때만 lazy-init되어 전환 후에도 구버전 유지
- **증상**: 상호작용 감지 기준 위치가 구캐릭터 transform
- **해결**: `PartyManager.OnSwapCompleted`에서 `_player` 명시적 갱신

#### `VitalOrbActor.cs`
- **문제**: `Initialize()` 시점에 Player 소켓을 캐싱하므로 이후 전환 미반영
- **증상**: 회복 구슬이 구캐릭터 위치로 흡입됨 (전환 직전 생성된 구슬만 해당)
- **해결**: Update()에서 `GameObjectManager.Instance.Player?.GetSocket()`을 직접 참조하거나, 짧은 생명주기 특성상 허용 가능

---

### ✅ 안전 (9개)

| 파일 | 안전한 이유 |
|------|-----------|
| `PartyManager.cs` | `OnSwapCompleted` 이벤트 발신 측이므로 항상 최신 |
| `PlayerSwapBehaviour.cs` | 동일 GameObject의 컴포넌트, 교체 불가 |
| `UI_Scene_Inventory.cs` | 각 메서드에서 매번 `GameObjectManager.Instance.Player` 획득 |
| `GatheringActor.cs` | 이벤트 시점마다 매번 획득 |
| `NpcTalkState.cs` | `UpdateRotation()` 매 프레임 획득 |
| `MotionEvent_*.cs` (6종) | 메서드 파라미터로 수신, 저장 없음 |
| `BaseProjectile.cs` | 발사체 생성 시점의 owner, 이후 독립 동작 |

---

## 3. 핵심 갱신 경로

```
PartyManager.RequestSwapTo()
    └─ NotifyActivePlayerChanged()
           ├─ GameObjectManager.SetActivePartyPlayer(newPlayer)  ← _player 갱신
           └─ CameraManager.SetTarget(newPlayer.transform)       ← 카메라 추적 대상 갱신
    └─ OnSwapCompleted?.Invoke(newPlayer)                        ← 구독자에게 전파
           └─ [현재 미구독] UI_HUD_GamePlay, UI_HUD_Minimap, UI_Scene_Map,
                            UI_HUD_PlayerInfo, GameInteractionHandler,
                            PlayerPreviewActor
```

`OnSwapCompleted`에 구독하는 것이 파티 전환에 대응하는 표준 패턴입니다.

---

## 4. 즉시 조치 우선순위

| 우선순위 | 대상 | 예상 증상 |
|---------|------|---------|
| P1 | `UI_HUD_PlayerInfo.cs` | HP·스킬 게이지 오동작 (플레이어 직접 체감) |
| P1 | `ItemActor.cs` | 아이템이 구캐릭터 위치로 이동 |
| P2 | `GameInteractionHandler.cs` | 상호작용 감지 오동작 |
| P2 | `UI_HUD_GamePlay.cs`, `UI_HUD_Minimap.cs`, `UI_Scene_Map.cs` | HUD 표시 오류 |
| P3 | `PlayerPreviewActor.cs` | 인벤토리 프리뷰 오표시 |
| P3 | `VitalOrbActor.cs` | 회복 구슬 흡입 위치 오류 (전환 직전 생성분만) |
