using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Projectile
{
    public enum ProjectileArcFlightTimeMode
    {
        Speed,
        Fixed,
    }

    public enum ProjectileSplitTrigger
    {
        Hit,
        Expire,
        HitOrExpire,
    }

    public interface IProjectileMotion { }
    public interface IProjectileBehavior { }

    [Serializable]
    public abstract class ProjectileMotionData : IProjectileMotion { }

    [Serializable]
    public sealed class LinearProjectileMotion : ProjectileMotionData
    {
        [Min(0f)] public float speed = 20f;
        public float acceleration;
        [Min(0f)] public float maxSpeed = 50f;
        public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    }

    [Serializable]
    public sealed class ArcProjectileMotion : ProjectileMotionData
    {
        [Min(0f)] public float speed = 15f;
        [Min(0f)] public float arcHeight = 5f;
        public ProjectileArcFlightTimeMode flightTimeMode = ProjectileArcFlightTimeMode.Speed;
        [Min(0.01f)] public float fixedFlightTime = 1f;
        public AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    [Serializable]
    public sealed class HomingProjectileMotion : ProjectileMotionData
    {
        [Min(0f)] public float speed = 15f;
        [Min(0f)] public float turnRate = 180f;
        [Min(0f)] public float activationDelay;
        [Range(0f, 180f)] public float maxTrackAngle = 120f;
        public AnimationCurve strengthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    }

    [Serializable]
    public sealed class StationaryProjectileMotion : ProjectileMotionData
    {
        public bool attachToGround;
        public LayerMask groundLayers = -1;
        [Min(0f)] public float groundProbeHeight = 10f;
        [Min(0.01f)] public float groundProbeDistance = 20f;
    }

    [Serializable]
    public sealed class OrbitProjectileMotion : ProjectileMotionData
    {
        [Min(0f)] public float radius = 3f;
        public float angularSpeed = 120f;
    }

    [Serializable]
    public sealed class HitscanProjectileMotion : ProjectileMotionData
    {
        [Min(0f)] public float range = 50f;
        [Min(0f)] public float visualDuration = 0.08f;
    }

    [Serializable]
    public abstract class ProjectileBehaviorData : IProjectileBehavior { }

    [Serializable]
    public sealed class PierceProjectileBehavior : ProjectileBehaviorData
    {
        [Min(0)] public int maxPierce = 1;
        [Range(0f, 1f)] public float damageMultiplierPerPierce = 0.85f;
    }

    [Serializable]
    public sealed class BounceProjectileBehavior : ProjectileBehaviorData
    {
        [Min(0)] public int maxBounce = 1;
        public LayerMask surfaceLayers = -1;
        [Range(0f, 1f)] public float speedRetention = 1f;
    }

    [Serializable]
    public sealed class SplitProjectileBehavior : ProjectileBehaviorData
    {
        public ProjectileDefinitionSO childDefinition;
        public ProjectileSplitTrigger trigger = ProjectileSplitTrigger.Hit;
        [Min(1)] public int count = 3;
        [Range(0f, 360f)] public float spreadAngle = 45f;
        [Range(0f, 2f)] public float damageScale = 0.7f;
    }

    [Serializable]
    public sealed class DetonateProjectileBehavior : ProjectileBehaviorData
    {
        [Min(0f)] public float radius = 3f;
        public bool onHit = true;
        public bool onExpire = true;
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
    }

    [Serializable]
    public sealed class AreaTickProjectileBehavior : ProjectileBehaviorData
    {
        [Min(0f)] public float radius = 3f;
        [Min(0.01f)] public float interval = 1f;
        [Min(0f)] public float activationDelay;
        [Tooltip("활성화 시 한 번만 피해를 적용합니다. 레거시 즉시 폭발 호환에 사용합니다.")]
        public bool applyOnce;
        public bool expandOverTime;
        [Min(0f)] public float expansionSpeed = 10f;
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
    }

    [Serializable]
    public sealed class AttachProjectileBehavior : ProjectileBehaviorData
    {
        public bool attachToTarget = true;
        [Min(0f)] public float attachedLifetime = 1f;
    }

    [Serializable]
    public sealed class ReflectableProjectileBehavior : ProjectileBehaviorData
    {
        [Min(0f)] public float reflectedSpeedMultiplier = 1.25f;
    }

    [CreateAssetMenu(
        fileName = "ProjectileDefinition",
        menuName = "UPlayGround/Projectile/Projectile Definition")]
    public sealed class ProjectileDefinitionSO : ScriptableObject
    {
        [Header("Visual")]
        public GameObject visualPrefab;
        public string hitEffectKey;
        public bool detachTrailOnReturn = true;

        [Header("Simulation")]
        [SerializeReference] public ProjectileMotionData motion = new LinearProjectileMotion();
        [SerializeReference] public List<ProjectileBehaviorData> behaviors = new();
        [Min(0.01f)] public float lifetime = 5f;
        [Min(0.01f)] public float collisionRadius = 0.25f;
        public bool destroyOnHit = true;
        public bool inheritOwnerTimeScale = true;

        [Header("Pool")]
        [Min(0)] public int prewarmCount = 4;
        [Min(1)] public int maxPoolSize = 64;
        [Min(0)] public int maxGeneration = 2;

        public T GetBehavior<T>() where T : ProjectileBehaviorData
        {
            if (behaviors == null)
                return null;
            for (int i = 0; i < behaviors.Count; i++)
                if (behaviors[i] is T typed)
                    return typed;
            return null;
        }

        public void CollectValidationErrors(List<string> errors)
        {
            if (errors == null)
                return;
            if (visualPrefab == null)
                errors.Add($"{name}: visualPrefab이 없습니다.");
            if (motion == null)
                errors.Add($"{name}: motion이 없습니다.");
            if (prewarmCount > maxPoolSize)
                errors.Add($"{name}: prewarmCount가 maxPoolSize보다 큽니다.");
            if (motion is HitscanProjectileMotion && GetBehavior<BounceProjectileBehavior>() != null)
                errors.Add($"{name}: HitscanMotion과 BounceBehavior는 함께 사용할 수 없습니다.");
            if (motion is StationaryProjectileMotion
                && (GetBehavior<BounceProjectileBehavior>() != null
                    || GetBehavior<PierceProjectileBehavior>() != null))
                errors.Add($"{name}: StationaryMotion에는 Bounce/PierceBehavior를 사용할 수 없습니다.");

            var behaviorTypes = new HashSet<Type>();
            if (behaviors != null)
            {
                for (int i = 0; i < behaviors.Count; i++)
                {
                    ProjectileBehaviorData behavior = behaviors[i];
                    if (behavior == null)
                    {
                        errors.Add($"{name}: behaviors[{i}]가 비어 있습니다.");
                        continue;
                    }
                    if (!behaviorTypes.Add(behavior.GetType()))
                        errors.Add($"{name}: {behavior.GetType().Name}이 중복되었습니다.");
                }
            }

            SplitProjectileBehavior split = GetBehavior<SplitProjectileBehavior>();
            if (split != null && split.childDefinition == null)
                errors.Add($"{name}: SplitBehavior의 childDefinition이 없습니다.");
            else if (split != null && split.childDefinition == this)
                errors.Add($"{name}: SplitBehavior가 자기 자신을 childDefinition으로 참조합니다.");
        }
    }
}
