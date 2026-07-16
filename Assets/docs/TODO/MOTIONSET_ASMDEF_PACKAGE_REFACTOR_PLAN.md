# MotionEvent/MotionSet asmdef 패키지 분리 리팩터 계획

> 작성일: 2026-07-16
> 대상 버전: Unity 6 (6000.0.60f1), URP
> 분류: 리팩터 계획(미실행). MotionEvent 타임라인 프레임워크를 asmdef 기반 embedded package로 분리해 타 프로젝트에서 재사용 가능하게 한다.
> 관련 문서: [PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md](PROJECT_SYSTEM_IMPROVEMENT_EXECUTION_PLAN.md) Phase 7(asmdef 도입 순서), [CODE_STRUCTURE_IMPROVEMENT_ROADMAP.md](CODE_STRUCTURE_IMPROVEMENT_ROADMAP.md) §7

---

## 0. 개요

2026-07-16 기준 소스 전수 조사 결과를 바탕으로 한 분리 계획이다.

핵심 결론:

- **분리 가능**: 데이터 모델(`MotionEventBase`, `Motion`, `MotionSet`, `MotionSetAsset`), 실행기(`MotionEventExecutor`, 결합 제거 후), 범용 에디터(`MotionSetEditor`/`MotionSetDrawer`/`MotionEventAddPopup` 등 7파일, 하드코딩 역전 후).
- **게임 잔류**: 구체 이벤트 ~28종(Manager/Components/State/Combat 결합), `ActorAnimationMotionSet` 계열(게임 enum + AYellowpaper), `MotionSetWindow` 파티얼 8개(Animancer+KCC+게임 컴포넌트).
- **직렬화 무손상 경로 존재**: Phase 1(프레임워크만 이동)은 SerializeReference 데이터 마이그레이션 리스크가 0이다. 구체 이벤트 이동은 `[MovedFrom]`이 필요한 별도 단계로 분리한다.
- 선행 리팩터 2건: ① `MotionEventExecutor`의 GameActor/워프 기즈모 결합 제거, ② `MotionEventStyle`·`MotionEventAddPopup`의 구체 타입 하드코딩(23 + 28건)을 어트리뷰트 기반으로 역전.

### 목표 / 비목표

| 구분 | 내용 |
|------|------|
| 목표 | 타임라인 프레임워크 + 범용 에디터를 `Packages/com.uplayground.motionset`로 분리, 타 프로젝트에서 git URL로 사용 |
| 목표 | 게임 측 구체 이벤트는 Assembly-CSharp에 남긴 채 패키지 에디터/실행기가 자동 인식 (SerializeReference + TypeCache) |
| 비목표 | `MotionSetWindow` 프리뷰 툴 포팅 (Phase 4 선택 과제 — 확장 탭 API 재설계 필요) |
| 비목표 | 프로젝트 전체 asmdef 전환 (실행 계획 Phase 7 담당) |

---

## 1. 현황과 지배 제약

### 1.1 asmdef 현황

| 항목 | 상태 |
|------|------|
| 자체 코드 asmdef | `UPlayGround.Core` 1개. 나머지 `02.Scripts` 전체가 Assembly-CSharp |
| Animancer Pro V8 | `Packages/com.kybernetik.animancer` — `Kybernetik.Animancer` asmdef **있음** → 패키지에서 참조 가능 |
| KCC | `Assets/ExternalAssets/Etc/KinematicCharacterController` — asmdef **없음** (Assembly-CSharp 소속) → asmdef 코드에서 참조 **불가** |
| AYellowpaper SerializedCollections | asmdef 없음 (Assembly-CSharp 소속) → asmdef 코드에서 참조 **불가** |
| KINEMATION MotionWarping | asmdef 있음 (본 계획과 무관) |

### 1.2 지배 제약

1. **asmdef 어셈블리는 Assembly-CSharp를 참조할 수 없다.** 패키지로 나가는 코드는 남는 게임 코드(그리고 asmdef 없는 플러그인)를 단 한 줄도 참조하면 안 된다.
2. **partial 클래스는 어셈블리 경계를 넘을 수 없다.** `MotionSetWindow`의 게임 전용 파티얼(CombatOverlay 등)이 있는 한 윈도우 본체도 함께 잔류해야 한다.
3. **`[SerializeReference]` managed reference는 YAML에 `{class, namespace, assembly}` 3요소를 기록한다.** 구체 이벤트 클래스의 소속 어셈블리가 바뀌면 기존 MotionSetAsset 데이터가 깨진다 (§4).

