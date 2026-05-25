# GameCameraCalculator.cs 코드 리뷰 (구문별 정밀 검토)

> 대상 파일: `unsave/GameCameraCalculator.cs` (512줄)
> 작성일: 2026-05-25 (정밀 검토 개정, Advisor 교차검증 반영)
> **전제: 현재 UPlayground와 무관한 독립 참고 코드.** 이식 관점이 아니라 코드 자체의 **① 로직·계산 정확성**과 **② 비대한 구조**에 집중.

---

## 0. 결론 (사용자 질문 직답: "계산상 틀린 부분이 없는가?")

- **산술(arithmetic) 계산이 틀린 곳은 없다.** 모든 수식 — 부호, 정규화, 내적/외적, 투영, 최종 위치 합성 — 은 구문별로 검증한 결과 내부적으로 일관되고 정확하다. (근거는 §A-4)
  - 단 **"산술이 정확함 ≠ 의미론적으로 올바름"**. 산술은 맞지만 *목적 대비* 결함인 곳이 있다 — 대표 사례가 floor rescue 임계(바닥 관통 허용, §E-2). CODEX 교차검증으로 보완.
- **단, 거동상 잘못된 결과를 내는 곳이 1곳 있다:** `GetCameraColliderPos`(`:434`)의 **기준축 선택 오류**. 산술이 틀린 게 아니라, 캐스트의 기하 기준을 "카메라 실제 위치"가 아닌 "카메라 회전"에서 잡는 **로직(축 선택) 오류**다. → §A-1
- **잠재(조건부) 오류 2건:** 레이어 이름 미발견 시 마스크 오염(`-1 → 1<<31`, §A-2), 멀티 터레인에서 타일 불일치(§A-3).
- **의미론적 의심 1건:** 바닥 구제(수직 보정)가 `CamColliderHit`(=줌 인터럽트)을 켠다. → §A-5(1)
- **구조 문제:** 512줄 단일 static 클래스에 6책임 + 부수효과 + 4중복. → §B

| # | 위치 | 분류 | 한 줄 |
|---|---|---|---|
| A-1 | `:434-509` | 🔴 로직 오류(축 선택) | 캐스트 방향을 회전에서 유도 → 실제 위치축과 불일치 |
| A-2 | `:17-22` | 🟠 잠재 버그 | `NameToLayer==-1 → 1<<31` 마스크 오염 |
| A-3 | `:307-315` | 🟠 조건부 오류 | `GetTerrain(tpos)` + `SampleHeight(cpos)` 타일 불일치 |
| A-5.1 | `:128` | 🟡 의미론 | 바닥 구제가 `CamColliderHit`/줌 인터럽트를 켬 |
| A-5.2 | `:120` | 🟡 엣지 | skin push 후 재검증 없음 (인접 벽 매립 가능) |
| B-1 | 전반 | 🟠 구조 | 단일 클래스 6책임 + 디졸브 부수효과 |
| B-2 | 4곳 | 🟡 구조 | 직교기저+링프로브 중복 |
| B-3 | `:434-509` | 🟡 구조 | 레거시·MultiProbe 2중 경로 |

---

# A. 로직 · 계산 정확성 (메서드별 구문 검증)

> ✅ 검증상 정확 / ⚠️ 조건부·한계 / ❌ 오류

| 메서드 | 줄 | 판정 | 근거 요약 |
|---|---|---|---|
| `ContainsFrustum` | `:27` | ✅ | 뷰포트 사각형 + `z>0` 정면 판정 |
| `ProcessColliderRevise` | `:77` | ✅(위임) | 산술 정확. 미사용 파라미터·no-op 반환 |
| `ProcessColliderReviseMultiProbe` | `:94` | ✅⚠️ | skin push 부호 정확, 단 재검증 없음(A-5.2) |
| `IsLineToCameraClear` | `:139` | ✅ | Linecast 부정 |
| `ProbeCameraReachMultiProbe` | `:149` | ✅ | 축 투영 최소값. n=0 시 0-나눗셈 안전 |
| `IsCameraPositionClear` | `:188` | ✅ | center+ring 검사 |
| `EnsureGroundClearance` | `:221` | ⚠️ | 수식 정확, fallback 지면 상이 가능 |
| `EnsureCameraNotBelowFloor` | `:263` | ✅산술 / ❌의미론 | `safeT` 산술 정확하나 임계가 바닥 관통 허용(§E-2) |
| `GetTerrainPos` | `:303` | ⚠️ | 높이 공식 정확, 타일 불일치(A-3) |
| `GetCameraColliderPosMultiProbe` | `:332` | ✅ | 투영·끼임 fallback 정확 |
| `GetCameraColliderPos` | `:434` | ❌ | 축 선택 로직 오류(A-1) |

