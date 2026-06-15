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
        [Tooltip("WeaponSlashVfxSpawner를 찾으면 Spawner에 설정된 prefab/offset/scale/destroyDelay를 사용한다.")]
        public bool useSpawnerSettings = true;
        [Tooltip("useSpawnerSettings가 켜져 있어도 위치/회전/스케일/수명은 이 이벤트 값을 사용한다.")]
        public bool overrideSpawnerTransform;
        public SlashVFXPresetSO preset;
        public GameObject vfxPrefab;

        [Header("Blade Point")]
        [Tooltip("비워두면 target 하위의 첫 WeaponSlashVfxSpawner를 사용한다. 지정하면 해당 이름의 오브젝트 하위 Spawner를 우선 사용한다.")]
        public string spawnerObjectName;
        [Tooltip("Spawner를 찾지 못했을 때 Blade Point 이름 검색 범위를 제한할 부모 이름. 비워두면 target 전체에서 검색한다.")]
        public string weaponRootName;
        public string basePointName = "Blade_Base";
        public string tipPointName = "Blade_Tip";

        [Header("Offset")]
        public SlashVFXPositionSpace positionSpace = SlashVFXPositionSpace.Blade;
        public Vector3 positionOffset;
        public SlashVFXRotationSpace rotationSpace = SlashVFXRotationSpace.BladeOffset;
        public Vector3 rotationOffset;

        [Header("Lifecycle")]
        public float scale = 1f;
        public float destroyDelay = 2f;

        public override string GetDisplayName() => "Slash VFX";

        public override string GetShortLabel()
        {
            GameObject prefab = preset != null ? preset.vfxPrefab : vfxPrefab;
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

            GameObject prefab = preset != null ? preset.vfxPrefab : vfxPrefab;
            string baseName = preset != null && !string.IsNullOrEmpty(preset.basePointName) ? preset.basePointName : basePointName;
            string tipName = preset != null && !string.IsNullOrEmpty(preset.tipPointName) ? preset.tipPointName : tipPointName;
            Vector3 offset = preset != null ? preset.positionOffset : positionOffset;
            Vector3 rotOffset = preset != null ? preset.rotationOffset : rotationOffset;
            float destroy = preset != null ? preset.destroyDelay : destroyDelay;
            Vector3 finalScale = preset != null ? preset.scaleMultiplier : Vector3.one * scale;

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

            Vector3 bladeDirection = bladeTip.position - bladeBase.position;
            if (bladeDirection.sqrMagnitude < 0.0001f)
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Invalid blade direction.", target);
                return;
            }

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(bladeBase.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.ProjectOnPlane(target.transform.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();

            Quaternion bladeRotation = Quaternion.LookRotation(bladeDirection, upDirection);
            Quaternion rotation = ResolveRotation(bladeRotation, rotOffset);

            Vector3 center = Vector3.Lerp(bladeBase.position, bladeTip.position, 0.5f);
            Vector3 spawnPosition = center + ResolveWorldPositionOffset(bladeRotation, offset);

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            instance.transform.localScale = finalScale;

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

            GameObject prefab = spawner.SlashVfxPrefab;
            if (prefab == null)
                prefab = preset != null ? preset.vfxPrefab : vfxPrefab;

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
            instance.transform.localScale = Vector3.one * finalScale;

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
            instance.transform.localScale = finalScale;

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

        private Vector3 ResolveWorldPositionOffset(Quaternion bladeRotation, Vector3 offset)
        {
            return positionSpace == SlashVFXPositionSpace.World ? offset : bladeRotation * offset;
        }

        private Quaternion ResolveRotation(Quaternion bladeRotation, Vector3 rotationEuler)
        {
            return rotationSpace == SlashVFXRotationSpace.World
                ? Quaternion.Euler(rotationEuler)
                : bladeRotation * Quaternion.Euler(rotationEuler);
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
