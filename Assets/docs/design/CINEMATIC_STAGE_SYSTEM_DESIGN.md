# 연출 스테이지(Cinematic Stage) 시스템 설계 문서

> 작성일: 2026-07-31 · **개정: 2026-07-31 (v2)**
> 대상: Unity 6 (6000.0.60f1), URP
> 분류: **런타임 기반 구현 완료 / 콘텐츠 수직 슬라이스 검증 대기**.
> 관련 문서: `Assets/docs/design/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md`, `Assets/docs/TODO/ULTIMATE_SEQUENCE_EDITOR_ADVANCEMENT_TODO.md`
> 관련 코드: `GameActor/Component/Player/UltimateSequencePlayer.cs`, `UltimateGameplayLockContext.cs`, `UltimatePlacementContext.cs`, `GameActor/Combat/Ultimate/UltimateSequenceAsset.cs`, `GameActor/Animation/MotionEvents/MotionEvent_Afterimage.cs`, `Camera/CameraManager.cs`

### 2026-07-31 구현 상태

- 구현 완료: 서비스 계약/티켓, `CinematicStageSO`, 매니저 등록, 비활성 루트 기반 클론 생성과 새니타이즈, 경로 기반 포즈 미러, 원본 Animator 컬링·SkinnedMeshRenderer 오프스크린 갱신 소유권 복구, 원본 렌더러 가시성 API, 전용 카메라와 강체 변환 추종, 라이트/Volume/레이어 격리, 전환 오버레이, 워치독, 클론 풀, T2 실루엣/T3 타깃 클론, 일반 MotionEvent와 Ultimate 타임라인 Enter/Exit 이벤트, Ultimate Editor 저작 항목.
- UI Toolkit 기반 `Cinematic Stage Builder`를 추가했다. 메뉴 `UPlayGround/캐릭터/궁극기/Cinematic Stage Builder`에서 Stage 에셋·기본 프리팹·Additive 씬 생성, 필수 레이어/마스크 보정, 궁극기 안전 연결, 프리팹 미리보기와 실시간 검증을 수행한다. 프리팹 생성은 Preview Scene에서 처리해 현재 작업 씬을 오염시키지 않는다.
- Additive 무대는 **사전 로드된 씬만** 사용한다. 발동 시 로드는 하지 않으며, 씬에 `CinematicStageRoot`가 없으면 프리팹 또는 절차형 빈 무대로 폴백한다.
- MagicaCloth2 실측: `Player.prefab` 기준 MeshCloth 62, BoneCloth 18, BoneSpring 3. 포즈 미러만으로 전달되지 않는 MeshCloth(`clothType: 0`)와 그 충돌 프록시는 클론에서만 독립 구동하는 화이트리스트 예외로 유지한다. BoneCloth/BoneSpring은 본 포즈 복사 결과를 사용하므로 제거한다.
- 자동 검증: Unity 배치 스크립트 컴파일 오류 0. 신규 EditMode 테스트 3개를 추가했으나 현재 머신의 headless Test Runner entitlement 오류로 CLI 결과 XML이 생성되지 않아 실행 확인은 Unity Test Runner에서 남아 있다.
- 콘텐츠 작업 대기: 실제 `CinematicStageSO`/무대 프리팹·Additive 씬·실루엣 프리팹 제작, 궁극기 에셋 1종 연결, MeshCloth 외형과 중단/사망/씬 전환 PlayMode 수직 슬라이스, Player Build 검증.

---

## 0. 개정 이력 — v1에서 무엇이 바뀌었나

**v1의 핵심 결정은 틀렸다.** v1은 "실제 액터를 강체 변환으로 지하 무대에 순간이동시킨다"였다. v2는 **"실제 액터는 제자리에 두고, 무대에는 시각 복제본(클론)만 세운다"** 로 전환한다.

| 항목 | v1 | v2 | 근거 |
|------|----|----|------|
| 무대에 서는 것 | 실제 `PlayerActor` / `MonsterActor` | **포즈 미러 클론** (렌더러+본만) | 아래 |
| 앵커 좌표 | 시전자 기준 `Y-1500` + `OverlapBox` 검증 필수 | 레이어 격리로 **겹쳐도 무방**. 소규모 오프셋은 선택 | 클론은 콜라이더가 없어 물리 간섭이 없음 |
| 세이브 좌표 오염 | 치명적 함정(7.2) | **문제 자체가 소멸** | 실제 액터가 안 움직임 |
| 드롭·사망 좌표 | 지연 플러시 큐 필요(7.3) | **문제 자체가 소멸** | 판정이 원래 월드에서 발생 |
| KCC 접지 | 순간이동 후 재확립 필요(7.4) | **문제 자체가 소멸** | Motor를 건드리지 않음 |
| 액터 레이어 | 변경 금지 → T1이 사실상 불가 | 실제 액터는 여전히 변경 금지. **클론은 전용 레이어** | 클론은 물리·판정에 안 쓰임 |

v1이 옳게 잡은 것은 **강체 변환 `S`** 라는 개념 자체다. v2에서도 `S`는 그대로 살아 있고, 다만 **적용 대상이 실제 액터에서 시각 프록시로 바뀐다.**

