# InputFrame 이력 기반 입력 시스템 고도화 스펙

> 작성일: 2026-07-26
> 대상 버전: Unity 6 (6000.0.60f1), Input System 1.14.2, URP
> 분류: TODO 구현 스펙
> 적용 범위: 게임플레이 입력 샘플링·이력·선입력 판정·모션 입력·입력 녹화/재생, 입력 계층 부수 개선
> 관련 문서:
>
> - `Assets/docs/Complete/INPUT_SYSTEM_GUIDE.md`
> - `Assets/docs/TODO/GAMEPAD_UI_INPUT_REBINDING_SYSTEM_SPEC.md` (§9 조합키 중재 — 본 스펙이 그 위에 쌓인다)
> - `Assets/docs/Complete/INPUT_CANCEL_WINDOW_EXPLICIT_AUTHORING_DESIGN.md`
> - `Assets/docs/guide/INPUT_KEYMAP_REFERENCE.md`
>
> 관련 코드:
>
> - `Assets/02.Scripts/Data/Input/InputBuffer.cs`
> - `Assets/02.Scripts/Data/Input/InputChordArbiter.cs`
> - `Assets/02.Scripts/Data/Input/InputDefine.cs`
> - `Assets/02.Scripts/Manager/Input/InputManager*.cs`
> - `Assets/02.Scripts/Contracts/GameServices.cs` (`IInputService`)
> - `Assets/02.Scripts/GameActor/Object/Player/PlayerActor.Input.cs`
> - `Assets/02.Scripts/GameActor/State/Player/PlayerAttackInputArbiter.cs`
> - `Assets/02.Scripts/Data/Combat/ComboRouteData.cs`
> - `Assets/02.Scripts/GameActor/AI/Debugging/` (`EncounterReplay`, `IntentScoreTimeline`)
> - `Assets/02.Scripts/Tool/PlayerControlFeelDebugHUD.cs` (F9 조작감 HUD — 본 스펙의 주요 확장 대상)

## 구현 진행 상태

- 미구현. 본 문서는 설계 확정 전 스펙이다.
- Phase 1만 선행 착수하는 것을 권고한다(§8 참조). Phase 3은 Phase 1의 실측 데이터 없이 착수하지 않는다.

---

## 1. 목적과 배경

### 1.1 현재 구조

```
Unity Input System
 → InputManager 콜백 게이트 (rebind / suppression / pointer-over-UI)
 → InputChordArbiter (grace 기반 조합키 중재, unscaledTime)
 → dispatch ─┬→ InputBuffer (Queue<BufferedInput>, string 키, Time.time)
             └→ PlayerActor 개별 InputCondition 플래그 필드
 + ComboInputTracker (발동 확정 시 ComboInputToken push)
```

조합키 중재(§9), 물리 입력 시각 보존(`ToBufferTimestamp`), 타임스탬프 기반 약/강 중재
(`PlayerAttackInputArbiter`)는 이미 정교하게 구현되어 있다. **본 스펙은 이 판정 규칙들을
바꾸려는 것이 아니라, 그 규칙들이 올라앉은 자료구조를 교체하는 것이다.**

### 1.2 자료구조 차원의 한계

**H1. 입력 이력이 존재하지 않는다.**
`InputBuffer`는 *미소비 대기 큐*다. 소비되면 사라지고 만료되면 사라진다. 결과:

- 방향 모션 입력(↓↘→, 44, 66) 판정이 원리적으로 불가능하다. 방향 이력이 없다.
- "선입력이 씹혔다"는 리포트를 사후에 검증할 수단이 없다. press 시각과 실제 발동 시각의
  차이를 계산할 데이터가 남지 않는다.
- 저스트가드·도지카운터 창 폭을 실측으로 튜닝할 수 없다. 현재는 감으로 조정한다.

**H2. 시간축이 3개 섞여 있다.**
`InputChordArbiter`는 `Time.unscaledTime`, `InputBuffer`는 `Time.time`,
suppression은 `Time.frameCount` + `Time.unscaledTime`을 쓴다.
`InputManager.Chord.cs:184`의 `ToBufferTimestamp`가 두 축을 변환하는 어댑터로 존재하는 것이
이 혼재의 증상이다. 히트스톱은 타임스케일로 구현되므로 scaled 축에서는 버퍼 만료가 사실상
멈추고, 이를 `InputBuffer.SetExpiryPaused`로 우회하고 있다. 우회 자체는 올바르게 동작하지만,
근본 원인은 "선입력 버퍼가 잘못된 시간축을 쓴다"이다.

**H3. 소비 주체가 분산되어 우선순위 규칙이 흩어진다.**

- `PlayerActor.Input.cs:99` — performed에서 HeavyAttack을 버퍼에서 제거
- `PlayerActor.Input.cs:113` — canceled에서 짧은 누름이면 다시 추가
- `PlayerAttackInputArbiter` — 약/강 승자를 소비
- 각 State 진입점 — `ConsumeInput` 직접 호출

"넣었다 뺐다 다시 넣기"는 큐 모델이 강제한 우회다. 이력 모델에서는 press와 release가 항상
남아 있으므로 release 프레임에서 질의하면 끝난다.

**H4. 비용.**

- `ConsumeInput`은 큐 전체를 2회 순회한다(dequeue 루프 + 재삽입 루프).
- `CleanExpiredInputs`는 매 호출마다 `new Queue<BufferedInput>()`을 할당한다.
  `HasInput`/`PeekInput`/`ConsumeInput`/`Count`/`GetSnapshot` 전부가 이를 호출한다.
