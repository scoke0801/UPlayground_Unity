using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.CameraSystem.Rendering
{
    /// <summary>
    /// 카메라 가림 페이드 대상 렌더러를 원본 셰이더 그대로 그리되 스텐실 디더로 소거한다.
    /// 대상은 <see cref="CameraOcclusionFader"/>가 만든 프록시 렌더러이며 프록시 레이어는 렌더러 데이터가
    /// 메인 패스에서 제외하므로, 이 패스가 불투명 직후에 슬롯별 가시 단계로 대신 그린다.
    /// 렌더러 데이터의 Opaque/Transparent/Prepass Layer Mask에서 프록시 레이어(CameraFadeProxy)를 빼 두어야 한다.
    /// </summary>
    public sealed class CameraOccluderFadeFeature : ScriptableRendererFeature
    {
        private const string MaskShaderName = "Hidden/UPlayGround/CameraOccluderDitherMask";

        [Tooltip("스텐실 베이어 마스크 셰이더. 비워 두면 이름으로 찾는다(빌드에서는 반드시 지정).")]
        [SerializeField] private Shader _maskShader;

        [Tooltip("페이드 대상을 다시 그릴 시점. 불투명 직후, 스카이박스 이전이어야 깊이가 올바르다.")]
        [SerializeField] private RenderPassEvent _passEvent = RenderPassEvent.AfterRenderingOpaques;

        private Material _maskMaterial;
        private OccluderFadePass _pass;
        private bool _isRegistered;

        /// <inheritdoc/>
        public override void Create()
        {
            ReleaseMaterial();
            Shader shader = _maskShader != null ? _maskShader : Shader.Find(MaskShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[CameraOccluderFadeFeature] 마스크 셰이더를 찾지 못했습니다: {MaskShaderName}");
                return;
            }

            _maskMaterial = CoreUtils.CreateEngineMaterial(shader);
            _pass = new OccluderFadePass(_maskMaterial) { renderPassEvent = _passEvent };
            if (_isRegistered) return;
            _isRegistered = true;
            CameraOccluderFadeState.RegisterFeature();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || CameraOccluderFadeState.ActiveSlotMask == 0) return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) return;

            _pass.renderPassEvent = _passEvent;
            renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _pass = null;
            ReleaseMaterial();
            if (!_isRegistered) return;
            _isRegistered = false;
            CameraOccluderFadeState.UnregisterFeature();
        }

        private void ReleaseMaterial()
        {
            if (_maskMaterial != null) CoreUtils.Destroy(_maskMaterial);
            _maskMaterial = null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("CameraOccluderDitherMask t:Shader");
            if (guids.Length > 0)
                _maskShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }
#endif

        private sealed class OccluderFadePass : ScriptableRenderPass
        {
            private const int StencilBitShift = 4;
            private const byte StencilReadMask = 0xF0;
            private const int PlaneCount = 4;
            private const int ClearPassIndex = 4;
            private static readonly int PixelScaleId = Shader.PropertyToID("_PixelScale");
            private static readonly int MaxLists = CameraOccluderFadeState.MaxSlots * 2;

            private static readonly List<ShaderTagId> ShaderTags = new()
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
            };

            private readonly Material _maskMaterial;

            public OccluderFadePass(Material maskMaterial)
            {
                _maskMaterial = maskMaterial;
                profilingSampler = new ProfilingSampler("Camera Occluder Fade");
            }

            private sealed class PassData
            {
                public Material MaskMaterial;
                public readonly RendererListHandle[] Lists = new RendererListHandle[MaxLists];
                public int ListCount;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                using IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass<PassData>(passName, out PassData passData, profilingSampler);

                passData.MaskMaterial = _maskMaterial;
                passData.ListCount = 0;
                _maskMaterial.SetFloat(PixelScaleId, CameraOccluderFadeState.PixelScale);

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                UseSharedTextures(builder, resourceData);

                BuildRendererLists(renderGraph, builder, passData, cameraData, renderingData, lightData);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => Execute(data, context));
            }

            private static void UseSharedTextures(IRasterRenderGraphBuilder builder, UniversalResourceData resourceData)
            {
                if (resourceData.mainShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.mainShadowsTexture, AccessFlags.Read);
                if (resourceData.additionalShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.additionalShadowsTexture, AccessFlags.Read);
                if (resourceData.ssaoTexture.IsValid())
                    builder.UseTexture(resourceData.ssaoTexture, AccessFlags.Read);

                TextureHandle[] dBuffer = resourceData.dBuffer;
                for (int i = 0; i < dBuffer.Length; i++)
                    if (dBuffer[i].IsValid())
                        builder.UseTexture(dBuffer[i], AccessFlags.Read);
            }

            private static void BuildRendererLists(
                RenderGraph renderGraph,
                IRasterRenderGraphBuilder builder,
                PassData passData,
                UniversalCameraData cameraData,
                UniversalRenderingData renderingData,
                UniversalLightData lightData)
            {
                int activeMask = CameraOccluderFadeState.ActiveSlotMask;
                for (int slot = 0; slot < CameraOccluderFadeState.MaxSlots; slot++)
                {
                    if ((activeMask & (1 << slot)) == 0) continue;

                    int level = CameraOccluderFadeState.GetLevel(slot);
                    uint layerMask = CameraOccluderFadeState.GetRenderingLayerMask(slot);
                    RenderStateBlock stateBlock = CreateStencilBlock(level);

                    AddList(renderGraph, builder, passData, cameraData, renderingData, lightData,
                        RenderQueueRange.opaque, layerMask, cameraData.defaultOpaqueSortFlags, stateBlock);
                    AddList(renderGraph, builder, passData, cameraData, renderingData, lightData,
                        RenderQueueRange.transparent, layerMask, SortingCriteria.CommonTransparent, stateBlock);
                }
            }

            private static void AddList(
                RenderGraph renderGraph,
                IRasterRenderGraphBuilder builder,
                PassData passData,
                UniversalCameraData cameraData,
                UniversalRenderingData renderingData,
                UniversalLightData lightData,
                RenderQueueRange queueRange,
                uint renderingLayerMask,
                SortingCriteria sorting,
                RenderStateBlock stateBlock)
            {
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
                    ShaderTags, renderingData, cameraData, lightData, sorting);
                var filtering = new FilteringSettings(queueRange, CameraOccluderFadeState.ProxyLayerMask, renderingLayerMask);

                var tagValues = new Unity.Collections.NativeArray<ShaderTagId>(1, Unity.Collections.Allocator.Temp);
                var stateBlocks = new Unity.Collections.NativeArray<RenderStateBlock>(1, Unity.Collections.Allocator.Temp);
                tagValues[0] = ShaderTagId.none;
                stateBlocks[0] = stateBlock;
                var rendererListParams = new RendererListParams(renderingData.cullResults, drawing, filtering)
                {
                    tagValues = tagValues,
                    stateBlocks = stateBlocks,
                    isPassTagName = false,
                };

                RendererListHandle handle = renderGraph.CreateRendererList(rendererListParams);
                builder.UseRendererList(handle);
                passData.Lists[passData.ListCount++] = handle;
            }

            /// <summary>가시 단계 level에서 베이어 값이 level 미만인 픽셀만 통과시키는 스텐실 상태를 만든다.</summary>
            private static RenderStateBlock CreateStencilBlock(int level)
            {
                bool isFullyVisible = level >= CameraOccluderFadeState.Levels;
                var stencil = new StencilState(
                    enabled: true,
                    readMask: StencilReadMask,
                    writeMask: 0,
                    compareFunction: isFullyVisible ? CompareFunction.Always : CompareFunction.Greater,
                    passOperation: StencilOp.Keep,
                    failOperation: StencilOp.Keep,
                    zFailOperation: StencilOp.Keep);
                return new RenderStateBlock(RenderStateMask.Stencil)
                {
                    stencilState = stencil,
                    stencilReference = Mathf.Clamp(level, 0, CameraOccluderFadeState.Levels - 1) << StencilBitShift,
                };
            }

            private static void Execute(PassData data, RasterGraphContext context)
            {
                RasterCommandBuffer cmd = context.cmd;
                for (int plane = 0; plane < PlaneCount; plane++)
                    cmd.DrawProcedural(Matrix4x4.identity, data.MaskMaterial, plane, MeshTopology.Triangles, 3);

                for (int i = 0; i < data.ListCount; i++)
                    cmd.DrawRendererList(data.Lists[i]);

                cmd.DrawProcedural(Matrix4x4.identity, data.MaskMaterial, ClearPassIndex, MeshTopology.Triangles, 3);
            }
        }
    }
}
