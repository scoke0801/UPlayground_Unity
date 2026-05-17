# BT 노드 카탈로그

`BehaviorTreeJsonUtility.ImportFromJsonFile`이 인식할 수 있는 모든 노드와 reflection으로 직렬화되는 필드 목록. JSON을 만들 때 **반드시 여기 등록된 노드만 사용한다.** 등록되지 않은 노드를 임의로 만들면 `Type.GetType()` 실패로 무음 누락된다.

표기 규칙:
- `type` 열의 값을 그대로 JSON `nodes[].type`에 넣는다 (모두 `, Assembly-CSharp` 어셈블리)
- "직렬화 필드" 열에 없는 필드는 JSON으로 옮길 수 없다 (필요하면 에디터에서 수동 입력)
- "기본값" 열은 필드의 코드 상 초기값 — JSON에서 그 값을 원하면 생략 가능

---

## Composite (분기/조합)

### SelectorNode
"위에서 아래로 한 자식이 Success할 때까지 시도". 우선순위 분기에 사용.

- type: `UPlayGround.AI.BehaviorTree.SelectorNode`
- 자식 수: ≥ 1
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_abortType` | BTAbortType | None | Self/LowerPriority/Both 중 하나로 조건부 abort 활성화 |

### SequenceNode
"모든 자식이 차례로 Success해야 Success. 하나라도 Failure면 즉시 Failure". "조건 → 행동" 쌍.

- type: `UPlayGround.AI.BehaviorTree.SequenceNode`
- 자식 수: ≥ 1
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_abortType` | BTAbortType | None | 조건부 abort |

### ParallelNode
모든 자식을 동시에 tick. `_requireAllSuccess`에 따라 종료 조건이 다름.

