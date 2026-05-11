using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// 워프 타겟 결정 정책.
    /// UseExisting 은 기존(AttackState 선결) 타겟을 그대로 둔다.
    /// </summary>
    public enum WarpResolverPolicy
    {
        UseExisting = 0,
        ConeNearest,
        LockOnFirst,
        Hybrid,
    }

    /// <summary>
    /// resolver 호출 시 필요한 컨텍스트.
    /// origin 은 공격자, hitRange/hitAngle 은 콘 판정 기준,
    /// targetLayer 는 OverlapSphere 마스크, targetFilter 는 추가 검증(옵션).
    /// </summary>
    public struct WarpResolverContext
    {
        public Transform origin;
        public float hitRange;
        public float hitAngle;
        public LayerMask targetLayer;
        public Func<Transform, bool> targetFilter;
    }

    public interface IWarpTargetResolver
    {
        Transform Resolve(in WarpResolverContext ctx);
    }

    public static class WarpTargetResolverFactory
    {
        public static IWarpTargetResolver For(WarpResolverPolicy policy) => policy switch
        {
            WarpResolverPolicy.ConeNearest => ConeNearestResolver.Instance,
            WarpResolverPolicy.LockOnFirst => LockOnFirstResolver.Instance,
            WarpResolverPolicy.Hybrid      => HybridResolver.Instance,
            _                              => null,
        };
    }

    /// <summary>
    /// 콘(hitRange/hitAngle) 안 최근접 타겟. PlayerCombat.FindAttackSnapTarget 의 일반화.
    /// </summary>
    public sealed class ConeNearestResolver : IWarpTargetResolver
    {
        public static readonly ConeNearestResolver Instance = new ConeNearestResolver();

        // 0-할당 OverlapSphere 용 공유 버퍼. 64 는 보통 격투에서 충분한 상한.
        // 초과 시 잘림이 가능하지만 콘 안 최근접만 고르므로 정확도 열화 가능성은 낮음.
        private static readonly Collider[] _hitsBuffer = new Collider[64];

        public Transform Resolve(in WarpResolverContext ctx)
        {
            if (ctx.origin == null) return null;

            Vector3 originPos = ctx.origin.position;
            Vector3 forward   = ctx.origin.forward;

            int hitCount = Physics.OverlapSphereNonAlloc(originPos, ctx.hitRange, _hitsBuffer, ctx.targetLayer);
            Transform best   = null;
            float     bestSq = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitsBuffer[i];
                if (hit == null) continue;
                if (hit.transform == ctx.origin || hit.transform.IsChildOf(ctx.origin)) continue;

                Vector3 dir = hit.transform.position - originPos;
                dir.y = 0f;
                if (Vector3.Angle(forward, dir) > ctx.hitAngle) continue;

                if (ctx.targetFilter != null && !ctx.targetFilter(hit.transform)) continue;

                float sq = dir.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best   = hit.transform;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// 락온 타겟만 사용. 락온이 없거나 자기 자신/필터 거부면 null.
    /// HybridResolver 와 시맨틱 일치.
    /// </summary>
    public sealed class LockOnFirstResolver : IWarpTargetResolver
    {
        public static readonly LockOnFirstResolver Instance = new LockOnFirstResolver();

        public Transform Resolve(in WarpResolverContext ctx)
        {
            if (CameraManager.Instance == null) return null;
            Transform lockOn = CameraManager.Instance.GetLockOnTarget();
            if (lockOn == null) return null;
            if (ctx.origin != null && lockOn == ctx.origin) return null;
            if (ctx.targetFilter != null && !ctx.targetFilter(lockOn)) return null;
            return lockOn;
        }
    }

    /// <summary>
    /// 락온 타겟이 콘(hitRange/hitAngle) 안에 있으면 락온 우선,
    /// 밖이면 ConeNearestResolver fallback. (1차 결정 정책)
    /// </summary>
    public sealed class HybridResolver : IWarpTargetResolver
    {
        public static readonly HybridResolver Instance = new HybridResolver();

        public Transform Resolve(in WarpResolverContext ctx)
        {
            Transform lockOn = CameraManager.Instance != null
                ? CameraManager.Instance.GetLockOnTarget()
                : null;

            if (lockOn != null && ctx.origin != null && lockOn != ctx.origin)
            {
                Vector3 dir = lockOn.position - ctx.origin.position;
                dir.y = 0f;
                bool inRange = dir.sqrMagnitude <= ctx.hitRange * ctx.hitRange;
                bool inAngle = Vector3.Angle(ctx.origin.forward, dir) <= ctx.hitAngle;
                if (inRange && inAngle)
                {
                    if (ctx.targetFilter == null || ctx.targetFilter(lockOn))
                        return lockOn;
                }
            }

            return ConeNearestResolver.Instance.Resolve(in ctx);
        }
    }
}
