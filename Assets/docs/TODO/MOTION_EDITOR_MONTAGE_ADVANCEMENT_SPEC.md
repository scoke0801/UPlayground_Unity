# MotionSet 에디터 몽타주급 고도화 설계서

> 작성일: 2026-07-26  
> 대상 버전: Unity 6 (6000.0.60f1), Animancer Pro V8, URP, 싱글플레이  
> 상태: 설계 / 백로그 (미구현)  
> 비교 기준: Unreal Engine 5.8 Animation Montage 공식 문서  
> 선행 문서: `Assets/docs/Complete/MOTION_EDITOR_UITOOLKIT_MIGRATION_SPEC.md`  
> 연관 문서: `Assets/docs/guide/MOTION_EVENT_ROLE_GUIDE.md`, `Assets/docs/Complete/MOTIONSET_ASMDEF_PACKAGE_REFACTOR_PLAN.md`, `Assets/docs/Complete/ACTOR_MOTION_FALLBACK_GUIDE.md`

---

## 1. 개요

현재 MotionSet 시스템은 여러 `AnimationClip`을 순차 재생하고, 병렬 Animancer 레이어와 `[SerializeReference]` 기반 MotionEvent를 같은 시간축에서 저작한다. UI Toolkit 코드 전환도 완료되어 룰러, 모션·타이밍·이벤트·전투 오버레이 트랙, 클립 트림, 이벤트 이동·리사이즈, 레이어 표시·잠금, 프레임 스냅, 드래그 앤 드롭을 지원한다. 다만 선행 전환 스펙에 기록된 조작·프로파일러 수동 검증은 별도 잔여 작업이다.

하지만 에디터 표현력은 아직 **“순차 클립 + 시작/종료 이벤트”** 수준에 머문다. 콘텐츠가 복잡해질수록 다음 문제가 커진다.

1. 클립 교체·트림·속도 변경 시 이벤트의 의미적 타이밍이 보존되지 않는다.
2. 콤보·차지·홀드·루프의 구간을 이름으로 식별하거나 런타임에서 안전하게 전환할 수 없다.
3. 구간 이벤트가 `Execute`/`OnCompleteEvent`만 제공하여 시간에 따른 연속 변화를 표현하기 어렵다.
4. 레이어는 Animancer 인덱스와 고정 가중치 중심이라 재생 채널의 의미와 중단 정책이 드러나지 않는다.
5. 블렌드·동기화·루트 모션·전투 판정의 관계를 한 화면에서 검증하기 어렵다.
6. 단일 이벤트 선택 중심이라 다중 이벤트 패턴을 빠르게 정렬·재사용하기 어렵다.

이 설계는 Unreal Animation Montage를 그대로 복제하지 않는다. Montage가 해결하는 핵심 문제를 분석하고, UPlayground의 `AbilitySetSO → MotionReferenceSO → MotionSetAsset`, Animancer 레이어, KCC, MotionWarp 구조에 맞는 기능만 단계적으로 채택한다.

### 목표

- 클립 변경에도 연출 의도가 유지되는 **의미 기반 시간 링크**를 도입한다.
- 이름 있는 Section과 명시적 전환 API로 콤보·홀드·루프의 **재생 흐름 제어**를 제공한다.
- 이벤트를 `Enter / Tick / Exit` 생명주기와 실행 정확도 정책으로 확장한다.
- 블렌드·레이어·동기화·루트 모션을 타임라인에서 함께 저작·검증한다.
- 다중 선택, 그룹, 프리셋, 관계 시각화로 반복 저작 비용을 줄인다.
- 기존 MotionSet 에셋을 값 손실 없이 점진적으로 마이그레이션한다.

### 비목표

- Unreal AnimGraph, Blueprint, Gameplay Ability System을 복제하지 않는다.
- MotionSet 내부에 조건식·GameplayTag 쿼리·BT 분기를 넣어 범용 비주얼 스크립팅으로 만들지 않는다.
- 공격 수치, `HitPhaseData`, 비용, 쿨다운의 권위를 MotionSet으로 옮기지 않는다. 이 데이터의 단일 소스는 계속 `AbilitySetSO` 계열이다.
- 네트워크 Montage 복제와 네트워크 Root Motion은 싱글플레이 프로젝트 범위 밖이다.
- 같은 재생 채널에서 임의의 클립을 겹쳐 NLE처럼 편집하는 기능은 1차 범위가 아니다. 병렬 표현은 `MotionLayer`를 사용한다.
- `MotionReferenceSO`의 무기별 override와 `ActorAnimationMotionSet` fallback을 대체하는 별도 Child Montage 상속 체계를 즉시 추가하지 않는다.

---

## 2. 현재 구현 감사

### 2.1 데이터 모델

```text
MotionSetAsset
└── MotionSet
    ├── motionSetName
    ├── motions[]                    # Base 순차 시퀀스
    │   └── Motion
    │       ├── motionClip
    │       ├── clipStartTime / clipEndTime
    │       ├── playbackSpeed
    │       └── events[]             # 모션 로컬 시간
    ├── globalEvents[]               # MotionSet 전체 시간
    ├── baseLayerIndex
    └── layers[]                     # 병렬 Animancer 레이어
        └── MotionLayer
            ├── animancerLayerIndex
            ├── avatarMask
            ├── Override / Additive
            ├── weight               # 고정값
            ├── holdLastFrame
            ├── motions[]
            └── globalEvents[]
```

현재 `Motion.Duration`은 트림된 클립 길이를 `playbackSpeed`로 나눈 값이고, `MotionSet.TotalDuration`은 Base와 활성 병렬 레이어 중 가장 긴 길이다. 같은 리스트 안의 모션은 앞 모션의 `Duration`을 누적하여 배치되므로 명시적인 시작 시각, 공백, 동일 채널 오버랩은 없다.

### 2.2 런타임 재생

`ActorAnimator`는 `PlayResolvedMotion`에서 Base 첫 모션과 병렬 레이어를 시작한다. 이후 현재 모션이 끝나면 다음 인덱스로 전환한다. 재생 진입 시 외부에서 받은 `fadeDuration`은 사용하지만 MotionSet 자체는 다음을 소유하지 않는다.

- Asset 기본 Blend In / Blend Out
- 정상 종료와 중단 종료의 서로 다른 블렌드
- 구간별 전환 블렌드
- 자동 Blend Out / 마지막 포즈 유지 정책
- 이름 있는 Section 시작·점프·다음 Section 예약

`LoopEvent`는 일반 MotionEvent 실행기가 아니라 `ActorAnimator`가 별도로 해석한다. 이는 구간 제어가 이벤트 타입 하나에 특수 결합되어 있다는 뜻이다.

