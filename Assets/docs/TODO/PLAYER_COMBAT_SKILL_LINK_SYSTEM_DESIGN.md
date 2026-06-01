# 플레이어 전투 스킬/연계 시스템 고도화 설계 문서

> 상태: **Phase 1 구현됨 (Unity 컴파일·플레이 검증 대기)**.
> 작성일: 2026-05-31 / 구현: 2026-05-31
> 레퍼런스: **명조(Wuthering Waves)** 전투 구조 (Basic / Heavy / Resonance Skill / Liberation / Forte / Intro·Outro)

---

## 0. 구현 상태 (2026-05-31, advisor 2차 리뷰 반영)

**Phase 1 작성 완료 — ⚠ 이 환경에서 컴파일/플레이 테스트 불가(Unity, CLI 빌드 없음). 콘솔 클린 여부는 사용자 확인 필요.**

| 영역 | 상태 | 파일 |
|------|------|------|
| 데이터 모델 | ✅ 작성 | `Data/Combat/ComboRouteData.cs` (ComboInputToken/ComboRouteEntry/ComboMatchMode/RouteGroundCondition) |
| 매칭 Resolver(순수 static) | ✅ 작성 | `Data/Combat/ComboRouteResolver.cs` ※설계 §10의 Component/Player 대신 Data/Combat에 배치(런타임·에디터 공유 용이). `IsExecutable` 방어 필터 포함 |
| 입력 트래커 | ✅ 작성 | `Input/ComboInputTracker.cs` (PlayerActor 소유, **간격 기반 만료**) |
| 데이터 필드 | ✅ 작성 | `PlayerAttackDataSO.comboRoutes` |
| 실행/결정 | ✅ 작성 | `PlayerCombat.ExecuteComboRoute`/`CanAffordRoute`/`ComboRoutes`, `PlayerAttackState`(GetAnimKey·PeekNextAnimKey·ChangeToNextState 단일 판정점) |
| 토큰 Push | ✅ 작성 | Dash→Dodge, Airborne(jump)→Jump, Attack→Light/Heavy/Skill1/Skill2, **ChargeAttack→Charge**, Hit/교체 시 Clear |
| 에디터 | ✅ 작성 | `PlayerAttackDataSODrawer` **"연계" 탭**(저작 PropertyField + 진단 + 입력 시뮬레이터). ※독립 패널 대신 기존 통합 드로어에 탭 추가 |

**플래그십 예시 동작 여부:**
- ✅ `약약약→강 (L L L H)` — 완전 배선. 콤보 윈도우 연계(ChangeToNextState)와 강공 인터럽트(forced) 양 경로 모두 단일 판정점에서 가로챔. advisor가 양 타이밍 경로 추적해 동작 확인.
- ✅ `대시→점프→스킬1 (D J S1)` — **배선 완료(미검증)**. `PlayerJumpAttackState`를 라우트 호스트로 확장(공중 물리/착지 기존 로직 재사용), `PlayerAirborneState`가 "스킬 입력 + 매칭 라우트"일 때만 JumpAttackState로 게이트 진입. 오케스트레이션은 `ComboRouteRunner`로 추출해 지상(PlayerAttackState)·공중(JumpAttackState)이 **동일 코드 공유**(peek/execute 드리프트 방지). ⚠ 다이브/강하 같은 커스텀 하강 물리는 **모션 의존**(별도 모션·velocity 작업) — 자기완결 루트모션 라우트면 호스트만으로 충분.

**Phase 1.1 추가 구현 (2026-05-31):**
- ✅ **오케스트레이션 추출** — `ComboRouteRunner`(State 네임스페이스): `ResolveRoute`(peek/execute), `TryExecuteRoute`(매칭 시 Clear+Execute), `HasMatchingRoute`(게이트용). PlayerAttackState의 3개 메서드를 이전, 양 호스트가 공유.
- ✅ **공중 라우트 호스트** — `PlayerJumpAttackState` 생성자에 `forcedAttackAction` 추가, OnEnter에서 라우트 우선 시도 후 폴백.
- ✅ **공중 스킬 게이트** — `PlayerAirborneState`가 `HasMatchingRoute(Skill)` 성립 시에만 JumpAttackState로 전환(일반 공중 스킬 동작은 변경 없음).
- ✅ **LinkWindow 데이터화** — `PlayerAttackDataSO.comboLinkWindow`(기본 1.0s), `RefreshAttackData`에서 트래커에 반영(캐릭터별).
- ✅ **HasSkillInput 검증** — edge-triggered(`OnInputPerformedSkill_N` press + 매 프레임 None 리셋) 확인 → held 스킬이 약공콤보 토큰 오염 우려 없음, 코드 변경 불필요.

