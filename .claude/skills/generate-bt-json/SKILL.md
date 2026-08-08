---
name: generate-bt-json
description: "UPlayGround 프로젝트 적 AI용 BT(Behavior Tree) JSON 파일을 새로 작성하거나 기존 것을 수정/확장한다. 다음 상황에서 반드시 이 스킬을 사용한다: (1) BT JSON 파일 생성 — '만들어줘', '짜줘', '작성해줘', '써줘', '생성해줘' 등의 동사와 'BT', 'bt', 'behavior tree', '행동트리', '행동 트리' 키워드가 함께 등장할 때. (2) 경로 명시 생성 — 'BT_xxx.json 만들어', 'SourceJson 경로에 BT 저장해줘'. (3) AI 로직의 BT 변환 — '~로직을 BehaviorTree JSON으로 표현', '적 AI가 ~하는 behavior tree JSON'. (4) 기존 BT 수정 — '기존 BT에 분기 추가', 'BT_xxx.json에 ~조건 붙여줘'. 근거리/원거리/비행/보스/페이즈/쿨다운 등 모든 적 유형과 전투 패턴에 적용. 반대로 'BehaviorTreeAsset 연결 방법', 'Behavior Tree 에디터 오류', 'EnemyBrain 디버깅' 등 JSON 파일 생성이 아닌 설명·디버깅·에디터 조작 요청에는 이 스킬을 사용하지 않는다."
---

# Generate BT JSON (Monster Behavior Rules)

UPlayGround 적 AI용 **Monster Behavior Rules JSON**을 작성한다. 이 JSON은 `Assets/10.Datas/AI/BehaviorTree/SourceJson/`에 저장하고, Unity 에디터의 `UPlayGround/비헤이비어 트리/JSON/` 메뉴(또는 `MonsterBehaviorTreeJsonImporter`)로 import하면 `Generated/`에 `BehaviorTreeAsset`이 생성된다.

## ⭐ 가장 중요한 규칙 — 스코어러를 실제로 소비하라

이 프로젝트는 **스코어러 표준화**를 결정했다. import 시 `MonsterBehaviorTreeJsonImporter`가 루트 Selector에 `EvaluateEnemyCombatIntentService`(9-Intent 스코어러)를 **자동 부착**한다 — 이 스코어러가 매 틱 `Decision.SelectedIntent`를 계산해 blackboard에 쓴다.

**그러나 서비스가 붙는 것만으로는 부족하다.** 트리에 `SelectedIntent` 값을 분기 조건으로 읽는 규칙이 없으면 스코어러 출력은 계산만 되고 무시된다 = 스코어러 우회(과거 raw BT-node 포맷의 결함)를 새 포맷에서 재현하는 것.

→ **모든 Ground 트리는 `"40 Execute Selected Intent"` 그룹을 반드시 포함한다.** 각 CombatIntent(`Attack`/`Punish`/`Counter`/`Pressure`/`KeepDistance`/`Defend`/`Retreat`/`Chase`/`Recover`)에 대해 `{ "condition": "SelectedIntent", "value": "..." }` 규칙을 두고 대응 행동을 실행한다. `references/example.json`의 40번 그룹이 정확한 형태다. 이 그룹이 스코어러를 행동에 연결하는 **부하지지(load-bearing) 요소**다. 생략 금지.

(비행 트리 `actorKind: "Flying"`은 스코어러/메모리/페이즈 서비스가 부착되지 않으므로 이 규칙에서 예외.)

## 작동 방식 — 왜 이 포맷인가

- raw BT-node JSON(노드/guid를 직접 나열)과 달리, Rules JSON은 **고수준 선언형**이다: `groups → rules → when(조건) / do(행동) | select+choices`.
- 거리/관찰/페이즈 동기화 서비스와 스코어러는 임포터가 자동으로 붙인다. 작성자는 **결정 규칙만** 선언한다.
- 그룹/규칙은 `priority` 내림차순으로 평가된다(루트 Selector). 위에서부터 `when`이 모두 참인 첫 규칙이 실행된다.

## 입력 → 출력 워크플로

