using UnityEngine;

namespace FX
{
    public sealed class WeaponSlashVfxSpawner : MonoBehaviour
    {
        [Header("Blade")]
        [SerializeField] private Transform bladeBase;
        [SerializeField] private Transform bladeTip;

        [Header("VFX")]
        [SerializeField] private GameObject slashVfxPrefab;
        [SerializeField] private float scale = 1f;
        [SerializeField] private float destroyDelay = 2f;

        [Header("Offset")]
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffsetEuler;

        public Transform BladeBase => bladeBase;
        public Transform BladeTip => bladeTip;
        public GameObject SlashVfxPrefab => slashVfxPrefab;
        public float Scale => scale;
        public float DestroyDelay => destroyDelay;
        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffsetEuler => rotationOffsetEuler;

        public void SpawnSlash()
        {
            if (!TryGetSpawnPose(out Vector3 spawnPosition, out Quaternion rotation))
                return;

            GameObject instance = Instantiate(slashVfxPrefab, spawnPosition, rotation);
            instance.transform.localScale *= scale;

            if (destroyDelay > 0f)
                Destroy(instance, destroyDelay);
        }

        public bool TryGetSpawnPose(out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(slashVfxPrefab, positionOffset, rotationOffsetEuler, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 localPositionOffset, Vector3 localRotationOffsetEuler, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(prefab, localPositionOffset, localRotationOffsetEuler, false, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 positionOffsetValue, Vector3 rotationOffsetEulerValue, bool useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null || prefab == null)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Missing reference.", this);
                return false;
            }

            // 인스턴스 경로는 액터 컨텍스트가 없으므로 기준 회전을 identity로 두어 기존 절대 월드 동작을 유지한다.
            if (!TryGetSpawnPose(bladeBase, bladeTip, transform, positionOffsetValue, rotationOffsetEulerValue, useWorldRotation, Quaternion.identity, out spawnPosition, out rotation))
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Invalid blade direction.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Blade Base/Tip 자세로부터 Slash VFX의 생성 위치/회전을 계산하는 공용 로직.
        /// Spawner 인스턴스와 SlashVFXEvent의 폴백 경로가 동일한 수식을 공유하기 위한 단일 소스다.
        /// </summary>
        /// <param name="upFallback">bladeBase.up이 칼날 방향과 평행할 때 사용할 보조 up 기준(없으면 World up).</param>
        /// <param name="referenceRotation">
        /// useWorldRotation 모드에서 회전의 기준이 되는 회전. 액터(캐릭터) 루트 회전을 넘기면
        /// 칼날 방향과 무관하게 캐릭터가 바라보는 방향을 따라간다. identity를 넘기면 절대 월드 기준이 된다.
        /// World 모드는 원래 칼날 방향을 무시(고정 오일러)하므로, 이 값을 액터 회전으로 주면 기존 World 튜닝값을
        /// 그대로 보존한 채(캐릭터 정면=identity일 때 동일) 캐릭터 회전만 추가로 반영된다.
        /// </param>
        public static bool TryGetSpawnPose(Transform bladeBase, Transform bladeTip, Transform upFallback, Vector3 positionOffsetValue, Vector3 rotationOffsetEulerValue, bool useWorldRotation, Quaternion referenceRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null)
                return false;

            Vector3 bladeDirection = bladeTip.position - bladeBase.position;

            if (bladeDirection.sqrMagnitude < 0.0001f)
                return false;

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(bladeBase.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f && upFallback != null)
                upDirection = Vector3.ProjectOnPlane(upFallback.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();

            Vector3 center = Vector3.Lerp(bladeBase.position, bladeTip.position, 0.5f);

            Quaternion bladeRotation = Quaternion.LookRotation(bladeDirection, upDirection);
            // World 모드는 칼날 방향 대신 referenceRotation(액터 루트 회전 등)을 기준으로 삼는다.
            // identity면 절대 월드, 액터 회전이면 캐릭터가 바라보는 방향을 따른다.
            rotation = useWorldRotation
                ? referenceRotation * Quaternion.Euler(rotationOffsetEulerValue)
                : bladeRotation * Quaternion.Euler(rotationOffsetEulerValue);

            // 위치 offset은 항상 칼날(blade) 기준으로 적용한다.
            // → 호밍/루트 facing이 어떻게 돌든 offset이 칼날에 일관되게 붙는다(facing 독립, tune-once).
            //   회전(rotation)은 위에서 referenceRotation 기준으로 계산되어 캐릭터 방향을 따라간다.
            //   (referenceRotation은 회전에만 사용되며 위치에는 영향을 주지 않는다.)
            Vector3 worldOffset = bladeRotation * positionOffsetValue;
            spawnPosition = center + worldOffset;
            return true;
        }

        public void SetBladePoints(Transform newBladeBase, Transform newBladeTip)
        {
            bladeBase = newBladeBase;
            bladeTip = newBladeTip;
        }

        public void ApplySettings(GameObject prefab, float newScale, float newDestroyDelay, Vector3 newPositionOffset, Vector3 newRotationOffsetEuler)
        {
            slashVfxPrefab = prefab;
            scale = newScale;
            destroyDelay = newDestroyDelay;
            positionOffset = newPositionOffset;
            rotationOffsetEuler = newRotationOffsetEuler;
        }
    }
}
