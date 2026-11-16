using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>반응 슬롯과 반응하지 않는 슬롯을 모델 기준으로 명시한다.</summary>
    [Serializable]
    public sealed class EnvironmentSurfaceBinding
    {
        public Renderer renderer;
        public int[] slots;
        public int[] excludedSlots;
    }

    /// <summary>원본 재질과 기존 슬롯 PropertyBlock을 보존하고 상태만 전달한다.</summary>
    public sealed class ReactiveEnvironmentSurface
    {
        private static readonly int State0 = Shader.PropertyToID("_EnvState0");
        private static readonly int State1 = Shader.PropertyToID("_EnvState1");
        private static readonly int Coverage = Shader.PropertyToID("_FuelCoverage");
        private readonly EnvironmentSurfaceBinding[] _bindings;
        private readonly MaterialPropertyBlock[][] _originals;
        private readonly MaterialPropertyBlock[] _rendererOriginals;
        private readonly bool _useSharedBuffer;
        private bool _useRendererUserValue;
        private const uint RendererUserValueFlag = 0x80000000u;
        private uint _rendererUserValue;
        private int _bufferIndex = -1;
        private uint _bufferEpoch;
        private readonly MaterialPropertyBlock _properties = new();
        private bool _hasApplied;
        private Vector4 _state0, _state1;
        private float _coverage;
        public int ApplyCount { get; private set; }
        private bool _hasWound;
        private Vector3 _woundPosition;
        private float _woundRadius;
        internal bool HasWound => _hasWound && _woundRadius > 0;

        /// <summary>저장된 누적 내구도를 국소 상처로 표시하며 재질 인스턴스를 만들지 않는다.</summary>
        public void ApplyWound(Vector3 position, float radius)
        {
            if (!_hasWound && radius <= 0) return;
            if (_hasWound && _woundPosition == position && _woundRadius == radius) return;
            _hasWound = true; _woundPosition = position; _woundRadius = radius;
            foreach (var binding in _bindings)
            {
                Vector3 local = binding.renderer.transform.InverseTransformPoint(position);
                float scale = Mathf.Max(0.001f, binding.renderer.transform.lossyScale.x);
                foreach (int slot in binding.slots)
                {
                    binding.renderer.GetPropertyBlock(_properties, slot);
                    _properties.SetVector("_EnvImpact", new Vector4(local.x, local.y, local.z, radius / scale));
                    binding.renderer.SetPropertyBlock(_properties, slot);
                }
            }
        }

        public ReactiveEnvironmentSurface(Renderer[] renderers) : this(BindAll(renderers)) { }

        public ReactiveEnvironmentSurface(EnvironmentSurfaceBinding[] bindings, bool allowSharedBuffer = true)
        {
            _bindings = bindings;
            _useSharedBuffer = allowSharedBuffer && Application.isPlaying && EnvironmentSurfaceStateBuffer.IsSupported
                && SupportsSharedBuffer(bindings);
            _useRendererUserValue = _useSharedBuffer && SupportsRendererUserValue(bindings);
            if (_useSharedBuffer) _rendererOriginals = new MaterialPropertyBlock[bindings.Length];
            _originals = new MaterialPropertyBlock[bindings.Length][];
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (_useSharedBuffer) _rendererOriginals[i] = new MaterialPropertyBlock();
                _originals[i] = new MaterialPropertyBlock[binding.slots.Length];
                for (int j = 0; j < binding.slots.Length; j++)
                {
                    var block = new MaterialPropertyBlock();
                    binding.renderer.GetPropertyBlock(block, binding.slots[j]);
                    _originals[i][j] = block;
                }
            }
        }

        private static bool SupportsSharedBuffer(EnvironmentSurfaceBinding[] bindings)
        {
            foreach (var binding in bindings)
            {
                var materials = binding.renderer.sharedMaterials;
                foreach (int slot in binding.slots)
                {
                    var material = materials[slot];
                    if (material == null || !material.HasProperty("_EnvironmentReactionBufferSupported")
                        || material.GetFloat("_EnvironmentReactionBufferSupported") != 1) return false;
                }
            }
            return true;
        }

        private static bool SupportsRendererUserValue(EnvironmentSurfaceBinding[] bindings)
        {
            foreach (var binding in bindings)
            {
                if (binding.renderer is not MeshRenderer mesh || mesh.GetShaderUserValue() != 0
                    || mesh.HasPropertyBlock() || binding.slots.Length != mesh.sharedMaterials.Length) return false;
            }
            return true;
        }

        /// <summary>이전 전체 Renderer 저작을 같은 슬롯 계약으로 해석한다.</summary>
        public static EnvironmentSurfaceBinding[] BindAll(Renderer[] renderers)
        {
            if (renderers == null) return Array.Empty<EnvironmentSurfaceBinding>();
            var bindings = new EnvironmentSurfaceBinding[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                int count = renderers[i] != null ? renderers[i].sharedMaterials.Length : 0;
                var slots = new int[count];
                for (int j = 0; j < count; j++) slots[j] = j;
                bindings[i] = new EnvironmentSurfaceBinding { renderer = renderers[i], slots = slots };
            }
            return bindings;
        }

        /// <summary>기존 전체 슬롯 바인딩을 검사한다.</summary>
        public static string Validate(Transform root, Renderer[] renderers) => Validate(root, BindAll(renderers), null);

        /// <summary>모든 Renderer/LOD/슬롯에 반응 또는 명시 제외 결정을 요구한다.</summary>
        public static string Validate(Transform root, EnvironmentSurfaceBinding[] bindings, Renderer[] excluded)
        {
            if (bindings == null || bindings.Length == 0) return "반응 표면이 비어 있습니다.";
            var mapped = new HashSet<Renderer>();
            foreach (var binding in bindings)
            {
                if (binding == null || binding.renderer == null || !binding.renderer.transform.IsChildOf(root)
                    || !mapped.Add(binding.renderer) || binding.slots == null || binding.slots.Length == 0)
                    return "반응 표면의 누락·중복·소유 계층을 확인하세요.";
                var materials = binding.renderer.sharedMaterials;
                var slots = new HashSet<int>();
                foreach (int slot in binding.slots)
                {
                    if (slot < 0 || slot >= materials.Length || !slots.Add(slot)) return "반응 슬롯 범위·중복 오류입니다.";
                    var material = materials[slot];
                    if (!SupportsReaction(material))
                        return "호환 툰 재질을 프로젝트 소유 복사본에 연결하세요.";
                    var collider = binding.renderer.GetComponent<Collider>();
                    if (material.GetFloat("_AllowFuelCoverage") > 0 && collider != null && collider.enabled)
                        return "잎 소거 표면에 게임플레이 Collider를 둘 수 없습니다.";
                }
                if (binding.excludedSlots != null)
                    foreach (int slot in binding.excludedSlots)
                        if (slot < 0 || slot >= materials.Length || !slots.Add(slot)) return "제외 슬롯 범위·중복 오류입니다.";
                if (slots.Count != materials.Length) return "각 재질 슬롯의 반응 또는 제외를 명시하세요.";
            }
            if (excluded != null)
                foreach (var renderer in excluded)
                    if (renderer == null || !renderer.transform.IsChildOf(root) || !mapped.Add(renderer))
                        return "제외 Renderer의 소유·중복 오류입니다.";
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (!mapped.Contains(renderer)) return "Renderer/LOD의 반응 또는 제외 결정이 누락되었습니다.";
            return null;
        }

        /// <summary>원본 식생 셰이더 파생도 같은 상태·슬롯 계약으로 사용한다.</summary>
        public static bool SupportsReaction(Material material) => material != null
            && (material.shader.name == "UPlayGround/Environment/Reactive Toon"
                || material.HasProperty("_EnvironmentReactionSupported") && material.GetFloat("_EnvironmentReactionSupported") == 1
                && material.HasProperty("_EnvState0") && material.HasProperty("_FuelCoverage"));

        /// <summary>동일 표현 상태는 건너뛰며 재질 인스턴스를 만들지 않는다.</summary>
        public void Apply(EnvironmentReactionState state, float maximumHealth, float initialFuel, float clearing = 0)
        {
            Vector4 surface = new(state.wetness, state.charAmount,
                1 - Mathf.Clamp01(state.durability / maximumHealth), state.combustion == EnvironmentCombustion.Burning ? 1 : 0);
            Vector4 thermal = new(state.heat, 0, clearing, 0);
            float coverage = initialFuel > 0 ? Mathf.Clamp01(state.fuel / initialFuel) : 1;
            if (_useSharedBuffer && _bufferIndex >= 0 && !EnvironmentSurfaceStateBuffer.IsValid(_bufferIndex, _bufferEpoch))
            { ReleaseRendererUserValue(); _bufferIndex = -1; _hasApplied = false; }
            if (_hasApplied && _state0 == surface && _state1 == thermal && _coverage == coverage) return;
            if (!_hasApplied)
                for (int i = 0; i < _bindings.Length; i++)
                    if (_bindings[i].renderer != null)
                    {
                        if (_useSharedBuffer) _bindings[i].renderer.GetPropertyBlock(_rendererOriginals[i]);
                        for (int j = 0; j < _bindings[i].slots.Length; j++)
                            _bindings[i].renderer.GetPropertyBlock(_originals[i][j], _bindings[i].slots[j]);
                    }
            _hasApplied = true; _state0 = surface; _state1 = thermal; _coverage = coverage;
            ApplyCount++;
            if (_useSharedBuffer)
            {
                if (_bufferIndex < 0)
                {
                    _bufferIndex = EnvironmentSurfaceStateBuffer.Register(surface, thermal, coverage);
                    _bufferEpoch = EnvironmentSurfaceStateBuffer.Epoch;
                    BindBufferIndex();
                }
                else EnvironmentSurfaceStateBuffer.Set(_bufferIndex, surface, thermal, coverage);
                return;
            }
            foreach (var binding in _bindings)
            {
                if (binding.renderer == null) continue;
                foreach (int slot in binding.slots)
                {
                    binding.renderer.GetPropertyBlock(_properties, slot);
                    if (_properties.isEmpty) binding.renderer.GetPropertyBlock(_properties);
                    _properties.SetVector(State0, surface);
                    _properties.SetVector(State1, thermal);
                    _properties.SetFloat(Coverage, coverage);
                    binding.renderer.SetPropertyBlock(_properties, slot);
                }
            }
        }

        private void BindBufferIndex()
        {
            _useRendererUserValue &= SupportsRendererUserValue(_bindings);
            if (_useRendererUserValue)
            {
                // 개별 MPB는 GPU Resident Drawer를 탈락시키므로 전용 렌더러 값으로 인덱스만 전달한다.
                _rendererUserValue = RendererUserValueFlag | (uint)_bufferIndex;
                foreach (var binding in _bindings)
                    if (binding.renderer is MeshRenderer mesh) mesh.SetShaderUserValue(_rendererUserValue);
                return;
            }
            foreach (var binding in _bindings)
            {
                if (binding.renderer == null) continue;
                binding.renderer.GetPropertyBlock(_properties);
                _properties.SetFloat(EnvironmentSurfaceStateBuffer.IndexProperty, _bufferIndex);
                binding.renderer.SetPropertyBlock(_properties);
                foreach (int slot in binding.slots)
                {
                    binding.renderer.GetPropertyBlock(_properties, slot);
                    if (_properties.isEmpty) binding.renderer.GetPropertyBlock(_properties);
                    _properties.SetFloat(EnvironmentSurfaceStateBuffer.IndexProperty, _bufferIndex);
                    binding.renderer.SetPropertyBlock(_properties, slot);
                }
            }
        }

        private void ReleaseRendererUserValue()
        {
            if (_rendererUserValue == 0) return;
            foreach (var binding in _bindings)
                if (binding.renderer is MeshRenderer mesh && mesh.GetShaderUserValue() == _rendererUserValue)
                    mesh.SetShaderUserValue(0);
            _rendererUserValue = 0;
        }

        /// <summary>반응 슬롯만 원래 PropertyBlock으로 복원한다.</summary>
        public void Release()
        {
            if (!_hasApplied) return;
            // 전용 값만 사용한 동안 뒤늦게 추가된 다른 모듈의 MPB는 소유하지 않는다.
            bool restoreProperties = !_useRendererUserValue || _hasWound;
            ReleaseRendererUserValue();
            for (int i = 0; restoreProperties && i < _bindings.Length; i++)
            {
                var binding = _bindings[i];
                if (binding.renderer == null) continue;
                for (int j = 0; j < binding.slots.Length; j++)
                    binding.renderer.SetPropertyBlock(_originals[i][j].isEmpty ? null : _originals[i][j], binding.slots[j]);
                if (_useSharedBuffer)
                    binding.renderer.SetPropertyBlock(_rendererOriginals[i].isEmpty ? null : _rendererOriginals[i]);
            }
            if (_bufferIndex >= 0) EnvironmentSurfaceStateBuffer.Release(_bufferIndex, _bufferEpoch);
            _bufferIndex = -1;
            _hasApplied = false;
            _hasWound = false;
        }
    }
}
