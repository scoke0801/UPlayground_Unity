using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Components
{
    /// <summary>준비한 단발 입자를 재사용하고 자연 종료된 슬롯만 다시 빌려준다.</summary>
    internal sealed class FootstepEffectPool
    {
        private readonly Dictionary<GameObject, ParticleSystem[]> _effects = new();
        private readonly GameObject _root;

        public FootstepEffectPool(FootstepSettingsSO settings, Transform owner)
        {
            _root = new GameObject("Footstep Effects");
            _root.transform.SetParent(owner, false);
            for (int i = 0; i < settings.surfaces.Length; i++)
            {
                FootstepSurfaceProfileSO surface = settings.surfaces[i];
                if (surface == null) continue;
                Prepare(surface.walk, settings.instancesPerEffect);
                Prepare(surface.run, settings.instancesPerEffect);
                Prepare(surface.landing, settings.instancesPerEffect);
            }
        }

        private void Prepare(FootstepSurfaceProfileSO.Response response, int count)
        {
            Prepare(response.effect, count);
            Prepare(response.secondaryEffect, count);
        }

        private void Prepare(GameObject prefab, int count)
        {
            if (prefab == null || _effects.ContainsKey(prefab)) return;
            var instances = new ParticleSystem[Mathf.Max(1, count)];
            for (int i = 0; i < instances.Length; i++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, _root.transform);
                instances[i] = instance.GetComponent<ParticleSystem>();
                if (instances[i] != null)
                    instances[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _effects.Add(prefab, instances);
        }

        public void Play(GameObject prefab, Vector3 position, Vector3 normal, Vector3 forward, float scale)
        {
            if (prefab == null || !_effects.TryGetValue(prefab, out var instances)) return;
            for (int i = 0; i < instances.Length; i++)
            {
                ParticleSystem particles = instances[i];
                if (particles == null || particles.IsAlive(true)) continue;
                Transform effectTransform = particles.transform;
                Vector3 tangent = Vector3.ProjectOnPlane(forward, normal);
                if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.ProjectOnPlane(Vector3.forward, normal);
                effectTransform.SetPositionAndRotation(position,
                    Quaternion.LookRotation(tangent, normal) * prefab.transform.localRotation);
                effectTransform.localScale = prefab.transform.localScale * scale;
                particles.Clear(true);
                particles.Play(true);
                return;
            }
        }

        public void Stop()
        {
            foreach (ParticleSystem[] instances in _effects.Values)
                for (int i = 0; i < instances.Length; i++)
                    if (instances[i] != null)
                        instances[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Dispose()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _effects.Clear();
        }
    }
}