**코드리뷰 반영 (advisor 2차):**
- ✅ **핫패스 문제 없음** — `Resolve`는 매 프레임이 아닌 공격 이벤트/입력게이트된 `CanEnter`에서만 호출(`UpdateState` 틱 경로엔 없음). `TryEnter`는 `HasInput` 게이트 뒤 → `CanAffordRoute` 메서드그룹 delegate 할당은 공격입력 프레임에만, 무시 가능.
- ✅ **null/논리 양호** — 모든 진입점 `playerActor/combat/controller` 가드, 트래커 `??=`, null `routes/tags/Motor` 처리.
- ✅ **방어 추가** — `ComboRouteResolver.IsExecutable`(attackInfo.baseInfo!=null && animKey!=None)로 미설정 라우트가 기본콤보를 가려 입력 먹통(dead input)되는 것 방지.
- ✅ **Charge 기능화** — 이전엔 어디서도 push 안 돼 Charge 포함 라우트가 영영 미매칭 → `ExecuteChargeAttack`에서 push 추가.
- ✅ **콤보 수명주기 seam 안전** — route 발동 후 `ChangeToNextState`가 `_comboInputted=false` 리셋 → route 모션 완료 시 else분기(ResetCombo→Idle) 종료, 팬텀체인 없음. (단 #1 플레이테스트 대상)

**알려진 비대칭(authoring 주의):**
- 지상 경로는 `CanEnter→PeekNextAnimKey→HasMotion`으로 진입 전 모션 보유를 확인하지만, **공중 경로는 모션 존재 사전 검사가 없다**. 라우트 `animKey`가 None은 아니지만(IsExecutable 통과) 실제 캐릭터 MotionSet에 없으면, JumpAttackState 진입 후 `PlayMotion`이 null → 완료 콜백 미구독 → 착지로 빠질 때까지 "아무 일도 안 일어남". authoring 오류일 때만 발생(에디터 진단이 None은 경고). 공중 라우트 animKey는 캐릭터 MotionSet에 실제 존재해야 한다.

**Phase 1.1 잔여(남은 것):**
1. 에디터 시뮬레이터: 태그 컨텍스트 입력(현재 tags=null이라 required 태그 라우트는 시뮬레이션 제외).
2. (선택) 다이브/강하 라우트의 커스텀 하강 물리(모션/velocity) — 자기완결 루트모션이면 불필요.
3. `ActorRuntimeMonitorWindow`에 현재 토큰 윈도우 컬럼(§7.5, Phase 3).
4. (선택) 토큰 칩 비주얼 빌더(클릭 순환) — 현재는 PropertyField + 가독 토큰 체인 요약으로 대체.

---

## 1. 목표

플레이어 전투를 다음 4버튼 입력 체계 위에서, **캐릭터·무기별로 다른 공격 데이터**를 가지면서
**입력 시퀀스(패턴)에 따라 서로 다른 연계스킬로 분기**할 수 있는 구조로 고도화한다.

| 슬롯 | 입력 | 역할 | 명조 대응 |
|------|------|------|-----------|
| **[1]** | 약공 | 약공 콤보 체인 | Basic Attack |
| **[2]** | 강공 | 강공 콤보 / 차지 | Heavy Attack (단, 명조는 hold-basic, 본작은 별도 버튼) |
| **[3]** | 스킬1 | 쿨다운 기반 리조넌스 스킬 | Resonance Skill |
| **[4]** | 스킬2 | 게이지 기반 강력기/궁극기 | Resonance Liberation |

**핵심 요구: 입력 시퀀스 분기 연계스킬(連繫, Combo Route)**

- 예시 1) `약 → 약 → 약 → 강` ⇒ 연계스킬 A (피니셔)
- 예시 2) `대시 → 점프 → 스킬1` ⇒ 연계스킬 B (공중 강하 스킬)

> 즉, 동일한 `[3] 스킬1` 입력이라도 **직전 입력 히스토리**에 따라 일반 스킬1 또는 연계스킬로 분기한다.

몬스터 측 구조는 기존 유지(`EnemyAttackInfo` / BT 기반)이며 본 설계는 **플레이어 전용**이다.

---

## 2. 현재 구조 요약 (As-Is)

### 2.1 데이터

- `PlayerAttackDataSO` (= `CharacterModelData.attackData`) — **캐릭터별** 공격 풀.
  - `liteComboAttackList`, `heavyComboAttackList`, `jumpAttackList`, `dashAttackList`, `skillAttackList`
  - `counterAttack`, `parryCounterAttack`, `entryAttack`, `swapSpecialAttack`
  - `chargeAnimKey` / `chargeStages` / `chargeStageThresholds`
- `PlayerAttackInfo { AttackInfoBase baseInfo; PlayerInterruptAction interruptActions; float hitAngle; }`
- `AttackInfoBase { AnimKey animKey; AttackType attackType; List<HitPhaseData> hitPhases; }`

### 2.2 실행 흐름

```
입력 → PlayerAttackState.OnEnter
         └ GetAnimKey() [우선순위 체인]
              0. 패리 반격 (_isParryCounter)
              1. 퍼펙트 가드 반격 (_isCounter)
              1. 풀게이지 교체 특수공격 (_isSwapSpecialAttack)
              2. 교체 등장 공격 (_isEntryAttack)
              -. 강제 Light / 강제 Heavy (인터럽트 캔슬 경로)
              1. 숫자 키 스킬 (HasSkillInput(i) + SkillGauge.ConsumeSkill)
              2. 기본 약/강 콤보 (ExecuteAttack / ExecuteHeavyAttack)
```

