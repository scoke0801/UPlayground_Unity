# Humanoid 몬스터 등급과 레벨별 공격 변주 설계

상태: 등급·레벨 데이터에 카타나/채찍 Lv30 추가 강공격과 쌍검 Boss 후반기를 반영하고 Sera/Nenmir의 실제 프리팹·무기·Set 연결을 교정했다. 이후 사용자 결정으로 신규 일반 창병 `Enemy_Field_Spear_001`을 제작했다. 기존 Reien·진행·월드 배치는 보존했다. 기존 교정은 12절 및 [후속 결과](HUMANOID_GRADE_FOLLOWUP_RESULT.md), 최신 창병 연결·공격 8종·검증은 [창병 제작 기록](HUMANOID_SPEAR_SOLDIER_RESULT.md)을 기준으로 한다. 실제 플레이 체감 검수는 사용자 테스트 대기다. 아래 창병 미결정·콘텐츠 보류 문구는 각 절의 과거 조사 시점 기록이다.

같은 무기를 쓰는 적에게서 익힌 대응은 상위 등급에서도 통한다. 상위 등급은 그 대응에 추가 판단을 요구하는 전용기와 강화 강공격을 가진다. 레벨이 높아졌다는 이유만으로 일반 적에게 보스의 전체 공격 목록을 주지 않는다.

## 1. 현재 기준과 기존 설계와의 관계

2026-11-06 작업본 재확인 기준이다. 기존 변경이 진행 중이므로 실제 적용 직전에 다시 집계한다.

| 항목 | 확인 내용 |
|---|---|
| 무기별 공격 Ability | 8개 무기군 174개. 무기 미분류 Humanoid 12개는 별도 |
| 레벨 조건 | 현재 174개 모두 `requiredLevel=1`. 앞선 조사 때 일부에 있던 Lv10 제한은 현재 철회된 상태 |
| 등급 구성 | 무기별 공용 Set을 일반·엘리트·보스가 공유. 해당 Set의 `baseSet`은 비어 있고 `abilityOverrides`도 없음 |
| 실행 데이터 | 조사한 공격은 단일 Variant. 같은 공격을 공유하면 HitPhase 기본 피해도 공유 |
| 레벨 성향 | `EnemyLevelTendencyProfileSO`가 공격성·반응·대기 시간을 조정. 공격 목록 변경과 별개 |
| 등급 수치 | `MonsterScaling_Default`의 AttackPower 계열 성장·등급 보정이 존재. 최종 피해는 기본 피해와 같지 않음 |
| 역할 계약 | 기존 `HumanoidArchetypeSelectionTests`는 공용 Set에서 여섯 역할의 후보·도달 거리를 검사 |

`FIELD_COMBAT_ENCOUNTER_AI_IMPROVEMENT_PLAN.md`에는 역할 공격을 레벨로 잠갔을 때 도달 거리 계약과 궁수 근접 대응이 깨져 철회한 기록이 있다. 같은 방식으로 공격을 일괄 잠그지 않는다.

기존 `HUMANOID_MONSTER_GAS_BT_DESIGN.md`의 무기별 역할과 공용 기반은 유지한다. 이 설계는 등급별 실사용 Set과 검증 계약을 추가한다. 옛 문서의 미배치 무기 판정·과거 연결 표는 현재 적용 근거로 사용하지 않는다.

## 2. 등급과 레벨의 역할

| 구분 | 플레이어 경험 | 공격 구성 | 피해 원칙 |
|---|---|---|---|
| Weak | 짧게 읽고 정리하는 적 | 기본기·접근·탈출 수단 유지, 대표기 제외 | 기존 약체 스탯 사용. 별도 강화 Ability 없음 |
| Normal | 무기의 기본 대응 학습 | 시작타·마무리·응징·반격·접근을 갖춘 작은 목록 | 기본 피해 유지 |
| Elite | 익숙한 공격 사이에서 강한 한 수를 경계 | Normal 기반 + 대표기, 선택한 강공격 교체 | 분명한 예고가 있는 공격만 강화 |
| Boss | 대표기와 전환 구간에 대응 | Elite 기반 + 액터별 대표기·후반 패턴 | 선택한 대표기 강화. 모든 공격 일괄 증폭은 하지 않음 |

**등급이 허용하는 공격 목록을 먼저 정하고, 그 안에서 레벨에 따라 추가 공격을 해금한다.** 레벨은 기존 스탯 성장과 `levelTendency`도 담당한다. Lv50 Normal도 Elite 전용기를 얻지 않고, Lv1 Elite도 기본 대응과 등급을 대표하는 공격을 가진다. 공격 해금 레벨은 몬스터 자신의 현재 레벨이며 플레이어 레벨이나 평균 파티 레벨을 사용하지 않는다.

Weak와 Normal의 목록 차이가 실제로 필요하지 않으면 같은 Normal Set을 사용한다. 등급 수만큼 Set을 기계적으로 만들지 않는다.

### 2.1 레벨별 해금 구간

아래 수치는 시범 적용안이다. 각 무기에 실제로 적합한 모션이 없으면 해당 구간의 추가기를 보류하며, 숫자를 채우려고 필수 대응기를 잠그지 않는다.

| 몬스터 레벨 | 추가 학습 요소 | 해금 원칙 |
|---|---|---|
| Lv1~9 | 무기의 기본 대응과 등급 식별 | 필수 다섯 역할을 모두 유지. Elite/Boss의 기본 Signature도 Lv1부터 제공 |
| Lv10~19 | 기본 공격의 방향·간격 변화 | 기존 기본기와 별개의 추가 기본기 해금 |
| Lv20~29 | 연계 마무리 변화 | 추가 Finisher 해금. 저레벨 마무리는 유지 |
| Lv30 이상 | 느리지만 큰 위협과 후반 패턴 | 전조가 있는 추가 강공격 해금. Boss는 레벨 조건과 HP 조건을 모두 만족할 때 후반 대표기 사용 |

Lv40·50에 모든 무기의 새 공격을 의무적으로 추가하지 않는다. 우선 Lv30까지 누적 해금한 공격과 기존 레벨 성향·스탯 성장을 사용하고, 반복 플레이 검수에서 부족한 무기에만 후속 콘텐츠를 설계한다. 일반 몬스터의 공격 수 증가와 연속 압박 횟수 증가는 별개이며, 해금 때 콤보 압박 상한을 자동으로 올리지 않는다.