v1에서 제기됐다가 v2에서 **폐기되는 함정**: 세이브 게이트, 드롭 지연 플러시, KCC 접지 재확립, 부동소수점 앵커 정밀도, 앵커 충돌 검증. 이 항목들을 다시 설계에 되살리지 않는다.

---

## 1. 목적

스킬(주로 궁극기·처형기) 연출 중, **주변 월드를 연출에서 배제한 전용 무대**를 만들어 재생한다.

### 해결하려는 실제 문제

| 문제 | 현재 상황 |
|------|-----------|
| 연출 카메라가 지형·구조물을 뚫는다 | `CameraSnapshotProfile`이 벽 안으로 들어가거나 지형에 가려짐 |
| 배경이 연출 톤과 충돌한다 | 대낮 초원에서 어두운 처형 연출을 해도 배경이 그대로 밝음 |
| 주변 잡몹·오브젝트가 프레임에 난입한다 | `pauseEnemyAI`로 멈추기만 할 뿐 화면에는 그대로 보임 |
| 연출 전용 대형 VFX가 지형과 간섭한다 | 지면 관통, 충돌, 라이팅 불일치 |
| 좁은 실내에서 광역 연출이 성립하지 않는다 | 원신이 물 위·근접 시 컷신을 스킵하는 것과 같은 회피가 필요 |

### 비목표

- 스토리 컷신 전용 편집 도구(별도 씬 저작 워크플로)
- 멀티플레이 동기화 (본 프로젝트는 싱글플레이)
- 연출 중 플레이어 조작 허용(QTE 등)
- **Timeline / PlayableDirector 도입** (5.5절에서 별도로 기각한다)

---

## 2. 핵심 원칙: 시뮬레이션과 표현의 분리

```text
실제 월드 (시뮬레이션)          전용 스테이지 (표현)
├─ PlayerActor        ─────┐    ├─ PlayerVisualClone   (포즈 미러)
│   MotionSet 재생         │    ├─ TargetVisualClone   (포즈 미러)
│   히트 윈도우/데미지      │    ├─ StageEnvironment
│   루트모션               ├───▶│  ├─ 전용 조명 리그
├─ TargetMonster           │    │  └─ 배경
│   HP/브레이크/사망       │    ├─ 전용 VFX
├─ 주변 적 (정지)     ─────┘    └─ 전용 카메라
└─ 렌더러만 숨김                    (S = 강체 변환)
```

> **원칙 1 — 실제 액터는 움직이지 않는다.**
> 시전자와 타깃은 지금까지와 완전히 동일하게 제자리에서 궁극기를 수행한다.
> MotionSet 재생, 히트 윈도우, 루트모션, 데미지, 사망, 드롭이 전부 원래 월드에서 일어난다.
>
> **원칙 2 — 무대의 클론은 읽기 전용 표현 객체다.**
> 클론은 데미지를 주지도 받지도 않고, 상태를 원본에 역으로 쓰지 않는다.
>
> **원칙 3 — 클론의 배치는 강체 변환 `S`가 결정한다.**
> `S`는 실제 액터의 포즈를 무대 공간으로 옮기는 변환이다. 참가자 간 상대 배치가 보존되므로,
> 카메라 저작(`CameraSnapshotSpace.ActorRelative`)과 연출 타이밍이 제자리 연출과 그대로 일치한다.

### 이 분리가 주는 것

- **게임플레이가 문자 그대로 불변**이다. 스테이지를 꺼도 전투 결과가 동일하다 — 원신이 물 위에서 컷신만 스킵하고 효과는 그대로 적용하는 것과 같은 성질을 공짜로 얻는다.
- **복구할 상태가 거의 없다.** 되돌릴 것은 "숨긴 렌더러 다시 켜기", "카메라 복귀", "클론 풀 반납"뿐이다. 위치 복원이 없다.
- **연출 실패 = 연출만 실패.** 무대를 못 만들면 그냥 T0으로 떨어지면 되고, 전투는 아무 영향이 없다.

### 포즈 미러(Pose Mirror)란

클론은 자기 애니메이션을 재생하지 않는다. **매 `LateUpdate`마다 원본 본의 로컬 포즈를 그대로 복사**한다.

```csharp
// 클론 생성 시 1회: 동일 계층이므로 인덱스가 일대일 대응한다
_sourceBones = sourceModelRoot.GetComponentsInChildren<Transform>(true);
_cloneBones  = cloneModelRoot.GetComponentsInChildren<Transform>(true);

// 매 LateUpdate
for (int i = 1; i < _sourceBones.Length; i++)   // 0 = 루트, S로 별도 배치
{
    _cloneBones[i].localPosition = _sourceBones[i].localPosition;
    _cloneBones[i].localRotation = _sourceBones[i].localRotation;
}
cloneModelRoot.SetPositionAndRotation(
    S.MultiplyPoint3x4(sourceModelRoot.position),
    S.rotation * sourceModelRoot.rotation);
```

**포즈 미러가 해결하는 것** — 이게 v2에서 가장 중요한 설계 지렛대다.

