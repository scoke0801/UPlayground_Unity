using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Event;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    public sealed class RemainsActor : GameActor, IInteractable
    {
        private string _remainsId;
        public void Initialize(string remainsId) => _remainsId = remainsId;
        protected override void Awake() { base.Awake(); _actorType = ActorType.Obstacle; }
        public void Interact(GameActor user) => CycleRemainsManager.Instance?.TryRecover(_remainsId);
        public void StopInteract() { }
        public bool CanInteract() => !string.IsNullOrEmpty(_remainsId);
        public bool IsInteracting() => false;
        public Transform GetInteractionTransform() => transform;
        public GameActor GetActor() => this;
        public InteractableActorSO GetData() => null;
        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data) where TData : IEventData { }
    }

    public static class CycleRemainsMarkerRegistry
    {
        public static event System.Action<Vector3> OnMarkerChanged;
        public static event System.Action OnMarkerRemoved;
        public static bool HasMarker { get; private set; }
        public static Vector3 Position { get; private set; }
        public static void Set(Vector3 position) { HasMarker = true; Position = position; OnMarkerChanged?.Invoke(position); }
        public static void Clear() { if (!HasMarker) return; HasMarker = false; OnMarkerRemoved?.Invoke(); }
    }
}
