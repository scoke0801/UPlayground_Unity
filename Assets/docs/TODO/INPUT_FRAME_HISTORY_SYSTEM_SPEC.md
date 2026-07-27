# InputFrame 이력 기반 입력 시스템 고도화 스펙

> 작성일: 2026-07-26
> 개정: 2026-07-26 (Advisor 검토 반영 — §0 참조)
> 대상 버전: Unity 6 (6000.0.60f1), Input System 1.14.2, URP
> 분류: TODO 구현 스펙
> 적용 범위: 게임플레이 입력 샘플링·이력·선입력 판정·입력 녹화, 입력 계층 부수 개선
> 관련 문서:
>
> - `Assets/docs/Complete/INPUT_SYSTEM_GUIDE.md`
> - `Assets/docs/TODO/GAMEPAD_UI_INPUT_REBINDING_SYSTEM_SPEC.md` (§9 조합키 중재 — 본 스펙이 그 위에 쌓인다)
> - `Assets/docs/Complete/INPUT_CANCEL_WINDOW_EXPLICIT_AUTHORING_DESIGN.md`
> - `Assets/docs/guide/INPUT_KEYMAP_REFERENCE.md`
>
> 관련 코드:
>
> - `Assets/02.Scripts/Data/Input/` (InputBuffer, InputChordArbiter, InputDefine, ComboInputTracker)
> - `Assets/02.Scripts/Manager/Input/InputManager*.cs`
> - `Assets/02.Scripts/Contracts/GameServices.cs` (`IInputService`)
> - `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Input.cs`, `.Combat.cs`, `.Lifecycle.cs`
> - `Assets/02.Scripts/GameActor/State/Player/PlayerAttackState.cs`, `PlayerAttackInputArbiter.cs`, `PlayerInterruptResolver.cs`
> - `Assets/02.Scripts/GameActor/Component/Player/PlayerCombat.Combo.cs`
> - `Assets/02.Scripts/GameActor/Base/GameActor.cs` (`LocalTimeScale`)
> - `Assets/02.Scripts/Manager/Handler/Combat/GameHitStopHandler.cs`, `Manager/KCCSimulator.cs`
> - `Assets/02.Scripts/Tool/PlayerControlFeelDebugHUD.cs` (F9 조작감 HUD — 본 스펙의 주요 산출물)
> - `Assets/02.Scripts/MovementController/ActorMovementController.cs`
> - `Assets/Resources/Input/PlayerInputActions.inputactions`

## 구현 진행 상태

- 미구현. 설계 확정 전 스펙이다.
- **§4(시간축 통일)는 초안의 전제가 사실과 반대임이 확인되어 폐기되었다.** §0.1 참조.
- 착수 순서는 §8을 따른다. **Phase 0부터 시작한다.**

---

## 0. 개정 이력 — Advisor 검토 결과

초안(2026-07-26)을 Advisor가 검토하고, 지적 전건을 실제 코드로 교차검증했다.
초안의 **핵심 전제 하나가 사실과 반대**였고, **회귀를 유발하는 삭제 제안 하나**가 있었다.
아래는 검증까지 완료한 확정 사항이다.

### 0.1 [폐기] 초안 §4 "시간축 통일" — 전제가 사실과 반대였다

초안은 "히트스톱이 `Time.timeScale`로 구현되므로 scaled 축에서 버퍼 만료가 멈추고,
이를 `SetExpiryPaused`로 우회하고 있다"고 주장했다. **틀렸다.**

검증: 히트스톱은 `GameActor.LocalTimeScale`(`GameActor.cs:87-105`)로 구현되며,
setter는 `_animator.Speed`만 바꾼다. `DeltaTime => Time.deltaTime * _localTimeScale`도
액터 로컬 값이다. **`Time.time`도 `Time.unscaledTime`도 멈추지 않는다.**
전역 `Time.timeScale` 대입은 `GameTimeManager.cs:231,302` 두 곳뿐이며 별도 경로다.
`GameHitStopHandler`의 플레이어 경로는 `// timeScale은 건드리지 않고 Actor-only 슬로우만 적용`.

결정적으로 `PlayerAttackState.cs:447-453`이 이미 정확한 반대 사실을 문서화해 두었다:

```csharp
// 추가: 공격 중 피격 히트스톱(LocalTimeScale freeze)은 애니메이션(=콤보 윈도우)만 얼리고
// InputBuffer는 실시간(Time.time) 기준으로 계속 만료된다. 공격 상태는 하이퍼아머라 피격에도
// 유지되므로, 콜리전 OFF 구간(콤보 윈도우)에서 프리즈가 걸리면 선입력한 다음 콤보가 프리즈
// 도중 만료돼 씹힌다. 프리즈 동안에도 만료를 정지해 보존한다(...)
bool hitStopFrozen = gameActor.LocalTimeScale < 1f;
Svc.Input.InputBuffer.SetExpiryPaused(_combat.IsPossibleCollide || hitStopFrozen);
```

즉 `SetExpiryPaused`는 "잘못된 시간축의 우회"가 아니라 **애니메이션 시간과 실시간의
괴리에 대한 정면 대응**이다. `unscaledTime` 전환은 이를 개선하지 못한다(두 축 모두
`LocalTimeScale`에 영향받지 않으므로 동작이 **동일**하다). 오히려 전역 timeScale 경로가
걸린 경우에는 만료를 **가속**시켜 씹힘을 늘린다.

또한 `SetExpiryPaused`의 다른 절반인 `_combat.IsPossibleCollide`는 시간축 문제가 아니다.
"캔슬 불가 구간 동안 만료 정지"는 유효 창을 **액티브 히트 길이만큼 동적 연장**한다.
초안 §4가 제안한 "직전 N초 내 press 질의"는 **고정 창이라 등가가 아니다.**
액티브 히트가 0.3s면 그 시작에 누른 입력은 캔슬창 개방 시 0.24s 창에서 탈락한다 —
**초안 §4를 그대로 구현하면 지금 잘 되는 선입력이 씹힌다.**

→ **§4를 폐기한다.** 이력은 `unscaledTime`으로 기록하되(§3.3), "우회 코드 제거"라는
초안의 이득 주장은 전부 철회한다. `SetExpiryPaused`에 상당하는 상태 추적은 어떤 모델에서도
남는다. 개별 결함은 §8의 Phase 3에서 이력 데이터를 근거로 하나씩 다룬다.

### 0.2 [수정] `PhysicalTime` 보존 — Advisor가 지목한 대상은 틀렸으나 결함은 실재한다

Advisor는 `ToBufferTimestamp` 삭제가 "Dodge/SkillUltimate/QuickSlot 4종의 선입력 창을
붕괴시킨다"고 지적했다. **결함은 실재하지만 대상이 반대다.**

`InputChordArbiter`의 판정 규칙(클래스 주석):
> 조합 후보 컨트롤의 **단일 액션**은 `GraceSeconds` 동안 provisional로 보류한다.

