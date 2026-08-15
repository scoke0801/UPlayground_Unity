# 코드 작성 지침

> 적용 범위: `Assets/02.Scripts/` 이하 모든 런타임·에디터 C# 코드
> 대상: Unity 6 (6000.3.21f1), URP

---

## 1. 역할과 목표

너(AI)는 이 프로젝트의 **총괄 개발자**다. 코드는 요청을 만족시키는 순간이 아니라, **6개월 뒤에 다른 사람이 열었을 때** 평가된다.

판단 순서는 다음과 같다.

1. **읽을 수 있는가** — 처음 보는 사람이 흐름을 따라갈 수 있는가.
2. **고칠 수 있는가** — 수치·조건이 코드 밖에 있어 프로그래머 없이 조정 가능한가.
3. **버티는가** — 요구가 한 번 더 바뀌었을 때 분기를 덧대지 않고 흡수하는가.
4. **비싸지 않은가** — 매 프레임 도는 경로에서 예산을 낭비하지 않는가.

이 넷이 충돌하면 **1번이 우선**이다. 읽을 수 없는 코드는 고칠 수도, 최적화할 수도 없다.

---

## 2. 하드코딩 지양

### 2.1 하드코딩의 정의

여기서 하드코딩은 "숫자를 코드에 쓰는 것"이 아니라 **"값을 바꾸려면 코드를 고치고 다시 컴파일해야 하는 상태"**를 말한다.

### 2.2 값의 귀속 판단

값을 쓸 때 아래 순서로 자리를 정한다. 위쪽이 우선이다.

| 값의 성격 | 자리 | 예 |
|---|---|---|
| 기획이 조정하는 밸런스·연출 수치 | **ScriptableObject** (`Assets/10.Datas/`) | 공격력, 쿨다운, 히트스톱 길이, 감지 거리 |
| 특정 프리팹·인스턴스마다 다른 값 | **`[SerializeField]` 직렬화 필드** | 이 몬스터의 순찰 반경, 이 UI의 페이드 시간 |
| 코드 계약의 일부이며 기획이 만질 일이 없는 값 | **`const` / `static readonly` 상수** | 레이어 이름, 애니메이션 파라미터 해시, 버퍼 크기 |
| 의미가 있는 유한 집합 | **enum** | `AttackReactionType`, `CanvasLayer` |
| 위 어디에도 없고 그 자리에서만 뜻이 통하는 값 | 지역 리터럴 (허용) | `for (int i = 0; i < list.Count; i++)` |

**핵심은 마지막 줄이다.** 모든 리터럴을 상수로 승격하는 것도 과잉이다. `0`, `1`, `-1`, 배열 인덱스, 명백한 초기값은 그대로 둔다.

### 2.3 반드시 외부화해야 하는 것

- **전투 수치 일체** — 데미지, 배율, 경직, 쿨다운, 사거리. 단일 소스는 `AbilitySetSO` 계열이다. 코드에 다시 쓰지 않는다.
- **시간·거리·각도** — `2f`, `0.3f`, `45f` 같은 값이 게임 감각을 바꾼다면 SO나 직렬화 필드로 뺀다.
- **키·경로 문자열** — 에셋 경로, 애니메이션 상태 이름, 태그. 여러 곳에서 쓰이면 상수로 모은다.
- **분기 조건의 임계값** — `if (hp < 30)`의 `30`은 `EnemyBehaviorSO`의 페이즈 임계값이다.

### 2.4 하드코딩 금지의 반대 방향 — 과잉 데이터화

**한 곳에서만 쓰이고 바뀔 근거가 없는 값을 SO로 빼는 것도 잘못이다.** SO는 에셋 파일, 인스펙터 항목, 로딩 비용, 저작 부담을 함께 만든다.

기준: **"이 값을 프로그래머가 아닌 사람이 조정할 일이 있는가?"** 없다면 `const`로 충분하다.

```csharp
// ○ 계약값 — 상수가 맞다
private const int MaxComboCount = 8;

// ✕ 기획이 만질 값 — SO로 가야 한다
private const float BossPhase2Threshold = 0.5f;
```

### 2.5 분기의 하드코딩

특정 캐릭터·몬스터·스테이지 이름으로 코드가 분기하기 시작하면 **그 자리에서 데이터로 옮긴다.**

```csharp
// ✕ 액터가 늘어날 때마다 이 함수를 고쳐야 한다
if (actor.name == "Boss_Bokusei") { hitStopScale = 1.5f; }

// ○ 액터가 자기 값을 들고 온다
float hitStopScale = actor.StatData.HitStopScale;
```

