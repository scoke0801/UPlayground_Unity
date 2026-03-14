using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround
{
    public abstract class GameActor : MonoBehaviour
    {
        [SerializeField] protected ActorType _actorType = ActorType.None;
        [SerializeField] protected CharacterActorType _characterActorType = CharacterActorType.None;

        [SerializeField] protected SerializedDictionary<ActorSocketType, Transform> _socketDict;

        protected ActorMovementController MovementController;
        protected ActorAnimator _animator;
        
        protected ActorColorChanger _colorChanger;
        protected DissolveController _dissolveController;

        private float _localTimeScale = 1.0f;

        /// <summary>
        /// 액터 개별 타임 스케일 (기본 1.0)
        /// </summary>
        public float LocalTimeScale
        {
            get => _localTimeScale;
            set
            {
                _localTimeScale = value;
                if (_animator != null)
                {
                    _animator.Speed = _localTimeScale;
                }
            }
        }

        /// <summary>
        /// 로컬 타임 스케일이 적용된 DeltaTime
        /// </summary>
        public float DeltaTime => Time.deltaTime * _localTimeScale;

        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

        public ActorType ActorType => _actorType;
        public CharacterActorType CharacterType => _characterActorType;

        /// <summary>
        /// 플래그 조합 체크. 예: actor.HasActorType(ActorType.Talkable)
        /// </summary>
        public bool HasActorType(ActorType flag) => (_actorType & flag) != 0;

        public ActorMovementController ActorController => MovementController;
        
        protected virtual void Awake()
        {
            MovementController = GetComponent<ActorMovementController>();
            _animator = GetComponent<ActorAnimator>();
            if (_animator != null)
            {
                _animator.Init(this);
            }

            _colorChanger = gameObject.GetOrAddComponent<ActorColorChanger>();
            _dissolveController = gameObject.GetOrAddComponent<DissolveController>();
            
            // 매니저에 등록
            GameObjectManager.Instance?.RegisterActor(this);
        }

        protected virtual void OnDestroy()
        {
            // 매니저에서 제거
            GameObjectManager.Instance?.UnregisterActor(this);
        }
        
        protected virtual void Start()
        {
            
        }

        public bool HasSocket(ActorSocketType socketType)
        {
            return _socketDict.ContainsKey(socketType);
        }
        
        public Transform GetSocket(ActorSocketType socketType)
        {
            if (_socketDict.TryGetValue(socketType, out var result))
            {
                return result;
            }

            return null;
        }

        public bool TryGetSocket(ActorSocketType socketType, out Transform socket)
        {
            if (_socketDict.TryGetValue(socketType, out socket))
            {
                return true;
            }
            
            return false;
        }
    }
}