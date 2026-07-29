# MotionSetEditor asmdef 편입 및 일반화 설계

> 작성일: 2026-07-27
> 대상: `Assets/02.Scripts/Editor/MotionSetWindow.*` (애니메이션 에디터 윈도우) 및 주변 에디터 파일
> 목표: 윈도우 본체를 `UPlayGround.MotionSet.Editor` asmdef로 편입한다. Actor는 인터페이스로, MotionEvent 편집 확장은 인터페이스로, 프리뷰 대상(Player/모델) 로드는 **등록된 데이터** 기준으로 일반화한다.
> 선행 문서: `Assets/docs/TODO/MOTIONSET_GAMEACTOR_SEPARATION_SPEC.md`, `Assets/docs/Complete/MOTIONSET_ASMDEF_PACKAGE_REFACTOR_PLAN.md`
> 구현 완료: 2026-07-28 — 제네릭 UI Toolkit 3열 편집기, 프리뷰 카탈로그 마이그레이션, 프로젝트 확장 패널 분리, 레거시 창 제거
> 후속 보강: 프리뷰 카탈로그의 씬 열기·Play 진입·프리팹 대상 스폰, 활성 모델 Animancer 결속, 좁은 창 반응형 패널 전환을 반영했다.

---

## 1. 결론

가능하다. 그리고 생각보다 표면적이 작다.

전수 조사 결과, 윈도우가 실제로 호출하는 프로젝트 타입의 **멤버**는 다음 19개뿐이다.

```
ActorAnimator          : DeltaPosition, DeltaRotation, UpperBodyMask, MotionSet
PlayerActorAnimator    : PlayerMotionSet
PlayerActor            : GetPlayerEquipment(), SetInputSuppressed()
PlayerEquipment        : GetMainWeaponType(), SetWeaponType(), IsMainWeaponEquipped,
                         IsSubWeaponEquipped, SetMainWeaponDrawn(), SetSubWeaponDrawn(),
                         BeginInteractionEquipment(), EndInteractionEquipment()
PlayerSwapBehaviour    : ActiveCharacterType, GetAllCharacterTypes(), GetModelData(), SwapTo()
ActorMovementController: Motor
KinematicCharacterMotor: SetPositionAndRotation(), enabled
InputManager           : SetPlayerActionInputSuppressed(), SetPlayerActionLookAllowed(), InputBuffer
```

나머지 결합은 전부 **패널 단위로 통째로 잘라낼 수 있는 덩어리**(전투 오버레이, 워프, 캡처 브리지, SlashVFX)다.

또한 범용 편집 코어(`MotionSetDrawer` 2787줄, `TimelineView` 2732줄, `MotionSetValidation`, `MotionEventInspectorView`, `MotionListView`, `MotionEventClipboard`, `MotionEventTypeRegistry`)는 이미 `UPlayGround.Data.Event`(= `MotionSet.Core`)만 참조한다. **수정 없이 이동 가능하다.** 이것이 전체 라인 수의 절반 이상이다.

지배 제약은 하나다: **partial 클래스는 어셈블리 경계를 넘지 못한다.** 따라서 `MotionSetEditorWindow`의 파티얼 10개 중 프로젝트 결합이 있는 4개는 *이동*이 아니라 *별도 클래스로의 해체*가 필요하다. 이 문서 설계의 절반은 그 해체를 위한 확장 API다.

---

## 2. 현황 전수

### 2.1 현재 asmdef 배치

| 어셈블리 | 경로 | 상태 |
|---|---|---|
| `UPlayGround.MotionSet.Core` | `02.Scripts/MotionSet/Core` | 참조 0개. 데이터·타임라인·이벤트 베이스 |
| `UPlayGround.MotionSet.Animancer` | `02.Scripts/MotionSet/Animancer` | Core + Animancer |
| `UPlayGround.MotionSet.Editor` | `02.Scripts/MotionSet/Editor` | Core만. 현재 3파일(카탈로그·폴백 인스펙터·검증기) |
| **(없음)** | `02.Scripts/Editor` | **Assembly-CSharp-Editor.** 윈도우 21파일 14,205줄이 여기 있다 |

즉 `MotionSet.Editor`는 이미 있고 비어 있다. 이 작업은 새 asmdef를 만드는 게 아니라 **기존 빈 껍데기를 채우는 일**이다.

### 2.2 파일별 결합 판정

