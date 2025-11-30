using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class JsonTextViewer : EditorWindow
{
    // JSON 파일 경로
    private string jsonFilePath = "";
    
    // 테이블 데이터
    private Dictionary<string, JArray> tableDataByName = new Dictionary<string, JArray>();
    private string[] availableTableNames = new string[0];
    private int currentTableIndex = 0;
    
    // 현재 선택된 테이블의 데이터
    private JArray currentTableData;
    private List<JToken> filteredTableRows = new List<JToken>();
    private List<string> tableColumnNames = new List<string>();
    
    // 스크롤 위치
    private Vector2 tableScrollPosition;
    private Vector2 detailScrollPosition; 
    
    // UI 상태
    private Dictionary<string, float> columnWidthByName = new Dictionary<string, float>();
    private Dictionary<string, bool> foldoutStateByKey = new Dictionary<string, bool>();
    private string searchKeyword = "";
    
    private int selectedRowIndex = -1; 

    // 행 높이 캐시
    private Dictionary<int, float> rowHeightByIndex = new Dictionary<int, float>();
    private const float DEFAULT_ROW_HEIGHT = 22f;

    // 컬럼 리사이징
    private int resizingColumnIndex = -1;
    private float resizeStartMouseX;
    private float resizeStartColumnWidth;
    
    // 분할 창 리사이징
    private float verticalSplitRatio = 0.6f;
    private bool isResizingVerticalSplit = false;
    private const float SPLITTER_HEIGHT = 5f;
    private const float TOOLBAR_HEIGHT = 25f;

    [MenuItem("Tools/JSON Table Viewer")]
    public static void ShowWindow()
    {
        GetWindow<JsonTextViewer>("JSON 테이블 뷰어");
    }

    protected void OnGUI()
    {
        ProcessDragAndDropEvent();
        RenderToolbar();
        
        if (tableDataByName.Count == 0 && string.IsNullOrEmpty(jsonFilePath))
        {
            EditorGUILayout.HelpBox("JSON 파일을 드래그하거나 열어주세요.", MessageType.Info);
            return;
        }

        float availableHeight = position.height - 50; 
        float tableSectionHeight = availableHeight * verticalSplitRatio;
        
        Rect tableAreaRect = new Rect(0, 50, position.width, tableSectionHeight);
        Rect splitterRect = new Rect(0, 50 + tableSectionHeight, position.width, SPLITTER_HEIGHT);
        Rect detailAreaRect = new Rect(0, 50 + tableSectionHeight + SPLITTER_HEIGHT, position.width, availableHeight - tableSectionHeight - SPLITTER_HEIGHT);

        RenderTableSection(tableAreaRect);
        RenderVerticalSplitter(splitterRect);
        RenderDetailSection(detailAreaRect);
        
        ProcessColumnResizeEvent();
        ProcessVerticalSplitResizeEvent(splitterRect);
    }

    public void LoadJSONFromPath(string filePath)
    {
        try
        {
            jsonFilePath = filePath;
            string jsonContent = System.IO.File.ReadAllText(filePath);
            var parsedToken = JToken.Parse(jsonContent);
            
            tableDataByName.Clear();
            currentTableData = null;
            selectedRowIndex = -1;
            searchKeyword = "";

            if (parsedToken is JArray arrayData)
            {
                tableDataByName.Add("Root Array", arrayData);
            }
            else if (parsedToken is JObject objectData)
            {
                foreach (var property in objectData.Properties())
                {
                    if (property.Value.Type == JTokenType.Array)
                    {
                        tableDataByName.Add(property.Name, (JArray)property.Value);
                    }
                }
                
                if (tableDataByName.Count == 0)
                {
                    tableDataByName.Add("Single Object", new JArray { objectData });
                }
            }

            if (tableDataByName.Count > 0)
            {
                availableTableNames = tableDataByName.Keys.ToArray();
                
                currentTableIndex = 0;
                for (int i = 0; i < availableTableNames.Length; i++)
                {
                    string lowerCaseName = availableTableNames[i].ToLower();
                    if (lowerCaseName.Contains("base") || (lowerCaseName.Contains("data") && !lowerCaseName.Contains("stack")))
                    {
                        currentTableIndex = i;
                        break;
                    }
                }
                
                SelectTableByName(availableTableNames[currentTableIndex]);
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("오류", $"JSON 로드 실패: {ex.Message}", "확인");
        }
    }

    private void SelectTableByName(string tableName)
    {
        if (tableDataByName.ContainsKey(tableName))
        {
            currentTableData = tableDataByName[tableName];
            foldoutStateByKey.Clear();
            rowHeightByIndex.Clear();
            selectedRowIndex = -1;
            
            ExtractColumnNamesFromCurrentTable();
            CalculateOptimalColumnWidths();
            ApplySearchFilter();
        }
    }

    private void ExtractColumnNamesFromCurrentTable()
    {
        tableColumnNames.Clear();
        if (currentTableData == null) return;

        HashSet<string> uniqueColumnNames = new HashSet<string>();
        
        foreach (var row in currentTableData)
        {
            if (row is JObject rowObject)
            {
                foreach (var property in rowObject.Properties())
                {
                    if (uniqueColumnNames.Add(property.Name))
                    {
                        tableColumnNames.Add(property.Name);
                    }
                }
            }
        }
    }

    private void CalculateOptimalColumnWidths()
    {
        columnWidthByName.Clear();
        foreach (var columnName in tableColumnNames)
        {
            float optimalWidth = 120f;
            float headerTextWidth = GUI.skin.label.CalcSize(new GUIContent(columnName)).x + 30;
            optimalWidth = Mathf.Max(optimalWidth, headerTextWidth);

            if (currentTableData != null)
            {
                int sampleRowCount = Mathf.Min(20, currentTableData.Count);
                for (int i = 0; i < sampleRowCount; i++)
                {
                    var rowObject = currentTableData[i] as JObject;
                    if (rowObject != null && rowObject[columnName] != null)
                    {
                        var cellValue = rowObject[columnName];
                        if (cellValue.Type == JTokenType.Array || cellValue.Type == JTokenType.Object) 
                        {
                            optimalWidth = Mathf.Max(optimalWidth, 300f);
                        }
                        else if (cellValue.Type == JTokenType.String)
                        {
                            float contentWidth = GUI.skin.textField.CalcSize(new GUIContent(cellValue.ToString())).x + 20;
                            optimalWidth = Mathf.Max(optimalWidth, contentWidth);
                        }
                    }
                }
            }
            columnWidthByName[columnName] = Mathf.Min(optimalWidth, 500f);
        }
    }

    private void RenderTableSection(Rect areaRect)
    {
        GUILayout.BeginArea(areaRect);
        
        if (currentTableData != null)
        {
            EditorGUILayout.LabelField($"Rows: {filteredTableRows.Count} / {currentTableData.Count}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("No Data", EditorStyles.boldLabel);
        }

        tableScrollPosition = EditorGUILayout.BeginScrollView(tableScrollPosition);
        
        if (tableColumnNames.Count > 0)
        {
            RenderTableHeader();
            RenderTableRowsWithOptimization(areaRect);
        }
        else if (currentTableData != null && currentTableData.Count == 0)
        {
            EditorGUILayout.HelpBox("이 테이블은 비어있습니다. (Empty Array)", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void RenderTableHeader()
    {
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(40));
        
        for (int columnIndex = 0; columnIndex < tableColumnNames.Count; columnIndex++)
        {
            string columnName = tableColumnNames[columnIndex];
            float columnWidth = columnWidthByName.ContainsKey(columnName) ? columnWidthByName[columnName] : 100f;
            
            GUILayout.Label(columnName, EditorStyles.boldLabel, GUILayout.Width(columnWidth));
            
            Rect headerRect = GUILayoutUtility.GetLastRect();
            Rect resizeHandleRect = new Rect(headerRect.xMax - 3, headerRect.y, 6, headerRect.height);
            EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
            {
                resizingColumnIndex = columnIndex;
                resizeStartMouseX = Event.current.mousePosition.x;
                resizeStartColumnWidth = columnWidth;
                Event.current.Use();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void RenderTableRowsWithOptimization(Rect areaRect)
    {
        // 가시성 렌더링 최적화
        if (filteredTableRows.Count > 0)
        {
            float currentRowY = 0f;
            float viewportHeight = areaRect.height;
            float scrollOffsetY = tableScrollPosition.y;
            
            float topSpaceHeight = 0f;
            float bottomSpaceHeight = 0f;

            for (int rowIndex = 0; rowIndex < filteredTableRows.Count; rowIndex++)
            {
                float rowHeight = rowHeightByIndex.ContainsKey(rowIndex) ? rowHeightByIndex[rowIndex] : DEFAULT_ROW_HEIGHT;
                bool isRowVisible = (currentRowY + rowHeight >= scrollOffsetY - rowHeight) && (currentRowY <= scrollOffsetY + viewportHeight + rowHeight);

                if (isRowVisible)
                {
                    if (topSpaceHeight > 0)
                    {
                        GUILayout.Space(topSpaceHeight);
                        topSpaceHeight = 0;
                    }

                    Rect rowRect = EditorGUILayout.BeginVertical();
                    RenderSingleTableRow(rowIndex, filteredTableRows[rowIndex] as JObject);
                    EditorGUILayout.EndVertical();

                    if (Event.current.type == EventType.Repaint)
                    {
                        float actualRowHeight = rowRect.height;
                        if (actualRowHeight > 1) rowHeightByIndex[rowIndex] = actualRowHeight;
                    }
                }
                else
                {
                    if (currentRowY < scrollOffsetY) topSpaceHeight += rowHeight;
                    else bottomSpaceHeight += rowHeight;
                }

                currentRowY += rowHeight;
            }

            if (bottomSpaceHeight > 0) GUILayout.Space(bottomSpaceHeight);
        }
        else
        {
            EditorGUILayout.HelpBox("표시할 데이터가 없습니다.", MessageType.Info);
        }
    }

    private void RenderSingleTableRow(int rowIndex, JObject rowObject)
    {
        if (rowObject == null) return;

        Color originalBackgroundColor = GUI.backgroundColor;
        if (rowIndex == selectedRowIndex)
        {
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1.0f);
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox); 
        
        if (GUILayout.Button(rowIndex.ToString(), EditorStyles.miniButton, GUILayout.Width(40)))
        {
            selectedRowIndex = rowIndex;
            GUI.FocusControl(null);
            Repaint();
        }

        foreach (string columnName in tableColumnNames)
        {
            float columnWidth = columnWidthByName[columnName];
            JToken cellValue = rowObject[columnName];

            if (cellValue != null)
            {
                if (cellValue.Type == JTokenType.Array)
                {
                    RenderNestedArray(columnName, (JArray)cellValue, $"{rowIndex}_{columnName}", columnWidth);
                }
                else if (cellValue.Type == JTokenType.Object)
                {
                    RenderNestedObject(columnName, (JObject)cellValue, $"{rowIndex}_{columnName}", columnWidth);
                }
                else
                {
                    string cellText = cellValue.ToString(Formatting.None);
                    EditorGUILayout.TextField(cellText, GUILayout.Width(columnWidth));
                }
            }
            else
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(columnWidth));
            }
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = originalBackgroundColor;
    }

    private void RenderNestedArray(string columnTitle, JArray arrayValue, string foldoutKey, float columnWidth)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));
        
        if (!foldoutStateByKey.ContainsKey(foldoutKey)) 
            foldoutStateByKey[foldoutKey] = false;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        string arrayCaption = $"{columnTitle} [{arrayValue.Count}]";
        foldoutStateByKey[foldoutKey] = EditorGUILayout.Foldout(foldoutStateByKey[foldoutKey], arrayCaption, true);
        EditorGUILayout.EndHorizontal();

        if (foldoutStateByKey[foldoutKey])
        {
            EditorGUI.indentLevel++;
            int maxDisplayCount = Mathf.Min(arrayValue.Count, 50); 
            
            for (int i = 0; i < maxDisplayCount; i++)
            {
                var arrayItem = arrayValue[i];
                if (arrayItem is JObject innerObject)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"[{i}]", EditorStyles.miniBoldLabel);
                    foreach (var property in innerObject.Properties())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(property.Name, EditorStyles.miniLabel, GUILayout.Width(columnWidth * 0.4f));
                        EditorGUILayout.TextField(property.Value.ToString(Formatting.None), EditorStyles.miniTextField);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", EditorStyles.miniLabel, GUILayout.Width(30));
                    EditorGUILayout.TextField(arrayItem.ToString(Formatting.None), EditorStyles.miniTextField);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            if (arrayValue.Count > maxDisplayCount)
            {
                EditorGUILayout.LabelField($"... {arrayValue.Count - maxDisplayCount} more items", EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(5);
        }
        EditorGUILayout.EndVertical();
    }

    private void RenderNestedObject(string columnTitle, JObject objectValue, string foldoutKey, float columnWidth)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(columnWidth));
        
        if (!foldoutStateByKey.ContainsKey(foldoutKey)) 
            foldoutStateByKey[foldoutKey] = false;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        string objectCaption = $"{columnTitle} {{...}}";
        foldoutStateByKey[foldoutKey] = EditorGUILayout.Foldout(foldoutStateByKey[foldoutKey], objectCaption, true);
        EditorGUILayout.EndHorizontal();

        if (foldoutStateByKey[foldoutKey])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            foreach (var property in objectValue.Properties())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(property.Name, EditorStyles.miniLabel, GUILayout.Width(columnWidth * 0.4f));
                
                string propertyValueText = property.Value.ToString(Formatting.None);
                EditorGUILayout.TextField(propertyValueText, EditorStyles.miniTextField);
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            GUILayout.Space(5);
        }
        EditorGUILayout.EndVertical();
    }

    // ==================================================================================
    // 하단 상세 뷰
    // ==================================================================================
    private void RenderDetailSection(Rect areaRect)
    {
        GUI.Box(areaRect, "", EditorStyles.helpBox);
        GUILayout.BeginArea(areaRect);
        
        EditorGUILayout.LabelField("상세 정보", EditorStyles.boldLabel);
        
        if (selectedRowIndex >= 0 && selectedRowIndex < filteredTableRows.Count)
        {
            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
            RenderDetailProperties(filteredTableRows[selectedRowIndex], indentLevel: 0);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("항목을 선택하면 상세 정보가 여기에 표시됩니다.", EditorStyles.centeredGreyMiniLabel);
        }
        
        GUILayout.EndArea();
    }

    private void RenderDetailProperties(JToken token, int indentLevel)
    {
        if (token is JObject objectToken)
        {
            foreach (var property in objectToken.Properties())
            {
                RenderDetailProperty(property.Name, property.Value, indentLevel);
            }
        }
        else if (token is JArray arrayToken)
        {
            for (int i = 0; i < arrayToken.Count; i++)
            {
                RenderDetailProperty($"[{i}]", arrayToken[i], indentLevel);
            }
        }
    }

    private void RenderDetailProperty(string propertyName, JToken propertyValue, int indentLevel)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indentLevel * 20);

        float labelWidth = Mathf.Max(100, 200 - indentLevel * 10);
        
        if (propertyValue is JObject || propertyValue is JArray)
        {
            EditorGUILayout.LabelField(propertyName, EditorStyles.boldLabel, GUILayout.Width(labelWidth));
            EditorGUILayout.EndHorizontal();
            RenderDetailProperties(propertyValue, indentLevel + 1);
        }
        else
        {
            EditorGUILayout.LabelField(propertyName, GUILayout.Width(labelWidth));
            EditorGUILayout.TextField(propertyValue?.ToString() ?? "null");
            EditorGUILayout.EndHorizontal();
        }
    }

    // ==================================================================================
    // 툴바 및 유틸리티
    // ==================================================================================
    private void RenderToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
        
        if (GUILayout.Button("JSON 파일 열기", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFilePanel("JSON 파일 선택", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(selectedPath)) 
                LoadJSONFromPath(selectedPath);
        }
        
        GUILayout.Label(System.IO.Path.GetFileName(jsonFilePath), EditorStyles.toolbarButton, GUILayout.Width(150));
        
        if (availableTableNames.Length > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Table:", GUILayout.Width(40));
            int newTableIndex = EditorGUILayout.Popup(currentTableIndex, availableTableNames, EditorStyles.toolbarPopup, GUILayout.Width(200));
            if (newTableIndex != currentTableIndex)
            {
                currentTableIndex = newTableIndex;
                SelectTableByName(availableTableNames[currentTableIndex]);
            }
        }

        GUILayout.FlexibleSpace();
        
        GUILayout.Label("검색:", GUILayout.Width(35));
        string newSearchKeyword = EditorGUILayout.TextField(searchKeyword, EditorStyles.toolbarTextField, GUILayout.Width(200));
        if (newSearchKeyword != searchKeyword)
        {
            searchKeyword = newSearchKeyword;
            ApplySearchFilter();
        }
        
        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchKeyword = "";
            ApplySearchFilter();
            GUI.FocusControl(null);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void RenderVerticalSplitter(Rect splitterRect)
    {
        EditorGUI.DrawRect(splitterRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);
    }

    private void ProcessVerticalSplitResizeEvent(Rect splitterRect)
    {
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && splitterRect.Contains(currentEvent.mousePosition))
        {
            isResizingVerticalSplit = true;
            currentEvent.Use();
        }

        if (isResizingVerticalSplit)
        {
            if (currentEvent.type == EventType.MouseDrag)
            {
                float relativeMouseY = currentEvent.mousePosition.y - 50;
                verticalSplitRatio = Mathf.Clamp(relativeMouseY / (position.height - 50), 0.2f, 0.8f);
                Repaint();
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                isResizingVerticalSplit = false;
                currentEvent.Use();
            }
        }
    }

    private void ApplySearchFilter()
    {
        if (currentTableData == null) return;
        
        if (string.IsNullOrEmpty(searchKeyword))
        {
            filteredTableRows = currentTableData.ToList();
        }
        else
        {
            string lowerCaseKeyword = searchKeyword.ToLower();
            filteredTableRows = currentTableData.Where(row => row.ToString().ToLower().Contains(lowerCaseKeyword)).ToList();
        }
        selectedRowIndex = -1;
        rowHeightByIndex.Clear(); 
    }

    private void ProcessColumnResizeEvent()
    {
        if (resizingColumnIndex >= 0)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                float mouseDeltaX = Event.current.mousePosition.x - resizeStartMouseX;
                string columnName = tableColumnNames[resizingColumnIndex];
                float newColumnWidth = Mathf.Clamp(resizeStartColumnWidth + mouseDeltaX, 50f, 800f);
                columnWidthByName[columnName] = newColumnWidth;
                Repaint();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                resizingColumnIndex = -1;
            }
        }
    }

    private void ProcessDragAndDropEvent()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                string draggedFilePath = DragAndDrop.paths[0];
                if (draggedFilePath.EndsWith(".json"))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (currentEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        LoadJSONFromPath(draggedFilePath);
                    }
                    currentEvent.Use();
                }
            }
        }
    }
}