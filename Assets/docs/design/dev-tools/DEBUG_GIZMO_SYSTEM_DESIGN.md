# 디버깅 기즈모 시스템 설계

## 개요

디버깅 기즈모 시스템은 전투, AI, 이동, 카메라, 투사체처럼 런타임 상태를 눈으로 확인해야 하는 기능을 한 곳에서 켜고 끄기 위한 개발용 시각화 계층이다.

현재 프로젝트는 `EnemyDetection`, `PlayerCombat`, `MotionWarpController`, `CameraManager`, `MonsterGroupController`, 투사체 스크립트가 각자 `OnDrawGizmosSelected()` 또는 `OnDrawGizmos()`를 직접 구현한다. 이 방식은 빠르게 확인하기에는 좋지만, 기능이 늘어날수록 다음 문제가 커진다.

- 어떤 기즈모가 어디서 그려지는지 찾기 어렵다.
- 전투/AI/이동 정보가 한 화면에 겹쳐 가독성이 떨어진다.
- Play Mode 중 대상 하나만 추적하거나 카테고리별로 토글하기 어렵다.
- 재현이 어려운 버그를 나중에 프레임 단위로 되감아 볼 수 없다.

따라서 이 설계는 기존 기즈모를 즉시 제거하지 않고, `DebugGizmoManager` 중심의 카테고리 토글, 선택 대상 추적, 런타임 오버레이, 프레임 스냅샷 기록을 단계적으로 도입하는 방향이다.

---

## 공개 사례 조사 요약

### Unreal Gameplay Debugger

Unreal Engine의 Gameplay Debugger는 Play In Editor, Simulate, Standalone 세션에서 런타임 게임플레이 데이터를 게임 뷰포트 오버레이로 보여주는 도구다. 기본 카테고리로 Pawn, AIController, Behavior Tree, Blackboard, EQS, Perception, Navmesh 정보를 제공하고, 프로젝트별 카테고리를 C++로 확장할 수 있다.

이 사례에서 가져올 점:

- 화면에 모든 정보를 항상 띄우지 않고 카테고리 단위로 제한한다.
- 디버그 대상 액터를 하나 선택하고 그 액터 중심의 데이터를 표시한다.
- AI/BT/Perception/Navmesh처럼 서로 다른 하위 시스템을 동일한 디버깅 UI에서 전환한다.
- 디버그 데이터 수집과 표시를 분리한다.

출처: Unreal Engine Gameplay Debugger 문서  
https://dev.epicgames.com/documentation/en-us/unreal-engine/using-the-gameplay-debugger-in-unreal-engine

### Unreal Visual Logger

Unreal Engine의 Visual Logger는 액터 상태, 텍스트 로그, 디버그 도형을 기록하고 에디터에서 타임라인으로 되감아 볼 수 있는 도구다. 특히 한 프레임만 바뀌는 AI 상태 변수나 드물게 재현되는 버그를 게임 영상만으로 추적하기 어려울 때 유용하다고 설명한다.

이 사례에서 가져올 점:

- 실시간 표시와 사후 분석을 분리한다.
- 액터별 스냅샷, 텍스트 로그, 디버그 도형을 같은 시간축에 묶는다.
- 기록된 세션에서 액터 리스트, 상태 카테고리, 텍스트 로그, 타임라인 스크럽을 제공한다.
- Shipping 빌드에는 포함되지 않도록 컴파일 가드를 둔다.

출처: Unreal Engine Visual Logger 문서  
https://dev.epicgames.com/documentation/en-us/unreal-engine/visual-logger-in-unreal-engine

### Unity Gizmos / Handles

Unity는 `OnDrawGizmos()`와 `OnDrawGizmosSelected()`에서 Scene View 또는 Game View 리페인트 시점에 기즈모를 그리도록 제공한다. Gizmos 메뉴는 Scene View와 Game View 양쪽에 있고, 스크립트별 아이콘/기즈모 표시를 제어할 수 있다.

이 사례에서 가져올 점:

- Unity 기본 기능과 충돌하지 않게 `OnDrawGizmosSelected()` 기반 경량 표시를 유지한다.
- 프로젝트 전역 토글은 Unity Gizmos 메뉴와 별개로 관리한다.
- `UnityEditor.Handles` 의존 코드는 반드시 `#if UNITY_EDITOR` 안에 둔다.
- 런타임 빌드용 오버레이는 Gizmos가 아니라 별도 UI/LineRenderer/GL 계층으로 분리한다.

