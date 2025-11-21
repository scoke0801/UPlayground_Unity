using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터별 애니메이션 클립을 관리하는 ScriptableObject
/// Key(String) - Value(AnimationClip) 구조로 관리
/// </summary>
[CreateAssetMenu(fileName = "New Character Animation Data", menuName = "Animation/Character Animation Data")]
public class CharacterAnimationData : ScriptableObject
{
    [System.Serializable]
    public class AnimationEntry
    {
        public string key;
        public AnimationClip clip;
    }
    
    [Header("애니메이션 클립 목록")]
    [SerializeField] private List<AnimationEntry> animations = new List<AnimationEntry>();
    
    // 런타임에서 빠른 검색을 위한 Dictionary (초기화 시 생성)
    private Dictionary<string, AnimationClip> animationDictionary;
    
    /// <summary>
    /// Dictionary 초기화
    /// </summary>
    public void Initialize()
    {
        animationDictionary = new Dictionary<string, AnimationClip>();
        
        foreach (var entry in animations)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning($"[CharacterAnimationData] 빈 Key가 있습니다: {name}");
                continue;
            }
            
            if (entry.clip == null)
            {
                Debug.LogWarning($"[CharacterAnimationData] '{entry.key}' 클립이 null입니다: {name}");
                continue;
            }
            
            if (animationDictionary.ContainsKey(entry.key))
            {
                Debug.LogWarning($"[CharacterAnimationData] 중복된 Key '{entry.key}': {name}");
                continue;
            }
            
            animationDictionary.Add(entry.key, entry.clip);
        }
        
        Debug.Log($"[CharacterAnimationData] '{name}' 초기화 완료 - {animationDictionary.Count}개 애니메이션");
    }
    
    /// <summary>
    /// Key로 애니메이션 클립 가져오기
    /// </summary>
    public AnimationClip GetClip(string key)
    {
        if (animationDictionary == null)
        {
            Initialize();
        }
        
        if (animationDictionary.TryGetValue(key, out AnimationClip clip))
        {
            return clip;
        }
        
        Debug.LogWarning($"[CharacterAnimationData] '{key}' 애니메이션을 찾을 수 없습니다: {name}");
        return null;
    }
    
    /// <summary>
    /// 특정 Key의 애니메이션이 존재하는지 확인
    /// </summary>
    public bool HasClip(string key)
    {
        if (animationDictionary == null)
        {
            Initialize();
        }
        
        return animationDictionary.ContainsKey(key);
    }
    
    /// <summary>
    /// 모든 애니메이션 Key 목록 가져오기
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (animationDictionary == null)
        {
            Initialize();
        }
        
        return new List<string>(animationDictionary.Keys);
    }
    
    /// <summary>
    /// 에디터용 - 애니메이션 추가
    /// </summary>
    public void AddAnimation(string key, AnimationClip clip)
    {
        #if UNITY_EDITOR
        animations.Add(new AnimationEntry { key = key, clip = clip });
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// 에디터용 - 애니메이션 제거
    /// </summary>
    public void RemoveAnimation(string key)
    {
        #if UNITY_EDITOR
        animations.RemoveAll(entry => entry.key == key);
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}
