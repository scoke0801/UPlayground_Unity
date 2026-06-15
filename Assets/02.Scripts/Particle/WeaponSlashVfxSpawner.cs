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
            instance.transform.localScale = Vector3.one * scale;

            if (destroyDelay > 0f)
                Destroy(instance, destroyDelay);
        }

        public bool TryGetSpawnPose(out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(slashVfxPrefab, positionOffset, rotationOffsetEuler, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 localPositionOffset, Vector3 localRotationOffsetEuler, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(prefab, localPositionOffset, false, localRotationOffsetEuler, false, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 positionOffsetValue, bool useWorldPositionOffset, Vector3 localRotationOffsetEuler, out Vector3 spawnPosition, out Quaternion rotation)
        {
            return TryGetSpawnPose(prefab, positionOffsetValue, useWorldPositionOffset, localRotationOffsetEuler, false, out spawnPosition, out rotation);
        }

        public bool TryGetSpawnPose(GameObject prefab, Vector3 positionOffsetValue, bool useWorldPositionOffset, Vector3 rotationOffsetEulerValue, bool useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = default;
            rotation = default;

            if (bladeBase == null || bladeTip == null || prefab == null)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Missing reference.", this);
                return false;
            }

            Vector3 bladeDirection = bladeTip.position - bladeBase.position;

            if (bladeDirection.sqrMagnitude < 0.0001f)
            {
                Debug.LogWarning($"{nameof(WeaponSlashVfxSpawner)}: Invalid blade direction.", this);
                return false;
            }

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(bladeBase.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.ProjectOnPlane(transform.up, bladeDirection);

            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();

            Vector3 center = Vector3.Lerp(bladeBase.position, bladeTip.position, 0.5f);

            Quaternion bladeRotation = Quaternion.LookRotation(bladeDirection, upDirection);
            rotation = useWorldRotation
                ? Quaternion.Euler(rotationOffsetEulerValue)
                : bladeRotation * Quaternion.Euler(rotationOffsetEulerValue);

            Vector3 worldOffset = useWorldPositionOffset ? positionOffsetValue : bladeRotation * positionOffsetValue;
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