| 대안(독립 애니메이션 클론)의 문제 | 포즈 미러에서는 |
|-----------------------------------|-----------------|
| 클론에 `ActorAnimator`/Animancer/MotionSet 재생 런타임을 복제해야 함 | 클론에 **Animator가 아예 없다** |
| 원본과 클론의 재생 시간이 어긋날 수 있음(드리프트) | **구조적으로 동기화 불가능한 상태가 없다** |
| MotionEvent를 클론에서 다시 발화시켜야 함 | 원본의 `MotionEventExecutor`가 그대로 동작 |
| Timeline 트랙을 런타임 클론에 재바인딩해야 함 | **바인딩 문제 자체가 발생하지 않음** (5.5절) |
| 히트 타이밍을 연출에서 전투로 역전달해야 함 | 원본이 자기 히트 윈도우를 그대로 수행 |
| MagicaCloth2를 클론에서 재초기화해야 함 | 원본에서 시뮬레이션된 본 결과가 그대로 복사됨(단, 7.6 제약) |

즉 **클론은 "렌더러 + 본 계층"뿐인 껍데기**이고, 로직은 하나도 복제되지 않는다.

---

## 3. Tier 구조

연출 강도를 데이터로 선택한다. 메커니즘(클론)은 T2 이상에서 동일하다.

| Tier | 이름 | 내용 | 비용 | 용도 |
|------|------|------|------|------|
| **T0** | 없음 | 기존 카메라 스냅샷 + VFX만 | 0 | 일반 스킬 |
| **T1** | 카메라 연출 | 제자리. 카메라 워크 + 전용 Volume만 | 매우 낮음 | 강스킬, 짧은 컷 |
| **T2** | 시전자 클론 스테이지 | 시전자 클론 + 무대 + 전용 조명·카메라. **타깃은 실루엣/VFX로 대체** | 중간 | **궁극기 기본값** |
| **T3** | 양측 클론 스테이지 | 시전자 + 타깃 모두 클론. 접촉 연출 가능 | 높음 | 처형기, 보스 피니시 |

T2와 T3의 경계는 **"적의 몸을 정확히 잡거나 베는 접촉 연출이 있는가"** 다. 접촉이 있으면 타깃 체형 대응(7.8)이 필요해지고 비용이 급증한다.

### 스테이지 자산을 어디에 둘 것인가 — Tier와 독립된 축

이건 **런타임 메커니즘이 아니라 저작·로딩 문제**다. Tier와 직교한다.

| 방식 | 평가 |
|------|------|
| 같은 씬에 프리팹으로 스폰 | 구현 최단. 무대 수가 적을 때 충분 |
| **Additive 전용 씬 (`UltimateStage.unity`)** ✅ | 연출 담당자가 무대를 독립적으로 편집 가능. 조명·Volume·배경을 씬 단위로 관리. **부팅 시 1회 사전 로드**하고 상주 |
| 궁극기 발동 시점에 로드 | **금지.** 발동에 로딩 유라가 들어간다 |

권장: **Additive 사전 로드**. 단, 새로 생성한 클론이 어느 씬에 소속되는지 주의해야 한다 — 런타임 생성 오브젝트는 활성 씬으로 들어가므로, 클론 생성 시 부모를 스테이지 씬의 `ActorRoot`로 고정하거나 `SceneManager.MoveGameObjectToScene`로 명시 이관한다.

---

## 4. 무대 좌표 — v1에서 대폭 축소

v1은 "지하 1500m + 콜라이더 검증 + 폴백"이라는 복잡한 앵커 해석기를 요구했다. **클론 방식에서는 거의 전부 불필요하다.**

클론에는 콜라이더도, Rigidbody도, KCC도 없다. 무대가 실제 지형과 물리적으로 겹쳐도 아무 일도 일어나지 않는다. 렌더 격리는 좌표가 아니라 **레이어**가 담당한다.

```text
Rendering / Culling
  Gameplay Camera cullingMask : Default, Ground, Player, Enemy, ...  (Ultimate* 제외)
  Ultimate Camera cullingMask : UltimateStage, UltimateActor, UltimateVFX  (그 외 전부 제외)
```

그래도 **소규모 오프셋(예: 상공 300~500m)은 권장**한다. 이유는 물리가 아니라 위생이다.

- 무대 VFX의 파티클 충돌 모듈이 월드 콜라이더를 잡는 사고 방지
- 리플렉션 프로브 박스가 월드 지오메트리와 겹치지 않게
- Scene 뷰 디버깅 시 무대와 월드가 시각적으로 분리됨

**v1처럼 극단적인 오프셋(1500m)과 콜라이더 검증·폴백 체인은 넣지 않는다.** 부동소수점 정밀도 우려도 이 규모에서는 무의미하다.

---

## 5. 아키텍처

### 5.1 모듈 배치

```
UPlayGround.Contracts
  ICinematicStageService              // Svc.CinematicStage
  CinematicStageTicket (struct)       // 소유권 핸들

UPlayGround.Data
  CinematicStageSO                    // 무대 정의 (씬/프리팹/조명/전환/Tier)
  CinematicStageSettings              // 스킬 에셋에 박히는 인라인 설정
  UltimateStageEnterEvent / ExitEvent // 타임라인 이벤트 (SerializeReference)

UPlayGround.Manager
  CinematicStageManager               // BaseManager<T>, IManager, ICinematicStageService
    ├ CinematicStageInstance          // 활성 무대 1개의 수명/참가자/변환 S
    ├ CinematicCloneFactory           // Model 서브루트 복제 + 새니타이즈 + 풀링
    ├ CinematicPoseMirror             // 본 포즈 복사 (LateUpdate)
    ├ CinematicStageLightingContext   // 카메라 Volume/라이트 마스크 전환
    └ CinematicStageTransition        // 진입/복귀 화면 전환(플래시·와이프)

UPlayGround.Actor
  ActorPresentation                   // 렌더러 가시성 전용 API (신규)
  MotionEvent_CinematicStage          // 일반 스킬에서 스테이지 사용
  UltimateSequencePlayer (확장)       // 궁극기 경로에서 소비
```

