# 모션 실행 키 탈-AnimKey 개선 스펙 (Motion Key Decoupling)

> 상태: SUPERSEDED — 2026-07-31 GAS Payload의 `MotionReferenceSO`를
> `(abilityId, variantId)` 기반 `AbilityMotionKey`로 교체했다.
> 실제 모션은 `ActorAnimationMotionSet.abilityMotions`가 소유하며,
> 이 문서의 MotionReference 설계와 기록은 이전 이력으로만 유지한다.
> 작성일: 2026-07-21
> 관련 문서: `GAMEPLAY_ABILITY_SYSTEM_SPEC.md`(docs/TODO), `ASMDEF_MODULARIZATION_PLAN.md`(docs/Complete)

### 구현 진행 기록

- 2026-07-21: `ActorAnimator.PlayMotion(MotionSetAsset)`과 공통 재생 시작 경로 추가.
- 2026-07-21: 재생/디버그 스냅샷에 `MotionSetAsset` 소스와 표시 키를 병기하고 직접 참조 복원을 지원.
- 2026-07-21: `ActorAnimationMotionSet`·`PlayerActorAnimationMotionSet`에 에셋 반환 조회 API 추가.
- 2026-07-21: 기본 모션과 무기별 오버라이드를 갖는 `MotionReferenceSO` 타입 추가.
- 2026-07-21: Payload/AttackInfo를 `MotionReferenceSO` 직접 참조로 전환하고 플레이어·몬스터 Ability 서브에셋까지 전수 조사.
- 2026-07-21: 의미 슬롯 130개를 `MotionTags.*`/`SerializedDictionary<GameplayTag, MotionSetAsset>`로 전환하고 상태머신·에디터 도구를 갱신.
- 2026-07-21: `AnimKey` enum, 런타임 오버로드, 구 딕셔너리, 레거시 Resolver를 제거. `Assets/02.Scripts`/`Assets/Tests` C# 참조 0건.
- 2026-07-21: 전체 Motion Payload 498개 중 489개에 MotionReference 에셋 생성·연결. 나머지 9개는 레거시 키에 해당하는 MotionSet이 원본 데이터에 없어 임의 매핑하지 않음(하단 검증 기록 참조).

### 최종 검증 기록 (2026-07-21)

- Unity Tundra 전체 스크립 빌드: 성공, C# 오류 0 (`1742 evaluated`).
- CLI 컴파일: `UPlayGround.Data`, `UPlayGround.Ability.UPlayGround`, `UPlayGround.Actor`, `UPlayGround.UI`, `UPlayGround.Camera`, `Assembly-CSharp`, `Assembly-CSharp-Editor` 오류 0.
- MotionReference: 489개 에셋, 연결된 Payload 중 빈 참조 0.
- 원본 매핑 부재 9개: dryad `Attack_1~3`, Training Dummy `Attack_1`, ElementalImbue `Dark/Fire/Light/Nature/Water`. 신규 모션 선정은 전투 연출 의사결정이 필요하므로 본 구조 마이그레이션에서 임의 대체하지 않음.

---

## 1. 배경과 문제 정의

### 1.1 현재 구조

모션 실행의 모든 경로가 전역 enum `AnimKey`(`Data/Enum/AnimKey.cs`)를 단일 식별자로 사용한다.

```text
[데이터]  UPlayGroundMotionAbilityPayloadSO.animKey : AnimKey
          AbilityAttackInfo.baseInfo.animKey        : AnimKey
          CombatData.victimForcedAnimKey             : AnimKey
          InteractableActorSO.interactionAnimKey     : AnimKey
          ComboRouteData / SpecialBreakAttackAsset   : AnimKey
                     │
[매핑]    ActorAnimationMotionSet
          └ SerializedDictionary<AnimKey, MotionSetAsset> (+ fallback 체인)
          PlayerActorAnimationMotionSet
          └ SerializedDictionary<WeaponType, ActorAnimationMotionSet>
                     │
[실행]    ActorAnimator.PlayMotion(AnimKey, fade, layer)
          → GetMotionSet(key) → MotionSet 타임라인 재생 (Animancer)
```

