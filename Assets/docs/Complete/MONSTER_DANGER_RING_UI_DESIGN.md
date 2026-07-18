# 몬스터 공격 UI Danger Ring 설계

> 작성일: 2026-05-24
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 상태: **Phase 1 코드 구현 완료 (2026-05-24)** / 에디터 와이어링 대기
> 레퍼런스 조사 반영: 2026-05-24 (명조 Weakness Halo 웹 조사)

---

## 구현 상태 (2026-05-24)

**코드 완료:**
- `AttackInfo.cs` — `AttackDefenseType { Parryable, GuardableOnly, Unblockable }` enum 추가.
- `CombatData.cs` — `AbilityAttackInfo`에 `useDangerRing`/`dangerRingDuration`/`dangerRingPrefabKey`/`defenseType`, `AttackData`에 `defenseType`(기본 `Parryable`).
- `ActorType.cs` — `ActorSocketType.UI_DangerRing`(**enum 끝에 추가** — 직렬화 값 시프트 방지).
- `EnemyCombat.cs` — `BeginTelegraph` 디스패처 재구조화(early-return 제거, 바닥FX/링 독립 분기), `BeginDangerRing`/`ResolveDangerRingDuration`, `ClearTelegraphs`에서 링 정리, `CheckMeleeAttackHit`에서 `defenseType` 복사.
- `PlayerGuardState.cs` — 퍼펙트 가드 시 `defenseType == Parryable`만 `OnParried()`+카운터 창.
- `UI_DangerRing.cs` (신규), `UI_WorldSpaceHudLayer.CreateDangerRing`, `UIManager.CreateDangerRing` + 기본 프리팹 등록(키 `"DangerRing"`).

**설계 대비 단순화(Phase 1):**
- 풀링 대신 **Instantiate/Destroy**(HP바·바닥FX와 동일). 풀링은 추후 최적화.
- 링은 `UpdateTelegraphs` 경유 Tick이 아니라 **자체 `LateUpdate`에서 `Time.deltaTime`(스케일 시간)으로 채움**. `GameTimeManager`가 전역 히트스톱(`Time.timeScale=_activeScale`)·일시정지(`=0`)를 timeScale로 관리함을 확인 → 링 채움이 자동 정지/감속한다. (단 퍼펙트 가드의 `HitStopIntensity.PlayerGuard`는 timeScale 미변경 Actor-only 슬로우라 링에 영향 없음 — 단, 이 시점은 이미 타격 후라 무관.)

**주의 — MotionEvent 타이밍 경로 footgun:**
`useMotionEventTelegraph = true`면 `EnemyAttackState.OnEnter`가 `BeginCurrentSkillTelegraph`를 건너뛰고 타임라인의 `TelegraphEvent`를 기다린다. 따라서 `useDangerRing=true, useTelegraph=false`라도 **MotionEvent 타이밍을 쓰려면 `TelegraphEvent` 오써링이 필수**다 — `useTelegraph=false`일 때도 동일 `TelegraphEvent`가 링 시작 신호가 된다. 이벤트가 없으면 링이 시작되지 않는다. (자동 시작 경로 `useMotionEventTelegraph=false`는 이벤트 없이도 OnEnter에서 시작.)

