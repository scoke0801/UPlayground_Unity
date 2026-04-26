# 전투 캐릭터 교체 시스템 설계

> 작성일: 2026-04-25  
> 최종 수정: 2026-04-25 (Phase 1 구현 완료)  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 1. 시스템 개요

전투 중 플레이어가 3종의 플레이어블 캐릭터(Bokusei·Honoka·LianLian)를 즉시 교체하여 전투를 이어갈 수 있는 시스템. 각 캐릭터는 고유 무기, 공격 데이터, 스킬 게이지를 독립적으로 유지하며 교체 후 이어받는 상태(위치, 카메라 타깃, 전투 타깃)는 공유한다.

참고 게임플레이 레퍼런스: Devil May Cry 5의 캐릭터 교체, Genshin Impact의 파티 전환.

---

## 2. 설계 목표 및 제약 조건

### 목표
- 전투 흐름을 끊지 않는 빠른 교체 (기본 쿨다운 0.5초)
- 대기 중인 캐릭터의 체력·스킬 게이지 독립 관리
- 기존 매니저·컴포넌트 아키텍처 최소 침범

### 제약
- 교체 불가 상태: `Death`, `Grab`, `Knockdown`, 스토리 시네마틱
- 대기 중인 캐릭터는 무적 (전투 타깃에서 제외)
- 파티 인원 1명일 때 교체 불가
- 한 번에 최대 3명 파티

---

## 3. 아키텍처 설계

### 3.1 전체 구조

```
GameManager
└── PartyManager (신규 매니저)
    ├── List<PartySlot> _slots (최대 3)
    ├── int _activeIndex
    └── CharacterSwapRequest / Response 흐름

GameObjectManager (기존 수정)
├── PlayerActor _activePlayer  ← 단일 참조 유지 (기존 Player 프로퍼티)
└── List<PlayerActor> _partyActors (신규 - 전체 파티)

PlayerActor (기존 수정)
└── PlayerSwapBehaviour (신규 컴포넌트)
    ├── 활성/대기 전환 처리
    └── 교체 진입·복귀 애니메이션 트리거
```

### 3.2 PartyManager

신규 매니저 (`BaseManager<PartyManager>` 상속, `IManager` 구현).  
씬 오브젝트 직접 참조 없이, `PartyConfigSO` 데이터와 `FindObjectsByType` 으로 파티를 구성한다.

```
Assets/02.Scripts/Manager/Party/PartyManager.cs
```

- `Init()`: `Resources.Load<PartyConfigSO>("Data/PartyConfig")` 로 SO 로드 + 스왑 입력 등록
- `AfterInit()`: 씬에서 `PlayerActor` 전체 탐색 → SO 순서로 `_partyMembers` 빌드 → 초기 활성/대기 상태 설정
- `OnSceneChanged()`: 씬 전환 시 파티 재구성

**핵심 인터페이스:**

```csharp
public bool RequestSwapNext();
public bool RequestSwapPrev();
public bool RequestSwapTo(int index);
public bool CanSwap();

public PlayerActor               ActiveCharacter { get; }
public IReadOnlyList<PlayerActor> PartyMembers   { get; }

public event Action<PlayerActor, PlayerActor> OnSwapStarted;
public event Action<PlayerActor>              OnSwapCompleted;
```

### 3.3 PartyConfigSO (신규 데이터)

```
Assets/02.Scripts/Data/Party/PartyConfigSO.cs
Assets/Resources/Data/PartyConfig.asset       ← 에디터에서 생성 필요
```

```csharp
[CreateAssetMenu(menuName = "UPlayGround/Party/Party Config")]
public class PartyConfigSO : ScriptableObject
{
    // 파티 슬롯 순서 — 씬에 해당 CharacterType의 PlayerActor가 있으면 자동 포함
    public List<CharacterActorType> partyOrder;
    public int startActiveIndex;
}
```

SO가 없거나 `partyOrder`가 비어있으면 씬의 모든 PlayerActor를 순서 없이 폴백으로 사용한다.

### 3.3 PlayerSwapBehaviour (신규 컴포넌트)

`PlayerActor`에 붙는 컴포넌트. 활성·대기 전환의 **비주얼·물리** 처리를 담당.

```csharp
// Assets/02.Scripts/GameActor/Component/Player/PlayerSwapBehaviour.cs

public class PlayerSwapBehaviour : PlayerActorComponent
{
    [SerializeField] float _swapOutDuration = 0.2f;
    [SerializeField] float _swapInDuration  = 0.3f;
    [SerializeField] GameObject _swapOutVFX;
    [SerializeField] GameObject _swapInVFX;

    // 활성화: 입력 등록, KCC 활성, 렌더러 표시
    public void EnterActive(Vector3 position, Quaternion rotation);

    // 대기: 입력 해제, KCC 비활성, 렌더러 숨김, 무적
    public void EnterStandby();

    // 비동기: 교체 연출 재생 후 콜백
    public IEnumerator PlaySwapOutAnimation(Action onComplete);
    public IEnumerator PlaySwapInAnimation(Action onComplete);
}
```

