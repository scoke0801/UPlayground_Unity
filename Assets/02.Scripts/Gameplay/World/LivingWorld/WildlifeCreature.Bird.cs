using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>작은 새의 바운딩 비행·이륙/착지 급박동·착지 후 호핑. <c>UseBirdMotion</c> 프로필에서만 동작한다.</summary>
    public sealed partial class WildlifeCreature
    {
        private const float OffsetBlendRate = 8f;
        private const float GroundOffsetBlendRate = 15f;
        private const float HopGroundProbeUp = 0.5f;
        private const float HopGroundProbeDistance = 1.5f;
        private float _flightOffset, _phaseTime;
        private bool _isGliding;
        private bool _isHopping;
        private float _hopElapsed, _hopWait, _poseTime;
        private AnimationClip _landedClip;
        private Vector3 _hopFrom, _hopTo;

        /// <summary>상하 파동 오프셋을 걷어낸 기준 위치에서 이동을 처리한 뒤 새 오프셋을 다시 얹는다.</summary>
        private void MoveInAir(float deltaTime)
        {
            if (_flightOffset != 0) transform.position -= Vector3.up * _flightOffset;
            MoveInAirCore(deltaTime);
            UpdateFlightOffset(deltaTime);
        }

        private void UpdateFlightOffset(float deltaTime)
        {
            if (!_profile.UseBirdMotion) return;
            bool isCruising = _activity == Activity.Roaming || _activity == Activity.Returning || _activity == Activity.Fleeing;
            float target = isCruising && _airSpeed > 0.1f && !IsFleeing
                ? (_isGliding ? -_profile.UndulationHeight : _profile.UndulationHeight)
                : 0;
            // 지면 가까운 구간에서는 아래로 파고들지 않도록 더 빠르게 0으로 복귀한다.
            float rate = target == 0 ? GroundOffsetBlendRate : OffsetBlendRate;
            _flightOffset = Mathf.Lerp(_flightOffset, target, 1 - Mathf.Exp(-rate * deltaTime));
            if (Mathf.Abs(_flightOffset) < 0.001f && target == 0) _flightOffset = 0;
            transform.position += Vector3.up * _flightOffset;
        }

        /// <summary>순항은 날갯짓-활공을 번갈아 하고, 도주·이륙·착지는 날갯짓 속도를 바꾼다.</summary>
        private void ApplyFlapRhythm(float deltaTime)
        {
            if (!_profile.UseBirdMotion) return;
            var state = _animation.States.Current;
            if (state == null) return;
            bool canGlide = _activity == Activity.Roaming || _activity == Activity.Returning;
            if (!canGlide)
            {
                _isGliding = false;
                _phaseTime = 0;
                bool isBurst = _activity == Activity.TakingOff || _activity == Activity.Landing;
                state.Speed = isBurst ? _profile.BurstFlapSpeed : IsFleeing ? _profile.FleeFlapSpeed : 1;
                return;
            }
            _phaseTime -= deltaTime;
            if (_phaseTime <= 0)
            {
                _isGliding = !_isGliding;
                Vector2 range = _isGliding ? _profile.GlideSeconds : _profile.FlapBurstSeconds;
                _phaseTime = Random.Range(range.x, range.y);
                if (_isGliding) state.NormalizedTime = _profile.GlidePoseNormalizedTime;
            }
            state.Speed = _isGliding ? 0 : 1;
        }

        /// <summary>위협 반대 방향으로 비스듬히 오르는 이륙 목표를 구한다. 수평 거리가 0이면 기존 수직 상승이다.</summary>
        private Vector3 GetTakeoffTarget(Vector3 observer, bool hasObserver, bool mustTakeOff)
        {
            Vector3 target = new Vector3(transform.position.x, _home.y, transform.position.z);
            if (!_profile.UseBirdMotion || !mustTakeOff || _profile.TakeoffForwardDistance <= 0) return target;
            Vector3 away = hasObserver ? transform.position - observer : transform.forward;
            away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            return target + away.normalized * _profile.TakeoffForwardDistance;
        }

        /// <summary>착지한 새가 짧은 포물선으로 뛰어 자세를 바꾼다. 지면과 장애물을 확인한 곳으로만 뛴다.</summary>
        private void UpdateLandedHops(float deltaTime)
        {
            if (!_profile.UseBirdMotion || _profile.HopIntervalSeconds.y <= 0 || _activity != Activity.Landed)
            {
                _isHopping = false;
                return;
            }
            if (_isHopping)
            {
                _hopElapsed += deltaTime;
                // 음수 구간은 웅크리는 동안 제자리에 머무는 시간이다.
                if (_hopElapsed < 0) return;
                float t = Mathf.Clamp01(_hopElapsed / _profile.HopSeconds);
                transform.position = Vector3.Lerp(_hopFrom, _hopTo, t) + Vector3.up * (4 * _profile.HopHeight * t * (1 - t));
                if (t >= 1) { _isHopping = false; _poseTime = 0; ScheduleNextHop(); }
                return;
            }
            _hopWait -= deltaTime;
            if (_hopWait <= 0) TryStartHop();
            if (!_isHopping) UpdateLandedPose(deltaTime);
        }

        /// <summary>착지 중에는 가만히 있기·고개 흔들기·쪼기를 번갈아 재생한다.</summary>
        private void UpdateLandedPose(float deltaTime)
        {
            _poseTime -= deltaTime;
            if (_poseTime <= 0)
            {
                int choice = Random.Range(0, 3);
                _landedClip = choice == 1 && _profile.Observe != null ? _profile.Observe
                    : choice == 2 && _profile.Feed != null ? _profile.Feed
                    : _profile.Idle;
                _poseTime = _landedClip == _profile.Idle ? Random.Range(1.5f, 4f) : _landedClip.length;
            }
            Play(_landedClip);
        }

        private void PlayHopClip()
        {
            if (_profile.Hop == null) return;
            var state = _animation.Play(_profile.Hop, _profile.CrossFade);
            state.Speed = 1;
            state.NormalizedTime = 0;
        }

        private void ScheduleNextHop() =>
            _hopWait = Random.Range(_profile.HopIntervalSeconds.x, _profile.HopIntervalSeconds.y);

        private void TryStartHop()
        {
            ScheduleNextHop();
            Vector2 circle = Random.insideUnitCircle.normalized;
            Vector3 direction = new Vector3(circle.x, 0, circle.y);
            Vector3 from = transform.position;
            Vector3 flatOffset = from - _landingPoint;
            flatOffset.y = 0;
            // 착지점에서 너무 멀어졌으면 돌아오는 쪽으로 뛴다.
            if (flatOffset.magnitude > _profile.HopRadius) direction = -flatOffset.normalized;
            Vector3 destination = from + direction * Random.Range(_profile.HopDistance.x, _profile.HopDistance.y);
            if (!Physics.Raycast(destination + Vector3.up * HopGroundProbeUp, Vector3.down, out var ground,
                    HopGroundProbeDistance, _profile.LandingMask, QueryTriggerInteraction.Ignore)
                || ground.rigidbody != null || Vector3.Angle(ground.normal, Vector3.up) > _profile.MaxLandingSlope) return;
            destination.y = ground.point.y + _profile.GroundClearance;
            Vector3 probeOrigin = from + Vector3.up * AirObstacleRadius;
            if (Physics.SphereCast(probeOrigin, AirObstacleRadius * 0.5f, direction,
                    out _, Vector3.Distance(from, destination) + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            _hopFrom = from;
            _hopTo = destination;
            _hopElapsed = _profile.Hop != null ? -_profile.HopWindupSeconds : 0;
            _isHopping = true;
            PlayHopClip();
            // 뛰는 순간 몸을 진행 방향으로 돌려 새가 방향을 바꾸는 모습을 만든다.
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
