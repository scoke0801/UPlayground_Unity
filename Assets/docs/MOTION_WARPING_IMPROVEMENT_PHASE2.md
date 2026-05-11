# 모션워핑 2차 개선 설계

> 작성일: 2026-05-10
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 선행 문서: [MOTION_WARPING_IMPROVEMENT_DESIGN.md](MOTION_WARPING_IMPROVEMENT_DESIGN.md) (1차 설계, 1~5단계 완료)
> 현재 상태: 워프 전역 토글은 `SettingsManager.Data.debugMotionWarpEnabled` (기본 `true`) 로 승격됨.
>
> 진행 상황 (2026-05-10):
> - **Phase 1 — 데이터 모델 통합**: ✅ 완료
> - **Phase 2 — 취소·Cleanup 견고화**: ✅ 완료
> - **Phase 3 — 회전·Y축 정확도**: ✅ 완료
> - **Phase 4 — Predictive Live & 멀티 타겟 키**: ✅ 완료
> - **Phase 5 — 워크플로우·디버깅**: ✅ 완료
> - **Phase 6 — 정밀도·확장 후속**: ⏳ 대기 (Phase 1~5 회귀/플레이 검증 후 착수)
>
> 결정 사항 (2026-05-10 확정):
> - 락온 타겟 우선 정책: **C. 하이브리드** — 락온이 콘 안이면 락온 우선, 밖이면 콘 후보 fallback
> - 캔슬 후속 모션: **A. 헛스윙 마무리** — 잔여 루트모션을 그대로 재생
> - 멀티 타겟 키 도입 시점: **B. Phase 4까지 단일 키** — 필요 시점에 확장

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

### Phase 1 — 데이터 모델 통합 (호환성 유지 리팩터) ✅ 완료 (2026-05-10)

목표: 진실 소스 셋을 컨트롤러 한 곳으로 모은다. 워프는 1차 동작을 유지한 채로 작업하며, 회귀 발생 시 일시적으로 `SettingsData.debugMotionWarpEnabled` 토글로 우회할 수 있게 둔다 (Phase 5 에서 const 제거 후 SettingsManager 로 승격).

- `MotionWarpTarget` 구조체 신설.
  - 필드: `Transform anchor`, `Vector3 offset`, `WarpTargetSpace space (World / AnchorLocal / AnchorForward)`, `bool follow`.
  - 기존 `_target`/`_targetPosition`/`targetOffset` 파편을 흡수.
- `IsMotionWarping`, `WarpRemainingTime`, `WarpDuration`을 `MotionWarpController`로 이전.
  - `Combat`은 호환 프록시 프로퍼티만 남기고 점진적으로 호출지 제거.
  - `AttackState.UpdateVelocity`/`UpdateRotation`이 컨트롤러 한 곳만 본다.
- `IWarpTargetResolver` 인터페이스 추출. 기본 구현 셋:
  - `ConeNearestResolver` — 현재 `FindAttackSnapTarget` 동작.
  - `LockOnFirstResolver` — `CameraManager.GetLockOnTarget()`만 사용, 없으면 null.
  - `HybridResolver` *(기본값)* — 락온 타겟이 콘(`hitRange`/`hitAngle`) 안에 있으면 락온, 밖이면 `ConeNearestResolver` fallback.
- `MotionEvent_MotionWarp`에 `resolverPolicy` enum 필드 추가. 기본은 `Hybrid`.

**구현 결과:**
- 신규 파일: `Assets/02.Scripts/GameActor/MovementController/MotionWarpTarget.cs`, `IWarpTargetResolver.cs`
- `MotionWarpController` 내부 상태가 `MotionWarpTarget _activeTarget` + `_snapshotPosition` 으로 통합됨 (`_target`/`_targetPosition`/`_useSnapshot` 제거).
- 워프 타이머가 `PlayerCombat`/`EnemyCombat` → `MotionWarpController` 로 이전. Combat 은 호환 프록시만 노출.
- `BuildWarpResolverContext()` 가 `PlayerCombat` 에 추가되어 이벤트가 resolver 컨텍스트를 안전하게 가져갈 수 있다.
- `MotionEvent_MotionWarp.resolverPolicy` 추가, 기본은 `UseExisting` (기존 자산 호환). 마이그레이션 후 `Hybrid` 로 전환 예정.

