# 전투 카메라 시스템 구조 개선 계획

> 작성일: 2026-06-04  
> 분류: 구조 개선 제안 / TODO  
> 기준 레퍼런스: 명조(Wuthering Waves)식 전투 카메라 감각  
> 관련 문서: `Assets/docs/Complete/CAMERA_SYSTEM_GUIDE.md`, `Assets/docs/Complete/CAMERA_MODE_ARCHITECTURE_DESIGN.md`, `Assets/docs/Complete/CAMERA_ENHANCEMENT_ROADMAP_DESIGN.md`, `Assets/docs/Complete/COMBAT_SYSTEM_NEXT_IMPROVEMENT_PROPOSAL.md`

---

## 1. 목적

현재 카메라 시스템은 `CameraManager`, `CameraModeController`, `InGameCameraMode`, `CameraLockOn`, `CameraDistanceController`, `CameraEffectManager`, `CameraSnapshotSequenceMode` 등으로 기본 구조가 잘 분리되어 있다.

다만 전투 카메라 관점에서는 아직 다음 문제가 남아 있다.

- 전투 피드백 코드가 `CameraManager.Instance.StartShake`, `Punch`, `TryKillCam`을 직접 호출한다.
- 공격 종류, 피격 결과, 전투 상황을 카메라가 이해할 수 있는 "의도"로 변환하는 계층이 없다.
- 라이트 히트, 강공격, 스킬, 패링, 회피 반격, 보스 페이즈 전환 같은 상황별 카메라 연출이 데이터로 통합 관리되지 않는다.
- `KillCamController`는 별도 코루틴에서 일부 FOV/거리 보간을 직접 만지며, 현재 모드/이펙트/스냅샷 구조와 완전히 통합되어 있지 않다.
- 명조식 전투 카메라의 핵심인 "자동 보정은 강하지만 입력을 침범하지 않는 구조"를 표현하는 정책 계층이 부족하다.

이 문서는 카메라 알고리즘 자체보다, 전투 이벤트를 카메라 연출로 연결하는 시스템 구조 개선안을 정리한다.

---

## 2. 현재 구조 요약

```
CombatResolution / MotionEvent / Actor State
        │
        ├── CombatFeedbackDispatcher
        │       ├── CameraManager.StartShake(...)
        │       ├── CameraManager.Punch(...)
        │       └── CameraManager.TryKillCam(...)
        │
        ├── MotionEvent_CameraEffect
        │       └── CameraManager.PlayEffect(...)
        │
        ├── MotionEvent_CameraLookAtSocket
        │       └── CameraManager.SetLookAtOverride(...)
        │
        └── MotionEvent_CameraSnapshotSequence
                └── CameraManager.PushCameraSnapshotSequence(...)
```

### 장점

| 항목 | 평가 |
|------|------|
| 모드 구조 | `InGame`, `Dialogue`, `Free`, `CameraSnapshotSequence` 모드가 이미 존재한다 |
| 데이터 기반 이펙트 | `CameraEffectData`와 `ICameraEffect`로 FOV, Zoom, Rotation, Shake, TimeScale 합성이 가능하다 |
| MotionEvent 연동 | 애니메이션 타임라인에서 카메라 이펙트, 소켓 주시, 스냅샷 시퀀스를 호출할 수 있다 |
| 락온 감각 | 거리 기반 오비탈 오프셋, 타겟 우선순위, ActiveFocus, 군중 줌아웃 기반이 있다 |
| 전투 피드백 중앙화 | `CombatFeedbackDispatcher`가 HitStop, VFX, DamageFloater, Camera 피드백을 모으기 시작했다 |

### 병목

| 문제 | 영향 |
|------|------|
| 카메라 직접 호출 분산 | 전투 상황별 카메라 정책이 코드 조건문으로 늘어난다 |
| 전투 컨텍스트 부족 | 카메라가 공격 등급, 피격 반응, 락온 상태, 위협 수, 보스 상태를 한 번에 판단하기 어렵다 |
| 프로파일 부재 | 라이트/헤비/스킬/패링/처형/보스 연출을 일관된 데이터 형식으로 튜닝하기 어렵다 |
| 킬캠 별도 흐름 | 스냅샷/이펙트/모드 구조와 분리되어 유지보수 비용이 커질 수 있다 |
| 자동 보정 정책 미분리 | 플레이어 입력 존중, 멀미 방지, 강제 재줌 금지 같은 정책을 코드가 암묵적으로 처리한다 |

