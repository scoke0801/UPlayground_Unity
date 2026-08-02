# 몬스터 그룹 AI 동작 개선 설계 문서

작성일: 2026-08-01
대상 시스템: `Assets/02.Scripts/GameActor/Group/` (`MonsterGroupController`, `MonsterGroupMemory`)
선행 문서:
- `Assets/docs/Complete/MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md` (Phase 1~5 구현 완료)
- `Assets/docs/Complete/MULTI_ENEMY_CONTROL_FEEL_DESIGN.md` (§4.3 그룹 슬롯 양보 미구현)

---

## 0. 검토 결론

그룹 조율의 **골격은 이미 전부 존재한다.** 공격 슬롯, breather, 적합도 기반 슬롯 교체, Intent 마스킹, formation, separation, 그룹 공유 메모리까지 Phase 1~5가 구현 완료 상태다.

따라서 이 문서는 신규 시스템 도입이 아니라 다음 세 축을 다룬다.

1. **끊긴 연결 복구** — 코드는 있으나 실행 경로가 닫혀 있거나 소비처가 없는 지점
2. **튜닝 축 부재** — 그룹 크기·상황에 따라 스케일되지 않는 고정 상수
3. **전술 다양화** — 현재 표현되지 않는 그룹 단위 행동

**재발명 금지**: 슬롯 시스템, breather, formation 8분할, Intent Bias, 그룹 메모리, separation은 이미 있다. 새로 만들지 말고 확장한다.

---

## 1. 확인된 결함 (코드 근거)

| # | 결함 | 근거 | 영향 |
|---|------|------|------|
| D1 | `Activate()`가 사실상 항상 no-op | `MonsterGroupController.cs:121-127`이 `includeInactive:true`로 수집 후 개수만 0이 아니면 `_isActivated = true`. `Activate()`(129행)는 `if (_isActivated) return;`에 걸림 | 트리거로 깨우는 매복 그룹이 동작 불가 (`ActivateGroupTriggerActionSO`) |
| D2 | 비활성 멤버 등록 실패 가능성 | `RegisterMember`(146행)가 `actor.AIController == null`이면 스킵. 비활성 오브젝트는 `Awake` 미실행 → `MonsterActor.cs:110-111`의 자동 캐싱 없음 | 프리팹에 `_groundAIController`가 직렬화 안 된 개체는 그룹 미소속. D1과 겹치면 슬롯 조율·`OnGroupDefeated` 모두 상실 |
| D3 | 비행 몬스터가 그룹 조율 전부 우회 | `EnemyFlyingAIController.TryRequestAttackSlot()`이 무조건 `true`(226-231행). `SetGroup`(319행)은 참조만 저장 | 지상+비행 혼성 그룹에서 비행체만 무제한 난타 |
| D4 | `MonsterGroupMemory` 데드 API | `NotifyMemberTookDamage`/`LastHitOnGroupTime` 호출·소비 0건. 그룹 쪽 `GetSkillHitAccuracy`도 소비처 없음 | "그룹이 맞은 직후 반응" 의도가 미완인 채 잔존 |
| D5 | 그룹 관찰 카운트가 인원수에 비례해 부풀음 | `EnemyAIController.cs:240-243`이 개체·그룹 양쪽에 기록. `IsPlayerDodgingFrequently(threshold=2)`는 고정 | 인원수만 늘려도 AI가 급격히 영악해짐 (의도되지 않은 난이도 스케일) |
| D6 | 슬롯 수가 인원과 무관한 고정값 | `_maxMeleeAttackers=2`, `_maxRangedAttackers=2` | 2인 그룹은 스로틀 0, 8인 그룹은 6마리 상시 대기 |
| D7 | 슬롯 점유에 최소 유지 시간/히스테리시스 없음 | `IsAttackSlotOwnerLocked`(604행)는 `Attack` 상태만 보호. `_aggroDecisionInterval` 0.1s마다 재평가 | 접근 중 점유자가 자주 교체 → "다가오다 물러나기" 진동 여지 |
| D8 | 경보 전파가 무조건 전역 | `AlertGroup`(717행)이 거리·시야 무관 전원 각성. 호출처는 `NotifyBTAttackStarted` 한 곳뿐 | 한 마리 어그로에 그룹 전체가 몰림. 단계적 인지 연출 부재 |
| D9 | 그룹 비소속은 슬롯 무제한 | `RequestAttackSlot`(184-185행)이 비소속에 `return true` | 씬에서 몬스터가 그룹 하위로 안 묶이면 시스템 전체가 무효 (선행 문서 §7-A ①과 동일 지적, 실측 미완) |