출처: Unity Gizmos 메뉴 / Gizmos API 문서  
https://docs.unity3d.com/Manual/GizmosMenu.html  
https://docs.unity3d.com/ScriptReference/Gizmos.html

### 게임 디버깅 연구 사례

2025년 게임 디버깅 병목 연구는 게임 개발에서 일반 디버거 외에도 On-Screen Console, Debug Draws, Debug Camera, Cheats, In-Game Menus, Data Scrubbing이 실제로 쓰인다고 정리한다. 또한 관찰 대상 개발자들이 많은 시간을 게임 아티팩트 검사와 로컬 재현에 사용한다고 보고한다.

이 사례에서 가져올 점:

- 기즈모 시스템은 단순 도형 그리기가 아니라 재현, 검사, 데이터 스크럽을 줄이는 개발 워크플로여야 한다.
- 치트 콘솔, 디버그 카메라, 인게임 메뉴와 연결해야 효과가 커진다.
- 거대한 범용 툴보다 자주 보는 전투/AI/카메라 정보를 빠르게 켜고 끄는 것이 우선이다. 사용 빈도가 낮은 기능은 나중에 붙인다.

출처: Identifying Video Game Debugging Bottlenecks: An Industry Perspective  
https://arxiv.org/abs/2510.08834

---

## 프로젝트 적용 방향

### 설계 원칙

| 원칙 | 설명 |
|------|------|
| 에디터 우선 | Unity Editor와 Play Mode에서 먼저 유용해야 한다. |
| 기존 기즈모 유지 | 기존 `OnDrawGizmosSelected()` 코드를 즉시 제거하지 않고 어댑터로 점진 이관한다. |
| 카테고리 중심 | 전투, AI, 이동, 카메라, 투사체, 스폰/그룹을 독립 토글한다. |
| 대상 중심 | 선택 액터 1개와 주변 관련 액터를 우선 표시한다. |
| 무할당 지향 | 매 프레임 문자열/리스트 할당을 피하고, 필요 시 캐시와 버퍼를 둔다. |
| 빌드 격리 | 에디터 전용 코드는 `#if UNITY_EDITOR`, 개발 빌드 전용 런타임 코드는 `DEVELOPMENT_BUILD` 조건으로 제한한다. |

### 우선순위

1. 실시간 카테고리 토글
2. 선택 대상 액터 디버그 패널
3. 기존 전투/AI/이동 기즈모 통합
4. 프레임 스냅샷 기록과 타임라인 리뷰
5. 인게임 개발 빌드 오버레이

---

## 아키텍처

```
GameManager
    └── DebugGizmoManager
          ├── DebugGizmoSettingsSO
          ├── DebugGizmoRegistry
          │     └── IDebugGizmoProvider[]
          ├── DebugGizmoDrawContext
          ├── DebugGizmoFrameRecorder
          └── DebugGizmoRuntimeOverlay

IDebugGizmoProvider
    ├── EnemyDetectionDebugProvider
    ├── PlayerCombatDebugProvider
    ├── MotionWarpDebugProvider
    ├── CameraDebugProvider
    ├── ProjectileDebugProvider
    └── MonsterGroupDebugProvider
```

### 파일 구조 제안

```
Assets/02.Scripts/Debug/Gizmo/
├── Runtime/
│   ├── DebugGizmoManager.cs
│   ├── DebugGizmoCategory.cs
│   ├── DebugGizmoSettingsSO.cs
│   ├── DebugGizmoDrawContext.cs
│   ├── DebugGizmoFrameSnapshot.cs
│   ├── DebugGizmoFrameRecorder.cs
│   ├── IDebugGizmoProvider.cs
│   └── Providers/
│       ├── EnemyDetectionDebugProvider.cs
│       ├── PlayerCombatDebugProvider.cs
│       ├── MotionWarpDebugProvider.cs
│       ├── CameraDebugProvider.cs
│       ├── ProjectileDebugProvider.cs
│       └── MonsterGroupDebugProvider.cs
└── Editor/
    ├── DebugGizmoWindow.cs
    ├── DebugGizmoSceneDrawer.cs
    └── DebugGizmoSnapshotViewer.cs
```

`Runtime` 폴더에 두더라도 `UnityEditor.Handles`, `EditorWindow`, `SceneView.duringSceneGui`는 반드시 `Editor` 폴더 또는 `#if UNITY_EDITOR`에 둔다.

---

## 핵심 타입 설계

### DebugGizmoCategory

