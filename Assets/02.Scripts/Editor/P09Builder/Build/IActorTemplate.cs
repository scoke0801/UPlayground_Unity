using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public interface IDescDef
    {
        Type DescType { get; }
        string Suffix { get; }
        void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config);
    }

    public interface IActorTemplate
    {
        BuilderActorKind Kind { get; }
        void AttachComponents(GameObject root, CharacterBuildConfig config);
        IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config);
        void WireDescAssets(GameObject root, List<ScriptableObject> generatedDescs, CharacterBuildConfig config);
    }

    public static class ActorTemplateFactory
    {
        public static IActorTemplate Get(BuilderActorKind kind)
        {
            switch (kind)
            {
                case BuilderActorKind.Enemy:  return new EnemyActorTemplate();
                case BuilderActorKind.Player: return new PlayerActorTemplate();
                case BuilderActorKind.Npc:    return new NpcActorTemplate();
                default:
                    throw new BuildException($"Unknown actor kind: {kind}");
            }
        }
    }
}
