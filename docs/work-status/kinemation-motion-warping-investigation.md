# KINEMATION MotionWarping 조사

2026-11-17. 설치된 소스 정적 조사이며 런타임 적용 작업이 아니다.

후속 사용자 지시에 따른 연목 작업점 정렬 구현은 [별도 상태](npc-work-alignment.md)와 [설계](../../Assets/docs/design/npc/NPC_WORK_ALIGNMENT.md)를 따른다. 아래의 미구현 표기는 각 조사 시점의 기록이다.

## 결론

구매 패키지는 루트 이동 곡선을 목표 위치·회전에 맞추는 범용 도구다. 전투 이동의 실행 정본은 기존 `MotionEvent_MotionWarp`와 `MotionWarpController`로 유지하는 편이 적절하다. 패키지 전체를 제거하면 현재 사용 중인 `TwoBoneIk` 의존성이 깨진다.

## 확인한 구성

- [설치 폴더](../../Assets/ExternalAssets/Etc/KINEMATION/MotionWarping): C# 20개, asmdef 2개, 오프라인 PDF와 공식 문서·데모 저장소 바로가기. 이 폴더에는 애니메이션·프리팹·씬·설정 SO 실물이 없다.
- [MotionWarpingAsset](../../Assets/ExternalAssets/Etc/KINEMATION/MotionWarping/Runtime/Core/MotionWarpingAsset.cs): 클립, XYZ 이동 곡선, 축별 보정·원본 이동·선형 보간 설정, 충돌 사용, 구간별 목표 오프셋과 재생 속도 범위.
- [실행 컴포넌트](../../Assets/ExternalAssets/Etc/KINEMATION/MotionWarping/Runtime/Core/MotionWarping.cs): `Play(asset, points)` 또는 `Interact(IWarpPointProvider)`로 시작한다. 구간과 목표 수가 일치해야 한다. `LateUpdate`에서 이동·회전을 적용하고 선택적으로 IK를 호출한다. Transform 기반 움직이는 목표를 지원한다.
- [Editor](../../Assets/ExternalAssets/Etc/KINEMATION/MotionWarping/Editor/Core/MotionWarpingAssetEditor.cs): 구간 타임라인, 포즈 프리뷰, 60Hz 루트 이동 곡선 추출. Humanoid는 RootT, Generic은 이름이 root로 끝나는 경로의 위치 곡선을 찾는다. Unity 내부 프리뷰 타입을 Reflection으로 사용한다.
- 예제는 Mantle(낮은/높은 턱), Vault(진입·반대쪽 모서리·착지 3점), Hang(매달리기/내려오기), Align(상대 정렬), WarpObstacle(사전 배치 목표)이다. 완성된 프로젝트 이동 상태·입력 시스템이 함께 제공되는 구성은 아니다.
- `MotionWarpingIk`는 Animator float 곡선으로 손발의 월드 위치·회전을 잠그는 방식이다. 지형 자동 발 찾기나 무기 파지 자동 생성 기능과는 다르다.

## 프로젝트 소비자

`Assets/02.Scripts` C# 검색에서 구매 패키지 namespace를 사용하는 파일은 아래 3개이며 모두 `TwoBoneIk.SolveTwoBoneIK`를 사용한다.

- [WeaponIKController](../../Assets/02.Scripts/GameActor/Component/Player/WeaponIKController.cs): 보조손 파지.
- [WeaponAimController](../../Assets/02.Scripts/GameActor/Component/Player/WeaponAimController.cs): 조준 보조손.
- [PlayerGliderBinding](../../Assets/02.Scripts/GameActor/Component/Player/PlayerGliderBinding.cs): 글라이더 손 위치.
- [Actor asmdef](../../Assets/02.Scripts/GameActor/UPlayGround.Actor.asmdef)에 `MotionWarping.Runtime` 참조가 있다.

전투는 [자체 MotionWarpController](../../Assets/02.Scripts/GameActor/MovementController/MotionWarpController.cs)와 [MotionEvent](../../Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_MotionWarp.cs)를 사용한다. 자체 구현에는 실제 평가된 모션 시간, 타깃 잠금·예측, 접촉 거리, 이동·회전 보정 예산, KCC 이동 결과 추적, 세션 종료 처리가 있다. 구매 컴포넌트를 동시에 붙이면 이동·시간 소유권이 겹칠 수 있다.

## 설치 소스의 주의점

실행 재현 전의 정적 판독이다. 구매 원본은 수정하지 않았다.

