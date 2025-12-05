using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Game.Data;

namespace Game.Editor
{
    /// <summary>
    /// 스킬 체인 트리를 시각적으로 편집하는 에디터 윈도우
    /// </summary>
    public class SkillChainEditorWindow : EditorWindow
    {
        // 노드 관리
        private List<SkillNode> nodes = new List<SkillNode>();
        private SkillNode selectedNode = null;
        private SkillNode connectingFromNode = null;
        
        // 뷰포트 관리
        private Vector2 scrollPosition;
        private Vector2 drag;
        private float zoom = 1.0f;
        
        // UI 스타일
        private GUIStyle nodeStyle;
        private GUIStyle selectedNodeStyle;
        private GUIStyle inPointStyle;
        private GUIStyle outPointStyle;
        
        // 파일 경로
        private string jsonFilePath = "Assets/10.Datas/Json/SkillChainData.json";
        
        [MenuItem("Tools/Skill Chain Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillChainEditorWindow>();
            window.titleContent = new GUIContent("Skill Chain Editor");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnEnable()
        {
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            // 일반 노드 스타일
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f));
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
            nodeStyle.padding = new RectOffset(10, 10, 10, 10);
            nodeStyle.normal.textColor = Color.white;
            nodeStyle.alignment = TextAnchor.MiddleCenter;
            nodeStyle.fontSize = 12;
            
            // 선택된 노드 스타일
            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.4f, 0.6f, 0.8f, 1f));
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
            selectedNodeStyle.padding = new RectOffset(10, 10, 10, 10);
            selectedNodeStyle.normal.textColor = Color.white;
            selectedNodeStyle.alignment = TextAnchor.MiddleCenter;
            selectedNodeStyle.fontSize = 12;
            