- type: `UPlayGround.AI.BehaviorTree.ParallelNode`
- 자식 수: ≥ 1
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_requireAllSuccess` | bool | True | true=모두 Success해야 Success / false=하나만 Success해도 Success |

### WeightedRandomSelectorNode
가중치 기반 자식 선택. 실패 시 남은 자식 중에서 재추첨.

- type: `UPlayGround.AI.BehaviorTree.WeightedRandomSelectorNode`
- 자식 수: ≥ 1
- 직렬화 필드: 없음 (`_weights`는 `List<float>`라 reflection 경로에서 직렬화되지 않는다 — 가중치는 에디터에서 직접 설정 필요)
- **JSON 작성 시 주의:** weights를 JSON으로 전달할 수 없으므로 결과 메시지에 "에디터에서 weights 설정 필요" 안내를 반드시 포함한다.

---

## Decorator (자식 1개를 감싸는 래퍼)

모든 Decorator는 **자식 정확히 1개** 필요.

### InverterNode
자식의 Success ↔ Failure 반전. Running은 그대로.

- type: `UPlayGround.AI.BehaviorTree.InverterNode`
- 직렬화 필드: 없음

### CooldownNode
자식이 Success한 뒤 지정한 초 동안 자식 실행을 차단(=Failure 반환).

- type: `UPlayGround.AI.BehaviorTree.CooldownNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_cooldown` | float | 1.0 | 쿨다운 초 |

### RepeatNode
자식을 N번 반복. 0 이하면 무한.

- type: `UPlayGround.AI.BehaviorTree.RepeatNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_repeatCount` | int | 1 | 반복 횟수, ≤0이면 무한 |

### TimeoutNode
자식이 지정 초 안에 끝내지 못하면 Abort + Failure.

- type: `UPlayGround.AI.BehaviorTree.TimeoutNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_timeout` | float | 1.0 | 제한 시간(초) |

### ReturnSuccessNode
자식 결과를 항상 Success로 변환 (Running은 그대로).

- type: `UPlayGround.AI.BehaviorTree.ReturnSuccessNode`
- 직렬화 필드: 없음

### ReturnFailureNode
자식 결과를 항상 Failure로 변환.

- type: `UPlayGround.AI.BehaviorTree.ReturnFailureNode`
- 직렬화 필드: 없음

### UntilSuccessNode
자식이 Success할 때까지 반복 (Failure면 자식 reset 후 Running 유지).

- type: `UPlayGround.AI.BehaviorTree.UntilSuccessNode`
- 직렬화 필드: 없음

### UntilFailureNode
자식이 Failure할 때까지 반복 (Failure 발생 시 Success로 종료).

- type: `UPlayGround.AI.BehaviorTree.UntilFailureNode`
- 직렬화 필드: 없음

### GuardConditionNode ⚠ JSON 한계
Blackboard bool 키가 기대값과 같을 때만 자식 실행.

- type: `UPlayGround.AI.BehaviorTree.GuardConditionNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_expectedValue` | bool | True | 키가 이 값일 때 자식 실행 |
- **JSON 한계:** `_key` (BlackboardKeySelector struct)는 reflection 경로에서 직렬화되지 않음 → 에디터에서 키 선택 필요. 사용자에게 안내할 것.

### ForceAbortNode ⚠ JSON 한계
Blackboard bool 키가 트리거 값으로 "변화"하면 자식을 강제 Abort.

- type: `UPlayGround.AI.BehaviorTree.ForceAbortNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_triggerOn` | bool | True | true가 됐을 때 abort할지 false가 됐을 때 abort할지 |
- **JSON 한계:** `_key`는 BlackboardKeySelector라 JSON 불가 → 에디터 수동 설정.

---

## Action (잎 노드, 실제 동작)

### WaitNode
지정 초 동안 Running, 시간 지나면 Success.

- type: `UPlayGround.AI.BehaviorTree.WaitNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_duration` | float | 1.0 | 대기 시간(초) |

### LogNode
디버그 메시지 출력 후 Success.

- type: `UPlayGround.AI.BehaviorTree.LogNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_message` | string | "Behavior Tree Log" | 출력 문자열 |
  | `_logEveryTick` | bool | False | true면 매 Tick 로그 |

### SetBlackboardValueNode
Blackboard 키에 값을 쓴다. 1회 Success.

- type: `UPlayGround.AI.BehaviorTree.SetBlackboardValueNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_key` | string | "" | 키 이름 |
  | `_valueType` | BlackboardValueType | Bool | 어떤 타입을 쓸지 |
  | `_boolValue` | bool | False | _valueType=Bool일 때 |
  | `_intValue` | int | 0 | _valueType=Int일 때 |
  | `_floatValue` | float | 0 | _valueType=Float일 때 |
  | `_stringValue` | string | "" | _valueType=String일 때 |
  | `_vector3Value` | Vector3 | (0,0,0) | _valueType=Vector3일 때 |
- **Object 타입(_objectValue)은 JSON으로 불가** — Object 참조는 에디터에서 직접 드래그 필요.

### SyncEnemyBlackboardNode
EnemyDetection/ActorMovementController 값을 Blackboard에 1회 동기화 후 Success. 전투 분기 진입 직전에 두면 분기 가독성 ↑.

- type: `UPlayGround.AI.BehaviorTree.SyncEnemyBlackboardNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_hasTargetKey` | string | "HasTarget" | bool 키 |
  | `_targetKey` | string | "Target" | Object 키 |
  | `_distanceKey` | string | "DistanceToTarget" | float 키 |
  | `_stateKey` | string | "CurrentState" | string 키 |

### TransitionEnemyStateNode (지상 적)
지정한 적 State로 전환. 가장 자주 쓰는 Action.

- type: `UPlayGround.AI.BehaviorTree.TransitionEnemyStateNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_targetState` | EnemyTransitionStateType | Idle | Idle/Patrol/Chase/Attack/Retreat/Dodge/Circle/Guard/Charge/Flank/Counter |
  | `_skipIfAlreadyInState` | bool | True | 이미 같은 State면 Success로 건너뜀 |

### TransitionFlyingEnemyStateNode (비행 적) ⚠ JSON 한계
비행형 적 State 전환.

- type: `UPlayGround.AI.BehaviorTree.TransitionFlyingEnemyStateNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_skipIfAlreadyInState` | bool | True | 동일 |
- **JSON 한계:** `_targetState` (FlyingEnemyTransitionStateType) — 이 enum은 `SupportedPropertyTypes`에 포함되지 않아 reflection 직렬화 불가. 에디터에서 직접 설정 필요.

### ExecuteEnemyAttackNode
조건 충족 시 EnemyAttackState로 진입. Attack 중에는 Running, 슬롯 부족 등은 Failure.

- type: `UPlayGround.AI.BehaviorTree.ExecuteEnemyAttackNode`
- 직렬화 필드: 없음

### RequestEnemyAttackSlotNode
그룹 공격 슬롯을 요청. 성공/실패에 따라 Blackboard `HasAttackSlot` 갱신.

- type: `UPlayGround.AI.BehaviorTree.RequestEnemyAttackSlotNode`
- 직렬화 필드: 없음

### KeepCurrentStateNode
무한 Running. "현재 State를 유지하면서 트리가 끝나지 않게 막을 때" 사용.

- type: `UPlayGround.AI.BehaviorTree.KeepCurrentStateNode`
- 직렬화 필드: 없음

### SubtreeNode ⚠ JSON 한계
다른 `BehaviorTreeAsset`을 자식 트리처럼 실행.

- type: `UPlayGround.AI.BehaviorTree.SubtreeNode`
- 직렬화 필드: 없음 (`_subtreeAsset`은 Unity Object 참조라 JSON 불가)
- **JSON 한계:** Subtree Asset 참조는 에디터에서 인스펙터로 직접 드래그.

### ResetFlyingCountersNode / ResetFlyingAirCountersNode / DescendFlyingNode / SelectFlyingDiveSkillNode / RequestFlyingAttackSlotNode
비행 적 전용 Action. 직렬화 필드 없음. type 문자열만 카탈로그에서 가져와 사용.

- `UPlayGround.AI.BehaviorTree.ResetFlyingCountersNode`
- `UPlayGround.AI.BehaviorTree.ResetFlyingAirCountersNode`
- `UPlayGround.AI.BehaviorTree.DescendFlyingNode`
- `UPlayGround.AI.BehaviorTree.SelectFlyingDiveSkillNode`
- `UPlayGround.AI.BehaviorTree.RequestFlyingAttackSlotNode`

---

## Condition (잎 노드, 판정)

### HasTargetNode
EnemyDetection.HasTarget이 기대값과 같으면 Success.

- type: `UPlayGround.AI.BehaviorTree.HasTargetNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_expectedValue` | bool | True | 비교 대상 |

### IsTargetInRangeNode
타겟과의 거리가 조건에 맞으면 Success.

- type: `UPlayGround.AI.BehaviorTree.IsTargetInRangeNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_comparison` | FloatComparisonType | LessOrEqual | LessOrEqual(≤_maxDistance) / GreaterOrEqual(≥_minDistance) / Between(_min ≤ d ≤ _max) |
  | `_minDistance` | float | 0 | |
  | `_maxDistance` | float | 3 | |

### IsCurrentActorStateNode
현재 State 이름이 기대값과 같은지.

- type: `UPlayGround.AI.BehaviorTree.IsCurrentActorStateNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_stateName` | string | "Idle" | 비교할 State 이름 (예: "Attack", "Chase", "Flying_AirCircle") |
  | `_expectedValue` | bool | True | true=일치하면 Success / false=다르면 Success |

### BlackboardBoolConditionNode
Blackboard bool 키 값이 기대값과 같으면 Success.

- type: `UPlayGround.AI.BehaviorTree.BlackboardBoolConditionNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_key` | string | "" | Blackboard 키 |
  | `_expectedValue` | bool | True | |

### IsEnemyPatrolEnabledNode
EnemyAIContext.EnablePatrol을 그대로 반환.

- type: `UPlayGround.AI.BehaviorTree.IsEnemyPatrolEnabledNode`
- 직렬화 필드: 없음

### IsEnemyPhaseNode
현재 페이즈가 지정 페이즈와 일치하는지. `_phaseName`이 비어있지 않으면 이름 기준, 아니면 `_phaseIndex` 기준.

- type: `UPlayGround.AI.BehaviorTree.IsEnemyPhaseNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_phaseName` | string | "" | 비어있지 않으면 이걸로 매치 |
  | `_phaseIndex` | int | -1 | 이름이 빈 경우의 인덱스 매치 (≥0이어야) |

### CanUseEnemySkillNode
EnemyCombat.HasAvailableSkillAtDistance(현재거리)가 true면 Success.

- type: `UPlayGround.AI.BehaviorTree.CanUseEnemySkillNode`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_requireTarget` | bool | True | 타겟 없으면 Failure로 직행할지 |

