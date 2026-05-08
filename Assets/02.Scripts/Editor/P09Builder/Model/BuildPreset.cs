using System;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// CharacterBuildConfig의 스냅샷을 ScriptableObject로 저장.
    /// 빌더 윈도우에서 Save/Load 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "P09_BuildPreset_", menuName = "UPlayGround/P09Builder/Build Preset")]
    public class BuildPreset : ScriptableObject
    {
        public CharacterBuildConfig config = new CharacterBuildConfig();
        public string description;

        [SerializeField] private string _createdAtIso;

        public DateTime CreatedAt
        {
            get
            {
                if (string.IsNullOrEmpty(_createdAtIso)) return DateTime.MinValue;
                return DateTime.TryParse(_createdAtIso, out var dt) ? dt : DateTime.MinValue;
            }
            set { _createdAtIso = value.ToString("o"); }
        }
    }
}
