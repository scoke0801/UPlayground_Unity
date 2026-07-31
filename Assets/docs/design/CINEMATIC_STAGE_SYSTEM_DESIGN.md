# 연출 스테이지(Cinematic Stage) 시스템 설계 문서

> 작성일: 2026-07-31
> 대상: Unity 6 (6000.0.60f1), URP
> 분류: **설계안 (미구현)**. 구현 착수 시 Phase 단위로 본 문서를 갱신한다.
> 관련 문서: `Assets/docs/design/ULTIMATE_SEQUENCE_SYSTEM_DESIGN.md`, `Assets/docs/TODO/ULTIMATE_SEQUENCE_EDITOR_ADVANCEMENT_TODO.md`
> 관련 코드: `GameActor/Component/Player/UltimateSequencePlayer.cs`, `UltimateGameplayLockContext.cs`, `UltimatePlacementContext.cs`, `GameActor/Combat/Ultimate/UltimateSequenceAsset.cs`, `Camera/CameraManager.cs`

---

## 1. 목적

스킬(주로 궁극기·처형기) 연출 중, **주변 월드를 연출에서 배제한 전용 무대**를 만들어 재생한다.

요구 형태는 세 가지로 제시되었다.

1. 연출 전용 "가상 씬"을 스폰한다
2. 주변 지형을 숨긴다
3. 액터를 별도 위치로 옮겨서 연출한다

이 문서는 세 가지를 **서로 배타적인 대안이 아니라 하나의 스펙트럼(Tier)** 으로 통합하고, 공통 오케스트레이터 하나로 처리하는 설계를 제안한다.

### 해결하려는 실제 문제

| 문제 | 현재 상황 |
|------|-----------|
| 연출 카메라가 지형/구조물을 뚫는다 | `CameraSnapshotProfile`이 벽 안으로 들어가거나 지형에 가려짐 |
| 배경이 연출 톤과 충돌한다 | 대낮 초원에서 어두운 처형 연출을 해도 배경이 그대로 밝음 |
| 주변 잡몹/오브젝트가 프레임에 난입한다 | `pauseEnemyAI`로 멈추기만 할 뿐 화면에는 그대로 보임 |
| 연출 전용 대형 VFX가 지형과 간섭한다 | 지면 관통, 충돌, 라이팅 불일치 |
| 좁은 실내에서 광역 연출이 성립하지 않는다 | 원신이 물 위/근접 시 컷신을 스킵하는 것과 같은 회피가 필요 |

### 비목표 (이번 설계 범위 밖)

- 스토리 컷신 전용 편집 도구(별도 씬 저작 워크플로)
- 멀티플레이 동기화 (본 프로젝트는 싱글플레이)
- 연출 중 플레이어 조작 허용(QTE 등)

---

## 2. 핵심 개념: 스테이지는 "강체 변환"이다

이 설계의 중심 원칙이다. 여기서 벗어나면 버그 클래스가 통째로 생긴다.

> **연출 스테이지는 월드 공간에 대한 강체 변환(rigid transform) `S`(위치 오프셋 + Yaw 회전)이다.
> 참가 액터는 `S`를 통해 스테이지 공간으로 이동하며, 참가자들 간의 상대 배치는 완전히 보존된다.**

```
worldPose  --( S )-->  stagePose        // 진입
stagePose  --(S⁻¹)-->  worldPose        // 복귀
```

이 원칙이 주는 것:

- **게임플레이 결과 불변**: 히트 판정, 모션 워프, 루트모션, 거리 기반 판정이 제자리 연출과 **완전히 동일**하다. 연출은 "보이는 방식"만 바꾼다.
- **복귀가 산술이다**: 액터·드롭·시체·VFX의 최종 위치를 `S⁻¹`로 되돌리면 된다. 별도 위치 기억 테이블이 필요 없다.
- **연출 실패 시 폴백이 자명하다**: `S = identity`로 두면 그대로 제자리 연출이 된다. 스테이지를 못 잡으면 연출을 스킵해도 전투 결과가 달라지지 않는다 (원신의 조건부 컷신과 같은 안전장치).
- **카메라 저작이 재사용된다**: `CameraSnapshotSpace.ActorRelative` 스냅샷은 시전자 기준이므로 스테이지로 옮겨도 프레이밍이 그대로 성립한다.

