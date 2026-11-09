using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.CameraSystem.Rendering
{
    /// <summary>선택된 플레이 카메라의 월드 화면에만 가장자리 방사형 블러를 적용한다.</summary>
    public sealed class SpeedRadialBlurFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;
        private Material _material;
        private BlurPass _pass;

        /// <inheritdoc/>
        public override void Create()
        {
            CoreUtils.Destroy(_material);
            _material = _shader != null ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _pass = _material != null ? new BlurPass(_material) : null;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || SpeedRadialBlurState.Strength <= 0f
                || renderingData.cameraData.camera != SpeedRadialBlurState.TargetCamera
                || renderingData.cameraData.cameraType != CameraType.Game
                || renderingData.cameraData.renderType != CameraRenderType.Base) return;
            renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }

        private sealed class BlurPass : ScriptableRenderPass
        {
            private static readonly int ParametersId = Shader.PropertyToID("_SpeedRadialBlur");
            private static readonly int FlowId = Shader.PropertyToID("_SpeedRadialBlurFlow");
            private static readonly int TextureSizeId = Shader.PropertyToID("_SpeedRadialBlurTextureSize");
            private readonly Material _material;
            private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

            public BlurPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                TextureHandle source = resources.activeColorTexture;
                var descriptor = graph.GetTextureDesc(source);
                descriptor.name = "Speed Radial Blur";
                descriptor.clearBuffer = false;
                TextureHandle destination = graph.CreateTexture(descriptor);
                Vector2 radii = SpeedRadialBlurState.Radii;
                _properties.SetVector(ParametersId, new Vector4(SpeedRadialBlurState.Strength, radii.x, radii.y, 0f));
                Vector3 flow = SpeedRadialBlurState.Flow;
                _properties.SetVector(FlowId, new Vector4(flow.x, flow.y, flow.z, SpeedRadialBlurState.MaxRadius));
                var cameraData = frameData.Get<UniversalCameraData>();
                var cameraDescriptor = cameraData.cameraTargetDescriptor;
                _properties.SetVector(TextureSizeId, new Vector4(cameraDescriptor.width, cameraDescriptor.height, 0f, 0f));
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination,
                    _material, 0, _properties);
                graph.AddBlitPass(parameters, passName: "Speed Radial Blur");
                resources.cameraColor = destination;
            }
        }
    }
}