| 파일 | 줄수 | 프로젝트 결합 | 판정 |
|---|---:|---|---|
| `MotionSetDrawer.cs` | 2787 | 없음 | **그대로 이동** |
| `UIToolkit/Timeline/TimelineView.cs` | 2732 | 없음 | **그대로 이동** |
| `UIToolkit/Timeline/MotionSetValidation.cs` | 365 | 없음 | **그대로 이동** |
| `MotionSetEditor.cs` (`MotionEventTypeRegistry`) | 339 | 없음 | **그대로 이동** |
| `UIToolkit/MotionListView.cs` | 306 | 없음 | **그대로 이동** |
| `UIToolkit/MotionEventInspectorView.cs` | 207 | 없음 | **그대로 이동** |
| `UIToolkit/MotionEditorShell.cs` | 151 | 없음 | **그대로 이동** |
| `MotionSetAssetEditor.cs` | 130 | 없음 | **그대로 이동** |
| `UIToolkit/Timeline/*` 나머지 3파일 | 135 | 없음 | **그대로 이동** |
| `MotionSetWindow.ControlPanels.cs` | 87 | 없음 | **그대로 이동** |
| `MotionSetWindow.cs` | 3537 | Actor·Player·Input·모션셋 SO·GameplayTag | **일반화 후 이동** (§3, §4, §5) |
| `UIToolkit/MotionSetEditorWindow.cs` | 73 | 없음(윈도우 파티얼) | 이동 |
| `MotionSetWindow.RootMotion.cs` | 349 | `ActorAnimator`, `KinematicCharacterMotor` | **일반화 후 이동** (§3.3) |
| `UIToolkit/…ControlPanelsUIToolkit.cs` | 389 | `AbilitySetSO`, `DialogueCameraRecorderWindow` | **분할**: 셸은 이동, Ability 탭은 확장 패널로 |
| `MotionSetWindow.CombatOverlay.cs` | 652 | Ability·HitPhase·Projectile·Tool.Editor.Combat | **확장 패널로 해체**, 프로젝트 잔류 |
| `MotionSetWindow.SlashVFXSceneTune.cs` | 1054 | `SlashVFXEvent` | **이벤트 씬 에디터로 해체**, 프로젝트 잔류 |
| `MotionSetWindow.WarpTarget.cs` | 449 | `MotionWarpController`, `PlayerCombat` | **확장 패널로 해체**, 프로젝트 잔류 |
| `MotionSetWindow.WarpBake.cs` | 274 | `MotionEvent_MotionWarp` | **확장 패널로 해체**, 프로젝트 잔류 |
| `MotionSetWindow.CaptureBridge.cs` | 189 | `DialogueCameraRecorderWindow` | **확장 패널로 해체**, 프로젝트 잔류 |
| `MotionTestRegistrySO(+Editor).cs` | — | `ActorDefinitionSO`, `ActorDatabase` | **§5로 대체** |

이동 대상 합계 약 7,900줄 중 **수정 없이 이동 6,900줄**, 일반화 필요 1,000줄. 프로젝트 잔류(해체) 약 2,600줄.

---

## 3. Seam 1 — Actor를 인터페이스로

### 3.1 원칙

단일 God 인터페이스를 만들지 않는다. 윈도우 기능 단위로 **선택적 능력(capability) 인터페이스**를 쪼개고, 대상이 해당 인터페이스를 구현하지 않으면 그 UI를 비활성화한다. 이유:

- Monster/NPC 프리뷰는 무기 스왑도 캐릭터 스왑도 없다. 지금은 `PlayerSwapBehaviour == null` 분기로 처리하는데, 이걸 인터페이스 유무로 바꾸면 분기가 자연스러워진다.
- 다른 프로젝트로 이식할 때 최소 구현(= `IMotionPreviewSubject` 하나)만으로 윈도우가 동작해야 한다.

### 3.2 필수 계약

```csharp
// MotionSet/Editor/Preview/IMotionPreviewSubject.cs
namespace UPlayGround.Animation.Editor
{
    /// <summary>프리뷰 대상 1개에 대한 최소 계약. 이것만 구현해도 윈도우는 동작한다.</summary>
    public interface IMotionPreviewSubject
    {
        GameObject Root { get; }

        /// <summary>Animancer 재생 호스트. null이면 재생 UI 비활성.</summary>
        Animancer.AnimancerComponent Animancer { get; }

        /// <summary>레이어별 AvatarMask. 현재 ActorAnimator.UpperBodyMask 대체.</summary>
        AvatarMask GetLayerMask(int layerIndex);

        /// <summary>대상이 노출하는 MotionSet 카탈로그(§4). 없으면 null.</summary>
        IMotionSetCatalog Catalog { get; }

        /// <summary>대상 상태가 외부에서 바뀌었을 때(모델 스왑 등) 캐시 재수집.</summary>
        void Refresh();
    }
}
```

