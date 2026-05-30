# 캐릭터 스왑 잔류 공격 시스템 설계 문서

> 작성일: 2026-05-26  
> 구현 갱신: 2026-05-27  
> 위치 정책 설계 갱신: 2026-05-29  
> 콤보 상태 구현 갱신: 2026-05-29  
> Phase 2 피드백 구현 갱신: 2026-05-29  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 레퍼런스: 명조 `Intro / Outro Skill`, 퀵스왑 잔류 공격

---

## 구현 현황

2026-05-27 기준으로 Phase 1 기반 런타임 구조와 전체 플레이어 공격 상태 잔류 허용까지 구현했다.

| 항목 | 상태 | 구현 파일 |
|------|------|-----------|
| 공격 스냅샷 생성 | 완료 | `PlayerCombat.TryCreateResidualAttackSnapshot()` |
| MotionEvent 전투 타겟 인터페이스 | 완료 | `IMotionEventCombatTarget` |
| Collision 이벤트 라우팅 확장 | 완료 | `MotionEvent_Collision`, `MotionEvent_DisableCollision` |
| 잔류 전용 히트 판정 | 완료 | `ResidualPlayerCombat` |
| 잔류 모델 러너 | 완료 | `SwapResidualAttackRunner` |
| 스왑 시 잔류 러너 생성 | 완료 | `PlayerSwapBehaviour.SwapTo()` |
| 잔류 옵션 데이터 | 완료 | `PartyConfigSO`, `PartyManager` 읽기 전용 프로퍼티 |
| 캐릭터별 콤보 상태 저장/복원 | 완료 | `PlayerCombat.SaveComboState()`, `PlayerCombat.RefreshAttackData(...)` |
| 같은 캐릭터 재교체 시 잔류 러너 정리 | 완료 | `SwapResidualAttackRunner.CancelRunnersForCharacter()` |
| 같은 캐릭터 잔류 위치 복귀 | 완료 | `SwapResidualAttackRunner.TryConsumeRunnerPosition()`, `PlayerSwapBehaviour.TryReturnToResidualRunner()` |
| 잔류 공격 히트스톱 별도 설정 | 완료 | `PartyConfigSO.residualAttackHitStopDuration`, `residualAttackHitStopTimeScale` |
| 잔류 피드백 중복 억제 | 완료 | `PartyConfigSO.residualAttackFeedbackMinInterval` |
| 잔류 트레일/디졸브 정리 | 완료 | `SwapResidualAttackRunner.Cancel()`, `DissolveController` |
| 잔류 데미지 플로터 캐릭터 표시 옵션 | 완료 | `residualAttackShowCharacterOnDamageFloater`, `ShowDamageFloaterLabel()` |
| 잔류 루트모션 제한 이동 | 부분 완료 | `residualAttackUseRootMotion`, 최대 이동 거리, blocker SphereCast. 플레이 검증 필요 |
| 빌드 검증 | 부분 완료 | 런타임 `dotnet build Assembly-CSharp.csproj --no-restore` 성공. 전체 솔루션은 별도 에디터 파일 `MotionSetWindow.cs` 누락 메서드로 실패 |

현재 구현 정책:

- 잔류 공격 시스템은 항상 활성화한다. `PartyConfigSO.enableResidualAttackOnSwap` 필드는 과거 설계 옵션으로 남아 있어도 런타임에서는 끄기 스위치로 사용하지 않는다.
- 잔류 허용 공격은 현재 `PlayerCombat.CurrentAttackData`가 있고 공격 MotionSet 스냅샷이 유효한 모든 플레이어 공격 상태다. 현재 포함 상태는 `Attack`, `JumpAttack`, `JumpDashAttack`, `Charge`, `FinishAttack`, `SpecialBreakAttack`이다.
- 잔류 모델은 원본 모델을 분리하지 않고 복제본을 사용한다.
- 잔류 모델은 `PlayerActor`가 아니며, 입력 / 피격 / KCC 상태 머신을 갖지 않는다.
- 잔류 러너 루트와 `ResidualPlayerCombat` 판정 위치는 스왑 시점의 원래 위치를 유지한다.
- 캐릭터 위치는 별도 오프셋으로 밀어내지 않는다. 공격 중 스왑 시 outgoing 잔류 모델과 incoming 원본 모델은 스왑 시점의 기본 위치 정책을 따른다.
- 같은 캐릭터 타입의 잔류 모델이 아직 필드에 남아 있고 `residualAttackReturnToSameCharacterRunner`가 켜져 있으면, 해당 캐릭터로 재스왑할 때 잔류 러너 위치로 복귀한 뒤 러너를 제거한다.
- 콤보 카운트는 단일 `PlayerCombat` 전역 상태가 아니라 캐릭터 타입별로 보존한다. 예를 들어 1번 캐릭터가 2타까지 진행한 뒤 2번 캐릭터로 1타를 치고 다시 1번으로 돌아오면, 1번 캐릭터는 3타 콤보를 이어간다.
- 캐릭터별 콤보 상태는 `PartyConfigSO.preserveComboStatePerCharacter`와 `comboStateMaxCarryTime`으로 제어한다. 등장 공격은 일반 콤보 상태를 소비하지 않도록 별도 실행한다.
- 같은 캐릭터 타입의 잔류 모델이 남아 있는 상태에서 해당 캐릭터로 다시 교체하면, `residualAttackReturnPositionMaxAge` 안의 러너 위치/회전으로 플레이어 KCC를 이동시킨 뒤 원본 모델을 활성화한다.
- 잔류 공격은 `ResidualPlayerCombat`가 별도 `AttackData` 복사본과 `_hitTargets`를 사용한다.
- `MotionEventExecutor.SetTargetObject()`로 이벤트 타겟을 잔류 러너 루트에 명시 지정한다.
- 루트모션은 `PartyConfigSO.residualAttackUseRootMotion` 기본값 `false`로 둔다.
- 잔류 공격 `OnAttackHit`는 `PartyManager.OnPlayerAttackHit`에 연결하지 않는다. 따라서 1차 정책상 파티 스킬 게이지 중복 충전은 발생하지 않는다.
- 차지 공격처럼 `AttackInfoBase`가 없는 공격은 스냅샷에 `HitPhaseData` 목록을 별도로 보존해 Collision 이벤트의 `hitPhaseIndex`를 처리한다.
- `FinishAttackEvent`, `SpecialBreakAttackEvent`는 잔류 전용 인터페이스를 우선 확인해 기존 `PlayerActorState` 없이도 잔류 러너에서 발동할 수 있다.
- 종료 처리는 MotionSet 완료 또는 타임아웃 시 콜리전/트레일을 끄고, `residualAttackFadeOutDuration`이 0보다 크면 디졸브 후 제거한다. 교체/씬 전환 등 강제 취소는 즉시 제거한다.

