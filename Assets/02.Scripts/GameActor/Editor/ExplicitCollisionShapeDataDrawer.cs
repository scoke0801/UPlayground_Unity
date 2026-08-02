using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Combat;
using UPlayGround.Data.Event;

namespace UPlayGround.Editor
{
    /// <summary>
    /// Collision Event의 명시적 판정 범위 인스펙터.
    /// Shape/Anchor 종류에 따라 의미 있는 필드만 표시하고, 값 오류를 인라인으로 보고한다.
    ///
    /// MotionSet 에디터는 UIToolkit이므로 <see cref="CreatePropertyGUI"/> 경로가 사용된다.
    /// 이 경로는 표준 <see cref="Foldout"/>을 반환해 '시간 링크' 같은 다른 중첩 필드와 접기 UI가 동일하다.
    /// <see cref="OnGUI"/>는 IMGUI 인스펙터(궁극기 타임라인 등)용 폴백이다.
    /// </summary>
    [CustomPropertyDrawer(typeof(ExplicitCollisionShapeData))]
    public sealed class ExplicitCollisionShapeDataDrawer : PropertyDrawer
    {
        private const string LabelShapeType = "판정 형상";
        private const string LabelAnchor = "기준 위치(Anchor)";
        private const string LabelSampling = "기준 추적 방식";
        private const string LabelEvaluation = "판정 방식";
        private const string LabelDirection = "공격 방향";
        private const string LabelWorldPosition = "월드 좌표";
        private const string LabelLocalOffset = "중심 오프셋";
        private const string LabelRotation = "회전 (도)";
        private const string LabelRadius = "반지름 (m)";
        private const string LabelBoxSize = "박스 크기 (가로·높이·세로)";
        private const string LabelCapsuleHeight = "전체 높이 (m)";