### 2.2 레벨 조건의 적용 규칙

유효 후보는 `등급 Set 포함 ∩ 현재 레벨 이상 해금 ∩ 역할/거리/상태 조건 충족 ∩ GAS 활성화 가능`으로 결정한다. 여기서 레벨 해금 판정은 `currentLevel >= requiredLevel`이다. BT는 후보 중 무엇을 언제 쓸지 판단하고, 레벨별 공격 이름 목록을 따로 소유하지 않는다.

- Opener·Punish·Counter·GapCloser·Finisher마다 적어도 한 공격은 `requiredLevel=1`로 남긴다. Elite/Boss는 Signature도 하나 이상 Lv1에 남긴다.
- 추가 공격은 기존 `AbilityAttackInfo.requiredLevel`만으로 누적 해금한다. 해금 상한은 두지 않으므로 Lv10 공격은 Lv20·30에서도 후보에 남는다.
- 레벨 구간마다 Set을 복제하거나 새 레벨 레지스트리를 만들지 않는다. 현재 등급 Set 안의 Payload 조건으로 처리한다.
- 공용 공격의 레벨·역할을 바꿔야 하면 Ability와 Payload를 Fork하고 해당 등급 Set에서만 사용한다. 공용 Set과 기존 소비자의 Lv1 계약을 보존한다.
- 등급 Replace로 필수 공격을 교체할 때 replacement는 Lv1부터 사용 가능해야 한다. replacement에 Lv20을 지정하면 원본도 Set에서 사라져 Lv1~19에 역할 공백이 생기므로 금지한다. 이번 레벨 성장은 추가 공격 Add로 처리한다.
- 첫 적용에서 기존 공격을 레벨 상승 시 자동 제거하지 않는다. 동일 모션의 하위/상위 피해판을 동시에 추가해 무작위로 섞는 방식도 사용하지 않는다. 레벨은 스탯 성장과 새로운 동작 해금으로 구분한다.
- 선택 가중치는 기존값과 모든 후보의 합계를 검토해 조율한다. 목록이 늘면서 기본기·반격 기회가 사라지거나 새 공격만 반복되지 않아야 한다. 가중치는 의도별 필터 이후 상대값이므로 전체 사용 확률로 표기하지 않는다.

재스폰 레벨 보정이나 명시적 레벨 변경이 있으면 선택 컨텍스트의 현재 레벨을 사용한다. 진행 중 공격은 시작할 때 확정한 Payload로 끝내고, 새 레벨은 다음 선택부터 반영한다. 후보 캐시가 있다면 레벨 변경 때 갱신되는지도 검사한다. 저장·로드와 재스폰 후에도 같은 레벨과 등급에서 같은 해금 결과가 나와야 한다.

## 3. 데이터 구조

기존 무기별 Set은 전체 공격을 보관하는 저작 기반으로 유지한다. 기존 GUID·안정 ID와 연결은 보존하고, 적용 대상 Profile만 파생 Set에 명시적으로 연결한다.

```text
기존 AbilitySet_Humanoid_<Weapon>AttackData
  └─ Normal Set: baseSet=기존 Set, 불필요한 공격 Remove
       └─ Elite Set: baseSet=Normal, 전용기 Add + 강공격 Replace
            └─ 액터별 Boss Set: baseSet=Elite, 대표기 Add/Replace

MonsterActorProfileSO.abilitySet → 등급별 Set
ActorDefinitionSO.EffectiveAbilitySet → 실제 유효 구성
Payload.requiredLevel → 몬스터 현재 레벨로 추가 공격 해금
Payload.motionKey → 소유 액터의 abilityMotions → MotionSet
```

Normal에서 제거한 기존 공격은 Elite의 `additionalAbilities`로 다시 추가할 수 있다. Elite에서 교체된 공격을 Boss에서 다시 교체할 때는 원본 공용 Ability가 아니라 **Elite의 유효 replacement Ability**를 source로 지정한다. Remove/Replace는 base의 유효 목록에 있는 대상에만 적용한다.

공용 Set의 공격 라우터·피격 등 보조 Ability는 유지한다. Normal 목록의 수는 AI 선택 공격만 세고, 보조 Ability 수와 혼합하지 않는다. 전체 공용 Set을 수정해 다른 사용자·플레이어 무기 Set·보스에 영향을 주는 작업은 하지 않는다.

새 경로는 기존 무기 폴더 아래 `GradeVariants/`를 사용한다. 예시는 `AbilitySet_Humanoid_GreatSword_Normal`, `AbilitySet_Humanoid_GreatSword_Elite`, `GA_Humanoid_GreatSword_Elite_Punish`다. 신규 안정 ID 예시는 `Monster.Humanoid.GreatSword.Elite.Punish`이며 생성 전 전역 중복 검사를 한다.

등급별 강화는 Ability와 Payload를 안전 Fork한다. TaskGraph와 변경하지 않는 Effect는 공유한다. 같은 동작·타이밍이면 원본 MotionKey를 유지하고, 모션 타이밍·연출 이벤트 자체가 달라질 때만 MotionSet을 분리해 새 키를 매핑한다. 등급별 Variant 선택을 위한 신규 태그·런타임 분기는 도입하지 않는다.

## 4. 공격을 남기는 기준과 BT 계약

공격 수보다 실제 대응의 완결성을 우선한다. 후보에 포함됐다는 사실과 BT에서 실제로 사용 가능하다는 사실을 구분한다.

| 역할 | Normal | Elite와 Boss | 필수 확인 |
|---|---|---|---|
| Opener | 유지 | 유지 | 근접 접근 완료 뒤 시작 가능한가 |
| Finisher | 유지 | 유지 | 압박 마무리 뒤 반격 틈이 있는가 |
| Punish | 유지 | 일부 강화 가능 | 플레이어 후딜을 처벌하되 평상시 무조건 맞지 않는가 |
| Counter | 유지 | 유지 | 특히 궁수의 근거리 탈출 수단을 없애지 않는가 |
| GapCloser | 유지 | 유지 | 현재 BT의 역할·카테고리 계약과 실제 도달 거리가 일치하는가 |
| Signature | 제외 가능 | 적어도 한 개 유지 | 별도 전조와 사용 후 기회가 있는가 |