---

## A-1. 🔴 `GetCameraColliderPos` — 축 선택 로직 오류 (산술 아님)

```csharp
var camFoward    = curRot * Vector3.forward;                   // :446  (오타: Foward)
var targetToCam  = -camFoward.normalized;                      // :447  카메라 방향을 "타겟→카메라"로 가정
var targetHitLength = Vector3.Dot((tpos - cpos), camFoward);   // :448
bool isHit = Physics.SphereCast(tpos, colliderRadius,
                 targetToCam.normalized, out var hitRay, targetHitLength, _camCldLayer); // :450
...
cldCamPos = tpos + hitRay.distance * targetToCam.normalized;   // :467
```

**산술은 일관됨.** `Dot` → `SphereCast` → `tpos + dist*dir` 합성은 그 자체로 올바르다. **틀린 것은 "어떤 축을 기준으로 삼느냐"** 다.

- **축 오류:** 타겟→카메라의 실제 방향은 `(cpos - tpos).normalized` 다. 그런데 코드는 `targetToCam = -(curRot*forward)`, 즉 **카메라가 바라보는 방향의 반대**를 쓴다. 두 벡터는 *카메라가 타겟을 정확히 정조준할 때만* 일치한다. TPS의 어깨너머 오프셋, 카메라 보간 지연, 락온 중 시선-위치 분리 등에서 시선축과 위치축이 θ만큼 어긋나면:
  - **방향이 틀려** 엉뚱한 벽을 잡거나 못 잡고,
  - **최종 위치도 틀려** 카메라가 시선축 위로 끌려 붙는다(실제로는 축 밖에 있었는데도).
- **길이 축소:** `targetHitLength = Dot(tpos-cpos, camFoward)` = 실제 거리의 시선축 투영 = `실제거리·cosθ`. 시선이 벌어질수록 캐스트가 짧아져 근처 벽을 놓친다.
- **자기모순:** 같은 메서드의 2차 fallback(`:475-488`)은 **올바르게 위치축**을 쓴다 — `actualDir = cpos - tpos`(`:477`). 1차는 회전축, 2차는 위치축이라 한 함수 안에서 기준이 엇갈린다.
- **반례 존재:** `GetCameraColliderPosMultiProbe`(`:346`)는 처음부터 `axis = cpos - tpos`(위치축)을 써서 이미 올바르다.

**수정안:** 1차 캐스트도 위치축으로 통일 → `curRot` 파라미터 자체가 불필요해짐.
```csharp
Vector3 axis = cpos - tpos;
float dist = axis.magnitude;
if (dist < 1e-5f) { cldCamPos = cpos; return false; }
Vector3 dir = axis / dist;
bool isHit = Physics.SphereCast(tpos, colliderRadius, dir, out var hitRay, dist, _camCldLayer);
...
cldCamPos = tpos + hitRay.distance * dir;
```
부수: `camFoward` 오타, `.normalized` 중복 호출(`targetToCam` 이미 단위인데 `:450,467` 재호출), `targetHitLength` 음수(카메라가 타겟 앞쪽) 가드 없음.

---

## A-2. 🟠 레이어 마스크 — `NameToLayer == -1` 오염 (잠재 버그)

```csharp
private static int _camCldLayer = (1 << LayerMask.NameToLayer("CameraCollider") |  // :17
                                   1 << LayerMask.NameToLayer("WalkableGround") |
                                   1 << LayerMask.NameToLayer("Obstacle"));
```
- **우선순위 정상:** C#에서 `<<` > `|` 라 `(1<<a)|(1<<b)|(1<<c)` 로 의도대로 평가. ✅
- **잠재 버그:** `NameToLayer("없는이름") == -1`, 그리고 C# 시프트 카운트는 `int` 에서 `& 0x1F` 로 마스킹 →
  ```
  1 << -1  ≡  1 << (-1 & 31)  ≡  1 << 31  ≡  int.MinValue
  ```
  이름 미발견 시 **조용히 layer 31 비트가 켜진 잘못된 마스크**. 에러가 없어 오타/레이어 삭제 시 "충돌이 그냥 안 먹는" 무증상 고장.
- **부수:** static 필드 초기화자에서 예외 발생 시 `TypeInitializationException` 으로 래핑되어 클래스 전체가 사용 불가가 되는 취약성도 있음.
- **수정안:** 초기화 시 `-1` 검증(`Bit()` 헬퍼 + `Debug.LogError`) 또는 인스펙터 `LayerMask` 외부화.

---

## A-3. 🟠 `GetTerrainPos` — 높이 공식은 정확, 타일 선택이 위험