1. `MotionWarping.cs:288`은 변위 `delta`를 `Rigidbody.MovePosition`에 전달한다. 해당 API의 위치 인자와 맞지 않아 잘못된 위치 이동의 원인 후보다.
2. 같은 파일의 충돌 경로는 이동 전 Transform을 기준 위치로 바꾼 뒤 CharacterController/Rigidbody에 이동을 요청한다. KCC 경로는 없으며 충돌 동작은 별도 검증이 필요하다.
3. `Play`는 구간 0개를 거절하지 않고 `warpPhases[0]`에 접근한다. 빈 설정의 예외 가능성이 있다.
4. 충돌·물리 상태의 원래 값을 저장하지 않는다. 종료 때 CharacterController를 켜거나 Rigidbody를 kinematic으로 고정하는 경로가 있다.
5. 시간 진행은 자체 `Time.deltaTime`과 Animator `WarpRate` 파라미터에 의존한다. 현재 프로젝트의 Animancer·히트스톱·MotionSet 시간과 자동 동기화되지 않는다.
6. Editor 내부 타입 Reflection과 Generic root 이름 규칙 때문에 현재 Unity/리그별 저작 도구 실행 확인이 필요하다.

## 활용 판단과 검증 범위

전투 시스템 교체의 이득은 현재 조사에서 확인되지 않았다. 장애물 탐색 예제와 다중 접촉점 저작 방식은 파쿠르 확장의 참고 대상으로 유용하다. 실제 도입 시 입력·상태·중단 복구와 KCC 적용을 기존 시스템에 연결하고, 설정 원본은 프로젝트 SO 계약을 유지해야 한다.

수행: 설치 코드·Editor·예제·asmdef 조사, `Assets/02.Scripts` C# 소비자 검색, 공식 소개 확인, 보고서 로컬 링크 존재 확인. Assets 전체 씬·프리팹·SO의 스크립트 GUID 검색은 장시간 소요되어 중단했으므로 직렬화된 컴포넌트의 전수 미사용을 단정하지 않는다. Unity 컴파일·Play·충돌/IK 품질·성능 측정·Player 빌드는 미수행이다. 패키지 버전 식별 정보는 확인하지 못했으며 코드의 2024 저작권 표기를 버전으로 간주하지 않는다. 기존 사용자 변경은 보존했고 런타임 코드·데이터는 변경하지 않았다. 조사 중 생성한 임시 파일은 없다.

