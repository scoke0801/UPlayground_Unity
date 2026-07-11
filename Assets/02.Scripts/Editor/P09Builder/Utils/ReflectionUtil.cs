using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public static class ReflectionUtil
    {
        public static bool SetField(Object obj, string fieldName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName))
                return false;

            var so = new SerializedObject(obj);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[P09Builder] Field '{fieldName}' not found on {obj.GetType().Name}");
                return false;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = value as Object;
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = System.Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.LayerMask:
                    prop.intValue = value is LayerMask mask
                        ? mask.value
                        : System.Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Enum:
                    // intValue 사용: Flags enum과 일반 enum 모두에서 안전하게 동작
                    prop.intValue = System.Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = System.Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = System.Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value as string ?? string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    if (value is Color c) prop.colorValue = c;
                    break;
                case SerializedPropertyType.Vector3:
                    if (value is Vector3 v3) prop.vector3Value = v3;
                    break;
                case SerializedPropertyType.Vector2:
                    if (value is Vector2 v2) prop.vector2Value = v2;
                    break;
                default:
                    Debug.LogWarning($"[P09Builder] Unsupported property type {prop.propertyType} for field '{fieldName}'");
                    return false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        public static SerializedProperty FindProperty(Object obj, string fieldName, out SerializedObject serializedObject)
        {
            serializedObject = null;
            if (obj == null || string.IsNullOrEmpty(fieldName))
                return null;
            serializedObject = new SerializedObject(obj);
            return serializedObject.FindProperty(fieldName);
        }
    }

    internal static class LayerAssignmentUtil
    {
        private const string DefaultLayerName = "Default";

        public static void ApplyActorLayer(GameObject root, string layerName)
        {
            if (root == null || string.IsNullOrEmpty(layerName)) return;

            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[P09Builder] Layer를 찾지 못했습니다: {layerName}");
                return;
            }

            var defaultLayer = LayerMask.NameToLayer(DefaultLayerName);
            var transforms = root.GetComponentsInChildren<Transform>(true);
            Undo.RecordObjects(transforms, "Apply Actor Layer");

            root.layer = layer;
            foreach (var transform in transforms)
            {
                var go = transform.gameObject;
                if (go == root || go.layer == defaultLayer)
                    go.layer = layer;
            }

            EditorUtility.SetDirty(root);
        }
    }
}