`UltimatePlacementContext`가 이미 "시전자/타겟을 서로 기준으로 재배치"를 하고 있는데, 이건 액터 **개별** 배치라 상대 관계가 바뀐다. 스테이지 변환은 그 위 계층에서 **집합 전체를 통째로** 옮기는 별개 단계다. 두 단계 순서는 `배치(Placement) → 스테이지 이동(Stage)`이다.

---

## 3. Tier 구조

같은 오케스트레이터가 4단계 강도를 데이터로 선택한다.

| Tier | 이름 | 방식 | 비용 | 용도 |
|------|------|------|------|------|
| **T0** | 없음 | 기존 카메라 스냅샷 + VFX만 | 0 | 일반 스킬 |
| **T1** | 인플레이스 은폐 | 제자리 유지. 카메라 `cullingMask`로 월드 레이어 배제 + 스테이지 프롭 스폰 + 라이팅 오버라이드 | 낮음 | 강스킬, 짧은 컷 |
| **T2** | 스테이지 이동 | 강체 변환 `S`로 샌드박스 이동. 전용 프리팹 무대 + 전용 라이팅 | 중간 | **궁극기 기본값** |
| **T3** | 가상 씬 | Addressables 추가 로드(Additive)한 전용 씬으로 이동 | 높음(로드 히치) | 보스 처형, 스토리 필살기 |

### T1 (인플레이스 은폐) — 가능하지만 한계가 뚜렷

카메라 `cullingMask`에서 `Ground / Default / Water / InteractableObject / Trigger`를 끄고, 시전자 주변에 무대 프롭(원형 아레나, 안개 벽)을 스폰한다.

**결정적 한계**: 주변 잡몹은 `Enemy` 레이어라 이걸 끄면 **연출 대상 몬스터도 같이 사라진다.** 액터 레이어를 임시 변경하는 우회는 **금지한다** — `Enemy`/`HitBox`/`Player` 레이어는 콜리전 매트릭스, 히트 판정, 락온 탐색이 함께 쓰는 값이고, 연출 도중 변경하면 판정이 조용히 깨진다.

따라서 T1은 **"프레임 안에 연출 대상 외 액터가 없다고 보장되는 상황"** 에서만 쓴다. 아니면 T2로 승격한다. 이게 T2를 기본값으로 두는 이유다.

### T2 (스테이지 이동) — 권장 기본

전용 프리팹 무대를 풀에서 꺼내 샌드박스 앵커에 배치하고, 참가자를 `S`로 옮긴다. 카메라는 시전자를 따라오므로 자연히 무대 안에 있고, 월드 지오메트리는 프러스텀 밖이라 **자동으로 안 그려진다** — 컬링 마스크 조작이 필요 없다. 레이어를 건드리지 않으므로 판정도 안전하다.

### T3 (가상 씬)

`AssetManager.LoadSceneAsync`(현재 단일 씬 로드만 존재하므로 Additive 경로 신규 필요) 기반. 로드 히치가 있으므로 **연출 시작 전 예열(프리로드)** 이 전제다. 30초마다 나가는 궁극기에는 부적합하고, 보스 처치 컷 같은 저빈도 연출용이다.

> **Tier 승격 판단은 런타임 검증에 맡긴다.** 데이터에 T1을 지정해도 프레임 내 비참가 액터가 감지되면 T2로 승격하거나 T0으로 강등한다(정책은 에셋에서 선택). 조용히 깨진 연출보다 등급을 낮춘 연출이 낫다.

---

## 4. 샌드박스 앵커 (T2의 좌표를 어디에 둘 것인가)