아직 Phase 2/3을 남긴 이유:

1. 우선순위는 “공격 중 스왑해도 이전 모델의 남은 MotionSet 이벤트가 독립 실행된다”는 런타임 구조 안정화다.
2. Phase 2의 카메라, 히트스톱, 트레일, 디졸브는 체감 품질 영역이라 실제 플레이 확인 후 강도와 중복 억제 정책을 정해야 한다.
3. Phase 3의 Intro / Outro, `swapSpecialAttack`, 버프, HUD는 전투 밸런스와 데이터 구조 변경 범위가 커서 잔류 러너 안정화 후 분리 구현하는 편이 안전하다.
4. 루트모션/워프/차지/스킬 공격까지 한 번에 열면 KCC 없는 잔류 모델의 위치 보정, 벽 관통, 무한 루프, 보상 중복 문제가 동시에 생긴다.

---

## 개요

현재 파티 교체는 단일 `PlayerActor`를 유지하고 `PlayerSwapBehaviour`가 하위 `CharacterModelData` 모델을 비활성/활성 전환하는 구조다. 이 방식은 체력, 스킬 게이지, 카메라 타겟, 입력, 상태 머신을 단순하게 유지한다는 장점이 있지만, 교체 순간 이전 모델이 즉시 사라지기 때문에 공격 모션 후반부, 잔여 히트, 퇴장 연출을 처리할 수 없다.

이 문서는 명조식 교대 전투를 참고해, 기존 캐릭터가 공격 중인 상태에서 스왑되면 이전 모델을 짧게 필드에 남겨 남은 공격 MotionSet 이벤트와 히트 판정을 처리한 뒤 사라지게 만드는 확장 설계다.

핵심 목표:

- 단일 `PlayerActor` 운영 방식은 유지한다.
- 퇴장 캐릭터 모델만 임시 잔류시켜 남은 공격 모션과 타임라인 이벤트를 실행한다.
- incoming 캐릭터의 기존 `entryAttack` / `swapSpecialAttack` 구조와 충돌하지 않는다.
- 잔류 공격은 별도 입력, 피격, KCC 상태 머신을 갖지 않는 실행 전용 컨텍스트로 제한한다.
- 다단 히트, MotionEvent 기반 Collision, VFX/SFX, 히트스톱 보상을 가능한 한 기존 데이터와 공유한다.

---

## 레퍼런스 조사

### 명조에서 확인한 구조

| 항목 | 레퍼런스 내용 | 설계 반영 |
|------|---------------|-----------|
| Intro / Outro | Concerto Energy가 가득 찬 상태에서 교체하면 퇴장 캐릭터의 Outro와 입장 캐릭터의 Intro가 발동한다. | 퇴장 캐릭터와 입장 캐릭터가 짧은 시간 동시에 전투 기여를 한다는 전투 감각을 목표로 한다. |
| 동시 실행 | Intro와 Outro가 동시에 실행되어 짧은 창 동안 두 캐릭터가 배치되고 피해를 준다는 설명이 있다. | 잔류 모델은 `PlayerActor`가 아니지만, 필드에 남아 별도 공격 판정을 실행한다. |
| 퀵스왑 | 일부 가이드에서는 빠른 스왑으로 버프 유지나 캐릭터 잔류 상태를 활용할 수 있다고 설명한다. | 일반 공격 도중 스왑해도 퇴장 모델이 남은 공격 프레임을 끝내고 사라지는 방향으로 해석한다. |
| 스왑 캔슬 | 커뮤니티/가이드 자료는 긴 공격 애니메이션 중 교체해도 이전 캐릭터의 공격이 끝까지 실행되는 것을 퀵스왑 핵심으로 설명한다. | outgoing 캐릭터는 스왑 시점 위치에 남아 남은 MotionSet을 실행한다. |
| 위치 처리 | 공개 텍스트 자료는 좌표 계산 방식까지 공식적으로 설명하지 않는다. | UPlayground는 임의 오프셋 이동을 하지 않고, 이후 잔류 모델의 루트모션을 고도화해 공격 모션 자체가 자연스럽게 전진하도록 처리한다. |
| 게이지 기반 강화 | Intro / Outro는 일반적으로 게이지가 찼을 때 강한 효과로 발동한다. | 1차 구현은 공격 중 스왑 잔류만 처리하고, 이후 `swapSpecialAttack` / 파티 스킬 게이지와 연결한다. |

참고 자료:

- WutheringWaves.gg, Intro & Outro Skill System Guide: https://wutheringwaves.gg/intro-outro-explained/
- Game8, Concerto Energy Guide: https://game8.co/games/Wuthering-Waves/archives/456637
- Pro Game Guides, What is Swap Canceling in Wuthering Waves: https://progameguides.com/wuthering-waves/what-is-swap-canceling-in-wuthering-waves/
- Destructoid, How to Swap Cancel in Wuthering Waves: https://www.destructoid.com/how-to-swap-cancel-in-wuthering-waves/
- Wuthering Waves Wiki, Concerto Energy: https://wutheringwaves.fandom.com/wiki/Concerto_Energy
- Reddit 플레이어 설명 사례, Outro 시 캐릭터 잔류 언급: https://www.reddit.com/r/WutheringWavesGuide/comments/1d2pgea/is_there_indicators_when_a_character_does_their/
- Reddit 플레이어 설명 사례, 퀵스왑 시 공격 후 짧은 필드 잔류 언급: https://www.reddit.com/r/WutheringWavesGuide/comments/1t9nznr/how_to_quickswap_properly_and_what/

### 명조 위치 정책 해석

웹 자료로 확실히 확인되는 부분은 다음이다.

1. Concerto가 가득 찬 상태에서 교체하면 outgoing Outro와 incoming Intro가 연결된다.
2. 퀵스왑/스왑 캔슬은 긴 공격 애니메이션 도중 교체해도 이전 캐릭터의 공격이 계속 실행되는 플레이로 설명된다.
3. 일부 플레이어 설명은 공격 후 이전 캐릭터가 짧게 필드에 남는다고 말한다.

반대로, 공개 텍스트 자료만으로는 명조가 내부적으로 어떤 좌표 알고리즘을 쓰는지 확정할 수 없다. 따라서 좌표 정책은 “명조 내부 복제”가 아니라, 사용자가 관찰한 명조식 체감과 위 레퍼런스가 확인하는 퀵스왑 구조를 UPlayground에 맞춰 해석한 설계로 둔다.

UPlayground의 후속 위치 정책은 다음을 우선한다.