- 콤보 진행: `PlayerCombat`이 `_normalComboIndex` / `_heavyComboIndex` **분리 체인**으로 관리.
  약↔강 전환 시 서로 리셋하지 않고 각자 진행도 보존(`ResetComboPreserveChains`).
- 캐릭터별 콤보 상태는 `_comboStatesByCharacter`(Dictionary)에 저장/복원.
- 캐릭터 교체: `PlayerSwapBehaviour.SwapTo` → `PlayerActor.RefreshForCharacter` →
  `_combat.RefreshAttackData(model.attackData, ...)` 로 **per-character 데이터 스왑**.

### 2.3 As-Is의 한계 — "왜 연계가 안 되는가"

> 과거 `ComboSequence` 시스템(2026-04-12 도입, 2026-04-14 `c70a4f8 "Combo 기능 제거"`로 삭제)이
> 존재했으나 아래 구조적 결함으로 폐기되었다. **본 설계는 그 결함을 고치고 부활시키는 것**이 핵심이다.

| 결함 | 내용 | 본 설계의 해결 |
|------|------|----------------|
| **토큰 스트림 위치** | 입력 히스토리(`InputSequenceTracker`)를 `PlayerCombat`이 소유 → `ResetCombo()`에서 소멸. 공격 상태를 벗어나면(대시/점프) 끊김 | 토큰 스트림을 **입력/플레이어 레이어**로 승격. 상태 전환을 넘어 유지, 타이밍 윈도우로만 만료 |
| **비공격 입력 미기록** | `ComboInputType`에 Dodge/Jump/Skill이 정의됐지만 실제 `Record()`는 `ExecuteAttack`/`ExecuteHeavyAttack`(공격 경로)에서만 호출 → `대시→점프→스킬1`을 물리적으로 기록 불가 | 각 상태(`PlayerDashState`, `PlayerAirborneState`, 스킬)가 **진입 시 자기 토큰을 직접 push** |
| **정확 길이 매칭만** | `Matches`가 `history.Count == sequence.Count` 전체 일치 강제 → 접미(suffix)/윈도우 매칭 불가, 경직 | **접미(suffix) 매칭** + 최장·최우선 우선. 긴 체인 끝에서도 짧은 라우트가 성립 |

---

## 3. 목표 아키텍처 (To-Be)

### 3.1 레이어 분리

```
┌─ 입력 레이어 ────────────────────────────────────────────────┐
│ ComboInputTracker  (신규, PlayerActor 레벨 소유)              │
│  - 토큰 스트림 기록 (시각 타임스탬프 포함)                    │
│  - 만료 윈도우 / 착지·전투이탈 리셋 규칙                      │
│  - 상태 전환을 넘어 생존                                      │
└──────────────────────────────────────────────────────────────┘
            ▲ push 토큰                       │ 조회
            │                                 ▼
┌─ 상태 레이어 ─────────┐      ┌─ 결정 레이어 ──────────────────┐
│ PlayerDashState       │      │ ComboRouteResolver (신규)      │
│  → push(Dash)         │      │  - 등록 라우트 vs 스트림 매칭   │
│ PlayerAirborneState   │      │  - 태그/상태/자원 조건 평가     │
│  → push(Jump)         │      │  - 최장·최우선 라우트 선택      │
│ PlayerAttackState     │─────▶│  GetAnimKey() 체인에 삽입       │
│  → push(Light/Heavy)  │      └────────────────────────────────┘
│  → push(Skill1/Skill2)│                 │
└───────────────────────┘                 ▼
                              ┌─ 데이터 레이어 ─────────────────┐
                              │ PlayerAttackDataSO.comboRoutes  │
                              │  (per-character)                │
                              │ + WeaponComboRouteSet (per-무기)│
                              └─────────────────────────────────┘
```

### 3.2 명조 구조 → 본작 매핑 (구조만 차용, 입력 스킴은 본작 4버튼 유지)

| 명조 개념 | 본작 매핑 | 비고 |
|-----------|-----------|------|
| Basic Attack | `[1]` 약공 → `liteComboAttackList` | 기존 유지 |
| Heavy Attack | `[2]` 강공 → `heavyComboAttackList` / `chargeStages` | 명조는 hold-basic이나 본작은 **독립 버튼** |
| Resonance Skill | `[3]` 스킬1 → `skillAttackList[0]` + 쿨다운 | `PlayerSkillGauge` 또는 쿨다운 |
| Resonance Liberation | `[4]` 스킬2 → `skillAttackList[1]` + 게이지 | 연출은 §6, `ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md` 재사용 |
| Forte Circuit | `chargeStages` / 자원 게이트 강화 라우트 | Phase 2, 조건부 라우트로 표현 |
| Intro Skill | `entryAttack` (교체 등장 공격) | 이미 존재 — 재사용 |
| Outro / 풀게이지 교체 | `swapSpecialAttack` | 이미 존재 — 재사용 |
| **연계(Combo Route)** | **신규 `comboRoutes`** | 본 설계 핵심 |

