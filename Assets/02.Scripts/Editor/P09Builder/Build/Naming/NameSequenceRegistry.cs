using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    public sealed class NameSequenceRegistry
    {
        public const string SEQUENCE_FILE_PATH = "Library/P09Builder/sequence.json";

        private Dictionary<string, int> _sequences = new();
        private bool _loaded = false;

        public void Load()
        {
            _sequences = new Dictionary<string, int>();
            try
            {
                if (!File.Exists(SEQUENCE_FILE_PATH))
                {
                    _loaded = true;
                    return;
                }

                var json = File.ReadAllText(SEQUENCE_FILE_PATH, Encoding.UTF8);
                ParseJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[P09Builder] Failed to load sequence file: {ex.Message}");
            }
            _loaded = true;
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SEQUENCE_FILE_PATH);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = SerializeJson();
                File.WriteAllText(SEQUENCE_FILE_PATH, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[P09Builder] Failed to save sequence file: {ex.Message}");
            }
        }

        public int NextSequence(string key)
        {
            if (!_loaded) Load();
            if (string.IsNullOrEmpty(key)) return 0;

            if (!_sequences.TryGetValue(key, out var current))
                current = 0;
            current++;
            _sequences[key] = current;
            Save();
            return current;
        }

        public int Peek(string key)
        {
            if (!_loaded) Load();
            if (string.IsNullOrEmpty(key)) return 1;
            if (_sequences.TryGetValue(key, out var v)) return v + 1;
            return 1;
        }

        public void Reset(string key)
        {
            if (!_loaded) Load();
            if (_sequences.ContainsKey(key))
                _sequences.Remove(key);
            Save();
        }

        public void ResetAll()
        {
            _sequences.Clear();
            Save();
        }

        // ---------- 자체 JSON 직렬화 (string→int 맵) ----------
        private void ParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}")) return;
            json = json.Substring(1, json.Length - 2);

            int i = 0;
            while (i < json.Length)
            {
                while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ',')) i++;
                if (i >= json.Length) break;
                if (json[i] != '"') break;
                i++;
                var sbKey = new StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        sbKey.Append(json[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        sbKey.Append(json[i]);
                        i++;
                    }
                }
                if (i >= json.Length) break;
                i++; // closing quote
                while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ':')) i++;

                var sbVal = new StringBuilder();
                while (i < json.Length && json[i] != ',' && json[i] != '}' && !char.IsWhiteSpace(json[i]))
                {
                    sbVal.Append(json[i]);
                    i++;
                }

                if (int.TryParse(sbVal.ToString(), out var num))
                    _sequences[sbKey.ToString()] = num;
            }
        }

        private string SerializeJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kv in _sequences)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"');
                sb.Append(EscapeJson(kv.Key));
                sb.Append("\":");
                sb.Append(kv.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (c == '"' || c == '\\') { sb.Append('\\'); sb.Append(c); }
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
