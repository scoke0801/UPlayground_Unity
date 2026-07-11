using System.Collections;
using System.Collections.Generic;
using INab.Common;
using UnityEngine;

namespace UPlayGround.Components
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
        private const float MinimumVisibleDuration = 0.12f;

        [SerializeField] private bool _debugLog;

        private readonly List<WeaponTrailEffect> _trails = new List<WeaponTrailEffect>();
        private bool _isDirty = true;
        private float _lastStartTime = -999f;
        private Coroutine _pendingStopCoroutine;

        private void Awake()
        {
            RefreshTrails();
        }

        private void OnDisable()
        {
            StopCachedAttackTrails(immediate: true);
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
            CancelPendingStop();
            _lastStartTime = Time.unscaledTime;

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] StartAttackTrails owner={name}, count={_trails.Count}");

            for (int i = 0; i < _trails.Count; i++)
            {
                var trail = _trails[i];
                if (!CanUseTrail(trail, i)) continue;

                SetTrailPrefabInstanceActive(trail, true);
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

            if (immediate)
                CancelPendingStop();

            if (!immediate)
            {
                float remainingVisibleTime = MinimumVisibleDuration - (Time.unscaledTime - _lastStartTime);
                if (remainingVisibleTime > 0f)
                {
                    CancelPendingStop();
                    _pendingStopCoroutine = StartCoroutine(CoStopAfterDelay(remainingVisibleTime));
                    return;
                }
            }

            for (int i = 0; i < _trails.Count; i++)
            {
                var trail = _trails[i];

                // 한 트레일 정지에서 예외가 나도 나머지(특히 활성 모델 트레일) 정지를 막지 않는다.
                try
                {
                    if (immediate)
                        StopTrailImmediate(trail);
                    else
                        StopTrailIfAvailable(trail);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ActorWeaponTrailController] 트레일 정지 실패 trail={(trail != null ? trail.name : "null")}, owner={name}: {e}");
                }
            }
        }

        private IEnumerator CoStopAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _pendingStopCoroutine = null;
            StopCachedAttackTrails(immediate: false);
        }

        private void CancelPendingStop()
        {
            if (_pendingStopCoroutine == null) return;

            StopCoroutine(_pendingStopCoroutine);
            _pendingStopCoroutine = null;
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

            // 활성 트레일은 StopTrail(0f)로 실행 중인 코루틴을 먼저 끊고,
            // 이후 VFX Graph 시뮬레이션까지 직접 초기화해 월드 공간 잔여 Trail을 제거한다.
            if (trail.isActiveAndEnabled)
                trail.StopTrail(0f);

            ForceStopTrailProperties(trail);
        }

        private static void StopTrailIfAvailable(WeaponTrailEffect trail)
        {
            if (trail == null || trail.vfxComponent == null) return;

            // StopTrailImmediate와 동일한 이유로 비활성 트레일은 코루틴을 거치지 않는다.
            if (!trail.isActiveAndEnabled)
            {
                ForceStopTrailProperties(trail);
                return;
            }

            trail.StopTrail(DefaultFadeOutDuration);
        }

        private static void ForceStopTrailProperties(WeaponTrailEffect trail)
        {
            if (trail == null || trail.vfxComponent == null) return;

            trail.vfxComponent.pause = false;
            trail.vfxComponent.Reinit();
            trail.SetProperty_EffectActive(false);
            trail.SetProperty_EffectAlive(0f);
            trail.SendStopEvent();
            trail.currentEffectState = WeaponTrailEffect.EffectState.Off;
            SetTrailPrefabInstanceActive(trail, false);
        }

        private static void SetTrailPrefabInstanceActive(WeaponTrailEffect trail, bool active)
        {
            if (trail == null || trail.instantiatedTrailPrefab == null) return;

            trail.instantiatedTrailPrefab.SetActive(active);
        }

        private void TryInstantiateTrailPrefab(WeaponTrailEffect trail)
        {
            if (trail == null || trail.trailPrefab == null) return;

            if (_debugLog)
                Debug.Log($"[ActorWeaponTrailController] Instantiate missing trail prefab. trail={trail.name}, prefab={trail.trailPrefab.name}, owner={name}");

            trail._InstantiateTrailPrefab();
            SetTrailPrefabInstanceActive(trail, false);
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