> 원칙: **명조의 입력 스킴을 복사하지 않는다. 구조(콤보→스킬 링크, 자원 게이트, intro/outro)를 차용**하여
> 이미 보유한 프리미티브(`entryAttack`/`swapSpecialAttack`/`skillAttackList`+`SkillGauge`/`chargeStages`) 위에 얹는다.

---

## 4. 데이터 모델 (제안)

### 4.1 입력 토큰

```csharp
namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 콤보 라우트가 인식하는 입력 토큰.
    /// 직렬화 호환을 위해 enum 정수값을 고정한다(과거 ComboInputType 값 계승).
    /// </summary>
    public enum ComboInputToken
    {
        LightAttack = 0,   // [1] 약공
        HeavyAttack = 1,   // [2] 강공
        Dodge       = 2,   // 회피(대시)
        Skill1      = 3,   // [3] 스킬1
        Jump        = 4,   // 점프
        Skill2      = 5,   // [4] 스킬2
        Charge      = 6,   // 강공 홀드(차지) 완료
        // 확장: AirAttack, DashAttack, PerfectDodge ...
    }
}
```

> 과거 `ComboInputType`의 `Skill`(=3)을 `Skill1`로 승계, `Skill2`(=5) 신규 추가.

### 4.2 라우트 엔트리

과거 `ComboSequenceEntry`를 계승하되 **매칭 모드/상태 조건/소비 자원**을 확장한다.

```csharp
[Serializable]
public class ComboRouteEntry
{
    [Tooltip("식별용 이름(에디터 표시)")]
    public string routeName = "New Route";

    [Header("입력 패턴 (왼→오 순서)")]
    public List<ComboInputToken> inputPattern = new();

    [Tooltip("Exact: 스트림 전체가 정확히 일치 / Suffix: 스트림 끝이 패턴과 일치(권장)")]
    public ComboMatchMode matchMode = ComboMatchMode.Suffix;

    [Header("조건 (GameplayTag)")]
    public List<GameplayTagId> requiredTagIds = new();  // AND
    public List<GameplayTagId> blockedTagIds  = new();  // 하나라도 있으면 차단

    [Header("상태/물리 조건")]
    [Tooltip("이 라우트가 성립하려면 플레이어가 공중에 있어야 하는지")]
    public RouteGroundCondition groundCondition = RouteGroundCondition.Any;

    [Header("자원 소비")]
    [Tooltip("차감할 스킬 게이지 슬롯(-1=없음). 부족하면 매칭돼도 미발동")]
    public int skillGaugeIndex = -1;
    [Tooltip("쿨다운(초). 0이면 쿨다운 없음")]
    public float cooldown = 0f;

    [Header("실행 공격")]
    public PlayerAttackInfo attackInfo = new();

    [Tooltip("같은 길이/우선 라우트 경합 시 우선순위(높을수록 먼저)")]
    public int priority = 0;

    public bool IsEmpty => inputPattern == null || inputPattern.Count == 0;
}

public enum ComboMatchMode  { Exact, Suffix }
public enum RouteGroundCondition { Any, Grounded, Airborne }
```

### 4.3 데이터 부착 지점

```csharp
public class PlayerAttackDataSO : AttackDataSO
{
    // ... 기존 필드 ...

    [Header("Combo Routes (연계스킬)")]
    [Tooltip("입력 시퀀스 분기 연계스킬 목록. per-character.")]
    public List<ComboRouteEntry> comboRoutes = new();
}
```

**무기 축 (캐릭터 or 무기):** 1차는 **per-character**(`CharacterModelData.attackData`)로 충분.
무기별 분기는 다음 둘 중 하나로 **Phase 2에 레이어링**한다(현 시점 미구현, 가정만 명시):

- (A) `WeaponComboRouteSet`(무기 SO)에 `comboRoutes`를 두고 캐릭터 라우트 위에 **머지(override > base)**.
- (B) `ComboRouteEntry.requiredTagIds`에 `Weapon.Sword` 등 **무기 태그 조건**을 부여해 단일 풀에서 분기.

> 권장: (B) 태그 조건 방식이 기존 GameplayTag 인프라를 그대로 쓰므로 초기 비용이 낮다.
> 무기별 풀 규모가 커지면 (A)로 승격.

---

## 5. 런타임 동작

### 5.1 ComboInputTracker (신규)

- **소유**: `PlayerActor`(또는 입력 컴포넌트). `PlayerCombat`이 아니다. → 상태 전환에도 생존.
- **기록 API**: `Push(ComboInputToken token)` — 각 상태가 진입/발동 시 자기 토큰을 push.
  - `PlayerAttackState` → Light/Heavy/Skill1/Skill2 (어느 슬롯으로 진입했는지)
  - `PlayerDashState.OnEnter` → `Dodge`
  - `PlayerAirborneState.OnEnter`(점프 입력 진입 시) → `Jump`
  - 차지 완료 → `Charge`
