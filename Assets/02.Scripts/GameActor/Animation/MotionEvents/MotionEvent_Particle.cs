using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 파티클 이펙트 재생 이벤트
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("Particle", "VFX / SFX", 0, "파티클 또는 VFX를 생성합니다.", "vfx", "effect", "fx", "이펙트", "파티클")]
    public class BeginParticleEvent : MotionEventBase
    {
        public GameObject particlePrefab;
        public string spawnPointName;
        public Vector3 offset;                    // 위치 보정용
        public Vector3 rotationOffset;            // 방향 보정용 오일러 각도
        public bool attachToTarget = true;        // 대상에 붙여서 생성할 지
        public bool detachAfterSpawn = false;     // 생성 이후, 끊어낼지
        public bool useSpawnRotation = true;
        public bool destroyOnFinish = true;
        public float particleLifeTime = 0f;
        
        private GameObject _instance;

        // 월드 포즈를 스냅샷으로 고정하는 모드(비부착 생성, 또는 생성 직후 분리)만 본 평가 후(LateUpdate)에
        // 실행해야 한다. 순수 부착(attachToTarget && !detachAfterSpawn)은 파티클이 부모(본/소켓)를
        // 매 프레임 따라가므로 평가 순서와 무관하다.
        public override bool RequiresPostEvaluation => !attachToTarget || detachAfterSpawn;

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

            if (destroyOnFinish == true)
            {
                particleLifeTime = 0f;
            }

            Debug.Log("PlayParticleEvent");
            Transform spawnPoint = target.transform;
            if (String.IsNullOrEmpty(spawnPointName) == false)
            {
                spawnPoint = FindTransformByName(target.transform, spawnPointName);
            }

            if (spawnPoint == null) spawnPoint = target.transform;

            if (attachToTarget)
            {
                _instance = GameObject.Instantiate(particlePrefab, spawnPoint);

                _instance.transform.localPosition = offset;
                _instance.transform.localRotation = particlePrefab.transform.rotation * Quaternion.Euler(rotationOffset);

                // 생성 직후 부모 해제 → 월드 위치/회전은 유지된 채 독립
                if (detachAfterSpawn)
                    _instance.transform.SetParent(null);
            }
            else
            {
                Vector3 worldPos = spawnPoint.position + spawnPoint.TransformDirection(offset);
                Quaternion baseRot = useSpawnRotation ? spawnPoint.rotation : Quaternion.identity;
                Quaternion finalRot = baseRot * Quaternion.Euler(rotationOffset);

                _instance = GameObject.Instantiate(particlePrefab, worldPos, finalRot);
            }

            if (destroyOnFinish == false && particleLifeTime > 0f)
            {
                ActorSvc.Objects?.RegisterFXInstance(_instance, particleLifeTime);
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
                Debug.Log("PlayParticleEvent - OnComplete");
                GameObject.Destroy(_instance);
                _instance = null;
            }
        }
    }

}