공식 참고: [문서](https://kinemation.gitbook.io/motion-warping-for-unity), [데모 저장소](https://github.com/kinemation/motion-warping). 기능·위험 판단은 웹 최신판이 아닌 위 설치 소스를 기준으로 했다.

## KCC 연동 검토 — 2026-11-17

### 판정

**연동 가능. 이동 API 한 곳 교체로는 불충분하며, 목표 자세 평가와 모터 적용을 분리해야 한다.** 현재 프로젝트의 전투 경로는 이미 이 분리를 구현했다. 구매 패키지의 고유한 다구간 파쿠르를 사용하려는 경우에만 별도 연동의 이득이 있다. 이번 검토에서는 코드·프리팹·SO를 변경하지 않았다.

### 확인한 호출 흐름

| 지점 | 실제 동작과 연동 의미 |
|---|---|
| [KCCSimulator](../../Assets/02.Scripts/Manager/KCCSimulator.cs) | FixedUpdate에서 전체 Motor Phase 1 → Mover 확정 → 전체 Motor Phase 2를 실행한다. Phase 1은 baseDt, Phase 2는 baseDt × Actor.LocalTimeScale이다. 기존 장벽과 실행 순서를 유지해야 한다. |
| [ActorMovementController](../../Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs) | BeforeCharacterUpdate에서 루트모션 스텝을 열고, 상태에 회전·속도 콜백을 위임한다. 상태 속도 뒤 중력·외부 충격을 합성하고, AfterCharacterUpdate에서 실제 이동을 기록하고 루트모션 스텝을 닫는다. |
| [PlayerAttackState](../../Assets/02.Scripts/GameActor/State/Player/PlayerAttackState.cs) | GetRootMotionStepVelocity → 자체 워프 EvaluateVelocity → 접근 속도 제한 → 수직 속도 보존. 회전은 RootMotionStepDeltaRotation과 자체 워프 결과로 계산한다. |
| [ActorAnimator](../../Assets/02.Scripts/GameActor/Animation/ActorAnimator.cs) | Animator 평가에서 발생한 루트 델타를 버퍼에 누적하고 KCC 스텝에서 한 번 소비한다. 실제 평가된 MotionSet 시간을 제공한다. |
| [KinematicCharacterMotor](../../Assets/ExternalAssets/Etc/KinematicCharacterController/Core/KinematicCharacterMotor.cs) | MoveCharacter는 다음 Phase 1의 예약 위치 이동이다. 이후 Phase 2에서 상태 속도 이동도 실행된다. UpdateRotation은 UpdateVelocity보다 먼저 호출된다. |

### 연동 방식 비교

| 방식 | 판단 |
|---|---|
| 구매 LateUpdate의 Transform 이동을 Motor.SetPosition으로 교체 | 연속 이동에 부적합. 직접 위치 설정은 충돌 이동 경로가 아니며 기존 보간·물리 권위와 충돌한다. |
| 구매 LateUpdate에서 Motor.MoveCharacter/RotateCharacter 예약 | KCC 단독 예제에서는 검토할 수 있으나 현재 프로젝트에는 비권장. 예약 이동은 LocalTimeScale 미적용 Phase 1에서 처리되고 Phase 2의 기존 상태 이동이 더해진다. 히트스톱·모션 시간 동기화를 별도로 해결해야 한다. |
| 숨은 Transform에 구매 워프를 적용한 뒤 액터가 따라가기 | 실제 충돌 위치와 프록시 궤적이 벌어지고 LateUpdate/FixedUpdate 시간차가 남는다. 실서비스 경로의 기본안으로 권장하지 않는다. |
| 목표 자세 평가를 분리하고 상태의 KCC 콜백에서 소비 | 권장. Actor/KCC/Animancer의 기존 실행 계약을 유지할 수 있다. |
| 기존 자체 워프와 MotionSet을 사용하고 파쿠르 탐색·접촉점 방식만 참고 | 이 프로젝트에서 우선 권장. 두 번째 워프 실행기를 추가하지 않는다. 단, 자체 전투 워프가 매달리기·파쿠르 전체를 이미 지원한다는 뜻은 아니다. |

### 구매 계산기를 사용한다면 필요한 최소 계약

아래는 구현안이며 현재 존재하는 API가 아니다.

1. **입력:** 실제 모터 위치·회전, 이전/현재 평가된 클립 시간, 구간·목표점. 액터 Transform을 임시로 변경하지 않고 위치·회전 연산으로 기준 좌표계를 구성한다. MotionSet 시간은 해당 클립 로컬 시간으로 변환해야 하며 체이닝·속도 변경도 반영한다.
2. **평가:** 한 물리 스텝에 한 번 목표 월드 자세를 계산해 캐시한다. 회전 콜백이 속도 콜백보다 먼저이므로 두 콜백에서 타이머·구간 인덱스를 각각 진행시키면 안 된다. BeforeCharacterUpdate는 시간 증가 없이 준비에 사용할 수 있다.
3. **이동 적용:** Phase 2의 상태 UpdateVelocity에서 `요청 속도 = (목표 위치 - Motor.TransientPosition) / scaledDt`를 사용한다. scaledDt가 0이면 이동·시간 진행·세션 완료를 보류한다. 이것은 계산 원칙이며 속도/오차 상한·축 정책을 적용해야 하는 최종 구현식이다.
4. **회전 적용:** 같은 스텝의 캐시된 목표 회전을 UpdateRotation에 전달한다. 원본 회전과 워프 회전의 합성 순서를 고정한다.
5. **루트 이동 소유:** 구매 곡선이 원본 이동 전체를 내놓는 모드에서는 Animator 원본 델타를 다시 더하지 않는다. 자체 실행기를 사용한다면 현재처럼 실제 Animator 델타를 기준으로 보정만 합성한다. 두 모델을 동시에 켜지 않는다.
6. **시간 소유:** 구매 `_warpPlayback += Time.deltaTime`과 Animator `WarpRate` 제어를 독립 실행하지 않는다. ActorAnimator의 평가된 시간·기존 재생 속도 경로를 사용한다. 긴 프레임에서 여러 구간을 건너거나 마지막 시각에 도달해도 구간 경계와 마지막 샘플을 처리하고 마지막 모터 소비 뒤 완료한다.
7. **실제 결과:** AfterCharacterUpdate에서 요청과 실제 이동의 차이를 확인한다. 막혔을 때 무한 보정·종료 스냅 대신 실패/중단을 반환한다. 현재 CompleteMotorStep에는 전투용 수평 차단 판정이 있으므로 파쿠르의 수직/3D 차단·도착 판정을 그대로 대체할 수 없다.

구매 컴포넌트의 WarpTranslation·WarpRotation·UpdateWarping과 내부 상태는 private이며, 결과만 반환하는 공개 평가 API가 없다. 외부 상속 어댑터 하나로 위 계약을 충족할 수 없고 Reflection으로 매 스텝 내부 메서드를 호출하는 방식도 피해야 한다. 구매 계산기를 직접 활용하려면 계산부를 별도 구현으로 분리하거나 공급 코드에 명시적 평가 API를 추가하는 변경이 필요하다. 구매 원본 변경은 이번 검토에서 시행하지 않았다.

### 파쿠르에서 추가로 결정할 항목

- **전용 상태:** 기존 상태 머신 안에서 입력·중력·워프 수명을 소유한다. 전체 궤적의 Y를 소유하는 구간에는 상태의 GravityOwnership 계약으로 중력 중복을 막고, 이륙 시 ForceUnground와 지면 붙기 정책을 정한다. 기존 일반 공격은 수직 탄도 보존 정책을 유지한다.
- **충돌:** 캡슐을 끄지 않고 통과 가능한 궤적을 저작한다. 출발·중간·도착 캡슐 여유와 천장을 검사한다. 손이 턱에 닿는 애니메이션만으로 액터 캡슐이 벽을 통과할 수 있는 것은 아니다. 목표점은 손 접점과 모터 원점의 차이를 반영해야 한다.
- **움직이는 플랫폼:** 목표점의 로컬/월드 변환과 KCC AttachedRigidbody 이동이 중복되지 않도록 부착·이탈 규칙을 정한다. 첫 검증은 정적 장애물로 한정한다.
- **IK:** 구매 LateUpdate IK를 그대로 추가하지 않는다. 실제 KCC 이동 결과에 맞는 손발 타깃을 기존 모델 평가 순서와 맞춰 적용하고 무기·글라이더 IK의 동시 실행을 막는다.
- **중단:** 피격·사망·목표 삭제·모델 교체·씬 이동에서 세션을 한 번만 종료한다. 낙하 중에는 안전한 공중 상태로 반환하고 위치를 강제 복구하지 않는다. 거리 시뮬레이션 정지와 재개도 세션 정책에 포함한다.
- **데이터:** MotionKey·MotionSet·프로젝트 SO가 실행 정본이다. 구매 MotionWarpingAsset을 같은 공격의 독립 실행 원본으로 추가하지 않는다. 파쿠르만 도입하더라도 구간·좌표 오프셋·중단 정책을 중복 저작하지 않도록 원본을 정한다.

### 권장 작업 범위와 검증

첫 구현 범위는 정적 낮은 턱 한 종류로 제한한다. 기존 MotionSet 재생과 KCC 전용 상태에 목표점 탐색·도착 보정을 연결하고, 구매 계산기가 필요한 차이가 실제로 남을 때만 별도 평가기를 도입한다. 전투 전체 교체·플랫폼·매달리기·자동 손발 IK는 첫 단계에 포함하지 않는다.

검증 항목: (1) 30/60/120 렌더 FPS와 물리 스텝 불일치에서 중복 이동 없음, (2) LocalTimeScale 1/0.1/0과 재개 시 타임라인·위치 일치, (3) 벽/천장/도착 지점 차단 시 관통·종료 스냅 없음, (4) 피격/목표 삭제/상태 이탈 후 정상 낙하·조작 복귀, (5) 경사 출발과 지면 재접촉, (6) 기존 공격·무기/글라이더 IK 회귀. 향후 플랫폼 단계에서는 플랫폼 이동 이중 합산을 추가 검사한다.

현재 판정은 **정적 연동 검토 완료 / 구현·컴파일·자동 테스트·사용자 플레이 미수행**이다. 관련 기존 테스트는 `Assets/Tests/EditMode/Movement/MotionWarpMotorStepTests.cs`, `MotionWarpEndpointTests.cs`, `MotionWarpTargetLifecycleTests.cs`에 있으나 이번에 재실행하지 않았다. Unity·구매 원본·기존 사용자 파일은 변경하지 않았고 추가 임시 부산물은 없다.

## NPC 물건 집기·생활 행동 적용 검토 — 2026-11-17

**적합한 활용처다.** 물건 앞에 서기·몸 방향 맞추기·짧게 다가가기에는 워핑을, 손 접촉에는 모델에 맞는 모션과 제한된 IK를 사용한다. 구매 패키지의 AlignComponent가 상대 좌표 정렬의 참고 예제다. 일반 길찾기와 소품 소유권은 워핑의 책임이 아니다.

### 실제 프로젝트의 재사용 지점

- [NpcRoutineState](../../Assets/02.Scripts/GameActor/State/NPC/NpcRoutineState.cs): 기존 KCC로 경유점·작업점에 접근하고 방향을 맞춘다. 작업 중에는 수평 속도를 0으로 만들며 Animator 루트 이동과 MotionWarp 평가 결과를 소비하는 경로는 없다.
- [NpcRoutine](../../Assets/02.Scripts/GameActor/Component/NPC/LivingWorld/NpcRoutine.cs): 도착 반경(코드 기본값 0.35m)·높이·방향 조건을 만족하면 작업 모션을 재생한다. 이는 정확한 손 접촉 위치 보장과 다르다. 실제 인스턴스의 직렬화 값은 별도 확인이 필요하다.
- [NpcWorkPoint](../../Assets/02.Scripts/GameActor/Component/NPC/LivingWorld/NpcWorkPoint.cs): 작업점 예약·세대 토큰·실물 소품과 소유자를 관리한다. TryTakeSceneProp은 실물의 월드 자세를 보존해 손에 부착하므로, 손이 빗나간 상태에서 실행해도 오차를 자동 보정하지 않는다.
- [NpcWorkPropEvent](../../Assets/02.Scripts/GameActor/Animation/MotionEvents/MotionEvent_NpcWorkProp.cs): 포즈 평가 후 접촉 프레임에 집기/놓기를 발화한다. 기존 예약된 일과 소품을 대상으로 하며 임의 월드 아이템 획득 시스템 전체를 뜻하지 않는다.
- 보유 후보 `Assets/ExternalAssets/AnimationOnly/VanillaLoopStudio/FreeSampleAnimationSet/Art/Animations/ItemPickupSet/Mannequin/A_ItemPickup_fromIdle_RH_100cm.fbx`의 존재를 확인했다. 이름만으로 지면 집기·모든 체형 적합성을 판정하지 않으며 실제 모델에서 확인해야 한다.

### 권장 행동 흐름

1. 기존 생활 시스템으로 작업점/대상을 예약하고 접근한다.
2. 대상의 **손 접촉점**과 NPC의 **서 있을 위치·방향**을 구분한다. 원본 모션의 접촉 시점 손 위치를 기준으로 몸의 목표 자세를 저작/계산한다. 물건 중심으로 NPC 루트를 이동시키지 않는다.
3. 접촉 전에 짧은 정렬 구간을 두어 기존 KCC 콜백에서 수평 위치·회전을 제한적으로 보정한다. 접지 후 발이 미끄러지지 않게 보정을 끝내고, 큰 오차는 재접근/실패 처리한다.
4. 높이·거리·체형에 맞는 집기 모션을 재생하고 작은 손 오차만 IK로 보완한다. 바닥·탁자·선반 높이를 하나의 모션에 무제한 워핑하지 않는다. 양손 물건·손가락 파지는 별도 저작이 필요하다.
5. 실제 접촉 프레임에서 기존 NpcWorkPropEvent로 같은 소품을 인계한다. 물건이 손에 붙은 뒤에는 이동한 물건을 몸 워프의 추적 대상으로 사용하지 않는다.
6. 작업·놓기·종료를 진행한다. 대화·위험·대상 삭제·중단에서는 기존 예약 세대와 소품 수명 정책을 지킨다.

### 구현 범위 판단

현재 MotionWarp 이벤트가 공용 컨트롤러에 구간을 열 수 있어도 NpcRoutineState가 결과를 소비하지 않으므로 이벤트 추가만으로 NPC가 워핑하지 않는다. NPC 행동의 정렬 구간과 KCC 소비 경로가 필요하다. 생활 동작을 PlayerCombat/EnemyCombat 또는 GAS 공격으로 우회하지 않는다.

첫 적용은 **고정 작업대의 작은 소품 하나를 집었다 놓는 기존 일과**가 적합하다. 기존 예약·인계 기능은 유지하고 정렬/접촉만 보완한다. 이후 임의로 떨어진 아이템을 실제로 수집하는 행동은 대상 검색·경로·예약 경쟁·획득 확정·중단/저장 시 소유권을 별도로 정의해야 한다. 기존 작업 소품 인계가 아이템 재고·퀘스트 획득까지 처리한다고 간주하지 않는다.

검증은 도착 오차가 다른 시작 위치, 실제 NPC 체형/의상, 손 접촉과 발 미끄러짐, 두 NPC의 같은 물건 경쟁, 접촉 전후 대화/위험 중단, 소품 제거·재개를 포함한다. 정적 코드 검토만 수행했으며 모델별 시연·IK 품질·런타임 테스트는 미수행이다. 신규 에셋·런타임 변경·임시 부산물은 없다.