---

## 3. 명조식 전투 카메라 목표

명조의 내부 구현은 공개되어 있지 않으므로, 본 프로젝트에서는 관찰 가능한 전투 감각을 다음 설계 목표로 환산한다.

2026-06-04 추가 조사 기준:

- 명조 추천 세팅 글들은 전투 카메라 거리를 높게 잡는 쪽을 권장한다. 넓은 전투 시야가 다수 적과 투사체 대응에 유리하기 때문이다.
- 일부 추천 세팅은 카메라 보정을 켜지만, 플레이어 피드백에서는 자동 보정과 강제 줌/리셋이 전투 중 불쾌한 요소로 반복 언급된다.
- 락온 우선순위는 화면 중앙에 가까운 visible target을 중시하는 사례가 관찰된다. 본 프로젝트도 `CameraDirection` 우선순위를 기본으로 유지하되, 자동 회전 강도는 낮춘다.
- 따라서 기본값은 "넓은 거리/FOV + 약한 자동 보정 + 짧은 FOV 펄스 + 작은 줌 델타"로 잡고, 강한 연출은 프로필 에셋에서 명시적으로 올리는 방식이 안전하다.

| 목표 | UPlayground 적용 방향 |
|------|-----------------------|
| 전투 가독성 | 락온/비락온 모두 적과 플레이어, 위험 방향이 화면에서 읽히도록 FOV·거리·오프셋 조정 |
| 입력 존중 | 자동 정렬과 카메라 보정은 최근 수동 입력이 있으면 강도를 낮춘다 |
| 상황 반응 | 라이트 히트, 헤비 히트, 스킬, 패링, 회피 반격, 킬, 보스 페이즈를 다른 카메라 의도로 처리 |
| 연출 데이터화 | MotionSet, AttackKind, CombatResult에서 발생한 이벤트를 `CombatCameraProfileSO`로 튜닝 |
| 대형 몬스터 대응 | 전투 범위 내 최대 몬스터 Bounds 크기에 따라 FOV와 카메라 거리를 확장 |
| 과보정 방지 | 스킬 종료 후 사용자가 맞춘 줌 거리와 방향을 불필요하게 덮어쓰지 않는다 |
| 멀미 대응 | Shake, 자동 회전, 전투 카메라 보정 강도는 옵션 또는 프로파일 강도로 조절 가능해야 한다 |

---

## 4. 목표 아키텍처

```
CombatResolutionPipeline / MotionEvent / Actor State
        │
        ▼
CombatCameraIntent
        │
        ▼
CombatCameraDirector
        ├── CombatCameraProfileSO
        ├── CombatCameraContext
        ├── CombatCameraPolicy
        └── CombatCameraPriority
                │
                ▼
CameraManager
        ├── PlayEffect(CameraEffectData)
        ├── StartShake / Punch
        ├── PushCameraSnapshotSequence
        ├── SetRotationSmooth
        ├── SetLookAtOverride
        └── InGameCameraMode / CameraSnapshotSequenceMode
```

핵심은 전투 시스템이 `CameraManager`의 세부 API를 직접 고르지 않고, 먼저 "이 전투 상황에서 카메라가 어떤 반응을 해야 하는가"를 `CombatCameraIntent`로 표현하는 것이다.

---

## 5. 신규 타입 제안

### CombatCameraIntentType

```csharp
namespace UPlayGround.CameraSystem
{
    public enum CombatCameraIntentType
    {
        None,
        LightHit,
        HeavyHit,
        SkillHit,
        ChargeHit,
        DashHit,
        PlayerDamaged,
        PlayerHeavyDamaged,
        PerfectGuard,
        PerfectDodge,
        DodgeCounter,
        BreakSpecial,
        Finisher,
        Kill,
        BossPhaseChange,
        CrowdCombat,
        TargetLost
    }
}
```

