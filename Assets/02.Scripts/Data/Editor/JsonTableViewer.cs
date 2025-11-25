using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JSONTableViewer : EditorWindow
{
    private string jsonFilePath = "";
    private JToken jsonData;
    private List<JToken> dataRows = new List<JToken>();
    private List<JToken> filteredRows = new List<JToken>();
    private List<string> columnNames = new List<string>();
    private Dictionary<string, float> columnWidths = new Dictionary<string, float>();
    
    private int selectedIndex = -1;
    private Vector2 tableScrollPos;
    private Vector2 detailScrollPos;
    private float defaultColumnWidth = 150f;
    private float minColumnWidth = 50f;
    private float maxColumnWidth = 500f;
    
    private string searchText = "";
    private int resizingColumnIndex = -1;
    private float resizeStartX;
    private float resizeStartWidth;
    
    // 분할 뷰 조절
    private float splitRatio = 0.6f;
    private bool isResizingSplit = false;
    private const float splitterHeight = 5f;

    [MenuItem("Tools/JSON Table Viewer")]
    public static void ShowWindow()
    {
        GetWindow<JSONTableViewer>("JSON 테이블 뷰어");
    }

    private void OnGUI()
    {
        HandleDragAndDrop();
        DrawToolbar();
        
        if (jsonData == null) return;

        float splitY = position.height * splitRatio;
        
        DrawTableView(new Rect(0, 70, position.width, splitY - 70));
        DrawSplitter(new Rect(0, splitY - splitterHeight / 2, position.width, splitterHeight));
        DrawDetailView(new Rect(0, splitY + splitterHeight / 2, position.width, position.height - splitY - splitterHeight / 2));
        
        HandleColumnResize();
        HandleSplitResize();
    }

    public void LoadJSONFromPath(string path)
    {
        try
        {
            jsonFilePath = path;
            string jsonText = System.IO.File.ReadAllText(path);
            jsonData = JToken.Parse(jsonText);
            ParseJSON();
            selectedIndex = -1;
            searchText = "";
            Repaint();
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("오류", $"JSON 파일 로드 실패: {e.Message}", "확인");
        }
    }
    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string path = DragAndDrop.paths[0];
                if (path.EndsWith(".json"))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        LoadJSONFromPath(path);
                    }
                    evt.Use();
                }
            }
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("JSON 파일 열기", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            LoadJSONFile();
        }

        GUILayout.Label(string.IsNullOrEmpty(jsonFilePath) ? "파일 없음" : System.IO.Path.GetFileName(jsonFilePath), 
            EditorStyles.toolbarButton);
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        // 검색 바
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("검색:", GUILayout.Width(40));
        
        string newSearchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarTextField);
        if (newSearchText != searchText)
        {
            searchText = newSearchText;
            ApplySearch();
        }
        
        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchText = "";
            ApplySearch();
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void LoadJSONFile()
    {
        string path = EditorUtility.OpenFilePanel("JSON 파일 선택", Application.dataPath, "json");
        if (!string.IsNullOrEmpty(path))
        {
            LoadJSONFromPath(path);
        }
    }

    private void ParseJSON()
    {
        dataRows.Clear();
        columnNames.Clear();
        columnWidths.Clear();

        if (jsonData is JArray array)
        {
            dataRows = array.ToList();
        }
        else if (jsonData is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                dataRows.Add(new JObject(new JProperty("_key", prop.Name), new JProperty("_value", prop.Value)));
            }
        }

        if (dataRows.Count > 0)
        {
            ExtractColumnNames();
            InitializeColumnWidths();
        }
        
        ApplySearch();
    }

    private void ExtractColumnNames()
    {
        HashSet<string> columns = new HashSet<string>();
        
        foreach (var row in dataRows)
        {
            if (row is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    columns.Add(prop.Name);
                }
            }
        }

        columnNames = columns.OrderBy(c => c).ToList();
    }

    private void InitializeColumnWidths()
    {
        foreach (var column in columnNames)
        {
            columnWidths[column] = defaultColumnWidth;
        }
    }

    private void ApplySearch()
    {
        if (string.IsNullOrEmpty(searchText))
        {
            filteredRows = new List<JToken>(dataRows);
        }
        else
        {
            filteredRows = dataRows.Where(row => RowMatchesSearch(row, searchText)).ToList();
        }
    }

    private bool RowMatchesSearch(JToken row, string search)
    {
        string searchLower = search.ToLower();
        
        if (row is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                if (TokenContainsText(prop.Value, searchLower))
                    return true;
            }
        }
        
        return false;
    }

    private bool TokenContainsText(JToken token, string search)
    {
        if (token == null) return false;
        
        if (token is JValue value)
        {
            return value.ToString().ToLower().Contains(search);
        }
        else if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                if (TokenContainsText(prop.Value, search))
                    return true;
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
            {
                if (TokenContainsText(item, search))
                    return true;
            }
        }
        
        return false;
    }

    private void DrawTableView(Rect rect)
    {
        GUI.Box(rect, "", EditorStyles.helpBox);
        
        GUILayout.BeginArea(rect);
        tableScrollPos = EditorGUILayout.BeginScrollView(tableScrollPos);

        DrawTableHeader();

        for (int i = 0; i < filteredRows.Count; i++)
        {
            DrawDataRow(i);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.toolbarButton, GUILayout.Width(40));
        
        for (int i = 0; i < columnNames.Count; i++)
        {
            string column = columnNames[i];
            float width = columnWidths[column];
            
            GUILayout.Label(column, EditorStyles.toolbarButton, GUILayout.Width(width));
            
            // 리사이즈 핸들
            Rect labelRect = GUILayoutUtility.GetLastRect();
            Rect resizeRect = new Rect(labelRect.xMax - 2, labelRect.y, 4, labelRect.height);
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && resizeRect.Contains(Event.current.mousePosition))
            {
                resizingColumnIndex = i;
                resizeStartX = Event.current.mousePosition.x;
                resizeStartWidth = width;
                Event.current.Use();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void HandleColumnResize()
    {
        if (resizingColumnIndex >= 0)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                float delta = Event.current.mousePosition.x - resizeStartX;
                float newWidth = Mathf.Clamp(resizeStartWidth + delta, minColumnWidth, maxColumnWidth);
                columnWidths[columnNames[resizingColumnIndex]] = newWidth;
                Repaint();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                resizingColumnIndex = -1;
            }
        }
    }

    private void DrawSplitter(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
    }

    private void HandleSplitResize()
    {
        Event evt = Event.current;
        float splitY = position.height * splitRatio;
        Rect splitterRect = new Rect(0, splitY - splitterHeight / 2, position.width, splitterHeight);

        if (evt.type == EventType.MouseDown && splitterRect.Contains(evt.mousePosition))
        {
            isResizingSplit = true;
            evt.Use();
        }

        if (isResizingSplit)
        {
            if (evt.type == EventType.MouseDrag)
            {
                splitRatio = Mathf.Clamp(evt.mousePosition.y / position.height, 0.2f, 0.8f);
                Repaint();
                evt.Use();
            }
            else if (evt.type == EventType.MouseUp)
            {
                isResizingSplit = false;
                evt.Use();
            }
        }
    }

    private void DrawDataRow(int index)
    {
        var actualRow = filteredRows[index];
        int actualIndex = dataRows.IndexOf(actualRow);
        
        Color originalColor = GUI.backgroundColor;
        if (actualIndex == selectedIndex)
        {
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // [변경 1] 행 번호를 클릭했을 때 해당 행이 선택되도록 변경 (데이터 셀이 텍스트 필드가 되었기 때문)
        if (GUILayout.Button(index.ToString(), EditorStyles.toolbarButton, GUILayout.Width(40)))
        {
            selectedIndex = actualIndex;
            // 텍스트 필드에서 포커스를 빼서, 상세 뷰가 즉시 갱신되도록 함
            GUI.FocusControl(null);
        }

        var row = actualRow as JObject;
        if (row != null)
        {
            foreach (var column in columnNames)
            {
                string value = GetCellValue(row, column);
                float width = columnWidths[column];
                
                // [변경 2] 기존 Button을 TextField로 변경하여 텍스트 드래그 및 복사(Ctrl+C) 가능하게 함
                // 'EditorStyles.textField' 스타일을 적용해 입력창 형태를 갖춤
                string newValue = EditorGUILayout.TextField(value, EditorStyles.textField, GUILayout.Width(width));
                
                // (선택 사항) 만약 여기서 값을 수정해서 JSON에 반영하고 싶다면 
                // newValue를 사용하여 row[column] 값을 업데이트하는 로직을 추가할 수 있습니다.
                // 현재는 뷰어 목적이므로 값만 표시합니다.
            }
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = originalColor;
    }

    private string GetCellValue(JObject row, string columnName)
    {
        if (!row.ContainsKey(columnName)) return "";

        var token = row[columnName];
        return FormatTokenValue(token);
    }

    private string FormatTokenValue(JToken token)
    {
        if (token == null) return "null";

        if (token is JValue value)
        {
            return value.ToString();
        }
        else if (token is JArray array)
        {
            var items = new List<string>();
            
            foreach (var item in array)
            {
                if (item is JValue jval)
                {
                    items.Add(jval.ToString());
                }
                else if (item is JObject jobj)
                {
                    string objStr = FormatObjectFull(jobj);
                    items.Add(objStr);
                }
                else if (item is JArray jarray)
                {
                    items.Add(FormatTokenValue(jarray));
                }
            }
            
            return $"[{string.Join(", ", items)}]";
        }
        else if (token is JObject obj)
        {
            return FormatObjectFull(obj);
        }
        
        return token.ToString();
    }

    private string FormatObjectBrief(JObject obj)
    {
        var props = obj.Properties().ToList();
        
        if (props.Count == 0) return "{}";
        
        var primaryKeys = new[] { "id", "key", "name", "type", "skill_id", "item_id" };
        var primaryProp = props.FirstOrDefault(p => primaryKeys.Contains(p.Name.ToLower()));
        
        if (primaryProp != null)
        {
            string mainValue = primaryProp.Value is JValue jv ? jv.ToString() : "...";
            
            if (props.Count > 1)
            {
                var otherProps = props.Where(p => p.Name != primaryProp.Name).Take(2);
                var extras = string.Join(", ", otherProps.Select(p => 
                {
                    string val = p.Value is JValue jv ? jv.ToString() : "...";
                    return $"{p.Name}:{val}";
                }));
                
                return $"{mainValue} ({extras})";
            }
            
            return mainValue;
        }
        
        var pairs = props.Take(2).Select(p =>
        {
            string val = p.Value is JValue jv ? jv.ToString() : "...";
            return $"{p.Name}:{val}";
        });
        
        string result = string.Join(", ", pairs);
        if (props.Count > 2) result += "...";
        
        return $"{{{result}}}";
    }

    private string FormatObjectFull(JObject obj)
    {
        var props = obj.Properties().ToList();
        
        if (props.Count == 0) return "{}";
        
        var pairs = props.Select(p =>
        {
            string val;
            if (p.Value is JValue jv)
            {
                val = jv.ToString();
            }
            else if (p.Value is JArray jarr)
            {
                val = $"[{jarr.Count}개]";
            }
            else if (p.Value is JObject jobj)
            {
                val = "{...}";
            }
            else
            {
                val = "...";
            }
            return $"{p.Name}:{val}";
        });
        
        return $"{{{string.Join(", ", pairs)}}}";
    }

    private void DrawDetailView(Rect rect)
    {
        GUI.Box(rect, "", EditorStyles.helpBox);
        
        GUILayout.BeginArea(rect);
        GUILayout.Label("상세 정보", EditorStyles.boldLabel);
        
        if (selectedIndex >= 0 && selectedIndex < dataRows.Count)
        {
            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);
            DrawDetailProperties(dataRows[selectedIndex], 0);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("항목을 선택하세요", EditorStyles.centeredGreyMiniLabel);
        }
        
        GUILayout.EndArea();
    }

    private void DrawDetailProperties(JToken token, int indent)
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                DrawProperty(prop.Name, prop.Value, indent);
            }
        }
        else if (token is JArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                DrawProperty($"[{i}]", array[i], indent);
            }
        }
    }

    private void DrawProperty(string name, JToken value, int indent)
    {
        EditorGUILayout.BeginHorizontal();
        // 들여쓰기 여백 처리
        GUILayout.Space(indent * 20);

        // 라벨 너비 계산 (들여쓰기가 너무 깊어져서 너비가 깨지는 것을 방지하기 위해 최소값 설정)
        float labelWidth = Mathf.Max(50, 200 - indent * 20);

        if (value is JObject || value is JArray)
        {
            // [변경] 복합 데이터(객체/배열)의 키 이름
            // LabelField 대신 TextField를 사용하여 키 이름도 복사 가능하게 변경
            // (기존의 EditorStyles.boldLabel은 TextField에서 텍스트 선택이 어려울 수 있어 기본 스타일 사용)
            EditorGUILayout.TextField(name, GUILayout.Width(labelWidth));
            
            EditorGUILayout.EndHorizontal();
            
            // 재귀 호출로 내부 데이터 표시
            DrawDetailProperties(value, indent + 1);
        }
        else
        {
            // [변경] 단일 데이터의 키(Key)
            EditorGUILayout.TextField(name, GUILayout.Width(labelWidth));
            
            // [변경] 단일 데이터의 값(Value)
            // 값을 TextField로 그려서 마우스 드래그 및 Ctrl+C 가능하게 처리
            string displayValue = value?.ToString() ?? "null";
            EditorGUILayout.TextField(displayValue);
            
            EditorGUILayout.EndHorizontal();
        }
    }
}