1. 공격 중 스왑하면 outgoing 잔류 모델은 스왑 시점 위치에서 공격을 계속한다.
2. incoming 캐릭터 또는 잔류 모델을 별도 오프셋으로 밀어내지 않는다.
3. 같은 캐릭터 타입으로 다시 스왑할 때 해당 캐릭터의 잔류 모델이 아직 존재하면, 그 잔류 모델 위치로 복귀하는 정책은 별도 검토한다.

### UPlayground에 맞춘 해석

명조의 정확한 내부 구현을 복제하는 것이 목표는 아니다. UPlayground에서는 다음 감각만 가져온다.

1. 스왑은 전투를 끊는 버튼이 아니라 콤보를 이어붙이는 버튼이다.
2. 퇴장 캐릭터의 마지막 행동은 즉시 취소되지 않고 짧게 완주할 수 있다.
3. 입장 캐릭터와 퇴장 캐릭터 위치는 별도 오프셋으로 분리하지 않는다.
4. 잔류 캐릭터의 전진/이탈감은 좌표 오프셋이 아니라 루트모션 또는 루트모션 보정 이벤트로 만든다.
5. 입장 캐릭터의 등장 공격과 퇴장 캐릭터의 잔류 공격이 겹치면 순간적으로 2인 협공처럼 보인다.
6. 캐릭터별 콤보 진행도는 스왑으로 즉시 초기화하지 않고, 각 캐릭터가 마지막으로 남긴 콤보 인덱스와 유효 시간을 유지한다.
7. 이중 조작 캐릭터가 생기면 시스템 복잡도가 급증하므로, 잔류 캐릭터는 입력/피격/이동 상태를 갖지 않는 공격 러너로 제한한다.

---

## 현재 기반

### 이미 구현된 기능

| 시스템 | 현재 역할 |
|--------|-----------|
| `PartyManager` | `RequestSwapTo()`에서 교체 가능 여부, 쿨다운, `OnSwapStarted`, `OnSwapCompleted`, 등장 공격 큐를 처리한다. |
| `PlayerSwapBehaviour` | 단일 `PlayerActor` 하위 `CharacterModelData` 모델을 교체한다. 현재는 이전 모델을 즉시 `SetActive(false)` 한다. |
| `CharacterModelData` | 캐릭터 타입, 기본 무기, `PlayerAttackDataSO`, 소켓, `AnimancerComponent`를 보유한다. |
| `PlayerActor.RefreshForCharacter()` | 캐릭터별 HP/스킬 게이지를 저장/복원하고 활성 모델의 `ActorAnimator`, `PlayerEquipment`, 공격 데이터를 갱신한다. |
| `PlayerCombat` | `ExecuteAttack`, `ExecuteEntryAttack`, `ExecuteSwapSpecialAttack`, `PerformHitDetection`, `SetHitPhaseIndex` 등 공격 데이터와 히트 판정을 처리한다. |
| `PlayerAttackState` | 공격 MotionSet 재생, Motion Warp, 콤보 전환, `OnMotionSetCompleted` 후 상태 전이를 처리한다. |
| `MotionEventExecutor` | MotionSet 타임라인 이벤트를 실행한다. `BeginCollisionEvent`가 `PlayerCombat`의 충돌 판정을 켜고 끈다. |
| `AnimKey.Player_SwapAttack_*` | 교체 등장 공격용 키가 이미 정의되어 있다. |
| `PlayerAttackDataSO.entryAttack` | 교체 등장 공격 데이터를 보유한다. |
| `PlayerAttackDataSO.swapSpecialAttack` | 풀 게이지 교체 특수공격 데이터 필드가 있다. 현재 `PartyManager`에서는 임시 비활성화 상태다. |

### 현재 한계

| 한계 | 이유 |
|------|------|
| 퇴장 모델 즉시 소멸 | `PlayerSwapBehaviour.SwapTo()`가 `_activeModel.gameObject.SetActive(false)`를 즉시 호출한다. |
| 공격 타임라인 보존 불가 | `RefreshForCharacter()`가 활성 모델 기준으로 `ActorAnimator`와 `PlayerCombat` 참조를 새로 잡는다. |
| 잔류 모델 히트 판정 불가 | `BeginCollisionEvent`는 `GameActor`를 찾아 `PlayerActor.GetCombat()`에만 이벤트를 전달한다. |
| 동일 `PlayerCombat` 공유 위험 | 스왑 후 incoming 공격과 outgoing 잔류 공격이 같은 `_currentAttackData`, `_hitTargets`, `_isCollideCollisionEnable`를 공유하면 데이터가 덮인다. |
| KCC 상태 머신 복제 부적합 | 퇴장 모델까지 `PlayerActorState`와 KCC를 복제하면 카메라, 입력, HP, 피격, GameObjectManager 참조가 꼬인다. |

---

## 목표 아키텍처

```
PartyManager.RequestSwapTo()
        │
        ▼
PlayerSwapBehaviour.SwapTo(targetType)
        │
        ├── 공격 중인 outgoing 모델 스냅샷 생성
        │       └── SwapResidualAttackRunner.Spawn(...)
        │
        ├── outgoing 모델은 원래 루트에서 분리/복제 후 잔류 실행
        │
        └── incoming 모델 활성화 + PlayerActor.RefreshForCharacter()
                │
                └── 기존 entryAttack / swapAssist 큐 유지

SwapResidualAttackRunner
├── CharacterActorType ownerType
├── PlayerAttackDataSO attackData
├── ActorAnimator residualAnimator
├── ResidualPlayerCombat residualCombat
├── float maxLifetime
├── bool followRootMotion
├── Vector3 residualModelPosition
├── Quaternion residualModelRotation
└── OnMotionSetCompleted / timeout / cancel 조건에서 정리
```

### 핵심 원칙

1. `PlayerActor`는 하나만 유지한다.
2. 잔류 모델은 `PlayerActor`가 아니라 `SwapResidualAttackRunner`의 소유 오브젝트다.
3. 잔류 공격은 독립 `ResidualPlayerCombat`가 처리한다.
4. 잔류 모델은 입력, 피격, HP, 스킬 게이지, 파티 슬롯 상태를 변경하지 않는다.
5. 스왑 시점에 진행 중이던 공격 MotionSet의 남은 구간만 실행한다.
6. 캐릭터 위치는 별도 오프셋으로 밀어내지 않는다.
7. 잔류 모델의 자연스러운 전진은 후속 루트모션 고도화로 처리한다.
8. 콤보 진행도는 캐릭터 타입별로 보존해 빠른 스왑 후 복귀해도 해당 캐릭터의 다음 콤보 타수를 이어갈 수 있게 한다.
9. 새 캐릭터의 `entryAttack`은 기존 방식대로 `PlayerAttackState`에서 실행한다.

### 위치 정책

기본 위치 정책은 “별도 위치 밀어내기 없음”이다.

