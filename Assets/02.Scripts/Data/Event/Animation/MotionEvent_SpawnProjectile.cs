using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace UPlayGround.Data.Event
{
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
        
        [Header("Move Setting")]
        public float speed = 10f;
        public float duration = 3f;
        
        [Header("Hit Setting")]
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
            
            // 회전을 위한 스폰 포인트 결정
            Transform rotationPoint = string.IsNullOrEmpty(spawnPointName)
                ? target.transform
                : FindTransformByName(target.transform, spawnPointName) ?? target.transform;
            
            Quaternion baseRot = useSpawnRotation
                ? Quaternion.Euler(0f, rotationPoint.rotation.eulerAngles.y, -rotationPoint.rotation.eulerAngles.z)
                : Quaternion.identity;
            
            Quaternion finalRot = baseRot * Quaternion.Euler(rotationOffset);
            
            Vector3 worldDirection = target.transform.forward;
            worldDirection.y = 0; // y축 방향 제거
            worldDirection.Normalize();
            
            var worldPos = target.transform.position + spawnOffset;
            var instance = GameObject.Instantiate(projectilePrefab, worldPos, finalRot);

            var projectile = instance.GetComponent<BaseProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(worldPos, worldDirection, 10f, speed, actor, duration, targetHitLayer, hitParticleName);
            }
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