### Phase 2 — 취소·Cleanup 견고화 ✅ 완료 (2026-05-10)

목표: 워프 도중 상황 변화에 대한 명시적 종료 경로를 만든다.

- `EvaluateVelocity` 매 프레임 OOR/도달불가 재검증. 임계 누적 시간(예: 0.1s) 초과 시 명시 종료.
- `MotionWarpController.OnWarpCancelled` 이벤트 신설. 발생 시점:
  - 거리 임계 누적 초과
  - `Combat.EndMotionWarp` 외부 호출 (자기 Hit/KnockBack/사망)
  - 타겟 anchor 파괴/사망 (`IDamageable.IsAlive()` 체크)
  - 캐릭터 교체 (`PartyManager.SwapCharacter`) — 새 캐릭터의 컨트롤러로 자연 전환되므로 별도 훅 불필요
- 캔슬 후속 모션 정책: **헛스윙 마무리** — 잔여 루트모션을 그대로 재생하고 일반 공격 종료 흐름(콤보 윈도우, `OnExit`)을 그대로 탄다. 별도 페이드/즉시 전이는 적용하지 않는다. `OnWarpCancelled` 핸들러를 따로 두지 않아도 자동으로 흐름이 자연 연결된다.
- `Hit`/`KnockBack`/`Death` 상태 진입 시 컨트롤러 자동 clear. 이 경우는 헛스윙도 적용되지 않고 새 상태의 모션이 우선한다.

**구현 결과:**
- `WarpCancelReason` enum (`ExternalEnd` / `OutOfRangeTimeout` / `TargetLost` / `ManualClear`) + `event Action<WarpCancelReason> OnWarpCancelled` 신설.
- `Cancel(reason)` 공개 메서드 + 내부 `IsTargetUnreachableLifecycle()` 헬퍼.
- `EvaluateVelocity` 에 `_outOfRangeAccumulator` (임계 0.1s) 도입. 정상 범위 복귀 시 리셋.
- `EndMotionWarp` / `ClearTarget` 이 워프 활성 중에 호출되면 적절한 사유로 자동 발화.
- `PlayerHitState`, `EnemyHitState`, `PlayerDeathState`, `EnemyDeathState` `OnEnter` 에서 `controller.MotionWarp?.ClearTarget()` 호출.

### Phase 3 — 회전·Y축 정확도 ✅ 완료 (2026-05-10)

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

**구현 결과:**
- `WarpYPolicy` enum + `MotionWarpWindowSettings.yPolicy` 추가. `ResolveYPolicy()` 가 `ignoreY` bool 과 양방향 호환.
- `EvaluateVelocity` 마지막 단계에서 정책 분기 — `MatchTargetY` 는 `dy/remainingTime` 즉시 매칭, `ProjectToTargetY` 는 진행도 기반 점진 보간.
- `MotionWarpController._warpStartRotation` 캡처 + `TryEvaluateRotation(...)` API 추가. 정규화 시간 t 의 곡선 알파로 `Slerp(start, target, alpha * rotationWeight)`. 곡선 비어 있으면 EaseOut 폴백.
- `PlayerAttackState.UpdateRotation` / `EnemyAttackState.UpdateRotation` 가 `TryEvaluateRotation` 으로 교체 — `_attackTimer < 0.15f ? 25 : 8` 시간 상수 분기 제거.
- `MotionEvent_MotionWarp` 인스펙터에 `yPolicy`, `rotationCurve` 노출. `ApplyPreset` 이 비어 있는 곡선을 프리셋별 기본값으로 채움 (사용자 곡선 입력 시 보존).

