using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Cinematic
{
    /// <summary>
    /// 부팅 중 Additive로 읽어 DDOL 비활성 루트로 보관할 시네마틱 무대 씬 목록.
    /// Builder가 CinematicStageSO의 sceneName을 기준으로 갱신한다.
    /// </summary>
    public sealed class CinematicStagePreloadCatalogSO : ScriptableObject
    {
        [SerializeField] private List<string> _sceneNames = new();

        public IReadOnlyList<string> SceneNames => _sceneNames;

        public void SetSceneNames(IEnumerable<string> sceneNames)
        {
            _sceneNames.Clear();
            if (sceneNames == null)
                return;

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string sceneName in sceneNames)
            {
                if (!string.IsNullOrWhiteSpace(sceneName) && unique.Add(sceneName))
                    _sceneNames.Add(sceneName);
            }

            _sceneNames.Sort(StringComparer.Ordinal);
        }
    }
}