즉 grace 지연을 받는 것은 조합 소유자(Dodge 등)가 아니라 **조합 트리거 컨트롤을 공유하는
단일 액션**이다. `.inputactions` 실측 결과:

| 조합 (소유자) | modifier + trigger | 같은 trigger의 단일 액션 | 그 액션의 버퍼 창 |
|---|---|---|---|
| Dodge | LB + RB | **ElementBuff** (RB) | 0.20s |
| SkillUltimate | LB + RT | **Interact** (RT) | 0.15s (수동 추가) |
| QuickSlot_Up | LB + dpad↑ | **CharacterSwap_1** | 0.15s |
| QuickSlot_Right | LB + dpad→ | **CharacterSwap_2** | 0.15s |
| QuickSlot_Down | LB + dpad↓ | **CharacterSwap_3** | 0.15s |
| QuickSlot_Left | LB + dpad← | **CharacterSwap_4** | 0.15s |

grace = `InputChordArbiter.DefaultGraceSeconds` = **0.12s**. 따라서
`ToBufferTimestamp`가 없으면 실효 버퍼 창은:

- `CharacterSwap_1~4`: 0.15 − 0.12 = **0.03s (60fps에서 약 2프레임)**
- `ElementBuff`: 0.20 − 0.12 = **0.08s**
- `Interact`: 0.15 − 0.12 = **0.03s**

**6개 조합 전부 게임패드 전용 바인딩이다**(`<Gamepad>/leftShoulder` modifier).
따라서 이 회귀는 **게임패드에서만** 나타나며, 키보드 위주로 개발·테스트하면 발견되지 않는다.
캐릭터 스왑은 이 게임의 핵심 조작이라 영향이 크다.

→ **`InputFrame`에 `PhysicalTime` 필드를 필수로 추가하고, 모든 창 판정은 `PhysicalTime`
기준으로 한다**(§3.3, §5.2). 초안의 "`ToBufferTimestamp` 불필요" 주장을 철회한다.
축 변환은 사라지지만 **물리 입력 시각 보존 자체는 반드시 이관한다.**

### 0.3 그 외 확정 반영

| 지적 | 검증 | 반영 |
|---|---|---|
| 샘플러 실행 순서 미보장 | `ProjectSettings/MonoManager.asset` 부재 확인. 소비 측은 별도 MonoBehaviour(`ActorMovementController.Update`) | §5.1 — `[DefaultExecutionOrder]` 확정 (코드베이스 선례 있음) |
| §8.2 유지 API 목록 불완전 | `Clear()` 호출부 **7곳**(에디터 2곳 포함) 확인 | §8.2 전면 개정 + **Phase 0 신설**, Phase 2 위험 "중" |
| H4는 20줄로 해소 가능 | `CleanExpiredInputs`의 `new Queue` 등 확인 | **Phase 0** 신설 |
| `HeldDuration`의 release 유실 | `OnInputEventCanceled`도 `PassesInputGates` 통과 필요 확인 | §5.1 함정 추가 |
| `_chargeHoldTime`은 scaled | `PlayerActor.Lifecycle.cs` `+= Time.deltaTime` 확인 | §5.3 — Phase 3 귀속 명기 |
| §9.3 근거 오류 | 델리게이트 `==`는 값 동일성(Target+Method). Register/UnRegister 15/15 대칭, 누수 없음 | **§9.3 스킵으로 강등** |
| 소비 지점 과소평가 | synthetic canceled, PartyManager 이중 경로, 액터 간 InputBuffer 공유 확인 | §1.2 H3 확장 |
| 이중 진실 소스 관측 불가 | `SuppressPlayerActionInputBriefly`는 버퍼만, `ClearAllInputState`는 플래그만 초기화 | §2 + §6.2 HUD에 3열 표시 |
| 액션 29개 (30 아님) | `InputDefine` 및 `.inputactions` 양쪽 29개 일치 | §3.1 정정 + `maskVersion` |
| 조합키 × 이력 미정의 | modifier는 액션이 아니라 컨트롤 폴링(`IsProbeControlPressed`) | §3.1 절 추가 + T11·T12 |
| `LinkWindow`는 데이터 주도 | `PlayerCombat.Combo.cs:256`. **에셋 34개 실측 전부 1.0** | §3.4 근거 교체 (실측값 명기) |
| §9.2 `Look` 매 프레임 RaycastAll | `Look`은 **Value 타입 + `<Mouse>/delta`** → 마우스 이동 중 매 프레임 | **§9.2를 §9 최우선으로 승격** |
| 결정론 근거에서 KCC 오지목 | `KCCSimulator`가 `AutoSimulation=false` + 고정 dt로 통제 | §6.3 근거 교체 |
| 방향 모션 입력 과설계 | — | **매처·패턴·테스트 삭제.** `Move`/`MoveDir9` 기록까지만 |
| §9.4 억제 태그 스택 중복 | — | **스킵** |

### 0.4 채택하지 않은 지적 1건

Advisor는 **녹화(`InputTrace`)까지 삭제**하고 F9 HUD만 남길 것을 권고했다.
1인 개발 기준으로 일리 있으나, **입력 녹화는 이 작업의 원래 요구사항이므로 유지한다.**
대신 Advisor 지적의 실질(에디터 타임라인 뷰가 비용의 대부분)을 받아들여
**에디터 타임라인 뷰와 주입 재생을 Phase 4(선택)로 격하**하고,
녹화 자체는 Phase 2의 독립 산출물로 둔다. §6·§8 참조.

---

## 1. 목적과 배경

### 1.1 현재 구조

```
Unity Input System
 → InputManager 콜백 게이트 (rebind / suppression / pointer-over-UI)
 → InputChordArbiter (grace 0.12s 조합키 중재, unscaledTime)
 → dispatch ─┬→ InputBuffer (Queue<BufferedInput>, string 키, Time.time)
             └→ PlayerActor 개별 InputCondition 플래그
 + ComboInputTracker (발동 확정 시 ComboInputToken push)
```

조합키 중재, 물리 입력 시각 보존(`ToBufferTimestamp`), 타임스탬프 기반 약/강 중재
(`PlayerAttackInputArbiter`), 만료 정지(`SetExpiryPaused`)는 **모두 실제 결함에 대한
정확한 대응이며 유지 대상이다.** §0.1·§0.2에서 확인했듯 초안은 이 중 둘을 오진했다.

**본 스펙은 판정 규칙을 바꾸지 않는다. 관측 불가능성(H1)을 해소하고 비용(H4)을 없애는 것이다.**

### 1.2 자료구조 차원의 한계

**H1. 입력 이력이 존재하지 않는다.** (← 본 스펙의 주 대상)
`InputBuffer`는 *미소비 대기 큐*다. 소비되거나 만료되면 사라진다. 결과:

- "선입력이 씹혔다"를 사후 검증할 수단이 없다. press 시각과 발동 시각의 차이를 계산할
  데이터가 남지 않는다.