### 2.3 이벤트 실행

`MotionEventBase` 공통 필드는 `startTime`, `endTime`, `globalStartTimeOffset`이다.

```text
start 진입  → Execute(target)
활성 유지   → HashSet에 보관하나 사용자 Tick 없음
end 이탈    → OnCompleteEvent(target)
강제 중단   → 활성 이벤트 전체 OnCompleteEvent
```

`MotionEventExecutor`는 프레임 사이에 시작된 이벤트를 범위 검색하여 누락을 막고, `RequiresPostEvaluation` 이벤트는 LateUpdate까지 미룬다. `SlashVFX` 같은 공간 이벤트에는 서브프레임 분율도 전달한다. 따라서 기본적인 프레임 누락 방지와 본 평가 후 실행은 이미 존재하지만 다음 정책은 없다.

- 이벤트별 `Queued / Exact` 정확도 선택
- 활성 구간 매 프레임 `Tick`
- 블렌드 가중치에 따른 발화 임계값
- Section 점프 시 이벤트 정리·재진입 규칙
- 이벤트 순서 충돌의 명시적 우선순위
- 클립 이동·스케일 변화에 대한 링크 방식

### 2.4 에디터

UI Toolkit 타임라인은 다음 기능을 이미 구현했다. 이후 단계에서 재제안하거나 IMGUI로 되돌리지 않는다.

| 영역 | 현재 구현 |
|------|-----------|
| 셸 | `CreateGUI`, 3열 분할, USS 디자인 토큰, 사이드바·인스펙터 폭 저장 |
| 타임라인 | Painter2D 기반 룰러·모션·타이밍·이벤트·오버레이 렌더 |
| 입력 | 커서 스크럽, 클립 트림, 이벤트 이동·시작·끝 리사이즈, 휠 줌·스크롤 |
| 레이어 | Base/병렬 레이어 표시, 추가·삭제·순서 변경, 접기·숨김·잠금 |
| 에셋 입력 | Project의 `AnimationClip` 드래그 앤 드롭 |
| 인스펙터 | `SerializedObject`/`SerializedProperty` 기반 이벤트·모션 편집 |
| 이벤트 추가 | 타입 검색, 카테고리, 최근 사용, 코드 프리셋, 사용자 프리셋 |
| 보조 도구 | 루트 모션, 워프 베이크·타깃, 전투 오버레이, 촬영 연동, Slash VFX 씬 튜닝 |

현재 가장 큰 UX 간극은 다중 선택·마퀴 선택·그룹 이동, 에디터 간 클립보드, 의미 기반 스냅, 관계선, 검증 뱃지, 곡선 트랙, Section 흐름 패널이다.

---

## 3. Unreal Montage 공식 기능 비교

조사 기준은 Epic Games의 Unreal Engine 5.8 공식 문서다.

### 3.1 비교표

| Unreal Montage 개념 | Unreal 동작 | 현재 MotionSet | 판정 |
|---------------------|-------------|----------------|------|
| Sequence Segment | 한 Slot에 여러 Animation Sequence를 순차 배치 | `Motion.motions[]`, 트림·속도 지원 | 기본 대응 |
| Montage Section | 이름 있는 구간. 시작 Section 선택, 점프, 다음 Section 예약, 루프 순서 구성 | `LoopEvent` 특수 처리 외 이름 있는 흐름 단위 없음 | **핵심 도입** |
| Slot | AnimGraph의 의미 있는 삽입 지점. 상·하체 등 일부 본에 재생 | Animancer layer index + `AvatarMask` | 부분 대응 |
| Slot Group | 같은 그룹 Montage 간 상호 배제·중단 | 명시적 재생 그룹 없음 | 축소 도입 |
| Notify | 특정 시점 1회 이벤트 | `MotionEventBase.Execute` | 대응 |
| Notify State | Begin / Tick / End 구간 이벤트 | Begin / End만 존재 | **Tick 도입** |
| Montage Notify Window | 재생 호출자에게 Begin / End 신호 반환 | 타입별 직접 실행 중심 | 외부 신호 계약 도입 |
| Notify Link Method | Absolute / Relative / Proportional로 Segment 편집을 추종 | 모션 로컬 초 또는 글로벌 초 고정 | **핵심 도입** |
| Notify Trigger Weight | 블렌드 가중치 임계값 이상에서 발화 | 없음 | 선택 도입 |
| Notify Tick Type | Queued 또는 정확한 Branching Point | 즉시 실행 + 일부 LateUpdate 지연 | 이름·계약 정리 |
| Timing Track | Section/Notify의 실행 순서를 번호와 색으로 표시 | 전환점·오버레이는 있으나 통합 실행 순서 없음 | 도입 |
| Blend In/Out | Asset 진입·종료 블렌드, Blend Profile, 중단 처리 | 호출자 `fadeDuration` 중심 | **핵심 도입** |
| Time Stretch Curve | 목표 재생 시간 변화 시 어느 구간을 더 압축할지 곡선으로 지정 | 모션별 균일 `playbackSpeed` | 후순위 도입 |
| Sync Group / Marker | Leader/Follower와 공통 마커로 서로 다른 길이의 모션 동기화 | 병렬 레이어가 동일 글로벌 초를 공유 | 후순위 도입 |
| Root Motion 정책 | Montage/모든 애니메이션 추출 정책, Notify State로 일부 구간 비활성화 | 별도 루트 모션 도구 + MotionWarp | 프로젝트식 확장 |
| Child Montage | 부모 구조를 읽기 전용 상속하고 Segment 클립만 교체 | MotionReference override와 Actor fallback이 다른 레벨에서 재사용 처리 | 즉시 도입하지 않음 |
| 재생 콜백 | Completed / Blend Out / Interrupted / Notify Begin / Notify End | `OnMotionSetCompleted`, `OnMotionSetEnded(bool)` 중심 | 종료 사유 명시화 |

### 3.2 채택할 핵심

#### Section

Unreal Montage Section은 타임라인을 이름 있는 구간으로 나누고, 런타임에서 시작 Section 선택·점프·다음 Section 예약을 제공한다. 콤보, 재장전 분기, 홀드 루프처럼 “클립이 무엇인가”보다 “현재 어느 의미 구간인가”가 중요한 액션에 적합하다.

UPlayground는 Section을 도입하되 조건 판단은 MotionSet에 넣지 않는다.

```text
Ability / 상태 / BT
└── 조건을 판단
    └── ActorAnimator.SetNextSection("Attack_2")
        또는 JumpToSection("Release")

MotionSet
└── Section 범위와 기본 next 관계만 소유
```

