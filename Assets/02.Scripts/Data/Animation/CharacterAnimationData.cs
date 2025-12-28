using UnityEngine;
using System.Collections.Generic;
using Animancer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "AnimData", menuName = "UP/ActorData/AnimData")]
    public class CharacterAnimationData : ScriptableObject
    {
        [Header("에디터 설정")]
        public DefaultAsset animationFolder; // 검색할 폴더 지정
        
        [System.Serializable]
        public class ClipEntry
        {
            public AnimKey key;
            public ClipTransition transition;
        }

        [System.Serializable]
        public class MixerEntry
        {
            public AnimKey key;
            public LinearMixerTransition mixer;
        }

        [Header("단일 애니메이션 클립 목록")]
        [SerializeField] private List<ClipEntry> clipAnimations = new List<ClipEntry>();

        [Header("믹서 애니메이션 클립 목록")]
        [SerializeField] private List<MixerEntry> mixerAnimations = new List<MixerEntry>();

        // 2. ITransition 인터페이스를 사용하여 통합 딕셔너리 구성
        private Dictionary<AnimKey, ITransition> animationDictionary;

        /// <summary>
        /// Dictionary 초기화 (캐릭터 생성 시 1회 호출)
        /// </summary>
        public void Initialize()
        {
            if (animationDictionary != null) return; 

            animationDictionary = new Dictionary<AnimKey, ITransition>();

            // 클립 데이터 삽입
            foreach (var entry in clipAnimations)
            {
                if (entry.key == AnimKey.None) continue;
                AddAnimation(entry.key, entry.transition);
            }

            // 믹서 데이터 삽입
            foreach (var entry in mixerAnimations)
            {
                if (entry.key == AnimKey.None) continue;
                AddAnimation(entry.key, entry.mixer);
            }
        }

        private void AddAnimation(AnimKey key, ITransition transition)
        {
            if (animationDictionary.ContainsKey(key))
            {
                Debug.LogWarning($"[CharacterAnimationData] 중복된 키 발견: {key} (데이터: {name})");
                return;
            }
            animationDictionary[key] = transition;
        }

        /// <summary>
        /// 클립/믹서 구분 없이 애니메이션 데이터를 가져옵니다.
        /// </summary>
        public ITransition GetAnimation(AnimKey key)
        {
            if (animationDictionary == null) Initialize();

            if (animationDictionary.TryGetValue(key, out ITransition anim))
            {
                return anim;
            }

            Debug.LogWarning($"[CharacterAnimationData] '{key}' 애니메이션을 찾을 수 없습니다: {name}");
            return null;
        }
        
    }
}