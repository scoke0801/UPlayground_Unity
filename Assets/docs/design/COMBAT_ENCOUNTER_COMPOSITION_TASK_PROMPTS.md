# 전투 구도 개선 — 작업 요청 프롬프트

> 기준 문서: [COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md](COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md)
> 사용법: 단계마다 새 세션을 열고 해당 프롬프트 블록 하나만 붙여 넣는다. 앞 단계의 Play Mode 확인이 끝나기 전에는 다음 단계를 요청하지 않는다. 한꺼번에 넣으면 무엇이 효과를 냈는지 가려낼 수 없다.

| 단계 | 프롬프트 | 선행 조건 |
|---|---|---|
| P0 | 데이터 정합성 (경직 정책 누락, 밸런스 기준) | 없음 |
| P1 | 소탕전 수치 (처치 시간, 그룹 프리셋) | P0 완료 |
| P2 | 넓은 무쌍식 카메라 | P1 Play Mode 확인 |
| P3 | 소울식 락온 (고정 거리, 자유 이동 + 행동 시작 정렬) | P2 Play Mode 확인 |
| P4 | 교전 판정 (탐색 / 소탕 / 결투) | P3 Play Mode 확인 |
| P5 | 혼합 장치 (정예 브레이크 충격파, 긴 콤보 대응) | P4 완료 |
| P6 | 측정과 튜닝 | P5 완료 |

---

## 공통 지침

모든 프롬프트가 이 절을 참조한다. 작업자는 착수 전에 이 절을 읽는다.

1. **먼저 읽는다.** `CLAUDE.md`, 기준 문서의 해당 절, `Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md`, `Assets/docs/guide/TEST_VALIDATION_GUIDE.md`. 코드를 쓰거나 구조를 바꾸면 `Assets/docs/guide/CODE_AUTHORING_GUIDE.md`도 읽는다.
2. **진단부터 다시 확인한다.** 기준 문서 §2의 파일·필드·값이 지금도 맞는지 코드와 에셋으로 확인한다. 달라졌으면 작업 전에 차이를 보고한다.
3. **이 단계 범위만 한다.** 다음 단계 작업을 미리 하지 않는다. 범위 밖에서 발견한 문제는 고치지 말고 보고한다.
4. **구조 규칙을 지킨다.** 판단은 BT, 수치는 GAS, 타이밍은 MotionSet. Camera 모듈 안에서는 `Svc`·`GameActor`·구체 매니저를 쓰지 않고 `ICameraRuntimeAdapter`를 거친다. 몬스터·스테이지 이름으로 분기하지 않는다. 기획이 조정할 값은 SO로 뺀다.
5. **에셋은 안전하게 고친다.** Unity가 열려 있으면 `.asset` YAML을 외부에서 직접 고치지 않는다(재직렬화로 되돌아간다). Unity MCP, 에디터 스크립트, 인스펙터를 쓴다. Ability는 `generate-gas-ability`, BT는 `generate-bt-json` 스킬로 작업하고 BT 포맷(`SourceJson/` Rules JSON 또는 `Json/` raw)을 명시한다.
6. **수치는 기준 문서의 초기 실험값에서 시작한다.** 바꾼 값은 "필드 / 이전값 / 새값 / 이유" 표로 남긴다.
7. **검증을 사실대로 보고한다.** 수행·실패·미수행을 구분한다. Play Mode 체감은 사용자가 확인할 항목을 체크리스트로 따로 준다. 컴파일만 된 상태를 완료라고 하지 않는다.
8. **기준 문서를 갱신한다.** 완료하면 기준 문서 §9 표에 해당 단계의 상태와 결과 요약을 적는다. 커밋은 요청받았을 때만 `write-commit-message` 스킬로 한다.

---

## P0 — 데이터 정합성

