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
        private bool _isFallbackProfile;

        public NpcBumpProfileSO Profile => _profile;

        /// <summary>
        /// 씬이나 프리팹에서 직접 지정한 프로필은 그대로 두고, 데이터가 준 프로필을 쓴다.
        /// 데이터가 없는 NPC에 임시로 준 기본 프로필은 이후 데이터의 프로필로 대체된다.
        /// </summary>
        public void SetProfile(NpcBumpProfileSO profile, bool isFallbackProfile)
        {
            if (_profile != null && !_isFallbackProfile) return;
            _profile = profile;
            _isFallbackProfile = isFallbackProfile;
        }

        /// <summary>기본 프로필로 동작 중이면 제거한다. 데이터가 이 NPC는 반응하지 않도록 정했을 때 쓴다.</summary>
        public void ClearFallbackProfile()
        {
            if (!_isFallbackProfile) return;
            _profile = null;
            _isFallbackProfile = false;
        }

        /// <summary>마지막으로 받아들인 충돌의 밀림 방향. 밀림 상태가 시작할 때 읽는다.</summary>
        public Vector3 LastPushDirection { get; private set; }

        /// <summary>마지막으로 받아들인 충돌의 단계.</summary>
        public NpcBumpTier LastTier { get; private set; }
        /// <summary>표시 소비자가 새 충돌을 구별한다. 오래된 반응을 뒤늦게 재생하지 않는다.</summary>
        public uint BumpSequence { get; private set; }
        public float LastBumpTime { get; private set; } = float.NegativeInfinity;

        /// <summary>마지막 충돌에 맞는 전용 모션 슬롯. 모션 없이 처리하는 단계는 default.</summary>
        public UPlayGround.Gameplay.Tag.GameplayTag LastMotionTag { get; private set; }

        /// <summary>이 NPC의 모션셋에 해당 슬롯의 전용 모션이 있는지. 슬롯이 비어 있으면 절차 연출만 쓴다.</summary>
        public bool HasMotion(UPlayGround.Gameplay.Tag.GameplayTag tag)
            => tag.IsValid() && _actor.Animator != null && _actor.Animator.HasMotion(tag);

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
            LastBumpTime = Time.time;
            BumpSequence++;
            LastMotionTag = NpcBumpResolver.ResolveMotionTag(tier, NpcBumpResolver.ResolveMotionDirection(part));
            _nextReactTime = Time.time + _profile.cooldownSeconds;

            // 전용 모션이 몸을 이미 움직이면 절차 연출은 보조로만 쓴다.
            float poseScale = HasMotion(LastMotionTag) ? _profile.motionPoseScale : 1f;
            PlayPose(tier, direction, lean * poseScale, twist * lateralSign * poseScale, poseScale);
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
            // 스토리·연출 중이거나 누군가 일과를 멈춰 세운 NPC는 건드리지 않는다.
            if (NpcBumpGate.IsStoryPresentationActive() || _actor.IsRoutinePaused)
                return NpcBumpContext.Locked;

            // 평상시 상태(대기·배회·일과)가 아니면 다른 시스템이 NPC를 쥐고 있으므로 끼어들지 않는다.
            ActorStateId? stateId = _controller.CurrentState?.StateId;
            if (stateId is not (ActorStateId.Idle or ActorStateId.Wander))
                return NpcBumpContext.Locked;
            if (_actor.IsInteracting() || _actor.IsDialogueStaged
                || (_combatReaction != null && _combatReaction.IsReacting))
                return NpcBumpContext.Locked;

            bool isRoutineWorking = _controller.Routine != null && _controller.Routine.IsWorking;
            bool isFixedWorker = _brain != null && !_brain.EnableWander && _brain.WorkMotion != null;
            return isRoutineWorking || isFixedWorker ? NpcBumpContext.Working : NpcBumpContext.Free;
        }

        private void PlayPose(NpcBumpTier tier, Vector3 direction, float lean, float twist, float poseScale)
        {
            if (!_isPoseBound)
            {
                _isPoseBound = true;
                var animator = GetComponentInChildren<Animator>(true);
                _pose = animator != null ? animator.GetComponent<NpcBumpPose>() ?? animator.gameObject.AddComponent<NpcBumpPose>() : null;
                if (_pose != null && !_pose.TryBind(animator)) _pose = null;
            }
            _pose?.Play(_profile, tier, direction, lean, twist, poseScale);
        }
    }
}