```csharp
public static bool GetTerrainPos(ref Vector3 cpos, out Vector3 rpos, ref Vector3 tpos)  // :303
{
    rpos = cpos;
    //return false;                                                        // :306 죽은 줄
    Terrain terrain = GrTerrainManager.Instance?.GetTerrain(tpos);         // :307
    if (terrain == null) return false;
    float radius = 0.5f;                                                   // :314 하드코딩
    float terrainPosY = terrain.GetPosition().y + terrain.SampleHeight(cpos) + radius; // :315
    if (cpos.y <= terrainPosY) { rpos.y = terrainPosY; return true; }
    return false;
}
```
- **높이 공식 ✅:** `Terrain.SampleHeight(worldPos)` 는 해당 터레인 transform 기준 상대 높이 → `GetPosition().y + SampleHeight(...)` = 월드 높이. 부호·합 정확.
- **⚠️ 타일 불일치:** 터레인은 `GetTerrain(tpos)`(캐릭터)로 고르는데 높이는 `SampleHeight(cpos)`(카메라)로 샘플. `SampleHeight` 는 *선택된 그 터레인의 로컬 공간* 기준이라, 카메라가 그 타일 footprint 밖이면 경계 clamp된 잘못된 높이. 단일 터레인이면 무해, 멀티면 버그 → `GetTerrain(cpos)` 로 재선택.
- **🟡 `ref` 오용:** `cpos`, `tpos` 가 `ref` 인데 미수정 → `in` 또는 값 전달이 옳음.
- **🟡** `radius=0.5f` 하드코딩, `//return false;` 죽은 줄.

---

## A-4. ✅ 정확성이 검증된 핵심 수식 (오류 아님 — 근거 명시)

사용자가 계산 오류를 우려했으므로 "맞다"는 것도 근거와 함께 남긴다. **아래는 모두 산술 검증 통과.**

**(1) skin push 방향 — `:113-120`** ✅
`axisDir=(curPos-targetPos)정규화`(타겟→카메라). 사이 벽 정면 normal은 광선(`tpos→cpos`=+axisDir) 반대 = `-axisDir` 쪽 → `Dot(hitNormal,-axisDir)≈+1`. push `+hitNormal`(벽→타겟)로 카메라를 당겨 밀착 방지. 방향 정확. clamp `Min(Max(0,skin), r*0.5)` 정상.

**(2) floor rescue `safeT` 산술 — `:291-296`** ✅(산술 한정)
`axisDir.y<-1e-3` 보장 하 `safeT=-r/axisDir.y>0`. `t=safeT` 의 y오프셋 `= axisDir.y·(-r/axisDir.y)=-r` → 카메라 y를 `tpos.y-r` 로. `currentT=Dot(cldCamPos-tpos,axisDir)=axisLen`, `currentT>safeT` 일 때만 당김(단조 후퇴). **산술은 정확**.
⚠️ 한계: `axisDir.y`가 `-1e-3` 근접(거의 수평)이면 `safeT` 폭증 → 수평으로 빠진 케이스 미구제.
❌ **의미론(§E-2에서 CODEX 반론 채택):** 발동 임계 `cy >= groundY-r → 미구제`(`:287`)는 구체가 지면을 관통해도(중심이 지면 아래 `r` 미만) 통과시킴 — 무관통 경계는 `groundY+r`이므로 약 `2r` band가 미구제. 또 목표 `tpos.y-r`는 pivot 상대(지면/경사 무관). "바닥 관통 방지" 목적 미충족.

**(3) 축 투영 최소값 — `:160,177,366,384`** ✅
`Dot(hit.point-tpos, axisDir)` = 축 투영 거리, 중앙+링 최소값 = 축상 최근접 장애물, `Max(0,...)` 음수 클램프. 정확.

**(4) 직교 기저 — `:164-168,201-205,353-357`** ✅
`right=Cross(axisDir,up)`, 평행 시 `Cross(axisDir,forward)` fallback, `up=Cross(right,axisDir)`. 정규직교 기저. 링을 2π 전체 스윕하므로 handedness 무관.
참고: fallback 임계 `right.sqrMagnitude<1e-4f`(`:165,202,354`)는 axisDir이 world-up과 ~0.57° 이내일 때 발동. 적절하나 튜너블 상수.

**(5) `ContainsFrustum:45-48`** ✅ `WorldToViewportPoint` z(정면>0)+xy∈[0,1]. (near/far clip 미반영은 용도상 무해.)

**(6) `EnsureGroundClearance:227-253`** ✅ `cam.y-ground.y<minClearance → cam.y=ground.y+minClearance`. 정확.
⚠️ 1차 raycast가 짧아(`minClearance+2r`) 미검출 시 pivot fallback — 지면이 카메라 밑과 다를 수 있고(단차), 카메라가 높을 때 매 프레임 불필요 raycast.