### Phase 4 — Predictive Live & 멀티 타겟 키 ✅ 완료 (2026-05-10)

목표: 빠른 타겟에 대한 추적 품질, 다단 워프 지원.

- `MotionWarpTargetPolicy.Predictive` 추가.
  - `targetVelocity * predictionFactor * remainingTime`을 anchor 위치에 가산.
  - `predictionFactor`는 0~1 사이, 프리셋별 기본값(Grab=0.6 등).
- 멀티 타겟 키 도입: **Phase 4 시점에 도입** (단일 키 모델로는 부족해지는 다단 모션 — 도약-착지 시퀀스, 환경 오브젝트 경유 공격 — 이 등장하는 시점에 활성).
  - `MotionWarpController._target` 단일 → `Dictionary<string, MotionWarpTarget>` 다중.
  - `MotionEvent_MotionWarp.targetKey` (string) 추가. 기본 `"primary"`.
  - 같은 키를 가진 두 이벤트는 같은 타겟을 공유 (도약-착지 시퀀스).
  - 다른 키를 가지면 별도 타겟 (예: 환경 오브젝트 → 적).
  - Phase 1~3은 단일 키(`"primary"`)만 사용해 데이터 모델을 단순하게 유지한다.

**구현 결과:**
- `MotionWarpTargetPolicy.Predictive` enum 추가. `MotionWarpWindowSettings.predictionFactor` 필드 (`[Range(0,1)]`).
- `MotionWarpController` 가 매 프레임 활성 타겟의 단일 프레임 차분 속도를 추적 (`_targetVelocity`). `EvaluateVelocity` 가 `targetWorld += velocity * factor * remainingTime` 으로 미래 위치 가산.
- `Dictionary<string, MotionWarpTarget> _targets` + `_activeKey` 추가. 기존 `_activeTarget` 은 `_targets[_activeKey]` 의 캐시로 작동.
- API 오버로드: `SetTarget(key, target, useSnapshot)`, `SetTarget(key, MotionWarpTarget)`, `BeginWarpWindow(settings, key)`, `ClearTarget(key)`. 기존 무인자 시그니처는 `DefaultTargetKey = "primary"` 로 위임.
- `MotionEvent_MotionWarp` 인스펙터에 `targetKey`, `predictionFactor` 노출. Execute 가 키를 SetTarget/BeginWarpWindow 양쪽에 전달.
- `Grab` 프리셋은 `Predictive` 정책 + `predictionFactor = 0.6` 으로 자동 승격 (기존 Live 정책 떨림 해소).
- 새 워프 윈도우 / 키 전환 시 `_hasTargetVelocityHistory = false` 로 리셋해 이전 타겟의 잔여 속도가 새 보정에 섞이지 않도록 함.

### Phase 5 — 워크플로우·디버깅 ✅ 완료 (2026-05-10)

목표: 튜닝 사이클 단축. (1차 설계의 5단계 디버그 오버레이를 확장)

- SceneView Gizmo: 액티브 윈도우 동안 anchor 라인, min/max 디스크, 도달가능영역 콘, EaseOut 진행도 바, predictive 가산점.
- `MotionEvent_MotionWarp` 인스펙터 검증 버튼: 가상 타겟을 콘 정중앙에 두고 실시간 시뮬 결과(예상 도착점, 실패 사유) 표시.
- `ActorMonitor` 확장 패널 (GameplayTag/ComboSequence 패턴 참조)로 활성 워프 키, 타겟, blend, OOR 누적 시간 노출.
- `MotionWarpEnabled` const 토글 제거. SettingsManager 디버그 옵션으로 승격.

