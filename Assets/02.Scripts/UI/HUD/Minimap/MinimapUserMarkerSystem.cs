using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 플레이어가 맵에서 직접 찍는 사용자 마커 데이터.
    /// </summary>
    public class UserMapMarker
    {
        public int     Id;
        public Vector3 WorldPosition;
        public string  Label;
    }

    /// <summary>
    /// 런타임 사용자 마커를 관리하는 정적 시스템.
    ///
    /// ■ 사용 방법
    ///   · 마커 추가: <see cref="AddMarker"/>
    ///   · 마커 제거: <see cref="RemoveMarker"/> 또는 <see cref="RemoveAll"/>
    ///   · UI는 <see cref="OnMarkerAdded"/> / <see cref="OnMarkerRemoved"/> / <see cref="OnAllMarkersCleared"/> 이벤트를 구독
    ///
    /// ■ 씬 전환
    ///   씬 전환 시 <see cref="RemoveAll"/>을 호출해 마커를 초기화하거나,
    ///   씬별 마커를 유지하려면 별도 저장/복원 로직을 추가하세요.
    /// </summary>
    public static class MinimapUserMarkerSystem
    {
        private static readonly Dictionary<int, UserMapMarker> _markers = new();
        private static int _nextId;

        public static event Action<UserMapMarker> OnMarkerAdded;
        public static event Action<UserMapMarker> OnMarkerRemoved;
        public static event Action               OnAllMarkersCleared;

        // ── CRUD ────────────────────────────────────────────────

        /// <summary>월드 좌표에 사용자 마커를 추가합니다. 추가된 마커를 반환합니다.</summary>
        public static UserMapMarker AddMarker(Vector3 worldPos, string label = "")
        {
            var marker = new UserMapMarker
            {
                Id            = _nextId++,
                WorldPosition = worldPos,
                Label         = label,
            };
            _markers[marker.Id] = marker;
            OnMarkerAdded?.Invoke(marker);
            return marker;
        }

        /// <summary>ID로 마커를 제거합니다. 성공 여부를 반환합니다.</summary>
        public static bool RemoveMarker(int id)
        {
            if (!_markers.TryGetValue(id, out var marker)) return false;
            _markers.Remove(id);
            OnMarkerRemoved?.Invoke(marker);
            return true;
        }

        /// <summary>모든 마커를 제거합니다.</summary>
        public static void RemoveAll()
        {
            _markers.Clear();
            OnAllMarkersCleared?.Invoke();
        }

        // ── 조회 ────────────────────────────────────────────────

        public static IReadOnlyList<UserMapMarker> GetAll() => new List<UserMapMarker>(_markers.Values);

        public static bool TryGet(int id, out UserMapMarker marker)
            => _markers.TryGetValue(id, out marker);

        public static int Count => _markers.Count;
    }
}
