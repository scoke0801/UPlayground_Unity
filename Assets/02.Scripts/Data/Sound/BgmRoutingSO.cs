using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    /// <summary>
    /// 씬 타입 / 맵 ID에 매칭된 평시 BGM을 결정한 결과.
    /// 단일 곡(BgmKey) 또는 플레이리스트(Playlist) 중 하나로 해석되며,
    /// 둘 다 비어 있으면 "이 씬/맵에서는 BGM 정지"(IsStop) 의도다.
    /// </summary>
    public struct BgmRouteResult
    {
        public string BgmKey;
        public BgmPlaylistSO Playlist;

        public bool HasPlaylist => Playlist != null && Playlist.Count > 0;
        public bool IsStop => !HasPlaylist && string.IsNullOrWhiteSpace(BgmKey);
    }

    /// <summary>
    /// 씬 타입 / 맵 ID → BGM 매핑 테이블.
    /// SoundManager가 씬 전환(OnSceneChanged) 시 이 테이블로 평시(베이스) BGM을 결정한다.
    ///
    /// 우선순위: mapId 매칭 > sceneType 매칭 > (매칭 없음 → 현재 BGM 유지).
    /// 한 라우트는 단일 곡(bgmKey) 또는 플레이리스트(playlist)를 가리킬 수 있다.
    /// 둘 다 비어 있는 라우트가 매칭되면 "이 씬/맵에서는 BGM 정지" 의도로 해석한다.
    /// playlist가 설정돼 있으면 bgmKey보다 우선한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BgmRouting", menuName = "UPlayGround/오디오/BGM Routing")]
    public sealed class BgmRoutingSO : ScriptableObject
    {
        [Serializable]
        public struct MapRoute
        {
            [Tooltip("SceneContext.MapID 와 동일한 문자열")]
            public string mapId;

            [Tooltip("재생할 BGM의 SoundDatabase key. 비우면 정지(또는 playlist 사용).")]
            public string bgmKey;

            [Tooltip("여러 곡을 번갈아 재생할 플레이리스트. 설정 시 bgmKey보다 우선.")]
            public BgmPlaylistSO playlist;
        }

        [Serializable]
        public struct SceneRoute
        {
            [Tooltip("SceneType 상수 문자열 (Title / GamePlay 등)")]
            public string sceneType;

            [Tooltip("재생할 BGM의 SoundDatabase key. 비우면 정지(또는 playlist 사용).")]
            public string bgmKey;

            [Tooltip("여러 곡을 번갈아 재생할 플레이리스트. 설정 시 bgmKey보다 우선.")]
            public BgmPlaylistSO playlist;
        }

        [Header("맵별 BGM (더 구체적 — 우선 적용)")]
        [SerializeField] private List<MapRoute> mapRoutes = new();

        [Header("씬 타입별 BGM (맵 매칭이 없을 때 폴백)")]
        [SerializeField] private List<SceneRoute> sceneRoutes = new();

        /// <summary>
        /// 씬 타입/맵 ID에 해당하는 평시 BGM을 결정한다.
        /// 반환값은 "라우트가 매칭됐는가". 매칭됐지만 곡/플레이리스트가 모두 비어 있으면
        /// 정지 의도다(result.IsStop). 매칭 자체가 없으면 false를 반환하며,
        /// 이 경우 호출부는 현재 BGM을 그대로 유지해야 한다.
        /// </summary>
        public bool TryResolve(string sceneType, string mapId, out BgmRouteResult result)
        {
            if (!string.IsNullOrEmpty(mapId))
            {
                foreach (var route in mapRoutes)
                {
                    if (route.mapId == mapId)
                    {
                        result = new BgmRouteResult { BgmKey = route.bgmKey, Playlist = route.playlist };
                        return true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(sceneType))
            {
                foreach (var route in sceneRoutes)
                {
                    if (route.sceneType == sceneType)
                    {
                        result = new BgmRouteResult { BgmKey = route.bgmKey, Playlist = route.playlist };
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }
    }
}