### CombatCameraIntent

```csharp
namespace UPlayGround.CameraSystem
{
    public readonly struct CombatCameraIntent
    {
        public readonly CombatCameraIntentType Type;
        public readonly Transform Attacker;
        public readonly Transform Victim;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly AttackKind AttackKind;
        public readonly AttackReactionType ReactionType;
        public readonly int Priority;
        public readonly bool IsKill;
        public readonly bool IsBossContext;
    }
}
```

### CombatCameraContext

`CombatCameraDirector`가 판단에 사용할 런타임 정보 묶음이다.

```csharp
namespace UPlayGround.CameraSystem
{
    public readonly struct CombatCameraContext
    {
        public readonly bool IsLockOn;
        public readonly Transform LockOnTarget;
        public readonly int NearbyEnemyCount;
        public readonly float PlayerSpeed;
        public readonly float TimeSinceLastManualCameraInput;
        public readonly CameraModeType CurrentMode;
        public readonly bool HasActiveCameraSequence;
    }
}
```

### CombatCameraDirector

```csharp
namespace UPlayGround.CameraSystem
{
    public sealed class CombatCameraDirector
    {
        public void Play(in CombatCameraIntent intent);
        public bool CanPlay(in CombatCameraIntent intent);
        public void StopCurrentSequence(bool immediate = false);
    }
}
```

권장 소유자는 `CameraManager`다. 외부에서는 `CameraManager.Instance.CombatCamera.Play(intent)` 또는 `CameraManager.Instance.PlayCombatCamera(intent)` 형태로 접근한다.

---

## 6. 데이터 설계

### CombatCameraProfileSO

```csharp
namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CombatCameraProfile", menuName = "UPlayGround/Camera/Combat Camera Profile")]
    public class CombatCameraProfileSO : ScriptableObject
    {
        public CombatCameraIntentType intentType;
        public int priority;
        public CameraSequenceInterruptPolicy interruptPolicy;

        public List<CameraEffectData> effects;
        public CameraShakeIdType shakeKey;
        public bool usePunch;
        public float punchStrength;
        public float punchDuration;

        public bool useSnapshotSequence;
        public CameraSnapshotProfile snapshotProfile;

        public bool lockInput;
        public bool respectManualCameraInput;
        public float manualInputSuppressDuration;
    }
}
```

### CombatCameraProfileDatabaseSO

```text
Assets/10.Datas/Camera/CombatCamera/CombatCameraProfileDatabase.asset
Addressables Key: CombatCameraProfileDatabase
```

역할:

- `CombatCameraIntentType`별 기본 프로파일 조회
- 보스/Elite/Normal, 플레이어 공격/적 공격 등 조건부 프로파일 오버라이드
- 난이도 또는 접근성 옵션에 따른 Shake/AutoCorrection 강도 보정

### 에디터 데이터 생성/검증

전투 카메라 프로파일은 다음 메뉴에서 생성한다.

```text
UPlayGround/World/Camera/Create Combat Camera Profile Database
UPlayGround/World/Camera/Validate Combat Camera Profile Database
```

생성 메뉴는 다음 작업을 수행한다.

- `Assets/10.Datas/Camera/CombatCamera/CombatCameraProfileDatabase.asset` 생성 또는 갱신
- `LightHit`, `HeavyHit`, `SkillHit`, `ChargeHit`, `DashHit`, `PlayerDamaged`, `PlayerHeavyDamaged`, `PlayerDeath`, `Kill` 기본 프로파일 생성
- 기본 FOV/Zoom `CameraEffectData` 에셋 생성
- `CombatCameraProfileDatabase` Addressables 키 등록
- DB 인스펙터에서 Addressables 키 재등록과 프로파일 검증 버튼 제공

`Kill` 프로파일에 `CameraSnapshotProfile`이 없으면 기존 `KillCamController` fallback을 계속 사용한다. 따라서 기본 데이터만 생성해도 기존 킬캠 회귀를 막고, 스냅샷 데이터가 준비된 뒤 점진적으로 교체할 수 있다.

---

## 7. 기존 코드 연결 지점

### CombatFeedbackDispatcher 변경 방향

