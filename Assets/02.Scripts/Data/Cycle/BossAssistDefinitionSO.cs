using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Data.Cycle
{
    public enum BossAssistRole { Damage, Break, Defense, Heal, Buff, Debuff, CrowdControl }
    public enum AssistPlacementPolicy { NearPlayer, NearTarget, PlayerForwardFixed }

    [CreateAssetMenu(fileName = "BossAssistDefinition", menuName = "UPlayGround/사이클/보스 어시스트 정의")]
    public sealed class BossAssistDefinitionSO : ScriptableObject
    {
        public string assistId;
        [Tooltip("플레이어 UI에 표시할 이름. 비어 있으면 assistId를 사용한다.")]
        public string displayName;
        public string sourceBossActorId;
        public BossAssistRole role;
        public Sprite icon;
        public GameObject assistPrefab;
        public MotionSetAsset motionSet;
        [Min(1f)] public float cooldownSeconds = 45f;
        [Min(0.1f)] public float maxExecutionSeconds = 5f;
        public AssistPlacementPolicy placementPolicy;
        public Vector3 placementOffset = new(1.5f, 0f, 1.5f);
        public bool requiresTarget;
        public bool recruitableFromCentralBoss;
        [Min(1)] public int requiredDefeatCount = 3;
        [Min(0f)] public float healAmount;
    }

}
