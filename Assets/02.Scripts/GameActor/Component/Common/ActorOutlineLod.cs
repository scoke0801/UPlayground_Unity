using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Config;

namespace UPlayGround.Components
{
    /// <summary>근접 디더와 같은 렌더러 소유자가 NPC의 외곽선 패스만 거리별로 교체하도록 돕는다.</summary>
    public sealed class ActorOutlineLod : IDisposable
    {
        private sealed class Binding
        {
            public Renderer Renderer;
            public Material[] Originals;
            public Material[] Reduced;
        }

        private readonly List<Binding> _bindings = new();
        private readonly Dictionary<Material, Material> _materials = new();
        private readonly List<Material> _currentMaterials = new();
        public bool IsReduced { get; private set; }

        /// <summary>모델 갱신 시 디더가 확보한 원본 재질을 함께 추적한다.</summary>
        public void AddRenderer(Renderer renderer, Material[] originals)
        {
            _bindings.Add(new Binding { Renderer = renderer, Originals = originals });
        }

        /// <summary>거리 전환 때만 재질을 교체하며 외부 재질과 Renderer 숨김 상태는 보존한다.</summary>
        public void Evaluate(NpcOutlineLodSettingsSO settings, float distanceSquared, float fieldOfView)
        {
            bool shouldReduce = settings != null && settings.ShouldReduce(IsReduced, distanceSquared, fieldOfView);
            if (shouldReduce == IsReduced)
                return;
            if (!shouldReduce)
            {
                Restore();
                return;
            }

            foreach (Binding binding in _bindings)
            {
                if (binding.Renderer == null)
                    continue;
                binding.Renderer.GetSharedMaterials(_currentMaterials);
                if (!Matches(_currentMaterials, binding.Originals))
                    continue;

                if (binding.Reduced == null)
                    binding.Reduced = CreateReducedMaterials(binding.Originals, settings);
                if (binding.Reduced != null)
                    binding.Renderer.sharedMaterials = binding.Reduced;
            }
            IsReduced = true;
        }

        /// <summary>자신이 교체한 슬롯만 복구하고 중간에 바뀐 외부 재질은 보존한다.</summary>
        public void Restore()
        {
            if (!IsReduced)
                return;
            foreach (Binding binding in _bindings)
            {
                if (binding.Renderer == null || binding.Reduced == null)
                    continue;
                binding.Renderer.GetSharedMaterials(_currentMaterials);
                bool changed = false;
                int count = Mathf.Min(_currentMaterials.Count, binding.Reduced.Length);
                for (int i = 0; i < count; i++)
                {
                    if (binding.Reduced[i] == binding.Originals[i] || _currentMaterials[i] != binding.Reduced[i])
                        continue;
                    _currentMaterials[i] = binding.Originals[i];
                    changed = true;
                }
                if (changed)
                    binding.Renderer.SetSharedMaterials(_currentMaterials);
            }
            IsReduced = false;
        }

        /// <summary>모델 교체·파괴 때 원본을 복구하고 소유한 런타임 재질을 해제한다.</summary>
        public void Dispose()
        {
            Restore();
            foreach (Material material in _materials.Values)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            _materials.Clear();
            _bindings.Clear();
            _currentMaterials.Clear();
        }

        private Material[] CreateReducedMaterials(Material[] originals, NpcOutlineLodSettingsSO settings)
        {
            Material[] reduced = null;
            for (int i = 0; i < originals.Length; i++)
            {
                Material source = originals[i];
                Shader shader = ResolveShader(source, settings);
                if (shader == null || !shader.isSupported)
                    continue;
                if (!_materials.TryGetValue(source, out Material material))
                {
                    // 원본 에셋/Variant를 수정하지 않으며, 폭 0으로 남겨 두는 대신 외곽선 패스 자체를 제거한다.
                    material = new Material(shader) { name = source.name + " (NPC Outline LOD)", hideFlags = HideFlags.DontSave };
                    material.CopyPropertiesFromMaterial(source);
                    material.renderQueue = source.renderQueue;
                    _materials.Add(source, material);
                }
                reduced ??= (Material[])originals.Clone();
                reduced[i] = material;
            }
            return reduced;
        }

        private static Shader ResolveShader(Material source, NpcOutlineLodSettingsSO settings)
        {
            if (source == null || source.shader == null)
                return null;
            // 투명·Multi·Fur 변형은 패스 의미가 다르므로 불투명 셰이더로 대체하지 않는다.
            return source.shader.name switch
            {
                "Hidden/lilToonOutline" => settings.opaqueShader,
                "Hidden/lilToonCutoutOutline" => settings.cutoutShader,
                _ => null
            };
        }

        private static bool Matches(List<Material> actual, Material[] expected)
        {
            if (actual.Count != expected.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }
    }
}