세 후보를 검토했다.

| 후보 | 장점 | 문제 |
|------|------|------|
| 월드 원점에서 아주 먼 XZ (예: `x=100000`) | 월드와 절대 안 겹침 | **부동소수점 정밀도 붕괴.** 10k 넘어가면 셰이더/그림자/VFX 지터. 채택 불가 |
| 맵마다 디자이너가 배치한 고정 지점 | 라이팅/리플렉션 프로브를 미리 구울 수 있음 | 맵마다 수작업, 신규 맵 누락 시 무연출. 사이클 런처럼 절차적 배치와 궁합 나쁨 |
| **시전자 기준 상대 오프셋 (예: `caster + (0, -1500, 0)`)** ✅ | XZ 정밀도 유지, 맵 무관, 자동 | 지하에 월드 콜라이더/킬존이 있으면 충돌 |

**채택: 시전자 상대 오프셋(기본 `Y-1500`), 진입 전 검증 필수.**

검증 절차 (`CinematicStageAnchorResolver`):

1. 후보 앵커 주변 `OverlapBox`로 월드 콜라이더 존재 여부 확인
2. 걸리면 `Y` 오프셋을 단계적으로 더 내리거나, 대체 축(수평 오프셋 ±2000)으로 재시도 — 최대 N회
3. 전부 실패하면 **`S = identity`로 폴백**(제자리 연출) 또는 연출 스킵. 절대 임의 지점에 강행하지 않는다.

Yaw 성분: 기본 `identity`(월드 방위 유지). 무대 프리팹에 정면(관객 방향)이 있으면 시전자→타겟 벡터에 무대 정면을 맞추는 Yaw를 `S`에 포함한다.

---

## 5. 아키텍처

### 5.1 모듈 배치

```
UPlayGround.Contracts
  ICinematicStageService              // Svc.CinematicStage
  CinematicStageTicket (struct)       // 소유권 핸들

UPlayGround.Data
  CinematicStageSO                    // 무대 정의 (프리팹/라이팅/전환/Tier)
  CinematicStageSettings              // 스킬 에셋에 박히는 인라인 설정
  UltimateStageEnterEvent / ExitEvent // 타임라인 이벤트 (SerializeReference)

UPlayGround.Manager
  CinematicStageManager               // BaseManager<T>, IManager, ICinematicStageService
    ├ CinematicStageAnchorResolver    // 앵커 후보 탐색/검증
    ├ CinematicStageInstance          // 활성 무대 1개의 수명/참가자/변환 S
    ├ CinematicStageLightingContext   // RenderSettings/Volume 스냅샷·복구
    └ CinematicStageTransition        // 진입/복귀 화면 전환(플래시·와이프)

UPlayGround.Actor
  MotionEvent_CinematicStage          // 일반 스킬에서 스테이지 사용
  UltimateSequencePlayer (확장)       // 궁극기 경로에서 소비
```

`CinematicStageManager`를 매니저로 두는 이유: 연출은 **전역 단일 자원**이다(동시 2개 금지). 카메라·라이팅·시간·저장 같은 전역 상태를 건드리므로, 소유권을 한 곳에서 티켓으로 중재해야 중복 진입과 복구 누락이 안 생긴다. `GameManager` 등록 순서는 `CameraManager` 이후, `ActorSpawnManager` 이전이 적절하다.

Camera 모듈 이식성 규약에 따라, 스테이지가 카메라에 요구하는 동작(즉시 스냅, 스냅샷 시퀀스 점유)은 **기존 `CameraManager` 공개 API(`SnapToTarget`, `PushCameraSnapshotSequence`)만** 사용하고, Camera 모듈이 스테이지를 역참조하지 않는다.

### 5.2 서비스 계약 (초안)

