using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Projectile
{
    public readonly struct ProjectilePatternShot
    {
        public readonly Vector3 Direction;
        public readonly float Delay;

        public ProjectilePatternShot(Vector3 direction, float delay)
        {
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Delay = Mathf.Max(0f, delay);
        }
    }

    [Serializable]
    public abstract class ProjectileSpawnPattern
    {
        public abstract void Build(Vector3 forward, List<ProjectilePatternShot> shots);
    }

    [Serializable]
    public sealed class SingleShotPattern : ProjectileSpawnPattern
    {
        public override void Build(Vector3 forward, List<ProjectilePatternShot> shots) =>
            shots.Add(new ProjectilePatternShot(forward, 0f));
    }

    [Serializable]
    public sealed class FanShotPattern : ProjectileSpawnPattern
    {
        [Min(1)] public int count = 3;
        [Range(0f, 180f)] public float spreadAngle = 30f;

        public override void Build(Vector3 forward, List<ProjectilePatternShot> shots)
        {
            int shotCount = Mathf.Max(1, count);
            for (int i = 0; i < shotCount; i++)
            {
                float ratio = shotCount == 1 ? 0.5f : i / (float)(shotCount - 1);
                float yaw = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, ratio);
                shots.Add(new ProjectilePatternShot(Quaternion.AngleAxis(yaw, Vector3.up) * forward, 0f));
            }
        }
    }

    [Serializable]
    public sealed class RingShotPattern : ProjectileSpawnPattern
    {
        [Min(1)] public int count = 8;

        public override void Build(Vector3 forward, List<ProjectilePatternShot> shots)
        {
            int shotCount = Mathf.Max(1, count);
            for (int i = 0; i < shotCount; i++)
                shots.Add(new ProjectilePatternShot(
                    Quaternion.AngleAxis(360f * i / shotCount, Vector3.up) * forward, 0f));
        }
    }

    [Serializable]
    public sealed class BurstShotPattern : ProjectileSpawnPattern
    {
        [Min(1)] public int count = 3;
        [Min(0f)] public float interval = 0.1f;

        public override void Build(Vector3 forward, List<ProjectilePatternShot> shots)
        {
            int shotCount = Mathf.Max(1, count);
            for (int i = 0; i < shotCount; i++)
                shots.Add(new ProjectilePatternShot(forward, interval * i));
        }
    }

    [Serializable]
    public sealed class MultiTargetShotPattern : ProjectileSpawnPattern
    {
        [Min(1)] public int maxTargets = 4;
        public override void Build(Vector3 forward, List<ProjectilePatternShot> shots) =>
            shots.Add(new ProjectilePatternShot(forward, 0f));
    }

    public struct ProjectileSpawnRequest
    {
        public ProjectileDefinitionSO definition;
        public GameObject owner;
        public Vector3 origin;
        public Vector3 logicalOrigin;
        public float barrelBlendTime;
        public Vector3 direction;
        public bool hasTargetPosition;
        public Vector3 targetPosition;
        public Transform targetTransform;
        public Transform orbitCenter;
        public int hitPhaseIndex;
        public LayerMask hitLayers;
        public float damageScale;
        public int generation;
        public float delay;
        public float legacyDamage;
        public float speedOverride;
        public float durationOverride;
        public string hitEffectOverride;
    }
}
