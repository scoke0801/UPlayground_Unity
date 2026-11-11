using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.Manager;

namespace UPlayGround.Components.Pet
{
    /// <summary>도달 가능한 드롭 한 개만 예약하고 근접 획득을 요청한다.</summary>
    public sealed class PetLootCollector
    {
        private readonly PetLootProfileSO _profile;
        private readonly PetFollowController _follow;
        private readonly Transform _pet;
        private readonly IGroundDropService _drops;
        private string _targetId;
        private Transform _target;
        private float _nextSearch;
        private float _reservationEnd;
        private float _retryAfter;
        private readonly IPetService _pets;
        private float SearchRadius => _pets?.PickupRadius ?? _profile.searchRadius;
        private float Leash => _pets?.LootLeash ?? _profile.maxPlayerDistance;
        private float SearchInterval => _pets?.PickupInterval ?? _profile.searchInterval;

        public PetLootCollector(Transform pet, PetLootProfileSO profile, PetFollowController follow, IGroundDropService drops)
        {
            _pet = pet; _profile = profile; _follow = follow; _drops = drops;
            Services.TryGet(out _pets);
        }

        public void Cancel()
        {
            bool hadTarget = _targetId != null;
            if (_targetId != null) _drops?.Release(_targetId, this);
            _target = null; _targetId = null;
            if (hadTarget) _follow.Stop();
        }

        public bool TryTick(Transform player, float deltaTime)
        {
            if (_profile == null || _drops == null) return false;
            if (_pets?.IsAutoPickupEnabled == false || SearchRadius <= 0f || SearchInterval <= 0f)
            { Cancel(); return false; }
            if (_targetId != null)
            {
                if (_target == null || !_target.gameObject.activeInHierarchy || Time.time >= _reservationEnd
                    || (_target.position - player.position).sqrMagnitude > Leash * Leash
                    || (_pet.position - player.position).sqrMagnitude > Leash * Leash)
                { Cancel(); return false; }
                if ((_target.position - _pet.position).sqrMagnitude <= _profile.arrivalDistance * _profile.arrivalDistance)
                {
                    bool collected = _drops.TryPetCollect(_targetId, this, _pet.position, _profile.arrivalDistance);
                    Cancel();
                    _nextSearch = Time.time + SearchInterval;
                    if (!collected) _retryAfter = Time.time + _profile.retryDelay;
                    return collected;
                }
                if (!_follow.Tick(_target.position, deltaTime, _profile.arrivalDistance, false))
                { Cancel(); _retryAfter = Time.time + _profile.retryDelay; return false; }
                return true;
            }
            if (Time.time < _nextSearch || Time.time < _retryAfter) return false;
            _nextSearch = Time.time + SearchInterval;
            var drops = _drops.VisibleDrops;
            float nearest = float.MaxValue;
            GroundDropView chosen = default;
            for (int i = 0; i < drops.Count; i++)
            {
                var drop = drops[i];
                if (drop.Transform == null || (drop.Transform.position - player.position).sqrMagnitude
                    > SearchRadius * SearchRadius) continue;
                float distance = (drop.Transform.position - _pet.position).sqrMagnitude;
                if (distance >= nearest || !_follow.CanReach(drop.Transform.position)) continue;
                chosen = drop; nearest = distance;
            }
            if (chosen.Transform == null || !_drops.TryReserve(chosen.Id, this, _profile.reservationSeconds)) return false;
            _targetId = chosen.Id; _target = chosen.Transform;
            _reservationEnd = Time.time + _profile.reservationSeconds;
            _follow.Stop();
            return true;
        }
    }
}