```csharp
public interface ICinematicStageService
{
    bool IsActive { get; }
    CinematicStageTicket ActiveTicket { get; }

    /// 무대 진입 시도. 실패 시 ticket.IsValid == false 이며 호출자는 제자리 연출로 진행한다.
    bool TryEnter(in CinematicStageRequest request, out CinematicStageTicket ticket);

    /// 진입 시 결정된 강체 변환. 실패/미진입이면 identity.
    Matrix4x4 StageTransform { get; }

    /// 스테이지 공간 좌표를 월드로 환산 (드롭/사망 처리용)
    Vector3 ToWorld(Vector3 stagePosition);

    /// 연출 도중 생성된 오브젝트를 무대 수명에 귀속시킨다 (Spawnable 패턴)
    void RegisterTransient(in CinematicStageTicket ticket, GameObject instance);

    void Exit(in CinematicStageTicket ticket, CinematicStageExitReason reason);
}
```

`CinematicStageRequest`는 시전자, 참가자 목록, `CinematicStageSO`, Tier 오버라이드, 폴백 정책을 담는다.

**티켓 방식이 중요한 이유**: `UltimateGameplayLockContext`가 이미 "내가 바꾼 것만 되돌린다"는 소유권 패턴을 쓰고 있다. 스테이지도 동일하게, 티켓을 가진 쪽만 `Exit`할 수 있어야 한다. 티켓 없는 `Exit` 호출은 무시한다.

### 5.3 데이터: `CinematicStageSO`

```csharp
[CreateAssetMenu(menuName = "UPlayGround/전투/Cinematic Stage")]   // CLAUDE.md flat 2단계 규약 준수
public class CinematicStageSO : ScriptableObject
{
    [Header("등급")]
    public CinematicStageTier tier = CinematicStageTier.Relocate;   // None/InPlace/Relocate/VirtualScene
    public CinematicStageFallback fallback = CinematicStageFallback.DemoteToInPlace;

    [Header("무대")]
    public GameObject stagePrefab;              // T1/T2 무대 프롭 (풀링 대상)
    public AssetReference virtualScene;         // T3 전용
    public Vector3 anchorOffset = new(0f, -1500f, 0f);
    public Vector3 anchorProbeExtents = new(60f, 30f, 60f);
    public bool alignStageYawToTarget = true;

    [Header("라이팅")]
    public bool overrideAmbient = true;
    public Color ambientColor = Color.black;
    public Material skyboxOverride;             // null이면 스카이박스 제거(검은 배경)
    public bool overrideFog = true;
    public Color fogColor; public float fogDensity;
    public VolumeProfile postProcessOverride;   // 최우선순위 Volume으로 주입
    public bool disableWorldDirectionalLight = true;

    [Header("전환")]
    public CinematicStageTransitionType enterTransition = CinematicStageTransitionType.WhiteFlash;
    [Min(0f)] public float enterTransitionDuration = 0.12f;
    public CinematicStageTransitionType exitTransition = CinematicStageTransitionType.Dissolve;
    [Min(0f)] public float exitTransitionDuration = 0.2f;

    [Header("인플레이스(T1) 전용")]
    public LayerMask hiddenWorldLayers;         // Ground/Default/Water/InteractableObject
}
```

### 5.4 스킬 에셋 연결

**궁극기**: `UltimateSequenceAsset`에 `[Header("5단계: 연출 스테이지")] public CinematicStageSettings stageSettings;` 추가. `UltimateSequencePlayer.BeginSequenceRoutine`에서 `_placementContext.Apply` **직후**, `PlayMotionSetAsset` **직전**에 진입한다. 복구는 `Restore()`에서 `_placementContext.Restore()` **직전**에 `Exit`.

**일반 스킬**: `MotionEvent_CinematicStage`가 진입/복귀를 발화. MotionSet 타임라인 기준이라 저작 흐름이 기존 이벤트와 동일하다.