### 1.3 관련 소스 분포

```
Assets/02.Scripts/
├── Data/Event/Animation/            ← MotionEventBase + 구체 이벤트 ~28종 (+SlashVFXPresetSO)
├── Data/Actor/Animation/            ← Motion, MotionSet(Motion.cs), MotionSetAsset,
│   │                                   ActorAnimationMotionSet 계열, WeaponMotionMappingConfig
│   └── Editor/                      ← MotionSetEditor/Drawer/AddPopup/Style/OffsetFieldUtil/
│                                       PresetLibrarySO/AssetEditor + MotionSetWindow 파티얼 8개
│                                       + LocoMotion/WeaponMotion 셋업 윈도우, MotionTestRegistry
└── GameActor/Animation/             ← MotionEventExecutor (+ 파일 하단 워프 기즈모 섹션)
```

---

## 2. 패키지 구조안

Embedded package로 시작해 검증 후 git URL 배포로 전환한다.

```
Packages/com.uplayground.motionset/
├── package.json                     (name: com.uplayground.motionset)
├── Runtime/
│   ├── UPlayGround.MotionSet.asmdef              (autoReferenced: true)
│   ├── MotionEvent.cs               ← MotionEventBase
│   ├── Motion.cs                    ← Motion, MotionSet
│   ├── MotionSetAsset.cs
│   ├── MotionEventExecutor.cs       ← 결합 제거 버전 (§5.1)
│   ├── IMotionEventTargetRoot.cs    ← 신규 인터페이스 (§5.1)
│   ├── MotionEventMetaAttribute.cs  ← 신규 어트리뷰트 (§5.2)
│   └── Events/                      ← Phase 3: LoopEvent 등 범용 이벤트 4종
└── Editor/
    ├── UPlayGround.MotionSet.Editor.asmdef       (includePlatforms: Editor,
    │                                              references: UPlayGround.MotionSet)
    ├── MotionSetEditor.cs
    ├── MotionSetDrawer.cs
    ├── MotionSetAssetEditor.cs
    ├── MotionEventAddPopup.cs       ← 카탈로그 역전 버전 (§5.2)
    ├── MotionEventStyle.cs          ← 스타일 역전 버전 (§5.2)
    ├── MotionEventOffsetFieldUtil.cs
    └── MotionEventPresetLibrarySO.cs
```

**규칙:**

- 기존 네임스페이스(`UPlayGround.Data.Event`, `UPlayGround.Animation`, `UPlayGround.Animation.Editor`)는 **변경하지 않는다**. 게임 측 using 파급과 SerializeReference 네임스페이스 기록을 모두 회피한다. asmdef `rootNamespace`는 비워둔다.
- Runtime asmdef는 `autoReferenced: true` — Assembly-CSharp(게임 코드)가 자동 참조한다. 역방향 참조는 구조적으로 불가능하므로 이것이 경계 검증기 역할을 한다.
- 파일 이동 시 **`.meta`를 반드시 함께 이동**한다 (ScriptableObject GUID 보존 — 씬/프리팹/에셋 참조 유지).

---

## 3. 파일별 경계 판정

### 3.1 그대로 이동 (데이터 마이그레이션 리스크 0)

| 파일 | 클래스 | 근거 |
|------|--------|------|
| `Data/Event/Animation/MotionEvent.cs` | `MotionEventBase` | 추상 클래스 — SerializeReference YAML에 구체 타입만 기록되므로 이동 무해 |
| `Data/Actor/Animation/Motion.cs` | `Motion`, `MotionSet` | 플레인 `[Serializable]` — 인라인(값) 직렬화라 어셈블리명 미기록 |
| `Data/Actor/Animation/MotionSetAsset.cs` | `MotionSetAsset` | ScriptableObject — .meta 동반 이동 시 GUID 유지 |
| `Editor/MotionSetEditor.cs` | | 의존: UnityEditor + `UPlayGround.Data.Event`뿐 (확인 완료) |
| `Editor/MotionSetDrawer.cs` | | 상동 |
| `Editor/MotionSetAssetEditor.cs` | | 상동 |
| `Editor/MotionEventOffsetFieldUtil.cs` | | 상동 |
| `Editor/MotionEventPresetLibrarySO.cs` | | 상동. 기존 프리셋 에셋은 .meta 이동으로 GUID 유지 |

