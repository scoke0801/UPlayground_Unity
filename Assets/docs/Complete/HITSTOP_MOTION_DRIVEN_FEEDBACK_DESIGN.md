# 모션 후딜레이/Hit 판정 기반 HitStop 자동 설계

## 1. 목표

> 긴 모션(= 강한 공격)은 한 타격의 공격 수치와 HitStop을 강하게,
> 짧은 모션(= 가벼운 공격)은 공격 수치와 HitStop 등 피드백을 약하게.

이 규칙을 **MotionSet의 객관적 타이밍 데이터(후딜레이 + Hit 판정 창)** 에서 자동으로 산출하도록
공격 데이터 생성기를 확장한다. 디자이너가 클립마다 HitStop 수치를 손으로 적는 대신,
모션을 만들면 피드백 강도가 따라오게 한다.

## 2. 현재 상태 진단 (이미 있는 것 / 빠진 것)

대부분의 인프라가 **이미 존재**한다. 새 시스템이 아니라 "이미 추출된 타이밍을 피드백 식에 연결"하는 작업이다.

| 요소 | 위치 | 상태 |
|------|------|------|
| HitStop 실행 | `GameHitStopHandler` (강도/스케일/Actor-local) | ✅ 완비 |
| 적중 시 HitStop 디스패치 | `CombatFeedbackDispatcher` → `reactionData.hitStopDuration/Scale` | ✅ 완비 |
| 모션 타이밍 추출 | `AttackDataFromMotionSetWindow.ScanEntry` (`GetPhaseActiveStart/End`, `Duration`) | ✅ 완비 |
| 피드백 자동 생성 | `ApplyAutoReaction()` → `hitStopDuration/Scale`, camera, FOV, trail | ✅ 동작하나 **카테고리 기반** |
| 후딜레이 분석값 | `analysis.recoveryDuration` (`CombatData.cs`) | ⚠️ **산출만 하고 미사용** |

### 핵심 갭

`ApplyAutoReaction`의 `impactScore`(`AttackDataFromMotionSetWindow.cs:1033`)는
사실상 **AttackCategory 룩업 4종**(weaponSpeed/rootMotion/bodyRotation/attackWeight)으로만 결정된다.
모션 길이는 `rootMotionScore` 안의 약한 항(가중치 0.25) 하나로만 새어 들어간다.

→ 즉 **"긴 모션 = 강한 HitStop"이 데이터가 아니라 카테고리 프리셋으로 결정**되고 있다.
같은 `Heavy` 카테고리면 클립이 길든 짧든 HitStop이 거의 같다. 사용자가 원하는 동작이 아니다.

그리고 후딜레이(`recoveryDuration`)는 이미 계산되어 `analysis`에 저장되지만 **피드백 식에 전혀 안 들어간다.**
필요한 신호가 이미 손에 있는데 안 쓰고 있는 상태.

## 3. 설계 방향

### 3.1 후딜레이를 1차 강도 축으로 (Hit 판정은 "타이밍" 축)

- **강도 = 후딜레이(endlag) 주도.** 후딜이 길다 = "이 공격에 크게 커밋했다" = 보상으로 강한 피드백.
  이것이 액션 게임에서 공격의 무게를 나타내는 정석 신호이고, 사용자가 명시한 "후딜레이" 그 자체다.
- **총 모션 길이(`Duration`)는 주축으로 쓰지 않는다.** 느린 선딜(startup)이 길이를 부풀려도
  커밋과는 무관하기 때문에 노이즈가 섞인다. 후딜을 주, 총 길이는 보조로만.
- **"Hit 판정"은 강도 2차 축이 아니라 타이밍 앵커.** `impactTime = activeStart`로 이미 설정됨(`:1057`).
  HitStop이 "언제" 터질지를 Hit 판정 창이 정한다. 강도는 후딜이 정한다.
- **카테고리는 보조 배율로 강등.** 이미 있는 `GetReactionTypeMultiplier`(`:1134`)를 그대로 활용해
  Light 0.8 ~ SwapSpecial 1.6 곱으로만 색을 입힌다. 주도권은 타이밍 데이터로 넘긴다.

### 3.2 ⚠️ 멀티히트 per-phase 후딜레이 계산 — 현재 필드 그대로 쓰면 의도가 뒤집힌다

현재 `recoveryDuration = Duration - activeEnd` (`:1027`)는 **멀티히트에서 잘못된 신호**다.

예) 3타 콤보 클립:
- phase 0은 일찍 끝남 → `Duration - activeEnd`가 **큼**
- 마지막 커밋 타격은 늦게 끝남 → `Duration - activeEnd`가 **작음**

→ 이대로 HitStop에 먹이면 **첫 잽이 가장 강하게 멈추고, 커밋한 마무리 타격이 가장 약해진다.**
"긴 모션 = 강한 타격"의 정반대.

**올바른 per-phase 신호 = "다음 타격까지의 공백(gap)".**

```
endlagᵢ = (i가 마지막 phase) ? (Duration - activeEndᵢ)
                            : (GetPhaseActiveStart(i+1) - activeEndᵢ)
```