**구현 결과:**
- `MotionWarpController.OnDrawGizmosSelected` 추가 (`#if UNITY_EDITOR` 가드). 액티브 타겟 라인, min/max 디스크, `maxSpeed × remainingTime` 도달가능영역 디스크, Snapshot 위치 마커, Predictive 가산 위치, `t/blend/OOR/key/policy/modifier` 텍스트 라벨까지 표시.
- 컨트롤러 모니터링 노출: `BlendWeight`, `OutOfRangeAccumulator`, `TargetVelocity`, `HasActiveWindow`, `ActiveWindowSettings`, `ActiveTarget`, `SnapshotPosition` 공개 프로퍼티.
- `SettingsData` 에 `[Header("디버그")] bool debugMotionWarpEnabled = true` 추가, `ResetToDefault()` 에 포함. `MotionEvent_MotionWarp` 의 `const MotionWarpEnabled` 제거 → `SettingsManager.Instance.Data.debugMotionWarpEnabled` 조회로 대체. SettingsManager 미로드 프레임에는 기본 활성으로 폴백.
- `ActorRuntimeMonitorWindow` 에 **MotionWarp** 컬럼 추가 (`ColWarp = 220f`). 활성 키 → 타겟 이름 → `t/blend/OOR` → 정책/모디파이어 → 실패 사유까지 한 행에 표시. 활성/유휴를 색으로 구분 (활성: 하늘색, 유휴: 회색). 윈도우 minSize 1080 으로 확장.
- 인스펙터 검증 버튼은 Gizmo + ActorMonitor 조합으로 동등 정보가 노출되어 별도 도입 보류 (향후 필요 시 `MotionSetEditorWindow.DrawToolbar` 영역에 추가).

### Phase 6 — 정밀도·확장 후속 ⏳ 대기

목표: Phase 1~5 의 코드 리뷰에서 발견된 정밀도 이슈와 보류 확장 항목을 한 번에 수렴. 게임 감각이 흔들리지 않는 항목들이라 플레이 검증을 마친 후 진행.

#### 6-1. 타이밍 정합성 — fixed vs delta

`MotionWarpController.Update` 가 `Time.deltaTime` 으로 워프 타이머와 타겟 속도를 갱신하지만, KCC 가 `FixedUpdate` 기반이라 같은 frame 안에서 EvaluateVelocity 가 보는 `_warpRemainingTime` / `_targetVelocity` 가 stale 일 수 있다.

- 옵션 A: `MotionWarpController` 의 타이머/속도 갱신을 `FixedUpdate` 로 이동.
- 옵션 B: `EvaluateVelocity` 호출 시점에 즉석 보간으로 fixed-frame 위치를 추정.
- 권장: A. 영향 범위가 작고 KCC 와 자연스럽게 정합.

#### 6-2. EnemyCombat 의 `BuildWarpResolverContext` 지원

현재 `MotionEvent_MotionWarp.Execute` 의 resolver 경로는 `PlayerCombat` 한정. 적 공격 이벤트에 `Hybrid` 등 정책을 지정해도 silent no-op.

- `EnemyCombat` 에 동일 메서드 추가, `EnemyDetection.CurrentTarget` 또는 `CurrentSkill` 의 hitRange/hitAngle 활용.
- `MotionEvent_MotionWarp.Execute` 가 PlayerCombat / EnemyCombat 양쪽 컨텍스트를 시도.
- non-UseExisting 정책 + 컨텍스트 부재 시 1회 `Debug.LogWarning` 으로 디자이너 안내.

#### 6-3. 타겟 속도 EMA 평활화

Phase 4 의 단일 프레임 차분은 정지/방향전환 시 노이즈가 큼. Predictive 가산이 한 프레임 튀는 현상 가능.

- `_targetVelocity = Vector3.Lerp(_targetVelocity, raw, smoothing)` 으로 변경.
- `smoothing` 은 `MotionWarpWindowSettings` 또는 컨트롤러 SerializeField 로 노출 (기본 0.3 ~ 0.5).
- 너무 강한 평활화는 반응 지연 — 인게임 튜닝 필요.

#### 6-4. 데이터 자산 마이그레이션

