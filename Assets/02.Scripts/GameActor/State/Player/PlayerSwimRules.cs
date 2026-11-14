using UnityEngine;

namespace UPlayGround.State
{
    /// <summary>수영 진입·이탈·부력 계산의 순수 규칙. 상태와 컨트롤러가 같은 기준을 쓰고 EditMode 테스트로 고정한다.</summary>
    public static class PlayerSwimRules
    {
        /// <summary>수영으로 자동 진입할 수 있는 상태. 전투·피격·연출 상태는 끝난 뒤 이 상태들로 돌아와 진입한다.</summary>
        public static bool CanEnterFrom(ActorStateId fromState) => fromState is
            ActorStateId.Idle or ActorStateId.GroundMove or ActorStateId.Airborne
            or ActorStateId.Stop or ActorStateId.TurnInPlace or ActorStateId.Crouching
            or ActorStateId.Land;

        /// <summary>발 아래 수심이 진입 깊이 이상이면 잠긴 것으로 본다.</summary>
        public static bool ShouldEnter(float depth, float enterDepth) => depth >= enterDepth;

        /// <summary>
        /// 이탈 깊이보다 얕아지고 디딜 곳이 있거나 완전히 물 밖일 때만 이탈한다.
        /// 진입 깊이보다 이탈 깊이를 낮게 두어 경계에서 상태가 깜박이지 않게 한다.
        /// </summary>
        public static bool ShouldExit(float depth, float exitDepth, bool hasGroundNearby) =>
            depth < exitDepth && (hasGroundNearby || depth <= 0f);

        /// <summary>떠 있는 몸의 목표 발 높이.</summary>
        public static float ResolveFloatHeight(float surfaceY, float floatDepth, float bobOffset) =>
            surfaceY - floatDepth + bobOffset;

        /// <summary>목표 높이로 다가가는 수직 속도. 거리에 비례하고 최대 속도로 제한해 입수 충격이 부드럽게 흡수된다.</summary>
        public static float ResolveVerticalSpeed(float currentY, float targetY, float sharpness, float maxSpeed) =>
            Mathf.Clamp((targetY - currentY) * sharpness, -maxSpeed, maxSpeed);

        /// <summary>발 아래 수심이 머리 높이를 넘으면 머리까지 잠긴 것으로 본다.</summary>
        public static bool IsSubmerged(float depth, float headHeight) => depth > headHeight;

        /// <summary>
        /// 잠수 중 요청 수직 속도(verticalCommand, 위가 양수)를 실제 수직 속도로 바꾼다.
        /// 수면 높이에서는 아래로 향하는 요청만 몸을 가라앉히고 그 외에는 부력 스프링이 띄운다.
        /// 수면 아래에서는 입력이 없으면 현재 깊이를 유지하고, 상승은 수면에 가까워지면 스프링 속도로 줄어
        /// 수면을 뚫고 튀어나가지 않으며, 최대 깊이 아래로는 내려가지 않는다.
        /// </summary>
        public static float ResolveDiveVerticalSpeed(
            float feetY, float floatY, float maxDiveY, float verticalCommand,
            float sharpness, float maxSpeed)
        {
            const float SurfaceBandTolerance = 0.05f;
            float spring = ResolveVerticalSpeed(feetY, floatY, sharpness, maxSpeed);
            if (verticalCommand < 0f && feetY <= maxDiveY) return 0f;
            if (feetY >= floatY - SurfaceBandTolerance) return verticalCommand < 0f ? verticalCommand : spring;
            return verticalCommand > 0f ? Mathf.Min(verticalCommand, spring) : verticalCommand;
        }

        /// <summary>
        /// 카메라 피치(아래가 양수)를 수중 이동 피치로 바꾼다. 기준 피치 주변 여유 각도는 0으로 두고,
        /// 그 바깥은 벗어난 각도에 배율을 곱해 ±80도로 제한한다.
        /// </summary>
        public static float ResolveSwimPitch(float cameraPitch, float neutralPitch, float deadZone, float gain)
        {
            float delta = cameraPitch - neutralPitch;
            float magnitude = Mathf.Max(0f, Mathf.Abs(delta) - deadZone) * gain;
            return Mathf.Clamp(Mathf.Sign(delta) * magnitude, -80f, 80f);
        }

        /// <summary>
        /// 실제 카메라 하향각과 전진 의도로 잠수 시작을 판정한다.
        /// 이동 벡터의 하강각을 쓰면 옆 입력을 섞는 순간 시작 각도가 달라지므로 입력 크기와 분리한다.
        /// </summary>
        public static bool IsDiveIntent(Vector2 moveInput, float cameraPitch, float startPitchDegrees) =>
            // 쿼터니언에서 복원한 경계 각도의 반올림 오차로 수평 시선이 잠수를 켜지 않게 한다.
            moveInput.y > 0f && cameraPitch > startPitchDegrees
            && !Mathf.Approximately(cameraPitch, startPitchDegrees);

        /// <summary>
        /// 수직 요청이 충분히 가파른 하강인지. 수평 속도가 거의 없거나 하강각이 임계 이상이면 머리를 아래로 한 자세를 쓴다.
        /// </summary>
        public static bool IsSteepDescent(float horizontalSpeed, float verticalSpeed, float steepAngleDegrees)
        {
            if (verticalSpeed >= 0f) return false;
            float angle = Mathf.Atan2(-verticalSpeed, Mathf.Max(0f, horizontalSpeed)) * Mathf.Rad2Deg;
            return angle >= steepAngleDegrees;
        }

        /// <summary>목표 높이까지 오르기 위해 필요한 초기 상향 속도(중력 가속도 크기는 상승 배율 포함).</summary>
        public static float ResolveHopSpeed(float riseHeight, float gravityMagnitude) =>
            Mathf.Sqrt(2f * Mathf.Max(0f, riseHeight) * Mathf.Max(0f, gravityMagnitude));
    }
}