### 3.4 GameObjectManager 수정

기존 `PlayerActor _player` 단일 참조는 **항상 현재 활성 캐릭터**를 가리키도록 유지 (하위 호환).  
파티 전체 참조는 별도 추가.

```csharp
// 기존
public PlayerActor Player => _player;

// 추가
public IReadOnlyList<PlayerActor> PartyActors => _partyActors;

// PartyManager의 OnSwapCompleted 이벤트 수신 시 _player 갱신
void OnSwapCompleted(PlayerActor newActive) => _player = newActive;
```

---

## 4. 교체 흐름 (Swap Flow)

```
[입력 수신]
  │ InputManager에서 스왑 키 이벤트 발생
  ▼
PartyManager.RequestSwapTo(index)
  │
  ├─ CanSwap() 실패 → 즉시 반환 (쿨다운, 불가 상태)
  │
  └─ CanSwap() 성공
       │
       ▼
  [1] OnSwapStarted 이벤트 발생
       │
       ▼
  [2] outgoing.PlayerSwapBehaviour.PlaySwapOutAnimation()
      - 교체 아웃 VFX 재생
      - PlayerCombat 공격 중단 (CurrentAttackData 클리어)
      - 코루틴 완료 대기
       │
       ▼
  [3] outgoing.PlayerSwapBehaviour.EnterStandby()
      - InputManager 입력 이벤트 해제
      - KCC(PhysicsMotor) 비활성
      - SkinnedMeshRenderer 비활성
      - Collider를 "무적 레이어"로 변경
      - 강제 Idle 상태 전환
       │
       ▼
  [4] incoming.PlayerSwapBehaviour.EnterActive(position, rotation)
      - outgoing의 위치·회전 이어받기
      - KCC 활성
      - SkinnedMeshRenderer 활성
      - InputManager 입력 이벤트 재등록
      - Idle 상태 진입
       │
       ▼
  [5] incoming.PlayerSwapBehaviour.PlaySwapInAnimation()
      - 교체 인 VFX 재생
      - 짧은 진입 모션 재생 (선택적)
       │
       ▼
  [6] CameraManager 타깃 갱신 (incoming Actor)
  [7] GameObjectManager._player 갱신
  [8] _lastSwapTime = Time.time, _isSwapping = false
       │
       ▼
  [9] OnSwapCompleted 이벤트 발생
```

---

## 5. 상태 관리

### 5.1 교체 시 공유되는 정보

| 항목 | 전달 여부 | 비고 |
|------|-----------|------|
| 월드 포지션·로테이션 | ✅ 전달 | incoming이 outgoing 위치에서 등장 |
| 현재 전투 타깃(락온) | ✅ 전달 | CameraManager/LockOn 시스템에서 처리 |
| 관성(Velocity) | ❌ 리셋 | incoming은 정지 상태로 시작 |
| 콤보 카운터 | ❌ 리셋 | 각 캐릭터 독립 |

### 5.2 교체 어시스트 (Swap Assist Counter)

**발동 조건:** 교체 직전 활성 캐릭터의 `PlayerCombat.IsPerfectDodgeWindow == true`  
→ 즉, 적의 공격 타이밍(Perfect Dodge 판정 창)에 도지 대신 교체를 선택했을 때 발동.

**흐름:**
```
적 공격 풍선(PerfectDodgeWindow 열림)
  → 플레이어가 도지 대신 교체 입력
  → PartyManager: isAssist = outgoing.GetCombat().IsPerfectDodgeWindow
  → outgoing EnterStandby
  → incoming EnterActive(pos, rot)
  → incoming.QueueSwapAssist()          ← PlayerActor._swapAssistQueued = true
  → 다음 프레임 incoming.Update():
      _attackInputCondition = Pressed   ← 일반 공격으로 처리
      → 상태머신이 자연스럽게 AttackState 진입
```

**현재 구현:** 일반 공격(AttackState) 발동. 추후 전용 AnimKey/공격 데이터로 교체 가능.  
**구현 위치:** `PartyManager.RequestSwapTo()` + `PlayerActor.QueueSwapAssist()`

### 5.2 캐릭터별 독립 상태

| 항목 | 관리 위치 |
|------|-----------|
| 현재 체력 | `PlayerActor._currentHealth` |
| 스킬 게이지 | `PlayerSkillGauge` |
| 장비 현황 | `PlayerEquipment` |
| 콤보 진행도 | `PlayerCombat` |
| 쿨다운 타이머 | 각 State 내부 |

### 5.3 교체 불가 상태 목록

`CanSwap()` 내부에서 현재 활성 캐릭터의 상태를 검사.

