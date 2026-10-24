using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>식생 머티리얼의 런타임 사본에 날씨 바람을 적용하고 해제 시 원본 참조를 복구한다.</summary>
    internal sealed class EnvironmentVegetationWind
    {
        private static readonly int[] StrengthProperties =
        {
            Shader.PropertyToID("_Sway_Wind_Intensity"),
            Shader.PropertyToID("_Wind_Intensity"),
            Shader.PropertyToID("_Wiggle_Wind_Intensity"),
            Shader.PropertyToID("_Wind_Small_Multiplier"),
            Shader.PropertyToID("_Wind_Large_Multiplier"),
            Shader.PropertyToID("_Wind_Small_Intensity"),
            Shader.PropertyToID("_Wind_Large_Intensity"),
            Shader.PropertyToID("_WindNoise01Multiplier"),
            Shader.PropertyToID("_WindNoise02Multiplier")
        };
        private static readonly int[] DirectionProperties =
        {
            Shader.PropertyToID("_Sway_Wind_Direction"),
            Shader.PropertyToID("_Wind_Direction")
        };

        private readonly List<MaterialState> _materials = new();
        private readonly List<RendererState> _renderers = new();

        /// <summary>명시한 풀·초화 머티리얼만 공유 사본으로 바꿔 나무와 다른 씬을 보존한다.</summary>
        public void Initialize(Transform root, Material[] windMaterials)
        {
            Release();
            if (root == null || windMaterials == null || windMaterials.Length == 0) return;
            var allowed = new HashSet<Material>(windMaterials);
            var copies = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] original = renderer.sharedMaterials;
                Material[] replacement = null;
                for (int i = 0; i < original.Length; i++)
                {
                    Material source = original[i];
                    if (source == null || !allowed.Contains(source) || !HasWind(source)) continue;
                    if (!copies.TryGetValue(source, out Material copy))
                    {
                        copy = new Material(source) { name = source.name + " (Weather)", hideFlags = HideFlags.DontSave };
                        copies.Add(source, copy);
                        _materials.Add(new MaterialState(copy));
                    }
                    replacement ??= (Material[])original.Clone();
                    replacement[i] = copy;
                }
                if (replacement == null) continue;
                _renderers.Add(new RendererState(renderer, original, replacement));
                renderer.sharedMaterials = replacement;
            }
        }

        private static bool HasWind(Material material)
        {
            for (int i = 0; i < StrengthProperties.Length; i++)
                if (material.HasProperty(StrengthProperties[i])) return true;
            return false;
        }

        /// <summary>셰이더 시간 속도는 보존해 날씨 전환 중 흔들림 위상이 튀지 않게 한다.</summary>
        public void Apply(Vector2 wind, float strength)
        {
            float amount = wind.magnitude * strength;
            Vector2 direction = wind.normalized;
            var vector = new Vector4(direction.x, 0, direction.y, 0);
            for (int i = 0; i < _materials.Count; i++)
                _materials[i].Apply(amount, vector);
        }

        /// <summary>이 객체가 교체한 슬롯만 복구하고 런타임 사본을 정리한다.</summary>
        public void Release()
        {
            for (int i = 0; i < _renderers.Count; i++) _renderers[i].Restore();
            _renderers.Clear();
            for (int i = 0; i < _materials.Count; i++)
            {
                Material material = _materials[i].Material;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            _materials.Clear();
        }

        private sealed class MaterialState
        {
            public readonly Material Material;
            private readonly List<int> _strengthIds = new();
            private readonly List<float> _strengths = new();
            private readonly List<int> _directionIds = new();

            public MaterialState(Material material)
            {
                Material = material;
                foreach (int id in StrengthProperties)
                {
                    if (!material.HasProperty(id)) continue;
                    _strengthIds.Add(id);
                    _strengths.Add(material.GetFloat(id));
                }
                foreach (int id in DirectionProperties)
                    if (material.HasProperty(id)) _directionIds.Add(id);
            }

            public void Apply(float amount, Vector4 direction)
            {
                if (Material == null) return;
                for (int i = 0; i < _strengthIds.Count; i++)
                    Material.SetFloat(_strengthIds[i], _strengths[i] * amount);
                for (int i = 0; i < _directionIds.Count; i++)
                    Material.SetVector(_directionIds[i], direction);
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer _renderer;
            private readonly Material[] _original, _replacement;

            public RendererState(Renderer renderer, Material[] original, Material[] replacement)
            {
                _renderer = renderer;
                _original = original;
                _replacement = replacement;
            }

            public void Restore()
            {
                if (_renderer == null) return;
                Material[] current = _renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length && i < _replacement.Length; i++)
                {
                    if (current[i] != _replacement[i]) continue;
                    current[i] = _original[i];
                    changed = true;
                }
                if (changed) _renderer.sharedMaterials = current;
            }
        }
    }
}
