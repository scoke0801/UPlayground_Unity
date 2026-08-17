# Cycle 구현 스펙 인덱스

> 기준 정책: UPlayground 통합 기획 v3.2의 확정 사항을 구현 계약으로 분해  
> 대상: P0 개발 빌드 약 20분, 정식 사이클 최대 40분  
> 목적: 상위 기획을 독립적으로 구현·검증 가능한 작업 단위로 분리한다.

> **2026-08-02 개정 — 랜덤성 축소:** 플레이어 시작 위치(02), 보스 어시스트 영입 판정(04)에서 RNG를 제거하고, 레벨업 보상을 플레이어 선택형 스킬 노드(08)로 전환했다. 시드가 실제로 결정하는 범위는 `01_CYCLE_RUNTIME_SPEC.md` 4절 "시드가 결정하지 않는 것" 표를 기준으로 한다.

> **현행 서사 기준:** [CYCLE_STORY_PLOT.md](CYCLE_STORY_PLOT.md) — 세계 전체가 되감기는 흐름, 선택 캐릭터 고정 주인공, 생활 앵커와 관계 누적을 중심으로 한 밝은 메인 플롯. 세계는 플레이어를 심사하거나 평가하지 않는다.

---

## 1. 문서 사용 규칙

- 이 인덱스의 P0 공통 제약은 사이클 게임 규칙의 기준선이다.
- 각 단위 문서는 P0 구현 계약과 코드 접점의 권위 소스다.
- 문서의 `신규 제안` 타입명은 아직 코드에 존재하지 않는다. 구현 시 이름을 바꾸면 관련 문서를 함께 갱신한다.
- `기존`으로 표시한 클래스와 API는 2026-07-13 프로젝트 코드에서 확인했다.
- P1/P2 기능은 P0 코드에 빈 추상화만 미리 만들지 않는다. 실제 확장 시점에 추가한다.
- `CYCLE_STORY_PLOT.md`는 서사 권위 문서이며 런타임 구현 계약을 덮어쓰지 않는다. 스토리 표현과 시스템 동작이 충돌하면 구현 스펙을 우선하고 플롯 표현을 조정한다.

---

## 2. 구현 단위

| 순서 | 문서 | 구현 결과 |
|---|---|---|
| 1 | [01_CYCLE_RUNTIME_SPEC.md](01_CYCLE_RUNTIME_SPEC.md) | 사이클 상태, 시드, 시작·완료·포기 오케스트레이션 |
| 2 | [02_WORLD_SPAWN_ENCOUNTER_SPEC.md](02_WORLD_SPAWN_ENCOUNTER_SPEC.md) | N개 스폰 후보 추첨, 보스 생성, `?` 마커, 조우 공개 |
| 3 | [03_CHARACTER_WEIGHT_SPEC.md](03_CHARACTER_WEIGHT_SPEC.md) | 경량·표준·중량 프로필과 전투 파생값 적용 |
| 4 | [04_BOSS_ASSIST_RECRUITMENT_SPEC.md](04_BOSS_ASSIST_RECRUITMENT_SPEC.md) | 조건부 확정 보스 영입, 누적 처치 보장선, 지정 스킬 1회 어시스트 |
| 5 | [05_REMAINS_RESPAWN_SPEC.md](05_REMAINS_RESPAWN_SPEC.md) | 파티 전멸, 유해 생성·회수·재사망, 부활 |
| 6 | [06_CYCLE_SAVE_SETTLEMENT_SPEC.md](06_CYCLE_SAVE_SETTLEMENT_SPEC.md) | 실행 중 저장, 영구 저장, 탈출 정산, 호환성 |
| 7 | [07_CYCLE_UI_TELEMETRY_VALIDATION_SPEC.md](07_CYCLE_UI_TELEMETRY_VALIDATION_SPEC.md) | HUD, 조우 연출, 텔레메트리, P0 판정 기준 |
| 8 | [08_CHARACTER_SKILL_GROWTH_SPEC.md](08_CHARACTER_SKILL_GROWTH_SPEC.md) | 레벨업 포인트, 캐릭터별 고정 스킬 노드 트리, 영속 육성 |
| - | [09_DETERMINISTIC_REPLAY_ADDITIONS.md](09_DETERMINISTIC_REPLAY_ADDITIONS.md) | (제안) 랜덤 제거 후 반복 플레이 동기를 채울 추가 요소 |
| 10 | [10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md](10_CYCLE_STORY_STATE_BOUNDARY_SPEC.md) | 반복/누적 상태 소유권, 저장·로드·새 게임 경계, P0 관계 증명 |
| 11 | [11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md](11_PROTAGONIST_DIALOGUE_CONTRACT_SPEC.md) | 최초 선택 주인공 저장, Player/Protagonist 화자·초상화·토큰 계약 |
| 12 | [12_LOOP_ANCHOR_QUEST_SPEC.md](12_LOOP_ANCHOR_QUEST_SPEC.md) | 첫 원정 생활 앵커, 첫 귀환 3분기, quest_main_003·SP30 재구조 |

### 2026-08-02 구현 대조 결과