근접은 Opener/Finisher=Basic, Punish=Heavy, Counter/GapCloser/Signature=Skill의 현재 계약을 따른다. 활의 Punish는 Skill이다. `skillType`이나 파일명으로 역할을 추측하지 않는다.

무기별 기존 BT를 우선 재사용한다. Normal에 없는 Signature 분기는 `CanActivateAbility(category, role)`에서 실패해 다음 유효 행동으로 넘어가야 한다. 이는 선택적 패턴의 정상 부재다. Opener·Punish·Counter·GapCloser·Finisher의 후보 부재는 데이터 오류로 취급한다. 실제 Rules/Generated 트리와 Intent 선택 경로 모두에서 fallback을 확인하고, 후보가 없는데 쿨다운·슬롯을 소비하거나 같은 요청을 반복하면 적용을 중단한다.

기존 공용 Set의 여섯 역할 검사는 유지하고, 실제 등급별 Set의 필수 다섯 역할과 선택적 Signature 검사를 별도로 추가한다. 기존 테스트를 삭제하거나 실패한 역할을 단순히 제외해 통과시키지 않는다.

## 5. 대검 우선 적용안

아래는 현재 파일의 피해·역할에 근거한 **Play Mode 조율용 초안**이다. 모션별 예비 동작·유효 사거리 확인 전 확정하지 않는다. 피해는 HitPhase 합계이며 최종 체력 감소량이 아니다.

| 기존 공격 파일 끝부분 | 역할 | 현재 피해 | Normal | Elite | Boss |
|---|---|---:|---|---|---|
| `01_Attack_1` | 일반 Basic | 6 | Lv1 유지 | 상속 | 상속 |
| `02_Attack_2` | 일반 Basic | 6 | Lv10 추가형으로 Fork | 상속 | 상속 |
| `06_Attack_6` | Opener | 8 | 유지 | 상속 | 상속 |
| `09_Attack_9` | Finisher | 10 | 유지 | 상속 | 상속 |
| `12_HeavyAttack_2` | Punish | 6+7 | 유지 | 상속 | 상속 |
| `14_HeavyAttack_4` | Punish | 11 | 유지 | 강화형 14로 교체 | Elite형 상속 |
| `19_Skill_2` | GapCloser | 18 | 유지 | 상속 | 상속 |
| `21_Counter_Attack_1` | Counter | 11 | 유지 | 상속 | 상속 |
| `10_Attack_10` | Finisher | 12 | Lv20 추가형으로 Fork | 상속 | 상속 |
| `17_HeavyAttack_7` | 현재 역할 미지정 | 16 | Lv30 Punish 추가형으로 Fork | 상속 | 상속 |
| `20_Skill_3` | Signature | 19 | 제외 | 추가, 우선 19 유지 | 강화형 24로 교체 |
| `18_Skill_1` | 현재 역할 미지정 | 16 | 제외 | 제외 | Lv30 이상 및 HP50% 이하 후반 대표기 후보 |

등급 Set에 포함된 최대 공격 수는 Normal 10개, Elite 11개, Boss 최대 12개다. 나머지 대검 공격은 Normal에서 Remove하고 상위 Set에도 자동 복구하지 않는다. 위 표에서 레벨이 생략된 공격은 Lv1이다. 추가 기본기는 기존 공격과 실제 방향·간격이 구분되어야 하며, 단순 중복이면 대체 후보를 조사한 뒤 해금을 보류한다.

| 대검의 몬스터 레벨 | Normal 해금 수 | Elite 해금 수 | Boss 해금 수 |
|---|---:|---:|---:|
| Lv1~9 | 7 | 8 | 8 |
| Lv10~19 | 8 | 9 | 9 |
| Lv20~29 | 9 | 10 | 10 |
| Lv30 이상 | 10 | 11 | 최대 12 |

Boss의 마지막 한 개는 HP 50% 이하까지 충족해야 활성화된다. 표의 해금 수는 레벨 조건을 통과한 수이며 거리·쿨다운·HP까지 만족한 즉시 실행 후보 수와 다르다. 대검의 Lv1 Normal 일곱 개는 Basic 1개·Opener 1개·Finisher 1개·Punish 2개·GapCloser 1개·Counter 1개로 필수 역할을 모두 충족한다.

`02_Attack_2`·`10_Attack_10`은 원본을 Normal에서 Remove하고 레벨 조건을 가진 Fork를 Add한다. `17_HeavyAttack_7`은 별도 Fork에 `aiRoles=Punish`, `attackCategory=Heavy`, `requiredLevel=30`을 지정해 현재 BT가 선택할 실행 경로를 만든다. 원본의 역할·레벨을 수정하지 않는다. 이 강공격의 피해 16은 우선 유지하고 전조·사거리·후딜 검수를 먼저 한다. 원본 모션이 조건을 충족하지 못하면 추가 후보에서 제외하며, 일반 Set의 다른 Punish 두 개는 그대로 남는다.

Boss의 `Skill_1`은 기존 Ability 자체를 수정하지 않고 Boss 전용 Ability/Payload로 Fork한다. `aiRoles=Signature`, `attackCategory=Skill`, `requiredLevel=30`, HP 50% 이하 조건을 부여하는 초안이다. 레벨과 HP는 모두 충족해야 하며 OR 그룹으로 우회시키지 않는다. 현재 Signature 분기의 HP 조건과 함께 검사한다. 피해 16은 우선 유지하고 역할·후반 패턴 추가 효과부터 확인한다. 강화 Signature와 별도 쿨다운을 명시하며, 실제 발동과 대응 가능성을 검증하기 전에는 Boss Set에 연결하지 않는다. 이 후보가 부적합하면 최대 11개 구성으로 출시하고 임의 모션으로 대체하지 않는다.

빠르게 접근하는 `Skill_2`와 Counter는 이번 단계에서 강화하지 않는다. 강한 진입 피해와 높은 반격 피해가 겹쳐 플레이어가 공격할 기회를 잃는 것을 피한다. 기본 피해 6인 `Counter_Attack_2`는 메타데이터 `expectedDamage`와 차이가 있으므로 별도 확인 대상으로 두고, 자동 평균화·보정하지 않는다.

## 6. 나머지 무기군의 적용 기준

