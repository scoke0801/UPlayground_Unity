using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class GenerateActorDescStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.PrefabName))
                throw new BuildException("PrefabName이 비어있습니다 (GenerateActorDescStep).");

            var template = ActorTemplateFactory.Get(ctx.Config.ActorKind);
            foreach (var def in template.GetDescDefs(ctx.Config))
            {
                if (def == null || def.DescType == null) continue;

                var so = ScriptableObject.CreateInstance(def.DescType);
                if (so == null)
                {
                    Debug.LogWarning($"[P09Builder] Failed to create ScriptableObject of type {def.DescType.Name}");
                    continue;
                }

                try
                {
                    def.ApplyDefaults(so, ctx.Config);
                }
                catch (System.Exception ex)
                {
                    Object.DestroyImmediate(so);
                    throw new BuildException($"ApplyDefaults 실패 ({def.DescType.Name}): {ex.Message}", ex);
                }

                var dataFolder = PathConfig.GetGeneratedDataFolder(def.DescType);
                // 중앙 고정 경로를 사용하되 기존 에셋은 제자리 갱신해 GUID와 외부 참조를 보존한다.
                so = PathConfig.CreateOrUpdateAsset(
                    so,
                    dataFolder,
                    $"{ctx.PrefabName}{def.Suffix}",
                    out string assetPath,
                    out bool created,
                    ctx);

                ctx.GeneratedDescs.Add(so);
                if (created)
                    ctx.GeneratedAssetPaths.Add(assetPath);
            }
        }
    }
}
