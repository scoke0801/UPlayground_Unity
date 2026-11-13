using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UPlayGround.Gameplay.World
{
    /// <summary>같은 카메라의 Render Graph 안에서 수중 광학 합성의 중복을 막는다.</summary>
    public sealed class WaterOpticsFrameData : ContextItem
    {
        private bool _isClaimed;
        internal object Owner;
        internal WaterRenderSnapshot Snapshot;
        internal BufferHandle Bodies, Cells, Dry, Stamps;
        internal TextureHandle SurfaceInfo;
        internal UnderwaterRendererFeature.FogSettings Fog;

        /// <summary>광학 합성을 아직 기록하지 않은 카메라 프레임에만 실행권을 부여한다.</summary>
        public bool TryClaim()
        {
            if (_isClaimed) return false;
            _isClaimed = true;
            return true;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            _isClaimed = false;
            Owner = null;
            Snapshot = null;
            Bodies = Cells = Dry = Stamps = default;
            SurfaceInfo = default;
            Fog = default;
        }
    }
}