**(7) `ProbeCameraReachMultiProbe`/`IsCameraPositionClear` n=0 처리** ✅ `n=Mathf.Max(0,probeCount)` 후 `i<n` 루프 → n=0이면 미실행이라 `(2π·i)/n` 0-나눗셈 발생 안 함.

---

## A-5. 구문별 정밀 패스 — 추가 검출 (소항목)

1. **🟡 의미론 — 바닥 구제가 줌 인터럽트를 켬 (`:128`).**
   `moved |= EnsureCameraNotBelowFloor(...)` → 수직 바닥 보정이 성공하면 `CamColliderHit=true` → `IsZoomInterrupted=true`(`CamUpdateInfo:148`). 벽 충돌이 아닌 **수직 보정인데 줌이 중단**될 수 있음. 충돌 플래그와 구제 플래그를 분리 권장.
2. **🟡 엣지 — skin push 후 재검증 없음 (`:120`).**
   `cldPos += hitNormal*clampedSkin` 뒤 그 위치가 여전히 clear한지 확인 안 함. 좁은 코너에서 벽 A의 normal로 밀다가 직교 벽 B에 매립 가능. `radius*0.5` clamp가 깊이를 제한할 뿐 제거하진 못함.
3. **🟡 중복 — `axis=curPos-targetPos` 재계산 (`:108`).**
   `GetCameraColliderPosMultiProbe`(`:346`)가 이미 동일 축을 내부 계산했는데 호출부에서 재계산. 사소하나 구문 단위 기록.
4. **🟡 죽은/유령 — 주석이 외부 심볼 참조 (`:125`).**
   주석에 `_currentArmLength`, `safeLen`, `SmoothDamp` 언급 — 이 파일에 없음. "Calculator"가 **호출부(카메라 본체)의 통합 가정에 암묵 결합**돼 있다는 신호.
5. **🟡 빌드 분기 2곳 (`:343`, `:443`).**
   `if (Application.isEditor == false && checkNoActors) return false;` 가 두 충돌 본체에 중복. 에디터/빌드 동작이 갈리고, **빌드에서 액터 없으면 충돌 보정 자체를 스킵**(로딩 중 무보정). §B-1과 직결.
6. **🟡 반복 프로퍼티 접근 (`:340-341`, `:439-441`).**
   `SRGameManager.Instance.ActorController.GameActorList` 를 null 체크·Count에서 반복 접근. 로컬 변수 캐싱 부재.
7. **🟡 매 프레임 O(actors) 디졸브 순회 (`:421-428`, `:498-505`).**
   카메라가 캐릭터/허트박스 근처일 때 전체 액터 루프 + `CheckInPointByBodySize`. 액터 많으면 비용. §B-1에서 분리하며 함께 정리.

---

# B. 구조 · 크기 ("코드가 너무 크다")

512줄 **단일 static 클래스**가 6책임을 담음 — 비대화의 근본 원인.

| 책임 | 메서드 |
|---|---|
| ① 가시성 | `ContainsFrustum` |
| ② 벽 충돌 (단일) | `ProcessColliderRevise`, `GetCameraColliderPos` |
| ② 벽 충돌 (MultiProbe) | `ProcessColliderReviseMultiProbe`, `GetCameraColliderPosMultiProbe`, `Probe…`, `Is…Clear` |
| ③ 지면 클리어런스 | `EnsureGroundClearance` |
| ④ 바닥 구제 | `EnsureCameraNotBelowFloor` |
| ⑤ 터레인 | `GetTerrainPos` |
| ⑥ 디졸브(부수효과) | `Get…ColliderPos*` 내부 |

**B-1. 🟠 디졸브 부수효과 분리.** `Get…ColliderPos*` 가 위치 계산 중 액터 디졸브 상태를 변경(`:418-429,495-506`)하고, 그 때문에 충돌 보정이 액터 존재에 묶임(`:343,443`). → `TriggerCameraOcclusionDissolve(pos,radius)` 로 분리, 보정은 액터 무관 수행.

**B-2. 🟡 중복 제거.** 직교기저+링프로브 패턴 4벌(`:164,201,353` + 호출) → `BuildAxisBasis` / `RingOffsets` 헬퍼 추출 (~60줄 감축 + 일관성).

**B-3. 🟡 2중 경로 정리.** 레거시 단일 캐스트(§A-1 부정확)를 MultiProbe로 일원화 → `:434-509` 약 75줄 삭제 가능.