`AnimancerComponent`가 인터페이스에 노출되므로 `MotionSet.Editor`는 `Kybernetik.Animancer`를 참조한다. 이는 허용된 외부 의존이다(선행 문서 §1: "KCC와 Animancer 같은 외부 라이브러리 의존은 허용한다"). 완전 탈-Animancer는 비목표.

### 3.3 선택 능력 계약

```csharp
/// 루트모션 프리뷰. 현재 RootMotion.cs가 ActorAnimator + KCC Motor를 직접 잡는 부분.
public interface IMotionPreviewRootMotion
{
    Vector3    DeltaPosition { get; }
    Quaternion DeltaRotation { get; }

    /// <summary>물리/상태머신 정지. 현재 KCC Motor.enabled = false 에 해당.</summary>
    void SetSimulationSuspended(bool suspended);

    /// <summary>워프/텔레포트. 현재 Motor.SetPositionAndRotation() 에 해당.</summary>
    void Teleport(Vector3 position, Quaternion rotation);
}

/// 프리뷰 중 입력 잠금. 현재 PlayerActor.SetInputSuppressed + InputManager 3종.
public interface IMotionPreviewInputLock
{
    void SetInputSuppressed(bool suppressed, bool allowCameraLook);
    void ClearBufferedInput();
}

/// 캐릭터 모델 스왑 + 무기 타입 + 생활도구를 하나의 축 개념으로 일반화.
public interface IMotionPreviewVariants
{
    IReadOnlyList<MotionPreviewAxis> Axes { get; }
    string GetSelected(string axisId);
    bool   Select(string axisId, string optionId);
}

/// 씬 뷰 상태 텍스트/기즈모를 대상이 직접 제공(현재 워프 상태 HUD).
public interface IMotionPreviewStatusOverlay
{
    string GetSceneStatusText();
}
```

```csharp
public sealed class MotionPreviewAxis
{
    public string Id;               // "character" | "weapon" | "tool"
    public string DisplayName;      // "캐릭터" | "무기" | "생활도구"
    public IReadOnlyList<MotionPreviewAxisOption> Options;
    /// <summary>true면 옵션 변경 시 Catalog가 바뀌므로 MotionSet 재선택이 필요하다.</summary>
    public bool AffectsCatalog;
}

public sealed class MotionPreviewAxisOption
{
    public string Id;               // enum.ToString() 등 프로젝트 자유
    public string DisplayName;
}
```

**이 축(Axis) 일반화가 이 설계의 핵심이다.** 현재 윈도우는 `CharacterActorType`(enum), `WeaponType`(enum), `InteractionObjectType`(enum) 3개 축을 각각 별도 필드·별도 UI·별도 EditorPrefs로 하드코딩한다. 셋 다 "옵션 목록 중 하나를 고르면 대상 GameObject가 재구성된다"는 동일한 구조다. 축으로 접으면 윈도우는 `foreach (axis in Axes) DrawPopup(...)` 한 줄이 되고, 프로젝트가 축을 추가해도 윈도우를 고치지 않는다.

`AffectsCatalog`는 현재 무기 스왑 시 `ResolveSelectedPlayerActorAnimationSet()`을 다시 도는 로직에 대응한다. 캐릭터 축·무기 축은 `true`, 생활도구 축은 `false`다.

### 3.4 대상 결속(binding) — 윈도우가 `PlayerActor`를 모르게 하는 방법

윈도우는 `GameObject`만 들고 있고, 어댑터 생성은 **TypeCache로 발견되는 바인더**에 위임한다.

```csharp
public interface IMotionPreviewSubjectBinder
{
    int Priority { get; }
    /// <summary>이 GameObject를 다룰 수 없으면 null 반환.</summary>
    IMotionPreviewSubject TryBind(GameObject root);
}

public static class MotionPreviewSubjectBinderRegistry
{
    // TypeCache.GetTypesDerivedFrom<IMotionPreviewSubjectBinder>() 로 수집,
    // Priority 내림차순으로 첫 성공 반환. 전부 실패하면 GenericAnimancerSubject 폴백.
    public static IMotionPreviewSubject Bind(GameObject root);
}
```

