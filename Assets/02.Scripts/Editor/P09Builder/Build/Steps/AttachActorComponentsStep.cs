namespace Game.Editor.P09Builder
{
    public sealed class AttachActorComponentsStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx.RootInstance == null)
                throw new BuildException("RootInstance가 null입니다 (AttachActorComponentsStep).");

            var template = ActorTemplateFactory.Get(ctx.Config.ActorKind);
            template.AttachComponents(ctx.RootInstance, ctx.Config);
        }
    }
}
