# Boss Assist & Recruitment 구현 스펙

## 1. 목표

외곽 보스 처치 결과로 보스 어시스트 영입을 판정하고, 장착한 보스가 별도 입력으로 지정 스킬 1회만 실행한 뒤 퇴장하게 한다.

핵심 제약:

- 기존 `PartyManager.Roster/BattleOrder`와 분리
- 기존 1~4번 캐릭터 스왑 유지
- 출전 어시스트 1마리
- 필드 잔류 없음
- 이동·추적 없음
- 적의 피격·충돌·어그로 대상 아님
- 자율 AI 없음
- 공격, 브레이크, 회복, 버프, 디버프, 군중 제어 중 캐릭터별 지정 스킬 1회
- 스킬 게이지를 쓰지 않고 쿨다운만 사용

---

## 2. 기존 코드 접점

| 기존 타입 | 사용 또는 분리 원칙 |
|---|---|
| `MonsterActor._recruitableAs` | 플레이어블 캐릭터 확정 해금 경로로 유지 |
| `PartyManager.UnlockCharacter` | 보스 어시스트 로스터에 사용하지 않음 |
| `PartyRosterService<CharacterActorType>` | 보스 어시스트에 재사용하지 않음 |
| `PlayerSwapBehaviour` | 어시스트 실행에 사용하지 않음 |
| `MotionSetAsset`, `MotionEventExecutor` | 지정 스킬 1회 재생과 이벤트 실행에 활용 |
| `ActorSpawnManager` | 어시스트 모델 생성 후보. 월드 보스 등록과 수명 분리 필요 |
| `InputManager.RegisterInputEvent` | 별도 `PlayerAction.BossAssist` 입력 등록 |
| `UISkillSlot.SetCooldownSource` | 어시스트 HUD 쿨다운 표시 패턴 재사용 |
| `RestPointActor` | 출전 어시스트 교체 진입점 확장 후보 |

보스 처치 흐름은 현재 `MonsterActor` 내부의 캐릭터 해금, 경험치, 골드, 드랍 처리와 결합되어 있다. 어시스트 영입은 해당 메서드에 확률 코드를 직접 추가하지 않고 처치 결과 이벤트 또는 전용 서비스 호출로 분리한다.

---

## 3. 식별자와 데이터

보스 어시스트는 `CharacterActorType`을 쓰지 않는다. Actor ID 문자열 또는 별도 안정 ID를 사용한다.

### `BossAssistDefinitionSO`

```csharp
public enum BossAssistRole
{
    Damage,
    Break,
    Defense,
    Heal,
    Buff,
    Debuff,
    CrowdControl,
}

public sealed class BossAssistDefinitionSO : ScriptableObject
{
    public string assistId;
    public string sourceBossActorId;
    public BossAssistRole role;
    public Sprite icon;
    public MotionSetAsset motionSet;
    public float cooldownSeconds;
    public float maxExecutionSeconds;
    public AssistPlacementPolicy placementPolicy;
    public AssistEffectDefinition effect;
}
```

| 데이터 | 규칙 |
|---|---|
| `assistId` | 저장용 유일 키. 에셋 이름에 의존하지 않음 |
| `sourceBossActorId` | 영입 판정과 원본 모델 연결 |
| `motionSet` | 정확히 한 번 실행할 스킬 |
| `placementPolicy` | `NearPlayer`, `NearTarget`, `PlayerForwardFixed` 등 |
| `maxExecutionSeconds` | 종료 이벤트 누락 시 강제 정리 시간 |
| `effect` | 공격 또는 비공격 효과를 공통 실행 계약으로 표현 |

공격 데이터가 없는 회복·버프 어시스트를 지원해야 하므로 `AbilitySetSO`만 필수로 삼지 않는다.

---

## 4. 로스터 서비스

### `AssistRosterService`

Unity 오브젝트와 무관한 순수 규칙 클래스로 구현한다.

```csharp
public sealed class AssistRosterService
{
    public IReadOnlyList<string> Roster { get; }
    public string EquippedAssistId { get; }

    public AssistRecruitResult TryRecruit(string assistId, int maxRosterSize);
    public bool Equip(string assistId);
    public bool Release(string assistId);
}
```

P0 규칙:

- 로스터 최대 4
- 장착 1
- 중복 영입은 로스터를 늘리지 않고 각인 파편 보상 요청으로 전환
- 5마리째 신규 영입은 즉시 방출 UI를 띄우지 않고 정산 보류 상태로 저장
- 사망 또는 사이클 종료로 로스터를 잃지 않음

`BossAssistManager`가 로스터 리스트를 직접 변경하지 않는다.

---

## 5. 영입 판정

### 확률

```text
최종 확률 = 40% + 브레이크 특수공격 마무리 35% + 노히트 15% + 실패당 15%
최종 확률은 100%로 제한
```

### `BossRecruitmentService`

입력 컨텍스트:

```csharp
public readonly struct BossDefeatContext
{
    public readonly string bossActorId;
    public readonly string cycleSpawnId;
    public readonly bool finishedBySpecialBreakAttack;
    public readonly bool noHit;
}
```

출력은 성공 여부뿐 아니라 UI와 저장에 필요한 세부 결과를 포함한다.

```csharp
public readonly struct BossRecruitmentResult
{
    public readonly bool rolled;
    public readonly bool success;
    public readonly float finalChance;
    public readonly int pityBefore;
    public readonly int pityAfter;
    public readonly AssistRecruitResult rosterResult;
}
```

규칙:

- 중앙 보스 영입 여부는 P0 데이터에서 보스별로 설정한다. 기본은 외곽 보스만 가능하다.
- 브레이크 보너스는 마지막 유효 피해 원인이 특수공격일 때만 적용한다.
- 노히트는 조우 시작부터 처치까지 실제 HP 피해가 0일 때만 참이다. 가드로 0 피해면 노히트로 인정한다.
- 천장은 보스별 영구 데이터다. 성공하면 0으로 초기화한다.
- 플레이어블 `_recruitableAs` 해금은 확정이며 이 확률식을 사용하지 않는다.
- 결정적 테스트를 위해 확률 RNG를 주입 가능하게 한다.

---

## 6. 입력과 실행 흐름

### 입력

- `InputDefine.PlayerAction`에 `BossAssist` 상수 추가
- Input Actions 에셋의 `PlayerAction` 맵에 액션·키보드·게임패드 바인딩 추가
- 기존 1~4 스왑 입력을 변경하지 않음
- P0 기본 키는 최종 입력표 확정 전 데이터 바인딩으로 관리하며 코드에 `T`를 직접 쓰지 않음
- 공격 입력 버퍼에는 넣지 않는다. 어시스트는 독립 실행 요청이며 같은 프레임 중복만 차단한다.

### `BossAssistManager`

```text
RequestAssist()
  -> 장착 어시스트 확인
  -> 사이클/플레이어 상태와 쿨다운 확인
  -> 스킬별 대상 요구조건 확인
  -> 안전한 고정 등장 위치 계산
  -> 전역 실행 잠금
  -> 모델 표시
  -> MotionSet 지정 스킬 1회 실행
  -> 효과 이벤트 실행
  -> 종료 또는 timeout
  -> 모델·판정·이벤트 구독 정리
  -> 쿨다운 시작
  -> 전역 실행 잠금 해제
```

쿨다운은 **유효한 실행이 시작된 뒤** 차감한다. 대상이나 위치가 없어 요청이 거부되면 쿨다운을 시작하지 않는다.

---

## 7. 배치와 효과 규칙

### 고정 위치

- `NearPlayer`: 플레이어 기준 지정 로컬 오프셋
- `NearTarget`: 대상 기준 지정 오프셋. NavMesh 이동 없이 한 번만 위치 계산
- `PlayerForwardFixed`: 플레이어 전방 고정 거리
- 지면 Raycast와 충돌 Overlap으로 배치 가능 여부 검사
- 실행 중 루트모션과 이동 컴포넌트를 비활성화

### 대상

- 공격·브레이크·디버프·CC는 락온 대상 우선, 없으면 카메라 전방 최근접 적
- 회복·방어·버프는 플레이어 또는 출전 파티를 데이터로 지정
- 유효 대상이 필요한 스킬은 대상이 없으면 발동 거부

### 비어그로·비충돌

- 어시스트 모델은 `MonsterActor`의 일반 AI 생명주기를 시작하지 않는다.
- 적 감지 목록과 `GameObjectManager` 적 레지스트리에 등록하지 않는다.
- Hurtbox와 물리 충돌은 비활성화한다.
- 공격형 스킬의 Hitbox만 이벤트 구간에 활성화하고 플레이어 진영 공격으로 처리한다.
- 적 공격은 어시스트를 타겟하거나 피해를 주지 못한다.

### 플레이어 상태

- 호출 중 플레이어 행동을 유지한다.
- 호출 자체는 무적·회피·캔슬 판정을 주지 않는다.
- Pause, Death, Grabbed, 컷신, 궁극기 잠금 중 호출을 거부한다.

---

## 8. 쿨다운과 부활

- 쿨다운은 `BossAssistDefinitionSO`가 소유한다.
- 공격·강제 경직은 45~60초, 방어·회복·버프는 30~45초에서 시작한다.
- `Time.time` 절대값만 저장하지 않는다. 저장 시 남은 초를 기록한다.
- 파티 전멸·부활 후 남은 쿨다운을 유지한다.
- 휴식 포인트 사용으로 쿨다운을 초기화하지 않는다.
- 사이클 정산 후 다음 사이클 시작 정책은 P0에서 전부 사용 가능 상태로 초기화한다.

---

## 9. 강제 정리

다음 모든 경로에서 동일한 `CleanupExecution`을 호출한다.

- 정상 MotionSet 종료
- `maxExecutionSeconds` 초과
- 씬 변경
- 사이클 완료·포기
- 플레이어 전멸
- 어시스트 모델 파괴

정리 항목:

- Hitbox·효과 구독 비활성화
- 모델 반환 또는 파괴
- 카메라·타임스케일·히트스톱 요청 해제
- 실행 잠금 해제
- 쿨다운 시작 여부 확정

---

## 10. 완료 조건

1. 기존 1~4번 스왑과 어시스트 입력이 동시에 동작한다.
2. 장착하지 않았거나 쿨다운 중이면 요청이 거부된다.
3. 공격·회복·버프형 스킬을 같은 실행기로 각각 1회 실행할 수 있다.
4. 어시스트는 실행 중 이동하지 않고 적 타겟·피격·충돌 대상이 되지 않는다.
5. 정상 종료와 timeout 모두 모델과 판정을 남기지 않는다.
6. 부활 후 쿨다운이 초기화되지 않는다.
7. 영입 확률, 브레이크 보너스, 노히트, 천장이 계산대로 동작한다.
8. 어시스트 로스터가 플레이어 `Roster/BattleOrder`를 변경하지 않는다.