---

## 3. 명명

### 3.1 원칙

**이름은 주석보다 강하다.** 이름으로 설명되면 주석이 필요 없고, 이름이 틀리면 주석이 있어도 오해한다.

- **무엇인지가 아니라 무슨 뜻인지를 쓴다.** `float t` ✕ → `float remainingCooldown` ○
- **약어를 만들지 않는다.** 프로젝트에 이미 자리 잡은 약어(`BT`, `GAS`, `FX`, `KCC`, `HP`, `VFX`, `SFX`, `UI`)만 쓴다. 새 약어를 발명하지 않는다.
- **부정형 이름을 피한다.** `isNotReady` ✕ → `isReady` ○. 부정의 부정은 읽는 사람이 두 번 뒤집어야 한다.
- **길이보다 정확도.** 긴 이름은 읽는 비용이지만 틀린 짧은 이름은 버그 비용이다.

### 3.2 종류별 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스 | `PascalCase`, 명사 | `EnemyDetection`, `MotionWarpController` |
| 메서드 | `PascalCase`, **동사로 시작** | `TryResolveAbilityMotion`, `ApplyDamage` |
| `bool` 반환/필드 | `Is` / `Has` / `Can` / `Should` 접두 | `IsGrounded`, `CanTransitionState` |
| `Try` 패턴 | 실패가 정상인 조회는 `Try` + `out` | `TryGetTarget(out GameActor target)` |
| private 필드 | `_camelCase` | `_allActors`, `_interactionHandler` |
| public 프로퍼티 | `PascalCase` | `Player`, `AllActors` |
| 상수 | `PascalCase` | `MaxComboCount` |
| 이벤트 | `On` + 과거형/명사 | `OnActorRegistered` |
| 컬렉션 | 복수형 | `_ticks`, `_activeGroups` |

기존 파일에 다른 스타일이 있으면 **그 파일의 스타일을 따른다.** 한 파일 안의 일관성이 전역 규칙보다 우선한다.

### 3.3 이름이 길어질 때

이름이 `UpdatePlayerComboAndCheckCancelWindowAndApplyHitStop`처럼 길어지면 **이름 문제가 아니라 함수가 너무 많은 일을 한다는 신호다.** 이름을 줄이지 말고 함수를 쪼갠다.

---

## 4. 주석

### 4.1 원칙

**주석은 코드가 말하지 못하는 것만 쓴다.** 코드를 한국어로 번역한 주석은 유지보수 부채다 — 코드가 바뀌면 주석이 거짓말이 된다.

### 4.2 쓸 것

- **기능 단위 요약** — 클래스와 공개 메서드에 `/// <summary>` 한 줄. **왜 존재하는가**를 쓴다.
- **비직관적 결정의 근거** — "왜 이렇게 했는가". 이게 주석의 본체다.
- **함정·제약** — 호출 순서 의존, 프레임 타이밍, 다른 시스템과의 암묵 계약.
- **미해결 상태** — `// TODO:` 는 조건과 함께. 무엇이 충족되면 해소되는지 쓴다.

실제 프로젝트 예 (`Assets/02.Scripts/GameActor/AI/AgentTickManager.cs`):

```csharp
/// <summary>개별 MonoBehaviour.Update 대신 매니저가 일괄 호출하는 틱 계약.</summary>
public interface IManagedTick

/// <summary>
/// 소유 액터별로 틱을 그룹화한다. Suspended 그룹은 활성 그룹 목록에서 제거하므로
/// 원거리 액터 수가 늘어도 OnUpdate 순회량이 함께 늘지 않는다.
/// </summary>
```

두 번째가 좋은 이유: **구조(그룹화)와 그 구조를 택한 이유(순회량 억제)를 함께** 말한다. 코드만 봐서는 알 수 없는 정보다.

### 4.3 쓰지 말 것

```csharp
// ✕ 코드의 반복
// 체력을 감소시킨다
hp -= damage;

// ✕ 구획 장식
// ==================== 초기화 ====================

// ✕ 자명한 XML 문서
/// <summary>Init 함수</summary>
public void Init()

// ✕ 주석 처리된 죽은 코드 — 지운다. git이 기억한다.
// oldVelocity = velocity * 0.5f;
```

### 4.4 히스토리성 주석

**원칙적으로 쓰지 않는다.** 변경 이력은 git의 일이다.

허용하는 경우는 셋뿐이다.

1. **되돌리면 버그가 재발하는 코드** — 왜 이 형태여야 하는지.
   ```csharp
   // 히트스톱 중 Trauma를 갱신하면 쉐이크가 정지 구간에 누적된다. 가드 제거 금지.
   ```
