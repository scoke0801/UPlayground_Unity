using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.CameraSystem
{
    public static class CameraSnapshotActorReferenceResolver
    {
        public static Transform Resolve(CameraSnapshotActorReference reference, Transform fallback = null)
        {
            if (!reference.enabled)
                return fallback;

            Transform actor = ResolveActor(reference);
            if (actor == null)
                return fallback;

            if (reference.socketType != ActorSocketType.None
                && CameraRuntimeServices.Adapter.TryGetSocket(
                    actor,
                    reference.socketType,
                    out Transform socket)
                && socket != null)
            {
                return socket;
            }

            return actor;
        }

        private static Transform ResolveActor(CameraSnapshotActorReference reference)
        {
            if (!reference.enabled)
                return null;

            string actorId = reference.ResolvedActorId;

            if (string.IsNullOrEmpty(actorId) && reference.useActivePlayerWhenEmpty)
                return CameraRuntimeServices.Adapter.ActivePlayer;

            if (string.IsNullOrEmpty(actorId))
                return null;

            return CameraRuntimeServices.Adapter.FindActor(actorId);
        }
    }
}
