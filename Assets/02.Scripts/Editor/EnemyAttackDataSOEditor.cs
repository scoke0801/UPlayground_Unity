using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// EnemyAttackDataSO 커스텀 인스펙터.
    /// </summary>
    [CustomEditor(typeof(EnemyAttackDataSO))]
    public class EnemyAttackDataSOEditor : UnityEditor.Editor
    {
        private EnemyAttackDataSODrawer _drawer;

        void OnEnable()
        {
            _drawer = new EnemyAttackDataSODrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _drawer.DrawGUI();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);

            GUI.backgroundColor = new Color(1f, 0.55f, 0.25f);
            if (GUILayout.Button("MotionSet 기반 생성기 열기", GUILayout.Height(26)))
                AttackDataFromMotionSetWindow.Open((EnemyAttackDataSO)target);
            GUI.backgroundColor = Color.white;
        }
    }
}
