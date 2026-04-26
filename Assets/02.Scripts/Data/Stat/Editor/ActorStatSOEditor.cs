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
        };

        // ── 슬라이더 범위 ─────────────────────────────────────────
        private static readonly Dictionary<StatType, (float min, float max)> _sliderRanges = new()
        {
            { StatType.MaxHealth,          (0f, 2000f) },
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
        };

        // ── 스타일 캐시 ───────────────────────────────────────────
        private GUIStyle _categoryHeaderStyle;
        private GUIStyle _missingLabelStyle;
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

            _missingLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
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

            // 슬라이더
            float newValue = EditorGUILayout.Slider(value, min, max);

            // 명시 여부 토글 (스왑 버튼)
            GUI.color = prevColor;
            if (isExplicit)
            {
                if (GUILayout.Button("↺", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    Undo.RecordObject(so, "Reset Stat");
                    so.EditorRemove(type);
                    EditorUtility.SetDirty(so);
                    return;
                }
            }
            else
            {
                GUILayout.Label("(폴백)", _missingLabelStyle, GUILayout.Width(34));
            }

            EditorGUILayout.EndHorizontal();

            if (!Mathf.Approximately(newValue, value))
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
                EditorApplication.ExecuteMenuItem("UPlayGround/Stat/Stat Database Editor");
            }
        }
    }
}
#endif
