using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World
{
    /// <summary>호수 안의 카메라에만 깊이 기반 색 흡수를 적용하며 날씨 전역 상태를 보존한다.</summary>
    public sealed class UnderwaterRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;
        private Material _material;
        private UnderwaterPass _pass;

        /// <inheritdoc/>
        public override void Create()
        {
            CoreUtils.Destroy(_material);
            _material = _shader != null ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _pass = _material != null ? new UnderwaterPass(_material) : null;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (_pass == null || camera.renderType != CameraRenderType.Base
                || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)) return;
            if (UnderwaterRegion.TryResolve(camera.camera.transform.position, out _, out _))
                renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }

        private sealed class UnderwaterPass : ScriptableRenderPass
        {
            private static readonly int DepthId = Shader.PropertyToID("_LakeDepthTexture");
            private static readonly int ColorId = Shader.PropertyToID("_LakeScattering");
            private static readonly int ExtinctionId = Shader.PropertyToID("_LakeExtinction");
            private static readonly int SurfaceId = Shader.PropertyToID("_LakeSurface");
            private static readonly int CausticId = Shader.PropertyToID("_LakeCaustic");
            private readonly Material _material;

            public UnderwaterPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            private sealed class PassData
            {
                public TextureHandle Source, Depth;
                public Material Material;
                public Color Scattering;
                public Vector4 Extinction, Surface, Caustic;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var camera = frameData.Get<UniversalCameraData>();
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                Vector3 position = camera.camera.transform.position;
                if (!UnderwaterRegion.TryResolve(position, out UnderwaterProfileSO profile, out float height)) return;
                var description = graph.GetTextureDesc(resources.activeColorTexture);
                description.name = "Lake Underwater";
                description.clearBuffer = false;
                TextureHandle destination = graph.CreateTexture(description);
                using var builder = graph.AddRasterRenderPass<PassData>("Lake Underwater", out var data);
                data.Source = resources.activeColorTexture;
                data.Depth = resources.cameraDepthTexture;
                data.Material = _material;
                data.Scattering = profile.scatteringColor.linear;
                Vector3 extinction = profile.extinction;
                data.Extinction = new Vector4(extinction.x, extinction.y, extinction.z, profile.maximumVisibility);
                data.Surface = new Vector4(height, profile.EvaluateImmersion(height - position.y), profile.minimumLight, 0);
                data.Caustic = new Vector4(profile.causticStrength, profile.causticScale, profile.causticSpeed, 0);
                builder.UseTexture(data.Source);
                builder.UseTexture(data.Depth);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
                {
                    pass.Material.SetColor(ColorId, pass.Scattering);
                    pass.Material.SetVector(ExtinctionId, pass.Extinction);
                    pass.Material.SetVector(SurfaceId, pass.Surface);
                    pass.Material.SetVector(CausticId, pass.Caustic);
                    context.cmd.SetGlobalTexture(DepthId, pass.Depth);
                    Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1, 1, 0, 0), pass.Material, 0);
                });
                resources.cameraColor = destination;
            }
        }
    }
}