---

## 2. 개선안

### P0 — 끊긴 연결 복구 (버그 성격, 비용 낮음)

#### P0-1. 잠복 그룹 활성화 복구 [D1]

- `Start`의 "멤버 수집 완료"와 "그룹 활성화"를 분리한다. 수집 결과로 `_isActivated`를 세우지 않는다.
- `[SerializeField] bool _startDormant` 추가. `false`(기본)면 기존처럼 시작부터 활성, `true`면 `Activate()` 호출 전까지 멤버를 비활성 유지.
- 기존 씬 호환: 기본값 `false`이므로 현재 배치의 동작은 불변.

#### P0-2. 비활성 멤버 지연 등록 [D2]

- `RegisterMember`에서 `AIController == null`이면 경고 후 폐기하지 말고 보류 목록에 적재, 멤버 활성화 시 재시도한다.
- 대안(더 단순): `MonsterActor`가 `OnEnable`에서 부모의 `MonsterGroupController`를 찾아 self-register. 이 경우 컨트롤러의 자식 수집은 폴백으로 남긴다.
- 어느 쪽이든 `AliveCount`/`OnGroupDefeated`가 잠복 멤버를 포함하도록 보장해야 한다.

#### P0-3. 비행 몬스터 슬롯 연결 [D3]

- 1단계(이번 범위): `EnemyFlyingAIController.TryRequestAttackSlot()`을 `EnemyAIController`와 동일하게 `_groupController.RequestAttackSlot`으로 교체. `ReleaseGroupSlot`/`NotifyMemberAttackEnded`도 연결. `CurrentGroupIntentBias`를 실제 그룹 값으로 반환.
- 2단계(별도): formation은 고도축(Y) 분리가 필요하므로 이번 범위에서 제외. 비행체는 슬롯·breather만 참여시킨다.
- 공격 타입 결정은 `EnemyAIController.Start`의 `_myAttackType` 캐싱 로직을 그대로 옮긴다.

#### P0-4. 그룹 메모리 데드 API 정리 [D4]

두 방향 중 택일. **권장은 (a)**.

- (a) 소비처 신설: `NotifyMemberTookDamage`를 `MonsterActor` 피격 경로에 연결하고, `GetIntentBias`에서 `LastHitOnGroupTime`이 최근(예: 1.5s 이내)이면 `keepDistanceBonus += x`, 근접 슬롯 상한 -1. "반격당한 직후 그룹이 한 박자 물러남" 연출.
- (b) 제거: 소비 계획이 없으면 API와 필드를 삭제해 오해를 없앤다.

---

### P1 — 체감 개선 (다인전 페이싱)

#### P1-1. player-breather [MULTI_ENEMY §4.3, 미구현]

선행 문서에서 설계만 되고 구현되지 않은 항목. 스턴락 대책의 마지막 축이며 **그룹 컨트롤러 한 곳만 수정**하면 된다.

- `_playerBreatherUntil` 추가. 플레이어가 Hit/Stun/Knockdown 진입을 그룹에 통지하면 `Time.time + _playerBreatherDuration`으로 갱신.
- `RequestAttackSlot`이 이 창 동안 **신규** 점유를 거부(기존 점유자는 유지 — 기존 `IsInBreatherWindow` 분기와 동일한 형태).
- 통지 경로는 `PlayerActor.OnDamaged` → 타깃팅 중인 그룹. 플레이어가 그룹을 역참조하지 않도록, 피격을 가한 `MonsterActor`의 `AIController.Group`을 타고 올라가는 방향을 권장.
- 기존 `_groupBreatherUntil`과 **별도 타이머**로 둔다. 합치면 원인 구분이 불가능해져 튜닝이 어려워진다.

