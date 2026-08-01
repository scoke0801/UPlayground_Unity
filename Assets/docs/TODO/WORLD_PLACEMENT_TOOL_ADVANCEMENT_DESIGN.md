# 월드 배치 도구 고도화 설계

대상: `Assets/02.Scripts/Tool/Editor/Map/GatheringPlacementEditorWindow.cs` (통합 배치 툴, 2988줄)
연관: `WorldPlacementBakeUtility.cs`, `WorldPlacementDataSO`, `WorldPlacementMetadata`, `MonsterGroupController`, `RuntimePlacementLoader`

---

## 0. 현행 진단

| 영역 | 현재 | 문제 |
|---|---|---|
| 배치 단위 | 클릭 1회 = 오브젝트 1개 | 조우(encounter) 하나 만드는 데 클릭 N회 + 수동 배치 정렬 |
| 몬스터 그룹 | 씬에 `MonsterGroupController`를 미리 만들고 부모만 지정 (`ShouldParentToGroup`) | 구성(멤버 조합/진형/역할)이 씬에만 존재. 재사용·복제·수정 전파 불가 |
| 배치 규칙 | 창 필드에 산재한 토글 8종 (`_surfaceSnapMode`, `_snapToGrid`, `_randomRotation`, `_raycastMask` …) | 캐릭터/채집물/바위마다 규칙이 다른데 매번 손으로 토글. 실수 시 지형에 파묻힘 |
| 검증 | 없음. 배치 후 눈으로 확인 | 경사 과다, 지형 관통, 겹침, NavMesh 밖 배치가 런타임에야 드러남 |
| 씬 전체 조망 | Bake 데이터 뷰어(읽기 전용, 마커 300개 상한)만 존재 | 씬에 이미 뭐가 몇 개 놓였는지 목록·검색·일괄 조작 수단 없음 |
| Undo | 배치 1건마다 Undo 엔트리 다수 | 그룹 배치로 확장하면 Ctrl+Z 수십 번 필요 |
| 파일 구조 | 단일 파일 2988줄 | 프로젝트의 `클래스명.기능.cs` partial 규약 미적용 |

---

## 1. 몬스터 배치 프리셋 (그룹 단위) — 핵심

### 1.1 개념

"조우 단위 저작". 프리셋 1개 = **그룹 파라미터 + 멤버 N개의 상대 배치**. 씬 뷰 클릭 한 번으로 `MonsterGroupController` 루트와 멤버 전원을 지형에 스냅해 생성한다.

### 1.2 데이터: `MonsterGroupPresetSO`

`Assets/02.Scripts/Data/World/MonsterGroupPresetSO.cs` (`UPlayGround.Data.World`)
에셋 위치 `Assets/10.Datas/World/GroupPreset/`
`[CreateAssetMenu(menuName = "UPlayGround/World/Monster Group Preset")]` — flat 2단계 도메인 규약 준수.

```csharp
[Serializable]
public sealed class MonsterGroupPresetMember
{
    public ActorDefinitionSO definition;     // 우선 소스
    public GameObject directPrefab;          // definition 없을 때 폴백
    public MemberPriority priority = MemberPriority.Normal;

    public Vector3 localOffset;              // 그룹 앵커 기준 (XZ 사용, Y는 스냅으로 재계산)
    public float localYaw;                   // 앵커 forward 기준 상대 yaw
    public Vector3 scale = Vector3.one;

    public int count = 1;                    // 2 이상이면 jitterRadius로 산개
    public float jitterRadius;
    public bool initiallyActive = true;
}

[Serializable]
public sealed class MonsterGroupPresetSettings   // MonsterGroupController 필드 스냅샷
{
    public int maxMeleeAttackers = 2;
    public int maxRangedAttackers = 2;
    public float breatherDuration = 0.6f;
    public int formationSlotCount = 8;
    // 필드 추가 시 MonsterGroupController와 1:1 유지 (아래 6.3 참조)
}

public sealed class MonsterGroupPresetSO : ScriptableObject
{
    [SerializeField] private string _presetId;        // 고유 키. bake record에 기록
    [SerializeField] private string _displayName;
    [SerializeField] private string _category;        // "숲 순찰", "보스 호위" 등 좌측 리스트 그룹핑
    [SerializeField] private MonsterGroupPresetSettings _settings = new();
    [SerializeField] private List<MonsterGroupPresetMember> _members = new();
    [SerializeField] private float _anchorRadiusHint = 6f;  // 씬 프리뷰 링 반경
}
```

