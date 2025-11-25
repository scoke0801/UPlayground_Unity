using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using System.Linq;

public class JsonTableViewer : EditorWindow
{
    private TextAsset jsonFile;
    private Vector2 masterScrollPosition;
    private Vector2 detailScrollPosition;
    private JArray jsonArray;
    private List<string> columnNames = new List<string>();
    private Dictionary<int, bool> expandedRows = new Dictionary<int, bool>();
    private string searchFilter = "";
    
    [MenuItem("Tools/JSON Table Viewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<JsonTableViewer>("JSON Viewer");
        window.minSize = new Vector2(1000, 700);
    }
    
    void OnGUI()
    {
        DrawToolbar();
        
        if (jsonArray != null && jsonArray.Count > 0)
        {
            DrawTable();
        }
        else
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("JSON 파일을 선택하고 'Load JSON' 버튼을 클릭하세요.\n\nJSON 배열 형식 [{ ... }, { ... }]을 지원합니다.", MessageType.Info);
        }
    }
    
    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("JSON File:", GUILayout.Width(70));
        jsonFile = (TextAsset)EditorGUILayout.ObjectField(jsonFile, typeof(TextAsset), false, GUILayout.Width(250));
        
        if (GUILayout.Button("Load JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadJsonData();
        }
        
        if (jsonArray != null && jsonArray.Count > 0)
        {
            GUILayout.Label($"| {jsonArray.Count} rows", EditorStyles.miniLabel);
        }
        
        GUILayout.FlexibleSpace();
        
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
        
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void LoadJsonData()
    {
        if (jsonFile == null)
        {
            EditorUtility.DisplayDialog("오류", "JSON 파일을 선택하세요.", "확인");
            return;
        }
        
        try
        {
            string jsonText = jsonFile.text.Trim();
            
            // JSON 배열 파싱
            if (jsonText.StartsWith("["))
            {
                jsonArray = JArray.Parse(jsonText);
            }
            else if (jsonText.StartsWith("{"))
            {
                // 단일 객체면 배열로 감싸기
                jsonArray = new JArray(JObject.Parse(jsonText));
            }
            else
            {
                throw new System.Exception("JSON은 배열 [] 또는 객체 {} 형식이어야 합니다.");
            }
            
            // 컬럼 이름 자동 추출
            columnNames.Clear();
            expandedRows.Clear();
            
            if (jsonArray.Count > 0)
            {
                // 모든 행에서 키를 수집 (첫 행만이 아니라)
                HashSet<string> allKeys = new HashSet<string>();
                
                foreach (JObject obj in jsonArray.OfType<JObject>())
                {
                    foreach (var prop in obj.Properties())
                    {
                        allKeys.Add(prop.Name);
                    }
                }
                
                columnNames = allKeys.OrderBy(k => k).ToList();
            }
            
            Debug.Log($"✓ JSON 로드 완료: {jsonArray.Count}개 행, {columnNames.Count}개 컬럼");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("JSON 파싱 오류", e.Message, "확인");
            Debug.LogError($"JSON 파싱 오류: {e.Message}");
            jsonArray = null;
        }
    }
    
    void DrawTable()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"총 {jsonArray.Count}개 항목", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("모두 접기", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            expandedRows.Clear();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        masterScrollPosition = EditorGUILayout.BeginScrollView(masterScrollPosition);
        
        // 헤더 그리기
        DrawHeader();
        
        // 데이터 행 그리기
        for (int i = 0; i < jsonArray.Count; i++)
        {
            if (jsonArray[i] is JObject obj)
            {
                // 검색 필터 적용
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    string rowText = obj.ToString().ToLower();
                    if (!rowText.Contains(searchFilter.ToLower()))
                        continue;
                }
                
                DrawRow(i, obj);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        EditorGUILayout.LabelField("#", EditorStyles.boldLabel, GUILayout.Width(40));
        
        foreach (var column in columnNames)
        {
            EditorGUILayout.LabelField(column, EditorStyles.boldLabel, GUILayout.Width(150));
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    void DrawRow(int index, JObject row)
    {
        // 배경색
        Color originalColor = GUI.backgroundColor;
        if (index % 2 == 0)
        {
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 0.3f);
        }
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.BeginHorizontal();
        
        // 인덱스
        EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(40));
        
        // 각 컬럼의 값 표시
        foreach (var column in columnNames)
        {
            JToken token = row[column];
            DrawCell(token, index);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 확장된 상태면 중첩 데이터 표시
        if (expandedRows.ContainsKey(index) && expandedRows[index])
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            DrawNestedData(row, 1);
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawCell(JToken token, int rowIndex)
    {
        if (token == null)
        {
            EditorGUILayout.LabelField("-", GUILayout.Width(150));
            return;
        }
        
        switch (token.Type)
        {
            case JTokenType.Object:
                // 중첩 객체
                if (!expandedRows.ContainsKey(rowIndex))
                    expandedRows[rowIndex] = false;
                
                JObject obj = (JObject)token;
                string objLabel = $"{{ {obj.Count} props }}";
                
                if (GUILayout.Button(objLabel, GUILayout.Width(150)))
                {
                    expandedRows[rowIndex] = !expandedRows[rowIndex];
                }
                break;
                
            case JTokenType.Array:
                // 배열
                if (!expandedRows.ContainsKey(rowIndex))
                    expandedRows[rowIndex] = false;
                
                JArray arr = (JArray)token;
                string arrLabel = $"[ {arr.Count} items ]";
                
                if (GUILayout.Button(arrLabel, GUILayout.Width(150)))
                {
                    expandedRows[rowIndex] = !expandedRows[rowIndex];
                }
                break;
                
            case JTokenType.Boolean:
                bool boolValue = (bool)token;
                GUI.enabled = false;
                EditorGUILayout.Toggle(boolValue, GUILayout.Width(150));
                GUI.enabled = true;
                break;
                
            case JTokenType.Integer:
            case JTokenType.Float:
                EditorGUILayout.LabelField(token.ToString(), GUILayout.Width(150));
                break;
                
            case JTokenType.String:
                string strValue = token.ToString();
                if (strValue.Length > 20)
                    strValue = strValue.Substring(0, 17) + "...";
                
                EditorGUILayout.SelectableLabel(strValue, EditorStyles.textField, GUILayout.Width(150), GUILayout.Height(18));
                break;
                
            case JTokenType.Null:
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("null", GUILayout.Width(150));
                GUI.color = Color.white;
                break;
                
            default:
                EditorGUILayout.LabelField(token.ToString(), GUILayout.Width(150));
                break;
        }
    }
    
    void DrawNestedData(JToken token, int indentLevel)
    {
        EditorGUI.indentLevel = indentLevel;
        
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                EditorGUILayout.BeginHorizontal();
                
                if (prop.Value.Type == JTokenType.Object)
                {
                    EditorGUILayout.LabelField($"{prop.Name}:", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    DrawNestedData(prop.Value, indentLevel + 1);
                }
                else if (prop.Value.Type == JTokenType.Array)
                {
                    JArray arr = (JArray)prop.Value;
                    EditorGUILayout.LabelField($"{prop.Name}: [{arr.Count} items]", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    // 배열 내용 표시
                    for (int i = 0; i < arr.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(40));
                        
                        if (arr[i].Type == JTokenType.Object || arr[i].Type == JTokenType.Array)
                        {
                            EditorGUILayout.EndHorizontal();
                            DrawNestedData(arr[i], indentLevel + 1);
                        }
                        else
                        {
                            EditorGUILayout.SelectableLabel(arr[i].ToString(), EditorStyles.textField, GUILayout.Height(18));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"{prop.Name}:", GUILayout.Width(150));
                    EditorGUILayout.SelectableLabel(prop.Value.ToString(), EditorStyles.textField, GUILayout.Height(18));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        else if (token is JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(40));
                
                if (array[i].Type == JTokenType.Object || array[i].Type == JTokenType.Array)
                {
                    EditorGUILayout.EndHorizontal();
                    DrawNestedData(array[i], indentLevel + 1);
                }
                else
                {
                    EditorGUILayout.SelectableLabel(array[i].ToString(), EditorStyles.textField, GUILayout.Height(18));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUI.indentLevel = 0;
    }
}