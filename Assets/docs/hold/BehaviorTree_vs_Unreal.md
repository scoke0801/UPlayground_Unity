# BehaviorTree — 현재 구조 vs Unreal Engine 비교 분석

## 개요

현재 시스템은 Unreal Engine BT의 개념을 참조하여 설계되었으나, 구현 범위와 실행 모델에서 차이가 있다.  
이 문서는 **어떤 차이가 존재하는지, 그 영향, 개선 우선순위**를 정리한다.

---

## 핵심 차이점 한눈에 보기

| 항목 | Unreal Engine | 현재 시스템 | 영향도 |
|---|---|---|---|
| **실행 모델** | 이벤트 기반 (BB 변화 감지) | 폴링 (0.1s 인터벌) | 중 |
| **Decorator 방식** | 노드에 List로 부착 | 트리 래퍼 노드 (흡수 뷰 적용) | 중 |
| **Service** | 컴포짓에 부착, 주기적 틱 | 없음 (`RefreshBlackboard`로 대체) | 중 |
| **Abort / Interrupt** | Self / LowerPriority / Both | 없음 | 높음 |
| **SimpleParallel** | 메인+백그라운드 병렬 실행 | 없음 | 낮음 |
| **Loop 데코레이터** | `BTDecorator_Loop` | 없음 | 낮음 |
| **ForceSuccess 데코레이터** | `BTDecorator_ForceSuccess` | 없음 | 낮음 |
| **서브트리 (RunBehavior)** | `BTTask_RunBehavior` | 없음 | 낮음 |
| **Blackboard Notify** | BB 키 변화 → 옵저버 알림 | 없음 | 중 |
| **에디터: 실행 순서 번호** | 노드마다 인덱스 표시 | 없음 | 낮음 |
| **에디터: 브레이크포인트** | 있음 | 없음 | 낮음 |
| **에디터: 서비스 스트립** | 노드 카드 하단 | 없음 | 낮음 |
| **RandomSelector** | 없음 (플러그인) | `BTRandomSelectorSO` ✅ | — |
| **weighted 액션 트리거** | 없음 | `BTCond_RandomChance` ✅ | — |

---

## 1. 실행 모델 — 폴링 vs 이벤트 기반

### Unreal 방식

```
BB.Key 변경 → 옵저버 데코레이터 알림 → 필요한 경우에만 BT 재평가
```

- Decorator에 **AbortType**을 설정하면 BB 값 변화 시 자동으로 해당 분기를 중단·재시작
- 매 프레임 트리 전체를 순회하지 않음 → CPU 효율적
- Running 중인 Task는 완료될 때까지 유지, 외부 조건이 변하면 Abort

### 현재 방식

```
Update() → decisionTimer -= deltaTime → timer ≤ 0 → MakeDecision() → BT 전체 Tick
```

- `EnemyBrain._decisionInterval = 0.1s` 주기로 강제 재평가
- 현재 Running 상태여도 다음 틱에 전혀 다른 분기가 선택될 수 있음
- `BTComposite._runningIndex`로 Running 재개를 시도하지만, 매 틱마다 조건부터 재확인

### 문제점

```
[Sequence] VsPlayerAttacking
  ├── BTCond_PlayerState [Attacking]     ← 플레이어가 공격 상태에서 벗어나면?
  └── BTAction_Guard                     ← 다음 0.1s 틱에 전혀 다른 분기로 점프 가능
```

현재 시스템에서는 Guard 액션이 시작된 직후 플레이어 상태가 바뀌어도, 다음 인터벌까지 Guard를 유지한다. 이는 의도된 행동(짧은 Guard)이지만, **의도치 않은 중단**이나 **Running 태스크의 부자연스러운 전환**도 가능하다.

---

## 2. Decorator — 래퍼 노드 vs 부착 리스트

### Unreal 방식

```
UBTComposite_Sequence (노드)
  ├── [Decorator] BTDecorator_Blackboard  ← 노드 자체에 List로 부착
  ├── [Decorator] BTDecorator_Cooldown
  │
  ├── Child Task A
  └── Child Task B
```

- `UBTDecorator`는 **해당 노드가 실행 가능한지** 판단하는 가드
- `CanExecute()` → false면 해당 컴포짓/태스크 전체를 스킵
- AbortType이 있어서 BB 변화 시 실행 중인 노드를 강제 중단 가능
- 하나의 노드에 **여러 Decorator** 부착 가능

### 현재 방식

```
BTInverterSO (래퍼 노드)
  └── BTCond_HasTargetSO (자식)

= 트리에서는 Inverter가 HasTarget의 부모로 존재하는 독립 노드
```

- `BTInverterSO`, `BTCooldownSO`는 **트리 안의 일반 노드**
- 에디터에서는 이번에 흡수 뷰(`AddDecoratorBadge`)로 시각적 개선을 적용했으나,
  **데이터 모델은 여전히 래퍼 노드 방식**
- AbortType 없음: 조건 변화에 반응하지 않음

### 현재 데이터 모델과 UE 차이

```
UE:
  Sequence → [Decorator: HasTarget] → [Decorator: Cooldown 1s]
             ↕ 조건 + 쿨다운을 한 노드에 다중 부착

현재:
  BTCooldownSO
    └── BTInverterSO
          └── BTSequenceSO   (= 두 데코레이터가 중첩 래퍼가 됨)
```

중첩 데코레이터가 많아질수록 트리 깊이가 늘어나고 레이아웃이 복잡해진다.

---

## 3. Service — 완전히 없음

