# 모션워핑 개선 설계

## 2026-10-17 Bokusei 기본 공격 접근 보정 보완

**Bokusei 기본 1·2타의 추가 이동을 0으로 유지했던 정책을 변경했다.** 원본 루트 이동이 작다는 사실은 접근 보정을 금지할 이유가 아니다. 작은 원본 이동에 비례한 예산 안에서 목표 방향으로 보정하며, 도착에 필요한 거리가 예산을 넘더라도 일부 접근을 허용한다. 아래 교체 구현 절의 `translationWeight=0 보존`은 이 보완 이전의 기록이다.

`MotionWarpCorrectionLimitPolicy`를 창 설정과 MotionWarp 이벤트에 추가했다. 기본값 `Reject`는 기존 에셋의 전체 거절 정책을 유지하고, 명시적 `Clamp`는 목표 끝점과 원본 끝점의 차이를 `min(최대 추가 거리, 원본 순이동 × 최대 비율)` 안으로 제한한다. 최소 원본 이동·순이동/경로 비율·최대 각도 검사는 그대로 적용하므로 이동 없는 클립이나 왕복 동작에 전진을 만들지 않는다. 제한된 경우 `CorrectionLimited`를 기록하되 원래 접촉 목표는 진단에 남겨 도착 성공으로 오인하지 않게 한다.

적용 대상은 Bokusei `Katana_Combo_Attack_1_1_PlayerGrounded_Bokusei`와 `Katana_Combo_Attack_1_2_PlayerGrounded_Bokusei` 두 MotionSet이다. `translationWeight=1`, Clamp, 최대 0.15m·원본 순이동 100%, 거리 2m, 보정 속도 1.5m/s, 이동 종료 여유 0.015초다. 리그에 맞는 프로필을 다시 준비했다. 실제 예산은 1타 약 9.56cm/2타 약 5.74cm이며, 남은 거리를 끝 프레임에 몰아넣지 않는다. 회전 상한 20도, 창 시간, 타격·캔슬·VFX·클립·GUID는 보존했다. 같은 Bokusei 테이블에서 해당 MotionSet을 재사용하는 진입·교체 MotionKey에도 적용된다.

관련 EditMode 96/96, GAS Editor PlayMode 8/8 통과. 신규 수학·에셋 검사는 거리/비율 예산, 0 예산, 무이동·왕복·각도 거절, 가까운 목표 초과 방지, 두 실제 에셋의 리그 프로필과 타격 전 종료 설정을 검증한다. 새 콘텐츠를 포함한 Windows Player에서 기본 1타 추가 이동 약 8.9cm, 콤보 2타 약 5.6cm를 측정했다. 실제 포즈 시간과 세션 기준으로 타격 확정 이후 보정 및 충돌 후 밀린 보정 0을 확인했다. 전체 회귀 15개 실패·별도 전투 데이터 오류는 이번 범위에서 수정하지 않았다. 실행 조건, 공유 참조, 보관 산출물과 시각 검증 한계는 [전투 테스트 기록](../guide/COMBAT_TEST_MAP_VALIDATION.md)의 최신 절을 따른다.

## 2026-10-17 교체 구현 및 이관

**아래 재검토 절의 미구현 표시는 작업 전 조사 시점의 기록이다. 교체 코드는 반영했고, 데이터 이관과 실행 검증은 이 절 및 `COMBAT_TEST_MAP_VALIDATION.md`의 최신 결과를 기준으로 한다. 과거 완료 기록을 이번 변경의 증거로 사용하지 않는다.**

### 실제 실행 계약

- `PlayerTargetingController`가 입력 방향(무입력은 정면), 평면 거리, 높이, 장애물, 적대·피해 가능 조건으로 선택한다. 거리 → 각도 → 인스턴스 ID 순이다. `PlayerCombat`은 명시적 락온에도 사거리·높이·시야를 검사한다. 원거리 무기 조준 대상과 근접 루트 보정 대상은 분리한다.
- 일반·대시·점프·피니시·적 공격 상태는 재생 전에 대상 또는 빈 선택을 바인딩한다. 콤보의 다음 공격은 새 선택이다. `MotionEvent_MotionWarp`는 대상을 찾지 않고 저장된 설정으로 창을 열고 닫는다.
- 수동 입력 방향도 시작 시 고정한다. 대상이 없어도 저작된 회전 창에서 입력 방향으로 돌아가며, `maxWarpAngle`은 적이 추가하는 조준 보정에만 적용한다. 수동 90° 방향 전환을 20° 어시스트 상한으로 막지 않는다. 대상이 있었는데 사망한 경우에는 입력 방향으로 추적을 다시 열지 않는다. 교체 잔류 모델에도 이 입력 방향을 함께 전달한다.
- `MotionWarpController`의 공통 세션에서 회전과 이동의 허용 여부를 따로 유지한다. 이동 거절이 회전을 끄지 않는다. 타격/투사체 판정 시작은 같은 공격의 추적을 끝내며, 잔류 교체 공격도 대상과 확정 여부를 이어받는다.
- 원본은 실제 Animator 델타다. v3 리그 프로필은 도착점 예측·진행 비율에만 쓰며 현재 델타를 대신하지 않는다. Avatar, 스케일, 원본 클립, 타임라인 서명이 일치해야 이동 보정한다. 누락·불일치는 명시적으로 거절하고 런타임 첫 실행 베이크나 숨은 폴백은 없다. Development 빌드도 프로필 검사를 통과해야 한다.
- `EndpointCorrection`은 원본 예상 끝점과 접촉 도착점의 차이를 먼저 검사한 뒤, 원본 경로 진행량과 실제 델타에 비례해 배분한다. 제자리·왕복 구간에 전진을 만들지 않는다. KCC가 실제 충돌을 처리하고 차단된 보정은 종료한다. 남은 거리 보충·마지막 프레임 스냅·도착 보장은 없다. 예측과 실제 블렌딩/방향 변화의 오차는 별도 계측한다.
- 상태의 `PlayerAttackFacing`/`PlayerAttackTranslation` 중복 보정과 피니시의 독립 슬라이드를 제거했다. 점프의 기존 수직/공중 이동은 유지한다. KCC가 없는 교체 잔류 모델은 기존 충돌 sweep을 유지하고 재검색과 창 밖 추적 회전을 제거했다.

