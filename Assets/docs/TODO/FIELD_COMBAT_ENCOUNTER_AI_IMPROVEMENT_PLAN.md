# 필드 전투 조우·AI 개선 기획안

> 작성일: 2026-11-05
> 대상: LakeVillageWorld 필드 몬스터 배치(448마리) 및 몬스터 AI
> 근거: `Tools/OpenWorld/*resolved.json`, `output/OpenWorld/MonsterPopulation/RESULT.md`, `Assets/10.Datas/Actor/Enemy/BehaviorData/`, `Assets/10.Datas/AI/BehaviorTree/`, `Assets/docs/Complete/MONSTER_GROUP_AI_ADVANCEMENT_DESIGN.md`
> 상태: 2026-11-06 단계 0~5 실행. 실행 결과와 남은 일은 **0절**, 아래 1~8절은 원래 기획 기록이다.

---

## 0. 실행 결과 (2026-11-06)

### 0.1 한 일

| 단계 | 결과 |
|---|---|
| 0 집계 | 씬 454마리 중 141마리가 Test BT(`BT_EnemyBehavior_Test_AggressiveGroundMelee_Tuned`) 사용. Ent 43·Golem 34·MainPlant 31·ChildPlant 17·PlantRoot 10·Spider 4·Lich 2. 원자료 `output/OpenWorld/MonsterPopulation/Improvement/ai-usage.tsv` |
| 1 종별 BT | 신규 Rules JSON 3종(`EnemyBehavior_Golem_Juggernaut`·`Ent_ForestWarden`·`ChildPlant_Swarm`), 기존에 작성만 되고 연결되지 않았던 `MainPlant_Stationary`·`PlantRoot_Stationary`를 가져와 각 `BehaviorData_*`에 연결. Test BT 사용은 135마리 줄어 Lich·Spider 6마리만 남음 |
| 1 GAS | Golem·Ent 발구르기(`Attack.3.03`·`4.04`)를 Basic→Heavy, `aiRoles=Punish`, 위험 링 사용. 판정 전 예비 동작은 약 1.0초(모션 `BeginCollisionEvent` 1.03~1.06초) |
| 2 레벨 변주 | 신규 `EnemyLevelTendencyProfileSO`(`EnemyBehaviorSO.levelTendency`): 레벨 구간별 공격성·반응·반격·처벌 확률 배율과 행동 대기 배율. 스코어러 입력과 기본/콤보 대기에만 적용하고 헛손질 페널티는 그대로 둔다. 필드 몬스터 BehaviorData 15종에 `EnemyLevelTendency_Field`(Lv1 0.6~0.85배·템포 1.25배 → Lv40 1.4~1.45배·템포 0.78배) 연결. 공격 해금 레벨(`requiredLevel`)로 입문 적의 돌진기·대표기를 잠그는 안은 시도 후 철회했다 — `HumanoidArchetypeSelectionTests`가 지키는 "모든 역할은 도달 거리를 가진다" 계약과 충돌하고, 저레벨 궁수의 근접 탈출 수단(반격)을 빼면 근거리에서 할 행동이 사라진다(`EnemyAttackRangePolicyTests`). 레퍼토리 성장이 필요하면 역할 계약을 레벨 기준으로 다시 정의하는 별도 설계가 먼저다 |
| 3 조우 | 입문 구간 신규 조우 11개(Lv1 2·Lv3 3·Lv5 6, 1인 결투·2인·혼성·3인 방어진·식물 군락)와 Lv10·15·20·25·30·40 4인 거점(근접 2+원거리 2) 6곳, 총 31마리 추가. 필드 성장 배치 423마리·176조우. 기존 진행 ID·좌표 변경 0, 신규 개체 충돌 0·왕복 NavMesh 통과 |
| 4 슬롯 | 실측으로 **양보 교착** 발견·수정(0.2절) |
| 5 이동 | 고정형 식물 스폰 이탈 최대 0.6m(미끄러짐 없음). 숲 장거리 `PathPartial`의 추격 영향은 이번 표본으로 판단 불가 |

### 0.2 실측으로 찾아 고친 결함 — 공격 슬롯 양보 교착

"슬롯이 없으면 선회" 규칙이 공격 규칙보다 우선순위가 높았다. 슬롯 값(`HasAttackSlot`)은 공격을 **시도**한 노드만 갱신하므로, 처음부터 `False`인 적은 공격을 시도하지 못하고 선회만 반복했다. 신규 골렘 BT 1차 표본에서 15초 동안 공격 0회, 선회 75%였다. 휴머노이드 BT 5종도 같은 구조였다(혼자인 Lv3 적의 선회 64%).