- 키가 문자열이라 매 조회가 문자열 비교다.

### 1.3 핵심 전환

> **버퍼에서 제거하지 않는다. 소비 표시만 한다. 만료는 링버퍼 오버라이트가 처리한다.**

이 한 줄이 H1~H4를 동시에 해소한다. 이미 소비된 입력도 이력에 남으므로 사후 분석이 가능해지고,
큐 재구성과 GC가 사라진다.

---

## 2. 계층 구조

```
L0 Device    Unity Input System (변경 없음)
L1 Sampler   매 프레임 고정 시점에 논리 입력을 InputFrame으로 스냅샷
L2 History   InputFrameHistory — 고정 크기 링버퍼, 구조체, 런타임 무할당
L3 Query     이력 위 순수 질의 (선입력 / 모션 / 차지 / negative edge)
L4 Ledger    소비 원장 (frameIndex + action → consumed 비트)
L5 Sink      InputManager 콜백 디스패치 · ComboInputTracker (기존 유지)
```

L5는 그대로 둔다. **콜백 기반 즉시 반응 경로를 이력 폴링으로 대체하려는 시도는 하지 않는다.**
그 경로는 UI 레이어 게이팅·`CheckFunc`·`CancelCallback` 의미가 얽혀 있어 교체 비용이
이득을 초과한다. 이력은 콜백 경로와 **병존하는 관측·판정 기반**이다.

### 2.1 asmdef 배치

`UPlayGround.Data`의 references는 `UPlayGround.Ability.Core`, `UPlayGround.Core`,
`AYellowpaper.SerializedCollections`뿐이며 **Unity Input System을 참조하지 않는다.**
`InputChordArbiter`가 Unity Input System 타입을 참조하지 않는 순수 로직으로 작성된
선례를 따른다.

| 요소 | 위치 | asmdef | 근거 |
|---|---|---|---|
| `InputFrame`, `ActionMask`, `Dir9` | `Data/Input/` | `UPlayGround.Data` | 순수 데이터 |
| `InputFrameHistory`, 질의·원장 | `Data/Input/` | `UPlayGround.Data` | 순수 로직 → EditMode 단독 검증 |
| `MotionPattern`, 매처 | `Data/Input/` | `UPlayGround.Data` | 순수 로직 |
| `InputFrameSampler` | `Manager/Input/` | Assembly-CSharp | `InputAction` 접촉 |
| `IInputFrameSource` 구현 | `Manager/Input/` | Assembly-CSharp | 라이브/트레이스 분기 |
| `InputTrace` 직렬화 | `Data/Input/` | `UPlayGround.Data` | 순수 데이터 |
| 타임라인 뷰 확장 | `GameActor/Editor/` | `UPlayGround.GameActor.Editor` | 기존 `IntentScoreTimelineView` 확장 |

이 분할의 실질 이득: **링버퍼와 모션 매칭 로직 전체가 Unity Input System 없이 EditMode에서
검증 가능하다.** `InputChordArbiterTests` 14개와 같은 방식이다.

---

## 3. 자료구조

### 3.1 `ActionMask`

```csharp
namespace UPlayGround.Input
{
    /// <summary>
    /// PlayerAction 액션 하나당 1비트. 존재 검사가 문자열 비교 대신 비트 AND가 된다.
    /// InputDefine.PlayerAction의 액션 수는 현재 30개이므로 ulong에 34비트 여유가 있다.
    /// </summary>
    [Flags]
    public enum ActionMask : ulong
    {
        None        = 0,
        Move        = 1UL << 0,
        Look        = 1UL << 1,
        // ... InputDefine.PlayerAction과 1:1
        Attack      = 1UL << 9,
        HeavyAttack = 1UL << 10,
        // ...

        AnyAttack   = Attack | HeavyAttack,
        AnySwap     = CharacterSwap_1 | CharacterSwap_2 | CharacterSwap_3 | CharacterSwap_4,
    }
}
```

**문자열 상수를 제거하지 않는다.** `InputDefine.PlayerAction`은 Unity `.inputactions` 에셋의
액션 이름과 묶여 있고 리바인딩·글리프·프로필 마이그레이션이 전부 문자열 기반이다.
`ActionMask`는 **병행 표현**이며, 양방향 매핑 테이블을 하나 둔다.

```csharp
public static class ActionMaskMap
{
    public static ActionMask FromName(string actionName);  // 정적 Dictionary, 1회 구축
    public static string ToName(ActionMask single);         // 비트 → 이름
}
```

> **함정:** `InputDefine.PlayerAction`에 액션을 추가하면 `ActionMask`도 함께 갱신해야 한다.
> 누락 시 `FromName`이 `None`을 반환해 조용히 판정에서 빠진다. EditMode 테스트로
> **양쪽 개수·전단사(bijection)를 단언**한다(§7.1 T1). 수동 동기화에 의존하지 않는다.
> 액션이 64개를 넘으면 `ulong` 하나로는 불가능하므로, 그 시점에 2워드 구조체로 승격한다
> (테스트가 63개 시점에 경고하도록 상한 단언을 포함한다).

### 3.2 `Dir9`

