using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    public sealed class GenerateActorDescStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (string.IsNullOrEmpty(ctx.PrefabName))
                throw new BuildException("PrefabName이 비어있습니다 (GenerateActorDescStep).");

            var template = ActorTemplateFactory.Get(ctx.Config.ActorKind);
            var descFolder = ctx.PrefabFolder + "/Descs";
            PathConfig.EnsureFolderExists(descFolder);

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

                var assetPath = $"{descFolder}/{ctx.PrefabName}{def.Suffix}.asset";
                AssetDatabase.CreateAsset(so, assetPath);

                ctx.GeneratedDescs.Add(so);
                ctx.GeneratedAssetPaths.Add(assetPath);
            }
        }
    }
}