```text
[전투 구도 P0 — 데이터 정합성]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §2.2(G1·G6), §2.3, §3.1, §9 P0
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.

목표: 모든 몬스터에 등급에 맞는 경직 규칙이 빠짐없이 적용되고, 밸런스 기준이 "잡몹=소탕전, 보스=결투전" 방향과 맞는 상태를 만든다.

1단계 — 조사 후 보고 (이 단계에서는 아무것도 수정하지 않는다)
- MonsterActorProfileSO 전체와 프로필 없는 ActorDefinitionSO의 등급, combatReactionPolicy, 실제로 적용되는 등급 규칙 유무를 표로 정리한다.
  구분: Boss인데 Boss 규칙 없음 / Elite인데 Elite 규칙 없음 / 등급과 정책 에셋이 어긋남 / 정상.
- Normal 등급 중 1:1 결투형으로 설계된 것으로 보이는 몬스터 후보와 근거(HP·강인도·Ability 구성·배치 위치).
- "프로젝트 기본 경직 정책"(정책 에셋이 없거나 해당 등급 규칙이 없을 때 사용)을 도입하면 동작이 바뀌는 몬스터 목록과 바뀌는 내용.
- 여기서 멈추고 두 가지 결정을 요청한다.
  ① 누락 정리 방식: 개별 에셋 지정 / 기본 정책 폴백 / 둘 다
  ② Normal → Elite 재분류 대상

2단계 — 승인된 범위만 적용
- 승인된 방식으로 정책 누락을 정리한다. 기본 정책 폴백을 택하면 CombatPolicyResolver 경로에 붙이고,
  EditMode 테스트를 추가한다: 정책 null / 정책은 있으나 해당 등급 규칙 없음 / 명시 규칙이 기본값보다 우선.
- 승인된 재분류를 적용한다.
- Assets/docs/LEVEL_GRADE_COMBAT_BALANCE_POLICY.md를 개정한다.
  Weak 1~2초, Normal 3~6초(단일 대상 집중), 무리 6~10마리 정리 25~45초(신설), Elite 유지 여부 검토, Boss 120초(90~180초) 유지. 개정 이유를 문서에 남긴다.
- BalanceCombatEstimator의 등급별 기본 목표 시간을 개정한 정책과 맞춘다(Weak 분리, Boss 90→120초).
  코드 상수 대신 데이터로 뺄 수 있으면 방법을 제안한다.

완료 기준
- 실제로 적용되는 등급 규칙이 빠진 몬스터 0건, 또는 승인된 예외 목록만 남음.
- 정책 문서와 밸런스 도구 기본값이 일치.
- 추가한 테스트와 기존 관련 테스트 통과.

하지 않는 것: HP·공격력·강인도 수치 변경(P1), 카메라·AI·이동 변경.
```

---

## P1 — 소탕전 수치

```text
[전투 구도 P1 — 소탕전 수치]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §4.3, §4.4, §4.6, §9 P1
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
선행: P0 완료. 개정된 LEVEL_GRADE_COMBAT_BALANCE_POLICY.md를 기준으로 삼는다.

목표: Weak·Normal을 소탕전 처치 시간에 맞추고, 무리 전투의 템포를 맞춘다.

작업
1. MonsterScalingSO의 등급 배율(HP·강인도 중심)로 Weak 1~2초, Normal 3~6초에 맞춘다.
   개별 몬스터의 HP를 손으로 고치지 않는다. 밸런스 도구로 등급별 변경 전후 비교표를 만든다.
2. 성장·보상 영향을 계산한다. 경험치·드롭이 마리당이면 소탕전 1회의 보상 총량 변화를 보고한다. 보상 수정은 제안만 한다.
3. Weak·Normal이 플레이어에게 쓰는 공격의 HitPhase reactionType 분포를 점검한다.
   텔레그래프 없는 공격이 넉백·다운을 주면 목록으로 만들고 Hit/Light로 맞추는 안을 제시한다.
   GAS 수정은 승인 후 generate-gas-ability 스킬로 한다.
4. 소탕 무리 그룹 프리셋(§4.4)을 전투 테스트 맵의 MonsterGroupController에 적용한다.
   무리 적들이 실제로 그룹에 등록되는지 확인한다. 그룹에 속하지 않은 적은 공격 슬롯 제한을 받지 않는다.
5. 한 번 휘둘러 여러 적이 맞을 때 플레이어 히트스톱이 적 수만큼 겹치는지 코드로 확인하고 보고한다(수정은 승인 후).
6. 공격 어시스트 거리(일반 3.5m, 대시 6m)는 Play Mode 실험용 임시값으로만 안내하고 커밋하지 않는다. 정식 반영은 P4다.

완료 기준
- 밸런스 도구의 등급별 예상 처치 시간이 목표 범위 안.
- 2·3·5번 보고서 제출.

Play Mode 확인 항목(사용자): 기준 문서 §8.1 시나리오 A의 0~20초 구간. 첫 타부터 주도권을 쥐는지, 콤보 끝에 다음 적이 사정거리에 있는지.
```

---

## P2 — 넓은 무쌍식 카메라