#### P1-2. 슬롯 수 동적 스케일 [D6]

- `maxMelee = Clamp(CeilToInt(AliveCount * _meleeSlotRatio), 1, _meleeSlotCap)` 형태.
- Inspector 노출: `_meleeSlotRatio`(기본 0.5), `_meleeSlotCap`(기본 2~3), 원거리도 동일.
- 기존 고정 필드는 cap 값으로 이관해 현재 배치의 상한을 유지한다.

#### P1-3. 슬롯 점유 히스테리시스 [D7]

- `_minSlotHoldDuration`(예: 0.5s) 추가. 점유 시각을 기록하고 그 안에는 fitness 교체 대상에서 제외(우선순위 교체는 예외로 허용할지 결정 필요 — 권장: 우선순위 교체도 동일하게 보호).
- fitness 교체 마진은 절대값 대신 정규화 차이 기준으로 판정해 거리 스케일에 덜 민감하게 한다.

#### P1-4. 카메라 가시성 가중치 [D8 인접]

- `ComputeAggroFitness`의 `frontScore`(플레이어 forward 기준 각도)를 실제 카메라 프러스텀 판정으로 보정.
- 화면 밖 멤버의 슬롯 획득 확률을 낮춰 "안 보이는 곳에서 맞는" 빈도 자체를 줄인다. Danger Ring UI(가시화)와 상호 보완 관계 — 이쪽은 발생 억제.
- Camera 모듈 경계 주의: 그룹 컨트롤러가 `CameraManager`를 직접 참조하지 않도록 판정 결과만 주입받거나 기존 카메라 계약을 경유한다 (CLAUDE.md 카메라 모듈 규약).

#### P1-5. 그룹 관찰 카운트 정규화 [D5]

- 임계값을 인원수 함수로: `threshold = base + Floor(AliveCount / 2)`.
- 또는 카운트를 인원수로 나눠 "1인당 관찰 빈도"로 저장.
- 어느 쪽이든 `EnemyCombatDecisionEvaluator`가 그룹 메모리를 우선 사용하는 현재 라우팅(61-74행)은 유지한다.

---

### P2 — 전술 다양화

#### P2-1. 역할 분리 formation

- 현 8슬롯은 근접/원거리 구분이 없어 원거리 멤버가 근접 반경에 배치될 수 있다.
- 반경 링을 2개로 분리(근접링 = `RetreatDistance`, 원거리링 = 원거리 `OptimalCombatDistance`). 슬롯 인덱스는 링별로 독립 관리.

#### P2-2. 그룹 페이즈 (잔존 인원 기반)

- 현재는 `AliveCount == 1`일 때 `retreatBonus += 0.15`가 전부(370-371행).
- 잔존 비율 구간별로 그룹 성향을 전환: 다수 → 포위·견제, 절반 → 표준, 1~2마리 → 저돌 또는 후퇴(개체 성향에 따라).
- `GroupIntentBias`에 이미 필드가 있으므로 값 산출만 확장하면 된다.

#### P2-3. 경보 전파 반경/지연 [D8]

- `AlertGroup`에 거리·LOS 필터와 전파 지연을 도입해 단계적으로 각성시킨다.
- 호출 시점을 `NotifyBTAttackStarted` 외에 "타깃 최초 획득"에도 추가하되, 위 필터가 전제 조건이다(필터 없이 추가하면 대군 급습이 악화된다).

#### P2-4. breather의 SO 오버라이드

- 선행 문서 §5.1이 미구현 상태. `EnemyBehaviorSO`/`BehaviorPhase`에 `breatherDurationOverride`(음수 = 미지정)를 추가.
- 보스+잡몹 혼성 구성에서 그룹 템포를 개체 성향에 맞춰 조율할 수 있게 된다.

---

### P3 — 검증 / 도구

#### P3-1. 씬 배치 검증 도구 [D9] — 실질 최우선

- 그룹 미소속 `MonsterActor`를 나열하는 에디터 체커.
- D9는 **조용히 시스템 전체를 무력화**하는 항목이라, 비용 대비 효과로는 P0와 동급이다. 위 모든 튜닝의 전제 조건.

