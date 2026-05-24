# 오프스크린 적 공격 인디케이터 설계

> 작성일: 2026-05-24  
> 대상 버전: Unity 6 (6000.0.60f1), URP  
> 상태: 1차(MVP) 코드 작성 완료 / 에디터 와이어링 대기 (2026-05-24)

---

## 개요

플레이어를 인식한 적이 카메라 화면 밖에 있으면서 플레이어를 공격하려 할 때, 화면 가장자리에 적의 방향을 가리키는 HUD 인디케이터를 표시하는 시스템 설계서. 명조(Kuro Games)·원신(호요버스)에서 볼 수 있는 "어그로된 오프스크린 적 → 방향 화살표" 패턴을 이 프로젝트의 매니저/상태머신/컴포넌트 구조에 맞춰 적용한다. 목적은 화면 밖 위협의 가시성을 높여 플레이어가 회피·가드·시점 전환을 판단할 수 있게 하는 것.

---

## 레퍼런스 분석

### 게임 사례

**원신(Genshin Impact, 호요버스)**
- 적이 공격 상태(Aggravation/Aggro)이면서 화면 밖에 있을 때만 플레이어 주변에 붉은 화살표를 표시한다.
- 숨거나 비전투 상태인 적은 표시하지 않는다.
- 감지(Detection) 시스템과 공격(Aggression) 시스템의 이원 구조와 연계된다.

**명조(Wuthering Waves, Kuro Games)**
- 동일 계열의 어그로 + 오프스크린 + 방향 화살표 패턴을 공유하는 것으로 추정된다.
- 공식 문서상 구체적 명칭/스펙은 공개 정보가 제한적이다.

### 기술 일반론

**오프스크린 인디케이터 수학**  
월드→스크린 변환 → 화면 밖 판정 → 가장자리 클램핑 → `atan2`로 방향각 회전이 표준이다. 클램핑 형태는 두 갈래가 있다 — (a) 화면 사각형 경계 교점, (b) 화면 중앙 기준 원형 테두리(방사형). 명조/원신 식 방사형 인디케이터에 가까운 **(b) 원형 테두리**를 이 프로젝트의 최종 방식으로 채택한다(아래 "원형 테두리 배치 수학" 참조).

**공격 텔레그래프와 반응시간**  
공격은 예비(Anticipation, 약 0.25~1.0초) → 공격(Attack) → 회복(Recovery) 3단계로 구성된다. 예비 단계가 플레이어 반응 시간을 제공한다. (출처: Game Developer, GDKeys 등 — 하단 출처 참조.)

**접근성**  
색상만으로 상태를 구분하면 색맹 사용자가 구분 불가하다. 색 + 형태 + 심볼 등 "이중 부호화(Double Coding)" 권장.

---

## 트리거 조건

인디케이터 표시는 두 조건면을 모두 만족해야 한다. 긴급도에 따라 등급을 구분한다.

### 필수 조건

| 조건면 | 상세 |
|------|------|
| **인식** | 적이 플레이어를 타겟으로 잡고 있어야 한다. `MonsterActor.Detection.HasTarget == true` 이고 `EnemyDetection.CurrentTarget == GameObjectManager.Player의 transform`. |
| **오프스크린** | 적이 카메라 화면 밖에 있어야 한다. `CameraManager.GetMainCamera()`의 `WorldToViewportPoint(적 위치)` 결과로 판정한다. |

### 오프스크린 판정 세부

| 판정 | 설명 |
|------|------|
| **카메라 뒤** | `viewportPos.z <= 0` 이면 화면 밖(필수 가드 — z<0에서는 x/y가 반전되어 정상값처럼 보이므로 반드시 먼저 체크). |
| **화면 경계 밖** | `viewportPos.x` 또는 `y`가 [0,1] 범위 밖. |
| **1차 구현 범위** | 카메라 프러스텀 밖만 처리한다. 프러스텀 안이지만 지형/오브젝트에 가려진(occluded) 적은 future work로 분리. |

### 등급 분기