`MotionSet.Editor`는 폴백 하나만 갖는다: `AnimancerComponent`만 찾아 재생하는 `GenericAnimancerPreviewSubject` (Priority 0). 이식 직후 아무 어댑터가 없어도 윈도우가 재생은 된다.

프로젝트 측 구현(`02.Scripts/Editor/MotionEditorExtensions/`):

| 바인더 | Priority | 구현 능력 |
|---|---:|---|
| `PlayerActorPreviewBinder` | 100 | Subject + RootMotion + InputLock + Variants(character/weapon/tool) + StatusOverlay |
| `GameActorPreviewBinder` | 50 | Subject + RootMotion (Monster/NPC. Variants 없음) |

`PlayerActorPreviewSubject`가 `PlayerActor` / `PlayerEquipment` / `PlayerSwapBehaviour` / `ActorMovementController.Motor` / `InputManager`를 전부 흡수한다. §1의 19개 멤버가 이 한 클래스 안으로 들어간다. **모델 스왑 후 `PlayerEquipment`를 매번 다시 읽어야 한다는 현재 주석의 함정**(`MotionSetWindow.cs:433`)은 `Refresh()` 계약으로 명시화한다 — 어댑터 내부가 아니라 계약으로 드러내야 이식 시 재현된다.

---

## 4. Seam 2 — MotionSet 소스를 인터페이스로

윈도우는 `ActorAnimationMotionSet`(GameplayTag → MotionSetAsset)과 `PlayerActorAnimationMotionSet`(WeaponType → ActorAnimationMotionSet)을 직접 다루며, `MotionTags` 리플렉션으로 슬롯 후보를 뽑고, `SerializedProperty`로 슬롯을 생성·할당한다. `GameplayTag`와 `MotionTags`는 프로젝트 타입이므로 그대로는 못 나간다.

```csharp
public interface IMotionSetCatalog
{
    UnityEngine.Object SourceAsset { get; }          // 인스펙터 표시/Undo 대상
    IReadOnlyList<MotionSetSlot> Slots { get; }      // 이미 채워진 슬롯
    IReadOnlyList<MotionSetSlot> AssignableSlots { get; } // 아직 비어 있는 후보(현재 AllGameplayTags)

    MotionSetAsset  Resolve(string slotId);
    bool            Assign(string slotId, MotionSetAsset asset);   // Undo 포함
    MotionSetAsset  CreateAndAssign(string slotId, string directory);
    void            Refresh();
}

public readonly struct MotionSetSlot
{
    public readonly string SlotId;       // GameplayTag 직렬화 문자열
    public readonly string DisplayName;
    public readonly string GroupLabel;   // 현재 GetActorKeyGroupLabel() 결과
}
```

프로젝트 구현 2종(`MotionEditorExtensions/`):

- `ActorAnimationMotionSetCatalog` — `GameplayTag ↔ string` 변환 + `MotionTags` 리플렉션 + 기존 `SerializedProperty` 생성/할당 로직을 그대로 옮긴다.
- `PlayerActorAnimationMotionSetCatalog` — 무기 축 선택값에 따라 내부 `ActorAnimationMotionSetCatalog`로 위임한다. 즉 §3.3의 `weapon` 축과 이 카탈로그가 한 쌍으로 움직인다.

`SlotId`는 `GameplayTag`의 직렬화 문자열을 쓴다. `int` 해시를 쓰면 태그 재정의 시 EditorPrefs에 저장된 선택값이 조용히 다른 슬롯을 가리키게 된다.

**주의:** 현재 `MotionSetEditorWindow.Open(ActorAnimationMotionSet, GameplayTag, MotionSetAsset)` 등 정적 진입점 4개가 프로젝트 타입을 시그니처에 노출한다. 이동 후 윈도우가 갖는 건 `Open(MotionSetAsset)` / `Open(IMotionSetCatalog, string slotId, MotionSetAsset)` 2개이고, 프로젝트 타입을 받는 오버로드는 프로젝트 측 정적 헬퍼(`MotionEditorProjectEntry.Open(...)`)로 내린다. 호출부(`ActorAnimationMotionSetEditor`, `PlayerActorAnimationMotionSetEditor`, `ActorAnimationMotionSetDuplicator`, `LocoMotionSetupWindow`, `WeaponMotionSetupWindow`)를 이 헬퍼로 돌린다.

---

## 5. Seam 3 — 프리뷰 대상 로드를 "등록된 데이터" 기준으로

### 5.1 현재의 문제

