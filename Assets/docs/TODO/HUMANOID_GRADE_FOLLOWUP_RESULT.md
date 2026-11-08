# Humanoid 등급 공격 후속 작업 결과

이 문서는 2026-11-06의 기존 등급 후속 작업 기록이다. 이후 사용자는 신규 일반 창병 제작을 선택했다. Reien은 보존하고 `Enemy_Field_Spear_001`을 독립 제작했다. 현재 연결·실물 창 판정·공격 8종·검증은 [창병 제작 기록](HUMANOID_SPEAR_SOLDIER_RESULT.md)을 따른다. 아래의 창병 미결정 기록은 이 결정 전 조사 이력이다. 실제 조작과 전투 품질은 계속 사용자 테스트 대기다.

## 실제 연결

| Definition 파일 / 안정 actorId | Profile | 실제 프리팹 | 유효 Set / MotionSet |
|---|---|---|---|
| Humanoid/MonsterSera / MonsterSera | MonsterProfile_MonsterSera | Humanoid/MonsterActor_Sera_GreatSword | GreatSword_Boss / Humanoid_GreatSwordAnimationSet |
| Humanoid/MonsterSeolA / MonsterNenmir | MonsterProfile_MonsterSeolA | Humanoid/MonsterActor_SeolA_Bow | Bow_Boss / Humanoid_BowAnimationSet |
| Humanoid/MonsterRaon / MonsterBokusei | MonsterProfile_MonsterRaon | Humanoid/MonsterActor_Raon_Katana | Katana_Boss / Humanoid_KatanaAnimationSet |
| Humanoid/MonsterLian / MonsterLian | MonsterProfile_MonsterLian | Humanoid/MonsterActor_Lian_Whip | Whip_Boss / MonsterLian_AnimationSet |
| Humanoid/MonsterYura / MonsterHichi | MonsterProfile_MonsterYura | Humanoid/MonsterActor_Yura_Assasin | DualBlade_Boss / Humanoid_DualBladeAnimationSet |

Definition은 `Assets/10.Datas/Actor/DataBase/`, Profile은 `Assets/10.Datas/Actor/Profiles/Monster/`, 프리팹은 `Assets/03.Prefabs/Actor/Monster/`, 등급 Set은 `Assets/10.Datas/Ability/Actor/GradeVariation/<Weapon>/AbilitySet_<Weapon>_<Grade>.asset` 아래다. 현재 Unity 로드 결과로 갱신한 전체 소유자 54개와 공격 전표 219행은 `Logs/HumanoidGrade/owners.after.txt`, `attacks.after.csv`에 있다.

Sera와 Nenmir는 모두 쌍도끼 `MonsterHwarin_AnimationSet`을 가리키고 부착 HitBox가 없었다. Sera의 이전 표시 무기는 `Spear1H_Epic`이었다. 파일명만 근거로 바꾸지 않고, `PlayerCharacterDefinition_Sera.defaultWeaponType=GreatSword` 및 동일 `Humanoid_FAvatar` 플레이어 모델의 실제 활성 장비 `Weapon/GreatSword/sword1_2/sword_2hand`를 확인한 뒤 그 무기 부착·무기 판정과 신체 판정을 옮겼다. GreatSword_005 등 비활성 메시 목록은 실제 활성 장비의 근거로 사용하지 않았다. Heavy4의 LeftFoot 판정까지 유지한다. 이전 Weapon/Weapon은 fileID 보존을 위해 비활성화했다.

Nenmir는 기존 Bow_004와 왼손 장착을 유지하고 실제 Bow MotionSet·MainWeapon 판정을 연결했다. 따라서 이미 저작된 Bow Boss Set에 실소유자가 생겼다. 두 Profile 모두 공유 소비 Definition이 하나임을 적용 전에 확인했다. Definition/Profile 및 프리팹 EnemyCombat의 Set을 함께 연결했다.

