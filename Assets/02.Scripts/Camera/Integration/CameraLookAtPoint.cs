using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>대화·연출 데이터가 ID로 찾을 수 있는 씬 카메라 주시 지점.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UPlayGround/Camera/Camera Look At Point")]
    public sealed class CameraLookAtPoint : MonoBehaviour
    {
        private static readonly Dictionary<string, CameraLookAtPoint> Points =
            new Dictionary<string, CameraLookAtPoint>(StringComparer.Ordinal);

        [SerializeField, Tooltip("대화 액션에서 참조할 씬 내 고유 ID.")]
        private string _pointId;

        [SerializeField, Min(0.01f), Tooltip("Scene 뷰에서 표시할 기즈모 크기.")]
        private float _gizmoRadius = 0.15f;

        [NonSerialized] private string _registeredId;

        public string PointId => _pointId;

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _pointId = _pointId?.Trim();
            _gizmoRadius = Mathf.Max(0.01f, _gizmoRadius);

            if (Application.isPlaying && isActiveAndEnabled)
                Register();
        }

        private void OnDrawGizmos()
        {
            Color previousColor = Gizmos.color;
            Gizmos.color = string.IsNullOrWhiteSpace(_pointId)
                ? new Color(1f, 0.3f, 0.2f, 0.9f)
                : new Color(0.25f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(transform.position, _gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * (_gizmoRadius * 2f));
            Gizmos.color = previousColor;
        }
#endif

        /// <summary>등록된 활성 주시 지점을 ID로 찾는다.</summary>
        public static bool TryResolve(string pointId, out Transform point)
        {
            point = null;
            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            string normalizedId = pointId.Trim();
            if (!Points.TryGetValue(normalizedId, out CameraLookAtPoint registered)
                || registered == null
                || !registered.isActiveAndEnabled)
            {
                Points.Remove(normalizedId);
                return false;
            }

            point = registered.transform;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            Points.Clear();
        }

        private void Register()
        {
            Unregister();

            string normalizedId = _pointId?.Trim();
            if (string.IsNullOrEmpty(normalizedId))
                return;

            if (Points.TryGetValue(normalizedId, out CameraLookAtPoint existing)
                && existing != null
                && existing != this)
            {
                Debug.LogError(
                    $"[CameraLookAtPoint] 중복 ID '{normalizedId}'가 있습니다. 씬 내 ID는 고유해야 합니다.",
                    this);
                return;
            }

            Points[normalizedId] = this;
            _registeredId = normalizedId;
        }

        private void Unregister()
        {
            if (string.IsNullOrEmpty(_registeredId))
                return;

            if (Points.TryGetValue(_registeredId, out CameraLookAtPoint registered)
                && registered == this)
            {
                Points.Remove(_registeredId);
            }

            _registeredId = null;
        }
    }
}
