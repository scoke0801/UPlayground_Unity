using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 배치된 씬이 시작될 때 전역 FPS 표시를 켠다. 시험장처럼 처음부터 프레임을 봐야 하는 씬에 둔다.
    /// 켠 뒤에는 단축키로 끌 수 있으며, 씬마다 FPS 프리팹을 직접 두면 전역 표시와 겹쳐 그려지므로 이 표식을 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIFrameRateAutoShow : MonoBehaviour
    {
        private void Start() => UIFrameRateOverlay.SetVisible(true);
    }
}