### 명시적 데이터 이관

`MotionWarpEndpointMigration`의 Analyze → Apply → PrepareProfiles를 통합 툴 런처에 등록했다. 레거시 프리셋의 **실제 해석값**을 먼저 기록하고 버전 1의 저장값으로 전환한다. 레거시 필드는 직렬화 호환과 이관을 위해 남겼고 런타임 덮어쓰기는 제거했다. 실패 시 Undo와 원본 바이트 백업을 복구한다.

- MotionSet 221개를 이관했다. 누락 리그 프로필 159개를 새로 측정했으며 현재 프로필은 1,662개다. 연결되지 않은 10개 에셋은 임의로 액터에 연결하지 않고 경고로 남겼다.
- 원본 프로젝트에는 같은 managed-reference rid의 워프 이벤트 블록만 이식했다. GUID, 창 시작·끝, 워프 바깥의 직렬화 내용이 221/221개 모두 보존됐다. 자동 재직렬화가 추가한 다른 필드는 가져오지 않았다. 근거: `output/MotionWarpReplacement/data-preservation.json`, `data-backup/`, `staged-data/`.
- 측정한 모든 연결 리그에서 이동 원본이 없는 공용 DualBlade Attack_8 두 개와 Staff 공격 네 개는 명시적으로 회전 전용으로 전환했다. Bokusei 기본 1·2타·강공격의 기존 `translationWeight=0`은 보존했다. 제자리 동작을 미끄러뜨려 캐릭터 간 이동량을 억지로 맞추지 않는다. 접근 공격은 별도 전진 클립·창을 사용한다.
- MotionKey, HitPhase, VFX, 히트스톱, 캔슬 이벤트와 SerializeReference 타입·GUID는 변경하지 않았다. 작업 전 사용자 변경 백업은 `output/MotionWarpReplacement/baseline/`과 `user-changes.patch`에 보관한다. 커밋하지 않았다.

실제 Player에서 추가로 확인한 소비자 연결 두 건을 보완했다. 공용 `AbilitySet_PlayerDoubleAxeAttackData`에는 DashAttack 바인딩이 없어 접근 입력이 즉시 Idle로 끝났다. 원본 전진 약 0.61m와 기존 판정을 가진 `Player.DoubleAxe.Light.01`을 DashAttack 슬롯에서도 재사용한다. Ability/Payload/HitPhase/Key를 복제하거나 수치를 새로 만들지 않았다. 이 Set을 쓰는 모든 쌍도끼 플레이어에 적용되며 몬스터 Set은 바꾸지 않는다. 선택 Set·Ability 검증 오류는 0건이다.

`Player/Katana/Katana_Finish`와 `Humanoid/Katana/Katana_Finish`에는 워프 창이 없었다. 상태의 독립 회전을 제거한 뒤에도 방향 정렬을 유지하도록 0~0.3초 회전 전용 창을 추가했다. 0.5초부터의 기존 카메라 연출과 뒤의 처형 이벤트는 그대로다. 피니시 후보에도 공통 높이·시야·거리 검사를 적용한다. 추가 수정 세 에셋의 기존 필드와 GUID 보존 근거는 `output/MotionWarpReplacement/final-content/preservation.json`이다.

### 검증 상태

Unity 6000.3.21f1에서 관련 EditMode 108/108, GAS Editor PlayMode 8/8 통과. 격리 사본 전체 회귀는 EditMode 870/891 통과·21 실패, PlayMode 20/20 통과다. 그래픽 활성 재검사 30/30 통과로 최초 실패 중 6개가 해소됐고, 15개는 미해결로 남는다. MotionSet 1,258개와 액터 프리팹 91개에서 누락 managed type·object reference·script는 0건이다. 전체 전투 데이터 검사에는 별도로 239 오류·64 경고가 있어 전체 프로젝트가 정상이라고 판정하지 않는다.

새로 빌드한 Windows Player의 실제 패드 입력에서 두 캐릭터의 기본 1·2타, 강공격, 접근, 락온, 높이·장애물 거절, 대상 사망, 경사·단차, 느린 재생·히트스톱, 교체 잔류 공격, 피니시를 확인했다. 화린 1.8m 콤보는 원본 경로 0.877m / 추가 보정 0.083m / 실제 KCC 0.795m, 접근은 0.662m / 0.040m / 0.671m였다. 수치는 워프 창의 경로 길이이며 공격 전체 순이동과 다르다. Bokusei 기본 공격의 추가 이동은 0이고, 접근도 이번 정면 조건에서는 원본 궤적의 변형 허용 범위를 넘어 보정을 거절했다. 원본 전진·타격은 실행됐다. 이를 보정 발동 성공으로 표시하거나, 캐릭터 간 수치를 맞추려고 한도를 늘리지 않았다.

상세 실행 시나리오·최종 Player 결과·시각 검증 한계는 [전투 테스트 기록](../guide/COMBAT_TEST_MAP_VALIDATION.md)의 최신 절에 기록한다. 발 디딤과 타격 연결의 연속 캡처를 검토했지만 VFX가 가리는 발과 프레임 사이 미세 미끄러짐까지 해결됐다고 판정하지 않는다.

### 주요 변경 파일

