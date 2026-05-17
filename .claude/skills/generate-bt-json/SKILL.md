---
name: generate-bt-json
description: UPlayGround 프로젝트의 BehaviorTreeAsset import용 BT(Behavior Tree) JSON을 생성한다. 사용자가 "BT 만들어줘", "Behavior Tree JSON", "BT json 짜줘", "적 AI 행동트리 만들어", "BT_xxx.json 생성", "근거리/원거리/비행 몬스터 BT 만들어", "보스 페이즈 BT" 등을 언급하거나, 새로운 적 AI 행동 패턴을 JSON으로 작성해야 할 때 반드시 이 스킬을 사용한다. 자연어로 받은 AI 요구사항(추격/공격/회피/순찰/페이즈 등)을 Composite/Decorator/Action/Condition/Service 노드 트리 구조로 변환하여 `BehaviorTreeJsonUtility.ImportFromJsonFile`이 그대로 읽을 수 있는 JSON 파일을 만든다. 기존 BT JSON 수정/확장에도 동일하게 사용한다.
---

# Generate BT JSON

UPlayGround의 `BehaviorTreeAsset` 시스템을 위한 JSON 파일을 작성한다. 결과 JSON은 Unity 에디터의 `UPlayGround/Character/AI/Behavior Tree Json/Import Json` 메뉴 또는 `BehaviorTreeJsonUtility.ImportFromJsonFile`로 그대로 import된다.

## 작동 방식 — 왜 이 포맷을 따라야 하나

런타임의 `BehaviorTreeJsonUtility.ImportFromData`는 다음 절차를 따른다:

1. `data.nodes` 순회 → `Type.GetType(node.type)`로 `BTNode` 파생 ScriptableObject 인스턴스 생성
2. `properties[]`를 reflection으로 필드에 대입. **각 property에서 실제로 읽히는 건 `name`(필드명)과 `value`(직렬화된 문자열)뿐이다.** 타입 변환은 C# 필드의 `FieldType`에서 가져오므로 `properties[].type` 문자열은 import에 사용되지 않는다 (문서/diff용 정보).
3. `children[]`의 guid 문자열로 부모-자식 링크 구성
4. `rootGuid`로 RootNode 지정
5. `blackboard[]`을 키 단위로 등록 + 초기값 설정

