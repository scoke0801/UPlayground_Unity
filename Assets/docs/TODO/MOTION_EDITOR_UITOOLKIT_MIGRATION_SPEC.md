# 애니메이션 에디터 UI Toolkit 전환 스펙

> 문서 버전: 1.0<br>
> 기준일: 2026-07-18<br>
> 대상 버전: Unity 6 (6000.0.60f1), 싱글플레이, URP<br>
> 상태: 계획 수립 / 미착수<br>
> 관련 문서: `GAMEPLAY_ABILITY_SYSTEM_SPEC.md`, `../guide/CAMERA_MODULE_PORTABILITY_GUIDE.md`

## 1. 목적

이 문서는 IMGUI로 작성된 애니메이션 에디터(`MotionSetEditorWindow`, 약 9,700줄)를 UI Toolkit(UIElements) 기반으로 **단계적**으로 전환하기 위한 구현 계약을 정의한다.

전환의 목표는 다음과 같다.

- Rect 수동 계산과 `Event.current` 분기(드래그·줌·커서 74곳)로 누적된 유지보수 비용 축소
- `PropertyField` + `SerializedObject` 바인딩으로 리플렉션 기반 인스펙터 제거
- 프로젝트 최초의 **공통 USS 디자인 시스템** 수립(현재 프로젝트 전체 `.uss` 파일 1개뿐, 인라인 스타일 산재)
- 재생 중에만 다시 그리는 구조를 강제하여 "에디터 열어두면 게임 프레임드랍" 회귀 방지

**빅뱅 재작성은 금지한다.** 각 Phase 종료 시점마다 에디터는 항상 동작하는 상태를 유지한다.

참고:

- Unity UI Toolkit: https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html
- TwoPaneSplitView: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.TwoPaneSplitView.html
- ListView 가상화: https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-uxml-element-ListView.html
- Painter2D / generateVisualContent: https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-particles.html
- Manipulator: https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-manipulators.html

---

## 2. 규범 용어

| 용어 | 의미 |
|------|------|
| 필수 | 구현이 반드시 따라야 한다 |
| 권장 | 특별한 이유가 없다면 따라야 한다 |
| 선택 | 후속 단계에서 도입할 수 있다 |
| 금지 | 구조 안정성을 위해 사용하지 않는다 |

신규 타입은 `제안`, 현재 코드에 존재하는 타입은 `기존`으로 표시한다.

---

## 3. 현재 구조 (검증 결과)

애니메이션 에디터 = `MotionSetEditorWindow` (namespace `UPlayGround.Animation.Editor`), IMGUI 기반.
메뉴: `UPlayGround/캐릭터/액터/애니메이션 에디터`.
파일 위치는 모두 `Assets/02.Scripts/Editor/`이며 이 폴더에는 별도 `asmdef`가 없어 **기본 `Assembly-CSharp-Editor`** 에 컴파일된다.

