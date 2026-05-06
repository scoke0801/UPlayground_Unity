# 몬스터 강공격 텔레그래프 연출 가이드

> 작성일: 2026-05-06  
> 대상 버전: Unity 6 (6000.0.60f1), URP

---

## 개요

몬스터의 강한 공격이 실제 판정에 들어가기 전에 플레이어가 회피/가드 판단을 할 수 있도록 텔레그래프 연출을 추가하기 위한 분석 및 작업 필요사항 문서.

현재 프로젝트의 몬스터 공격은 `EnemyAttackDataSO`의 `EnemyAttackInfo`를 선택하고, `EnemyAttackState`에서 해당 `AnimKey` 모션을 재생하며, MotionSet 타임라인의 `BeginCollisionEvent`가 실제 히트 판정 구간을 켜는 구조다. 따라서 텔레그래프는 별도 공격 상태를 새로 만들기보다 **공격 데이터/히트 페이즈/모션 이벤트 타임라인에 연동되는 경고 연출 레이어**로 추가하는 것이 맞다.

핵심 목표는 다음과 같다.

- 강공격 판별 기준을 데이터로 명시한다.
- 판정 전 경고 VFX를 MotionSet 타임라인에서 재생한다.
- 기존 `BeginCollisionEvent` 시작 전까지 텔레그래프가 보이도록 타이밍을 맞춘다.
- 몬스터 피격/사망/상태 이탈 시 연출 인스턴스가 남지 않도록 정리한다.
- 기존 `EnemyCombat.CheckMeleeAttackHit()`의 실제 판정 위치/반경과 시각적 경고 범위를 일치시킨다.

---

## 현재 구조

```
BehaviorTree
└── ExecuteEnemyAttackNode
        └── EnemyAttackState 진입
                ├── EnemyCombat.SelectAndExecuteSkill(distance)
                │       └── EnemyAttackDataSO.skills 중 사용 가능한 EnemyAttackInfo 선택
                ├── ActorAnimator.PlayMotion(currentSkill.baseInfo.animKey)
                └── MotionSet 타임라인 실행
                        ├── BeginParticleEvent       기존 파티클 이벤트
                        ├── MotionEvent_MotionWarp   공격 전 보정 이동
                        └── BeginCollisionEvent      실제 히트 판정 ON/OFF
                                └── EnemyCombat.SetHitPhaseIndex(index)
                                └── EnemyCombat.SetEnableCollision(true/false)

EnemyCombat.Update/EnemyAttackState.UpdateState
└── IsPossibleCollide == true일 때 CheckMeleeAttackHit()
        └── 현재 HitPhaseData의 attackOffset/attackRadius/hitHeightRange로 OverlapSphere
```

### 관련 파일