1. **요구사항 파악** — 적 유형(근접/원거리/하이브리드/비행/보스), 거리/HP/페이즈 임계값, 우선순위(생존 → 긴급반응 → 펀시 → 플레이어리드 → SelectedIntent → 기본리듬).
2. **카탈로그 참조** — `references/rules-catalog.md`에서 사용할 condition/action 키, 스코프, `value` 의미, enum 값을 확인. **카탈로그에 없는 키/enum 값은 쓰지 않는다**(Validator가 막는다).
3. **예제에서 출발** — `references/example.json`(Ground melee)을 출발점으로 삼고 필요한 곳만 바꾼다. 특히 `"40 Execute Selected Intent"` 그룹은 유지한다.
4. **그룹 계층 구성** (권장 표준):
   - `00 Survival And Interrupt` — `IsBlockedEnemyState`→`KeepCurrentState`, 무타겟→`PatrolOrIdle`
   - `10 Emergency Reactions` — PoiseBreak/연속피격 탈출
   - `20 Punish Windows` — `IsPlayerStaggered`/`IsPlayerRecovering` 펀시
   - `30 Player Read Reactions` — `IsPlayer*Frequently` 대응
   - `40 Execute Selected Intent` — **필수** (위 ⭐)
   - `50 Default Combat Rhythm` — 거리/콤보 기반 기본 행동 + `KeepCurrentState` fallback
5. **저장 경로 결정** — 기본은 `Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_<이름>.json`. (보스 페이즈 변형은 `SourceJson/Boss/` 등 하위 폴더 가능.)
6. **파일 쓰기** — Write 툴로 저장.
7. **정적 검증 (필수)** — 저장 직후 반드시 실행한다. 오류가 남은 채로 완료 보고하지 않는다.
   ```powershell
   python ".claude/skills/generate-bt-json/scripts/validate_bt_json.py" "<저장한 경로>"
   ```
   이 스크립트는 문서가 아니라 저장소의 현재 C#(`MonsterBehaviorJsonNodeKeys.cs`, NodeFactory, enum, registry)에서 카탈로그를 읽어 unknown key/enum 오타/actor scope 위반/payload 누락/priority 순서/id 중복을 잡는다. 전체 검사와 warning 승격은 `... "Assets/10.Datas/AI/BehaviorTree/SourceJson" --strict`. 상세 절차는 `references/validation.md`.
8. **안내 출력** — (a) 경로, (b) 그룹/규칙 한 줄 요약, (c) SelectedIntent 그룹 포함 확인, (d) 정적 검증 결과, (e) import 메뉴 안내를 포함한다. Unity import와 Play Mode 스모크를 실행하지 못했으면 "미검증"으로 명시한다.

## 스키마 요약

```jsonc
{
  "schemaVersion": 1,
  "id": "EnemyBehavior_XXX",
  "displayName": "...",
  "actorKind": "Ground",            // "Ground" | "Flying"
  "sourceBehaviorSo": "Assets/.../BehaviorData_xxx.asset",  // 선택
  "blackboard": { "tickInterval": 0.08, "optimalCombatDistance": 2.4, ... },
  "groups": [
    {
      "name": "00 Survival And Interrupt", "priority": 1000,
      "rules": [
        { "name": "BlockedStateKeep", "priority": 1000,
          "when": [ { "condition": "IsBlockedEnemyState" } ],
          "do":   [ { "action": "KeepCurrentState" } ] }
      ]
    }
    // ... 10/20/30/40/50 그룹
  ]
}
```

- rule은 `do`(순차 Sequence) **또는** `select: "WeightedRandom"` + `choices`(가중 랜덤) 중 하나를 갖는다.
- condition은 `"invert": true`로 부정. `value`/`key`/`op`/`state`/`intent`/`style`/`attackCategory`/`duration`/`cooldownId`/`cooldownDuration`/`weight`의 의미·필수 여부는 `references/rules-catalog.md` 참조.
- `CooldownReady`의 `value`(쿨다운 id)는 같은 rule action의 `cooldownId`와 문자열이 일치해야 짝이 맞는다.

## Validator가 막는 것 (import 전 체크)

