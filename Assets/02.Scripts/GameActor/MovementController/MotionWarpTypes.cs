using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround;
using UPlayGround.Debugging;
using UPlayGround.State;

namespace UPlayGround.MovementController
{
    public enum MotionWarpModifierType
    {
        Additive,
        Scale,
        Skew,
        // delta-warp: 원본 루트 델타를 재생하며 잔여 보정을 루트모션 비례 분배 → 커브 보존 + 정확 착지.
        // 신규 표준 경로. Additive/Scale/Skew 는 레거시(기존 .asset 호환)로 보존.
        DeltaWarp
    }

    public enum MotionWarpTargetPolicy
    {
        Snapshot,
        Live,
        // 매 프레임 anchor 위치를 갱신하면서, 추정된 타겟 속도 × predictionFactor × 남은 시간 만큼
        // 미래 위치를 예측해 보정. Live 단독 사용 시의 떨림을 줄이고 빠른 타겟 추적 정확도 개선.
        Predictive,
    }

    /// <summary>
    /// 워프 Y축 처리 정책. 기본은 IgnoreY 로 1차 동작 호환.
    /// </summary>
    public enum WarpYPolicy
    {
        IgnoreY = 0,         // 수평 보정만, Y 는 루트모션/중력에 위임 (현재 ignoreY=true 동등).
        MatchTargetY,        // 타겟 Y 도 적극 추적. 점프/공중 마무리 공격용.
        ProjectToTargetY,    // 워프 진행도에 따라 Y 도 점진 보간. 지면 높이 차 흡수용.
    }

    public enum MotionWarpPreset
    {
        Custom,
        LightAttack,
        HeavyAttack,
        FinishAttack,
        Grab
    }

    [Serializable]
    public struct MotionWarpWindowSettings
    {
        public float duration;
        public MotionWarpPreset preset;
        public MotionWarpModifierType modifierType;
        public MotionWarpTargetPolicy targetPolicy;
        public float translationWeight;
        public float rotationWeight;
        public bool ignoreY;
        public WarpYPolicy yPolicy;
        public bool overrideDistance;
        public float minDistance;
        public float maxDistance;
        public float maxSpeed;
        public Vector3 targetOffset;
        // 정규화 시간 t 를 회전 보간 알파로 매핑하는 곡선. null 이면 EaseOut(1-(1-t)^2) 폴백.
        public AnimationCurve rotationCurve;
        // Predictive 정책에서 타겟 속도를 어느 정도 가산할지 (0~1). 0 = Live 와 동일.
        public float predictionFactor;

        // ── 루트모션 속도 증폭 (직교 프리멀티플라이어) ──
        // 타겟 워프와 별개로, 루트모션 고유 속도 곡선에 게인을 곱해 증폭한다.
        // 타겟이 없어도 동작하며, 타겟이 있으면 증폭된 속도 위에서 워프가 합성된다.
        public bool amplifyEnabled;
        // 정규화 시간 t(0~1) → 게인 배율. null/빈 커브면 증폭 없음.
        // 접지 프레임 ≈1, 버스트 구간만 >1 로 두어 풋 슬라이딩을 최소화한다.
        public AnimationCurve amplifyGainCurve;
        // 증폭 결과 수평 속력의 자체 상한. 기존 워프 maxSpeed 와 분리.
        public float amplifyMaxSpeed;

        // ── delta-warp 캐시 키용 윈도우 절대 시간 (MotionEvent 의 start/endTime) ──
        // 액션 재생 간 동일 → 윈도우 총 루트모션 캐시 키의 일부.
        public float windowStartTime;
        public float windowEndTime;

        // ── 에디터 베이크 시드 (첫 시전부터 정확 delta-warp) ──
        // bakedValid 면 런타임 캐시 lookup 보다 우선해 BeginWarpWindow 가 _activeTotal 을 직접 시드한다.
        // 콤보/스킬처럼 세션 내 같은 단 재시전이 드물어 캐시가 못 데워지는 경우(=대부분의 실전 스윙)에도
        // 첫 시전부터 정확 모드로 진입한다. 베이크는 실제 액터 프리팹의 ActorAnimator.DeltaPosition 누적이라
        // 런타임 측정과 동일 소스·동일 스케일 → 변환 불필요.
        public bool    bakedValid;
        public Vector3 bakedLocalTotal;   // facing-불변 로컬 수평 총 변위 (런타임 _accumRootLocal 과 동일 정의)
        public float   bakedPathLen;      // 수평 경로 길이 (런타임 _accumRootPath 과 동일 정의)

        public static MotionWarpWindowSettings Default(float duration)
        {
            return new MotionWarpWindowSettings
            {
                duration = duration,
                preset = MotionWarpPreset.Custom,
                modifierType = MotionWarpModifierType.Additive,
                targetPolicy = MotionWarpTargetPolicy.Snapshot,
                translationWeight = 1f,
                rotationWeight = 1f,
                ignoreY = true,
                yPolicy = WarpYPolicy.IgnoreY,
                overrideDistance = false,
                minDistance = 0.3f,
                maxDistance = 4f,
                maxSpeed = 18f,
                targetOffset = Vector3.zero,
                rotationCurve = null,
                predictionFactor = 0.5f,
                amplifyEnabled = false,
                amplifyGainCurve = null,
                amplifyMaxSpeed = 25f,
                windowStartTime = 0f,
                windowEndTime = 0f,
                bakedValid = false,
                bakedLocalTotal = Vector3.zero,
                bakedPathLen = 0f,
            };
        }

        /// <summary>
        /// ignoreY bool 과 yPolicy enum 의 호환 매핑.
        /// 데이터 측에서 yPolicy 가 IgnoreY 기본값이면 ignoreY bool 을 우선 사용.
        /// </summary>
        public WarpYPolicy ResolveYPolicy()
        {
            if (yPolicy != WarpYPolicy.IgnoreY) return yPolicy;
            return ignoreY ? WarpYPolicy.IgnoreY : WarpYPolicy.MatchTargetY;
        }
    }

    /// <summary>
    /// MotionSet 이벤트로 열린 워프 구간에서 루트모션 속도를 타겟 방향으로 보정한다.
    /// State는 타겟 선택과 Combat 타이머만 전달하고, 스냅샷/도달 가능성/블렌딩은 여기서 공통 처리한다.
    /// </summary>
    /// <summary>
    /// 워프 캔슬 사유. OnWarpCancelled 이벤트와 함께 전달.
    /// </summary>
    public enum WarpCancelReason
    {
        ExternalEnd,        // EndMotionWarp 가 외부에서 조기 호출됨 (Hit/KnockBack/사망)
        OutOfRangeTimeout,  // OOR 누적 시간이 임계 초과
        TargetLost,         // 타겟이 파괴/사망
        ManualClear,        // ClearTarget 호출
    }
}