| 파일 | 줄수 | 역할 |
|------|------|------|
| `MotionSetWindow.cs` | 3,291 | 메인. `OnGUI`(1329행)에서 툴바 → 액터 모션 사이드바(수동 스크롤 + 검색 + 키보드 포커스) → 재생 컨트롤 → 탭 → 본문 순서로 그린다. 테스트 액터 레지스트리, 플레이모드 프리뷰, 빈 상태 화면 포함 |
| `MotionSetDrawer.cs` | 2,407 | 타임라인. `DrawTimeline`(1538행)이 Rect 수동 계산으로 룰러·몽타주·타이밍·노티파이·오버레이 트랙과 커서를 그림. `DrawFullGUI`(261행)는 `GUILayoutUtility.GetRect` splitter로 2단(인스펙터/타임라인) 분할. 클립 트림 핸들 드래그(1912행~), 이벤트 바 드래그, 타이밍 마커, 줌 슬라이더 + 스크롤휠, `DrawObjectFieldsInspector`(559행) 리플렉션 인스펙터 |
| `MotionSetWindow.ControlPanels.cs` | 86 | 보조 패널 5종 탭 스트립(`DrawControlPanelTabs`). `EditorPrefs` 영속화(`MotionSetWindow_PanelTab`, `MotionSetWindow_PanelHelp`). `RunControlPanelSideEffects()`가 패널 표시 여부와 무관하게 `LoadCombatPrefsOnce()` + `RefreshCombatOverlayTracks()`를 매 프레임 실행 |
| `MotionSetWindow.RootMotion.cs` | 349 | 루트 모션 패널 + SceneView 기즈모 |
| `MotionSetWindow.WarpBake.cs` | 274 | 워프 베이크 패널 |
| `MotionSetWindow.WarpTarget.cs` | 449 | 워프 타깃 패널 + SceneView 핸들 |
| `MotionSetWindow.CombatOverlay.cs` | 593 | 전투 오버레이 트랙 갱신 + 히트박스 기즈모 |
| `MotionSetWindow.CaptureBridge.cs` | 189 | 촬영 연동 패널 |
| `MotionSetWindow.SlashVFXSceneTune.cs` | 1,054 | Slash VFX 씬 튜닝 + SceneView 핸들 |
| `MotionEventAddPopup.cs` | 613 | 이벤트 추가 팝업 |
| `MotionEventStyle.cs` | 70 | 색/스타일 상수 |
| `MotionSetEditor.cs` | 339 | 인스펙터(Inspector) 진입점, `_drawer` 공유 |

---

## 4. 범위와 비범위

### 4.1 범위 (전환 대상)

- 창 셸: 툴바, 보조 패널 탭 스트립, 2단/3단 분할 레이아웃
- 액터 모션 사이드바: 수동 스크롤 리스트 → `ListView` 가상화 + 검색
- 이벤트 인스펙터: 리플렉션 `DrawObjectFieldsInspector` → `PropertyField` + `SerializedObject` 바인딩
- 보조 패널 5종의 UI 부분
- 타임라인: Rect + `Event.current` 렌더/드래그 → 커스텀 `VisualElement` + `Painter2D` + `Manipulator`

### 4.2 비범위 (그대로 유지)

- **SceneView `Handles` API 코드** — `DrawCombatHitboxGizmo`, `DrawSlashVfxSceneTuneHandle`, `DrawWarpTargetSceneHandle`, `DrawRootMotionGizmo` 등. UI Toolkit과 무관하며 그대로 유지한다.
- **플레이모드 프리뷰 로직** — Animancer 재생, `PlayerActor` 입력 잠금, 캐릭터 스왑. UI가 아닌 실행 로직이므로 전환하지 않는다.
- 각 패널의 **비즈니스 로직**(베이크 계산, 오버레이 트랙 산출, 프리셋 저장) — UI만 교체하고 로직은 재사용한다.
- MotionSet 데이터 모델, `MotionEventBase` 계층, `AbilitySetSO` 파이프라인 — 데이터는 손대지 않는다.

---

## 5. 확정 설계 결정

| ID | 결정 |
|----|------|
| D-01 | 전환은 4개 Phase(A/B/C/D) 하이브리드로 진행하며 각 Phase 종료 시 에디터가 동작해야 한다 |
| D-02 | 미전환 영역은 `IMGUIContainer`로 감싸 UI Toolkit 셸 안에서 그대로 동작시킨다 |
| D-03 | 프로젝트 첫 공통 USS 디자인 시스템을 신설하고, 신규 UI Toolkit 코드는 인라인 스타일 대신 USS 클래스를 사용한다 |
| D-04 | 이벤트 인스펙터는 `SerializedObject`/`SerializedProperty` 바인딩을 표준으로 하고 리플렉션 인스펙터를 제거한다 |
| D-05 | 다시 그리기는 **변경 시에만** 발생시킨다. 상시 `Repaint`/`MarkDirtyRepaint` 습관을 금지한다 |
| D-06 | `RefreshCombatOverlayTracks()` 등 매 프레임 부작용은 탭 visibility가 아니라 `schedule.Execute` 주기 실행으로 분리한다 |
| D-07 | 타임라인 드래그 Undo(`Undo.RecordObject`)는 UI Toolkit에서도 수동 처리하며 기존 시맨틱을 그대로 이식한다 |
| D-08 | 신규 파일 네임스페이스는 폴더 경로 기반 `UPlayGround.*` 규칙을 따른다 |

