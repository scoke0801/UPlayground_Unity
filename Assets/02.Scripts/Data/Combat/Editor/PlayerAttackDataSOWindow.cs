using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// PlayerAttackDataSO 전용 에디터 윈도우.
    /// 인스펙터 하단 "에디터 창에서 열기" 버튼 또는 메뉴에서 열 수 있습니다.
    /// </summary>
    public class PlayerAttackDataSOWindow : EditorWindow
    {
        // ─── 메뉴 진입점 ─────────────────────────────────────────────
        [MenuItem("UPlayGround/공격 데이터 에디터")]
        public static void OpenFromMenu()
        {
            // 현재 선택된 에셋이 있으면 그걸 바인딩
            var selected = Selection.activeObject as PlayerAttackDataSO;
            Open(selected);
        }

        /// <summary> CustomEditor에서 호출. </summary>
        public static void Open(PlayerAttackDataSO so)
        {
            var w = GetWindow<PlayerAttackDataSOWindow>("공격 데이터 에디터");
            w.minSize = new Vector2(420, 400);
            if (so != null) w.Bind(so);
            w.Show();
        }

        // ─── 상태 ────────────────────────────────────────────────────
        private PlayerAttackDataSO   _target;
        private SerializedObject     _serialized;
        private PlayerAttackDataSODrawer _drawer;
        private Vector2              _scroll;

        // ─── 바인딩 ──────────────────────────────────────────────────
        private void Bind(PlayerAttackDataSO so)
        {
            _target     = so;
            _serialized = new SerializedObject(so);
            _drawer     = new PlayerAttackDataSODrawer(_serialized);
        }

        private void OnSelectionChange()
        {
            // 인스펙터에서 에셋 선택 시 자동으로 따라오기
            var sel = Selection.activeObject as PlayerAttackDataSO;
            if (sel != null && sel != _target)
            {
                Bind(sel);
                Repaint();
            }
        }

        // ─── GUI ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();

            if (_target == null || _drawer == null)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.HelpBox(
                    "PlayerAttackDataSO 에셋을 위 필드에 드래그하거나\n인스펙터에서 에셋을 선택하세요.",
                    MessageType.Info);
                return;
            }

            _serialized.Update();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _drawer.DrawGUI();
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
            _serialized.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            // 배경 바
            Rect headerRect = GUILayoutUtility.GetRect(0, 42, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // 탭 컬러 하단 바
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 2, headerRect.width, 2),
                               new Color(0.4f, 0.65f, 1f, 0.9f));

            // 제목
            EditorGUI.LabelField(
                new Rect(headerRect.x + 10, headerRect.y, 160, headerRect.height),
                "공격 데이터 에디터",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = Color.white } });

            // Object Field
            float fieldX = headerRect.x + 160;
            float fieldW = headerRect.width - 170;
            EditorGUI.BeginChangeCheck();
            var newTarget = (PlayerAttackDataSO)EditorGUI.ObjectField(
                new Rect(fieldX, headerRect.y + (headerRect.height - 18) * 0.5f, fieldW, 18),
                _target, typeof(PlayerAttackDataSO), false);
            if (EditorGUI.EndChangeCheck() && newTarget != _target)
                Bind(newTarget);
        }
    }
}