- 연타 중간 타격은 다음 타격이 곧 이어지므로 gap이 작다 → 약한 HitStop (연타 리듬 유지).
- 콤보 마무리 타격은 뒤가 비어 있으므로 gap이 크다 → 강한 HitStop.
- 단타 무거운 공격은 그 자체로 후딜이 길다 → 강한 HitStop.

이게 정확히 원하는 손맛이다. `ScanEntry`에 이미 `GetPhaseActiveStart(i)`가 있으므로 `i+1`만 조회하면 된다.

### 3.3 motionWeight 스칼라 정의

```
endlagNorm = InverseLerp(SHORT_ENDLAG, LONG_ENDLAG, endlagᵢ)   // 예: 0.08s ~ 0.5s
activeNorm = InverseLerp(...)(선택, 보조)                         // 긴 타격 판정창 = 묵직함, 약가중
motionWeight = Clamp01( endlagNorm * 0.8 + activeNorm * 0.2 )
finalWeight  = Clamp01( motionWeight ) * GetReactionTypeMultiplier(category)
```

`SHORT_ENDLAG`/`LONG_ENDLAG`는 생성기 창에 슬라이더로 노출(현재 `_motionDurationWeight` 옆).

### 3.4 finalWeight → 피드백 매핑 (기존 Lerp 식 재사용, impactScore만 교체)

`ApplyAutoReaction`(`:1058~1066`)의 Lerp 구조를 그대로 두고 입력만 `impactScore` → `finalWeight`로 바꾼다.

| 출력 | 짧은 모션(impact 0) | 긴 모션(impact 1) |
|------|------|------|
| `hitStopDuration` (×typeMultiplier, cap 0.20) | ~0.03s | ~0.12s |
| `hitStopScale` = **공격자** 스케일(낮을수록 강함) | 0.15 | 0.0 |
| `cameraShakeAmplitude` | 0.15 | 0.8 |
| `fovKickAmount` | 0.5 | 3.5 |
| `trailIntensity` | 0.4 | 1.2 |

→ 자연히 "짧은 모션 약한 피드백 / 긴 모션 강한 피드백" 곡선이 나온다.

### 3.5 공격자/피격자 비대칭 (런타임) + 튜닝 시작값

AAA 액션은 ① 타격감 ② 조작감 ③ 애니메이션 연속성을 동시에 만족시켜야 하며, 셋이 충돌하므로
**공격자·피격자를 같은 값으로 멈추는 게 항상 정답이 아니다.** 핵심 원리:

- **피격자 = 항상 풀프리즈(0.0)**, **공격자 = 약하게(0.15→0.0)**. 사람 눈은 0%↔10% 차이를 잘 못 느끼지만
  공격자 쪽 루트모션/카메라가 미세하게 진행되어 조작감·연속성이 끊기지 않는다(DMC 계열 구조).
- **체감은 scale보다 duration이 더 좌우한다.** "0.1이냐 0.2냐"보다 "0.04초냐 0.08초냐"가 타격감 차이를 만든다.
  → duration 범위를 넓게(0.03~0.20s ≈ 2~12F @60fps), 공격자 scale은 좁게 잡았다.

구현: `GameHitStopHandler.ExecuteLocalImpact`에 `victimTimeScale`(기본 -1=대칭, 하위호환) 추가.
`CombatFeedbackDispatcher.ApplyPlayerAttackLocalHitStop`이 `victimTimeScale: 0`(풀프리즈)을 넘겨
**플레이어 공격 적중**에서만 피격자(적)는 풀프리즈, 공격자(플레이어)는 reactionData의 약한 스케일로 멈춘다.
피격(incoming)/스페셜브레이크 경로는 기존 대칭 유지(플레이어가 피격 시 풀프리즈되면 조작감 손실).

권장 시작값(명조+DMC 계열 3D 액션, 디자이너가 슬라이더·manualOverride로 재튜닝):

| 상황 | 공격자 scale | 피격자 scale | duration |
|------|------|------|------|
| Light | 0.15 | 0.0 | 0.03~0.04 |
| Heavy | ~0.05 | 0.0 | 0.05~0.08 |
| Counter/Skill | ~0.0 | 0.0 | 0.08~0.13 |
| Break/Ultimate | 0.0 | 0.0 | 0.12~0.20 |

> 웹 corroboration: Guilty Gear Xrd 라이트 7F/헤비 10F, Smash hitlag(공격자=타격프레임/피격자=플린치 첫프레임 동결),
> DMC hitstop = Start time + Duration. 2D 격투는 더 길고, 3D 액션은 위 표처럼 짧은 편이 적합.

## 4. 손대지 않을 것 (스코프 경계)

