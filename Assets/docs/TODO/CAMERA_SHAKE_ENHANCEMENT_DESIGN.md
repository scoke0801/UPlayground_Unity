# 카메라 쉐이크 고도화 설계

**작성일**: 2026-06-05 | **상태**: 설계 / 미구현 | **레퍼런스**: 명조(Wuthering Waves), Eiserloh GDC 2016, 중국 전투설계 자료

> 상위 카메라 로드맵은 [[CAMERA_ENHANCEMENT_ROADMAP_DESIGN]] 참조. 본 문서는 **쉐이크/펀치 서브시스템**에 한정한 별도 설계서다. 로드맵의 C1(충돌 견고성) 약점과 직접 맞물리는 항목(§4.A)이 있어 교차 참조한다.

---

## 1. 배경 및 목표

전투 타격감(打击感)을 결정하는 가장 직접적인 수단이 화면 진동(震屏)이다. 현재 쉐이크 시스템은 동작하지만 **위치 이동 기반 단일 랜덤 쉐이크**에 머물러 있어, 레퍼런스(명조)와 업계 표준(Eiserloh)이 3D 액션에서 권장하는 **회전 기반 정합 노이즈 쉐이크**와 구조적으로 벌어져 있다.

**목표:** 현재 인프라(`CameraShaker`, `CameraShakeData`, 이펙트 파이프라인)를 유지하면서 ① 회전 쉐이크, ② 정합 노이즈, ③ Trauma 누적 모델로 전환해 명조 수준의 타격 리듬·방향 매칭·히트스톱 결합을 확보한다. 동시에 멀미 유발을 회피한다.

---

## 2. 현재 구조 분석

### 2.1 데이터 흐름

```
MotionEvent_CameraEffect ─┐
CombatCameraDirector ─────┼─→ CameraManager.StartShake / Punch
CombatFeedbackDispatcher ─┘         │
                                    ▼
                            CameraShaker (단일 인스턴스, CameraManager 자식)
                            ├─ Shake : 랜덤 진동 (위치)
                            └─ Punch : 방향성 임펄스 (위치)
                                    │ Pre/PostRender 콜백
                                    ▼
                            cam.transform.localPosition += shake + punch
```

### 2.2 구성 요소

| 요소 | 파일 | 역할 |
|---|---|---|
| `CameraShaker` | `Camera/CameraShaker.cs` | 코어. Shake + Punch 2종, Pre/PostRender 위치 조작 |
| `CameraShakeData` (SO) | `Data/Camera/CameraShakeData.cs` | Duration/Delay/AmplitudeX·Y/Frequency/Dampening/ShakeSpace |
| `ShakeCameraEffect` | `Camera/Effects/ShakeCameraEffect.cs` | 이펙트 수명주기 래퍼 (실작업은 CameraShaker에 위임) |
| `CombatCameraDirector` | `Camera/Combat/CombatCameraDirector.cs` | 공격종류별 shakeKey/punch 강도·지속 결정 |
| `CameraShakeDatabase` | `Data/Path/CameraShakeDatabase.cs` | 키→SO 조회 |

### 2.3 동작 핵심 (코드 기준)

- **Shake 벡터**: `Random.value`로 매 `ShakesDelay`(=1/Frequency) 간격마다 새 랜덤 벡터를 생성하고 `ShakeCurve`로 감쇠 (`CameraShaker.cs:198-202`).
- **적용**: `onPreRenderCamera`에서 `cam.transform.localPosition += shake + punch`, PostRender에서 원위치 복원 (`CameraShaker.cs:221-232`).
- **Punch만 방향성**: `HitDirection`을 카메라 local XY로 투영 후 감쇠 (`CameraShaker.cs:339`, decay 기본 `1 - t²`).
- **교체 모델**: `StartShake`가 진행 중 쉐이크를 `StopShake`로 끊고 재시작 (`CameraShaker.cs:71`).
- **단일 인스턴스**: `CameraManager._shaker` 하나, `ManualUpdate(Time.deltaTime)`로 수동 틱 (`CameraManager.cs:196`).
- **세팅 스케일**: `SettingsCombatCameraShakeScale` × 프로필 `cameraShakeScale`로 강도 스케일링 (`CombatCameraDirector.cs:393-400`) — 접근성 슬라이더 일부 존재.

### 2.4 강점