#### Notify Link Method

Unreal은 Notify가 Segment 변경을 어떻게 따라갈지 `Absolute`, `Relative`, `Proportional`로 구분한다. 이 기능은 현재 MotionSet에서 클립 교체·트림·속도 변경 후 이벤트를 손으로 다시 맞추는 문제를 직접 해결한다.

#### Notify State Tick

Unreal Notify State는 Begin/Tick/End를 제공한다. UPlayground의 카메라 가중치, 레이어 블렌드, MotionWarp weight, VFX 강도처럼 구간 중 연속 변화가 필요한 기능에 대응한다.

#### Slot/Group의 의미

Unreal Slot은 단순 정수 레이어가 아니라 “UpperBody”, “FullBody” 같은 의미 있는 재생 지점이고, Slot Group은 동시 재생과 중단 규칙을 가진다. UPlayground는 Animancer를 사용하므로 AnimGraph Slot을 복제할 필요는 없지만, 정수 인덱스 위에 의미 채널과 동시성 정책을 두는 것은 유효하다.

### 3.3 채택하지 않거나 변형할 것

- **AnimGraph Slot 노드**: Animancer 레이어 + `AvatarMask`로 이미 해결하므로 복제하지 않는다.
- **동일 Slot Segment 오버랩**: Unreal 공식 문서도 같은 Slot의 동시 오버랩을 권장하지 않는다. UPlayground도 동일 채널은 순차 시퀀스를 유지하고, 병렬 표현은 별도 `MotionLayer`를 사용한다.
- **네트워크 Root Motion 복제**: 싱글플레이 비목표다.
- **Skeleton 전역 Slot Manager**: 1인 개발 규모에서는 관리 비용이 크다. 프로젝트 공용 `MotionChannelDefinitionSO` 또는 제한된 태그 목록으로 축소한다.
- **Child Montage 상속 트리**: 현재 `MotionReferenceSO` 무기 override와 `ActorAnimationMotionSet` fallback의 책임과 겹친다. 먼저 “클립 교체 시 타이밍 유지” 도구만 제공한다.
- **Asset 내부 조건 분기**: 조건은 Ability/상태/BT가 계속 소유한다. Section은 목적지와 기본 연결만 표현한다.

---

## 4. 확정 설계 결정

| ID | 결정 |
|----|------|
| D-01 | 기존 `MotionSetAsset`을 유지하며 별도 Montage 에셋으로 이원화하지 않는다. |
| D-02 | 기존 에셋은 필드 기본값만으로 동일하게 재생되어야 한다. 마이그레이션 전후 시간·이벤트 결과가 같아야 한다. |
| D-03 | Section은 이름 있는 재생 구간과 기본 next만 소유한다. 조건식은 Ability/상태/BT가 소유한다. |
| D-04 | 같은 재생 채널의 모션은 계속 순차 배치한다. 임의 오버랩은 병렬 `MotionLayer`로 표현한다. |
| D-05 | MotionEvent 시간 링크는 `Absolute`, `Relative`, `Proportional`을 우선 도입하고 Marker 링크는 후속 확장한다. |
| D-06 | 연속 이벤트는 기존 상속 계층을 깨지 않는 선택적 인터페이스로 추가한다. 기존 이벤트에 추상 메서드를 새로 강제하지 않는다. |
| D-07 | 정확도 정책은 `Queued`, `Exact`, 실행 단계는 `Update`, `PostEvaluation`으로 분리한다. “Branching Point”라는 Unreal 명칭은 그대로 사용하지 않는다. |
| D-08 | 공격 수치와 HitPhase 권위는 Ability 데이터에 남긴다. MotionSet은 stable hit phase 참조와 읽기 전용 오버레이만 제공한다. |
| D-09 | 재생 종료는 `Completed`, `Interrupted`, `Stopped`, `Invalidated` 사유를 구분한다. |
| D-10 | `MotionReferenceSO`/fallback과 겹치는 Child Montage 상속은 본 계획에서 보류한다. |
| D-11 | 에디터 기능과 런타임 데이터 변경을 분리하여 각 Phase 종료 시 기존 에셋을 계속 열고 재생할 수 있어야 한다. |
| D-12 | `[SerializeReference]` 타입 이동은 하지 않는다. 불가피하면 기존 어셈블리를 명시한 `[MovedFrom]`을 먼저 적용하고 데이터 검사를 통과한 뒤 이동한다. |

---

## 5. 목표 데이터 모델

아래 타입명은 설계 제안이며 구현 전 네임스페이스·asmdef를 최종 확정한다.

### 5.1 스키마 버전

```csharp
[Serializable]
public class MotionSet
{
    public int schemaVersion;
    public string motionSetName;
    public MotionSetBlendSettings blend;
    public List<MotionSection> sections;
    public List<MotionSyncMarker> syncMarkers;
    // 기존 필드 유지
}
```

- `schemaVersion == 0`: 현재 에셋.
- 필드가 비어 있으면 기존 순차 재생과 동일하게 해석한다.
- 버전 증가는 명시적 업그레이더가 처리한다.
- `OnEnable`에서 에셋을 자동 저장하거나 대량 재직렬화하지 않는다.

### 5.2 Section

```csharp
[Serializable]
public sealed class MotionSection
{
    public string id;                 // 에셋 내부 stable ID, 사용자 표시 이름과 분리
    public string displayName;
    public float startTime;
    public string defaultNextId;      // 비어 있으면 시간축의 다음 Section
    public MotionSectionEndPolicy endPolicy;
}

public enum MotionSectionEndPolicy
{
    Continue,
    Stop,
    Hold,
    LoopSelf,
}
```

규칙:

- 첫 Section은 0초에 있어야 한다.
- Section 구간은 현재 Section 시작부터 다음 Section 시작 전까지다.
- `id`는 rename과 무관하게 유지한다.
- `defaultNextId`는 기본 흐름만 표현한다.
- 런타임 조건 분기는 외부가 `SetNextSection` 또는 `JumpToSection`으로 덮어쓴다.
- `LoopSelf`는 기존 `LoopEvent`의 일반적인 무한 루프 용도를 흡수할 수 있지만, 기존 이벤트 제거는 콘텐츠 마이그레이션 완료 뒤 별도 결정한다.

### 5.3 재생 API

```csharp
public readonly struct MotionPlaybackRequest
{
    public MotionSetAsset asset;
    public string startSectionId;
    public float playRate;
    public float? blendInOverride;
}

public enum MotionSetEndReason
{
    Completed,
    Interrupted,
    Stopped,
    Invalidated,
}

public bool TryPlay(in MotionPlaybackRequest request);
public bool TryJumpToSection(string sectionId);
public bool TrySetNextSection(string fromSectionId, string nextSectionId);
public bool TryGetCurrentSection(out string sectionId);
public void StopMotionSet(float? blendOutOverride = null);
```

