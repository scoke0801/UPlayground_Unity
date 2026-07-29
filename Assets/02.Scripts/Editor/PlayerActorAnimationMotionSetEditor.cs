using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Actor.Animation.Editor
{
    [CustomEditor(typeof(PlayerActorAnimationMotionSet))]
    public class PlayerActorAnimationMotionSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("motionSets"), true);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("애니메이션 에디터에서 열기", GUILayout.Height(24)))
                UPlayGround.Animation.Editor.MotionEditorProjectEntry.Open(
                    (PlayerActorAnimationMotionSet)target);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
