#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Validation
{
    public enum EditorValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct EditorValidationIssue
    {
        public readonly EditorValidationSeverity Severity;
        public readonly string Domain;
        public readonly string AssetPath;
        public readonly UnityEngine.Object Asset;
        public readonly string Field;
        public readonly string Message;
        public readonly string FixHint;

        public EditorValidationIssue(
            EditorValidationSeverity severity,
            string domain,
            string assetPath,
            UnityEngine.Object asset,
            string field,
            string message,
            string fixHint = "")
        {
            Severity = severity;
            Domain = domain;
            AssetPath = assetPath;
            Asset = asset;
            Field = field;
            Message = message;
            FixHint = fixHint;
        }

        public MessageType ToMessageType()
        {
            return Severity switch
            {
                EditorValidationSeverity.Error => MessageType.Error,
                EditorValidationSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
        }
    }
}
#endif