- `AnimKey`는 약 260개 항목, 수동 ID 대역 관리(100=Attack, 500=Skill, 700=Hit, 5000=Stop...).
- 참조 규모: **108개 파일, 790곳** (상태머신, Combat, Ability, 에디터 툴, 밸런스 툴 포함).

### 1.2 문제점

| # | 문제 | 증상 |
|---|------|------|
| P1 | **콘텐츠 추가 = 코드 수정** | 공격/스킬 하나 추가할 때마다 enum 항목 추가 → 재컴파일. `Attack_1~10`, `Skill_1~9`처럼 슬롯 소진 시 enum 확장 필요. 순수 데이터 작업이 코드 작업이 됨 |
| P2 | **ID 대역 수동 관리** | `= 100`, `= 500`, `= 6000` 등 대역을 사람이 기억·관리. 대역 충돌/오타 시 `SerializedDictionary<AnimKey,...>`가 int로 직렬화되어 있어 기존 에셋이 조용히 다른 모션을 가리키게 됨 (renumbering 사고) |
| P3 | **의미 없는 번호 네이밍** | `Attack_3`이 무슨 모션인지 enum만 봐서는 알 수 없음. 캐릭터마다 `Attack_3`의 실제 의미가 다름 |
| P4 | **역할 혼재** | 하나의 enum에 두 가지 성격이 섞여 있음 (아래 1.3) |
| P5 | **모듈 경계 오염** | `Ability.Core`는 프로젝트 비의존이지만 어댑터 payload가 `Data.EnumType.AnimKey`에 결박. 카메라 모듈처럼 이식 가능한 경계를 만들 수 없음 |
| P6 | **런타임 박싱** | Animancer 내부 상태 키로 enum을 쓰면 매 호출 boxing GC 발생 (Animancer 공식 문서가 명시한 비효율) |
| P7 | **런타임 확장 불가** | 새 모션을 빌드 이후(패치 데이터, 실험적 밸런스 에셋)에 추가할 수 없음. enum에 없는 모션은 존재할 수 없음 |

### 1.3 핵심 통찰: AnimKey는 두 가지 역할을 겸하고 있다

이 스펙의 설계 근거가 되는 분류다.

**역할 A — 콘텐츠 ID (데이터가 지정하는 임의 모션)**
`Attack_1~10`, `HeavyAttack_*`, `Skill_1~9`, `ChargeAttack_*`, `Player_SwapAttack_*`, `DashAttack_*`, `JumpAttack_*` 등.
- "어떤 모션인지"를 **데이터(AbilitySet/Payload)가 결정**한다. 코드가 특정 값을 알 필요가 없다.
- enum일 이유가 전혀 없다. → **에셋 직접 참조로 대체 가능** (약 60여 항목, 항목 수 증가의 주범).

**역할 B — 의미 슬롯 (코드/상태머신이 요구하는 계약)**
`Idle`, `Hit_F/B/L/R`, `Die`, `Guard`, `Stun`, `Knockdown`, `Getup`, `Walk/Run/Sprint` 방향 세트, `Move_Stop_*`, `Turn_*`, `Fly_*` 등.
- `PlayerHitState`가 "피격 방향에 맞는 Hit 모션"을 요청하듯, **코드가 의미를 알고 조회**한다.
- 액터마다 다른 에셋으로 채워지는 "슬롯"이므로 **식별자 체계 자체는 유지가 필요**하다. 다만 enum일 필요는 없다.

---

## 2. 웹 사례 조사

### 2.1 Animancer 공식 (본 프로젝트 애니메이션 백엔드)

- **Keys 문서**: 상태 딕셔너리 키로 enum 사용 시 boxing GC를 경고. 권장은 **AnimationClip/Transition 객체 자체를 키로 사용** — 즉 "무엇을 재생할지"를 식별자 조회가 아니라 **재생 대상 객체 직접 전달**로 해결.
- **Transition Assets**: 전환 설정을 ScriptableObject로 두고 여러 캐릭터가 공유 참조. "정의는 한 곳, 참조는 여러 곳" — 본 스펙의 에셋 직접 참조 방향과 일치.
- **Transition Libraries (v8)**: Transition Asset들을 라이브러리 에셋으로 묶고, "이전 상태 → 다음 상태" 조합별 페이드를 데이터로 오버라이드. 런타임 수정 가능. 코드 로직 없는 순수 데이터 매핑 계층의 선례.

