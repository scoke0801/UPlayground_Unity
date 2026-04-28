# Map Placement Tool 가이드

## 개요

Map Placement Tool은 씬 뷰에서 지형 또는 충돌체 위 위치를 찍어 적, NPC, 포탈 같은 배치 오브젝트를 빠르게 배치하는 에디터 도구다.

핵심 특징:

| 기능 | 설명 |
|------|------|
| 씬 클릭 배치 | 씬 뷰에서 좌클릭한 월드 위치에 선택 프리팹을 배치 |
| ActorDatabase 연동 | `ActorDefinitionSO.prefab`을 사용해 몬스터/NPC를 배치하고 `_actorId`와 NPC 데이터를 자동 주입 |
| 직접 프리팹 배치 | `PortalActor`처럼 `GameActor`가 아닌 프리팹도 직접 선택해 배치 |
| 표면/그리드 보정 | Raycast hit 위치, 표면 노멀 정렬, 그리드 스냅, Y 오프셋 지원 |
| 부모 컨테이너 정리 | 배치된 오브젝트를 `MapPlacementRoot` 또는 사용자가 지정한 부모 아래로 정리 |

---

## 아키텍처

```
MapPlacementEditorWindow (EditorWindow)
    │
    ├── ActorDatabase
    │     └── ActorDefinitionSO
    │           ├── prefab → GameActor / MonsterActor / NpcActor
    │           └── npcData → NpcActorSO
    │
    ├── Direct Prefab
    │     └── GameObject prefab → PortalActor / Trigger / 기타 배치물
    │
    └── SceneView.duringSceneGui
          └── Raycast 또는 Plane hit → PrefabUtility.InstantiatePrefab
```

### 파일 구조

```
Assets/
├── 02.Scripts/
│   └── Tool/
│       └── Editor/
│           └── Map/
│               └── MapPlacementEditorWindow.cs
└── docs/
    └── MAP_PLACEMENT_TOOL_GUIDE.md
```

---

## 핵심 클래스

### MapPlacementEditorWindow

| 항목 | 설명 |
|------|------|
| 메뉴 | `UPlayGround/Map/Map Placement Tool` |
| 타입 | `EditorWindow` |
| 주요 입력 | `ActorDatabase`, `ActorDefinitionSO`, 직접 프리팹, Raycast LayerMask |
| 주요 출력 | 씬에 배치된 프리팹 인스턴스 |

주요 설정:

| 필드 | 설명 |
|------|------|
| Placement Source | `ActorDatabase` 또는 직접 프리팹 중 배치 소스 선택 |
| Actor Filter | ActorDatabase 목록을 `Monster`, `NPC`, `Combat`, `Talkable` 등으로 필터 |
| Parent | 배치된 인스턴스를 넣을 부모 Transform |
| Auto Create Root | 부모가 없을 때 `MapPlacementRoot`를 자동 생성 |
| Align To Surface | 표면 노멀 기준으로 배치물의 Up 방향 정렬 |
| Snap To Grid | 클릭 위치를 지정 간격 그리드로 스냅 |
| Random Yaw | 배치 시 Y축 회전을 무작위 적용 |

---

## 셋업 방법

1. Unity 상단 메뉴에서 `UPlayGround/Map/Map Placement Tool`을 연다.
2. 몬스터/NPC를 배치할 때는 `ActorDatabase`를 연결한다.
3. NPC를 배치할 때는 `UPlayGround/NPC/NPC Data Generator`로 `NpcActorSO`와 NPC용 `ActorDefinitionSO`를 생성하거나, 해당 `ActorDefinitionSO.npcData`에 `NpcActorSO`를 직접 연결한다.
4. 포탈을 배치할 때는 `직접 프리팹` 모드로 `Assets/03.Prefabs/Actor/Portal/` 아래 프리팹을 연결한다.
5. 필요한 경우 `Parent`를 씬의 정리용 Transform으로 지정한다.
6. `배치 모드`를 켠 뒤 씬 뷰에서 좌클릭한다.

---