`Boss/MonsterBossAlternateSelfNenmir`의 actorId는 `BossAlternateSelfNenmir`이며 Humanoid Nenmir와 다른 Profile·프리팹·고유 `AbilitySet_Boss_Raon`을 쓴다. 현재 프리팹은 아린 분신의 카타나다. 이 스토리 경로는 수정하지 않았고 EffectiveAbilitySet이 기존 고유 Set을 계속 반환하는 회귀 검사를 추가했다. NPC·플레이어·스토리 진행·도감 ID·씬 배치는 수정하지 않았다.

## 추가 공격

신규 Set 없이 기존 21개 등급 Set에 Fork 3쌍을 추가했다. 신규 Fork 전체는 44쌍이다. 카타나·채찍 Normal에 Add한 공격은 Elite/Boss에 상속되며, 쌍검 후반기는 Boss에만 Add한다. 기존 Lv1 역할과 대표기, 궁수 근접 대응은 유지한다.

| 공격 / 기존 MotionKey | 모션 근거 | 조건·역할 | Phase / 합계 | 타임라인과 대응 | 쿨다운 |
|---|---|---|---|---|---|
| Katana Heavy6 / Humanoid.KatanaAttackData.HeavyAttack.6.16 | Frank Katana S2_Combo02_03, Katana_Combo_Attack_5_3 | Lv≥30, Heavy/Punish | 17 / 17 | 전체 1.767초, 판정 0.519~0.636, 이후 1.131초. 전조 뒤 옆으로 피하고 회수 중 반격 | 기존 Heavy1과 Humanoid.Katana.Heavy 3초 공유 |
| Whip Heavy3 / Humanoid.WhipAttackData.HeavyAttack.3.09 | Frank Whip Combo03_All, WhipCharacter_HeavyAttack_3 | Lv≥30, Heavy/Punish | 5+5+6+6+6 / 28 | 전체 3.333초. 판정 시작 0.396/1.198/1.497/2.033/2.339, 마지막 끝 2.456, 이후 0.877초. 첫 타를 피한 뒤 범위 밖에서 연계 종료를 보고 접근 | 기존 Heavy2와 Humanoid.Whip.Heavy 3초 공유 |
| DualBlade Heavy2 / Humanoid.DualBladeAttackData.HeavyAttack.2.12 | Frank Dual Combo04_2, HeavyAttack_2 | Lv≥30 AND HP≤50%, Skill/Signature | 8+9 / 17 | 전체 1.733초, 판정 0.350~0.500와 0.634~0.870, 이후 0.863초. 첫 회피 뒤 두 번째 베기까지 확인하고 반격 | 후반기 고유 그룹 14초 |

쌍검은 외부 PowerAttack·InfinitySlash 및 기존 전체 공격을 조사했으나, 추가 판정 저작 없이 실제 연결 가능한 별도 이연격을 선택했다. 기존 Rolling 대표기를 중복 강화판으로 Add하지 않았다. 카타나 Skill4/5의 레거시 고정 피해 투사체와 124 피해 난무는 추가하지 않았다. 세 공격 모두 기존 MotionKey·클립·속도·Collision 타이밍을 유지하며 MotionSet을 복제하지 않았다.

기존 EnemyAttackState는 첫 판정 0.15초 전까지 조준하고 그 뒤 다시 조준 창을 열지 않는다. 따라서 새 무워프 공격의 추적 종료 기준은 카타나 약 0.369초, 채찍 약 0.246초, 쌍검 약 0.200초다. 기존 Authored Rotation·Warp 유무와 현재 실제 타임라인을 함께 확인해야 하며, 숫자는 손실 없는 회피 보장이 아니다. 위험 링 시간은 다음 Collision 시점에서 계산하고 수동으로 길게 늘리지 않았다. 특히 채찍 0.396초와 쌍검 0.350초 전조가 실제 화면에서 충분히 읽히는지는 사용자 검수 대기다.