따라서 다음 3가지가 정확해야 한다: ① 노드의 `type` 문자열 (Type.GetType 대상, 잘못되면 노드 자체가 누락), ② 필드명 `_xxx` (오타 시 기본값으로 남음), ③ `value`의 직렬화 형식(enum 이름, "True"/"False", float invariant culture 등 — `DeserializeValue`가 C# 필드 타입에 맞춰 파싱). `properties[].type`은 가독성을 위해 카탈로그 값을 그대로 복사해 두면 충분하다.

## 입력 → 출력 워크플로

1. **요구사항 파악** — 자연어 입력에서 다음을 추출:
   - 적의 타입(근거리/원거리/하이브리드/비행/보스 페이즈)
   - 우선순위 분기(공격 가능 → 후퇴 → 추격 → idle 같은 fallback 순서)
   - 거리/HP/페이즈 같은 조건 임계값
   - Blackboard에 폴링이 필요한 값(타겟/거리/state)
2. **트리 구조 설계** — 보통 Root는 `SelectorNode`. "가능하면 A, 아니면 B, 아니면 idle" 패턴이 대부분. Sequence는 "조건 → 행동" 쌍에 사용. 한 번 결정한 노드를 위에서 끊고 싶다면 부모 Composite의 `_abortType`를 설정한다.
3. **노드 카탈로그 참조** — `references/node-catalog.md`에서 사용할 노드의 정확한 type AQN과 프로퍼티 필드명/타입을 가져온다. **반드시 카탈로그에 있는 노드만 사용한다.** 카탈로그에 없는 노드를 임의로 만들지 않는다.
4. **JSON 작성** — `references/example.json`의 구조를 그대로 따른다. guid는 사람이 읽을 수 있는 snake_case 문자열을 쓴다(예: `seq_attack_when_skill_available`). 위치(`position`)는 그래프 가독성을 위해 부모-자식 간 충분히 벌려 둔다 (가로 200~300, 세로 180~200 권장).
5. **저장 경로 결정** — 기본은 `Assets/10.Datas/AI/BehaviorTree/Json/BT_<이름>.json`. 비행/보스 같은 변형이 명시되면 그에 맞는 서브 분류를 사용해도 된다(`Assets/10.Datas/AI/BehaviorTree/SourceJson/Boss/` 등은 monster behavior rules 포맷 전용 폴더이므로 헷갈리지 말 것).
6. **파일 쓰기 + 안내 출력** — Write 툴로 파일 생성. 출력에는 (a) 생성 경로, (b) 트리 한 줄 요약, (c) Unity 에디터에서 import하는 메뉴 경로 안내를 포함한다.

## 출력 JSON 골격

ALWAYS 이 정확한 구조를 사용한다:

```json
{
    "rootGuid": "<root 노드의 guid>",
    "blackboard": [
        { "key": "<키 이름>", "valueType": <int 0~5>, "boolValue": false, "intValue": 0, "floatValue": 0.0, "stringValue": "", "vector3Value": {"x":0,"y":0,"z":0}, "objectAssetPath": "" }
    ],
    "nodes": [
        {
            "type": "<AQN, 예: UPlayGround.AI.BehaviorTree.SelectorNode, Assembly-CSharp>",
            "guid": "<고유 guid>",
            "displayName": "<그래프에 표시할 이름>",
            "comment": "<이 노드가 왜 존재하는지 한 줄 설명>",
            "position": { "x": 0.0, "y": 0.0 },
            "children": ["<자식 노드 guid 1>", "<자식 노드 guid 2>"],
            "properties": [
                { "name": "<필드명, 예: _abortType>", "type": "<필드 타입 AQN>", "value": "<문자열로 직렬화한 값>" }
            ]
        }
    ]
}
```

`blackboard[].valueType` 정수 매핑 (BlackboardValueType enum):

| 값 | 이름     |
|----|----------|
| 0  | Bool     |
| 1  | Int      |
| 2  | Float    |
| 3  | String   |
| 4  | Vector3  |
| 5  | Object   |

## 값 직렬화 규칙

`BehaviorTreeJsonUtility.SerializeValue`/`DeserializeValue`가 처리할 수 있는 형식은 정해져 있다.

| 필드 타입 | type 문자열 | value 표기 예 |
|-----------|------------|--------------|
| bool | `System.Boolean, mscorlib` | `"True"` / `"False"` (대문자 시작) |
| int | `System.Int32, mscorlib` | `"3"` |
| float | `System.Single, mscorlib` | `"0.8"` (InvariantCulture, 소수점은 `.`) |
| string | `System.String, mscorlib` | `"HasTarget"` |
| Vector2 | `UnityEngine.Vector2, UnityEngine.CoreModule` | `"{\"value\":{\"x\":1.0,\"y\":0.0}}"` (Vector2Wrapper로 감싼 JSON 문자열) |
| Vector3 | `UnityEngine.Vector3, UnityEngine.CoreModule` | `"{\"value\":{\"x\":1.0,\"y\":0.0,\"z\":0.0}}"` |
| BTAbortType | `UPlayGround.AI.BehaviorTree.BTAbortType, Assembly-CSharp` | `"None"`, `"Self"`, `"LowerPriority"`, `"Both"` |
| BlackboardValueType | `UPlayGround.AI.BehaviorTree.BlackboardValueType, Assembly-CSharp` | `"Bool"`, `"Int"`, `"Float"`, `"String"`, `"Vector3"`, `"Object"` |
| FloatComparisonType | `UPlayGround.AI.BehaviorTree.FloatComparisonType, Assembly-CSharp` | `"LessOrEqual"`, `"GreaterOrEqual"`, `"Between"` |
| EnemyTransitionStateType | `UPlayGround.AI.BehaviorTree.EnemyTransitionStateType, Assembly-CSharp` | `"Idle"`, `"Patrol"`, `"Chase"`, `"Attack"`, `"Retreat"`, `"Dodge"`, `"Circle"`, `"Guard"`, `"Charge"`, `"Flank"`, `"Counter"` |

**다른 타입은 reflection import 경로에서 직렬화되지 않는다** — `FlyingEnemyTransitionStateType`, `BlackboardKeySelector`(struct), `UnityEngine.Object` 참조 등은 JSON으로 안정적으로 옮길 수 없다. 따라서 다음 노드는 JSON에서 핵심 프로퍼티가 비워진 채 import 되며, 에디터에서 수동 보강이 필요하다 — JSON에 넣더라도 사용자에게 명시적으로 알린다:

- `TransitionFlyingEnemyStateNode._targetState` (FlyingEnemyTransitionStateType)
- `GuardConditionNode._key`, `ForceAbortNode._key` (BlackboardKeySelector)
- `SubtreeNode._subtreeAsset` (UnityEngine.Object 참조)

가능하면 비행 트리는 별도 요청이 없으면 짜지 않거나, 짜더라도 위 한계를 결과 메시지에서 안내한다.

## 구조 규칙 (Validator가 잡는 것)

JSON을 만들 때 다음을 반드시 지킨다 — 어기면 import 후 에디터에서 빨간 에러로 표시된다:

- **Root 필수** — `rootGuid`가 가리키는 노드가 `nodes` 안에 있어야 함
- **Composite는 자식 ≥ 1** — Sequence/Selector/Parallel/WeightedRandomSelector는 빈 children 금지
- **Decorator는 자식 정확히 1** — Inverter/Cooldown/Repeat/Timeout/ReturnSuccess/ReturnFailure/UntilSuccess/UntilFailure/GuardCondition/ForceAbort
- **Service는 children에 넣지 않는다** — Service는 Composite의 별도 `Services` 리스트에 부착되어야 한다. **하지만 현재 JSON 포맷은 Services 필드를 직렬화하지 않는다** — JSON에는 Service 노드를 `nodes[]`에 추가할 수는 있지만, 어느 Composite에 attach될지는 사용자가 에디터에서 직접 끌어다 놓아야 한다. 따라서 JSON으로 BT를 만들 땐 가능하면 Service 대신 동일 기능의 Action 노드(`SyncEnemyBlackboardNode` 등)를 Sequence 맨 앞에 두는 패턴을 권장한다.
- **WeightedRandomSelector** — `_weights` 리스트 길이가 children 수와 같아야 한다(다르면 누락분이 1.0으로 패딩되어 경고 발생).
- **`type` AQN의 짧은 형태를 쓴다** — 풀 AQN(버전/PublicKeyToken 포함)도 동작하지만, 카탈로그의 짧은 형태(`UPlayGround.AI.BehaviorTree.XYZ, Assembly-CSharp`)를 사용해 가독성을 유지한다.

## displayName / comment 작성 규칙

- `displayName`은 그래프 노드에 표시되는 라벨이다. 노드의 의도가 보이도록 작성한다 — `Branch_Attack_WhenSkillAvailable`, `Condition_TargetTooClose`처럼 의도/조건이 드러나는 이름을 선호.
- `comment`는 노드가 왜 존재하는지를 한 줄로 적는다. 게임 디자인 의도(예: "personalSpaceDistance 0.8m 이내면 후퇴")를 적어 추후 디버깅/튜닝의 단서가 되게 한다.
- 한국어로 적는다 (프로젝트 규약).

## 자주 쓰는 패턴

### 패턴 1: "타겟 있으면 전투, 없으면 순찰" 표준 골격
```
Root: SelectorNode
├─ Sequence "Combat_HasTarget"
│  ├─ Action  SyncEnemyBlackboardNode   (타겟/거리/state 폴링)
│  ├─ Condition HasTargetNode           (_expectedValue=True)
│  └─ Selector "Combat_Decision"
│     ├─ Sequence "Attack" — CanUseEnemySkill + ExecuteEnemyAttack
│     ├─ Sequence "Retreat" — IsTargetInRange(<= 0.8) + TransitionEnemyState(Retreat)
│     ├─ Sequence "Chase" — IsTargetInRange(>= 2.0) + TransitionEnemyState(Chase)
│     └─ Action TransitionEnemyState(Idle)   (fallback)
└─ Sequence "NonCombat_NoTarget"
   ├─ Condition HasTargetNode(_expectedValue=False)
   └─ Selector
      ├─ Sequence — IsEnemyPatrolEnabled + TransitionEnemyState(Patrol)
      └─ Action TransitionEnemyState(Idle)
```

이 골격은 `references/example.json`(`BT_EnemyGroundBasic_Test.json`)에 그대로 구현되어 있다. **새 BT를 짤 때는 이 예시를 출발점으로 삼고 필요한 곳만 바꾸는 것이 가장 안전하다.**

### 패턴 2: HP 기반 페이즈 분기
HP에 따라 행동이 달라지는 보스/엘리트는:
- 페이즈별 행동은 별도 `BehaviorTreeAsset`로 분리하고 `SubtreeNode`로 호출 (단 `_subtreeAsset` 참조는 JSON에서 비워지므로 사용자에게 안내 필요)
- 또는 같은 트리 내에서 `IsEnemyPhaseNode`(`_phaseName`/`_phaseIndex`)로 분기

### 패턴 3: 우선순위 가로채기 (interrupt)
"공격 중이라도 HP가 떨어지면 즉시 후퇴"가 필요하면:
- 후퇴 분기를 Selector에서 공격 분기보다 위에 배치
- Selector의 `_abortType = LowerPriority` 또는 `Both`로 설정 → 조건이 늦게 참이 되어도 실행 중인 하위 우선순위 자식을 abort

### 패턴 4: 쿨다운/제한 횟수
- `CooldownNode` (decorator): 자식이 Success한 뒤 N초 동안 자식 실행을 막는다
- `RepeatNode._repeatCount`: 자식을 N번 실행, 0 이하면 무한 반복

## 결과 출력 형식

JSON 파일 작성 후 다음 형식으로 사용자에게 보고한다:

---
**생성된 BT JSON:**
- `Assets/10.Datas/AI/BehaviorTree/Json/BT_<이름>.json`

**트리 요약:**
- Root: `<root displayName>` (`<Composite 타입>`)
- 주요 분기: `<한 줄로 어떤 결정이 어떤 순서로 일어나는지>`
- Blackboard 키: `<나열>`

**Unity에서 사용하는 법:**
1. Unity 에디터에서 `UPlayGround/Character/AI/Behavior Tree Json/Import Json` 메뉴 클릭
2. 생성된 JSON 파일을 선택
3. 저장할 `BehaviorTreeAsset` 경로(`.asset`) 지정
4. Behavior Tree Editor가 열리며 그래프가 자동 검증된다 — Error 0개 / Warning 몇 개인지 콘솔로 확인

**[수동 보강 필요]** (해당 시):
- `<노드 displayName>`: `<어떤 필드를 에디터에서 직접 채워야 하는지>`
---

## 참조 자료

- `references/node-catalog.md` — 사용 가능한 모든 BT 노드의 type AQN과 직렬화 가능한 프로퍼티 (반드시 이 카탈로그에 있는 노드만 사용)
- `references/example.json` — 실제 프로젝트에 들어 있는 `BT_EnemyGroundBasic_Test.json`의 풀 JSON. 새 BT의 골격 출발점으로 활용
