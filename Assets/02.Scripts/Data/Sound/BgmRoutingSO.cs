using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Sound
{
    /// <summary>
    /// 씬 타입 / 맵 ID → BGM key 매핑 테이블.
    /// SoundManager가 씬 전환(OnSceneChanged) 시 이 테이블로 평시(베이스) BGM을 결정한다.
    ///
    /// 우선순위: mapId 매칭 > sceneType 매칭 > (매칭 없음 → 현재 BGM 유지).
    /// bgmKey를 비워 둔 라우트가 매칭되면 "이 씬/맵에서는 BGM 정지" 의도로 해석한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BgmRouting", menuName = "UPlayGround/오디오/BGM Routing")]
    public sealed class BgmRoutingSO : ScriptableObject
    {
        [Serializable]
        public struct MapRoute
        {
            [Tooltip("SceneContext.MapID 와 동일한 문자열")]
            public string mapId;

            [Tooltip("재생할 BGM의 SoundDatabase key. 비우면 이 맵에서는 BGM 정지.")]
            public string bgmKey;
        }

        [Serializable]
        public struct SceneRoute
        {
            [Tooltip("SceneType 상수 문자열 (Title / GamePlay 등)")]
            public string sceneType;

            [Tooltip("재생할 BGM의 SoundDatabase key. 비우면 이 씬 타입에서는 BGM 정지.")]
            public string bgmKey;
        }

        [Header("맵별 BGM (더 구체적 — 우선 적용)")]
        [SerializeField] private List<MapRoute> mapRoutes = new();

        [Header("씬 타입별 BGM (맵 매칭이 없을 때 폴백)")]
        [SerializeField] private List<SceneRoute> sceneRoutes = new();

        /// <summary>
        /// 씬 타입/맵 ID에 해당하는 평시 BGM을 결정한다.
        /// 반환값은 "라우트가 매칭됐는가". 매칭됐지만 bgmKey가 비어 있으면 정지 의도다(out은 빈 문자열).
        /// 매칭 자체가 없으면 false를 반환하며, 이 경우 호출부는 현재 BGM을 그대로 유지해야 한다.
        /// </summary>
        public bool TryResolve(string sceneType, string mapId, out string bgmKey)
        {
            if (!string.IsNullOrEmpty(mapId))
            {
                foreach (var route in mapRoutes)
                {
                    if (route.mapId == mapId)
                    {
                        bgmKey = route.bgmKey;
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
                        bgmKey = route.bgmKey;
                        return true;
                    }
                }
            }

            bgmKey = null;
            return false;
        }
    }
}
