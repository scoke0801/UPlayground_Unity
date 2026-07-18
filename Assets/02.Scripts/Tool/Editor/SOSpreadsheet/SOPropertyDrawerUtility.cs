using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.SOSpreadsheet
{
    /// <summary>
    /// 커스텀 PropertyDrawer / 데코레이터([Header]/[Space]) 판별 유틸리티.
    /// UnityEditor 내부 PropertyHandler를 리플렉션으로 조회하며,
    /// 실패 시 조용히 false/0을 반환해 평탄화 쪽 경로로 진행한다 (버전 변화 대비).
    /// </summary>
    internal static class SOPropertyDrawerUtility
    {
        private static System.Reflection.MethodInfo s_getHandler;
        private static System.Reflection.PropertyInfo s_hasPropertyDrawer;
        private static System.Reflection.PropertyInfo s_propertyDrawer;
        private static System.Reflection.FieldInfo s_decoratorDrawers;
        private static bool s_reflectionFailed;

        private static object GetHandlerFor(SerializedProperty prop)
        {
            if (s_reflectionFailed)
                return null;

            try
            {
                if (s_getHandler == null)
                {
                    var assembly = typeof(UnityEditor.Editor).Assembly;
                    var utility = assembly.GetType("UnityEditor.ScriptAttributeUtility");
                    s_getHandler = utility?.GetMethod(
                        "GetHandler",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    var handlerType = assembly.GetType("UnityEditor.PropertyHandler");
                    var flags = System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance;
                    s_hasPropertyDrawer = handlerType?.GetProperty("hasPropertyDrawer", flags);
                    s_propertyDrawer = handlerType?.GetProperty("propertyDrawer", flags);
                    s_decoratorDrawers = handlerType?.GetField("m_DecoratorDrawers", flags);

                    if (s_getHandler == null || (s_hasPropertyDrawer == null && s_propertyDrawer == null))
                    {
                        s_reflectionFailed = true;
                        return null;
                    }
                }
                return s_getHandler.Invoke(null, new object[] { prop });
            }
            catch
            {
                s_reflectionFailed = true;
                return null;
            }
        }

        /// <summary>프로퍼티에 커스텀 PropertyDrawer가 붙어 있는지. 판별 실패 시 false(평탄화 진행).</summary>
        public static bool HasCustomDrawer(SerializedProperty prop)
        {
            object handler = GetHandlerFor(prop);
            if (handler == null)
                return false;

            try
            {
                if (s_hasPropertyDrawer != null)
                    return (bool)s_hasPropertyDrawer.GetValue(handler);
                return s_propertyDrawer.GetValue(handler) != null;
            }
            catch
            {
                s_reflectionFailed = true;
                return false;
            }
        }

        /// <summary>프로퍼티 앞에 붙는 데코레이터([Header]/[Space] 등)의 총 높이.</summary>
        public static float GetDecoratorHeight(SerializedProperty prop)
        {
            object handler = GetHandlerFor(prop);
            if (handler == null || s_decoratorDrawers == null)
                return 0f;

            try
            {
                if (s_decoratorDrawers.GetValue(handler) is not System.Collections.IEnumerable drawers)
                    return 0f;
                float height = 0f;
                foreach (object drawer in drawers)
                {
                    if (drawer is DecoratorDrawer decorator)
                        height += decorator.GetHeight();
                }
                return height;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>드로어가 그리는 높이가 1줄 이내인지 (열 구성 시 대표 에셋으로 한 번만 판정).</summary>
        public static bool IsSingleLineDrawn(SerializedProperty prop)
        {
            float height = EditorGUI.GetPropertyHeight(prop, GUIContent.none, true) - GetDecoratorHeight(prop);
            return height <= EditorGUIUtility.singleLineHeight + 2f;
        }

        /// <summary>
        /// IMGUI 폴백 드로어에서 잘라낼 위쪽 높이. 데코레이터([Header]/[Space])와
        /// [TextArea]류 드로어가 예약하는 빈 라벨 줄은 필드 단위로 일정하므로 열 구성 시 계산한다.
        /// </summary>
        public static float ComputeTopCut(SerializedProperty prop, bool customDrawer)
        {
            float cut = GetDecoratorHeight(prop);
            if (prop.propertyType == SerializedPropertyType.String && customDrawer)
            {
                float remaining = EditorGUI.GetPropertyHeight(prop, GUIContent.none, true) - cut;
                if (remaining > EditorGUIUtility.singleLineHeight + 1f)
                    cut += EditorGUIUtility.singleLineHeight;
            }
            return cut;
        }
    }
}
