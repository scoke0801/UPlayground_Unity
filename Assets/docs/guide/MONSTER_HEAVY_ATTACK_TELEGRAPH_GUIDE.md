# 몬스터 공격 범위 텔레그래프 가이드

> 작성일: 2026-05-13  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

몬스터가 공격 또는 스킬을 사용하기 전에 바닥에 공격 범위를 표시해 플레이어가 회피/가드/거리 조절을 판단할 수 있게 하는 텔레그래프 시스템 가이드.

현재 프로젝트에는 원형 텔레그래프의 런타임 기반과 MotionSet 타임라인 이벤트가 들어와 있다. `EnemyAttackInfo`에서 공격별 사용 여부, 형태, FX 키, 위치 기준, 타이밍 제어 방식을 설정하고, `EnemyAttackState` 또는 `TelegraphEvent`가 `EnemyCombat.BeginTelegraph()`를 호출한다. 실제 표시 위치와 크기는 `HitPhaseData.attackOffset` / `attackRadius` 기준으로 계산되며, AOE 장판형 공격은 텔레그래프가 예약한 위치를 실제 판정 위치로 재사용할 수 있다.

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
                │       ├── useMotionEventTelegraph == false일 때 자동 시작
                │       ├── TelegraphShape.Circle 확인
                │       ├── TelegraphAnchorType 기준 위치 계산
                │       └── telegraphFXKey 또는 기본 FX 키로 GameObjectManager.ShowFX(...)
                ├── MotionSet TelegraphEvent
                │       ├── useMotionEventTelegraph == true인 공격의 타임라인 시작점
                │       └── hitPhaseIndex / lockPositionOnStart 기준으로 BeginTelegraph 호출
                └── UpdateState
                        ├── EnemyCombat.UpdateTelegraphs()
                        └── IsPossibleCollide == true일 때 CheckMeleeAttackHit()
                                └── useTelegraphPositionForHit이면 예약된 텔레그래프 위치에서 판정

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
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Telegraph.cs` | `TelegraphEvent` 정의. MotionSet에서 텔레그래프 시작/종료 타이밍 제어 |
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

### TelegraphAnchorType

텔레그래프 위치 기준 enum.

```csharp
public enum TelegraphAnchorType
{
    CasterOffset,
    TargetPosition
}
```

| 값 | 설명 |
|------|------|
| `CasterOffset` | 기존 방식. `_attackOrigin`과 `HitPhaseData.attackOffset` 기준 |
| `TargetPosition` | 실행 시점의 현재 타겟 위치 기준. 지면에 미리 찍고 N초 뒤 폭발하는 AOE에 사용 |

### EnemyAttackInfo 텔레그래프 필드

| 필드 | 설명 |
|------|------|
| `useTelegraph` | 해당 공격이 범위 예고 표시를 사용할지 여부 |
| `telegraphShape` | 표시 형태. 현재 런타임은 `Circle`만 지원 |
| `telegraphRadiusScale` | `HitPhaseData.attackRadius`에 곱할 표시 배율 |
| `telegraphFXKey` | 비어 있으면 형태별 기본 FX 키 사용 |
| `useMotionEventTelegraph` | `true`면 공격 상태 진입 자동 표시를 건너뛰고 MotionSet의 `TelegraphEvent` 타이밍을 사용 |
| `telegraphAnchorType` | 텔레그래프 위치 기준 |
| `useTelegraphPositionForHit` | `true`면 텔레그래프 예약 위치를 실제 Collision 판정 위치로 사용 |

현재 필드:

```csharp
[Header("Telegraph")]
[Tooltip("강공격 판정 전에 텔레그래프 경고 연출을 사용할지 여부")]
public bool useTelegraph = false;

[Tooltip("텔레그래프 형태. 현재 런타임 구현은 Circle만 지원한다.")]
public TelegraphShape telegraphShape = TelegraphShape.Circle;

[Tooltip("현재 히트 반경에 곱할 텔레그래프 표시 배율")]
public float telegraphRadiusScale = 1f;

[Tooltip("비워두면 기본 형태별 FX 키를 사용한다. 현재 기본값: EnemyHeavyAttackTelegraph_Circle")]
public string telegraphFXKey;

[Tooltip("true면 EnemyAttackState 진입 시 자동 표시하지 않고 MotionSet의 TelegraphEvent 타이밍을 따른다.")]
public bool useMotionEventTelegraph = false;

[Tooltip("텔레그래프 위치 기준. TargetPosition은 시전 시작 시 현재 타겟 위치에 고정하는 AOE 장판에 사용한다.")]
public TelegraphAnchorType telegraphAnchorType = TelegraphAnchorType.CasterOffset;

