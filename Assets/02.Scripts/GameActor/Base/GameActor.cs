using System;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround
{
    public abstract class GameActor : MonoBehaviour
    {
        [SerializeField] protected ActorType _actorType = ActorType.None;
        [SerializeField] protected CharacterActorType _characterActorType = CharacterActorType.None;
        
        protected ActorMovementController MovementController;
        protected ActorAnimator _animator;
        
        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

        public ActorType ActorType => _actorType;
        public CharacterActorType CharacterType => _characterActorType;
        protected virtual void Awake()
        {
            MovementController = GetComponent<ActorMovementController>();
            _animator = GetComponent<ActorAnimator>();
            if (_animator != null)
            {
                _animator.Init(this);
            }
        }
        
        protected virtual void Start()
        {
            
        }
    }
}