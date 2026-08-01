#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Group;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// MonsterGroupController의 직렬화 설정을 프리셋에 저장/복원한다.
    /// 필드를 하나씩 미러링하면 컨트롤러가 바뀔 때마다 드리프트가 생기므로 JSON 스냅샷을 쓴다.
    /// </summary>
    /// <remarks>
    /// Unity 내장 필드(m_GameObject, m_Script 등)를 그대로 덮어쓰면 컴포넌트가 다른 오브젝트를
    /// 가리키도록 망가질 수 있으므로, 캡처 단계에서 "m_" 접두 키를 제거하고 저장한다.
    /// FromJsonOverwrite는 없는 키를 무시하므로 복원 시 내장 필드는 건드리지 않는다.
    /// 씬 오브젝트 참조 필드(예: _visibilityCamera)는 이 방식으로 보존되지 않는다.
    /// </remarks>
    public static class MonsterGroupSettingsSnapshot
    {
        public static string Capture(MonsterGroupController group)
        {
            if (group == null)
                return string.Empty;

            return StripBuiltinKeys(EditorJsonUtility.ToJson(group));
        }

        public static void Apply(MonsterGroupController group, string json)
        {
            if (group == null || string.IsNullOrEmpty(json))
                return;

            Undo.RecordObject(group, "Apply Group Preset Settings");
            EditorJsonUtility.FromJsonOverwrite(json, group);
            EditorUtility.SetDirty(group);
        }

        /// <summary>최상위 오브젝트에서 "m_"으로 시작하는 Unity 내장 키를 제거한다.</summary>
        private static string StripBuiltinKeys(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            int bodyStart = json.IndexOf('{');
            if (bodyStart < 0)
                return json;

            var kept = new StringBuilder();
            int i = bodyStart + 1;

            while (i < json.Length)
            {
                // 키 시작 따옴표 탐색
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '}')
                        break;
                    i++;
                }

                if (i >= json.Length || json[i] != '"')
                    break;

                int keyStart = ++i;
                while (i < json.Length && json[i] != '"')
                    i++;

                string key = json.Substring(keyStart, i - keyStart);
                i++; // 닫는 따옴표

                while (i < json.Length && json[i] != ':')
                    i++;
                i++; // ':'

                int valueStart = i;
                int valueEnd = ScanValueEnd(json, valueStart);
                if (valueEnd < 0)
                    break;

                if (!key.StartsWith("m_", System.StringComparison.Ordinal))
                {
                    if (kept.Length > 0)
                        kept.Append(',');

                    kept.Append('"').Append(key).Append("\":").Append(json, valueStart, valueEnd - valueStart);
                }

                i = valueEnd;
                if (i < json.Length && json[i] == ',')
                    i++;
                else
                    break;
            }

            // EditorJsonUtility.ToJson(UnityEngine.Object)는 타입 래퍼 없이 직렬화 필드를
            // 최상위에 둔다. FromJsonOverwrite도 같은 형태를 기대한다.
            return "{" + kept + "}";
        }

        /// <summary>valueStart에서 시작하는 JSON 값의 끝 인덱스(배타)를 반환한다.</summary>
        private static int ScanValueEnd(string json, int valueStart)
        {
            int depth = 0;
            bool inString = false;

            for (int i = valueStart; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    if (c == '\\')
                        i++;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '{':
                    case '[':
                        depth++;
                        break;
                    case '}':
                    case ']':
                        if (depth == 0)
                            return i;
                        depth--;
                        break;
                    case ',':
                        if (depth == 0)
                            return i;
                        break;
                }
            }

            return -1;
        }
    }
}
#endif