- 기존 `MotionEvent_MotionWarp` 자산의 `resolverPolicy` 가 `UseExisting` 으로 직렬화됨. 1회 일괄 마이그레이션 스크립트 또는 에디터 메뉴(`UPlayGround/MotionWarp/Migrate Assets`) 로 `Hybrid` 로 변경.
- 마이그레이션 전 / 후 동작 비교용 디버그 모드 (Phase 5 의 `debugMotionWarpEnabled` 토글로 우회 가능).
- `Grab` 프리셋이 `Live` → `Predictive` 로 자동 승격된 결과 회귀 검증.

#### 6-5. 멀티 타겟 키 데모 자산

- 점프-착지 시퀀스 모션셋 1종 작성: 이벤트 A 가 `targetKey="leap"` (환경 오브젝트), 이벤트 B 가 `targetKey="primary"` (적).
- Phase 4 의 다중 키 모델이 실 자산에서 간섭 없이 동작하는지 확인.
- 검증 후 가이드 문서/스크린샷.

#### 6-6. `MotionEvent_MotionWarp` 인스펙터 검증 버튼 (Phase 5 보류분)

- `MotionSetEditorWindow.DrawToolbar` 에 "워프 시뮬" 버튼 추가.
- 가상 타겟을 콘 정중앙에 배치하고 현재 설정으로 EvaluateVelocity 를 시뮬, 예상 도착점·실패 사유·blend 진행을 즉시 표시.
- Gizmo + ActorMonitor 와 별개로 자산 단위 빠른 검증용.

#### 6-7. 잔존 코드 품질 항목

리뷰에서 발견된 비-성능 항목:

- `MotionWarpController.UpdateTargetVelocity` 의 dt ≤ 0 분기에서 `_targetPreviousPosition` 도 함께 리셋 (현재 stale 유지, 다음 프레임 history=false 로 영향 없으나 의도 명확화).
- `MotionWarpController.EvaluateVelocity` 의 타겟 사망 분기에서 `Cancel(...)` + `cancelWarp?.Invoke()` 중복 호출. Cancel 한쪽으로 단일화.
- `MotionEvent_MotionWarp.Execute` 의 `"primary"` 문자열을 `MotionWarpController.DefaultTargetKey` 상수로 단일화.
- `ActorRuntimeMonitorWindow.DrawWarpCell` 의 `IsNullOrEmpty` 분기 제거 (warpInfo 가 항상 non-null/non-empty).
- `MotionWarpController` 의 enum/struct 다수를 별도 파일(`MotionWarpEnums.cs` 등) 로 분리해 navigation 향상.

### 우선순위 (Phase 6 내부)

```
6-1 (타이밍 정합성)        ─┐
6-3 (EMA 평활화)            ├──▶ 게임 감각에 직접 영향 → 우선
6-4 (데이터 마이그레이션)   ─┘
6-2 (EnemyCombat resolver) ──▶ 디자이너 워크플로우 확장
6-5 (멀티 키 데모 자산)
6-6 (인스펙터 검증 버튼)
6-7 (코드 품질 마이크로)    ──▶ 시간 날 때 묶음 처리
```

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

## 결정 사항

2026-05-10 확정. Phase 1 구현 시 본 결정에 맞춰 진행한다.

### 1. 락온 타겟 우선 정책 — **C. 하이브리드**

락온 타겟이 공격 콘(`hitRange`/`hitAngle`) 안에 있으면 락온 타겟을 워프 대상으로 채택하고, 콘 밖이면 `ConeNearestResolver` 결과를 사용한다.

- 채택 이유: 1인 보스전(락온)과 다대일 잡몹전(군중) 모두를 한 정책으로 커버.
- 검토했던 다른 안:
  - A. 락온 강제 — Souls 스타일이지만 군중전에서 락온 타겟이 멀리 있으면 가까운 적을 무시.
  - B. 콘 우선 — DMC 스타일이지만 락온 의도(특정 적 집중)를 깬다.
- 구현: `HybridResolver`가 기본 `IWarpTargetResolver`. `MotionEvent_MotionWarp.resolverPolicy` 기본값 `Hybrid`.

