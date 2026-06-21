using System;
using System.Linq;
using FX;
using UnityEngine;

namespace UPlayGround.Data.Event
{
    public enum SlashVFXPositionSpace
    {
        Blade,
        World,
    }

    public enum SlashVFXRotationSpace
    {
        BladeOffset,
        World,
    }

    [Serializable]
    public class SlashVFXEvent : MotionEventBase
    {
        [Header("Slash VFX Setting")]
        [Tooltip("WeaponSlashVfxSpawner를 찾으면 Spawner 설정을 사용한다. 단, vfxPrefab이 직접 지정되어 있으면 Spawner의 prefab보다 우선한다.")]
        public bool useSpawnerSettings = true;
        [Tooltip("useSpawnerSettings가 켜져 있어도 위치/회전/스케일/수명은 이 이벤트 값을 사용한다.")]
        public bool overrideSpawnerTransform = true;
        
        public GameObject vfxPrefab;

        [Header("Blade Point")]
        [Tooltip("비워두면 target 하위의 첫 WeaponSlashVfxSpawner를 사용한다. 지정하면 해당 이름의 오브젝트 하위 Spawner를 우선 사용한다.")]
        public string spawnerObjectName;
        [Tooltip("Spawner를 찾지 못했을 때 Blade Point 이름 검색 범위를 제한할 부모 이름. 비워두면 target 전체에서 검색한다.")]
        public string weaponRootName;
        public string basePointName = "Blade_Base";
        public string tipPointName = "Blade_Tip";

        [Header("Offset")]
        public SlashVFXPositionSpace positionSpace = SlashVFXPositionSpace.World;
        public Vector3 positionOffset;
        public SlashVFXRotationSpace rotationSpace = SlashVFXRotationSpace.World;
        public Vector3 rotationOffset;

        [Header("Lifecycle")]
        public float scale = 1f;
        public float destroyDelay = 2f;

        public override string GetDisplayName() => "Slash VFX";

        public override string GetShortLabel()
        {
            GameObject prefab = vfxPrefab;
            return prefab != null ? $"Slash VFX: {prefab.name}" : "Slash VFX: (None)";
        }

        public override void Execute(GameObject target)
        {
            if (target == null)
                return;

            SpawnSlash(target);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }

        private void SpawnSlash(GameObject target)
        {
            if (useSpawnerSettings)
            {
                TrySpawnFromSpawnerSettings(target);
                return;
            }

            GameObject prefab = vfxPrefab;
            string baseName = basePointName;
            string tipName = tipPointName;
            Vector3 offset = positionOffset;
            Vector3 rotOffset = rotationOffset;
            float destroy = destroyDelay;
            Vector3 finalScale =  Vector3.one * scale;

            if (prefab == null)
                return;

            if (TrySpawnFromSpawner(target, prefab, offset, rotOffset, finalScale, destroy))
                return;

            Transform searchRoot = ResolveSearchRoot(target.transform, weaponRootName);
            Transform bladeBase = FindTransformByName(searchRoot, baseName);
            Transform bladeTip = FindTransformByName(searchRoot, tipName);

            if (bladeBase == null || bladeTip == null)
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Missing blade points.", target);
                return;
            }

