using System;
using System.Collections.Generic;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Story;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>새 게임 주인공에 대응하는 제1장 최종 보스 정의를 제공합니다.</summary>
    [FlowNodeMenu(
        "스토리/다른 가능성 Variant",
        Summary = "StoryProtagonistType에 명시적으로 매핑된 최종 보스 정의를 제공합니다.",
        Keywords = new[] { "story", "protagonist", "boss", "주인공", "보스" })]
    [Serializable]
    public sealed class AlternateSelfVariantNode : FlowDataNode
    {
        public const string BossActorPort = "BossActor";
        public const string ActorIdPort = "ActorId";
        public const string IsConfiguredPort = "IsConfigured";

        public AlternateSelfVariantSetSO variantSet;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.DataOutput<ActorDefinitionSO>(
                    BossActorPort,
                    displayName: "보스 액터");
                yield return FlowPortDef.DataOutput<string>(
                    ActorIdPort,
                    displayName: "Actor ID");
                yield return FlowPortDef.DataOutput<bool>(
                    IsConfiguredPort,
                    displayName: "매핑됨");
            }
        }

        public override bool TryEvaluate(
            FlowContext context,
            FlowGraphSO graph,
            string outputPortId,
            out object value)
        {
            CharacterActorType protagonist =
                Services.TryGet<IPartyService>(out IPartyService party)
                    ? party.StoryProtagonistType
                    : CharacterActorType.None;
            ActorDefinitionSO bossActor = null;
            bool isConfigured = variantSet != null
                                && variantSet.TryGetVariant(
                                    protagonist,
                                    out bossActor);

            switch (outputPortId)
            {
                case BossActorPort:
                    value = bossActor;
                    return isConfigured;
                case ActorIdPort:
                    value = isConfigured ? bossActor.actorId : string.Empty;
                    return isConfigured;
                case IsConfiguredPort:
                    value = isConfigured;
                    return true;
                default:
                    value = null;
                    return false;
            }
        }
    }
}
