using UnityEngine;
using UPlayGround.State;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Components.Pet
{
    public sealed partial class PetBrain
    {
        private PlayerGliderView _seatView;
        private PlayerGliderView _attemptedView;
        private bool _isSwimming;
        private float _stableGroundTime;
        private float _boardingTime;
        private Vector3 _lastGroundPosition;
        private Quaternion _lastGroundRotation;
        private bool _hasGroundPose;
        private bool _awaitingGround;

        private bool TickTraversal(float deltaTime)
        {
            var controller = _playerMovement;
            if (controller == null) return false;
            bool grounded = controller.Motor != null && controller.Motor.GroundingStatus.IsStableOnGround;
            if (controller.IsSwimming)
            {
                LeaveSeat(); Cancel(); SetHidden(true);
                _isSwimming = true; _stableGroundTime = 0f; _recall.ResetContinuity();
                return true;
            }
            if (_isSwimming || _awaitingGround)
            {
                _stableGroundTime = grounded ? _stableGroundTime + deltaTime : 0f;
                if (_stableGroundTime < _profile.swimExitDelay) return true;
                _isSwimming = false; _awaitingGround = false;
                if (_hasSpawned) SetHidden(false);
            }
            bool gliding = controller.CurrentState is PlayerSuperJumpGlidingState { IsGliding: true };
            var view = gliding ? controller.GliderBinding?.ActiveView : null;
            if (_seatView != null && _seatView != view) LeaveSeat();
            if (!gliding) _attemptedView = null;
            if (gliding)
            {
                if (view == null || view.PetSeatAnchor == null || _seatedMotion == null || !_hasGroundPose)
                { Cancel(); SetHidden(true); _awaitingGround = true; return true; }
                if (_attemptedView != view)
                {
                    Cancel(); _attemptedView = view; _seatView = view;
                    _seatView.ReleasingPassengers += LeaveSeat;
                    _boardingTime = _profile.boardingDuration;
                    _movement.Motor.BaseVelocity = Vector3.zero;
                    SetHidden(true);
                }
                if (_seatView == null) return true;
                if (_boardingTime > 0f)
                {
                    _boardingTime -= deltaTime;
                    if (_boardingTime > 0f) return true;
                    _actor.transform.SetParent(view.PetSeatAnchor, true);
                    _actor.transform.SetPositionAndRotation(view.PetSeatAnchor.position, view.PetSeatAnchor.rotation);
                    BoardingCount++;
                    _presentation?.SetVisible(true);
                    _actor.Animator?.SetSimulationPaused(false);
                    _actor.Animator?.PlayMotion(_seatedMotion);
                    PlayArrivalEffect();
                }
                return true;
            }
            if (_hasSpawned && _movement.Motor != null && _movement.Motor.GroundingStatus.IsStableOnGround)
            {
                _lastGroundPosition = _actor.transform.position;
                _lastGroundRotation = _actor.transform.rotation;
                _hasGroundPose = true;
            }
            return false;
        }

        private void LeaveSeat()
        {
            if (ReferenceEquals(_seatView, null)) return;
            var view = _seatView; _seatView = null;
            view.ReleasingPassengers -= LeaveSeat;
            _actor.transform.SetParent(null, true);
            _movement.Motor.BaseVelocity = Vector3.zero;
            var controller = _player != null ? _playerMovement : null;
            bool landed = controller?.Motor != null && controller.Motor.GroundingStatus.IsStableOnGround;
            if (!landed)
            {
                if (_hasGroundPose) _actor.PlaceAtPose(_lastGroundPosition, _lastGroundRotation);
                SetHidden(true); _awaitingGround = true; _stableGroundTime = 0f;
                return;
            }
            // KCC 보간 기준만 현재 월드 자세로 재설정하고 지면으로 낙하한다.
            _actor.PlaceAtPose(_actor.transform.position, _actor.transform.rotation);
            SetHidden(false);
            _actor.Animator?.PlayMotion(MotionTags.Idle);
        }
    }
}