## 사용 예시

### 몬스터 배치

1. `Source`를 `ActorDatabase`로 둔다.
2. `Actor Filter`에서 `Monster`를 선택한다.
3. 목록에서 원하는 `ActorDefinitionSO`를 선택한다.
4. 씬 뷰 지형 위를 클릭한다.
5. 배치된 프리팹의 `GameActor._actorId`는 선택한 `ActorDefinitionSO.actorId`로 자동 주입된다.

### NPC 배치

1. `UPlayGround/NPC/NPC Data Generator`에서 Actor ID, 표시 이름, `dialogueGraph`를 입력한다.
2. `NPC 데이터 생성`을 실행해 `NpcActorSO`와 NPC용 `ActorDefinitionSO`를 생성한다.
3. 생성기는 `actorType`을 `NPC | Talkable`로 설정하고, NPC 프리팹과 `npcData`를 Definition에 자동 연결한다.
4. Map Placement Tool에서 해당 `ActorDefinitionSO`를 선택해 씬 뷰에 배치한다.
5. 배치된 씬 인스턴스에는 `_actorId`와 `NpcActor._data`가 함께 주입된다.

### 포탈 배치

1. `Source`를 `Direct Prefab`으로 변경한다.
2. `Portal_Full.prefab`, `Portal_OnlyGate.prefab` 등 포탈 프리팹을 연결한다.
3. 씬 뷰에서 배치 위치를 클릭한다.
4. 배치 후 Inspector에서 `PortalActor`의 타겟 씬 또는 목적지 Transform을 설정한다.

---

## 에디터 도구

| 메뉴 경로 | 기능 |
|-----------|------|
| `UPlayGround/Map/Map Placement Tool` | 씬 클릭 기반 몬스터/NPC/포탈 배치 |
| `UPlayGround/NPC/NPC Data Generator` | `NpcActorSO` 생성 및 NPC용 `ActorDefinitionSO.npcData` 자동 연결 |
| `UPlayGround/Generator Tool/NPC Data Generator` | Generator Tool 아래에서 여는 동일 기능 alias |

작업 방식:

| 조작 | 결과 |
|------|------|
| 좌클릭 | 현재 선택한 프리팹을 씬에 배치 |
| Shift + 좌클릭 | 기존 씬 선택을 방해하지 않고 연속 배치 |
| Ctrl/Cmd + Z | Unity Undo로 마지막 배치 취소 |
| ESC | 배치 모드 해제 |

---

## 주의 사항

| 항목 | 설명 |
|------|------|
| 런타임 스폰과 구분 | 이 도구는 씬에 프리팹 인스턴스를 직접 배치한다. 런타임 동적 스폰은 `ActorSpawnManager.SpawnActor`를 사용한다. |
| ActorDatabase 항목 | `ActorDefinitionSO.prefab`이 비어 있으면 배치할 수 없다. |
| NPC 데이터 | NPC는 `ActorDefinitionSO.npcData`에 `NpcActorSO`를 연결해야 대화 상호작용이 동작한다. |
| PortalActor | `PortalActor`는 `GameActor`가 아니므로 ActorDatabase가 아니라 직접 프리팹 모드로 배치한다. |
| 씬 저장 | 배치 후 씬은 dirty 상태가 되며 Unity에서 씬 저장이 필요하다. |
| LayerMask | 지형/바닥 Collider가 Raycast LayerMask에 포함되어 있어야 정확한 표면에 배치된다. |

---

## 확장 포인트

| 확장 | 방향 |
|------|------|
| 배치 프리셋 | 자주 쓰는 포탈/NPC/트리거 프리팹 목록을 ScriptableObject로 저장 |
| 브러시 배치 | 반경과 개수를 지정해 몬스터 무리를 랜덤 분산 배치 |
| 그룹 배치 | `MonsterGroupController`를 자동 생성하고 몬스터들을 그룹 하위로 배치 |
| 미니맵 연동 | 배치된 포탈/NPC에 `MinimapMarkerRegistrar` 기본값을 자동 설정 |