```csharp
/// <summary>
/// 이동 스틱/WASD의 8방향 + 뉴트럴 양자화. 넘패드 표기(격투게임 관례):
///   7 8 9        좌상 상 우상
///   4 5 6   =    좌  중 우
///   1 2 3        좌하 하 우하
/// 5 = 뉴트럴. 캐릭터 facing 기준 좌우 반전은 저장 시점이 아니라 질의 시점에 적용한다.
/// </summary>
public enum Dir9 : byte { None = 0, DownLeft = 1, Down = 2, DownRight = 3,
    Left = 4, Neutral = 5, Right = 6, UpLeft = 7, Up = 8, UpRight = 9 }
```

`Neutral`(5)과 `None`(0)을 구분한다. `None`은 "이 프레임에 이동 입력 채널 자체가 없었다"
(억제 중 등), `Neutral`은 "입력은 살아 있고 스틱이 중앙"이다. 모션 매칭에서 두 값의
의미가 다르다.

> **함정:** 양자화 임계값(데드존, 대각 판정 각도)은 반드시 데이터로 노출한다. 하드코딩하면
> 패드/키보드 간 모션 입력 성공률이 갈리고 원인 추적이 불가능해진다. 데드존은 기존
> Input System processor 값과 **이중으로 적용되지 않도록** 주의한다 — 샘플러는 이미
> processor를 통과한 값을 받는다.

### 3.3 `InputFrame`

```csharp
public readonly struct InputFrame
{
    public readonly int        Index;      // 단조 증가. 이력 슬롯 식별자
    public readonly float      Time;       // unscaledTime 단일 축 (§4)
    public readonly float      DeltaTime;  // unscaled
    public readonly ActionMask Pressed;    // 이 프레임 눌림 (edge)
    public readonly ActionMask Held;       // 유지 중 (level)
    public readonly ActionMask Released;   // 이 프레임 떼짐 (edge)
    public readonly Vector2    Move;       // processor 통과 후 원본
    public readonly Vector2    Look;
    public readonly Dir9       MoveDir9;
    public readonly byte       Device;     // ActiveInputDevice — 리플레이/글리프 검증용
    public readonly byte       Layer;      // InputLayer 압축 — 억제 구간 사후 판별용
}
```

패딩 포함 64바이트. 링버퍼 256프레임 = **16KB 사전 할당, 런타임 무할당.**

`Vector2`를 쓰고 `half2`를 쓰지 않는다. `UPlayGround.Data`의 `Unity.Mathematics` 가용성이
asmdef references에 명시되지 않아 불확실하고, 절약되는 16바이트/프레임(총 4KB)이
그 불확실성을 감당할 가치가 없다.

`Layer`를 넣는 이유: 사후 분석에서 "이 press가 왜 무시됐나"의 가장 흔한 답이 "UI 레이어가
올라가 있었다"이기 때문이다. 이력에 없으면 매번 추측하게 된다.

### 3.4 `InputFrameHistory`

```csharp
public sealed class InputFrameHistory
{
    private readonly InputFrame[] _frames;        // 사전 할당, 링
    private readonly ulong[]      _consumed;      // 병렬 배열 — 프레임당 소비 비트
    private int _head;                            // 최신 슬롯
    private int _count;
    private int _nextIndex;                       // 단조 증가 카운터

    public InputFrameHistory(int capacity = 256);

    public int Count      { get; }
    public int LatestIndex { get; }

    /// <summary>backOffset 0 = 최신. 범위 밖이면 false.</summary>
    public bool TryGetBack(int backOffset, out InputFrame frame);

    /// <summary>단조 인덱스로 조회. 이미 오버라이트됐으면 false.</summary>
    public bool TryGet(int frameIndex, out InputFrame frame);

    public void Push(in InputFrame frame);        // 오버라이트 시 _consumed 슬롯도 초기화

    public bool IsConsumed(int frameIndex, ActionMask action);
    public void MarkConsumed(int frameIndex, ActionMask action);
}
```

**용량 근거:** 256프레임 ≈ 4.27초 @60fps. 최장 선입력 창(0.24s)의 17배이고,
`ComboInputTracker.LinkWindow`(1.0s)의 4배다. 가변 프레임레이트에서 30fps로 떨어지면
8.5초로 늘어나므로 여유는 더 커진다. 반대로 프레임레이트가 높으면 시간 폭이 줄어든다 —
**144fps에서 1.78초**이므로 `LinkWindow` 1.0s는 여전히 커버되지만, 향후 창을 늘리면
용량 재검토가 필요하다. 질의 API는 전부 시간 창 기준이므로 프레임 수가 아니라 시간으로
사고한다.

> **함정:** `Push`에서 오버라이트되는 슬롯의 `_consumed` 비트를 반드시 0으로 지운다.
> 누락하면 새 프레임의 입력이 "이미 소비됨"으로 판정되어 **입력이 조용히 씹힌다.**
> 이 스펙에서 가장 위험한 단일 버그 지점이다. §7.1 T4로 단언한다.

### 3.5 소비 원장이 큐 삭제보다 나은 이유

| 항목 | 큐 삭제 (현재) | 소비 원장 (제안) |
|---|---|---|
| 소비 후 분석 | 불가 (사라짐) | 가능 (플래그만 켜짐) |
| 소비 연산 | 큐 2회 순회 + 재삽입 | 비트 OR 1회 |
| 만료 처리 | `new Queue` 재구성 | 링 오버라이트 (무비용) |
| 중복 소비 방지 | 존재 자체가 방지 | 명시적 플래그 검사 필요 |
| 동일 액션 연타 | 각각 별 엔트리 | 프레임별 분리 → 자연히 구분 |