[Tooltip("true면 TelegraphEvent에서 예약한 위치를 실제 Collision 판정 위치로 사용한다. TargetPosition AOE에 사용한다.")]
public bool useTelegraphPositionForHit = false;
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

`EnemyAttackState.OnEnter()`에서 스킬 선택 후 모션을 재생한다. `useMotionEventTelegraph == false`인 공격은 상태 진입 시점에 자동으로 텔레그래프를 시작한다.

```csharp
_currentSkill = _combat.SelectAndExecuteSkill(distanceToTarget);

if (_currentSkill != null)
{
    var animState = gameActor.Animator.PlayMotion(_currentSkill.baseInfo.animKey, 0.1f);
    if (!_currentSkill.useMotionEventTelegraph)
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

`BeginCurrentSkillTelegraph()`는 `BeginTelegraph(0, false)`의 호환 래퍼다. 기존 데이터는 자동 시작으로 동작하고, MotionSet 타이밍을 쓰는 공격은 `TelegraphEvent`가 `BeginTelegraph(hitPhaseIndex, lockPositionOnStart)`를 직접 호출한다.

`BeginTelegraph()`의 현재 동작:

1. 기존 텔레그래프를 정리한다.
2. `_currentSkill == null` 또는 `useTelegraph == false`이면 종료한다.
3. `telegraphShape != Circle`이면 경고 로그 후 종료한다.
4. 요청한 `hitPhaseIndex`를 현재 스킬의 `hitPhases` 범위로 클램프한다.
5. `telegraphAnchorType`에 따라 바닥 정렬 위치를 계산한다.
   - `CasterOffset`: `_attackOrigin + HitPhaseData.attackOffset`
   - `TargetPosition`: 현재 타겟 위치
6. `telegraphFXKey`가 있으면 해당 키를, 비어 있으면 기본 Circle 키를 사용해 FX를 생성한다.
7. `lockPositionOnStart == true`이면 생성 시점 위치/회전을 고정하고, 아니면 공격자 이동을 따라 갱신한다.
8. 계산된 위치를 `hitPhaseIndex`별 예약 판정 위치로 저장한다.
9. `ApplyTelegraphScale()`로 `attackRadius * telegraphRadiusScale` 스케일을 적용한다.
10. 생성된 인스턴스를 `_telegraphInstances`에 등록한다.

`useTelegraphPositionForHit == true`이면 `GetAttackPosition(hitPhaseIndex)`가 `HitPhaseData.attackOffset` 대신 예약된 텔레그래프 위치를 반환한다. 따라서 `TelegraphEvent` 종료 직후 `BeginCollisionEvent`를 배치하면 같은 위치에서 실제 AOE 판정이 발생한다.

현재 사용 FX 키:

```csharp
private const string DefaultCircleTelegraphFXKey = "EnemyHeavyAttackTelegraph_Circle";
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
| `telegraphFXKey` | 공격별 다른 프리팹이 필요할 때만 입력. 비우면 기본 Circle 키 사용 |
| `useMotionEventTelegraph` | 공격 모션 시작 즉시 표시하려면 `false`, Collision 직전 등 타임라인 제어가 필요하면 `true` |
| `telegraphAnchorType` | 전방 공격이면 `CasterOffset`, 타겟 위치 장판이면 `TargetPosition` |
| `useTelegraphPositionForHit` | 타겟 위치 장판이면 `true` |
| `baseInfo.hitPhases[n].attackOffset` | 실제 판정 중심 위치 |
| `baseInfo.hitPhases[n].attackRadius` | 실제 판정 반경 |

### 2. FX 프리팹 준비

기본 Circle 텔레그래프는 `"EnemyHeavyAttackTelegraph_Circle"` 키를 사용한다. 해당 키가 `FXPrefabDatabase`에 등록되어 있어야 한다. 공격별 다른 FX가 필요하면 `EnemyAttackInfo.telegraphFXKey`에 별도 키를 입력한다.

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

### 4. TargetPosition AOE 설정 예시

타겟 위치에 장판을 미리 표시하고 2초 뒤 폭발하는 공격은 다음처럼 설정한다.

`EnemyAttackInfo`:

| 필드 | 값 |
|------|------|
| `useTelegraph` | `true` |
| `telegraphShape` | `Circle` |
| `useMotionEventTelegraph` | `true` |
| `telegraphAnchorType` | `TargetPosition` |
| `useTelegraphPositionForHit` | `true` |
| `telegraphRadiusScale` | `1.0` |

MotionSet:

