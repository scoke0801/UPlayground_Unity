using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    public sealed class InstantiateBaseStep : IBuildStep
    {
        private readonly NameSequenceRegistry _registry;

        public InstantiateBaseStep(NameSequenceRegistry registry)
        {
            _registry = registry;
        }

        public void Execute(BuildContext ctx)
        {
            var path = PathConfig.GetBasePrefabPath(ctx.Config.Sex, ctx.Config.UseMagicaCloth);
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (basePrefab == null)
                throw new BuildException($"베이스 프리팹을 찾을 수 없습니다: {path}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            if (instance == null)
                throw new BuildException("베이스 프리팹 인스턴스화에 실패했습니다.");

            // Completely 모드로 중첩 프리팹까지 모두 해제. SaveAsPrefabAsset 가 nested 인스턴스 때문에
            // 실패하는 경우를 방지한다.
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            ctx.RootInstance = instance;

            // 이름 미리 계산해서 폴더/이름을 확정
            var name = CharacterNameGenerator.Generate(ctx.Config, _registry);
            ctx.PrefabName = name;
            ctx.Bag["tempName"] = name;

            var kindFolder = CharacterNameGenerator.GetKindFolderName(ctx.Config.ActorKind);
            ctx.PrefabFolder = PathConfig.GetPrefabFolder(kindFolder, name);

            instance.name = name;
        }
    }
}
