using UnityEngine;

namespace UPlayGround.Data.Config
{
    [CreateAssetMenu(
        fileName = "ActorSimulationSettings",
        menuName = "UPlayGround/Config/Actor Simulation Settings")]
    public sealed class ActorSimulationSettingsSO : ScriptableObject
    {
        [Min(0f)] public float wakeDistance = 55f;
        [Min(0f)] public float sleepDistance = 65f;
        [Min(0.01f)] public float evaluationInterval = 0.2f;
        [Min(1)] public int evaluationBuckets = 4;
        [Min(0f)] public float minimumActiveDuration = 1f;
        [Min(0.01f)] public float unsafeRetryInterval = 0.25f;
        [Min(0f)] public float teleportRefreshDistance = 20f;
        [Min(0f)] public float maximumSuspendSpeed = 0.1f;
        public bool includeEliteMonsters;

        public float WakeDistanceSquared => wakeDistance * wakeDistance;
        public float SleepDistanceSquared => sleepDistance * sleepDistance;
        public float TeleportRefreshDistanceSquared =>
            teleportRefreshDistance * teleportRefreshDistance;
        public float MaximumSuspendSpeedSquared =>
            maximumSuspendSpeed * maximumSuspendSpeed;

        private void OnValidate()
        {
            wakeDistance = Mathf.Max(0f, wakeDistance);
            sleepDistance = Mathf.Max(wakeDistance + 0.01f, sleepDistance);
            evaluationInterval = Mathf.Max(0.01f, evaluationInterval);
            evaluationBuckets = Mathf.Max(1, evaluationBuckets);
            minimumActiveDuration = Mathf.Max(0f, minimumActiveDuration);
            unsafeRetryInterval = Mathf.Max(0.01f, unsafeRetryInterval);
            teleportRefreshDistance = Mathf.Max(0f, teleportRefreshDistance);
            maximumSuspendSpeed = Mathf.Max(0f, maximumSuspendSpeed);
        }
    }
}