**설계 의도 — 위치는 로컬 오프셋만 저장한다.** 월드 좌표를 저장하면 프리셋이 특정 지형에 종속된다. Y는 저장하지 않고 배치 시 각 멤버 지점에서 개별 레이캐스트로 결정한다(경사면에서 그룹이 통째로 떠 있거나 파묻히는 문제 방지).

### 1.3 배치 흐름

Actor 탭에 하위 모드 추가: `ActorPlacementSource`에 `GroupPreset = 2` 추가 (`ActorDatabase` / `DirectPrefab`과 동렬).

1. 좌측 패널이 프리셋 리스트로 전환 (카테고리 폴드아웃 + 검색, 기존 `DrawActorDefinitionList` 패턴 재사용).
2. 씬 뷰 프리뷰: 앵커 디스크(`_anchorRadiusHint`) + 멤버별 작은 디스크·이름 라벨. 멤버 지점 레이캐스트 실패 시 해당 멤버만 빨간색.
3. **마우스 다운→드래그로 앵커 방향(yaw) 결정**, 마우스 업에 확정. 드래그 거리가 임계 미만이면 씬 카메라 forward 기준 yaw로 즉시 배치(단발 클릭 호환).
4. 생성: 앵커 GameObject(`MonsterGroupController` + `MonsterGroupPresetLink`) → 멤버 인스턴스 N개를 앵커 하위에 생성. 각 멤버는 기존 `ApplyPositionRules` / `StickInstanceToSurface` / `AddSceneEntityIdIfNeeded` / `AddPlacementMetadataIfNeeded` 경로를 **그대로 재사용**한다(중복 구현 금지).
5. 전체를 단일 Undo 그룹으로 collapse (아래 1.6).

### 1.4 역방향 캡처 — "씬 그룹 → 프리셋 저장"

저작 비용을 실제로 줄이는 건 이쪽이다. 손으로 배치해 다듬은 그룹을 프리셋으로 굳힌다.

- 대상: 하이어라키에서 선택된 `MonsterGroupController`.
- 앵커: 그룹 트랜스폼. 멤버 오프셋 = `anchor.InverseTransformPoint(member.position)`의 XZ, yaw = 상대 yaw.
- 멤버 소스 복원: 멤버의 `WorldPlacementMetadata.SourceId`로 `ActorDefinitionSO` 역참조, 실패 시 프리팹 참조 폴백. **둘 다 실패하면 그 멤버는 건너뛰지 말고 캡처를 실패시킨다** (부분 캡처가 조용히 성공하면 프리셋이 조용히 빈다 — 에디터 데이터 도구 안전 규칙).
- 버튼 2종: `새 프리셋으로 저장` / `선택 프리셋 덮어쓰기`(확인 다이얼로그 필수).

### 1.5 프리셋 ↔ 배치 인스턴스 연결: `MonsterGroupPresetLink`

`Assets/02.Scripts/GameActor/Component/Common/MonsterGroupPresetLink.cs` (에디터 저작 정보만 보유, 런타임 로직 없음)

```csharp
[SerializeField] private string _presetId;
[SerializeField] private int _appliedRevision;   // 프리셋 저장 시 증가하는 리비전
```

- 프리셋 리비전 > 인스턴스 리비전이면 씬 뷰/창에 "프리셋이 갱신됨" 배지.
- `프리셋 변경 재적용` 버튼: **파괴적 경로**다. 반드시 (a) 확인 다이얼로그, (b) 단일 Undo 그룹, (c) 중간 실패 시 `Undo.RevertAllDownToGroup`으로 전체 롤백. 부분 적용 상태를 성공으로 처리하지 않는다.
- 재적용 정책: 위치만 재적용 / 멤버 구성까지 재적용 두 가지 모드. 기본은 전자(수동 미세조정 보존).

### 1.6 Undo 원자성

현재는 배치 1건에도 `RegisterCreatedObjectUndo` + `SetTransformParent` + `AddComponent`가 개별 엔트리로 쌓인다. 그룹 배치는 이게 곱해진다.

```csharp
int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName($"Place Group Preset: {preset.DisplayName}");
try { /* 앵커 + 멤버 전원 생성 */ }
catch { Undo.RevertAllDownToGroup(group); throw; }
finally { Undo.CollapseUndoOperations(group); }
```

**단발 배치에도 동일 적용한다** (Phase 0에서 선행 처리).

### 1.7 Bake / 런타임 연동

