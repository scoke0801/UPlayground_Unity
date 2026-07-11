namespace UPlayGround.Editor.P09Builder
{
    public sealed class AssignStatsStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx.RootInstance == null)
                throw new BuildException("RootInstance가 null입니다 (AssignStatsStep).");

            var template = ActorTemplateFactory.Get(ctx.Config.ActorKind);
            template.WireDescAssets(ctx.RootInstance, ctx.GeneratedDescs, ctx.Config);
        }
    }
}
