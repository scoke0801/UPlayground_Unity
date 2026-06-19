using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/소스/Collider Exit")]
    public sealed class ColliderExitTriggerSourceSO : TriggerSourceSO
    {
        [SerializeField] private ActorType _actorFilter = ActorType.Player;
        [SerializeField] private string _fallbackTag = "Player";

        public override void HandleTriggerExit(TriggerComposer composer, Collider other, Action<TriggerContext> onFire)
        {
            if (composer == null || other == null)
                return;

            var actor = other.GetComponent<GameActor>();
            if (actor != null)
            {
                if (_actorFilter != ActorType.None && !actor.HasActorType(_actorFilter))
                    return;
            }
            else if (!string.IsNullOrEmpty(_fallbackTag) && !other.CompareTag(_fallbackTag))
            {
                return;
            }

            onFire?.Invoke(new TriggerContext(composer, this).WithCollider(other, actor));
        }
    }
}
