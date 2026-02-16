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
        public string spawnPointName;
        public Vector3 offset;
        public bool attachToTarget = true;        
        public bool useSpawnRotation = true;
        public bool destroyOnFinish = true;
        
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
            
            Transform spawnPoint = target.transform;
            if (String.IsNullOrEmpty(spawnPointName) == false)
            {
                //spawnPoint = target.transform.Find(spawnPointName);
                spawnPoint    = FindTransformByName(target.transform, spawnPointName);
            }
            if (spawnPoint == null) spawnPoint = target.transform;

            if (attachToTarget)
            {
                _instance = GameObject.Instantiate(particlePrefab, spawnPoint);
                _instance.transform.localPosition = offset;
                
                if (useSpawnRotation)
                {
                    _instance.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                Vector3 worldOffset = spawnPoint.TransformDirection(offset);
                Vector3 position = spawnPoint.position + worldOffset;
                Quaternion rotation = useSpawnRotation ? spawnPoint.rotation : Quaternion.identity;
                
                _instance = GameObject.Instantiate(particlePrefab, position, rotation);
            }
        }
        
        private Transform FindTransformByName(Transform parent, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            Transform[] children = parent.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name == name)
                    return child;
            }
            return null;
        }
        public override void OnCompleteEvent(GameObject target)
        {
            if (_instance != null && destroyOnFinish == true)
            {
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }
    }

}