**제안 분할:**
```
CameraVisibility        : ContainsFrustum
CameraCollisionSolver   : 충돌 보정(MultiProbe 단일화) + 공유 프로브 헬퍼
CameraGroundSolver      : EnsureGroundClearance + EnsureCameraNotBelowFloor + GetTerrainPos
CameraOcclusionDissolve : 디졸브 트리거(부수효과 격리)
CameraMath (internal)   : BuildAxisBasis, RingOffsets, 공유 epsilon 상수
```

**B-4. 🟡 잡정리.** 죽은 주석블록(`:29-43,50-53,54-64,306`), 죽은 using(`BinaryFormatter:5`, `Profiling:10`), 미사용 파라미터(`colliderHitLength:78`, 수정 후 `curRot`), 9개 인자→설정 struct(`:94`), `CamUpdateInfo` 반환 no-op(class라 in-place·재대입 무의미 → `void` 또는 struct), 매직넘버(`1e-5/1e-4/1e-3/0.5/1.5/10/*2`)→상수, 미완성 주석(`:116`).

---

## C. 우선순위

1. **A-1** — 캐스트 축을 위치축으로 통일(거동 버그). `curRot` 제거.
2. **A-2** — 레이어 `-1` 가드.
3. **B-1 / A-5.5** — 디졸브 분리 + 액터-게이팅 제거(정확성·구조 동시).
4. **A-5.1** — 바닥 구제와 줌 인터럽트 플래그 분리.
5. **B-3 → B-2** — 레거시 제거 + 헬퍼 추출(~130줄 감축).
6. **A-3 / A-5.2 / B-4** — 터레인 타일, skin 재검증, `ref`, 잡정리.

---

# D. 충돌 통합 · 블렌딩 · 락온 (관련 파일)

> `GameCameraCalculator` 는 충돌 "계산"만 한다. 그 결과를 받아 **줌/위치로 블렌딩**하고 **락온 상태**를 다루는 코드는 같은 `unsave/` 폴더의 다른 3개 파일에 있다.
> - `SRControllableCameraBase.cs` — 충돌→줌 블렌딩 통합(`UpdateCamProperty`), 회전/정렬 블렌딩
> - `ZoomCollisionState.cs` / `LockOnState.cs` — 줌·충돌·락온 **상태 데이터 구조체** (로직 없음, 필드만)
>
> **범위 한정:** 락온 핵심 알고리즘(`SignedOffsetAngle`/`FreeFactor`/`SideFlip`/`ActiveFocus` 를 실제로 굴리는 코드)은 본 4개 파일에 **없다**(파생 클래스 미포함). 따라서 락온은 *상태 구조체 설계* 관점에 한정한다.

## D-1. 충돌 통합 — 프로덕션이 버그 경로를 사용

`UpdateCamProperty`(`SRControllableCameraBase.cs:363-467`)가 실제 카메라 충돌 보정 진입점이다.

- 🔴 **`:380` 이 §A-1의 버그 경로를 호출한다** — `GameCameraCalculator.ProcessColliderRevise`(단일 SphereCast, 회전 기반 축). 즉 §A-1의 축 선택 오류가 **프로덕션에서 라이브**이며, 우월한 MultiProbe 경로는 미사용.
- 🟡 **`:384-385` 회전 기반 축 재계산 중복** — `camFoward = curRot*forward`, `targetHitLength = Dot(targetPos-curPos, camFoward)` 를 계산기 내부와 동일하게 또 수행. A-1과 같은 시선축 투영 부정확성 + 코드 중복.
- 🟠 **`:396, :410-414` `nextZoomRate`/`_zoomRate` 미클램프 (검증 완료).**
  `nextZoomRate = (newcamLength - min)/(max - min)` 는 [0,1] 로 clamp되지 않는다. 벽이 `minCamRenderLen` 보다 가까우면 음수가 되고, `:412 length = Mathf.LerpUnclamped(min, max, _zoomRate)` 와 `:414 UpdateCameraTfm(_zoomRate, …)` 로 그대로 전달된다. 후자 float 오버로드(`:499-502`)는 **clamp하지 않는다** — `_zoomRate` 의 `Clamp01`(`:495`)은 매개변수 없는 다른 오버로드 전용이라 이 경로엔 적용 안 됨. 결과: 근접 충돌 시 카메라 길이가 min 미만/범위 밖으로 산출될 수 있음.
- 🟡 **`:396` 분모 중복 계산** — `renderDiff`(`:395`)와 동일한 `(max-min)` 을 인라인 재계산.

## D-2. 🔴 블렌딩 — 프레임률 의존 보간 (rotation은 맞고 zoom은 틀림)

같은 클래스 안에서 **두 가지 보간 모델이 엇갈린다.**