- 대상 선정: [PlayerTargetingController.cs](../../02.Scripts/GameActor/Component/Player/PlayerTargetingController.cs), [PlayerCombat.Finish.cs](../../02.Scripts/GameActor/Component/Player/PlayerCombat.Finish.cs).
- 세션·루트 보정·KCC 계측: [MotionWarpController.cs](../../02.Scripts/GameActor/MovementController/MotionWarpController.cs), [MotionWarpController.Endpoint.cs](../../02.Scripts/GameActor/MovementController/MotionWarpController.Endpoint.cs), [MotionWarpMath.cs](../../02.Scripts/GameActor/MovementController/MotionWarpMath.cs).
- 이벤트·리그 프로필: [MotionEvent_MotionWarp.cs](../../02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_MotionWarp.cs), [MotionWarpTrajectory.cs](../../02.Scripts/GameActor/MovementController/MotionWarpTrajectory.cs).
- 이관·베이크: [MotionWarpEndpointMigration.cs](../../02.Scripts/GameActor/Editor/MotionWarpEndpointMigration.cs), [WarpRootMotionBatchBakeWindow.cs](../../02.Scripts/GameActor/Editor/WarpRootMotionBatchBakeWindow.cs). 통합 툴 런처에서 분석·이관·프로필 준비를 실행한다.
- 소비 상태: `GameActor/State/Player/`의 Attack·DashAttack·JumpAttack·FinishAttack, `State/Enemy/EnemyAttackState.cs`, `Component/Player/SwapResidualAttackRunner.cs`.
- 회귀 검사: [MotionWarpEndpointTests.cs](../../Tests/EditMode/Movement/MotionWarpEndpointTests.cs), [KatanaMotionWarpTests.cs](../../Tests/EditMode/Movement/KatanaMotionWarpTests.cs), `EnemyDetectionTests.cs`, `GameplayAbilityVerticalSlicePlayModeTests.cs`.

## 2026-10-17 재검토: 루트모션 수정 파이프라인 교체

**상태: 코드·에셋 정적 조사와 Epic 공식 자료 비교 완료. 아래 교체 설계는 미구현이며 실행 검증되지 않았다.** `Complete` 폴더명은 과거 문서 위치다. 현재 플레이어 워핑이 완료됐다는 의미로 사용하지 않는다. 이 절 아래의 기존 설계는 역사적 기록이며, 충돌하는 정책은 이 절의 교체 설계를 따른다.

사용자 재현은 “Bokusei는 워핑하지 않고, 화린은 과하게 미끄러진다”다. 앞선 공격당 추가 이동 상한 0.35m/0.75m는 새 구조의 해법으로 채택하지 않는다. 공격 대상 선정부터 애니메이션 루트 델타, 워프 구간, 충돌 적용까지 계약을 다시 정의한다. 아래 값과 소스 분석은 현재 작업 트리 기준이며, 실행 중 바이너리에 동일하게 반영되었다는 증거는 아니다.

### 1. 확인한 원인과 아직 확인하지 않은 것

| 근거 | 현재 동작 | 구조상 문제 |
|---|---|---|
| `PlayerModel_Raon` → `Bokusei_PlayerAnimationSet` → `Bokusei_KatanaAnimationSet` | 기본 1·2타와 강공격의 `PlayerGrounded_Bokusei` 이벤트가 Custom, `translationWeight = 0` | 이 공격들은 데이터상 이동 워프를 꺼 놓았다. 원본 전진량을 늘리는 것과 대상까지 워핑하는 것은 다른 작업이다. |
| `PlayerModel_Hwarin` → `Humanoid_Player_AnimationSet` → `Humanoid_Player_DoubleAxeAnimationSet` | 화린 전용 이름의 세트가 아니라 공용 쌍도끼 매핑을 사용 | 캐릭터 이름으로 파일을 찾아 조정하면 실행 데이터와 어긋난다. Ability MotionKey에서 실제 무기 매핑까지 추적해야 한다. |
| `Humanoid_DoubleAxeAxeAnimationSet_Attack_1` + `MotionEvent_MotionWarp.ApplyPreset` | 저장된 거리 4m가 LightAttack/TargetCenter 해석 후 7m, 이동 가중치 1이 됨. ContactShell인 HeavyAttack_1은 다른 분기 | 같은 쌍도끼 안에서도 도착점·보정 제한 정책이 다르다. 모든 화린 공격이 7m라는 뜻은 아니다. Inspector 원시값만으로 실행값을 알 수 없다. |
| `MotionWarpController.EvaluateDeltaWarpVelocity` | 보정량 제한을 ContactShell 분기에서만 적용 | TargetCenter 데이터는 같은 근접 공격이어도 접촉 도착점과 보정 제한을 공유하지 않는다. |
| `TryGetFacingDirection` | 이동 계산의 `_isApplicable`이 회전 조건 | 이동 불가가 회전 불가로 전파된다. |
| `PlayerAttackFacing` / `PlayerAttackTranslation` | 공격 상태가 독립 회전과 속력 차이 예산을 적용 | 타임라인 회전 저작을 우회한다. 속력 차이는 벡터 보정량이 아니므로 같은 속력의 측면 이동은 예산에 잡히지 않는다. |
| `EvaluateVelocity`의 v3 trajectory 처리 | 현재 Animator 델타를 베이크 프레임 델타로 대체하고, 시작 시 회전 기준을 사용 | 현재 포즈·블렌드·회전과 적용 이동이 서로 다른 소스를 사용할 수 있다. 베이크 존재 여부에 따라 원본 이동 경로까지 달라진다. |
| `PlayerAttackState` 진입 | `PlayCurrentAttackMotion` 뒤에 대상 선정·잠금 | 0초 이벤트와 대상 바인딩 순서가 계약으로 보장되지 않는다. 실제 이벤트가 같은 호출에서 실행되는지는 통합 계측이 필요하다. |
| `EvaluateScaleVelocity` / `EvaluateSkewVelocity` | 거리/남은 시간으로 목표 속력을 만들거나 원본 속력과 혼합 | 명칭이 Unreal의 Scale/Skew와 같아도 같은 수학이나 계약이 아니다. |

Bokusei 1타의 `bakedPathLen ≈ 0.093m`, 화린 공용 쌍도끼 1타의 `≈ 0.208m`는 저장된 워프 구간의 경로 길이다. 실제 전체 클립 순전진량이나 최신 Avatar의 Player 측정값으로 인용하지 않는다. `translationWeight = 0`은 이동 워프 비활성의 직접 근거지만, 관측된 모든 회전·발 미끄러짐의 단일 원인을 확정하지는 못한다.

