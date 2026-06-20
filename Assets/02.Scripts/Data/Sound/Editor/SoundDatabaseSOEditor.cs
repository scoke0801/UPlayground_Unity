using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Sound;

namespace UPlayGround.Data.Sound.Editor
{
    [CustomEditor(typeof(SoundDatabaseSO))]
    public sealed class SoundDatabaseSOEditor : UnityEditor.Editor
    {
        private readonly List<ValidationMessage> _messages = new();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);

            var database = (SoundDatabaseSO)target;
            Validate(database, _messages);

            DrawSummary(_messages);

            if (GUILayout.Button("Validate Sound Database"))
                LogValidationResult(database, _messages);
        }

        private static void DrawSummary(List<ValidationMessage> messages)
        {
            int errorCount = 0;
            int warningCount = 0;

            foreach (var message in messages)
            {
                if (message.Type == MessageType.Error) errorCount++;
                else if (message.Type == MessageType.Warning) warningCount++;
            }

            if (messages.Count == 0)
            {
                EditorGUILayout.HelpBox("SoundDatabase 검증 통과.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox($"SoundDatabase 검증: Error {errorCount}개 / Warning {warningCount}개", errorCount > 0 ? MessageType.Error : MessageType.Warning);

            foreach (var message in messages)
                EditorGUILayout.HelpBox(message.Text, message.Type);
        }

        private static void LogValidationResult(SoundDatabaseSO database, List<ValidationMessage> messages)
        {
            if (messages.Count == 0)
            {
                Debug.Log($"[SoundDatabase] '{database.name}' Validate: 문제 없음.", database);
                return;
            }

            int errorCount = 0;
            int warningCount = 0;

            foreach (var message in messages)
            {
                if (message.Type == MessageType.Error)
                {
                    errorCount++;
                    Debug.LogError($"[SoundDatabase][Validate] {message.Text}", database);
                }
                else
                {
                    warningCount++;
                    Debug.LogWarning($"[SoundDatabase][Validate] {message.Text}", database);
                }
            }

            Debug.Log($"[SoundDatabase] '{database.name}' Validate 완료: Error {errorCount}개 / Warning {warningCount}개.", database);
        }

        private static void Validate(SoundDatabaseSO database, List<ValidationMessage> messages)
        {
            messages.Clear();

            if (database == null)
                return;

            var entries = database.Entries;
            var keyToIndex = new Dictionary<string, int>();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string label = $"entries[{i}]";

                if (entry == null)
                {
                    messages.Add(Error($"{label}: SoundEntrySO 참조가 비어 있습니다(에셋 미할당)."));
                    continue;
                }

                // key가 비면 런타임에서 에셋 이름을 key로 사용하므로 유효 key 기준으로 검증한다.
                string key = string.IsNullOrWhiteSpace(entry.key) ? entry.name?.Trim() : entry.key.Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    messages.Add(Error($"{label}: key와 에셋 이름이 모두 비어 있습니다."));
                }
                else if (keyToIndex.TryGetValue(key, out int firstIndex))
                {
                    messages.Add(Error($"{label}: key '{key}'가 entries[{firstIndex}]와 중복됩니다."));
                }
                else
                {
                    keyToIndex.Add(key, i);
                }

                if (entry.clip == null)
                    messages.Add(Error($"{label} '{key}': AudioClip이 비어 있습니다."));

                if (entry.distanceMode == SoundDistanceMode.Custom3D &&
                    (entry.customRolloff == null || entry.customRolloff.length == 0))
                {
                    messages.Add(Warning($"{label} '{key}': Custom3D인데 customRolloff가 비어 있습니다. 런타임에서는 기본 Logarithmic에 가까운 동작으로 남을 수 있습니다."));
                }

                if (entry.distanceMode != SoundDistanceMode.None2D)
                {
                    if (entry.minDistance <= 0f)
                        messages.Add(Warning($"{label} '{key}': minDistance는 0보다 커야 합니다."));

                    if (entry.maxDistance <= entry.minDistance)
                        messages.Add(Warning($"{label} '{key}': maxDistance는 minDistance보다 커야 합니다."));
                }

                if (entry.pitchMin <= 0f || entry.pitchMax <= 0f)
                    messages.Add(Warning($"{label} '{key}': pitchMin/pitchMax는 0보다 커야 합니다."));

                if (entry.maxSimultaneous < 0)
                    messages.Add(Warning($"{label} '{key}': maxSimultaneous가 음수입니다. 0은 제한 없음으로 처리됩니다."));

                if (entry.cooldown < 0f)
                    messages.Add(Warning($"{label} '{key}': cooldown이 음수입니다."));

                if (entry.distanceMode == SoundDistanceMode.None2D && entry.preCullByMaxDistance)
                    messages.Add(Warning($"{label} '{key}': 2D 사운드에는 preCullByMaxDistance가 적용되지 않습니다."));
            }
        }

        private static ValidationMessage Error(string text)
            => new(text, MessageType.Error);

        private static ValidationMessage Warning(string text)
            => new(text, MessageType.Warning);

        private readonly struct ValidationMessage
        {
            public readonly string Text;
            public readonly MessageType Type;

            public ValidationMessage(string text, MessageType type)
            {
                Text = text;
                Type = type;
            }
        }
    }
}
