#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround.Gameplay.Ability.Editor
{
    public static class GasAbilityTaskMigrationTool
    {
        private const string SearchRoot = "Assets/10.Datas/Ability";
        private const string OutputRoot = "Assets/10.Datas/Ability/Migrated/TaskGraphs";
        private const string TaskPath = OutputRoot + "/AbilityTask_LegacyMotionPayload.asset";
        private const string GraphPath = OutputRoot + "/AbilityTaskGraph_LegacyMotionPayload.asset";

        public readonly struct Report
        {
            public readonly int Total;
            public readonly int AlreadyAssigned;
            public readonly int Assigned;
            public readonly int Errors;

            public Report(int total, int alreadyAssigned, int assigned, int errors)
            {
                Total = total;
                AlreadyAssigned = alreadyAssigned;
                Assigned = assigned;
                Errors = errors;
            }

            public override string ToString() =>
                $"Total={Total}, Existing={AlreadyAssigned}, Assigned={Assigned}, Errors={Errors}";
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability/GAS Task Graph 변환 Preview")]
        public static void Preview()
        {
            Report report = Inspect();
            Debug.Log($"[GAS Task Migration][Preview] {report}");
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/Ability/GAS Task Graph 변환 Apply")]
        public static void ApplyFromMenu()
        {
            Report report = ApplyAll();
            Debug.Log($"[GAS Task Migration][Apply] {report}");
        }

        public static Report Inspect()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayAbilitySO", new[] { SearchRoot });
            int existing = 0;
            int errors = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameplayAbilitySO ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                if (ability == null) errors++;
                else if (ability.taskGraph != null) existing++;
            }
            return new Report(guids.Length, existing, 0, errors);
        }

        public static Report ApplyAll()
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("GAS Ability Task Graph 변환");
            try
            {
                EnsureFolder(OutputRoot);
            LegacyMotionPayloadTask task =
                AssetDatabase.LoadAssetAtPath<LegacyMotionPayloadTask>(TaskPath);
                if (task == null)
                {
                task = ScriptableObject.CreateInstance<LegacyMotionPayloadTask>();
                    task.name = "AbilityTask_LegacyMotionPayload";
                    AssetDatabase.CreateAsset(task, TaskPath);
                    Undo.RegisterCreatedObjectUndo(task, "GAS Legacy Motion Task 생성");
                }

                AbilityTaskGraphSO graph = AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(GraphPath);
                if (graph == null)
                {
                    graph = ScriptableObject.CreateInstance<AbilityTaskGraphSO>();
                    graph.name = "AbilityTaskGraph_LegacyMotionPayload";
                    AssetDatabase.CreateAsset(graph, GraphPath);
                    Undo.RegisterCreatedObjectUndo(graph, "GAS Legacy Motion Graph 생성");
                }
                var graphObject = new SerializedObject(graph);
                SerializedProperty root = graphObject.FindProperty("_root");
                if (root == null) throw new InvalidOperationException("AbilityTaskGraphSO._root를 찾을 수 없습니다.");
                if (root.objectReferenceValue != task)
                {
                    Undo.RecordObject(graph, "GAS Task Root 연결");
                    root.objectReferenceValue = task;
                    graphObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                }

                string[] guids = AssetDatabase.FindAssets(
                    "t:GameplayAbilitySO", new[] { SearchRoot });
                int existing = 0;
                int assigned = 0;
                int errors = 0;
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameplayAbilitySO ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                    if (ability == null)
                    {
                        errors++;
                        continue;
                    }
                    if (ability.taskGraph == graph)
                    {
                        existing++;
                        continue;
                    }
                    if (ability.taskGraph != null)
                    {
                        existing++;
                        continue;
                    }
                    Undo.RecordObject(ability, "GAS Task Graph 연결");
                    ability.taskGraph = graph;
                    EditorUtility.SetDirty(ability);
                    assigned++;
                }

                if (errors > 0)
                    throw new InvalidOperationException($"GameplayAbilitySO 로드 오류 {errors}건");
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                return new Report(guids.Length, existing, assigned, 0);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