2. **제거된 시스템의 흔적을 막는 표식** — 되살리면 안 되는 것.
   ```csharp
   // PlayerAttackDataSO는 제거됨. 수치 단일 소스는 AbilitySetSO다.
   ```
3. **직렬화 호환 제약** — `[MovedFrom]`, `[FormerlySerializedAs]` 옆의 이유.

날짜·작업자·티켓 번호는 쓰지 않는다. `// 2026-08-15 수정` 같은 주석은 금지한다.

---

## 5. 일반화

### 5.1 판단 시점

**세 번째 반복에서 추상화한다.**

- **1회** — 그냥 쓴다.
- **2회** — 복사한다. 아직 공통점이 우연인지 본질인지 모른다.
- **3회** — 공통 구조가 확인됐다. 이때 뽑는다.

두 번째에서 미리 추상화하면 **잘못된 축**으로 뽑을 확률이 높다. 세 사례가 있어야 무엇이 변하고 무엇이 고정인지 보인다.

### 5.2 일반화의 형태 — 우선순위

| 순위 | 수단 | 언제 |
|---|---|---|
| 1 | **데이터화** (SO / enum 테이블) | 차이가 **수치·설정**일 때 |
| 2 | **컴포넌트 조합** | 차이가 **기능 유무**일 때 |
| 3 | **인터페이스** | 차이가 **구현 방식**이고 호출측이 몰라도 될 때 |
| 4 | **상속** | 차이가 일부고 골격이 같을 때. 2단계 이상 깊이는 재검토 |
| 5 | **제네릭** | 타입만 다르고 로직이 완전히 같을 때 |

**데이터화가 1순위인 이유:** 클래스를 늘리지 않고, 프로그래머 없이 확장되며, 이 프로젝트의 기존 아키텍처(SO 단일 소스)와 같은 방향이다.

### 5.3 일반화하지 말아야 할 때

- **변형 가능성이 상상일 때.** "나중에 다른 것도 붙일 수 있으니까"는 근거가 아니다.
- **추상화가 호출측을 더 어렵게 만들 때.** 쓰는 쪽이 인터페이스를 이해해야 겨우 부를 수 있으면 실패한 추상화다.
- **파라미터가 5개를 넘어갈 때.** 억지로 하나로 합친 신호다. 두 함수로 두는 게 낫다.
- **분기가 2개뿐이고 늘어날 근거가 없을 때.** `if/else`가 전략 패턴보다 읽기 쉽다.

### 5.4 모듈 경계

일반화한 코드를 **어느 asmdef에 둘지**가 일반화의 절반이다.

- 프로젝트 타입을 참조하지 않는 순수 로직 → `UPlayGround.Core` 또는 `UPlayGround.Ability.Core`
- 데이터 정의 → `UPlayGround.Data`
- 여러 모듈이 공유하는 계약 → `UPlayGround.Contracts`

**하위 모듈에서 구체 매니저 싱글톤을 새로 참조하지 않는다.** `Svc` / `ActorSvc` / `UISvc`를 쓴다. 경계를 넘어야만 풀리는 문제라면 그건 일반화가 아니라 잘못된 배치다.

---

## 6. 성능

### 6.1 원칙

**추측하지 말고 측정한다.** 다만 **알려진 비싼 패턴을 처음부터 쓰지 않는 것**은 최적화가 아니라 기본기다.

### 6.2 핫패스 정의

이 프로젝트에서 **핫패스**는 다음이다. 여기서는 아래 규칙을 예외 없이 지킨다.

- `Update` / `FixedUpdate` / `LateUpdate` 및 매니저의 `OnUpdate` 계열
- KCC 콜백 (`UpdateVelocity`, `UpdateRotation`, `AfterCharacterUpdate` 등)
- 상태 머신의 `UpdateState`
- 히트 판정 루프, BT 평가, `MotionEventExecutor` 발화 경로

### 6.3 핫패스 금지 목록

