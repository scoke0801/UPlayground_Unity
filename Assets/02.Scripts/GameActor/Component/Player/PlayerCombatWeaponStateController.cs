using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Components
{
    /// <summary>
    /// PlayerCombat의 전투 상태 변화에 맞춰 주 무기를 손/등 위치로 전환한다.
    /// 공격, 피격처럼 즉시 반응해야 하는 상태에서는 요청만 보관하고 안전 상태에서 처리한다.
    /// </summary>
    [RequireComponent(typeof(PlayerCombat))]
    public class PlayerCombatWeaponStateController : PlayerActorComponent
    {
        private PlayerActor _player;
        private PlayerCombat _combat;
        private PlayerEquipment _equipment;
        private ActorAnimator _animator;

        private bool? _pendingDrawn;
        private bool _isSubscribed;
        private bool _isPlayingDrawMotion;
        private bool _trailsSuppressedForNonCombat;

        private void Awake()
        {
            _player = GetComponent<PlayerActor>();
            RefreshReferences();
        }

        private void OnEnable()
        {
            RefreshReferences();
            SubscribeCombat();
        }

        private void OnDisable()
        {
            UnsubscribeCombat();
        }

        private void Update()
        {
            if (_combat != null && !_combat.IsInCombat && !_trailsSuppressedForNonCombat)
                SuppressAttackTrails();

            if (_isPlayingDrawMotion && !IsSafeState())
            {
                _equipment?.CancelMainWeaponDrawMotionRequest();
                _isPlayingDrawMotion = false;
            }

            if (_pendingDrawn.HasValue && CanPlayNow())
            {
                bool drawn = _pendingDrawn.Value;
                _pendingDrawn = null;
                PlayDrawMotion(drawn);
            }
        }

        public void RefreshReferences()
        {
            UnsubscribeCombat();

            if (_player == null)
                _player = GetComponent<PlayerActor>();

            _combat = _player != null ? _player.GetCombat() : GetComponent<PlayerCombat>();
            _equipment = _player != null ? _player.GetPlayerEquipment() : GetComponentInChildren<PlayerEquipment>();
            _animator = _player != null ? _player.Animator : GetComponentInChildren<ActorAnimator>();

            if (isActiveAndEnabled)
                SubscribeCombat();
        }

        private void SubscribeCombat()
        {
            if (_isSubscribed || _combat == null)
                return;

            _combat.OnChangeCombatState += OnCombatStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeCombat()
        {
            if (!_isSubscribed || _combat == null)
                return;

            _combat.OnChangeCombatState -= OnCombatStateChanged;
            _isSubscribed = false;
        }

        private void OnCombatStateChanged(bool isInCombat)
        {
            if (!isInCombat)
                SuppressAttackTrails();
            else
                _trailsSuppressedForNonCombat = false;

            RequestDrawn(isInCombat);
        }

        private void SuppressAttackTrails()
        {
            RefreshReferences();

            if (_equipment != null)
                ActorWeaponTrailController.SuppressAttackTrails(_equipment);

            if (_player != null)
                ActorWeaponTrailController.SuppressAttackTrails(_player);

            _trailsSuppressedForNonCombat = true;
        }

        private void RequestDrawn(bool drawn)
        {
            RefreshReferences();

            if (_equipment == null || !_equipment.CanToggleMainWeapon())
                return;

            if (!CanPlayNow())
            {
                _pendingDrawn = drawn;
                return;
            }

            PlayDrawMotion(drawn);
        }

        private void PlayDrawMotion(bool drawn)
        {
            if (_equipment == null || !_equipment.CanToggleMainWeapon())
                return;

            if (_equipment.IsMainWeaponEquipped == drawn)
                return;

            _isPlayingDrawMotion = true;
            bool motionPlayed = _equipment.TryPlayMainWeaponDrawMotion(drawn, _animator, () =>
            {
                _isPlayingDrawMotion = false;
                PlayCurrentStateMotionIfSafe();
            });

            if (!motionPlayed)
                _isPlayingDrawMotion = false;
        }

        private bool CanPlayNow()
        {
            return !_isPlayingDrawMotion && IsSafeState();
        }

        private bool IsSafeState()
        {
            if (_player == null || _player.PlayerController == null)
                return false;

            string stateName = _player.PlayerController.CurrentState?.StateName;
            return stateName is "Idle" or "GroundMove" or "Stop" or "TurnInPlace";
        }

        private void PlayCurrentStateMotionIfSafe()
        {
            if (_animator == null || _player == null || _player.PlayerController == null)
                return;

            string stateName = _player.PlayerController.CurrentState?.StateName;
            switch (stateName)
            {
                case "Idle":
                    _animator.PlayMotion(AnimKey.Idle, 0.1f);
                    break;
                case "GroundMove":
                    _animator.PlayMotion(GetMoveAnimKey(), 0.1f);
                    break;
            }
        }

        private AnimKey GetMoveAnimKey()
        {
            return _player.MoveAnimType switch
            {
                BaseMoveAnimType.Walk => AnimKey.Walk,
                BaseMoveAnimType.Sprint => AnimKey.Sprint,
                _ => AnimKey.Run,
            };
        }
    }
}
