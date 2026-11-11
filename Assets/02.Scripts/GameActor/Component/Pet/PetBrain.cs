using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Data.Combat;

namespace UPlayGround.Components.Pet
{
    /// <summary>중단·복귀·전투 대기·회수·추종 순으로 동행 행동을 선택한다.</summary>
    public sealed partial class PetBrain : IManagedTick
    {
        private readonly PetActor _actor;
        private readonly PlayerActor _player;
        private readonly PetMovementController _movement;
        private readonly PlayerMovementController _playerMovement;
        private readonly PetFollowProfileSO _profile;
        private readonly PetDefinitionSO _definition;
        private readonly PetFollowController _follow;
        private readonly PetLootCollector _loot;
        private readonly PetPresentation _presentation;
        private readonly UPlayGround.Animation.MotionSetAsset _seatedMotion;
        private float _decisionTime;
        private float _nextRecovery;
        private bool _suspended;
        private bool _hasSpawned;
        private readonly PetRecallGate _recall = new();
        public int GroundRecallCount { get; private set; }
        public int SpawnPlacementCount { get; private set; }
        public int BoardingCount { get; private set; }
        public bool HasSafeGround { get; private set; }
        public bool CanSupport => !_suspended && !_isSwimming && !_awaitingGround && _seatView == null
            && HasSafeGround && _movement.Motor != null && _movement.Motor.enabled
            && _movement.Motor.GroundingStatus.IsStableOnGround
            && _playerMovement != null && !_playerMovement.IsTraversing && !_playerMovement.IsSwimming
            && _playerMovement.Motor.GroundingStatus.IsStableOnGround;

        public PetBrain(PetActor actor, PlayerActor player, PetMovementController movement,
            PetDefinitionSO definition, IGroundDropService drops, PetPresentation presentation)
        {
            _actor = actor; _player = player; _movement = movement; _profile = definition.followProfile;
            _definition = definition;
            _playerMovement = player.ActorController as PlayerMovementController;
            _presentation = presentation;
            _seatedMotion = definition.seatedMotion;
            _follow = new PetFollowController(movement, _profile);
            _loot = new PetLootCollector(actor.transform, definition.lootProfile, _follow, drops);
            SetHidden(true);
        }

        public void SetSuspended(bool suspended, bool hide = true)
        {
            if (_suspended == suspended)
            { if (suspended && hide) SetHidden(true); return; }
            _suspended = suspended;
            if (suspended)
            {
                Cancel(); LeaveSeat(); _recall.ResetContinuity();
                if (hide) SetHidden(true);
            }
            else { _decisionTime = _profile.decisionInterval; }
        }
        public void Cancel() { _loot.Cancel(); _follow.Stop(); }
        public void Dispose() { Cancel(); LeaveSeat(); }
        public void PreserveGroundPose(Vector3 position, Quaternion rotation)
        {
            _actor.PlaceAtPose(position, rotation);
            _hasSpawned = true; HasSafeGround = true;
            _recall.ResetContinuity();
        }

        public void ManagedTick(float deltaTime)
        {
            if (_suspended || deltaTime <= 0f || _player == null) return;
            if (TickTraversal(deltaTime)) { _recall.ResetContinuity(); _decisionTime = 0f; return; }
            _decisionTime += deltaTime;
            if (_decisionTime < _profile.decisionInterval) return;
            float elapsed = _decisionTime; _decisionTime = 0f;
            if (_player.ActorController?.Motor == null) return;
            bool grounded = _player.ActorController.Motor.GroundingStatus.IsStableOnGround;
            float distance = Vector3.Distance(_actor.transform.position, _player.transform.position);
            bool canRecall = _recall.Tick(distance, grounded, elapsed, _profile);
            if (!grounded) { Cancel(); return; }
            if (_hasSpawned) SetHidden(false);
            if (!_follow.TryGetFollowPoint(_player.transform, out var target))
            { Cancel(); return; }
            if (!_hasSpawned || canRecall)
            {
                Cancel();
                if (Time.time < _nextRecovery) return;
                _nextRecovery = Time.time + _profile.recoveryInterval;
                // 첫 동행은 가까운 추종 위치에서 확인할 수 있게 하고, 원거리 복귀만 시야 밖을 선호한다.
                if (_hasSpawned && Services.TryGet<ICameraViewService>(out var cameras))
                {
                    var camera = cameras.GetMainCamera();
                    Vector3 opposite = _player.transform.position * 2f - target;
                    if (camera != null && IsInView(camera, target)
                        && _follow.TrySafePoint(opposite, _player.transform.position, out var other)
                        && !IsInView(camera, other)) target = other;
                }
                _actor.PlaceAtPose(target, Quaternion.LookRotation(_player.transform.forward, Vector3.up));
                if (_hasSpawned) GroundRecallCount++; else SpawnPlacementCount++;
                _hasSpawned = true; HasSafeGround = true;
                _recall.MarkRecalled(_profile);
                SetHidden(false);
                PlayArrivalEffect();
                return;
            }
            SetHidden(false);
            bool inCombat = Services.TryGet<ICombatEncounterService>(out var encounter)
                && encounter.CurrentEncounter != CombatEncounterKind.Explore;
            if (inCombat) _loot.Cancel();
            else if (_loot.TryTick(_player.transform, elapsed)) return;
            _follow.Tick(target, elapsed, _profile.stopDistance, true);
        }

        private void PlayArrivalEffect()
        {
            if (string.IsNullOrWhiteSpace(_definition.arrivalFxKey)) return;
            var effect = ActorSvc.Objects?.ShowFX(_definition.arrivalFxKey,
                _actor.transform.position + _actor.transform.rotation * _definition.arrivalFxOffset,
                duration: _definition.arrivalFxDuration);
            if (effect != null) effect.transform.localScale *= _definition.arrivalFxScale;
        }

        private void SetHidden(bool hidden)
        {
            _presentation?.SetVisible(!hidden);
            _actor.Animator?.SetSimulationPaused(hidden);
            if (_movement.Motor != null) _movement.Motor.enabled = !hidden;
            if (_movement.Motor?.Capsule != null) _movement.Motor.Capsule.enabled = !hidden;
        }
        private static bool IsInView(Camera camera, Vector3 point)
        {
            Vector3 viewport = camera.WorldToViewportPoint(point);
            return viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        }
    }
}