---

## 6. 타깃 레이아웃

선례인 `GameplayAbilityEditorWindow`(`CreateGUI`에서 `BuildToolbar`/`BuildTabs`/`BuildMain` 구성)의 3열 구조를 참고하되, 타임라인이 있으므로 하단에 타임라인 독을 둔다.

```
┌─────────────────────────────────────────────────────────────────────────┐
│ [툴바] 에셋 선택 ▾  임시셋  |  ◀ ▶ 재생/정지  속도 ▾   검색 [        ]    │  ← 상단 툴바
├─────────────────────────────────────────────────────────────────────────┤
│ [탭] 루트모션 | 워프 | 이벤트 디버그 | 전투 오버레이 | 촬영 연동   ⓘ도움말 │  ← 보조 패널 탭 스트립
│  (선택된 패널 본문: 기본 전부 닫힘 → 타임라인이 위로)                     │
├────────────┬──────────────────────────────────────────┬───────────────────┤
│ 사이드바   │           재생 컨트롤 + 프리뷰 정보        │    인스펙터        │
│ [검색   ]  │                                            │  (선택 이벤트/     │
│ ▸ 모션 A   │  ┌──────────────────────────────────────┐  │   모션 프로퍼티)   │
│ ▸ 모션 B   │  │  타임라인 (Painter2D VisualElement)  │  │  PropertyField     │
│ ▸ 모션 C   │  │  룰러 / 몽타주 / 타이밍 / 노티파이 / │  │  바인딩             │
│ (ListView  │  │  오버레이 / 재생 커서                │  │                    │
│  가상화)   │  └──────────────────────────────────────┘  │                    │
│            │      ◀── TwoPaneSplitView(수평) ──▶        │                    │
└────────────┴──────────────────────────────────────────┴───────────────────┘
       ◀────────────── TwoPaneSplitView(수평, 좌 사이드바 / 우 나머지) ─────────▶
```

- 좌: 모션 리스트 + 검색 (`ListView`)
- 중앙: 재생 컨트롤 + 타임라인 독
- 우: 선택 대상 인스펙터 (`PropertyField`)
- 상단: 툴바 + 보조 패널 탭

---

## 7. USS 디자인 시스템

### 7.1 파일 배치 (제안)

신규 폴더 `Assets/02.Scripts/Editor/UIToolkit/` 를 만들고 다음을 둔다.

```
Assets/02.Scripts/Editor/UIToolkit/
├── Styles/
│   ├── UPlayGroundEditor.uss        # 공통 팔레트·변수·기본 컴포넌트
│   └── MotionEditor.uss             # 애니메이션 에디터 전용 스타일
├── MotionEditorShell.cs             # Phase A 셸 (제안, UPlayGround.Animation.Editor.UIToolkit)
├── MotionListView.cs                # Phase B 사이드바 (제안)
├── MotionEventInspectorView.cs      # Phase B 인스펙터 (제안)
└── Timeline/
    ├── TimelineView.cs              # Phase D 루트 VisualElement (제안)
    ├── TimelineTrackElement.cs      # 트랙 1줄 (제안)
    └── TimelineManipulators.cs      # 드래그/줌/스냅 Manipulator (제안)
```