- **만료 규칙(구현됨, 중요)**: **간격(gap) 기반**. 마지막 토큰 이후 `now - lastTokenTime > LinkWindow`(기본 1.0s)면 체인 **전체를 폐기**. 절대 나이 기반이 아니라 누적 시간이 긴 콤보(약약약→강, 애니 1.2~1.8s)도 끊김 없이 누적된다.
  - ⚠ 절대 나이 기반(`now - 토큰생성시각 > window`)으로 짜면 약약약 도중 첫 L이 만료돼 `[L,L,L,H]`가 영영 매칭 안 되는 버그 → 간격 기반으로 확정.
- **리셋 규칙(구현됨)**:
  - 간격 타임아웃(위) — 조회/Push 시 lazy 평가
  - 피격(`PlayerHitState.OnEnter`) 진입 시 전체 클리어
  - 캐릭터 교체(`PlayerActor.RefreshForCharacter`) 시 전체 클리어
  - 연계 라우트 발동 시 전체 클리어(`GetAnimKey`에서 stale 접두 재매칭 방지)
  - (Phase 1.1) 착지 시 공중 토큰 정리, 전투 이탈 클리어 — 현재는 간격 만료로 대체
- **조회 API(구현됨)**: `GetWindow()`(만료 제외 현재 스트림), `GetWindowWith(pending)`(가상 append, peek용). 둘 다 내부 캐시 재사용 → 호출 직후 동기 사용.

> 과거 트래커는 `PlayerCombat`이 소유 + `ResetCombo`에서 Clear라 대시/점프를 못 넘겼다.
> 소유권을 올리고 만료를 **간격 기반**으로 바꾸는 것이 부활의 핵심.

### 5.2 ComboRouteResolver (신규)

```csharp
// PlayerAttackState.GetAnimKey() 안, "숫자 키 스킬"과 "기본 약/강 콤보" 사이에 삽입.
ComboRouteEntry route = _routeResolver.Resolve(
    pendingToken,                 // 이번 입력으로 들어올 토큰
    _inputTracker.Window,         // 만료 제외 토큰 스트림
    _playerActor.Tags,            // 태그 조건
    motor.GroundingStatus,        // 지상/공중 조건
    playerActor.SkillGauge);      // 자원 조건

if (route != null)
{
    _currentAttack = _combat.ExecuteComboRoute(route);
    return _currentAttack.animKey;
}
```

**매칭 알고리즘** (`pendingToken`을 스트림 끝에 가상 append 후):

1. `comboRoutes` 순회, `IsEmpty` 스킵.
2. `matchMode`에 따라:
   - `Suffix`: 스트림의 **마지막 N개**(N=패턴 길이)가 패턴과 일치?
   - `Exact`: 스트림 전체가 패턴과 정확히 일치?
3. 태그(`required` 전부 / `blocked` 없음) + `groundCondition` + 자원(게이지/쿨다운) 통과?
4. 통과 후보 중 **(패턴 길이 큰 것 > priority 큰 것)** 순으로 1개 선택.
5. 없으면 `null` → 기존 기본 콤보 폴백.

> 가상 append는 try/finally 없이 **순수 조회**로 구현(스트림 비변형). 실제 토큰 push는 Execute에서.

### 5.3 GetAnimKey() 우선순위 (To-Be)

```
0. 패리 반격
1. 퍼펙트 가드 반격
1. 풀게이지 교체 특수공격
2. 교체 등장 공격(Intro)
-. 강제 Light / 강제 Heavy (캔슬 경로)
1. 숫자 키 스킬 (raw Skill1/Skill2)
★  ComboRouteResolver.Resolve()  ← 신규 삽입 (raw 스킬 다음, 기본 콤보 앞)
2. 기본 약/강 콤보 (폴백)
```

> 삽입 위치 근거: raw 스킬보다 **뒤**에 두면 "스킬1 단독 = 일반 스킬1 / 패턴 끝의 스킬1 = 연계"가
> 자연스럽게 갈린다. 단, 라우트가 raw 스킬을 **선점**해야 하는 경우(예: `대시→점프→스킬1`은
> 일반 스킬1보다 우선)는 raw 스킬 루프에서 "현재 윈도우에 매칭 라우트가 있으면 raw 스킵" 가드를 둔다.
> → **구현 시 검증 필요 항목**(§8 미검증 리스크).

### 5.4 PlayerCombat 확장

- `ExecuteComboRoute(ComboRouteEntry route)` 추가 — 과거 `ExecuteComboSequence` 계승.
  - 패턴 마지막 토큰으로 `AttackKind`/`AttackState` 결정.
  - `ConvertToAttackData(route.attackInfo, kind)`.
  - 게이지/쿨다운 차감.
  - 콤보 인덱스: 연계 진입은 보통 체인 리셋 후 단발 → `ResetComboPreserveChains` 정책과 정합 확인.

---

## 6. 스킬2 / 궁극기 연출 연동

