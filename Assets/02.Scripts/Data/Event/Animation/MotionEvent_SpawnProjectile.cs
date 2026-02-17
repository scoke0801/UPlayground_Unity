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
        public GameObject projectilePrefab;
        public Vector3 spawnOffset;
        public float speed = 10f;
        public float duration = 3f;
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

            var pos = target.transform.position + spawnOffset;
            
            // target의 forward 방향 (이미 월드 좌표계)
            Vector3 worldDirection = target.transform.forward;
            worldDirection.y = 0; // y축 방향 제거
            worldDirection.Normalize();
    
            Quaternion rot = Quaternion.LookRotation(-worldDirection);

            var instance = GameObject.Instantiate(projectilePrefab, pos, rot);

            // BaseProjectile 컴포넌트 확인 및 초기화
            var projectile = instance.GetComponent<BaseProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(pos, worldDirection, 10f, target, duration, targetHitLayer, hitParticleName);
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}