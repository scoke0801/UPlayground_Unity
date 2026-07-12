# 카메라 충돌 레이어 규약 가이드

## 개요

3인칭 카메라가 벽·지형에 막혀 캐릭터 쪽으로 당겨지는 충돌 처리에서, **무엇이 카메라를 막을 수 있는가**를 레이어 기준으로 정의하는 규약 문서다.

- 카메라 충돌은 **정적 환경 지오메트리에만** 반응한다 (화이트리스트 방식)
- 트리거 콜라이더는 카메라 충돌에서 **무조건 제외**된다 (`QueryTriggerInteraction.Ignore`)
- 캐릭터·NPC·인터랙션 오브젝트는 카메라를 당기지 않는다
- 카메라에 막혀야 하는 대형 오브젝트는 물리 차단용 콜라이더를 규약 레이어에 배치한다

### 배경 (2026-07 수정 사례)

인터랙션 대상 근처에서 카메라가 캐릭터 등에 붙을 정도로 당겨지는 현상이 발생했다. 원인은 두 가지 구조적 허점의 결합이었다.

1. 카메라 충돌 마스크가 **제외(exclude) 방식**이었다 — 전체 레이어에서 Player/Enemy/Npc/Projectile/Trigger만 빼는 구조라, 규약을 모르는 새 오브젝트(퀘스트 트리거 볼륨 등)가 Default 레이어에 저작되면 자동으로 카메라 차단체가 됐다.
2. 프로젝트 물리 설정이 `Queries Hit Triggers = true`인데 카메라 물리 쿼리가 `QueryTriggerInteraction`을 지정하지 않아, **트리거 볼륨도 카메라를 밀어냈다**. 실제 범인은 씬에 Default 레이어로 저작된 퀘스트 트리거 박스(`TriggerComposer`)였다.

---

## 규약

### 1. 카메라 충돌 레이어 화이트리스트

카메라를 막을 수 있는 레이어는 `CameraConfig.CollisionIncludeLayer`에 나열된 것뿐이다.

```csharp
// Assets/02.Scripts/Data/Config/CameraConfig.cs
public static readonly string[] CollisionIncludeLayer = new string[]
{
    "Default",
    "Ground",
};
```

| 레이어 | 카메라 충돌 | 용도 |
|--------|:---:|------|
| Default | O | 벽, 대형 구조물, 정적 환경 프랍 |
| Ground | O | 지형, 바닥 |
| Player / Enemy / Npc | X | 캐릭터 (카메라를 당기면 안 됨) |
| InteractableObject | X | 채집물·휴식지점 등 인터랙션 감지용 |
| Trigger | X | 퀘스트·이벤트 트리거 볼륨 |
| Projectile / HitBox | X | 전투 판정용 |

새 레이어를 추가할 때 카메라에 막혀야 한다면 `CollisionIncludeLayer`에 **명시적으로 추가**해야 한다. 기본값은 "카메라를 막지 않음"이다.

### 2. 트리거 콜라이더는 카메라를 절대 막지 않는다

카메라 계열의 모든 물리 쿼리는 `QueryTriggerInteraction.Ignore`를 명시한다. 프로젝트 전역 설정(`Queries Hit Triggers = true`)에 의존하지 않는다.

적용 위치:

| 파일 | 쿼리 |
|------|------|
| `Camera/CameraCollision.cs` | 충돌 SphereCast, 멀티프로브 Linecast, FloorRescue Raycast x2 |
| `Camera/Modifiers/CollisionCameraModifier.cs` | SafeBack SphereCast, 지면 관통 방지 Raycast |
| `Manager/CameraManager.cs` | 경사 피치 보정 Raycast |
| `Camera/CameraLockOn.cs` | 시야(LoS) SphereCast/Raycast (기존부터 적용됨) |

카메라 관련 코드에 물리 쿼리를 새로 추가할 때도 반드시 `QueryTriggerInteraction.Ignore`를 붙인다.

### 3. 트리거 볼륨은 Trigger 레이어에 저작한다

퀘스트 트리거, 이벤트 볼륨, 인터랙션 범위 감지 등 `isTrigger = true` 콜라이더는 **Trigger 레이어(13)** 에 배치한다. Default 레이어에 트리거를 두면 카메라 외에도 적 시야 차폐 판정 등 Default 마스크를 쓰는 다른 물리 쿼리에 오탐을 일으킨다.

물리 충돌 매트릭스는 전 레이어 상호 충돌 허용 상태이므로, Trigger 레이어로 옮겨도 `OnTriggerEnter` 등 이벤트 수신에는 영향이 없다.

### 4. 대형 인터랙션 오브젝트의 콜라이더 분리

집·대형 바위처럼 **카메라가 뚫고 들어가면 안 되는** 인터랙션 오브젝트는 콜라이더를 역할별로 분리한다.

```
대형 인터랙션 오브젝트 (예: 휴식지점 건물)
├── Body (Layer: Default)            ← 물리 차단용 solid 콜라이더. 카메라·이동을 막는다
└── InteractRange (Layer: InteractableObject, isTrigger)
                                     ← 인터랙션 감지용. GameInteractionHandler의
                                        OverlapSphere(_interactionLayer)에 잡힌다
```

- 물리 차단 콜라이더: `Default` 레이어, `isTrigger = false` → 카메라와 캐릭터 이동을 막음
- 인터랙션 감지 콜라이더: `InteractableObject` 레이어 → 카메라는 무시, 인터랙션 감지만 수행
- 채집물처럼 작은 오브젝트는 차단 콜라이더 없이 감지 콜라이더만 두면 된다

플레이어의 인터랙션 감지 마스크(`PlayerActor._interactionLayer`)는 `InteractableObject + Npc`이므로, 감지 콜라이더는 이 두 레이어 중 하나에 있어야 잡힌다.

---

## 관련 코드 흐름

```
CameraConfig.GetCollisionLayerMask()          ← 화이트리스트 → 마스크 생성
        ↓
CameraManager.Init()                          ← _collisionLayers 캐싱
        ↓
CameraContext.CollisionLayers                 ← Modifier 파이프라인에 전달
        ↓
CollisionCameraModifier (Priority 800)
    ├── CameraCollision.Evaluate()            ← 멀티프로브 차폐 감지 + 거리 스무딩
    ├── ResolveSafeBackPosition()             ← 클리핑 방지 백스톱 SphereCast
    ├── ResolveGroundPenetration()            ← 지면 관통 방지
    ├── CameraCollision.ApplyFloorRescue()    ← 낭떠러지 바닥 구조
    └── ResolveCharacterCapsuleExclusion()    ← 플레이어 캡슐 내부 진입 방지
```

---

## 주의 사항

- **NPC는 루트 캡슐만 Npc 레이어면 충분하지 않다** — 자식에 콜라이더를 추가할 경우 그 자식의 레이어도 확인할 것. 씬 NPC들의 `HitBox` 자식은 Npc 레이어(12)를 사용한다 (`NPC_Default.prefab` 포함).
- **Water 레이어는 현재 카메라를 막지 않는다** — 수면에서 카메라가 물속으로 들어가는 것이 문제가 되면 `CollisionIncludeLayer`에 `"Water"`를 추가해 대응한다.
- **FloorRescue 전용 마스크** — `CameraSettings.floorRescueLayerMask`가 0이면 충돌 마스크를 그대로 쓴다. 지면 판정을 별도 레이어로 제한하고 싶을 때만 설정한다.
- 화이트리스트 전환으로 TransparentFX·UI·HitBox·CharacterPreview 등 기타 레이어도 더 이상 카메라를 막지 않는다. 의도된 동작이다.
