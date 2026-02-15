using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.Enum;

namespace UPlayGround.Data
{
    public enum AttackType
    {
        Melee,      // 근접 공격
        Projectile,  // 발사체 공격
    }

    // 에디터 타임에 결정되는 공격 정보
    [System.Serializable]
    public class ComboData
    {
        [Tooltip("공격 애니메이션 키")]
        public AnimKey animKey;
        
        [Tooltip("공격 데미지")]
        public float damage;
        
        [Tooltip("피격 반응")]
        public AttackReactionType reactionType = AttackReactionType.Hit;

        [Tooltip("공격 중 끊을 수 있는지 여부")]
        public bool canBeInterrupted;
        
        [Header("Hit Detection")]
        [Tooltip("히트 판정 범위 (반지름)")]
        public float hitRadius = 2.0f;
        
        [Tooltip("히트 판정 각도 (전방 기준, 양쪽 각도)")]
        public float hitAngle = 60f;

        [Tooltip("히트 판정 오프셋")] 
        public Vector3 attackOffset = Vector3.zero;
        
        [Header("Projectile Settings (발사체 공격일 때만)")]
        [Tooltip("생성할 발사체 프리팹")]
        public BaseProjectile projectilePrefab;
    
        [Tooltip("발사체 생성 딜레이 (초)")]
        public float projectileSpawnDelay = 0.3f;
    }
    
    // 런타임에 결정되는 공격 정보
    public class AttackData
    {
        public AnimKey animKey;
        public float damage;
        public float duration;
        public bool canBeInterrupted;

        public AttackReactionType reactionType = AttackReactionType.Hit;
        
        // Hit Detection Data
        public float hitRange;
        public float hitAngle;
        public float hitHeightOffset;
        
        public Vector3 hitPoint;        // 공격 적중 위치
        public GameObject hitTarget;     // 피격 대상
        public float criticalMultiplier; // 크리티컬 배율
        public bool isCounterAttack;     // 카운터 공격 여부
        public Vector3 attackDirection;
    }

}