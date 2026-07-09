#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Editor.Stat
{
    /// <summary>
    /// ActorStatSO 인스펙터 커스텀 에디터.
    /// 카테고리별로 묶어 슬라이더+필드로 편집한다.
    /// </summary>
    [CustomEditor(typeof(ActorStatSO))]
    public class ActorStatSOEditor : UnityEditor.Editor
    {
        // ── 카테고리 정의 ─────────────────────────────────────────
        private static readonly (string label, StatType[] types, Color color)[] _categories =
        {
            ("생존",  new[] { StatType.MaxHealth, StatType.HealthRegenRate },                      new Color(0.20f, 0.75f, 0.30f)),
            ("전투",  new[] { StatType.AttackPower, StatType.Defense, StatType.CritRate, StatType.CritMultiplier }, new Color(0.85f, 0.30f, 0.30f)),
            ("이동",  new[] { StatType.MoveSpeed, StatType.DashDistance },                          new Color(0.30f, 0.55f, 0.90f)),
            ("강인도", new[] { StatType.MaxPoise, StatType.PoiseRecoveryRate, StatType.PoiseRecoveryDelay }, new Color(0.85f, 0.70f, 0.10f)),
            ("스킬",  new[] { StatType.SkillGaugeRate, StatType.InvincibleDuration },               new Color(0.60f, 0.30f, 0.90f)),
            ("생활",  new[] { StatType.GatheringPower },                                             new Color(0.35f, 0.70f, 0.65f)),
        };

        // ── 슬라이더 범위 ─────────────────────────────────────────
        private static readonly Dictionary<StatType, (float min, float max)> _sliderRanges = new()
        {
            { StatType.MaxHealth,          (0f, 999999f) },
            { StatType.HealthRegenRate,    (0f, 50f)   },
            { StatType.AttackPower,        (0f, 5f)    },
            { StatType.Defense,            (0f, 1f)    },
            { StatType.CritRate,           (0f, 1f)    },
            { StatType.CritMultiplier,     (1f, 5f)    },
            { StatType.MoveSpeed,          (0f, 3f)    },
            { StatType.DashDistance,       (0f, 3f)    },
            { StatType.MaxPoise,           (0f, 500f)  },
            { StatType.PoiseRecoveryRate,  (0f, 200f)  },
            { StatType.PoiseRecoveryDelay, (0f, 10f)   },
            { StatType.SkillGaugeRate,     (0f, 5f)    },
            { StatType.InvincibleDuration, (0f, 3f)    },
            { StatType.GatheringPower,     (0f, 50f)   },
        };

        // ── 스타일 캐시 ───────────────────────────────────────────
        private GUIStyle _categoryHeaderStyle;
        private bool _stylesInitialized;

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 0, 2, 2),
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            var so = (ActorStatSO)target;

            DrawSummaryHeader(so);
            EditorGUILayout.Space(4);
            DrawCategories(so);
            EditorGUILayout.Space(6);
            DrawActionButtons(so);
        }

        // ── 요약 헤더 ─────────────────────────────────────────────

        private void DrawSummaryHeader(ActorStatSO so)
        {
            int defined = 0;
            int total   = System.Enum.GetValues(typeof(StatType)).Length;
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                if (so.TryGetExplicit(type, out _)) defined++;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField($"{so.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"명시 스탯: {defined} / {total}  (누락 항목은 기본값 폴백)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── 카테고리 ──────────────────────────────────────────────

        private void DrawCategories(ActorStatSO so)
        {
            foreach (var category in _categories)
            {
                DrawCategoryHeader(category.label, category.color);
                foreach (var type in category.types)
                    DrawStatRow(so, type, category.color);
                EditorGUILayout.Space(3);
            }
        }

        private void DrawCategoryHeader(string label, Color color)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.6f));

            // 좌측 컬러 바
            var bar = new Rect(rect.x, rect.y, 4, rect.height);
            EditorGUI.DrawRect(bar, color);

            var labelRect = new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height);
            GUI.Label(labelRect, $"▌{label}", _categoryHeaderStyle);
        }

        private void DrawStatRow(ActorStatSO so, StatType type, Color color)
        {
            bool isExplicit = so.TryGetExplicit(type, out float value);
            (float min, float max) = _sliderRanges.TryGetValue(type, out var r) ? r : (0f, 100f);

            EditorGUILayout.BeginHorizontal();

            // 명시 여부 색상 표시
            var prevColor = GUI.color;
            GUI.color = isExplicit ? Color.white : new Color(0.6f, 0.6f, 0.6f);

            // 라벨
            GUILayout.Label(type.ToString(), GUILayout.Width(155));

            bool resetClicked = false;
            bool addClicked   = false;
            float newValue    = value;

            if (isExplicit)
            {
                // 슬라이더 범위는 권장 범위(드래그 편의용)일 뿐, 입력 필드는 제한 없음.
                // EditorGUILayout.Slider는 타이핑 입력까지 범위로 클램프해서 분리했다.
                EditorGUI.BeginChangeCheck();
                float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                    newValue = sliderValue;

                EditorGUI.BeginChangeCheck();
                float fieldValue = EditorGUILayout.FloatField(newValue, GUILayout.Width(55));
                if (EditorGUI.EndChangeCheck())
                    newValue = fieldValue;

                GUI.color = prevColor;
                resetClicked = GUILayout.Button(new GUIContent("↺", "명시 해제 (기본값 폴백)"),
                    GUILayout.Width(22), GUILayout.Height(18));
            }
            else
            {
                // 폴백 = 미등록 상태. 직접 편집 대신 [+]로 명시 등록 후 편집한다.
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
                    EditorGUILayout.FloatField(value, GUILayout.Width(55));
                }
                GUI.color = prevColor;
                addClicked = GUILayout.Button(new GUIContent("+", "기본값으로 명시 등록"),
                    GUILayout.Width(22), GUILayout.Height(18));
            }

            EditorGUILayout.EndHorizontal();

            if (resetClicked)
            {
                Undo.RecordObject(so, "Reset Stat");
                so.EditorRemove(type);
                EditorUtility.SetDirty(so);
                return;
            }

            if (addClicked)
            {
                Undo.RecordObject(so, "Add Stat");
                so.EditorSet(type, value);
                EditorUtility.SetDirty(so);
                return;
            }

            if (isExplicit && !Mathf.Approximately(newValue, value))
            {
                Undo.RecordObject(so, "Edit Stat");
                so.EditorSet(type, newValue);
                EditorUtility.SetDirty(so);
            }
        }

        // ── 액션 버튼 ─────────────────────────────────────────────

        private void DrawActionButtons(ActorStatSO so)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("누락 스탯 채우기", GUILayout.Height(22)))
            {
                Undo.RecordObject(so, "Fill Missing Stats");
                so.EditorFillMissing();
                EditorUtility.SetDirty(so);
            }

            if (GUILayout.Button("전체 초기화", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("초기화", "모든 명시 스탯을 제거할까요? (이후 모두 기본값 폴백 사용)", "확인", "취소"))
                {
                    Undo.RecordObject(so, "Reset All Stats");
                    foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                        so.EditorRemove(type);
                    EditorUtility.SetDirty(so);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (GUILayout.Button("Stat Database Editor 열기", GUILayout.Height(22)))
            {
                EditorApplication.ExecuteMenuItem("UPlayGround/게임플레이/스탯/스탯 데이터베이스 에디터");
            }
        }
    }
}
#endif