멤버는 지금처럼 **개별 `WorldPlacementRecord`로 bake**된다. `WorldPlacementBakeUtility`는 이미 `groupName`을 기록하고(`:575`) 복원 시 `MonsterGroupController`를 찾아 붙인다(`:106~110`).

추가는 필드 하나로 끝낸다.

```csharp
// WorldPlacementRecord
public string groupPresetId;   // 프리셋 유래 추적 / 텔레메트리 / 재구성용
```

**`RuntimePlacementLoader`와 런타임 스폰 경로는 변경하지 않는다.** 프리셋은 순수 저작 계층이며 런타임 계약을 건드리지 않는 것이 이 설계의 리스크 상한선이다.

> 주의: `groupName` 복원이 `GameObject.Find`(`:109`)라 동명 그룹이 있으면 오복원된다. 프리셋 도입으로 그룹 수가 급증하므로 **Phase 1에서 `groupName` → 앵커의 `SceneEntityId`(GUID) 기반 매칭으로 교체**한다. 이건 프리셋과 별개로 이미 존재하는 잠재 버그다.

---

## 2. 배치 규칙 프로필 (`PlacementRuleProfileSO`)

현재 산재한 규칙 토글 묶음을 에셋으로 저장·전환한다.

- 필드: `surfaceSnapMode`, `alignToSurface`, `snapToGrid`/`gridSize`, `randomRotation` + XYZ 범위, `heightOffset`, `raycastMask`, `ignoreTriggerColliders`, `autoSetupCollider`, `addSceneEntityId`, `placementBakeMode`.
- 기본 제공 프로필: `캐릭터`(LowerOnly, 랜덤회전 없음), `채집물`(LowerOnly, 랜덤 yaw), `바위/장식`(Full 스냅, 전축 랜덤 회전), `트리거/포탈`(스냅 없음, 콜라이더 자동설정 끔).
- 상단 툴바에 프로필 드롭다운 + `현재 설정을 프로필로 저장`.
- **프리셋 멤버는 프로필을 참조할 수 있다** — 그룹 안에서 몬스터와 장식물이 다른 규칙을 쓰는 경우 대응.

---

## 3. 배치 검증 게이트

### 3.1 배치 시점 (프리뷰 색으로 즉시 피드백)

| 규칙 | 판정 | 기본 동작 |
|---|---|---|
| 경사 각도 | `Vector3.Angle(hitNormal, up) > maxSlope` (기본 35°) | 경고 + 배치 허용 |
| 겹침 | 반경 내 기존 `WorldPlacementMetadata` 검색 (기본 0.5m) | 경고 |
| NavMesh 이탈 | `NavMesh.SamplePosition` 실패 (몬스터/NPC 한정) | 경고 |
| 지형 관통 | 스냅 후 렌더러 바운드 최저점이 표면보다 아래 | 차단 |
| 레이 미스 | 현행 `_hasPreviewHit == false` | 차단(현행 유지) |

그룹 프리셋 배치는 **멤버 단위로 판정**하고, 실패 멤버가 있으면 앵커 라벨에 `3/5 배치 가능`처럼 표시한다.

### 3.2 일괄 감사

`씬 배치 검증` 버튼 → 씬 전체 `WorldPlacementMetadata` 순회 후 리포트(항목 클릭 시 핑 + 프레임). 검출: 위 규칙 위반, 프리팹 참조 유실(고아), `SceneEntityId` 중복, bake 데이터와 씬 상태 불일치.

---

## 4. 씬 배치 인벤토리 패널

Bake 데이터 뷰어(읽기 전용) 옆에 **씬 현재 상태** 탭 추가.

- 트리: `그룹 / 배치 루트` → 멤버. 모드·ActorType·프리셋으로 필터, 이름 검색.
- 행 조작: 선택·프레임·삭제, **표면 재스냅**(지형 수정 후 일괄 보정 — 실사용 빈도 높음), 규칙 프로필 재적용.
- 카운터: 타입별 개수. 사이클 런 밀도 감각을 잡는 데 쓴다.

---

## 5. 스캐터 브러시 (채집물/장식물 대량 배치)

몬스터가 아닌 대량 배치물 전용 모드. 그룹 프리셋과 목적이 다르므로 별도 모드로 둔다.

