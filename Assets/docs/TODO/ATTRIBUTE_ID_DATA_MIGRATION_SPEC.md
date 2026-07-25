# Attribute(AttributeId) 데이터화 마이그레이션 스펙

> 작성일: 2026-07-25
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: TODO 구현 스펙 (구현 진행 중)
> 적용 범위: GAS Attribute 식별자 · 기본값 · 표시/포맷 메타데이터, AttributeSet 정의, Effect Modifier 저작, Core Attribute 런타임
> 관련 문서:
>
> - `Assets/docs/TODO/GAMEPLAY_TAG_DATA_MIGRATION_SPEC.md` — 자매 스펙(태그 데이터화). **단, 본 문서는 태그의 codegen/enum 방식을 채택하지 않는다(§3.2)**
> - `Assets/docs/guide/GAS_SYSTEM_GUIDE.html`
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_GAS_FULL_MIGRATION_SPEC.md`
> - `Assets/docs/Complete/GAMEPLAY_ABILITY_SYSTEM_SPEC.md`
>
> 참조 구현 (이미 데이터화 완료된 GameplayTag — 본 스펙이 이식할 원본, §1-A):
>
> - `Assets/02.Scripts/Data/Gameplay/GameplayTagRegistrySO.cs` — 정의 SO + `static GameplayTagRegistry`(Resources.Load 진입점, **코드 생성 없음**)
> - `Assets/02.Scripts/Data/Gameplay/GameplayTag.cs` — implicit 제거, `CreateRegistered`/`CreateCodeDefined` 값 타입
> - `Assets/02.Scripts/Data/Gameplay/GameplayTags.cs` — 손 작성 정적 코드 슬롯(`CreateCodeDefined`)
> - `Assets/02.Scripts/Gameplay/Tag/Editor/GameplayTagRegistryBuildValidator.cs` — 빌드 게이트 3중 검증기
> - `Assets/02.Scripts/Gameplay/Tag/Editor/GameplayTagReferenceTool.cs` — **사용처 검색 + 일괄 리네임**(한 파일에 검색·리네임·창 모두. 일반화 대상 → §4.8)
> - `Assets/02.Scripts/Data/Editor/Gameplay/GameplayTagPropertyDrawer.cs` — 드롭다운 드로어
>
> 관련 코드 (개선 대상):
>
> - `Assets/02.Scripts/Ability/Core/AttributeRuntimeTypes.cs` — `AttributeId` struct, `AttributeIds` 상수 클래스, `GameplayAttributeDefinition`, `AttributeSetDefinitionSO`
> - `Assets/02.Scripts/Ability/Core/AttributeSetRuntime.cs` — 런타임 등록/집계 (`Register(AttributeSetDefinitionSO)` / `Register(GameplayAttributeDefinition)`)
> - `Assets/02.Scripts/Data/Stat/UPlayGroundAttributeDefaults.cs` — 코드 하드코딩 기본값 + `All` 목록
> - `Assets/02.Scripts/Data/Stat/StatDisplayFormatter.cs` — 코드 하드코딩 표시명/값 포맷
> - `Assets/02.Scripts/GameActor/Gameplay/Ability/UPlayGroundAbilityOwnerPorts.cs` — `statId` 문자열 → `AttributeId` 왕복 지점
> - `Assets/02.Scripts/Ability/Core/AbilitySystemRuntime.cs` — `SetBase(new AttributeId(entry.attributeId), ...)` 문자열 경유 등록

## 구현 진행 기록 (2026-07-25)

- 완료: Phase 0 인벤토리. 코드 `AttributeIds.*` 참조 38개 파일과 직렬화 `_attributeId` 915개(고유 15개)를 확인했다.
- 완료: Phase 1~2의 레지스트리 기반. `Resources/AttributeRegistry.asset`에 27개 ID·stableId·기본값·표시·포맷·클램프 정책을 이관했고, `AttributeReference`, 런타임 인터닝 테이블, Core `IAttributeResolver` 포트를 추가했다.
- 완료: `UPlayGroundAttributeDefaults`와 `StatDisplayFormatter`의 Attribute if-chain을 레지스트리 조회로 교체하고, ASC 표준 Attribute 등록도 레지스트리 정의를 사용하도록 전환했다.
- 완료: 빌드 전 레지스트리/정적 슬롯/직렬화 참조 검증기, Attribute 드롭다운, 드라이런 우선 트랜잭션 마이그레이션 도구를 추가했다.
- 완료: 기존 GameplayTag 사용처/리네임 도구에 Attribute 도메인을 추가했다. Attribute 리네임은 구 ID를 `aliases`에 보존하고 메타데이터 내부 참조도 함께 갱신한다.
- 완료: 세이브 스키마를 v3으로 올리고 로드 시 alias→정규 ID 변환, 미등록 경고·기본값 유지 경로를 연결했다.
- 완료: Phase 2.5. `AttributeIds` 클래스와 프로젝트의 `AttributeIds.*` 참조를 제거했다. Core 표준 실행기는 Attribute ID를 생성자 주입받고, 프로젝트 직접 참조는 손 작성 `Attributes` 정적 슬롯을 사용한다.
- 완료: `AttributeSetRuntime`은 리졸버가 주입된 프로젝트 런타임에서 인터닝 `AttributeHandle`을 내부 Dictionary 키로 사용한다. Core 단독 테스트/외부 이식 환경은 리졸버가 없을 때 기존 `AttributeId` fallback 키를 유지한다.

---

## 0. 한 줄 요약

Attribute의 실체가 **코드에 상수로 박힌 `AttributeId` 문자열 + 코드 if-chain에 흩어진 메타데이터(기본값·표시명·포맷·클램프)**라서, 새 Attribute 추가·리네임·밸런싱이 전부 코드 수정이다.
Attribute를 **레지스트리를 단일 소스로 하는 데이터**로 승격하고, 메타데이터를 정의 옆에 데이터로 모은다.
**GameplayTag가 이미 완료한 무-enum·무-codegen 데이터화(§1-A)를 그대로 이식**한다 — 레지스트리에 항목을 추가/수정해도 **어떤 `.cs`도 재생성·재컴파일되지 않아야 한다**(핵심 제약). Core의 프로젝트 비의존성은 유지한다.

---

## 1. 목적

현재 GAS Attribute는 계층형 문자열(`"Combat.AttackPower"`)을 `AttributeIds` static 클래스가 코드 상수로 소유하고,
그 Attribute에 딸린 **기본값·표시명·포맷·클램프 규칙**이 서로 다른 코드 파일의 `if (id == AttributeIds.X)` 사슬에 흩어져 있다. 이 작업의 목적은:

1. Attribute를 **프로젝트 데이터(레지스트리)로 단일화**하여 코드 수정·**재컴파일 없이** 추가·삭제·리네임·밸런싱이 가능하게 한다. (enum·코드 생성 미도입이 이를 강제한다)
2. Attribute에 딸린 **메타데이터(기본값·표시명·포맷·단위·클램프 정책)를 정의 한 곳**에 모아, 흩어진 if-chain(`UPlayGroundAttributeDefaults`, `StatDisplayFormatter`)을 제거한다.
3. Attribute 식별자에 **안정적 stableId**를 부여해 리네임·삭제·Find References가 가능하게 한다.
4. 미등록 `AttributeId` 문자열(오타 `"Combat.AttckPower"`)을 저작/검증 단계에서 **드러나게** 한다.
5. Core(`UPlayGround.Ability.Core`)의 **프로젝트 비의존 경계를 깨지 않고** 위를 달성한다.

문자열을 완전히 없애는 것이 목적이 아니다. **직렬화 형태로서의 문자열(`GameplayAttributeDefinition._attributeId`)은 유지**하되, 그 문자열이 항상 레지스트리로 검증되고, 메타데이터가 코드가 아닌 데이터에서 나오게 만드는 것이 목적이다.

---

## 1-A. 참조 구현: 현행 GameplayTag 데이터화 구조 (이미 완료됨)

GameplayTag는 **이 스펙이 목표하는 무코드생성 데이터화가 이미 적용**되어 있다. Attribute는 이 검증된 패턴을 그대로 이식한다. 현행 구조(2026-07-25 기준 실제 코드):

| 요소 | 현행 GameplayTag 구현 | Attribute 대응 |
|---|---|---|
| 데이터 원본 | `GameplayTagRegistrySO` @ `Resources/GameplayTagRegistry.asset` (정의 = `tagName`/`description`/`color`, **enumName·constName 필드 삭제됨**) | `AttributeRegistrySO` @ `Resources/AttributeRegistry.asset` |
| 런타임 진입점 | `static GameplayTagRegistry` — `Resources.Load`로 SO 1회 로드·캐시, `IsRegistered`/`TryResolve`/`GetRequired`/`Definitions` 제공. **코드 생성 없음** | `static AttributeRegistry` (동형) |
| 값 타입 | `struct GameplayTag` — **`implicit operator(string)` 제거됨.** `CreateRegistered`(등록 검증, 미등록 throw) / `CreateCodeDefined`(정적 코드 슬롯용, 값만 생성) 2경로. `IsValid()` = 레지스트리 등록 여부 | Data 계층 `AttributeReference` (동형) |
| 정적 코드 슬롯 | **손으로 작성**한 `GameplayTags.cs`/`MotionTags.cs`의 `public static readonly GameplayTag State_Move = CreateCodeDefined("State.Move")`. 생성 파일 아님 | `Attributes.cs` (손 작성, `CreateCodeDefined`) |
| 빌드 검증기 | `GameplayTagRegistryBuildValidator : IPreprocessBuildWithReport` (+메뉴 툴) — ①레지스트리 무결성(정확히 1개·중복·공백) ②코드 슬롯 전수(리플렉션으로 `GameplayTags`/`MotionTags` 필드 → 미등록 throw) ③직렬화 자산 스캔(`01.Scenes`/`03.Prefabs`/`10.Datas`/`Resources`의 `.asset/.prefab/.unity`에서 `_tagName:` 라인 → 미등록 throw). 불일치 시 **빌드 차단** | `AttributeRegistryBuildValidator` (동형, `_attributeId:` 스캔) |
| 저작 | `GameplayTagPropertyDrawer` 드롭다운 | `AttributeReferencePropertyDrawer` |
| 사용처 검색 + 리네임 | `GameplayTagReferenceTool`(검색 3종+계층, 리네임 백업/롤백/BOM/충돌검증) | **별도 툴 신설 금지 — 공용 툴에 Attribute 도메인 추가**(§4.8) |
| Core 경계 | Core `AbilityTagId`는 **순수 문자열 유지**(implicit 포함). 레지스트리 검증은 **Data 계층 `GameplayTag`에만** 적용, Core는 비의존 | Core `AttributeId`도 순수 문자열 유지, 검증은 Data `AttributeReference`에 |

**핵심 교훈 3가지 (Attribute에 그대로 적용):**

1. **코드 생성·enum이 전혀 없다.** 태그 추가 = `Resources/GameplayTagRegistry.asset` 편집. 재컴파일 없음. 요구사항이 이미 여기서 충족되고 있으므로 Attribute도 동일하게 간다.
2. **정적 코드 참조는 손으로 쓴 `CreateCodeDefined` 슬롯 + 빌드 검증기로 안전화**한다. 즉 "코드가 지목하는 소수 태그"는 여전히 타입 있는 정적 필드로 두되, 빌드 검증기가 레지스트리와의 일치를 강제한다. → 본 스펙의 `WellKnownAttributes` 개념은 이 `GameplayTags.cs` 패턴으로 구체화한다.
3. **Core는 건드리지 않는다.** 레지스트리 검증은 Data 계층 값 타입(`GameplayTag`↔`AttributeReference`)에만 있고, Core(`AbilityTagId`↔`AttributeId`)는 순수 문자열 + 리졸버 포트로 남아 이식성을 지킨다.

---

## 2. 현재 상태와 문제

### 2.1 현재 Attribute 모델 — 식별자는 코드 상수, 메타데이터는 코드 if-chain

| 관심사 | 실체 | 위치 | 문제 |
|---|---|---|---|
| 식별자 정의 | `AttributeIds` static 클래스의 `new("Combat.AttackPower")` 상수 | `AttributeRuntimeTypes.cs:33~85` | 새 Attribute = 코드 추가·재컴파일 |
| 값 struct | `AttributeId` (단일 `string _value`) | `AttributeRuntimeTypes.cs:8~31` | `implicit operator (string)` 존재 → 오타 리터럴이 유효 취급 |
| 기본값 | `if (id == ...) return N;` 사슬 + `All[]` 배열 | `UPlayGroundAttributeDefaults.cs:8~45` | 밸런싱 = 코드 수정, 목록 이중 관리 |
| 표시명 | `if (id == ...) return "공격력";` 사슬 | `StatDisplayFormatter.cs:10~24` | 로컬라이즈·표기 변경 = 코드 수정 |
| 값 포맷/단위 | Defense/CritRate 등 특정 id 하드코딩 분기 | `StatDisplayFormatter.cs:28~66` | 퍼센트/플랫 판정이 코드에 매몰 |
| 클램프/Max 정책 | `GameplayAttributeDefinition` 필드 | `AttributeRuntimeTypes.cs:132~184` | **이미 데이터화 가능** — 하지만 정의를 코드에서 생성하는 경로가 병존 |

- **이미 존재하는 데이터 그릇**: `AttributeSetDefinitionSO`(`AttributeRuntimeTypes.cs:189`)와 `GameplayAttributeDefinition`은 클램프·Max 정책·기본값을 직렬화할 수 있다. `AttributeSetRuntime.Register(AttributeSetDefinitionSO)`(`AttributeSetRuntime.cs:136`)로 데이터 등록 경로도 있다.
- **문제는 단일 소스가 없다는 것**: 식별자의 정본은 코드(`AttributeIds`)이고, 기본값·표시명은 또 다른 코드 파일이며, 클램프 정책은 SO다. 세 소스가 서로를 강제하지 않는다.

### 2.2 위험 지점 (근거 포함)

- **A1 — 암묵적 string→AttributeId 변환.** `AttributeId`가 `implicit operator AttributeId(string)`(`AttributeRuntimeTypes.cs:30`)을 가진다. 오타 리터럴(`"Combat.AttckPower"`)이 **컴파일되고 유효한 AttributeId로 취급**되어, 절대 매칭되지 않는 유령 Attribute가 조용히 생긴다. 레지스트리 대조가 없다.
- **A2 — 메타데이터가 코드 if-chain에 흩어짐(R2의 Attribute판).** 하나의 Attribute를 온전히 추가하려면 최소 3곳(`AttributeIds` 상수, `UPlayGroundAttributeDefaults.All`+`Get`, `StatDisplayFormatter.GetDisplayName`+`FormatValue`)을 손대야 한다. 한 곳만 빠뜨리면 "이름은 있는데 기본값 0" / "값은 도는데 UI에 raw id 표기" 같은 조용한 결함이 난다(`StatDisplayFormatter.cs:25` 폴백이 이를 은폐).
- **A3 — 기본값·목록 이중 관리.** `UPlayGroundAttributeDefaults`가 `All[]`(어떤 Attribute가 있는가)과 `Get()`(각 기본값)을 **따로** 유지한다(`UPlayGroundAttributeDefaults.cs:8~45`). 둘이 어긋나면 등록은 되나 기본값이 0, 혹은 그 반대.
- **A4 — 자산 정체성 부재.** Attribute가 에셋/레지스트리 항목이 아니므로 stableId·Find References·안전한 리네임/삭제가 없다. `"Combat.AttackPower"`를 바꾸면 SO/코드/세이브에 박힌 문자열이 **경고 없이 무효화**된다(`saveBaseValue` Attribute는 세이브 호환까지 깨질 수 있음).
- **A5 — 문자열 경유 등록.** 세이브/스탯 파이프라인이 `new AttributeId(entry.attributeId)`(`AbilitySystemRuntime.cs:148`)와 `new AttributeId(statId)`(`UPlayGroundAbilityOwnerPorts.cs:97`)로 검증 없이 문자열을 AttributeId로 승격한다. 미등록 문자열이 런타임 진입점에서 걸러지지 않는다.
- **A6 — Core ↔ 데이터 단절.** Core의 `AttributeId`는 비의존 원칙상 프로젝트 레지스트리를 참조할 수 없어 **순수 문자열**이다. 즉 레지스트리 검증이 Core 계층까지 도달하지 못한다(태그 스펙 R5와 동형).

### 2.3 이미 존재하는 자산 (재사용)

- `AttributeSetDefinitionSO` + `GameplayAttributeDefinition` — 기본값·클램프·Max 정책·메타 여부를 직렬화하는 정의 그릇이 **이미 있다.**
- `AttributeSetRuntime.Register(AttributeSetDefinitionSO)` — SO에서 런타임 등록하는 경로가 **이미 있다.**
- `GameplayTagRegistrySO` / `GameplayTagRegistryEditorWindow` — 레지스트리 SO + 코드 생성기 패턴이 **선례로 있다.** 이 구조를 Attribute용으로 복제한다.

즉 **인프라 대부분이 있고**, 빠진 것은 (a) 식별자·메타데이터의 단일 소스 레지스트리, (b) 코드 상수/if-chain을 레지스트리에서 생성·조회하도록 전환, (c) 미등록 문자열 차단, (d) Core 경계 리졸버다.

---

## 3. 목표 모델

### 3.1 원칙

1. **레지스트리 = 단일 소스.** 모든 Attribute(식별자 + 기본값 + 표시명 + 포맷 + 클램프 정책)는 레지스트리(`AttributeRegistrySO`)에 등록되어야만 존재한다.
2. **enum 없음 · 코드 생성 없음 · 데이터 추가 ≠ 재컴파일 (핵심).** GameplayTag는 `GameplayTagId` **enum + 코드 생성기**를 유지하지만, 본 스펙은 그 방식을 **채택하지 않는다.** enum/생성 상수 파일은 레지스트리에 항목을 추가할 때마다 파일이 바뀌어 **재컴파일을 유발**하기 때문이다. Attribute는 오직 데이터(레지스트리 SO 편집)만으로 추가·삭제·수정되며, **어떤 코드도 재생성/재컴파일되지 않아야 한다.**
3. **직렬화는 문자열, 런타임은 핸들.** 정의/에셋/세이브에는 지금처럼 `attributeId` 문자열을 저장(하위호환)하되, 로드/초기화 시 레지스트리로 **resolve → 인터닝된 정수 `AttributeHandle`**로 승격한다. hot path 비교/집계는 정수로 한다. 인터닝 테이블은 **런타임에 레지스트리를 읽어 구성**한다(생성 코드 아님).
4. **메타데이터는 정의 옆에.** 기본값·표시명·값 포맷(퍼센트/플랫)·단위·클램프·Max 정책을 레지스트리 항목 한 곳에 둔다. `UPlayGroundAttributeDefaults`/`StatDisplayFormatter`의 if-chain은 **레지스트리 조회로 대체**한다.
5. **검증은 Data 계층 값 타입에.** GameplayTag가 `AbilityTagId`(Core)는 순수 문자열로 두고 `GameplayTag`(Data)에만 레지스트리 검증을 넣은 것처럼, **Core `AttributeId`는 그대로 두고** Data 계층 `AttributeReference`에 `CreateRegistered`/`CreateCodeDefined` + 미등록 throw를 둔다. 자유 문자열 유입은 Data 경계와 빌드 검증기에서 차단한다.
6. **Core 비의존 유지.** Core는 `AttributeId`(문자열) + 주입된 `IAttributeResolver` 포트만 안다. 레지스트리·`Resources.Load`는 Data/어댑터 쪽에 있고 Core로 새지 않는다.
7. **저작·참조는 드롭다운/직렬화 필드.** Effect Modifier·AttributeSet·성장 SO의 Attribute 필드는 자유 입력이 아니라 레지스트리 기반 드롭다운 드로어로 편집하고, 코드가 하드코딩 상수로 특정 Attribute를 지목하던 자리는 직렬화된 `AttributeReference`나 런타임 resolve로 옮긴다.

### 3.2 설계 선택지 비교

| 옵션 | 개요 | 장점 | 단점 | 판정 |
|---|---|---|---|---|
| A. 코드 상수 유지 + 검증만 추가 | `AttributeIds`/if-chain 유지, 등록 검증 패스만 | 최소 변경 | 메타데이터 산개(A2)·이중 관리(A3)·Core 단절(A6) 미해결, **데이터 추가마다 코드 수정** | 부족 |
| B. Attribute 1개 = SO 에셋 | Attribute마다 ScriptableObject | GUID·Find References·풍부한 메타 | 에셋 증가, Core가 SO 참조 불가, `CreateAssetMenu` flat 도메인 규약과 충돌 | 기각 |
| C-gen. 레지스트리 + **생성된 enum/상수** + 핸들 (구 태그 스펙 문서가 제안했던 방식) | 레지스트리에서 `AttributeIds` 상수/enum을 코드 생성 | 호출부 컴파일 참조 무변경, 타입 안전 | **레지스트리에 항목 추가 = 상수 파일 재생성 = 재컴파일.** "데이터 추가 = 무재컴파일" 원칙 위반 | **기각** |
| **C-data. 레지스트리 단일 소스 + 런타임 인터닝 핸들 + 직렬화 참조 + 리졸버 (코드 생성 없음)** | 레지스트리는 SO, 참조는 직렬화 `AttributeReference`/`CreateCodeDefined` 정적 슬롯, 런타임은 핸들, 경계는 리졸버. **코드 생성·enum 없음** | A1~A6 해소 + **데이터 추가 시 무재컴파일**, Core 비의존·직렬화 하위호환 유지 | 하드코딩 상수 호출부(39파일)를 직렬화 참조/정적 슬롯로 이전하는 리팩터 필요 | **채택** |

> **현행 GameplayTag가 이미 C-data다.** 태그 스펙 *문서*(`GAMEPLAY_TAG_DATA_MIGRATION_SPEC.md`)는 `GameplayTagId` enum + 코드 생성 유지를 전제로 쓰였지만, **실제 태그 코드는 그 방식을 버리고 enum·codegen 없는 데이터화(C-data)로 이미 이행 완료**됐다(`GameplayTagId` enum은 코드에서 소멸, §1-A). 따라서 Attribute는 태그와 갈라서는 게 아니라 **태그가 이미 도달한 지점을 그대로 따라간다.** 대가는 동일 — `AttributeIds.Combat.AttackPower`처럼 컴파일 상수를 직접 참조하는 39개 파일을 직렬화 `AttributeReference` 또는 `Attributes.cs` 정적 슬롯으로 이전(§4.4). 이 이전이 이 스펙의 실제 작업량 대부분이다.
>
> GUID 안정성은 레지스트리 항목별 `stableId` + 리네임 별칭 테이블로 확보한다.

---

## 4. 대상 타입 설계 (옵션 C-data)

### 4.1 레지스트리 (`AttributeRegistrySO`)

`GameplayTagRegistrySO`를 본떠, 항목은 다음을 갖는다:

```csharp
[Serializable]
public class AttributeRegistryEntry
{
    public string attributeId;      // "Combat.AttackPower" — 직렬화/계층 키
    public string stableId;         // 불변 식별자(최초 부여 후 고정), 리네임해도 참조 유지
    public List<string> aliases;    // 과거 attributeId 보존 → 기존 직렬화 resolve
    public string displayName;      // "공격력" (StatDisplayFormatter 대체)
    public string category;         // Vital/Combat/Movement/Resource/Life/Meta (분류/드로어 그룹핑용, 코드 생성 아님)

