using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Gameplay.World
{
    /// <summary>기존 RendererData 연결을 유지하며 수면·수중·공기 안개를 한 번 합성한다.</summary>
    public sealed class UnderwaterRendererFeature : ScriptableRendererFeature
    {
        public enum DebugView { None, Body, OpticalLength, SurfaceNormal, FogOwner, IntervalBudget }
        [SerializeField] private Shader _shader;
        [SerializeField] private bool _showInSceneView = true;
        [SerializeField] private DebugView _debugView;
        private Material _material;
        private PreparePass _prepare;
        private ResolvePass _resolve;
        private CleanupPass _cleanup;
        public static long ResolveExecutionCount { get; private set; }
        public static int LastResolvedCamera { get; private set; }
        public static int LastBodyCount { get; private set; }
        public static int LastCellCount { get; private set; }
        public static int LastRejectedCount { get; private set; }
        public static int LastColorSamples { get; private set; }
        public static int LastDepthSamples { get; private set; }

        public override void Create()
        {
            LinearFog = GlobalKeyword.Create("FOG_LINEAR");
            ExpFog = GlobalKeyword.Create("FOG_EXP");
            Exp2Fog = GlobalKeyword.Create("FOG_EXP2");
            CoreUtils.Destroy(_material);
            _material = _shader != null ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _prepare = new PreparePass(this);
            _resolve = new ResolvePass(this, _material);
            _cleanup = new CleanupPass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData;
            if (_material == null || !_material.shader.isSupported || SystemInfo.graphicsShaderLevel < 45
                || !SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.Render)
                || camera.renderType != CameraRenderType.Base
                || (camera.cameraType != CameraType.Game && !(_showInSceneView && camera.cameraType == CameraType.SceneView))
                || !WaterRenderSnapshot.HasRenderableRegions(camera.camera)) return;
            renderer.EnqueuePass(_prepare);
            renderer.EnqueuePass(_resolve);
            renderer.EnqueuePass(_cleanup);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            Shader.SetGlobalFloat(ActiveId, 0f);
            Shader.SetGlobalFloat(SubmergedId, 0f);
        }

        private static readonly int ActiveId = Shader.PropertyToID("_WaterSceneActive");
        private static readonly int SubmergedId = Shader.PropertyToID("_WaterCameraSubmerged");
        private static readonly int BodiesId = Shader.PropertyToID("_WaterBodies");
        private static readonly int CellsId = Shader.PropertyToID("_WaterCells");
        private static readonly int DryId = Shader.PropertyToID("_WaterDryVolumes");
        private static readonly int StampsId = Shader.PropertyToID("_WaterStamps");
        private static readonly int CountId = Shader.PropertyToID("_WaterBodyCount");
        private static readonly int StampCountId = Shader.PropertyToID("_WaterStampCount");
        private static readonly int TimeId = Shader.PropertyToID("_WaterClock");
        private static readonly int InfoId = Shader.PropertyToID("_WaterSurfaceInfo");
        private static readonly int DepthId = Shader.PropertyToID("_WaterOpaqueDepth");
        private static readonly int FogId = Shader.PropertyToID("_WaterAirFog");
        private static readonly int FogColorId = Shader.PropertyToID("_WaterAirFogColor");
        private static readonly int DebugId = Shader.PropertyToID("_WaterDebugView");
        private static readonly int OpaqueColorId = Shader.PropertyToID("_CameraOpaqueTexture");
        private static GlobalKeyword LinearFog, ExpFog, Exp2Fog;

        internal struct FogSettings
        {
            public bool Enabled;
            public FogMode Mode;
            public Vector4 Parameters;
            public Color Color;

            public static FogSettings Capture() => new()
            {
                Enabled = RenderSettings.fog,
                Mode = RenderSettings.fogMode,
                Parameters = new Vector4(RenderSettings.fog ? (int)RenderSettings.fogMode : 0,
                    RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance),
                Color = RenderSettings.fogColor.linear
            };

            public void Restore(RasterCommandBuffer command)
            {
                command.SetKeyword(LinearFog, Enabled && Mode == FogMode.Linear);
                command.SetKeyword(ExpFog, Enabled && Mode == FogMode.Exponential);
                command.SetKeyword(Exp2Fog, Enabled && Mode == FogMode.ExponentialSquared);
            }
        }

        private sealed class UploadData
        {
            public readonly WaterRenderSnapshot Snapshot = new();
            public BufferHandle Bodies, Cells, Dry, Stamps;
            public FogSettings Fog;
            public bool IsSubmerged;
        }

        private sealed class PreparePass : ScriptableRenderPass
        {
            private readonly object _owner;
            public PreparePass(object owner)
            {
                _owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                var state = frameData.GetOrCreate<WaterOpticsFrameData>();
                if (!state.TryClaim()) return;
                using var builder = graph.AddUnsafePass<UploadData>("Water Domain Upload / Fog Ownership", out var data);
                data.Snapshot.Capture(frameData.Get<UniversalCameraData>().camera);
                if (data.Snapshot.BodyCount == 0)
                {
                    // 입력이 전부 거절된 경우 전역 Fog를 건드리지 않고 이 미사용 패스를 그래프가 제거한다.
                    builder.SetRenderFunc(static (UploadData pass, UnsafeGraphContext context) => { });
                    return;
                }
                data.Fog = FogSettings.Capture();
                data.IsSubmerged = UnderwaterRegion.TryResolve(frameData.Get<UniversalCameraData>().camera.transform.position, out _, out _);
                data.Bodies = graph.CreateBuffer(new BufferDesc(data.Snapshot.BodyCount, System.Runtime.InteropServices.Marshal.SizeOf<WaterGpuRecords.Body>()) { name = "Water Bodies" });
                data.Cells = graph.CreateBuffer(new BufferDesc(Mathf.Max(1, data.Snapshot.CellCount), 48) { name = "Water Triangle Prisms" });
                data.Dry = graph.CreateBuffer(new BufferDesc(Mathf.Max(1, data.Snapshot.DryCount), 32) { name = "Water Dry Volumes" });
                data.Stamps = graph.CreateBuffer(new BufferDesc(Mathf.Max(1, data.Snapshot.StampCount), 32) { name = "Water Contact Stamps" });
                builder.UseBuffer(data.Bodies, AccessFlags.Write);
                builder.UseBuffer(data.Cells, AccessFlags.Write);
                builder.UseBuffer(data.Dry, AccessFlags.Write);
                builder.UseBuffer(data.Stamps, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                state.Owner = _owner;
                state.Snapshot = data.Snapshot;
                state.Bodies = data.Bodies;
                state.Cells = data.Cells;
                state.Dry = data.Dry;
                state.Stamps = data.Stamps;
                state.Fog = data.Fog;
                builder.SetRenderFunc(static (UploadData pass, UnsafeGraphContext context) =>
                {
                    var command = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var snapshot = pass.Snapshot;
                    command.SetBufferData((GraphicsBuffer)pass.Bodies, snapshot.Bodies, 0, 0, snapshot.BodyCount);
                    command.SetBufferData((GraphicsBuffer)pass.Cells, snapshot.Cells, 0, 0, Mathf.Max(1, snapshot.CellCount));
                    command.SetBufferData((GraphicsBuffer)pass.Dry, snapshot.DryVolumes, 0, 0, Mathf.Max(1, snapshot.DryCount));
                    command.SetBufferData((GraphicsBuffer)pass.Stamps, snapshot.Stamps, 0, 0, Mathf.Max(1, snapshot.StampCount));
                    command.SetGlobalBuffer(BodiesId, pass.Bodies);
                    command.SetGlobalBuffer(CellsId, pass.Cells);
                    command.SetGlobalBuffer(DryId, pass.Dry);
                    command.SetGlobalBuffer(StampsId, pass.Stamps);
                    command.SetGlobalInt(CountId, snapshot.BodyCount);
                    command.SetGlobalInt(StampCountId, snapshot.StampCount);
                    command.SetGlobalFloat(TimeId, snapshot.Time);
                    command.SetGlobalFloat(ActiveId, 1f);
                    command.SetGlobalFloat(SubmergedId, pass.IsSubmerged ? 1f : 0f);
                    command.SetGlobalVector(FogId, pass.Fog.Parameters);
                    command.SetGlobalColor(FogColorId, pass.Fog.Color);
                    command.DisableShaderKeyword("FOG_LINEAR");
                    command.DisableShaderKeyword("FOG_EXP");
                    command.DisableShaderKeyword("FOG_EXP2");
                });
            }
        }

        private sealed class SurfaceData
        {
            public WaterRenderSnapshot Snapshot;
        }

        private sealed class ResolveData
        {
            public TextureHandle Source, Depth, Surface;
            public Material Material;
            public FogSettings Fog;
            public int Debug;
            public int CameraId, BodyCount, CellCount, RejectedCount;
            public int ColorSamples, DepthSamples;
        }
        private sealed class OpaqueCopyData { public TextureHandle Source; }

        private static void UseBuffers(IBaseRenderGraphBuilder builder, WaterOpticsFrameData state)
        {
            builder.UseBuffer(state.Bodies, AccessFlags.Read);
            builder.UseBuffer(state.Cells, AccessFlags.Read);
            builder.UseBuffer(state.Dry, AccessFlags.Read);
            builder.UseBuffer(state.Stamps, AccessFlags.Read);
        }

        private sealed class ResolvePass : ScriptableRenderPass
        {
            private readonly UnderwaterRendererFeature _owner;
            private readonly Material _material;
            public ResolvePass(UnderwaterRendererFeature owner, Material material)
            {
                _owner = owner;
                _material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var state = frameData.GetOrCreate<WaterOpticsFrameData>();
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                if (state.Owner != _owner || state.Snapshot == null || !resources.cameraDepthTexture.IsValid()) return;
                var descriptor = new TextureDesc(camera.cameraTargetDescriptor.width, camera.cameraTargetDescriptor.height)
                {
                    name = "Water Surface Info (eye depth / oct normal / signed body)",
                    colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
                    clearBuffer = true, clearColor = Color.clear, filterMode = FilterMode.Point,
                    msaaSamples = MSAASamples.None
                };
                var info = graph.CreateTexture(descriptor);
                descriptor.name = "Water Surface Private Depth";
                descriptor.colorFormat = GraphicsFormat.None;
                descriptor.depthBufferBits = DepthBits.Depth32;
                var depth = graph.CreateTexture(descriptor);
                using (var builder = graph.AddRasterRenderPass<SurfaceData>("Water Surface Info", out var data))
                {
                    data.Snapshot = state.Snapshot;
                    UseBuffers(builder, state);
                    builder.SetRenderAttachment(info, 0);
                    builder.SetRenderAttachmentDepth(depth);
                    builder.SetGlobalTextureAfterPass(info, InfoId);
                    builder.SetRenderFunc(static (SurfaceData pass, RasterGraphContext context) =>
                    {
                        var snapshot = pass.Snapshot;
                        for (int body = 0; body < snapshot.BodyCount; body++)
                        {
                            Mesh mesh = snapshot.Meshes[body];
                            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                                context.cmd.DrawMesh(mesh, snapshot.Matrices[body], snapshot.Materials[body], submesh,
                                    snapshot.SurfacePasses[body], snapshot.Properties[body]);
                        }
                    });
                }
                state.SurfaceInfo = info;
                var outputDesc = graph.GetTextureDesc(resources.activeColorTexture);
                outputDesc.name = "Water Scene Resolve";
                outputDesc.clearBuffer = false;
                // 투명 패스가 기존 MSAA 깊이를 이어 쓰므로 카메라 색의 샘플 수를 유지한다.
                outputDesc.bindTextureMS = false;
                var destination = graph.CreateTexture(outputDesc);
                using (var builder = graph.AddRasterRenderPass<ResolveData>("Water Scene Resolve", out var data))
                {
                    data.Source = resources.activeColorTexture;
                    data.Depth = resources.cameraDepthTexture;
                    data.Surface = info;
                    data.Material = _material;
                    data.Fog = state.Fog;
                    data.Debug = (int)_owner._debugView;
                    data.CameraId = camera.camera.GetInstanceID();
                    data.BodyCount = state.Snapshot.BodyCount;
                    data.CellCount = state.Snapshot.CellCount;
                    data.RejectedCount = state.Snapshot.RejectedCount;
                    data.ColorSamples = (int)outputDesc.msaaSamples;
                    data.DepthSamples = (int)graph.GetTextureDesc(resources.activeDepthTexture).msaaSamples;
                    UseBuffers(builder, state);
                    builder.UseTexture(data.Source);
                    builder.UseTexture(data.Depth);
                    builder.UseTexture(info);
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture);
                    builder.SetRenderAttachment(destination, 0);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (ResolveData pass, RasterGraphContext context) =>
                    {
                        ResolveExecutionCount++;
                        LastResolvedCamera = pass.CameraId;
                        LastBodyCount = pass.BodyCount;
                        LastCellCount = pass.CellCount;
                        LastRejectedCount = pass.RejectedCount;
                        LastColorSamples = pass.ColorSamples;
                        LastDepthSamples = pass.DepthSamples;
                        context.cmd.SetGlobalTexture(DepthId, pass.Depth);
                        context.cmd.SetGlobalTexture(InfoId, pass.Surface);
                        context.cmd.SetGlobalInt(DebugId, pass.Debug);
                        Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1f, 1f, 0f, 0f), pass.Material, 0);
                        pass.Fog.Restore(context.cmd);
                    });
                }
                resources.cameraColor = destination;
                if (resources.cameraOpaqueTexture.IsValid())
                {
                    // 기존 굴절 재질도 Fog 처리 전 Cbase를 다시 가져오지 않도록 별도 읽기 스냅샷을 제공한다.
                    outputDesc.name = "Water Resolved Opaque Snapshot";
                    outputDesc.msaaSamples = MSAASamples.None;
                    TextureHandle opaqueSnapshot = graph.CreateTexture(outputDesc);
                    using var builder = graph.AddRasterRenderPass<OpaqueCopyData>("Water Opaque Snapshot", out var copy);
                    copy.Source = destination;
                    builder.UseTexture(destination);
                    builder.SetRenderAttachment(opaqueSnapshot, 0);
                    builder.SetGlobalTextureAfterPass(opaqueSnapshot, OpaqueColorId);
                    builder.SetRenderFunc(static (OpaqueCopyData pass, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1f, 1f, 0f, 0f), 0f, false));
                }
            }
        }

        private sealed class CleanupData { public FogSettings Fog; }
        private sealed class CleanupPass : ScriptableRenderPass
        {
            private readonly object _owner;
            public CleanupPass(object owner)
            {
                _owner = owner;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var state = frameData.GetOrCreate<WaterOpticsFrameData>();
                if (state.Owner != _owner || state.Snapshot == null) return;
                using var builder = graph.AddRasterRenderPass<CleanupData>("Water Camera State Restore", out var data);
                data.Fog = state.Fog;
                UseBuffers(builder, state);
                if (state.SurfaceInfo.IsValid()) builder.UseTexture(state.SurfaceInfo);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CleanupData pass, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(ActiveId, 0f);
                    context.cmd.SetGlobalFloat(SubmergedId, 0f);
                    context.cmd.SetGlobalInt(CountId, 0);
                    pass.Fog.Restore(context.cmd);
                });
            }
        }
    }
}
