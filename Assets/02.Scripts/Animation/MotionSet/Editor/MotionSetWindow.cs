using UnityEngine;
using UnityEditor;

/// <summary>
/// MotionSet 전용 에디터 윈도우
/// Window → Animation → Motion Set Editor 메뉴로 열기
/// </summary>
public class MotionSetWindow : EditorWindow
{
    private MotionSet selectedMotionSet;
    private Editor motionSetEditor;
    private Vector2 scrollPosition;
    
    /// <summary>
    /// 메뉴에서 윈도우 열기
    /// </summary>
    [MenuItem("Tools/Animation/Motion Set Editor")]
    public static void OpenWindow()
    {
        MotionSetWindow window = GetWindow<MotionSetWindow>("Motion Set Editor");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        DrawHeader();
        
        EditorGUILayout.Space(10);
        
        DrawMotionSetSelector();
        
        if (selectedMotionSet != null)
        {
            EditorGUILayout.Space(10);
            DrawMotionSetEditor();
        }
    }
    
    /// <summary>
    /// 헤더
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🎬 Motion Set Editor", titleStyle);
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// MotionSet 선택기
    /// </summary>
    private void DrawMotionSetSelector()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("📁 Motion Set 선택", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        selectedMotionSet = (MotionSet)EditorGUILayout.ObjectField(
            "편집할 MotionSet",
            selectedMotionSet,
            typeof(MotionSet),
            false
        );
        
        if (EditorGUI.EndChangeCheck())
        {
            // MotionSet이 변경되면 에디터 재생성
            if (selectedMotionSet != null)
            {
                motionSetEditor = Editor.CreateEditor(selectedMotionSet);
            }
            else
            {
                DestroyImmediate(motionSetEditor);
                motionSetEditor = null;
            }
        }
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("➕ 새 MotionSet 생성", GUILayout.Height(25)))
        {
            CreateNewMotionSet();
        }
        
        GUI.enabled = selectedMotionSet != null;
        if (GUILayout.Button("📍 프로젝트에서 선택", GUILayout.Height(25)))
        {
            Selection.activeObject = selectedMotionSet;
            EditorGUIUtility.PingObject(selectedMotionSet);
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// MotionSet 에디터 표시
    /// </summary>
    private void DrawMotionSetEditor()
    {
        if (motionSetEditor == null) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 커스텀 에디터 그리기
        motionSetEditor.OnInspectorGUI();
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 새 MotionSet 생성
    /// </summary>
    private void CreateNewMotionSet()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "새 MotionSet 생성",
            "NewMotionSet",
            "asset",
            "MotionSet을 저장할 위치를 선택하세요."
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        MotionSet newMotionSet = CreateInstance<MotionSet>();
        
        // SerializedObject를 통해 private 필드에 접근
        SerializedObject serializedMotionSet = new SerializedObject(newMotionSet);
        SerializedProperty nameProperty = serializedMotionSet.FindProperty("motionSetName");
        if (nameProperty != null)
        {
            nameProperty.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
            serializedMotionSet.ApplyModifiedProperties();
        }
        
        AssetDatabase.CreateAsset(newMotionSet, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        selectedMotionSet = newMotionSet;
        motionSetEditor = Editor.CreateEditor(selectedMotionSet);
        
        Selection.activeObject = newMotionSet;
        EditorGUIUtility.PingObject(newMotionSet);
        
        Debug.Log($"새 MotionSet이 생성되었습니다: {path}");
    }
    
    private void OnDestroy()
    {
        if (motionSetEditor != null)
        {
            DestroyImmediate(motionSetEditor);
        }
    }
}