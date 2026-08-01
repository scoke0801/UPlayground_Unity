using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Group;

namespace UPlayGround.Editor.Validation
{
    public sealed class MonsterGroupRuntimeDebuggerWindow : EditorWindow
    {
        private double _nextRefreshTime;
        private Vector2 _scroll;

        [MenuItem("Tools/UPlayGround/Debug/Monster Group Runtime Overlay")]
        private static void Open()
        {
            GetWindow<MonsterGroupRuntimeDebuggerWindow>("Monster Groups");
        }

        private void OnEnable() => EditorApplication.update += RefreshIncrementally;
        private void OnDisable() => EditorApplication.update -= RefreshIncrementally;

        private void RefreshIncrementally()
        {
            if (!EditorApplication.isPlaying)
                return;

            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
                return;

            _nextRefreshTime = EditorApplication.timeSinceStartup + 0.2d;
            Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서 슬롯과 breather 상태를 표시합니다.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var groups = Object.FindObjectsByType<MonsterGroupController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var group in groups)
            {
                if (group == null || !group.gameObject.scene.IsValid())
                    continue;

                var snapshot = group.GetDebugSnapshot();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(group, typeof(MonsterGroupController), true);
                EditorGUILayout.LabelField(
                    "생존 / 슬롯",
                    $"{snapshot.AliveCount} / M {snapshot.MeleeOwners}:{group.CurrentMeleeSlotLimit}, "
                    + $"R {snapshot.RangedOwners}:{group.CurrentRangedSlotLimit}");
                EditorGUILayout.LabelField(
                    "후보 / Formation",
                    $"M {snapshot.MeleeCandidates}, R {snapshot.RangedCandidates} / {snapshot.FormationOwners}");
                EditorGUILayout.LabelField(
                    "Breather",
                    $"Group {snapshot.GroupBreatherRemaining:0.00}s / Player {snapshot.PlayerBreatherRemaining:0.00}s");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

    }

    /// <summary>
    /// 열린 씬에서 MonsterGroupController 계층 밖에 배치된 몬스터를 찾는다.
    /// 그룹 비소속이 의도된 개체는 검사 결과를 확인한 뒤 별도 그룹으로 명시적으로 묶는다.
    /// </summary>
    public static class MonsterGroupPlacementValidator
    {
        [MenuItem("Tools/UPlayGround/Validation/Monster Group Placement")]
        public static void ValidateAndLog()
        {
            var ungrouped = FindUngroupedMonsters();
            if (ungrouped.Count == 0)
            {
                Debug.Log("[MonsterGroupPlacementValidator] 열린 씬의 그룹 미소속 MonsterActor: 0");
                return;
            }

            foreach (var monster in ungrouped)
            {
                Debug.LogWarning(
                    $"[MonsterGroupPlacementValidator] 그룹 미소속 몬스터: {GetHierarchyPath(monster.transform)}",
                    monster);
            }

            Debug.LogWarning($"[MonsterGroupPlacementValidator] 그룹 미소속 MonsterActor: {ungrouped.Count}");
            Selection.activeObject = ungrouped[0];
        }

        public static List<MonsterActor> FindUngroupedMonsters()
        {
            var result = new List<MonsterActor>();
            var monsters = Object.FindObjectsByType<MonsterActor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var monster in monsters)
            {
                if (monster == null || !monster.gameObject.scene.IsValid())
                    continue;
                if (monster.GetComponentInParent<MonsterGroupController>(includeInactive: true) != null)
                    continue;

                result.Add(monster);
            }

            result.Sort((a, b) => string.CompareOrdinal(
                GetHierarchyPath(a.transform),
                GetHierarchyPath(b.transform)));
            return result;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return $"{transform.gameObject.scene.name}/{path}";
        }
    }
}
