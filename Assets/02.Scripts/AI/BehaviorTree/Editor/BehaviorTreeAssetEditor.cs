#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [CustomEditor(typeof(BehaviorTreeAsset))]
    public class BehaviorTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Behavior Tree Editor 열기"))
                BehaviorTreeEditorWindow.Open(target as BehaviorTreeAsset);
        }
    }
}
#endif