현재:

```text
ApplyPlayerAttackHitFeedback
    ├── CameraManager.Instance.Punch(...)
    ├── CameraManager.Instance.StartShake(...)
    ├── CameraManager.Instance.TryKillCam(...)
    └── GameHitStop.Execute(...)
```

목표:

```text
ApplyPlayerAttackHitFeedback
    ├── GameHitStop.Execute(...)
    ├── VitalOrb.TrySpawn(...)
    └── CombatCameraDirector.Play(intent)
```

카메라 연출 선택은 `CombatFeedbackDispatcher`가 아니라 `CombatCameraDirector`와 `CombatCameraProfileSO`가 담당한다.

### MotionEvent_CameraEffect

기존 `MotionEvent_CameraEffect`는 유지한다. 단, 액션별 고정 연출은 그대로 사용하고, 전투 결과 기반 피드백은 `CombatCameraDirector`로 보낸다.

권장 구분:

| 경로 | 용도 |
|------|------|
| `MotionEvent_CameraEffect` | 특정 모션 타이밍에 반드시 발생해야 하는 연출 |
| `CombatCameraDirector` | 실제 히트/방어/킬/패링 결과에 따라 달라지는 전투 반응 |
| `CameraSnapshotSequenceEvent` | 궁극기, 처형, 보스 페이즈 전환 같은 샷 시퀀스 |

### KillCamController

단기적으로는 유지하되, 후속 단계에서 `CombatCameraIntentType.Kill` + `CombatCameraProfileSO` + `CameraSnapshotSequenceMode` 조합으로 이전한다.

목표:

```text
Kill hit
    └── CombatCameraIntentType.Kill
            └── CombatCameraProfileSO
                    ├── TimeScaleCameraEffect
                    ├── FOVCameraEffect
                    ├── ShakeCameraEffect
                    └── CameraSnapshotProfile
```

---

## 8. 전투 상황별 권장 프로파일

| Intent | 카메라 반응 | 주의 |
|--------|-------------|------|
| `LightHit` | 약한 punch + 짧은 light shake | 연타 시 과한 흔들림 방지. 쿨다운 또는 누적 감쇠 필요 |
| `HeavyHit` | 강한 punch + heavy shake + 짧은 FOV 펄스 | 플레이어 입력 중이면 회전 보정은 하지 않음 |
| `SkillHit` | FOV/Zoom/Shake 조합 + 필요 시 입력 잠금 | MotionEvent 연출과 중복되지 않게 priority 관리 |
| `PerfectGuard` | 짧은 FOV 압축 + TimeScale + 약한 카메라 정렬 | 기존 `PerfectGuardFOV` 데이터 활용 |
| `PerfectDodge` | 짧은 bullet time + 시야 유지 | 명조식 숙련 보상. 자동 회전은 약하게 |
| `DodgeCounter` | 반격 대상 방향으로 약한 정렬 + punch | 락온이 없을 때만 soft target 사용 |
| `BreakSpecial` | snapshot sequence 또는 소켓 look-at | UI/QTE 개선과 연계 |
| `Kill` | kill profile. 일반 적은 확률, Elite/Boss는 높은 우선순위 | 기존 `KillCamController`를 점진 대체 |
| `BossPhaseChange` | CameraSnapshotSequence + TimeScale + 입력 잠금 | `InGame` 복귀 상태 보존 필수 |
| `CrowdCombat` | 거리/FOV 확대, 회전 보정 없음 | 이미 `CameraDistanceController` 군중 줌아웃과 연계 |
| `LargeMonsterCombat` | 범위 내 최대 몬스터 크기에 따른 FOV/거리 확장 | 단일 보스가 있어도 시야가 좁아지지 않게 처리 |
| `TargetLost` | 강제 회전 대신 짧은 align 후보 | 멀미와 시점 급전환 방지 |

---

## 8.1 대형 몬스터 기반 시야 확장

기존 `CameraDistanceController`는 전투 중 주변 적 수가 `crowdEnemyThreshold` 이상일 때 `crowdZoomOutDistance`로 줌아웃한다. 이 방식은 다수의 일반 몬스터에는 효과적이지만, 단일 보스나 대형 몬스터가 화면을 크게 차지하는 상황에는 충분하지 않다.