마지막 행이 부수 이득이다. 현재 `ConsumeInput`은 "가장 오래된 매칭"을 반환하므로 같은
액션을 빠르게 두 번 누르면 순서 의존성이 생긴다. 프레임 단위 원장에서는 어느 프레임의
press인지가 명확하다.

---

## 4. 시간축 통일

**모든 입력 이력은 `Time.unscaledTime`을 쓴다.**

근거: 히트스톱이 타임스케일로 구현되므로 scaled 축에서는 "0.24초 선입력 창"이 히트스톱
길이만큼 늘어난다. 플레이어의 손가락은 실시간으로 움직이므로 입력 창은 실시간이어야 한다.

이 통일의 부수 효과:

- `ToBufferTimestamp`(Chord.cs:184) 변환이 불필요해진다.
- `InputBuffer.SetExpiryPaused`가 불필요해진다. "캔슬 불가 구간에서 선입력을 보존한다"는
  의도는 만료 정지가 아니라 **질의 시점 재해석**으로 달성된다 — 이력이 남아 있으니
  캔슬창이 열리는 프레임에서 "직전 N초 내 press"를 물어보면 된다.
- `EncounterReplayRecorder`(AI 디버깅)와 시간축이 일치하므로 **플레이어 입력과 AI 의도
  타임라인을 한 화면에 겹쳐 볼 수 있다.** 이것이 §6의 최대 효과 지점이다.

> **위험 — 이것은 동작 변경이다.** `SetExpiryPaused` 제거와 축 전환은 선입력 관용도를
> 실제로 바꾼다. 방향(관용도 감소/증가)조차 콘텐츠별로 다를 수 있다. 그래서 Phase 3으로
> 미루고, **Phase 1에서 수집한 실측 데이터로 전후를 비교 검증한 뒤에만** 적용한다.
> 절대 Phase 1과 묶어서 진행하지 않는다.

`ComboInputTracker`도 `Time.time`을 쓴다(`LinkWindow` 만료). 같은 이유로 Phase 3에서
함께 전환한다.

---

## 5. 샘플러와 질의

### 5.1 샘플링 시점

`InputFrameSampler`는 `InputManager.OnUpdate` **선두**에서 1회 실행한다. 현재
`OnUpdate`는 `TickChordArbiter()`만 호출하므로 그 앞에 넣는다.

```csharp
public void OnUpdate()
{
    _frameSampler.Sample();   // 신규 — 이력 push
    TickChordArbiter();
}
```

순서 근거: 중재기 tick이 grace 만료로 콜백을 디스패치하면 그 콜백이 게임플레이 상태를
바꿀 수 있다. 이력은 **그 이전의 물리 입력 상태**를 기록해야 인과가 맞다.

> **함정:** `Held` 마스크는 `InputAction.IsPressed()`로 폴링하지만, `Pressed`/`Released`는
> **엣지**다. 샘플러가 `Held` 전후 차분으로 엣지를 만들면 한 프레임 안에 press+release가
> 모두 일어난 초단타 입력(1프레임 탭)을 놓친다. Unity Input System 콜백은 이를 잡는다.
> 따라서 **엣지는 콜백에서 누적한 pending 마스크를 샘플러가 소비**하고, `Held`만 폴링한다.
> 하이브리드가 필수이며, 순수 폴링 구현은 이 스펙 위반이다.

```csharp
// InputManager.Event.cs의 dispatch 경로에서 누적
private ActionMask _pendingPressed, _pendingReleased;
// 샘플러가 읽고 0으로 리셋
```

엣지를 dispatch 경로(= 중재기 통과 후)에서 누적할지, 게이트 통과 직후에 누적할지는
설계 선택이다. **dispatch 경로를 택한다** — 게이트에서 차단된 입력을 이력에 넣으면
"눌렀는데 안 나갔다"의 원인이 게이트인지 판정 로직인지 구분이 안 된다. 대신
차단된 입력은 §9.2의 별도 진단 링에 기록한다.

### 5.2 질의 API

```csharp
public static class InputHistoryQuery
{
    /// <summary>window 초 내 미소비 press가 있으면 소비하고 true.</summary>
    public static bool TryConsumeBuffered(
        this InputFrameHistory h, ActionMask action, float window, float now,
        out InputFrame frame);

    /// <summary>소비하지 않고 존재만 확인.</summary>
    public static bool WasPressedWithin(
        this InputFrameHistory h, ActionMask action, float window, float now);

    /// <summary>현재 연속 홀드 시간(초). 홀드 중이 아니면 0.</summary>
    public static float HeldDuration(
        this InputFrameHistory h, ActionMask action, float now);

    /// <summary>candidates 중 가장 최근 미소비 press. 약/강 중재의 단일 진입점.</summary>
    public static bool TryGetMostRecent(
        this InputFrameHistory h, ActionMask candidates, float window, float now,
        out InputFrame frame, out ActionMask which);

    /// <summary>방향 모션 패턴 일치 여부. facing 반전을 질의 시점에 적용.</summary>
    public static bool MatchMotion(
        this InputFrameHistory h, in MotionPattern pattern, bool facingRight, float now);
}
```

`now`를 인자로 받는 이유: **순수 함수로 유지해 EditMode 테스트에서 시간을 주입**할 수 있게
한다. `Time.unscaledTime`을 내부에서 읽으면 링버퍼 로직을 PlayMode 없이 검증할 수 없다.
`InputChordArbiter`가 이미 이 패턴이다(`Submit(..., Time.unscaledTime, ...)`).

