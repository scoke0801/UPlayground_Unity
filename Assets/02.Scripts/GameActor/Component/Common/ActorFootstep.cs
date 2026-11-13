using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Event;
using UPlayGround.Data.Sound;
using UPlayGround.Manager;

namespace UPlayGround.Components
{
    /// <summary>평가가 끝난 발 포즈와 실제 바닥을 연결해 모델별 환경 효과를 재생한다.</summary>
    public sealed class ActorFootstep : ActorComponent
    {
        public FootstepSettingsSO settings;
        private static readonly Dictionary<GameObject, ActorFootstep> Receivers = new();
        private readonly Dictionary<FootstepSurfaceProfileSO.Response, int> _previousSounds = new();
        private PlayerActor _actor;
        private KinematicCharacterMotor _motor;
        private Transform _leftFoot;
        private Transform _rightFoot;
        private FootstepEffectPool _effects;
        private float _lastStepTime = float.NegativeInfinity;
        private float _lastLandingTime = float.NegativeInfinity;

        public int PlayedCount { get; private set; }
        public FootstepSurfaceProfileSO LastSurface { get; private set; }
        public Vector3 LastPosition { get; private set; }

        /// <summary>모델 교체 시 액터와 양발을 새로 연결하고 입자를 미리 준비한다.</summary>
        public void Initialize(PlayerActor actor)
        {
            Unregister();
            _actor = actor;
            // 액터의 Awake가 컨트롤러보다 먼저 실행돼도 모터를 놓치지 않는다.
            _motor = actor != null ? actor.GetComponent<KinematicCharacterMotor>() : null;
            Animator animator = GetComponent<Animator>();
            _leftFoot = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            _rightFoot = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
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
            if (_actor != null && settings != null) Receivers[_actor.gameObject] = this;
        }

        private void OnDisable()
        {
            Unregister();
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
            receiver.Play(foot, kind, volume);
            return true;
        }

        private void Play(FootstepEvent.Foot foot, FootstepKind kind, float volume)
        {
            if (!Application.isPlaying || settings == null || _motor == null
                || !_motor.GroundingStatus.IsStableOnGround || _actor.IsInputSuppressed
                || !_actor.IsAlive() || _leftFoot == null || _rightFoot == null) return;
            float now = _actor.ActorTime;
            bool isLanding = kind == FootstepKind.Landing;
            if (now - _lastStepTime < settings.minimumStepInterval) return;
            if (!isLanding && now - _lastLandingTime < settings.landingStepSuppression) return;
            if (!isLanding && Vector3.ProjectOnPlane(_motor.BaseVelocity, _motor.CharacterUp).sqrMagnitude
                < settings.minimumMoveSpeed * settings.minimumMoveSpeed) return;

            Vector3 footPosition = isLanding ? (_leftFoot.position + _rightFoot.position) * 0.5f
                : foot == FootstepEvent.Foot.Left ? _leftFoot.position : _rightFoot.position;
            Vector3 origin = new(footPosition.x, _actor.transform.position.y + settings.probeHeight, footPosition.z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                    settings.probeHeight + settings.probeDepth, settings.groundLayers, QueryTriggerInteraction.Ignore)) return;
            if (!isLanding && footPosition.y - hit.point.y > settings.maximumFootHeight) return;
            FootstepSurfaceProfileSO surface = FootstepSurface.Resolve(hit.collider, hit.point) ?? settings.fallbackSurface;
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
            _lastStepTime = now;
            if (isLanding) _lastLandingTime = now;
            LastSurface = surface;
            LastPosition = point;
            PlayedCount++;
            FootstepSurfaceProfileSO.Response response = surface.GetResponse(kind);
            PlaySound(response, point, volume);
            point += normal * settings.surfaceOffset;
            _effects?.Play(response.effect, point, normal, _actor.transform.forward, response.scale);
            _effects?.Play(response.secondaryEffect, point, normal, _actor.transform.forward, response.scale);
        }

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