- 저스트가드·도지카운터 창 폭을 실측으로 튜닝할 수 없다. 현재는 감으로 조정한다.
- 방향 입력 이력이 전혀 없다.

이 한계는 이미 코드에 드러나 있다. `Tool/PlayerControlFeelDebugHUD.cs`(F9, 426행)는
조작감 문제 분류용 HUD인데 `AppendInputBuffer`가 `GetSnapshot()`으로 **대기 중인 입력만**
나열한다. 즉 *"지금 무엇이 대기 중인가"는 보이지만 **"방금 누른 게 왜 안 나갔는가"는
볼 수 없다.*** 이력 모델이 필요한 가장 직접적인 증거이며 §6.2의 확장 지점이다.

**H2. (철회)** 초안의 시간축 진단은 전제가 틀렸다. §0.1 참조.
남는 사실만 기록한다: 이력은 `unscaledTime`으로 기록하고, 창 판정은 `PhysicalTime`
기준으로 한다(§0.2). 기존 `Time.time` 기반 `InputBuffer`는 Phase 3까지 그대로 둔다.

**H3. 소비 주체가 분산되어 우선순위 규칙이 흩어진다.**
초안은 4곳으로 봤으나 실제는 최소 8개 계열이다:

1. `PlayerActor.Input.cs:99` — performed에서 HeavyAttack 제거
2. `PlayerActor.Input.cs:113` — canceled에서 짧은 누름이면 **과거 시점 press 합성 추가**
3. `PlayerAttackInputArbiter` — 약/강 승자 소비
4. `InputManager.Chord.cs:142-143` — **synthetic canceled가 버퍼를 소비**
5. `PartyManager` — CharacterSwap 4종을 OnUpdate 폴링과 등록 콜백 **양쪽에서** 처리
6. `PlayerInterruptResolver` — Guard는 버퍼가 아니라 `InputCondition` 플래그로 판정
7. State 계층 — `HasInput`/`ConsumeInput` 직접 호출 다수
8. `Clear()` 7곳 — `InputManager`(2), `PlayerActor`(2), 카메라 어댑터(1), **에디터(2)**

> **가장 중요한 함정:** `ActorMovementController.cs:115`의 주석이 밝히듯
> **`InputBuffer`는 파티 전 액터가 공유하는 단일 인스턴스**이며, 소비 배타성을
> `Motor.enabled`로 보장하고 있다. 소비 원장 모델에서 이 배타성은 명시적 플래그 검사로만
> 유지된다. 누락하면 **두 캐릭터가 같은 입력을 각각 소비**한다.

**H4. 비용.** `ConsumeInput`은 큐를 2회 순회하고, `CleanExpiredInputs`는 매 호출마다
`new Queue<BufferedInput>()`을 할당한다(`HasInput`/`PeekInput`/`Count`/`GetSnapshot`
전부가 호출). 키는 문자열 비교다.
→ **이것은 이력 모델과 무관하게 `InputBuffer.cs` 내부 수정만으로 해소된다(Phase 0).**

### 1.3 핵심 전환

> **버퍼에서 제거하지 않는다. 소비/무효화 표시만 한다. 만료는 링 오버라이트가 처리한다.**

이미 소비된 입력도 이력에 남으므로 사후 분석이 가능해진다. 이것이 H1의 해법이다.
H4는 Phase 0이 별도로 해결하므로, **이력 모델의 정당성은 H1(+H3 관측)에만 걸려 있다.**

---

## 2. 계층 구조

```
L0 Device    Unity Input System (변경 없음)
L1 Sampler   프레임당 1회 논리 입력을 InputFrame으로 스냅샷
L2 History   InputFrameHistory — 고정 크기 링버퍼, 구조체, 런타임 무할당
L3 Query     이력 위 순수 질의 (PhysicalTime 기준 창 판정)
L4 Ledger    소비/무효화 원장 (frameIndex + action → 2비트)
L5 Sink      InputManager 콜백 디스패치 · InputBuffer · ComboInputTracker (기존 유지)
```

L5는 그대로 둔다. 콜백 경로를 이력 폴링으로 대체하려는 시도는 하지 않는다 —
UI 레이어 게이팅·`CheckFunc`·`CancelCallback` 의미가 얽혀 있어 교체 비용이 이득을 초과한다.

> **이중 진실 소스를 인식하고 관측 가능하게 만든다.**
> 이력은 진실 소스가 아니라 **관측 기반**이며, `InputCondition` 플래그·`InputBuffer` 잔량과
> 어긋날 수 있다. 실제로 두 상태는 서로 다른 진입점에서 독립적으로 초기화된다 —
> `SuppressPlayerActionInputBriefly`는 **버퍼만** 비우고(`InputManager.cs:208`) 플래그는 남기며,
> `ClearAllInputState`는 **플래그만** 지운다(`PlayerActor.Input.cs:195`).
> 따라서 §6.2 HUD는 **이력 / `InputCondition` 플래그 / 버퍼 잔량 3열을 나란히** 표시해야 하고,
> **세 값이 어긋난 순간이 정확히 조사 대상**이다. 이력만으로 답하려 하면 안 된다.

### 2.1 asmdef 배치

`UPlayGround.Data`의 references는 `UPlayGround.Ability.Core`, `UPlayGround.Core`,
`AYellowpaper.SerializedCollections`뿐이며 **Unity Input System을 참조하지 않는다**(확인).
`noEngineReferences`가 없으므로 `UnityEngine`(→`Vector2`)은 사용 가능하다.
`InputChordArbiter`가 Unity Input System 없이 작성된 선례를 따른다.

| 요소 | 위치 | asmdef | 근거 |
|---|---|---|---|
| `InputFrame`, `ActionMask` | `Data/Input/` | `UPlayGround.Data` | 순수 데이터 |
| `InputFrameHistory`, 질의·원장 | `Data/Input/` | `UPlayGround.Data` | 순수 로직 → EditMode 단독 검증 |
| `InputFrameSampler` | `Manager/Input/` | Assembly-CSharp | `InputAction` 접촉 |
| `InputTrace` 직렬화 | `Data/Input/` | `UPlayGround.Data` | 순수 데이터 |

실질 이득: **링버퍼와 질의 로직 전체가 Unity Input System 없이 EditMode에서 검증 가능하다.**

---

## 3. 자료구조

### 3.1 `ActionMask`

```csharp
[Flags]
public enum ActionMask : ulong
{
    None        = 0,
    Move        = 1UL << 0,
    // ... InputDefine.PlayerAction과 1:1
    Attack      = 1UL << 9,
    HeavyAttack = 1UL << 10,

    AnyAttack   = Attack | HeavyAttack,   // TryGetMostRecent 실사용처 있음
}
```

`InputDefine.PlayerAction` 상수는 **29개**이며 `.inputactions`의 PlayerAction 맵도
정확히 29개다(2026-07-26 실측 일치. UI 25, Gamepad 11, System 4는 이력 대상 아님).
`ulong`에 35비트 여유가 있다.