| 무기 | Normal에 남길 경험 | Elite 전용·강화 방향 | Boss 방향 |
|---|---|---|---|
| 쌍도끼 | 짧은 연속 베기, 접근, 반격 | 길게 노출되는 다단기 한 개와 명확한 마무리 강타 | 후반 대표기 추가, 연속 압박 뒤 확실한 휴지 |
| 쌍검 | 빠른 짧은 연계와 탈출 대응 | 긴 다단기는 전용기로 제한, 마지막 타격만 강화 검토 | 추적 종료·후딜이 읽히는 연계 변주 |
| 검방패 | 방어 후 응징과 기본 압박 | 느린 강타·대표기 구분 | 방어를 무조건 무력화하지 않고 방향·간격 대응 요구 |
| 활 | 원거리 기본 사격과 근접 반격 | 읽히는 연사·착탄 대표기 | 여러 발의 추적 종료 시점을 구분한 패턴 |
| 카타나 | 짧은 베기와 기본 대응 | 긴 난무를 일반 목록에서 분리 | 긴 난무는 후반 대표기로 검토 |
| 창 | 찌르기 거리 학습과 접근 대응 | 긴 전진기·강한 찌르기를 선별 | 간격 변화와 대표기 조합 |
| 채찍 | 중거리 견제와 근거리 대응 | 장시간 다단기를 선별 | 범위·회수 후딜이 명확한 대표기 |

각 무기의 구체 목록은 대검 검증 이후 실제 MotionSet과 역할 데이터를 대조해 확정한다. 동일 공격 수를 맞추려고 불필요한 공격을 추가하지 않는다. 현재 카타나 일부 공격은 HitPhase 합계가 124이므로 단타 수치만 보고 강화하지 않는다. 다단 합계·실제 중복 적중·공격 지속시간을 함께 본다.

각 무기도 Lv10 추가 기본기·Lv20 추가 마무리·Lv30 추가 강공격을 우선 틀로 사용한다. 활은 추가 사격·연사 마무리·예고된 강한 사격으로 대응시키되, 근접 Counter를 해금 대상으로 잠그지 않는다. 해당 구간의 역할에 맞는 모션이 없으면 해금 시점과 공격을 별도로 재설계하고 대검의 개수·역할을 강제로 복제하지 않는다.

## 7. 피해와 연출 조율

### 7.1 공격 Ability별 피해 차등

**공격 Ability별 피해 차이를 유지하고, 차이가 없는 공격군은 실제 모션의 위력과 위험에 맞게 차등화한다.** 레벨·등급 스탯 배율만으로 공격 간 차이를 대신하지 않는다. 현재 무기별 174개에는 이미 차이가 있으므로 일괄 재계산하지 않는다.

| 현재 공격군 | Ability 수 | 서로 다른 HitPhase 합계 수 | 기본 피해 합계 범위 |
|---|---:|---:|---:|
| 활 | 12 | 9 | 9~31 |
| 쌍도끼 | 19 | 11 | 8~31 |
| 쌍검 | 24 | 13 | 8~38 |
| 대검 | 22 | 12 | 6~19 |
| 카타나 | 33 | 18 | 7~124 |
| 창 | 23 | 14 | 8~25 |
| 검방패 | 25 | 16 | 8~26 |
| 채찍 | 16 | 12 | 7~43 |
| 무기 미분류 Humanoid | 12 | 1 | 전부 10 |

이 집계는 Payload의 HitPhase 피해 합계다. 각 Phase가 한 번씩 맞았다는 기준값이며, 실제 적중 횟수·공격 지속시간·피격자의 방어·공격자의 AttackPower를 반영한 실전 피해나 DPS는 아니다. 공격별 실제 판정에서 반복 적중이 가능한지도 함께 확인한다.

대검은 `Attack_1=6`, `Attack_9=10`, `HeavyAttack_2=6+7`, `Skill_2=18`, `Skill_3=19`로 이미 차등화되어 있다. 반면 무기 미분류 Humanoid의 기본 공격 9개와 스킬 3개는 모두 10이므로, 실제 소비 액터와 모션을 확인한 뒤 차등화할 별도 대상이다. 이 Set이 현재 전투에 사용되는지는 이번 피해 집계만으로 단정하지 않는다.

차등화 기준은 다음과 같다.

- 빠른 견제·추적이 강한 접근기·안전한 반격은 상대적으로 낮은 피해 예산을 준다. 느리고 피할 수 있으며 헛쳤을 때 빈틈이 큰 공격에는 더 큰 피해 예산을 준다.
- 다단기는 각 타격과 전체 적중 합계를 함께 정한다. 타수가 많다는 이유로 단타 피해를 그대로 반복해 총피해를 폭증시키지 않는다. 마무리의 피해·리액션을 구분한다.
- 레벨 해금은 새 동작을 추가하는 조건이다. 높은 레벨에 해금된 공격이라고 기존 모든 공격보다 반드시 강하게 만들지 않는다. 더 긴 사거리·범위·다른 타이밍도 공격의 가치다.
- 같은 피해가 반복된다는 이유만으로 모든 Ability에 서로 다른 숫자를 강제로 부여하지 않는다. 모션·사거리·경직·공격 시간이 비슷하면 같은 피해를 허용하되, 공격군 전체가 같은 피해로 평준화되거나 느린 강타가 빠른 견제와 같은 보상만 갖는 구성은 재검토한다.
- 무기 미분류 12개와 같은 균일 피해군의 변경 전, Ability→실제 소유 액터→MotionSet→HitPhase별 타이밍을 조사하고 공격별 기존 피해·제안 피해·근거를 작성한다. 모션 근거 없이 10·11·12처럼 번호순으로 수치를 발명하지 않는다. 공유 데이터는 역참조 조사와 안전 Fork 원칙을 따른다.

### 7.2 등급별 강화와 전투 피드백

대검의 11→14, 19→24는 선택한 공격의 제안값이다. 모든 스킬에 적용하는 등급 배율이 아니다. 실제 저장 시 각 Payload HitPhase에 확정 수치를 기록하고, 외부 배율을 다시 곱하는 런타임 시스템은 추가하지 않는다. `balance.expectedDamage`도 같은 변경에서 갱신한다.