### Unreal 방식

```
UBTComposite_Selector (노드)
  ├── [Service] BTService_DefaultFocus   ← 노드 카드 하단 스트립으로 표시
  ├── [Service] BTService_RunEQS
  │
  ├── Child A
  └── Child B
```

- 해당 컴포짓이 **활성 상태인 동안** 일정 인터벌로 `TickNode()` 호출
- 주로 BB 값 업데이트, 지각 정보 갱신에 사용
- 컴포짓에서 분리되어 있어 재사용 가능

### 현재 방식

```csharp
// BTRunner.MakeDecision() — BT Tick 전에 BB를 직접 갱신
_bb.Set(BBKey.HasTarget,        _detectionCache?.HasTarget ?? false);
_bb.Set(BBKey.DistanceToTarget, _detectionCache?.DistanceToTarget ?? float.MaxValue);
// ... 14개 키를 매 틱마다 전부 갱신
```

- `BTRunner.RefreshBlackboard()`가 Service 역할을 대신
- 단점: **모든 값을 매 틱 갱신**, 선택적 갱신 불가
- 단점: 특정 서브트리가 활성화될 때만 필요한 값도 항상 계산

### Service가 없어서 생기는 실제 문제

예) 비행 적의 공중 루프 중 지상 거리 계산은 불필요하지만, 현재 `RefreshBlackboard`에서 항상 계산한다.

---

## 4. Abort / Interrupt — 없음

### Unreal 방식

```
Selector
  ├── Sequence [Decorator: HasTarget, AbortType=LowerPriority]  ← 우선순위 높음
  │   ├── BTCond_HasTarget
  │   └── Task_Attack
  │
  └── Task_Patrol                 ← 순찰 중 타겟 감지 시 즉시 Abort → Attack으로 전환
```

AbortType:
- `Self`: 자신의 실행 중 조건이 false가 되면 스스로 중단
- `LowerPriority`: 자신의 조건이 true가 되면 하위 우선순위 형제 브랜치를 중단
- `Both`: 둘 다

### 현재 방식

Abort 개념 없음. 대신:
- 현재 상태를 `BTCond_CurrentState`로 체크하여 특정 상태이면 패스
- `TryInterruptCurrentState()` 로직을 BT 안에 직접 구현 (Circle 중 0.02 확률 공격 등)
- Running 중인 노드는 다음 틱에 재평가됨으로써 자연스럽게 "전환"

### 문제

```
[현재] 순찰 Task가 Running 상태에서 타겟이 갑자기 등장해도,
다음 MakeDecision 틱(최대 0.1s 후)까지 순찰이 계속됨.

[UE] Patrol Task에 붙은 HasTarget Decorator(AbortType=Self)가
BB.HasTarget 변화를 즉시 감지 → Patrol 즉시 중단 → Selector가 Chase 선택
```

---

## 5. 노드 타입 상세 비교

### 컴포짓

| 노드 | UE | 현재 |
|---|---|---|
| Selector | `BTComposite_Selector` | `BTSelectorSO` ✅ |
| Sequence | `BTComposite_Sequence` | `BTSequenceSO` ✅ |
| SimpleParallel | `BTComposite_SimpleParallel` | ❌ |
| RandomSelector | 없음 (3rd-party) | `BTRandomSelectorSO` ✅ |

### 데코레이터

| 노드 | UE | 현재 |
|---|---|---|
| Blackboard (조건) | `BTDecorator_Blackboard` | `BTCond_BBBool`, `BTCond_DistanceBB` ✅ |
| Inverter | `BTDecorator_Inverter` | `BTInverterSO` ✅ |
| Cooldown | `BTDecorator_Cooldown` | `BTCooldownSO` ✅ |
| Loop | `BTDecorator_Loop` | ❌ |
| ForceSuccess | `BTDecorator_ForceSuccess` | ❌ |
| TimeLimit | `BTDecorator_TimeLimit` | ❌ |
| Cone Check | `BTDecorator_ConeCheck` | ❌ |

### 태스크(액션)

| 노드 | UE | 현재 |
|---|---|---|
| Wait | `BTTask_Wait` | ❌ |
| MoveTo | `BTTask_MoveTo` (NavigationSystem 연동) | ❌ (State 머신이 담당) |
| RunBehavior | `BTTask_RunBehavior` (서브트리) | ❌ |
| PlayAnimation | `BTTask_PlayAnimation` | ❌ (Animancer 직접 연동) |
| **게임 전용** | — | `BTAction_Chase/Attack/Retreat/Circle/Guard/Charge/Flank/Patrol/Idle` ✅ |
| **Flying 전용** | — | `BTAction_TakeOff/Descend/FlyingChase/...` ✅ |

---

## 6. Blackboard 비교

### Unreal

- BB는 **에셋 파일** (`UBlackboardData`)
- 키 타입: `Object`, `Class`, `Enum`, `Int`, `Float`, `Bool`, `String`, `Vector`, `Rotator`, `Name`
- Decorator가 BB 키를 구독 → 키 변화 시 `OnBlackboardValueChange` 콜백
- `FBlackboard::SetValue<T>()` — 타입 안전 API

### 현재

- `BBKey.cs` — 문자열 상수 (`const string HasTarget = "HasTarget"`)
- `BTBlackboardSO.cs` — 키 정의 ScriptableObject (에디터용)
- `RuntimeBlackboard` — `Dictionary<string, bool/float/int/string>` 런타임 저장소
- BB 변화 감지 없음 (Notify/Observer 패턴 미구현)

