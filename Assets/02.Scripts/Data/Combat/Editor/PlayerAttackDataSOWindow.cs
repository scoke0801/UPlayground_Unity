using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;

namespace UPlayGround.Editor
{
    /// <summary>
    /// 플레이어 / 몬스터 공격 데이터 통합 에디터 윈도우.
    /// PlayerAttackDataSO, EnemyAttackDataSO 모두 지원.
    /// </summary>
    public class PlayerAttackDataSOWindow : EditorWindow
    {
        // ─── 메뉴 진입점 ─────────────────────────────────────────────
        [MenuItem("UPlayGround/공격 데이터 에디터")]
        public static void OpenFromMenu()
        {
            var w = GetWindow<PlayerAttackDataSOWindow>("공격 데이터 에디터");
            w.minSize = new Vector2(480, 400);

            // 현재 선택된 에셋 자동 바인딩
            if (Selection.activeObject is PlayerAttackDataSO player) w.BindPlayer(player);
            else if (Selection.activeObject is EnemyAttackDataSO enemy) w.BindEnemy(enemy);

            w.Show();
        }

        /// <summary> CustomEditor 등 외부에서 플레이어 SO를 지정해 열기. </summary>
        public static void Open(PlayerAttackDataSO so)
        {
            var w = GetWindow<PlayerAttackDataSOWindow>("공격 데이터 에디터");
            w.minSize = new Vector2(480, 400);
            if (so != null) w.BindPlayer(so);
            w.Show();
        }

        /// <summary> 몬스터 SO를 지정해 열기. </summary>
        public static void Open(EnemyAttackDataSO so)
        {
            var w = GetWindow<PlayerAttackDataSOWindow>("공격 데이터 에디터");
            w.minSize = new Vector2(480, 400);
            if (so != null) w.BindEnemy(so);
            w.Show();
        }

        // ─── 상태 ────────────────────────────────────────────────────
        private enum TargetType { None, Player, Enemy }
        private TargetType _targetType = TargetType.None;

        // 플레이어
        private PlayerAttackDataSO      _playerTarget;
        private SerializedObject        _playerSerialized;
        private PlayerAttackDataSODrawer _playerDrawer;

        // 몬스터
        private EnemyAttackDataSO       _enemyTarget;
        private SerializedObject        _enemySerialized;
        private EnemyAttackDataSODrawer  _enemyDrawer;

        private Vector2 _scroll;

        // ─── 바인딩 ──────────────────────────────────────────────────
        private void BindPlayer(PlayerAttackDataSO so)
        {
            _playerTarget     = so;
            _playerSerialized = new SerializedObject(so);
            _playerDrawer     = new PlayerAttackDataSODrawer(_playerSerialized);
            _enemyTarget      = null;
            _targetType       = TargetType.Player;
        }

        private void BindEnemy(EnemyAttackDataSO so)
        {
            _enemyTarget      = so;
            _enemySerialized  = new SerializedObject(so);
            _enemyDrawer      = new EnemyAttackDataSODrawer(_enemySerialized);
            _playerTarget     = null;
            _targetType       = TargetType.Enemy;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is PlayerAttackDataSO player && player != _playerTarget)
            {
                BindPlayer(player);
                Repaint();
            }
            else if (Selection.activeObject is EnemyAttackDataSO enemy && enemy != _enemyTarget)
            {
                BindEnemy(enemy);
                Repaint();
            }
        }

        // ─── GUI ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();

            if (_targetType == TargetType.None)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.HelpBox(
                    "PlayerAttackDataSO 또는 EnemyAttackDataSO 에셋을 위 필드에 드래그하거나\n인스펙터에서 에셋을 선택하세요.",
                    MessageType.Info);
                return;
            }

            if (_targetType == TargetType.Player && _playerSerialized != null)
            {
                _playerSerialized.Update();
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                _playerDrawer.DrawGUI();
                EditorGUILayout.Space(20);
                EditorGUILayout.EndScrollView();
                _playerSerialized.ApplyModifiedProperties();
            }
            else if (_targetType == TargetType.Enemy && _enemySerialized != null)
            {
                _enemySerialized.Update();
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                _enemyDrawer.DrawGUI();
                EditorGUILayout.Space(20);
                EditorGUILayout.EndScrollView();
                _enemySerialized.ApplyModifiedProperties();
            }
        }

        private void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 42, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            Color barColor = _targetType == TargetType.Enemy
                ? new Color(1f, 0.45f, 0.20f, 0.9f)
                : new Color(0.4f, 0.65f, 1f, 0.9f);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 2, headerRect.width, 2), barColor);

            string title = _targetType == TargetType.Enemy ? "적 공격 데이터 에디터" : "공격 데이터 에디터";
            EditorGUI.LabelField(
                new Rect(headerRect.x + 10, headerRect.y, 180, headerRect.height),
                title,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = Color.white } });

            float fieldX = headerRect.x + 184;
            float fieldW = headerRect.width - 194;
            float fieldY = headerRect.y + (headerRect.height - 18) * 0.5f;

            // 하나의 ObjectField로 두 타입 모두 지원 (Object 타입으로 받아 타입 감지)
            UnityEngine.Object current = _targetType == TargetType.Player ? (UnityEngine.Object)_playerTarget : _enemyTarget;

            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(
                new Rect(fieldX, fieldY, fieldW, 18),
                current, 
                typeof(AttackDataSO), // 필터링할 타입을 공통 부모로 지정
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (newObj is PlayerAttackDataSO p && p != _playerTarget) BindPlayer(p);
                else if (newObj is EnemyAttackDataSO e && e != _enemyTarget) BindEnemy(e);
            }
        }
    }
}
