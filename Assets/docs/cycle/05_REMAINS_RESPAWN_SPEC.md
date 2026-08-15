# Remains & Respawn 구현 스펙

## 1. 목표

출전 파티 전멸 시 손실물을 사망 지점의 유해에 보관하고, 회수하면 원래 상태로 복구한다. 회수 전에 다시 전멸하면 기존 유해를 제거하고 새 유해로 교체한다.

P0 손실물:

- `BattleOrder` 전원의 현재 레벨 경험치 진행분 30%
- `CycleLootLedger`의 미정산 재료 전량

P0 비손실물:

- 캐릭터 레벨
- 영구 인벤토리와 골드
- 장비와 고유 무기
- 플레이어블 캐릭터 해금
- 보스 어시스트 로스터
- 사이클 한정 기능이 추가되기 전의 기타 데이터

---

## 2. 기존 코드 접점

| 기존 타입 | 현재 동작 | 변경 방향 |
|---|---|---|
| `PlayerDeathState` | 지연 후 `PartyManager.TrySwitchToNextAliveAfterActiveDeath()` 시도 | 전환 실패 시 즉시 기존 팝업을 열지 않고 사이클 사망 서비스 호출 |
| `PartyManager.TrySwitchToNextAliveAfterActiveDeath` | 살아 있는 출전 멤버로 자동 교체 | 그대로 유지. 유해는 전환 실패 시에만 생성 |
| `PartyManager.GetExp` | 캐릭터별 현재 레벨 진행 경험치 조회 | 손실 스냅샷 계산에 사용 |
| `PartyManager.AwardBattleExp` | `BattleOrder` 전원에게 동일 경험치 지급 | 유해 회수 시 캐릭터별 복구 API는 별도 필요 |
| `PlayerActor.Respawn` | 지정 위치·회전·회복률로 액티브 캐릭터 부활 | 파티 전원 회복 후 대표 액터 위치 복원에 사용 |
| `PartyManager.HealAllParty` | 액티브·벤치 전체 회복 | 부활 시 `reviveDowned: true` 호출 |
| `RestPointActor` | 파티 전원 회복, 무제한 사용 | 활성 부활 지점의 상호작용 기반으로 활용 |
| `PortalActor.GetArrivalPoint` | 기존 사망 UI가 최근접 포탈 위치 사용 | 사이클 모드에서는 활성 휴식 지점으로 대체 |
| 미니맵 마커 시스템 | 정적/액터 마커 표시 | 유해 전용 마커 추가 |

기존 `UI_Popup_Respawn`의 현장 부활/포탈 부활 선택은 사이클 모드에서 사용하지 않는다. 전멸 후 가장 가까운 활성 휴식 지점으로 자동 부활한다.

---

## 3. 미정산 재료 원장

### `CycleLootLedger`

P0 사이클 재료는 획득 즉시 `InventoryManager`에 넣지 않는다.

```csharp
[Serializable]
public sealed class CycleLootLedger
{
    public List<CycleItemStack> unsettledMaterials;

    public void Add(int itemId, int count);
    public IReadOnlyList<CycleItemStack> Snapshot();
    public void Clear();
    public void Restore(IReadOnlyList<CycleItemStack> items);
}
```

- `CycleConfigSO` 또는 별도 데이터에 P0 미정산 대상 Item ID 목록을 둔다.
- 해당 아이템 픽업은 `CycleLootLedger.Add`로 라우팅한다.
- 일반 소비 아이템과 기존 영구 아이템은 기존 `InventoryManager` 흐름을 유지한다.
- 탈출 정산 때만 `InventoryManager.AddItem`으로 커밋한다.
- 유해 생성 시 원장을 스냅샷하고 비운다.
- 회수 시 유해의 재료를 원장에 되돌린다.

인벤토리에 먼저 넣고 사망 때 빼는 방식은 장착·소비·퀘스트 알림과 충돌하므로 사용하지 않는다.

---

## 4. 경험치 손실 API

현재 `PartyManager`는 경험치 지급 API를 제공하지만 현재 레벨 진행분을 안전하게 차감·복구하는 공개 API는 없다. 다음 API를 `PartyManager`에 추가한다.

```csharp
public long RemoveCurrentLevelExp(CharacterActorType type, long amount);
public void RestoreCurrentLevelExp(CharacterActorType type, long amount);
```

규칙:

- `RemoveCurrentLevelExp`는 0 아래로 내리지 않고 실제 차감량을 반환한다.
- 레벨은 절대 내리지 않는다. 레벨이 내려가면 이미 지급·소비한 스킬 포인트를 회수해야 하고, 그 시점에 찍은 노드를 강제 해제하는 문제가 생긴다. 이 규칙은 `08_CHARACTER_SKILL_GROWTH_SPEC.md`의 전제이므로 완화하지 않는다.
- **스킬 포인트와 취득 노드는 손실 대상이 아니다.** 유해는 경험치 진행분과 미정산 재료만 다룬다.
- `RestoreCurrentLevelExp`는 사망 시 차감한 값을 복구하는 전용 API다.
- 복구 경험치로 레벨업하지 않는다. 사망 직전 같은 현재 레벨 진행분으로 돌아가는 것이 목적이다.
- 두 API 모두 기존 `OnExpChanged`와 `OnPartyProgressionChanged`를 적절히 발행한다.

손실 계산:

```text
각 BattleOrder 멤버 loss = floor(GetExp(type) * 0.30)
```