```text
[전투 구도 P2 — 넓은 무쌍식 카메라]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §2.2(G2·G3), §2.3 첫 항목, §6 전체, §9 P2
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
추가로 먼저 읽을 것: Assets/docs/guide/CAMERA_MODULE_PORTABILITY_GUIDE.md, Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md
선행: P1 Play Mode 확인.

목표: 소탕전에서 무리 전체와 바닥의 공격 범위가 한 화면에 보이는 넓고 높은 구도를 만든다.

작업 (번호마다 독립적으로 검증 가능한 단위로 나눈다)
1. 성능: CameraDistanceController.EvaluateTargetMaxSize의 매 프레임 GetComponentsInChildren 호출을 대상별 크기 캐시로 바꾼다.
   대상이 파괴·비활성화되면 캐시를 정리한다. 동작이 바뀌지 않았는지 확인한다.
2. 구도 프리셋 구조: CameraSettings에 탐색 / 소탕 / 결투 구도 프리셋을 둔다.
   (거리 범위, 피치, FOV, 피벗 오프셋, 최대 거리, 타격 시야 보정 1회 최대, 전환 시간)
   교전 종류는 ICameraRuntimeAdapter로 받는다. P4 전까지 UPlayGround 어댑터는 "전투 중이면 소탕, 아니면 탐색"으로 기본 구현한다.
   기존 fovExplore / fovCombat / explorePitch / combatPitch / defaultOffset / combatOffset과의 관계를 정리해 같은 의미의 필드를 두 벌 남기지 않는다.
   기존 에셋 값이 유실되지 않게 직렬화 호환을 처리한다.
3. 무리 크기에 비례하는 거리: "적 3마리 이상이면 7.4m" 스위치를, 교전 중인 적이 퍼진 반경과 마리 수로 정하는 연속 목표 거리로 바꾼다.
   마리 수 경계에서 거리가 진동하지 않아야 한다.
4. 무리 프레이밍 Modifier: 교전 중인 적의 가중 중심 쪽으로 피벗을 비율 0.25, 최대 1.5m 옮긴다. 회전은 바꾸지 않는다. 소탕 프리셋에서만 동작한다.
   기존 Modifier 파이프라인의 우선순위 체계에 맞춰 추가하고, CameraManager에 분기를 추가하지 않는다.
5. 프리셋 값은 기준 문서 §6.2 초기값으로 넣는다. 사용자 접근성 설정(셰이크·자동 보정 스케일)은 프리셋 위에 곱한다.

하지 않는 것: 자동 회전 보정 확대, 락온 동작 변경(P3), 화면 밖 위협 마커 제거.

검증
- Camera asmdef 컴파일(dotnet build).
- 무리 거리 계산 EditMode 테스트: 마리 수, 퍼짐 반경, 경계 히스테리시스.
- 가능하면 적 15마리 상황에서 1번 변경 전후 프로파일러 비교.

Play Mode 확인 항목(사용자): 탐색↔전투 진입 시 줌이 튀는지, 2↔3마리 경계, 바닥 텔레그래프가 보이는지, 멀미, 근접 타격감 저하 정도.
```

---

## P3 — 소울식 락온

