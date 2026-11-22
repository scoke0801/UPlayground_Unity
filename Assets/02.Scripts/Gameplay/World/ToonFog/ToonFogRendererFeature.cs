using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UPlayGround.Data.Rendering;

namespace UPlayGround.Gameplay.World.ToonFog
{
    /// <summary>깊이 합성과 투명 어댑터에 같은 카메라의 안개를 공급한다.</summary>
    public sealed class ToonFogRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;

        private static readonly int EnabledId = Shader.PropertyToID("_UPGToonFogEnabled");
        private static readonly int CountId = Shader.PropertyToID("_UPGToonFogLocalCount");
        private static readonly int SamplesId = Shader.PropertyToID("_UPGToonFogSamples");
        private static readonly int DebugId = Shader.PropertyToID("_UPGToonFogDebug");
        private static readonly int DistanceId = Shader.PropertyToID("_UPGToonFogDistance");
        private static readonly int HeightId = Shader.PropertyToID("_UPGToonFogHeight");
        private static readonly int ColorsId = Shader.PropertyToID("_UPGToonFogColors");
        private static readonly int HeightTintId = Shader.PropertyToID("_UPGToonFogHeightTint");
        private static readonly int TintId = Shader.PropertyToID("_UPGToonFogEnvironmentTint");
        internal static readonly int PaletteId = Shader.PropertyToID("_UPGToonFogPalette");
        internal static readonly int NoiseId = Shader.PropertyToID("_UPGToonFogNoise");
        private static readonly int MatricesId = Shader.PropertyToID("_UPGToonFogWorldToLocal");
        private static readonly int LocalSettingsId = Shader.PropertyToID("_UPGToonFogLocalSettings");
        private static readonly int LocalTintId = Shader.PropertyToID("_UPGToonFogLocalTint");
        private static readonly int LocalNoiseId = Shader.PropertyToID("_UPGToonFogLocalNoise");
        private static readonly int RefractionId = Shader.PropertyToID("_UPGToonFogRefraction");
        private static readonly int RefractionReadyId = Shader.PropertyToID("_UPGToonFogRefractionReady");

        public enum CameraStatus { Neutral, Composite, WaterResolve, LegacyFog, MissingProfile, MissingDepthOrColor, MissingShader }
        public struct Diagnostics
        {
            public CameraStatus Status;
            public ToonFogProfile Profile;
            public int Registered, Candidates, Active, Rejected, Regions;
        }

        private sealed class CameraEntry
        {
            internal readonly ToonFogResolver Resolver = new();
            internal Camera Camera;
            internal ToonFogCameraPolicy Policy;
            internal RTHandle PaletteHandle, NoiseHandle;
            internal Texture NoiseTexture;
            internal double LastSeen;
            internal Diagnostics Diagnostics;

            internal void Dispose()
            {
                PaletteHandle?.Release();
                NoiseHandle?.Release();
                Resolver.Dispose();
            }
        }

        private readonly Dictionary<int, CameraEntry> _cameras = new();
        private readonly List<int> _expired = new();
        private double _nextCleanup;
        private Material _material;
        private FogPass _fog;
        private ResetPass _reset;

        /// <inheritdoc/>
        public override void Create()
        {
            Release();
            _material = _shader != null ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _fog = new FogPass(this);
            _reset = new ResetPass();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if (cameraData.camera == null) return;
            CleanCameras();
            var world = ToonFogWorld.Find(cameraData.camera);
            bool eligible = world != null && world.fogEnabled && !cameraData.camera.orthographic && !cameraData.xr.enabled
                && cameraData.renderType == CameraRenderType.Base
                && (cameraData.cameraType == CameraType.Game || (cameraData.isSceneViewCamera && world.showInSceneView));
            if (!eligible)
            {
                if (_cameras.TryGetValue(cameraData.camera.GetInstanceID(), out var old))
                {
                    old.Resolver.Invalidate();
                    old.Diagnostics = default;
                }
                return;
            }

            var entry = GetCamera(cameraData.camera);
            if (entry.Policy != null && entry.Policy.isActiveAndEnabled && !entry.Policy.fogEnabled)
            {
                entry.Resolver.Invalidate();
                entry.Diagnostics = default;
                return;
            }
            if (_material == null || !_material.shader.isSupported)
            {
                RecordFailure(entry, CameraStatus.MissingShader);
                return;
            }
            if (RenderSettings.fog)
            {
                RecordFailure(entry, CameraStatus.LegacyFog);
                return;
            }
            if (world.profile == null || !world.profile.IsValid || world.quality == null)
            {
                RecordFailure(entry, CameraStatus.MissingProfile);
                return;
            }
            _fog.requiresIntermediateTexture = world.captureLegacyRefraction;
            renderer.EnqueuePass(_fog);
            renderer.EnqueuePass(_reset);
        }

        /// <summary>진단 화면에서 카메라별 마지막 기록 결과를 확인한다.</summary>
        public bool TryGetDiagnostics(Camera camera, out Diagnostics diagnostics)
        {
            diagnostics = default;
            if (camera == null || !_cameras.TryGetValue(camera.GetInstanceID(), out var entry)) return false;
            diagnostics = entry.Diagnostics;
            return true;
        }