카테고리는 `[Flags]` enum으로 둔다. 토글과 필터링이 빠르고, `CheatConsoleWindow` 스타일의 에디터 UI에서도 다루기 쉽다.

```csharp
namespace UPlayGround.Debugging
{
    [System.Flags]
    public enum DebugGizmoCategory
    {
        None       = 0,
        Combat     = 1 << 0,
        AI         = 1 << 1,
        Movement   = 1 << 2,
        Camera     = 1 << 3,
        Projectile = 1 << 4,
        SpawnGroup = 1 << 5,
        Animation  = 1 << 6,
        All        = ~0,
    }
}
```

### IDebugGizmoProvider

각 시스템은 자신이 아는 데이터를 제공만 하고, 전역 토글/선택/기록 정책은 매니저가 판단한다.

```csharp
namespace UPlayGround.Debugging
{
    public interface IDebugGizmoProvider
    {
        DebugGizmoCategory Category { get; }
        Object Owner { get; }
        bool IsAvailable { get; }

        void CollectSnapshot(DebugGizmoFrameSnapshot snapshot);
        void DrawGizmos(DebugGizmoDrawContext context);
    }
}
```

`Owner`는 `GameObject`, `Component`, `ScriptableObject`까지 받을 수 있게 `UnityEngine.Object`로 둔다. 선택 대상 필터는 `Owner`가 붙은 `GameObject` 또는 부모 `GameActor`를 기준으로 처리한다.

### DebugGizmoDrawContext

기즈모 호출마다 공통 상태를 넘긴다.

```csharp
namespace UPlayGround.Debugging
{
    public readonly struct DebugGizmoDrawContext
    {
        public readonly DebugGizmoCategory EnabledCategories;
        public readonly GameObject FocusObject;
        public readonly bool DrawLabels;
        public readonly bool DrawOnlyFocus;
        public readonly float MaxDrawDistance;
        public readonly float Time;

        public bool IsEnabled(DebugGizmoCategory category)
            => (EnabledCategories & category) != 0;
    }
}
```

### DebugGizmoManager

`GameManager`에 등록하는 런타임 매니저다. EditorWindow와 치트 콘솔은 이 매니저의 상태를 조작한다.

| 책임 | 내용 |
|------|------|
| Provider 등록 | `RegisterProvider`, `UnregisterProvider` |
| 전역 토글 | 카테고리별 ON/OFF, 라벨 표시, 포커스 대상 |
| 런타임 기록 | Play Mode 중 프레임 스냅샷 ring buffer 유지 |
| 씬 전환 정리 | `OnSceneChanged`에서 죽은 Provider 제거 |
| 개발 빌드 오버레이 | 필요 시 Game View용 간단 텍스트/라인 표시 |

`DebugGizmoManager`는 `CheatManager`와 비슷하게 개발용 매니저로 유지한다. 다만 기능이 커지므로 `CheatManager` 내부에 넣지 않고 독립 매니저로 둔다.

---

## 카테고리별 통합 대상

### AI

현재 대상:

- `EnemyDetection.OnDrawGizmosSelected()`
- `EnemyAIController`, `EnemyFlyingAIController`의 상태/의사결정 정보
- `EnemyTacticalMemory`
- Behavior Tree 런타임 상태

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 탐지 범위 | 노란 원 |
| 추적 해제 범위 | 빨간 원 |
| 아군 탐지 범위 | 청록 원 |
| 시야각 | 부채꼴 경계선 |
| 현재 타겟 | 초록 라인 |
| 현재 AI 상태 | 액터 머리 위 라벨 |
| BT 현재 노드 | 선택 대상 패널 텍스트 |

1차 이관은 `EnemyDetection`에 `EnemyDetectionDebugProvider`를 붙이는 방식이 가장 안전하다. 기존 `OnDrawGizmosSelected()`는 유지하되, 전역 시스템이 활성화되면 Provider 경로를 우선 사용한다.

### Combat

현재 대상:

- `PlayerCombat.OnDrawGizmosSelected()`
- `EnemyCombat` 공격 데이터
- `CombatHitDetector`
- `MotionSetWindow.CombatOverlay`

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 현재 공격 판정 반경 | 빨간 원 또는 부채꼴 |
| `HitPhaseData` 높이 범위 | 상하 원 + 수직 라인 |
| 콤보/캔슬 가능 상태 | 라벨 |
| 타격 후보 | 대상 라인 및 색상 |
| 가드/패리 판정 | 전방 각도 표시 |

