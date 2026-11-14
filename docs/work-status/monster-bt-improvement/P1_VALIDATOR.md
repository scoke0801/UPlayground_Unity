# P1 검증기·BehaviorData 감사

정본: [설계](../../../Assets/docs/design/combat/monster/MONSTER_BT_BEHAVIOR_IMPROVEMENT_DESIGN.md) · [상태](STATUS.md)

## 구현과 대조

- `SelectedIntent`: Enum.TryParse + 정의 여부 + 정확한 이름 확인. 숫자 문자열과 소문자 오타도 거절한다. 런타임은 정확한 의도 문자열을 읽는다.
- `ExecuteAttack`의 cooldownId 또는 0이 아닌 cooldownDuration: 오류. RequestAction 또는 ActionDelayElapsed 사용 안내.
- CooldownReady: 같은 트리의 RequestAction/Transition에서 양수 duration으로 등록되는 ID와 대조해 누락을 경고한다. 그룹 조건도 포함한다.
- 도달 불가: 우선순위 내림차순·동률 원본 순서를 대조한다. 조건 없는 단일 Wait 뒤의 규칙만 확정 경고한다. KeepCurrentState는 비차단 상태에서 실패하므로 경고 대상으로 간주하지 않는다. 조건만 비었다는 이유로 모두 도달 불가로 판정하는 설계 초안을 보정했다.
- 감사는 읽기 전용이며 툴 런처의 `몬스터 BT 연결 감사`에서 실행한다. ActorDefinition의 실제 프리팹과 EffectiveBehaviorData로 지상·비행을 구분하고, 비행의 실제 `_behaviorTree` 연결을 별도 보고한다. 미사용 BehaviorData 칸은 오류로 취급하지 않는다. ActorDefinition에서 사용처를 확인하지 못한 데이터는 정보로 구분한다.
- 조건 검사는 importer Validate에 연결했다. RequestEnemyActionNode의 성공/트리거 수락 시점과 EnemyActionResolver를 통한 Transition의 쿨다운 등록, CooldownReadyNode의 Blackboard 준비 시각 조회를 대조했다. ExecuteAttack 팩토리는 쿨다운을 넘기지 않는다는 사실을 확인했다.

## 현행 데이터

- [감사 전체 연결표](P1_AUDIT_BEFORE.md), [JSON 27종 검사](P1_JSON_VALIDATION.txt).
- `BehaviorData_humanoid` GUID `547eec6e48eb3d949afb7715414b42dd`는 `Assets/BT_EnemyGroundMelee_Organic.asset`으로 정상 해석된다. 깨진 참조가 아니므로 보존한다.
- AggressiveGroundMelee_Tuned의 ComboResetKeepThreat가 등록되지 않는 ComboReset을 읽고 있었다. 항상 true였던 무효 조건만 제거했다. 각 선택 액션의 개별 쿨다운과 ActionDelayElapsed는 유지한다. 이 단계에서 BT 연결은 변경하지 않는다.
- 빈/깨진 BehaviorData BT 참조는 감사 결과에서 확인되지 않았다. Test 연결 13개 중 griffin의 칸은 미사용이다. 실제 비행 BT는 별도 표로 확인한다.

## 검증

- 단일 Editor 정상 컴파일 및 후속 MCP 실행 성공, scriptCompilationFailed=false.
- 최종 신규 EditMode 11/11 통과. [Test Runner 원본](P1_TEST_RESULTS.json), job cd8e1dce8f8a49338becf17afb45dd5b. 도달 불가 판정 보정 후 정상 컴파일한 최신 코드로 재실행했다.
- 플레이 체감 평가는 필요 없는 에디터 검사 단계다. Player·Addressables 빌드 미수행.