**구간 스테이지(Phase 2)**: `UltimateStageEnterEvent` / `UltimateStageExitEvent`를 `[SerializeReference]` 타임라인 이벤트로 추가하면 "0.4초에 무대 진입 → 2.8초에 복귀" 같은 부분 구간 연출이 가능하다. 앞뒤는 실제 월드에서 진행되어 연출 연결이 자연스러워진다. Phase 1에서는 에셋 단위 전 구간만 지원한다.

---

## 6. 실행 시퀀스

### 6.1 진입

```
1. 요청 검증        : 이미 활성 스테이지가 있으면 거부 → 호출자는 T0으로 진행
2. 앵커 해석        : AnchorResolver → 실패 시 fallback 정책 적용
3. 참가자 확정      : 시전자 + 해석된 타겟 집합 (그 외 월드 액터는 비참가)
4. 화면 전환 시작   : 플래시/와이프로 텔레포트 프레임을 가린다 ← 필수
5. 무대 인스턴스화  : 풀에서 stagePrefab 획득, 앵커에 배치, 활성화
6. 라이팅 스냅샷/적용: RenderSettings + Volume 저장 후 오버라이드 주입
7. 액터 이동        : 참가자 전원에 S 적용 (Motor.SetPositionAndRotation)
8. 카메라 스냅      : CameraManager.SnapToTarget() — 블렌드 금지, 반드시 컷
9. 무대 물리 안정화 : 1 FixedUpdate 대기하여 KCC 접지 재확립
10. 티켓 발급 → 호출자가 모션/타임라인 시작
```

**4번(화면 전환)과 8번(카메라 컷)은 타협 불가다.** 블렌드로 처리하면 카메라가 1500m를 활강하는 게 그대로 보인다.

### 6.2 복귀

```
1. 화면 전환 시작 (exitTransition)
2. 무대에서 생성된 transient 오브젝트 정리 (Spawnable 패턴)
3. 참가자 최종 포즈에 S⁻¹ 적용 → 월드 복귀
   - restorePositionsOnFinish면 진입 전 포즈로, 아니면 연출 중 이동분을 반영한 위치로
4. 라이팅 복구 (스냅샷 역적용, 자기가 바꾼 것만)
5. 무대 인스턴스 풀 반납
6. 카메라 스냅 → 기존 모드 복귀
7. 지연 처리 플러시: 사망/드롭/경험치 등 (아래 7.3)
8. 티켓 무효화
```

### 6.3 이상 종료 (반드시 같은 경로를 탄다)

`UltimateSequencePlayer.Restore()`가 이미 인터럽트/사망/씬전환/비활성화/실패를 한 함수로 모으는 좋은 패턴을 쓰고 있다. 스테이지도 동일하게:

- `CinematicStageManager.Dispose()` / `OnSceneChanged()`에서 활성 스테이지 강제 종료
- **워치독**: 활성 시간이 `maxStageSeconds`(예: 30초)를 넘으면 경고 로그 + 강제 종료. 소유자 컴포넌트가 파괴된 채 무대만 남아 플레이어가 지하에 갇히는 사고를 막는 최후 방어선
- 강제 종료 시 액터 위치는 **무조건 진입 전 월드 포즈로 복구**한다(연출 중 이동분 반영을 시도하지 않는다)

---

## 7. 함정 목록 (설계 시점에 못 박아둘 것)

### 7.1 레이어를 바꾸지 마라

`Enemy`/`HitBox`/`Player` 레이어는 콜리전 매트릭스, 히트 판정, 락온 탐색이 공유한다. "연출 중에만 잠깐" 바꾸면 그 프레임의 판정이 조용히 사라진다. 렌더 격리는 **레이어가 아니라 물리적 거리(T2)** 로 얻는다.

### 7.2 세이브 차단

`SaveManager`가 파티 위치를 저장한다. 스테이지 활성 중 저장이 발생하면 **지하 1500m 좌표가 세이브에 박힌다.** `ICinematicStageService.IsActive`를 세이브 게이트에 물려 저장을 거부하거나 지연시킨다. 자동 저장 경로(`CycleRunManager`, `WorldStateManager`)도 함께 확인해야 한다.