### HasEnemyActionDelayElapsedNode
`Blackboard[NextActionAllowedTime]`을 지났는지 (행동 간 인터벌 게이트).

- type: `UPlayGround.AI.BehaviorTree.HasEnemyActionDelayElapsedNode`
- 직렬화 필드: 없음

### IsBlockedEnemyStateNode
현재 State가 Hit/Death/Grabbed/Airborne 등 "행동 금지" 상태인지.

- type: `UPlayGround.AI.BehaviorTree.IsBlockedEnemyStateNode`
- 직렬화 필드: 없음

### IsFlyingAirStateNode / IsFlyingGroundCombatStateNode / IsAirAttackLimitReachedNode / ShouldFlyingTakeOffNode / HasDiveSkillAvailableNode / RollDiveChanceNode / FlyingCanUseSkillNode
비행 적 전용 Condition. 직렬화 필드 없음.

- `UPlayGround.AI.BehaviorTree.IsFlyingAirStateNode`
- `UPlayGround.AI.BehaviorTree.IsFlyingGroundCombatStateNode`
- `UPlayGround.AI.BehaviorTree.IsAirAttackLimitReachedNode`
- `UPlayGround.AI.BehaviorTree.ShouldFlyingTakeOffNode`
- `UPlayGround.AI.BehaviorTree.HasDiveSkillAvailableNode`
- `UPlayGround.AI.BehaviorTree.RollDiveChanceNode`
- `UPlayGround.AI.BehaviorTree.FlyingCanUseSkillNode`

