using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionSetAsset에 대한 커스텀 인스펙터.
    /// 에셋을 선택했을 때 전용 에디터 창을 열 수 있는 버튼을 제공합니다.
    /// </summary>
    [CustomEditor(typeof(MotionSetAsset))]
    public class MotionSetAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 기본 인스펙터 필드 표시 (MotionSet 데이터 등)
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            
            // 시각적 구분을 위한 라인
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(10);

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f); // 버튼 강조 색상 (하늘색)
            if (GUILayout.Button("Open Motion Set Editor Window", GUILayout.Height(40)))
            {
                // 메뉴 아이템으로 등록된 Open() 메서드를 호출하거나 직접 윈도우 생성
                // GetWindow를 통해 기존 창이 있으면 포커스하고, 없으면 새로 엽니다.
                var window = EditorWindow.GetWindow<MotionSetEditorWindow>();
                window.titleContent = new GUIContent("모션 셋 에디터");
                window.Show();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.HelpBox("위 버튼을 누르면 타임라인 기반의 상세 편집 창이 열립니다.", MessageType.Info);
        }
    }
}