| 상황 | 위치 처리 |
|------|----------|
| 공격 중 A에서 B로 스왑 | A 잔류 러너는 스왑 시점 위치에 생성한다. B 원본 모델도 기존 스왑 흐름의 기본 위치를 유지한다. |
| A 잔류 중 B에서 A로 재스왑 | 같은 캐릭터 잔류 위치 복귀는 후속 검토 항목으로 둔다. 구현 전까지는 기존 스왑 위치를 유지한다. |
| 잔류 모델 전진 필요 | 별도 좌표 오프셋 대신 루트모션 또는 루트모션 보정 이벤트로 처리한다. |
| 벽/타겟 관통 위험 | 루트모션 고도화 단계에서 충돌 보정과 이동 제한을 함께 처리한다. |

이 정책은 임의 좌표 보정으로 캐릭터가 튀는 문제를 피하고, 공격 애니메이션 자체의 이동감으로 잔류 공격을 읽히게 만드는 방향이다.

### 캐릭터별 콤보 상태 정책

현재 단일 `PlayerActor` 구조에서는 `PlayerCombat.CurrentComboIndex`, `LastAttackTime`, `CanCombo`, `_attackState`가 활성 캐릭터 교체와 함께 사실상 전역 상태처럼 취급될 수 있다. 후속 구현에서는 이 값을 캐릭터 타입별로 저장/복원한다.

목표 예시:

```
1번 캐릭터: 1타 → 2타
스왑: 2번 캐릭터
2번 캐릭터: 1타
스왑: 1번 캐릭터
1번 캐릭터: 3타
```

권장 데이터:

```csharp
public struct CharacterComboState
{
    public CharacterActorType characterType;
    public int currentComboIndex;
    public float lastAttackTime;
    public bool canCombo;
    public AttackState attackState;
    public AnimKey lastAttackAnimKey;
}
```

저장 위치는 `PlayerCombat` 내부 딕셔너리 또는 `PlayerActor`의 캐릭터별 런타임 상태 저장소가 후보가 된다. `PartyManager`는 파티/교체 정책을 관리하고, 실제 콤보 판정과 공격 데이터 선택은 `PlayerCombat` 책임으로 남기는 편이 낫다.

운영 규칙:

| 상황 | 처리 |
|------|------|
| 캐릭터가 공격을 시작함 | 해당 캐릭터 타입의 `CharacterComboState`를 갱신한다. |
| 다른 캐릭터로 스왑 | outgoing 캐릭터의 콤보 인덱스와 마지막 공격 시간을 저장한다. incoming 캐릭터의 저장된 콤보 상태를 복원한다. |
| 같은 캐릭터로 빠르게 복귀 | 콤보 유효 시간이 남아 있으면 다음 콤보 인덱스부터 공격한다. |
| 콤보 유효 시간 초과 | 해당 캐릭터의 콤보 상태를 0타 시작 상태로 초기화한다. |
| 잔류 공격 실행 중 | 잔류 러너는 콤보 상태를 갱신하지 않는다. 콤보 진행은 실제 조작 가능한 `PlayerCombat`만 갱신한다. |
| entryAttack / swapAssist | 일반 콤보 인덱스를 소비할지 별도 정책 필요. 1차 권장은 일반 콤보 상태를 변경하지 않는 것이다. |

이 정책은 “캐릭터마다 자기 콤보 흐름을 들고 있다”는 감각을 만든다. 단, 스왑만으로 후딜 제거와 고타수 콤보 유지가 동시에 가능해지므로 콤보 유효 시간은 현재보다 짧게 잡거나, 스왑 중 경과 시간도 그대로 계산해야 한다.

---

## 신규 구성 요소

### `SwapResidualAttackSettings`

`PartyConfigSO` 또는 별도 `ScriptableObject`에 둘 수 있다. 1차 구현은 `PartyConfigSO` 필드로 충분하다.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `enableResidualAttackOnSwap` | `bool` | `true` | 과거 설계 옵션. 2026-05-27 이후 런타임에서는 잔류 공격을 항상 활성화한다. |
| `residualAttackMaxLifetime` | `float` | `1.8f` | 무한 루프/이벤트 누락 대비 최대 생존 시간 |
| `residualAttackFadeOutDuration` | `float` | `0.25f` | 종료 시 디졸브 또는 알파 페이드 시간 |
| `residualAttackAllowHitStop` | `bool` | `true` | 잔류 공격 히트 시 히트스톱/카메라 피드백 허용 |
| `residualAttackAllowComboWindowEvents` | `bool` | `false` | 잔류 모델의 콤보 입력 창 이벤트 무시 여부 |
| `residualAttackUseRootMotion` | `bool` | `false` | 잔류 모델 루트모션 이동 허용 여부 |
| `residualAttackMaxCount` | `int` | `1` | 동시에 남을 수 있는 퇴장 모델 수 |
| `residualAttackReturnToSameCharacterRunner` | `bool` | `true` | 후속 설계. 같은 캐릭터 타입 잔류 러너가 있으면 해당 위치로 복귀 |
| `residualAttackReturnPositionMaxAge` | `float` | `1.8` | 후속 설계. 너무 오래된 잔류 위치로 복귀하지 않기 위한 최대 유효 시간 |
| `preserveComboStatePerCharacter` | `bool` | `true` | 후속 설계. 캐릭터 타입별 콤보 인덱스/유효 시간을 저장하고 복원 |
| `comboStateMaxCarryTime` | `float` | 기존 콤보 유효 시간 사용 | 후속 설계. 스왑 후 복귀 시 콤보를 이어갈 수 있는 최대 시간 |

권장: 1차 구현에서는 `residualAttackUseRootMotion = false`로 둔다. 현재 `PlayerAttackState`의 이동은 KCC + MotionWarpController가 계산하므로, 잔류 모델에 동일 이동을 적용하면 벽 통과/타겟 관통 문제가 생길 수 있다.

후속 개선은 위치 오프셋이 아니라 `residualAttackUseRootMotion`을 고도화하는 방향이다. 잔류 모델이 공격 애니메이션의 루트모션을 따라 자연스럽게 앞으로 나가되, KCC 없는 러너 환경에서도 벽 관통/타겟 관통/무한 이동이 생기지 않도록 충돌 보정과 최대 이동 거리 제한을 함께 둔다.

### `SwapResidualAttackRunner`

잔류 모델 하나의 생명주기를 담당하는 런타임 컴포넌트다.

주요 책임:

- 스왑 시점의 모델 프리팹/인스턴스를 받아 잔류용 오브젝트를 만든다.
- 잔류 러너 루트와 모델 인스턴스는 별도 오프셋 없이 스냅샷 위치에 생성한다.
- 후속 위치 정책에서는 잔류 모델 위치 기록, 같은 캐릭터 복귀 위치 제공, 루트모션 보정 실행을 책임으로 둔다.
- `ActorAnimator.MotionPlaybackSnapshot` 또는 공격 스냅샷으로 MotionSet 진행률을 복원한다.
- `MotionEventExecutor`의 타겟을 잔류 컨텍스트로 지정한다.
- `ActorAnimator.OnMotionSetCompleted` 또는 타임아웃 시 종료한다.
- 종료 시 무기 트레일, 콜리전, 활성 이벤트를 정리하고 오브젝트를 제거한다.

권장 API:

```csharp
public sealed class SwapResidualAttackRunner : MonoBehaviour
{
    public void Initialize(SwapResidualAttackRequest request);
    public void Cancel(SwapResidualAttackCancelReason reason);
}
```

### `SwapResidualAttackRequest`

스왑 순간 필요한 데이터를 묶는 값 타입이다.

```csharp
public readonly struct SwapResidualAttackRequest
{
    public readonly PlayerActor OwnerPlayer;
    public readonly CharacterModelData SourceModel;
    public readonly CharacterActorType CharacterType;
    public readonly PlayerAttackDataSO AttackData;
    public readonly AttackData CurrentAttackData;
    public readonly AttackInfoBase CurrentAttackInfoBase;
    public readonly ActorAnimator.MotionPlaybackSnapshot PlaybackSnapshot;
    public readonly LayerMask TargetLayerMask;
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
}
```

`CurrentAttackData`와 `CurrentAttackInfoBase`는 현재 `PlayerCombat` 내부에 private로 묶여 있으므로, 실제 구현 시에는 복사 API가 필요하다.

권장 추가 API:

```csharp
public bool TryCreateResidualAttackSnapshot(out PlayerResidualAttackSnapshot snapshot);
```

### `ResidualPlayerCombat`

`PlayerCombat` 전체를 재사용하지 않고, 잔류 공격에 필요한 최소 기능만 가진 컴포넌트다.

필요 기능:

- `AttackData` 깊은 복사본 보유
- `AttackInfoBase` 기반 `SetHitPhaseIndex(int index)`
- `SetEnableCollision(bool)`
- `PerformHitDetection()`
- `ClearHitTargets()`
- `OnAttackHit` 발화
- `GameObjectManager.ShowFX`, `UIManager.ShowDamageFloater`, 선택적 히트스톱 호출

재사용하지 않을 기능:

- 콤보 상태
- 입력 버퍼
- 차지 공격 상태
- 패리/카운터 창
- 스킬 게이지 소비
- Motion Warp
- 무기 장착 상태 전환

`AttackData.attacker`는 실제 `PlayerActor`를 넣어도 되지만, 피해 보상 귀속을 명확히 하려면 `ownerType`을 별도로 보존하는 것이 좋다. 기존 `IDamageable.TakeDamage(AttackData)` 계약은 유지하되, 추후 킬 보상/게이지 충전에서 outgoing 캐릭터 귀속이 필요하면 `AttackData`에 `CharacterActorType sourceCharacterType`을 추가한다.

### `ResidualMotionEventTarget`

현재 `BeginCollisionEvent`는 `target.GetComponent<GameActor>()`를 요구한다. 잔류 러너는 `GameActor`가 아니므로 이벤트 라우팅 확장이 필요하다.

권장 인터페이스:

```csharp
public interface IMotionEventCombatTarget
{
    void SetTargetLayerMask(LayerMask targetLayerMask);
    void SetHitPhaseIndex(int hitPhaseIndex);
    void SetEnableCollision(bool enabled);
    void ClearHitTargets();
}
```

`BeginCollisionEvent` 처리 순서:

1. `target.GetComponent<IMotionEventCombatTarget>()`가 있으면 우선 처리한다.
2. 없으면 기존 `GameActor` 기반 `PlayerActor` / `MonsterActor` 처리로 폴백한다.

이렇게 하면 기존 MotionEvent 데이터는 수정하지 않고 잔류 러너만 새 타겟으로 받을 수 있다.

---

## 실행 흐름

### 일반 공격 중 스왑

```
PlayerAttackState 실행 중
        │
        ├── PlayerCombat.CurrentAttackData 존재
        ├── ActorAnimator가 공격 MotionSet 재생 중
        │
        ▼
PlayerSwap 입력
        │
        ▼
PartyManager.RequestSwapTo(targetIndex)
        │
        ▼
PlayerSwapBehaviour.SwapTo(targetType)
        │
        ├── TryCreateResidualAttackSnapshot()
        ├── outgoing SwapResidualAttackRunner 생성
        ├── outgoing 모델은 현재 위치에서 잔류 공격 실행
        ├── incoming 모델 활성화
        └── PlayerActor.RefreshForCharacter(incoming)
                │
                ▼
PartyManager.QueueEntryAttack() 또는 QueueSwapAssist()
```

### 같은 캐릭터 잔류 위치로 복귀

```
B 조작 중 A로 스왑 요청
        │
        ├── ActiveRunners에서 ownerType == A 탐색
        │
        ├── A 잔류 러너가 있고 유효 시간 내라면
        │       ├── 후속 정책에 따라 A 잔류 모델 위치/회전 복귀 여부 판단
        │       ├── A 잔류 러너 Cancel(Replaced 또는 Consumed)
        │       └── 원본 A 모델 활성화
        │
        └── A 잔류 러너가 없으면 기존 스왑 위치 유지
```

같은 캐릭터 잔류 위치 복귀는 임의 오프셋과 다르게 “이미 필드에 남아 있던 해당 캐릭터 위치로 돌아간다”는 정책이다. 다만 이 기능도 충돌/카메라/락온 처리 확인 전까지는 후속 구현으로 둔다.

### 잔류 러너 종료

```
잔류 MotionSet 완료
        │
        ├── MotionEventExecutor.Stop()
        ├── ResidualPlayerCombat.SetEnableCollision(false)
        ├── ActorWeaponTrailController.StopAttackTrails(...)
        ├── 선택: Dissolve / FadeOut
        └── Destroy(residualRoot)
```

### 인터럽트 종료

다음 조건에서는 잔류 공격을 즉시 취소한다.

| 조건 | 처리 |
|------|------|
| 씬 전환 | 모든 잔류 러너 제거 |
| 플레이어 사망 | 잔류 러너 제거 |
| 같은 캐릭터로 즉시 재교체 | 기존 잔류 러너 제거 후 실제 모델 활성화 |
| `residualAttackMaxCount` 초과 | 가장 오래된 잔류 러너 제거 |
| 타임아웃 | 충돌 비활성화 후 페이드아웃 |

---

## 구현 단계

### 1단계: 스냅샷 API 추가

