# 액터 업데이트 구조 가이드

## 개요

현재 프로젝트는 **화면 가시성 또는 카메라 거리 기반의 액터 업데이트 컬링이 존재하지 않습니다**. 등록된 모든 `GameActor`는 카메라 위치/뷰 프러스텀과 무관하게 매 프레임 자체 `Update`를 실행합니다.

이 문서는 액터의 업데이트 호출 경로, 부분적으로 작동하는 비용 절감 장치, 그리고 LOD/컬링 도입 시 유의할 지점을 정리합니다.

---

## 업데이트 호출 경로

### 1. 매니저 → 매니저 일괄 tick

`GameManager`가 등록된 모든 `IManager`의 라이프사이클 콜백을 매 프레임 호출합니다.

```
GameManager.Update()       → IManager.OnUpdate()
GameManager.FixedUpdate()  → IManager.OnFixedUpdate()
GameManager.LateUpdate()   → IManager.OnLateUpdate()
```

위치: `Assets/02.Scripts/Manager/GameManager.cs:157`

### 2. GameObjectManager는 액터를 tick하지 않음

`GameObjectManager`는 `_allActors` 리스트로 액터를 보유하지만, `OnUpdate`에서 이 리스트를 순회하지 않습니다. 핸들러(`GameInteractionHandler` 등)와 FX 정리만 처리합니다.

위치: `Assets/02.Scripts/Manager/Object/GameObjectManager.cs:116`

`_allActors`의 용도는 다음으로 한정됩니다.

| 용도 | 메서드 |
|------|--------|
| 등록/해제 알림 이벤트 | `OnActorRegistered`, `OnActorUnregistered` |
| 전역 슬로우 모션 | `SetGlobalTimeScaleExceptPlayer(float, float)` |
| 외부 조회 | `AllActors` (IReadOnlyList) |

### 3. 각 액터/컴포넌트는 자체 MonoBehaviour Update를 가짐

매니저가 액터를 tick하지 않으므로, 모든 갱신은 Unity의 MonoBehaviour 메시지 시스템에 의존합니다.

| 컴포넌트 | 위치 | 매 프레임 수행 작업 |
|----------|------|---------------------|
| `ActorMovementController` | `MovementController/ActorMovementController.cs:94` | 현재 상태머신 `UpdateState(deltaTime)` 호출 |
| `EnemyBrain` | `Component/Enemy/EnemyBrain.cs:184` | `_decisionTimer` 누적, 인터벌 도달 시 `MakeDecision()` |
| `EnemyFlyingBrain` | `Component/Enemy/EnemyFlyingBrain.cs` | 위와 동일 (비행 적) |
| `EnemyDetection` | `Component/Enemy/EnemyDetection.cs` | 시야/거리 감지 갱신 |
| 그 외 컴포넌트 | — | 각자 자체 Update |

→ 컴포넌트가 `enabled`이고 GameObject가 활성인 한, 카메라 밖이어도 모두 호출됩니다.

---

## 가시성/거리 기반 컬링 부재

`Assets/02.Scripts/GameActor` 전체에서 다음 항목 모두 **사용처 없음**:

- `OnBecameVisible` / `OnBecameInvisible`
- `Renderer.isVisible`
- `CullingGroup` (Unity API)
- `GeometryUtility.CalculateFrustumPlanes`
- 거리 기반 `gameObject.SetActive(false)` 또는 `enabled = false`

가시성 관련 코드는 다음 위치에만 존재하며, 이들은 액터 시뮬레이션이 아닌 UI/렌더 처리용입니다.

- `Assets/02.Scripts/UI/UICharacterPreviewRenderer.cs`
- `Assets/02.Scripts/UI/HUD/MinimapEntityIcon.cs`
- `Assets/02.Scripts/Tool/Editor/Minimap/MinimapCaptureEditorWindow.cs`
- 외부 에셋: `MagicaCloth2`(자체 컬링 보유)

---

## 부분적인 비용 절감 장치

완전한 컬링은 아니지만, 다음 메커니즘이 부하를 일부 완화합니다.

### Motor 비활성 시 상태머신 스킵