한 명만 손실시키면 전투 경험치를 전원에게 주는 현재 정책과 맞지 않으므로 출전 멤버 전원을 대상으로 한다.

---

## 5. 유해 데이터

```csharp
[Serializable]
public sealed class RemainsState
{
    public string remainsId;
    public string mapId;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public List<LostExpEntry> lostExp;
    public List<CycleItemStack> materials;
    public bool recovered;
}
```

- 동시에 유효한 유해는 하나만 존재한다.
- `remainsId`는 저장과 중복 회수 방지용이다.
- 유해는 이전 사이클로 가져가지 않는다.
- 씬 좌표는 기존 `SerializableVector3/Quaternion`을 사용한다.

### `RemainsActor`

`IInteractable`을 구현하는 전용 액터를 권장한다.

- 플레이어가 상호작용하면 `RemainsService.TryRecover(remainsId)` 호출
- 회수 성공 후 FX·SFX와 함께 제거
- 자동 아이템 픽업으로 만들지 않음
- 적이나 공격 판정의 대상이 아님
- 미니맵과 나침반에 항상 유해 아이콘 표시

---

## 6. 전멸 처리

```text
PlayerDeathState
  -> 살아 있는 다음 멤버가 있으면 기존 자동 스왑 후 종료
  -> 없으면 RemainsService.HandlePartyWipe(deathPosition)
      -> 기존 유해가 있으면 영구 폐기
      -> BattleOrder 경험치 손실 계산·차감
      -> CycleLootLedger 스냅샷·비우기
      -> 새 RemainsState 저장
      -> RemainsActor 생성
      -> 최근접 활성 휴식 지점 선택
      -> 파티 전원 부활·풀 회복
      -> 플레이어 이동·카메라 스냅
      -> 어시스트 남은 쿨다운 유지
      -> 저장 요청
```

`HandlePartyWipe`는 같은 전멸 애니메이션에서 두 번 호출되어도 유해를 중복 생성하지 않도록 실행 잠금을 둔다.

---

## 7. 부활 지점 선택

### `CycleRespawnPoint`

```csharp
public sealed class CycleRespawnPoint : MonoBehaviour
{
    public string RespawnId { get; }
    public bool IsActive { get; }
    public Transform ArrivalPoint { get; }
}
```

- 사이클 레이아웃에서 활성화된 지점만 후보로 사용한다.
- 사망 위치와 `ArrivalPoint`의 거리 제곱이 가장 작은 지점을 선택한다.
- 동률이면 `RespawnId` 정렬 순서로 결정해 재현성을 유지한다.
- 활성 지점이 없으면 플레이어 시작 `spawnId`를 폴백으로 사용한다.
- 최근접 `PortalActor` 검색은 사이클 모드에서 사용하지 않는다.

부활 순서:

1. 사망 상태 코루틴과 입력 잠금 정리
2. `PartyManager.HealAllParty(true)`
3. 현재 액티브 멤버가 유효하지 않으면 첫 번째 BattleOrder로 활성 보정
4. `PlayerActor.Respawn` 또는 KCC Motor로 위치·회전 적용
5. `CameraManager.SnapToTarget`
6. HUD·유해 마커 갱신

---

## 8. 회수와 재사망

### 회수

```text
TryRecover(remainsId)
  -> 현재 유해 ID 일치 확인
  -> lostExp를 캐릭터별 복구
  -> materials를 CycleLootLedger에 복구
  -> recovered = true
  -> 유해 상태와 Actor 제거
  -> 마커 제거
  -> 저장 요청
```

모든 복구 가능성을 먼저 검증한 뒤 커밋한다. 일부 경험치만 복구되고 재료 복구가 실패하는 부분 성공을 허용하지 않는다.

### 회수 전 재사망

- 기존 유해 경험치와 재료는 복구 없이 폐기한다.
- 현재 남은 경험치와 새 원장만 대상으로 새 손실을 계산한다.
- 기존 마커와 Actor를 제거한 뒤 새 유해를 만든다.
- 영구 손실량을 텔레메트리에 별도로 기록한다.

---

## 9. 사이클 경계

- 중앙 보스 처치만으로 유해를 제거하지 않는다.
- 탈출 포털 진입 시 미회수 유해는 영구 폐기한다.
- 새 사이클 시작 전 이전 유해 상태가 없어야 한다.
- 저장 로드 후 유해가 존재하면 해당 씬과 좌표에 Actor를 한 번만 복원한다.
- 유해 좌표가 유효하지 않으면 최근접 활성 휴식 지점이 아니라 원래 사망 위치 근처의 안전 지면으로 보정한다. 보정 실패 시 로드 오류로 기록하고 자동 회수하지 않는다.

---

## 10. 완료 조건

1. 멤버 한 명 사망 시 살아 있는 멤버로 스왑되고 유해가 생기지 않는다.
2. 파티 전멸 시 유해가 정확히 하나 생성된다.
3. 출전 멤버 전원의 현재 레벨 경험치 30%와 미정산 재료가 유해로 이동한다.
4. 레벨, 장비, 골드, 로스터는 손실되지 않는다.
5. 회수 시 손실분이 정확히 복구된다.
6. 회수 전 재전멸 시 기존 유해가 사라지고 새 유해만 남는다.
7. 부활 후 어시스트 쿨다운이 유지된다.
8. 저장·로드 후 유해와 손실 데이터가 중복 없이 복원된다.
