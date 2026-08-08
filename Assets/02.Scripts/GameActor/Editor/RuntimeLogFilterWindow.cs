#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Diagnostics;

namespace UPlayGround.Editor.Diagnostics
{
    /// <summary>
    /// RuntimeLog의 카테고리 마스크를 편집하고 PlayerPrefs 저장값과 동기화한다.
    /// 체크 변경은 Edit Mode와 Play Mode의 현재 RuntimeLog에 즉시 반영된다.
    /// </summary>
    public sealed class RuntimeLogFilterWindow : EditorWindow
    {
        private readonly struct CategoryDescriptor
        {
            public CategoryDescriptor(
                RuntimeLogCategory category,
                string label,
                string description)
            {
                Category = category;
                Label = label;
                Description = description;
            }

            public RuntimeLogCategory Category { get; }
            public string Label { get; }
            public string Description { get; }
        }

        private static readonly CategoryDescriptor[] Categories =
        {
            new(RuntimeLogCategory.Default, "Default", "별도 기능 분류가 없는 일반 진단"),
            new(RuntimeLogCategory.Combat, "Combat", "공격, 피해, 리액션과 전투 흐름"),
            new(RuntimeLogCategory.Player, "Player", "플레이어 상태와 플레이어 전용 진단"),
            new(RuntimeLogCategory.Monster, "Monster", "몬스터 BT와 몬스터 전용 진단"),
            new(RuntimeLogCategory.Input, "Input", "입력 라우팅, 버퍼와 콜백 처리"),
            new(RuntimeLogCategory.UI, "UI", "UI 열기, 숨김과 제거 생명주기"),
            new(RuntimeLogCategory.System, "System", "매니저와 시스템 생명주기"),
            new(RuntimeLogCategory.Boot, "Boot", "게임과 매니저 초기화"),
            new(RuntimeLogCategory.AI, "AI", "Behavior Tree 외 AI 판단과 탐지"),
            new(RuntimeLogCategory.Camera, "Camera", "카메라 모드, 락온과 연출"),
            new(RuntimeLogCategory.Asset, "Asset", "Addressables와 데이터 로딩"),
            new(RuntimeLogCategory.Performance, "Performance", "성능 측정과 기준선 저장"),
        };

        private static readonly RuntimeLogCategory KnownCategoryMask = CreateKnownCategoryMask();

        private Vector2 _scroll;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/Debug/런타임 로그 필터")]
        public static void Open()
        {
            RuntimeLogFilterWindow window = GetWindow<RuntimeLogFilterWindow>();
            window.titleContent = new GUIContent(
                "Log Filter",
                EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
            window.minSize = new Vector2(420f, 440f);
            window.Show();
        }

        private void OnEnable()
        {
            RuntimeLog.ReloadEnabledCategories();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            EditorGUILayout.Space(6f);

            EditorGUILayout.HelpBox(
                "체크 변경은 즉시 적용되고 PlayerPrefs에 저장됩니다. " +
                "여러 카테고리가 지정된 로그는 활성 카테고리와 하나라도 겹치면 출력됩니다.",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawCategories();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            DrawSummary();
        }

        private static void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 38f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.15f, 0.19f));
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 20f),
                "Runtime Log Filter",
                EditorStyles.boldLabel);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 21f, rect.width - 24f, 15f),
                EditorApplication.isPlaying ? "Play Mode에 즉시 적용 중" : "다음 Play Mode에도 저장값 유지",
                EditorStyles.miniLabel);
        }

        private static void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("모두 켜기", EditorStyles.toolbarButton))
                    RuntimeLog.SetEnabledCategories(RuntimeLogCategory.All);

                if (GUILayout.Button("모두 끄기", EditorStyles.toolbarButton))
                    RuntimeLog.SetEnabledCategories(RuntimeLogCategory.None);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("저장값 다시 읽기", EditorStyles.toolbarButton))
                    RuntimeLog.ReloadEnabledCategories();
            }
        }

        private static void DrawCategories()
        {
            RuntimeLogCategory current = RuntimeLog.EnabledCategories;
            RuntimeLogCategory next = current;

            EditorGUILayout.LabelField("카테고리", EditorStyles.boldLabel);
            for (int i = 0; i < Categories.Length; i++)
            {
                CategoryDescriptor descriptor = Categories[i];
                bool enabled = (current & descriptor.Category) != 0;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    bool nextEnabled = EditorGUILayout.ToggleLeft(
                        descriptor.Label,
                        enabled,
                        GUILayout.Width(125f));
                    EditorGUILayout.LabelField(descriptor.Description, EditorStyles.miniLabel);

                    if (nextEnabled == enabled)
                        continue;

                    if (nextEnabled)
                        next |= descriptor.Category;
                    else
                        next &= ~descriptor.Category;
                }
            }

            if (next != current)
                RuntimeLog.SetEnabledCategories(next);
        }

        private static void DrawSummary()
        {
            RuntimeLogCategory enabled = RuntimeLog.EnabledCategories & KnownCategoryMask;
            int enabledCount = 0;
            var names = new StringBuilder();

            for (int i = 0; i < Categories.Length; i++)
            {
                CategoryDescriptor descriptor = Categories[i];
                if ((enabled & descriptor.Category) == 0)
                    continue;

                if (names.Length > 0)
                    names.Append(", ");
                names.Append(descriptor.Label);
                enabledCount++;
            }

            string activeNames = names.Length > 0 ? names.ToString() : "None";
            EditorGUILayout.LabelField(
                $"활성 {enabledCount} / {Categories.Length}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(activeNames, EditorStyles.wordWrappedMiniLabel);
        }

        private static RuntimeLogCategory CreateKnownCategoryMask()
        {
            RuntimeLogCategory mask = RuntimeLogCategory.None;
            for (int i = 0; i < Categories.Length; i++)
                mask |= Categories[i].Category;
            return mask;
        }
    }
}
#endif
