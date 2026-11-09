using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World
{
    /// <summary>지역 내부에서 완전히 적용하고 외곽 띠에서 기본 하늘로 섞이는 범위다.</summary>
    [DisallowMultipleComponent]
    public sealed class SkyRegionBinding : MonoBehaviour
    {
        public SkyProfileSO profile;
        public int priority;
        public Vector3 size = new(300, 2000, 300);
        [Min(0.01f)] public float blendDistance = 80;

        /// <summary>회전·스케일을 반영한 상자 밖 월드 거리로 연속 가중치를 구한다.</summary>
        public float EvaluateWeight(Vector3 position)
        {
            if (!isActiveAndEnabled || profile == null) return 0;
            Vector3 local = transform.InverseTransformPoint(position);
            Vector3 half = size * 0.5f;
            Vector3 outside = new(Mathf.Max(0, Mathf.Abs(local.x) - half.x),
                Mathf.Max(0, Mathf.Abs(local.y) - half.y), Mathf.Max(0, Mathf.Abs(local.z) - half.z));
            float distance = transform.TransformVector(outside).magnitude;
            return 1 - Mathf.SmoothStep(0, 1, distance / Mathf.Max(0.01f, blendDistance));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