**문자열 상수를 제거하지 않는다.** 리바인딩·글리프·프로필 마이그레이션이 전부 문자열
기반이다. `ActionMask`는 병행 표현이며 양방향 매핑 테이블을 둔다
(`ActionMaskMap.FromName` / `ToName`).

> **함정 1 — 수동 동기화.** `InputDefine.PlayerAction`에 액션을 추가하면 `ActionMask`도
> 갱신해야 한다. 누락 시 `FromName`이 `None`을 반환해 조용히 판정에서 빠진다.
> EditMode 테스트로 **전단사와 개수를 단언**한다(T1). 총 비트가 63에 도달하면
> 사전 경고하도록 상한 단언을 포함한다.
>
> **함정 2 — 비트 배정은 영구적이다.** `ActionMask` 비트는 `InputTrace`로 **디스크에
> 직렬화**된다(§6.1). 한 번 부여한 비트 번호는 **재사용·재정렬하지 않는다.**
> 액션이 제거되면 그 비트는 결번으로 남긴다. `InputTrace`에 `maskVersion`을 넣어
> 로드 시 불일치를 감지한다. 예시가 `InputDefine` 선언 순서와 같아 재정렬 유혹이 크다.
>
> **함정 3 — 사용처 없는 조합 상수를 미리 만들지 않는다.** 함정 1의 동기화 표면적을
> 늘린다. `AnyAttack`만 두고 `AnySwap` 등은 필요 시 추가한다.

#### 3.1.1 조합키와 이력의 관계 (신설)

조합키 액션은 액션 이름으로 해소되므로(`RegisterChord(map, action, modifier, trigger)`)
`ActionMask` 단일 비트로 표현된다. 다만:

- **modifier 컨트롤은 액션이 아니다.** `IsProbeControlPressed`(`Chord.cs:66-70`)가 장치
  컨트롤을 직접 폴링한다. 따라서 이력만으로는 "LB+RB를 눌렀는데 왜 Dodge가 아니라
  ElementBuff가 나갔나"를 답할 수 없다 — modifier 상태가 이력에 없다.
- **provisional 보류 상태와 `IsSynthetic` canceled도 이력에 자리가 없다.**

→ **이력에는 확정된 액션만 담는다.** 조합 오판 진단은 §9.2 진단 링에
`IsSynthetic`·provisional 사유와 함께 기록하는 것으로 한정한다.

### 3.2 방향 기록 — 매처는 만들지 않는다

`InputFrame`에 `Move`(Vector2)와 `MoveDir9`(8방향+뉴트럴 양자화)를 **기록만** 한다.

```csharp
public enum Dir9 : byte { None = 0, DownLeft = 1, Down = 2, DownRight = 3,
    Left = 4, Neutral = 5, Right = 6, UpLeft = 7, Up = 8, UpRight = 9 }
```

`None`(입력 채널 자체가 없음)과 `Neutral`(입력은 살아 있고 스틱 중앙)을 구분한다.

> **초안의 `MotionPattern`/`MotionStep`/`MatchMotion` 매처는 삭제했다.**
> 근거: (a) 방향 모션 입력이 3D TPS에서 직관적인지 자체가 미검증 디자인 가설이다.
> (b) 실제로 원하는 것이 44/66 더블탭이면 `WasPressedWithin` 두 번 호출로 끝나며
> `MotionPattern`이 필요 없다. (c) 이력에 방향이 남아 있으면 **나중에 언제든 매처를
> 얹을 수 있다** — 지금 설계할 이유가 없다.
>
> **함정:** `MoveDir9` 양자화 임계값(데드존·대각 각도)은 데이터로 노출한다.
> 하드코딩하면 패드/키보드 간 결과가 갈린다. 샘플러는 Input System processor를 이미
> 통과한 값을 받으므로 **데드존 이중 적용에 주의**한다.

### 3.3 `InputFrame`

```csharp
public readonly struct InputFrame
{
    public readonly int        Index;        // 단조 증가
    public readonly float      Time;         // 샘플 시각 (unscaledTime)
    public readonly float      PhysicalTime; // 원래 물리 입력 시각 — 창 판정의 기준 (§0.2)
    public readonly float      DeltaTime;    // unscaled
    public readonly ActionMask Pressed;      // edge
    public readonly ActionMask Held;         // level — IsPressed() 폴링이 진실 소스
    public readonly ActionMask Released;     // edge (유실 가능 — §5.1 함정)
    public readonly Vector2    Move, Look;
    public readonly Dir9       MoveDir9;
    public readonly byte       Device;       // ActiveInputDevice
    public readonly byte       Layer;        // InputLayer 압축 — 억제 사후 판별용
}
```

패딩 포함 **72바이트**. 256프레임 = 18.4KB 사전 할당, 런타임 무할당.

- **`PhysicalTime`이 이 구조체의 핵심이다.** §0.2에서 확인했듯 이것 없이는 게임패드
  캐릭터 스왑 선입력 창이 0.15s → 0.03s로 붕괴한다. 프레임에 여러 press가 섞이면
  액션별 `PhysicalTime`이 달라지므로, **정밀 판정이 필요한 액션은 별도 병렬 배열
  `float[] physicalTimePerAction`을 두거나 press를 개별 엔트리로 분리**한다.
  구현 착수 시 둘 중 하나를 확정한다(단일 필드로는 부족하다).
- `Layer`를 넣는 이유: "왜 무시됐나"의 가장 흔한 답이 "UI 레이어가 올라가 있었다"다.
- `Vector2`를 쓴다. `Unity.Mathematics`는 autoReferenced 패키지라 `overrideReferences`가
  없는 `UPlayGround.Data`에서도 사용 가능할 것으로 보이나 **`Data` 폴더 내 실사용 0건으로
  미검증**이다. 절약분 4KB가 검증 비용에 미치지 못한다.

### 3.4 `InputFrameHistory`

```csharp
public sealed class InputFrameHistory
{
    private readonly InputFrame[] _frames;
    private readonly ulong[]      _consumed;     // Consumed 비트
    private readonly ulong[]      _invalidated;  // Invalidated 비트 (§8.2 Clear 대응)
    private int _head, _count, _nextIndex;

    public bool TryGetBack(int backOffset, out InputFrame frame);  // 0 = 최신
    public bool TryGet(int frameIndex, out InputFrame frame);      // 오버라이트 시 false
    public void Push(in InputFrame frame);
    public bool IsConsumed(int frameIndex, ActionMask action);
    public void MarkConsumed(int frameIndex, ActionMask action);
    public void MarkInvalidated(int frameIndex, ActionMask action);
}
```