`PlayerCombat`에 현재 공격을 잔류 실행용으로 복사하는 API를 추가한다.

필요 변경:

- `_currentAttackData` 깊은 복사 메서드 추가
- `_currentAttackInfoBase` 노출 대신 스냅샷에 복사
- 현재 공격이 잔류 가능한지 판정

잔류 가능 조건:

| 조건 | 허용 |
|------|------|
| 현재 상태가 `Attack` | 허용 |
| `CurrentAttackData` 있음 | 허용 |
| 공격 MotionSet 재생 중 | 허용 |
| `FinishAttack`, `SpecialBreakAttack` | 허용 |
| `Ultimate` | 별도 시퀀스 연동 지점 확인 전까지 미구현 |
| Hit/Death/Grabbed 상태 | 제외 |

### 2단계: MotionEvent 라우팅 확장

`BeginCollisionEvent`가 `IMotionEventCombatTarget`을 먼저 확인하도록 확장한다.

기존 `GameActor` 경로는 그대로 유지해야 한다. 이 변경은 잔류 러너 외에도 향후 설치형 스킬, 분신, 소환수의 MotionSet 이벤트 실행에 재사용할 수 있다.

### 3단계: `ResidualPlayerCombat` 구현

`PlayerCombat.PerformHitDetection()`의 최소 복사본으로 시작한다.

주의:

- `_hitTargets`는 잔류 러너별로 독립한다.
- `AttackData`는 타겟별 `hitPoint`, `hitTarget`, `attackDirection`을 변경하므로 반드시 러너 전용 복사본이어야 한다.
- `OnAttackHit`는 `PartyManager.OnPlayerAttackHit`에 연결할지 별도 정책이 필요하다.

권장 1차 정책:

| 보상 | 1차 처리 |
|------|----------|
| 피해 | 적용 |
| 피격 FX | 적용 |
| 데미지 플로터 | 적용 |
| 히트스톱 | 적용 가능 |
| 카메라 펀치/쉐이크 | 약하게 적용 또는 비활성 |
| 스킬 게이지 충전 | 중복 충전 방지를 위해 비활성 |
| VitalOrb 생성 | 비활성 또는 별도 밸런스 후 활성 |

### 4단계: `SwapResidualAttackRunner` 구현

구현 위치 권장:

```
Assets/02.Scripts/GameActor/Component/Player/SwapResidualAttackRunner.cs
Assets/02.Scripts/GameActor/Component/Player/ResidualPlayerCombat.cs
Assets/02.Scripts/GameActor/Component/Player/PlayerResidualAttackSnapshot.cs
```

러너 생성 방식은 두 가지가 있다.

| 방식 | 장점 | 단점 | 권장 |
|------|------|------|------|
| 모델 복제 | 기존 active 모델을 즉시 incoming으로 전환할 수 있음 | 복제 비용, 무기/트레일/Animator 참조 정리 필요 | 1차 권장 |
| 모델 분리 후 원본 잔류 | 복제 비용 적음 | incoming 모델 활성화 전 원본 계층 관리가 복잡함 | 보류 |

1차 구현은 `Instantiate(sourceModel.gameObject, position, rotation)`로 잔류 모델을 복제하고, 복제본에서 `PlayerActor`, `PlayerMovementController`, 입력 관련 컴포넌트를 제거하거나 비활성화하는 방식을 권장한다. 현재 `CharacterModelData`는 모델 서브루트에 붙어 있으므로 복제 단위가 명확하다.

### 5단계: `PlayerSwapBehaviour.SwapTo()` 확장

현재 흐름:

```csharp
_activeModel?.gameObject.SetActive(false);
_activeModel = target;
_activeModel.gameObject.SetActive(true);
_playerActor.RefreshForCharacter(_activeModel, animationSnapshot);
```

변경 흐름:

```csharp
TrySpawnResidualAttack(_activeModel);

_activeModel?.gameObject.SetActive(false);
_activeModel = target;
_activeModel.gameObject.SetActive(true);
_playerActor.RefreshForCharacter(_activeModel, movementSnapshot);
```

`movementSnapshot`은 지금처럼 이동/로코모션 복원용으로 유지한다. 공격 스냅샷은 잔류 러너로 넘기고 incoming 모델에는 복원하지 않는다. 공격 중 스왑 후 incoming 모델이 같은 공격 진행률을 복원하면 연출이 어색해진다.

후속 위치 정책에서도 incoming 모델을 별도 위치로 밀어내지 않는다. 공격 중 스왑의 이동감은 잔류 러너 루트모션을 자연스럽게 적용하는 방식으로 개선한다.

### 6단계: 데이터 확장

`PartyConfigSO`에 기본 옵션을 추가한다.

추후 캐릭터별로 다른 잔류 정책이 필요하면 `CharacterModelData`에 override 필드를 추가한다.

예:

| 캐릭터 | 정책 |
|--------|------|
| Bokusei | 일반 공격 후반부만 잔류 |
| Honoka | 큰 도끼 공격은 잔류 시간 길게 허용 |
| Reine | 투사체/원거리 스킬은 잔류 대신 투사체만 계속 유지 |
| Inori | 지원형 Outro 버프와 연결 |

---

## 데이터 정책

### 공격 종류별 1차 정책

| 공격 종류 | 잔류 허용 | 이유 |
|----------|----------|------|
| 일반 약공격 | 허용 | 가장 기대되는 퀵스왑 감각 |
| 일반 강공격 | 허용 | 후딜 긴 공격을 스왑으로 이어붙이는 재미 |
| 대시 공격 | 허용 | 공격 MotionSet 스냅샷과 Collision 이벤트를 잔류 러너가 이어받는다. |
| 점프 공격 | 허용 | KCC 낙하 처리는 복제하지 않고 현재 위치의 잔류 모션/히트 실행으로 제한한다. |
| 차지 공격 | 허용 | 차지 릴리즈 후 생성된 `CurrentAttackData`와 `HitPhaseData` 스냅샷을 사용한다. |
| 스킬 공격 | 허용 | 게이지 충전 이벤트는 연결하지 않아 보상 중복을 막는다. |
| 피니시 공격 | 허용 | 처형 타겟을 스냅샷으로 보존하고 `FinishAttackEvent`가 잔류 인터페이스로 발동한다. |
| 브레이크 특수공격 | 허용 | 브레이크 타겟과 피해율/고정 피해를 스냅샷으로 보존하고 전용 이벤트가 잔류 인터페이스로 발동한다. |
| 궁극기 | 미구현 | 현재 별도 `UltimateSequence` 연동 지점이 확인되지 않았다. 추가 시 별도 시퀀스 스냅샷 정책이 필요하다. |

### MotionSet 이벤트 정책