`CinematicStageManager`를 매니저로 두는 이유: 연출은 **전역 단일 자원**이다(동시 2개 금지). 카메라·조명·입력·UI를 점유하므로 소유권을 티켓으로 중재해야 한다. `GameManager` 등록 순서는 `CameraManager` 이후.

Camera 모듈 이식성 규약에 따라, 스테이지는 기존 `CameraManager` 공개 API만 사용하고 Camera 모듈이 스테이지를 역참조하지 않는다.

### 5.2 서비스 계약 (초안)

```csharp
public interface ICinematicStageService
{
    bool IsActive { get; }
    CinematicStageTicket ActiveTicket { get; }

    /// 무대 진입 시도. 실패 시 ticket.IsValid == false 이며 호출자는 그대로 제자리 연출로 진행한다.
    bool TryEnter(in CinematicStageRequest request, out CinematicStageTicket ticket);

    /// 실제 액터 포즈를 무대 공간으로 옮기는 강체 변환. 미진입이면 identity.
    Matrix4x4 StageTransform { get; }

    /// 연출 도중 생성된 무대 전용 오브젝트를 무대 수명에 귀속시킨다.
    void RegisterTransient(in CinematicStageTicket ticket, GameObject instance);

    void Exit(in CinematicStageTicket ticket, CinematicStageExitReason reason);
}
```

**티켓 소유권**: `UltimateGameplayLockContext`가 이미 "내가 바꾼 것만 되돌린다" 패턴을 쓴다. 스테이지도 동일하게, 티켓을 가진 쪽만 `Exit`할 수 있고 무효 티켓의 `Exit`는 무시한다.

### 5.3 클론 생성 — 무엇을 복제하는가

이 프로젝트에는 이미 **Model 서브루트**가 있다. `CharacterModelData`의 주석이 이를 명시한다("Model 서브루트에 붙는 캐릭터 식별·전투 데이터 컨테이너"). 장비·코스튬·헤어가 전부 이 하위에 조립되어 있으므로, **Model 서브루트를 통째로 복제하면 현재 외형이 그대로 따라온다.**

```text
PlayerActor                      ← 복제 안 함
├─ PlayerMovementController      ← 복제 안 함
├─ PlayerCombat                  ← 복제 안 함
└─ Model (CharacterModelData)    ← 이것만 Instantiate
   ├─ AnimancerComponent         ← 새니타이저가 제거
   ├─ PlayerActorAnimator        ← 새니타이저가 제거
   ├─ PlayerEquipment            ← 새니타이저가 제거
   ├─ Armature (본 계층)         ← 유지 (포즈 미러 대상)
   ├─ SkinnedMeshRenderer ×N     ← 유지
   └─ 무기 Visual                ← 유지
```

**화이트리스트 방식을 쓴다.** "복제 후 위험한 컴포넌트를 지우는" 블랙리스트는 새 컴포넌트가 추가될 때마다 조용히 깨진다. 클론에 남길 타입을 명시적으로 나열하고, 목록에 없는 `MonoBehaviour`는 전부 제거한다.

```csharp
// 클론에 허용되는 것: Transform, SkinnedMeshRenderer, MeshRenderer, MeshFilter만
// 그 외 모든 Component는 제거. Animator/Animancer도 제거한다(포즈 미러가 대체).
```

> **외형 스냅샷 팩토리는 만들지 않는다.** 장비 ID·코스튬 ID로 클론을 재조립하는 방식은
> 스킨 수집형 게임에는 맞지만, 이 프로젝트는 Model 서브루트에 이미 조립이 끝나 있으므로
> 런타임 복제가 더 단순하고 항상 최신 외형을 보장한다. 초기 구현 비용도 훨씬 낮다.

**재사용 자산**: `MotionEvent_Afterimage`가 이미 렌더러 수집·머티리얼 인스턴스화·인스턴스 풀링을 구현하고 있다(`_sourceRenderers` 수집, `EnsurePool`/`GetPooledInstance`). 클론 팩토리는 이 코드의 구조를 따르거나 공통 유틸로 승격한다.

### 5.4 데이터: `CinematicStageSO`

