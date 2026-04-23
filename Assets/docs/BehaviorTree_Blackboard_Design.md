# BehaviorTree — Dynamic Blackboard 설계

> **✅ 구현 완료** — 설계에서 다루는 모든 항목이 적용되어 있다.

## 개요

기존 `EnemyBlackboard`(C# 프로퍼티 하드코딩)를 **UE 스타일의 동적 키-값 저장소**로 교체하여:

- 에디터에서 키 정의를 **ScriptableObject로 관리**
- 노드들이 문자열 키로 값을 읽고 씀
- Blackboard 패널이 편집 모드에서 **키 목록을 시각적으로 표시**

---

## 현재 구조 vs 새 구조

### 현재 (하드코딩)
```
EnemyBlackboard (C# class)
  public bool  HasTarget        { get; set; }
  public float DistanceToTarget { get; set; }
  ...

BTCond_HasTargetSO:
  b => b.HasTarget ? Success : Failure
```

### 새 구조 (동적)
```
BTBlackboardSO (ScriptableObject)      ← 키 정의 에셋
  keys: [ { "HasTarget", Bool }, { "DistanceToTarget", Float }, ... ]

RuntimeBlackboard (C# class)           ← 런타임 값 저장
  Dictionary<string, bool>   _bools
  Dictionary<string, float>  _floats
  Dictionary<string, int>    _ints
  Dictionary<string, string> _strings
  + 컴포넌트 참조 (Runner, Detection, Combat, Memory, Movement)

BBKey (static constants)               ← 문자열 키 상수
  const string HasTarget = "HasTarget"
  const string DistanceToTarget = "DistanceToTarget"
  ...

BTCond_HasTargetSO:
  b => b.GetBool(BBKey.HasTarget) ? Success : Failure
```

---

## 새 파일 목록

```
BehaviorTree/
├── Core/
│   ├── RuntimeBlackboard.cs    ← EnemyBlackboard 대체 (키-값 저장소)
│   └── BBKey.cs                ← 문자열 키 상수 모음
└── Data/
    ├── BlackboardKeyDefinition.cs  ← 키 정의 데이터 클래스 + enum
    └── BTBlackboardSO.cs           ← 키 정의 ScriptableObject
```

---

## BlackboardKeyType

```csharp
public enum BlackboardKeyType { Bool, Float, Int, String }
```

---

## BlackboardKeyDefinition

```csharp
[Serializable]
public class BlackboardKeyDefinition
{
    public string           keyName;
    public BlackboardKeyType keyType;
    [TextArea(1, 2)]
    public string           description;

    // 타입별 기본값
    public bool   defaultBool;
    public float  defaultFloat;
    public int    defaultInt;
    public string defaultString;
}
```

---

## BTBlackboardSO

```csharp
[CreateAssetMenu(menuName = "BehaviorTree/Blackboard")]
public class BTBlackboardSO : ScriptableObject
{
    public List<BlackboardKeyDefinition> keys;

    // RuntimeBlackboard에 기본값으로 초기화
    public void InitializeBlackboard(RuntimeBlackboard bb);

    // 키 조회
    public BlackboardKeyDefinition GetKey(string keyName);
}
```

---

## RuntimeBlackboard

```csharp
public class RuntimeBlackboard
{
    // ── 컴포넌트 참조 (타입 안전 캐시) ─────────────────
    public BTRunner              Runner    { get; set; }
    public EnemyDetection        Detection { get; set; }
    public EnemyCombat           Combat    { get; set; }
    public EnemyTacticalMemory   Memory    { get; set; }
    public ActorMovementController Movement { get; set; }

    // ── 키-값 API ───────────────────────────────────────
    public bool   GetBool  (string key, bool   def = false)
    public float  GetFloat (string key, float  def = 0f)
    public int    GetInt   (string key, int    def = 0)
    public string GetString(string key, string def = "")

    public void Set(string key, bool   value)
    public void Set(string key, float  value)
    public void Set(string key, int    value)
    public void Set(string key, string value)

    // ── 파생 프로퍼티 ────────────────────────────────────
    public bool IsActionReady
        => Time.time - GetFloat(BBKey.LastActionTime, -999f)
           >= GetFloat(BBKey.NextActionDelay, 0.5f);
    
    // ── 에디터 접근 (BTBlackboardView용) ─────────────────
    public IReadOnlyDictionary<string, bool>   Bools
    public IReadOnlyDictionary<string, float>  Floats
    public IReadOnlyDictionary<string, int>    Ints
    public IReadOnlyDictionary<string, string> Strings
}
```

---

## BBKey 상수 목록

| 카테고리 | 키 이름 | 타입 |
|---|---|---|
| **Perception** | HasTarget | Bool |
| | DistanceToTarget | Float |
| | CurrentStateName | String |
| **Action Timing** | LastActionTime | Float |
| | NextActionDelay | Float |
| **Phase** | PhaseAllowCharge | Bool |
| | PhaseAllowFlank | Bool |
| | PhaseChargeChance | Float |
| | PhaseFlankChance | Float |
| | PhaseMaxConsecutiveAttacks | Int |
| **Combat Distance** | OptimalCombatDistance | Float |
| | MaxAttackRange | Float |
| | PersonalSpaceDistance | Float |
| | MinCombatDistance | Float |
| | RetreatDistance | Float |
| **State** | ConsecutiveDefensiveCount | Int |
| | HasGuardMotion | Bool |
| **Self Stats** | SelfHPPercent | Float |

---

## BehaviorTreeSO 변경

```csharp
public class BehaviorTreeSO : ScriptableObject
{
    public BTNodeSO       rootNode;
    public BTBlackboardSO blackboard;   // ← 추가: 연결된 블랙보드 정의

    public BTNode CreateRuntimeTree(RuntimeBlackboard bb);
}
```

---

## BTNode 시그니처 변경

```csharp
// 전: EnemyBlackboard bb
// 후: RuntimeBlackboard bb
public NodeStatus Tick(RuntimeBlackboard bb)
protected abstract NodeStatus TickInternal(RuntimeBlackboard bb)
public virtual void OnEnter(RuntimeBlackboard bb) { }
public virtual void OnExit(RuntimeBlackboard bb)  { }
```

---

## BTRunner MakeDecision 변경

```csharp
// 전
_bb.HasTarget        = _detectionCache?.HasTarget ?? false;
_bb.DistanceToTarget = _detectionCache?.DistanceToTarget ?? float.MaxValue;

// 후
_bb.Set(BBKey.HasTarget,        _detectionCache?.HasTarget ?? false);
_bb.Set(BBKey.DistanceToTarget, _detectionCache?.DistanceToTarget ?? float.MaxValue);
```

---

## 노드 코드 변경 패턴

| 전 (EnemyBlackboard) | 후 (RuntimeBlackboard) |
|---|---|
| `b.HasTarget` | `b.GetBool(BBKey.HasTarget)` |
| `b.DistanceToTarget` | `b.GetFloat(BBKey.DistanceToTarget)` |
| `b.CurrentStateName` | `b.GetString(BBKey.CurrentStateName)` |
| `b.IsActionReady` | `b.IsActionReady` (파생 프로퍼티 유지) |
| `b.PhaseAllowCharge` | `b.GetBool(BBKey.PhaseAllowCharge)` |
| `b.ConsecutiveDefensiveCount` | `b.GetInt(BBKey.ConsecutiveDefensiveCount)` |

컴포넌트 참조는 그대로:
- `b.Runner`, `b.Detection`, `b.Combat`, `b.Memory`, `b.Movement`

---

## BTBlackboardView 동작

### 편집 모드 (BehaviorTreeSO.blackboard 연결 시)
```
Blackboard: EnemyBlackboard
────────────────────────────────
[BOOL]   HasTarget
[FLOAT]  DistanceToTarget
[STR]    CurrentStateName
... (전체 키 목록, 타입 색상 구분)
```

### 런타임 모드 (BTRunner 선택 시)
```
Blackboard: EnemyBlackboard (런타임)
────────────────────────────────
[BOOL]   HasTarget          True
[FLOAT]  DistanceToTarget   2.34
[STR]    CurrentStateName   Chase
...
```

---

## 구현 단계

> **모든 단계 완료 ✅**

| 순서 | 파일 | 작업 |
|---|---|---|
| 1 | `BBKey.cs` | ✅ 신규 — 키 상수 (`SelfHPPercent` 포함 18개) |
| 2 | `BlackboardKeyDefinition.cs` | ✅ 신규 — 키 정의 데이터 |
| 3 | `BTBlackboardSO.cs` | ✅ 신규 — 키 정의 SO |
| 4 | `RuntimeBlackboard.cs` | ✅ 신규 — 키-값 런타임 (`EnemyBlackboard` 대체) |
| 5 | `BTNode.cs` | ✅ 수정 — 시그니처 교체 |
| 6 | `BTLeaf/Composite/Decorator.cs` | ✅ 수정 — 시그니처 교체 |
| 7 | `BehaviorTreeSO.cs` | ✅ 수정 — `blackboard` 필드 추가 |
| 8 | `BTRunner.cs` | ✅ 수정 — `RuntimeBlackboard` 사용, `SelfHPPercent` 갱신 포함 |
| 9 | 모든 노드 (21개) | ✅ 수정 — BBKey 패턴으로 변경 (`BTCond_BBBool`, `BTCond_DistanceBB` 신규 포함) |
| 10 | `EnemyBlackboard.cs` | ✅ 삭제 완료 |
| 11 | `BTBlackboardView.cs` | ✅ 수정 — 편집/런타임 모드 키 목록 표시 |
| 12 | `BehaviorTreeEditorWindow.cs` | ✅ 수정 — Blackboard SO 연동 |

### 구현 중 추가된 파일

| 파일 | 설명 |
|---|---|
| `BTCond_BBBoolSO` | BB의 임의 bool 키를 확인하는 범용 조건 노드. `key` + `invert` 설정으로 어떤 bool 키든 사용 가능 |
| `BTCond_DistanceBBSO` | BB의 임의 거리 키를 임계값으로 사용하는 범용 거리 조건. `thresholdKey` + `multiplier` 설정 |
