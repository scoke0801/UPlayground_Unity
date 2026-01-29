using System;
using Animancer;
using UnityEngine;

namespace UPlayGround.GameActor
{
    public class PlayerPreviewActor : MonoBehaviour
    {
        private AnimancerComponent _animator;
        [SerializeField] ClipTransition _idleTransition;
        
        private void Awake()
        {
            _animator = GetComponent<AnimancerComponent>();

            _animator.Play(_idleTransition);
        }
    }
}