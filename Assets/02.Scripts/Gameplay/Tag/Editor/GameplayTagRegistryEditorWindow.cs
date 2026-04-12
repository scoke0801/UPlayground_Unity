using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag.Editor
{
    /// <summary>
    /// GameplayTag 정의 목록을 관리하고 GameplayTagsGenerated.cs 를 자동 생성하는 에디터 창.
    /// 메뉴: UPlayGround/GameplayTag/Tag Registry Editor
    ///
    /// 워크플로우:
    ///   1. 태그 추가 / 제거 / 수정
    ///   2. "코드 생성" 버튼 클릭
    ///   3. Unity 재컴파일 후 GameplayTagId enum으로 태그 사용 가능
    /// </summary>
    public class GameplayTagRegistryEditorWindow : EditorWindow
    {
        // ── 경로 상수 ─────────────────────────────────────────────────
        private const string GeneratedFilePath =
            "Assets/02.Scripts/Gameplay/Tag/GameplayTagsGenerated.cs";
        private const string DefaultRegistryPath =
            "Assets/02.Scripts/Gameplay/Tag/GameplayTagRegistry.asset";

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.13f, 0.13f, 0.18f);
        private static readonly Color ColorRowEven  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd   = new(0.24f, 0.24f, 0.26f);
        private static readonly Color ColorSelected = new(0.18f, 0.36f, 0.58f);
        private static readonly Color ColorSuccess  = new(0.25f, 0.75f, 0.35f);
        private static readonly Color ColorWarn     = new(0.85f, 0.60f, 0.10f);

        // ── 상태 ─────────────────────────────────────────────────────
        private GameplayTagRegistrySO _registry;
        private SerializedObject      _serializedObj;
        private SerializedProperty    _tagsProp;

        private ReorderableList _rl;
        private int             _selectedIndex = -1;
        private Vector2         _listScroll;
        private Vector2         _detailScroll;

        private bool   _showPreview;
        private string _previewCode = "";
        private string _statusMsg   = "";
        private bool   _statusOk    = true;
        private double _statusExpireTime;

        private const float LeftPanelW  = 260f;
        private const float RowH        = 24f;
        private const float SwatchW     = 14f;

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/GameplayTag/Tag Registry Editor")]
        public static void Open()
        {
            var w = GetWindow<GameplayTagRegistryEditorWindow>();
            w.titleContent = new GUIContent("GameplayTag Registry",
                EditorGUIUtility.IconContent("d_FilterByLabel").image);
            w.minSize = new Vector2(800f, 480f);
            w.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────────
        private void OnEnable()
        {
            TryAutoLoadRegistry();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _serializedObj?.ApplyModifiedProperties();
        }

        private void OnEditorUpdate()
        {
            if (!string.IsNullOrEmpty(_statusMsg) &&
                EditorApplication.timeSinceStartup > _statusExpireTime)
            {
                _statusMsg = "";
                Repaint();
            }
        }

        // ── 레지스트리 로드 ───────────────────────────────────────────
        private void TryAutoLoadRegistry()
        {
            // 이미 지정된 경우 유지
            if (_registry != null) return;

            // 프로젝트에서 GameplayTagRegistrySO 탐색
            var guids = AssetDatabase.FindAssets("t:GameplayTagRegistrySO");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                SetRegistry(AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(path));
                return;
            }
        }

        private void SetRegistry(GameplayTagRegistrySO reg)
        {
            if (reg == null) return;
            _registry      = reg;
            _serializedObj = new SerializedObject(reg);
            _tagsProp      = _serializedObj.FindProperty("tags");
            _selectedIndex = -1;
            BuildReorderableList();
            Repaint();
        }

        // ── OnGUI ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            _serializedObj?.Update();

            DrawToolbar();

            if (_registry == null)
            {
                DrawNoRegistryPanel();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                DrawLeftPanel();
                DrawRightPanel();
                EditorGUILayout.EndHorizontal();

                DrawPreviewSection();
                DrawStatusBar();
            }

            _serializedObj?.ApplyModifiedProperties();
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Registry SO 선택
            GUILayout.Label("Registry SO:", EditorStyles.toolbarButton, GUILayout.Width(80));
            var newReg = (GameplayTagRegistrySO)EditorGUILayout.ObjectField(
                _registry, typeof(GameplayTagRegistrySO), false, GUILayout.Width(200));
            if (newReg != _registry && newReg != null)
                SetRegistry(newReg);

            GUILayout.Space(6);

            // 신규 생성 버튼
            if (GUILayout.Button("새로 생성", EditorStyles.toolbarButton, GUILayout.Width(65)))
                CreateNewRegistry();

            GUILayout.FlexibleSpace();

            if (_registry != null)
            {
                GUILayout.Label($"{_tagsProp?.arraySize ?? 0}개 태그",
                    EditorStyles.toolbarButton, GUILayout.Width(60));

                if (GUILayout.Button("기본값으로 초기화", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    if (EditorUtility.DisplayDialog("초기화 확인",
                        "현재 태그 목록을 지우고 기본 태그로 채웁니다. 계속하시겠습니까?", "초기화", "취소"))
                    {
                        Undo.RecordObject(_registry, "Reset GameplayTag Defaults");
                        _registry.ResetToDefaults();
                        EditorUtility.SetDirty(_registry);
                        _serializedObj.Update();
                        BuildReorderableList();
                        _previewCode = "";
                        ShowStatus("기본 태그로 초기화 완료.", true);
                    }
                }

                GUILayout.Space(4);

                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.65f, 0.95f);
                if (GUILayout.Button("▶  코드 생성", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    GenerateCode();
                GUI.backgroundColor = oldBg;

                if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    _serializedObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_registry);
                    AssetDatabase.SaveAssets();
                    ShowStatus("저장 완료.", true);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── Registry 없을 때 ─────────────────────────────────────────
        private void DrawNoRegistryPanel()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("GameplayTagRegistrySO를 지정하세요.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("기본 경로에 새 Registry 생성", GUILayout.Width(220), GUILayout.Height(28)))
                CreateNewRegistry();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        private void CreateNewRegistry()
        {
            string dir = Path.GetDirectoryName(DefaultRegistryPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var so = CreateInstance<GameplayTagRegistrySO>();
            so.ResetToDefaults();
            AssetDatabase.CreateAsset(so, DefaultRegistryPath);
            AssetDatabase.SaveAssets();
            SetRegistry(so);
            ShowStatus($"Registry 생성: {DefaultRegistryPath}", true);
        }

        // ── ReorderableList 구성 ──────────────────────────────────────
        private void BuildReorderableList()
        {
            if (_tagsProp == null) return;

            _rl = new ReorderableList(_serializedObj, _tagsProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _rl.drawHeaderCallback = rect =>
                GUI.Label(rect, "태그 목록 (드래그로 순서 변경)", EditorStyles.boldLabel);

            _rl.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (_tagsProp.arraySize <= index) return;
                DrawListRow(rect, _tagsProp.GetArrayElementAtIndex(index), index, isActive);
            };

            _rl.elementHeight    = RowH + 2f;
            _rl.onSelectCallback = list => _selectedIndex = list.index;

            _rl.onAddCallback = list =>
            {
                _tagsProp.arraySize++;
                int ni = _tagsProp.arraySize - 1;
                var e  = _tagsProp.GetArrayElementAtIndex(ni);
                e.FindPropertyRelative("tagName").stringValue     = "";
                e.FindPropertyRelative("enumName").stringValue    = "";
                e.FindPropertyRelative("description").stringValue = "";
                var c = e.FindPropertyRelative("color");
                c.colorValue = new Color(0.4f, 0.8f, 1.0f);
                _selectedIndex = ni;
                _serializedObj.ApplyModifiedProperties();
            };
        }

        // ── 왼쪽: 목록 패널 ─────────────────────────────────────────
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelW), GUILayout.ExpandHeight(true));
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            _rl?.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawListRow(Rect rect, SerializedProperty elem, int index, bool isActive)
        {
            Color bg = isActive ? ColorSelected : (index % 2 == 0 ? ColorRowEven : ColorRowOdd);
            EditorGUI.DrawRect(rect, bg);

            float x = rect.x + 2;
            float y = rect.y + (rect.height - 16f) * 0.5f;

            // 색상 스워치
            var colorProp = elem.FindPropertyRelative("color");
            var swatchRect = new Rect(x, y, SwatchW, 16f);
            EditorGUI.DrawRect(swatchRect, colorProp.colorValue);
            x += SwatchW + 4f;

            // enum 이름
            var enumProp = elem.FindPropertyRelative("enumName");
            var tagProp  = elem.FindPropertyRelative("tagName");
            string display = string.IsNullOrWhiteSpace(enumProp.stringValue)
                ? (string.IsNullOrWhiteSpace(tagProp.stringValue)
                    ? "(이름 없음)"
                    : tagProp.stringValue.Replace('.', '_'))
                : enumProp.stringValue;

            GUI.Label(new Rect(x, y, LeftPanelW - x - 8f, 16f), display, EditorStyles.miniLabel);
        }

        // ── 오른쪽: 디테일 패널 ──────────────────────────────────────
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedIndex < 0 || _selectedIndex >= _tagsProp.arraySize)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← 왼쪽에서 태그를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            DrawTagDetail(_tagsProp.GetArrayElementAtIndex(_selectedIndex));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagDetail(SerializedProperty elem)
        {
            // 헤더
            Rect hdr = GUILayoutUtility.GetRect(0, 28f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(hdr, ColorHeader);
            GUI.Label(new Rect(hdr.x + 8, hdr.y + 5, hdr.width, 18f),
                "태그 상세 설정", EditorStyles.whiteBoldLabel);

            GUILayout.Space(8);

            var tagProp   = elem.FindPropertyRelative("tagName");
            var enumProp  = elem.FindPropertyRelative("enumName");
            var descProp  = elem.FindPropertyRelative("description");
            var colorProp = elem.FindPropertyRelative("color");

            // 태그 이름
            EditorGUILayout.LabelField("태그 이름 (tagName)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "계층형 경로. '.'으로 구분. 예) State.Combat.Sprint\n" +
                "이 값이 런타임 GameplayTag.TagName 이 됩니다.",
                MessageType.None);
            tagProp.stringValue = EditorGUILayout.TextField(tagProp.stringValue);

            GUILayout.Space(6);

            // enum 이름 + 자동 채우기
            EditorGUILayout.LabelField("enum 멤버 이름 (enumName)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "생성될 GameplayTagId 의 멤버 이름. 비워두면 tagName에서 자동 생성.\n" +
                "예) tagName=\"State.Combat.Sprint\" → enumName=\"State_Combat_Sprint\"",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            enumProp.stringValue = EditorGUILayout.TextField(enumProp.stringValue);
            if (GUILayout.Button("자동", GUILayout.Width(40)))
                enumProp.stringValue = tagProp.stringValue.Replace('.', '_').Replace(' ', '_');
            EditorGUILayout.EndHorizontal();

            string effectiveEnum = string.IsNullOrWhiteSpace(enumProp.stringValue)
                ? tagProp.stringValue.Replace('.', '_')
                : enumProp.stringValue;

            EditorGUILayout.HelpBox($"→  GameplayTagId.{effectiveEnum}", MessageType.None);

            GUILayout.Space(6);

            // 설명
            EditorGUILayout.LabelField("설명 (description)", EditorStyles.boldLabel);
            descProp.stringValue = EditorGUILayout.TextField(descProp.stringValue);

            GUILayout.Space(6);

            // 색상
            EditorGUILayout.LabelField("에디터 시각화 색상", EditorStyles.boldLabel);
            colorProp.colorValue = EditorGUILayout.ColorField(colorProp.colorValue);

            GUILayout.Space(10);

            // 미리보기: 이 태그만 코드로 표시
            if (!string.IsNullOrWhiteSpace(tagProp.stringValue))
            {
                EditorGUILayout.LabelField("사용 예시", EditorStyles.boldLabel);
                string preview =
                    $"// 태그 추가\n" +
                    $"actor.Tags.AddTag(GameplayTagId.{effectiveEnum}.ToTag());\n\n" +
                    $"// 태그 보유 여부 확인\n" +
                    $"actor.Tags.HasTag(GameplayTagId.{effectiveEnum});\n\n" +
                    $"// ComboSequenceEntry requiredTagIds 에 추가\n" +
                    $"// → 에디터에서 드롭다운으로 선택 가능";
                EditorGUILayout.TextArea(preview,
                    GUILayout.Height(EditorStyles.textArea.lineHeight * 7 + 6));
            }
        }

        // ── 코드 생성 ─────────────────────────────────────────────────
        private void GenerateCode()
        {
            _serializedObj.ApplyModifiedProperties();

            if (_registry.tags == null || _registry.tags.Count == 0)
            {
                ShowStatus("태그가 없습니다. 먼저 태그를 추가하세요.", false);
                return;
            }

            // 유효성 검사
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var def in _registry.tags)
            {
                if (!def.IsValid())
                {
                    ShowStatus("tagName이 비어 있는 항목이 있습니다. 모든 태그를 채워주세요.", false);
                    return;
                }
                string enumName = def.GetEffectiveEnumName();
                if (!seen.Add(enumName))
                {
                    ShowStatus($"중복된 enum 이름: \"{enumName}\"", false);
                    return;
                }
            }

            string code = BuildGeneratedCode(_registry.tags);

            // 파일 쓰기
            string fullPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                GeneratedFilePath);
            string dir = Path.GetDirectoryName(fullPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, code, Encoding.UTF8);
            AssetDatabase.Refresh();

            _previewCode = code;
            _showPreview = true;
            ShowStatus($"코드 생성 완료 → {GeneratedFilePath}", true);
        }

        private string BuildGeneratedCode(List<GameplayTagDefinition> defs)
        {
            var sb = new StringBuilder();
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 파일 헤더
            sb.AppendLine("// ============================================================");
            sb.AppendLine("// AUTO-GENERATED — GameplayTagRegistry Editor");
            sb.AppendLine("// UPlayGround/GameplayTag/Tag Registry Editor 에서 관리하세요.");
            sb.AppendLine("// 직접 편집하지 마세요. 저장 후 에디터에서 \"코드 생성\"을 눌러야 반영됩니다.");
            sb.AppendLine($"// Generated: {date}");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();
            sb.AppendLine("namespace UPlayGround.Gameplay.Tag");
            sb.AppendLine("{");

            // ── enum ──
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// GameplayTag 식별자 열거형.");
            sb.AppendLine("    /// GameplayTagRegistrySO + Tag Registry Editor 에서 관리하며 코드가 자동 생성된다.");
            sb.AppendLine("    /// 코드에서는 반드시 이 enum을 사용하고, 문자열을 직접 쓰지 않는다.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public enum GameplayTagId");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");

            int idx = 1;
            int maxEnumLen = 0;
            foreach (var def in defs)
                maxEnumLen = Math.Max(maxEnumLen, def.GetEffectiveEnumName().Length);

            foreach (var def in defs)
            {
                string en = def.GetEffectiveEnumName();
                string padded = en.PadRight(maxEnumLen);
                sb.AppendLine($"        {padded} = {idx},");
                idx++;
            }

            sb.AppendLine("    }");
            sb.AppendLine();

            // ── Extension ──
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// GameplayTagId → GameplayTag 변환 확장 메서드.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class GameplayTagIdExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        private static readonly string[] s_TagNames = new string[]");
            sb.AppendLine("        {");
            sb.AppendLine("            \"\",  // None = 0");

            idx = 1;
            int maxTagLen = 0;
            foreach (var def in defs)
                maxTagLen = Math.Max(maxTagLen, def.tagName.Length + 2); // +2 for quotes

            foreach (var def in defs)
            {
                string quoted        = $"\"{def.tagName}\",";           // 쉼표 포함
                string padded        = quoted.PadRight(maxTagLen + 3);  // +3 = 따옴표 2 + 쉼표 1
                string comment       = $"// {def.GetEffectiveEnumName()} = {idx}";
                if (!string.IsNullOrWhiteSpace(def.description))
                    comment += $"  ({def.description})";
                sb.AppendLine($"            {padded}  {comment}");
                idx++;
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>GameplayTagId를 GameplayTag 구조체로 변환한다.</summary>");
            sb.AppendLine("        public static GameplayTag ToTag(this GameplayTagId id)");
            sb.AppendLine("        {");
            sb.AppendLine("            int i = (int)id;");
            sb.AppendLine("            return new GameplayTag(i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>GameplayTagId의 태그 이름 문자열을 반환한다.</summary>");
            sb.AppendLine("        public static string TagName(this GameplayTagId id)");
            sb.AppendLine("        {");
            sb.AppendLine("            int i = (int)id;");
            sb.AppendLine("            return i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ── 미리보기 섹션 ─────────────────────────────────────────────
        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showPreview = GUILayout.Toggle(_showPreview, "생성 코드 미리보기",
                EditorStyles.toolbarButton, GUILayout.Width(120));

            if (GUILayout.Button("미리보기 갱신", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                _serializedObj.ApplyModifiedProperties();
                if (_registry.tags?.Count > 0)
                {
                    _previewCode = BuildGeneratedCode(_registry.tags);
                    _showPreview = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_showPreview && !string.IsNullOrEmpty(_previewCode))
            {
                float h = Mathf.Clamp(position.height * 0.30f, 80f, 200f);
                EditorGUILayout.TextArea(_previewCode, GUILayout.Height(h));
            }
        }

        // ── 상태 바 ───────────────────────────────────────────────────
        private void DrawStatusBar()
        {
            if (string.IsNullOrEmpty(_statusMsg)) return;

            var oldColor = GUI.contentColor;
            GUI.contentColor = _statusOk ? ColorSuccess : ColorWarn;
            EditorGUILayout.LabelField(_statusMsg, EditorStyles.miniLabel);
            GUI.contentColor = oldColor;
        }

        private void ShowStatus(string msg, bool ok)
        {
            _statusMsg        = msg;
            _statusOk         = ok;
            _statusExpireTime = EditorApplication.timeSinceStartup + 4.0;
            Repaint();
        }
    }
}