**동등한 부분:** 키 정의를 SO로 외부화 + 런타임 딕셔너리 구조는 UE BB와 유사  
**없는 부분:** 타입 안전 제네릭 API, BB 변화 알림, 벡터/회전 타입 키

---

## 7. 에디터 UX 비교

### Unreal 에디터

```
┌──────────────────────────────┐
│ ! HasTarget (BB, Abort=Both) │  ← 데코레이터 스트립 (노드 상단)
├──────────────────────────────┤
│      →  Sequence             │  ← 노드 본체 (타입 + 이름)
│         공격 시퀀스           │
├──────────────────────────────┤
│ ★ DefaultFocus (Service)     │  ← 서비스 스트립 (노드 하단)
└──────────────────────────────┘
         │    │
      Task  Task
```

- 각 노드에 **실행 순서 인덱스** 표시 (0, 1, 2...)
- 실행 중 노드: 녹색 테두리, 완료: 흰색, 실패: 빨간색
- Breakpoint 지원 (특정 노드에서 일시정지)
- **서비스 스트립** (노드 카드 하단) 시각화

### 현재 에디터

```
┌───────────────────────────┐
│ ├─── [인풋 포트]           │
│ →  Sequence               │  ← 헤더 (타입 아이콘 + 타입명)
│─────────────────────────── │
│ [! Inverter]               │  ← 데코레이터 뱃지 (흡수 적용, 이번 구현)
│ HasTarget                  │  ← 노드 이름
│ [desc text]                │  ← 설명 텍스트
│ ── [아웃풋 포트]            │
└───────────────────────────┘
```

- ✅ 데코레이터 뱃지 흡수 (이번 구현)
- ✅ 런타임 색상 하이라이트 (Running=노란, Success=초록, Failure=빨강)
- ✅ Blackboard 패널 (런타임 키-값 실시간 표시)
- ❌ 실행 순서 인덱스
- ❌ 서비스 스트립
- ❌ 브레이크포인트
- ❌ 스텝 실행

---

## 8. 구조적 차이 요약

### "BT가 결정 레이어만 담당" 설계의 트레이드오프

현재 시스템은 **BT → State 전환 명령**만 내리는 경량 설계:

```
BT Tick → BTAction_Chase → controller.TransitionToState(new EnemyChaseState(...))
```

UE는 **BT Task가 직접 행동을 제어**:

```
BTTask_MoveTo → AIController.MoveToLocation() → 매 프레임 이동 계산 (Task가 Running 반환)
```

| 비교 항목 | 현재 (경량) | UE (풀 제어) |
|---|---|---|
| BT 복잡도 | 낮음 (State 머신이 실행 담당) | 높음 (BT가 직접 실행) |
| Running 활용 | 적음 | 많음 |
| State 머신 필요 | 반드시 필요 | 필요 없음 (BT가 대체) |
| AI 이동 | KCC + State | NavMesh + Task |
| 애니메이션 | Animancer + State | Animation Blueprint |

이 설계는 **기존 State Machine 재활용**에 최적화되어 있다. State Machine의 이동/물리/애니 로직을 건드리지 않고 BT를 도입한 것이 핵심 목적이므로, UE 방식으로 완전히 전환하는 것은 적합하지 않다.

---

## 9. 개선 우선순위

### 🔴 높음 — 실제 게임플레이 버그 가능성

#### Abort / Interrupt 메커니즘
현재 없는 상태. 근본적인 구현 방향:

```csharp
// 옵션 A: 간단한 "조건 실패 시 즉시 Failure 반환"
// BTCooldown처럼 조건을 매 틱 앞에서 체크하는 Decorator Guard 패턴

public class BTGuardDecorator : BTNode
{
    private readonly Func<RuntimeBlackboard, bool> _condition;
    private readonly BTNode _child;

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        if (!_condition(bb)) return NodeStatus.Failure;  // 조건 실패 → 즉시 중단
        return _child.Tick(bb);
    }
}
```

구현 비용: **중간** (런타임 노드 1개 + SO 1개)

---

### 🟡 중간 — 코드 품질 / 확장성

#### ForceSuccess Decorator
```csharp
// 런타임
protected override NodeStatus TickInternal(RuntimeBlackboard bb)
{
    _child.Tick(bb);
    return NodeStatus.Success;  // 항상 Success
}
```
구현 비용: **매우 낮음** — 런타임 5줄 + SO 10줄

---

#### Service 개념 도입

현재 `RefreshBlackboard()`를 Service로 분리하면:
- 어떤 브랜치가 활성화됐을 때만 특정 BB 키를 갱신 가능
- CPU 낭비 감소

```csharp
// 개념적 구조
public abstract class BTService
{
    public float tickInterval = 0.1f;
    public abstract void OnTick(RuntimeBlackboard bb);
}

// BTCompositeSO에 서비스 목록 추가
public class BTSelectorSO : BTNodeSO
{
    public List<BTNodeSO>  children = new();
    public List<BTService> services = new();  // ← 추가
}
```

구현 비용: **높음** (런타임 틱 관리 + 에디터 스트립 UI 필요)

---

#### BB Notify (변화 감지)
```csharp
// RuntimeBlackboard에 이벤트 추가
public event Action<string> OnValueChanged;

public void Set(string key, bool value)
{
    bool old = GetBool(key);
    _bools[key] = value;
    if (old != value) OnValueChanged?.Invoke(key);
}
```

구현 비용: **낮음** (이벤트 추가만) / 활용(Decorator 옵저버 연동)은 **높음**