네임스페이스는 `UPlayGround.Animation.Editor.UIToolkit`(및 `.Timeline`)로 폴더 경로를 따른다.
`.uss`/`.cs` 는 기본 `Assembly-CSharp-Editor`에 그대로 컴파일되므로 새 asmdef는 불필요하다.

### 7.2 변수 팔레트

`GameplayAbilityEditorWindow`의 인라인 색(`Bg0~Bg2`, `Border`, `Accent`)을 USS 커스텀 프로퍼티로 승격한다. 기존 IMGUI 색상(`MotionEventStyle.cs`, `COL_BG`, 트랙 색)과 톤을 맞춘다.

```css
:root {
    --up-bg-0: rgb(14, 19, 26);      /* 창 배경 */
    --up-bg-1: rgb(20, 26, 33);      /* 패널 */
    --up-bg-2: rgb(28, 33, 41);      /* 툴바/헤더 */
    --up-border: rgb(56, 69, 82);
    --up-accent: rgb(46, 133, 235);
    --up-text: rgb(224, 230, 240);
    --up-text-dim: rgb(160, 168, 180);

    --up-space-1: 4px;
    --up-space-2: 8px;
    --up-space-3: 12px;
    --up-radius: 4px;
    --up-font-sm: 11px;
    --up-font-md: 12px;
}
```

트랙 색(몽타주/타이밍/노티파이/오버레이)도 `--up-track-*` 변수로 정의해 타임라인 `Painter2D`가 참조하게 한다(`resolvedStyle`로 읽거나 코드 상수 미러링).

### 7.3 테마 대응 방침

- **권장**: Unity 에디터 테마 전환에 대응하려면 다크/라이트 각각의 값을 별도 클래스(`.up-theme-dark`/`.up-theme-light`)로 두고, `EditorGUIUtility.isProSkin`으로 루트에 토글한다.
- **선택**: 초기 버전은 다크 고정으로 시작(현재 에디터가 사실상 다크 전용)하고, 라이트 대응은 팔레트 변수만 갖춰 후속으로 미룬다.

---

## 8. Phase A — 셸 전환

### 8.1 목표

`OnGUI` 진입점을 `CreateGUI` 기반 UI Toolkit 셸로 교체한다. 본문 로직은 손대지 않고 `IMGUIContainer`로 감싸 **동작 무손실**을 보장한다.

### 8.2 작업 항목

- **신규**: `UIToolkit/MotionEditorShell.cs`, `Styles/UPlayGroundEditor.uss`
- **수정**: `MotionSetWindow.cs` — `OnGUI` 제거, `CreateGUI` 추가. `rootVisualElement`에
  - `Toolbar`(에셋 선택/재생/검색) — 기존 `DrawToolbar` 내용을 `IMGUIContainer`로 임시 래핑 가능
  - 보조 패널 탭 스트립 (Phase C 전까지 기존 `DrawControlPanelTabs`를 `IMGUIContainer`로 래핑)
  - `TwoPaneSplitView`(좌 사이드바 / 우 본문) 골격
  - 본문 = `IMGUIContainer(() => { DrawMotionSetEditorBody or DrawActorSetEditorLayout; })`
- **수정**: `RunControlPanelSideEffects()` 호출을 `OnGUI` 흐름에서 떼어 `rootVisualElement.schedule.Execute(...).Every(N)`로 이전 (D-06)
- **유지**: `HandlePlaybackShortcuts` → 루트 `RegisterCallback<KeyDownEvent>`로 이관하거나, 과도기에는 `IMGUIContainer` 내부에 유지

### 8.3 UX/디자인 개선

- 상단 툴바 시각 정리(그룹 구분선, 아이콘)
- 창 최소 크기 지정(`minSize`), 사이드바/인스펙터 폭 `EditorPrefs` 영속화

### 8.4 완료 기준 (DoD)

- `OnGUI`가 사라지고 창이 `CreateGUI`로만 구성된다
- 기존 모든 기능(사이드바, 타임라인, 5개 패널, 재생)이 이전과 동일하게 동작
- `RefreshCombatOverlayTracks()`가 패널 닫힘 상태에서도 계속 호출됨(전투 트랙 갱신 유지)

