using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// Motion Warp 활성 구간 이벤트.
    /// startTime ~ endTime 구간 동안 IsMotionWarping = true.
    /// Execute 시 이벤트 구간과 세션 핸들을 Controller에 전달하고,
    /// OnComplete에서는 마지막 KCC 소비 뒤 종료하도록 예약한다.
    /// 플레이어(PlayerCombat)와 몬스터(EnemyCombat) 모두 지원.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("MotionWarp", "Movement / Time", 0, "모션 워핑을 적용합니다.", "warp", "root", "타겟 보정", "워프")]
    public class MotionEvent_MotionWarp : MotionEventBase
    {
        public const int TotalOnlyBakeFormatVersion = 2;
        public const int CurrentBakeFormatVersion = 3;
        public const int TrajectorySampleCount = 33;
        [HideInInspector] public int settingsVersion;

        // 워프 전역 토글은 SettingsManager.Data.debugMotionWarpEnabled 로 위임.
        // SettingsManager 미로드/Data null 인 초기 프레임에는 활성 기본값.
        private static bool IsMotionWarpEnabled
        {
            get
            {
                var sm = Svc.Settings;
                if (sm == null || !sm.IsLoaded || sm.Data == null) return true;
                return sm.Data.debugMotionWarpEnabled;
            }
        }

        [Header("Warp Modifier")]
        [HideInInspector] public MotionWarpPreset preset = MotionWarpPreset.Custom;
        [HideInInspector] public MotionWarpModifierType modifierType = MotionWarpModifierType.EndpointCorrection;
        public MotionWarpTargetPolicy targetPolicy = MotionWarpTargetPolicy.Snapshot;

        [Header("Target Resolver")]
        [Tooltip("UseExisting: AttackState 가 미리 설정한 타겟 그대로 사용 (기존 호환).\n" +
                 "ConeNearest / LockOnFirst / Hybrid: 이벤트 발화 시점에 재결정.\n" +
                 "Hybrid 권장: 락온이 콘 안이면 락온, 밖이면 콘 최근접.")]
        [HideInInspector] public WarpResolverPolicy resolverPolicy = WarpResolverPolicy.UseExisting;

        [Header("Multi-Target Key")]
        [Tooltip("같은 키를 가진 두 이벤트는 같은 타겟을 공유 (도약-착지 등 다단 모션).\n" +
                 "다른 키를 쓰면 별도 타겟. 비워두면 \"primary\" 기본 키 사용.")]
        public string targetKey = "primary";

        [Header("Predictive Live")]
        [Tooltip("targetPolicy 가 Predictive 일 때만 사용.\n" +
                 "타겟 추정 속도 × 이 비율 × 남은 워프 시간 만큼 미래 위치 가산. 0 = Live 동등.")]
        [Range(0f, 1f)]
        [HideInInspector] public float predictionFactor = 0.5f;

        [Range(0f, 1f)]
        public float translationWeight = 1f;
        [Range(0f, 1f)]
        public float rotationWeight = 1f;

        [Tooltip("[Deprecated] yPolicy 가 IgnoreY 기본값일 때만 사용. 신규 설정은 yPolicy 를 사용.")]
        public bool ignoreY = true;

        [Header("Y Axis Policy")]
        [Tooltip("IgnoreY: 수평만 보정 (현재 동작).\n" +
                 "MatchTargetY: 점프/공중 마무리 등 Y 도 적극 추적.\n" +
                 "ProjectToTargetY: 진행도에 따라 Y 점진 보간 (지면 높이 차 흡수).")]
        public WarpYPolicy yPolicy = WarpYPolicy.IgnoreY;

        [Header("Rotation Curve")]
        [Tooltip("정규화 시간 t(0~1) → 회전 보간 알파(0~1).\n" +
                 "비워두면 EaseOut(1-(1-t)^2) 폴백. 프리셋이 자동으로 곡선을 채울 수 있음.")]
        public AnimationCurve rotationCurve;

        [Header("Override Range")]
        public bool overrideDistance = false;
        public float minDistance = 0.3f;
        public float maxDistance = 4f;
        public float maxSpeed = 18f;

        [Header("Offset")]
        public Vector3 targetOffset = Vector3.zero;

        [Header("Arrival")]
        [Tooltip("TargetCenter는 기존 에셋 호환, ContactShell은 양쪽 캡슐 표면과 무기 간격을 반영합니다.")]
        public WarpArrivalMode arrivalMode = WarpArrivalMode.TargetCenter;
        [Min(0f)] public float desiredStandOff = 0.18f;
        [Tooltip("타겟 로컬 축 기준 도착점 미세 조정입니다.")]
        public Vector3 localArrivalOffset = Vector3.zero;

        [Header("Translation Constraints")]
        [Min(0f)] public float minimumSourceDisplacement = 0.05f;
        [Range(0f, 1f)] public float minimumNetToPathRatio = 0.5f;
        [Min(0f)] public float maxHeightDifference = 0.6f;
        public LayerMask obstacleLayers = (1 << 0) | (1 << 3);
        [Min(0f)] public float noTranslationWithinReach = 0.12f;
        [Min(0f)] public float maxCorrectionDistance = 0.5f;
        [Tooltip("원본 전진량 대비 추가 보정 배수입니다. 1은 100%이며, 절대 거리 상한도 함께 적용됩니다.")]
        [Min(0f)] public float maxCorrectionRatio = 0.3f;
        [Tooltip("Reject는 예산 초과 목표를 거절하고, Clamp는 원본 이동·거리 상한 안에서만 일부 접근합니다.")]
        public MotionWarpCorrectionLimitPolicy correctionLimitPolicy;
        [Tooltip("RootPath: 원본 루트가 움직인 만큼만 보정합니다. 발 고정이 우선인 접촉 보정용입니다.\n" +
                 "AuthoredCurve: 아래 곡선의 시간 진행대로 보정합니다. 원본 이동이 작은 공격이 대상까지 치고 들어가는 거리 좁히기용이며, " +
                 "원본 비율 상한 대신 최대 추가 거리만 적용합니다.")]
        public MotionWarpCorrectionTiming correctionTiming = MotionWarpCorrectionTiming.RootPath;
        [Tooltip("AuthoredCurve 전용. 이동 보정 구간(종료 여유 전까지) 진행도 0~1 → 누적 보정 비율 0~1.\n" +
                 "앞쪽이 가파를수록 초반에 빠르게 들어갑니다. 비워 두면 일정한 속도입니다. 최대 속도 상한을 넘는 몫은 뒤 프레임에서 이어 소비합니다.")]
        public AnimationCurve correctionTimingCurve;
        [Range(0f, 180f)] public float maxWarpAngle = 45f;
        [Tooltip("정규화 시간별 위치 보정 Weight. 비어 있으면 1입니다.")]
        public AnimationCurve translationCurve;
        [Min(0f)]
        [Tooltip("윈도우 종료 전 위치 보정을 먼저 끝내는 시간입니다. 회전 보정은 유지됩니다.")]
        public float translationEndLeadTime = 0.06f;

        [Header("Playback Rate Warp")]
        [Tooltip("LegacyTargetCenter는 기존 TargetCenter 에셋의 속도 워프를 보존합니다. 일반 공격은 Disabled, 돌진·잡기는 Enabled를 명시합니다.")]
        [HideInInspector] public PlaybackRateWarpPolicy playbackRateWarpPolicy = PlaybackRateWarpPolicy.LegacyTargetCenter;
        [HideInInspector]
        public bool usePlaybackRateWarp;
        [HideInInspector] public Vector2 playbackRateRange = new(0.95f, 1.05f);

        [Header("Root Motion Amplify")]
        [Tooltip("루트모션 고유 속도 곡선을 게인으로 증폭한다 (타겟 워프와 직교).\n" +
                 "타겟 없이도 동작하며, 타겟이 있으면 증폭된 속도 위에서 워프가 합성됨.\n" +
                 "게인 커브: 접지 프레임 ≈1, 도약/런지 버스트 구간만 >1 로 두어 풋 슬라이딩 최소화.")]
        [HideInInspector] public bool amplifyEnabled = false;
        [Tooltip("정규화 워프 진행도 t(0~1) → 게인 배율. 비워두면 증폭 없음(=1).")]
        [HideInInspector] public AnimationCurve amplifyGainCurve;
        [Tooltip("증폭 결과 수평 속력의 자체 상한. 기존 워프 maxSpeed 와 분리.")]
        [HideInInspector] public float amplifyMaxSpeed = 25f;

        [Header("Baked Root Motion (에디터 베이크)")]
        [Tooltip("MotionSet 에디터의 'Bake Warp Root Motion' 으로 채워진다.\n" +
                 "이 윈도우 [startTime,endTime] 구간의 순수 애니메이션 루트 변위 총량(실제 액터 스케일 기준).\n" +
                 "리그·스케일·클립 출처가 일치할 때만 보정 예측에 사용합니다. 실제 Animator 델타를 대체하지 않습니다.")]
        public bool    bakedValid = false;
        public Vector3 bakedLocalTotal = Vector3.zero;  // facing-불변 로컬 수평 총 변위
        public float   bakedPathLen = 0f;               // 수평 경로 길이
        // 베이크 당시 윈도우 구간. 현재 start/end 와 다르면(=윈도우 시간 편집됨) 베이크는 stale → 무효.
        public float   bakedStartTime = 0f;
        public float   bakedEndTime = 0f;
        [HideInInspector] public int bakedFormatVersion;
        [HideInInspector] public bool bakedFromPlayMode;
        [HideInInspector] public Avatar bakedAvatar;
        [HideInInspector] public Vector3 bakedAnimatorScale = Vector3.one;
        [HideInInspector]
        public List<MotionWarpRootMotionBakeProfile> bakedProfiles = new();

        [NonSerialized]
        private Dictionary<int, MotionWarpHandle> _runtimeHandles;

        /// <summary>
        /// 베이크가 유효하고, 베이크 당시 구간이 현재 윈도우 구간과 일치하는가.
        /// 윈도우 시간을 편집한 뒤 재베이크를 잊은 경우 stale 시드를 런타임에 넘기지 않는다.
        /// </summary>
        private bool IsBakeTimingCurrent =>
            Mathf.Approximately(bakedStartTime, startTime)
            && Mathf.Approximately(bakedEndTime, endTime);

        private bool IsBakedUsable =>
            IsBakeTimingCurrent
            && (bakedValid || (bakedProfiles?.Count ?? 0) > 0);

        /// <summary>현재 윈도우와 리그 조건에 맞는 프로필 베이크를 찾는다.</summary>
        public bool TryGetBakedProfile(
            Avatar avatar,
            Vector3 animatorScale,
            out MotionWarpRootMotionBakeProfile profile)
        {
            if (!IsBakeTimingCurrent || bakedProfiles == null)
            {
                profile = null;
                return false;
            }

            for (int index = bakedProfiles.Count - 1; index >= 0; index--)
            {
                MotionWarpRootMotionBakeProfile candidate = bakedProfiles[index];
                if (candidate == null
                    || !candidate.IsValid
                    || candidate.formatVersion != CurrentBakeFormatVersion
                    || !candidate.HasTrajectory
                    || !candidate.Matches(avatar, animatorScale))
                    continue;
                profile = candidate;
                return true;
            }

            profile = null;
            return false;
        }

        /// <summary>
        /// 리그·스케일뿐 아니라 이 이벤트를 소유한 MotionSet의 현재 클립까지 일치하는, 런타임이 실제로 쓰는 프로필을 찾는다.
        /// 클립을 교체하거나 MotionSet을 복제하면 <see cref="TryGetBakedProfile"/>은 통과해도 이 조회는 실패한다.
        /// </summary>
        public bool TryGetRuntimeProfile(
            Avatar avatar,
            Vector3 animatorScale,
            UPlayGround.Animation.MotionSet ownerSet,
            out MotionWarpRootMotionBakeProfile profile)
        {
            profile = null;
            if (!IsBakeTimingCurrent || bakedProfiles == null)
                return false;

            string signature = MotionWarpTrajectory.BuildSourceSignature(ownerSet);
            for (int index = bakedProfiles.Count - 1; index >= 0; index--)
            {
                MotionWarpRootMotionBakeProfile candidate = bakedProfiles[index];
                if (candidate == null
                    || !candidate.IsValid
                    || !MotionWarpTrajectory.IsUsableAtRuntime(candidate, avatar, animatorScale, ownerSet, signature))
                    continue;
                profile = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 리그별 베이크를 갱신하며, 기존 단일 베이크는 최초 프로필 생성 전에 승격해 보존한다.
        /// </summary>
        public void RecordBakedProfile(
            Avatar avatar,
            Vector3 animatorScale,
            string sourceFingerprint,
            Vector3[] cumulativeLocalPositions,
            float[] cumulativePathLengths,
            float[] cumulativeYaw,
            bool fromPlayMode)
        {
            if (cumulativeLocalPositions == null
                || cumulativePathLengths == null
                || cumulativeYaw == null
                || cumulativeLocalPositions.Length != TrajectorySampleCount
                || cumulativePathLengths.Length != TrajectorySampleCount
                || cumulativeYaw.Length != TrajectorySampleCount)
            {
                throw new ArgumentException(
                    $"MotionWarp v3 trajectory는 {TrajectorySampleCount}개 누적 샘플이 필요합니다.");
            }

            RecordProfile(
                CurrentBakeFormatVersion,
                avatar,
                animatorScale,
                cumulativeLocalPositions[^1],
                cumulativePathLengths[^1],
                sourceFingerprint,
                cumulativeLocalPositions,
                cumulativePathLengths,
                cumulativeYaw,
                fromPlayMode);
        }

        /// <summary>수동 구형 계측기가 만든 총량을 v2 폴백 프로필로 기록한다.</summary>
        public void RecordTotalOnlyProfile(
            Avatar avatar,
            Vector3 animatorScale,
            Vector3 localTotal,
            float pathLen,
            bool fromPlayMode)
        {
            // 수동 구형 패널이 이미 검증된 v3 trajectory를 총량 프로필로 덮어쓰지 못하게 한다.
            if (TryGetBakedProfile(avatar, animatorScale, out _))
                return;
            RecordProfile(
                TotalOnlyBakeFormatVersion,
                avatar,
                animatorScale,
                localTotal,
                pathLen,
                string.Empty,
                null,
                null,
                null,
                fromPlayMode);
        }

        private void RecordProfile(
            int formatVersion,
            Avatar avatar,
            Vector3 animatorScale,
            Vector3 localTotal,
            float pathLen,
            string sourceFingerprint,
            Vector3[] cumulativeLocalPositions,
            float[] cumulativePathLengths,
            float[] cumulativeYaw,
            bool fromPlayMode)
        {
            if (!IsBakeTimingCurrent)
            {
                bakedValid = false;
                bakedProfiles?.Clear();
            }
            PromoteAttributedLegacyBake();
            bakedProfiles ??= new List<MotionWarpRootMotionBakeProfile>();

            MotionWarpRootMotionBakeProfile profile = null;
            for (int index = bakedProfiles.Count - 1; index >= 0; index--)
            {
                MotionWarpRootMotionBakeProfile candidate = bakedProfiles[index];
                if (candidate == null || !candidate.Matches(avatar, animatorScale))
                    continue;
                if (profile == null)
                    profile = candidate;
                else
                    bakedProfiles.RemoveAt(index);
            }

            profile ??= new MotionWarpRootMotionBakeProfile();
            if (!bakedProfiles.Contains(profile))
                bakedProfiles.Add(profile);

            profile.formatVersion = formatVersion;
            profile.fromPlayMode = fromPlayMode;
            profile.avatar = avatar;
            profile.animatorScale = animatorScale;
            profile.localTotal = localTotal;
            profile.pathLen = pathLen;
            profile.sourceFingerprint = sourceFingerprint ?? string.Empty;
            profile.cumulativeLocalPositions = Clone(cumulativeLocalPositions);
            profile.cumulativePathLengths = Clone(cumulativePathLengths);
            profile.cumulativeYaw = Clone(cumulativeYaw);
            if (fromPlayMode)
            {
                profile.playModeReferenceFormatVersion =
                    formatVersion;
                profile.playModeReferenceLocalTotal = localTotal;
                profile.playModeReferencePathLen = pathLen;
                profile.playModeReferenceCumulativeLocalPositions =
                    Clone(cumulativeLocalPositions);
                profile.playModeReferenceCumulativePathLengths =
                    Clone(cumulativePathLengths);
                profile.playModeReferenceCumulativeYaw = Clone(cumulativeYaw);
            }

            bool shouldMirrorSingleBake =
                !bakedValid
                || bakedFormatVersion <= 0
                || (bakedFormatVersion > 0
                    && bakedAvatar == avatar
                    && (bakedAnimatorScale - animatorScale).sqrMagnitude <= 0.0001f);
            if (shouldMirrorSingleBake)
                WriteSingleBakeMirror(profile);

            bakedStartTime = startTime;
            bakedEndTime = endTime;
        }

        private static Vector3[] Clone(Vector3[] values) =>
            values != null ? (Vector3[])values.Clone() : null;

        private static float[] Clone(float[] values) =>
            values != null ? (float[])values.Clone() : null;

        private void PromoteAttributedLegacyBake()
        {
            if (!bakedValid
                || bakedFormatVersion <= 0
                || bakedPathLen <= 0.0001f
                || !IsBakeTimingCurrent)
                return;

            bakedProfiles ??= new List<MotionWarpRootMotionBakeProfile>();
            for (int index = bakedProfiles.Count - 1; index >= 0; index--)
            {
                MotionWarpRootMotionBakeProfile candidate = bakedProfiles[index];
                if (candidate != null
                    && candidate.Matches(bakedAvatar, bakedAnimatorScale))
                    return;
            }

            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = bakedFormatVersion,
                fromPlayMode = bakedFromPlayMode,
                avatar = bakedAvatar,
                animatorScale = bakedAnimatorScale,
                localTotal = bakedLocalTotal,
                pathLen = bakedPathLen,
            };
            if (bakedFromPlayMode)
            {
                profile.playModeReferenceFormatVersion = bakedFormatVersion;
                profile.playModeReferenceLocalTotal = bakedLocalTotal;
                profile.playModeReferencePathLen = bakedPathLen;
            }
            bakedProfiles.Add(profile);
        }

        private void WriteSingleBakeMirror(
            MotionWarpRootMotionBakeProfile profile)
        {
            bakedLocalTotal = profile.localTotal;
            bakedPathLen = profile.pathLen;
            bakedValid = profile.IsValid;
            bakedFormatVersion = profile.formatVersion;
            bakedFromPlayMode = profile.fromPlayMode;
            bakedAvatar = profile.avatar;
            bakedAnimatorScale = profile.animatorScale;
        }

        public override string GetDisplayName() => "Motion Warp";
        public override string GetShortLabel()  => $"Warp:{modifierType}";

        /// <summary>저작 도구와 런타임이 동일한 프리셋 적용 결과를 사용한다.</summary>
        public MotionWarpWindowSettings ResolveSettings()
            => BuildSettings(endTime - startTime);

        public override void Execute(GameObject target)
        {
            if (!IsMotionWarpEnabled) return;

            string key = string.IsNullOrEmpty(targetKey) ? "primary" : targetKey;
            MotionWarpWindowSettings settings = ResolveSettings();

            var residualWarpTarget = target.GetComponent<IResidualMotionWarpTarget>()
                                  ?? target.GetComponentInParent<IResidualMotionWarpTarget>()
                                  ?? target.GetComponentInChildren<IResidualMotionWarpTarget>();
            if (residualWarpTarget != null)
            {
                MotionWarpHandle residualHandle = residualWarpTarget.BeginResidualMotionWarp(
                    settings,
                    key);
                RecordRuntimeHandle(target, residualHandle);
                return;
            }

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
            var controller = ResolveController(target);

            MotionWarpHandle handle = ConfigureMotionWarp(target, settings, key);
            RecordRuntimeHandle(target, handle);

            if (playerCombat != null)
            {
                playerCombat.BeginMotionWarp(handle);
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            enemyCombat?.BeginMotionWarp(handle);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (target == null || !TryTakeRuntimeHandle(target, out MotionWarpHandle handle))
                return;

            var residualWarpTarget = target.GetComponent<IResidualMotionWarpTarget>()
                                  ?? target.GetComponentInParent<IResidualMotionWarpTarget>()
                                  ?? target.GetComponentInChildren<IResidualMotionWarpTarget>();
            if (residualWarpTarget != null)
            {
                residualWarpTarget.EndResidualMotionWarp(handle);
                return;
            }

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.EndMotionWarp(handle);
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            if (enemyCombat != null)
            {
                enemyCombat.EndMotionWarp(handle);
                return;
            }

            ResolveController(target)?.MotionWarp.RequestEndWarpWindow(handle);
        }

        private MotionWarpHandle ConfigureMotionWarp(
            GameObject target,
            MotionWarpWindowSettings settings,
            string key)
        {
            var controller = ResolveController(target);
            if (controller == null || controller.MotionWarp == null)
                return default;

            return controller.MotionWarp.BeginWarpWindow(settings, key);
        }

        private void RecordRuntimeHandle(
            GameObject target,
            MotionWarpHandle handle)
        {
            if (target == null || !handle.IsValid)
                return;
            _runtimeHandles ??= new Dictionary<int, MotionWarpHandle>();
            _runtimeHandles[target.GetInstanceID()] = handle;
        }

        private bool TryTakeRuntimeHandle(
            GameObject target,
            out MotionWarpHandle handle)
        {
            handle = default;
            if (_runtimeHandles == null
                || !_runtimeHandles.TryGetValue(target.GetInstanceID(), out handle))
                return false;
            _runtimeHandles.Remove(target.GetInstanceID());
            return handle.IsValid;
        }

        private MotionWarpWindowSettings BuildSettings(float duration)
        {
            return new MotionWarpWindowSettings
            {
                duration = duration,
                preset = preset,
                modifierType = modifierType,
                targetPolicy = targetPolicy,
                translationWeight = translationWeight,
                rotationWeight = rotationWeight,
                ignoreY = ignoreY,
                yPolicy = yPolicy,
                overrideDistance = overrideDistance,
                minDistance = minDistance,
                maxDistance = maxDistance,
                maxSpeed = maxSpeed,
                targetOffset = targetOffset,
                arrivalMode = arrivalMode,
                desiredStandOff = desiredStandOff,
                localArrivalOffset = localArrivalOffset,
                minimumSourceDisplacement = minimumSourceDisplacement,
                minimumNetToPathRatio = minimumNetToPathRatio,
                maxHeightDifference = maxHeightDifference,
                obstacleLayers = obstacleLayers,
                noTranslationWithinReach = noTranslationWithinReach,
                maxCorrectionDistance = maxCorrectionDistance,
                maxCorrectionRatio = maxCorrectionRatio,
                correctionLimitPolicy = correctionLimitPolicy,
                correctionTiming = correctionTiming,
                correctionTimingCurve = correctionTimingCurve,
                maxWarpAngle = maxWarpAngle,
                translationCurve = translationCurve,
                translationEndLeadTime = translationEndLeadTime,
                usePlaybackRateWarp = ResolvePlaybackRateWarp(),
                playbackRateRange = playbackRateRange,
                rotationCurve = rotationCurve,
                predictionFactor = predictionFactor,
                amplifyEnabled = amplifyEnabled,
                amplifyGainCurve = amplifyGainCurve,
                amplifyMaxSpeed = amplifyMaxSpeed,
                windowStartTime = startTime + globalStartTimeOffset,
                windowEndTime = endTime + globalStartTimeOffset,
                bakedValid = IsBakedUsable,   // stale(구간 편집) 베이크는 무효 처리
                bakedLocalTotal = bakedLocalTotal,
                bakedPathLen = bakedPathLen,
                bakedFormatVersion = bakedFormatVersion,
                bakedAvatar = bakedAvatar,
                bakedAnimatorScale = bakedAnimatorScale,
                bakedProfiles = IsBakedUsable ? bakedProfiles : null,
            };
        }

        /// <summary>
        /// 기존 TargetCenter 에셋은 종전 동작을 보존하되, 신규 저작 데이터는
        /// 정책으로 재생 속도 워프를 명시적으로 켜고 끌 수 있다.
        /// </summary>
        public bool ResolvePlaybackRateWarp()
        {
            return MotionWarpMath.ResolvePlaybackRateWarp(
                playbackRateWarpPolicy,
                arrivalMode,
                usePlaybackRateWarp);
        }

        private static ActorMovementController ResolveController(GameObject target)
        {
            return target.GetComponent<ActorMovementController>()
                ?? target.GetComponentInParent<ActorMovementController>()
                ?? target.GetComponentInChildren<ActorMovementController>();
        }

#if UNITY_EDITOR
        /// <summary>이관 도구에서만 종전 실행 설정을 복원한다. 런타임은 저장값만 읽는다.</summary>
        public MotionWarpWindowSettings ResolveLegacySettings()
            => ApplyPreset(BuildSettings(endTime - startTime));

        private static MotionWarpWindowSettings ApplyPreset(MotionWarpWindowSettings settings)
        {
            switch (settings.preset)
            {
                case MotionWarpPreset.LightAttack:
                    settings.modifierType = MotionWarpModifierType.DeltaWarp;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.yPolicy = WarpYPolicy.IgnoreY;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.25f;
                    settings.maxDistance = 7f;
                    settings.maxSpeed = 22f;
                    if (settings.arrivalMode == WarpArrivalMode.ContactShell)
                    {
                        settings.desiredStandOff = 0.18f;
                        settings.noTranslationWithinReach = 0.12f;
                        settings.maxCorrectionDistance = 0.5f;
                        settings.maxCorrectionRatio = 0.3f;
                        settings.maxWarpAngle = 45f;
                        settings.translationEndLeadTime = 0.06f;
                        settings.maxDistance = 2.5f;
                        settings.maxSpeed = 18f;
                    }
                    if (HasCurve(settings.rotationCurve) == false)
                        settings.rotationCurve = BuildLightCurve();
                    break;
                case MotionWarpPreset.HeavyAttack:
                    settings.modifierType = MotionWarpModifierType.DeltaWarp;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    // DeltaWarp의 잔여 보정을 온전히 적용하도록 translationWeight를 1.0으로 고정한다.
                    // 무게감은 rotationCurve(BuildHeavyCurve)/게인으로 표현.
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.yPolicy = WarpYPolicy.IgnoreY;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.35f;
                    settings.maxDistance = 8f;
                    settings.maxSpeed = 20f;
                    if (settings.arrivalMode == WarpArrivalMode.ContactShell)
                    {
                        settings.desiredStandOff = 0.24f;
                        settings.noTranslationWithinReach = 0.15f;
                        settings.maxCorrectionDistance = 0.8f;
                        settings.maxCorrectionRatio = 0.4f;
                        settings.maxWarpAngle = 35f;
                        settings.translationEndLeadTime = 0.1f;
                        settings.maxDistance = 3f;
                        settings.maxSpeed = 16f;
                    }
                    if (HasCurve(settings.rotationCurve) == false)
                        settings.rotationCurve = BuildHeavyCurve();
                    break;
                case MotionWarpPreset.FinishAttack:
                    settings.modifierType = MotionWarpModifierType.DeltaWarp;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.yPolicy = WarpYPolicy.IgnoreY;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.1f;
                    settings.maxDistance = 5f;
                    settings.maxSpeed = 16f;
                    if (HasCurve(settings.rotationCurve) == false)
                        settings.rotationCurve = BuildFinishCurve();
                    break;
                case MotionWarpPreset.Grab:
                    settings.modifierType = MotionWarpModifierType.DeltaWarp;
                    // Grab 은 움직이는 타겟 잡기 — Predictive 로 승격해 떨림 감소 + 도달 정확도 개선.
                    settings.targetPolicy = MotionWarpTargetPolicy.Predictive;
                    settings.predictionFactor = 0.6f;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.yPolicy = WarpYPolicy.IgnoreY;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.05f;
                    settings.maxDistance = 3f;
                    settings.maxSpeed = 12f;
                    if (HasCurve(settings.rotationCurve) == false)
                        settings.rotationCurve = BuildLightCurve();
                    break;
            }

            return settings;
        }

        private static bool HasCurve(AnimationCurve c) => c != null && c.length > 0;

        // 프리셋 곡선은 읽기 전용으로만 사용되므로 static 인스턴스 공유.
        // 누군가 외부에서 AddKey/RemoveKey 로 변형하면 공유 인스턴스가 오염될 수 있어 주의.
        private static readonly AnimationCurve LightCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.5f, 0.85f, 1f, 1f),
            new Keyframe(1f, 1f, 0f, 0f));

        private static readonly AnimationCurve HeavyCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.5f),
            new Keyframe(0.5f, 0.5f, 1.5f, 1.5f),
            new Keyframe(1f, 1f, 0.5f, 0f));

        private static readonly AnimationCurve FinishCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.5f),
            new Keyframe(0.7f, 0.7f, 1f, 1f),
            new Keyframe(1f, 1f, 1f, 0f));

        // 빠른 EaseOut: 앞부분에서 강하게 정렬 (LightAttack, Grab).
        private static AnimationCurve BuildLightCurve()  => LightCurve;
        // 느린 EaseIn-Out: 무게감 (HeavyAttack).
        private static AnimationCurve BuildHeavyCurve()  => HeavyCurve;
        // 마지막 프레임에 거의 정확히 일치 (FinishAttack).
        private static AnimationCurve BuildFinishCurve() => FinishCurve;
#endif
    }
}
