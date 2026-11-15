# 무기 9종 시그니처 궁극기 클립 실행 설계 (확정본)

> **상태: 구현 반영·허용된 자동 검사 완료, 실제 플레이·미술 인수 대기.** 남은 열린 결정: **0건**(§0.2). 모든 수치는 구현 시 측정으로 조정하는 제안값이다.
> 근거 지침: [전투 저작](../../../guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md) · [무기 모션 이식](../../../guide/WEAPON_MOTION_RETARGETING_GUIDE.md) · [NPC 애니메이션 BlenderMCP 절차](../../../guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md#6-blendermcp-신규-제작) · [UI/UX 저작](../../../guide/UI_UX_AUTHORING_GUIDE.md) · [카메라 이식 경계](../../../guide/CAMERA_MODULE_PORTABILITY_GUIDE.md) · [공통 하네스 아트 인수 조건](../../../../../Tools/Harness/README.md#구매mcp-제작-아트의-인수-조건) · FX 스킬 `.claude/skills/uplayground-vfx/SKILL.md`
> 관련: [궁극기 시퀀스 시스템](ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md) · [궁극기 연출 컨셉 §6](PLAYABLE_ULTIMATE_PRESENTATION_CONCEPT_DESIGN.md#6-클립-조사-후-확정한-제작-방향과-에셋) · [궁극기 연출 상태](../../../../../docs/work-status/weapon-ultimate-presentation/STATUS.md) · [궁극기 사용자 테스트](../../../../../docs/work-status/weapon-ultimate-presentation/USER_TESTS.md) · [Katana FX 상태](../../../../../docs/work-status/player-katana-fx.md) · [전투 위키](../../../../../docs/wiki/combat.md)

**변경 이력**

| 판 | 내용 |
|---|---|
| 1판 | 무기 9종 가능성 판정, 클립·데이터·FX·Blender 파이프라인 설계 |
| 2판 | D1=기존 궁극기 슬롯에 연결, D2=공유 세트는 시그니처 공유, Whip을 원형 휩쓸기로 전환, FX 절차를 `uplayground-vfx` 스킬로 교체 |
| 3판 | 카메라 연출(§13)과 스킬 컷인 연결(§14) 추가, Spear 공유 세트 포크 발견 |
| **확정본** | 시퀀스 경로는 전용 상태에서 이동 0이라 시그니처 9종 모두 일반 공격형으로 확정(시퀀스 2종은 보존·연결 해제), V 항목을 코드 읽기로 해소(파일:행), R 최종 채택·보류·폐기, D3 허용·D4 규칙 반영, 아린 궁극기 실행 불가 해소, 문서 내 모순 정리 |

## 0. 요약

### 0.1 판정 요약

판정 기준은 다음과 같다.

- **가능:** 클립·데이터·구매 FX만으로 만든다. C# 변경이 없다.
- **조건부:** 채택한 C# 요구사항(§11)이 있어야 요구를 온전히 충족한다. 데이터만으로 만드는 출시 대체안을 함께 확정했다.

| # | 무기(`WeaponType`) | 캐릭터 | 궁극기 실행 경로(확정) | 판정 | 결정적 근거 |
|---|---|---|---|---|---|
| 1 | Katana | 라온, 아린 | 일반 공격형(기존 시퀀스형에서 전환) | **가능** | 직선 피해는 몸을 따라가는 Window Box, 대시는 루트모션+워프로 처리하고 대상 앞에서 정지한다(관통 R2는 보류). 기본 GA를 일반 공격형으로 바꾸면 아린 실행 불가도 함께 해소 |
| 2 | Bow | 설아 | 일반 공격형 | **조건부(R4)** | 화살비 = `Stationary`+`AreaTick`. 대상이 없으면 장판이 시전자 위치에 생긴다(코드 확인) → R4 채택 |
| 3 | Spear | 레이네 | 일반 공격형(파생 세트 `AbilitySet_PlayerSpear`) | **조건부(R3·R4)** | 도약은 `AddForceEvent` 상향, 투척은 목표 지향 하향 투사체(코드 확인). 창 숨김은 R3, 대상 없을 때 착탄점은 R4 |
| 4 | DoubleAxe | 화린 | 일반 공격형(기존 시퀀스형에서 전환) | **조건부(R1)** | 공격 상태에 입력 이동 경로가 없음 → R1 채택. 시퀀스 상태는 이동 0이라 쓰지 않음 |
| 5 | DualBlade | 유라 | 일반 공격형 | **가능** | 토네이도 = `Stationary`+`AreaTick`, 대상별 interval 재타격(코드 확인) |
| 6 | Staff | 연화·묘묘(공유) | 일반 공격형 | **가능** | 지속 빔 = 짧은 Window Box 반복. 벽 관통을 허용하고 FX도 관통 표현으로 맞춘다(R5 폐기) |
| 7 | GreatSword | 세라·릴리(공유) | 일반 공격형 | **가능** | Box 1회 + 충격파 Sphere. 거대 검 FX는 판정 직전 0.2초 하강으로 확정 |
| 8 | Whip | 리안 | 일반 공격형(몸+채찍 본 쌍) | **가능** | 원형 휩쓸기 2회 + 전방 마무리. 기존 쌍 클립을 재사용한다 |
| 9 | SwordShield | 묘령 | 일반 공격형 | **조건부(R3·R4)** | `Arc`+`Detonate` 투사체. 방패(보조 무기) 숨김은 R3, 착지점은 R4 |

### 0.2 확정한 결정 목록 (열린 결정 0건)

| ID | 결정 | 근거 절 |
|---|---|---|
| D1 | 시그니처는 기존 Ultimate 슬롯에 연결한다. 새 슬롯은 만들지 않는다 | §1.1 |
| D2 | 같은 AbilitySet을 쓰는 캐릭터(세라·릴리, 연화·묘묘)는 시그니처를 공유한다 | §1.3 |
| D3 | 컷인 9종의 importer를 Sprite로 전환하는 것을 **허용**한다. GUID와 PNG는 유지한다 | §14.2 |
| D4 | 유라 컷인은 별도 작업에서 제작 완료되어 기존 PNG를 연결했다. 이미지가 없으면 표시를 생략하는 규칙은 유지한다 | §14.2 |
| E1 | 라온·화린 시퀀스는 시그니처에 **연결하지 않는다**(에셋·잠금 값 보존, 미사용). 시퀀스 실행 상태는 이동 속도를 0으로 고정하고 사망 외 이탈을 막아서(`UltimateSequencePlayer.cs:556-576`), 대시·휠윈드가 성립하지 않는다. 9종 모두 일반 공격형이므로 입력 잠금·무적·적 정지가 없다. 상태: 확정(구현 시 재인수 필요) | §1.2·§1.5 |
| E2 | Katana·DoubleAxe 궁극기 Payload를 일반 공격형(`UPlayGroundMotionAbilityPayloadSO`)으로 전환한다. 아린 실행 불가도 이걸로 해소된다. 라온은 `AbilitySet_Raon`이 기본 세트를 상속하므로 같은 GA를 쓴다. R7 폐기 | §1.1 |
| E3 | Spear는 공유 Default 세트를 건드리지 않고 파생 세트로 분리한다 | §1.1 |
| E4 | Whip은 원형 휩쓸기 추천안으로 확정한다 | §5.8 |
| E5 | 카메라 연출은 포즈 합성 효과(FOV·Zoom·Rotation·Shake·TimeScale)만 쓴다. `CameraLookAtSocketEvent`·Snapshot은 쓰지 않는다 | §13 |
| E6 | 궁극기는 시작 시점에 비용을 Commit한다(코드 확인). 그래서 대표 임팩트 전에는 캔슬을 열지 않는다 | §3.2 V13 |
| E7 | R1·R3·R4·R8 채택, R2·R9 보류, R5·R6·R7·R10·R11 폐기 | §11 |
| E8 | 반복 구간은 `LoopEvent`가 아니라 클립 체인으로 만든다 | §3.2 V1 |
| E9 | 스왑 후에도 투사체 장판은 끝까지 유지한다. 사망·씬 이탈 때는 정리한다. 실제 동작은 구현 단계에서 검증한다 | §9.2 |

구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1. 궁극기 연결

#구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1.1 연결 방식과 매핑

```text
기존 GA_Player<무기>_Ultimate (abilityId·비용 1.0·쿨다운 12초·cooldownGroupId 유지)
└ Variant Default → 기존 Payload (GUID 유지, 내용만 재저작)
     motionKey 기존 키 유지 / hitPhases 재작성
무기 자기 MotionSet.abilityMotions[기존 키] → 새 <무기>_Signature.asset
기존 궁극기 MotionSet 에셋은 보존 (복구·비교용)
```

| 무기 | 데이터 변경(확정) |
|---|---|
| Katana | 라온은 `AbilitySet_Raon`(baseSet = `PlayerKatanaAttackData`, 대체 목록은 다른 GA만 교체, `AbilitySet_Raon.asset:22-29`)을 쓰고, 아린은 기본 세트를 직접 쓴다. 둘 다 기본 `GA_PlayerKatanaAttackData_Ultimate`를 쓴다. 현재 이 GA는 라온 소유 시퀀스형이라 **아린 요청이 거부된다**(§3.2 V12). 확정안은 다음과 같다. ① 이 GA의 Payload를 **일반 공격형(`UPlayGroundMotionAbilityPayloadSO`)으로 교체**한다. 위 블록의 "Payload GUID 유지"의 예외로, Payload 서브 에셋 종류가 바뀐다. GA GUID·abilityId·cooldownGroupId는 유지한다. ② `Katana.Ultimate` 매핑을 `Set_Katana`(라온, 무기 본 이식판 MotionSet)와 `Humanoid_Player_KatanaAnimationSet`(아린, 일반 MotionSet) 각각의 시그니처로 교체한다. ③ `UltimateSequence_Raon`은 보존하고 연결만 해제한다. `Bokusei_KatanaAnimationSet`은 정의 SO 참조가 없어 바꾸지 않는다(IV-7) |
| DoubleAxe | `GA_PlayerDoubleAxeAttackData_Ultimate`의 Payload를 일반 공격형으로 교체(Katana와 같은 예외)하고, `Humanoid_Player_DoubleAxeAnimationSet`의 `DoubleAxe.Ultimate`를 교체한다. `UltimateSequence_Hwarin`은 보존·연결 해제한다. 화린만 이 세트를 쓴다 |
| SwordShield | `Humanoid_Player_SwordShieldAnimationSet`의 `SwordShield.Ultimate` |
| Spear | **포크.** `PlayerDefaultAttackData`는 레이네 외에 `AbilitySet_PlayerStaff.baseSet`, 몬스터 프로필 YeonHoa·Reien·Myomyo, `MonsterYeonHoa` DB, `Humanoid_Player_CommonAnimationSet.attackAbilitySet`이 참조한다(GUID 79e1c4a3… 검색 14건). 그래서 파생 `AbilitySet_PlayerSpear`(baseSet = Default)를 만들고, 대체 목록으로 Ultimate만 `GA_PlayerSpear_Ultimate`(abilityId `Player.Spear.Ultimate`, motionKey `Spear.Ultimate`)로 바꾼다. 레이네 정의 SO의 `abilitySet`을 이 세트로 교체하고, `Spear.Ultimate`를 `Humanoid_Player_SpearAnimationSet`에 신규 등록한다. 기존 `Default.Ultimate`는 손대지 않는다 |
| Whip | `Humanoid_Player_WhipAnimationSet`과 `WeaponMotion/Whip/WhipMotionSet`의 `Whip.Ultimate` **둘 다** |
| Bow | `Humanoid_Player_BowAnimationSet`의 `Bow.Ultimate` |
| GreatSword | `Humanoid_Player_GreatSwordAnimationSet`의 `GreatSword.Ultimate` |
| Staff | `Humanoid_Player_StaffAnimationSet`의 `Staff.Ultimate`(현재 GUID 1128af88…). 플레이어 Staff 세트의 Ultimate는 자체 GA(`GA_PlayerStaff_Ultimate`) |
| DualBlade | `Humanoid_Player_DualBladeAnimationSet`의 `DualBlade.Ultimate` |

각 키는 다른 무기 세트(교차 매핑)와 몬스터 세트(`MonsterHwarin_AnimationSet` 등)에도 있다. 교체는 위 표의 세트만 한다.

#구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1.2 실행 경로와 시퀀스 잠금 확정

`PlayerCombat.RequestUltimate`(`PlayerCombat.cs:740-812`)는 Payload 종류로 갈린다.

- **시퀀스형**(`UPlayGroundUltimateAbilityPayloadSO`): `UltimateSequencePlayer`가 전용 `PlayerUltimateState`로 전환한다(`UltimateSequencePlayer.cs:374`). 이 상태는 `UpdateVelocity`에서 **속도를 0으로 고정**하고, `InterruptLocked` 태그와 함께 사망 외 이탈을 막는다(`UltimateSequencePlayer.cs:556-576`). 그래서 루트모션 대시·워프·`AddForce` 도약·이동 휠윈드·회피 캔슬이 모두 불가능하다.
- **일반 공격형**: `PlayerAttackState`로 실행한다. 루트모션 평면 이동 + 워프, KCC 수직 탄도를 보존하고(`PlayerAttackState.cs:1048-1092`), `interruptActions`로 캔슬을 제어한다.

**확정: 시그니처 9종 모두 일반 공격형으로 실행한다.** 라온·화린 시퀀스는 다음과 같이 처리한다.

- 에셋과 잠금 값(입력 잠금 1·카메라 잠금 1·적 AI 정지 1·적 고정 1·시전자 무적 1·피격 무시 1)을 **변경 없이 보존**하고, 궁극기 Payload와의 연결만 해제한다. 잠금 동작 근거: `UltimateGameplayLockContext.cs:40-111`.
- 시그니처 실행 중의 잠금 효과는 다음과 같이 확정한다.

| 항목 | 확정 값(9종 공통) | 근거 |
|---|---|---|
| 입력 | 공격 상태 + Payload `interruptActions`로 대표 임팩트 전 캔슬 차단(E6) | 비용이 시작 시 Commit됨 |
| 카메라 입력 | §13.3 표에서 지정한 구간만 `CameraEffectEvent.lockCameraInput` | 무기별 조작 요구가 다름(DoubleAxe는 잠그지 않음) |
| 적 AI 정지·적 고정 | 없음 | 위험·보상 유지, 9종 형평 |
| 시전자 무적 | MotionSet `InvincibilityEvent` 구간만(Katana F12–21) | 전 구간 무적 금지 |
| 피격 반응 무시 | 없음 | 넘어짐·넉백이 화면에 보여야 함 |
| 얼굴 패널 | R8 MotionEvent(12명 공통) | 시퀀스 미사용 |

**상태: 확정(구현 시 재인수 필요).** 재인수 대상은 §1.5에 있다. 시퀀스를 다시 쓰려면 시퀀스 상태에 이동을 허용하는 C#이 필요하다. 이번 설계에서는 채택하지 않는다.

#구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1.3 기존 궁극기 결정과의 충돌·교체 범위 (캐릭터별)

| 캐릭터 | 무기 | 기존 결정·자산 | 확정 변경 | 보존 |
|---|---|---|---|---|
| 라온 | Katana | 전용 시퀀스(5.01초 모션, 흔들림 4.008초, Slow 2.083초, 얼굴 hold 1.4) | 일반 공격형 시그니처(약 1.7초, 무기 본 이식판). 슬로·흔들림은 MotionSet 카메라 SO, 얼굴은 R8 | 시퀀스 에셋(연결 해제·보존), 얼굴 PNG·정의 SO 연결, Katana FX 24개(다른 모션) |
| 아린 | Katana | §6 A1 현행 체인 채택, 얼굴 사용자 통과 | 일반 공격형 시그니처, 얼굴 R8 | 통과한 얼굴 PNG, §6.2 입력 에셋 |
| 화린 | DoubleAxe | 전용 시퀀스(1.83초, Callback·CameraEffect, 얼굴 hold 1.2) | 일반 공격형 휠윈드(약 3.9초), 얼굴 R8 | 시퀀스 에셋(연결 해제·보존) |
| 묘령 | SwordShield | §6 M1 현행 유지 | 방패 투척으로 교체, 얼굴 R8 | 얼굴 작화 |
| 레이네 | Spear | §6 R0 보존 + R1 직선 후보 | R0(점프)를 기반 후보로 승격. R1 직선 후보는 폐기(라이브러리 보관). 파생 세트 | R0·R1 에셋 |
| 리안 | Whip | §6 L1+LW1 쌍 재사용(T3 접촉은 컨셉 문서의 별도 사안) | 원형 휩쓸기로 교체. T3 접촉·속박은 시그니처에 넣지 않음(이 설계의 결정 대상 아님) | L1·LW1 에셋 |
| 설아 | Bow | §6 B1 현행 유지 | 화살비로 교체 | — |
| 세라·릴리 | GreatSword | §6 조합 조건부, 세라 0.357초 차이 | 공용 시그니처 하나(D2). 캐릭터 전용 분리안과 0.357초 문제는 실행 경로에서 사라짐 | 조합 조사 결과 |
| 연화·묘묘 | Staff | §6 S1 + 손가락 레이어, FX 차별화 | 공용 빔(D2). 캐릭터 차이는 컷인 이미지만(R8이 시전자별 이미지 표시) | 손가락 마스크 계약 |
| 유라 | DualBlade | §6 Y1 현행 유지 | 토네이도로 교체 | — |

컨셉 §6에서 "현행 재사용"으로 확정한 7명의 실행 소스는 모두 시그니처로 대체된다. §6.2 입력 에셋은 삭제하지 않는다. 컨셉 §2.2·§2.3의 화면 원칙은 그대로 적용한다.

#구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1.4 비용·쿨다운·카메라 접점

- **비용·쿨다운:** 9종 모두 비용 `resourceType 1`·`value 1`(게이지 전체), 쿨다운 12초다. 그대로 유지한다. Spear 파생 GA도 같은 값과 기존 Default 궁극기의 `cooldownGroupId`를 쓴다(세이브 쿨다운 호환).
- **Commit 시점:** 시작할 때 비용을 소모한다(E6). 그래서 대표 임팩트 전 캔슬은 열지 않는다.
- **카메라:** 시퀀스 경로를 쓰지 않으므로 [궁극기 시퀀스 시스템](ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md)의 `cameraProfile`·`UltimateTimeScaleEvent`도 쓰지 않는다. 흔들림·FOV·슬로는 모두 MotionSet `CameraEffectEvent` 한 곳이 소유한다(§13).
- **밸런스:** 시그니처 총 기대 피해를 현재 궁극기 Payload hitPhases 합과 비교해 S3에서 정한다.

#구현 진행·검증 기록: [시그니처 작업 상태](../../../../../docs/work-status/weapon-signature/STATUS.md).

## 1.5 확정 후 재테스트 목록 (구현 시 재인수)

| 대상 | 다시 받을 테스트 | 이유 |
|---|---|---|
| [궁극기 사용자 테스트](../../../../../docs/work-status/weapon-ultimate-presentation/USER_TESTS.md) | U01~U09 | 라온·화린이 시퀀스에서 일반 공격형 + R8 패널로 바뀜. U03(라온 0.2배속 구간)·U04(HUD/Stage)·U07(입력 잠금)·U08(무게감)·U09(카메라)는 기대 결과 자체를 새 설계로 갱신해야 함 |
| 라온·화린 얼굴 재제작 | CR01~CR02 | 같은 이미지가 R8 경로·새 hold로 표시됨 |
| 아린·묘령 신규 작화 | F01~F03, M01~M02 | 아린·묘령 패널이 R8로 처음 표시됨 |
| 궁극기 자동 검사 | 얼굴 패널 PlayMode(`FaceStripPlayModeTests`), P1~P4 EditMode | 시퀀스 연결 해제에 따른 기대값 변경 |
| Katana FX | KFX03 | 라온 교대·특수 공격 FX 보존 여부(시그니처와 별개 모션, 회귀 확인) |

## 2. 실제 명칭 대조

| 요구 이름 | `WeaponType` | AbilitySet (`Assets/10.Datas/Ability/`) | 시그니처 GA / motionKey(확정) | Payload |
|---|---|---|---|---|
| Katana | `Katana`(10) | `Migrated/PlayerKatanaAttackData`(아린), `Player/Raon/AbilitySet_Raon`(라온, 파생) | `GA_PlayerKatanaAttackData_Ultimate`(라온·아린 공용) / `Katana.Ultimate` | 일반(시퀀스형에서 전환) |
| Bow | `Bow`(5) | `Migrated/PlayerBowAttackData` | `..._Ultimate` / `Bow.Ultimate` | 일반 |
| Spear | `Spear`(13) | `Migrated/PlayerDefaultAttackData` → 파생 `AbilitySet_PlayerSpear`(신규) | `GA_PlayerSpear_Ultimate`(신규) / `Spear.Ultimate` | 일반 |
| Double Axe | `DoubleAxe`(11) | `Migrated/PlayerDoubleAxeAttackData` | `..._Ultimate` / `DoubleAxe.Ultimate` | 일반(시퀀스형에서 전환) |
| Dual Blade | `DualBlade`(14) | `Migrated/PlayerDualBladeAttackData` | `..._Ultimate` / `DualBlade.Ultimate` | 일반 |
| Staff | `Staff`(4) | `Player/Staff/AbilitySet_PlayerStaff`(baseSet Default) | `GA_PlayerStaff_Ultimate` / `Staff.Ultimate` | 일반 |
| GreatSword | `GreatSword`(3) | `Migrated/PlayerGreatSwordAttackData` | `..._Ultimate` / `GreatSword.Ultimate` | 일반 |
| Whip | `Whip`(12) | `Migrated/PlayerWhipAttackData` | `..._Ultimate` / `Whip.Ultimate` | 일반 |
| Sword Shield | `SwordShield`(2) | `Migrated/PlayerSwordShieldAttackData` | `..._Ultimate` / `SwordShield.Ultimate` | 일반 |

## 3. 공통 데이터 계약

```text
GA (비용·쿨다운·활성 태그 단일 소스)
└ Payload: motionKey / hitPhases[0..n] / interruptActions / useTelegraph …
무기 MotionSet.abilityMotions[키] → <무기>_Signature.asset
   └ 클립 체인 + 이벤트: Collision · SpawnProjectile · Particle · SlashVFX · Sound · CancelWindow
                         · Invincibility · Afterimage · CameraEffect · AddForce · HideTarget(R3 전까지) · 얼굴 패널(R8)
투사체: ProjectileDefinitionSO (Projectile_<무기>Signature*.asset, 기존 공유 정의 수정 금지)
시퀀스: UltimateSequence_Raon/Hwarin은 연결 해제·보존(§1.2)
```

- 저작은 `generate-gas-ability` 스킬로 한다. 저작 전 기존 Payload·MotionSet·시퀀스 해시를 기록한다.
- Payload hitPhases 교체와 MotionSet 매핑 교체는 **같은 작업 단위**로 묶는다. 그래야 기존 궁극기가 중간 상태로 깨지지 않는다.
- 피해·강인도는 hitPhases에만 둔다. 투사체는 `hitPhaseIndex`로 참조한다. 판정 시점은 MotionSet, 장판 틱·수명은 투사체 정의가 소유한다.
- 틱 타격 페이즈와 Katana 스윕 페이즈는 `reactionProfile.manualOverride`로 히트스톱을 0~0.02초로 둔다. 대형 임팩트 페이즈는 카메라 흔들림을 0으로 둔다(§13.2 단일 소유).

### 3.1 실행 수단

| 수단 | 파일 | 용도 |
|---|---|---|
| `BeginCollisionEvent` + `ExplicitShape` | `MotionEvent_Collision.cs`, `ExplicitCollisionShapeData.cs` | Sphere/Box/Capsule, Anchor·샘플링·평가 방식 |
| 적중 중복 방지 | `PlayerCombat.BeginCollision` → `ClearHitTargets()` | Window 하나는 대상당 1회 |
| `SpawnProjectileEvent` | `MotionEvent_SpawnProjectile.cs` | 패턴·목표 모드·지면 투영 |
| `ProjectileDefinitionSO` / `ProjectileRuntime` | `ProjectileDefinitionSO.cs`, `ProjectileRuntime.cs:765-800·830` | `AreaTick` 대상별 interval 재타격, `Detonate` 1회 |
| `AddForceEvent` | `MotionEvent_AddForce.cs`, `ActorMovementController.cs:43·108-132` | 상향은 `AddLaunch`(상한 12m/s, 상태가 허용할 때) |
| `HideTargetEvent` | `MotionEvent_HideTarget.cs` | 이름 기반 Renderer 토글(R3 전까지 사용) |
| 카메라 이벤트·SO | §13.1 | |

### 3.2 코드 읽기로 해소한 항목 (V)

| ID | 질문 | 결과·근거 | 설계 반영 |
|---|---|---|---|
| V1 | `LoopEvent` 구간 안 이벤트 재발화 | 루프 시 `SeekCurrentMotionLocal`이 `SeekTo`를 호출한다(`MotionSetPlaybackController.cs:497-509·522-534`). `SeekTo`는 실행 기록을 비우고 **되감은 시각 이전** 이벤트만 실행 처리한다(`MotionEventExecutor.cs:389-414`). 따라서 루프 구간 안 이벤트는 재발화한다. 단, 루프 시작 시각과 같은 시각의 이벤트는 재발화하지 않을 수 있다 | **클립 체인 확정(E8).** 반복마다 hitPhase를 명시할 수 있고 이미 검증된 패턴이다. LoopEvent는 쓰지 않는다 |
| V2 | `HideTargetEvent` 중단 시 복구 | `StopMotionSet` → `MotionEventExecutor.Stop` → `ExitActiveEvents` → `OnCompleteEvent`(`ActorAnimator.cs:1102-1114`, `MotionEventExecutor.cs:352-361·363-368`). MotionSet 정지 경로에서는 복구됨 | 스왑·사망 경로와 이름 지정 실패 때문에 **R3 채택**. 스왑·사망 확인은 구현 단계 검증(IV-1) |
| V3 | 목표 지향 시 하향 발사 | 목표 위치가 있으면 비행 방향 = `(target − spawn).normalized`로 높이 성분을 포함한다. `Stationary`면 생성 위치 = 목표 위치. 목표가 없으면 정면 수평이고 생성 위치는 스폰점(`MotionEvent_SpawnProjectile.cs:103-106·121-145`) | 창 하향 투척 가능(`Linear`). 대상 없으면 장판이 시전자 위치에 생김 → **R4 채택** |
| V4 | `AreaTick` 재타격 | 대상별 interval 쿨다운 후 재타격(`ProjectileRuntime.cs:798`), `Detonate`는 무한 쿨다운으로 1회(`:830`) | 화살비·토네이도 성립 |
| V5 | Ultimate 슬롯 바인딩 | `TryPreparePlayerSlot(PlayerSkillSlot.Ultimate)`(`PlayerCombat.cs:770-777`) | D1 |
| V6 | Staff 키 소유 | `Humanoid_Player_StaffAnimationSet`의 `Staff.Ultimate` | §1.1 |
| V7 | 후보 클립 FPS | 코드로 알 수 없음(Unity 조회 금지) | 프레임표는 30fps 제안값으로 확정. 실측은 구현 단계(IV-2) |
| V8 | 스왑·사망·씬 이탈 시 투사체 | 이번 읽기 범위 밖 | 정책 확정(E9). 확인은 구현 단계(IV-3) |
| V9 | 파티클 FX 시간 기준 | `BeginParticleEvent`는 프리팹을 Instantiate하고 끝에서 Destroy할 뿐 액터 시간·공격 속도를 반영하지 않는다(`MotionEvent_Particle.cs:76·100·148-151`) | 거대 검 등 동기형 FX는 **판정 직전 0.2초 하강**으로 짧게 확정. 이벤트 구간이 FX 수명을 덮도록 저작 |
| V10 | 형상 판정의 장애물 처리 | `CombatHitDetector`는 Overlap만 쓰고(`CombatHitDetector.cs:216-252`) Combat 폴더에 Raycast·Linecast가 없다 | 벽 관통 판정으로 확정. 빔 FX도 관통 표현으로 맞춘다. **R5 폐기** |
| V11 | `isAerialSkill`·`aerialMovement` 의미 | `isAerialSkill`은 몬스터 AI 선택 필터(`EnemyCombat.cs:155·1109`, `EnemyAttackRangePolicy.cs:212`). `aerialMovement`는 점프 공격 상태만 소비(`PlayerJumpDashAttackState.cs:50·85-98`) | Spear 도약은 `AddForceEvent` 상향으로 확정. 근거: 공격 상태가 상향을 허용하고(`GameActorState.cs:46`), 공격 상태 속도 계산은 루트모션을 **평면에만** 반영하고 KCC 수직 탄도(발사 속도·중력)를 보존한다(`PlayerAttackState.cs:1085-1092`). 그래서 루트모션 Y로는 뜨지 않는다. 두 필드는 쓰지 않음 |
| V12 | 아린 Katana 궁극기 | 시퀀스 소유자가 다르면 거부(`UltimateSequencePlayer.cs:180-184·258-262`) → `RequestUltimate`가 Rejected(`PlayerCombat.cs:797-811`). 아린은 기본 Katana 세트를 직접 쓴다(정의 SO abilitySet 1adb2f37…) | **현재 아린 궁극기는 실행되지 않는다**(코드 기준, PlayMode 미확인). E2(일반 공격형 전환)로 해결, R7 폐기 |
| V13 | 비용 Commit 시점 | 일반 공격형은 공격 시작 직후 Commit(`PlayerAttackState.cs:957-972`), 시퀀스형은 잠금 획득 직후 Commit(`UltimateSequencePlayer.cs:295-301`) | 캔슬하면 게이지·쿨다운을 소모한다 → 대표 임팩트 전 캔슬 금지(E6) |
| V14 | `pitchDelta` 부호 | 인게임 합성은 `CameraRotation * AngleAxis(pitchDelta, right)`, 카메라는 pivot 뒤로 배치(`EffectRotationInjectCameraModifier.cs:23-28`) → **양수 = 내려다봄, 음수 = 올려다봄** | §13.3 값에 부호 반영 |
| V15 | TimeScale 효과의 지속 소유 | `useHitStopManager=true`이면 어댑터 `ExecuteHitStop(duration, scale)`로 넘기고 효과 duration이 지속을 소유한다. 중단 시 `StopHitStop`(`TimeScaleCameraEffect.cs:33-39·57-63`) | 슬로 0.08~0.12초는 효과 SO duration으로 지정 |
| V16 | 락온과 연출 회전 | 연출 회전은 포즈에만 합성되고 지속 상태에 누적되지 않는다. 락온 안정 시야는 연출 회전을 빼고 계산한다. 단, LookAt override·회전 전환 중에는 갱신을 건너뛴다(`InGameCameraBehavior.cs:46-52`, `EffectRotationInjectCameraModifier.cs:5-8`) | 포즈 합성 효과만 사용, `CameraLookAtSocketEvent` 미사용(E5). **R10 폐기** |
| V17 | `reactionProfile` 카메라 값 소비 | `AttackData`가 `cameraShakeAmplitude`·`fovKickAmount`·히트스톱을 동작 반응 데이터로 옮긴다(`AttackData.cs:108-113`) | 대형 임팩트 페이즈는 `manualOverride` 카메라 0, 틱 페이즈는 히트스톱 최소. 0으로 두는 쪽은 소비 여부와 무관하게 안전 |

## 4. 프레임 기준

- 프레임은 **30fps 기준 제안값**이다(초 = 프레임/30). 클립 확정 후 실제 `AnimationClip.frameRate`와 MotionSet 시계로 다시 잰다(IV-2).
- 연결된 Blender의 씬 FPS는 24였다(1판 읽기 조회). 제작 파일에 FPS를 명시한다.
- 표기: **H**=판정, **M**=이동, **C**=캔슬 허용(모두 대표 임팩트 이후), **I**=무적.

## 5. 무기별 설계

각 절의 카메라는 §13.3, 컷인은 §14.3의 같은 무기 행이 이 프레임표에 붙는다. FX 제작 단위는 §8.3이다.

### 5.1 Katana — 발도 일섬 대시

> 카메라: §13.3 Katana · 컷인: §14.3 라온·아린(R8)

**판정: 가능.** 관통 대시(R2)는 보류하고, 대상 앞 정지형으로 확정한다. 칼집은 라온만 휴대 사례가 있다(`ArtSource/FrankWeaponRetarget/enable_carried_sheath.csx`). 아린은 칼을 장착한 채 발도 연출 없이 시작한다.

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 준비 | 0–12 | 무게중심을 낮추고 칼집을 앞으로, 시선은 대상 | |
| 발도·출발 | 12–15 | 칼을 6할 뽑으며 앞발 착지 | I 12–21 |
| 대시 | 15–21 | 최대 8m 수평 이동(0.2초) | M 15–21, **H 15–21 스윕**, 잔상 |
| 일섬 정점 | 21 | 칼이 수평 통과 후 정지 | 섬광·슬로(`TimeScale` SO) |
| 칼끝 연장 | 21–24 | 잔심 | **H 21–24(전방 2m)** |
| 잔심·납도 | 24–51 | 칼집 회수(칼집 없으면 장착 유지) | C 회피 36~, 이동 42~ |

- **데이터:** §1.1 Katana 행. `hitPhases[0]` 스윕(Heavy 계열, 히트스톱 `manualOverride` 약 0초), `hitPhases[1]` 칼끝 연장. 스윕은 적마다 다른 프레임에 맞으므로, 자동 히트스톱(약 0.105초)이 적마다 걸리면 대시가 끊긴다. 정지감은 F21에 한 번만 준다.
- **히트 형상:** F15–21 Box, `ActorRoot`, `FollowDuringWindow`, `Window`, `(2.5, 2, 2)`, `localOffset (0, 1, 0.5)`, `ActorForward`. 실제로 지나간 경로만 쓸고 간다. F21–24는 같은 Box를 `localOffset (0, 1, 2)`로 둔다.
- **이동:** 루트모션 + MotionWarp(최대 8m, 대상 앞 1.0m 정지). 대상이 없으면 최대 거리로 허공 대시한다. 공격 상태의 루트모션 평면 이동과 워프가 이동을 맡는다(`PlayerAttackState.cs:1067-1083`).
- **FX:** `Sword Slash 11` 직선 변형, `VFX_Dash`, `AfterimageEvent`, `CFXR4 Sword Hit PLAIN (Cross)`.
- **클립 후보(미검수):** `Frank_RPG_Katana_S1/S2/S3_Skill01·02`, `Grruzam .../M_katana_Blade@Dash_Attack_ver_A/B`, `Katana_Skill_3_PlayerApproach`, Kevin `UnsheatheHips01_R`. 라온은 같은 원본에서 무기 본 커브를 이식한 변형이 필요하다.
- **재미:** 무적은 F12–21로 한정한다. 대상 앞 정지형의 "베고 지나감" 부족은 칼끝 연장 판정·슬로·흔들림으로 보완한다(지연 절단 R6 폐기).

### 5.2 Bow — 하늘 사격 화살비

> 카메라: §13.3 Bow · 컷인: §14.3 설아(R8)

**판정: 조건부(R4).**

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 하늘 조준 | 0–10 | 활을 60~75° 위로 | |
| 시위 당김 | 10–22 | 시위 최대 | 모이는 빛 |
| 발사 | 22 | 손을 놓고 반동 | 상승 화살, 예고 시작 |
| 팔로스루·복귀 | 22–48 | 활을 내림 | C 회피 30~(발사 후), 이동 38~ |
| (필드) 화살비 | 40 ~ 112 | 예고 0.6초 → 0.3초 간격 6회 | **H 틱×6** |

- **데이터:** `GA_PlayerBowAttackData_Ultimate`. `hitPhases[0]` 틱(Light, 히트스톱 최소).
- **히트 형상:** F22 `SpawnProjectile` `TargetPosition` + `projectTargetToGround`. 정의 `Projectile_BowSignatureRain`: `Stationary(attachToGround)`, `AreaTick(radius 4, interval 0.3, activationDelay 0.6)`, `lifetime 2.4`, `destroyOnHit=false`. 상승 화살은 피해 없는 `BeginParticleEvent`로 처리한다. 대상이 없으면 R4 폴백(전방 12m 지면)을 쓴다.
- **FX:** `Meteor shower 2` 구조를 화살로 교체, `CFXR4 Rain Splashes` 계열, 예고 링.
- **클립 후보:** B1 `Humanoid_Bow_Skill_2`, Platform `Stander@Bow_Skill8_Air`·`Skill7_Around`. 하늘 조준 포즈는 Blender 1순위다. `suppressWeaponAim=true`.
- **재미:** 틱 Light 경직으로 이탈을 늦춘다(영구 고정 금지). 발사 후 회피로 "깔고 빠지기"가 된다. 보스 단일 대상 기대 피해는 B1 이상으로 맞춘다.

### 5.3 Spear — 도약 내리꽂기 투척

> 카메라: §13.3 Spear · 컷인: §14.3 레이네(R8)

**판정: 조건부(R3·R4).**

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 웅크림 | 0–9 | 창을 뒤로 끌며 무릎 굽힘 | |
| 도약 | 9–27 | 2.5~3m 상승, 창을 머리 뒤로 | **AddForce F9 상향 9m/s + 공격 중력 배율 0.3**, M 수직 |
| 정점·조준 | 27–30 | 몸이 활처럼 휨 | |
| 투척 | 30 | 손을 놓음 | **SpawnProjectile 30**, 창 숨김 30–50 |
| 착탄 | 약 39 | 창이 지면에 박힘 | **H 착탄(Detonate)** |
| 착지·복귀 | 30–60 | 착지, F50 창 재등장 | C 회피 52~ |

- **데이터:** 파생 세트의 `GA_PlayerSpear_Ultimate`(§1.1). `hitPhases[0]` 착탄(Knockdown 또는 Airborne).
- **도약:** `AddForceEvent` direction (0,1,0)·force 9와 `AttackGravityEvent` 배율 0.3. 상향 상한은 12m/s(`ActorMovementController.cs:43`)이고 공격 상태는 상향을 허용한다(V11). `isAerialSkill`·`aerialMovement`는 쓰지 않는다.
- **히트 형상:** `Projectile_SpearSignatureFall`: `Arc(arcHeight 0, Fixed 0.3초)`, `Detonate(radius 3.5, onHit=false, onExpire=true)`. 비행 접촉 피해는 생략한다. `TargetPosition` + `projectTargetToGround`. 대상이 없으면 R4(전방 8m).
- **창 숨김:** R3(슬롯 단위 숨김·무조건 복구) 구현 후 그것을 쓴다. R3 전 임시로는 `HideTargetEvent`도 MotionSet 정지 시 복구된다.
- **FX:** 실제 창 메시 복제 `visualPrefab` + 트레일, 착탄 `Meteor hit 2` + `CFXR2 Ground Hit` + 균열 데칼, 재등장 섬광.
- **클립 후보:** R0 `Humanoid_Spear_Skill_2`, `Frank_RPG_Spear_Skill03`, Platform `Stander@Spear_Skill8_Air`. 투척 해제 구간(F27–36)은 Blender로 만든다.
- **재미:** 체공 1초 동안 조작이 묶이는 대신 높이가 지상 공격 회피라는 보상이 된다. 착탄 시각과 판정을 일치시킨다.

### 5.4 DoubleAxe — 이동 휠윈드

> 카메라: §13.3 DoubleAxe · 컷인: §14.3 화린(R8)

**판정: 조건부(R1).** R1 전 출시 대체안은 "루트모션 전진 + 대상 워프로 따라가는 휠윈드"다.

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 진입 | 0–12 | 양도끼를 벌림 | |
| 회전 | 12–87(15f × 5, 클립 체인) | 상체 수평 회전, 하체 보폭 | **H 회전마다 ×5**, M(R1) |
| 마무리 | 87–102 | 내려찍기 | **H 강타** |
| 복귀 | 102–117 | 균형 회복 | C 회피 87~(마무리 후), 이동 108~ |

- **데이터:** `GA_PlayerDoubleAxeAttackData_Ultimate`(일반 공격형으로 전환, §1.1). `hitPhases[0]` 회전 틱(Light, 히트스톱 최소), `[1]` 마무리(KnockBack).
- **히트 형상:** 회전마다 Sphere r 2.8 `FollowDuringWindow` `Window` 0.1초, 마무리 Sphere r 3.5 `OnceOnBegin`.
- **이동(R1):** 공격 상태에서 MotionSet "입력 이동 창" 이벤트 구간 동안 입력 벡터 × 배율(달리기의 55%)과 회전 속도를 루트 속도에 합산한다. 시퀀스 상태(이동 0)는 쓰지 않으므로 공격 상태만 고치면 된다. 클립 루트 회전은 Bake Into Pose로 둔다.
- **FX:** 양 도끼 `CFXR4 Sword Trail PLAIN (360 Spiral)`, 회전 먼지, 마무리 `CFXR2 Ground Hit`.
- **클립 후보:** `Frank_RPG_2Hand_Skill01_WhirlWind.FBX`, 기존 `Humanoid_DoubleAxeAxeAnimationSet_Skill_1`(Collision 28~49f).
- **재미:** 이동 55%, 지속 2.5초 고정, 강인도 위주 + Light 경직, 슈퍼아머 없음, 피격 시 종료. 피격 종료로 게이지를 잃는 것은 리스크로 인정한다(E6).

### 5.5 DualBlade — 회전 토네이도

> 카메라: §13.3 DualBlade · 컷인: §14.3 유라

**판정: 가능.**

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 교차 준비 | 0–9 | 두 칼 X자 교차 | |
| 회전 | 9–30 | 2회전 | **H 근접 ×2** |
| 토네이도 생성 | 27 | 칼을 위로 쳐올림 | **SpawnProjectile 27** |
| 마무리 | 30–45 | 칼을 털고 물러섬 | C 회피 36~, 이동 40~ |
| (필드) | 27 ~ +3.0초 | 칼날 파편 회오리 | **H 틱 0.25초 ×12** |

- **데이터:** `GA_PlayerDualBladeAttackData_Ultimate`. `hitPhases[0]` 근접, `[1]` 토네이도 틱(히트스톱 0).
- **히트 형상:** 근접 Sphere r 2.2 Window ×2. 토네이도 `Projectile_DualBladeSignatureTornado`: `Stationary(attachToGround)`, `AreaTick(radius 3.5, interval 0.25)`, `lifetime 3.0`. 정지형으로 확정한다(전진형은 불채택. 판정 위치를 읽기 쉽게).
- **FX:** `vfx_Tornado_01` + 칼날 파편 메시 파티클(바깥으로 퍼짐, 흡인처럼 보이지 않게) + 피격 섬광.
- **클립 후보:** Y1 `Skill_2`, `Frank_RPG_Dual_Skill01_WhirlWind.FBX`.
- **재미:** 틱 총합을 기존 궁극기 기대 피해 이하로 둔다. 흡인은 넣지 않는다.

### 5.6 Staff — 기 모으기 + 에네르기파 빔 (연화·묘묘 공유)

> 카메라: §13.3 Staff · 컷인: §14.3 연화·묘묘(R8, 시전자별 이미지)

**판정: 가능.**

| 기존 자원 | 판단 |
|---|---|
| `Projectile_StaffBeam`(Hitscan 20m, 0.22초, `destroyOnHit 1`, 5개 GA 공유) | 피해 원본으로 재사용하지 않는다. 시전 섬광만 시각 참고 |
| S1 `Staff_Ultimate`(Magic_Attack_2, 오른손 손가락 마스크 레이어) | 기 모으기 포즈 참고. 손가락 레이어는 새 MotionSet에도 유지 |
| `PlayerChargeState` 홀드 차지 | 불채택(고정 길이 확정) |

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 기 모으기 | 0–24 | 지팡이를 앞으로, 구체 성장 | 공격 정렬로 대상 조준 |
| 반동 | 24–27 | 상체가 밀림 | 빔 생성 |
| 빔 유지 | 27–75 | 반동을 버팀(Blender 0.5초 루프 체인) | **H 0.2초 ×8** |
| 종료 | 75–90 | 빔 소멸 | C 회피 78~ |

- **데이터:** `GA_PlayerStaff_Ultimate`. `hitPhases[0]` 틱, `[1]` 마지막 틱(KnockBack).
- **히트 형상:** 0.2초마다 Box `ActorRoot` `FollowDuringWindow` `Window` 0.05초, `(1.6, 1.6, 18)`, `localOffset (0, 1.2, 9.5)`. **벽 관통 판정**(V10)이다. 빔 FX도 충돌로 끊기지 않게 18m 전체를 표시한다(보이는 곳 = 맞는 곳).
- **FX:** `Laser beam 5 magic`(또는 9 ray), `Flash 10 blue laser`, `Hit 10 blue laser`.
- **재미:** 1.6초 고정 자세 → 피격 시 종료, 슈퍼아머 없음.

### 5.7 GreatSword — 거대 검 형상 내려치기 (세라·릴리 공유)

> 카메라: §13.3 GreatSword · 컷인: §14.3 세라·릴리(R8, 시전자별 이미지)

**판정: 가능.**

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 들어올림 | 0–15 | 검을 머리 위 수직으로 | 하늘에 거대 검 출현(디졸브) |
| 정점 홀드 | 15–27 | 발을 넓게 딛음 | 예고(캔슬 없음, E6) |
| 내려침 | 27–33 | 몸 전체로 내리침 | 거대 검 0.2초 하강(V9) |
| 임팩트 | 33 | 거대 검이 땅에 박힘 | **H 33 Box**, **H 36 충격파** |
| 회수 | 33–60 | 검을 뽑아 올림 | C 회피 48~, 이동 52~ |

- **데이터:** `GA_PlayerGreatSwordAttackData_Ultimate`. `hitPhases[0]` 본타(Knockdown, 카메라 흔들림 0), `[1]` 충격파(KnockBack 약).
- **히트 형상:** F33 Box `ActorRoot` `SnapshotOnBegin` `OnceOnBegin` `(3, 3, 9)` `localOffset (0, 1.5, 5.5)`, F36 Sphere r 4 `localOffset (0, 0, 7)`.
- **FX:** GreatSword 무기 메시 ×5 + 발광 재질(출현 F15, 하강 F27–33, 0.5초 잔류 후 분해). Particle 이벤트 구간은 F15–F48로 FX 수명을 덮는다. 착지는 `CFXR2 Ground Hit` + 균열 + 먼지 링.
- **클립 후보:** GS `..._HeavyAttack_3`, G0 `..._Skill_2`, `Grruzam .../M_Big_Sword@Jump_Attack_Combo_*`.
- **재미:** 0.9초 예비 동작은 볼거리다. 홀드 중 캔슬은 게이지 손실이라 열지 않는다(E6).

### 5.8 Whip — 원형 휩쓸기 (확정)

> 카메라: §13.3 Whip · 컷인: §14.3 리안(R8)

**판정: 가능.** 원안과의 비교:

| 기준 | 원안: 바닥 강타 → 파편 방사 | **확정: 원형 휩쓸기 2회 + 전방 마무리** |
|---|---|---|
| 제작 난이도 | 새 쌍 클립(몸 Humanoid + 채찍 Generic 체인)을 Blender로 제작. 최고 난이도 | 기존 쌍 클립 재사용. 후보: `WhipCharacter_Skill_1` + `Whip_Skill_1`(Collision 5개, 0.514~1.745초), `WhipCharacter_HeavyAttack_1~5` + `Whip_Attack_HeavyAttack_1~5`, `WhipCharacter_Skill_Critical` + `Whip_Skill_Critical` |
| 판정 | 파편 투사체는 6m에서 간격 약 3.8m라 틈이 생김 → 동심원 판정 필요 | 휩쓸기 Sphere r 5.5 Window ×2 + 마무리 Capsule 7m. 채찍이 그리는 원 = 판정 원 |
| 재미 | 강한 착지감, 그러나 둔기 느낌 | 채찍 길이 = 판정 반경으로 무기 정체성이 살아남 |
| 위험 | 일정 | 회전 계열 중복 → 몸은 반 바퀴 이내, 채찍이 큰 원, FX는 바깥 테두리에 집중 |

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 감아올림 | 0–12 | 채찍을 머리 위로 원형 회전 | |
| 1차 휩쓸기 | 12–27 | 채찍이 몸 둘레 큰 원 | **H 1차** |
| 2차 휩쓸기 | 27–42 | 반대 방향 원(상체 반 바퀴) | **H 2차** |
| 마무리 | 42–51 | 전방으로 곧게 내리침 | **H 마무리** |
| 회수 | 51–72 | 끝부터 회수 | C 회피 57~, 이동 62~ |

- **데이터:** `GA_PlayerWhipAttackData_Ultimate`. `hitPhases[0]`·`[1]` 휩쓸기(Light + KnockBack 약), `[2]` 마무리.
- **히트 형상:** 휩쓸기 Sphere r 5.5 `FollowDuringWindow` `Window` 약 0.3초, `ShapeCenterToTarget`. 마무리 Capsule 길이 7m 전방 수평 `OnceOnBegin`.
- **매핑:** 몸용·채찍용 MotionSet 2개를 같은 구간·속도·길이로 만들어 각 세트의 `Whip.Ultimate`에 연결한다.
- **FX:** 반경 5.5m 원형 궤적(가운데는 비움), 채찍 끝 트레일(기존 트레일 컨트롤러 조사 후), 마무리 소형 균열.
- **원안의 지위:** 쌍 클립 검수(S2)에서 쓸 수 있는 후보가 하나도 없을 때만 쓰는 대체안이다. 그 경우에도 판정은 동심원으로 하고 파편은 FX로만 쓴다. T3 접촉은 넣지 않는다.

### 5.9 SwordShield — 거대 방패 낙하

> 카메라: §13.3 SwordShield · 컷인: §14.3 묘령(R8)

**판정: 조건부(R3·R4).**

| 구간 | 프레임 | 키포즈 | 표기 |
|---|---|---|---|
| 준비 | 0–12 | 방패를 아래로 끌어내림 | |
| 투척 | 12–18 | 방패를 수직으로 던짐 | 방패 숨김 16~, **SpawnProjectile 16** |
| 체공 | 18–45 | 검을 하늘로 겨눔 | 가드·패리 차단 |
| 착지 | 약 45 | 거대 방패가 지면 강타, 지면 Break | **H 착지(Detonate)** |
| 회수 | 45–60 | 손에 방패 재등장(55) | C 회피 57~ |

- **데이터:** `GA_PlayerSwordShieldAttackData_Ultimate`. `hitPhases[0]` 착지(Knockdown, 큰 강인도 피해, 카메라 흔들림 0).
- **히트 형상:** `Projectile_SwordShieldSignatureFall`: `Arc(arcHeight 8, 고정 비행 시간 0.9)`(고정 시간 모드 enum 이름은 저작 시 확인, 기본값은 `Speed`), `Detonate(radius 4.5, onExpire)`. `TargetPosition` + `projectTargetToGround`, 대상이 없으면 R4(전방 6m).
- **거대화:** 투사체 시각 프리팹에서 처리한다(실제 방패 메시 복제본, 하강 0.5초 동안 ×1 → ×6, 착지 시 시각 반경 4.5m). 몸 모션이나 무기 본으로 하지 않는다(ParentConstraint 스케일 미전달, 이식 지침 §7).
- **방어 충돌:**
  - `interruptActions`에서 Guard 캔슬은 F57 이후로만 연다. Ultimate GA의 `executionGrantedTagIds`로 실행 중 태그를 주고 `ParryCounter`·`Counter` GA의 `blockedTagIds`에 넣는다.
  - 방패 숨김·복구는 R3를 쓴다.
  - 던진 방패는 시전자가 중단돼도 떨어진다(의도된 동작).
- **FX:** 방패 트레일, 정점 `Magic shield holy` 문양, 착지 `CFXR2 Ground Hit` + `Meteor hit 2` + 방사 균열·바위 파편·먼지 링, 재등장 섬광.
- **클립 후보:** M1 `Humanoid_SwordShield_Skill_2`, `Frank_RPG_Warrior_ShieldSkill01·02`, Platform `Sword&Shield_Skill1_Float`·`Skill4_Push`, Kevin `AttackShield01·02`. 하늘 투척 자세는 Blender 1순위다.
- **재미:** 방어 수단이 없는 1.5초는 체공 중 피해 감소 GE(`ownerEffects`, 50%)로 보완하는 것으로 확정한다. 착지 예고 원으로 위치를 읽게 한다.

## 6. 특수 요구 구현 정리

| 요구 | 확정 방식 | 영향 | C# |
|---|---|---|---|
| 순간 대시(Katana) | 루트모션 + 워프 + 따라가는 Window Box + 무적 F12–21 + 잔상 | 일반 공격형(시퀀스 상태는 이동 0) | 없음(R2 보류) |
| 이동 휠윈드(DoubleAxe) | 입력 이동 창 이벤트 | 일반 공격형 | **R1** |
| 점프 투척(Spear) | `AddForceEvent` 상향 + 목표 지향 하향 투사체 | 일반 공격형 | R3·R4 |
| 지속 빔(Staff) | Window Box 반복, 벽 관통 | 일반 | 없음 |
| 지속 장판(Bow·DualBlade) | `Stationary`+`AreaTick` | 시전자와 분리 | Bow는 R4 |
| 소품 분리(Spear·SwordShield) | 슬롯 숨김 + 투사체 시각 프리팹 | 일반 | **R3** |
| 휩쓸기(Whip) | 기존 쌍 클립 체인 + Sphere Window | 몸·채찍 세트 동시 매핑 | 없음 |
| 컷인(9종 12명) | 얼굴 패널 MotionEvent | MotionSet 소유 시점 | **R8** |

## 7. Blender MCP 모션 파이프라인

### 7.1 1판 탐침 결과 (읽기 전용)

Blender 5.2.2 LTS, 저장 안 된 빈 씬(Camera·Cube·Light), 아마추어·액션 0, 씬 FPS 24, `io_scene_fbx` 활성. 이번 확정 작업에서는 추가 조작이 없다.

### 7.2 절차

[NPC 애니메이션 §6](../../../guide/NPC_DAILY_ANIMATION_AUTHORING_GUIDE.md#6-blendermcp-신규-제작)과 [이식 지침 §4~§9](../../../guide/WEAPON_MOTION_RETARGETING_GUIDE.md#4-세-단계의-저작-책임)를 따른다.

1. 대상 확인: `Assets/03.Prefabs/Actor/Player/Models/PlayerModel_<캐릭터>.prefab`의 원본 FBX·Avatar를 GUID로 해석하고, 무기 메시·소켓과 라온 `Weapon_Blade`를 기록한다.
2. 원본 프로필: 리그·축·단위·FPS.
3. 제작 파일: 사용자 씬을 건드리지 않고 `ArtSource/Animation/Signature/<무기>/<무기>_Signature.blend`(신규 예정 경로)와 제작 카드를 쓴다.
4. 저작: 키포즈 → 체중·접지 → 무기 그립. Spear·SwordShield는 해제 이후 손을 비운 포즈를 만든다.
5. 익스포트: `unity-export` 스킬, 선택 아마추어·액션만, 베이크, Leaf Bone 제외.
6. Unity 임포트: `Assets/07.Animations/Player/Signature/<무기>/`(신규 예정 경로), 대상 Avatar, 회전 Bake 정책. 구매 원본·Importer·Avatar는 변경하지 않는다.
7. MotionSet 생성과 §1.1 매핑(Payload와 같은 단위).
8. 검수: Blender 재생·3방향 캡처 → Unity 실제 모델 재생 → `CombatMotionSampler` 120Hz.

**Blender 신규 제작 확정 범위:** Bow 하늘 조준, Spear 투척 해제 구간, SwordShield 하늘 투척, Staff 빔 유지 루프, Katana 발도→대시 연결(라온 무기 본판 포함). DoubleAxe·DualBlade·GreatSword·Whip은 기존 후보로 시작하고, S2 검수 실패 구간만 추가 제작한다.

### 7.3 리타게팅 규칙

| 무기 | 반드시 측정 | 금지 |
|---|---|---|
| Katana | 칼집 입구 횡오차, 칼끝 궤적 | 라온 고정 오프셋(Frank `-X` → 대상 `+Y`)을 아린에 복사 |
| Bow | 시위 손·화살 소켓, 상향 각도 | WeaponAim IK와 동시 제어 |
| Spear | 해제 직전 그립 | 해제 이후 창 본 커브 |
| DoubleAxe | 좌우 손 각각 | 한 손만 맞춤 |
| DualBlade | 교차 순간 칼끝 거리 | |
| Staff | 지팡이 끝, 손가락 레이어 | 손가락 레이어 누락 |
| GreatSword | 보조손 접촉 | 감속으로 판정 창 맞추기 |
| Whip | 몸·채찍 쌍의 구간·속도·길이 일치 | 몸만 재생하고 완성 판정 |
| SwordShield | 투척 직전 방패 면 방향 | 방패 스케일을 본/Constraint로 처리 |

## 8. FX 파이프라인 — `uplayground-vfx` 스킬

### 8.1 스킬 범위

정의 파일은 `.claude/skills/uplayground-vfx/SKILL.md`·`references/project-workflow.md`·`agents/openai.yaml`(Codex 진입점, 호출 예 `$uplayground-vfx`)이다. 1판의 "Codex는 2D 텍스처만, FX 프리팹 파이프라인은 없음" 서술은 정정한다. 첫 조사 때 스킬 목록에 이 스킬이 없었다.

이 스킬이 다루는 범위:

- 전투·환경 FX 프리팹 제작·튜닝
- 기존 연결 경로: `BeginParticleEvent`, `SlashVFXEvent`+`WeaponSlashVfxSpawner`/`SlashVFXPresetSO`, `ActorWeaponTrailController`/`ElementalWeaponTrailLibrarySO`, `IParticleEffectService.RentParticles/ReturnParticles`
- 시각·수명·성능 검증
- 새 회화 텍스처: imagegen 지침

### 8.2 적용 규칙

| 스킬 단계 | 시그니처에서 할 일 |
|---|---|
| 진입 | 필독 문서·Editor 상태 확인, **Unity 슬롯 필요** |
| 제작 계약 | 역할·기준 에셋·부착·**실제 판정 범위**·시작·정점·소멸 프레임을 프롬프트에 명시. 예산은 임시 가정, 성능 합격은 실측 전 보류 |
| 재사용 | 구매 프리팹은 파생본으로만(원본·GUID 보존) |
| 제작 | 예고/본체/잔광 분리. 궁극기 급 밝기는 시그니처에만 |
| 연결 | 기존 MotionEvent만. FX 때문에 판정·캔슬을 바꾸지 않음 |
| 검증 | 정적/로드, 발화·정점·소멸 촬영, 중단·재발화·풀 반환 잔류, 실제 모델 소켓·반대 방향, 같은 씬 FX 유무 비교 |

**스킬 범위 밖이라 전투 데이터 단계에서 처리할 것**

- 투사체 `visualPrefab` 연결(`ProjectileDefinitionSO`)
- 시퀀스 `UltimateTimelineEvent` 연결: 시그니처 FX는 MotionSet 쪽에만 둔다

스킬이 참조하는 `uplayground-motion-skill`·`uplayground-gas-data`는 저장소 `.claude/skills/`에 없다. GAS 저작은 `generate-gas-ability`로 한다.

### 8.3 스킬 호출 프롬프트 초안 (9종)

공통 꼬리말: `판정·피해·캔슬 구간은 바꾸지 말고, 구매 원본은 파생본으로만 수정해. 결과는 Assets/03.Prefabs/FX/Signature/<무기>/에 두고, 발화·정점·소멸을 게임 카메라로 촬영해 검증하고, 사용자 테스트 명세를 남겨줘.`

| ID | 호출 초안 |
|---|---|
| K-1 | `$uplayground-vfx Katana 궁극기 일섬 직선 참격 FX 만들어줘. 기준: Hovl Sword slash VFX/Sword Slash 11 파생. 대시(최대 8m) F15–21 동안 액터를 따라 남는 얇은 직선, F21 정점에서 가장 선명하고 0.3초에 걷힘. 실제 판정은 폭 2.5m·높이 2m Box가 몸을 따라감. Katana_Signature MotionSet에 연결, 라온 Set_Katana와 아린 Katana 세트 둘 다 칼날 소켓 대조.` |
| K-2 | `$uplayground-vfx Katana 궁극기 대시 잔상·발밑 먼지·정점 교차 섬광 만들어줘. 기준: Anime VFX URP VFX_Dash, CFXR4 Sword Hit PLAIN (Cross). 대시 F15–21, 섬광 F21 1회. 기존 AfterimageEvent와 역할 분리.` |
| B-1 | `$uplayground-vfx Bow 궁극기 하늘 상승 화살 FX 만들어줘. 피해 없는 연출, F22 활에서 위로 0.4초 상승 후 사라짐. BeginParticleEvent로 화살 소켓에 연결.` |
| B-2 | `$uplayground-vfx Bow 궁극기 화살비 FX 만들어줘. 기준: Hovl AOE Magic spells Vol.1/Meteor shower 2 구조를 화살 메시로 교체. 반경 4m, 예고 0.6초 후 0.3초 간격 6회, 총 2.4초. Projectile_BowSignatureRain visualPrefab용 프리팹만 만들고 낙하 지점과 반경 4m 일치 확인.` |
| B-3 | `$uplayground-vfx Bow 궁극기 지면 예고 링과 착탄 먼지 만들어줘. 반경 4m, 0.6초 동안 차오르는 링, 착탄마다 CFXR4 Rain Splashes 계열 먼지. 링 텍스처가 없으면 절차 링 우선.` |
| S-1 | `$uplayground-vfx Spear 궁극기 투척 창 비행 FX 만들어줘. 실제 창 메시 복제 + 트레일, 비행 약 0.3초, 하향 궤적. Projectile_SpearSignatureThrow visualPrefab용. 창 길이축과 비행 방향 일치 확인.` |
| S-2 | `$uplayground-vfx Spear 궁극기 착탄 폭발 FX 만들어줘. 기준: Hovl Meteor hit 2 + CFXR2 Ground Hit 파생, 반경 3.5m(Detonate와 동일), 빛기둥 0.3초 + 충격파 링 + 공용 방사 균열 데칼(이 프롬프트에서 제작해 다른 무기와 공유).` |
| S-3 | `$uplayground-vfx Spear 궁극기 손에 창이 다시 나타나는 짧은 섬광 만들어줘. F50, 0.2초, 오른손 무기 소켓.` |
| D-1 | `$uplayground-vfx DoubleAxe 궁극기 휠윈드 FX 만들어줘. 기준: CFXR4 Sword Trail PLAIN (360 Spiral) 파생을 양 도끼에, 회전 15프레임 × 5회, 반경 2.8m 판정과 궤적 크기 일치. 이동 중에도 궤적이 캐릭터를 따라가는지 확인.` |
| D-2 | `$uplayground-vfx DoubleAxe 궁극기 마무리 지면 타격 FX 만들어줘. CFXR2 Ground Hit 파생, 반경 3.5m, F87 1회.` |
| Y-1 | `$uplayground-vfx DualBlade 궁극기 토네이도 FX 만들어줘. 기준: FreeQuickEffectsVol1/vfx_Tornado_01 파생, 반경 3.5m·높이 4m, 3.0초, 칼날 파편 메시 파티클이 궤도를 돌며 바깥으로 퍼짐(흡인처럼 보이지 않게). Projectile_DualBladeSignatureTornado visualPrefab용, 풀 반환 후 잔류 확인.` |
| Y-2 | `$uplayground-vfx DualBlade 궁극기 회전 2회 쌍검 궤적과 토네이도 생성 섬광 만들어줘. 회전 F9–30, 생성 F27, 반경 2.2m.` |
| T-1 | `$uplayground-vfx Staff 궁극기 기 모으기 FX 만들어줘. 지팡이 끝 0.8초 구체 성장. 기준: Hovl AAA Projectiles Vol 1/Flash 10 blue laser. 연화·묘묘 공유이므로 캐릭터별 차이는 넣지 말 것. 손가락 레이어 포즈에서 지팡이 끝 소켓 확인.` |
| T-2 | `$uplayground-vfx Staff 궁극기 에네르기파 빔 FX 만들어줘. 기준: Hovl 3D Lasers Pack/Laser beam 5 magic 파생, 길이 18m·두께 1.6m(판정 Box와 동일), F27–75 1.6초. 판정이 벽을 관통하므로 빔 FX도 충돌로 끊기지 않게 18m 전체 표시. 끝단 Hit 10 blue laser.` |
| G-1 | `$uplayground-vfx GreatSword 궁극기 거대 검 형상 FX 만들어줘. 실제 GreatSword 무기 메시 ×5 + 발광 재질, F15 출현(디졸브), F27–33 0.2초 하강, F33 박힘, 0.5초 잔류 후 분해. 검 폭 2.5m가 판정 Box 폭 3m 안에. Particle 이벤트 구간 F15–48이 FX 수명을 덮게.` |
| G-2 | `$uplayground-vfx GreatSword 궁극기 착지 충격 FX 만들어줘. CFXR2 Ground Hit 파생 + 공용 균열 데칼 + 먼지 링 반경 4m, F33·F36.` |
| L-1 | `$uplayground-vfx Whip 궁극기 원형 휩쓸기 궤적 FX 만들어줘. 기준: CFXR4 Sword Trail PLAIN (360 Spiral)을 반경 5.5m로, 가운데는 비우고 바깥 테두리에 집중. 1차 F12–27, 2차 F27–42 반대 방향. 판정 Sphere r5.5와 일치.` |
| L-2 | `$uplayground-vfx Whip 궁극기 채찍 끝 트레일 조사·연결해줘. 기존 ActorWeaponTrailController와 ElementalWeaponTrailLibrarySO로 채찍 끝 본 궤적이 가능한지 먼저 확인하고 TrailRenderer를 중복 생성하지 말 것.` |
| L-3 | `$uplayground-vfx Whip 궁극기 마무리 전방 채찍질 균열 FX 만들어줘. 전방 7m 직선, F42–51, 공용 균열 데칼 재사용.` |
| M-1 | `$uplayground-vfx SwordShield 궁극기 방패 비행·거대화 FX 만들어줘. 실제 방패 메시 복제, 상승 0.4초(×1) → 하강 0.5초 동안 ×6, 착지 시 시각 반경 4.5m(Detonate와 동일). 정점에 Hovl Magic circles/Magic shield holy 문양. Projectile_SwordShieldSignatureFall visualPrefab용.` |
| M-2 | `$uplayground-vfx SwordShield 궁극기 지면 Break FX 만들어줘. CFXR2 Ground Hit + Hovl Meteor hit 2 파생, 반경 4.5m 방사 균열(공용 데칼), 바위 파편 메시 파티클, 먼지 링.` |
| M-3 | `$uplayground-vfx SwordShield 궁극기 방패가 손에 돌아오는 섬광 만들어줘. F55, 0.2초, 왼손(보조 무기) 소켓, 방패 복구 시점과 일치.` |

## 9. 단계 계획 (향후 구현 시 참조용)

| 단계 | 내용 | 담당 | Unity 슬롯 | 완료 조건 |
|---|---|---|---|---|
| S1 | 기존 Payload·MotionSet·시퀀스·정의 SO 해시 기록 | 디자이너 | 읽기 | 원본 해시 보존 |
| S2 | 후보 클립 실제 모델 검수(9종, Whip 쌍 클립 수평 궤적 포함), 클립 FPS 실측(IV-2) | 아티스트 | 읽기·프리뷰 | 무기별 소스 결정표 |
| S3 | 데이터 골격: Katana·DoubleAxe 궁극기 Payload를 일반 공격형으로 전환(시퀀스 연결 해제), Spear 파생 세트·레이네 정의 SO·`BalanceScenario_Reine`의 세트 참조 변경, 투사체 정의 | 디자이너(`generate-gas-ability`) | 에셋 쓰기 | `AbilityDataValidator` 신규 Error 0 |
| S4 | 일반 공격형 4종 선행: DualBlade·Staff·GreatSword·Whip(Payload와 매핑을 한 단위로) | 디자이너 + 아티스트 | 에셋 쓰기·Play | SG05~SG08 |
| S5 | R1·R3·R4·R8 구현 | 서브 프로그래머 | 컴파일·테스트 | 회귀 + 신규 테스트 |
| S6 | Blender 신규 클립(§7.2 확정 범위) | 아티스트(순차) | 임포트 | Blender·Unity 각각 화면 근거 |
| S7 | 나머지 5종: Katana(아린)·Bow·Spear·DoubleAxe·SwordShield | 디자이너 | 에셋 쓰기·Play | SG01~SG04, SG09 |
| S8 | 라온·화린 전환 마무리: 시퀀스 연결 해제 상태 확인(에셋·잠금 값 보존), 기존 시퀀스 이벤트의 기능(얼굴·슬로·흔들림·Callback)을 R8·카메라 SO로 대체했는지 대조. 이벤트 시각은 **실제 MotionKey로 해석된 모션 길이**로 맞춘다(미리보기 길이 금지, 라온 2.08초 미리보기 vs 5.01초 실제 차이 교훈) | 디자이너 | 에셋 쓰기·Play | §1.5 재테스트 |
| S9 | FX: §8.3 프롬프트 단위 | 아티스트 | 에셋 쓰기 | FX 반경 = 판정 반경 대조표 |
| S9a | 카메라: §13.3 SO 신규·이벤트 연결, 단일 소유 정리 | 디자이너 | 에셋 쓰기·Play | SG12~SG14 |
| S9b | 컷인: 9종 importer Sprite 전환(D3), 정의 SO `ultimateFaceSprite` 지정, R8 이벤트를 9종 시그니처 MotionSet에 배치(Katana는 라온 `Set_Katana`용과 아린용 MotionSet 각각 1개씩, 중복 없음), 유라는 이미지가 들어오면 정의 SO만 지정 | 디자이너 | 에셋 쓰기·Play | §14.4 대조, SG15~SG16 |
| S10 | 독립 검증 | QA 검토자 | 읽기 | 체크리스트 근거 |

### 9.1 검증 준비표

| 항목 | 방법 | 근거 | 구분 |
|---|---|---|---|
| Ability 정합성 | `AbilityDataValidator` 전수(대체 목록·파생 세트 포함) | 신규 Error 0 | 자동 |
| 키 해석 | 실제 모델 × 키 → 새 MotionSet GUID(라온 `Set_Katana`, 아린, 레이네 `Spear.Ultimate`, 리안 몸·채찍 세트) | 해석 결과 | 자동 |
| 궁극기 경로 | 12명(H09 제외) 각각 `RequestUltimate` = Started(아린 포함) | PlayMode | 자동 |
| 공유 세트 보호 | Default 세트 소비자(Staff 플레이어 세트, 몬스터 3종, Common 세트)의 Ultimate 해석이 변경 전과 동일 | 해시·해석 비교 | 자동 |
| 판정·FX 크기 | 판정 기즈모 vs FX 최종 반경 | 캡처 | 자동+화면 |
| 타이밍 | 120Hz 샘플링 | 측정 JSON | 자동 |
| 카메라 단일 소유 | 슬로 소유자 1곳, 대형 페이즈 reactionProfile 흔들림 0, 회전 ≤15°·Roll ≤0.8°·슬로 누적 ≤0.15초 | 값 대조표 | 자동 |
| 컷인 대조 | 정규 12명 × 정의 SO 필드 × 이미지 × importer × 표시 트리거. 누락은 유라 1명(이미지 미완성 사유) | 대조표 | 자동 |
| 컷인 원본 보존 | PNG 해시 불변, `.meta` GUID 불변(importer 필드만 변경) | 해시·diff | 자동 |
| 체감·미술 | §10 | 회신 | 사용자 |

### 9.2 구현 단계 검증 (PlayMode, 코드 읽기로 닫을 수 없는 항목)

| ID | 확인 | 기대 | 실패 시 조치 |
|---|---|---|---|
| IV-1 | 창·방패를 숨긴 상태에서 스왑·사망·씬 이탈 | R3로 무기가 다시 보임 | R3 복구 경로 보완 |
| IV-2 | 후보·신규 클립의 실제 FPS와 판정 프레임 | §5 프레임표와 ±1프레임 | 프레임표 갱신(판정 시각은 MotionSet 기준) |
| IV-3 | 장판 투사체(화살비·토네이도·방패) 중 스왑·사망·씬 이탈 | 스왑 후 끝까지 유지, 사망·씬 이탈 때 정리 | 투사체 정리 정책 보완 |
| IV-4 | Katana 스윕이 여러 적에게 맞을 때 대시 끊김 | 히트스톱 없이 끝까지 대시, F21에만 정지감 | reactionProfile 값 재조정 |
| IV-5 | Spear `AddForceEvent` 9m/s + 공격 중력 배율 0.3 도약 높이 | 2.5~3m | force 값 조정(상한 12) |
| IV-6 | 9종 시그니처를 피격·회피·스왑·사망으로 중단 | R8 패널 닫힘, 카메라 효과·TimeScale 해제(`CameraEffectEvent.OnCompleteEvent`, `TimeScaleCameraEffect.ForceDispose`), 남은 잠금 없음 | 이벤트 종료 경로 보완 |
| IV-7 | `Bokusei_KatanaAnimationSet` 소비자 재확인 | 정의 SO 참조 없음 유지 | 소비자가 있으면 같은 매핑 교체 |

## 10. 사용자 테스트 항목

CombatTestMap에서 게이지를 채운 뒤 궁극기를 사용한다. 회신 형식은 `ID / 통과·실패·미수행 / 증상 / 영상`이다. 구현 후 §1.5 재테스트도 함께 받는다.

| ID | 조작 | 기대 결과 | 실패 기준 |
|---|---|---|---|
| SG01 | 라온·아린 Katana: 일렬의 적 3마리, 대상 없이 | 둘 다 발동, 지나온 직선의 적 전원 피격·리액션 | 아린 미발동, 직선 밖 피격, 적에 막혀 제자리, 리액션 미표시 |
| SG02 | 설아 Bow: 락온/비락온, 이동하는 적 | 비락온이면 전방 12m에 장판, 원 안에서만 피해, 발사 후 회피 가능 | 원 밖 피격, 발밑 장판 |
| SG03 | 레이네 Spear: 평지·경사·벽 앞 | 2.5~3m 도약, 착탄 폭발, 창이 손에 다시 보임 | 창 사라진 채 남음, 공중 정지 |
| SG04 | 화린 DoubleAxe: 회전 중 이동, 적 무리 | 느린 이동 회전, 2.5초 후 종료, 적이 움직이고 반응함 | 무한 경직, 조작 불가, 적 정지 |
| SG05 | 유라 DualBlade: 생성 후 이탈·스왑·사망 | 3초 지속, 원 안만 틱, 스왑 후 유지 | 영구 잔류, 오류 |
| SG06 | 연화·묘묘 Staff: 일직선 적, 벽 뒤 적 | 18m 관통, 빔이 벽을 관통해 보이고 벽 뒤 적도 맞음. 두 캐릭터 동일 | 빔 밖 피격, 빔은 벽에서 끊겼는데 벽 뒤 피격 |
| SG07 | 세라·릴리 GreatSword: 정면·측면 적 | 거대 검 자리 = 피해 위치, 두 캐릭터 동일 | 검 폭보다 넓은 피해, 타이밍 어긋남 |
| SG08 | 리안 Whip: 근접·5m 적, DoubleAxe·DualBlade와 번갈아 | 채찍 원 테두리 피격, 회전 3종이 실루엣으로 구분 | 원 밖 피격, 3종 구분 안 됨 |
| SG09 | 묘령 SwordShield: 체공 중 가드·패리 입력, 피격, 회피 | 체공 중 가드 불가, 피해 감소 체감, 착지 후 방패 복귀 | 방패 없는 가드, 방패 영구 숨김 |
| SG10 | 9종 각 10회 | 섬광·흔들림 피로 없음, 무게감 차이 | 명중 정보 소실 |
| SG11 | 라온·화린 시그니처(시퀀스 → 일반 공격형 전환) | 패널이 한 번만 짧게 나오고 물러남, 시전 중 적이 움직이고 반응함, 종료 후 카메라·시간 복구 | 패널 두 번 표시, 패널이 임팩트를 덮음, 슬로 잔류 |
| SG12 | 카메라 — 락온/비락온에서 9종 | 틸트·줌이 락온 프레이밍과 싸우지 않고 종료 후 복귀 | 떨림·튐, 대상 화면 밖, 각도 고정 |
| SG13 | 카메라 — 적 5마리·큰 적·벽/기둥·경사 | 시전자·무기·판정 영역이 화면 안, 가림은 페이드 | 착탄점 이탈, 큰 적에 가림, 발밑 구도 |
| SG14 | 카메라 — DoubleAxe 이동 중 카메라 조작, 9종 연속 10회 | 조작 방해·멀미 없음 | 조작 지연, 어지러움, 슬로가 답답함 |
| SG15 | 컷인 — 12명 각각 | 12명은 자기 이미지가 좌측 패널로 짧게 나옴. 세라/릴리·연화/묘묘는 각자 이미지. 이미지가 없는 정의도 패널 없이 정상 발동 | 다른 캐릭터 이미지, 깨짐·늘어남, 유라 오류 |
| SG16 | 컷인 — 표시가 짧은 라온·아린·연화·묘묘, 히트스톱·슬로·일시정지 중 | 대표 임팩트 전에 사라짐, 슬로 중에도 실시간 진행, 일시정지 중 패널만 정리 | 패널이 임팩트를 가림, 늘어짐, 얼어붙음 |

## 11. C# 요구사항 최종 판정

| ID | 요구 | 영향 | **판정** | 근거 |
|---|---|---|---|---|
| R1 | 공격 상태에서 MotionSet "입력 이동 창" 이벤트 구간 동안 입력 이동(속도 배율·회전 속도) 합산 | DoubleAxe | **채택** | 요구의 핵심. 시퀀스 경로를 쓰지 않으므로(§1.2) 공격 상태만 대상 |
| R2 | 대시 중 적 캡슐과의 이동 충돌 무시 + 종료 시 겹침 해소 | Katana | **보류** | 대상 앞 정지형으로 출시. 관통이 체감상 필요하다는 사용자 테스트 결과가 나오면 재검토 |
| R3 | 장착 슬롯(Main/Sub) 단위 숨김, 상태 종료·중단·스왑·사망 시 무조건 복구 | Spear, SwordShield | **채택** | 이름 기반 숨김은 조용히 실패할 수 있고 스왑·사망 경로가 미보장(V2) |
| R4 | 플레이어 투사체 목표 위치 폴백: 공격 보조 대상 → 없으면 전방 N m 지면(N은 이벤트 데이터) | Bow, Spear, SwordShield | **채택** | 대상이 없으면 장판이 시전자 위치에 생김(V3) |
| R5 | 형상 판정 장애물 차단 | Staff | **폐기** | 벽 관통을 설계로 확정하고 FX도 관통 표현으로 맞춤(V10) |
| R6 | 지연 절단(리액션 지연) | Katana | **폐기** | F21 슬로·흔들림으로 대체 |
| R7 | 공유 GA의 캐릭터별 시퀀스 선택 | Katana | **폐기** | 시퀀스 경로 미사용, Katana 궁극기를 일반 공격형으로 전환(E2) |
| R8 | MotionSet용 "궁극기 얼굴 패널" MotionEvent(시전자 정의 SO의 `ultimateFaceSprite` → `Svc.FaceStrip`, 정지 시 닫기, 몬스터 Ignored, sprite가 null이면 아무것도 하지 않음) | 9종 전부(12명) | **채택** | 일반 공격형 궁극기에는 패널 경로가 없고, 9종 모두 일반 공격형으로 확정 |
| R9 | 카메라가 현재 대상·월드 지점(착탄점) 주시 | Bow, Spear, SwordShield | **보류** | 정렬된 정면 + 거리 확보로 출시. SG13 실패 시 재검토 |
| R10 | 카메라 효과의 락온 조건 생략 | — | **폐기** | 연출 회전은 포즈에만 합성되고 락온 안정 시야 계산에서 제외됨(V16). LookAt은 미사용(E5) |
| R11 | 컷인 Texture2D UI 경로 | — | **폐기** | D3 허용으로 불필요 |

## 12. 위험

모든 V 항목은 §3.2에서 해소했거나 설계로 확정했다. PlayMode로만 확인할 수 있는 것은 §9.2로 분리했다. 남은 위험은 다음과 같다.

- **아린 궁극기는 현재 실행되지 않는다**(V12, 코드 근거). 이 설계와 무관한 기존 결함이다. E2 데이터 변경으로 함께 해소한다.
- **기존 연출 재인수:** 라온·화린의 잠금·모션·hold가 바뀌므로 §1.5 테스트를 다시 받아야 한다.
- **공유 세트 보호:** Default 세트(Spear 원래 소속)는 Staff 플레이어 세트와 몬스터 3종이 함께 쓴다. 파생 세트로 분리하고 §9.1 "공유 세트 보호" 검사로 확인한다.
- **회전 계열 중복:** DoubleAxe·DualBlade·Whip. Whip 규칙(몸 대신 채찍 원)과 카메라 차별화(§13.3 Whip 부감)로 구분하고 SG08로 확인한다.
- **판정·FX 크기 불일치:** §8.3 프롬프트마다 판정 값을 넣었다.
- **Staff 벽 관통:** 설계상 허용이다. SG06에서 시각과 판정이 일치하는지 본다.
- **수치:** 피해·강인도는 S3에서 기존 궁극기 기준으로 정한다.

## 13. 카메라 연출

### 13.1 재사용하는 것과 쓰지 않는 것

| 수단 | 위치 | 판단 |
|---|---|---|
| `CameraEffectEvent` | `MotionEvent_CameraEffect.cs` | **사용.** SO 목록을 구간 동안 재생, `lockCameraInput` 선택 |
| `CameraEffectData` 하위 SO | `Camera/Data/Effects/` | **사용.** `FOV`(fovDelta), `Zoom`(distanceDelta·offsetDelta), `Rotation`(yaw/pitchDelta 상대값, **양수 = 내려다봄**, V14), `Shake`(`CameraShakeData`), `TimeScale`(`useHitStopManager=true`, duration이 지속 소유, V15). 공통 duration·blend·priority·`useUnscaledTime` |
| `CameraShakeData` | `Camera/Data/CameraShakeData.cs`, 에셋 `Assets/10.Datas/Camera/CameraShake/` | **사용.** Pitch/Yaw/Roll 진폭(Roll 기본 0, "멀미 유발" 주석), Perlin |
| 포즈 합성 | `EffectRotationInjectCameraModifier.cs:5-28` | 연출 회전·거리·피벗은 포즈에만 합성되고 지속 상태에 누적되지 않는다 → 락온과 충돌하지 않음(V16) |
| `CameraLookAtSocketEvent` | `MotionEvent_CameraLookAtSocket.cs` | **사용 안 함(E5).** LookAt override 중에는 락온 안정 시야 갱신이 멈춘다(`InGameCameraBehavior.cs:46-47`) |
| `CameraSnapshotSequenceEvent` / `CameraSnapshotProfile` | | **사용 안 함.** 구도 회귀 이력, 주시 대상이 고정 액터 ID뿐 |
| 궁극기 시퀀스 이벤트·`cameraProfile` | `UltimateTimeScaleEvent` 등 | **사용 안 함.** 시퀀스 경로 미사용(§1.2) |
| 타격 자동 카메라 | `reactionProfile`(`AttackData.cs:108-113`로 소비) | 대형 페이즈는 `manualOverride` 흔들림 0, 틱 페이즈는 히트스톱 최소 |
| 상시 Modifier(LockOn·Encounter·HitAssist·Collision 등) | `Camera/Modifiers/` | 손대지 않음. 연출은 상대값으로 그 위에 얹는다 |
| 가림 페이드 | `CameraOcclusionFader`, `CameraOccluderNameRules.asset` | 자동 처리, 스킬별 저작 없음 |

착탄점 주시(R9)는 보류, 락온 조건(R10)은 폐기다. 창·방패는 공격 속도에 따라 비행 시작 시각이 달라지므로 실제 착탄 FX의 `CameraEffectOnSpawn` 어댑터가 기존 카메라 효과 SO를 재생한다. 어댑터는 자기 효과 핸들만 정리하며 카메라 모듈 경계([이식 가이드](../../../guide/CAMERA_MODULE_PORTABILITY_GUIDE.md))는 유지한다.

### 13.2 공통 규칙

- **저작 위치:**
  - 효과 SO: `Assets/10.Datas/Camera/CameraEffect/Signature/<무기>/`
  - 흔들림: `Assets/10.Datas/Camera/CameraShake/Signature_<무기>_<용도>.asset`(신규 예정 경로)
  - 연결: 각 시그니처 MotionSet의 `CameraEffectEvent`
  - 액터·스테이지 이름 분기 없음
- **단일 소유:**
  - 슬로: 9종 모두 `TimeScaleCameraEffectData(useHitStopManager=true)` 한 곳.
  - 흔들림: 대형 임팩트는 이벤트 1개, 해당 페이즈 reactionProfile 흔들림 0.
- **시간 기준:** TimeScale 효과와, 그와 겹치는 흔들림·FOV 킥은 `useUnscaledTime=true`로 둔다. 표의 슬로 시간은 실시간 기준이다.
- **멀미·가독성 상한:** 회전 ≤15°, Roll 노이즈 ≤0.8°, FOV 킥 ≤+10, 슬로 누적 ≤0.15초. 틱 타격은 매 틱 흔들림 대신 저진폭 연속 노이즈 1개를 쓴다.
- **조작:** `CameraEffectEvent.lockCameraInput`은 아래 표에서 지정한 구간에서만 켠다.
- **컷인과의 순서:** 얼굴 패널은 시전 0초에 좌측에 들어와 대표 임팩트 0.1초 전까지 사라진다(§14.3). 대표 임팩트 이전의 작은 판정과 겹치는 것은 허용한다.

### 13.3 무기별 카메라 설계

| 무기 | 구간 | 효과(SO·값) | 플레이어 조작 | 락온·다수 적·가림·멀미 |
|---|---|---|---|---|
| **Katana** 대시 추적 | F0–12 | `Zoom` −0.8 + `FOV` −4 | `lockCameraInput` F0–24(라온·아린 공통) | 회전 효과 없음 |
| | F15–21 | `FOV` +8(blendIn 0.05) + `Zoom` +1.2 | 이동 불가(공격 상태) | 스윕 히트스톱 0(§5.1). 추종 지연으로 화면 이탈 시 Zoom +2까지 |
| | F21 | `Shake` `Signature_Katana_Slash`(Pitch 1.5·Yaw 1.0·0.15초·DirectionalBias 0.7, unscaled) + `TimeScale` 0.3배 0.12초(unscaled, 라온·아린 공통) | 〃 | 일섬 페이즈 흔들림 0 |
| | F24–51 | 전부 blendOut 0.3 | F36부터 회피 | — |
| **Bow** 하늘 틸트 → 장판 | F0–22 | `Rotation` pitchDelta **−12**(올려다봄) + `FOV` +5 | 카메라 허용 | 회전은 포즈 합성이라 락온 안전 |
| | F22–40 | `Rotation` blendOut 0.4 + `Zoom` +2.0(반경 4m 장판) | F30 회피 | 측면 대상은 화면 가장자리에 보일 수 있음(R9 보류) |
| | F40 | `Shake` LiteHit 1회, 이후 틱은 흔들림 없음 | 자유 | 2.4초 연속 흔들림 금지 |
| **Spear** 점프 추적 → 착탄 줌 | F9–27 | `Zoom` +1.5 + `Rotation` pitchDelta **−6**(위로 따라감) | 공중 조작 불가 | 경사·천장은 Collision Modifier가 처리 |
| | F30 | `FOV` −6 0.1초 | 〃 | — |
| | 약 F39 | `Shake` `Signature_Spear_Impact`(Explosion 계열) + `FOV` +8 + `TimeScale` 0.25배 0.08초 + `Zoom` −1.0 0.2초(모두 unscaled) | 〃 | 슬로는 이 한 번만 |
| | F48–60 | blendOut | F52 회피 | — |
| **DoubleAxe** 약한 연출 | F12–87 | `FOV` +4 유지 + 연속 노이즈 `Signature_DoubleAxe_Spin`(Pitch 0.2·Yaw 0.3, 저주파) | **이동·카메라 조작 가능**(카메라 잠금 없음) | 회전 효과 금지, 회전마다 흔들림 금지. 다수 적은 EncounterFraming |
| | F87 | `Shake` MediumHit | 〃 | — |
| **DualBlade** 미세 롤 | F9–30 | `Shake` `Signature_DualBlade_Spin`(Roll 0.6°·Yaw 0.4·0.7초) + `FOV` +6 펄스 | 카메라 허용 | Roll은 노이즈만 |
| | F27 | `Zoom` +1.5 + `Rotation` pitchDelta **−4**(높이 4m 토네이도) | 〃 | — |
| | F36–45 | blendOut 0.4 | F36 회피 | 토네이도 3초 동안은 기본 카메라 |
| **Staff** 차징 줌인 → 빔 | F0–24 | `Zoom` −1.2, offsetDelta (0.4, 0, 0)(우측 어깨), `FOV` −6 | `lockCameraInput` F0–27(정렬 유지) | 빔 방향은 공격 정렬된 시전자 정면 |
| | F24–27 | `FOV` +10 킥 0.15초 + `Shake` Explosion 저진폭 | 〃 | — |
| | F27–75 | 연속 노이즈 `Signature_Staff_Beam`(Pitch 0.3) + `Zoom` +1.5(18m 빔) | 카메라 허용 | 카메라를 돌려도 판정 불변 |
| | F75–90 | blendOut | F78 회피 | — |
| **GreatSword** 로우앵글 → 내려침 | F0–27 | `Rotation` pitchDelta **−10**(올려다보는 완만한 로우앵글) + `Zoom` −0.5 | `lockCameraInput` F0–33 | 컨셉 §2.3: 극저각·발밑 금지 → −10에서 멈춤 |
| | F33 | `Shake` `Signature_GreatSword_Slam`(HeavyHit 상위) + `FOV` +8 + `TimeScale` 0.25배 0.1초(unscaled) | 〃 | 본타 흔들림 0 |
| | F36–60 | 전부 blendOut 0.4 | F48 회피 | 큰 적은 Zoom으로 거리 확보 |
| **Whip** 광역 와이드 | F0–45 | `Zoom` +2.5 + `Rotation` pitchDelta **+8**(약한 부감) | 카메라 허용 | 회전 계열 중 유일한 부감·와이드 |
| | F42–51 | `Shake` MediumHit | 〃 | — |
| | F51–72 | blendOut 0.5 | F57 회피 | — |
| **SwordShield** 상승 틸트 → 착지 | F12–30 | `Rotation` pitchDelta **−15**(방패 상승 추적) | `lockCameraInput` F12–45 | 방패가 위로 이탈하면 FOV +5 보조 |
| | F30–45 | `Rotation` 원복 0.3초 + `Zoom` +2.0(반경 4.5m) | 〃 | 착지점은 R4 폴백으로 정면 |
| | 약 F45 | `Shake` `Signature_SwordShield_Break` + `FOV` +6 + `TimeScale` 0.2배 0.1초(unscaled) | 〃 | 착지 페이즈 흔들림 0 |
| | F45–60 | blendOut | F57 회피 | — |

## 14. 스킬 컷인(얼굴 패널) 연결

### 14.1 현재 연결 실태

런타임 경로는 다음과 같다.

```text
PlayerCharacterDefinitionSO.ultimateFaceSprite (Sprite)
→ UltimateFaceStripEvent (UltimateSequenceAsset.events)
   faceOverride가 없으면 Svc.Party.GetCharacterDefinition(caster).ultimateFaceSprite, null이면 반환
   (UltimateFaceStripEvent.cs:28-32)
→ Svc.FaceStrip → UIFaceStrip (Image, DOTween SetUpdate(true))
```

| 항목 | 결과 |
|---|---|
| 이미지 경로를 직접 참조하는 코드 | `Assets/Tests/PlayMode/UI/FaceStripPlayModeTests.cs` 한 곳(테스트). 런타임은 정의 SO 필드로 참조 |
| 정의 SO 연결 | 라온·화린만 지정. 나머지 미지정 |
| 표시 이벤트 | `UltimateSequence_Raon`(hold 1.4)·`_Hwarin`(hold 1.2)에만 있음 |
| importer | 라온·화린 Sprite(`textureType 8`), 나머지 9종 Default(`textureType 0`) |
| 일반 공격형 궁극기 | 패널 경로 없음 → R8 |

### 14.2 연결 방식 (확정)

1. **importer(D3 허용):** 9종의 `textureType`을 Sprite, `spriteMode`를 Single로 바꾼다. GUID는 유지하고 PNG는 수정하지 않는다. 바꾼 뒤 9개 정의 SO의 `ultimateFaceSprite`를 지정한다.
2. **표시:** 12명 모두 R8 이벤트로 표시한다. 시퀀스 연결을 해제하므로 기존 `UltimateFaceStripEvent`는 실행되지 않고, 패널이 두 번 뜨지 않는다. R8은 각 시그니처 MotionSet에 하나씩 둔다. Katana는 라온(`Set_Katana`)용과 아린용 MotionSet에 각각 1개다. 공유 세트도 시전자 정의로 이미지가 갈리므로 캐릭터 분기가 없다.
3. **UI 규칙:** [UI/UX 저작 지침](../../../guide/UI_UX_AUTHORING_GUIDE.md)을 따른다. 기존 `UIFaceStrip`은 `SetUpdate(true)`이고 좌측 패널이다. 플레이어 노출 텍스트는 추가하지 않는다.
4. **이미지 미완성 규칙(D4):** 정의 SO의 `ultimateFaceSprite`가 null이면 패널을 표시하지 않고 궁극기는 정상 실행한다(기존 이벤트 `UltimateFaceStripEvent.cs:31-32`와 R8이 같은 규칙). 유라 이미지가 들어오면 정의 SO 필드만 지정한다. MotionSet·코드는 바꾸지 않는다.

### 14.3 캐릭터별 대장과 타이밍

패널 표시 시간 = `inDuration` + `hold` + `outDuration`. `faceDelay` 0.04초는 in 내부 지연으로 전체 시간에 더하지 않는다. **패널 종료 ≤ 대표 임팩트 − 0.1초**가 되게 한다.

| 캐릭터 | 무기 | 이미지 | importer(확정 후) | 표시 트리거 | 대표 임팩트 | in / hold / out | 패널 종료 | 순서 |
|---|---|---|---|---|---|---|---|---|
| 라온 | Katana | `CutIn_Raon.png` | Sprite(현행) | R8(기존 시퀀스 이벤트 대체) | F21 0.70초 | 0.12 / 0.25 / 0.15 | 0.52초 | 패널+줌인 → 0.5초 대시(퇴장 중) → 0.7초 일섬 |
| 아린 | Katana | `CutIn_Arin.png` | Sprite | R8 | 0.70초 | 0.12 / 0.25 / 0.15 | 0.52초 | 라온과 동일 |
| 화린 | DoubleAxe | `CutIn_Hwarin.png` | Sprite(현행) | R8(기존 시퀀스 이벤트 대체) | 마무리 F87 2.90초 | 0.18 / 0.8 / 0.2 | 1.18초 | 패널 → 0.4초 회전(조작 가능) → 2.9초 마무리 |
| 묘령 | SwordShield | `CutIn_MyoRyeong.png` | Sprite | R8 | 착지 1.50초 | 0.18 / 0.6 / 0.2 | 0.98초 | 패널 → 0.53초 투척 → 1.5초 착지 |
| 레이네 | Spear | `CutIn_Reine.png` | Sprite | R8 | 착탄 1.30초 | 0.18 / 0.5 / 0.2 | 0.88초 | 패널 → 도약 → 1.0초 투척 → 1.3초 착탄 |
| 리안 | Whip | `CutIn_Lian.png` | Sprite | R8 | 마무리 1.40초 | 0.18 / 0.6 / 0.2 | 0.98초 | 패널 → 0.4초 첫 휩쓸기(겹침 허용) → 1.4초 마무리 |
| 설아 | Bow | `CutIn_SeolA.png` | Sprite | R8 | 첫 낙하 1.33초 | 0.18 / 0.5 / 0.2 | 0.88초 | 패널+하늘 틸트 → 0.73초 발사 → 1.33초 화살비 |
| 세라 | GreatSword | `CutIn_Sera.png` | Sprite | R8 | F33 1.10초 | 0.18 / 0.5 / 0.2 | 0.88초 | 패널+로우앵글 → 1.1초 임팩트 |
| 릴리 | GreatSword | `CutIn_Lili.png` | Sprite | R8 | 1.10초 | 0.18 / 0.5 / 0.2 | 0.88초 | 세라와 동일 |
| 연화 | Staff | `CutIn_YeonHoa.png` | Sprite | R8 | 빔 F27 0.90초 | 0.12 / 0.35 / 0.15 | 0.62초 | 패널+차징 줌인 → 0.9초 빔 |
| 묘묘 | Staff | `CutIn_Myomyo.png` | Sprite | R8 | 0.90초 | 0.12 / 0.35 / 0.15 | 0.62초 | 연화와 동일 |
| 유라 | DualBlade | `CutIn_Yura.png` | Sprite | R8 | 토네이도 F27 0.90초 | 0.12 / 0.35 / 0.15 | 0.62초 | 다른 캐릭터와 같은 경로로 표시 |

### 14.4 누락 대조

| 대조 | 결과 |
|---|---|
| 정규 사용 12명(H09 제외) vs 이미지 12종 | 누락 0. 향후 Sprite가 null이면 D4 규칙으로 표시만 생략 |
| 이미지 중 시그니처 대상 밖 | 0. 12종 모두 9무기 소속. 라온·화린도 D1로 시그니처 대상 |
| H09 | 정의 SO는 있으나 이미지 없음. 정규 스토리 미사용으로 대상 제외 |
| 이름 | 파일명 `CutIn_<캐릭터>`이 정의 SO 파일명과 일치 |

## 15. 구현 시 실제 코드·에셋 대조로 확정한 보정

- Spear 원래 Default 궁극기는 hitPhases가 0개다. 신규 단일 착탄은 GreatSword 원본 단일타 damage 141 및 poise·break 세 값만 초기 기준으로 사용한다. SourcePayload_Spear는 변경 없이 감사용으로 보존한다. 기존 피해 합 보존으로 판정하지 않는다.
- 실제 중력 -30·상승 배율 1.5에서 7.5m/s는 약 0.625m다. 도약은 9m/s와 별도 AttackGravityEvent 배율 0.3으로 시작하고 IV-5에서 실측 조정한다.
- Spear 투사체는 거리별 착탄 카메라 어긋남을 막도록 고정 0.3초 Arc(arcHeight 0)로 저작한다. Spear·SwordShield는 비행 접촉 피해를 생략하고 지정 지점에서만 Detonate한다. Bow는 대상당 최대 6회, DualBlade 장판은 최대 12회로 수명 경계 중복을 막는다.
- 대검 형상은 TimedPropEvent의 모션 시계로 F15 출현·F27~33 하강·F48 제거를 제어한다. 실시간 파티클에 하강을 맡겨 공격 속도와 어긋나는 경로는 쓰지 않는다.
- 유라 CutIn_Yura.png는 이미 제작됐다. 12명 모두 정의 SO에 연결하며 null Sprite 시 표시 생략 계약은 유지한다.
- 검방패 보호 ownerEffects는 55f/30초 고정 지속이며 교대 시 제거한다. 피격 중단 시에도 남은 짧은 보호시간은 지속한다. 방패 없는 동안 반격 차단은 실행 태그가 소유한다.
- Blender에서 다시 내보낸 Frank 리그는 Generic/Humanoid의 Armature 래퍼 처리가 다르다. 신규 FBX에만 명시 Human 매핑·원본 T포즈 회전 보정·신규 bind 길이를 적용한 독립 Avatar를 만들고 실제 모델로 검수한다. 구매 원본 Avatar는 수정하지 않는다.

### 15.1 실제 모델 루트·접지 보정 판정

12명 120Hz 샘플에서 Katana 옆축 이동, DoubleAxe 21.74m·DualBlade 5.27m 원본 전진, Spear 몸 높이 잔류를 발견했다. 원본과 공용 런타임은 보존하고 별도 native 파생 클립을 저작한다. Katana는 추출 좌표계·모델 humanScale을 반영해 F15~21 정면 8m로 한정한다. DoubleAxe·DualBlade는 평면 루트를 제거하고 몸 회전 포즈는 보존한다. Spear는 매시각 낮은 발을 0.08m에 두는 공통 RootT.y 보정만 적용하며 근육·RootQ·골반에서 관절까지 상대벡터를 보존한다. KCC가 도약 높이를 소유한다. 수치 검증과 실제 그립·자연스러움 인수는 구분한다. 적용·재측정 판정은 작업 상태를 따른다.

### 15.2 FX 충실도 감사 후 보강

초기 구매 파생만으로 부족했던 발광·디졸브/분해, 투척 트레일·방패 문양·착탄 예고, 복귀 손 섬광, 양손/채찍 끝 궤적, 외향 칼날·바위·먼지·균열을 전용24개 프리팹에 연결했다. 대검 소멸은 TimedPropEvent의 선택적 MPB 곡선이 모션 시계로 제어하고, 방패 착탄 예고는 확정 투사체 좌표에 생성해 회수·사망·풀 반환·비활성화 때 정리한다. 기존 데이터는 선택 필드 null 기본값으로 보존한다. Katana 측정은 전방7.99946m·횡편차약9.3cm이며 Astra가 허용오차로 채택했다. 검사 결과와 캡처·남은 사용자 인수는 [현재 상태](../../../../../docs/work-status/weapon-signature/STATUS.md)에 기록한다.
