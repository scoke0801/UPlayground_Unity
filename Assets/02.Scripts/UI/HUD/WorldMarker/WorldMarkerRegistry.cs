using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인게임 HUD에 노출할 월드 마커 1개의 데이터.
    /// 월드 위치는 <see cref="follow"/>(Transform 추종) 또는 <see cref="staticPosition"/>(고정 좌표) 중 하나로 결정된다.
    /// Transform이 지정되면 매 프레임 그 위치를 따라가고, null이면 고정 좌표를 사용한다.
    /// </summary>
    public sealed class WorldMarkerData
    {
        /// <summary>마커 식별자. Register/Remove의 키.</summary>
        public readonly string id;

        /// <summary>추종할 대상 Transform. null이면 <see cref="staticPosition"/>을 사용한다.</summary>
        public Transform follow;

        /// <summary>Transform이 없을 때 사용하는 고정 월드 좌표.</summary>
        public Vector3 staticPosition;

        /// <summary>마커 아이콘 스프라이트.</summary>
        public Sprite icon;

        /// <summary>아이콘 틴트 색상.</summary>
        public Color color;

        /// <summary>true면 거리 라벨을 숨긴다(Config가 전역으로 켜져 있어도 이 마커만 개별 숨김).</summary>
        public bool hideDistance;

        public WorldMarkerData(string id, Transform follow, Sprite icon, Color color, bool hideDistance)
        {
            this.id = id;
            this.follow = follow;
            this.icon = icon;
            this.color = color;
            this.hideDistance = hideDistance;
        }

        public WorldMarkerData(string id, Vector3 staticPosition, Sprite icon, Color color, bool hideDistance)
        {
            this.id = id;
            this.staticPosition = staticPosition;
            this.icon = icon;
            this.color = color;
            this.hideDistance = hideDistance;
        }

        /// <summary>현재 유효한 월드 좌표. 추종 Transform이 살아 있으면 그 위치, 아니면 고정 좌표.</summary>
        public Vector3 WorldPosition => follow != null ? follow.position : staticPosition;

        /// <summary>추종 대상이 지정됐지만 파괴된 경우 true. 등록 해제 대상.</summary>
        public bool IsFollowLost => !ReferenceEquals(follow, null) && follow == null;
    }

    /// <summary>
    /// 인게임 월드 마커를 런타임에 추적하는 범용 정적 레지스트리.
    /// 어떤 시스템(퀘스트/사이클/상호작용/디버그 등)이든 여기에 마커를 등록하면
    /// <see cref="UI_HUD_WorldMarker"/>가 이를 화면에 투영해 노출한다.
    ///
    /// 씬 전환 시 남은 마커는 <see cref="Clear"/>로 정리한다. Transform 추종 마커는
    /// 대상이 파괴되면 표시 측에서 자동으로 정리된다(<see cref="WorldMarkerData.IsFollowLost"/>).
    /// </summary>
    public static class WorldMarkerRegistry
    {
        private static readonly Dictionary<string, WorldMarkerData> _markers = new();
        // 핫패스 순회용 병렬 리스트 — IReadOnlyDictionary foreach의 열거자 boxing(프레임당 GC)을 피한다.
        private static readonly List<WorldMarkerData> _ordered = new();

        public static event Action<WorldMarkerData> OnMarkerAdded;
        public static event Action<WorldMarkerData> OnMarkerChanged;
        public static event Action<string> OnMarkerRemoved;

        /// <summary>인덱스로 순회 가능한 활성 마커 목록. 표시 측 핫패스에서 무할당 순회에 사용한다.</summary>
        public static IReadOnlyList<WorldMarkerData> Active => _ordered;
        public static int Count => _markers.Count;

        public static bool TryGet(string id, out WorldMarkerData marker) => _markers.TryGetValue(id, out marker);
        public static bool Contains(string id) => _markers.ContainsKey(id);

        /// <summary>
        /// 마커를 등록하거나 갱신한다. 같은 id가 있으면 데이터를 교체하고 Changed 이벤트를 발생시킨다.
        /// </summary>
        public static void Register(WorldMarkerData marker)
        {
            if (marker == null || string.IsNullOrEmpty(marker.id)) return;
            bool existed = _markers.TryGetValue(marker.id, out WorldMarkerData old);
            _markers[marker.id] = marker;
            if (existed)
            {
                int idx = _ordered.IndexOf(old);
                if (idx >= 0) _ordered[idx] = marker; else _ordered.Add(marker);
                OnMarkerChanged?.Invoke(marker);
            }
            else
            {
                _ordered.Add(marker);
                OnMarkerAdded?.Invoke(marker);
            }
        }

        /// <summary>Transform 추종 마커 등록 편의 오버로드.</summary>
        public static void Register(string id, Transform follow, Sprite icon, Color? color = null, bool hideDistance = false)
            => Register(new WorldMarkerData(id, follow, icon, color ?? Color.white, hideDistance));

        /// <summary>고정 좌표 마커 등록 편의 오버로드.</summary>
        public static void Register(string id, Vector3 worldPosition, Sprite icon, Color? color = null, bool hideDistance = false)
            => Register(new WorldMarkerData(id, worldPosition, icon, color ?? Color.white, hideDistance));

        public static void Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_markers.TryGetValue(id, out WorldMarkerData marker))
            {
                _markers.Remove(id);
                _ordered.Remove(marker);
                OnMarkerRemoved?.Invoke(id);
            }
        }

        public static void Clear()
        {
            if (_markers.Count == 0) return;
            string[] ids = new string[_markers.Count];
            _markers.Keys.CopyTo(ids, 0);
            _markers.Clear();
            _ordered.Clear();
            foreach (string id in ids) OnMarkerRemoved?.Invoke(id);
        }
    }
}