### 8.5 검증 절차 (Unity 수동)

1. 메뉴로 창 열기 → 에셋 선택 → 타임라인 표시 확인
2. 보조 패널 5개 전부 열고 닫아 동작 확인
3. 전투 오버레이 패널을 **닫은 채** 전투 데이터 변경 → 타임라인 오버레이 트랙이 갱신되는지 확인
4. 재생/정지/속도 조절 확인, 비재생 시 CPU 유휴(프로파일러) 확인

### 8.6 예상 위험

- `IMGUIContainer` 내부 IMGUI가 매 프레임 그려져 유휴 부하 발생 → 과도기 한정, Phase D에서 해소. 필요 시 컨테이너에 `MarkDirtyRepaint` 트리거를 명시적으로만 건다.
- 키보드 단축키(Space/S) 포커스 충돌 → `KeyDownEvent`에서 `focusController` 상태 확인.

---

## 9. Phase B — 사이드바 · 인스펙터

### 9.1 목표

모션 리스트를 `ListView` 가상화 + 검색으로, 이벤트 인스펙터를 `PropertyField` + `SerializedObject` 바인딩으로 전환한다. 리플렉션 인스펙터(`DrawObjectFieldsInspector`)를 제거한다.

### 9.2 작업 항목

- **신규**: `UIToolkit/MotionListView.cs`, `UIToolkit/MotionEventInspectorView.cs`, `Styles/MotionEditor.uss`
- **사이드바**: 수동 스크롤/검색/키보드 포커스(`_actorMotionListHasKeyboardFocus`) 로직을 `ListView`(가상화) + `ToolbarSearchField`로 교체. 그룹 라벨(`GetActorKeyGroupLabel`, `ACTOR_KEY_RANGES`)은 헤더 아이템 또는 `foldout` 그룹으로 재현
- **인스펙터**: `MotionSet`을 `SerializedObject`로 열고, 선택 이벤트/모션에 대해 `PropertyField`를 바인딩. `[SerializeReference]` 이벤트 클래스도 `PropertyField`가 자동 렌더(관리형 참조 지원)
- **삭제**: `DrawObjectFieldsInspector`, `DrawSingleField`, `DrawListProperty`(리플렉션 경로). 단, 타임라인이 아직 IMGUI인 동안은 인스펙터만 먼저 UI Toolkit 패널로 띄우고 기존 코드는 Phase D 완료까지 병존 가능

### 9.3 UX/디자인 개선

- 검색 즉시 필터, 그룹 접기, 선택 항목 하이라이트
- 인스펙터에서 표준 Unity 필드 UX(우클릭 되돌리기, prefab override 표시) 확보

### 9.4 완료 기준 (DoD)

- 모션 리스트가 `ListView`로 가상화되어 대량 항목에서도 스크롤 부드러움
- 이벤트/모션 편집이 `SerializedProperty` 경로로 이뤄지고 Undo/Redo가 표준 동작
- 리플렉션 인스펙터 코드가 제거(또는 참조 0)

### 9.5 검증 절차 (Unity 수동)

1. 다수 AnimKey를 가진 액터 세트에서 스크롤/검색/그룹 접기 확인
2. `[SerializeReference]` 이벤트(예: `MotionEvent_SlashVFX`, `MotionEvent_MotionWarp`) 필드 편집 → 값 반영·직렬화 확인
3. 편집 후 Ctrl+Z/Ctrl+Y로 Undo/Redo 정상 동작
4. 편집 결과가 에셋에 저장(`SetDirty`)되는지 확인

### 9.6 예상 위험

