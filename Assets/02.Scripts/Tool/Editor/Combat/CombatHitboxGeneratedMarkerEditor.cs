#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Tool.Editor.Combat
{
    [CustomEditor(typeof(CombatHitboxGeneratedMarker))]
    public sealed class CombatHitboxGeneratedMarkerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var marker = (CombatHitboxGeneratedMarker)target;
            EditorGUILayout.Space(4);
            if (!marker.ManuallyModified && GUILayout.Button("수동 수정으로 표시"))
            {
                Undo.RecordObject(marker, "HitBox 수동 수정 표시");
                marker.MarkManuallyModified();
                EditorUtility.SetDirty(marker);
            }
        }
    }
}
#endif