**용량 근거 (실측 기반).** `ComboInputTracker.LinkWindow`는 기본값 1.0f이지만
`PlayerCombat.Combo.cs:256`에서 `_attackData.comboLinkWindow`로 **데이터 주도 설정**된다
(`AbilitySetSO.comboLinkWindow`, `[Min(0.05f)]`, 상한 없음).
**에셋 34개 실측 결과 전부 1.0**(2026-07-26)이다. 256프레임은 240fps에서 1.07s이므로
현 데이터에서는 충분하다. 그러나 상한이 없으므로:

> 용량은 **에셋 전체 `comboLinkWindow` 최댓값 × 목표 최대 프레임레이트**로 산정한다.
> 링 오버런은 `TryGet` false로 **조용히** 나타나므로, `요청Index < LatestIndex - capacity`
> 발생 시 1회 경고 로그를 남긴다. 이 로그 없이는 용량 부족을 발견할 수 없다.

> **최대 위험 지점:** `Push`에서 오버라이트되는 슬롯의 `_consumed`·`_invalidated` 비트를
> 반드시 0으로 지운다. 누락하면 새 프레임 입력이 "이미 소비됨"으로 판정되어
> **입력이 조용히 씹힌다.** 증상이 간헐적이라 필드 추적이 거의 불가능하다. T4로 단언한다.

---

## 4. (폐기) 시간축 통일

초안 §4는 전제가 사실과 반대여서 폐기되었다. §0.1 참조.

남는 결정만 기록한다:

- 이력은 `unscaledTime`으로 기록한다(`Time`, `PhysicalTime` 모두).
- 창 판정은 `PhysicalTime` 기준이다(§0.2).
- 기존 `InputBuffer`의 `Time.time`, `SetExpiryPaused`, `ToBufferTimestamp`,
  `ComboInputTracker`의 `Time.time`은 **모두 유지한다.**
- 개별 결함(있다면)은 Phase 3에서 이력 데이터를 근거로 하나씩 다룬다.
  **일괄 축 전환은 하지 않는다.**

---

## 5. 샘플러와 질의

### 5.1 샘플링 시점 — 실행 순서를 착수 전에 확정한다

샘플러는 `InputManager.OnUpdate` 선두에서 1회 실행한다.

```csharp
public void OnUpdate()
{
    _frameSampler.Sample();   // 신규
    TickChordArbiter();
}
```

순서 근거: 중재기 tick이 grace 만료로 콜백을 디스패치하면 게임플레이 상태가 바뀔 수 있다.
이력은 그 이전의 물리 입력 상태를 기록해야 인과가 맞다.

> **함정 A — 실행 순서가 미정의다.** `GameManager`는 MonoBehaviour `Update()`에서 매니저를
> 순회하지만 소비 측은 **별도 MonoBehaviour**다(`ActorMovementController.Update`,
> `PlayerActor.Update`). 그리고 이 프로젝트에는 **`ProjectSettings/MonoManager.asset`이
> 없다**(확인) — 즉 상대 순서가 미정의다.
> 현재 구조는 순서에 무관하다(Input System 콜백이 플레이어 루프의 입력 단계에서 발화해
> 버퍼를 채우므로). 그러나 샘플러를 도입하면 `ActorMovementController.Update`가 먼저 도는
> 조합에서 상태 머신이 **1프레임 낡은 이력**을 질의한다. 증상은 "간헐적 1프레임 입력 지연"
> — 정확히 이 스펙이 없애려는 종류의 버그다.
>
> **해결: `GameManager`에 `[DefaultExecutionOrder]` 음수 값을 부여한다.** 코드베이스에
> 선례가 있다(`KCCSimulator` −99, `GamepadCoreApi` −1000). 대안은
> `InputSystem.onAfterUpdate` 훅 또는 질의 시 lazy sample이다. **Phase 1 착수 전에 확정한다.**
>
> **함정 B — `InputSettings.updateMode`.** Fixed로 설정되면 프레임당 샘플 수와
> `FixedUpdate` 횟수가 어긋난다. 현 프로젝트는 `.inputsettings.asset`이 확인되지 않아
> 기본값 Dynamic으로 추정하나 **미확인**이다. 착수 시 확인한다.

**엣지 하이브리드가 필수다.** `Held`는 `InputAction.IsPressed()` 폴링으로 얻지만,
`Pressed`/`Released`는 엣지다. 폴링 차분으로 엣지를 만들면 한 프레임 안의 press+release
(1프레임 탭)를 놓친다. 따라서 **엣지는 콜백에서 누적한 pending 마스크를 샘플러가 소비**하고
`Held`만 폴링한다.

엣지는 **dispatch 경로(중재기 통과 후)** 에서 누적한다. 게이트에서 차단된 입력을 이력에
넣으면 원인이 게이트인지 판정 로직인지 구분이 안 된다. 차단된 입력은 §9.2 진단 링에 기록한다.

> **함정 C — `Released` 엣지는 유실될 수 있다.** `OnInputEventCanceled`도
> `PassesInputGates`를 통과해야 하므로(`Event.cs:222-228`),
> `SuppressPlayerActionInputBriefly` 구간에 release가 걸리면 **canceled가 드롭된다.**
> 따라서 `HeldDuration`을 엣지 차분만으로 계산하면 **차지가 무한 홀드로 판정된다.**
> 현재 코드는 `ClearAllInputState()`가 `_chargeAttackHeld=false`로 강제 복구하는 경로가
> 있어 이 문제가 없다 — 이력으로 옮기면 그 복구가 사라진다.
> **`Held`는 반드시 `IsPressed()` 폴링을 진실 소스로 삼고 `Released`는 보조로만 쓴다.**
>
> **함정 D — pending 마스크는 새 가변 상태다.** `_chordArbiter.Reset()`,
> `RefreshInputLayer`, 억제 진입 시 **리셋을 누락하면 유령 입력**이 된다.
> 기존 리셋 지점 전부에 pending 마스크 클리어를 함께 넣는다.

### 5.2 질의 API

```csharp
public static class InputHistoryQuery
{
    // 모든 창 판정은 frame.PhysicalTime 기준 (§0.2)
    public static bool TryConsumeBuffered(this InputFrameHistory h,
        ActionMask action, float window, float now, out InputFrame frame);
    public static bool WasPressedWithin(this InputFrameHistory h,
        ActionMask action, float window, float now);
    public static float HeldDuration(this InputFrameHistory h,
        ActionMask action, float now);
    public static bool TryGetMostRecent(this InputFrameHistory h,
        ActionMask candidates, float window, float now,
        out InputFrame frame, out ActionMask which);
}
```

`now`를 인자로 받아 **순수 함수로 유지**한다 — EditMode에서 시간을 주입해 링버퍼 로직을
PlayMode 없이 검증한다. `InputChordArbiter`가 이미 이 패턴이다.

### 5.3 기존 코드와의 관계 (초안에서 대폭 축소)

