using System.Collections.Generic;
using INab.Common;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 공격 상태 기준으로 액터 하위 WeaponTrailEffect를 직접 제어한다.
    /// Animancer/MotionSet 구조에서는 AnimationClip 이벤트에 의존하지 않기 위한 런타임 브리지다.
    /// </summary>
    public class ActorWeaponTrailController : MonoBehaviour
    {
        private const float DefaultFadeInDuration = 0.04f;
        private const float DefaultFadeOutDuration = 0.08f;
        private const float DefaultTrailLengthLifetime = 0.35f;

        [SerializeField] private bool _debugLog;

        private readonly List<WeaponTrailEffect> _trails = new List<WeaponTrailEffect>();
        private bool _isDirty = true;

        private void Awake()
        {
            RefreshTrails();
        }

        public static void StartAttackTrails(UnityEngine.Component owner)
        {
            if (owner == null) return;

            GetOrAdd(owner).PlayAttackTrails();
        }

        public static void StopAttackTrails(UnityEngine.Component owner)
        {
            if (owner == null) return;

            GetOrAdd(owner).StopCachedAttackTrails();
        }

        public static void RefreshAttackTrails(UnityEngine.Component owner)
        {
            if (owner == null) return;

            GetOrAdd(owner).RefreshTrails();
        }

        public static void SuppressAttackTrails(UnityEngine.Component owner)
        {
            if (owner == null) return;

            GetOrAdd(owner).StopCachedAttackTrails(immediate: true);
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void RefreshTrails()
        {
            _trails.Clear();
            GetComponentsInChildren(true, _trails);
            _isDirty = false;

            StopCachedAttackTrails(immediate: true);

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] RefreshTrails owner={name}, count={_trails.Count}");
        }

        private static ActorWeaponTrailController GetOrAdd(UnityEngine.Component owner)
        {
            var controller = owner.GetComponent<ActorWeaponTrailController>();
            if (controller != null) return controller;

            controller = owner.gameObject.AddComponent<ActorWeaponTrailController>();
            controller.RefreshTrails();
            return controller;
        }

        private void PlayAttackTrails()
        {
            EnsureCache();

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] StartAttackTrails owner={name}, count={_trails.Count}");

            for (int i = 0; i < _trails.Count; i++)
            {
                var trail = _trails[i];
                if (!CanUseTrail(trail, i)) continue;

                ResetTrailForStart(trail);
                trail.SetTrailLength(DefaultTrailLengthLifetime);
                trail.StartTrail(DefaultFadeInDuration);
            }
        }

        private void StopCachedAttackTrails()
        {
            StopCachedAttackTrails(immediate: false);
        }

        private void StopCachedAttackTrails(bool immediate)
        {
            EnsureCache();

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] StopAttackTrails owner={name}, count={_trails.Count}");

            for (int i = 0; i < _trails.Count; i++)
            {
                var trail = _trails[i];
                if (immediate)
                {
                    StopTrailImmediate(trail);
                    continue;
                }

                if (!CanUseTrail(trail, i)) continue;
                trail.StopTrail(DefaultFadeOutDuration);
            }
        }

        private bool CanUseTrail(WeaponTrailEffect trail, int index)
        {
            if (trail == null)
            {
                if (_debugLog)
                    Debug.LogWarning($"[ActorWeaponTrailController] Trail[{index}] is null. owner={name}");
                return false;
            }

            if (!trail.isActiveAndEnabled)
            {
                if (_debugLog)
                    Debug.LogWarning($"[ActorWeaponTrailController] Trail[{index}] is inactive. trail={trail.name}, owner={name}");
                return false;
            }

            if (TryGetComponent(out PlayerEquipment equipment) &&
                !equipment.IsWeaponTrailDrawable(trail))
            {
                if (_debugLog)
                    Debug.LogWarning($"[ActorWeaponTrailController] Trail[{index}] is not on a drawn weapon. trail={trail.name}, owner={name}");
                return false;
            }

            if (trail.vfxComponent == null)
            {
                TryInstantiateTrailPrefab(trail);
            }

            if (trail.vfxComponent == null)
            {
                if (_debugLog)
                    Debug.LogWarning($"[ActorWeaponTrailController] Trail[{index}] has no vfxComponent. trail={trail.name}, owner={name}");
                return false;
            }

            if (trail.lineTipTransform == null || trail.lineBottomTransform == null)
            {
                if (_debugLog)
                    Debug.LogWarning($"[ActorWeaponTrailController] Trail[{index}] missing line transforms. trail={trail.name}, owner={name}");
                return false;
            }

            return true;
        }

        private static void ResetTrailForStart(WeaponTrailEffect trail)
        {
            if (trail == null || trail.vfxComponent == null) return;

            trail.vfxComponent.Reinit();
            trail.SetProperty_EffectActive(false);
            trail.SetProperty_EffectAlive(0f);
        }

        private static void StopTrailImmediate(WeaponTrailEffect trail)
        {
            if (trail == null || trail.vfxComponent == null) return;

            trail.StopTrail(0f);
        }

        private void TryInstantiateTrailPrefab(WeaponTrailEffect trail)
        {
            if (trail == null || trail.trailPrefab == null) return;

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] Instantiate missing trail prefab. trail={trail.name}, prefab={trail.trailPrefab.name}, owner={name}");

            trail._InstantiateTrailPrefab();
        }

        private void EnsureCache()
        {
            if (_isDirty || HasMissingReference())
                RefreshTrails();
        }

        private bool HasMissingReference()
        {
            for (int i = 0; i < _trails.Count; i++)
            {
                if (_trails[i] == null)
                    return true;
            }

            return false;
        }
    }
}
