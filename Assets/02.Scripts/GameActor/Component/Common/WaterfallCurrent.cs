using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>낙구로 흐르는 물살과 부력을 해제할 영역을 제공한다. 로컬 +Z가 하류다.</summary>
    [DisallowMultipleComponent]
    public sealed class WaterfallCurrent : MonoBehaviour
    {
        [SerializeField] private Bounds _region = new(Vector3.zero, new Vector3(12f, 12f, 12f));
        [SerializeField, Tooltip("로컬 Z가 이 값 이상이면 수영 부력을 해제한다.")]
        private float _fallStart;
        [SerializeField, Tooltip("로컬 Y가 이 높이 이하면 하류 소의 수영 판정을 다시 허용한다.")]
        private float _releaseHeight = -8f;
        [SerializeField, Min(0f)] private float _flowSpeed = 6f;
        [SerializeField, Min(0f)] private float _minimumFallSpeed = 3f;

        private static readonly List<WaterfallCurrent> Currents = new();

        private void OnEnable()
        {
            if (!Currents.Contains(this)) Currents.Add(this);
        }

        private void OnDisable() => Currents.Remove(this);

        /// <summary>영역 안의 물살을 구한다. 하류 입수 높이에 도달하면 더 이상 구속하지 않는다.</summary>
        public bool TrySample(Vector3 position, out Vector3 velocity, out bool isFalling)
        {
            velocity = Vector3.zero;
            isFalling = false;
            Vector3 local = transform.InverseTransformPoint(position);
            if (!_region.Contains(local) || local.y <= _releaseHeight) return false;

            isFalling = local.z >= _fallStart;
            float weight = Mathf.InverseLerp(_region.min.z, _fallStart, local.z);
            velocity = transform.forward * (_flowSpeed * weight * weight);
            return true;
        }

        /// <summary>낙하 중 물살의 하류 속도와 최소 하강 속도를 유지하며 더 빠른 낙하는 보존한다.</summary>
        public void ConstrainFallVelocity(ref Vector3 velocity)
        {
            Vector3 forward = transform.forward;
            Vector3 up = transform.up;
            velocity += forward * Mathf.Max(0f, _flowSpeed - Vector3.Dot(velocity, forward));
            velocity -= up * Mathf.Max(0f, Vector3.Dot(velocity, up) + _minimumFallSpeed);
        }

        /// <summary>등록된 물살 중 현재 위치에 적용되는 첫 영역을 반환한다.</summary>
        public static bool TryResolve(Vector3 position, out WaterfallCurrent current, out Vector3 velocity, out bool isFalling)
        {
            for (int i = 0; i < Currents.Count; i++)
            {
                current = Currents[i];
                if (current != null && current.TrySample(position, out velocity, out isFalling)) return true;
            }
            current = null;
            velocity = Vector3.zero;
            isFalling = false;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_region.center, _region.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(_region.min.x, 0f, _fallStart), new Vector3(_region.max.x, 0f, _fallStart));
            Gizmos.matrix = previous;
        }
    }
}