`DamageResolver`는 공격자의 AttackPower와 피격자의 방어·속성 등을 반영한다. 등급 스탯 강화에 Payload 강화가 중첩되므로, 동일 레벨·장비·방어 조건에서 원본/강화형의 최종 피해와 플레이어 최대 HP 대비 비율을 함께 측정한다. 스탯 생성기용 `attackDamageMultiplier`를 별도 런타임 배율로 오해해 중복 적용하지 않는다.

| 항목 | 저작·검수 기준 |
|---|---|
| 강화 식별 | 같은 동작이라도 피해가 더 크면 판정 전에 차이를 알 수 있는 기존 위험 링·무기 FX·SFX 조합을 지정 |
| 예비 동작 | 첫 판정 전 인지 가능한 준비 구간 확보. 초안 목표는 강타 0.6초 이상, 큰 대표기 0.8초 이상이며 클립 조사 뒤 확정 |
| 기존 모션 한계 | 준비 시간이 부족하면 그 모션은 피해 강화 대상에서 제외. 재생 속도를 임의로 늦춰 해결하지 않음 |
| 방어 판정 | 피해 강화와 함께 가드 불가·패링 불가를 자동 부여하지 않음 |
| 회전·추적 | 피할 방향이 정해진 뒤 늦게 재추적하지 않는지 실제 모션·워프 확인 |
| 후딜 | 큰 공격을 회피한 뒤 기본 반격이 들어갈 창을 확보. levelTendency의 템포 보정과 실제 적용 결과를 함께 검사 |
| 피드백 | 히트스톱·카메라·타격음을 무게에 맞추되 다인 전투에서 플레이어 조작이 연속으로 잠기지 않도록 검사 |
| 취소·중단 | 피격·브레이크·사망·상태 이탈 시 히트박스·위험 링·추적 FX가 남지 않아야 함 |

현재 Circle 지원 범위 안에서 실제 타격 범위와 맞는 텔레그래프를 사용한다. 길쭉한 찌르기를 큰 원으로 표시해 오해를 만들지 않는다. `useMotionEventTelegraph`를 켜려면 실제 Motion에 해당 이벤트가 있어야 한다. 공유 MotionSet에 이벤트를 넣으면 다른 소비자도 영향을 받으므로 역참조 검사 후 필요할 때만 분리한다.

## 8. 프로필 연결과 재생성 경로

`MonsterActorProfileSO.abilitySet`을 기본 연결 지점으로 사용한다. Definition의 다른 Set이 Profile에서 파생되지 않았다면 Profile이 우선하므로, Definition 필드만 바꿔 완료 처리하지 않는다. 같은 Profile을 서로 다른 등급·목록이 필요한 개체가 공유하면 기존 복제 경로로 Profile을 분리한다.

P09 빌더는 원본 Profile을 복제하고 `definition.abilitySet=entry.Source.EffectiveAbilitySet`을 넣으며, 검증도 원본 Set과의 일치를 요구한다. 따라서 생성된 Profile만 수동 수정하면 재생성 때 되돌아갈 수 있다. 구현 시 기존 제작 데이터에 역할별 Normal/Elite Set 참조를 명시하고, 이미 결정된 tier grade로 선택한 **동일 결과를 Profile·Definition·검증에 사용**한다. 액터명·갑옷명으로 분기하지 않는다. Boss는 액터별 Profile에서 명시적으로 연결한다. 신규 레지스트리나 Manager는 필요 없다.

P09의 삭제·교체 빌더는 완전한 transaction이 아니다. 이 경로 변경은 별도 선행 설계로 스테이징·기존 에셋 백업과 GUID 보존·중간 실패 복구·실패 주입 검증을 구체화한 뒤 진행한다. 이 설계만으로 전체 P09 재빌드를 실행하지 않는다. 첫 적용은 기존 에셋을 재생성하지 않는 참조 교체를 Preview/Undo로 수행하고, 재생성 경로 정합성이 해결되기 전 완료로 간주하지 않는다.

대검 `GreatSword_002`와 Armor08~12의 검방패 참조는 별도 선행 교정 대상이다. 현재 실제 프리팹의 무기·그립·MotionSet을 확인한 뒤 Profile/Definition을 함께 정리한다. 등급 분리 작업에서 잘못된 Set을 기반으로 상속하지 않는다. 필드 조우 보고서의 38마리 수치는 적용 직전에 다시 집계한다.

## 9. 구현 순서와 산출물

1. **기준 확정:** 현재 Profile→EffectiveAbilitySet→Payload→MotionKey→실제 프리팹 MotionSet을 Unity에서 읽는다. 공유 참조, 플레이어 소비자, 기본/강화 피해, 모션 판정 시각을 기록한다. 대검 연결 이상을 먼저 해결한다.
2. **대검 후보 확정:** 위 최대 10/11/12개 목록의 사거리·필수 역할·텔레그래프·후딜과 Lv1/10/20/30 해금을 검사한다. 부적합 모션은 목록 또는 강화 대상에서 제외하고 근거를 남긴다.
3. **데이터 제작:** Ability Editor/Dashboard 안전 Fork로 강화 Ability·Payload 2개, 레벨 해금용 3개와 필요한 Boss 후반 Ability 1개를 만든다. 후보를 모두 채택하면 Fork는 6개다. Normal/Elite/액터별 Boss Set을 구성하고 기존 공용 데이터와 ID는 보존한다.
4. **연결:** Preview에서 개체별 이전/이후 Set·등급·현재 레벨에서의 해금 공격·잠긴 공격과 요구 레벨·역참조를 제시하고 단일 Undo 단위로 적용한다. 오류 시 전체 롤백하고 저장한다. P09 재생성 경로는 8절의 별도 설계를 먼저 완료한다.
5. **검증:** 아래 정적·EditMode·Play Mode 기준을 통과한 대검만 시범 적용한다. 전체 무기군을 한 번에 바꾸지 않는다.
6. **확장:** 쌍도끼·쌍검·검방패·활 순으로 같은 기준을 적용한다. 카타나·창·채찍은 실제 콘텐츠 확인 후 별도 목록을 확정한다.

## 10. 검증과 완료 조건

`TEST_VALIDATION_GUIDE.md`를 따른다. 기본 범위는 관련 EditMode와 Editor Play Mode다. 데이터 수치·참조 변경만으로 전체 Player Build를 반복하지 않는다. 직렬화·모듈 경계·빌드 경로 변경이나 주요 통합 기준에 해당하면 Player 검증을 추가한다.