기존 `PlayMotion(...)` API는 호환 래퍼로 유지한다.

Section 점프 시 이벤트 정책:

1. 현재 활성 이벤트의 `Exit`를 역순으로 호출한다.
2. 지연 실행 대기열을 폐기한다.
3. 새 시간까지 지나간 이벤트를 “이미 실행됨”으로 표시한다.
4. 새 Section 시작 시각에 걸린 이벤트는 정확히 한 번 진입한다.
5. `LoopSelf` 재진입에서는 반복 허용 이벤트만 다시 실행한다.

이를 위해 이벤트 반복 정책을 명시한다.

```csharp
public enum MotionEventReentryPolicy
{
    OncePerPlayback,
    OncePerSectionEntry,
    EveryCrossing,
}
```

기본값은 기존 동작을 보존하는 `OncePerPlayback`이다.

### 5.4 블렌드

```csharp
[Serializable]
public sealed class MotionSetBlendSettings
{
    public float blendInDuration;
    public AnimationCurve blendInCurve;
    public float blendOutDuration;
    public AnimationCurve blendOutCurve;
    public float interruptedBlendOutDuration;
    public bool autoBlendOut = true;
    public bool holdLastPose;
}
```

1차 구현은 Animancer가 제공하는 레이어·상태 페이드를 사용한다.

- Asset 설정이 없으면 호출자 `fadeDuration`을 그대로 사용한다.
- 호출자 override > Asset 설정 > 기존 기본값 순으로 해석한다.
- 정상 종료와 중단 종료를 구분한다.
- 본별 Blend Profile은 Animancer/KCC 구조와 비용을 검토한 뒤 후속 과제로 둔다.
- 개별 `Motion` 사이 전환 블렌드는 Section/Asset 블렌드 안정화 후 도입한다.

### 5.5 의미 재생 채널

기존 `MotionLayer.animancerLayerIndex`는 실제 실행 바인딩으로 유지하고, 의미 식별자를 추가한다.

```csharp
[Serializable]
public sealed class MotionLayer
{
    public string channelId;          // 예: FullBody, UpperBody, AdditiveReaction
    public string concurrencyGroupId; // 같은 그룹의 새 재생이 기존 재생을 중단
    public MotionInterruptionPolicy interruptionPolicy;
    // 기존 필드 유지
}
```

권장 기본 채널:

| 채널 | 용도 |
|------|------|
| `FullBody` | 상태 모션을 대체하는 전신 액션 |
| `UpperBody` | 하체 로코모션 위 공격·장전 |
| `AdditiveReaction` | 반동·호흡·피격 가산 |
| `Cinematic` | 피니시·궁극기 연출 |

`channelId`가 비어 있으면 현재 `animancerLayerIndex` 직접 바인딩으로 동작한다. 전역 Slot Manager는 만들지 않고 프로젝트 설정 에셋에서 허용 채널과 기본 레이어·마스크만 검증한다.

### 5.6 이벤트 시간 링크

```csharp
[Serializable]
public class Motion // 기존 타입에 아래 필드 추가
{
    public string id;
    public List<MotionMarker> markers;
}

public enum MotionEventLinkMode
{
    Absolute,      // MotionSet 글로벌 초 고정
    Relative,      // 연결된 Motion 이동은 추종, 길이 변화는 추종하지 않음
    Proportional,  // 연결된 Motion 이동과 길이 비율 변화를 모두 추종
    Marker,        // 후속: 이름 있는 마커 기준 오프셋
}

[Serializable]
public struct MotionEventTimeLink
{
    public MotionEventLinkMode mode;
    public string linkedMotionId;
    public string markerId;
    public float startValue;
    public float endValue;
}

[Serializable]
public abstract class MotionEventBase // 기존 타입에 아래 필드 추가
{
    public MotionEventTimeLink timeLink;
    public MotionEventReentryPolicy reentryPolicy;
    public int executionOrder;
    public MotionEventDispatchMode dispatchMode;
    public MotionEventEvaluationPhase evaluationPhase;
}
```

해석:

| 모드 | 저장 의미 | 클립 이동 | 트림·속도 변경 |
|------|-----------|-----------|----------------|
| Absolute | 글로벌 초 | 고정 | 고정 |
| Relative | 모션 시작으로부터 초 | 추종 | 초 값 유지 |
| Proportional | 모션 정규화 0~1 | 추종 | 비율 유지 |
| Marker | 마커 + 초/프레임 오프셋 | 마커 추종 | 마커 추종 |

호환 규칙:

- `Motion.events[]`의 기존 이벤트는 `Relative`로 간주한다.
- `MotionSet.globalEvents[]`의 기존 이벤트는 `Absolute`로 간주한다.
- 기존 `startTime/endTime`은 당장 제거하지 않고 해석 결과 캐시 또는 호환 직렬화 필드로 유지한다.
- 시간 링크 편집은 Undo 가능한 단일 트랜잭션으로 처리한다.

### 5.7 이름 있는 마커

```csharp
[Serializable]
public sealed class MotionMarker
{
    public string id;
    public string displayName;
    public float normalizedTime;
    public MotionMarkerKind kind;
}

public enum MotionMarkerKind
{
    Generic,
    Anticipation,
    Impact,
    Recovery,
    CancelOpen,
    CancelClose,
    LeftFoot,
    RightFoot,
}
```

마커의 우선 용도:

- 이벤트 스냅과 링크
- 서로 다른 클립 교체 시 의미 타이밍 유지
- 병렬 레이어 동기화
- 에디터 관계선과 검증

`Impact` 마커가 공격 수치나 판정을 소유하지는 않는다. `BeginCollisionEvent`·VFX·카메라 이벤트가 같은 마커를 참조하여 정렬되는 구조다.

### 5.8 연속 이벤트

기존 `MotionEventBase`에 추상 메서드를 추가하면 모든 구현과 SerializeReference 타입에 영향을 준다. 선택적 인터페이스로 확장한다.

```csharp
public interface IMotionEventTick
{
    void Tick(GameObject target, float normalizedTime, float deltaTime);
}

public interface IMotionEventSignal
{
    string SignalId { get; }
}
```

실행 규칙:

- `Execute` = Enter.
- `IMotionEventTick.Tick` = 활성 구간 중 매 프레임.
- `OnCompleteEvent` = Exit.
- 중단·Section 점프에도 Exit를 보장한다.
- Tick 순서는 `executionOrder`, 같으면 타임라인·리스트 순서로 결정한다.
- 프리뷰에서는 부수효과 없는 `PreviewEvaluate` 포트를 별도로 둔다. 에디트 모드에서 런타임 `Execute`를 직접 호출하지 않는다.

우선 적용 후보:

- 애니메이션 속도 곡선
- MotionLayer 가중치 곡선
- MotionWarp translation/rotation weight
- 카메라 LookAt/FOV weight
- VFX 강도
- 오디오 볼륨·피치

### 5.9 이벤트 정확도와 실행 단계

```csharp
public enum MotionEventDispatchMode
{
    Queued,
    Exact,
}

public enum MotionEventEvaluationPhase
{
    Update,
    PostAnimationEvaluation,
}
```

- `Queued`: 일반 VFX·SFX처럼 한 프레임 내부 순서에 덜 민감한 이벤트. 같은 프레임의 이벤트를 수집 후 정렬 실행한다.
- `Exact`: Collision, Section 전환 신호처럼 순서와 서브프레임 위치가 중요한 이벤트. 타임 교차 순서대로 즉시 처리한다.
- `PostAnimationEvaluation`: 현재 `RequiresPostEvaluation`을 명시적 정책으로 일반화한다.
- 초기 기본값은 현재 동작을 보존한다. 기존 `RequiresPostEvaluation` override는 새 정책으로 연결하는 호환 래퍼를 둔다.

### 5.10 곡선 트랙

1차 곡선은 범용 문자열 Reflection 바인딩이 아니라 타입이 정해진 채널로 제한한다.

```csharp
public enum MotionCurveChannel
{
    PlaybackRate,
    LayerWeight,
    WarpTranslationWeight,
    WarpRotationWeight,
    CameraWeight,
    VfxIntensity,
}
```

각 곡선은 시간 링크와 동일하게 Absolute/Relative/Proportional 기준을 가진다. `AnimationSpeedEvent`처럼 현재 로그 중심인 이벤트는 실제 곡선 채널로 대체할 수 있지만, 기존 타입 제거는 에셋 사용처 0을 확인한 뒤 진행한다.

### 5.11 동기화 그룹

후순위 기능이다.

```csharp
[Serializable]
public sealed class MotionSyncSettings
{
    public string groupId;
    public MotionSyncRole role;       // Leader / CanLead / Follower
    public MotionSyncFallback fallback; // NormalizedTime / None
}
```

- 공통 마커가 있으면 마커 사이 정규화 위치를 맞춘다.
- 공통 마커가 부족하면 전체 길이 정규화로 폴백한다.
- Base와 UpperBody처럼 동작 의미가 다른 레이어를 강제 동기화하지 않는다.
- 걷기/달리기보다 공격의 상·하체 병렬 클립, 무기 보조 모델, 연출 레이어 동기화에 우선 사용한다.
- Follower 이벤트 발화 여부는 기본 false로 하여 중복 판정을 막는다.

---

## 6. 에디터 UX 설계

### 6.1 목표 레이아웃

```text
┌──────────────────────────────────────────────────────────────────────┐
│ Asset / Play / Section / Speed / Snap / Validate / Search            │
├──────────────┬──────────────────────────────────────┬─────────────────┤
│ Motion 목록  │ Section 흐름 / 타임라인              │ Inspector       │
│              │ ┌──────────────────────────────────┐ │                 │
│ AnimKey      │ │ Sections                         │ │ Selection       │
│ Favorites    │ │ Motion Channels                  │ │ Time Link       │
│ Validation   │ │ Markers                          │ │ Blend           │
│              │ │ Events / Curves / Overlay        │ │ Relations       │
│              │ └──────────────────────────────────┘ │ Validation      │
└──────────────┴──────────────────────────────────────┴─────────────────┘
```

### 6.2 Section 흐름 패널

- Section 헤더 생성·이름 변경·이동·삭제
- 기본 `next` 연결을 노드가 아닌 간결한 목록으로 표시
- `Preview All`, `Preview From Here`, `Loop Section`
- 런타임 현재 Section과 예약된 next Section 표시
- 순환 연결, 존재하지 않는 목적지, 도달 불가 Section 검증
- 복잡한 조건 노드·분기 그래프는 제공하지 않는다.

### 6.3 다중 선택과 그룹 편집

- Ctrl/Shift 토글 선택
- 빈 공간 드래그 마퀴 선택
- `Ctrl+A`, `Ctrl+C/V/D`, Delete
- 선택 이벤트 리지드 이동
- 시작/끝 비율 스케일
- 다른 MotionSetAsset 간 딥클론 붙여넣기
- 붙여넣기 기준: 원본 시간 / 커서 시간 / 선택 마커
- 선택 묶음을 이름 있는 저작 그룹으로 저장

저작 그룹은 런타임 실행 단위가 아니다. 에디터 선택·정렬·프리셋 메타데이터일 뿐이다.

### 6.4 이벤트 레인

현재 이벤트 타입별 1행 나열을 의미 레인으로 개선한다.

| 레인 | 주요 이벤트 |
|------|-------------|
| Combat | Collision, DisableCollision, Telegraph, FinishAttack |
| Window | ComboWindow, CancelWindow, Invincibility |
| Movement | AddForce, MotionWarp, Root Motion Disable |
| VFX | Particle, SlashVFX, Afterimage, SpawnSkill |
| Projectile | SpawnProjectile |
| SFX | PlaySound, Footstep |
| Camera | CameraEffect, LookAt, Snapshot, SideView |
| Flow | Loop 호환 표시, Section Signal |
| Utility | Callback, HideTarget, Freeze |

기능:

- 레인 접기·숨김·Solo·잠금
- 이벤트 밀도에 따른 자동 lane packing
- 레인 필터와 타입 검색
- 동일 마커/HitPhase를 공유하는 이벤트 하이라이트
- 이벤트 발화 순서 번호 표시

### 6.5 의미 스냅

스냅 우선순위:

1. 명시적 Marker
2. 다른 이벤트 시작·끝
3. Section 경계
4. Motion 경계
5. 프레임 격자

- 픽셀 임계값 기반 흡착
- 세로 가이드와 대상 라벨 표시
- Alt로 일시 해제
- Shift 드래그 시 커서를 선택 이벤트와 함께 이동
- 스냅 결과가 어떤 기준인지 툴팁으로 표시

### 6.6 관계 시각화

다음 항목은 같은 관계 키로 연결한다.

