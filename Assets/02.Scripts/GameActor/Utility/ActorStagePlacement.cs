using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround
{
    /// <summary>
    /// 연출로 액터를 세울 위치를 검증하는 공용 배치 도우미.
    /// 조우 등장과 대화 임시 화자가 같은 판정(지면 찾기 + 캡슐 여유)을 요구하므로 한곳에 모았다.
    /// </summary>
    public static class ActorStagePlacement
    {
        public const float GroundProbeUp = 2f;
        public const float GroundProbeDown = 6f;

        // 허용 오차 안에서 지면을 찾지 못했을 때 쓰는 마지막 탐지 범위.
        // 허용 오차를 넘겼다는 것은 저작 높이가 지면과 무관하다는 뜻(파묻힘·공중)이므로 넓게 훑는다.
        public const float WideGroundProbeUp = 20f;
        public const float WideGroundProbeDown = 60f;

        // 이상적인 자리에서 실패했을 때 기준점 쪽으로 물러나며 재시도하는 횟수.
        // 기준점(플레이어 발밑)은 반드시 유효한 지면이므로 당길수록 성공 확률이 올라간다.
        private const int StageRetreatStepCount = 3;

        // 배치 판정은 연출 시작 시 참가자 수만큼만 일어나므로 정적 버퍼 재사용으로 충분하다.
        private static readonly Collider[] s_overlapBuffer = new Collider[8];
        private static readonly RaycastHit[] s_groundHitBuffer = new RaycastHit[16];

        /// <summary>
        /// 연출로 세울 액터의 최종 위치를 결정한다. 실패하지 않는다.
        /// 이상적인 자리에서 지면을 찾지 못하면 기준점 쪽으로 물러나며 다시 찾고,
        /// 그래도 못 찾으면 기준점 높이에 세운다.
        /// 연출에서는 "완벽한 자리에 못 세우면 안 세운다"가 가장 나쁜 결과다 —
        /// 화자가 화면에서 통째로 사라지거나 저작 높이 그대로 지면 아래에 묻힌 채 남는다.
        /// </summary>
        /// <param name="candidate">이상적인 배치 위치.</param>
        /// <param name="anchor">지면 위임이 보장된 기준점(플레이어 발밑 등). 높이 기준이자 후퇴 방향이다.</param>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        public static Vector3 ResolveStagePosition(
            Vector3 candidate,
            Vector3 anchor,
            float maxHeightDelta)
        {
            if (TryProbeGround(candidate, anchor.y, maxHeightDelta, out Vector3 grounded))
                return grounded;

            for (int step = StageRetreatStepCount - 1; step >= 1; step--)
            {
                Vector3 retreated = RetreatTowardAnchor(candidate, anchor, step);
                if (TryProbeGround(retreated, anchor.y, maxHeightDelta, out grounded))
                    return grounded;
            }

            // 지면 탐지가 전부 실패해도 기준점 바로 앞·기준점 높이라면 플레이어와 같은 바닥 위다.
            return RetreatTowardAnchor(candidate, anchor, 1);
        }

        /// <summary>후보를 기준점 쪽으로 당기되 높이는 기준점에 맞춘다.</summary>
        private static Vector3 RetreatTowardAnchor(Vector3 candidate, Vector3 anchor, int step)
        {
            Vector3 retreated = Vector3.LerpUnclamped(
                anchor, candidate, (float)step / StageRetreatStepCount);
            retreated.y = anchor.y;
            return retreated;
        }

        /// <summary>
        /// 높이 허용 오차 없이 발밑 지면을 찾는다. 파묻히거나 떠 있는 저작 위치를 되살릴 때 쓴다.
        /// 아래를 먼저 훑고, 아무것도 없을 때만(=지형 안에서 레이가 출발한 경우) 지형 위로 나가서 다시 쏜다.
        /// 처음부터 위로 크게 올려 잡으면 공중에 뜬 대상이 머리 위 지붕·다리 위로 올라간다.
        /// </summary>
        /// <param name="ignoreRoot">이 트랜스폼 하위의 콜라이더는 지면 후보에서 제외한다.</param>
        public static bool TryProbeGroundIgnoringHeight(
            Vector3 candidate,
            Transform ignoreRoot,
            out Vector3 grounded)
        {
            if (TryProbeGround(
                    candidate, candidate.y, 0f,
                    GroundProbeUp, WideGroundProbeDown, ignoreRoot, out grounded))
            {
                return true;
            }

            return TryProbeGround(
                candidate, candidate.y, 0f,
                WideGroundProbeUp, WideGroundProbeDown, ignoreRoot, out grounded);
        }

        /// <summary>
        /// 허용 오차 안에서 지면을 찾지 못한 액터를 위한 광역 지면 탐지.
        /// 고지·구조물 위 저작을 지키려는 허용 오차가, 정작 파묻히거나 떠 있는 액터를 방치하는 것을 막는다.
        /// </summary>
        public static bool TryResolveGroundedPositionAnywhere(
            GameActor actor,
            Vector3 candidate,
            out Vector3 grounded)
        {
            grounded = candidate;
            if (actor == null)
                return false;
            if (!TryProbeGroundIgnoringHeight(candidate, actor.transform, out grounded))
                return false;

            KinematicCharacterMotor motor = actor.ActorController?.Motor;
            if (motor == null)
                return true;

            return motor.CharacterCollisionsOverlap(
                grounded,
                actor.transform.rotation,
                s_overlapBuffer) == 0;
        }

        /// <summary>
        /// 후보 위치의 지면을 찾는다. 기준 높이에서 크게 벗어난 지면(지붕·절벽)은 후보에서 제외한다.
        /// </summary>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        public static bool TryProbeGround(
            Vector3 candidate,
            float referenceHeight,
            float maxHeightDelta,
            out Vector3 grounded)
        {
            return TryProbeGround(
                candidate,
                referenceHeight,
                maxHeightDelta,
                GroundProbeUp,
                GroundProbeDown,
                out grounded);
        }

        /// <summary>
        /// 탐지 범위를 지정해 후보 위치의 지면을 찾는다.
        /// 후보가 지면 아래에 파묻힌 경우 기본 상향 탐지(<see cref="GroundProbeUp"/>)로는 지면 위로 나가지 못해
        /// 레이가 지형 내부에서 출발한다. 이때는 호출부가 더 높은 시작점을 지정해야 한다.
        /// </summary>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        public static bool TryProbeGround(
            Vector3 candidate,
            float referenceHeight,
            float maxHeightDelta,
            float probeUp,
            float probeDown,
            out Vector3 grounded)
        {
            return TryProbeGround(
                candidate,
                referenceHeight,
                maxHeightDelta,
                probeUp,
                probeDown,
                ignoreRoot: null,
                out grounded);
        }

        /// <summary>
        /// 자신의 콜라이더를 제외하고 지면을 찾는다.
        /// 액터가 서 있는 자리를 그대로 찍으면 레이가 자기 캡슐의 윗면을 지면으로 잡아 액터를 자기 머리 위로 올린다.
        /// </summary>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        /// <param name="ignoreRoot">이 트랜스폼 하위의 콜라이더는 지면 후보에서 제외한다.</param>
        public static bool TryProbeGround(
            Vector3 candidate,
            float referenceHeight,
            float maxHeightDelta,
            float probeUp,
            float probeDown,
            Transform ignoreRoot,
            out Vector3 grounded)
        {
            grounded = candidate;
            probeUp = Mathf.Max(probeUp, GroundProbeUp);
            probeDown = Mathf.Max(probeDown, GroundProbeDown);
            Vector3 probe = candidate + Vector3.up * probeUp;
            if (!TryFindNearestGroundHit(
                    probe,
                    probeUp + probeDown,
                    ignoreRoot,
                    out RaycastHit hit))
            {
                return false;
            }

            if (maxHeightDelta > 0f && Mathf.Abs(hit.point.y - referenceHeight) > maxHeightDelta)
                return false;

            grounded = hit.point;
            return true;
        }

        /// <summary>제외 대상이 아닌 첫 지면 접촉을 찾는다.</summary>
        private static bool TryFindNearestGroundHit(
            Vector3 origin,
            float distance,
            Transform ignoreRoot,
            out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                s_groundHitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_groundHitBuffer[i];
                if (ignoreRoot != null && hit.transform.IsChildOf(ignoreRoot))
                    continue;
                if (found && hit.distance >= nearest.distance)
                    continue;

                nearest = hit;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// 후보 위치의 지면을 찾고 액터 캡슐이 들어갈 수 있는지 확인한다.
        /// 실패하면 호출부가 원래 위치를 유지해야 한다 — 벽이나 지붕 안쪽 배치가 더 나쁜 결과다.
        /// </summary>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        public static bool TryResolveGroundedPosition(
            GameActor actor,
            Vector3 candidate,
            float referenceHeight,
            float maxHeightDelta,
            out Vector3 grounded)
        {
            return TryResolveGroundedPosition(
                actor,
                candidate,
                referenceHeight,
                maxHeightDelta,
                GroundProbeUp,
                GroundProbeDown,
                out grounded);
        }

        /// <summary>
        /// 탐지 범위를 지정해 지면과 캡슐 여유를 확인한다.
        /// 파묻힌 저작 위치를 되살릴 때처럼 기본 상향 탐지로 부족한 경우에 쓴다.
        /// </summary>
        /// <param name="maxHeightDelta">기준 높이와의 허용 차이. 0 이하면 높이 검사를 생략한다.</param>
        public static bool TryResolveGroundedPosition(
            GameActor actor,
            Vector3 candidate,
            float referenceHeight,
            float maxHeightDelta,
            float probeUp,
            float probeDown,
            out Vector3 grounded)
        {
            grounded = candidate;
            if (actor == null)
                return false;
            if (!TryProbeGround(
                    candidate,
                    referenceHeight,
                    maxHeightDelta,
                    probeUp,
                    probeDown,
                    actor.transform,
                    out grounded))
            {
                return false;
            }

            KinematicCharacterMotor motor = actor.ActorController?.Motor;
            if (motor == null)
                return true;

            return motor.CharacterCollisionsOverlap(
                grounded,
                actor.transform.rotation,
                s_overlapBuffer) == 0;
        }
    }
}