```
0.10s  TelegraphEvent 시작
2.10s  TelegraphEvent 종료
2.10s  BeginCollisionEvent 시작
2.25s  BeginCollisionEvent 종료
```

`TelegraphEvent.lockPositionOnStart = true`로 두면 시전 시작 시점의 플레이어 위치에 장판이 고정된다. `false`로 두면 텔레그래프가 타겟을 따라가며, 예약 판정 위치도 매 프레임 갱신된다.

---

## 실전 제작 가이드

### 패턴 선택 기준

공격을 만들 때 먼저 텔레그래프 위치와 타이밍 정책을 정한다.

| 공격 패턴 | 권장 위치 기준 | 권장 타이밍 | 권장 설정 |
|------|------|------|------|
| 제자리 충격파 | `CasterOffset` | 상태 진입 자동 표시 | `useMotionEventTelegraph = false` |
| 전방 내려찍기 | `CasterOffset` | 상태 진입 자동 표시 또는 MotionSet | 공격 모션과 판정 타이밍이 단순하면 자동 표시 |
| 타겟 위치 폭발 장판 | `TargetPosition` | MotionSet | `useMotionEventTelegraph = true`, `lockPositionOnStart = true` |
| 추적형 경고 장판 | `TargetPosition` | MotionSet | `lockPositionOnStart = false` |
| 연속 장판 | `TargetPosition` 또는 `CasterOffset` | MotionSet | 히트 페이즈별 `TelegraphEvent` 배치 |

일반 원칙:

- 플레이어가 보고 피해야 하는 공격만 텔레그래프를 켠다.
- 텔레그래프가 보인 위치와 실제 판정 위치가 달라지면 안 된다.
- 타겟 위치 장판은 대부분 고정형이 읽기 쉽다.
- 추적형 장판은 의도적으로 압박을 주는 패턴에만 사용한다.

### 일반 근접 원형 공격 제작 절차

몬스터 주변 또는 전방에 원형 범위를 표시하고 실제 공격 위치도 몬스터 기준으로 계산하는 방식이다.

1. `EnemyAttackDataSO`에서 대상 스킬을 연다.
2. `useTelegraph = true`로 설정한다.
3. `telegraphShape = Circle`로 둔다.
4. `telegraphAnchorType = CasterOffset`으로 둔다.
5. `useTelegraphPositionForHit = false`로 둔다.
6. `useMotionEventTelegraph = false`로 둔다.
7. `baseInfo.hitPhases[0].attackOffset`으로 표시 중심을 맞춘다.
8. `baseInfo.hitPhases[0].attackRadius`로 실제 판정 반경을 맞춘다.
9. `telegraphRadiusScale`을 `1.0`부터 시작해 프리팹 크기와 체감을 조정한다.

이 방식은 `EnemyAttackState.OnEnter()`에서 자동으로 `BeginCurrentSkillTelegraph()`가 호출된다. MotionSet에 `TelegraphEvent`를 추가하지 않아도 된다.

### 타겟 위치 AOE 장판 제작 절차

플레이어 위치에 장판을 먼저 찍고 N초 뒤 같은 위치에서 폭발시키는 방식이다.

1. `EnemyAttackDataSO`에서 대상 스킬을 연다.
2. `useTelegraph = true`로 설정한다.
3. `telegraphShape = Circle`로 둔다.
4. `telegraphAnchorType = TargetPosition`으로 설정한다.
5. `useTelegraphPositionForHit = true`로 설정한다.
6. `useMotionEventTelegraph = true`로 설정한다.
7. `baseInfo.hitPhases[0].attackRadius`를 실제 폭발 반경으로 설정한다.
8. MotionSet에 `TelegraphEvent`를 추가한다.
9. `TelegraphEvent.hitPhaseIndex = 0`으로 설정한다.
10. `TelegraphEvent.lockPositionOnStart = true`로 설정한다.
11. N초 뒤 `BeginCollisionEvent`를 배치한다.

권장 타임라인:

```
0.00s  시전 모션 시작
0.10s  TelegraphEvent 시작
2.10s  TelegraphEvent 종료
2.10s  BeginCollisionEvent 시작
2.25s  BeginCollisionEvent 종료
```

이때 `TelegraphEvent`가 저장한 위치를 `GetAttackPosition(hitPhaseIndex)`가 재사용한다. 그래서 `BeginCollisionEvent`가 시작될 때 몬스터가 이동했거나 플레이어가 이동했더라도, 실제 판정은 장판이 표시된 위치에서 발생한다.

### 멀티 히트 텔레그래프 제작 절차