| 이벤트 | 잔류 러너 처리 |
|--------|----------------|
| `BeginCollisionEvent` | 처리 |
| `MotionEvent_ComboWindow` | 잔류 러너에는 `PlayerActor`가 없으므로 자연스럽게 무시 |
| `MotionEvent_MotionWarp` | 1차 무시 |
| `MotionEvent_AddForce` | 1차 무시 |
| VFX/SFX 이벤트 | 처리 |
| `FinishAttackEvent` | 처리 |
| `SpecialBreakAttackEvent` | 처리 |
| Camera 이벤트 | 기존 이벤트별 타겟 해석에 따름. 잔류 전용 보정은 Phase 2에서 정리 |
| TimeScale 이벤트 | 기본 비활성 |
| Projectile Spawn | 처리 가능. 단 owner를 `PlayerActor`로 넘길지 별도 검토 |

---

## 밸런스 정책

잔류 공격을 허용하면 스왑만으로 공격 후딜을 제거하면서 피해는 유지할 수 있다. 따라서 다음 제한이 필요하다.

| 제한 | 권장값 | 목적 |
|------|--------|------|
| 퇴장 캐릭터별 스왑 쿨다운 | 기존 `PartyConfigSO.swapCooldown` 유지 | 무한 퀵스왑 방지 |
| 잔류 러너 수 | 1 | 다중 잔상 누적 방지 |
| 잔류 생존 시간 | 1.2~1.8초 | 긴 스킬 잔류 남용 방지 |
| 잔류 루트모션 | 공격별 최대 이동 거리 제한 | 별도 위치 오프셋 없이 공격 모션 자체가 자연스럽게 전진하도록 처리 |
| 같은 캐릭터 잔류 위치 복귀 | 후속 검토 | 이전에 남겨둔 캐릭터로 돌아가는 감각 강화. 임의 오프셋과는 분리 |
| 캐릭터별 콤보 상태 유지 | 기존 콤보 유효 시간 안에서만 허용 | 빠른 스왑 복귀 시 자기 콤보를 이어가되 무한 고타수 유지 방지 |
| 스킬 게이지 충전 | 1차 비활성 | 자기 증폭 루프 방지 |
| 히트스톱 | 약공격 기준만 허용 | 화면 피드백 과잉 방지 |
| 보스 경직 | 기존 Poise / Break 정책 따름 | 잔류 공격만 예외 처리하지 않음 |

---

## 주의 사항

### 같은 모델을 두 번 활성화하지 않는다

퇴장 모델 원본은 `PlayerSwapBehaviour` 관리 하위에 있어야 한다. 잔류 러너는 원본을 직접 들고 나가지 말고 복제본을 사용한다. 그래야 incoming으로 다시 돌아올 때 원본 모델의 `CharacterModelData`, 장비, 소켓 상태를 안전하게 재사용할 수 있다.

### `PlayerCombat`를 공유하지 않는다

incoming 캐릭터의 공격과 outgoing 잔류 공격이 같은 `PlayerCombat`를 공유하면 `_currentAttackData`와 `_hitTargets`가 덮인다. 잔류 공격은 별도 `ResidualPlayerCombat`를 써야 한다.

### 공격 진행률 복원 범위를 구분한다

`PlayerSwapBehaviour`의 기존 `CaptureMovementPlaybackSnapshot()`은 이동 모션 복원용이다. 공격 중 스왑에서는 incoming 모델에 공격 진행률을 복원하지 말고, 잔류 러너에만 공격 스냅샷을 넘겨야 한다.

### 콤보 진행도는 캐릭터별로 저장한다

단일 `PlayerCombat`를 쓰더라도 콤보 인덱스는 전역으로 공유하지 않는다. 스왑 직전 outgoing 캐릭터의 콤보 상태를 저장하고, incoming 캐릭터의 저장 상태를 복원한다. 저장 상태가 없거나 콤보 유효 시간이 지났으면 0타 시작으로 초기화한다.

잔류 러너는 조작 가능한 캐릭터가 아니므로 콤보 상태를 증가시키지 않는다. 예를 들어 1번 캐릭터가 2타 중 스왑되어 잔류 공격을 마무리하더라도, 그 잔류 히트가 1번 캐릭터의 콤보 인덱스를 3타 이후로 밀어서는 안 된다. 다음 조작 입력이 들어올 때 저장된 2타 상태와 시간 조건을 보고 3타를 선택한다.

### 이벤트 타겟을 명확히 한다

`MotionEventExecutor.TargetObject`는 현재 부모 `GameActor`를 자동 탐색한다. 잔류 러너에서는 이 자동 탐색이 실제 `PlayerActor`로 빠지면 안 된다. 잔류 복제본에는 명시적으로 `_targetObject` 또는 인터페이스 타겟을 지정해야 한다.

### 루트모션 이동은 2차로 미룬다

현재 공격 이동은 `PlayerAttackState.UpdateVelocity()`와 `MotionWarpController`가 담당한다. 잔류 모델은 KCC가 없으므로 같은 이동을 재현하기 어렵다. 1차 구현은 제자리 잔류 + 회전 고정 + 히트박스 실행으로 시작하고, 캐릭터별로 필요한 공격만 별도 이동 이벤트를 추가한다.

### 위치 오프셋은 사용하지 않는다

잔류 모델 또는 incoming 캐릭터를 별도 오프셋으로 밀어내지 않는다. 위치를 인위적으로 분리하면 히트 판정 중심, 무기 궤적, 카메라 타겟, 락온 거리 해석이 공격 데이터와 어긋날 수 있다.

후속 개선은 루트모션을 고도화해 처리한다. 잔류 러너가 KCC를 갖지 않더라도 공격 애니메이션의 루트 이동을 제한적으로 반영하고, 필요하면 충돌 보정/최대 이동 거리/타겟 방향 클램프를 둔다. 목표는 “스폰 후 앞으로 자연스럽게 튀어나가는 공격”이지, 스왑 순간 좌표를 임의로 밀어내는 것이 아니다.

---

## 구현 우선순위

### Phase 1: 최소 기능

1. `PlayerCombat.TryCreateResidualAttackSnapshot()` 추가
2. `IMotionEventCombatTarget` 추가
3. `BeginCollisionEvent` 라우팅 확장
4. `ResidualPlayerCombat` 추가
5. `SwapResidualAttackRunner` 추가
6. `PlayerSwapBehaviour.SwapTo()`에서 공격 중이면 잔류 러너 생성
7. 모든 `CurrentAttackData` 기반 플레이어 공격 상태 허용

완료 기준:

- 공격 MotionSet의 Collision 이벤트가 스왑 후에도 잔류 모델에서 실행된다.
- incoming 캐릭터는 기존처럼 즉시 조작 가능하다.
- 잔류 모델은 MotionSet 완료 또는 타임아웃 후 제거된다.
- 같은 프레임에 incoming `entryAttack`과 outgoing 잔류 히트가 서로의 `AttackData`를 덮지 않는다.

