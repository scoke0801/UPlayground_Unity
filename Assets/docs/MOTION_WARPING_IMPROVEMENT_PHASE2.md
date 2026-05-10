# 모션워핑 2차 개선 설계

> 작성일: 2026-05-10
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 선행 문서: [MOTION_WARPING_IMPROVEMENT_DESIGN.md](MOTION_WARPING_IMPROVEMENT_DESIGN.md) (1차 설계, 1~5단계 완료)
> 현재 상태: `MotionEvent_MotionWarp.MotionWarpEnabled = false`로 워프 기능 전역 비활성화. 본 문서의 Phase 1을 마치는 시점에 다시 활성화한다.

---

## 목적

1차 설계로 `MotionWarpController` 공통 컨트롤러, Additive/Scale/Skew modifier, Snapshot/Live 정책, 프리셋, 디버그 오버레이가 도입되어 플레이어/적 모두 같은 경로로 워프가 동작한다. 이후 실제 전투 튜닝 과정에서 다음 한계가 누적 관측되었다.

- 락온 우선순위 부재로 락온 중에도 다른 적이 더 가까우면 그쪽으로 빨려 들어간다.
- 워프 도중 타겟이 사망/이탈/넉백되면 깔끔히 종료되지 못하고 너덜너덜한 보간으로 공격이 끝난다.
- 회전 보정이 시간 상수 두 단계(0.15s 기준 25 → 8)로 하드코딩되어 모션 길이/거리에 따라 어색해진다.
- 한 공격 안에서 여러 워프 구간을 의미 있게 연결할 수 없다 (예: 도약 → 착지 → 마무리 정렬).
- 빠르게 움직이는 타겟에 대해 Snapshot은 빗나가고 Live는 떨린다 — 예측(prediction) 단계가 비어 있다.
- 데이터 모델이 `Combat`(타이머·속성), `MotionWarpController`(설정·타겟), `MotionEvent_MotionWarp`(이벤트 옵션) 세 곳에 분산되어 진실 소스가 셋이다.

본 문서는 1차 설계의 골격을 유지한 채 위 한계를 단계적으로 해소하는 후속 개선안이다.

---

## 현재 구현 진단

### 진실 소스 분산

| 항목 | 위치 | 현재 의도 |
|------|------|-----------|
| `IsMotionWarping`, `WarpRemainingTime`, `WarpDuration` | `PlayerCombat`, `EnemyCombat` | Combat이 보유한 카운트다운 타이머 |
| 워프 윈도우 설정 (`MotionWarpWindowSettings`), 타겟, blend 가중치 | `MotionWarpController` (`ActorMovementController` 내부) | 보정 알고리즘 입력 |
| 윈도우 시작·종료 시점, 프리셋 매핑 | `MotionEvent_MotionWarp` | 타임라인 기반 발화 |

`AttackState.UpdateVelocity`는 셋 모두를 인자로 호출해야 한다 (`PlayerAttackState.cs` UpdateVelocity 본문 참조). 한 곳만 desync되면 침묵 실패한다.

### Feasibility 판정 비대칭

`MotionWarpController.EvaluateVelocity`(`ActorMovementController.cs:481`)는 첫 프레임에만 `outOfRange`/`unreachable`을 검사하고 cancel 콜백을 호출한다. 이후 프레임에서 OOR이 되면 `_blendWeight`만 감쇠시킬 뿐 종료 신호를 외부에 보내지 않아, 상태머신은 워프가 진행 중이라고 믿는다.

### 회전 처리

`PlayerAttackState.UpdateRotation`(`PlayerAttackState.cs:426` 부근)은 `_attackTimer < 0.15f`일 때 25, 이후 8로 회전 보간 속도를 분기한다. 클립 길이/총 워프 시간/현재 진행도와 무관한 절대값이라 모션마다 다르게 어색하다.

### Y축 정책

`MotionWarpWindowSettings.ignoreY`가 사실상 항상 `true`로 들어온다(프리셋 모두 `ignoreY = true`). 점프 마무리, 공중 처형, 비행 적 추격에서는 Y 보정이 필요한데 옵션이 닫혀 있다.

### 타겟 결정

