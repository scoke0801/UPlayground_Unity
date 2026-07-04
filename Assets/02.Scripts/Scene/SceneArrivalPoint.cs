using System.Collections.Generic;
using UnityEngine;

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

        public string     Id       => _id;
        public Vector3    Position => transform.position;
        public Quaternion Rotation => transform.rotation;

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
        private static readonly Dictionary<string, SceneArrivalPoint> _map = new();

        public static void Register(SceneArrivalPoint point)
        {
            if (point == null || string.IsNullOrEmpty(point.Id)) return;
            _map[point.Id] = point;
        }

        public static void Unregister(SceneArrivalPoint point)
        {
            if (point == null || string.IsNullOrEmpty(point.Id)) return;
            if (_map.TryGetValue(point.Id, out var stored) && stored == point)
                _map.Remove(point.Id);
        }

        public static bool TryGet(string id, out SceneArrivalPoint point)
            => _map.TryGetValue(id, out point) && point != null;
    }
}
