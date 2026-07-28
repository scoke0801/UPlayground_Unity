using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 별도 프로젝트 편집기가 없을 때 사용되는 모듈 기본 인스펙터.
    /// UPlayground의 프로젝트 전용 인스펙터가 있으면 비-fallback Editor가 우선한다.
    /// </summary>
    [CustomEditor(typeof(MotionSetAsset), true, isFallback = true)]
    public sealed class MotionSetAssetFallbackEditor : UnityEditor.Editor
    {
        private SerializedProperty _motionSet;

        private void OnEnable()
        {
            _motionSet = serializedObject.FindProperty("motionSet");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                $"MotionSet Core · 이벤트 타입 {MotionEventCatalog.Descriptors.Count}개 검색됨",
                MessageType.Info);

            if (_motionSet == null)
            {
                EditorGUILayout.HelpBox(
                    "motionSet 직렬화 필드를 찾을 수 없습니다.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.PropertyField(_motionSet, true);
            }

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(target);

            if (GUILayout.Button("이벤트 카탈로그 새로고침"))
            {
                MotionEventCatalog.Refresh();
                Repaint();
            }
        }
    }
}