```csharp
bool CanSwap()
{
    if (_isSwapping) return false;
    if (Time.time - _lastSwapTime < _swapCooldown) return false;
    if (_slots.Count(s => s.IsAvailable) < 2) return false;

    var state = ActiveCharacter.MovementController.CurrentState;
    if (state is PlayerDeathState)       return false;
    if (state is PlayerGrabState)        return false;
    if (state is PlayerKnockdownState)   return false;
    // 필요 시 추가 (특수 스킬, 시네마틱 등)

    return true;
}
```

---

## 6. 입력 연동

### 6.1 입력 액션 추가

`PlayerInputActions` 애셋에 `Party` 액션 맵 추가.

| 액션명 | 기본 바인딩 | 설명 |
|--------|------------|------|
| `SwapNext` | Q / LB | 다음 캐릭터로 교체 |
| `SwapPrev` | E / RB (Hold) | 이전 캐릭터로 교체 |
| `SwapSlot1` | (선택) 1 | 1번 슬롯 직접 교체 |
| `SwapSlot2` | (선택) 2 | 2번 슬롯 직접 교체 |
| `SwapSlot3` | (선택) 3 | 3번 슬롯 직접 교체 |

### 6.2 InputLayer

교체 입력은 `InputLayer.Level_1 (Scene)` 레벨에 등록.  
전투 중이 아닐 때도 교체 가능하므로 HUD 레벨보다 높게 설정.

### 6.3 입력 등록 위치

`PartyManager.Init()` 또는 `AfterInit()`에서 InputManager에 직접 등록.  
(PlayerActor가 아닌 PartyManager가 교체 입력의 주인임)

```csharp
InputManager.Instance.RegisterInputEvent(
    mapName: "Party", actionName: "SwapNext",
    performed: _ => RequestSwapNext(),
    layer: InputLayer.Level_1
);
```

---

## 7. UI 연동

### 7.1 파티 HUD 구성

```
화면 하단 좌측:
┌─────────────────────────────────┐
│ [★ Bokusei ] [HP ████████░░] [스킬 ██░░] │  ← 활성 (강조)
│ [  Honoka  ] [HP ██████░░░░] [스킬 ░░░░] │
│ [  Reine   ] [HP ████░░░░░░] [스킬 ███░] │
└─────────────────────────────────┘
```

- 활성 캐릭터는 초상화 테두리·이름 강조
- 대기 캐릭터 HP가 0이 되면 쓰러짐 표시 (교체 불가)
- 스왑 쿨다운은 활성 캐릭터 초상화에 쿨다운 오버레이로 표시

### 7.2 관련 UI 클래스

| 클래스 (신규) | 역할 |
|--------------|------|
| `UI_PartyHUD` | 파티 전체 HUD 패널 |
| `UI_PartySlot` | 개별 캐릭터 슬롯 (초상화, HP, 스킬 게이지) |
| `UI_SwapCooldown` | 교체 쿨다운 오버레이 |

`UIManager.CanvasLayer.HUD`에 배치.  
`PartyManager.OnSwapStarted/OnSwapCompleted` 이벤트를 구독하여 갱신.

---

## 8. 데이터 아키텍처

### 8.1 파티 구성 SO

```
Assets/10.Datas/Party/
├── PartySlotDataSO_Bokusei.asset
├── PartySlotDataSO_Honoka.asset
└── PartySlotDataSO_LianLian.asset
```

### 8.2 파티 초기 구성 SO

```csharp
// Assets/10.Datas/Party/PartyConfigSO.cs
[CreateAssetMenu]
public class PartyConfigSO : ScriptableObject
{
    public List<PartySlotDataSO> defaultParty;  // 게임 시작 시 기본 파티
    public int startActiveIndex;
}
```

씬별 파티 구성은 `StoryManager` 또는 씬 데이터에서 `PartyManager.ConfigureParty(config)` 호출.

### 8.3 CharacterActorType 추가 (필요 시)

현재 `CharacterActorType` enum에 `LianLian`이 없고 `Reine`으로 매핑되어 있을 수 있음.  
캐릭터 이름 정책 확정 후 enum 값 추가 또는 `Reine → LianLian` 이름 변경 검토.

---

## 9. 기존 시스템 영향 분석

| 시스템 | 변경 필요 여부 | 변경 내용 |
|--------|--------------|-----------|
| `GameObjectManager` | ✅ 수정 | `_partyActors` 추가, `_player`를 활성 캐릭터로 갱신 |
| `GameManager` | ✅ 수정 | `PartyManager` 초기화 순서 추가 (GameObjectManager 직후) |
| `PlayerActor` | ✅ 수정 | `PlayerSwapBehaviour` 컴포넌트 참조 추가 |
| `InputManager` | ✅ 수정 | Party 액션 맵 추가 |
| `UIManager` | ✅ 수정 | `UI_PartyHUD` 초기화 추가 |
| `CameraManager` | ⚠️ 검토 | 타깃 캐릭터 교체 API가 있으면 연동, 없으면 추가 |
| `EnemyCombat` / `EnemyDetection` | ⚠️ 검토 | 대기 캐릭터가 무적 레이어에 있으면 자동 무시됨 (레이어 기반이면 OK) |
| `EventManager` | ❌ 변경 없음 | 이벤트 버스로 교체 이벤트 전파 (기존 구조 활용) |
| `InventoryManager` | ❌ 변경 없음 | 파티 전체 공유 인벤토리 유지 |

