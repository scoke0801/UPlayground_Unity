#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    [Serializable]
    public class BehaviorTreeJsonData
    {
        public string rootGuid;
        public List<BlackboardEntryJson> blackboard = new();
        public List<BehaviorTreeNodeJson> nodes = new();
    }

    [Serializable]
    public class BlackboardEntryJson
    {
        public string key;
        public BlackboardValueType valueType;
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Vector3 vector3Value;
        public string objectAssetPath;
    }

    [Serializable]
    public class BehaviorTreeNodeJson
    {
        public string type;
        public string guid;
        public string displayName;
        public string comment;
        public Vector2 position;
        public List<string> children = new();
        public List<BehaviorTreeNodePropertyJson> properties = new();
    }

    [Serializable]
    public class BehaviorTreeNodePropertyJson
    {
        public string name;
        public string type;
        public string value;
    }

    public static class BehaviorTreeJsonUtility
    {
        private static readonly Type[] SupportedPropertyTypes =
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(BTAbortType),
            typeof(BlackboardValueType),
            typeof(FloatComparisonType),
            typeof(EnemyTransitionStateType)
        };

        [MenuItem("UPlayGround/AI/Behavior Tree Json/Export Selected")]
        public static void ExportSelected()
        {
            if (Selection.activeObject is not BehaviorTreeAsset tree)
            {
                EditorUtility.DisplayDialog("BT Json Export", "BehaviorTreeAsset을 선택하세요.", "확인");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Behavior Tree Json Export",
                Application.dataPath,
                tree.name + ".json",
                "json");

            if (string.IsNullOrWhiteSpace(path))
                return;

            ExportToJsonFile(tree, path);
        }

        [MenuItem("UPlayGround/AI/Behavior Tree Json/Import Json")]
        public static void ImportJson()
        {
            var jsonPath = EditorUtility.OpenFilePanel("Behavior Tree Json Import", Application.dataPath, "json");
            if (string.IsNullOrWhiteSpace(jsonPath))
                return;

            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Behavior Tree Asset 저장",
                Path.GetFileNameWithoutExtension(jsonPath),
                "asset",
                "JSON에서 생성할 BehaviorTreeAsset 저장 위치를 선택하세요.",
                "Assets/10.Datas/AI/BehaviorTree");

            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            var tree = ImportFromJsonFile(jsonPath, assetPath);
            EditorGUIUtility.PingObject(tree);
            BehaviorTreeEditorWindow.Open(tree);
        }

        public static void ExportToJsonFile(BehaviorTreeAsset tree, string absolutePath)
        {
            var data = ExportToData(tree);
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(absolutePath, json);
            AssetDatabase.Refresh();
            Debug.Log($"[BT] Json Export 완료: {absolutePath}");
        }

        public static BehaviorTreeAsset ImportFromJsonFile(string absoluteJsonPath, string assetPath)
        {
            var json = File.ReadAllText(absoluteJsonPath);
            var data = JsonUtility.FromJson<BehaviorTreeJsonData>(json);
            return ImportFromData(data, assetPath);
        }

        public static BehaviorTreeJsonData ExportToData(BehaviorTreeAsset tree)
        {
            var data = new BehaviorTreeJsonData
            {
                rootGuid = tree.RootNode != null ? tree.RootNode.Guid : ""
            };

            foreach (var entry in tree.Blackboard.Entries)
            {
                data.blackboard.Add(new BlackboardEntryJson
                {
                    key = entry.Key,
                    valueType = entry.ValueType,
                    boolValue = entry.BoolValue,
                    intValue = entry.IntValue,
                    floatValue = entry.FloatValue,
                    stringValue = entry.StringValue,
                    vector3Value = entry.Vector3Value,
                    objectAssetPath = entry.ObjectValue != null ? AssetDatabase.GetAssetPath(entry.ObjectValue) : ""
                });
            }

            foreach (var node in tree.Nodes.Where(node => node != null))
            {
                data.nodes.Add(new BehaviorTreeNodeJson
                {
                    type = node.GetType().AssemblyQualifiedName,
                    guid = node.Guid,
                    displayName = node.DisplayName,
                    comment = node.Comment,
                    position = node.EditorPosition,
                    children = node.Children.Where(child => child != null).Select(child => child.Guid).ToList(),
                    properties = ExportNodeProperties(node)
                });
            }

            return data;
        }

        public static BehaviorTreeAsset ImportFromData(BehaviorTreeJsonData data, string assetPath)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            EnsureAssetDirectory(assetPath);

            var tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            AssetDatabase.CreateAsset(tree, assetPath);

            var nodeMap = new Dictionary<string, BTNode>();
            foreach (var nodeJson in data.nodes)
            {
                var type = Type.GetType(nodeJson.type);
                if (type == null || !typeof(BTNode).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"[BT] Json Import: 노드 타입을 찾을 수 없습니다. {nodeJson.type}");
                    continue;
                }

                var node = ScriptableObject.CreateInstance(type) as BTNode;
                if (node == null)
                    continue;

                node.name = nodeJson.displayName;
                node.Guid = nodeJson.guid;
                node.DisplayName = nodeJson.displayName;
                node.Comment = nodeJson.comment;
                node.EditorPosition = nodeJson.position;
                ApplyNodeProperties(node, nodeJson.properties);

                AssetDatabase.AddObjectToAsset(node, tree);
                tree.Nodes.Add(node);
                nodeMap[node.Guid] = node;
            }

            foreach (var nodeJson in data.nodes)
            {
                if (!nodeMap.TryGetValue(nodeJson.guid, out var node))
                    continue;

                node.Children.Clear();
                foreach (var childGuid in nodeJson.children)
                {
                    if (nodeMap.TryGetValue(childGuid, out var child))
                        node.Children.Add(child);
                }
            }

            if (!string.IsNullOrWhiteSpace(data.rootGuid) && nodeMap.TryGetValue(data.rootGuid, out var root))
                tree.RootNode = root;

            foreach (var entry in data.blackboard)
            {
                tree.Blackboard.AddEntry(entry.key, entry.valueType);
                switch (entry.valueType)
                {
                    case BlackboardValueType.Bool:
                        tree.Blackboard.SetBool(entry.key, entry.boolValue);
                        break;
                    case BlackboardValueType.Int:
                        tree.Blackboard.SetInt(entry.key, entry.intValue);
                        break;
                    case BlackboardValueType.Float:
                        tree.Blackboard.SetFloat(entry.key, entry.floatValue);
                        break;
                    case BlackboardValueType.String:
                        tree.Blackboard.SetString(entry.key, entry.stringValue);
                        break;
                    case BlackboardValueType.Vector3:
                        tree.Blackboard.SetVector3(entry.key, entry.vector3Value);
                        break;
                    case BlackboardValueType.Object:
                        var obj = string.IsNullOrWhiteSpace(entry.objectAssetPath)
                            ? null
                            : AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.objectAssetPath);
                        tree.Blackboard.SetObject(entry.key, obj);
                        break;
                }
            }

            EditorUtility.SetDirty(tree);
            foreach (var node in tree.Nodes)
                EditorUtility.SetDirty(node);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BT] Json Import 완료: {assetPath}");
            return tree;
        }

        private static List<BehaviorTreeNodePropertyJson> ExportNodeProperties(BTNode node)
        {
            var result = new List<BehaviorTreeNodePropertyJson>();
            foreach (var field in GetSerializableNodeFields(node.GetType()))
            {
                var value = field.GetValue(node);
                result.Add(new BehaviorTreeNodePropertyJson
                {
                    name = field.Name,
                    type = field.FieldType.AssemblyQualifiedName,
                    value = SerializeValue(field.FieldType, value)
                });
            }

            return result;
        }

        private static void ApplyNodeProperties(BTNode node, List<BehaviorTreeNodePropertyJson> properties)
        {
            if (properties == null)
                return;

            var fields = GetSerializableNodeFields(node.GetType()).ToDictionary(field => field.Name);
            foreach (var property in properties)
            {
                if (!fields.TryGetValue(property.name, out var field))
                    continue;

                field.SetValue(node, DeserializeValue(field.FieldType, property.value));
            }
        }

        private static IEnumerable<FieldInfo> GetSerializableNodeFields(Type type)
        {
            while (type != null && type != typeof(BTNode))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (field.IsNotSerialized)
                        continue;

                    var isSerialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                    if (isSerialized && SupportedPropertyTypes.Contains(field.FieldType))
                        yield return field;
                }

                type = type.BaseType;
            }
        }

        private static string SerializeValue(Type type, object value)
        {
            if (type == typeof(Vector2))
                return JsonUtility.ToJson(new Vector2Wrapper { value = (Vector2)value });
            if (type == typeof(Vector3))
                return JsonUtility.ToJson(new Vector3Wrapper { value = (Vector3)value });
            if (type.IsEnum)
                return value.ToString();
            if (type == typeof(float))
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static object DeserializeValue(Type type, string value)
        {
            if (type == typeof(bool))
                return bool.TryParse(value, out var boolValue) && boolValue;
            if (type == typeof(int))
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue) ? intValue : 0;
            if (type == typeof(float))
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0f;
            if (type == typeof(string))
                return value;
            if (type == typeof(Vector2))
                return JsonUtility.FromJson<Vector2Wrapper>(value).value;
            if (type == typeof(Vector3))
                return JsonUtility.FromJson<Vector3Wrapper>(value).value;
            if (type.IsEnum)
                return Enum.Parse(type, value);
            return null;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(directory) || AssetDatabase.IsValidFolder(directory))
                return;

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        [Serializable]
        private struct Vector2Wrapper
        {
            public Vector2 value;
        }

        [Serializable]
        private struct Vector3Wrapper
        {
            public Vector3 value;
        }
    }
}
#endif