### 7.3 사망·드롭·경험치의 좌표

타겟이 무대에서 죽으면 시체·드롭·경험치 오브가 무대 좌표에 생성된다. 그대로 두면 복귀 후 월드에 아무것도 없고, 지하에 아이템이 떨어진다.

두 가지 처리 중 택일 (권장: **B**):

- **A. 즉시 변환**: 무대 중 생성되는 모든 월드 오브젝트에 `S⁻¹`를 적용해 월드에 바로 배치. → 무대에서 안 보이게 되어 연출상 어색
- **B. 지연 플러시** ✅: 무대 중 사망/드롭을 **큐에 적립**하고 렌더용 연출 오브젝트만 무대에 띄운다. 복귀 시점(6.2-7)에 월드 좌표로 실제 스폰. `VitalOrbActor`, 드롭 아이템, `MonsterRespawnManager` 등록이 모두 이 큐를 타야 한다

### 7.4 KCC 접지

`Motor.SetPositionAndRotation`으로 순간이동하면 KCC의 접지 상태가 한 프레임 무효다. 무대 바닥 콜라이더가 없거나 액터가 바닥 아래에 놓이면 **낙하 상태로 전환되어 연출이 깨진다.** 무대 프리팹은 충분히 넓은(반경 30m+) 바닥 콜라이더를 가져야 하고, 진입 후 1 FixedUpdate 대기 + 접지 검증을 넣는다.

### 7.5 라이팅은 전역 상태다

`RenderSettings.ambient*`, `skybox`, `fog`는 전역이라 스테이지가 바꾸면 **월드도 같이 바뀐다**(월드는 안 보이니 상관없지만, 복구 누락 시 게임 전체가 어두워진다). `UltimateGameplayLockContext`와 같은 "소유권 기록 후 자기 것만 복구" 패턴을 반드시 적용한다. `WorldLightingManager`/`WorldLightingController`가 같은 값을 관리하므로 **경합한다** — 스테이지 활성 중에는 `WorldLightingController` 갱신을 일시 정지시키는 조정이 필요하다.

### 7.6 리플렉션 프로브 / GI

무대는 런타임 스폰이라 베이크 GI가 없다. 무대 프리팹에 **박스 프로젝션 리플렉션 프로브 + Custom 큐브맵**을 미리 넣어 배송한다(런타임 베이크 금지 — 히치). 캐릭터가 `ambient = black`에서 새까맣게 나오는 문제는 무대 프리팹 안의 전용 라이트 리그(키/필/림)로 해결한다.

### 7.7 프리워밍

무대 프리팹 첫 인스턴스화 = 셰이더 컴파일 + 텍스처 업로드 히치. 궁극기는 게이지가 차면 언제든 나가므로, **스킬 게이지 만충 시점에 무대 프리팹을 비활성 상태로 미리 인스턴스화**해두는 예열이 필요하다. `GameObjectManager` 풀링에 얹는다.

### 7.8 오디오

무대는 1500m 아래라 월드의 3D 사운드가 자연히 감쇠한다(장점). 다만 앰비언트 루프가 2D면 그대로 들리므로 스테이지 진입 시 앰비언트 버스 덕킹을 넣는다. 리스너는 카메라를 따라가므로 별도 처리 불필요.

### 7.9 비참가 액터에 대한 피해

무대 안 광역기는 무대에 없는 월드 몬스터를 못 때린다. **정책을 명시적으로 정한다**: 스테이지 연출의 판정 대상은 참가자 집합으로 한정하고, 필요하면 복귀 시 "월드 잔여 대상에 결과 적용" 패스를 별도로 돈다. 암묵적으로 두면 "궁극기가 T2일 때만 데미지가 덜 들어간다"는 밸런스 버그가 된다.

### 7.10 Tier 강등의 관측 가능성