- 드래그하는 동안 브러시 반경 내에 밀도만큼 산포. 최소 간격(포아송 디스크), 경사 상한, 레이어 필터 적용.
- `Ctrl+드래그` = 지우개(브러시 반경 내 해당 소스 배치물 제거).
- 배치 시드 고정 옵션 → 동일 브러시 스트로크 재현.
- 성능: 드래그 1스트로크 = Undo 1엔트리. 스트로크 종료 시점에만 `MarkSceneDirty`.

---

## 6. 부수 개선

### 6.1 파일 분할 (선행 필수)

프로젝트 규약(`클래스명.기능.cs`)에 맞춰 partial 분리:

```
GatheringPlacementEditorWindow.cs              // 필드, OnEnable/OnGUI 골격, 상태
GatheringPlacementEditorWindow.Actor.cs        // Actor 모드 UI/배치
GatheringPlacementEditorWindow.Interaction.cs  // Interaction/DropItem
GatheringPlacementEditorWindow.CycleSpawn.cs   // 사이클 스폰 마커
GatheringPlacementEditorWindow.GroupPreset.cs  // 신규 1장
GatheringPlacementEditorWindow.Brush.cs        // 신규 5장
GatheringPlacementEditorWindow.Scene.cs        // OnSceneGUI, 프리뷰, 스냅 유틸
GatheringPlacementEditorWindow.Bake.cs         // Bake 뷰어 / 인벤토리 / 검증
```

클래스명이 내용(액터·사이클·그룹 전반)과 어긋나므로 `WorldPlacementEditorWindow`로 개명 검토. 단 `PrefsPrefix = "UPlayground.GatheringPlacement."` 유지 시 사용자 설정이 보존되므로 **Prefs 키는 바꾸지 않는다**.

### 6.2 사이클 스폰 대시보드

사이클 규칙(외곽 보스 3 + 중앙 보스 1)에 맞춰 섹터별 마커 통계 표시: 역할별 개수, `_cycleSafetyRadius` 겹침, 섹터 미커버 경고. 현재는 마커를 찍을 수만 있고 규칙 충족 여부를 알 수 없다.

### 6.3 데이터 변경 시 인스펙터 동기화

`MonsterGroupPresetSettings`는 `MonsterGroupController`의 필드 미러다. 한쪽에 필드가 늘면 다른 쪽과 커스텀 인스펙터도 함께 갱신해야 한다. 드리프트 방지를 위해 EditMode 테스트 1개를 둔다: 리플렉션으로 양쪽 필드 집합을 비교해 미러 누락 시 실패.

### 6.4 잔여 함정

- `UPlayGround.Object` 네임스페이스가 존재하므로 신규 코드에서 `Object.FindObjectsByType` 등은 `UnityEngine.Object`로 명시(현행 파일도 그렇게 하고 있음).
- `MonsterGroupController.RegisterMember`는 런타임 등록 경로다. 에디터 배치는 **부모 관계만** 만들고 컴포넌트 등록을 흉내내지 않는다(현행 `ShouldParentToGroup` 정책 유지).
- `DrawBakedRecordMarkers`의 300개 상한은 인벤토리/그룹 프리뷰에도 동일하게 적용한다(씬 뷰 핸들 드로우가 프레임을 잡아먹음).

---

## 7. 단계 계획

| 단계 | 내용 | 산출물 |
|---|---|---|
| **P0** | partial 분리, Undo 그룹 collapse(단발 배치 포함) | 리팩터만. 기능 변화 없음 |
| **P1** | 몬스터 그룹 프리셋 (SO / 배치 / 역방향 캡처 / Link / bake 필드 1개 / `groupName`→GUID 매칭 교체) | 본 문서 1장 |
| **P2** | 배치 규칙 프로필 + 배치 시점 검증 게이트 | 2, 3.1장 |
| **P3** | 씬 인벤토리 패널 + 일괄 감사 | 3.2, 4장 |
| **P4** | 스캐터 브러시 | 5장 |
| **P5** | 사이클 스폰 대시보드 | 6.2장 |

P1이 단독으로 가치를 내므로 P0→P1까지가 최소 유효 범위다. P2 이후는 독립적이라 순서 교체 가능.

## 8. 테스트

- EditMode: 프리셋 캡처↔배치 왕복(오프셋/yaw 오차 허용범위 내 일치), 소스 역참조 실패 시 캡처 실패, 재적용 중간 실패 시 롤백으로 씬 오브젝트 수 불변, 설정 미러 필드 일치(6.3).
- 수동: 경사면 그룹 배치 시 멤버 개별 스냅, 그룹 배치 후 Ctrl+Z 1회로 전량 원복, bake→로드 후 그룹 소속 유지.
