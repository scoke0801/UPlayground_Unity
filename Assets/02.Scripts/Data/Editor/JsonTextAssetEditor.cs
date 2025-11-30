using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextAsset))]
public class JsonTextAssetEditor : Editor
{
    private TextAsset targetTextAsset;
    private bool isJsonFileType;
    private Vector2 previewScrollPosition;

    private string cachedPreviewContent;
    private const int MAX_PREVIEW_CHARACTER_COUNT = 7700;

    private void OnEnable()
    {
        targetTextAsset = target as TextAsset;
        if (targetTextAsset == null) return;

        string assetFilePath = AssetDatabase.GetAssetPath(targetTextAsset);
        isJsonFileType = !string.IsNullOrEmpty(assetFilePath) && assetFilePath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);

        if (isJsonFileType)
        {
            string fullJsonText = targetTextAsset.text;
            
            if (fullJsonText.Length > MAX_PREVIEW_CHARACTER_COUNT)
            {
                cachedPreviewContent = fullJsonText.Substring(0, MAX_PREVIEW_CHARACTER_COUNT) + 
                                       $"\n\n... (생략됨. 전체 내용은 뷰어로 확인하세요. 전체 길이: {fullJsonText.Length:N0} 자)";
            }
            else
            {
                cachedPreviewContent = fullJsonText;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        // JSON 파일이 아니면 기본 Inspector 표시
        if (!isJsonFileType)
        {
            DrawDefaultInspector();
            return;
        }

        // GUI 활성화 (TextAsset이 읽기 전용이라도 버튼은 동작해야 함)
        GUI.enabled = true;

        if (GUILayout.Button("JSON Table Viewer로 열기", GUILayout.Height(30)))
        {
            OpenJsonInTableViewer();
        }

        EditorGUILayout.Space(10);
        RenderSeparatorLine(Color.gray);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("JSON 미리보기 (Read Only)", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.MaxHeight(300));
        
        EditorGUILayout.TextArea(cachedPreviewContent, EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(5);
    }

    private void OpenJsonInTableViewer()
    {
        // JSON 뷰어 윈도우 가져오기 또는 생성
        JsonTextViewer viewerWindow = EditorWindow.GetWindow<JsonTextViewer>("JSON 테이블 뷰어");
        viewerWindow.Show();

        string assetRelativePath = AssetDatabase.GetAssetPath(targetTextAsset);
        string assetAbsolutePath = System.IO.Path.GetFullPath(assetRelativePath);

        viewerWindow.LoadJSONFromPath(assetAbsolutePath);
    }

    private void RenderSeparatorLine(Color lineColor, int lineThickness = 1, int verticalPadding = 10)
    {
        Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(verticalPadding + lineThickness));
        lineRect.height = lineThickness;
        lineRect.y += verticalPadding * 0.5f;
        lineRect.x -= 2;
        lineRect.width += 6;
        EditorGUI.DrawRect(lineRect, lineColor);
    }
}