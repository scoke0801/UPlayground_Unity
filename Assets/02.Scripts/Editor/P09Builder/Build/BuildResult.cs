using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public readonly struct BuildResult
    {
        public bool Success { get; }
        public GameObject Prefab { get; }
        public string PrefabPath { get; }
        public List<string> GeneratedAssetPaths { get; }
        public List<string> Logs { get; }
        public string ErrorMessage { get; }

        private BuildResult(bool success, GameObject prefab, string prefabPath,
            List<string> assets, List<string> logs, string error)
        {
            Success = success;
            Prefab = prefab;
            PrefabPath = prefabPath;
            GeneratedAssetPaths = assets ?? new List<string>();
            Logs = logs ?? new List<string>();
            ErrorMessage = error;
        }

        public static BuildResult Ok(GameObject prefab, string path, List<string> assets, List<string> logs)
            => new BuildResult(true, prefab, path, assets, logs, null);

        public static BuildResult Fail(string error)
            => new BuildResult(false, null, null, null, null, error);

        public static BuildResult Fail(string error, List<string> logs)
            => new BuildResult(false, null, null, null, logs, error);
    }
}
