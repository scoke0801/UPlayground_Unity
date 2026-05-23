#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.AI.CombatDecision;
using UPlayGround.AI.Debugging;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    internal static class IntentScoreTimelineRenderer
    {
        private static readonly CombatIntent[] Intents =
        {
            CombatIntent.Attack,
            CombatIntent.Punish,
            CombatIntent.Counter,
            CombatIntent.Pressure,
            CombatIntent.Chase,
            CombatIntent.Retreat,
            CombatIntent.KeepDistance,
            CombatIntent.Defend,
            CombatIntent.Recover
        };

        private static Texture2D _pixel;

        public static void Draw(IReadOnlyList<IntentScoreSnapshot> snapshots)
        {
            EnsurePixel();

            EditorGUILayout.LabelField("Intent Score Timeline", EditorStyles.boldLabel);
            if (snapshots == null || snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("Play Mode에서 Debug Runner를 지정하면 Intent 점수 히스토리가 표시됩니다.", MessageType.Info);
                return;
            }

            var latest = snapshots[snapshots.Count - 1];
            EditorGUILayout.LabelField($"Selected: {latest.SelectedIntent}   Rhythm: {latest.RhythmPhase}   Repeat: {latest.ConsecutiveIntentCount}");
            EditorGUILayout.LabelField(latest.Reason, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            var chartRect = GUILayoutUtility.GetRect(10f, 182f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(chartRect, _pixel, ScaleMode.StretchToFill, false, 0f, new Color(0.055f, 0.055f, 0.07f), 0f, 0f);
            DrawSelectedIntentBand(chartRect, snapshots);

            var rowHeight = chartRect.height / Intents.Length;
            for (var i = 0; i < Intents.Length; i++)
            {
                var intent = Intents[i];
                var row = new Rect(chartRect.x, chartRect.y + i * rowHeight, chartRect.width, rowHeight - 1f);
                DrawIntentRow(row, snapshots, intent, GetIntentColor(intent));
            }

            DrawHover(chartRect, snapshots);
            DrawLegend();
        }

        private static void DrawSelectedIntentBand(Rect chartRect, IReadOnlyList<IntentScoreSnapshot> snapshots)
        {
            var band = new Rect(chartRect.x, chartRect.y, chartRect.width, 5f);
            for (var i = 0; i < snapshots.Count; i++)
            {
                var x = Mathf.Lerp(band.xMin, band.xMax, snapshots.Count <= 1 ? 1f : i / (float)(snapshots.Count - 1));
                var width = Mathf.Max(1f, band.width / Mathf.Max(1, snapshots.Count - 1));
                GUI.DrawTexture(new Rect(x, band.y, width, band.height), _pixel, ScaleMode.StretchToFill, false, 0f, GetIntentColor(snapshots[i].SelectedIntent), 0f, 0f);
            }
        }

        private static void DrawIntentRow(Rect row, IReadOnlyList<IntentScoreSnapshot> snapshots, CombatIntent intent, Color color)
        {
            GUI.DrawTexture(row, _pixel, ScaleMode.StretchToFill, false, 0f, new Color(1f, 1f, 1f, 0.025f), 0f, 0f);
            GUI.Label(new Rect(row.x + 4f, row.y + 1f, 82f, row.height), intent.ToString(), EditorStyles.miniLabel);

            var graph = new Rect(row.x + 84f, row.y + 3f, Mathf.Max(1f, row.width - 88f), row.height - 6f);
            var previousX = graph.x;
            var previousY = graph.yMax - snapshots[0].GetScore(intent) * graph.height;
            for (var i = 1; i < snapshots.Count; i++)
            {
                var t = i / (float)(snapshots.Count - 1);
                var x = Mathf.Lerp(graph.xMin, graph.xMax, t);
                var y = graph.yMax - Mathf.Clamp01(snapshots[i].GetScore(intent)) * graph.height;
                DrawLine(new Vector2(previousX, previousY), new Vector2(x, y), color, 2f);
                previousX = x;
                previousY = y;
            }
        }

        private static void DrawHover(Rect chartRect, IReadOnlyList<IntentScoreSnapshot> snapshots)
        {
            var evt = Event.current;
            if (evt == null || !chartRect.Contains(evt.mousePosition))
                return;

            var normalized = Mathf.InverseLerp(chartRect.xMin, chartRect.xMax, evt.mousePosition.x);
            var index = Mathf.Clamp(Mathf.RoundToInt(normalized * (snapshots.Count - 1)), 0, snapshots.Count - 1);
            var snapshot = snapshots[index];
            var x = Mathf.Lerp(chartRect.xMin, chartRect.xMax, normalized);
            GUI.DrawTexture(new Rect(x, chartRect.y, 1f, chartRect.height), _pixel, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);

            var tooltip = $"t={snapshot.Time:0.00}\nSelected={snapshot.SelectedIntent}\nAttack={snapshot.AttackScore:0.00} Punish={snapshot.PunishScore:0.00} Counter={snapshot.CounterScore:0.00}\nPressure={snapshot.PressureScore:0.00} Chase={snapshot.ChaseScore:0.00} Retreat={snapshot.RetreatScore:0.00}\nKeep={snapshot.KeepDistanceScore:0.00} Defend={snapshot.DefendScore:0.00} Recover={snapshot.RecoverScore:0.00}\n{snapshot.Reason}";
            GUI.Label(new Rect(chartRect.x + 8f, chartRect.yMax - 82f, chartRect.width - 16f, 78f), tooltip, EditorStyles.helpBox);
        }

        private static void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();
            foreach (var intent in Intents)
            {
                var rect = GUILayoutUtility.GetRect(10f, 14f, GUILayout.Width(12f));
                GUI.DrawTexture(rect, _pixel, ScaleMode.StretchToFill, false, 0f, GetIntentColor(intent), 0f, 0f);
                EditorGUILayout.LabelField(intent.ToString(), EditorStyles.miniLabel, GUILayout.Width(74f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Handles.BeginGUI();
            var previous = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(width, a, b);
            Handles.color = previous;
            Handles.EndGUI();
        }

        private static Color GetIntentColor(CombatIntent intent)
        {
            return intent switch
            {
                CombatIntent.Attack => new Color(0.95f, 0.32f, 0.28f),
                CombatIntent.Punish => new Color(1.00f, 0.58f, 0.24f),
                CombatIntent.Counter => new Color(0.98f, 0.82f, 0.28f),
                CombatIntent.Pressure => new Color(0.34f, 0.78f, 0.42f),
                CombatIntent.Chase => new Color(0.22f, 0.68f, 0.90f),
                CombatIntent.Retreat => new Color(0.36f, 0.48f, 0.92f),
                CombatIntent.KeepDistance => new Color(0.58f, 0.42f, 0.90f),
                CombatIntent.Defend => new Color(0.62f, 0.78f, 0.92f),
                CombatIntent.Recover => new Color(0.78f, 0.78f, 0.78f),
                _ => Color.white
            };
        }

        private static void EnsurePixel()
        {
            if (_pixel != null)
                return;

            _pixel = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }
    }
}
#endif
