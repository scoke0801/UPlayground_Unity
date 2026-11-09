using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 함께 흐려지는 렌더러 묶음과 그 페이드 상태.
    /// 흐려지는 동안 원본은 그림자만 남기고(ShadowsOnly) 같은 메시·머티리얼의 프록시가 전용 레이어에서 대신 그려진다.
    /// 프록시는 메인 패스에서 제외된 레이어에 있어 RendererFeature만 그리므로, 임의 셰이더를 수정하지 않고 디더할 수 있다.
    /// </summary>
    internal sealed class OccluderGroup
    {
        private readonly Renderer[] _renderers;
        private readonly ShadowCastingMode[] _originalShadowModes;
        private readonly GameObject[] _proxies;
        private readonly Renderer[] _proxyRenderers;

        /// <summary>현재 가시율(1=완전 표시, 최소값=가장 흐림).</summary>
        public float Visibility = 1f;

        /// <summary>시야선에서 벗어난 뒤 복귀를 시작하기까지 남은 시간.</summary>
        public float HoldTimer;

        /// <summary>할당된 렌더링 레이어 슬롯. 비활성이면 -1.</summary>
        public int Slot = -1;

        /// <summary>프록시로 교체된 상태인지 여부.</summary>
        public bool IsEngaged => Slot >= 0;

        public OccluderGroup(Renderer[] renderers)
        {
            _renderers = renderers;
            _originalShadowModes = new ShadowCastingMode[renderers.Length];
            _proxies = new GameObject[renderers.Length];
            _proxyRenderers = new Renderer[renderers.Length];
        }

        /// <summary>원본을 그림자 전용으로 돌리고 슬롯 비트를 단 프록시를 만든다.</summary>
        public void Engage(int slot, int proxyLayer)
        {
            Slot = slot;
            Visibility = 1f;
            uint slotMask = CameraOccluderFadeState.GetRenderingLayerMask(slot);
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer source = _renderers[i];
                if (source == null)
                    continue;

                _proxies[i] = CreateProxy(source, proxyLayer, slotMask);
                _proxyRenderers[i] = _proxies[i].GetComponent<Renderer>();
                _originalShadowModes[i] = source.shadowCastingMode;
                source.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        /// <summary>프록시를 제거하고 원본 그림자 모드를 복원한다.</summary>
        public void Release()
        {
            if (!IsEngaged) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_proxies[i] == null) continue;
                DestroyProxy(_proxies[i]);
                _proxies[i] = null;
                _proxyRenderers[i] = null;
                if (_renderers[i] != null)
                    _renderers[i].shadowCastingMode = _originalShadowModes[i];
            }
            Slot = -1;
            Visibility = 1f;
            HoldTimer = 0f;
        }

        /// <summary>표시 상태와 블렌드 셰이프를 원본과 동기화한다.</summary>
        public void SyncRenderers()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer source = _renderers[i];
                Renderer proxy = _proxyRenderers[i];
                if (proxy == null) continue;
                proxy.enabled = source != null && source.enabled;
                if (source is not SkinnedMeshRenderer skin || proxy is not SkinnedMeshRenderer skinProxy || skin.sharedMesh == null) continue;
                skinProxy.localBounds = skin.localBounds;
                for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
                    skinProxy.SetBlendShapeWeight(shape, skin.GetBlendShapeWeight(shape));
            }
        }

        private static GameObject CreateProxy(Renderer source, int layer, uint slotMask)
        {
            var go = new GameObject("CameraFadeProxy") { hideFlags = HideFlags.HideAndDontSave, layer = layer };
            go.transform.SetParent(source.transform, false);
            Renderer proxy;
            if (source is SkinnedMeshRenderer skin)
            {
                var skinProxy = go.AddComponent<SkinnedMeshRenderer>();
                skinProxy.sharedMesh = skin.sharedMesh;
                skinProxy.bones = skin.bones;
                skinProxy.rootBone = skin.rootBone;
                skinProxy.localBounds = skin.localBounds;
                skinProxy.quality = skin.quality;
                skinProxy.updateWhenOffscreen = skin.updateWhenOffscreen;
                proxy = skinProxy;
            }
            else
            {
                go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                proxy = go.AddComponent<MeshRenderer>();
            }
            proxy.sharedMaterials = source.sharedMaterials;
            proxy.shadowCastingMode = ShadowCastingMode.Off;
            proxy.receiveShadows = source.receiveShadows;
#if UNITY_EDITOR
            // receiveGI는 에디터 전용 API라 플레이어 빌드에서는 컴파일되지 않는다.
            if (source is MeshRenderer meshSource && proxy is MeshRenderer meshProxy)
                meshProxy.receiveGI = meshSource.receiveGI;
#endif
            proxy.lightmapIndex = source.lightmapIndex;
            proxy.lightmapScaleOffset = source.lightmapScaleOffset;
            proxy.realtimeLightmapIndex = source.realtimeLightmapIndex;
            proxy.realtimeLightmapScaleOffset = source.realtimeLightmapScaleOffset;
            proxy.lightProbeUsage = source.lightProbeUsage;
            proxy.reflectionProbeUsage = source.reflectionProbeUsage;
            proxy.probeAnchor = source.probeAnchor;
            proxy.motionVectorGenerationMode = source.motionVectorGenerationMode;
            // 라이트·데칼이 같은 렌더링 레이어로 판정하도록 원본 비트를 유지하고 슬롯 비트만 더한다.
            proxy.renderingLayerMask = source.renderingLayerMask | slotMask;

            if (source.HasPropertyBlock())
            {
                var block = new MaterialPropertyBlock();
                source.GetPropertyBlock(block);
                proxy.SetPropertyBlock(block);
            }
            return go;
        }

        private static void DestroyProxy(GameObject proxy)
        {
            if (Application.isPlaying) Object.Destroy(proxy);
            else Object.DestroyImmediate(proxy);
        }
    }
}