한 스킬 안에서 여러 번 다른 위치에 판정이 발생하는 경우에는 자동 표시 방식보다 MotionSet 방식이 적합하다.

1. `baseInfo.hitPhases`에 필요한 히트 페이즈를 추가한다.
2. 각 페이즈의 `attackOffset`, `attackRadius`, `damageMultiplier`를 설정한다.
3. `useMotionEventTelegraph = true`로 설정한다.
4. MotionSet에 히트 페이즈별 `TelegraphEvent`를 배치한다.
5. 각 `TelegraphEvent.hitPhaseIndex`를 대응되는 히트 페이즈 인덱스로 설정한다.
6. 각 `BeginCollisionEvent.hitPhaseIndex`도 같은 인덱스로 맞춘다.

예시:

```
0.10s  TelegraphEvent hitPhaseIndex=0 시작
0.80s  TelegraphEvent hitPhaseIndex=0 종료
0.82s  BeginCollisionEvent hitPhaseIndex=0 시작
0.95s  BeginCollisionEvent hitPhaseIndex=0 종료

1.00s  TelegraphEvent hitPhaseIndex=1 시작
1.70s  TelegraphEvent hitPhaseIndex=1 종료
1.72s  BeginCollisionEvent hitPhaseIndex=1 시작
1.85s  BeginCollisionEvent hitPhaseIndex=1 종료
```

자동 시작 방식은 0번 히트 페이즈만 표시한다. 멀티 히트는 반드시 MotionSet 이벤트로 제어한다.

### FX 프리팹 제작 기준

원형 텔레그래프 프리팹은 월드 스케일이 반경 계산과 맞아야 한다.

| 항목 | 권장 |
|------|------|
| 기본 지름 | Unity 단위 2m 기준 |
| 피벗 | 원 중심 |
| 방향 | XZ 평면에 눕힌 형태 |
| 색상 | 위험 영역을 명확히 읽을 수 있는 붉은색/주황색 계열 |
| 가장자리 | 실제 판정 경계가 보이도록 링 또는 외곽선 포함 |
| 애니메이션 | 시간이 줄어드는 수축/채움/점멸 중 하나 |
| 콜라이더 | 불필요. 시각 표시 전용 |

`EnemyCombat.ApplyTelegraphScale()`는 `attackRadius * telegraphRadiusScale` 값을 X/Z 스케일에 적용한다. 프리팹 기본 메시가 지름 2m 기준이면 `attackRadius = 2`일 때 지름 4m 범위로 보이는 구성이 자연스럽다.

### Unity 에디터 확인 순서

새 공격을 추가한 뒤에는 다음 순서로 확인한다.

1. `FXPrefabDatabase`에 `EnemyHeavyAttackTelegraph_Circle` 키가 등록되어 있는지 확인한다.
2. 몬스터 프리팹의 `EnemyCombat._telegraphGroundLayers`가 지형 레이어를 포함하는지 확인한다.
3. `EnemyAttackDataSO`에서 `useTelegraph`와 반경 값을 확인한다.
4. MotionSet 방식이면 `TelegraphEvent`와 `BeginCollisionEvent`의 `hitPhaseIndex`가 같은지 확인한다.
5. 플레이 모드에서 표시 위치와 실제 피격 위치를 비교한다.
6. 경사 지형에서 장판이 바닥에 붙는지 확인한다.
7. 몬스터가 피격/사망/상태 전환될 때 장판이 사라지는지 확인한다.

### 문제 해결

| 증상 | 확인할 항목 |
|------|------|
| 장판이 보이지 않음 | `useTelegraph`, `telegraphShape`, FX 키 등록, `FXPrefabDatabase` |
| 장판은 보이지만 타격 위치가 다름 | `useTelegraphPositionForHit`, `hitPhaseIndex`, MotionSet 이벤트 순서 |
| 장판이 몬스터를 계속 따라감 | `TelegraphEvent.lockPositionOnStart` 값 |
| 장판이 공중에 뜸 | `_telegraphGroundLayers`, `_telegraphGroundProbeHeight`, `_telegraphGroundProbeDistance` |
| 장판 크기가 판정보다 다름 | `attackRadius`, `telegraphRadiusScale`, 프리팹 기본 메시 크기 |
| 멀티 히트에서 첫 번째만 표시됨 | 자동 시작 방식 사용 여부. `useMotionEventTelegraph = true` 필요 |
| Cone/Line이 표시되지 않음 | 현재 런타임 구현은 `Circle`만 지원 |

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

### 1. FX 키 데이터화

현재 `EnemyCombat`는 `EnemyAttackInfo.telegraphFXKey`가 비어 있을 때만 기본 Circle 키를 사용한다. 형태가 늘어나면 형태별 기본 키 매핑을 확장하면 된다.

