using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Data.Combat;

namespace UPlayGround.Components.Pet
{
    /// <summary>중단·복귀·전투 대기·회수·추종 순으로 동행 행동을 선택한다.</summary>
    public sealed class PetBrain : IManagedTick
    {
        private readonly PetActor _actor;
        private readonly PlayerActor _player;
        private readonly PetMovementController _movement;
        private readonly PetFollowProfileSO _profile;
        private readonly PetFollowController _follow;
        private readonly PetLootCollector _loot;
        private readonly PetPresentation _presentation;
        private float _decisionTime;
        private float _nextRecovery;
        private bool _suspended;
        public bool HasSafeGround { get; private set; }

        public PetBrain(PetActor actor, PlayerActor player, PetMovementController movement,
            PetDefinitionSO definition, IGroundDropService drops, PetPresentation presentation)
        {
            _actor = actor; _player = player; _movement = movement; _profile = definition.followProfile;
            _presentation = presentation;
            _follow = new PetFollowController(movement, _profile);
            _loot = new PetLootCollector(actor.transform, definition.lootProfile, _follow, drops);
            SetHidden(true);
        }

        public void SetSuspended(bool suspended)
        {
            if (_suspended == suspended) return;
            _suspended = suspended;
            if (suspended) { Cancel(); SetHidden(true); }
            else { _nextRecovery = 0f; _decisionTime = _profile.decisionInterval; }
        }
        public void Cancel() { _loot.Cancel(); _follow.Stop(); }

        public void ManagedTick(float deltaTime)
        {
            if (_suspended || deltaTime <= 0f || _player == null) return;
            _decisionTime += deltaTime;
            if (_decisionTime < _profile.decisionInterval) return;
            float elapsed = _decisionTime; _decisionTime = 0f;
            if (_player.ActorController?.Motor == null || !_player.ActorController.Motor.GroundingStatus.IsStableOnGround)
            { Cancel(); SetHidden(true); return; }
            if (!_follow.TryGetFollowPoint(_player.transform, out var target))
            { Cancel(); SetHidden(true); return; }
            float distance = Vector3.Distance(_actor.transform.position, _player.transform.position);
            if (!HasSafeGround || distance >= _profile.returnDistance || _follow.NeedsRecovery)
            {
                Cancel(); SetHidden(true);
                if (Time.time < _nextRecovery) return;
                _nextRecovery = Time.time + _profile.recoveryInterval;
                if (Services.TryGet<ICameraViewService>(out var cameras))
                {
                    var camera = cameras.GetMainCamera();
                    Vector3 opposite = _player.transform.position * 2f - target;
                    if (camera != null && IsInView(camera, target)
                        && _follow.TrySafePoint(opposite, _player.transform.position, out var other)
                        && !IsInView(camera, other)) target = other;
                }
                _actor.PlaceAtPose(target, Quaternion.LookRotation(_player.transform.forward, Vector3.up));
                SetHidden(false);
                return;
            }
            bool inCombat = Services.TryGet<ICombatEncounterService>(out var encounter)
                && encounter.CurrentEncounter != CombatEncounterKind.Explore;
            if (inCombat) _loot.Cancel();
            else if (_loot.TryTick(_player.transform, elapsed)) return;
            _follow.Tick(target, elapsed, _profile.stopDistance, true);
        }

        private void SetHidden(bool hidden)
        {
            HasSafeGround = !hidden;
            _presentation?.SetVisible(!hidden);
            _actor.Animator?.SetSimulationPaused(hidden);
            if (_movement.Motor != null) _movement.Motor.enabled = !hidden;
        }
        private static bool IsInView(Camera camera, Vector3 point)
        {
            Vector3 viewport = camera.WorldToViewportPoint(point);
            return viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        }
    }
}
