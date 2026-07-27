using UnityEngine;

namespace UPlayGround.Animation
{
    /// <summary>Animator 평가 델타를 누적하고 물리 스텝별 스냅샷으로 한 번만 전달한다.</summary>
    public struct RootMotionStepBuffer
    {
        public Vector3 PendingPosition { get; private set; }
        public Quaternion PendingRotation { get; private set; }
        public Vector3 StepPosition { get; private set; }
        public Quaternion StepRotation { get; private set; }

        public static RootMotionStepBuffer Create()
            => new()
            {
                PendingRotation = Quaternion.identity,
                StepRotation = Quaternion.identity,
            };

        public void Accumulate(Vector3 position, Quaternion rotation)
        {
            PendingPosition += position;
            PendingRotation = (PendingRotation * rotation).normalized;
        }

        public void BeginStep()
        {
            StepPosition = PendingPosition;
            StepRotation = PendingRotation;
            PendingPosition = Vector3.zero;
            PendingRotation = Quaternion.identity;
        }

        public void EndStep()
        {
            StepPosition = Vector3.zero;
            StepRotation = Quaternion.identity;
        }

        public void ConsumePending(out Vector3 position, out Quaternion rotation)
        {
            position = PendingPosition;
            rotation = PendingRotation;
            PendingPosition = Vector3.zero;
            PendingRotation = Quaternion.identity;
        }

        public void Flush()
        {
            PendingPosition = Vector3.zero;
            PendingRotation = Quaternion.identity;
            EndStep();
        }
    }
}
