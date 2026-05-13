# 몬스터 공격 범위 텔레그래프 가이드

> 작성일: 2026-05-13  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

몬스터가 공격 또는 스킬을 사용하기 전에 바닥에 공격 범위를 표시해 플레이어가 회피/가드/거리 조절을 판단할 수 있게 하는 텔레그래프 시스템 가이드.

현재 프로젝트에는 원형 텔레그래프의 런타임 기반이 이미 들어와 있다. `EnemyAttackInfo`에서 공격별 사용 여부와 형태를 설정하고, `EnemyAttackState`가 공격 모션 재생과 함께 `EnemyCombat.BeginCurrentSkillTelegraph()`를 호출한다. 실제 표시 위치와 크기는 `HitPhaseData.attackOffset` / `attackRadius` 기준으로 계산된다.

핵심 방향은 다음과 같다.

- 공격 데이터(`EnemyAttackInfo`)가 텔레그래프 사용 여부와 형태를 결정한다.
- 실제 판정 데이터(`HitPhaseData`)를 시각 범위의 기준으로 사용한다.
- 공격 상태 진입/업데이트/종료 지점에서 생성, 추적, 정리를 보장한다.
- 1차 구현은 현재 코드와 맞는 `Circle` 중심으로 유지한다.
- 이후 `Cone`, `Line`, 착탄형 장판, URP Decal Projector로 확장한다.

---

## 현재 구조

```
BehaviorTree
└── ExecuteEnemyAttackNode
        └── EnemyAttackState 진입
                ├── EnemyCombat.SelectAndExecuteSkill(distance)
                │       └── EnemyAttackDataSO.skills 중 EnemyAttackInfo 선택
                ├── ActorAnimator.PlayMotion(currentSkill.baseInfo.animKey)
                ├── EnemyCombat.BeginCurrentSkillTelegraph()
                │       ├── EnemyAttackInfo.useTelegraph 확인
                │       ├── TelegraphShape.Circle 확인
                │       ├── HitPhaseData.attackOffset 기준 위치 계산
                │       └── GameObjectManager.ShowFX("EnemyHeavyAttackTelegraph_Circle", ...)
                └── UpdateState
                        ├── EnemyCombat.UpdateTelegraphs()
                        └── IsPossibleCollide == true일 때 CheckMeleeAttackHit()

EnemyAttackState.OnExit
└── EnemyCombat.ClearTelegraphs()
```

### 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `TelegraphShape`, `HitPhaseData`, `EnemyAttackInfo` 정의 |
| `Assets/02.Scripts/Data/Combat/EnemyAttackDataSO.cs` | 몬스터 스킬 목록, 거리 조건, 가중치 선택 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 스킬 선택, 히트 판정, 텔레그래프 생성/갱신/정리 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyAttackState.cs` | 공격 상태 진입, 모션 재생, 텔레그래프 생명주기 호출 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Telegraph.cs` | 현재는 주석만 있는 자리. 향후 MotionSet 타임라인 제어용으로 구현 후보 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Collision.cs` | MotionSet 타임라인에서 실제 히트 판정 ON/OFF |
| `Assets/02.Scripts/Manager/Object/GameObjectManager.FX.cs` | FX 키 기반 생성 경로 |

---

## 핵심 데이터

### TelegraphShape

`CombatData.cs`에 정의된 텔레그래프 형태 enum.

```csharp
public enum TelegraphShape
{
    Circle,
    Cone,
    Line
}
```

현재 런타임 구현은 `Circle`만 지원한다. `Cone`, `Line`은 데이터 enum에는 있지만 `EnemyCombat.BeginCurrentSkillTelegraph()`에서 경고 후 반환된다.

### EnemyAttackInfo 텔레그래프 필드

| 필드 | 설명 |
|------|------|
| `useTelegraph` | 해당 공격이 범위 예고 표시를 사용할지 여부 |
| `telegraphShape` | 표시 형태. 현재 런타임은 `Circle`만 지원 |
| `telegraphRadiusScale` | `HitPhaseData.attackRadius`에 곱할 표시 배율 |

현재 필드:

```csharp
[Header("Telegraph")]
[Tooltip("강공격 판정 전에 텔레그래프 경고 연출을 사용할지 여부")]
public bool useTelegraph = false;

[Tooltip("텔레그래프 형태. 현재 런타임 구현은 Circle만 지원한다.")]
public TelegraphShape telegraphShape = TelegraphShape.Circle;