추가 정책:

```text
전투 중 매 프레임 1회:
    OverlapSphere(crowdDetectRadius, lockOnLayer)
        ├── 살아있는 IDamageable만 수집
        ├── MonsterActor root 기준 중복 제거
        └── root 하위 Collider bounds의 최대 축 크기 계산

sizeFactor = InverseLerp(monsterSizeReference, monsterSizeForMaxFOV, nearbyMaxMonsterSize)
additionalFOV = sizeFactor * monsterSizeFOVMax
additionalDistance = sizeFactor * monsterSizeDistanceMax
```

적용 원칙:

- 다수 적 줌아웃과 대형 몬스터 줌아웃은 같은 물리 쿼리 결과를 공유한다.
- `monsterSizeReference` 이하의 일반 몬스터는 추가 시야 확장 대상이 아니다.
- `monsterSizeForMaxFOV` 이상이면 최대 FOV/거리 확장을 적용한다.
- 락온 중에도 대형 몬스터가 있으면 `lockOnDistance`보다 더 먼 거리 후보를 허용한다.
- 비락온 전투에서는 유저 줌을 기본 존중하되, 대형 몬스터가 기준 이상이면 시야 확보를 위해 거리 확장을 허용한다.

관련 `CameraSettings` 필드:

| 필드 | 설명 |
|------|------|
| `enableMonsterSizeFOV` | 대형 몬스터 기반 FOV/거리 확장 활성화 |
| `monsterSizeReference` | 이 크기 이하에서는 추가 확장 없음 |
| `monsterSizeForMaxFOV` | 이 크기 이상에서 최대 확장 적용 |
| `monsterSizeFOVMax` | 최대 추가 FOV |
| `monsterSizeDistanceMax` | 최대 추가 카메라 거리 |
| `monsterSizeDistanceSmoothTime` | 거리 확장 보간 시간 |

이 기능은 `CombatCameraDirector` 이전에도 `CameraDistanceController`에서 즉시 적용할 수 있다. 이후 Director 구조가 들어오면 `LargeMonsterCombat` intent/profile로 승격할 수 있다.

## 9. 자동 보정 정책

명조식 카메라 감각에서 중요한 점은 자동 보정 자체보다 "플레이어 입력과 싸우지 않는 것"이다.

### 최근 수동 입력 감쇠

```text
if (TimeSinceLastManualCameraInput < manualInputSuppressDuration)
{
    autoRotationWeight *= 0.2f;
    autoZoomWeight *= 0.5f;
}
```

### 강제 재줌 금지

스킬/연출 종료 시 다음을 피한다.

- 사용자가 조절한 `TargetDistance`를 무조건 `defaultDistance`로 되돌림
- 락온 해제 직후 카메라를 강제로 정면 정렬
- 전투 진입만으로 매 프레임 combat distance를 덮어씀

권장:

- 연출용 거리/FOV는 `CameraEffectData` 델타로 적용한다.
- 종료 시 기본 상태를 직접 쓰지 않고 blend-out으로 자연 복귀한다.
- 사용자가 조절한 줌 거리는 `CameraDistanceController.EvaluateDistance()`의 `-1` 반환 원칙처럼 존중한다.

### Shake 접근성

`SettingsManager` 또는 카메라 옵션에 다음 값을 추가할 수 있다.

| 옵션 | 설명 |
|------|------|
| `CameraShakeScale` | 모든 전투 shake 강도 배율 |
| `CombatCameraAutoCorrection` | 전투 자동 보정 on/off 또는 강도 |
| `CombatCameraSequenceIntensity` | 스킬/킬/보스 연출 카메라 강도 |

---

## 10. 단계별 구현 계획

## P0 — 문서/데이터 정리

- [ ] `CombatCameraIntentType` 목록 확정
- [ ] 현재 `CombatFeedbackDispatcher`의 카메라 직접 호출 지점 목록화
- [ ] 기존 `CameraEffectData`, `CameraSnapshotProfile`, `KillCamData` 중 재사용 가능한 에셋 분류

## P1 — CombatCameraDirector 최소 구현