윈도우는 대상을 3가지 서로 다른 경로로 얻는다.

1. **Player 모드**: 씬에서 이름 `"Player"`(`_testActorName` 문자열)로 `GameObject.Find`
2. **Other 모드**: `MotionTestRegistrySO.entries[i].actorDef.prefab`을 `Instantiate`
3. **수동**: `ObjectField`로 직접 드래그

그리고 테스트 씬 경로가 `"Assets/01.Scenes/Test/MotionTestMap.unity"`로 하드코딩되어 있다. `MotionTestRegistrySO`는 `ActorDefinitionSO`/`ActorDatabase`(프로젝트 `Data`)에 묶여 있어 같이 나갈 수 없다.

Player가 "모드"로 특별 취급되는 게 근본 문제다. Player는 *씬에 이미 존재하는 대상*일 뿐, 다른 종류가 아니다.

### 5.2 설계 — `MotionPreviewCatalogSO`

`MotionSet.Editor`가 소유하는 SO 하나로 3경로를 통합한다.

```csharp
// MotionSet/Editor/Preview/MotionPreviewCatalogSO.cs
[CreateAssetMenu(menuName = "UPlayGround/Motion/Preview Catalog")]
public sealed class MotionPreviewCatalogSO : ScriptableObject
{
    [Serializable]
    public sealed class SubjectEntry
    {
        public string        id;              // EditorPrefs 저장 키. 프리팹 이름 변경에 견디도록 별도 id
        public string        displayName;
        public SubjectSource source;          // ScenePrefab | ScenePresent
        public GameObject    prefab;          // ScenePrefab일 때
        public string        sceneObjectName; // ScenePresent일 때 (현재 "Player")
        public AnimationClip idleClip;
        public Vector3       spawnOffset;
    }

    public enum SubjectSource { ScenePrefab, ScenePresent }

    public SceneAsset          previewScene;   // 현재 하드코딩 경로 대체
    public List<SubjectEntry>  subjects = new();
}
```

윈도우 로직은 하나로 수렴한다:

```
선택된 SubjectEntry
  → source == ScenePresent ? 씬에서 sceneObjectName 탐색 : prefab Instantiate(spawnOffset)
  → MotionPreviewSubjectBinderRegistry.Bind(go)      // §3.4
  → subject.Catalog 로 MotionSet 목록 표시            // §4
  → subject as IMotionPreviewVariants 로 축 UI 표시   // §3.3
```

`TestActorMode` enum, `_scenePlayer`, `_spawnedTestActor`, `_testActorName`, `_testScenePath`, `_selectedRegistryIndex` 6개 필드가 `SubjectEntry` 선택 하나로 대체된다.

### 5.3 프로젝트 데이터로 카탈로그 채우기

`ActorDatabase → MotionPreviewCatalogSO` 동기화는 **프로젝트 측 인스펙터 확장**으로 내린다.

```csharp
public interface IMotionPreviewCatalogPopulator
{
    string ButtonLabel { get; }                                  // "ActorDatabase에서 채우기 (Monster)"
    void Populate(MotionPreviewCatalogSO catalog);
}
```

`MotionPreviewCatalogSO`의 커스텀 인스펙터가 TypeCache로 populator를 수집해 버튼을 그린다. 기존 `MotionTestRegistrySOEditor`의 타입별 동기화 버튼(전체/Monster/Player/NPC)과 위험 구역 UI는 그대로 `ActorDatabaseMotionPreviewPopulator`로 옮긴다. `MotionTestRegistrySO`는 1회 마이그레이션 후 삭제한다.

> CLAUDE.md의 `CreateAssetMenu` flat 도메인 규약에 따라 메뉴는 `UPlayGround/Motion/Preview Catalog` 2단계로 둔다.

---

## 6. Seam 4 — MotionEvent 편집 확장을 인터페이스로

`MotionEventBase.Execute(GameObject)`는 이미 프로젝트 비의존이다. 문제는 **에디터가 구체 이벤트 타입을 특별 취급하는 곳**이다: SlashVFX 씬 튜닝(1054줄), 워프 베이크(274줄).

이미 있는 `MotionEventDescriptorAttribute`(표시 메타데이터) 옆에 편집 확장 2종을 추가한다.

### 6.1 이벤트 씬 에디터

