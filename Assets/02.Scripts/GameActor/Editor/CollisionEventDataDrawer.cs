using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 궁극기 타임라인 등 공용 Collision 저작 데이터의 인스펙터.
    /// 판정 소스에 따라 부착형 그룹 또는 명시적 범위만 표시한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(CollisionEventData))]
    public sealed class CollisionEventDataDrawer : PropertyDrawer
    {
        private static readonly float Line =
            EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            using var indent = new EditorGUI.IndentLevelScope();
            float y = position.y + Line;

            SerializedProperty source = property.FindPropertyRelative("collisionSource");

            y = Draw(position, y, property.FindPropertyRelative("hitPhaseIndex"), "히트 페이즈 인덱스");
            y = Draw(position, y, source, "판정 소스");

            if ((CollisionSourceType)source.enumValueIndex == CollisionSourceType.ExplicitShape)
            {
                Draw(position, y, property.FindPropertyRelative("explicitShape"), "명시적 판정 범위");
            }
            else
            {
                y = Draw(position, y, property.FindPropertyRelative("hitboxGroupId"), "HitBox 그룹 ID");
                Draw(position, y, property.FindPropertyRelative("additionalHitboxGroupIds"), "추가 HitBox 그룹");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return Line;

            SerializedProperty source = property.FindPropertyRelative("collisionSource");
            float height = Line * 3f; // 헤더 + 히트 페이즈 + 판정 소스

            if ((CollisionSourceType)source.enumValueIndex == CollisionSourceType.ExplicitShape)
            {
                height += Height(property.FindPropertyRelative("explicitShape"));
            }
            else
            {
                height += Line;
                height += Height(property.FindPropertyRelative("additionalHitboxGroupIds"));
            }

            return height;
        }

        private static float Draw(Rect position, float y, SerializedProperty property, string koreanLabel)
        {
            if (property == null)
                return y;

            float height = EditorGUI.GetPropertyHeight(property, true);
            var rect = new Rect(position.x, y, position.width, height);
            EditorGUI.PropertyField(rect, property, new GUIContent(koreanLabel, property.tooltip), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float Height(SerializedProperty property)
            => property == null
                ? 0f
                : EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing;
    }
}
