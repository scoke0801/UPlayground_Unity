using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Editor
{
    public static class MissingScriptCleaner
    {
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
