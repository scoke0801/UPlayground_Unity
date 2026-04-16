using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// PlayerAttackDataSO 커스텀 인스펙터.
    /// </summary>
    [CustomEditor(typeof(PlayerAttackDataSO))]
    public class PlayerAttackDataSOEditor : UnityEditor.Editor
    {
        internal PlayerAttackDataSODrawer Drawer { get; private set; }

        void OnEnable()
        {
            Drawer = new PlayerAttackDataSODrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Drawer.DrawGUI();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("에디터 창에서 열기", GUILayout.Height(30)))
                PlayerAttackDataSOWindow.Open((PlayerAttackDataSO)target);
            GUI.backgroundColor = Color.white;
        }
    }
}