    // 메타데이터 (기존 GameplayAttributeDefinition 흡수)
    public float defaultBaseValue;                  // UPlayGroundAttributeDefaults.Get 대체
    public AttributeValueFormat format;             // Flat / Percent01 (FormatValue 대체)
    public string unit;                             // 선택: "%", "m" 등
    public AttributeClampPolicy clampPolicy;
    public float fixedMinimum, fixedMaximum;
    public string minimumAttributeId, maximumAttributeId, dependentResourceId;
    public AttributeMaxChangePolicy maxChangePolicy;
    public bool saveBaseValue;
    public bool isMetaAttribute;
}
```

- 기존 `GameplayAttributeDefinition`의 정책 필드를 이 항목이 그대로 흡수한다. `AttributeSetDefinitionSO`는 "어떤 Attribute를 그 액터가 갖는가"의 **구성 목록**으로 남기고, **각 Attribute의 정본 메타데이터는 레지스트리**가 소유하도록 역할을 분리한다.
- `AttributeValueFormat` enum(`Flat`/`Percent01`)이 `StatDisplayFormatter`의 Defense/CritRate 하드코딩 분기를 대체한다.

**런타임 진입점 — `GameplayTagRegistry`와 동형(코드 생성 없음):**

```csharp
// Resources/AttributeRegistry.asset을 런타임·에디터가 함께 읽는다. 생성 코드 아님.
public static class AttributeRegistry
{
    // Resources.Load<AttributeRegistrySO>("AttributeRegistry") 1회 로드·캐시
    public static IReadOnlyList<AttributeRegistryEntry> Definitions { get; }
    public static bool IsRegistered(string attributeId);
    public static bool TryResolve(string attributeId, out AttributeReference reference);
    public static AttributeReference GetRequired(string attributeId); // 미등록 throw
#if UNITY_EDITOR
    internal static void SetEditorRegistry(AttributeRegistrySO r); // OnValidate에서 주입
#endif
}
```

- 레지스트리 SO는 `OnValidate`에서 조회 캐시(`HashSet<string>`)를 `RebuildLookup`하고 `SetEditorRegistry(this)`로 에디터 인스턴스를 연결한다(GameplayTag와 동일).
- 레지스트리 자산은 **정확히 1개**여야 하며(빌드 검증기가 강제), 새 Attribute 추가는 이 자산 편집만으로 끝난다 → 재컴파일 없음.

### 4.2 런타임 인터닝 핸들 (코드 생성·enum 없음)

**코드 생성기도, enum도 만들지 않는다.** 대신 레지스트리를 **런타임에 한 번 읽어** 인터닝 테이블을 구성한다. 레지스트리에 항목을 추가/삭제해도 **재생성되는 소스 파일이 없으므로 재컴파일이 없다.**

```csharp
public readonly struct AttributeHandle : IEquatable<AttributeHandle>
{
    public int Index { get; }         // 인터닝 테이블 인덱스 (런타임 구성)
    public bool IsValid => Index > 0; // 0 = Invalid
    // 비교/해시 int 기준 — 0-alloc
}

