#if UNITY_EDITOR
using System.IO;
using UPlayGround.AI.Debugging;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal static class EncounterReplayLoader
    {
        public static EncounterReplay LoadFromFilePanel()
        {
            var defaultPath = Path.Combine(Application.persistentDataPath, "EncounterReplays");
            var path = EditorUtility.OpenFilePanel("Encounter Replay JSON 열기", defaultPath, "json");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return JsonUtility.FromJson<EncounterReplay>(File.ReadAllText(path));
        }
    }
}
#endif