`PlayerCombat.FindAttackSnapTarget`(`PlayerCombat.cs:989`)이 콘 안 최근접만 선택한다. `CameraManager.GetLockOnTarget`은 `UpdateRotation` 단계에서 워프가 비활성일 때만 참조된다. 즉 락온 중이라도 워프가 활성이면 락온 타겟은 무시된다.

### 단일 타겟·단일 윈도우 가정

`MotionWarpController`가 `Transform _target` 하나만 보관한다. 한 모션셋 안에 워프 이벤트를 두 개 두면 두 번째 이벤트가 첫 이벤트의 타겟을 덮어쓴다. UE5 SyncPoint처럼 타겟 키로 식별하는 모델이 부재.

---

## 외부 사례 요약

| 출처 | 핵심 |
|------|------|
| [UE5 Motion Warping Docs](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine) | Notify State 구간 + 이름 키 모델. `AddOrUpdateWarpTargetFromComponent`로 런타임 갱신, `FollowComponent` 옵션. 한 몽타주에 다중 SyncPoint 지원. |
| [UE5 SkewWarp 모디파이어](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/MotionWarping/AddRootMotionModifierSkewWarp) | 남은 루트모션을 스케일·전단 변환으로 재맵. translation/rotation 분리 가중치, ZAxis 무시 옵션. |
| [Quod Soler 가이드](https://www.quodsoler.com/blog/motion-warping-character-attacks-using-blueprints-no-c-required) | 입력 → Sphere Trace로 후보 → 최단거리 선정 → AddOrUpdate → Notify가 알아서 워프. |
| [UE 포럼: Moving Targets](https://forums.unrealengine.com/t/motion-warping-and-moving-targets/612472) | Live 정책 단독 사용 시 떨림. Follow + 리드타임 + 클램핑이 표준. |
| [Kinemation Motion Warping for Unity](https://kinemation.gitbook.io/motion-warping-for-unity/concept/how-this-asset-works) | LateUpdate에서 루트모션 델타 누적 / 총합 비율을 알파로. 회전은 정규화 시간 SLERP. |
| [Kinemation 자산 필드](https://kinemation.gitbook.io/motion-warping-for-unity/fundametals/motion-warping-asset) | `Phases Amount`로 멀티 워프 포인트(점프-착지-마무리). 페이즈마다 T/R Offset 분리. |
| [Soulslike Framework 가이드](https://soulslike-framework.isik.vip/extending-functionality/using-motion-warping) | 어빌리티 시작 시 update, 종료/캔슬 시 명시 clear. Notify State 구간 한정 적용 권장. |

---

## 개선 항목

### Phase 1 — 데이터 모델 통합 (호환성 유지 리팩터)

목표: 진실 소스 셋을 컨트롤러 한 곳으로 모은다. 이 단계가 끝난 시점에 `MotionWarpEnabled` 토글을 다시 켠다.

- `MotionWarpTarget` 구조체 신설.
  - 필드: `Transform anchor`, `Vector3 offset`, `WarpTargetSpace space (World / AnchorLocal / AnchorForward)`, `bool follow`.
  - 기존 `_target`/`_targetPosition`/`targetOffset` 파편을 흡수.
- `IsMotionWarping`, `WarpRemainingTime`, `WarpDuration`을 `MotionWarpController`로 이전.
  - `Combat`은 호환 프록시 프로퍼티만 남기고 점진적으로 호출지 제거.
  - `AttackState.UpdateVelocity`/`UpdateRotation`이 컨트롤러 한 곳만 본다.
- `IWarpTargetResolver` 인터페이스 추출. 기본 구현 두 가지:
  - `ConeNearestResolver` — 현재 `FindAttackSnapTarget` 동작.
  - `LockOnFirstResolver` — `CameraManager.GetLockOnTarget()` 우선, 없으면 콘 후보 fallback.
- `MotionEvent_MotionWarp`에 `resolverPolicy` enum 필드 추가. 기본은 프로젝트 결정 포인트(아래) 결과대로.

### Phase 2 — 취소·Cleanup 견고화

목표: 워프 도중 상황 변화에 대한 명시적 종료 경로를 만든다.

- `EvaluateVelocity` 매 프레임 OOR/도달불가 재검증. 임계 누적 시간(예: 0.1s) 초과 시 명시 종료.
- `MotionWarpController.OnWarpCancelled` 이벤트 신설. 발생 시점:
  - 거리 임계 누적 초과
  - `Combat.EndMotionWarp` 외부 호출 (자기 Hit/KnockBack/사망)
  - 타겟 anchor 파괴/사망 (`IDamageable.IsDead` 체크)
  - 캐릭터 교체 (`PartyManager.SwapCharacter`)
- `AttackState`에 `OnWarpCancelled` 핸들러 추가. 캔슬 후속 모션 정책은 결정 포인트(아래).
- `Hit`/`KnockBack` 상태 진입 시 컨트롤러 자동 clear.

### Phase 3 — 회전·Y축 정확도

목표: 시간 상수 하드코딩 제거, Y 보정 옵션 개방.

- `MotionEvent_MotionWarp`에 `AnimationCurve rotationCurve` 추가. 정규화 t (0~1)를 회전 보간 알파로 사용.
- 기본 곡선 프리셋:
  - `LightAttack`: 빠른 EaseOut (앞부분 강하게 정렬)
  - `HeavyAttack`: 느린 EaseIn-Out (무게감)
  - `FinishAttack`: 거의 정확히 일치 (마지막 프레임에 1.0)
- Y축 정책 enum 도입:
  - `IgnoreY` (현재 동작)
  - `MatchTargetY` (점프 마무리 등)
  - `ProjectToTargetY` (지면 높이 차 흡수)
- `ignoreY` bool은 enum의 호환 매핑으로 유지.

### Phase 4 — Predictive Live & 멀티 타겟 키

목표: 빠른 타겟에 대한 추적 품질, 다단 워프 지원.

- `MotionWarpTargetPolicy.Predictive` 추가.
  - `targetVelocity * predictionFactor * remainingTime`을 anchor 위치에 가산.
  - `predictionFactor`는 0~1 사이, 프리셋별 기본값(Grab=0.6 등).
- `MotionWarpController._target` 단일 → `Dictionary<string, MotionWarpTarget>` 다중.
  - `MotionEvent_MotionWarp.targetKey` (string) 추가. 기본 `"primary"`.
  - 같은 키를 가진 두 이벤트는 같은 타겟을 공유 (도약-착지 시퀀스).
  - 다른 키를 가지면 별도 타겟 (예: 환경 오브젝트 → 적).

### Phase 5 — 워크플로우·디버깅

목표: 튜닝 사이클 단축. (1차 설계의 5단계 디버그 오버레이를 확장)

- SceneView Gizmo: 액티브 윈도우 동안 anchor 라인, min/max 디스크, 도달가능영역 콘, EaseOut 진행도 바, predictive 가산점.
- `MotionEvent_MotionWarp` 인스펙터 검증 버튼: 가상 타겟을 콘 정중앙에 두고 실시간 시뮬 결과(예상 도착점, 실패 사유) 표시.
- `ActorMonitor` 확장 패널 (GameplayTag/ComboSequence 패턴 참조)로 활성 워프 키, 타겟, blend, OOR 누적 시간 노출.
- `MotionWarpEnabled` const 토글 제거. SettingsManager 디버그 옵션으로 승격.

---

## 단계별 우선순위

```
Phase 1 (데이터 통합)
  └─▶ Phase 2 (취소·Cleanup)
        └─▶ Phase 3 (회전·Y)
              └─▶ Phase 4 (Predictive·다중 키)
                    └─▶ Phase 5 (디버깅 툴)
```

이유:

1. 데이터 모델이 분산된 채 윈도우 모델을 확장하면 desync 부채가 늘어난다.
2. 취소가 견고해야 새 기능을 자신 있게 추가할 수 있다.
3. Phase 4는 게임플레이 임팩트가 크지만 견고한 기반 위에서만 빛난다.
4. Phase 5는 Phase 1~4 작업 중에도 부분적으로 도움이 되지만, 후반에 한 번에 정리하는 편이 효율적이다.

---

## 결정 필요 포인트

Phase 1 착수 전에 확정이 필요한 항목.

### 1. 락온 타겟 우선 정책

| 안 | 동작 | 적합 게임 톤 |
|----|------|-------------|
| A. 락온 강제 (Sekiro/Souls 스타일) | 락온이 켜져 있으면 항상 락온 타겟으로 워프, 콘은 무시 | 무겁고 정확한 1대1 |
| B. 콘 우선 (DMC 스타일) | 콘 안 최근접을 우선, 락온은 fallback | 다대일, 군중 처리 |
| C. 하이브리드 | 락온 타겟이 콘 안에 있으면 락온, 밖이면 콘 후보 | 절충형 |

기본 추천은 C. Bokusei가 솔로 액션이고 `EnemyCombatStyle`이 다대일 상황을 가정하므로.

### 2. 캔슬 후속 모션

| 안 | 동작 |
|----|------|
| A. 헛스윙 마무리 | 그 자리에 잔여 루트모션만 재생 |
| B. 즉시 Idle 복귀 | 캔슬 즉시 `PlayerIdleState`로 전이 |
| C. 잔여 루트모션 + 페이드 | 0.15~0.2s에 걸쳐 잔여 모션을 약화시키며 재생 |

기본 추천은 C. A는 어색하고, B는 끊겨 보인다.

### 3. 멀티 타겟 키 도입 시점

| 안 | 동작 |
|----|------|
| A. Phase 1에 같이 도입 | 데이터 모델 정리 시 Dictionary로 직행 |
| B. Phase 4까지 단일 키 | 현재 시나리오 충분, 필요 시점에 확장 |

기본 추천은 B. 현재 보스 1 vs 플레이어 1 시나리오에서는 단일 키로 충분하고, 멀티 키는 점프-착지 같은 다단 모션을 만들 때 도입한다.

---

## 검증 기준

각 Phase 종료 시점에 다음 항목으로 회귀 점검.

### Phase 1

- 기존 `Light/Heavy/Finish/Grab` 프리셋 워프 결과가 1차 설계 종료 시점과 동일한 프레임 단위 거리·각도로 재현된다.
- `Combat.IsMotionWarping`을 외부에서 직접 참조하는 호출지가 0이다.
- `MotionWarpEnabled` 토글을 다시 `true`로 켜도 회귀가 없다.

### Phase 2

- 워프 도중 타겟 사망 시 `OnWarpCancelled` 발화 → 후속 모션 정책에 맞춰 종료한다.
- 자신의 Hit 진입 시 컨트롤러가 자동 clear된다.
- `MotionWarpController`가 활성 윈도우 없이 cancel 신호를 두 번 보내지 않는다 (idempotent).

### Phase 3

- 동일 프리셋의 짧은 클립과 긴 클립이 회전 진행률 곡선상 동일 비율 위치에서 같은 정렬도를 보인다.
- `MatchTargetY` 옵션이 점프 마무리 모션의 착지점을 ±0.05m 이내로 정렬한다.

### Phase 4

- `Predictive` 정책으로 `EnemyMovementController` 주행 적을 워프 시 도착 오차가 Live 단독 대비 50% 이상 감소한다.
- 두 개의 다른 키를 사용하는 이벤트가 한 모션셋 안에서 서로 간섭 없이 동작한다.

### Phase 5

- `ActorMonitor`에서 활성 워프 키/타겟/blend/실패사유를 한 화면에서 볼 수 있다.
- `MotionEvent_MotionWarp` 인스펙터 검증 버튼으로 실패 사유와 도착 오차를 즉시 확인할 수 있다.

---

## 참고 링크

- [UE5 Motion Warping 공식 문서](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine)
- [UE5 SkewWarp 모디파이어](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/MotionWarping/AddRootMotionModifierSkewWarp)
- [Quod Soler — Motion Warping Blueprint Guide](https://www.quodsoler.com/blog/motion-warping-character-attacks-using-blueprints-no-c-required)
- [UE Forums — Motion Warping and Moving Targets](https://forums.unrealengine.com/t/motion-warping-and-moving-targets/612472)
- [Kinemation Motion Warping for Unity — How it Works](https://kinemation.gitbook.io/motion-warping-for-unity/concept/how-this-asset-works)
- [Kinemation Motion Warping for Unity — Asset Fields](https://kinemation.gitbook.io/motion-warping-for-unity/fundametals/motion-warping-asset)
- [Soulslike Framework — Using Motion Warping](https://soulslike-framework.isik.vip/extending-functionality/using-motion-warping)