---

## 10. 구현 단계 (Phase)

### Phase 1 — 코어 스왑 ✅ 완료

| 항목 | 파일 | 상태 |
|------|------|------|
| `PartyManager` 구현 | `Assets/02.Scripts/Manager/Party/PartyManager.cs` | ✅ |
| `PartyConfigSO` 구현 | `Assets/02.Scripts/Data/Party/PartyConfigSO.cs` | ✅ |
| `PlayerSwapBehaviour` 구현 | `Assets/02.Scripts/GameActor/Component/Player/PlayerSwapBehaviour.cs` | ✅ |
| `PlayerActor` 입력 on/off + 어시스트 큐 | `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.cs` | ✅ |
| `GameObjectManager.SetActivePartyPlayer()` | `Assets/02.Scripts/Manager/Object/GameObjectManager.cs` | ✅ |
| `GameManager`에 `PartyManager` 등록 | `Assets/02.Scripts/Manager/GameManager.cs` | ✅ |
| SwapNext(Q)/SwapPrev(E) 입력 액션 | `Assets/Resources/Input/PlayerInputActions.inputactions` | ✅ |
| `InputDefine` 상수 추가 | `Assets/02.Scripts/Input/InputDefine.cs` | ✅ |
| InputBuffer에 Swap 액션 등록 | `Assets/02.Scripts/Manager/Input/InputManager.Event.cs` | ✅ |
| 교체 어시스트 (PerfectDodgeWindow 재활용) | `PartyManager` + `PlayerActor.QueueSwapAssist()` | ✅ |
| 대기 캐릭터 HP 0 시 교체 불가 | `PartyManager.RequestSwapTo()` | ✅ |
| 쿨다운 중 입력 버퍼링 (OnUpdate 재시도) | `PartyManager.OnUpdate()` | ✅ |

**씬 셋업 필요 사항 (유니티 에디터):**
1. `Assets/Resources/Data/PartyConfig.asset` 생성 (Create → UPlayGround/Party/Party Config)
2. `partyOrder` 에 `[Bokusei, Honoka, Reine]` 등 원하는 순서 지정
3. 각 PlayerActor 프리팹에 `PlayerSwapBehaviour` 컴포넌트 부착

### Phase 2 — 상태 안정화
1. 대기 캐릭터 무적 처리 (Physics Layer 전환)
2. 카메라 타깃 교체 연동
3. 락온 타깃 유지

### Phase 3 — 비주얼 폴리싱
1. 교체 아웃·인 애니메이션 + VFX
2. `UI_PartyHUD` (HP/스킬 게이지 + 쿨다운 오버레이)

### Phase 4 — 게임플레이 확장 (추후)
- 교체 어시스트 전용 AnimKey / 공격 데이터 교체 (현재: 일반 Attack 재활용)
- 연계 스킬 (특정 캐릭터 조합 보너스)
- 대기 캐릭터 패시브 효과

---

## 11. 확정된 설계 결정

| # | 결정 내용 |
|---|-----------|
| 1 | 대기 캐릭터 체력 회복 **없음** |
| 2 | 교체 어시스트 **구현** — PerfectDodgeWindow 타이밍에 교체 시 incoming 캐릭터 공격 자동 발동 |
| 3 | 파티 구성 **씬 고정** (PartyConfigSO로 데이터 제어, 로비 편성 없음) |
| 4 | `CharacterActorType` enum 정책 — **사용자 직접 수정** (Reine/LianLian 네이밍 포함) |
| 5 | 쿨다운 중 입력 버퍼링 **지원** — InputBuffer + OnUpdate 재시도 |
| 6 | HP 0 대기 캐릭터 교체 **불가**, 부활 **없음** |

---

## 12. 아키텍처 재설계: 단일 PlayerActor + Model 교체 방식

> 작성일: 2026-04-26  
> Phase 1의 "다중 PlayerActor" 방식에서 "단일 PlayerActor + Model 서브루트 교체" 방식으로 전환한다.

### 12.1 변경 전/후 비교

| 구분 | Phase 1 (다중 Actor) | Phase 2 (단일 Actor) |
|------|---------------------|---------------------|
| 씬의 PlayerActor 수 | 캐릭터마다 1개 (최대 3) | 1개 고정 |
| 교체 메커니즘 | PlayerActor 전체 enable/disable | Model 자식 SetActive 전환 |
| 컴포넌트 공유 | 각 Actor 독립 | 단일 PlayerActor에 부착, 교체 시 갱신 |
| 위치·회전 전달 | outgoing → incoming motor 위치 복사 | 불필요 (root 고정) |
| KCC / 물리 | 활성 Actor만 enable | 항상 enable (단일 root) |

