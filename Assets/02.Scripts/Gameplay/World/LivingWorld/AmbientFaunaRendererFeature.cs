using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>GPU 군집을 URP RenderGraph의 명시적 색상·깊이/노멀 패스에서 출력한다.</summary>
    public sealed class AmbientFaunaRendererFeature : ScriptableRendererFeature
    {
        internal static readonly List<AmbientFaunaGroup> Groups = new();
        private FaunaPass _colorPass, _depthPass;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Groups.Clear();

        /// <inheritdoc/>
        public override void Create()
        {
            _colorPass = new FaunaPass(false) { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
            _depthPass = new FaunaPass(true) { renderPassEvent = RenderPassEvent.AfterRenderingPrePasses };
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (Groups.Count == 0 || camera.renderType != CameraRenderType.Base
                || (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)) return;
            for (int i = 0; i < Groups.Count; i++)
                if (Groups[i] != null && (camera.camera.cullingMask & (1 << Groups[i].gameObject.layer)) != 0)
                    Groups[i].MarkRendererAvailable();
            renderer.EnqueuePass(_depthPass);
            renderer.EnqueuePass(_colorPass);
        }

        private sealed class FaunaPass : ScriptableRenderPass
        {
            private readonly bool _isDepth;
            private readonly Plane[] _planes = new Plane[6];
            private readonly List<AmbientFaunaGroup> _visible = new(32);
            private sealed class PassData { public List<AmbientFaunaGroup> Groups; public int ShaderPass; }
            public FaunaPass(bool isDepth) => _isDepth = isDepth;

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var camera = frameData.Get<UniversalCameraData>().camera;
                var resources = frameData.Get<UniversalResourceData>();
                TextureHandle depth = resources.activeDepthTexture;
                bool hasNormals = _isDepth && resources.cameraNormalsTexture.IsValid();
                if (_isDepth && resources.cameraDepthTexture.IsValid()
                    && graph.GetTextureDesc(resources.cameraDepthTexture).depthBufferBits != DepthBits.None)
                    depth = resources.cameraDepthTexture;
                if (!depth.IsValid()) return;
                _visible.Clear();
                GeometryUtility.CalculateFrustumPlanes(camera, _planes);
                for (int i = 0; i < Groups.Count; i++)
                {
                    var group = Groups[i];
                    if (group == null || !group.CanRender || (camera.cullingMask & (1 << group.gameObject.layer)) == 0
                        || !GeometryUtility.TestPlanesAABB(_planes, group.WorldBounds)) continue;
                    _visible.Add(group);
                }
                if (_visible.Count == 0) return;
                using var builder = graph.AddRasterRenderPass<PassData>(_isDepth ? "Ambient Fauna Depth" : "Ambient Fauna Color", out var data);
                data.Groups = _visible;
                data.ShaderPass = _isDepth ? (hasNormals ? 2 : 1) : 0;
                if (!_isDepth) builder.SetRenderAttachment(resources.activeColorTexture, 0);
                else if (hasNormals) builder.SetRenderAttachment(resources.cameraNormalsTexture, 0);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                for (int i = 0; i < _visible.Count; i++)
                {
                    builder.UseBuffer(graph.ImportBuffer(_visible[i].ParticleBuffer), AccessFlags.Read);
                    builder.UseBuffer(graph.ImportBuffer(_visible[i].ArgumentBuffer), AccessFlags.Read);
                }
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                {
                    for (int i = 0; i < pass.Groups.Count; i++)
                    {
                        var group = pass.Groups[i];
                        if (!group.CanRender) continue;
                        context.cmd.DrawMeshInstancedIndirect(group.DrawMesh, 0, group.DrawMaterial,
                            pass.ShaderPass, group.ArgumentBuffer, 0, group.DrawProperties);
                    }
                });
            }
        }
    }
}