| 범위 | 완료 조건 |
|---|---|
| Set 합성 | base 순환·중복 override 없음. 원본과 replacement가 동시에 후보에 남지 않음 |
| 등급 격리 | 실제 Normal Set에서 Elite/Boss 전용 공격 0개. Lv1·3·5·9·10·19·20·29·30·40·50에서 유지 |
| 역할 | 위 모든 레벨에서 등급별 필수 다섯 역할에 유효 거리와 선택 후보 존재. Elite/Boss는 Signature도 존재 |
| 해금 경계 | 각 추가 공격은 Lv9/10·19/20·29/30 직전/직후에 후보 제외/포함. Lv40·50에도 유지 |
| 낮은 레벨 교체 | Elite/Boss의 Replace가 Lv1부터 해석되고 원본 제거 때문에 역할이 비지 않음 |
| Boss 복합 조건 | Lv29는 HP가 낮아도 후반기 불가. Lv30은 HP50% 초과 시 불가, HP50% 이하 시 레벨/HP 조건 통과 |
| 레벨 변경 | 레벨 변경·재스폰·저장/로드 뒤 해금 결과 일치. 진행 중 공격 Payload는 유지하고 다음 선택에 변경 반영 |
| 선택적 부재 | Normal의 Signature 요청 실패 뒤 기본 공격/접근으로 진행. 슬롯·쿨다운 누수와 반복 정지 없음 |
| 궁수 | 교전 거리 8m와 personalSpace 안쪽에서 공격·반격 또는 유효 탈출 행동 보장 |
| Motion | `MonsterAbilitySetIntegrationTests`에서 각 소유 액터의 MotionKey·HitPhase·히트박스 매핑 검사. 누락을 건너뛰지 않음 |
| 피해 | 고정 스탯 조건에서 원본/강화형 계산 비교. 선택한 공격만 증가, 실제 최종 피해와 다단 합계를 기록 |
| 공격별 차등 | 공격별 단타·다단 합계·실제 적중 횟수·지속시간을 비교. 균일 피해군은 실제 소비·모션을 조사하고 차등안과 예외 근거를 남긴 뒤 검증 |
| 저장·재생성 | GUID·actorId 보존. 새 등급 Set 연결이 P09 재실행으로 되돌아가지 않음. 실패 중간 지점 복구 검증 |
| 전수 검사 | `AbilityDataValidator.ValidateAll()` 실행. 기존 오류와 신규 오류를 구분하고 자동 Fix는 사용하지 않음 |
| 공유 데이터 | 기존 공용 Ability/Payload/MotionSet과 적용 밖 보스·플레이어의 수치/연결 diff 없음 |

Play Mode는 동일 레벨 Normal 대 Elite, 같은 등급의 Lv9/10·19/20·29/30을 각각 비교한다. 해금 경계 직전에는 추가기가 없고 직후에는 실제 BT 경로로 발동하는지 확인한다. 1대1에서 접근·회피·가드·패링·공격 헛치기를 직접 유발하고, 근접 2+궁수 1 혼성에서 공격 슬롯 양보·위험 링·연속 히트스톱을 확인한다. Boss는 레벨 29/30과 HP 50% 전후 조합, 공격 도중 브레이크·사망을 재현한다.

자동 표본에서는 개체·선택 Ability ID·탈락 이유·역할·최종 피해·전조 시작/첫 판정·마지막 판정/상태 종료를 기록한다. 공격 기회와 쿨다운 해제 조건을 충족시켜 전용기의 실제 발동을 확인하고, 랜덤 표본에서 한 번 안 나왔다는 이유만으로 정상/오류를 판정하지 않는다. 정지한 무적 플레이어 표본만으로 손맛·난이도 검수를 대체하지 않는다.

구현 완료는 데이터 정합성 통과와 실제 플레이에서 등급 차이·예측 가능성·반격 기회가 함께 확인된 상태다. 데이터 구현과 자동 검사 결과는 12절에 기록하고, 사람의 플레이 검수와 구분한다.

## 11. 근거 파일

- [기존 Humanoid 역할 설계](HUMANOID_MONSTER_GAS_BT_DESIGN.md)
- [필드 전투 개선 실행 기록](FIELD_COMBAT_ENCOUNTER_AI_IMPROVEMENT_PLAN.md)
- [전투 시스템 작성 지침](../guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md)
- [검증 지침](../guide/TEST_VALIDATION_GUIDE.md)
- `Assets/02.Scripts/Data/Ability/AbilitySetSO.cs`
- `Assets/02.Scripts/Data/Actor/ActorDefinitionSO.cs`
- `Assets/02.Scripts/Data/Combat/CombatData.cs`
- `Assets/02.Scripts/Data/Combat/EnemySkillCondition.cs`
- `Assets/02.Scripts/Data/Enum/AttackInfo.cs`
- `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs`
- `Assets/02.Scripts/GameActor/Combat/Resolution/DamageResolver.cs`
- `Assets/02.Scripts/Editor/P09Builder/P09ProgressionBuilder.cs`
- `Assets/Tests/EditMode/Ability/HumanoidArchetypeSelectionTests.cs`
- `Assets/Tests/EditMode/AI/EnemyAttackRangePolicyTests.cs`

## 12. 현재 작업본 구현 결과

### 후속 작업의 최신 판정

아래 최초 작업 기록에 이어 [후속 구현·검증 결과](HUMANOID_GRADE_FOLLOWUP_RESULT.md)를 반영했다. 기존 21개 Set과 GUID를 유지하면서 Fork 3쌍을 추가했고, 실제 등급 Set 소비자는 54개다. 카타나 Normal Lv30 Heavy6(17), 채찍 Normal Lv30 Heavy3(5+5+6+6+6), 쌍검 Boss Lv30 AND HP≤50% 이연격(8+9)을 연결했다. 카타나·채찍 기존 Punish도 새 공격과 같은 3초 쿨다운 그룹을 사용한다.