### 2. 캔슬 후속 모션 — **A. 헛스윙 마무리**

워프가 캔슬되면 잔여 루트모션을 그대로 재생하고 일반 공격 종료 흐름(콤보 윈도우, `OnExit`)을 그대로 탄다. 별도 페이드나 즉시 Idle 전이는 적용하지 않는다.

- 채택 이유: 가장 단순하고 예측 가능한 동작. 페이드/전이 로직을 추가하면 콤보 윈도우와 충돌할 위험이 있다.
- 검토했던 다른 안:
  - B. 즉시 Idle — 끊겨 보이고 콤보 시퀀스를 끊는다.
  - C. 잔여 루트모션 + 페이드 — 자연스럽지만 페이드 구간 동안 콤보 입력 처리가 모호.
- 구현: `OnWarpCancelled` 발화만 하고 `PlayerAttackState`는 별도 처리 없이 기존 `UpdateState` 흐름을 그대로 둔다. 잔여 루트모션은 `_motionWarp.IsMotionWarping = false` 상태에서 KCC가 그대로 처리.
- 자기 Hit/KnockBack 진입 시는 새 상태가 우선하므로 헛스윙도 적용되지 않는다.

### 3. 멀티 타겟 키 도입 시점 — **B. Phase 4까지 단일 키**

Phase 1~3은 단일 타겟(`"primary"` 키)만 사용해 데이터 모델을 단순하게 유지한다. 다단 모션(도약-착지, 환경 경유 공격)이 등장하는 Phase 4 시점에 `Dictionary<string, MotionWarpTarget>`으로 확장.

- 채택 이유: 현재 보스 1 vs 플레이어 1 시나리오에서 멀티 키는 사용되지 않는다. 미리 도입하면 검증 부담만 늘어남.
- 검토했던 다른 안:
  - A. Phase 1 도입 — 데이터 모델은 깨끗해지지만 미사용 코드로 남아 회귀 검증 부담 증가.
- 구현 메모: Phase 1의 `MotionWarpTarget` 구조체는 키 확장 시에도 변형 없이 그대로 사용할 수 있게 설계해 둔다 (앞으로 Dictionary value로 들어가도 무리 없게).

---

## 검증 기준

각 Phase 종료 시점에 다음 항목으로 회귀 점검.

### Phase 1 ✅

- [x] 기존 `Light/Heavy/Finish/Grab` 프리셋 코드 경로가 동일 (resolverPolicy 기본 `UseExisting` 으로 자산 회귀 없음). 실제 인게임 프레임 비교는 플레이 검증 잔존.
- [x] `Combat.IsMotionWarping` 외부 참조 0 — 타이머 진실 소스가 `MotionWarpController` 단일.
- [x] `MotionWarpEnabled = true` 활성 상태 유지.

### Phase 2 ✅

- [x] 타겟 사망 감지 (`IDamageable.IsAlive() == false`) → `Cancel(WarpCancelReason.TargetLost)` 발화.
- [x] `PlayerHitState` / `EnemyHitState` / `PlayerDeathState` / `EnemyDeathState` `OnEnter` 에서 `MotionWarp.ClearTarget()` 호출.
- [x] `Cancel` / `EndMotionWarp` / `ClearTarget` 모두 `_warpRemainingTime > 0f` 가드로 idempotent. 활성 윈도우 없이 cancel 이 두 번 발화하지 않는다.
- [ ] (잔존) OOR 누적 0.1s 임계의 실제 게임 감각 튜닝.

### Phase 3 ✅

- [x] 시간 상수 (25/8) 분기 제거. `TryEvaluateRotation` 이 정규화 t 의 곡선 알파만 사용.
- [x] `WarpYPolicy` 분기 (`IgnoreY` / `MatchTargetY` / `ProjectToTargetY`) 가 `EvaluateVelocity` 마지막 단계에서 분기.
- [ ] (잔존) `MatchTargetY` 옵션이 실제 점프 마무리 모션의 착지점을 ±0.05m 이내로 정렬하는지 인게임 검증.
- [ ] (잔존) 짧은/긴 클립 동일 정렬도 회귀 검증.