- [x] `CombatCameraIntent` 구조체 추가
- [x] `CombatCameraDirector` 추가
- [x] `CameraManager`가 director를 소유하도록 연결
- [x] `LightHit`, `HeavyHit`, `SkillHit`, `ChargeHit`, `DashHit`, `Kill` 우선 처리
- [x] `PlayerDamaged`, `PlayerHeavyDamaged`, `PlayerDeath` 카메라 피드백 경로 director로 수렴

완료 기준:

- [x] `CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback`에서 카메라 직접 호출이 줄고, director 호출로 대체된다.
- [x] 기존 히트스톱, 데미지 플로터, VFX 동작은 변하지 않는다.

구현 파일:

```text
Assets/02.Scripts/Camera/Combat/CombatCameraIntentType.cs
Assets/02.Scripts/Camera/Combat/CombatCameraIntent.cs
Assets/02.Scripts/Camera/Combat/CombatCameraDirector.cs
Assets/02.Scripts/Manager/CameraManager.cs
Assets/02.Scripts/GameActor/Combat/Feedback/CombatFeedbackDispatcher.cs
```

주의:

- P1은 기존 `PlayerAttackHitFeedbackProfile`을 재사용한다. `CombatCameraProfileSO` 기반 데이터화는 P2에서 진행한다.

## P2 — CombatCameraProfileSO 데이터화

- [x] `CombatCameraProfileSO` 추가
- [x] `CombatCameraProfileDatabaseSO` 추가
- [x] Intent별 profile 조회 구현
- [x] Shake/Punch/FOV/Zoom effect를 profile에서 재생
- [x] `CombatCameraProfileDatabase` Addressables 키 로드 경로 추가
- [x] Unity Editor에서 `CombatCameraProfileDatabase` 에셋 생성 및 Addressables 등록 가능한 메뉴 추가
- [x] `CombatCameraProfileDatabase` 인스펙터 검증/Addressables 등록 버튼 추가

완료 기준:

- [x] Light/Heavy/Skill 히트 카메라 강도를 코드 수정 없이 에셋으로 조정할 수 있다.
- [ ] Unity Editor에서 생성된 실제 프로파일 에셋 기반 튜닝 검증

## P3 — 전투 결과 기반 intent 생성

- [x] `CombatResult` 또는 `HitContext`에서 intent 생성 헬퍼 추가
- [x] `AttackKind`, `AttackReactionType`, attacker/victim을 intent에 반영
- [x] 플레이어 공격과 플레이어 피격 intent를 구분
- [x] kill 여부와 attacker/victim 등급 기반 프로파일 오버라이드 고도화
- [x] `CombatCameraProfileSO`에 attacker/victim 몬스터 등급 조건과 `triggerChance` 추가

완료 기준:

- [x] 공격 종류 추가 시 `CombatFeedbackDispatcher` 조건문이 늘어나지 않고 profile 추가로 대응 가능하다.

## P4 — KillCamController 점진 대체

- [x] `CombatCameraIntentType.Kill` 프로파일 경로 추가
- [x] 일반 적/Elite/Boss/Weak 별 kill profile 분기
- [x] `CameraSnapshotSequenceMode`와 `TimeScaleCameraEffect` 기반으로 킬 연출 가능한 profile 경로 추가
- [x] 기존 `KillCamController`는 fallback으로 유지
- [x] `Kill` 프로파일에 스냅샷이 없을 때 기존 `KillCamController`를 계속 호출하도록 보완
- [x] 등급별 kill profile 발동 확률 기본값 적용: Weak 15%, Normal 25%, Elite 60%, Boss 100%

완료 기준:

- [ ] `Kill` 프로파일 에셋이 등록되면 킬캠이 별도 코루틴 대신 카메라 효과/스냅샷 시스템으로 동작한다.

## P5 — 명조식 soft target / counter camera

- [x] 락온이 없을 때 지정 target 방향으로 약한 보정 가능한 `PlaySoftTargetAssist` 추가
- [ ] 화면 중앙 위협 자동 선택
- [x] `PerfectDodge`, `DodgeCounter`, `PerfectGuard` 이벤트 배선
- [x] 최근 수동 입력이 있으면 보정 강도를 낮춤

