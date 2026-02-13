using System;
using Animancer;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.MovementController;

namespace UPlayGround
{
    public abstract class GameActor : MonoBehaviour
    {
        protected ActorMovementController MovementController;
        protected ActorAnimator _animator;
        
        public virtual ActorAnimator Animator => _animator;
        public BaseMoveAnimType MoveAnimType { get; set; } = BaseMoveAnimType.Run;

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