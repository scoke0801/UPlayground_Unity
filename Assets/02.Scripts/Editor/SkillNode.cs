using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Game.Data;

namespace Game.Editor
{
    /// <summary>
    /// 스킬 체인 에디터의 개별 노드
    /// </summary>
    [System.Serializable]
    public class SkillNode
    {
        public int skillID;
        public Rect rect;
        public List<SkillChainBranch> branches = new List<SkillChainBranch>();
        public float inputWindowStart = 0.5f;
        public float inputWindowEnd = 0.9f;
        
        private bool isDragged;
        private Rect inputPointRect;
        private Rect outputPointRect;
        private const float pointSize = 15f;
        
        public SkillNode(int id, Vector2 position)
        {
            skillID = id;
            rect = new Rect(position.x, position.y, 150, 80);
        }
        
        /// <summary>
        /// 노드 그리기
        /// </summary>
        public void Draw(GUIStyle normalStyle, GUIStyle selectedStyle, GUIStyle inStyle, GUIStyle outStyle, bool isSelected)
        {
            // 입력/출력 포인트 위치 계산
            inputPointRect = new Rect(rect.x - pointSize / 2, rect.y + rect.height / 2 - pointSize / 2, pointSize, pointSize);
            outputPointRect = new Rect(rect.x + rect.width - pointSize / 2, rect.y + rect.height / 2 - pointSize / 2, pointSize, pointSize);
            
            // 입력 포인트 그리기
            GUI.Box(inputPointRect, "", inStyle);
            
            // 출력 포인트 그리기
            GUI.Box(outputPointRect, "", outStyle);
            
            // 노드 박스 그리기
            GUIStyle style = isSelected ? selectedStyle : normalStyle;
            GUI.Box(rect, "", style);
            
            // 노드 내용 그리기
            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical();
            
            GUILayout.Label($"ID: {skillID}", EditorStyles.boldLabel);
            GUILayout.Label($"Branches: {branches.Count}");
            GUILayout.Label($"Window: {inputWindowStart:F2}-{inputWindowEnd:F2}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 이벤트 처리
        /// </summary>
        public bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            isDragged = true;
                            GUI.changed = true;
                            return true;
                        }
                    }
                    break;
                    
                case EventType.MouseUp:
                    isDragged = false;
                    break;
                    
                case EventType.MouseDrag:
                    if (e.button == 0 && isDragged)
                    {
                        Drag(e.delta);
                        e.Use();
                        return true;
                    }
                    break;
            }
            
            return false;
        }
        
        /// <summary>
        /// 노드 드래그
        /// </summary>
        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }
        
        /// <summary>
        /// 입력 포인트 클릭 체크
        /// </summary>
        public bool IsInputPointClicked(Vector2 mousePosition)
        {
            return inputPointRect.Contains(mousePosition);
        }
        
        /// <summary>
        /// 출력 포인트 클릭 체크
        /// </summary>
        public bool IsOutputPointClicked(Vector2 mousePosition)
        {
            return outputPointRect.Contains(mousePosition);
        }
        
        /// <summary>
        /// 입력 포인트 위치 가져오기
        /// </summary>
        public Vector2 GetInputPoint()
        {
            return new Vector2(rect.x, rect.y + rect.height / 2);
        }
        
        /// <summary>
        /// 출력 포인트 위치 가져오기
        /// </summary>
        public Vector2 GetOutputPoint()
        {
            return new Vector2(rect.x + rect.width, rect.y + rect.height / 2);
        }
        
        /// <summary>
        /// 분기 추가
        /// </summary>
        public void AddBranch(string inputKey, int nextSkillID, string description)
        {
            // 중복 체크
            if (branches.Exists(b => b.inputKey == inputKey))
            {
                EditorUtility.DisplayDialog("Error", $"'{inputKey}' 입력 분기가 이미 존재합니다.", "OK");
                return;
            }
            
            branches.Add(new SkillChainBranch
            {
                inputKey = inputKey,
                nextSkillID = nextSkillID,
                description = description
            });
        }
        
        /// <summary>
        /// 분기 제거
        /// </summary>
        public void RemoveBranch(string inputKey)
        {
            branches.RemoveAll(b => b.inputKey == inputKey);
        }
    }
}
