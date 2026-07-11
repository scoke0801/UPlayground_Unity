using UPlayGround.Particle;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation.Editor;

namespace UPlayGround.Editor.VFX
{
    [CustomEditor(typeof(WeaponSlashVfxSpawner))]
    public sealed class WeaponSlashVfxSpawnerEditor : UnityEditor.Editor
    {
        private SerializedProperty bladeBase;
        private SerializedProperty bladeTip;
        private SerializedProperty slashVfxPrefab;
        private SerializedProperty scale;
        private SerializedProperty destroyDelay;
        private SerializedProperty positionOffset;
        private SerializedProperty rotationOffsetEuler;

        private void OnEnable()
        {
            bladeBase = serializedObject.FindProperty("bladeBase");
            bladeTip = serializedObject.FindProperty("bladeTip");
            slashVfxPrefab = serializedObject.FindProperty("slashVfxPrefab");
            scale = serializedObject.FindProperty("scale");
            destroyDelay = serializedObject.FindProperty("destroyDelay");
            positionOffset = serializedObject.FindProperty("positionOffset");
            rotationOffsetEuler = serializedObject.FindProperty("rotationOffsetEuler");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Blade", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bladeBase);
            EditorGUILayout.PropertyField(bladeTip);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(slashVfxPrefab);
            EditorGUILayout.PropertyField(scale);
            EditorGUILayout.PropertyField(destroyDelay);

            EditorGUILayout.Space(6);
            DrawLocalOffsetField();
            DrawRotationOffsetField();

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Position Offset은 Blade Pose 기준 위치 보정입니다. Rotation Offset은 VFX 축 보정입니다. Scene View에서 이동 핸들은 위치, 회전 핸들은 VFX 방향을 직접 조정합니다.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLocalOffsetField()
        {
            Vector3 value = positionOffset.vector3Value;

            EditorGUILayout.LabelField("Position Offset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField("Blade Right / X", value.x);
            value.y = EditorGUILayout.FloatField("Blade Up / Y", value.y);
            value.z = EditorGUILayout.FloatField("Blade Forward / Z", value.z);
            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Offset", GUILayout.Height(22)))
                    value = Vector3.zero;

                if (GUILayout.Button("Frame Blade", GUILayout.Height(22)))
                    FrameBlade();
            }

            positionOffset.vector3Value = value;
        }

        private void DrawRotationOffsetField()
        {
            Vector3 value = rotationOffsetEuler.vector3Value;

            EditorGUILayout.LabelField("Rotation Offset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField("Pitch / X", value.x);
            value.y = EditorGUILayout.FloatField("Yaw / Y", value.y);
            value.z = EditorGUILayout.FloatField("Roll / Z", value.z);
            EditorGUI.indentLevel--;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Rotation", GUILayout.Height(22)))
                    value = Vector3.zero;

                if (GUILayout.Button("Flip Forward", GUILayout.Height(22)))
                    value = MotionEventOffsetFieldUtil.FlipForwardKeepingUp(value);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Roll +90", GUILayout.Height(22)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z + 90f);

                if (GUILayout.Button("Roll -90", GUILayout.Height(22)))
                    value.z = MotionEventOffsetFieldUtil.NormalizeAngle(value.z - 90f);
            }

            rotationOffsetEuler.vector3Value = value;
        }

        private void OnSceneGUI()
        {
            serializedObject.Update();

            Transform baseTransform = bladeBase.objectReferenceValue as Transform;
            Transform tipTransform = bladeTip.objectReferenceValue as Transform;
            if (!TryGetOffsetBasis(baseTransform, tipTransform, out Vector3 center, out Quaternion bladeRotation))
                return;

            Vector3 offset = positionOffset.vector3Value;
            Vector3 spawnPosition = center + bladeRotation * offset;
            Quaternion vfxRotation = bladeRotation * Quaternion.Euler(rotationOffsetEuler.vector3Value);

            Handles.color = Color.cyan;
            Handles.DrawLine(center, spawnPosition);
            Handles.SphereHandleCap(0, center, Quaternion.identity, HandleUtility.GetHandleSize(center) * 0.06f, EventType.Repaint);
            DrawBasis(center, bladeRotation, "Blade", 0.45f);
            DrawBasis(spawnPosition, vfxRotation, "VFX", 0.35f);
            Handles.Label(spawnPosition, "Slash Spawn / Rotation");

            EditorGUI.BeginChangeCheck();
            Vector3 newSpawnPosition = Handles.PositionHandle(spawnPosition, bladeRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Weapon Slash Spawn Offset");
                positionOffset.vector3Value = Quaternion.Inverse(bladeRotation) * (newSpawnPosition - center);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            Quaternion newVfxRotation = Handles.RotationHandle(vfxRotation, spawnPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Rotate Weapon Slash VFX Offset");
                Quaternion localRotation = Quaternion.Inverse(bladeRotation) * newVfxRotation;
                rotationOffsetEuler.vector3Value = NormalizeEuler(localRotation.eulerAngles);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private bool TryGetOffsetBasis(Transform baseTransform, Transform tipTransform, out Vector3 center, out Quaternion rotation)
        {
            center = default;
            rotation = default;

            if (baseTransform == null || tipTransform == null)
                return false;

            Vector3 bladeDirection = tipTransform.position - baseTransform.position;
            if (bladeDirection.sqrMagnitude < 0.0001f)
                return false;

            bladeDirection.Normalize();

            Vector3 upDirection = Vector3.ProjectOnPlane(baseTransform.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.ProjectOnPlane(((WeaponSlashVfxSpawner)target).transform.up, bladeDirection);
            if (upDirection.sqrMagnitude < 0.0001f)
                upDirection = Vector3.up;

            upDirection.Normalize();

            center = Vector3.Lerp(baseTransform.position, tipTransform.position, 0.5f);
            rotation = Quaternion.LookRotation(bladeDirection, upDirection);
            return true;
        }

        private void FrameBlade()
        {
            Transform baseTransform = bladeBase.objectReferenceValue as Transform;
            Transform tipTransform = bladeTip.objectReferenceValue as Transform;
            if (baseTransform == null || tipTransform == null || SceneView.lastActiveSceneView == null)
                return;

            Bounds bounds = new Bounds(baseTransform.position, Vector3.zero);
            bounds.Encapsulate(tipTransform.position);
            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        private static void DrawBasis(Vector3 origin, Quaternion rotation, string label, float size)
        {
            float handleSize = HandleUtility.GetHandleSize(origin) * size;
            Handles.color = Color.red;
            Handles.DrawLine(origin, origin + rotation * Vector3.right * handleSize);
            Handles.color = Color.green;
            Handles.DrawLine(origin, origin + rotation * Vector3.up * handleSize);
            Handles.color = Color.blue;
            Handles.DrawLine(origin, origin + rotation * Vector3.forward * handleSize);
            Handles.Label(origin + rotation * Vector3.up * handleSize, label);
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.x),
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.y),
                MotionEventOffsetFieldUtil.NormalizeAngle(euler.z));
        }
    }
}
