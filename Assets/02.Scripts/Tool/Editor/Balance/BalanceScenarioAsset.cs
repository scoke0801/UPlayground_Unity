#if UNITY_EDITOR
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    [CreateAssetMenu(fileName = "BalanceScenario_", menuName = "UPlayGround/도구/Balance Scenario")]
    public sealed class BalanceScenarioAsset : ScriptableObject
    {
        [Header("Player")]
        public CharacterActorType playerCharacter = CharacterActorType.Bokusei;
        public ActorStatSO playerStatData;
        public AbilitySetSO playerAbilitySet;
        [Min(1)] public int playerLevel = 1;
        [Tooltip("playerStatData가 없을 때 사용하는 플레이어 공격력 배율")]
        [Min(0f)] public float manualPlayerAttackPower = 1f;
        [Min(0f)] public float manualPlayerDps = 45f;
        [Min(0.05f)] public float playerAttackInterval = 1.2f;

        [Header("Encounter")]
        [Min(1f)] public float targetDuration = 30f;
        [Min(0f)] public float assumedDistance = 2.5f;
        [Min(1)] public int overrideMonsterLevel = 1;
        public bool useActorDefinitionLevel = true;

        [Header("Player Defense Assumptions")]
        [Range(0f, 1f)] public float hitReceiveRate = 0.45f;
        [Range(0f, 1f)] public float guardMitigationRate = 0.35f;
        [Range(0f, 1f)] public float dodgeSuccessRate = 0.15f;
        [Range(0f, 1f)] public float parrySuccessRate = 0.05f;

        [Header("Threshold")]
        [Min(0f)] public float minAttackOpportunities = 1f;
    }
}
#endif