```csharp
[CreateAssetMenu(menuName = "UPlayGround/전투/Cinematic Stage")]   // CLAUDE.md flat 2단계 규약 준수
public class CinematicStageSO : ScriptableObject
{
    [Header("등급")]
    public CinematicStageTier tier = CinematicStageTier.CasterClone;  // None/CameraOnly/CasterClone/BothClones
    public CinematicStageFallback fallback = CinematicStageFallback.DemoteToCameraOnly;

    [Header("무대")]
    public string stageSceneName;               // Additive 상주 씬의 StageRoot 식별자
    public GameObject stagePrefab;              // 씬을 쓰지 않을 때의 대체 경로
    public Vector3 anchorOffset = new(0f, 400f, 0f);
    public bool alignStageYawToTarget = true;

    [Header("타깃 표현")]
    public CinematicTargetRepresentation targetMode = CinematicTargetRepresentation.Silhouette;
    // Clone / Silhouette / DummyRig / VfxOnly / None
    public GameObject silhouettePrefab;
    public UltimateTargetSizeAnchors sizeAnchors;   // Small/Medium/Large/Giant 앵커

    [Header("렌더/조명")]
    public LayerMask stageCullingMask;           // UltimateStage/UltimateActor/UltimateVFX
    public VolumeProfile stageVolumeProfile;
    public bool hideSourceRenderers = true;

    [Header("전환")]
    public CinematicStageTransitionType enterTransition = CinematicStageTransitionType.WhiteFlash;
    [Min(0f)] public float enterTransitionDuration = 0.12f;
    public CinematicStageTransitionType exitTransition = CinematicStageTransitionType.Dissolve;
    [Min(0f)] public float exitTransitionDuration = 0.2f;
}
```

### 5.5 Timeline / PlayableDirector를 도입하지 않는다

업계 일반론에서는 연출 스테이지에 Timeline + `SetGenericBinding` 런타임 바인딩을 쓰는 것이 표준 패턴이다. **이 프로젝트에서는 채택하지 않는다.**

| 기각 사유 | 내용 |
|-----------|------|
| 저작 축이 이미 존재한다 | 궁극기 연출은 `MotionSetAsset` + `[SerializeReference] UltimateTimelineEvent` 목록으로 저작한다. UIToolkit 기반 `UltimateSequenceEditorWindow`가 2026-07-23에 완성되어 드래그·다중선택·레인 패킹·검증을 지원한다. Timeline 도입은 **이중 저작 시스템**을 만든다 |
| 애니메이션 소스가 Animancer다 | MotionSet 체이닝·AvatarMask 레이어 분리는 Animancer 런타임 위에 있다. Timeline Animation Track과 개념이 겹치면서 호환되지 않는다 |
| **포즈 미러가 바인딩 문제를 소거한다** | Timeline 런타임 바인딩이 필요한 이유는 "클론에 애니메이션을 재생시켜야 해서"다. 포즈 미러는 클론에 Animator를 두지 않으므로 **바인딩할 대상 자체가 없다** |
| 히트 역전달이 불필요하다 | Timeline Marker → `ApplyRealHit` 구조는 연출이 전투를 구동할 때 필요하다. 여기서는 원본이 자기 히트 윈도우를 그대로 수행한다 |

정리하면, 일반론이 Timeline을 요구하는 세 이유(클론 애니메이션 / 트랙 바인딩 / 히트 역전달)가 포즈 미러 채택으로 **전부 사라진다.**

---

## 6. 실행 시퀀스

### 6.1 진입

```
 1. 요청 검증        : 활성 스테이지가 있으면 거부 → 호출자는 T0으로 진행
 2. 참가자 확정      : 시전자 + 해석된 타깃(기존 UltimateTargetResolver 재사용)
 3. 타깃 표현 결정   : 체형 분류 → Clone/Silhouette/VfxOnly (7.8)
 4. 클론 획득        : 풀에서 대여, 없으면 Model 서브루트 복제 + 새니타이즈
 5. S 산출           : StageRoot 앵커 기준 강체 변환
 6. 포즈 미러 시작   : 원본↔클론 본 배열 매칭, LateUpdate 등록
 7. Animator 컬링 해제: 원본 Animator.cullingMode = AlwaysAnimate ← 필수 (7.2)
 8. 화면 전환 시작   : 플래시·와이프 ← 필수
 9. 원본 렌더러 숨김 : ActorPresentation.SetVisible(false) — SetActive 금지 (7.3)
10. 카메라 전환      : Ultimate Camera 활성 + cullingMask/Volume 전환. 블렌드 금지, 컷
11. 티켓 발급        → 호출자가 원본에서 MotionSet·타임라인 시작
```

**순서 주의**: 클론 배치(4~6)가 화면 전환(8)보다 **먼저**여야 한다. 화면을 먼저 바꾸면 한 프레임 동안 빈 무대가 보인다.

### 6.2 복귀

```
1. 화면 전환 시작 (exitTransition)
2. 카메라 복귀 (Gameplay Camera)
3. 포즈 미러 해제 + Animator.cullingMode 원복
4. 원본 렌더러 표시 (ActorPresentation.SetVisible(true))
5. 무대 transient 오브젝트 정리
6. 클론 풀 반납
7. 티켓 무효화
```

**되돌릴 위치가 없다는 점이 v1 대비 가장 큰 차이다.**

### 6.3 이상 종료 — 반드시 같은 경로

`UltimateSequencePlayer.Restore()`가 인터럽트·사망·씬전환·비활성화·실패를 한 함수로 모으는 패턴을 그대로 따른다.

- `CinematicStageManager.Dispose()` / `OnSceneChanged()`에서 활성 스테이지 강제 종료
- **워치독**: 활성 시간이 `maxStageSeconds`(예: 30초) 초과 시 경고 로그 + 강제 종료. 소유자가 파괴된 채 무대만 남아 **화면이 무대에 고정되는 사고**를 막는 최후 방어선
- 중단 트리거 목록: 타깃이 시작 직전 사망 / 타깃 오브젝트 파괴 / 씬 전환 / 플레이어 사망 / 스킵 입력 / 일시정지 / 클론 생성 실패 / 궁극기 연속 발동

