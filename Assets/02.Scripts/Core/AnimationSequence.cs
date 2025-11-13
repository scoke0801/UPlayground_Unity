using UnityEngine;

namespace Core.Animation
{
    /// <summary>
    /// 언리얼 엔진의 Animation Sequence와 유사한 래퍼 클래스
    /// 단일 애니메이션 클립과 메타데이터를 포함
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimSequence", menuName = "Animation/Animation Sequence")]
    public class AnimationSequence : ScriptableObject
    {
        [Header("Animation Data")]
        [SerializeField] private AnimationClip clip;
        
        [Header("Settings")]
        [SerializeField] private bool loop = true;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool enableRootMotion = false;
        
        [Header("Metadata")]
        [SerializeField, TextArea(3, 5)] private string description;

        public AnimationClip Clip => clip;
        public bool Loop => loop;
        public float PlaybackSpeed => playbackSpeed;
        public bool EnableRootMotion => enableRootMotion;
        public float Duration => clip != null ? clip.length : 0f;

        /// <summary>
        /// AnimationClip으로 암시적 변환
        /// </summary>
        public static implicit operator AnimationClip(AnimationSequence sequence)
        {
            return sequence?.clip;
        }
    }
}
