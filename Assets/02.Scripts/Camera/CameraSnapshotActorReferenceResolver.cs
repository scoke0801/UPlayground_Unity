using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.CameraSystem
{
    public static class CameraSnapshotActorReferenceResolver
    {
        public static Transform Resolve(CameraSnapshotActorReference reference, Transform fallback = null)
        {
            if (!reference.enabled)
                return fallback;

            IWorldActor actor = ResolveActor(reference);
            if (actor == null)
                return fallback;

            if (reference.socketType != ActorSocketType.None
                && actor.TryGetSocket(reference.socketType, out Transform socket)
                && socket != null)
            {
                return socket;
            }

            return actor.Transform;
        }

        public static IWorldActor ResolveActor(CameraSnapshotActorReference reference)
        {
            if (!reference.enabled)
                return null;

            string actorId = reference.ResolvedActorId;

            if (string.IsNullOrEmpty(actorId) && reference.useActivePlayerWhenEmpty)
                return Svc.ActorQuery?.Player;

            if (string.IsNullOrEmpty(actorId))
                return null;

            return Svc.ActorQuery?.FindActor(actorId);
        }
    }
}