현재 필드:

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

`MotionEvent_Telegraph.cs`에는 `TelegraphEvent`가 구현되어 있다. 공격별로 예고 시작/종료 타이밍을 정밀하게 다루려면 `EnemyAttackInfo.useMotionEventTelegraph`를 켜고 MotionSet 타임라인에 `TelegraphEvent`를 배치한다.

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

텔레그래프 종료 시점은 실제 판정 시작과 같거나 아주 조금 앞서는 편이 좋다. `lockPositionOnStart == false`이면 공격자 이동과 Motion Warp를 따라가고, `true`이면 이벤트 시작 지점의 위치에 고정된다.

---

## 주의 사항

- 현재 런타임은 `Circle`만 지원한다. `EnemyAttackInfo.telegraphShape`를 `Cone` 또는 `Line`으로 설정하면 경고 로그만 출력되고 표시되지 않는다.
- 텔레그래프 크기는 `attackRadius * telegraphRadiusScale`이다. 프리팹 기본 크기와 이 계산식이 맞지 않으면 실제 판정보다 크게 또는 작게 보인다.
- `ClearTelegraphs()`는 현재 `Destroy()`를 사용한다. FX 풀링을 강하게 쓰는 프로젝트 정책과 맞추려면 반환 API로 바꾸는 것이 좋다.
- `Motion Warp` 공격은 `lockPositionOnStart == false`일 때 공격자가 이동하면서 텔레그래프도 매 프레임 갱신된다. 플레이어 입장에서는 경고 위치가 미끄러져 보일 수 있으므로 공격별로 추적형/고정형을 선택해야 한다.
- `TargetPosition` AOE는 일반적으로 `lockPositionOnStart == true`가 맞다. 추적형 장판은 의도적으로 설계한 패턴에만 사용한다.
- `useTelegraphPositionForHit == true`인데 해당 `hitPhaseIndex`의 텔레그래프가 실행되지 않으면 기존 `CasterOffset` 판정으로 폴백한다.
- 바닥 정렬 레이어에 몬스터/플레이어 콜라이더가 포함되면 텔레그래프가 잘못된 높이에 붙을 수 있다.
- 자동 시작 방식은 0번 히트 페이즈만 표시한다. 멀티 히트 공격은 `useMotionEventTelegraph`를 켜고 히트 페이즈별 `TelegraphEvent`를 배치한다.

---

## 구현 우선순위

| 순서 | 작업 | 목적 |
|------|------|------|
| 1 | 원형 텔레그래프 프리팹 품질 정리 | 현재 구현을 바로 게임에서 쓸 수 있게 함 |
| 2 | 공격별 FX 키 설정 | 몬스터/공격별 다른 표시 가능 |
| 3 | `EnemyAttackTelegraphController` 분리 | `Cone`, `Line`, 착탄형 확장 준비 |
| 4 | MotionSet에 `TelegraphEvent` 배치 | 공격별 예고 타이밍 정밀 제어 |
| 5 | `Cone` 지원 | 휘두르기/브레스 전조 |
| 6 | `Line` 지원 | 돌진/레이저 전조 |
| 7 | URP Decal Projector 프리팹 도입 | 보스 장판, 대형 광역기 품질 개선 |

---

## 테스트 체크리스트

| 항목 | 확인 내용 |
|------|-----------|
| 기본 표시 | `useTelegraph == true`인 공격에서 공격 모션 시작 후 원형 범위가 보인다 |
| 타임라인 표시 | `useMotionEventTelegraph == true`인 공격은 `TelegraphEvent` 구간에서만 범위가 보인다 |
| TargetPosition AOE | 타겟 위치에 표시된 장판과 실제 Collision 판정 위치가 같다 |
| 판정 일치 | `attackRadius`와 표시 반경의 체감이 맞다 |
| 위치 일치 | `attackOffset` 변경 시 표시 중심과 실제 판정 중심이 함께 이동한다 |
| 바닥 정렬 | 경사/계단/불규칙 지형에서 바닥 위에 표시된다 |
| 상태 중단 | 몬스터 피격, 사망, 상태 전환 시 텔레그래프가 남지 않는다 |
| Motion Warp | 워프 공격에서 범위가 부당하게 순간 이동하거나 늦게 따라오지 않는다 |
| 데이터 누락 | FX 키/프리팹 누락 시 에러가 명확히 드러난다 |
| 멀티 히트 | 0번 히트 페이즈만 표시되는 현재 제약을 인지하고 데이터가 구성되어 있다 |