Sera는 같은 Avatar의 실제 활성 대검 장비·신체 판정과 GreatSword Boss Set을, Nenmir는 기존 Bow_004 및 Bow Boss Set을 사용한다. 별도 아린 분신 보스의 고유 Set은 유지한다. 창은 무소비가 아니라 **Reien Definition의 명시 Set이 Profile 때문에 가려지고 프리팹 무기/판정도 없는 상태**임을 확인했다. 창병 복구 대상 선택 질문의 답변과 전용 무기 이식이 남았다.

후속 전체 Ability EditMode 287/288 통과(변경 전부터 있던 TautString 1건 실패), Editor Play Mode 6/6 통과, Validator Error7/Warning599/Info648이다. P09 Validate는 기존 Armor06 Bow HP bake 불일치로 중단한다. 실제 입력·전조 가독성·반격 기회·혼성전 피로도는 사용자 테스트 대기다. 아래 수치·시험 개수는 **최초 작업 당시 기록**이며 현재 통과 결과와 혼합하지 않는다.

### 실제 연결과 저장

- `HumanoidGradeVariation.json`이 원본 공격·등급·레벨·역할·피해·적용 Definition 경로를 명시한다. `GradeVariation/<Weapon>/`에 Normal→Elite→Boss 파생 Set 21개와 Ability/Payload Fork 41쌍을 Unity Editor API로 생성했다. 레벨별 Set, 런타임 Manager/레지스트리는 추가하지 않았다.
- Profile/Definition 51쌍을 연결했다. Profile을 공유하는 `StoryLili`까지 실제 유효 소비자는 52개다. 현존 Boss Lili/Hwarin/Yura/MyoRyeong/Raon/Lian은 각 무기의 Boss Set을 사용한다. Bow Boss Set은 저작했으나 잘못된 원본 프리팹 문제 때문에 실소유자에게 연결하지 않았다.
- `GreatSword_002`와 P09 Armor08~12의 실제 무기·프리팹 MotionSet은 대검이었다. Profile/Definition의 검방패 Set 참조만 대검 등급 Set으로 교체했다. Armor06~07도 Normal Set을 사용한다. 프리팹·모션·actorId·GUID·필드 배치는 바꾸지 않았다.
- P09 제작표의 역할별 Normal/Elite Set GUID를 명시하고 생성/검증이 같은 `ResolveAbilitySet`을 사용한다. 생성 시 Profile과 Definition에 같은 Set을 넣는다. 전체 P09 Build는 실행하지 않았다.
- Armor08~12 대검의 저장 Attribute 전체가 교정 전 검방패 공식과 일치해, 해당 5개의 AttributeProfile/Poise/BreakGauge만 기존 API로 GUID를 유지하며 갱신했다. 첫 개체 저장 후 실패 주입의 바이트 복구도 통과했다. 기존 공식과 다른 원본 GS002·Armor06/07의 조율 스탯은 보존했다.
- 신규 저장 후/참조 교체 후의 실패 주입에서 원본 에셋과 meta 바이트 복구, 신규 에셋 제거를 확인했다. [저장·복구 경계](HUMANOID_GRADE_APPLY_SAFETY.md)와 `Logs/HumanoidGrade/Backup/`을 참고한다. 전체 삭제·교체 빌더의 transaction 검증을 대신하지 않는다.
- 최초 대검 저장에서는 미저장 객체 참조가 비는 문제가 신규 테스트에 잡혔다. Payload→Ability→Set 저장 순서와 런타임 인덱스 재구축으로 수정했고, 신규 대검 에셋의 기존 GUID를 유지하여 참조를 복구한 뒤 19개 관련 검사를 통과했다.

### 공격 목록과 피해 결정

공격별 Phase 피해·총합·첫/마지막 판정·지속시간·후딜은 [실제 공격 데이터 전표](HUMANOID_GRADE_ATTACK_DATA.md)에 전수 기록했다. CSV와 소유 프리팹 연결표는 `Logs/HumanoidGrade/attacks.after.csv`, `owners.after.txt`다.

| 무기 | Normal Lv1/10/20/30 | Elite | Boss | 초안과 다른 결정 |
|---|---|---|---|---|
| 대검 | 7/8/9/10 | 8/9/10/11 | 8/9/10/12 | Heavy4는 첫 판정 0.344초라 11 유지. Boss Signature만 19→24 |
| 쌍도끼 | 7/8/9/10 | 8/9/10/11 | 8/9/10/12 | Signature 후딜 0.180초라 피해 31 유지. Boss 후반에 별도 2연타 9+12 추가 |
| 쌍검 | 7/8/9/10 | 8/9/10/11 | 8/9/10/11 | Boss Signature 마지막 Phase만 8→10, 총합 38→40. 전조 0.762초, 다른 모션 없는 추가 후반기는 보류 |
| 검방패 | 7/8/9/10 | 8/9/10/11 | 8/9/10/12 | Boss Signature 26→30. 후반기는 별도 Skill3 23 |
| 활 | 7/8/9/10 | 8/9/10/11 | 8/9/10/12 | Boss Signature 22→25. Lv30 4연사의 첫 발은 0.545초, 이후 0.836/1.142/2.063초. 총합 31 유지, 두 Counter는 Lv1 유지 |
| 카타나 | 5/6/7/7 | 6/7/8/8 | 6/7/8/9 | 실제 Raon의 매핑을 검증. Signature는 Skill2 11+14, 후반은 Skill3 7+7+8+8+8+9. Skill4/5의 고정 피해 투사체와 124 피해 난무는 제외 |
| 채찍 | 5/6/7/7 | 6/7/8/8 | 6/7/9/9 | 실제 Lian의 채찍 판정을 검증. Boss Lv20에 별도 마무리 9 추가. 충분한 긴 전조를 가진 Lv30 강공격 모션이 없어 억지로 해금하지 않음 |

Boss 후반 추가기는 `requiredLevel=30`과 AND HP≤50%를 함께 사용한다. 표의 Boss 수는 레벨 해금 수이며 HP·거리·쿨다운을 통과한 즉시 후보 수가 아니다. Replace는 Lv1 Signature에만 사용했다. 공용 Ability/Payload/MotionSet은 변경하지 않았고 같은 MotionKey를 공유한다. 신규 Fork의 `expectedDamage`는 Phase 합계와 일치한다.

