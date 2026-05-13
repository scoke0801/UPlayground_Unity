# 모션워핑 개선 설계

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