| 등급 | 조건 | 비주얼(방향) | 비고 |
|------|------|------|------|
| **공격 임박** (주 트리거) | 위 필수 조건 + `MonsterActor.MovementController.CurrentState.StateName == "Attack"` | 붉은색 + 펄스 애니메이션 + 큰 크기 (+ 옵션 경고 SFX) | 강조됨. 주 기능. |
| **인식만** (약한 표시) | 위 필수 조건 충족, 공격 상태 아님 | 주황/회색, 작게, 펄스 없음 | 보조 표시. Config로 비활성화 가능. |

> 과제의 핵심 요구는 "공격하려 함"이므로 **공격 임박 등급이 주 기능**이다.

---

## 데이터 소스 및 캐싱 전략

### 적 목록 수집

- 적 목록: `GameObjectManager.Instance.AllActors`(IReadOnlyList<GameActor>) 중 `MonsterActor`만 필터한다.
- 매 프레임 전체 순회를 피하기 위해 `OnActorRegistered`/`OnActorUnregistered` 이벤트를 구독해 `_trackedMonsters` 캐시를 유지한다.
- 패턴: 미니맵 시스템과 동일한 이벤트 기반 캐시 방식.

### 갱신 주기

`LateUpdate`에서 캐시된 적을 폴링한다(미니맵의 LateUpdate 갱신 패턴과 일관).

### 플레이어 참조

- `GameObjectManager.Instance.Player`
- `PartyManager.OnSwapCompleted`를 구독해 활성 캐릭터(플레이어) 변경 시 참조 갱신.

---

## 원형 테두리(Ring) 배치 수학

마커는 화면 사각형 가장자리가 아니라, **화면 중앙 기준 가상 원형 테두리** 위에 놓는다(명조/원신 식 방사형 인디케이터). 구현은 카메라 투영을 후보당 1회(`WorldToViewportPoint`)만 사용한다.

### 개념

1. **뷰포트 1회 투영**: `WorldToViewportPoint`로 적의 뷰포트 좌표를 구한다. `z <= 0`이면 카메라 뒤로 판정한다(가드 먼저).
2. **방향 벡터**: 뷰포트 델타 `(vx-0.5, vy-0.5)`를 화면 비율로 환산(`*Screen.width`, `*Screen.height`)해 화면상의 실제 방향을 얻는다. 카메라 뒤면 부호를 반전한다.
3. **각도**: `angle = atan2(dy, dx)`.
4. **링 위 좌표**: 마커 컨테이너 로컬 좌표 = `(cos angle, sin angle) * ringRadius`. 사각형 교점 계산이 필요 없다.
5. **회전각**: 화살표 회전 `Z = angle*Rad2Deg + markerForwardAngleOffset`로 적 방향을 가리킨다. 보정각은 스프라이트 기본 향함에 맞춘다 — 오른쪽(+X) 향함이면 `0`, 위(+Y) 향함이면 `-90`.

### 성능(핫패스) 설계

`LateUpdate`가 매 프레임 돌므로 후보당 비용을 최소화한다.

- **카메라 투영 1회**: 사각형 클램프 방식이 쓰던 두 번째 투영(`WorldToScreenPoint`)과 `RectTransformUtility.ScreenPointToLocalPointInRectangle` 호출을 제거. 오프스크린 판정에 쓴 `WorldToViewportPoint` 결과를 방향 계산에 재사용한다.
- **싼 필터 우선**: 인식 여부(`HasTarget`/`CurrentTarget`) → 거리 `sqrMagnitude` → 카메라 투영 → 상태명 조회 순. 투영은 "플레이어를 타겟으로 잡은 + 거리 내" 후보에만 수행한다.
- **루프 밖 호이스팅**: 설정값(색/스케일/펄스/ringRadius), 플레이어 위치, `Screen.width/height`를 루프 전 지역 변수로 1회만 읽는다.
- **무할당**: 루프 내 `new Vector2/3`는 구조체라 GC 없음. 마커는 풀링.

---

## 비주얼 및 접근성

### 이중 부호화

색상(공격 임박=붉은, 인식=주황)만으로 구분하지 않고 **화살표 방향 + 크기 + 펄스 유무**를 함께 사용한다(색맹 대비 이중 부호화). 1차 구현에서는 별도 "!" 심볼 없이 화살표 단독으로 두되, 공격 임박은 큰 크기 + 펄스로 구분한다. 옵션 경고 SFX는 future work.

### 외부화

