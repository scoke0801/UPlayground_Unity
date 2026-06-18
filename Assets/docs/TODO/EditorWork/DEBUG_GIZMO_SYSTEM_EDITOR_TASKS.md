# 디버그 기즈모 시스템 에디터 작업

## 목적

중앙집중식 디버그 기즈모 시스템(`DebugGizmoManager` + `IDebugGizmoProvider`)을 동작시키기 위한 Unity 에디터 수작업을 정리한다.

코드 구현은 완료되어 있으며, 이 문서는 에디터에서 사람이 직접 해야 하는 연결/검증 작업만 다룬다.

**핵심 전제:** 이 시스템은 **에디터 전용**이다. `DebugGizmoManager`는 `#if UNITY_EDITOR`에서만 `GameManager`에 등록되고, provider 등록 static 진입점도 빌드에서는 no-op이다. 따라서 빌드에는 매니저·provider 비용·설정 에셋이 들어가지 않는다.

## 관련 코드 (참고)

- 매니저: `Assets/02.Scripts/Debugging/Gizmo/Runtime/DebugGizmoManager.cs`
- 설정 SO: `Assets/02.Scripts/Debugging/Gizmo/Runtime/DebugGizmoSettingsSO.cs`
- 에디터 창: `Assets/02.Scripts/Debugging/Gizmo/Editor/DebugGizmoWindow.cs`
- 등록 지점: `Assets/02.Scripts/Manager/GameManager.cs` (`#if UNITY_EDITOR` 가드)

## 1. DebugGizmoSettings 에셋 생성

설정 SO 인스턴스를 만든다.

- 프로젝트 창에서 우클릭 → **Create → UPlayGround → Debug → Debug Gizmo Settings**
- 저장 위치 권장: `Assets/10.Datas/Debug/DebugGizmoSettings.asset`
  - (디버그 전용 데이터 폴더가 따로 있으면 그쪽에 둬도 무방)

필드 권장값:

| 필드 | 권장값 | 비고 |
|------|--------|------|
| `defaultCategories` | `Combat \| AI \| Movement` | 시작 시 켜둘 카테고리 |
| `defaultContentTypes` | `All` | content-type 필터 |
| `drawLabels` | `true` | Handles 라벨 표시 |
| `drawOnlyFocus` | `false` | 포커스 오브젝트만 그릴지 |
| `maxDrawDistance` | `60` | 씬 카메라 기준 컬링 거리 |
| `recordFrames` | `false` | 프레임 스냅샷 레코더 |
| `recordSeconds` | `10` | 레코더 링버퍼 길이(초) |

> 로드 전/실패 시에는 `DebugGizmoManager`의 필드 기본값이 사용되므로, 에셋을 안 만들어도 시스템 자체는 기본값으로 동작한다. 다만 위 설정으로 시작 상태를 제어하려면 에셋이 필요하다.

## 2. Addressables 등록

`DebugGizmoManager.Init()`은 `#if UNITY_EDITOR`에서 다음 키로 설정을 로드한다.

```csharp
private const string SettingsAddressableKey = "DebugGizmoSettings";
```

작업:

1. **Window → Asset Management → Addressables → Groups** 열기
2. 1단계에서 만든 `DebugGizmoSettings.asset`을 그룹으로 드래그(또는 인스펙터에서 `Addressable` 체크)
3. 해당 항목의 **Address(주소)를 정확히 `DebugGizmoSettings`로 설정** (대소문자 일치 필수)

주소가 다르면 로드가 실패하고 콘솔에 다음 경고가 뜬다.

```
[DebugGizmoManager] 'DebugGizmoSettings' Addressable 로드 실패: 기본값 사용
```

이 경우에도 크래시 없이 기본값으로 동작하지만, 설정값은 반영되지 않는다.

## 3. 빌드 제외 처리

이 설정 에셋은 **빌드에 포함될 필요가 없다.** 코드는 빌드에서 로드를 시도하지 않으므로(에디터 전용), 에셋만 빌드 대상에서 빼면 된다.

권장 방식 중 하나:

- **전용 그룹 분리:** 디버그/에디터 전용 Addressable 그룹을 따로 만들고, 그 그룹을 빌드 산출에서 제외한다.
  - 그룹 Schema의 `Include In Build` 옵션을 끄거나, 빌드 스크립트에서 해당 그룹을 빌드 대상에서 제외
- 또는 `Editor` 전용 Addressable 그룹/라벨 컨벤션이 프로젝트에 이미 있으면 그것을 따른다.

> 코드 차원의 빌드 제외는 이미 끝났다(매니저 등록·static 진입점 모두 `#if UNITY_EDITOR`). 이 단계는 "에셋이 빌드에 묻어 들어가지 않게" 하는 정리 작업이다.

## 4. Debug Gizmo Window 사용법 (런타임 제어)

플레이 모드에서 카테고리/포커스/레코더를 토글하는 에디터 창이 있다.

- 메뉴: **UPlayGround → Debug → Debug Gizmo Window**
- 플레이 모드에서만 동작(에디트 모드에서는 안내 HelpBox 표시)
- Global / Categories / Content Types / Focus / Providers / Recorder 섹션 제공
- `Focus`의 **Selection** 버튼: 현재 선택 오브젝트를 포커스 대상으로 지정 → `Draw Only Focus`와 조합해 특정 액터만 디버그

## 5. 검증

### 5-1. 에디트 모드 기즈모 (Play 끈 상태)

1. 씬에서 적(`EnemyDetection` 보유) 오브젝트를 선택한다.
2. **Play를 켜지 않은 상태**에서 탐지/추적해제/아군 범위·시야각 기즈모가 보이는지 확인한다.
   - 중앙 매니저는 플레이 모드 전용이므로, 이 경로는 각 컴포넌트의 `OnDrawGizmosSelected`(로컬)가 담당한다.

### 5-2. 플레이 모드 단일 렌더링

1. Play 진입 후 적/플레이어를 선택한다.
2. 기즈모가 **정확히 한 번만** 그려지는지 확인한다.
   - 중앙 매니저가 그리고, 로컬 `OnDrawGizmosSelected`는 `DebugGizmoManager.ShouldSuppressLocalGizmos`로 억제되어 이중 렌더가 발생하지 않아야 한다.
3. Debug Gizmo Window에서 카테고리를 끄면 해당 기즈모가 사라지는지 확인한다.

### 5-3. 설정 로드 확인

1. Play 진입 시 콘솔에 `[DebugGizmoManager] 설정 로드 완료` 로그가 뜨는지 확인한다.
2. 실패 경고가 뜨면 2단계의 Addressable 주소(`DebugGizmoSettings`)를 다시 확인한다.

### 5-4. 빌드 제외 확인 (선택)

1. 개발 빌드를 만든다.
2. `DebugGizmoSettings` 에셋이 빌드 산출물(Addressable bundle)에 포함되지 않는지 확인한다.

## 참고

- 새 UI 프리팹/키 추가는 없다.
- `DebugGizmoManager`는 씬/프리팹에 배치하지 않는다. `GameManager`가 `#if UNITY_EDITOR`에서 `.Instance`로 자동 생성·등록한다.
- 설정 에셋을 안 만들어도 시스템은 기본값으로 동작하므로, 1~2단계는 "시작 상태를 데이터로 제어하고 싶을 때"의 작업이다.
- provider를 추가할 컴포넌트는 `IDebugGizmoProvider`를 구현하고 `OnEnable`/`AfterInit`에서 `DebugGizmoManager.RegisterProvider(this)`, 해제 시 `UnregisterProvider(this)`를 호출하면 된다(빌드에서는 자동 no-op).