`[4] 스킬2`가 게이지 풀 시 궁극기로 동작하는 경우, **연출 오케스트레이션은 신규 구현하지 않고**
기존 설계 `Assets/docs/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md`(카메라 스냅샷 + 잠금 + 타임라인)를 재사용한다.

- 연계 라우트의 `attackInfo`가 궁극기 MotionSet을 지목 → `UltimateSequencePlayer.Play(asset, ...)` 트리거.
- 즉, **본 문서 = "무엇을 어떤 입력으로 발동하나(결정/라우팅)"**,
  **Ultimate 문서 = "발동된 연출을 어떻게 보여주나(연출/잠금/복구)"**. 두 축은 직교.

---

## 7. 에디터 툴 설계 (Combo Route Editor)

연계스킬은 **입력 패턴 + 조건 + 공격 데이터**의 다축 데이터라 raw 인스펙터로는 편집/검증이 어렵다.
과거 `ComboSequenceEditor`(509줄, `0d64239`에 보존)의 **비주얼 입력 체인 빌더 UX를 부활**시키되,
신규 토큰·필드·진단(diagnostics)을 더해 개선한다.

### 7.1 As-Is 에디터 지형

| 파일 | 역할 | 본 작업 영향 |
|------|------|--------------|
| `PlayerAttackDataSOWindow` | 플레이어/몬스터 공격 데이터 **통합 윈도우** (`UPlayGround/Gameplay/Combat/공격 데이터 에디터`) | **여기에 Combo Route 탭 추가** |
| `PlayerAttackDataSOEditor` | 커스텀 인스펙터 ("에디터 창에서 열기" 버튼) | 버튼 그대로, 통합 윈도우로 유도 |
| `PlayerAttackDataSODrawer` | 인스펙터/윈도우 공용 GUI 드로어 | `comboRoutes` 섹션 추가 |
| `ComboSequenceEditor` *(삭제됨)* | 독립 비주얼 체인 빌더 윈도우 | **UX 부활 → 통합 윈도우 탭으로 흡수** |

### 7.2 배치 결정: 독립 창 → 통합 윈도우 탭

> 과거엔 `ComboSequenceEditor`가 **별도 EditorWindow**였다. 윈도우 난립을 막기 위해
> **`PlayerAttackDataSOWindow` 안의 탭/섹션**으로 흡수하는 것을 권장한다.
> (같은 SO를 두 창에서 열어 `SerializedObject` 중복 → 저장 충돌 위험 제거)

```
PlayerAttackDataSOWindow [공격 데이터 에디터]
├─ Tab: 기본 공격 풀     (기존 PlayerAttackDataSODrawer)
└─ Tab: 연계 라우트 ★신규 (ComboRoutePanel — 과거 비주얼 빌더 UX)
     ├─ 좌: ReorderableList (미니 체인 미리보기 + 이름 + priority + 게이지 배지)
     └─ 우: 디테일 (체인 빌더 / 조건 / 공격정보 / 진단)
```

### 7.3 부활할 UX (과거 검증된 패턴)

- **토큰별 색상 코딩 + 약어 칩**: `L`(약) `H`(강) `D`(회피) `S1`/`S2`(스킬) `J`(점프) `C`(차지).
  - 과거 매핑(L/H/D/S/J) → 신규 토큰(`ComboInputToken`)에 맞춰 **S→S1, S2/C 추가**.
- **클릭 순환 입력 체인**: 칩 클릭 시 토큰 순환(`L→H→D→S1→S2→J→C→L`), `[×]` 삭제, `[+]` 스텝 추가.
- **좌측 리스트 미니 미리보기**: 각 라우트의 첫 5토큰을 색칩으로, 초과 시 `…`.
- **태그 조건 드롭다운**: `requiredTagIds`(초록) / `blockedTagIds`(빨강) — `GameplayTagId` enum 드롭다운,
  툴바에 `🏷 Tag Registry Editor` 바로가기(기존 `GameplayTagRegistryEditorWindow.Open()`).
- **공격 정보 섹션**: `attackInfo`(`PlayerAttackInfo`) 인라인 편집.

### 7.4 신규/개선 항목 (과거 대비)

| 항목 | 설명 |
|------|------|
| **신규 토큰** | `Skill1`/`Skill2`/`Charge` 칩·색상·순환 추가 (과거 Skill 단일 → 분리) |
| **매칭 모드 선택** | 디테일에 `matchMode`(Exact/Suffix) 토글. Suffix 기본, 시각적으로 "끝 N개 매칭" 표기 |
| **상태 조건** | `groundCondition`(Any/Grounded/Airborne) 드롭다운 — 공중 라우트 식별 아이콘 |
| **자원 표기** | `skillGaugeIndex` 배지(`G:n`) + `cooldown` 필드. 0 미만/초과 경고 |
| **충돌 진단(diagnostics)** | ★ 핵심: 라우트 간 **그림자(shadow)/도달불가** 자동 검출. 예) 짧은 Suffix 라우트가 긴 라우트의 접미를 항상 선점 → 경고. 같은 패턴+조건 중복 → 경고 |
| **입력 시뮬레이터** | ★ 토큰 스트림을 직접 입력(버튼 클릭 누적)하면 **`ComboRouteResolver`와 동일 로직**으로 어떤 라우트가 매칭되는지 실시간 하이라이트. 만료 윈도우/우선순위 결과 미리보기 |
| **MotionSet 연동** | `attackInfo.animKey`를 캐릭터 MotionSet에서 드롭다운 선택 (기존 `AttackDataFromMotionSetWindow` 패턴 재사용) |

