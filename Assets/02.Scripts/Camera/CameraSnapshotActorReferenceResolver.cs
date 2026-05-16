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

            GameActor actor = ResolveActor(reference);
            if (actor == null)
                return fallback;

            if (reference.socketType != ActorSocketType.None
                && actor.TryGetSocket(reference.socketType, out Transform socket)
                && socket != null)
            {
                return socket;
            }

            return actor.transform;
        }

        public static GameActor ResolveActor(CameraSnapshotActorReference reference)
        {
            if (!reference.enabled)
                return null;

            string actorId = reference.ResolvedActorId;

            if (string.IsNullOrEmpty(actorId) && reference.useActivePlayerWhenEmpty)
                return GameObjectManager.Instance != null ? GameObjectManager.Instance.Player : null;

            if (string.IsNullOrEmpty(actorId))
                return null;

            if (GameObjectManager.Instance != null)
            {
                foreach (GameActor actor in GameObjectManager.Instance.AllActors)
                {
                    if (actor != null && actor.ActorId == actorId)
                        return actor;
                }
            }

            if (ActorSpawnManager.Instance != null)
            {
                var spawnedActors = ActorSpawnManager.Instance.GetSpawnedActors(actorId);
                if (spawnedActors != null && spawnedActors.Count > 0)
                    return spawnedActors[0];
            }

            return null;
        }
    }
}