---

## 7. 함정 목록

### 7.1 실제 액터의 레이어를 바꾸지 마라 — 클론은 반대

`Enemy`/`HitBox`/`Player` 레이어는 콜리전 매트릭스, 히트 판정, 락온 탐색이 공유한다. 연출 중 변경하면 판정이 조용히 깨진다.

**반대로 클론은 반드시 전용 레이어(`UltimateActor`)로 생성한다.** 클론에는 콜라이더가 없으므로 물리 영향이 없고, 레이어 분리가 렌더 격리의 유일한 수단이다. 새 레이어 3개(`UltimateStage`, `UltimateActor`, `UltimateVFX`)를 추가한다 — 현재 TagManager에 14개만 쓰이고 있어 여유가 충분하다.

### 7.2 Animator 컬링 — 포즈 미러의 최대 함정

원본 렌더러를 숨기면 Unity가 **애니메이션 평가를 중단할 수 있다.** `Animator.cullingMode`가 `CullUpdateTransforms` 또는 `CullCompletely`면, 렌더러가 보이지 않는 순간 본 갱신이 멈춘다. 그러면 포즈 미러의 소스가 얼어붙고 **클론이 T-포즈나 첫 프레임에서 정지한다.**

```csharp
// 진입 시
_previousCullingMode = animator.cullingMode;
animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
// 복귀 시 _previousCullingMode로 원복 (소유권 패턴)
```

`SkinnedMeshRenderer.updateWhenOffscreen`도 함께 검토한다. **이 항목은 PlayMode 검증 1순위다.**

### 7.3 원본 렌더러를 `SetActive(false)`로 숨기지 마라

`GameObject.SetActive(false)`는 Animator, 코루틴, 이벤트 수신, MotionEvent 발화까지 전부 중단시킨다. 원본이 궁극기를 수행해야 하므로 치명적이다.

렌더러 가시성만 다루는 전용 API를 둔다.

```text
숨김: SkinnedMeshRenderer / MeshRenderer / TrailRenderer / 게임플레이 VFX
유지: Animator, Combat, Stats, Buff, Ability, Targeting, MotionEventExecutor
```

원본을 숨겨야 하는 이유는 카메라가 어차피 안 보여주기 때문이 아니라 — 미니맵 카메라, 반사, 디버그 카메라, 복귀 프레임의 중복 노출 때문이다.

### 7.4 기존 VFX·연출 컴포넌트의 대상 전환

`DissolveController`, `ActorColorChanger`(피격 플래시), `MotionEvent_Afterimage`는 **원본 렌더러**를 대상으로 동작한다. 원본이 숨겨진 동안 이 효과들은 화면에 안 보인다.

궁극기 연출에서 디졸브·잔상이 필요하면 **클론의 렌더러를 대상으로 다시 적용**해야 한다. 클론 획득 시 이들 컴포넌트가 참조할 렌더러 목록을 클론 쪽으로 바꿔주는 어댑터가 필요하다.

### 7.5 클론은 읽기 전용이다

클론 상태를 원본에 역으로 쓰지 않는다. 클론이 데미지를 주거나 받지 않는다. 무대의 타깃 클론은 **피격 애니메이션·디졸브·흔들림만** 재생하고, HP 감소·브레이크·상태이상·사망은 전부 원래 월드의 실제 타깃에서 일어난다.

이 경계가 무너지면 데미지 중복, 타깃 불일치, 사망 처리 누락이 빠르게 늘어난다.

### 7.6 MagicaCloth2 / 스프링본 — 검증 필요

포즈 미러는 **본 기반** 변형만 복사한다. MagicaCloth2의 BoneCloth는 원본에서 시뮬레이션된 본 회전이 그대로 복사되므로 문제없다. 그러나 **MeshCloth(정점 변형)는 본 복사로 전달되지 않는다.**

- 확인 필요: 각 캐릭터가 BoneCloth를 쓰는가, MeshCloth를 쓰는가
- MeshCloth를 쓴다면 → 클론에서 MagicaCloth를 독립 구동(초기화 비용·풀링 복잡도 증가)하거나, 해당 부위를 본 구동으로 전환
- 원본 렌더러를 숨겼을 때 MagicaCloth 시뮬레이션이 컬링되는지도 7.2와 함께 확인

**이 항목은 설계 단계에서 결론을 못 낸다. P1에서 실측한다.**

### 7.7 조명 격리는 Additive 씬만으로 안 된다

Additive 씬으로 나눠도 라이트와 Volume은 자동 격리되지 않는다. 명시적으로 나눠야 한다.

- 무대 라이트: `cullingMask`를 `UltimateActor`/`UltimateStage`로 한정
- 월드 Directional Light: `cullingMask`에서 Ultimate 레이어 제외
- Volume: Ultimate Camera의 Volume Layer Mask를 전용 레이어로 지정
- 그림자: 라이트 컬링 마스크로 캐스터가 걸러지는지 확인 필요

`WorldLightingController`가 이미 `_characterFillLight.cullingMask`를 쓰고 있어 라이트 마스크 운용 선례가 있다. 다만 같은 라이트를 스테이지가 건드리면 경합하므로, 스테이지 활성 중 `WorldLightingController` 갱신 일시 정지가 필요하다.

