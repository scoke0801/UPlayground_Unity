using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class NameAndSaveStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx.RootInstance == null)
                throw new BuildException("RootInstance가 null입니다 (NameAndSaveStep).");
            if (string.IsNullOrEmpty(ctx.PrefabName))
                throw new BuildException("PrefabName이 비어있습니다 (NameAndSaveStep).");
            if (string.IsNullOrEmpty(ctx.PrefabFolder))
                throw new BuildException("PrefabFolder가 비어있습니다 (NameAndSaveStep).");

            ctx.RootInstance.name = ctx.PrefabName;

            // 인스턴스가 prefab asset 자체를 가리키거나 prefab edit mode 상태이면 SaveAsPrefabAsset 가 실패한다.
            if (PrefabUtility.IsPartOfPrefabAsset(ctx.RootInstance))
            {
                throw new BuildException(
                    "RootInstance 가 프리팹 에셋 자체를 가리킵니다. 인스턴스화가 제대로 되지 않았습니다.");
            }

            var prefabPath = $"{ctx.PrefabFolder}/{ctx.PrefabName}.prefab";
            RemoveMissingScripts(ctx.RootInstance);

            // 파이프라인이 AssetDatabase.StartAssetEditing 스코프 안이라
            // AssetDatabase.CreateFolder / IsValidFolder / SaveAsPrefabAsset 가 deferred 상태에서는 실패한다.
            // (AssetDatabase 가 새로 만든 폴더를 모르는 상태로 SaveAsPrefabAsset 가 호출되어
            //  folderValid=False 로 떨어짐.) 폴더 생성 + 저장 구간만 batching 을 풀어둔다.
            AssetDatabase.StopAssetEditing();
            GameObject saved = null;
            bool success = false;
            try
            {
                EnsureFolderRobust(ctx.PrefabFolder);

                try
                {
                    saved = PrefabUtility.SaveAsPrefabAsset(ctx.RootInstance, prefabPath, out success);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[P09Builder] SaveAsPrefabAsset 예외: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                    throw new BuildException($"프리팹 저장 중 예외 발생: {prefabPath} ({ex.Message})");
                }

                if (!success || saved == null)
                {
                    Debug.LogError(
                        $"[P09Builder] SaveAsPrefabAsset 실패. path='{prefabPath}', " +
                        $"folderValid={AssetDatabase.IsValidFolder(ctx.PrefabFolder)}, " +
                        $"physicalFolderExists={Directory.Exists(ctx.PrefabFolder)}, " +
                        $"rootName='{ctx.RootInstance.name}', " +
                        $"rootScene='{ctx.RootInstance.scene.name}'");
                    throw new BuildException($"프리팹 저장에 실패했습니다: {prefabPath}");
                }
            }
            finally
            {
                // 파이프라인의 바깥 try/finally 가 짝을 맞추도록 다시 batching 진입.
                AssetDatabase.StartAssetEditing();
            }

            ctx.GeneratedAssetPaths.Add(prefabPath);
            ctx.Bag["finalPrefabPath"] = prefabPath;
            ctx.Bag["finalPrefabAsset"] = saved;

            Object.DestroyImmediate(ctx.RootInstance);
            ctx.RootInstance = null;
        }

        private static void EnsureFolderRobust(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;

            if (AssetDatabase.IsValidFolder(folder)) return;

            // 부모부터 한 단계씩 AssetDatabase.CreateFolder 로 만든다 (배치 스코프 밖이므로 즉시 등록됨).
            var parts = folder.Replace('\\', '/').Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        // CreateFolder 가 실패하면 물리 폴더를 만들고 ImportAsset 으로 등록.
                        if (!Directory.Exists(next))
                            Directory.CreateDirectory(next);
                        AssetDatabase.ImportAsset(next, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
                current = next;
            }
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            if (root == null) return;

            int removed = 0;
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t == null) continue;

                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count <= 0) continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                removed += count;
            }

            if (removed > 0)
                Debug.Log($"[P09Builder] MissingScript {removed}개 제거 완료: {root.name}");
        }
    }
}
