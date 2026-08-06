# BT Rules JSON 검증 절차

작성/수정한 JSON은 저장 직후 이 순서로 검증한다. 1번은 Claude가 직접 실행하고, 2~4번은 Unity가 필요하므로 실행 못 했으면 "미검증"으로 명시 보고한다.

## 1. 정적 검증 (필수, 항상 실행)

프로젝트 루트에서 변경 파일을 검사한다.

```powershell
python ".claude/skills/generate-bt-json/scripts/validate_bt_json.py" "Assets/10.Datas/AI/BehaviorTree/SourceJson/대상.json"
```

전체 검사 + warning도 실패 처리:

```powershell
python ".claude/skills/generate-bt-json/scripts/validate_bt_json.py" "Assets/10.Datas/AI/BehaviorTree/SourceJson" --strict
```

이 스크립트는 문서가 아니라 저장소의 **현재 C#**에서 카탈로그를 읽는다.

- condition/action 카탈로그: `MonsterBehaviorJsonNodeKeys.cs`
- actor scope(Ground/Flying/Common): `MonsterBehaviorTreeJsonImporter.NodeFactory.cs`
- enum 값: `Assets/02.Scripts` 전체 rglob
- Blackboard DTO 필드: `MonsterBehaviorTreeJsonImporter.cs`
- Blackboard key/alias: `BehaviorTreeEditorRegistry.json`

검사 항목: JSON 파싱, 중복 key, unknown field, 잘못된 actor scope, payload 누락, enum 오타, priority 내림차순, id 중복, `sourceBehaviorSo` 경로 실재 여부.

warning은 importer가 허용하지만 의도 확인이 필요한 항목이다. **스크립트 통과는 Unity importer 실행을 대체하지 않는다.**

## 2. Unity import

- 변경 JSON만: Project 창에서 선택 후 `UPlayGround/비헤이비어 트리/JSON/Project 선택 JSON 가져오기`
- 전체 재생성이 의도일 때만: `.../SourceJson 전체 가져오기`
- Rules JSON / raw BT-node JSON 자동 판별: `.../AI JSON 가져오기 (자동 감지)`

Console의 import 실패와 `BehaviorTreeAssetValidator` 결과를 확인한다.

## 3. 그래프와 diff 확인

- Root 자식이 의도한 group 우선순서인지 확인.
- group `when`이 rule selector 앞에 적용됐는지 확인.
- 각 rule의 condition 순서와 action/WeightedRandom choice 확인.
- fallback이 항상 존재하고 고우선순위 branch가 하위 branch를 영구 차단하지 않는지 확인.
- `Assets/10.Datas/AI/BehaviorTree/Generated/`와 `.meta` diff 검사. 자동 변경 범위가 예상보다 넓으면 저장을 중단하고 원인을 확인한다.
- 사용자 변경이나 무관한 에셋 재직렬화는 보존한다.

## 4. Play Mode 스모크

- 지상형: 타깃 획득/상실, 공격, 추격, 거리 조정, 피격 반응, fallback
- 비행형: 지상/공중 루프, 이륙/착륙, 공중 공격 제한, dive, fallback
- 보스/페이즈형: HP 경계 전후 phase 전환과 각 phase의 공격 해석

## 코드까지 바꿨을 때

- importer/registry/enum/BT 런타임 C# 변경 → 관련 asmdef 또는 `dotnet build UPlayGround.sln --no-restore`로 컴파일 보조 확인.
- Blackboard registry 변경 → `UPlayGround/Generator Tool/Enemy Blackboard Keys`로 `EnemyBlackboardKeys.generated.cs` 재생성 후 무가드 raw string 잔존 확인. 런타임 `const string`을 손으로 추가하지 않는다.