**✅ 회전 블렌딩은 올바름** — `UpdateCameraRotation`(`:927-951`):
```csharp
Mathf.SmoothDampAngle(_cameraRotY, _targetRotY, ref _rotationVelocityY, smoothedTime, Mathf.Infinity, Time.deltaTime); // :934, :944
```
`SmoothDamp` 는 `deltaTime` 을 적분에 반영해 **프레임률 독립**이다. 정석.

**❌ 줌 블렌딩은 프레임률 의존** — `UpdateCamProperty`:
```csharp
_zoomRate = Mathf.Lerp(_zoomRate, nextZoomRate, lerpVal * Time.deltaTime); // :408
_zoomRate = Mathf.Lerp(_zoomRate, _zoomRateOrigin, Time.deltaTime * 2f);   // :437
```
`Mathf.Lerp(현재, 목표, k·dt)` 는 전형적 프레임률 의존 안티패턴이다. 같은 벽시계 시간이라도 FPS에 따라 결과가 달라지고, **저FPS에서는 `k·dt` 가 커져 부드러운 접근이 거의 스냅으로 무너진다**(`lerpVal` 은 `:403` 에서 최대 21까지 → 30fps에서 `21·0.033≈0.7`, 10fps에서 `≈2.1`). *오버슈트는 아니다 — `Mathf.Lerp` 는 `t` 를 [0,1] 로 clamp하므로 목표를 넘기진 않는다*(넘기는 건 `LerpUnclamped`이며 그건 D-1의 `:412` 별건). 핵심 결함은 **프레임률 의존 + 저FPS 스냅 붕괴**.
→ 회전부처럼 `SmoothDamp` 또는 지수 평활 `1 - Exp(-k·dt)` 로 통일 권장.

**🟡 두 충돌-블렌딩 모델 공존(정책 확인 필요).** `ZoomCollisionState.cs` 는 "length(미터) 도메인 단일 SmoothDamp 모델"(`ArmLengthVelocity`, `SmoothedSafeLen`, `CollisionViewBlend`)을 서술한다 — 이것이 `GameCameraCalculator:125` 의 유령 주석(`_currentArmLength`/`safeLen`/`SmoothDamp`)이 가리키던 소비자다. 그런데 이 베이스의 `UpdateCamProperty` 는 옛 **zoomRate-Lerp** 모델을 쓴다. 둘 중 무엇이 최신인지/대체 관계인지는 본 파일들만으로 단정 불가 — **한 시스템 안에 두 보간 모델이 공존하므로 일관된 정책인지 확인 필요**(예: 베이스=비락온 경로, length-SmoothDamp=락온 파생 경로일 가능성).

**🟡 정밀/에폭실론 불일치.** `:407` 은 `EPSILON(1e-5)`, `:435` 는 `0.000000001(1e-9)` 하드코딩. `:446 if (_zoomRateOrigin != _zoomRate)` 는 float **정확 등치 비교** — `:441` 의 정확 대입에 의존하므로 동작은 하나 취약한 스타일.

## D-3. 정렬 블렌딩 — 대체로 건전, 상태 결합 주의

`UpdateAlignment`(`:1002-1039`):
- ✅ `normalized = Clamp01(elapsed/duration)` 시간정규화 → **프레임률 독립**. `duration>0` 0-나눗셈 가드(`:1024`)도 있음.
- 🟡 **선형 보간(ease 없음)** — `LerpAngle(start, target, normalized)` 라 시작/끝이 급격. 연출 품질상 ease-in/out 고려.
- 🟡 **정렬↔회전 상태 결합 취약.** `UpdateAlignment` 진입 시 `_isRotatingX/Y=false`(`:1004-1005`)로 수동 회전을 끄고, 완료 시 `FinalizeAlignmentSnap`(`:1056-1065`)이 `_cameraRot`·`_targetRot` 동기화 후 다시 `_isRotatingX/Y=true` 로 켠다. 주석(`:1042-1044`)이 과거 "카메라 튐" 이슈를 명시 — stale target 으로 인한 점프를 막는 정교하지만 깨지기 쉬운 상태 기계.
- 🟡 **`CheckIsStopX/Y`(`:973-981`) 속도-only 정지(`|vel|<0.1`).** 남은 각도를 무시하고 속도만 본다. 속도가 일시적으로 0.1 아래로 떨어지면 목표에 도달하기 전 정지 가능. 임계 `0.1` 매직넘버.

## D-4. 락온 상태 구조체 (LockOnState.cs) — 복잡도 플래그

> 로직은 없으므로 구조 설계만 평가.