거리에 따른 크기/투명도 페이드, 펄스 속도 등은 모두 Config 수치(TBD)로 외부화한다.

---

## 다중 적 처리

### 1차 (MVP)

- 거리 임계값(TBD) 초과 적을 제외한다.
- 공격 임박 > 거리 가까움 순 우선순위 정렬.

### Future

같은 화면 가장자리에 N개 이상 몰릴 때 "N+" 배지로 클러스터링한다.

---

## 기존 시스템 통합

### 카메라 모드 게이트

`CameraManager.CurrentCameraMode`가 `InGame`이 아닐 때(대화/킬캠/컷씬 등) 인디케이터를 숨긴다.

### 파티 스왑

`PartyManager.OnSwapCompleted`를 구독해 활성 캐릭터(플레이어)가 바뀌면 비교 대상 player 참조를 갱신한다. (`UI_HudPlayerInfo`가 같은 이벤트를 구독하는 패턴 참조).

### UI 생명주기

`UI_Base`의 `OnShow`에서 이벤트 구독·캐시 초기화, `OnHide`에서 해제한다(기존 HUD UI 패턴과 동일).

---

## 신규/기존 파일 및 식별자

| 파일/식별자 | 역할 | 신규/기존 |
|------|------|------|
| `Assets/02.Scripts/UI/HUD/UI_HudOffscreenThreatIndicator.cs` | HUD 인디케이터 메인. `UI_Base` 상속, HUD 캔버스 레이어 | 신규 |
| `Assets/02.Scripts/UI/HUD/UIOffscreenThreatMarker.cs` | 개별 화살표 마커 컴포넌트(풀링) | 신규 |
| `Assets/02.Scripts/Data/UI/OffscreenThreatConfigSO.cs` | 색 팔레트·거리 임계값·링 반경·펄스 등 수치 외부화 SO | 신규 |
| `Assets/03.Prefabs/UI/` | 인디케이터 프리팹 배치 위치 | 신규 |
| `Assets/10.Datas/UI/` | Config 에셋 배치 위치 | 신규 |
| `GameObjectManager.AllActors` / `Player` / `OnActorRegistered` / `OnActorUnregistered` | 적 후보 수집·플레이어 참조 | 기존 |
| `MonsterActor.Detection` / `MonsterActor.Combat` | 인식 상태·전투 컴포넌트 접근 | 기존 |
| `EnemyDetection.HasTarget` / `CurrentTarget` | 어그로 판정 | 기존 |
| `MovementController.CurrentState.StateName` | 공격 상태 판정("Attack") | 기존 |
| `CameraManager.GetMainCamera()` / `CurrentCameraMode` | 월드→스크린 변환·모드 게이트 | 기존 |
| `PartyManager.OnSwapCompleted` | 활성 캐릭터 변경 반영 | 기존 |
| `UIKeyType` / `UIPrefabDatabase` | UI 키 등록 | 기존 |

---

## 신규 HUD 등록 절차

1. `UI_HudOffscreenThreatIndicator`를 `UI_Base` 상속으로 작성하고 `_layer`를 HUD 레이어로 설정한다.
2. 프리팹을 `Assets/03.Prefabs/UI/`에 만들고 루트에 Canvas + 컴포넌트를 부착한다.
3. `UIKeyType`은 자동 생성 파일이므로 직접 수정하지 말고 "UPlayGround/ID Enum Generator" 창에서 키를 추가·재생성한다.
4. `UIPrefabDatabase.asset`에 key·prefab·defaultLayer(HUD)를 등록한다.
5. HUD 부팅 흐름(예: `UI_GamePlay`)에서 표시한다.

---

## 구현 로드맵

### 1차 (MVP)

**코드 작성 완료 (2026-05-24)** — 아래 3개 스크립트:
- `Assets/02.Scripts/Data/UI/OffscreenThreatConfigSO.cs`
- `Assets/02.Scripts/UI/HUD/UIOffscreenThreatMarker.cs`
- `Assets/02.Scripts/UI/HUD/UI_HudOffscreenThreatIndicator.cs`