### 12.2 씬 계층 구조

```
Player (PlayerActor, 단일 고정)
├── Model_Bokusei  (CharacterModelData) ← 활성 시 SetActive(true)
│   ├── Armature
│   └── [SkinnedMeshRenderers, MagicaCloth2...]
├── Model_Honoka   (CharacterModelData) ← 비활성 시 SetActive(false)
│   ├── Armature
│   └── [SkinnedMeshRenderers, MagicaCloth2...]
├── Model_LianLian (CharacterModelData)
│   ├── Armature
│   └── [SkinnedMeshRenderers, MagicaCloth2...]
├── Weapon
└── Magica cloth2 (공유 천 시뮬레이션이 있다면)
```

### 12.3 컴포넌트 변경 필요성 분류

#### 변경 불필요 (공통 — 단일 root에 그대로 유지)

| 컴포넌트 / 필드 | 이유 |
|----------------|------|
| `PlayerMovementController` | 이동·물리는 캐릭터와 무관 |
| `KinematicCharacterMotor` | root 고정이므로 항상 동작 |
| Input 등록/해제 | PlayerActor.OnEnable/OnDisable 그대로 |
| `PlayerSkillGauge` (컴포넌트 자체) | 구조는 유지, 값만 캐릭터별 저장·복원 |
| `_actorType` | 항상 `Player \| Combat` |
| Camera / LockOn 타깃 | 항상 동일한 root transform |

#### 변경 필요 (캐릭터별 — 교체 시 갱신)

| 컴포넌트 / 필드 | 변경 내용 | 갱신 방법 |
|----------------|-----------|-----------|
| `GameActor._characterActorType` | 현재 활성 캐릭터 타입 | 직접 대입 |
| `PlayerActorAnimator._playerActorAnimationMotionSet` | 캐릭터별 애니메이션 세트 SO | `CharacterModelData`에서 주입 |
| `ActorAnimator._animator` (AnimancerComponent) | 활성 Model의 AnimancerComponent 참조 | `GetComponentInChildren` 재획득 |
| `Animator` (UnityAnimator) | Avatar가 다르면 `Rebind()` 필요 | CharacterModelData에서 Animator 제공 |
| `PlayerCombat._attackData` | 캐릭터별 공격 데이터 SO | `CharacterModelData`에서 주입 |
| `PlayerEquipment` 무기 타입 + Constraint 본 | 캐릭터마다 장착 무기 다름 | `CharacterModelData`에서 Constraint 제공 |
| `GameActor._socketDict` | 소켓 Transform = Model 내부 본 | `CharacterModelData`에서 소켓 맵 제공 |
| `ActorColorChanger` | 렌더러 목록이 Model에 종속 | `InitializeRendererData()` 재호출 |
| `DissolveController` | 렌더러 목록이 Model에 종속 | `InitializeRendererData()` 재호출 |
| `FootIKController` | Animator, 발·골반 본이 Model에 종속 | `Refresh(Animator, hip, lFoot, rFoot)` |

#### 저장·복원 필요 (캐릭터별 독립 상태)

