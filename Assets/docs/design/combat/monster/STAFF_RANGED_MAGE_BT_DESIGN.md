# Staff 원거리 마법사 BT

[작업 상태](../../../../../docs/work-status/monster-bt-improvement/STAFF_RANGED_MAGE.md) · [전투 위키](../../../../../docs/wiki/combat.md)

## 목표와 조사 근거

플레이어가 Staff를 거리 확보 후 마법을 시전하는 원거리 적으로 읽게 한다. 대상은 `AbilitySet_P09Staff`를 사용하는 P09 Staff 14종이다. 별도 전투 세트를 쓰는 구형 YeonHoa 프리팹은 이번 대상에 포함하지 않는다.

- [Guerrilla, Killzone 전투 AI 발표](https://www.guerrilla-games.com/media/News/Files/gdce05_killzone_ai.pdf), 6~14쪽: 공격 위치는 선호 전투 거리·사선·위협을 함께 평가한다.
- [Microsoft, Halo 2 AI Engineering Outline](https://learn.microsoft.com/en-us/halo-master-chief-collection/h2/ai/aiengineeringoutline): 공격 위치와 행동 구역을 분리하고 사격·후퇴 위치에 공간 정보를 사용한다.

여기서 도출한 프로젝트 적용 판단은 거리 확보를 근접 압박보다 우선하고, 사용 가능한 주문의 사거리를 추격 조건과 분리하는 것이다. 외부 자료는 아래 거리·시간 수치의 근거가 아니다. 수치는 현재 Bolt/Beam 2~20m, Sweep 0~2m와 기존 이동 속도를 기준으로 한 초기 조율값이며 체감 검수가 필요하다.

## 기존 원인

P09 Staff는 궁수 BehaviorData와 BT를 공유한다. 일반 사격은 8m 이하 조건을 쓰며 Flank·Counter·GapCloser가 섞인다. Staff Beam의 역할은 GapCloser로 복제되어 궁수 Signature 분기에 들어가지 못하고, Bolt와 Sweep은 모두 Basic/Opener다. 또한 Chase는 필터 없이 접근하면 보유 근접 공격의 사거리까지 정지 거리를 줄인다. 단순히 SO의 선호 거리만 늘려서는 해결되지 않는다.

실제 Play 관찰에서 추가로 궁수 공유 전략의 동일 Ability 반복 상한에 걸려 Bolt가 `RepetitionLimit`로 제외됨을 확인했다. 레벨 20 미만 Staff의 유일한 원거리 기본 주문을 막으면 다른 공격을 고르려 근접하거나 영구 대기할 수 있다. 전용 `CombatStrategy_P09Staff`는 동일 Ability 반복 상한을 0(무제한), 반복 점수 배율을 1로 두고 GAS 쿨다운·행동 딜레이·재배치가 발사 리듬을 제어하게 한다. 궁수 공유 전략은 보존한다.

## 행동 계약

1. 공격·피격·회피 등 잠긴 상태와 타깃 부재 처리를 최우선으로 둔다.
2. 6m 이내에서는 12m 확보를 위한 후퇴를 먼저 요청한다. 3.5초 간격으로 제한하며 기존 구역·회피 제한을 보존한다.
3. 후퇴 쿨다운 중 2m 이내에서만 Basic 긴급 공격을 허용한다. 4초 간격으로 제한한다. Sweep을 쓰기 위해 접근하지 않는다.
4. 6m 밖에서는 사선·행동 딜레이·공격 슬롯·실제 Ability 사용 가능 여부를 검사한다. Bolt는 GAS의 실제 사거리까지 쓴다. 레벨 20 이상 Beam은 0.35초 준비 후 재검사하고 7초 간격으로 사용한다. 기존 MotionKey·모션·피해·GAS 쿨다운을 유지한다.
5. 실제 공격 사거리 밖이며 12m보다 먼 경우에만 Chase를 요청한다. 쿨다운 대기를 사거리 부족으로 해석하지 않는다. 사거리 안에서는 Idle 전환으로 기존 Chase를 끝내 근접 사거리까지 계속 걷는 것을 막는다.
6. 공격 사이와 사선이 막혔을 때 0.65초 Circle로 재배치한다. Flank·Charge·가드·반격의 무작위 선택은 사용하지 않는다. 후퇴/재배치가 막히면 근접 돌진 대신 시전 위치에서 다음 판단을 기다린다.

## 연결과 재생성

Rules JSON → importer 생성 BT → 전용 `BehaviorData_P09Staff` → ActorDefinition·MonsterProfile·프리팹 AI 참조를 일치시킨다. 기존 ID·GUID·모션을 보존한다. P09 제작표의 `behaviorSource`는 기존 최하위 Staff 정의를 재사용한다. 빌더는 생성 전에 원본의 행동·스킬·도감과 임시 복제본을 확보하므로 별도 미등록 ActorDefinition을 늘리지 않고 재제작 시 궁수 행동으로 되돌아가는 문제를 막는다. 전체 제작은 외형/데이터를 폭넓게 재생성하므로 이번 검증에서는 실행하지 않는다.

좁은 공간의 완전한 전술 위치 탐색·주문 신규 제작·궁수 공통 BT 수정은 범위에 포함하지 않는다. 실제 사선 우회 성공과 플레이어의 추격·공략 가능성은 사용자 테스트로 판정한다.

## 공격 구성 확장

후속 사용자 요청으로 주문 신규 저작을 이번 범위에 포함한다. [확장 상태·검증](../../../../../docs/work-status/monster-bt-improvement/STAFF_ABILITY_EXPANSION.md). 원형은 원거리 마법사를 유지한다. 기존 3종의 ID/GUID를 보존하고 신규 3종만 추가한다.

| Ability | 역할·거리 | 원본 MotionKey·발사/접촉 | 레벨 |
|---|---|---|---|
| Bolt | 단발 견제, 3~20m | Staff.Light.01 / 0.37초 | 1 |
| Fan | 부채꼴 3발, 5~16m | Staff.Ability / 0.65초 | 10 |
| Beam | 직선 즉발, 6~20m | Staff.Heavy.00 / 1.00초 | 15 |
| ChargedBeam | 긴 준비 뒤 강한 직선 즉발, 8~20m | Staff.Ultimate / 1.05초 | 25 |
| Sweep | 짧은 근접 견제, 0~2m | Staff.Light.00 / 0.25초 | 1 |
| Repulse | 밀착 추격 대응 강타, 0~2.5m | Staff.Counter / 0.45초 | 15 |

신규 Ability/Payload/Motion은 몬스터 전용으로 독립 복제하고 원본의 접촉·발사 타이밍을 유지한다. 준비 VFX는 시전 시작부터 표시하고 발사 SFX는 판정과 일치시킨다. 몬스터용 타임라인에서 플레이어 Combo/Cancel 이벤트를 제거하고 기존 오른손 쥐기 레이어를 유지한다. 모든 투사체는 EnemySkillTarget·StaffMagicMuzzle·유효 HitPhase로 실행한다. 피해 수치는 기존 몬스터 Bolt/Beam/Sweep을 기준으로 조율한 초기값이며 체감 검수가 필요하다.

Fan은 26도 3발, 각 발은 Bolt 피해의 0.55배다. 단일 투사체와 구분되는 횡이동 견제이며 동시 전탄 피해와 경직을 제한한다. ChargedBeam은 Beam 피해의 1.5배와 긴 쿨다운·붉은 위험 예고를 사용한다. 복제한 전용 Hitscan 정의는 벽 차단·단일 타격을 유지한다. Repulse는 Sweep 피해의 1.2배와 강제 보장하지 않는 넉백으로 근접 공략을 무력화하지 않는다. 모든 근접 대응은 사용 후 후퇴를 시도하며 접근용으로 선택하지 않는다.

BT는 공격 역할로 후보를 좁힌다. Signature=ChargedBeam, Punish=Beam, Finisher=Fan, Opener=Bolt, Counter=Sweep/Repulse다. GAS 쿨다운이 실제 사용 빈도를 소유하며 BT는 공격 기회와 거리·사선·슬롯을 판단한다. 신규 긴 준비 동작 앞에 무표정 Wait를 추가하지 않는다. 14종은 공용 Set과 Motion 매핑을 통해 변경을 받는다. 제작표에도 6종을 기록하되 기존 전투 세트를 삭제해 재생성하는 작업은 실행하지 않는다.

## 원거리 추가 확장과 레벨 잠금

사용자 추가 요청으로 원거리 4종을 더 저작해 최종 투사체 4·Beam 4·근접 2종으로 구성한다. [후속 상태](../../../../../docs/work-status/monster-bt-improvement/STAFF_RANGED_EXPANSION.md). 실제 P09 성장 단계 10/15/20/25/30/40/50에 새 행동을 순서대로 연결한다.

| 해금 레벨 | 추가 공격 | 거리·쿨다운 | 모션·판정 |
|---|---|---|---|
| 20 | Burst: 마력 연사 | 6~18m / 5초 | Staff.Light.05를 0.7배속으로 재생하고 이벤트도 같은 비율로 옮겨 약 0.41초부터 0.16초 간격 3발. 접근 워프 제거, 1발 피해는 Bolt의 0.5배 |
| 30 | Homing: 추적 마력탄 | 6~18m / 6초 | Staff.Dash의 0.40초 발사. 11m/s, 유도 시작 0.25초, 초당 회전 65도·추적각 70도, 피해 Bolt의 1.25배 |
| 40 | DoubleBeam: 두 겹 섬광 | 8~20m / 13초 | Staff.Charge의 집중/발사 2클립 재사용. 플레이어 홀드 Loop 제거, 발사 클립 0.15/0.45초에 독립 HitPhase 2개, 각 피해 Beam의 0.65배 |
| 50 | FanBeam: 갈래 섬광 | 9~20m / 14초 | Staff.Ultimate의 1.05초 발사에 24도 3갈래 즉발. 각 피해 Beam의 0.45배, 가드 가능 |

Burst는 Basic/Opener로 Bolt와 교대하고 Homing은 Basic/Punish, FanBeam은 Skill/Finisher, DoubleBeam은 Skill/Signature로 요청한다. 동일 역할의 변형은 GAS의 거리·쿨다운·레벨 필터를 통과한 후보끼리만 선택한다. 근접 대응·영역 복귀는 기존 계약을 유지한다.

잠금 단일 소스는 `AbilityAttackInfo.requiredLevel`이다. GAS 후보 선별의 `IsUnlockedForLevel`을 사용하고 BT에 레벨 숫자를 중복 입력하지 않는다. 14종의 EffectiveLevel에서 열린 목록을 검사하고 각 해금 직전/해금 레벨을 비교한다. 예상 개수는 Lv10=3, Lv15=5, Lv20=6, Lv25=7, Lv30=8, Lv40=9, Lv50=10이다. 데이터 잠금 검사와 실제 인게임 시전을 구분한다.