### 5.3 기존 코드가 접히는 지점

| 현재 | 이후 |
|---|---|
| `PlayerAttackInputArbiter` 3개 메서드 (68행) | `TryGetMostRecent(AnyAttack, ...)` |
| `_chargeHoldTime` + `_chargeAttackHeld` + Update 누적 | `HeldDuration(HeavyAttack, now)` |
| HeavyAttack 제거→재추가 트릭 (Input.cs:99, 113) | 삭제 — press/release가 이력에 상주 |
| `InputBuffer.SetExpiryPaused` (30행 + 호출부) | 삭제 — 질의 창 재해석 |
| `ToBufferTimestamp` (Chord.cs:184) | 삭제 — 단일 축 |
| `ConsumeInput` 큐 2회 순회 | 비트 OR |

`PlayerAttackInputArbiter`의 **판정 규칙("더 최근에 누른 쪽이 이긴다")은 그대로 유지한다.**
구현만 이력 질의로 바뀐다. 이 규칙은 웹 레퍼런스가 권장하는 원칙과 일치하므로 바꿀 이유가 없다.

---

## 6. 녹화와 재생

### 6.1 녹화

이력을 append-only로 흘리면 그대로 트레이스다.

```csharp
[Serializable]
public class InputTrace
{
    public string  sceneName;
    public string  characterActorType;
    public int     seed;              // CycleRunManager 시드 — 재현 조건 기록
    public float   startUnscaledTime;
    public List<InputFrame> frames;   // struct 리스트
}
```

64바이트/프레임 → 1분 @60fps ≈ **230KB**, 무압축으로 충분하다. 압축은 도입하지 않는다.

저장 위치는 `EncounterReplay`와 형제로 둔다. 그 쪽 패턴(JSON, `Assets/../Debugging/`)을
그대로 따르고 새 파이프라인을 만들지 않는다.

### 6.2 타임라인 뷰 — 최소 비용 최대 효과

`Assets/02.Scripts/GameActor/Editor/IntentScoreTimelineView.cs`와
`EncounterReplayLoader.cs`가 이미 존재한다. §4로 시간축이 통일되면 여기에 **입력 레인만
추가**해서 다음을 한 화면에 겹칠 수 있다:

```
t →  ───────────────────────────────────────────────
입력   │ L    L    H(hold 0.4s)──┘   D
발동   │  ▲atk1 ▲atk2      ▲charge      ▲dodge
지연   │  32ms  41ms        —           128ms  ← 씹힘 의심
AI의도 │ [Approach][Attack     ][Retreat]
피격   │              ✕
```

**"입력 → 발동 지연"의 시각화가 이 작업 전체의 실질 산출물이다.** 이것 하나로
조작감 리포트가 추측에서 수치로 바뀐다.

구현 시 `IntentScoreTimelineView`를 확장하되, BT 에디터에서 학습한 성능 함정을 반복하지
않는다 — 증분 갱신만 하고 매 갱신마다 전체 요소를 재스타일하지 않는다
(`project_bt_editor_debug_perf` 메모 참조).

### 6.3 재생 — 두 종류를 분리한다

**(a) 분석 재생 (Phase 1, 필수).** 게임을 돌리지 않고 에디터 창에서 트레이스를 스크럽한다.
결정론이 전혀 필요 없다. **실질 가치의 대부분이 여기에 있다.**

**(b) 주입 재생 (Phase 4, 범위 제한).** 샘플러 앞단을 인터페이스로 분리한다.

```csharp
public interface IInputFrameSource { bool TryProduce(float now, out InputFrame frame); }
// LiveInputFrameSource   — Unity Input System
// TraceInputFrameSource  — InputTrace 재생
```

> **범위 못박기 — 완전 결정론은 이 프로젝트에서 불가능하다.**
> KCC(물리), Animancer(시간 기반 블렌딩), `UnityEngine.Random`(`PlayerActor.Input.cs`가
> 직접 import), 프레임레이트 가변성이 겹쳐 있다. 동일 입력이 동일 결과를 보장하지 않는다.
> 따라서 **회귀 테스트로 쓸 수 없다.** 용도는 "같은 입력 시나리오를 반복 재현해서
> 사람이 관찰한다"까지다.
>
> Unity 자체 `InputRecorder`/`InputEventTrace`는 디바이스 이벤트 레벨이라 재현성이 오히려
> 더 낮다(장치 상태·프레임 마커 의존). 논리 액션 레벨인 우리 샘플러 앞단 주입이 더 안정적이다.
>
> **이 항목에 과투자하면 실패한다.** Phase 4를 Phase 1~3보다 먼저 하려는 유혹을 경계한다.

PlayMode 테스트 용도로는 제한적으로 유용하다 — 기존 Ability PlayMode 수직 슬라이스 2개에
"입력 시나리오 주입 → 특정 Ability 활성화 여부" 수준의 단언은 가능하다. 프레임 정확한
결과 비교는 불가능하다.

---

## 7. 새로 열리는 기능

### 7.1 방향 모션 입력

`ComboInputToken`은 현재 버튼 9종뿐이고 방향이 없다. 이력이 있으면 확장 가능하다.

```csharp
public readonly struct MotionStep
{
    public readonly Dir9  Dir;
    public readonly float Window;   // 이 스텝까지 허용 시간(초)
    public readonly bool  Strict;   // false면 인접 방향 허용 (3 요구 시 2/3/6 수용)
}

public readonly struct MotionPattern
{
    public readonly MotionStep[] Steps;  // 역순 저장 — 뒤에서부터 스캔
    public readonly ActionMask   Trigger;
}
```