새 Lv30 근접 강공격 4개는 기존 Punish와 `Humanoid.<Weapon>.Heavy` 쿨다운 6초를 공유한다. 추가 공격 때문에 공용 응징 쿨다운을 우회해 강공격 압박이 이어지지 않도록 했다. Boss 후반기는 명시적인 독립 14초 쿨다운을 사용한다.

무기 미분류 Humanoid 12개는 10 피해 그대로 두었다. 역참조 검사에서 이 12개를 참조하는 것은 미사용 `AbilitySet_humanoid`뿐이며 그 Set의 소비 에셋은 발견되지 않았다(`ExternalAssets` 제외 프로젝트 YAML 에셋·프리팹·씬 검사 및 실제 Definition 조사). 실제 전투에 연결되지 않은 혼합 무기 묶음에 임의의 차등 수치를 주지 않았다. 대검 Counter2의 공용 `expectedDamage=14` 대 Phase=6 불일치는 조사 기록으로 남기고 이번 적용 목록에서 제외했다.

### 최종 피해와 반복 적중의 경계

`DamageExecution.Calculate`는 `Phase 피해 × AttackPower × (1-방어율) × 피격/속성/치명 배율`을 한 번 계산한다. 등급·레벨은 이미 AttackPower 생성에 반영되며 별도 등급 피해 배율을 추가하지 않았다. `attackDamageMultiplier`를 런타임에서 다시 곱하지 않았다.

실제 저장 AttributeProfile의 AttackPower를 읽어 방어율 20%, 나머지 배율 1 조건의 모든 해금 공격을 `Logs/HumanoidGrade/final-damage.csv`에 계산했다. Lili Lv10 Boss의 AttackPower 약 1.842에서 Signature는 동일 스탯 원본 19일 때 약 27.991, 변경 24일 때 약 35.357이다. 교정 후 Armor08 Lv20 Elite 대검의 AttackPower 1.884에서 Signature 19는 약 28.637이다. 이는 엔진 계산식의 고정 조건 비교이며 실제 플레이어의 장비/도감/속성/방어·현재 HP를 반영한 실측 체력 감소는 아니다.

판정 전표는 각 Collision 윈도우/SingleShotProjectile 이벤트와 Phase 인덱스를 기록한다. 근접은 윈도우별 `_hitTargets` 중복 방지, 다음 윈도우의 초기화가 있으므로 전체 Phase 합계와 반복 판정 이벤트를 함께 봐야 한다. 실제 회피·가드·이동을 포함한 적중 횟수·DPS·플레이어 HP 대비 비율은 사용자 플레이 검수 대기다. 전표의 투사체 endTime은 발사 이벤트 표시 구간 끝이므로 이를 투사체 비행시간으로 읽지 않는다.

### 검증 현황과 남은 항목

- Unity 6000.3.21f1 컴파일 오류 0. Assembly-CSharp-Editor 보조 CLI 컴파일 오류 0(기존 참조/obsolete 경고 있음).
- 변경 전 선택/몬스터 연결 검사 13/13 통과. 대검 저장 참조 오류 수정 뒤 관련 검사 19/19 통과.
- 최종 Ability EditMode 280개 중 279 통과. 유일한 실패는 `CharacterSkillProgressionServiceTests`의 SeolA/Nenmir.Attack.TautString 연결이며 이전 필드 개선 기록에도 있던 변경 범위 밖 문제다. 신규 등급 검사·기존 Humanoid 검사·MonsterAbilitySetIntegrationTests는 통과했다.
- 마지막 라우터/공용 쿨다운 검사 추가 및 네임스페이스 수정 뒤 관련 EditMode 21/21 통과. 공용 쿨다운 보완 뒤 실제 대검 프리팹 Play Mode 검사도 다시 통과했다.
- AbilityDataValidator Error는 변경 전/후 동일 7건(플레이어 기본 Ability/Ultimate HitPhase 2, 패시브 DB 5). 자동 Fix는 하지 않았다. 신규 Fork의 아이콘은 기존 리소스를 연결했고, 이름 현지화 키 부재 Info는 공용 원본의 표시 정보 부재를 복제한 항목이다.
- 마지막 전수 검증은 Error 7 / Warning 599 / Info 645. 기준선 대비 신규 Error 0, 신규 Warning 0이다. 원래 아이콘이 없는 공용 원본은 바꾸지 않았다.
- P09 전체 Validate는 기존 Armor06 Bow `Vital.MaxHealth` bake 불일치에서 중단한다. 공격 참조는 등급 검사로 별도 확인했으며 HP를 임의로 재생성하지 않았다.
- Editor Play Mode 및 사람의 체감 검수 결과는 아래 후속 기록과 [사용자 테스트 명세](HUMANOID_GRADE_PLAYTEST.md)에 구분한다. 혼성 조우·실제 회피/가드/패링·보스 후반 반격창을 정지 표본으로 대체하지 않는다.
- 기존 Ability 수직 슬라이스 4개 통과. 신규 실제 대검 프리팹 Play Mode 검사도 통과하여 일반/Elite/Boss의 런타임 레벨 경계, Boss HP51%/49%, 라우터 실행, 실제 상태 OnExit 후 실행 핸들·Collision·워프 정리를 확인했다. 초기화 중 테스트 네임스페이스 오류, 고정 1m 가정, ASC 종료와 실제 상태 이탈을 혼동한 fixture를 수정한 후 통과한 결과다. 브레이크/사망 개별 경로와 혼성 조우 체감·링의 실제 화면 품질은 사용자 검수 대기다.
- `MonsterSera`/`MonsterNenmir`는 무기와 실제 프리팹 MotionSet·HitBox가 맞지 않아 적용 제외. 창은 대응 Set의 실제 소비 Definition이 없어 미적용. 채찍/카타나의 일반 Lv30 추가 강공격과 쌍검의 별도 Boss 후반기는 콘텐츠 후속 작업이다.
- 최종 diff에서 공용 Humanoid Ability/Payload/MotionSet 및 프리팹 변경 0. 작업 전 사용자 변경 32개 diff는 동일하고, 기존 Definition/Profile은 의도한 abilitySet 교체 외 변경 0(자동 추가 기본 필드 복원), 기존 에셋 meta 117개의 원본 바이트가 일치했다. 기존 씬으로 복귀했고 이번 Test Runner 임시 씬은 제거됐다. 사용자 기존 InitTestScene은 보존했다. 커밋·Player Build는 하지 않았다.