- 이펙트 수명주기(BlendIn/Out, Priority)와 분리되어 SO 데이터 주도로 키 조회 가능.
- 에디터 프리뷰(`Animate`) 지원, URP/레거시 양쪽 렌더 콜백 대응.
- 세팅 스케일·프로필 스케일로 강도 외부 조정 가능.
- Punch는 이미 방향성을 가짐 (방향 매칭의 토대 존재).

---

## 3. 레퍼런스 결론 (웹 조사)

명조 사례 + Eiserloh GDC 2016 + 중국 전투설계 자료의 합의:

| 원칙 | 결론 | 출처 |
|---|---|---|
| **축** | 3D는 **위치 이동이 아닌 회전(Pitch·Yaw)** 쉐이크. 위치 이동은 카메라가 벽·지형을 뚫음. **Roll은 멀미 → 회피** | Eiserloh, 机核 |
| **노이즈** | `Random`이 아닌 **Perlin/coherent noise** — 부드럽고 연속적, 계단식 스냅·멀미 방지 | Eiserloh, Borderline |
| **방향 매칭** | 명조 양양(秧秧) 세검: **내려치기 동작 = 상하 Pitch 진동 매칭** | 机核 |
| **리듬** | 명조 6단 평타: **막타 강타에만 큰 진동값**. "모든 히트 동급 = 메트로놈처럼 단조" | 机核 |
| **진폭 절제** | 명조도 과하면 "오래 보면 멀미". 흑신화 오공은 과회전 시 **추적 포기 안전밸브** | 机核 |
| **히트스톱 결합** | 顿帧 0.05~0.08초 + 쉐이크 동시 작동 = 卡肉感(살에 박히는 느낌) 핵심 | 机核 |

---

## 4. 약점 진단 (레퍼런스 대비)

| # | 약점 | 코드 근거 | 레퍼런스 갭 |
|---|---|---|---|
| W1 | **회전 쉐이크 부재** — 전부 위치 이동 | `CameraShaker.cs:226` | 명조/Eiserloh 1순위. 로드맵 C1(충돌)을 **위치 쉐이크가 악화** |
| W2 | **`Random.value` 비정합 노이즈**, ShakesDelay 스냅 | `CameraShaker.cs:198` | Perlin 미사용 → 계단식·멀미 |
| W3 | **Trauma 누적 없음** — StartShake가 교체 | `CameraShaker.cs:71` | 다단히트 겹침 시 리셋, 막타 버스트 불가 |
| W4 | **단일 쉐이커 — 레이어링 불가** | `CameraManager.cs:33` | 지속 럼블 + 순간 히트 동시 불가 |
| W5 | **Shake 자체 방향성 없음(대칭 랜덤)** | `CameraShaker.cs:198` | HitDirection이 Punch에만 쓰임 → 양양식 매칭 불가 |
| W6 | **카덴스 제어 없음** (Light/Heavy 2단뿐) | `CombatCameraDirector.cs:175-198` | "메트로놈" 리스크 |
| W7 | **히트스톱 중 쉐이크 정지** | `CameraShaker.cs:219` (`timeScale<=0` 가드) | 卡肉感 핵심 타이밍 손실 |
| W8 | **이펙트 파이프라인 우회** (Apply 비어있음) | `ShakeCameraEffect.cs:42` | CameraEffectState 합성·우선순위 미참여 |
| W9 | **거리 감쇠 없음** | — | 원거리 폭발도 동일 강도 |

---

## 5. 개선 방안 (Tier)

### Tier 1 — 체감 즉효 · 저위험

- **A. 회전 쉐이크 도입 ★** (W1, W9, 로드맵 C1)
  - `CameraShakeData`에 `PitchAmplitude` / `YawAmplitude`(도 단위), `RollAmplitude`(기본 0, 옵션) 추가.
  - 적용을 `localPosition` 오프셋 → `localRotation`(또는 회전 오프셋 쿼터니언) 합성으로 전환. 위치 쉐이크는 폭발 등 한정 옵션으로 강등(`PositionAmplitude` 보존).
  - `ShakeSpace`는 회전에선 무의미하므로 회전 모드에서 비활성.
- **B. Perlin 노이즈 교체 ★** (W2)
  - `Random.value` → `Mathf.PerlinNoise(seedₐ, t·Frequency)`를 축별 독립 시드(Pitch/Yaw/Roll)로. `[-1,1]` 정규화.
  - ShakesDelay 데시메이션 제거 — Perlin 자체가 연속적이라 불필요.