완료 기준:

- [ ] 수동 락온 없이도 반격/패링 성공 시 적 방향이 읽히지만, 카메라가 강제로 튀지 않는다.

## P6 — 접근성 옵션 연동

- [x] 전투 카메라 shake 강도 배율
- [x] 자동 보정 강도
- [x] 스냅샷/킬캠 연출 강도
- [x] `SettingsData`와 `CameraSettings` 기본값 연동

완료 기준:

- [x] 멀미 민감 플레이어가 전투 카메라 보정을 줄일 수 있는 런타임 값이 준비된다.
- [ ] 설정 UI에서 신규 옵션 조작 지원

---

## 11. 검증 체크리스트

### 기능 검증

- [ ] LightHit 연타 시 카메라 shake가 과하게 누적되지 않는다.
- [ ] HeavyHit/SkillHit에서 hitstop, shake, punch, FOV 효과가 의도한 순서로 보인다.
- [ ] Kill intent가 일반 적/Elite/Boss 별 확률 또는 우선순위 정책을 따른다.
- [ ] MotionEvent 기반 카메라 연출과 result 기반 카메라 피드백이 중복으로 과하게 발화하지 않는다.
- [ ] 전투 중 수동 카메라 입력 직후 자동 정렬이 약해진다.
- [ ] 락온 해제/타겟 소실 시 카메라가 급격히 튀지 않는다.

### 회귀 검증

- [ ] `CameraManager.IsLockOnActive()` / `GetLockOnTarget()` 기존 호출부가 유지된다.
- [ ] `MotionEvent_CameraEffect`, `MotionEvent_CameraLookAtSocket`, `MotionEvent_CameraSnapshotSequence`가 기존처럼 동작한다.
- [ ] `CameraEffectManager.StopAll(immediate: true)`가 씬 전환 시 모든 전투 카메라 효과를 정리한다.
- [ ] `SetInputLock(true)`로 잠긴 연출이 종료 후 반드시 해제된다.
- [x] `dotnet build UPlayground.sln --no-restore`가 통과한다.

---

## 12. 리스크와 대응

| 리스크 | 대응 |
|--------|------|
| Director가 또 다른 거대 매니저가 될 수 있음 | intent 해석과 profile 재생만 담당. 타겟 탐색, 카메라 포즈 계산은 기존 클래스에 맡긴다 |
| MotionEvent 연출과 중복 | `CombatCameraProfileSO.priority`, `interruptPolicy`, effect id로 중복 재생을 제어한다 |
| 카메라 자동 보정이 입력을 방해 | 최근 수동 입력 감쇠와 옵션화를 P5/P6에서 필수 처리한다 |
| 킬캠 이전 중 회귀 | `KillCamController`를 바로 삭제하지 않고 `Kill` intent fallback으로 유지한다 |
| 에셋 수 증가 | `CombatCameraProfileDatabaseSO`에서 기본 프로파일을 모아 관리하고, 의도별 오버라이드만 추가한다 |

---

## 13. 판단

현재 카메라 시스템은 알고리즘보다 연결 구조를 개선할 타이밍이다. `CameraModeController`, `CameraEffectManager`, `CameraSnapshotSequenceMode`가 이미 있으므로 새 전투 카메라 시스템을 크게 새로 만들 필요는 없다.

우선순위는 다음과 같다.

1. `CombatFeedbackDispatcher`의 카메라 직접 호출을 `CombatCameraIntent`로 감싼다.
2. `CombatCameraDirector`가 intent와 profile을 기준으로 효과를 재생한다.
3. Light/Heavy/Skill/Kill부터 데이터화한다.
4. 이후 PerfectGuard, PerfectDodge, DodgeCounter, BossPhaseChange로 확장한다.
5. 마지막으로 soft target, 입력 존중, 접근성 옵션을 붙여 명조식 전투 카메라 감각에 접근한다.

이 방식은 기존 카메라 모드 구조를 유지하면서, 전투 상황별 카메라 반응을 코드 조건문이 아니라 데이터와 정책으로 관리할 수 있게 한다.