### 3.2 리팩터 후 이동

| 파일 | 결합 지점 | 조치 |
|------|-----------|------|
| `GameActor/Animation/MotionEventExecutor.cs` | ① `GetComponentInParent<GameActor>()` 타깃 해석 ② 파일 하단(345행~) `UPlayGround.Debugging` 워프 기즈모 섹션(`ActorMovementController.MotionWarp` 사용) | §5.1 |
| `Editor/MotionEventStyle.cs` | 구체 이벤트 23종 `typeof` 체인 하드코딩 | §5.2 |
| `Editor/MotionEventAddPopup.cs` | `Meta<구체타입>` 카탈로그 28건 하드코딩 | §5.2 |

### 3.3 게임 잔류

| 대상 | 잔류 사유 |
|------|-----------|
| 구체 이벤트 ~24종 — `BeginCollisionEvent`, `DisableCollisionEvent`, `BeginParticleEvent`, `CameraEffectEvent`, `CameraLookAtSocketEvent`, `PlaySoundEvent`, `FootstepEvent`, `AddForceEvent`, `TimeScaleEvent`, `ComboWindowEvent`, `CancelWindowEvent`, `SlashVFXEvent`, `SpawnProjectileEvent`, `SpawnSkillEvent`, `HealSkillEvent`, `FinishAttackEvent`, `SpecialBreakAttackEvent`, `FinishSideViewEvent`, `InvincibilityEvent`, MotionWarp/FreezeEnemy/Telegraph/Interaction/Afterimage/CameraSnapshotSequence 이벤트 | Manager·Components·State·Combat·CameraSystem·MovementController·Particle 결합. SerializeReference + TypeCache 구조 덕에 게임 어셈블리에 있어도 패키지 에디터/실행기가 그대로 인식하므로 옮길 필요 없음 |
| `ActorAnimationMotionSet`, `ActorAnimationStringKeyMotionSet`, `PlayerActorAnimationMotionSet`, `WeaponMotionMappingConfig` | 게임 enum(`UPlayGround.Data.EnumType`) + AYellowpaper(asmdef 없음) 의존 |
| `MotionSetWindow` 파티얼 8개 (`.cs`/`.CombatOverlay`/`.ControlPanels`/`.RootMotion`/`.WarpBake`/`.WarpTarget`/`.SlashVFXSceneTune`/`.CaptureBridge`) | Animancer·KCC(asmdef 없음)·Manager·Components·Particle·전투 오버레이 결합. partial이 어셈블리 경계를 못 넘음 → Phase 4 선택 과제 |
| `LocoMotionSetupWindow`, `WeaponMotionSetupWindow`, `MotionTestRegistrySO(+Editor)`, `ActorAnimationMotionSetEditor/Duplicator`, `PlayerActorAnimationMotionSetEditor` | 게임 데이터 타입(위 MotionSet 계열, `UPlayGround.Data.Actor`) 의존 |
| `IMotionEventCombatTarget` (`GameActor/Component/Player/`) | 게임 전투 계약. 잔류 (필요 시 Phase 4에서 재검토) |

### 3.4 범용 이벤트 4종 — Phase 3에서 이동 (MovedFrom 필요)

의존이 System + UnityEngine뿐임을 확인한 타입:

| 클래스 | 파일 |
|--------|------|
| `LoopEvent` | `MotionEvent_Loop.cs` |
| `AnimationSpeedEvent` | `MotionEvent_AnimationSpeed.cs` |
| `CustomCallbackEvent` | `MotionEvent_CustomCallback.cs` |
| `HideTargetEvent` | `MotionEvent_HideTarget.cs` (Renderer 조작만) |

`InvincibilityEvent`는 `GetComponent<GameActor>()`를 사용하므로 제외 — 패키지 인터페이스(예: `IInvincibilityTarget`)로 역전하기 전까지 게임 잔류.

---

## 4. 직렬화 안전성 분석

### 4.1 왜 Phase 1은 무손상인가