```text
Anticipation Marker
└── Telegraph(hitPhase: Heavy_1)

Impact Marker
├── Collision(hitPhase: Heavy_1)
├── SlashVFX
├── CameraShake
└── Sound
```

- 선택 시에만 관계선을 표시하여 화면 혼잡을 막는다.
- `hitPhaseIndex`는 최소한 에디터에서 Ability의 해당 `HitPhaseData` 이름과 함께 표시한다.
- 장기적으로 배열 인덱스 대신 stable phase ID를 사용하되, 이는 Ability 데이터 마이그레이션과 함께 별도 진행한다.
- MotionSet에 피해량·범위 수치를 복제하지 않는다.

### 6.7 곡선 편집기

- 타임라인 하단 Curve 영역
- 채널별 색상과 최소·최대 범위
- 키 추가·삭제·탄젠트 변경
- 선택 구간 Normalize
- 프리뷰 중 현재 값 표시
- LayerWeight는 대상 `channelId`를 필수 지정
- 곡선이 없는 경우 기존 고정값 사용

### 6.8 클립 교체

Unreal Child Montage의 장점 중 “클립을 바꿔도 기존 구간을 유지”하는 부분만 에디터 도구로 먼저 제공한다.

`Replace Clip Preserving Timing` 옵션:

- `PreserveSeconds`: 트림·이벤트 초 유지
- `PreserveNormalized`: 트림·Proportional 이벤트 비율 유지
- `PreserveMarkers`: 같은 이름 마커를 기준으로 재배치
- 교체 전후 총 길이·Impact 시각·이벤트 이동량 미리보기
- 적용 전 Undo 스냅샷

별도 상속 에셋은 만들지 않는다.

### 6.9 프리뷰

프리뷰는 “애니메이션 재생”에서 “액션 상황 검증”으로 확장한다.

- 공격자·표적 동시 배치
- 거리·높이·좌우 각도 프리셋
- Section 시작 재생과 next Section 수동 전환
- Root Motion 궤적과 속도 그래프
- MotionWarp 목표·예상 경로·도착 오차
- 히트박스 sweep 잔상
- 투사체 궤적·착탄 위치
- 카메라 프러스텀·LookAt 경로
- 활성 이벤트와 Curve 현재값
- 이전 저장 버전 또는 비교 에셋 고스트
- 드라이런이 기본이며, VFX/SFX/카메라 등 부수효과 실행은 명시적 토글

에디트 모드에서는 실제 전투 판정·오브젝트 생성·서비스 점유를 실행하지 않는다. 타입별 `IMotionEventPreviewAdapter`가 읽기 전용 시각화를 제공한다.

---

## 7. 검증 설계

### 7.1 정적 검증 규칙

| ID | 심각도 | 규칙 |
|----|--------|------|
| M001 | 오류 | Motion/Section/Marker stable ID 중복 |
| M002 | 오류 | Section이 0초에서 시작하지 않음 |
| M003 | 오류 | `defaultNextId` 대상 없음 |
| M004 | 경고 | 도달할 수 없는 Section |
| M005 | 오류 | 이벤트 `end < start` 또는 유효 시간축 밖 |
| M006 | 오류 | linked Motion/Marker가 없음 |
| M007 | 경고 | Proportional 값이 0~1 밖 |
| M008 | 오류 | 같은 Animancer layer index에 충돌하는 활성 채널 |
| M009 | 경고 | 병렬 레이어 길이 또는 Section 경계 불일치 |
| M010 | 오류 | Collision/Telegraph의 HitPhase 참조 불일치 |
| M011 | 경고 | 카메라 입력 잠금 이벤트의 복구 경로가 불명확 |
| M012 | 경고 | MotionWarp 대상 정책과 프리뷰 대상 계약 불일치 |
| M013 | 오류 | 필수 VFX/Audio/Prefab/Profile 참조 누락 |
| M014 | 정보 | 런타임 실동작이 제한된 이벤트 사용 |
| M015 | 경고 | Exact 이벤트가 같은 시간·같은 순서로 충돌 |
| M016 | 오류 | Loop Section에서 `OncePerPlayback` 필수 이벤트만 존재해 재진입 의도 불명확 |
| M017 | 경고 | Sync Group 공통 마커 부족 |
| M018 | 오류 | `[SerializeReference]` missing type |

검증 결과는 다음 세 곳에 동시에 표시한다.

- 툴바 상태 pill
- 타임라인 블록·Section·레이어 뱃지
- 인스펙터 상세 목록과 “문제로 이동” 버튼

### 7.2 자동 테스트

#### EditMode

- 기존 schemaVersion 0 에셋의 Duration·이벤트 글로벌 시간이 변경되지 않음
- Section 경계 해석과 default next
- Section 점프 시 활성 이벤트 Exit 보장
- ReentryPolicy별 재실행 횟수
- Absolute/Relative/Proportional 시간 링크 변환
- 클립 트림·속도 변경 후 링크 보존
- stable ID 생성·중복 검출
- 정상/중단 Blend Out 선택
- Tick 이벤트 Enter/Tick/Exit 순서
- 같은 프레임 Exact 이벤트 정렬
- 딥클론 시 Unity Object 참조와 SerializeReference 파생 필드 보존
- Undo/Redo 후 Section·Marker·Event 선택 복구

#### PlayMode

- 콤보 Section 예약 후 다음 구간 재생
- 홀드 Section 루프 후 Release Section 점프
- 중단 시 Collision·무적·카메라 잠금 정리
- Base + UpperBody 병렬 레이어와 마스크
- MotionWarp + Section 점프
- 루트 모션 궤적과 KCC 이동 일치
- 재생 완료/중단/정지 종료 사유
- 기존 Ability Payload가 동일 MotionSet을 정상 실행

### 7.3 성능 기준

- 비재생·비편집 상태에서 상시 전체 타임라인 repaint 금지
- 재생 중 커서는 별도 VisualElement 위치만 갱신
- 드래그 중 검증은 경량 증분 규칙만 실행
- 전체 검증은 입력 정지 후 debounce하거나 명시적 요청으로 실행
- 500 이벤트 기준 선택·줌·스크롤에서 에디터 GC 할당 최소화
- Tick 이벤트는 활성 이벤트만 순회하며 매 프레임 LINQ·새 리스트 생성을 금지

---

## 8. 단계별 구현 계획

### Phase 0 — 안전망과 기준선

- [ ] 대표 MotionSet 샘플 선정: 일반 근접, 다단 콤보, 홀드/루프, 상체 레이어, MotionWarp, 카메라 연출
- [ ] 현재 에셋 Duration·이벤트 글로벌 시간 스냅샷 테스트
- [ ] `schemaVersion`과 읽기 전용 검증기 추가
- [ ] Missing managed reference/VFX 기준선 재측정
- [ ] 에디터 유휴 CPU·메모리 기준선 기록