> v1과 달리 **`RenderSettings.ambient`/`skybox`/`fog` 전역 값은 건드리지 않는다.** 카메라 단위 Volume과 라이트 마스크로 해결한다. 전역 상태 복구 누락 사고가 사라진다.

### 7.8 타깃 체형 — T3의 진짜 비용

적은 크기·형태·리그가 제각각이다. 무대에 적 클론을 세우는 순간 이 문제가 전부 들어온다.

```csharp
public enum UltimateTargetSize { Small, Medium, Large, Giant }
```

대응 순서:

1. 타깃 Bounding Box 높이로 크기 분류
2. 크기별 Spawn 앵커·카메라 프리셋 선택
3. 극단적 체형·비인간형은 **클론을 포기하고 실루엣/VFX 대체**

**클론을 강제 스케일링하지 않는다.** 무기 접촉점, 바닥 접지, VFX 크기가 전부 어긋난다. 스케일 대신 앵커와 카메라를 보정한다.

T2(시전자만 클론, 타깃은 실루엣)를 기본값으로 두는 이유가 이것이다. 검광 발사 → 화면 전환 → 실루엣 피격 → 폭발 구조면 체형 문제가 거의 사라진다.

### 7.9 원래 월드의 시간 정책

궁극기 중 월드를 어떻게 다룰지 정해야 한다. **`Time.timeScale = 0`은 쓰지 않는다** — 연출 자체와 파티클까지 멈춘다.

```text
GameplaySimulation : 정지 또는 저속 (주변 AI, 투사체, 잡몹 Animator)
시전자/타깃        : 정상 진행 ← 포즈 미러의 소스이므로 절대 정지 금지
UltimatePresentation: 정상 진행
UI / Audio         : 정상 진행
```

`UltimateGameplayLockContext`가 이미 `pauseEnemyAI` + `enemyFreezeRadius`로 주변 AI 정지를 구현하고 있으므로 그대로 재사용한다. **시전자와 타깃의 Animator는 반드시 계속 돌아야 한다** — 일반적인 "궁극기 중 실제 캐릭터 Animator 일시정지" 최적화는 포즈 미러와 양립하지 않는다.

### 7.10 프리워밍과 풀링

클론 첫 생성 = SkinnedMeshRenderer 생성 + 머티리얼 인스턴스화 + 계층 구축 + GC 할당. 궁극기는 게이지가 차면 언제든 나가므로 **스킬 게이지 만충 시점에 미리 만들어 비활성 상태로 보관**한다.

```text
시전자 클론  : 캐릭터별 1개 상시 풀 (파티 활성 캐릭터 우선)
공통 실루엣  : 상시 풀
타깃 클론    : 현재 타깃 종류만 온디맨드 + 캐시
```

`MotionEvent_Afterimage`의 풀 구조를 참고한다.

### 7.11 Tier 강등의 관측 가능성

강등·폴백은 정상 동작이다(원신이 물 위에서 컷신을 스킵하듯). 다만 **디버그 로그와 치트 오버레이에 강등 사유를 노출**해야 한다. 안 그러면 "가끔 연출이 안 나온다"는 재현 불가 버그로 남는다.

### 7.12 RenderTexture는 지금 쓰지 않는다

전용 카메라 출력을 RenderTexture로 받아 화면에 합성하는 방식은 강한 격리와 특수 전환 효과를 주지만, 메모리·해상도·AA·TAA/모션벡터 처리 부담이 따른다. **전면 궁극기 컷신에는 전용 Base Camera 직접 전환이 더 단순하다.**

RenderTexture가 정당해지는 경우는 화면 일부만 쓰는 컷인, 카드 연출, 무대 화면을 소재로 쓰는 왜곡 전환이다. 그때 별도 설계로 추가한다.

---

## 8. 구현 Phase

| Phase | 범위 | 완료 기준 |
|-------|------|-----------|
| **P1** | 계약 + 매니저 골격 + 클론 팩토리·새니타이저 + 포즈 미러 + Animator 컬링 대응 + 레이어 3종 추가 | 시전자 클론이 무대에서 원본과 완전히 동기화되어 움직인다. **MagicaCloth 거동 실측 결과가 문서화된다(7.6)** |
| **P2** | Additive 스테이지 씬 + 전용 카메라 전환 + 화면 전환 + 티켓/복구/워치독 | 궁극기 1종이 무대에서 재생되고, 인터럽트·사망·씬전환 모두에서 정상 복귀. **전투 결과가 스테이지 유무와 무관하게 동일함을 확인** |
| **P3** | 조명·Volume 격리 + `WorldLightingController` 조정 + 원본 렌더러 은닉 API | 무대에서 캐릭터가 의도한 톤으로 보이고, 복귀 후 월드 렌더링이 진입 전과 동일 |
| **P4** | 타깃 표현(실루엣/VFX) + 체형 분류 + Tier 자동 강등 + 치트 진단 | 임의의 적을 상대로 궁극기 발동 시 체형과 무관하게 연출이 성립 |
| **P5** | 클론·VFX 풀링 + 프리워밍 + 기존 VFX 컴포넌트 대상 전환(7.4) | 첫 발동 유라 없음. 디졸브·잔상이 클론에 정상 적용 |
| **P6** | T3 양측 클론 + 접촉 연출 + 타임라인 구간 스테이지 이벤트 | 보스 처형 연출 1종 완성. 에디터 타임라인에서 무대 구간 저작 가능 |