```csharp
/// <summary>특정 MotionEvent 타입에 Scene View 편집 UI를 붙인다.</summary>
public interface IMotionEventSceneEditor
{
    Type EventType { get; }
    /// <summary>Scene View 핸들. 값을 바꿨으면 true 반환 → 윈도우가 Repaint + Undo flush.</summary>
    bool OnSceneGUI(MotionEventBase evt, IMotionEditorContext ctx);
    /// <summary>인스펙터 하단 추가 패널. 불필요하면 no-op.</summary>
    void OnInspectorGUI(MotionEventBase evt, IMotionEditorContext ctx);
}
```

TypeCache로 수집해 `Dictionary<Type, IMotionEventSceneEditor>`를 만든다. 선택된 이벤트의 런타임 타입(및 베이스 체인)으로 조회한다. `SlashVFXSceneTune` 1054줄 전체가 `SlashVFXEventSceneEditor` 한 클래스로 이동하며, 윈도우 쪽에는 "선택된 이벤트에 씬 에디터가 있으면 호출한다" 3줄만 남는다.

### 6.2 에디터 패널 확장

```csharp
/// <summary>애니메이션 에디터에 탭/패널을 추가한다. 현재 파티얼로 붙어 있던 것들의 대체.</summary>
public interface IMotionEditorPanel
{
    string Title { get; }
    int    Order { get; }
    bool   IsAvailable(IMotionEditorContext ctx);
    void   OnGUI(IMotionEditorContext ctx);
    void   OnSceneGUI(IMotionEditorContext ctx);
    void   OnPlaybackStateChanged(IMotionEditorContext ctx, MotionPreviewPlaybackState state);
}
```

`OnPlaybackStateChanged`가 필요한 이유: 워프 타겟 주입은 "재생 시작 시 1회"라는 시점 요구가 있고(`WarpTarget.cs` 주석), 전투 오버레이는 재생 중 캐시 무효화가 필요하다. 순수 `OnGUI`만으로는 표현할 수 없다.

### 6.3 편집 컨텍스트

```csharp
public interface IMotionEditorContext
{
    MotionSetAsset            Asset { get; }
    MotionSet                 CurrentSet { get; }
    Motion                    CurrentMotion { get; }
    MotionEventBase           SelectedEvent { get; }
    IMotionPreviewSubject     Subject { get; }        // null 가능
    float                     PlaybackTime { get; }
    bool                      IsPlaying { get; }

    void Repaint();
    void RecordUndo(string label);
    void SetPlaybackTime(float time);
}
```

윈도우가 `this`를 이 인터페이스로 넘긴다. 확장 패널은 윈도우의 구체 타입을 모른다.

### 6.4 프로젝트 패널 배치

| 확장 클래스 | 대체하는 파티얼 | 구현 |
|---|---|---|
| `CombatOverlayPanel` | `CombatOverlay.cs` (652줄) | `IMotionEditorPanel` |
| `WarpTargetPanel` | `WarpTarget.cs` (449줄) | `IMotionEditorPanel` |
| `WarpBakePanel` | `WarpBake.cs` (274줄) | `IMotionEditorPanel` |
| `DialogueCaptureBridgePanel` | `CaptureBridge.cs` (189줄) + `ControlPanelsUIToolkit.cs:351` | `IMotionEditorPanel` |
| `AbilitySetBindingPanel` | `ControlPanelsUIToolkit.cs` Ability 탭 부분 | `IMotionEditorPanel` |
| `SlashVFXEventSceneEditor` | `SlashVFXSceneTune.cs` (1054줄) | `IMotionEventSceneEditor` |

이 6개는 **`Assets/02.Scripts/Editor/MotionEditorExtensions/`(Assembly-CSharp-Editor)에 둔다.** 새 asmdef를 만들지 않는 이유: `CombatOverlayPanel`이 `UPlayGround.Tool.Editor.Combat.MotionSetCombatEvents`를 쓰는데 이건 asmdef 없는 Assembly-CSharp-Editor 소속이고, `CaptureBridge`는 `UPlayGround.Data.Editor`를 쓴다. Assembly-CSharp-Editor는 모든 asmdef를 참조할 수 있으므로 여기 두면 추가 이동이 0이다. `MotionSet.Editor`가 `autoReferenced: true`이므로 참조도 자동으로 잡힌다.

기존 파티얼은 `partial class MotionSetEditorWindow`의 private 필드(`_targetActor`, `_asset`, `_playbackTime` 등)에 자유롭게 접근하고 있다. 해체 시 이 접근은 전부 `IMotionEditorContext`를 통하도록 바꿔야 한다 — **이것이 이 작업에서 가장 손이 많이 가는 부분이며, 실제 리스크가 여기 집중된다.**