- **`[SerializeReference]` 이벤트 클래스**를 다른 어셈블리로 이동하면 `[MovedFrom(true, sourceAssembly: "...")]` 없이는 역직렬화 실패(CLAUDE.md 규칙). **인스펙터 전환은 클래스를 이동하지 않으므로 원칙적으로 안전**하나, 정리 과정에서 이동이 끼어들지 않도록 주의.
- 사이드바 키보드 포커스 시맨틱(`_actorMotionListHasKeyboardFocus`)이 `ListView` 기본 포커스와 다를 수 있음 → 동작 매핑 표를 만들어 이관.

---

## 10. Phase C — 보조 패널 5종

### 10.1 목표

이미 탭화된(`DrawControlPanelTabs`) 5개 패널을 패널 단위로 하나씩 UI Toolkit으로 전환한다. 로직은 재사용하고 UI만 교체한다.

### 10.2 작업 항목 (패널별 독립)

| 순번 | 패널 | 소스 파일 | 비고 |
|------|------|-----------|------|
| 1 | 촬영 연동 | `CaptureBridge.cs`(189) | 가장 작음, 파일럿으로 우선 |
| 2 | 워프 베이크 | `WarpBake.cs`(274) | |
| 3 | 루트 모션 | `RootMotion.cs`(349) | SceneView 기즈모는 유지 |
| 4 | 워프 타깃 | `WarpTarget.cs`(449) | SceneView 핸들은 유지 |
| 5 | 전투 오버레이 | `CombatOverlay.cs`(593) | `RefreshCombatOverlayTracks` 주기 실행과 UI 분리(D-06) |

- **수정**: `ControlPanels.cs` — 탭 스트립을 `Toolbar` + `ToggleButtonGroup` 또는 탭 버튼으로 교체. `EditorPrefs`(`MotionSetWindow_PanelTab`, `MotionSetWindow_PanelHelp`) 키는 그대로 유지해 사용자 설정 보존
- 각 패널의 UI 구성 메서드(`DrawXxxControls`)를 대응 `VisualElement` 빌더로 이관, 계산 메서드는 그대로 호출

### 10.3 완료 기준 (DoD)

- 5개 패널이 모두 UI Toolkit으로 렌더되고 탭 전환/도움말 토글 유지
- `RefreshCombatOverlayTracks()`가 전투 오버레이 패널 UI와 **독립적으로** 계속 실행됨
- 각 패널의 SceneView 기즈모/핸들이 이전과 동일하게 동작

### 10.4 검증 절차 (Unity 수동)

1. 패널별로 열고 값 변경 → 기능(베이크/오버레이/촬영) 결과 확인
2. 창 재시작 후 마지막 선택 탭·도움말 상태 복원 확인
3. SceneView에서 기즈모/핸들 표시·드래그 확인

### 10.5 예상 위험

- 패널 로직이 `MotionSetEditorWindow` partial 필드에 강결합 → 이관 시 상태 필드는 그대로 두고 UI만 분리
- 탭 visibility에 부작용을 묶는 실수 재발 금지(현재 코드가 의도적으로 분리해 둔 이유)

---

## 11. Phase D — 타임라인 재작성

### 11.1 목표

`MotionSetDrawer`의 Rect + `Event.current` 타임라인(`DrawTimeline` 등)을 커스텀 `VisualElement` + `generateVisualContent`(`Painter2D`) + `Manipulator`(드래그/줌/스냅)로 대체한다. 가장 크고 이득이 큰 작업.

### 11.2 작업 항목