매칭은 **버튼이 눌린 프레임에서 뒤로 스캔**한다(웹 레퍼런스 공통 방식). 스텝별 독립
시간 창이 "얼마나 빨리 입력해야 하는가"를 결정한다.

> **함정 1 — facing 반전.** 좌우 반전은 저장이 아니라 **질의 시점**에 적용한다.
> 넘패드 표기에서 좌우 반전은 `1↔3, 4↔6, 7↔9`이며, 3D TPS인 이 프로젝트에서는
> "facing"이 카메라 기준인지 캐릭터 기준인지부터 정의해야 한다. 2D 격투게임의
> 반전 공식(±2)을 그대로 쓸 수 없다. **이동 입력은 이미 카메라 상대이므로
> `Move`가 카메라 공간이라는 점을 확인한 뒤 설계한다.**
>
> **함정 2 — 3D에서의 실효성.** 방향 모션 입력은 2D 격투게임의 관용구다. 카메라가
> 자유롭게 돌아가는 TPS에서 ↓↘→가 직관적인지는 **디자인 검증이 필요하다.**
> 44(더블탭 백스텝), 66(더블탭 대시)처럼 **축 대칭인 패턴만** 우선 도입하고
> 사분원 계열은 보류하는 것을 권고한다.

### 7.2 입력 지연 실측

press 프레임 인덱스와 실제 발동 프레임 인덱스의 차이. §6.2 타임라인의 "지연" 레인이 이것이다.
발동 측 훅은 이미 존재한다 — `CombatLogRecorder.ResultObserved`
(`project_balance_tooling_suite` 메모)와 MotionEvent 발화 시점.

### 7.3 저스트가드 / 도지카운터 창 튜닝

성공·실패 시 press 시각과 피격 시각의 오프셋 분포를 실측한다. 현재는 감으로 조정한다.
`GrantStaggerImmunity`, `OpenAssistParryWindow`, `BeginSwapEvadeIFrame` 등 이미 창 기반
로직이 다수 존재하므로 실측 데이터의 수요가 크다.

### 7.4 negative edge / release 타이밍

`Released` 마스크가 프레임 단위로 존재하므로 차지 해제 타이밍을 정밀 처리할 수 있다.
현재 `OnHeavyAttackCanceled`의 `_chargeHoldTime < ChargeThreshold` 단일 임계값을
다단 임계값으로 확장하는 것이 가능해진다.

---

## 8. 마이그레이션 — `InputBuffer`를 삭제하지 않는다

| Phase | 내용 | 조작감 위험 |
|---|---|---|
| **1** | 샘플러 + 이력 + 녹화 + 타임라인 뷰. **기존 경로 무변경** | **0 (순수 추가)** |
| **2** | `InputBuffer` 내부를 이력 질의로 교체, public API 시그니처 유지 → 호출부 무변경 | 낮음 |
| **3** | 호출부를 `ActionMask` 질의로 점진 이관. 시간축 통일, 우회 코드 제거 | **중** |
| **4** | 방향 모션 입력 / 주입 재생 | 낮음 |

### 8.1 Phase 순서가 중요한 이유

Phase 1은 순수 추가라 조작감 회귀 위험이 0이다. 그리고 **Phase 1에서 수집한 실측
데이터로 Phase 3의 시간축 통일이 조작감을 실제로 어떻게 바꾸는지 검증할 수 있다.**
순서를 뒤집으면 감으로 판단하게 되고, 그건 지금 상태와 다를 게 없다.

### 8.2 Phase 2의 어댑터 전략

```csharp
public class InputBuffer   // 클래스 유지, 내부만 교체
{
    private readonly InputFrameHistory _history;

    // 시그니처 전부 유지 → PlayerActor, PlayerAttackInputArbiter, State 무변경
    public bool HasInput(string inputName);
    public BufferedInput PeekInput(string inputName);
    public BufferedInput ConsumeInput(string inputName);
    public void AddInput(string, object, float?, float?);
    public void SetExpiryPaused(bool);   // Phase 2에서는 no-op 아님 — 3에서 제거
}
```

`IInputService.InputBuffer`가 공개 계약이므로(GameServices.cs:47) 이 유지가 중요하다.
Phase 2 완료 시점에 **호출부 변경 0으로 H4(GC·문자열 비용)가 해소된다.**

> **함정:** `AddInput`의 `object data` 인자와 `BufferedInput.Data`를 쓰는 호출부가
> 있는지 Phase 2 착수 전에 전수 조사한다. `InputFrame`은 구조체라 임의 페이로드를
> 담을 수 없다. 현재 코드에서 `data`를 넘기는 호출은 확인되지 않았지만
> (`bufferTime:`/`timestamp:` named 인자만 사용), 검증 없이 진행하지 않는다.

### 8.3 Phase 3 병주 검증

Phase 3에서 시간축을 전환할 때는 기존 경로를 즉시 삭제하지 않고 플래그로 전환 가능하게
두어, 같은 조작 시나리오를 양쪽에서 녹화해 §6.2 타임라인으로 지연 분포를 비교한다.
비교 없이 전환하면 조작감 회귀를 발견할 방법이 없다.

---

## 9. 입력 계층 부수 개선