- **C. 방향 매칭** (W5)
  - `intent.HitDirection`을 Pitch/Yaw 바이어스로 변환: 수직 성분→Pitch 부호, 수평 성분→Yaw 부호. 노이즈에 방향 오프셋을 더해 첫 임펄스가 타격 궤적과 일치.

### Tier 2 — 시스템 모델

- **D. Trauma 모델 ★** (W3)
  - `AddTrauma(float amount)`: trauma 0~1 누적(가산, clamp01).
  - `shake = trauma² × maxAngle` (비선형, Eiserloh), 초당 `traumaDecay` 감쇠.
  - `StartShake`를 리셋이 아닌 **AddTrauma 호출**로 변경 → 다단히트가 자연 누적, 막타에서 trauma 포화 = 명조식 버스트.
- **E. 채널 레이어링** (W4)
  - `s_Shakers` 정적 리스트는 이미 다중 지원. 매니저가 trauma를 채널(Hit / Rumble / Explosion)별로 합산해 최종 회전 오프셋 = Σ채널.
- **F. 히트스톱 호환 ★** (W7)
  - `timeScale<=0` 가드(`CameraShaker.cs:219`) 제거, 틱을 `unscaledDeltaTime` 기반으로. 顿帧 구간에도 쉐이크 지속 → 卡肉感.

### Tier 3 — 연출 디테일

- **G. 카덴스** (W6): `CombatCameraDirector`에 콤보 인덱스/막타 플래그 주입 → trauma amount 동적 스케일(중간타 약, 막타 강).
- **H. 거리 감쇠** (W9): 임팩트 월드 위치와 카메라 거리로 trauma 감쇠(폭발 한정).
- **I. 멀미 세이프밸브**: trauma 상한 클램프 + 흑신화식 과회전 컷. `SettingsCombatCameraShakeScale` 슬라이더 연동(인프라 존재).

---

## 6. 아키텍처 변경점

| 항목 | 변경 | 영향 파일 |
|---|---|---|
| 데이터 | 회전 진폭 3축 + PositionAmplitude 분리, traumaDecay, 노이즈 시드 | `CameraShakeData.cs` |
| 코어 | Perlin 노이즈, 회전 오프셋 적용, Trauma 누적 API, unscaled 틱 | `CameraShaker.cs` |
| 합성 | (선택) `ShakeCameraEffect.Apply`를 Rotation 채널 델타 기여로 전환, `AffectedChannels`에 Rotation 추가 | `ShakeCameraEffect.cs` |
| 디렉터 | HitDirection→축 바이어스, 콤보 카덴스 amount | `CombatCameraDirector.cs` |
| 매니저 | StartShake→AddTrauma 경로, 채널 합산 | `CameraManager.cs` |

**호환성:** 기존 `CameraShakeData` 에셋은 `PitchAmplitude=0`이면 위치 쉐이크 폴백으로 유지 가능. 마이그레이션은 AmplitudeX/Y→Yaw/Pitch 자동 변환 유틸로 일괄 처리.

---

## 7. 핵심 결론

가장 임팩트 큰 단일 변경은 **위치 쉐이크 → Pitch/Yaw 회전 쉐이크 전환(§5.A)** 이다. 명조·Eiserloh·전투설계 자료 셋 다 3D에서 이를 1순위로 꼽으며, 동시에 로드맵의 카메라 충돌(C1) 약점까지 함께 해소된다. B(Perlin)·F(히트스톱 호환)가 멀미 회피와 卡肉感의 양 축을 담당한다. **권장 착수 순서: A → B → F → C → D → (E·G·H·I).**

---

## 8. 출처

- Eiserloh, "Math for Game Programmers: Juicing Your Cameras with Math", GDC 2016 — `mathforgameprogrammers.com/gdc2016/GDC2016_Eiserloh_Squirrel_JuicingYourCameras.pdf`
- 浅谈游戏战斗设计——战斗表现, 机核 GCORES — `gcores.com/articles/202329`
- 游戏的打击感从何而来？, 知乎 — `zhihu.com/question/285096068`
- Trauma-based screenshake, Borderline Blog — `blog.borderline.games/tutorials/gettinghit!/trauma-based-screenshake.html`