---

## 7. 최종 배치

```text
Assets/02.Scripts/MotionSet/Editor/            (UPlayGround.MotionSet.Editor)
├── UPlayGround.MotionSet.Editor.asmdef        refs: MotionSet.Core, MotionSet.Animancer,
│                                                    Kybernetik.Animancer
├── MotionEventCatalog.cs                      (기존)
├── MotionSetAssetFallbackEditor.cs            (기존)
├── MotionSetModuleValidator.cs                (기존, 아래 검증 추가)
├── Window/
│   ├── MotionSetEditorWindow.cs               ← MotionSetWindow.cs (일반화 후)
│   ├── MotionSetEditorWindow.ControlPanels.cs
│   ├── MotionSetEditorWindow.RootMotion.cs
│   ├── MotionSetEditorWindow.UIToolkit.cs
│   └── MotionEditorShell.cs
├── Drawer/
│   ├── MotionSetDrawer.cs
│   ├── MotionSetAssetEditor.cs
│   └── MotionEventTypeRegistry.cs             ← MotionSetEditor.cs
├── Timeline/
│   ├── TimelineView.cs / TimelineTrackElement.cs / TimelineManipulators.cs
│   ├── MotionEventClipboard.cs
│   └── MotionSetValidation.cs
├── Views/
│   ├── MotionListView.cs
│   └── MotionEventInspectorView.cs
├── Preview/
│   ├── IMotionPreviewSubject.cs               (§3.2, §3.3)
│   ├── MotionPreviewAxis.cs
│   ├── IMotionPreviewSubjectBinder.cs         (§3.4)
│   ├── GenericAnimancerPreviewSubject.cs      (폴백)
│   ├── MotionPreviewCatalogSO.cs              (§5.2)
│   ├── MotionPreviewCatalogSOEditor.cs
│   └── IMotionPreviewCatalogPopulator.cs      (§5.3)
├── Catalog/
│   └── IMotionSetCatalog.cs                   (§4)
├── Extension/
│   ├── IMotionEditorContext.cs                (§6.3)
│   ├── IMotionEditorPanel.cs                  (§6.2)
│   ├── IMotionEventSceneEditor.cs             (§6.1)
│   └── MotionEditorExtensionRegistry.cs       (TypeCache 수집)
└── Styles/MotionEditor.uss

Assets/02.Scripts/Editor/MotionEditorExtensions/   (Assembly-CSharp-Editor)
├── MotionEditorProjectEntry.cs                 (§4 정적 진입점 헬퍼)
├── Subject/
│   ├── PlayerActorPreviewBinder.cs + PlayerActorPreviewSubject.cs
│   └── GameActorPreviewBinder.cs  + GameActorPreviewSubject.cs
├── Catalog/
│   ├── ActorAnimationMotionSetCatalog.cs
│   └── PlayerActorAnimationMotionSetCatalog.cs
├── Populator/ActorDatabaseMotionPreviewPopulator.cs
├── Panel/
│   ├── CombatOverlayPanel.cs
│   ├── WarpTargetPanel.cs
│   ├── WarpBakePanel.cs
│   ├── DialogueCaptureBridgePanel.cs
│   └── AbilitySetBindingPanel.cs
└── EventEditor/SlashVFXEventSceneEditor.cs
```

`MotionSet.Editor.asmdef`에 추가할 참조는 `UPlayGround.MotionSet.Animancer`와 `Kybernetik.Animancer` 둘뿐이다. **`UPlayGround.Data` / `Actor` / `Contracts` / KCC를 절대 추가하지 않는다** — 이것이 경계 검증의 유일한 기준이다.

---

## 8. 이행 순서

각 단계는 독립적으로 컴파일 가능해야 하며, 단계마다 애니메이션 에디터를 열어 기존 동작을 확인한다.

