using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Components
{
    /// <summary>
    /// 비전투 NPC의 전투 반응 상태 보관소. 판단은 BT(<c>IsNpcThreatWithin</c>/<c>HoldNpcReaction</c>)가 하고,
    /// 이 컴포넌트는 결정된 단계와 위협 위치를 들고 있다가 <see cref="NpcReactState"/>에 넘긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcCombatReaction : MonoBehaviour
    {
        [SerializeField] private NpcCombatReactionProfileSO _profile;

        private NpcActor _actor;
        private NpcMovementController _controller;
        private int _senseFrame = -1;
        private bool _hasThreat;
        private Vector3 _scanThreatPosition;
        private float _threatDistance;
        private float _lastThreatTime;
        private float _onsetAt;
        private bool _isOnsetPending;

        public NpcCombatReactionProfileSO Profile => _profile;
        public NpcReactionLevel Level { get; private set; }
        public Vector3 ThreatPosition { get; private set; }
        public bool IsReacting => Level != NpcReactionLevel.None;

        /// <summary>반응을 시작하기 직전의 위치. 해제 후 되돌아갈 자리다.</summary>
        public Vector3 OriginPosition { get; private set; }
        public bool HasOrigin { get; private set; }

        /// <summary>복귀를 마쳤거나 포기했을 때 기록한 원위치를 비운다.</summary>
        public void ClearOrigin() => HasOrigin = false;

        private void Awake()
        {
            _actor = GetComponent<NpcActor>();
            _controller = GetComponent<NpcMovementController>();
            // 전투 가능 NPC(동료 등)는 도망치지 않는다.
            if (_actor == null || _controller == null || _profile == null || _actor.HasActorType(ActorType.Combat))
                enabled = false;
        }

        private void OnDisable()
        {
            Clear();
            ClearOrigin();
        }

        /// <summary>가장 가까운 위협이 해당 단계 반경 안인지. 같은 프레임의 중복 호출은 스캔 결과를 재사용한다.</summary>
        public bool IsThreatWithin(NpcReactionLevel level)
        {
            Sense();
            return _hasThreat && NpcReactionResolver.IsWithinLevel(_profile, level, Level, _threatDistance);
        }

        /// <summary>
        /// 단계를 유지한다. 처음 반응할 때는 주민별 지연이 지난 뒤 반응 상태로 진입한다.
        /// 지금 반응할 수 없는 상황(대화 중 등)이면 false.
        /// </summary>
        public bool Hold(NpcReactionLevel level)
        {
            if (!CanReact()) return false;

            Sense();
            _lastThreatTime = Time.time;
            if (_hasThreat) ThreatPosition = _scanThreatPosition;

            if (Level == NpcReactionLevel.None && !WaitOnsetDelay()) return true;

            if (!IsReacting && !HasOrigin)
            {
                OriginPosition = transform.position;
                HasOrigin = true;
            }

            Level = level;
            if (_controller.CurrentState is not NpcReactState)
                _controller.TransitionToState(new NpcReactState(_controller));
            return true;
        }

        /// <summary>유지 시간이 지났으면 반응을 해제한다. 아직이면 false(계속 유지).</summary>
        public bool TryRelease()
        {
            if (!IsReacting && !_isOnsetPending) return true;
            if (Time.time - _lastThreatTime < _profile.releaseHoldSeconds) return false;
            Clear();
            return true;
        }

        public void Clear()
        {
            Level = NpcReactionLevel.None;
            _isOnsetPending = false;
        }

        private void Sense()
        {
            if (_senseFrame == Time.frameCount) return;
            _senseFrame = Time.frameCount;
            _hasThreat = NpcThreatScanner.TryGetNearest(transform.position, out _scanThreatPosition, out _threatDistance);
        }

        private bool WaitOnsetDelay()
        {
            if (!_isOnsetPending)
            {
                _isOnsetPending = true;
                Vector2 range = _profile.onsetDelayRange;
                _onsetAt = Time.time + Random.Range(range.x, range.y);
            }

            if (Time.time < _onsetAt) return false;
            _isOnsetPending = false;
            return true;
        }

        private bool CanReact()
        {
            if (_actor == null || !_actor.isActiveAndEnabled) return false;
            if (_actor.IsInteracting() || _actor.IsDialogueStaged) return false;
            return !Services.TryGet<IDialogueService>(out var dialogue) || !dialogue.IsDialogueActive;
        }
    }
}
