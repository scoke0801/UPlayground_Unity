
using System;
using System.Collections.Generic;
using Animancer;
using Interaction.Enum;
using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data.Actor.Animation
{
    [Serializable]
    public struct AnimationData
    {
        public AnimKey key;
        public AnimLayer animLayer;
        
        public ClipTransition Transition;
        
        // [TODO] AnimEvent: 우선은 복잡하게 구현은 하지 않고 추후 적용이 필요할지만 고민해보자
        // [TODO] AnimCurve: 우선은 복잡하게 구현은 하지 않고 추후 적용이 필요할지만 고민해보자
    }
    
    [CreateAssetMenu(fileName = "ActorAnimationSet", menuName = "UPlayGround/ActorData/ActorAnimationSet")]
    public class ActorAnimationSet : ScriptableObject
    {
        [SerializeField] private List<AnimationData> _animationList;
        
        private Dictionary<AnimKey, AnimationData> _animationDict;
        private void OnEnable()
        {
            InitializeDictionary();
        }
        public void InitializeDictionary()
        {
            _animationDict = new Dictionary<AnimKey, AnimationData>();
            foreach (var data in _animationList)
            {
                if (data.key != null)
                {
                    if (_animationDict.ContainsKey(data.key))
                    {
                        Debug.LogWarning($"Duplicate AnimKey: {data.key}");
                    }
                    else
                    {
                        _animationDict.Add(data.key, data);
                    }
                }
            }
        }

        public ClipTransition GetClipTransition(AnimKey key)
        {
            return _animationDict.TryGetValue(key, out AnimationData data) ? data.Transition : null;
        }
        
        public AnimationClip GetAnimationClip(AnimKey key)
        {
            return _animationDict.TryGetValue(key, out AnimationData data) ? data.Transition.Clip : null;
        }
    }
}

