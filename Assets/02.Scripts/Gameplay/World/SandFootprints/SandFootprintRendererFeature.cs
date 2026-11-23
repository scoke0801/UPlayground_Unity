using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Gameplay.World
{
    public sealed class SandFootprintRendererFeature : ScriptableRendererFeature
    {
        private UpdatePass _pass;

        public override void Create() => _pass = new UpdatePass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses - 1
        };

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying || renderingData.cameraData.cameraType != CameraType.Game) return;
            var world = SandFootprintWorld.Active;
            if (world != null && world.Prepare()) renderer.EnqueuePass(_pass);
        }

        private sealed class UpdatePass : ScriptableRenderPass
        {
            private sealed class PassData
            {
                public SandFootprintWorld World;
                public bool Owner;
            }

            private static bool IsOwner(Camera camera, CameraRenderType renderType)
            {
                if (renderType != CameraRenderType.Base) return false;
                var main = Camera.main;
                return main == null || main == camera;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var world = SandFootprintWorld.Active;
                if (world == null || world.CacheHandle == null) return;
                using var builder = graph.AddUnsafePass<PassData>("모래 발자국 타일", out var pass);
                pass.World = world;
                var cameraData = frameData.Get<UniversalCameraData>();
                pass.Owner = IsOwner(cameraData.camera, cameraData.renderType);
                var cache = graph.ImportTexture(world.CacheHandle);
                builder.UseTexture(cache, pass.Owner ? AccessFlags.ReadWrite : AccessFlags.Read);
                if (pass.Owner) builder.UseTexture(graph.ImportTexture(world.ScratchHandle), AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(cache, Shader.PropertyToID(SandFootprintWorld.CacheProperty));
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                    data.World.Record(CommandBufferHelpers.GetNativeCommandBuffer(context.cmd), data.Owner));
            }

            [System.Obsolete("URP 호환 모드의 발자국 갱신 경로")]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var world = SandFootprintWorld.Active;
                if (world == null) return;
                var cmd = CommandBufferPool.Get("모래 발자국 타일");
                try
                {
                    world.Record(cmd, IsOwner(renderingData.cameraData.camera, renderingData.cameraData.renderType));
                    context.ExecuteCommandBuffer(cmd);
                }
                finally { CommandBufferPool.Release(cmd); }
            }
        }
    }
}