| 단계 | 내용 | 이동 라인 | 리스크 |
|---|---|---:|---|
| **S1** | 계약 파일만 신규 작성(§3·§4·§6). 기존 코드 무변경 | 0 | 없음 |
| **S2** | 결합 0인 10파일을 `MotionSet.Editor`로 이동(`.meta` 동반). `MotionSetAssetEditor`의 `CustomEditor` GUID가 유지되는지 확인 | ~6,900 | 낮음 |
| **S3** | `MotionPreviewCatalogSO` 도입 + 기존 `MotionTestRegistrySO` 데이터 1회 마이그레이션. 윈도우는 아직 Assembly-CSharp-Editor에 있는 채로 새 카탈로그를 쓰도록 전환 | ~250 | 중 (에셋 데이터) |
| **S4** | `PlayerActorPreviewSubject` / `GameActorPreviewSubject` 작성. 윈도우의 Actor 직접 접근 19개를 어댑터 경유로 교체. **아직 이동하지 않는다** | ~600 | 중 |
| **S5** | 카탈로그(§4) 전환. `GameplayTag` 직접 사용을 `slotId` 문자열로 교체. `Open(...)` 진입점 4개를 프로젝트 헬퍼로 이설 + 호출부 5곳 수정 | ~400 | 중 |
| **S6** | 파티얼 4개 + SlashVFX를 확장 클래스로 해체. private 필드 접근을 `IMotionEditorContext`로 전환 | ~2,600 | **높음** |
| **S7** | 윈도우 본체 + RootMotion + ControlPanels를 `MotionSet.Editor`로 이동. asmdef 참조 확정 | ~4,000 | 중 |
| **S8** | `MotionSetModuleValidator`에 경계 검증 추가. `MotionTestRegistrySO` 삭제 | — | 낮음 |

S6이 위험 구간이다. S6 전에 S4·S5를 끝내 두면 S6에서 다뤄야 할 공유 상태가 `IMotionEditorContext` 9개 멤버로 줄어든다. 순서를 바꾸면 안 된다.

---

## 9. 검증

### 9.1 자동 검증 — `MotionSetModuleValidator` 확장

기존 검증기에 다음을 추가한다.

1. `UPlayGround.MotionSet.Editor` 어셈블리의 `GetReferencedAssemblies()`에 `UPlayGround.Data` / `UPlayGround.Actor` / `UPlayGround.Contracts` / `KinematicCharacterController` / `Assembly-CSharp*`가 없을 것.
2. `MotionSet.Editor` 안에 `partial class MotionSetEditorWindow` 파티얼이 프로젝트 어셈블리에 남아 있지 않을 것(어셈블리 분산 검출).
3. TypeCache로 수집된 `IMotionEditorPanel` / `IMotionEventSceneEditor` 구현이 전부 인스턴스화 가능(공개 무인자 생성자)할 것.

### 9.2 수동 검증 체크리스트

- Player 대상: 캐릭터 스왑 → 무기 스왑 → MotionSet 자동 재선택이 기존과 동일한가
- Monster 대상: Variants 축 UI가 뜨지 않고 재생이 정상인가
- 루트모션 프리뷰: KCC Motor 정지/복구, 누적 위치가 기존과 일치하는가 (`project_motion_editor_root_preview` 메모의 Player 이중 적용 리스크 재확인)
- SlashVFX 씬 튜닝: 핸들 조작 → Undo → 에셋 저장 경로가 유지되는가
- 전투 오버레이: AbilitySet 자동 연결과 HitPhase 트랙이 기존과 동일한가
- 워프 베이크 / 더미 타겟 주입 시점(재생 시작 1회)이 유지되는가
- 대화 카메라 캡처 브리지 진입이 동작하는가
- 기존 `MotionSetAsset` 에셋의 SerializeReference 이벤트가 전부 살아 있는가 (**이동 대상에 런타임 이벤트 타입이 없으므로 원칙적으로 무영향** — 그래도 확인)

### 9.3 이식성 확인 (이 작업의 최종 목표)

빈 Unity 6 프로젝트에 Animancer만 넣고 `02.Scripts/MotionSet/` 폴더를 `.meta`와 함께 복사했을 때:

- 컴파일이 통과하고
- 애니메이션 에디터 윈도우가 열리고
- `MotionPreviewCatalogSO`를 만들어 아무 Animancer 프리팹을 등록하면
- `GenericAnimancerPreviewSubject` 폴백으로 MotionSet 재생/이벤트 편집이 된다

여기까지가 성공 기준이다. 전투 오버레이·워프·SlashVFX가 없는 것은 정상이다.

---

## 10. 비목표

- `ActorAnimator` 런타임 API 변경. 이 문서는 **에디터 전용**이다.
- 구체 MotionEvent의 어셈블리 이동. `[MovedFrom]` 리스크를 만들지 않는다.
- Animancer 비의존화.
- UPM 패키지화. 선행 문서에서 이미 철회했다. 폴더 복사 이식이 기준이다.
- IMGUI → UI Toolkit 추가 전환. 현재 혼재 상태를 그대로 옮긴다.
