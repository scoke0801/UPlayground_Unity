using UnityEngine;
using UPlayGround.Debugging;
using UPlayGround.MovementController;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 범용 MotionSet 이벤트 진단에 Actor MotionWarp 상태를 추가한다.
    /// </summary>
    public sealed class ActorMotionSetDebugOverlay : MotionSetEventDebugOverlay
    {
        protected override string BuildAdditionalStatus(GameObject target)
        {
            if (target == null)
                return string.Empty;

            ActorMovementController controller =
                target.GetComponent<ActorMovementController>()
                ?? target.GetComponentInParent<ActorMovementController>()
                ?? target.GetComponentInChildren<ActorMovementController>();
            if (controller == null || controller.MotionWarp == null)
                return string.Empty;

            MotionWarpController warp = controller.MotionWarp;
            if (warp.IsApplicable)
                return $"Warp: 적용 / 오차 {warp.LastArrivalError:F2}m";
            if (!string.IsNullOrEmpty(warp.LastFailureReason))
                return $"Warp: {warp.LastFailureReason}";
            return "Warp: 대기";
        }
    }
}
