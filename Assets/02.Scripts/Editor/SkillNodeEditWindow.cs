using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Game.Data;

namespace Game.Editor
{
    /// <summary>
    /// 개별 노드를 편집하는 윈도우
    /// </summary>
    public class SkillNodeEditWindow : EditorWindow
    {
        private SkillNode targetNode;
        private System.Action onChanged;
        
        private Vector2 scrollPosition;
        
        public static void ShowWindow(SkillNode node, System.Action onChanged)
        {
            var window = GetWindow<SkillNodeEditWindow>();
            window.titleContent = new GUIContent($"Edit Node {node.skillID}");
            window.targetNode = node;
            window.onChanged = onChanged;
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            if (targetNode == null)
            {
                EditorGUILayout.HelpBox("No node selected", MessageType.Warning);
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Node Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 스킬 ID 편집
            EditorGUI.BeginChangeCheck();
            int newSkillID = EditorGUILayout.IntField("Skill ID", targetNode.skillID);
            if (EditorGUI.EndChangeCheck())
            {
                targetNode.skillID = newSkillID;
                onChanged?.Invoke();
            }
            
            EditorGUILayout.Space(10);
            
            // 입력 윈도우 설정
            EditorGUILayout.LabelField("Input Window", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            float newStart = EditorGUILayout.Slider("Window Start", targetNode.inputWindowStart, 0f, 1f);
            float newEnd = EditorGUILayout.Slider("Window End", targetNode.inputWindowEnd, 0f, 1f);
            
            if (EditorGUI.EndChangeCheck())
            {
                targetNode.inputWindowStart = Mathf.Min(newStart, newEnd - 0.05f);
                targetNode.inputWindowEnd = Mathf.Max(newEnd, newStart + 0.05f);
                onChanged?.Invoke();
            }
            
            // 입력 윈도우 시각화
            EditorGUILayout.Space(5);
            Rect timelineRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            DrawInputWindowTimeline(timelineRect, targetNode.inputWindowStart, targetNode.inputWindowEnd);
            
            EditorGUILayout.Space(10);
            
            // 분기 목록
            EditorGUILayout.LabelField("Branches", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (targetNode.branches.Count == 0)
            {
                EditorGUILayout.HelpBox("No branches. Connect to other nodes to create branches.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < targetNode.branches.Count; i++)
                {
                    DrawBranchItem(i);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // 수동 분기 추가
            EditorGUILayout.LabelField("Add Branch Manually", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add X Branch"))
            {
                ShowAddBranchDialog("X");
            }
            
            if (GUILayout.Button("Add Y Branch"))
            {
                ShowAddBranchDialog("Y");
            }
            
            if (GUILayout.Button("Add A Branch"))
            {
                ShowAddBranchDialog("A");
            }
            
            if (GUILayout.Button("Add B Branch"))
            {
                ShowAddBranchDialog("B");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // 노드 위치 정보
            EditorGUILayout.LabelField("Node Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Position: ({targetNode.rect.x:F0}, {targetNode.rect.y:F0})");
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// 입력 윈도우 타임라인 그리기
        /// </summary>
        private void DrawInputWindowTimeline(Rect rect, float start, float end)
        {
            // 배경
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            
            // 입력 윈도우 영역
            float windowStart = rect.x + rect.width * start;
            float windowWidth = rect.width * (end - start);
            Rect windowRect = new Rect(windowStart, rect.y, windowWidth, rect.height);
            EditorGUI.DrawRect(windowRect, new Color(0.3f, 0.7f, 0.3f, 0.5f));
            
            // 시작/종료 선
            Handles.BeginGUI();
            Handles.color = Color.green;
            Handles.DrawLine(new Vector3(windowStart, rect.y), new Vector3(windowStart, rect.y + rect.height));
            Handles.DrawLine(new Vector3(windowStart + windowWidth, rect.y), new Vector3(windowStart + windowWidth, rect.y + rect.height));
            Handles.color = Color.white;
            Handles.EndGUI();
            
            // 라벨
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(rect.x + 5, rect.y + 5, 50, 20), "0%", labelStyle);
            GUI.Label(new Rect(rect.x + rect.width - 50, rect.y + 5, 50, 20), "100%", labelStyle);
            GUI.Label(new Rect(windowStart + 5, rect.y + 5, 100, 20), $"{start * 100:F0}%", labelStyle);
            GUI.Label(new Rect(windowStart + windowWidth - 50, rect.y + 5, 100, 20), $"{end * 100:F0}%", labelStyle);
        }
        
        /// <summary>
        /// 분기 항목 그리기
        /// </summary>
        private void DrawBranchItem(int index)
        {
            var branch = targetNode.branches[index];
            
            EditorGUILayout.BeginHorizontal("box");
            
            // 입력 키 표시 (색상 구분)
            Color keyColor = GetKeyColor(branch.inputKey);
            GUI.backgroundColor = keyColor;
            GUILayout.Label(branch.inputKey, EditorStyles.boldLabel, GUILayout.Width(30));
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.BeginVertical();
            
            // 다음 스킬 ID
            EditorGUI.BeginChangeCheck();
            int newNextSkillID = EditorGUILayout.IntField("→ Next Skill ID", branch.nextSkillID);
            if (EditorGUI.EndChangeCheck())
            {
                branch.nextSkillID = newNextSkillID;
                onChanged?.Invoke();
            }
            
            // 설명
            EditorGUI.BeginChangeCheck();
            string newDescription = EditorGUILayout.TextField("Description", branch.description);
            if (EditorGUI.EndChangeCheck())
            {
                branch.description = newDescription;
                onChanged?.Invoke();
            }
            
            EditorGUILayout.EndVertical();
            
            // 삭제 버튼
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Delete Branch", $"'{branch.inputKey}' 분기를 삭제하시겠습니까?", "Yes", "No"))
                {
                    targetNode.branches.RemoveAt(index);
                    onChanged?.Invoke();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// 키 색상 가져오기
        /// </summary>
        private Color GetKeyColor(string key)
        {
            switch (key)
            {
                case "X": return new Color(1f, 0.3f, 0.3f);
                case "Y": return new Color(1f, 1f, 0.3f);
                case "A": return new Color(0.3f, 1f, 0.3f);
                case "B": return new Color(0.3f, 0.5f, 1f);
                default: return Color.gray;
            }
        }
        
        /// <summary>
        /// 분기 추가 다이얼로그
        /// </summary>
        private void ShowAddBranchDialog(string inputKey)
        {
            // 중복 체크
            if (targetNode.branches.Exists(b => b.inputKey == inputKey))
            {
                EditorUtility.DisplayDialog("Error", $"'{inputKey}' 입력 분기가 이미 존재합니다.", "OK");
                return;
            }
            
            // 다음 스킬 ID 입력
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Enter Skill ID..."), false, () =>
            {
                var popup = ScriptableObject.CreateInstance<SkillIDInputPopup>();
                popup.Initialize(inputKey, (skillID, description) =>
                {
                    targetNode.AddBranch(inputKey, skillID, description);
                    onChanged?.Invoke();
                });
                popup.ShowUtility();
            });
            
            menu.ShowAsContext();
        }
    }
    
    /// <summary>
    /// 스킬 ID 입력 팝업
    /// </summary>
    public class SkillIDInputPopup : EditorWindow
    {
        private string inputKey;
        private int skillID = 1001;
        private string description = "";
        private System.Action<int, string> onConfirm;
        
        public void Initialize(string key, System.Action<int, string> callback)
        {
            inputKey = key;
            onConfirm = callback;
            titleContent = new GUIContent($"Add {key} Branch");
            minSize = new Vector2(300, 120);
            maxSize = new Vector2(300, 120);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField($"Adding {inputKey} Branch", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            skillID = EditorGUILayout.IntField("Next Skill ID", skillID);
            description = EditorGUILayout.TextField("Description", description);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            
            if (GUILayout.Button("Add"))
            {
                onConfirm?.Invoke(skillID, description);
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
