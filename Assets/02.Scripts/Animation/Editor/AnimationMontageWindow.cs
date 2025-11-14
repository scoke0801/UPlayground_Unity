using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Animation.Editor
{
    /// <summary>
    /// 애니메이션 몽타쥬 편집을 위한 전용 에디터 윈도우
    /// 타임라인 형식으로 섹션과 노티파이를 시각화하고 편집
    /// </summary>
    public class AnimationMontageWindow : EditorWindow
    {
        // 현재 편집 중인 몽타쥬
        private AnimationMontage currentMontage;
        private SerializedObject serializedMontage;
        
        // 윈도우 레이아웃
        private Vector2 scrollPosition;
        private float timelineHeight = 100f;
        private float sectionHeight = 60f;
        private float pixelsPerSecond = 50f;
        
        // 선택된 섹션
        private int selectedSectionIndex = -1;
        
        // 색상 테마
        private readonly Color sectionColor = new Color(0.3f, 0.6f, 0.9f);
        private readonly Color selectedColor = new Color(0.9f, 0.6f, 0.3f);
        private readonly Color notifyColor = new Color(0.9f, 0.3f, 0.3f);
        
        [MenuItem("Window/Animation/Montage Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<AnimationMontageWindow>("Montage Editor");
            window.minSize = new Vector2(800, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Selection 변경 감지
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }
        
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }
        
        private void OnSelectionChanged()
        {
            // 선택된 오브젝트가 AnimationMontage인지 확인
            if (Selection.activeObject is AnimationMontage montage)
            {
                currentMontage = montage;
                serializedMontage = new SerializedObject(currentMontage);
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            if (currentMontage == null)
            {
                DrawEmptyState();
                return;
            }
            
            serializedMontage.Update();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawMontageInfo();
            EditorGUILayout.Space(10);
            
            DrawTimeline();
            EditorGUILayout.Space(10);
            
            DrawSectionDetails();
            
            EditorGUILayout.EndScrollView();
            
            serializedMontage.ApplyModifiedProperties();
        }
        
        #region Toolbar
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 몽타쥬 선택
            EditorGUI.BeginChangeCheck();
            var newMontage = (AnimationMontage)EditorGUILayout.ObjectField(
                currentMontage, typeof(AnimationMontage), false, GUILayout.Width(200));
            
            if (EditorGUI.EndChangeCheck() && newMontage != null)
            {
                currentMontage = newMontage;
                serializedMontage = new SerializedObject(currentMontage);
                selectedSectionIndex = -1;
            }
            
            GUILayout.FlexibleSpace();
            
            // 줌 컨트롤
            GUILayout.Label("Zoom:", GUILayout.Width(50));
            pixelsPerSecond = EditorGUILayout.Slider(pixelsPerSecond, 20f, 200f, GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        #region Empty State
        private void DrawEmptyState()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label("몽타쥬 에디터", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            GUILayout.Label("편집할 Animation Montage를 선택하거나\n" +
                          "Project 뷰에서 선택하세요.");
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("새 몽타쥬 생성", GUILayout.Height(30)))
            {
                CreateNewMontage();
            }
            
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        
        private void CreateNewMontage()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "새 몽타쥬 저장",
                "New Montage",
                "asset",
                "몽타쥬 에셋을 저장할 위치를 선택하세요.");
            
            if (!string.IsNullOrEmpty(path))
            {
                var montage = CreateInstance<AnimationMontage>();
                AssetDatabase.CreateAsset(montage, path);
                AssetDatabase.SaveAssets();
                
                currentMontage = montage;
                serializedMontage = new SerializedObject(currentMontage);
                Selection.activeObject = montage;
            }
        }
        #endregion
        
        #region Montage Info
        private void DrawMontageInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("몽타쥬 정보", EditorStyles.boldLabel);
            
            var montageNameProp = serializedMontage.FindProperty("montageName");
            var slotNameProp = serializedMontage.FindProperty("slotName");
            
            EditorGUILayout.PropertyField(montageNameProp, new GUIContent("몽타쥬 이름"));
            EditorGUILayout.PropertyField(slotNameProp, new GUIContent("슬롯 이름"));
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("섹션 추가", GUILayout.Width(100), GUILayout.Height(25)))
            {
                AddNewSection();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void AddNewSection()
        {
            var sectionsProp = serializedMontage.FindProperty("sections");
            sectionsProp.InsertArrayElementAtIndex(sectionsProp.arraySize);
            
            var newSection = sectionsProp.GetArrayElementAtIndex(sectionsProp.arraySize - 1);
            newSection.FindPropertyRelative("sectionName").stringValue = $"Section{sectionsProp.arraySize}";
            newSection.FindPropertyRelative("fadeInDuration").floatValue = 0.25f;
            newSection.FindPropertyRelative("fadeOutDuration").floatValue = 0.25f;
            newSection.FindPropertyRelative("playRate").floatValue = 1.0f;
            newSection.FindPropertyRelative("loop").boolValue = false;
            newSection.FindPropertyRelative("nextSection").stringValue = "";
            
            serializedMontage.ApplyModifiedProperties();
            selectedSectionIndex = sectionsProp.arraySize - 1;
        }
        #endregion
        
        #region Timeline
        private void DrawTimeline()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("타임라인", EditorStyles.boldLabel);
            
            var sectionsProp = serializedMontage.FindProperty("sections");
            
            if (sectionsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("섹션이 없습니다. '섹션 추가' 버튼을 클릭하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }
            
            // 타임라인 배경
            var timelineRect = GUILayoutUtility.GetRect(
                position.width - 20,
                timelineHeight * sectionsProp.arraySize,
                GUILayout.ExpandWidth(true));
            
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f));
            
            // 각 섹션 그리기
            float currentY = timelineRect.y;
            float totalTime = 0f;
            
            for (int i = 0; i < sectionsProp.arraySize; i++)
            {
                var section = sectionsProp.GetArrayElementAtIndex(i);
                var clip = section.FindPropertyRelative("clip").objectReferenceValue as AnimationClip;
                
                if (clip != null)
                {
                    var sectionRect = DrawSectionBar(section, i, currentY, totalTime);
                    
                    // 섹션 클릭 감지
                    if (Event.current.type == EventType.MouseDown && sectionRect.Contains(Event.current.mousePosition))
                    {
                        selectedSectionIndex = i;
                        Event.current.Use();
                        Repaint();
                    }
                    
                    totalTime += clip.length;
                }
                
                currentY += timelineHeight;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private Rect DrawSectionBar(SerializedProperty section, int index, float yPos, float startTime)
        {
            var clip = section.FindPropertyRelative("clip").objectReferenceValue as AnimationClip;
            var sectionName = section.FindPropertyRelative("sectionName").stringValue;
            var playRate = section.FindPropertyRelative("playRate").floatValue;
            
            if (clip == null) return Rect.zero;
            
            float duration = clip.length / playRate;
            float width = duration * pixelsPerSecond;
            float xPos = 10 + startTime * pixelsPerSecond;
            
            var rect = new Rect(xPos, yPos + 10, width, sectionHeight);
            
            // 섹션 배경
            Color barColor = selectedSectionIndex == index ? selectedColor : sectionColor;
            EditorGUI.DrawRect(rect, barColor);
            
            // 섹션 테두리
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.white);
            
            // 섹션 정보 텍스트
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontStyle = FontStyle.Bold;
            
            GUI.Label(rect, $"{sectionName}\n{clip.name}\n{duration:F2}s", labelStyle);
            
            // 노티파이 표시
            DrawNotifies(section, rect, duration);
            
            return rect;
        }
        
        private void DrawNotifies(SerializedProperty section, Rect sectionRect, float duration)
        {
            var notifies = section.FindPropertyRelative("notifies");
            
            for (int i = 0; i < notifies.arraySize; i++)
            {
                var notify = notifies.GetArrayElementAtIndex(i);
                var triggerTime = notify.FindPropertyRelative("triggerTime").floatValue;
                var notifyName = notify.FindPropertyRelative("notifyName").stringValue;
                
                float xPos = sectionRect.x + (sectionRect.width * triggerTime);
                var notifyRect = new Rect(xPos - 2, sectionRect.y, 4, sectionRect.height);
                
                EditorGUI.DrawRect(notifyRect, notifyColor);
                
                // 노티파이 이름 표시 (작은 라벨)
                var labelRect = new Rect(xPos + 5, sectionRect.y, 100, 15);
                var labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = notifyColor;
                GUI.Label(labelRect, notifyName, labelStyle);
            }
        }
        #endregion
        
        #region Section Details
        private void DrawSectionDetails()
        {
            if (selectedSectionIndex < 0)
            {
                EditorGUILayout.HelpBox("타임라인에서 섹션을 선택하여 자세한 정보를 편집하세요.", MessageType.Info);
                return;
            }
            
            var sectionsProp = serializedMontage.FindProperty("sections");
            
            if (selectedSectionIndex >= sectionsProp.arraySize)
            {
                selectedSectionIndex = -1;
                return;
            }
            
            var section = sectionsProp.GetArrayElementAtIndex(selectedSectionIndex);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"섹션 상세 정보 - {selectedSectionIndex}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("섹션 삭제", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("섹션 삭제", 
                    $"섹션 {selectedSectionIndex}를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    sectionsProp.DeleteArrayElementAtIndex(selectedSectionIndex);
                    selectedSectionIndex = -1;
                    serializedMontage.ApplyModifiedProperties();
                    Repaint();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 기본 정보
            EditorGUILayout.PropertyField(section.FindPropertyRelative("sectionName"), new GUIContent("섹션 이름"));
            EditorGUILayout.PropertyField(section.FindPropertyRelative("clip"), new GUIContent("애니메이션 클립"));
            
            EditorGUILayout.Space(5);
            
            // 페이드 설정
            EditorGUILayout.LabelField("페이드 설정", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(section.FindPropertyRelative("fadeInDuration"), new GUIContent("페이드 인"));
            EditorGUILayout.PropertyField(section.FindPropertyRelative("fadeOutDuration"), new GUIContent("페이드 아웃"));
            
            EditorGUILayout.Space(5);
            
            // 재생 설정
            EditorGUILayout.LabelField("재생 설정", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(section.FindPropertyRelative("playRate"), new GUIContent("재생 속도"));
            EditorGUILayout.PropertyField(section.FindPropertyRelative("loop"), new GUIContent("루프"));
            EditorGUILayout.PropertyField(section.FindPropertyRelative("nextSection"), new GUIContent("다음 섹션"));
            
            EditorGUILayout.Space(5);
            
            // 노티파이
            DrawNotifyList(section);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawNotifyList(SerializedProperty section)
        {
            var notifies = section.FindPropertyRelative("notifies");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("노티파이", EditorStyles.miniBoldLabel);
            
            if (GUILayout.Button("추가", GUILayout.Width(50)))
            {
                notifies.InsertArrayElementAtIndex(notifies.arraySize);
                var newNotify = notifies.GetArrayElementAtIndex(notifies.arraySize - 1);
                newNotify.FindPropertyRelative("notifyName").stringValue = "NewNotify";
                newNotify.FindPropertyRelative("triggerTime").floatValue = 0.5f;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel++;
            
            for (int i = 0; i < notifies.arraySize; i++)
            {
                var notify = notifies.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(notify.FindPropertyRelative("notifyName"), new GUIContent("이름"));
                
                var triggerTimeProp = notify.FindPropertyRelative("triggerTime");
                EditorGUILayout.Slider(triggerTimeProp, 0f, 1f, new GUIContent("발생 시점 (0~1)"));
                
                EditorGUILayout.EndVertical();
                
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(40)))
                {
                    notifies.DeleteArrayElementAtIndex(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
            }
            
            EditorGUI.indentLevel--;
        }
        #endregion
    }
}