완료 기준:

- 기존 에셋을 저장하지 않고 검사 가능하다.
- 기존 재생 결과를 비교할 자동 기준이 있다.

### Phase 1 — 저위험 에디터 UX

- [ ] 다중 선택·마퀴 선택·그룹 이동·그룹 리사이즈
- [ ] 에셋 간 클립보드와 커서 기준 붙여넣기
- [ ] 이벤트 의미 레인·필터·Solo/Mute/Lock
- [ ] 이벤트/Section/Motion 경계 스냅 가이드
- [ ] 통합 발화 순서 Timing Track
- [ ] 검증 pill·뱃지·문제로 이동
- [ ] 사용자 프리셋 클론을 `JsonUtility`에서 SerializeReference 안전 경로로 교체

완료 기준:

- 데이터 모델 변경 없이 반복 저작과 정렬 작업이 가능하다.
- 다중 편집이 한 Undo group으로 복구된다.
- 중첩 SerializeReference가 있는 이벤트도 프리셋에서 손실되지 않는다.

### Phase 2 — Section과 종료 사유

- [ ] `MotionSection`, stable ID, default next
- [ ] Section 흐름 패널
- [ ] 시작 Section, Jump, SetNextSection API
- [ ] `MotionSetEndReason`
- [ ] Section 점프 이벤트 정리 규칙
- [ ] 기존 `LoopEvent`와 Section Loop의 우선순위·호환 처리
- [ ] 콤보/홀드/Release 수직 슬라이스

완료 기준:

- 콤보와 홀드 공격을 MotionSet 내부 조건식 없이 Section API로 제어한다.
- 중단 시 활성 이벤트 누수가 없다.
- Section이 없는 기존 에셋은 동일하게 순차 재생한다.

### Phase 3 — 시간 링크와 마커

- [ ] `MotionEventTimeLink`
- [ ] Absolute/Relative/Proportional 변환
- [ ] Motion/Marker stable ID
- [ ] 마커 생성·이동·이름 변경
- [ ] 의미 스냅과 관계선
- [ ] 클립 교체 타이밍 보존 미리보기
- [ ] Ability HitPhase 읽기 전용 관계 표시

완료 기준:

- 클립 트림·속도·교체 후 선택한 링크 정책대로 이벤트 의도가 보존된다.
- Impact 마커 하나로 Collision/VFX/SFX/Camera를 정렬할 수 있다.

### Phase 4 — 이벤트 생명주기와 정확도

- [ ] `IMotionEventTick`
- [ ] ReentryPolicy
- [ ] Queued/Exact dispatch
- [ ] Update/PostAnimationEvaluation 단계
- [ ] 결정적 실행 순서
- [ ] 외부 Begin/End Signal 포트
- [ ] 프리뷰 어댑터 계약

완료 기준:

- 연속 이벤트가 Enter/Tick/Exit로 동작한다.
- Section 점프·루프·강제 중단에도 정리 순서가 결정적이다.
- Collision과 공간 VFX의 실행 시점 회귀가 없다.

### Phase 5 — 블렌드와 의미 채널

- [ ] Asset Blend In/Out/Interrupted 설정
- [ ] 자동 Blend Out·Hold Last Pose
- [ ] `channelId`, concurrency group, interruption policy
- [ ] LayerWeight 곡선
- [ ] 채널 충돌 검증
- [ ] FullBody/UpperBody/AdditiveReaction 수직 슬라이스

완료 기준:

- 호출부마다 흩어진 기본 fade 값을 Asset 정책으로 통일할 수 있다.
- UpperBody 액션 중단과 FullBody 액션 우선순위가 데이터에서 드러난다.

### Phase 6 — Curve와 상황 프리뷰

- [ ] Curve Track UI
- [ ] PlaybackRate·Warp·Camera·VFX 표준 채널
- [ ] 드라이런 Preview Adapter
- [ ] 공격자·표적 프리셋
- [ ] Root Motion·Warp·Hitbox·Projectile·Camera 통합 시각화
- [ ] A/B 고스트 비교

완료 기준:

- PlayMode 진입 없이 액션의 포즈·경로·판정·연출 타이밍을 안전하게 검토한다.
- 실제 부수효과는 명시적으로 허용하지 않는 한 발생하지 않는다.

### Phase 7 — 동기화와 시간 스트레치

- [ ] Sync Group/Role
- [ ] 공통 Marker 기반 동기화
- [ ] Follower 이벤트 억제 정책
- [ ] Time Stretch Curve
- [ ] 공격 속도 변경 시 Impact 보호 구간
- [ ] 병렬 레이어·보조 모델 수직 슬라이스

완료 기준:

- 길이가 다른 병렬 클립이 공통 마커 기준으로 동기화된다.
- 공격 속도를 바꿔도 Impact 구간은 보존하고 준비·회수 구간을 우선 압축할 수 있다.

---

## 9. 마이그레이션 전략

### 9.1 원칙

1. 새 필드는 모두 기존 동작을 보존하는 기본값을 가진다.
2. 로드 시 자동 저장하지 않는다.
3. 업그레이드는 단일 에셋 미리보기 → 선택 에셋 → 전체 에셋 순으로 분리한다.
4. Dry Run에서 변경될 필드·이벤트 시간·stable ID를 보고한다.
5. 업그레이드는 `Undo` 또는 백업 가능한 명시적 명령으로만 실행한다.
6. 컴파일 오류나 missing managed reference가 있으면 저장·일괄 재직렬화를 금지한다.

### 9.2 schemaVersion 0 해석

| 기존 데이터 | 신규 해석 |
|-------------|-----------|
| Section 없음 | 암시적 `Default` Section, 0초부터 끝까지 Continue |
| `Motion.events[]` | Relative 링크 |
| `MotionSet.globalEvents[]` | Absolute 링크 |
| `MotionLayer.weight` | 곡선 없는 고정 weight |
| 호출자 fadeDuration | Asset blend 미지정 시 그대로 사용 |
| `LoopEvent` | 기존 ActorAnimator 특수 처리 유지 |
| `RequiresPostEvaluation` | PostAnimationEvaluation 단계 |

### 9.3 stable ID

- stable ID는 에디터 업그레이더가 1회 생성한다.
- 이름·리스트 인덱스를 ID로 사용하지 않는다.
- 복제 시 새 ID를 생성하되 내부 링크도 함께 치환한다.
- 에셋 간 붙여넣기에서 외부 링크는 명시적으로 재바인딩하거나 경고한다.