// 런타임 인터닝기 — 레지스트리 SO에서 구성. 생성 코드 아님.
public sealed class AttributeInternTable
{
    // stableId/attributeId/alias → Index, 그리고 부모 Index 테이블을 레지스트리 로드 시 1회 구성
    public bool TryResolve(string attributeIdOrAlias, out AttributeHandle handle);
    public int GetParent(AttributeHandle handle);   // 계층 판정 O(depth), 0-alloc
}
```

- 인덱스는 레지스트리 로드 순서(또는 stableId 정렬)로 런타임에 부여한다. **에셋/세이브에 인덱스를 저장하지 않는다**(인덱스는 세션 내부 값, 직렬화 키는 항상 문자열 `attributeId`/`stableId`).
- 부모 인덱스 테이블도 런타임에 계산해 계층 판정을 0-alloc으로 만든다.

### 4.3 Core 리졸버 포트

```csharp
// Ability.Core — 비의존
public interface IAttributeResolver
{
    bool TryResolve(string attributeId, out int handle);
    float GetDefaultBaseValue(int handle);
    bool TryGetMetadata(int handle, out AttributeMetadata meta); // 표시명/포맷/클램프
}
```

- `AttributeSetRuntime`는 문자열 대신 리졸버가 준 핸들로 등록·집계·조회한다.
- UPlayGround 어댑터가 레지스트리 기반 구현을 주입한다(`AbilitySystemComponent` 초기화 시점). **Core→Data 참조 없음.**

### 4.4 하드코딩 상수 참조 이전 (본 스펙의 실작업 대부분)

enum·생성 상수를 두지 않으므로, 현재 `AttributeIds.Combat.AttackPower`처럼 **컴파일 상수를 직접 참조**하는 39개 파일을 이전해야 한다. 참조를 성격에 따라 둘로 나눈다.

- **콘텐츠 참조 (데이터에서 지목) → 직렬화 `AttributeReference` 필드.** Effect Modifier, AttributeSet 구성, 성장/스케일 SO 등 "어떤 Attribute를 대상으로 하는가"를 데이터가 고르는 자리는 코드 상수를 없애고 직렬화 필드로 만든다. **이 자리는 새 Attribute를 추가해도 코드가 전혀 바뀌지 않는다.** GameplayTag의 직렬화 값 타입(`GameplayTag`)과 동형:

```csharp
[Serializable]
public struct AttributeReference   // GameplayTag 대응. 인스펙터 드롭다운 드로어로 편집
{
    [SerializeField] string _attributeId;             // 직렬화 키는 문자열
    // implicit operator(string) 없음 — CreateRegistered / CreateCodeDefined 경유만
    // 런타임에 리졸버로 AttributeHandle 캐시 (인덱스는 저장 안 함)
}
```

- **엔진 임계 참조 (코드가 하드 의존) → `CreateCodeDefined` 정적 슬롯.** 데미지 파이프라인이 `Vital.Health`를, Poise 시스템이 `Vital.Poise`를 다루는 것처럼 **C# 로직이 특정 Attribute에 구조적으로 의존**하는 소수 자리는, 현행 `GameplayTags.cs` 패턴을 그대로 이식한다 — **손으로 작성한** 정적 슬롯 파일 `Attributes.cs`. 코드 생성이 아니며, 빌드 검증기가 레지스트리와의 일치를 강제한다.

```csharp
// 현행 GameplayTags.cs와 동형. 손 작성, 생성 코드 아님.
// 새 '콘텐츠' Attribute 추가는 이 파일을 건드리지 않는다 → 무재컴파일.
// 코드가 '새로' 특정 Attribute에 하드 의존할 때만(=그 자체가 코드 변경) 슬롯이 는다.
// 각 슬롯의 등록 여부는 AttributeRegistryBuildValidator가 전수 검사.
public static class Attributes
{
    public static readonly AttributeReference Health    = AttributeReference.CreateCodeDefined("Vital.Health");
    public static readonly AttributeReference MaxHealth = AttributeReference.CreateCodeDefined("Vital.MaxHealth");
    public static readonly AttributeReference Poise     = AttributeReference.CreateCodeDefined("Vital.Poise");
    // ... 데미지/경직 등 엔진이 직접 다루는 최소 집합만
}
```

> 구분 기준: **"이 Attribute가 없으면 특정 C# 시스템이 동작 자체를 못 하나?"** 그렇다면 엔진 임계(`Attributes.cs` 정적 슬롯), 아니면 콘텐츠(직렬화 `AttributeReference`). 대부분의 39개 참조는 콘텐츠 쪽이며, 엔진 임계는 소수다. 핵심은 **콘텐츠 Attribute를 아무리 추가해도 `Attributes.cs`도 다른 어떤 코드도 바뀌지 않는다**는 것. (이 구분·검증 구조가 현행 GameplayTag에서 이미 돌고 있다 → §1-A.)

### 4.5 조회 어댑터 (코드 if-chain 대체)

- `UPlayGroundAttributeDefaults.Get/All` → 레지스트리 조회로 대체. **`All`은 레지스트리 항목 열거로 대체**되므로, 새 Attribute를 넣어도 이 코드는 바뀌지 않는다(현재는 배열에 손으로 추가해야 함 = A3).
- `StatDisplayFormatter.GetDisplayName/FormatValue/FormatModifier` → 레지스트리의 `displayName`/`format`/`unit`을 읽어 포맷. 코드 상수 비교 제거.

### 4.6 저작 드로어

- Effect Modifier·AttributeSet·성장 SO의 `AttributeReference` 필드에 **레지스트리 드롭다운 드로어**를 붙여 자유 입력을 막는다(현행 `GameplayTagPropertyDrawer` 대응).
- 미등록 값이 이미 직렬화돼 있으면 **경고 배지 + "레지스트리에 추가" 퀵픽스**를 노출한다.

### 4.7 빌드 검증기 (`AttributeRegistryBuildValidator`)

현행 `GameplayTagRegistryBuildValidator`를 그대로 이식한다. `IPreprocessBuildWithReport`(+메뉴 툴)로, 빌드 전 3중 검사에 실패하면 `BuildFailedException`으로 **빌드를 차단**한다:

1. **레지스트리 무결성**: `AttributeRegistrySO`가 **정확히 1개**, `attributeId`/`stableId` 공백·중복 없음, 계층 부모·`min/max/dependent` 참조 실재.
2. **정적 코드 슬롯 전수**: 리플렉션으로 `Attributes.cs`의 `public static AttributeReference` 필드를 모두 읽어, `CreateCodeDefined` 값이 레지스트리에 등록됐는지 확인(미등록 시 파일·필드명과 함께 오류).
3. **직렬화 자산 스캔**: `01.Scenes`/`03.Prefabs`/`10.Datas`/`Resources`의 `.asset`/`.prefab`/`.unity`에서 `_attributeId:` 라인을 읽어 미등록 값을 파일:라인과 함께 보고. (태그의 `_tagName:` 스캔과 동형)

> 이 검증기가 있으면 §4.4의 "직렬화 참조 + 정적 슬롯" 구조가 리네임/삭제에도 안전해진다. GameplayTag에서 이미 이 3중 검증이 빌드 게이트로 작동 중이므로, Attribute도 동일 게이트를 갖추는 것이 정합적이다.

### 4.8 사용처 검색 + 일괄 리네임 도구 (공용 툴로 일반화 — 도메인별 분리 금지)

리네임과 사용처 검색은 이 데이터화가 실제로 안전해지는 핵심 도구다. **이미 `GameplayTagReferenceTool.cs`에 완성된 구현이 있으므로, Attribute용 별도 툴을 새로 만들지 않는다.** 대신 그 툴을 **도메인 서술자(descriptor)로 파라미터화한 공용 툴**로 일반화하고, 태그·Attribute(및 향후 심볼)를 같은 툴이 처리하게 확장한다.

#### 4.8.1 현행 태그 툴이 이미 하는 것 (재사용 대상)

- **사용처 검색** (`GameplayTagReferenceSearch.Find`): 3종 소스를 한 번에 스캔 — ①레지스트리 정의(`- tagName:`) ②직렬화 자산(`.asset/.prefab/.unity`의 `_tagName:`) ③코드(`.cs`의 문자열 리터럴). `includeDescendants`로 **계층(하위 태그) 포함** 검색. 결과는 소스종류/경로/라인/프리뷰로 정렬.
- **일괄 리네임** (`GameplayTagRenameService.Rename`): 계층 suffix 보존 `BuildRenameMap`(`A.B`→`X` 시 `A.B.C`→`X.C`), 충돌 검증(미변경 태그와 결과 충돌·결과끼리 중복 차단), **전 대상 파일 byte[] 백업 → 실패 시 전량 복구**, `StartAssetEditing`/`DisallowAutoRefresh`, UTF-8 **BOM 보존**, 코드·직렬화·레지스트리 정의 동시 치환.
- **창 UI** (`GameplayTagReferenceWindow` + `GameplayTagRenameWindow`): 메뉴 `UPlayGround/게임플레이/게임플레이 태그/태그 사용처 검색`, 드롭다운 선택·하위포함 토글·라인 열기·이름변경 진입.

#### 4.8.2 일반화 설계 — "하나의 툴, 여러 도메인"

도메인마다 다른 것은 **문자열 몇 개와 계층 규칙뿐**이다. 이를 서술자로 뽑아 검색·리네임 엔진과 창을 **도메인 무관**하게 만든다.

```csharp
// 검색/리네임 엔진이 도메인에 대해 알아야 하는 전부. 태그/Attribute가 각각 인스턴스를 제공.
public interface ISymbolDomain
{
    string DisplayName { get; }                 // "GameplayTag" / "Attribute"
    string RegistryAssetPath { get; }           // Resources/GameplayTagRegistry.asset / .../AttributeRegistry.asset
    string SerializedValueKey { get; }          // "_tagName:" / "_attributeId:"
    string RegistryDefinitionKey { get; }       // "- tagName:" / "- attributeId:"
    Regex NameValidation { get; }               // 이름 형식(둘 다 dot 계층 → 사실상 공유 가능)
    IReadOnlyList<string> AllNames { get; }      // 레지스트리에서 열거(드롭다운·검증용)
    bool IsHierarchical { get; }                 // dot 계층 여부(둘 다 true)
    // 코드 리터럴 탐지·정적 슬롯 컨테이너 타입 등 도메인별 훅
}
```

- 기존 `GameplayTagReferenceSearch`/`GameplayTagRenameService`의 **로직 본문을 그대로** `SymbolReferenceSearch`/`SymbolRenameService`로 옮기고, 하드코딩된 `"_tagName:"`·`RegistryPath`·계층 prefix 계산을 `ISymbolDomain`에서 읽게 바꾼다.
- 창은 **도메인 선택 드롭다운**(GameplayTag / Attribute)을 툴바에 추가한 단일 `SymbolReferenceWindow`. 도메인 전환 = 서술자 교체뿐, 검색/리네임 코드 경로는 공유.
- 태그 전용 진입 메뉴는 **호환용 얇은 래퍼**(선택한 도메인=Tag로 공용 창 열기)로 남겨 기존 사용성을 깨지 않는다.

#### 4.8.3 Attribute 추가 = 서술자 1개

Attribute 지원은 **`AttributeSymbolDomain : ISymbolDomain`** 하나를 추가하고 도메인 목록에 등록하면 끝난다. 검색·리네임·백업/롤백·BOM·충돌검증 로직은 **한 줄도 복제하지 않는다.**

```csharp
sealed class AttributeSymbolDomain : ISymbolDomain
{
    public string RegistryAssetPath => "Assets/Resources/AttributeRegistry.asset";
    public string SerializedValueKey => "_attributeId:";
    public string RegistryDefinitionKey => "- attributeId:";
    // 계층/이름검증은 태그와 동일 규칙 → 공용 기본값 재사용
    // AllNames는 AttributeRegistry.Definitions에서 열거
}
```

#### 4.8.4 리네임과 `aliases`/세이브의 연동 (도메인 훅)

Attribute 리네임은 태그와 **한 가지가 다르다**: `saveBaseValue` Attribute는 세이브 키이므로(§5-A.4), 리네임 시 구 이름을 **자동으로 `aliases`에 추가**해야 세이브 하위호환이 유지된다. 이를 `ISymbolDomain`의 리네임 후처리 훅으로 표현한다(태그는 no-op, Attribute는 `aliases.Add(old)`). 즉 공용 엔진은 그대로 두고 **도메인별 차이는 훅으로만** 흡수한다.

---

## 5. 마이그레이션 단계

각 단계는 독립적으로 컴파일/검증 가능해야 한다.

### Phase 0 — 인벤토리 (조사)
- 프로젝트 전체에서 (a) `AttributeIds.*` 컴파일 참조(현 39개 파일), (b) `new AttributeId("...")` / implicit 문자열 변환 호출부, (c) `UPlayGroundAttributeDefaults`·`StatDisplayFormatter`의 if-chain 항목을 전수 수집한다.
- 세 소스(`AttributeIds` / defaults / formatter) 간 **누락·불일치 항목**을 실측한다(A2/A3 근거).

### Phase 1 — 레지스트리 단일 소스화
- `AttributeRegistrySO` + `AttributeRegistryEntry` 신설. 현 `AttributeIds` 25개 항목을 이관하고 각 항목에 `defaultBaseValue`(defaults에서), `displayName`/`format`(formatter에서), 클램프 정책(기존 `GameplayAttributeDefinition`에서)을 채운다.
- 각 항목에 `stableId` 발급(마이그레이션 1회), `aliases`에 기존 `attributeId` 보존.
- **에디터 검증 패스**: 프로젝트 내 모든 Attribute 문자열이 레지스트리에 존재하는지 검사·보고(빌드 게이트 후보).

### Phase 1.5 — 런타임 진입점 + 빌드 검증기 (GameplayTag 패턴 이식)
- `Resources/AttributeRegistry.asset` + `static AttributeRegistry`(Resources.Load·캐시·`IsRegistered`/`TryResolve`/`GetRequired`) 구축. `OnValidate` → `RebuildLookup`/`SetEditorRegistry` (현행 `GameplayTagRegistrySO` 그대로).
- **`AttributeRegistryBuildValidator` 이식**(§4.7): 레지스트리 무결성 + `Attributes.cs` 정적 슬롯 전수 + `_attributeId:` 자산 스캔. 이 게이트가 Phase 2.5의 참조 이전을 안전하게 받쳐준다.

### Phase 2 — 런타임 인터닝 + 조회 어댑터 (코드 생성 없음)
- 레지스트리를 런타임에 읽어 `AttributeInternTable`(문자열/별칭→Index + 부모 Index) 구성. **생성 소스 파일 없음** → 이후 레지스트리 편집이 재컴파일을 유발하지 않는다.
- `UPlayGroundAttributeDefaults.Get/All`, `StatDisplayFormatter`를 레지스트리 조회로 내부 교체(if-chain 제거).

### Phase 2.5 — 하드코딩 상수 참조 이전 (§4.4, 작업량 핵심)
- 39개 파일의 `AttributeIds.*` 참조를 콘텐츠(직렬화 `AttributeReference`) / 엔진 임계(`Attributes.cs`의 `CreateCodeDefined` 정적 슬롯) 로 분류·이전.
- 이전이 끝나면 **콘텐츠 Attribute 추가는 코드 변경 0**이 됨을 이 단계의 완료 기준으로 삼는다.
- 이전 기간에는 기존 `AttributeIds` 상수를 `[Obsolete]`로 남겨 잔존 참조를 컴파일 경고로 색출(이전 완료 후 제거).

### Phase 3 — Core 리졸버 경계
- `IAttributeResolver` 도입, `AttributeSetRuntime`/집계를 핸들 기반으로 전환(외부 API는 `AttributeId` 오버로드 유지).
- **A5 제거**: `AbilitySystemRuntime.cs:148`·`UPlayGroundAbilityOwnerPorts.cs:97`의 문자열 승격을 리졸버 경유 검증으로 대체(미등록 문자열 즉시 실패).

### Phase 4 — 자유 문자열 유입 차단 (핵심 리스크 제거)
- Data 계층 `AttributeReference`는 `implicit operator (string)` 없이 `CreateRegistered`/`CreateCodeDefined`만 노출(현행 `GameplayTag`와 동일). Core `AttributeId`는 이식성 위해 그대로 둔다.
- 미등록 문자열은 `CreateRegistered` throw + 빌드 검증기에서 드러난다.

### Phase 5 — 저작 드로어 & 공용 사용처/리네임 툴 (§4.8)
- `AttributeReference` 드롭다운 드로어(`GameplayTagPropertyDrawer` 대응), 미등록 경고/퀵픽스.
- **`GameplayTagReferenceTool`을 `ISymbolDomain` 기반 공용 툴로 일반화**(태그 로직 본문 이동, 하드코딩 키 → 서술자). 태그 메뉴는 호환 래퍼로 존치.
- **`AttributeSymbolDomain` 1개 추가**로 Attribute 사용처 검색/일괄 리네임 확보. 리네임 후처리 훅으로 구 이름을 `aliases`에 자동 추가(세이브 하위호환). 안전 삭제(참조·`saveBaseValue` 세이브 참조 존재 시 차단).

### Phase 6 — 검증 & 테스트
- 아래 §6.

---

## 5-A. 실 데이터 마이그레이션 (에셋·세이브·1회성 변환)

§5의 Phase는 **코드/구조 전환 순서**다. 그러나 이 작업은 코드만 바꾸면 끝나지 않는다. 이미 직렬화된 자산이 세 종류 존재하며, 각각을 **자동 변환**하지 않으면 데이터가 조용히 깨진다. 본 절은 그 실 변환 절차를 규정한다.

### 5-A.1 변환 대상 자산 (전수)

| 대상 | 어디에 무엇이 박혀 있나 | 리스크 |
|---|---|---|
| **AttributeSet 정의 SO** | `GameplayAttributeDefinition._attributeId` 문자열, 클램프/Max 정책 필드 | 정본이 레지스트리로 이동하면서 SO는 구성 목록으로 슬림화 → 필드 이관 필요 |
| **Effect SO의 Modifier** | Modifier가 참조하는 `attributeId` 문자열 | 미등록/오타 문자열이 섞여 있을 수 있음(A1) |
| **세이브 파일** | `saveBaseValue` Attribute의 `attributeId` 문자열 키 | 리네임 시 구 키를 못 찾으면 **플레이어 진행 손실** |

> Phase 0에서 이 3종을 **먼저 스캔·인벤토리**한다. 리네임이 필요한 항목이 하나라도 있으면 세이브 마이그레이션(5-A.4)이 필수 경로가 된다.

### 5-A.2 1회성 마이그레이션 툴 (에디터)

`GameplayTagRegistryEditorWindow`와 형제인 **에디터 전용 1회성 변환 툴**을 만든다. 이 툴은 프로젝트 규약(`CLAUDE.md`의 "Editor 데이터 도구 안전 규칙")을 **그대로** 따른다:

1. **식별 순서**: 기존 에셋/항목 매칭은 **GUID 정확 일치 → path 정확 일치 → (둘 다 없을 때만) 이름** 순. `attributeId` 문자열은 GUID/path가 아니므로, 리네임 매핑은 반드시 명시적 `구 attributeId → stableId` 테이블로만 처리하고 유사 이름 폴백을 두지 않는다. 동일 후보가 복수면 **모호성 오류로 중단**.
2. **transaction 롤백**: 변환 전체를 하나의 `Undo` group으로 묶고, 중간에 예외가 나면 `Undo.RevertAllDownToGroup`으로 **전량 롤백** 후 저장한다. 일부만 적용된 상태를 성공처럼 collapse하지 않는다.
3. **백업/복구**: 에셋을 덮어쓰기 전에 대상 SO를 임시 스테이징에 백업하고, 실패 시 복구한다(P09 빌더가 완전한 transaction이 아니라는 선례를 반복하지 않는다).
4. **드라이런 우선**: 실제 쓰기 전에 "무엇을 어떻게 바꿀지" diff 리포트를 먼저 출력하고, 사용자가 확인한 뒤에만 적용한다. 미등록 문자열은 자동 매핑하지 말고 **미해결로 보고**한다.

### 5-A.3 에셋 변환 절차 (드라이런 → 적용)

1. **스캔**: 모든 `AttributeSetDefinitionSO`·`GameplayEffectSO`를 순회해 등장하는 `attributeId` 문자열을 수집.
2. **대조**: 각 문자열을 레지스트리(및 `aliases`)로 resolve. 성공/리네임(별칭 히트)/미등록으로 3분류.
3. **리포트**: 분류 결과와 예정 변경(구 문자열 → 정규 `attributeId`/stableId)을 diff로 출력. 미등록은 **차단 목록**으로 별도 표기.
4. **적용**: 미등록 0건일 때만 적용 허용. 정의 SO의 정책 필드(클램프/Max/기본값)를 레지스트리로 이관하고, SO에는 참조 키만 남긴다. 전 과정 단일 Undo group + 백업.
5. **검증**: 적용 후 재스캔으로 미등록 0건, 모든 참조가 유효 stableId를 가리키는지 재확인.

### 5-A.4 세이브 마이그레이션 (버전 게이트)

`saveBaseValue` Attribute의 키를 리네임한 경우, 기존 세이브가 구 `attributeId`를 들고 있으므로 **로드 시점 변환**이 필요하다.

- 세이브 스키마에 **버전 번호**를 두고, 로드 시 현재 버전 미만이면 마이그레이션 패스를 실행한다.
- 변환은 레지스트리 `aliases`를 사용한다: 세이브의 구 키를 `aliases`로 resolve → 현재 정규 `attributeId`로 치환 후 로드.
- `aliases`로도 resolve 실패한 키는 **경고 로그 + 기본값 폴백**(정의 `defaultBaseValue`)으로 처리하되, 조용히 삭제하지 않는다.
- 마이그레이션 후 최신 스키마로 재저장(선택: 원본 세이브 백업 보존).

### 5-A.5 데이터 마이그레이션 검증

- **왕복 무손실**: 마이그레이션 전 세이브 → 로드 → 재저장 후, 모든 `saveBaseValue` Attribute 값이 보존됨.
- **리네임 시나리오**: `attributeId`를 바꾸고 `aliases`에 구 값을 넣은 뒤, 구 세이브·구 에셋이 모두 정상 resolve됨.
- **롤백 안전**: 변환 도중 강제 예외를 주입해도 에셋이 변환 전 상태로 완전히 복구됨(부분 적용 0).
- **미등록 차단**: 미등록 문자열이 포함된 에셋은 적용이 거부되고 미해결 목록에 정확히 보고됨.

---

## 6. 검증 · 테스트

- **레지스트리 무결성**: `attributeId`/`stableId` 유일성, 계층 부모 존재성, 별칭 충돌 없음, `min/max/dependent` 참조 Attribute 실재성.
- **무재컴파일 (핵심 요구사항 검증)**: 레지스트리에 콘텐츠 Attribute를 추가/삭제/리네임해도 **생성/변경되는 `.cs`가 없어** 컴파일 산출물(asmdef DLL) 해시가 불변임을 확인. 새 Attribute가 데이터만으로 런타임에 등록·표시·Effect 적용까지 도달하는 end-to-end 시나리오 테스트.
- **소스 일치**: Phase 1 이관 후 레지스트리 기본값·표시명·포맷이 기존 `UPlayGroundAttributeDefaults`/`StatDisplayFormatter` 결과와 **전 항목 동일**함을 파라미터라이즈드 테스트로 대조(회귀 방지).
- **참조 무결성 (빌드 검증기)**: `AttributeRegistryBuildValidator`가 레지스트리 1개·무중복, `Attributes.cs` 정적 슬롯 전수 등록, 자산 `_attributeId:` 미등록 0건을 빌드 전에 강제(현행 GameplayTag 검증기와 동일 커버리지). 메뉴 툴로도 수동 실행 가능.
- **하위호환**: 기존 직렬화(문자열)·세이브 로드 → 핸들 resolve 성공. 리네임 후 별칭 경유 resolve 성공. `saveBaseValue` Attribute 세이브 왕복 무손실.
- **왕복 제거**: 등록/집계 hot path 문자열 할당 0 확인(할당 어서션).
- **Core 비의존**: `UPlayGround.Ability.Core` asmdef가 레지스트리/Data를 참조하지 않음(asmdef 경계 검사).
- **공용 사용처/리네임 툴(§4.8)**: 도메인=Attribute로 사용처 검색이 레지스트리·직렬화·코드 3종을 모두 잡고, 계층 하위 포함이 동작. 일괄 리네임이 (a)계층 suffix 보존 (b)충돌 차단 (c)실패 시 전량 롤백(백업 복구) (d)BOM 보존 (e)구 이름 `aliases` 자동 추가를 만족. **같은 툴로 도메인=Tag 리그레션이 깨지지 않음**(공용화 후 태그 회귀 테스트).
- 기존 `AttributeSetRuntimeTests` / `AbilitySystemRuntimeTests`가 리졸버 경유로도 통과.

---

## 7. 하위호환 · 리스크

- **직렬화 형태 유지 필수.** `GameplayAttributeDefinition._attributeId`(문자열)와 세이브에 박힌 Attribute 문자열은 이미 다수 SO/세이브에 직렬화돼 있다. 문자열 필드는 **그대로 두고** 비직렬화 핸들을 병행한다. 필드 제거/이름 변경 금지.
- **세이브 데이터 특별 주의.** `saveBaseValue = true` Attribute(예: `Vital.MaxHealth`류)는 세이브 파일이 `attributeId` 문자열을 키로 쓴다. 리네임 시 반드시 `aliases`로 구 문자열을 보존해 기존 세이브가 깨지지 않게 한다.
- **asmdef 경계.** Core는 리졸버 포트만 안다. 레지스트리 참조가 Core로 새어들면 이식성 붕괴 → 경계 테스트로 방어.
- **enum/생성 상수 미도입 (현행 GameplayTag와 동일).** 본 스펙은 `GameplayTagId` 같은 enum도, `AttributeIds` codegen도 만들지 않는다(태그가 이미 그렇게 이행 완료). 이유는 요구사항 — **데이터 추가 시 재컴파일 금지.** 기존 `AttributeIds` 상수는 이전 기간에만 `[Obsolete]`로 존치하고, 참조 이전 완료 후 제거한다.
- **엔진 임계 키 목록(`WellKnownAttributes`)은 예외적 코드.** 이건 codegen이 아니라 손으로 유지하는 소수 문자열 상수다. 늘어나는 경우는 오직 **C# 코드가 새로 특정 Attribute에 하드 의존할 때**뿐이며, 그 자체가 이미 코드 변경이므로 "데이터 추가 = 무재컴파일" 원칙과 충돌하지 않는다.
- **`AttributeSetDefinitionSO` 역할 재정의.** 기존 SO가 메타데이터까지 들고 있던 경우, 정본은 레지스트리로 옮기고 SO는 "구성 목록"으로 슬림화한다. 이관 중 두 소스가 다르면 레지스트리를 우선하되 diff를 보고한다.

---

## 8. 비목표 (Non-Goals)

- Attribute를 Attribute당 하나의 ScriptableObject 에셋으로 만드는 것(옵션 B) — 기각.
- **enum·코드 생성 도입 — 하지 않는다.** 데이터 추가가 재컴파일을 유발하는 어떤 구조도 도입하지 않는다(현행 GameplayTag와 동일 방침).
- 한 번의 커밋으로 전체 전환 — 반드시 Phase 단위로 나눈다.
- Attribute **수치 밸런싱 자체**의 변경 — 본 작업은 저장 위치를 코드→데이터로 옮길 뿐, 값은 보존한다.

---

## 9. 연관 개선 (교차 참조)

- **GameplayTag 데이터화** — 태그는 이미 C-data로 이행 완료(§1-A)이므로, 본 스펙은 새 인프라를 설계한다기보다 **완성된 태그 인프라를 Attribute로 이식**하는 작업에 가깝다. 재사용 대상: 레지스트리 SO + `Resources.Load` 진입점, 값 타입의 `CreateRegistered`/`CreateCodeDefined`, 정적 슬롯 파일, 빌드 검증기, 드롭다운 드로어. 공통 베이스로 추출(제네릭 레지스트리/검증기)하면 두 시스템의 중복을 줄일 수 있다.
- **태그 스펙 문서 정합성** — `GAMEPLAY_TAG_DATA_MIGRATION_SPEC.md`는 enum/codegen 유지 전제로 작성돼 현행 코드와 어긋난다. 별도로 그 문서를 현행 구현(C-data)에 맞춰 갱신할 것을 권고(본 스펙 범위 밖).
- **Effect 저작 노출** — Attribute가 데이터화되면 `GameplayEffectSO` Modifier의 Attribute 선택이 드롭다운으로 안전해지고, 표시명/포맷이 저작 UI에 바로 뜬다.
