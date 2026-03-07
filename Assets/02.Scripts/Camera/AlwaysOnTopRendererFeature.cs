using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 지정한 레이어의 오브젝트를 Depth 무시하고 항상 위에 렌더링한다.
/// 사용법: URP Renderer Asset → Add Renderer Feature → AlwaysOnTopRendererFeature
///         Layer Mask에 "PlayerBody" 레이어 설정
/// </summary>
public class AlwaysOnTopRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public string passTag = "AlwaysOnTop";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask layerMask = 0;
    }

    public Settings settings = new Settings();

    private AlwaysOnTopPass _pass;

    public override void Create()
    {
        _pass = new AlwaysOnTopPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 게임 카메라에만 적용 (씬뷰, 반사 카메라 제외)
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        renderer.EnqueuePass(_pass);
    }

    // ──────────────────────────────────────────────

    private class AlwaysOnTopPass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private readonly FilteringSettings _filteringSettings;

        // Depth Clear용 렌더 상태
        private static readonly ShaderTagId[] ShaderTagIds =
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
        };

        public AlwaysOnTopPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;

            _filteringSettings = new FilteringSettings(
                RenderQueueRange.opaque,
                settings.layerMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // ① Depth Buffer 초기화 (해당 픽셀을 항상 앞으로)
            var cmd = CommandBufferPool.Get(_settings.passTag);
            cmd.SetRenderTarget(
                renderingData.cameraData.renderer.cameraColorTargetHandle,
                renderingData.cameraData.renderer.cameraDepthTargetHandle);
            // 깊이만 초기화해서 이후 드로우콜이 항상 통과되도록 함
            cmd.ClearRenderTarget(clearDepth: true, clearColor: false, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // ② 해당 레이어 오브젝트 재렌더
            var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = CreateDrawingSettings(
                new System.Collections.Generic.List<ShaderTagId>(ShaderTagIds),
                ref renderingData, sortingCriteria);

            context.DrawRenderers(
                renderingData.cullResults,
                ref drawSettings,
                ref _filteringSettings);
        }
    }
}
