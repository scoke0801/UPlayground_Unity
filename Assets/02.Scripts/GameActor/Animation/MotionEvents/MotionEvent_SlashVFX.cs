using System;
using System.Linq;
using UPlayGround.Particle;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UPlayGround.Data.Event
{
    public enum SlashVFXPositionSpace
    {
        // 오프셋을 칼날(Blade) 로컬 기준으로 적용 — 칼날 방향을 따라간다.
        Blade,
        // 오프셋을 액터(캐릭터) 루트 회전 기준으로 적용. 칼날 방향은 무시하고 캐릭터가 바라보는 방향만 따른다.
        // (직렬화 호환을 위해 멤버명은 World 유지. 캐릭터 정면=identity일 때 절대 월드와 동일하게 동작한다.)
        World,
    }

    public enum SlashVFXRotationSpace
    {
        // 칼날 회전 * 오프셋 — 칼날 방향을 따라간다.
        BladeOffset,
        // 액터(캐릭터) 루트 회전 * 오프셋. 칼날 방향은 무시하고 캐릭터가 바라보는 방향만 따른다.
        // (직렬화 호환을 위해 멤버명은 World 유지. 캐릭터 정면=identity일 때 절대 월드와 동일하게 동작한다.)
        World,
    }

    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
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
        [Tooltip("World = 액터(캐릭터) 루트 회전 기준. 칼날 방향은 무시하고 캐릭터가 바라보는 방향을 따른다(정면=identity일 때 절대 월드와 동일). Blade = 칼날 로컬 기준.")]
        public SlashVFXPositionSpace positionSpace = SlashVFXPositionSpace.World;
        public Vector3 positionOffset;
        [Tooltip("World = 액터(캐릭터) 루트 회전 기준. 칼날 방향은 무시하고 캐릭터가 바라보는 방향을 따른다(정면=identity일 때 절대 월드와 동일). BladeOffset = 칼날 회전 기준.")]
        public SlashVFXRotationSpace rotationSpace = SlashVFXRotationSpace.World;
        public Vector3 rotationOffset;

        [Header("Lifecycle")]
        public float scale = 1f;
        public float destroyDelay = 2f;

        [Tooltip("켜면 생성된 VFX를 액터 루트에 부착해 루트모션/호밍 이동을 따라간다(월드 포즈 유지). " +
                 "끄면 스폰 위치에 월드 고정(허공 고정) — 칼자국을 의도적으로 남기는 연출용. " +
                 "정지 공격에서는 둘이 동일하게 보인다.")]
        public bool attachToActor = true;

        // 블레이드 Base/Tip 본의 월드 포즈를 즉석 샘플링하므로 본 평가 후(LateUpdate)에 실행해야 한다.
        public override bool RequiresPostEvaluation => true;

        // 스폰된 파티클 시드를 결정적으로 고정하기 위한 기본 시드.
        // 모든 SlashVFX 스폰 경로가 동일 시드를 공유하도록 스포너의 단일 출처를 참조한다.
        private const uint SlashParticleSeed = WeaponSlashVfxSpawner.DefaultSlashParticleSeed;

        public override string GetDisplayName() => "Slash VFX";

        public override string GetShortLabel()
        {
            GameObject prefab = vfxPrefab;
            return prefab != null ? $"Slash VFX: {prefab.name}" : "Slash VFX: (None)";
        }

        public override void Execute(GameObject target)
        {
            // 분율 정보 없이 직접 호출되는 경로(레거시/디버그)는 현재 프레임 포즈로 스폰한다.
            Execute(target, 1f);
        }

        public override void Execute(GameObject target, float subFrameFraction)
        {
            if (target == null)
                return;

            // 스포너가 있으면 블레이드를 발화 시각(eventStart)에 해당하는 보간 포즈로 임시 이동시켜
            // 스폰한 뒤 원복한다 → 발화 프레임의 오버슈트와 무관하게 스폰 위치가 결정적이 된다.
            // 스포너가 없는 이름탐색 폴백 경로는 직전 프레임 캐시가 없으므로 기존(현재 포즈) 동작을 유지한다.
            WeaponSlashVfxSpawner spawner = ResolveSpawner(target);
            if (spawner != null && spawner.BeginInterpolatedBladePose(subFrameFraction, out var savedPose))
            {
                try { SpawnSlash(target); }
                finally { spawner.EndInterpolatedBladePose(savedPose); }
            }
            else
            {
                SpawnSlash(target);
            }
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

            bool useWorldPosition = positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = rotationSpace == SlashVFXRotationSpace.World;
            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(bladeBase, bladeTip, target.transform, offset, useWorldPosition, rotOffset, useWorldRotation, target.transform.rotation, out Vector3 spawnPosition, out Quaternion rotation))
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Invalid blade direction.", target);
                return;
            }

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            WeaponSlashVfxSpawner.ApplyDeterministicParticleSeed(instance, SlashParticleSeed);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, finalScale);
            AttachToActorIfNeeded(instance, target.transform);

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
            bool useWorldPosition = overrideSpawnerTransform && positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = overrideSpawnerTransform && rotationSpace == SlashVFXRotationSpace.World;

            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(spawner.BladeBase, spawner.BladeTip, target.transform, offset, useWorldPosition, rotOffset, useWorldRotation, target.transform.rotation, out Vector3 spawnPosition, out Quaternion rotation))
            {
                Debug.LogWarning($"{nameof(SlashVFXEvent)}: Failed to resolve spawn pose from spawner={spawner.name}. Check Blade Base / Blade Tip references.", spawner);
                return false;
            }

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            WeaponSlashVfxSpawner.ApplyDeterministicParticleSeed(instance, SlashParticleSeed);
            AttachToActorIfNeeded(instance, target.transform);
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

            bool useWorldPosition = positionSpace == SlashVFXPositionSpace.World;
            bool useWorldRotation = rotationSpace == SlashVFXRotationSpace.World;
            if (!WeaponSlashVfxSpawner.TryGetSpawnPose(spawner.BladeBase, spawner.BladeTip, target.transform, offset, useWorldPosition, rotOffset, useWorldRotation, target.transform.rotation, out Vector3 spawnPosition, out Quaternion rotation))
                return false;

            GameObject instance = GameObject.Instantiate(prefab, spawnPosition, rotation);
            WeaponSlashVfxSpawner.ApplyDeterministicParticleSeed(instance, SlashParticleSeed);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, finalScale);
            AttachToActorIfNeeded(instance, target.transform);

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

        // 생성된 VFX를 액터 루트에 부착해 루트모션/호밍 이동을 따라가게 한다(월드 포즈 유지).
        // 블레이드가 아니라 루트에 붙이는 이유: 블레이드에 붙이면 진행 중인 스윙을 따라 번진다.
        // attachToActor가 false면 스폰 위치에 월드 고정("허공 고정") — 의도적으로 칼자국을 남기는 연출용.
        private void AttachToActorIfNeeded(GameObject instance, Transform actorRoot)
        {
            if (instance == null || !attachToActor || actorRoot == null)
                return;

            instance.transform.SetParent(actorRoot, worldPositionStays: true);
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