---

### 🟢 낮음 — UX / 편의

#### 에디터 실행 순서 인덱스
컴포짓 노드의 자식에 `[0]`, `[1]`, `[2]` 번호를 뱃지로 표시.  
`BTNodeView.BuildBody()`에서 자식 인덱스를 받아 소형 라벨 추가.  
구현 비용: **낮음**

#### Loop Decorator
```csharp
public class BTLoop : BTNode
{
    private readonly BTNode _child;
    private readonly int    _loopCount;  // -1 = 무한
    private int             _count;

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        var status = _child.Tick(bb);
        if (status == NodeStatus.Success)
        {
            _count++;
            if (_loopCount > 0 && _count >= _loopCount)
            { _count = 0; return NodeStatus.Success; }
            return NodeStatus.Running;  // 계속 반복
        }
        return status;
    }
}
```
구현 비용: **낮음**

---

## 10. "현재 시스템에만 있는 것" (UE 대비 우위)

| 항목 | 설명 |
|---|---|
| `BTRandomSelectorSO` | 가중치 기반 랜덤 자식 선택. UE 기본에는 없음 |
| `BTCond_RandomChance` | 확률 기반 통과 조건 (0.0~1.0). UE는 직접 구현 필요 |
| `BTCond_PlayerState` | 플레이어 행동 상태 직접 참조 (TacticalMemory 연동) |
| `BTCond_OverAttacking` | 연속 공격 카운터 기반 조건 |
| `BTCond_PersonalSpace` | 적 개인 공간 침범 감지 |
| `BTCond_BBBool` + `BTCond_DistanceBB` | BB 키를 동적으로 참조하는 범용 조건 |
| `BTRunnerFlying` | 지상/공중 루프 혼합 적 전용 BT 실행기 |
| State Machine 공존 | BT가 결정만 내리고 물리/애니는 KCC+Animancer State가 담당 |

---

---

# 개선 진행방안

> 이 섹션은 Claude가 세션마다 참조해서 구현을 이어나갈 수 있도록 작성됐다.  
> 각 Phase는 독립적으로 완수 가능하며, 완료 시 해당 Phase 앞에 `✅`를 붙인다.

---

## 코드 위치 빠른 참조 (매 세션 진입점)

| 목적 | 파일 경로 |
|---|---|
| 런타임 노드 추가 | `Assets/02.Scripts/BehaviorTree/Core/BTDecorator.cs` |
| SO 노드 추가 | `Assets/02.Scripts/BehaviorTree/Data/BTDecoratorSO.cs` |
| 컴포짓 SO/런타임 | `BTCompositeSO.cs` / `BTComposite.cs` |
| Decorator 흡수 판단 | `BehaviorTreeGraphView.cs` — `IsAbsorbableDecorator()` (line ~469) |
| JSON 직렬화 지원 추가 | `BTJsonSerializer.cs` — `GetSOChildren()` + `WireChildren()` |
| 에디터 노드 표시 추가 | `BTNodeView.cs` — `GetTypeClass()`, `GetTypeName()`, `GetDescText()` |
| 컨텍스트 메뉴 추가 | `BehaviorTreeGraphView.cs` — `BuildContextualMenu()` |
| BB 키 추가 | `BBKey.cs` |
| BB Runner 갱신 | `BTRunner.cs` — `MakeDecision()` |

### 새 노드를 추가할 때마다 수정해야 하는 파일 체크리스트

```
런타임:  BTDecorator.cs (또는 BTComposite.cs)
SO:      BTDecoratorSO.cs (또는 BTCompositeSO.cs)
JSON:    BTJsonSerializer.cs → GetSOChildren() + WireChildren()
GraphView: BehaviorTreeGraphView.cs
           → GetSOChildren()          (레이아웃·엣지 순회용)
           → IsAbsorbableDecorator()  (흡수 여부, 해당하는 경우)
           → GetDecoratorChild()      (흡수 여부, 해당하는 경우)
           → BuildContextualMenu()    (Create 메뉴 항목)
에디터 뷰: BTNodeView.cs
           → GetTypeClass()           (헤더 색상 CSS 클래스)
           → GetTypeName()            (헤더 타입명)
           → GetDescText()            (본문 설명 텍스트)
USS:       BehaviorTreeEditor.uss     (필요 시 색상 추가)
```

---

## Phase 1 — 경량 데코레이터 3종 ✅

**ForceSuccess + Loop + BTTask_Wait**  
서로 독립적이므로 어느 순서든 무관. 합산 작업량 소.

---

### 1-A. ForceSuccess Decorator ⬜

**목적:** 자식이 Failure를 반환해도 Success로 바꿔 상위 Sequence가 계속 진행되도록 한다.  
`if (tried == false) { still proceed; }` 패턴에 사용.

**런타임 — `BTDecorator.cs` 말미에 추가:**

```csharp
public class BTForceSuccess : BTNode
{
    private readonly BTNode _child;
    public BTForceSuccess(string name, BTNode child) { NodeName = name; _child = child; }

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        _child.Tick(bb);
        return NodeStatus.Success;
    }
}
```

**SO — `BTDecoratorSO.cs` 말미에 추가:**

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Decorator/ForceSuccess", fileName = "BTForceSuccess")]
public class BTForceSuccessSO : BTNodeSO
{
    public BTNodeSO child;

    protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
    {
        var c = child != null ? child.CreateAndBindNode(bb)
                              : new BTLeaf("Empty", _ => NodeStatus.Failure);
        return new BTForceSuccess(nodeName, c);
    }
}
```

**JSON 지원 — `BTJsonSerializer.cs` `GetSOChildren` / `WireChildren` switch에 추가:**

```csharp
// GetSOChildren
case BTForceSuccessSO fs: if (fs.child != null) list.Add(fs.child); break;

// WireChildren
case BTForceSuccessSO fs: fs.child = resolved.Count > 0 ? resolved[0] : null; break;
```

**GraphView — `BehaviorTreeGraphView.cs`:**

```csharp
// GetSOChildren (두 곳 모두)
case BTForceSuccessSO fs: if (fs.child != null) list.Add(fs.child); break;

// IsAbsorbableDecorator
BTForceSuccessSO fs => fs.child != null,

// GetDecoratorChild
BTForceSuccessSO fs => fs.child,

// AddChildToSO / RemoveChildFromSO
case BTForceSuccessSO fs: fs.child = child; break;   // Add
case BTForceSuccessSO fs: if (fs.child == child) fs.child = null; break;  // Remove

// BuildContextualMenu
evt.menu.AppendAction("Create/Decorator/ForceSuccess", _ => CreateNode(typeof(BTForceSuccessSO), mousePos));
```

**BTNodeView — 각 switch에 추가:**

```csharp
// GetTypeClass
BTForceSuccessSO => "bt-type-forcesuccess",

// GetTypeName
BTForceSuccessSO => "ForceSuccess",

// GetDescText
BTForceSuccessSO => "",   // 설명 없음
```

**USS — `BehaviorTreeEditor.uss`에 추가:**

```css
.bt-type-forcesuccess #title { background-color: #1a5080; }
```

**검증:** `[Sequence] → [ForceSuccess → BTAction_Idle] → [BTAction_Chase]` 트리를 만들고 Idle이 실패해도 Chase가 실행되는지 확인.

---

### 1-B. Loop Decorator ⬜

**목적:** 자식 노드를 N회(또는 무한) 반복 실행한다.  
순찰 루프, 공격 N회 연속 등에 활용.

**런타임 — `BTDecorator.cs` 말미에 추가:**

```csharp
public class BTLoop : BTNode
{
    private readonly BTNode _child;
    private readonly int    _count;  // -1 = 무한
    private int             _done;

    public BTLoop(string name, BTNode child, int count)
    {
        NodeName = name; _child = child; _count = count;
    }

    public override void OnEnter(RuntimeBlackboard bb) => _done = 0;

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        var s = _child.Tick(bb);
        if (s == NodeStatus.Failure) return NodeStatus.Failure;
        if (s == NodeStatus.Success)
        {
            _done++;
            if (_count >= 0 && _done >= _count) { _done = 0; return NodeStatus.Success; }
            // 아직 반복 중
        }
        return NodeStatus.Running;
    }
}
```

> **주의:** Loop가 Running을 반환하려면 BT가 "Running 상태 유지" 로직을 타야 한다.  
> 현재 `BTRunner.MakeDecision()`은 매 인터벌마다 `_runtimeTree.Tick(_bb)`를 호출한다.  
> Sequence의 `_runningIndex`가 Loop 노드에서 멈춰있으면 다음 Tick에 Loop.Tick이 재호출된다 — **정상 동작**.

**SO — `BTDecoratorSO.cs` 말미에 추가:**

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Decorator/Loop", fileName = "BTLoop")]
public class BTLoopSO : BTNodeSO
{
    [Tooltip("-1 = 무한 반복")]
    public int  loopCount = 3;
    public BTNodeSO child;

    protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
    {
        var c = child != null ? child.CreateAndBindNode(bb)
                              : new BTLeaf("Empty", _ => NodeStatus.Failure);
        return new BTLoop(nodeName, c, loopCount);
    }
}
```

**JSON/GraphView/NodeView:** ForceSuccess와 동일한 패턴 적용.  
`GetDescText` 케이스: `BTLoopSO l => l.loopCount < 0 ? "∞" : $"× {l.loopCount}"`  
흡수(IsAbsorbable): Loop도 single-child이므로 **흡수 대상으로 추가한다.**

**검증:** `[Loop ×3] → [BTAction_Retreat]` 노드를 추가하고 상태 전환이 3회 발생 후 Success 반환되는지 확인.

---

### 1-C. BTTask_Wait ⬜

**목적:** 지정 시간 동안 아무 것도 하지 않고 Running을 유지하는 태스크.  
이 노드가 있어야 **Phase 2 Guard**가 의미를 갖는다(Running 중 조건 재평가).

> 현재 모든 Action 노드는 Success를 즉시 반환한다.  
> Wait는 최초로 **진정한 Running 태스크**가 된다.

**신규 파일: `Assets/02.Scripts/BehaviorTree/Nodes/Actions/BTAction_Wait.cs`:**

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Action/Wait", fileName = "BTAction_Wait")]
public class BTAction_WaitSO : BTNodeSO
{
    [Min(0f)] public float duration = 1f;
    [Tooltip("±범위로 랜덤화. 0이면 고정.")]
    public float randomDeviation = 0f;

    protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
    {
        float dur = duration;
        float dev = randomDeviation;
        float endTime = -1f;

        return new BTLeaf(nodeName, b =>
        {
            if (endTime < 0f)
                endTime = Time.time + dur + Random.Range(-dev, dev);

            if (Time.time < endTime)
                return NodeStatus.Running;

            endTime = -1f;
            return NodeStatus.Success;
        });
    }
}
```

> **⚠ 주의:** BTLeaf 람다 클로저에 `endTime` 로컬 변수를 캡처하므로  
> 인스턴스마다 독립적인 타이머를 가진다. SO를 공유해도 런타임 인스턴스는 독립적.

**JSON/GraphView/NodeView:** 다른 Action 노드와 동일한 패턴 (자식 없음).

**검증:** `[Wait 2s]` 단독 노드를 루트에 두고 에디터 런타임 하이라이트로 2초간 Running(노란) → Success(초록) 전환 확인.

---

## Phase 2 — Guard Decorator (조건 재평가) ✅

**전제:** Phase 1-C (`BTTask_Wait`) 완료 후 진행.  
Wait처럼 Running을 반환하는 태스크가 있어야 Guard의 실제 가치가 생긴다.

**목적:** Sequence와 달리, 자식이 Running 중일 때도 **매 Tick마다 조건을 재확인**한다.  
조건이 false가 되면 자식을 즉시 중단(Failure 반환).

**동기:**

```
현재 BTSequence 동작:
  Tick1: [Cond → Success] [Action → Running]  → _runningIndex=1
  Tick2: 조건 건너뜀, [Action → Running] 계속 → 조건 변화 무시됨 ❌

BTGuard 동작:
  Tick1: _condition(bb) → true  → _child.Tick → Running
  Tick2: _condition(bb) → false → return Failure (자식 즉시 중단) ✅
```

**런타임 — `BTDecorator.cs` 말미에 추가:**

```csharp
public class BTGuard : BTNode
{
    private readonly BTNode          _condNode;  // 조건 노드 (leaf)
    private readonly BTNode          _child;

    public BTGuard(string name, BTNode condNode, BTNode child)
    {
        NodeName  = name;
        _condNode = condNode;
        _child    = child;
    }

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        // 조건을 매 Tick 재평가
        if (_condNode.Tick(bb) == NodeStatus.Failure)
            return NodeStatus.Failure;

        return _child.Tick(bb);
    }
}
```

**SO — `BTDecoratorSO.cs` 말미에 추가:**

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Decorator/Guard", fileName = "BTGuard")]
public class BTGuardSO : BTNodeSO
{
    [Tooltip("매 Tick 재평가할 조건 노드 (Condition leaf만 연결)")]
    public BTNodeSO condition;
    [Tooltip("조건 통과 시 실행할 자식")]
    public BTNodeSO child;

    protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
    {
        var condNode  = condition?.CreateAndBindNode(bb)
                        ?? new BTLeaf("AlwaysTrue", _ => NodeStatus.Success);
        var childNode = child?.CreateAndBindNode(bb)
                        ?? new BTLeaf("Empty",      _ => NodeStatus.Failure);
        return new BTGuard(nodeName, condNode, childNode);
    }
}
```

**JSON 지원 — `BTJsonSerializer.cs`:**

```csharp
// GetSOChildren — condition + child 순서로 수집
case BTGuardSO g:
    if (g.condition != null) list.Add(g.condition);
    if (g.child     != null) list.Add(g.child);
    break;

// WireChildren — 순서 기반 복원
case BTGuardSO g:
    g.condition = resolved.Count > 0 ? resolved[0] : null;
    g.child     = resolved.Count > 1 ? resolved[1] : null;
    break;
```

**GraphView:**  
Guard는 child가 2개(condition + child)이므로 **흡수 대상이 아니다** (단일 child가 아님).  
`GetSOChildren`에만 추가하면 된다. 에디터에서는 일반 노드로 렌더링.

**BTNodeView:**

```csharp
// GetTypeClass
BTGuardSO => "bt-type-guard",

// GetTypeName
BTGuardSO => "Guard",

// GetDescText
BTGuardSO g => g.condition != null ? $"if {g.condition.nodeName}" : "no condition",
```

**USS:**

```css
.bt-type-guard #title { background-color: #6b1a1a; }
```

**검증:**

```
[Sequence]
  └── [Guard]
        ├── BTCond_HasTarget (condition)
        └── BTTask_Wait 3s   (child)
```

타겟이 있을 때 Guard 진입 → Wait Running → 3초 전에 타겟 소실 → 다음 Tick에 Guard Failure → Wait 즉시 중단.

---

## Phase 3 — BB Notify (값 변화 이벤트) ✅

**전제:** 독립. Phase 1, 2 없이도 구현 가능.  
Phase 4(AbortType) 구현의 기반이 된다.

**목적:** BB 키 값이 변경될 때 구독자에게 알림을 보낸다.

**`RuntimeBlackboard.cs` 수정:**

```csharp
// 기존 Set API를 변경
public event Action<string> OnBoolChanged;
public event Action<string> OnFloatChanged;

public void Set(string key, bool value)
{
    bool changed = !_bools.TryGetValue(key, out var old) || old != value;
    _bools[key] = value;
    if (changed) OnBoolChanged?.Invoke(key);
}

public void Set(string key, float value)
{
    bool changed = !_floats.TryGetValue(key, out var old) || !Mathf.Approximately(old, value);
    _floats[key] = value;
    if (changed) OnFloatChanged?.Invoke(key);
}
// int, string도 동일하게 추가
```

> **⚠ 주의:** `BTRunner.MakeDecision()`에서 매 틱 BB 값을 덮어쓰므로  
> 값이 바뀌지 않았음에도 이벤트가 발생하지 않도록 **변경 감지 로직이 필수**.

**구독 패턴 (Phase 4에서 활용):**

