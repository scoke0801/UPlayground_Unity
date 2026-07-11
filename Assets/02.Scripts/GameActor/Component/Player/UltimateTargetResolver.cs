using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.Components
{
    public static class UltimateTargetResolver
    {
        private static readonly Collider[] OverlapBuffer = new Collider[128];

        public static bool TryResolve(
            PlayerActor caster,
            UltimateTargetPolicy policy,
            Transform manualTarget,
            UltimateRuntimeContext context,
            out string error)
        {
            context.Targets.Clear();
            context.PrimaryTarget = null;

            if (policy == null || policy.mode == UltimateTargetMode.None)
            {
                error = string.Empty;
                return policy == null || !policy.requireTarget;
            }

            Transform preferred = policy.mode switch
            {
                UltimateTargetMode.CurrentLockOn => CameraManager.Instance?.GetLockOnTarget(),
                UltimateTargetMode.ManualTransform => manualTarget,
                _ => null
            };

            if (IsValidTarget(preferred))
                context.Targets.Add(NormalizeTarget(preferred));

            if (policy.mode is UltimateTargetMode.NearestEnemy or UltimateTargetMode.ForwardCone
                || (policy.mode == UltimateTargetMode.CurrentLockOn && context.Targets.Count == 0))
            {
                CollectCandidates(caster, policy, context.Targets);
            }

            context.Targets.RemoveAll(target => !IsValidTarget(target));
            context.Targets.Sort((a, b) =>
            {
                float aDistance = (a.position - caster.transform.position).sqrMagnitude;
                float bDistance = (b.position - caster.transform.position).sqrMagnitude;
                return aDistance.CompareTo(bDistance);
            });

            int maxTargets = policy.includeMultipleTargets
                ? Mathf.Max(1, policy.maxTargets)
                : 1;
            if (context.Targets.Count > maxTargets)
                context.Targets.RemoveRange(maxTargets, context.Targets.Count - maxTargets);

            context.PrimaryTarget = context.Targets.Count > 0
                ? context.Targets[0]
                : null;

            if (context.PrimaryTarget == null && policy.requireTarget)
            {
                error = "궁극기 실행에 필요한 타겟을 찾지 못했습니다.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static void CollectCandidates(
            PlayerActor caster,
            UltimateTargetPolicy policy,
            List<Transform> result)
        {
            if (caster == null || policy.searchRadius <= 0f)
                return;

            int count = Physics.OverlapSphereNonAlloc(
                caster.transform.position,
                policy.searchRadius,
                OverlapBuffer,
                policy.targetLayer);

            Vector3 forward = caster.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            float halfAngle = policy.coneAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Collider hit = OverlapBuffer[i];
                if (hit == null)
                    continue;

                Transform target = NormalizeTarget(hit.transform);
                if (!IsValidTarget(target) || result.Contains(target))
                    continue;

                if (policy.mode == UltimateTargetMode.ForwardCone)
                {
                    Vector3 direction = target.position - caster.transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude <= 0.0001f
                        || Vector3.Angle(forward, direction.normalized) > halfAngle)
                    {
                        continue;
                    }
                }

                result.Add(target);
            }
        }

        private static Transform NormalizeTarget(Transform target)
        {
            if (target == null)
                return null;

            MonsterActor monster = target.GetComponent<MonsterActor>()
                                   ?? target.GetComponentInParent<MonsterActor>();
            return monster != null ? monster.transform : target;
        }

        private static bool IsValidTarget(Transform target)
        {
            if (target == null)
                return false;

            MonsterActor monster = target.GetComponent<MonsterActor>()
                                   ?? target.GetComponentInParent<MonsterActor>();
            return monster != null && monster.IsAlive();
        }
    }
}