### 9.4 직렬화 안전

- 기존 `[SerializeReference]` 이벤트 클래스의 namespace/assembly를 이동하지 않는다.
- 이동이 필요하면 `[MovedFrom(true, sourceAssembly: "...")]`을 먼저 추가한다.
- 프리셋·클립보드 딥클론은 `JsonUtility` 의존을 제거한다. 현재 방식은 중첩 폴리모픽 필드를 보존하지 못한다.
- 전체 업그레이드 전 MotionSet/Ultimate 1,156개, managed reference 1,638개, VFX 168개 기준 검사를 다시 실행하고 최신 기준값을 문서에 기록한다.
- `Assets/10.Datas/`와 `Assets/03.Prefabs/` 자동 변경은 모두 diff를 검사한다.

---

## 10. 모듈 경계

| 책임 | 위치 |
|------|------|
| `MotionSection`, 링크·마커·블렌드·동기화 데이터 | 기존 Motion 데이터가 속한 `UPlayGround.Data` 경계 |
| Section 재생, 이벤트 Dispatch/Tick, 채널 중단 정책 | `UPlayGround.Actor` |
| Animancer 연결 | `UPlayGround.Actor` 내부 애니메이션 어댑터 |
| 타임라인·인스펙터·검증기·마이그레이터 | 모듈별 Editor asmdef 또는 `Assets/02.Scripts/Editor/` |
| Ability HitPhase 읽기 전용 오버레이 | Editor가 Ability 데이터에서 읽되 MotionSet에 수치 복제 금지 |
| Camera 프리뷰 | Camera 런타임 직접 의존을 새로 만들지 않고 기존 어댑터/에디터 프리뷰 경계 사용 |

Camera 모듈 내부에 `Svc.*`, `IWorldActor`, 구체 전투 서비스를 추가하지 않는다. MotionEvent의 Camera 실행 경로를 정리할 때도 `ICameraRuntimeAdapter` 경계를 우선 검토한다.

---

## 11. 위험과 대응

| 위험 | 영향 | 대응 |
|------|------|------|
| Section이 상태 머신을 침범 | 흐름 권위 이중화 | 조건은 외부, Section은 범위·기본 next만 소유 |
| 시간 링크 추가로 기존 시간이 변함 | 전투 타이밍 회귀 | schema 0 호환 해석 + 스냅샷 테스트 |
| Section 점프 중 이벤트 누수 | 무적·Collision·카메라 잠금 잔류 | Exit 역순 보장 + PlayMode 중단 테스트 |
| Tick 이벤트 남용 | CPU 증가 | 활성 이벤트만 순회, 채널 제한, 프로파일링 기준 |
| 채널과 Animancer 인덱스 불일치 | 잘못된 본 레이어 재생 | 프로젝트 채널 설정 검증 + 명시적 fallback |
| 마커/ID 복제 충돌 | 링크 오작동 | 복제 시 ID 재발급과 내부 참조 치환 |
| 프리뷰가 런타임 부수효과 실행 | 에디터 씬·서비스 오염 | Preview Adapter와 Dry Run 기본값 |
| Child Montage식 상속 추가 | MotionReference/fallback과 책임 중복 | 본 계획에서 보류 |
| 대량 재직렬화 | managed reference/VFX 유실 | 컴파일·타입 매핑·Dry Run 검증 전 저장 금지 |
| UI 기능 급증 | 1인 개발 유지보수 부담 | Phase별 수직 슬라이스, 범용 그래프·NLE 비목표 유지 |

---

## 12. 전체 완료 조건

- [ ] 기존 MotionSet 에셋의 재생 시간과 이벤트 발화가 호환 모드에서 동일하다.
- [ ] Section 시작·점프·다음 예약·루프가 결정적으로 동작한다.
- [ ] Section 점프·중단·정지 후 활성 MotionEvent 누수가 없다.
- [ ] Absolute/Relative/Proportional/Marker 링크가 편집 후 의도대로 유지된다.
- [ ] 다중 선택·그룹·클립보드·의미 스냅이 Undo/Redo와 함께 동작한다.
- [ ] Enter/Tick/Exit 이벤트와 실행 정확도·평가 단계가 검증된다.
- [ ] Blend In/Out/Interrupted와 의미 채널 중단 정책이 동작한다.
- [ ] Curve Track과 Preview Adapter가 런타임 부수효과 없이 결과를 보여준다.
- [ ] HitPhase·VFX·Camera·Warp·Projectile 관계 오류가 에디터에서 드러난다.
- [ ] Unity 컴파일 오류 0.
- [ ] 런타임 무가드 `UnityEditor` 참조 0.
- [ ] Missing Script 0.
- [ ] managed reference/VFX 누락 0.
- [ ] Play Mode 서비스 경고·예외 0.
- [ ] Camera Play Mode 스모크와 StandaloneWindows64 Development Player Build 오류 0.

---

## 13. 공식 레퍼런스

- [Animation Montage — Sections, Slots, Timing Track, Child Montages](https://dev.epicgames.com/documentation/unreal-engine/animation-montage-in-unreal-engine?lang=en-US)
- [Animation Montage Editor — Blend In/Out, Sync Group, Time Stretch Curve](https://dev.epicgames.com/documentation/unreal-engine/animation-montage-editor-in-unreal-engine?lang=en-US)
- [Animation Notifies — Notify State, Tick, Trigger Weight, Link Method, Montage Tick Type](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-notifies-in-unreal-engine)
- [Animation Slots — Slot, Slot Group, 상·하체 레이어와 중단 규칙](https://dev.epicgames.com/documentation/unreal-engine/animation-slots-in-unreal-engine)
- [Animation Sync Groups — Leader/Follower와 Marker 기반 동기화](https://dev.epicgames.com/documentation/unreal-engine/animation-sync-groups-in-unreal-engine?lang=en-US)
- [Root Motion — 추출·적용 정책과 성능 고려](https://dev.epicgames.com/documentation/unreal-engine/root-motion-in-unreal-engine)
- [Play Montage — Completed, Blend Out, Interrupted, Notify Begin/End 콜백](https://dev.epicgames.com/documentation/unreal-engine/BlueprintAPI/Animation/Montage/PlayMontage?lang=en-US)
- [Blend Masks and Blend Profiles — 본별 블렌드 속도](https://dev.epicgames.com/documentation/unreal-engine/blend-masks-and-blend-profiles-in-unreal-engine?lang=en-US)
- [FTimeStretchCurve — 재생 속도 변화에서 구간별 시간 압축](https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Engine/Animation/FTimeStretchCurve?application_version=5.5)