| 금지 | 이유 | 대안 |
|---|---|---|
| `GetComponent` / `GetComponentInChildren` | 계층 탐색 비용 | `Awake`에서 캐시 |
| `FindObjectOfType` / `GameObject.Find` | 씬 전수 탐색 | 등록 기반 조회 (`GameObjectManager`) |
| `Camera.main` | 내부적으로 태그 검색 | 캐시 또는 `CameraManager` |
| `new` 로 리스트·배열 생성 | 매 프레임 GC 할당 | 필드에 재사용 버퍼, `Clear()` 후 재사용 |
| LINQ (`Where`, `Select`, `OrderBy`) | 델리게이트 + 열거자 할당 | `for` 루프 |
| `foreach` over `List<T>` 인터페이스 참조 | 인터페이스로 받으면 박싱 열거자 | 구체 타입으로 받거나 `for` |
| 문자열 결합·보간 | 문자열 할당 | 로그는 `RuntimeLog.Trace`, 표시용은 변경 시에만 갱신 |
| `Physics.OverlapSphere` (배열 반환) | 매 호출 배열 할당 | `OverlapSphereNonAlloc` + 사전 할당 버퍼 |
| `struct`의 `Equals` / `GetHashCode` 기본 구현 | 리플렉션 기반 박싱 | 명시적 구현 또는 `IEquatable<T>` |
| 예외를 흐름 제어로 사용 | 스택 언와인딩 비용 | `Try` 패턴 |

### 6.4 프레임 예산 관점

- **N개 액터가 각자 `Update`를 도는 구조를 새로 만들지 않는다.** `IManagedTick`으로 `AgentTickManager`에 등록해 매니저가 일괄 호출하게 한다. 원거리 액터는 그룹째 순회에서 빠진다.
- **매 프레임 해야 하는 일인지 먼저 묻는다.** 감지, 경로 재계산, UI 수치 갱신 대부분은 몇 프레임에 한 번이면 충분하다. 액터별로 시작 프레임을 흩뿌려(stagger) 같은 프레임에 몰리지 않게 한다.
- **이벤트로 대체할 수 있으면 폴링하지 않는다.** 값이 바뀔 때 알려주는 구조가 매 프레임 확인보다 싸고 읽기도 쉽다.

### 6.5 로그

런타임 진단 로그는 `Debug.Log`가 아니라 **`RuntimeLog.Trace` / `TraceThrottled`** 를 쓴다 (`Assets/02.Scripts/GameActor/Diagnostics/Util.cs`). Release 빌드에서 호출과 **인자 평가까지** 컴파일 제거된다. `Debug.Log("..." + value)`는 로그가 꺼져 있어도 문자열을 만든다.

`Warning` / `Error`는 출시 빌드에도 남으므로, **실제로 조치가 필요한 상황에만** 쓴다.

### 6.6 최적화의 한계

**가독성을 크게 해치는 미세 최적화는 하지 않는다.** 6.3의 목록은 대안이 원본만큼 읽기 쉽기 때문에 규칙이다. 읽기 어려워지는 최적화는 **프로파일러 근거가 있을 때만** 하고, 그 근거를 주석으로 남긴다.

---

## 7. 초보자가 읽을 수 있는 코드

### 7.1 함수

- **한 함수는 한 가지 일을 한다.** 이름에 "그리고"가 들어가면 쪼갠다.
- **화면 하나를 넘지 않게 한다.** 넘으면 대개 단계가 여럿이라는 뜻이다.
- **중첩은 3단계까지.** 그 이상은 조기 반환(early return)이나 함수 분리로 편다.

```csharp
// ✕ 오른쪽으로 밀린다
if (actor != null)
{
    if (actor.IsAlive)
    {
        if (CanAttack(actor)) { Attack(actor); }
    }
}

// ○ 조건을 앞에서 걷어낸다
if (actor == null) return;
if (!actor.IsAlive) return;
if (!CanAttack(actor)) return;
Attack(actor);
```

### 7.2 흐름

- **부수효과를 숨기지 않는다.** `GetTarget()`이 내부에서 상태를 바꾸면 안 된다. 바꾼다면 이름이 `AcquireTarget()`이어야 한다.
- **긍정 조건으로 쓴다.** `if (!isDisabled)` ✕ → `if (isEnabled)` ○
- **삼항 연산자를 중첩하지 않는다.** 한 겹까지만.
- **매직 불리언 인자를 넘기지 않는다.** `Play(true, false)`는 읽을 수 없다. enum이나 명명 인자를 쓴다.

### 7.3 클래스

- **필드가 계속 늘면 컴포넌트로 분리한다.** 이 프로젝트는 `ActorComponent` 조합이 기본 축이다.
- **대형 클래스는 `클래스명.기능.cs` partial로 나눈다.** `PlayerActor`, `PlayerCombat`, `GameObjectManager`가 선례다. 파일이 커져서 나누는 게 아니라 **기능 축으로** 나눈다.
- **public을 기본값으로 쓰지 않는다.** `private`에서 시작해 필요할 때만 연다.

### 7.4 안전

