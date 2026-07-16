using UnityEngine;

namespace UPlayGround.Data.Cycle
{
    // 파일명과 클래스명이 일치해야 MonoScript가 에셋에 연결된다 (CharacterWeightProfileSO.cs에서 분리).
    [CreateAssetMenu(fileName = "VitalRecoveryPolicy", menuName = "UPlayGround/사이클/바이탈 회복 정책")]
    public sealed class VitalRecoveryPolicySO : ScriptableObject
    {
        [Header("일반 유효 히트")]
        [Range(0f, 1f)] public float normalHitSpawnChance = 0.1f;
        [Min(0)] public int normalHitOrbCount = 1;
        [Min(0f)] public float normalHitHealScale = 0.25f;
        [Header("브레이크 특수공격")]
        [Range(0f, 1f)] public float specialBreakSpawnChance = 0.25f;
        [Min(0)] public int specialBreakOrbCount = 1;
        [Min(0f)] public float specialBreakHealScale = 1f;
    }
}
