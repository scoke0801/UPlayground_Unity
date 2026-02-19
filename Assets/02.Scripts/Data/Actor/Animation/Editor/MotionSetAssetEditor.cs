using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionSetAsset 커스텀 인스펙터.
    /// MotionSetDrawer를 사용해 인스펙터 안에서 직접 편집할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(MotionSetAsset))]
    public class MotionSetAssetEditor : UnityEditor.Editor
    {
        MotionSetDrawer _drawer;

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(
                () => target,       // Undo/Dirty 대상 = 에셋 자체
                Repaint             // 리페인트 콜백
            );
        }

        public override void OnInspectorGUI()
        {
            var asset = (MotionSetAsset)target;
            if (asset == null) return;

            // motionSet 필드가 null이면 초기화
            if (asset.motionSet == null)
            {
                Undo.RecordObject(asset, "Init MotionSet");
                asset.motionSet = new MotionSet { motionSetName = asset.name };
                EditorUtility.SetDirty(asset);
            }

            serializedObject.Update();

            // ── MotionSetDrawer로 전체 편집 UI 그리기 ──
            EditorGUI.BeginChangeCheck();
            _drawer.DrawFullGUI(asset.motionSet);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            EditorGUILayout.Space(6);

            // ── 구분선 ──
            Rect line = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(line, new Color(0.5f, 0.5f, 0.5f, 0.4f));

            EditorGUILayout.Space(6);

            // ── 에디터 창 열기 버튼 ──
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("에디터 창에서 열기", GUILayout.Height(32)))
            {
                var window = EditorWindow.GetWindow<MotionSetEditorWindow>();
                window.titleContent = new GUIContent("애니메이션 에디터");
                window.minSize      = new Vector2(600, 400);
                window.Show();

                // 창에 현재 에셋 바인딩
                // (OnEnable → TryBindFromSelection이 Selection을 보므로 먼저 선택)
                Selection.activeObject = asset;
            }
            GUI.backgroundColor = Color.white;
        }
    }
}