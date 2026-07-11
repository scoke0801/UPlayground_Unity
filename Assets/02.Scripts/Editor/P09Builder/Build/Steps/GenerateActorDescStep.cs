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
                // 중앙 폴더에 고정 경로로 생성 → 재빌드 시 _1,_2 중복 누적 없이 덮어쓴다.
                var assetPath = PathConfig.CreateOrReplaceAsset(so, dataFolder, $"{ctx.PrefabName}{def.Suffix}");

                ctx.GeneratedDescs.Add(so);
                ctx.GeneratedAssetPaths.Add(assetPath);
            }
        }
    }
}
