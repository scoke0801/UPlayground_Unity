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

        // 배치 판정은 연출 시작 시 참가자 수만큼만 일어나므로 정적 버퍼 재사용으로 충분하다.
        private static readonly Collider[] s_overlapBuffer = new Collider[8];

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
            grounded = candidate;
            Vector3 probe = candidate + Vector3.up * GroundProbeUp;
            if (!Physics.Raycast(
                    probe,
                    Vector3.down,
                    out RaycastHit hit,
                    GroundProbeUp + GroundProbeDown,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (maxHeightDelta > 0f && Mathf.Abs(hit.point.y - referenceHeight) > maxHeightDelta)
                return false;

            grounded = hit.point;
            return true;
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
            grounded = candidate;
            if (actor == null)
                return false;
            if (!TryProbeGround(candidate, referenceHeight, maxHeightDelta, out grounded))
                return false;

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
