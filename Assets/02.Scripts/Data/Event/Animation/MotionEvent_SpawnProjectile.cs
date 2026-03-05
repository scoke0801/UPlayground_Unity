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
        public float damage = 10f;
        
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
            Quaternion finalRot = Quaternion.LookRotation(flyDirection, projectedUp);

            Vector3 worldPos = spawnPoint.TransformPoint(spawnOffset);
    
            var instance = GameObject.Instantiate(projectilePrefab, worldPos, finalRot);
            var projectile = instance.GetComponent<BaseProjectile>();
    
            if (projectile != null)
            {
                projectile.Initialize(worldPos, flyDirection, damage, speed, actor, duration, targetHitLayer, hitParticleName);
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