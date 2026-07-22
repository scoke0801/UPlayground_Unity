using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.SOSpreadsheet
{
    internal enum SOExchangeFormat
    {
        Json,
        Csv,
    }

    internal readonly struct SOImportResult
    {
        public readonly int created;
        public readonly int updated;
        public readonly int skipped;
        public readonly List<string> warnings;

        public SOImportResult(int created, int updated, int skipped, List<string> warnings)
        {
            this.created = created;
            this.updated = updated;
            this.skipped = skipped;
            this.warnings = warnings;
        }
    }

    /// <summary>
    /// SO 스프레드시트 JSON/CSV 왕복 계층.
    /// JSON은 타입이 보존된 JToken, CSV는 동일 토큰의 문자열 표현을 사용한다.
    /// ObjectReference는 에셋 GUID를 기준으로 저장해 이름 변경과 경로 이동에 안전하다.
    /// </summary>
    internal static class SODataExchange
    {
        private const string GuidKey = "$guid";
        private const string PathKey = "$path";
        private const string NameKey = "$name";
        private const string TypeKey = "$type";

        public static string BuildExportText(
            TypeEntry typeEntry,
            IReadOnlyList<RowEntry> rows,
            IReadOnlyList<ColumnInfo> columns,
            SOExchangeFormat format)
        {
            var records = BuildRecords(typeEntry, rows, columns);
            return format == SOExchangeFormat.Json
                ? records.ToString(Formatting.Indented)
                : BuildCsv(records, columns);
        }

        public static void WriteExport(string path, string content, SOExchangeFormat format)
        {
            var encoding = format == SOExchangeFormat.Csv
                ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(path, content, encoding);
        }

        public static List<JObject> ReadRecords(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (extension == ".json")
            {
                var token = JToken.Parse(text);
                if (token is not JArray array)
                    throw new InvalidDataException("JSON 최상위 값은 레코드 배열이어야 합니다.");
                return array.OfType<JObject>().ToList();
            }
            if (extension == ".csv")
                return ParseCsv(text);
            throw new InvalidDataException(".json 또는 .csv 파일만 불러올 수 있습니다.");
        }

        public static (int creates, int updates, int mappedFields, int unmappedFields) Analyze(
            TypeEntry typeEntry,
            IReadOnlyList<RowEntry> existingRows,
            IReadOnlyList<ColumnInfo> columns,
            IReadOnlyList<JObject> records)
        {
            var map = BuildColumnMap(columns);
            int creates = 0;
            int updates = 0;
            int mapped = 0;
            int unmapped = 0;
            foreach (var record in records)
            {
                if (FindExisting(typeEntry, existingRows, record) != null)
                    updates++;
                else
                    creates++;

                foreach (var property in record.Properties())
                {
                    if (IsMetadata(property.Name))
                        continue;
                    if (ResolveColumn(map, property.Name) != null)
                        mapped++;
                    else
                        unmapped++;
                }
            }
            return (creates, updates, mapped, unmapped);
        }

        public static SOImportResult Import(
            TypeEntry typeEntry,
            IReadOnlyList<RowEntry> existingRows,
            IReadOnlyList<ColumnInfo> columns,
            IReadOnlyList<JObject> records,
            string fallbackFolder)
        {
            var warnings = new List<string>();
            var columnMap = BuildColumnMap(columns);
            int created = 0;
            int updated = 0;
            int skipped = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"{typeEntry.type.Name} 데이터 불러오기");
            bool completed = false;

            try
            {
                foreach (var record in records)
                {
                    string sourceType = record.Value<string>(TypeKey);
                    if (!string.IsNullOrEmpty(sourceType) && sourceType != typeEntry.type.FullName &&
                        sourceType != typeEntry.type.AssemblyQualifiedName)
                    {
                        warnings.Add($"{RecordLabel(record)}: 타입이 다릅니다 ({sourceType}).");
                        skipped++;
                        continue;
                    }

                    var row = FindExisting(typeEntry, existingRows, record);
                    ScriptableObject asset;
                    if (row != null)
                    {
                        asset = row.asset != null
                            ? row.asset
                            : AssetDatabase.LoadAssetAtPath<ScriptableObject>(row.path);
                        if (asset == null)
                        {
                            warnings.Add($"{RecordLabel(record)}: 기존 에셋을 로드하지 못했습니다.");
                            skipped++;
                            continue;
                        }
                        Undo.RecordObject(asset, "SO 데이터 불러오기");
                        updated++;
                    }
                    else
                    {
                        asset = ScriptableObject.CreateInstance(typeEntry.type);
                        string newPath = ResolveNewAssetPath(record, fallbackFolder, typeEntry.type.Name);
                        AssetDatabase.CreateAsset(asset, newPath);
                        Undo.RegisterCreatedObjectUndo(asset, "SO 데이터 불러오기");
                        created++;
                    }

                    var serialized = new SerializedObject(asset);
                    serialized.Update();
                    foreach (var source in record.Properties())
                    {
                        if (IsMetadata(source.Name))
                            continue;
                        var column = ResolveColumn(columnMap, source.Name);
                        if (column == null)
                            continue;
                        var target = serialized.FindProperty(column.propertyPath);
                        if (target == null)
                        {
                            warnings.Add($"{asset.name}.{column.propertyPath}: 대상 필드를 찾지 못했습니다.");
                            continue;
                        }
                        if (!TrySetValue(target, source.Value, warnings, $"{asset.name}.{column.propertyPath}"))
                            continue;
                    }
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                }

                AssetDatabase.SaveAssets();
                completed = true;
            }
            catch
            {
                try
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                    AssetDatabase.SaveAssets();
                }
                catch (Exception rollbackException)
                {
                    Debug.LogException(rollbackException);
                }
                throw;
            }
            finally
            {
                if (completed)
                    Undo.CollapseUndoOperations(undoGroup);
            }

            return new SOImportResult(created, updated, skipped, warnings);
        }

        private static JArray BuildRecords(
            TypeEntry typeEntry,
            IReadOnlyList<RowEntry> rows,
            IReadOnlyList<ColumnInfo> columns)
        {
            var result = new JArray();
            foreach (var row in rows)
            {
                var serialized = row.GetSerialized();
                if (serialized == null)
                    continue;
                serialized.UpdateIfRequiredOrScript();
                string path = AssetDatabase.GetAssetPath(serialized.targetObject);
                var record = new JObject
                {
                    [GuidKey] = AssetDatabase.AssetPathToGUID(path),
                    [PathKey] = path,
                    [NameKey] = serialized.targetObject.name,
                    [TypeKey] = typeEntry.type.FullName,
                };
                foreach (var column in columns)
                {
                    var property = row.GetProperty(column.propertyPath);
                    if (property != null)
                        record[column.propertyPath] = ToToken(property);
                }
                result.Add(record);
            }
            return result;
        }

        private static JToken ToToken(SerializedProperty property)
        {
            if (property == null)
                return JValue.CreateNull();
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                var array = new JArray();
                for (int i = 0; i < property.arraySize; i++)
                    array.Add(ToToken(property.GetArrayElementAtIndex(i)));
                return array;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    return new JValue(property.longValue);
                case SerializedPropertyType.Boolean:
                    return new JValue(property.boolValue);
                case SerializedPropertyType.Float:
                    return new JValue(property.doubleValue);
                case SerializedPropertyType.String:
                    return new JValue(property.stringValue ?? string.Empty);
                case SerializedPropertyType.Enum:
                {
                    int index = property.enumValueIndex;
                    return new JValue(index >= 0 && index < property.enumNames.Length
                        ? property.enumNames[index]
                        : property.intValue.ToString(CultureInfo.InvariantCulture));
                }
                case SerializedPropertyType.ObjectReference:
                {
                    var value = property.objectReferenceValue;
                    if (value == null)
                        return JValue.CreateNull();
                    string path = AssetDatabase.GetAssetPath(value);
                    return new JObject
                    {
                        [GuidKey] = AssetDatabase.AssetPathToGUID(path),
                        [PathKey] = path,
                        [NameKey] = value.name,
                        [TypeKey] = value.GetType().AssemblyQualifiedName,
                    };
                }
                case SerializedPropertyType.Color:
                    return new JValue("#" + ColorUtility.ToHtmlStringRGBA(property.colorValue));
                case SerializedPropertyType.Vector2:
                    return VectorToken(property.vector2Value.x, property.vector2Value.y);
                case SerializedPropertyType.Vector3:
                    return VectorToken(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                case SerializedPropertyType.Vector4:
                    return VectorToken(property.vector4Value.x, property.vector4Value.y,
                        property.vector4Value.z, property.vector4Value.w);
                case SerializedPropertyType.Vector2Int:
                    return VectorToken(property.vector2IntValue.x, property.vector2IntValue.y);
                case SerializedPropertyType.Vector3Int:
                    return VectorToken(property.vector3IntValue.x, property.vector3IntValue.y,
                        property.vector3IntValue.z);
                case SerializedPropertyType.Quaternion:
                {
                    var q = property.quaternionValue;
                    return VectorToken(q.x, q.y, q.z, q.w);
                }
                case SerializedPropertyType.Rect:
                {
                    var r = property.rectValue;
                    return new JObject { ["x"] = r.x, ["y"] = r.y, ["width"] = r.width, ["height"] = r.height };
                }
                case SerializedPropertyType.Bounds:
                {
                    var b = property.boundsValue;
                    return new JObject { ["center"] = VectorToken(b.center.x, b.center.y, b.center.z),
                        ["size"] = VectorToken(b.size.x, b.size.y, b.size.z) };
                }
                case SerializedPropertyType.Generic:
                {
                    var obj = new JObject();
                    foreach (var child in DirectChildren(property))
                        obj[child.name] = ToToken(child);
                    return obj;
                }
                default:
                    return new JValue(SOSpreadsheetModel.GetValueText(
                        new ColumnInfo { propType = property.propertyType }, property));
            }
        }

        private static bool TrySetValue(
            SerializedProperty property, JToken token, List<string> warnings, string label)
        {
            try
            {
                if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    var array = CoerceArray(token);
                    if (array == null)
                        return Warn(warnings, label, "배열 값이 아닙니다.");
                    property.arraySize = array.Count;
                    for (int i = 0; i < array.Count; i++)
                        TrySetValue(property.GetArrayElementAtIndex(i), array[i], warnings, $"{label}[{i}]");
                    return true;
                }

                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.Character:
                        property.longValue = CoerceLong(token);
                        return true;
                    case SerializedPropertyType.Boolean:
                        property.boolValue = CoerceBool(token);
                        return true;
                    case SerializedPropertyType.Float:
                        property.doubleValue = CoerceDouble(token);
                        return true;
                    case SerializedPropertyType.String:
                        property.stringValue = token.Type == JTokenType.Null ? string.Empty : token.ToString();
                        return true;
                    case SerializedPropertyType.Enum:
                        return SetEnum(property, token, warnings, label);
                    case SerializedPropertyType.ObjectReference:
                        property.objectReferenceValue = ResolveReference(token);
                        if (token.Type != JTokenType.Null && property.objectReferenceValue == null)
                            return Warn(warnings, label, $"에셋 참조를 찾지 못했습니다 ({token}).");
                        return true;
                    case SerializedPropertyType.Color:
                        if (ColorUtility.TryParseHtmlString(token.ToString(), out Color color))
                        {
                            property.colorValue = color;
                            return true;
                        }
                        return Warn(warnings, label, "색상은 #RRGGBB 또는 #RRGGBBAA 형식이어야 합니다.");
                    case SerializedPropertyType.Vector2:
                    {
                        var v = Components(token, 2);
                        property.vector2Value = new Vector2(v[0], v[1]);
                        return true;
                    }
                    case SerializedPropertyType.Vector3:
                    {
                        var v = Components(token, 3);
                        property.vector3Value = new Vector3(v[0], v[1], v[2]);
                        return true;
                    }
                    case SerializedPropertyType.Vector4:
                    {
                        var v = Components(token, 4);
                        property.vector4Value = new Vector4(v[0], v[1], v[2], v[3]);
                        return true;
                    }
                    case SerializedPropertyType.Vector2Int:
                    {
                        var v = Components(token, 2);
                        property.vector2IntValue = new Vector2Int(Mathf.RoundToInt(v[0]), Mathf.RoundToInt(v[1]));
                        return true;
                    }
                    case SerializedPropertyType.Vector3Int:
                    {
                        var v = Components(token, 3);
                        property.vector3IntValue = new Vector3Int(Mathf.RoundToInt(v[0]), Mathf.RoundToInt(v[1]),
                            Mathf.RoundToInt(v[2]));
                        return true;
                    }
                    case SerializedPropertyType.Quaternion:
                    {
                        var v = Components(token, 4);
                        property.quaternionValue = new Quaternion(v[0], v[1], v[2], v[3]);
                        return true;
                    }
                    case SerializedPropertyType.Rect:
                    {
                        var o = CoerceObject(token);
                        property.rectValue = new Rect((float)o.Value<double>("x"), (float)o.Value<double>("y"),
                            (float)o.Value<double>("width"), (float)o.Value<double>("height"));
                        return true;
                    }
                    case SerializedPropertyType.Bounds:
                    {
                        var o = CoerceObject(token);
                        var center = Components(o["center"], 3);
                        var size = Components(o["size"], 3);
                        property.boundsValue = new Bounds(new Vector3(center[0], center[1], center[2]),
                            new Vector3(size[0], size[1], size[2]));
                        return true;
                    }
                    case SerializedPropertyType.Generic:
                    {
                        var obj = CoerceObject(token);
                        foreach (var child in DirectChildren(property))
                        {
                            if (obj.TryGetValue(child.name, StringComparison.OrdinalIgnoreCase, out JToken value))
                                TrySetValue(child, value, warnings, $"{label}.{child.name}");
                        }
                        return true;
                    }
                    default:
                        return Warn(warnings, label, $"지원하지 않는 타입입니다 ({property.propertyType}).");
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException ||
                                       ex is OverflowException || ex is ArgumentException)
            {
                return Warn(warnings, label, ex.Message);
            }
        }

        private static RowEntry FindExisting(
            TypeEntry typeEntry, IReadOnlyList<RowEntry> rows, JObject record)
        {
            string guid = record.Value<string>(GuidKey);
            if (!string.IsNullOrEmpty(guid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(guidPath))
                {
                    throw new InvalidDataException(
                        $"{RecordLabel(record)}: GUID에 해당하는 에셋을 찾지 못했습니다 ({guid}).");
                }
                var byGuid = rows.FirstOrDefault(r => r.path == guidPath);
                if (byGuid != null)
                    return byGuid;
                throw new InvalidDataException(
                    $"{RecordLabel(record)}: GUID 에셋이 현재 편집 대상에 포함되지 않습니다 ({guidPath}).");
            }
            string path = record.Value<string>(PathKey);
            if (!string.IsNullOrEmpty(path))
            {
                var byPath = rows.FirstOrDefault(r => r.path == path);
                if (byPath != null)
                    return byPath;
                throw new InvalidDataException(
                    $"{RecordLabel(record)}: 경로에 해당하는 편집 대상 에셋을 찾지 못했습니다 ({path}).");
            }
            string name = record.Value<string>(NameKey);
            if (string.IsNullOrEmpty(name))
                return null;

            RowEntry[] nameMatches = rows
                .Where(r => string.Equals(r.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (nameMatches.Length > 1)
            {
                throw new InvalidDataException(
                    $"{RecordLabel(record)}: GUID와 경로가 일치하지 않고 같은 이름의 에셋이 여러 개입니다.");
            }
            return nameMatches.Length == 1 ? nameMatches[0] : null;
        }

        private static string ResolveNewAssetPath(JObject record, string fallbackFolder, string typeName)
        {
            string sourcePath = record.Value<string>(PathKey);
            if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith("Assets/", StringComparison.Ordinal) &&
                sourcePath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                string sourceDir = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(sourceDir) && AssetDatabase.IsValidFolder(sourceDir) &&
                    AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
                    return sourcePath;
            }

            string folder = AssetDatabase.IsValidFolder(fallbackFolder) ? fallbackFolder : "Assets";
            string name = SanitizeFileName(record.Value<string>(NameKey));
            if (string.IsNullOrEmpty(name))
                name = $"New {typeName}";
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
        }

        private static UnityEngine.Object ResolveReference(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || string.IsNullOrWhiteSpace(token.ToString()))
                return null;
            string guid = token is JObject obj ? obj.Value<string>(GuidKey) : token.ToString();
            if (!string.IsNullOrEmpty(guid))
            {
                string guidPath = AssetDatabase.GUIDToAssetPath(guid);
                return string.IsNullOrEmpty(guidPath)
                    ? null
                    : AssetDatabase.LoadMainAssetAtPath(guidPath);
            }

            if (token is JObject objectToken)
            {
                string objectPath = objectToken.Value<string>(PathKey);
                if (!string.IsNullOrEmpty(objectPath))
                    return AssetDatabase.LoadMainAssetAtPath(objectPath);
            }

            string name = token is JObject named ? named.Value<string>(NameKey) : token.ToString();
            if (string.IsNullOrEmpty(name))
                return null;

            Type expectedType = null;
            if (token is JObject typed)
            {
                string typeName = typed.Value<string>(TypeKey);
                if (!string.IsNullOrEmpty(typeName))
                    expectedType = Type.GetType(typeName);
            }

            UnityEngine.Object uniqueMatch = null;
            foreach (string candidateGuid in AssetDatabase.FindAssets(name))
            {
                UnityEngine.Object candidate = AssetDatabase.LoadMainAssetAtPath(
                    AssetDatabase.GUIDToAssetPath(candidateGuid));
                if (candidate == null
                    || !string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase)
                    || (expectedType != null && !expectedType.IsInstanceOfType(candidate)))
                {
                    continue;
                }

                if (uniqueMatch != null && uniqueMatch != candidate)
                    return null;
                uniqueMatch = candidate;
            }
            return uniqueMatch;
        }

        private static bool SetEnum(
            SerializedProperty property, JToken token, List<string> warnings, string label)
        {
            string value = token.ToString();
            for (int i = 0; i < property.enumNames.Length; i++)
            {
                if (!string.Equals(property.enumNames[i], value, StringComparison.OrdinalIgnoreCase) &&
                    (i >= property.enumDisplayNames.Length ||
                     !string.Equals(property.enumDisplayNames[i], value, StringComparison.OrdinalIgnoreCase)))
                    continue;
                property.enumValueIndex = i;
                return true;
            }
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int raw))
            {
                property.intValue = raw;
                return true;
            }
            return Warn(warnings, label, $"enum 값 '{value}'을 찾지 못했습니다.");
        }

        private static Dictionary<string, ColumnInfo> BuildColumnMap(IReadOnlyList<ColumnInfo> columns)
        {
            var map = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                map.TryAdd(column.propertyPath, column);
                map.TryAdd(column.displayName, column);
                map.TryAdd(Normalize(column.propertyPath), column);
                map.TryAdd(Normalize(column.displayName), column);
            }
            return map;
        }

        private static ColumnInfo ResolveColumn(Dictionary<string, ColumnInfo> map, string sourceName)
        {
            if (map.TryGetValue(sourceName, out var exact))
                return exact;
            map.TryGetValue(Normalize(sourceName), out var normalized);
            return normalized;
        }

        private static string BuildCsv(JArray records, IReadOnlyList<ColumnInfo> columns)
        {
            var headers = new List<string> { GuidKey, PathKey, NameKey, TypeKey };
            headers.AddRange(columns.Select(c => c.propertyPath));
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (JObject record in records)
            {
                var values = new List<string>(headers.Count);
                foreach (string header in headers)
                    values.Add(TokenToCsv(record[header]));
                builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
            }
            return builder.ToString();
        }

        private static List<JObject> ParseCsv(string text)
        {
            var rows = ParseCsvRows(text);
            if (rows.Count == 0)
                return new List<JObject>();
            var headers = rows[0].Select(h => h.Trim().TrimStart('\uFEFF')).ToList();
            var records = new List<JObject>();
            for (int r = 1; r < rows.Count; r++)
            {
                if (rows[r].All(string.IsNullOrEmpty))
                    continue;
                var record = new JObject();
                for (int c = 0; c < headers.Count; c++)
                {
                    string value = c < rows[r].Count ? rows[r][c] : string.Empty;
                    record[headers[c]] = CsvToToken(value);
                }
                records.Add(record);
            }
            return records;
        }

        private static List<List<string>> ParseCsvRows(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (ch == '"')
                        quoted = false;
                    else
                        field.Append(ch);
                    continue;
                }
                if (ch == '"')
                    quoted = true;
                else if (ch == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (ch == '\n')
                {
                    row.Add(field.ToString().TrimEnd('\r'));
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                }
                else
                    field.Append(ch);
            }
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString().TrimEnd('\r'));
                rows.Add(row);
            }
            return rows;
        }

        private static string TokenToCsv(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return string.Empty;
            if (token is JObject reference && reference.ContainsKey(GuidKey) && reference.Count <= 3)
                return reference.Value<string>(GuidKey) ?? string.Empty;
            return token is JValue value
                ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty
                : token.ToString(Formatting.None);
        }

        private static JToken CsvToToken(string value)
        {
            string trimmed = value.Trim();
            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                try { return JToken.Parse(trimmed); }
                catch (JsonReaderException) { }
            }
            return new JValue(value);
        }

        private static string EscapeCsv(string value)
        {
            value ??= string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static IEnumerable<SerializedProperty> DirectChildren(SerializedProperty parent)
        {
            var iterator = parent.Copy();
            var end = parent.GetEndProperty();
            int targetDepth = parent.depth + 1;
            if (!iterator.NextVisible(true))
                yield break;
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                if (iterator.depth == targetDepth)
                    yield return iterator.Copy();
                if (!iterator.NextVisible(false))
                    yield break;
            }
        }

        private static JObject VectorToken(params float[] values)
        {
            string[] names = { "x", "y", "z", "w" };
            var result = new JObject();
            for (int i = 0; i < values.Length; i++)
                result[names[i]] = values[i];
            return result;
        }

        private static JObject VectorToken(params int[] values)
        {
            string[] names = { "x", "y", "z", "w" };
            var result = new JObject();
            for (int i = 0; i < values.Length; i++)
                result[names[i]] = values[i];
            return result;
        }

        private static float[] Components(JToken token, int count)
        {
            var obj = CoerceObject(token);
            string[] names = { "x", "y", "z", "w" };
            var values = new float[count];
            for (int i = 0; i < count; i++)
                values[i] = (float)CoerceDouble(obj[names[i]]);
            return values;
        }

        private static JObject CoerceObject(JToken token)
        {
            if (token is JObject obj)
                return obj;
            if (token?.Type == JTokenType.String)
                return JObject.Parse(token.ToString());
            throw new InvalidCastException("오브젝트 형식이 아닙니다.");
        }

        private static JArray CoerceArray(JToken token)
        {
            if (token is JArray array)
                return array;
            if (token?.Type == JTokenType.String)
            {
                string text = token.ToString();
                if (text.StartsWith("[", StringComparison.Ordinal))
                    return JArray.Parse(text);
            }
            return null;
        }

        private static long CoerceLong(JToken token)
        {
            if (token.Type == JTokenType.Integer)
                return token.Value<long>();
            return long.Parse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static double CoerceDouble(JToken token)
        {
            if (token == null)
                throw new FormatException("숫자 값이 없습니다.");
            if (token.Type is JTokenType.Integer or JTokenType.Float)
                return token.Value<double>();
            return double.Parse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static bool CoerceBool(JToken token)
        {
            if (token.Type == JTokenType.Boolean)
                return token.Value<bool>();
            string value = token.ToString().Trim();
            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "y", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "n", StringComparison.OrdinalIgnoreCase))
                return false;
            return bool.Parse(value);
        }

        private static bool Warn(List<string> warnings, string label, string message)
        {
            warnings.Add($"{label}: {message}");
            return false;
        }

        private static bool IsMetadata(string name) => name.StartsWith("$", StringComparison.Ordinal);

        private static string RecordLabel(JObject record) =>
            record.Value<string>(NameKey) ?? record.Value<string>(PathKey) ?? "이름 없는 레코드";

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c != ' ' && c != '_' && c != '-')
                    builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Trim();
        }
    }
}