| 현재 | 이후 | 비고 |
|---|---|---|
| `PlayerAttackInputArbiter` 3개 메서드 | `TryGetMostRecent(AnyAttack, ...)` | **판정 규칙은 그대로**. 구현만 교체 |
| `_chargeHoldTime` + `_chargeAttackHeld` | `HeldDuration(HeavyAttack, now)` | **Phase 3.** 현재 `+= Time.deltaTime`(scaled)이므로 unscaled 전환은 전역 히트스톱 중 차지 속도를 바꾸는 **동작 변경**이다 |
| HeavyAttack 제거→재추가 (Input.cs:99,113) | 이력 질의로 대체 가능 | **Phase 3.** 합성 press 의미를 §8.2 (d)로 보존 |
| `SetExpiryPaused` | **유지** | §0.1 — 등가 구현이 없다 |
| `ToBufferTimestamp` | **유지** (축 변환), `PhysicalTime`으로 이관 | §0.2 |
| `ConsumeInput` 큐 2회 순회 | 비트 OR | **Phase 0**이 in-place로 선해소 |

---

## 6. 녹화

### 6.1 `InputTrace`

```csharp
[Serializable]
public class InputTrace
{
    public int     maskVersion;   // ActionMask 비트 배정 버전 (§3.1 함정 2)
    public string  sceneName;
    public string  characterActorType;
    public int     seed;
    public float   startUnscaledTime;
    public List<InputFrame> frames;
}
```

72바이트/프레임 → 1분 @60fps ≈ **260KB**, 무압축으로 충분하다. 압축은 도입하지 않는다.
저장은 `EncounterReplay`(AI 디버깅)와 같은 패턴을 따르고 새 파이프라인을 만들지 않는다.

### 6.2 F9 HUD 확장 — Phase 1의 산출물

에디터 창보다 먼저 손대야 할 곳은 기존 `PlayerControlFeelDebugHUD`다.
`AppendInputBuffer`가 대기 큐 대신 **이력 최근 N프레임**을 렌더하면 플레이를 멈추지 않고
직전 입력과 지연을 볼 수 있다. §2의 결정에 따라 **3열을 나란히** 표시한다.

```
입력 이력 (최근 1.0s)          플래그        버퍼
 -0.016 L press → atk1 (+32ms)  Attack=Handled  —
 -0.240 H hold 0.41s            Heavy=Pressed   H(0.05s)
 -0.512 D press  미소비          Dodge=None      —      ← Layer=Level_2 (UI 열림)
 -0.688 Swap1 press 미소비       —               —      ← Invalidated (Clear)
```

마지막 두 행이 §3.3에서 `Layer`를, §3.4에서 `_invalidated`를 둔 이유다.
**"눌렀는데 왜 안 나갔나"에 대한 답이 이 화면에서 끝나야 한다.**

### 6.3 재생

**(a) 분석 재생** — 트레이스를 읽어 §6.2와 같은 형식으로 스크럽. 결정론 불필요.

**(b) 주입 재생 (Phase 4, 선택)** — `IInputFrameSource`(`Live`/`Trace`)를 샘플러 앞단에 둔다.

> **범위 못박기 — 완전 결정론은 불가능하다.** 근거는 다음이다:
> (1) 상태 머신이 `Update()`에서 `Actor.DeltaTime = Time.deltaTime * _localTimeScale`로
> 돌아 **가변 프레임레이트에 직접 종속**된다(`ActorMovementController.cs:113-121`,
> `GameActor.cs:105`). (2) `LocalTimeScale` 그룹 구성이 프레임마다 달라진다
> (`KCCSimulator`). (3) Animancer 시간 기반 블렌딩. (4) `UnityEngine.Random`.
>
> **KCC 자체는 비결정 요인이 아니다** — `KCCSimulator`가 `AutoSimulation = false`로 끄고
> 고정 dt로 직접 `Simulate`하므로 오히려 통제되어 있다(초안의 오지목을 정정).
>
> 따라서 **회귀 테스트로 쓸 수 없다.** 용도는 "같은 입력 시나리오를 반복 재현해 사람이
> 관찰한다"까지다. Unity `InputRecorder`/`InputEventTrace`는 디바이스 이벤트 레벨이라
> 재현성이 더 낮다. **이 항목에 과투자하면 실패한다.**

### 6.4 에디터 타임라인 뷰 (Phase 4, 선택)

`IntentScoreTimelineView` + `EncounterReplayLoader`를 확장해 입력 레인을 추가하면
**AI 의도와 플레이어 입력을 겹쳐 볼 수 있다.** 매력적이지만 §6.2 HUD가 진단의 대부분을
담당하므로 **HUD로 해결되지 않는 사례가 실제로 나온 뒤에 착수한다.**
BT 에디터에서 겪은 성능 함정(증분 갱신, 전체 재스타일 금지)을 반복하지 않는다.

---

## 7. 새로 열리는 기능

1. **입력 지연 실측** — press → 발동 프레임 차이. §6.2의 핵심. 발동 측 훅은
   `CombatLogRecorder.ResultObserved`와 MotionEvent 발화 시점에 이미 있다.
2. **저스트가드 / 도지카운터 창 튜닝** — 성공·실패 시 press와 피격의 오프셋 분포 실측.
   `GrantStaggerImmunity`, `OpenAssistParryWindow`, `BeginSwapEvadeIFrame` 등 창 기반
   로직이 이미 다수라 수요가 크다.
3. **게임패드 조합키 회귀 감시** — §0.2의 grace vs 버퍼 창 마진(현재
   CharacterSwap 0.03s)을 실측으로 감시한다. 이 마진은 지금도 위험하게 얇다.
4. **negative edge / release 타이밍** — `Released`를 보조로 쓴 다단 차지 임계값.
   §5.1 함정 C를 준수해야 한다.

> **방향 모션 입력(↓↘→)은 목표가 아니다.** §3.2 참조. 44/66이 필요해지면
> `WasPressedWithin` 두 번으로 구현한다.

---

## 8. 마이그레이션

| Phase | 내용 | 위험 |
|---|---|---|
| **0** | `InputBuffer` **내부만** in-place 최적화. H4 종결 | **0** (의미·호출부 변경 없음) |
| **1** | 샘플러 + 이력 + F9 HUD 3열 확장. 기존 경로 무변경 | **낮음** (0은 아니다) |
| **2** | `InputTrace` 녹화 + 분석 재생 | 낮음 |
| **3** | 개별 결함 수정 (`_chargeHoldTime`, HeavyAttack 트릭 등). **일괄 전환 아님** | **중** |
| **4** | (선택) 에디터 타임라인, 주입 재생 | 낮음 |

### 8.1 Phase 0 — H4를 먼저 끝낸다 (신설)

`InputBuffer.cs` 내부만 고친다. **호출부·의미·public API 변경 0.**

- `CleanExpiredInputs`(218-236행): `new Queue<BufferedInput>()` → in-place 압축
- `ConsumeInput`(114-142행): 2회 순회 → 인덱스 링 단일 순회
- 키: `string` → 인터닝된 정수 (내부 표현만)

약 20줄. **이력 모델의 정당성이 H1(관측)에만 걸려 있게 만드는 것이 이 Phase의 목적이다.**
성능을 이유로 이력 모델을 정당화하는 논리를 차단한다.