다단 판정은 채찍 5개 윈도우/5 Phase, 쌍검 2개 윈도우/2 Phase다. 윈도우 내부 대상 중복 방지와 다음 윈도우 초기화를 기준으로 합계를 정했다. 실제 재진입·이동·가드·회피를 포함한 반복 적중 횟수, 상태 종료까지의 실시간 지속과 체감 압박은 미측정이다. 타임라인 후딜을 실제 반격 성공으로 간주하지 않는다.

## 최종 피해 비교

Unity에서 현재 `MonsterScaling_Default`와 `MonsterStatCalculator.Calculate`, `DamageExecution.Calculate`를 호출했다. Lv30, 난이도1, 방어율20%, 피격·속성·치명 배율1의 비교다. 공용 원본과 신규 Fork의 기본 피해는 같고 역할·해금·Set 포함 범위가 바뀐다.

| 공격 | Normal AP1.87 | Elite AP2.244 | Boss AP2.7115 |
|---|---:|---:|---:|
| 카타나 17 | 25.4320 | 30.5184 | 36.8764 |
| 채찍 5+5+6+6+6 | 41.8880 | 50.2656 | 60.7376 |
| 쌍검 후반 8+9 | 미포함 | 미포함 | 17.3536+19.5228 = 36.8764 |

등급 보정은 AttackPower에 이미 들어 있다. `attackDamageMultiplier`를 다시 곱하지 않았다. 현재 저장된 Raon/Lian/Yura의 Boss Lv10 AP는 모두 1.8415이며, 저장 스탯은 변경하지 않았다. 위 표는 표준 Lv30 성장 비교이고 플레이어 장비·버프·실제 HP 감소량은 아니다. Phase별 계산은 `Logs/HumanoidGrade/Remaining/final-damage-followup.csv`, 현재 소유자의 저장 AP 기준 계산은 `Logs/HumanoidGrade/final-damage.csv`다.

## 창 조사와 남은 결정

`Humanoid_SpeatAttackData`는 오타를 포함한 기존 안정 키를 보존했다. 원본 Set은 `MonsterReien` Definition에 명시돼 있으나 Profile이 `AbilitySet_PlayerDefaultAttackData`를 가리켜 실제 유효 Set이 아니다. 실제 `MonsterActor_Reien_Spear`에는 창 무기와 CombatHitbox가 없고 Hwarin MotionSet을 참조한다. 따라서 단순 등급 Set 교체는 실사용 창병 복구가 아니다.

원본 Spear MotionSet 23개를 조사했다. 순수 창 Attack/Heavy/Skill 다수에 Collision 이벤트가 없고, 일부 Speat 키는 쌍검/카타나 모션을 해석한다. 매핑 존재만으로 통과시킬 수 없다. Sera의 기존 단창을 임시 사본에서 60Hz로 측정했으며 기존 부착의 보조손-축 간격은 공격에 따라 최대 약 0.8~1.1m였다. 정식 Humanoid 클립에는 추가 무기 본 커브가 없고 미리보기 데이터에는 R_Hand_Weapon/Spear_Root/Spear_Bone01/02가 있다. 원본 커브를 임시 이식한 사본 역시 검수 가능한 그립을 확정하지 못했다. Constraint 추종 오차를 그립 성공으로 처리하지 않았다. 원본·프리팹은 이 조사로 저장하지 않았다.

구체 후보는 **기존 Reien Definition/ID/배치 유지 복구** 또는 **새 일반 창병 제작·배치 보류**다. 필요한 범위는 대상 무기·그립 확정, 추가 본 커브를 담은 독립 클립, 실제 접촉을 기준으로 한 Collision 타이밍·전진 거리·중단 복구, 해당 액터 전용 MotionSet 매핑, 역할별 등급 데이터, 짧은 실행 검사다. 임의 기존 몬스터 전환이나 미사용 등급 Set을 추가하지 않았다. 캐릭터 선택 질문에 대한 사용자 답변을 기다린다.

## 검증과 보존