전투 기즈모는 디자이너가 가장 자주 볼 가능성이 높으므로, `Combat` 카테고리는 기본 ON 후보로 둔다. 단, 라벨은 화면을 많이 가리므로 선택 대상일 때만 표시한다.

### Movement

현재 대상:

- `MotionWarpController`의 디버그 기즈모
- KCC `ActorMovementController.CurrentState`
- impulse, root motion, motion warp target

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 현재 상태명 | 액터 머리 위 라벨 |
| motion warp target | 라인 + 구 |
| min/max 거리 | 원 |
| 도달 가능 거리 | 원 |
| predictive 위치 | 라인 + 구 |
| impulse 벡터 | 화살표 |

`MotionWarpController`는 이미 디버그 정보가 잘 정리되어 있으므로 `MotionWarpDebugProvider`로 분리하기 좋다. 특히 `_gizmoLabelSb`처럼 문자열 빌더를 재사용하는 패턴은 전역 시스템에서도 유지한다.

### Camera

현재 대상:

- `CameraManager.OnDrawGizmosSelected()`
- 락온 타겟, 카메라 오프셋, 충돌 보정

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 카메라 pivot | 노란 구 |
| 카메라 위치 라인 | 청록 라인 |
| 락온 범위 | 초록/빨간 원 |
| 현재 락온 타겟 | 빨간 라인 + 구 |
| 카메라 모드 | Game View 오버레이 텍스트 |

카메라 디버그는 Scene View뿐 아니라 Game View에서도 필요하다. Unity Gizmos의 Game View 표시 여부에 의존하지 않도록 V2에서 `DebugGizmoRuntimeOverlay`를 둔다.

### Projectile

현재 대상:

- `LinearProjectile.OnDrawGizmosSelected()`
- `AOEProjectile.OnDrawGizmosSelected()`
- `ArcingProjectile.OnDrawGizmosSelected()`

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 충돌 반경 | 노란 원 |
| 이전 위치와 현재 위치 | 빨간 라인 |
| AOE 반경 | 반투명 구 + 와이어 |
| 곡사 목표 위치 | 구 + 궤적 샘플 |

투사체는 인스턴스 수가 많을 수 있으므로 `MaxDrawDistance`, `DrawOnlyFocus`, 샘플 수 제한이 필요하다.

### SpawnGroup

현재 대상:

- `MonsterGroupController.OnDrawGizmos()`
- 그룹 슬롯, 오너, 멤버 연결

표시할 정보:

| 정보 | 표시 방식 |
|------|----------|
| 그룹 중심 | 구 |
| 멤버 연결 | 라인 |
| 슬롯 위치 | 작은 와이어 구 |
| 오너/리더 | 강조 색 |

항상 표시하면 맵 작업에서 유용하지만 전투 디버깅에는 방해가 될 수 있으므로 기본 OFF를 권장한다.

---

## 에디터 도구

### DebugGizmoWindow

메뉴 경로:

```csharp
[MenuItem("UPlayGround/Debug/Debug Gizmo Window")]
```

기능:

| 기능 | 설명 |
|------|------|
| Play 상태 표시 | `CheatConsoleWindow`와 같은 헤더 패턴 |
| 카테고리 토글 | Combat, AI, Movement, Camera, Projectile, SpawnGroup |
| 포커스 대상 | 현재 Selection 또는 수동 핀 |
| 라벨 표시 | 라벨 ON/OFF |
| 거리 제한 | Scene 카메라 기준 최대 표시 거리 |
| 스냅샷 녹화 | Start/Stop/Clear |
| 타임라인 리뷰 | 녹화된 프레임 인덱스 스크럽 |

### SceneView 연동

`SceneView.duringSceneGui`에서 전역 드로어가 Provider를 순회해 그린다. 이 경로는 에디터 전용이다.

```csharp
#if UNITY_EDITOR
using UnityEditor;

namespace UPlayGround.Debugging.Editor
{
    [InitializeOnLoad]
    public static class DebugGizmoSceneDrawer
    {
        static DebugGizmoSceneDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!Application.isPlaying)
                return;

            DebugGizmoManager.Instance?.DrawSceneViewGizmos(sceneView.camera);
        }
    }
}
#endif
```

---

## 런타임 스냅샷

### 목적

실시간 기즈모는 버그가 지나간 뒤에는 정보를 잃는다. AI가 한 프레임만 잘못된 상태로 바뀌거나, MotionWarp 타겟이 짧게 유실되는 문제는 프레임 기록이 있어야 분석이 쉽다.

