using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    public class SpawnProjectileEvent : MotionEventBase
    {
        public GameObject projectilePrefab;
        public Transform spawnPoint;
        public Vector3 direction = Vector3.forward;
        public float speed = 10f;

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

            var pos = spawnPoint != null ? spawnPoint.position : target.transform.position;
            var rot = target.transform.rotation;
            var instance = GameObject.Instantiate(projectilePrefab, pos, rot);

            var rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = target.transform.TransformDirection(direction) * speed;
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}