원신이 물 위에서 컷신을 스킵하듯, 강등/폴백은 정상 동작이다. 다만 **디버그 로그와 치트 오버레이에 강등 사유를 노출**해야 한다. 안 그러면 "가끔 연출이 안 나온다"는 재현 불가 버그로 남는다.

---

## 8. 구현 Phase

| Phase | 범위 | 완료 기준 |
|-------|------|-----------|
| **P1** | 계약 + 매니저 골격 + T2(스테이지 이동) + 앵커 검증 + 화면 전환 + 티켓/복구/워치독 | 궁극기 1종이 지하 무대에서 재생되고, 인터럽트·사망·씬전환 모두에서 월드로 정상 복귀 |
| **P2** | 라이팅 컨텍스트 + `WorldLightingController` 조정 + 무대 프리팹 라이트 리그 + 리플렉션 프로브 | 무대에서 캐릭터가 의도한 톤으로 보이고, 복귀 후 월드 라이팅이 진입 전과 픽셀 동일 |
| **P3** | 지연 플러시(사망/드롭/경험치) + 세이브 게이트 + 비참가 대상 정책 | 무대에서 처치한 몬스터의 드롭/경험치가 월드 정위치에 생성. 연출 중 저장 시도가 차단됨 |
| **P4** | 프리워밍/풀링 + T1(인플레이스) + Tier 자동 강등 + 치트 진단 표시 | 첫 발동 히치 없음. 좁은 실내에서 자동 강등 동작 확인 |
| **P5** | 타임라인 구간 스테이지 이벤트 + 궁극기 시퀀스 에디터 트랙 표시 | 에디터 타임라인에서 무대 구간이 시각적으로 보이고 저작 가능 |
| **P6** (선택) | T3 가상 씬(Additive) | 보스 처형 연출 1종이 전용 씬에서 재생 |

P1~P3이 "안전한 시스템"의 최소 단위다. P4 이후는 품질/저작 편의.

---

## 9. 검증 계획

### EditMode
- `CinematicStageAnchorResolver`: 콜라이더가 있는 후보를 거르고, 전부 막히면 identity 폴백을 반환하는가
- `S` / `S⁻¹` 왕복 변환의 위치·회전 오차가 허용치 이내인가
- 티켓 소유권: 무효 티켓의 `Exit` 호출이 무시되는가

### PlayMode (수직 슬라이스)
- 진입 → 복귀 후 참가자 위치·회전이 진입 전과 동일
- 모션 중단(피격/사망/씬 전환) 각 경로에서 무대가 남지 않음
- 라이팅 복구: 진입 전후 `RenderSettings` 값 동일
- 워치독: 소유자를 강제 파괴해도 `maxStageSeconds` 내에 무대가 회수됨

### 수동 체크리스트
- 좁은 실내 / 절벽 / 물 위 / 지하 던전에서 각각 발동
- 연출 중 일시정지 → 재개, 연출 중 세이브 시도
- 연속 발동(캐릭터 스왑 후 즉시 궁극기)

---

## 10. 열린 질문 (결정 필요)

1. **무대 프리팹을 캐릭터별로 둘 것인가, 속성/등급별 공용으로 둘 것인가.** 12캐릭터 × 전용 무대는 제작비가 크다. 공용 무대 3~4종 + 캐릭터별 VFX/라이팅 오버라이드가 현실적으로 보인다.
2. **T2를 궁극기 전체에 적용할 것인가, 일부만 적용할 것인가.** 원신처럼 "일부 캐릭터만 컷신"으로 가면 연출 밀도 차이가 캐릭터 개성이 된다.
3. **연출 스킵 기능.** 반복 플레이에서 매번 3초 무대는 피로하다. 스킵 시에도 게임플레이 결과가 동일해야 하므로 2절의 원칙이 여기서도 근거가 된다.
4. **사이클 런 텔레메트리와의 관계.** `CycleTelemetrySession`이 무대 체류 시간을 전투 시간으로 셀지 여부.