- `schemaVersion`은 1, `id` 필수.
- `groups` 또는 `rules` 중 최소 하나. group은 `name` 필수, `rules` 비면 안 됨.
- 모든 condition/action 키는 카탈로그에 존재해야 함.
- enum 값(`intent`/`style`/`attackCategory`/`state`/`SelectedIntent.value`/`HasStateTag.value`)은 정확한 enum 멤버여야 함.
- `BlackboardCompare`는 `key` + (`value` 또는 `valueKey`) 필요, `op`는 유효한 `BlackboardComparisonType`.
- `IsCurrentState`/`IsEnemyPhase`/`SelectedIntent`는 `value` 필수.
- actorKind=Ground 트리에 Flying 전용 노드(또는 그 반대) 사용 금지.

## displayName / comment / 한국어 규약

- 그룹·규칙 `name`은 의도가 드러나게(`PunishStagger`, `FrequentAttackCounter`).
- 게임 디자인 의도가 보이도록 작성. 설명은 한국어(프로젝트 규약).

## 역할 지명 패턴 (보스·아키타입 전용)

기본 `ExecuteAttack` 대신 **`CanActivateAbility` 확인 → `RequestAction` 실행**으로 짜면 공격을 역할 단위로 지명할 수 있다. 보스 5종(`SourceJson/Boss/`)이 이 형태다.

```jsonc
"when": [ { "condition": "CanActivateAbility", "attackCategory": "Skill", "abilityRole": "Signature" },
          { "condition": "CooldownReady", "value": "XxxFinal" } ],
"do":   [ { "action": "RequestAction", "intent": "Attack",
            "attackCategory": "Skill", "abilityRole": "Signature",
            "cooldownId": "XxxFinal", "cooldownDuration": 8.5 } ]
```

- `RequestAction`은 `cooldownId`를 **기록한다** — `ExecuteAttack`과 달리 `CooldownReady` 게이트가 실제로 작동한다.
- `CanActivateAbility`를 선행하지 않으면 후보가 0일 때 빈 스윙이 된다. 항상 짝으로 쓴다.
- `abilityRole`은 Payload의 `aiRoles`와 매칭된다. **대상 Ability의 `aiRoles`가 `None`이면 영구히 잡히지 않는다** — 역할 지명 BT를 쓰기 전에 AbilitySet 쪽 `aiRoles`가 채워졌는지 먼저 확인한다.
- 이 패턴을 써도 Ground 트리에서는 `40 Execute Selected Intent`를 유지하는 것을 권장한다. 역할 지명(결정론)과 스코어러(확률적 변주)는 배타적이지 않다. 보스는 `IsEnemyPhase` 결정론으로 개성을 만들어 40번을 생략했지만, 페이즈가 없는 일반 몬스터가 이를 따라하면 거리 분기만 남아 균질해진다.

## 결과 출력 형식

```
**생성된 BT Rules JSON:**
- Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_<이름>.json

**구성 요약:**
- actorKind / 그룹 목록 / 핵심 규칙 한 줄
- ✅ "40 Execute Selected Intent" 그룹 포함 (스코어러 소비 확인)  ← Ground 필수

**검증:**
- 정적 검증: 오류 0 / 경고 N (validate_bt_json.py)
- Unity import·Play Mode: 미검증 (또는 실행 결과)

**Unity에서 사용하는 법:**
1. `UPlayGround/비헤이비어 트리/JSON/선택 JSON 가져오기` (또는 Project에서 JSON 우클릭 → UPlayGround/AI/Import)
2. import 시 Generated/ 에 BehaviorTreeAsset 생성, 루트에 스코어러 서비스 자동 부착
3. Behavior Tree Editor가 열리며 자동 검증 — Error 0 / Warning 확인
```

## 참조 자료

- `references/rules-catalog.md` — condition/action/select 어휘, `value` 의미, enum 값. **반드시 이 카탈로그 범위 내에서만 작성.**
- `references/example.json` — 완전한 Ground melee Rules JSON. 새 트리의 출발점(특히 40번 그룹 유지).
- `references/node-catalog.md` — (레거시) raw BT-node 포맷 카탈로그. **보스처럼 완전 수기 제어가 필요한 예외**에만 사용하며, 이 경우 스코어러는 자동 부착되지 않는다. 기본 경로는 Rules JSON이다.