### 2. Unreal 조사에서 가져올 계약

Epic의 [Motion Warping 개요](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine)는 이름 있는 목표와 애니메이션의 시작·종료 구간을 연결한다. Skew Warp의 목적은 그 구간 끝의 위치·회전 정렬이다. 이동과 회전은 별도 옵션이며, 회전은 목표를 바라보기 또는 목표 회전에 맞추기를 선택한다. 자동 적 선정은 이 기능의 책임이 아니다.

[SkewWarp API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/URootMotionModifier_SkewWarp)의 `WarpTranslation` 입력은 현재 변환, 현재 델타, 총 변위, 목표 위치다. 기존의 거리/시간 속력 혼합을 이름만 Skew로 유지하지 않는다. 공개 API와 설명을 확인했으며 Epic 비공개 소스의 알고리즘을 그대로 포팅했다고 주장하지 않는다.

[MotionWarpingComponent API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/UMotionWarpingComponent)는 이름 기반 목표 등록, 컴포넌트 추적 여부, 수정자 목록, 월드 변환 전 루트모션 처리 진입점을 분리한다. 이를 참고해 프로젝트에서는 Animancer의 델타를 KCC가 소비하기 전 한 번만 수정하는 경계를 둔다. Unity와 Unreal의 축·델타 공간은 다르므로 수치를 그대로 복사하지 않는다.

[Warp modifier API](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/MotionWarping/URootMotionModifier_Warp)는 원본 이동이 없을 때의 추가 이동 easing도 제공한다. 따라서 “Unreal도 제자리 모션은 이동시킬 수 없다”는 설명은 틀리다. 다만 이동 생성이 발 접지 품질을 보장하지는 않는다. 이 프로젝트의 일반 공격에서는 원본 이동이 없는 구간의 자동 추가 전진을 금지하고, 필요한 진입 동작은 별도 저작 정책으로 명시한다. 이는 프로젝트의 게임플레이 선택이다.