```csharp
// 예: Guard가 HasTarget 키를 구독
_bb.OnBoolChanged += key =>
{
    if (key == BBKey.HasTarget && !_bb.GetBool(BBKey.HasTarget))
        RequestAbort();  // Phase 4에서 구현
};
```

**검증:** `RuntimeBlackboard.Set`을 호출할 때 실제 값이 바뀐 경우에만 이벤트가 발생하는지 단위 테스트.  
(Unity Test Runner → EditMode 테스트로 작성)

---

## Phase 4 — AbortType (Observe & Abort) ✅

**전제:** Phase 3 (BB Notify) 완료 후 진행.

**목적:** Guard Decorator에 `AbortType` 프로퍼티를 추가하여,  
BB 키 변화를 감지하면 실행 중인 자식을 중단시킨다.

**설계:**

```
AbortType:
  None          — 조건 변화 무시 (현재 동작과 동일)
  Self          — 자신의 자식이 Running 중일 때 조건이 false가 되면 즉시 Failure 반환
  LowerPriority — (미래 확장) 형제 노드 순서까지 제어 필요 — 1차 구현 범위 제외
```

**BTGuardSO에 추가:**

```csharp
public enum AbortType { None, Self }

[CreateAssetMenu(...)]
public class BTGuardSO : BTNodeSO
{
    public BTNodeSO condition;
    public BTNodeSO child;
    public AbortType abortType = AbortType.None;  // 추가
    public string    observeKey = "";             // 감지할 BB 키 이름 (AbortType.Self 시)
    ...
}
```

**BTGuard 런타임 수정:**

```csharp
public class BTGuard : BTNode
{
    private readonly BTNode    _condNode;
    private readonly BTNode    _child;
    private readonly AbortType _abortType;
    private readonly string    _observeKey;
    private bool               _abortRequested;
    private RuntimeBlackboard  _bb;

    public override void OnEnter(RuntimeBlackboard bb)
    {
        _bb = bb;
        _abortRequested = false;
        if (_abortType == AbortType.Self && !string.IsNullOrEmpty(_observeKey))
            bb.OnBoolChanged += OnBBChanged;  // 구독
    }

    public override void OnExit(RuntimeBlackboard bb)
    {
        if (_abortType == AbortType.Self)
            bb.OnBoolChanged -= OnBBChanged;  // 해제
    }

    private void OnBBChanged(string key)
    {
        if (key == _observeKey && _condNode.Tick(_bb) == NodeStatus.Failure)
            _abortRequested = true;
    }

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        if (_abortRequested) { _abortRequested = false; return NodeStatus.Failure; }
        if (_condNode.Tick(bb) == NodeStatus.Failure) return NodeStatus.Failure;
        return _child.Tick(bb);
    }
}
```

> **⚠ 주의:** `OnEnter` / `OnExit`는 현재 BTNode에 있지만, 실제 호출 시점 확인 필요.  
> 현재 `BTSequence.TickInternal`이 Running 재개 시 해당 자식의 `OnEnter`를 **호출하지 않는다**.  
> 이 Phase 전에 `BTComposite`가 Running → 재진입 시 `OnEnter`를 호출하도록 수정해야 할 수 있다.

**에디터 — BTNodeView.GetDescText 케이스 추가:**

```csharp
BTGuardSO g when g.abortType != AbortType.None
    => $"if {g.condition?.nodeName ?? "?"} [{g.abortType}]",
```

**검증:** Phase 2 Guard 검증 시나리오에서 `abortType = Self`, `observeKey = "HasTarget"` 설정.  
타겟 소실 → `OnBoolChanged` 발생 → `_abortRequested = true` → 다음 Tick Failure 반환.

---

## Phase 5 — Service 경량 구현 ✅

**전제:** 독립. 언제든 구현 가능.

**목적:** 컴포짓 노드가 활성 상태일 때만 특정 BB 키를 갱신하도록 분리.  
현재 `BTRunner.MakeDecision()`이 매 틱 모든 값을 갱신하는 낭비를 줄인다.

**설계 원칙 (경량):**
- 완전한 UE Service가 아닌, **"BB 갱신 람다 목록"** 수준으로 구현
- 컴포짓 SO에 `List<BTServiceSO> services` 추가
- 컴포짓이 Tick될 때 services를 먼저 실행하고, 이후 자식 평가

**신규 추상 클래스 `BTServiceSO.cs`:**

```csharp
// Assets/02.Scripts/BehaviorTree/Data/BTServiceSO.cs
public abstract class BTServiceSO : ScriptableObject
{
    public string serviceName = "Service";
    [Min(0.05f)] public float tickInterval = 0.1f;

    [NonSerialized] private float _lastTick = -999f;

    public void TryTick(RuntimeBlackboard bb)
    {
        if (Time.time - _lastTick < tickInterval) return;
        _lastTick = Time.time;
        OnTick(bb);
    }

    protected abstract void OnTick(RuntimeBlackboard bb);
}
```

**BTSelectorSO / BTSequenceSO 수정:**

```csharp
public class BTSelectorSO : BTNodeSO
{
    public List<BTNodeSO>  children = new();
    public List<BTServiceSO> services = new();  // 추가

    protected override BTNode CreateRuntimeNode(RuntimeBlackboard bb)
    {
        // services를 BTSelector에 전달
        var svcList = services.Where(s => s != null).ToList();
        return new BTSelector(nodeName, runtimeChildren, svcList);
    }
}
```

**BTSelector 런타임 수정:**

