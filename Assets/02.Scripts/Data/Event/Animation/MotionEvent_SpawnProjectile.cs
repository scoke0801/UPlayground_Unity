using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    public enum ProjectileTargetMode
    {
        Forward,
        LockOnTarget,
        EnemySkillTarget,
        TargetPosition,
        TelegraphPosition
    }

    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    public class SpawnProjectileEvent : MotionEventBase
    {
        [Header("Projectile Setting")]
        public BaseProjectile projectilePrefab;
        public string spawnPointName;             // 스폰 기준 본/트랜스폼 이름
        public Vector3 spawnOffset;               // 위치 보정 (로컬)
        public Vector3 rotationOffset;            // 방향 보정 오일러 각도
        public bool useSpawnRotation = true;      // 스폰 포인트 회전을 기준으로 할지
        public float damage = 10f;

        [Header("Targeting Setting")]
        public ProjectileTargetMode targetMode = ProjectileTargetMode.Forward;
        public Vector3 targetOffset;
        public bool projectTargetToGround;
        public LayerMask groundLayerMask = -1;
        public float groundProbeHeight = 10f;
        public float groundProbeDistance = 20f;

        [Header("Move Setting")]
        public float speed = 10f;
        public float duration = 3f;

        [Header("Hit Setting")]
        [Tooltip("Player/Monster 오너는 ActorDefinition 또는 ActorType 기본 규칙으로 자동 결정한다. 그 외 액터의 fallback 값으로 사용한다.")]
        public LayerMask targetHitLayer;
        public string hitParticleName;

        public override string GetDisplayName() => "Projectile";

        public override string GetShortLabel()
        {
            if (projectilePrefab != null)
                return $"Spawn: {projectilePrefab.name}";
            return "Projectile: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (projectilePrefab == null) return;

            var actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            // 스폰 기준점 (무기 본/트랜스폼)
            Transform spawnPoint = string.IsNullOrEmpty(spawnPointName)
                ? target.transform
                : FindTransformByName(target.transform, spawnPointName) ?? target.transform;

            // 물리적 이동 궤적 (캐릭터 정면 수평)
            Vector3 flyDirection = target.transform.forward;
            flyDirection.y = 0f;
            if (flyDirection == Vector3.zero) flyDirection = target.transform.forward;
            flyDirection.Normalize();

            // 무기의 현재 회전값 및 축 보정
            Quaternion weaponRot = useSpawnRotation ? spawnPoint.rotation : target.transform.rotation;
            weaponRot *= Quaternion.Euler(rotationOffset);

            Vector3 weaponUp = weaponRot * Vector3.up;
            Vector3 projectedUp = Vector3.ProjectOnPlane(weaponUp, flyDirection).normalized;

            if (projectedUp == Vector3.zero)
            {
                projectedUp = Vector3.up;
            }

            Vector3 worldPos = spawnPoint.TransformPoint(spawnOffset);
            bool hasTargetPosition = TryResolveTargetPosition(actor, worldPos, flyDirection, out Vector3 targetPosition);

            if (hasTargetPosition)
            {
                targetPosition += targetOffset;
                if (projectTargetToGround)
                    targetPosition = ProjectToGround(targetPosition);

                Vector3 targetDirection = targetPosition - worldPos;
                if (targetDirection.sqrMagnitude > 0.001f)
                {
                    flyDirection = targetDirection.normalized;
                    projectedUp = Vector3.ProjectOnPlane(weaponUp, flyDirection).normalized;
                    if (projectedUp == Vector3.zero)
                        projectedUp = Vector3.up;
                }

                if (projectilePrefab is AOEProjectile)
                    worldPos = targetPosition;
            }

            Quaternion finalRot = Quaternion.LookRotation(flyDirection, projectedUp);
            var instance = GameObject.Instantiate(projectilePrefab, worldPos, finalRot);
            var projectile = instance.GetComponent<BaseProjectile>();

            if (projectile != null)
            {
                LayerMask hitLayer = ResolveTargetHitLayer(actor);
                projectile.Initialize(worldPos, flyDirection, damage, speed, actor, duration, hitLayer, hitParticleName);

                if (hasTargetPosition && projectile is AOEProjectile aoeProjectile)
                    aoeProjectile.SetCenterPosition(targetPosition);

                if (hasTargetPosition && projectile is ArcingProjectile arcingProjectile)
                    arcingProjectile.SetTargetPosition(targetPosition);
            }

            if (actor is MonsterActor monsterActor)
                monsterActor.Combat?.CompleteDangerRing();
        }

        private LayerMask ResolveTargetHitLayer(GameActor actor)
        {
            if (actor == null) return targetHitLayer;

            LayerMask actorTargetLayer = actor.GetAttackTargetLayerMask();
            return actorTargetLayer.value != 0 ? actorTargetLayer : targetHitLayer;
        }

        private bool TryResolveTargetPosition(GameActor actor, Vector3 spawnPosition, Vector3 fallbackDirection, out Vector3 position)
        {
            position = spawnPosition + fallbackDirection.normalized * Mathf.Max(0f, speed);
            Vector3 resolvedPosition;

            switch (targetMode)
            {
                case ProjectileTargetMode.Forward:
                    return false;

                case ProjectileTargetMode.LockOnTarget:
                    if (TryGetLockOnTargetPosition(out resolvedPosition))
                    {
                        position = resolvedPosition;
                        return true;
                    }
                    return false;

                case ProjectileTargetMode.EnemySkillTarget:
                    if (TryGetEnemySkillTargetPosition(actor, out resolvedPosition))
                    {
                        position = resolvedPosition;
                        return true;
                    }
                    return false;

                case ProjectileTargetMode.TargetPosition:
                    if (TryGetPrimaryTargetPosition(actor, out resolvedPosition))
                    {
                        position = resolvedPosition;
                        return true;
                    }
                    return false;

                case ProjectileTargetMode.TelegraphPosition:
                    if (TryGetTelegraphPosition(actor, out resolvedPosition))
                    {
                        position = resolvedPosition;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private static bool TryGetPrimaryTargetPosition(GameActor actor, out Vector3 position)
        {
            if (TryGetEnemySkillTargetPosition(actor, out position))
                return true;

            return TryGetLockOnTargetPosition(out position);
        }

        private static bool TryGetLockOnTargetPosition(out Vector3 position)
        {
            position = default;

            Transform lockOnTarget = CameraManager.Instance != null
                ? CameraManager.Instance.GetLockOnTarget()
                : null;
            if (lockOnTarget == null)
                return false;

            position = lockOnTarget.position;
            return true;
        }

        private static bool TryGetEnemySkillTargetPosition(GameActor actor, out Vector3 position)
        {
            position = default;

            EnemyCombat combat = actor != null ? actor.GetComponent<EnemyCombat>() : null;
            if (combat == null || combat.SkillTargetList == null || combat.SkillTargetList.Count == 0)
                return false;

            Transform targetTransform = combat.SkillTargetList[0]?.GetTransform();
            if (targetTransform == null)
                return false;

            position = targetTransform.position;
            return true;
        }

        private static bool TryGetTelegraphPosition(GameActor actor, out Vector3 position)
        {
            position = default;

            EnemyCombat combat = actor != null ? actor.GetComponent<EnemyCombat>() : null;
            if (combat == null)
                return false;

            position = combat.GetCurrentAttackPosition();
            return true;
        }

        private Vector3 ProjectToGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * Mathf.Max(0f, groundProbeHeight);
            float distance = Mathf.Max(0.01f, groundProbeHeight + groundProbeDistance);

            return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundLayerMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : position;
        }

        private Transform FindTransformByName(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                if (child.name == name) return child;
            }
            return null;
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