        // ================================================================
        //  UIToolkit (MotionSet 에디터)
        // ================================================================

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout
            {
                text = ResolveLabel(property),
                value = property.isExpanded,
            };
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                    property.isExpanded = evt.newValue;
            });

            SerializedProperty shapeType = property.FindPropertyRelative("shapeType");
            SerializedProperty anchor = property.FindPropertyRelative("anchor");

            var shapeTypeField = new PropertyField(shapeType, LabelShapeType);
            var anchorField = new PropertyField(anchor, LabelAnchor);
            var samplingField = new PropertyField(property.FindPropertyRelative("anchorSampling"), LabelSampling);
            var evaluationField = new PropertyField(property.FindPropertyRelative("evaluation"), LabelEvaluation);
            var directionField = new PropertyField(property.FindPropertyRelative("direction"), LabelDirection);
            var worldPositionField = new PropertyField(property.FindPropertyRelative("worldPosition"), LabelWorldPosition);
            var localOffsetField = new PropertyField(property.FindPropertyRelative("localOffset"), LabelLocalOffset);
            var rotationField = new PropertyField(property.FindPropertyRelative("localEulerAngles"), LabelRotation);
            var radiusField = new PropertyField(property.FindPropertyRelative("radius"), LabelRadius);
            var boxSizeField = new PropertyField(property.FindPropertyRelative("boxSize"), LabelBoxSize);
            var capsuleHeightField = new PropertyField(property.FindPropertyRelative("capsuleHeight"), LabelCapsuleHeight);

            var errorBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);

            foldout.Add(shapeTypeField);
            foldout.Add(anchorField);
            foldout.Add(samplingField);
            foldout.Add(evaluationField);
            foldout.Add(directionField);
            foldout.Add(worldPositionField);
            foldout.Add(localOffsetField);
            foldout.Add(rotationField);
            foldout.Add(radiusField);
            foldout.Add(boxSizeField);
            foldout.Add(capsuleHeightField);
            foldout.Add(errorBox);

            SerializedProperty shapeTypeCopy = shapeType.Copy();
            SerializedProperty anchorCopy = anchor.Copy();
            SerializedProperty propertyCopy = property.Copy();

            void Sync()
            {
                propertyCopy.serializedObject.UpdateIfRequiredOrScript();

                var type = (CollisionShapeType)shapeTypeCopy.enumValueIndex;
                var anchorType = (CollisionAnchorType)anchorCopy.enumValueIndex;

                SetVisible(worldPositionField, anchorType == CollisionAnchorType.WorldPosition);
                // Sphere는 회전이 판정에 영향을 주지 않으므로 표시하지 않는다.
                SetVisible(rotationField, type != CollisionShapeType.Sphere);
                SetVisible(radiusField, type is CollisionShapeType.Sphere or CollisionShapeType.Capsule);
                SetVisible(boxSizeField, type == CollisionShapeType.Box);
                SetVisible(capsuleHeightField, type == CollisionShapeType.Capsule);

                string error = Validate(propertyCopy, type);
                SetVisible(errorBox, error != null);
                if (error != null)
                    errorBox.text = error;
            }

            Sync();
            // 자식 필드 변경은 foldout까지 버블링되므로 한 곳에서 전부 갱신한다.
            foldout.RegisterCallback<SerializedPropertyChangeEvent>(_ => Sync());

            return foldout;
        }

        private static void SetVisible(VisualElement element, bool visible)
            => element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>
        /// 부모가 지정한 한국어 라벨(<see cref="MotionEventLabelAttribute"/>)을 사용한다.
        /// CreatePropertyGUI에는 label 인자가 없어 필드 attribute에서 직접 읽는다.
        /// </summary>
        private string ResolveLabel(SerializedProperty property)
        {
            var label = fieldInfo?.GetCustomAttribute<MotionEventLabelAttribute>();
            return string.IsNullOrEmpty(label?.Label) ? property.displayName : label.Label;
        }

        // ================================================================
        //  IMGUI 폴백
        // ================================================================

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            using var indent = new EditorGUI.IndentLevelScope();
            var layout = new Layout(position);

            SerializedProperty shapeType = property.FindPropertyRelative("shapeType");
            SerializedProperty anchor = property.FindPropertyRelative("anchor");

            layout.Field(shapeType, LabelShapeType);
            layout.Field(anchor, LabelAnchor);
            layout.Field(property.FindPropertyRelative("anchorSampling"), LabelSampling);
            layout.Field(property.FindPropertyRelative("evaluation"), LabelEvaluation);
            layout.Field(property.FindPropertyRelative("direction"), LabelDirection);

            if ((CollisionAnchorType)anchor.enumValueIndex == CollisionAnchorType.WorldPosition)
                layout.Field(property.FindPropertyRelative("worldPosition"), LabelWorldPosition);

            layout.Field(property.FindPropertyRelative("localOffset"), LabelLocalOffset);

            var type = (CollisionShapeType)shapeType.enumValueIndex;
            if (type != CollisionShapeType.Sphere)
                layout.Field(property.FindPropertyRelative("localEulerAngles"), LabelRotation);

            switch (type)
            {
                case CollisionShapeType.Sphere:
                    layout.Field(property.FindPropertyRelative("radius"), LabelRadius);
                    break;

                case CollisionShapeType.Box:
                    layout.Field(property.FindPropertyRelative("boxSize"), LabelBoxSize);
                    break;

                case CollisionShapeType.Capsule:
                    layout.Field(property.FindPropertyRelative("radius"), LabelRadius);
                    layout.Field(property.FindPropertyRelative("capsuleHeight"), LabelCapsuleHeight);
                    break;
            }

            string error = Validate(property, type);
            if (error != null)
                layout.HelpBox(error, MessageType.Error);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded)
                return line;

            SerializedProperty shapeType = property.FindPropertyRelative("shapeType");
            SerializedProperty anchor = property.FindPropertyRelative("anchor");
            var type = (CollisionShapeType)shapeType.enumValueIndex;

            // 폴드아웃 헤더 + 공통 5줄 + localOffset
            int lines = 1 + 5 + 1;
            if ((CollisionAnchorType)anchor.enumValueIndex == CollisionAnchorType.WorldPosition)
                lines++;
            if (type != CollisionShapeType.Sphere)
                lines++;
            lines += type == CollisionShapeType.Capsule ? 2 : 1;

            float height = lines * line;
            if (Validate(property, type) != null)
                height += HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        private const float HelpBoxHeight = 32f;

        // ================================================================
        //  공용 검증
        // ================================================================

        private static string Validate(SerializedProperty property, CollisionShapeType type)
        {
            float radius = property.FindPropertyRelative("radius").floatValue;
            Vector3 boxSize = property.FindPropertyRelative("boxSize").vector3Value;
            float capsuleHeight = property.FindPropertyRelative("capsuleHeight").floatValue;

            return type switch
            {
                CollisionShapeType.Sphere when radius <= 0f =>
                    "반지름은 0보다 커야 합니다.",
                CollisionShapeType.Box when boxSize.x <= 0f || boxSize.y <= 0f || boxSize.z <= 0f =>
                    "박스 크기의 모든 축이 0보다 커야 합니다.",
                CollisionShapeType.Capsule when radius <= 0f =>
                    "반지름은 0보다 커야 합니다.",
                CollisionShapeType.Capsule when capsuleHeight < radius * 2f =>
                    "캡슐 전체 높이는 반지름 × 2 이상이어야 합니다.",
                _ => null,
            };
        }

        private struct Layout
        {
            private readonly float _x;
            private readonly float _width;
            private float _y;

            public Layout(Rect position)
            {
                _x = position.x;
                _width = position.width;
                _y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            public void Field(SerializedProperty property, string koreanLabel)
            {
                if (property == null)
                    return;

                var rect = new Rect(_x, _y, _width, EditorGUIUtility.singleLineHeight);
                // 툴팁은 필드에 붙은 [Tooltip]을 유지하고 표시 이름만 한국어로 바꾼다.
                EditorGUI.PropertyField(rect, property, new GUIContent(koreanLabel, property.tooltip));
                _y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            public void HelpBox(string message, MessageType type)
            {
                var rect = new Rect(_x, _y, _width, HelpBoxHeight);
                EditorGUI.HelpBox(rect, message, type);
                _y += HelpBoxHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }
    }
}