[Tooltip("현재 히트 반경에 곱할 텔레그래프 표시 배율")]
public float telegraphRadiusScale = 1f;
```

### HitPhaseData 연동

텔레그래프는 별도 범위 데이터를 만들지 않고 실제 히트 판정 데이터에서 위치와 반경을 읽는다.

| `HitPhaseData` 필드 | 텔레그래프 사용 방식 |
|------|------|
| `attackOffset` | `_attackOrigin` 기준 전방/우측/상단 오프셋으로 중심 위치 계산 |
| `attackRadius` | 원형 텔레그래프 반경 기준 |
| `hitHeightRange` | 현재 텔레그래프 표시에는 직접 반영하지 않음. 실제 판정 높이 필터에만 사용 |

이 방식은 시각 범위와 실제 판정 데이터가 분리되어 어긋나는 문제를 줄인다.

---

## 런타임 흐름

### EnemyAttackState

`EnemyAttackState.OnEnter()`에서 스킬 선택 후 모션을 재생하고 텔레그래프를 시작한다.

```csharp
_currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);

if (_currentSkill != null)
{
    var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
    _combat.BeginCurrentSkillTelegraph();
    ...
}
```

공격 중에는 위치를 계속 갱신한다.

```csharp
_combat.UpdateTelegraphs();
```

상태 종료 시 남은 인스턴스를 정리한다.

```csharp
_combat.ClearTelegraphs();
```

### EnemyCombat

`BeginCurrentSkillTelegraph()`의 현재 동작:

1. 기존 텔레그래프를 정리한다.
2. `_currentSkill == null` 또는 `useTelegraph == false`이면 종료한다.
3. `telegraphShape != Circle`이면 경고 로그 후 종료한다.
4. `GetTelegraphPosition(0)`으로 바닥 정렬 위치를 계산한다.
5. `GameObjectManager.Instance.ShowFX()`로 원형 FX를 생성한다.
6. `ApplyTelegraphScale()`로 `attackRadius * telegraphRadiusScale` 스케일을 적용한다.
7. 생성된 인스턴스를 `_telegraphInstances`에 등록한다.

현재 사용 FX 키:

```csharp
private const string HeavyAttackTelegraphFXKey = "EnemyHeavyAttackTelegraph_Circle";
```

---

## 바닥 정렬

`EnemyCombat`는 텔레그래프 위치를 바닥에 붙이기 위한 레이캐스트 옵션을 가진다.

| 필드 | 설명 |
|------|------|
| `_alignTelegraphToGround` | 바닥 정렬 사용 여부 |
| `_telegraphGroundLayers` | 바닥 탐지 레이어 |
| `_telegraphGroundProbeHeight` | 위쪽 레이 시작 높이 |
| `_telegraphGroundProbeDistance` | 아래쪽 탐지 거리 |
| `_telegraphGroundYOffset` | z-fighting 방지용 바닥 오프셋 |

위치 계산은 다음 기준이다.

```csharp
Vector3 position = _attackOrigin.position
    + _attackOrigin.forward * phase.attackOffset.z
    + _attackOrigin.right   * phase.attackOffset.x
    + _attackOrigin.up      * phase.attackOffset.y;