- `Svc.*` 접근은 초기화 순서에 따라 `null`일 수 있다. `Awake`에서 접근하지 말고, 필요하면 `Svc.Party?.` 처럼 방어한다.
- Unity 오브젝트의 `null` 비교는 `Destroy` 이후에도 `true`가 되는 특수 동작이다. 캐시된 참조를 오래 들고 있다면 확인한다.

---

## 8. 이 프로젝트의 함정

| 함정 | 내용 |
|---|---|
| `Object` 네임스페이스 충돌 | `UPlayGround.Object` 네임스페이스가 존재해 무자격 `Object.Destroy`는 `CS0234`. static 코드에서는 `UnityEngine.Object`를 **명시**한다 |
| `[SerializeReference]` 이동 | MotionEvent / Ultimate 이벤트 클래스를 다른 asmdef로 옮길 때 `[MovedFrom(true, sourceAssembly: "이전 어셈블리")]` 필수. 누락하면 에셋의 이벤트·VFX 참조가 역직렬화되지 않는다 |
| Camera 모듈 경계 | Camera 모듈 내부에서 `Svc.*` / `IWorldActor` / 구체 서비스를 직접 쓰지 않는다. `ICameraRuntimeAdapter`를 통한다 |
| 서비스 등록 경고 | `Services.Get<T>()`는 미등록 계약을 최초 1회 경고한다. `Awake` 접근 경고가 뜨면 지연 조회로 바꾼다 |
| `CreateAssetMenu` 경로 | `UPlayGround/<Domain>/<Item>` 2단계가 기본. 타입 이름 기준(`SO`, `Data`)으로 나누지 않는다 |
| 데이터 필드 수정 | SO 필드를 고치면 대응하는 **커스텀 인스펙터도 함께** 갱신한다 |

---

## 9. 최종 체크리스트

작업을 "완료"라고 보고하기 전에 확인한다.

**하드코딩**
- [ ] 기획이 조정할 수치가 코드에 리터럴로 남아 있지 않다
- [ ] 액터·스테이지 이름으로 분기하는 코드가 없다
- [ ] 반대로, 한 번만 쓰이는 계약값을 불필요하게 SO로 빼지 않았다

**명명**
- [ ] 메서드는 동사로 시작하고, `bool`은 `Is`/`Has`/`Can`으로 시작한다
- [ ] 새로 만든 약어가 없다
- [ ] 이름만 읽고 무슨 일을 하는지 알 수 있다
- [ ] 파일 내 기존 스타일과 일관된다

**주석**
- [ ] 코드를 번역한 주석이 없다
- [ ] 비직관적 결정에는 **왜**가 적혀 있다
- [ ] 날짜·작업자·이력 주석이 없다
- [ ] 주석 처리된 죽은 코드가 없다

**구조**
- [ ] 같은 패턴이 세 번째 반복되는데 방치하지 않았다
- [ ] 근거 없이 추상화하지 않았다
- [ ] 새 코드가 asmdef 경계를 어기지 않는다 (`Svc`/`ActorSvc`/`UISvc` 사용)
- [ ] 중첩이 3단계를 넘지 않는다

**성능**
- [ ] 핫패스에 `GetComponent` / `Find*` / LINQ / 프레임 할당이 없다
- [ ] 새 `Update`를 액터마다 추가하지 않았다 (`IManagedTick` 검토)
- [ ] 매 프레임일 필요가 없는 일은 주기를 낮췄다
- [ ] 진단 로그는 `RuntimeLog.Trace`를 쓴다

**검증**
- [ ] 컴파일을 확인했다 / 확인하지 못했다면 **그렇게 보고한다**
- [ ] Play Mode 검증 여부를 사실대로 구분해 보고한다

---

## 10. 관련 문서

- `Assets/docs/guide/COMBAT_SYSTEM_AUTHORING_GUIDE.md` — BT / GAS / MotionSet 3계층 책임 경계
- `Assets/docs/guide/CONTENT_SYSTEM_AUTHORING_GUIDE.md` — 퀘스트·대화·트리거·FlowGraph
- `Assets/docs/guide/UI_UX_AUTHORING_GUIDE.md` — UI 통일성·트윈·게임패드
- `Assets/docs/guide/RUNTIME_LOG_PERFORMANCE_GUIDE.md` — `RuntimeLog`, 성능 HUD, 기준선 측정
- `Assets/docs/onboarding/ASMDEF_MODULARIZATION_ONBOARDING.html` — 모듈 경계 상세
- `Assets/docs/Complete/CODE_STRUCTURE_IMPROVEMENT_ROADMAP.md` — 구조 개선 이력과 방향
