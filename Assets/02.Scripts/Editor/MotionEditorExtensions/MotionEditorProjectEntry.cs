using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation.Editor
{
    public static class MotionEditorProjectEntry
    {
        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/캐릭터/액터/애니메이션 에디터",
            priority = 101)]
        public static void OpenWindow()
        {
            MotionSetEditorWindow.Open((MotionSetAsset)null);
        }

        public static void Open(ActorAnimationMotionSet actorSet)
        {
            MotionSetEditorWindow.Open(
                actorSet != null
                    ? new ActorAnimationMotionSetCatalog(actorSet)
                    : null);
        }

        public static void Open(PlayerActorAnimationMotionSet playerSet)
        {
            MotionSetEditorWindow.Open(
                playerSet != null
                    ? new PlayerActorAnimationMotionSetCatalog(
                        playerSet,
                        null)
                    : null);
        }

        public static void Open(
            ActorAnimationMotionSet actorSet,
            GameplayTag slot,
            MotionSetAsset asset)
        {
            MotionSetEditorWindow.Open(
                actorSet != null
                    ? new ActorAnimationMotionSetCatalog(actorSet)
                    : null,
                slot.TagName,
                asset);
        }
    }
}