§1~8과 독립적이며 지금 처리 가능하다. 값어치 순.

### 9.1 `InvokeCancelEvents` 프레임 할당 — 즉시 처리 권고

`InputManager.Event.cs:349,351`

```csharp
HashSet<Action> executedCancels = new HashSet<Action>();     // 매 호출 할당
var dicts = new[] { startCallbackDict, performCallbackDict, cancelCallbackDict };  // 매 호출 할당
```

레이어 변경은 UI를 열고 닫을 때마다 발생한다. 둘 다 필드로 캐시하고 `Clear()`로 재사용한다.
위험 없는 순수 개선.

### 9.2 `IsPointerOverUI`의 매 입력 `RaycastAll` — 즉시 처리 권고

`InputManager.Event.cs:163`. 마우스 입력(= 대부분의 경우 공격)마다 전체 UI 레이캐스트를 돈다.
같은 프레임 내 재질의는 결과를 재사용한다.

```csharp
private int     _pointerOverUiFrame = -1;
private Vector2 _pointerOverUiPos;
private bool    _pointerOverUiResult;
```

> **함정:** 같은 프레임이라도 포인터 좌표가 바뀌면(고빈도 마우스) 캐시를 무효화해야 한다.
> 프레임 번호만으로 캐시하면 드래그 중 오판이 생긴다. 좌표를 함께 비교한다.

겸사로 **차단된 입력을 진단 링에 기록**한다(§5.1 참조). "눌렀는데 안 나갔다"의 원인이
포인터 게이트인지 레이어인지 억제인지 구분 가능해진다. 소형 링버퍼(32엔트리) 하나로 충분하다.

### 9.3 `PlayerActor.Input.cs`의 수동 Register/UnRegister 15쌍 — 권고

39~54행과 62~76행이 완전히 대칭인 15줄씩이다. 두 목록이 어긋나면 **조용히 누수**한다.
캐릭터 스왑이 빈번한 게임이므로 실비용이 있다.

```csharp
private static readonly InputBindingEntry[] Bindings = { /* action, started, performed, canceled, check, cancel */ };
// Register/UnRegister가 같은 배열을 순회 → 비대칭이 구조적으로 불가능
```

> **함정:** 콜백이 인스턴스 메서드라 `static` 배열에 직접 담을 수 없다. 인스턴스 생성
> 시점에 1회 구축하거나, `Func<PlayerActor, Action<...>>` 셀렉터 배열로 만든다.
> 후자는 델리게이트 참조 동일성이 깨져 `UnRegisterInputEvent`의
> `list[i].Callback == callback` 비교가 실패한다 — **인스턴스 배열 1회 구축을 택한다.**

### 9.4 억제 상태를 이유 태그 스택으로 — 디버깅 편의

`ShouldSuppressPlayerActionInput`이 `_isPlayerActionInputSuppressed`(bool) +
`_playerActionSuppressedUntilFrame`(frame) + `_playerActionSuppressedUntilTime`(time)
3축이라 "지금 왜 막혔나"를 알 수 없다. 이유 태그를 가진 스택으로 바꾼다.
`_cursorVisibleStack`이 이미 스택 패턴이라 일관성도 생긴다.

우선순위는 §9.1~9.3보다 낮다. §9.2의 진단 링이 있으면 상당 부분 대체된다.

### 9.5 콜백 디스패치 dict + List 선형 순회 — **프로파일 후 판단**

`ExecuteCallbacksForAction`이 매 입력마다 `Dictionary` 해시 + `List` 순회 +
`CheckFunc` 델리게이트 호출을 한다. `Move`/`Look`은 PassThrough라 매 프레임 통과한다.
액션 인덱스 기반 배열화로 해시를 제거할 수 있다.

> **실측 없이 손대지 않는다.** 프레임당 통과 액션이 2~3개이고 리스트 길이가 1~2이므로
> 실제 비용이 무의미할 가능성이 높다. §9.1의 할당 제거가 GC 측면에서 훨씬 확실한 이득이다.

### 9.6 `InputChordArbiter.GraceSeconds` 전역 상수 — 스킵

액션별 grace가 필요해지면 막히지만, 현재 문제가 보고되지 않았다. **투기적 일반화를 하지 않는다.**
필요해지는 시점에 처리한다.

---

## 10. 테스트

### 10.1 EditMode (필수)

`Assets/Tests/EditMode/Input/`에 추가한다.
`InputChordArbiterTests`(14개)와 같은 방식 — Unity Input System 없이 순수 로직 검증.

| ID | 대상 | 단언 |
|---|---|---|
| T1 | `ActionMaskMap` | `InputDefine.PlayerAction`의 모든 상수 ↔ `ActionMask` 전단사. 개수 일치. 총 비트 수 ≤ 63 (64 도달 시 사전 경고) |
| T2 | 링버퍼 순환 | capacity 초과 push 후 `TryGetBack`/`TryGet` 경계. 오버라이트된 인덱스는 `false` |
| T3 | 소비 원장 | `MarkConsumed` 후 `IsConsumed` true, 다른 액션은 false, 다른 프레임은 false |
| T4 | **오버라이트 시 원장 초기화** | capacity 바퀴를 돈 뒤 같은 슬롯의 새 프레임이 미소비 상태여야 한다 (§3.4 최대 위험 지점) |
| T5 | `TryConsumeBuffered` | 창 내/외 경계, 이미 소비된 press는 무시, 소비 후 재질의 false |
| T6 | `TryGetMostRecent` | 약/강 동시 존재 시 최근 승, 한쪽만 존재, 둘 다 창 밖 |
| T7 | `HeldDuration` | 홀드 중 누적, release 후 0, 홀드 중간에 이력 오버라이트된 경우 |
| T8 | `Dir9` 양자화 | 데드존 경계, 대각 판정 각도, `None` vs `Neutral` 구분 |
| T9 | `MatchMotion` | 스텝별 창 초과 시 실패, `Strict` on/off, facing 반전, 무관 방향 개입 |
| T10 | 시간 주입 | 모든 질의가 `now` 인자로만 시간을 판단 (`Time.*` 미참조) |

