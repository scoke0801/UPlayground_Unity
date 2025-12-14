using UnityEngine;

namespace Actor
{
    public abstract class BaseActor : MonoBehaviour
    {
        protected long _actorKey;

        public long ActorKey => _actorKey;
    }
}