MotionSetAsset YAML에서 어셈블리명이 기록되는 곳은 `[SerializeReference]` managed reference의 **구체 타입** 항목뿐이다:

```yaml
references:
  version: 2
  RefIds:
  - rid: 1
    type: {class: BeginCollisionEvent, ns: UPlayGround.Data.Event, asm: Assembly-CSharp}
```

- `MotionEventBase`(추상)는 인스턴스화되지 않으므로 YAML에 등장하지 않는다 → 이동 무해.
- `Motion`/`MotionSet`은 `[Serializable]` 값 직렬화 → 타입 정보 미기록 → 이동 무해.
- `MotionSetAsset`은 GUID(.meta) 기반 → .meta 동반 이동 시 무해.
- 구체 이벤트는 전부 Assembly-CSharp에 남으므로 `asm:` 기록과 일치 유지.

### 4.2 구체 이벤트를 옮길 때 (Phase 3)

이동하는 각 타입에 `[MovedFrom]`을 부착해야 기존 데이터가 살아남는다:

```csharp
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
[Serializable]
public class LoopEvent : MotionEventBase { ... }
```

- 네임스페이스는 변경하지 않으므로 `sourceNamespace`는 불필요.
- 어트리뷰트는 데이터 재저장 후에도 **제거하지 말 것** (미저장 에셋이 남아 있을 수 있음).
- 검증: 이동 후 대표 MotionSetAsset을 열어 이벤트 리스트가 `Unknown managed type` 없이 로드되는지 확인. 전수 검증은 `Assets/10.Datas` 내 MotionSet YAML에서 `asm: Assembly-CSharp` + 이동 클래스명 조합을 grep.

---

## 5. 선행 리팩터 상세

### 5.1 MotionEventExecutor 결합 제거

**① 타깃 해석 인터페이스화** — 패키지 Runtime에 정의:

```csharp
namespace UPlayGround.Animation
{
    /// <summary>
    /// MotionEventExecutor가 모델(자식)에 붙었을 때 이벤트 타깃으로 쓸
    /// 루트 GameObject를 제공한다. 게임 측에서 GameActor가 구현한다.
    /// </summary>
    public interface IMotionEventTargetRoot
    {
        GameObject EventTargetRoot { get; }
    }
}
```

Executor의 `GetComponentInParent<GameActor>()` →
`GetComponentInParent<IMotionEventTargetRoot>()?.EventTargetRoot ?? gameObject`.
게임 측: `GameActor : IMotionEventTargetRoot` 구현 1줄 추가 (`EventTargetRoot => gameObject`).

**② 워프 기즈모 분리** — `MotionEventExecutor.cs` 하단의 `namespace UPlayGround.Debugging` 섹션(`ActorMovementController.MotionWarp` 참조)은 별도 파일 `GameActor/Animation/MotionEventWarpGizmo.cs`(가칭)로 잘라 게임에 남긴다. 신규 파일이므로 GUID 이슈 없음.

### 5.2 에디터 하드코딩 → 어트리뷰트 역전

어트리뷰트는 **Runtime asmdef에 정의**한다 (게임 이벤트 클래스가 에디터 어셈블리를 참조할 수 없으므로).

```csharp
namespace UPlayGround.Data.Event
{
    /// <summary>추가 팝업 카탈로그 + 타임라인 스타일 메타. 미부착 타입은 기본값(회색 ▸, Misc 카테고리).</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MotionEventMetaAttribute : Attribute
    {
        public string DisplayName;
        public string Category;      // 예: "Combat", "Camera", "Movement", "Sound", "Misc"
        public string Description;
        public string[] Aliases;     // 팝업 검색 별칭
        public string ColorHex;      // 타임라인 바 색 (예: "#FF5959")
        public string Icon;          // 트랙 레이블 아이콘 (예: "⚔")
    }
}
```

- `MotionEventStyle.GetByType`: 23건 `typeof` 체인 → 어트리뷰트 조회 + 캐시. 미부착 타입은 현행 기본값(`COL_MISC`, `"▸"`) 유지.
- `MotionEventAddPopup`: `Meta<T>()` 28건 카탈로그 → `TypeCache.GetTypesDerivedFrom<MotionEventBase>()` 스캔(추상 제외) + 어트리뷰트 메타. `MotionEvent_MotionWarp` 특례 1건도 어트리뷰트로 흡수.
- 게임 측 작업: 구체 이벤트 ~28종에 어트리뷰트 부착 (기존 카탈로그의 표시명/카테고리/설명/별칭/색을 그대로 옮겨 적음 — 표시 결과 불변이 완료 기준).
- 타 프로젝트 효과: 자기 이벤트 클래스를 정의하고 어트리뷰트만 붙이면 팝업/타임라인에 자동 등록.