T4를 별도 항목으로 세우는 이유는 §3.4에 적었다. 이 버그는 재현이 간헐적이고
증상이 "입력이 가끔 씹힘"이라 필드에서 원인 추적이 매우 어렵다.

### 10.2 PlayMode (Phase 4)

기존 Ability 수직 슬라이스 2개 옆에 입력 시나리오 주입 테스트를 추가한다.
**프레임 정확한 결과 비교는 단언하지 않는다**(§6.3). "주입한 시퀀스로 의도한 Ability가
활성화되는가" 수준까지다.

### 10.3 수동 검증

Phase 3 전후로 동일 시나리오를 녹화해 §6.2 타임라인에서 지연 분포를 비교한다.
최소 시나리오: 약약약 콤보, 약→강 전환, 히트스톱 중 선입력, 대시 취소,
캐릭터 스왑 중 공격 선입력, UI 닫기 직후 공격.

마지막 항목은 `SuppressPlayerActionInputBriefly`가 담당하는 경로이므로
§9.2의 진단 링과 함께 확인한다.

---

## 11. 비목표

명시적으로 하지 않는 것들. 범위 확산을 막기 위해 기록한다.

- **콜백 경로를 이력 폴링으로 대체하지 않는다.** L5는 유지된다(§2).
- **완전 결정론 리플레이를 목표하지 않는다.** 불가능하다(§6.3).
- **`InputDefine` 문자열 상수를 제거하지 않는다.** 리바인딩·글리프·프로필이 의존한다(§3.1).
- **`InputChordArbiter`의 판정 로직을 바꾸지 않는다.** 이미 검증된 14개 테스트가 있다.
- **`PlayerAttackInputArbiter`의 "최근 승" 규칙을 바꾸지 않는다.** 구현만 교체한다.
- **트레이스 압축을 도입하지 않는다.** 230KB/분은 무압축으로 충분하다.
- **네트워크 롤백을 준비하지 않는다.** 싱글플레이 프로젝트다. 링버퍼가 롤백과 구조가
  비슷해 보여도 상태 스냅샷·복원이 없으므로 롤백이 아니다.

---

## 12. 외부 레퍼런스

본 설계는 두 계보의 합성이다.

**격투게임 계열** — 고정 크기 프레임 버퍼, 넘패드 방향 표기, **역방향 스캔** 매칭.
`{ 방향, 프레임 창 너비, strict 플래그 }` 구조체 체인으로 모션을 정의하고 버튼이 눌린
순간 버퍼를 뒤에서부터 훑는다. 방향별 독립 창이 입력 속도 요구를 결정한다. 차지 입력은
연속 홀드 카운트로 별도 처리하고, facing 반전 함정과 "무관 방향 개입 검사"(SNK long-cut)가
명시되어 있다. §3.2, §7.1이 여기서 왔다.

- [How to Code Fighting Game Motion Inputs — Celia Wagar's CritPoints](https://critpoints.net/2025/02/05/how-to-code-fighting-game-motion-inputs/)
- [Fighting Game Input Systems — pangaea](https://pangaea.neocities.org/post/fighting-game-input-systems/)
- [Implementation of Input Buffer for Fighting Games](https://seung-cha.github.io/coding/2024/01/26/fighting-game-input-buffer.html)

**입력 스택 계열** — raw device layer / input processing layer 2단 분리, timestamp 기반
intent queue. "각 입력 이벤트에 정확한 발생 시각을 붙여 나이를 추적하고 더 최근 입력을
우선한다"는 원칙은 **이 프로젝트가 `PlayerAttackInputArbiter`에서 이미 올바르게 구현**하고
있다. 본 스펙은 그 원칙을 전역으로 확장하는 것이다. §2, §5가 여기서 왔다.

- [Designing a Robust Input Handling System for Games — GameDev.net](https://gamedev.net/tutorials/programming/general-and-gameplay-programming/designing-a-robust-input-handling-system-for-games-r2975/)
- [Input Buffering: The Key to Responsive Game Feel — Wayline](https://www.wayline.io/blog/input-buffering-responsive-game-feel)

**리플레이 계열** — 입력 기반 리플레이는 초기 상태 1회 + 프레임별 입력만 저장하는 것이
정석이며 **결정론이 전제**다. 결정론이 없으면 상태 스냅샷 기반으로 가야 한다.
이 프로젝트는 결정론이 없으므로 **분석용 재생에 무게를 두고 주입 재생 범위를 좁힌다**는
§6.3의 판단이 여기서 나왔다.

- [Implementing a replay system in Unity and how I'd do it differently next time — Game Developer](https://www.gamedeveloper.com/programming/implementing-a-replay-system-in-unity-and-how-i-d-do-it-differently-next-time)
- [Unity InputRecorder API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/api/UnityEngine.InputSystem.InputRecorder.html)
