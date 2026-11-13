using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>객체의 모든 LOD/슬롯을 묶어 원본을 보존하는 런타임 재질 lease를 소유한다.</summary>
    public sealed class ReactiveEnvironmentSurface
    {
        private static readonly int State0 = Shader.PropertyToID("_EnvState0");
        private static readonly int State1 = Shader.PropertyToID("_EnvState1");
        private static readonly int Coverage = Shader.PropertyToID("_FuelCoverage");
        private readonly Renderer[] _renderers;
        private readonly Material[][] _originals;
        private readonly Dictionary<Material, Material> _leases = new();
        private bool _hasLease;

        public ReactiveEnvironmentSurface(Renderer[] renderers)
        {
            _renderers = renderers;
            _originals = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++) _originals[i] = renderers[i].sharedMaterials;
        }

        /// <summary>미지원 셰이더와 누락 LOD를 조용히 무시하지 않고 저작 경계에서 거부한다.</summary>
        public static string Validate(Transform root, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return "반응형 툰 표면이 비어 있습니다.";
            var mapped = new HashSet<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.transform.IsChildOf(root) || !mapped.Add(renderer))
                    return "반응형 툰 표면의 누락·중복·소유 계층을 확인하세요.";
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !material.HasProperty(State0) || !material.HasProperty(State1)
                        || material.shader.name != "UPlayGround/Environment/Reactive Toon")
                        return "반응형 표면에 지원하지 않는 셰이더 또는 빈 슬롯이 있습니다.";
                    if (material.GetFloat("_AllowFuelCoverage") > 0 && renderer.GetComponent<Collider>() != null)
                        return "연료 실루엣 소거 표면에는 게임플레이 Collider를 둘 수 없습니다.";
                }
            }
            foreach (LODGroup group in root.GetComponentsInChildren<LODGroup>(true))
                foreach (LOD lod in group.GetLODs())
                    foreach (Renderer renderer in lod.renderers)
                        if (renderer != null && !mapped.Contains(renderer)) return "LOD의 반응형 표면 바인딩이 누락되었습니다.";
            return null;
        }

        /// <summary>한 객체 안의 동일 원본 재질은 한 lease를 공유하며 dirty 상태에서만 갱신한다.</summary>
        public void Apply(EnvironmentReactionState state, float maximumHealth, float initialFuel)
        {
            if (!_hasLease) Acquire();
            Vector4 surface = new(state.wetness, state.charAmount,
                1 - Mathf.Clamp01(state.durability / maximumHealth), state.combustion == EnvironmentCombustion.Burning ? 1 : 0);
            Vector4 thermal = new(state.heat, 0, 0, 0);
            float coverage = initialFuel > 0 ? Mathf.Clamp01(state.fuel / initialFuel) : 1;
            foreach (Material material in _leases.Values)
            {
                material.SetVector(State0, surface);
                material.SetVector(State1, thermal);
                material.SetFloat(Coverage, coverage);
            }
        }

        private void Acquire()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var slots = new Material[_originals[i].Length];
                for (int slot = 0; slot < slots.Length; slot++)
                {
                    Material original = _originals[i][slot];
                    if (!_leases.TryGetValue(original, out var material))
                    {
                        material = new Material(original) { name = original.name + " (Environment Lease)",
                            hideFlags = HideFlags.DontSave };
                        _leases.Add(original, material);
                    }
                    slots[slot] = material;
                }
                _renderers[i].sharedMaterials = slots;
            }
            _hasLease = true;
        }

        /// <summary>풀 반환·언로드 시 원래 슬롯을 복원하고 소유한 재질만 정리한다.</summary>
        public void Release()
        {
            if (!_hasLease) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var slots = _renderers[i].sharedMaterials;
                for (int slot = 0; slot < slots.Length && slot < _originals[i].Length; slot++)
                    if (_leases.TryGetValue(_originals[i][slot], out var lease) && slots[slot] == lease)
                        slots[slot] = _originals[i][slot];
                _renderers[i].sharedMaterials = slots;
            }
            foreach (Material material in _leases.Values)
                if (Application.isPlaying) Object.Destroy(material); else Object.DestroyImmediate(material);
            _leases.Clear();
            _hasLease = false;
        }
    }
}