```

그 후 위에서 아래로 레이캐스트해 바닥 높이에 맞춘다.

---

## 셋업 방법

### 1. 공격 데이터 설정

`EnemyAttackDataSO`의 대상 `EnemyAttackInfo`에서 다음을 설정한다.

| 필드 | 권장값 |
|------|------|
| `useTelegraph` | 강공격, 장판, 넉백, 잡기 등 회피 판단이 필요한 공격에 `true` |
| `telegraphShape` | 현재는 `Circle` |
| `telegraphRadiusScale` | 실제 반경과 시각 체감이 맞도록 `1.0`부터 조정 |
| `baseInfo.hitPhases[n].attackOffset` | 실제 판정 중심 위치 |
| `baseInfo.hitPhases[n].attackRadius` | 실제 판정 반경 |

### 2. FX 프리팹 준비

현재 코드는 `"EnemyHeavyAttackTelegraph_Circle"` 키를 사용한다. 해당 키가 `FXPrefabDatabase`에 등록되어 있어야 한다.

권장 프리팹 기준:

| 항목 | 기준 |
|------|------|
| 피벗 | 바닥 중심 |
| 기본 크기 | 반지름 1 기준 또는 스케일 보정 규칙을 프로젝트에서 통일 |
| 렌더링 | 투명 머티리얼, ZWrite Off, 바닥보다 약간 위 |
| 색상 | 위험 경고용 붉은색/주황색 계열 |
| 애니메이션 | 생성 후 판정 직전까지 알파 또는 외곽선 펄스 |

### 3. 몬스터 컴포넌트 설정

`EnemyCombat` 인스펙터에서 확인할 값:

| 필드 | 확인 |
|------|------|
| `_attackData` | 텔레그래프 설정이 들어간 `EnemyAttackDataSO` |
| `_attackOrigin` | 공격 기준 Transform. 비어 있으면 `transform` 사용 |
| `_targetLayer` | 실제 히트 판정 대상 |
| `_telegraphGroundLayers` | 바닥 레이어 포함 여부 |

---

## 웹 구현 사례 조사 요약

Unity/URP에서 적 공격 범위 표시는 보통 두 방식으로 구현한다.

| 방식 | 장점 | 단점 | 프로젝트 적합도 |
|------|------|------|------|
| Plane/Mesh + Transparent Shader | 구현 단순, FX 프리팹/풀링과 잘 맞음 | 경사면/복잡한 지형 밀착 품질이 낮음 | 현재 구조에 가장 적합 |
| URP Decal Projector | 지형/메시에 투영되어 품질이 좋음 | URP Renderer Feature, Decal Shader, 성능/호환성 관리 필요 | 보스 장판/대형 스킬에 적합 |

참고 문서:

- Unity URP Decal Projector: `https://docs.unity.cn/6000.0/Documentation/Manual/urp/renderer-feature-decal-projector-reference.html`
- Unity URP Decal Shader: `https://docs.unity.cn/6000.1/Documentation/Manual/urp/decal-shader.html`

현재 프로젝트는 `GameObjectManager.ShowFX()`로 FX를 생성하는 구조가 있으므로 1차 구현은 Plane/Mesh 기반 프리팹이 가장 적합하다. 지형 밀착 품질이 중요한 보스 광역기부터 Decal Projector를 별도 프리팹으로 도입하는 방식이 안전하다.

---

## 확장 설계

### 1. FX 키 고정 제거

현재 `EnemyCombat`는 `HeavyAttackTelegraphFXKey` 상수 하나만 사용한다. 형태가 늘어나면 데이터 기반으로 바꾸는 것이 좋다.

후보 필드:

```csharp
public string telegraphFXKey;
```

또는 형태별 기본 키 매핑:

```csharp
private string GetTelegraphFXKey(TelegraphShape shape) => shape switch
{
    TelegraphShape.Circle => "EnemyTelegraph_Circle",
    TelegraphShape.Cone   => "EnemyTelegraph_Cone",
    TelegraphShape.Line   => "EnemyTelegraph_Line",
    _ => "EnemyTelegraph_Circle",
};
```

### 2. EnemyAttackTelegraphController 분리

`EnemyCombat`가 스킬 선택, 판정, 텔레그래프까지 모두 담당하고 있으므로 확장 시 클래스 분리가 필요하다.

권장 구조:

```
EnemyCombat
├── SelectAndExecuteSkill()
├── CheckMeleeAttackHit()
└── EnemyAttackTelegraphController
        ├── Show(skill, hitPhaseIndex)
        ├── UpdateTelegraph()
        └── Clear()

EnemyTelegraphView
├── Setup(skill, phase)
├── SetWorldPose(position, rotation)
├── SetProgress(normalizedTime)
└── Hide()
```

분리 기준:

- `EnemyCombat`: 전투 판정과 스킬 상태
- `EnemyAttackTelegraphController`: 인스턴스 생성/추적/정리
- `EnemyTelegraphView`: 프리팹 표시, 머티리얼, 애니메이션

### 3. Cone 지원

전방 부채꼴 공격용.

필요 작업:

- `EnemyAttackInfo`에 각도 필드 추가
- 부채꼴 Mesh 프리팹 또는 런타임 Mesh 생성
- 실제 히트 판정도 `OverlapSphere + Vector3.Angle` 필터로 맞추는 정책 검토

후보 필드:

```csharp
public float telegraphAngle = 60f;
```

### 4. Line 지원

돌진, 브레스, 레이저 공격용.

필요 작업:

- 길이와 폭 데이터 추가
- 실제 판정이 Sphere 기반이면 시각과 판정이 어긋난다. Box/Capsule 판정 확장과 함께 진행하는 것이 좋다.

후보 필드:

```csharp
public float telegraphLength = 6f;
public float telegraphWidth = 1.2f;
```

### 5. MotionEvent 기반 타이밍 제어

현재는 공격 모션 시작과 동시에 텔레그래프가 시작된다. 공격별로 예고 시작/종료 타이밍을 정밀하게 다루려면 `MotionEvent_Telegraph.cs`를 실제 구현해야 한다.

권장 이벤트:

```csharp
[Serializable]
public class TelegraphEvent : MotionEventBase
{
    public int hitPhaseIndex;
    public bool lockPositionOnStart;

    public override void Execute(GameObject target)
    {
        target.GetComponent<EnemyCombat>()?.BeginTelegraph(hitPhaseIndex, lockPositionOnStart);
    }

    public override void OnCompleteEvent(GameObject target)
    {
        target.GetComponent<EnemyCombat>()?.ClearTelegraphs();
    }
}
```

MotionSet 규칙:

```
0.00s        공격 모션 시작
0.20s        TelegraphEvent 시작
0.75s        TelegraphEvent 종료
0.78s        BeginCollisionEvent 시작
0.95s        BeginCollisionEvent 종료
```

텔레그래프 종료 시점은 실제 판정 시작과 같거나 아주 조금 앞서는 편이 좋다.

---

## 주의 사항

- 현재 런타임은 `Circle`만 지원한다. `EnemyAttackInfo.telegraphShape`를 `Cone` 또는 `Line`으로 설정하면 경고 로그만 출력되고 표시되지 않는다.
- 텔레그래프 크기는 `attackRadius * telegraphRadiusScale`이다. 프리팹 기본 크기와 이 계산식이 맞지 않으면 실제 판정보다 크게 또는 작게 보인다.
- `ClearTelegraphs()`는 현재 `Destroy()`를 사용한다. FX 풀링을 강하게 쓰는 프로젝트 정책과 맞추려면 반환 API로 바꾸는 것이 좋다.
- `Motion Warp` 공격은 공격자가 이동하면서 텔레그래프도 매 프레임 갱신된다. 플레이어 입장에서는 경고 위치가 미끄러져 보일 수 있으므로 공격별로 추적형/고정형 정책이 필요하다.
- 바닥 정렬 레이어에 몬스터/플레이어 콜라이더가 포함되면 텔레그래프가 잘못된 높이에 붙을 수 있다.
- 멀티 히트 공격은 현재 `_telegraphHitPhaseIndex = 0`만 사용한다. 히트 페이즈별 텔레그래프가 필요하면 MotionEvent 기반으로 전환해야 한다.

---

## 구현 우선순위

| 순서 | 작업 | 목적 |
|------|------|------|
| 1 | 원형 텔레그래프 프리팹 품질 정리 | 현재 구현을 바로 게임에서 쓸 수 있게 함 |
| 2 | FX 키 고정 제거 | 몬스터/공격별 다른 표시 가능 |
| 3 | `EnemyAttackTelegraphController` 분리 | `Cone`, `Line`, 착탄형 확장 준비 |
| 4 | `MotionEvent_Telegraph` 구현 | 공격별 예고 타이밍 정밀 제어 |
| 5 | `Cone` 지원 | 휘두르기/브레스 전조 |
| 6 | `Line` 지원 | 돌진/레이저 전조 |
| 7 | URP Decal Projector 프리팹 도입 | 보스 장판, 대형 광역기 품질 개선 |

---

## 테스트 체크리스트

| 항목 | 확인 내용 |
|------|-----------|
| 기본 표시 | `useTelegraph == true`인 공격에서 공격 모션 시작 후 원형 범위가 보인다 |
| 판정 일치 | `attackRadius`와 표시 반경의 체감이 맞다 |
| 위치 일치 | `attackOffset` 변경 시 표시 중심과 실제 판정 중심이 함께 이동한다 |
| 바닥 정렬 | 경사/계단/불규칙 지형에서 바닥 위에 표시된다 |
| 상태 중단 | 몬스터 피격, 사망, 상태 전환 시 텔레그래프가 남지 않는다 |
| Motion Warp | 워프 공격에서 범위가 부당하게 순간 이동하거나 늦게 따라오지 않는다 |
| 데이터 누락 | FX 키/프리팹 누락 시 에러가 명확히 드러난다 |
| 멀티 히트 | 0번 히트 페이즈만 표시되는 현재 제약을 인지하고 데이터가 구성되어 있다 |

