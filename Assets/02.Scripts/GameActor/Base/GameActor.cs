using AYellowpaper.SerializedCollections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Animation;
using UPlayGround.Component;
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

        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

        public ActorType ActorType => _actorType;
        public CharacterActorType CharacterType => _characterActorType;

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
    }
}