- **데미지/밸런스 식은 건드리지 않는다.** `CalculateTotalDamage`/`durationMultiplier`/DPS 정규화/
  Poise·Break 게이지 분수는 의도적으로 튜닝된 시스템이다(메모리: DPS 자기정규화, Poise statData 단일소스).
  사용자 요청은 **HitStop/피드백**이고, "한 타격의 공격 수치" 분배는 이미 DPS 정규화 +
  `multiHitCompensation`이 처리한다. motionWeight를 데미지에 합치면 잡아둔 밸런스가 흔들린다.
- 변경은 **`ApplyAutoReaction` 내부(피드백/리액션 측)** 로 한정한다.
- **결정 확정(사용자):** per-hit 데미지는 endlag로 재키잉하지 않고 기존 밸런스 시스템(총 클립 길이
  `durationMultiplier` + 콤보 램프 `Lerp(1,1.25)` + DPS 정규화) 유지. 피드백 강도 축(endlag)과
  데미지 축(총 Duration)이 다르지만, 데미지도 이미 모션 길이에 비례하고 튜닝된 밸런스를 흔들지 않기 위함.

> 참고: 후딜레이는 프로젝트 전역에서 authored 필드가 아니라 collision(Hit 판정) 타이밍으로 산출된다.
> `CombatTimelineUtility.ComputeFrameMetrics`의 `recovery = total - maxEnd` 정의와
> `GetPhaseEndlag`의 마지막 phase 식이 일치하며, 이를 멀티히트 per-phase로 확장한 것이다.

## 5. 작업 항목 (구현 완료)

**A. 모션 후딜 기반 자동 산출 — `AttackDataFromMotionSetWindow.cs`**
1. ✅ `ScanEntry.GetPhaseEndlag(i)` — 마지막 phase = `Duration - activeEnd`,
   중간 phase = `GetPhaseActiveStart(i+1) - activeEnd`(gap≤0이면 0으로 폴백, 의도 역전 방지).
2. ✅ `ApplyAutoReaction` `impactScore`를 후딜 주도로 교체. 카테고리 4종 룩업은 `analysis` 기록용 보존,
   강도는 `GetReactionTypeMultiplier` 보조배율로만 반영. `analysis.recoveryDuration`=per-phase endlag.
3. ✅ 생성기 창에 `_shortEndlag`/`_longEndlag`/`_activeWindowWeight` 슬라이더 +
   스캔 프리뷰에 `후딜 ㎳` / `HS ㎳·xScale`(가장 강한 phase 기준) 컬럼.
4. ✅ **DRY(코드 리뷰):** 메커닉 식을 `ComputePhaseImpactScore`(impactScore) +
   `ResolveAutoHitStop`(duration/scale) 단일 진실 소스로 추출 → 실제 생성과 프리뷰가 절대 어긋나지 않음.
   매직넘버는 `AutoHitStopDuration*`/`AutoHitStopAttackerScale*`/`ActiveWindow*Sec` 상수화.

**B. 공격자/피격자 비대칭 + 튜닝값 (3.5)**
5. ✅ `GameHitStopHandler.ExecuteLocalImpact`에 `victimTimeScale`(기본 -1=대칭, 하위호환) 추가.
6. ✅ `CombatFeedbackDispatcher.ApplyPlayerAttackLocalHitStop`이 `victimTimeScale: 0`(풀프리즈) 전달 —
   플레이어 공격 적중에서 피격자 풀프리즈/공격자 약하게. 피격·스페셜브레이크는 대칭 유지.
7. ✅ duration 범위 0.03~0.20s(cap 0.20), 공격자 scale 0.15→0.0으로 권장 테이블 반영.

**C. 남은 일**
8. ⏳ 기존 에셋 일괄 재생성 시 `_overwriteExistingReaction`/`Replace` 정책으로 재생성(메모리: 재생성 overwrite 필수).

> 데미지/밸런스 식은 무수정. 변경: 생성기 + `GameHitStopHandler` + `CombatFeedbackDispatcher` 3파일.
> Unity 에디터 재컴파일 + 실제 클립 재생성 검증 필요(아래 6장).

## 6. 검증

- 단타 Heavy(긴 후딜) vs 라이트 1타(짧은 후딜): HitStop duration/scale 격차가 카테고리 동일 시에도 벌어지는지.
- 3타 콤보: phase 0·1 약한 freeze, phase 2(마무리) 강한 freeze — 첫 타가 더 강하지 않은지 **반드시 확인**(3.2 함정).
- 연타형 스킬: 중간 타격 gap이 작아 리듬이 끊기지 않는지.
- **비대칭**: 플레이어 라이트 공격 적중 시 적은 완전히 굳고 플레이어 무기/카메라는 미세하게 진행되는지(완전 정지 X).
- **프리뷰 일치**: 생성기 프리뷰의 `HS ㎳·xScale`가 실제 생성된 HitPhase의 `hitStopDuration/Scale`과 동일한지.

## 참고 (외부 레퍼런스)

- Sakurai, "Thinking About Hitstop" — HitStop은 타격감 강조용 연출이며 공격 무게에 비례해야 한다.
- Celia Wagar, "Stunning Detail: Hitstun in Depth" — hitstop(연출)과 hitstun(피격 경직)의 분리, 무게별 차등.