### 2.2 Unreal GAS — "Ability가 Montage를 소유한다"

- UE ARPG 샘플·GAS 표준 패턴: GameplayAbility 에셋이 재생할 AnimMontage를 **직접 참조**하고 `PlayMontageAndWait` 태스크로 재생. 전역 "모션 enum"이 존재하지 않는다.
- 본 프로젝트의 `GameplayAbilitySO → Payload → AnimKey → 딕셔너리 → MotionSet` 경로에서 가운데 두 단계(AnimKey, 딕셔너리)를 제거하면 GAS와 같은 구조가 된다: **Payload가 MotionSetAsset을 직접 참조**.
- 몽타주 내 타이밍 이벤트는 AnimNotify → GameplayEvent(태그)로 어빌리티에 통지 — 본 프로젝트 MotionEvent 타임라인과 동일한 역할 분담이므로 이 경로는 변경 불필요.

### 2.3 Unreal FName / GameplayTag — 의미 슬롯의 식별자

- UE는 전역 enum 대신 **해시된 문자열(FName)** 과 **계층형 GameplayTag** (`Ability.Attack.Melee`)를 식별자로 사용. 등록제 + 해시 비교라 문자열 비용 없이 enum의 경직성을 회피.
- 본 프로젝트에는 이미 동일 컨셉의 인프라가 있다: `GameplayTag`(해시 struct) + `GameplayTagRegistrySO` + 코드젠(`GameplayTagsGenerated.cs`). **의미 슬롯을 태그로 옮기면 새 인프라 없이 FName 등가물을 얻는다.**

### 2.4 Unity 커뮤니티 — "enum 대신 ScriptableObject"

- enum+switch는 OCP 위반(추가할 때마다 코드 수정)이라는 지적과 함께, 타입/식별자를 ScriptableObject 에셋으로 두는 패턴이 표준 대안으로 정착 (Ryan Hipple 계열). 에셋이므로 rename에도 GUID 참조가 안전하고, 프로젝트 밖(패치/모드)에서 추가 가능.

**사례 종합**: 업계 공통 방향은 두 갈래다 —
1. 임의 콘텐츠 모션: **식별자 조회를 없애고 에셋을 직접 참조** (Animancer Transition Asset, GAS Montage).
2. 코드가 요구하는 공용 슬롯: **등록제 해시 식별자** (FName/GameplayTag) 또는 SO 키.

---

## 3. 설계 대안 비교

| 대안 | 내용 | 장점 | 단점 |
|------|------|------|------|
| A. 문자열 키 | `PlayMotion("Attack_Slash1")` | 확장 자유 | 오타 무검증, rename 지옥, 해시 안 하면 비교 비용. 단독 채택 부적합 |
| B. GameplayTag 전면 전환 | 모든 AnimKey를 `Motion.*` 태그로 | 기존 인프라 재활용, 계층 질의 가능, 등록제 검증 | 260개 전부 태그화하면 "enum이 태그로 바뀐 것"일 뿐 — 콘텐츠 ID(역할 A)의 조회 단계가 그대로 남음 |
| C. 에셋 직접 참조 전면 전환 | 모든 곳에서 `MotionSetAsset` 참조 | 조회 제거, 의존 최소 | 역할 B(Hit/Idle 등)는 액터·무기별로 다른 에셋이 필요 → 코드가 에셋을 직접 들 수 없음. 단독 채택 불가 |
| **D. 역할 분리 하이브리드 (권장)** | 역할 A → 에셋 직접 참조, 역할 B → GameplayTag 슬롯 | 두 문제를 각각 올바른 도구로 해결, 마이그레이션 단계 분리 가능 | 이중 체계 (단, 역할이 다르므로 자연스러움) |

### 무기별 변형 문제 (대안 C·D의 핵심 쟁점)

현재 `PlayerActorAnimationMotionSet.GetMotionSet(WeaponType, AnimKey)`가 같은 AnimKey를 무기별 다른 MotionSet으로 해석한다. Payload가 에셋을 직접 참조하면 이 간접층이 사라진다. 해결:

- 공격/스킬(역할 A)은 이미 `AbilitySetSO`가 캐릭터별로 분리되어 있고 Variant 선택 정책이 있으므로, **무기별 차이는 Variant 차원에서 흡수**하는 것이 원칙.
- 하나의 Payload가 무기별 모션을 가져야 하는 잔여 케이스를 위해 소형 간접 에셋 `MotionReferenceSO`(기본 MotionSetAsset + 무기별 오버라이드 목록)를 둔다. 전역 enum 없이 "필요한 곳에만" 간접층을 유지하는 방식.
- 로코모션·Hit 등 역할 B는 어차피 무기별 `ActorAnimationMotionSet` 딕셔너리 조회를 유지하므로 영향 없음.

---

## 4. 권장 설계 (대안 D)

### 4.1 목표 구조

```text
[역할 A: 콘텐츠 모션 — 조회 제거]
UPlayGroundMotionAbilityPayloadSO
  └ motionRef : MotionReferenceSO ─┐        (또는 MotionSetAsset 직접)
                                   ▼
ActorAnimator.PlayMotion(MotionSetAsset asset, fade, layer)   ← 신규 오버로드
  → 딕셔너리 조회 없이 asset.motionSet 재생

[역할 B: 의미 슬롯 — 태그 기반 슬롯 계약]
PlayerHitState 등
  └ PlayMotion(MotionTags.Hit_F)            ← GameplayTag (Motion.Hit.F)
ActorAnimationMotionSet
  └ SerializedDictionary<GameplayTag, MotionSetAsset>  (+ fallback 체인 유지)
```

### 4.2 신규/변경 타입

**`MotionReferenceSO`** (신규, `UPlayGround.Data`)
```csharp
[CreateAssetMenu(menuName = "UPlayGround/애니메이션/Motion Reference")]
public class MotionReferenceSO : ScriptableObject
{
    public MotionSetAsset defaultMotion;

    [Serializable] public struct WeaponOverride
    {
        public WeaponType weaponType;
        public MotionSetAsset motion;
    }
    public WeaponOverride[] weaponOverrides; // 비어 있으면 defaultMotion 고정

    public MotionSetAsset Resolve(WeaponType weaponType) { ... }
}
```
- CreateAssetMenu는 flat 도메인 규약(`UPlayGround/<Domain>/<Item>`)을 따른다.

**`ActorAnimator`** (변경)
```csharp
// 신규: 에셋 직접 재생. 내부 재생 로직은 기존 PlayMotion(AnimKey)과 공유.
public AnimancerState PlayMotion(MotionSetAsset asset, float fade = 0f, int layer = 0);

// 신규: 태그 슬롯 재생 (역할 B)
public AnimancerState PlayMotion(GameplayTag slot, float fade = 0f, int layer = 0);

// 기존: AnimKey 오버로드는 마이그레이션 기간 동안 유지 후 제거
```
- `_lastPlayedKey`, `MotionPlaybackSnapshot.Key`, `AnimationDebugSnapshot.Key` 등 추적 필드는 `AnimKey` 대신 **재생 소스 객체(MotionSetAsset)와 표시용 문자열**로 전환. 디버그 툴(ActorMonitor 등)은 문자열 표시로 충분하다.

**`UPlayGroundMotionAbilityPayloadSO`** (변경)
```csharp
public MotionReferenceSO motionRef;          // 신규 — 우선 사용
public AnimKey animKey = AnimKey.None;       // 폴백 — 마이그레이션 완료 후 제거
```
- `ResolveAnimKey()` → `ResolveMotion(WeaponType)`으로 대체. `UPlayGroundAbilityPayloadResolver`도 동일하게 에셋 반환 시그니처 추가.
- `AbilityAttackInfo.baseInfo.animKey` 폴백 경로도 같은 방식으로 이관.