```text
[전투 구도 P3 — 소울식 락온 (고정 거리 · 자유 이동 + 행동 시작 정렬)]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §2.2(G4·G5), §7 전체, §9 P3
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
추가로 먼저 읽을 것: Assets/docs/guide/CAMERA_LOCK_ON_SYSTEM_GUIDE.md, Assets/docs/guide/CONTROL_FEEL_IMPROVEMENT_GUIDE.md, Assets/docs/Complete/MOTION_WARPING_IMPROVEMENT_DESIGN.md
선행: P2 Play Mode 확인.

결정 사항: 락온 중 옆걸음은 하지 않는다. 캐릭터는 지금처럼 입력 방향으로 자유롭게 움직이고, 카메라만 소울식으로 대상을 고정한다.
옆걸음 이동·옆걸음 애니메이션·대상 기준 방향 회피를 제안하거나 구현하지 않는다.

목표: 락온하면 결투 거리감이 항상 일정하고, 캐릭터가 어느 쪽을 보고 있든 공격·스킬·차지·가드를 시작하는 순간 대상을 향하는 상태를 만든다.

작업
1. 락온 고정 거리: lockOnDistance 사용 여부를 enableLockOnAdaptiveFraming과 분리한 별도 설정으로 만든다.
   결투 프리셋은 고정 5.5~6.0m, 소탕 프리셋은 max(고정 거리, 무리 거리).
   LockOnFramingCameraModifier의 피치 계산 기준 거리와 실제 카메라 거리가 일치하는지 확인한다.
2. 현재 경로 조사 후 보고: 락온 중 다음 행동이 시작될 때 대상 쪽으로 회전하는지 표로 정리한다.
   대상: 일반·강공격, 콤보 이어치기, 대시 공격, 점프 공격, 차지, 스킬, 궁극기, 퍼펙트 회피 반격, 퍼펙트 가드 반격, 가드.
   조건별로 나눈다: 대상이 공격 어시스트 거리 안 / 밖, 공격에 MotionWarp 이벤트가 있음 / 없음.
   알려진 지점: PlayerAttackState는 락온 중 BeginTargetLock(Vector3.zero)로 입력 방향을 쓰지 않고, GetAttackWarpTarget이 거리 밖 대상을 비워 회전도 빠진다.
   PlayerGuardState.UpdateRotation은 현재 방향을 유지한다.
3. 회전과 접근 분리: 락온 중에는 대상이 거리 밖이어도 몸은 대상 쪽으로 돌리고, 접근 워프는 기존 거리 규칙 안에서만 한다.
   2번에서 빠진 경로들을 행동마다 따로 고치지 말고 하나의 공통 정렬 경로로 묶는다. 구조안과 근거를 먼저 제시한 뒤 진행한다.
   정렬은 첫 판정 프레임 전에 끝나야 한다(초기 목표: 선딜 0.15초 안에 180°). 정렬 속도는 데이터로 둔다.
4. 가드: 락온 중 가드에 들어가면 대상 쪽으로 돌고, 유지하는 동안 각속도 제한을 두고 대상을 따라간다.
   가드 방향 판정 추가는 범위 밖이다(현재 DefenseResolver에 방향 판정 없음). 필요하다고 판단되면 영향만 보고한다.
5. 실효 락온 피치 12~16°(결투 거리 기준)를 맞추는 lockOnPreferredPitch 제안값을 계산 근거와 함께 제시한다.

하지 않는 것: 옆걸음 이동, 새 이동 애니메이션, 락온 대상 선정·전환 로직 변경, 락온 없는 상태의 공격 방향 규칙 변경, 결투전 자동 락온.
핫패스 규칙: KCC 콜백과 UpdateState 안에서 GetComponent·LINQ·매 프레임 할당 금지.

검증
- Actor·Camera asmdef 컴파일.
- EditMode: 락온 거리 계산(고정 / 무리 거리 max), 정렬 목표 방향 계산(거리 밖 대상, 대상 없음, 락온 해제 직후).
- 락온하지 않은 상태의 공격 방향이 바뀌지 않았는지 회귀 확인.

Play Mode 확인 항목(사용자):
- 락온한 채 좌우 입력으로 대상 주위 돌기 (원이 바깥으로 벌어지는지)
- 옆·뒤로 달린 직후, 회피 직후 공격 → 대상 쪽으로 휘두르는지, 정렬이 답답하지 않은지
- 대상이 3~5m 떨어진 상태에서 공격 → 다가가지 않고 대상 쪽으로만 도는지
- 가드 진입·유지 중 방향, 대상 뒤로 돌아갈 때 카메라, 대형 보스 고저차, 락온 해제 직후 이동
```

---

## P4 — 교전 판정

```text
[전투 구도 P4 — 교전 판정 (탐색 / 소탕 / 결투)]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §3.2, §3.3, §8.3, §9 P4
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
선행: P3 Play Mode 확인.

목표: "지금 어떤 전투인가"를 한 곳에서 판정하고, 카메라·플레이어 공격 보정·락온이 그 결과만 읽게 만든다.

작업
1. 판정 서비스: Contracts에 계약(현재 교전 종류, 전환 알림)을 두고, 구현 위치는 기존 매니저 구조(예: GameCombatManager 산하 핸들러) 중에서 근거를 들어 고른다.
   판정은 이 서비스 한 곳에서만 한다. 카메라가 CameraTargetInfo.Grade로 스스로 판정하게 만들지 않는다.
2. 판정 규칙
   - 결투: 보스 아레나 트리거 활성, 또는 Boss 등급이 플레이어를 인지한 채 반경 안에 있음.
   - 해제: 보스 사망·이탈 후 일정 시간 유지 뒤 해제. 보스 공격 모션 도중에는 전환을 미룬다.
   - 보스 아레나 트리거는 Trigger/FlowGraph에서 켜고 끌 수 있는 액션으로 제공한다(CONTENT_SYSTEM_AUTHORING_GUIDE.md 적용).
   - 반경과 유지 시간은 SO 값으로 둔다(초기값: 20m, 2초).
3. 소비자 연결
   - 카메라: UPlayGroundCameraRuntimeAdapter가 판정 결과를 넘겨 P2 프리셋을 고른다(P2의 임시 기본 구현을 대체).
   - PlayerCombat: 교전 프로필 SO의 공격 어시스트 거리 배율(초기값 소탕 3.5m / 대시 6.0m, 결투 2.0m).
   - 락온: P3의 거리 규칙을 교전 종류에 따라 적용.
4. 시나리오 C의 규칙(보스 합류 시 남은 무리 슬롯 0, 결투 중 소환 부하 규칙)이 기존 그룹 기능으로 되는지 확인하고, 안 되면 필요한 최소 변경을 제안한다.

하지 않는 것: Camera 모듈에서 Svc·GameActor 참조, 몬스터·스테이지 이름 분기.

검증
- EditMode: 진입, 해제, 히스테리시스, 보스 공격 중 전환 지연, 트리거 우선순위.
- 관련 asmdef 컴파일. 저장/로드 직후와 씬 전환 직후 판정 초기 상태 확인.

Play Mode 확인 항목(사용자): 시나리오 B 아레나 진입 전환, 시나리오 C 세 규칙, 소탕↔결투 전환 시 카메라 튐.
```