양보 규칙을 각 BT의 `50 Default Combat Rhythm` 그룹 안, 공격 규칙 바로 아래로 옮겼다. 별도 그룹을 그 아래에 두면 50번 그룹 끝의 조건 없는 `RhythmIdle`이 항상 성공해 도달하지 못한다. 이제 공격을 먼저 시도하고, 그룹이 거절하면 같은 틱에서 양보한다.

### 0.3 Play Mode 관찰 (에디터, 무적 플레이어가 서 있기만 한 상태, 조우당 25초)

도구: `Tools/OpenWorld/sample-encounter-ai.csx` → `summarize-encounter-sample.mjs`. 결과 `output/OpenWorld/MonsterPopulation/Improvement/play-sample-final.tsv`(양보 수정 후 5개 조우), 식물 행은 수정 전 `play-sample-3.tsv`(식물 BT는 양보 규칙 위치만 다름).

| 조우 | 공격 비율 | 동시 공격 | 위험 링 | 비고 |
|---|---:|---:|---:|---|
| 골렘 둘 Lv25 | 33% | 0% | 1/5 | 수정 전 0%. 선회 49% |
| 엔트 둘 Lv20 | 41% | 12% | 2/6 | |
| 새싹+큰 식물 Lv10 | 25% | 3% | 0/8 | 큰 식물이 소환·스킬 사용, 스폰 이탈 ≤0.6m |
| 입문 1인 Lv3 | 18% | - | 0/3 | 선회 62%. 의도가 Pressure·Defend·KeepDistance로 쏠림 |
| Lv20 거점 4인 | 28% | 24% | 8/12 | 근접 1+원거리 1 동시 허용이 설계 의도 |
| Lv20 2인 | 35% | 10% | 4/6 | |

**자동 검증:** BT JSON 정적 검증 27개 파일 오류 0. EditMode AI·Ability·Combat·Core 700개 중 692 통과·8 실패. 실패 8건(SeolA 스킬트리 Ability, WoodlandWolf 등급 규칙, Sera 눈 깜빡임 프로필, NPC 장비 표시 테스트의 콜라이더 요구, 씬 도착 ID 테스트의 리플렉션 모호성)은 이번 변경 파일과 무관한 영역이다. 이번 작업 전에도 실패했는지는 확인하지 않았다. 신규 `EnemyLevelTendencyProfileTests` 7개 통과. `AbilityDataValidator` 전수 검증에서 Golem·Ent에는 아이콘·로컬라이즈 Warning/Info만 있고, Error 7건은 플레이어·패시브 데이터의 것이다.

**검증 한계:** 플레이어가 반격·회피하지 않는 정지 표본이다. 손맛·난이도·프레임은 사용자 플레이 검수 대상이다. 관찰 중 게임 안내 팝업이 시간을 멈추는 경우가 있어 도구가 기록 후 닫는다.

### 0.4 남은 일과 범위 밖 발견 (임의 수정하지 않음)

Humanoid 후속 작업에서 카타나·채찍 Lv30 추가기, 쌍검 보스 후반기와 Sera/Nenmir 대검/활 연결을 교정했다. [현재 결과와 남은 창병 선택](HUMANOID_GRADE_FOLLOWUP_RESULT.md), [직접 플레이 G12~G17](HUMANOID_GRADE_PLAYTEST.md)을 따른다. 3인/근접1+원거리1의 자동 슬롯 계약 검사와 시험장 기본 6인/근접2+원거리1을 구분한다. 자동 슬롯 반환 검사와 상태 주입은 자율 AI·거리 관리·연속 히트스톱 체감 검수의 완료 근거가 아니다.