> **진단 로직은 런타임 `ComboRouteResolver`와 동일 코드를 공유**해야 한다(에디터 전용 재구현 금지).
> Resolver의 매칭 함수를 `static`/순수 함수로 분리해 에디터·런타임 양쪽에서 호출하도록 설계한다.

### 7.5 런타임 모니터 연동

- `ActorRuntimeMonitorWindow`에 **현재 토큰 윈도우** 실시간 컬럼 추가
  (기존 GameplayTags 컬럼 패턴 재사용 — [[project_gameplaytag_combo]] 참고).
  플레이 중 `대시→점프→스킬1` 입력이 색칩 스트림으로 흐르고, 매칭된 라우트명이 강조 표시.

---

## 8. 구현 단계 (Phase)

### Phase 1 — 기반 부활 (per-character, 태그 조건)
1. `ComboInputToken` enum + `ComboRouteEntry`/`ComboMatchMode`/`RouteGroundCondition` 데이터.
2. `ComboRouteResolver`의 **매칭 함수를 순수/static으로 분리** (런타임·에디터 공유 — §7.4).
3. `ComboInputTracker`를 `PlayerActor` 레벨에 신설(시간 윈도우/리셋 규칙).
4. 각 상태에서 토큰 `Push`(Dash/Jump/Attack/Skill1/Skill2/Charge).
5. `ComboRouteResolver` + `PlayerCombat.ExecuteComboRoute`.
6. `GetAnimKey()` 체인 삽입 + raw 스킬 선점 가드.
7. **에디터**: `PlayerAttackDataSO.comboRoutes` 필드 + 통합 윈도우 **연계 라우트 탭**
   (과거 `ComboSequenceEditor` 비주얼 빌더 UX 부활 — §7.3) + 충돌 진단/시뮬레이터(§7.4).
8. 예시 2종 데이터로 검증: `L L L H`, `Dodge Jump Skill1`.

### Phase 2 — 무기 축 / Forte
9. 무기 분기: 태그 조건(B안) → 필요 시 `WeaponComboRouteSet`(A안) 머지.
10. Forte 유사 자원 게이트(차지/스택)로 강화 라우트 조건화.

### Phase 3 — 표현/툴
11. HUD 연계 힌트(과거 `GetNextComboHints`/`NextComboHint` 부활) — 다음 가능한 입력 미리보기.
12. `ActorRuntimeMonitorWindow`에 현재 토큰 윈도우 실시간 표시(§7.5).

---

## 9. 리스크 / 결정 (구현 후 갱신)

> ✅=구현으로 해소, ⏳=Phase 1.1/검증 대기

| 항목 | 내용 | 결론 |
|------|------|------|
| ✅ raw 스킬 vs 라우트 선점 | `대시→점프→스킬1`이 일반 스킬1보다 우선해야 함 | **단일 판정점**으로 해결 — GetAnimKey의 forced/skill/basic 분기 '앞'(entry 직후)에서 라우트 가로챔. peek/콤보연속도 동일 인지 |
| ✅ 토큰 만료 윈도우 값 | linkWindow(초). 길면 의도치 않은 연계, 짧으면 입력 실패 | **간격 기반 1.0s** 확정(절대 나이는 긴 콤보 미매칭 버그). 데이터별 노출은 ⏳Phase 1.1 |
| ✅ 콤보 인덱스 상호작용 | 연계 진입 후 약/강 분리 체인 복원 정책 | `ExecuteComboRoute`가 `ResetComboPreserveChains` 호출(단발). seam은 `_comboInputted=false`로 안전 종료 확인 |
| ✅ 입력 버퍼와 중복 | `InputBuffer` 선입력과 토큰 이중 기록 위험 | 토큰은 "발동 확정 시점"(GetAnimKey 단일 지점)에만 push |
| ✅ 캐릭터 교체 중 윈도우 | 교체 시 토큰 스트림 클리어 여부 | `RefreshForCharacter`에서 `ComboInputTracker.Clear()` |
| ✅ 미설정 라우트 dead input | animKey 미설정 라우트가 기본콤보를 가려 입력 먹통 | `Resolver.IsExecutable` 필터로 매칭 제외(런타임+에디터 진단 일치) |
| ⏳ 공중 스킬 라우트 호스트 | 공중 스킬 입력이 PlayerAttackState로 미전환 | Phase 1.1 — `PlayerAirborneState` 전환 배선 + 공중 물리 |
| ⏳ HasSkillInput 시맨틱 | edge/held — held면 약공콤보 중 Skill1 오판 가능 | 기존 skill-loop와 동일 동작이라 무해 추정, 검증 필요 |
| ⏳ 무기 축 선택 | (A)무기 SO 머지 vs (B)무기 태그 조건 | (B) 우선, 규모 커지면 (A) — Phase 2 |
| ⏳ 스킬2=궁극기 게이팅 | 쿨다운 기반 vs 게이지 기반 분리 | 스킬1=쿨다운, 스킬2=게이지 — Phase 2 |
| ✅ 에디터 배치 | 독립 창 vs 통합 윈도우 탭 | **통합 드로어 "연계" 탭**으로 흡수(창 난립/저장 충돌 방지) |
| ✅ 진단 코드 공유 | 에디터 재구현 시 런타임과 괴리 | `ComboRouteResolver.Resolve` 순수 static을 에디터 시뮬레이터가 그대로 호출 |

