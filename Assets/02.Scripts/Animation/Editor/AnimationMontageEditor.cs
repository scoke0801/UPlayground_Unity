using UnityEngine;
using UnityEditor;

namespace Animation.Editor
{
    /// <summary>
    /// 애니메이션 몽타쥬를 위한 커스텀 에디터
    /// Inspector에서 더 나은 편집 경험 제공
    /// </summary>
    [CustomEditor(typeof(AnimationMontage))]
    public class AnimationMontageEditor : UnityEditor.Editor
    {
        private SerializedProperty montageName;
        private SerializedProperty slotName;
        private SerializedProperty sections;
        
        private bool showSections = true;
        
        private void OnEnable()
        {
            montageName = serializedObject.FindProperty("montageName");
            slotName = serializedObject.FindProperty("slotName");
            sections = serializedObject.FindProperty("sections");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Montage Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 몽타쥬 기본 정보
            EditorGUILayout.PropertyField(montageName, new GUIContent("Montage Name"));
            EditorGUILayout.PropertyField(slotName, new GUIContent("Slot Name"));
            
            EditorGUILayout.Space(10);
            
            // 섹션 리스트
            showSections = EditorGUILayout.Foldout(showSections, $"Sections ({sections.arraySize})", true, EditorStyles.foldoutHeader);
            
            if (showSections)
            {
                EditorGUI.indentLevel++;
                
                // 섹션 추가/제거 버튼
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Section", GUILayout.Height(25)))
                {
                    sections.InsertArrayElementAtIndex(sections.arraySize);
                    var newSection = sections.GetArrayElementAtIndex(sections.arraySize - 1);
                    InitializeSection(newSection, sections.arraySize - 1);
                }
                
                GUI.enabled = sections.arraySize > 0;
                if (GUILayout.Button("Remove Last", GUILayout.Height(25)))
                {
                    sections.DeleteArrayElementAtIndex(sections.arraySize - 1);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // 각 섹션 표시
                for (int i = 0; i < sections.arraySize; i++)
                {
                    DrawSection(sections.GetArrayElementAtIndex(i), i);
                    EditorGUILayout.Space(5);
                }
                
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // 정보 박스
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Tip: 섹션 순서대로 자동 재생됩니다. Next Section을 지정하면 해당 섹션으로 점프합니다.",
                MessageType.Info);
        }
        
        private void DrawSection(SerializedProperty section, int index)
        {
            var sectionName = section.FindPropertyRelative("sectionName");
            var clip = section.FindPropertyRelative("clip");
            var fadeInDuration = section.FindPropertyRelative("fadeInDuration");
            var fadeOutDuration = section.FindPropertyRelative("fadeOutDuration");
            var playRate = section.FindPropertyRelative("playRate");
            var loop = section.FindPropertyRelative("loop");
            var nextSection = section.FindPropertyRelative("nextSection");
            var notifies = section.FindPropertyRelative("notifies");
            
            // 섹션 박스
            EditorGUILayout.BeginVertical("box");
            
            // 섹션 헤더
            EditorGUILayout.BeginHorizontal();
            section.isExpanded = EditorGUILayout.Foldout(
                section.isExpanded,
                $"Section {index}: {sectionName.stringValue}",
                true,
                EditorStyles.foldoutHeader);
            
            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
            {
                sections.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            if (section.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // 기본 설정
                EditorGUILayout.PropertyField(sectionName, new GUIContent("Section Name"));
                EditorGUILayout.PropertyField(clip, new GUIContent("Animation Clip"));
                
                EditorGUILayout.Space(3);
                
                // 페이드 설정
                EditorGUILayout.LabelField("Fade Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(fadeInDuration, new GUIContent("Fade In"));
                EditorGUILayout.PropertyField(fadeOutDuration, new GUIContent("Fade Out"));
                
                EditorGUILayout.Space(3);
                
                // 재생 설정
                EditorGUILayout.LabelField("Playback Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(playRate, new GUIContent("Play Rate"));
                EditorGUILayout.PropertyField(loop, new GUIContent("Loop"));
                EditorGUILayout.PropertyField(nextSection, new GUIContent("Next Section"));
                
                if (!string.IsNullOrEmpty(nextSection.stringValue))
                {
                    EditorGUILayout.HelpBox(
                        $"이 섹션 후 '{nextSection.stringValue}' 섹션으로 이동합니다.",
                        MessageType.Info);
                }
                
                EditorGUILayout.Space(3);
                
                // 노티파이
                EditorGUILayout.PropertyField(notifies, new GUIContent("Notifies"), true);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void InitializeSection(SerializedProperty section, int index)
        {
            section.FindPropertyRelative("sectionName").stringValue = $"Section{index}";
            section.FindPropertyRelative("fadeInDuration").floatValue = 0.25f;
            section.FindPropertyRelative("fadeOutDuration").floatValue = 0.25f;
            section.FindPropertyRelative("playRate").floatValue = 1.0f;
            section.FindPropertyRelative("loop").boolValue = false;
            section.FindPropertyRelative("nextSection").stringValue = "";
        }
    }
}