```csharp
// ActorMovementController.cs:94
protected virtual void Update()
{
    if (_currentState != null && (Motor == null || Motor.enabled))
    {
        float deltaTime = Actor.DeltaTime;
        _currentState.UpdateState(deltaTime);
    }
}
```

- `KinematicCharacterMotor.enabled == false`이면 상태머신 갱신을 건너뜁니다.
- 실사용처: **파티 대기 중인 비활성 PlayerActor**. 적/NPC에는 적용되지 않습니다.

### AI 의사결정 인터벌

```csharp
// EnemyBrain.cs:184
protected virtual void Update()
{
    _decisionTimer += Time.deltaTime;
    _actionCooldownTimer += Time.deltaTime;

    if (_decisionTimer >= _decisionInterval)
    {
        _decisionTimer = 0f;
        MakeDecision();
    }
    ...
}
```

- 분기 평가가 매 프레임 실행되지는 않지만, **`Update` 진입 자체는 매 프레임** 발생.
- `SKILL_CHECK_INTERVAL` 기반 스킬 체크도 동일한 시간 분산 패턴.

### LocalTimeScale

```csharp
// GameObjectManager.cs:70
public void SetGlobalTimeScaleExceptPlayer(float timeScale, float duration = 0f)
```

- `GameActor.LocalTimeScale`을 일괄 조정해 애니메이터 속도와 `DeltaTime`을 줄임.
- 슬로우 모션 연출용이며, **`Update` 호출 자체는 그대로** 발생.

---

## 함의

| 상황 | 결과 |
|------|------|
| 대형 필드/던전에 적 다수 상주 | 화면 밖 적도 KCC Motor 시뮬, AI 의사결정, 감지 모두 동작 → 비용 누적 |
| 보스 룸 입장 후 멀리 떨어진 잡몹 | 컬링되지 않음 |
| 파티 대기 캐릭터 | 상태머신은 멈추지만 컴포넌트 Update 자체는 호출됨 |

---

## LOD 도입 시 고려 사항

향후 비용 누적이 문제가 된다면 다음 계층을 검토합니다.

### 권장 게이팅 위치

1. **거리 1차 게이팅** — `EnemyDetection`이 플레이어와의 거리로 빠르게 판단.
2. **컴포넌트 단위 비활성화** — 일정 거리 밖에서 `EnemyBrain.enabled = false` + 감지 주기 연장.
3. **Motor 슬립** — `Motor.enabled = false`로 KCC 비용 절감.

`GameObjectManager._allActors`가 이미 등록 컨테이너로 존재하므로, 매니저에서 후보를 순회해 일괄 토글하는 구조가 자연스럽습니다.

### 주의: KCC Motor 슬립의 부작용

KCC는 매 프레임 중력/지면 추적을 굴려 위치를 유지합니다. 단순 `Motor.enabled = false`만 하면:

- 공중에 떠 있는 적이 그대로 멈춤
- 슬로프 위 적이 미끄러지지 않고 정지
- 재활성 시 위치 보정 필요

→ 슬립 진입 시 지면 스냅 위치 저장, 재활성 시 복원 정책을 함께 설계해야 합니다.

### 주의: 가시성 기반 컬링의 한계

`OnBecameVisible/Invisible`은 카메라 컬링과 강결합되어 있어 게임플레이 컬링 기준으로 부적합합니다(예: 카메라가 잠시 다른 방향을 봐도 적은 살아 있어야 함). 거리/존(Zone) 기반 게이팅이 더 안전합니다.

---

## 관련 파일

| 역할 | 경로 |
|------|------|
| 매니저 일괄 tick | `Assets/02.Scripts/Manager/GameManager.cs` |
| 액터 등록/해제 | `Assets/02.Scripts/Manager/Object/GameObjectManager.cs` |
| 액터 베이스 | `Assets/02.Scripts/GameActor/Base/GameActor.cs` |
| 상태머신 호스트 | `Assets/02.Scripts/GameActor/MovementController/ActorMovementController.cs` |
| 적 의사결정 | `Assets/02.Scripts/GameActor/Component/Enemy/EnemyBrain.cs` |
| 적 감지 | `Assets/02.Scripts/GameActor/Component/Enemy/EnemyDetection.cs` |