**남은 에디터 작업(사용자):** 아래 [에디터 작업 (Unity 직접 수행)](#에디터-작업-unity-직접-수행) 섹션 참조 — 프리팹 생성 → DB 등록 → 소켓 배치 → 테스트 공격 설정 → 검증.

---

## 개요

몬스터가 공격을 시작할 때, 공격하는 적의 머리 위에 **화면을 따라다니는 원형 라디얼 필 링**을 표시해 "언제 타격이 들어오는지"를 시각적으로 알려주는 HUD 시스템 설계서. 윈드업(예비 동작) 동안 `Image.fillAmount`가 `0 → 1`로 차오르고, 가득 차는 순간이 실제 타격 순간과 일치하도록 한다. 플레이어는 링의 채워지는 속도를 보고 회피·가드·패링 타이밍을 잡는다.

**주 레퍼런스는 명조(Wuthering Waves, Kuro Games)** 의 패리 신호 "Weakness Halo"(적 위에 뜨는 원형 타이밍 고리)이며, 세키로(perilous attack)의 위험 심볼을 보조 참조로 둔다. 이 "원형 1점 타이밍" 패턴을, 이 프로젝트의 기존 **월드 공간 HUD 인프라**(`UI_WorldSpaceHudLayer` + `UI_ActorHpBar`)와 **텔레그래프 생명주기**(`EnemyCombat.BeginTelegraph` → `UpdateTelegraphs` → `ClearTelegraphs`)에 얹어 구현한다.

핵심 방향은 다음과 같다.

- **기존 바닥 원형 FX 텔레그래프를 대체하지 않고 보완한다.** 바닥 데칼은 "어디"(공간 범위), Danger Ring은 "언제"(타이밍)를 담당한다.
- **Danger Ring은 바닥 텔레그래프와 독립이다.** 같은 생명주기 훅(`OnEnter`/`UpdateState`/`OnExit`, `TelegraphEvent`)에 얹지만, `useTelegraph`와 `useDangerRing`은 **서로 다른 플래그**로 각자 켜고 끈다. 바닥 텔레그래프가 꺼져 있어도(`useTelegraph=false`) Danger Ring은 단독 출력될 수 있어야 한다. (요구사항: 텔레그래프 미출력 시에도 링 출력)
- 위치 추적·화면 변환·`fillAmount` 구동은 이미 검증된 `UI_ActorHpBar` 패턴을 그대로 재사용한다.
- 채우는 시간(윈드업 길이)은 공격 데이터(`AbilityAttackInfo`)가 명시한 단일 값을 진실 소스로 사용한다.
- **공격마다 패링 가능/불가를 구분한다.** 명조 Weakness Halo처럼, 링의 비주얼(색)이 "이 공격이 패링 가능한가"를 표현한다. 이를 위해 공격 데이터에 방어 타입 축(`AttackDefenseType`)을 추가하고, 플레이어 가드/카운터 로직이 이를 검사하도록 함께 수정한다. (현재는 모든 공격이 무조건 패링/카운터 가능)
- 1차 구현은 **공격당 단일 링**으로 한정한다. 다단 히트·동시 다수 적·오프스크린 핸드오프는 아래 "미해결 과제"에서 범위를 분리한다.

---

## 레퍼런스 분석

### 게임 사례

**명조(Wuthering Waves, Kuro Games) — Weakness Halo [주 레퍼런스]**
- 패리 가능한 적 공격 직전, 적 위에 **노란색 동심원 2개**가 나타나고 바깥 원이 안쪽 원으로 **수축한다**. 두 원이 **겹치는 순간**에 기본 공격을 입력하면 패리(Counterattack)가 성립한다. 오버랩 윈도우는 약 0.5초.
- 이 고리의 정식 명칭은 **"Weakness Halo"**(노란 링). 패리 불가 공격에는 나타나지 않으므로, "이 고리가 보이면 곧 위협 + 패리 타이밍"이라는 **존재 자체가 정보**다.
- 색(노랑) + 형태(이중 동심원) + 모션(수축)을 결합한 이중 부호화. 명조는 위험 표시에 **붉은색을 쓰지 않는다** — 붉은색은 별도의 텍스트 "Red Warning Prompt"로 패리 불가·회피 공격에만 사용한다.
- 별도의 **차징 캐스트 바는 없다.** 진동강도(Vibration Strength)는 HP 바 아래 흰색 막대지만, 경직 상태 게이지일 뿐 공격 예고용이 아니다.
- 출처: Game8, wuthering.gg, GuildJen (하단 참조).

**세키로(Sekiro, FromSoftware) — 위험 공격(危, Perilous Attack) [보조 참조]**
- 잡기/찌르기/휩쓸기 공격 직전 적 머리 위에 붉은 한자 심볼을 띄워 "지금 들어오는 공격은 가드 불가, 특정 대응 필요"를 알린다.
- 색·심볼·타이밍을 결합한 **이중 부호화(double coding)** 의 대표 사례.

### 명조 Weakness Halo ↔ 본 설계 Danger Ring

| 항목 | 명조 Weakness Halo | 본 설계 Danger Ring |
|------|------|------|
| 기하 | 이중 동심원, 바깥→안 **수축**, 겹침 = 1점 타이밍 | 단일 원, **0→100% 라디얼 필**, 가득 참 = 1점 타이밍 |
| 의미 | 패리 가능 공격에만 등장(조건부 신호) | 패링 가능 여부를 색으로 구분(아래 방어 타입) |
| 색 | 노랑(= "패리 가능") | 패링 가능=노랑 / 패링 불가=빨강 (`AttackDefenseType` 매핑) |
| 캐스트 바 | 없음 | 없음(링이 곧 타이밍 게이지) |

**수축 vs 필 — 채택 결정:** 명조는 "두 원이 겹치는 한 점", 본 설계는 "게이지가 가득 차는 순간"을 노린다. 둘 다 **원형 1점 타이밍**이라는 본질은 같다. Phase 1은 앞서 선택한 **라디얼 필**을 유지하고, 명조식 이중 동심원 수축은 Phase 2 비주얼 옵션으로 분리한다.

**색-의미 매핑(명조 컨벤션 채택):** 명조는 노랑 = "패리 가능", 빨강 = "패리 불가/회피"로 색을 의미에 매핑한다. 본 설계도 이를 따른다 — Danger Ring 색을 자유 필드가 아니라 **`AttackDefenseType`에서 파생**시킨다(아래 "패링 가능/불가 구분" 참조). 패링 가능 공격은 노란 링, 패링 불가 공격은 붉은 링으로 그려 색만으로 대응법(패링 vs 회피)을 읽게 한다.

### 기술 일반론

**공격 3단계 구조**
공격은 예비(Anticipation, 약 0.25~1.0초) → 공격(Attack/Active) → 회복(Recovery)으로 구성된다. Danger Ring이 채워지는 구간은 **예비 단계**이며, 가득 차는 시점이 Active(타격) 시작 시점과 일치해야 한다. (출처: Game Developer, GDKeys — 하단 참조.)

**접근성**
색상만으로 위험도를 구분하면 색각 이상 사용자가 구분할 수 없다. 색 + 채워지는 모션 + (완성 시) 펄스/플래시로 **이중 부호화**를 적용한다.

---

## 현재 구조와의 관계

Danger Ring은 새 시스템이 아니라 기존 텔레그래프 생명주기 훅에 표시 객체 하나를 더 붙이는 것이다. **단, 바닥 FX와 링은 디스패처 안에서 각자 플래그로 독립 분기**한다 — `useTelegraph`로 early-return하던 기존 구조를 두 개의 독립 분기로 쪼갠다.

```
EnemyAttackState.OnEnter
  └─ EnemyCombat.SelectAndExecuteSkill(distance)
  └─ Animator.PlayMotion(skill.baseInfo.animKey)
  └─ useMotionEventTelegraph == false  →  EnemyCombat.BeginCurrentSkillTelegraph()
        └─ BeginTelegraph(0, false)   ── 디스패처: 두 분기를 독립 호출
              ├─ if skill.useTelegraph   → BeginGroundTelegraph(...) ── GameObjectManager.ShowFX(...)
              └─ if skill.useDangerRing  → BeginDangerRing(...)       ── UIManager.CreateDangerRing(...)
                  └─ 둘은 서로의 플래그에 영향받지 않음 (텔레그래프 OFF + 링 ON 가능)

EnemyAttackState.UpdateState (매 프레임, deltaTime)
  └─ EnemyCombat.UpdateTelegraphs()
        ├─ if 바닥 FX 존재  → 위치/스케일 추적
        └─ if 링 존재       → _dangerRing.Tick(deltaTime) → fillAmount 갱신

MotionSet 타임라인  TelegraphEvent (useMotionEventTelegraph == true 경로)
  └─ Execute(startTime)        → BeginTelegraph(hitPhaseIndex, ...) → 같은 디스패처(각자 플래그로 분기)
  └─ OnCompleteEvent(endTime)  → ClearTelegraphs()                  → 바닥 FX + 링 함께 정리

EnemyAttackState.OnExit
  └─ EnemyCombat.ClearTelegraphs()  → 바닥 FX + Danger Ring 함께 정리
```

> **핵심:** 기존 `BeginTelegraph`는 `!useTelegraph`이면 즉시 return하여 바닥 FX·링 모두 안 떴다. 이 early-return을 제거하고 **각 분기를 자기 플래그로만 게이트**하도록 바꾼다. `TelegraphEvent`/`EnemyAttackState` 호출부 시그니처는 그대로 둔다.

### 재사용하는 인프라

| 기존 자산 | Danger Ring에서의 역할 |
|------|------|
| `UI_WorldSpaceHudLayer` | Screen Space Overlay 캔버스에서 월드 위치 추적 UI를 생성·관리. `CreateDangerRing()` 추가 |
| `UI_ActorHpBar` | `WorldToScreenPoint` 위치 추적, `Image.fillAmount`, 카메라 뒤 alpha 처리, 소켓 기반 위치 — 그대로 복제할 패턴 |
| `EnemyCombat` 텔레그래프 메서드 | `BeginTelegraph`/`UpdateTelegraphs`/`ClearTelegraphs` 생명주기에 링 처리 분기 추가 |
| `ActorSocketType` (`ActorType.cs`) | `UI_HpBar`처럼 `UI_DangerRing` 소켓 항목 추가 |
| `UIKeyType` / `GetUIPrefabEntry` | `ActorHpBar`처럼 `DangerRing` 프리팹 키 등록 |

> HP Bar는 몬스터당 1개를 스폰 시 영속 생성하지만, Danger Ring은 **공격 윈드업마다 생성·소멸하는 트랜지언트** 객체다. 따라서 데미지 플로터(`UI_DamageFloater`)와 같은 **풀링** 방식이 더 적합하다(아래 "UI 구현" 참조).

---

## 타이밍 소스 (핵심 설계 결정)

링이 `0 → 1`로 채워지는 시간(= 윈드업 길이)을 **무엇을 기준으로 정하는가**가 이 시스템의 정확도를 좌우한다.

### 결정: `AbilityAttackInfo.dangerRingDuration` 단일 명시 값

공격 데이터에 윈드업 길이를 초 단위로 직접 명시한다. 두 진입 경로(자동 시작 / MotionEvent) 모두 같은 값을 사용해 진실 소스를 하나로 유지한다.

```csharp
[Tooltip("Danger Ring이 0→1로 채워지는 시간(초). 모션의 '윈드업 시작 → 실제 타격(Collision)' 간격과 같게 맞춘다.")]
public float dangerRingDuration = 0.6f;
```

**오써링 컨벤션 (반드시 문서화):**
링이 가득 차는 순간 == 실제 타격 순간이 되려면, `dangerRingDuration`은 "텔레그래프 시작 시점 → MotionSet의 `MotionEvent_Collision.startTime`"의 시간 간격과 같아야 한다.

- **자동 시작 경로**(`useMotionEventTelegraph == false`): 텔레그래프가 `OnEnter`에서 시작하므로 `dangerRingDuration` = `Collision.startTime`(모션 시작 기준).
- **MotionEvent 경로**(`useMotionEventTelegraph == true`): 텔레그래프가 `TelegraphEvent.startTime`에서 시작하므로 `dangerRingDuration` = `Collision.startTime − TelegraphEvent.startTime`.
  - 이 경로에서는 `TelegraphEvent`가 자신의 구간 길이(`endTime − startTime`)를 알 수 있으므로, `dangerRingDuration`이 0 이하이면 **이벤트 구간 길이를 폴백으로 사용**하도록 한다. 단 이때 `TelegraphEvent.endTime`을 `Collision.startTime`에 맞춰 배치해야 한다.

> **주의:** 현재 `TelegraphEvent.OnCompleteEvent`(= `endTime`)는 "텔레그래프 정리(clear)" 시점이지 "타격" 시점이라는 보장이 없다. 아직 실제 MotionSet에 오써링된 `TelegraphEvent` 데이터가 없어 검증할 대상이 없으므로, **이 컨벤션을 새로 정립**하는 것으로 한다. 컨벤션을 어기면(endTime을 타격보다 늦게 두면) 링이 타격 전에 100%에 도달한다.

### 폴백·미래 확장
- `dangerRingDuration <= 0`이고 이벤트 구간도 없으면 상수 기본값(예: 0.6초) 사용 + 경고 로그.
- **(미래)** 시작 시점에 현재 재생 중인 MotionSet에서 `Collision` 이벤트의 `startTime`을 조회해 윈드업을 자동 산출 → 명시 값이 모션과 어긋날 위험 제거. Phase 1 범위에서는 제외.

---

## 데이터 필드 추가 (`AbilityAttackInfo`)

`Assets/02.Scripts/Data/Combat/CombatData.cs`의 기존 `[Header("Telegraph")]` 블록 아래에 추가한다.

```csharp
[Header("Danger Ring (UI)")]
[Tooltip("공격 윈드업 동안 적 머리 위에 라디얼 필 타이밍 링을 표시할지 여부. useTelegraph와 독립.")]
public bool useDangerRing = false;

[Tooltip("Danger Ring이 0→1로 채워지는 시간(초). 윈드업 시작 → 실제 타격(Collision) 간격과 일치시킨다.")]
public float dangerRingDuration = 0.6f;

[Tooltip("비워두면 기본 Danger Ring 프리팹(UIKeyType.DangerRing)을 사용한다.")]
public string dangerRingPrefabKey;
```

> **링 색은 자유 필드가 아니다.** 아래 `defenseType`(`AttackDefenseType`)에서 파생한다 — 패링 가능=노랑, 패링 불가=빨강. 색 상수는 프로젝트 공용 팔레트나 `UI_DangerRing` 프리팹에 둔다.
>
> `telegraphRadiusScale`·`telegraphAnchorType` 등 기존 텔레그래프 필드와 **완전히 독립**이다. 바닥 데칼 없이 링만 쓰거나(`useTelegraph=false, useDangerRing=true`), 반대도, 둘 다도 가능하다.

---

## 패링 가능/불가 구분 (방어 타입)

> 명조 Weakness Halo는 "패리 가능한 공격에만" 나타난다. 본 설계도 공격마다 **패링 가능 여부**를 구분하고, Danger Ring 색이 이를 표현하도록 한다. 이 구분은 Danger Ring만의 문제가 아니라 **플레이어 가드/카운터 판정 로직을 함께 고쳐야 하는** 전투 구조 변경이다.

### 현재 동작 (구분 없음)

`PlayerGuardState.OnAttackBlocked(AttackData incomingAttack)`은 퍼펙트 가드 창(0.3초) 안에서 막으면 들어온 공격의 종류와 **무관하게 항상** 반격 창을 열고 공격자를 경직시킨다.

```csharp
// PlayerGuardState.OnAttackBlocked — 현재: 모든 공격이 패링/카운터 가능
if (isPerfectGuard)
{
    ...
    monster?.AIController?.OnParried();      // 무조건 호출
    _combat.OpenPerfectGuardCounterWindow(); // 무조건 열림
}
```

### 추가할 방어 타입 enum

`Assets/02.Scripts/Data/Enum/AttackInfo.cs`(기존 `AttackType`, `AttackReactionType`와 같은 파일)에 추가:

```csharp
/// <summary> 플레이어가 이 공격에 대해 취할 수 있는 방어 대응. </summary>
public enum AttackDefenseType
{
    Parryable,     // 퍼펙트 가드 시 패링/카운터 성립 (현재 기본 동작) — 노란 링
    GuardableOnly, // 막을 수는 있으나 카운터 불가 — 노란 링(카운터 표시 없음) 또는 별도
    Unblockable,   // 가드 불가, 회피 필수 (명조 Red Warning / 세키로 危) — 붉은 링
}
```

> 최소 요구는 `Parryable` / `Unblockable` 2분기다. `GuardableOnly`는 확장 여지로 둔다. 기존 동작 보존을 위해 **기본값은 `Parryable`**.

### 전파 경로

1. **`AbilityAttackInfo`** (스킬 단위)에 필드 추가 — 패링 가능 여부는 "텔레그래프되는 스윙 1개"의 속성이므로 스킬 단위가 자연스럽다.
   ```csharp
   [Header("Defense")]
   [Tooltip("이 공격에 대한 플레이어 방어 대응 분류. Danger Ring 색·패링 성립 여부를 결정한다.")]
   public AttackDefenseType defenseType = AttackDefenseType.Parryable;
   ```
2. **`AttackData`**(런타임, `CombatData.cs`)에 `public AttackDefenseType defenseType = AttackDefenseType.Parryable;` 추가.
3. **`EnemyCombat.CheckMeleeAttackHit`**의 `new AttackData { ... }` 빌드 시 `defenseType = _currentSkill.defenseType` 복사. (투사체 경로 `BaseProjectile`/`MotionEvent_SpawnProjectile`, `MonsterActor`의 AttackData 빌드 지점도 동일하게 채운다. 기본값이 `Parryable`이라 누락 시 현재 동작 유지.)
4. **`PlayerGuardState.OnAttackBlocked`**가 `incomingAttack.defenseType`을 검사:
   ```csharp
   if (isPerfectGuard && incomingAttack.defenseType == AttackDefenseType.Parryable)
   {
       monster?.AIController?.OnParried();
       _combat.OpenPerfectGuardCounterWindow();
   }
   else if (incomingAttack.defenseType == AttackDefenseType.Unblockable)
   {
       // 가드해도 막히지 않음 → 가드 브레이크/피격 처리로 분기 (별도 설계)
   }
   // 그 외: 일반 블록(카운터 없음)
   ```

### Danger Ring 색 매핑

`UI_DangerRing`은 생성 시 `AbilityAttackInfo.defenseType`(또는 `AttackData.defenseType`)을 받아 색을 정한다.

| `AttackDefenseType` | 링 색 | 의미 | 레퍼런스 |
|------|------|------|------|
| `Parryable` | 노랑 | "이 타이밍에 패링" | 명조 Weakness Halo |
| `GuardableOnly` | 노랑(카운터 글로우 없음) | "막을 수 있음" | — |
| `Unblockable` | 빨강 | "회피 필수" | 명조 Red Warning / 세키로 危 |

> **범위 주의:** `Unblockable`의 실제 전투 처리(가드 불가 → 가드 브레이크/관통 피격)는 이 문서 범위 밖의 별도 작업이다. 본 문서는 (a) 분류 축 추가, (b) 패링 성립 조건에 분류 반영, (c) Danger Ring 색 매핑까지를 다룬다.

---

## 표시 위치 (소켓)

`ActorType.cs`의 `ActorSocketType` enum에 `UI_HpBar` 옆에 추가:

```csharp
public enum ActorSocketType
{
    None = 0,
    LeftHand, RightHand,
    Center,
    Head,
    UI_HpBar,
    UI_DangerRing,   // 신규 — Danger Ring 앵커
    Weapon,
    GuardPosition,
}
```

- 링은 `actor.GetSocket(ActorSocketType.UI_DangerRing)`를 우선 사용하고, 소켓이 없으면 `머리 위 오프셋`으로 폴백한다(`UI_ActorHpBar._worldOffset`와 동일 패턴).
- HP Bar(`UI_HpBar`)와 겹치지 않도록 약간 더 높은 위치를 기본 오프셋으로 둔다.

---

## 생명주기 / 연동 지점

### `EnemyCombat` — `BeginTelegraph`를 디스패처로 재구조화

핵심 변경: 기존 `BeginTelegraph`의 `!useTelegraph` early-return을 제거하고, **바닥 FX와 링을 각자 플래그로 독립 분기**한다. 기존 `_telegraphInstances` 옆에 단일 링 참조(공격당 1개)를 둔다.

```csharp
private UI_DangerRing _dangerRing;

public void BeginTelegraph(int hitPhaseIndex, bool lockPositionOnStart)
{
    ClearTelegraphs();
    if (_currentSkill == null) return;

    int idx = GetClampedHitPhaseIndex(hitPhaseIndex);

    // 분기 1: 바닥 원형 FX — useTelegraph 일 때만 (기존 본문 그대로)
    if (_currentSkill.useTelegraph && _currentSkill.telegraphShape == TelegraphShape.Circle)
        BeginGroundTelegraph(idx, lockPositionOnStart);

    // 분기 2: Danger Ring — useDangerRing 일 때만 (텔레그래프와 무관)
    if (_currentSkill.useDangerRing)
    {
        float duration = ResolveDangerRingDuration(_currentSkill /*, 이벤트 구간*/);
        _dangerRing = UIManager.Instance.CreateDangerRing(_ownerActor, _currentSkill, duration);
    }
}

// UpdateTelegraphs(...) 안에서 (바닥 FX 추적과 별개로)
_dangerRing?.Tick(deltaTime);   // elapsed 누적 → fillAmount 갱신

// ClearTelegraphs(...) 안에서
if (_dangerRing != null) { _dangerRing.Release(); _dangerRing = null; }
```

- 이렇게 하면 `useTelegraph=false, useDangerRing=true`에서 바닥 FX 없이 링만 출력된다 ← **요구사항 충족**.
- `CreateDangerRing`에 넘긴 `_currentSkill`로 `defenseType` → 링 색(노랑/빨강)을 결정한다.
- `UpdateTelegraphs`는 `EnemyAttackState.UpdateState`에서 **스케일된 `deltaTime`** 으로 호출된다 → 히트스톱/타임스케일 시 링 채움도 같이 멈춘다(아래 "히트스톱" 참조). 별도 처리 불필요.

### `TelegraphEvent`(MotionEvent 경로)

`Execute`/`OnCompleteEvent`는 이미 `BeginTelegraph`/`ClearTelegraphs`를 호출하므로 **추가 배선이 거의 없다.** 이벤트 구간을 폴백 duration으로 넘기려면 `Execute`에서 `endTime − startTime`을 함께 전달하도록 시그니처를 확장한다(선택).

### `UIManager` / `UI_WorldSpaceHudLayer`

`CreateHpBar` 미러로 `CreateDangerRing` 추가. HP Bar와 달리 풀에서 꺼내고 `Release()`로 반납한다(데미지 플로터 풀 패턴 재사용).

```csharp
// UIManager
public UI_DangerRing CreateDangerRing(GameActor actor, AbilityAttackInfo skill, float duration)
    => _worldSpaceHudLayer?.CreateDangerRing(actor, skill, duration);
```

---

## UI 구현 (`UI_DangerRing`)

`UI_ActorHpBar`를 복제·축소한 신규 컴포넌트. `Assets/02.Scripts/UI/WorldSpace/UI_DangerRing.cs`.

| 책임 | 구현 |
|------|------|
| 위치 추적 | `LateUpdate`에서 소켓/오프셋 월드 위치 → `WorldToScreenPoint` → `anchoredPosition`. `UI_ActorHpBar.UpdatePosition` 그대로 |
| 카메라 뒤 처리 | `screenPos.z < 0` → `CanvasGroup.alpha = 0` (SetActive 토글 금지, HP Bar와 동일) |
| 채움 | `Tick(deltaTime)`로 `_elapsed` 누적 → `fillImage.fillAmount = Mathf.Clamp01(_elapsed / _duration)` |
| 색 (방어 타입) | `Init` 시 `defenseType`을 받아 베이스 색 결정 — `Parryable`=노랑, `Unblockable`=빨강 |
| 완성 강조 | `fillAmount >= 1` 직전/직후 1회 펄스(스케일/알파) + 채도/밝기 강조 |
| 정리 | `Release()`로 풀 반납(또는 즉시 Destroy). `_target == null`이면 자가 정리 |

- `fillImage`는 `Image.type = Filled`, `Radial360`. 12시 방향 시작, 시계방향 권장.
- 링 자체는 화면을 향하는 빌보드(스크린 스페이스 UI라 자동).

---

## 히트스톱 / 타임스케일

- 링 채움은 `EnemyCombat.UpdateTelegraphs` → `Tick(deltaTime)` 경로로만 진행되며, 이 `deltaTime`은 상태머신이 넘기는 **스케일된 시간**이다.
- 따라서 히트스톱(`GameHitStopManager`)·슬로우모션 발동 시 링 채움도 함께 정지/감속한다 → 연출과 타이밍 일관성 자동 확보.
- `LateUpdate`의 **위치 추적**은 `unscaledDeltaTime`과 무관(좌표 변환만 하므로) → 정지 중에도 화면 추적은 유지된다.

---

## 미해결 과제 / 범위 분리

| 항목 | Phase 1 처리 | 비고 |
|------|------|------|
| **다단 히트 공격** | 첫 번째 히트 페이즈 기준 링 1개만 표시 | `TelegraphEvent.hitPhaseIndex`가 여럿일 때 페이즈별 순차 링은 Phase 2. 1차는 "첫 위협 타이밍"만 |
| **오프스크린 적** | 링은 화면 밖이면 비표시(alpha 0). 방향 경고는 기존 `UIOffscreenThreatMarker`가 담당 | 두 시스템 **핸드오프**: 적이 화면 안이면 Danger Ring, 밖이면 오프스크린 마커. 연동은 Phase 2 |
| **동시 다수 적** | 표시 자체는 적별 1개 허용, **동시 표시 상한(예: 3개)** 두고 거리·위험도 우선 | 클러터 방지. 상한 초과 시 가장 가깝거나 곧 타격할 적 우선 |
| **일시정지** | `GamePause` 시 `Time.timeScale = 0` → `Tick`도 0 → 자연 정지 | 별도 처리 불필요 |
| **타이밍 자동 산출** | 명시 `dangerRingDuration` 사용 | MotionSet `Collision.startTime` 자동 조회는 미래 확장 |

---

## 구현 단계 (Phase 1)

**A. 방어 타입 구분 (패링 가능/불가)**
1. `AttackInfo.cs`에 `AttackDefenseType` enum 추가.
2. `AbilityAttackInfo`에 `defenseType`, `AttackData`에 `defenseType` 필드 추가(기본값 `Parryable`).
3. `EnemyCombat.CheckMeleeAttackHit`(및 기타 AttackData 빌드 지점)에서 `defenseType` 복사.
4. `PlayerGuardState.OnAttackBlocked`에서 `defenseType == Parryable`일 때만 `OnParried()` + 카운터 창. → 회귀 테스트: 기본값 `Parryable`이라 기존 동작 유지되는지 확인.

**B. Danger Ring (독립 출력 구조)**
5. `ActorSocketType.UI_DangerRing` enum 추가 + 적 프리팹에 머리 위 소켓 Transform 배치.
6. `AbilityAttackInfo`에 `useDangerRing` / `dangerRingDuration` / `dangerRingPrefabKey` 필드 추가.
7. `UI_DangerRing.cs` 작성(`UI_ActorHpBar` 복제 → `fillAmount` 채움 + `defenseType` 색 + 완성 강조).
8. Danger Ring 프리팹 생성(`Image` Filled/Radial360 + `CanvasGroup`) 및 `UIKeyType.DangerRing` 키 등록. *(에디터 작업 — 직접 수행 필요)*
9. `UI_WorldSpaceHudLayer.CreateDangerRing` + `UIManager.CreateDangerRing` 추가(풀링).
10. `EnemyCombat.BeginTelegraph`를 **디스패처로 재구조화**(early-return 제거 → 바닥 FX/링 독립 분기) + `Tick`/`Release` + `ResolveDangerRingDuration` 헬퍼.
11. 테스트: `useTelegraph=false, useDangerRing=true` 공격으로 **텔레그래프 없이 링만** 출력되는지 + "가득 참 == 타격" 정렬 + 패링/회피 색 구분 확인.

---

## 에디터 작업 (Unity 직접 수행)

코드는 모두 들어가 있으나, 아래 에셋 작업이 끝나야 실제로 링이 화면에 표시된다. **순서대로** 진행한다.

### 1단계 — Danger Ring 프리팹 생성

월드 HUD에 올라가는 UGUI 프리팹. HP Bar 프리팹(`ActorHpBar`)과 같은 계열이다.

| 항목 | 설정 |
|------|------|
| 루트 GameObject | `RectTransform` (pivot 0.5,0.5 / 크기 예: 96×96) + `CanvasGroup` + **`UI_DangerRing`** 컴포넌트 |
| 자식 `Image` (링 본체) | `Source Image` = 원형 링 스프라이트 / **`Image Type = Filled`** / **`Fill Method = Radial 360`** / `Fill Origin = Top` / `Clockwise = true` |
| `UI_DangerRing._fillImage` | 위 자식 `Image`를 드래그 연결 (**필수 — 미연결 시 채움 안 보임**) |
| (선택) `_worldOffset` | 기본 `(0, 2.2, 0)`. HP Bar와 안 겹치게 조정 |
| (선택) `_parryableColor` / `_unblockableColor` | 기본 노랑/빨강. 팔레트에 맞춰 조정 가능 |

> 색은 코드(`Init`)에서 `defenseType` 따라 `_fillImage.color`로 덮어쓴다. 프리팹의 Image 색은 초기값일 뿐이다.

### 2단계 — UIPrefabDatabase에 등록

기존 **`UIPrefabDatabase`** 에셋(`ActorHpBar`·`DamageFloater`가 등록된 그 에셋)을 연다. 인스펙터의 `Prefabs` 리스트에 항목 1개 추가:

| 필드 | 값 |
|------|------|
| `key` | **`DangerRing`** (정확히. 대소문자 일치 — 코드의 `DANGER_RING_KEY` 상수와 동일) |
| `prefab` | 1단계에서 만든 프리팹 |
| `defaultLayer` | 아무거나(예: `HUD`). **무관** — `CreateDangerRing`은 `ShowUI`를 안 쓰고 월드 HUD 레이어 밑에 직접 Instantiate한다 |
| `description` | 선택 (예: "몬스터 공격 타이밍 링") |

> `UIKeyType` enum 재생성(`UPlayGround/ID Enum Generator`)은 **선택**이다. 런타임 코드는 문자열 키 `"DangerRing"`를 쓰므로 enum 항목이 없어도 동작한다. 다른 곳에서 `UIKeyType.DangerRing`로 참조하고 싶을 때만 재생성한다.

### 3단계 — 적 프리팹에 소켓 배치 (선택)

링을 띄울 적 프리팹을 연다.

1. 머리/상단 위치에 빈 자식 GameObject 생성(예: 머리 본 밑 `Socket_DangerRing`). 머리 위로 적당히 올림.
2. 루트의 **`GameActor`** 컴포넌트 → `Socket Dict`(`_socketDict`)에 항목 추가: **Key = `UI_DangerRing`**, **Value = 위 Transform**.

> 소켓을 안 만들면 `_worldOffset`(머리 위 오프셋)으로 폴백하므로 **필수는 아니다.** 위치를 정밀 제어하고 싶을 때만 한다.

### 4단계 — 테스트 공격 데이터 설정

링을 띄울 적의 `AbilitySetSO` → `skills` 중 한 `AbilityAttackInfo`:

| 필드 | 테스트 값 |
|------|------|
| `useDangerRing` | **`true`** |
| `dangerRingDuration` | 윈드업 시작 → 실제 타격(`Collision`) 간격(초). 모션 보고 맞춤. (0 이하면 0.6초 폴백) |
| `defenseType` | `Parryable`(노랑) / `Unblockable`(빨강)로 번갈아 테스트 |
| `dangerRingPrefabKey` | **비워둠**(기본 `DangerRing` 프리팹 사용) |
| `useTelegraph` | **`false`로 둬도** 링만 단독 출력됨 — 독립 동작 확인용 |

### 5단계 — 검증 (스모크 테스트)

1. 위 적에게 접근해 공격을 맞아본다 → 머리 위에 링이 떠서 `0 → 1`로 차오르고, **가득 차는 순간이 타격 순간과 맞는지** 확인. 어긋나면 `dangerRingDuration` 조정.
2. `useTelegraph = false`인데 링이 뜨는지 확인(텔레그래프 독립).
3. `defenseType = Unblockable`로 바꾸면 **붉은 링** + 퍼펙트 가드해도 **카운터 창이 안 열리는지** 확인. `Parryable`이면 노란 링 + 카운터 정상.
4. 히트스톱/일시정지 중 링 채움이 함께 멈추는지 확인.

> ⚠️ 1·2단계(프리팹+DB 등록)를 안 하면 `CreateDangerRing`이 `null`을 반환해 **조용히 아무것도 안 뜬다**(에러 없음). 링이 안 보이면 먼저 DB의 `DangerRing` 키 등록과 `_fillImage` 연결부터 확인.

---

## 관련 파일

| 파일 | 역할 |
|------|------|
| `Assets/02.Scripts/Data/Combat/CombatData.cs` | `AbilityAttackInfo`에 Danger Ring 필드 + `defenseType` 추가, `AttackData`에 `defenseType` 추가 |
| `Assets/02.Scripts/Data/Enum/AttackInfo.cs` | `AttackDefenseType` enum 추가 |
| `Assets/02.Scripts/Data/Enum/ActorType.cs` | `ActorSocketType.UI_DangerRing` 추가 |
| `Assets/02.Scripts/GameActor/Component/Enemy/EnemyCombat.cs` | `BeginTelegraph` 디스패처 재구조화(독립 분기), `CheckMeleeAttackHit`에 `defenseType` 복사 |
| `Assets/02.Scripts/GameActor/State/Player/PlayerGuardState.cs` | `OnAttackBlocked`에서 `defenseType` 검사 — `Parryable`만 카운터 성립 |
| `Assets/02.Scripts/GameActor/State/Enemy/EnemyAttackState.cs` | 변경 없음(기존 `BeginTelegraph`/`UpdateTelegraphs`/`ClearTelegraphs` 호출 재사용) |
| `Assets/02.Scripts/Data/Event/Animation/MotionEvent_Telegraph.cs` | (선택) `Execute`에서 이벤트 구간 길이를 폴백 duration으로 전달 |
| `Assets/02.Scripts/UI/WorldSpace/UI_DangerRing.cs` | **신규.** 월드 추적 라디얼 필 링 컴포넌트 |
| `Assets/02.Scripts/UI/WorldSpace/UI_WorldSpaceHudLayer.cs` | `CreateDangerRing` + 풀 관리 추가 |
| `Assets/02.Scripts/Manager/UIManager.cs` | `CreateDangerRing` 진입점, `UIKeyType.DangerRing` 프리팹 등록 |
| `Assets/02.Scripts/UI/WorldSpace/UI_ActorHpBar.cs` | 복제 기준 패턴(위치 추적·fillAmount·카메라 뒤 처리) |
| `Assets/02.Scripts/UI/HUD/UIOffscreenThreatMarker.cs` | 오프스크린 핸드오프 대상(Phase 2) |

---

## 출처

- 명조 Weakness Halo(노란 동심원, 겹칠 때 패리) — Game8, "How to Parry and Counterattack": https://game8.co/games/Wuthering-Waves/archives/455880
- 명조 패리/카운터 가이드 — wuthering.gg: https://wuthering.gg/guide/fighting/counterattack
- 명조 전투/패리 타이밍 — GuildJen: https://guildjen.com/wuthering-waves-combat-guide/
- 명조 진동강도(Vibration Strength) 게이지(공격 예고 아님, 경직용) — Twinfinite: https://twinfinite.net/guides/wuthering-waves-vibration-meter-explained/
- 위험 공격 심볼 이중 부호화 — Sekiro: Shadows Die Twice (FromSoftware) 危 표기.
- 공격 예비/실행/회복 3단계 및 텔레그래프 반응시간 — Game Developer, GDKeys (게임 컴뱃 텔레그래프 일반론).
- 프로젝트 내부 참조 — `MONSTER_HEAVY_ATTACK_TELEGRAPH_GUIDE.md`, `OFFSCREEN_THREAT_INDICATOR_DESIGN.md`.
