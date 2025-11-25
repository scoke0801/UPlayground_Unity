using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextAsset))]
public class JsonTextAssetEditor : Editor
{
    private TextAsset textAsset;
    private bool isJsonFile;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        textAsset = target as TextAsset;
        
        // JSON 파일인지 확인
        string assetPath = AssetDatabase.GetAssetPath(textAsset);
        isJsonFile = !string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".json");
    }

    public override void OnInspectorGUI()
    {
        // JSON 파일이 아니면 기본 Inspector 표시
        if (!isJsonFile)
        {
            DrawDefaultInspector();
            return;
        }

        // 기존 JSON 미리보기 기능 유지
        EditorGUILayout.LabelField("JSON 파일", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // JSON 텍스트 미리보기
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(300));
        EditorGUILayout.TextArea(textAsset.text, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();

        // 구분선
        EditorGUILayout.Space(10);
        DrawUILine(Color.gray);
        EditorGUILayout.Space(5);

        // JSON Table Viewer 버튼
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
        GUI.enabled = true;
        if (GUILayout.Button("JSON Table Viewer로 열기", GUILayout.Height(30)))
        {
            OpenInTableViewer();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
    }

    private void OpenInTableViewer()
    {
        // JSON Table Viewer 창 열기
        JSONTableViewer window = EditorWindow.GetWindow<JSONTableViewer>("JSON 테이블 뷰어");
        window.Show();
        
        // 현재 선택된 JSON 파일 경로를 가져와서 자동으로 로드
        string assetPath = AssetDatabase.GetAssetPath(textAsset);
        string fullPath = System.IO.Path.GetFullPath(assetPath);
        
        // public LoadJSON 메서드를 통해 파일 로드
        window.LoadJSONFromPath(fullPath);
    }

    private void DrawUILine(Color color, int thickness = 1, int padding = 10)
    {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        rect.height = thickness;
        rect.y += padding / 2;
        rect.x -= 2;
        rect.width += 6;
        EditorGUI.DrawRect(rect, color);
    }
}