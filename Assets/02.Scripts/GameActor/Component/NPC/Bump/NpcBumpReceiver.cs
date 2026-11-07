using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Components
{
    /// <summary>
    /// 플레이어에게 부딪힌 NPC의 반응을 받아 상체 포즈와 밀림 상태로 연결한다.
    /// 피해가 아니므로 전투 반응(위협)에는 전달하지 않는다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NpcActor))]
    public sealed class NpcBumpReceiver : MonoBehaviour
    {
        private static readonly Dictionary<Collider, NpcBumpReceiver> Registry = new();

        [SerializeField] private NpcBumpProfileSO _profile;

        private NpcActor _actor;
        private NpcMovementController _controller;
        private NpcBrain _brain;
        private NpcCombatReaction _combatReaction;
        private Collider[] _colliders;
        private NpcBumpPose _pose;
        private bool _isPoseBound;
        private float _nextReactTime;

        public NpcBumpProfileSO Profile => _profile;

        /// <summary>씬에서 직접 지정한 프로필이 없을 때만 데이터의 기본 프로필을 쓴다.</summary>
        public void SetProfileIfMissing(NpcBumpProfileSO profile)
        {
            if (_profile == null) _profile = profile;
        }

        /// <summary>마지막으로 받아들인 충돌의 밀림 방향. 밀림 상태가 시작할 때 읽는다.</summary>
        public Vector3 LastPushDirection { get; private set; }

        /// <summary>마지막으로 받아들인 충돌의 단계.</summary>
        public NpcBumpTier LastTier { get; private set; }

        /// <summary>플레이어 이동 콜백이 부딪힌 콜라이더의 NPC 반응기를 찾는다. NPC가 아니면 false.</summary>
        public static bool TryGetFor(Collider collider, out NpcBumpReceiver receiver)
            => Registry.TryGetValue(collider, out receiver);

        private void Awake()
        {
            _actor = GetComponent<NpcActor>();
            _controller = GetComponent<NpcMovementController>();
            _brain = GetComponent<NpcBrain>();
            _combatReaction = GetComponent<NpcCombatReaction>();
        }

        private void OnEnable()
        {
            _colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < _colliders.Length; i++)
                Registry[_colliders[i]] = this;
        }

        private void OnDisable()
        {
            if (_colliders == null) return;
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) Registry.Remove(_colliders[i]);
        }

        /// <summary>
        /// 플레이어 충돌을 받아 반응을 시작한다. 쿨다운·NPC 상황·속도 때문에 거부되면 false.
        /// </summary>
        public bool TryReceive(Vector3 playerVelocity, Vector3 contactPoint, Vector3 contactNormal)
        {
            if (_profile == null || Time.time < _nextReactTime) return false;

            float speed = Vector3.ProjectOnPlane(playerVelocity, Vector3.up).magnitude;
            NpcBumpTier tier = NpcBumpResolver.ResolveTier(_profile, speed, ResolveContext());
            if (tier == NpcBumpTier.None) return false;

            Vector3 direction = NpcBumpResolver.ResolvePushDirection(playerVelocity, contactNormal);
            if (direction == Vector3.zero) return false;

            NpcBumpPart part = ResolvePartAt(contactPoint, out float lateralSign);
            var (lean, twist) = NpcBumpResolver.ResolveUpperBodyAngles(_profile, tier, part);
            LastPushDirection = direction;
            LastTier = tier;
            _nextReactTime = Time.time + _profile.cooldownSeconds;

            PlayPose(direction, lean, twist * lateralSign);
            if (tier >= NpcBumpTier.Stagger)
                _controller.TransitionToState(new NpcBumpState(_controller, this));
            return true;
        }

        private NpcBumpPart ResolvePartAt(Vector3 contactPoint, out float lateralSign)
        {
            Vector3 offset = contactPoint - transform.position;
            var local = new Vector3(Vector3.Dot(offset, transform.right), offset.y, Vector3.Dot(offset, transform.forward));
            lateralSign = _profile.shoulderOffset > 0f ? Mathf.Clamp(local.x / _profile.shoulderOffset, -1f, 1f) : 0f;
            return NpcBumpResolver.ResolvePart(_profile, local, GetActorHeight());
        }

        private float GetActorHeight() => _colliders.Length > 0 ? _colliders[0].bounds.size.y : 0f;

        private NpcBumpContext ResolveContext()
        {
            ActorStateId? stateId = _controller.CurrentState?.StateId;
            if (stateId is ActorStateId.Talk or ActorStateId.NpcReact or ActorStateId.NpcReturn or ActorStateId.NpcBump)
                return NpcBumpContext.Locked;
            if (_actor.IsInteracting() || _actor.IsDialogueStaged
                || (_combatReaction != null && _combatReaction.IsReacting))
                return NpcBumpContext.Locked;

            bool isRoutineWorking = _controller.Routine != null && _controller.Routine.IsWorking;
            bool isFixedWorker = _brain != null && !_brain.EnableWander && _brain.WorkMotion != null;
            return isRoutineWorking || isFixedWorker ? NpcBumpContext.Working : NpcBumpContext.Free;
        }

        private void PlayPose(Vector3 direction, float lean, float twist)
        {
            if (!_isPoseBound)
            {
                _isPoseBound = true;
                var animator = GetComponentInChildren<Animator>(true);
                _pose = animator != null ? animator.GetComponent<NpcBumpPose>() ?? animator.gameObject.AddComponent<NpcBumpPose>() : null;
                if (_pose != null && !_pose.TryBind(animator)) _pose = null;
            }
            _pose?.Play(_profile, direction, lean, twist);
        }
    }
}