### 8.2 Phase 1은 위험 0이 아니다

초안은 "순수 추가라 위험 0"이라 했으나 두 가지 실제 위험이 있다:

1. **실행 순서 미정의**(§5.1 함정 A) — 1프레임 지연이 씬마다 다르게 나타난다.
2. **pending 마스크는 새 가변 상태**(§5.1 함정 D) — 리셋 누락이 유령 입력이 된다.

둘 다 착수 전에 해결 방식을 확정해야 한다. 위험은 **"낮음"**이 맞고 0은 아니다.

### 8.3 Phase 2 어댑터를 만들 것인가 — 기본은 "만들지 않는다"

초안 §8.2는 `InputBuffer` 내부를 이력으로 교체하고 public API를 유지하는 어댑터를
제안했다. **Phase 0이 H4를 해소하면 이 어댑터의 존재 이유가 사라진다.**

만약 그래도 진행한다면 유지해야 할 API는 초안이 나열한 5개가 아니라 **10개**다:
`AddInput`, `HasInput`, `PeekInput`, `ConsumeInput`, `GetLatestInput`, `GetSnapshot`,
`Clear`, `SetExpiryPaused`, `Count`, `DebugPrint`. 그중 셋은 등가 구현이 자명하지 않다:

- **(a) `Clear()` (7 호출부, 에디터 2곳 포함)** — 이력을 파괴할 수 없으므로
  "창 내 전 액션을 `Invalidated`로 표시"로 정의한다. 그래서 §3.4에 `_invalidated`를
  `_consumed`와 **분리**해 뒀다 — 합치면 §6.2 HUD가 억제로 무효화된 입력을 "소비됨"으로
  오표시해 원인 판별이 망가진다.
- **(b) `SetExpiryPaused`** — §0.1에 따라 고정 창 질의로 등가 구현이 **불가능하다.**
  정지 누적 시간을 유지해 질의 창을 동적으로 연장해야 한다. 즉 상태 추적이 남는다.
- **(c) `AddInput`** — `PlayerActor.Input.cs:113`은 **과거 시점 press를 합성**한다.
  현재 프레임에 `Synthetic` 플래그 press를 주입하는 형태로 보존한다.

**결론: Phase 2를 "녹화"로 재정의하고, 어댑터 교체는 필요가 증명될 때까지 보류한다.**
어댑터의 실제 이관 범위는 State 계층 약 50개 호출 지점이며 "낮은 위험"이 아니다.

### 8.4 Phase 3은 일괄 전환이 아니라 개별 수정이다

초안은 "시간축 통일"이라는 일괄 전환을 계획했다. §0.1에 따라 폐기했다.
Phase 3은 **Phase 1~2에서 수집한 이력 데이터로 실제 결함이 확인된 항목만** 개별 수정한다.
후보: `_chargeHoldTime`의 scaled/unscaled 결정, HeavyAttack 제거→재추가 트릭 정리,
게임패드 조합키 마진(§7.3).

---

## 9. 입력 계층 부수 개선

§1~8과 독립적이다. **순서가 초안에서 바뀌었다.**

### 9.1 `Look`의 매 프레임 `RaycastAll` — 최우선 (초안에서 승격)

`Look`은 **Value 타입이고 `<Mouse>/delta`에 바인딩**되어 있다(실측). 따라서 마우스가
움직이는 동안 매 프레임 `performed`가 발화하고, `ShouldBlockPointerPlayerActionOverUI`의
3중 게이트(PlayerAction 맵 + pointer-like 장치 + `Level_0`)를 **모두 통과**해
`EventSystem.RaycastAll`이 **매 프레임 실행된다.** TPS에서 마우스는 거의 항상 움직인다.

```csharp
private int     _pointerOverUiFrame = -1;
private Vector2 _pointerOverUiPos;
private bool    _pointerOverUiResult;
```

> **함정:** 프레임 번호만으로 캐시하면 드래그 중 오판이 생긴다. **좌표를 함께 비교**한다.
> 또한 할당은 이미 `_uiPointerEventData`/`_uiRaycastResults` 재사용으로 해소돼 있으므로
> (`Event.cs:141-142`) 캐시의 목적은 **`RaycastAll` 횟수 감소로 한정**한다.
>
> 더 근본적으로는 `Look`/`Move` 같은 Value 액션을 pointer 게이트에서 **아예 제외**하는
> 편이 낫다 — 카메라 회전이 UI 위에서 막혀야 할 이유가 없다. 이쪽을 먼저 검토한다.

### 9.2 진단 링 — Phase 1과 함께

게이트에서 차단된 입력을 소형 링(32엔트리)에 사유와 함께 기록한다:
`Rebind` / `Suppressed` / `PointerOverUI` / `Layer` / `ChordProvisional` / `ChordSynthetic`.

§5.1에서 차단된 입력을 이력에 넣지 않기로 했으므로 이 링이 그 짝이다.
§3.1.1의 조합키 진단도 여기로 들어온다. **§6.2 HUD가 이 링을 함께 표시한다.**

### 9.3 `InvokeCancelEvents` 프레임 할당

`Event.cs:349,351` — 레이어 변경마다 `new HashSet<Action>` + `new[]{dict,dict,dict}`.
UI를 열고 닫을 때마다 발생한다. 필드 캐시 + `Clear()` 재사용. 위험 없는 순수 개선.

### 9.4 (스킵) Register/UnRegister 테이블화

초안은 이를 권고하며 "셀렉터 배열은 델리게이트 **참조 동일성**이 깨져
`list[i].Callback == callback` 비교가 실패한다"고 했다. **근거가 틀렸다** —
C# 델리게이트 `==`는 `Delegate.Equals`(Target + Method 비교)이므로 메서드 그룹 변환이
매번 새 인스턴스를 만들어도 동일 인스턴스의 동일 메서드면 같다. 현재 코드는 이 사실 위에서
정상 동작한다. 실제 위험은 **클로저를 새로 만드는 래핑 람다**이며,
`PartyManager.GetOrCreateSwapHandler`가 캐시로 이를 회피한 선례가 있다.

그리고 `PlayerActor.Input.cs`의 두 목록은 **현재 15/15 대칭이며 누수가 없다.**
1인 개발에서 동작하는 30줄을 리팩터링해 얻는 것은 미래 비대칭 방지뿐이고,
잃는 것은 "이 콜백이 어디서 등록되나"의 Go-to-reference 추적성이다.
**§9.6과 같은 기준으로 스킵한다.**

### 9.5 (스킵) 억제 이유 태그 스택

초안 스스로 "§9.2 진단 링이 있으면 상당 부분 대체된다"고 썼다. 대체되는 것을 남기지 않는다.

### 9.6 (프로파일 후 판단) 콜백 디스패치 배열화