### 데이터 구조

```csharp
namespace UPlayGround.Debugging
{
    public sealed class DebugGizmoFrameSnapshot
    {
        public int Frame;
        public float Time;
        public readonly List<DebugGizmoTextEntry> Texts = new();
        public readonly List<DebugGizmoShapeEntry> Shapes = new();
    }
}
```

V1에서는 단순히 최근 10초 ring buffer만 유지한다. 파일 저장은 V2로 미룬다.

### 기록 대상

| 데이터 | V1 | V2 |
|--------|----|----|
| 액터 이름/ID | O | O |
| 현재 상태명 | O | O |
| AI 타겟 | O | O |
| 공격 판정 | O | O |
| MotionWarp 상태 | O | O |
| 라인/원/구 도형 | 일부 | O |
| JSON 저장 | X | O |
| 타임라인 에디터 | 간단 스크럽 | 상세 뷰 |

---

## 셋업 방법

### 1. DebugGizmoManager 등록

`GameManager.InitializeManagers()`에서 `CheatManager` 근처에 등록한다.

```csharp
RegisterManager(DebugGizmoManager.Instance);
RegisterManager(CheatManager.Instance);
```

디버그 기능은 다른 매니저의 상태를 읽어야 하므로 초기화 순서는 핵심 런타임 매니저 뒤, `CheatManager` 근처가 적절하다.

### 2. DebugGizmoSettingsSO 생성

권장 경로:

```
Assets/10.Datas/Debug/DebugGizmoSettings.asset
```

주요 필드:

| 필드 | 기본값 | 설명 |
|------|--------|------|
| `defaultCategories` | `Combat | AI | Movement` | Play Mode 시작 시 기본 표시 |
| `drawLabels` | true | 라벨 표시 |
| `drawOnlyFocus` | false | 선택 대상만 표시 |
| `maxDrawDistance` | 60 | Scene 카메라 기준 표시 거리 |
| `recordFrames` | false | 프레임 기록 여부 |
| `recordSeconds` | 10 | ring buffer 길이 |

### 3. Provider 부착

가장 안전한 시작점:

- `EnemyDetectionDebugProvider`
- `MotionWarpDebugProvider`
- `PlayerCombatDebugProvider`

각 Provider는 대상 컴포넌트와 같은 GameObject에 붙이거나, 대상 컴포넌트가 `OnEnable()`에서 직접 등록한다.

```csharp
private void OnEnable()
{
    if (Application.isPlaying)
        DebugGizmoManager.Instance?.RegisterProvider(this);
}

private void OnDisable()
{
    DebugGizmoManager.Instance?.UnregisterProvider(this);
}
```

### 4. 기존 기즈모 이관

기존 코드는 다음 순서로 옮긴다.

1. 기존 `OnDrawGizmosSelected()` 코드를 Provider의 `DrawGizmos()`로 복사한다.
2. 전역 시스템이 비활성화된 경우 기존 로컬 기즈모가 계속 보이도록 둔다.
3. 전역 시스템이 안정화되면 로컬 `_drawGizmos` 필드를 `DebugGizmoManager` 카테고리 토글로 대체한다.

---

## 구현 단계

### Phase 1: 전역 토글과 Provider 등록

목표:

- `DebugGizmoManager`
- `DebugGizmoCategory`
- `IDebugGizmoProvider`
- `DebugGizmoWindow`
- AI/Movement Provider 2종

완료 기준:

- Play Mode에서 Window를 열고 AI/Movement 카테고리를 켜고 끌 수 있다.
- 선택 대상만 표시하는 옵션이 동작한다.
- 기존 `EnemyDetection`, `MotionWarpController` 기즈모와 같은 정보를 볼 수 있다.

### Phase 2: 전투/카메라/투사체 통합

목표:

- `PlayerCombatDebugProvider`
- `CameraDebugProvider`
- `ProjectileDebugProvider`
- 색상 팔레트와 라벨 규칙 통일

완료 기준:

- 전투 판정과 카메라 락온 정보를 같은 창에서 토글한다.
- 투사체가 많은 상황에서도 Scene View가 눈에 띄게 느려지지 않는다.

### Phase 3: 프레임 스냅샷 기록

목표:

- `DebugGizmoFrameRecorder`
- 최근 N초 ring buffer
- 간단 타임라인 스크럽

완료 기준:

- Play Mode 중 녹화 시작/정지/초기화가 가능하다.
- 선택 프레임의 액터 상태와 주요 도형을 에디터 창에서 다시 볼 수 있다.

### Phase 4: 개발 빌드 오버레이

목표:

- `DebugGizmoRuntimeOverlay`
- 개발 빌드 전용 입력 토글
- Game View 텍스트 패널

완료 기준:

- `DEVELOPMENT_BUILD`에서만 오버레이가 컴파일된다.
- 카테고리별 간단 텍스트와 선택 대상 상태를 게임 화면에서 볼 수 있다.

---

## 성능 정책

| 항목 | 정책 |
|------|------|
| 문자열 | 라벨 문자열은 `StringBuilder` 재사용 |
| Provider 목록 | 등록/해제 기반 리스트 유지, 매 프레임 `FindObjectsOfType` 금지 |
| 거리 제한 | Scene 카메라와 대상 거리 기준으로 culling |
| 투사체 | 기본은 선택 대상 또는 근거리만 표시 |
| 스냅샷 | 고정 크기 ring buffer |
| 빌드 포함 | Editor 코드는 Editor 폴더, 런타임 오버레이는 개발 빌드 조건 |

---

## 색상 규칙

| 의미 | 색상 |
|------|------|
| 공격/피해 | Red |
| 경고/범위 한계 | Yellow |
| 정상 타겟/성공 | Green |
| 탐지/센서 | Cyan |
| 이동/도달 가능 | Blue |
| 예측/미래 위치 | Magenta |
| 비활성/과거 스냅샷 | Gray |

현재 코드에 이미 가까운 색상 규칙이 있으므로 새 팔레트는 기존 색을 크게 바꾸지 않는다.

---

## 주의 사항

- `UnityEditor.Handles.Label`은 런타임 빌드에 포함되면 안 된다.
- `OnDrawGizmos()`는 선택하지 않은 오브젝트도 계속 그리므로, 많은 액터가 있는 씬에서는 기본 사용을 줄인다.
- Provider가 대상 컴포넌트의 private 필드를 직접 읽어야 한다면, reflection을 쓰지 말고 디버그용 읽기 전용 public property를 추가한다.
- `DebugGizmoManager.Instance`가 없는 에디터 정지 상태에서도 기존 기즈모가 깨지지 않아야 한다.
- Scene View 기즈모와 Game View 오버레이는 같은 데이터 모델을 쓰되 렌더링 경로는 분리한다.

---

## 확장 포인트

### Behavior Tree

Behavior Tree 에디터와 런타임 상태를 연결하면 선택 몬스터의 현재 노드, 블랙보드 키, 최근 전이 사유를 Scene View와 BT 에디터 양쪽에서 볼 수 있다.

### Balance Designer

`BalanceDesignerWindow`와 연결해 전투 시뮬레이션 결과를 기즈모로 재생할 수 있다. 예를 들어 N초 전투 분석 중 공격 판정, 이동 궤적, 피격 프레임을 Scene View에 표시한다.

### CombatDataValidator

공격 데이터 검증 경고를 Scene View에서 직접 표시할 수 있다. 예를 들어 `hitRange`가 너무 작거나 `hitHeightRange`가 0인 공격을 선택하면 노란 경고 라벨을 표시한다.

### Debug Camera

Unreal의 Debug Camera 사례처럼 선택 액터 궤도 회전, 버퍼/모드 전환까지 확장할 수 있다. Unity에서는 기존 카메라 시스템과 충돌하지 않도록 `CameraManager`의 모드로 넣기보다 별도 에디터 도구로 시작하는 편이 안전하다.

---

## 권장 결론

이 프로젝트에는 대형 AAA식 완성형 디버거보다 `전투/AI/이동을 빠르게 켜고 끄는 통합 기즈모`가 먼저 필요하다. V1은 `DebugGizmoManager + Provider + EditorWindow`만 구현하고, Visual Logger식 타임라인 기록은 V2로 미루는 것이 작업 대비 효과가 가장 크다.

가장 먼저 이관할 대상은 다음 3개다.

1. `EnemyDetection` - AI 감지 범위와 타겟 확인 빈도가 높다.
2. `MotionWarpController` - 이미 디버그 데이터가 잘 노출되어 있고 전투 이동 튜닝에 중요하다.
3. `PlayerCombat` - 공격 판정/각도/거리 튜닝에 직접 필요하다.

이 세 영역이 안정화되면 `CameraManager`, `Projectile`, `MonsterGroupController`를 순서대로 추가한다.