- 🟡 **단일 구조체에 30+ 필드, 8개 SmoothDamp velocity 채널** (`TargetPosVelocity`, `BlendTVelocity`, `OrbitPitchVelocity`, `OffsetAngleVelocity`, `FreeFactorVelocity`, `InitialReleaseFactorVelocity`, `ActiveFocusPosVelocity`, `ActiveFocusRatioVelocity`). 독립 평활 채널이 8개라는 것은 소비 로직(미포함)이 그만큼 많은 튜닝 파라미터·상호작용을 가진다는 신호 → 정합 유지·디버깅 난이도 높음. 책임별 하위 구조체 분리(`LockOnBlend`, `LockOnSideFlip`, `LockOnFocus`) 고려.
- 🟡 **매직 센티넬** `LastSideFlipUnscaledTime = -100f`(`:107`) — "충분히 과거" 의도의 매직값. 명명 상수 권장.
- 🟡 **`struct`(값 타입) 변형 함정.** 필드로 보유해 in-place 변형하는 전제(주석 `:4`)라면 OK지만, 프로퍼티 getter로 노출하면 복사본을 변형하게 됨. ~120B 복사 비용도 값 전달 시 주의. `class` 가 의도에 더 안전할 수 있음.

## D-5. 우선순위 (D 섹션)

1. **D-2 줌 블렌딩 프레임률 독립화** — 회전부와 동일하게 `SmoothDamp`/지수평활로. (체감 품질·플랫폼 일관성 직결)
2. **D-1 충돌 경로 교정** — `:380` 을 MultiProbe로 교체(§A-1 해소) + `nextZoomRate` `Clamp01`.
3. **D-2 보간 모델 일원화** — zoomRate-Lerp vs length-SmoothDamp 정책 확정.
4. **D-3 정렬/회전 상태 결합 정리** + `CheckIsStop` 에 각도 조건 추가.
5. **D-4 LockOnState 분할** + 센티넬 상수화.

---

# E. CODEX 리뷰 교차검증 반영 (`CODEX_REVIEW_CAMERA.md` 대조)

별도 진행된 CODEX 리뷰와 상호 대조했다. **두 리뷰가 독립적으로 다음 11개에 수렴**(상호 검증 완료): A-1 축 선택 로직 오류 · A-2 레이어 `-1` 오염 · A-3 터레인 타일 불일치 · A-5.1 floor 플래그↔줌인터럽트 · A-5.2 skin 재검증 부재 · D-1 프로덕션이 단일 캐스트 경로 사용 · D-1 `nextZoomRate` 미클램프 · D-2 프레임률 의존 Lerp · D-2 두 보간 모델 공존 · D-3 `CheckIsStop` 속도-only · D-4 `LockOnState` 과밀. → 핵심 결론은 양쪽 동일.

아래는 CODEX가 **추가로 정확히 짚어 본 문서에 채택**하는 항목.

## E-1. 🟠 MultiProbe ring 기하 — 반지름 튜브가 아닌 pivot 부채꼴 (신규)
모든 ring probe가 `Linecast(tpos, cpos+offset)` 로 **동일 pivot에서 출발**한다(`:382, :175, :212` 검증). 의도(코너 지터 완화)는 이해되나 결과는 반지름 원통 검사가 아닌 pivot 고정 부채꼴이다:
- **pivot 근처**: probe가 한 점으로 수렴 → 반지름 측면 커버리지 소멸(좁은 기둥/문틀 보호 약화).
- **카메라 근처**: probe가 반지름보다 넓게 벌어짐 → 과보수적(실제보다 먼 벽 감지).
→ 진짜 반지름 검사가 목표면 `Linecast(tpos+offset, cpos+offset)`(평행 probe). pivot 근처 보호는 center probe로 별도. (투영 수식 자체는 §A-4(3)대로 정확 — 기하와 별개 문제.)

## E-2. ❌ floor rescue 임계가 바닥 관통 허용 (§A-4(2) 판정 보완 — CODEX 반론 채택)
앞서 `EnsureCameraNotBelowFloor` 를 "산술 정확"으로만 평가했으나 CODEX 반론이 타당해 보완한다. **safeT 산술은 여전히 정확**하지만 임계·기준이 목적을 못 채운다:
- 발동 조건 `cldCamPos.y >= groundY - r → 미구제`(`:287`) — 카메라 중심이 지면보다 `r` 이상 아래여야 발동.
- 구체(중심 cy, 반지름 r) 무관통 경계는 `cy >= groundY + r`. 임계는 그보다 `2r` 낮음 → **중심이 지면 아래로 잠겨도 구제 안 되는 band 존재**.
- `EnsureGroundClearance` 는 카메라에서 **아래로** raycast → 카메라 머리 위 바닥(카메라가 바닥 밑)인 이 band를 못 잡음 → 두 함수 사이 사각지대.
- 보정 목표 `tpos.y - r` 는 **pivot 상대**(지면/경사 무관, 높은 pivot에서 과당김).
→ ground 기준 권장: `safeY = groundY + r`, `safeT = (safeY - tpos.y)/axisDir.y` 후 `[0, axisLen]` clamp. (§0의 "산술 정확 ≠ 의미론적 정확" 대표 사례.)