**P1이 기술 리스크의 대부분을 차지한다.** 포즈 미러가 이 프로젝트의 Animancer + MagicaCloth 조합에서 실제로 동작하는지가 설계 전체의 전제다. P1을 프로토타입으로 먼저 검증하고 나머지를 진행한다.

---

## 9. 검증 계획

### EditMode
- 클론 새니타이저: 화이트리스트 외 `MonoBehaviour`가 전부 제거되는가
- 원본/클론 본 배열 매칭: 길이와 계층 경로가 일대일 대응하는가
- 티켓 소유권: 무효 티켓의 `Exit`가 무시되는가
- `S` 왕복: 원본 포즈 → 무대 포즈 변환의 상대 배치 보존

### PlayMode (수직 슬라이스)
- **포즈 동기화**: 궁극기 전 구간에서 클론 본 포즈가 원본과 프레임 단위로 일치
- **컬링 회귀**: 원본 렌더러를 숨긴 상태에서 클론이 계속 애니메이션되는가 (7.2)
- **결과 동일성**: 스테이지 켬/끔 두 조건에서 타깃의 최종 HP·브레이크·사망 여부가 동일
- 중단 경로(피격·사망·씬 전환·스킵) 각각에서 렌더러 복원 + 카메라 복귀 + 클론 반납
- 워치독: 소유자를 강제 파괴해도 `maxStageSeconds` 내 회수

### 수동 체크리스트
- 좁은 실내 / 절벽 / 물 위 / 지하 던전
- 소형·중형·대형·비인간형 적 각각을 타깃으로 발동
- 코스튬·무기 변경 후 발동 → 클론 외형이 최신인가
- 연출 중 일시정지 → 재개, 연출 중 세이브
- 연속 발동(캐릭터 스왑 후 즉시 궁극기)

---

## 10. 열린 질문

1. **MagicaCloth2가 BoneCloth인가 MeshCloth인가** — 7.6. P1에서 실측해 결론을 이 문서에 반영한다. MeshCloth 비중이 크면 클론 비용이 크게 오른다.
2. **무대를 캐릭터별로 둘 것인가, 공용 3~4종 + 오버라이드로 갈 것인가** — 12캐릭터 × 전용 무대는 제작비가 크다. 제작비에 가장 크게 영향을 주는 결정.
3. **T2를 궁극기 전체에 적용할 것인가, 대표 캐릭터만 적용할 것인가** — 일부만 적용하면 연출 밀도 차이가 캐릭터 개성이 된다.
4. **연출 스킵** — 반복 플레이에서 매번 3초는 피로하다. 2절 원칙 덕분에 스킵해도 전투 결과가 동일하므로 구현은 어렵지 않다.
5. **사이클 런 텔레메트리** — `CycleTelemetrySession`이 무대 체류 시간을 전투 시간으로 셀지.

---

## 부록. 설계 근거

명조·원신 류의 궁극기 연출 **내부 구현**에 대한 공개 기술 자료는 사실상 없다. 아래는 공개 자료 + 엔진 공개 기능 + 본 프로젝트의 기존 인프라다.

| 근거 | 설계에 반영된 곳 |
|------|------------------|
| 원신: 물 위·근접 시 버스트 컷신 스킵, 애니메이션과 효과는 그대로 | 2절 — 연출 실패가 전투 결과에 영향을 주지 않아야 한다 |
| 원신: 씬은 디퍼드, 캐릭터는 포워드로 분리 렌더 | 7.7 — 무대 전용 조명으로 캐릭터 톤을 별도 통제 |
| Unreal Sequencer Spawnable: 시퀀스 수명에 귀속되는 컷신 전용 액터 | `RegisterTransient` / 6.2-5 |
| Unity Additive Scene, 활성 씬과 `MoveGameObjectToScene` | 3절 — 스테이지 자산 배치와 클론 소속 씬 |
| URP 카메라 `cullingMask` / Volume Layer Mask / Light `cullingMask` | 4절, 7.7 |
| Cinemachine 0초 블렌드(컷) | 6.1-10 |

### 프로젝트 내부 기반 (이미 존재)

| 기존 자산 | 역할 |
|-----------|------|
| `CharacterModelData` (Model 서브루트) | **클론 복제 단위.** 장비·코스튬이 이미 하위에 조립되어 있음 |
| `MotionEvent_Afterimage` | 렌더러 수집·머티리얼 인스턴스화·풀링의 선례 |
| `UltimateSequencePlayer` | "잠금 → 배치 → 타임라인 → 단일 Restore" 골격 |
| `UltimateGameplayLockContext` | "내가 바꾼 것만 되돌린다" 소유권 패턴 + AI 정지 |
| `UltimateTargetResolver` | 참가자 확정 |
| `CameraSnapshotSpace.ActorRelative` | 시전자 기준 프레이밍 → 무대에서도 저작 재사용 |
| `WorldLightingController._characterFillLight.cullingMask` | 라이트 컬링 마스크 운용 선례 |
| `UltimateSequenceEditorWindow` (UIToolkit) | 연출 저작 축. **Timeline 도입을 기각하는 근거**(5.5) |