Epic의 [UE-354367](https://issues.unrealengine.com/issue/UE-354367)은 notify 시작 뒤 늦게 목표를 등록할 때 워프가 활성화되지 않는 사례다. 프로젝트도 재생 전에 대상을 준비하는 순서를 계약으로 삼는다. 이 이슈 자체를 현재 Unity 버그의 재현 증거로 사용하지 않는다.

설치된 `KINEMATION/MotionWarping`도 검토했다. 해당 런타임은 자체 LateUpdate, CharacterController/Rigidbody 또는 Transform 이동, 선택적 Animator 재생을 소유한다. 현재 KCC·Animancer 파이프라인에 컴포넌트를 추가하면 이동 권한이 중복되므로 그대로 교체 투입하지 않는다. 외부 패키지를 추가할 필요도 없다.

### 3. 교체할 실행 경계

아래는 새 계약의 개념 흐름이며 아직 존재하지 않는 API가 구현됐다는 뜻이 아니다.

```text
입력 / 명시적 락온 / 반격 대상
  → PlayerTargetingController: 공격 의도와 후보를 한 번 결정
  → PlayerCombat: MotionKey 해석, 대상·빈 선택을 공격 실행 ID에 바인딩
  → ActorAnimator: 바인딩 완료 후 MotionSet 재생
  → MotionEvent_MotionWarp: 구간과 저작 설정으로 세션만 시작/종료
  → MotionWarpController: 같은 애니메이션 span의 이동·회전을 함께 평가
  → ActorMovementController: 계산 결과를 KCC 회전/속도 콜백에 전달
  → KCC: 충돌·지면 처리 후 실제 적용 변위를 세션에 반환
```

- **대상 선정:** 일반 공격은 입력 방향 원뿔 안의 유효한 가까운 대상이 우선이다. 무입력은 정면, 동일 거리이면 각도, 완전 동률이면 안정된 액터 식별자로 정한다. 하드 락온은 명시적 대상 의도를 유지하되 이동 도달 가능성을 면제하지 않는다. 벽·높이 차·적대 관계·피해 가능 여부를 검사한다. 공격 중 사망해도 다른 대상으로 갈아타지 않고 다음 공격에서 재선정한다. 거리 계산은 액터 원점 거리와 접촉면 거리를 혼용하지 않는다.
- **바인딩:** 선택 없음도 확정 결과다. MotionEvent가 적을 재검색하거나 탐색 반경을 늘리지 않는다. 대상 확정 → 재생 → 0초 이벤트 순서를 일반·접근·콤보에 동일하게 적용한다. 재생 실패 시 해당 실행 ID의 바인딩을 해제한다.
- **도착점:** 근접 공격은 적 중심이 아닌 양쪽 캡슐과 저작된 공격 간격으로 계산한 접촉 도착점을 쓴다. 무기 HitBox의 명중 판정은 별도로 유지한다. 목표 좌표에 도착했다고 자동 명중시키지 않는다.
- **원본 델타:** 현재 프레임 이동·회전의 원본은 실제 Animator 출력이다. 베이크는 남은 구간 예측에만 사용한다. Avatar·스케일·클립·타임라인 fingerprint가 맞지 않으면 이동 워프를 거절하고 사유를 남긴다. 첫 실행과 캐시가 데워진 실행이 다른 정책을 사용하지 않는다.
- **애니메이션 시간:** 이전 평가 시각→현재 평가 시각의 span을 사용한다. 창 경계를 넘는 프레임은 창 밖/안으로 분리하고 마지막 구간을 한 번만 소비한다. KCC가 애니메이션보다 자주 호출돼도 같은 델타를 재소비하지 않는다. 히트스톱은 진행도·보정량을 소비하지 않는다.
- **이동·회전:** 하나의 세션이 둘을 계산하되 적용 가능 여부는 각각 가진다. 이동이 거절돼도 저작된 회전 창은 동작할 수 있다. 동일 시각의 결과를 두 KCC 콜백이 공유하며, 회전이 앞 프레임의 이동 성공 플래그를 읽지 않는다. State의 별도 RotateTowards를 제거한다.
- **종료:** Snapshot은 구간 시작 도착점을 고정한다. 명시적 추적형 동작만 목표 갱신을 허용한다. 타격 확정 이후에는 대상 측면 회피를 따라 돌거나 추가 접근하지 않는다. 중단·사망·교체·콤보 재생 시 이전 handle의 종료가 새 세션에 영향을 주지 않는다.

### 4. 이동 보정의 수학적 계약

기본 근접 공격의 첫 구현은 이미 있는 trajectory 샘플링을 활용한 **끝점 보정**으로 통일한다. Unreal Skew와 동일한 구현이라고 명명하지 않는다. Scale/Skew/Delta의 서로 다른 폴백을 새 경로에 겹쳐 두지 않는다.

1. 구간 시작에 실제 루트 위치, 원본 예상 끝점, 접촉 목표를 같은 좌표계로 변환한다. 목표와 원본 끝점의 차이를 보정 벡터로 저장한다.
2. 원본 예상 이동량 대비 늘림/줄임 비율, 회전 각도, 절대 보정량, 높이와 장애물 조건으로 **시작 전에** 도달 가능성을 판정한다. 범위를 넘으면 이동 워프를 거절한다. 먼 상대를 골랐다는 이유로 접근 공격으로 자동 변경하지 않는다.
3. 원본 누적 경로의 진행도 `q`로 추가 보정을 배분한다. 고정 보정 벡터 `C`일 때 구간의 보정은 `C × (q1 − q0)`이고, 합은 `C`를 넘지 않는다. 경로가 멈춘 구간에는 추가 전진도 없다. 현재 원본 델타와 예측 궤적의 오차는 별도로 측정한다.
4. 회전과 이동 좌표계는 한 세션에서 관리한다. 프레임별 실제 root delta와 남은 root 예측에 같은 기준 변환을 적용한다. 방향이 바뀌는 클립, 블렌딩, 다중 클립 체이닝은 별도 테스트한다. 순변위가 0인데 왕복 경로는 긴 클립을 전진 가능한 모션으로 오판하지 않는다.
5. KCC가 막은 거리를 이후 프레임의 보정 부채로 재분배하지 않는다. 지속 차단 시 이동 보정 세션을 중단하고 원본 이동도 KCC 충돌을 따른다. 순간 속도 제한은 안전장치이며, 제한 때문에 끝점에 못 갔으면 미도달로 기록한다. 마지막 프레임 스냅은 하지 않는다.

접근 공격은 충분한 전진 루트가 있는 클립과 그 발이 움직이는 구간을 사용한다. Bokusei의 이동 가중치를 일괄 1로 바꾸거나 화린의 모든 클립에 같은 거리 상한을 붙이는 방식은 채택하지 않는다. 같은 **알고리즘·저작 의미**를 쓰되, 각 클립의 원본 궤적과 허용 변형은 다를 수 있다.

### 5. 데이터와 기존 코드의 이관

| 대상 | 처리 |
|---|---|
| `MotionEvent_MotionWarp` | 기존 SerializeReference 타입·어셈블리·GUID 유지. 저작 설정과 세션 시작/종료만 담당. 플레이어 후보 재검색 제거. |
| `ApplyPreset` | 런타임 덮어쓰기를 폐기할 때 모든 참조 에셋의 현재 ResolveSettings 결과를 먼저 저장한다. 프리셋은 Editor에서 명시적 값으로 적용하는 편의 기능으로 전환. 단순히 switch를 삭제하면 기존 에셋 의미가 바뀌므로 금지. |
| `MotionWarpWindowSettings` | 구간별 이동/회전 정책의 단일 입력. 유효 설정을 도구에서 그대로 표시. 별도 PlayerCombat 값으로 결과를 다시 제한하지 않음. |
| `MotionWarpController` | 대상 저장, 세션 수명, 델타 평가, KCC 결과 반영을 분리. 기존 handle 보호와 대상 유실 처리는 보존. `MotionWarpMath`·`MotionWarpTrajectory`를 재사용하고 새 매니저/asmdef는 만들지 않음. |
| `PlayerAttackFacing` / `PlayerAttackTranslation` | 새 공통 경로의 회전·보정 계약과 테스트를 연결한 뒤 호출부와 함께 제거. 단순 삭제 후 옛 `_isApplicable` 회전 경로로 돌아가지 않음. |
| PlayerAttack/DashAttack 및 다른 워프 소비 상태 | 진입·종료와 공격 의도만 소유. 상태마다 별도 이동·회전 수식을 두지 않음. Finish/Grab/Jump/교체 공격과 Enemy 소비자를 모두 검색해 이관 목록 확정. |
| 베이크 도구 | 실제 모델 Avatar/스케일과 해결된 MotionKey를 보고, stale·누락 프로필을 오류로 표시. 데이터 적용 전 dry-run 및 실패 시 전체 Undo 롤백. |
| Motion Editor | 원본 경로/예상 보정 경로/접촉 목표/회전 종료를 함께 표시. 새 진입점은 통합 툴 런처에 등록. |

GAS는 발동·비용·히트 수치, MotionSet은 워프와 타격·캔슬 시각을 계속 소유한다. 에셋 생성·프로필 재베이크·프리셋 이관은 Unity AssetDatabase 도구로 수행한다. 공유 쌍도끼 에셋의 다른 소비자를 조사한 뒤 공용 정책 변경인지 특정 무기 모션 파생인지 결정하며 캐릭터 이름 분기는 넣지 않는다.

### 6. 구현 순서와 통과 조건

| 단계 | 산출물 | 다음 단계 진입 기준 |
|---|---|---|
| A. 실행 데이터 감사 | 두 캐릭터의 Ability/MotionKey/무기 매핑/클립/Avatar/유효 워프 설정 보고서 | 실제 Unity 로드 결과와 사용 모델 일치. 현재 텍스트 조사는 예비 근거일 뿐 |
| B. 공통 델타 처리 | 단일 span 평가, 이동/회전 독립 gate, 사전 목표 바인딩 | 아래 순수 계산·수명주기 회귀 통과. 상태 외부 보정 제거 |
| C. 데이터 이관 | 프리셋 실체화, 두 캐릭터의 기본·강·접근 공격 창/프로필 | GUID·MotionKey·HitPhase·VFX 유지, 누락 0, 재실행 이관 diff 0 |
| D. 통합 | 일반·접근·콤보와 나머지 소비 상태 연결 | 실제 입력→GAS→MotionSet→KCC 경로 검증. 내부 필드 주입만으로 대체 금지 |
| E. Player 검증 | 새 Windows 빌드와 자동 실행 계측 | 빌드/실행 오류 0, 이동/회전 계측 및 화면 검토를 별도로 기록 |

필수 회귀 사례:

- 동일 궤적을 30/60/120Hz 및 창 경계를 건너는 큰 span으로 평가해 누적 보정 오차 1mm 이하. 회전 수치 비교 허용 오차 0.1도. 이는 계산 테스트 기준이며 실제 KCC 충돌 오차와 혼동하지 않는다.
- 대상 없음/워프 비활성에서 원본 델타 그대로, 제자리/왕복 클립에서 임의 전진 없음, 이동 거절 중 허용된 회전 동작.
- 0초 창·콤보 첫 프레임에 올바른 실행 ID 대상 사용. 취소/재진입/이전 handle 종료 시 새 실행 보호.
- 같은 공격의 첫 실행/재실행, 유효/누락/stale/다른 Avatar 프로필 비교. 프로필 오류는 눈에 보이는 진단이며 성공 워프로 집계하지 않음.
- 락온 유무, 근접 측면/먼 정면 후보, 벽 뒤/사망/동일 거리/대형 캡슐. 선정 이유·거리·각도·거절 사유 기록.
- 두 캐릭터의 기본 1·2타/강공격/접근 공격을 같은 배치에서 비교. 원본 이동·추가 보정·KCC 실제 이동·최종 잔차를 별도 계측.
- 벽/경사/단차와 이동하는 대상, 히트스톱/느린 재생/공격 속도, 추적 종료 뒤 측면 회피. 벽에 막힌 뒤 뒤늦게 튀어나가는 현상 없음.
- 텔레그래프·HitPhase·히트스톱·공격→회피 캔슬 창 유지. 수치 통과와 발 미끄러짐·타격 감각의 시각 판정은 별개.

### 7. 이번 조사 결과의 범위

Epic 공식 문서/API 및 이슈, 현재 C# 처리 경로, 직렬화 에셋, 기존 KINEMATION 구현을 읽고 비교했다. 이번 재검토에서는 런타임·에셋을 수정하지 않았으며 새 컴파일/Unity 테스트/Windows 빌드를 수행하지 않았다. 앞선 보조 검사 11/11은 공격당 속력 제한의 검사일 뿐 위 구조의 검증 증거가 아니다. **구조 개편 구현과 사용자 증상 해결은 아직 남아 있다.**

---

## 과거 설계 기록

## 목적

현재 모션워핑은 `MotionEvent_MotionWarp`가 공격 MotionSet의 특정 구간을 열고, `PlayerAttackState`와 `EnemyAttackState`가 남은 시간 기준으로 타겟까지의 속도를 역산하는 방식이다. 이 방식은 구현이 단순하지만 플레이어/적 로직이 분리되어 있고, 원본 루트모션 곡선을 보존하기 어렵다.

개선 목표는 다음과 같다.

- MotionSet 이벤트 데이터만으로 워프 구간과 옵션을 튜닝한다.
- 플레이어, 적, 피니시 어택, 잡기 진입, 교체 등장 공격이 같은 워프 계산 경로를 사용한다.
- KCC 이동 경로를 유지해 충돌, 지면, 슬로프 처리를 보존한다.
- 테스트씬에서 MotionSet 이벤트 실행 결과를 즉시 확인한다.

## 현재 구조

- 이벤트 시작: `MotionEvent_MotionWarp.Execute()`가 `PlayerCombat.BeginMotionWarp()` 또는 `EnemyCombat.BeginMotionWarp()` 호출
- 이벤트 종료: `MotionEvent_MotionWarp.OnCompleteEvent()`가 `EndMotionWarp()` 호출
- 플레이어 이동 보정: `PlayerAttackState.UpdateVelocity()`
- 적 이동 보정: `EnemyAttackState.UpdateVelocity()`
- 루트모션 입력: `ActorAnimator.OnAnimatorMove()`에서 `DeltaPosition`, `DeltaRotation` 저장
- 테스트 재생: `MotionSetWindow`가 플레이 모드에서 테스트씬을 열고 Animancer로 MotionSet을 직접 재생

## 문제점

1. 워프 상태가 Combat에 있고, 실제 이동 보정은 State에 있어 책임이 분산되어 있다.
2. 플레이어와 적의 워프 품질이 다르다. 플레이어는 스냅샷, 블렌딩, 도달 가능성 판정이 있지만 적은 단순 타겟 추적이다.
3. `남은 거리 / 남은 시간` 방식은 발 타이밍과 원본 루트모션 리듬을 쉽게 잃는다.
4. 워프 타이머가 `Time.deltaTime`을 사용해 히트스톱, 로컬 타임스케일, MotionSet 루프/프리즈와 어긋날 수 있다.
5. MotionSet 이벤트 재생 결과를 테스트씬에서 확인하는 도구가 부족하다.

## 목표 구조

### MotionWarpController

액터 공통 컴포넌트로 분리한다.

책임:

- 현재 워프 윈도우 상태 보관
- 워프 타겟 스냅샷 또는 live 추적
- 루트모션 수정자 선택
- 워프 실패 사유 기록
- 디버그 기즈모/오버레이용 상태 제공

State는 `MotionWarpController.EvaluateVelocity(rootVelocity, deltaTime)` 결과만 사용한다.

### MotionWarpTargetResolver

타겟 선택은 액터별 정책을 유지한다.

- 플레이어: 락온 타겟 우선, 없으면 공격 범위/각도 기반 자동 스냅
- 적: `EnemyDetection.CurrentTarget`
- 피니시/잡기: 지정 Transform 또는 소켓

타겟 선택 결과는 `MotionWarpController.BeginWarp()`에 전달한다.

### RootMotionModifier

초기 구현 순서:

1. `AdditiveWarp`: 현재 방식과 가장 비슷한 보정. 루트모션에 부족한 이동량을 더한다.
2. `ScaleWarp`: 남은 루트모션 총 이동량 대비 목표 거리 비율로 XZ 델타를 스케일한다.
3. `SkewWarp`: 최종 위치/회전을 맞추도록 남은 루트모션 경로를 회전/왜곡한다.

`ScaleWarp`부터 도입하면 현재 플레이 감각을 크게 흔들지 않고 원본 모션 리듬을 조금 더 보존할 수 있다.

## MotionEvent_MotionWarp 확장안

추가 필드:

- `warpTargetName`: 워프 타겟 식별자
- `modifierType`: `Additive`, `Scale`, `Skew`
- `targetPolicy`: `Snapshot`, `Live`, `Predictive`
- `translationWeight`
- `rotationWeight`
- `ignoreY`
- `rotationMode`: `None`, `FaceTarget`, `MatchTargetRotation`
- `minDistance`
- `maxDistance`
- `maxSpeed`
- `targetOffset`
- `easingCurve`

초기 적용에서는 기존 필드를 깨지 않도록 기본값을 현재 동작과 동일하게 둔다.

## MotionEvent_MotionWarp 옵션 설명

### Start / End

Motion Warp 이벤트가 활성화되는 MotionSet 타임라인 시간이다.

- `Start`: 워프 시작 시간
- `End`: 워프 종료 시간

예를 들어 `Start = 0`, `End = 0.2`이면 모션 시작 후 0.2초 동안 워프가 적용된다. 일반 공격은 히트 판정이 켜지기 직전까지 워프를 열어두는 구성이 안전하다.

### preset

자주 쓰는 설정 묶음이다. `Custom`이 아니면 런타임에서 일부 옵션을 프리셋 값으로 덮어쓴다.

- `Custom`: 인스펙터에 입력한 값을 그대로 사용한다.
- `LightAttack`: 약공격용. `Additive`, `Snapshot`, 빠른 접근 보정.
- `HeavyAttack`: 강공격용. `Scale`, `Snapshot`, 약간 무거운 접근 보정.
- `FinishAttack`: 피니시/정렬 공격용. `Skew`, `Snapshot`, 위치 정렬 우선.
- `Grab`: 잡기 진입용. `Skew`, `Live`, 움직이는 타겟 추적 우선.

프리셋을 사용하면 `modifierType`, `targetPolicy`, `translationWeight`, `rotationWeight`, `ignoreY`, 거리/속도 제한 일부가 프리셋 값으로 적용된다. 개별 값을 직접 튜닝하려면 `Custom`을 사용한다.

### modifierType

루트모션 속도를 타겟 방향으로 어떻게 보정할지 결정한다.

- `Additive`: 기존 워프 감각을 보존하는 기본값이다. 남은 시간 안에 타겟까지 도달하도록 타겟 방향 속도를 강하게 섞는다. 반응이 빠르고 일반 약공격에 적합하다.
- `Scale`: 원본 루트모션의 수평 속도를 타겟 방향으로 스케일한다. 발 타이밍과 모션 리듬 보존이 `Additive`보다 낫다. 강공격처럼 무게감이 필요한 공격에 적합하다.
- `Skew`: 원본 수평 속도를 일부 보존하면서 남은 시간 기준 도착 보정을 강하게 적용한다. 피니시 공격, 잡기, 특정 위치 정렬이 중요한 액션에 적합하다.

### targetPolicy

워프 목표 위치를 언제 기준으로 사용할지 결정한다.

- `Snapshot`: 워프 시작 순간의 타겟 위치를 고정한다. 경로가 흔들리지 않아 대부분의 근접 공격에 적합하다.
- `Live`: 매 프레임 타겟 위치를 다시 읽는다. 움직이는 타겟을 계속 따라붙어야 하는 잡기/추적형 액션에 적합하지만, 타겟 이동이 심하면 경로가 흔들릴 수 있다.

### translationWeight

이동 보정 강도다.

- `0`: 루트모션 원본만 사용한다.
- `1`: 워프 이동 보정을 최대 적용한다.

공격 모션의 발 미끄러짐이 크면 값을 낮추고, 히트 거리 보정이 부족하면 값을 높인다.

### rotationWeight

워프 중 타겟 방향 회전 보정 허용 여부/강도다.

현재 1차 구현에서는 `0`이면 워프 방향 회전을 끄고, `0`보다 크면 타겟 방향 회전 보정을 허용한다. 향후에는 0~1 사이 값을 실제 회전 보간 강도로 사용할 수 있다.

### ignoreY

Y축 보정을 무시할지 결정한다.

체크하면 수평 이동만 워프하고 Y축은 루트모션, 중력, KCC 지면 처리 흐름을 유지한다. 지상 공격은 대부분 켜는 것이 안전하다. 공중 잡기나 수직 정렬이 필요한 특수 액션에서만 끄는 것을 검토한다.

### overrideDistance

이벤트 전용 거리/속도 제한을 사용할지 결정한다.

- 꺼짐: `PlayerCombat` 또는 `EnemyCombat`의 기본 워프 거리/속도 설정을 사용한다.
- 켜짐: 아래 `minDistance`, `maxDistance`, `maxSpeed`를 이 이벤트 전용 값으로 사용한다.

공격별로 사거리와 접근 속도가 다르면 켠다. 전체 캐릭터 공통 감각을 유지하고 싶으면 끈다.

### minDistance

타겟이 이 거리보다 가까우면 워프를 적용하지 않는다.

너무 가까운 타겟에게 워프하면 캐릭터가 타겟 안으로 파고들거나 미세하게 튈 수 있다. 일반 공격은 `0.25~0.35` 정도가 무난하다.

### maxDistance

타겟이 이 거리보다 멀면 워프를 적용하지 않는다.

공격 사거리 밖의 타겟에게 과도하게 빨려 들어가는 것을 막는다. 약공격은 짧게, 강공격/돌진 공격은 길게 설정한다.

### maxSpeed

워프 보정의 최대 속도다.

남은 시간 동안 `maxSpeed`로 이동해도 타겟에 도달할 수 없으면 워프가 취소된다. 값이 너무 높으면 빨려 들어가는 느낌이 강해지고, 너무 낮으면 워프 실패가 잦아진다.

### targetOffset

타겟 위치에 더할 월드 기준 오프셋이다.

타겟 중심이 아니라 타겟 앞쪽/옆쪽에 정렬하고 싶을 때 사용한다. 예를 들어 피니시 공격에서 적 중심보다 살짝 앞에 멈추고 싶으면 공격 방향 기준 오프셋이 필요하다. 현재 구현은 월드 기준 오프셋이므로, 향후 타겟/공격자 로컬 기준 오프셋 옵션을 추가할 수 있다.

### globalStartTimeOffset

MotionSet 내부에서 이전 모션들의 누적 시간을 더하기 위한 런타임 보조값이다. 직접 편집하는 값이 아니다.

이 값은 인스펙터에서 숨기는 편이 좋다. 표시된다면 에디터 드로어에서 `globalStartTimeOffset`을 제외하도록 개선한다.

## 테스트씬 확인 기능

`MotionSetWindow`에서 다음을 제공한다.

- 테스트씬 로드 후 플레이 모드 진입
- 선택한 액터에 MotionSet 재생
- MotionSet 이벤트 시작/종료 로그
- 현재 활성 이벤트 목록 표시
- Scene 뷰 라벨 표시
- Game 뷰 오버레이 표시용 `MotionSetEventDebugOverlay` 자동 부착

이 기능은 모션워핑 개선 전에 먼저 넣는다. 이후 워프 계산을 바꿀 때 이벤트 실행 타이밍과 상태 변화를 같은 테스트씬에서 검증하기 위함이다.

## 단계별 작업

### 1단계: 관측 도구

- `MotionSetWindow`의 이벤트 실행 경로에서 글로벌 이벤트도 처리한다.
- 이벤트 시작/종료 내역을 창과 Scene/Game 뷰에 표시한다.
- `MotionSetEventDebugOverlay`를 추가해 플레이 중 현재 활성 이벤트와 최근 이벤트를 확인한다.

상태: 완료. `MotionSetWindow` 이벤트 디버그 패널, Scene 라벨, Game 오버레이, 글로벌 이벤트 실행 처리를 추가했다.

### 2단계: 공통 컨트롤러 도입

- `MotionWarpController`를 추가한다.
- 기존 플레이어 워프 로직을 기능 보존 상태로 이관한다.
- `PlayerAttackState`는 컨트롤러 호출만 남긴다.

상태: 1차 완료. `ActorMovementController`가 `MotionWarpController`를 자동 보유하고, `PlayerAttackState`가 공통 컨트롤러로 속도/회전 보정을 위임한다.

### 3단계: 적 워프 통합

- `EnemyAttackState`도 같은 컨트롤러를 사용한다.
- 적 워프도 스냅샷, 도달 가능성 판정, 블렌딩을 동일하게 적용한다.

상태: 1차 완료. `EnemyCombat`에 `WarpDuration`, `WarpMaxSpeed`를 추가하고 `EnemyAttackState`도 공통 컨트롤러를 사용한다.

### 4단계: 루트모션 수정자 개선

- `AdditiveWarp`로 현재 동작을 대체한다.
- `ScaleWarp`를 추가하고 공격별로 비교한다.
- 고정 위치 정렬이 중요한 피니시/잡기 계열에 `SkewWarp`를 적용한다.

상태: 완료. `MotionWarpModifierType.Additive`, `Scale`, `Skew`를 추가했고 `MotionEvent_MotionWarp`에서 modifier를 선택할 수 있다. `Additive`는 기존 플레이어 워프 감각을 보존하는 기본값이며, `Scale`은 루트모션 수평 속도를 타겟 방향으로 스케일한다. `Skew`는 원본 수평 속도를 일부 보존하면서 남은 시간 기준 도착 보정을 강하게 적용한다.

### 5단계: 데이터 튜닝

- MotionSet 이벤트 인스펙터에서 워프 옵션을 노출한다.
- 액션별 기본 프리셋을 만든다.
- 실패 사유와 도착 오차를 디버그 오버레이에 표시한다.

상태: 완료. `MotionEvent_MotionWarp`에 modifier, 프리셋, 타겟 정책, translation/rotation weight, Y 무시, 거리/속도 override, target offset을 추가했다. `LightAttack`, `HeavyAttack`, `FinishAttack`, `Grab` 프리셋을 제공한다. 테스트씬 이벤트 디버그에는 워프 적용 여부, 실패 사유, 남은 도착 오차를 표시한다.

## 검증 기준

- MotionSetWindow에서 모션별 이벤트 시작/종료가 시간축과 일치한다.
- `MotionEvent_MotionWarp` 시작/종료가 테스트씬 오버레이에 표시된다.
- 히트스톱 또는 Freeze 중 이벤트 상태가 의도와 다르게 빨리 닫히지 않는다.
- 플레이어/적의 워프 실패 조건이 동일한 규칙으로 판단된다.
- 워프 적용 후 KCC 충돌 처리가 유지된다.