- 변경 전 Ability EditMode **281/282 통과**, TautString 연결 1건 실패. Validator **Error7/Warning599/Info645**를 현재 작업본에서 다시 실행했다. 변경 전 기존 대검 프리팹 Play 검사 1개도 통과했다.
- 변경 후 전체 Ability EditMode **287/288 통과**, 같은 TautString 1건 실패. 신규 데이터·보스 연결 검사 6개는 통과했다. 중간에 새 fixture의 오래된 `attackWeaponType` 메타데이터 가정 2건을 실제 소유 MotionSet 비교로 수정했으며, 데이터의 오류를 숨기기 위해 테스트를 제외하지 않았다.
- Editor Play Mode **6/6 통과**: 기존 수직 슬라이스 4개, 8 Definition×Idle/Hit/Stun/Death 상태 이탈 및 레벨·HP·라우터 검사 1개, 실제 근접2+궁수1/슬롯1+1의 점유 거절·양보·반환 검사 1개. 처음에는 동적 Hit 상태를 캐시 상태로 요청한 fixture가 실패해 실제 동적 상태 전환 API로 수정했다.
- 자동 재직렬화 정리 뒤 실제 에셋을 다시 로드하고 관련 EditMode **27/27**, 변경 프리팹과 혼성 슬롯 Play Mode **2/2**를 재통과했다. 정리 중 중첩 Bow 자식 연결 누락은 검사 실패로 확인·복구했다. 저장 직후 검토용 사본과 다른 후속 사용자 변경은 정리 스크립트가 덮어쓰지 않도록 차단한다.
- 슬롯 검사는 AI를 정지한 상태에서 직접 요청한 계약 검사다. 거리 관리·AI 정지 여부·자율 혼성전·연속 히트스톱·실제 피해·패링·화면 전조·FX 품질의 자동 통과 결과가 아니다. 피격/브레이크/사망의 데미지 진입 경로는 상태 주입과 구분해 사용자 검수 대기로 둔다.
- 변경 후 Validator **Error7/Warning599/Info648**. 새 Error/Warning 0, 새 Fork 표시 정보 Info3. P09 Validate는 현재도 **Armor06 Bow/Vital.MaxHealth bake 불일치**에서 중단한다. 이 스탯을 재생성하지 않았다.
- 보조 CLI: Assembly-CSharp-Editor 오류0/기존 obsolete 경고3, Ability.Tests 오류0, Ability.PlayModeTests 오류0. Unity 컴파일 Console 오류0 확인. Player/Addressables Build와 커밋은 하지 않았다.
- 신규 Fork 저장/Set 연결 실패 주입 6건 및 보스 첫 프리팹 저장/Profile 연결 실패 주입 2건에서 원본 에셋/meta 바이트 복구. 백업은 `Logs/HumanoidGrade/Remaining/Backup`, `BossBindingBackup`에 보존한다.
- 공용 Ability/Payload/MotionSet·플레이어 원본·스탯·기존 등급 Set GUID는 보존했다. 의도한 기존 Fork 쿨다운 2개와 등급 Set Add3개 외의 공유 공격은 변경하지 않았다. 실제 프리팹과 기존 Definition/Profile의 GUID·actorId·역참조를 유지했다. 작업 중 관찰된 NPC 코드·재질 등 다른 변경은 수정·복원하지 않았다.
- 테스트 명세: [G12~G17](HUMANOID_GRADE_PLAYTEST.md). 사람의 검수 결과는 아직 없다.

Test Runner 실행 식별자: 변경 전 전체 `f882a0ae3e8f48688a9d53d4d31065a5`, 변경 후 전체 `8b5935fec5f241c29013c88a4bad304e`, Play6 `45fb774d4927419d89472910cdf2378c`, 정리 후 Edit27 `e94356f9e4cc4337b7f3861f520ac8af`, 정리 후 Play2 `88d467abcc964d69b35b2ee5da7a0532`. 단계별 요약은 `Logs/HumanoidGrade/Remaining/test-results.json`에 보관한다.