| 파일 | 현재 역할 |
|------|-----------|
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `HitPhaseData`, `AttackInfoBase`, `EnemyAttackInfo`, `AttackData` 정의 |
| `Assets/02.Scripts/Data/Combat/EnemyAttackDataSO.cs` | 몬스터 스킬 목록, 거리 조건, 가중치 선택 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | 스킬 선택, 현재 스킬 보관, 히트 판정 위치/반경 계산 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyAttackState.cs` | 공격 상태 진입, 모션 재생, 판정 중 히트 체크 |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Collision.cs` | MotionSet 타임라인에서 히트 판정 ON/OFF |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Particle.cs` | 타임라인 기반 파티클 생성/정리 |
| `Assets/02.Scripts/Manager/Object/GameObjectManager.FX.cs` | `FXPrefabDatabase` 기반 FX 생성 및 수명 관리 |
| `Assets/02.Scripts/Data/Path/FXKeyType.cs` | FX 키 자동 생성 enum |

---

## 확인된 코드 지점

### 공격 데이터

`HitPhaseData`는 이미 히트 구간별 수치와 반응 타입을 가지고 있다.

| 위치 | 내용 |
|------|------|
| `CombatData.cs:15` | `HitPhaseData` 정의 |
| `CombatData.cs:21` | `AttackReactionType reactionType` 보유 |
| `CombatData.cs:53` | `AttackInfoBase` 정의 |
| `CombatData.cs:82` | `EnemyAttackInfo` 정의 |
| `CombatData.cs:87` | `SkillType skillType` 보유 |
| `CombatData.cs:91` | `selectionWeight` 보유 |
| `CombatData.cs:114` | `IsInRange(float distance)`로 거리 조건 확인 |

현재 `EnemyAttackInfo`에는 플레이어 공격의 `AttackKind.HeavyAttack` 같은 강공격 구분 필드가 없다. 몬스터 강공격을 안정적으로 판별하려면 새 데이터 필드가 필요하다.

### 공격 실행

`EnemyCombat`는 현재 선택된 스킬과 현재 히트 페이즈를 알고 있으며, 텔레그래프 위치 계산에 필요한 API 일부를 이미 가지고 있다.

| 위치 | 내용 |
|------|------|
| `EnemyCombat.cs:34` | `_currentSkill` 보관 |
| `EnemyCombat.cs:59` | `CurrentSkill` public getter |
| `EnemyCombat.cs:165` | `SelectAndExecuteSkill(float distanceToTarget)` |
| `EnemyCombat.cs:178` | 사용 가능한 스킬 중 `_currentSkill` 선택 |
| `EnemyCombat.cs:275` | `CheckMeleeAttackHit()` |
| `EnemyCombat.cs:280` | 현재 `HitPhaseData` 기준으로 판정 위치 계산 |
| `EnemyCombat.cs:338` | `SetHitPhaseIndex(int index)` |
| `EnemyCombat.cs:341` | `GetCurrentAttackPosition()` |
| `EnemyCombat.cs:351` | `GetCurrentAttackRadius()` |

`GetCurrentAttackPosition()`와 `GetCurrentAttackRadius()`는 텔레그래프 MotionEvent가 현재 히트 페이즈 기준 경고 영역을 만들 때 재사용할 수 있다.

### 공격 상태

`EnemyAttackState`는 공격 시작과 종료 정리 지점이 명확하다.

| 위치 | 내용 |
|------|------|
| `EnemyAttackState.cs:50` | 공격 상태 `OnEnter` |
| `EnemyAttackState.cs:58` | 공격 중 HyperArmor 활성 |
| `EnemyAttackState.cs:65` | 선택된 스킬의 `AnimKey` 모션 재생 |
| `EnemyAttackState.cs:88` | 공격 상태 `OnExit` |
| `EnemyAttackState.cs:98` | HyperArmor 해제 |
| `EnemyAttackState.cs:109` | 판정 활성 중 `CheckMeleeAttackHit()` 호출 |

텔레그래프 인스턴스 정리는 MotionEvent 자체의 `OnCompleteEvent`와 함께 `EnemyAttackState.OnExit`에서도 보장하는 편이 안전하다.

### MotionEvent

`BeginCollisionEvent`는 히트 페이즈와 실제 판정 ON/OFF 타이밍을 이미 타임라인에서 제어한다.

| 위치 | 내용 |
|------|------|
| `MotionEvent_Collision.cs:13` | `BeginCollisionEvent` 정의 |
| `MotionEvent_Collision.cs:18` | `hitPhaseIndex` 보유 |
| `MotionEvent_Collision.cs:69` | 몬스터 공격의 현재 히트 페이즈 지정 |
| `MotionEvent_Collision.cs:71` | 몬스터 충돌 판정 활성/비활성 |

`BeginParticleEvent`는 타임라인 기반 파티클 재생이 가능하지만, 현재 필드는 프리팹 직접 참조와 소켓 기준 배치에 맞춰져 있다. 바닥 범위 경고처럼 현재 공격 반경을 읽어 스케일링해야 하는 텔레그래프에는 전용 MotionEvent가 필요하다.

---

## 작업 필요사항

### 1. 강공격 판별 데이터 추가

`EnemyAttackInfo`에 강공격/텔레그래프 여부를 명시하는 필드를 추가한다.

권장 필드:

```csharp
[Header("Telegraph")]
public bool useTelegraph = false;
public TelegraphShape telegraphShape = TelegraphShape.Circle;
public float telegraphDuration = 0.6f;
public float telegraphRadiusScale = 1f;
public string telegraphFXKey = "";
```

권장 enum:

```csharp
public enum TelegraphShape
{
    Circle,
    Cone,
    Line
}
```

초기 버전에서는 `Circle`만 구현해도 된다. 현재 근접 판정이 `OverlapSphere` 기반이므로 `Circle`이 가장 적은 변경으로 실제 판정과 맞는다.

강공격 판별 정책은 다음 중 하나로 고정해야 한다.

| 방식 | 장점 | 단점 |
|------|------|------|
| `useTelegraph` 직접 체크 | 기획자가 공격별로 명확히 제어 가능 | 모든 강공격 에셋에 수동 설정 필요 |
| `reactionType` 기반 자동 판별 | 기존 데이터만으로 빠른 적용 가능 | `KnockBack`, `Airborne` 등 모든 강한 리액션이 항상 경고 대상인지 애매함 |
| `poiseDamage`/`damage` 임계값 | 대량 데이터 자동 분류 가능 | 밸런스 조정 시 연출 대상이 의도치 않게 바뀜 |

권장안은 `useTelegraph`를 명시 필드로 두고, 에디터 마이그레이션 단계에서 `AttackReactionType.Heavy`, `KnockBack`, `Airborne`, `Knockdown`, `Grab`을 가진 공격에 기본값을 제안하는 방식이다.

### 2. 텔레그래프 MotionEvent 추가

신규 파일 후보:

`Assets/02.Scripts/Data/Event/Animation/MotionEvent_Telegraph.cs`

역할:

- MotionSet 타임라인에서 특정 시점에 텔레그래프 FX를 생성한다.
- `MonsterActor.Combat.CurrentSkill`과 `hitPhaseIndex`를 읽는다.
- `EnemyCombat.GetCurrentAttackPosition()` / `GetCurrentAttackRadius()` 기준으로 위치와 크기를 맞춘다.
- 이벤트 종료 시 FX를 제거하거나 페이드 아웃한다.

초기 API 후보:

```csharp
[Serializable]
public class BeginTelegraphEvent : MotionEventBase
{
    public int hitPhaseIndex = 0;
    public string fxKey = "";
    public bool useCurrentHitRadius = true;
    public float radiusScale = 1f;
    public Vector3 offset;

