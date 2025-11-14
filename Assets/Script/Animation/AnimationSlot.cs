using UnityEngine;
using Animancer;

namespace Animation
{
    /// <summary>
    /// 레이어 블렌딩 모드
    /// </summary>
    public enum LayerBlendMode
    {
        Override = 0,  // 이전 레이어를 덮어씀
        Additive = 1   // 이전 레이어에 더함
    }
    
    /// <summary>
    /// 애니메이션 슬롯 설정
    /// Avatar Mask와 Layer를 사용하여 실제로 특정 본만 제어
    /// </summary>
    [CreateAssetMenu(fileName = "New Animation Slot", menuName = "Animation/Slot")]
    public class AnimationSlot : ScriptableObject
    {
        [Header("슬롯 정보")]
        [SerializeField] private string slotName = "DefaultSlot";
        [SerializeField] private string groupName = "DefaultGroup";
        
        [Header("레이어 설정")]
        [SerializeField] private int layerIndex = 0;
        [Tooltip("레이어 가중치 (0~1)")]
        [SerializeField] private float layerWeight = 1f;
        
        [Header("본 마스크")]
        [Tooltip("이 슬롯이 제어할 본을 정의하는 Avatar Mask")]
        [SerializeField] private AvatarMask avatarMask;
        
        [Header("블렌딩 설정")]
        [SerializeField] private LayerBlendMode blendingMode = LayerBlendMode.Override;
        
        public string SlotName => slotName;
        public string GroupName => groupName;
        public int LayerIndex => layerIndex;
        public float LayerWeight => layerWeight;
        public AvatarMask AvatarMask => avatarMask;
        public LayerBlendMode BlendingMode => blendingMode;
        public bool IsAdditive => blendingMode == LayerBlendMode.Additive;
    }
}