### Phase 4 ✅

- [x] `MotionWarpTargetPolicy.Predictive` 코드 경로 활성. Grab 프리셋이 자동으로 Predictive 채택.
- [x] `Dictionary<string, MotionWarpTarget>` + `_activeKey` 모델로 다중 키 데이터 모델 도입.
- [x] 기존 단일 키 호출 (`SetTarget(target)`, `BeginWarpWindow(settings)`, `ClearTarget()`) 모두 `"primary"` 키로 위임 — Phase 1~3 코드 무수정.
- [ ] (잔존) `Predictive` 도착 오차가 `Live` 대비 50% 이상 감소하는지 인게임 측정.
- [ ] (잔존) 다른 키 두 이벤트가 한 모션셋 안에서 간섭 없이 동작하는지 데모 자산 검증.

### Phase 5 ✅

- [x] `ActorRuntimeMonitorWindow` 에서 활성 워프 키/타겟/blend/OOR/실패사유를 한 행에서 볼 수 있다.
- [x] SceneView Gizmo 로 anchor / min·max / 도달 영역 / Predictive 가산점이 시각화된다.
- [x] `SettingsData.debugMotionWarpEnabled` 로 워프 전역 토글이 런타임 조정 가능 — `MotionWarpEnabled` const 제거.
- [ ] (Phase 6 이관) `MotionEvent_MotionWarp` 인스펙터 전용 시뮬 버튼 — 현재 Gizmo + ActorMonitor 로 등가 정보 확보. Phase 6-6 에 통합.

### Phase 6 ⏳

- [ ] `MotionWarpController` 타이머·속도 갱신이 FixedUpdate 와 정합한다 (KCC 와 같은 frame).
- [ ] `EnemyCombat.BuildWarpResolverContext` 가 적 공격에서도 Hybrid/ConeNearest resolver 를 사용 가능.
- [ ] Predictive 정책 사용 시 빠른 적의 정지/방향전환에서 한 프레임 튐 현상이 EMA 평활화로 해소된다.
- [ ] 기존 자산이 `Hybrid` resolverPolicy 로 일괄 마이그레이션, Grab 의 `Live → Predictive` 전환 회귀 통과.
- [ ] 점프-착지 멀티 키 데모 자산 1종 작성, 두 이벤트가 서로 간섭 없이 동작.
- [ ] `MotionSetEditorWindow` 에 워프 시뮬 버튼 — 가상 타겟 시뮬 결과 즉시 확인.
- [ ] 잔존 코드 품질 항목 (Phase 6-7) 묶음 정리.

---

## 참고 링크

- [UE5 Motion Warping 공식 문서](https://dev.epicgames.com/documentation/en-us/unreal-engine/motion-warping-in-unreal-engine)
- [UE5 SkewWarp 모디파이어](https://dev.epicgames.com/documentation/en-us/unreal-engine/BlueprintAPI/MotionWarping/AddRootMotionModifierSkewWarp)
- [Quod Soler — Motion Warping Blueprint Guide](https://www.quodsoler.com/blog/motion-warping-character-attacks-using-blueprints-no-c-required)
- [UE Forums — Motion Warping and Moving Targets](https://forums.unrealengine.com/t/motion-warping-and-moving-targets/612472)
- [Kinemation Motion Warping for Unity — How it Works](https://kinemation.gitbook.io/motion-warping-for-unity/concept/how-this-asset-works)
- [Kinemation Motion Warping for Unity — Asset Fields](https://kinemation.gitbook.io/motion-warping-for-unity/fundametals/motion-warping-asset)
- [Soulslike Framework — Using Motion Warping](https://soulslike-framework.isik.vip/extending-functionality/using-motion-warping)