### Phase 2: 전투 피드백 정리

1. 잔류 루트모션 고도화 - 부분 완료. 루트모션 델타 기반 러너 이동, 최대 이동 거리, blocker SphereCast를 추가했다. 캐릭터별 애니메이션에서 `ActorAnimator.DeltaPosition` 품질은 플레이 검증이 필요하다.
2. 루트모션 이동 거리 제한, 충돌 보정, 타겟 방향 클램프 정리 - 부분 완료. 이동 거리 제한과 blocker 보정은 구현, 타겟 방향 클램프는 후속.
3. 캐릭터 타입별 콤보 상태 저장/복원 - 완료.
4. 같은 캐릭터 타입 잔류 위치 복귀 정책 검토 - 완료. 옵션으로 켜고 끌 수 있다.
5. 잔류 공격 히트스톱 강도 별도 설정 - 완료.
6. 카메라 쉐이크 중복 억제 - 완료. 잔류 공격은 별도 카메라 쉐이크를 발생시키지 않고 히트스톱 재발동 간격만 제한한다.
7. 무기 트레일/디졸브 페이드 정리 - 완료.
8. 데미지 플로터에 outgoing 캐릭터 타입 표시 옵션 추가 - 완료.

### Phase 3: 명조식 Intro / Outro 확장

1. `PlayerAttackDataSO.outroAttack` 또는 `SwapOutroAttackDataSO` 추가
2. 파티 스킬 게이지가 가득 찬 캐릭터가 퇴장할 때 전용 Outro 실행
3. incoming `swapSpecialAttack` 재활성화
4. Outro 버프를 GameplayTag 또는 Stat Modifier로 적용
5. HUD에 교체 가능/Outro 준비 표시 추가

---

## 예상 파일 변경

| 파일 | 변경 내용 |
|------|----------|
| `PlayerSwapBehaviour.cs` | 스왑 전 잔류 공격 스폰 훅 추가 |
| `PlayerCombat.cs` | 잔류 공격 스냅샷 생성 API, AttackData 복사 유틸 추가 |
| `MotionEvent_Collision.cs` | `IMotionEventCombatTarget` 우선 라우팅 |
| `MotionEvent_DisableCollision.cs` | `IMotionEventCombatTarget` 우선 라우팅 |
| `MotionEventExecutor.cs` | 명시 타겟 지정용 `SetTargetObject()` 추가 |
| `PartyConfigSO.cs` | 잔류 공격 옵션 추가 |
| `PartyManager.cs` | 잔류 공격 옵션 읽기 전용 프로퍼티 추가 |
| `SwapResidualAttackRunner.cs` | 신규 런타임 러너, 후속 구현에서 루트모션 보정 및 캐릭터 타입별 잔류 위치 조회 API 추가 |
| `ResidualPlayerCombat.cs` | 신규 잔류 히트 판정 컴포넌트 |
| `PlayerResidualAttackSnapshot.cs` | 신규 스냅샷 타입 |
| `IMotionEventCombatTarget.cs` | 신규 MotionEvent 전투 타겟 인터페이스 |
| `PlayerAttackDataSODrawer.cs` | 필요 시 잔류 허용 플래그 표시. 2026-05-27 구현에서는 미변경 |
| `PlayerCombat.cs` | 후속 구현에서 캐릭터 타입별 콤보 상태 저장/복원 API 추가 |

---

## 검증 체크리스트

| 시나리오 | 기대 결과 |
|----------|----------|
| 약공격 히트 직전 스왑 | 퇴장 모델이 남아 히트 후 사라진다. |
| 공격 중 스왑 후 위치 확인 | 잔류 모델과 incoming 캐릭터는 별도 오프셋으로 밀려나지 않는다. |
| 후속 루트모션 정책: 공격 중 스왑 | 잔류 모델이 공격 애니메이션의 루트 이동으로 자연스럽게 전진하되, 최대 이동 거리와 충돌 보정을 따른다. |
| 후속 루트모션 정책: 벽/타겟 관통 위험 | 루트모션 이동이 제한되거나 클램프되어 벽 관통과 과도한 타겟 통과가 발생하지 않는다. |
| 후속 콤보 정책: 1번 2타 후 2번 1타, 다시 1번 | 1번 캐릭터의 콤보 유효 시간이 남아 있으면 3타 콤보가 나간다. |
| 후속 콤보 정책: 복귀 시간이 늦음 | 캐릭터별 콤보 유효 시간이 만료되면 해당 캐릭터는 1타부터 다시 시작한다. |
| 후속 콤보 정책: 잔류 히트 발생 | 잔류 러너의 히트는 조작 캐릭터의 콤보 카운트를 증가시키지 않는다. |
| 약공격 히트 후 스왑 | 남은 모션만 재생하고 추가 히트가 없으면 사라진다. |
| 다단 히트 공격 중 스왑 | 아직 지나지 않은 Collision 이벤트만 실행된다. |
| 대시/점프/차지/스킬 공격 중 스왑 | 퇴장 모델이 남은 MotionSet 이벤트를 실행한다. |
| 피니시/브레이크 특수공격 중 스왑 | 잔류 러너가 보존된 타겟에 전용 MotionEvent를 적용한다. |
| 스왑 직후 incoming 등장 공격 | incoming 공격과 outgoing 잔류 공격이 모두 독립 판정된다. |
| 잔류 중 씬 전환 | 잔류 오브젝트가 즉시 정리된다. |
| 잔류 중 같은 캐릭터로 재교체 | 기존 잔류 오브젝트가 제거되고 원본 모델이 활성화된다. |
| 보스 피격 | Poise/Break 처리가 기존 공격과 동일하게 동작한다. |
| 스킬 게이지 | 1차 정책에서는 잔류 공격으로 게이지가 중복 충전되지 않는다. |

---

## 결론

이 시스템은 `PlayerActor`를 여러 개 만드는 방식이 아니라, 현재 프로젝트의 단일 플레이어 액터 구조 위에 짧게 사라지는 공격 실행체를 얹는 확장이다. 핵심은 `PlayerCombat` 상태를 공유하지 않는 것과, MotionEvent 타겟을 `GameActor` 전용에서 잔류 전투 타겟 인터페이스로 확장하는 것이다.

1차 목표는 “공격 중 스왑해도 이전 모델의 남은 Collision 이벤트가 독립 실행된다”까지로 제한한다. 이후 파티 스킬 게이지, `swapSpecialAttack`, Outro 버프를 연결하면 명조식 교대 협공에 가까운 구조로 확장할 수 있다.