**의미 슬롯 태그** (신규 등록, 기존 GameplayTag 레지스트리 사용)
```text
Motion.Locomotion.Idle / Walk / Run / Sprint / Walk.B_L45 ...
Motion.Hit.F / B / L / R
Motion.Death, Motion.Guard, Motion.Guard.Break, Motion.Stun
Motion.Knockdown, Motion.Knockdown.Getup, Motion.Grabbed ...
Motion.Fly.Start / Move / Landing / Attack / Idle
Motion.Stop.Walking / Running / ... , Motion.Turn.Idle.L45 ...
Motion.Interaction.Gathering / Mining.Ground / Fishing.Throw ...
```
- 코드젠(`GameplayTagsGenerated.cs`)으로 `MotionTags.Hit_F` 형태의 컴파일 타임 상수를 얻는다 → enum 수준의 타입 안전 + 등록제 확장성.
- 계층 구조 덕에 "Turn 계열 전부", "Locomotion 여부" 같은 질의(`IsMovementPlaybackKey` 대체)가 태그 매칭으로 자연스럽게 표현된다.

### 4.3 유지되는 것

- `MotionSetAsset` / `MotionSet` 타임라인, `MotionEventExecutor`, Loop/Freeze 이벤트 — 재생 계층은 변경 없음. 바뀌는 것은 "무엇을 재생할지 지정하는 방법"뿐.
- `ActorAnimationMotionSet`의 fallback 체인(공용 휴머노이드 모션) — 키 타입만 태그로 교체.
- `PlayerActorAnimationMotionSet`의 무기별 체계 — 역할 B 조회에서 유지.

---

## 5. 마이그레이션 계획

전면 교체는 790곳 동시 수정이라 불가능. **양방향 공존 → 경로별 전환 → 제거**의 3단계로 간다.

### Phase 1 — 실행 계층 준비 (코드만, 에셋 무변경)
1. `ActorAnimator.PlayMotion(MotionSetAsset)` 오버로드 추가. 기존 `PlayMotion(AnimKey)`은 내부적으로 조회 후 이 오버로드로 위임하도록 재구성.
2. 스냅샷/디버그 구조체의 키 필드를 소스 객체+문자열 기반으로 전환 (AnimKey 필드는 당분간 병기).
3. `MotionReferenceSO` 타입 추가.

### Phase 2 — 역할 A(콘텐츠 모션) 전환
1. Payload에 `motionRef` 필드 추가, `motionRef != null`이면 우선 사용·아니면 기존 animKey 폴백.
2. **일괄 마이그레이션 에디터 툴**: 전체 Payload(493개)를 순회하며 `animKey`를 각 캐릭터의 딕셔너리에서 해석 → 결과 MotionSetAsset으로 `MotionReferenceSO` 생성(무기별 결과가 갈리면 오버라이드로 bake) → `motionRef`에 할당. AbilityDataValidator에 "animKey/motionRef 불일치" 검사 추가.
3. `ComboRouteData`, `SpecialBreakAttackAsset`, `CombatData.victimForcedAnimKey`, `InteractableActorSO.interactionAnimKey` 등 데이터 내 AnimKey 필드를 같은 패턴으로 이관. (victimForcedAnimKey는 "피격자 쪽 모션"이므로 피격자 딕셔너리를 거쳐야 함 → 이 필드는 역할 B로 분류해 태그로 이관하는 것이 맞다.)
4. 콘텐츠 대역 enum 항목(`Attack_*`, `Skill_*` 등)을 `[Obsolete]` 마킹.

### Phase 3 — 역할 B(의미 슬롯) 전환
1. GameplayTag 레지스트리에 `Motion.*` 태그 등록 + 코드젠.
2. `ActorAnimationMotionSet`에 태그 딕셔너리 병기(신·구 공존), **에디터 변환 툴**로 기존 `SerializedDictionary<AnimKey,...>` → 태그 딕셔너리 자동 이전.
3. 상태머신(Player 23종 / Enemy 30종 / NPC 3종)의 `PlayMotion(AnimKey.X)` 호출을 `PlayMotion(MotionTags.X)`로 기계적 치환. 상태별 커밋을 잘게 나눠 진행.
4. 에디터 툴(MotionSetWindow, Combat/Balance 툴, 각종 Validator)의 AnimKey 의존을 태그/에셋 기반으로 갱신. **데이터 구조 변경 시 연관 커스텀 인스펙터 동기화 원칙 준수.**

### Phase 4 — 제거
1. `AnimKey` enum, AnimKey 오버로드, 구 딕셔너리 필드 삭제.
2. `Ability.UPlayGround` 어댑터에서 `Data.EnumType` 의존 제거 확인 → Ability 시스템 이식성 개선 완료.