            // 입력 포인트 스타일
            inPointStyle = new GUIStyle();
            inPointStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.8f, 0.2f, 1f));
            inPointStyle.border = new RectOffset(4, 4, 4, 4);
            
            // 출력 포인트 스타일
            outPointStyle = new GUIStyle();
            outPointStyle.normal.background = MakeTexture(2, 2, new Color(0.8f, 0.2f, 0.2f, 1f));
            outPointStyle.border = new RectOffset(4, 4, 4, 4);
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawGrid(20, 0.2f, Color.gray);
            DrawGrid(100, 0.4f, Color.gray);
            
            DrawConnections();
            DrawNodes();
            DrawConnectionLine();
            
            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);
            
            if (GUI.changed) Repaint();
        }
        
        /// <summary>
        /// 상단 툴바 그리기
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                AddNode(new Vector2(100, 100));
            }
            
            if (GUILayout.Button("Load JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                LoadFromJson();
            }
            
            if (GUILayout.Button("Save JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                SaveToJson();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "모든 노드를 삭제하시겠습니까?", "Yes", "No"))
                {
                    nodes.Clear();
                    selectedNode = null;
                    connectingFromNode = null;
                }
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label($"Nodes: {nodes.Count}", EditorStyles.toolbarButton);
            
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 그리드 배경 그리기
        /// </summary>
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);
            
            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            
            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(gridSpacing * i, 0, 0),
                    new Vector3(gridSpacing * i, position.height, 0)
                );
            }
            
            for (int i = 0; i < heightDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(0, gridSpacing * i, 0),
                    new Vector3(position.width, gridSpacing * i, 0)
                );
            }
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 노드들 그리기
        /// </summary>
        private void DrawNodes()
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Draw(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, selectedNode == nodes[i]);
                }
            }
        }
        
        /// <summary>
        /// 연결선 그리기
        /// </summary>
        private void DrawConnections()
        {
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    foreach (var branch in node.branches)
                    {
                        var targetNode = nodes.FirstOrDefault(n => n.skillID == branch.nextSkillID);
                        if (targetNode != null)
                        {
                            DrawConnection(node.GetOutputPoint(), targetNode.GetInputPoint(), branch.inputKey);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 연결선 그리기 (베지어 곡선)
        /// </summary>
        private void DrawConnection(Vector2 start, Vector2 end, string label)
        {
            Handles.BeginGUI();
            
            Color color = label == "X" ? Color.red : Color.yellow;
            Handles.color = color;
            
            Vector3 startPos = new Vector3(start.x, start.y, 0);
            Vector3 endPos = new Vector3(end.x, end.y, 0);
            Vector3 startTangent = startPos + Vector3.right * 50;
            Vector3 endTangent = endPos + Vector3.left * 50;
            
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, color, null, 3f);
            
            // 라벨 그리기
            Vector3 midPoint = (startPos + endPos) / 2;
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = color;
            labelStyle.fontStyle = FontStyle.Bold;
            Handles.Label(midPoint, $"[{label}]", labelStyle);
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 연결 중인 선 그리기
        /// </summary>
        private void DrawConnectionLine()
        {
            if (connectingFromNode != null)
            {
                Handles.BeginGUI();
                Handles.color = Color.green;
                
                Vector3 startPos = new Vector3(connectingFromNode.GetOutputPoint().x, connectingFromNode.GetOutputPoint().y, 0);
                Vector3 endPos = Event.current.mousePosition;
                Vector3 startTangent = startPos + Vector3.right * 50;
                Vector3 endTangent = endPos + Vector3.left * 50;
                
                Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.green, null, 2f);
                
                Handles.color = Color.white;
                Handles.EndGUI();
                
                Repaint();
            }
        }
        
        /// <summary>
        /// 노드 이벤트 처리
        /// </summary>
        private void ProcessNodeEvents(Event e)
        {
            if (nodes != null)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    bool guiChanged = nodes[i].ProcessEvents(e);
                    
                    if (guiChanged)
                    {
                        GUI.changed = true;
                    }
                }
            }
        }
        
        /// <summary>
        /// 전역 이벤트 처리
        /// </summary>
        private void ProcessEvents(Event e)
        {
            drag = Vector2.zero;
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        // 노드 선택
                        selectedNode = null;
                        foreach (var node in nodes)
                        {
                            if (node.rect.Contains(e.mousePosition))
                            {
                                selectedNode = node;
                                GUI.changed = true;
                                break;
                            }
                        }
                        
                        // 출력 포인트 클릭 체크
                        if (selectedNode != null && selectedNode.IsOutputPointClicked(e.mousePosition))
                        {
                            connectingFromNode = selectedNode;
                            e.Use();
                        }
                        
                        // 입력 포인트 클릭 체크 (연결 완료)
                        if (connectingFromNode != null)
                        {
                            foreach (var node in nodes)
                            {
                                if (node != connectingFromNode && node.IsInputPointClicked(e.mousePosition))
                                {
                                    ShowAddBranchDialog(connectingFromNode, node);
                                    connectingFromNode = null;
                                    e.Use();
                                    break;
                                }
                            }
                        }
                    }
                    else if (e.button == 1) // 우클릭
                    {
                        ProcessContextMenu(e.mousePosition);
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (e.button == 0 && connectingFromNode != null)
                    {
                        connectingFromNode = null;
                        e.Use();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 컨텍스트 메뉴 처리
        /// </summary>
        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();
            
            // 노드 위에서 우클릭
            SkillNode clickedNode = nodes.FirstOrDefault(n => n.rect.Contains(mousePosition));
            if (clickedNode != null)
            {
                menu.AddItem(new GUIContent("Edit Node"), false, () => ShowNodeEditDialog(clickedNode));
                menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(clickedNode));
            }
            else
            {
                // 빈 공간에서 우클릭
                menu.AddItem(new GUIContent("Add Node"), false, () => AddNode(mousePosition));
            }
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// 새 노드 추가
        /// </summary>
        private void AddNode(Vector2 position)
        {
            int newID = nodes.Count > 0 ? nodes.Max(n => n.skillID) + 1 : 1001;
            var node = new SkillNode(newID, position);
            nodes.Add(node);
        }
        
        /// <summary>
        /// 노드 삭제
        /// </summary>
        private void DeleteNode(SkillNode node)
        {
            if (EditorUtility.DisplayDialog("Delete Node", $"노드 {node.skillID}을(를) 삭제하시겠습니까?", "Yes", "No"))
            {
                // 이 노드로 연결된 다른 노드의 분기 제거
                foreach (var n in nodes)
                {
                    n.branches.RemoveAll(b => b.nextSkillID == node.skillID);
                }
                
                nodes.Remove(node);
                
                if (selectedNode == node)
                    selectedNode = null;
            }
        }
        
        /// <summary>
        /// 노드 편집 다이얼로그
        /// </summary>
        private void ShowNodeEditDialog(SkillNode node)
        {
            SkillNodeEditWindow.ShowWindow(node, () => Repaint());
        }
        
        /// <summary>
        /// 분기 추가 다이얼로그
        /// </summary>
        private void ShowAddBranchDialog(SkillNode fromNode, SkillNode toNode)
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Add X Branch"), false, () => 
            {
                fromNode.AddBranch("X", toNode.skillID, "X Input Branch");
            });
            
            menu.AddItem(new GUIContent("Add Y Branch"), false, () => 
            {
                fromNode.AddBranch("Y", toNode.skillID, "Y Input Branch");
            });
            
            menu.AddItem(new GUIContent("Add A Branch"), false, () => 
            {
                fromNode.AddBranch("A", toNode.skillID, "A Input Branch");
            });
            
            menu.AddItem(new GUIContent("Add B Branch"), false, () => 
            {
                fromNode.AddBranch("B", toNode.skillID, "B Input Branch");
            });
            
            menu.ShowAsContext();
        }
        
        /// <summary>
        /// Json 파일 저장
        /// </summary>
        private void SaveToJson()
        {
            string path = EditorUtility.SaveFilePanel("Save Skill Chain Data", "Assets/10.Datas/Json", "SkillChainData", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                var chainDataList = new List<SkillChainData>();
                
                foreach (var node in nodes)
                {
                    var chainData = new SkillChainData
                    {
                        currentSkillID = node.skillID,
                        branches = node.branches.ToArray(),
                        inputWindowStart = node.inputWindowStart,
                        inputWindowEnd = node.inputWindowEnd
                    };
                    chainDataList.Add(chainData);
                }
                
                var wrapper = new SkillChainDataWrapper
                {
                    chains = chainDataList.ToArray()
                };
                
                string json = JsonUtility.ToJson(wrapper, true);
                System.IO.File.WriteAllText(path, json);
                
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Save Complete", $"Saved to: {path}", "OK");
            }
        }
        
        /// <summary>
        /// Json 파일 로드
        /// </summary>
        private void LoadFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Load Skill Chain Data", "Assets/10.Datas/Json", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<SkillChainDataWrapper>(json);
                
                if (wrapper != null && wrapper.chains != null)
                {
                    nodes.Clear();
                    
                    // 노드 생성 (첫 번째 패스)
                    for (int i = 0; i < wrapper.chains.Length; i++)
                    {
                        var data = wrapper.chains[i];
                        Vector2 position = new Vector2(100 + (i % 5) * 200, 100 + (i / 5) * 150);
                        
                        var node = new SkillNode(data.currentSkillID, position)
                        {
                            inputWindowStart = data.inputWindowStart,
                            inputWindowEnd = data.inputWindowEnd
                        };
                        
                        if (data.branches != null)
                        {
                            node.branches = new List<SkillChainBranch>(data.branches);
                        }
                        
                        nodes.Add(node);
                    }
                    
                    EditorUtility.DisplayDialog("Load Complete", $"Loaded {nodes.Count} nodes", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Failed", "Failed to parse JSON", "OK");
                }
            }
        }
    }
}