- **신규**: `UIToolkit/Timeline/TimelineView.cs`, `TimelineTrackElement.cs`, `TimelineManipulators.cs`
- **렌더**: 룰러 / 몽타주 / 타이밍 / 노티파이 / 오버레이 트랙 / 재생 커서를 `generateVisualContent`의 `Painter2D`로 그린다. 트랙별 요소를 `VisualElement`로 두되, 바 내부 채색은 Painter2D로 처리
- **입력**: 클립 트림 핸들 드래그(`MotionSetDrawer.cs` 1912행~), 이벤트 바 시작/끝/바디 드래그, 타이밍 마커 드래그, 커서 스크럽, 줌(슬라이더 + 스크롤휠), 가로 스크롤을 각각 `Manipulator`로 구현(`MouseDownEvent`/`MouseMoveEvent`/`MouseUpEvent`/`WheelEvent`)
- **인스펙터 분할**: `DrawFullGUI`의 `GUILayoutUtility.GetRect` splitter(279행)를 `TwoPaneSplitView`로 대체(Phase A/B와 정합)
- **삭제**: `MotionSetDrawer.cs`의 IMGUI 타임라인/드래그 경로. `MotionSetEditor.cs`(인스펙터 진입점)가 `_drawer`를 공유하므로 **인스펙터에서도 새 타임라인을 재사용**하도록 함께 이관

### 11.3 다시 그리기 규칙 (필수)

- 플레이헤드는 **재생 중에만** `MarkDirtyRepaint()` 호출. 정지 중 상시 repaint 금지(D-05)
- 데이터/드래그 변경 시에만 해당 트랙 요소를 dirty 처리. 트리 전체 재스타일 금지
- **BT 에디터 회귀 교훈**: 매 갱신마다 전 노드를 무조건 재스타일해 게임 프레임드랍이 발생했었다. 타임라인은 처음부터 "변경분만 다시 그림" 구조로 설계한다.

### 11.4 Undo 규칙 (필수)

- 드래그 시작 시 `Undo.RecordObject`(`RecordUndo`) 호출 시맨틱을 그대로 이식(예: "Drag Clip Start"/"Drag Clip End"/"Drag Event Start"). UI Toolkit에서도 자동 처리되지 않으므로 수동 유지(D-07).

### 11.5 UX/디자인 개선

- 스냅(프레임/타이밍 마커) 옵션, 드래그 중 값 툴팁, 커서 정밀 스크럽
- 핸들 히트 영역 확대, 커서 모양(`cursor` USS) 명시

### 11.6 완료 기준 (DoD)

- IMGUI 타임라인/드래그 코드가 제거되고 창·인스펙터 모두 새 `TimelineView` 사용
- 클립 트림/이벤트/마커/커서/줌/스크롤이 이전과 동등하거나 개선된 조작감
- 정지 상태에서 유휴 CPU 0에 수렴(프로파일러 확인), 재생 시에만 repaint

### 11.7 검증 절차 (Unity 수동)

1. 클립 트림 핸들 드래그 → `clipStartTime`/`clipEndTime` 반영, Undo 확인
2. 이벤트 바 시작/끝/바디 드래그, 타이밍 마커 드래그 확인
3. 줌 슬라이더 + 스크롤휠 줌 + 가로 스크롤 확인
4. 재생 커서 스크럽 및 재생 중 커서 이동 확인
5. 인스펙터(`MotionSetEditor`)에서도 동일 타임라인 동작 확인
6. 창을 열어둔 채 Play Mode 진입 → 게임 프레임 저하 없는지 프로파일러로 확인(BT 회귀 방지)

### 11.8 예상 위험

- Painter2D 좌표/스크롤 오프셋 계산이 기존 `pps`(BASE_PPS × zoom), `scrollX`, `tOff` 로직과 어긋날 위험 → 기존 수식을 그대로 포팅하고 단위 테스트/시각 대조
- 창 밖 MouseUp/포커스 손실 시 드래그 미종료(기존 `HandleGlobalDragTermination`이 방어) → `PointerCaptureOutEvent`/`MouseCaptureController`로 동등 방어 구현

---

## 12. 위험 / 주의 요약

