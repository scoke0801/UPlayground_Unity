# Staff 투사체·Beam 추가 확장과 레벨 해금

[설계](../../../Assets/docs/design/combat/monster/STAFF_RANGED_MAGE_BT_DESIGN.md#원거리-추가-확장과-레벨-잠금) · [전투 위키](../../wiki/combat.md) · [이전 6종 작업](STAFF_ABILITY_EXPANSION.md).

2026-11-14. 사용자 요청에 따라 Burst/Homing/DoubleBeam/FanBeam 4종을 추가하고 최종 10종을 실제 P09 성장 단계에 연결한다. 기존 공용 Set·매핑·GUID와 사용자 미커밋 변경을 보존한다. 이번 시작 시 추가로 존재한 `Projectile_P09Staff_Bolt_Parry` 등 다른 작업의 산출물도 보존한다.

## 조사·준비 단계

실제 P09 레벨은 10/15/20/25/30/40/50이다. 신규 해금은 Burst20/Homing30/DoubleBeam40/FanBeam50으로 정하고 기존 Beam15/Repulse15/ChargedBeam25를 유지한다. 잠금은 GAS `requiredLevel`과 실제 후보 선별 경로로 적용한다.

시작 시 다른 변경의 `ProjectileAttackDataTests.cs` 구문 오류(CS0106/CS1529)가 확인돼 에셋 저장 전 정상화를 확인한다. 새로운 타입이나 직렬화 구조는 추가하지 않으며 기존 투사체 motion/pattern·HitPhase·MotionEvent를 사용한다. 저작·검증·인게임 품질은 아직 완료가 아니다.

## 저작·연결 단계

Console 오류 0과 컴파일 종료를 확인한 뒤 신규 4종의 Ability/Payload/Motion/전용 Projectile 정의를 생성하고 기존 Set/매핑에 연결했다. Burst는 원본 클립과 이벤트를 함께 0.7배속으로 조정했으며, DoubleBeam은 홀드 Loop를 제거하고 두 발사·SFX와 독립 HitPhase를 연결했다. 기존 6종·사용자 추가 Parry 정의는 변경하지 않았다. 실제 Set에 총 10개 AI 공격과 해금 레벨이 로드됨을 확인했다.

BT에 Skill/Finisher(FanBeam)·Basic/Punish(Homing) 분기를 추가했다. Burst는 Basic/Opener 후보, DoubleBeam은 Skill/Signature 후보로 기존 분기를 공유한다. 해금 레벨을 BT에 중복 기록하지 않는다. 제작표에도 신규 4개 목록을 반영했다.

기존 fixture에 10종 연결·신규 투사체 스냅샷/도달거리, 해금 직전/직후의 실제 EnemyAbilitySelectionPolicy, 7개 성장 단계 남녀 14개 정의의 해금 개수 검사를 추가했다. 컴파일 요청 시 다른 테스트가 실행 중이라 `tests_running`으로 거절됐고 이를 취소하거나 우회하지 않았다. 컴파일·테스트 재실행은 대기 중이다.

## 검증 결과

- [실제 14종 잠금 목록](staff-ranged-expansion/level-unlocks.txt): Unity에서 ActorDefinition.EffectiveLevel과 공용 AbilitySet을 로드하고 실제 `EnemyAbilitySelectionPolicy.Evaluate`의 LevelLocked 결과로 열린/잠긴 목록을 분리했다. 남녀 모두 Lv10=3, Lv15=5, Lv20=6, Lv25=7, Lv30=8, Lv40=9, Lv50=10으로 일치한다. 런타임 EnemyCombat.CurrentLevel이 소유 MonsterActor.Level을 읽는 경로도 코드에서 확인했다.
- [BT 원본](staff-ranged-expansion/bt-validation.txt): 87노드, importer 경고 0, 그래프 오류/경고 0. 기존 BT GUID 보존.
- [Ability 기준선](staff-ranged-expansion/ability-before.txt) / [변경 후](staff-ranged-expansion/ability-after.txt): 기존 오류 7건 그대로, 신규 오류 0, P09Staff 관련 오류/경고 0.
- 신규 4종의 Ability/Payload/Motion/Projectile 및 meta가 실제 존재하며 DoubleBeam의 두 HitPhase 모두 자기 Projectile을 참조한다. Burst의 이벤트는 0.7배속에 맞춰 옮겼고 발사 패턴은 0.16초 간격 3발이다. Homing의 11m/s·2초 수명은 18m 사거리를 커버한다.
- 확장 C# 테스트 소스는 정상 컴파일됐으나 정식 NUnit 실행 요청은 `tests_running`으로 반복 거절됐고 MCP 연결도 간헐적으로 끊겼다. 사용자의 병행 작업 여부를 비동기로 확인 요청했으며 다른 테스트를 중단하거나 stuck 상태를 임의 초기화하지 않았다. 조회한 타 테스트 job 110건은 이 작업의 합격 근거로 쓰지 않는다. 추가한 fixture의 정식 러너 결과는 **미확보**다. 이전 6종의 15/15를 10종의 합격으로 승계하지 않는다.

**판정: 10종 데이터·BT·레벨 잠금 적용, 실제 14종의 GAS 해금 목록 확인, BT/Ability 검사 통과. 정식 NUnit 재실행과 신규 4종 인게임 시연·최종 미술 품질은 미완료다.**

## 후속 직접 테스트

LakeVillageWorld 또는 전투 시험장에서 해당 레벨의 P09 Staff와 교전한다. 키보드/마우스 또는 게임패드의 현재 이동·가드·회피 입력을 사용하고 저장 데이터를 덮어쓰지 않는다.

| ID | 대상·조작 | 기대 결과·실패 기준 |
|---|---|---|
| R01 | Lv15와 Lv20, 8~16m에서 옆으로 이동 | Lv15는 Burst를 쓰지 않고 Lv20부터 3발이 시간차로 나온다. 조준이 매 발 순간이동해 회피를 무효화하거나 시전자 처치 뒤 발사가 계속되면 실패 |
| R02 | Lv25와 Lv30, 이동 후 발사 직전에 방향 전환·엄폐 | Lv30부터 Homing 사용. 유도 시작과 완만한 선회가 읽히며 벽에서 차단된다. 즉시 뒤돌아 추적·벽 관통이면 실패 |
| R03 | Lv30과 Lv40, Beam 준비 후 가드·회피 | Lv40부터 DoubleBeam 사용. 두 번의 발사음·Beam·피해 시점이 구분되고 각 펄스는 한 번만 타격한다 |
| R04 | Lv40과 Lv50, 9~18m에서 좌우 이동·가드 | Lv50부터 FanBeam 3갈래 사용. 가까이 붙으려 돌진하지 않고 갈래 사이 회피/가드 대응이 가능해야 한다 |
| R05 | Lv10/15/20/25/30/40/50 모두 교전 | 원거리 마법사 거리 유지·근접 대응 2종을 보존하고 낮은 레벨이 높은 레벨 주문을 사용하지 않아야 한다 |

회신: `R01~R05 / 통과·실패·미수행 / 레벨·실제 결과·입력 장치 / 필요한 영상·로그`.

## 보존·정리

기존 6종과 Parry 정의를 보존했고 신규 4종만 독립 복제했다. 플레이어 Staff 원본·프리팹·다른 작업의 C# 파일은 수정하지 않았다. 새 에셋/meta, 검사 원본·레벨 목록·테스트 소스·문서·하네스 준비표를 보존한다. 작업 전 `output/staff-ranged-expansion-before` 사본은 기존 미커밋/미추적 에셋 복구용이라 보존한다. 테스트 액터·가상 입력·무적은 생성하지 않았고 Player/Addressables 빌드·커밋도 실행하지 않았다.
