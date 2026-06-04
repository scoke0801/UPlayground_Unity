#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Tool.Editor.Combat
{
    public class CombatLogRecorderWindow : EditorWindow
    {
        private int _capacity;
        private float _expectedDuration;
        private Vector2 _scroll;

        [MenuItem("UPlayGround/Combat/Combat Log Recorder")]
        public static void Open()
        {
            GetWindow<CombatLogRecorderWindow>("Combat Log");
        }

        private void OnEnable()
        {
            _capacity = CombatLogRecorder.Capacity;
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();
            DrawSummary();
            EditorGUILayout.Space();
            DrawPreview();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool enabled = GUILayout.Toggle(CombatLogRecorder.Enabled, "Enabled", "Button", GUILayout.Width(90));
                if (enabled != CombatLogRecorder.Enabled)
                    CombatLogRecorder.Enabled = enabled;

                GUILayout.Label("Capacity", GUILayout.Width(55));
                int nextCapacity = EditorGUILayout.IntField(_capacity, GUILayout.Width(80));
                if (nextCapacity != _capacity)
                {
                    _capacity = Mathf.Max(1, nextCapacity);
                    CombatLogRecorder.SetCapacity(_capacity);
                }

                if (GUILayout.Button("Clear", GUILayout.Width(80)))
                    CombatLogRecorder.Clear();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Expected Duration", GUILayout.Width(115));
                _expectedDuration = Mathf.Max(0f, EditorGUILayout.FloatField(_expectedDuration, GUILayout.Width(80)));

                if (GUILayout.Button("Export CSV", GUILayout.Width(100)))
                    ExportCsv();

                if (GUILayout.Button("Export Markdown", GUILayout.Width(130)))
                    ExportMarkdown(_expectedDuration);
            }
        }

        private void DrawSummary()
        {
            EditorGUILayout.HelpBox(
                $"Recording: {(CombatLogRecorder.Enabled ? "ON" : "OFF")} | Entries: {CombatLogRecorder.Count} / {CombatLogRecorder.Capacity}\n" +
                "피해가 실제 적용된 CombatResult만 기록됩니다. Guard/Parry/Invincible early-out은 현재 로그 대상이 아닙니다.",
                MessageType.Info);
        }

        private void DrawPreview()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CombatLogEntry entry in CombatLogRecorder.Entries)
            {
                CombatResult result = entry.Result;
                string attacker = result.Attacker != null ? result.Attacker.ActorId : "";
                string victim = result.Victim != null ? result.Victim.ActorId : "";
                EditorGUILayout.LabelField(
                    $"#{entry.Sequence} t={entry.CombatTime:0.###} {attacker} -> {victim} {result.Hit.AnimKey}[{result.Hit.HitPhaseIndex}] " +
                    $"raw={result.Hit.Damage:0.###} final={result.Damage.FinalDamage:0.###} def={result.Defense.Outcome} react={result.Reaction.TargetState}");
            }
            EditorGUILayout.EndScrollView();
        }

        private static void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel(
                "Combat Log CSV 저장",
                Application.dataPath,
                "CombatLog.csv",
                "csv");
            if (string.IsNullOrWhiteSpace(path))
                return;

            File.WriteAllText(path, CombatLogExportUtility.ToCsv(CombatLogRecorder.Entries), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }

        private static void ExportMarkdown(float expectedDuration)
        {
            string path = EditorUtility.SaveFilePanel(
                "Combat Log Markdown 저장",
                Application.dataPath,
                "CombatLogReport.md",
                "md");
            if (string.IsNullOrWhiteSpace(path))
                return;

            File.WriteAllText(path, CombatLogExportUtility.ToMarkdown(CombatLogRecorder.Entries, expectedDuration), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }
    }
}
#endif