1. `RefreshCombatOverlayTracks()`는 전투 오버레이 패널이 **닫혀 있어도** 매 프레임 실행돼야 한다(타임라인 전투 트랙 갱신). 탭 visibility에 묶지 말고 `schedule.Execute` 주기 실행으로 옮긴다.
2. 타임라인 드래그 Undo(`RecordUndo` → `Undo.RecordObject`)는 UI Toolkit에서도 수동 처리한다. 기존 라벨/타이밍을 그대로 이식.
3. 플레이헤드는 **재생 중에만** `MarkDirtyRepaint()`. IMGUI 시절의 상시 Repaint 습관을 금지한다.
4. BT 에디터의 "열어두면 게임 프레임드랍"(매 갱신 무조건 재스타일)을 반복하지 않도록, 타임라인은 처음부터 변경분만 다시 그리는 구조를 강제한다.
5. `[SerializeReference]` 이벤트 클래스를 다른 어셈블리로 이동할 경우 `[MovedFrom(true, sourceAssembly: "...")]`가 필수(CLAUDE.md). 인스펙터 전환 자체는 클래스 이동을 요구하지 않으므로, 정리 중 무심코 이동시키지 않는다.

---

## 13. Phase별 체크리스트

### Phase A — 셸 전환
- [ ] `Styles/UPlayGroundEditor.uss` 신설, 팔레트 변수 정의
- [ ] `MotionEditorShell.cs` 신설, `CreateGUI` 진입점 구성
- [ ] `OnGUI` 제거, 툴바/탭/`TwoPaneSplitView` + 본문 `IMGUIContainer` 래핑
- [ ] `RunControlPanelSideEffects()`를 `schedule.Execute`로 이전
- [ ] 재생 단축키(Space/S) 이관 또는 과도기 유지
- [ ] 전 기능 무손실 + 오버레이 트랙 갱신 유지 검증

### Phase B — 사이드바 · 인스펙터
- [ ] `MotionListView.cs`(ListView 가상화 + 검색 + 그룹) 신설
- [ ] `MotionEventInspectorView.cs`(`PropertyField` + `SerializedObject`) 신설
- [ ] `[SerializeReference]` 이벤트 편집·직렬화·Undo 검증
- [ ] 리플렉션 인스펙터(`DrawObjectFieldsInspector` 등) 제거
- [ ] 키보드 포커스 시맨틱 매핑 확인

### Phase C — 보조 패널 5종
- [ ] 촬영 연동(파일럿)
- [ ] 워프 베이크
- [ ] 루트 모션 (SceneView 기즈모 유지)
- [ ] 워프 타깃 (SceneView 핸들 유지)
- [ ] 전투 오버레이 (주기 실행과 UI 분리)
- [ ] `EditorPrefs` 탭/도움말 상태 보존 검증

### Phase D — 타임라인 재작성
- [ ] `TimelineView` / `TimelineTrackElement` / `TimelineManipulators` 신설
- [ ] 룰러/몽타주/타이밍/노티파이/오버레이/커서 Painter2D 렌더
- [ ] 클립 트림·이벤트·마커·커서·줌·스크롤 Manipulator 구현
- [ ] splitter → `TwoPaneSplitView` 전환, 인스펙터도 새 타임라인 재사용
- [ ] Undo 라벨 이식, 드래그 종료 방어(`PointerCaptureOut`)
- [ ] 정지 시 유휴 0 / 재생 시에만 repaint 프로파일러 검증
- [ ] IMGUI 타임라인 경로 제거

---

## 14. 완료 정의 (전체)

- 4개 Phase 완료 후 `MotionSetWindow.cs`/`MotionSetDrawer.cs`의 IMGUI 렌더·드래그 경로가 제거된다.
- 창과 인스펙터가 공통 USS 디자인 시스템을 공유한다.
- SceneView 기즈모/핸들과 플레이모드 프리뷰 로직은 변경 없이 유지된다.
- 정지 상태 유휴 부하 0, 재생 시에만 repaint 하는 구조가 확립된다.
- 각 Phase 종료 시점 스냅샷에서 에디터가 항상 동작했음을 커밋 이력으로 확인할 수 있다.
