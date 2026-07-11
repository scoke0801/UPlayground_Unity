using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class PrefabBuildPipeline
    {
        private readonly List<IBuildStep> _steps;
        private readonly NameSequenceRegistry _registry;

        public PrefabBuildPipeline()
        {
            _registry = new NameSequenceRegistry();
            _registry.Load();

            _steps = new List<IBuildStep>
            {
                new InstantiateBaseStep(_registry),
                new ToggleMagicaClothStep(),
                new AttachActorComponentsStep(),
                new GenerateActorDescStep(),
                new AssignStatsStep(),
                new ApplyAppearanceStep(),
                new ApplyWeaponStep(),
                new NameAndSaveStep(),
                new SyncActorDatabaseStep(),
            };
        }

        public BuildResult Build(CharacterBuildConfig config)
        {
            if (config == null)
                return BuildResult.Fail("CharacterBuildConfig가 null입니다.");

            // 1. Validate
            var errors = new List<string>(config.Validate());
            if (errors.Count > 0)
            {
                var sb = new StringBuilder("유효성 검증 실패:");
                foreach (var e in errors) sb.Append("\n  - ").Append(e);
                return BuildResult.Fail(sb.ToString());
            }

            var ctx = new BuildContext(config);
            BuildResult result = BuildResult.Fail("Unknown error");
            var undo = new UndoGroupScope("P09 Builder: Build Character");

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < _steps.Count; i++)
                {
                    var step = _steps[i];
                    var stepName = step.GetType().Name;
                    EditorUtility.DisplayProgressBar(
                        "P09 Character Prefab Builder",
                        stepName,
                        (i + 1f) / _steps.Count);
                    ctx.Logs.Add(stepName);
                    step.Execute(ctx);
                }

                var prefabPath = ctx.Bag.TryGetValue("finalPrefabPath", out var p) ? p as string : null;
                var prefabAsset = ctx.Bag.TryGetValue("finalPrefabAsset", out var a) ? a as GameObject : null;

                result = BuildResult.Ok(prefabAsset, prefabPath, new List<string>(ctx.GeneratedAssetPaths), new List<string>(ctx.Logs));
            }
            catch (BuildException bex)
            {
                ctx.Logs.Add($"실패: {bex.Message}");
                Rollback(ctx);
                result = BuildResult.Fail(bex.Message, new List<string>(ctx.Logs));
            }
            catch (Exception ex)
            {
                ctx.Logs.Add($"예외: {ex.Message}");
                Rollback(ctx);
                Debug.LogException(ex);
                result = BuildResult.Fail($"예외 발생: {ex.Message}", new List<string>(ctx.Logs));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                undo.Collapse();
                undo.Dispose();
            }

            return result;
        }

        private static void Rollback(BuildContext ctx)
        {
            try
            {
                if (ctx.RootInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(ctx.RootInstance);
                    ctx.RootInstance = null;
                }
            }
            catch { }

            // 생성된 에셋 삭제
            for (int i = ctx.GeneratedAssetPaths.Count - 1; i >= 0; i--)
            {
                var path = ctx.GeneratedAssetPaths[i];
                if (string.IsNullOrEmpty(path)) continue;
                try { AssetDatabase.DeleteAsset(path); } catch { }
            }

            // 빈 폴더 정리
            if (!string.IsNullOrEmpty(ctx.PrefabFolder))
            {
                try
                {
                    var descFolder = ctx.PrefabFolder + "/Descs";
                    if (AssetDatabase.IsValidFolder(descFolder))
                    {
                        var assets = AssetDatabase.FindAssets(string.Empty, new[] { descFolder });
                        if (assets == null || assets.Length == 0)
                            AssetDatabase.DeleteAsset(descFolder);
                    }
                    if (AssetDatabase.IsValidFolder(ctx.PrefabFolder))
                    {
                        var assets = AssetDatabase.FindAssets(string.Empty, new[] { ctx.PrefabFolder });
                        if (assets == null || assets.Length == 0)
                            AssetDatabase.DeleteAsset(ctx.PrefabFolder);
                    }
                }
                catch { }
            }
        }
    }
}