구현 항목:
- [x] 프러스텀 밖 판정(viewport/z, 카메라 뒤 가드)
- [x] 공격 임박 등급(`CurrentState.StateName == "Attack"`) + 인식만 보조 등급
- [x] 가상 원형 테두리(ring) 배치 + atan2 회전 (사각형 클램프에서 변경)
- [x] 마커 풀링
- [x] Config 외부화(`OffscreenThreatConfigSO`)
- [x] 카메라 모드 게이트(`CurrentCameraMode != InGame` 시 숨김)
- [x] 파티 스왑 반영(`PartyManager.OnSwapCompleted`)
- [x] `UIKeyType.OffscreenThreatIndicator` 키 생성(ID Enum Generator) + `UI_GamePlay`에 `ShowUI`/`HideUI` 배선

> 마커는 색맹 대비 "!" 심볼 없이 화살표 단독으로 변경됨(색+형태+방향으로 구분). 클래스명: `UIOffscreenThreatMarker`, `UI_HudOffscreenThreatIndicator`.

**남은 에디터 와이어링 (Unity 내 수동 작업):**
- [ ] `OffscreenThreatConfig` 에셋 생성(메뉴: `UPlayGround/UI/OffscreenThreatConfig`) → `10.Datas/UI/`
- [ ] 마커 프리팹(화살표 Image +Y 향함) + 인디케이터 프리팹(풀스크린 stretch 마커 컨테이너, pivot 0.5) → `03.Prefabs/UI/`
- [ ] `UIPrefabDatabase.asset`에 `OffscreenThreatIndicator` 키·프리팹·HUD 레이어 등록

**Unity 실행 후 튜닝/확인:** 루트 위치 보정(키 큰 적은 발끝 기준이라 머리가 화면 안인데 off-screen 처리될 수 있음 → 필요 시 `+up*height` 보정), 거리/링 반경/펄스 수치(TBD).

### Future

- Occlusion(시야 내 가려짐) 판정
- "인식만" 등급 정교화
- 클러스터링("N+")
- 거리별 크기/투명도 페이드
- 경고 SFX/컨트롤러 진동
- ActorMonitor/디버그 토글

---

## 미해결 의존성

### 텔레그래프 기반 정밀 트리거

현재 `EnemyCombat`에는 텔레그래프 활성 여부를 외부에서 읽는 public 접근자가 없다:
- `IsPossibleCollide` — 충돌 판정 구간 플래그이지만, 텔레그래프 인스턴스 목록은 private.
- `BeginCurrentSkillTelegraph()` — 시작 메서드이며, 진행 중 텔레그래프 상태 조회 불가.

**1차 구현은 `CurrentState.StateName == "Attack"`만 트리거로 사용한다.**

텔레그래프 예비 단계까지 더 이르게 잡고 싶으면 `EnemyCombat`에 `IsTelegraphActive`(예: 텔레그래프 인스턴스 존재 여부) 같은 public getter를 추가하는 작업이 선행되어야 한다. 이를 명시적 선행 의존 작업으로 분리한다.

### 수치 정의

거리 임계값·링 반경·펄스 속도 등 모든 튜닝 수치: TBD.

---

## 출처

- [Game UI Database - Genshin Impact (Console)](https://www.gameuidatabase.com/gameData.php?id=470)
- [Genshin Impact Starter Guide: Introduction and UI Explanation](https://www.hoyolab.com/article/5921298)
- [Positioning On-Screen Indicators to Point to Off-Screen Targets | Envato Tuts+](https://gamedevelopment.tutsplus.com/tutorials/positioning-on-screen-indicators-to-point-to-off-screen-targets--gamedev-6644)
- [GitHub - jinincarnate/off-screen-indicator](https://github.com/jinincarnate/off-screen-indicator)
- [Aggravation | Genshin Impact Wiki | Fandom](https://genshin-impact.fandom.com/wiki/Aggravation)
- [Enemy Attacks and Telegraphing | Game Developer](https://www.gamedeveloper.com/design/enemy-attacks-and-telegraphing)
- [Keys to Combat Design: Anatomy of an Attack - GDKeys](https://gdkeys.com/keys-to-combat-design-1-anatomy-of-an-attack/)
- [Designing for colorblind access. Part 1: UI components | Medium](https://medium.com/queer-design-club/going-beyond-color-9d3830559e10)
- [HUD in Video Games: Meaning, Examples & Design Guide | Sunstrike Studios](https://sunstrikestudios.com/en/blog/HUD_design_in_games/)
