# P2 플레이어 읽기

[상태](STATUS.md) · [설계](../../../Assets/docs/design/combat/monster/MONSTER_BT_BEHAVIOR_IMPROVEMENT_DESIGN.md)

- 신규 조건 5종을 importer 키/팩토리/표시명 레지스트리에 등록했다. 값 계산은 EnemyTacticalMemory가 ManagedTick에서 수행하고 조건은 캐시를 읽는다. SetPlayerTarget에서 컨트롤러·타겟 액터를 캐시한다.
- 차지: 새 의미 태그 CastingOrCharging을 PlayerChargeState가 발사 전까지만 켠다. 문자열 목록은 늘리지 않았다. 다른 시전/긴 선딜은 실제 타이밍 계약을 확정하기 전 임의 분류하지 않는다.
- 후방: 수평 내적과 전체 원뿔 각도로 계산한다. rearAngle/가드 파괴 기억 기간/저체력 기준은 EnemyBehaviorSO를 단일 소스로 사용했다. 블랙보드에 동일 수치를 중복 저작하지 않으려고 계획의 rearAngle 블랙보드 입력 대신 SO를 읽는다. 기존 별도 커스텀 인스펙터는 없어 기본 인스펙터로 노출된다.
- 가드 파괴: CombatLogRecorder.ResultObserved에서 자신이 피해자인 실제 DefenseOutcome.GuardBreak만 기록한다. 현재 EnemyGuardState는 블록 시 반격하므로 이 결과를 생성하는 적 가드 소모 모델은 없다. Poise Break를 가드 파괴로 간주하지 않는다. 조건/만료 API는 준비했으나 실전 가드 파괴 시나리오는 해당 전투 데이터 경로가 생기기 전 보류다.
- 슬롯: 기존 Blackboard alias를 실제 부작용 없는 CanRequestAttackSlot 조회로 교체했다. 지상·비행 컨텍스트 모두 그룹의 빈 슬롯/브리더/우선순위/적합도 교체 조건을 조회한다. 점유는 기존 RequestAttackSlot이 맡는다. 그룹 비소속의 제한 없음 동작은 보존한다.
- 저체력은 타겟 IDamageable의 비율을 읽는다. 기본 BT에는 추가하지 않았다.
- 새 관측값은 이번에는 규칙 전용으로 사용하므로 IntentEvaluationContext/IW 점수 필드에 추가하지 않았다.
- 실제 사용 중인 Humanoid_Bow 1종에 시야·슬롯·기본 공격 사용 가능·행동 딜레이를 갖춘 차지 견제 규칙을 넣었다. 재import 후 GUID aca100cbf85f12b4b803afdf62e81254 유지.

## 검증

- [관련 EditMode 31/31](P2_TEST_RESULTS.json): P1 11, 후방/기억/슬롯 부작용 신규 10, 기존 슬롯 정책 10. 컴파일 오류 0, MCP 후속 실행 성공.
- [표시명 5종 조회](P2_NODE_LABELS.txt) 정상. BT 편집기 실제 화면 캡처는 미수행이며 API 조회로 화면 검수를 대체하지 않는다.
- 핫패스 신규 GetComponent/LINQ/컬렉션 할당 없음. 실기 프로파일링은 미수행.
- 사용자 테스트 대기: LakeVillageWorld의 랜덤 궁수에게 강공격을 차지해 견제 사격을 확인한다(P2-01). 후방 조건과 가드 파괴는 저작 API·자동 검사까지 확인했으며 이를 쓰는 원형 BT의 실전 체감은 P3 이후다. 그룹 전투에서 슬롯이 찼을 때 조회만으로 다른 몬스터의 슬롯을 뺏지 않는지 확인한다(P2-02).
- P0 연결의 사용자 결정/플레이 검수와 P2 체감 검수는 대기 상태다. 의존하지 않는 구역·회피 공통 실행 제약(P2a)은 별도 변경으로 구현하고 원형 분화와 섞지 않는다.
