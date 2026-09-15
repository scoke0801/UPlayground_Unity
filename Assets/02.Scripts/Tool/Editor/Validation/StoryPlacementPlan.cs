using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.EditorTools
{
    /// <summary>검토한 월드 좌표 변경안을 기존 배치와 대조한 뒤 Undo 단위로 적용한다.</summary>
    public static class StoryPlacementPlan
    {
        [Serializable]
        private sealed class Plan
        {
            public string scene;
            public Change[] changes;
        }

        [Serializable]
        private sealed class Change
        {
            public string path;
            public Vector3 expectedPosition;
            public Vector3 position;
            public bool changeBox;
            public Vector3 boxCenter;
            public Vector3 boxSize;
        }

        /// <summary>Temp/StoryPlacementPlan.json의 좌표를 적용하며 저장은 씬 저장 명령으로 수행한다.</summary>
        [UPlaygroundTool("UPlayGround/검증/스토리 배치 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서만 배치를 적용할 수 있습니다.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText("Temp/StoryPlacementPlan.json"));
            Scene scene = SceneManager.GetActiveScene();
            if (plan == null || plan.scene != scene.path || plan.changes == null)
                throw new InvalidOperationException("열린 씬과 배치 변경안이 일치하지 않습니다.");

            var targets = new List<Transform>();
            var paths = new HashSet<string>();
            foreach (Change change in plan.changes)
            {
                Transform target = FindUniqueTransform(scene, change.path);
                if (!paths.Add(change.path) || !IsFinite(change.position)
                    || Vector3.Distance(target.position, change.expectedPosition) > 0.01f)
                    throw new InvalidOperationException("기존 좌표 불일치 또는 잘못된 변경안: " + change.path);
                if (change.changeBox && (target.GetComponent<BoxCollider>() == null
                    || !IsFinite(change.boxCenter) || !IsFinite(change.boxSize)
                    || change.boxSize.x <= 0f || change.boxSize.y <= 0f || change.boxSize.z <= 0f))
                    throw new InvalidOperationException("BoxCollider 변경안을 확인하세요: " + change.path);
                targets.Add(target);
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("스토리 배치 변경안 적용");
            try
            {
                for (int index = 0; index < targets.Count; index++)
                {
                    Transform target = targets[index];
                    Change change = plan.changes[index];
                    Undo.RecordObject(target, "스토리 월드 좌표");
                    target.position = change.position;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    if (!change.changeBox) continue;
                    BoxCollider box = target.GetComponent<BoxCollider>();
                    Undo.RecordObject(box, "스토리 진입 범위");
                    box.center = change.boxCenter;
                    box.size = change.boxSize;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                }
                Physics.SyncTransforms();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Debug.Log($"[StoryPlacementPlan] {targets.Count}개 배치 적용. 검증 후 씬을 저장하세요.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static Transform FindUniqueTransform(Scene scene, string path)
        {
            Transform found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (GetPath(candidate) != path) continue;
                if (found != null) throw new InvalidOperationException("중복된 객체 경로: " + path);
                found = candidate;
            }
            return found != null ? found : throw new InvalidOperationException("객체 경로 누락: " + path);
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static string GetPath(Transform target) =>
            target.parent == null ? target.name : GetPath(target.parent) + "/" + target.name;
    }
}