1. **대검의 검방패 Set 참조 교정 반영.** 실제 대검 무기·MotionSet을 확인하고 `GreatSword_002`와 Armor08~12의 Profile/Definition을 대검 등급 Set으로 교체했다. GUID·배치·진행 ID를 유지했으며 전체 P09 재빌드는 하지 않았다. 생성/검증은 제작표의 Normal/Elite Set GUID를 같은 함수로 선택한다. 상세 결과·남은 Play 검수는 [등급 공격 설계 12절](HUMANOID_MONSTER_GRADE_SKILL_VARIATION_DESIGN.md#12-현재-작업본-구현-결과)을 참고한다.
2. **큰 식물 근접 안전지대(보조 히트박스 적용, 재측정 대기).** `Monster_Plant_V1~V3`에 루트 직속 `[HitBox] Close`(박스 중심 (0,0.8,0.7)·크기 (2.8,1.8,2.0), 그룹 `Close`)를 추가하고 `MainPlant_Attack`의 `BeginCollisionEvent.additionalHitboxGroupIds`에 `Close`를 연결했다. 이전 분석 리포트(변경 전)에서 Plant_1·2는 실측 하한이 0.60~0.65m였고 하한 1.15m는 Plant_3(V3, 히트박스가 Spine3 아래)에서 나온 값이었다. `GA_MainPlant_01_Attack_1.minDistance`는 아직 1.15 그대로다. Edit Mode에서 `AnalyzeAll`을 다시 돌려 세 프리팹의 추천 하한을 확인한 뒤 낮춰야 한다(`BakeAll`은 프로젝트 전체를 덮어쓰므로 쓰지 말고 이 Ability 하나만 반영). 아래는 변경 전 원인 기록이다. `GA_MainPlant_01_Attack_1.minDistance 1.15`는 임의값이 아니라 `MonsterMeleeRangeBakeTool`이 `[HitBox] Default`의 애니메이션 포즈를 대상 캡슐과 겹쳐 본 실측 하한이다(`MeasureAttachedHitboxRange`). 즉 데이터로 `minDistance`만 낮추면 공격은 발동하지만 헛손질이 된다. 해결은 (a) 근접용 보조 히트박스 그룹을 프리팹(`Monster_Plant_V1`)에 추가하고 해당 모션의 `BeginCollisionEvent.additionalHitboxGroupIds`에 연결한 뒤 재베이크하거나, (b) 근접 광역기 모션을 추가하는 것이다. 둘 다 프리팹·모션 저작과 Play Mode 육안 확인이 필요해 에디터 시각 검증 없이 적용하지 않았다.
3. **입문 1인 결투가 수동적**(선회 62%). Lv1 성향 배율(공격성 0.85)과 스코어러 의도 분포의 조합이다. 사용자 플레이로 체감을 확인한 뒤 `EnemyLevelTendency_Field` Lv1 구간이나 Intent Weights를 조정한다.
4. **Test BT 잔존 6마리**(Lich 2·Spider 4)는 종 설계가 필요하다.
5. **기존 배치 드리프트(해결: 의도된 이동으로 확인, 감사 기준 수정).** 8마리는 `world-playability` 계획(`plan-world-playability.mjs`의 `moves`)이 폐채석장·석관 묘역·숲 보루·역참 설계에 맞춰 옮긴 개체이며, 씬 좌표는 `world-playability-resolved.json`과 수평 오차 0.000으로 일치한다. 되돌리면 사이트 설계가 깨지므로 좌표는 그대로 두었다. `audit-monster-population.csx`가 이동 전 좌표를 기준으로 삼아 오류를 냈던 것이므로, 이동 대상은 후속 계획 좌표로 비교하고 시작점 거리 성장 구간·본길 왕복 검사에서 제외하도록 고쳤다(사이트 진입로 왕복은 `audit-world-playability`가 담당). 이동 대상 8마리는 사이트 설계 레벨(Lv20~25)을 유지한다. 수정한 감사 스크립트 전체 실행은 하지 않았다.
6. **ChildPlant 프리팹 BT 실행기 3개 중복**(`Monster_ChildPlant_V1`)과 스켈레톤 프리팹 2개 중복. 첫 실행기만 쓰이고 나머지는 빈 트리로 틱에 등록된다.
7. 숲 장거리 `PathPartial`의 실제 추격·복귀 영향은 미확인이다.
8. 씬·지역 데이터 백업 정책(Git LFS 또는 별도 저장소)은 사용자 결정 대기다. 이번 작업 전 씬은 `Improvement/scene.before.unity.gz`(SHA1 `40938fdb…`)로 보존했다.

---

## 1. 목표

필드를 걸으며 만나는 전투가 **종마다 다르게 느껴지고, 레벨 구간마다 새로운 판단을 요구하며, 반복해도 지루하지 않게** 만든다. 수치만 올라가는 같은 전투를 반복하는 상태에서 벗어나는 것이 핵심이다.

## 2. 현황 요약

### 2.1 배치

| 항목 | 현재 값 |
|---|---|
| 신규 조우 | 273개 (2마리 175, 3마리 98) / 신규 392마리 |
| 씬 전체 | 448마리 (기존 필드 23, 시나리오 12 포함) |
| 레벨 밴드(조우 수) | Lv1:1 · Lv3:6 · Lv5:11 · Lv10:34 · Lv15:46 · Lv20:48 · Lv25:46 · Lv30:48 · Lv40:25 · Lv50:8 |
| 조우 규칙 | Lv20 미만 3마리, 이상 2마리. 근접·원거리 공격 슬롯 상한 각 1 |

### 2.2 AI

- 그룹 조율(슬롯·브리더·Intent 마스킹·공유 메모리·포메이션)은 구현 완료. 단 문서상 검증은 `dotnet build` 통과뿐이다.
- Golem·Ent·Plant 계열의 `BehaviorData`(`golem` 등)는 `BT_EnemyBehavior_Test_AggressiveGroundMelee_Tuned`를 참조한다. `SourceJson/Test/` 아래의 테스트용 BT이다.
- P09 휴머노이드는 무기별 BT 5종(`Humanoid_SwordShield/GreatSword/DualBlade/DualAxe/Bow`)을 Armor06~12 전 등급이 공유한다.

## 3. 문제 정의

| # | 문제 | 플레이어 체감 | 우선순위 |
|---|---|---|---|
| P1 | 비휴머노이드 다수가 Test BT 사용 | 골렘·엔트·식물이 비슷하게 달려들어 종 개성이 없다 | 높음 |
| P2 | 플레이 검증 전무 | 추격·복귀·프레임·체감 난이도를 알 수 없다 | 높음 |
| P3 | 조우가 2~3마리 단일 구성 | 오래 걸을수록 같은 전투의 반복 | 중간 |
| P4 | Lv1~5 조우가 18개(약 7%) | 학습 구간이 짧고 Lv10 진입이 급하다 | 중간 |
| P5 | 구역별 종 편중(Golem·Ent) | 지역 정체성이 약하다 | 중간 |
| P6 | 같은 무기 BT가 Lv5~50 공유 | 수치만 오르고 행동은 그대로 | 중간 |
| P7 | 2마리 조우 + 슬롯 상한 1 | 한 마리만 싸우고 다른 한 마리는 서 있어 보일 수 있다(미검증 가설) | 확인 필요 |
| P8 | 숲 `OffRoad=6`에서 장거리 `PathPartial` | 추격·복귀 중 끊김 가능성(미검증) | 확인 필요 |
| P9 | 씬이 Git 미추적, 생성기 미합성 | 데이터 소실·덮어쓰기 위험 | 높음(운영) |
| P10 | 기존 결함 | Definition 빈 몬스터 6마리, Armor06_Bow HP 불일치, DissolveMaterial 로드 실패 | 낮음 |

## 4. 개선안

### 4.1 단계 0 — 현황 정밀 집계 (선행, 읽기 전용)

수정 전에 사실을 확정한다. 추정치로 설계하지 않는다.

- 씬의 `MonsterActor`를 `Definition.behaviorData → behaviorTree`로 집계해 **BT별 마릿수 표**를 만든다.
- 조우별 구성(종·무기·레벨)과 구역별 종 분포 표를 만든다.
- 산출물: `output/OpenWorld/MonsterPopulation/AI_USAGE.md`.
- 방법: 기존 `audit-monster-population.csx` 패턴을 따르는 읽기 전용 스크립트. 런처에는 등록하지 않는다(AI 작업 확인용).

### 4.2 단계 1 — 종별 BT 분리 (P1, P6 일부)

**원칙:** 판단은 BT, 수치는 GAS, 타이밍은 MotionSet(전투 시스템 가이드의 3계층 경계 유지).

| 종 | 행동 방향 | 비고 |
|---|---|---|
| Golem | 느린 접근, 큰 텔레그래프, 방어 불가 대형기, 경직 저항 높음 | 위치 선점·거리 관리를 요구 |
| Ent | 중거리에서 뿌리·광역 견제 후 접근, 플레이어 후딜 처벌 | |
| Plant / RootPlant / ChildPlant | 이동 제한 또는 고정, 영역 방어·매복(범위 진입 시 개시) | 현재 추격형 BT 사용 여부 확인 필요 |
| Skeleton / Lich / Spider | 기존 전용 BehaviorData 유지, 사용 BT 점검만 | |

작업 방식:
1. `generate-bt-json` 스킬로 종별 Rules JSON을 작성한다. 이 프로젝트는 Rules JSON(스코어러 자동 부착)과 raw BT 노드 JSON의 두 포맷이 있으므로 어느 포맷으로 쓸지 먼저 정한다.
2. 기존 `BehaviorData_golem` 등 **원본 에셋은 수정하지 않고** 필요한 경우 지역 파생 `BehaviorData`를 만들어 연결한다(원본 보존 원칙).
3. 보류 상태인 공격(`isMotionContentPending`)은 AI 후보에서 이미 제외되므로 건드리지 않는다.

완료 기준: 종별로 첫 공격까지의 거리·템포·텔레그래프가 육안으로 구분된다. 컴파일·EditMode 통과만으로 완료 처리하지 않는다.

### 4.3 단계 2 — 레벨 밴드별 행동 변주 (P6)

BT 파일을 레벨마다 늘리지 않는다. 구조 선택은 다음 순서로 검토한다(데이터화 우선).

1. `BehaviorData`의 블랙보드 값(aggression, reactionChance, counterChance, dodgeChance, maxComboPressureCount 등)을 **레벨 밴드별 파생**으로 조정한다.
2. 부족하면 상위 밴드에서만 켜지는 규칙 그룹(예: 가드 파괴 공격, 처벌 확률 상승)을 추가한다.

| 밴드 | 행동 목표(안) |
|---|---|
| Lv1~5 | 단일 패턴 위주, 텔레그래프 길게, 콤보 압박 낮음. 공격·회피·가드 학습 |
| Lv10~15 | 가드·반격 반응 도입, 2연계 |
| Lv20~30 | 플레이어 후딜 처벌, 측면 이동, 원거리 지원 |
| Lv40~50 | 가드 압박·페이크, 콤보 압박 상한 상향 |

수치는 안 개념이며 단계 3의 플레이 검증 후 확정한다.

### 4.4 단계 3 — 조우 구성 다변화 (P3, P4, P5)

기존 배치·진행 ID·좌표는 보존한다. 신규 조우는 **추가 또는 기존 조우의 구성 교체**로 한정하고, 기존 GUID는 유지한다.

조우 템플릿(안):

| 템플릿 | 구성 | 의도 |
|---|---|---|
| 정찰조 | 근접 2 | 현행. 입문·반복 구간용 |
| 혼성조 | 근접 2 + 원거리 1 | 우선순위 판단(원거리 처리 vs 근접 대응) |
| 정예조 | 정예 1 + 졸개 2 | 목표 지정과 개체 강도 대비 |
| 방어진 | 근접(방패) 1 + 원거리 2 | 접근로 판단 |
| 매복 | 식물/고정형 + 근접 | 지형 활용 |

적용 규칙:
- 슬롯 상한(근접 1, 원거리 1)은 조우 크기 4 이상에서 재검토 대상이다. 한 번에 올리지 않고 템플릿 단위로 실험한다.
- 구역마다 **주력 종과 보조 종**을 지정해 지역 정체성을 만든다(예: 북동 깊은 숲은 Ent 주력, 서쪽 채석지는 Golem 주력).
- Lv1~5 밴드는 현 18개에서 시작 지점 반경 내 확충한다. 목표치는 단계 0의 분포표를 보고 정한다.
- 템플릿은 코드가 아니라 **데이터(JSON/SO)**로 저작한다. 일회성 적용 스크립트는 런처에 올리지 않는다.

### 4.5 단계 4 — 2마리 조우와 슬롯 (P7)

가설 검증이 먼저다. 플레이 로그로 "비공격 멤버의 대기 시간 비율"을 측정한다.

- 측정: 조우당 전투 시간 중 공격 슬롯 보유 비율, 비보유 멤버의 행동(Flank/Circle/KeepDistance) 비중.
- 판정: 대기 멤버가 서 있는 시간이 길면 Intent 마스킹의 비공격 폴백(Flank·Circle·Step)이 실제로 호출되는지 확인하고, 호출이 약하면 브리더 길이·폴백 가중치를 SO 값으로 조정한다.
- 근거 없이 슬롯 상한을 올리지 않는다.

### 4.6 단계 5 — 이동·추격 품질 (P8)

- 숲 `OpenWorldOffRoad` 비용 6에서 추격 중 `PathPartial`이 발생하는 지점을 샘플링해 **실제 추격 포기·복귀 실패**로 이어지는지 확인한다.
- 이어진다면 후보: ① 추격 경로 조회에 길 비용 완화 필터 적용, ② 특정 구역 NavMesh 보정, ③ 추격 포기 후 스폰 복귀 규칙 보강. 런타임 이동 비용과 기존 NavMesh 에셋은 근거 없이 바꾸지 않는다.

### 4.7 단계 6 — 운영·안정성 (P9, P10)

- 씬·지역 데이터·Terrain·전용 NavMesh의 **백업 정책**을 정한다(Git LFS 또는 별도 저장소). 현재는 로컬 백업이 유일한 복구 수단이다.
- 직접 저작 배치를 월드 생성기에 합성하기 전까지 전체 재생성은 실행하지 않는다는 경고를 README 최상단으로 올린다.
- 기존 결함은 별도 항목으로 처리한다.
  - `WorldExpansionActors`의 Definition 빈 6마리: 등록·초기화 경로를 확인 후 연결.
  - `Armor06_Bow` 저장 HP 불일치: 수동 튜닝 여부를 확인한 뒤 판단.
  - DissolveMaterial 로드 실패: 정상 초기화 후 런타임 재현 여부 확인.

## 5. 검증 계획

테스트 작업 지침(`TEST_VALIDATION_GUIDE.md`)에 따라 변경 위험에 맞춰 수행하고, 수행·실패·미수행을 구분해 보고한다.

| 구분 | 내용 | 수행 주체 |
|---|---|---|
| 정적 | 단계 0 집계, 데이터 참조 무결성 감사 | AI |
| EditMode | 기존 `MonsterAbilitySetIntegrationTests`, BT 로딩/검증 | AI |
| Play Mode(에디터) | 종별 첫 공격 거리·템포, 조우 슬롯 비율 로그, 추격·복귀 | AI + 사용자 |
| 체감 | 레벨 구간 곡선, 종별 개성, 반복 피로 | 사용자 |
| 프레임 | 448마리 중 활성 범위 기준 CPU 시간(BT 평가, 감지) | 사용자(프로파일러) |

체감과 프레임 항목은 AI가 완료로 판정하지 않는다.

## 6. 작업 순서와 의존

```
단계0 집계 ─┬─> 단계1 종별 BT ─> 단계2 레벨 변주 ─┐
            ├─> 단계3 조우 다변화 ────────────────┼─> Play 검증/튜닝
            └─> 단계4/5 측정(슬롯·이동) ──────────┘
단계6 운영(백업·경고)은 병행, 단계 3 적용 전에 완료
```

- 단계 3은 씬을 수정하므로 **적용 전 씬 백업을 선행**한다.
- 단계 1은 단계 0의 사용 현황 표가 나온 뒤 범위를 확정한다.

## 7. 리스크와 비목표

**리스크**
- 종별 BT를 새로 쓰면 기존 보스·튜닝 BT와 조작 감각이 어긋날 수 있다. 공통 규칙 그룹은 재사용한다.
- 조우 크기를 늘리면 활성 몬스터 수가 증가해 프레임 예산이 늘어난다. 단계 3 적용 전에 프로파일링 기준값을 확보한다.
- 씬이 Git 미추적이라 실수가 복구되지 않는다. 백업 전 적용 금지.

**비목표**
- 신규 Ability·MotionSet 제작(모션 콘텐츠 보류 건은 임의 연결하지 않는다).
- 보스·스토리 전투 변경.
- EncounterDirector 같은 신규 상위 시스템 도입(그룹 문서의 결론대로 `MonsterGroupController` 확장 범위 내에서 해결).
- 레벨마다 BT 파일을 복제하는 구조.

## 8. 열린 질문

1. 식물·고정형 몬스터에 지금 어떤 이동 상태가 쓰이는가? (단계 0에서 확인)
2. 4마리 이상 조우를 도입할 때 슬롯 상한을 템플릿별 값으로 둘 것인가, 전역 상수로 둘 것인가?
3. Lv1~5 확충 목표 조우 수는 얼마인가? (단계 0 분포표 이후 결정)
4. 씬 백업을 Git LFS로 갈 것인가, 별도 저장소로 갈 것인가?
