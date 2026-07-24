using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Editor
{
    public static class MissingScriptCleaner
    {
        /// <summary>
        /// 에셋을 수정하지 않고 전체 Prefab/Scene의 Missing Script를 검사한다.
        /// BatchMode 완료 게이트에서 사용하며 하나라도 발견하면 실패로 종료한다.
        /// </summary>
        public static void ValidateAllMissingScriptsBatch()
        {
            AssetDatabase.Refresh();
            int missing = CountMissingScriptsInPrefabs()
                          + CountMissingScriptsInScenes();
            if (missing > 0)
                throw new System.InvalidOperationException(
                    $"Missing Script {missing}개를 발견했습니다.");

            Debug.Log("[MissingScriptCleaner] Missing Script 검증 성공: 0개");
        }

        public static void RemoveAllMissingScriptsBatch()
        {
            AssetDatabase.Refresh();

            int removed = 0;
            removed += CleanPrefabs();
            removed += CleanScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MissingScriptCleaner] Missing Script 정리 완료. 제거 수: {removed}");
        }

        private static int CleanPrefabs()
        {
            int removed = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int count = RemoveMissingScriptsInHierarchy(root);
                    if (count <= 0)
                        continue;

                    removed += count;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[MissingScriptCleaner] Prefab 정리: {path} ({count})");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return removed;
        }

        private static int CleanScenes()
        {
            int removed = 0;
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = 0;

                foreach (var root in scene.GetRootGameObjects())
                    count += RemoveMissingScriptsInHierarchy(root);

                if (count <= 0)
                    continue;

                removed += count;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[MissingScriptCleaner] Scene 정리: {path} ({count})");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return removed;
        }

        private static int CountMissingScriptsInPrefabs()
        {
            int missing = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Prefab", new[] { "Assets/03.Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    missing += CountMissingScriptsInHierarchy(root);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return missing;
        }

        private static int CountMissingScriptsInScenes()
        {
            int missing = 0;
            string[] scenePaths = AssetDatabase.FindAssets(
                    "t:Scene", new[] { "Assets/01.Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            foreach (string path in scenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(
                    path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    missing += CountMissingScriptsInHierarchy(root);
            }
            return missing;
        }

        private static int CountMissingScriptsInHierarchy(GameObject root)
        {
            int missing = 0;
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    transform.gameObject);
            }
            return missing;
        }

        private static int RemoveMissingScriptsInHierarchy(GameObject root)
        {
            int removed = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);

            foreach (var transform in transforms)
            {
                var gameObject = transform.gameObject;
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                if (count <= 0)
                    continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                removed += count;
                EditorUtility.SetDirty(gameObject);
            }

            return removed;
        }
    }
}