    public override string GetDisplayName() => "Telegraph";
    public override string GetShortLabel() => $"Telegraph [{hitPhaseIndex}]";
}
```

이 이벤트는 기존 `BeginCollisionEvent`와 같은 `hitPhaseIndex`를 사용해야 한다. 멀티 히트 강공격은 각 히트 구간마다 별도 텔레그래프 이벤트를 배치한다.

### 3. 텔레그래프 FX 생성 경로 결정

현재 프로젝트에는 두 가지 FX 경로가 있다.

| 방식 | 사용 지점 | 특징 |
|------|-----------|------|
| 프리팹 직접 참조 | `BeginParticleEvent.particlePrefab` | MotionSet 에셋에 직접 프리팹을 물리기 쉬움 |
| 키 기반 생성 | `GameObjectManager.ShowFX(string key, ...)` | Addressables의 `FXPrefabDatabase`와 연동 |

텔레그래프는 여러 몬스터/공격 데이터에서 공통 재사용될 가능성이 높으므로 키 기반을 권장한다. 단, `FXKeyType.cs`는 자동 생성 파일이므로 직접 수정하지 않고 `UPlayGround/ID Enum Generator` 창에서 재생성해야 한다.

필요한 데이터 작업:

- `FXPrefabDatabase.asset`에 `EnemyHeavyAttackTelegraph_Circle` 같은 키 추가
- `FXKeyType` 재생성
- 원형 경고 프리팹 제작
- 프리팹 피벗은 바닥 중심, 기본 반지름은 1m 기준으로 통일

### 4. 상태 이탈 시 정리 보장

MotionEvent의 `OnCompleteEvent`만 믿으면 피격/사망/상태 전환으로 모션이 끊겼을 때 경고 FX가 남을 수 있다. `MotionEventExecutor.Stop()`은 활성 이벤트의 `OnCompleteEvent`를 호출하지만, 공격 상태 종료 시점과 모션 정지 호출 순서가 항상 명확하다고 가정하지 않는 편이 좋다.

권장 작업:

- `EnemyCombat`에 현재 텔레그래프 인스턴스 등록/해제 API 추가
- `EnemyAttackState.OnExit`에서 `EnemyCombat.ClearTelegraphs()` 호출
- `BeginTelegraphEvent.OnCompleteEvent`에서도 동일하게 개별 인스턴스 제거

신규 API 후보:

```csharp
public void RegisterTelegraph(GameObject instance);
public void UnregisterTelegraph(GameObject instance);
public void ClearTelegraphs();
```

### 5. 에디터 UI 보강

`EnemyAttackDataSOEditor`는 `EnemyAttackDataSODrawer`를 통해 커스텀 인스펙터를 사용한다. `EnemyAttackInfo`에 필드가 늘어나면 Drawer에도 텔레그래프 섹션을 추가해야 한다.

표시 권장:

| 필드 | 표시 조건 |
|------|-----------|
| `useTelegraph` | 항상 표시 |
| `telegraphShape` | `useTelegraph == true` |
| `telegraphDuration` | `useTelegraph == true` |
| `telegraphRadiusScale` | `useTelegraph == true` |
| `telegraphFXKey` | `useTelegraph == true` |

추가로 `reactionType`이 강한 리액션인데 `useTelegraph == false`이면 인스펙터 경고를 표시하면 데이터 누락을 줄일 수 있다.

### 6. MotionSet 타임라인 설정 규칙

강공격 모션에는 다음 순서로 이벤트를 배치한다.

```
0.00s        공격 모션 시작
0.10~0.20s   BeginTelegraphEvent 시작
0.65s        BeginTelegraphEvent 종료
0.65s        BeginCollisionEvent 시작
0.80s        BeginCollisionEvent 종료
```

규칙:

- `BeginTelegraphEvent.endTime`은 `BeginCollisionEvent.startTime`과 같거나 조금 앞서야 한다.
- `hitPhaseIndex`는 두 이벤트가 같은 값을 사용해야 한다.
- Motion Warp를 쓰는 공격은 워프가 끝난 뒤 최종 판정 위치 기준으로 텔레그래프를 보여줄지, 워프 중 따라가게 할지 정책을 정해야 한다.
- 첫 구현은 바닥 고정형으로 두고, `attachToTarget` 방식의 추적형 경고는 이후 확장으로 미룬다.

---

## 구현 순서 제안

1. `CombatData.cs`에 `TelegraphShape`와 `EnemyAttackInfo` 텔레그래프 필드를 추가한다.
2. `EnemyCombat`에 텔레그래프 인스턴스 등록/정리 API를 추가한다.
3. `MotionEvent_Telegraph.cs`를 추가해 원형 경고 FX를 생성/정리한다.
4. `EnemyAttackState.OnExit`에서 남은 텔레그래프를 정리한다.
5. `EnemyAttackDataSODrawer`에 텔레그래프 필드 UI와 경고 표시를 추가한다.
6. `FXPrefabDatabase.asset`에 원형 텔레그래프 프리팹 키를 추가하고 `FXKeyType`을 재생성한다.
7. Golem, Griffin, Spider_Queen 등 강한 리액션 공격 에셋부터 `useTelegraph`를 켠다.
8. 각 강공격 MotionSet에 `BeginTelegraphEvent`를 `BeginCollisionEvent` 직전에 배치한다.

---

## 테스트 체크리스트

| 항목 | 확인 내용 |
|------|-----------|
| 기본 표시 | 강공격 시작 후 판정 전 텔레그래프가 보인다 |
| 판정 일치 | 텔레그래프 반경과 실제 `attackRadius` 체감 범위가 맞다 |
| 히트 페이즈 | 멀티 히트 공격에서 각 경고가 올바른 `hitPhaseIndex`를 사용한다 |
| 상태 중단 | 몬스터가 피격/사망/상태 전환될 때 텔레그래프가 남지 않는다 |
| Motion Warp | 워프 공격에서 경고 위치가 플레이어에게 부당하게 느껴지지 않는다 |
| 데이터 누락 | `useTelegraph`가 켜졌지만 FX 키가 비었을 때 에러 또는 경고가 표시된다 |
| Addressables | `FXPrefabDatabase`가 로드된 뒤 키 기반 FX 생성이 동작한다 |

---

## 주의 사항

- 현재 `AttackReactionType.Heavy`는 enum 값상 강한 피격 반응이지만, 일부 몬스터 에셋은 `KnockBack` 계열을 강공격처럼 사용한다. 강공격을 `Heavy` 하나로만 판별하면 누락될 수 있다.
- `EnemyAttackInfo`는 직렬화 데이터이므로 필드 추가 후 기존 `.asset` 파일의 기본값이 의도대로 들어갔는지 Unity Inspector에서 확인해야 한다.
- `FXKeyType.cs`는 자동 생성 파일이다. 수동 편집 대신 ID Enum Generator로 재생성한다.
- `BeginParticleEvent`는 프리팹 직접 참조 기반이라 빠른 임시 구현에는 쓸 수 있지만, 현재 공격 반경을 읽어 스케일링하는 범위 경고에는 전용 이벤트가 더 적합하다.
- 원거리 공격 텔레그래프는 현재 문서 범위 밖이다. 원거리 투사체 예고선/착탄 범위는 `BaseProjectile`/`AOEProjectile` 흐름까지 별도 분석이 필요하다.

---

## 확장 포인트

| 확장 | 설명 |
|------|------|
| `Cone` 텔레그래프 | 전방 부채꼴 공격용. 실제 판정도 각도 기반으로 맞추는 추가 작업 필요 |
| `Line` 텔레그래프 | 돌진/브레스/레이저 공격용. Motion Warp와 충돌 범위 정책 정리 필요 |
| 색상 단계 | 남은 시간에 따라 노랑 → 빨강으로 머티리얼 파라미터 변경 |
| 풀링 | 텔레그래프가 자주 생성되면 `GameObjectManager` FX 풀링으로 확장 |
| 에디터 자동 배치 | `BeginCollisionEvent` 앞에 `BeginTelegraphEvent`를 자동 삽입하는 MotionSet 에디터 기능 |

