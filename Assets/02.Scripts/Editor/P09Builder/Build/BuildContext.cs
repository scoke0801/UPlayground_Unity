using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class BuildContext
    {
        public CharacterBuildConfig Config { get; }
        public GameObject RootInstance { get; set; }
        public string PrefabFolder { get; set; }
        public string PrefabName { get; set; }
        public List<ScriptableObject> GeneratedDescs { get; } = new();
        public List<string> GeneratedAssetPaths { get; } = new();
        public List<string> Logs { get; } = new();
        public Dictionary<string, object> Bag { get; } = new();

        public BuildContext(CharacterBuildConfig config)
        {
            Config = config;
        }
    }
}