### 검증 게이트 (각 Phase 공통)
- `dotnet build` asmdef별 컴파일 확인.
- EditMode 테스트(Ability 14종) 통과.
- Play Mode 수동 검증: 캐릭터별 기본 콤보/스킬/피격/사망/로코모션 + 무기 스왑 후 동일 확인.
- AbilityDataValidator·CombatDataValidator 전체 에셋 무경고.

---

## 6. 리스크와 완화

| 리스크 | 내용 | 완화 |
|--------|------|------|
| 직렬화 사고 | `SerializedDictionary<AnimKey,...>`는 int 직렬화 — 마이그레이션 툴 버그 시 조용한 모션 뒤바뀜 | 변환 툴은 **신규 필드에 기록만** 하고 구 필드를 지우지 않음. Validator로 신·구 해석 결과 diff 검사 후에만 구 필드 제거 |
| 무기별 변형 누락 | Payload 직접 참조로 전환하며 무기별 분기가 사라지는 케이스 | 마이그레이션 툴이 무기별 해석 결과가 갈리는 Payload를 리포트 → `MotionReferenceSO` 오버라이드로 bake |
| 에디터 툴 파손 | MotionSetWindow/밸런스 툴이 AnimKey를 축으로 동작 | Phase 3까지 AnimKey 병기 유지, 툴 전환을 별도 작업 항목으로 분리 |
| 태그 오타/미등록 | 태그는 enum보다 등록 검증이 느슨 | 코드는 반드시 코드젠 상수(`MotionTags.*`)만 사용, 원시 문자열 태그 생성 금지. Validator에 미등록 태그 검사 추가 |
| 범위 폭주 | 790곳 일괄 수정 유혹 | Phase 경계 엄수. Phase 2(데이터)만으로도 P1·P7(콘텐츠 추가 시 코드 수정)이 해소되므로 가치가 먼저 나온다 |

---

## 7. 성공 기준

- [x] 새 공격/스킬 모션 추가가 **코드 수정 없이** (MotionSetAsset + MotionReferenceSO + Payload 연결) 완료된다.
- [x] `AnimKey` enum이 삭제되고, `Ability.UPlayGround`가 `Data.EnumType.AnimKey`를 참조하지 않는다.
- [x] 상태머신 공용 모션은 `MotionTags.*` 코드젠 상수로 호출되어 컴파일 타임 안전성이 유지된다.
- [ ] 기존 전 캐릭터·전 몬스터의 모션 재생 결과가 마이그레이션 전과 동일하다 (Validator diff 0건).

---

## 8. 참고 자료 (웹 조사)

- Animancer — Keys (enum 키 boxing 경고): https://kybernetik.com.au/animancer/docs/manual/playing/keys
- Animancer — Transition Assets (에셋 공유 참조 패턴): https://kybernetik.com.au/animancer/docs/manual/transitions/assets/
- Animancer — Transition Libraries (데이터 주도 전환 매핑): https://kybernetik.com.au/animancer/docs/manual/transitions/libraries/
- Unreal ARPG 샘플 — Melee Abilities (Ability가 Montage 직접 소유): https://dq8iqaixvew1d.cloudfront.net/en-US/Resources/SampleGames/ARPG/GameplayAbilitiesinActionRPG/MeleeAbilitiesInARPG/index.html
- GAS Ability Tasks — PlayMontageAndWait: https://www.quodsoler.com/blog/from-wait-delays-to-play-montage-10-useful-gas-ability-tasks
- Unreal — FName (해시 문자열 식별자): https://dev.epicgames.com/documentation/en-us/unreal-engine/fname-in-unreal-engine
- GameplayTags/FName 심층 분석: https://itsbaffled.github.io/posts/UE/GameplayTags-And-FNames-In-Depth
- Unity 커뮤니티 — 확장 가능한 enum 대안(SO 키): https://discussions.unity.com/t/alternative-to-extensible-enum/952466
- enum+switch의 OCP 위반과 SO 대체 패턴: https://www.linkedin.com/pulse/replace-switch-scriptable-objects-rakib-jahan