---

## 10. 영향 파일 (구현 반영)

**신규 (작성됨)**
- `Assets/02.Scripts/Data/Combat/ComboRouteData.cs` — 토큰/엔트리/enum
- `Assets/02.Scripts/Data/Combat/ComboRouteResolver.cs` — 순수 static 매칭(런타임·에디터 공유). ※설계 초안의 Component/Player 대신 Data/Combat에 배치
- `Assets/02.Scripts/Input/ComboInputTracker.cs` — 과거 InputSequenceTracker 개량, 소유권 상향

**수정 (작성됨)**
- `Data/Combat/PlayerAttackDataSO.cs` — `comboRoutes` 필드
- `GameActor/Component/Player/PlayerCombat.cs` — `ExecuteComboRoute`/`CanAffordRoute`/`ComboRoutes`/`RouteAttackKind`, `ExecuteChargeAttack`에 Charge push
- `GameActor/State/Player/PlayerAttackState.cs` — `ResolveComboRoute`/`TryComputePendingToken`(GetAnimKey·PeekNextAnimKey·ChangeToNextState 단일 판정점), `using Data.Combat`
- `GameActor/State/Player/PlayerDashState.cs` — Dodge push
- `GameActor/State/Player/PlayerAirbornState.cs` — Jump push(점프 입력 진입 한정)
- `GameActor/State/Player/PlayerHitState.cs` — 피격 시 트래커 Clear
- `GameActor/Object/Player/PlayerActor.cs` — `ComboInputTracker` 소유 + 교체 시 Clear
- `Data/Combat/Editor/PlayerAttackDataSODrawer.cs` — **"연계" 탭**(저작 PropertyField + 진단 + 시뮬레이터). 통합 윈도우/인스펙터 양쪽에 자동 반영

**미작성 (Phase 1.1/3)**
- `ActorRuntimeMonitorWindow` — 현재 토큰 윈도우 컬럼(§7.5)
- (선택) 토큰 칩 비주얼 빌더 PropertyDrawer

**참고(부활 원본, git 보존)**
- `0d64239:Assets/02.Scripts/Data/Combat/Editor/ComboSequenceEditor.cs` (509줄 비주얼 빌더)
- `0d64239:Assets/02.Scripts/Input/InputSequenceTracker.cs` (74줄 트래커)
- `0d64239:Assets/02.Scripts/Data/Combat/ComboSequenceData.cs` (엔트리/태그조건)

**참고(직교, 재사용)**
- `Assets/docs/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md` — 스킬2/궁극기 연출
- `Assets/docs/Complete/GAMEPLAY_TAG_SYSTEM_GUIDE.md` — 라우트 태그 조건

---

## 부록 A. 과거 ComboSequence 폐기 이력 (git)

- `423c860` (04-12) 게임플레이 태그 / 콤보 시퀀스 기반 스킬 확장 — 도입
- `0d64239` (04-13) ComboSequence Test
- `c70a4f8` (04-14) **Combo 기능 제거** — `ComboSequenceData.cs`, `ComboSequenceEditor.cs`(509줄),
  `InputSequenceTracker.cs`(74줄), `PlayerCombat`/`PlayerAttackState` 통합 코드 일괄 삭제.

> 폐기 원인(코드 분석): §2.3의 3대 결함(토큰 소유권/비공격 미기록/정확 매칭). 데이터·에디터·트래커
> 원형 코드는 git에 보존되어 있으므로 **본 설계대로 결함을 고쳐 부활**하는 것이 합리적.

## 부록 B. 명조 참고 출처

- [Combat System Guide | Game8](https://game8.co/games/Wuthering-Waves/archives/452894)
- [Combat Guide | wutheringwaves.gg](https://wutheringwaves.gg/combat-basics/)
- [Intro and Outro Skills | Gacha HQ](https://gachahq.com/wuthering-waves/intro-outro-skills/)
- [Concerto Energy / Outro·Intro | Game8](https://game8.co/games/Wuthering-Waves/archives/456637)
- [Forte | Wuthering Waves Wiki](https://wutheringwaves.fandom.com/wiki/Forte)