#### P3-2. 그룹 런타임 오버레이

- 슬롯 점유자 / 후보 큐 / breather 잔여 / formation 점유를 씬 뷰 또는 디버그 창에 노출.
- 기존 `OnDrawGizmos`(747-779행)의 formation 시각화를 확장하는 형태.
- BT 에디터 디버그 viz의 성능 교훈(매 갱신 전면 재스타일 금지)을 반복하지 않도록, 증분 갱신으로 작성한다.

---

## 3. 권장 착수 순서

1. **P3-1** (씬 배치 검증) — 나머지 작업의 전제. 이게 깨져 있으면 다른 개선의 효과 측정이 불가능하다.
2. **P0-1 ~ P0-3** — 기능이 있는데 안 도는 케이스. 리스크 대비 효과 최대.
3. **P1-1** (player-breather) — 다인전 스턴락 대책의 남은 마지막 축.
4. **P1-2, P1-3** — 그룹 크기 튜닝 축 확보.
5. **P0-4, P1-4, P1-5** — 순서 무관.
6. **P2** 전체 — 위가 안정된 뒤 콘텐츠 성격에 맞춰 선별.

---

## 4. 영향 파일

| 파일 | 변경 | 관련 항목 |
|------|------|-----------|
| `GameActor/Group/MonsterGroupController.cs` | 확장 | P0-1, P0-2, P0-4, P1-1~1-4, P2-1~2-4, P3-2 |
| `GameActor/Group/MonsterGroupMemory.cs` | 확장 또는 축소 | P0-4, P1-5 |
| `GameActor/Component/Enemy/EnemyFlyingAIController.cs` | 확장 | P0-3 |
| `GameActor/Object/Monster/MonsterActor.cs` | 확장 | P0-2(self-register 안 채택 시), P0-4 |
| `GameActor/Component/Enemy/EnemyAIController.cs` | 소폭 | P1-5 |
| `GameActor/Object/Player/PlayerActor.*.cs` | 소폭 | P1-1 통지 경로 |
| `Data/Actor/Enemy/EnemyBehaviorSO.cs` | 필드 추가 | P2-4 |
| 신규 에디터 체커 | 신규 | P3-1 |

---

## 5. 비목표 (Out of Scope)

선행 문서 §9를 그대로 승계한다.

- EQS 도입 안 함. formation은 환경 무시 각도 분할 유지.
- GOAP / Hierarchical BT 도입 안 함.
- 플레이어 N-gram 예측 모델 도입 안 함.
- 그룹이 개별 State를 직접 전환하지 않는다. 그룹은 슬롯·Bias·공유 메모리만 제공하고 실행은 각 멤버 BT/Intent Resolver 담당.

추가 비목표:

- 비행 formation(고도축 분리)은 P0-3 2단계로 미룬다.
- 플레이어 Poise 시스템은 이 문서 범위 밖(선행 문서 §7-A ② 참조).

---

## 6. 검증 시나리오

| 항목 | 시나리오 |
|------|----------|
| P0-1 | `_startDormant` 그룹을 트리거로 활성화 → 전 멤버가 켜지고 BT가 도는지 |
| P0-2 | 비활성 상태로 배치된 멤버가 활성화 후 `AliveCount`에 포함되고 `OnGroupDefeated`가 정확히 1회 발동하는지 |
| P0-3 | 지상2+비행2 그룹에서 동시 공격자 수가 슬롯 상한을 넘지 않는지 |
| P1-1 | 플레이어 피격 중 신규 공격 슬롯 부여가 멈추고, 기존 점유자의 진행 중 공격은 유지되는지 |
| P1-2 | 2인/5인/8인 그룹에서 동시 공격자 수가 의도한 비율로 스케일되는지 |
| P1-3 | 접근 중 슬롯 교체 진동(다가오다 물러나기)이 사라지는지 |
| P3-1 | 체커가 그룹 미소속 몬스터를 빠짐없이 보고하는지 |

Blackboard 키(`Group.*`)와 formation Gizmos로 런타임 확인 가능.