`ExecuteCallbacksForAction`의 `Dictionary` 해시 + `List` 순회.
프레임당 통과 액션 2~3개, 리스트 길이 1~2이므로 실익이 없을 가능성이 높다.
**§9.1이 비교 불가능하게 큰 이득이므로 그쪽을 먼저 한다.**

### 9.7 (스킵) `InputChordArbiter.GraceSeconds` 액션별 분리

투기적 일반화를 하지 않는다. 단 §0.2에서 드러났듯 **grace 0.12s와 버퍼 창 0.15s의
마진이 0.03s로 얇다** — 이것이 문제로 확인되면 그때 액션별 grace를 도입한다.

---

## 10. 테스트

### 10.1 EditMode (필수)

`Assets/Tests/EditMode/Input/`에 추가. `InputChordArbiterTests`(14개)와 같은 방식.

| ID | 대상 | 단언 |
|---|---|---|
| T1 | `ActionMaskMap` | `InputDefine.PlayerAction` **29개** ↔ `ActionMask` 전단사. 총 비트 ≤ 63 |
| T2 | 링버퍼 순환 | capacity 초과 후 `TryGetBack`/`TryGet` 경계. 오버라이트된 인덱스는 false + 경고 |
| T3 | 원장 | `Consumed`/`Invalidated` **분리** 확인. 액션별·프레임별 독립 |
| T4 | **오버라이트 시 원장 초기화** | 링 한 바퀴 후 같은 슬롯의 새 프레임이 미소비 (§3.4 최대 위험) |
| T5 | `TryConsumeBuffered` | 창 경계, 소비된 press 무시, 소비 후 false |
| T6 | `TryGetMostRecent` | 약/강 동시 시 최근 승 (기존 규칙 보존 확인) |
| T7 | `HeldDuration` | 홀드 누적, release 후 0, **release 유실 시 폴링 값이 이긴다** (§5.1 함정 C) |
| T8 | `Dir9` 양자화 | 데드존 경계, 대각 각도, `None` vs `Neutral` |
| T9 | **`PhysicalTime` 기준 창 판정** | grace 0.12s 지연된 press가 0.15s 창 **내부**로 판정 (§0.2 회귀 방지) |
| T10 | 시간 주입 | 모든 질의가 `now` 인자로만 시간 판단 (`Time.*` 미참조) |
| T11 | 조합 전이 | provisional → 확정/synthetic-cancel이 이력·진단 링에 각각 **1회만** 기록 |
| T12 | pending 마스크 리셋 | `Reset`/레이어 변경/억제 진입 후 유령 입력 없음 (§5.1 함정 D) |

**T4와 T9가 가장 중요하다.** T4는 §3.4의 최대 위험, T9는 §0.2 회귀의 유일한 자동 방어선이다.
초안의 방향 모션 테스트(구 T9)는 매처 삭제와 함께 제거했다.

### 10.2 PlayMode (Phase 4)

프레임 정확한 결과 비교는 **단언하지 않는다**(§6.3).
"주입 시퀀스로 의도한 Ability가 활성화되는가" 수준까지다.

### 10.3 수동 검증

Phase 1 완료 후 §6.2 HUD로 확인한다. **게임패드를 반드시 포함한다** — §0.2의 회귀는
게임패드 전용이다.

- 약약약 콤보 / 약→강 전환
- 히트스톱 중 선입력 (`LocalTimeScale` freeze 구간)
- 액티브 히트 중 선입력 → 캔슬창 개방 시 소비
- **게임패드 dpad 캐릭터 스왑** (grace 0.12s vs 창 0.15s)
- **게임패드 LB+RB 조합 회피** vs 단독 RB (ElementBuff) 구분
- 레이어 변경 중 조합 보류 폐기 (`RefreshInputLayer` → `_chordArbiter.Reset()`)
- UI 닫기 직후 공격 (`SuppressPlayerActionInputBriefly`)
- 파티 스왑 직후 입력 (액터 간 `InputBuffer` 공유 배타성)

---

## 11. 비목표

- **콜백 경로(L5)를 이력 폴링으로 대체하지 않는다.**
- **완전 결정론 리플레이를 목표하지 않는다.** 불가능하다(§6.3).
- **`InputDefine` 문자열 상수를 제거하지 않는다.**
- **`InputChordArbiter` 판정 로직을 바꾸지 않는다.** 검증된 14개 테스트가 있다.
- **`PlayerAttackInputArbiter`의 "최근 승" 규칙을 바꾸지 않는다.**
- **`SetExpiryPaused`·`ToBufferTimestamp`를 제거하지 않는다.** §0.1, §0.2.
- **방향 모션 매처를 만들지 않는다.** §3.2.
- **트레이스 압축을 도입하지 않는다.**
- **네트워크 롤백을 준비하지 않는다.** 싱글플레이다.
- **시간축을 일괄 전환하지 않는다.** §0.1.

---

## 12. 외부 레퍼런스

**격투게임 계열** — 고정 크기 프레임 버퍼, 넘패드 방향 표기, 역방향 스캔 매칭.
`{ 방향, 창 너비, strict }` 체인으로 모션을 정의한다. §3.2의 `Dir9` 표기가 여기서 왔다
(매처는 채택하지 않았다 — §3.2).

- [How to Code Fighting Game Motion Inputs — Celia Wagar's CritPoints](https://critpoints.net/2025/02/05/how-to-code-fighting-game-motion-inputs/)
- [Fighting Game Input Systems — pangaea](https://pangaea.neocities.org/post/fighting-game-input-systems/)
- [Implementation of Input Buffer for Fighting Games](https://seung-cha.github.io/coding/2024/01/26/fighting-game-input-buffer.html)

**입력 스택 계열** — raw device / input processing 2단 분리, timestamp 기반 intent queue.
"각 입력에 정확한 발생 시각을 붙여 나이를 추적하고 더 최근 입력을 우선한다"는 원칙은
**이 프로젝트가 `PlayerAttackInputArbiter`와 `ToBufferTimestamp`에서 이미 올바르게 구현**하고
있다. §0.2가 그 사실을 재확인했다.

- [Designing a Robust Input Handling System for Games — GameDev.net](https://gamedev.net/tutorials/programming/general-and-gameplay-programming/designing-a-robust-input-handling-system-for-games-r2975/)
- [Input Buffering: The Key to Responsive Game Feel — Wayline](https://www.wayline.io/blog/input-buffering-responsive-game-feel)

**리플레이 계열** — 입력 기반 리플레이는 초기 상태 1회 + 프레임별 입력만 저장하며
**결정론이 전제**다. 이 프로젝트는 결정론이 없으므로 분석용 재생에 무게를 두고 주입 재생
범위를 좁힌다(§6.3).

- [Implementing a replay system in Unity and how I'd do it differently next time — Game Developer](https://www.gamedeveloper.com/programming/implementing-a-replay-system-in-unity-and-how-i-d-do-it-differently-next-time)
- [Unity InputRecorder API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/api/UnityEngine.InputSystem.InputRecorder.html)