| 상태 | 저장 시점 | 복원 시점 |
|------|-----------|-----------|
| `_currentHealth` | 교체 직전 (outgoing) | 교체 직후 (incoming) |
| `_maxHealth` | — | `CharacterModelData.maxHealth` 로 갱신 |
| `PlayerSkillGauge` 게이지 값 | 교체 직전 | 교체 직후 |
| 콤보 진행도 | 교체 시 리셋 (설계 결정 #5와 동일) | — |

---

### 12.4 신규 파일: `CharacterModelData`

각 Model 자식 루트에 부착하는 **데이터 컨테이너 컴포넌트**.  
PlayerSwapBehaviour가 이 컴포넌트를 읽어 PlayerActor를 갱신한다.

```
Assets/02.Scripts/GameActor/Component/Player/CharacterModelData.cs
```

```csharp
public class CharacterModelData : MonoBehaviour
{
    [Header("Identity")]
    public CharacterActorType characterType;

    [Header("Animation")]
    public PlayerActorAnimationMotionSet motionSet;  // AnimKey → Clip 매핑 SO
    public Animator characterAnimator;               // 이 Model의 Unity Animator

    [Header("Combat")]
    public PlayerAttackDataSO attackData;
    public WeaponType weaponType;

    [Header("Stats")]
    public float maxHealth = 100f;

    [Header("Sockets — Model 내부 본 직접 할당")]
    public Transform rightHandSocket;
    public Transform leftHandSocket;
    public Transform rightFootSocket;
    public Transform leftFootSocket;
    // ... 필요 소켓 추가

    [Header("Equipment Constraints — Model 내부 본")]
    public ParentConstraint swordConstraint;
    public ParentConstraint doubleAxeConstraint;
    // ... 무기 타입별 Constraint 추가

    [Header("Foot IK Bones")]
    public Transform hipBone;
    public Transform leftFootBone;
    public Transform rightFootBone;
}
```

---

### 12.5 수정 파일 목록 및 변경 내용

#### ① `PlayerSwapBehaviour.cs` — 전면 개편

**현재:** Model 서브루트 단일 참조 + SetActive  
**변경 후:** 전체 Model 목록 관리 + 컴포넌트 갱신 오케스트레이션

```csharp
public class PlayerSwapBehaviour : MonoBehaviour
{
    private List<CharacterModelData> _models;   // 모든 Model 자식 목록
    private CharacterModelData       _active;   // 현재 활성 Model

    // 초기화: 자식에서 CharacterModelData 전부 수집
    void Awake()  →  _models = GetComponentsInChildren<CharacterModelData>(true).ToList()

    // 교체 진입점
    public void SwapTo(CharacterActorType type)
    {
        _active?.gameObject.SetActive(false);               // 기존 Model 비활성화
        _active = _models.Find(m => m.characterType == type);
        _active.gameObject.SetActive(true);                 // 새 Model 활성화
        _playerActor.RefreshForCharacter(_active);          // 컴포넌트 갱신
    }

    // EnterStandby / EnterActive는 삭제 또는 빈 메서드 유지 (단일 Actor 방식에서 불필요)
}
```

#### ② `PlayerActor.cs` — `RefreshForCharacter()` 추가

```csharp
// PlayerActor.base 파일에 추가
private Dictionary<CharacterActorType, float> _characterHealthMap = new();
private Dictionary<CharacterActorType, float> _characterSkillMap  = new();

public void RefreshForCharacter(CharacterModelData data)
{
    // 현재 캐릭터 상태 저장
    _characterHealthMap[_characterActorType] = _currentHealth;
    _characterSkillMap[_characterActorType]  = _skillGauge.CurrentValue;

    // Identity
    _characterActorType = data.characterType;

    // Health
    _maxHealth     = data.maxHealth;
    _currentHealth = _characterHealthMap.GetValueOrDefault(data.characterType, data.maxHealth);

    // Skill Gauge
    _skillGauge.SetValue(_characterSkillMap.GetValueOrDefault(data.characterType, 0f));

    // Animation
    _playerActorAnimator.RefreshMotionSet(data.motionSet, data.characterAnimator);

    // Combat
    _combat.RefreshAttackData(data.attackData);

    // Equipment
    _equipment.RefreshForModel(data);

    // Sockets
    RefreshSockets(data);

    // Visual effects — 새 Model 렌더러로 재초기화
    _colorChanger.InitializeRendererData();  // 기존 public 메서드 재호출로 충분
    _dissolveController.RefreshRenderers();  // 인스턴스 머티리얼 해제 후 재초기화

    // Foot IK
    _footIK.Refresh(data.characterAnimator, data.hipBone, data.leftFootBone, data.rightFootBone);

    // 무기 타입별 초기 장착 처리
    _equipment.SetWeaponType(data.weaponType);

    // HP 변경 이벤트 발송 (UI 갱신)
    OnHpChanged?.Invoke(_currentHealth, _maxHealth);
}

private void RefreshSockets(CharacterModelData data)
{
    // _socketDict를 data의 소켓 Transform으로 갱신
    if (data.rightHandSocket) _socketDict[ActorSocketType.RightHand] = data.rightHandSocket;
    if (data.leftHandSocket)  _socketDict[ActorSocketType.LeftHand]  = data.leftHandSocket;
    // ... 기타 소켓
}
```

**Awake의 WeaponType 분기 제거:**  
현재 `Awake()`에 있는 `switch(_characterActorType)` → `RefreshForCharacter`에서 처리하므로 제거.

#### ③ `PlayerActorAnimator.cs` — `RefreshMotionSet()` 추가

**확정 방향:** AnimancerComponent를 각 Model로 이동.  
Avatar가 캐릭터별로 다르기 때문에 하나의 AnimancerComponent로 공유 불가.  
각 Model이 자신의 AnimancerComponent + Animator(고유 Avatar)를 소유하고,  
Model SetActive 전환만으로 AnimancerComponent도 함께 활성/비활성된다.

**씬 구조 변경:**
```
Player
├── ActorAnimator (PlayerActorAnimator) — AnimancerComponent 필드 동적 참조
├── Model_Bokusei
│   ├── AnimancerComponent  ← Bokusei Avatar
│   ├── Animator            ← Bokusei Avatar
│   └── Armature / Meshes
├── Model_Honoka
│   ├── AnimancerComponent  ← Honoka Avatar
│   └── ...
```

**`ActorAnimator.Awake()` 변경:**
```csharp
// 기존: _animator = GetComponent<AnimancerComponent>();
// 변경: 활성 Model의 AnimancerComponent를 가져옴
_animator = GetComponentInChildren<AnimancerComponent>();
```

**`PlayerActorAnimator.RefreshMotionSet()` 구현:**
```csharp
public void RefreshMotionSet(PlayerActorAnimationMotionSet newSet, AnimancerComponent newAnimancer)
{
    _playerActorAnimationMotionSet = newSet;
    _animator = newAnimancer;  // ActorAnimator의 protected 필드 직접 교체
    // 진행 중인 모션 상태 초기화
    _isPlayingMotionSet = false;
    _currentMotionSet   = null;
}
```

**`CharacterModelData` 필드 변경:**
```csharp
// 기존: public Animator characterAnimator;
// 변경:
public AnimancerComponent animancerComponent;  // 이 Model의 AnimancerComponent
```

**`PlayerActor.RefreshForCharacter()` 내 호출 변경:**
```csharp
// 기존: _playerActorAnimator.RefreshMotionSet(data.motionSet, data.characterAnimator);
// 변경:
_playerActorAnimator.RefreshMotionSet(data.motionSet, data.animancerComponent);
```

#### ④ `PlayerCombat.cs` — `RefreshAttackData()` 추가

```csharp
public void RefreshAttackData(PlayerAttackDataSO newData)
{
    _attackData = newData;
    // 진행 중인 공격 콤보 초기화
    ResetCombo();
}
```

#### ⑤ `PlayerEquipment.cs` — `RefreshForModel()` 추가

ParentConstraint의 **소스 본**이 Model 내부 본이므로, Model 교체 시 소스를 새 본으로 교체해야 한다.  
Constraint 컴포넌트 자체(무기 오브젝트에 붙은 것)는 유지, **소스 Transform만** 교체.

```csharp
public void RefreshForModel(CharacterModelData data)
{
    // Constraint 소스 본 교체
    UpdateConstraintSource(swordConstraint,     data.swordAttachBone);
    UpdateConstraintSource(doubleAxeConstraint, data.doubleAxeAttachBone);
    // ... 무기 타입별 추가

    // WeaponType 변경
    SetWeaponType(data.weaponType);
}

private void UpdateConstraintSource(ParentConstraint constraint, Transform newBone)
{
    if (constraint == null || newBone == null) return;
    var sources = new List<ConstraintSource>();
    constraint.GetSources(sources);
    if (sources.Count == 0) return;
    sources[0] = new ConstraintSource { sourceTransform = newBone, weight = sources[0].weight };
    constraint.SetSources(sources);
}
```

**`CharacterModelData`에 추가할 필드:**
```csharp
[Header("Weapon Attach Bones — Constraint 소스")]
public Transform swordAttachBone;      // 예: 오른손 본
public Transform doubleAxeAttachBone;  // 예: 오른손 본
// WeaponType에 따라 필요한 본 추가
```

#### ⑥ `ActorColorChanger.cs` — `InitializeRendererData()` public 확인

현재 `InitializeRendererData()`는 `public`이고 `GetComponentsInChildren<Renderer>()` 방식.  
Model 교체 후 재호출하면 새 Model의 렌더러를 자동 탐색하므로 **메서드 시그니처 변경 불필요**.

#### ⑦ `DissolveController.cs` — `RefreshRenderers()` 보완

`InitializeRendererData()`는 이미 `public`이고 `RefreshRenderers()` 래퍼도 존재.  
단, 재호출 시 **이전 인스턴스 머티리얼이 해제되지 않아 메모리 누수** 발생.  
`RefreshRenderers()`를 아래와 같이 보완한다.

```csharp
public void RefreshRenderers()
{
    // 1. 진행 중인 Dissolve 중단
    StopAllCoroutines();

    // 2. 기존 인스턴스 머티리얼 해제
    foreach (var mat in _instancedMaterials)
        if (mat != null) Destroy(mat);
    _instancedMaterials.Clear();

    // 3. 새 Model의 렌더러로 재초기화
    InitializeRendererData();
}
```

`PlayerActor.RefreshForCharacter()`에서는 `_dissolveController.InitializeRendererData()` 대신  
`_dissolveController.RefreshRenderers()`를 호출한다.

#### ⑧ `FootIKController.cs` — `Refresh()` 추가

```csharp
public void Refresh(Animator animator, Transform hip, Transform leftFoot, Transform rightFoot)
{
    _animator  = animator;
    _hipBone   = hip;
    _leftFoot  = leftFoot;
    _rightFoot = rightFoot;
    _initialized = false; // 다음 OnAnimatorIK에서 재초기화
}
```

#### ⑨ `PartyManager.cs` — 단일 PlayerActor 기반으로 개편

```csharp
// 변경 전: List<PlayerActor> _partyMembers
// 변경 후:
private PlayerActor _player;                     // 씬의 단일 PlayerActor
private List<CharacterActorType> _partyOrder;    // 파티 순서
private int _activeIndex;

// BuildPartyFromScene 변경
private void BuildPartyFromScene()
{
    _player = Object.FindFirstObjectByType<PlayerActor>();
    _partyOrder = _config?.partyOrder ?? new List<CharacterActorType>();
}

// RequestSwapTo 변경
public bool RequestSwapTo(int targetIndex)
{
    if (!CanSwap()) return false;
    var targetType = _partyOrder[targetIndex];
    _player.GetComponent<PlayerSwapBehaviour>().SwapTo(targetType);
    _activeIndex = targetIndex;
    ...
}

// CanSwap: ActiveCharacter → _player
public bool CanSwap() { ... _player?.PlayerController?.CurrentState ... }
```

---

### 12.6 교체 흐름 (신규)

```
[입력] SwapNext/SwapPrev
  ↓
PartyManager.RequestSwapTo(index)
  ├─ CanSwap() 실패 → 버퍼 저장, OnUpdate 재시도
  └─ CanSwap() 성공
       ↓
PlayerSwapBehaviour.SwapTo(CharacterActorType)
  ├─ [1] _active.gameObject.SetActive(false)   ← 이전 Model 비활성화
  ├─ [2] _active = 새 CharacterModelData
  ├─ [3] _active.gameObject.SetActive(true)    ← 새 Model 활성화
  └─ [4] _playerActor.RefreshForCharacter(_active)
           ├─ 이전 캐릭터 HP/스킬 저장
           ├─ _characterActorType 갱신
           ├─ HP/스킬 복원
           ├─ Animator / MotionSet 갱신
           ├─ AttackData 갱신
           ├─ Equipment(Constraint + WeaponType) 갱신
           ├─ SocketDict 갱신
           ├─ ColorChanger / DissolveController 재초기화
           └─ FootIK 재초기화
  ↓
PartyManager: NotifyActivePlayerChanged() → CameraManager, GameObjectManager
  ↓
OnSwapCompleted 이벤트 발생 → UI 갱신
```

---

### 12.7 구현 순서

| 순서 | 작업 | 파일 |
|------|------|------|
| 1 | `CharacterModelData` 신규 생성 | `Component/Player/CharacterModelData.cs` |
| 2 | `ActorAnimator` — `_animator` 초기화를 `GetComponentInChildren`으로 변경 | `Animation/ActorAnimator.cs` |
| 3 | `PlayerActorAnimator` — `RefreshMotionSet(motionSet, AnimancerComponent)` 추가 | `Animation/PlayerActorAnimator.cs` |
| 4 | `PlayerSwapBehaviour` 개편 | `Component/Player/PlayerSwapBehaviour.cs` |
| 5 | `PlayerActor` — `RefreshForCharacter` + 체력맵 추가, Awake `switch` 제거 | `Object/Player/PlayerActor.cs` |
| 6 | `PlayerCombat` — `RefreshAttackData` 추가 | `Component/Player/PlayerCombat.cs` |
| 7 | `PlayerEquipment` — `RefreshForModel` 추가 | `Component/Player/PlayerEquipment.cs` |
| 8 | `DissolveController` — `InitializeRendererData` public 변경 | `Component/Common/DissolveController.cs` |
| 9 | `FootIKController` — `Refresh` 추가 | `Component/Player/FootIKController.cs` |
| 10 | `PartyManager` 개편 | `Manager/Party/PartyManager.cs` |
| 11 | 씬 셋업: AnimancerComponent를 각 Model로 이동 + CharacterModelData 할당 | Unity Editor |

---

### 12.8 검토 결과

| # | 항목 | 결정 |
|---|------|------|
| 1 | **Animator Avatar 공유 여부** | ✅ 확정 — **공유하지 않음**. 각 Model이 고유 Avatar 보유. AnimancerComponent를 Model로 이동하여 해결. |
| 2 | **AnimancerComponent 위치** | ✅ 확정 — **각 Model로 이동**. Avatar가 다르므로 root 공유 불가. Model SetActive와 함께 자동 활성/비활성. |
| 3 | **MagicaCloth2 재시뮬레이션** | ✅ 확정 — **별도 처리 불필요**. Model이 비활성화되면 애니메이션 정지와 함께 시뮬레이션도 자동 정지. |
| 4 | **Weapon Constraint 소스** | ✅ 확정 — **Model 내부 본 사용 확인. 교체 필요.** `CharacterModelData`에 무기 부착 본 추가. `PlayerEquipment.RefreshForModel()`에서 `ParentConstraint` 소스를 새 본으로 교체. |
| 5 | **DissolveController 머티리얼 인스턴스 해제** | ✅ 확정 — **재초기화 필요.** `InitializeRendererData()`는 이미 public이나, 재호출 전 `_instancedMaterials` 해제 + 진행 중 Dissolve 중단 로직을 `RefreshRenderers()`에 추가. |