        private CameraEntry GetCamera(Camera camera)
        {
            int id = camera.GetInstanceID();
            if (!_cameras.TryGetValue(id, out var entry) || entry.Camera != camera)
            {
                entry?.Dispose();
                camera.TryGetComponent(out ToonFogCameraPolicy policy);
                entry = new CameraEntry { Camera = camera, Policy = policy };
                _cameras[id] = entry;
            }
            entry.LastSeen = Time.realtimeSinceStartupAsDouble;
            return entry;
        }

        private void RecordFailure(CameraEntry entry, CameraStatus status)
        {
            if (entry.Diagnostics.Status != status)
                Debug.LogWarning($"툰 안개 중립 처리 ({name}, {entry.Camera.name}): {status}", entry.Camera);
            entry.Resolver.Invalidate();
            entry.Diagnostics = new Diagnostics { Status = status };
        }

        private void CleanCameras()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < _nextCleanup) return;
            _nextCleanup = now + 5;
            _expired.Clear();
            foreach (var pair in _cameras)
                if (pair.Value.Camera == null || now - pair.Value.LastSeen > 30) _expired.Add(pair.Key);
            foreach (int id in _expired)
            {
                _cameras[id].Dispose();
                _cameras.Remove(id);
            }
        }

        protected override void Dispose(bool disposing) => Release();

        private void Release()
        {
            foreach (var entry in _cameras.Values) entry.Dispose();
            _cameras.Clear();
            CoreUtils.Destroy(_material);
            _material = null;
            ResetImmediate();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetImmediate()
        {
            Shader.SetGlobalFloat(EnabledId, 0);
            Shader.SetGlobalInt(CountId, 0);
            Shader.SetGlobalFloat(RefractionReadyId, 0);
            Shader.SetGlobalTexture(PaletteId, Texture2D.blackTexture);
            Shader.SetGlobalTexture(NoiseId, Texture2D.grayTexture);
            Shader.SetGlobalTexture(RefractionId, Texture2D.blackTexture);
        }

        internal sealed class FrameData : ContextItem
        {
            internal TextureHandle Palette, Noise;
            internal bool Enabled;

            public override void Reset() { Enabled = false; Palette = Noise = default; }

            internal void UseTextures(IBaseRenderGraphBuilder builder)
            {
                if (!Enabled) return;
                builder.UseGlobalTexture(PaletteId);
                builder.UseGlobalTexture(NoiseId);
            }
        }

        private sealed class GlobalsData
        {
            internal Vector4 Distance, Height, Colors;
            internal Color HeightTint, Tint;
            internal int Count, Samples, Debug;
            internal readonly Matrix4x4[] Matrices = new Matrix4x4[ToonFogResolver.MaxLocals];
            internal readonly Vector4[] Settings = new Vector4[ToonFogResolver.MaxLocals];
            internal readonly Vector4[] LocalTint = new Vector4[ToonFogResolver.MaxLocals];
            internal readonly Vector4[] LocalNoise = new Vector4[ToonFogResolver.MaxLocals];
        }

        private sealed class DrawData { internal Material Material; }
        private sealed class CopyData { internal TextureHandle Source; }
        private sealed class NeutralData { }

        private sealed class FogPass : ScriptableRenderPass
        {
            private readonly ToonFogRendererFeature _owner;

            internal FogPass(ToonFogRendererFeature owner)
            {
                _owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var camera = frameData.Get<UniversalCameraData>().camera;
                var resources = frameData.Get<UniversalResourceData>();
                var entry = _owner.GetCamera(camera);
                var world = ToonFogWorld.Find(camera);
                if (!resources.cameraDepthTexture.IsValid() || !resources.activeColorTexture.IsValid())
                {
                    _owner.RecordFailure(entry, CameraStatus.MissingDepthOrColor);
                    return;
                }
                if (world == null || !entry.Resolver.Resolve(camera, world, entry.Policy != null ? entry.Policy.anchorOverride : null))
                {
                    _owner.RecordFailure(entry, CameraStatus.MissingProfile);
                    return;
                }

                var state = frameData.GetOrCreate<FrameData>();
                var water = frameData.GetOrCreate<WaterOpticsFrameData>();
                bool delegated = water.Owner != null && water.Snapshot != null && resources.activeDepthTexture.IsValid();
                var resolver = entry.Resolver;
                entry.Diagnostics = new Diagnostics
                {
                    Status = delegated ? CameraStatus.WaterResolve : CameraStatus.Composite, Profile = world.profile,
                    Registered = resolver.RegisteredCount, Candidates = resolver.CandidateCount, Active = resolver.LocalCount,
                    Rejected = resolver.RejectedCount, Regions = resolver.RegionCount
                };
                if (world.diagnostics == ToonFogWorld.View.Final
                    && (resolver.Distance.w <= 0 || (resolver.Distance.x <= 0 && resolver.Height.x <= 0 && resolver.LocalCount == 0)))
                    return;

                entry.PaletteHandle ??= RTHandles.Alloc(resolver.Lut);
                if (entry.NoiseTexture != resolver.Noise || entry.NoiseHandle == null)
                {
                    entry.NoiseHandle?.Release();
                    entry.NoiseTexture = resolver.Noise;
                    entry.NoiseHandle = RTHandles.Alloc(resolver.Noise);
                }
                state.Palette = graph.ImportTexture(entry.PaletteHandle);
                state.Noise = graph.ImportTexture(entry.NoiseHandle);
                state.Enabled = true;
                RecordGlobals(graph, state, resolver, delegated ? ToonFogWorld.View.Final : world.diagnostics);

                if (delegated) return;
                using (var builder = graph.AddRasterRenderPass<DrawData>("Toon Fog Composite (depth only)", out var data))
                {
                    data.Material = _owner._material;
                    builder.UseTexture(resources.cameraDepthTexture);
                    state.UseTextures(builder);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (DrawData pass, RasterGraphContext context) =>
                        context.cmd.DrawProcedural(Matrix4x4.identity, pass.Material, 0, MeshTopology.Triangles, 3, 1));
                }

                if (world.captureLegacyRefraction && !resources.isActiveTargetBackBuffer)
                    RecordRefraction(graph, resources.activeColorTexture);
            }
        }

        private static void RecordGlobals(RenderGraph graph, FrameData frame, ToonFogResolver resolver, ToonFogWorld.View debug)
        {
            using var builder = graph.AddRasterRenderPass<GlobalsData>("Toon Fog Camera Setup", out var data);
            data.Distance = resolver.Distance;
            data.Height = resolver.Height;
            data.Colors = resolver.Colors;
            data.HeightTint = resolver.HeightTint;
            data.Tint = resolver.EnvironmentTint;
            data.Count = resolver.LocalCount;
            data.Samples = resolver.Samples;
            data.Debug = (int)debug;
            for (int i = 0; i < data.Count; i++)
            {
                var local = resolver.Locals[i];
                data.Matrices[i] = local.Inverse;
                data.Settings[i] = local.Settings;
                data.LocalTint[i] = local.Tint;
                data.LocalNoise[i] = local.Noise;
            }
            builder.AllowGlobalStateModification(true);
            builder.SetGlobalTextureAfterPass(frame.Palette, PaletteId);
            builder.SetGlobalTextureAfterPass(frame.Noise, NoiseId);
            builder.SetRenderFunc(static (GlobalsData pass, RasterGraphContext context) =>
            {
                var cmd = context.cmd;
                cmd.SetGlobalFloat(EnabledId, 1);
                cmd.SetGlobalInt(CountId, pass.Count);
                cmd.SetGlobalInt(SamplesId, pass.Samples);
                cmd.SetGlobalInt(DebugId, pass.Debug);
                cmd.SetGlobalVector(DistanceId, pass.Distance);
                cmd.SetGlobalVector(HeightId, pass.Height);
                cmd.SetGlobalVector(ColorsId, pass.Colors);
                cmd.SetGlobalVector(HeightTintId, pass.HeightTint);
                cmd.SetGlobalVector(TintId, pass.Tint);
                if (pass.Count == 0) return;
                cmd.SetGlobalMatrixArray(MatricesId, pass.Matrices);
                cmd.SetGlobalVectorArray(LocalSettingsId, pass.Settings);
                cmd.SetGlobalVectorArray(LocalTintId, pass.LocalTint);
                cmd.SetGlobalVectorArray(LocalNoiseId, pass.LocalNoise);
            });
        }

        private static void RecordRefraction(RenderGraph graph, TextureHandle source)
        {
            var desc = graph.GetTextureDesc(source);
            desc.name = "Toon Fog Refraction Background";
            desc.msaaSamples = MSAASamples.None;
            desc.bindTextureMS = false;
            desc.clearBuffer = false;
            var target = graph.CreateTexture(desc);
            using var builder = graph.AddRasterRenderPass<CopyData>("Toon Fog Legacy Refraction Copy", out var data);
            data.Source = source;
            builder.UseTexture(source);
            builder.SetRenderAttachment(target, 0);
            builder.SetGlobalTextureAfterPass(target, RefractionId);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (CopyData pass, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1, 1, 0, 0), 0f, false);
                context.cmd.SetGlobalFloat(RefractionReadyId, 1);
            });
        }

        private sealed class ResetPass : ScriptableRenderPass
        {
            internal ResetPass() => renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                using var builder = graph.AddRasterRenderPass<NeutralData>("Toon Fog Camera Reset", out _);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(graph.defaultResources.blackTexture, PaletteId);
                builder.SetGlobalTextureAfterPass(graph.defaultResources.blackTexture, NoiseId);
                builder.SetGlobalTextureAfterPass(graph.defaultResources.blackTexture, RefractionId);
                builder.SetRenderFunc(static (NeutralData _, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(EnabledId, 0);
                    context.cmd.SetGlobalInt(CountId, 0);
                    context.cmd.SetGlobalFloat(RefractionReadyId, 0);
                });
            }
        }
    }
}
