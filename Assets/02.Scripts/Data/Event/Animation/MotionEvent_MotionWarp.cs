using System;
using UnityEngine;
using UPlayGround.Component;

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
        // 모션 워핑 기능 개선 작업 중 — 임시 전역 비활성화 토글.
        // true 로 되돌리면 기존 동작 복구.
        private const bool MotionWarpEnabled = false;

        [Header("Warp Modifier")]
        public MotionWarpPreset preset = MotionWarpPreset.Custom;
        public MotionWarpModifierType modifierType = MotionWarpModifierType.Additive;
        public MotionWarpTargetPolicy targetPolicy = MotionWarpTargetPolicy.Snapshot;

        [Range(0f, 1f)]
        public float translationWeight = 1f;
        [Range(0f, 1f)]
        public float rotationWeight = 1f;
        public bool ignoreY = true;

        [Header("Override Range")]
        public bool overrideDistance = false;
        public float minDistance = 0.3f;
        public float maxDistance = 4f;
        public float maxSpeed = 18f;

        [Header("Offset")]
        public Vector3 targetOffset = Vector3.zero;

        public override string GetDisplayName() => "Motion Warp";
        public override string GetShortLabel()  => $"Warp:{modifierType}";

        public override void Execute(GameObject target)
        {
            if (!MotionWarpEnabled) return;

            float warpDuration = endTime - startTime;
            ConfigureMotionWarp(target, warpDuration);

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
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
            if (!MotionWarpEnabled) return;

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

        private void ConfigureMotionWarp(GameObject target, float duration)
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
                overrideDistance = overrideDistance,
                minDistance = minDistance,
                maxDistance = maxDistance,
                maxSpeed = maxSpeed,
                targetOffset = targetOffset
            };

            controller.MotionWarp.BeginWarpWindow(ApplyPreset(settings));
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
                    settings.modifierType = MotionWarpModifierType.Additive;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.25f;
                    settings.maxDistance = 4f;
                    settings.maxSpeed = 18f;
                    break;
                case MotionWarpPreset.HeavyAttack:
                    settings.modifierType = MotionWarpModifierType.Scale;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    settings.translationWeight = 0.9f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.35f;
                    settings.maxDistance = 5f;
                    settings.maxSpeed = 16f;
                    break;
                case MotionWarpPreset.FinishAttack:
                    settings.modifierType = MotionWarpModifierType.Skew;
                    settings.targetPolicy = MotionWarpTargetPolicy.Snapshot;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.1f;
                    settings.maxDistance = 3f;
                    settings.maxSpeed = 12f;
                    break;
                case MotionWarpPreset.Grab:
                    settings.modifierType = MotionWarpModifierType.Skew;
                    settings.targetPolicy = MotionWarpTargetPolicy.Live;
                    settings.translationWeight = 1f;
                    settings.rotationWeight = 1f;
                    settings.ignoreY = true;
                    settings.overrideDistance = true;
                    settings.minDistance = 0.05f;
                    settings.maxDistance = 2f;
                    settings.maxSpeed = 10f;
                    break;
            }

            return settings;
        }
    }
}
