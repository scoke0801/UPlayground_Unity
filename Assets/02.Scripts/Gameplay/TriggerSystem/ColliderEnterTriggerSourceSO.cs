using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Source/Collider Enter")]
    public sealed class ColliderEnterTriggerSourceSO : TriggerSourceSO
    {
        [SerializeField] private ActorType _actorFilter = ActorType.Player;
        [SerializeField] private string _fallbackTag = "Player";

        public override void HandleTriggerEnter(TriggerComposer composer, Collider other, Action<TriggerContext> onFire)
        {
            if (!TryBuildContext(composer, other, out var context))
                return;

            onFire?.Invoke(context);
        }

        private bool TryBuildContext(TriggerComposer composer, Collider other, out TriggerContext context)
        {
            context = null;
            if (composer == null || other == null)
                return false;

            var actor = other.GetComponent<GameActor>();
            if (actor != null)
            {
                if (_actorFilter != ActorType.None && !actor.HasActorType(_actorFilter))
                    return false;
            }
            else if (!string.IsNullOrEmpty(_fallbackTag) && !other.CompareTag(_fallbackTag))
            {
                return false;
            }

            context = new TriggerContext(composer, this).WithCollider(other, actor);
            return true;
        }
    }
}
