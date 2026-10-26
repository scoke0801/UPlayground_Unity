using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround
{
    /// <summary>
    /// 파스트트래블 도착 지점. 씬에 배치하고 Id를 부여하면,
    /// 다른 지역 맵에서 이 씬으로 이동(SceneManager 파스트트래블)할 때
    /// 플레이어가 이 지점에 스폰된다.
    ///
    /// MapRegionInfoSO.PortalEntry.arrivalId 와 동일한 Id로 맞춘다.
    /// </summary>
    public class SceneArrivalPoint : MonoBehaviour
    {
        [Tooltip("MapRegionInfoSO.PortalEntry.arrivalId 와 일치시킬 식별자")]
        [SerializeField] private string _id;
        [Tooltip("이 도착점에서 플레이어와 카메라 준비를 마친 뒤 해금할 지도 거점. 비우면 해금하지 않는다.")]
        [SerializeField] private string _activationId;

        public string     Id       => _id;
        public Vector3    Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public string ActivationId => _activationId;

        private void Awake()     => SceneArrivalRegistry.Register(this);
        private void OnDestroy() => SceneArrivalRegistry.Unregister(this);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, $"[Arrival] {_id}");
        }
#endif
    }

    /// <summary>
    /// 씬에 배치된 <see cref="SceneArrivalPoint"/>를 런타임에 추적하는 정적 레지스트리.
    /// 씬 전환 시 오브젝트가 파괴되면 OnDestroy에서 자동 해제된다.
    /// </summary>
    public static class SceneArrivalRegistry
    {
        private static readonly Dictionary<string, HashSet<SceneArrivalPoint>> _map = new();

        public static void Register(SceneArrivalPoint point)
        {
            if (point == null || string.IsNullOrEmpty(point.Id)) return;
            if (!_map.TryGetValue(point.Id, out var points))
                _map.Add(point.Id, points = new HashSet<SceneArrivalPoint>());
            points.Add(point);
        }

        public static void Unregister(SceneArrivalPoint point)
        {
            if (point == null || string.IsNullOrEmpty(point.Id)) return;
            if (_map.TryGetValue(point.Id, out var points))
            {
                points.Remove(point);
                if (points.Count == 0) _map.Remove(point.Id);
            }
        }

        public static bool TryGet(string id, out SceneArrivalPoint point)
        {
            return TryFindUnique(id, null, out point);
        }

        /// <summary>다른 지역의 같은 ID와 구별하여 지정 씬 안의 유일한 도착점만 반환한다.</summary>
        public static bool TryGet(string id, Scene scene, out SceneArrivalPoint point)
        {
            point = null;
            return scene.IsValid() && scene.isLoaded && TryFindUnique(id, scene.handle, out point);
        }

        private static bool TryFindUnique(string id, int? sceneHandle, out SceneArrivalPoint point)
        {
            point = null;
            if (string.IsNullOrEmpty(id) || !_map.TryGetValue(id, out var points))
                return false;

            foreach (var candidate in points)
            {
                if (candidate == null || !candidate.gameObject.scene.isLoaded
                    || (sceneHandle.HasValue && candidate.gameObject.scene.handle != sceneHandle.Value))
                    continue;

                if (point != null)
                {
                    point = null;
                    return false;
                }
                point = candidate;
            }
            return point != null;
        }
    }
}
