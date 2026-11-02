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
        [Tooltip("비부착 생성 위치를 아래 지면에 맞춥니다. 지면이 없으면 생성하지 않습니다.")]
        public bool projectToGround;
        [Min(0.01f)] public float groundProbeDistance = 2f;
        public LayerMask groundLayers = ~0;
        
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

            ICinematicStageService stageService = Svc.CinematicStage;
            CinematicStageTicket stageTicket = stageService?.ActiveTicket ?? default;
            Transform presentationSpawnPoint = null;
            bool useStagePresentation = stageTicket.IsValid
                                        && stageService.TryResolvePresentationTransform(
                                            spawnPoint,
                                            out presentationSpawnPoint);
            if (useStagePresentation)
                spawnPoint = presentationSpawnPoint;

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

                if (projectToGround)
                {
                    var actor = target.GetComponentInParent<GameActor>();
                    Transform actorRoot = actor != null ? actor.transform : target.transform;
                    if (!TryGetGroundPose(worldPos, actorRoot, out RaycastHit ground)) return;
                    worldPos = ground.point;
                    finalRot = Quaternion.FromToRotation(Vector3.up, ground.normal) * finalRot;
                }

                _instance = GameObject.Instantiate(particlePrefab, worldPos, finalRot);
            }

            if (useStagePresentation)
            {
                UPlayGround.Data.CinematicStageRuntimeUtility.SetLayerRecursively(
                    _instance,
                    "UltimateVFX");
                stageService.RegisterTransient(stageTicket, _instance);
            }

            if (destroyOnFinish == false && particleLifeTime > 0f)
            {
                ActorSvc.Objects?.RegisterFXInstance(_instance, particleLifeTime);
            }
        }

        /// <summary>도약 프레임의 루트 높이와 무관하게 액터 외부의 가장 가까운 지면을 찾는다.</summary>
        private bool TryGetGroundPose(Vector3 position, Transform actorRoot, out RaycastHit ground)
        {
            ground = default;
            float nearest = float.PositiveInfinity;
            // 이벤트 발화 때 한 번만 조회하며, 겹친 자기 콜라이더를 제외한다.
            var hits = Physics.RaycastAll(position, Vector3.down, groundProbeDistance,
                groundLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(actorRoot) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                ground = hit;
            }
            return nearest < float.PositiveInfinity;
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
