using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// Motion Warp 활성 구간 이벤트.
    /// startTime ~ endTime 구간 동안 IsMotionWarping = true.
    /// Execute 시 이벤트 구간 길이(endTime - startTime)를 Combat에 전달해
    /// AttackState가 정확한 남은 시간 기반으로 속력을 역산한다.
    /// 플레이어(PlayerCombat)와 몬스터(EnemyCombat) 모두 지원.
    /// </summary>
    [Serializable]
    public class MotionEvent_MotionWarp : MotionEventBase
    {
        // 워프 전역 토글은 SettingsManager.Data.debugMotionWarpEnabled 로 위임.
        // SettingsManager 미로드/Data null 인 초기 프레임에는 활성 기본값.
        private static bool IsMotionWarpEnabled
        {
            get
            {
                var sm = SettingsManager.Instance;
                if (sm == null || !sm.IsLoaded || sm.Data == null) return true;
                return sm.Data.debugMotionWarpEnabled;
            }
        }

        [Header("Warp Modifier")]
        public MotionWarpPreset preset = MotionWarpPreset.Custom;
        public MotionWarpModifierType modifierType = MotionWarpModifierType.DeltaWarp;
        public MotionWarpTargetPolicy targetPolicy = MotionWarpTargetPolicy.Snapshot;

        [Header("Target Resolver")]
        [Tooltip("UseExisting: AttackState 가 미리 설정한 타겟 그대로 사용 (기존 호환).\n" +
                 "ConeNearest / LockOnFirst / Hybrid: 이벤트 발화 시점에 재결정.\n" +
                 "Hybrid 권장: 락온이 콘 안이면 락온, 밖이면 콘 최근접.")]
        public WarpResolverPolicy resolverPolicy = WarpResolverPolicy.UseExisting;

        [Header("Multi-Target Key")]
        [Tooltip("같은 키를 가진 두 이벤트는 같은 타겟을 공유 (도약-착지 등 다단 모션).\n" +
                 "다른 키를 쓰면 별도 타겟. 비워두면 \"primary\" 기본 키 사용.")]
        public string targetKey = "primary";

        [Header("Predictive Live")]
        [Tooltip("targetPolicy 가 Predictive 일 때만 사용.\n" +
                 "타겟 추정 속도 × 이 비율 × 남은 워프 시간 만큼 미래 위치 가산. 0 = Live 동등.")]
        [Range(0f, 1f)]
        public float predictionFactor = 0.5f;

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

        [Header("Root Motion Amplify")]
        [Tooltip("루트모션 고유 속도 곡선을 게인으로 증폭한다 (타겟 워프와 직교).\n" +
                 "타겟 없이도 동작하며, 타겟이 있으면 증폭된 속도 위에서 워프가 합성됨.\n" +
                 "게인 커브: 접지 프레임 ≈1, 도약/런지 버스트 구간만 >1 로 두어 풋 슬라이딩 최소화.")]
        public bool amplifyEnabled = false;
        [Tooltip("정규화 워프 진행도 t(0~1) → 게인 배율. 비워두면 증폭 없음(=1).")]
        public AnimationCurve amplifyGainCurve;
        [Tooltip("증폭 결과 수평 속력의 자체 상한. 기존 워프 maxSpeed 와 분리.")]
        public float amplifyMaxSpeed = 25f;

        public override string GetDisplayName() => "Motion Warp";
        public override string GetShortLabel()  => $"Warp:{modifierType}";

        public override void Execute(GameObject target)
        {
            if (!IsMotionWarpEnabled) return;

            float warpDuration = endTime - startTime;
            string key = string.IsNullOrEmpty(targetKey) ? "primary" : targetKey;

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();

            // resolverPolicy != UseExisting 인 경우 이벤트 발화 시점에 타겟 재결정.
            // 현재 PlayerCombat 경로에서만 컨텍스트를 구성한다 (EnemyCombat 은 Phase 후속에서 확장).
            if (resolverPolicy != WarpResolverPolicy.UseExisting && playerCombat != null)
            {
                IWarpTargetResolver resolver = WarpTargetResolverFactory.For(resolverPolicy);
                if (resolver != null)
                {
                    WarpResolverContext ctx = playerCombat.BuildWarpResolverContext();
                    if (ctx.origin != null)
                    {
                        Transform resolved = resolver.Resolve(in ctx);
                        var controller = ResolveController(target);
                        if (resolved != null && controller != null)
                        {
                            bool useSnapshot = targetPolicy == MotionWarpTargetPolicy.Snapshot;
                            controller.MotionWarp.SetTarget(key, resolved, useSnapshot);
                        }
                    }
                }
            }

            ConfigureMotionWarp(target, warpDuration, key);

            if (playerCombat != null)
            {
                playerCombat.BeginMotionWarp(warpDuration);
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            enemyCombat?.BeginMotionWarp(warpDuration);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (!IsMotionWarpEnabled) return;

            ResolveController(target)?.MotionWarp.EndWarpWindow();

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.EndMotionWarp();
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            enemyCombat?.EndMotionWarp();
        }

        private void ConfigureMotionWarp(GameObject target, float duration, string key)
        {
            var controller = ResolveController(target);
            if (controller == null || controller.MotionWarp == null) return;

            MotionWarpWindowSettings settings = new MotionWarpWindowSettings
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
                rotationCurve = rotationCurve,
                predictionFactor = predictionFactor,
                amplifyEnabled = amplifyEnabled,
                amplifyGainCurve = amplifyGainCurve,
                amplifyMaxSpeed = amplifyMaxSpeed,
                windowStartTime = startTime,
                windowEndTime = endTime,
            };

            controller.MotionWarp.BeginWarpWindow(ApplyPreset(settings), key);
        }

        private static ActorMovementController ResolveController(GameObject target)
        {
            return target.GetComponent<ActorMovementController>()
                ?? target.GetComponentInParent<ActorMovementController>()
                ?? target.GetComponentInChildren<ActorMovementController>();
        }

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
                    if (HasCurve(settings.rotationCurve) == false)
                        settings.rotationCurve = BuildLightCurve();
                    break;
                case MotionWarpPreset.HeavyAttack:
                    settings.modifierType = MotionWarpModifierType.DeltaWarp;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    // DeltaWarp 는 보정이 translationWeight 로 게이팅되므로 정확 착지를 위해 1.0.
                    // 무게감은 rotationCurve(BuildHeavyCurve)/게인으로 표현.
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.yPolicy = WarpYPolicy.IgnoreY;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.35f;
                    settings.maxDistance = 8f;
                    settings.maxSpeed = 20f;
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
    }
}