            bool useWorldPositionOffset = positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = rotationSpace == SlashVFXRotationSpace.World;
            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(bladeBase, bladeTip, target.transform, offset, useWorldPositionOffset, rotOffset, useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation))
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Invalid blade direction.", target);
                return;
            }

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, finalScale);

            if (destroy > 0f)
                GameObject.Destroy(instance, destroy);
        }

        private bool TrySpawnFromSpawnerSettings(GameObject target)
        {
            WeaponSlashVfxSpawner spawner = ResolveSpawner(target);
            if (spawner == null)
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: {nameof(useSpawnerSettings)} is enabled, but {nameof(WeaponSlashVfxSpawner)} was not found. target={target?.name}, spawnerObjectName={spawnerObjectName}", target);
                return false;
            }

            GameObject prefab = vfxPrefab != null ? vfxPrefab : spawner.SlashVfxPrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Missing VFX prefab. spawner={spawner.name}", spawner);
                return false;
            }

            Vector3 offset = overrideSpawnerTransform ? positionOffset : spawner.PositionOffset;
            Vector3 rotOffset = overrideSpawnerTransform ? rotationOffset : spawner.RotationOffsetEuler;
            bool useWorldPositionOffset = overrideSpawnerTransform && positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = overrideSpawnerTransform && rotationSpace == SlashVFXRotationSpace.World;

            if (!spawner.TryGetSpawnPose(prefab, offset, useWorldPositionOffset, rotOffset, useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation))
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Failed to resolve spawn pose from spawner={spawner.name}. Check Blade Base / Blade Tip references.", spawner);
                return false;
            }

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            float finalScale = overrideSpawnerTransform ? scale : spawner.Scale;
            instance.transform.localScale *= finalScale;

            float finalDestroyDelay = overrideSpawnerTransform ? destroyDelay : spawner.DestroyDelay;
            if (finalDestroyDelay > 0f)
                GameObject.Destroy(instance, finalDestroyDelay);

            return true;
        }

        private bool TrySpawnFromSpawner(GameObject target, GameObject prefab, Vector3 offset, Vector3 rotOffset, Vector3 finalScale, float destroy)
        {
            WeaponSlashVfxSpawner spawner = ResolveSpawner(target);
            if (spawner == null)
                return false;

            bool useWorldPositionOffset = positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = rotationSpace == SlashVFXRotationSpace.World;
            if (!spawner.TryGetSpawnPose(prefab, offset, useWorldPositionOffset, rotOffset, useWorldRotation, out Vector3 spawnPosition, out Quaternion rotation))
                return false;

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, finalScale);

            if (destroy > 0f)
                GameObject.Destroy(instance, destroy);

            return true;
        }

        private WeaponSlashVfxSpawner ResolveSpawner(GameObject target)
        {
            if (target == null)
                return null;

            if (!string.IsNullOrEmpty(spawnerObjectName))
            {
                Transform spawnerRoot = FindTransformByName(target.transform, spawnerObjectName);
                if (spawnerRoot != null)
                    return SelectBestSpawner(spawnerRoot.GetComponentsInChildren<WeaponSlashVfxSpawner>(true));

                return null;
            }

            if (!string.IsNullOrEmpty(weaponRootName))
            {
                Transform weaponRoot = FindTransformByName(target.transform, weaponRootName);
                if (weaponRoot != null)
                    return SelectBestSpawner(weaponRoot.GetComponentsInChildren<WeaponSlashVfxSpawner>(true));
            }

            return SelectBestSpawner(target.GetComponentsInChildren<WeaponSlashVfxSpawner>(true));
        }

        private WeaponSlashVfxSpawner SelectBestSpawner(WeaponSlashVfxSpawner[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
                return null;

            return candidates.FirstOrDefault(IsActiveUsableSpawner)
                ?? candidates.FirstOrDefault(IsUsableSpawner)
                ?? candidates.FirstOrDefault(spawner => spawner != null && spawner.isActiveAndEnabled)
                ?? candidates.FirstOrDefault(spawner => spawner != null);
        }

        private bool IsActiveUsableSpawner(WeaponSlashVfxSpawner spawner)
        {
            return spawner != null && spawner.isActiveAndEnabled && IsUsableSpawner(spawner);
        }

        private bool IsUsableSpawner(WeaponSlashVfxSpawner spawner)
        {
            return spawner != null && spawner.BladeBase != null && spawner.BladeTip != null;
        }

        private Transform ResolveSearchRoot(Transform target, string rootName)
        {
            if (target == null || string.IsNullOrEmpty(rootName))
                return target;

            return FindTransformByName(target, rootName) ?? target;
        }

        private Transform FindTransformByName(Transform parent, string transformName)
        {
            if (parent == null || string.IsNullOrEmpty(transformName))
                return null;

            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == transformName)
                    return child;
            }

            return null;
        }
    }
}
