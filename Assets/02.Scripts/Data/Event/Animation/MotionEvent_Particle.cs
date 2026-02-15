using System;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 파티클 이펙트 재생 이벤트
    /// </summary>
    [Serializable]
    public class BeginParticleEvent : MotionEventBase
    {
        public GameObject particlePrefab;
        public Vector3 offset;
        public bool attachToTarget = true;

        private GameObject _instance;
        public override string GetDisplayName() => "Particle";

        public override string GetShortLabel()
        {
            if (particlePrefab != null)
                return $"Particle: {particlePrefab.name}";
            return "Particle: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (particlePrefab == null) return;

            if (attachToTarget)
            {
                _instance = GameObject.Instantiate(particlePrefab, target.transform);
                _instance.transform.localPosition = offset;
            }
            else
            {
                _instance = GameObject.Instantiate(particlePrefab);
                _instance.transform.position = target.transform.position + offset;
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            if (_instance != null)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }
    }

}