```csharp
public class BTSelector : BTNode
{
    private readonly List<BTNode>      _children;
    private readonly List<BTServiceSO> _services;

    protected override NodeStatus TickInternal(RuntimeBlackboard bb)
    {
        foreach (var svc in _services) svc.TryTick(bb);  // Service 먼저 실행
        // ... 기존 Selector 로직
    }
}
```

**구체적 Service 예시 — `BTService_UpdatePerception.cs`:**

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Service/UpdatePerception")]
public class BTService_UpdatePerceptionSO : BTServiceSO
{
    protected override void OnTick(RuntimeBlackboard bb)
    {
        bb.Set(BBKey.HasTarget,        bb.Detection?.HasTarget        ?? false);
        bb.Set(BBKey.DistanceToTarget, bb.Detection?.DistanceToTarget ?? float.MaxValue);
    }
}
```

이후 `BTRunner.MakeDecision()`에서는 Service에 위임된 키들을 제거한다.

**에디터 — BTNodeView 서비스 스트립:**  
`BTSelectorSO`, `BTSequenceSO`의 서비스 목록을 노드 카드 하단에 표시.

```csharp
// BTNodeView.BuildBody() 말미에 추가 (서비스가 있을 때만)
private void BuildServiceStrips(List<BTServiceSO> services)
{
    foreach (var svc in services)
    {
        var strip = new VisualElement();
        strip.AddToClassList("bt-service-strip");
        strip.Add(new Label($"★ {svc.serviceName}  {svc.tickInterval:F2}s"));
        extensionContainer.Add(strip);
    }
}
```

**USS 추가:**

```css
.bt-service-strip {
    background-color: #1a3a1a;
    border-top-width: 1px;
    border-top-color: #3a7a3a;
    padding: 3px 8px;
    font-size: 10px;
    color: #6ec06e;
}
```

**검증:** BTService_UpdatePerception을 루트 Selector에 부착하고,  
`BTRunner.MakeDecision()`에서 HasTarget/DistanceToTarget 갱신 코드를 제거한 후 동작 확인.

---

## Phase 6 — 에디터 개선 ⬜

**전제:** 독립. 언제든 구현 가능. 게임플레이에 영향 없음.

### 6-A. 실행 순서 인덱스 배지

컴포짓 노드의 각 자식 연결 엣지에 순서 번호를 표시한다.  
UE 에디터에서 `[0]`, `[1]`, `[2]` 형태로 보여주는 것.

**`BTNodeView.cs`에 추가:**

```csharp
public void SetChildIndex(int index)
{
    var badge = new Label($"[{index}]");
    badge.AddToClassList("bt-child-index");
    // inputContainer 위쪽에 삽입
    inputContainer.Insert(0, badge);
}
```

**`BehaviorTreeGraphView.ConnectEdges()`에서 호출:**

```csharp
// 컴포짓의 자식을 연결할 때 인덱스 전달
for (int idx = 0; idx < resolvedChildren.Count; idx++)
{
    if (_nodeViews.TryGetValue(resolvedChildren[idx], out var cv))
        cv.SetChildIndex(idx);
}
```

### 6-B. 브레이크포인트 (간이)

노드를 더블클릭하면 `BTNode.BreakpointEnabled = true` 설정.  
런타임 Tick 시 브레이크포인트 노드 진입 직전에 `Debug.Break()` 호출.

**`BTNode.cs`에 추가:**

```csharp
public bool BreakpointEnabled { get; set; }

public NodeStatus Tick(RuntimeBlackboard bb)
{
    if (BreakpointEnabled)
    {
        Debug.Log($"[BT Breakpoint] {NodeName}");
        Debug.Break();
    }
    LastStatus = TickInternal(bb);
    return LastStatus;
}
```

**`BTNodeView.cs` — 더블클릭 이벤트 등록:**

```csharp
// 생성자에서
RegisterCallback<MouseDownEvent>(e =>
{
    if (e.clickCount == 2 && _runtimeNode != null)
    {
        _runtimeNode.BreakpointEnabled = !_runtimeNode.BreakpointEnabled;
        EnableInClassList("bt-breakpoint", _runtimeNode.BreakpointEnabled);
    }
});
```

---

## Phase 체크리스트 요약

```
Phase 1-A  ForceSuccess Decorator          ✅
Phase 1-B  Loop Decorator                  ✅
Phase 1-C  BTTask_Wait                     ✅

Phase 2    Guard Decorator (재평가)        ✅

Phase 3    BB Notify 이벤트               ✅

Phase 4    AbortType (Self)               ✅

Phase 5    Service 경량 구현              ✅

Phase 6-A  실행 순서 인덱스 배지          ✅
Phase 6-B  브레이크포인트 (간이)          ✅
```

### 구현 시 주의사항 (공통)

1. **새 노드 추가 시 체크리스트 6곳 반드시 확인** (문서 상단 참조)
2. **BTTask_Wait 이전에는 Loop의 Running 반환이 실제로 Tick되지 않는다**  
   (MakeDecision이 새 틱에서 Tick을 호출하므로 실제로는 동작하나, 테스트 필요)
3. **Phase 4 구현 전 BTComposite의 Running 재진입 시 OnEnter 호출 여부 확인 필요**  
   현재 `BTSequence/BTSelector`는 `_runningIndex`로 재개하지만 `OnEnter`를 재호출하지 않음
4. **BTJsonSerializer는 자식 순서에 의존한다** — `GetSOChildren`과 `WireChildren`의 순서가 반드시 일치해야 함