---

## 6. 단계별 실행 계획

| Phase | 내용 | 데이터 리스크 | 규모 |
|-------|------|---------------|------|
| 1 | 패키지 골격(package.json + asmdef 2개) 생성, §3.1 파일 이동(.meta 동반), §5.1 Executor 리팩터 | 없음 | 소 |
| 2 | §5.2 어트리뷰트 역전 (Style/AddPopup 재작성 + 게임 이벤트 28종 부착) | 없음 (표시 계층만) | 중 |
| 3 | 범용 이벤트 4종 `[MovedFrom]` 부착 후 Runtime/Events/로 이동 | 있음 — §4.2 검증 필수 | 소 |
| 4 (선택) | `MotionSetWindow` 포팅 — 코어 윈도우 + 확장 탭 등록 API 재설계, KCC asmdef 추가 여부 결정 | 없음 | 대 (별도 설계 문서 필요) |

### Phase별 완료 기준

- **Phase 1**: 컴파일 0에러. 기존 MotionSetAsset 인스펙터 정상 로드. `MotionEventExecutor`가 붙은 프리팹(모델 자식 배치 케이스 포함)에서 타깃 해석 동작 동일. 워프 기즈모 표시 동일.
- **Phase 2**: 추가 팝업의 항목 수/카테고리/검색, 타임라인 바 색·아이콘이 리팩터 전과 완전 동일.
- **Phase 3**: `Assets/10.Datas` 전체 MotionSet 에셋에서 이동 4종 이벤트가 데이터 유실 없이 로드 (§4.2 grep 검증 + 대표 에셋 육안 확인). 재생 시 Loop/AnimationSpeed/CustomCallback/HideTarget 동작 동일.

### Unity 수작업 체크리스트 (각 Phase 공통)

1. 에디터 포커스 → 신규 파일 .meta 생성 및 콘솔 컴파일 에러 0 확인.
2. MotionSetWindow 열어 임의 MotionSet 로드·프리뷰·이벤트 편집 스모크 테스트.
3. 플레이 모드: 공격 콤보(콜리전/파티클/사운드 이벤트 발화), SlashVFX 위치 안정성(RequiresPostEvaluation 경로) 확인.
4. Phase 3 한정: 세이브된 씬/프리팹 재저장 전후 YAML diff에서 의도치 않은 `type:` 변화가 없는지 확인.

---

## 7. 후속 분리 후보 (본 계획 범위 밖)

2026-07-16 폴더 간 의존 전수 측정 결과 기준. Manager↔GameActor↔Data↔UI는 전방위 순환 결합이라 말단부터만 분리 가능하다.

| 후보 | 난이도 | 비고 |
|------|--------|------|
| `Particle/` (2파일, 외부 의존 0) | 즉시 가능 | `SlashVFXEvent`·MotionSetWindow가 사용 → 본 패키지의 선행/동반 분리 후보 |
| 에디터 검증 프레임워크 (`Tool/Editor/Validation`의 `EditorValidationFramework`/`Issue`/`Report`) | 쉬움 | 범용 에디터 인프라. 개별 Validator는 게임 잔류 |
| `Util/` (3파일) | 쉬움 | InputDefine·Manager 의존 제거 후 `UPlayGround.Core` 합류 |
| AI/BT 프레임워크 코어 (노드/러너/그래프 에디터) | 중간 | 게임 리프·스코어러 분리 시 재사용 가치 큼. BT JSON의 타입명 기록에 §4와 동일한 어셈블리명 함정이 있는지 선확인 필요 |
| Camera 프레임워크 (Director/Behavior/Modifier) | 어려움 | Combat/Manager/State 결합 — 인터페이스 추출 필요, 중장기 |

부수 효과: asmdef로 나간 코드는 게임 코드 수정 시 재컴파일되지 않으므로 반복 컴파일 시간이 단축된다.