---

## Service (백그라운드 폴링) ⚠ JSON 한계

Service 노드는 Composite의 `_services` 리스트에 부착되어 주기적으로 tick된다. **현재 JSON 포맷은 Composite의 `_services` 리스트를 직렬화하지 않으므로**, Service 노드를 JSON에서 만들더라도 어느 Composite에 부착할지는 에디터에서 직접 끌어다 놓아야 한다.

→ **권장:** JSON 단계에서는 Service 대신 동일 기능의 Action 노드를 Sequence 맨 앞에 두는 패턴을 쓴다 (예: `SyncEnemyBlackboardService` 대신 `SyncEnemyBlackboardNode`).

만약 그래도 Service 노드를 JSON에 포함시키려면:

### SyncEnemyBlackboardService
- type: `UPlayGround.AI.BehaviorTree.SyncEnemyBlackboardService`
- 직렬화 필드:
  | 이름 | 타입 | 기본값 | 설명 |
  |------|------|--------|------|
  | `_interval` | float | 0.5 | 호출 주기(초) — BTServiceNode 상속 |
  | `_tickOnEnter` | bool | True | Composite 진입 시 1회 즉시 tick |
  | `_hasTargetKey` | string | "HasTarget" | |
  | `_targetKey` | string | "Target" | |
  | `_distanceKey` | string | "DistanceToTarget" | |
  | `_stateKey` | string | "CurrentState" | |

### SyncEnemyMemoryService
플레이어 행동 패턴(공격 중/가드 중/스태거/회복/회피 빈도)을 Blackboard 5개 bool 키에 동기화.

- type: `UPlayGround.AI.BehaviorTree.SyncEnemyMemoryService`
- 직렬화 필드: `_interval`, `_tickOnEnter` (BTServiceNode 상속)

### SyncEnemyPhaseService
HP 기반 페이즈를 Blackboard에 동기화. HpPercent / CurrentPhaseName / PhaseIndex / AllowCharge / AllowFlank / MaxConsecutiveAttacks 등 채움.

- type: `UPlayGround.AI.BehaviorTree.SyncEnemyPhaseService`
- 직렬화 필드: `_interval`, `_tickOnEnter`

---

## Blackboard 표준 키 (지상)

`EnemyBlackboardKeys` 상수로 정의된 키 — SyncEnemyBlackboardService/Node와 SyncEnemyPhaseService가 자동으로 채우거나 다른 노드가 참조한다. 새 BT에서 동일 의미로 키를 사용할 땐 이 이름을 그대로 쓴다.

| 키 | 타입 | 의미 |
|----|------|------|
| HasTarget | Bool | 적 감지됨 |
| Target | Object | 타겟 Transform 참조 |
| DistanceToTarget | Float | 타겟과의 거리 |
| CurrentState | String | 현재 State 이름 |
| HpPercent | Float | 체력 비율 0~1 |
| CurrentPhaseName | String | 현재 페이즈 이름 |
| PhaseIndex | Int | 현재 페이즈 인덱스 |
| AllowCharge | Bool | 차지 공격 허용 |
| AllowFlank | Bool | 측면 공격 허용 |
| MaxConsecutiveAttacks | Int | 연속 공격 최대치 |
| ContinueAttackChance | Float | 다음 공격 이어갈 확률 |
| GuardChance | Float | 가드 시도 확률 |
| RetreatChance | Float | 후퇴 시도 확률 |
| IsPlayerAttacking | Bool | 플레이어 공격 중 |
| IsPlayerGuarding | Bool | 플레이어 가드 중 |
| IsPlayerStaggered | Bool | 플레이어 스태거 |
| IsPlayerRecovering | Bool | 플레이어 회복 중 |
| IsPlayerDodgingFrequently | Bool | 플레이어 회피 잦음 |
| CanUseSkill | Bool | 스킬 사용 가능 |
| HasAttackSlot | Bool | 그룹 공격 슬롯 확보 여부 |
| NextActionAllowedTime | Float | 다음 행동 허용 시점(Time.time 기준) |

## Blackboard 표준 키 (비행 전용 추가)

| 키 | 타입 | 의미 |
|----|------|------|
| AirAttackCount | Int | 현재 공중 공격 횟수 |
| AirAttackLimit | Int | 공중 공격 한도 |
| GroundTimer | Float | 지상 체류 누적 시간 |
| GroundAttackCount | Int | 지상 공격 횟수 |