---

## P5 — 혼합 장치

```text
[전투 구도 P5 — 혼합 장치 (정예 브레이크 충격파 · 정예 우선 슬롯 · 긴 콤보 대응)]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §4.2, §4.4 신규 규칙, §4.5, §9 P5
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
선행: P4 완료.

목표: 소탕전 안에 "정예가 흐름을 끊고, 정예를 무너뜨리면 더 크게 쓸어버리는" 순간을 만든다.

작업
1. 정예 브레이크 충격파: 정예의 브레이크 발생에 반응해 반경 4m 안의 Weak·Normal을 넘어뜨리는 Ability를 만든다.
   먼저 기존 브레이크 이벤트 경로를 확인하고 연결점을 제시한다. 태그 트리거 구조를 우선 검토한다.
   Ability는 generate-gas-ability 스킬로, 반경·리액션은 Ability 데이터에, 연출(VFX·셰이크 1회)은 MotionSet/카메라 이펙트에 둔다.
2. 정예 우선 슬롯: 정예가 잡몹과 공격 슬롯을 다투지 않게 한다. MemberPriority를 확장한다면 기존 직렬화 값의 순서를 바꾸지 않는다.
3. 긴 콤보 대응: 플레이어가 한 대상을 3초 이상 연속 공격하면 측면·후방 적 1명에게 공격 슬롯 우선권을 준다.
   그 적은 화면 안에 있거나 화면 밖 위협 마커가 켜진 상태여야 한다.
   어느 레이어(그룹 컨트롤러 / BT / 스코어러)가 맡아야 하는지 근거를 먼저 제시한다. 시간값은 데이터로 둔다.

검증
- AbilityDataValidator 전수 검증, MonsterAbilitySetIntegrationTests.
- 그룹 슬롯 우선권 EditMode 테스트(정예 우선, 긴 콤보 조건, 화면 조건).

Play Mode 확인 항목(사용자): 시나리오 A 20~38초 구간. 정예가 흐름을 끊는지, 브레이크 순간 주변이 넘어지는지, 긴 콤보 위험이 보이는 곳에서 오는지.
```

---

## P6 — 측정과 튜닝

```text
[전투 구도 P6 — 측정과 튜닝]

기준: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_GUIDE.md §10.1, §11
공통 지침: Assets/docs/design/COMBAT_ENCOUNTER_COMPOSITION_TASK_PROMPTS.md의 "공통 지침"을 먼저 읽고 따른다.
선행: P5 완료.

목표: 소탕전과 결투전이 의도한 플레이를 실제로 보상하는지 숫자로 확인하고, 한 번에 하나씩 조정한다.

작업
1. CombatLogRecorder / CombatTelemetrySession에 §10.1의 지표 6종을 추가하고 교전 종류별로 집계한다.
   핫패스 할당 금지, 진단 로그는 RuntimeLog 규칙을 따른다.
2. 밸런스 도구의 텔레메트리 가져오기 화면에서 교전 종류별로 볼 수 있게 한다.
3. 사용자가 시나리오 A·B를 플레이한 로그를 주면 다음을 보고한다.
   - 지표별 목표 대비 표
   - 어긋난 지표마다 원인 레이어 추정(BT / GAS / MotionSet / 카메라 / 그룹 AI)
   - 한 번에 하나만 바꾸는 조정안 (무엇을, 얼마나, 기대 효과)

완료 기준: 지표 수집이 동작하고, 로그 1회 분석 보고서가 나온다.
```