## E-3. 🟠 `minNormalAlignment` 가 hit 채택에 미반영 (신규)
`ProcessColliderReviseMultiProbe(... minNormalAlignment ...)` 가 이 값을 **skin 적용(`:114`)에만** 쓰고, 실제 hit를 고르는 `GetCameraColliderPosMultiProbe` 엔 **전달조차 안 함**(`:103-104`). 내부는 `projLen<bestProjLen` 만으로 모든 hit 채택(`:367,385`) → **거의 평행하게 스친 표면/모서리 hit도 카메라를 당김**. 파라미터 이름이 "hit를 거른다"는 오해를 줌. → 채택 시점에 `Dot(hit.normal, -axisDir) >= minNormalAlignment` 필터 추가.

## E-4. 🟠 락온 sign 결정 ↔ 최종 보정 기준 불일치 (충돌·락온 상호작용, 신규)
`ProbeCameraReachMultiProbe` 는 주석(`:146`)상 **락온 좌/우 sign 결정용 reach 비교**에 쓰인다. 그런데:
- reach 함수엔 skin·normal alignment·CheckSphere fallback이 **없고**, 최종 보정 `ProcessColliderReviseMultiProbe` 엔 **있다** → **고른 방향(reach 기준)과 실제 카메라 도달 위치(최종 기준)가 다름.**
- ring basis가 후보 축마다 새로 계산(E-1) → 좌/우 후보가 **같은 월드 방향을 비교하지 않음** → 대칭 장애물에서도 reach 비대칭 → **좁은 모서리에서 sign 매 프레임 flip**(흔들림 증폭). `LockOnState.SideFlip*`/`SustainedCollidingElapsedSec`(§D-4)가 이를 막으려는 흔적.
- floor rescue가 reach엔 없는 건 **올바름**(sign은 "어느 쪽이 덜 막히나"여야 함). 단 skin은 3D push 대신 축거리 `reach - skinWidth` 로 반영해야 비교가 안정적.
→ 권장: ① sign 비교는 side-effect 없는 reach만, ② 평행 probe(E-1)로 좌우 대칭 보장, ③ `ReachRatio = reach/desired` + 시간·거리 hysteresis(예: 0.3m·0.15s)로 flip 억제.

## E-5. 🟡 소항목 (CODEX 추가, 채택)
- **skin push 대안:** 축 유지가 목적이면 `bestProjLen -= skinWidth` 가 normal push보다 예측가능(§A-5.2 보완).
- **오타** `ShpereCast`(XML 주석 `:71,74` 등) → `SphereCast`.
- **`IsCameraPositionClear`** 가 후보 위치 자체의 `CheckSphere` overlap 미검사(중심선+ring line만) → 라인이 안 걸치는 매립은 통과 가능.
- **`dropThresholdM`**(floor rescue) 입력 음수 가드 없음 → 음수면 거의 항상 발동.
- **`probeCount`** 미clamp(≥1) → 0이면 함수명과 달리 center-only.

## E-6. CODEX의 조건부/미수용에 대한 본 문서 입장
- **`ContainsFrustum` near/far clip**(CODEX 조건부): §A-4(5) 유지 — 화면방향 판정이면 무해, **락온 가시성 필터로 쓰면** near/far+bounds 필요. CODEX와 동일 결론.
- **`SRGameManager.Instance` null 접근**(CODEX #8): `IsValidActorController` 가 Instance/ActorController 비null을 보장하는지 미공개 → 단정 보류. 단 디졸브 분리(§B-1) 시 의존 자체가 소멸.

## E-7. 우선순위 갱신 반영
기존 §C·§D-5에 더해: **E-2 floor 임계 ground 기준화**, **E-3 normal alignment hit 필터**, **E-1 평행 probe 전환**(E-4 락온 sign 안정화와 동반), **E-4 sign hysteresis**. 특히 E-1↔E-4는 한 수정(평행 probe)으로 충돌 정확도와 락온 흔들림을 동시 개선.

---

## 부록. 검토 범위

| 파일 | 검토 | 비고 |
|---|---|---|
| `GameCameraCalculator.cs` | 전체 512줄 구문별 | §A·B·C |
| `SRControllableCameraBase.cs` | 충돌 통합·블렌딩·회전·정렬 부분 | §D-1~3 (전체 1000줄+ 중 해당 부) |
| `ZoomCollisionState.cs` | 구조체 전체 | §D-2 |
| `LockOnState.cs` | 구조체 전체 | §D-4 (락온 로직은 미포함) |
