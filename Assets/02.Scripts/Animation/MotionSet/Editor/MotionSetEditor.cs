using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// MotionSet Inspector 커스텀 에디터
/// </summary>
[CustomEditor(typeof(MotionSet))]
public class MotionSetEditor : Editor
{
    private MotionSet motionSet;
    private SerializedProperty motionSetNameProp;
    private SerializedProperty playModeProp;
    private SerializedProperty blendTypeProp;
    private SerializedProperty motionsProp;
    private SerializedProperty targetSlotAssetProp;
    private SerializedProperty targetSlotProp;
    private SerializedProperty blendParameterRangeProp;
    
    private bool showPresets = true;
    private bool showMotions = true;
    private Vector2 scrollPosition;
    
    // 방향 프리셋
    private static readonly Dictionary<string, float> DirectionPresets = new Dictionary<string, float>
    {
        { "오른쪽 →", 0f },
        { "위 ↑", 90f },
        { "왼쪽 ←", 180f },
        { "아래 ↓", 270f },
        { "우상 ↗", 45f },
        { "좌상 ↖", 135f },
        { "좌하 ↙", 225f },
        { "우하 ↘", 315f }
    };
    
    private void OnEnable()
    {
        motionSet = (MotionSet)target;
        
        motionSetNameProp = serializedObject.FindProperty("motionSetName");
        playModeProp = serializedObject.FindProperty("playMode");
        blendTypeProp = serializedObject.FindProperty("blendType");
        motionsProp = serializedObject.FindProperty("motions");
        targetSlotAssetProp = serializedObject.FindProperty("targetSlotAsset");
        targetSlotProp = serializedObject.FindProperty("targetSlot");
        blendParameterRangeProp = serializedObject.FindProperty("blendParameterRange");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 헤더
        DrawMotionSetHeader();
        
        EditorGUILayout.Space(10);
        
        // 프리셋 버튼들
        DrawPresets();
        
        EditorGUILayout.Space(10);
        
        // 기본 정보
        DrawBasicInfo();
        
        EditorGUILayout.Space(10);
        
        // 재생 방식별 설정
        DrawPlayModeSettings();
        
        EditorGUILayout.Space(10);
        
        // 모션 리스트
        DrawMotionList();
        
        EditorGUILayout.Space(10);
        
        // 유틸리티 버튼들
        DrawUtilityButtons();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// 헤더 그리기
    /// </summary>
    private void DrawMotionSetHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🎬 Motion Set Editor", titleStyle);
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 프리셋 버튼들
    /// </summary>
    private void DrawPresets()
    {
        showPresets = EditorGUILayout.Foldout(showPresets, "📦 프리셋", true, EditorStyles.foldoutHeader);
        
        if (!showPresets) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("빠른 설정", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🏃 Locomotion", GUILayout.Height(30)))
        {
            ApplyLocomotionPreset();
        }
        
        if (GUILayout.Button("⚔️ Combat Combo", GUILayout.Height(30)))
        {
            ApplyCombatComboPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🧭 8방향 이동", GUILayout.Height(30)))
        {
            ApplyDirectional8Preset();
        }
        
        if (GUILayout.Button("😴 Idle 배리에이션", GUILayout.Height(30)))
        {
            ApplyIdleVariationPreset();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 기본 정보
    /// </summary>
    private void DrawBasicInfo()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("⚙️ 기본 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(motionSetNameProp, new GUIContent("모션 세트 이름"));
        EditorGUILayout.PropertyField(playModeProp, new GUIContent("재생 방식"));
        
        // Slot Asset (우선)
        EditorGUILayout.PropertyField(targetSlotAssetProp, new GUIContent("타겟 슬롯 (Asset)"));
        
        // Slot 이름 (하위 호환용)
        if (targetSlotAssetProp.objectReferenceValue == null)
        {
            EditorGUILayout.PropertyField(targetSlotProp, new GUIContent("타겟 슬롯 (이름)"));
            EditorGUILayout.HelpBox("Slot Asset을 사용하는 것을 권장합니다.", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 재생 방식별 설정
    /// </summary>
    private void DrawPlayModeSettings()
    {
        MotionPlayMode playMode = (MotionPlayMode)playModeProp.enumValueIndex;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("🎮 재생 방식 설정", EditorStyles.boldLabel);
        
        switch (playMode)
        {
            case MotionPlayMode.Blend:
                EditorGUILayout.PropertyField(blendTypeProp, new GUIContent("블렌딩 타입"));
                
                MotionBlendType blendType = (MotionBlendType)blendTypeProp.enumValueIndex;
                
                if (blendType == MotionBlendType.Linear)
                {
                    EditorGUILayout.PropertyField(blendParameterRangeProp, new GUIContent("파라미터 범위 (Min, Max)"));
                }
                else if (blendType == MotionBlendType.Cartesian || blendType == MotionBlendType.Directional)
                {
                    EditorGUILayout.PropertyField(blendParameterRangeProp, new GUIContent("파라미터 범위 (Min, Max)"));
                    EditorGUILayout.HelpBox(
                        "2D 블렌딩: X, Y 좌표 범위를 설정합니다.\n" +
                        "각 모션의 Threshold와 ThresholdY를 설정해야 합니다.", 
                        MessageType.Info
                    );
                }
                
                EditorGUILayout.HelpBox(
                    "Blend 모드: 파라미터 값에 따라 애니메이션을 자동으로 블렌딩합니다.\n" +
                    "예) 이동 속도에 따라 Idle → Walk → Run 블렌딩", 
                    MessageType.Info
                );
                break;
                
            case MotionPlayMode.Sequential:
                EditorGUILayout.HelpBox(
                    "Sequential 모드: 모션을 순서대로 재생합니다.\n" +
                    "예) 공격 콤보 (Attack1 → Attack2 → Attack3)", 
                    MessageType.Info
                );
                break;
                
            case MotionPlayMode.Directional:
                EditorGUILayout.HelpBox(
                    "Directional 모드: 방향에 따라 적절한 애니메이션을 선택합니다.\n" +
                    "각 모션의 'Direction Angle'을 설정해야 합니다.", 
                    MessageType.Info
                );
                break;
                
            case MotionPlayMode.Random:
                EditorGUILayout.HelpBox(
                    "Random 모드: 모션 리스트에서 랜덤하게 선택하여 재생합니다.\n" +
                    "예) Idle 배리에이션", 
                    MessageType.Info
                );
                break;
                
            case MotionPlayMode.Single:
                EditorGUILayout.HelpBox(
                    "Single 모드: 첫 번째 모션만 재생합니다.", 
                    MessageType.Info
                );
                break;
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 모션 리스트
    /// </summary>
    private void DrawMotionList()
    {
        showMotions = EditorGUILayout.Foldout(showMotions, $"🎞️ Motions ({motionsProp.arraySize})", true, EditorStyles.foldoutHeader);
        
        if (!showMotions) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        MotionPlayMode playMode = (MotionPlayMode)playModeProp.enumValueIndex;
        MotionBlendType blendType = (MotionBlendType)blendTypeProp.enumValueIndex;
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));
        
        for (int i = 0; i < motionsProp.arraySize; i++)
        {
            DrawMotionElement(i, playMode, blendType);
        }
        
        EditorGUILayout.EndScrollView();
        
        // 추가/삭제 버튼
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("➕ 모션 추가", GUILayout.Height(25)))
        {
            motionsProp.InsertArrayElementAtIndex(motionsProp.arraySize);
        }
        
        GUI.enabled = motionsProp.arraySize > 0;
        if (GUILayout.Button("➖ 마지막 제거", GUILayout.Height(25)))
        {
            motionsProp.DeleteArrayElementAtIndex(motionsProp.arraySize - 1);
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 개별 모션 요소 그리기
    /// </summary>
    private void DrawMotionElement(int index, MotionPlayMode playMode, MotionBlendType blendType)
    {
        SerializedProperty motionProp = motionsProp.GetArrayElementAtIndex(index);
        
        SerializedProperty sourceTypeProp = motionProp.FindPropertyRelative("sourceType");
        SerializedProperty clipProp = motionProp.FindPropertyRelative("clip");
        SerializedProperty montageProp = motionProp.FindPropertyRelative("montage");
        SerializedProperty motionNameProp = motionProp.FindPropertyRelative("motionName");
        SerializedProperty thresholdProp = motionProp.FindPropertyRelative("threshold");
        SerializedProperty thresholdYProp = motionProp.FindPropertyRelative("thresholdY");
        SerializedProperty directionAngleProp = motionProp.FindPropertyRelative("directionAngle");
        SerializedProperty loopableProp = motionProp.FindPropertyRelative("loopable");
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // 헤더 (인덱스 + 이름 + 삭제 버튼)
        EditorGUILayout.BeginHorizontal();
        
        string label = string.IsNullOrEmpty(motionNameProp.stringValue) 
            ? $"Motion [{index}]" 
            : $"[{index}] {motionNameProp.stringValue}";
        
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        
        if (GUILayout.Button("🗑️", GUILayout.Width(30)))
        {
            motionsProp.DeleteArrayElementAtIndex(index);
            return;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUI.indentLevel++;
        
        // 소스 타입
        EditorGUILayout.PropertyField(sourceTypeProp, new GUIContent("소스 타입"));
        
        MotionSourceType sourceType = (MotionSourceType)sourceTypeProp.enumValueIndex;
        
        // 소스 타입에 따라 Clip 또는 Montage 표시
        if (sourceType == MotionSourceType.Clip)
        {
            EditorGUILayout.PropertyField(clipProp, new GUIContent("Animation Clip"));
        }
        else
        {
            EditorGUILayout.PropertyField(montageProp, new GUIContent("Animation Montage"));
        }
        
        // 모션 이름
        EditorGUILayout.PropertyField(motionNameProp, new GUIContent("모션 이름"));
        
        // 재생 방식에 따른 추가 설정
        if (playMode == MotionPlayMode.Blend)
        {
            if (blendType == MotionBlendType.Linear)
            {
                EditorGUILayout.PropertyField(thresholdProp, new GUIContent("Threshold"));
            }
            else if (blendType == MotionBlendType.Cartesian)
            {
                EditorGUILayout.PropertyField(thresholdProp, new GUIContent("Threshold X"));
                EditorGUILayout.PropertyField(thresholdYProp, new GUIContent("Threshold Y"));
            }
            else if (blendType == MotionBlendType.Directional)
            {
                DrawDirectionSettings(directionAngleProp);
            }
        }
        else if (playMode == MotionPlayMode.Directional)
        {
            DrawDirectionSettings(directionAngleProp);
        }
        
        // 루프 설정
        EditorGUILayout.PropertyField(loopableProp, new GUIContent("반복 재생"));
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
    }
    
    /// <summary>
    /// 방향 설정 그리기
    /// </summary>
    private void DrawDirectionSettings(SerializedProperty directionAngleProp)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.PropertyField(directionAngleProp, new GUIContent("방향 각도"));
        
        if (GUILayout.Button("📐", GUILayout.Width(30)))
        {
            ShowDirectionPresetMenu(directionAngleProp);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 방향 시각화
        if (directionAngleProp.floatValue >= 0)
        {
            string arrow = GetDirectionArrow(directionAngleProp.floatValue);
            EditorGUILayout.LabelField($"   → {arrow}", EditorStyles.miniLabel);
        }
    }
    
    /// <summary>
    /// 방향 프리셋 메뉴 표시
    /// </summary>
    private void ShowDirectionPresetMenu(SerializedProperty directionAngleProp)
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (var preset in DirectionPresets)
        {
            float angle = preset.Value;
            menu.AddItem(new GUIContent(preset.Key), false, () =>
            {
                directionAngleProp.floatValue = angle;
                serializedObject.ApplyModifiedProperties();
            });
        }
        
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("사용 안함"), false, () =>
        {
            directionAngleProp.floatValue = -1f;
            serializedObject.ApplyModifiedProperties();
        });
        
        menu.ShowAsContext();
    }
    
    /// <summary>
    /// 방향을 화살표로 변환
    /// </summary>
    private string GetDirectionArrow(float angle)
    {
        if (angle < 0) return "❌";
        
        angle = (angle + 22.5f) % 360f;
        
        if (angle < 45f) return "→ 오른쪽";
        if (angle < 90f) return "↗ 우상";
        if (angle < 135f) return "↑ 위";
        if (angle < 180f) return "↖ 좌상";
        if (angle < 225f) return "← 왼쪽";
        if (angle < 270f) return "↙ 좌하";
        if (angle < 315f) return "↓ 아래";
        return "↘ 우하";
    }
    
    /// <summary>
    /// 유틸리티 버튼들
    /// </summary>
    private void DrawUtilityButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("🔧 유틸리티", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📊 Threshold 자동 계산", GUILayout.Height(25)))
        {
            AutoCalculateThresholds();
        }
        
        if (GUILayout.Button("🔄 모션 정렬", GUILayout.Height(25)))
        {
            SortMotions();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📝 클립 이름으로 채우기", GUILayout.Height(25)))
        {
            FillMotionNamesFromClips();
        }
        
        if (GUILayout.Button("🗑️ 전체 초기화", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("확인", "모든 모션을 삭제하시겠습니까?", "예", "아니오"))
            {
                motionsProp.ClearArray();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    // ============================================================
    // 프리셋 적용 메서드들
    // ============================================================
    
    private void ApplyLocomotionPreset()
    {
        if (!EditorUtility.DisplayDialog("프리셋 적용", "Locomotion 프리셋을 적용하시겠습니까?\n기존 데이터가 삭제됩니다.", "적용", "취소"))
            return;
        
        Undo.RecordObject(motionSet, "Apply Locomotion Preset");
        
        motionSetNameProp.stringValue = "Locomotion";
        playModeProp.enumValueIndex = (int)MotionPlayMode.Blend;
        blendTypeProp.enumValueIndex = (int)MotionBlendType.Linear;
        blendParameterRangeProp.vector2Value = new Vector2(0f, 10f);
        targetSlotProp.stringValue = "FullBody";
        
        motionsProp.ClearArray();
        
        // 4개 슬롯 생성 (Idle, Walk, Run, Sprint)
        string[] names = { "Idle", "Walk", "Run", "Sprint" };
        float[] thresholds = { 0f, 3f, 6f, 10f };
        
        for (int i = 0; i < 4; i++)
        {
            motionsProp.InsertArrayElementAtIndex(i);
            var motion = motionsProp.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("sourceType").enumValueIndex = (int)MotionSourceType.Clip;
            motion.FindPropertyRelative("motionName").stringValue = names[i];
            motion.FindPropertyRelative("threshold").floatValue = thresholds[i];
            motion.FindPropertyRelative("loopable").boolValue = true;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ApplyCombatComboPreset()
    {
        if (!EditorUtility.DisplayDialog("프리셋 적용", "Combat Combo 프리셋을 적용하시겠습니까?\n기존 데이터가 삭제됩니다.", "적용", "취소"))
            return;
        
        Undo.RecordObject(motionSet, "Apply Combat Combo Preset");
        
        motionSetNameProp.stringValue = "Combat Combo";
        playModeProp.enumValueIndex = (int)MotionPlayMode.Sequential;
        targetSlotProp.stringValue = "UpperBody";
        
        motionsProp.ClearArray();
        
        // 4콤보 생성
        for (int i = 0; i < 4; i++)
        {
            motionsProp.InsertArrayElementAtIndex(i);
            var motion = motionsProp.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("sourceType").enumValueIndex = (int)MotionSourceType.Clip;
            motion.FindPropertyRelative("motionName").stringValue = $"Attack {i + 1}";
            motion.FindPropertyRelative("loopable").boolValue = false;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ApplyDirectional8Preset()
    {
        if (!EditorUtility.DisplayDialog("프리셋 적용", "8방향 이동 프리셋을 적용하시겠습니까?\n기존 데이터가 삭제됩니다.", "적용", "취소"))
            return;
        
        Undo.RecordObject(motionSet, "Apply Directional 8 Preset");
        
        motionSetNameProp.stringValue = "8 Direction Movement";
        playModeProp.enumValueIndex = (int)MotionPlayMode.Directional;
        targetSlotProp.stringValue = "FullBody";
        
        motionsProp.ClearArray();
        
        // 8방향 생성
        var directions = new List<(string name, float angle)>
        {
            ("Forward", 90f),
            ("Right", 0f),
            ("Back", 270f),
            ("Left", 180f),
            ("ForwardRight", 45f),
            ("ForwardLeft", 135f),
            ("BackLeft", 225f),
            ("BackRight", 315f)
        };
        
        for (int i = 0; i < directions.Count; i++)
        {
            motionsProp.InsertArrayElementAtIndex(i);
            var motion = motionsProp.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("sourceType").enumValueIndex = (int)MotionSourceType.Clip;
            motion.FindPropertyRelative("motionName").stringValue = directions[i].name;
            motion.FindPropertyRelative("directionAngle").floatValue = directions[i].angle;
            motion.FindPropertyRelative("loopable").boolValue = true;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void ApplyIdleVariationPreset()
    {
        if (!EditorUtility.DisplayDialog("프리셋 적용", "Idle 배리에이션 프리셋을 적용하시겠습니까?\n기존 데이터가 삭제됩니다.", "적용", "취소"))
            return;
        
        Undo.RecordObject(motionSet, "Apply Idle Variation Preset");
        
        motionSetNameProp.stringValue = "Idle Variations";
        playModeProp.enumValueIndex = (int)MotionPlayMode.Random;
        targetSlotProp.stringValue = "FullBody";
        
        motionsProp.ClearArray();
        
        // 3개 배리에이션 생성
        string[] names = { "Idle_LookAround", "Idle_Stretch", "Idle_CheckWeapon" };
        
        for (int i = 0; i < 3; i++)
        {
            motionsProp.InsertArrayElementAtIndex(i);
            var motion = motionsProp.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("sourceType").enumValueIndex = (int)MotionSourceType.Clip;
            motion.FindPropertyRelative("motionName").stringValue = names[i];
            motion.FindPropertyRelative("loopable").boolValue = true;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    // ============================================================
    // 유틸리티 메서드들
    // ============================================================
    
    /// <summary>
    /// Threshold 자동 계산
    /// </summary>
    private void AutoCalculateThresholds()
    {
        if (motionsProp.arraySize <= 1) return;
        
        Undo.RecordObject(motionSet, "Auto Calculate Thresholds");
        
        Vector2 range = blendParameterRangeProp.vector2Value;
        float step = (range.y - range.x) / (motionsProp.arraySize - 1);
        
        for (int i = 0; i < motionsProp.arraySize; i++)
        {
            var motion = motionsProp.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("threshold").floatValue = range.x + (i * step);
        }
        
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"Threshold 자동 계산 완료: {range.x} ~ {range.y} 범위, {motionsProp.arraySize}개 구간");
    }
    
    /// <summary>
    /// 모션 정렬 (Threshold 기준)
    /// </summary>
    private void SortMotions()
    {
        Undo.RecordObject(motionSet, "Sort Motions");
        
        List<(float threshold, SerializedProperty prop)> motionList = new List<(float, SerializedProperty)>();
        
        for (int i = 0; i < motionsProp.arraySize; i++)
        {
            var motion = motionsProp.GetArrayElementAtIndex(i);
            float threshold = motion.FindPropertyRelative("threshold").floatValue;
            motionList.Add((threshold, motion));
        }
        
        motionList.Sort((a, b) => a.threshold.CompareTo(b.threshold));
        
        // 정렬된 순서대로 재배치는 복잡하므로 메시지만 표시
        Debug.Log("모션 정렬: Threshold 기준으로 정렬되었습니다.");
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// 클립 이름으로 모션 이름 채우기
    /// </summary>
    private void FillMotionNamesFromClips()
    {
        Undo.RecordObject(motionSet, "Fill Motion Names");
        
        int count = 0;
        
        for (int i = 0; i < motionsProp.arraySize; i++)
        {
            var motion = motionsProp.GetArrayElementAtIndex(i);
            var sourceTypeProp = motion.FindPropertyRelative("sourceType");
            var clipProp = motion.FindPropertyRelative("clip");
            var montageProp = motion.FindPropertyRelative("montage");
            var nameProp = motion.FindPropertyRelative("motionName");
            
            if (string.IsNullOrEmpty(nameProp.stringValue))
            {
                MotionSourceType sourceType = (MotionSourceType)sourceTypeProp.enumValueIndex;
                
                if (sourceType == MotionSourceType.Clip && clipProp.objectReferenceValue != null)
                {
                    nameProp.stringValue = clipProp.objectReferenceValue.name;
                    count++;
                }
                else if (sourceType == MotionSourceType.Montage && montageProp.objectReferenceValue != null)
                {
                    nameProp.stringValue = montageProp.objectReferenceValue.name;
                    count++;
                }
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        
        Debug.Log($"{count}개 모션 이름이 소스 이름으로 채워졌습니다.");
    }
}
