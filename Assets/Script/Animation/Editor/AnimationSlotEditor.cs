using UnityEngine;
using UnityEditor;

namespace Animation.Editor
{
    /// <summary>
    /// AnimationSlot의 커스텀 인스펙터
    /// </summary>
    [CustomEditor(typeof(AnimationSlot))]
    public class AnimationSlotEditor : UnityEditor.Editor
    {
        private SerializedProperty slotName;
        private SerializedProperty groupName;
        private SerializedProperty layerIndex;
        private SerializedProperty layerWeight;
        private SerializedProperty avatarMask;
        private SerializedProperty blendingMode;
        
        private void OnEnable()
        {
            slotName = serializedObject.FindProperty("slotName");
            groupName = serializedObject.FindProperty("groupName");
            layerIndex = serializedObject.FindProperty("layerIndex");
            layerWeight = serializedObject.FindProperty("layerWeight");
            avatarMask = serializedObject.FindProperty("avatarMask");
            blendingMode = serializedObject.FindProperty("blendingMode");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("애니메이션 슬롯", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "슬롯은 특정 레이어와 Avatar Mask를 사용하여\n" +
                "캐릭터의 특정 부위(상체/하체 등)만 제어합니다.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 슬롯 정보
            EditorGUILayout.LabelField("슬롯 정보", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(slotName, new GUIContent("슬롯 이름"));
            EditorGUILayout.PropertyField(groupName, new GUIContent("그룹 이름"));
            
            EditorGUILayout.Space(10);
            
            // 레이어 설정
            EditorGUILayout.LabelField("레이어 설정", EditorStyles.miniBoldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(layerIndex, new GUIContent("레이어 인덱스"));
            
            if (EditorGUI.EndChangeCheck() && layerIndex.intValue < 0)
            {
                layerIndex.intValue = 0;
            }
            
            if (layerIndex.intValue == 0)
            {
                EditorGUILayout.HelpBox("레이어 0은 기본 레이어입니다. 전신 애니메이션에 사용됩니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(layerWeight, new GUIContent("레이어 가중치"));
                layerWeight.floatValue = Mathf.Clamp01(layerWeight.floatValue);
            }
            
            EditorGUILayout.Space(10);
            
            // Avatar Mask
            EditorGUILayout.LabelField("본 마스크", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(avatarMask, new GUIContent("Avatar Mask"));
            
            if (avatarMask.objectReferenceValue == null && layerIndex.intValue > 0)
            {
                EditorGUILayout.HelpBox(
                    "Avatar Mask가 없으면 모든 본이 제어됩니다.\n" +
                    "상체/하체를 분리하려면 Avatar Mask를 생성하세요.",
                    MessageType.Warning);
                
                if (GUILayout.Button("Avatar Mask 생성 방법 보기"))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/class-AvatarMask.html");
                }
            }
            
            EditorGUILayout.Space(10);
            
            // 블렌딩 모드
            EditorGUILayout.LabelField("블렌딩 설정", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(blendingMode, new GUIContent("블렌딩 모드"));
            
            // 블렌딩 모드 설명
            DrawBlendingModeInfo(blendingMode.enumValueIndex);
            
            serializedObject.ApplyModifiedProperties();
            
            // 미리보기 정보
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("슬롯 요약", EditorStyles.boldLabel);
            DrawSlotSummary();
        }
        
        private void DrawBlendingModeInfo(int modeIndex)
        {
            string info = modeIndex switch
            {
                0 => "Override: 이 레이어가 이전 레이어를 완전히 덮어씁니다. (일반적인 애니메이션)",
                1 => "Additive: 이 레이어의 애니메이션이 이전 레이어에 더해집니다. (호흡, 몸 떨림 등)",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(info))
            {
                EditorGUILayout.HelpBox(info, MessageType.None);
            }
        }
        
        private void DrawSlotSummary()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("슬롯 이름:", slotName.stringValue);
            EditorGUILayout.LabelField("그룹:", groupName.stringValue);
            EditorGUILayout.LabelField("레이어:", layerIndex.intValue.ToString());
            
            if (layerIndex.intValue > 0)
            {
                EditorGUILayout.LabelField("가중치:", layerWeight.floatValue.ToString("F2"));
            }
            
            var mask = avatarMask.objectReferenceValue as AvatarMask;
            EditorGUILayout.LabelField("Avatar Mask:", mask != null ? mask.name : "없음");
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 프리셋 버튼
            EditorGUILayout.LabelField("프리셋", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("FullBody 프리셋"))
            {
                ApplyFullBodyPreset();
            }
            
            if (GUILayout.Button("UpperBody 프리셋"))
            {
                ApplyUpperBodyPreset();
            }
            
            if (GUILayout.Button("LowerBody 프리셋"))
            {
                ApplyLowerBodyPreset();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void ApplyFullBodyPreset()
        {
            slotName.stringValue = "FullBody";
            groupName.stringValue = "DefaultGroup";
            layerIndex.intValue = 0;
            layerWeight.floatValue = 1.0f;
            avatarMask.objectReferenceValue = null;
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("프리셋 적용", "FullBody 프리셋이 적용되었습니다.", "확인");
        }
        
        private void ApplyUpperBodyPreset()
        {
            slotName.stringValue = "UpperBody";
            groupName.stringValue = "DefaultGroup";
            layerIndex.intValue = 1;
            layerWeight.floatValue = 1.0f;
            blendingMode.enumValueIndex = 0; // Override
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("프리셋 적용", 
                "UpperBody 프리셋이 적용되었습니다.\n\n" +
                "다음 단계: 상체 본만 활성화된 Avatar Mask를 생성하여 할당하세요.",
                "확인");
        }
        
        private void ApplyLowerBodyPreset()
        {
            slotName.stringValue = "LowerBody";
            groupName.stringValue = "DefaultGroup";
            layerIndex.intValue = 2;
            layerWeight.floatValue = 1.0f;
            blendingMode.enumValueIndex = 0; // Override
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("프리셋 적용", 
                "LowerBody 프리셋이 적용되었습니다.\n\n" +
                "다음 단계: 하체 본만 활성화된 Avatar Mask를 생성하여 할당하세요.",
                "확인");
        }
    }
}
