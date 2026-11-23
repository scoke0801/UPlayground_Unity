using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Sound;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround.Components
{
    /// <summary>평가가 끝난 발 포즈와 실제 바닥을 연결해 모델별 환경 효과를 재생한다.</summary>
    public sealed class ActorFootstep : ActorComponent
    {
        public FootstepSettingsSO settings;
        private static readonly Dictionary<GameObject, ActorFootstep> Receivers = new();
        private readonly Dictionary<FootstepSurfaceProfileSO.Response, int> _previousSounds = new();
        private GameActor _actor;
        private PlayerActor _player;
        private KinematicCharacterMotor _motor;
        private Transform _leftFoot;
        private Transform _rightFoot;
        private Transform _leftToes;
        private Transform _rightToes;
        private FootContact _leftContact, _rightContact;
        private LocomotionFoot _leftStride, _rightStride;
        private PoseFoot _leftPose, _rightPose;
        private Vector3 _strideDirection;
        private Vector3 _lastRoot;
        private bool _hasRoot;
        private float _landUntil = float.NegativeInfinity;
        private readonly RaycastHit[] _soleHits = new RaycastHit[16];
        private FootstepEffectPool _effects;
        private float _lastStepTime = float.NegativeInfinity;
        private float _lastLandingTime = float.NegativeInfinity;

        public int PlayedCount { get; private set; }
        public FootstepSurfaceProfileSO LastSurface { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public static event System.Action<RaycastHit, Vector3, Vector2, FootstepKind, bool, bool> SandContact;

        private struct FootContact
        {
            public bool Planted;
            public int StampFrame;
            public FootstepKind Kind;
            public float PendingUntil;

            public bool CanConfirm(int frame) => !Planted && StampFrame != frame;

            public bool IsPending(float time) => PendingUntil > 0f && time <= PendingUntil;

            public bool TryConfirm(int frame, float gap, float maxGap)
            {
                if (!CanConfirm(frame) || gap > maxGap || gap < -0.09f) return false;
                Planted = true;
                StampFrame = frame;
                PendingUntil = 0f;
                return true;
            }

            public void Release()
            {
                Planted = false;
                PendingUntil = 0f;
            }
        }

        private struct LocomotionFoot
        {
            private float _previous;
            private float _rear;
            private bool _hasSample;
            private bool _swung;

            public bool Sample(float position, float swingDistance, float settleDistance)
            {
                if (!_hasSample)
                {
                    _previous = _rear = position;
                    _hasSample = true;
                    return false;
                }

                float advance = position - _previous;
                _previous = position;
                if (!_swung)
                {
                    _rear = Mathf.Min(_rear, position);
                    _swung = position - _rear >= swingDistance;
                }
                return _swung && advance <= settleDistance;
            }

            public void Plant()
            {
                _rear = _previous;
                _swung = false;
            }
        }

        private struct PoseFoot
        {
            private Vector3 _previous, _anchor;
            private bool _hasSample, _lifted, _relocating;

            public bool Sample(Vector3 position, Vector3 up, float moveDistance, float liftDistance,
                float settleDistance, bool trackStride, bool strideReady)
            {
                if (!_hasSample)
                {
                    Plant(position);
                    return false;
                }

                bool settled = (position - _previous).sqrMagnitude <= settleDistance * settleDistance;
                _previous = position;
                Vector3 displacement = position - _anchor;
                float lift = Vector3.Dot(displacement, up);
                _lifted |= lift >= liftDistance;
                bool relocated = Vector3.ProjectOnPlane(displacement, up).sqrMagnitude >= moveDistance * moveDistance;
                _relocating |= !settled;
                if (trackStride)
                    return strideReady && (_lifted || relocated);
                return settled && (_lifted || (_relocating && relocated));
            }

            public void Plant(Vector3 position)
            {
                _anchor = _previous = position;
                _hasSample = true;
                _lifted = _relocating = false;
            }
        }

        private bool UsesPlayerPose => SandContact != null && _player != null && settings != null
            && settings.sandSoleSize.x > 0f && settings.sandSoleSize.y > 0f;

        private ActorStateId? CurrentStateId => _player?.PlayerController?.CurrentState?.StateId;

        /// <summary>대시도 지면을 달리는 이동이므로 같은 발 디딤 경로로 처리한다.</summary>
        private bool IsGroundLocomotion => CurrentStateId is ActorStateId.GroundMove or ActorStateId.Dash;

        /// <summary>모델 교체 시 액터와 양발을 새로 연결하고 입자를 미리 준비한다.</summary>
        public void Initialize(GameActor actor)
        {
            Unregister();
            _actor = actor;
            _player = actor as PlayerActor;
            // 액터의 Awake가 컨트롤러보다 먼저 실행돼도 모터를 놓치지 않는다.
            _motor = actor != null ? actor.GetComponent<KinematicCharacterMotor>() : null;
            Animator animator = GetComponent<Animator>();
            _leftFoot = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            _rightFoot = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
            _leftToes = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftToes) : null;
            _rightToes = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightToes) : null;
            ResetSandContact();
            _lastStepTime = _lastLandingTime = float.NegativeInfinity;
            if (settings == null || _leftFoot == null || _rightFoot == null) return;
            if (_effects == null && actor != null)
                _effects = new FootstepEffectPool(settings, actor.transform);
            _previousSounds.Clear();
            for (int i = 0; i < settings.surfaces.Length; i++)
            {
                FootstepSurfaceProfileSO surface = settings.surfaces[i];
                if (surface == null) continue;
                _previousSounds[surface.walk] = -1;
                _previousSounds[surface.run] = -1;
                _previousSounds[surface.landing] = -1;
            }
            if (isActiveAndEnabled && actor != null) Receivers[actor.gameObject] = this;
        }

        private void OnEnable()
        {
            _lastStepTime = _lastLandingTime = float.NegativeInfinity;
            ResetSandContact();
            if (_actor != null && settings != null) Receivers[_actor.gameObject] = this;
        }

        private void OnDisable()
        {
            Unregister();
            ResetSandContact();
            _effects?.Stop();
        }

        private void OnDestroy() => _effects?.Dispose();

        private void Unregister()
        {
            if (_actor != null && Receivers.TryGetValue(_actor.gameObject, out var receiver) && receiver == this)
                Receivers.Remove(_actor.gameObject);
        }

        /// <summary>이벤트 대상의 등록된 모델로 전달한다. 접촉이 거부돼도 기본음을 중복 재생하지 않는다.</summary>
        public static bool TryPlay(GameObject target, FootstepEvent.Foot foot, FootstepKind kind, float volume)
        {
            if (target == null || !Receivers.TryGetValue(target, out var receiver)) return false;
            if (kind != FootstepKind.Landing && receiver.UsesPlayerPose && receiver.IsGroundLocomotion) return true;
            receiver.Play(foot, kind, volume);
            return true;
        }

        public static void ReleaseHint(GameObject target, FootstepEvent.Foot foot)
        {
            if (SandContact == null || !Receivers.TryGetValue(target, out var receiver) || !receiver.CanProbeSand()) return;
            if (receiver.UsesPlayerPose) return;
            if (foot == FootstepEvent.Foot.Left) receiver._leftContact.Release();
            else receiver._rightContact.Release();
        }

        public static void HintStableLanding(GameObject target)
        {
            if (SandContact == null || !Receivers.TryGetValue(target, out var receiver)
                || receiver.settings == null || receiver.settings.sandSoleSize.x <= 0) return;
            receiver._landUntil = Time.time + receiver.settings.sandLandingContactWindow;
        }

        private void Play(FootstepEvent.Foot foot, FootstepKind kind, float volume, bool requireConfirmed = false)
        {
            if (!Application.isPlaying || settings == null || _motor == null
                || !_motor.GroundingStatus.IsStableOnGround || (_player != null && _player.IsInputSuppressed)
                || _actor is IDamageable { } damageable && !damageable.IsAlive()
                || _leftFoot == null || _rightFoot == null) return;
            float now = _actor.ActorTime;
            bool isLanding = kind == FootstepKind.Landing;
            bool confirmed = TrySandHint(foot, kind, out RaycastHit hit);
            if (requireConfirmed && !confirmed) return;
            if (now - _lastStepTime < settings.minimumStepInterval) return;
            if (now - _lastLandingTime < settings.landingStepSuppression) return;
            if (!isLanding && !IsMoving()) return;

            Vector3 footPosition = isLanding ? (_leftFoot.position + _rightFoot.position) * 0.5f
                : foot == FootstepEvent.Foot.Left ? _leftFoot.position : _rightFoot.position;
            FootstepSurfaceProfileSO surface = confirmed ? FootstepSurface.Resolve(hit.collider, hit.point) : null;
            if (surface == null)
            {
                Vector3 origin = new(footPosition.x, _actor.transform.position.y + settings.probeHeight, footPosition.z);
                if (!Physics.Raycast(origin, Vector3.down, out hit,
                        settings.probeHeight + settings.probeDepth, settings.groundLayers, QueryTriggerInteraction.Ignore))
                {
                    // WorldBuilder Terrain은 Default 레이어일 수 있다. 명시적으로 연결한 표면만 여기서 재생한다.
                    if (settings.sandSoleSize.x <= 0 || !ProbeSole(foot != FootstepEvent.Foot.Right, out hit, out _, out float gap)
                        || gap > settings.sandContactGap || gap < -0.09f) return;
                    surface = FootstepSurface.Resolve(hit.collider, hit.point);
                }
                else
                {
                    if (!isLanding && footPosition.y - hit.point.y > settings.maximumFootHeight) return;
                    surface = FootstepSurface.Resolve(hit.collider, hit.point) ?? settings.fallbackSurface;
                }
            }
            PlayResponse(hit, surface, kind, volume);
        }

        private void PlayResponse(RaycastHit hit, FootstepSurfaceProfileSO surface, FootstepKind kind, float volume)
        {
            Vector3 point = hit.point;
            Vector3 normal = hit.normal;
            if (FootstepWaterSurface.TryResolve(point, out Vector3 waterPoint, out var waterSurface))
            {
                point = waterPoint;
                normal = Vector3.up;
                surface = waterSurface;
                if (FootstepWaterSurface.TryResolveSurface(hit.point, out var water, out _) && water.dynamics != null)
                    WaterContactStamps.TryEmit(hit.point, water.dynamics.stepStrength);
            }
            if (surface == null) return;
            _lastStepTime = _actor.ActorTime;
            if (kind == FootstepKind.Landing) _lastLandingTime = _lastStepTime;
            LastSurface = surface;
            LastPosition = point;
            PlayedCount++;
            FootstepSurfaceProfileSO.Response response = surface.GetResponse(kind);
            PlaySound(response, point, volume);
            point += normal * settings.surfaceOffset;
            _effects?.Play(response.effect, point, normal, _actor.transform.forward, response.scale);
            _effects?.Play(response.secondaryEffect, point, normal, _actor.transform.forward, response.scale);
        }

        private void ResetSandContact()
        {
            _leftContact = _rightContact = new FootContact { StampFrame = -1 };
            ResetStride();
            _leftPose = _rightPose = default;
            _hasRoot = false;
            _landUntil = float.NegativeInfinity;
        }

        private void LateUpdate()
        {
            if (SandContact == null || settings == null || settings.sandSoleSize.x <= 0 || _motor == null) return;
            TrackSandContact();
            if (!CanProbeSand())
            {
                ResetStride();
                _leftPose = _rightPose = default;
                return;
            }
            ReleaseFoot(true, ref _leftContact);
            ReleaseFoot(false, ref _rightContact);
            if (Time.time <= _landUntil)
            {
                Play(FootstepEvent.Foot.Left, FootstepKind.Landing, 1, requireConfirmed: true);
                if (_leftContact.Planted && _rightContact.Planted) _landUntil = float.NegativeInfinity;
                ResetStride();
                _leftPose = _rightPose = default;
                return;
            }
            if (UsesPlayerPose)
            {
                UpdatePlayerFeet();
                return;
            }
            ResetStride();
            if (!IsMoving()) return;
            if (_leftContact.IsPending(Time.time))
                ConfirmSandContact(true, _leftContact.Kind, ref _leftContact, out _);
            if (_rightContact.IsPending(Time.time))
                ConfirmSandContact(false, _rightContact.Kind, ref _rightContact, out _);
        }

        private void ResetStride()
        {
            _leftStride = _rightStride = default;
            _strideDirection = default;
        }

        private void UpdatePlayerFeet()
        {
            if (_leftFoot == null || _rightFoot == null) return;
            Vector3 velocity = Vector3.ProjectOnPlane(_motor.BaseVelocity, _motor.CharacterUp);
            bool locomotion = IsGroundLocomotion;
            bool trackStride = locomotion && velocity.sqrMagnitude >= settings.minimumMoveSpeed * settings.minimumMoveSpeed;
            bool leftReady = false, rightReady = false;
            if (trackStride)
            {
                Transform root = _actor.transform;
                Vector3 direction = root.InverseTransformDirection(velocity.normalized);
                if (Vector3.Dot(direction, _strideDirection) < 0.5f) ResetStride();
                _strideDirection = direction;
                float settle = settings.locomotionFootSettleSpeed * Time.deltaTime;
                leftReady = _leftStride.Sample(Vector3.Dot(root.InverseTransformPoint(_leftFoot.position), direction),
                    settings.locomotionFootSwingDistance, settle);
                rightReady = _rightStride.Sample(Vector3.Dot(root.InverseTransformPoint(_rightFoot.position), direction),
                    settings.locomotionFootSwingDistance, settle);
            }
            else ResetStride();

            // 대시 중에는 MoveAnimType이 직전 이동 상태로 남아 있으므로 상태로 먼저 판정한다.
            bool running = CurrentStateId == ActorStateId.Dash || _actor.MoveAnimType != BaseMoveAnimType.Walk;
            var kind = locomotion && running ? FootstepKind.Run : FootstepKind.Walk;
            SamplePlayerFoot(true, _leftFoot.position, trackStride, leftReady, locomotion, kind, ref _leftStride, ref _leftPose);
            SamplePlayerFoot(false, _rightFoot.position, trackStride, rightReady, locomotion, kind, ref _rightStride, ref _rightPose);
        }

        private void SamplePlayerFoot(bool left, Vector3 position, bool trackStride, bool strideReady, bool locomotion,
            FootstepKind kind, ref LocomotionFoot stride, ref PoseFoot pose)
        {
            if (!pose.Sample(position, _motor.CharacterUp, settings.poseFootRelocationDistance,
                    settings.poseFootLiftDistance, settings.poseFootSettleSpeed * Time.deltaTime, trackStride, strideReady)
                || !ProbeSole(left, out var hit, out var direction, out float gap))
                return;

            gap = FootprintGap(gap, hit.normal, direction, _motor.CharacterUp, settings.sandSoleSize);
            if (gap > settings.sandContactGap || gap < -0.09f) return;
            stride.Plant();
            pose.Plant(position);
            if (!FootstepWaterSurface.TryResolve(hit.point, out _, out _))
                SandContact?.Invoke(hit, direction, settings.sandSoleSize, kind, left, true);

            if (!locomotion || _actor.ActorTime - _lastStepTime < settings.minimumStepInterval
                || _actor.ActorTime - _lastLandingTime < settings.landingStepSuppression) return;
            var surface = FootstepSurface.Resolve(hit.collider, hit.point);
            if (surface == null && (settings.groundLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
                surface = settings.fallbackSurface;
            PlayResponse(hit, surface, kind, 1f);
        }

        private void TrackSandContact()
        {
            Vector3 root = _actor.transform.position;
            bool teleported = _hasRoot && (root - _lastRoot).sqrMagnitude > 16f;
            if (!_hasRoot || teleported || !_motor.GroundingStatus.IsStableOnGround)
            {
                _leftContact.Release();
                _rightContact.Release();
                ResetStride();
                _leftPose = _rightPose = default;
            }
            if (teleported) _landUntil = float.NegativeInfinity;
            _lastRoot = root;
            _hasRoot = true;
        }

        private bool CanProbeSand() => Time.deltaTime > 0 && _motor != null && _motor.GroundingStatus.IsStableOnGround
            && (_player == null || !_player.IsInputSuppressed)
            && (_actor is not IDamageable damageable || damageable.IsAlive());

        private bool IsMoving() => Vector3.ProjectOnPlane(_motor.BaseVelocity, _motor.CharacterUp).sqrMagnitude
            >= settings.minimumMoveSpeed * settings.minimumMoveSpeed;

        private void ReleaseFoot(bool left, ref FootContact contact)
        {
            if (contact.Planted && (!ProbeSole(left, out var hit, out var direction, out float gap)
                || FootprintGap(gap, hit.normal, direction, _motor.CharacterUp, settings.sandSoleSize)
                    > settings.sandContactGap + 0.045f))
                contact.Release();
        }

        private bool TrySandHint(FootstepEvent.Foot foot, FootstepKind kind, out RaycastHit hit)
        {
            hit = default;
            if (SandContact == null || settings.sandSoleSize.x <= 0 || settings.sandSoleSize.y <= 0
                || Time.deltaTime <= 0) return false;
            TrackSandContact();
            if (kind == FootstepKind.Landing)
            {
                bool left = ConfirmSandContact(true, kind, ref _leftContact, out hit);
                bool right = ConfirmSandContact(false, kind, ref _rightContact, out var rightHit);
                if (!left && right) hit = rightHit;
                return left || right;
            }

            if (UsesPlayerPose || !IsMoving() || Time.time <= _landUntil) return false;
            bool isLeft = foot == FootstepEvent.Foot.Left;
            ref FootContact contact = ref (isLeft ? ref _leftContact : ref _rightContact);
            if (!contact.CanConfirm(Time.frameCount)) return false;
            // 이벤트 프레임에 발이 아직 떠 있으면 이후 포즈에서 같은 발의 접촉을 확인한다.
            contact.PendingUntil = Time.time + settings.sandStepContactWindow;
            contact.Kind = kind;
            return ConfirmSandContact(isLeft, kind, ref contact, out hit);
        }

        // FootstepEvent는 ActorAnimator가 그래프 평가 후 발행한다.
        private bool ConfirmSandContact(bool left, FootstepKind kind, ref FootContact contact, out RaycastHit hit)
        {
            hit = default;
            if (!contact.CanConfirm(Time.frameCount) || !ProbeSole(left, out hit, out Vector3 direction, out float gap))
                return false;
            gap = FootprintGap(gap, hit.normal, direction, _motor.CharacterUp, settings.sandSoleSize);
            if (!contact.TryConfirm(Time.frameCount, gap, settings.sandContactGap))
                return false;
            if (!FootstepWaterSurface.TryResolve(hit.point, out _, out _))
                SandContact?.Invoke(hit, direction, settings.sandSoleSize, kind, left, _player != null);
            return true;
        }

        // 경사면에서는 발바닥 중심보다 발끝이나 뒤꿈치가 먼저 지면에 닿는다.
        private static float FootprintGap(float gap, Vector3 normal, Vector3 direction, Vector3 up, Vector2 size)
        {
            float facing = Vector3.Dot(normal, up);
            if (facing <= 0f) return gap;
            Vector3 forward = Vector3.ProjectOnPlane(direction, up).normalized;
            Vector3 right = Vector3.Cross(up, forward);
            float reach = (size.y * Mathf.Abs(Vector3.Dot(normal, forward))
                + size.x * Mathf.Abs(Vector3.Dot(normal, right))) / (2f * facing);
            return Mathf.MoveTowards(gap, 0f, reach);
        }

        private bool ProbeSole(bool left, out RaycastHit hit, out Vector3 direction, out float gap)
        {
            var foot = left ? _leftFoot : _rightFoot;
            var toes = left ? _leftToes : _rightToes;
            hit = default;
            gap = float.PositiveInfinity;
            direction = _actor.transform.forward;
            if (foot == null) return false;
            if (toes != null && (toes.position - foot.position).sqrMagnitude > 0.0001f)
                direction = (toes.position - foot.position).normalized;
            Vector3 up = _motor.CharacterUp;
            Vector3 horizontal = Vector3.ProjectOnPlane(direction, up).normalized;
            Vector3 sole = foot.position + horizontal * settings.sandSoleForwardOffset - up * settings.sandAnkleToSole;
            float rise = 0.15f + settings.sandSoleSize.y * 0.5f;
            Vector3 origin = sole + up * rise;
            float distance = rise + 0.20f;
            if (!Physics.Raycast(origin, -up, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            var root = _actor.transform;
            if (hit.collider.transform.IsChildOf(root))
            {
                int count = Physics.RaycastNonAlloc(origin, -up, _soleHits, distance, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);
                if (count == _soleHits.Length) return false;
                hit = default;
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    var candidate = _soleHits[i];
                    if (candidate.distance >= nearest || candidate.collider.transform.IsChildOf(root)) continue;
                    nearest = candidate.distance;
                    hit = candidate;
                }
                if (hit.collider == null) return false;
            }
            gap = Vector3.Dot(sole - hit.point, up);
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (settings == null || settings.sandSoleSize.x <= 0) return;
            var animator = GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;
            DrawSole(animator, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
            DrawSole(animator, HumanBodyBones.RightFoot, HumanBodyBones.RightToes);
        }

        private void DrawSole(Animator animator, HumanBodyBones footBone, HumanBodyBones toeBone)
        {
            var foot = animator.GetBoneTransform(footBone);
            if (foot == null) return;
            var toes = animator.GetBoneTransform(toeBone);
            Vector3 up = animator.transform.up;
            Vector3 forward = Vector3.ProjectOnPlane(toes != null ? toes.position - foot.position : animator.transform.forward, up);
            if (forward.sqrMagnitude < 0.0001f) return;
            forward.Normalize();
            Vector3 center = foot.position + forward * settings.sandSoleForwardOffset - up * settings.sandAnkleToSole;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.LookRotation(forward, up), Vector3.one);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(settings.sandSoleSize.x, 0.015f, settings.sandSoleSize.y));
            Gizmos.matrix = previous;
        }
#endif

        private void PlaySound(FootstepSurfaceProfileSO.Response response, Vector3 position, float volume)
        {
            int count = response.sounds.Length;
            if (count == 0) return;
            int index = Random.Range(0, count);
            if (count > 1 && _previousSounds.TryGetValue(response, out int previous) && previous >= 0)
            {
                index = Random.Range(0, count - 1);
                if (index >= previous) index++;
            }
            _previousSounds[response] = index;
            SoundEntrySO sound = response.sounds[index];
            if (sound != null) Svc.Sound?.PlaySfx(sound.key, position, volume * response.volume);
        }
    }
}