- `02`: `CycleWorldSpawnService`가 플레이어 시작점을 다시 추첨하던 구현을 제거하고 `fixedPlayerSpawnId`를 필수 검증·사용하도록 수정했다.
- `04`: 문서만 확정 판정으로 개정되고 코드는 확률/pity를 유지하던 불일치를 수정했다. 신규 저장은 `defeatCounts`, 구버전 `pity.failures`는 누적 처치 수로 1회 호환 이관한다.
- `08`: 고정 스킬 트리 런타임·저장·전투 보정·전용 `UI_Scene_SkillTree`·검증기를 추가했다. 보쿠세이 14노드와 나머지 현행 플레이어블 10명의 13노드 v1을 실제 무기 Ability·패시브에 연결했다. H09는 미사용 타입이므로 확장 대상에서 제외한다.
- `09`: 여전히 후보 제안서다. A~G 중 채택되지 않은 기능은 구현 계약으로 간주하지 않는다.

---

## 3. 의존 관계

```text
01 Cycle Runtime
 ├─ 02 World Spawn & Encounter
 ├─ 03 Character Weight
 ├─ 04 Boss Assist & Recruitment
 ├─ 05 Remains & Respawn
 ├─ 06 Save & Settlement
 └─ 07 UI & Telemetry

08 Character Skill Growth  (사이클과 독립된 영속 진행)
 ├─ 06 Save & Settlement (영구 저장 섹션)
 └─ 07 UI & Telemetry (노드 취득 지표)

02 World Spawn & Encounter
 ├─ 04 Boss Recruitment (보스 식별/처치 결과)
 └─ 07 UI (미발견/발견 마커)

05 Remains & Respawn
 └─ 06 Save & Settlement (유해 상태 영속)

10 Cycle Story State Boundary
 ├─ 06 Save & Settlement (주인공·플래그·BossAssist 영속)
 ├─ 11 Protagonist Dialogue Contract
 └─ 12 Loop Anchor Quest

11 Protagonist Dialogue Contract
 └─ 12 Loop Anchor Quest (주인공 화자와 본문 토큰)

12 Loop Anchor Quest
 ├─ 01 Cycle Runtime (첫 사이클 게이트·첫 정산)
 ├─ 06 Save & Settlement (앵커 중간 상태 복원)
 └─ 07 UI & Telemetry (다단계 HUD·플레이어용 명칭)
```

`01`의 상태 모델을 먼저 확정한다. 이후 `02`와 `03`은 병렬 구현할 수 있으며, `04`와 `05`는 각각 전투와 사망 흐름에 붙인다. `06`은 각 서비스의 저장 DTO가 확정된 뒤 연결하고 `07`에서 전체 플레이 흐름을 검증한다.

---

## 4. P0 공통 제약

- 출전 플레이어블 파티는 최대 4명이며 기존 1~4번 스왑을 유지한다.
- P0 데이터는 현재 `CycleWorld_lakeoflife` 외곽 보스 풀과 성장 데이터가 함께 존재하는 Honoka, Bokusei, Hichi 3명을 우선 저작한다. `H09`는 현재 성장 데이터가 없고 enum에서도 미사용 상태이므로 P0 대상이 아니다.
- 기술상 외곽·중앙 배치 모두 조우 전에는 `?` 아이콘과 `미확인 상대` 라벨을 쓰고, 조우 후 실제 이름·아이콘으로 바꾼다.
- 보스 어시스트는 별도 입력, 장착 1마리, 지정 스킬 1회, 비이동·비어그로다.
- 유해는 출전 파티 전멸 시에만 생성한다.
- P0 유해 손실물은 현재 레벨 경험치 진행분 30%와 미정산 재료 전량이다.
- 장비 손실, 유물, 랜덤 접사, 랜덤 이벤트, 제작, 메타 재화는 P0에서 제외한다.
- 사이클 1~3 배율만 구현한다.
- 완전 절차 생성이 아니라 수작업 검증된 스폰 후보를 조합한다.
- 플레이어 시작 위치는 설정이 지정한 단일 `spawnId`로 고정하며 시드의 영향을 받지 않는다.
- 보스 어시스트 영입은 확률 롤 없이 조건 달성 시 확정된다.
- 캐릭터 스킬 노드 트리는 캐릭터별 고정 저작이며 사이클마다 재추첨하지 않는다.
- 스킬 포인트와 취득 노드는 영구 데이터이며 정산·유해 손실 대상이 아니다.

---

## 5. 전체 완료 정의

1. 같은 시드는 같은 보스·부활 지점 배치를 생성하고, 플레이어 시작 위치는 시드와 무관하게 항상 동일하다.
2. 모든 보스는 조우 전 `?`, 조우 후 정식 아이콘과 이름으로 전환된다.
3. 기존 파티 스왑과 어시스트 입력이 충돌하지 않는다.
4. 어시스트는 지정 스킬 1회 후 항상 정리되며 적의 타겟이 되지 않는다.
5. 파티 전멸 후 유해가 하나만 존재하고 회수 또는 재사망 규칙이 정확하다.
6. 저장 후 재실행해도 시드, 배치, 발견, 어시스트 로스터, 유해가 복원된다.
7. 중앙 보스 처치 후 포털 진입 시에만 사이클이 정산된다.
8. 개발 사이클 중앙값이 약 20분이고 정식 콘텐츠도 40분 상한을 넘기지 않도록 측정 가능하다.
9. 레벨업 포인트로 찍은 스킬 노드가 사이클을 넘어 유지되고, 같은 캐릭터의 스킬판은 언제나 동일하다.
10. 보스 영입이 확률이 아니라 명시된 조건 달성으로만 발생하고 진행도가 UI에 노출된다.
